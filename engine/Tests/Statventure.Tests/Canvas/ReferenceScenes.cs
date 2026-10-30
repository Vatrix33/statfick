using System.Numerics;
using System.Text;
using System.Text.Json;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Text;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Canvas;

/// <summary>
/// One element of a reference scene: its CSS (rendered by the current game's browser to make the
/// reference image) and the canvas calls that must paint the same thing.
/// </summary>
public sealed record SceneItem(float X, float Y, float W, float H, string Css, Action<CanvasRenderer, RectF> Paint, SceneItem[]? Children = null, string Html = "")
{
    public RectF Rect => new(X, Y, W, H);
}

public sealed record ReferenceScene(string Name, int Width, int Height, string Background, SceneItem[] Items);

/// <summary>
/// Scenes compared against the browser: boxes, shadows, gradients and effects. Their references are
/// made by Tools/Scripts/make-css-references.sh, which renders <see cref="ExportScript"/> in the
/// current game's test client.
/// </summary>
public static class ReferenceScenes
{
    static Color C(string hex) => Color.FromHex(hex);
    static Color Rgba(float r, float g, float b, float a) => new(r / 255f, g / 255f, b / 255f, a);
    static Brush S(string hex) => Brush.Solid(C(hex));

    static SceneItem Box(float x, float y, float w, float h, string css, Action<CanvasRenderer, RectF> paint, params SceneItem[] children) =>
        new(x, y, w, h, css, paint, children.Length > 0 ? children : null);

    public static readonly ReferenceScene Boxes = new("boxes", 640, 360, "#f4ecd8",
    [
        Box(20, 20, 120, 70, "background:#c9533a;border-radius:12px",
            (c, r) => c.DrawBox(r, S("#c9533a"), new CornerRadii(12), Border.None)),
        Box(160, 20, 120, 70, "background:#5c734c;border-radius:0 24px 0 24px;border:4px solid #1c1511",
            (c, r) => c.DrawBox(r, S("#5c734c"), new CornerRadii(0, 24, 0, 24), new Border(4, C("#1c1511")))),
        Box(300, 20, 160, 70, "background:#88b5ac;border-radius:50%",
            (c, r) => c.DrawBox(r, S("#88b5ac"), new CornerRadii(new Vector2(80, 35), new Vector2(80, 35), new Vector2(80, 35), new Vector2(80, 35)), Border.None)),
        Box(480, 20, 120, 70, "background:transparent;border:3px dashed #54345a;border-radius:8px",
            (c, r) => c.DrawBox(r, null, new CornerRadii(8), new Border(3, C("#54345a"), BorderStyle.Dashed))),
        Box(20, 120, 120, 70, "background:#e8c04a;border-style:solid;border-width:2px 8px 2px 8px;border-color:#7d4523;border-radius:6px",
            (c, r) => c.DrawBox(r, S("#e8c04a"), new CornerRadii(6), new Border(new Vector4(2, 8, 2, 8), C("#7d4523")))),
        Box(160, 120, 70, 70, "background:#2f3448;border-radius:35px",
            (c, r) => c.DrawBox(r, S("#2f3448"), new CornerRadii(35), Border.None)),
        Box(250, 120, 200, 70, "background:rgba(28,21,17,0.5);border:6px solid rgba(255,255,255,0.6);border-radius:20px / 10px",
            (c, r) => c.DrawBox(r, Brush.Solid(Rgba(28, 21, 17, 0.5f)), new CornerRadii(new Vector2(20, 10), new Vector2(20, 10), new Vector2(20, 10), new Vector2(20, 10)), new Border(6, Rgba(255, 255, 255, 0.6f)))),
        Box(480, 120, 120, 70, "background:#a9b4c5;border-radius:40px 4px 40px 4px;border:2px solid #130e0b",
            (c, r) => c.DrawBox(r, S("#a9b4c5"), new CornerRadii(40, 4, 40, 4), new Border(2, C("#130e0b")))),
        Box(20, 220, 600, 4, "background:#130e0b",
            (c, r) => c.FillRect(r, C("#130e0b"))),
        Box(20, 240, 100, 100, "background:#ffe27a;border:10px solid #ac8845;border-radius:50%",
            (c, r) => c.DrawBox(r, S("#ffe27a"), new CornerRadii(50), new Border(10, C("#ac8845")))),
        Box(140, 240, 1, 60, "background:#130e0b",
            (c, r) => c.FillRect(r, C("#130e0b"))),
        Box(150, 240, 3, 60, "background:#c9533a;border-radius:1.5px",
            (c, r) => c.DrawBox(r, S("#c9533a"), new CornerRadii(1.5f), Border.None)),
        Box(170, 240, 200, 100, "background:#fbf6ea;border:1px solid #b9af9a;border-radius:4px",
            (c, r) => c.DrawBox(r, S("#fbf6ea"), new CornerRadii(4), new Border(1, C("#b9af9a")))),
        Box(390, 240, 230, 100, "background:#3a3026;border:2px solid #5a4a3a;border-radius:14px 14px 0 0",
            (c, r) => c.DrawBox(r, S("#3a3026"), new CornerRadii(14, 14, 0, 0), new Border(2, C("#5a4a3a")))),
    ]);

