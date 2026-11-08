using System.Numerics;

namespace Statventure.Canvas;

/// <summary>
/// Which copy of a raster image Chromium 152 draws, as cc's software image decode cache picks it (the references are
/// rastered without the GPU, <c>--disable-gpu-compositing</c>): Blink asks for every decoded image's draw at medium
/// filter quality (<c>BitmapImage::IsLazyDecoded</c>, <c>GraphicsContext::ComputeFilterQuality</c>,
/// <c>GetDefaultInterpolationQuality</c>: medium off Android), and at raster the cache keys the draw by the part of the
/// image used and the size it lands at on the device (<c>CacheKey::FromDrawImage</c>,
/// cc/tiles/software_image_decode_cache_utils.cc): a draw that shrinks the image to half or less in both directions
/// gets a copy scaled down to the mip level just above its size (<c>MipMapUtil</c>, cc/tiles/mipmap_util.cc), made by
/// <see cref="SkiaMipmap.ScalePixels"/>, and samples that copy bilinearly (<c>GetDecodedFilterQuality</c>: low); any
/// other draw samples the image itself bilinearly. Only a pixelated draw (filter quality none, <c>image-rendering</c>,
/// which the engine doesn't read) would skip the copy.
/// </summary>
public static class ImageDecodeScale
{
    /// <summary>The copy a draw uses: the image itself, a scaled-down copy of <see cref="Source"/> of <see cref="Target"/>'s size, or none (nothing drawn).</summary>
    public readonly record struct Decode(DecodeKind Kind, RectI Source, int TargetWidth, int TargetHeight);

    public enum DecodeKind
    {
        /// <summary>The draw lands on an empty size: Chromium skips the decode and draws nothing.</summary>
        Nothing,
        /// <summary>The image itself (kOriginal; kSubrectOriginal is the same pixels).</summary>
        Original,
        /// <summary>A copy of the source part scaled down to a mip level's size (kSubrectAndScale).</summary>
        Scaled,
    }

    /// <summary>A rectangle of whole pixels.</summary>
    public readonly record struct RectI(int X, int Y, int Width, int Height)
    {
        public bool IsEmpty => Width <= 0 || Height <= 0;
    }

    /// <summary>
    /// cc's DrawImage scale (<c>ExtractScale</c>, cc/paint/draw_image.cc) of the matrix from the image's pixels to the
    /// device's: its diagonal for a scale and translation, else SkMatrix::decomposeScale's column lengths; null when
    /// those are nearly zero or not finite (not decomposable: the draw uses the image itself).
    /// </summary>
    public static Vector2? ExtractScale(Matrix3x2 m)
    {
        // Skia's matrix [scaleX skewX transX; skewY scaleY transY] is System.Numerics' row-vector form transposed:
        // scaleX = M11, skewY = M12, skewX = M21, scaleY = M22.
        if (m.M12 == 0 && m.M21 == 0)
            return new Vector2(m.M11, m.M22);
        var sx = MathF.Sqrt(m.M11 * m.M11 + m.M12 * m.M12);
        var sy = MathF.Sqrt(m.M21 * m.M21 + m.M22 * m.M22);
        const float nearlyZero = 1f / (1 << 12);
        if (!float.IsFinite(sx) || !float.IsFinite(sy) || sx <= nearlyZero || sy <= nearlyZero)
            return null;
        return new Vector2(sx, sy);
    }

    /// <summary>
    /// The copy for drawing <paramref name="source"/> (the image's pixels used, rounded out and inside the image) of a
    /// <paramref name="imageWidth"/> × <paramref name="imageHeight"/> image through <paramref name="toDevice"/> (image
    /// pixels to device pixels): <c>CacheKey::FromDrawImage</c>.
    /// </summary>
    public static Decode For(int imageWidth, int imageHeight, RectI source, Matrix3x2 toDevice)
    {
        // A matrix that can't be decomposed counts as no scale (cc's (1, 1)): its target is the source's size, level 0.
        var factor = ExtractScale(toDevice) ?? Vector2.One;
        // The exact size on the device, SkScalarRoundToInt.
        var targetWidth = RoundToInt(MathF.Abs(source.Width * factor.X));
        var targetHeight = RoundToInt(MathF.Abs(source.Height * factor.Y));
        if (targetWidth <= 0 || targetHeight <= 0)
            return new Decode(DecodeKind.Nothing, source, targetWidth, targetHeight);
        var level = LevelForSize(source.Width, source.Height, targetWidth, targetHeight);
        if (level == 0)
            return new Decode(DecodeKind.Original, new RectI(0, 0, imageWidth, imageHeight), imageWidth, imageHeight);
        var (width, height) = SizeForLevel(source.Width, source.Height, level);
        return new Decode(DecodeKind.Scaled, source, width, height);
    }

    /// <summary>SkScalarRoundToInt: floor(x + 0.5).</summary>
    static int RoundToInt(float x) => (int)MathF.Floor(x + 0.5f);

    /// <summary>
    /// <c>MipMapUtil::GetLevelForSize</c>: the deepest mip level (each level half the last, rounded up) whose next level
    /// would be smaller than the target in either direction; so the level is never smaller than the target.
    /// </summary>
    public static int LevelForSize(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
    {
        var nextHeight = sourceHeight;
        var nextWidth = sourceWidth;
        for (var level = 0; ; level++)
        {
            var height = nextHeight;
            var width = nextWidth;
            nextHeight = AxisAtLevel(sourceHeight, level + 1);
            nextWidth = AxisAtLevel(sourceWidth, level + 1);
            if (nextHeight < targetHeight || nextWidth < targetWidth)
                return level;
            if (height == 1 && width == 1)
                return level;
        }
    }

    /// <summary><c>MipMapUtil::GetSizeForLevel</c>: each axis divided by 2^level, rounded up, at least 1.</summary>
    public static (int Width, int Height) SizeForLevel(int width, int height, int level) => (AxisAtLevel(width, level), AxisAtLevel(height, level));

    /// <summary><c>ScaleAxisToMipLevel</c>.</summary>
    static int AxisAtLevel(int size, int level)
    {
        if (level == 0)
            return size;
        var rounded = (long)size + (1L << level) - 1;
        return Math.Max(1, (int)Math.Min(int.MaxValue, rounded >> level));
    }
}
