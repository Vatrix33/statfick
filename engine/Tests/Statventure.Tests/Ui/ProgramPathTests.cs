using Brutal.VulkanApi;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Platform;
using Statventure.Tests.Core;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Storage;
using Statventure.Text;
using Statventure.UI;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using CanvasRenderer = Statventure.Canvas.Canvas;
using LayerOptions = Statventure.Canvas.LayerOptions;

namespace Statventure.Tests.Ui;

/// <summary>
/// A document drawn as a program draws it (GM1's start screen drawn grey, 2026-11-15; Docs/Compositing-Port.md, "The
/// target's format"): a real application in a hidden window, its document on the window's host (<see cref="UiHost"/>),
/// drawn into the window's swapchain image and saved by the application's own capture, against the same document drawn
/// as the tests' harnesses draw it (an RGBA image of its own). The program's target is the swapchain's (B8G8R8A8 on the
/// reference PC, on the GPU and on lavapipe), the harnesses' the canvas's own layer format, so a canvas rule that holds
/// in one and not the other shows here: a layer that starts from its backdrop (an opacity layer keeping LCD text) and a
/// backdrop filter read the target's pixels, and a byte copy from a BGRA target swapped their red and blue (the dialog
/// around the start screen's greyed Continue turned the average of itself and its swapped copy, grey). Needs a display
/// (the suite runs in a headless compositor); in the "Window" collection, alone (GLFW is one for the process, and a
/// hidden window presents one frame).
/// </summary>
[Collection("Window")]
public class ProgramPathTests(GpuFixture gpu) : IClassFixture<GpuFixture>
{
    const int Width = 320, Height = 200;

    static readonly Lazy<FontLibrary> Fonts = new(() =>
    {
        var fonts = new FontLibrary();
        fonts.AddFile(Path.Combine(AppContext.BaseDirectory, "Data", "Fonts", "Lato-Regular.ttf"), "Lato", 400);
        return fonts;
    });

    /// <summary>The start dialog in small: a coloured box with a half-transparent (greyed) button holding text inside.</summary>
    const string DialogCss = """
        html { background: #120d0a; }
        .dialog { margin: 20px; padding: 30px; overflow: hidden; border-radius: 6px; background: rgb(160, 90, 30); }
        button { display: block; width: 200px; padding: 10px; border: 1px solid #c49a56; color: #f3e8cc; background: #2a1d14; font: 16px Lato; }
        button:disabled { opacity: .5; }
        """;
    const string DialogHtml = """<div class="dialog"><button disabled>Continue</button></div>""";

    /// <summary>A window's darkened ground as the game's windows have it: a backdrop blur over the coloured page.</summary>
    const string BlurCss = """
        html { background: rgb(160, 90, 30); }
        p { margin: 20px; font: 16px Lato; color: #f3e8cc; }
        .ground { position: fixed; inset: 0; background: rgba(0, 0, 0, .25); backdrop-filter: blur(2px); }
        """;
    const string BlurHtml = """<p>Behind the window</p><div class="ground"></div>""";

    [Fact]
    public void ADialogAroundAGreyedButtonKeepsItsColourInTheWindow()
    {
        if (!TestApplication.HasDisplay)
            return;
        var (window, format) = DrawnByTheProgram(DialogCss, DialogHtml);
        var harness = DrawnByTheHarness(gpu.Device, DialogCss, DialogHtml);
        // The dialog's ground beside the button, and below it: its own colour (not the average of it and its red and blue
        // swapped, (95, 90, 95)).
        Assert.Equal((160, 90, 30), Pixel(window, 30, 30));
        Assert.Equal((160, 90, 30), Pixel(window, 160, 105));
        Assert.Equal((160, 90, 30), Pixel(harness, 160, 105));
        AssertSame(harness, window, format);
    }

    [Fact]
    public void AWindowsBlurredGroundKeepsTheColoursUnderItInTheWindow()
    {
        if (!TestApplication.HasDisplay)
            return;
        var (window, format) = DrawnByTheProgram(BlurCss, BlurHtml);
        var harness = DrawnByTheHarness(gpu.Device, BlurCss, BlurHtml);
        // The page's colour blurred (it stays itself away from the text) under a quarter of black.
        Assert.Equal((120, 67, 22), Pixel(harness, 300, 180));
        Assert.Equal((120, 67, 22), Pixel(window, 300, 180));
        AssertSame(harness, window, format);
    }

