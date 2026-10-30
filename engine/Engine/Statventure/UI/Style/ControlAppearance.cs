using Statventure.UI.Dom;

namespace Statventure.UI.Style;

/// <summary>The native looks a control can have (Blink's AppearanceValue, for the controls the engine has).</summary>
public enum ControlLook
{
    None,
    Checkbox,
    Radio,
    PushButton,
    Button,
    TextField,
    SearchField,
    TextArea,
    Menulist,
    /// <summary>A select whose author styled its background or border: CSS paints it, the theme only its arrow.</summary>
    MenulistButton,
    Listbox,
    ProgressBar,
    Meter,
    SliderHorizontal,
}

/// <summary>
/// The look a control gets (Blink's LayoutTheme::AdjustStyle): appearance auto (or a keyword
/// valid for the element) gives the element's native look, unless the author styled its
/// background or border (a select then keeps only its arrow); appearance none gives none.
/// </summary>
public static class ControlAppearance
{
    /// <summary>The look the element would have with appearance: auto.</summary>
    public static ControlLook AutoLook(UiElement element) => element.Tag switch
    {
        "button" => ControlLook.Button,
        "meter" => ControlLook.Meter,
        "progress" => ControlLook.ProgressBar,
        "textarea" => ControlLook.TextArea,
        "select" => element.HasAttribute("multiple") || (int.TryParse(element.GetAttribute("size"), out var size) && size > 1) ? ControlLook.Listbox : ControlLook.Menulist,
        "input" => element.InputType switch
        {
            "checkbox" => ControlLook.Checkbox,
            "radio" => ControlLook.Radio,
            "submit" or "reset" or "button" => ControlLook.PushButton,
            "range" => ControlLook.SliderHorizontal,
            "search" => ControlLook.SearchField,
            "text" or "tel" or "url" or "email" or "password" or "number" or "date" or "month" or "week" or "time" or "datetime-local" => ControlLook.TextField,
            _ => ControlLook.None,
        },
        _ => ControlLook.None,
    };

    /// <summary>The element's look with its style (none for elements without a native look).</summary>
    public static ControlLook Effective(UiElement element, ComputedStyle style)
    {
        var appearance = style.Appearance;
        if (appearance == "none")
            return ControlLook.None;
        var auto = AutoLook(element);
        if (auto == ControlLook.None)
            return ControlLook.None;
        // AdjustAppearanceWithElementType: the keywords that keep their meaning on this element.
        var look = appearance switch
        {
            "button" when auto == ControlLook.PushButton => ControlLook.Button,
            "menulist-button" when auto == ControlLook.Menulist => ControlLook.MenulistButton,
            "textfield" when element.Tag == "input" && element.InputType == "search" => ControlLook.TextField,
            _ => auto,
        };
        // AdjustAppearanceWithAuthorStyle (LayoutTheme::IsControlStyled).
        var styled = look switch
        {
            ControlLook.PushButton or ControlLook.Button or ControlLook.ProgressBar or ControlLook.Meter => style.HasAuthorBackground || style.HasAuthorBorder,
            ControlLook.Menulist or ControlLook.SearchField or ControlLook.TextArea or ControlLook.TextField =>
                style.HasAuthorBackground || style.HasAuthorBorder || style.BoxShadows.Length > 0,
            _ => false,
        };
        if (styled)
            return look == ControlLook.Menulist ? ControlLook.MenulistButton : ControlLook.None;
        return look;
    }
}
