using Statventure.Core;
using Statventure.UI.Layout;

namespace Statventure.UI.Paint;

/// <summary>How a scrollbar part is drawn: Chromium's NativeTheme states.</summary>
public enum ScrollbarPartState
{
    Disabled,
    Hovered,
    Normal,
    Pressed,
}

/// <summary>
/// The colours and arrow shapes of Chromium 152's Fluent scrollbars in the light colour scheme
/// (ui/native_theme/native_theme_fluent.cc, ui/color/fluent_ui_color_mixer.cc): a #FCFCFC track,
/// a #8B8B8B thumb and arrows that darken to #636363 when hovered or pressed. With scrollbar-color,
/// the thumb and arrows take the thumb colour and the track and buttons the track colour; hovered
/// and pressed parts are then blended for contrast as Chromium does (color_utils::BlendForMinContrast).
/// </summary>
public static class ScrollbarTheme
{
    public static readonly Color Track = Color.FromBytes(0xFC, 0xFC, 0xFC);
    public static readonly Color Thumb = Color.FromBytes(0x8B, 0x8B, 0x8B);
    public static readonly Color ThumbActive = Color.FromBytes(0x63, 0x63, 0x63);

    /// <summary>Fluent's contrast ratio for hovered and pressed parts, and Chromium's minimum visible contrast.</summary>
    const float PartContrastRatio = 1.8f, MinimumVisibleContrast = 3f;

    public static Color TrackColor(Color? custom) => custom ?? Track;

    public static Color CornerColor(Color? customTrack) => customTrack ?? Track;

    public static Color ButtonBackground(Color? customTrack) => customTrack ?? Track;

    public static Color ThumbColor(Color? customThumb, Color? customTrack, ScrollbarPartState state)
    {
        var background = customTrack ?? Track;
        if (Contrasting(customThumb, background, state) is { } color)
            return color;
        return state is ScrollbarPartState.Hovered or ScrollbarPartState.Pressed ? ThumbActive : Thumb;
    }

    public static Color ArrowColor(Color? customThumb, Color? customTrack, ScrollbarPartState state)
    {
        if (Contrasting(customThumb, ButtonBackground(customTrack), state) is { } color)
            return color;
        return state is ScrollbarPartState.Hovered or ScrollbarPartState.Pressed ? ThumbActive : Thumb;
    }

    /// <summary>NativeThemeBase::GetContrastingColorForScrollbarPart.</summary>
    static Color? Contrasting(Color? foreground, Color? background, ScrollbarPartState state)
    {
        if (foreground is not { } fg || state is not (ScrollbarPartState.Hovered or ScrollbarPartState.Pressed) || ToBytes(fg).A == 0)
            return foreground;
        var color = BlendForMinContrast(ToBytes(fg), ToBytes(fg) with { A = 255 }, PartContrastRatio);
        if (background is { } bg)
            color = BlendForMinContrast(color, ToBytes(bg) with { A = 255 }, MinimumVisibleContrast);
        return Color.FromBytes(color.R, color.G, color.B, color.A);
    }

    readonly record struct Rgba(byte R, byte G, byte B, byte A);

    static Rgba ToBytes(Color c)
    {
        static byte B(float v) => (byte)Math.Clamp((int)MathF.Round(v * 255f, MidpointRounding.AwayFromZero), 0, 255);
        return new Rgba(B(c.R), B(c.G), B(c.B), B(c.A));
    }

    static readonly Rgba White = new(255, 255, 255, 255);
    /// <summary>gfx::kGoogleGrey900, color_utils' darkest colour.</summary>
    static readonly Rgba Darkest = new(0x20, 0x21, 0x24, 255);
    const float LuminanceMidpoint = 0.211692036f;

    static float Linearize(float c) => c <= 0.04045f ? c / 12.92f : MathF.Pow((c + 0.055f) / 1.055f, 2.4f);

    static float Luminance(Rgba c) => 0.2126f * Linearize(c.R / 255f) + 0.7152f * Linearize(c.G / 255f) + 0.0722f * Linearize(c.B / 255f);

    static float ContrastRatio(float a, float b)
    {
        a += 0.05f;
        b += 0.05f;
        return a > b ? a / b : b / a;
    }

    /// <summary>color_utils::AlphaBlend with an alpha of 0..255.</summary>
    static Rgba AlphaBlend(Rgba foreground, Rgba background, int alpha)
    {
        if (alpha == 0)
            return background;
        if (alpha == 255)
            return foreground;
        var a = alpha / 255f;
        var normalizer = foreground.A * a + background.A * (1 - a);
        if (normalizer == 0)
            return default;
        var fw = foreground.A * a / normalizer;
        var bw = background.A * (1 - a) / normalizer;
        static byte Round(float v) => (byte)Math.Clamp((int)MathF.Round(v, MidpointRounding.AwayFromZero), 0, 255);
        return new Rgba(Round(foreground.R * fw + background.R * bw), Round(foreground.G * fw + background.G * bw), Round(foreground.B * fw + background.B * bw), Round(normalizer));
    }

