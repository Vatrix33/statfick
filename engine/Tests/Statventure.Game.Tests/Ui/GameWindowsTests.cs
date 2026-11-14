using Statventure.Game.Sounds;
using Statventure.Game.Ui;
using Statventure.Platform;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The window manager as the old <c>.modal-layer</c>s behave (Docs/Game/Foundation.md, "How it works", step 8; the old
/// <c>updateModalState</c>, the modules' open and close, <c>music.js</c>'s Escape and Tab): a window shown and hidden,
/// <c>body.modal-open</c>, the focus moved in, kept in and given back, Escape closing the topmost or opening the menu,
/// the sounds of opening and closing.
/// </summary>
[Collection("Game log")]
public class GameWindowsTests
{
    [Fact]
    public void OpeningShowsTheLayerSetsModalOpenSoundsAndFocusesItsCloseButton()
    {
        using var h = new GameUiHarness();
        var opener = h.Document.Body.Append("button", "opener", "Open");
        opener.SetAttribute("type", "button");
        h.Frame();
        opener.Focus();
        var layer = h.Layer("first");
        Assert.Null(layer.Parent);
        h.Ui.Windows.Open(layer);
        h.Frame();
        Assert.Same(h.Document.Body, layer.Parent);
        Assert.False(layer.Hidden);
        Assert.True(h.Document.Body.HasClass(GameWindows.ModalOpenClass));
        Assert.Same(layer, h.Ui.Windows.Top);
        Assert.Equal([layer], h.Ui.Windows.Layers);
        Assert.True(h.Ui.Windows.IsOpen(layer));
        Assert.Same(layer.Descendants().Single(e => e.HasClass("modal-close")), h.Document.FocusedElement);
        Assert.Equal([SoundNames.Open], h.Sounds);
        // Opened again: nothing (no second sound).
        h.Ui.Windows.Open(layer);
        h.Frame();
        Assert.Equal([SoundNames.Open], h.Sounds);
    }

    [Fact]
    public void TheFocusGoesWhereAskedElseToTheFirstControlNotTheBackdrop()
    {
        using var h = new GameUiHarness();
        var layer = h.Layer("asked");
        var two = layer.Descendants().Single(e => e.HasClass("two"));
        h.Ui.Windows.Open(layer, two);
        Assert.Same(two, h.Document.FocusedElement);
        var bare = h.Layer("bare", close: false);
        h.Ui.Windows.Open(bare);
        Assert.Same(bare.Descendants().Single(e => e.HasClass("one")), h.Document.FocusedElement);
        // The close button even when a control comes before it (as the old windows focus theirs).
        var later = h.Layer("later");
        var section = later.Descendants().Single(e => e.Tag == "section");
        var before = new UiElement("button") { ClassName = "before" };
        before.SetAttribute("type", "button");
        section.InsertBefore(before, section.ElementChildren.First());
        h.Ui.Windows.Open(later);
        Assert.Same(later.Descendants().Single(e => e.HasClass("modal-close")), h.Document.FocusedElement);
    }

    [Fact]
    public void ClosingHidesTheLayerTakesModalOpenAwayAtTheLastSoundsAndGivesTheFocusBack()
    {
        using var h = new GameUiHarness();
        var opener = h.Document.Body.Append("button", "opener", "Open");
        opener.SetAttribute("type", "button");
        // A window in the markup from the start (as the old ones) stays there, hidden; one Open put in goes again.
        var kept = h.Layer("kept");
        h.Document.Body.Append(kept);
        h.Frame();
        opener.Focus();
        h.Ui.Windows.Open(kept);
        h.Frame();
        var added = h.Layer("added");
        h.Ui.Windows.Open(added);
        h.Frame();
        h.Sounds.Clear();
        h.Ui.Windows.Close(added);
        h.Frame();
        Assert.Null(added.Parent);
        Assert.True(added.Hidden);
        Assert.True(h.Document.Body.HasClass(GameWindows.ModalOpenClass));
        Assert.Same(kept, h.Ui.Windows.Top);
        // The focus back where it was when the closed window opened: in the window under it.
        Assert.Same(kept.Descendants().Single(e => e.HasClass("modal-close")), h.Document.FocusedElement);
        Assert.Equal([SoundNames.Close], h.Sounds);
        Assert.True(h.Ui.Windows.CloseTop());
        h.Frame();
        Assert.Same(h.Document.Body, kept.Parent);
        Assert.True(kept.Hidden);
        Assert.False(h.Document.Body.HasClass(GameWindows.ModalOpenClass));
        Assert.Null(h.Ui.Windows.Top);
        Assert.Same(opener, h.Document.FocusedElement);
        Assert.Equal([SoundNames.Close, SoundNames.Close], h.Sounds);
        // Nothing open: nothing to close, no sound.
        Assert.False(h.Ui.Windows.CloseTop());
        h.Ui.Windows.Close(kept);
        h.Frame();
        Assert.Equal(2, h.Sounds.Count);
    }

    [Fact]
    public void AWindowUnderTheTopClosingLeavesTheFocusAndPassesItsOwnBack()
    {
        using var h = new GameUiHarness();
        var opener = h.Document.Body.Append("button", "opener", "Open");
        opener.SetAttribute("type", "button");
        h.Frame();
        opener.Focus();
        var under = h.Layer("under");
        h.Ui.Windows.Open(under);
        var top = h.Layer("top");
        h.Ui.Windows.Open(top);
        var focused = h.Document.FocusedElement;
        h.Ui.Windows.Close(under);
        h.Frame();
        Assert.Same(focused, h.Document.FocusedElement);
        Assert.Equal([top], h.Ui.Windows.Layers);
        // The top one, opened from inside the closed one, gives the focus back to where that one's came from.
        h.Ui.Windows.Close(top);
        h.Frame();
        Assert.Same(opener, h.Document.FocusedElement);
    }

    [Fact]
    public void EscapeClosesTheTopmostWindowThenOpensTheMenu()
    {
        using var h = new GameUiHarness();
        var ran = new List<string>();
        h.Commands(ran, GameUi.MenuCommand);
        var first = h.Layer("first");
        var second = h.Layer("second");
        h.Ui.Windows.Open(first);
        h.Ui.Windows.Open(second);
        h.Frame();
        Assert.False(h.Key("Escape", "Escape"));
        Assert.Equal([first], h.Ui.Windows.Layers);
        Assert.False(h.Key("Escape", "Escape"));
        Assert.Empty(h.Ui.Windows.Layers);
        Assert.Empty(ran);
        Assert.False(h.Key("Escape", "Escape"));
        Assert.Equal([GameUi.MenuCommand], ran);
    }

    [Fact]
    public void EscapeWithNoMenuYetIsLoggedAndAWindowMayAnswerItItself()
    {
        using var h = new GameUiHarness();
        var log = GameTesting.CaptureLog(() => h.Key("Escape", "Escape"));
        Assert.Contains("Info [Commands] open-menu: not registered yet, nothing done", log);
        var asked = 0;
        var layer = h.Layer("own");
        h.Ui.Windows.Open(layer, escape: () => asked++);
        h.Key("Escape", "Escape");
        Assert.Equal(1, asked);
        Assert.True(h.Ui.Windows.IsOpen(layer));
        // A key the page already handled isn't Escape's.
        layer.AddEventListener<UiKeyboardEvent>("keydown", e => e.PreventDefault());
        h.Key("Escape", "Escape");
        Assert.Equal(1, asked);
    }

    [Fact]
    public void TabStaysAmongTheTopWindowsControls()
    {
        using var h = new GameUiHarness();
        var outside = h.Document.Body.Append("button", "outside", "Outside");
        outside.SetAttribute("type", "button");
        var after = h.Document.Body.Append("button", "after", "After");
        after.SetAttribute("type", "button");
        var under = h.Layer("under");
        h.Ui.Windows.Open(under);
        var layer = h.Layer("trap");
        h.Ui.Windows.Open(layer);
        h.Frame();
        UiElement Control(string name) => layer.Descendants().Single(e => e.HasClass(name));
        var close = Control("modal-close");
        Assert.Same(close, h.Document.FocusedElement);
        // In the window's order, the disabled button and the backdrop left out; from the last to the first and back.
        h.Key("Tab", "Tab");
        Assert.Same(Control("one"), h.Document.FocusedElement);
        h.Key("Tab", "Tab");
        Assert.Same(Control("two"), h.Document.FocusedElement);
        h.Key("Tab", "Tab");
        Assert.Same(close, h.Document.FocusedElement);
        h.Key("Tab", "Tab", KeyModifiers.Shift);
        Assert.Same(Control("two"), h.Document.FocusedElement);
        // From outside the window, into it: Tab to its first control, Shift+Tab to its last.
        outside.Focus();
        h.Key("Tab", "Tab");
        Assert.Same(close, h.Document.FocusedElement);
        outside.Focus();
        h.Key("Tab", "Tab", KeyModifiers.Shift);
        Assert.Same(Control("two"), h.Document.FocusedElement);
        // With no window open, Tab is the page's own.
        h.Ui.Windows.Close(layer);
        h.Ui.Windows.Close(under);
        h.Frame();
        outside.Focus();
        h.Key("Tab", "Tab");
        Assert.Same(after, h.Document.FocusedElement);
    }

    [Fact]
    public void TheEventsSayWhichWindowOpenedAndClosed()
    {
        using var h = new GameUiHarness();
        var said = new List<string>();
        h.Ui.Windows.Opened += l => said.Add("opened " + l.Id);
        h.Ui.Windows.Closed += l => said.Add("closed " + l.Id);
        var layer = h.Layer("told");
        h.Ui.Windows.Open(layer);
        h.Ui.Windows.Close(layer);
        Assert.Equal(["opened told", "closed told"], said);
    }
}
