using Statventure.Audio;
using Statventure.Editor.Checks;
using Statventure.Editor.Import;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Storage;

namespace Statventure.Editor.Session;

/// <summary>
/// Why a save is refused (M11-12): an entry and its first problem, in the words the form shows it; or a problem of the
/// data that no entry shows (<see cref="Entry"/> null).
/// </summary>
public sealed record SaveProblem(string? Entry, string Problem)
{
    public override string ToString() => Entry is null ? Problem : $"{Entry}: {Problem}";
}

/// <summary>
/// An entry Save As offers (M11-16, P24, the manager's decision Q4 of 2026-11-08): whether it can be kept (an entry whose
/// content is damaged can't), whether it starts ticked (the entries the editor reads; those it can't read as records start
/// unticked), and why it can't be read (null for one that can).
/// </summary>
public sealed record SaveAsChoice(EditorEntry Entry, bool Offered, bool Ticked, string? Reason)
{
    /// <summary>Kept as its pack stores it, unread (an entry the editor can't read, offered: a new file holding it opens read-only).</summary>
    public bool KeptUnread => Offered && Reason is not null;
}

/// <summary>
/// Saving and mods (step 11; Docs/Editor.md, "Saving and mods (steps 7 and 11)" and "How it works", step 11): the whole
/// pack written as step 2 says (P23: unchanged entries as they are stored, edited ones in the current versions, removed
/// ones left out, imported files with their bytes; a new file swapped in), refused while an entry has a problem (M11-12,
/// step 10's checks), the file's index hash compared first (P23); after a save the pack is read again and the entries
/// stay, their saved state now their current draft (the manager's decision Q6 of 2026-11-08: histories kept). Save As
/// writes the entries ticked to a new file, checked first as the game would see them (Q5), and opens it (Q3); New mod
/// writes a mod holding its info into the player's mods folder at once and opens it (M11-5, M11-15, P25, Q2). No DOM.
/// </summary>
public sealed partial class EditorSession
{
    /// <summary>Whether the open pack can be saved over now (it takes changes: a mod, or in the dev setup one of the game's packs).</summary>
    public bool CanSave => Pack is not null && Writable;

    /// <summary>Whether a pack is open, so Save As can write its entries to a new file.</summary>
    public bool CanSaveAs => Pack is not null;

    /// <summary>
    /// The entries a save writes, by path: the open pack's own (at their paths now), the new ones (made, duplicated,
    /// imported) and the game's entries changed in this session (the mod's own copies, P7). Removed entries and the game's
    /// unchanged ones aren't among them.
    /// </summary>
    public IReadOnlyList<EditorEntry> SaveEntries => [.. Entries.Where(e => e.Source != EntrySource.Game || e.IsChanged)];

    /// <summary>
    /// Why a save of the open pack is refused (M11-12, P18): the checks across entries run first if they are due
    /// (<see cref="EntryChecks.RunPending"/>), then each entry with a problem with its first (step 10's
    /// <see cref="EntriesWithProblems"/>, in path order) and the data's problem no entry shows (<see cref="EntryChecks.DataProblem"/>).
    /// Empty when it can be saved.
    /// </summary>
    public IReadOnlyList<SaveProblem> SaveProblems()
    {
        Checks.RunPending();
        var problems = EntriesWithProblems.Select(e => new SaveProblem(e.Path, e.Problems[0].ToString())).ToList();
        if (Checks.DataProblem is { } data)
            problems.Add(new SaveProblem(null, data));
        return problems;
    }

    /// <summary>
    /// Whether another program changed the open pack's file since it was opened or last saved here (P23): null when its
    /// index hash is the one read then; else why ("was changed by another program since it was opened here", "isn't there
    /// any more", "can't be read now: …").
    /// </summary>
    public string? ChangedOnDisk()
    {
        if (Pack is not { } pack)
            return null;
        try
        {
            using var now = Packs.PackFile.Open(pack.File);
            return now.IndexHashHex == pack.IndexHashHex ? null : "was changed by another program since it was opened here";
        }
        catch (Exception problem) when (problem is FileNotFoundException or DirectoryNotFoundException)
        {
            return "isn't there any more";
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            return $"can't be read now: {(problem is DataFileException data ? data.Problem : problem.Message)}";
        }
    }

