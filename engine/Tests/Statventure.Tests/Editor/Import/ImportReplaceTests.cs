using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Records;
using Statventure.Tests.Render3D;
using static Statventure.Tests.Editor.Import.ImportTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Import;

/// <summary>
/// An import whose path is taken (Docs/Editor.md, "Import (step 12)"; the manager's decisions of 2026-11-07, Q3 and
/// Q4): after the import dialog, the replace question; replacing is not undoable: the pack's entry replaced is listed
/// as removed for saving, a new one goes, a game entry stays under the new one and shows again when it is removed;
/// a record of the same kind with no value that differs is imported again after the same question, one step of its
/// history; Cancel changes nothing; a path the open game pack can't take is said in the dialog.
/// </summary>
public class ImportReplaceTests
{
    [Fact]
    public void ThePacksFileReplacedAfterAskingIsListedAsRemoved()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var old = editor.Session.Entry("notes/readme.txt")!;
        // A pack's file isn't one imported: saving reads it from the pack.
        Assert.Null(old.ImportedFile);
        var file = scratch.Write("downloads/readme.txt", "The new notes.");
        editor.ClickImport(file);
        editor.ImportAt("notes/readme.txt");
        Assert.Equal(("Replace the entry", "notes/readme.txt is already there (a file). Replace it with the imported file? This can't be undone."),
            (editor.Title(), editor.Message()));
        Assert.Equal(["Replace", "Cancel"], editor.Buttons());
        // Cancel: nothing changes.
        editor.Answer("Cancel");
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Same(old, editor.Session.Entry("notes/readme.txt"));
        Assert.False(editor.Session.HasUnsavedWork);

