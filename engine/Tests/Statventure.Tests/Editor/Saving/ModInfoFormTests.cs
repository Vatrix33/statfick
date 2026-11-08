using Statventure.Audio;
using Statventure.Editor.Form;
using Statventure.Editor.Saving;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Records;
using Statventure.UI.Dom;
using static Statventure.Tests.Editor.Checks.CheckTesting;
using static Statventure.Tests.Editor.Form.FormTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;
using static Statventure.Tests.Editor.Saving.SavingTesting;

namespace Statventure.Tests.Editor.Saving;

/// <summary>
/// The mod's info in a form of its own (M11-15, G1, the manager's decision Q1 of 2026-11-08; Docs/Editor.md, "How it works",
/// step 11) and the form pane's hook it is built on (<see cref="IEntryForm"/>): the four fields, each committing as one
/// step of the entry's history, a field's own typing undone first, read-only when the pack doesn't take changes, the
/// generic form for any other entry.
/// </summary>
public class ModInfoFormTests
{
    static EditorHarness Mod(Scratch scratch, ModInfo info, float height = 640)
    {
        var mod = scratch.Mod(w =>
        {
            w.AddRecord(ModInfo.Path, info);
            w.AddRecord("audio/rooms/crypt", new RoomSpec(2.5, 0.5, 0.25));
        });
        var editor = scratch.Editor(mod, height: height);
        editor.Session.Select(editor.Session.At(ModInfo.Path));
        editor.Frame();
        return editor;
    }

    static readonly ModInfo Halls = new() { Name = "Brighter Halls", Author = "Vatrix", Version = "1.0", Description = "Every hall echoes longer." };

    static FormPart FormPane(EditorHarness editor) => editor.View.Part<FormPart>();

    [Fact]
    public void TheModsInfoShowsInAFormOfItsOwn()
    {
        using var scratch = new Scratch();
        using var editor = Mod(scratch, Halls);
        var form = editor.ModInfoForm();
        Assert.Same(form, FormPane(editor).OwnForm);
        Assert.Null(FormPane(editor).Form);
        var pane = editor.View.Pane(EditorPane.Form);
        Assert.Empty(pane.Descendants().Where(e => e.HasClass("form-tabs") || e.HasClass("form-rows") || e.HasClass("form-head")));
        Assert.Equal("Mod info" + ModInfo.Path, editor.Q(".mod-info-head").TextContent);
        Assert.Equal("What players will see of this mod in a mod list (the game doesn't show it yet). It is saved with the mod.", editor.Q(".mod-info-about").TextContent);
        Assert.Equal(["Name", "Author", "Version", "Description"], pane.Descendants().Where(e => e.HasClass("mod-info-label")).Select(e => e.TextContent));
        Assert.Equal(("input", "text"), (form.Field("Name").Tag, form.Field("Name").InputType));
        Assert.Equal("textarea", form.Field("Description").Tag);
        Assert.Equal(["Brighter Halls", "Vatrix", "1.0", "Every hall echoes longer."], ModInfoForm.Fields.Select(f => form.Field(f.Field).Value));
        Assert.All(ModInfoForm.Fields, f => Assert.False(form.Field(f.Field).IsDisabled));
        // Any other entry: the generic form.
        editor.Session.Select(editor.Session.At("audio/rooms/crypt"));
        editor.Frame();
        Assert.Null(FormPane(editor).OwnForm);
        Assert.NotNull(FormPane(editor).Form);
        Assert.Empty(pane.Descendants().Where(e => e.HasClass("mod-info")));
        // A mod.info record elsewhere isn't the mod's info.
        var elsewhere = editor.Session.Add("notes/info", RecordDraft.From(Halls));
        editor.Session.Select(elsewhere);
        editor.Frame();
        Assert.Null(FormPane(editor).OwnForm);
        // Nor another kind at mod/info.
        Assert.False(editor.ModInfoForm().Shows(EditorEntry.ForRecord(ModInfo.Path, new RoomSpec(1, 0, 0), EntrySource.Pack)));
    }

