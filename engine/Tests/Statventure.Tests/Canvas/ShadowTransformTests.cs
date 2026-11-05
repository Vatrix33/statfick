using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Tests.Infrastructure;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.Tests.Canvas;

/// <summary>
/// A blurred box shadow under a transform falls off as Skia blurs it: Blink draws the shadow with a
/// draw looper whose blur respects the transform (kShadowRespectsTransforms), so the blur mask filter
/// takes the sigma (half the CSS blur radius) to the device with one factor for the whole matrix,
/// SkMatrix::mapRadius (the geometric mean of the lengths of the matrix's two columns), at most 128
/// (SkBlurMaskFilterImpl::computeXformedSigma), and blurs the shape as the transform places it, the
/// same in every direction. So under scaleX(0.2) a 12 px blur falls off across the bar with σ 6 × √0.2,
/// not σ 6 (the boss intro's progress bar; Engine-Plan.md, Progress, "Canvas: blurred shadows under
/// non-uniform transforms"). Each pixel is held to that blur computed on the target, in the target's
/// own axes: the shape (the box with its spread and offset, as the transform places it) is cut into
/// rows, each row's extent integrated across x in closed form and the rows weighted by the Gaussian
/// down y, finely enough to be exact here; the canvas works in the box's units instead, so this checks
/// its covariance, the correlation a shear brings included. An inset shadow is cast from Blink's frame,
/// only the blur radius wide around the padding box (<see cref="InsetCaster"/>), which a transform that
/// shrinks an axis makes thinner than the device blur. Chromium's references are
/// ShadowTransformPaintReferenceTests.
/// </summary>
[Collection("GPU")]
public class ShadowTransformTests(GpuFixture gpu)
{
    const int Size = 480;
    /// <summary>Levels allowed: the canvas samples four rows across the blur and approximates the error function, up to about 3 levels off the exact integral on the untransformed box alike.</summary>
    const double Tolerance = 4;
    static readonly Vector2 Box = new(100, 40);

    static Matrix3x2 Rotation(float degrees) => Matrix3x2.CreateRotation(degrees * MathF.PI / 180);

    /// <summary>CSS transform lists (applied right to left, about the box's centre).</summary>
    static readonly Dictionary<string, Matrix3x2> Transforms = new()
    {
        ["scaleX(0.2)"] = Matrix3x2.CreateScale(0.2f, 1),
        ["scaleX(0.6)"] = Matrix3x2.CreateScale(0.6f, 1),
        ["scaleY(0.3)"] = Matrix3x2.CreateScale(1, 0.3f),
        ["scaleX(2.5)"] = Matrix3x2.CreateScale(2.5f, 1),
        ["scale(1.6, 0.5)"] = Matrix3x2.CreateScale(1.6f, 0.5f),
        ["scaleX(-0.4)"] = Matrix3x2.CreateScale(-0.4f, 1),
        // A rotation after a one-axis scale: the shape stays a rectangle, turned.
        ["rotate(30deg) scaleX(0.4)"] = Matrix3x2.CreateScale(0.4f, 1) * Rotation(30),
        // Shears: the box's axes are no longer at right angles on the target, so the blur in its units
        // correlates them, and mapRadius (the columns' lengths) is not the determinant's root.
        ["scale(2, 0.5) rotate(30deg)"] = Rotation(30) * Matrix3x2.CreateScale(2, 0.5f),
        ["skewX(40deg)"] = Matrix3x2.CreateSkew(40 * MathF.PI / 180, 0),
        ["skewY(-25deg) scaleY(0.5)"] = Matrix3x2.CreateScale(1, 0.5f) * Matrix3x2.CreateSkew(0, -25 * MathF.PI / 180),
        // Uniform: the blur scales with the box, as before.
        ["rotate(20deg) scale(1.5)"] = Matrix3x2.CreateScale(1.5f) * Rotation(20),
        ["none"] = Matrix3x2.Identity,
    };

