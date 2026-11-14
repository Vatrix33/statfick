using Statventure.Game.Sounds;
using Statventure.Game.Ui;
using Statventure.Platform;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The interface sounds by name (GM1-11, Q13; Docs/Game/Foundation.md, "The sounds", Wiring; "How it works", step 8):
/// <c>ui-press</c> for a click on an enabled button, <c>ui-toggle</c> for a switch, box or select changed, the window
/// manager's <c>ui-open</c> and <c>ui-close</c>, the game's event sounds, one per action (the one that says most);
/// counted, silent: through a sound of the test's own with no output, or none. No music is started (Q15).
/// </summary>
[Collection("Game log")]
public class GameSoundsTests
{
    static UiElement Button(GameUiHarness h, string name, string? role = null, bool disabled = false)
    {
        var button = h.Document.Body.Append("button", name, name);
        button.SetAttribute("type", "button");
        if (role is not null)
            button.SetAttribute("role", role);
        if (disabled)
            button.SetAttribute("disabled", "");
        return button;
    }

    [Fact]
    public void AClickOnAnEnabledButtonPressesOnceASwitchToggles()
    {
        using var h = new GameUiHarness();
        var plain = Button(h, "plain");
        plain.Append("span", text: " (inside)");
        var toggle = Button(h, "switch", role: "switch");
        var off = Button(h, "off", disabled: true);
        var input = h.Document.Body.Append("input");
        input.SetAttribute("type", "button");
        input.SetAttribute("value", "Input");
        h.Frame();
        h.Click(plain);
        Assert.Equal([SoundNames.Press], h.Sounds);
        // A click on what a button holds is the button's.
        h.Click(plain.ElementChildren.Single());
        h.Click(toggle);
        h.Click(input);
        Assert.Equal([SoundNames.Press, SoundNames.Press, SoundNames.Toggle, SoundNames.Press], h.Sounds);
        // A disabled button sounds nothing, even for a click dispatched to it; nor does the page.
        h.Sounds.Clear();
        h.Click(off);
        off.DispatchEvent(new UiMouseEvent("click"));
        h.Frame();
        h.Click(new System.Numerics.Vector2(700, 500));
        Assert.Empty(h.Sounds);
    }

    [Fact]
    public void ABoxARadioButtonAndASelectChangedToggleASliderAndAFieldDont()
    {
        using var h = new GameUiHarness();
        Markup.SetInnerHtml(h.Document.Body, """
            <label class="box-label"><input class="box" type="checkbox">Box</label>
            <input class="radio" type="radio" name="r"><input class="other" type="radio" name="r">
            <select class="select"><option>One</option><option>Two</option></select>
            <input class="slider" type="range" min="0" max="100" value="50">
            <input class="field" type="text">
            """);
        h.Frame();
        h.Click(h.Q(".box"));
        h.Click(h.Q(".box-label"));
        h.Click(h.Q(".radio"));
        Assert.Equal([SoundNames.Toggle, SoundNames.Toggle, SoundNames.Toggle], h.Sounds);
        h.Q(".select").Focus();
        h.Key("ArrowDown", "ArrowDown");
        Assert.Equal(4, h.Sounds.Count);
        h.Q(".slider").Focus();
        h.Key("ArrowRight", "ArrowRight");
        h.Q(".field").Focus();
        h.Type("text");
        h.Q(".slider").Focus();
        Assert.Equal(4, h.Sounds.Count);
        Assert.All(h.Sounds, s => Assert.Equal(SoundNames.Toggle, s));
    }