    [Fact]
    public void EachFieldCommitsAsOneStepOfTheEntrysHistory()
    {
        using var scratch = new Scratch();
        using var editor = Mod(scratch, Halls);
        var info = editor.Session.At(ModInfo.Path);
        var form = editor.ModInfoForm();
        editor.Enter(form.Field("Name"), "Brightest Halls");
        Assert.Equal(Halls with { Name = "Brightest Halls" }, info.Build!.Value);
        Assert.Equal(1, info.HistoryPosition);
        Assert.True(info.IsChanged);
        // The same text again: no step.
        editor.Enter(form.Field("Name"), "Brightest Halls");
        Assert.Equal(1, info.HistoryPosition);
        // The description commits as it loses the focus (Enter is a new line there).
        editor.Type(form.Field("Description"), "Halls echo.");
        editor.Key("Enter", "Enter");
        editor.TypeKeys("Longer.");
        Assert.Equal(1, info.HistoryPosition);
        editor.Press(form.Field("Version"));
        Assert.Equal(Halls with { Name = "Brightest Halls", Description = "Halls echo.\nLonger." }, info.Build!.Value);
        Assert.Equal(2, info.HistoryPosition);
        // Undo per entry, the fields following the draft.
        editor.Press(editor.View.Button(EditorCommand.Undo)!);
        Assert.Equal("Every hall echoes longer.", form.Field("Description").Value);
        editor.Press(editor.View.Button(EditorCommand.Undo)!);
        Assert.Equal("Brighter Halls", form.Field("Name").Value);
        Assert.False(info.IsChanged);
        editor.Press(editor.View.Button(EditorCommand.Redo)!);
        Assert.Equal("Brightest Halls", form.Field("Name").Value);
        // Typing not committed survives a change elsewhere in the draft.
        editor.Type(form.Field("Author"), "Someone");
        editor.Session.Change(info, "Version", "2.0");
        Assert.Equal("2.0", form.Field("Version").Value);
        Assert.Equal("Someone", form.Field("Author").EditorText);
        // Saved, as any entry.
        editor.Key("Enter", "Enter");
        editor.Key("s", "KeyS", KeyModifiers.Control);
        using var pack = PackFile.Open(editor.Session.Pack!.File);
        Assert.Equal(Halls with { Name = "Brightest Halls", Author = "Someone", Version = "2.0" }, RecordSerializer.FromBinary<ModInfo>(pack.Read(ModInfo.Path)));
    }

    /// <summary>P13: a field's own typing is undone and redone first (the field's own undo), then the entry's history.</summary>
    [Fact]
    public void AFieldsOwnTypingIsUndoneFirst()
    {
        using var scratch = new Scratch();
        using var editor = Mod(scratch, Halls);
        var info = editor.Session.At(ModInfo.Path);
        var form = editor.ModInfoForm();
        editor.Enter(form.Field("Version"), "1.1");
        var author = form.Field("Author");
        editor.Type(author, "Someone");
        Assert.True(form.HasTypingToUndo(author));
        Assert.False(form.HasTypingToUndo(form.Field("Name")));
        Assert.False(form.HasTypingToUndo(null));
        editor.Undo();
        Assert.Equal("Vatrix", author.EditorText);
        Assert.Equal(1, info.HistoryPosition);
        Assert.False(form.HasTypingToUndo(author));
        Assert.True(form.HasTypingToRedo(author));
        Assert.False(form.HasTypingToRedo(form.Field("Name")));
        editor.Redo();
        Assert.Equal("Someone", author.EditorText);
        Assert.Equal(1, info.HistoryPosition);
        Assert.True(form.HasTypingToRedo(author));
        editor.Undo();
        editor.Undo();
        Assert.Equal(0, info.HistoryPosition);
        Assert.Equal("1.0", form.Field("Version").Value);
        Assert.Same(author, editor.Document.FocusedElement);
        // The field's own undone typing stays its to redo (its draft value didn't change).
        Assert.True(form.HasTypingToRedo(author));
        editor.Redo();
        Assert.Equal("Someone", author.EditorText);
        Assert.Equal(0, info.HistoryPosition);
    }

