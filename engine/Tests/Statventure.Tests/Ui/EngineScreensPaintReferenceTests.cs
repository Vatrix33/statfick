using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using Statventure.UI;
using Statventure.UI.Dom;
using Statventure.UI.Paint;
using CanvasImage = Statventure.Canvas.CanvasImage;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Ui;

/// <summary>
/// The splash and the credits view as Chromium 152 shows the same markup and stylesheets (Golden/ui-splash*@1.png and
/// @1.65.png, ui-credits*, made by Tools/Scripts/make-scaled-paint-references.sh 1 1 engine-screen-fixtures.json and 1.65
/// 1 engine-screen-fixtures.json: under X11, CPU raster, with the harness's Lato, the old game's files, the same bytes as
/// the engine's own): the engine's own documents (<see cref="Splash"/>, <see cref="Credits"/>) painted, their image
/// read from their own data as a window's host reads it, at the paint fixtures' limits (12 levels, 0.2 % of pixels).
/// The splash in a big window (the logo at its 696 CSS px), a narrow one (scaled down to the window's width) and a
/// short one (to its height, the logo kept whole and centred in its box: object-fit); the
/// credits' top and, scrolled to the end, their foot. Chromium's page gets the logo as a data url (its harness loads
/// no files).
/// </summary>
[Collection("GPU")]
public class EngineScreensPaintReferenceTests(GpuFixture gpu)
{
    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    static readonly ProgramInfo Release = new("Statventure.Sandbox", "2026.11.8.1", null);

    const string CreditsTitle = "Statventure Sandbox";

    static readonly string LogoUrl = "data:image/png;base64," + Convert.ToBase64String(Splash.Logo);

    public static readonly PaintFixture[] All =
    [
        new("splash", 1000, 600, Splash.Css, Splash.Markup(LogoUrl)),
        new("splash-narrow", 480, 300, Splash.Css, Splash.Markup(LogoUrl)),
        new("splash-short", 800, 100, Splash.Css, Splash.Markup(LogoUrl)),
        new("credits", 720, 800, Credits.ViewCss + Credits.Css, $"""<div class="credits-view">{Credits.Markup(CreditsTitle, Release)}</div>"""),
        new("credits-end", 720, 800, Credits.ViewCss + Credits.Css, $"""<div class="credits-view" data-scroll-top="100000">{Credits.Markup(CreditsTitle, Release)}</div>"""),
    ];

    static readonly float[] Scales = [1, 1.65f];

    public static TheoryData<string, float> Fixtures
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in All)
                foreach (var scale in Scales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        // The documents' own stylesheets only (no paint fixtures' base sheet): Chromium gets what the engine shows.
        var path = Path.Combine(ReferenceDirectory, "engine-screen-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = f.Css, html = f.Html,
            scrollX = f.ScrollX, scrollY = f.ScrollY, windowFocused = f.WindowFocused,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    /// <summary>The engine's document for a fixture, laid out at <paramref name="scale"/>, scrolled as the fixture is.</summary>
    static (UiDocument Document, IDisposable Owner) Build(PaintFixture fixture, float scale)
    {
        UiDocument document;
        IDisposable owner;
        if (fixture.Name.StartsWith("splash", StringComparison.Ordinal))
        {
            var splash = new Splash();
            (document, owner) = (splash.Document, splash);
        }
        else
        {
            var credits = new Credits(CreditsTitle, Release);
            (document, owner) = (credits.Document, credits);
        }
        // The images' sizes from the document's data, before anything is laid out.
        var sizes = new Dictionary<string, Vector2?>();
        document.Layout.Context.ImageSize = url =>
        {
            if (!sizes.TryGetValue(url, out var size))
                sizes[url] = size = document.Files is { } files && files.Exists(url) && ImageFile.Load(files.ReadAllBytes(url)) is { } image ? new Vector2(image.Width, image.Height) : null;
            return size;
        };
        document.SetViewport(fixture.Width, fixture.Height);
        document.SetZoom(scale, 1);
        document.Update(0);
        if (fixture.Name == "credits-end")
            document.QuerySelector(".credits-view")!.ScrollTop = 100000;
        document.Update(0);
        return (document, owner);
    }

    /// <summary>A document painted, its images read from its own data (PNG) as UiHost reads them.</summary>
    internal static RgbaImage Render(GpuDevice device, UiDocument document, int width, int height)
    {
        var images = new Dictionary<string, CanvasImage?>();
        CanvasImage? Image(string url)
        {
            if (!images.TryGetValue(url, out var image))
                images[url] = image = document.Files is { } files && files.Exists(url) ? new CanvasImage(ImageFile.Load(files.ReadAllBytes(url))) : null;
            return image;
        }
        try
        {
            using var painter = new UiPainter(document) { ImageTexture = Image };
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
        finally
        {
            foreach (var image in images.Values)
                image?.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheScreenLooksAsInChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var (document, owner) = Build(fixture, scale);
        using (owner)
        {
            var image = Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
            Golden.Match($"ui-{name}@{Number(scale)}", image, tolerance: fixture.Tolerance, maxDifferentRatio: 0.002);
        }
    }
}
