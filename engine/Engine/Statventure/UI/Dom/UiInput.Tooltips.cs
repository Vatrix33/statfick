using System.Globalization;
using System.Numerics;
using Statventure.Text;

namespace Statventure.UI.Dom;

/// <summary>
/// Title tooltips as Chromium 152 shows them, in its two halves. The page's side (Blink's
/// ChromeClient::UpdateTooltipUnderCursor) finds the text under the pointer after each move — the
/// nearest title (an SVG element's first &lt;title&gt; child), else an invalid control's validation
/// message — and passes it on when the point or the text changed. The window's side (views'
/// TooltipController and TooltipStateManager) shows it 500 ms after the pointer came to rest, at the
/// pointer (10 px right, 15 px down, kept in the window, above the pointer when there's no room
/// below), and keeps it until the text changes; a press hides it until the text under the pointer
/// changes, a key press until the next text change, the wheel and leaving the window hide it.
/// The tooltip is an &lt;sv-tooltip&gt; element in the top layer (game-styled; a plain look by default).
/// </summary>
public sealed partial class UiInput
{
    /// <summary>How long the pointer rests before a tooltip shows (views' kDefaultShowTooltipDelay).</summary>
    public const double TooltipShowDelay = 0.5;
    /// <summary>Where a tooltip goes from the pointer (TooltipAura's kCursorOffsetX and Y).</summary>
    public const int TooltipOffsetX = 10, TooltipOffsetY = 15;

    // The page's side (ChromeClient): what it passed on last.
    Vector2? _lastTipPoint;
    string _lastTipText = "";
    UiNode? _lastTipNode;
    bool _didRequestNonEmptyTip;

    // The window's side: the text the page set, whether the pointer is over the window, and the
    // tooltip state (TooltipStateManager): its text, whether one is pending or shown, the timer, the place.
    string _windowTip = "";
    bool _tipObserved;
    string _tipText = "";
    bool _tipActive;
    double? _tipShowAt;
    string _tipTrimmed = "";
    Vector2 _tipPosition;
    Vector2 _lastMouseLocation;
    UiElement? _tooltip;
    // A press hides the tooltip until the text under the pointer differs from the one pressed on.
    bool _tipPressed;
    string _tipTextAtPress = "";

    /// <summary>The tooltip shown now (an &lt;sv-tooltip&gt; in the top layer), and the point it is anchored to.</summary>
    public UiElement? Tooltip => _tooltip is { Parent: not null } ? _tooltip : null;
    internal Vector2 TooltipAnchor => _tipPosition;

    bool TooltipPending => _tipShowAt is not null;

    // ---------- The window's side ----------

    /// <summary>A real mouse move reached the window (before the page sees it): TooltipController::OnMouseEvent.</summary>
    void TooltipPointerMoved(Vector2 position)
    {
        _lastMouseLocation = position;
        // No tooltips while a button is held.
        if (TooltipPending)
            _tipPosition = position;
        SetTooltipObserved(_buttons == 0);
        if (Tooltip is not null || (_tipObserved && _windowTip != _tipText))
            UpdateTooltipIfRequired();
    }

    /// <summary>A press: the tooltip goes, and stays away while the text under the pointer is the one pressed on.</summary>
    void TooltipPointerPressed()
    {
        _tipPressed = true;
        _tipTextAtPress = _windowTip;
        HideTooltipState();
    }

    /// <summary>The wheel turned: a shown tooltip goes.</summary>
    void TooltipWheel()
    {
        if (Tooltip is not null)
            HideTooltipState();
    }

    /// <summary>A key went down, or the window lost focus: the tooltip goes, and the window stops being watched.</summary>
    void HideTooltip()
    {
        HideTooltipState();
        SetTooltipObserved(false);
    }

    void SetTooltipObserved(bool observed)
    {
        if (_tipObserved == observed)
            return;
        if (!observed && _tipActive)
            HideTooltipState();
        _tipObserved = observed;
    }

    /// <summary>The page set the text under the pointer (RenderWidgetHostViewAura::UpdateTooltip, TooltipController::UpdateTooltip).</summary>
    void SetWindowTooltip(string text)
    {
        _windowTip = text;
        if (_tipObserved && (_tipActive || _windowTip != _tipText))
            UpdateTooltipIfRequired();
        if (_tipPressed && _windowTip != _tipTextAtPress)
            _tipPressed = false;
    }

    void UpdateTooltipIfRequired()
    {
        if (_buttons != 0 || (_windowTip.Length > 0 && _tipPressed && _windowTip == _tipTextAtPress))
        {
            HideTooltipState();
            return;
        }
        _tipPressed = false;
        if (Tooltip is null || _windowTip != _tipText)
            ShowTooltipLater(_windowTip, _lastMouseLocation);
    }