    public static readonly ReferenceScene Shadows = new("shadows", 640, 360, "#f4ecd8",
    [
        Box(30, 30, 120, 70, "background:#fff;box-shadow:0 4px 12px rgba(0,0,0,0.35);border-radius:10px",
            (c, r) =>
            {
                c.DrawBoxShadow(r, new CornerRadii(10), new BoxShadow(new Vector2(0, 4), 12, 0, Rgba(0, 0, 0, 0.35f)));
                c.DrawBox(r, S("#ffffff"), new CornerRadii(10), Border.None);
            }),
        Box(190, 30, 120, 70, "background:#fff;box-shadow:6px 6px 0 #54345a",
            (c, r) =>
            {
                c.DrawBoxShadow(r, CornerRadii.Zero, new BoxShadow(new Vector2(6, 6), 0, 0, C("#54345a")));
                c.FillRect(r, C("#ffffff"));
            }),
        Box(350, 30, 120, 70, "background:#88b5ac;box-shadow:0 0 20px 6px rgba(255,226,122,0.8);border-radius:16px",
            (c, r) =>
            {
                c.DrawBoxShadow(r, new CornerRadii(16), new BoxShadow(Vector2.Zero, 20, 6, Rgba(255, 226, 122, 0.8f)));
                c.DrawBox(r, S("#88b5ac"), new CornerRadii(16), Border.None);
            }),
        Box(510, 30, 100, 70, "background:#fff;box-shadow:inset 0 0 12px rgba(0,0,0,0.6);border-radius:8px",
            (c, r) =>
            {
                c.DrawBox(r, S("#ffffff"), new CornerRadii(8), Border.None);
                c.DrawBoxShadow(r, new CornerRadii(8), new BoxShadow(Vector2.Zero, 12, 0, Rgba(0, 0, 0, 0.6f), Inset: true));
            }),
        Box(30, 170, 120, 70, "background:#fff;box-shadow:inset 4px 4px 0 rgba(84,52,90,0.7)",
            (c, r) =>
            {
                c.FillRect(r, C("#ffffff"));
                c.DrawBoxShadow(r, CornerRadii.Zero, new BoxShadow(new Vector2(4, 4), 0, 0, Rgba(84, 52, 90, 0.7f), Inset: true));
            }),
        Box(190, 170, 120, 70, "background:transparent;box-shadow:0 8px 18px -4px rgba(0,0,0,0.6);border-radius:12px",
            (c, r) => c.DrawBoxShadow(r, new CornerRadii(12), new BoxShadow(new Vector2(0, 8), 18, -4, Rgba(0, 0, 0, 0.6f)))),
        Box(350, 170, 120, 70, "background:#2f3448;box-shadow:0 2px 4px rgba(0,0,0,.4), 0 10px 30px rgba(0,0,0,.25);border-radius:6px",
            (c, r) =>
            {
                // CSS paints the first listed shadow on top.
                c.DrawBoxShadow(r, new CornerRadii(6), new BoxShadow(new Vector2(0, 10), 30, 0, Rgba(0, 0, 0, 0.25f)));
                c.DrawBoxShadow(r, new CornerRadii(6), new BoxShadow(new Vector2(0, 2), 4, 0, Rgba(0, 0, 0, 0.4f)));
                c.DrawBox(r, S("#2f3448"), new CornerRadii(6), Border.None);
            }),
        Box(510, 170, 100, 100, "background:#c9533a;border-radius:50%;box-shadow:0 0 0 6px #e8c04a, 0 0 24px rgba(201,83,58,.9)",
            (c, r) =>
            {
                c.DrawBoxShadow(r, new CornerRadii(50), new BoxShadow(Vector2.Zero, 24, 0, Rgba(201, 83, 58, 0.9f)));
                c.DrawBoxShadow(r, new CornerRadii(50), new BoxShadow(Vector2.Zero, 0, 6, C("#e8c04a")));
                c.DrawBox(r, S("#c9533a"), new CornerRadii(50), Border.None);
            }),
        Box(30, 280, 280, 60, "background:#fbf6ea;border:2px solid #7d4523;box-shadow:inset 0 2px 0 rgba(255,255,255,.7), 0 3px 8px rgba(0,0,0,.3);border-radius:10px",
            (c, r) =>
            {
                c.DrawBoxShadow(r, new CornerRadii(10), new BoxShadow(new Vector2(0, 3), 8, 0, Rgba(0, 0, 0, 0.3f)));
                c.DrawBox(r, S("#fbf6ea"), new CornerRadii(10), new Border(2, C("#7d4523")));
                // Inset shadows sit inside the border: the padding box.
                var padding = new RectF(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4);
                c.DrawBoxShadow(padding, new CornerRadii(8), new BoxShadow(new Vector2(0, 2), 0, 0, Rgba(255, 255, 255, 0.7f), Inset: true));
            }),
    ]);

