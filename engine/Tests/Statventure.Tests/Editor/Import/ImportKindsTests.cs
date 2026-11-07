using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;
using Statventure.Tests.Render3D;
using static Statventure.Tests.Editor.Import.ImportTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Import;

/// <summary>
/// Each kind the editor imports (Docs/Editor.md, "Import (step 12)"; M11-8, P28), by the Import button and by a drop,
/// is the entry the packer makes of the same file at the same path: a <c>.glb</c> model, a <c>.terrain.json</c> terrain
/// with its images beside it, a <c>.css</c> stylesheet whose urls are checked against the data the mod sees (the game's
/// packs, the mod's entries, and what was imported in the session), a record's <c>.json</c> form, and loose files
/// (<c>.ttf</c>, <c>.otf</c>, <c>.png</c>, <c>.svg</c>, <c>.txt</c>); each a new entry of the session (unsaved, the pack
/// file untouched), selected.
/// </summary>
public class ImportKindsTests
{
    static string Data(string relative) => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Data", relative);

    [Fact]
    public void AModelByTheButtonIsThePackersEntry()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var mod = File.ReadAllBytes(editor.Session.Pack!.File);
        var file = scratch.Write("downloads/Arena Stage (Final).GLB", File.ReadAllBytes(TestModelPack.SourceOf("blocks")));
        Assert.False(editor.View.Button(EditorCommand.Import)!.IsDisabled);
        editor.ClickImport(file);
        Assert.Equal("Import", editor.Title());
        Assert.Equal("Arena Stage (Final).GLB imports as a model. Its entry's path:", editor.Message());
        Assert.Equal("models/arena-stage-final", editor.Dialog().PathField.Value);
        Assert.Same(editor.Dialog().PathField, editor.Document.FocusedElement);
        Assert.Equal(["Import", "Cancel"], editor.Buttons());
        editor.Answer("Import");
        Assert.Null(editor.View.Dialogs.Top);
        var entry = editor.Session.Entry("models/arena-stage-final")!;
        Assert.Equal(scratch.Packed(file, "models/arena-stage-final.glb", "models/arena-stage-final"), Content(entry));
        Assert.Equal((EntrySource.New, "Model", true), (entry.Source, entry.KindName, entry.IsChanged));
        Assert.Same(entry, editor.Session.Selected);
        Assert.Equal("Imported Arena Stage (Final).GLB as models/arena-stage-final.", editor.View.Status);
        Assert.True(editor.Session.HasUnsavedWork);
        Assert.StartsWith("• ", editor.View.Title);
        // Importing never writes the pack (step 11 saves).
        Assert.Equal(mod, File.ReadAllBytes(editor.Session.Pack!.File));
    }

    [Fact]
    public void ATerrainTakesItsImagesFromBesideItAndIsThePackersEntry()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var file = TestTerrainPack.Copy(scratch.Folder, "downloads/maps", "Green Vale");
        editor.ClickImport(file);
        Assert.Equal("Green Vale.terrain.json imports as a terrain. Its entry's path:", editor.Message());
        Assert.Equal("world/terrain/green-vale", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        var entry = editor.Session.Entry("world/terrain/green-vale")!;
        byte[] Image(string name) => File.ReadAllBytes(scratch[$"downloads/maps/{name}"]);
        Assert.Equal(scratch.Packed(file, "world/terrain/green-vale.terrain.json", "world/terrain/green-vale",
            ("world/terrain/test-height.png", Image("test-height.png")), ("world/terrain/test-cover.png", Image("test-cover.png"))), Content(entry));
        RecordAssert.Equal(TerrainData.Load(TestTerrainPack.Description), entry.Build!.Value!);
        // Only the terrain: its images are its sources, not entries of their own.
        Assert.Null(editor.Session.Entry("world/terrain/test-height.png"));

        // An image that isn't beside it: said, nothing imported.
        File.Delete(scratch["downloads/maps/test-cover.png"]);
        var other = scratch.Write("downloads/maps/Other.terrain.json", File.ReadAllBytes(file));
        editor.ClickImport(other);
        Assert.Equal(("Can't import", "Other.terrain.json can't be imported: the world importer: No 'test-cover.png' among the files the import can read."),
            (editor.Title(), editor.Message()));
        Assert.Equal(["OK"], editor.Buttons());
        editor.Answer("OK");
        Assert.Null(editor.Session.Entry("world/terrain/other"));
    }

    const string Sheet = """
        @font-face { font-family: Hand; src: url("ui/fonts/lato-regular.ttf") }
        .card { background: url(ui/images/mod.png) no-repeat; color: #d77a50 }
        .new { --icon: url("ui/images/new.png") }
        """;

    [Fact]
    public void AStylesheetsUrlsAreCheckedAgainstTheGamesPacksTheModAndWhatWasImported()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor(w => w.Add("ui/images/mod.png", [1, 2, 3]));
        var css = scratch.Write("downloads/Card.css", Sheet);
        // Before the image it names is imported, its url is a problem, named in the packer's words.
        editor.ClickImport(css);
        Assert.Equal("Card.css can't be imported: the ui importer: 1 problem: url(\"ui/images/new.png\") names no file in the data being packed.", editor.Message());
        editor.Answer("OK");
        Assert.Null(editor.Session.Entry("ui/styles/card"));
        // The image imported (unsaved) is in the data the mod sees: the sheet now imports.
        var png = scratch.Write("downloads/New.png", File.ReadAllBytes(Data("Terrain/test-cover.png")));
        editor.ClickImport(png);
        Assert.Equal("ui/images/new.png", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        editor.ClickImport(css);
        Assert.Equal("ui/styles/card", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        var entry = editor.Session.Entry("ui/styles/card")!;
        Assert.Equal(scratch.Packed(css, "ui/styles/card.css", "ui/styles/card",
            ("ui/fonts/lato-regular.ttf", [1]), ("ui/images/mod.png", [1]), ("ui/images/new.png", [1])), Content(entry));
        Assert.Equal("Stylesheet", entry.KindName);
        // A url the base, the pack and the session lack is still refused.
        var gone = scratch.Write("downloads/gone.css", ".a { background: url(ui/images/gone.png) }");
        editor.ClickImport(gone);
        Assert.Equal("gone.css can't be imported: the ui importer: 1 problem: url(\"ui/images/gone.png\") names no file in the data being packed.", editor.Message());
        editor.Answer("OK");
    }

    [Fact]
    public void ARecordsJsonFormIsThePackersRecordInItsKindsFolder()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var json = scratch.Write("downloads/Great Hall.json", RecordSerializer.ToJson(new RoomSpec(2.5, 0.25, 0.75)));
        editor.ClickImport(json);
        Assert.Equal("Great Hall.json imports as a room (its JSON form). Its entry's path:", editor.Message());
        Assert.Equal("audio/rooms/great-hall", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        var entry = editor.Session.Entry("audio/rooms/great-hall")!;
        Assert.Equal(scratch.Packed(json, "audio/rooms/great-hall.json", "audio/rooms/great-hall"), Content(entry));
        Assert.Equal(new RoomSpec(2.5, 0.25, 0.75), entry.Build!.Value);

        // A .json that isn't a record's JSON form, one of a kind the editor doesn't know, and one that isn't JSON: said.
        foreach (var (name, text, why) in new[]
        {
            ("plain.json", "{\"name\": \"not a record\"}", "it isn't a record's JSON form (its first key isn't $types)."),
            ("ogre.json", RecordSerializer.ToJson(LooseRecordTests.Ogre), "a record of type test.creature, which isn't a record type known here."),
        })
        {
            editor.ClickImport(scratch.Write("downloads/" + name, text));
            Assert.Equal($"{name} can't be imported: {why}", editor.Message());
            editor.Answer("OK");
        }
        // An instrument of the game's, by its JSON form ("an" before a vowel).
        var harp = editor.Session.Entry("audio/instruments/harp")!;
        editor.ClickImport(scratch.Write("downloads/harp.json", RecordSerializer.ToJson(harp.Kind!.Type, harp.Saved!)));
        Assert.Equal("harp.json imports as an instrument (its JSON form). Its entry's path:", editor.Message());
        editor.Answer("Cancel");
        editor.ClickImport(scratch.Write("downloads/cut.json", "{\"$types\": {"));
        Assert.StartsWith("cut.json can't be imported: ", editor.Message());
        editor.Answer("OK");
        Assert.Equal(["audio/rooms/great-hall"], editor.Session.Entries.Where(e => e.Source == EntrySource.New).Select(e => e.Path));
    }

    public static TheoryData<string, string, string> LooseFiles => new()
    {
        { "Fonts/Lato-Bold.ttf", "Hand Bold.TTF", "ui/fonts/hand-bold.ttf" },
        { "Fonts/STIXTwoText-Italic.otf", "STIX Two Text Italic.otf", "ui/fonts/stix-two-text-italic.otf" },
        { "Terrain/test-height.png", "Height Map.png", "ui/images/height-map.png" },
        { "", "Icône.SVG", "ui/images/icone.svg" },
        { "Fonts/Lato-OFL.txt", "Lato OFL.txt", "lato-ofl.txt" },
    };

    [Theory]
    [MemberData(nameof(LooseFiles))]
    public void ALooseFileIsThePackersFileInItsEndingsFolder(string data, string name, string path)
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var bytes = data.Length > 0 ? File.ReadAllBytes(Data(data)) : "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray();
        var file = scratch.Write("downloads/" + name, bytes);
        editor.ClickImport(file);
        Assert.Equal($"{name} imports as a file. Its entry's path:", editor.Message());
        Assert.Equal(path, editor.Dialog().PathField.Value);
        editor.Answer("Import");
        var entry = editor.Session.Entry(path)!;
        Assert.Equal(scratch.Packed(file, path, path), Content(entry));
        Assert.Equal((EntrySource.New, EntryContent.File, (long)bytes.Length, "File", true), (entry.Source, entry.Content, entry.FileSize, entry.KindName, entry.ReadOnly));
        Assert.True(entry.IsChanged);
        Assert.Same(entry, editor.Session.Selected);
        Assert.Equal($"A file of {bytes.Length.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} bytes. It changes only by importing it again.", editor.Q(".form-about").TextContent);
    }

    [Fact]
    public void DroppedFilesImportInTheirOrderEachAskedCancelSkippingOnlyThatOne()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var model = scratch.Write("drops/Sword.glb", File.ReadAllBytes(TestModelPack.SourceOf("sword")));
        var png = scratch.Write("drops/Badge.png", File.ReadAllBytes(Data("Terrain/test-cover.png")));
        var css = scratch.Write("drops/badge.css", ".badge { background: url(ui/images/badge.png) }");
        var text = scratch.Write("drops/notes.txt", "Notes.");
        var other = scratch.Write("drops/archive.zip", [1, 2]);
        editor.Drop(model, png, other, css, text);
        Assert.Empty(editor.Session.PendingImports);
        Assert.Equal("models/sword", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        Assert.Equal("ui/images/badge.png", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        Assert.Equal(("Can't import", "archive.zip can't be imported: the editor imports .css, .glb, .terrain.json, .json, .ttf, .otf, .png, .svg and .txt files."),
            (editor.Title(), editor.Message()));
        editor.Answer("OK");
        // The sheet names the image imported just before it.
        Assert.Equal("ui/styles/badge", editor.Dialog().PathField.Value);
        editor.Answer("Cancel");
        Assert.Equal("notes.txt", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Equal(["models/sword", "notes.txt", "ui/images/badge.png"],
            editor.Session.Entries.Where(e => e.Source == EntrySource.New).Select(e => e.Path));
        Assert.Equal(scratch.Packed(model, "models/sword.glb", "models/sword"), Content(editor.Session.Entry("models/sword")!));
        Assert.Equal(scratch.Packed(png, "ui/images/badge.png", "ui/images/badge.png"), Content(editor.Session.Entry("ui/images/badge.png")!));
        Assert.Equal(scratch.Packed(text, "notes.txt", "notes.txt"), Content(editor.Session.Entry("notes.txt")!));
        Assert.Equal("notes.txt", editor.Session.Selected!.Path);
        Assert.Equal("Imported notes.txt as notes.txt.", editor.View.Status);

        // A pack and a file dropped together: the pack opens, then the file imports into it.
        var pak = scratch.Mod("other.pak");
        editor.Drop(text, pak);
        Assert.Equal("Unsaved changes", editor.Title());
        editor.Answer("Discard");
        Assert.Equal("other.pak", editor.Session.Pack!.Name);
        Assert.Equal("notes.txt", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        Assert.NotNull(editor.Session.Entry("notes.txt"));
    }

    [Fact]
    public void NothingImportsWithoutAPackThatTakesChanges()
    {
        using var scratch = new Scratch();
        using var editor = new EditorHarness();
        editor.Session.PlayerFolder = scratch["player"];
        var text = scratch.Write("drops/notes.txt", "Notes.");
        var font = scratch.Write("drops/a.ttf", [1]);
        Assert.True(editor.View.Button(EditorCommand.Import)!.IsDisabled);
        editor.Drop(text, font);
        Assert.Equal(("Nothing imported", "notes.txt and a.ttf can't be imported: no pack is open. Open a mod or make one first."), (editor.Title(), editor.Message()));
        editor.Answer("OK");
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Empty(editor.Session.PendingImports);
        // The game's own pack outside the dev setup is read-only.
        using var installed = new EditorHarness();
        var session = new EditorSession(new GameData(installed.Session.Game.PacksFolder, false)) { PlayerFolder = scratch["player"] };
        session.Open(Path.Combine(session.Game.PacksFolder, "audio.pak"));
        Assert.False(session.CanImportFiles);
        Assert.Throws<InvalidOperationException>(() => session.PlanImport(session.ImportSourceOf(text), "notes.txt"));
        session.Dispose();
        // A mod it can't fully read is read-only (M11-16): said with why.
        editor.Session.Open(scratch.Mod("broken.pak", w => AddUnreadable(w, "bad/entry", "not a record")));
        editor.Frame();
        Assert.True(editor.View.Button(EditorCommand.Import)!.IsDisabled);
        editor.Drop(text);
        Assert.Equal("notes.txt can't be imported: broken.pak is read-only (an entry can't be read). Open a mod or make one first.", editor.Message());
        editor.Answer("OK");
        Assert.Null(editor.Session.Entry("notes.txt"));
    }

    [Fact]
    public void FilesGivenWhileAnImportAsksWaitForIt()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var a = scratch.Write("drops/a.txt", "a");
        var b = scratch.Write("drops/b.txt", "b");
        var c = scratch.Write("drops/c.txt", "c");
        editor.Drop(a);
        Assert.Equal("a.txt", editor.Dialog().PathField.Value);
        // Dropped meanwhile, or given by the button's own way: queued after it.
        editor.Drop(b);
        var given = editor.Run(() => editor.Import().ImportFiles([c]));
        Assert.True(given.IsCompleted);
        Assert.Empty(given.Result);
        Assert.Equal([b, c], editor.Session.PendingImports);
        Assert.Equal("a.txt", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        Assert.Equal("b.txt", editor.Dialog().PathField.Value);
        Assert.Empty(editor.Session.PendingImports);
        editor.Answer("Cancel");
        Assert.Equal("c.txt", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Equal(["a.txt", "c.txt"], editor.Session.Entries.Where(e => e.Source == EntrySource.New).Select(e => e.Path));
        // The browser's pick comes back as the entries imported.
        var browsed = editor.Run(() => editor.Import().Browse());
        var browser = editor.View.Part<Statventure.Editor.Browser.FileBrowserPart>().Current!;
        Assert.Equal(("Import a file", "Import"), (browser.Request.Title, browser.Request.Button));
        Assert.Equal(editor.Session.ImportEndings, browser.Request.Endings);
        Assert.Equal((Statventure.Storage.BrowserPlaceKind.Documents, false), (browser.Request.Fallback, browser.Request.NewFile));
        browser.PathField.Value = b;
        browser.ReadTyped();
        editor.Frame();
        editor.Answer("Import");
        Assert.Equal(["b.txt"], browsed.Done().Select(e => e.Path));
        Assert.Same(editor.Session.Entry("b.txt"), editor.Session.Selected);
    }

    [Fact]
    public void TheSessionsImportersAndKindsAreTheOnesUsed()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor(w => w.Add("parts/left-arm.upper", "OLD"u8));
        editor.Session.Types = Form.FormTesting.Types;
        editor.Session.Importers = Statventure.Packs.Importers.Of(typeof(Packs.Importing.LinesImporter), typeof(Packs.Importing.TextImporter), typeof(Packs.Importing.FailingImporter));
        // Their endings, an ending both an importer and the files take said once.
        Assert.Equal([".fail", ".lines", ".txt", ".json", ".ttf", ".otf", ".png", ".svg"], editor.Session.ImportEndings);
        var lines = scratch.Write("downloads/Left Arm.lines", "a left arm");
        editor.ClickImport(lines);
        Assert.Equal("Left Arm.lines imports as a body part. Its entry's path:", editor.Message());
        Assert.Equal("parts/left-arm", editor.Dialog().PathField.Value);
        // Two entries: the record new, its file over the mod's (the replace question for it alone).
        editor.Answer("Import");
        Assert.Equal("parts/left-arm.upper is already there (a file). Replace it with the imported file? This can't be undone.", editor.Message());
        editor.Answer("Replace");
        var arm = editor.Session.Entry("parts/left-arm")!;
        Assert.Equal(new Part { Name = "a left arm" }, arm.Build!.Value);
        Assert.Same(arm, editor.Session.Selected);
        Assert.Equal("A LEFT ARM", System.Text.Encoding.UTF8.GetString(editor.Session.Entry("parts/left-arm.upper")!.ImportedFile!.Value.Span));
        // Again: the record of its kind imported again, its values listed; the file replaced.
        editor.Edit(arm, "$.weight", 3.0);
        editor.ClickImport(lines);
        editor.Answer("Import");
        Assert.Equal("parts/left-arm.upper is already there (a file). Replace it with the imported file? This can't be undone.", editor.Message());
        editor.Answer("Replace");
        Assert.Equal(["$.weight"], editor.Import().CurrentList!.Rows.Select(r => r.Difference.Path.ToString()));
        Assert.Equal("parts/left-arm is already a body part: its bulk data comes from Left Arm.lines. One value differs between the entry and the file: keep the entry's or take the file's.",
            editor.Message());
        editor.Answer("Import again");
        Assert.Equal(3.0, ((Part)arm.Build!.Value!).Weight);
        Assert.Equal("Imported Left Arm.lines again into parts/left-arm.", editor.View.Status);
        // A .txt goes through the importer that takes it; an importer's failure is said in its words.
        editor.ClickImport(scratch.Write("downloads/note.txt", "hi"));
        Assert.Equal("note.txt imports as a file. Its entry's path:", editor.Message());
        Assert.Equal("note", editor.Dialog().PathField.Value);
        // At a path the importer's own entry can't have (a name of 256 characters), its words on the line, made a sentence.
        editor.Dialog().PathField.Value = new string('n', 251);
        editor.Answer("Import");
        Assert.Equal($"the test-text importer: '{new string('n', 251)}.seen' isn't a valid data path: a name is longer than 255 characters (Parameter 'path').", editor.Dialog().Problem);
        editor.Dialog().PathField.Value = "note";
        editor.Answer("Import");
        Assert.Equal(".txt:hi", System.Text.Encoding.UTF8.GetString(editor.Session.Entry("note.seen")!.ImportedFile!.Value.Span));
        editor.ClickImport(scratch.Write("downloads/bad.fail", "x"));
        Assert.Equal("bad.fail can't be imported: the test-failing importer: this source can't be imported.", editor.Message());
        editor.Answer("OK");
        // A record's JSON form of a kind only the session's kinds know.
        editor.ClickImport(scratch.Write("downloads/Right Leg.json", RecordSerializer.ToJson(new Part { Name = "right leg", Weight = 2 })));
        Assert.Equal("parts/right-leg", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        Assert.Equal(new Part { Name = "right leg", Weight = 2 }, editor.Session.Entry("parts/right-leg")!.Build!.Value);
        // A file the session's kinds don't know.
        editor.Session.Types = Statventure.Packs.RecordTypes.Engine;
        editor.ClickImport(lines);
        Assert.Equal("Left Arm.lines can't be imported: a record of type test.part, which isn't a record type known here.", editor.Message());
        editor.Answer("OK");
    }

    /// <summary>An importer that makes nothing of its file.</summary>
    [Statventure.Packs.Importer("test-nothing", ".nothing")]
    public sealed class NothingImporter : Statventure.Packs.IImporter
    {
        public void Import(Statventure.Packs.ImportContext context)
        {
        }
    }

    [Fact]
    public void AnImportThatMakesNoEntryIsSaid()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        editor.Session.Importers = Statventure.Packs.Importers.Of(typeof(NothingImporter));
        editor.ClickImport(scratch.Write("downloads/void.nothing", "x"));
        Assert.Equal("void.nothing can't be imported: the import made no entry.", editor.Message());
        editor.Answer("OK");
    }

    /// <summary>A ".before" file: the record <c>&lt;stem&gt;</c> (a part) and a file whose path comes before it (<c>0-&lt;name&gt;.txt</c> in its folder).</summary>
    [Statventure.Packs.Importer("test-before", ".before")]
    public sealed class BeforeImporter : Statventure.Packs.IImporter
    {
        public void Import(Statventure.Packs.ImportContext context)
        {
            var stem = context.PathWithoutEnding;
            var slash = stem.LastIndexOf('/') + 1;
            context.Add(stem[..slash] + "0-" + stem[slash..] + ".txt", "first"u8);
            context.AddRecord(stem, new Part { Name = "made" });
        }
    }

    [Fact]
    public void TheEntryAtThePathChosenIsTheImportsOwnNotTheFirstMade()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        editor.Session.Types = Form.FormTesting.Types;
        editor.Session.Importers = Statventure.Packs.Importers.Of(typeof(BeforeImporter));
        editor.ClickImport(scratch.Write("downloads/Hand.before", "x"));
        Assert.Equal("Hand.before imports as a body part. Its entry's path:", editor.Message());
        Assert.Equal("parts/hand", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        Assert.Equal("Part", editor.Session.Selected!.Kind!.Type.Name);
        Assert.Equal("parts/hand", editor.Session.Selected!.Path);
        Assert.Equal("Imported Hand.before as parts/hand.", editor.View.Status);
        Assert.NotNull(editor.Session.Entry("parts/0-hand.txt"));
    }

    [Fact]
    public void AFileThatCantBeReadIsSaid()
    {
        if (!OperatingSystem.IsLinux())
            return;
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var file = scratch.Write("downloads/locked.txt", "x");
        File.SetUnixFileMode(file, UnixFileMode.None);
        try
        {
            if (Environment.UserName == "root")
                return;
            editor.ClickImport(file);
            Assert.StartsWith("locked.txt can't be imported: Access to the path ", editor.Message());
            editor.Answer("OK");
        }
        finally
        {
            File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    [Fact]
    public void TheSessionImportsOnlyIntoAPackThatTakesChangesAndNoJsonButARecords()
    {
        using var scratch = new Scratch();
        var session = new EditorSession(GameData.Repo(EditorHarness.EngineFolder)) { PlayerFolder = scratch["player"] };
        using (session)
        {
            // Writable said, but no pack: nothing to import into.
            session.Writable = true;
            Assert.False(session.CanImportFiles);
            session.Open(scratch.Mod());
            Assert.True(session.CanImportFiles);
            var changed = 0;
            session.Changed += () => changed++;
            var plain = session.ImportSourceOf(scratch.Write("downloads/plain.json", "{\"a\": 1}"));
            Assert.Equal("it isn't a record's JSON form (its first key isn't $types)", Assert.Throws<ImportException>(() => session.PlanImport(plain, "plain")).Message);
            Assert.Equal(0, changed);
            session.Import(session.PlanImport(session.ImportSourceOf(scratch.Write("downloads/a.txt", "a")), "a.txt"));
            Assert.Equal(1, changed);
            Assert.True(session.HasUnsavedWork);
            // The session's checks (step 10's) run on an imported record at once.
            session.AddCheck((entry, built) => built is Statventure.Audio.RoomSpec { Wet: > 0.5 } ? [new RecordProblem(RecordPath.Root.Field("wet"), "too wet")] : []);
            var wet = session.Import(session.PlanImport(session.ImportSourceOf(scratch.Write("downloads/wet.json", RecordSerializer.ToJson(new RoomSpec(1, 0, 0.75)))), "audio/rooms/wet"))[0];
            Assert.Equal(["too wet (at $.wet)"], wet.Problems.Select(p => p.ToString()));
            // A plan made while the pack took changes isn't imported once it doesn't.
            var late = session.PlanImport(session.ImportSourceOf(scratch.Write("downloads/b.txt", "b")), "b.txt");
            session.Writable = false;
            Assert.Equal("no pack that takes changes is open", Assert.Throws<InvalidOperationException>(() => session.Import(late)).Message);
            Assert.Null(session.Entry("b.txt"));
        }
    }
}
