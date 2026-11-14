using Statventure.Editor.Import;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Editor.Session;

/// <summary>How a file is imported (Docs/Editor.md, "Import (step 12)").</summary>
public enum ImportWay
{
    /// <summary>Through the importer its name's ending chooses (<c>.glb</c>, <c>.terrain.json</c>, <c>.css</c>, …): the entry's path is given without the ending.</summary>
    Importer,
    /// <summary>A record's JSON form (<c>.json</c> starting with <c>$types</c>; M11-18's way back in): the entry's path is given without <c>.json</c>.</summary>
    Record,
    /// <summary>A file as it is (a font, an image, a text): the entry's path is given with its ending.</summary>
    File,
}

/// <summary>
/// A file to import, as its name and start say it is imported (<see cref="EditorSession.ImportSourceOf"/>): the way, the
/// ending that chose it (lower case: <c>Arena.GLB</c>'s is <c>.glb</c>), the name without it, and the importer.
/// </summary>
public sealed record ImportSource(string File, ImportWay Way, string Ending, string Stem, ImporterInfo? Importer)
{
    /// <summary>The file's name (no folder), as the dialogs name it.</summary>
    public string Name => Path.GetFileName(File);

    /// <summary>The data path the import runs on for an entry path <paramref name="path"/>: the path with the ending, a file's path as it is.</summary>
    public string SourcePath(string path) => Way == ImportWay.File ? path : path + Ending;
}

/// <summary>What importing does with one entry it makes (the manager's decision of 2026-11-07, Q3).</summary>
public enum ImportAction
{
    /// <summary>The path is free: a new entry (not undoable, M11-7; the import dialog asked).</summary>
    Add,
    /// <summary>A record of the same kind is there: imported again over it (P29), one step of its history.</summary>
    ImportAgain,
    /// <summary>An entry of another kind (or a file over a file) is there: replaced after asking, not undoable.</summary>
    Replace,
}

/// <summary>
/// One entry an import makes, and what it does at its path: the entry as imported (a record's binary form or a file's
/// bytes), the record and its kind, the entry there now and, importing again, the comparison of values (P29).
/// </summary>
public sealed class ImportTarget
{
    internal ImportTarget(ImportedEntry entry, object? record, EditorEntry? existing, ImportAction action, ImportComparison? comparison)
    {
        Entry = entry;
        Record = record;
        Kind = record is null ? null : RecordShape.Of(record.GetType());
        Existing = existing;
        Action = action;
        Comparison = comparison;
    }

    /// <summary>The entry as the import made it (<see cref="PackWriter.ImportFile"/>).</summary>
    public ImportedEntry Entry { get; }

    public string Path => Entry.Path;

    /// <summary>The record (read from its binary form), or null for a file.</summary>
    public object? Record { get; }

    /// <summary>The record's kind, or null for a file.</summary>
    public RecordShape? Kind { get; }

    /// <summary>The entry shown at the path now, or null.</summary>
    public EditorEntry? Existing { get; }

    public ImportAction Action { get; }

    /// <summary>Importing again: the values that differ between the entry and the file (P29); otherwise null.</summary>
    public ImportComparison? Comparison { get; }
}

/// <summary>A file imported at a chosen path (<see cref="EditorSession.PlanImport"/>), not yet in the session.</summary>
public sealed class ImportPlan
{
    internal ImportPlan(ImportSource source, string path, IReadOnlyList<ImportTarget> targets)
    {
        Source = source;
        Path = path;
        Targets = targets;
        Main = targets.FirstOrDefault(t => t.Path == path) ?? targets[0];
    }

    public ImportSource Source { get; }

    /// <summary>The entry path chosen (without the importer's ending).</summary>
    public string Path { get; }

    /// <summary>The entries the import makes, in path order.</summary>
    public IReadOnlyList<ImportTarget> Targets { get; }

    /// <summary>The entry at the chosen path (the one the importers make), else the first.</summary>
    public ImportTarget Main { get; }
}

