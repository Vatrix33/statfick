using System.Runtime.Loader;
using Statventure.Editor.Game;
using Statventure.Editor.Settings;
using Statventure.Editor.Shell;
using Statventure.Records;
using Statventure.Tests.Packs;

namespace Statventure.Tests.Editor.Game;

/// <summary>
/// The editor's lookup of the game (Q16, Q18, Q19; M12 step 6) on scratch game folders: what a game's folder holds, its
/// version against the editor's, its data library's kinds loaded once per run, the folder remembered in
/// <c>editor/game.json</c>, the dev setup's library built in the repo, and the words of each problem.
/// </summary>
public class GameLookupTests
{
    [Fact]
    public void AGamesFolderHoldsItsPacksAndItsDataLibraryOfTheEditorsVersion()
    {
        using var folder = new TempFolder();
        var game = GameTesting.MakeGame(folder["game"]);
        var (library, problem) = GameLookup.Check(game, EditorView.Version);
        Assert.Null(problem);
        Assert.Equal((folder["game/Statventure.Game.Data.dll"], "Statventure.Game.Data", "dev"), (library!.File, library.Program.Name, library.Version));
        // Its build id is the module's: the same for every copy of one build.
        Assert.Equal(GameLibrary.Read(GameTesting.BuiltLibrary).Build, library.Build);
        // The program beside it isn't looked at (build.sh's dev layout has Statventure.Sandbox): packs/ and the library are the game.
        Assert.False(File.Exists(folder["game/stat-fick"]));
        // A trailing separator, a relative path: the same folder.
        Assert.Equal(library, GameLookup.Check(game + Path.DirectorySeparatorChar, EditorView.Version).Library);
    }

    [Fact]
    public void AFolderWithoutThePacksOrTheLibraryIsntTheGame()
    {
        using var folder = new TempFolder();
        Assert.Equal(new GameProblem(GameProblemKind.Missing, folder["none"]), GameLookup.Check(folder["none"], "dev").Problem);
        GameTesting.MakeGame(folder["no-packs"], packs: false);
        Assert.Equal(new GameProblem(GameProblemKind.NotTheGame, folder["no-packs"], "its packs folder"), GameLookup.Check(folder["no-packs"], "dev").Problem);
        GameTesting.MakeGame(folder["no-library"], library: false);
        Assert.Equal(new GameProblem(GameProblemKind.NotTheGame, folder["no-library"], "Statventure.Game.Data.dll"), GameLookup.Check(folder["no-library"], "dev").Problem);
        Directory.CreateDirectory(folder["empty"]);
        Assert.Equal(new GameProblem(GameProblemKind.NotTheGame, folder["empty"], "its packs folder and Statventure.Game.Data.dll"), GameLookup.Check(folder["empty"], "dev").Problem);
        // A file named packs, a folder named as the library: neither counts.
        folder.Write("files/packs", "not a folder");
        Directory.CreateDirectory(folder["files/Statventure.Game.Data.dll"]);
        Assert.Equal(new GameProblem(GameProblemKind.NotTheGame, folder["files"], "its packs folder and Statventure.Game.Data.dll"), GameLookup.Check(folder["files"], "dev").Problem);
    }

    [Fact]
    public void ALibraryThatIsntTheGamesCantBeUsed()
    {
        using var folder = new TempFolder();
        GameTesting.MakeGame(folder["garbage"], library: false);
        folder.Write("garbage/Statventure.Game.Data.dll", "MZ but nothing more");
        Assert.Equal(new GameProblem(GameProblemKind.Unreadable, folder["garbage"], "it isn't a .NET assembly"), GameLookup.Check(folder["garbage"], "dev").Problem);
        GameTesting.MakeGame(folder["other"], library: false);
        GameTesting.FakeLibrary(folder["other/Statventure.Game.Data.dll"], "dev", name: "Something.Else");
        Assert.Equal(new GameProblem(GameProblemKind.Unreadable, folder["other"], "it is the assembly Something.Else, not Statventure.Game.Data"),
            GameLookup.Check(folder["other"], "dev").Problem);
        // A native library (not .NET) beside the program, named as the data library.
        GameTesting.MakeGame(folder["native"], library: false);
        File.WriteAllBytes(folder["native/Statventure.Game.Data.dll"], [0x7F, (byte)'E', (byte)'L', (byte)'F', 2, 1, 1, 0]);
        Assert.Equal("it isn't a .NET assembly", GameLookup.Check(folder["native"], "dev").Problem!.Detail);
    }

