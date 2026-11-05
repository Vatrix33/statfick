using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Ui;

/// <summary>
/// Transformed boxes at fractional positions, as Chromium 152 paints them (Golden/ui-offset-*@1.png
/// and @1.65.png, made by Tools/Scripts/make-scaled-paint-references.sh 1 1 offset-fixtures.json and
/// 1.65 1 offset-fixtures.json). Blink rounds a transformed box's paint offset to a whole pixel (its
/// paint offset translation) and keeps the fraction (the subpixel accumulation) only along the axes
/// the transform doesn't scale; under will-change: transform it keeps none, under a transform
/// animation all of it or none (PaintPropertyTreeBuilder::UpdateForPaintOffsetTranslation,
/// CanPropagateSubpixelAccumulation): the box and everything in it are snapped from there.
/// </summary>
[Collection("GPU")]
public class TransformOffsetReferenceTests(GpuFixture gpu)
{
    const string BoxesCss = """
        body { background: #fff; }
        .t { position: absolute; transform-origin: 0 0; }
        .card { width: 80px; height: 44px; border: 2px solid #222; background: #f2d98a; }
        .chip { position: absolute; left: 5.5px; top: 6.3px; width: 24px; height: 12px; border: 1px solid #7a2a2a; background: #e8eef8; }
        .label { position: absolute; left: 6.5px; top: 22.4px; font: 12px Lato; color: #222; }
        .meter { position: absolute; width: 120px; height: 6px; background: rgba(40, 30, 20, 0.12); }
        .meter span { display: block; height: 100%; background: #a55444; transform-origin: left; }
        .bar { position: absolute; width: 160px; height: 2px; background: #c58cff33; }
        .bar span { display: block; height: 100%; background: #8a4fd0; transform-origin: left; }
        """;

    /// <summary>
    /// Transforms painted with the page: a uniform scale, one-axis scales, a translation (the
    /// fraction kept), a scaled box in a scaled box, the game's meters and the boss
    /// intro's progress bar (a span scaled along x in a box at a fractional place), an untransformed
    /// card, and a scaled card at a negative half pixel (rounded half up).
    /// </summary>
    const string BoxesHtml = """
        <div class="t card" style="left: 20.5px; top: 20.3px; transform: scale(0.8)"><div class="chip"></div><div class="label">Ab</div></div>
        <div class="t card" style="left: 140.3px; top: 20.6px; transform: scaleX(0.6)"><div class="chip"></div></div>
        <div class="t card" style="left: 260.7px; top: 20.4px; transform: scaleY(0.6)"><div class="chip"></div></div>
        <div class="t card" style="left: 380.5px; top: 20.5px; transform: translate(0.3px, 0.4px)"><div class="chip"></div><div class="label">Ab</div></div>
        <div class="t" style="left: 20.5px; top: 120.5px; width: 130px; height: 70px; background: #e0e8f0; transform: scaleX(0.9)"><div class="t card" style="left: 10.3px; top: 8.6px; transform: scale(0.8)"><div class="chip"></div></div></div>
        <div class="meter" style="left: 200.5px; top: 120.3px"><span style="transform: scaleX(0.42)"></span></div>
        <div class="meter" style="left: 200.5px; top: 140.6px"><span style="transform: scaleX(0.75)"></span></div>
        <div class="bar" style="left: 200.3px; top: 160.5px"><span style="transform: scaleX(0.3)"></span></div>
        <div class="t card" style="left: 380.5px; top: 120.5px"><div class="chip"></div><div class="label">Ab</div></div>
        <div class="t card" style="left: -40.5px; top: 205.5px; transform: scale(0.8)"><div class="chip" style="left: 50.5px"></div></div>
        """;

    const string LayersCss = """
        body { background: #fff; }
        .layer { position: absolute; transform-origin: 0 0; width: 80px; height: 44px; background: #2a4a7a; }
        .layer i { position: absolute; left: 5.5px; top: 6.3px; width: 24px; height: 12px; background: #f2d98a; }
        @keyframes grow { from { transform: scale(0.5); } to { transform: scale(1); } }
        @keyframes slide { from { transform: translate(0, 0); } to { transform: translate(10px, 5px); } }
        @keyframes mixed { from { transform: translate(2px, 1px); } to { transform: scale(0.8); } }
        """;

