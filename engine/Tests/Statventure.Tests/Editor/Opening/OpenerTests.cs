using Statventure.Audio;
using Statventure.Editor.Shell;
using Statventure.Platform;
using Statventure.Tests.Storage;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Opening;

/// <summary>
/// Opening from the view (Docs/Editor.md, "Opening packs (step 8)"): Ctrl+O and the Open button through the browser,
/// a pack <c>Open</c> refuses said with why, unsaved work asked about first (and when the editor closes, M11-17),
/// and files dropped on the window (P27).
/// </summary>
public class OpenerTests
{
    /// <summary>A scratch folder with a player's mods: <c>a.pak</c> and <c>b.pak</c>, each a room of its own.</summary>
    sealed class Scratch : IDisposable
    {
        public readonly StorageFolder Folder = new();

        public Scratch()
        {
            Pack("player/mods/a.pak", "audio/rooms/a");
            Pack("player/mods/b.pak", "audio/rooms/b");
        }

        public string Pack(string relative, string room)
        {
            var writer = new Statventure.Packs.PackWriter();
            writer.AddRecord(room, new RoomSpec(2, 0.5, 0.5));
            var file = Folder[relative];
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            writer.Write(file);
            return file;
        }

        public EditorHarness Editor()
        {
            var editor = new EditorHarness(1000, 640);
            editor.Session.PlayerFolder = Folder["player"];
            return editor;
        }

        public void Dispose() => Folder.Dispose();
    }

    /// <summary>Makes the open pack's room changed (unsaved work).</summary>
    static void Change(EditorHarness editor)
    {
        var room = editor.Session.Entries.First(e => e.Source == Statventure.Editor.Session.EntrySource.Pack);
        editor.Session.Commit(room, room.Draft!.Set("Seconds", 4.0));
        Assert.True(editor.Session.HasUnsavedWork);
    }

