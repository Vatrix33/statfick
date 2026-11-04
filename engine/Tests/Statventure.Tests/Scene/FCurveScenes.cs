using Statventure.Scene;

namespace Statventure.Tests.Scene;

/// <summary>One curve of a scene, the times (seconds) it is evaluated at.</summary>
public sealed record CurveCase(string Name, FCurve Curve, double[] Times);

/// <summary>A group of curves Blender evaluates together (Golden/curve-&lt;name&gt;.json).</summary>
public sealed record CurveScene(string Name, CurveCase[] Curves);

/// <summary>
/// The F-curves checked against Blender 5.2 (Docs/Scene.md, "How it works", step 2): every
/// interpolation, easing and handle type, both smoothing modes and extrapolations, keys at odd
/// spacing, coincident handles and the key search's threshold. Key times sit on frames or halves of
/// them and values, handles and parameters are numbers a float holds exactly, so Blender (which holds
/// them as float32) evaluates the same curve; written to Reference/curve-scenes.json, evaluated by
/// Tools/BlenderReference/evaluate_curves.py.
/// </summary>
public static class FCurveScenes
{
    /// <summary>Seconds of a frame at 60 fps.</summary>
    public static double F(double frame) => frame / FCurve.FramesPerSecond;

    static readonly KeyInterpolation[] Easings =
    [
        KeyInterpolation.Sine, KeyInterpolation.Quad, KeyInterpolation.Cubic, KeyInterpolation.Quart, KeyInterpolation.Quint,
        KeyInterpolation.Expo, KeyInterpolation.Circ, KeyInterpolation.Back, KeyInterpolation.Bounce, KeyInterpolation.Elastic,
    ];

    /// <summary>
    /// The frames a curve is evaluated at: every half frame between its keys and every frame for 8
    /// before and after them, every key and a quarter and an eighth to either side, and frames just
    /// inside and just outside the key search's 0.0001-frame threshold on both sides of every key.
    /// </summary>
    public static double[] TimesFor(FCurve curve)
    {
        var frames = new SortedSet<double>();
        double first = curve.Start * 60, last = curve.End * 60;
        for (var f = Math.Floor(first) - 8; f <= Math.Ceiling(last) + 8; f += f < first || f > last ? 1 : 0.5)
            frames.Add(f);
        foreach (var key in curve.Keys)
        {
            var x = key.Time * 60;
            foreach (var offset in new[] { 0, 0.125, -0.125, 0.25, -0.25, 0.00005, -0.00005, 0.0003, -0.0003 })
                frames.Add(x + offset);
        }
        return frames.Select(F).ToArray();
    }

    static CurveCase Case(string name, FCurve curve) => new(name, curve, TimesFor(curve));

    static FCurve Curve(CurveExtrapolation extrapolation, CurveSmoothing smoothing, params FCurveKey[] keys) => new(keys, extrapolation, smoothing);

    static FCurve Curve(params FCurveKey[] keys) => new(keys);

    /// <summary>Four keys at odd spacing, every key of one interpolation.</summary>
    static FCurve Same(KeyInterpolation interpolation, CurveExtrapolation extrapolation = CurveExtrapolation.Constant) =>
        new([new(0, 1, interpolation), new(F(21), -0.5, interpolation), new(F(36), 2, interpolation), new(F(60), 0.25, interpolation)],
            extrapolation);

    static CurveScene Interpolation() => new("interpolation",
    [
        Case("constant", Same(KeyInterpolation.Constant)),
        Case("linear", Same(KeyInterpolation.Linear)),
        Case("bezier", Same(KeyInterpolation.Bezier)),
        Case("mixed", Curve(new FCurveKey(F(3), 0.5, KeyInterpolation.Constant), FCurveKey.Linear(F(14), 1.75), new FCurveKey(F(29), -1),
            FCurveKey.Ease(F(40), 2.5, KeyInterpolation.Sine, KeyEasing.InOut), FCurveKey.Ease(F(52), 0, KeyInterpolation.Bounce),
            FCurveKey.Constant(F(61), 1.5), FCurveKey.Linear(F(70), 0.75))),
    ]);

