namespace Statventure.Editor.Session;

/// <summary>
/// Where the game's packs are and whether the editor may save into them (P5, M11-5): the dev setup
/// (<c>--repo &lt;engine folder&gt;</c>, what <c>Tools/Scripts/editor.sh</c> passes) has the repo's
/// <c>engine/Assets/Packs/</c>, which it can open and save in place; otherwise the game's <c>packs/</c> folder
/// (beside the program, where M12 puts it, or <c>--game &lt;folder&gt;</c>'s), read-only. The editor's own
/// UI takes its fonts from <see cref="UiPack"/> (the game's Lato, P4).
/// </summary>
public sealed record GameData(string PacksFolder, bool DevSetup)
{
    /// <summary>The game's UI pack (fonts) in <see cref="PacksFolder"/>.</summary>
    public string UiPack => Path.Combine(PacksFolder, "ui.pak");

    /// <summary>The dev setup: the repo's <c>Assets/Packs</c> under <paramref name="engineFolder"/>, writable.</summary>
    public static GameData Repo(string engineFolder) => new(Path.GetFullPath(Path.Combine(engineFolder, "Assets", "Packs")), true);

    /// <summary>An installed game: its <c>packs/</c> folder under <paramref name="gameFolder"/>, read-only.</summary>
    public static GameData Installed(string gameFolder) => new(Path.GetFullPath(Path.Combine(gameFolder, "packs")), false);

    /// <summary>Why the editor can't start on this data (no <c>ui.pak</c>, so no fonts), or null.</summary>
    public string? Problem() => File.Exists(UiPack) ? null : $"the game's packs aren't in {PacksFolder}: ui.pak isn't there";
}
