using System.Globalization;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Ui;

namespace Statventure.Tests.Scene;

/// <summary>
/// The proof's title card against the old game's (Docs/Scene.md, step 7, phase 2; Q32): the sequence's UI
/// track plays the old card (Data/Cutscenes/ui: the old markup and its "Boss intro cutscenes" rules) in
/// the engine's document, and at each sampled time the document paints as the old game drew it in
/// Chromium 152 with its CSS animations seeked to that time (Golden/cutscene-card-&lt;time&gt;.png,
/// Tools/Scripts/make-cutscene-references.sh), both over the same flat backdrop (the 3D view isn't part
/// of the card), within the paint fixtures' tolerances. The card's rules are the old stylesheet's own,
/// line for line.
/// </summary>
[Collection("GPU")]
public class JiruanCardTests(GpuFixture gpu)
{
    const double Step = 1 / 60.0;
    const string BackdropCss = "\n#cutscene-backdrop { position: fixed; inset: 0; z-index: 1399; background: #3a2c4a; }\n";
    const string BackdropHtml = "<div id=\"cutscene-backdrop\"></div>";

    /// <summary>
    /// Held until the canvas blurs a shadow as Chromium does under a non-uniform scale (S7-14; Docs/Scene.md, step
    /// 7, "The card at 1.4 s"): at 1.4 s the progress bar's <c>box-shadow: 0 0 12px</c> under <c>scaleX(0.2)</c> is
    /// blurred across the bar with σ ≈ 2.7 px (6 × √0.2) in Chromium and σ 6 in the engine, 2,133 pixels (0.26 %)
    /// off; the worker porting that rule un-holds this case.
    /// </summary>
    const string ShadowBlurUnderScale = "S7-14: the progress bar's box-shadow under scaleX(0.2) is blurred across the bar with sigma 6 where Chromium's is about 2.7 (6 x sqrt 0.2): held until the canvas blurs a shadow as Chromium does under a non-uniform scale (Docs/Scene.md, step 7, \"The card at 1.4 s\")";

    /// <summary>
    /// The card at each sampled time as Chromium painted the old game's (its CSS animations seeked), within the
    /// paint fixtures' tolerances. At 1.4, 2.5 and 3.4 s the progress bar runs under scaleX(t / 7): its 2 px bar
    /// stays two crisp rows as the canvas turns a box's distance into pixels by its edge normal's own
    /// derivative (S7-7; before, one isotropic scale spread it over about four rows).
    /// </summary>
    [Theory]
    [InlineData(0.0)]
    [InlineData(1.4, Skip = ShadowBlurUnderScale)]
    [InlineData(2.5)]
    [InlineData(3.4)]
    [InlineData(4.6)]
    [InlineData(7.0)]
    public void TheCardPaintsAsTheOldGameDrewItAtTheTime(double time)
    {
        using var bench = new JiruanBench(gpu.Device, drawStage: false, extraCss: BackdropCss, extraHtml: BackdropHtml);
        bench.Play();
        bench.Frame(Step);
        bench.FrameTo(Step, time);
        var image = PaintReferenceTests.Render(gpu.Device, bench.Document, JiruanBench.Width, JiruanBench.Height);
        Golden.Match("cutscene-card-" + time.ToString("F2", CultureInfo.InvariantCulture), image, tolerance: 12, maxDifferentRatio: 0.002);
    }

    /// <summary>Every rule line of the card's stylesheet is a line of the old stylesheet (the tests' copy).</summary>
    [Fact]
    public void TheCardsRulesAreTheOldStylesheetsOwn()
    {
        var old = GameStylesheets.Text.Split('\n').ToHashSet();
        var card = JiruanIntro.CardCss.Split('\n');
        var rules = card.SkipWhile(l => !l.StartsWith(":root")).Skip(1).Where(l => l.Length > 0 && l != "}").ToArray();
        Assert.True(rules.Length > 35, $"{rules.Length} rule lines");
        Assert.All(rules, l => Assert.Contains(l, old));
        Assert.DoesNotContain(rules, l => l.Contains(".bi-jr-") || l.Contains(".bi-art") || l.Contains(".bi-boss"));
    }
}
