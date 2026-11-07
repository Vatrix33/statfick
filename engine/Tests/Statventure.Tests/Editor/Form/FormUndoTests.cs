using Statventure.Editor.Shell;
using Statventure.Records;
using Statventure.Tests.Records;

namespace Statventure.Tests.Editor.Form;

/// <summary>
/// Undo and redo per entry (M11-7) by the keys and the top bar's buttons: each entry's own history, a form field's
/// own typing undone and redone first (P13), structure brought back by undo; adding, renaming and removing entries
/// out of reach (they ask instead).
/// </summary>
public class FormUndoTests
{
    static Kinds Built(EditorHarness editor) => editor.Built<Kinds>();

    static EditorHarness Two() => FormTesting.Open(FormTesting.Entry("kinds/a", FormScreens.Sample()), FormTesting.Entry("kinds/b", FormScreens.Sample()));

    [Fact]
    public void UndoAndRedoArePerEntry()
    {
        using var editor = Two();
        var (a, b) = (editor.Session.Entries[0], editor.Session.Entries[1]);
        editor.Enter(editor.Row("$.int32").Control("input", "number"), "1");
        editor.Enter(editor.Row("$.int32").Control("input", "number"), "2");
        editor.Session.Select(b);
        editor.Frame();
        editor.Enter(editor.Row("$.int32").Control("input", "number"), "9");
        editor.Session.Select(a);
        editor.Frame();
        Assert.Equal("2", editor.Row("$.int32").Control("input", "number").Value);
        editor.Undo();
        Assert.Equal(1, Built(editor).Int32);
        Assert.Equal("1", editor.Row("$.int32").Control("input", "number").Value);
        Assert.Equal(9, ((Kinds)b.Build!.Value!).Int32);
        editor.Undo();
        Assert.Equal(-70000, Built(editor).Int32);
        Assert.False(a.CanUndo);
        // Nothing left to undo: the key does nothing (no handler can run).
        Assert.False(editor.View.Commands.CanRun(EditorCommand.Undo));
        editor.Undo();
        Assert.Equal(-70000, Built(editor).Int32);
        editor.Redo();
        Assert.Equal(1, Built(editor).Int32);
        editor.Session.Select(b);
        editor.Frame();
        editor.Undo();
        Assert.Equal(-70000, ((Kinds)b.Build!.Value!).Int32);
        Assert.Equal(1, ((Kinds)a.Build!.Value!).Int32);
        editor.Key("z", "KeyZ", Statventure.Platform.KeyModifiers.Control | Statventure.Platform.KeyModifiers.Shift);
        Assert.Equal(9, ((Kinds)b.Build!.Value!).Int32);
    }

    [Fact]
    public void AFieldsOwnTypingIsUndoneFirst()
    {
        using var editor = Two();
        editor.Enter(editor.Row("$.int32").Control("input", "number"), "5");
        var shorts = editor.Row("$.int16").Control("input", "number");
        editor.Type(shorts, "77");
        Assert.Equal("77", shorts.EditorText);
        editor.Undo();
        // The field's own undo: its text back, the entry untouched.
        Assert.Equal("300", shorts.EditorText);
        Assert.Equal(5, Built(editor).Int32);
        Assert.Equal(1, editor.Session.Selected!.HistoryPosition);
        editor.Undo();
        Assert.Equal(-70000, Built(editor).Int32);
        Assert.Equal(0, editor.Session.Selected!.HistoryPosition);
        Assert.Same(shorts, editor.Document.FocusedElement);
        Assert.Equal("300", shorts.EditorText);
    }

    [Fact]
    public void RedoTakesTheFieldsOwnUndoneTypingFirst()
    {
        using var editor = Two();
        editor.Enter(editor.Row("$.int32").Control("input", "number"), "5");
        editor.Press(editor.View.Button(EditorCommand.Undo)!);
        Assert.True(editor.Session.Selected!.CanRedo);
        var shorts = editor.Row("$.int16").Control("input", "number");
        editor.Type(shorts, "77");
        editor.Undo();
        Assert.Equal("300", shorts.EditorText);
        editor.Redo();
        // The field's typing came back; the entry's redo waits.
        Assert.Equal("77", shorts.EditorText);
        Assert.Equal(-70000, Built(editor).Int32);
        Assert.True(editor.Session.Selected!.CanRedo);
        // Typing not committed keeps Redo the field's too.
        editor.Redo();
        Assert.Equal(-70000, Built(editor).Int32);
        // Typing again after the field's own undo ends its redo: once its text is the committed one, Redo is the entry's.
        editor.Undo();
        Assert.Equal("300", shorts.EditorText);
        // (The undo selected the field's text again: the caret goes to its end first.)
        editor.Key("End", "End");
        editor.Type(shorts, "1", replace: false);
        editor.Key("Backspace", "Backspace");
        Assert.Equal("300", shorts.EditorText);
        editor.Redo();
        Assert.Equal(5, Built(editor).Int32);
        Assert.Equal("300", shorts.EditorText);
        editor.Press(editor.Row("$.flag").Control("input", "checkbox"));
        Assert.False(editor.Session.Selected!.CanRedo);
    }

