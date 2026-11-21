using System.Numerics;
using Statventure.Core;
using Statventure.Platform;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Css;
using Statventure.UI.Dom;
using Statventure.UI.Paint;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// The colour scheme's rules the Chromium pages (<see cref="ColorSchemeReferenceTests"/>) can't show: the preferred scheme
/// as the engine's default (light) and when it changes, a scheme change restyling the children, the select's list in the
/// select's scheme, the search field's dark cancel image, the base background.
/// </summary>
[Collection("GPU")]
public class ColorSchemeTests(GpuFixture gpu)
{
    static UiDocument Document(string css, string html)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        document.AddStylesheet(css);
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    static string Rgb(Color c) => ComputedValues.Color(c);

    static MediaQuery Query(string text) => MediaQuery.Parse(CssTokenizer.Tokenize(text));

    /// <summary>
    /// prefers-color-scheme follows the environment's preference (the engine's default is light; the tests act as the
    /// reference PC, whose Chromium prefers dark), and so does the used scheme of <c>color-scheme: light dark</c>; a change of
    /// the preference restyles the document.
    /// </summary>
    [Fact]
    public void ThePreferredSchemeDecidesLightDarkAndTheMediaQuery()
    {
        Assert.True(PlatformBehaviour.PrefersDarkColorScheme);
        Assert.True(Query("(prefers-color-scheme: dark)").Matches(new MediaEnvironment(100, 100, PrefersDark: true)));
        Assert.False(Query("(prefers-color-scheme: light)").Matches(new MediaEnvironment(100, 100, PrefersDark: true)));
        Assert.True(Query("(prefers-color-scheme: light)").Matches(new MediaEnvironment(100, 100)));
        Assert.False(Query("(prefers-color-scheme: dark)").Matches(new MediaEnvironment(100, 100)));
        var document = Document("""
            #both { color-scheme: light dark; color: CanvasText; }
            #pref { color: rgb(1, 1, 1); }
            @media (prefers-color-scheme: dark) { #pref { color: rgb(2, 2, 2); } }
            """, """<p id="both">both</p><p id="pref">pref</p>""");
        var both = document.GetElementById("both")!;
        var pref = document.GetElementById("pref")!;
        Assert.True(both.Style!.DarkColorScheme);
        Assert.Equal("rgb(255, 255, 255)", Rgb(both.Style.Color));
        Assert.Equal("rgb(2, 2, 2)", Rgb(pref.Style!.Color));
        // The engine's own default: light.
        var environment = document.Styles.Environment;
        document.Styles.SetEnvironment(environment with { PrefersDark = false });
        document.Update(0.1);
        Assert.False(both.Style!.DarkColorScheme);
        Assert.Equal("rgb(0, 0, 0)", Rgb(both.Style.Color));
        Assert.Equal("rgb(1, 1, 1)", Rgb(pref.Style!.Color));
        // A change of the preference alone (no media query depends on it) restyles the document too.
        var plain = Document("#both { color-scheme: light dark; color: CanvasText; }", """<p id="both">both</p>""");
        Assert.True(plain.GetElementById("both")!.Style!.DarkColorScheme);
        plain.Styles.SetEnvironment(plain.Styles.Environment with { PrefersDark = false });
        plain.Update(0.1);
        Assert.False(plain.GetElementById("both")!.Style!.DarkColorScheme);
        Assert.Equal("rgb(0, 0, 0)", Rgb(plain.GetElementById("both")!.Style!.Color));
        // dark alone, or with only, is dark whatever the preference.
        Assert.True(Properties.UsesDarkScheme(["dark"], false));
        Assert.True(Properties.UsesDarkScheme(["dark", "only"], false));
        Assert.False(Properties.UsesDarkScheme(["light", "dark"], false));
        Assert.True(Properties.UsesDarkScheme(["dark", "light"], true));
        Assert.False(Properties.UsesDarkScheme(["foo"], true));
        Assert.False(Properties.UsesDarkScheme([], true));
    }

