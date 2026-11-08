using Statventure.Audio;
using Statventure.Editor.Browser;
using Statventure.Editor.Saving;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Records;
using static Statventure.Tests.Editor.Checks.CheckTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;
using static Statventure.Tests.Editor.Saving.SavingTesting;

namespace Statventure.Tests.Editor.Saving;

/// <summary>
/// Saving from the view (Docs/Editor.md, "How it works", step 11): Ctrl+S and the button, what the title, the list and the
/// status line say after; a save refused naming the problems; a file changed on disk asked about; Save As's ticks, the
/// browser and the files it refuses; the unsaved-changes question's Save when opening and closing.
/// </summary>
public class SavingViewTests
{
    const string Crypt = "audio/rooms/crypt", Boom = "audio/effects/boom";

    static readonly RoomSpec CryptRoom = new(2.5, 0.5, 0.25);

    static void Save(EditorHarness editor) => editor.Key("s", "KeyS", KeyModifiers.Control);

    static void SaveAs(EditorHarness editor) => editor.Key("S", "KeyS", KeyModifiers.Control | KeyModifiers.Shift);

    /// <summary>A button of the open dialog's content (All, None) clicked.</summary>
    static void Tool(EditorHarness editor, string text) =>
        editor.Click(editor.View.Dialogs.Top!.Content.Descendants().Single(e => e.Tag == "button" && e.TextContent == text));

    static string Lines(EditorDialog dialog) => string.Join("\n", dialog.Content.Descendants().Where(e => e.Tag is "p" or "li").Select(e => e.TextContent));

    /// <summary>The file browser open now, the file <paramref name="file"/> typed and picked (Enter).</summary>
    static void Pick(EditorHarness editor, string file)
    {
        var browser = editor.Browser().Current ?? throw new InvalidOperationException("the browser didn't open");
        browser.PathField.Value = file;
        browser.PathField.Focus();
        editor.Key("Enter", "Enter");
        editor.Frame();
    }