    /// <summary>The shadows: outer, outer with a spread and an offset, inset with a spread and an offset.</summary>
    static readonly Dictionary<string, BoxShadow> Shadows = new()
    {
        ["outer"] = new BoxShadow(Vector2.Zero, 12, 0, Color.FromBytes(0, 0, 0)),
        ["outer spread offset"] = new BoxShadow(new Vector2(4, -3), 12, 3, Color.FromBytes(0, 0, 0)),
        ["inset spread offset"] = new BoxShadow(new Vector2(3, 2), 12, 2, Color.FromBytes(0, 0, 0), Inset: true),
        // A negative spread widens the hole and the frame around it.
        ["inset negative spread"] = new BoxShadow(new Vector2(-4, 3), 12, -3, Color.FromBytes(0, 0, 0), Inset: true),
    };

    public static TheoryData<string, string, float> Cases
    {
        get
        {
            var data = new TheoryData<string, string, float>();
            foreach (var transform in Transforms.Keys)
                foreach (var shadow in Shadows.Keys)
                    foreach (var scale in new[] { 1f, 1.65f })
                        data.Add(transform, shadow, scale);
            return data;
        }
    }

    static Matrix3x2 Placement(Matrix3x2 transform, Vector2 at, float scale) =>
        Matrix3x2.CreateTranslation(-Box / 2) * transform * Matrix3x2.CreateTranslation((at + new Vector2(0.3f, 0.45f)) / scale);

    RgbaImage Render(int size, Matrix3x2 placement, float scale, BoxShadow shadow)
    {
        var device = gpu.Device;
        using var canvas = new CanvasRenderer(device);
        using var target = GpuImage.CreateRenderTarget(device, size, size);
        var frame = device.BeginFrame();
        canvas.Begin(size, size, scale);
        canvas.PushTransform(placement);
        canvas.DrawBoxShadow(new RectF(0, 0, Box.X, Box.Y), CornerRadii.Zero, shadow);
        canvas.PopTransform();
        canvas.Render(frame, target);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(size, size, target.ReadPixels());
    }

    /// <summary>Skia's device sigma: mapRadius of the sigma through the whole matrix, at most 128.</summary>
    static float DeviceSigma(float sigma, Matrix3x2 toDevice) =>
        MathF.Min(sigma * MathF.Sqrt(new Vector2(toDevice.M11, toDevice.M12).Length() * new Vector2(toDevice.M21, toDevice.M22).Length()), 128);

    /// <summary>A parallelogram on the target: its corners going round and each edge's outward normal.</summary>
    sealed class Shape
    {
        readonly Vector2[] _corners;
        readonly Vector2[] _outward = new Vector2[4];

        public IReadOnlyList<Vector2> Corners => _corners;

        public Shape(RectF local, Matrix3x2 toDevice)
        {
            _corners = new[] { new Vector2(local.Left, local.Top), new Vector2(local.Right, local.Top), new Vector2(local.Right, local.Bottom), new Vector2(local.Left, local.Bottom) }
                .Select(c => Vector2.Transform(c, toDevice)).ToArray();
            var clockwise = Cross(_corners[1] - _corners[0], _corners[2] - _corners[1]) > 0;
            for (var i = 0; i < 4; i++)
            {
                var edge = _corners[(i + 1) % 4] - _corners[i];
                _outward[i] = Vector2.Normalize(clockwise ? new Vector2(edge.Y, -edge.X) : new Vector2(-edge.Y, edge.X));
            }
        }

        /// <summary>How far the point is outside the shape's nearest edge line (the largest of the four; negative inside).</summary>
        public float Outside(Vector2 point) => Enumerable.Range(0, 4).Max(i => Vector2.Dot(point - _corners[i], _outward[i]));

        /// <summary>The shape's extent along the row y = <paramref name="y"/> (left > right when the row misses it).</summary>
        public (double Left, double Right) Row(double y)
        {
            double left = double.MaxValue, right = double.MinValue;
            for (var i = 0; i < 4; i++)
            {
                var a = _corners[i];
                var b = _corners[(i + 1) % 4];
                if (y < Math.Min(a.Y, b.Y) || y > Math.Max(a.Y, b.Y))
                    continue;
                if (a.Y == b.Y)
                {
                    left = Math.Min(left, Math.Min(a.X, b.X));
                    right = Math.Max(right, Math.Max(a.X, b.X));
                    continue;
                }
                var x = a.X + (b.X - a.X) * (y - a.Y) / (b.Y - a.Y);
                left = Math.Min(left, x);
                right = Math.Max(right, x);
            }
            return (left, right);
        }
    }

    static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;

