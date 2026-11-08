using Statventure.Core;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;

namespace Statventure.Editor;

/// <summary>
/// The editor's start (M12 step 6; <c>Program.cs</c> calls <see cref="Start"/>): the command line read
/// (<see cref="EditorArguments"/>), the game it names found (<c>--repo</c>, <c>--game</c>), then the editor run. A wrong command line, or a game it names that the editor can't
/// work on, stops it before any window with exit code 2: the problem and the usage on the terminal, and on Windows,
/// where the editor is a program without a console (Q6), also in a plain message window in the crash window's look
/// (<see cref="ShowMessage"/>).
/// </summary>
public static class EditorProgram
{
    /// <summary>The heading of the message window for a command line it can't start with.</summary>
    public const string CantStart = "Statventure Editor can't start";

    /// <summary>
    /// Shows a plain message window (its title, heading and text) until it is closed, giving false when no window
    /// could be shown: the engine's message window in the crash window's look (<see cref="MessageWindow.Show(string, string, string)"/>,
    /// M12 step 5). The terminal has had the message first in any case.
    /// </summary>
    internal static Func<string, string, string, bool> ShowMessage { get; } = MessageWindow.Show;

    /// <summary>The program's start: its exit code (0, or 2 for a command line it can't start with).</summary>
    public static int Start(string[] args) => Run(args, OperatingSystem.IsWindows(), Console.Error, ShowMessage, (arguments, game) =>
    {
        using var editor = new EditorApplication(game, arguments.PlayerFolder, arguments.Capture, arguments.Frames);
        editor.Run();
    });

    /// <summary>
    /// Reads <paramref name="args"/> and runs <paramref name="run"/> with them and the game they name (null: the
    /// editor looks for it), or tells the problem (<see cref="Tell"/>) and gives 2.
    /// </summary>
    internal static int Run(IReadOnlyList<string> args, bool windows, TextWriter terminal, Func<string, string, string, bool>? showMessage,
        Action<EditorArguments, GameData?> run)
    {
        var arguments = EditorArguments.Parse(args);
        var problem = arguments.Problem;
        GameData? game = null;
        if (problem is null)
        {
            var found = arguments.FindGame(EditorView.Version);
            game = found.Game;
            problem = found.Problem?.Text(EditorView.Version);
        }
        if (problem is not null)
        {
            Tell(problem, windows, terminal, showMessage);
            return 2;
        }
        run(arguments, game);
        return 0;
    }

    /// <summary>A command line the editor can't start with: on the terminal, and on Windows in the message window too (<paramref name="showMessage"/>, Q6).</summary>
    internal static void Tell(string problem, bool windows, TextWriter terminal, Func<string, string, string, bool>? showMessage)
    {
        terminal.WriteLine($"Statventure Editor: {problem}\n{EditorArguments.UsageFor(windows)}");
        if (windows)
            showMessage?.Invoke(EditorView.TitleOf(EditorView.Version, null, unsaved: false), CantStart,
                $"{char.ToUpperInvariant(problem[0])}{problem[1..]}.\n\n{EditorArguments.UsageFor(windows: true)}");
    }
}
