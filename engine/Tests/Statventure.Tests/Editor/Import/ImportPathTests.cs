using Statventure.Editor.Import;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Records;
using Statventure.Tests.Render3D;
using static Statventure.Tests.Editor.Import.ImportTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Import;

/// <summary>
/// The path an import suggests and takes (P28; the manager's decisions of 2026-11-07): a file's name made to fit the
/// path rule, a record in its kind's folder by the kind's name rule (the name as it is first: M11-18's way back in), a
/// kind without a folder at the top, the mod's info at <c>mod/info</c>, files by their ending; editable, and what can't
/// be a path said in the dialog.
/// </summary>
public class ImportPathTests
{
    [Theory]
    [InlineData("Arena Stage (Final)", "arena-stage-final")]
    [InlineData("  --Ärger über Öl--  ", "arger-uber-ol")]
    [InlineData("a__b.c-d", "a__b.c-d")]
    [InlineData("a - b -- c", "a-b-c")]
    [InlineData("..hidden..", "hidden")]
    [InlineData("combat@alder-vale", "combat-alder-vale")]
    [InlineData("日本", "imported")]
    [InlineData("", "imported")]
    [InlineData("CON", "con-1")]
    [InlineData("aux.tar", "aux-1.tar")]
    [InlineData("Lpt9.x.y", "lpt9-1.x.y")]
    public void AFilesNameIsMadeToFitThePathRule(string name, string fitted)
    {
        Assert.Equal(fitted, ImportNames.Fit(name));
        Assert.Null(PackPath.Problem(fitted));
    }

    [Fact]
    public void ALongNameIsCutToItsLengthWithoutAnEndingDotOrDash()
    {
        Assert.Equal(new string('a', ImportNames.MaxLength), ImportNames.Fit(new string('a', 300)));
        Assert.Equal(new string('a', ImportNames.MaxLength - 1), ImportNames.Fit(new string('a', ImportNames.MaxLength - 1) + "-b"));
        Assert.Equal(new string('a', ImportNames.MaxLength - 1), ImportNames.Fit(new string('a', ImportNames.MaxLength - 1) + ".b"));
    }

    [Fact]
    public void FilesGoIntoTheirEndingsFolder()
    {
        Assert.Equal(("ui/fonts", "ui/fonts", "ui/images", "ui/images", ""),
            (ImportNames.FolderOf(".ttf"), ImportNames.FolderOf(".otf"), ImportNames.FolderOf(".png"), ImportNames.FolderOf(".svg"), ImportNames.FolderOf(".txt")));
        Assert.Equal("ui/fonts/my-hand.otf", ImportNames.ForFile("My Hand", ".otf"));
        Assert.Equal("read-me.txt", ImportNames.ForFile("Read Me", ".txt"));
    }

    [Fact]
    public void ARecordGoesIntoItsKindsFolderByItsRule()
    {
        // A plain name rule; a name the rule takes as it is; one it doesn't, fitted; the rule's own storing (capitals).
        Assert.Equal("parts/left-arm", ImportNames.ForRecord(RecordShape.Of<Part>(), "Left Arm"));
        Assert.Equal("parts/arm", ImportNames.ForRecord(RecordShape.Of<Part>(), "arm"));
        Assert.Equal("creatures/_ogre", ImportNames.ForRecord(RecordShape.Of<Creature>(), "Ogre"));
        Assert.Equal("creatures/ogre-king", ImportNames.ForRecord(RecordShape.Of<Creature>(), "ogre king"));
        // No folder: the top; the mod's info: its one place.
        Assert.Equal("seven-kinds", ImportNames.ForRecord(RecordShape.Of<Kinds>(), "Seven Kinds"));
        Assert.Equal(ModInfo.Path, ImportNames.ForRecord(RecordShape.Of<ModInfo>(), "My Mod"));
    }

