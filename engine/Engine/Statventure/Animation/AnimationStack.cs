namespace Statventure.Animation;

/// <summary>How a strip's channels go onto what the layers below give (Blender's NLA blend types).</summary>
public enum BlendMode
{
    /// <summary>Mixes toward the strip's values by its influence: lower·(1 − i) + strip·i per component.</summary>
    Replace,
    /// <summary>Adds the strip on top: locations added, scales multiplied, rotations multiplied (an additive layer).</summary>
    Combine,
}

/// <summary>What a strip gives outside its range (Blender's NLA extrapolation).</summary>
public enum Extrapolation
{
    /// <summary>Nothing outside the strip.</summary>
    Nothing,
    /// <summary>Its first frame before it (only as a layer's first strip) and its last frame after it.</summary>
    Hold,
    /// <summary>Its last frame after it, until the layer's next strip.</summary>
    HoldForward,
}

/// <summary>
/// A clip placed on a layer, as an NLA strip of Blender's: from <see cref="Start"/> (seconds on the
/// stack's clock) it plays the part <see cref="ClipStart"/> to <see cref="ClipEnd"/> of its clip,
/// stretched by <see cref="Scale"/>, <see cref="Repeat"/> times; its influence ramps up over
/// <see cref="BlendIn"/> and down over <see cref="BlendOut"/>, unless it has a fixed
/// <see cref="Influence"/> (Docs/Animation.md, "Rules from Blender and glTF").
/// </summary>
public sealed class AnimationStrip(ClipBinding clip)
{
    /// <summary>
    /// Seconds within which two times count as one. Blender compares frames as floats (IS_EQF), while
    /// clip lengths come in as float32 seconds (0.6 s is 0.60000002): exact tests in double seconds
    /// would put a strip's end or a wrap of its part on the wrong side.
    /// </summary>
    public const double Epsilon = 1e-6;

    public ClipBinding Clip { get; } = clip;
    public double Start { get; set; }
    public double ClipStart { get; set; }
    public double ClipEnd { get; set; } = clip.Clip.Duration;
    /// <summary>Time stretch (Blender's scale): 2 plays at half speed.</summary>
    public double Scale { get; set; } = 1;
    /// <summary>How many times the part plays (fractions allowed, as Blender's repeat).</summary>
    public double Repeat { get; set; } = 1;
    /// <summary>Plays the part backwards.</summary>
    public bool Reversed { get; set; }
    public double BlendIn { get; set; }
    public double BlendOut { get; set; }
    /// <summary>A fixed influence (Blender's animated influence held constant); null: from the blend in and out.</summary>
    public double? Influence { get; set; }
    public BlendMode Mode { get; set; } = BlendMode.Replace;
    public Extrapolation Extrapolation { get; set; } = Extrapolation.HoldForward;

    /// <summary>
    /// The part's length (a part of no length counts as one 60 fps frame, as Blender counts one
    /// frame, BKE_nla_clip_length_get_nonzero).
    /// </summary>
    double PartLength => ClipEnd - ClipStart is var length && length != 0 ? length : 1.0 / 60;

    public double Length => PartLength * Math.Abs(Scale) * Repeat;
    public double End => Start + Length;

    /// <summary>
    /// The influence at <paramref name="time"/>: linear ramps (nlastrip_get_influence), the blend-in
    /// limited to the strip's length and the blend-out to what the blend-in leaves (Blender's setters).
    /// </summary>
    public double InfluenceAt(double time)
    {
        if (Influence is { } fixedInfluence)
            return fixedInfluence;
        var length = End - Start;
        var blendIn = Math.Clamp(BlendIn, 0, Math.Max(length, 0));
        var blendOut = Math.Clamp(BlendOut, 0, Math.Max(length - blendIn, 0));
        if (blendIn != 0 && time <= Start + blendIn)
            return Math.Abs(time - Start) / blendIn;
        if (blendOut != 0 && time >= End - blendOut)
            return Math.Abs(End - time) / blendOut;
        return 1;
    }

