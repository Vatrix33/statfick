using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Xunit.Abstractions;

namespace Statventure.Tests.Scene;

/// <summary>
/// The engine's F-curves evaluate as Blender 5.2 evaluates the same curves, and work out the handles
/// Blender's update leaves (Golden/curve-&lt;scene&gt;.json, from Tools/Scripts/make-blender-references.sh,
/// which builds Reference/curve-scenes.json's curves as Blender keyframes with
/// Tools/BlenderReference/evaluate_curves.py). Blender holds a curve's numbers as float32, so the
/// engine's curve for the comparison is made from the same float32 numbers (the scenes' numbers are
/// float32 already, except Blender's defaults for back and the elastic period) and evaluated at the
/// float32 frames Blender was given. What is left is Blender's float32 arithmetic against the engine's
/// double (Q5), bounded below (<see cref="Allowance"/>).
/// </summary>
public class FCurveReferenceTests(ITestOutputHelper output)
{
    /// <summary>Float32's unit roundoff, 2⁻²⁴.</summary>
    const double Unit = 1.0 / (1 << 24);

    static string GoldenDirectory => Path.GetDirectoryName(Golden.GoldenPath("x"))!;
    static string ReferenceDirectory => Path.Combine(GoldenDirectory, "..", "Reference");

    public static TheoryData<string> SceneNames => new(FCurveScenes.All.Select(s => s.Name));

    static string Camel(Enum value)
    {
        var name = value.ToString();
        return char.ToLowerInvariant(name[0]) + name[1..];
    }