    /// <summary>
    /// Saves the open pack over its file (M11-17, P23): every entry of <see cref="SaveEntries"/> written (an unchanged one
    /// as its pack stores it, at its path now; an edited, new or copied record in the current versions; an imported file
    /// with its bytes), the removed ones left out, the new file swapped in (<see cref="PackWriter.Write"/>). Refused (an
    /// <see cref="InvalidOperationException"/>, nothing written) when it can't be saved over or <see cref="SaveProblems"/>
    /// has any; the view asks first about a file changed on disk (<see cref="ChangedOnDisk"/>). Then the pack is read
    /// again (later reads see the new file, its index hash is the one compared next time) and the entries stay as they
    /// are, saved: they lose their marks, their histories kept.
    /// </summary>
    public PackWriteResult Save()
    {
        if (!CanSave)
            throw new InvalidOperationException("the open pack can't be saved over");
        if (SaveProblems() is { Count: > 0 } problems)
            throw new InvalidOperationException($"the pack can't be saved: {problems[0]}");
        var saved = SaveEntries;
        var file = Pack!.File;
        var result = Writer(saved).Write(file);
        Saved(file, saved);
        return result;
    }

    /// <summary>A writer holding <paramref name="entries"/> as a save writes them (P23).</summary>
    PackWriter Writer(IEnumerable<EditorEntry> entries)
    {
        var writer = new PackWriter();
        foreach (var entry in entries)
        {
            if (entry.ImportedFile is { } bytes)
                writer.Add(entry.Path, bytes.Span);
            else if (WrittenFromDraft(entry))
                writer.AddRecord(entry.Path, entry.Kind!.Type, entry.Build!.Value!);
            else
                writer.AddFrom(PackFile!, PackFile!.Find(entry.SavedPath!)!, entry.Path);
        }
        return writer;
    }

    /// <summary>Whether a save writes <paramref name="entry"/> from its draft (an edited, new or copied record), not as its pack stores it.</summary>
    static bool WrittenFromDraft(EditorEntry entry) => entry.IsRecord && (entry.Source != EntrySource.Pack || entry.IsChanged);

    /// <summary>After a save over <paramref name="file"/>: the pack read again, the removed ones forgotten, the entries saved.</summary>
    void Saved(string file, IReadOnlyList<EditorEntry> saved)
    {
        var reopened = Packs.PackFile.Open(file);
        _packFile!.Dispose();
        _packFile = reopened;
        _open = _open! with { IndexHashHex = reopened.IndexHashHex };
        _removed.Clear();
        var marked = new List<EditorEntry>();
        foreach (var entry in saved)
        {
            if (entry.Source == EntrySource.Pack && !entry.IsChanged && !entry.IsRenamed)
                continue;
            if (entry.Source == EntrySource.Game)
                // The mod holds its own copy now: the game's entry goes under it (Revert shows it again).
                _shadowed[entry.Path] = EditorEntry.ForRecord(entry.Path, entry.Saved!, EntrySource.Game, entry.ReadOnly);
            entry.MarkSaved();
            marked.Add(entry);
        }
        // (Their problems stay as they were: the same values at the same paths.)
        foreach (var entry in marked)
            EntryChanged?.Invoke(entry);
        NotifyChanged();
    }

    // ---------- Save As (M11-16, P24; the manager's decisions Q3–Q5 of 2026-11-08) ----------

