namespace Statventure.Packs;

/// <summary>The files of a loose folder as entries: their paths below it, under the path rule (Q29: a file outside it is an error naming the file).</summary>
internal static class LooseFolder
{
    /// <summary>
    /// Every file under <paramref name="folder"/> (at any depth, hidden ones too) as its entry path,
    /// below <paramref name="at"/> when given, and its full path.
    /// </summary>
    public static List<(string Path, string File)> Scan(string folder, string at = "")
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
        var files = new List<(string Path, string File)>();
        foreach (var file in Directory.EnumerateFiles(root, "*", options))
        {
            var relative = System.IO.Path.GetRelativePath(root, file).Replace(System.IO.Path.DirectorySeparatorChar, '/');
            var path = at.Length == 0 ? relative : at + "/" + relative;
            if (PackPath.Problem(path) is { } problem)
                throw new DataFileException(file, null, $"a name outside the path rule: {problem}");
            files.Add((path, file));
        }
        return files;
    }
}
