using Statventure.Editor.Browser;
using Statventure.Editor.Game;
using Statventure.Editor.Session;
using Statventure.Editor.Settings;
using Statventure.Editor.Shell;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.Tests.Storage;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor.Game;

/// <summary>
/// "Where is Stat Fick?" (Q16; M12 step 6): the editor's screen while it knows no game, in the editor's look and the
/// engine's Lato, its file browser picking a folder; the game chosen remembered and given to the program, a folder it
/// can't work on said under the listing. Driven as the user drives it, in the harness in the view's place.
/// </summary>
public class GameScreenTests
{
    /// <summary>A scratch tree: <c>player/</c>, <c>games/</c> holding a game (<c>stat-fick-dev</c>), a release's game, and a folder that isn't one.</summary>
    sealed class Scratch : IDisposable
    {
        public readonly StorageFolder Folder = new();

        public Scratch()
        {
            GameTesting.MakeGame(Folder["games/stat-fick-dev"]);
            GameTesting.MakeGame(Folder["games/stat-fick-2026.11.8.1"], library: false);
            GameTesting.FakeLibrary(Folder["games/stat-fick-2026.11.8.1/Statventure.Game.Data.dll"], "2026.11.8.1+0123");
            Folder.Write("games/notes/readme.txt", [1]);
            Directory.CreateDirectory(Folder["player"]);
        }

        public string Player => Folder["player"];
        public string Games => Folder["games"];
        public string Game => Folder["games/stat-fick-dev"];

        /// <summary>The screen in a harness (its own view underneath, unused), the recent places naming <c>games/</c> when asked.</summary>
        public (EditorHarness Editor, GameScreen Screen) Show(GameProblem? problem = null, bool recent = true, bool windows = false)
        {
            if (recent)
                RecentPlacesFile.Write(Player, new RecentPlaces([Games]));
            var editor = new EditorHarness(1000, 640);
            var screen = new GameScreen(Player, problem, "dev", windows, s => s.TimeZone = TimeZoneInfo.Utc);
            editor.ShowScreen(screen);
            return (editor, screen);
        }

        public void Dispose() => Folder.Dispose();
    }

    static int Row(GameScreen screen, string name) => screen.Browser.Entries.ToList().FindIndex(e => e.Name == name);

    static UiElement RowElement(GameScreen screen, string name) => screen.Browser.Listing.ElementChildren.ElementAt(Row(screen, name));

    [Fact]
    public void TheScreenAsksForTheGamesFolderInTheEditorsLook()
    {
        using var scratch = new Scratch();
        var (editor, screen) = scratch.Show();
        using var _ = editor;
        Assert.Equal("Where is Stat Fick?", editor.Q(".game-page h1").TextContent);
        Assert.Equal("Statventure Editor makes mods for Stat Fick and works on the game's data, so it needs the game: choose the folder Stat Fick was unpacked into, the one holding stat-fick and its packs folder.",
            editor.Q(".game-lead").TextContent);
        Assert.Equal("This is Statventure Editor dev, which works with Stat Fick dev.", editor.Q(".game-version").TextContent);
        // First start: nothing to say about a remembered folder.
        Assert.Null(editor.Document.QuerySelector(".game-problem"));
        Assert.Equal("", screen.Problem);
        // The browser in the page, picking a folder: Use this folder, no Cancel.
        Assert.Equal(["Use this folder"], editor.Q(".game-buttons").ElementChildren.Select(b => b.TextContent));
        Assert.True(screen.Browser.Request.Folder);
        Assert.Same(screen.Browser.Listing, editor.Document.FocusedElement);
        // Its places: the mods folder, Documents, home, the roots, the recent places; no game packs (no game yet).
        var places = screen.Browser.PlaceItems;
        Assert.Equal(("Mods", Path.Combine(scratch.Player, "mods")), (places[0].Label, places[0].Folder));
        Assert.DoesNotContain(places, p => p.Kind is null);
        Assert.Equal(["Mods", "Documents", "Home", "/", "games"], places.Select(p => p.Label));
        // It starts in the most recent place, its files listed too (the game's folder recognised by them).
        Assert.Equal(scratch.Games, screen.Browser.Folder);
        Assert.Equal(["notes", "stat-fick-2026.11.8.1", "stat-fick-dev"], screen.Browser.Entries.Select(e => e.Name));
        // The editor's stylesheets and the engine's Lato (Q17): it draws before any game is known.
        Assert.Equal(EditorView.Stylesheets.Select(s => s.Name), editor.Document.Styles.Sheets.Skip(1).Select(s => s.Name));
        Assert.Equal(Statventure.UI.EngineFonts.Bold.ToArray(), editor.Document.Files!.ReadAllBytes("ui/fonts/lato-bold.ttf"));
        // Windows names its program as it is there (M12-2).
        Assert.EndsWith("the one holding Stat Fick.exe and its packs folder.", GameScreen.LeadLine(windows: true));
        using var windows = new GameScreen(scratch.Player, null, "dev", windows: true);
        Assert.Equal(GameScreen.LeadLine(windows: true), windows.Document.QuerySelector(".game-lead")!.TextContent);
    }

