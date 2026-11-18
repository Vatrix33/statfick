using System.Text;
using Statventure.Imaging;
using Statventure.Render3D;

namespace Statventure.Tests.Render3D;

// The comparisons of a render with Blender's, a part of Render3DReferenceTests in a file of its own (GM2 step 5) so the
// game's tests link it with ReferenceLimits.cs: the looks the game builds into figures are held to Blender's assembly of
// them as the engine's scenes are (Tests/Statventure.Game.Tests/Figures/AssemblyTests.cs).
partial class Render3DReferenceTests
{
    /// <summary>Line coverage 0..1: Freestyle's alpha, or the engine's white-on-black coverage (sRGB-encoded in the target).</summary>
    internal static float[] InkCoverage(RgbaImage image, bool alpha)
    {
        var coverage = new float[image.Width * image.Height];
        for (var i = 0; i < coverage.Length; i++)
            coverage[i] = alpha ? image.Pixels[i * 4 + 3] / 255f : Colors.ToLinear(image.Pixels[i * 4] / 255f);
        return coverage;
    }

    /// <summary>
    /// A scene with cast shadows (Docs/Renderer-3D.md, "Shadows"). Pixels on the geometry's edges (as
    /// the engine draws the scene without shadows) are compared as in <see cref="Compare"/>. Inside
    /// surfaces the shadows' soft fringes are a sampled average, Blender's of 64 jittered maps at its
    /// own resolution, the engine's of 16 maps at 1024²: single pixels in a fringe differ by up to
    /// about 10 levels (the engine's blend steps by sixteenths), and the thinnest shadows wander. So,
    /// inside surfaces: at most 0.2% of pixels differ by more than 16 levels (measured: at most
    /// 0.15%), and the difference left over is at most a tenth of the shadows' own (the summed
    /// difference from Blender over the summed difference between Blender and the engine without
    /// shadows; measured: at most 0.091, in the fringe scene). On the geometry's edges, one pixel in
    /// 10,000 may stray (measured: up to 7, on the terrain): scenery shades a sub-pixel sliver of a
    /// band edge or a contact shadow once per pixel where Blender shades each sample. Checked: hard
    /// shadows, a sun 30% too small or too large, shadows on characters and no shadows each fail.
    /// Those are M6's limits (<see cref="ReferenceLimits.M6"/>); a detailed stage's are its own.
    /// </summary>
    internal static string CompareShadowed(RgbaImage expected, RgbaImage actual, RgbaImage unshadowed, ReferenceLimits? limits = null)
    {
        limits ??= ReferenceLimits.M6;
        int w = expected.Width, h = expected.Height;
        byte[] e = expected.Pixels, a = actual.Pixels, u = unshadowed.Pixels;
        var onGeometry = new bool[w * h];
        long inside = 0, far = 0;
        double left = 0, signal = 0;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 4;
                var uniform = true;
                for (var dy = -1; dy <= 1 && uniform; dy++)
                    for (var dx = -1; dx <= 1 && uniform; dx++)
                    {
                        var j = (Math.Clamp(y + dy, 0, h - 1) * w + Math.Clamp(x + dx, 0, w - 1)) * 4;
                        uniform = u[j] == u[i] && u[j + 1] == u[i + 1] && u[j + 2] == u[i + 2];
                    }
                if (!uniform)
                {
                    onGeometry[y * w + x] = true;
                    continue;
                }
                int difference = 0, shadow = 0;
                for (var c = 0; c < 3; c++)
                {
                    difference = Math.Max(difference, Math.Abs(e[i + c] - a[i + c]));
                    shadow = Math.Max(shadow, Math.Abs(e[i + c] - u[i + c]));
                }
                inside++;
                if (difference > 16)
                    far++;
                left += difference;
                signal += shadow;
            }
        var text = new StringBuilder(Compare(expected, actual, edgeMean: limits.EdgeMean, only: (x, y) => onGeometry[y * w + x], strays: w * h / limits.StrayEvery));
        if (far > limits.FarInside * inside)
            text.Append($" {far} of {inside} pixels inside surfaces differ by more than 16");
        if (signal > 0 && left / signal > limits.LeftOver)
            text.Append($" {left / signal:P1} of the shadows' difference left over");
        return text.ToString().Trim();
    }

    /// <summary>
    /// Ink lines against Freestyle's, which strokes edges where the engine finds them per pixel
    /// (Docs/Renderer-3D.md, "Ink lines"). Measured over the reference scenes: every Freestyle line
    /// pixel (coverage ≥ 0.5) has an engine line pixel within one pixel but for 0.15%; engine line
    /// pixels away from any Freestyle line are at most 2.9% (where surfaces meet seen almost edge-on,
    /// the fin on the bent tube or a hill's far slope going into the lake: there the engine can't tell
    /// two surfaces crossing from a silhouette; and a corner where a part without ink meets another);
    /// the total ink is within 2.4%; coverage differs by 0.03-0.08 on average over line pixels; around
    /// a Freestyle line pixel, the engine's ink in the 7×7 pixels about it is 0.75 to 1.33 times
    /// Freestyle's but for 1.1% (line ends, corners). Checked: missing creases, lines 20%
    /// thin, lines on parts without ink, and outlines only against the background (a model over
    /// another gets an inner line: 8.5% of line pixels out of the local range) each fail one of:
    /// misses ≤ 2%, extras ≤ 3%, total ink within 10%, mean coverage difference ≤ 0.12, local ink
    /// out of range ≤ 3%; and a crest before the slope behind it (a fold within a part seen almost
    /// edge-on) taken for a surface running on fails terrain-oblique (3.5% of its line pixels missed).
    /// Those are M6's limits (<see cref="ReferenceLimits.M6"/>); a detailed stage's are its own.
    /// </summary>
    internal static string CompareInk(float[] expected, float[] actual, int width, ReferenceLimits? limits = null)
    {
        limits ??= ReferenceLimits.M6;
        var height = expected.Length / width;
        float Near(float[] map, int x, int y)
        {
            var best = 0f;
            for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                    best = Math.Max(best, map[Math.Clamp(y + dy, 0, height - 1) * width + Math.Clamp(x + dx, 0, width - 1)]);
            return best;
        }
        int lines = 0, engineLines = 0, missed = 0, extra = 0, covered = 0;
        double ink = 0, engineInk = 0, difference = 0;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var i = y * width + x;
                float e = expected[i], a = actual[i];
                ink += e;
                engineInk += a;
                if (e > 0.02f || a > 0.02f)
                {
                    covered++;
                    difference += Math.Abs(e - a);
                }
                if (e >= 0.5f)
                {
                    lines++;
                    if (Near(actual, x, y) < 0.5f)
                        missed++;
                }
                if (a >= 0.5f)
                {
                    engineLines++;
                    if (Near(expected, x, y) < 0.5f)
                        extra++;
                }
            }
        // The local width: ink in the 7×7 pixels about each Freestyle line pixel, from summed-area tables.
        double[] Sums(float[] map)
        {
            var sums = new double[(width + 1) * (height + 1)];
            for (var y = 0; y < height; y++)
            {
                double row = 0;
                for (var x = 0; x < width; x++)
                {
                    row += map[y * width + x];
                    sums[(y + 1) * (width + 1) + x + 1] = sums[y * (width + 1) + x + 1] + row;
                }
            }
            return sums;
        }
        var (expectedSums, actualSums) = (Sums(expected), Sums(actual));
        double Box(double[] sums, int x, int y)
        {
            int x0 = Math.Max(0, x - 3), y0 = Math.Max(0, y - 3), x1 = Math.Min(width, x + 4), y1 = Math.Min(height, y + 4);
            return sums[y1 * (width + 1) + x1] - sums[y0 * (width + 1) + x1] - sums[y1 * (width + 1) + x0] + sums[y0 * (width + 1) + x0];
        }
        var unlike = 0;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                if (expected[y * width + x] >= 0.5f && Box(actualSums, x, y) / Box(expectedSums, x, y) is < 0.75 or > 1.33)
                    unlike++;
        var text = new StringBuilder();
        if (unlike > limits.Unlike * lines)
            text.Append($"{unlike} of {lines} line pixels with unlike ink about them ");
        if (missed > limits.Missed * lines)
            text.Append($"{missed} of {lines} line pixels missed ");
        if (extra > limits.Extra * engineLines)
            text.Append($"{extra} of {engineLines} line pixels where Freestyle has none ");
        if (ink == 0 ? engineInk > 0 : Math.Abs(engineInk / ink - 1) > limits.TotalInk)
            text.Append($"ink {engineInk:F0} against {ink:F0} ");
        if (covered > 0 && difference / covered > limits.Coverage)
            text.Append($"coverage differs by {difference / covered:F3} on average ");
        return text.ToString().Trim();
    }

    /// <summary>
    /// The frame's outermost pixels aren't compared: Blender's jittered samples reach past the frame,
    /// so an edge just outside shades them. Inside (the reference's 3×3 neighbourhood is one colour):
    /// every channel within 2. On an edge,
    /// MSAA 4× covers a pixel in quarters and resolves it alone, where Blender filters 64 samples
    /// across neighbouring pixels: they differ by up to 0.61 of the edge's local contrast (the 3×3
    /// neighbourhood's range), 3.1 levels on average. An edge half a pixel out differs by about 9 on
    /// average; a missing or extra feature by about the whole contrast. So each edge pixel within 0.7
    /// of its local contrast (or 8 levels), and all of them within 4 on average. Returns what failed,
    /// or "". Stray pixels may be allowed: `strays` of either kind together (a shadowed scene's edges), or `insideStrays`
    /// inside and `edgeStrays` on edges apart (a profile's <see cref="ReferenceLimits.InsideEvery"/> and
    /// <see cref="ReferenceLimits.EdgeEvery"/>; none under M6).
    /// </summary>
    internal static string Compare(RgbaImage expected, RgbaImage actual, int insideTolerance = 2, double edgeContrast = 0.7, int edgeFloor = 8, double edgeMean = 4,
        Func<int, int, bool>? only = null, int strays = 0, int insideStrays = 0, int edgeStrays = 0)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
            return $"size {actual.Width}x{actual.Height}, reference {expected.Width}x{expected.Height}";
        int w = expected.Width, h = expected.Height;
        byte[] e = expected.Pixels, a = actual.Pixels;
        int insideBad = 0, edgeBad = 0, worstInside = 0, worstEdge = 0, edgeCount = 0;
        long edgeSum = 0;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                if (only != null && !only(x, y))
                    continue;
                // The frame's outermost pixels: Blender's jittered samples reach past the frame (an
                // edge just outside shades them), the engine's stay inside.
                if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                    continue;
                var i = (y * w + x) * 4;
                Span<int> low = [255, 255, 255], high = [0, 0, 0];
                for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        int nx = Math.Clamp(x + dx, 0, w - 1), ny = Math.Clamp(y + dy, 0, h - 1);
                        var j = (ny * w + nx) * 4;
                        for (var c = 0; c < 3; c++)
                        {
                            low[c] = Math.Min(low[c], e[j + c]);
                            high[c] = Math.Max(high[c], e[j + c]);
                        }
                    }
                var contrast = Math.Max(high[0] - low[0], Math.Max(high[1] - low[1], high[2] - low[2]));
                var uniform = contrast == 0;
                var diff = 0;
                for (var c = 0; c < 3; c++)
                    diff = Math.Max(diff, Math.Abs(e[i + c] - a[i + c]));
                if (uniform)
                {
                    worstInside = Math.Max(worstInside, diff);
                    if (diff > insideTolerance)
                        insideBad++;
                }
                else
                {
                    worstEdge = Math.Max(worstEdge, diff);
                    edgeCount++;
                    edgeSum += diff;
                    if (diff > edgeFloor && diff > edgeContrast * contrast)
                        edgeBad++;
                }
            }
        var text = new StringBuilder();
        if (insideBad + edgeBad <= strays)
            insideBad = edgeBad = 0;
        if (insideBad <= insideStrays)
            insideBad = 0;
        if (edgeBad <= edgeStrays)
            edgeBad = 0;
        if (insideBad > 0)
            text.Append($"{insideBad} inside pixels differ (worst {worstInside}) ");
        if (edgeBad > 0)
            text.Append($"{edgeBad} edge pixels differ beyond {edgeContrast} of their contrast (worst {worstEdge}) ");
        if (edgeCount > 0 && (double)edgeSum / edgeCount > edgeMean)
            text.Append($"edges differ by {(double)edgeSum / edgeCount:F1} on average ");
        return text.ToString().Trim();
    }

    /// <summary>A scene without shadows under a profile's limits: its stray inside and edge pixels and its edges' mean.</summary>
    internal static string Compare(RgbaImage expected, RgbaImage actual, ReferenceLimits limits) =>
        Compare(expected, actual, edgeMean: limits.EdgeMean, insideStrays: (int)((long)expected.Width * expected.Height / limits.InsideEvery),
            edgeStrays: (int)((long)expected.Width * expected.Height / limits.EdgeEvery));
}
