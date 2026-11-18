using Statventure.Canvas;
using Statventure.Core;
using Statventure.Tests.Infrastructure;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Canvas;

/// <summary>
/// Where the hairlines' masks go (Canvas.StrokeRoundRect; Docs/Compositing-Port.md, "Thin rounded borders"): never into the
/// shared coverage atlas, which is cleared when it fills and so spoils the draws already made from it in that frame (the
/// old game's shop, its page behind the dialog drawn with stray blocks once the hairlines filled it), but into an atlas
/// of their own that is cleared between frames only; and a long straight side takes no room in it.
/// </summary>
[Collection("GPU")]
public class HairlineAtlasTests(GpuFixture gpu)
{
    const int Width = 400, Height = 300;

    [Fact]
    public void HairlinesKeepOutOfTheSharedAtlas()
    {
        using var canvas = new CanvasRenderer(gpu.Device);
        canvas.Begin(Width, Height);
        for (var i = 0; i < 20; i++)
            canvas.StrokeRoundRect(new RectF(10 + i * 3, 10 + i * 5, 60 + i, 40), new CornerRadii(20), 1, Color.FromHex("#85673f"));
        Assert.True(canvas.HairlineAtlas.UsedPages > 0);
        Assert.Equal(0, canvas.Atlas.UsedPages);
    }

    [Fact]
    public void TheHairlineAtlasIsClearedBetweenFramesOnly()
    {
        using var canvas = new CanvasRenderer(gpu.Device);
        canvas.Begin(Width, Height);
        var atlas = canvas.HairlineAtlas;
        var generation = atlas.Generation;
        // Circles of one size after another: distinct masks until half the atlas is used, all in one frame.
        for (var size = 20; atlas.UsedPages * 2 < atlas.MaxPages && size < 2000; size++)
            canvas.StrokeRoundRect(new RectF(0.5f, 0.5f, size, size), new CornerRadii(size), 1, Color.FromHex("#85673f"));
        Assert.True(atlas.UsedPages * 2 >= atlas.MaxPages);
        Assert.Equal(generation, atlas.Generation);
        canvas.Begin(Width, Height);
        Assert.Equal(generation + 1, atlas.Generation);
        Assert.Equal(0, atlas.UsedPages);
    }

    /// <summary>
    /// The atlas's curved tiles and the straight runs together draw the hairline's mask: a rounded rect a quarter pixel off
    /// the grid (its sides on two part-covered rows and columns), black on white, every pixel its mask's coverage.
    /// </summary>
    [Fact]
    public void TheDrawsAreTheMask()
    {
        var device = gpu.Device;
        using var canvas = new CanvasRenderer(device);
        using var target = Statventure.Graphics.GpuImage.CreateRenderTarget(device, Width, Height);
        var frame = device.BeginFrame();
        canvas.Begin(Width, Height);
        canvas.FillRect(new RectF(0, 0, Width, Height), Color.White);
        canvas.StrokeRoundRect(new RectF(20.25f, 30.25f, 300, 150), new CornerRadii(18), 1, Color.Black);
        canvas.Render(frame, target);
        device.EndFrame(frame);
        device.WaitIdle();
        var pixels = target.ReadPixels();
        Span<System.Numerics.Vector2> radii = [new(18), new(18), new(18), new(18)];
        var mask = SkiaHairline.RRect(SkRRect.Make(20.25f, 30.25f, 320.25f, 180.25f, radii).Inset(0.5f), 1)!;
        var worst = 0;
        var partial = 0;
        for (var y = 0; y < Height; y++)
            for (var x = 0; x < Width; x++)
            {
                int mx = x - mask.X, my = y - mask.Y;
                var coverage = mx >= 0 && my >= 0 && mx < mask.Width && my < mask.Height ? mask.Coverage(mx, my) : 0;
                if (coverage is > 0.05f and < 0.95f)
                    partial++;
                var expected = 255 * (1 - coverage);
                worst = Math.Max(worst, (int)MathF.Abs(pixels[(y * Width + x) * 4] - expected));
            }
        // Part-covered pixels all along the sides (two rows and two columns of them), each within a level of its mask.
        Assert.True(partial > 2 * (300 + 150), $"{partial} part-covered pixels");
        Assert.True(worst <= 1, $"a pixel {worst} levels from the mask");
    }

    [Fact]
    public void AStraightSideTakesNoRoomInTheAtlas()
    {
        using var canvas = new CanvasRenderer(gpu.Device);
        canvas.Begin(Width, Height);
        canvas.StrokeRoundRect(new RectF(10, 10, 300, 200), new CornerRadii(4), 1, Color.FromHex("#85673f"));
        var used = canvas.HairlineAtlas.UsedPages;
        canvas.Begin(Width, Height);
        canvas.StrokeRoundRect(new RectF(10, 10, 300, 200), new CornerRadii(4), 1, Color.FromHex("#85673f"));
        // The four corners' tiles: one page, and a shelf of 16 px rows at most.
        Assert.Equal(1, used);
        Assert.True(canvas.PrimitiveCount <= 4 + 8, $"{canvas.PrimitiveCount} draws for a rounded rect's hairline");
    }
}
