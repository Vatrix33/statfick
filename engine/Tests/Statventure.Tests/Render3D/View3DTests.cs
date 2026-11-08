using System.Numerics;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Canvas;
using Statventure.UI;
using Statventure.UI.Dom;
using Statventure.UI.Paint;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Render3D;

/// <summary>3D views in the UI (Docs/Renderer-3D.md, "3D views in the UI"): when they render, their pictures, surface elements and picking.</summary>
[Collection("GPU")]
public class View3DTests(GpuFixture gpu)
{
    static readonly Scene3D Shapes = Scenes3D.All.Single(s => s.Name == "cel-shapes");

    /// <summary>The cel-shapes scene in a view: its model drawn with <paramref name="owner"/>, its camera, sun, style and background.</summary>
    (View3D View, Model Model, List<DrawItem> Items) ShapesView(object? owner = null)
    {
        var model = Model.Load(gpu.Device, Path.Combine(Render3DReferenceTests.ModelDirectory, Shapes.Models[0].File));
        var items = new List<DrawItem>();
        var placed = Shapes.Models[0];
        model.Draw(items, placed.Model, new Palette(Shapes.Palette, Shapes.Shadows), Shapes.Style, placed.Scenery, placed.PoseOf(model.Data), owner);
        var view = new View3D(gpu.Device)
        {
            Items = items, Camera = Shapes.Camera.ToCamera(), Sun = Shapes.Sun ?? default, Style = Shapes.Style, Background = Colors.LinearFromHex(Shapes.Background),
        };
        return (view, model, items);
    }

    bool Update(View3D view, int width, int height, float scale = 1)
    {
        var frame = gpu.Device.BeginFrame();
        var rendered = view.Update(frame, width, height, scale);
        gpu.Device.EndFrame(frame);
        gpu.Device.WaitIdle();
        return rendered;
    }

    [Fact]
    public void AViewRendersOnlyWhenItChanges()
    {
        var (view, model, items) = ShapesView();
        using var _ = view;
        using var __ = model;
        Assert.True(Update(view, 200, 120));
        Assert.False(Update(view, 200, 120));
        // The camera's settings, changed in place.
        view.Camera.Position += new Vector3(0.1f, 0, 0);
        Assert.True(Update(view, 200, 120));
        Assert.False(Update(view, 200, 120));
        Assert.True(Update(view, 220, 120));
        Assert.True(Update(view, 220, 120, 2));
        view.Items = items;
        Assert.True(Update(view, 220, 120, 2));
        view.Invalidate();
        Assert.True(Update(view, 220, 120, 2));
        Assert.False(Update(view, 220, 120, 2));
        Assert.Equal(6, view.RenderCount);
    }

    [Fact]
    public void AnOpaqueViewsPictureIsItsSceneRendered()
    {
        var (view, model, _) = ShapesView();
        using var _v = view;
        using var _m = model;
        Update(view, Shapes.Width, Shapes.Height);
        var picture = view.Picture!.ReadPixels();
        var direct = new Render3DReferenceTests(gpu).Render(Shapes);
        Assert.Equal(direct.Pixels, picture);
    }

