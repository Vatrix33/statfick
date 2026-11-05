using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Canvas;

/// <summary>
/// A box is antialiased by its distance in device pixels under any 2D transform: coverage =
/// clamp(0.5 − d), d the pixel centre's signed distance to the box as the transform places it on the
/// target, so each edge is one pixel wide across itself. Transforms that scale one axis more than
/// the other (the game's meters' scaleX, a rotation scaled afterwards, skews) give each edge its own
/// scale, which one isotropic scale for the whole box got wrong. Chromium's references for such
/// boxes are TransformPaintReferenceTests; on edges that slant Skia fills by area with its own
/// snapping (Engine-Plan.md, Progress, "Canvas: box antialiasing under non-uniform transforms"), so
/// the rule is held here to its geometry, and to the box drawn without the scale.
/// </summary>
[Collection("GPU")]
public class BoxTransformTests(GpuFixture gpu)
{
    const int Size = 240;
    static readonly Vector2 Box = new(60, 40);

    /// <summary>CSS transform lists (applied right to left, about the box's centre).</summary>
    static readonly Dictionary<string, Matrix3x2> Transforms = new()
    {
        ["scaleX(0.1)"] = Matrix3x2.CreateScale(0.1f, 1),
        ["scaleX(0.37)"] = Matrix3x2.CreateScale(0.37f, 1),
        ["scaleX(3)"] = Matrix3x2.CreateScale(3, 1),
        ["scaleX(-0.6)"] = Matrix3x2.CreateScale(-0.6f, 1),
        ["scaleY(0.25)"] = Matrix3x2.CreateScale(1, 0.25f),
        ["scaleY(2.5)"] = Matrix3x2.CreateScale(1, 2.5f),
        ["rotate(30deg) scaleX(0.3)"] = Matrix3x2.CreateScale(0.3f, 1) * Rotation(30),
        ["scale(2, 0.5) rotate(30deg)"] = Rotation(30) * Matrix3x2.CreateScale(2, 0.5f),
        ["rotate(-35deg) scaleY(1.8)"] = Matrix3x2.CreateScale(1, 1.8f) * Rotation(-35),
        ["skewX(40deg)"] = Matrix3x2.CreateSkew(40 * MathF.PI / 180, 0),
        ["skewY(-20deg)"] = Matrix3x2.CreateSkew(0, -20 * MathF.PI / 180),
        // A uniform scale and a rotation: one scale for the whole box was right here, and still is.
        ["rotate(20deg) scale(1.5)"] = Matrix3x2.CreateScale(1.5f) * Rotation(20),
    };

    static Matrix3x2 Rotation(float degrees) => Matrix3x2.CreateRotation(degrees * MathF.PI / 180);

    public static TheoryData<string, float, float> Cases
    {
        get
        {
            var data = new TheoryData<string, float, float>();
            foreach (var name in Transforms.Keys)
                foreach (var scale in new[] { 1f, 1.65f })
                    foreach (var radius in new[] { 0f, 8f })
                        data.Add(name, scale, radius);
            return data;
        }
    }

    /// <summary>
    /// A transform about the box's centre, then to the middle of the target (canvas units), a
    /// fraction of a pixel off so that edges along the pixel grid fall inside pixels too.
    /// </summary>
    static Matrix3x2 Placement(Vector2 box, Matrix3x2 transform, float scale) =>
        Matrix3x2.CreateTranslation(-box / 2) * transform * Matrix3x2.CreateTranslation((new Vector2(Size / 2f) + new Vector2(0.3f, 0.45f)) / scale);

