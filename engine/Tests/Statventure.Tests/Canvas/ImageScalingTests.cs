using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using CanvasRenderer = Statventure.Canvas.Canvas;
using RectI = Statventure.Canvas.ImageDecodeScale.RectI;

namespace Statventure.Tests.Canvas;

/// <summary>
/// The rules of a shrunk raster image as Chromium 152 draws one (<see cref="ImageDecodeScale"/>, cc's software decode
/// cache; <see cref="SkiaMipmap"/>, Skia's mipmaps; the canvas's sampling, Skia's legacy raster shader), each against
/// numbers worked out by hand from the sources (reference-src/chromium-152-source/mipmap/, reference-src/skia/mipmap/).
/// The paint fixtures (Ui/ImageScalingPaintReferenceTests) check the whole against Chromium.
/// </summary>
public class ImageScalingTests
{
    [Theory]
    // Each level half the last, rounded up; the deepest whose next level isn't smaller than the target.
    [InlineData(320, 160, 320, 160, 0)]
    [InlineData(320, 160, 161, 80, 0)]
    [InlineData(320, 160, 160, 80, 1)]
    [InlineData(320, 160, 160, 81, 0)]
    [InlineData(320, 160, 80, 40, 2)]
    [InlineData(320, 160, 79, 40, 2)]
    [InlineData(320, 160, 53, 27, 2)]
    [InlineData(201, 101, 101, 51, 1)]
    [InlineData(201, 101, 101, 50, 1)]
    [InlineData(201, 101, 100, 52, 0)]
    [InlineData(201, 101, 51, 26, 2)]
    [InlineData(320, 160, 1, 1, 9)]
    [InlineData(4096, 896, 696, 152, 2)]
    [InlineData(4096, 896, 1148, 251, 1)]
    public void TheMipLevelIsTheDeepestAboveTheTarget(int width, int height, int targetWidth, int targetHeight, int level) =>
        Assert.Equal(level, ImageDecodeScale.LevelForSize(width, height, targetWidth, targetHeight));

    [Theory]
    [InlineData(320, 160, 1, 160, 80)]
    [InlineData(201, 101, 1, 101, 51)]
    [InlineData(201, 101, 2, 51, 26)]
    [InlineData(201, 101, 3, 26, 13)]
    [InlineData(5, 1, 3, 1, 1)]
    [InlineData(4096, 896, 2, 1024, 224)]
    public void ALevelsSizeIsRoundedUp(int width, int height, int level, int levelWidth, int levelHeight) =>
        Assert.Equal((levelWidth, levelHeight), ImageDecodeScale.SizeForLevel(width, height, level));

    [Fact]
    public void TheScaleIsTheDiagonalOrTheColumnsLengths()
    {
        Assert.Equal(new Vector2(0.5f, -2f), ImageDecodeScale.ExtractScale(new Matrix3x2(0.5f, 0, 0, -2f, 10, 20)));
        // A rotation by 90° scaled by 2 and 3: SkMatrix::decomposeScale's column lengths (x: scaleX and skewY).
        var turned = ImageDecodeScale.ExtractScale(Matrix3x2.CreateScale(2, 3) * Matrix3x2.CreateRotation(MathF.PI / 2))!.Value;
        Assert.Equal(2, turned.X, 4);
        Assert.Equal(3, turned.Y, 4);
        // A sheared matrix: x's column (M11, M12), y's (M21, M22).
        var sheared = ImageDecodeScale.ExtractScale(new Matrix3x2(3, 4, 0, 1, 0, 0))!.Value;
        Assert.Equal(new Vector2(5, 1), sheared);
        Assert.Null(ImageDecodeScale.ExtractScale(new Matrix3x2(1e-5f, 1e-5f, 1, 1, 0, 0)));
    }