    static GradientStop Stop(float offset, Color color) => new(offset, color);

    public static readonly ReferenceScene Gradients = new("gradients", 640, 360, "#f4ecd8",
    [
        Box(20, 20, 180, 90, "background:linear-gradient(90deg, #c9533a, #e8c04a)",
            (c, r) => c.DrawBox(r, Brush.CssLinear(r.Size, 90, Stop(0, C("#c9533a")), Stop(1, C("#e8c04a"))), CornerRadii.Zero, Border.None)),
        Box(220, 20, 180, 90, "background:linear-gradient(135deg, #2f3448 0%, #88b5ac 60%, #f4ecd8 100%);border-radius:14px",
            (c, r) => c.DrawBox(r, Brush.CssLinear(r.Size, 135, Stop(0, C("#2f3448")), Stop(0.6f, C("#88b5ac")), Stop(1, C("#f4ecd8"))), new CornerRadii(14), Border.None)),
        Box(420, 20, 180, 90, "background:linear-gradient(to top right, #54345a, #ffe27a)",
            (c, r) => c.DrawBox(r, Brush.CssLinear(r.Size, Brush.CssCornerAngle(r.Size, toRight: true, toBottom: false), Stop(0, C("#54345a")), Stop(1, C("#ffe27a"))), CornerRadii.Zero, Border.None)),
        Box(20, 130, 180, 90, "background:radial-gradient(circle at 30% 40%, #fff 0%, #88b5ac 45%, #2f3448 100%)",
            (c, r) =>
            {
                var center = new Vector2(54, 36);
                var radius = MathF.Sqrt(126 * 126 + 54 * 54);
                c.DrawBox(r, Brush.Radial(center, new Vector2(radius), Stop(0, C("#ffffff")), Stop(0.45f, C("#88b5ac")), Stop(1, C("#2f3448"))), CornerRadii.Zero, Border.None);
            }),
        Box(220, 130, 180, 90, "background:radial-gradient(ellipse, rgba(255,226,122,0.9), rgba(255,226,122,0) 70%), #2f3448",
            (c, r) =>
            {
                c.FillRect(r, C("#2f3448"));
                var radii = new Vector2(90, 45) * MathF.Sqrt(2);
                c.DrawBox(r, Brush.Radial(new Vector2(90, 45), radii, Stop(0, Rgba(255, 226, 122, 0.9f)), Stop(0.7f, Rgba(255, 226, 122, 0f))), CornerRadii.Zero, Border.None);
            }),
        Box(420, 130, 180, 90, "background:repeating-linear-gradient(45deg, #c0ac63 0px, #c0ac63 6px, #a5884d 6px, #a5884d 12px)",
            (c, r) =>
            {
                var length = MathF.Abs(r.Width * MathF.Sin(MathF.PI / 4)) + MathF.Abs(r.Height * MathF.Cos(MathF.PI / 4));
                var brush = Brush.CssLinear(r.Size, 45, Stop(0, C("#c0ac63")), Stop(6 / length, C("#c0ac63")), Stop(6 / length, C("#a5884d")), Stop(12 / length, C("#a5884d"))) with { Repeat = true };
                c.DrawBox(r, brush, CornerRadii.Zero, Border.None);
            }),
        Box(20, 240, 580, 40, "background:linear-gradient(180deg, rgba(255,255,255,0.6), rgba(0,0,0,0.3));border-radius:20px;border:2px solid #130e0b",
            (c, r) => c.DrawBox(r, Brush.CssLinear(r.Size, 180, Stop(0, Rgba(255, 255, 255, 0.6f)), Stop(1, Rgba(0, 0, 0, 0.3f))), new CornerRadii(20), new Border(2, C("#130e0b")))),
        Box(20, 300, 280, 44, "background:linear-gradient(90deg, #130e0b 0%, transparent 50%, #130e0b 100%)",
            (c, r) => c.DrawBox(r, Brush.CssLinear(r.Size, 90, Stop(0, C("#130e0b")), Stop(0.5f, Color.Transparent), Stop(1, C("#130e0b"))), CornerRadii.Zero, Border.None)),
    ]);

