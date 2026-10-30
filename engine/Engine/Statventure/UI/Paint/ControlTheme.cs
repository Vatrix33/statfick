using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.UI.Layout;
using CanvasPath = Statventure.Canvas.Path;

namespace Statventure.UI.Paint;

/// <summary>A control's state as the native theme draws it (Blink's GetWebThemeState).</summary>
public enum ThemeState
{
    Disabled,
    Hovered,
    Normal,
    Pressed,
}

/// <summary>
/// Chromium 152's native form controls (ui/native_theme/native_theme_base.cc and the web control
/// colours of ui/color/color_provider_utils.cc), light and dark: checkboxes, radios, buttons, text
/// fields, menu lists, sliders, progress bars and spin buttons. Rectangles are whole pixels.
/// </summary>
internal static class ControlTheme
{
    const float BorderWidth = 1, SliderTrackThickness = 8, SliderThumbThickness = 16, CheckboxSize = 13;

    sealed record Palette(
        Color[] Accent, Color[] Border, Color[] CheckboxBackground, Color[] Fill, Color[] Slider, Color[] SliderBorder,
        Color[] ButtonFill, Color[] ButtonBorder, Color LightenLayer);

    // Indexed by ThemeState: disabled, hovered, normal, pressed.
    static readonly Palette Light = new(
        Accent: [Argb(0x4D, 0x76, 0x76, 0x76), Rgb(0x00, 0x5C, 0xC8), Rgb(0x00, 0x75, 0xFF), Rgb(0x37, 0x93, 0xFF)],
        Border: [Argb(0x4D, 0x76, 0x76, 0x76), Rgb(0x4F, 0x4F, 0x4F), Rgb(0x76, 0x76, 0x76), Rgb(0x8D, 0x8D, 0x8D)],
        CheckboxBackground: [Argb(0x99, 0xFF, 0xFF, 0xFF), Rgb(0xFF, 0xFF, 0xFF), Rgb(0xFF, 0xFF, 0xFF), Rgb(0xFF, 0xFF, 0xFF)],
        Fill: [Argb(0x4D, 0xEF, 0xEF, 0xEF), Rgb(0xE5, 0xE5, 0xE5), Rgb(0xEF, 0xEF, 0xEF), Rgb(0xF5, 0xF5, 0xF5)],
        Slider: [Rgb(0xCB, 0xCB, 0xCB), Rgb(0x00, 0x5C, 0xC8), Rgb(0x00, 0x75, 0xFF), Rgb(0x37, 0x93, 0xFF)],
        SliderBorder: [Argb(0x4D, 0x76, 0x76, 0x76), Argb(0x80, 0x4F, 0x4F, 0x4F), Argb(0x80, 0x76, 0x76, 0x76), Argb(0x80, 0x8D, 0x8D, 0x8D)],
        ButtonFill: [Argb(0x4D, 0xEF, 0xEF, 0xEF), Rgb(0xE5, 0xE5, 0xE5), Rgb(0xEF, 0xEF, 0xEF), Rgb(0xF5, 0xF5, 0xF5)],
        ButtonBorder: [Argb(0x4D, 0x76, 0x76, 0x76), Rgb(0x4F, 0x4F, 0x4F), Rgb(0x76, 0x76, 0x76), Rgb(0x8D, 0x8D, 0x8D)],
        LightenLayer: Argb(0x33, 0xA9, 0xA9, 0xA9));

