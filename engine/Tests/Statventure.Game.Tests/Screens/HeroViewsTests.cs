using Statventure.Game.Data.Looks;
using Statventure.Game.Screens.Races;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// What the race carousel and the character maker ask of their 3D views (GM2 step 8; Q28, Q36, Q41; GM2-P16): which look's
/// figure each surface shows and when, the shown race's emblem, the placeholder giving way to a live emblem, and every
/// view let go when its screen hides. Building the views is GM2 step 11's (<see cref="IHeroViews"/>); the program has none
/// until then.
/// </summary>
public class HeroViewsTests
{
    /// <summary>The calls a screen makes, in order ("figure races-figure human test-human-male-1").</summary>
    sealed class Recording : IHeroViews
    {
        public List<string> Calls { get; } = [];
        public bool Live { get; set; }

        public void ShowFigure(UiElement surface, string race, LookHero? look) =>
            Calls.Add($"figure {surface.Id} {race} {(look is null ? "none" : look.Preset ?? look.Body)}");

        public bool ShowEmblem(UiElement surface, string race, string? emblem)
        {
            Calls.Add($"emblem {surface.Id} {race} {emblem ?? "none"}");
            return Live;
        }

        public void Hide(UiElement surface) => Calls.Add($"hide {surface.Id}");
    }

    static (HeroScreensHarness, Recording) Harness()
    {
        var s = new HeroScreensHarness();
        var views = new Recording();
        s.Races.Views = views;
        s.Maker.Views = views;
        s.NewHero();
        return (s, views);
    }

    [Fact]
    public void TheCarouselShowsTheRacesFirstPresetAndItsEmblemAndLetsThemGoWhenItHides()
    {
        var (s, views) = Harness();
        using var _ = s;
        s.Run(HeroFlow.RacesCommand);
        Assert.Equal(["figure races-figure human test-human-male-1", "emblem races-emblem-view human human"], views.Calls);
        Assert.False(s.Q("#races-emblem").HasClass("is-live"));
        // A race with no preset (the elf: no looks in the test data) has no look to show; an enemy-only race no emblem.
        views.Calls.Clear();
        s.Key("ArrowRight");
        Assert.Equal(["figure races-figure elf none", "emblem races-emblem-view elf elf"], views.Calls);
        // The keys browse nothing while the carousel is hidden.
        s.Session.Leave();
        views.Calls.Clear();
        s.Key("ArrowRight");
        Assert.Empty(views.Calls);
        s.NewHero();
        s.Run(HeroFlow.RacesCommand);
        views.Calls.Clear();
        // A live emblem takes the placeholder's place.
        views.Live = true;
        views.Calls.Clear();
        s.Click(s.Q("#races-strip .races-strip-item[data-race=\"human\"]"));
        Assert.True(s.Q("#races-emblem").HasClass("is-live"));
        Assert.Null(s.Q("#races-emblem .races-emblem-mark").Box);
        // Taking the race: both views let go, the maker's figure shown.
        views.Calls.Clear();
        s.Click(s.Q("#races-choose"));
        Assert.Equal(["hide races-figure", "hide races-emblem-view", "figure maker-figure human test-human-male-1"], views.Calls);
    }

    [Fact]
    public void InDevModeTheCarouselShowsTheHerosOwnLookForItsRaceAndNoEmblemForAnEnemyOnlyRace()
    {
        var (s, views) = Harness();
        using var _ = s;
        var own = HeroTestData.Presets["test-human-female-2"].Look with { Preset = "test-human-female-2" };
        s.Session.Change(save => save with { Hero = save.Hero with { Race = "human" }, Look = own });
        s.H.Settings.DevMode = true;
        s.Run(HeroFlow.RacesCommand);
        Assert.Equal("figure races-figure human test-human-female-2", views.Calls[0]);
        views.Calls.Clear();
        s.Click(s.Q("#races-strip .races-strip-item[data-race=\"goblin\"]"));
        Assert.Equal(["figure races-figure goblin none", "emblem races-emblem-view goblin none"], views.Calls);
        // Leaving lets them go.
        views.Calls.Clear();
        s.Click(s.Q("#races-leave"));
        Assert.Equal(["hide races-figure", "hide races-emblem-view"], views.Calls);
    }

    [Fact]
    public void TheMakersFigureFollowsEveryChangeAndIsLetGoWhenItHides()
    {
        var (s, views) = Harness();
        using var _ = s;
        s.Run(HeroFlow.MakerCommand, HeroTestData.Human);
        Assert.Equal(["figure maker-figure human test-human-male-1"], views.Calls);
        views.Calls.Clear();
        s.Click(s.Row("build").Descendants().First(e => e.GetAttribute("data-value") == "strong"));
        s.Click(s.Q("#maker-random"));
        Assert.Equal("figure maker-figure human test-human-male-1", views.Calls[0]);
        Assert.Equal(2, views.Calls.Count);
        Assert.StartsWith("figure maker-figure human human-", views.Calls[1]);
        views.Calls.Clear();
        s.Click(s.Q("#maker-begin"));
        Assert.Equal(["hide maker-figure"], views.Calls);
        // A screen that isn't showing lets nothing go again.
        s.Session.Leave();
        Assert.Equal(["hide maker-figure"], views.Calls);
    }

    [Fact]
    public void WithoutRacesTheCarouselsViewsAreLetGo()
    {
        var (s, views) = Harness();
        using var _ = s;
        s.Races.Data = () => null;
        s.Run(HeroFlow.RacesCommand);
        Assert.Equal(["hide races-figure", "hide races-emblem-view"], views.Calls);
    }
}
