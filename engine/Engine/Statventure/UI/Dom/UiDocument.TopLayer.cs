using System.Numerics;
using Statventure.Core;

namespace Statventure.UI.Dom;

/// <summary>
/// The top layer: elements the engine shows above the page (the validation bubble, the select
/// list, tooltips). They are ordinary elements under &lt;sv-top-layer&gt;, the last child of the
/// root, so the game's stylesheet styles them (sv-validation-bubble, sv-validation-main,
/// sv-validation-sub, sv-select-popup, sv-tooltip); the user-agent style gives them a plain look.
/// </summary>
public sealed partial class UiDocument
{
    UiElement? _topLayer;
    UiElement? _bubble, _bubbleAnchor;
    double _bubbleUntil;

    /// <summary>The container of the top layer (made when first needed).</summary>
    public UiElement TopLayer
    {
        get
        {
            if (_topLayer is null || _topLayer.Parent != Root)
            {
                _topLayer = new UiElement("sv-top-layer");
                Root.InsertBefore(_topLayer, null);
            }
            return _topLayer;
        }
    }

    /// <summary>The validation bubble shown now, and the control it points at.</summary>
    public UiElement? ValidationBubble => _bubble is { Parent: not null } ? _bubble : null;
    public UiElement? ValidationBubbleAnchor => ValidationBubble is null ? null : _bubbleAnchor;

    /// <summary>
    /// An invalid control is reported (reportValidity, a blocked submit): it is scrolled into view
    /// and focused, and Chromium's message shows in the validation bubble under it — for 5 seconds
    /// plus 50 ms per character, or until the control changes, loses focus or the user acts.
    /// </summary>
    internal void ReportInvalid(UiElement control)
    {
        control.ScrollIntoView(ScrollAlign.Nearest, ScrollAlign.Nearest);
        Input.Focus(control, FocusOrigin.Script);
        var message = control.ValidationMessage;
        if (message.Length == 0)
            return;
        HideValidationBubble();
        var parts = message.Split('\n', 2);
        var bubble = new UiElement("sv-validation-bubble");
        bubble.Append("sv-validation-main", text: parts[0]);
        if (parts.Length > 1 && parts[1].Length > 0)
            bubble.Append("sv-validation-sub", text: parts[1]);
        TopLayer.InsertBefore(bubble, null);
        _bubble = bubble;
        _bubbleAnchor = control;
        _bubbleUntil = Animations.Time + 5 + 0.05 * message.Length;
        control.ValueChanged += HideBubbleFor;
        PlaceValidationBubble();
    }

    void HideBubbleFor(UiElement control)
    {
        if (control == _bubbleAnchor)
            HideValidationBubble();
    }

    /// <summary>Takes the validation bubble away.</summary>
    public void HideValidationBubble()
    {
        if (_bubbleAnchor is { } anchor)
            anchor.ValueChanged -= HideBubbleFor;
        _bubble?.Remove();
        _bubble = null;
        _bubbleAnchor = null;
    }

    /// <summary>
    /// Places a select's list (Chromium's list picker window): at least as wide as the select, at
    /// most 20 rows tall (it scrolls beyond), under the select, or above it when it doesn't fit
    /// below but does above; when it fits neither, on the roomier side, shortened to fit.
    /// </summary>
    internal void PlaceSelectPopup(UiElement select, UiElement popup)
    {
        if (select.Box is null)
            return;
        // Placed on whole device pixels; styles take CSS pixels.
        string Px(float value) => CssPx(MathF.Round(value));
        void Set(string property, string value)
        {
            if (popup.GetStyle(property) != value)
                popup.SetStyle(property, value);
        }
        var anchor = LayoutBoundingClientRect(select);
        Set("min-width", Px(anchor.Width));
        if (popup.Box is not { } box)
        {
            Set("left", Px(anchor.Left));
            Set("top", Px(anchor.Bottom));
            return;
        }
        // Twenty rows' height (the list picker is a list box of size 20).
        var rows = popup.Descendants().Where(e => e.Tag is "sv-select-option" or "sv-select-group-label" && e.Box is not null).Take(20).ToList();
        var frame = box.Border.Vertical + box.Padding.Vertical;
        var height = rows.Count == 0 ? box.ViewportRect.Height : rows.Sum(r => r.Box!.ViewportRect.Height) + frame;
        var natural = height;
        var below = _viewport.Y - anchor.Bottom;
        var above = anchor.Top;
        float top;
        if (height <= below)
            top = anchor.Bottom;
        else if (height <= above)
            top = anchor.Top - height;
        else if (below >= above)
        {
            height = below;
            top = anchor.Bottom;
        }
        else
        {
            height = above;
            top = 0;
        }
        Set("max-height", Px(MathF.Min(natural, height)));
        var width = box.ViewportRect.Width;
        var left = anchor.Left + width > _viewport.X ? MathF.Max(0, _viewport.X - width) : anchor.Left;
        Set("left", Px(left));
        Set("top", Px(top));
    }

    /// <summary>A length in layout units (device pixels) as a CSS px value for a style.</summary>
    string CssPx(float layout) => (layout / Zoom).ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "px";

    /// <summary>
    /// Places a tooltip as views' TooltipAura does, in window units (DIPs), with the window for the
    /// screen: its text wraps at min(800, (width + 1) / 2); it sits 10 right of and 15 below the
    /// pointer, shifted left to stay in the window, above the pointer when it doesn't fit below,
    /// and kept inside.
    /// </summary>
    internal void PlaceTooltip(UiElement tooltip, Vector2 anchor)
    {
        var scale = DeviceScale;
        string Px(float dip) => CssPx(dip * scale);
        void Set(string property, string value)
        {
            if (tooltip.GetStyle(property) != value)
                tooltip.SetStyle(property, value);
        }
        var viewport = _viewport / scale;
        Set("max-width", Px(MathF.Min(800, MathF.Floor((viewport.X + 1) / 2))));
        var pointer = new Vector2(MathF.Round(anchor.X / scale), MathF.Round(anchor.Y / scale));
        var x = pointer.X + UiInput.TooltipOffsetX;
        var y = pointer.Y + UiInput.TooltipOffsetY;
        if (tooltip.Box is { } box)
        {
            var width = MathF.Ceiling(box.ViewportRect.Width / scale);
            var height = MathF.Ceiling(box.ViewportRect.Height / scale);
            if (x + width > viewport.X)
                x -= x + width - viewport.X;
            if (y + height > viewport.Y)
                y = pointer.Y - height;
            x = MathF.Max(0, MathF.Min(x, viewport.X - width));
            y = MathF.Max(0, MathF.Min(y, viewport.Y - height));
        }
        Set("left", Px(x));
        Set("top", Px(y));
    }

    /// <summary>Keeps the bubble under its control (below the border box, at its start), and ends it when its time is up.</summary>
    void PlaceValidationBubble()
    {
        if (_bubble is null || _bubbleAnchor is not { } anchor)
            return;
        if (anchor.Document != this || Animations.Time >= _bubbleUntil || FocusedElement != anchor)
        {
            HideValidationBubble();
            return;
        }
        var rect = anchor.Box is null ? default : LayoutBoundingClientRect(anchor);
        var left = CssPx(MathF.Round(rect.Left));
        var top = CssPx(MathF.Round(rect.Bottom));
        if (_bubble.GetStyle("left") != left)
            _bubble.SetStyle("left", left);
        if (_bubble.GetStyle("top") != top)
            _bubble.SetStyle("top", top);
    }
}
