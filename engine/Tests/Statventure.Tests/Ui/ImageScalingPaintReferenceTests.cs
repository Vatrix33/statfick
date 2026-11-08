using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using CanvasImage = Statventure.Canvas.CanvasImage;
using ImageDecodeScale = Statventure.Canvas.ImageDecodeScale;
using SkiaMipmap = Statventure.Canvas.SkiaMipmap;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using Statventure.UI.Paint;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Ui;

/// <summary>
/// Raster images drawn smaller than they are, as Chromium 152 draws them (Golden/ui-image-*@1.png and @1.65.png, made
/// by Tools/Scripts/make-scaled-paint-references.sh 1 1 image-fixtures.json and 1.65 1 image-fixtures.json: under X11,
/// CPU raster): cc's software decode cache draws a copy scaled down to the mip level above the size the image lands at
/// whenever that is half its size or less (<see cref="ImageDecodeScale"/>), made with Skia's mipmaps
/// (<see cref="SkiaMipmap"/>); other draws sample the image itself, all bilinearly and premultiplied. The images
/// (Data/Images, made by make-images.py there) are one-pixel checks, two-pixel stripes, fine rings, yellow beside
/// transparent white and translucent lines, which tell a mip level from plain bilinear sampling and premultiplied
/// filtering from straight. Shrunk 1.5, 2, 2.7, 3.2, 4 and 6 times, by a transform, in a rounded box, by object-fit
/// (contain; and cover, which draws only the part its box shows, ImagePainter::PaintIntoRect), as a single background
/// tile and as a repeated one; the odd-sized image (201 × 101) 2, 3.3 and 4.3 times, where cc's mip sizes round up
/// and Skia's down. The draw samples as Skia's legacy raster shader does (weights in sixteenths, truncated: without
/// it the 1.5 times image alone had 239 pixels beyond 12 levels at 1×, and image-shrink 0.3 % of its pixels at 1.65).
/// Each draw has an image of its own (a data url with its own parameter): the decode cache scales a second draw of
/// one image from a copy it already holds (<c>FindCachedCandidate</c>), in whatever order the tiles are rastered.
/// </summary>
[Collection("GPU")]
public class ImageScalingPaintReferenceTests(GpuFixture gpu)
{
    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    static string ImagesDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Data", "Images");

    static readonly Lazy<string> Even = new(() => Convert.ToBase64String(File.ReadAllBytes(Path.Combine(ImagesDirectory, "scaling-even.png"))));
    static readonly Lazy<string> Odd = new(() => Convert.ToBase64String(File.ReadAllBytes(Path.Combine(ImagesDirectory, "scaling-odd.png"))));

    /// <summary>A data url of the image, made a resource of its own by <paramref name="copy"/>.</summary>
    static string Url(string base64, string copy) => $"data:image/png;copy={copy};base64,{base64}";

    const string Css = """
        body { background: #2b2d33; }
        img, div { position: absolute; display: block; }
        """;

    /// <summary>The image elements: the even image 320 × 160 and the odd one 201 × 101 at several widths (heights by their ratio).</summary>
    static PaintFixture Elements()
    {
        var html = new StringBuilder();
        void Img(string base64, string copy, string style) => html.Append($"""<img src="{Url(base64, copy)}" style="{style}">""");
        Img(Even.Value, "a", "left: 10px; top: 10px; width: 213px");
        Img(Even.Value, "b", "left: 240px; top: 10px; width: 160px");
        Img(Even.Value, "c", "left: 420px; top: 10px; width: 118px");
        Img(Even.Value, "d", "left: 560px; top: 10px; width: 80px");
        Img(Even.Value, "e", "left: 660px; top: 10px; width: 53px");
        Img(Even.Value, "f", "left: 10px; top: 170px; width: 320px; transform: scale(0.3); transform-origin: 0 0");
        Img(Even.Value, "g", "left: 130px; top: 170px; width: 100px; border-radius: 12px");
        Img(Even.Value, "h", "left: 250px; top: 170px; width: 100px; height: 100px; object-fit: contain; background: #44464d");
        Img(Odd.Value, "i", "left: 380px; top: 170px; width: 100px");
        Img(Odd.Value, "j", "left: 500px; top: 170px; width: 61px");
        Img(Odd.Value, "k", "left: 580px; top: 170px; width: 47px");
        // Cover cuts the part its box shows, from column 33.7 of 320, rounded out to 33: an odd start, so the decode's copy of
        // that part (its first level) pairs other columns than a copy of the whole image would (about 1,400 pixels apart).
        Img(Even.Value, "p", "left: 380px; top: 250px; width: 120px; height: 76px; object-fit: cover");
        return new PaintFixture("image-shrink", 740, 340, Css, html.ToString());
    }

    /// <summary>Background images: a single tile shrunk 5 and 3 times (one rounded), and a pattern of tiles shrunk 8 times.</summary>
    static PaintFixture Backgrounds()
    {
        string Box(string base64, string copy, string style) => $"""<div style="background-image: url('{Url(base64, copy)}'); {style}"></div>""";
        var html = Box(Even.Value, "l", "left: 10px; top: 10px; width: 64px; height: 32px; background-size: 64px 32px; background-repeat: no-repeat")
            + Box(Even.Value, "m", "left: 100px; top: 10px; width: 107px; height: 54px; background-size: 107px 53.5px; background-repeat: no-repeat; border-radius: 10px")
            + Box(Even.Value, "n", "left: 230px; top: 10px; width: 200px; height: 60px; background-size: 40px 20px; background-repeat: repeat")
            + Box(Odd.Value, "o", "left: 450px; top: 10px; width: 67px; height: 34px; background-size: 67px 33.67px; background-repeat: no-repeat");
        return new PaintFixture("image-shrink-backgrounds", 540, 100, Css, html);
    }

    public static readonly PaintFixture[] All = [Elements(), Backgrounds()];

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
        var path = Path.Combine(ReferenceDirectory, "image-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = LayoutFixtures.BaseCss + f.Css, html = f.Html,
            scrollX = f.ScrollX, scrollY = f.ScrollY, windowFocused = f.WindowFocused,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    /// <summary>A fixture painted by the engine, its data urls decoded (PNG) as the document's images.</summary>
    internal static RgbaImage Render(GpuDevice device, PaintFixture fixture, float scale)
    {
        var images = new Dictionary<string, CanvasImage>();
        try
        {
            CanvasImage? Image(string url)
            {
                if (images.TryGetValue(url, out var known))
                    return known;
                var comma = url.IndexOf(";base64,", StringComparison.Ordinal);
                if (!url.StartsWith("data:image/png", StringComparison.Ordinal) || comma < 0)
                    return null;
                var image = new CanvasImage(ImageFile.Load(Convert.FromBase64String(url[(comma + 8)..])));
                images[url] = image;
                return image;
            }
            var document = PaintReferenceTests.Build(fixture, scale, prepare: d =>
                d.Layout.Context.ImageSize = url => Image(url) is { } image ? new Vector2(image.Width, image.Height) : null);
            var width = (int)MathF.Round(fixture.Width * scale);
            var height = (int)MathF.Round(fixture.Height * scale);
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
                image.Dispose();
        }
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void ShrunkImagesLookAsInChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        Golden.Match($"ui-{name}@{Number(scale)}", Render(gpu.Device, fixture, scale), tolerance: fixture.Tolerance, maxDifferentRatio: 0.002);
    }
}
