using System.Numerics;
using Statventure.Platform;
using Statventure.Tests.Canvas;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// The window host's input mapping: GLFW's events (as the window queues them) become the DOM
/// events Chromium dispatches for the same keys, wheels and focus changes.
/// </summary>
public class UiHostInputTests
{
    sealed class Page
    {
        public readonly UiDocument Document;
        public readonly UiHostInput Input;
        public readonly List<string> Log = [];

        public Page(string html, Func<Key, int, string?>? keyName = null)
        {
            Document = new UiDocument(TestFonts.Library);
            Document.SetViewport(400, 300);
            Document.AddStylesheet("body { margin: 0; } .tall { height: 2000px; }");
            Markup.SetInnerHtml(Document.Body, html);
            Document.Update(0);
            Input = new UiHostInput(Document.Input, keyName ?? ((key, _) => key is >= Key.A and <= Key.Z ? ((char)(key - Key.A + 'a')).ToString() : null));
            foreach (var type in new[] { "keydown", "keyup" })
                Document.AddEventListener(type, e =>
                {
                    var k = (UiKeyboardEvent)e;
                    Log.Add($"{k.Type} {k.Key} {k.Code}" + (k.ShiftKey ? " shift" : "") + (k.CtrlKey ? " ctrl" : "") + (k.Repeat ? " repeat" : ""));
                }, capture: true);
            Document.AddEventListener("wheel", e => Log.Add($"wheel {((UiWheelEvent)e).DeltaX} {((UiWheelEvent)e).DeltaY}"), capture: true);
        }

        public void Send(params InputEvent[] events)
        {
            Input.Dispatch(events);
            Document.Update(Document.Animations.Time + 1 / 60.0);
        }
    }

    static InputEvent Down(Key key, KeyModifiers mods = KeyModifiers.None) => new(InputEventKind.KeyDown, key, mods);
    static InputEvent Up(Key key, KeyModifiers mods = KeyModifiers.None) => new(InputEventKind.KeyUp, key, mods);
    static InputEvent Char(char c) => new(InputEventKind.Text, Codepoint: c);

    [Fact]
    public void AKeyTakesTheCharacterItTypes()
    {
        var page = new Page("""<input id="t">""");
        page.Document.QuerySelector("#t")!.Focus();
        page.Send(Down(Key.A), Char('a'), Up(Key.A));
        page.Send(Down(Key.LeftShift), Down(Key.A, KeyModifiers.Shift), Char('A'), Up(Key.A, KeyModifiers.Shift), Up(Key.LeftShift, KeyModifiers.Shift));
        // Shift+1 types the layout's symbol, and its keyup says the same.
        page.Send(Down(Key.LeftShift), Down(Key.D1, KeyModifiers.Shift), Char('!'), Up(Key.D1, KeyModifiers.Shift), Up(Key.LeftShift, KeyModifiers.Shift));
        Assert.Equal("aA!", page.Document.QuerySelector("#t")!.Value);
        Assert.Equal(
        [
            "keydown a KeyA", "keyup a KeyA",
            "keydown Shift ShiftLeft shift", "keydown A KeyA shift", "keyup A KeyA shift", "keyup Shift ShiftLeft",
            "keydown Shift ShiftLeft shift", "keydown ! Digit1 shift", "keyup ! Digit1 shift", "keyup Shift ShiftLeft",
        ], page.Log);
    }

    [Fact]
    public void KeysThatTypeNothingAreNamedOrTakeTheLayoutsCharacter()
    {
        var page = new Page("<p>text</p>");
        page.Send(Down(Key.LeftControl), Down(Key.A, KeyModifiers.Control), Up(Key.A, KeyModifiers.Control), Up(Key.LeftControl, KeyModifiers.Control));
        page.Send(Down(Key.Enter), Up(Key.Enter), Down(Key.F5), Up(Key.F5), Down(Key.Left), Down(Key.Left), Up(Key.Left));
        Assert.Equal(
        [
            "keydown Control ControlLeft ctrl", "keydown a KeyA ctrl", "keyup a KeyA ctrl", "keyup Control ControlLeft",
            "keydown Enter Enter", "keyup Enter Enter", "keydown F5 F5", "keyup F5 F5", "keydown ArrowLeft ArrowLeft", "keydown ArrowLeft ArrowLeft", "keyup ArrowLeft ArrowLeft",
        ], page.Log);
    }

    [Fact]
    public void RepeatsAndCapsLock()
    {
        var page = new Page("<p>text</p>");
        page.Send(new InputEvent(InputEventKind.KeyDown, Key.B, KeyModifiers.Control | KeyModifiers.CapsLock), new InputEvent(InputEventKind.KeyRepeat, Key.B, KeyModifiers.Control | KeyModifiers.CapsLock));
        Assert.Equal(["keydown B KeyB ctrl", "keydown B KeyB ctrl repeat"], page.Log);
    }

    [Fact]
    public void TheKeypadMovesWithoutNumLock()
    {
        var page = new Page("<p>text</p>");
        page.Send(Down(Key.Keypad7), Up(Key.Keypad7));
        page.Send(Down(Key.Keypad7, KeyModifiers.NumLock), Char('7'), Up(Key.Keypad7, KeyModifiers.NumLock));
        Assert.Equal(["keydown Home Numpad7", "keyup Home Numpad7", "keydown 7 Numpad7", "keyup 7 Numpad7"], page.Log);
    }

    [Fact]
    public void WheelNotchesScrollDownAndRight()
    {
        var page = new Page("""<div class="tall"></div>""");
        page.Send(new InputEvent(InputEventKind.MouseMove, Position: new Vector2(50, 50)), new InputEvent(InputEventKind.Scroll, Scroll: new Vector2(0, -1)));
        page.Send(new InputEvent(InputEventKind.Scroll, Scroll: new Vector2(1, 0)));
        Assert.Equal(["wheel 0 120", "wheel -120 0"], page.Log);
    }

    [Fact]
    public void LosingTheWindowsFocusBlursAndGainingItFocusesAgain()
    {
        var page = new Page("""<input id="t">""");
        page.Document.QuerySelector("#t")!.Focus();
        page.Send(new InputEvent(InputEventKind.FocusLost));
        Assert.False(page.Document.WindowFocused);
        page.Send(new InputEvent(InputEventKind.FocusGained));
        Assert.True(page.Document.WindowFocused);
        Assert.Equal(page.Document.QuerySelector("#t"), page.Document.FocusedElement);
    }
}
