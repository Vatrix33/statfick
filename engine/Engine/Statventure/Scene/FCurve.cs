using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Scene;

// Blender's F-curves (Docs/Scene.md, "Curves (step 2)", "How it works"): keys with Bezier, linear,
// constant or eased segments, handles of Blender's five types and the curve's extrapolation and auto
// smoothing, evaluated as Blender 5.2's fcurve.cc does, in double precision (Q5), times in seconds
// (Q4). Blender counts in frames and some of its rules aren't free of the unit (thresholds in frames,
// aligned handles measured as lengths in the (frame, value) plane), so the curve works in frames at the
// clips' 60 fps inside and in seconds outside.

/// <summary>How a key's segment runs to the next key (Blender's interpolation; the easing types use <see cref="KeyEasing"/>).</summary>
public enum KeyInterpolation { Constant, Linear, Bezier, Sine, Quad, Cubic, Quart, Quint, Expo, Circ, Back, Bounce, Elastic }

/// <summary>
/// The easing of an eased segment. <see cref="Auto"/> is Blender's automatic: ease out for back,
/// bounce and elastic, ease in for the others.
/// </summary>
public enum KeyEasing { Auto, In, Out, InOut }

/// <summary>
/// A handle's type, as Blender's: free and aligned handles keep the positions the key stores (an
/// aligned one turned to line up with the other side); vector, auto and auto-clamped handles are
/// worked out from the neighbouring keys. Auto-clamped (Blender's default) keeps a key that is a
/// peak or a dip flat and its handles within the neighbours' values.
/// </summary>
public enum HandleType { Free, Aligned, Vector, Auto, AutoClamped }

/// <summary>Before the first key and after the last: the end key's value, or a straight line (Blender's extrapolation).</summary>
public enum CurveExtrapolation { Constant, Linear }

/// <summary>
/// Blender's auto handle smoothing: none (each auto handle from its neighbours alone), or continuous
/// acceleration (the auto handles of a run of auto keys solved together so the curve's second
/// derivative is continuous; Blender 5.2's default for new F-curves).
/// </summary>
public enum CurveSmoothing { None, ContinuousAcceleration }