    [Fact]
    public void TheSuggestionComesFromWhatTheFileImportsAs()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var session = editor.Session;
        (string, RecordShape?) Suggest(string name, byte[] content) => session.SuggestImportPath(session.ImportSourceOf(scratch.Write("downloads/" + name, content)));
        var blocks = File.ReadAllBytes(TestModelPack.SourceOf("blocks"));
        Assert.Equal(("models/imported", RecordShape.Of<Statventure.Render3D.ModelData>()), Suggest("!!!.glb", blocks));
        Assert.Equal(("models/v2", RecordShape.Of<Statventure.Render3D.ModelData>()), Suggest("V2.Glb", blocks));
        Assert.Equal(("idle-walk", RecordShape.Of<Statventure.Animation.StateMachineDefinition>()),
            Suggest("Idle Walk.JSON", RecordFixtures.Read("animation.state-machine", "state-machine", ".json")));
        Assert.Equal((ModInfo.Path, RecordShape.Of<ModInfo>()), Suggest("about.json", RecordFixtures.Read("mod.info", "every-field", ".json")));
        Assert.Equal(("ui/images/a-b.png", null), Suggest("A&B.PNG", [1]));
        // A theme's name stored by the music's rule (S6-1): its JSON form exported under its name comes back to its path.
        var theme = session.Entries.First(e => e.Path.StartsWith("audio/themes/", StringComparison.Ordinal) && e.Name.Contains('@'));
        Assert.Equal((theme.Path, theme.Kind), Suggest(theme.Name + ".json", System.Text.Encoding.UTF8.GetBytes(RecordSerializer.ToJson(theme.Kind!.Type, theme.Saved!))));
    }

    [Fact]
    public void ThePathCanBeChangedAndWhatCantBeAPathIsSaid()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var file = scratch.Write("downloads/arena.glb", File.ReadAllBytes(TestModelPack.SourceOf("blocks")));
        editor.ClickImport(file);
        var dialog = editor.Dialog();
        foreach (var (path, problem) in new[]
        {
            ("", "Give the entry a path."),
            ("Models/arena", "That can't be its path: 'M' isn't allowed (only a–z, 0–9, '-', '_', '.' and '/')."),
            ("notes", "That can't be its path: notes is the folder of notes/readme.txt."),
            ("notes/readme.txt/arena", "That can't be its path: notes/readme.txt is an entry, so it can't be a folder."),
            ("models/" + new string('a', 252), "That can't be its path: with its ending, a name is longer than 255 characters."),
        })
        {
            dialog.PathField.Value = path;
            editor.Answer("Import");
            Assert.Same(dialog, editor.Import().Current);
            Assert.Equal(problem, dialog.Problem);
            // Typing takes the problem away.
            dialog.PathField.Focus();
            editor.Key("x", "KeyX");
            Assert.Equal("", dialog.Problem);
        }
        // Enter in the field imports (not with Shift, Ctrl or Alt); the path typed is taken, any folder.
        dialog.PathField.Value = "models/stages/arena";
        dialog.PathField.Focus();
        foreach (var modifiers in new[] { Statventure.Platform.KeyModifiers.Shift, Statventure.Platform.KeyModifiers.Control, Statventure.Platform.KeyModifiers.Alt, Statventure.Platform.KeyModifiers.Super })
        {
            editor.Key("Enter", "Enter", modifiers);
            Assert.Same(dialog, editor.Import().Current);
        }
        editor.Key("Enter", "Enter");
        Assert.Null(editor.Import().Current);
        Assert.Equal(scratch.Packed(file, "models/stages/arena.glb", "models/stages/arena"), Content(editor.Session.Entry("models/stages/arena")!));
        // Cancel and Escape import nothing.
        editor.ClickImport(file);
        editor.Answer("Cancel");
        editor.ClickImport(file);
        editor.Key("Escape", "Escape");
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Null(editor.Session.Entry("models/arena"));
    }

    [Fact]
    public void APathsEndingCantMakeItImportAsSomethingElse()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor();
        var session = editor.Session;
        var text = session.ImportSourceOf(scratch.Write("downloads/notes.txt", "Notes."));
        var room = session.ImportSourceOf(scratch.Write("downloads/room.json", RecordSerializer.ToJson(new Statventure.Audio.RoomSpec(1, 0, 0))));
        Assert.Equal((ImportWay.File, ".txt", "notes"), (text.Way, text.Ending, text.Stem));
        var model = session.ImportSourceOf(scratch.Write("downloads/Arena Stage.GLB", File.ReadAllBytes(TestModelPack.SourceOf("blocks"))));
        Assert.Equal((ImportWay.Importer, ".glb", "Arena Stage", "models"), (model.Way, model.Ending, model.Stem, model.Importer!.Name));
        Assert.Equal((ImportWay.Record, ".json", "room"), (room.Way, room.Ending, room.Stem));
        Assert.Null(session.ImportPathProblem(text, "docs/notes.md"));
        Assert.Equal("That can't be its path: a path ending as .glb would be imported as something else.", session.ImportPathProblem(text, "docs/notes.glb"));
        Assert.Equal("That can't be its path: a path ending as .json would be imported as something else.", session.ImportPathProblem(text, "docs/notes.json"));
        Assert.Equal("That can't be its path: a/b.terrain.json would go through the world importer.", session.ImportPathProblem(room, "a/b.terrain"));
        Assert.Null(session.ImportPathProblem(room, "a/b.terrains"));
        var ex = Assert.Throws<ImportException>(() => session.PlanImport(text, "docs/notes.css"));
        Assert.Equal("That can't be its path: a path ending as .css would be imported as something else.", ex.Message);
        // What isn't imported at all, and a file that is gone.
        Assert.Equal("the editor imports .css, .glb, .terrain.json, .json, .ttf, .otf, .png, .svg and .txt files",
            Assert.Throws<ImportException>(() => session.ImportSourceOf(scratch.Write("downloads/a.wav", [1]))).Message);
        Assert.Equal("the editor imports .css, .glb, .terrain.json, .json, .ttf, .otf, .png, .svg and .txt files",
            Assert.Throws<ImportException>(() => session.ImportSourceOf(scratch.Write("downloads/.txt", [1]))).Message);
        Assert.Equal("the editor imports .css, .glb, .terrain.json, .json, .ttf, .otf, .png, .svg and .txt files",
            Assert.Throws<ImportException>(() => session.ImportSourceOf(scratch.Write("downloads/.json", "{\"$types\": {}}"))).Message);
        Assert.Equal("the file isn't there", Assert.Throws<ImportException>(() => session.ImportSourceOf(scratch["downloads/gone.txt"])).Message);
        var gone = session.ImportSourceOf(scratch.Write("downloads/gone.png", [1]));
        File.Delete(gone.File);
        Assert.Equal("the file isn't there", Assert.Throws<ImportException>(() => session.PlanImport(gone, "ui/images/gone.png")).Message);
        Assert.Equal(session.ImportEndings, [".css", ".glb", ".terrain.json", ".json", ".ttf", ".otf", ".png", ".svg", ".txt"]);
    }
}
