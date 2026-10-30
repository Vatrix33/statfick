using System.Runtime.InteropServices;

namespace Statventure.Text;

/// <summary>
/// A rasterized glyph: coverage and where its top left sits from the pen position. Grayscale masks
/// have one byte per pixel; LCD masks have four (red, green, blue coverage and their maximum).
/// Preblended masks (Skia's, <see cref="Skia.SkiaGlyphs"/>) already have the mask gamma applied;
/// their LCD coverage runs 0–32.
/// </summary>
public sealed record GlyphBitmap(int Width, int Height, int Left, int Top, byte[] Coverage, bool Lcd = false, bool Preblended = false);

/// <summary>
/// Draws glyphs with FreeType as Chromium on Linux does: the hinting the system asks for (slight is
/// vertical-only), grayscale or LCD antialiasing with the system's LCD filter, fake bold by
/// emboldening the outline by ppem/24, fake italic by a 1/4 shear, optional quarter-pixel offsets.
/// </summary>
public static unsafe class GlyphRasterizer
{
    public const int SubpixelSteps = 4;
    static int _lcdFilter = -1;

    public static GlyphBitmap Render(FontFace face, uint glyph, float sizePixels, int subpixel, bool syntheticBold, bool syntheticOblique, FontRenderSettings? settings = null, bool lcd = false)
    {
        settings ??= FontRenderSettings.Grayscale;
        lcd &= settings.IsSubpixel;
        var ft = face.FtFace;
        if (lcd && _lcdFilter != settings.LcdFilter)
        {
            FreeType.LibrarySetLcdFilter(face.FtLibrary, settings.LcdFilter);
            _lcdFilter = settings.LcdFilter;
        }
        FreeType.Check(FreeType.SetCharSize(ft, new CLong(0), new CLong((nint)MathF.Round(sizePixels * 64f)), 72, 72), "FT_Set_Char_Size");
        var matrix = new FreeType.Matrix
        {
            XX = new CLong(0x10000),
            XY = new CLong(syntheticOblique ? 0x4000 : 0),
            YX = new CLong(0),
            YY = new CLong(0x10000),
        };
        var delta = new FreeType.Vector { X = new CLong(subpixel * 64 / SubpixelSteps), Y = new CLong(0) };
        FreeType.SetTransform(ft, &matrix, &delta);
        var flags = FreeType.LoadNoBitmap | settings.Hinting switch
        {
            TextHinting.None => FreeType.LoadNoHinting,
            TextHinting.Slight => FreeType.LoadTargetLight,
            _ => FreeType.LoadTargetNormal,
        };
        FreeType.Check(FreeType.LoadGlyph(ft, glyph, flags), "FT_Load_Glyph");
        var slot = ft->Glyph;
        if (syntheticBold)
        {
            var strength = (long)MathF.Round(sizePixels * 64f / 24f);
            FreeType.OutlineEmboldenXY(&slot->OutlineStart, new CLong((nint)strength), new CLong((nint)strength));
            FreeType.OutlineTranslate(&slot->OutlineStart, new CLong((nint)(-strength / 2)), new CLong((nint)(-strength / 2)));
        }
        FreeType.Check(FreeType.RenderGlyph(slot, lcd ? FreeType.RenderModeLcd : FreeType.RenderModeNormal), "FT_Render_Glyph");
        FreeType.SetTransform(ft, null, null);
        var bitmap = slot->Bitmap;
        var height = (int)bitmap.Rows;
        if (bitmap.PixelMode == FreeType.PixelModeLcd)
        {
            var width = (int)bitmap.Width / 3;
            var rgba = new byte[width * height * 4];
            var bgr = settings.Antialias == TextAntialias.SubpixelBgr;
            for (var y = 0; y < height; y++)
            {
                var row = bitmap.Buffer + y * bitmap.Pitch;
                for (var x = 0; x < width; x++)
                {
                    byte r = row[x * 3], g = row[x * 3 + 1], b = row[x * 3 + 2];
                    if (bgr)
                        (r, b) = (b, r);
                    var i = (y * width + x) * 4;
                    rgba[i] = r;
                    rgba[i + 1] = g;
                    rgba[i + 2] = b;
                    rgba[i + 3] = Math.Max(r, Math.Max(g, b));
                }
            }
            return new GlyphBitmap(width, height, slot->BitmapLeft, slot->BitmapTop, rgba, Lcd: true);
        }
        var gray = new byte[(int)bitmap.Width * height];
        for (var y = 0; y < height; y++)
            new ReadOnlySpan<byte>(bitmap.Buffer + y * bitmap.Pitch, (int)bitmap.Width).CopyTo(gray.AsSpan(y * (int)bitmap.Width, (int)bitmap.Width));
        return new GlyphBitmap((int)bitmap.Width, height, slot->BitmapLeft, slot->BitmapTop, gray);
    }
}