/// <summary>
/// A key (record <c>scene.key</c>, S2-1): its time in seconds and value; the interpolation of the
/// segment to the next key (the last key's decides the extrapolation after it: constant stays flat,
/// linear follows the line from the key before, any other the right handle; the first key's likewise
/// before it, with its left handle) and an eased segment's easing, overshoot (<see cref="Back"/>),
/// <see cref="Amplitude"/> (0: the change) and <see cref="Period"/> (seconds; 0: 0.3 of the segment);
/// both handles' types; and for a free or aligned side only, that handle's position (time in seconds,
/// value). The defaults are Blender's for an inserted key (its period 4.1 frames).
/// </summary>
[Record("scene.key", 1)]
[Kind("Key")]
[Help("A key of a curve: its value at its time, and how the curve runs on to the next key, as in Blender's graph editor.")]
public sealed record FCurveKey(
    [property: Field(1), Help("When the key is."), Unit("s")] double Time,
    [property: Field(2), Help("The curve's value at the key.")] double Value,
    [property: Field(3), Help("How the curve runs to the next key: holding, straight, a Bezier curve, or one of Blender's easings.")] KeyInterpolation Interpolation = KeyInterpolation.Bezier,
    [property: Field(4), Help("Which end of an eased segment the easing is at (auto: Blender's choice for the easing).")] KeyEasing Easing = KeyEasing.Auto,
    [property: Field(5), Help("The left handle's type: free and aligned handles keep their position, the others are worked out from the keys around.")] HandleType Left = HandleType.AutoClamped,
    [property: Field(6), Help("The right handle's type: free and aligned handles keep their position, the others are worked out from the keys around.")] HandleType Right = HandleType.AutoClamped,
    [property: Field(7), Help("The left handle's time, for a free or aligned handle only."), Unit("s")] double? LeftTime = null,
    [property: Field(8), Help("The left handle's value, for a free or aligned handle only.")] double? LeftValue = null,
    [property: Field(9), Help("The right handle's time, for a free or aligned handle only."), Unit("s")] double? RightTime = null,
    [property: Field(10), Help("The right handle's value, for a free or aligned handle only.")] double? RightValue = null,
    [property: Field(11), Help("How far a back easing overshoots.")] double Back = FCurveKey.DefaultBack,
    [property: Field(12), Help("How far an elastic easing swings (0: as far as the change)."), Limits(0, double.PositiveInfinity)] double Amplitude = FCurveKey.DefaultAmplitude,
    [property: Field(13), Help("How long each swing of an elastic easing is (0: 0.3 of the segment)."), Unit("s")] double Period = FCurveKey.DefaultPeriod)
{
    /// <summary>Blender's overshoot for a new key (animrig's initialize_bezt).</summary>
    public const double DefaultBack = 1.70158;
    /// <summary>Blender's elastic amplitude for a new key.</summary>
    public const double DefaultAmplitude = 0.8;
    /// <summary>Blender's elastic period for a new key, 4.1 frames, in seconds.</summary>
    public const double DefaultPeriod = 4.1 / FCurve.FramesPerSecond;

    /// <summary>A key whose segment holds its value until the next key.</summary>
    public static FCurveKey Constant(double time, double value) => new(time, value, KeyInterpolation.Constant);

    /// <summary>A key whose segment runs in a straight line to the next key.</summary>
    public static FCurveKey Linear(double time, double value) => new(time, value, KeyInterpolation.Linear);

    /// <summary>A Bezier key with computed handles (auto-clamped by default, or vector or auto).</summary>
    public static FCurveKey Bezier(double time, double value, HandleType left = HandleType.AutoClamped, HandleType right = HandleType.AutoClamped)
    {
        if (left is HandleType.Free or HandleType.Aligned || right is HandleType.Free or HandleType.Aligned)
            throw new ArgumentException("free and aligned handles need their positions: use Free or Aligned");
        return new(time, value, KeyInterpolation.Bezier, Left: left, Right: right);
    }

    /// <summary>A key whose segment is one of Blender's easings (sine to elastic) with its parameters.</summary>
    public static FCurveKey Ease(double time, double value, KeyInterpolation type, KeyEasing easing = KeyEasing.Auto,
        double back = DefaultBack, double amplitude = DefaultAmplitude, double period = DefaultPeriod)
    {
        if (type is KeyInterpolation.Constant or KeyInterpolation.Linear or KeyInterpolation.Bezier)
            throw new ArgumentException($"{type} isn't an easing type");
        return new(time, value, type, easing, Back: back, Amplitude: amplitude, Period: period);
    }

    /// <summary>A Bezier key with free handles at the given positions (time in seconds, value).</summary>
    public static FCurveKey Free(double time, double value, (double Time, double Value) left, (double Time, double Value) right) =>
        new(time, value, KeyInterpolation.Bezier, KeyEasing.Auto, HandleType.Free, HandleType.Free, left.Time, left.Value, right.Time, right.Value);

    /// <summary>A Bezier key with aligned handles at the given positions (the left one turned to line up with the right one).</summary>
    public static FCurveKey Aligned(double time, double value, (double Time, double Value) left, (double Time, double Value) right) =>
        new(time, value, KeyInterpolation.Bezier, KeyEasing.Auto, HandleType.Aligned, HandleType.Aligned, left.Time, left.Value, right.Time, right.Value);
}

/// <summary>A key's handles as the curve uses them (stored or worked out): times in seconds, values.</summary>
public readonly record struct KeyHandles(double LeftTime, double LeftValue, double RightTime, double RightValue);

