using Statventure.Editor.Shell;
using Statventure.Platform;

namespace Statventure.Tests.Editor.Shell;

/// <summary>
/// The shell's commands and keys (Docs/Editor.md, "The shell (step 4)"): handlers in order until one acts, the top
/// bar's buttons, the key chords, a text field's own keys, and the page zoom (P4: the old game's View menu).
/// </summary>
public class CommandTests
{
    [Fact]
    public void HandlersRunInOrderUntilOneActsSkippingThoseThatCantRun()
    {
        var commands = new EditorCommands();
        var log = new List<string>();
        var changed = 0;
        commands.Changed += () => changed++;
        Assert.False(commands.CanRun(EditorCommand.Save));
        Assert.False(commands.Run(EditorCommand.Save));
        var firstCan = false;
        commands.Handle(EditorCommand.Save, _ => { log.Add("first"); return true; }, () => firstCan);
        Assert.False(commands.CanRun(EditorCommand.Save));
        commands.Handle(EditorCommand.Save, _ => { log.Add("second"); return false; });
        commands.Handle(EditorCommand.Save, r => { log.Add("third " + r.Command); return true; });
        commands.Handle(EditorCommand.Save, _ => { log.Add("fourth"); return true; });
        Assert.Equal(4, changed);
        Assert.True(commands.CanRun(EditorCommand.Save));
        Assert.True(commands.Run(EditorCommand.Save));
        Assert.Equal(["second", "third Save"], log);
        log.Clear();
        firstCan = true;
        Assert.True(commands.Run(new CommandRequest(EditorCommand.Save)));
        Assert.Equal(["first"], log);
        Assert.False(commands.CanRun(EditorCommand.Open));
    }

