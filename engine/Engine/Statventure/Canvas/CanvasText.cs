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
        var m = _transform * Matrix3x2.CreateScale(Scale);
        var pixelScale = MathF.Sqrt(MathF.Abs(m.GetDeterminant()));
        // Glyphs are made at the font's size as Blink's font cache keeps it, to 0.01 px truncated
        // (FontDescription::EffectiveFontSize: 12 px at 1.65 is 19.79), then scaled by the canvas.
        var size = (int)(run.Style.Size * 100f) / 100f * pixelScale;
        if (size < 0.5f || color.A <= 0)
            return;
        var lcd = UseLcd(m);
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