    [Fact]
    public void AGameOfAnotherVersionIsRefusedNamingBothAndNeverLoaded()
    {
        using var folder = new TempFolder();
        GameTesting.MakeGame(folder["release"], library: false);
        var file = GameTesting.FakeLibrary(folder["release/Statventure.Game.Data.dll"], "2026.11.8.1+0123abcd");
        // Q18: the dev editor refuses a release's game, naming both versions.
        var found = GameLookup.Find(folder["release"], "dev");
        Assert.Equal(new GameProblem(GameProblemKind.OtherVersion, folder["release"], Version: "2026.11.8.1"), found.Problem);
        Assert.Null(found.Game);
        Assert.Equal($"{folder["release"]} holds Stat Fick 2026.11.8.1, and this editor works only with Stat Fick dev", found.Problem!.Text("dev"));
        // A release's editor takes only its own version: exactly.
        Assert.Null(GameLookup.Check(folder["release"], "2026.11.8.1").Problem);
        Assert.Equal("2026.11.8.1", GameLookup.Check(folder["release"], "2026.11.8.2").Problem!.Version);
        Assert.Equal("2026.11.8.1", GameLookup.Check(folder["release"], "2026.11.8").Problem!.Version);
        // A dev game for a release's editor; a library without a version is a dev build.
        Assert.Equal("dev", GameLookup.Check(GameTesting.MakeGame(folder["dev"]), "2026.11.8.1").Problem!.Version);
        GameTesting.MakeGame(folder["none"], library: false);
        GameTesting.FakeLibrary(folder["none/Statventure.Game.Data.dll"], null);
        Assert.Equal("dev", GameLookup.Check(folder["none"], "2026.11.8.1").Problem!.Version);
        // The refused library was read, not loaded: no load context holds it.
        var build = GameLibrary.Read(file).Build;
        Assert.DoesNotContain(AssemblyLoadContext.All.SelectMany(c => c.Assemblies), a => a.ManifestModule.ModuleVersionId == build);
    }

    [Fact]
    public void DevBuildsMatchWhateverTheirCommits()
    {
        // Left to the step (Docs/Packaging.md, "Left to the steps"): "dev" against "dev", the commit not compared.
        using var folder = new TempFolder();
        GameTesting.MakeGame(folder["game"], library: false);
        GameTesting.FakeLibrary(folder["game/Statventure.Game.Data.dll"], "dev+ffffffffffffffffffffffffffffffffffffffff");
        var (library, problem) = GameLookup.Check(folder["game"], "dev");
        Assert.Null(problem);
        Assert.Equal(("dev", "ffffffffffffffffffffffffffffffffffffffff"), (library!.Version, library.Program.Commit));
    }

    [Fact]
    public void TheGamesKindsAreTheEnginesAndItsLibrarysLoadedInAContextOfItsOwn()
    {
        using var folder = new TempFolder();
        var found = GameLookup.Find(GameTesting.MakeGame(folder["game"]), EditorView.Version);
        Assert.Null(found.Problem);
        var game = found.Game!;
        Assert.Equal((folder["game/packs"], false, folder["game"], folder["game/Statventure.Game.Data.dll"]), (game.PacksFolder, game.DevSetup, game.Folder, game.Library));
        var kind = game.Types.Find(GameTesting.StandInKind)!;
        Assert.NotNull(game.Types.Find("audio.room"));
        Assert.Equal(Statventure.Packs.RecordTypes.Engine.ByName.Count + 1, game.Types.ByName.Count);
        Assert.Equal(Statventure.Packs.Importers.Engine.All.Select(i => i.Name), game.Importers.All.Select(i => i.Name));
        // Its own load context, its engine the editor's: the kind's marks are the engine's attributes.
        Assert.NotSame(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(kind.Assembly));
        var shape = RecordShape.Of(kind);
        Assert.Equal(("Stand-in item", "stand-in/items"), (shape.DisplayName, shape.Folder));
        Assert.Equal(["Title", "Price", "Rarity"], shape.Fields.Select(f => f.Name));
        // The run loaded that build once: another copy of it is the same library, the same kinds.
        Assert.Equal(GameLibrary.Read(GameTesting.BuiltLibrary).Build, GameLibraryLoader.Run.Loaded!.Build);
        var again = GameLookup.Find(GameTesting.MakeGame(folder["copy"]), EditorView.Version).Game!;
        Assert.Same(kind, again.Types.Find(GameTesting.StandInKind));
        Assert.Same(kind, GameTesting.StandInType());
    }

