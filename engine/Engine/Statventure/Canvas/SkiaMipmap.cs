using Statventure.Imaging;

namespace Statventure.Canvas;

/// <summary>
/// Skia's part of a shrunk image in Chromium 152 (Skia at b6d106297ff9, reference-src/skia/mipmap/): the decoded
/// pixels premultiplied (<see cref="Premultiply"/>), their mipmaps (<see cref="Downsample"/>, the HQ downsampler
/// Chromium builds, SkMipmapHQDownSampler.cpp) and <c>SkPixmap::scalePixels</c> with linear filtering and the nearest
/// mipmap (<see cref="ScalePixels"/>), which cc's decode cache uses to make its scaled copies
/// (<c>GenerateCacheEntryFromCandidate</c>). Every image here is premultiplied RGBA, 8 bits a channel.
/// </summary>
public static class SkiaMipmap
{
    /// <summary>
    /// Straight-alpha RGBA as files decode, premultiplied as Skia's codecs premultiply a PNG's rows (Blink 152 decodes
    /// PNGs with <c>SkPngRustDecoder</c>; the swizzler's <c>RGBA_to_rgbA</c>: each colour times alpha over 255, rounded).
    /// </summary>
    public static RgbaImage Premultiply(RgbaImage straight)
    {
        var source = straight.Pixels;
        var result = new byte[source.Length];
        for (var i = 0; i < source.Length; i += 4)
        {
            var a = source[i + 3];
            result[i + 3] = a;
            for (var c = 0; c < 3; c++)
                result[i + c] = a == 255 ? source[i + c] : (byte)((source[i + c] * a + 127) / 255);
        }
        return new RgbaImage(straight.Width, straight.Height, result);
    }

    /// <summary>
    /// One mip level below <paramref name="source"/> (<c>HQDownSampler::buildLevel</c>): half its size, rounded down,
    /// at least 1; each pixel the truncated weighted mean of the 2 × 2 pixels at twice its place, widened to the three
    /// columns (1 2 1) where the source's width is odd and above 1, to one where it is 1, and the same for rows; the
    /// weights' sum is a power of two, divided by shifting (the downsample_W_H filters).
    /// </summary>
    public static RgbaImage Downsample(RgbaImage source)
    {
        int width = source.Width, height = source.Height;
        var result = new RgbaImage(Math.Max(1, width >> 1), Math.Max(1, height >> 1));
        ReadOnlySpan<int> one = [1], two = [1, 1], three = [1, 2, 1];
        var columns = width == 1 ? one : (width & 1) == 1 ? three : two;
        var rows = height == 1 ? one : (height & 1) == 1 ? three : two;
        var total = 0;
        foreach (var r in rows)
            foreach (var c in columns)
                total += r * c;
        var shift = System.Numerics.BitOperations.Log2((uint)total);
        var src = source.Pixels;
        var dst = result.Pixels;
        Span<int> sum = stackalloc int[4];
        for (var y = 0; y < result.Height; y++)
            for (var x = 0; x < result.Width; x++)
            {
                sum.Clear();
                for (var j = 0; j < rows.Length; j++)
                    for (var i = 0; i < columns.Length; i++)
                    {
                        var weight = rows[j] * columns[i];
                        var at = ((y * 2 + j) * width + x * 2 + i) * 4;
                        for (var c = 0; c < 4; c++)
                            sum[c] += src[at + c] * weight;
                    }
                var to = (y * result.Width + x) * 4;
                for (var c = 0; c < 4; c++)
                    dst[to + c] = (byte)(sum[c] >> shift);
            }
        return result;
    }

    /// <summary>
    /// <c>SkMipmap::ComputeLevelCount</c>: the levels below the base, down to 1 × 1 (the largest axis' significant
    /// bits less one).
    /// </summary>
    public static int LevelCount(int width, int height)
    {
        var largest = Math.Max(width, height);
        if (width < 1 || height < 1 || largest < 2)
            return 0;
        return 32 - System.Numerics.BitOperations.LeadingZeroCount((uint)largest) - 1;
    }

