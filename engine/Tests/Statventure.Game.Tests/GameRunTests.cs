using System.Runtime.ExceptionServices;
using Statventure.Imaging;

namespace Statventure.Game.Tests;

/// <summary>
/// The game run for real on the parts' stubs (a hidden window, no log, no sound, a scratch per-user folder): the engine's
/// start, the game's start sequence, its first frame drawn and captured, and its end leaving the open character.
/// Needs a display (the suite runs in a headless compositor).
/// </summary>
[Collection("Game log")]
public class GameRunTests
{
    /// <summary>Notes what the run had at <c>Load</c>, makes a character, and quits in the first update (a document task), so the hidden window presents one frame.</summary>
    sealed class Probe(List<string> record) : IGameScreen
    {
        public void Attach(GameParts parts)
        {
            record.Add($"title {parts.Game.Window.Title}");
            record.Add($"window {parts.Game.GameWindow?.GetType().Name}");
            record.Add($"host {parts.Ui.Host is not null}");
            parts.Session.New("Ser Alrik");
            // A later start's request before the first update: taken in it.
            parts.Game.AskForward();
            parts.Ui.Document.SetTimeout(() =>
            {
                record.Add("quit");
                parts.Game.Quit();
            });
        }
    }

    [Fact]
    public void TheGameRunsItsStartOnTheStubsAndDrawsItsPage()
    {
        using var scratch = new ScratchFolder();
        var record = new List<string>();
        var capture = scratch["page.png"];
        var game = GameTesting.Game(scratch["p"], new GameOptions { Capture = capture, Frames = 0 }, [.. StatFickGame.NewScreens(), new Probe(record)]);
        // On a thread of its own with a time limit, so a run that doesn't quit fails the test rather than hanging the
        // test run (a hidden window's presents hang after a few frames under wlroots); a run that doesn't end is left
        // (not disposed: its device may be stuck).
        List<string> log = [];
        Exception? failed = null;
        var run = new Thread(() =>
        {
            try
            {
                log = GameTesting.CaptureLog(game.Run);
            }
            catch (Exception exception)
            {
                failed = exception;
            }
        })
        {
            Name = "Stat Fick test run",
            IsBackground = true,
        };
        run.Start();
        Assert.True(run.Join(TimeSpan.FromSeconds(60)), "the run didn't end within 60 s");
        using var ended = game;
        if (failed is not null)
            ExceptionDispatchInfo.Capture(failed).Throw();

        Assert.Equal(["title Stat Fick", "window EngineGameWindow", "host True", "quit"], record);
        Assert.Contains("Info [Commands] start-screen: not registered yet, nothing done", log);
        Assert.Contains(log, line => line.StartsWith("Info [Game] the run's random seed ", StringComparison.Ordinal));
        Assert.Contains(log, line => line.StartsWith("Info [Game] another start asked for the window: ", StringComparison.Ordinal));
        // The window's state kept in the settings by the update.
        Assert.NotNull(game.Parts!.Settings.Window);
        // The page drawn: the game's name on the parchment, captured from the first frame.
        var page = ImageFile.Load(capture);
        var colours = new Dictionary<(byte, byte, byte), int>();
        for (var i = 0; i < page.Pixels.Length; i += 4)
        {
            var colour = (page.Pixels[i], page.Pixels[i + 1], page.Pixels[i + 2]);
            colours[colour] = colours.GetValueOrDefault(colour) + 1;
        }
        Assert.Equal(((byte)0xd5, (byte)0xc3, (byte)0x9b), colours.MaxBy(c => c.Value).Key);
        Assert.True(colours.Count > 2, "the game's name is drawn on the page");
        // Its end left the character, saved to its slot.
        Assert.Null(game.Parts!.Session.Current);
        Assert.Equal(["ser-alrik"], game.Parts.Session.Slots().Select(s => s.Slot));
    }
}
