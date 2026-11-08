using System.Text.Json;
using System.Text.Json.Nodes;
using Statventure.Editor.Session;
using Statventure.Editor.Settings;

namespace Statventure.Editor.Game;

/// <summary>Why a folder isn't a game the editor can work on.</summary>
public enum GameProblemKind
{
    /// <summary>The folder isn't there.</summary>
    Missing,
    /// <summary>It doesn't hold the game: its <c>packs/</c> folder or its data library isn't in it.</summary>
    NotTheGame,
    /// <summary>Its data library can't be read or its kinds loaded.</summary>
    Unreadable,
    /// <summary>The game is of another version than the editor (Q18).</summary>
    OtherVersion,
    /// <summary>The dev setup's data library isn't built (<c>--repo</c>).</summary>
    NotBuilt,
    /// <summary>Another build of the data library is loaded in this run (Q-C: only a new start can use this one).</summary>
    AnotherBuild,
}

/// <summary>
/// Why <see cref="Folder"/> isn't a game the editor can work on, with what the sentence needs: what isn't in it
/// (<see cref="Detail"/>), why its library can't be read, the game's <see cref="Version"/>, the library loaded instead.
/// </summary>
public sealed record GameProblem(GameProblemKind Kind, string Folder, string? Detail = null, string? Version = null)
{
    /// <summary>
    /// The problem in a sentence (no capital, no full stop: the command line and the screen add theirs) for the editor
    /// of <paramref name="editorVersion"/>: a remembered folder says "any more" where that is the reason.
    /// </summary>
    public string Text(string editorVersion, bool remembered = false) => Kind switch
    {
        GameProblemKind.Missing => remembered ? $"Stat Fick isn't in {Folder} any more" : $"{Folder} isn't there",
        GameProblemKind.NotTheGame => remembered
            ? $"{Folder} doesn't hold Stat Fick any more: {Detail} {(Detail!.Contains(" and ", StringComparison.Ordinal) ? "aren't" : "isn't")} in it"
            : $"{Folder} isn't Stat Fick's folder: {Detail} {(Detail!.Contains(" and ", StringComparison.Ordinal) ? "aren't" : "isn't")} in it",
        GameProblemKind.Unreadable => $"Stat Fick's data library in {Folder} can't be used: {Detail}",
        GameProblemKind.OtherVersion => $"{Folder} holds Stat Fick {Version}, and this editor works only with Stat Fick {editorVersion}",
        GameProblemKind.NotBuilt => $"the game's data library isn't built: {Detail} isn't there (Tools/Scripts/editor.sh builds it)",
        _ => $"this editor has already loaded another build of Stat Fick's data library (from {Detail})",
    };

    /// <summary>As <see cref="Text"/>, a sentence of its own: a capital first, a full stop last.</summary>
    public string Sentence(string editorVersion, bool remembered = false)
    {
        var text = Text(editorVersion, remembered);
        return char.ToUpperInvariant(text[0]) + text[1..] + ".";
    }
}

/// <summary>A game found (with its kinds loaded), or why not; <see cref="Folder"/> the folder it is about (none: nothing to look at).</summary>
public sealed record GameFound(GameData? Game, GameProblem? Problem, string? Folder = null);

/// <summary>
/// Finding the game (Q16, Q18, Q19; Docs/Packaging.md, "The editor on its own"; Docs/Editor.md, "How it works", M12 step 6).
/// A game's folder holds its <c>packs/</c> folder and its data library, <c>Statventure.Game.Data.dll</c> (its program
/// isn't looked at: build.sh's dev layout, whose program is <c>Statventure.Sandbox</c>, is a game folder too); its
/// version is the library's (read from its metadata, <see cref="GameLibrary.Read"/>), which must be the editor's
/// (Q18: a release's exactly; a dev build matches any dev build); then its kinds are loaded
/// (<see cref="GameLibraryLoader.Kinds"/>). Where the folder comes from: <c>--repo</c> (the repo's packs and the library's
/// build there), <c>--game</c>, else the folder remembered in <c>editor/game.json</c> (<see cref="RememberedGame"/>),
/// else the "Where is Stat Fick?" screen (<see cref="GameScreen"/>), which remembers the folder chosen.
/// </summary>
public static class GameLookup
{
    /// <summary>
    /// Whether <paramref name="folder"/> holds a game the editor of <paramref name="editorVersion"/> works with: its
    /// library (read, not loaded), or the problem.
    /// </summary>
    public static (GameLibraryFile? Library, GameProblem? Problem) Check(string folder, string editorVersion)
    {
        var full = Full(folder);
        if (!Directory.Exists(full))
            return (null, new GameProblem(GameProblemKind.Missing, full));
        var file = Path.Combine(full, GameLibrary.FileName);
        var packs = Directory.Exists(Path.Combine(full, "packs"));
        var library = File.Exists(file);
        if (!packs || !library)
            return (null, new GameProblem(GameProblemKind.NotTheGame, full,
                !packs && !library ? $"its packs folder and {GameLibrary.FileName}" : !packs ? "its packs folder" : GameLibrary.FileName));
        return CheckLibrary(full, file, editorVersion);
    }