    [Fact]
    public void ADrawShrunkToHalfOrLessUsesAScaledCopyOfItsSource()
    {
        var whole = new RectI(0, 0, 320, 160);
        Assert.Equal(new ImageDecodeScale.Decode(ImageDecodeScale.DecodeKind.Scaled, whole, 160, 80), ImageDecodeScale.For(320, 160, whole, Matrix3x2.CreateScale(0.5f)));
        // Rounded to whole pixels first (SkScalarRoundToInt): 0.502 lands on 160.6, 161 pixels, more than half.
        Assert.Equal(ImageDecodeScale.DecodeKind.Original, ImageDecodeScale.For(320, 160, whole, Matrix3x2.CreateScale(0.502f)).Kind);
        Assert.Equal(ImageDecodeScale.DecodeKind.Scaled, ImageDecodeScale.For(320, 160, whole, Matrix3x2.CreateScale(0.501f)).Kind);
        // An upscale and the same size use the image itself, its whole.
        Assert.Equal(new ImageDecodeScale.Decode(ImageDecodeScale.DecodeKind.Original, whole, 320, 160), ImageDecodeScale.For(320, 160, new RectI(10, 10, 100, 50), Matrix3x2.CreateScale(1.65f)));
        // A part: the copy is of the part, at its level's size.
        var part = new RectI(10, 20, 101, 51);
        Assert.Equal(new ImageDecodeScale.Decode(ImageDecodeScale.DecodeKind.Scaled, part, 26, 13), ImageDecodeScale.For(320, 160, part, Matrix3x2.CreateScale(0.25f)));
        // A mirrored draw shrinks by the scale's size.
        Assert.Equal(ImageDecodeScale.DecodeKind.Scaled, ImageDecodeScale.For(320, 160, whole, Matrix3x2.CreateScale(-0.5f, 0.5f)).Kind);
        // Landing on no pixel: nothing is drawn (the decode is skipped).
        Assert.Equal(ImageDecodeScale.DecodeKind.Nothing, ImageDecodeScale.For(320, 160, whole, Matrix3x2.CreateScale(0.001f)).Kind);
        // A matrix that can't be decomposed counts as no scale: the image itself.
        Assert.Equal(ImageDecodeScale.DecodeKind.Original, ImageDecodeScale.For(320, 160, whole, new Matrix3x2(0.1f, 0.0001f, 0.0001f, 0.0001f, 0, 0)).Kind);
    }

    static RgbaImage Pixels(int width, int height, params byte[] rgba) => new(width, height, rgba);

    [Fact]
    public void DecodedPixelsArePremultipliedRounded()
    {
        var premultiplied = SkiaMipmap.Premultiply(Pixels(4, 1, 255, 128, 1, 128, 255, 255, 255, 0, 200, 100, 50, 255, 1, 1, 1, 1));
        Assert.Equal(new byte[] { 128, 64, 1, 128, 0, 0, 0, 0, 200, 100, 50, 255, 0, 0, 0, 1 }, premultiplied.Pixels);
    }

    [Fact]
    public void ALevelOfEvenSizeIsTheTruncatedMeanOfFour()
    {
        var level = SkiaMipmap.Downsample(Pixels(2, 2, 1, 255, 0, 0, 1, 255, 0, 0, 1, 255, 0, 0, 2, 254, 3, 7));
        Assert.Equal((1, 1), (level.Width, level.Height));
        // (1+1+1+2)/4 = 1.25, (255·3+254)/4 = 254.75, 3/4, 7/4: each truncated.
        Assert.Equal(new byte[] { 1, 254, 0, 1 }, level.Pixels);
    }