    static readonly Palette Dark = new(
        Accent: [Rgb(0x75, 0x75, 0x75), Rgb(0xD1, 0xE6, 0xFF), Rgb(0x99, 0xC8, 0xFF), Rgb(0x61, 0xA9, 0xFF)],
        Border: [Rgb(0x62, 0x62, 0x62), Rgb(0xAC, 0xAC, 0xAC), Rgb(0x85, 0x85, 0x85), Rgb(0x6E, 0x6E, 0x6E)],
        CheckboxBackground: [Rgb(0x3B, 0x3B, 0x3B), Rgb(0x3B, 0x3B, 0x3B), Rgb(0x3B, 0x3B, 0x3B), Rgb(0x3B, 0x3B, 0x3B)],
        Fill: [Rgb(0x36, 0x36, 0x36), Rgb(0x3B, 0x3B, 0x3B), Rgb(0x3B, 0x3B, 0x3B), Rgb(0x3B, 0x3B, 0x3B)],
        Slider: [Rgb(0x75, 0x75, 0x75), Rgb(0xD1, 0xE6, 0xFF), Rgb(0x99, 0xC8, 0xFF), Rgb(0x61, 0xA9, 0xFF)],
        SliderBorder: [Rgb(0x62, 0x62, 0x62), Rgb(0xAC, 0xAC, 0xAC), Rgb(0x85, 0x85, 0x85), Rgb(0x6E, 0x6E, 0x6E)],
        ButtonFill: [Rgb(0x36, 0x36, 0x36), Rgb(0x7B, 0x7B, 0x7B), Rgb(0x6B, 0x6B, 0x6B), Rgb(0x61, 0x61, 0x61)],
        ButtonBorder: [Rgb(0x36, 0x36, 0x36), Rgb(0x7B, 0x7B, 0x7B), Rgb(0x6B, 0x6B, 0x6B), Rgb(0x61, 0x61, 0x61)],
        LightenLayer: Rgb(0x3B, 0x3B, 0x3B));

    static Color Rgb(byte r, byte g, byte b) => Color.FromBytes(r, g, b);
    static Color Argb(byte a, byte r, byte g, byte b) => Color.FromBytes(r, g, b, a);

    /// <summary>The accent colour for a state: as given when normal, lighter or darker when hovered or pressed (HSL lightness ±0.11), the theme's when disabled or not given.</summary>
    static Color AccentOrControl(Color? accent, Color[] colors, ThemeState state, bool dark)
    {
        if (state == ThemeState.Disabled || accent is not { } color)
            return colors[(int)state];
        if (state == ThemeState.Normal)
            return color;
        var adjust = state == ThemeState.Pressed ? 0.11 : -0.11;
        if (dark)
            adjust = -adjust;
        static byte B(float v) => (byte)Math.Clamp((int)MathF.Round(v * 255, MidpointRounding.AwayFromZero), 0, 255);
        var (h, s, l) = ToHsl(B(color.R), B(color.G), B(color.B));
        l = Math.Clamp(l + adjust, 0, 1);
        return FromHsl(h, s, l, color.A);
    }

    /// <summary>color_utils::SkColorToHSL.</summary>
    static (double H, double S, double L) ToHsl(byte rb, byte gb, byte bb)
    {
        float r = rb / 255f, g = gb / 255f, b = bb / 255f;
        var vmin = MathF.Min(r, MathF.Min(g, b));
        var vmax = MathF.Max(r, MathF.Max(g, b));
        var delta = vmax - vmin;
        var l = (vmin + vmax) / 2;
        if (rb == gb && rb == bb)
            return (0, 0, l);
        var dr = ((vmax - r) / 6f + delta / 2f) / delta;
        var dg = ((vmax - g) / 6f + delta / 2f) / delta;
        var db = ((vmax - b) / 6f + delta / 2f) / delta;
        float h;
        if (r >= g && r >= b)
            h = db - dg;
        else if (g >= r && g >= b)
            h = 1f / 3f + dr - db;
        else
            h = 2f / 3f + dg - dr;
        if (h < 0)
            h++;
        else if (h > 1)
            h--;
        var s = delta / (l < 0.5f ? vmax + vmin : 2 - vmax - vmin);
        return (h, s, l);
    }

