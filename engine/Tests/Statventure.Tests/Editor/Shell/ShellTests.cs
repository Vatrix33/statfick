using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Editor.Shell;

/// <summary>
/// The editor's shell (M11 step 4; Docs/Editor.md, "The shell (step 4)"): its layout, stylesheets and fonts, the
/// status line, the title from the session, and the parts found and attached as P2 says.
/// </summary>
public class ShellTests
{
    [Fact]
    public void TheShellFillsTheWindowWithTheTopBarThePanesAndTheStatusLine()
    {
        using var editor = new EditorHarness(1000, 640);
        Assert.Equal((0f, 0f, 1000f, 33f), Box(editor.Rect(".topbar")));
        Assert.Equal((0f, 615f, 1000f, 25f), Box(editor.Rect(".status")));
        Assert.Equal((0f, 33f, 280f, 582f), Box(editor.Rect("[data-pane=entries]")));
        Assert.Equal((280f, 33f, 4f, 582f), Box(editor.Rect("[data-splitter=entries]")));
        Assert.Equal((284f, 33f, 352f, 582f), Box(editor.Rect("[data-pane=form]")));
        Assert.Equal((636f, 33f, 4f, 582f), Box(editor.Rect("[data-splitter=preview]")));
        Assert.Equal((640f, 33f, 360f, 582f), Box(editor.Rect("[data-pane=preview]")));
        Assert.Same(editor.Q("[data-pane=form]"), editor.View.Pane(EditorPane.Form));
        // A narrower window takes it from the middle pane.
        editor.Document.SetViewport(900, 500);
        editor.Frame();
        Assert.Equal((284f, 33f, 252f, 442f), Box(editor.Rect("[data-pane=form]")));
    }

    static (float, float, float, float) Box(Statventure.Core.RectF r) => (r.X, r.Y, r.Width, r.Height);

    [Fact]
    public void TheTopBarHasTheCommandsButtonsInOrderDisabledUntilAPartHandlesThem()
    {
        using var editor = new EditorHarness();
        var buttons = editor.Q(".topbar").ElementChildren.Where(e => e.Tag == "button").ToList();
        Assert.Equal(["New mod", "Open", "Save", "Save As", "New entry", "Import", "Undo", "Redo", "Game folder…", "About"], buttons.Select(b => b.TextContent));
        EditorCommand[] commands = [EditorCommand.NewMod, EditorCommand.Open, EditorCommand.Save, EditorCommand.SaveAs, EditorCommand.NewEntry, EditorCommand.Import,
            EditorCommand.Undo, EditorCommand.Redo, EditorCommand.GameFolder, EditorCommand.About];
        Assert.Equal(buttons, commands.Select(c => editor.View.Button(c)));
        // Game folder… and About at the right end (M12 step 6, Q30): after a filler taking the width left.
        Assert.Equal(1000 - 8, editor.Rect("[data-command=about]").Right, 0.01f);
        Assert.Equal(editor.Rect("[data-command=about]").X - 4, editor.Rect("[data-command=game-folder]").Right, 0.01f);
        // Open (step 8's Browser.Opener), New mod (step 11's Saving.SavingPart) and About (step 6 of M12) can always
        // run; with no pack open the others can't (their parts' handlers say so), and the dev setup has no game folder
        // to change.
        EditorCommand[] always = [EditorCommand.NewMod, EditorCommand.Open, EditorCommand.About];
        Assert.All(buttons.Where(b => !always.Any(c => editor.View.Button(c) == b)), b => Assert.True(b.IsDisabled));
        Assert.All(always, c => Assert.False(editor.View.Button(c)!.IsDisabled));
        Assert.Null(editor.View.Button(EditorCommand.Rename));
    }

    [Fact]
    public void TheStatusLineSaysNoPackIsOpenUntilAPartSaysOtherwise()
    {
        using var editor = new EditorHarness();
        Assert.Equal("No pack open", editor.View.Status);
        editor.View.Status = "Opened my-mod.pak";
        Assert.Equal("Opened my-mod.pak", editor.Q(".status").TextContent);
    }

