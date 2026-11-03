namespace Statventure.Packs;

/// <summary>
/// The one rule every path in the game's data follows (Docs/Assets.md, Q4, Q28 and Q31): an entry's
/// path in a pack, a file's path in a loose folder, a path asked of the virtual file system. Only
/// lowercase ASCII letters, digits, '-', '_', '.' and '/'; '/' separates names, none of them empty;
/// and nothing Windows can't hold as a file, so a path works the same in a pack and in a loose folder
/// on Linux and Windows: no name ending in a dot (which also rules out "." and ".."), no reserved
/// device name (con, prn, aux, nul, com0–com9, lpt0–lpt9, alone or before an extension), no name
/// longer than 255 characters. Checked when packing, when mounting and on every call.
/// </summary>
public static class PackPath
{
    /// <summary>The longest name between two '/' (Linux's and Windows' file systems' limit).</summary>
    public const int MaxNameLength = 255;

    /// <summary>Whether <paramref name="path"/> follows the rule.</summary>
    public static bool IsValid(string path) => Problem(path) is null;

    /// <summary>What in <paramref name="path"/> breaks the rule, or null when it follows it.</summary>
    public static string? Problem(string path)
    {
        if (path.Length == 0)
            return "the path is empty";
        foreach (var c in path)
            if (!IsAllowed(c))
                return $"'{c}' isn't allowed (only a–z, 0–9, '-', '_', '.' and '/')";
        var start = 0;
        while (true)
        {
            var end = path.IndexOf('/', start);
            var name = end < 0 ? path.AsSpan(start) : path.AsSpan(start, end - start);
            if (NameProblem(name) is { } problem)
                return problem;
            if (end < 0)
                return null;
            start = end + 1;
        }
    }

    /// <summary>Throws an <see cref="ArgumentException"/> naming the problem when <paramref name="path"/> breaks the rule.</summary>
    public static void Check(string path, string parameter = "path")
    {
        if (Problem(path) is { } problem)
            throw new ArgumentException($"'{path}' isn't a valid data path: {problem}", parameter);
    }

    static bool IsAllowed(char c) => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.' or '/';

    static string? NameProblem(ReadOnlySpan<char> name)
    {
        if (name.Length == 0)
            return "a name is empty (a leading, trailing or doubled '/')";
        if (name.Length > MaxNameLength)
            return $"a name is longer than {MaxNameLength} characters";
        if (name[^1] == '.')
            return $"the name '{name}' ends in a dot";
        var dot = name.IndexOf('.');
        if (IsDeviceName(dot < 0 ? name : name[..dot]))
            return $"'{name}' is a name Windows keeps for a device";
        return null;
    }

    static bool IsDeviceName(ReadOnlySpan<char> stem) =>
        stem is "con" or "prn" or "aux" or "nul"
        || stem.Length == 4 && (stem.StartsWith("com") || stem.StartsWith("lpt")) && char.IsAsciiDigit(stem[3]);
}
