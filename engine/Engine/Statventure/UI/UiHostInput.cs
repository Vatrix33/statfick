using System.Numerics;
using Statventure.Platform;
using Statventure.UI.Dom;

namespace Statventure.UI;

/// <summary>
/// The window's input as the document's, as Chromium maps a desktop's events: a key's DOM value is
/// the character it typed (GLFW's char event right after it), else its name, the keypad's
/// navigation keys without Num Lock, or the character the layout gives it (a letter in capitals
/// with Shift or Caps Lock); its code is its place on a US keyboard; a modifier's own key counts
/// as held while it goes down; wheel notches become pixels (down and right positive); focus and
/// leaving the window pass on.
/// </summary>
internal sealed class UiHostInput(UiInput document, Func<Key, int, string?> keyName)
{
    readonly UiInput _document = document;
    readonly Func<Key, int, string?> _keyName = keyName;
    // The value each held key went down with, and the modifiers then (its keyup reports the same value unless they changed).
    readonly Dictionary<Key, (string Value, KeyModifiers Modifiers)> _keyValues = [];
    Vector2 _pointer;

    /// <summary>Pixels a wheel notch scrolls.</summary>
    public float WheelNotch { get; set; } = 120;

    /// <summary>Passes a frame's events on in order.</summary>
    public void Dispatch(IReadOnlyList<InputEvent> events)
    {
        for (var i = 0; i < events.Count; i++)
        {
            var e = events[i];
            // A key that types sends its character right after it (GLFW's char event).
            var text = i + 1 < events.Count && events[i + 1].Kind == InputEventKind.Text && e.Kind is InputEventKind.KeyDown or InputEventKind.KeyRepeat
                ? char.ConvertFromUtf32(events[++i].Codepoint)
                : null;
            Dispatch(e, text);
        }
    }

    void Dispatch(in InputEvent e, string? text)
    {
        var input = _document;
        var modifiers = e.Modifiers & ~(KeyModifiers.CapsLock | KeyModifiers.NumLock);
        switch (e.Kind)
        {
            case InputEventKind.KeyDown or InputEventKind.KeyRepeat:
            {
                // A modifier's own key counts as held when it goes down (as Chromium reports it).
                modifiers |= ModifierOf(e.Key);
                var value = text ?? KeyValue(e.Key, e.Scancode, e.Modifiers, _keyName);
                _keyValues[e.Key] = (value, modifiers);
                input.KeyDown(value, KeyCode(e.Key), modifiers, repeat: e.Kind == InputEventKind.KeyRepeat);
                break;
            }
            case InputEventKind.KeyUp:
            {
                modifiers &= ~ModifierOf(e.Key);
                var value = _keyValues.Remove(e.Key, out var down) && (down.Modifiers & ~ModifierOf(e.Key)) == modifiers
                    ? down.Value
                    : KeyValue(e.Key, e.Scancode, e.Modifiers, _keyName);
                input.KeyUp(value, KeyCode(e.Key), modifiers);
                break;
            }
            case InputEventKind.Text:
                // Text without a key (an input method's): typed as a key of its own.
                var typed = char.ConvertFromUtf32(e.Codepoint);
                input.KeyDown(typed, "", modifiers);
                input.KeyUp(typed, "", modifiers);
                break;
            case InputEventKind.MouseMove:
                _pointer = e.Position;
                input.PointerMove(e.Position, modifiers);
                break;
            case InputEventKind.MouseDown:
                _pointer = e.Position;
                input.PointerDown(e.Position, e.Button, modifiers);
                break;
            case InputEventKind.MouseUp:
                _pointer = e.Position;
                input.PointerUp(e.Position, e.Button, modifiers);
                break;
            case InputEventKind.Scroll:
                // GLFW counts notches up and left; the page's deltas go down and right (an unturned axis stays +0).
                input.Wheel(_pointer, Vector2.Zero - e.Scroll * WheelNotch, modifiers, precise: false);
                break;
            case InputEventKind.MouseLeave:
                input.PointerLeave();
                break;
            case InputEventKind.FocusLost:
                _keyValues.Clear();
                input.WindowBlur();
                break;
            case InputEventKind.FocusGained:
                input.WindowFocus();
                break;
        }
    }

    static KeyModifiers ModifierOf(Key key) => key switch
    {
        Key.LeftShift or Key.RightShift => KeyModifiers.Shift,
        Key.LeftControl or Key.RightControl => KeyModifiers.Control,
        Key.LeftAlt or Key.RightAlt => KeyModifiers.Alt,
        Key.LeftSuper or Key.RightSuper => KeyModifiers.Super,
        _ => KeyModifiers.None,
    };