    [Fact]
    public void TheStylesheetsAreTheFontsTheShellsThenEveryOtherOneByName()
    {
        var names = EditorView.Stylesheets.Select(s => s.Name).ToList();
        // The engine's credits' sheet third (the About dialog's content, M12 step 6), before the parts' (About.css adjusts it).
        Assert.Equal([EditorView.FontsSheet, EditorView.ShellSheet, EditorView.CreditsSheet], names.Take(3));
        Assert.Equal(Statventure.UI.Credits.Css, EditorView.Stylesheets[2].Css);
        Assert.Equal(names.Skip(3).Order(StringComparer.Ordinal), names.Skip(3));
        var resources = typeof(EditorView).Assembly.GetManifestResourceNames().Where(n => n.EndsWith(".css", StringComparison.Ordinal));
        Assert.Equal(resources.Order(StringComparer.Ordinal), names.Where(n => n != EditorView.CreditsSheet).Order(StringComparer.Ordinal));
        // Only the fonts sheet has @font-face rules.
        Assert.All(EditorView.Stylesheets, s => Assert.Equal(s.Name == EditorView.FontsSheet, s.Css.Contains("@font-face")));
        using var editor = new EditorHarness();
        Assert.Equal(names, editor.Document.Styles.Sheets.Skip(1).Select(s => s.Name));
    }

    [Fact]
    public void TheFontsAreTheEnginesLatoAtUiPaksPaths()
    {
        // Q17 (M12 step 6): the engine's embedded Lato (EngineFonts.Mount), not the game's ui.pak: the editor draws before
        // it knows the game. Fonts.css names ui.pak's paths, which the engine's mount gives.
        using var editor = new EditorHarness();
        var files = editor.Document.Files!;
        Assert.Equal(["the engine's fonts"], files.Mounts.Select(m => m.Path));
        Assert.Equal(Statventure.UI.EngineFonts.Regular.ToArray(), files.ReadAllBytes("ui/fonts/lato-regular.ttf"));
        Assert.Equal(Statventure.UI.EngineFonts.Bold.ToArray(), files.ReadAllBytes("ui/fonts/lato-bold.ttf"));
        Assert.Empty(editor.Document.Fonts.Faces);
        Assert.Equal([("Lato", 400), ("Lato", 700)], editor.Document.Fonts.SheetFaces.Select(f => (f.Family, f.Weight)));
        // Lato Regular and Lato Bold (the tests' copies are ui.pak's files, UiResourcesTests).
        var faces = Canvas.TestFonts.Library.Faces;
        Assert.Equal([faces[0].AverageCharWidth, faces[2].AverageCharWidth], editor.Document.Fonts.SheetFaces.Select(f => f.AverageCharWidth));
    }

    [Fact]
    public void TheTitleNamesTheVersionTheOpenPackAndMarksUnsavedWork()
    {
        using var editor = new EditorHarness();
        var changes = 0;
        editor.View.TitleChanged += () => changes++;
        // The tests' build is a dev build (Directory.Build.props; ProgramInfoTests): its version is "dev".
        Assert.Equal("Statventure Editor dev", editor.View.Title);
        editor.Session.PackName = "my-mod.pak";
        Assert.Equal("Statventure Editor dev — my-mod.pak", editor.View.Title);
        Assert.Equal(1, changes);
        var unsaved = false;
        editor.Session.AddUnsavedWork(() => false);
        editor.Session.AddUnsavedWork(() => unsaved);
        editor.Session.NotifyChanged();
        Assert.Equal(1, changes);
        unsaved = true;
        editor.Session.NotifyChanged();
        Assert.Equal("• Statventure Editor dev — my-mod.pak", editor.View.Title);
        Assert.Equal(2, changes);
        editor.Session.PackName = null;
        Assert.Equal("• Statventure Editor dev", editor.View.Title);
        unsaved = false;
        editor.View.Refresh();
        Assert.Equal("Statventure Editor dev", editor.View.Title);
        Assert.Equal(4, changes);
    }

    [Fact]
    public void TheTitleCarriesTheEditorsVersionAReleasesOrDev()
    {
        // Q3: the version in the editor's title, the commit in none (M12-6, Q1: a release's four numbers, else "dev").
        Assert.Equal("Statventure Editor 2026.11.8.1 — ember-rite.pak", EditorView.TitleOf("2026.11.8.1", "ember-rite.pak", unsaved: false));
        Assert.Equal("Statventure Editor 2026.11.8.1", EditorView.TitleOf("2026.11.8.1", null, unsaved: false));
        Assert.Equal("• Statventure Editor 2026.11.8.1 — ember-rite.pak", EditorView.TitleOf("2026.11.8.1", "ember-rite.pak", unsaved: true));
        Assert.Equal("• Statventure Editor dev", EditorView.TitleOf("dev", null, unsaved: true));
        // The editor's version is what its own program file records, the editor's assembly's (not the test host's).
        var recorded = Statventure.Core.ProgramInfo.Of(typeof(EditorView).Assembly);
        Assert.Equal(("Statventure.Editor", "dev"), (recorded.Name, EditorView.Version));
        Assert.Equal(recorded.Version, EditorView.Version);
    }