    [Fact]
    public void AnOddSizeWidensTheFilterToOneTwoOne()
    {
        // 3 × 2: (a + 2b + c) over both rows, / 8.
        var row = SkiaMipmap.Downsample(Pixels(3, 2, 8, 0, 0, 0, 16, 0, 0, 0, 40, 0, 0, 0, 8, 0, 0, 0, 16, 0, 0, 0, 40, 0, 0, 0));
        Assert.Equal((1, 1), (row.Width, row.Height));
        Assert.Equal(20, row.Pixels[0]);
        // 3 × 3: the same down the rows, / 16.
        var source = new byte[3 * 3 * 4];
        int[] weights = [1, 2, 1, 2, 4, 2, 1, 2, 1];
        for (var i = 0; i < 9; i++)
            source[i * 4] = (byte)(i * 10 + 3);
        var both = SkiaMipmap.Downsample(new RgbaImage(3, 3, source));
        Assert.Equal(weights.Select((w, i) => w * (i * 10 + 3)).Sum() / 16, both.Pixels[0]);
        // 2 × 3 (rows 1 2 1, columns 1 1): / 8; 1 × 3: / 4; 1 × 2: / 2; 3 × 1 and 2 × 1 alike.
        Assert.Equal((8 + 2 * 16 + 40 + 8 + 2 * 16 + 40) / 8, SkiaMipmap.Downsample(Pixels(2, 3, 8, 0, 0, 0, 8, 0, 0, 0, 16, 0, 0, 0, 16, 0, 0, 0, 40, 0, 0, 0, 40, 0, 0, 0)).Pixels[0]);
        Assert.Equal((8 + 2 * 16 + 41) / 4, SkiaMipmap.Downsample(Pixels(1, 3, 8, 0, 0, 0, 16, 0, 0, 0, 41, 0, 0, 0)).Pixels[0]);
        Assert.Equal((8 + 17) / 2, SkiaMipmap.Downsample(Pixels(1, 2, 8, 0, 0, 0, 17, 0, 0, 0)).Pixels[0]);
        Assert.Equal((8 + 2 * 16 + 41) / 4, SkiaMipmap.Downsample(Pixels(3, 1, 8, 0, 0, 0, 16, 0, 0, 0, 41, 0, 0, 0)).Pixels[0]);
        Assert.Equal((8 + 17) / 2, SkiaMipmap.Downsample(Pixels(2, 1, 8, 0, 0, 0, 17, 0, 0, 0)).Pixels[0]);
        // Five wide: two pixels from columns 0-2 and 2-4, sharing the middle one.
        var five = SkiaMipmap.Downsample(Pixels(5, 1, 0, 0, 0, 0, 4, 0, 0, 0, 8, 0, 0, 0, 12, 0, 0, 0, 100, 0, 0, 0));
        Assert.Equal(new byte[] { 4, 0, 0, 0, 33, 0, 0, 0 }, five.Pixels);
    }

    [Theory]
    [InlineData(4096, 896, 12)]
    [InlineData(2, 1, 1)]
    [InlineData(3, 3, 1)]
    [InlineData(4, 1, 2)]
    [InlineData(1, 1, 0)]
    [InlineData(201, 101, 7)]
    public void TheChainGoesDownToOnePixel(int width, int height, int levels) => Assert.Equal(levels, SkiaMipmap.LevelCount(width, height));

    [Fact]
    public void SkiasLevelIsTheGeometricMeansLogLessAHalf()
    {
        Assert.Equal(1.5f, SkiaMipmap.ComputeLevel(0.25f, 0.25f), 5);
        Assert.Equal(0.5f, SkiaMipmap.ComputeLevel(0.5f, 0.5f), 5);
        // The geometric mean (Chromium's SK_SUPPORT_LEGACY_ANISOTROPIC_MIPMAP_SCALE), not the smaller scale.
        Assert.Equal(0.5f, SkiaMipmap.ComputeLevel(0.25f, 1), 5);
        Assert.Equal(0f, SkiaMipmap.ComputeLevel(0.8f, 0.8f));
        Assert.Equal(-1f, SkiaMipmap.ComputeLevel(1, 1));
        Assert.Equal(-1f, SkiaMipmap.ComputeLevel(2, 2));
        // The nearest mipmap rounds half up; a level past the chain samples the image itself.
        Assert.Equal(2, SkiaMipmap.NearestLevel(320, 160, 4, 4));
        Assert.Equal(1, SkiaMipmap.NearestLevel(320, 160, 2, 2));
        Assert.Equal(1, SkiaMipmap.NearestLevel(320, 160, 2.5f, 2.5f));
        Assert.Equal(0, SkiaMipmap.NearestLevel(201, 101, 201f / 101, 101f / 51));
        Assert.Equal(0, SkiaMipmap.NearestLevel(4, 4, 64, 64));
    }

