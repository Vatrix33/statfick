using System.Numerics;
using Statventure.Editor.Shell;
using Statventure.Platform;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor.Shell;

/// <summary>
/// The shell's dialogs through UI events: shown over everything with the rest inert, focus kept inside (Tab and
/// Shift+Tab, a press outside the box), Escape cancels, buttons close with their values, focus goes back.
/// </summary>
public class DialogTests
{
    /// <summary>A dialog's value, once it has closed (never waits).</summary>
    static string? Done(Task<string?> closed)
    {
        Assert.True(closed.IsCompleted);
        return closed.Result;
    }

    [Fact]
    public void ADialogShowsOverTheShellWhichTurnsInertAndTakesTheFocus()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        part.Button.Focus();
        var closed = editor.View.Dialogs.Ask("Question", "Sure?", ("Yes", "yes"), ("Later", "later"), ("Cancel", null));
        var dialog = editor.View.Dialogs.Top!;
        Assert.Same(editor.Document.Body, dialog.Layer.Parent);
        Assert.Same(dialog.Layer, editor.Document.Body.ElementChildren.Last());
        Assert.True(editor.View.Shell.Inert);
        Assert.Equal("dialog", dialog.Element.GetAttribute("role"));
        Assert.Equal("true", dialog.Element.GetAttribute("aria-modal"));
        Assert.Equal("Question", editor.Document.GetElementById(dialog.Element.GetAttribute("aria-labelledby")!)!.TextContent);
        Assert.Equal("Sure?", dialog.Content.TextContent);
        var buttons = dialog.Buttons.ElementChildren.ToList();
        Assert.Equal(["Yes", "Later", "Cancel"], buttons.Select(b => b.TextContent));
        Assert.Equal([true, false, false], buttons.Select(b => b.HasClass("primary")));
        // Its first focusable element has the focus.
        Assert.Same(buttons[0], editor.Document.FocusedElement);
        // It covers the window: the shell's button can't be reached by the pointer either.
        Assert.Equal((0f, 0f, 1000f, 640f), (dialog.Layer.GetBoundingClientRect().X, dialog.Layer.GetBoundingClientRect().Y, dialog.Layer.GetBoundingClientRect().Width, dialog.Layer.GetBoundingClientRect().Height));
        editor.Click(editor.Middle(part.Button));
        Assert.Same(buttons[0], editor.Document.FocusedElement);
        Assert.False(closed.IsCompleted);
        Assert.Same(dialog, Assert.Single(editor.View.Dialogs.Open));
    }

    [Fact]
    public void TabKeepsTheFocusInsideTheDialog()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        editor.Frame();
        // Without a dialog Tab goes through the shell: the top bar's buttons (enabled by the part), the pane's field.
        editor.Key("Tab", "Tab");
        Assert.Same(editor.View.Button(EditorCommand.NewMod), editor.Document.FocusedElement);
        part.Field.Focus();
        var dialog = editor.View.Dialogs.Create("Fields");
        var name = dialog.Content.Append("input");
        var ok = dialog.AddButton("OK", "ok", primary: true);
        var cancel = dialog.AddButton("Cancel", null);
        dialog.Show();
        Assert.Same(name, editor.Document.FocusedElement);
        var seen = new List<UiElement?>();
        for (var i = 0; i < 6; i++)
        {
            editor.Key("Tab", "Tab");
            seen.Add(editor.Document.FocusedElement);
        }
        Assert.Equal([ok, cancel, name, ok, cancel, name], seen);
        editor.Key("Tab", "Tab", KeyModifiers.Shift);
        Assert.Same(cancel, editor.Document.FocusedElement);
        editor.Key("Tab", "Tab", KeyModifiers.Shift);
        Assert.Same(ok, editor.Document.FocusedElement);
    }

    [Fact]
    public void APressOutsideTheBoxLeavesTheFocusWhereItIs()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        _ = editor.View.Dialogs.Ask("Question", "Sure?", ("OK", "ok"));
        var ok = editor.View.Dialogs.Top!.Buttons.ElementChildren.Single();
        Assert.Same(ok, editor.Document.FocusedElement);
        editor.Click(new Vector2(20, 300));
        Assert.Same(ok, editor.Document.FocusedElement);
        // A press on the box's text focuses the box, still inside.
        editor.Click(editor.Middle(editor.View.Dialogs.Top!.Content));
        Assert.Same(editor.View.Dialogs.Top!.Element, editor.Document.FocusedElement);
    }

    [Fact]
    public void EscapeCancelsAndTheFocusGoesBack()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        part.Button.Focus();
        var closed = editor.View.Dialogs.Ask("Question", "Sure?", ("OK", "ok"), ("Cancel", null));
        var layer = editor.View.Dialogs.Top!.Layer;
        Assert.False(editor.Key("Escape", "Escape"));
        Assert.True(closed.IsCompleted);
        Assert.Null(closed.Result);
        Assert.Null(layer.Parent);
        Assert.Null(editor.View.Dialogs.Top);
        Assert.False(editor.View.Shell.Inert);
        Assert.Same(part.Button, editor.Document.FocusedElement);
        // Escape without a dialog, or with a modifier, closes nothing.
        Assert.True(editor.Key("Escape", "Escape"));
        _ = editor.View.Dialogs.Ask("Question", "Sure?", ("OK", "ok"));
        editor.Key("Escape", "Escape", KeyModifiers.Shift);
        editor.Key("Escape", "Escape", KeyModifiers.Control);
        Assert.NotNull(editor.View.Dialogs.Top);
    }

    [Fact]
    public void AButtonClosesTheDialogWithItsValue()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        var closed = editor.View.Dialogs.Ask("Question", "Sure?", ("Yes", "yes"), ("No", "no"));
        editor.Click(editor.View.Dialogs.Top!.Buttons.ElementChildren.Last());
        Assert.Equal("no", Done(closed));
        Assert.Null(editor.View.Dialogs.Top);
        // A cancel button is null, as Escape; Enter on the focused button clicks it.
        closed = editor.View.Dialogs.Ask("Question", "Sure?", ("Cancel", null), ("OK", "ok"));
        Assert.Equal([false, true], editor.View.Dialogs.Top!.Buttons.ElementChildren.Select(b => b.HasClass("primary")));
        editor.Key("Enter", "Enter");
        Assert.True(closed.IsCompleted);
        Assert.Null(closed.Result);
        // Nothing had the focus before: nothing has it after.
        Assert.Null(editor.Document.FocusedElement);
    }

    [Fact]
    public void WhileADialogIsOpenOnlyTheZoomKeysAreCommands()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        _ = editor.View.Dialogs.Ask("Question", "Sure?", ("OK", "ok"));
        editor.Key("s", "KeyS", KeyModifiers.Control);
        editor.Key("Delete", "Delete");
        Assert.Empty(part.Requests);
        editor.Key("=", "Equal", KeyModifiers.Control);
        Assert.True(editor.View.Zoom > 1);
        editor.Key("Escape", "Escape");
        editor.Key("s", "KeyS", KeyModifiers.Control);
        Assert.Single(part.Requests);
    }

    [Fact]
    public void ADialogOverADialogLeavesTheLowerOneInertUntilItCloses()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        part.Field.Focus();
        var lower = editor.View.Dialogs.Create("Lower");
        var field = lower.Content.Append("input");
        lower.AddButton("OK", "ok");
        lower.Show();
        Assert.Same(field, editor.Document.FocusedElement);
        var upper = editor.View.Dialogs.Create("Upper");
        upper.AddButton("Close", "close");
        upper.Show();
        Assert.Equal([lower, upper], editor.View.Dialogs.Open);
        Assert.True(lower.Layer.Inert);
        Assert.False(upper.Layer.Inert);
        for (var i = 0; i < 3; i++)
            editor.Key("Tab", "Tab");
        Assert.True(editor.Document.FocusedElement!.IsDescendantOf(upper.Element));
        editor.Key("Escape", "Escape");
        Assert.Null(Done(upper.Closed));
        Assert.False(lower.Closed.IsCompleted);
        Assert.False(lower.Layer.Inert);
        Assert.True(editor.View.Shell.Inert);
        Assert.Same(field, editor.Document.FocusedElement);
        editor.Key("Escape", "Escape");
        Assert.True(lower.Closed.IsCompleted);
        Assert.False(editor.View.Shell.Inert);
        Assert.Same(part.Field, editor.Document.FocusedElement);
    }

    [Fact]
    public void TheFocusGoesToAnAutofocusElementElseTheFirstFocusableElseTheBox()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        var dialog = editor.View.Dialogs.Create("Auto");
        dialog.Content.Append("input");
        var auto = dialog.Content.Append("input");
        auto.SetAttribute("autofocus", "");
        dialog.AddButton("OK", "ok");
        dialog.Show();
        Assert.Same(auto, editor.Document.FocusedElement);
        dialog.Close("x");
        var plain = editor.View.Dialogs.Create("Words only");
        plain.Content.Append("p", text: "Nothing to focus.");
        var skipped = plain.Content.Append("span", text: "Not in the tab order");
        skipped.SetAttribute("tabindex", "-1");
        plain.Show();
        Assert.Same(plain.Element, editor.Document.FocusedElement);
    }

    [Fact]
    public void TheFocusGoesBackOnlyWhenItWasInTheDialog()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var part = editor.View.Part<RecordingPart>();
        part.Button.Focus();
        var dialog = editor.View.Dialogs.Create("Q");
        dialog.AddButton("OK", "ok");
        dialog.Show();
        // Something outside the dialog took the focus meanwhile (here a script).
        editor.View.Shell.Inert = false;
        editor.Frame();
        part.Field.Focus();
        dialog.Close("ok");
        Assert.Same(part.Field, editor.Document.FocusedElement);
    }

    [Fact]
    public void ADialogIsShownOnceAndClosedOnce()
    {
        using var editor = new EditorHarness(parts: typeof(RecordingPart));
        var dialog = editor.View.Dialogs.Create("Q");
        Assert.False(dialog.IsOpen);
        dialog.Close("early");
        Assert.False(dialog.Closed.IsCompleted);
        dialog.Show();
        Assert.True(dialog.IsOpen);
        Assert.Throws<InvalidOperationException>(dialog.Show);
        dialog.Close("first");
        dialog.Close("second");
        Assert.Equal("first", Done(dialog.Closed));
        Assert.Throws<InvalidOperationException>(dialog.Show);
        // Each dialog's title has an id of its own.
        var a = editor.View.Dialogs.Create("A");
        var b = editor.View.Dialogs.Create("B");
        Assert.NotEqual(a.Element.GetAttribute("aria-labelledby"), b.Element.GetAttribute("aria-labelledby"));
    }
}
