namespace Statventure.Game.Tests;

/// <summary>The game's command line (GM1-P12): the checks' options, no usage text, nothing that stops a start.</summary>
public class GameOptionsTests
{
    [Fact]
    public void NoArgumentsIsAPlayersStart()
    {
        var options = GameOptions.Parse([]);
        Assert.Equal((null, 30, null, false, null, CrashOnPurpose.None), (options.Capture, options.Frames, options.CaptureSplash, options.About, options.Open, options.Crash));
        Assert.Empty(options.Problems);
        Assert.Null(options.OpenCommand);
        Assert.Empty(options.Run);
    }

    [Fact]
    public void EachOptionIsRead()
    {
        var options = GameOptions.Parse(["--capture", "shot.png", "--frames", "12", "--capture-splash", "splash.png", "--about", "--open", "settings", "--crash-thread"]);
        Assert.Equal(Path.GetFullPath("shot.png"), options.Capture);
        Assert.Equal(12, options.Frames);
        Assert.Equal(Path.GetFullPath("splash.png"), options.CaptureSplash);
        Assert.True(options.About);
        Assert.Equal("settings", options.Open);
        Assert.Equal(CrashOnPurpose.OtherThread, options.Crash);
        Assert.Empty(options.Problems);
        Assert.Equal(CrashOnPurpose.MainThread, GameOptions.Parse(["--crash"]).Crash);
        // A full path stays as it is.
        Assert.Equal("/tmp/a.png", GameOptions.Parse(["--capture", "/tmp/a.png"]).Capture);
    }

    [Theory]
    [InlineData("start", "start-screen")]
    [InlineData("settings", "open-settings")]
    [InlineData("slots", "open-slots")]
    [InlineData("menu", "open-menu")]
    public void OpenRunsTheCommandThatOpensTheScreen(string screen, string command) =>
        Assert.Equal(command, GameOptions.Parse(["--open", screen]).OpenCommand);

    [Fact]
    public void RunNamesCommandsInTheOrderGivenEachWithWhatFollowsItsFirstEqualsSign()
    {
        // GM1-P20: --run <command>[=<argument>], as often as wanted.
        var options = GameOptions.Parse(["--run", "continue", "--about", "--run", "load-character=/tmp/a=b.json", "--run", "open-slot=", "--run", "continue"]);
        Assert.Equal(
            [new CommandRun("continue", null), new CommandRun("load-character", "/tmp/a=b.json"), new CommandRun("open-slot", ""), new CommandRun("continue", null)],
            options.Run);
        Assert.True(options.About);
        Assert.Empty(options.Problems);
        // The argument as given (not made a full path: a command's argument needn't be a path).
        Assert.Equal(new CommandRun("load-character", "a.json"), GameOptions.Parse(["--run", "load-character=a.json"]).Run.Single());
    }

    [Fact]
    public void AWrongCommandLineIsSaidAndIgnored()
    {
        var run = GameOptions.Parse(["--run", "=x.json", "--run", "continue", "--run"]);
        Assert.Equal(["--run needs a command, not '=x.json'", "--run needs a value"], run.Problems);
        Assert.Equal([new CommandRun("continue", null)], run.Run);
        var options = GameOptions.Parse(["--frames", "0", "--frames", "x2", "--frames", "1234567890", "--nope", "stray", "--open", "--about", "--capture"]);
        Assert.Equal(
            [
                "--frames needs a whole number from 1, not '0'",
                "--frames needs a whole number from 1, not 'x2'",
                "--frames needs a whole number from 1, not '1234567890'",
                "'--nope' isn't an option",
                "'stray' isn't an option",
                "--open needs a value",
                "--capture needs a value",
            ],
            options.Problems);
        // What was right is kept: --about after the --open that had no value.
        Assert.Equal((30, null, true, null), (options.Frames, options.Open, options.About, options.Capture));
    }
}
