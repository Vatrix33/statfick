using Statventure.Game.Ui;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Sheet;

/// <summary>
/// The Field tools rail as a drawer on a narrow window (Q34; the old <c>old-game/scripts/interface.js</c>, its first part,
/// ported line by line): while the page is 840 px wide or less (<c>matchMedia("(max-width: 840px)")</c>; the window's
/// smallest width is 820, and a page zoom narrows it further) the rail is off the page, shown by the "☰ Field tools"
/// toggle and hidden again by its shade, Escape or a tool's click; Tab stays among its controls and the toggle while it is
/// open; the rail is inert while hidden. A wider page shows the rail always (game.css). Nothing of the character changes.
/// </summary>
sealed class FieldTools
{
    /// <summary>The width at which the rail becomes a drawer (the old <c>interface.js</c>'s and game.css's).</summary>
    public const string CompactQuery = "(max-width: 840px)";

    /// <summary>The body's class while the drawer is open (the old <c>field-tools-open</c>).</summary>
    public const string OpenClass = "field-tools-open";

    readonly UiDocument _document;
    readonly GameWindows _windows;
    readonly UiElement _toggle, _shade, _menu;
    readonly UiMediaQueryList _compact;
    bool _open;

    public FieldTools(UiDocument document, GameWindows windows)
    {
        _document = document;
        _windows = windows;
        _toggle = document.QuerySelector("#field-tools-toggle")!;
        _shade = document.QuerySelector("#field-tools-shade")!;
        _menu = document.QuerySelector("#field-tools-menu")!;
        _compact = document.MatchMedia(CompactQuery);
        _toggle.AddEventListener("click", _ =>
        {
            if (_open)
            {
                Close();
                return;
            }
            _open = true;
            Update();
            Focusable().FirstOrDefault()?.Focus();
        });
        _shade.AddEventListener("click", _ => Close());
        // Navigation opens its own window first: the drawer doesn't take that window's focus back.
        _menu.AddEventListener("click", e =>
        {
            for (var element = e.Target; element is not null && element != _menu; element = element.Parent)
                if (element.Matches(".main-tools-actions button, [data-companion-open]"))
                {
                    Close(restoreFocus: false);
                    return;
                }
        });
        // Before the game's own keys (Escape opens the menu when no window is open): a window over the page takes them.
        document.AddEventListener<UiKeyboardEvent>("keydown", Keys, capture: true);
        _compact.AddChangeListener(_ => Close(restoreFocus: false));
        Update();
    }

    /// <summary>Whether the page is narrow enough for the drawer.</summary>
    public bool Compact => _compact.Matches;

    /// <summary>Whether the drawer is open (only while <see cref="Compact"/>).</summary>
    public bool IsOpen => _open;

    /// <summary>The rail's controls that can take the focus: its enabled buttons and fields, laid out.</summary>
    IEnumerable<UiElement> Focusable()
    {
        _document.UpdateLayout();
        return _menu.Descendants().Where(e => (e.Tag is "button" or "input" && !e.IsDisabled || e.Tag == "a" && e.HasAttribute("href"))
            && e.GetClientRects().Count > 0);
    }

    /// <summary>The rail shown or hidden as the page's width and the drawer say (the old <c>update</c>).</summary>
    public void Update()
    {
        var compact = _compact.Matches;
        var visible = !compact || _open;
        _menu.Inert = !visible;
        _menu.SetAttribute("aria-hidden", visible ? null : "true");
        _document.Body.ToggleClass(OpenClass, compact && _open);
        _toggle.SetAttribute("aria-expanded", compact && _open ? "true" : "false");
        _shade.Hidden = !(compact && _open);
    }

    /// <summary>Closes the drawer; the focus back on the toggle when it was open (not when a tool takes it).</summary>
    public void Close(bool restoreFocus = true)
    {
        var wasOpen = _open;
        _open = false;
        Update();
        if (wasOpen && restoreFocus)
            _toggle.Focus();
    }

    void Keys(UiKeyboardEvent e)
    {
        if (!_compact.Matches || !_open || e.DefaultPrevented || _windows.Top is not null)
            return;
        if (e.Key == "Escape")
        {
            e.PreventDefault();
            Close();
            return;
        }
        if (e.Key != "Tab")
            return;
        List<UiElement> controls = [.. Focusable(), _toggle];
        var (first, last) = (controls[0], controls[^1]);
        if (e.ShiftKey && _document.FocusedElement == first)
        {
            e.PreventDefault();
            last.Focus();
        }
        else if (!e.ShiftKey && _document.FocusedElement == last)
        {
            e.PreventDefault();
            first.Focus();
        }
    }
}
