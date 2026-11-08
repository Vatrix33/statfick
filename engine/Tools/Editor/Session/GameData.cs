using Statventure.Packs;

namespace Statventure.Editor.Session;

/// <summary>
/// The game the editor works on (P5, M11-5; M12 step 6): where its packs are and whether the editor may save into them,
/// and the record kinds and importers its data holds. The dev setup (<c>--repo &lt;engine folder&gt;</c>, what
/// <c>Tools/Scripts/editor.sh</c> passes) has the repo's <c>engine/Assets/Packs/</c>, which it can open and save in
/// place; an installed game (its folder found by <see cref="Game.GameLookup"/>: <c>--game</c>, the folder remembered,
/// or the "Where is Stat Fick?" screen) its <c>packs/</c>, read-only. The kinds are the engine's and those of the game's
/// data library (<c>Statventure.Game.Data.dll</c>, M12-9, Q19) once <see cref="Game.GameLookup"/> has loaded it; the
/// factories here give the engine's only (the tests' sessions).
/// </summary>
public sealed record GameData(string PacksFolder, bool DevSetup)
{
    /// <summary>The game's folder (holding its program, <c>packs/</c> and its data library), or null in the dev setup.</summary>
    public string? Folder { get; init; }

    /// <summary>The data library the kinds were taken from, or null when they are the engine's only.</summary>
    public string? Library { get; init; }

    /// <summary>The record kinds of the game's data: the engine's and the data library's.</summary>
    public RecordTypes Types { get; init; } = RecordTypes.Engine;

    /// <summary>The importers the editor offers: the engine's and the data library's.</summary>
    public Importers Importers { get; init; } = Importers.Engine;

    /// <summary>The dev setup: the repo's <c>Assets/Packs</c> under <paramref name="engineFolder"/>, writable (the engine's kinds).</summary>
    public static GameData Repo(string engineFolder) => new(Path.GetFullPath(Path.Combine(engineFolder, "Assets", "Packs")), true);

    /// <summary>An installed game: its <c>packs/</c> folder under <paramref name="gameFolder"/>, read-only (the engine's kinds).</summary>
    public static GameData Installed(string gameFolder) =>
        new(Path.GetFullPath(Path.Combine(gameFolder, "packs")), false) { Folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(gameFolder)) };
}
