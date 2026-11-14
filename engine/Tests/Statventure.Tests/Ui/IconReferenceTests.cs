using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Statventure.Core;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Text;
using Statventure.Text;
using Statventure.UI.Dom;
using Statventure.UI.Layout;

namespace Statventure.Tests.Ui;

/// <summary>A cell of an icon fixture: text of icons in a font size, weight and colour on a ground, maybe faded, turned or with a fill colour.</summary>
public sealed record IconCell(string Text, float Size, int Weight, string Color, string Ground, float Opacity = 1, string? Transform = null, string? FillColor = null);

/// <summary>An icon fixture: cells of a size (CSS px, multiples of 20: whole device pixels at 1 and 1.65) in rows of <paramref name="Columns"/>.</summary>
public sealed record IconFixture(string Name, int Columns, int CellWidth, int CellHeight, IconCell[] Cells)
{
    public int Width => Columns * CellWidth;
    public int Height => (Cells.Length + Columns - 1) / Columns * CellHeight;
}

/// <summary>
/// The game's icons as the engine draws them against Chromium filling the same outlines (GM1 step 7; Docs/Game/
/// Foundation.md, "How it works", step 7). The engine draws each cell's text (the icons in place of the symbols); the
/// fixture Chromium renders (<c>Reference/icon-fixtures.json</c>, written by <see cref="TheFixtureListIsUpToDate"/>, one
/// per fixture and scale) holds the same cells with each icon as an inline SVG of its outline in the cell's colour,
/// placed where the engine's glyph lands (its pen on the snapped baseline at the quarter pixel, at the text's size as
/// the glyph is made): the drawing is what's compared, at 1 and 1.65, within the limits of the inline SVG paint
/// fixture (<c>svg-transforms</c>: 60 levels, 0.2 % of the pixels; the canvas keeps its own path antialiasing, the user's
/// decision of 2026-10-25). Where the glyph lands is checked against the old game's captures in
/// <see cref="IconLayoutTests"/>. Captured by <c>Tools/Scripts/make-icon-references.sh</c>.
/// </summary>
[Collection("GPU")]
public class IconReferenceTests(GpuFixture gpu)
{
    static readonly float[] Scales = [1, 1.65f];

    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    static string Css(double value) => value.ToString("0.####", CultureInfo.InvariantCulture);