    /// <summary>color_utils::HSLToSkColor.</summary>
    static Color FromHsl(double hue, double saturation, double lightness, float alpha)
    {
        static byte Round(double v) => (byte)Math.Clamp((int)Math.Round(v, MidpointRounding.AwayFromZero), 0, 255);
        if (saturation == 0)
        {
            var light = Round(lightness * 255);
            return Color.FromBytes(light, light, light, Round(alpha * 255));
        }
        var temp2 = lightness < 0.5 ? lightness * (1 + saturation) : lightness + saturation - lightness * saturation;
        var temp1 = 2 * lightness - temp2;
        byte Hue(double h)
        {
            var t1 = (float)temp1;
            var t2 = (float)temp2;
            var hf = (float)h;
            if (hf < 0)
                hf++;
            else if (hf > 1)
                hf--;
            var result = t1;
            if (hf * 6 < 1)
                result = t1 + (t2 - t1) * hf * 6;
            else if (hf * 2 < 1)
                result = t2;
            else if (hf * 3 < 2)
                result = t1 + (t2 - t1) * (2f / 3f - hf) * 6;
            return Round(result * 255);
        }
        return Color.FromBytes(Hue(hue + 1.0 / 3.0), Hue(hue), Hue(hue - 1.0 / 3.0), Round(alpha * 255));
    }

    /// <summary>
    /// Whether an accent colour turns the control dark (Blink's CalculateColorSchemeForAccentColor):
    /// when it contrasts too little (under 3:1) with the light background and less than with the dark one.
    /// </summary>
    static bool DarkForAccent(Color? accent, Color lightBackground, Color darkBackground)
    {
        if (accent is not { } color)
            return false;
        var withLight = ContrastRatio(color, lightBackground);
        var withDark = ContrastRatio(color, darkBackground);
        return withLight < 3 && withLight < withDark;
    }

    static float Luminance(Color c)
    {
        static float L(float v)
        {
            v = MathF.Round(v * 255) / 255f;
            return v <= 0.04045f ? v / 12.92f : MathF.Pow((v + 0.055f) / 1.055f, 2.4f);
        }
        return 0.2126f * L(c.R) + 0.7152f * L(c.G) + 0.0722f * L(c.B);
    }

    static float ContrastRatio(Color a, Color b)
    {
        var la = Luminance(a) + 0.05f;
        var lb = Luminance(b) + 0.05f;
        return la > lb ? la / lb : lb / la;
    }

    static RectF Inset(RectF r, float dx, float dy) => new(r.X + dx, r.Y + dy, r.Width - 2 * dx, r.Height - 2 * dy);

    /// <summary>A round rect as Skia draws one: the radius shrinks to fit (half the shorter side at most).</summary>
    static CornerRadii Fit(RectF r, float radius) => new(MathF.Max(0, MathF.Min(radius, MathF.Min(r.Width, r.Height) / 2)));

    /// <summary>NativeTheme::AdjustBorderWidthByZoom: a stroke width times the zoom, floored, at least 1.</summary>
    internal static float BorderWidthByZoom(float width, float zoom) => MathF.Max(MathF.Floor(width * zoom), 1);

    /// <summary>A round rect stroked <paramref name="width"/> wide along <paramref name="outer"/> inset by half the width (as Skia strokes it).</summary>
    static void StrokeRoundRect(PaintTarget target, RectF outer, float radius, Color color, float width = BorderWidth)
    {
        var centerline = Inset(outer, width / 2, width / 2);
        var r = Fit(centerline, radius);
        var outerRadius = r.TopLeft.X > 0 ? r.TopLeft.X + width / 2 : 0;
        target.DrawBox(outer, null, new CornerRadii(outerRadius), new Border(width, color, BorderStyle.Solid));
    }

    static void FillRoundRect(PaintTarget target, RectF rect, float radius, Color color) =>
        target.DrawBox(rect, Brush.Solid(color), Fit(rect, radius), Border.None);