    [Fact]
    public void ScalingPixelsToALevelsSizeCopiesTheLevel()
    {
        var checks = Checks(8, 4);
        var scaled = SkiaMipmap.ScalePixels(checks, 2, 1);
        var level = SkiaMipmap.Downsample(SkiaMipmap.Downsample(checks));
        Assert.Equal(level.Pixels, scaled.Pixels);
        Assert.Equal(checks.Pixels, SkiaMipmap.ScalePixels(checks, 8, 4).Pixels);
        Assert.NotSame(checks.Pixels, SkiaMipmap.ScalePixels(checks, 8, 4).Pixels);
    }

    [Fact]
    public void ScalingPixelsBetweenLevelsSamplesBilinearlyAtPixelCentres()
    {
        // 3 → 2 wide (no level: 0.67 is no mip), one row: centres at 0.75 and 2.25, less half a pixel.
        var scaled = SkiaMipmap.ScalePixels(Pixels(3, 1, 0, 0, 0, 0, 100, 0, 0, 0, 200, 0, 0, 0), 2, 1);
        // 0.25 between 0 and 100 → 25; 1.75 → 175.
        Assert.Equal(25, scaled.Pixels[0]);
        Assert.Equal(175, scaled.Pixels[4]);
        // 2 → 1: the first level, the two pixels' mean.
        Assert.Equal(new byte[] { 50, 0, 0, 0 }, SkiaMipmap.ScalePixels(Pixels(2, 1, 0, 0, 0, 0, 100, 0, 0, 0), 1, 1).Pixels);
    }

