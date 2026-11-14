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
/// the icons' boxes masked in both.
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
        // The icons are step 7's to compare (on their own sheet): their boxes are the golden's in the render.
        var masks = GameScreenFixtures.IconBoxes(harness.Document);
        if (masks.Count > 0 && File.Exists(golden))
        {
            var reference = ImageFile.Load(golden);
            if (reference.Width == width && reference.Height == height)
                Mask(render, reference, masks);
        }
        // A comparison held with the user's word checks what it can (HeldScreens; GM1 step 10).
        if (HeldScreens.Held(name, scale) is { } why)
        {
            HeldScreens.MatchWithinAPixel($"game-screen-{name}@{GameScreenFixtures.ScaleName(scale)}", render, why);
            return;
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

    /// <summary>The harness's document drawn offscreen at <paramref name="width"/> × <paramref name="height"/> device pixels.</summary>
    internal static RgbaImage Render(GpuDevice device, GameUiHarness harness, int width, int height)
    {
        using var painter = new UiPainter(harness.Document);
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