/// <summary>
/// An F-curve (record <c>scene.curve</c>, S2-1, S2-2): its keys in time order, its extrapolation and its
/// auto smoothing; one animated number (a track uses one curve per component). Immutable and always
/// valid: making or reading one that breaks the rules (S2-3: no keys, keys not increasing at least
/// Blender's 0.01 frame apart, a free or aligned side without its position or an auto or vector side
/// with one, a number that isn't finite, a negative amplitude) throws an
/// <see cref="ArgumentException"/> naming the key and the problem. Handles are resolved once, on
/// first use, as Blender's update after an edit leaves them (<c>fcurve.update()</c>: crossed handles
/// swapped, handles clamped to their key's side, then <c>BKE_fcurve_handles_recalc</c> with the
/// smoothing pass); a copy made with <c>with</c> resolves its own.
/// </summary>
[Record("scene.curve", 1)]
[Kind("F-curve")]
[Help("A value changing over time, as an F-curve in Blender's graph editor: keys, and how the curve runs between them.")]
public sealed record FCurve
{
    /// <summary>The frame rate Blender's frames are counted in (the clips', M7): its thresholds are in frames.</summary>
    public const double FramesPerSecond = 60;
    /// <summary>Keys closer than this (frames) are one key to Blender (BEZT_BINARYSEARCH_THRESH, its merge on update).</summary>
    public const double MergeThreshold = 0.01;

    readonly FCurveKey[] _keys = [];
    FCurveResolved? _resolved;

    /// <summary>A curve; throws an <see cref="ArgumentException"/> when the keys break the rules (see the type).</summary>
    public FCurve(IReadOnlyList<FCurveKey> keys, CurveExtrapolation extrapolation = CurveExtrapolation.Constant,
        CurveSmoothing smoothing = CurveSmoothing.ContinuousAcceleration)
    {
        Keys = keys;
        Extrapolation = extrapolation;
        Smoothing = smoothing;
    }

    /// <summary>The keys, in time order.</summary>
    [Field(1), Help("The keys, in time order, at least one.")]
    public IReadOnlyList<FCurveKey> Keys
    {
        get => _keys;
        init
        {
            var keys = value?.ToArray() ?? throw new ArgumentNullException(nameof(value));
            Check(keys);
            _keys = keys;
            _resolved = null;
        }
    }