    /// <summary>
    /// What Save As offers (Q4): every entry of <see cref="SaveEntries"/>, by path. The entries the editor reads start ticked;
    /// those it can't read as records (a kind not known here, a newer version, a record that doesn't read) are offered
    /// unticked with why, kept as their pack stores them when ticked; an entry whose content is damaged isn't offered.
    /// </summary>
    public IReadOnlyList<SaveAsChoice> SaveAsChoices()
    {
        var choices = new List<SaveAsChoice>();
        foreach (var entry in SaveEntries)
        {
            if (entry.Content != EntryContent.Unreadable)
            {
                choices.Add(new SaveAsChoice(entry, true, true, null));
                continue;
            }
            var intact = true;
            try
            {
                PackFile!.Read(PackFile.Find(entry.SavedPath!)!);
            }
            catch (DataFileException)
            {
                intact = false;
            }
            choices.Add(new SaveAsChoice(entry, intact, false, entry.Reason));
        }
        return choices;
    }

    /// <summary>
    /// Why Save As can't write <paramref name="file"/> (null when it can): one of the game's packs outside the repo's dev
    /// setup (M11-5), or the open pack when it can't be saved over (M11-16).
    /// </summary>
    public string? SaveAsFileProblem(string file)
    {
        var name = System.IO.Path.GetFileName(file);
        if (!Game.DevSetup && IsGamePack(file))
            return $"{name} is one of the game's packs, which are read-only (only the repo's dev setup saves into them)";
        if (Pack is { } pack && !CanSave && string.Equals(System.IO.Path.GetFullPath(file), pack.File, FileBrowser.NameComparison))
            return $"{name} is the pack open here, which can't be saved over: {pack.ReadOnlyReason ?? "it is read-only"}";
        return null;
    }

    /// <summary>
    /// Why the entries <paramref name="ticked"/> can't be saved as a pack of their own at <paramref name="file"/> (Q5): the
    /// data as the game would see it with only them over the game's packs (the open pack's base with them mounted on top,
    /// named by <paramref name="file"/>), checked as step 10's checks across entries check the session (each record entry
    /// that data shows, the ticked ones and the game's: its kind's checks against the data, its references resolved there,
    /// the theme's path rule), then the music's loading while no music entry has a problem. A ticked record whose draft
    /// doesn't build gives its first build problem. Each entry with its first problem, in path order, then a problem of
    /// the data no entry shows; empty when they can be saved. An entry the editor can't read is kept unread and not
    /// checked (G2).
    /// </summary>
    public IReadOnlyList<SaveProblem> SaveAsProblems(IEnumerable<EditorEntry> ticked, string file)
    {
        if (Base is not { } over)
            throw new InvalidOperationException("no pack is open");
        var chosen = ticked.ToDictionary(e => e.Path, StringComparer.Ordinal);
        var found = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var values = new List<(string Path, object Value)>();
        foreach (var entry in chosen.Values)
        {
            if (!entry.IsRecord)
                continue;
            if (!entry.Builds)
                found[entry.Path] = entry.Problems[0].ToString();
            else
                values.Add((entry.Path, entry.Build!.Value!));
        }
        foreach (var game in GameEntries())
            if (!chosen.ContainsKey(game.Path) && game.Saved is { } value)
                values.Add((game.Path, value));
        string? dataProblem = null;
        try
        {
            using var data = SaveAsData(over, chosen, file);
            var references = new SubsetReferences(this, chosen);
            foreach (var (path, value) in values)
            {
                IReadOnlyList<RecordProblem> problems;
                try
                {
                    problems = [.. RecordChecks.Check(value, data, references), .. EntryChecks.PathRules(path, value)];
                }
                catch (Exception problem) when (problem is not OutOfMemoryException)
                {
                    problems = [new(RecordPath.Root, $"its check failed: {problem.Message}")];
                }
                if (problems.Count > 0)
                    found[path] = problems[0].ToString();
            }
            if (!found.Keys.Any(path => path.StartsWith("audio/", StringComparison.Ordinal)))
            {
                try
                {
                    MusicLibrary.Load(data);
                }
                catch (DataFileException problem)
                {
                    if (problem.Entry is { } entry && values.Exists(v => v.Path == entry))
                        found[entry] = problem.Problem;
                    else
                        dataProblem = problem.Message;
                }
            }
        }
        catch (DataFileException problem)
        {
            // The entries ticked can't be mounted together (a record beside a file named as its JSON form).
            dataProblem = problem.Message;
        }
        var result = found.Select(p => new SaveProblem(p.Key, p.Value)).ToList();
        if (dataProblem is not null)
            result.Add(new SaveProblem(null, dataProblem));
        return result;
    }