    [Fact]
    public async Task OneSoundPerActionTheOneThatSaysMost()
    {
        using var h = new GameUiHarness();
        var opens = Button(h, "opens");
        var layer = h.Layer("window");
        opens.AddEventListener("click", _ => h.Ui.Windows.Open(layer));
        layer.Descendants().Single(e => e.HasClass("modal-close")).AddEventListener("click", _ => h.Ui.Windows.Close(layer));
        h.Frame();
        // A click that opens a window: only the window's opening.
        h.Click(opens);
        Assert.Equal([SoundNames.Open], h.Sounds);
        // A click that closes one: only its closing.
        h.Click(layer.Descendants().Single(e => e.HasClass("modal-close")));
        Assert.Equal([SoundNames.Open, SoundNames.Close], h.Sounds);
        // A confirmed action whose event has a sound of its own: only the event's (the window's closing and the press under it).
        h.Sounds.Clear();
        var delete = Button(h, "delete");
        delete.AddEventListener("click", async _ =>
        {
            if (await h.Ui.Dialogs.Confirm("Delete Ser Alrik?", "Delete"))
                h.Ui.Sound(SoundNames.Deleted);
        });
        h.Frame();
        h.Click(delete);
        h.Click(h.Q(".game-dialog-accept"));
        Assert.Equal([SoundNames.Open, SoundNames.Deleted], h.Sounds);
        // Cancelled: the closing.
        h.Click(delete);
        h.Click(h.Q(".game-dialog-cancel"));
        Assert.Equal([SoundNames.Open, SoundNames.Deleted, SoundNames.Open, SoundNames.Close], h.Sounds);
        // Two actions within one frame are two sounds: the next input ends the one before.
        h.Sounds.Clear();
        var plain = Button(h, "plain");
        h.Frame();
        var at = h.Middle(plain);
        h.Input.PointerDown(at, MouseButton.Left);
        h.Input.PointerUp(at, MouseButton.Left);
        h.Input.PointerDown(at, MouseButton.Left);
        h.Input.PointerUp(at, MouseButton.Left);
        h.Frame();
        Assert.Equal([SoundNames.Press, SoundNames.Press], h.Sounds);
        // So are two keys: two windows closed by Escape within one frame.
        h.Sounds.Clear();
        h.Ui.Windows.Open(h.Layer("one"));
        h.Ui.Windows.Open(h.Layer("two"));
        h.Frame();
        h.Input.KeyDown("Escape", "Escape");
        h.Input.KeyDown("Escape", "Escape");
        h.Frame();
        Assert.Equal([SoundNames.Open, SoundNames.Close, SoundNames.Close], h.Sounds);
        await Task.CompletedTask;
    }

    [Fact]
    public void TheEventsSoundsOutweighTheWindowsAndTheFirstOfThemWins()
    {
        Assert.Equal([4, 4, 4, 4, 3, 2, 1, 0],
            new[] { SoundNames.Saved, SoundNames.Opened, SoundNames.Deleted, SoundNames.Refused, SoundNames.Open, SoundNames.Close, SoundNames.Toggle, SoundNames.Press }.Select(GameSounds.Weight));
        using var h = new GameUiHarness();
        h.Ui.Sound(SoundNames.Press);
        h.Ui.Sound(SoundNames.Opened);
        h.Ui.Sound(SoundNames.Close);
        h.Ui.Sound(SoundNames.Saved);
        h.Frame();
        Assert.Equal([SoundNames.Opened], h.Sounds);
    }

    [Fact]
    public void TheSoundsGoThroughTheEffectsAPatchMissingIsLoggedOnceAndNoMusicStarts()
    {
        using var h = new GameUiHarness(sound: true);
        var press = Button(h, "press");
        h.Frame();
        var log = GameTesting.CaptureLog(() =>
        {
            h.Click(press);
            _ = h.Ui.Dialogs.Confirm("Delete?", "Delete");
            h.Frame();
            h.Ui.Sound("ui-missing");
            h.Frame();
            h.Ui.Sound("ui-missing");
            h.Frame();
        });
        // The committed patches play (the engine took them); a name with none is skipped, said once.
        Assert.Equal([SoundNames.Press, SoundNames.Open], h.Sounds);
        Assert.Equal(["Warning [Ui] sound ui-missing: no patch at audio/effects/ui-missing in the data, skipped"], log.Where(l => l.Contains("[Ui] sound")));
        Assert.Null(h.Audio!.Music.Theme);
    }
}