    public static readonly ReferenceScene Effects = new("effects", 640, 360, "#f4ecd8",
    [
        Box(20, 20, 160, 100, "opacity:0.5",
            (c, r) =>
            {
                c.PushLayer(new LayerOptions { Opacity = 0.5f, Bounds = r });
                c.FillRect(new RectF(r.X, r.Y, 100, 70), C("#c9533a"));
                c.FillRect(new RectF(r.X + 60, r.Y + 30, 100, 70), C("#2f3448"));
                c.PopLayer();
            },
            Box(0, 0, 100, 70, "background:#c9533a", (_, _) => { }),
            Box(60, 30, 100, 70, "background:#2f3448", (_, _) => { })),
        Box(200, 20, 120, 70, "background:#5c734c;border-radius:10px;filter:brightness(1.4)",
            (c, r) =>
            {
                c.ColorFilter = ColorMatrix.For(Filter.Brightness(1.4f));
                c.DrawBox(r, S("#5c734c"), new CornerRadii(10), Border.None);
                c.ColorFilter = null;
            }),
        Box(340, 20, 120, 70, "background:linear-gradient(90deg,#c9533a,#88b5ac);filter:grayscale(1)",
            (c, r) =>
            {
                c.ColorFilter = ColorMatrix.For(Filter.Grayscale(1));
                c.DrawBox(r, Brush.CssLinear(r.Size, 90, Stop(0, C("#c9533a")), Stop(1, C("#88b5ac"))), CornerRadii.Zero, Border.None);
                c.ColorFilter = null;
            }),
        Box(480, 20, 120, 70, "background:#88b5ac;filter:sepia(0.8) saturate(1.6)",
            (c, r) =>
            {
                c.ColorFilter = ColorMatrix.Then(ColorMatrix.For(Filter.Sepia(0.8f)), ColorMatrix.For(Filter.Saturate(1.6f)));
                c.FillRect(r, C("#88b5ac"));
                c.ColorFilter = null;
            }),
        Box(20, 160, 100, 80, "background:#e8c04a;border-radius:20px;filter:drop-shadow(4px 6px 6px rgba(0,0,0,0.6))",
            (c, r) =>
            {
                c.PushLayer(new LayerOptions { Filters = [Filter.DropShadow(new Vector2(4, 6), 6, Rgba(0, 0, 0, 0.6f))], Bounds = r });
                c.DrawBox(r, S("#e8c04a"), new CornerRadii(20), Border.None);
                c.PopLayer();
            }),
        Box(150, 160, 100, 80, "background:#c9533a;border-radius:8px;filter:blur(4px)",
            (c, r) =>
            {
                c.PushLayer(new LayerOptions { Filters = [Filter.Blur(4)], Bounds = r });
                c.DrawBox(r, S("#c9533a"), new CornerRadii(8), Border.None);
                c.PopLayer();
            }),
        Box(290, 170, 100, 60, "background:#54345a;border-radius:8px;transform:rotate(-12deg) scale(1.1)",
            (c, r) =>
            {
                var center = r.Center;
                c.PushTransform(Matrix3x2.CreateTranslation(-center) * Matrix3x2.CreateScale(1.1f) * Matrix3x2.CreateRotation(-12f * MathF.PI / 180f) * Matrix3x2.CreateTranslation(center));
                c.DrawBox(r, S("#54345a"), new CornerRadii(8), Border.None);
                c.PopTransform();
            }),
        Box(420, 160, 180, 90, "overflow:hidden;border-radius:24px;background:#2f3448",
            (c, r) =>
            {
                c.DrawBox(r, S("#2f3448"), new CornerRadii(24), Border.None);
                c.PushClip(r, new CornerRadii(24));
                c.FillRect(new RectF(r.X - 20, r.Y - 20, 120, 80), C("#e8c04a"));
                c.FillRect(new RectF(r.X + 100, r.Y + 50, 120, 80), C("#88b5ac"));
                c.PopClip();
            },
            Box(-20, -20, 120, 80, "background:#e8c04a", (_, _) => { }),
            Box(100, 50, 120, 80, "background:#88b5ac", (_, _) => { })),
        Box(20, 280, 120, 60, "background:#c9533a;filter:hue-rotate(90deg)",
            (c, r) =>
            {
                c.ColorFilter = ColorMatrix.For(Filter.HueRotate(90));
                c.FillRect(r, C("#c9533a"));
                c.ColorFilter = null;
            }),
        Box(160, 270, 120, 70, "background:#88b5ac",
            (c, r) => c.FillRect(r, C("#88b5ac"))),
        Box(200, 290, 120, 60, "background:#e8c04a;mix-blend-mode:multiply",
            (c, r) =>
            {
                c.Blend = BlendMode.Multiply;
                c.FillRect(r, C("#e8c04a"));
                c.Blend = BlendMode.Premultiplied;
            }),
        Box(340, 270, 80, 70, "background:#54345a",
            (c, r) => c.FillRect(r, C("#54345a"))),
        Box(360, 290, 60, 60, "background:#c9533a;mix-blend-mode:screen",
            (c, r) =>
            {
                c.Blend = BlendMode.Screen;
                c.FillRect(r, C("#c9533a"));
                c.Blend = BlendMode.Premultiplied;
            }),
        Box(450, 260, 170, 90, "background:repeating-linear-gradient(90deg, #130e0b 0px, #130e0b 8px, #ffe27a 8px, #ffe27a 16px)",
            (c, r) => c.DrawBox(r, Brush.CssLinear(r.Size, 90, Stop(0, C("#130e0b")), Stop(8 / r.Width, C("#130e0b")), Stop(8 / r.Width, C("#ffe27a")), Stop(16 / r.Width, C("#ffe27a"))) with { Repeat = true }, CornerRadii.Zero, Border.None)),
        Box(470, 280, 130, 50, "background:rgba(255,255,255,0.3);backdrop-filter:blur(4px);border-radius:10px",
            (c, r) =>
            {
                c.DrawBackdropBlur(r, new CornerRadii(10), 4);
                c.DrawBox(r, Brush.Solid(Rgba(255, 255, 255, 0.3f)), new CornerRadii(10), Border.None);
            }),
    ]);