        editor.Session.Select(old);
        editor.ClickImport(file);
        editor.ImportAt("notes/readme.txt");
        editor.Answer("Replace");
        var entry = editor.Session.Entry("notes/readme.txt")!;
        Assert.NotSame(old, entry);
        Assert.Equal((EntrySource.New, "The new notes."), (entry.Source, System.Text.Encoding.UTF8.GetString(entry.ImportedFile!.Value.Span)));
        Assert.Equal([old], editor.Session.Removed);
        Assert.Same(entry, editor.Session.Selected);
        Assert.Equal("Imported readme.txt as notes/readme.txt, replacing what was there.", editor.View.Status);
        Assert.True(editor.Session.HasUnsavedWork);
        Assert.False(entry.CanUndo);
    }

    [Fact]
    public void ARecordOfAnotherKindAndANewEntryAreReplacedTheNewOneNotListed()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor(w => w.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5)));
        var crypt = editor.Session.Entry("audio/rooms/crypt")!;
        // A model over the pack's room.
        var model = scratch.Write("downloads/crypt.glb", File.ReadAllBytes(TestModelPack.SourceOf("blocks")));
        editor.ClickImport(model);
        editor.ImportAt("audio/rooms/crypt");
        Assert.Equal("audio/rooms/crypt is already there (a room). Replace it with the imported model? This can't be undone.", editor.Message());
        editor.Answer("Replace");
        var imported = editor.Session.Entry("audio/rooms/crypt")!;
        Assert.Equal(("Model", EntrySource.New), (imported.KindName, imported.Source));
        Assert.Equal([crypt], editor.Session.Removed);
        // A file over the new model: the new one goes, not listed for saving.
        editor.ClickImport(scratch.Write("downloads/crypt.txt", "x"));
        editor.ImportAt("audio/rooms/crypt");
        Assert.Equal("audio/rooms/crypt is already there (a model). Replace it with the imported file? This can't be undone.", editor.Message());
        editor.Answer("Replace");
        Assert.Equal(EntryContent.File, editor.Session.Entry("audio/rooms/crypt")!.Content);
        Assert.Equal([crypt], editor.Session.Removed);
        Assert.DoesNotContain(imported, editor.Session.Entries);
    }

    [Fact]
    public void AGameEntryReplacedInAModShowsAgainWhenTheNewOneIsRemoved()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var font = editor.Session.Entry("ui/fonts/lato-bold.ttf")!;
        Assert.Equal(EntrySource.Game, font.Source);
        var file = scratch.Write("downloads/Hand.ttf", File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Infrastructure.Golden.GoldenPath("x"))!, "..", "Data", "Fonts", "Lato-Regular.ttf")));
        editor.ClickImport(file);
        Assert.Equal("ui/fonts/hand.ttf", editor.Dialog().PathField.Value);
        editor.ImportAt("ui/fonts/lato-bold.ttf");
        Assert.Equal("ui/fonts/lato-bold.ttf is already there (a file of the game's). Replace it with the imported file? The game's own entry shows again if the new one is removed. This can't be undone.",
            editor.Message());
        editor.Answer("Replace");
        var mine = editor.Session.Entry("ui/fonts/lato-bold.ttf")!;
        Assert.Equal(EntrySource.New, mine.Source);
        Assert.Same(font, editor.Session.Overridden("ui/fonts/lato-bold.ttf"));
        Assert.Empty(editor.Session.Removed);
        editor.Session.Remove(mine);
        Assert.Same(font, editor.Session.Entry("ui/fonts/lato-bold.ttf"));

        // A game record changed in the session (the mod's copy) and replaced by another kind: back to the game's when shown again.
        var room = editor.Session.Entries.First(e => e.Source == EntrySource.Game && e.Path.StartsWith("audio/rooms/", StringComparison.Ordinal));
        editor.Edit(room, "$.wet", 0.125);
        Assert.True(editor.Session.IsModCopy(room));
        editor.ClickImport(scratch.Write("downloads/room.txt", "x"));
        editor.ImportAt(room.Path);
        editor.Answer("Replace");
        Assert.Same(room, editor.Session.Overridden(room.Path));
        editor.Session.Remove(editor.Session.Entry(room.Path)!);
        Assert.Same(room, editor.Session.Entry(room.Path));
        Assert.Same(room.SavedDraft, room.Draft);
        Assert.False(room.CanUndo);
        Assert.False(editor.Session.IsModCopy(room));
    }

    [Fact]
    public void ARecordOfTheSameKindWithNothingDifferentIsImportedAgainAfterAsking()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor(w => w.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5)));
        var crypt = editor.Session.Entry("audio/rooms/crypt")!;
        var json = scratch.Write("downloads/crypt.json", RecordSerializer.ToJson(new RoomSpec(2, 0.5, 0.5)));
        editor.ClickImport(json);
        Assert.Equal("audio/rooms/crypt", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        Assert.Equal(("Import again", "audio/rooms/crypt is already a room. Import crypt.json again over it? Its bulk data comes from the file; no value edited in the form differs."),
            (editor.Title(), editor.Message()));
        Assert.Equal(["Import again", "Cancel"], editor.Buttons());
        editor.Answer("Cancel");
        Assert.False(crypt.CanUndo);
        editor.ClickImport(json);
        editor.Answer("Import");
        editor.Answer("Import again");
        // The same entry, one step of its history (undoable), its values the file's.
        Assert.Same(crypt, editor.Session.Entry("audio/rooms/crypt"));
        Assert.Equal(1, crypt.HistoryPosition);
        Assert.True(crypt.CanUndo);
        Assert.False(crypt.IsChanged);
        Assert.Empty(editor.Session.Removed);
        Assert.Equal("Imported crypt.json again into audio/rooms/crypt.", editor.View.Status);
        Assert.True(editor.Session.Undo(crypt));
    }

    [Fact]
    public void BesideAGamePackInTheDevSetupTheOtherGamePacksPathsCantBeTaken()
    {
        using var scratch = new Scratch();
        // A scratch copy of the repo's packs as the dev setup's game.
        foreach (var name in new[] { "audio.pak", "ui.pak" })
            File.Copy(Path.Combine(Infrastructure.TestPaths.Packs, name), scratch[name]);
        using var session = new EditorSession(new GameData(scratch.Folder.Root, true)) { PlayerFolder = scratch["player"] };
        session.Open(scratch["audio.pak"]);
        Assert.True(session.CanImportFiles);
        var font = session.ImportSourceOf(scratch.Write("downloads/a.ttf", [1]));
        Assert.Equal("That can't be its path: ui/fonts/lato-bold.ttf is in another of the game's packs, which is read-only beside this one.",
            Assert.Throws<ImportException>(() => session.PlanImport(font, "ui/fonts/lato-bold.ttf")).Message);
        // Its own entries can be.
        var room = session.Entries.First(e => e.Source == EntrySource.Pack && e.Path.StartsWith("audio/rooms/", StringComparison.Ordinal));
        Assert.Equal(ImportAction.Replace, session.PlanImport(font, room.Path).Main.Action);
    }

    [Fact]
    public void APlanMustStillFitTheSession()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var session = editor.Session;
        var source = session.ImportSourceOf(scratch.Write("downloads/a.txt", "a"));
        var plan = session.PlanImport(source, "notes/a.txt");
        Assert.Equal(ImportAction.Add, plan.Main.Action);
        Assert.Single(session.Import(plan));
        Assert.Equal("notes/a.txt changed since the import was planned", Assert.Throws<InvalidOperationException>(() => session.Import(plan)).Message);
    }

    [Fact]
    public void TheSessionReportsAReplacementAsTheOldEntryRemovedAndTheNewAddedTheSelectionGone()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var session = editor.Session;
        var old = session.Entry("notes/readme.txt")!;
        session.Select(old);
        var changes = new List<EntriesChange>();
        var selections = 0;
        session.EntriesChanged += changes.Add;
        session.SelectionChanged += () => selections++;
        var plan = session.PlanImport(session.ImportSourceOf(scratch.Write("downloads/a.txt", "a")), "notes/readme.txt");
        Assert.Equal((ImportAction.Replace, old), (plan.Main.Action, plan.Main.Existing));
        var made = Assert.Single(session.Import(plan));
        Assert.Equal([new EntriesChange(EntriesChangeKind.Removed, old, "notes/readme.txt"), new EntriesChange(EntriesChangeKind.Added, made)], changes);
        Assert.Null(session.Selected);
        Assert.Equal(1, selections);
        // The list shows the new entry once, in the old one's place.
        editor.Frame();
        Assert.Single(editor.Document.QuerySelectorAll(".entries-item[data-path=\"notes/readme.txt\"]"));
        // A new entry added: reported as added, nothing removed; the selection left.
        changes.Clear();
        session.Select(made);
        session.Import(session.PlanImport(session.ImportSourceOf(scratch.Write("downloads/b.txt", "b")), "notes/b.txt"));
        Assert.Equal([EntriesChangeKind.Added], changes.Select(c => c.Kind));
        Assert.Same(made, session.Selected);
    }
}
