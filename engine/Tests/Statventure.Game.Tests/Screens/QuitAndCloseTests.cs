using System.Runtime.ExceptionServices;
using Statventure.Storage;

namespace Statventure.Game.Tests;

/// <summary>
/// Quit and the window's close (Docs/Game/Foundation.md, "Quit and close"; step 9): the game run for real (a hidden window,
/// no log, no sound, a scratch per-user folder) with its screens; a character made after the start screen, its slot's
/// files then taken away, and the run ended by the menu's Quit or by the window's close: either way it ends at once, with
/// no question, and the open character is saved to its slot again on the way out.
/// </summary>
[Collection("Game log")]
public class QuitAndCloseTests
{
    /// <summary>
    /// In the first update (a document task, after the start screen): a character made, its files removed, then the run
    /// ended the given way. So early because a hidden window's presents hang after a few frames under wlroots
    /// (<see cref="GameRunTests"/>).
    /// </summary>
    sealed class Ender(string how, List<string> record) : IGameScreen
    {
        public void Attach(GameParts parts) => parts.Ui.Document.SetTimeout(() =>
        {
            parts.Session.New("Ser Alrik");
            var saves = parts.Game.Storage.Saves;
            parts.Game.Storage.Flush();
            File.Delete(saves.SavePath("ser-alrik"));
            File.Delete(saves.BackupPath("ser-alrik"));
            record.Add($"made, files removed: {File.Exists(saves.SavePath("ser-alrik"))}");
            if (how == "menu")
            {
                parts.Ui.Commands.Run("open-menu");
                parts.Ui.Document.UpdateLayout();
                parts.Ui.Document.QuerySelectorAll("#game-menu .game-menu-item").Single(i => i.ElementChildren.First().TextContent == "Quit").Click();
            }
            else
                parts.Game.Window.CloseRequested = true;
            record.Add("asked to end");
        });
    }

    [Theory]
    [InlineData("menu")]
    [InlineData("close")]
    public void QuitAndTheWindowsCloseEndAtOnceSavingTheOpenCharacter(string how)
    {
        using var scratch = new ScratchFolder();
        var record = new List<string>();
        var game = GameTesting.Game(scratch["p"], screens: [.. StatFickGame.NewScreens(), new Ender(how, record)]);
        Exception? failed = null;
        var run = new Thread(() =>
        {
            try
            {
                game.Run();
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
        Assert.Equal(["made, files removed: False", "asked to end"], record);
        Assert.Null(game.Parts!.Session.Current);
        // The run's storage is closed with it: the per-user folder looked at afresh.
        using var storage = new GameStorage(scratch["p"]);
        Assert.True(File.Exists(storage.Saves.SavePath("ser-alrik")), "the character was saved on the way out");
        Assert.Equal(["ser-alrik"], game.Parts.Session.Slots().Select(s => s.Slot));
    }
}