    /// <summary>The Gaussian's integral up to z standard deviations (Abramowitz and Stegun 7.1.26, within 1.5e-7).</summary>
    static double Phi(double z)
    {
        var x = Math.Abs(z) / Math.Sqrt(2);
        var t = 1 / (1 + 0.3275911 * x);
        var erf = 1 - (((((1.061405429 * t - 1.453152027) * t) + 1.421413741) * t - 0.284496736) * t + 0.254829592) * t * Math.Exp(-x * x);
        return z >= 0 ? 0.5 * (1 + erf) : 0.5 * (1 - erf);
    }

    /// <summary>
    /// The shape blurred by a Gaussian of this sigma at a point of the target: the rows from 4σ above
    /// to 4σ below, each weighted by the Gaussian and integrated across x in closed form. The rows'
    /// extent is linear between the corners' heights and the shape starts and ends at them, so the rows
    /// are summed piece by piece between those heights, 24 a piece at their midpoints.
    /// </summary>
    static double Blurred(Shape shape, Vector2 point, double sigma)
    {
        const int rows = 24;
        var breaks = new List<double> { -4 * sigma, 4 * sigma };
        foreach (var corner in shape.Corners)
            if (Math.Abs(corner.Y - point.Y) < 4 * sigma)
                breaks.Add(corner.Y - point.Y);
        breaks.Sort();
        var sum = 0.0;
        for (var piece = 0; piece + 1 < breaks.Count; piece++)
        {
            var step = (breaks[piece + 1] - breaks[piece]) / rows;
            for (var r = 0; r < rows; r++)
            {
                var t = breaks[piece] + (r + 0.5) * step;
                var (left, right) = shape.Row(point.Y + t);
                if (left > right)
                    continue;
                var weight = Math.Exp(-t * t / (2 * sigma * sigma)) / (Math.Sqrt(2 * Math.PI) * sigma) * step;
                sum += weight * (Phi((right - point.X) / sigma) - Phi((left - point.X) / sigma));
            }
        }
        return sum;
    }

    /// <summary>
    /// The frame Blink casts an inset shadow from, before the hole is taken out
    /// (BoxPainterBase::PaintInsetBoxShadow, AreaCastingShadowInHole): the padding box outset by the blur
    /// radius, and by the spread's size when it is negative, joined with that moved back by the offset,
    /// all drawn moved by the offset.
    /// </summary>
    static RectF InsetCaster(BoxShadow shadow)
    {
        var reach = shadow.Blur + MathF.Max(-shadow.Spread, 0);
        var low = Vector2.Min(shadow.Offset, Vector2.Zero) - new Vector2(reach);
        var high = Box + Vector2.Max(shadow.Offset, Vector2.Zero) + new Vector2(reach);
        return RectF.FromEdges(low.X, low.Y, high.X, high.Y);
    }

    /// <summary>
    /// Checks every other pixel (in each direction) within the shadow's reach (3σ of the shape, Skia's
    /// margin) where the box doesn't decide alone: an outer shadow a pixel or more outside the box (the box
    /// cuts it out), an inset one a pixel or more inside (the frame blurred less the hole blurred).
    /// Returns how many were checked, the worst difference and the first few beyond the tolerance.
    /// </summary>
    static (int Checked, double Worst, List<string> Problems) Check(RgbaImage image, int size, Matrix3x2 toDevice, BoxShadow shadow, float sigma)
    {
        var spread = shadow.Inset ? -shadow.Spread : shadow.Spread;
        var shape = new Shape(new RectF(shadow.Offset.X - spread, shadow.Offset.Y - spread, Box.X + 2 * spread, Box.Y + 2 * spread), toDevice);
        var caster = shadow.Inset ? new Shape(InsetCaster(shadow), toDevice) : null;
        var box = new Shape(new RectF(0, 0, Box.X, Box.Y), toDevice);
        var checkedPixels = 0;
        var worst = 0.0;
        var problems = new List<string>();
        for (var y = 0; y < size; y += 2)
            for (var x = 0; x < size; x += 2)
            {
                var centre = new Vector2(x + 0.5f, y + 0.5f);
                var outsideBox = box.Outside(centre);
                if (shadow.Inset ? outsideBox > -1 : outsideBox < 1)
                    continue;
                if (shape.Outside(centre) > 3 * sigma - 0.5f)
                    continue;
                var value = Blurred(shape, centre, sigma);
                var expected = 255 * (caster is not null ? Math.Clamp(Blurred(caster, centre, sigma) - value, 0, 1) : value);
                var alpha = image.Pixels[(y * size + x) * 4 + 3];
                checkedPixels++;
                var difference = Math.Abs(alpha - expected);
                worst = Math.Max(worst, difference);
                if (difference > Tolerance && problems.Count < 12)
                    problems.Add($"({x},{y}): alpha {alpha}, expected {expected:F1}");
            }
        return (checkedPixels, worst, problems);
    }

