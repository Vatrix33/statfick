using Statventure.Core;
using Statventure.Game.Screens.Races;
using Statventure.UI.Style;

namespace Statventure.Game.Tests;

/// <summary>
/// Rules of the carousel's and the maker's sheets (GM2 step 8; <c>races.css</c>, <c>maker.css</c>) whose pixels stay under
/// the screen comparisons' limits (12 levels, 0.2 %), checked as computed: the stage's chevrons and an open row's, the
/// race's kind in capitals, the strip's ring on the shown race, the chosen preset's ring, a colour's swatch.
/// </summary>
public class HeroSheetsTests
{
    static float Rotation(ComputedStyle style) => style.Transform.Single(t => t.Kind == TransformKind.Rotate).A;

    [Fact]
    public void TheRulesUnderTheComparisonsLimitsHold()
    {
        using var s = new HeroScreensHarness();
        s.NewHero();
        s.Run(HeroFlow.RacesCommand);
        s.H.Frame();
        // The stage's chevrons: the previous one points left, the next one right.
        Assert.Equal((45f, -135f), (Rotation(s.Q("#races-previous").BeforeStyle!), Rotation(s.Q("#races-next").BeforeStyle!)));
        // The race's kind in capitals, as approved ("PLAYABLE"; a few hundred pixels of small text).
        Assert.Equal(Statventure.Text.TextTransform.Uppercase, s.Q("#races-kind").Style!.TextTransform);
        // The shown race's medallion: its 1 px border and an outer ring of 2 px in the race's colour, over the well.
        var accent = Color.FromHex("#c7683f");
        var current = s.Q("#races-strip .races-strip-item.is-current").Style!;
        Assert.Equal(2, current.BoxShadows.Length);
        Assert.Contains(current.BoxShadows, shadow => !shadow.Inset && shadow.Spread == 2 && shadow.Color == accent);
        Assert.Equal(new Color?[] { accent, accent, accent, accent }, new[] { current.BorderTopColor, current.BorderRightColor, current.BorderBottomColor, current.BorderLeftColor });
        // The maker: the chosen preset's ring in the race's colour; a swatch's disc its colour; an open row's chevron up.
        s.Click(s.Q("#races-choose"));
        s.H.Frame();
        var chosen = s.Q("#maker-presets .maker-preset[aria-checked=\"true\"]").Style!;
        Assert.Equal([new ShadowValue(0, 0, 0, 1, accent, true)], chosen.BoxShadows);
        Assert.Equal(Color.FromHex("#e8c3a0"), s.Row("colour:skin").Q(".maker-swatch").Style!.BackgroundColor);
        Assert.Equal(45f, Rotation(s.Row("part:hair").Q(".maker-row-chevron").Style!));
        var head = s.Row("part:hair").ElementChildren.First();
        head.Focus();
        s.Key("Enter");
        s.H.Frame();
        Assert.Equal(-135f, Rotation(s.Row("part:hair").Q(".maker-row-chevron").Style!));
    }

    /// <summary>
    /// Dev Mode's enemy-only medallions dimmed to .8 as the user approved them (Q51), the shown one not: a few hundred pixels
    /// a few levels apart, under the Dev Mode fixtures' limits.
    /// </summary>
    [Fact]
    public void DevModesEnemyOnlyMedallionsAreDimmedButNotTheShownOne()
    {
        using var s = new HeroScreensHarness(before: settings => settings.DevMode = true);
        s.NewHero();
        s.Run(HeroFlow.RacesCommand, "goblin");
        s.H.Frame();
        float Opacity(string race) => s.Q($"#races-strip .races-strip-item[data-race=\"{race}\"]").Style!.Opacity;
        Assert.Equal((1f, 0.8f, 1f), (Opacity("human"), Opacity("orc"), Opacity("goblin")));
    }
}