    /// <summary>
    /// A changed scheme restyles the descendants that inherit it (their system colours and light-dark() resolve again),
    /// and only paints: the boxes keep their layout.
    /// </summary>
    [Fact]
    public void ASchemeChangeRestylesTheChildrenAndOnlyPaints()
    {
        var document = Document("""
            .dark { color-scheme: dark; }
            .dark-only { color-scheme: only dark; }
            #child { color: CanvasText; background-color: light-dark(rgb(1, 1, 1), rgb(2, 2, 2)); }
            """, """<div id="parent"><span id="child">text</span></div>""");
        var parent = document.GetElementById("parent")!;
        var child = document.GetElementById("child")!;
        Assert.Equal("rgb(0, 0, 0)", Rgb(child.Style!.Color));
        var laidOut = document.Layout.BoxesLaidOut;
        parent.AddClass("dark");
        document.Update(0.1);
        Assert.True(child.Style!.DarkColorScheme);
        Assert.Equal("rgb(255, 255, 255)", Rgb(child.Style.Color));
        Assert.Equal("rgb(2, 2, 2)", Rgb(child.Style.BackgroundColor));
        // The same used scheme, another computed value: inherited too.
        parent.RemoveClass("dark");
        parent.AddClass("dark-only");
        document.Update(0.2);
        Assert.Equal(["dark", "only"], child.Style!.ColorScheme);
        Assert.Equal(StyleChange.Paint, StyleDifference.Compare(Properties.Initial, new ComputedStyle { DarkColorScheme = true }));
        Assert.Equal(StyleChange.Paint, StyleDifference.Compare(Properties.Initial, new ComputedStyle { ColorScheme = ["dark"] }));
        Assert.Equal(laidOut, document.Layout.BoxesLaidOut);
    }

    /// <summary>
    /// An open select's list is in the select's used scheme, whatever the page's (InternalPopupMenu writes the select's
    /// DarkColorScheme as the popup's "only dark" or "only light" color-scheme).
    /// </summary>
    [Theory]
    [InlineData("dark", "", true)]
    [InlineData("dark", "light", false)]
    [InlineData("light", "dark", true)]
    [InlineData("light", "", false)]
    public void TheSelectsListTakesTheSelectsScheme(string page, string select, bool dark)
    {
        var style = select.Length > 0 ? $" style=\"color-scheme: {select}\"" : "";
        var document = Document($$"""
            :root { color-scheme: {{page}}; }
            body { margin: 0; }
            select { position: absolute; left: 10px; top: 10px; width: 160px; font: 14px Lato; }
            """, $"<select{style}><option>First</option><option>Second</option></select>");
        var middle = new Vector2(50, 20);
        document.Input.PointerDown(middle, MouseButton.Left);
        document.Input.PointerUp(middle, MouseButton.Left);
        document.Update(0.1);
        var popup = document.Input.SelectPopup!;
        Assert.Equal(dark ? "only dark" : "only light", popup.GetStyle("color-scheme"));
        Assert.Equal(dark, popup.Style!.DarkColorScheme);
        // Its rows in the scheme's colours (the user agent's Field and FieldText).
        Assert.Equal(dark ? "rgb(255, 255, 255)" : "rgb(0, 0, 0)", Rgb(popup.Style.Color));
        Assert.Equal(dark ? "rgb(59, 59, 59)" : "rgb(255, 255, 255)", Rgb(popup.Style.BackgroundColor));
    }

