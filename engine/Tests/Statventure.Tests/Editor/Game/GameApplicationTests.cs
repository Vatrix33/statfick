using Statventure.Editor;
using Statventure.Editor.Game;
using Statventure.Editor.Settings;
using Statventure.Editor.Shell;
using Statventure.Imaging;
using Statventure.Tests.Core;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Editor.Game;

/// <summary>
/// The editor's program finding its game (Q16, Q20; M12 step 6): its first start shows "Where is Stat Fick?" and the game
/// chosen there becomes the editor's view; the folder remembered starts the editor at once, one gone shows the screen
/// saying so; Game folder… makes the session and view anew; <c>--capture</c> draws, writes the frame and quits. Real
/// applications in hidden windows (nothing shown), counted as minimized with immediate wake-ups where they run many
/// updates (a hidden window's presents hang after a few frames under wlroots), in the "Window" collection.
/// </summary>
[Collection("Window")]
public class GameApplicationTests
{
    sealed class Scratch : IDisposable
    {
        public readonly StorageFolder Folder = new();

        public Scratch()
        {
            GameTesting.MakeGame(Folder["games/a"]);
            GameTesting.MakeGame(Folder["games/b"]);
            Directory.CreateDirectory(Folder["player"]);
        }

        public string Player => Folder["player"];

        public void Dispose() => Folder.Dispose();
    }

    /// <summary>
    /// An editor application on <paramref name="game"/> (null: it looks itself) in a hidden window, counted as minimized,
    /// whose updates <paramref name="update"/> drives (given the update's number; true quits; past 60 it quits, so a loop
    /// that wouldn't fails rather than hangs); gives the number of updates.
    /// </summary>
    static int Run(Statventure.Editor.Session.GameData? game, string player, Action<EditorApplication> loaded, Func<EditorApplication, int, bool> update)
    {
        var updates = 0;
        using var application = new EditorApplication(game, player, visible: false)
        {
            AfterLoad = a =>
            {
                a.Window.MinimizedForTests = true;
                loaded(a);
            },
            AfterUpdate = a =>
            {
                var n = updates++;
                a.Window.RequestWakeUp(0);
                if (update(a, n) || n >= 60)
                    a.Quit();
            },
        };
        application.Run();
        return updates;
    }

