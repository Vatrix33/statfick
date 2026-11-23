using System.Runtime.ExceptionServices;
using Statventure.Game.Figures;
using Statventure.Game.Screens.Maker;
using Statventure.Game.Screens.Races;
using Statventure.Game.Screens.Sheet;

namespace Statventure.Game.Tests;

/// <summary>
/// The program's screens take the game's live views on its window (GM2 step 11: <see cref="LiveHeroViews.For"/>), each
/// screen its own, and a document without a window (the tests' screens) none. The game run for real as
/// <see cref="GameRunTests"/> runs it (a hidden window, no log, no sound, a scratch per-user folder); needs a display (the
/// suite runs in a headless compositor).
/// </summary>
[Collection("Game log")]
public class LiveViewsProgramTests
{
    sealed class Quit : IGameScreen
    {
        public void Attach(GameParts parts) => parts.Ui.Document.SetTimeout(parts.Game.Quit);
    }

    [Fact]
    public void TheProgramsScreensTakeLiveViewsAndAWindowlessDocumentNone()
    {
        using var scratch = new ScratchFolder();
        var screens = StatFickGame.NewScreens();
        var game = GameTesting.Game(scratch["p"], new GameOptions(), [.. screens, new Quit()]);
        Exception? failed = null;
        var run = new Thread(() =>
        {
            try
            {
                GameTesting.CaptureLog(game.Run);
            }
            catch (Exception exception)
            {
                failed = exception;
            }
        })
        {
            Name = "Stat Fick live views test run",
            IsBackground = true,
        };
        run.Start();
        Assert.True(run.Join(TimeSpan.FromSeconds(60)), "the run didn't end within 60 s");
        using var ended = game;
        if (failed is not null)
            ExceptionDispatchInfo.Capture(failed).Throw();
        IHeroViews[] views = [screens.OfType<SheetScreen>().Single().Views, screens.OfType<RacesScreen>().Single().Views, screens.OfType<MakerScreen>().Single().Views];
        Assert.All(views, v => Assert.IsType<LiveHeroViews>(v));
        Assert.Equal(3, views.Distinct().Count());
        // No view is left alive when the run has ended.
        Assert.All(views, v => Assert.Empty(((LiveHeroViews)v).Live));

        // A document without a window: none.
        using var harness = new ScreensHarness();
        Assert.IsType<NoHeroViews>(harness.Sheet.Views);
        using var hero = new HeroScreensHarness();
        Assert.IsType<NoHeroViews>(hero.Races.Views);
        Assert.IsType<NoHeroViews>(hero.Maker.Views);
    }
}
