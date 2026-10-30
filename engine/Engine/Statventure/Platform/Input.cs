using System.Numerics;

namespace Statventure.Platform;

public enum InputEventKind
{
    KeyDown,
    KeyRepeat,
    KeyUp,
    Text,
    MouseMove,
    MouseDown,
    MouseUp,
    Scroll,
    MouseLeave,
    FocusLost,
    FocusGained,
}

/// <summary>One input event. Positions are in logical units (window coordinates, not pixels).</summary>
public readonly record struct InputEvent(
    InputEventKind Kind,
    Key Key = Key.Unknown,
    KeyModifiers Modifiers = KeyModifiers.None,
    int Codepoint = 0,
    Vector2 Position = default,
    MouseButton Button = MouseButton.Left,
    Vector2 Scroll = default,
    int Scancode = 0
);

/// <summary>
/// Collects a window's input: events in the order they happened since the last frame, plus the
/// current state of keys, buttons and the mouse. The window feeds it; the game and UI read it.
/// </summary>
public sealed class Input
{
    readonly List<InputEvent> _pending = [];
    readonly List<InputEvent> _events = [];
    readonly HashSet<Key> _keysDown = [];
    readonly HashSet<Key> _keysPressed = [];
    readonly bool[] _buttonsDown = new bool[8];
    readonly bool[] _buttonsPressed = new bool[8];
    readonly bool[] _buttonsReleased = new bool[8];

    /// <summary>The events of the current frame, oldest first.</summary>
    public IReadOnlyList<InputEvent> Events => _events;
    public Vector2 MousePosition { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public Vector2 ScrollDelta { get; private set; }
    public bool MouseInside { get; private set; }
    public KeyModifiers Modifiers { get; private set; }

    public bool IsDown(Key key) => _keysDown.Contains(key);
    public bool WasPressed(Key key) => _keysPressed.Contains(key);
    public bool IsDown(MouseButton button) => _buttonsDown[(int)button];
    public bool WasPressed(MouseButton button) => _buttonsPressed[(int)button];
    public bool WasReleased(MouseButton button) => _buttonsReleased[(int)button];

    /// <summary>Moves the events gathered since the last call into <see cref="Events"/> and updates the state.</summary>
    public void BeginFrame()
    {
        _events.Clear();
        _keysPressed.Clear();
        Array.Clear(_buttonsPressed);
        Array.Clear(_buttonsReleased);
        var before = MousePosition;
        ScrollDelta = Vector2.Zero;
        lock (_pending)
        {
            _events.AddRange(_pending);
            _pending.Clear();
        }
        foreach (var e in _events)
        {
            switch (e.Kind)
            {
                case InputEventKind.KeyDown:
                    _keysDown.Add(e.Key);
                    _keysPressed.Add(e.Key);
                    Modifiers = e.Modifiers;
                    break;
                case InputEventKind.KeyRepeat:
                    Modifiers = e.Modifiers;
                    break;
                case InputEventKind.KeyUp:
                    _keysDown.Remove(e.Key);
                    Modifiers = e.Modifiers;
                    break;
                case InputEventKind.MouseMove:
                    MousePosition = e.Position;
                    MouseInside = true;
                    break;
                case InputEventKind.MouseDown:
                    _buttonsDown[(int)e.Button] = true;
                    _buttonsPressed[(int)e.Button] = true;
                    Modifiers = e.Modifiers;
                    break;
                case InputEventKind.MouseUp:
                    _buttonsDown[(int)e.Button] = false;
                    _buttonsReleased[(int)e.Button] = true;
                    Modifiers = e.Modifiers;
                    break;
                case InputEventKind.Scroll:
                    ScrollDelta += e.Scroll;
                    break;
                case InputEventKind.MouseLeave:
                    MouseInside = false;
                    break;
                case InputEventKind.FocusLost:
                    _keysDown.Clear();
                    Array.Clear(_buttonsDown);
                    Modifiers = KeyModifiers.None;
                    break;
            }
        }
        MouseDelta = MousePosition - before;
    }

    /// <summary>Adds an event; it shows in <see cref="Events"/> from the next <see cref="BeginFrame"/>. Thread-safe.</summary>
    public void Push(in InputEvent e)
    {
        lock (_pending)
            _pending.Add(e);
    }
}