    [Fact]
    public void UndoIsntOfferedOnceThePackTakesNoChanges()
    {
        using var editor = Two();
        editor.Press(editor.Row("$.flag").Control("input", "checkbox"));
        editor.Press(editor.Row("$.flag").Control("input", "checkbox"));
        editor.Press(editor.View.Button(EditorCommand.Undo)!);
        Assert.True(editor.Session.Selected!.CanUndo && editor.Session.Selected!.CanRedo);
        editor.Session.Writable = false;
        Assert.False(editor.View.Commands.CanRun(EditorCommand.Undo));
        Assert.False(editor.View.Commands.CanRun(EditorCommand.Redo));
        var draft = editor.Session.Selected!.Draft;
        editor.Undo();
        editor.Redo();
        Assert.Same(draft, editor.Session.Selected!.Draft);
    }

    [Fact]
    public void TheTopBarsButtonsUndoAndRedoTheSelectedEntry()
    {
        using var editor = Two();
        var undo = editor.View.Button(EditorCommand.Undo)!;
        var redo = editor.View.Button(EditorCommand.Redo)!;
        Assert.True(undo.HasAttribute("disabled"));
        Assert.True(redo.HasAttribute("disabled"));
        editor.Press(editor.Row("$.flag").Control("input", "checkbox"));
        Assert.False(undo.HasAttribute("disabled"));
        editor.Press(undo);
        Assert.True(Built(editor).Flag);
        Assert.True(undo.HasAttribute("disabled"));
        Assert.False(redo.HasAttribute("disabled"));
        editor.Press(redo);
        Assert.False(Built(editor).Flag);
        // Another entry selected: its own history decides.
        editor.Session.Select(editor.Session.Entries[1]);
        editor.Frame();
        Assert.True(undo.HasAttribute("disabled"));
        editor.Session.Select(null);
        editor.Frame();
        Assert.True(undo.HasAttribute("disabled"));
        Assert.False(editor.View.Commands.Run(EditorCommand.Undo));
    }

    [Fact]
    public void UndoBringsBackStructure()
    {
        using var editor = Two();
        editor.Press(editor.Row("$.ints[0]").Button("Remove"));
        Assert.Throws<InvalidOperationException>(() => editor.Row("$.ints[1]"));
        editor.Undo();
        Assert.Equal("-3", editor.Row("$.ints[1]").Control("input", "number").Value);
        Assert.Equal([2, -3], Built(editor).Ints);
        editor.Choose(editor.Row("$.shape").Control("select"), "Square");
        editor.Undo();
        Assert.Equal("2", editor.Row("$.shape.radius").Control("input", "number").Value);
        Assert.Equal("Circle", editor.Row("$.shape").Control("select").SelectedOption!.TextContent);
        editor.Press(editor.Row("$.maybePart").Control("input", "checkbox"));
        editor.Enter(editor.Row("$.maybePart.name").Control("input", "text"), "Fin");
        editor.Undo();
        Assert.Equal(["needs a value"], editor.Row("$.maybePart.name").Problems());
        Assert.Equal("", editor.Row("$.maybePart.name").Control("input", "text").Value);
        editor.Undo();
        Assert.Throws<InvalidOperationException>(() => editor.Row("$.maybePart.name"));
        Assert.Null(Built(editor).MaybePart);
        editor.Redo();
        editor.Redo();
        Assert.Equal(new Part { Name = "Fin" }, Built(editor).MaybePart);
    }

    [Fact]
    public void AddingRenamingAndRemovingEntriesArentUndone()
    {
        using var editor = Two();
        var a = editor.Session.Entries[0];
        editor.Session.Rename(a, "kinds/z");
        editor.Session.Add("kinds/new", RecordDraft.Blank(typeof(Kinds)));
        editor.Session.Remove(editor.Session.Entry("kinds/b")!);
        editor.Session.Select(a);
        editor.Undo();
        editor.Undo();
        Assert.Equal(["kinds/new", "kinds/z"], editor.Session.Entries.Select(e => e.Path));
        Assert.False(a.CanUndo);
    }
}