    [Fact]
    public void CtrlOAsksTheBrowserForAPackAndOpensIt()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        Assert.True(editor.View.Commands.CanRun(EditorCommand.Open));
        Assert.False(editor.View.Button(EditorCommand.Open)!.HasAttribute("disabled"));
        editor.Key("o", "KeyO", KeyModifiers.Control);
        var dialog = editor.Browser().Current!;
        Assert.Equal("Open a pack", editor.Asked().Title);
        Assert.Equal(["a.pak", "b.pak"], dialog.Entries.Select(e => e.Name));
        editor.Key("ArrowDown", "ArrowDown");
        editor.Key("Enter", "Enter");
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Equal("a.pak", editor.Session.Pack!.Name);
        Assert.Equal("Statventure Editor — a.pak", editor.View.Title);
        Assert.Equal("Opened a.pak: 1 entry", editor.View.Status);
        // The top bar's button too; cancelled, nothing changes.
        editor.Click(editor.View.Button(EditorCommand.Open)!);
        Assert.NotNull(editor.Browser().Current);
        editor.Key("Escape", "Escape");
        Assert.Equal("a.pak", editor.Session.Pack!.Name);
    }

    [Fact]
    public void APackOpenRefusesIsSaidWithWhyAndTheOpenOneStays()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var a = editor.Run(() => editor.Opener().OpenFile(scratch.Folder["player/mods/a.pak"]));
        Assert.True(a.IsCompleted && a.Done());
        var refused = Refused(scratch.Folder, "player/mods/refused.pak");
        var opening = editor.Run(() => editor.Opener().OpenFile(refused));
        Assert.Equal("The pack can't be opened", editor.Asked().Title);
        Assert.StartsWith("refused.pak can't be opened: ", editor.Asked().Text);
        Assert.DoesNotContain(scratch.Folder.Root, editor.Asked().Text);
        Assert.Equal(["OK"], editor.Buttons());
        editor.Answer("OK");
        Assert.True(opening.IsCompleted);
        Assert.False(opening.Done());
        Assert.Equal("a.pak", editor.Session.Pack!.Name);
        var missing = editor.Run(() => editor.Opener().OpenFile(scratch.Folder["player/mods/missing.pak"]));
        Assert.Equal("missing.pak can't be opened: the file isn't there.", editor.Asked().Text);
        editor.Answer("OK");
        Assert.False(missing.Done());
        Assert.Equal("a.pak", editor.Session.Pack!.Name);
    }

    [Fact]
    public void OpeningOverUnsavedWorkAsksFirst()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        editor.Session.Open(scratch.Folder["player/mods/a.pak"]);
        Change(editor);
        // (Without step 11's Save first: Discard and Cancel only.)
        editor.Opener().SaveFirst = null;
        var cancelled = editor.Run(() => editor.Opener().OpenFile(scratch.Folder["player/mods/b.pak"]));
        Assert.Equal(("Unsaved changes", "a.pak has changes that aren't saved. Discard them and open b.pak?"), editor.Asked());
        Assert.Equal(["Discard", "Cancel"], editor.Buttons());
        editor.Answer("Cancel");
        Assert.True(cancelled.IsCompleted);
        Assert.False(cancelled.Done());
        Assert.Equal("a.pak", editor.Session.Pack!.Name);
        Assert.True(editor.Session.HasUnsavedWork);
        var discarded = editor.Run(() => editor.Opener().OpenFile(scratch.Folder["player/mods/b.pak"]));
        editor.Answer("Discard");
        Assert.True(discarded.IsCompleted && discarded.Done());
        Assert.Equal("b.pak", editor.Session.Pack!.Name);
        Assert.False(editor.Session.HasUnsavedWork);
        // Save first (step 11 gives it): offered first; a save that didn't happen keeps the pack open.
        Change(editor);
        var saves = new Queue<bool>([false, true]);
        editor.Opener().SaveFirst = () => Task.FromResult(saves.Dequeue());
        var unsaved = editor.Run(() => editor.Opener().OpenFile(scratch.Folder["player/mods/a.pak"]));
        Assert.Equal(["Save", "Discard", "Cancel"], editor.Buttons());
        editor.Answer("Save");
        Assert.True(unsaved.IsCompleted);
        Assert.False(unsaved.Done());
        Assert.Equal("b.pak", editor.Session.Pack!.Name);
        var saved = editor.Run(() => editor.Opener().OpenFile(scratch.Folder["player/mods/a.pak"]));
        editor.Answer("Save");
        Assert.True(saved.IsCompleted && saved.Done());
        Assert.Equal("a.pak", editor.Session.Pack!.Name);
        // Nothing unsaved: no question.
        var plain = editor.Run(() => editor.Opener().OpenFile(scratch.Folder["player/mods/b.pak"]));
        Assert.True(plain.IsCompleted && plain.Done());
        Assert.Null(editor.View.Dialogs.Top);
    }

    [Fact]
    public void ClosingWithUnsavedWorkAsksOnceAndQuitsWhenLetGo()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var quits = 0;
        void Quit() => quits++;
        Assert.True(editor.Opener().ConfirmClose(Quit));
        editor.Session.Open(scratch.Folder["player/mods/a.pak"]);
        Assert.True(editor.Opener().ConfirmClose(Quit));
        Change(editor);
        Assert.False(editor.Opener().ConfirmClose(Quit));
        editor.Frame();
        Assert.Equal(("Unsaved changes", "a.pak has changes that aren't saved. Discard them and close the editor?"), editor.Asked());
        // Closed again while it asks: the one question stays.
        Assert.False(editor.Opener().ConfirmClose(Quit));
        editor.Frame();
        Assert.Single(editor.View.Dialogs.Open);
        editor.Answer("Cancel");
        Assert.Equal(0, quits);
        Assert.Null(editor.View.Dialogs.Top);
        // Asked again on the next close; Discard quits.
        Assert.False(editor.Opener().ConfirmClose(Quit));
        editor.Frame();
        editor.Answer("Discard");
        Assert.Equal(1, quits);
    }

    [Fact]
    public void ADroppedPackOpensAndOtherFilesWaitForImport()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var model = scratch.Folder.Write("drops/arena.glb", [1]);
        var css = scratch.Folder.Write("drops/style.css", "}"u8.ToArray());
        var drop = editor.Run(() => editor.Opener().DropAsync([model, scratch.Folder["player/mods/a.pak"], scratch.Folder["drops"],
            scratch.Folder["player/mods/b.pak"], css, "https://example.com/x.pak", scratch.Folder["drops/gone.png"]]));
        Assert.True(drop.IsCompleted);
        Assert.Equal("a.pak", editor.Session.Pack!.Name);
        // Step 12's import takes the queue at once (so the status doesn't say the files wait) and asks about each file
        // in its order: these two can't be imported.
        Assert.True(editor.Session.CanImport);
        Assert.Empty(editor.Session.PendingImports);
        Assert.Equal("Opened a.pak: 1 entry; not opened: drops, b.pak, x.pak, gone.png.", editor.View.Status);
        Assert.StartsWith("arena.glb can't be imported: the models importer: ", editor.Asked().Text);
        editor.Answer("OK");
        Assert.StartsWith("style.css can't be imported: the ui importer: ", editor.Asked().Text);
        editor.Answer("OK");
        Assert.Null(editor.View.Dialogs.Top);
        // A pack the case of whose ending differs opens too; over unsaved work, after asking.
        var upper = scratch.Pack("player/mods/C.PAK", "audio/rooms/c");
        Change(editor);
        editor.Opener().Drop([upper]);
        editor.Frame();
        Assert.Equal("Unsaved changes", editor.Asked().Title);
        editor.Answer("Discard");
        Assert.Equal("C.PAK", editor.Session.Pack!.Name);
        Assert.Equal("Opened C.PAK: 1 entry.", editor.View.Status);
        // A pack that can't be opened: said, the open one stays.
        editor.Opener().Drop([Refused(scratch.Folder, "drops/refused.pak")]);
        editor.Frame();
        editor.Answer("OK");
        Assert.Equal("C.PAK", editor.Session.Pack!.Name);
    }

    [Fact]
    public void ADroppedPackThatCantBeOpenedLeavesTheStatusAsItWas()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var drop = editor.Run(() => editor.Opener().DropAsync([Refused(scratch.Folder, "drops/refused.pak")]));
        Assert.Equal("The pack can't be opened", editor.Asked().Title);
        editor.Answer("OK");
        Assert.True(drop.IsCompletedSuccessfully);
        Assert.Null(editor.Session.Pack);
        Assert.Equal("No pack open", editor.View.Status);
        // Only things passed over: said, from a capital. A relative name isn't a file here, even one the working folder has.
        Assert.True(File.Exists("Statventure.Tests.dll"));
        drop = editor.Run(() => editor.Opener().DropAsync([scratch.Folder.Root, "Statventure.Tests.dll"]));
        Assert.True(drop.IsCompletedSuccessfully);
        Assert.Empty(editor.Session.PendingImports);
        Assert.Equal($"Not opened: {Path.GetFileName(scratch.Folder.Root)}, Statventure.Tests.dll.", editor.View.Status);
    }

    [Fact]
    public void ThePackOpenedSaysWhyItIsReadOnly()
    {
        using var scratch = new Scratch();
        var broken = scratch.Folder["player/mods/broken.pak"];
        var writer = new Statventure.Packs.PackWriter();
        AddUnreadable(writer, "bad/one", "not a record");
        AddUnreadable(writer, "bad/two", "unknown kind");
        writer.Write(broken);
        using var editor = scratch.Editor();
        var open = editor.Run(() => editor.Opener().OpenFile(broken));
        Assert.True(open.Done());
        Assert.Equal("Opened broken.pak: 2 entries, read-only: 2 entries can't be read", editor.View.Status);
    }
}