    /// <summary>
    /// The clip's time at <paramref name="time"/> (nlastrip_get_frame_actionclip): the part's start
    /// plus the time into the strip, wrapped by the stretched part's length, over the stretch; at the
    /// strip's very end with whole repeats the part's end (not its start); reversed counts back.
    /// </summary>
    public double ClipTimeAt(double time)
    {
        var scale = Math.Abs(Scale);
        var atEnd = Math.Abs(time - End) <= Epsilon && Repeat == Math.Floor(Repeat);
        if (Reversed)
            return atEnd ? ClipStart : ClipEnd - Remainder(time - Start, PartLength * scale) / scale;
        return atEnd ? ClipEnd : ClipStart + Remainder(time - Start, PartLength * scale) / scale;
    }

    /// <summary>C's fmod (C#'s %), a remainder within <see cref="Epsilon"/> of the period taken as a whole period (0).</summary>
    static double Remainder(double x, double y)
    {
        var r = x % y;
        return Math.Abs(y) - Math.Abs(r) <= Epsilon ? 0 : r;
    }
}

/// <summary>A clip event a strip fired: the marker's name, the time on the stack's clock, the layer (bottom 0) and the strip.</summary>
public readonly record struct ClipEvent(string Name, double Time, int Layer, AnimationStrip Strip);

/// <summary>A layer of an <see cref="AnimationStack"/>: an NLA track (<see cref="AnimationLayer"/>) or a state machine's cross-fading layer (<see cref="FadeLayer"/>).</summary>
public abstract class StackLayer
{
    public bool Muted { get; set; }

    /// <summary>Puts the layer's channels at <paramref name="time"/> onto <paramref name="accumulated"/> (what the layers below give).</summary>
    internal abstract void Apply(double time, PoseBuffer accumulated, AnimationStack stack);

    /// <summary>The events its strips pass in (from, to] (see <see cref="AnimationStack.CollectEvents"/>).</summary>
    internal abstract void CollectEvents(int index, double from, double to, List<(ClipEvent Event, int Pass, int Marker)> found);

    /// <summary>
    /// Each marker of a strip's clip part, once per pass through the part (its cycles; reversed
    /// strips pass it backwards), at Start + pass · period + offset, inside the strip's range and in
    /// (from, to], where <paramref name="acting"/> says the strip acts.
    /// </summary>
    internal static void StripEvents(AnimationStrip strip, int layer, double from, double to, Func<double, bool> acting, List<(ClipEvent, int, int)> found)
    {
        var markers = strip.Clip.Clip.Events;
        if (markers.Count == 0 || strip.End <= from + AnimationStrip.Epsilon || strip.Start > to + AnimationStrip.Epsilon)
            return;
        var scale = Math.Abs(strip.Scale);
        var period = (strip.ClipEnd - strip.ClipStart is var part && part != 0 ? part : 1.0 / 60) * scale;
        var passes = double.IsPositiveInfinity(strip.Repeat) ? int.MaxValue : (int)Math.Ceiling(strip.Repeat - 1e-9);
        var firstPass = (int)Math.Max(0, Math.Floor((from - strip.Start) / period) - 1);
        var lastPass = (int)Math.Min(passes - 1.0, Math.Floor((to - strip.Start) / period) + 1);
        for (var pass = firstPass; pass <= lastPass; pass++)
            for (var m = 0; m < markers.Count; m++)
            {
                var marker = markers[m];
                if (marker.Time < strip.ClipStart - AnimationStrip.Epsilon || marker.Time > strip.ClipEnd + AnimationStrip.Epsilon)
                    continue;
                var offset = (strip.Reversed ? strip.ClipEnd - marker.Time : marker.Time - strip.ClipStart) * scale;
                var time = strip.Start + pass * period + offset;
                if (time > strip.End + AnimationStrip.Epsilon || time <= from + AnimationStrip.Epsilon || time > to + AnimationStrip.Epsilon)
                    continue;
                if (acting(time))
                    found.Add((new ClipEvent(marker.Name, time, layer, strip), pass, strip.Reversed ? markers.Count - 1 - m : m));
            }
    }
}

/// <summary>A layer of strips (an NLA track): strips in time order, not overlapping.</summary>
public sealed class AnimationLayer : StackLayer
{
    public List<AnimationStrip> Strips { get; } = [];