    static SceneItem Svg(float x, float y, string svgBody, Action<CanvasRenderer> paint) =>
        new(x, y, 120, 120, "", (c, r) =>
        {
            c.PushTranslation(r.X, r.Y);
            paint(c);
            c.PopTransform();
        }, Html: $"<svg width=\"120\" height=\"120\" style=\"position:absolute;left:0;top:0;overflow:visible\">{svgBody}</svg>");

    public static readonly ReferenceScene Paths = new("paths", 640, 360, "#f4ecd8",
    [
        Svg(20, 20, "<path d=\"M10 10 L110 10 L60 110 Z\" fill=\"#c9533a\"/>",
            c => c.FillPath(Statventure.Canvas.Path.ParseSvg("M10 10 L110 10 L60 110 Z"), S("#c9533a"))),
        Svg(160, 20, "<path d=\"M60 5 L95 110 L5 40 L115 40 L25 110 Z\" fill=\"#2f3448\" fill-rule=\"evenodd\"/>",
            c => c.FillPath(Statventure.Canvas.Path.ParseSvg("M60 5 L95 110 L5 40 L115 40 L25 110 Z"), S("#2f3448"), FillRule.EvenOdd)),
        Svg(300, 20, "<circle cx=\"60\" cy=\"60\" r=\"50\" fill=\"none\" stroke=\"#54345a\" stroke-width=\"8\"/>",
            c => c.StrokePath(Statventure.Canvas.Path.Circle(new Vector2(60, 60), 50), S("#54345a"), new StrokeStyle(8))),
        Svg(440, 20, "<path d=\"M10 100 C 30 10, 90 10, 110 100\" fill=\"none\" stroke=\"#5c734c\" stroke-width=\"6\" stroke-linecap=\"round\"/>",
            c => c.StrokePath(Statventure.Canvas.Path.ParseSvg("M10 100 C 30 10, 90 10, 110 100"), S("#5c734c"), new StrokeStyle(6, Cap: LineCap.Round))),
        Svg(20, 200, "<path d=\"M10 110 L40 20 L70 110 L100 20\" fill=\"none\" stroke=\"#130e0b\" stroke-width=\"5\" stroke-linejoin=\"miter\"/>",
            c => c.StrokePath(Statventure.Canvas.Path.ParseSvg("M10 110 L40 20 L70 110 L100 20"), S("#130e0b"), new StrokeStyle(5, LineJoin.Miter))),
        Svg(160, 200, "<path d=\"M10 110 L40 20 L70 110 L100 20\" fill=\"none\" stroke=\"#130e0b\" stroke-width=\"5\" stroke-linejoin=\"round\"/>",
            c => c.StrokePath(Statventure.Canvas.Path.ParseSvg("M10 110 L40 20 L70 110 L100 20"), S("#130e0b"), new StrokeStyle(5, LineJoin.Round))),
        Svg(300, 200, "<defs><linearGradient id=\"sv-g\" x1=\"0\" y1=\"0\" x2=\"1\" y2=\"0\"><stop offset=\"0\" stop-color=\"#e8c04a\"/><stop offset=\"1\" stop-color=\"#c9533a\"/></linearGradient></defs><path d=\"M60 110 C 20 80, 0 50, 20 25 C 35 8, 55 15, 60 30 C 65 15, 85 8, 100 25 C 120 50, 100 80, 60 110 Z\" fill=\"url(#sv-g)\"/>",
            c => c.FillPath(Statventure.Canvas.Path.ParseSvg("M60 110 C 20 80, 0 50, 20 25 C 35 8, 55 15, 60 30 C 65 15, 85 8, 100 25 C 120 50, 100 80, 60 110 Z"),
                Brush.Linear(new Vector2(6.75f, 0), new Vector2(113.25f, 0), new GradientStop(0, C("#e8c04a")), new GradientStop(1, C("#c9533a"))))),
        Svg(440, 200, "<path d=\"M10 60 Q60 0 110 60 T 110 110 A 30 30 0 0 1 10 110 Z\" fill=\"#88b5ac\" stroke=\"#130e0b\" stroke-width=\"2\"/>",
            c =>
            {
                var path = Statventure.Canvas.Path.ParseSvg("M10 60 Q60 0 110 60 T 110 110 A 30 30 0 0 1 10 110 Z");
                c.FillPath(path, S("#88b5ac"));
                c.StrokePath(path, S("#130e0b"), new StrokeStyle(2));
            }),
    ]);