    /// <summary>Every easing type in every mode, rising then falling, with the default parameters.</summary>
    static CurveScene Easing()
    {
        var cases = new List<CurveCase>();
        foreach (var type in Easings)
            foreach (var mode in Enum.GetValues<KeyEasing>())
                cases.Add(Case($"{type}-{mode}".ToLowerInvariant(), Curve(
                    FCurveKey.Ease(0, 0.5, type, mode), FCurveKey.Ease(F(25), 2, type, mode), FCurveKey.Ease(F(40), -1, type, mode), new FCurveKey(F(52), -0.25))));
        return new("easing", [.. cases]);
    }

    /// <summary>Back's overshoot and elastic's amplitude and period, each branch of their rules.</summary>
    static CurveScene Parameters()
    {
        FCurve Back(double back) => Curve(
            FCurveKey.Ease(0, 0, KeyInterpolation.Back, KeyEasing.In, back: back), FCurveKey.Ease(F(20), 1.5, KeyInterpolation.Back, KeyEasing.Out, back: back),
            FCurveKey.Ease(F(38), -0.5, KeyInterpolation.Back, KeyEasing.InOut, back: back), new FCurveKey(F(60), 1));
        FCurve Elastic(double amplitude, double period) => Curve(
            FCurveKey.Ease(0, 0, KeyInterpolation.Elastic, KeyEasing.In, amplitude: amplitude, period: period),
            FCurveKey.Ease(F(24), 1.5, KeyInterpolation.Elastic, KeyEasing.Out, amplitude: amplitude, period: period),
            FCurveKey.Ease(F(48), -0.5, KeyInterpolation.Elastic, KeyEasing.InOut, amplitude: amplitude, period: period), new FCurveKey(F(80), 1));
        return new("parameters",
        [
            Case("back-0", Back(0)),
            Case("back-2.5", Back(2.5)),
            Case("back-negative", Back(-0.75)),
            Case("elastic-default", Elastic(FCurveKey.DefaultAmplitude, FCurveKey.DefaultPeriod)),
            Case("elastic-amplitude-0", Elastic(0, F(6))),
            Case("elastic-amplitude-small", Elastic(0.5, F(6))),
            Case("elastic-amplitude-large", Elastic(3, F(6))),
            Case("elastic-period-0", Elastic(2.5, 0)),
            Case("elastic-period-long", Elastic(2.5, F(15))),
        ]);
    }

