using Statventure.Audio;
using Statventure.Records;

namespace Statventure.Packs;

/// <summary>
/// Which of a mods folder's mods mount (GM1-P13; Docs/Game/Foundation.md, "Mods: the list and the check at start"): asked
/// by a mods mount (<see cref="Mount.Mods"/>) each time the file system mounts it (when made, and on every
/// <see cref="VirtualFileSystem.Reload"/>), with the mods found there in the order they mount (full paths) and the mounts
/// below the mods folder (the game's data the mods go over, <paramref name="game"/>); gives the mods to mount. Those mount
/// in the order they were found; a file it gives that wasn't among <paramref name="mods"/> is ignored. The game gives one
/// (<c>ApplicationOptions.ModFilter</c>) that leaves out a mod switched off and a mod <see cref="ModCheck.Check"/> refuses.
/// </summary>
public delegate IEnumerable<string> ModFilter(IReadOnlyList<string> mods, IReadOnlyList<Mount> game);

/// <summary>
/// A problem the mod check found (<see cref="ModCheck"/>), in the editor's words: the entry at fault and what is wrong, or
/// (<see cref="Entry"/> null) a problem of the pack or of the data as a whole.
/// </summary>
public sealed record ModProblem(string? Entry, string Problem)
{
    /// <summary>As the editor's refusal to save lists it: "entry: problem", or the problem alone.</summary>
    public override string ToString() => Entry is null ? Problem : $"{Entry}: {Problem}";
}

/// <summary>What <see cref="ModCheck.Check"/> found for one mod: its file, its info (null when it has none that reads) and its problems, the first first.</summary>
public sealed record ModReport(string File, ModInfo? Info, IReadOnlyList<ModProblem> Problems)
{
    /// <summary>Whether the mod passed: no problem, so the game mounts it and the editor would save it.</summary>
    public bool Passed => Problems.Count == 0;
}

/// <summary>
/// One entry of a pack as the editor and the mod check read it (<see cref="ModCheck.Read"/>): a record of a kind known
/// here (<see cref="Record"/>, its value), a file (neither set), or unreadable (<see cref="Problem"/>, why).
/// </summary>
public sealed record PackEntryRead(PackEntry Entry, object? Record, string? Problem)
{
    /// <summary>A file (not a record), which read.</summary>
    public bool IsFile => Record is null && Problem is null;
}

/// <summary>A record entry for <see cref="ModCheck.CheckRecords"/>: its path, its value (null for a record that has none, a draft that doesn't build) and its own problems.</summary>
public sealed record RecordToCheck(string Path, object? Value, IReadOnlyList<RecordProblem> Own);

/// <summary>
/// What <see cref="ModCheck.CheckRecords"/> found: for each record given, in the same order, what the data adds to its own
/// problems (the music's problem among them when it names that record); and a problem of the data no record can show.
/// </summary>
public sealed record RecordsChecked(IReadOnlyList<IReadOnlyList<RecordProblem>> Added, string? DataProblem);

/// <summary>
/// The one check of a mod that the editor's save and the game's start share (GM1-P13; Docs/Game/Foundation.md, "Mods: the
/// list and the check at start"; the user's Q4: a broken mod is what the editor refuses to save). <see cref="Check"/> checks a
/// mod alone over the game's data, as the editor checks a mod it has open over the game's packs: the pack opens (its header
/// and its index's hash); every entry reads (its content's hash) and every record entry is a kind known here (the engine's
/// and the game's data library's) at a version it reads (<see cref="Read"/>, the editor's words for an entry it can't
/// read); then every record the data shows, the mod's and the game's below it, passes its checks with the game's data and
/// the mod as its data and the references resolved over them (<see cref="Problems"/>, <see cref="CheckRecords"/>: the
/// kinds' checks and the marks', the rules only an entry's path tells, <see cref="PathRules"/>), then the game's music
/// loading on it while no music record has a problem (G5: a theme that doesn't compile with the mod); and the mod's
/// <c>mod/info</c>, when there, is a mod's info that reads (<see cref="InfoProblem"/>). The editor's checks are built on
/// the same pieces (Tools/Editor/Checks/EntryChecks.cs and the session's saving), so both refuse the same, in the same
/// words: each entry's first problem as the editor's form shows it (its own check's first, then what the data adds, the
/// first at each path), in path order, then the data's problem.
/// </summary>
public static class ModCheck
{
    /// <summary>The folder the music is read from: while a record there has a problem, the music's loading isn't tried.</summary>
    const string MusicFolder = "audio/";

