using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Tests.Infrastructure;
using Path = Statventure.Canvas.Path;

namespace Statventure.Tests.Canvas;

public class ColorAndGeometryTests
{
    [Fact]
    public void HexColoursParseInEveryForm()
    {
        Assert.Equal(Color.FromBytes(255, 0, 0), Color.FromHex("#f00"));
        Assert.Equal(Color.FromBytes(0x13, 0x0e, 0x0b), Color.FromHex("#130e0b"));
        Assert.Equal(Color.FromBytes(255, 255, 255, 0x80), Color.FromHex("#ffffff80"));
        Assert.Equal("#c9533a", Color.FromHex("#C9533A").ToHex());
    }

    [Fact]
    public void ColoursMixInPremultipliedSpaceLikeCss()
    {
        // Half way from opaque red to transparent is half-transparent red, not darkened.
        var mixed = Color.Lerp(new Color(1, 0, 0), Color.Transparent, 0.5f);
        Assert.Equal(1f, mixed.R, 3);
        Assert.Equal(0.5f, mixed.A, 3);
    }

    [Fact]
    public void RectanglesIntersectAndTransform()
    {
        var a = new RectF(0, 0, 10, 10);
        Assert.Equal(new RectF(5, 5, 5, 5), a.Intersect(new RectF(5, 5, 10, 10)));
        Assert.True(a.Intersect(new RectF(20, 20, 5, 5)).IsEmpty);
        var rotated = a.Transform(Matrix3x2.CreateRotation(MathF.PI / 4));
        Assert.Equal(MathF.Sqrt(200), rotated.Width, 3);
    }

    [Fact]
    public void ColorMatricesFollowTheFilterEffectsSpecification()
    {
        var red = new Vector4(1, 0, 0, 1);
        var gray = ColorMatrix.For(Filter.Grayscale(1)).Apply(red);
        Assert.Equal(0.2126f, gray.X, 3);
        Assert.Equal(0.2126f, gray.Y, 3);
        var bright = ColorMatrix.For(Filter.Brightness(2)).Apply(new Vector4(0.25f, 0.5f, 0.1f, 1));
        Assert.Equal(new Vector4(0.5f, 1f, 0.2f, 1), bright);
        var half = ColorMatrix.For(Filter.Opacity(0.5f)).Apply(red);
        Assert.Equal(0.5f, half.W, 3);
        var chained = ColorMatrix.Then(ColorMatrix.For(Filter.Brightness(2)), ColorMatrix.For(Filter.Brightness(0.5f))).Apply(red);
        Assert.Equal(red, chained);
    }

    [Fact]
    public void CornerRadiiShrinkWhenTheyDoNotFit()
    {
        var radii = new CornerRadii(60).Fit(new Vector2(100, 40));
        Assert.Equal(20f, radii.TopLeft.X, 3);
        Assert.Equal(20f, radii.TopLeft.Y, 3);
    }
}

public class PathTests
{
    static byte[] Rasterize(Path path, int size, FillRule rule = FillRule.NonZero) =>
        PathRasterizer.Fill(path.Flatten(Matrix3x2.Identity).Select(c => (IReadOnlyList<Vector2>)c.Points), size, size, rule);

    [Fact]
    public void ASquareCoversExactlyItsArea()
    {
        var coverage = Rasterize(Path.Rectangle(new RectF(2.5f, 2.5f, 10, 10)), 16);
        var area = coverage.Sum(b => b / 255.0);
        Assert.Equal(100.0, area, 0);
        Assert.InRange(coverage[2 * 16 + 2], 60, 68);    // a quarter-covered corner pixel
        Assert.InRange(coverage[2 * 16 + 5], 125, 130);  // a half-covered edge pixel
        Assert.Equal(255, coverage[5 * 16 + 5]);
    }

    [Fact]
    public void ACircleCoversPiRSquared()
    {
        // Curves are flattened to within 0.1 px, so the area is a little under pi r^2.
        var coverage = Rasterize(Path.Circle(new Vector2(20, 20), 15), 40);
        Assert.InRange(coverage.Sum(b => b / 255.0), Math.PI * 225 * 0.99, Math.PI * 225);
    }

    [Fact]
    public void EvenOddLeavesTheStarCentreEmpty()
    {
        var star = Path.ParseSvg("M60 5 L95 110 L5 40 L115 40 L25 110 Z");
        var nonzero = Rasterize(star, 120);
        var evenOdd = Rasterize(star, 120, FillRule.EvenOdd);
        Assert.Equal(255, nonzero[60 * 120 + 60]);
        Assert.Equal(0, evenOdd[60 * 120 + 60]);
    }

    [Fact]
    public void SvgPathDataParsesRelativeAndShorthandCommands()
    {
        var a = Path.ParseSvg("M10 10 h20 v20 h-20 z");
        var b = Path.ParseSvg("M10,10 L30,10 L30,30 L10,30 Z");
        Assert.Equal(b.ToString(), a.ToString());
        // S reflects the previous control point (20,10) through the current point (20,20).
        var smooth = Path.ParseSvg("M0 0 C10 0 20 10 20 20 S30 40 40 40");
        Assert.Equal("M0 0 C10 0 20 10 20 20 C20 30 30 40 40 40", smooth.ToString());
        var exponents = Path.ParseSvg("M1e1 -5E-1 l.5.5");
        Assert.Equal("M10 -0.5 L10.5 0", exponents.ToString());
    }

    [Fact]
    public void ArcsEndExactlyWhereTheyShould()
    {
        var path = Path.ParseSvg("M10 60 A50 50 0 0 1 110 60");
        var last = path.Flatten(Matrix3x2.Identity)[0].Points[^1];
        Assert.Equal(110f, last.X, 3);
        Assert.Equal(60f, last.Y, 3);
        // A half circle above the chord: its top is 50 above.
        var top = path.Flatten(Matrix3x2.Identity)[0].Points.Min(p => p.Y);
        Assert.Equal(10f, top, 1);
    }

    [Fact]
    public void StrokesCoverWidthTimesLength()
    {
        var line = Path.ParseSvg("M5 10 L35 10");
        var polygons = PathRasterizer.Stroke(line.Flatten(Matrix3x2.Identity), new StrokeStyle(4));
        var coverage = PathRasterizer.Fill(polygons, 40, 20, FillRule.NonZero);
        Assert.Equal(120.0, coverage.Sum(b => b / 255.0), 0);
        var squared = PathRasterizer.Stroke(line.Flatten(Matrix3x2.Identity), new StrokeStyle(4, Cap: LineCap.Square));
        Assert.Equal(136.0, PathRasterizer.Fill(squared, 40, 20, FillRule.NonZero).Sum(b => b / 255.0), 0);
    }
}

[Collection("GPU")]
public class AtlasTests(GpuFixture gpu)
{
    [Fact]
    public void EntriesArePackedWithoutOverlapAndClearingStartsAGeneration()
    {
        using var atlas = new TextureAtlas(gpu.Device, pageSize: 128, maxPages: 1);
        var entries = Enumerable.Range(0, 20).Select(i => atlas.Add(20, 10 + i % 5, new byte[20 * (10 + i % 5)])).ToList();
        for (var i = 0; i < entries.Count; i++)
            for (var j = i + 1; j < entries.Count; j++)
            {
                var a = entries[i];
                var b = entries[j];
                var overlap = a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;
                Assert.False(overlap, $"entries {i} and {j} overlap");
            }
        var generation = atlas.Generation;
        for (var i = 0; i < 40; i++)
            atlas.Add(60, 60, new byte[3600]);
        Assert.True(atlas.Generation > generation);
    }
}
