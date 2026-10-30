using Statventure.Platform;

namespace Statventure.UI.Dom;

/// <summary>
/// DOM names for platform keys: <see cref="Code"/> names the physical key (UI Events code values,
/// "KeyA", "Digit1", "ArrowLeft" ...); <see cref="NamedKey"/> gives the key value of keys that
/// don't type a character ("Enter", "Escape", "Shift" ...). For keys that type, the key value is
/// the character typed (the window host takes it from the text input that follows).
/// </summary>
public static class KeyNames
{
    public static string Code(Key key) => key switch
    {
        >= Key.A and <= Key.Z => "Key" + (char)('A' + (key - Key.A)),
        >= Key.D0 and <= Key.D9 => "Digit" + (char)('0' + (key - Key.D0)),
        >= Key.F1 and <= Key.F12 => "F" + (key - Key.F1 + 1),
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
        Key.KeypadEnter => "NumpadEnter",
        Key.LeftShift => "ShiftLeft",
        Key.LeftControl => "ControlLeft",
        Key.LeftAlt => "AltLeft",
        Key.LeftSuper => "MetaLeft",
        Key.RightShift => "ShiftRight",
        Key.RightControl => "ControlRight",
        Key.RightAlt => "AltRight",
        Key.RightSuper => "MetaRight",
        Key.Menu => "ContextMenu",
        _ => "Unidentified",
    };

    /// <summary>The key value of a key that types no character; null for keys that type one.</summary>
    public static string? NamedKey(Key key) => key switch
    {
        >= Key.F1 and <= Key.F12 => "F" + (key - Key.F1 + 1),
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
        Key.LeftShift or Key.RightShift => "Shift",
        Key.LeftControl or Key.RightControl => "Control",
        Key.LeftAlt or Key.RightAlt => "Alt",
        Key.LeftSuper or Key.RightSuper => "Meta",
        Key.Menu => "ContextMenu",
        Key.Unknown => "Unidentified",
        _ => null,
    };
}