    /// <summary>
    /// The mod in <paramref name="file"/> checked alone over <paramref name="game"/> (the mounts the mods go over: the game's
    /// packs and its <c>classes/</c>), its record kinds <paramref name="kinds"/> (the engine's and the data library's). Its
    /// problems, the first first: the pack's that it doesn't open (one, in the words the editor's Open says it with, with the
    /// entry its index names when it refuses one); else
    /// the entries that can't be read (each with why); else each record with a problem, its first (the mod's and the game's,
    /// in path order, a file at <c>mod/info</c> among them), then the data's problem. A check that fails for another reason
    /// is a problem too ("its check failed: …"), never an exception. Nothing is written.
    /// </summary>
    public static ModReport Check(string file, IReadOnlyList<Mount> game, RecordTypes kinds)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(kinds);
        file = Path.GetFullPath(file);
        PackFile pack;
        try
        {
            pack = PackFile.Open(file);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            // (An entry the pack's index names, when the index refuses one.)
            return new ModReport(file, null, [new ModProblem((problem as DataFileException)?.Entry, OpenProblem(problem))]);
        }
        using (pack)
        {
            ModInfo? info = null;
            try
            {
                var reads = pack.Entries.Select(entry => Read(pack, entry, kinds)).ToList();
                info = reads.FirstOrDefault(r => r.Entry.Path == ModInfo.Path)?.Record as ModInfo;
                var unreadable = reads.Where(r => r.Problem is not null).Select(r => new ModProblem(r.Entry.Path, r.Problem!)).ToList();
                if (unreadable.Count > 0)
                    return new ModReport(file, info, unreadable);
                return new ModReport(file, info, Checked(file, reads, game, kinds));
            }
            catch (Exception problem) when (problem is not OutOfMemoryException)
            {
                // (A file that can't be read on, the system's error; anything else that throws.)
                return new ModReport(file, info, [new ModProblem(null, $"its check failed: {problem.Message}")]);
            }
        }
    }

    /// <summary>Why a pack can't be opened, as the editor's Open says it: the pack's problem, "the file isn't there", or the system's message.</summary>
    static string OpenProblem(Exception problem) => problem switch
    {
        DataFileException data => data.Problem,
        FileNotFoundException or DirectoryNotFoundException => "the file isn't there",
        _ => problem.Message,
    };

    /// <summary>The record checks of a mod whose entries all read: the data the game would see with it, every record it shows checked.</summary>
    static List<ModProblem> Checked(string file, List<PackEntryRead> reads, IReadOnlyList<Mount> game, RecordTypes kinds)
    {
        VirtualFileSystem data;
        try
        {
            data = new VirtualFileSystem([.. game, Mount.Pack(file)]);
        }
        catch (DataFileException problem)
        {
            // The mod can't be mounted over the game's data (the editor's "entries that can't be mounted together").
            return [new ModProblem(null, problem.Message)];
        }
        using (data)
        {
            var mine = reads.ToDictionary(r => r.Entry.Path, StringComparer.Ordinal);
            var records = new List<RecordToCheck>();
            var kindsAt = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var path in data.List())
            {
                // The mod is on top: a path it holds is its entry; any other is the game's.
                var value = mine.TryGetValue(path, out var read) ? read.Record : GameRecord(data, path, kinds);
                if (value is null)
                    continue;
                kindsAt[path] = RecordTypes.NameOf(value.GetType());
                records.Add(new RecordToCheck(path, value, Problems(path, value)));
            }
            var result = CheckRecords(records, data, new KindLookup(kindsAt));
            var problems = new List<ModProblem>();
            for (var i = 0; i < records.Count; i++)
                if (FirstPerPath([.. records[i].Own, .. result.Added[i]]) is { Count: > 0 } shown)
                    problems.Add(new ModProblem(records[i].Path, shown[0].ToString()));
            // A file where a mod keeps its info (a record of another kind there is a path rule, above).
            if (mine.TryGetValue(ModInfo.Path, out var info) && info.IsFile)
            {
                var at = problems.FindIndex(p => string.CompareOrdinal(p.Entry, ModInfo.Path) > 0);
                problems.Insert(at < 0 ? problems.Count : at, new ModProblem(ModInfo.Path, InfoProblem(null)));
            }
            if (result.DataProblem is { } dataProblem)
                problems.Add(new ModProblem(null, dataProblem));
            return problems;
        }
    }

    /// <summary>The game's record at <paramref name="path"/> (below the mod), or null for a file or a record the game's data can't give (which the editor shows unreadable and doesn't check).</summary>
    static object? GameRecord(VirtualFileSystem data, string path, RecordTypes kinds)
    {
        if (!data.IsRecord(path))
            return null;
        try
        {
            return data.Load(kinds, path);
        }
        catch (Exception problem) when (problem is DataFileException or RecordFormatException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The kinds of the record entries the data shows, by path (the editor's lookup is its session's entries, the same).</summary>
    sealed class KindLookup(Dictionary<string, string> kinds) : IReferenceLookup
    {
        public string? KindAt(string path) => kinds.GetValueOrDefault(path);
    }

    /// <summary>
    /// One entry of <paramref name="pack"/> read as the editor reads it when it opens a pack (M11-16, P24; the reasons in the
    /// words of the pack's dump, <see cref="PackDump"/>): a record of a kind <paramref name="kinds"/> knows, with its value; a
    /// file; or unreadable with why: its content damaged (it doesn't match its hash), a record whose binary form doesn't
    /// start as one, of a kind not known here, or that doesn't read as its kind (a newer version than the engine reads,
    /// damaged data, values its type refuses).
    /// </summary>
    public static PackEntryRead Read(PackFile pack, PackEntry entry, RecordTypes kinds)
    {
        byte[] content;
        try
        {
            content = pack.Read(entry);
        }
        catch (DataFileException problem)
        {
            return new PackEntryRead(entry, null, problem.Problem);
        }
        if (!entry.IsRecord)
            return new PackEntryRead(entry, null, null);
        string name;
        try
        {
            name = RecordSerializer.RootTypeName(content);
        }
        catch (RecordFormatException problem)
        {
            return new PackEntryRead(entry, null, $"not a readable record: {problem.Message}");
        }
        if (kinds.Find(name) is not { } type)
            return new PackEntryRead(entry, null, $"a record of type {name}, which isn't a record type known here");
        try
        {
            return new PackEntryRead(entry, RecordSerializer.FromBinary(type, content), null);
        }
        catch (Exception problem) when (problem is RecordFormatException or ArgumentException)
        {
            return new PackEntryRead(entry, null, $"not a readable {name} record: {problem.Message}");
        }
    }

    /// <summary>
    /// The problems of the record <paramref name="value"/> at <paramref name="path"/>: its kind's checks and its marks'
    /// (<see cref="RecordChecks.Check"/>, with <paramref name="data"/> and <paramref name="references"/> when given), then the
    /// rules only its path tells (<see cref="PathRules"/>). A check that fails (a kind's own check throwing: a bug, or an
    /// entry it reads that the data can't give) is the record's problem, in its words ("its check failed: …"), rather than
    /// the end of the checks.
    /// </summary>
    public static IReadOnlyList<RecordProblem> Problems(string path, object value, VirtualFileSystem? data = null, IReferenceLookup? references = null)
    {
        try
        {
            return [.. RecordChecks.Check(value, data, references), .. PathRules(path, value)];
        }
        catch (Exception problem) when (problem is not OutOfMemoryException)
        {
            return [new(RecordPath.Root, $"its check failed: {problem.Message}")];
        }
    }

    /// <summary>
    /// The rules outside the kinds' checks that only the entry's path tells (Docs/Editor.md, "How it works", step 5): a theme
    /// in the music's folder must hold the id its path stores (<see cref="MusicLibrary.Load"/>'s words); a record at
    /// <c>mod/info</c> must be a mod's info (<see cref="InfoProblem"/>).
    /// </summary>
    public static IEnumerable<RecordProblem> PathRules(string path, object value)
    {
        if (value is ThemeSpec theme && path.StartsWith(MusicNames.Themes + "/", StringComparison.Ordinal)
            && MusicNames.FromPathName(path[(MusicNames.Themes.Length + 1)..]) is { } name && theme.Id != name)
            yield return new(RecordPath.Root.Field(nameof(ThemeSpec.Id)), $"the theme's id is '{theme.Id}', but its path holds '{name}' (a theme's path is made from its id)");
        if (path == ModInfo.Path && value is not ModInfo)
            yield return new(RecordPath.Root, InfoProblem(RecordTypes.NameOf(value.GetType())));
    }

    /// <summary>
    /// Why what a mod holds at <c>mod/info</c> isn't its info (the mod's name, author, version and description the game's mod
    /// list shows): a record of the kind <paramref name="kind"/>, or (null) a file. A mod's info that is there must read as
    /// the engine's <c>mod.info</c>; its other ways of not reading (damaged, a newer version, not a record) are those of any
    /// entry (<see cref="Read"/>).
    /// </summary>
    public static string InfoProblem(string? kind) =>
        $"a mod's info must be a mod.info record ({(kind is null ? "this is a file" : $"this is a {kind} record")})";

    /// <summary>The first problem at each path, in order (the manager's decision of 2026-11-06: the editor shows only it).</summary>
    public static IReadOnlyList<RecordProblem> FirstPerPath(IEnumerable<RecordProblem> problems)
    {
        var seen = new HashSet<RecordPath>();
        return [.. problems.Where(p => seen.Add(p.Path))];
    }

    /// <summary>
    /// The checks across records (the editor's, a pause after the last change, P17; the mod check's): each record with a
    /// value checked with <paramref name="data"/> and its references resolved by <paramref name="references"/>
    /// (<see cref="Problems"/>), what that adds to its own problems kept for it (the same path and words being the same
    /// problem); then, with data and while no record under <c>audio/</c> has a problem (or has no value: a draft that doesn't
    /// build), the game's music loading on the data (<see cref="Music"/>: what only it refuses, a music folder holding
    /// another kind, a name the music's rule can't store, music without a theme order, a theme that doesn't compile with
    /// the data), its problem added to the record it names when that is one given with a value, else the data's problem.
    /// </summary>
    public static RecordsChecked CheckRecords(IReadOnlyList<RecordToCheck> records, VirtualFileSystem? data, IReferenceLookup? references)
    {
        var added = new IReadOnlyList<RecordProblem>[records.Count];
        for (var i = 0; i < records.Count; i++)
            added[i] = records[i].Value is { } value ? Without(Problems(records[i].Path, value, data, references), records[i].Own) : [];
        return new RecordsChecked(added, data is null ? null : MusicProblem(records, data, added));
    }

    /// <summary>The music's loading for <see cref="CheckRecords"/>: null when it loads or isn't tried, or its problem is a record's (added to it there).</summary>
    static string? MusicProblem(IReadOnlyList<RecordToCheck> records, VirtualFileSystem data, IReadOnlyList<RecordProblem>[] added)
    {
        for (var i = 0; i < records.Count; i++)
            if (records[i].Path.StartsWith(MusicFolder, StringComparison.Ordinal) && (records[i].Value is null || records[i].Own.Count > 0 || added[i].Count > 0))
                return null;
        if (Music(data) is not { } problem)
            return null;
        if (problem.Entry is { } path)
            for (var i = 0; i < records.Count; i++)
                if (records[i].Path == path && records[i].Value is not null)
                {
                    added[i] = [.. added[i], new(RecordPath.Root, problem.Problem)];
                    return null;
                }
        return problem.Message;
    }

    /// <summary>The game's music loading (<see cref="MusicLibrary.Load"/>) on <paramref name="data"/>: null when it loads, else its problem naming the pack and entry.</summary>
    public static DataFileException? Music(VirtualFileSystem data)
    {
        try
        {
            MusicLibrary.Load(data);
            return null;
        }
        catch (DataFileException problem)
        {
            return problem;
        }
    }

    /// <summary><paramref name="all"/> without the problems of <paramref name="some"/> (by path and words).</summary>
    static IReadOnlyList<RecordProblem> Without(IReadOnlyList<RecordProblem> all, IReadOnlyList<RecordProblem> some)
    {
        var known = new HashSet<RecordProblem>(some);
        return [.. all.Where(p => !known.Contains(p))];
    }
}
