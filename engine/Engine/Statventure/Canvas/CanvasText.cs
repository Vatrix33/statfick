using System.Numerics;
using Statventure.Core;
using Statventure.Text;

namespace Statventure.Canvas;

public sealed partial class Canvas
{
    readonly record struct GlyphKey(int Face, uint Glyph, float Size, int Subpixel, bool Bold, bool Oblique, bool Lcd, int Luminance);

    readonly Dictionary<GlyphKey, (int Generation, AtlasEntry Entry, int Left, int Top)> _glyphs = [];
    TextureAtlas? _lcdAtlas;
    const uint FlagLcd32 = 4;
    internal const uint KindLcd = 7;

    /// <summary>How glyphs are antialiased and hinted; the desktop's settings by default.</summary>
    public FontRenderSettings FontSettings { get; set; } = FontRenderSettings.System;

    TextureAtlas LcdAtlas => _lcdAtlas ??= new TextureAtlas(_device, bytesPerPixel: 4);

    /// <summary>
    /// LCD text needs to know what is behind it, so as in Chromium it is only used straight on the
    /// target (no layer), without filters or blend modes, and without rotation.
    /// </summary>
    bool UseLcd(Matrix3x2 m) =>
        FontSettings.IsSubpixel && _device.SupportsDualSourceBlend && (_textLcd ?? _lcdText)
        && ColorFilter is null && Blend == Graphics.BlendMode.Premultiplied && MathF.Abs(m.M12) < 1e-5f && MathF.Abs(m.M21) < 1e-5f;

    /// <summary>
    /// SK_MAX_SIZE_FOR_LCDTEXT (Skia's <c>SkScalerContext.cpp</c>, Chromium 152's pin): no glyph larger than this many device
    /// pixels is made as an LCD mask ("Beyond this size, LCD doesn't appreciably improve quality").
    /// </summary>
    internal const float MaxLcdTextSize = 48;

    /// <summary>
    /// Skia's <c>too_big_for_lcd</c> (<c>SkScalerContext::MakeRecAndEffects</c>): with a device matrix that only translates,
    /// the text size past <see cref="MaxLcdTextSize"/>; with one that scales or skews, the text size squared times the
    /// area of the matrix's 2×2 part (each entry relaxed to 1/1024, <c>sk_relax</c>) past its square. Such text gets
    /// Skia's A8 mask (with <c>kGenA8FromLCD</c>, which Fontations, every web font's scaler on Linux, ignores): the same
    /// grayscale glyph as text where the layer allows no LCD, its mask gamma the grayscale one. Nothing else changes
    /// with it: the glyph's subpixel position is the font's (<c>kSubpixelPositioning</c>), and the full hinting Skia
    /// lowers to normal for a mask that isn't LCD is already the engine's normal hinting (both FreeType's light-less
    /// <c>LOAD_TARGET_NORMAL</c>, <c>SkiaGlyphs.Outline</c>).
    /// </summary>
    /// <param name="textSize">The font's size as Skia gets it (Blink's effective size: to 0.01 px, truncated).</param>
    /// <param name="m">The device matrix the glyphs are drawn with (the canvas's transform and scale).</param>
    internal static bool TooBigForLcd(float textSize, Matrix3x2 m)
    {
        // SkMatrix's type mask: kScale_Mask when a diagonal entry isn't 1, kAffine_Mask when a skew entry isn't 0.
        if (m.M11 == 1 && m.M22 == 1 && m.M12 == 0 && m.M21 == 0)
            return textSize > MaxLcdTextSize;
        static float Relax(float x) => MathF.Floor(x * 1024 + 0.5f) / 1024;
        // fPost2x2: [0][0] scaleX, [1][1] scaleY, [0][1] skewX, [1][0] skewY (System.Numerics: M11, M22, M21, M12).
        var area = Relax(m.M11) * Relax(m.M22) - Relax(m.M12) * Relax(m.M21);
        area *= textSize * textSize;
        return area > MaxLcdTextSize * MaxLcdTextSize;
    }

    /// <summary>
    /// Place glyphs at quarter-pixel positions (as the game's Chromium does) instead of whole pixels.
    /// </summary>
    public bool SubpixelText { get; set; } = true;