    /// <summary>The program's frame and the harness's render the same, pixel for pixel.</summary>
    static void AssertSame(RgbaImage harness, RgbaImage window, VkFormat format)
    {
        Assert.Equal((harness.Width, harness.Height), (window.Width, window.Height));
        var differing = 0;
        for (var i = 0; i < harness.Pixels.Length; i += 4)
            if (harness.Pixels[i] != window.Pixels[i] || harness.Pixels[i + 1] != window.Pixels[i + 1] || harness.Pixels[i + 2] != window.Pixels[i + 2])
                differing++;
        Assert.True(differing == 0, $"{differing} pixels differ between the window ({format}) and the harness");
    }

    static (int R, int G, int B) Pixel(RgbaImage image, int x, int y)
    {
        var i = (y * image.Width + x) * 4;
        return (image.Pixels[i], image.Pixels[i + 1], image.Pixels[i + 2]);
    }

    static UiDocument Document(string css, string html)
    {
        var document = new UiDocument(Fonts.Value);
        document.AddStylesheet(LayoutFixtures.BaseCss + css, "program path");
        Markup.SetInnerHtml(document.Body, html);
        return document;
    }

    /// <summary>The document drawn as the tests' harnesses draw a page: offscreen, into an RGBA image of its own.</summary>
    static RgbaImage DrawnByTheHarness(GpuDevice device, string css, string html)
    {
        var document = Document(css, html);
        document.SetViewport(Width, Height);
        document.SetZoom(1);
        document.Update(0);
        return PaintReferenceTests.Render(device, document, Width, Height);
    }

    /// <summary>
    /// The document drawn as a program draws its window: <see cref="Application.Run"/> in a hidden window of
    /// <see cref="Width"/> × <see cref="Height"/>, its first frame drawn by the window's host into the swapchain's image and
    /// saved by <see cref="Application.CaptureNextFrame"/>; with the swapchain's format.
    /// </summary>
    static (RgbaImage Image, VkFormat Format) DrawnByTheProgram(string css, string html)
    {
        using var folder = new StorageFolder();
        var capture = folder["frame.png"];
        using var program = new OnePage(folder["player"], Document(css, html), capture);
        program.Run();
        Assert.True(File.Exists(capture), "the program's first frame was captured");
        var image = ImageFile.Load(capture);
        Assert.Equal((Width, Height), (image.Width, image.Height));
        return (image, program.Format);
    }

