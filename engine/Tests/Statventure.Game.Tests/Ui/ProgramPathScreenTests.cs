using Brutal.VulkanApi;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Platform;
using Statventure.Tests.Infrastructure;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The game's screens drawn as the program draws them are the pictures the comparisons with Chromium judge (the start
/// dialog drawn grey in the program while its fixture passed, 2026-11-15; Docs/Game/Foundation.md, "How it works", "The
/// start dialog drawn grey"). Every fixture at 1 in a real window: an application in a hidden window of the fixture's
/// size, the fixture's document on the window's host (<see cref="UiHost"/>, as <c>GameUi.Load</c> makes the game's), drawn
/// into the window's swapchain image and saved by the application's own capture (<c>--capture</c>'s path): the same
/// pixels as <see cref="GameScreenTests.Render"/>, which draws into the window's format at both scales. Needs a display
/// (the suite runs in a headless compositor); in the "Game log" collection, alone (GLFW is one for the process; a hidden
/// window presents one frame).
/// </summary>
[Collection("Game log")]
public class ProgramPathScreenTests(GpuFixture gpu) : IClassFixture<GpuFixture>
{
    static bool HasDisplay => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));

    /// <summary>Every fixture, at 1 (a window's scale under the test compositor).</summary>
    public static TheoryData<string> AtOne => new(GameScreenFixtures.All.Select(f => f.Name));

    [Theory]
    [MemberData(nameof(AtOne))]
    public void TheScreenDrawsInAWindowAsInTheHarness(string name)
    {
        if (!HasDisplay)
            return;
        var fixture = GameScreenFixtures.All.Single(f => f.Name == name);
        using var harness = GameScreenFixtures.Build(fixture, 1);
        // The harness's picture first: the window's host unloads the document when it goes.
        var drawn = GameScreenTests.Render(gpu.Device, harness, fixture.Width, fixture.Height);
        using var folder = new ScratchFolder();
        using var program = new ScreenInAWindow(folder["p"], harness.Document, fixture.Width, fixture.Height, folder["frame.png"]);
        program.Run();
        var frame = ImageFile.Load(folder["frame.png"]);
        Assert.Equal((fixture.Width, fixture.Height), (frame.Width, frame.Height));
        var differing = Differing(drawn, frame);
        Assert.True(differing == 0, $"{name}: {differing} pixels of the window's frame ({program.Format}) differ from the harness's render");
    }

    /// <summary>Pixels whose colour differs at all (the capture is opaque).</summary>
    static int Differing(RgbaImage a, RgbaImage b)
    {
        var differing = 0;
        for (var i = 0; i < a.Pixels.Length; i += 4)
            if (a.Pixels[i] != b.Pixels[i] || a.Pixels[i + 1] != b.Pixels[i + 1] || a.Pixels[i + 2] != b.Pixels[i + 2])
                differing++;
        return differing;
    }

    /// <summary>A program showing one document in its window (no splash, no sound): its first frame captured, then it quits.</summary>
    sealed class ScreenInAWindow(string folder, UiDocument document, int width, int height, string capture) : Application(new ApplicationOptions
    {
        Window = new WindowOptions { Title = "Stat Fick program path test", Width = width, Height = height, Visible = false },
        Audio = false,
        GameFolder = folder,
    })
    {
        UiHost? _host;

        public VkFormat Format { get; private set; }

        protected override void Load() => _host = new UiHost(Window, Gpu, document);

        // This frame is drawn, presented and captured, and the run ends (a hidden window presents one frame). The document
        // isn't updated again: it is the fixture's as the harness left it.
        protected override void Update(in GameTime time)
        {
            CaptureNextFrame(capture);
            Quit();
        }

        protected override void Render(Frame frame, GpuImage target, in GameTime time)
        {
            Format = target.Format;
            _host!.Render(frame, target);
        }

        protected override void Unload() => _host?.Dispose();
    }
}