    [Fact]
    public void SaveWritesThePackAndTheMarksAndTheTitleFollow()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord(Crypt, CryptRoom));
        using var editor = scratch.Editor(mod);
        var save = editor.View.Button(EditorCommand.Save)!;
        Assert.False(save.IsDisabled);
        Assert.False(editor.View.Button(EditorCommand.SaveAs)!.IsDisabled);
        // Nothing to save: nothing written (another program's version stays), no question.
        var other = new PackWriter();
        other.AddRecord(Crypt, CryptRoom with { Wet = 1 });
        other.Write(mod);
        var theirs = File.ReadAllBytes(mod);
        Save(editor);
        Assert.Equal("Nothing to save: my-mod.pak is as saved.", editor.View.Status);
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Equal(theirs, File.ReadAllBytes(mod));
        // Nothing unsaved is as good as saved (the unsaved-changes question's Save goes on).
        Assert.True(editor.Run(() => editor.Saving().Save()).Done());
        editor.Session.Open(mod);
        var crypt = editor.Session.At(Crypt);
        editor.Session.Change(crypt, "Seconds", 4.0);
        editor.Session.Select(crypt);
        editor.Frame();
        Assert.Equal("• Statventure Editor dev — my-mod.pak", editor.View.Title);
        Assert.Contains("•", editor.List().Row(crypt)!.TextContent);
        Save(editor);
        Assert.Equal("Saved my-mod.pak.", editor.View.Status);
        Assert.Equal("Statventure Editor dev — my-mod.pak", editor.View.Title);
        Assert.DoesNotContain("•", editor.List().Row(crypt)!.TextContent);
        Assert.Equal((true, Binary(CryptRoom with { Wet = 1, Seconds = 4.0 })), Contents(mod)[Crypt]);
        // The button saves too.
        editor.Session.Change(crypt, "Dark", 0.75);
        editor.Click(save);
        Assert.Equal((true, Binary(CryptRoom with { Wet = 1, Seconds = 4.0, Dark = 0.75 })), Contents(mod)[Crypt]);
        Assert.False(editor.Session.HasUnsavedWork);
    }

    [Fact]
    public void SaveIsThereOnlyForAPackThatTakesChanges()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        Assert.True(editor.View.Button(EditorCommand.Save)!.IsDisabled);
        Assert.True(editor.View.Button(EditorCommand.SaveAs)!.IsDisabled);
        Assert.False(editor.View.Button(EditorCommand.NewMod)!.IsDisabled);
        Assert.False(editor.Run(() => editor.Saving().Save()).Done());
        Assert.False(editor.Run(() => editor.Saving().SaveAs()).Done());
        editor.Session.Open(scratch.Mod(w =>
        {
            w.AddRecord(Crypt, CryptRoom);
            AddUnreadable(w, "parts/arm", "unknown kind");
        }));
        editor.Frame();
        Assert.True(editor.View.Button(EditorCommand.Save)!.IsDisabled);
        Assert.False(editor.View.Button(EditorCommand.SaveAs)!.IsDisabled);
    }

    /// <summary>Ctrl+S saves what a field being typed in shows: it commits first, as the button's click does.</summary>
    [Fact]
    public void CtrlSCommitsTheFieldBeingTypedInFirst()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        editor.Session.NewMod("Halls", "halls.pak");
        editor.Session.Select(editor.Session.At(ModInfo.Path));
        editor.Frame();
        var author = editor.ModInfoForm().Field("Author");
        Form.FormTesting.Type(editor, author, "Vatrix");
        Assert.False(editor.Session.HasUnsavedWork);
        Save(editor);
        Assert.Equal("Saved halls.pak.", editor.View.Status);
        using var pack = PackFile.Open(editor.Session.Pack!.File);
        Assert.Equal(new Statventure.Packs.ModInfo { Name = "Halls", Author = "Vatrix" }, RecordSerializer.FromBinary<Statventure.Packs.ModInfo>(pack.Read(ModInfo.Path)));
        Assert.Same(author, editor.Document.FocusedElement);
        // Save As too: the field commits before the ticks are shown.
        Form.FormTesting.Type(editor, author, "Someone");
        var saveAs = editor.Run(() => editor.Saving().SaveAs());
        editor.Answer("Choose file…");
        Pick(editor, scratch["player/mods/other.pak"]);
        Assert.True(saveAs.Done());
        Assert.Equal("Saved 1 entry as other.pak.", editor.View.Status);
        using var other = PackFile.Open(scratch["player/mods/other.pak"]);
        Assert.Equal("Someone", RecordSerializer.FromBinary<Statventure.Packs.ModInfo>(other.Read(ModInfo.Path)).Author);
    }

    /// <summary>M11-12: the refusal names each entry with its first problem, the first eight, then how many more; the data's own problem too.</summary>
    [Fact]
    public void ASaveRefusedNamesTheEntriesAndTheirProblems()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            for (var i = 0; i < 10; i++)
                w.AddRecord($"audio/effects/e{i}", Patch());
            w.Add("audio/rooms/echo", "Not a room."u8);
        });
        using var editor = scratch.Editor(mod);
        var bytes = File.ReadAllBytes(mod);
        foreach (var i in new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 })
            editor.Session.Change(editor.Session.At($"audio/effects/e{i}"), "MaxVoices", 0);
        var saving = editor.Run(() => editor.Saving().Save());
        var dialog = editor.View.Dialogs.Top!;
        Assert.Equal("Can't save", editor.Asked().Title);
        var lines = dialog.Content.Descendants().Where(e => e.Tag is "p" or "li").Select(e => e.TextContent).ToList();
        Assert.Equal("my-mod.pak can't be saved until these are fixed:", lines[0]);
        Assert.Equal([.. Enumerable.Range(0, 8).Select(i => $"audio/effects/e{i}: allows no voices (at $.maxVoices)"), "and 2 more"], lines.Skip(1));
        Assert.Equal(SavingPart.Listed, dialog.Content.Descendants().Count(e => e.HasClass("saving-problem-entry")));
        Assert.Equal(["OK"], editor.Buttons());
        editor.Answer("OK");
        Assert.False(saving.Done());
        Assert.Equal(bytes, File.ReadAllBytes(mod));
        // Fewer: all named; the data's problem as a line of its own, last (the music's loading runs while no music entry has a problem).
        foreach (var i in Enumerable.Range(0, 10))
            editor.Session.Undo(editor.Session.At($"audio/effects/e{i}"));
        editor.Session.Add("world/cutscenes/intro", RecordDraft.From(Cutscene(Sound("thud"))));
        _ = editor.Run(() => editor.Saving().Save());
        Assert.NotNull(editor.Session.Checks.DataProblem);
        Assert.Equal(["my-mod.pak can't be saved until these are fixed:",
            "world/cutscenes/intro: the sequence intro, track 0 (audio): cue 0 plays the effect thud, which isn't in the game's data (audio/effects/thud) (at $.tracks[0].cues[0].name)",
            editor.Session.Checks.DataProblem!], Lines(editor.View.Dialogs.Top!).Split('\n'));
        editor.Answer("OK");
    }

    /// <summary>P23: a file another program changed asks first: Overwrite writes, Cancel doesn't, Save As goes on to Save As.</summary>
    [Fact]
    public void AFileChangedOnDiskAsksFirst()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord(Crypt, CryptRoom));
        using var editor = scratch.Editor(mod);
        void Theirs(double wet)
        {
            var other = new PackWriter();
            other.AddRecord(Crypt, CryptRoom with { Wet = wet });
            other.Write(mod);
        }
        editor.Session.Change(editor.Session.At(Crypt), "Seconds", 4.0);
        Theirs(1);
        var theirs = File.ReadAllBytes(mod);
        var cancelled = editor.Run(() => editor.Saving().Save());
        Assert.Equal(("The file changed", "my-mod.pak was changed by another program since it was opened here. Overwrite it with the editor's version, save as another file, or cancel?"),
            editor.Asked());
        Assert.Equal(["Overwrite", "Save As", "Cancel"], editor.Buttons());
        editor.Answer("Cancel");
        Assert.False(cancelled.Done());
        Assert.Equal(theirs, File.ReadAllBytes(mod));
        var overwritten = editor.Run(() => editor.Saving().Save());
        editor.Answer("Overwrite");
        Assert.True(overwritten.Done());
        Assert.Equal((true, Binary(CryptRoom with { Seconds = 4.0 })), Contents(mod)[Crypt]);
        // Save As chosen: Save As goes on, and its answer is the save's.
        editor.Session.Change(editor.Session.At(Crypt), "Seconds", 5.0);
        Theirs(0.5);
        var instead = editor.Run(() => editor.Saving().Save());
        editor.Answer("Save As");
        Assert.NotNull(editor.Saving().SaveAsShown);
        editor.Answer("Choose file…");
        Pick(editor, scratch["player/mods/mine.pak"]);
        Assert.True(instead.Done());
        Assert.Equal("mine.pak", editor.Session.Pack!.Name);
        Assert.Equal((true, Binary(CryptRoom with { Seconds = 5.0 })), Contents(scratch["player/mods/mine.pak"])[Crypt]);
    }

    [Fact]
    public void SaveAsTicksTheEntriesThenTheBrowserThenOpensTheNewFile()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord(Boom, Patch());
            w.AddRecord(Crypt, CryptRoom);
            w.Add("notes/a.txt", "A."u8);
        });
        using var editor = scratch.Editor(mod);
        editor.Session.Change(editor.Session.At(Crypt), "Seconds", 4.0);
        editor.Session.Change(editor.Session.At(GameRoom), "Seconds", 6.0);
        var task = editor.Run(() => editor.Saving().SaveAs());
        var dialog = editor.Saving().SaveAsShown!;
        Assert.Equal("Save As", editor.Asked().Title);
        Assert.Contains("Tick the entries of my-mod.pak to save in a new pack; it opens in place of my-mod.pak once saved.", editor.Asked().Text);
        Assert.Equal([Boom, Crypt, GameRoom, "notes/a.txt"], dialog.Choices.Select(c => c.Entry.Path));
        Assert.Equal(dialog.Choices.Select(c => c.Entry), dialog.Ticked);
        Assert.Same(dialog.ChooseButton, editor.Document.FocusedElement);
        Assert.Empty(dialog.NoteLines);
        // Left out with unsaved changes: named as let go; nothing ticked: the button waits.
        editor.Click(dialog.Box(editor.Session.At(Crypt)));
        Assert.Equal(["Left out, their unsaved changes are let go: audio/rooms/crypt."], dialog.NoteLines);
        editor.Click(dialog.Box(editor.Session.At(Boom)));
        Assert.Equal(["Left out, their unsaved changes are let go: audio/rooms/crypt."], dialog.NoteLines);
        Tool(editor, "None");
        Assert.Equal(["Left out, their unsaved changes are let go: audio/rooms/crypt, audio/rooms/hall.", "Tick the entries to save."], dialog.NoteLines);
        Assert.True(dialog.ChooseButton.IsDisabled);
        dialog.Choose();
        Assert.True(dialog.Dialog.IsOpen);
        Tool(editor, "All");
        Assert.Empty(dialog.NoteLines);
        editor.Click(dialog.Box(editor.Session.At("notes/a.txt")));
        // Clicking the row's name ticks it too (a label).
        editor.Click(dialog.Box(editor.Session.At(Boom)).Parent!.ElementChildren.First(e => e.HasClass("saving-path")));
        Assert.Equal([Crypt, GameRoom], dialog.Ticked.Select(e => e.Path));
        editor.Answer("Choose file…");
        var browser = editor.Browser().Current!;
        Assert.Equal(("Save As", "Save", true, "my-mod.pak"), (browser.Request.Title, browser.Request.Button, browser.Request.NewFile, browser.Request.SuggestedName));
        Assert.Equal(FileBrowserPart.PackEndings, browser.Request.Endings);
        // A name without its ending gets .pak.
        Pick(editor, scratch["player/mods/copy"]);
        Assert.True(task.Done());
        Assert.Equal("copy.pak", editor.Session.Pack!.Name);
        Assert.Equal("Statventure Editor dev — copy.pak", editor.View.Title);
        Assert.Equal("Saved 2 entries as copy.pak.", editor.View.Status);
        Assert.Equal([Crypt, GameRoom], Contents(scratch["player/mods/copy.pak"]).Keys.Order(StringComparer.Ordinal));
        Assert.False(File.Exists(scratch["player/mods/copy"]));
        // The name with .pak added that is there already: asked first.
        var again = editor.Run(() => editor.Saving().SaveAs());
        editor.Answer("Choose file…");
        Pick(editor, scratch["player/mods/copy"]);
        Assert.Equal(("Replace the file?", "copy.pak is there already. Replace it?"), editor.Asked());
        editor.Answer("Cancel");
        // The browser again.
        Assert.NotNull(editor.Browser().Current);
        editor.Key("Escape", "Escape");
        Assert.False(again.Done());
        Assert.Equal("copy.pak", editor.Session.Pack!.Name);
    }

    /// <summary>M11-16 and Q4 in the dialog: why the pack is read-only, the reasons beside entries kept unread, a damaged one not offered, the note when one is kept; the pack itself refused as the file.</summary>
    [Fact]
    public void SaveAsFromAPackItCantFullyRead()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord(Crypt, CryptRoom);
            AddUnreadable(w, "parts/arm", "unknown kind");
            AddUnreadable(w, "parts/bad", "damaged");
            AddUnreadable(w, "parts/room", "newer version");
        }, "broken.pak");
        Damage(mod, "parts/bad");
        using var editor = scratch.Editor(mod);
        var task = editor.Run(() => editor.Saving().SaveAs());
        var dialog = editor.Saving().SaveAsShown!;
        Assert.StartsWith("broken.pak is read-only (3 entries can't be read). Tick the entries to keep in a new pack;", editor.Asked().Text);
        // The offered ones by path, then the one not offered.
        Assert.Equal([Crypt, "parts/arm", "parts/room", "parts/bad"], dialog.Choices.Select(c => c.Entry.Path));
        var rows = dialog.Dialog.Content.Descendants().Where(e => e.HasClass("saving-entry")).ToList();
        Assert.Single(rows[0].ElementChildren);
        Assert.False(rows[1].HasClass("not-offered"));
        Assert.Equal("Kept as it is, unread: a record of type test.part, which isn't a record type known here", rows[1].ElementChildren.Last().TextContent);
        Assert.True(rows[3].HasClass("not-offered"));
        Assert.Equal("Not offered: the content doesn't match its hash (damaged)", rows[3].ElementChildren.Last().TextContent);
        Assert.True(dialog.Box(editor.Session.At("parts/bad")).IsDisabled);
        Assert.Equal([Crypt], dialog.Ticked.Select(e => e.Path));
        // Nothing kept unread ticked: no note.
        Assert.Empty(dialog.NoteLines);
        // A damaged one can't be ticked, by code either.
        dialog.Tick(editor.Session.At("parts/bad"), true);
        Assert.False(dialog.Box(editor.Session.At("parts/bad")).Checked);
        editor.Click(dialog.Box(editor.Session.At("parts/bad")));
        Assert.Equal([Crypt], dialog.Ticked.Select(e => e.Path));
        editor.Click(dialog.Box(editor.Session.At("parts/arm")));
        Assert.Equal(["The new pack keeps entries the editor can't read, so it opens read-only."], dialog.NoteLines);
        Tool(editor, "None");
        Tool(editor, "All");
        Assert.Equal([Crypt, "parts/arm", "parts/room"], dialog.Ticked.Select(e => e.Path));
        Assert.False(dialog.Box(editor.Session.At("parts/bad")).Checked);
        editor.Answer("Choose file…");
        // The pack itself can't be saved over: said, the browser again.
        Pick(editor, mod);
        editor.Answer("Replace");
        Assert.Equal(("Can't save there", "broken.pak is the pack open here, which can't be saved over: 3 entries can't be read. Choose another file."), editor.Asked());
        editor.Answer("OK");
        Pick(editor, scratch["player/mods/fixed.pak"]);
        Assert.True(task.Done());
        Assert.Equal("Saved 3 entries as fixed.pak, open read-only: 2 entries can't be read.", editor.View.Status);
    }

    /// <summary>The note names at most eight entries let go, then how many more; a renamed one left out is one too.</summary>
    [Fact]
    public void TheNoteNamesTheEntriesLetGo()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            for (var i = 0; i < 10; i++)
                w.AddRecord($"audio/rooms/r{i}", CryptRoom);
            w.Add("notes/a.txt", "A."u8);
        });
        using var editor = scratch.Editor(mod);
        foreach (var i in Enumerable.Range(0, 9))
            editor.Session.Change(editor.Session.At($"audio/rooms/r{i}"), "Seconds", 4.0);
        editor.Session.Rename(editor.Session.At("notes/a.txt"), "notes/b.txt");
        _ = editor.Run(() => editor.Saving().SaveAs());
        var dialog = editor.Saving().SaveAsShown!;
        dialog.TickAll(false);
        Assert.Equal(["Left out, their unsaved changes are let go: " + string.Join(", ", Enumerable.Range(0, 8).Select(i => $"audio/rooms/r{i}")) + " and 2 more.",
            "Tick the entries to save."], dialog.NoteLines);
        dialog.TickAll(true);
        dialog.Tick(editor.Session.At("notes/b.txt"), false);
        Assert.Equal(["Left out, their unsaved changes are let go: notes/b.txt."], dialog.NoteLines);
        editor.Answer("Cancel");
    }

    /// <summary>A write that fails (here the mods folder made read-only) is said, and nothing changes in the editor.</summary>
    [Fact]
    public void AWriteThatFailsIsSaid()
    {
        // (The folder is made read-only by its Unix mode.)
        if (OperatingSystem.IsWindows())
            return;
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord(Crypt, CryptRoom));
        using var editor = scratch.Editor(mod);
        editor.Session.Change(editor.Session.At(Crypt), "Seconds", 4.0);
        var mode = File.GetUnixFileMode(scratch.Mods);
        File.SetUnixFileMode(scratch.Mods, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            var saving = editor.Run(() => editor.Saving().Save());
            Assert.Equal("Can't save", editor.Asked().Title);
            Assert.StartsWith("my-mod.pak couldn't be written: ", editor.Asked().Text);
            Assert.EndsWith(".", editor.Asked().Text);
            Assert.DoesNotContain("..", editor.Asked().Text);
            editor.Answer("OK");
            Assert.False(saving.Done());
            Assert.True(editor.Session.HasUnsavedWork);
            var saveAs = editor.Run(() => editor.Saving().SaveAs());
            editor.Answer("Choose file…");
            Pick(editor, scratch["player/mods/copy.pak"]);
            Assert.Equal("Can't save", editor.Asked().Title);
            Assert.StartsWith("copy.pak couldn't be written: ", editor.Asked().Text);
            editor.Answer("OK");
            Assert.False(saveAs.Done());
            Assert.Equal("my-mod.pak", editor.Session.Pack!.Name);
            var made = editor.Run(() => editor.Saving().NewMod());
            editor.TypeKeys("Halls");
            editor.Answer("Create");
            editor.Answer("Discard");
            Assert.Equal("Can't make the mod", editor.Asked().Title);
            Assert.StartsWith("halls.pak couldn't be made: ", editor.Asked().Text);
            editor.Answer("OK");
            Assert.Null(made.Done());
            Assert.Equal("my-mod.pak", editor.Session.Pack!.Name);
        }
        finally
        {
            File.SetUnixFileMode(scratch.Mods, mode);
        }
    }

    /// <summary>
    /// An entry damaged in the open file by another program (its index as it was, so nothing changed by the index's hash):
    /// keeping it fails, said in the pack's words; nothing is written.
    /// </summary>
    [Fact]
    public void AnEntryDamagedOnDiskIsSaidWhenSaving()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord(Crypt, CryptRoom);
            w.Add("notes/a.txt", "Stored as it is, damaged later."u8);
        });
        using var editor = scratch.Editor(mod);
        editor.Session.Change(editor.Session.At(Crypt), "Seconds", 4.0);
        Damage(mod, "notes/a.txt");
        var bytes = File.ReadAllBytes(mod);
        Assert.Null(editor.Session.ChangedOnDisk());
        var saving = editor.Run(() => editor.Saving().Save());
        Assert.Equal(("Can't save", "my-mod.pak couldn't be written: the content doesn't match its hash (damaged)."), editor.Asked());
        editor.Answer("OK");
        Assert.False(saving.Done());
        Assert.Equal(bytes, File.ReadAllBytes(mod));
    }

    /// <summary>Save As by its key and its button; Cancel in the dialog ends it with nothing written and no browser.</summary>
    [Fact]
    public void SaveAsByItsKeyAndButtonAndCancelled()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord(Crypt, CryptRoom));
        using var editor = scratch.Editor(mod);
        SaveAs(editor);
        Assert.NotNull(editor.Saving().SaveAsShown);
        editor.Answer("Cancel");
        Assert.Null(editor.Saving().SaveAsShown);
        Assert.Null(editor.Browser().Current);
        editor.Click(editor.View.Button(EditorCommand.SaveAs)!);
        Assert.NotNull(editor.Saving().SaveAsShown);
        editor.Key("Escape", "Escape");
        Assert.Null(editor.Browser().Current);
        var cancelled = editor.Run(() => editor.Saving().SaveAs());
        editor.Answer("Cancel");
        Assert.False(cancelled.Done());
        Assert.Null(editor.Browser().Current);
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Equal(["my-mod.pak"], Directory.GetFiles(scratch.Mods).Select(Path.GetFileName));
    }

    /// <summary>Q5 in the view: the entries ticked refused as the game would see them, named; nothing written.</summary>
    [Fact]
    public void SaveAsRefusedNamesTheProblemsOfTheEntriesTicked()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.AddRecord(Boom, Patch());
            w.AddRecord("world/cutscenes/intro", Cutscene(Sound("boom")));
        });
        using var editor = scratch.Editor(mod);
        var task = editor.Run(() => editor.Saving().SaveAs());
        editor.Click(editor.Saving().SaveAsShown!.Box(editor.Session.At(Boom)));
        editor.Answer("Choose file…");
        Pick(editor, scratch["player/mods/copy.pak"]);
        Assert.Equal("Can't save", editor.Asked().Title);
        Assert.Equal(["The entries ticked can't be saved as copy.pak until these are fixed:",
            "world/cutscenes/intro: the sequence intro, track 0 (audio): cue 0 plays the effect boom, which isn't in the game's data (audio/effects/boom) (at $.tracks[0].cues[0].name)"],
            Lines(editor.View.Dialogs.Top!).Split('\n'));
        editor.Answer("OK");
        Assert.False(task.Done());
        Assert.False(File.Exists(scratch["player/mods/copy.pak"]));
        Assert.Equal("my-mod.pak", editor.Session.Pack!.Name);
    }

    /// <summary>Save As over the open pack (it takes changes) when another program changed it: asked first; another file goes back to the browser.</summary>
    [Fact]
    public void SaveAsOverTheOpenPackChangedOnDiskAsksFirst()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w => w.AddRecord(Crypt, CryptRoom));
        using var editor = scratch.Editor(mod);
        var other = new PackWriter();
        other.AddRecord(Crypt, CryptRoom with { Wet = 1 });
        other.Write(mod);
        var theirs = File.ReadAllBytes(mod);
        var cancelled = editor.Run(() => editor.Saving().SaveAs());
        editor.Answer("Choose file…");
        Pick(editor, mod);
        editor.Answer("Replace");
        Assert.Equal(("The file changed", "my-mod.pak was changed by another program since it was opened here. Overwrite it with the entries ticked, choose another file, or cancel?"),
            editor.Asked());
        Assert.Equal(["Overwrite", "Another file", "Cancel"], editor.Buttons());
        editor.Answer("Another file");
        Assert.NotNull(editor.Browser().Current);
        Pick(editor, mod);
        editor.Answer("Replace");
        editor.Answer("Cancel");
        Assert.False(cancelled.Done());
        Assert.Equal(theirs, File.ReadAllBytes(mod));
        var overwritten = editor.Run(() => editor.Saving().SaveAs());
        editor.Answer("Choose file…");
        Pick(editor, mod);
        editor.Answer("Replace");
        editor.Answer("Overwrite");
        Assert.True(overwritten.Done());
        Assert.Equal((true, Binary(CryptRoom)), Contents(mod)[Crypt]);
    }

    /// <summary>The unsaved-changes question's Save (step 8's Opener.SaveFirst): opening another pack and closing save first; a save refused keeps the pack open and the editor running.</summary>
    [Fact]
    public void TheUnsavedChangesQuestionSavesFirst()
    {
        using var scratch = new Scratch();
        var a = scratch.Mod(w => w.AddRecord(Crypt, CryptRoom), "a.pak");
        var b = scratch.Mod(w => w.AddRecord(Boom, Patch()), "b.pak");
        using var editor = scratch.Editor(a);
        editor.Session.Change(editor.Session.At(Crypt), "Seconds", 4.0);
        var opening = editor.Run(() => editor.Opener().OpenFile(b));
        Assert.Equal(["Save", "Discard", "Cancel"], editor.Buttons());
        editor.Answer("Save");
        Assert.True(opening.Done());
        Assert.Equal("b.pak", editor.Session.Pack!.Name);
        Assert.Equal((true, Binary(CryptRoom with { Seconds = 4.0 })), Contents(a)[Crypt]);
        // Closing: Save saves, then the editor quits.
        editor.Session.Change(editor.Session.At(Boom), "MaxVoices", 3);
        var quits = 0;
        Assert.False(editor.Opener().ConfirmClose(() => quits++));
        editor.Frame();
        Assert.Equal(("Unsaved changes", "b.pak has changes that aren't saved. Discard them and close the editor?"), editor.Asked());
        editor.Answer("Save");
        Assert.Equal(1, quits);
        Assert.False(editor.Session.HasUnsavedWork);
        // A save refused: said, and the editor doesn't quit.
        editor.Session.Change(editor.Session.At(Boom), "MaxVoices", 0);
        Assert.False(editor.Opener().ConfirmClose(() => quits++));
        editor.Frame();
        editor.Answer("Save");
        Assert.Equal("Can't save", editor.Asked().Title);
        editor.Answer("OK");
        Assert.Equal(1, quits);
        Assert.True(editor.Session.HasUnsavedWork);
        Assert.Null(editor.View.Dialogs.Top);
    }
}