    /// <summary>
    /// The strip that acts at <paramref name="time"/> and the time it's evaluated at
    /// (nlastrips_ctime_get_strip): one whose range holds the time (ends included); before a layer's
    /// first strip that strip at its start if it holds both ways; between two strips the earlier at
    /// its end unless it extrapolates nothing; after the last strip that one at its end likewise.
    /// </summary>
    public (AnimationStrip Strip, double Time)? StripAt(double time)
    {
        for (var i = 0; i < Strips.Count; i++)
        {
            var strip = Strips[i];
            if (time >= strip.Start - AnimationStrip.Epsilon && time <= strip.End + AnimationStrip.Epsilon)
                return (strip, time);
            if (time < strip.Start)
            {
                if (i == 0)
                    return strip.Extrapolation == Extrapolation.Hold ? (strip, strip.Start) : null;
                var previous = Strips[i - 1];
                return previous.Extrapolation != Extrapolation.Nothing ? (previous, previous.End) : null;
            }
            if (i == Strips.Count - 1)
                return strip.Extrapolation != Extrapolation.Nothing ? (strip, strip.End) : null;
        }
        return null;
    }

    internal override void Apply(double time, PoseBuffer accumulated, AnimationStack stack)
    {
        if (StripAt(time) is not var (strip, at))
            return;
        var influence = strip.InfluenceAt(at);
        if (influence <= 0)
            return;
        var upper = stack.Rent();
        strip.Clip.Sample(strip.ClipTimeAt(at), upper);
        AnimationStack.Blend(accumulated, upper, strip.Mode, influence);
        stack.Return(upper);
    }

    internal override void CollectEvents(int index, double from, double to, List<(ClipEvent, int, int)> found)
    {
        foreach (var strip in Strips)
            // Within its range (events lie there), so not held: acting if its influence is above 0.
            StripEvents(strip, index, from, to, time => StripAt(time) is var (acting, _) && acting == strip && strip.InfluenceAt(time) > 0, found);
    }
}

/// <summary>
/// A cross-fade within a layer: a strip (null: nothing, an empty state) fading in from
/// <see cref="FadeStart"/> over <see cref="FadeDuration"/> seconds over what its layer showed before.
/// </summary>
public sealed class FadeEntry(AnimationStrip? strip, double fadeStart, double fadeDuration)
{
    public AnimationStrip? Strip { get; } = strip;
    public double FadeStart { get; } = fadeStart;
    public double FadeDuration { get; } = fadeDuration;

    /// <summary>Its share at <paramref name="time"/>: a linear ramp from 0 to 1 (a fade of no length: 1 from its start).</summary>
    public double WeightAt(double time) => FadeDuration <= 0
        ? (time >= FadeStart - AnimationStrip.Epsilon ? 1 : 0)
        : Math.Clamp((time - FadeStart) / FadeDuration, 0, 1);

    /// <summary>When it's fully in.</summary>
    public double FullAt => FadeStart + Math.Max(FadeDuration, 0);
}

/// <summary>
/// A state machine's layer (Docs/Animation.md, "State machines in detail"): entries oldest first,
/// each cross-fading over what the entries before it give, by Blender's NLA transition rule
/// (nlastrip_evaluate_transition): both sides are the layers below with that side's strip blended on
/// (its blend mode, a channel it doesn't key keeps the lower value), then mixed over every channel by
/// the fade's share, as Replace mixes. So a channel only the outgoing clip keys eases back to the
/// layers below, and a fade starting during another fades over the current mix. Each strip is held
/// outside its range (both ways) and plays through its fade.
/// </summary>
public sealed class FadeLayer : StackLayer
{
    public List<FadeEntry> Entries { get; } = [];

    internal override void Apply(double time, PoseBuffer accumulated, AnimationStack stack)
    {
        PoseBuffer? mix = null;
        foreach (var entry in Entries)
        {
            var weight = entry.WeightAt(time);
            if (weight <= 0)
                continue;
            var side = stack.Rent();
            side.CopyFrom(accumulated);
            if (entry.Strip is { } strip)
            {
                var upper = stack.Rent();
                strip.Clip.Sample(strip.ClipTimeAt(Math.Clamp(time, strip.Start, strip.End)), upper);
                AnimationStack.Blend(side, upper, strip.Mode, strip.Influence ?? 1);
                stack.Return(upper);
            }
            if (mix == null)
            {
                mix = stack.Rent();
                mix.CopyFrom(accumulated);
            }
            mix.MixToward(side, weight);
            stack.Return(side);
        }
        if (mix != null)
        {
            accumulated.CopyFrom(mix);
            stack.Return(mix);
        }
    }