    /// <summary>Every handle type and their mixes on the middle keys, without smoothing and with it.</summary>
    static CurveScene Handles()
    {
        var cases = new List<CurveCase>();
        foreach (var smoothing in Enum.GetValues<CurveSmoothing>())
        {
            var s = smoothing == CurveSmoothing.None ? "none" : "smooth";
            FCurve Middle(FCurveKey middle) => Curve(CurveExtrapolation.Constant, smoothing,
                new FCurveKey(0, 0), new FCurveKey(F(13), 1.25), middle, new FCurveKey(F(47), 0.5), new FCurveKey(F(58), 1.75));
            cases.Add(Case($"auto-{s}", Middle(FCurveKey.Bezier(F(30), 2.5, HandleType.Auto, HandleType.Auto))));
            cases.Add(Case($"auto-clamped-{s}", Middle(FCurveKey.Bezier(F(30), 2.5))));
            cases.Add(Case($"auto-clamped-slope-{s}", Middle(FCurveKey.Bezier(F(30), 0.75))));
            cases.Add(Case($"vector-{s}", Middle(FCurveKey.Bezier(F(30), 2.5, HandleType.Vector, HandleType.Vector))));
            cases.Add(Case($"free-{s}", Middle(FCurveKey.Free(F(30), 2.5, (F(24.5), 3.25), (F(33), 1.5)))));
            cases.Add(Case($"aligned-{s}", Middle(FCurveKey.Aligned(F(30), 2.5, (F(24.5), 3.25), (F(33), 1.5)))));
            cases.Add(Case($"aligned-vector-{s}", Middle(new FCurveKey(F(30), 2.5, Left: HandleType.Aligned, Right: HandleType.Vector, LeftTime: F(26), LeftValue: 2))));
            cases.Add(Case($"vector-aligned-{s}", Middle(new FCurveKey(F(30), 2.5, Left: HandleType.Vector, Right: HandleType.Aligned, RightTime: F(36), RightValue: 2.75))));
            cases.Add(Case($"aligned-auto-{s}", Middle(new FCurveKey(F(30), 2.5, Left: HandleType.Aligned, Right: HandleType.Auto, LeftTime: F(26), LeftValue: 2))));
            cases.Add(Case($"auto-clamped-aligned-{s}", Middle(new FCurveKey(F(30), 0.75, Left: HandleType.AutoClamped, Right: HandleType.Aligned, RightTime: F(36), RightValue: 2))));
            cases.Add(Case($"free-vector-{s}", Middle(new FCurveKey(F(30), 2.5, Left: HandleType.Free, Right: HandleType.Vector, LeftTime: F(27), LeftValue: 2.25))));
            cases.Add(Case($"vector-auto-clamped-{s}", Middle(new FCurveKey(F(30), 0.75, Left: HandleType.Vector, Right: HandleType.AutoClamped))));
            cases.Add(Case($"free-auto-{s}", Middle(new FCurveKey(F(30), 2.5, Left: HandleType.Free, Right: HandleType.Auto, LeftTime: F(22), LeftValue: 3))));
            // A free side keeps an aligned other side from being lined up.
            cases.Add(Case($"free-aligned-{s}", Middle(new FCurveKey(F(30), 2.5, Left: HandleType.Free, Right: HandleType.Aligned,
                LeftTime: F(26), LeftValue: 3, RightTime: F(35), RightValue: 3.25))));
            // An auto-clamped handle that would reach past the next key's value (clamped, the other side lined up).
            cases.Add(Case($"auto-clamped-overshoot-{s}", Curve(CurveExtrapolation.Constant, smoothing, new FCurveKey(0, 0), new FCurveKey(F(10), 1.875),
                new FCurveKey(F(40), 2), new FCurveKey(F(46), 0.25), new FCurveKey(F(70), 0.125))));
            // Every key auto, and a run of auto keys between vector, free and aligned ones.
            cases.Add(Case($"all-auto-{s}", Curve(CurveExtrapolation.Linear, smoothing, FCurveKey.Bezier(0, 0, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(9), 1, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(22), 0.25, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(30), 2, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(48), 1.5, HandleType.Auto, HandleType.Auto))));
            cases.Add(Case($"runs-{s}", Curve(CurveExtrapolation.Constant, smoothing, FCurveKey.Bezier(0, 0, HandleType.Vector, HandleType.Vector),
                new FCurveKey(F(10), 1), new FCurveKey(F(17), 1.5), new FCurveKey(F(25), 0.5),
                FCurveKey.Free(F(34), 2, (F(31), 2.5), (F(38.5), 1.25)), new FCurveKey(F(42), 1), new FCurveKey(F(51), 2.25),
                FCurveKey.Aligned(F(60), 0.75, (F(56), 1), (F(66), 0.25)), new FCurveKey(F(69), 1.5),
                FCurveKey.Bezier(F(80), 0, HandleType.Vector, HandleType.Vector))));
        }
        return new("handles", [.. cases]);
    }