    /// <summary>
    /// The field's own undos are counted as FormView counts them: typing starts the count again, a redo of its own counts
    /// down, and the draft's value there changing (an undo of the entry) starts it again.
    /// </summary>
    [Fact]
    public void TheFieldsOwnUndosAreCounted()
    {
        using var scratch = new Scratch();
        using var editor = Mod(scratch, Halls);
        var form = editor.ModInfoForm();
        var version = form.Field("Version");
        // Two typings, the second back to the draft's text: undone twice and redone twice, nothing of its own left to redo.
        editor.Type(version, "a");
        editor.Type(version, "1.0");
        editor.Undo();
        Assert.Equal("a", version.EditorText);
        editor.Undo();
        Assert.Equal("1.0", version.EditorText);
        editor.Redo();
        Assert.Equal("a", version.EditorText);
        Assert.True(form.HasTypingToRedo(version));
        editor.Redo();
        Assert.Equal("1.0", version.EditorText);
        Assert.False(form.HasTypingToRedo(version));
        // Undone, then typed: the count starts again.
        editor.Type(version, "x");
        editor.Undo();
        Assert.True(form.HasTypingToRedo(version));
        editor.Type(version, "1.0");
        Assert.False(form.HasTypingToRedo(version));
        // A field's own undo, then the entry's undo by the button changing the field: nothing of its own to redo.
        editor.Enter(version, "1.1");
        editor.Type(version, "y");
        editor.Undo();
        Assert.Equal("1.1", version.EditorText);
        Assert.True(form.HasTypingToRedo(version));
        editor.Press(editor.View.Button(EditorCommand.Undo)!);
        Assert.Equal("1.0", version.Value);
        Assert.False(form.HasTypingToRedo(version));
    }

    [Fact]
    public void TheModsInfoIsReadOnlyWhenThePackDoesntTakeChanges()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord(ModInfo.Path, Halls);
            AddUnreadable(w, "parts/arm", "unknown kind");
        });
        using var editor = scratch.Editor(mod);
        editor.Session.Select(editor.Session.At(ModInfo.Path));
        editor.Frame();
        var form = editor.ModInfoForm();
        Assert.All(ModInfoForm.Fields, f => Assert.True(form.Field(f.Field).IsDisabled));
        Assert.Equal("read-only", editor.Q(".mod-info-state").TextContent);
        Assert.Equal("Brighter Halls", form.Field("Name").Value);
    }

    /// <summary>A rename takes the entry out of its form of its own (the generic form shows it), and back.</summary>
    [Fact]
    public void ARenameTakesItInAndOutOfItsForm()
    {
        using var scratch = new Scratch();
        using var editor = Mod(scratch, Halls);
        var info = editor.Session.At(ModInfo.Path);
        editor.Session.Rename(info, "notes/info");
        editor.Frame();
        Assert.Null(FormPane(editor).OwnForm);
        Assert.NotNull(FormPane(editor).Form);
        Assert.Equal("notes/info", editor.Q(".form-path").TextContent);
        editor.Session.Rename(info, ModInfo.Path);
        editor.Frame();
        Assert.Same(editor.ModInfoForm(), FormPane(editor).OwnForm);
        Assert.Equal("Brighter Halls", editor.ModInfoForm().Field("Name").Value);
        // A rename to another path of the generic form keeps the generic form as it was (its head follows).
        var crypt = editor.Session.At("audio/rooms/crypt");
        editor.Session.Select(crypt);
        editor.Frame();
        var rows = editor.Q(".form-rows");
        editor.Session.Rename(crypt, "audio/rooms/vault");
        editor.Frame();
        Assert.Same(rows, editor.Q(".form-rows"));
        Assert.Equal("audio/rooms/vault", editor.Q(".form-path").TextContent);
    }
}

/// <summary>The form pane's hook (<see cref="IEntryForm"/>) with a form of the tests' own: what the form pane asks of it and when.</summary>
public class EntryFormHookTests
{
    /// <summary>A form of its own for entries under <c>notes/own/</c>: records what it is asked, says it has typing to undo while asked to.</summary>
    public sealed class OwnForm : EditorPart, IEntryForm
    {
        public readonly List<string> Calls = [];
        public bool Typing;
        public UiElement? Element;

        protected internal override void Attach(UiElement? element)
        {
        }

        public bool Shows(EditorEntry entry) => entry.Path.StartsWith("notes/own/", StringComparison.Ordinal);

        public void Show(EditorEntry entry, UiElement element)
        {
            Calls.Add("show " + entry.Path);
            Element = element;
            var field = element.Append("input", "own-field");
            field.SetAttribute("type", "text");
        }

        public void Update() => Calls.Add("update");

        public bool HasTypingToUndo(UiElement? element)
        {
            Calls.Add("undo? " + element?.Tag);
            return Typing;
        }