    /// <summary>Draws laid-out text with its top left at <paramref name="position"/> (logical units).</summary>
    public void DrawText(TextLayout layout, Vector2 position, Color? color = null)
    {
        var m = _transform * Matrix3x2.CreateScale(Scale);
        var pixelScale = MathF.Sqrt(MathF.Abs(m.GetDeterminant()));
        foreach (var line in layout.Lines)
            foreach (var run in line.Runs)
            {
                var style = run.Style;
                DrawGlyphs(run, position + new Vector2(line.Left + run.X, line.Baseline), color ?? style.Color);
                if (style.Underline)
                {
                    var face = run.Font.Face;
                    var unit = style.Size / face.UnitsPerEm;
                    var thickness = MathF.Max(1f / pixelScale, MathF.Round(face.UnderlineThickness * unit * pixelScale) / pixelScale);
                    var offset = MathF.Round(face.UnderlinePosition * unit * pixelScale) / pixelScale;
                    FillRect(new RectF(position.X + line.Left + run.X, position.Y + line.Baseline + offset, run.Width, thickness), color ?? style.Color);
                }
            }
    }

    /// <summary>
    /// Draws a run's glyphs with the run's start on the baseline at <paramref name="origin"/>
    /// (logical units). Each glyph lands where Skia puts it (SkGlyphPositionRoundingSpec): the
    /// baseline rounded to a pixel, x to the nearest quarter pixel at or after it.
    /// </summary>
    public void DrawGlyphs(GlyphRun run, Vector2 origin, Color color)
    {
        if (run.Font.Face.IsIconFace)
        {
            DrawIcons(run, origin, color);
            return;
        }
        var m = _transform * Matrix3x2.CreateScale(Scale);
        var pixelScale = MathF.Sqrt(MathF.Abs(m.GetDeterminant()));
        // Glyphs are made at the font's size as Blink's font cache keeps it, to 0.01 px truncated
        // (FontDescription::EffectiveFontSize: 12 px at 1.65 is 19.79), then scaled by the canvas.
        var textSize = (int)(run.Style.Size * 100f) / 100f;
        var size = textSize * pixelScale;
        if (size < 0.5f || color.A <= 0)
            return;
        // One verdict for the run (one font size and matrix, as Skia makes one scaler context for a run): LCD where the
        // canvas allows it, unless the glyphs are too big for LCD masks.
        var lcd = UseLcd(m) && !TooBigForLcd(textSize, m);
        var previousBlend = Blend;
        if (lcd)
            Blend = Graphics.BlendMode.DualSourceLcd;
        var paint = color.Premultiplied;
        // Skia draws the engine's fonts (web fonts in Chromium) from their outlines with the mask
        // gamma built in, fake bold too (it strokes the outline).
        var luminance = Text.Skia.SkiaGlyphs.LuminanceKey(color, lcd);
        foreach (var glyph in run.Glyphs)
        {
            var local = origin + new Vector2(glyph.X, glyph.Y);
            var pixel = Vector2.Transform(local, m);
            var baseline = MathF.Floor(pixel.Y + 0.5f);
            float x;
            int subpixel;
            if (SubpixelText)
            {
                var mapped = pixel.X + 1f / (2 * GlyphRasterizer.SubpixelSteps);
                x = MathF.Floor(mapped);
                subpixel = (int)((mapped - x + 1f) * GlyphRasterizer.SubpixelSteps) & (GlyphRasterizer.SubpixelSteps - 1);
            }
            else
            {
                x = MathF.Floor(pixel.X + 0.5f);
                subpixel = 0;
            }
            var cached = Glyph(run.Font, glyph.Glyph, size, subpixel, lcd, luminance);
            if (cached.Entry.Width == 0 || cached.Entry.Height == 0)
                continue;
            var rect = new RectF(x + cached.Left, baseline - cached.Top, cached.Entry.Width, cached.Entry.Height);
            var primitive = PixelPrimitive(rect, lcd ? KindLcd : KindCoverage);
            primitive.Flags |= lcd ? FlagLcd32 : 0;
            primitive.Texture = (uint)cached.Entry.TextureIndex;
            primitive.Sampler = (uint)Graphics.SamplerKind.NearestClamp;
            primitive.Uv = new Vector4(cached.Entry.U0, cached.Entry.V0, cached.Entry.U1, cached.Entry.V1);
            primitive.Color = paint;
            primitive.ColorMatrix = CurrentMatrix();
            Add(primitive);
        }
        Blend = previousBlend;
    }

