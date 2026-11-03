namespace Statventure.Packs;

/// <summary>
/// A pack or loose file the game's data can't use: damaged, of a format version this engine doesn't
/// read, or holding a path outside the rule. It names the file and, when it is about one entry, the
/// entry (Docs/Assets.md, Q6); nothing is read from a lower mount instead, and the game stops with it.
/// </summary>
public sealed class DataFileException(string file, string? entry, string problem)
    : IOException(entry is null ? $"{file}: {problem}" : $"{file}: entry '{entry}': {problem}")
{
    /// <summary>The pack, or the loose file, at fault (a full path).</summary>
    public string File { get; } = file;

    /// <summary>The entry at fault, or null when the problem is the file's as a whole.</summary>
    public string? Entry { get; } = entry;

    /// <summary>What is wrong, without the names.</summary>
    public string Problem { get; } = problem;
}
