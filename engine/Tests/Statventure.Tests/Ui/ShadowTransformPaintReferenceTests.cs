using System.Globalization;
using System.Text;
using System.Text.Json;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Ui;

/// <summary>
/// Blurred box shadows under transforms that scale one axis more than the other (the boss intro's
/// progress bar, `box-shadow: 0 0 12px` under an animated `scaleX`), as Chromium 152 paints them
/// (Golden/ui-shadow-*@1.png and @1.65.png, made by Tools/Scripts/make-scaled-paint-references.sh 1 1
/// shadow-fixtures.json and 1.65 1 shadow-fixtures.json: under X11, CPU raster). Blink draws a
/// shadow with a draw looper whose blur respects the transform (`kShadowRespectsTransforms`), so
/// Skia's blur mask filter takes its sigma through the whole matrix with one factor,
/// `SkMatrix::mapRadius` (the geometric mean of the lengths the matrix gives a unit step along x
/// and along y), and blurs the box as the transform places it on the device, the same in every
/// direction. Every transformed box sits at a multiple of 20 CSS px and its scaled sides are
/// multiples of 20 too, whole device pixels at 1.65, so neither the paint offset's rounding nor
/// Blink's one-pixel shadow clip inset (known differences) shows. The card's bar is captured static
/// and under its paused CSS animation (a composited layer in Chromium): the glow is the same in both,
/// only the bar's two ends differ at 1× (the layer's ends resampled, 2 pixels a row, up to 46 levels),
/// none at 1.65. Before the rule, shadow-blurs had 4.5 % of its pixels beyond 12 levels, the card 0.86 %
/// (1×) and 0.83 % (1.65); after, 0.09 % / 0.15 % and none; with the inset shadow cast from Blink's frame
/// too, shadow-blurs 0.02 % / 0.05 %, shadow-scale-x 0.17 % / 0.16 % (0.33 % / 0.26 % without it),
/// shadow-scale-y 0.21 % / 0.17 % and shadow-rotate 0.35 % / 0.21 % (0.59 % / 0.47 %).
/// </summary>
[Collection("GPU")]
public class ShadowTransformPaintReferenceTests(GpuFixture gpu)
{
    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>The shadows each fixture puts under every transform: outer, offset and spread, inset, inset offset and spread, rounded, around a box with no background.</summary>
    const string KindsCss = """
        body { background: #fff; }
        div { position: absolute; transform-origin: 0 0; }
        .o { background: #c8b48a; box-shadow: 0 0 12px #1e2a50; }
        .os { background: #c8b48a; box-shadow: 6px 4px 12px 3px #6a2a8a; }
        .i { background: #e8eef8; box-shadow: inset 0 0 12px #102040; }
        .is { background: #f2e8d0; box-shadow: inset 5px 3px 12px 2px #803020; }
        .r { background: #c8b48a; border-radius: 10px; box-shadow: 0 0 12px #1e2a50; }
        .e { box-shadow: 0 0 12px 2px #2a7a4a; }
        """;

    static readonly string[] Kinds = ["o", "os", "i", "is", "r", "e"];

    /// <summary>
    /// Boxes 100 × 40 with the six shadows (rows) under none, a uniform scale(0.6), scaleX(0.2),
    /// scaleX(0.6) and scaleX(2) (columns), blur 12 px (σ 6).
    /// </summary>
    static PaintFixture ScaleX()
    {
        var html = new StringBuilder();
        (string Transform, int Left)[] columns = [("none", 40), ("scale(0.6)", 200), ("scaleX(0.2)", 320), ("scaleX(0.6)", 400), ("scaleX(2)", 540)];
        var top = 40;
        foreach (var kind in Kinds)
        {
            foreach (var (transform, left) in columns)
                html.Append($"""<div class="{kind}" style="left: {left}px; top: {top}px; width: 100px; height: 40px; transform: {transform}"></div>""");
            top += 100;
        }
        return new PaintFixture("shadow-scale-x", 800, 620, KindsCss, html.ToString());
    }