    /// <summary>
    /// Transforms held to where their edges are (<see cref="Layers"/>), not to their pixels: those
    /// Chromium composites into layers of their own, will-change: transform on a scale and on a
    /// translation (no fraction kept), a scale animation (none kept) and a translation animation
    /// (all kept), each paused (Chromium rasters such a layer by itself and draws it through its
    /// transform, which the engine doesn't: it draws through the transform directly), and a
    /// rotation (none kept), whose slanting edges Skia fills by area where the canvas ramps them
    /// (both in Engine-Plan.md, M5's "Known differences left").
    /// </summary>
    const string LayersHtml = """
        <div class="layer" style="left: 20.5px; top: 20.3px; transform: scale(0.8); will-change: transform"><i></i></div>
        <div class="layer" style="left: 140.5px; top: 20.4px; animation: grow 10s linear -6s infinite paused"><i></i></div>
        <div class="layer" style="left: 260.5px; top: 20.6px; animation: slide 10s linear -3.3s infinite paused"><i></i></div>
        <div class="layer" style="left: 380.5px; top: 20.5px; transform: translate(0.3px, 0.4px); will-change: transform"><i></i></div>
        <div class="layer" style="left: 500.4px; top: 20.7px; transform: rotate(8deg)"><i></i></div>
        <div class="layer" style="left: 620.5px; top: 20.5px; animation: mixed 10s linear infinite paused"><i></i></div>
        <div class="layer" style="left: -40.5px; top: 60.5px; transform: scale(0.8)"><i style="left: 55.5px"></i></div>
        """;

    public static readonly PaintFixture[] All =
    [
        new("offset-boxes", 640, 260, BoxesCss, BoxesHtml),
        new("offset-measured", 720, 120, LayersCss, LayersHtml),
    ];

    static readonly float[] Scales = [1, 1.65f];

    public static TheoryData<float> ScaleData => new(Scales);

    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "offset-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html,
            scrollX = f.ScrollX, scrollY = f.ScrollY, windowFocused = f.WindowFocused,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    RgbaImage Render(PaintFixture fixture, float scale)
    {
        var document = PaintReferenceTests.Build(fixture, scale);
        return PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
    }

    /// <summary>The transforms painted with the page, against Chromium's pixels.</summary>
    [Theory]
    [MemberData(nameof(ScaleData))]
    public void TheBoxesSitWhereChromiumPutsThem(float scale)
    {
        var fixture = All.Single(f => f.Name == "offset-boxes");
        Golden.Match($"ui-{fixture.Name}@{Number(scale)}", Render(fixture, scale), tolerance: fixture.Tolerance, maxDifferentRatio: 0.002);
    }

    /// <summary>A measured card: its place (CSS px), its transform (a scale, then a rotation, then a translation).</summary>
    readonly record struct Layer(string Name, float Left, float Top, float ScaleX, float ScaleY, float X, float Y, float Degrees = 0, float ChipLeft = 5.5f)
    {
        /// <summary>A point of the card (CSS px from its corner) on the page.</summary>
        public Vector2 At(float x, float y)
        {
            var angle = Degrees * MathF.PI / 180;
            var scaled = new Vector2(x * ScaleX, y * ScaleY);
            return new Vector2(Left + X, Top + Y) + new Vector2(scaled.X * MathF.Cos(angle) - scaled.Y * MathF.Sin(angle), scaled.X * MathF.Sin(angle) + scaled.Y * MathF.Cos(angle));
        }
    }