    /// <summary>
    /// <c>SkMipmap::ComputeLevel</c> as Chromium builds Skia (<c>SK_SUPPORT_LEGACY_ANISOTROPIC_MIPMAP_SCALE</c>,
    /// skia/config/SkUserConfig.h): the geometric mean of the two scales, the level log2(1/scale) less a half (the
    /// GPU's sharpening bias), at least 0; -1 for no shrink.
    /// </summary>
    public static float ComputeLevel(float scaleX, float scaleY)
    {
        var scale = MathF.Sqrt(scaleX * scaleY);
        if (scale >= 1 || scale <= 0 || !float.IsFinite(scale))
            return -1;
        var level = MathF.Max(-MathF.Log2(scale) - 0.5f, 0);
        return float.IsFinite(level) ? level : -1;
    }

    /// <summary>
    /// The level <c>SkMipmapAccessor</c> samples for the nearest mipmap at an inverse scale of
    /// <paramref name="inverseX"/>, <paramref name="inverseY"/> (destination to source): the computed level rounded
    /// (sk_float_round2int), 0 (the image itself) for no shrink or a level past the chain's end.
    /// </summary>
    public static int NearestLevel(int width, int height, float inverseX, float inverseY)
    {
        var level = ComputeLevel(1 / inverseX, 1 / inverseY);
        if (level <= 0)
            return 0;
        var number = (int)MathF.Floor(level + 0.5f);
        // SkMipmap::getLevel fails past the last level: the accessor then samples the base.
        return number > LevelCount(width, height) ? 0 : number;
    }

    /// <summary>
    /// <c>SkPixmap::scalePixels</c> with linear filtering and the nearest mipmap (cc's medium quality,
    /// <c>FilterQualityToSkSamplingOptions</c>): the same size copied; else the level <see cref="NearestLevel"/> picks
    /// (built from <paramref name="source"/> by <see cref="Downsample"/>), sampled bilinearly at each destination
    /// pixel's centre, clamped at the edges (the raster pipeline's <c>bilerp_clamp_8888</c>), stored rounded.
    /// </summary>
    public static RgbaImage ScalePixels(RgbaImage source, int width, int height)
    {
        if (source.Width == width && source.Height == height)
            return new RgbaImage(width, height, (byte[])source.Pixels.Clone());
        var inverseX = (float)source.Width / width;
        var inverseY = (float)source.Height / height;
        var level = source;
        for (var i = NearestLevel(source.Width, source.Height, inverseX, inverseY); i > 0; i--)
            level = Downsample(level);
        // Destination pixel centres to the level's pixels: through the source's size, as the shader's matrix and the
        // accessor's level scale do.
        var toLevelX = inverseX * level.Width / source.Width;
        var toLevelY = inverseY * level.Height / source.Height;
        var result = new RgbaImage(width, height);
        var src = level.Pixels;
        var dst = result.Pixels;
        Span<float> sum = stackalloc float[4];
        for (var y = 0; y < height; y++)
        {
            var cy = (y + 0.5f) * toLevelY;
            var fy = Fract(cy + 0.5f);
            for (var x = 0; x < width; x++)
            {
                var cx = (x + 0.5f) * toLevelX;
                var fx = Fract(cx + 0.5f);
                sum.Clear();
                for (var py = -1; py <= 1; py += 2)
                    for (var px = -1; px <= 1; px += 2)
                    {
                        var sx = Clamp(cx + px * 0.5f, level.Width);
                        var sy = Clamp(cy + py * 0.5f, level.Height);
                        var area = (px > 0 ? fx : 1 - fx) * (py > 0 ? fy : 1 - fy);
                        var at = (sy * level.Width + sx) * 4;
                        for (var c = 0; c < 4; c++)
                            sum[c] += src[at + c] / 255f * area;
                    }
                var to = (y * width + x) * 4;
                for (var c = 0; c < 4; c++)
                    dst[to + c] = (byte)Math.Clamp(MathF.Round(sum[c] * 255), 0, 255);
            }
        }
        return result;
    }

    static float Fract(float v) => v - MathF.Floor(v);

    /// <summary><c>ix_and_ptr</c>'s clamp: the coordinate held inside the image, then truncated.</summary>
    static int Clamp(float v, int size) => (int)MathF.Min(MathF.Max(0, v), size - 1);
}
