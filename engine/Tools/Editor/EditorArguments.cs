using System.Globalization;
using Statventure.Editor.Game;

namespace Statventure.Editor;

/// <summary>
/// The editor's command line (P5; M12 step 6). <c>--repo &lt;engine folder&gt;</c> is the dev setup
/// (<c>Tools/Scripts/editor.sh</c> passes it: the game's packs are the repo's <c>Assets/Packs</c>, which it can save
/// into, and its kinds those of the data library built in the repo); <c>--game &lt;game folder&gt;</c> names an installed
/// game's folder (read-only); without either the editor takes the game's folder it remembers, or asks for it ("Where is
/// Stat Fick?", Q16). <c>--player-folder</c> is the player's game folder (its mods, the editor's own settings, G6; by
/// default this machine's), so a check can run the editor on a folder of its own. <c>--capture &lt;file.png&gt;</c> draws
/// <c>--frames</c> frames (30 unless given), writes the last into the file and quits, as the sandbox's (Q20).
/// </summary>
public sealed record EditorArguments(string? Repo, string? GameFolder, string? PlayerFolder, string? Capture, int Frames, string? Problem)
{
    /// <summary>The frames drawn before a capture unless <c>--frames</c> says (the sandbox's).</summary>
    public const int DefaultFrames = 30;

    /// <summary>The usage line for this system's program name (<see cref="UsageFor"/>).</summary>
    public static string Usage => UsageFor(OperatingSystem.IsWindows());

    /// <summary>The usage line naming the program as it is on Windows (<c>"Statventure Editor.exe"</c>) or Linux (<c>statventure-editor</c>; M12-2).</summary>
    public static string UsageFor(bool windows) =>
        $"usage: {(windows ? "\"Statventure Editor.exe\"" : "statventure-editor")} [--repo <engine folder> | --game <game folder>] [--player-folder <folder>] [--capture <file.png> [--frames <n>]]";

    /// <summary>Reads <paramref name="args"/>: the folders made full paths, or the problem with them.</summary>
    public static EditorArguments Parse(IReadOnlyList<string> args)
    {
        string? repo = null, game = null, player = null, capture = null;
        int? frames = null;
        static EditorArguments Wrong(string problem) => new(null, null, null, null, DefaultFrames, problem);
        for (var i = 0; i < args.Count; i++)
        {
            var name = args[i];
            if (name is not ("--repo" or "--game" or "--player-folder" or "--capture" or "--frames"))
                return Wrong($"'{name}' isn't an option");
            if (i + 1 >= args.Count)
                return Wrong(name switch { "--capture" => "--capture needs a file", "--frames" => "--frames needs a number", _ => $"{name} needs a folder" });
            var value = args[++i];
            switch (name)
            {
                case "--player-folder":
                    if (player is not null)
                        return Wrong("--player-folder is given twice");
                    player = Path.GetFullPath(value);
                    break;
                case "--capture":
                    if (capture is not null)
                        return Wrong("--capture is given twice");
                    capture = Path.GetFullPath(value);
                    break;
                case "--frames":
                    if (frames is not null)
                        return Wrong("--frames is given twice");
                    if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var n) || n < 1)
                        return Wrong($"--frames needs a whole number from 1, not '{value}'");
                    frames = n;
                    break;
                default:
                    if (repo is not null || game is not null)
                        return Wrong("--repo and --game name the game's data once");
                    if (name == "--repo")
                        repo = Path.GetFullPath(value);
                    else
                        game = Path.GetFullPath(value);
                    break;
            }
        }
        if (frames is not null && capture is null)
            return Wrong("--frames goes with --capture");
        return new(repo, game, player, capture, frames ?? DefaultFrames, null);
    }

    /// <summary>
    /// The game the command line names, for the editor of <paramref name="editorVersion"/>: the dev setup's
    /// (<c>--repo</c>) or the folder <c>--game</c> names, checked and its kinds loaded; neither given: no game and no
    /// problem (the editor looks for it itself).
    /// </summary>
    public GameFound FindGame(string editorVersion) =>
        Repo is { } repo ? GameLookup.Repo(repo, editorVersion)
        : GameFolder is { } folder ? GameLookup.Find(folder, editorVersion)
        : new GameFound(null, null);
}