    static readonly Layer[] Layers =
    [
        new("will-change scale(0.8)", 20.5f, 20.3f, 0.8f, 0.8f, 0, 0),
        new("scale animation", 140.5f, 20.4f, 0.8f, 0.8f, 0, 0),
        new("translation animation", 260.5f, 20.6f, 1, 1, 3.3f, 1.65f),
        new("will-change translate", 380.5f, 20.5f, 1, 1, 0.3f, 0.4f),
        new("rotate(8deg)", 500.4f, 20.7f, 1, 1, 0, 0, 8),
        new("animation from a translation to a scale, on the translation", 620.5f, 20.5f, 1, 1, 2, 1),
        // Rounded half up (to -40), not away from zero: Blink's LayoutUnit::Round.
        new("scale(0.8) at -40.5 px", -40.5f, 60.5f, 0.8f, 0.8f, 0, 0, 0, 55.5f),
    ];

    /// <summary>
    /// The measured cards' edges (the card's left and top, its chip's left and top; of the rotated
    /// card only the left ones, which run nearly down the screen) where Chromium has them, each
    /// measured across a few pixels as the coverage between the two colours summed (blurring by a
    /// layer's raster, or covering by area instead of a ramp, keeps that sum): within a tenth of a
    /// pixel (the user approved 0.1 px on 2026-11-05). A box that kept the fraction it should drop
    /// (or dropped one it should keep) is 0.3 to 1 px off.
    /// </summary>
    [Theory]
    [MemberData(nameof(ScaleData))]
    public void TheLayersSitWhereChromiumPutsThem(float scale)
    {
        var fixture = All.Single(f => f.Name == "offset-measured");
        var image = Render(fixture, scale);
        var reference = ImageFile.Load(Golden.GoldenPath($"ui-{fixture.Name}@{Number(scale)}"));
        Vector3 white = new(255, 255, 255), card = new(0x2a, 0x4a, 0x7a), chip = new(0xf2, 0xd9, 0x8a);
        var problems = new List<string>();
        foreach (var layer in Layers)
        {
            var cardLeft = layer.At(0, 30);
            var cardTop = layer.At(60, 0);
            var chipLeft = layer.At(layer.ChipLeft, 12.3f);
            var chipTop = layer.At(layer.ChipLeft + 12, 6.3f);
            var edges = new List<(string What, bool Row, float Line, float At, Vector3 Outside, Vector3 Inside)>
            {
                ("card's left", true, cardLeft.Y, cardLeft.X, white, card),
                ("chip's left", true, chipLeft.Y, chipLeft.X, card, chip),
            };
            if (layer.Degrees == 0)
            {
                edges.Add(("card's top", false, cardTop.X, cardTop.Y, white, card));
                edges.Add(("chip's top", false, chipTop.X, chipTop.Y, card, chip));
            }
            foreach (var (what, row, line, at, outside, inside) in edges)
            {
                var fixedLine = (int)MathF.Floor(line * scale);
                var from = (int)MathF.Floor((at - 2) * scale);
                var to = (int)MathF.Floor((at + 3) * scale);
                // (An edge off the page isn't measured: the card at -40.5 px has its left edge there.)
                if (from < 0)
                    continue;
                var want = Edge(reference, row, fixedLine, from, to, outside, inside);
                var got = Edge(image, row, fixedLine, from, to, outside, inside);
                if (MathF.Abs(got - want) > 0.1f)
                    problems.Add($"{layer.Name}, {what}: {got:F3} px, Chromium {want:F3}");
            }
        }
        // The render goes to Actual/ for review when it fails (and with STATVENTURE_KEEP_RENDERS=1, as Golden.Match does).
        if (problems.Count > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(Golden.ActualPath($"ui-{fixture.Name}@{Number(scale)}"), image);
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    const string RichCss = """
        html { overflow: hidden; }
        body { background: #fff; }
        .rich { position: absolute; transform: scale(0.8); transform-origin: 0 0; width: 300px; padding: 4px; border: 2px solid #333;
                background: #fff8e8; box-shadow: 2px 2px 0 #999; font: 13px Lato; color: #222; }
        .rich p { margin: 0 0 3px; }
        .scroller { width: 60px; height: 30px; overflow: scroll; border: 1px solid #666; display: inline-block; }
        .clipped { width: 40px; height: 12px; overflow: hidden; border-radius: 4px; background: #cde; display: inline-block; }
        .outlined { width: 30px; height: 10px; outline: 2px solid #c33; outline-offset: 1px; margin: 4px; display: inline-block; }
        """;

    const string RichHtmlTemplate = """
        <p><u>Ab</u> cd <span style="text-decoration: underline wavy #c33">ef</span></p>
        <details open><summary>Summary</summary></details>
        <p><input type="checkbox" checked> <input type="range" value="40" style="width: 80px"> <select><option>One</option></select></p>
        <p><input type="number" value="3" style="width: 50px"{number}> <input type="search" value="x" style="width: 60px"{search}> <input type="text" value="Hi" style="width: 50px"></p>
        <p><svg width="24" height="14"><rect x="2" y="2" width="18" height="9" fill="#36a"/></svg> <span class="clipped"><span style="display: block; width: 60px; height: 30px; background: #a63"></span></span></p>
        <div class="scroller"><div style="height: 60px; background: linear-gradient(#eee, #999)"></div></div><div class="outlined"></div>
        """;

    /// <summary>The rich card with one of its fields focused (its spin or cancel button and caret show).</summary>
    static string RichHtml(string focused) => RichHtmlTemplate
        .Replace("{number}", focused == "number" ? " data-focus" : "")
        .Replace("{search}", focused == "search" ? " data-focus" : "");

    public static TheoryData<float, string> RichCases => new()
    {
        { 1, "number" },
        { 1.65f, "number" },
        { 1, "search" },
        { 1.65f, "search" },
    };

    /// <summary>
    /// A box under a scale drops the fraction of its paint offset along both axes, so it paints the
    /// same, every part of it (its decorations and shadow, text and its decorations, a disclosure
    /// marker, form controls and their text, the focused number field's spin button or search
    /// field's cancel button and its caret, an inline SVG, a clipped box, a scroller's scrollbars,
    /// an outline), from any place that rounds to the same pixel: every pixel equal. The places
    /// span most of that pixel (0.56 px at 1×, 0.92 device px at 1.65), so a position snapped from
    /// layout's place rather than the rounded one crosses a half pixel between two of them, whatever
    /// its own fraction.
    /// </summary>
    [Theory]
    [MemberData(nameof(RichCases))]
    public void AScaledBoxPaintsTheSameFromPlacesThatRoundAlike(float scale, string focused)
    {
        RgbaImage At(float place) => Render(new PaintFixture("rich", 320, 220, RichCss, $"""<div class="rich" style="left: {Number(place)}px; top: {Number(place)}px">{RichHtml(focused)}</div>"""), scale);
        // 19.72 to 20.28 px round to 20 at 1× and to 33 device pixels at 1.65.
        float[] places = [19.72f, 19.9f, 20.1f, 20.28f];
        var first = At(places[0]);
        var problems = new List<string>();
        foreach (var place in places.Skip(1))
        {
            var other = At(place);
            var different = 0;
            for (var i = 0; i < first.Pixels.Length; i += 4)
                if (first.Pixels.AsSpan(i, 4).SequenceEqual(other.Pixels.AsSpan(i, 4)) == false)
                    different++;
            if (different == 0)
                continue;
            problems.Add($"at {Number(place)} px {different} pixels differ from {Number(places[0])} px");
            ImageFile.SavePng(Golden.ActualPath($"rich-{focused}-{Number(places[0])}@{Number(scale)}"), first);
            ImageFile.SavePng(Golden.ActualPath($"rich-{focused}-{Number(place)}@{Number(scale)}"), other);
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems) + $" (renders in Actual/rich-{focused}-*@{Number(scale)}.png)");
    }

    /// <summary>
    /// Hit testing follows the painted places (Blink hit tests through the paint property trees,
    /// rounded paint offsets and all): the rich card at places that round alike takes the same
    /// element at every point of a half-pixel grid over it, its inline SVG's shapes included (they
    /// are tested later, in the space the SVG was painted in).
    /// </summary>
    [Fact]
    public void AScaledBoxHitTestsTheSameFromPlacesThatRoundAlike()
    {
        (Vector2 Point, string Hit)[] HitsAt(float place)
        {
            var fixture = new PaintFixture("rich", 320, 220, RichCss, $"""<div class="rich" style="left: {Number(place)}px; top: {Number(place)}px">{RichHtml("number")}</div>""");
            var document = PaintReferenceTests.Build(fixture);
            PaintReferenceTests.Render(gpu.Device, document, fixture.Width, fixture.Height);
            var elements = document.AllElements().ToList();
            var hits = new List<(Vector2, string)>();
            for (var y = 18.25f; y < 200; y += 0.5f)
                for (var x = 18.25f; x < 270; x += 0.5f)
                    hits.Add((new Vector2(x, y), document.HitTest(new Vector2(x, y)) is { } hit ? $"{hit.Element.Tag} #{elements.IndexOf(hit.Element)}" : "nothing"));
            return hits.ToArray();
        }
        var first = HitsAt(19.72f);
        foreach (var place in new[] { 20.1f, 20.28f })
        {
            var other = HitsAt(place);
            var different = first.Zip(other).Where(pair => pair.First.Hit != pair.Second.Hit).ToList();
            Assert.True(different.Count == 0, $"at {Number(place)} px {different.Count} points hit another element than at 19.72 px, as "
                + string.Join("; ", different.Take(8).Select(d => $"({d.First.Point.X}, {d.First.Point.Y}): {d.First.Hit}, then {d.Second.Hit}")));
        }
    }

    const string TransitionCss = """
        html { overflow: hidden; }
        body { background: #fff; }
        .tr { position: absolute; transform-origin: 0 0; width: 80px; height: 44px; background: #2a4a7a; transform: translate(2px, 1px); transition: transform 10s linear; }
        .tr.scaled { transform: scale(0.8); }
        .tr i { position: absolute; left: 5.5px; top: 6.3px; width: 24px; height: 12px; background: #f2d98a; }
        """;

    /// <summary>
    /// A transition counts with the transform animations: a box whose transform transition runs from a
    /// translation to a scale, seen at its start (the translation shown), drops the fraction, so it
    /// paints the same from places that round alike (Chromium's harness can't hold a transition, so
    /// this is the engine's rule against itself).
    /// </summary>
    [Theory]
    [MemberData(nameof(ScaleData))]
    public void ABoxInATransitionToAScaleDropsTheFraction(float scale)
    {
        RgbaImage At(float place)
        {
            var fixture = new PaintFixture("transition", 200, 100, TransitionCss, $"""<div class="tr" style="left: {Number(place)}px; top: {Number(place)}px"><i></i></div>""");
            var document = PaintReferenceTests.Build(fixture, scale);
            document.AllElements().Single(e => e.GetAttribute("class") == "tr").SetAttribute("class", "tr scaled");
            document.Update(0);
            return PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        }
        var first = At(20.1f);
        var second = At(20.25f);
        Assert.True(first.Pixels.AsSpan().SequenceEqual(second.Pixels), "the box in a transition paints differently from places that round alike");
    }

    /// <summary>
    /// Where an edge between two colours lies along a row (or a column) of pixels from..to (the
    /// first fully outside, the last fully inside): from plus the pixels' summed share of the
    /// outside colour (each pixel's colour projected on the line between the two).
    /// </summary>
    static float Edge(RgbaImage image, bool row, int line, int from, int to, Vector3 outside, Vector3 inside)
    {
        var span = inside - outside;
        var sum = 0f;
        for (var i = from; i <= to; i++)
        {
            var (x, y) = row ? (i, line) : (line, i);
            var p = (y * image.Width + x) * 4;
            var color = new Vector3(image.Pixels[p], image.Pixels[p + 1], image.Pixels[p + 2]);
            sum += 1 - Math.Clamp(Vector3.Dot(color - outside, span) / span.LengthSquared(), 0, 1);
        }
        return from + sum;
    }
}
