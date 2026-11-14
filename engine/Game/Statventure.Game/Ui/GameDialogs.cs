using Statventure.Game.Sounds;
using Statventure.UI.Dom;

namespace Statventure.Game.Ui;

/// <summary>
/// The confirm and prompt dialogs in place of the old <c>window.confirm</c> (12 uses) and <c>window.prompt</c> (5 uses)
/// (Docs/Game/Foundation.md, "Dialogs, tooltips and the window manager"; GM1-8), each a window of
/// <see cref="GameWindows"/> and a task finished when the player answers. Their markup is the old game's alert dialogs'
/// (<c>old-game/index.html</c> 797–824, the travel encounter and the regional border: a <c>.modal-layer.map-encounter-layer</c>
/// with a backdrop that does nothing, a <c>.map-encounter-dialog</c>, its <c>.map-encounter-actions</c> of two
/// <c>.backup-button</c>s, the one that stays first) with the classes of their own in <c>dialogs.css</c>
/// (<c>.game-dialog</c>, <c>.game-confirm</c>, <c>.game-prompt</c>). A confirm: the question, Cancel and the button named
/// for the action, which has the focus. A prompt: the question as the label of a text field holding the value (focused, its
/// text selected), Cancel and the OK button; Enter in the field is OK; a check may refuse the text, which then stays,
/// its problem in the field's validation bubble and <c>ui-refused</c> played. Escape is Cancel, and so is the dialog's
/// window closed by anything else.
/// </summary>
public sealed class GameDialogs
{
    readonly UiDocument _document;
    readonly GameWindows _windows;
    readonly Action<string> _sound;
    int _made;

    internal GameDialogs(UiDocument document, GameWindows windows, Action<string> sound)
    {
        _document = document;
        _windows = windows;
        _sound = sound;
    }

    /// <summary>The words of the button that cancels a dialog.</summary>
    public const string CancelText = "Cancel";

    /// <summary>The topmost dialog's layer while one is open (for the screens' checks and the tests), or null.</summary>
    public UiElement? Open => _windows.Layers.LastOrDefault(l => l.HasClass("game-dialog-layer"));

    /// <summary>Asks <paramref name="question"/> with a button <paramref name="yes"/> and Cancel: true for yes.</summary>
    public Task<bool> Confirm(string question, string yes)
    {
        var answered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var number = ++_made;
        var layer = Layer();
        var dialog = layer.Append("section", "map-encounter-dialog game-dialog game-confirm");
        dialog.SetAttribute("role", "alertdialog");
        dialog.SetAttribute("aria-modal", "true");
        dialog.SetAttribute("aria-describedby", $"game-dialog-{number}-question");
        var text = dialog.Append("p", "game-dialog-question", question);
        text.Id = $"game-dialog-{number}-question";
        var (cancel, accept) = Actions(dialog, yes, submit: false);
        void Answer(bool value)
        {
            answered.TrySetResult(value);
            _windows.Close(layer);
        }
        cancel.AddEventListener("click", _ => Answer(false));
        accept.AddEventListener("click", _ => Answer(true));
        CancelWhenClosed(layer, () => answered.TrySetResult(false));
        _windows.Open(layer, accept, () => Answer(false));
        return answered.Task;
    }

    /// <summary>
    /// Asks <paramref name="question"/> (its line breaks kept) with a text field holding <paramref name="value"/>,
    /// <paramref name="ok"/> and Cancel: the text, or null for Cancel. <paramref name="check"/> may refuse a text: its
    /// problem (null when the text will do) shows in the field's validation bubble, <c>ui-refused</c> plays and the dialog
    /// stays. <paramref name="maxLength"/> limits what can be typed (the field's <c>maxlength</c>).
    /// </summary>
    public Task<string?> Prompt(string question, string value, string ok, Func<string, string?>? check = null, int? maxLength = null)
    {
        var answered = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var number = ++_made;
        var layer = Layer();
        // The dialog is the form: Enter in the field submits it (implicit submission); the game checks the text itself.
        var dialog = layer.Append("form", "map-encounter-dialog game-dialog game-prompt");
        dialog.SetAttribute("role", "dialog");
        dialog.SetAttribute("aria-modal", "true");
        dialog.SetAttribute("aria-labelledby", $"game-dialog-{number}-question");
        dialog.SetAttribute("novalidate", "");
        var label = dialog.Append("label", "game-dialog-question", question);
        label.Id = $"game-dialog-{number}-question";
        label.SetAttribute("for", $"game-dialog-{number}-field");
        var field = dialog.Append("input", "game-prompt-field");
        field.Id = $"game-dialog-{number}-field";
        field.SetAttribute("type", "text");
        field.SetAttribute("autocomplete", "off");
        field.SetAttribute("spellcheck", "false");
        if (maxLength is { } max)
            field.SetAttribute("maxlength", max.ToString(System.Globalization.CultureInfo.InvariantCulture));
        field.Value = value;
        var (cancel, accept) = Actions(dialog, ok, submit: true);
        void Answer(string? text)
        {
            answered.TrySetResult(text);
            _windows.Close(layer);
        }
        field.AddEventListener("input", _ => field.SetCustomValidity(""));
        dialog.AddEventListener("submit", e =>
        {
            e.PreventDefault();
            var text = field.Value;
            if (check?.Invoke(text) is { } problem)
            {
                field.SetCustomValidity(problem);
                field.ReportValidity();
                _sound(SoundNames.Refused);
                return;
            }
            Answer(text);
        });
        cancel.AddEventListener("click", _ => Answer(null));
        CancelWhenClosed(layer, () => answered.TrySetResult(null));
        _windows.Open(layer, field, () => Answer(null));
        field.Select();
        return answered.Task;
    }

    /// <summary>A dialog closed by anything else than its answer (another part closing the topmost window) is cancelled.</summary>
    void CancelWhenClosed(UiElement layer, Action cancel)
    {
        void Closed(UiElement closed)
        {
            if (closed != layer)
                return;
            _windows.Closed -= Closed;
            cancel();
        }
        _windows.Closed += Closed;
    }

    /// <summary>A dialog's layer: the old alert dialogs' (<c>.modal-layer.map-encounter-layer</c>) with a backdrop that does nothing.</summary>
    static UiElement Layer()
    {
        var layer = new UiElement("div") { ClassName = "modal-layer map-encounter-layer game-dialog-layer" };
        var backdrop = layer.Append("div", "modal-backdrop");
        backdrop.SetAttribute("aria-hidden", "true");
        return layer;
    }

    /// <summary>The dialog's two buttons in the old order (the one that stays first): Cancel, then the action.</summary>
    static (UiElement Cancel, UiElement Accept) Actions(UiElement dialog, string accept, bool submit)
    {
        var actions = dialog.Append("div", "map-encounter-actions game-dialog-actions");
        var cancel = actions.Append("button", "backup-button game-dialog-cancel", CancelText);
        cancel.SetAttribute("type", "button");
        var yes = actions.Append("button", "backup-button game-dialog-accept", accept);
        yes.SetAttribute("type", submit ? "submit" : "button");
        return (cancel, yes);
    }
}