    /// <summary>The same across: boxes 40 × 100 under scaleY (rows), the six shadows (columns).</summary>
    static PaintFixture ScaleY()
    {
        var html = new StringBuilder();
        (string Transform, int Top)[] rows = [("none", 40), ("scale(0.6)", 200), ("scaleY(0.2)", 320), ("scaleY(0.6)", 400), ("scaleY(2)", 540)];
        foreach (var (transform, top) in rows)
        {
            var left = 40;
            foreach (var kind in Kinds)
            {
                html.Append($"""<div class="{kind}" style="left: {left}px; top: {top}px; width: 40px; height: 100px; transform: {transform}"></div>""");
                left += 100;
            }
        }
        return new PaintFixture("shadow-scale-y", 620, 800, KindsCss, html.ToString());
    }

    /// <summary>Blur 4 px and 24 px (σ 2 and 12), outer with and without spread and offset, inset, under none, scale(0.6) and scaleX 0.2, 0.6 and 2.</summary>
    static PaintFixture Blurs()
    {
        const string css = """
            body { background: #fff; }
            div { position: absolute; transform-origin: 0 0; width: 100px; height: 40px; }
            .o4 { background: #c8b48a; box-shadow: 0 0 4px #1e2a50; }
            .o24 { background: #c8b48a; box-shadow: 0 0 24px #1e2a50; }
            .os24 { background: #c8b48a; box-shadow: -8px 6px 24px 6px #6a2a8a; }
            .i4 { background: #e8eef8; box-shadow: inset 0 0 4px #102040; }
            .i24 { background: #f2e8d0; box-shadow: inset 4px -3px 24px 3px #803020; }
            """;
        var html = new StringBuilder();
        (string Transform, int Left)[] columns = [("none", 60), ("scale(0.6)", 260), ("scaleX(0.2)", 420), ("scaleX(0.6)", 540), ("scaleX(2)", 700)];
        var top = 60;
        foreach (var kind in new[] { "o4", "o24", "os24", "i4", "i24" })
        {
            foreach (var (transform, left) in columns)
                html.Append($"""<div class="{kind}" style="left: {left}px; top: {top}px; transform: {transform}"></div>""");
            top += 140;
        }
        return new PaintFixture("shadow-blurs", 960, 760, css, html.ToString());
    }

    /// <summary>
    /// Rotations and shears (columns): a uniform rotation (the sigma unchanged), a rotation after
    /// scaleX(0.4), and two transforms that shear the box's axes (a scale after a rotation, a skew),
    /// where mapRadius isn't the determinant's root; outer, offset and spread, inset and rounded
    /// (rows). The boxes turn about their centres.
    /// </summary>
    static PaintFixture Rotate()
    {
        const string css = """
            body { background: #fff; }
            div { position: absolute; width: 100px; height: 40px; transform-origin: 50% 50%; }
            .o { background: #c8b48a; box-shadow: 0 0 12px #1e2a50; }
            .os { background: #c8b48a; box-shadow: 6px 4px 12px 3px #6a2a8a; }
            .i { background: #e8eef8; box-shadow: inset 0 0 12px #102040; }
            .r { background: #c8b48a; border-radius: 10px; box-shadow: 0 0 12px #1e2a50; }
            """;
        var html = new StringBuilder();
        (string Transform, int Left)[] columns = [("rotate(25deg)", 80), ("rotate(30deg) scaleX(0.4)", 340), ("scale(2, 0.5) rotate(30deg)", 600), ("skewX(25deg)", 860)];
        var top = 80;
        foreach (var kind in new[] { "o", "os", "i", "r" })
        {
            foreach (var (transform, left) in columns)
                html.Append($"""<div class="{kind}" style="left: {left}px; top: {top}px; transform: {transform}"></div>""");
            top += 180;
        }
        return new PaintFixture("shadow-rotate", 1040, 760, css, html.ToString());
    }

