using System.Globalization;
using Statventure.Core;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// An element's computed values as Chromium's getComputedStyle(element).getPropertyValue(property) prints them, for
/// the properties the stylesheet rule fixtures compare (<see cref="StylesheetRuleFixtures"/>): custom properties (a
/// registered one's computed value, an unregistered one's tokens as written, nothing for none), colours as rgb() or
/// rgba() (Blink's legacy colour serialization), a width in px, background-clip's list.
/// </summary>
public static class ComputedValues
{
    static string N(float v) => Math.Round(v, 4).ToString(CultureInfo.InvariantCulture);

    /// <summary>Color::SerializeAsCSSColor for an sRGB colour (the engine's, <see cref="PropertyRegistration.SerializeLegacy(Color)"/>).</summary>
    public static string Color(Color c) => PropertyRegistration.SerializeLegacy(c);

    public static string Of(UiElement element, string property)
    {
        var s = element.Style ?? throw new InvalidOperationException("no style for " + element.Tag);
        if (property.StartsWith("--", StringComparison.Ordinal))
            return s.Custom.TryGetValue(property, out var tokens) ? string.Concat(tokens.Select(t => t.ToString())).Trim() : "";
        return property switch
        {
            "color" => Color(s.Color),
            "background-color" => Color(s.BackgroundColor),
            "border-top-color" => Color(s.BorderTopColor ?? s.Color),
            "-webkit-text-fill-color" => Color(s.TextFillColor ?? s.Color),
            // One per layer: as many as the images, the clips repeated (Blink's FillLayer::CullEmptyLayers, FillUnsetProperties).
            "background-clip" or "-webkit-background-clip" => string.Join(", ", Enumerable.Range(0, s.BackgroundImages.Length).Select(i => Clip(s.BackgroundClips[i % s.BackgroundClips.Length]))),
            "width" => element.Box is { } box ? N(box.Width - (s.BoxSizing == BoxSizing.ContentBox ? box.Border.Horizontal + box.Padding.Horizontal : 0)) + "px" : "auto",
            _ => throw new ArgumentException("no serialization for " + property),
        };
    }

    static string Clip(BackgroundClip clip) => clip switch
    {
        BackgroundClip.PaddingBox => "padding-box",
        BackgroundClip.ContentBox => "content-box",
        BackgroundClip.Text => "text",
        _ => "border-box",
    };
}
