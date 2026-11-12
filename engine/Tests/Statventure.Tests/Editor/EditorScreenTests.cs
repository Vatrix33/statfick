using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Paint;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Editor;

/// <summary>
/// The editor's screens look as Chromium 152 shows the same markup and stylesheets (M11-20, P36): every
/// <see cref="EditorFixture"/> at device scales 1 and 1.65, at the paint fixtures' limits (12 levels, 0.2 % of
/// the pixels), against <c>Golden/editor-&lt;name&gt;@&lt;scale&gt;.png</c> (Tools/Scripts/make-editor-references.sh).
/// </summary>
[Collection("GPU")]
public class EditorScreenTests(GpuFixture gpu)
{
    public static TheoryData<string, float> Screens
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in EditorFixtures.All)
                foreach (var scale in EditorFixtures.Scales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    /// <summary>The run writes the fixture list (all parts' fixtures) for make-editor-references.sh when it changed.</summary>
    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "editor-fixtures.json");
        var json = EditorFixtures.ListJson();
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.Equal(EditorFixtures.All.Count, EditorFixtures.All.Select(f => f.Name).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Screens))]
    public void TheScreenPaintsLikeChromium(string name, float scale)
    {
        var fixture = EditorFixtures.All.Single(f => f.Name == name);
        using var editor = EditorFixtures.Build(fixture, scale);
        var width = (int)MathF.Round(fixture.Width * scale);
        var height = (int)MathF.Round(fixture.Height * scale);
        Golden.Match($"editor-{name}@{EditorFixtures.ScaleName(scale)}", Render(gpu.Device, editor, width, height), tolerance: 12, maxDifferentRatio: 0.002);
    }

    /// <summary>The harness's document drawn offscreen at <paramref name="width"/> × <paramref name="height"/> device pixels (also the end-to-end flow's About picture, M12 step 8).</summary>
    internal static RgbaImage Render(GpuDevice device, EditorHarness editor, int width, int height)
    {
        using var painter = new UiPainter(editor.Document);
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, width, height);
        var frame = device.BeginFrame();
        canvas.Begin(width, height);
        painter.Paint(canvas);
        canvas.Render(frame, target);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(width, height, target.ReadPixels());
    }
}