    /// <summary>One-pixel checks of 240 and 24 grey, opaque.</summary>
    internal static RgbaImage Checks(int width, int height)
    {
        var image = new RgbaImage(width, height);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var v = (byte)((x + y) % 2 == 0 ? 240 : 24);
                image[x, y] = (v, v, v, 255);
            }
        return image;
    }

    [Fact]
    public void ACanvasImageCutsAPartBeforeScalingIt()
    {
        var pixels = new RgbaImage(8, 4);
        for (var y = 0; y < 4; y++)
            for (var x = 0; x < 8; x++)
                pixels[x, y] = ((byte)(x * 30), (byte)(y * 60), 0, 255);
        using var image = new CanvasImage(pixels);
        var part = image.ScaledPixels(new RectI(2, 2, 4, 2), 2, 1);
        // The mean of columns 2-3 and 4-5 of rows 2-3.
        Assert.Equal(new byte[] { 75, 150, 0, 255, 135, 150, 0, 255 }, part.Pixels);
    }

    /// <summary>The canvas draws them: a GPU test of the copies chosen and of Skia's sampling in the shader.</summary>
    [Collection("GPU")]
    public class Drawn(GpuFixture gpu)
    {
        RgbaImage Render(int width, int height, Action<CanvasRenderer> draw, float scale = 1)
        {
            var device = gpu.Device;
            using var canvas = new CanvasRenderer(device);
            using var target = GpuImage.CreateRenderTarget(device, width, height);
            var frame = device.BeginFrame();
            canvas.Begin(width, height, scale);
            draw(canvas);
            canvas.Render(frame, target);
            device.EndFrame(frame);
            device.WaitIdle();
            return new RgbaImage(width, height, target.ReadPixels());
        }

        [Fact]
        public void AnImageShrunkToHalfIsItsFirstLevel()
        {
            using var image = new CanvasImage(Checks(64, 32));
            var half = Render(32, 16, c => c.DrawImage(image, new RectF(0, 0, 32, 16)));
            // Every pixel the truncated mean of two 240s and two 24s: 132.
            Assert.All(Enumerable.Range(0, 32 * 16), i => Assert.Equal(new byte[] { 132, 132, 132, 255 }, half.Pixels[(i * 4)..(i * 4 + 4)]));
            Assert.Equal(1, image.ScaledCopies);
            // Through the canvas's scale and transform alike: 2 px a unit, scaled by 0.25.
            using var again = new CanvasImage(Checks(64, 32));
            var scaled = Render(32, 16, c =>
            {
                c.PushTransform(Matrix3x2.CreateScale(0.25f));
                c.DrawImage(again, new RectF(0, 0, 64, 32));
                c.PopTransform();
            }, scale: 2);
            Assert.Equal(half.Pixels, scaled.Pixels);
            // Columns (no level looks like another): the canvas's own scale counts. 64 columns through a scale of 2 and
            // a transform of 0.25: 32 pixels, the first level, each the mean of two columns (x·4 and x·4 + 4).
            using var columns = new CanvasImage(Columns(64, 4));
            var levelled = Render(32, 2, c =>
            {
                c.PushTransform(Matrix3x2.CreateScale(0.25f));
                c.DrawImage(columns, new RectF(0, 0, 64, 4));
                c.PopTransform();
            }, scale: 2);
            for (var x = 0; x < 32; x++)
                Assert.Equal(8 * x + 2, levelled.Pixels[x * 4]);
        }

        [Fact]
        public void AnImageShrunkLessThanHalfIsSampledItself()
        {
            using var image = new CanvasImage(Checks(64, 32));
            var drawn = Render(33, 17, c => c.DrawImage(image, new RectF(0, 0, 33, 17)));
            Assert.Equal(0, image.ScaledCopies);
            Assert.Contains(Enumerable.Range(0, 33 * 17), i => drawn.Pixels[i * 4] != drawn.Pixels[0]);
        }

        [Fact]
        public void AnImageLandingOnNoPixelIsNotDrawn()
        {
            using var image = new CanvasImage(Checks(64, 32));
            var drawn = Render(8, 8, c => c.DrawImage(image, new RectF(2, 2, 0.007f, 0.007f)));
            Assert.All(drawn.Pixels, v => Assert.Equal(0, v));
            Assert.Equal(0, image.ScaledCopies);
        }

        /// <summary>Skia's legacy bilinear sample along one row (S32_alpha_D32_filter_DX): the texel and the weight in sixteenths of x - 0.5, clamped or wrapped.</summary>
        static int Expected(byte[] row, double x, bool wrap)
        {
            var v = x - 0.5;
            var low = (int)Math.Floor(v);
            var w = (int)Math.Floor((v - low) * 16);
            int Index(int i) => wrap ? ((i % row.Length) + row.Length) % row.Length : Math.Clamp(i, 0, row.Length - 1);
            // One row: the y pass multiplies by 16 and divides by 256 with the row's own weights, (top · 16) >> 8.
            var top = row[Index(low)] * (16 - w) + row[Index(low + 1)] * w;
            return top * 16 >> 8;
        }

        [Fact]
        public void TheSamplingWeighsInSixteenthsAndTruncates()
        {
            byte[] values = [0, 255, 30, 200, 90];
            var pixels = new RgbaImage(values.Length, 1);
            for (var i = 0; i < values.Length; i++)
                pixels[i, 0] = (values[i], values[i], values[i], 255);
            using var image = new CanvasImage(pixels);
            // Stretched 5 → 16 (an upscale: the image itself) three pixels in; no centre lands on a sixteenth exactly,
            // where float rounding would pick a side.
            var drawn = Render(19, 1, c => c.DrawImage(image, new RectF(3, 0, 16, 1)));
            for (var x = 0; x < 16; x++)
                Assert.Equal(Expected(values, (x + 0.5) * 5 / 16, wrap: false), drawn.Pixels[(x + 3) * 4]);
            // As a pattern from half way into the image: two repeats over 32 pixels, wrapped.
            var repeated = Render(32, 1, c => c.DrawImage(image, new RectF(0, 0, 32, 1), new RectF(0.5f, 0, 2, 1), sampler: SamplerKind.LinearRepeat));
            for (var x = 0; x < 32; x++)
                Assert.Equal(Expected(values, 2.5 + (x + 0.5) * 10 / 32, wrap: true), repeated.Pixels[x * 4]);
        }

        /// <summary>Columns of their own values (x · 4), every row alike, opaque.</summary>
        static RgbaImage Columns(int width, int height)
        {
            var image = new RgbaImage(width, height);
            for (var y = 0; y < height; y++)
                for (var x = 0; x < width; x++)
                    image[x, y] = ((byte)(x * 4), (byte)(x * 4), (byte)(x * 4), 255);
            return image;
        }

        [Fact]
        public void APartIsCutBeforeItIsShrunk()
        {
            // Columns 32.6 to 64 of 64 (rounded out from 32), drawn 16 wide: shrunk past half, a copy of columns 32-63
            // at its first level, 16 wide, each the mean of two columns.
            using var image = new CanvasImage(Columns(64, 4));
            var part = new RectF(32.6f / 64, 0, (64 - 32.6f) / 64, 1);
            var drawn = Render(16, 2, c => c.DrawImage(image, new RectF(0, 0, 16, 2), part));
            Assert.Equal(1, image.ScaledCopies);
            var copy = image.ScaledPixels(new ImageDecodeScale.RectI(32, 0, 32, 4), 16, 2);
            Assert.Equal(new byte[] { 130, 130, 130, 255 }, copy.Pixels[..4]);
            // Sampled where the part lies in the copy: 0.6 of a column in, at 31.4 / 32 of a column a pixel.
            for (var x = 0; x < 16; x++)
            {
                var row = Enumerable.Range(0, 16).Select(i => copy.Pixels[i * 4]).ToArray();
                Assert.Equal(Expected(row, (0.6 + (x + 0.5) * 31.4 / 16) / 2, wrap: false), drawn.Pixels[x * 4]);
            }
        }

        [Fact]
        public void APatternShrinksTheWholeImage()
        {
            // Repeated from a quarter in, each repeat 32 of the image's 64 columns: the whole image's first level.
            using var image = new CanvasImage(Columns(64, 4));
            var drawn = Render(64, 2, c => c.DrawImage(image, new RectF(0, 0, 64, 2), new RectF(0.25f, 0, 2, 1), sampler: SamplerKind.LinearRepeat));
            var level = image.ScaledPixels(new ImageDecodeScale.RectI(0, 0, 64, 4), 32, 2);
            var row = Enumerable.Range(0, 32).Select(i => level.Pixels[i * 4]).ToArray();
            for (var x = 0; x < 64; x++)
                Assert.Equal(Expected(row, 8 + (x + 0.5), wrap: true), drawn.Pixels[x * 4]);
        }

        [Fact]
        public void ImagesAreFilteredPremultiplied()
        {
            // Opaque white beside transparent white: premultiplied, the half way pixel is half white at half alpha
            // (straight filtering would keep it white at half alpha too, then multiply it again).
            using var image = new CanvasImage(Pixels(2, 1, 255, 255, 255, 255, 255, 255, 255, 0));
            var drawn = Render(4, 1, c => c.DrawImage(image, new RectF(0, 0, 4, 1)));
            // x = 1.5 · 2/4 = 0.75 - 0.5: weight 4 of the transparent texel: 255 · 12 / 16 = 191 for colour and alpha.
            Assert.Equal(new byte[] { 191, 191, 191, 191 }, drawn.Pixels[4..8]);
        }
    }
}