    /// <summary>
    /// Continuous acceleration's solver: runs of auto-clamped keys where the clamps hold (a peak, an
    /// overshooting neighbour), keys at very uneven spacing, a run of two, the run's ends kept at
    /// handles other than a third of their interval long, vector ends solved.
    /// </summary>
    static CurveScene Smoothing()
    {
        var smooth = CurveSmoothing.ContinuousAcceleration;
        return new("smoothing",
        [
            Case("clamped-run", Curve(CurveExtrapolation.Constant, smooth, new FCurveKey(0, 0), new FCurveKey(F(8), 1), new FCurveKey(F(16), 1.125),
                new FCurveKey(F(24), 3), new FCurveKey(F(32), 2.875), new FCurveKey(F(40), 0.5), new FCurveKey(F(48), 0.5), new FCurveKey(F(56), 1))),
            Case("auto-run", Curve(CurveExtrapolation.Constant, smooth, FCurveKey.Bezier(0, 0, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(8), 1, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(16), 1.125, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(24), 3, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(32), 2.875, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(40), 0.5, HandleType.Auto, HandleType.Auto))),
            Case("uneven", Curve(CurveExtrapolation.Linear, smooth, new FCurveKey(0, 0), new FCurveKey(F(1), 0.5), new FCurveKey(F(2.5), 0.25),
                new FCurveKey(F(30), 2), new FCurveKey(F(31), 1.5), new FCurveKey(F(70.5), -1), new FCurveKey(F(72), 0))),
            Case("uneven-auto", Curve(CurveExtrapolation.Linear, smooth, FCurveKey.Bezier(0, 0, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(1), 0.5, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(2.5), 0.25, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(30), 2, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(31), 1.5, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(70.5), -1, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(72), 0, HandleType.Auto, HandleType.Auto))),
            Case("two-auto", Curve(CurveExtrapolation.Linear, smooth, FCurveKey.Bezier(F(5), 0.25, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(15), 1, HandleType.Auto, HandleType.Auto))),
            Case("two-vector-end", Curve(CurveExtrapolation.Linear, smooth, FCurveKey.Bezier(F(5), 0.25, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(15), 1, HandleType.Vector, HandleType.Vector))),
            Case("kept-ends", Curve(CurveExtrapolation.Constant, smooth, FCurveKey.Free(0, 0, (F(-4), -0.5), (F(7), 1.5)),
                FCurveKey.Bezier(F(10), 1, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(21), 0.5, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(30), 1.75, HandleType.Auto, HandleType.Auto), FCurveKey.Aligned(F(38), 1, (F(37), 2), (F(44), -0.5)))),
            Case("kept-long-handles", Curve(CurveExtrapolation.Constant, smooth, FCurveKey.Free(0, 0, (F(-4), -0.5), (F(16), 2)),
                FCurveKey.Bezier(F(10), 1, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(21), 0.5, HandleType.Auto, HandleType.Auto),
                FCurveKey.Free(F(30), 1.75, (F(18), 1), (F(34), 2)))),
            Case("vector-ends", Curve(CurveExtrapolation.Constant, smooth, FCurveKey.Bezier(0, 0, HandleType.Vector, HandleType.Vector),
                FCurveKey.Bezier(F(10), 1, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(21), 0.5, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(30), 1.75, HandleType.Auto, HandleType.Auto), new FCurveKey(F(38), 1, Left: HandleType.Vector, Right: HandleType.Aligned,
                    RightTime: F(41), RightValue: 0.5), new FCurveKey(F(47), 0))),
            Case("flat-ends", Curve(CurveExtrapolation.Constant, smooth, new FCurveKey(0, 1), new FCurveKey(F(12), 1), new FCurveKey(F(20), 2), new FCurveKey(F(31), 2))),
            // Auto and auto-clamped keys mixed, so the solver's clamps act on free ends and flat intervals (found
            // among random curves): a clamped first key beside a level interval, a rising and a falling one.
            Case("mixed-flat", Curve(CurveExtrapolation.Linear, smooth, FCurveKey.Bezier(0, 2), FCurveKey.Bezier(F(17), 2, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(21), 1))),
            Case("mixed-rising", Curve(CurveExtrapolation.Linear, smooth, FCurveKey.Bezier(0, -1, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(8), 1, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(17), 1.25))),
            Case("mixed-falling", Curve(CurveExtrapolation.Linear, smooth, FCurveKey.Bezier(0, 1), FCurveKey.Bezier(F(1.5), 0.5, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(21), 0))),
            // The solver locks a handle at its limit, then releases it (found among random curves).
            Case("released", Curve(CurveExtrapolation.Linear, smooth, new FCurveKey(0, 1.375), new FCurveKey(F(8.5), 1), new FCurveKey(F(19), 0.875),
                new FCurveKey(F(20), -1.375), new FCurveKey(F(38), -0.625))),
        ]);
    }