    static void LightenLayer(PaintTarget target, RectF rect, float radius, ThemeState state, Palette palette)
    {
        if (state == ThemeState.Disabled)
            FillRoundRect(target, rect, radius, palette.LightenLayer);
    }

    // ---------- Checkboxes and radios ----------

    /// <summary>NativeThemeBase::PaintCheckboxRadioCommon: the background and border of the largest square that fits.</summary>
    static RectF? CheckboxRadioCommon(PaintTarget target, RectF rect, ThemeState state, bool isCheckbox, bool isChecked, bool indeterminate, float radius, Color? accent, Palette palette, bool dark)
    {
        var side = MathF.Min(rect.Width, rect.Height);
        var square = new RectF(rect.X + (rect.Width - side) / 2, rect.Y + (rect.Height - side) / 2, side, side);
        var border = isChecked ? AccentOrControl(accent, palette.Accent, state, dark) : palette.Border[(int)state];
        if (square.Width <= 2)
        {
            target.FillRect(square, border);
            return null;
        }
        var background = Inset(square, BorderWidth * 0.2f, BorderWidth * 0.2f);
        LightenLayer(target, background, radius, state, palette);
        FillRoundRect(target, background, radius, palette.CheckboxBackground[(int)state]);
        if (!isCheckbox || (!isChecked && !indeterminate))
            StrokeRoundRect(target, square, radius, border);
        return square;
    }

    public static void PaintCheckbox(PaintTarget target, RectF rect, ThemeState state, bool isChecked, bool indeterminate, Color? accent)
    {
        var dark = isChecked && state != ThemeState.Disabled && DarkForAccent(accent, (state == ThemeState.Disabled ? Light.CheckboxBackground[0] : Light.CheckboxBackground[2]), Dark.CheckboxBackground[2]);
        var palette = dark ? Dark : Light;
        const float radius = 2;
        if (CheckboxRadioCommon(target, rect, state, true, isChecked, indeterminate, radius, accent, palette, dark) is not { } r)
            return;
        if (indeterminate || isChecked)
            FillRoundRect(target, r, radius, AccentOrControl(accent, palette.Accent, state, dark));
        var mark = palette.CheckboxBackground[(int)state];
        if (indeterminate)
        {
            const float xInset = (CheckboxSize - 8) / 2f, yInset = (CheckboxSize - 2) / 2f;
            FillRoundRect(target, Inset(r, xInset * r.Width / CheckboxSize, yInset * r.Height / CheckboxSize), radius, mark);
        }
        else if (isChecked)
        {
            var check = new CanvasPath()
                .MoveTo(new Vector2(r.X + r.Width * 0.2f, r.Y + r.Height / 2))
                .LineTo(new Vector2(r.X + r.Width * 0.4f, r.Y + r.Height / 2 + r.Height * 0.2f))
                .LineTo(new Vector2(r.Right - r.Width * 0.2f, r.Y + r.Height * 0.2f));
            target.StrokePath(check, Brush.Solid(mark), new StrokeStyle(r.Height * 0.16f, LineJoin.Miter, LineCap.Butt));
        }
    }

    public static void PaintRadio(PaintTarget target, RectF rect, ThemeState state, bool isChecked, Color? accent)
    {
        var dark = isChecked && state != ThemeState.Disabled && DarkForAccent(accent, Light.CheckboxBackground[2], Dark.CheckboxBackground[2]);
        var palette = dark ? Dark : Light;
        var radius = MathF.Max(rect.Width, rect.Height) * 0.5f;
        if (CheckboxRadioCommon(target, rect, state, false, isChecked, false, radius, accent, palette, dark) is not { } r || !isChecked)
            return;
        FillRoundRect(target, Inset(r, r.Width * 0.2f, r.Height * 0.2f), radius, AccentOrControl(accent, palette.Accent, state, dark));
    }

    // ---------- Buttons and fields ----------

