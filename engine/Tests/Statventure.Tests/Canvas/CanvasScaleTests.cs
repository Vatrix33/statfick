using System.Diagnostics;
using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using Xunit.Abstractions;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Canvas;

[Collection("GPU")]
public class CanvasScaleTests(GpuFixture gpu, ITestOutputHelper output)
{
    RgbaImage Render(ReferenceScene scene, float scale)
    {
        var device = gpu.Device;
        int w = (int)(scene.Width * scale), h = (int)(scene.Height * scale);
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, w, h);
        var frame = device.BeginFrame();
        canvas.Begin(w, h, scale);
        ReferenceScenes.Paint(scene, canvas);
        canvas.Render(frame, target);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(w, h, target.ReadPixels());
    }

    [Theory]
    [InlineData("boxes")]
    [InlineData("shadows")]
    [InlineData("effects")]
    [InlineData("paths")]
    public void DoubleScaleRendersTheSamePictureSharper(string name)
    {
        var scene = ReferenceScenes.All.Single(s => s.Name == name);
        var normal = Render(scene, 1f);
        var doubled = Render(scene, 2f);
        if (Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(Golden.ActualPath("scale2-" + name), doubled);
        // Average 2x2 blocks of the double-scale render back down.
        var reduced = new RgbaImage(normal.Width, normal.Height);
        for (var y = 0; y < normal.Height; y++)
            for (var x = 0; x < normal.Width; x++)
                for (var c = 0; c < 4; c++)
                {
                    var sum = 0;
                    for (var dy = 0; dy < 2; dy++)
                        for (var dx = 0; dx < 2; dx++)
                            sum += doubled.Pixels[((y * 2 + dy) * doubled.Width + x * 2 + dx) * 4 + c];
                    reduced.Pixels[(y * normal.Width + x) * 4 + c] = (byte)((sum + 2) / 4);
                }
        var different = 0;
        for (var i = 0; i < normal.Pixels.Length; i += 4)
            if (Math.Abs(normal.Pixels[i] - reduced.Pixels[i]) > 24 || Math.Abs(normal.Pixels[i + 1] - reduced.Pixels[i + 1]) > 24 || Math.Abs(normal.Pixels[i + 2] - reduced.Pixels[i + 2]) > 24)
                different++;
        Assert.True(different < normal.Width * normal.Height * 0.01, $"{different} pixels differ between 1x and reduced 2x");
    }

    [Fact]
    public void TwentyThousandPrimitivesRecordAndRenderQuickly()
    {
        var device = gpu.Device;
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, 1920, 1080);
        var random = new Random(7);
        var brush = Brush.CssLinear(new Vector2(40, 20), 90, new GradientStop(0, Color.FromHex("#c9533a")), new GradientStop(1, Color.FromHex("#e8c04a")));
        double recordMs = 0, totalMs = 0;
        for (var round = 0; round < 5; round++)
        {
            var clock = Stopwatch.StartNew();
            var frame = device.BeginFrame();
            canvas.Begin(1920, 1080);
            for (var i = 0; i < 20_000; i++)
            {
                var rect = new RectF(random.Next(1900), random.Next(1060), 40, 20);
                if (i % 3 == 0)
                    canvas.DrawBox(rect, brush, new CornerRadii(6), new Border(1, Color.Black));
                else if (i % 3 == 1)
                    canvas.DrawBoxShadow(rect, new CornerRadii(6), new BoxShadow(new Vector2(0, 2), 6, 0, new Color(0, 0, 0, 0.3f)));
                else
                    canvas.DrawBox(rect, Brush.Solid(new Color(0.2f, 0.3f, 0.4f, 0.8f)), CornerRadii.Zero, Border.None);
            }
            recordMs = clock.Elapsed.TotalMilliseconds;
            canvas.Render(frame, target);
            device.EndFrame(frame);
            device.WaitIdle();
            totalMs = clock.Elapsed.TotalMilliseconds;
        }
        output.WriteLine($"20000 primitives: record {recordMs:0.0} ms, record+render+wait {totalMs:0.0} ms");
        Assert.True(recordMs < 30, $"recording took {recordMs:0.0} ms");
        // A CPU Vulkan (a cloud machine without a GPU) renders far slower: only the recording is timed there.
        if (!device.IsSoftware)
            Assert.True(totalMs < 100, $"the frame took {totalMs:0.0} ms");
    }
}