    /// <summary>An entry's share of the layer's result: its own fade times what each later entry has not yet covered.</summary>
    public double ShareAt(int entry, double time)
    {
        var share = Entries[entry].WeightAt(time);
        for (var j = entry + 1; j < Entries.Count && share > 0; j++)
            share *= 1 - Entries[j].WeightAt(time);
        return share;
    }

    internal override void CollectEvents(int index, double from, double to, List<(ClipEvent, int, int)> found)
    {
        for (var e = 0; e < Entries.Count; e++)
            if (Entries[e].Strip is { } strip)
            {
                var entry = e;
                StripEvents(strip, index, from, to, time => ShareAt(entry, time) > 0, found);
            }
    }
}

/// <summary>
/// Layers evaluated as Blender evaluates an NLA stack (Docs/Animation.md): bottom first, every
/// channel starting from its default (the rest pose); each NLA layer's acting strip changes only the
/// channels its clip keys, by its influence and blend mode (anim_sys.cc); a state machine's layers
/// cross-fade their states (<see cref="FadeLayer"/>).
/// </summary>
public sealed class AnimationStack(Skeleton skeleton)
{
    readonly Stack<PoseBuffer> _free = [];

    public Skeleton Skeleton { get; } = skeleton;
    /// <summary>Bottom first.</summary>
    public List<StackLayer> Layers { get; } = [];

    internal PoseBuffer Rent()
    {
        var buffer = _free.Count > 0 ? _free.Pop() : new PoseBuffer(Skeleton);
        buffer.Clear();
        return buffer;
    }

    internal void Return(PoseBuffer buffer) => _free.Push(buffer);

    /// <summary>A new NLA layer on top with this strip on it.</summary>
    public AnimationStrip Add(ClipBinding clip, double start, Action<AnimationStrip>? setup = null)
    {
        var strip = new AnimationStrip(clip) { Start = start };
        setup?.Invoke(strip);
        var layer = new AnimationLayer();
        layer.Strips.Add(strip);
        Layers.Add(layer);
        return strip;
    }

    public void Evaluate(double time, PoseBuffer into)
    {
        into.Clear();
        foreach (var layer in Layers)
            if (!layer.Muted)
                layer.Apply(time, into, this);
    }

    /// <summary>
    /// The events strips pass after <paramref name="from"/> up to and including <paramref name="to"/>
    /// (seconds on the stack's clock), in time order (then bottom layer first, then each strip's
    /// passes in order): each marker of a strip's clip part, once per pass through the part (its
    /// cycles; reversed strips pass the part backwards), fired where the strip acts at that moment
    /// (an NLA layer's strip there, within its range rather than held, influence above 0; a fade
    /// layer's entry with a share above 0; Docs/Animation.md, "Clip events"). Times within
    /// <see cref="AnimationStrip.Epsilon"/> count as one: an event at a frame's time belongs to that frame.
    /// </summary>
    public void CollectEvents(double from, double to, List<ClipEvent> into)
    {
        if (!(to > from))
            return;
        var found = new List<(ClipEvent Event, int Pass, int Marker)>();
        for (var l = 0; l < Layers.Count; l++)
            if (!Layers[l].Muted)
                Layers[l].CollectEvents(l, from, to, found);
        // By time; events within the epsilon of the first of a run count as one moment, ordered by
        // layer, pass and marker (float32 clip lengths put a pass's end and the next pass's start
        // a few 10⁻⁸ s apart).
        found.Sort((a, b) => a.Event.Time.CompareTo(b.Event.Time));
        for (var run = 0; run < found.Count;)
        {
            var end = run + 1;
            while (end < found.Count && found[end].Event.Time - found[run].Event.Time <= AnimationStrip.Epsilon)
                end++;
            foreach (var (e, _, _) in found.GetRange(run, end - run).OrderBy(f => f.Event.Layer).ThenBy(f => f.Pass).ThenBy(f => f.Marker))
                into.Add(e);
            run = end;
        }
    }