/// <summary>Why a file can't be imported, or can't be at a path, in words for the dialogs.</summary>
public sealed class ImportException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Importing files (step 12; Docs/Editor.md, "Import (step 12)", "How it works", step 12; M11-8, M11-14, M11-18, P28,
/// P29): what a file imports as and the path suggested for it; the file imported at a path as packing it there would
/// (<see cref="PackWriter.ImportFile"/>, a stylesheet's urls checked against the data the mod sees: the base and the pack
/// as edited); and its entries joining the session as unsaved work, never written into the pack (step 11 saves): into a
/// free path a new entry, over a record of the same kind imported again (the bulk data from the file, the differing
/// values kept or taken, one step of the entry's history), over anything else a replacement. Files dropped on the window
/// wait in step 8's queue (<see cref="QueueImports"/>), which the import part takes.
/// </summary>
public sealed partial class EditorSession
{
    /// <summary>The ending of a record's JSON form (a loose record's, S3-1).</summary>
    public const string JsonEnding = ".json";

    /// <summary>The endings of the files imported as they are (the design's list: fonts, images, texts).</summary>
    public static readonly IReadOnlyList<string> FileEndings = [".ttf", ".otf", ".png", ".svg", ".txt"];

    /// <summary>The importers (the engine's unless the editor is given others, G3), chosen by a file's ending.</summary>
    public Importers Importers { get; set; } = Importers.Engine;

    /// <summary>The endings the editor imports: the importers', <c>.json</c> (a record's JSON form) and <see cref="FileEndings"/>.</summary>
    public IReadOnlyList<string> ImportEndings =>
        [.. Importers.All.SelectMany(i => i.Endings).Order(StringComparer.Ordinal).Append(JsonEnding).Concat(FileEndings).Distinct()];

    /// <summary>Whether files can be imported now: a pack is open and takes changes.</summary>
    public bool CanImportFiles => Pack is not null && Writable;

    /// <summary>
    /// What <paramref name="file"/> imports as, by its name's ending in any case: through an importer, as a record's JSON
    /// form (<c>.json</c>; whether it starts as one, with <c>$types</c>, and its kind are the import's to find, as packing
    /// finds them), or as a file of one of <see cref="FileEndings"/>. Anything else is an <see cref="ImportException"/>
    /// saying why.
    /// </summary>
    public ImportSource ImportSourceOf(string file)
    {
        var full = Path.GetFullPath(file);
        var name = Path.GetFileName(full);
        if (!System.IO.File.Exists(full))
            throw new ImportException("the file isn't there");
        var lower = name.ToLowerInvariant();
        if (Importers.For(lower) is { } match)
            return new ImportSource(full, ImportWay.Importer, match.Ending, name[..^match.Ending.Length], match.Importer);
        if (lower.Length > JsonEnding.Length && lower.EndsWith(JsonEnding, StringComparison.Ordinal))
            return new ImportSource(full, ImportWay.Record, JsonEnding, name[..^JsonEnding.Length], null);
        if (FileEndings.FirstOrDefault(e => lower.Length > e.Length && lower.EndsWith(e, StringComparison.Ordinal)) is { } ending)
            return new ImportSource(full, ImportWay.File, ending, name[..^ending.Length], null);
        throw new ImportException($"the editor imports {string.Join(", ", ImportEndings.SkipLast(1))} and {ImportEndings[^1]} files");
    }

    /// <summary>
    /// The path suggested for <paramref name="source"/> (P28): the file imported once at its fitted name to learn the kind
    /// it makes (a problem with it is an <see cref="ImportException"/>), then the kind's folder and the file's name made to
    /// fit the path rule (<see cref="ImportNames"/>); a file by its ending's folder (and an importer that makes a file,
    /// not a record, its name at the top). Gives the path and the kind (null when it makes a file).
    /// </summary>
    public (string Path, RecordShape? Kind) SuggestImportPath(ImportSource source)
    {
        var trial = source.Way == ImportWay.File ? ImportNames.ForFile(source.Stem, source.Ending) : ImportNames.Fit(source.Stem);
        var entries = RunImport(source, trial);
        var main = entries.FirstOrDefault(e => e.Path == trial) ?? entries[0];
        CheckRecords(source, entries);
        if (!main.IsRecord)
            return (trial, null);
        var kind = RecordShape.Of(ReadRecord(main).GetType());
        return (ImportNames.ForRecord(kind, source.Stem), kind);
    }

    /// <summary>
    /// Why <paramref name="path"/> can't be the entry path <paramref name="source"/> is imported at (null when it can): empty,
    /// outside the path rule (with its ending too), or a path whose ending would choose another importer (or, for a file,
    /// any importer or a record's JSON form).
    /// </summary>
    public string? ImportPathProblem(ImportSource source, string path)
    {
        if (path.Length == 0)
            return "Give the entry a path.";
        if (PackPath.Problem(path) is { } problem)
            return $"That can't be its path: {problem}.";
        var sourcePath = source.SourcePath(path);
        if (PackPath.Problem(sourcePath) is { } withEnding)
            return $"That can't be its path: with its ending, {withEnding}.";
        var importer = Importers.For(sourcePath)?.Importer;
        if (source.Way == ImportWay.File)
        {
            if (importer is not null || path.EndsWith(JsonEnding, StringComparison.Ordinal))
                return $"That can't be its path: a path ending as {path[path.LastIndexOf('.')..]} would be imported as something else.";
        }
        else if (importer != source.Importer)
            // (An importer's ending added to a path always chooses an importer: only a longer ending can take it instead.)
            return $"That can't be its path: {sourcePath} would go through the {importer!.Name} importer.";
        return null;
    }

    /// <summary>
    /// Imports <paramref name="source"/> at the entry path <paramref name="path"/> (<see cref="PackWriter.ImportFile"/>: the
    /// same entries as packing it there), without changing the session: the entries it makes and what each does at its
    /// path. An <see cref="ImportException"/> says why it can't: the path (<see cref="ImportPathProblem"/>), the import's
    /// own problem (in the importer's or the packer's words), an entry that would be a folder of another or below one, or
    /// a path taken by an entry of another of the game's packs (beside a game pack in the dev setup, they are read-only).
    /// </summary>
    public ImportPlan PlanImport(ImportSource source, string path)
    {
        if (!CanImportFiles)
            throw new InvalidOperationException("no pack that takes changes is open");
        if (ImportPathProblem(source, path) is { } problem)
            throw new ImportException(problem);
        var targets = new List<ImportTarget>();
        var entries = RunImport(source, path);
        CheckRecords(source, entries);
        foreach (var entry in entries)
        {
            foreach (var shown in _entries)
            {
                if (shown.Path.StartsWith(entry.Path + "/", StringComparison.Ordinal))
                    throw new ImportException($"That can't be its path: {entry.Path} is the folder of {shown.Path}.");
                if (entry.Path.StartsWith(shown.Path + "/", StringComparison.Ordinal))
                    throw new ImportException($"That can't be its path: {shown.Path} is an entry, so it can't be a folder.");
            }
            var record = entry.IsRecord ? ReadRecord(entry) : null;
            var existing = Entry(entry.Path);
            if (existing is null)
            {
                targets.Add(new(entry, record, null, ImportAction.Add, null));
                continue;
            }
            if (existing.Source == EntrySource.Game && Pack!.IsGamePack)
                throw new ImportException($"That can't be its path: {entry.Path} is in another of the game's packs, which is read-only beside this one.");
            // (A record of the pack or a game record in a mod: one the open pack takes changes to, as the check above leaves.)
            if (record is not null && existing.Kind == RecordShape.Of(record.GetType()))
                targets.Add(new(entry, record, existing, ImportAction.ImportAgain, ImportComparison.Of(existing.Draft!, RecordDraft.From(record))));
            else
                targets.Add(new(entry, record, existing, ImportAction.Replace, null));
        }
        return new ImportPlan(source, path, targets);
    }

    /// <summary>
    /// Puts <paramref name="plan"/>'s entries into the session as unsaved work (the pack isn't written: step 11 saves),
    /// as the manager decided on 2026-11-07 (Q3): into a free path a new entry; over a record of the same kind the file's
    /// draft with the entry's values kept at every difference <paramref name="take"/> doesn't take (P29; none taken by
    /// default), one step of the entry's history (a game entry in a mod becomes the mod's copy, P7); over anything else
    /// a new entry in its place, not undoable: a pack's entry replaced is listed in <see cref="Removed"/> (saving leaves
    /// it out), a new one goes, and a game entry stays under the new one, back to the game's draft, shown again when the
    /// new one is removed or renamed. Gives the entries at the plan's paths, in its order. The plan must still fit the
    /// session (the entries at its paths as they were when it was made).
    /// </summary>
    public IReadOnlyList<EditorEntry> Import(ImportPlan plan, Func<ImportDifference, bool>? take = null)
    {
        if (!CanImportFiles)
            throw new InvalidOperationException("no pack that takes changes is open");
        foreach (var target in plan.Targets)
            if (Entry(target.Path) != target.Existing)
                throw new InvalidOperationException($"{target.Path} changed since the import was planned");
        var made = new List<EditorEntry>();
        foreach (var target in plan.Targets)
        {
            switch (target.Action)
            {
                case ImportAction.ImportAgain:
                    Commit(target.Existing!, target.Comparison!.Merge(take));
                    made.Add(target.Existing!);
                    continue;
                case ImportAction.Replace:
                    Replaced(target.Existing!);
                    break;
            }
            var entry = target.Record is { } record
                ? EditorEntry.ForNew(target.Path, RecordDraft.From(record))
                : EditorEntry.ForImportedFile(target.Path, target.Entry.Content);
            entry.Refresh(_checks);
            Insert(entry);
            EntriesChanged?.Invoke(new(EntriesChangeKind.Added, entry));
            made.Add(entry);
        }
        // (The entries' unsaved work is registered: a pack is open, so its entries were shown.)
        NotifyChanged();
        return made;
    }

    /// <summary>
    /// The data a stylesheet's urls are checked against (P28: the data the mod sees, the base and the pack): the entries
    /// shown, which are the game's other packs' (the base's) and the open pack's as edited (its own, the new and imported
    /// ones, a removed one's game entry shown again in its place).
    /// </summary>
    public bool DataHas(string path) => Entry(path) is not null;

    /// <summary>Runs the import (<see cref="PackWriter.ImportFile"/>) for the entry path <paramref name="path"/>, its problems as <see cref="ImportException"/>s.</summary>
    IReadOnlyList<ImportedEntry> RunImport(ImportSource source, string path)
    {
        try
        {
            var entries = PackWriter.ImportFile(source.File, source.SourcePath(path), new ImportSources { Exists = DataHas }, Types, Importers);
            return entries.Count > 0 ? entries : throw new ImportException("the import made no entry");
        }
        catch (DataFileException problem)
        {
            throw new ImportException(problem.Problem, problem);
        }
        catch (FileNotFoundException problem)
        {
            throw new ImportException("the file isn't there", problem);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            throw new ImportException(problem.Message, problem);
        }
    }

    /// <summary>A <c>.json</c> file packs as a record only when it starts as a record's JSON form; the editor imports no other (the design's list).</summary>
    static void CheckRecords(ImportSource source, IReadOnlyList<ImportedEntry> entries)
    {
        if (source.Way == ImportWay.Record && entries.Any(e => !e.IsRecord))
            throw new ImportException("it isn't a record's JSON form (its first key isn't $types)");
    }

    /// <summary>
    /// An imported record entry read as the session reads a pack's (<see cref="ReadEntry"/>), in a kind <see cref="Types"/>
    /// knows (another is an <see cref="ImportException"/>), and one a pack may hold (the user's Q28 of GM1, 2026-11-14: a
    /// kind marked <see cref="NotAPackKindAttribute"/>, the save, is refused, <see cref="NotAPackKindProblem"/>; with New
    /// entry leaving it out, Q26, nothing in the editor makes such an entry). Every import reads its records here: the
    /// path's suggestion and the plan, the Import button's files and dropped ones alike.
    /// </summary>
    object ReadRecord(ImportedEntry entry)
    {
        var name = RecordSerializer.RootTypeName(entry.Content);
        var type = Types.Find(name) ?? throw new ImportException($"a record of type {name}, which isn't a record type known here");
        if (NotAPackKindProblem(RecordShape.Of(type)) is { } problem)
            throw new ImportException(problem);
        // (The importer wrote it from a value of its type in the current versions: it reads back.)
        return RecordSerializer.FromBinary(type, entry.Content);
    }

    /// <summary>
    /// Why Import refuses a record of <paramref name="kind"/> (the user's Q28 of GM1): a kind marked
    /// <see cref="NotAPackKindAttribute"/> isn't something a mod can hold ("a save game (save.game) isn't something a mod can
    /// hold"); null for any other kind.
    /// </summary>
    public static string? NotAPackKindProblem(RecordShape kind)
    {
        if (kind.IsPackKind)
            return null;
        var words = kind.DisplayName.ToLowerInvariant();
        return $"{(words.Length > 0 && "aeiou".Contains(words[0]) ? "an" : "a")} {words} ({kind.Name}) isn't something a mod can hold";
    }

    /// <summary>An entry an import replaces taken out of the set (see <see cref="Import"/>).</summary>
    void Replaced(EditorEntry entry)
    {
        _entries.Remove(entry);
        switch (entry.Source)
        {
            case EntrySource.Pack:
                _removed.Add(entry);
                break;
            case EntrySource.Game:
                // Back to the game's draft; shown again (and checked again) when the new entry goes.
                entry.Reset();
                _shadowed[entry.Path] = entry;
                break;
        }
        if (_selected == entry)
        {
            _selected = null;
            SelectionChanged?.Invoke();
        }
        EntriesChanged?.Invoke(new(EntriesChangeKind.Removed, entry, entry.Path));
    }
}

public sealed partial class EditorEntry
{
    byte[]? _importedFile;

    /// <summary>
    /// A file entry imported in this session (step 12): a new entry (<see cref="EntrySource.New"/>) holding the file's bytes,
    /// which saving writes (step 11, <c>PackWriter.Add</c>) and the custom mount of the session reads (steps 10 and 13).
    /// </summary>
    internal static EditorEntry ForImportedFile(string path, byte[] content) =>
        new(path, EntrySource.New, EntryContent.File) { FileSize = content.Length, ReadOnly = true, _importedFile = content };

    /// <summary>
    /// The bytes of a file entry imported in this session (step 12), as packing the file gives them: what saving writes for
    /// it and the custom mount reads (give each reader its own copy: <c>ToArray()</c>); null for every other entry (a pack's
    /// file is read from the pack, a record is its draft).
    /// </summary>
    public ReadOnlyMemory<byte>? ImportedFile
    {
        get
        {
            // (Not a conditional: the null array converts to an empty memory, not to null.)
            if (_importedFile is null)
                return null;
            return _importedFile;
        }
    }
}