    [Fact]
    public void TheFirstStartAsksWhereStatFickIsThenShowsTheEditorOnTheGameChosen()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var scratch = new Scratch();
        var seen = new List<string>();
        var updates = Run(null, scratch.Player, a => seen.Add($"load: screen {a.Screen is not null}, view {a.View is not null}, \"{a.Window.Title}\", problem \"{a.Screen?.Problem}\""), (a, n) =>
        {
            if (n == 0)
            {
                var browser = a.Screen!.Browser;
                browser.PathField.Value = scratch.Folder["games/a"];
                a.Screen.Document.RunScript(browser.Choose);
                return false;
            }
            if (a.View is null)
                return false;
            seen.Add($"update {n}: screen {a.Screen is not null}, the game in {a.Session!.Game.Folder}, its kind {a.Session.Types.Find(GameTesting.StandInKind) is not null}, \"{a.Window.Title}\"");
            return true;
        });
        Assert.Equal([
            "load: screen True, view False, \"Statventure Editor dev\", problem \"\"",
            $"update 1: screen False, the game in {scratch.Folder["games/a"]}, its kind True, \"Statventure Editor dev\"",
        ], seen);
        Assert.Equal(2, updates);
        Assert.Equal(scratch.Folder["games/a"], RememberedGame.Read(scratch.Player));
    }

    [Fact]
    public void TheScreenTakesTheRememberedZoomAndClosesAtOnce()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var scratch = new Scratch();
        new ShellSettings(1, 1000, 700).Write(scratch.Player);
        float? zoom = null;
        // The window's close while the screen shows: nothing to ask, the run ends (the size and zoom remembered as they were).
        var updates = Run(null, scratch.Player, a => zoom = a.Screen!.Document.PageZoom, (a, n) =>
        {
            if (n == 0)
                a.Window.CloseRequested = true;
            return false;
        });
        Assert.Equal(1.2f, zoom!.Value, 0.0001f);
        Assert.Equal(1, updates);
        Assert.Equal(new ShellSettings(1, 1000, 700), ShellSettings.Read(scratch.Player));
    }

    [Fact]
    public void TheRememberedGameStartsTheEditorAndOneGoneShowsTheScreenSayingSo()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var scratch = new Scratch();
        RememberedGame.Write(scratch.Player, scratch.Folder["games/b"]);
        string? folder = null;
        Run(null, scratch.Player, a => folder = a.View is null ? "screen" : a.Session!.Game.Folder, (_, _) => true);
        Assert.Equal(scratch.Folder["games/b"], folder);
        // Gone (a new version unpacked beside it and the old one deleted): the screen, saying so, the browser beside it.
        Directory.Delete(scratch.Folder["games/b"], recursive: true);
        string? said = null, shown = null;
        Run(null, scratch.Player, a => (said, shown) = (a.Screen?.Problem, a.Screen?.Browser.Folder), (_, _) => true);
        Assert.Equal($"Stat Fick isn't in {scratch.Folder["games/b"]} any more.", said);
        Assert.Equal(scratch.Folder["games"], shown);
        // The command line's game wins over the one remembered (and isn't remembered).
        RememberedGame.Write(scratch.Player, scratch.Folder["games/b"]);
        string? given = null;
        Run(GameLookup.Find(scratch.Folder["games/a"], EditorView.Version).Game, scratch.Player, a => given = a.Session!.Game.Folder, (_, _) => true);
        Assert.Equal((scratch.Folder["games/a"], scratch.Folder["games/b"]), (given, RememberedGame.Read(scratch.Player)));
    }

    [Fact]
    public void GameFolderMakesTheSessionAndViewAnew()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var scratch = new Scratch();
        new ShellSettings(1, 1000, 700).Write(scratch.Player);
        EditorView? first = null;
        var seen = new List<string>();
        Run(GameLookup.Find(scratch.Folder["games/a"], EditorView.Version).Game, scratch.Player, a => first = a.View, (a, n) =>
        {
            if (n == 0)
            {
                a.View!.ZoomLevel = 2;
                a.View.Document.RunScript(() => _ = a.View.Part<GamePart>().Change());
                return false;
            }
            if (n == 1)
            {
                var browser = a.View!.Part<Statventure.Editor.Browser.FileBrowserPart>().Current!;
                browser.PathField.Value = scratch.Folder["games/b"];
                a.View.Document.RunScript(browser.Choose);
                return false;
            }
            if (a.View == first)
                return false;
            seen.Add($"update {n}: a new view {a.View is not null}, the game in {a.Session!.Game.Folder}, zoom {a.View!.ZoomLevel}");
            return true;
        });
        Assert.Equal([$"update 2: a new view True, the game in {scratch.Folder["games/b"]}, zoom 2"], seen);
        Assert.Equal(scratch.Folder["games/b"], RememberedGame.Read(scratch.Player));
        Assert.Equal(2, ShellSettings.Read(scratch.Player).ZoomLevel);
    }

    [Fact]
    public void CaptureDrawsTheFramesWritesTheLastAndQuits()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var scratch = new Scratch();
        var file = scratch.Folder["shot.png"];
        var updates = 0;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        // Its first start: the screen is what it draws (Q20: the packaged editor's check, step 8). A hidden window's
        // second present hangs under wlroots, so the capture is of the first frame, the window counted as minimized
        // from the second update on (no frame acquired then) and woken at once; the capture's own quit (a task after the
        // frame is saved) ends the run, within 10 s or the test does.
        using (var application = new EditorApplication(null, scratch.Player, visible: false)
        {
            CaptureFile = file,
            CaptureFrames = 0,
            AfterUpdate = a =>
            {
                a.Window.MinimizedForTests = ++updates >= 2;
                a.Window.RequestWakeUp(0);
                if (clock.Elapsed.TotalSeconds > 10)
                    a.Quit();
            },
        })
            application.Run();
        Assert.True(clock.Elapsed.TotalSeconds < 10, $"the capture didn't end the run ({updates} updates)");
        Assert.True(updates >= 2);
        var image = ImageFile.Load(file);
        Assert.True(image.Width > 0 && image.Height > 0);
        var colours = new HashSet<int>();
        for (var i = 0; i < image.Pixels.Length; i += 4)
            colours.Add(image.Pixels[i] << 16 | image.Pixels[i + 1] << 8 | image.Pixels[i + 2]);
        Assert.Contains(0x1a1c1a, colours);
        Assert.True(colours.Count > 20, $"{colours.Count} colours");
        // The program's own options reach it: --capture and --frames.
        // A crash in a capture run saves the crash window's first frame there and ends (step 5's CrashCapture); the
        // program's splash first (its log name), the tests' none.
        using var program = new EditorApplication(null, scratch.Player, file, 7);
        Assert.Equal((file, 7, file, true), (program.CaptureFile, program.CaptureFrames, program.CrashCapture, program.ShowsSplash));
        using var none = new EditorApplication(null, scratch.Player);
        Assert.Equal((null, 30, null), (none.CaptureFile, none.CaptureFrames, none.CrashCapture));
        using var tests = new EditorApplication(null, scratch.Player, visible: false);
        Assert.False(tests.ShowsSplash);
    }
}
