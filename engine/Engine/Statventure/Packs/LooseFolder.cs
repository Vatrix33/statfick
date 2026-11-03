namespace Statventure.Packs;

/// <summary>A file of a loose folder: its path below the folder and its full path.</summary>
internal readonly record struct LooseFile(string Path, string File);

/// <summary>A loose folder's entry: its path, the file that gives it, and whether it is a record (the file is its JSON form).</summary>
internal readonly record struct LooseEntry(string Path, string File, bool Record);

/// <summary>
/// The files of a loose folder as entries (Docs/Assets.md, Q7, Q29, S3-1): every file under the path
/// rule (a file outside it is an error naming the file), a <c>&lt;path&gt;.json</c> file that starts as a
/// record's JSON form being the record <c>&lt;path&gt;</c>.
/// </summary>
internal static class LooseFolder
{
    /// <summary>
    /// Every file under <paramref name="folder"/> (at any depth, hidden ones too) with its path below
    /// it, below <paramref name="at"/> when given, in ordinal path order.
    /// </summary>
    public static List<LooseFile> Files(string folder, string at = "")
    {
        var root = System.IO.Path.GetFullPath(folder);
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException($"No folder {root}");
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = 0,
            IgnoreInaccessible = false,
            MatchType = MatchType.Simple,
        };
        var files = new List<LooseFile>();
        foreach (var file in Directory.EnumerateFiles(root, "*", options))
        {
            var relative = System.IO.Path.GetRelativePath(root, file).Replace(System.IO.Path.DirectorySeparatorChar, '/');
            var path = at.Length == 0 ? relative : at + "/" + relative;
            if (PackPath.Problem(path) is { } problem)
                throw new DataFileException(file, null, $"a name outside the path rule: {problem}");
            files.Add(new LooseFile(path, file));
        }
        files.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
        return files;
    }

    /// <summary>
    /// The entries of <paramref name="folder"/>: each file, or the record whose JSON form it is (each
    /// <c>.json</c> file's start read to tell). Two files giving one entry (a record <c>a</c> beside a file
    /// <c>a</c>), or an entry that is also a folder of others (a record <c>a</c> beside <c>a/b</c>), are errors
    /// naming the file, as packing them would be.
    /// </summary>
    public static List<LooseEntry> Scan(string folder, string at = "") => Entries(Files(folder, at));

    /// <summary>The entries the files give (see <see cref="Scan"/>).</summary>
    public static List<LooseEntry> Entries(IEnumerable<LooseFile> files)
    {
        var entries = new List<LooseEntry>();
        foreach (var (path, file) in files)
        {
            if (!LooseRecord.MayBeRecord(path) || !LooseRecord.FileStartsAsRecord(file))
            {
                entries.Add(new LooseEntry(path, file, false));
                continue;
            }
            var record = LooseRecord.Twin(path, record: false);
            if (record is null || PackPath.Problem(record) is not null)
                throw new DataFileException(file, null, $"a record's JSON form, but '{record}' isn't a path under the rule");
            entries.Add(new LooseEntry(record, file, true));
        }
        CheckDistinct(entries);
        return entries;
    }

    static void CheckDistinct(List<LooseEntry> entries)
    {
        var byPath = new Dictionary<string, LooseEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
            if (!byPath.TryAdd(entry.Path, entry))
            {
                var record = entry.Record ? entry : byPath[entry.Path];
                var file = entry.Record ? byPath[entry.Path] : entry;
                throw new DataFileException(record.File, null, $"the record '{entry.Path}', and the file {file.File} is that entry too");
            }
        foreach (var entry in entries)
            for (var slash = entry.Path.IndexOf('/'); slash >= 0; slash = entry.Path.IndexOf('/', slash + 1))
                if (byPath.TryGetValue(entry.Path[..slash], out var folder))
                    throw new DataFileException(folder.File, null, $"the record '{folder.Path}', which is also the folder of {entry.File}");
    }
}
