using Statventure.Editor.About;
using Statventure.Editor.Shell;
using Statventure.UI;
using Statventure.UI.Dom;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.About;

/// <summary>
/// The editor's credits (Q30; M12 step 6): About in the top bar opens the engine's credits view in a dialog over the
/// shell (the editor's name and version, BRUTAL's notice verbatim, the components, where the licences are), its content
/// scrolling within the window; Close or Escape closes it.
/// </summary>
public class AboutTests
{
    [Fact]
    public void AboutShowsTheEnginesCreditsInADialog()
    {
        using var editor = new EditorHarness(1000, 640);
        var about = editor.View.Button(EditorCommand.About)!;
        Assert.Equal("About", about.TextContent);
        editor.Click(about);
        var dialog = editor.View.Part<AboutPart>().Current!;
        Assert.Same(dialog, editor.View.Dialogs.Top);
        Assert.True(dialog.Element.HasClass("about-dialog"));
        Assert.Equal("About", editor.Asked().Title);
        // The engine's view, as Credits.Markup makes it for the editor of its own version (Q3: no commit).
        using (var fonts = new Statventure.Text.FontLibrary())
        {
            var made = new UiDocument(fonts);
            Markup.SetInnerHtml(made.Body, Credits.Markup("Statventure Editor", AboutPart.Program));
            Assert.Equal(Markup.Serialize(made.Body.ElementChildren.Single()), Markup.Serialize(dialog.Content.ElementChildren.Single()));
        }
        Assert.Equal(("Statventure.Editor", "dev"), (AboutPart.Program.Name, AboutPart.Program.Version));
        Assert.Equal("Statventure Editor", editor.Q(".about-dialog .credits h1").TextContent);
        Assert.Equal("Version dev", editor.Q(".about-dialog .credits .version").TextContent);
        Assert.Equal(Credits.Notice, editor.Q(".about-dialog .credits .notice").TextContent);
        Assert.StartsWith(Credits.BrutalCopyright, Credits.Notice);
        Assert.Equal(Credits.LicencesLine, editor.Q(".about-dialog .credits").ElementChildren.Last().TextContent);
        // Close has the focus; the shell is inert behind it.
        Assert.Equal(["Close"], editor.Buttons());
        Assert.Same(dialog.Buttons.ElementChildren.Single(), editor.Document.FocusedElement);
        Assert.True(editor.View.Shell.Inert);
        editor.Answer("Close");
        Assert.Null(editor.View.Part<AboutPart>().Current);
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Same(about, editor.Document.FocusedElement);
        // Escape closes it too.
        editor.Click(about);
        Assert.NotNull(editor.View.Dialogs.Top);
        editor.Key("Escape", "Escape");
        Assert.Null(editor.View.Dialogs.Top);
    }

    [Fact]
    public void TheCreditsScrollWithinTheWindow()
    {
        foreach (var (width, height) in new[] { (1000, 640), (800, 500) })
        {
            using var editor = new EditorHarness(width, height);
            editor.Click(editor.View.Button(EditorCommand.About)!);
            var box = editor.Rect(".about-dialog");
            // 640 px wide (the credits' column), 24 px from the window's edges at most, its buttons inside it.
            Assert.Equal(Math.Min(640, width - 48), box.Width);
            Assert.Equal(height - 48, box.Height);
            Assert.Equal((height - box.Height) / 2, box.Y, 0.5f);
            var content = editor.Q(".about-dialog .dialog-content");
            Assert.True(content.ScrollHeight > content.ClientHeight + 1000, $"{content.ScrollHeight} in {content.ClientHeight}");
            Assert.True(editor.Rect(".about-dialog .dialog-buttons").Bottom <= box.Bottom);
            // A wheel over the content scrolls it.
            editor.Input.PointerMove(editor.Middle(content));
            editor.Input.Wheel(editor.Middle(content), new System.Numerics.Vector2(0, 400), precise: true);
            editor.Frame();
            Assert.True(content.ScrollTop > 0);
        }
    }

    [Fact]
    public void AboutWhileItIsOpenOpensNoSecond()
    {
        using var editor = new EditorHarness();
        var part = editor.View.Part<AboutPart>();
        var first = editor.Run(() => part.Show());
        var dialog = part.Current;
        var second = editor.Run(() => part.Show());
        Assert.True(second.IsCompleted);
        Assert.Same(dialog, part.Current);
        Assert.Single(editor.View.Dialogs.Open);
        editor.Answer("Close");
        Assert.True(first.IsCompleted);
    }
}