    [Fact]
    public void WithoutARecentPlaceItStartsAtHome()
    {
        using var scratch = new Scratch();
        // Home, not the mods folder (there): the game isn't a mod.
        Directory.CreateDirectory(scratch.Folder["player/mods"]);
        var (editor, screen) = scratch.Show(recent: false);
        using var _ = editor;
        Assert.Equal(screen.Browser.PlaceItems.Single(p => p.Kind == BrowserPlaceKind.Home).Folder, screen.Browser.Folder);
    }

    [Fact]
    public void ARememberedFolderThatCantBeUsedIsSaidAndTheBrowserStartsBesideIt()
    {
        using var scratch = new Scratch();
        var gone = new GameProblem(GameProblemKind.Missing, Path.Combine(scratch.Games, "stat-fick-2026.11.1.1"));
        var (editor, screen) = scratch.Show(gone, recent: false);
        using var _ = editor;
        Assert.Equal($"Stat Fick isn't in {gone.Folder} any more.", editor.Q(".game-problem").TextContent);
        Assert.Equal(screen.Problem, editor.Q(".game-problem").TextContent);
        Assert.Equal(scratch.Games, screen.Browser.Folder);
        // Another version, naming both.
        var other = new GameProblem(GameProblemKind.OtherVersion, Path.Combine(scratch.Games, "stat-fick-2026.11.8.1"), Version: "2026.11.8.1");
        using var again = new GameScreen(scratch.Player, other, "dev");
        Assert.Equal($"{other.Folder} holds Stat Fick 2026.11.8.1, and this editor works only with Stat Fick dev.", again.Problem);
        // A remembered folder whose place is gone too: the most recent place, else home.
        using var nowhere = new GameScreen(scratch.Player, new GameProblem(GameProblemKind.Missing, "/no/where/at/all"), "dev");
        Assert.Equal(nowhere.Browser.PlaceItems.Single(p => p.Kind == BrowserPlaceKind.Home).Folder, nowhere.Browser.Folder);
    }

    [Fact]
    public void ChoosingTheGamesFolderGivesTheGameAndRemembersIt()
    {
        using var scratch = new Scratch();
        var (editor, screen) = scratch.Show(recent: false);
        using var _ = editor;
        screen.Browser.GoTo(scratch.Games);
        var chosen = new List<GameData>();
        screen.Chosen += chosen.Add;
        // A press selects the game's folder; Use this folder picks the folder selected (not going into it).
        editor.Click(RowElement(screen, "stat-fick-dev"));
        editor.Click(editor.Q(".game-buttons button"));
        var game = Assert.Single(chosen);
        Assert.Same(game, screen.Game);
        Assert.Equal((scratch.Game, Path.Combine(scratch.Game, "packs"), false), (game.Folder, game.PacksFolder, game.DevSetup));
        Assert.NotNull(game.Types.Find(GameTesting.StandInKind));
        Assert.Equal(scratch.Game, RememberedGame.Read(scratch.Player));
        // Its folder's place, the games folder, is the most recent place.
        Assert.Equal(scratch.Games, RecentPlacesFile.Read(scratch.Player).Folders[0]);
    }