    /// <summary>A program showing one document in its window: its first frame captured, then it quits.</summary>
    sealed class OnePage(string folder, UiDocument document, string capture) : Application(new ApplicationOptions
    {
        Window = new WindowOptions { Title = "Statventure program path test", Width = Width, Height = Height, Visible = false },
        Audio = false,
        GameFolder = folder,
    })
    {
        UiHost? _host;

        public VkFormat Format { get; private set; }

        protected override void Load() => _host = new UiHost(Window, Gpu, document);

        protected override void Update(in GameTime time)
        {
            _host!.Update(time.Total);
            // This frame is drawn, presented and captured, and the run ends (a hidden window presents one frame).
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

/// <summary>
/// The canvas draws the same pixels into a target of any 8-bit format (2026-11-15): a window's swapchain image is
/// B8G8R8A8 where the canvas's layers are R8G8B8A8, and what a layer reads from its target (Skia's kInitWithPrevious, a
/// backdrop filter's input) is the target's colours, converted, not its bytes (SkCanvas::internalSaveLayer draws the
/// prior device into the layer). The same drawing into an R8G8B8A8 and a B8G8R8A8 image, compared pixel for pixel.
/// </summary>
[Collection("GPU")]
public class TargetFormatTests(GpuFixture gpu)
{
    const int Size = 64;
    static readonly Color Ground = Color.FromBytes(160, 90, 30);
    static readonly Color Line = Color.FromBytes(240, 240, 240);

    [Fact]
    public void ALayerStartedFromItsBackdropHoldsTheTargetsColours()
    {
        void Draw(CanvasRenderer canvas)
        {
            // Two grounds and a line a pixel wide, so a layer's backdrop taken from the wrong place or stretched shows.
            canvas.FillRect(new RectF(0, 0, Size, Size), Ground);
            canvas.FillRect(new RectF(0, 0, Size / 2, Size), Color.FromBytes(30, 150, 220));
            canvas.FillRect(new RectF(44, 36, 1, 10), Line);
            // A layer over the whole canvas (as a stacking context's opacity gives one), a square in it at the bottom left.
            canvas.PushLayer(new LayerOptions { Opacity = 0.5f, StartsFromBackdrop = true });
            canvas.FillRect(new RectF(4, 52, 8, 8), Color.FromBytes(40, 60, 200));
            canvas.PopLayer();
            // A layer of bounds of its own across the two grounds, a square in it on the right.
            canvas.PushLayer(new LayerOptions { Opacity = 0.5f, StartsFromBackdrop = true, Bounds = new RectF(16, 8, 40, 40) });
            canvas.FillRect(new RectF(40, 20, 10, 10), Color.FromBytes(40, 60, 200));
            canvas.PopLayer();
        }
        var rgba = Drawn(VkFormat.R8G8B8A8UNorm, Draw);
        var bgra = Drawn(VkFormat.B8G8R8A8UNorm, Draw);
        // Outside the drawn squares the layers hold their backdrop: half of it over itself is itself.
        Assert.Equal((30, 150, 220), Pixel(bgra, 5, 5));
        Assert.Equal((160, 90, 30), Pixel(bgra, 60, 60));
        Assert.Equal((30, 150, 220), Pixel(bgra, 20, 30));
        Assert.Equal((160, 90, 30), Pixel(bgra, 52, 40));
        Assert.Equal((35, 105, 210), Pixel(bgra, 8, 56));
        Assert.Equal((100, 75, 115), Pixel(bgra, 45, 25));
        Assert.Equal((240, 240, 240), Pixel(bgra, 44, 40));
        Assert.Equal(rgba.Pixels, bgra.Pixels);
    }

    [Fact]
    public void ABackdropBlurReadsTheTargetsColours()
    {
        void Draw(CanvasRenderer canvas)
        {
            canvas.FillRect(new RectF(0, 0, Size, Size), Ground);
            canvas.FillRect(new RectF(0, 0, Size / 2, Size), Color.FromBytes(30, 150, 220));
            // A line a pixel wide, far from the points read below (more than three of the blur's deviations).
            canvas.FillRect(new RectF(44, 46, 1, 10), Line);
            canvas.DrawBackdropBlur(new RectF(8, 8, 48, 48), default, 3);
        }
        var rgba = Drawn(VkFormat.R8G8B8A8UNorm, Draw);
        var bgra = Drawn(VkFormat.B8G8R8A8UNorm, Draw);
        // Away from the edge between the two colours the blur keeps each.
        Assert.Equal((30, 150, 220), Pixel(bgra, 12, 32));
        Assert.Equal((160, 90, 30), Pixel(bgra, 52, 32));
        Assert.Equal(rgba.Pixels, bgra.Pixels);
    }

    RgbaImage Drawn(VkFormat format, Action<CanvasRenderer> draw)
    {
        using var canvas = new CanvasRenderer(gpu.Device);
        using var target = GpuImage.CreateRenderTarget(gpu.Device, Size, Size, format);
        var frame = gpu.Device.BeginFrame();
        canvas.Begin(Size, Size);
        draw(canvas);
        canvas.Render(frame, target);
        gpu.Device.EndFrame(frame);
        gpu.Device.WaitIdle();
        // ReadPixels gives RGBA order whatever the format.
        return new RgbaImage(Size, Size, target.ReadPixels());
    }

    static (int R, int G, int B) Pixel(RgbaImage image, int x, int y)
    {
        var i = (y * image.Width + x) * 4;
        return (image.Pixels[i], image.Pixels[i + 1], image.Pixels[i + 2]);
    }
}