    /// <summary>
    /// The data as the game would see it with only the entries <paramref name="ticked"/> saved at <paramref name="file"/> (Q5):
    /// the open pack's base with them mounted on top, named by the file, each as Save As writes it. A
    /// <see cref="DataFileException"/> when they can't be mounted together.
    /// </summary>
    internal VirtualFileSystem SaveAsData(IEnumerable<EditorEntry> ticked, string file) =>
        SaveAsData(Base ?? throw new InvalidOperationException("no pack is open"), ticked.ToDictionary(e => e.Path, StringComparer.Ordinal), file);

    VirtualFileSystem SaveAsData(VirtualFileSystem over, Dictionary<string, EditorEntry> chosen, string file) =>
        new([.. over.Mounts, Mount.Custom(System.IO.Path.GetFullPath(file), new SubsetEntries(this, chosen))]);

    /// <summary>The game's entries the open pack is seen over, each once: those shown (changed or not) and those under the pack's entries.</summary>
    IEnumerable<EditorEntry> GameEntries() => Entries.Where(e => e.Source == EntrySource.Game).Concat(_shadowed.Values);

    /// <summary>
    /// Save As (M11-16, P24; Q3–Q5): the entries <paramref name="ticked"/> (of <see cref="SaveAsChoices"/>, offered ones)
    /// written to <paramref name="file"/> as a save writes them (an entry the editor can't read as its pack stores it, unread),
    /// the new file swapped in, then opened in place of the open pack (<see cref="Open(string)"/>: the entries read again, the
    /// histories begun anew; changes of entries not ticked are let go). Refused (an <see cref="InvalidOperationException"/>,
    /// nothing written) for a file <see cref="SaveAsFileProblem"/> refuses and while <see cref="SaveAsProblems"/> has any.
    /// </summary>
    public OpenPack SaveAs(string file, IEnumerable<EditorEntry> ticked)
    {
        if (!CanSaveAs)
            throw new InvalidOperationException("no pack is open");
        var chosen = ticked.ToList();
        var entries = SaveEntries;
        foreach (var entry in chosen)
            if (!entries.Contains(entry))
                throw new ArgumentException($"{entry.Path} isn't an entry a save writes", nameof(ticked));
        file = System.IO.Path.GetFullPath(file);
        if (SaveAsFileProblem(file) is { } fileProblem)
            throw new InvalidOperationException(fileProblem);
        if (SaveAsProblems(chosen, file) is { Count: > 0 } problems)
            throw new InvalidOperationException($"the entries can't be saved: {problems[0]}");
        Writer(chosen).Write(file);
        return Open(file);
    }

    /// <summary>The entries ticked for Save As, as the game would read them from the new file (Q5).</summary>
    sealed class SubsetEntries(EditorSession session, Dictionary<string, EditorEntry> chosen) : IMountEntries
    {
        public IEnumerable<string> Paths => chosen.Keys;

        public bool IsRecord(string path)
        {
            var entry = chosen[path];
            return entry.ImportedFile is null && (WrittenFromDraft(entry) || session.PackFile!.Find(entry.SavedPath!)!.IsRecord);
        }

        public byte[] Read(string path)
        {
            var entry = chosen[path];
            if (entry.ImportedFile is { } bytes)
                return bytes.ToArray();
            if (WrittenFromDraft(entry))
                return RecordSerializer.ToBinary(entry.Kind!.Type, entry.Build!.Value!);
            return session.PackFile!.Read(session.PackFile.Find(entry.SavedPath!)!);
        }
    }

