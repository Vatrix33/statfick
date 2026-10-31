using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Animation;

/// <summary>
/// One animated property of one node: its keys as the glTF file stores them, sampled as glTF 2.0's
/// Appendix C says (Docs/Animation.md, "Rules from Blender and glTF"). A bone's keys hold its
/// rest-relative value (Blender's pose channel), see <see cref="AnimationClip"/>.
/// </summary>
public sealed class AnimationCurve
{
    public AnimationCurve(AnimationPath path, AnimationInterpolation interpolation, float[] times, float[] values)
    {
        var perKey = (interpolation == AnimationInterpolation.CubicSpline ? 3 : 1) * (path == AnimationPath.Rotation ? 4 : 3);
        if (times.Length == 0 || values.Length != times.Length * perKey)
            throw new ArgumentException($"{values.Length} values don't fit {times.Length} keys of {perKey}");
        Path = path;
        Interpolation = interpolation;
        Times = times;
        Values = values;
    }

    public AnimationPath Path { get; }
    public AnimationInterpolation Interpolation { get; }
    /// <summary>The keys' times in seconds, increasing.</summary>
    public float[] Times { get; }
    /// <summary>Per key: the value (<see cref="Components"/> numbers), or with CUBICSPLINE its in-tangent, value and out-tangent.</summary>
    public float[] Values { get; }
    /// <summary>4 for rotations (x, y, z, w), 3 otherwise.</summary>
    public int Components => Path == AnimationPath.Rotation ? 4 : 3;
    public float End => Times[^1];

    /// <summary>
    /// The value at <paramref name="time"/> (seconds from the clip's start) into
    /// <paramref name="result"/> (<see cref="Components"/> numbers). Before the first key and after
    /// the last the output is clamped to them; at a key its value is used as is.
    /// </summary>
    public void Evaluate(double time, Span<double> result)
    {
        var n = Times.Length;
        if (time <= Times[0])
        {
            Key(0, result);
            return;
        }
        if (time >= Times[n - 1])
        {
            Key(n - 1, result);
            return;
        }
        // The segment k with Times[k] <= time < Times[k + 1].
        int low = 0, high = n - 1;
        while (high - low > 1)
        {
            var middle = (low + high) / 2;
            if (Times[middle] <= time)
                low = middle;
            else
                high = middle;
        }
        var k = low;
        double tk = Times[k], duration = Times[k + 1] - tk, t = (time - tk) / duration;
        switch (Interpolation)
        {
            case AnimationInterpolation.Step:
                Key(k, result);
                break;
            case AnimationInterpolation.Linear when Path == AnimationPath.Rotation:
                Slerp(k, t, result);
                break;
            case AnimationInterpolation.Linear:
                for (var c = 0; c < Components; c++)
                    result[c] = (1 - t) * Values[k * Components + c] + t * Values[(k + 1) * Components + c];
                break;
            default:
                Cubic(k, t, duration, result);
                break;
        }
    }

    /// <summary>The value of key <paramref name="k"/> (with CUBICSPLINE the middle of its three).</summary>
    void Key(int k, Span<double> result)
    {
        var c = Components;
        var start = Interpolation == AnimationInterpolation.CubicSpline ? (3 * k + 1) * c : k * c;
        for (var i = 0; i < c; i++)
            result[i] = Values[start + i];
    }

    /// <summary>
    /// glTF's spherical linear interpolation: a = arccos(|q_k · q_k+1|), the next quaternion taken
    /// with the dot product's sign (the short way along the great circle); linear where a is about 0.
    /// </summary>
    void Slerp(int k, double t, Span<double> result)
    {
        int a0 = 4 * k, b0 = 4 * (k + 1);
        double dot = 0;
        for (var i = 0; i < 4; i++)
            dot += (double)Values[a0 + i] * Values[b0 + i];
        var sign = dot < 0 ? -1.0 : 1.0;
        var angle = Math.Acos(Math.Min(Math.Abs(dot), 1));
        var sine = Math.Sin(angle);
        double wa, wb;
        if (sine < 1e-9)
        {
            wa = 1 - t;
            wb = sign * t;
        }
        else
        {
            wa = Math.Sin(angle * (1 - t)) / sine;
            wb = sign * Math.Sin(angle * t) / sine;
        }
        for (var i = 0; i < 4; i++)
            result[i] = wa * Values[a0 + i] + wb * Values[b0 + i];
    }

    /// <summary>
    /// glTF's cubic Hermite spline: v_t = (2t³ − 3t² + 1)·v_k + t_d·(t³ − 2t² + t)·b_k + (−2t³ + 3t²)·v_k+1
    /// + t_d·(t³ − t²)·a_k+1, with the tangents in units per second scaled by the segment's duration
    /// t_d; rotations normalised after.
    /// </summary>
    void Cubic(int k, double t, double duration, Span<double> result)
    {
        var c = Components;
        double t2 = t * t, t3 = t2 * t;
        double hv0 = 2 * t3 - 3 * t2 + 1, hb0 = duration * (t3 - 2 * t2 + t), hv1 = -2 * t3 + 3 * t2, ha1 = duration * (t3 - t2);
        int value0 = (3 * k + 1) * c, out0 = (3 * k + 2) * c, in1 = (3 * (k + 1)) * c, value1 = (3 * (k + 1) + 1) * c;
        for (var i = 0; i < c; i++)
            result[i] = hv0 * Values[value0 + i] + hb0 * Values[out0 + i] + hv1 * Values[value1 + i] + ha1 * Values[in1 + i];
        if (Path == AnimationPath.Rotation)
        {
            var length = Math.Sqrt(result[0] * result[0] + result[1] * result[1] + result[2] * result[2] + result[3] * result[3]);
            if (length > 0)
                for (var i = 0; i < 4; i++)
                    result[i] /= length;
        }
    }
}