    static Rgba ResultingPaintColor(Rgba foreground, Rgba background) => AlphaBlend(foreground with { A = 255 }, background, foreground.A);

    static Rgba BlendForMinContrast(Rgba foreground, Rgba background, float ratio)
    {
        foreground = ResultingPaintColor(foreground, background);
        var backgroundLuminance = Luminance(background);
        if (ContrastRatio(Luminance(foreground), backgroundLuminance) >= ratio)
            return foreground;
        var target = ResultingPaintColor(backgroundLuminance < LuminanceMidpoint ? White : Darkest, background);
        var best = target;
        for (int low = 0, high = 256; low < high;)
        {
            var alpha = low + (high - low) / 2;
            var color = AlphaBlend(target, foreground, alpha);
            if (ContrastRatio(Luminance(color), backgroundLuminance) >= ratio)
            {
                best = color;
                high = alpha;
            }
            else
                low = alpha + 1;
        }
        return best;
    }

    // ---------- Arrows ----------

    /// <summary>
    /// The arrow of a button as Chromium draws it without the Segoe Fluent Icons font: a triangle,
    /// filled without antialiasing, as the pixel rows it covers (x, y, width; one pixel tall).
    /// </summary>
    public static List<RectF> ArrowPixels(RectF button, ScrollbarPart part, bool vertical, ScrollbarPartState state)
    {
        // NativeThemeFluent::GetArrowRect: a 9 px square (7 px pressed) scaled with the button,
        // centred, on whole pixels, shifted a pixel towards the bar's end.
        var unscaled = state == ScrollbarPartState.Pressed ? 7 : 9;
        var minSide = (int)MathF.Min(button.Width, button.Height);
        var maxSide = (int)MathF.Max(button.Width, button.Height);
        var scale = maxSide / (float)ScrollbarGeometry.ButtonLength;
        var side = (int)MathF.Ceiling(unscaled * scale);
        side += (minSide - side) % 2;
        var w = MathF.Min(button.Width, side);
        var h = MathF.Min(button.Height, side);
        var x = MathF.Floor(button.X + (button.Width - w) / 2);
        var y = MathF.Floor(button.Y + (button.Height - h) / 2);
        var back = part == ScrollbarPart.BackButton;
        var offset = MathF.Round((back ? -1 : 1) * scale, MidpointRounding.AwayFromZero);
        if (vertical)
            y += offset;
        else
            x += offset;

        // NativeThemeBase::PathForArrow: an up or right pointing triangle, mirrored for down and left.
        Span<(float X, float Y)> points = stackalloc (float, float)[3];
        var cx = x + w / 2;
        var cy = y + h / 2;
        if (vertical)
        {
            var height = (int)MathF.Round(h, MidpointRounding.AwayFromZero) / 2 + 1;
            var baseY = y + h - height / 2 + 1;
            points[0] = (x, baseY);
            points[1] = (x + w, baseY);
            points[2] = (x + w / 2, baseY - height);
            if (!back)
                for (var i = 0; i < 3; i++)
                    points[i].Y = 2 * cy - points[i].Y;
        }
        else
        {
            var width = (int)MathF.Round(w, MidpointRounding.AwayFromZero) / 2 + 1;
            var baseX = x + width / 2;
            points[0] = (baseX, y);
            points[1] = (baseX, y + h);
            points[2] = (baseX + width, y + h / 2);
            if (back)
                for (var i = 0; i < 3; i++)
                    points[i].X = 2 * cx - points[i].X;
        }
        return Rasterize(points);
    }

    /// <summary>
    /// Fills a polygon without antialiasing, as Skia does: a pixel is filled when its centre is
    /// inside (rows and spans half-open). Returns the filled spans.
    /// </summary>
    internal static List<RectF> Rasterize(ReadOnlySpan<(float X, float Y)> points)
    {
        var spans = new List<RectF>();
        float top = float.MaxValue, bottom = float.MinValue;
        foreach (var p in points)
        {
            top = MathF.Min(top, p.Y);
            bottom = MathF.Max(bottom, p.Y);
        }
        Span<float> xs = stackalloc float[points.Length];
        for (var row = (int)MathF.Floor(top); row < (int)MathF.Ceiling(bottom); row++)
        {
            var cy = row + 0.5f;
            var count = 0;
            for (var i = 0; i < points.Length; i++)
            {
                var (x1, y1) = points[i];
                var (x2, y2) = points[(i + 1) % points.Length];
                if (y1 == y2)
                    continue;
                if (MathF.Min(y1, y2) <= cy && cy < MathF.Max(y1, y2))
                    xs[count++] = x1 + (cy - y1) * (x2 - x1) / (y2 - y1);
            }
            xs[..count].Sort();
            for (var i = 0; i + 1 < count; i += 2)
            {
                // Pixels whose centres are in [left, right).
                var first = (int)MathF.Ceiling(xs[i] - 0.5f);
                var end = (int)MathF.Ceiling(xs[i + 1] - 0.5f);
                if (end > first)
                    spans.Add(new RectF(first, row, end - first, 1));
            }
        }
        return spans;
    }
}
