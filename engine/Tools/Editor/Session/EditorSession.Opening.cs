using Statventure.Core;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Storage;

namespace Statventure.Editor.Session;

/// <summary>
/// The pack the session has open (Docs/Editor.md, "Opening packs (step 8)"): its file, whether it is one of the
/// game's packs, the index's hash as it was when opened (P23: saving compares it), and why it doesn't take
/// changes (null when it does): entries it can't read (M11-16, then <see cref="Unreadable"/> lists them), or one of
/// the game's packs outside the dev setup (M11-5, P5).
/// </summary>
public sealed record OpenPack(string File, bool IsGamePack, string IndexHashHex, string? ReadOnlyReason, IReadOnlyList<EditorEntry> Unreadable)
{
    /// <summary>Its file name, as the title names it.</summary>
    public string Name => System.IO.Path.GetFileName(File);

    /// <summary>Whether it takes changes (<see cref="EditorSession.Writable"/>).</summary>
    public bool Writable => ReadOnlyReason is null;
}

/// <summary>
/// Opening a pack (step 8; Docs/Editor.md, "Opening packs (step 8)"): one pack at a time over the game's packs (P6),
/// every entry read and classified (a record of a kind the session knows, a file, or unreadable with its reason:
/// M11-16, P24), the open pack's entries over the game's (<see cref="ShowEntries"/>); the base the open pack is seen
/// over (a <see cref="VirtualFileSystem"/> of the game's other packs); the player's folders (its mods, the editor's
/// own settings, G6); and the files dropped to import, queued for import (step 12) to take.
/// </summary>
public sealed partial class EditorSession
{
    /// <summary>The folder in the player's game folder that holds the editor's own settings (G6).</summary>
    public const string SettingsFolderName = "editor";

    string? _playerFolder;
    PackFile? _packFile;
    VirtualFileSystem? _base;
    OpenPack? _open;
    bool _ownsOpening;
    readonly List<string> _imports = [];

    /// <summary>
    /// The player's game folder (its <c>mods/</c>, the editor's <c>editor/</c>): the application's
    /// (<c>Application.Storage.Folder</c>, which follows <c>--player-folder</c>), by default this machine's (in tests the
    /// run's redirected folder).
    /// </summary>
    public string PlayerFolder
    {
        get => _playerFolder ??= GameFolder.ForThisMachine();
        set => _playerFolder = System.IO.Path.GetFullPath(value);
    }

    /// <summary>The player's mods folder (where mods are opened from and saved to; it may not exist yet).</summary>
    public string ModsFolder => System.IO.Path.Combine(PlayerFolder, GameStorage.ModsName);

    /// <summary>The editor's settings folder, <c>&lt;player folder&gt;/editor</c> (G6).</summary>
    public string SettingsFolder => System.IO.Path.Combine(PlayerFolder, SettingsFolderName);

    /// <summary>
    /// The game's packs: the area packs in <see cref="GameData.PacksFolder"/> that are there, in the order the game mounts
    /// them (<see cref="Application.AreaPacks"/>; other files there aren't the game's).
    /// </summary>
    public IReadOnlyList<string> GamePacks =>
        [.. Application.AreaPacks.Select(area => System.IO.Path.Combine(Game.PacksFolder, area + ".pak")).Where(File.Exists)];

    /// <summary>Whether <paramref name="file"/> is one of the game's packs (<see cref="GamePacks"/>).</summary>
    public bool IsGamePack(string file)
    {
        var full = System.IO.Path.GetFullPath(file);
        return GamePacks.Any(pack => string.Equals(pack, full, FileBrowser.NameComparison));
    }

    /// <summary>The pack open now, or null.</summary>
    public OpenPack? Pack => _open;

    /// <summary>The open pack's file as read when it was opened (saving adds its unchanged entries from it, step 11), or null.</summary>
    public PackFile? PackFile => _packFile;

    /// <summary>
    /// The base the open pack is seen over (P6): the game's packs other than the open one, mounted in the game's order;
    /// null while no pack is open. The custom mount of the session's entries goes on top of it (steps 10, 13).
    /// </summary>
    public VirtualFileSystem? Base => _base;

    /// <summary>
    /// The game's packs that couldn't be read when the open pack was opened, each with why (their entries aren't
    /// shown and they aren't in <see cref="Base"/>); empty when all could.
    /// </summary>
    public IReadOnlyList<string> GamePackProblems { get; private set; } = [];

    /// <summary>A pack was opened (<see cref="Pack"/> is the new one).</summary>
    public event Action? PackOpened;

    /// <summary>
    /// Opens <paramref name="file"/> (<see cref="Packs.PackFile.Open"/>, then <see cref="Open(Packs.PackFile)"/>).
    /// A pack <c>Open</c> refuses throws as it does (a <see cref="DataFileException"/> naming why; an
    /// <see cref="IOException"/> or <see cref="UnauthorizedAccessException"/> for a file that can't be read), and
    /// nothing changes.
    /// </summary>
    public OpenPack Open(string file) => Open(Packs.PackFile.Open(file));