    [Fact]
    public void ARunUsesOneBuildOfTheLibraryAnotherOnlyAfterANewStart()
    {
        using var folder = new TempFolder();
        GameTesting.StandInType();
        GameTesting.MakeGame(folder["other-build"], library: false);
        var other = GameTesting.FakeLibrary(folder["other-build/Statventure.Game.Data.dll"], "dev+abc");
        var found = GameLookup.Find(folder["other-build"], EditorView.Version);
        Assert.Equal(new GameProblem(GameProblemKind.AnotherBuild, folder["other-build"], GameLibraryLoader.Run.Loaded!.File), found.Problem);
        Assert.Null(found.Game);
        Assert.Equal($"this editor has already loaded another build of Stat Fick's data library (from {GameLibraryLoader.Run.Loaded!.File})", found.Problem!.Text("dev"));
        Assert.DoesNotContain(AssemblyLoadContext.All.SelectMany(c => c.Assemblies), a => a.ManifestModule.ModuleVersionId == GameLibrary.Read(other).Build);
        // A loader of its own (a new start, as far as the library goes): the first build it is given, then that build only.
        var loader = new GameLibraryLoader();
        Assert.Null(loader.Loaded);
        var first = GameLookup.Find(folder["other-build"], EditorView.Version, loader);
        Assert.Null(first.Problem);
        Assert.Equal(folder["other-build/Statventure.Game.Data.dll"], loader.Loaded!.File);
        Assert.Null(first.Game!.Types.Find(GameTesting.StandInKind));
        Assert.Equal(GameProblemKind.AnotherBuild, GameLookup.Find(GameTesting.MakeGame(folder["stand-in"]), EditorView.Version, loader).Problem!.Kind);
        Assert.Throws<InvalidOperationException>(() => loader.Kinds(GameLibrary.Read(GameTesting.BuiltLibrary)));
    }

    [Fact]
    public void ALibraryWhoseKindsCantBeLoadedIsSaidAndLeftUnloaded()
    {
        using var folder = new TempFolder();
        GameTesting.MakeGame(folder["game"], library: false);
        GameTesting.FakeLibraryNeeding(folder["game/Statventure.Game.Data.dll"], "Statventure.Missing.Part");
        var loader = new GameLibraryLoader();
        var found = GameLookup.Find(folder["game"], EditorView.Version, loader);
        Assert.Equal(GameProblemKind.Unreadable, found.Problem!.Kind);
        Assert.StartsWith("its kinds can't be loaded: ", found.Problem.Detail);
        Assert.Contains("Statventure.Missing.Part", found.Problem.Detail);
        Assert.Null(loader.Loaded);
    }

    [Fact]
    public void ALibraryChangedBetweenItsReadingAndItsLoadingIsntUsed()
    {
        // Its build id read, then another file loaded (replaced meanwhile): refused, nothing kept.
        var loader = new GameLibraryLoader();
        var read = GameLibrary.Read(GameTesting.BuiltLibrary) with { Build = Guid.NewGuid() };
        var problem = Assert.Throws<GameLibraryException>(() => loader.Kinds(read));
        Assert.Equal("it changed while it was being read", problem.Message);
        Assert.Null(loader.Loaded);
    }

    [Fact]
    public void TheDevSetupTakesTheLibraryBuiltInTheRepo()
    {
        using var folder = new TempFolder();
        var engine = GameTesting.MakeRepo(folder["engine"]);
        var found = GameLookup.Repo(engine, EditorView.Version);
        Assert.Null(found.Problem);
        Assert.Equal((folder["engine/Assets/Packs"], true, null), (found.Game!.PacksFolder, found.Game.DevSetup, found.Game.Folder));
        Assert.Equal(GameLibrary.InRepo(engine), found.Game.Library);
        Assert.NotNull(found.Game.Types.Find(GameTesting.StandInKind));
        // Not built: said with where it should be and what builds it.
        File.Delete(GameLibrary.InRepo(engine));
        var problem = GameLookup.Repo(engine, EditorView.Version).Problem!;
        Assert.Equal(new GameProblem(GameProblemKind.NotBuilt, Path.GetDirectoryName(GameLibrary.InRepo(engine))!, GameLibrary.InRepo(engine)), problem);
        Assert.Equal($"the game's data library isn't built: {GameLibrary.InRepo(engine)} isn't there (Tools/Scripts/editor.sh builds it)", problem.Text("dev"));
        // Of another version (a release's editor on the repo's dev build).
        GameTesting.MakeRepo(engine);
        Assert.Equal(GameProblemKind.OtherVersion, GameLookup.Repo(engine, "2026.11.8.1").Problem!.Kind);
    }

