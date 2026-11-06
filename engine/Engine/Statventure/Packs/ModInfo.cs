using Statventure.Records;

namespace Statventure.Packs;

/// <summary>
/// A mod's name, author, version and description, as text (Docs/Editor.md, G1; M11-15): the record entry at
/// <see cref="Path"/> (<c>mod/info</c>) that a mod made by the editor carries inside its pack, a kind the
/// engine knows so that its own tools (the pack's dump, the packer) show it and a mod list in the game can read
/// it later from each mod file. The game never reads it for now: mods load as they do without it, and a mod
/// without one still loads. Each field may be empty, and an absent key in the JSON form is empty text.
/// </summary>
[Record("mod.info", 1)]
public sealed record ModInfo
{
    /// <summary>Where a mod carries its info: the entry <c>mod/info</c>.</summary>
    public const string Path = "mod/info";

    /// <summary>The mod's name as players see it.</summary>
    [Field(1)] public string Name { get; init; } = "";

    /// <summary>Who made it.</summary>
    [Field(2)] public string Author { get; init; } = "";

    /// <summary>The mod's own version, as its author writes it ("1.2", "beta 3").</summary>
    [Field(3)] public string Version { get; init; } = "";

    /// <summary>What it changes, in the author's words.</summary>
    [Field(4)] public string Description { get; init; } = "";
}