    /// <summary>
    /// Opens <paramref name="pack"/> (the session owns it from now on), in place of the pack open before (its unsaved
    /// work is lost: the view asks first). Every entry of it and of the game's other packs is read and classified
    /// (<see cref="ReadEntry"/>); the pack takes changes unless it has entries that can't be read (M11-16) or is one of
    /// the game's packs outside the dev setup (M11-5); in the dev setup the game's other packs' entries are shown
    /// read-only beside one of the game's packs (copying one would put its path in two of the game's packs). The
    /// entries are shown (<see cref="ShowEntries"/>: the selection cleared), the title names the pack.
    /// </summary>
    public OpenPack Open(PackFile pack)
    {
        ArgumentNullException.ThrowIfNull(pack);
        var isGamePack = IsGamePack(pack.FilePath);
        var entries = new List<EditorEntry>();
        var unreadable = new List<EditorEntry>();
        foreach (var packEntry in pack.Entries)
        {
            var entry = ReadEntry(pack, packEntry, EntrySource.Pack, Types);
            if (entry.Content == EntryContent.Unreadable)
                unreadable.Add(entry);
            entries.Add(entry);
        }
        var mounts = new List<Mount>();
        var problems = new List<string>();
        foreach (var file in GamePacks)
        {
            if (string.Equals(file, pack.FilePath, FileBrowser.NameComparison))
                continue;
            try
            {
                using var game = Packs.PackFile.Open(file);
                foreach (var packEntry in game.Entries)
                    entries.Add(ReadEntry(game, packEntry, EntrySource.Game, Types, readOnly: isGamePack));
                mounts.Add(Mount.Pack(file));
            }
            catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
            {
                problems.Add($"{System.IO.Path.GetFileName(file)}: {(problem is DataFileException data ? data.Problem : problem.Message)}");
            }
        }
        VirtualFileSystem baseData;
        try
        {
            baseData = new VirtualFileSystem(mounts);
        }
        catch
        {
            pack.Dispose();
            throw;
        }
        var reason = unreadable.Count > 0
            ? unreadable.Count == 1 ? "an entry can't be read" : $"{unreadable.Count} entries can't be read"
            : isGamePack && !Game.DevSetup ? "the game's packs open read-only (only the repo's dev setup saves into them)"
            : null;
        CloseOpenPack();
        _packFile = pack;
        _base = baseData;
        _open = new OpenPack(pack.FilePath, isGamePack, pack.IndexHashHex, reason, unreadable);
        GamePackProblems = problems;
        if (!_ownsOpening)
        {
            _ownsOpening = true;
            Own(new Closer(this));
        }
        Writable = reason is null;
        ShowEntries(entries);
        PackName = _open.Name;
        PackOpened?.Invoke();
        NotifyChanged();
        return _open;
    }

    /// <summary>
    /// One entry of a pack read as the session shows it (M11-16, P24; the reasons in the words of the pack's dump,
    /// <see cref="PackDump"/>): a record of a kind <paramref name="types"/> knows (<see cref="EditorEntry.ForRecord"/>,
    /// read-only when <paramref name="readOnly"/>), a file (<see cref="EditorEntry.ForFile"/>), or unreadable
    /// (<see cref="EditorEntry.ForUnreadable"/>) with why: its content damaged (it doesn't match its hash), a record
    /// whose binary form doesn't start as one, of a kind not known here, or that doesn't read as its kind (a newer
    /// version than the engine reads, damaged data, values its type refuses). Read as the shared mod check reads it
    /// (<see cref="ModCheck.Read"/>, GM1-P13), so an entry the editor can't read leaves a mod out of the game too.
    /// </summary>
    public static EditorEntry ReadEntry(PackFile pack, PackEntry entry, EntrySource source, RecordTypes types, bool readOnly = false)
    {
        var read = ModCheck.Read(pack, entry, types);
        if (read.Problem is { } problem)
            return EditorEntry.ForUnreadable(entry.Path, problem, source);
        return read.Record is { } record ? EditorEntry.ForRecord(entry.Path, record, source, readOnly) : EditorEntry.ForFile(entry.Path, entry.ContentLength, source);
    }

    // ---------- Files dropped to import (step 12 takes them) ----------

    /// <summary>The files waiting to be imported, in the order they came (dropped on the window, P27).</summary>
    public IReadOnlyList<string> PendingImports => _imports;

    /// <summary>Files were queued for import (step 12's import takes them with <see cref="TakeImports"/>).</summary>
    public event Action? ImportsQueued;

    /// <summary>Whether something takes the queued imports (step 12 listens to <see cref="ImportsQueued"/>).</summary>
    public bool CanImport => ImportsQueued is not null;

    /// <summary>Queues <paramref name="files"/> for import, after those already waiting, in their order.</summary>
    public void QueueImports(IEnumerable<string> files)
    {
        var count = _imports.Count;
        _imports.AddRange(files);
        if (_imports.Count > count)
            ImportsQueued?.Invoke();
    }

    /// <summary>The files waiting to be imported, in order; the queue is empty after it.</summary>
    public IReadOnlyList<string> TakeImports()
    {
        var files = _imports.ToList();
        _imports.Clear();
        return files;
    }

    void CloseOpenPack()
    {
        _base?.Dispose();
        _base = null;
        _packFile?.Dispose();
        _packFile = null;
    }

    /// <summary>Closes the open pack and its base when the session is disposed.</summary>
    sealed class Closer(EditorSession session) : IDisposable
    {
        public void Dispose() => session.CloseOpenPack();
    }
}
