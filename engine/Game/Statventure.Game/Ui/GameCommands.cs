using Statventure.Core;
using Statventure.Platform;
using Statventure.UI.Dom;

namespace Statventure.Game.Ui;

/// <summary>
/// A key with its modifiers, as an Electron accelerator names one ("Ctrl+S", "Ctrl+Plus", "Ctrl+-", "F11"): the key and
/// exactly the modifiers held (Ctrl, Shift, Alt, Meta), as Electron matches a menu's accelerator (<c>ui::Accelerator</c>:
/// the key code and the modifiers). The key is the key's unshifted character on a US keyboard, as Electron's
/// <c>KeyboardCodeFromCharCode</c> takes a shifted character as its key with Shift (so "Plus" is the =/+ key with Shift),
/// a letter in lower case; or a named key ("F11", "Escape").
/// </summary>
public readonly record struct KeyChord(string Key, KeyModifiers Modifiers)
{
    const KeyModifiers Held = KeyModifiers.Control | KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Super;

    // Electron's KeyboardCodeFromCharCode: each shifted character of a US keyboard is its key with Shift.
    static readonly Dictionary<char, char> Unshifted = new()
    {
        ['!'] = '1', ['@'] = '2', ['#'] = '3', ['$'] = '4', ['%'] = '5', ['^'] = '6', ['&'] = '7', ['*'] = '8', ['('] = '9', [')'] = '0',
        ['_'] = '-', ['+'] = '=', ['{'] = '[', ['}'] = ']', ['|'] = '\\', [':'] = ';', ['"'] = '\'', ['<'] = ',', ['>'] = '.', ['?'] = '/', ['~'] = '`',
    };

    /// <summary>
    /// The chord an accelerator's text names: tokens joined by '+' (a trailing "-" is the minus key), the modifiers Ctrl
    /// (Control, CmdOrCtrl, CommandOrControl: Ctrl on Linux and Windows), Shift, Alt (Option), Meta (Super, Cmd); the
    /// key a character or a name; "Plus" and a shifted character add Shift, as Electron's parser does.
    /// </summary>
    public static KeyChord Parse(string text)
    {
        var modifiers = KeyModifiers.None;
        string? key = null;
        // Electron's StringToAccelerator: split at '+', empty tokens dropped ("Ctrl+-" is the minus key; '+' itself is "Plus").
        foreach (var token in text.Split('+').Select(t => t.Trim()).Where(t => t.Length > 0))
        {
            switch (token.ToLowerInvariant())
            {
                case "ctrl" or "control" or "cmdorctrl" or "commandorcontrol":
                    modifiers |= KeyModifiers.Control;
                    continue;
                case "shift":
                    modifiers |= KeyModifiers.Shift;
                    continue;
                case "alt" or "option":
                    modifiers |= KeyModifiers.Alt;
                    continue;
                case "meta" or "super" or "cmd" or "command":
                    modifiers |= KeyModifiers.Super;
                    continue;
                case "plus":
                    key = "=";
                    modifiers |= KeyModifiers.Shift;
                    continue;
                case "esc" or "escape":
                    key = "Escape";
                    continue;
            }
            if (token.Length == 1)
            {
                var c = char.ToLowerInvariant(token[0]);
                if (Unshifted.TryGetValue(c, out var unshifted))
                {
                    c = unshifted;
                    modifiers |= KeyModifiers.Shift;
                }
                key = c.ToString();
            }
            else if (token.Length is 2 or 3 && (token[0] is 'F' or 'f') && int.TryParse(token[1..], out var f) && f is >= 1 and <= 24)
                key = "F" + f;
            else
                throw new FormatException($"'{token}' in '{text}' isn't a key the game binds");
        }
        return key is null ? throw new FormatException($"'{text}' names no key") : new KeyChord(key, modifiers);
    }

    /// <summary>The chord a key event is: its key value taken to its unshifted US character (a letter in lower case), the modifiers it was pressed with.</summary>
    public static KeyChord Of(UiKeyboardEvent e)
    {
        var modifiers = (e.CtrlKey ? KeyModifiers.Control : 0) | (e.ShiftKey ? KeyModifiers.Shift : 0) | (e.AltKey ? KeyModifiers.Alt : 0) | (e.MetaKey ? KeyModifiers.Super : 0);
        var key = e.Key;
        if (key.Length == 1)
        {
            var c = char.ToLowerInvariant(key[0]);
            key = (Unshifted.TryGetValue(c, out var unshifted) ? unshifted : c).ToString();
        }
        return new KeyChord(key, modifiers & Held);
    }
}