    [Fact]
    public void AButtonIsEnabledWhileItsCommandCanRunAndAClickRunsIt()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        var save = editor.View.Button(EditorCommand.Save)!;
        Assert.False(save.IsDisabled);
        editor.Click(save);
        var request = Assert.Single(part.Requests);
        Assert.Equal(new CommandRequest(EditorCommand.Save, null, save), request);
        part.CanRun = false;
        editor.View.Refresh();
        editor.Frame();
        Assert.True(save.IsDisabled);
        editor.Click(save);
        Assert.Single(part.Requests);
        // A handler registered later enables its button at once (Import: no part of the editor's handles it yet).
        using var bare = new EditorHarness();
        Assert.True(bare.View.Button(EditorCommand.Import)!.IsDisabled);
        bare.View.Commands.Handle(EditorCommand.Import, _ => true);
        Assert.False(bare.View.Button(EditorCommand.Import)!.IsDisabled);
    }

    public static TheoryData<string, string, KeyModifiers, EditorCommand?> Chords => new()
    {
        { "s", "KeyS", KeyModifiers.Control, EditorCommand.Save },
        { "S", "KeyS", KeyModifiers.Control | KeyModifiers.Shift, EditorCommand.SaveAs },
        { "o", "KeyO", KeyModifiers.Control, EditorCommand.Open },
        { "z", "KeyZ", KeyModifiers.Control, EditorCommand.Undo },
        { "Z", "KeyZ", KeyModifiers.Control | KeyModifiers.Shift, EditorCommand.Redo },
        { "y", "KeyY", KeyModifiers.Control, EditorCommand.Redo },
        { "f", "KeyF", KeyModifiers.Control, EditorCommand.Filter },
        { "F2", "F2", KeyModifiers.None, EditorCommand.Rename },
        { "Delete", "Delete", KeyModifiers.None, EditorCommand.Remove },
        { "=", "Equal", KeyModifiers.Control, EditorCommand.ZoomIn },
        { "+", "Equal", KeyModifiers.Control | KeyModifiers.Shift, EditorCommand.ZoomIn },
        { "+", "NumpadAdd", KeyModifiers.Control, EditorCommand.ZoomIn },
        { "-", "Minus", KeyModifiers.Control, EditorCommand.ZoomOut },
        { "-", "NumpadSubtract", KeyModifiers.Control, EditorCommand.ZoomOut },
        // Not commands.
        { "s", "KeyS", KeyModifiers.None, null },
        { "s", "KeyS", KeyModifiers.Control | KeyModifiers.Alt, null },
        { "s", "KeyS", KeyModifiers.Control | KeyModifiers.Super, null },
        { "O", "KeyO", KeyModifiers.Control | KeyModifiers.Shift, null },
        { "Y", "KeyY", KeyModifiers.Control | KeyModifiers.Shift, null },
        { "F", "KeyF", KeyModifiers.Control | KeyModifiers.Shift, null },
        { "_", "Minus", KeyModifiers.Control | KeyModifiers.Shift, null },
        { "F2", "F2", KeyModifiers.Shift, null },
        { "Delete", "Delete", KeyModifiers.Shift, null },
        { "Delete", "Delete", KeyModifiers.Control, null },
        { "F2", "F2", KeyModifiers.Alt, null },
        { "Escape", "Escape", KeyModifiers.None, null },
    };

    [Theory]
    [MemberData(nameof(Chords))]
    public void EachChordIsItsCommand(string key, string code, KeyModifiers modifiers, EditorCommand? command)
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        var zoom = editor.View.Zoom;
        var allowed = editor.Key(key, code, modifiers);
        if (command is EditorCommand.ZoomIn or EditorCommand.ZoomOut)
        {
            Assert.Empty(part.Requests);
            Assert.True(command == EditorCommand.ZoomIn ? editor.View.Zoom > zoom : editor.View.Zoom < zoom);
            Assert.False(allowed);
            return;
        }
        Assert.Equal(zoom, editor.View.Zoom);
        if (command is null)
        {
            Assert.Empty(part.Requests);
            return;
        }
        // Open and Filter are the editor's own parts' from step 8 (the browser opens; the entry list's filter takes the focus), before the test's part.
        if (command == EditorCommand.Open)
        {
            Assert.Empty(part.Requests);
            Assert.Equal("Open a pack", editor.View.Dialogs.Top!.Element.ElementChildren.First().TextContent);
            Assert.False(allowed);
            return;
        }
        if (command == EditorCommand.Filter)
        {
            Assert.Empty(part.Requests);
            Assert.True(editor.Document.FocusedElement!.HasClass("entries-filter"));
            Assert.False(allowed);
            return;
        }
        var request = Assert.Single(part.Requests);
        Assert.Equal(command, request.Command);
        Assert.Equal(key, request.Key!.Key);
        Assert.Same(editor.Document.Body, request.Target);
        Assert.False(request.FromTextField);
        // A command that acted cancels the key's default action.
        Assert.False(allowed);
    }

    [Fact]
    public void AKeyNoHandlerActsOnKeepsItsDefault()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        part.Acts = false;
        Assert.True(editor.Key("s", "KeyS", KeyModifiers.Control));
        Assert.Single(part.Requests);
        // Nor does a command nothing can run now.
        part.CanRun = false;
        Assert.True(editor.Key("s", "KeyS", KeyModifiers.Control));
        Assert.Single(part.Requests);
    }

    [Fact]
    public void AKeyAnElementHandledFirstIsntACommand()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        part.Button.Focus();
        part.Button.AddEventListener<UI.Dom.UiKeyboardEvent>("keydown", e => e.PreventDefault());
        editor.Key("s", "KeyS", KeyModifiers.Control);
        Assert.Empty(part.Requests);
    }

    [Fact]
    public void InATextFieldDeleteIsTheFieldsAndUndoSaysWhereItCameFrom()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        part.Field.Focus();
        editor.Key("Delete", "Delete");
        Assert.Empty(part.Requests);
        editor.Key("F2", "F2");
        editor.Key("z", "KeyZ", KeyModifiers.Control);
        Assert.Equal([EditorCommand.Rename, EditorCommand.Undo], part.Requests.Select(r => r.Command));
        Assert.All(part.Requests, r => Assert.Same(part.Field, r.Target));
        Assert.All(part.Requests, r => Assert.True(r.FromTextField));
        // Outside the field Delete is the command again.
        part.Button.Focus();
        editor.Key("Delete", "Delete");
        Assert.Equal(EditorCommand.Remove, part.Requests[^1].Command);
        Assert.False(part.Requests[^1].FromTextField);
        // A textarea and contenteditable take text too; a checkbox doesn't.
        Assert.True(EditorCommands.TakesText(new UI.Dom.UiElement("textarea")));
        var editable = new UI.Dom.UiElement("div");
        editable.SetAttribute("contenteditable", "");
        Assert.True(EditorCommands.TakesText(editable));
        editable.SetAttribute("contenteditable", "false");
        Assert.False(EditorCommands.TakesText(editable));
        var box = new UI.Dom.UiElement("input");
        box.SetAttribute("type", "checkbox");
        Assert.False(EditorCommands.TakesText(box));
    }

    [Fact]
    public void TheZoomStepsHalfAZoomLevelWithinTheLimits()
    {
        using var editor = new EditorHarness();
        Assert.Equal(1, editor.View.Zoom);
        editor.Key("=", "Equal", KeyModifiers.Control);
        Assert.Equal(Math.Pow(1.2, 0.5), editor.View.Zoom, 5);
        Assert.Equal(editor.View.Zoom, editor.Document.PageZoom);
        Assert.Equal(1, editor.Document.DeviceScale);
        editor.Key("-", "Minus", KeyModifiers.Control);
        Assert.Equal(1, editor.View.Zoom, 5);
        // In to 300 % at most: the last step is level 6 (299 %).
        for (var i = 0; i < 20; i++)
            editor.Key("=", "Equal", KeyModifiers.Control);
        Assert.Equal(Math.Pow(1.2, 6), editor.View.Zoom, 5);
        // At the limit the key is still the zoom's (no default action).
        Assert.False(editor.Key("=", "Equal", KeyModifiers.Control));
        // Out to 50 % at least: level -3.5 (53 %).
        for (var i = 0; i < 40; i++)
            editor.Key("-", "Minus", KeyModifiers.Control);
        Assert.Equal(Math.Pow(1.2, -3.5), editor.View.Zoom, 5);
        Assert.Equal(editor.View.Zoom, editor.Document.PageZoom, 5);
    }

    [Fact]
    public void TheProgramPutsTheZoomIntoEffectItself()
    {
        using var editor = new EditorHarness(1000, 640, 1.65f);
        var applied = new List<float>();
        editor.View.ApplyZoom = applied.Add;
        editor.Key("=", "Equal", KeyModifiers.Control);
        Assert.Equal([(float)Math.Pow(1.2, 0.5)], applied);
        Assert.Equal(1, editor.Document.PageZoom);
        editor.View.ApplyZoom = null;
        editor.Key("=", "Equal", KeyModifiers.Control);
        Assert.Equal(1.2f, editor.Document.PageZoom, 4);
        Assert.Equal(1.65f, editor.Document.DeviceScale);
    }
}