    /// <summary>TooltipStateManager::Show: the text (cut at 2048 characters, trimmed) shows after the delay, unless it is blank.</summary>
    void ShowTooltipLater(string text, Vector2 position)
    {
        HideTooltipState();
        _tipPosition = position;
        _tipText = text;
        _tipActive = true;
        var trimmed = Truncate(text, 2048).Trim();
        if (trimmed.Length == 0)
            return;
        _tipTrimmed = trimmed;
        _tipShowAt = Now + TooltipShowDelay;
    }

    void HideTooltipState()
    {
        _tooltip?.Remove();
        _tooltip = null;
        _tipShowAt = null;
        _tipActive = false;
    }

    /// <summary>The show timer fired: the tooltip element comes (the document places it once it is laid out).</summary>
    void AdvanceTooltip(double time)
    {
        if (_tipShowAt is not { } at || time < at)
            return;
        _tipShowAt = null;
        if (!_tipActive)
            return;
        var tooltip = new UiElement("sv-tooltip");
        // Tabs show as eight spaces (TooltipViewAura).
        tooltip.AppendText(_tipTrimmed.Replace("\t", "        "));
        _tooltip = tooltip;
        _document.TopLayer.InsertBefore(tooltip, null);
        _document.PlaceTooltip(tooltip, _tipPosition);
    }

    /// <summary>gfx::TruncateString with word breaks: at most <paramref name="length"/> characters, ending in an ellipsis.</summary>
    static string Truncate(string text, int length)
    {
        if (text.Length <= length)
            return text;
        const string Ellipsis = "…";
        var index = length - 1;
        var boundaries = WordBreaker.Boundaries(text);
        var preceding = boundaries.LastOrDefault(b => b < length, -1);
        if (preceding > 0)
            index = preceding;
        static bool Blank(char c) =>
            char.IsWhiteSpace(c) || char.GetUnicodeCategory(c) is UnicodeCategory.Control or UnicodeCategory.NonSpacingMark;
        for (var i = index - 1; i >= 0; i--)
            if (!Blank(text[i]))
                return text[..(i + 1)] + Ellipsis;
        return index != length - 1 ? text[..(length - 1)] + Ellipsis : Ellipsis;
    }

    // ---------- The page's side ----------

    /// <summary>
    /// The page handled a move (or a fake one after a change under the pointer): ChromeClient's
    /// UpdateTooltipUnderCursor — the text under the pointer, passed on when the point or the text
    /// changed (cleared first when the same text comes from another node; nothing over a scrollbar).
    /// </summary>
    void PageTooltipUnderCursor(Vector2 position, HitResult? hit)
    {
        if (_document.OverViewportScrollbars(position) || (hit is { Box.IsScrollContainer: true } && _document.ScrollbarAt(position) is not null))
        {
            ClearPageTooltip();
            return;
        }
        UiNode? node = hit?.Box.TextNode ?? (UiNode?)hit?.Element;
        var text = TitleOf(hit?.Element) ?? DefaultToolTip(hit?.Element) ?? "";
        if (_lastTipPoint == position && _lastTipText == text)
            return;
        if (node != _lastTipNode && _lastTipText.Length > 0 && text == _lastTipText)
            ClearPageTooltip();
        _lastTipPoint = position;
        _lastTipText = text;
        _lastTipNode = node;
        SendPageTooltip(text);
    }

    /// <summary>ChromeClient::ClearToolTip (what it passed on last stays, so the same text isn't sent again).</summary>
    void ClearPageTooltip() => SendPageTooltip("");

    /// <summary>ChromeClientImpl: a text is passed on; an empty one only once after a text.</summary>
    void SendPageTooltip(string text)
    {
        if (text.Length > 0)
        {
            SetWindowTooltip(text);
            _didRequestNonEmptyTip = true;
        }
        else if (_didRequestNonEmptyTip)
        {
            SetWindowTooltip(text);
            _didRequestNonEmptyTip = false;
        }
    }

    /// <summary>
    /// HitTestResult::Title: the nearest title — an HTML element's title attribute (an empty one
    /// says none), an SVG element's first &lt;title&gt; child. An open select list is a page of its own.
    /// </summary>
    static string? TitleOf(UiElement? element)
    {
        for (var e = element; e is not null; e = e.Parent)
        {
            if (IsSvgElement(e))
            {
                if (e.ElementChildren.FirstOrDefault(c => c.Tag == "title") is { } title)
                    return title.TextContent;
            }
            else if (e.GetAttribute("title") is { } text)
                return text;
            if (e.Tag == "sv-select-popup")
                break;
        }
        return null;
    }

    static bool IsSvgElement(UiElement element)
    {
        for (var e = element; e is not null; e = e.Parent)
            if (e.Tag == "svg")
                return true;
        return false;
    }

    /// <summary>Element::DefaultToolTip: an invalid input, textarea or select shows its validation message (unless its form doesn't validate).</summary>
    static string? DefaultToolTip(UiElement? element)
    {
        if (element is null || element.Tag is not ("input" or "textarea" or "select"))
            return null;
        if (element.Form?.HasAttribute("novalidate") == true || (element.Tag == "textarea" && element.HasAttribute("novalidate")))
            return null;
        return element.ToolTipValidationMessage;
    }
}