    [Fact]
    public void TheSceneFileIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "curve-scenes.json");
        var text = JsonSerializer.Serialize(new
        {
            scenes = FCurveScenes.All.Select(scene => new
            {
                name = scene.Name,
                curves = scene.Curves.Select(c => new
                {
                    name = c.Name,
                    extrapolation = Camel(c.Curve.Extrapolation),
                    smoothing = Camel(c.Curve.Smoothing),
                    keys = c.Curve.Keys.Select(k => new
                    {
                        time = k.Time, value = k.Value, interpolation = Camel(k.Interpolation), easing = Camel(k.Easing),
                        left = Camel(k.Left), right = Camel(k.Right), leftTime = k.LeftTime, leftValue = k.LeftValue,
                        rightTime = k.RightTime, rightValue = k.RightValue, back = k.Back, amplitude = k.Amplitude, period = k.Period,
                    }),
                    times = c.Times,
                }),
            }),
        }, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }) + "\n";
        // A curve's times on one line.
        text = Regex.Replace(text, @"\[\s+(-?[0-9][-0-9.eE+,\s]*?)\s+\]", m => "[" + Regex.Replace(m.Groups[1].Value, @",\s+", ", ") + "]");
        if (!File.Exists(path) || File.ReadAllText(path) != text)
            File.WriteAllText(path, text);
        Assert.NotEmpty(FCurveScenes.All);
    }

    static double F32(double value) => (float)value;

    static double? F32(double? value) => value is { } v ? (float)v : null;

    /// <summary>A time in seconds as Blender holds it: its frame as a float32.</summary>
    static double Time32(double seconds) => (float)(seconds * FCurve.FramesPerSecond) / FCurve.FramesPerSecond;

    static double? Time32(double? seconds) => seconds is { } s ? Time32(s) : null;

    /// <summary>The curve with every number as Blender holds it.</summary>
    internal static FCurve AsBlenderHoldsIt(FCurve curve) => curve with
    {
        Keys = curve.Keys.Select(k => k with
        {
            Time = Time32(k.Time), Value = F32(k.Value), LeftTime = Time32(k.LeftTime), LeftValue = F32(k.LeftValue),
            RightTime = Time32(k.RightTime), RightValue = F32(k.RightValue), Back = F32(k.Back), Amplitude = F32(k.Amplitude), Period = Time32(k.Period),
        }).ToArray(),
    };

    static JsonNode Load(string scene) => JsonNode.Parse(File.ReadAllText(Path.Combine(GoldenDirectory, $"curve-{scene}.json")))!;

    /// <summary>
    /// What Blender's float32 arithmetic may put a value off by at <paramref name="frame"/>, in units of
    /// float32's roundoff. Each operation Blender rounds costs at most one unit of the size of what it
    /// holds, and a value is a short chain of them: at most 16, counted generously (elastic's, the
    /// longest, has 14). What they hold is of two sizes: values (the segment's keys and handles and the
    /// result, V; for an elastic one its amplitude), and times, whose rounding moves the curve along by
    /// up to X + 3L frames' worth of roundoff and so the value by its slope v′ times that: X the largest
    /// key frame (the frames Blender holds and subtracts), 3L for the segment's parameter (a Bezier's time
    /// runs at most 3L per unit of it, an easing's normalised time and elastic's phase within L, the
    /// segment's length). The slope is measured inside the segment, clear of the keys' search threshold,
    /// so a jump at a key never widens it. Outside the keys the line's slope comes from rounded handles h
    /// frames long (or the neighbouring key), so its run is off by X/h of itself.
    /// </summary>
    internal static double Allowance(FCurve curve, double frame)
    {
        const double roundings = 16;
        var keys = curve.Keys;
        var handles = curve.Handles;
        var n = keys.Count;
        var x = keys.Select(k => k.Time * FCurve.FramesPerSecond).ToArray();
        var X = Math.Max(1, x.Max(Math.Abs));
        double At(double f) => curve.Evaluate(f / FCurve.FramesPerSecond);
        var result = Math.Abs(At(frame));
        if (frame <= x[0] || frame >= x[n - 1])
        {
            var end = frame <= x[0] ? 0 : n - 1;
            var run = Math.Abs(frame - x[end]);
            var h = handles[end];
            var handleLength = Math.Max(Math.Abs((end == 0 ? h.LeftTime : h.RightTime) * FCurve.FramesPerSecond - x[end]), n > 1 ? Math.Abs(x[1] - x[0]) : 1);
            var line = run > 0 ? Math.Abs(result - keys[end].Value) / run : 0;
            return roundings * (Math.Max(Math.Abs(keys[end].Value), result) + line * run * (1 + X / handleLength));
        }
        var a = Array.FindIndex(x, v => v > frame);
        var p = a - 1;
        var length = x[a] - x[p];
        var V = new[] { keys[p].Value, keys[a].Value, handles[p].RightValue, handles[a].LeftValue, result }.Max(Math.Abs);
        if (keys[p].Interpolation == KeyInterpolation.Elastic)
            V = Math.Max(V, keys[p].Amplitude + Math.Abs(keys[p].Value));
        const double clear = 0.001;
        var step = 1e-4 * length;
        double low = Math.Clamp(frame - step, x[p] + clear, x[a] - clear), high = Math.Clamp(frame + step, x[p] + clear, x[a] - clear);
        var slope = high > low ? Math.Abs(At(high) - At(low)) / (high - low) : 0;
        return roundings * (V + slope * (X + 3 * length));
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void CurvesEvaluateAsBlenderEvaluatesThem(string name)
    {
        var scene = FCurveScenes.All.Single(s => s.Name == name);
        var reference = Load(name)["curves"]!.AsArray();
        Assert.Equal(scene.Curves.Length, reference.Count);
        double worst = 0;
        var where = "";
        foreach (var (c, golden) in scene.Curves.Zip(reference))
        {
            Assert.Equal(c.Name, (string)golden!["name"]!);
            var values = golden["values"]!.AsArray().Select(v => (double)v!).ToArray();
            Assert.Equal(c.Times.Length, values.Length);
            var curve = AsBlenderHoldsIt(c.Curve);
            for (var i = 0; i < c.Times.Length; i++)
            {
                var frame = (double)(float)(c.Times[i] * FCurve.FramesPerSecond);
                var value = curve.Evaluate(frame / FCurve.FramesPerSecond);
                var difference = Math.Abs(value - values[i]);
                var ratio = difference == 0 ? 0 : difference / (Unit * Allowance(curve, frame));
                if (ratio > worst)
                    (worst, where) = (ratio, $"{c.Name} at frame {frame:R}: engine {value:R}, Blender {values[i]:R}");
                Assert.True(ratio <= 1, $"{c.Name} at frame {frame:R}: engine {value:R}, Blender {values[i]:R}, off by {ratio:F2} of the allowance");
            }
        }
        output.WriteLine($"{name}: at most {worst:F4} of the allowance ({where})");
    }

    /// <summary>
    /// The handles: the stored ones after Blender's fixes and the worked-out ones, each coordinate within
    /// 16 roundings of the size of the numbers it is made from (the key's and its neighbours' frames and
    /// values and the handle's own), times the smoothing system's growth of rounding where the curve
    /// is smoothed (at most 8 here: diagonally dominant rows, l² + 1/l' below 2(l + 1) for the spacing).
    /// </summary>
    [Theory]
    [MemberData(nameof(SceneNames))]
    public void HandlesAreWhereBlenderPutsThem(string name)
    {
        var scene = FCurveScenes.All.Single(s => s.Name == name);
        var reference = Load(name)["curves"]!.AsArray();
        double worst = 0;
        var where = "";
        foreach (var (c, golden) in scene.Curves.Zip(reference))
        {
            var curve = AsBlenderHoldsIt(c.Curve);
            var handles = golden!["handles"]!.AsArray().Select(h => h!.AsArray().Select(v => (double)v!).ToArray()).ToArray();
            Assert.Equal(curve.Keys.Count, handles.Length);
            var growth = curve.Smoothing == CurveSmoothing.ContinuousAcceleration ? 8 : 1;
            for (var i = 0; i < handles.Length; i++)
            {
                var engine = curve.Handles[i];
                var nearby = Enumerable.Range(Math.Max(0, i - 1), Math.Min(curve.Keys.Count, i + 2) - Math.Max(0, i - 1)).Select(j => curve.Keys[j]).ToArray();
                var frames = nearby.Max(k => Math.Abs(k.Time)) * 60;
                var values = nearby.Max(k => Math.Abs(k.Value));
                double[] mine = [engine.LeftTime * 60, engine.LeftValue, engine.RightTime * 60, engine.RightValue];
                double[] theirs = [handles[i][0] * 60, handles[i][1], handles[i][2] * 60, handles[i][3]];
                for (var j = 0; j < 4; j++)
                {
                    var size = Math.Max(1, j % 2 == 0 ? Math.Max(frames, Math.Abs(theirs[j])) : Math.Max(values, Math.Abs(theirs[j])));
                    var ratio = Math.Abs(mine[j] - theirs[j]) / (Unit * 16 * growth * size);
                    var what = $"{c.Name} key {i} {(j < 2 ? "left" : "right")} {(j % 2 == 0 ? "frame" : "value")}: engine {mine[j]:R}, Blender {theirs[j]:R}";
                    if (ratio > worst)
                        (worst, where) = (ratio, what);
                    Assert.True(ratio <= 1, $"{what}, off by {ratio:F2} of the allowance");
                }
            }
        }
        output.WriteLine($"{name}: at most {worst:F4} of the allowance ({where})");
    }
}