    /// <summary>NativeThemeBase::PaintButton: the border width and radius zoomed (AdjustBorderWidthByZoom, AdjustBorderRadiusByZoom).</summary>
    public static void PaintButton(PaintTarget target, RectF rect, ThemeState state, float zoom = 1)
    {
        var palette = Light;
        var fill = palette.ButtonFill[(int)state];
        if (rect.Width < 5 || rect.Height < 5)
        {
            target.FillRect(rect, fill);
            return;
        }
        var border = BorderWidthByZoom(BorderWidth, zoom);
        var inner = Inset(rect, border / 2, border / 2);
        var radius = BorderWidthByZoom(2, zoom);
        LightenLayer(target, inner, radius, state, palette);
        // The fill covers the stroke's centreline rect; the stroke then covers its edge.
        FillRoundRect(target, inner, radius, fill);
        StrokeRoundRect(target, rect, radius, palette.ButtonBorder[(int)state], border);
    }

    /// <summary>
    /// NativeThemeBase::PaintTextField: a white rounded box (when the field has a background) and a
    /// 1 px border, both without antialiasing (its paint flags leave it off), so the corners are
    /// notched pixels; only the disabled lighten layer is antialiased.
    /// </summary>
    public static void PaintTextField(PaintTarget target, RectF rect, ThemeState state, Color background, bool hasBorder, float zoom = 1)
    {
        var palette = Light;
        var border = BorderWidthByZoom(BorderWidth, zoom);
        var inner = Inset(rect, border / 2, border / 2);
        var radius = BorderWidthByZoom(2, zoom);
        var scale = target.Scale;
        if (background.A > 0)
        {
            LightenLayer(target, inner, radius, state, palette);
            foreach (var span in AliasedRoundRect(inner, radius, null, scale))
                target.FillRect(span, palette.CheckboxBackground[(int)state]);
        }
        if (hasBorder)
            foreach (var span in AliasedRoundRect(Inset(inner, -border / 2, -border / 2), radius + border / 2, (Inset(inner, border / 2, border / 2), radius - border / 2), scale))
                target.FillRect(span, palette.Border[(int)state]);
    }

    /// <summary>
    /// The pixels Skia fills for a round rect without antialiasing (those whose centres are inside,
    /// spans half-open), less a hole (a stroke's inside edge), as spans one device pixel tall.
    /// </summary>
    static List<RectF> AliasedRoundRect(RectF rect, float radius, (RectF Rect, float Radius)? hole, float scale)
    {
        // The span of device pixels whose centres a round rect covers on the row centred at y.
        static (int First, int End)? Row(RectF r, float radius, float y)
        {
            if (y < r.Top || y >= r.Bottom)
                return null;
            radius = MathF.Max(0, MathF.Min(radius, MathF.Min(r.Width, r.Height) / 2));
            var dy = y < r.Top + radius ? r.Top + radius - y : y > r.Bottom - radius ? y - (r.Bottom - radius) : 0;
            var dx = dy > 0 ? radius - MathF.Sqrt(MathF.Max(0, radius * radius - dy * dy)) : 0;
            var first = (int)MathF.Ceiling(r.Left + dx - 0.5f);
            var end = (int)MathF.Ceiling(r.Right - dx - 0.5f);
            return end > first ? (first, end) : null;
        }
        RectF Device(RectF r) => new(r.X * scale, r.Y * scale, r.Width * scale, r.Height * scale);
        var outer = Device(rect);
        var inner = hole is { } h ? Device(h.Rect) : RectF.Empty;
        var innerRadius = hole is { } h2 ? h2.Radius * scale : 0;
        var spans = new List<RectF>();
        void Add(int row, int first, int end)
        {
            if (end > first)
                spans.Add(new RectF(first / scale, row / scale, (end - first) / scale, 1 / scale));
        }
        for (var row = (int)MathF.Floor(outer.Top); row < (int)MathF.Ceiling(outer.Bottom); row++)
        {
            if (Row(outer, radius * scale, row + 0.5f) is not var (first, end))
                continue;
            if (hole is not null && !inner.IsEmpty && Row(inner, innerRadius, row + 0.5f) is var (holeFirst, holeEnd))
            {
                Add(row, first, Math.Min(end, holeFirst));
                Add(row, Math.Max(first, holeEnd), end);
            }
            else
                Add(row, first, end);
        }
        return spans;
    }