    public Render3D.ModelPose Pose(double time)
    {
        var buffer = new PoseBuffer(Skeleton);
        Evaluate(time, buffer);
        return buffer.ToPose();
    }

    /// <summary>
    /// Puts the upper values onto the lower ones where the upper are set (the blend domain):
    /// Replace mixes every component by the influence (quaternions too, normalised only when the pose
    /// is built: nla_blend_value); Combine adds locations, multiplies scales by (upper / 1)^influence
    /// and multiplies normalised quaternions, lower · upper^influence (nla_combine_value,
    /// nla_combine_quaternion).
    /// </summary>
    internal static void Blend(PoseBuffer lower, PoseBuffer upper, BlendMode mode, double influence)
    {
        for (var node = 0; node < upper.Set.Length; node++)
        {
            var set = upper.Set[node];
            if (set == ChannelSet.None)
                continue;
            lower.Relative[node] = upper.Relative[node];
            lower.Set[node] |= set;
            if ((set & ChannelSet.Translation) != 0)
                for (var c = 3 * node; c < 3 * node + 3; c++)
                    lower.Translation[c] = mode == BlendMode.Replace
                        ? lower.Translation[c] * (1 - influence) + upper.Translation[c] * influence
                        : lower.Translation[c] + upper.Translation[c] * influence;
            if ((set & ChannelSet.Scale) != 0)
                for (var c = 3 * node; c < 3 * node + 3; c++)
                    lower.Scale[c] = mode == BlendMode.Replace
                        ? lower.Scale[c] * (1 - influence) + upper.Scale[c] * influence
                        : lower.Scale[c] * Math.Pow(upper.Scale[c], influence);
            if ((set & ChannelSet.Rotation) != 0)
            {
                var r = 4 * node;
                if (mode == BlendMode.Replace)
                    for (var c = r; c < r + 4; c++)
                        lower.Rotation[c] = lower.Rotation[c] * (1 - influence) + upper.Rotation[c] * influence;
                else
                    CombineQuaternion(lower.Rotation.AsSpan(r, 4), upper.Rotation.AsSpan(r, 4), influence);
            }
        }
    }

    /// <summary>
    /// nla_combine_quaternion: both normalised, the upper raised to the influence
    /// (pow_qt_fl_normalized: its angle times the influence, from acos of its w, so a quaternion with
    /// w &lt; 0 turns the long way), then lower · upper. In double precision: Blender's float32 acos
    /// is so steep near w = 1 that it quantises tiny rotations (it drops those under about 0.03°).
    /// </summary>
    static void CombineQuaternion(Span<double> lower, ReadOnlySpan<double> upper, double influence)
    {
        Span<double> a = stackalloc double[4], b = stackalloc double[4];
        Normalize(lower, a);
        Normalize(upper, b);
        // x, y, z, w; Blender keeps w first.
        var angle = influence * Math.Acos(Math.Clamp(b[3], -1, 1));
        var sine = Math.Sin(angle);
        var vector = Math.Sqrt(b[0] * b[0] + b[1] * b[1] + b[2] * b[2]);
        var w = Math.Cos(angle);
        double x = 0, y = 0, z = 0;
        if (vector > 0)
        {
            x = b[0] / vector * sine;
            y = b[1] / vector * sine;
            z = b[2] / vector * sine;
        }
        // Hamilton product a · (x, y, z, w).
        lower[3] = a[3] * w - a[0] * x - a[1] * y - a[2] * z;
        lower[0] = a[3] * x + a[0] * w + a[1] * z - a[2] * y;
        lower[1] = a[3] * y - a[0] * z + a[1] * w + a[2] * x;
        lower[2] = a[3] * z + a[0] * y - a[1] * x + a[2] * w;
    }

    /// <summary>Blender's normalize_qt_qt: a zero quaternion becomes the unit one.</summary>
    static void Normalize(ReadOnlySpan<double> q, Span<double> result)
    {
        var length = Math.Sqrt(q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3]);
        if (length > 1e-35)
            for (var i = 0; i < 4; i++)
                result[i] = q[i] / length;
        else
        {
            result.Clear();
            result[3] = 1;
        }
    }
}
