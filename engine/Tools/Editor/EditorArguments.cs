using Statventure.Editor.Session;

namespace Statventure.Editor;

/// <summary>
/// The editor's command line (P5): <c>--repo &lt;engine folder&gt;</c> is the dev setup (<c>Tools/Scripts/editor.sh</c>
/// passes it: the game's packs are the repo's <c>Assets/Packs</c>, which it can save into); otherwise the packs are
/// <c>&lt;game folder&gt;/packs</c> (<c>--game</c>), by default the <c>packs/</c> folder beside the program, read-only.
/// <c>--player-folder</c> is the player's game folder (its mods, the editor's own settings, G6; by default this
/// machine's), so a check can run the editor on a folder of its own.
/// </summary>
public sealed record EditorArguments(GameData? Game, string? PlayerFolder, string? Problem)
{
    public const string Usage = "usage: Statventure.Editor [--repo <engine folder> | --game <game folder>] [--player-folder <folder>]";

    /// <summary>Reads <paramref name="args"/>; <paramref name="programFolder"/> is where the program is (its <c>packs/</c> by default).</summary>
    public static EditorArguments Parse(IReadOnlyList<string> args, string programFolder)
    {
        GameData? game = null;
        string? playerFolder = null;
        for (var i = 0; i < args.Count; i++)
        {
            var name = args[i];
            if (name is not ("--repo" or "--game" or "--player-folder"))
                return new(null, null, $"'{name}' isn't an option");
            if (i + 1 >= args.Count)
                return new(null, null, $"{name} needs a folder");
            var value = args[++i];
            if (name == "--player-folder")
            {
                if (playerFolder is not null)
                    return new(null, null, "--player-folder is given twice");
                playerFolder = Path.GetFullPath(value);
                continue;
            }
            if (game is not null)
                return new(null, null, "--repo and --game name the game's data once");
            game = name == "--repo" ? GameData.Repo(value) : GameData.Installed(value);
        }
        game ??= GameData.Installed(programFolder);
        return new(game, playerFolder, game.Problem());
    }
}
