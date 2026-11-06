using Statventure.UI.Dom;

namespace Statventure.Editor.Shell;

/// <summary>
/// The dialogs open over the shell, as a modal dialog is in HTML (the engine has no <c>&lt;dialog&gt;</c>; the
/// game's modals are <c>aria-modal</c> elements with the rest <c>inert</c> too): each one a layer over everything
/// (<c>div.dialog-layer</c>, the last child of the body) holding its box (<c>div.dialog</c>, role dialog).
/// While one is open the shell and every dialog under the topmost one are inert, so neither the pointer nor
/// Tab reaches them; Escape cancels the topmost one.
/// </summary>
public sealed class EditorDialogs
{
    readonly EditorView _view;
    readonly List<EditorDialog> _open = [];
    int _made;

    internal EditorDialogs(EditorView view) => _view = view;

    /// <summary>The topmost open dialog, or null.</summary>
    public EditorDialog? Top => _open.Count > 0 ? _open[^1] : null;

    /// <summary>The open dialogs, the topmost last.</summary>
    public IReadOnlyList<EditorDialog> Open => _open;

    /// <summary>A new dialog with <paramref name="title"/>, not yet shown: fill its content and buttons, then <see cref="EditorDialog.Show"/>.</summary>
    public EditorDialog Create(string title) => new(this, _view.Document, title, ++_made);

    /// <summary>
    /// Asks: a dialog with a message and buttons, each a text and the value it closes the dialog with (null for
    /// one that cancels, as Escape does); the first button with a value is the primary one. Gives the value.
    /// </summary>
    public Task<string?> Ask(string title, string message, params (string Text, string? Value)[] buttons)
    {
        var dialog = Create(title);
        dialog.Content.Append("p", text: message);
        var primary = true;
        foreach (var (text, value) in buttons)
        {
            dialog.AddButton(text, value, primary && value is not null);
            primary &= value is null;
        }
        dialog.Show();
        return dialog.Closed;
    }

    internal void Opened(EditorDialog dialog)
    {
        foreach (var below in _open)
            below.Layer.Inert = true;
        _view.Shell.Inert = true;
        _open.Add(dialog);
    }

    internal void Closed(EditorDialog dialog)
    {
        _open.Remove(dialog);
        if (Top is { } top)
            top.Layer.Inert = false;
        else
            _view.Shell.Inert = false;
    }
}

/// <summary>
/// A dialog: a title, the caller's content and a row of buttons. Shown, it takes the focus as Chromium's modal
/// dialog does (<c>Element::GetFocusDelegate</c>: the first <c>autofocus</c> element in it, else its first
/// keyboard-focusable element, else the box itself); a press on the layer outside the box leaves the focus where
/// it is. Closed (by a button, Escape or
/// <see cref="Close"/>), it goes, the focus returns to where it was before it opened (when that can still take
/// it), and <see cref="Closed"/> gives the value.
/// </summary>
public sealed class EditorDialog
{
    readonly EditorDialogs _dialogs;
    readonly UiDocument _document;
    readonly TaskCompletionSource<string?> _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    UiElement? _before;

    internal EditorDialog(EditorDialogs dialogs, UiDocument document, string title, int number)
    {
        _dialogs = dialogs;
        _document = document;
        Layer = new UiElement("div");
        Layer.ClassName = "dialog-layer";
        Element = Layer.Append("div", "dialog");
        Element.SetAttribute("role", "dialog");
        Element.SetAttribute("aria-modal", "true");
        Element.SetAttribute("aria-labelledby", $"dialog-{number}-title");
        Element.SetAttribute("tabindex", "-1");
        Element.Append("h2", text: title).Id = $"dialog-{number}-title";
        Content = Element.Append("div", "dialog-content");
        Buttons = Element.Append("div", "dialog-buttons");
        Layer.AddEventListener<UiMouseEvent>("mousedown", e =>
        {
            if (e.Target == Layer)
                e.PreventDefault();
        });
    }

    /// <summary>The layer over the shell (inert while a dialog is open above it).</summary>
    public UiElement Layer { get; }

    /// <summary>The dialog's box.</summary>
    public UiElement Element { get; }

    /// <summary>Where the caller's content goes.</summary>
    public UiElement Content { get; }

    /// <summary>The row of buttons.</summary>
    public UiElement Buttons { get; }

    /// <summary>The value the dialog closed with (null: cancelled).</summary>
    public Task<string?> Closed => _closed.Task;

    public bool IsOpen => Layer.Parent is not null;

    /// <summary>A button that closes the dialog with <paramref name="value"/> (null: cancels); the primary one stands out.</summary>
    public UiElement AddButton(string text, string? value, bool primary = false)
    {
        var button = Buttons.Append("button", primary ? "primary" : null, text);
        button.SetAttribute("type", "button");
        button.AddEventListener("click", _ => Close(value));
        return button;
    }

    /// <summary>Shows the dialog over everything else, the rest inert, and focuses it.</summary>
    public void Show()
    {
        if (IsOpen || _closed.Task.IsCompleted)
            throw new InvalidOperationException("a dialog is shown once");
        _before = _document.FocusedElement;
        _document.Body.Append(Layer);
        _dialogs.Opened(this);
        // Focusable means laid out and not inert: the layer's boxes and the shell's inertness first.
        _document.UpdateLayout();
        var inside = Element.Descendants().ToList();
        var target = inside.FirstOrDefault(e => e.HasAttribute("autofocus") && e.IsFocusable)
            ?? inside.FirstOrDefault(e => e.IsFocusable && (e.TabIndex ?? 0) >= 0)
            ?? Element;
        target.Focus();
    }

    /// <summary>Closes the dialog with <paramref name="value"/> (null: cancelled); nothing when it isn't open.</summary>
    public void Close(string? value)
    {
        if (!IsOpen)
            return;
        var hadFocus = _document.FocusedElement is { } focused && (focused == Element || focused.IsDescendantOf(Element));
        Layer.Remove();
        _dialogs.Closed(this);
        if (hadFocus && _before is { Document: not null } before)
        {
            _document.UpdateLayout();
            if (before.IsFocusable)
                before.Focus();
        }
        _closed.TrySetResult(value);
    }
}