/// <summary>
/// The commands (Docs/Game/Foundation.md, "Who calls whom", the commands and who registers them): each registered once by
/// its part or screen, run by name with an optional argument (<c>open-slot</c>'s slot); a command not registered yet does
/// nothing and is logged, so every step runs with the others missing. Keys are bound to command names
/// (<see cref="Bind"/>): <see cref="GameUi"/> binds the game's keys at its start (Ctrl+S, Ctrl+O, the zoom's, F11, Quit's)
/// and runs a bound command when its key reaches the document unhandled.
/// </summary>
public sealed class GameCommands
{
    readonly Dictionary<string, (Action<string?> Run, Func<bool>? Enabled)> _commands = new(StringComparer.Ordinal);
    readonly Dictionary<KeyChord, (string Keys, string Command)> _keys = [];

    /// <summary>The names registered.</summary>
    public IReadOnlyCollection<string> Names => _commands.Keys;

    /// <summary>Registers <paramref name="name"/>; <paramref name="enabled"/> says when it can run (always without it). A name registered twice is a mistake.</summary>
    public void Register(string name, Action<string?> run, Func<bool>? enabled = null)
    {
        if (!_commands.TryAdd(name, (run, enabled)))
            throw new InvalidOperationException($"the command {name} is registered already");
    }

    /// <summary>Whether <paramref name="name"/> is registered.</summary>
    public bool IsRegistered(string name) => _commands.ContainsKey(name);

    /// <summary>Whether <paramref name="name"/> is registered and can run now.</summary>
    public bool IsEnabled(string name) => _commands.TryGetValue(name, out var command) && command.Enabled?.Invoke() != false;

    /// <summary>Runs <paramref name="name"/>; false (and logged) when it isn't registered or can't run now.</summary>
    public bool Run(string name, string? argument = null)
    {
        if (!_commands.TryGetValue(name, out var command))
        {
            Log.Info("Commands", $"{name}: not registered yet, nothing done");
            return false;
        }
        if (command.Enabled?.Invoke() == false)
        {
            Log.Info("Commands", $"{name}: not possible now, nothing done");
            return false;
        }
        command.Run(argument);
        return true;
    }

    /// <summary>
    /// Binds the keys <paramref name="keys"/> (an accelerator's text: "Ctrl+S", "Ctrl+Plus", "F11"; <see cref="KeyChord.Parse"/>)
    /// to the command <paramref name="command"/>, registered or not yet. Keys bound twice are a mistake.
    /// </summary>
    public void Bind(string keys, string command)
    {
        if (!_keys.TryAdd(KeyChord.Parse(keys), (keys, command)))
            throw new InvalidOperationException($"the keys {keys} are bound already");
    }

    /// <summary>The bound keys (their text as bound) and their commands, in the order bound.</summary>
    public IReadOnlyList<(string Keys, string Command)> Bindings => [.. _keys.Values];

    /// <summary>The keys bound to <paramref name="command"/> as bound ("Ctrl+S"), or null.</summary>
    public string? KeysOf(string command) => _keys.Values.Where(k => k.Command == command).Select(k => k.Keys).FirstOrDefault();

    /// <summary>The command bound to the keys of <paramref name="e"/>, or null.</summary>
    public string? CommandFor(UiKeyboardEvent e) => _keys.TryGetValue(KeyChord.Of(e), out var bound) ? bound.Command : null;
}
