using System.Numerics;
using Statventure.Platform;

namespace Statventure.Tests.Platform;

public class InputTests
{
    [Fact]
    public void EventsShowUpInTheNextFrameInOrder()
    {
        var input = new Input();
        input.Push(new InputEvent(InputEventKind.KeyDown, Key: Key.A));
        input.Push(new InputEvent(InputEventKind.Text, Codepoint: 'a'));
        Assert.Empty(input.Events);
        input.BeginFrame();
        Assert.Equal([InputEventKind.KeyDown, InputEventKind.Text], input.Events.Select(e => e.Kind));
        Assert.True(input.IsDown(Key.A));
        Assert.True(input.WasPressed(Key.A));
        input.BeginFrame();
        Assert.Empty(input.Events);
        Assert.True(input.IsDown(Key.A));
        Assert.False(input.WasPressed(Key.A));
    }

    [Fact]
    public void MouseStateFollowsMovesButtonsAndScrolling()
    {
        var input = new Input();
        input.Push(new InputEvent(InputEventKind.MouseMove, Position: new Vector2(10, 20)));
        input.Push(new InputEvent(InputEventKind.MouseDown, Button: MouseButton.Left, Position: new Vector2(10, 20)));
        input.Push(new InputEvent(InputEventKind.Scroll, Scroll: new Vector2(0, -1)));
        input.Push(new InputEvent(InputEventKind.Scroll, Scroll: new Vector2(0, -2)));
        input.BeginFrame();
        Assert.Equal(new Vector2(10, 20), input.MousePosition);
        Assert.Equal(new Vector2(10, 20), input.MouseDelta);
        Assert.True(input.WasPressed(MouseButton.Left));
        Assert.Equal(new Vector2(0, -3), input.ScrollDelta);
        input.Push(new InputEvent(InputEventKind.MouseMove, Position: new Vector2(15, 20)));
        input.Push(new InputEvent(InputEventKind.MouseUp, Button: MouseButton.Left));
        input.BeginFrame();
        Assert.Equal(new Vector2(5, 0), input.MouseDelta);
        Assert.False(input.IsDown(MouseButton.Left));
        Assert.True(input.WasReleased(MouseButton.Left));
        Assert.Equal(Vector2.Zero, input.ScrollDelta);
    }

    [Fact]
    public void LosingFocusReleasesEverything()
    {
        var input = new Input();
        input.Push(new InputEvent(InputEventKind.KeyDown, Key: Key.LeftShift, Modifiers: KeyModifiers.Shift));
        input.Push(new InputEvent(InputEventKind.MouseDown, Button: MouseButton.Right));
        input.BeginFrame();
        input.Push(new InputEvent(InputEventKind.FocusLost));
        input.BeginFrame();
        Assert.False(input.IsDown(Key.LeftShift));
        Assert.False(input.IsDown(MouseButton.Right));
        Assert.Equal(KeyModifiers.None, input.Modifiers);
    }
}