    /// <summary>The boss intro card's progress bar (Data/Cutscenes/ui, the old stylesheet's rules): its track and bar at the card's place, scaleX(0.2) and scaleX(0.6).</summary>
    const string CardCss = """
        body { background: #05030a; }
        .track { position: absolute; left: 84px; width: 1032px; height: 2px; background: #c58cff33; }
        .track span { display: block; height: 100%; background: #c58cff; box-shadow: 0 0 12px #c58cff; transform-origin: left; }
        @keyframes bi-progress { from { transform: scaleX(0); } to { transform: scaleX(1); } }
        """;

    /// <summary>The bar under a static transform.</summary>
    static PaintFixture Card() => new("shadow-card", 1200, 200, CardCss, """
        <div class="track" style="top: 60px"><span style="transform: scaleX(0.2)"></span></div>
        <div class="track" style="top: 140px"><span style="transform: scaleX(0.6)"></span></div>
        """);

    /// <summary>The bar as the card has it, under its CSS animation (paused at 2 s and 6 s of 10 s: scaleX(0.2) and 0.6), which Chromium may draw as a composited layer.</summary>
    static PaintFixture CardAnimated() => new("shadow-card-animated", 1200, 200, CardCss, """
        <div class="track" style="top: 60px"><span style="animation: bi-progress 10s linear -2s both paused"></span></div>
        <div class="track" style="top: 140px"><span style="animation: bi-progress 10s linear -6s both paused"></span></div>
        """);

    public static readonly PaintFixture[] All = [ScaleX(), ScaleY(), Blurs(), Rotate(), Card(), CardAnimated()];

    /// <summary>
    /// Two of Chromium's rules these fixtures show beside the sigma are known differences (the user's decision
    /// of 2026-11-05, Engine-Plan.md, "Decision on blurred shadows under non-uniform transforms"): Blink gives
    /// a box whose transform is a 2D scale an effect node of its own (NeedsEffectFor2DScaleTransform), drawn
    /// as a layer bounded by what the box paints in its own units, which cuts a shadow blurred wider than that
    /// on the device; and Skia blurs a rectangle with its edges rounded to whole pixels (SkBlurMask::BlurRect),
    /// so a glow around a fractional edge sits up to half a pixel off. The third, Blink's inset shadow cast
    /// from a frame the blur radius wide (AreaCastingShadowInHole), is ported. shadow-scale-y at 1× keeps
    /// 0.214 % of its pixels beyond 12 levels, past the paint fixtures' 0.2 % (1,059 against 992): the two
    /// scaleY(0.2) boxes with a spread (598 pixels: the 2D-scale effect cut and Skia's rect-blur rounding),
    /// the offset and spread under scale(0.6) (276: the rounding) and the rounded box's corners (178,
    /// untransformed alike: Skia blurs a round rect's mask, the canvas Wallace's closed form). The user
    /// approved 0.25 % for it at 1× only (2026-11-05); at 1.65 it keeps 0.2 %.
    /// </summary>
    static double MaxDifferentRatio(string name, float scale) => name switch
    {
        // box-slant's 1 %: its slanted box edges, a known difference (the user's decision of 2026-11-05).
        "shadow-rotate" => 0.01,
        // The 2D-scale effect cut, the rect-blur rounding and the rounded-corner blur (above; the user's decision of 2026-11-05).
        "shadow-scale-y" when scale == 1 => 0.0025,
        _ => 0.002,
    };

    static readonly float[] Scales = [1, 1.65f];

    public static TheoryData<string, float> Fixtures
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in All)
                foreach (var scale in Scales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "shadow-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html,
            scrollX = f.ScrollX, scrollY = f.ScrollY, windowFocused = f.WindowFocused,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheShadowsLookAsInChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var document = PaintReferenceTests.Build(fixture, scale);
        var image = PaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
        Golden.Match($"ui-{name}@{Number(scale)}", image, tolerance: fixture.Tolerance, maxDifferentRatio: MaxDifferentRatio(name, scale));
    }
}