    [Fact]
    public void AFolderTheEditorCantWorkOnIsSaidUnderTheListing()
    {
        using var scratch = new Scratch();
        var (editor, screen) = scratch.Show();
        using var _ = editor;
        var chosen = 0;
        screen.Chosen += _ => chosen++;
        editor.Click(RowElement(screen, "notes"));
        editor.Click(editor.Q(".game-buttons button"));
        Assert.Equal($"{Path.Combine(scratch.Games, "notes")} isn't Stat Fick's folder: its packs folder and Statventure.Game.Data.dll aren't in it.", screen.Browser.Message);
        editor.Click(RowElement(screen, "stat-fick-2026.11.8.1"));
        editor.Click(editor.Q(".game-buttons button"));
        Assert.Equal($"{Path.Combine(scratch.Games, "stat-fick-2026.11.8.1")} holds Stat Fick 2026.11.8.1, and this editor works only with Stat Fick dev.", screen.Browser.Message);
        // The games folder itself (nothing selected).
        screen.Browser.Select(-1);
        editor.Click(editor.Q(".game-buttons button"));
        Assert.Equal($"{scratch.Games} isn't Stat Fick's folder: its packs folder and Statventure.Game.Data.dll aren't in it.", screen.Browser.Message);
        Assert.Equal((0, null), (chosen, RememberedGame.Read(scratch.Player)));
        Assert.Null(screen.Game);
    }

    [Fact]
    public void UseThisFolderTakesTheTypedFolderElseTheSelectedElseTheOneShown()
    {
        using var scratch = new Scratch();
        var (editor, screen) = scratch.Show();
        using var _ = editor;
        var chosen = new List<string?>();
        screen.Chosen += g => chosen.Add(g.Folder);
        // Into the game's folder (a double click), nothing selected: the folder shown.
        var row = RowElement(screen, "stat-fick-dev");
        editor.Input.PointerMove(editor.Middle(row));
        editor.Input.PointerDown(editor.Middle(row), MouseButton.Left);
        editor.Input.PointerUp(editor.Middle(row), MouseButton.Left);
        editor.Input.PointerDown(editor.Middle(row), MouseButton.Left);
        editor.Input.PointerUp(editor.Middle(row), MouseButton.Left);
        editor.Frame();
        Assert.Equal(scratch.Game, screen.Browser.Folder);
        Assert.Equal(-1, screen.Browser.Selected);
        editor.Click(editor.Q(".game-buttons button"));
        Assert.Equal([scratch.Game], chosen);
        // A file activated stands for the folder holding it.
        screen.Browser.Activate(Row(screen, "Statventure.Game.Data.dll"));
        editor.Frame();
        Assert.Equal([scratch.Game, scratch.Game], chosen);
        // A path typed: Enter goes there; the button picks it.
        screen.Browser.GoTo(scratch.Games);
        editor.Click(screen.Browser.PathField);
        screen.Browser.PathField.Value = "stat-fick-dev";
        editor.Key("Enter", "Enter");
        Assert.Equal((scratch.Game, 2), (screen.Browser.Folder, chosen.Count));
        screen.Browser.GoTo(scratch.Games);
        screen.Browser.PathField.Value = scratch.Game;
        editor.Click(editor.Q(".game-buttons button"));
        Assert.Equal(3, chosen.Count);
        // A typed path that isn't there.
        screen.Browser.PathField.Value = "nowhere";
        editor.Click(editor.Q(".game-buttons button"));
        Assert.Equal("nowhere isn't there.", screen.Browser.Message);
        Assert.Equal(3, chosen.Count);
    }
}
