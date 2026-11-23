using Statventure.Game.Figures;
using Statventure.Game.Screens.Races;

namespace Statventure.Game.Tests;

/// <summary>
/// A look the data can't build (GM2 step 11, as step 5's builder says: a mod gone under a slot's save, a save opened
/// unchecked) shows no figure and crashes nothing: the sheet shows, its panel its ground, and the log says what is missing.
/// In the "Game log" collection (the log's sink is one for the process); the refusal comes before anything is made on the
/// GPU, so no device is needed.
/// </summary>
[Collection("Game log")]
public class LiveViewsLogTests
{
    [Fact]
    public void ALookTheDataCantBuildShowsNothingAndIsLogged()
    {
        using var data = new LiveViewData(emblems: false);
        using var s = new LiveScreens(null!, data);
        // The new hero's carousel isn't looked at here (its preset would be built on the GPU).
        s.S.Races.Views = NoHeroViews.Instance;
        var look = LiveViewData.ManLook() with
        {
            Parts = new Dictionary<string, string> { ["face"] = "face-human-male-1", ["hair"] = "hair-gone" },
        };
        var lines = GameTesting.CaptureLog(() => s.ReadyHero(look));
        Assert.True(s.S.Sheet!.Showing);
        Assert.Empty(s.Live);
        Assert.Null(s.Surfaces.ViewOf(s.Surface.Sheet));
        Assert.Contains("Warning [Figures] #hero-view: the human's look can't be shown (the look can't be built: no look part \"hair-gone\" (data/looks/parts/hair-gone)): the view stays empty", lines);
        // Saved again (the same look asked for again): still nothing, said once while the sheet shows.
        lines = GameTesting.CaptureLog(() =>
        {
            s.S.Session.Change(save => save with { Hero = save.Hero with { Experience = 1 } });
            s.H.Frame();
        });
        Assert.Empty(s.Live);
        Assert.DoesNotContain(lines, l => l.Contains("[Figures]"));
        // Opened again: said again.
        lines = GameTesting.CaptureLog(() =>
        {
            var slot = s.S.Session.CurrentSlot!;
            s.S.Session.Leave();
            s.S.Session.Open(slot);
            s.H.Frame();
        });
        Assert.Single(lines, l => l.Contains("[Figures]"));
    }
}