    [Fact]
    public void ATransparentViewLeavesItsBackgroundClear()
    {
        var (view, model, _) = ShapesView();
        using var _v = view;
        using var _m = model;
        // Without ink lines, so the silhouettes' edges are the models' own colours.
        view.Style = Shapes.Style with { OutlineWidth = 0, InnerWidth = 0 };
        Update(view, Shapes.Width, Shapes.Height);
        var opaque = view.Picture!.ReadPixels();
        view.Background = null;
        Update(view, Shapes.Width, Shapes.Height);
        var clear = view.Picture!.ReadPixels();
        int w = Shapes.Width, h = Shapes.Height, inside = 0;
        var background = Colors.FromHex(Shapes.Background) * 255;
        for (var y = 1; y < h - 1; y++)
            for (var x = 1; x < w - 1; x++)
            {
                var i = (y * w + x) * 4;
                var uniform = true;
                for (var dy = -1; dy <= 1 && uniform; dy++)
                    for (var dx = -1; dx <= 1 && uniform; dx++)
                    {
                        var j = ((y + dy) * w + x + dx) * 4;
                        uniform = opaque[j] == opaque[i] && opaque[j + 1] == opaque[i + 1] && opaque[j + 2] == opaque[i + 2];
                    }
                if (!uniform)
                    continue;
                var isBackground = Math.Abs(opaque[i] - background.X) < 1 && Math.Abs(opaque[i + 1] - background.Y) < 1 && Math.Abs(opaque[i + 2] - background.Z) < 1;
                if (isBackground)
                    Assert.Equal(0, clear[i + 3]);
                else
                {
                    inside++;
                    Assert.Equal((opaque[i], opaque[i + 1], opaque[i + 2], (byte)255), (clear[i], clear[i + 1], clear[i + 2], clear[i + 3]));
                }
            }
        Assert.True(inside > 1000);
        // Edges keep their coverage as alpha and their colour straight: laid over the background in
        // linear light, as the opaque view blended them, they come back to the opaque view's pixels.
        var edges = 0;
        var under = Colors.LinearFromHex(Shapes.Background);
        for (var i = 0; i < clear.Length; i += 4)
        {
            var a = clear[i + 3] / 255f;
            if (a is <= 0.1f or >= 0.9f)
                continue;
            edges++;
            for (var c = 0; c < 3; c++)
            {
                var mixed = Colors.ToLinear(clear[i + c] / 255f) * a + under[c] * (1 - a);
                Assert.InRange(Colors.ToSrgb(mixed) * 255 - opaque[i + c], -3, 3);
            }
        }
        Assert.True(edges > 200, $"{edges} edge pixels");
    }

    /// <summary>
    /// The glow's halo spills over a transparent view's background as compose.py lays it on: where
    /// only the halo lies, the picture laid over the background colour in sRGB is the opaque view.
    /// Checked: a halo that keeps the view's alpha fails.
    /// </summary>
    [Fact]
    public void TheGlowSpillsOverATransparentViewsBackground()
    {
        var scene = Scenes3D.All.Single(s => s.Name == "glow-figure");
        var palette = new Palette(scene.Palette, scene.Shadows);
        var models = scene.Models.Select(m => Model.Load(gpu.Device, Path.Combine(Render3DReferenceTests.ModelDirectory, m.File))).ToList();
        try
        {
            var items = new List<DrawItem>();
            for (var i = 0; i < models.Count; i++)
                models[i].Draw(items, scene.Models[i].Model, palette, scene.Style, scene.Models[i].Scenery, scene.Models[i].PoseOf(models[i].Data));
            using var view = new View3D(gpu.Device)
            {
                Items = items, Camera = scene.Camera.ToCamera(), Sun = scene.Sun ?? default, Style = scene.Style, Background = Colors.LinearFromHex(scene.Background),
            };
            int w = scene.Width, h = scene.Height;
            Update(view, w, h);
            var opaque = view.Picture!.ReadPixels();
            view.Items = [.. items.Select(i => i with { Glow = Vector3.Zero })];
            Update(view, w, h);
            var unlit = view.Picture!.ReadPixels();
            view.Items = items;
            view.Background = null;
            Update(view, w, h);
            var clear = view.Picture!.ReadPixels();
            var background = Colors.FromHex(scene.Background) * 255;
            var halo = 0;
            for (var y = 1; y < h - 1; y++)
                for (var x = 1; x < w - 1; x++)
                {
                    // The background without the glow all about, a halo with it.
                    var bare = true;
                    for (var dy = -1; dy <= 1 && bare; dy++)
                        for (var dx = -1; dx <= 1 && bare; dx++)
                        {
                            var j = ((y + dy) * w + x + dx) * 4;
                            bare = Math.Abs(unlit[j] - background.X) < 1 && Math.Abs(unlit[j + 1] - background.Y) < 1 && Math.Abs(unlit[j + 2] - background.Z) < 1;
                        }
                    var i = (y * w + x) * 4;
                    if (!bare || Math.Abs(opaque[i] - background.X) + Math.Abs(opaque[i + 1] - background.Y) + Math.Abs(opaque[i + 2] - background.Z) < 6)
                        continue;
                    halo++;
                    var a = clear[i + 3] / 255f;
                    Assert.True(a > 0, $"no halo at {x},{y}");
                    for (var c = 0; c < 3; c++)
                        Assert.InRange(clear[i + c] * a + background[c] * (1 - a) - opaque[i + c], -2.5f, 2.5f);
                }
            Assert.True(halo > 500, $"{halo} halo pixels");
        }
        finally
        {
            foreach (var model in models)
                model.Dispose();
        }
    }