    /// <summary>
    /// An icon face's glyphs (<see cref="FontLibrary.AddIcons"/>; GM1 step 7): each icon's outline filled by the path
    /// fill in <paramref name="color"/>, at the run's size as its glyphs would be made (Blink's font size to 0.01 px,
    /// truncated), where its glyph would land: with no rotation or skew the baseline rounded to a pixel and the pen to
    /// the quarter pixel at or after it, as Skia places glyphs (SkGlyphPositionRoundingSpec; whole pixels without
    /// <see cref="SubpixelText"/>), else exactly. Coverage is grayscale and takes no mask gamma whatever the text's LCD
    /// verdict, as Chromium fills an inline SVG path, the icons' references; the blend mode, colour filter, clip and
    /// layers are the canvas's as for any drawing. A space (the parent font's, <see cref="IconFaceData"/>) draws nothing.
    /// </summary>
    void DrawIcons(GlyphRun run, Vector2 origin, Color color)
    {
        var m = _transform * Matrix3x2.CreateScale(Scale);
        var size = (int)(run.Style.Size * 100f) / 100f;
        if (size * MathF.Sqrt(MathF.Abs(m.GetDeterminant())) < 0.5f || color.A <= 0)
            return;
        var group = run.Font.Face.Icons!.Group;
        var brush = Brush.Solid(color);
        var em = Matrix3x2.CreateScale(size);
        var axisAligned = MathF.Abs(m.M12) < 1e-5f && MathF.Abs(m.M21) < 1e-5f;
        foreach (var glyph in run.Glyphs)
        {
            if (group.Outline(glyph.Glyph) is not { } outline)
                continue;
            var local = origin + new Vector2(glyph.X, glyph.Y);
            var placed = em * Matrix3x2.CreateTranslation(local) * m;
            if (axisAligned)
            {
                var pixel = Vector2.Transform(local, m);
                float x;
                if (SubpixelText)
                {
                    var mapped = pixel.X + 1f / (2 * GlyphRasterizer.SubpixelSteps);
                    var whole = MathF.Floor(mapped);
                    var subpixel = (int)((mapped - whole + 1f) * GlyphRasterizer.SubpixelSteps) & (GlyphRasterizer.SubpixelSteps - 1);
                    x = whole + subpixel / (float)GlyphRasterizer.SubpixelSteps;
                }
                else
                    x = MathF.Floor(pixel.X + 0.5f);
                placed.M31 = x;
                placed.M32 = MathF.Floor(pixel.Y + 0.5f);
            }
            DrawPath(outline, brush, FillRule.NonZero, null, placed);
        }
    }

    (AtlasEntry Entry, int Left, int Top) Glyph(ResolvedFont font, uint glyph, float size, int subpixel, bool lcd, int luminance)
    {
        var atlas = lcd ? LcdAtlas : Atlas;
        var key = new GlyphKey(font.Face.Id, glyph, size, subpixel, font.SyntheticBold, font.SyntheticOblique, lcd, luminance);
        if (_glyphs.TryGetValue(key, out var cached) && cached.Generation == atlas.Generation)
            return (cached.Entry, cached.Left, cached.Top);
        var bitmap = Text.Skia.SkiaGlyphs.Render(font.Face, glyph, size, subpixel, font.SyntheticBold, font.SyntheticOblique, FontSettings, lcd, luminance);
        var entry = bitmap.Width > 0 && bitmap.Height > 0 ? atlas.Add(bitmap.Width, bitmap.Height, bitmap.Coverage) : default;
        _glyphs[key] = (atlas.Generation, entry, bitmap.Left, bitmap.Top);
        return (entry, bitmap.Left, bitmap.Top);
    }
}
