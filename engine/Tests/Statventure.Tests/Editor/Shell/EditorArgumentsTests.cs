using Statventure.Editor;
using Statventure.Editor.Game;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Tests.Editor.Game;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;

namespace Statventure.Tests.Editor.Shell;

/// <summary>
/// The editor's command line (P5; M12 step 6): the dev setup with --repo (the repo's packs, writable, the kinds of the
/// data library built in the repo), an installed game's folder with --game (read-only), neither: the editor looks for
/// the game itself; the player's folder; --capture and --frames (Q20); the usage naming the program as M12-2 does.
/// </summary>
public class EditorArgumentsTests
{
    [Fact]
    public void RepoIsTheDevSetupOnTheReposPacksWithTheLibraryBuiltThere()
    {
        var parsed = EditorArguments.Parse(["--repo", EditorHarness.EngineFolder]);
        Assert.Equal((EditorHarness.EngineFolder, null, null, null, 30, null), (parsed.Repo, parsed.GameFolder, parsed.PlayerFolder, parsed.Capture, parsed.Frames, parsed.Problem));
        var found = parsed.FindGame(EditorView.Version);
        Assert.Null(found.Problem);
        var game = found.Game!;
        Assert.Equal((TestPaths.Packs, true, null, GameTesting.BuiltLibrary), (game.PacksFolder, game.DevSetup, game.Folder, game.Library));
        // The library's build in the repo: Samples/Statventure.Game.Data/bin/<the editor's configuration>/net10.0.
        Assert.Equal(Path.Combine(EditorHarness.EngineFolder, "Samples", "Statventure.Game.Data", "bin", GameLibrary.Configuration, "net10.0", "Statventure.Game.Data.dll"), GameTesting.BuiltLibrary);
        Assert.NotNull(game.Types.Find(GameTesting.StandInKind));
        Assert.NotNull(game.Types.Find("audio.room"));
    }

    [Fact]
    public void GameNamesAnInstalledGamesFolderReadOnly()
    {
        using var folder = new TempFolder();
        GameTesting.MakeGame(folder["game"]);
        var parsed = EditorArguments.Parse(["--player-folder", folder["player"], "--game", folder["game"]]);
        Assert.Equal((null, folder["game"], folder["player"], null), (parsed.Repo, parsed.GameFolder, parsed.PlayerFolder, parsed.Problem));
        var game = parsed.FindGame(EditorView.Version).Game!;
        Assert.Equal((folder["game/packs"], false, folder["game"], folder["game/Statventure.Game.Data.dll"]), (game.PacksFolder, game.DevSetup, game.Folder, game.Library));
        Assert.NotNull(game.Types.Find(GameTesting.StandInKind));
        // A relative folder is taken from the working folder.
        Assert.Equal(Path.GetFullPath("player"), EditorArguments.Parse(["--player-folder", "player"]).PlayerFolder);
        Assert.Equal(Path.GetFullPath("game"), EditorArguments.Parse(["--game", "game"]).GameFolder);
    }

    [Fact]
    public void NeitherLeavesTheGameToTheEditor()
    {
        // No packs/ beside the program any more (Q16, M12-1): the editor takes the folder it remembers, or asks.
        var parsed = EditorArguments.Parse([]);
        Assert.Equal(new EditorArguments(null, null, null, null, 30, null), parsed);
        Assert.Equal(new GameFound(null, null), parsed.FindGame(EditorView.Version));
    }

    [Fact]
    public void CaptureDrawsFramesThenWritesTheLast()
    {
        var parsed = EditorArguments.Parse(["--capture", "shot.png", "--frames", "5"]);
        Assert.Equal((Path.GetFullPath("shot.png"), 5, null), (parsed.Capture, parsed.Frames, parsed.Problem));
        Assert.Equal((Path.GetFullPath("a.png"), EditorArguments.DefaultFrames), (EditorArguments.Parse(["--capture", "a.png"]).Capture, EditorArguments.Parse(["--capture", "a.png"]).Frames));
        Assert.Equal(30, EditorArguments.DefaultFrames);
    }

    public static TheoryData<string[], string> Wrong => new()
    {
        { ["--repo"], "--repo needs a folder" },
        { ["--game"], "--game needs a folder" },
        { ["--player-folder"], "--player-folder needs a folder" },
        { ["--capture"], "--capture needs a file" },
        { ["--capture", "a.png", "--frames"], "--frames needs a number" },
        { ["--frob", "x"], "'--frob' isn't an option" },
        { ["x"], "'x' isn't an option" },
        { ["--repo", "a", "--game", "b"], "--repo and --game name the game's data once" },
        { ["--game", "a", "--game", "b"], "--repo and --game name the game's data once" },
        { ["--repo", "a", "--repo", "b"], "--repo and --game name the game's data once" },
        { ["--player-folder", "a", "--player-folder", "b"], "--player-folder is given twice" },
        { ["--capture", "a.png", "--capture", "b.png"], "--capture is given twice" },
        { ["--capture", "a.png", "--frames", "2", "--frames", "3"], "--frames is given twice" },
        { ["--capture", "a.png", "--frames", "0"], "--frames needs a whole number from 1, not '0'" },
        { ["--capture", "a.png", "--frames", "-3"], "--frames needs a whole number from 1, not '-3'" },
        { ["--capture", "a.png", "--frames", "+3"], "--frames needs a whole number from 1, not '+3'" },
        { ["--capture", "a.png", "--frames", "2.5"], "--frames needs a whole number from 1, not '2.5'" },
        { ["--capture", "a.png", "--frames", "many"], "--frames needs a whole number from 1, not 'many'" },
        { ["--frames", "4"], "--frames goes with --capture" },
    };

    [Theory]
    [MemberData(nameof(Wrong))]
    public void AWrongCommandLineIsAProblem(string[] args, string problem)
    {
        var parsed = EditorArguments.Parse(args);
        Assert.Equal(new EditorArguments(null, null, null, null, 30, problem), parsed);
    }

    [Fact]
    public void TheUsageNamesTheProgramAsItIsOnEachSystem()
    {
        // M12-2's names (Docs/Packaging.md, "The executables' names"), the Windows one quoted for its space.
        Assert.Equal("usage: statventure-editor [--repo <engine folder> | --game <game folder>] [--player-folder <folder>] [--capture <file.png> [--frames <n>]]",
            EditorArguments.UsageFor(windows: false));
        Assert.Equal("usage: \"Statventure Editor.exe\" [--repo <engine folder> | --game <game folder>] [--player-folder <folder>] [--capture <file.png> [--frames <n>]]",
            EditorArguments.UsageFor(windows: true));
        Assert.Equal(EditorArguments.UsageFor(OperatingSystem.IsWindows()), EditorArguments.Usage);
    }
}
