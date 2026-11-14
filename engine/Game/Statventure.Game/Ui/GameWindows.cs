using Statventure.Game.Sounds;
using Statventure.UI.Dom;

namespace Statventure.Game.Ui;

/// <summary>
/// The game's windows, as the old game's <c>.modal-layer</c>s behave (<c>old-game/scripts/app.js</c> 5751–5942,
/// <c>music.js</c> 1164–1196, the modules' <c>open</c> and <c>close</c>; Docs/Game/Foundation.md, "Dialogs, tooltips and
/// the window manager"): a window is a layer (a <c>.modal-layer</c> with its backdrop and dialog) shown over the page.
/// <see cref="Open"/> shows it (put in the document's body when it isn't in the document; its <c>hidden</c> taken away),
/// sets <c>body.modal-open</c> (the old <c>updateModalState</c>), plays <c>ui-open</c> and moves the focus into it: the
/// element asked for, else its <c>.modal-close</c> (as most old windows do), else its first control. While it is the
/// topmost, Tab and Shift+Tab stay among its controls (the old settings window's trap: the buttons, fields and selects
/// that aren't disabled, not the backdrop; from the last to the first and back, and into it from outside), and Escape
/// closes it (or does what the window asked for). <see cref="Close"/> hides it (<c>hidden</c>; taken out again when
/// <see cref="Open"/> put it in), takes <c>modal-open</c> away when no window is left, plays <c>ui-close</c> and gives the
/// focus back to where it was before the window opened (the old modules focus their opening button again). With no
/// window open, Escape runs <c>open-menu</c> (Q7).
/// </summary>
public sealed class GameWindows
{
    /// <summary>The class <c>body</c> has while a window is open (the old <c>updateModalState</c>).</summary>
    public const string ModalOpenClass = "modal-open";

    sealed class Shown(UiElement layer, UiElement? before, bool added, Action? escape)
    {
        public UiElement Layer { get; } = layer;
        public UiElement? Before { get; set; } = before;
        public bool Added { get; } = added;
        public Action? Escape { get; } = escape;
    }

    readonly UiDocument _document;
    readonly Action<string> _sound;
    readonly List<Shown> _open = [];

    internal GameWindows(UiDocument document, Action<string> sound)
    {
        _document = document;
        _sound = sound;
    }

    /// <summary>The topmost open window, or null.</summary>
    public UiElement? Top => _open.Count > 0 ? _open[^1].Layer : null;

    /// <summary>The open windows, the topmost last.</summary>
    public IReadOnlyList<UiElement> Layers => [.. _open.Select(w => w.Layer)];

    /// <summary>Whether <paramref name="layer"/> is open.</summary>
    public bool IsOpen(UiElement layer) => _open.Any(w => w.Layer == layer);

    /// <summary>A window opened (after its focus moved in).</summary>
    public event Action<UiElement>? Opened;

    /// <summary>A window closed (after the focus was given back).</summary>
    public event Action<UiElement>? Closed;

    /// <summary>
    /// Opens <paramref name="layer"/> on top: in the body if it isn't in the document, shown, <c>body.modal-open</c>,
    /// <c>ui-open</c>, the focus moved to <paramref name="focus"/> (else the layer's <c>.modal-close</c>, else its first
    /// control). <paramref name="escape"/> is what Escape does while it is the topmost (by default: it closes). A window
    /// already open stays as it is.
    /// </summary>
    public void Open(UiElement layer, UiElement? focus = null, Action? escape = null)
    {
        if (IsOpen(layer))
            return;
        var added = layer.Document != _document;
        if (added)
            _document.Body.Append(layer);
        layer.Hidden = false;
        _open.Add(new Shown(layer, _document.FocusedElement, added, escape));
        UpdateModalState();
        _sound(SoundNames.Open);
        // Focusable means laid out: the layer's boxes first.
        _document.UpdateLayout();
        var target = focus ?? layer.Descendants().FirstOrDefault(e => e.Matches(".modal-close") && e.IsFocusable) ?? Controls(layer).FirstOrDefault();
        target?.Focus();
        Opened?.Invoke(layer);
    }

    /// <summary>
    /// Closes <paramref name="layer"/>: hidden (taken out of the document when <see cref="Open"/> put it in), <c>modal-open</c>
    /// gone when no window is left, <c>ui-close</c>, the focus given back to where it was before the window opened (when the
    /// window was the topmost and that element can take it). Nothing when it isn't open.
    /// </summary>
    public void Close(UiElement layer)
    {
        var index = _open.FindIndex(w => w.Layer == layer);
        if (index < 0)
            return;
        var shown = _open[index];
        var wasTop = index == _open.Count - 1;
        _open.RemoveAt(index);
        // A window above it that was opened from inside it gives the focus back to where this one's came from.
        foreach (var above in _open.Skip(index))
            if (above.Before is { } before && (before == layer || before.IsDescendantOf(layer)))
                above.Before = shown.Before;
        layer.Hidden = true;
        if (shown.Added)
            layer.Remove();
        UpdateModalState();
        _sound(SoundNames.Close);
        if (wasTop && shown.Before is { } back && back.Document == _document)
        {
            _document.UpdateLayout();
            if (back.IsFocusable)
                back.Focus();
        }
        Closed?.Invoke(layer);
    }

    /// <summary>Closes the topmost window; false when none is open.</summary>
    public bool CloseTop()
    {
        if (Top is not { } top)
            return false;
        Close(top);
        return true;
    }

    /// <summary>Escape: the topmost window does what it asked for (by default it closes); false when none is open.</summary>
    internal bool Escape()
    {
        if (_open.Count == 0)
            return false;
        var top = _open[^1];
        if (top.Escape is { } escape)
            escape();
        else
            Close(top.Layer);
        return true;
    }

    /// <summary>
    /// Tab with a window open (the old settings window's trap, <c>music.js</c> 1186–1195): from its last control to its
    /// first, Shift+Tab from its first to its last, and from outside it into it; true when the focus was moved.
    /// </summary>
    internal bool Tab(bool backwards)
    {
        if (Top is not { } top)
            return false;
        var controls = Controls(top).ToList();
        if (controls.Count == 0)
            return false;
        var focused = _document.FocusedElement;
        UiElement? target = null;
        if (focused is null || !(focused == top || focused.IsDescendantOf(top)))
            target = backwards ? controls[^1] : controls[0];
        else if (backwards && focused == controls[0])
            target = controls[^1];
        else if (!backwards && focused == controls[^1])
            target = controls[0];
        if (target is null)
            return false;
        target.Focus();
        return true;
    }

    /// <summary>The window's controls the trap moves among: buttons, fields and selects that can take the focus (not disabled: <c>IsFocusable</c>), but not the backdrop.</summary>
    static IEnumerable<UiElement> Controls(UiElement layer) =>
        layer.Descendants().Where(e => e.Tag is "button" or "input" or "select" or "textarea" && !e.HasClass("modal-backdrop") && e.IsFocusable);

    void UpdateModalState() => _document.Body.ToggleClass(ModalOpenClass, _open.Count > 0);
}