/// <summary>
/// The curves a clip has for one node, found again on a model by the node's name (Docs/Animation.md:
/// clips are shared by bone names). A bone's track (<see cref="Relative"/>) holds Blender's pose
/// channels: location, rotation and scale relative to the bone's rest transform, so it plays on any
/// model with a bone of that name, whatever its proportions; any other node's track holds its local
/// transform itself (Blender's object channels have no rest).
/// </summary>
public sealed record AnimationTrack(string Node, bool Relative, AnimationCurve? Translation, AnimationCurve? Rotation, AnimationCurve? Scale);

/// <summary>
/// A clip: one glTF animation (one Blender action), its tracks per node, keyed channels only (the
/// clips are exported with unkeyed bones dropped, Docs/Animation.md).
/// </summary>
public sealed class AnimationClip
{
    public AnimationClip(string name, IReadOnlyList<AnimationTrack> tracks, IReadOnlyList<ModelAnimationEvent>? events = null)
    {
        Name = name;
        Tracks = tracks;
        Events = events ?? [];
        Duration = tracks.SelectMany(t => new[] { t.Translation, t.Rotation, t.Scale }).Where(c => c != null).Select(c => c!.End).DefaultIfEmpty(0).Max();
    }

    public string Name { get; }
    public IReadOnlyList<AnimationTrack> Tracks { get; }
    /// <summary>Its events (Blender's pose markers), in time order: seconds from the clip's start.</summary>
    public IReadOnlyList<ModelAnimationEvent> Events { get; }
    /// <summary>Seconds from the clip's start (time 0, as glTF counts) to its last key.</summary>
    public float Duration { get; }

    /// <summary>Every animation of a model as a clip.</summary>
    public static AnimationClip[] AllOf(ModelData model) => model.Animations.Select(a => From(model, a)).ToArray();

    /// <summary>
    /// A model's animation as a clip. A channel on one of the model's skin joints (a Blender bone)
    /// is turned into its rest-relative value: Blender's exporter writes a bone's local transform as
    /// its rest transform times its pose channels (rest · basis), so the keys are taken back through
    /// the inverse of the node's rest translation, rotation and scale (tangents through the linear
    /// part only). Sampling commutes with this, as the rest is fixed.
    /// </summary>
    public static AnimationClip From(ModelData model, ModelAnimation animation)
    {
        var joints = model.Skins.SelectMany(s => s.Joints).ToHashSet();
        var tracks = new List<AnimationTrack>();
        foreach (var group in animation.Channels.GroupBy(c => c.Node))
        {
            var node = model.Nodes[group.Key];
            var relative = joints.Contains(group.Key);
            AnimationCurve? translation = null, rotation = null, scale = null;
            foreach (var channel in group)
            {
                var values = relative ? Relativize(channel, node) : channel.Values;
                var curve = new AnimationCurve(channel.Path, channel.Interpolation, channel.Times, values);
                switch (channel.Path)
                {
                    case AnimationPath.Translation: translation = curve; break;
                    case AnimationPath.Rotation: rotation = curve; break;
                    default: scale = curve; break;
                }
            }
            tracks.Add(new AnimationTrack(node.Name, relative, translation, rotation, scale));
        }
        return new AnimationClip(animation.Name, tracks, animation.Events);
    }

    /// <summary>A bone channel's keys relative to the node's rest: t_b = S⁻¹·R⁻¹·(t − t_rest), q_b = q_rest⁻¹·q, s_b = s / s_rest.</summary>
    static float[] Relativize(ModelAnimationChannel channel, ModelNode node)
    {
        var values = (float[])channel.Values.Clone();
        var c = channel.Components;
        var cubic = channel.Interpolation == AnimationInterpolation.CubicSpline;
        var inverse = Quaternion.Conjugate(Quaternion.Normalize(node.Rotation));
        for (var i = 0; i < values.Length; i += c)
        {
            // With CUBICSPLINE the in- and out-tangents (first and third of each key) are directions.
            var tangent = cubic && (i / c) % 3 != 1;
            switch (channel.Path)
            {
                case AnimationPath.Translation:
                    var v = new Vector3(values[i], values[i + 1], values[i + 2]);
                    if (!tangent)
                        v -= node.Translation;
                    v = Vector3.Transform(v, inverse) / node.Scale;
                    (values[i], values[i + 1], values[i + 2]) = (v.X, v.Y, v.Z);
                    break;
                case AnimationPath.Rotation:
                    var q = inverse * new Quaternion(values[i], values[i + 1], values[i + 2], values[i + 3]);
                    (values[i], values[i + 1], values[i + 2], values[i + 3]) = (q.X, q.Y, q.Z, q.W);
                    break;
                default:
                    values[i] /= node.Scale.X;
                    values[i + 1] /= node.Scale.Y;
                    values[i + 2] /= node.Scale.Z;
                    break;
            }
        }
        return values;
    }
}
