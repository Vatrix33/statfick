using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Canvas;

/// <summary>
/// The canvas must paint like the current game's browser: each reference scene is rendered by the
/// canvas and compared with the browser's rendering of the same CSS (Golden/css-*.png).
/// </summary>
[Collection("GPU")]
public class CanvasReferenceTests(GpuFixture gpu)
{
    public static TheoryData<string> Scenes => new(ReferenceScenes.All.Select(scene => scene.Name));

    [Fact]
    public void TheBrowserScriptIsUpToDate()
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference", "reference-scenes.js");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var script = ReferenceScenes.ExportScript();
        if (!File.Exists(path) || File.ReadAllText(path) != script)
            File.WriteAllText(path, script);
        Assert.Contains("statventureReferences", script);
    }

    [Theory]
    [MemberData(nameof(Scenes))]
    public void TheCanvasPaintsLikeTheBrowser(string name)
    {
        var scene = ReferenceScenes.All.Single(s => s.Name == name);
        var image = Render(gpu.Device, scene);
        // Text antialiasing differs from Chromium's by a few levels on glyph edges; a wrong line
        // break or shifted run would still fail by far.
        if (name == "text")
            Golden.Match("css-" + name, image, tolerance: 40, maxDifferentRatio: 0.005);
        else
            Golden.Match("css-" + name, image, tolerance: 12, maxDifferentRatio: 0.01);
    }

    internal static RgbaImage Render(GpuDevice device, ReferenceScene scene)
    {
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, scene.Width, scene.Height);
        var frame = device.BeginFrame();
        canvas.Begin(scene.Width, scene.Height);
        ReferenceScenes.Paint(scene, canvas);
        canvas.Render(frame, target);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(scene.Width, scene.Height, target.ReadPixels());
    }
}