    [Fact]
    public void TheSessionNotesChangesOwnsWhatItIsGivenAndKnowsTheGamesData()
    {
        using var session = new EditorSession(GameData.Repo(EditorHarness.EngineFolder));
        Assert.Equal(TestPaths.Packs, session.Game.PacksFolder);
        Assert.True(session.Game.DevSetup);
        var changed = 0;
        session.Changed += () => changed++;
        session.PackName = "a.pak";
        session.PackName = "a.pak";
        Assert.Equal(1, changed);
        Assert.False(session.HasUnsavedWork);
        session.AddUnsavedWork(() => true);
        Assert.True(session.HasUnsavedWork);
        var disposed = new List<int>();
        session.Own(new Disposable(() => disposed.Add(1)));
        session.Own(new Disposable(() => disposed.Add(2)));
        session.Dispose();
        Assert.Equal([2, 1], disposed);
        session.Dispose();
        Assert.Equal([2, 1], disposed);
    }

    sealed class Disposable(Action action) : IDisposable
    {
        public void Dispose() => action();
    }

    [Fact]
    public void PartsAreAttachedWithoutAPaneFirstThenByPaneOrderAndName()
    {
        NotingPart.Attached.Clear();
        using var editor = new EditorHarness(parts: [typeof(PreviewPart), typeof(FormSecondToo), typeof(EntriesLate), typeof(FormSecond), typeof(NoPane), typeof(FormFirst), typeof(RecordingPart)]);
        Assert.Equal(["NoPane", "EntriesLate", "FormFirst", "FormSecond", "FormSecondToo", "PreviewPart"], NotingPart.Attached);
        // The test parts among the editor's own (the later steps' parts are attached by the same rule).
        Assert.Equal(["NoPane", "RecordingPart", "EntriesLate", "FormFirst", "FormSecond", "FormSecondToo", "PreviewPart"],
            editor.View.Parts.Where(p => p.GetType().Assembly == typeof(NotingPart).Assembly).Select(p => p.GetType().Name));
        Assert.Null(editor.View.Part<NoPane>().Element);
        // A part with a pane has its own element there, stacked in order.
        var noted = editor.View.Parts.OfType<NotingPart>().Select(p => p.Element).ToList();
        Assert.Equal([editor.View.Part<FormFirst>().Element, editor.View.Part<FormSecond>().Element, editor.View.Part<FormSecondToo>().Element],
            editor.View.Pane(EditorPane.Form).ElementChildren.Where(noted.Contains));
        Assert.All(editor.View.Parts.OfType<NotingPart>().Where(p => p.Pane is not null), p => Assert.Equal("part", p.Element!.ClassName));
        Assert.Same(editor.View.Pane(EditorPane.Preview), editor.View.Part<PreviewPart>().Element!.Parent);
        Assert.All(editor.View.Parts.OfType<NotingPart>(), p => Assert.True(p.HadSessionAndView));
        Assert.Same(editor.Session, editor.View.Part<FormFirst>().Session);
        Assert.Same(editor.View, editor.View.Part<FormFirst>().View);
    }

    [Fact]
    public void APartIsAnyConcreteSubclassInTheAssembly()
    {
        var found = EditorPart.Find(typeof(ShellTests).Assembly);
        Assert.Contains(typeof(RecordingPart), found);
        Assert.Contains(typeof(NoPane), found);
        Assert.DoesNotContain(typeof(NotingPart), found);
        Assert.Equal(found.Select(t => t.FullName).Order(StringComparer.Ordinal), found.Select(t => t.FullName));
        Assert.All(found, t => Assert.True(t.IsSubclassOf(typeof(EditorPart))));
        // The editor's own parts are those of its assembly (attached in the order of their panes).
        using var editor = new EditorHarness();
        Assert.Equal(EditorPart.Find(typeof(EditorView).Assembly), editor.View.Parts.Select(p => p.GetType()).OrderBy(t => t.FullName, StringComparer.Ordinal));
    }
}
