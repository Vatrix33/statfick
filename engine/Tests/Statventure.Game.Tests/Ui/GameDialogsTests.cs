using Statventure.Game.Sounds;
using Statventure.Game.Ui;
using Statventure.Platform;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The confirm and prompt dialogs in place of <c>window.confirm</c> and <c>window.prompt</c> (Docs/Game/Foundation.md,
/// "How it works", step 8): the old alert dialogs' markup, their answers by every way of giving one, the confirm's focus
/// on its action (Q29), the prompt's text selected and its check refusing with the validation bubble.
/// </summary>
[Collection("Game log")]
public class GameDialogsTests
{
    /// <summary>A dialog's answer, given at once by the click or key that answered it (a test fails rather than waits).</summary>
    static T Answered<T>(Task<T> task)
    {
        Assert.True(task.IsCompleted, "the dialog hasn't answered");
        return task.Result;
    }

    [Fact]
    public void AConfirmIsTheOldAlertDialogWithTheQuestionCancelAndTheActionFocused()
    {
        using var h = new GameUiHarness();
        var answer = h.Ui.Dialogs.Confirm("Unlearn Fire Bolt? You will need to learn or purchase it again.", "Unlearn");
        h.Frame();
        var layer = h.Ui.Dialogs.Open!;
        Assert.Same(layer, h.Ui.Windows.Top);
        Assert.Equal("modal-layer map-encounter-layer game-dialog-layer", layer.ClassName);
        Assert.Equal(["div", "section"], layer.ElementChildren.Select(e => e.Tag));
        Assert.Equal("modal-backdrop", layer.ElementChildren.First().ClassName);
        var dialog = h.Q(".game-dialog");
        Assert.Equal(("map-encounter-dialog game-dialog game-confirm", "alertdialog", "true"), (dialog.ClassName, dialog.GetAttribute("role"), dialog.GetAttribute("aria-modal")));
        var question = h.Q(".game-dialog-question");
        Assert.Equal(("p", "Unlearn Fire Bolt? You will need to learn or purchase it again."), (question.Tag, question.TextContent));
        Assert.Equal(question.Id, dialog.GetAttribute("aria-describedby"));
        Assert.Equal([("Cancel", "backup-button game-dialog-cancel"), ("Unlearn", "backup-button game-dialog-accept")],
            h.Q(".game-dialog-actions").ElementChildren.Select(b => (b.TextContent, b.ClassName)));
        Assert.Equal("map-encounter-actions game-dialog-actions", h.Q(".game-dialog-actions").ClassName);
        Assert.Same(h.Q(".game-dialog-accept"), h.Document.FocusedElement);
        Assert.False(answer.IsCompleted);
    }

    [Fact]
    public void AConfirmIsAnsweredByItsButtonsEnterAndEscape()
    {
        using var h = new GameUiHarness();
        var opener = h.Document.Body.Append("button", "opener", "Delete");
        opener.SetAttribute("type", "button");
        h.Frame();
        opener.Focus();
        var yes = h.Ui.Dialogs.Confirm("Delete it?", "Delete");
        h.Frame();
        h.Click(h.Q(".game-dialog-accept"));
        Assert.True(Answered(yes));
        Assert.Null(h.Ui.Dialogs.Open);
        Assert.Null(h.Ui.Windows.Top);
        Assert.Same(opener, h.Document.FocusedElement);
        var no = h.Ui.Dialogs.Confirm("Delete it?", "Delete");
        h.Frame();
        h.Click(h.Q(".game-dialog-cancel"));
        Assert.False(Answered(no));
        // Enter on the focused action (the old native box's default), Escape cancels.
        var entered = h.Ui.Dialogs.Confirm("Delete it?", "Delete");
        h.Frame();
        h.Key("Enter", "Enter");
        Assert.True(Answered(entered));
        var escaped = h.Ui.Dialogs.Confirm("Delete it?", "Delete");
        h.Frame();
        h.Key("Escape", "Escape");
        Assert.False(Answered(escaped));
        Assert.Empty(h.Ui.Windows.Layers);
        // The backdrop does nothing (a question waits for its answer).
        var waiting = h.Ui.Dialogs.Confirm("Delete it?", "Delete");
        h.Frame();
        h.Click(new System.Numerics.Vector2(10, 10));
        Assert.False(waiting.IsCompleted);
        Assert.NotNull(h.Ui.Dialogs.Open);
    }

    [Fact]
    public void APromptIsAFormWithTheQuestionAsTheFieldsLabelItsTextSelected()
    {
        using var h = new GameUiHarness();
        var answer = h.Ui.Dialogs.Prompt("Choose:\n1. Fire Bolt\n2. Frost Lance", "1", "OK", maxLength: 80);
        h.Frame();
        var dialog = h.Q(".game-dialog");
        Assert.Equal(("form", "map-encounter-dialog game-dialog game-prompt", "dialog", true), (dialog.Tag, dialog.ClassName, dialog.GetAttribute("role"), dialog.HasAttribute("novalidate")));
        var label = h.Q(".game-dialog-question");
        Assert.Equal(("label", "Choose:\n1. Fire Bolt\n2. Frost Lance"), (label.Tag, label.TextContent));
        Assert.Equal(label.Id, dialog.GetAttribute("aria-labelledby"));
        var field = h.Q(".game-prompt-field");
        Assert.Equal((field.Id, "text", "1", "80"), (label.GetAttribute("for"), field.GetAttribute("type"), field.Value, field.GetAttribute("maxlength")));
        // Its line breaks shown (pre-line): three lines of the question's 1.25rem × 1.45.
        Assert.InRange(label.GetBoundingClientRect().Height, 3 * 29 - 1, 3 * 29 + 1);
        Assert.Same(field, h.Document.FocusedElement);
        Assert.Equal((0, 1), (field.SelectionStart, field.SelectionEnd));
        Assert.Equal([("Cancel", "button"), ("OK", "submit")], h.Q(".game-dialog-actions").ElementChildren.Select(b => (b.TextContent, b.GetAttribute("type")!)));
        Assert.False(answer.IsCompleted);
    }

    [Fact]
    public void APromptIsAnsweredByOkEnterCancelAndEscape()
    {
        using var h = new GameUiHarness();
        var ok = h.Ui.Dialogs.Prompt("Name your character", "Character 1", "OK");
        h.Frame();
        h.Type("Ser Alrik");
        // The form's submit is the dialog's: kept from the form (the engine logs a submit nobody cancelled).
        var log = GameTesting.CaptureLog(() => h.Click(h.Q(".game-dialog-accept")));
        Assert.DoesNotContain(log, l => l.Contains("was submitted"));
        Assert.Equal("Ser Alrik", Answered(ok));
        var entered = h.Ui.Dialogs.Prompt("Name your character", "Character 2", "OK");
        h.Frame();
        h.Key("Enter", "Enter");
        Assert.Equal("Character 2", Answered(entered));
        // An empty text is an answer like any other unless a check refuses it (window.prompt's).
        var empty = h.Ui.Dialogs.Prompt("Name your character", "", "OK");
        h.Frame();
        h.Key("Enter", "Enter");
        Assert.Equal("", Answered(empty));
        var cancelled = h.Ui.Dialogs.Prompt("Name your character", "Character 3", "OK");
        h.Frame();
        h.Click(h.Q(".game-dialog-cancel"));
        Assert.Null(Answered(cancelled));
        var escaped = h.Ui.Dialogs.Prompt("Name your character", "Character 3", "OK");
        h.Frame();
        h.Key("Escape", "Escape");
        Assert.Null(Answered(escaped));
        Assert.Empty(h.Ui.Windows.Layers);
    }

    [Fact]
    public void ACheckRefusesATextWithTheBubbleAndTheRefusedSoundAndTheDialogStays()
    {
        using var h = new GameUiHarness();
        var answer = h.Ui.Dialogs.Prompt("Name your character", "", "OK", text => text.Trim().Length == 0 ? "Please fill out this field." : null);
        h.Frame();
        h.Sounds.Clear();
        h.Click(h.Q(".game-dialog-accept"));
        h.Frame();
        Assert.False(answer.IsCompleted);
        var field = h.Q(".game-prompt-field");
        Assert.Same(field, h.Document.ValidationBubbleAnchor);
        Assert.Equal("Please fill out this field.", h.Document.ValidationBubble!.TextContent);
        Assert.Same(field, h.Document.FocusedElement);
        Assert.Equal([SoundNames.Refused], h.Sounds);
        // Enter refuses it again; typing takes the problem away; then it is taken.
        h.Key("Enter", "Enter");
        Assert.False(answer.IsCompleted);
        Assert.Equal([SoundNames.Refused, SoundNames.Refused], h.Sounds);
        h.Type("Ser Alrik");
        Assert.Equal("", field.ValidationMessage);
        h.Key("Enter", "Enter");
        Assert.Equal("Ser Alrik", Answered(answer));
    }

    [Fact]
    public void ADialogClosedByAnythingElseIsCancelled()
    {
        using var h = new GameUiHarness();
        var confirm = h.Ui.Dialogs.Confirm("Delete it?", "Delete");
        h.Frame();
        Assert.True(h.Ui.Windows.CloseTop());
        Assert.False(Answered(confirm));
        var prompt = h.Ui.Dialogs.Prompt("Name?", "A", "OK");
        h.Frame();
        h.Ui.Windows.Close(h.Ui.Dialogs.Open!);
        Assert.Null(Answered(prompt));
        Assert.Empty(h.Ui.Windows.Layers);
    }

    [Fact]
    public void DialogsStackAndTheQuestionIsText()
    {
        using var h = new GameUiHarness();
        var under = h.Ui.Dialogs.Confirm("<b>Sell</b> all?", "Sell");
        var over = h.Ui.Dialogs.Prompt("How many?", "2", "OK");
        h.Frame();
        Assert.Equal(2, h.Ui.Windows.Layers.Count);
        Assert.Equal("game-prompt", h.Ui.Dialogs.Open!.ElementChildren.Last().ClassName.Split(' ')[^1]);
        Assert.Equal("<b>Sell</b> all?", h.Document.QuerySelector(".game-confirm .game-dialog-question")!.TextContent);
        Assert.Empty(h.Document.QuerySelectorAll(".game-confirm b"));
        h.Key("Escape", "Escape");
        Assert.Null(Answered(over));
        Assert.False(under.IsCompleted);
        Assert.Same(h.Q(".game-dialog-accept"), h.Document.FocusedElement);
        h.Key("Enter", "Enter");
        Assert.True(Answered(under));
    }
}