        public bool HasTypingToRedo(UiElement? element)
        {
            Calls.Add("redo? " + element?.Tag);
            return Typing;
        }
    }

    /// <summary>A second form of its own for the same entries, after the first in the parts' order.</summary>
    public sealed class OwnFormLater : EditorPart, IEntryForm
    {
        protected internal override void Attach(UiElement? element)
        {
        }

        public bool Shows(EditorEntry entry) => entry.Path.StartsWith("notes/own/", StringComparison.Ordinal);

        public void Show(EditorEntry entry, UiElement element) => element.Append("p", "later", "later");

        public void Update()
        {
        }

        public bool HasTypingToUndo(UiElement? element) => false;

        public bool HasTypingToRedo(UiElement? element) => false;
    }

    [Fact]
    public void TheFirstFormInThePartsOrderShowsIt()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord("notes/own/a", Patch()));
        using var editor = new EditorHarness(parts: [typeof(OwnFormLater), typeof(OwnForm)]);
        editor.Session.Open(mod);
        editor.Session.Select(editor.Session.At("notes/own/a"));
        editor.Frame();
        Assert.Same(editor.View.Part<OwnForm>(), editor.View.Part<FormPart>().OwnForm);
        Assert.Empty(editor.View.Pane(EditorPane.Form).Descendants().Where(e => e.HasClass("later")));
    }

    [Fact]
    public void AnEntryAPartsFormShowsIsShownThere()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord("notes/own/a", Patch());
            w.AddRecord("notes/plain", Patch());
        });
        using var editor = new EditorHarness(parts: typeof(OwnForm));
        editor.Session.PlayerFolder = scratch.Player;
        editor.Session.Open(mod);
        var own = editor.View.Part<OwnForm>();
        var pane = editor.View.Part<FormPart>();
        var a = editor.Session.At("notes/own/a");
        editor.Session.Select(a);
        editor.Frame();
        Assert.Equal(["show notes/own/a"], own.Calls);
        Assert.Same(own, pane.OwnForm);
        // Its element is the form pane's, emptied: nothing of the generic form.
        Assert.Contains(editor.View.Pane(EditorPane.Form), own.Element!.Ancestors());
        Assert.True(own.Element!.HasClass("form-own"));
        Assert.Empty(editor.View.Pane(EditorPane.Form).Descendants().Where(e => e.HasClass("form-head") || e.HasClass("form-tabs")));
        // A change of the entry: Update.
        editor.Session.Change(a, "MaxVoices", 3);
        Assert.Equal(["show notes/own/a", "update"], own.Calls);
        // Undo by the key asks it first: typing of its own keeps the entry's history as it is.
        var field = editor.Q(".own-field");
        field.Focus();
        own.Typing = true;
        editor.Undo();
        Assert.Equal(1, a.HistoryPosition);
        Assert.Equal("undo? input", own.Calls[^1]);
        own.Typing = false;
        editor.Undo();
        Assert.Equal(0, a.HistoryPosition);
        editor.Redo();
        Assert.Equal(1, a.HistoryPosition);
        Assert.Equal("update", own.Calls[^1]);
        own.Typing = true;
        editor.Undo();
        editor.Redo();
        Assert.Equal(1, a.HistoryPosition);
        Assert.Contains("redo? input", own.Calls);
        // The top bar's buttons act on the entry (not asked).
        var asked = own.Calls.Count;
        editor.Click(editor.View.Button(EditorCommand.Undo)!);
        Assert.Equal(0, a.HistoryPosition);
        Assert.DoesNotContain(own.Calls.Skip(asked), c => c.StartsWith("undo?"));
        // Another entry: the generic form, the own form no longer told.
        editor.Session.Select(editor.Session.At("notes/plain"));
        editor.Frame();
        Assert.Null(pane.OwnForm);
        Assert.NotNull(pane.Form);
        var calls = own.Calls.Count;
        editor.Session.Change(editor.Session.At("notes/plain"), "MaxVoices", 3);
        Assert.Equal(calls, own.Calls.Count);
        // Renamed into its folder: shown there again.
        editor.Session.Rename(editor.Session.At("notes/plain"), "notes/own/b");
        editor.Frame();
        Assert.Same(own, pane.OwnForm);
        Assert.Equal("show notes/own/b", own.Calls[^1]);
    }
}
