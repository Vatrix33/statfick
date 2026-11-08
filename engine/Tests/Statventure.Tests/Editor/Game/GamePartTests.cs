using Statventure.Audio;
using Statventure.Editor.Browser;
using Statventure.Editor.Game;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Records;
using Statventure.Tests.Storage;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Game;

/// <summary>
/// Game folder… (Q16, the manager's decision Q-C; M12 step 6): the top bar's button, off in the dev setup; the browser
/// picking a folder over the shell; a game whose library is the run's build becomes the editor's at once once the
/// unsaved work is saved or let go, another build only after a new start; the folder remembered either way.
/// </summary>
public class GamePartTests
{
    sealed class Scratch : IDisposable
    {
        public readonly StorageFolder Folder = new();

        public Scratch()
        {
            GameTesting.MakeGame(Folder["games/a"]);
            GameTesting.MakeGame(Folder["games/b"]);
            GameTesting.MakeGame(Folder["games/release"], library: false);
            GameTesting.FakeLibrary(Folder["games/release/Statventure.Game.Data.dll"], "2026.11.8.1+0123");
            GameTesting.MakeGame(Folder["games/other-build"], library: false);
            GameTesting.FakeLibrary(Folder["games/other-build/Statventure.Game.Data.dll"], "dev+0123");
            Directory.CreateDirectory(Folder["player"]);
        }

        public string Player => Folder["player"];

        /// <summary>The editor on the game in <c>games/a</c>, as the program finds it.</summary>
        public EditorHarness Editor()
        {
            var game = GameLookup.Find(Folder["games/a"], EditorView.Version).Game!;
            var editor = new EditorHarness(game, 1000, 640, 1);
            editor.Session.PlayerFolder = Player;
            return editor;
        }

        public void Dispose() => Folder.Dispose();
    }

    static GamePart Part(EditorHarness editor) => editor.View.Part<GamePart>();

    static FileBrowserDialog Browser(EditorHarness editor) => editor.Browser().Current ?? throw new InvalidOperationException("the browser isn't open");

    static void Use(EditorHarness editor, string folder)
    {
        var browser = Browser(editor);
        browser.PathField.Value = folder;
        editor.Click(browser.Button);
    }

    [Fact]
    public void GameFolderIsOffOnlyInTheDevSetup()
    {
        using (var dev = new EditorHarness())
        {
            var button = dev.View.Button(EditorCommand.GameFolder)!;
            Assert.True(button.IsDisabled);
            Assert.Equal("Stat Fick's folder (the dev setup's game is the repo)", button.GetAttribute("title"));
            dev.Click(button);
            Assert.Null(dev.Browser().Current);
        }
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var enabled = editor.View.Button(EditorCommand.GameFolder)!;
        Assert.False(enabled.IsDisabled);
        Assert.Equal($"Stat Fick's folder: {scratch.Folder["games/a"]}", enabled.GetAttribute("title"));
        editor.Click(enabled);
        var browser = Browser(editor);
        Assert.Equal(("Where is Stat Fick?", "Use this folder", true), (browser.Dialog.Element.ElementChildren.First().TextContent, browser.Button.TextContent, browser.Request.Folder));
        Assert.Equal(GameScreen.LeadLine(windows: false) + $" The editor works on the game in {scratch.Folder["games/a"]} now.", editor.Q(".browser-lead").TextContent);
        // It starts beside the game's folder.
        Assert.Equal(scratch.Folder["games"], browser.Folder);
        editor.Key("Escape", "Escape");
        Assert.Null(editor.Browser().Current);
    }

    [Fact]
    public void AGameOfTheRunsBuildBecomesTheEditorsOnceTheUnsavedWorkIsLetGo()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var chosen = new List<GameData>();
        Part(editor).Chosen += chosen.Add;
        // Unsaved work: a mod with a room changed.
        var mod = scratch.Folder["player/mods/my-mod.pak"];
        Directory.CreateDirectory(Path.GetDirectoryName(mod)!);
        var writer = new Statventure.Packs.PackWriter();
        writer.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5));
        writer.Write(mod);
        editor.Session.Open(mod);
        var crypt = editor.Session.Entry("audio/rooms/crypt")!;
        editor.Session.Commit(crypt, crypt.Draft!.With("Seconds", DraftValue.Of(4.0)));
        Assert.True(editor.Session.HasUnsavedWork);
        // Cancelled at the question: nothing changes, nothing remembered.
        var first = editor.Run(() => Part(editor).Change());
        Use(editor, scratch.Folder["games/b"]);
        Assert.Equal(("Unsaved changes", $"my-mod.pak has changes that aren't saved. Discard them and work on the game in {scratch.Folder["games/b"]}?"), editor.Asked());
        Assert.Equal(["Save", "Discard", "Cancel"], editor.Buttons());
        editor.Answer("Cancel");
        Assert.False(first.Done());
        Assert.Empty(chosen);
        Assert.Null(RememberedGame.Read(scratch.Player));
        // Let go: the game in b, its kinds the run's library's.
        var second = editor.Run(() => Part(editor).Change());
        Use(editor, scratch.Folder["games/b"]);
        editor.Answer("Discard");
        Assert.True(second.Done());
        var game = Assert.Single(chosen);
        Assert.Equal((scratch.Folder["games/b"], false), (game.Folder, game.DevSetup));
        Assert.Same(GameTesting.StandInType(), game.Types.Find(GameTesting.StandInKind));
        Assert.Equal(scratch.Folder["games/b"], RememberedGame.Read(scratch.Player));
    }

    [Fact]
    public void AFolderTheEditorCantWorkOnIsSaidInTheBrowser()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var chosen = 0;
        Part(editor).Chosen += _ => chosen++;
        var change = editor.Run(() => Part(editor).Change());
        Use(editor, scratch.Folder["games/release"]);
        Assert.Equal($"{scratch.Folder["games/release"]} holds Stat Fick 2026.11.8.1, and this editor works only with Stat Fick dev.", Browser(editor).Message);
        Use(editor, scratch.Folder["games"]);
        Assert.Equal($"{scratch.Folder["games"]} isn't Stat Fick's folder: its packs folder and Statventure.Game.Data.dll aren't in it.", Browser(editor).Message);
        editor.Answer("Cancel");
        Assert.False(change.Done());
        Assert.Equal((0, null), (chosen, RememberedGame.Read(scratch.Player)));
    }

    [Fact]
    public void AnotherBuildOfTheLibraryNeedsANewStartTheFolderRemembered()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var chosen = 0;
        Part(editor).Chosen += _ => chosen++;
        var change = editor.Run(() => Part(editor).Change());
        Use(editor, scratch.Folder["games/other-build"]);
        Assert.Null(editor.Browser().Current);
        Assert.Equal(("Start the editor again", $"Start the editor again to work on the game in {scratch.Folder["games/other-build"]}: this editor has already loaded " +
            $"another build of Stat Fick's data library (from {GameLibraryLoader.Run.Loaded!.File}). The folder is remembered for the next start."), editor.Asked());
        editor.Answer("OK");
        Assert.False(change.Done());
        Assert.Equal(0, chosen);
        Assert.Equal(scratch.Folder["games/other-build"], RememberedGame.Read(scratch.Player));
    }
}