    /// <summary>Both extrapolations with every kind of end key, one and two keys.</summary>
    static CurveScene Extrapolation()
    {
        var linear = CurveExtrapolation.Linear;
        var none = CurveSmoothing.None;
        return new("extrapolation",
        [
            Case("constant-bezier", Same(KeyInterpolation.Bezier)),
            Case("linear-bezier", Same(KeyInterpolation.Bezier, linear)),
            Case("linear-linear", Same(KeyInterpolation.Linear, linear)),
            Case("linear-constant", Same(KeyInterpolation.Constant, linear)),
            Case("linear-eased", Curve(linear, CurveSmoothing.ContinuousAcceleration, FCurveKey.Ease(F(4), 1, KeyInterpolation.Cubic, KeyEasing.Out),
                FCurveKey.Ease(F(20), -0.5, KeyInterpolation.Quad), FCurveKey.Ease(F(31), 0.75, KeyInterpolation.Elastic))),
            Case("linear-auto-ends", Curve(linear, none, FCurveKey.Bezier(F(4), 1, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(20), -0.5, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(31), 0.75, HandleType.Auto, HandleType.Auto))),
            Case("constant-auto-ends", Curve(CurveExtrapolation.Constant, none, FCurveKey.Bezier(F(4), 1, HandleType.Auto, HandleType.Auto),
                FCurveKey.Bezier(F(20), -0.5, HandleType.Auto, HandleType.Auto), FCurveKey.Bezier(F(31), 0.75, HandleType.Auto, HandleType.Auto))),
            Case("linear-vector-ends", Curve(linear, none, FCurveKey.Bezier(F(4), 1, HandleType.Vector, HandleType.Vector),
                new FCurveKey(F(20), -0.5), FCurveKey.Bezier(F(31), 0.75, HandleType.Vector, HandleType.Vector))),
            Case("linear-free-ends", Curve(linear, none, FCurveKey.Free(F(4), 1, (F(1), 0.25), (F(8), 1.5)),
                new FCurveKey(F(20), -0.5), FCurveKey.Free(F(31), 0.75, (F(27), 1.25), (F(33), 1.25)))),
            Case("linear-mixed-ends", Curve(linear, none, FCurveKey.Linear(F(4), 1), new FCurveKey(F(20), -0.5), FCurveKey.Constant(F(31), 0.75))),
            Case("one-key", Curve(linear, none, new FCurveKey(F(10), 1.5))),
            Case("one-key-linear", Curve(linear, none, FCurveKey.Linear(F(10), 1.5))),
            Case("one-free-key", Curve(linear, none, FCurveKey.Free(F(10), 1.5, (F(6), 0.5), (F(12), 2.5)))),
            Case("one-key-constant", Curve(CurveExtrapolation.Constant, none, FCurveKey.Free(F(10), 1.5, (F(6), 0.5), (F(12), 2.5)))),
            // Free handles of one key (nothing recalculated) on its time, one past it (clamped to it), both past it (swapped).
            Case("one-key-handles-on-it", Curve(linear, none, FCurveKey.Free(F(10), 1.5, (F(10), 0.5), (F(10), 2.5)))),
            Case("one-key-stray-handle", Curve(linear, none, FCurveKey.Free(F(10), 1.5, (F(12), 0.5), (F(14), 2.5)))),
            Case("one-key-crossed-handles", Curve(linear, none, FCurveKey.Free(F(10), 1.5, (F(13), 0.5), (F(7), 2.25)))),
            Case("two-keys", Curve(linear, CurveSmoothing.ContinuousAcceleration, new FCurveKey(F(10), 1.5), new FCurveKey(F(14.5), -0.25))),
        ]);
    }

    /// <summary>Keys at odd spacing (Blender's 5× ratio limit on auto handles without smoothing) and a key on frame 0.</summary>
    static CurveScene Spacing()
    {
        FCurve Odd(CurveSmoothing smoothing, HandleType type) => Curve(CurveExtrapolation.Linear, smoothing,
            FCurveKey.Bezier(0, 0, type, type), FCurveKey.Bezier(F(0.5), 1, type, type), FCurveKey.Bezier(F(1), 0.25, type, type),
            FCurveKey.Bezier(F(13.5), 2, type, type), FCurveKey.Bezier(F(14), 1.75, type, type), FCurveKey.Bezier(F(57), -1, type, type),
            FCurveKey.Bezier(F(58.5), 0.5, type, type), FCurveKey.Bezier(F(120), 0.25, type, type));
        return new("spacing",
        [
            Case("odd-auto-none", Odd(CurveSmoothing.None, HandleType.Auto)),
            Case("odd-clamped-none", Odd(CurveSmoothing.None, HandleType.AutoClamped)),
            Case("odd-auto-smooth", Odd(CurveSmoothing.ContinuousAcceleration, HandleType.Auto)),
            Case("odd-clamped-smooth", Odd(CurveSmoothing.ContinuousAcceleration, HandleType.AutoClamped)),
            Case("odd-vector", Odd(CurveSmoothing.None, HandleType.Vector)),
            Case("late", Curve(CurveExtrapolation.Linear, CurveSmoothing.ContinuousAcceleration, new FCurveKey(F(600), 1), new FCurveKey(F(603.5), 2),
                new FCurveKey(F(640), -1), FCurveKey.Ease(F(645), 0.5, KeyInterpolation.Elastic, KeyEasing.InOut), new FCurveKey(F(700), 0))),
        ]);
    }