    [Field(2), Help("Before the first key and after the last: the end key's value (constant), or the line through it (linear).")]
    public CurveExtrapolation Extrapolation
    {
        get;
        init
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentException($"{value} isn't an extrapolation");
            field = value;
            _resolved = null;
        }
    }

    [Field(3), Help("How the worked-out handles of a run of keys are smoothed: each from its neighbours alone, or together so the motion has no jolts (Blender's default).")]
    public CurveSmoothing Smoothing
    {
        get;
        init
        {
            if (!Enum.IsDefined(value))
                throw new ArgumentException($"{value} isn't a smoothing mode");
            field = value;
            _resolved = null;
        }
    }

    /// <summary>The first key's time (seconds).</summary>
    public double Start => _keys[0].Time;
    /// <summary>The last key's time (seconds).</summary>
    public double End => _keys[^1].Time;

    /// <summary>Each key's handles as the curve uses them: the stored free and aligned ones (after Blender's fixes) and the worked-out others.</summary>
    public IReadOnlyList<KeyHandles> Handles => Resolved.Handles;

    /// <summary>The curve's value at <paramref name="time"/> (seconds), as Blender evaluates the F-curve at that frame.</summary>
    public double Evaluate(double time) => Resolved.Evaluate(time * FramesPerSecond, Extrapolation);

    FCurveResolved Resolved
    {
        get
        {
            var resolved = Volatile.Read(ref _resolved);
            if (resolved != null)
                return resolved;
            resolved = new FCurveResolved(_keys, Extrapolation, Smoothing);
            return Interlocked.CompareExchange(ref _resolved, resolved, null) ?? resolved;
        }
    }

    /// <summary>
    /// The curve at <paramref name="path"/> in the game's data (a record entry, or its JSON form in the
    /// development folder): a <see cref="DataFileException"/> naming the pack and entry or the loose file
    /// when it doesn't read or breaks the rules.
    /// </summary>
    public static FCurve Load(VirtualFileSystem files, string path)
    {
        try
        {
            return files.Load<FCurve>(path);
        }
        catch (ArgumentException problem)
        {
            throw files.Problem(path, $"not a valid F-curve: {problem.Message}", problem);
        }
    }

    // Equality by content: the resolved handles are a cache.
    public bool Equals(FCurve? other) =>
        other is not null && Extrapolation == other.Extrapolation && Smoothing == other.Smoothing && _keys.AsSpan().SequenceEqual(other._keys);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Extrapolation);
        hash.Add(Smoothing);
        foreach (var key in _keys)
            hash.Add(key);
        return hash.ToHashCode();
    }

    /// <summary>The rules every curve keeps (S2-3): the first problem of <see cref="ProblemsOf"/> as an <see cref="ArgumentException"/>.</summary>
    static void Check(FCurveKey[] keys)
    {
        if (ProblemsOf(keys).FirstOrDefault() is { } problem)
            throw new ArgumentException(problem.Message);
    }

    /// <summary>
    /// What a curve with these keys would break (S2-3; the rules making or reading one refuses, with their
    /// words), each at the path of what it names within the curve: <c>$.keys</c> when there are none, a key
    /// (<c>$.keys[1]</c>) that is null, else the key's fields (its time, value, types, easing numbers, a
    /// handle's position; a key too close after the one before at its time), in key order and each key's in
    /// the order a curve checks them. A curve that exists keeps every rule, so a built record never has these
    /// (its check is empty): a form edits keys first, and takes these paths below the curve's own (M11, P10).
    /// </summary>
    public static IReadOnlyList<RecordProblem> ProblemsOf(IReadOnlyList<FCurveKey> keys)
    {
        var problems = new List<RecordProblem>();
        var list = RecordPath.Root.Field("keys");
        if (keys.Count == 0)
            problems.Add(new(list, "an F-curve needs at least one key"));
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            var at = list.Index(i);
            void Report(string message, params (bool Broken, string Field)[] fields)
            {
                foreach (var (broken, field) in fields)
                    if (broken)
                        problems.Add(new(at.Field(field), message));
            }
            if (key == null)
            {
                problems.Add(new(at, $"key {i} is null"));
                continue;
            }
            var where = $"key {i} (at {key.Time:R} s)";
            Report($"key {i}: its time and value must be finite numbers", (!double.IsFinite(key.Time), "time"), (!double.IsFinite(key.Value), "value"));
            Report($"{where}: an interpolation, easing or handle type that isn't one", (!Enum.IsDefined(key.Interpolation), "interpolation"),
                (!Enum.IsDefined(key.Easing), "easing"), (!Enum.IsDefined(key.Left), "left"), (!Enum.IsDefined(key.Right), "right"));
            Report($"{where}: back, amplitude and period must be finite numbers", (!double.IsFinite(key.Back), "back"),
                (!double.IsFinite(key.Amplitude), "amplitude"), (!double.IsFinite(key.Period), "period"));
            Report($"{where}: a negative amplitude ({key.Amplitude:R}; Blender's are 0 or more)", (key.Amplitude < 0, "amplitude"));
            Side(Report, where, "left", key.Left, key.LeftTime, key.LeftValue);
            Side(Report, where, "right", key.Right, key.RightTime, key.RightValue);
            if (i > 0 && keys[i - 1] is { } before && !((key.Time - before.Time) * FramesPerSecond > MergeThreshold))
                Report($"{where}: keys must be in increasing time order at least {MergeThreshold} frame (1/{FramesPerSecond / MergeThreshold:R} s) apart, and key {i - 1} is at {before.Time:R} s",
                    (true, "time"));
        }
        return problems;
    }

    static string Name(HandleType type) => type == HandleType.AutoClamped ? "auto-clamped" : type.ToString().ToLowerInvariant();

    /// <summary>A handle's rules: a free or aligned side has its finite position, a worked-out one none.</summary>
    static void Side(Action<string, (bool, string)[]> report, string where, string side, HandleType type, double? time, double? value)
    {
        var stored = type is HandleType.Free or HandleType.Aligned;
        if (stored && (time == null || value == null))
            report($"{where}: its {side} handle is {Name(type)} but has no position (time and value)", [(time == null, side + "Time"), (value == null, side + "Value")]);
        else if (!stored && (time != null || value != null))
            report($"{where}: its {side} handle is {Name(type)}, which is worked out, but a position is given", [(time != null, side + "Time"), (value != null, side + "Value")]);
        else if (stored && (!double.IsFinite(time!.Value) || !double.IsFinite(value!.Value)))
            report($"{where}: its {side} handle's position must be finite numbers", [(!double.IsFinite(time.Value), side + "Time"), (!double.IsFinite(value.Value), side + "Value")]);
    }
}
