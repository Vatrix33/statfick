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
        Assert.Equal(["New mod", "Open", "Save", "Save As", "Import", "Undo", "Redo"], buttons.Select(b => b.TextContent));
        EditorCommand[] commands = [EditorCommand.NewMod, EditorCommand.Open, EditorCommand.Save, EditorCommand.SaveAs, EditorCommand.Import, EditorCommand.Undo, EditorCommand.Redo];
        Assert.Equal(buttons, commands.Select(c => editor.View.Button(c)));
        Assert.All(buttons, b => Assert.True(b.IsDisabled));
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
        Assert.Equal([EditorView.FontsSheet, EditorView.ShellSheet], names.Take(2));
        Assert.Equal(names.Skip(2).Order(StringComparer.Ordinal), names.Skip(2));
        var resources = typeof(EditorView).Assembly.GetManifestResourceNames().Where(n => n.EndsWith(".css", StringComparison.Ordinal));
        Assert.Equal(resources.Order(StringComparer.Ordinal), names.Order(StringComparer.Ordinal));
        // Only the fonts sheet has @font-face rules.
        Assert.All(EditorView.Stylesheets, s => Assert.Equal(s.Name == EditorView.FontsSheet, s.Css.Contains("@font-face")));
        using var editor = new EditorHarness();
        Assert.Equal(names, editor.Document.Styles.Sheets.Skip(1).Select(s => s.Name));
    }

    [Fact]
    public void TheFontsAreTheGamesLatoFromUiPak()
    {
        using var editor = new EditorHarness();
        Assert.Empty(editor.Document.Fonts.Faces);
        Assert.Equal([("Lato", 400), ("Lato", 700)], editor.Document.Fonts.SheetFaces.Select(f => (f.Family, f.Weight)));
        // Lato Regular and Lato Bold (the tests' copies are ui.pak's files, UiResourcesTests).
        var faces = Canvas.TestFonts.Library.Faces;
        Assert.Equal([faces[0].AverageCharWidth, faces[2].AverageCharWidth], editor.Document.Fonts.SheetFaces.Select(f => f.AverageCharWidth));
    }

    [Fact]
    public void TheTitleNamesTheOpenPackAndMarksUnsavedWork()
    {
        using var editor = new EditorHarness();
        var changes = 0;
        editor.View.TitleChanged += () => changes++;
        Assert.Equal("Statventure Editor", editor.View.Title);
        editor.Session.PackName = "my-mod.pak";
        Assert.Equal("Statventure Editor — my-mod.pak", editor.View.Title);
        Assert.Equal(1, changes);
        var unsaved = false;
        editor.Session.AddUnsavedWork(() => false);
        editor.Session.AddUnsavedWork(() => unsaved);
        editor.Session.NotifyChanged();
        Assert.Equal(1, changes);
        unsaved = true;
        editor.Session.NotifyChanged();
        Assert.Equal("• Statventure Editor — my-mod.pak", editor.View.Title);
        Assert.Equal(2, changes);
        editor.Session.PackName = null;
        Assert.Equal("• Statventure Editor", editor.View.Title);
        unsaved = false;
        editor.View.Refresh();
        Assert.Equal("Statventure Editor", editor.View.Title);
        Assert.Equal(4, changes);
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
        Assert.Equal(["NoPane", "RecordingPart", "EntriesLate", "FormFirst", "FormSecond", "FormSecondToo", "PreviewPart"], editor.View.Parts.Select(p => p.GetType().Name));
        Assert.Null(editor.View.Part<NoPane>().Element);
        // A part with a pane has its own element there, stacked in order.
        Assert.Equal([editor.View.Part<FormFirst>().Element, editor.View.Part<FormSecond>().Element, editor.View.Part<FormSecondToo>().Element],
            editor.View.Pane(EditorPane.Form).ElementChildren);
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
        // The editor's own parts are those of its assembly (none before step 8).
        using var editor = new EditorHarness();
        Assert.Equal(EditorPart.Find(typeof(EditorView).Assembly), editor.View.Parts.Select(p => p.GetType()));
    }
}