    /// <summary>
    /// Handles on their keys (zero length: Blender keeps them 0.001 frame off), handles of neighbouring
    /// keys meeting, handles reaching past the neighbouring key (shortened to the segment), crossed and
    /// stray handles Blender's update fixes, and segments flat within FLT_EPSILON.
    /// </summary>
    static CurveScene Coincident()
    {
        var none = CurveSmoothing.None;
        return new("coincident",
        [
            Case("on-their-keys", Curve(CurveExtrapolation.Linear, none, FCurveKey.Free(0, 0, (0, 0), (0, 0)), FCurveKey.Free(F(20), 2, (F(20), 2), (F(20), 2)),
                FCurveKey.Free(F(36), 1, (F(36), 1), (F(36), 1)))),
            Case("on-their-keys-aligned", Curve(CurveExtrapolation.Linear, none, FCurveKey.Aligned(0, 0, (0, 0), (F(5), 1)),
                FCurveKey.Aligned(F(20), 2, (F(20), 2), (F(20), 2)), FCurveKey.Aligned(F(36), 1, (F(30), 0.5), (F(36), 1)))),
            Case("meeting", Curve(CurveExtrapolation.Constant, none, FCurveKey.Free(0, 0, (F(-6), 0), (F(10), 1.5)),
                FCurveKey.Free(F(20), 2, (F(10), 1.5), (F(28), 2.5)), FCurveKey.Free(F(36), 1, (F(28), 2.5), (F(40), 1)))),
            Case("past-the-neighbour", Curve(CurveExtrapolation.Constant, none, FCurveKey.Free(0, 0, (F(-3), -0.5), (F(30), 3)),
                FCurveKey.Free(F(20), 2, (F(-10), 0.5), (F(24), 2.5)), FCurveKey.Free(F(36), 1, (F(32), 0.5), (F(44), 1)))),
            Case("crossed", Curve(CurveExtrapolation.Linear, none, new FCurveKey(0, 0), FCurveKey.Free(F(20), 2, (F(26), 2.5), (F(15), 1.25)),
                FCurveKey.Aligned(F(36), 1, (F(39), 0.5), (F(31), 1.5)), new FCurveKey(F(50), 0.5))),
            Case("stray", Curve(CurveExtrapolation.Linear, none, new FCurveKey(0, 0),
                new FCurveKey(F(20), 2, Left: HandleType.Free, Right: HandleType.AutoClamped, LeftTime: F(23), LeftValue: 2.5),
                new FCurveKey(F(36), 1, Left: HandleType.AutoClamped, Right: HandleType.Free, RightTime: F(33), RightValue: 1.5), new FCurveKey(F(50), 0.5))),
            // Handles 2⁻²⁴ off the keys' value (flat to Blender: within FLT_EPSILON), then 2⁻²³ (not).
            Case("flat", Curve(CurveExtrapolation.Linear, none, new FCurveKey(0, 1), new FCurveKey(F(10), 1),
                FCurveKey.Free(F(20), 1, (F(16), 1 - 1.0 / (1 << 24)), (F(24), 1)), FCurveKey.Free(F(30), 1, (F(26), 1 - 1.0 / (1 << 23)), (F(34), 1)),
                new FCurveKey(F(40), 1))),
        ]);
    }

    /// <summary>
    /// The key search: constant segments (where it shows) between many keys, sampled within the
    /// 0.0001-frame threshold before and after each key and just outside it.
    /// </summary>
    static CurveScene Threshold()
    {
        var keys = new List<FCurveKey>();
        double[] frames = [0, 3, 7, 7.5, 12, 18, 18.25, 25, 33, 34, 45];
        for (var i = 0; i < frames.Length; i++)
            keys.Add(FCurveKey.Constant(F(frames[i]), (i % 3 - 1) * 0.75 + i * 0.125));
        var curve = new FCurve(keys, CurveExtrapolation.Linear);
        var near = frames.SelectMany(f => new[] { f - 0.0001, f - 0.00009, f - 0.00011, f + 0.00009, f + 0.00011 })
            .Concat(TimesFor(curve).Select(t => t * 60)).Distinct().Order().Select(F).ToArray();
        return new("threshold", [new CurveCase("constant", curve, near), Case("linear", new FCurve(keys.Select(k => k with { Interpolation = KeyInterpolation.Linear }).ToArray()))]);
    }

    public static IReadOnlyList<CurveScene> All { get; } =
        [Interpolation(), Easing(), Parameters(), Handles(), Smoothing(), Extrapolation(), Spacing(), Coincident(), Threshold()];
}
