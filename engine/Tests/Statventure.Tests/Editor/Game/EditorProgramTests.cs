using Statventure.Editor;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Tests.Packs;

namespace Statventure.Tests.Editor.Game;

/// <summary>
/// The editor's start (M12 step 6; Q6, Q20): the command line read and the game it names found before any window; a
/// problem told on the terminal with the usage (exit code 2), on Windows also in the plain message window (step 5's,
/// a seam until it is merged); otherwise the editor run with its arguments and the game (none: it looks itself).
/// </summary>
public class EditorProgramTests
{
    sealed record Shown(string Title, string Heading, string Text);

    static (int Code, string Terminal, List<Shown> Shown, List<(EditorArguments, GameData?)> Runs) Start(bool windows, params string[] args)
    {
        var terminal = new StringWriter();
        var shown = new List<Shown>();
        var runs = new List<(EditorArguments, GameData?)>();
        var code = EditorProgram.Run(args, windows, terminal, (title, heading, text) =>
        {
            shown.Add(new Shown(title, heading, text));
            return true;
        }, (arguments, game) => runs.Add((arguments, game)));
        return (code, terminal.ToString().Replace("\r\n", "\n"), shown, runs);
    }

    [Fact]
    public void AWrongCommandLineIsToldWithTheUsageAndEndsWithTwo()
    {
        var linux = Start(windows: false, "--frob");
        Assert.Equal((2, "Statventure Editor: '--frob' isn't an option\n" + EditorArguments.UsageFor(false) + "\n"), (linux.Code, linux.Terminal));
        Assert.Empty(linux.Shown);
        Assert.Empty(linux.Runs);
        // Windows: the editor has no console there (Q6), so the message window tells it too.
        var windows = Start(windows: true, "--capture", "a.png", "--frames", "0");
        Assert.Equal(2, windows.Code);
        Assert.Equal("Statventure Editor: --frames needs a whole number from 1, not '0'\n" + EditorArguments.UsageFor(true) + "\n", windows.Terminal);
        Assert.Equal([new Shown("Statventure Editor dev", "Statventure Editor can't start",
            "--frames needs a whole number from 1, not '0'.\n\n" + EditorArguments.UsageFor(true))], windows.Shown);
        Assert.Empty(windows.Runs);
        var capital = Start(windows: true, "--repo");
        Assert.Equal("--repo needs a folder.\n\n" + EditorArguments.UsageFor(true), capital.Shown.Single().Text);
        Assert.Equal("Statventure Editor can't start", EditorProgram.CantStart);
    }

    [Fact]
    public void AGameTheCommandLineNamesThatCantBeUsedStopsItBeforeAnyWindow()
    {
        using var folder = new TempFolder();
        var missing = Start(windows: false, "--game", folder["nowhere"]);
        Assert.Equal((2, $"Statventure Editor: {folder["nowhere"]} isn't there\n{EditorArguments.UsageFor(false)}\n"), (missing.Code, missing.Terminal));
        GameTesting.MakeGame(folder["release"], library: false);
        GameTesting.FakeLibrary(folder["release/Statventure.Game.Data.dll"], "2026.11.8.1+0123");
        var other = Start(windows: true, "--game", folder["release"]);
        Assert.Equal(2, other.Code);
        Assert.StartsWith($"Statventure Editor: {folder["release"]} holds Stat Fick 2026.11.8.1, and this editor works only with Stat Fick dev\n", other.Terminal);
        Assert.Equal($"{folder["release"]} holds Stat Fick 2026.11.8.1, and this editor works only with Stat Fick dev.\n\n{EditorArguments.UsageFor(true)}", other.Shown.Single().Text);
        Directory.CreateDirectory(folder["engine/Assets/Packs"]);
        var notBuilt = Start(windows: true, "--repo", folder["engine"]);
        Assert.Equal(2, notBuilt.Code);
        Assert.StartsWith("Statventure Editor: the game's data library isn't built", notBuilt.Terminal);
        // The message window's text a sentence: its capital first.
        Assert.StartsWith("The game's data library isn't built: ", notBuilt.Shown.Single().Text);
        Assert.Empty(missing.Runs.Concat(other.Runs).Concat(notBuilt.Runs));
    }

    [Fact]
    public void OtherwiseTheEditorRunsWithItsArgumentsAndTheGameNamed()
    {
        using var folder = new TempFolder();
        var repo = Start(windows: false, "--repo", EditorHarness.EngineFolder, "--player-folder", folder["player"], "--capture", folder["shot.png"], "--frames", "3");
        Assert.Equal((0, ""), (repo.Code, repo.Terminal));
        var (arguments, game) = repo.Runs.Single();
        Assert.Equal((folder["player"], folder["shot.png"], 3), (arguments.PlayerFolder, arguments.Capture, arguments.Frames));
        Assert.True(game!.DevSetup);
        Assert.NotNull(game.Types.Find(GameTesting.StandInKind));
        var installed = Start(windows: false, "--game", GameTesting.MakeGame(folder["game"]));
        Assert.Equal(folder["game"], installed.Runs.Single().Item2!.Folder);
        // Neither: the editor looks for the game itself (the folder it remembers, or the screen).
        var neither = Start(windows: true);
        Assert.Equal((0, "", 0), (neither.Code, neither.Terminal, neither.Shown.Count));
        Assert.Null(neither.Runs.Single().Item2);
    }

    [Fact]
    public void TheMessageWindowIsTheEnginesPlainOne()
    {
        // Q6 (M12 step 5's window): the program's start gives the engine's message window, in the crash window's look.
        var show = EditorProgram.ShowMessage.Method;
        Assert.Equal(typeof(Statventure.Core.MessageWindow).GetMethod(nameof(Statventure.Core.MessageWindow.Show), [typeof(string), typeof(string), typeof(string)]), show);
    }
}