    /// <summary>
    /// The menu list's arrow (NativeThemeBase::PaintMenuList): a "v" stroked 2 px wide in the text
    /// colour, its base twice its height, at <paramref name="arrowX"/> (left edge) and centred on
    /// <paramref name="arrowY"/>, shrunk to fit the rect.
    /// </summary>
    public static void PaintMenuListArrow(PaintTarget target, RectF rect, int arrowX, int arrowY, float arrowSize, Color color)
    {
        var intendedWidth = (int)arrowSize;
        var intendedHeight = (int)MathF.Floor(intendedWidth / 2f);
        var arrow = RectF.FromEdges(
            MathF.Max(rect.Left, arrowX), MathF.Max(rect.Top, arrowY - intendedHeight / 2),
            MathF.Min(rect.Right, arrowX + intendedWidth), MathF.Min(rect.Bottom, arrowY - intendedHeight / 2 + intendedHeight));
        if (arrow.Width <= 0 || arrow.Height <= 0)
            return;
        if ((int)arrow.Width != intendedWidth || (int)arrow.Height != intendedHeight)
        {
            var heightClip = (intendedHeight - (int)arrow.Height) * 2;
            var widthClip = intendedWidth - (int)arrow.Width;
            float w = arrow.Width, h = arrow.Height;
            if (heightClip > widthClip)
                w = h * 2;
            else
                h = (int)(w / 2);
            arrow = new RectF(arrowX + (int)((intendedWidth - w) / 2), arrowY - (int)(h / 2), w, h);
        }
        var path = new CanvasPath()
            .MoveTo(new Vector2(arrow.X, arrow.Y))
            .LineTo(new Vector2(arrow.X + (int)(arrow.Width / 2), arrow.Bottom))
            .LineTo(new Vector2(arrow.Right, arrow.Y));
        target.StrokePath(path, Brush.Solid(color), new StrokeStyle(2, LineJoin.Miter, LineCap.Butt));
    }

    // ---------- Sliders and progress bars ----------

    /// <summary>AlignSliderTrack: a horizontal track of a thickness centred in the rect; the value part ends past the thumb's start.</summary>
    static RectF AlignTrack(RectF rect, bool isValue, float thumbX, float thickness)
    {
        var centerY = rect.Y + rect.Height / 2;
        var half = thickness / 2;
        var right = isValue ? rect.X + thumbX + half : rect.Right;
        return RectF.FromEdges(rect.X, MathF.Max(rect.Y, centerY - half), right, MathF.Min(rect.Bottom, centerY + half));
    }

    /// <summary>NativeThemeBase::PaintSliderTrack: the track's thickness and border zoomed.</summary>
    public static void PaintSliderTrack(PaintTarget target, RectF rect, ThemeState state, float thumbX, Color? accent, float zoom = 1)
    {
        var dark = state != ThemeState.Disabled && DarkForAccent(accent, Light.Fill[2], Dark.Fill[2]);
        var palette = dark ? Dark : Light;
        var thickness = SliderTrackThickness * zoom;
        var track = AlignTrack(rect, false, 0, thickness);
        const float radius = 40;
        track = new RectF(track.X + 1, track.Y, track.Width - 2, track.Height);
        FillRoundRect(target, track, radius, palette.Fill[(int)state]);
        target.PushClip(AlignTrack(rect, true, thumbX, thickness));
        FillRoundRect(target, track, radius, AccentOrControl(accent, palette.Slider, state, dark));
        target.PopClip();
        StrokeRoundRect(target, track, radius, palette.SliderBorder[(int)state], BorderWidthByZoom(BorderWidth, zoom));
    }