    [Fact]
    public void TheEditorRemembersTheGamesFolderInItsSettings()
    {
        using var folder = new TempFolder();
        var player = folder["player"];
        // None remembered: nothing found and nothing to say (the screen asks).
        Assert.Equal(new GameFound(null, null), GameLookup.Remembered(player, "dev"));
        var game = GameTesting.MakeGame(folder["games/stat-fick-dev"]);
        Assert.True(RememberedGame.Write(player, game));
        Assert.Equal("{\n  \"version\": 1,\n  \"folder\": " + System.Text.Json.JsonSerializer.Serialize(game) + "\n}\n", File.ReadAllText(folder["player/editor/game.json"]));
        Assert.Equal(game, RememberedGame.Read(player));
        var found = GameLookup.Remembered(player, "dev");
        Assert.Equal((game, null), (found.Game!.Folder, found.Problem));
        // Gone: the screen says so. No longer the game: likewise. Another version: naming both.
        Directory.Move(game, folder["games/moved"]);
        Assert.Equal(new GameProblem(GameProblemKind.Missing, game), GameLookup.Remembered(player, "dev").Problem);
        Directory.CreateDirectory(game);
        Assert.Equal(GameProblemKind.NotTheGame, GameLookup.Remembered(player, "dev").Problem!.Kind);
        Directory.Delete(game);
        Directory.Move(folder["games/moved"], game);
        Assert.Equal(new GameProblem(GameProblemKind.OtherVersion, game, Version: "dev"), GameLookup.Remembered(player, "2026.11.8.1").Problem);
        // A file that can't be used remembers nothing: damaged, another version of the file, no folder, a relative one.
        foreach (var text in new[] { "{", "{\"version\": 2, \"folder\": \"/a\"}", "{\"version\": 1}", "{\"version\": 1, \"folder\": 3}", "{\"version\": 1, \"folder\": \"games/x\"}", "{\"version\": 1, \"folder\": \"\"}" })
        {
            File.WriteAllText(folder["player/editor/game.json"], text);
            Assert.Null(RememberedGame.Read(player));
            Assert.Equal(new GameFound(null, null), GameLookup.Remembered(player, "dev"));
        }
        Assert.Equal(Path.Combine(EditorSettings.Folder(player), "game.json"), RememberedGame.File(player));
    }

    [Fact]
    public void EachProblemHasItsWords()
    {
        // The screen's sentences (a remembered folder: "any more"), the browser's and the command line's.
        Assert.Equal("Stat Fick isn't in /games/sf any more.", new GameProblem(GameProblemKind.Missing, "/games/sf").Sentence("dev", remembered: true));
        Assert.Equal("/games/sf isn't there.", new GameProblem(GameProblemKind.Missing, "/games/sf").Sentence("dev"));
        Assert.Equal("/games/sf doesn't hold Stat Fick any more: its packs folder isn't in it.",
            new GameProblem(GameProblemKind.NotTheGame, "/games/sf", "its packs folder").Sentence("dev", remembered: true));
        Assert.Equal("/games/sf isn't Stat Fick's folder: its packs folder and Statventure.Game.Data.dll aren't in it.",
            new GameProblem(GameProblemKind.NotTheGame, "/games/sf", "its packs folder and Statventure.Game.Data.dll").Sentence("dev"));
        Assert.Equal("/games/sf isn't Stat Fick's folder: Statventure.Game.Data.dll isn't in it",
            new GameProblem(GameProblemKind.NotTheGame, "/games/sf", "Statventure.Game.Data.dll").Text("dev"));
        Assert.Equal("Stat Fick's data library in /games/sf can't be used: it isn't a .NET assembly.",
            new GameProblem(GameProblemKind.Unreadable, "/games/sf", "it isn't a .NET assembly").Sentence("dev", remembered: true));
        Assert.Equal("/games/sf holds Stat Fick 2026.11.8.1, and this editor works only with Stat Fick 2026.11.9.1.",
            new GameProblem(GameProblemKind.OtherVersion, "/games/sf", Version: "2026.11.8.1").Sentence("2026.11.9.1", remembered: true));
        Assert.Equal("This editor has already loaded another build of Stat Fick's data library (from /games/a/Statventure.Game.Data.dll).",
            new GameProblem(GameProblemKind.AnotherBuild, "/games/sf", "/games/a/Statventure.Game.Data.dll").Sentence("dev"));
    }
}