    /// <summary>
    /// A surface element shows its view's picture as an image element shows the same picture: laid
    /// out by CSS, clipped to its rounded content box, in a moved, translucent frame, at device
    /// scales 1 and 2 (the picture rendered at the content box's device pixels). Unturned since M12
    /// step 4: an image is sampled as Skia's raster backend samples one (weights in sixteenths), a
    /// surface by the GPU, so the two agree only where the picture lands on whole pixels.
    /// </summary>
    [Theory]
    [InlineData(1f)]
    [InlineData(2f)]
    public void ASurfaceElementShowsItsViewAsAnImageWould(float scale)
    {
        const string Css = """
            body { margin: 0; background: #e8dcc0; }
            .frame { position: absolute; left: 24px; top: 18px; padding: 6px; background: #6a4a2a; transform: translate(7px, -5px); opacity: 0.85; }
            surface, img { display: block; width: 210px; height: 120px; border: 2px solid #2a1a0a; border-radius: 14px; }
            """;
        UiDocument Document(string html)
        {
            var document = new UiDocument(TestFonts.Library);
            document.SetViewport(280 * scale, 180 * scale);
            document.SetZoom(scale, 1);
            document.AddStylesheet(Css, "view");
            Markup.SetInnerHtml(document.Body, html);
            document.Update(0);
            return document;
        }
        var (view, model, _) = ShapesView();
        using var _v = view;
        using var _m = model;
        // The shapes' own opaque background: an image is premultiplied as it is decoded and filtered premultiplied
        // (CanvasImage, as Chromium draws a decoded image), the surface's picture filtered as it is, so the two agree
        // only where the picture is opaque (its transparent background is the test above's).
        int width = (int)(280 * scale), height = (int)(180 * scale);
        var shown = Document("<div class=frame><surface></surface></div>");
        var surface = shown.AllElements().Single(e => e.Tag == "surface");
        var views = new SurfaceViews(shown);
        views.Show(surface, view);
        RgbaImage Paint(UiDocument document, Func<UiElement, int?>? surfaces, Func<string, Statventure.Canvas.CanvasImage?>? images)
        {
            using var painter = new UiPainter(document) { SurfaceTexture = surfaces, ImageTexture = images };
            using var canvas = new CanvasRenderer(gpu.Device);
            using var target = GpuImage.CreateRenderTarget(gpu.Device, width, height);
            var frame = gpu.Device.BeginFrame();
            views.Update(frame);
            canvas.Begin(width, height);
            painter.Paint(canvas);
            canvas.Render(frame, target);
            gpu.Device.EndFrame(frame);
            gpu.Device.WaitIdle();
            return new RgbaImage(width, height, target.ReadPixels());
        }
        var withView = Paint(shown, views.Texture, null);
        Assert.NotEqual(Paint(shown, null, null).Pixels, withView.Pixels);
        Assert.Equal((210 * (int)scale, 120 * (int)scale), (view.Picture!.Width, view.Picture.Height));
        // The same picture as an image.
        var pixels = view.Picture.ReadPixels();
        using var picture = new Statventure.Canvas.CanvasImage(new RgbaImage(view.Picture.Width, view.Picture.Height, pixels));
        var asImage = Document("<div class=frame><img src=view.png></div>");
        asImage.Layout.Context.ImageSize = url => new Vector2(view.Picture.Width, view.Picture.Height);
        asImage.Update(0);
        var withImage = Paint(asImage, null, url => picture);
        Assert.Equal(withImage.Pixels, withView.Pixels);
        // The model shows: the picture isn't its background alone.
        Assert.True(Enumerable.Range(0, pixels.Length / 4).Select(i => BitConverter.ToInt32(pixels, i * 4)).Distinct().Count() > 1);
    }