    static string ToolsDirectory([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", "..", "Tools"));

    // ---------- The fixtures ----------

    /// <summary>Every use of a symbol in the old game (step 3's <c>Tools/IconSheet/uses.json</c>): its size, weight, colour, ground, opacity and turn in the light theme.</summary>
    static IconFixture Uses()
    {
        using var uses = JsonDocument.Parse(File.ReadAllText(Path.Combine(ToolsDirectory(), "IconSheet", "uses.json")));
        var cells = new List<IconCell>();
        foreach (var use in uses.RootElement.GetProperty("uses").EnumerateArray())
        {
            var font = use.GetProperty("font").GetString()!.Split(' ');
            var size = float.Parse(font[2].TrimEnd('p', 'x'), CultureInfo.InvariantCulture);
            var light = use.GetProperty("light");
            cells.Add(new IconCell(use.GetProperty("symbol").GetString()!, size, int.Parse(font[1], CultureInfo.InvariantCulture),
                light.GetProperty("color").GetString()!, light.GetProperty("ground").GetString()!, light.GetProperty("opacity").GetSingle(),
                light.TryGetProperty("transform", out var transform) ? transform.GetString() : null));
        }
        return new IconFixture("icon-uses", 10, 100, 100, [.. cells]);
    }

    /// <summary>The 28 large, in the ink on the parchment.</summary>
    static IconFixture Large() =>
        new("icon-large", 7, 160, 180, [.. IconFiles.All.Select(i => new IconCell(i.Character, 96, 400, "#22160d", "#f3e8cc"))]);

    /// <summary>
    /// Icons in runs (pens at quarter pixels), the fill colour, opacity, a turn, a fractional size, the smallest bold
    /// uses together, the dark theme's ink.
    /// </summary>
    static IconFixture Rules() => new("icon-rules", 4, 200, 100,
    [
        new("◆◇★☆", 24, 400, "#34523a", "#f3e8cc"),
        new("⚙✦⌖", 31, 400, "#cc0000", "#f3e8cc", FillColor: "#0066cc"),
        new("♜♡◈", 27, 400, "#7a2a1d", "#f3e8cc", Opacity: 0.5f),
        new("⚔↶", 40, 400, "#22160d", "#efe6d0", Transform: "rotate(30deg)"),
        new("✧✧◫", 19.2f, 400, "#22160d", "#f3e8cc"),
        new("▲▼◆◎⚙✸", 9, 700, "#ffcf8a", "#3c4448"),
        new("♟◫◌☰", 23.5f, 400, "#c49a56", "#261c15"),
        new("↺↻⇧⌄♛✺", 17, 700, "#9cbca1", "#261c15"),
    ]);

    static readonly Lazy<IconFixture[]> Fixtures = new(() => [Uses(), Large(), Rules()]);

    public static TheoryData<string, float> Cases
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in Fixtures.Value)
                foreach (var scale in Scales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    // ---------- The engine's side ----------

    static readonly Lazy<FontLibrary> Fonts = new(() =>
    {
        var fonts = new FontLibrary();
        var dir = Path.Combine(AppContext.BaseDirectory, "Data", "Fonts");
        fonts.AddFile(Path.Combine(dir, "Lato-Regular.ttf"), "Lato", 400);
        fonts.AddFile(Path.Combine(dir, "Lato-Semibold.ttf"), "Lato", 500, weightMax: 600);
        fonts.AddFile(Path.Combine(dir, "Lato-Bold.ttf"), "Lato", 700, weightMax: 900);
        fonts.AddIcons(IconFiles.All.Select(i => i.Glyph));
        return fonts;
    });

    // (No scrollbars: the turned cell reaches past the page, which Chromium would scroll.)
    const string Base = "html { overflow: hidden; } html, body { margin: 0; background: #fff; } .c { position: absolute; box-sizing: border-box; padding-left: 20px; font-family: Lato; white-space: pre; } .c > svg { position: absolute; }";

    /// <summary>A cell's box and looks (the same on both sides); <paramref name="color"/> the text's or, for Chromium, the colour it fills.</summary>
    static string CellStyle(IconFixture fixture, int index, string color, string? fillColor)
    {
        var cell = fixture.Cells[index];
        var style = new StringBuilder();
        style.Append($"left:{index % fixture.Columns * fixture.CellWidth}px;top:{index / fixture.Columns * fixture.CellHeight}px;");
        style.Append($"width:{fixture.CellWidth}px;height:{fixture.CellHeight}px;line-height:{fixture.CellHeight}px;");
        style.Append($"font-size:{Css(cell.Size)}px;font-weight:{cell.Weight};color:{color};background:{cell.Ground};");
        if (cell.Opacity != 1)
            style.Append($"opacity:{Css(cell.Opacity)};");
        if (cell.Transform is { } transform)
            style.Append($"transform:{transform};");
        if (fillColor is { } fill)
            style.Append($"-webkit-text-fill-color:{fill};");
        return style.ToString();
    }

    static string EngineHtml(IconFixture fixture) =>
        string.Concat(fixture.Cells.Select((cell, i) => $"<div class=c style=\"{CellStyle(fixture, i, cell.Color, cell.FillColor)}\">{cell.Text}</div>"));

    public static UiDocument Build(IconFixture fixture, float scale)
    {
        var document = new UiDocument(Fonts.Value);
        document.SetViewport(fixture.Width, fixture.Height);
        document.SetZoom(scale);
        document.AddStylesheet(Base, fixture.Name);
        Markup.SetInnerHtml(document.Body, EngineHtml(fixture));
        document.Update(0);
        return document;
    }

    // ---------- Chromium's side ----------

    /// <summary>
    /// Where the engine draws each icon of a cell: the pen and baseline in device pixels from the cell's corner (before
    /// its transform), snapped as the canvas snaps a glyph's (the baseline to a pixel, the pen to the quarter pixel at or
    /// after it) unless the cell is turned, and the size its glyphs are made at (0.01 px, truncated).
    /// </summary>
    static List<(IconGlyph Icon, Vector2 Pen, float Size)> Placed(UiDocument document, UiElement cell, bool turned)
    {
        var box = cell.Box!;
        var corner = box.ViewportPosition;
        var placed = new List<(IconGlyph, Vector2, float)>();
        foreach (var line in box.Inline!.Lines)
            foreach (var item in line.Items)
                if (item.Run is { Font.Face: { IsIconFace: true } face } run)
                {
                    var size = (int)(run.Style.Size * 100f) / 100f;
                    foreach (var glyph in run.Glyphs)
                    {
                        if (face.IconOf(glyph.Glyph) is not { } icon)
                            continue;
                        var pen = corner + new Vector2(item.X + glyph.X, item.Baseline + glyph.Y);
                        if (!turned)
                        {
                            var mapped = pen.X + 1f / 8;
                            var whole = MathF.Floor(mapped);
                            pen = new Vector2(whole + ((int)((mapped - whole + 1f) * 4) & 3) / 4f, MathF.Floor(pen.Y + 0.5f));
                        }
                        placed.Add((icon, pen - corner, size));
                    }
                }
        return placed;
    }

    /// <summary>
    /// An icon as an inline SVG filling its outline (<c>currentColor</c>) at <paramref name="size"/> device pixels to the
    /// em with its pen at <paramref name="pen"/>: a box of whole device pixels around the ink (Blink snaps an svg root's
    /// box to pixels) and a view box that puts the outline's origin at the pen, its CSS lengths a hair over the exact
    /// value so the layout's truncation to 1/64 px gives the whole pixels.
    /// </summary>
    static string Svg(IconGlyph icon, Vector2 pen, float size, float scale)
    {
        var bounds = Statventure.Canvas.Path.ParseSvg(icon.Outline).TightBounds;
        var unit = size / IconGlyph.UnitsPerEm;
        var left = MathF.Floor(pen.X + bounds.Left * unit) - 2;
        var top = MathF.Floor(pen.Y + bounds.Top * unit) - 2;
        var width = MathF.Ceiling(pen.X + bounds.Right * unit) + 2 - left;
        var height = MathF.Ceiling(pen.Y + bounds.Bottom * unit) + 2 - top;
        string Length(float pixels) => Css(Math.Ceiling(pixels / (double)scale * 10000) / 10000);
        string Units(double value) => value.ToString("0.######", CultureInfo.InvariantCulture);
        var viewBox = $"{Units((left - pen.X) / unit)} {Units((top - pen.Y) / unit)} {Units(width / unit)} {Units(height / unit)}";
        return $"<svg viewBox=\"{viewBox}\" preserveAspectRatio=\"none\" style=\"left:{Length(left)}px;top:{Length(top)}px;width:{Length(width)}px;height:{Length(height)}px\">"
            + $"<path fill=\"currentColor\" fill-rule=\"nonzero\" d=\"{icon.Outline}\"/></svg>";
    }

    /// <summary>The fixture as Chromium renders it at a scale: the cells with their icons as inline SVG where the engine draws them.</summary>
    static string ChromiumHtml(IconFixture fixture, float scale)
    {
        var document = Build(fixture, scale);
        var cells = document.Body.Children.OfType<UiElement>().ToList();
        var html = new StringBuilder();
        for (var i = 0; i < fixture.Cells.Length; i++)
        {
            var cell = fixture.Cells[i];
            html.Append($"<div class=c style=\"{CellStyle(fixture, i, cell.FillColor ?? cell.Color, null)}\">");
            foreach (var (icon, pen, size) in Placed(document, cells[i], cell.Transform is not null))
                html.Append(Svg(icon, pen, size, scale));
            html.Append("</div>");
        }
        return html.ToString();
    }

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "icon-fixtures.json");
        var list = Fixtures.Value.SelectMany(f => Scales.Select(scale => new
        {
            name = f.Name, scale, width = f.Width, height = f.Height, css = Base, html = ChromiumHtml(f, scale),
            scrollX = 0, scrollY = 0, windowFocused = true,
        }));
        var json = JsonSerializer.Serialize(list);
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    /// <summary>Every icon of every fixture is drawn, each glyph where its outline is: the engine's runs hold all the cells' characters as icons.</summary>
    [Fact]
    public void EveryCellIsDrawnInIcons()
    {
        foreach (var fixture in Fixtures.Value)
        {
            var document = Build(fixture, 1);
            var cells = document.Body.Children.OfType<UiElement>().ToList();
            for (var i = 0; i < fixture.Cells.Length; i++)
                Assert.Equal(fixture.Cells[i].Text, string.Concat(Placed(document, cells[i], false).Select(p => char.ConvertFromUtf32(p.Icon.CodePoint))));
        }
        Assert.Equal(83, Fixtures.Value[0].Cells.Length);
        Assert.Equal(28, Fixtures.Value[1].Cells.Select(c => c.Text).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void TheIconsDrawAsChromiumFillsTheirOutlines(string name, float scale)
    {
        var fixture = Fixtures.Value.Single(f => f.Name == name);
        var document = Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{name}@{Number(scale)}", image, tolerance: 60, maxDifferentRatio: 0.002);
    }
}
