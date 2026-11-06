using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Canvas;

/// <summary>
/// Tiles (Canvas.PushTile/PopTile): content drawn once into a texture of its own and repeated with a
/// repeat sampler (a background layer's pattern), drawn premultiplied (DrawImage's premultiplied flag).
/// </summary>
[Collection("GPU")]
public class CanvasTileTests(GpuFixture gpu)
{
    const int Width = 160, Height = 96;

    RgbaImage Render(float scale, Action<CanvasRenderer> draw)
    {
        var device = gpu.Device;
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, Width, Height);
        var frame = device.BeginFrame();
        canvas.Begin(Width, Height, scale);
        canvas.FillRect(new RectF(0, 0, Width / scale, Height / scale), Color.White);
        draw(canvas);
        canvas.Render(frame, target);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(Width, Height, target.ReadPixels());
    }

    /// <summary>A 10 by 8 tile (canvas units): two boxes of their own colours on a third, one rounded.</summary>
    static void TileContent(CanvasRenderer canvas, Vector2 at)
    {
        canvas.FillRect(new RectF(at.X, at.Y, 10, 8), Color.FromHex("#e8d9b8"));
        canvas.FillRect(new RectF(at.X + 1, at.Y + 1, 4, 3), Color.FromHex("#7a4a2a"));
        canvas.DrawBox(new RectF(at.X + 5, at.Y + 4, 4, 3), Brush.Solid(Color.FromHex("#2a5a8a")), new CornerRadii(1), Border.None);
    }

    static (int Different, int Worst) Compare(RgbaImage a, RgbaImage b, int tolerance)
    {
        var different = 0;
        var worst = 0;
        for (var i = 0; i < a.Pixels.Length; i += 4)
        {
            var d = 0;
            for (var c = 0; c < 4; c++)
                d = Math.Max(d, Math.Abs(a.Pixels[i + c] - b.Pixels[i + c]));
            worst = Math.Max(worst, d);
            if (d > tolerance)
                different++;
        }
        return (different, worst);
    }

    /// <summary>
    /// Repeated from one texture, the tile is the same as its content drawn at each place, where the
    /// tiles fall on whole pixels: at a phase of whole texels into the tile and at a canvas scale of 2.
    /// </summary>
    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void ATileRepeatedIsItsContentDrawnAtEachPlace(float scale)
    {
        // The area starts 3 units into a tile (a whole number of texels at either scale).
        var area = new RectF(12, 9, 52, 30);
        var origin = new Vector2(area.X - 3, area.Y - 3);
        var repeated = Render(scale, canvas =>
        {
            var texels = (int)(10 * scale);
            canvas.PushTile(texels, (int)(8 * scale));
            canvas.PushTransform(Matrix3x2.CreateScale(scale));
            TileContent(canvas, Vector2.Zero);
            canvas.PopTransform();
            var tile = canvas.PopTile();
            var uv = new RectF((area.X - origin.X) / 10, (area.Y - origin.Y) / 8, area.Width / 10, area.Height / 8);
            canvas.DrawImage(tile, area, uv, sampler: SamplerKind.LinearRepeat, premultiplied: true);
        });
        var direct = Render(scale, canvas =>
        {
            canvas.PushClip(area);
            for (var y = origin.Y; y < area.Bottom; y += 8)
                for (var x = origin.X; x < area.Right; x += 10)
                    TileContent(canvas, new Vector2(x, y));
            canvas.PopClip();
        });
        var (different, worst) = Compare(repeated, direct, 1);
        Assert.True(different == 0, $"{different} pixels differ (worst {worst})");
    }

    /// <summary>
    /// A tile repeated at fractional places (and under a scale that leaves its texels off the pixel
    /// grid) has no seams: a tile of one colour repeated over an area is that colour over the area,
    /// antialiased at the area's own edges only, as one box of it.
    /// </summary>
    [Theory]
    [InlineData(1f, 0f)]
    [InlineData(1.65f, 0f)]
    [InlineData(1f, 0.4f)]
    public void ATileRepeatedHasNoSeams(float scale, float scaleX)
    {
        var area = new RectF(12.3f, 9.6f, 52.7f, 30.45f);
        var transform = scaleX > 0 ? Matrix3x2.CreateScale(scaleX, 1) * Matrix3x2.CreateTranslation(20.3f, 0) : Matrix3x2.Identity;
        var color = Color.FromHex("#2a7a4a");
        var repeated = Render(scale, canvas =>
        {
            canvas.PushTile(7, 5);
            // (A tile's draws are in its pixels.)
            canvas.FillRect(new RectF(0, 0, 7, 5), color);
            var tile = canvas.PopTile();
            canvas.PushTransform(transform);
            canvas.DrawImage(tile, area, new RectF(0.37f, 0.61f, area.Width / 4.3f, area.Height / 3.1f), sampler: SamplerKind.LinearRepeat, premultiplied: true);
            canvas.PopTransform();
        });
        var direct = Render(scale, canvas =>
        {
            canvas.PushTransform(transform);
            canvas.FillRect(area, color);
            canvas.PopTransform();
        });
        var (different, worst) = Compare(repeated, direct, 1);
        Assert.True(different == 0, $"{different} pixels differ (worst {worst})");
    }

    /// <summary>A half-transparent tile is premultiplied once: drawn from its texture it is as the same colour drawn directly.</summary>
    [Fact]
    public void AHalfTransparentTileIsPremultipliedOnce()
    {
        var color = new Color(0.8f, 0.3f, 0.2f, 0.5f);
        var area = new RectF(10, 10, 60, 40);
        var repeated = Render(1, canvas =>
        {
            canvas.PushTile(6, 6);
            canvas.FillRect(new RectF(0, 0, 6, 6), color);
            var tile = canvas.PopTile();
            canvas.DrawImage(tile, area, new RectF(0, 0, 10, 40 / 6f), sampler: SamplerKind.LinearRepeat, premultiplied: true);
        });
        var direct = Render(1, canvas => canvas.FillRect(area, color));
        var (different, worst) = Compare(repeated, direct, 1);
        Assert.True(different == 0, $"{different} pixels differ (worst {worst})");
    }

    /// <summary>
    /// A tile is filtered and blended once, where it is drawn: its content goes into the tile without the
    /// colour filter and blend mode around it (multiplying onto the empty tile would leave nothing, and a
    /// brightness filter applied twice darkens twice).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ATileIsFilteredAndBlendedOnceWhereItIsDrawn(bool multiply)
    {
        var color = Color.FromHex("#c8643c");
        var area = new RectF(10, 10, 60, 40);
        void Around(CanvasRenderer canvas, Action draw)
        {
            canvas.FillRect(new RectF(0, 0, Width, Height), Color.FromHex("#9ab4d2"));
            canvas.ColorFilter = ColorMatrix.For(new Filter(FilterKind.Brightness, 0.6f));
            if (multiply)
                canvas.Blend = BlendMode.Multiply;
            draw();
            canvas.Blend = BlendMode.Premultiplied;
            canvas.ColorFilter = null;
        }
        var repeated = Render(1, canvas => Around(canvas, () =>
        {
            canvas.PushTile(6, 6);
            canvas.FillRect(new RectF(0, 0, 6, 6), color);
            var tile = canvas.PopTile();
            canvas.DrawImage(tile, area, new RectF(0, 0, 10, 40 / 6f), sampler: SamplerKind.LinearRepeat, premultiplied: true);
        }));
        var direct = Render(1, canvas => Around(canvas, () => canvas.FillRect(area, color)));
        var (different, worst) = Compare(repeated, direct, 1);
        Assert.True(different == 0, $"{different} pixels differ (worst {worst})");
    }

    /// <summary>A tile's texture is exactly its size, and the next recording's tile of that size takes it again from the pool.</summary>
    [Fact]
    public void TileTexturesAreExactAndRecycled()
    {
        var device = gpu.Device;
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, 64, 64);
        GpuImage Frame()
        {
            var frame = device.BeginFrame();
            canvas.Begin(64, 64);
            canvas.PushTile(17, 13);
            canvas.FillRect(new RectF(0, 0, 17, 13), Color.FromHex("#336699"));
            var tile = canvas.PopTile();
            canvas.DrawImage(tile, new RectF(0, 0, 64, 64), new RectF(0, 0, 64 / 17f, 64 / 13f), sampler: SamplerKind.NearestRepeat, premultiplied: true);
            canvas.Render(frame, target);
            device.EndFrame(frame);
            device.WaitIdle();
            return tile;
        }
        var first = Frame();
        Assert.Equal((17, 13), (first.Width, first.Height));
        var second = Frame();
        Assert.Same(first, second);
    }
}
