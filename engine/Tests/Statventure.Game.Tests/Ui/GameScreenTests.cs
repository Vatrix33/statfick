using Statventure.Core;
using Statventure.Game.Icons;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Paint;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Game.Tests;

/// <summary>
/// The game's screens look as Chromium 152 shows the same markup and stylesheets (GM1-P17): every
/// <see cref="GameScreenFixture"/> at device scales 1 and 1.65, at the paint fixtures' limits (12 levels, 0.2 % of the
/// pixels), against <c>Golden/game-screen-&lt;name&gt;@&lt;scale&gt;.png</c> (Tools/Scripts/make-game-screen-references.sh),
/// the icons' boxes and the boxes a fixture names (<see cref="GameScreenFixture.Masks"/>) masked in both; drawn into the
/// program's window format (<see cref="Render"/>).
/// </summary>
[Collection("GPU")]
public class GameScreenTests(GpuFixture gpu)
{
    /// <summary>Every fixture at every scale.</summary>
    public static TheoryData<string, float> Screens
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in GameScreenFixtures.All)
                foreach (var scale in GameScreenFixtures.Scales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    /// <summary>The run writes the fixture list (all parts' fixtures) for make-game-screen-references.sh when it changed.</summary>
    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        var json = GameScreenFixtures.ListJson();
        if (!File.Exists(GameScreenFixtures.ListPath) || File.ReadAllText(GameScreenFixtures.ListPath) != json)
            File.WriteAllText(GameScreenFixtures.ListPath, json);
        Assert.Equal(GameScreenFixtures.All.Count, GameScreenFixtures.All.Select(f => f.Name).Distinct().Count());
        // Every fixture is captured as a page's own main frame, as the game's document is, so its size is whole device
        // pixels at each scale (a view is whole DIPs).
        using var list = System.Text.Json.JsonDocument.Parse(json);
        Assert.All(list.RootElement.EnumerateArray(), f => Assert.True(f.GetProperty("mainFrame").GetBoolean()));
        foreach (var fixture in GameScreenFixtures.All)
            foreach (var scale in GameScreenFixtures.Scales)
                Assert.True(IsWhole(fixture.Width * scale) && IsWhole(fixture.Height * scale), $"{fixture.Name} at {scale}: {fixture.Width} x {fixture.Height} isn't whole device pixels (use multiples of 20 px)");
    }

    static bool IsWhole(float length) => MathF.Abs(length - MathF.Round(length)) < 1e-3f;

    [Theory]
    [MemberData(nameof(Screens))]
    public void TheScreenPaintsLikeChromium(string name, float scale)
    {
        var fixture = GameScreenFixtures.All.Single(f => f.Name == name);
        using var harness = GameScreenFixtures.Build(fixture, scale);
        var width = (int)MathF.Round(fixture.Width * scale);
        var height = (int)MathF.Round(fixture.Height * scale);
        var render = Render(gpu.Device, harness, width, height);
        var golden = Golden.GoldenPath($"game-screen-{name}@{GameScreenFixtures.ScaleName(scale)}");
        // The icons are step 7's to compare (on their own sheet): their boxes are the golden's in the render; so are the
        // boxes the fixture names (a part compared on its own elsewhere).
        var masks = GameScreenFixtures.IconBoxes(harness.Document);
        masks.AddRange(GameScreenFixtures.MaskBoxes(harness.Document, [.. GameScreenFixtures.Views, .. fixture.Masks ?? []]));
        if (masks.Count > 0 && File.Exists(golden))
        {
            var reference = ImageFile.Load(golden);
            if (reference.Width == width && reference.Height == height)
                Mask(render, reference, masks);
        }
        Golden.Match($"game-screen-{name}@{GameScreenFixtures.ScaleName(scale)}", render, tolerance: 12, maxDifferentRatio: 0.002);
    }

    /// <summary>The pixels of <paramref name="boxes"/> (device pixels) taken from <paramref name="from"/> into <paramref name="into"/>.</summary>
    internal static void Mask(RgbaImage into, RgbaImage from, IEnumerable<RectF> boxes)
    {
        foreach (var box in boxes)
            for (var y = Math.Max(0, (int)MathF.Floor(box.Top)); y < Math.Min(into.Height, (int)MathF.Ceiling(box.Bottom)); y++)
                for (var x = Math.Max(0, (int)MathF.Floor(box.Left)); x < Math.Min(into.Width, (int)MathF.Ceiling(box.Right)); x++)
                    Array.Copy(from.Pixels, (y * into.Width + x) * 4, into.Pixels, (y * into.Width + x) * 4, 4);
    }

    /// <summary>
    /// The harness's document drawn offscreen at <paramref name="width"/> × <paramref name="height"/> device pixels, into an
    /// image of the format the program's window draws into (<see cref="Swapchain.PreferredFormat"/>; the start dialog drawn
    /// grey in the program while its fixture, drawn in R8G8B8A8, passed: 2026-11-15). <c>ProgramPathScreenTests</c> checks
    /// that a window's frame is this picture.
    /// </summary>
    internal static RgbaImage Render(GpuDevice device, GameUiHarness harness, int width, int height)
    {
        using var painter = new UiPainter(harness.Document);
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, width, height, Swapchain.PreferredFormat);
        var frame = device.BeginFrame();
        canvas.Begin(width, height);
        painter.Paint(canvas);
        canvas.Render(frame, target);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(width, height, target.ReadPixels());
    }

    /// <summary>
    /// A fixture's named boxes: each element its selectors match masked as its ink laid out (its border box and its shadow,
    /// before its own transform), a pixel around, in device pixels; nothing else.
    /// </summary>
    [Fact]
    public void AFixturesNamedBoxesAreTheirInkAsLaidOut()
    {
        foreach (var scale in GameScreenFixtures.Scales)
        {
            using var harness = new GameUiHarness(800, 600, scale);
            var round = harness.Document.Body.Append("div", "round");
            round.SetAttribute("style", "position: absolute; left: 30px; top: 40px; width: 100px; height: 100px; border-radius: 50%; box-shadow: 0 0 0 6px black; transform: rotate(11deg)");
            harness.Document.Body.Append("p", "other", "beside").SetAttribute("style",
                "position: absolute; left: 300px; top: 40px; width: 50px; height: 20px; margin: 0; box-shadow: 3px 5px 2px 1px black, inset 0 0 30px red");
            harness.Frame();
            var mask = Assert.Single(GameScreenFixtures.MaskBoxes(harness.Document, [".round"]));
            Assert.Equal(new RectF(24 * scale - 1, 34 * scale - 1, 112 * scale + 2, 112 * scale + 2), mask, new RectComparer());
            Assert.Empty(GameScreenFixtures.MaskBoxes(harness.Document, ["#nothing"]));
            // A shadow's reach past its offset (blur 2, spread 1, offset 3 and 5): left 0, top 0, right 6, bottom 8; an inset one adds nothing.
            var other = GameScreenFixtures.MaskBoxes(harness.Document, [".round", ".other"]);
            Assert.Equal(2, other.Count);
            Assert.Equal(RectF.FromEdges(300 * scale - 1, 40 * scale - 1, 356 * scale + 1, 68 * scale + 1), other[1], new RectComparer());
        }
    }

    sealed class RectComparer : IEqualityComparer<RectF>
    {
        public bool Equals(RectF a, RectF b) => MathF.Abs(a.X - b.X) < 0.02f && MathF.Abs(a.Y - b.Y) < 0.02f && MathF.Abs(a.Width - b.Width) < 0.02f && MathF.Abs(a.Height - b.Height) < 0.02f;
        public int GetHashCode(RectF rect) => 0;
    }

    /// <summary>
    /// Every 3D view's box is what every fixture masks besides what it names (GM2-P12, GM2 step 5): each <c>surface</c>
    /// element's border box, wherever it is and however many; nothing else of the page. (The fixture
    /// <c>view-masked-dark</c> shows the mask in a comparison: <see cref="ViewScreenFixtures"/>.)
    /// </summary>
    [Fact]
    public void EveryViewsBoxIsMasked()
    {
        Assert.Equal(["surface"], GameScreenFixtures.Views);
        foreach (var scale in GameScreenFixtures.Scales)
        {
            using var harness = new GameUiHarness(800, 600, scale);
            var panel = harness.Document.Body.Append("div");
            panel.SetAttribute("style", "position: absolute; left: 100px; top: 60px; width: 300px; height: 400px; padding: 20px");
            panel.Append("surface").SetAttribute("style", "display: block; width: 260px; height: 360px; border: 2px solid red");
            harness.Document.Body.Append("surface").SetAttribute("style", "position: absolute; left: 500px; top: 300px; display: block; width: 120px; height: 80px");
            harness.Document.Body.Append("p", text: "not a view").SetAttribute("style", "position: absolute; left: 500px; top: 40px; margin: 0");
            harness.Frame();
            var boxes = GameScreenFixtures.MaskBoxes(harness.Document, GameScreenFixtures.Views);
            Assert.Equal(2, boxes.Count);
            // Its border box (the game's sheets size boxes by their borders: 260 × 360 with the border).
            Assert.Equal(RectF.FromEdges(120 * scale - 1, 80 * scale - 1, 380 * scale + 1, 440 * scale + 1), boxes[0], new RectComparer());
            Assert.Equal(RectF.FromEdges(500 * scale - 1, 300 * scale - 1, 620 * scale + 1, 380 * scale + 1), boxes[1], new RectComparer());
        }
    }

    /// <summary>The masks' rule: an icon's characters are found on the laid-out lines and their boxes taken from the golden.</summary>
    [Fact]
    public void AnIconsBoxIsTakenFromTheGolden()
    {
        var into = new RgbaImage(4, 2, [.. Enumerable.Repeat((byte)10, 32)]);
        var from = new RgbaImage(4, 2, [.. Enumerable.Repeat((byte)200, 32)]);
        Mask(into, from, [new RectF(1, 0, 2, 1)]);
        Assert.Equal(new byte[] { 10, 200, 200, 10, 10, 10, 10, 10 }, Enumerable.Range(0, 8).Select(i => into.Pixels[i * 4]));
        // With no icon table (before step 7) nothing is masked.
        using var harness = new GameUiHarness();
        harness.Document.Body.Append("p", text: "Settings ⚙");
        harness.Frame();
        Assert.Equal(GameIcons.Table.ContainsKey('⚙'), GameScreenFixtures.IconBoxes(harness.Document).Count > 0);
    }
}