    RgbaImage Render(Matrix3x2 placement, float scale, RectF box, CornerRadii radii, Border border)
    {
        var device = gpu.Device;
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, Size, Size);
        var frame = device.BeginFrame();
        canvas.Begin(Size, Size, scale);
        canvas.PushTransform(placement);
        canvas.DrawBox(box, Brush.Solid(Color.FromBytes(0, 0, 0)), radii, border);
        canvas.PopTransform();
        canvas.Render(frame, target);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(Size, Size, target.ReadPixels());
    }

    /// <summary>
    /// Every pixel's coverage against the box's distance on the target: past the box's edges' lines
    /// (the largest of them inside and beside an edge), beyond a square corner (past both of its
    /// edges) to the corner point, and in a rounded corner's square its ellipse's distance to first
    /// order: (|e| − 1)·min(r) with e = d / r in the box's units, over its gradient's length in pixels,
    /// the gradient carried to pixels by the transform itself (a shear makes the gradient's direction
    /// and signs matter).
    /// </summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void EdgesAreOnePixelWideAcrossThemselves(string name, float scale, float radius)
    {
        var placement = Placement(Box, Transforms[name], scale);
        // Elliptical corners, so an ellipse's normal and a circle's radius part ways.
        var r = new Vector2(radius, radius * 0.625f);
        var image = Render(placement, scale, new RectF(0, 0, Box.X, Box.Y), new CornerRadii(r, r, r, r), Border.None);
        // The box's corners on the target, in device pixels, going round; edge i runs from corner i to i + 1.
        var corners = new[] { Vector2.Zero, new Vector2(Box.X, 0), Box, new Vector2(0, Box.Y) }
            .Select(c => Vector2.Transform(c, placement) * scale).ToArray();
        var clockwise = Cross(corners[1] - corners[0], corners[2] - corners[1]) > 0;
        var outward = Enumerable.Range(0, 4).Select(i =>
        {
            var edge = corners[(i + 1) % 4] - corners[i];
            return Vector2.Normalize(clockwise ? new Vector2(edge.Y, -edge.X) : new Vector2(-edge.Y, edge.X));
        }).ToArray();
        // The box's units from the target's pixels, and their change a pixel along x and along y.
        Matrix3x2.Invert(placement * Matrix3x2.CreateScale(scale), out var toLocal);
        var alongX = new Vector2(toLocal.M11, toLocal.M12);
        var alongY = new Vector2(toLocal.M21, toLocal.M22);
        var edgePixels = 0;
        var arcPixels = 0;
        var problems = new List<string>();
        for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                var centre = new Vector2(x + 0.5f, y + 0.5f);
                var past = Enumerable.Range(0, 4).Select(i => Vector2.Dot(centre - corners[i], outward[i])).ToArray();
                var distance = past.Max();
                var q = Vector2.Transform(centre, toLocal) - Box / 2;
                var d = Vector2.Abs(q) - Box / 2 + r;
                if (radius > 0 && d.X > 0 && d.Y > 0)
                {
                    var e = d / r;
                    var k = e.Length();
                    var m = MathF.Min(r.X, r.Y);
                    var side = new Vector2(q.X < 0 ? -1 : 1, q.Y < 0 ? -1 : 1);
                    var gradient = side * m * (e / r) / k;
                    var perPixel = new Vector2(Vector2.Dot(gradient, alongX), Vector2.Dot(gradient, alongY));
                    distance = (k - 1) * m / perPixel.Length();
                    if (MathF.Abs(distance) < 1)
                        arcPixels++;
                }
                else if (radius == 0)
                    for (var i = 0; i < 4; i++)
                        if (past[(i + 3) % 4] > 0 && past[i] > 0)
                            distance = Vector2.Distance(centre, corners[i]);
                var expected = Math.Clamp(0.5f - distance, 0, 1) * 255;
                var alpha = image.Pixels[(y * Size + x) * 4 + 3];
                if (MathF.Abs(distance) < 1)
                    edgePixels++;
                if (MathF.Abs(alpha - expected) > 2 && problems.Count < 12)
                    problems.Add($"({x},{y}): coverage {alpha}, {expected:F1} at {distance:F3} px from the box");
            }
        Assert.True(edgePixels >= 50, $"only {edgePixels} pixels within a pixel of the edges checked");
        Assert.True(radius == 0 || arcPixels >= 20, $"only {arcPixels} pixels within a pixel of the arcs checked");
        Assert.True(problems.Count == 0, $"{name} at {scale}, radius {radius}:\n" + string.Join("\n", problems));
    }

    /// <summary>A scale, then a rotation (or none).</summary>
    public static TheoryData<float, float, float, float> ScaledCases => new()
    {
        { 0.1f, 1, 0, 1 },
        { 0.37f, 1, 0, 1.65f },
        { 2.5f, 1, 0, 1 },
        { 1, 0.3f, 0, 1.65f },
        { 1, 2.2f, 0, 1 },
        { 0.4f, 1, 30, 1 },
        { 1.6f, 0.7f, -15, 1.65f },
        { 1, 1.8f, -35, 1 },
    };

    /// <summary>
    /// A box (rounded, with a border) scaled along x and y and then rotated covers the same pixels
    /// as the box drawn that much larger, its radii and border widths scaled with it, and only rotated:
    /// the two are one shape on the target, and a rotation keeps distances. (A black box with a red
    /// border: alpha is the outer edge, red the border's band.)
    /// </summary>
    [Theory]
    [MemberData(nameof(ScaledCases))]
    public void AScaleBeforeARotationDrawsLikeTheScaledBox(float scaleX, float scaleY, float degrees, float scale)
    {
        var radii = new CornerRadii(new Vector2(10, 6), new Vector2(4, 4), new Vector2(12, 12), new Vector2(0, 0));
        var transformed = Render(Placement(Box, Matrix3x2.CreateScale(scaleX, scaleY) * Rotation(degrees), scale), scale,
            new RectF(0, 0, Box.X, Box.Y), radii, new Border(new Vector4(2, 3, 1.5f, 2.5f), Color.FromBytes(255, 0, 0)));
        var s = new Vector2(scaleX, scaleY);
        var sized = Box * s;
        var scaledRadii = new CornerRadii(radii.TopLeft * s, radii.TopRight * s, radii.BottomRight * s, radii.BottomLeft * s);
        var plain = Render(Placement(sized, Rotation(degrees), scale), scale,
            new RectF(0, 0, sized.X, sized.Y), scaledRadii, new Border(new Vector4(2 * scaleY, 3 * scaleX, 1.5f * scaleY, 2.5f * scaleX), Color.FromBytes(255, 0, 0)));
        var worst = 0;
        var different = 0;
        for (var i = 0; i < transformed.Pixels.Length; i++)
        {
            var difference = Math.Abs(transformed.Pixels[i] - plain.Pixels[i]);
            worst = Math.Max(worst, difference);
            if (difference > 2)
                different++;
        }
        Assert.True(different == 0, $"scale({scaleX}, {scaleY}) then rotate({degrees}deg) at {scale}: {different} pixels differ from the scaled box by more than 2 levels (worst {worst})");
    }

    static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}