    /// <summary>
    /// The DOM key value of a key that typed nothing: a named key, the keypad's navigation keys
    /// without Num Lock, else the character the key types on the layout (a letter in capitals with
    /// Shift or Caps Lock), else "Unidentified".
    /// </summary>
    static string KeyValue(Key key, int scancode, KeyModifiers modifiers, Func<Key, int, string?> keyName)
    {
        if (NamedKey(key, modifiers.HasFlag(KeyModifiers.NumLock)) is { } named)
            return named;
        if (keyName(key, scancode) is not { Length: > 0 } name)
            return "Unidentified";
        var upper = modifiers.HasFlag(KeyModifiers.Shift) != modifiers.HasFlag(KeyModifiers.CapsLock);
        return upper && name.Length == 1 && char.IsLetter(name[0]) ? name.ToUpperInvariant() : name;
    }

    static string? NamedKey(Key key, bool numLock) => key switch
    {
        Key.Escape => "Escape",
        Key.Enter or Key.KeypadEnter => "Enter",
        Key.Tab => "Tab",
        Key.Backspace => "Backspace",
        Key.Insert => "Insert",
        Key.Delete => "Delete",
        Key.Right => "ArrowRight",
        Key.Left => "ArrowLeft",
        Key.Down => "ArrowDown",
        Key.Up => "ArrowUp",
        Key.PageUp => "PageUp",
        Key.PageDown => "PageDown",
        Key.Home => "Home",
        Key.End => "End",
        Key.CapsLock => "CapsLock",
        Key.ScrollLock => "ScrollLock",
        Key.NumLock => "NumLock",
        Key.PrintScreen => "PrintScreen",
        Key.Pause => "Pause",
        >= Key.F1 and <= Key.F25 => "F" + (key - Key.F1 + 1),
        Key.LeftShift or Key.RightShift => "Shift",
        Key.LeftControl or Key.RightControl => "Control",
        Key.LeftAlt or Key.RightAlt => "Alt",
        Key.LeftSuper or Key.RightSuper => "Meta",
        Key.Menu => "ContextMenu",
        // Without Num Lock the keypad moves (X's KP_Home and friends).
        Key.Keypad0 when !numLock => "Insert",
        Key.Keypad1 when !numLock => "End",
        Key.Keypad2 when !numLock => "ArrowDown",
        Key.Keypad3 when !numLock => "PageDown",
        Key.Keypad4 when !numLock => "ArrowLeft",
        Key.Keypad5 when !numLock => "Clear",
        Key.Keypad6 when !numLock => "ArrowRight",
        Key.Keypad7 when !numLock => "Home",
        Key.Keypad8 when !numLock => "ArrowUp",
        Key.Keypad9 when !numLock => "PageUp",
        Key.KeypadDecimal when !numLock => "Delete",
        _ => null,
    };

    /// <summary>The DOM code: the physical key, named by its place on a US keyboard ("" for keys without one).</summary>
    static string KeyCode(Key key) => key switch
    {
        >= Key.A and <= Key.Z => "Key" + (char)key,
        >= Key.D0 and <= Key.D9 => "Digit" + (char)key,
        >= Key.Keypad0 and <= Key.Keypad9 => "Numpad" + (key - Key.Keypad0),
        >= Key.F1 and <= Key.F25 => "F" + (key - Key.F1 + 1),
        Key.Space => "Space",
        Key.Apostrophe => "Quote",
        Key.Comma => "Comma",
        Key.Minus => "Minus",
        Key.Period => "Period",
        Key.Slash => "Slash",
        Key.Semicolon => "Semicolon",
        Key.Equal => "Equal",
        Key.LeftBracket => "BracketLeft",
        Key.Backslash => "Backslash",
        Key.RightBracket => "BracketRight",
        Key.GraveAccent => "Backquote",
        Key.World1 or Key.World2 => "IntlBackslash",
        Key.Escape => "Escape",
        Key.Enter => "Enter",
        Key.Tab => "Tab",
        Key.Backspace => "Backspace",
        Key.Insert => "Insert",
        Key.Delete => "Delete",
        Key.Right => "ArrowRight",
        Key.Left => "ArrowLeft",
        Key.Down => "ArrowDown",
        Key.Up => "ArrowUp",
        Key.PageUp => "PageUp",
        Key.PageDown => "PageDown",
        Key.Home => "Home",
        Key.End => "End",
        Key.CapsLock => "CapsLock",
        Key.ScrollLock => "ScrollLock",
        Key.NumLock => "NumLock",
        Key.PrintScreen => "PrintScreen",
        Key.Pause => "Pause",
        Key.KeypadDecimal => "NumpadDecimal",
        Key.KeypadDivide => "NumpadDivide",
        Key.KeypadMultiply => "NumpadMultiply",
        Key.KeypadSubtract => "NumpadSubtract",
        Key.KeypadAdd => "NumpadAdd",
        Key.KeypadEnter => "NumpadEnter",
        Key.KeypadEqual => "NumpadEqual",
        Key.LeftShift => "ShiftLeft",
        Key.LeftControl => "ControlLeft",
        Key.LeftAlt => "AltLeft",
        Key.LeftSuper => "MetaLeft",
        Key.RightShift => "ShiftRight",
        Key.RightControl => "ControlRight",
        Key.RightAlt => "AltRight",
        Key.RightSuper => "MetaRight",
        Key.Menu => "ContextMenu",
        _ => "",
    };
}