    /// <summary>
    /// The search field's cancel button is Chromium's dark image in the dark scheme (white strokes) and its light one in the
    /// light scheme (dark strokes). (Its Chromium page differs from the engine's in both schemes alike, by the button's
    /// place and the image's filtering, so it isn't among the reference pages.)
    /// </summary>
    [Theory]
    [InlineData("light", false)]
    [InlineData("dark", true)]
    public void TheCancelButtonIsTheSchemesImage(string scheme, bool dark)
    {
        Assert.NotSame(ControlImages.SearchCancel(false, false), ControlImages.SearchCancel(false, true));
        Assert.NotSame(ControlImages.SearchCancel(true, false), ControlImages.SearchCancel(true, true));
        var fixture = new PaintFixture("cancel", 300, 60, $":root {{ color-scheme: {scheme} }}\ninput {{ font: 16px Lato; }}",
            """<input type="search" value="cave" data-focus style="margin: 10px">""");
        var image = PaintReferenceTests.Render(gpu.Device, PaintReferenceTests.Build(fixture), 300, 60);
        var input = PaintReferenceTests.Build(fixture).QuerySelector("input")!;
        var box = input.Box!.ViewportRect;
        // The button's strokes, the right end of the field's content: the lightest and darkest pixels there.
        int lightest = 0, darkest = 255;
        for (var y = (int)box.Y + 4; y < (int)box.Bottom - 4; y++)
            for (var x = (int)box.Right - 18; x < (int)box.Right - 4; x++)
            {
                var p = image[x, y];
                var level = (p.R + p.G + p.B) / 3;
                lightest = Math.Max(lightest, level);
                darkest = Math.Min(darkest, level);
            }
        if (dark)
            Assert.True(lightest > 200, $"the strokes reach {lightest} on the dark field");
        else
            Assert.True(darkest < 120, $"the strokes reach {darkest} on the light field");
    }

    /// <summary>
    /// The number field's spin button in a dark page has Fluent's dark arrows (#9f9f9f, where the light ones are #8b8b8b),
    /// as Chromium's page shows them: the brightest pixel of the button's arrows in the engine's picture and in Chromium's.
    /// (The page's own comparison takes 40 levels, the controls' limit, which the two greys are within.)
    /// </summary>
    [Fact]
    public void TheSpinButtonsArrowsAreTheDarkSchemes()
    {
        var fixture = ColorSchemeFixtures.All.Single(f => f.Name == "color-scheme-focus-number-dark");
        var document = PaintReferenceTests.Build(fixture.Paint);
        var image = PaintReferenceTests.Render(gpu.Device, document, fixture.Width, fixture.Height);
        var golden = Statventure.Imaging.ImageFile.Load(Golden.GoldenPath("ui-color-scheme-focus-number-dark@1"));
        var box = document.GetElementById("focused")!.Box!.ViewportRect;
        int Brightest(Func<int, int, (byte R, byte G, byte B)> pixel)
        {
            var brightest = 0;
            for (var y = (int)box.Y + 3; y < (int)box.Bottom - 3; y++)
                for (var x = (int)box.Right - 18; x < (int)box.Right - 3; x++)
                {
                    var (r, g, b) = pixel(x, y);
                    // The arrows are grey; the field's white text and the ring are left of them or around them.
                    if (r == g && g == b && r < 0xc0)
                        brightest = Math.Max(brightest, r);
                }
            return brightest;
        }
        var engine = Brightest((x, y) => (image[x, y].R, image[x, y].G, image[x, y].B));
        var chromium = Brightest((x, y) => (golden[x, y].R, golden[x, y].G, golden[x, y].B));
        Assert.Equal(0x9f, chromium);
        Assert.Equal(chromium, engine);
    }

    /// <summary>
    /// Under a dark root the page's base is the dark scheme's canvas, #121212, unless the document's base is transparent
    /// (LocalFrameView::BaseBackgroundColor with UseColorAdjustBackground::kIfBaseNotTransparent).
    /// </summary>
    [Fact]
    public void ADarkRootsBaseIsTheDarkCanvas()
    {
        var document = Document(":root { color-scheme: dark; }", "<p>text</p>");
        Assert.Equal(Color.FromBytes(0x12, 0x12, 0x12), document.UsedBaseBackground);
        document.BaseBackground = Color.Transparent;
        Assert.Equal(Color.Transparent, document.UsedBaseBackground);
        var light = Document(":root { color-scheme: light dark; } @media (prefers-color-scheme: dark) { :root { color-scheme: light; } }", "<p>text</p>");
        Assert.Equal(Color.FromBytes(255, 255, 255), light.UsedBaseBackground);
    }
}
