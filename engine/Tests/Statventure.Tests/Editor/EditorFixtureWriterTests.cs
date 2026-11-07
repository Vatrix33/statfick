using System.Text.Json;
using Statventure.Editor.Shell;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor;

/// <summary>
/// The writer of the editor's Chromium fixtures (P36): the stylesheets without the fonts sheet, the live document's
/// markup with its state as attributes and the preview's surface as an empty box, the list of every part's fixtures.
/// </summary>
public class EditorFixtureWriterTests
{
    [Fact]
    public void TheFixturesCssIsTheEditorsSheetsWithoutTheFonts()
    {
        var css = EditorFixtures.Css;
        Assert.DoesNotContain("@font-face", css);
        Assert.Equal(string.Join("\n", EditorView.Stylesheets.Skip(1).Select(s => s.Css)), css);
        Assert.Contains(".dialog-layer", css);
    }

    static UiDocument Document(string html)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        document.AddStylesheet("body { margin: 0; font: 13px Lato; } .scroll { width: 100px; height: 40px; overflow: auto; } .tall { width: 300px; height: 200px; } surface.view { display: block; width: 120px; height: 50px; margin: 3px; }");
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    [Fact]
    public void TheMarkupIsTheBodysContentWithItsStateAsAttributes()
    {
        var document = Document("""<p class="a" title="x &quot;y&quot; &amp; z">1 &lt; 2 &amp; 3&nbsp;4</p><input type="text" value="old"><input type="checkbox" checked><input type="checkbox"><input type="checkbox" id="mixed"><select><option>A</option><option>B</option></select><textarea>was</textarea><button id="b">B</button><div class="scroll"><div class="tall"></div></div><br><img alt="">""");
        var inputs = document.QuerySelectorAll("input").ToList();
        inputs[0].Value = "new \"one\"";
        inputs[1].Checked = false;
        inputs[2].Checked = true;
        inputs[3].Indeterminate = true;
        document.QuerySelectorAll("option").Last().Selected = true;
        document.QuerySelector("textarea")!.Value = "now <b>";
        document.GetElementById("b")!.Focus();
        var scroller = document.QuerySelector(".scroll")!;
        scroller.ScrollTop = 30;
        scroller.ScrollLeft = 12;
        Assert.Equal(
            """<p class="a" title="x &quot;y&quot; &amp; z">1 &lt; 2 &amp; 3&nbsp;4</p>"""
            + """<input type="text" value="new &quot;one&quot;"><input type="checkbox"><input type="checkbox" checked>"""
            + """<input type="checkbox" id="mixed" data-indeterminate><select><option>A</option><option selected>B</option></select>"""
            + """<textarea>now &lt;b&gt;</textarea><button id="b" data-focus>B</button>"""
            + """<div class="scroll" data-scroll-top="30" data-scroll-left="12"><div class="tall"></div></div><br><img alt>""",
            EditorFixtures.Html(document));
    }

    [Fact]
    public void ASurfaceIsAnEmptyBoxOfItsSize()
    {
        var document = Document("""<span>a</span><surface class="view" id="v" style="border: 1px solid red"><i>inside</i></surface><surface style="width: 30px; height: 20px"></surface>""");
        Assert.Equal(
            """<span>a</span><div class="view" id="v" style="border: 1px solid red; display: block; box-sizing: border-box; width: 122px; height: 52px"></div>"""
            + """<div style="width: 30px; height: 20px; display: inline-block; box-sizing: border-box; width: 30px; height: 20px"></div>""",
            EditorFixtures.Html(document));
    }

    [Fact]
    public void TheListHoldsEveryPartsFixturesByNameAsPaintReferenceReadsThem()
    {
        // Every part's fixtures (the shell's among them), by name.
        Assert.Equal(EditorFixtures.All.Select(f => f.Name).Order(StringComparer.Ordinal), EditorFixtures.All.Select(f => f.Name));
        Assert.Equal("dialog", EditorFixtures.All[0].Name);
        Assert.Contains("shell", EditorFixtures.All.Select(f => f.Name));
        var list = JsonDocument.Parse(EditorFixtures.ListJson()).RootElement;
        Assert.Equal(EditorFixtures.All.Count, list.GetArrayLength());
        var dialog = list[0];
        Assert.Equal(["name", "width", "height", "css", "html", "scrollX", "scrollY", "windowFocused"], dialog.EnumerateObject().Select(p => p.Name));
        Assert.Equal(("dialog", 1000, 640, true), (dialog.GetProperty("name").GetString(), dialog.GetProperty("width").GetInt32(), dialog.GetProperty("height").GetInt32(), dialog.GetProperty("windowFocused").GetBoolean()));
        Assert.Equal(EditorFixtures.Css, dialog.GetProperty("css").GetString());
        // The live editor in the fixture's state at scale 1.
        using var editor = EditorFixtures.Build(EditorFixtures.All[0], 1);
        Assert.Equal(EditorFixtures.Html(editor.Document), dialog.GetProperty("html").GetString());
        Assert.Contains("class=\"dialog-layer\"", dialog.GetProperty("html").GetString());
        Assert.Contains("<button class=\"primary\" type=\"button\" data-focus>OK</button>", dialog.GetProperty("html").GetString());
        Assert.DoesNotContain("dialog-layer", list.EnumerateArray().Single(f => f.GetProperty("name").GetString() == "shell").GetProperty("html").GetString());
        // A fixture drawn with the window unfocused says so.
        var unfocused = JsonDocument.Parse(EditorFixtures.ListJson([new EditorFixture("blurred", 300, 200, _ => { }, WindowFocused: false)])).RootElement;
        Assert.False(Assert.Single(unfocused.EnumerateArray()).GetProperty("windowFocused").GetBoolean());
    }

    [Fact]
    public void AFixtureIsBuiltAtItsSizeAndScaleWithTheWindowAsItSays()
    {
        var fixture = new EditorFixture("x", 500, 300, editor => editor.View.Status = "set up", WindowFocused: false);
        using var editor = EditorFixtures.Build(fixture, 1.65f);
        Assert.Equal((825f, 495f), (editor.Document.ViewportSize.X, editor.Document.ViewportSize.Y));
        Assert.Equal(1.65f, editor.Document.DeviceScale);
        Assert.Equal("set up", editor.View.Status);
        Assert.False(editor.Document.WindowFocused);
        using var focused = EditorFixtures.Build(fixture with { WindowFocused = true }, 1);
        Assert.True(focused.Document.WindowFocused);
    }
}
