using Statventure.UI.Dom;

namespace Statventure.Editor.Shell;

/// <summary>The editor's commands: the top bar's buttons and the keys the shell maps (Docs/Editor.md, "The shell (step 4)").</summary>
public enum EditorCommand
{
    NewMod,
    Open,
    Save,
    SaveAs,
    Import,
    Undo,
    Redo,
    Filter,
    Rename,
    Remove,
    ZoomIn,
    ZoomOut,
}

/// <summary>
/// A command asked for: by a key (its event and the element it went to: a text field's own editing may want
/// Ctrl+Z, P13) or by the top bar's button.
/// </summary>
public sealed record CommandRequest(EditorCommand Command, UiKeyboardEvent? Key = null, UiElement? Target = null)
{
    /// <summary>The key went to an element that takes text (a text field, a textarea, contenteditable).</summary>
    public bool FromTextField => Target is { } target && EditorCommands.TakesText(target);
}

/// <summary>
/// Who does what for each command. Parts register handlers (<see cref="Handle"/>); a command runs its handlers in
/// the order they were registered until one says it acted. A command is available when one of its handlers is
/// (no handler: never; the top bar's button is then disabled).
/// </summary>
public sealed class EditorCommands
{
    sealed record Handler(Func<CommandRequest, bool> Run, Func<bool>? CanRun);

    readonly Dictionary<EditorCommand, List<Handler>> _handlers = [];

    /// <summary>
    /// Handles <paramref name="command"/>: <paramref name="run"/> gives whether it acted (false leaves the
    /// request to the next handler, and a key to its element's own action); <paramref name="canRun"/> (none:
    /// always) whether it can now.
    /// </summary>
    public void Handle(EditorCommand command, Func<CommandRequest, bool> run, Func<bool>? canRun = null)
    {
        if (!_handlers.TryGetValue(command, out var list))
            _handlers[command] = list = [];
        list.Add(new Handler(run, canRun));
        Changed?.Invoke();
    }

    /// <summary>Whether a handler of <paramref name="command"/> can run now.</summary>
    public bool CanRun(EditorCommand command) =>
        _handlers.TryGetValue(command, out var list) && list.Exists(h => h.CanRun?.Invoke() ?? true);

    /// <summary>Runs the request's handlers that can run, in order, until one acts; gives whether one did.</summary>
    public bool Run(CommandRequest request)
    {
        if (!_handlers.TryGetValue(request.Command, out var list))
            return false;
        foreach (var handler in list.ToList())
            if ((handler.CanRun?.Invoke() ?? true) && handler.Run(request))
                return true;
        return false;
    }

    public bool Run(EditorCommand command) => Run(new CommandRequest(command));

    /// <summary>A handler was added (the view refreshes its buttons).</summary>
    public event Action? Changed;

    /// <summary>Elements whose keys are text editing: text fields, textareas, contenteditable (Blink's MayTriggerVirtualKeyboard).</summary>
    public static bool TakesText(UiElement element) =>
        element.IsTextField || element.GetAttribute("contenteditable") is { } editable && editable.ToLowerInvariant() is "" or "true" or "plaintext-only";
}
