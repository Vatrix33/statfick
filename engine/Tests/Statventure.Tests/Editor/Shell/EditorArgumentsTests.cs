using Statventure.Editor;
using Statventure.Editor.Session;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;

namespace Statventure.Tests.Editor.Shell;

/// <summary>The editor's command line (P5): the dev setup with --repo, an installed game's packs otherwise, read-only; the player's folder.</summary>
public class EditorArgumentsTests
{
    [Fact]
    public void RepoIsTheDevSetupOnTheReposPacks()
    {
        var parsed = EditorArguments.Parse(["--repo", EditorHarness.EngineFolder], "/nowhere");
        Assert.Null(parsed.Problem);
        Assert.Equal(new GameData(TestPaths.Packs, true), parsed.Game);
        Assert.Equal(Path.Combine(TestPaths.Packs, "ui.pak"), parsed.Game!.UiPack);
        Assert.Null(parsed.PlayerFolder);
    }

    [Fact]
    public void OtherwiseTheGamesPacksReadOnlyBesideTheProgramOrWhereGameSays()
    {
        using var folder = new TempFolder();
        folder.Write("program/packs/ui.pak", [1]);
        folder.Write("game/packs/ui.pak", [1]);
        var beside = EditorArguments.Parse([], folder["program"]);
        Assert.Equal(new GameData(folder["program/packs"], false), beside.Game);
        Assert.Null(beside.Problem);
        var game = EditorArguments.Parse(["--player-folder", folder["player"], "--game", folder["game"]], folder["program"]);
        Assert.Equal(new GameData(folder["game/packs"], false), game.Game);
        Assert.Equal(folder["player"], game.PlayerFolder);
        Assert.Null(game.Problem);
        // A relative player folder is taken from the working folder.
        Assert.Equal(Path.GetFullPath("player"), EditorArguments.Parse(["--player-folder", "player"], folder["program"]).PlayerFolder);
    }

    public static TheoryData<string[], string> Wrong => new()
    {
        { ["--repo"], "--repo needs a folder" },
        { ["--player-folder"], "--player-folder needs a folder" },
        { ["--frob", "x"], "'--frob' isn't an option" },
        { ["x"], "'x' isn't an option" },
        { ["--repo", "a", "--game", "b"], "--repo and --game name the game's data once" },
        { ["--game", "a", "--game", "b"], "--repo and --game name the game's data once" },
        { ["--player-folder", "a", "--player-folder", "b"], "--player-folder is given twice" },
    };

    [Theory]
    [MemberData(nameof(Wrong))]
    public void AWrongCommandLineIsAProblem(string[] args, string problem)
    {
        var parsed = EditorArguments.Parse(args, EditorHarness.EngineFolder);
        Assert.Equal(problem, parsed.Problem);
        Assert.Null(parsed.Game);
    }

    [Fact]
    public void WithoutUiPakTheEditorCantStart()
    {
        using var folder = new TempFolder();
        Directory.CreateDirectory(folder["game/packs"]);
        var parsed = EditorArguments.Parse(["--game", folder["game"]], "/nowhere");
        Assert.Equal($"the game's packs aren't in {folder["game/packs"]}: ui.pak isn't there", parsed.Problem);
        Assert.Equal($"the game's packs aren't in {Path.GetFullPath("/nowhere/packs")}: ui.pak isn't there", EditorArguments.Parse([], "/nowhere").Problem);
    }
}
