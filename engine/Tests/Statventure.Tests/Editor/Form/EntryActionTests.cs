using Statventure.Editor.Form;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Records;
using Statventure.Tests.Records;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor.Form;

/// <summary>
/// New, Duplicate, Rename, Remove and Revert through their dialogs (M11-7, P14, P15, P7): each asks first and
/// does nothing when cancelled; names are checked in the kind's folder by its rule before anything changes; none is
/// undone by Undo; F2 and Delete ask too, Delete only from outside the form's rows.
/// </summary>
public class EntryActionTests
{
    static readonly Creature Grim = new() { Name = "Grim", Speed = 2 };

    static EditorHarness Open(params EditorEntry[] entries) =>
        FormTesting.Open(entries.Length > 0 ? entries : [FormTesting.Entry("creatures/_grim", Grim)]);

    static UiElement Head(EditorHarness editor, string action) => editor.Document.QuerySelector($".form-head [data-action={action}]")!;

    [Fact]
    public void NewAsksForAKindAndANameInItsFolder()
    {
        using var editor = Open();
        var made = editor.Ask(a => a.New());
        Assert.Equal("New entry", editor.View.Dialogs.Top!.Element.ElementChildren.First().TextContent);
        var kinds = editor.DialogField("select");
        Assert.Equal(editor.Session.NewKinds().Select(k => $"{k.DisplayName} ({k.Name})"), kinds.Options.Select(o => o.TextContent));
        // The selected entry's kind first, its folder named.
        Assert.Equal("Creature (test.creature)", kinds.SelectedOption!.TextContent);
        var name = editor.DialogField("input");
        Assert.Same(name, editor.Document.FocusedElement);
        Assert.Equal("Name in creatures", name.Parent!.ElementChildren.First().TextContent);
        editor.Press(editor.DialogButton("Create"));
        Assert.Equal("Give the entry a name.", editor.DialogProblem());
        editor.Enter(name, "Bold one");
        Assert.Equal("\"Bold one\" can't name a creature in creatures.", editor.DialogProblem());
        editor.Enter(name, "Grim");
        Assert.Equal("That can't be its path: there is already an entry at creatures/_grim.", editor.DialogProblem());
        Assert.False(made.IsCompleted);
        Assert.Single(editor.Session.Entries);
        editor.Enter(name, "Bold");
        var entry = editor.Done(made)!;
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Equal("creatures/_bold", entry.Path);
        Assert.Equal(EntrySource.New, entry.Source);
        Assert.Same(entry, editor.Session.Selected);
        // The kind's blank draft: its required name not given, the rest the defaults.
        Assert.Same(DraftValue.Missing, entry.Draft!["name"]);
        Assert.Equal(RecordSerializer.ToBinary(new Creature { Name = "x" }), RecordSerializer.ToBinary((Creature)entry.Draft.With("name", DraftValue.Of("x")).Build().Value!));
        // Its required fields not given show as problems.
        Assert.Equal(["needs a value"], editor.Row("$.name").Problems());
        Assert.Equal("creatures/_bold", editor.Document.QuerySelector(".form-path")!.TextContent);
    }

    [Fact]
    public void NewTakesAWholePathForAKindWithoutAFolder()
    {
        using var editor = Open();
        var made = editor.Ask(a => a.New());
        editor.Choose(editor.DialogField("select"), "Kinds (test.kinds)");
        var name = editor.DialogField("input");
        Assert.Equal("Path", name.Parent!.ElementChildren.First().TextContent);
        editor.Enter(name, "Sets/One");
        Assert.StartsWith("That can't be its path: 'S' isn't allowed", editor.DialogProblem());
        editor.Enter(name, "creatures/_grim/x");
        Assert.Equal("That can't be its path: creatures/_grim is an entry, so it can't be a folder.", editor.DialogProblem());
        editor.Choose(editor.DialogField("select"), "Body part (test.part)");
        Assert.Equal("Name in parts", name.Parent!.ElementChildren.First().TextContent);
        editor.Choose(editor.DialogField("select"), "Kinds (test.kinds)");
        editor.Enter(name, "sets/one");
        Assert.Equal("sets/one", editor.Done(made)!.Path);
        Assert.Equal("Kinds", editor.Session.Selected!.KindName);
    }

    [Fact]
    public void CancellingAsksNothingMore()
    {
        using var editor = Open();
        var grim = editor.Session.Entries[0];
        var made = editor.Ask(a => a.New());
        editor.Press(editor.DialogButton("Cancel"));
        Assert.Null(editor.Done(made));
        var copy = editor.Ask(a => a.Duplicate(grim));
        editor.Key("Escape", "Escape");
        Assert.Null(editor.Done(copy));
        var renamed = editor.Ask(a => a.Rename(grim));
        editor.Press(editor.DialogButton("Cancel"));
        Assert.False(editor.Done(renamed));
        var removed = editor.Ask(a => a.Remove(grim));
        editor.Press(editor.DialogButton("Cancel"));
        Assert.False(editor.Done(removed));
        Assert.Equal(["creatures/_grim"], editor.Session.Entries.Select(e => e.Path));
        Assert.False(editor.Session.HasUnsavedWork);
        // Nothing to ask while entries can't be added or changed.
        editor.Session.Writable = false;
        Assert.Null(editor.Done(editor.Ask(a => a.New())));
        Assert.Null(editor.Done(editor.Ask(a => a.Duplicate(grim))));
        Assert.False(editor.Done(editor.Ask(a => a.Rename(grim))));
        Assert.False(editor.Done(editor.Ask(a => a.Remove(grim))));
        Assert.Null(editor.View.Dialogs.Top);
    }