    [Fact]
    public void PickingFindsTheModelAndPartUnderAPoint()
    {
        var owner = new object();
        var (view, model, items) = ShapesView(owner);
        using var _v = view;
        using var _m = model;
        int w = Shapes.Width, h = Shapes.Height;
        Assert.Null(view.Pick(new Vector2(10, 10)));
        Update(view, w, h);
        var camera = view.Camera;
        var viewProjection = camera.ViewProjection((float)w / h);
        // Each part's middle, where nothing else of the model is in front, picks that part.
        var found = new HashSet<int>();
        foreach (var item in items)
        {
            var middle = Vector3.Transform((item.Mesh.BoundsMin + item.Mesh.BoundsMax) / 2, item.Model);
            var clip = Vector4.Transform(new Vector4(middle, 1), viewProjection);
            var pixel = new Vector2((clip.X / clip.W * 0.5f + 0.5f) * w, (clip.Y / clip.W * 0.5f + 0.5f) * h);
            if (view.Pick(pixel) is { } pick && pick.Part == item.Part)
            {
                Assert.Same(owner, pick.Owner);
                var depth = -Vector3.Transform(middle, camera.View).Z;
                var reach = Vector3.Distance(item.Mesh.BoundsMin, item.Mesh.BoundsMax);
                Assert.InRange(pick.Depth, depth - reach, depth + reach);
                found.Add(pick.Part);
            }
        }
        Assert.True(found.Count >= 3, $"parts found: {string.Join(",", found)}");
        Assert.Null(view.Pick(new Vector2(2, 2)));
        Assert.Null(view.Pick(new Vector2(-1, 5)));

        // Through a surface element turned and moved by CSS: a point of the viewport picks what its
        // place in the picture shows.
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(600, 400);
        document.SetZoom(1, 1);
        document.AddStylesheet($"body {{ margin: 0; }} surface {{ position: absolute; left: 60px; top: 40px; width: {w}px; height: {h}px; border: 3px solid black; transform: rotate(8deg) scale(1.1); }}", "pick");
        Markup.SetInnerHtml(document.Body, "<surface></surface>");
        document.Update(0);
        var surface = document.AllElements().Single(e => e.Tag == "surface");
        var views = new SurfaceViews(document);
        views.Show(surface, view);
        var box = surface.Box!;
        var transform = UiDocument.TransformOf(box);
        var origin = new Vector2(box.ViewportRect.X + box.ContentLeft, box.ViewportRect.Y + box.ContentTop);
        var checkedPoints = 0;
        for (var y = 20; y < h; y += 23)
            for (var x = 15; x < w; x += 29)
            {
                var inPicture = new Vector2(x + 0.5f, y + 0.5f);
                Assert.Equal(view.Pick(inPicture), views.Pick(surface, Vector2.Transform(origin + inPicture, transform)));
                checkedPoints++;
            }
        Assert.True(checkedPoints > 50);
        Assert.Null(views.Pick(surface, Vector2.Transform(origin + new Vector2(-2, 5), transform)));
    }
}