    static readonly string[] Ui = ["Lato"];
    static readonly string[] Title = ["STIX Two Text"];

    static TextStyle Font(string[] families, float size, string color = "#1c1511", int weight = 400, bool italic = false, float letterSpacing = 0, TextTransform transform = TextTransform.None, float? lineHeight = null, bool underline = false) =>
        new() { Families = families, Size = size, Color = C(color), Weight = weight, Italic = italic, LetterSpacing = letterSpacing, Transform = transform, LineHeight = lineHeight, Underline = underline };

    static SceneItem Text(float x, float y, float w, float h, string css, string html, TextSpan[] spans, ParagraphStyle paragraph) =>
        new(x, y, w, h, css, (c, r) => c.DrawText(TextLayout.Create(TestFonts.Library, spans, paragraph with { MaxWidth = paragraph.MaxWidth }), r.Position), Html: html);

    public static readonly ReferenceScene TextScene = new("text", 640, 430, "#f4ecd8",
    [
        Text(20, 16, 600, 24, "font:16px Lato;color:#1c1511;white-space:nowrap", "The quick brown fox jumps over the lazy dog. 0123456789",
            [new("The quick brown fox jumps over the lazy dog. 0123456789", Font(Ui, 16))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
        Text(20, 46, 600, 24, "font:700 13px Lato;color:#7d4523;letter-spacing:.16em;text-transform:uppercase;white-space:nowrap", "Combat begins against Goblin.",
            [new("Combat begins against Goblin.", Font(Ui, 13, "#7d4523", 700, letterSpacing: 13 * 0.16f, transform: TextTransform.Uppercase))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
        Text(20, 70, 600, 50, "font:400 36px 'STIX Two Text';color:#1c1511;letter-spacing:-0.045em;white-space:nowrap", "Field of <em style=\"color:#c7683f;font-style:italic\">battle</em>",
            [new("Field of ", Font(Title, 36, letterSpacing: 36 * -0.045f)), new("battle", Font(Title, 36, "#c7683f", italic: true, letterSpacing: 36 * -0.045f))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
        Text(20, 130, 280, 120, "font:15px Lato;line-height:1.6;color:#1c1511", "A warband of goblins ambushes the caravan near the old mill. Each of them carries a crude spear and a grudge.",
            [new("A warband of goblins ambushes the caravan near the old mill. Each of them carries a crude spear and a grudge.", Font(Ui, 15, lineHeight: 24))], new ParagraphStyle { MaxWidth = 280 }),
        Text(330, 130, 280, 120, "font:600 14px Lato;line-height:1.5;color:#54345a;text-align:center", "Every spell beyond the Basic that strikes it hardens its bearer.",
            [new("Every spell beyond the Basic that strikes it hardens its bearer.", Font(Ui, 14, "#54345a", 600, lineHeight: 21))], new ParagraphStyle { MaxWidth = 280, Align = TextAlign.Center }),
        Text(20, 250, 160, 22, "font:14px Lato;color:#1c1511;white-space:nowrap;overflow:hidden;text-overflow:ellipsis", "Exalted Keening of the Elven Masters",
            [new("Exalted Keening of the Elven Masters", Font(Ui, 14))], new ParagraphStyle { MaxWidth = 160, WhiteSpace = WhiteSpace.NoWrap, Ellipsis = true }),
        Text(200, 250, 120, 22, "font:12px Lato;color:#4a6964;text-align:right;white-space:nowrap", "31 / 32 HP",
            [new("31 / 32 HP", Font(Ui, 12, "#4a6964"))], new ParagraphStyle { MaxWidth = 120, Align = TextAlign.Right, WhiteSpace = WhiteSpace.NoWrap }),
        Text(340, 250, 280, 30, "font:italic 18px 'STIX Two Text';color:#2f3448;white-space:nowrap", "Sealed Transformer, true italic",
            [new("Sealed Transformer, true italic", Font(Title, 18, "#2f3448", italic: true))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
        Text(20, 280, 300, 24, "font:italic 15px Lato;color:#1c1511;white-space:nowrap", "Synthetic oblique Lato text",
            [new("Synthetic oblique Lato text", Font(Ui, 15, italic: true))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
        Text(340, 280, 280, 30, "font:700 20px 'STIX Two Text';color:#1c1511;letter-spacing:-0.02em;white-space:nowrap", "Stat Fick synthetic bold",
            [new("Stat Fick synthetic bold", Font(Title, 20, weight: 700, letterSpacing: 20 * -0.02f))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
        Text(20, 312, 90, 100, "font:13px Lato;line-height:1.4;color:#1c1511;overflow-wrap:anywhere", "Supercalifragilisticexpialidocious spear",
            [new("Supercalifragilisticexpialidocious spear", Font(Ui, 13, lineHeight: 18.2f))], new ParagraphStyle { MaxWidth = 90, OverflowWrap = OverflowWrap.Anywhere }),
        Text(130, 312, 190, 0, "font:13px Lato;line-height:1.5;color:#1c1511;display:-webkit-box;-webkit-line-clamp:3;-webkit-box-orient:vertical;overflow:hidden", "The Golden Apex splits the blow into light: one quarter stays Basic, the other three become random other elements of the world.",
            [new("The Golden Apex splits the blow into light: one quarter stays Basic, the other three become random other elements of the world.", Font(Ui, 13, lineHeight: 19.5f))], new ParagraphStyle { MaxWidth = 190, MaxLines = 3, Ellipsis = true }),
        Text(340, 316, 280, 40, "font:28px Lato;color:#1c1511;white-space:nowrap", "AVATAR To Wa ffi",
            [new("AVATAR To Wa ffi", Font(Ui, 28))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
        Text(340, 360, 280, 24, "font:12px Lato;color:#7d4523;text-decoration:underline;white-space:nowrap", "Underlined text button",
            [new("Underlined text button", Font(Ui, 12, "#7d4523", underline: true))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
        Box(130, 384, 490, 36, "background:#2f3448", (c, r) => c.FillRect(r, C("#2f3448"))),
        Text(136, 390, 300, 24, "font:15px Lato;color:#f4ecd8;white-space:nowrap", "Light parchment text on dusk blue",
            [new("Light parchment text on dusk blue", Font(Ui, 15, "#f4ecd8"))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
        Text(400, 390, 220, 24, "font:700 13px Lato;color:#e8c04a;white-space:nowrap;letter-spacing:.1em", "GOLD LABEL",
            [new("GOLD LABEL", Font(Ui, 13, "#e8c04a", 700, letterSpacing: 1.3f))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
        Text(20, 392, 110, 24, "font:15px Lato;color:#c7683f;white-space:nowrap", "Ember text",
            [new("Ember text", Font(Ui, 15, "#c7683f"))], new ParagraphStyle { WhiteSpace = WhiteSpace.NoWrap }),
    ]);

    public static readonly ReferenceScene[] All = [Boxes, Shadows, Gradients, Effects, Paths, TextScene];

    /// <summary>Paints a scene with the canvas (background included).</summary>
    public static void Paint(ReferenceScene scene, CanvasRenderer canvas)
    {
        canvas.FillRect(new RectF(0, 0, scene.Width, scene.Height), C(scene.Background));
        foreach (var item in scene.Items)
            item.Paint(canvas, item.Rect);
    }

    /// <summary>A test-client script (Stats/tests) that renders every scene with the browser and captures it.</summary>
    public static string ExportScript()
    {
        object Item(SceneItem item) => new
        {
            x = item.X,
            y = item.Y,
            w = item.W,
            h = item.H,
            css = item.Css,
            html = item.Html,
            children = item.Children?.Select(Item).ToArray() ?? [],
        };
        var scenes = All.Select(scene => new { name = scene.Name, width = scene.Width, height = scene.Height, background = scene.Background, items = scene.Items.Select(Item).ToArray() });
        var json = JsonSerializer.Serialize(scenes);
        var script = new StringBuilder();
        script.AppendLine("// Generated by Statventure.Tests (ReferenceScenes.ExportScript): renders the canvas reference scenes.");
        script.AppendLine("const STATVENTURE_SCENES = " + json + ";");
        script.AppendLine("""
            async function statventureReferences() {
              const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
              const build = (item, parent) => {
                const el = document.createElement("div");
                el.style.cssText = `position:absolute;box-sizing:border-box;left:${item.x}px;top:${item.y}px;width:${item.w}px;` + (item.h > 0 ? `height:${item.h}px;` : "") + item.css;
                if (item.html) el.innerHTML = item.html;
                for (const child of item.children) build(child, el);
                parent.append(el);
              };
              for (const scene of STATVENTURE_SCENES) {
                const host = document.createElement("div");
                host.style.cssText = `position:fixed;left:0;top:0;width:${scene.width}px;height:${scene.height}px;background:${scene.background};z-index:2147483647;overflow:hidden;isolation:isolate;`;
                for (const item of scene.items) build(item, host);
                document.body.append(host);
                await wait(300);
                await window.statFickDesktop.captureTestScreen("css-" + scene.name, 1600, 900);
                host.remove();
              }
            }
            """);
        return script.ToString();
    }
}