    [Fact]
    public void DuplicateCopiesTheDraftAsItIsNow()
    {
        using var editor = Open(FormTesting.Entry("creatures/_grim", Grim), FormTesting.Entry("loose/grim", Grim));
        editor.Enter(editor.Row("$.speed").Control("input", "number"), "7");
        editor.Press(Head(editor, "duplicate"));
        var name = editor.DialogField("input");
        Assert.Equal("Grim-copy", name.Value);
        editor.Key("Enter", "Enter");
        var copy = editor.Session.Selected!;
        Assert.Equal("creatures/_grim-copy", copy.Path);
        Assert.Equal(7, ((Creature)copy.Build!.Value!).Speed);
        Assert.False(copy.CanUndo);
        // Again: the next free name.
        editor.Session.Select(editor.Session.Entry("creatures/_grim"));
        editor.Frame();
        editor.Press(Head(editor, "duplicate"));
        Assert.Equal("Grim-copy-2", editor.DialogField("input").Value);
        editor.Press(editor.DialogButton("Duplicate"));
        Assert.Equal("creatures/_grim-copy-2", editor.Session.Selected!.Path);
        // An entry outside its kind's folder takes a whole path.
        editor.Session.Select(editor.Session.Entry("loose/grim"));
        editor.Frame();
        editor.Press(Head(editor, "duplicate"));
        Assert.Equal("Path", editor.DialogField("input").Parent!.ElementChildren.First().TextContent);
        Assert.Equal("loose/grim-copy", editor.DialogField("input").Value);
        editor.Key("Enter", "Enter");
        Assert.Equal("loose/grim-copy", editor.Session.Selected!.Path);
    }

    [Fact]
    public void RenameAsksForTheNewNameAndIsntUndone()
    {
        using var editor = Open();
        var grim = editor.Session.Entries[0];
        editor.Press(editor.Row("$.speed").Control("input", "number"));
        editor.Key("F2", "F2");
        Assert.Equal("Rename entry", editor.View.Dialogs.Top!.Element.ElementChildren.First().TextContent);
        var name = editor.DialogField("input");
        Assert.Equal("Grim", name.Value);
        // The same name: nothing changes.
        editor.Key("Enter", "Enter");
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Equal("creatures/_grim", grim.Path);
        editor.Key("F2", "F2");
        editor.Enter(editor.DialogField("input"), "Gob");
        Assert.Equal("creatures/_gob", grim.Path);
        Assert.Equal("creatures/_gob", editor.Document.QuerySelector(".form-path")!.TextContent);
        editor.Undo();
        Assert.Equal("creatures/_gob", grim.Path);
        // Other parts add to the dialog and act once it is confirmed (step 10's referrers, P19).
        var seen = new List<string?>();
        editor.Actions().Asking += asking =>
        {
            Assert.Equal(EntryAction.Rename, asking.Action);
            Assert.Same(grim, asking.Entry);
            asking.Dialog.Content.Append("p", "referrers", "2 entries refer to it.");
            asking.Confirmed.Add(seen.Add);
        };
        var renamed = editor.Ask(a => a.Rename(grim));
        Assert.Equal("2 entries refer to it.", editor.View.Dialogs.Top!.Content.Descendants().First(e => e.HasClass("referrers")).TextContent);
        editor.Enter(editor.DialogField("input"), "Hob");
        Assert.True(editor.Done(renamed));
        Assert.Equal(["creatures/_hob"], seen);
        // Cancelled, or confirmed with the same name: not renamed, and what other parts added isn't done.
        var cancelled = editor.Ask(a => a.Rename(grim));
        editor.Press(editor.DialogButton("Cancel"));
        Assert.False(editor.Done(cancelled));
        var same = editor.Ask(a => a.Rename(grim));
        editor.Key("Enter", "Enter");
        Assert.False(editor.Done(same));
        Assert.Equal(["creatures/_hob"], seen);
        Assert.Equal("creatures/_hob", grim.Path);
    }