    public static void PaintSliderThumb(PaintTarget target, RectF rect, ThemeState state, Color? accent)
    {
        var dark = state != ThemeState.Disabled && DarkForAccent(accent, Light.Fill[2], Dark.Fill[2]);
        var palette = dark ? Dark : Light;
        var radius = MathF.Max(rect.Width, rect.Height) * 0.5f;
        FillRoundRect(target, Inset(rect, 0.5f, 0.5f), radius, AccentOrControl(accent, palette.Slider, state, dark));
    }

    /// <summary>NativeThemeBase::PaintProgressBar: a track half as thick as the bar, the value clipped to it, a 1 px border.</summary>
    public static void PaintProgressBar(PaintTarget target, RectF rect, ThemeState state, float position, bool determinate, Color? accent, float zoom = 1)
    {
        var dark = DarkForAccent(accent, Light.Fill[2], Dark.Fill[2]);
        var palette = dark ? Dark : Light;
        var thickness = rect.Height * SliderTrackThickness / SliderThumbThickness;
        var track = AlignTrack(rect, false, 0, thickness);
        const float radius = 40;
        FillRoundRect(target, track, radius, palette.Fill[(int)state]);
        // The value rect: whole pixels of the width, at least 2 when there is any.
        var valueWidth = (int)(rect.Width * position);
        if (valueWidth > 0)
            valueWidth = Math.Max(valueWidth, 2);
        var value = AlignTrack(new RectF(rect.X, rect.Y, valueWidth, rect.Height), false, 0, thickness);
        var color = AccentOrControl(accent, palette.Accent, state, dark);
        if (determinate)
        {
            target.PushClip(track, Fit(track, radius));
            target.FillRect(value, color);
            target.PopClip();
        }
        else
            FillRoundRect(target, value, radius, color);
        StrokeRoundRect(target, track, radius, palette.SliderBorder[(int)state], BorderWidthByZoom(BorderWidth, zoom));
    }

    // ---------- Spin buttons ----------

    /// <summary>
    /// The number field's spin button (NativeThemeBase::PaintInnerSpinButton with Fluent arrow
    /// buttons): the upper half steps up, the lower half down; only the hovered or pressed half
    /// shows its state.
    /// </summary>
    public static void PaintInnerSpinButton(PaintTarget target, RectF rect, ThemeState state, bool upHovered)
    {
        var up = state;
        var down = state;
        if (upHovered)
            down = state == ThemeState.Disabled ? ThemeState.Disabled : ThemeState.Normal;
        else
            up = state == ThemeState.Disabled ? ThemeState.Disabled : ThemeState.Normal;
        var half = (int)(rect.Height / 2);
        PaintArrowButton(target, new RectF(rect.X, rect.Y, rect.Width, half), ScrollbarPart.BackButton, up);
        PaintArrowButton(target, new RectF(rect.X, rect.Y + half, rect.Width, half), ScrollbarPart.ForwardButton, down);
    }

    static void PaintArrowButton(PaintTarget target, RectF rect, ScrollbarPart part, ThemeState state)
    {
        if (rect.Width <= 0 || rect.Height <= 0)
            return;
        target.FillRect(rect, ScrollbarTheme.Track);
        var arrowState = state switch
        {
            ThemeState.Disabled => ScrollbarPartState.Disabled,
            ThemeState.Hovered => ScrollbarPartState.Hovered,
            ThemeState.Pressed => ScrollbarPartState.Pressed,
            _ => ScrollbarPartState.Normal,
        };
        var color = ScrollbarTheme.ArrowColor(null, null, arrowState);
        foreach (var span in ScrollbarTheme.ArrowPixels(rect, part, vertical: true, arrowState))
            target.FillRect(span, color);
    }
}