    /// <summary>The kinds of the record entries the data of a Save As shows (Q5): the ticked entries over the game's.</summary>
    sealed class SubsetReferences(EditorSession session, Dictionary<string, EditorEntry> chosen) : IReferenceLookup
    {
        public string? KindAt(string path)
        {
            if (chosen.TryGetValue(path, out var entry))
                return entry.Kind?.Name;
            var shown = session.Entry(path);
            var game = shown is { Source: EntrySource.Game } ? shown : session.Overridden(path);
            return game?.Kind?.Name;
        }
    }

    // ---------- New mod (M11-5, M11-15, P25; the manager's decision Q2 of 2026-11-08) ----------

    /// <summary>The file name New mod suggests for a mod named <paramref name="name"/> (P25): the name made to fit the path rule, with <c>.pak</c>; "" for no name.</summary>
    public static string NewModFileName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
            return "";
        var stem = ImportNames.Fit(trimmed);
        // (Nothing of the name fits the rule: a mod, not import's "imported".)
        if (stem == ImportNames.Fallback && !trimmed.Equals(ImportNames.Fallback, StringComparison.OrdinalIgnoreCase))
            stem = "mod";
        return stem + ".pak";
    }

    /// <summary>
    /// Why New mod can't make a mod named <paramref name="name"/> in <paramref name="fileName"/> (null when it can): a name
    /// is needed; the file's name ends in <c>.pak</c>, is one name (no folder) under the path rule, and isn't a mod's already in
    /// the mods folder (in any case: never replaced).
    /// </summary>
    public string? NewModProblem(string name, string fileName)
    {
        if (name.Trim().Length == 0)
            return "Give the mod a name.";
        if (fileName.Length == 0)
            return "Give the mod's file a name.";
        if (!fileName.EndsWith(".pak", StringComparison.Ordinal))
            return "A mod's file name ends in .pak.";
        if (fileName.Contains('/'))
            return "That can't be the file's name: it holds a '/'.";
        if (PackPath.Problem(fileName) is { } problem)
            return $"That can't be the file's name: {problem}.";
        if (Directory.Exists(ModsFolder))
            foreach (var there in Directory.EnumerateFileSystemEntries(ModsFolder))
                if (System.IO.Path.GetFileName(there) is var other && other.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                    return $"There is already {other} in the mods folder.";
        return null;
    }

    /// <summary>
    /// New mod (M11-5, M11-15, P25, Q2): a pack in the player's mods folder holding only the mod's info (<see cref="ModInfo.Path"/>,
    /// its name; author, version and description empty), written at once, then opened in place of the open pack (whose
    /// unsaved work is lost: the view asks first). Refused (an <see cref="ArgumentException"/>) for what
    /// <see cref="NewModProblem"/> refuses.
    /// </summary>
    public OpenPack NewMod(string name, string fileName)
    {
        if (NewModProblem(name, fileName) is { } problem)
            throw new ArgumentException(problem, nameof(fileName));
        var writer = new PackWriter();
        writer.AddRecord(ModInfo.Path, new ModInfo { Name = name.Trim() });
        var file = System.IO.Path.Combine(ModsFolder, fileName);
        writer.Write(file);
        return Open(file);
    }
}

public sealed partial class EditorEntry
{
    /// <summary>
    /// The entry was saved (step 11): it is the open pack's at its path now, its current draft its saved state (a record:
    /// its value built), an imported file read from the pack from now on; its history stays (the
    /// manager's decision Q6 of 2026-11-08), an undo past the save making it changed again.
    /// </summary>
    internal void MarkSaved()
    {
        // (An unchanged record's built value is the one it was read as.)
        if (IsRecord)
        {
            Saved = _build!.Value;
            SavedDraft = Draft;
            // (The cache of whether a draft is the saved one needs no reset: the draft it knows is the saved draft now.)
            _savedBinary = null;
        }
        Source = EntrySource.Pack;
        SavedPath = Path;
        _importedFile = null;
    }
}