    [Fact]
    public void RemoveAsksAndDeleteAsksOnlyFromOutsideTheRows()
    {
        using var editor = Open(FormTesting.Entry("creatures/_grim", Grim), FormTesting.Entry("creatures/_bold", Grim));
        var grim = editor.Session.Entry("creatures/_grim")!;
        editor.Session.Select(grim);
        editor.Frame();
        // Delete in the form's rows is the controls' (a checkbox, a select): no question.
        editor.Press(editor.Row("$.title").Control("input", "checkbox"));
        editor.Key("Delete", "Delete");
        Assert.Null(editor.View.Dialogs.Top);
        editor.Press(Head(editor, "rename"));
        editor.Press(editor.DialogButton("Cancel"));
        editor.Key("Delete", "Delete");
        Assert.Equal("Remove entry", editor.View.Dialogs.Top!.Element.ElementChildren.First().TextContent);
        Assert.Equal("Remove creatures/_grim? This can't be undone.", editor.View.Dialogs.Top.Content.TextContent);
        editor.Press(editor.DialogButton("Remove"));
        Assert.Null(editor.Session.Entry("creatures/_grim"));
        Assert.Null(editor.Session.Selected);
        Assert.Empty(editor.View.Pane(EditorPane.Form).Descendants().Where(e => e.HasClass("form-view")));
        Assert.Equal([grim], editor.Session.Removed);
        // Nothing selected: F2 and Delete have nothing to act on.
        Assert.False(editor.View.Commands.CanRun(EditorCommand.Remove));
        Assert.False(editor.View.Commands.CanRun(EditorCommand.Rename));
    }

    [Fact]
    public void GameEntriesArentRenamedOrRemovedButTheModsCopiesRevert()
    {
        var game = FormTesting.Entry("creatures/_grim", Grim, EntrySource.Game);
        var copy = FormTesting.Entry("creatures/_grim", Grim with { Speed = 9 });
        var other = FormTesting.Entry("creatures/_hob", Grim, EntrySource.Game);
        using var editor = Open(other, game, copy);
        Assert.Same(other, editor.Session.Selected);
        // A game entry: renaming and removing are off (P7); no revert until it is changed.
        Assert.True(Head(editor, "rename").HasAttribute("disabled"));
        Assert.True(Head(editor, "remove").HasAttribute("disabled"));
        Assert.True(Head(editor, "revert").HasAttribute("hidden"));
        Assert.False(Head(editor, "duplicate").HasAttribute("disabled"));
        editor.Key("F2", "F2");
        Assert.Null(editor.View.Dialogs.Top);
        // Its first change makes the mod's copy, which can go back to the game's.
        editor.Enter(editor.Row("$.speed").Control("input", "number"), "4");
        Assert.False(Head(editor, "revert").HasAttribute("hidden"));
        Assert.False(Head(editor, "revert").HasAttribute("disabled"));
        Assert.Equal("Revert to the game's", Head(editor, "revert").TextContent);
        editor.Press(Head(editor, "revert"));
        Assert.Equal("Revert to the game's", editor.View.Dialogs.Top!.Element.ElementChildren.First().TextContent);
        editor.Press(editor.DialogButton("Cancel"));
        Assert.True(other.IsChanged);
        editor.Press(Head(editor, "revert"));
        editor.Press(editor.DialogButton("Revert"));
        Assert.False(other.IsChanged);
        Assert.False(other.CanUndo);
        Assert.Equal("2", editor.Row("$.speed").Control("input", "number").Value);
        Assert.True(Head(editor, "revert").HasAttribute("hidden"));
        // The pack's copy over a game entry: revert shows the game's again.
        editor.Session.Select(copy);
        editor.Frame();
        Assert.False(Head(editor, "revert").HasAttribute("hidden"));
        Assert.False(Head(editor, "remove").HasAttribute("disabled"));
        var reverted = editor.Ask(a => a.Revert(copy));
        editor.Press(editor.DialogButton("Revert"));
        Assert.True(editor.Done(reverted));
        Assert.Same(game, editor.Session.Selected);
        Assert.Equal("2", editor.Row("$.speed").Control("input", "number").Value);
        Assert.Equal([copy], editor.Session.Removed);
        Assert.False(editor.Done(editor.Ask(a => a.Revert(game))));
        // A copy shown while the pack takes no changes: its Revert is there, disabled.
        editor.Enter(editor.Row("$.speed").Control("input", "number"), "5");
        editor.Session.Writable = false;
        editor.Session.Select(null);
        editor.Session.Select(game);
        editor.Frame();
        Assert.False(Head(editor, "revert").HasAttribute("hidden"));
        Assert.True(Head(editor, "revert").HasAttribute("disabled"));
    }

    [Fact]
    public void RemovingTheCopyOfAGameEntrySaysTheGamesComesBack()
    {
        var game = FormTesting.Entry("creatures/_grim", Grim, EntrySource.Game);
        var copy = FormTesting.Entry("creatures/_grim", Grim with { Speed = 9 });
        using var editor = Open(copy, game);
        var removed = editor.Ask(a => a.Remove(editor.Session.Selected!));
        Assert.Equal("Remove creatures/_grim from this pack? The game's own entry shows again. This can't be undone.", editor.View.Dialogs.Top!.Content.TextContent);
        editor.Press(editor.DialogButton("Remove"));
        Assert.True(editor.Done(removed));
        Assert.Same(game, editor.Session.Entry("creatures/_grim"));
    }
}
