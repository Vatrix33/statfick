using System.Runtime.CompilerServices;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Editor.Session;

/// <summary>
/// The data as the game would see it after saving (Docs/Editor.md, "The program", P38; "How it works", step 13,
/// "The data as edited"): the game's packs the open pack is seen over (<see cref="Base"/>'s mounts, in their order)
/// with a custom mount on top giving the open pack's entries as edited, so the engine's own loaders and cross-entry
/// checks read unsaved work unchanged (the preview, step 13; the checks across entries, step 10). Owned by step 13
/// (the manager's decision of 2026-11-07); step 10 builds on it.
/// </summary>
public sealed partial class EditorSession
{
    VirtualFileSystem? _edited;
    VirtualFileSystem? _editedOver;
    EditedEntries? _editedEntries;
    Dictionary<string, (EditorEntry Entry, bool Record)>? _editedFailed;
    bool _editedOwned;

    /// <summary>
    /// The data as edited: a file system of <see cref="Base"/>'s mounts with a custom mount on top
    /// (<see cref="Mount.Custom"/>, named by the open pack's file, so a loader's error names the pack and the entry as
    /// the saved pack's would). The custom mount gives every entry of the open pack (by its path now: renamed ones at
    /// their new path, removed ones not at all, so a game entry under a removed one shows through as it would in the
    /// game), every new entry (a file imported in this session with its bytes, step 12's <see cref="EditorEntry.ImportedFile"/>)
    /// and every game entry changed in this session (the mod's own copy, P7): a record as its
    /// <see cref="EditorEntry.LastBuilt"/> value's binary form (an entry whose draft doesn't build gives the last value
    /// that did, M11-12; a new entry that never built isn't there), an unchanged record of the pack and every file and
    /// unreadable entry as the pack stores it (reading a damaged one throws the pack's error). Null while no pack is
    /// open, or when the entries can't be mounted (<see cref="EditedDataProblem"/> says why). Brought up to date each time
    /// it is read (so also from a listener of the session's events, whichever runs first): its paths and kinds are taken
    /// again and, when they differ from those mounted (an entry added, removed or renamed, a game entry made the mod's
    /// copy or taken back), <see cref="VirtualFileSystem.Reload"/> runs on it (its <see cref="VirtualFileSystem.Reloaded"/>
    /// listeners hear it); its bytes are read from the entries whenever a path is read. A new pack opened gives a new file
    /// system (the old one disposed): read this property each time the data is used rather than keeping it across changes.
    /// On the editor's thread, as the session.
    /// </summary>
    public VirtualFileSystem? EditedData
    {
        get
        {
            if (!_editedOwned)
            {
                _editedOwned = true;
                Own(new EditedDataCloser(this));
            }
            if (Base is not { } over || Pack is not { } pack)
            {
                DropEditedData();
                EditedDataProblem = null;
                return null;
            }
            if (_editedOver != over)
            {
                // A pack opened since: mounted afresh.
                DropEditedData();
                _editedOver = over;
                _editedEntries = new EditedEntries(this, pack.File);
            }
            var entries = _editedEntries!;
            var mounted = entries.Snapshot;
            entries.Take();
            if (_edited is not null && entries.SameAs(mounted))
            {
                _editedFailed = null;
                EditedDataProblem = null;
                return _edited;
            }
            // Entries that couldn't be mounted, unchanged since: not tried again.
            if (_editedFailed is { } failed && entries.SameAs(failed))
                return null;
            try
            {
                if (_edited is null)
                    _edited = new VirtualFileSystem([.. over.Mounts, Mount.Custom(pack.File, entries)]);
                else
                    _edited.Reload();
            }
            catch (DataFileException problem)
            {
                // A file system that was mounted keeps the mounts it had, with the entries they were taken from.
                _editedFailed = entries.Snapshot;
                if (_edited is not null)
                    entries.Restore(mounted);
                EditedDataProblem = problem.Message;
                return null;
            }
            _editedFailed = null;
            EditedDataProblem = null;
            return _edited;
        }
    }

    /// <summary>Why the session's entries can't be mounted as the data as edited (two entries a pack can't hold together), or null.</summary>
    public string? EditedDataProblem { get; private set; }

    void DropEditedData()
    {
        _edited?.Dispose();
        _edited = null;
        _editedOver = null;
        _editedEntries = null;
        _editedFailed = null;
    }

    sealed class EditedDataCloser(EditorSession session) : IDisposable
    {
        public void Dispose() => session.DropEditedData();
    }

    /// <summary>
    /// The custom mount's entries: which paths it gives (taken by <see cref="Take"/>: at mount and before a Reload) and
    /// each path's bytes, read from its entry when the path is read.
    /// </summary>
    sealed class EditedEntries(EditorSession session, string name) : IMountEntries
    {
        /// <summary>A record's binary form per value built (a large record isn't written again on every read).</summary>
        readonly ConditionalWeakTable<object, byte[]> _binary = [];

        public string Name { get; } = name;

        /// <summary>The paths given, each with its entry and whether it is a record, as last taken.</summary>
        public Dictionary<string, (EditorEntry Entry, bool Record)> Snapshot { get; private set; } = new(StringComparer.Ordinal);

        public void Take()
        {
            var taken = new Dictionary<string, (EditorEntry, bool)>(StringComparer.Ordinal);
            foreach (var entry in session.Entries)
                if (Gives(entry) && RecordFlag(entry) is { } record)
                    taken[entry.Path] = (entry, record);
            Snapshot = taken;
        }

        public void Restore(Dictionary<string, (EditorEntry Entry, bool Record)> snapshot) => Snapshot = snapshot;

        public bool SameAs(Dictionary<string, (EditorEntry Entry, bool Record)> other) =>
            other.Count == Snapshot.Count && other.All(p => Snapshot.TryGetValue(p.Key, out var mine) && mine == p.Value);

        /// <summary>
        /// Whether the custom mount gives <paramref name="entry"/>: the open pack's, a new one that has built or is a file
        /// imported in this session, a game entry changed in this session.
        /// </summary>
        static bool Gives(EditorEntry entry) => entry.Source switch
        {
            EntrySource.Pack => true,
            EntrySource.New => entry.LastBuilt is not null || entry.ImportedFile is not null,
            _ => entry.IsRecord && entry.IsChanged && entry.LastBuilt is not null,
        };

        /// <summary>Whether it is given as a record (an unreadable entry as its pack stores it); null when its pack doesn't hold it.</summary>
        bool? RecordFlag(EditorEntry entry)
        {
            if (entry.ImportedFile is not null)
                return false;
            if (entry.Content == EntryContent.Record && (entry.Source != EntrySource.Pack || entry.IsChanged))
                return true;
            return entry.SavedPath is { } saved && session.PackFile?.Find(saved) is { } stored ? stored.IsRecord : null;
        }

        IEnumerable<string> IMountEntries.Paths => Snapshot.Keys;

        bool IMountEntries.IsRecord(string path) => Snapshot[path].Record;

        byte[] IMountEntries.Read(string path)
        {
            var entry = Snapshot[path].Entry;
            if (entry.Content == EntryContent.Record && (entry.Source != EntrySource.Pack || entry.IsChanged) && entry.LastBuilt is { } value)
            {
                var binary = _binary.GetValue(value, v => RecordSerializer.ToBinary(entry.Kind!.Type, v));
                return (byte[])binary.Clone();
            }
            if (entry.ImportedFile is { } imported)
                return imported.ToArray();
            return session.PackFile!.Read(session.PackFile.Find(entry.SavedPath!)!);
        }
    }
}