    /// <summary>Every transform, shadow and scale: each checked pixel within <see cref="Tolerance"/> of the device blur.</summary>
    [Theory]
    [MemberData(nameof(Cases))]
    public void TheShadowFallsOffWithOneDeviceSigma(string transformName, string shadowName, float scale)
    {
        var shadow = Shadows[shadowName];
        var placement = Placement(Transforms[transformName], new Vector2(Size / 2f), scale);
        var toDevice = placement * Matrix3x2.CreateScale(scale);
        var image = Render(Size, placement, scale, shadow);
        var sigma = DeviceSigma(shadow.Blur / 2, toDevice);
        var (checkedPixels, worst, problems) = Check(image, Size, toDevice, shadow, sigma);
        Assert.True(checkedPixels >= 100, $"only {checkedPixels} pixels checked");
        Assert.True(problems.Count == 0, $"{shadowName} under {transformName} at {scale} (device sigma {sigma:F2}, worst {worst:F1}):\n" + string.Join("\n", problems));
    }

    /// <summary>
    /// An inset shadow whose spread leaves no hole fills the padding box with its colour, unblurred
    /// (BoxPainterBase::PaintInsetBoxShadow: an empty inner rect is filled with FillContouredRect, no frame and
    /// no looper): a 100 × 40 box with a 25 px spread, a 12 px blur and an offset, under scaleX(0.5).
    /// </summary>
    [Fact]
    public void AnInsetShadowWithNoHoleLeftFillsTheBox()
    {
        const float scale = 1;
        var shadow = new BoxShadow(new Vector2(3, 2), 12, 25, Color.FromBytes(0, 0, 0), Inset: true);
        var placement = Placement(Matrix3x2.CreateScale(0.5f, 1), new Vector2(Size / 2f), scale);
        var image = Render(Size, placement, scale, shadow);
        var box = new Shape(new RectF(0, 0, Box.X, Box.Y), placement * Matrix3x2.CreateScale(scale));
        var checkedPixels = 0;
        var problems = new List<string>();
        for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                if (box.Outside(new Vector2(x + 0.5f, y + 0.5f)) > -1)
                    continue;
                checkedPixels++;
                var alpha = image.Pixels[(y * Size + x) * 4 + 3];
                if (alpha < 254 && problems.Count < 12)
                    problems.Add($"({x},{y}): alpha {alpha}");
            }
        Assert.True(checkedPixels >= 1000, $"only {checkedPixels} pixels checked");
        Assert.True(problems.Count == 0, "the box isn't filled:\n" + string.Join("\n", problems));
    }

    /// <summary>
    /// Skia caps the device sigma at 128 (computeXformedSigma's kMaxBlurSigma): a 300 px blur at 1.65 under
    /// scaleX(1.2) (σ 150 × 1.65 × √1.2 ≈ 271 px wanted) falls off like σ 128.
    /// </summary>
    [Fact]
    public void TheDeviceSigmaIsAtMost128()
    {
        const int size = 1100;
        const float scale = 1.65f;
        var shadow = new BoxShadow(Vector2.Zero, 300, 0, Color.FromBytes(0, 0, 0));
        var placement = Placement(Matrix3x2.CreateScale(1.2f, 1), new Vector2(size / 2f), scale);
        var toDevice = placement * Matrix3x2.CreateScale(scale);
        Assert.True(DeviceSigma(150, toDevice) == 128, "the case must be past the cap");
        var image = Render(size, placement, scale, shadow);
        var (checkedPixels, worst, problems) = Check(image, size, toDevice, shadow, 128);
        Assert.True(checkedPixels >= 1000, $"only {checkedPixels} pixels checked");
        Assert.True(problems.Count == 0, $"a 300 px blur at 1.65 under scaleX(1.2) (worst {worst:F1}):\n" + string.Join("\n", problems));
    }
}