    static (GameLibraryFile?, GameProblem?) CheckLibrary(string folder, string file, string editorVersion)
    {
        GameLibraryFile read;
        try
        {
            read = GameLibrary.Read(file);
        }
        catch (Exception problem) when (problem is GameLibraryException or IOException or UnauthorizedAccessException)
        {
            return (null, new GameProblem(GameProblemKind.Unreadable, folder, problem.Message));
        }
        if (read.Version != editorVersion)
            return (null, new GameProblem(GameProblemKind.OtherVersion, folder, Version: read.Version));
        return (read, null);
    }

    /// <summary>
    /// The game in <paramref name="folder"/>, checked and its kinds loaded (by <paramref name="loader"/>, by default the
    /// run's), for the editor of <paramref name="editorVersion"/>.
    /// </summary>
    public static GameFound Find(string folder, string editorVersion, GameLibraryLoader? loader = null)
    {
        var full = Full(folder);
        var (library, problem) = Check(full, editorVersion);
        if (problem is not null)
            return new GameFound(null, problem, full);
        return Load(GameData.Installed(full), full, library!, loader ?? GameLibraryLoader.Run);
    }

    /// <summary>The dev setup (<c>--repo</c>): the repo's packs, writable, with the kinds of the library built in the repo (<see cref="GameLibrary.InRepo"/>).</summary>
    public static GameFound Repo(string engineFolder, string editorVersion, GameLibraryLoader? loader = null)
    {
        var game = GameData.Repo(engineFolder);
        var file = GameLibrary.InRepo(engineFolder);
        var folder = Path.GetDirectoryName(file)!;
        if (!File.Exists(file))
            return new GameFound(null, new GameProblem(GameProblemKind.NotBuilt, folder, file), folder);
        var (library, problem) = CheckLibrary(folder, file, editorVersion);
        if (problem is not null)
            return new GameFound(null, problem, folder);
        return Load(game, folder, library!, loader ?? GameLibraryLoader.Run);
    }

    static GameFound Load(GameData game, string folder, GameLibraryFile library, GameLibraryLoader loader)
    {
        if (!loader.CanUse(library))
            return new GameFound(null, new GameProblem(GameProblemKind.AnotherBuild, folder, loader.Loaded?.File), folder);
        try
        {
            var (types, importers) = loader.Kinds(library);
            return new GameFound(game with { Library = library.File, Types = types, Importers = importers }, null, folder);
        }
        catch (GameLibraryException problem)
        {
            return new GameFound(null, new GameProblem(GameProblemKind.Unreadable, folder, problem.Message), folder);
        }
    }

    /// <summary>
    /// The game remembered in <paramref name="playerFolder"/>'s <c>editor/game.json</c>: found, or the problem with it
    /// (gone, no longer the game, another version: the screen says it), or nothing when none is remembered.
    /// </summary>
    public static GameFound Remembered(string playerFolder, string editorVersion, GameLibraryLoader? loader = null) =>
        RememberedGame.Read(playerFolder) is { } folder ? Find(folder, editorVersion, loader) : new GameFound(null, null);

    static string Full(string folder) => Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder)) is { Length: > 0 } full ? full : Path.GetFullPath(folder);
}

/// <summary>
/// The game's folder the editor remembers (Q16; G6), <c>editor/game.json</c>: <c>{ "version": 1, "folder": … }</c>,
/// written when the player chooses the game's folder (the "Where is Stat Fick?" screen, or Game folder… in the editor),
/// never for <c>--game</c> or <c>--repo</c>; read at each start without them.
/// </summary>
public static class RememberedGame
{
    public const string FileName = "game.json";

    /// <summary>The file in a player's game folder.</summary>
    public static string File(string playerFolder) => Path.Combine(EditorSettings.Folder(playerFolder), FileName);

    /// <summary>The folder remembered in <paramref name="playerFolder"/>, or null (none, or a file that can't be used, or a folder that isn't a full path).</summary>
    public static string? Read(string playerFolder) =>
        EditorSettings.Read(File(playerFolder)) is { } values && values["folder"] is JsonValue value && value.GetValueKind() == JsonValueKind.String
            && value.GetValue<string>() is var folder && Path.IsPathFullyQualified(folder) ? folder : null;

    /// <summary>Remembers <paramref name="folder"/> in <paramref name="playerFolder"/>.</summary>
    public static bool Write(string playerFolder, string folder) =>
        EditorSettings.Write(File(playerFolder), new JsonObject { ["folder"] = Path.GetFullPath(folder) });
}
