using System.Collections.Concurrent;
using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Animation;

/// <summary>
/// A model's nodes as animation sees them: found by name (clips are shared by bone names,
/// Docs/Animation.md) with their rest transforms, the file's translation, rotation and scale.
/// </summary>
public sealed class Skeleton
{
    readonly Dictionary<string, int> _byName = [];
    readonly ConcurrentDictionary<string, Lazy<ContactShape?>> _shapes = new();

    /// <summary>
    /// The skeleton of <paramref name="model"/>, the body, wearing <paramref name="worn"/>: the garments bound to its bones
    /// by name (its parts, its clothing; its rigid items not), which with its skin are what a clip's contacts meet the
    /// ground with (GM2-P37; <see cref="ClipContacts"/>). A change of what is worn is a new skeleton, whose bindings work
    /// the contacts out again.
    /// </summary>
    public Skeleton(ModelData model, IEnumerable<ModelData>? worn = null)
    {
        Model = model;
        Worn = worn?.ToArray() ?? [];
        var joints = model.Skins.SelectMany(s => s.Joints).ToHashSet();
        // A name used twice (two armatures' bones, an object named like a bone) finds a joint first.
        foreach (var pass in new[] { true, false })
            for (var i = 0; i < model.Nodes.Count; i++)
                if (joints.Contains(i) == pass)
                    _byName.TryAdd(model.Nodes[i].Name, i);
    }

    public ModelData Model { get; }
    /// <summary>The garments the body wears on its bones (GM2-P37: they meet the ground with its skin).</summary>
    public IReadOnlyList<ModelData> Worn { get; }
    public int Count => Model.Nodes.Count;

    /// <summary>The body and its worn set as a clip's contacts meet the ground (once per set of chains), or null when it can't (no mesh, a joint missing).</summary>
    internal ContactShape? ShapeFor(ClipContacts contacts) =>
        _shapes.GetOrAdd(contacts.Signature, _ => new Lazy<ContactShape?>(() => ContactShape.Of(this, contacts), LazyThreadSafetyMode.ExecutionAndPublication)).Value;

    /// <summary>The node of this name (a joint before any other node), or −1.</summary>
    public int Find(string name) => _byName.TryGetValue(name, out var node) ? node : -1;

    public ModelNode this[int node] => Model.Nodes[node];

    Matrix4x4[]? _rest;

    /// <summary>
    /// The length of a chain of joints at rest (GM2-P22: a clip's measure, <see cref="ClipMeasure"/>): the distances from
    /// each joint's place in model space to the next's, added up, in double precision; null when a joint isn't here or the
    /// chain has fewer than two.
    /// </summary>
    public double? ChainLength(IReadOnlyList<string> chain)
    {
        if (chain.Count < 2)
            return null;
        _rest ??= Model.WorldMatrices();
        var length = 0.0;
        Vector3? last = null;
        foreach (var name in chain)
        {
            var node = Find(name);
            if (node < 0)
                return null;
            var place = _rest[node].Translation;
            if (last is { } previous)
            {
                double dx = (double)place.X - previous.X, dy = (double)place.Y - previous.Y, dz = (double)place.Z - previous.Z;
                length += Math.Sqrt(dx * dx + dy * dy + dz * dz);
            }
            last = place;
        }
        return length;
    }

    /// <summary>The length of a chain of joints at rest on a model (<see cref="ChainLength(IReadOnlyList{string})"/>).</summary>
    public static double? ChainLength(ModelData model, IReadOnlyList<string> chain) => new Skeleton(model).ChainLength(chain);
}

/// <summary>
/// Channel values for each node of a skeleton and which of them are set (Blender's blend domain):
/// a bone's values are rest-relative (Blender's pose channels: location, rotation, scale on top of
/// the rest transform), another node's are its local transform itself. Rotations are kept as four
/// numbers that needn't be unit (Blender blends the components and normalises when it builds the
/// pose). Unset channels are the rest pose.
/// </summary>
public sealed class PoseBuffer
{
    public PoseBuffer(Skeleton skeleton)
    {
        Skeleton = skeleton;
        var n = skeleton.Count;
        Translation = new double[3 * n];
        Rotation = new double[4 * n];
        Scale = new double[3 * n];
        Set = new ChannelSet[n];
        Relative = new bool[n];
        Clear();
    }

    public Skeleton Skeleton { get; }
    public double[] Translation { get; }
    /// <summary>x, y, z, w per node.</summary>
    public double[] Rotation { get; }
    public double[] Scale { get; }
    public ChannelSet[] Set { get; }
    /// <summary>Whether a node's values are rest-relative (a bone's track set them).</summary>
    public bool[] Relative { get; }

    /// <summary>Every channel unset, at Blender's defaults (location 0, the unit quaternion, scale 1).</summary>
    public void Clear()
    {
        Array.Clear(Translation);
        Array.Clear(Rotation);
        Array.Fill(Scale, 1.0);
        for (var i = 0; i < Set.Length; i++)
        {
            Rotation[4 * i + 3] = 1;
            Set[i] = ChannelSet.None;
            Relative[i] = true;
        }
    }

    /// <summary>Takes every value and flag of <paramref name="other"/> (of the same skeleton).</summary>
    public void CopyFrom(PoseBuffer other)
    {
        Array.Copy(other.Translation, Translation, Translation.Length);
        Array.Copy(other.Rotation, Rotation, Rotation.Length);
        Array.Copy(other.Scale, Scale, Scale.Length);
        Array.Copy(other.Set, Set, Set.Length);
        Array.Copy(other.Relative, Relative, Relative.Length);
    }

    /// <summary>
    /// Mixes toward <paramref name="other"/> by <paramref name="weight"/> on every channel either has
    /// set, component by component as Replace mixes (Blender's transitions blend all channels); the
    /// unset side holds its default (both sides of a fade start from the same lower values).
    /// </summary>
    public void MixToward(PoseBuffer other, double weight)
    {
        for (var node = 0; node < Set.Length; node++)
        {
            var set = Set[node] | other.Set[node];
            if (set == ChannelSet.None)
                continue;
            if (other.Set[node] != ChannelSet.None)
                Relative[node] = other.Relative[node];
            Set[node] = set;
            if ((set & ChannelSet.Translation) != 0)
                for (var c = 3 * node; c < 3 * node + 3; c++)
                    Translation[c] = Translation[c] * (1 - weight) + other.Translation[c] * weight;
            if ((set & ChannelSet.Rotation) != 0)
                for (var c = 4 * node; c < 4 * node + 4; c++)
                    Rotation[c] = Rotation[c] * (1 - weight) + other.Rotation[c] * weight;
            if ((set & ChannelSet.Scale) != 0)
                for (var c = 3 * node; c < 3 * node + 3; c++)
                    Scale[c] = Scale[c] * (1 - weight) + other.Scale[c] * weight;
        }
    }

    /// <summary>
    /// The pose these channels give: for each node with a channel set, a bone's local transform is
    /// its rest transform times its channels (T(location)·R·S after the rest, Blender's
    /// BKE_pchan_to_mat4: the rotation normalised first), another node's the channels themselves,
    /// unset ones from the rest. Composed as translation, rotation and scale, which is exact while
    /// the rest scale is uniform (Blender's bones have none).
    /// </summary>
    public ModelPose ToPose(ModelPose? into = null)
    {
        var pose = into ?? new ModelPose();
        for (var i = 0; i < Set.Length; i++)
        {
            if (Set[i] == ChannelSet.None)
                continue;
            var node = Skeleton[i];
            var t = new Vector3((float)Translation[3 * i], (float)Translation[3 * i + 1], (float)Translation[3 * i + 2]);
            var s = new Vector3((float)Scale[3 * i], (float)Scale[3 * i + 1], (float)Scale[3 * i + 2]);
            var r = NormalizedRotation(i);
            if (Relative[i])
            {
                var restRotation = Quaternion.Normalize(node.Rotation);
                pose.Set(i, node.Translation + Vector3.Transform(node.Scale * t, restRotation), restRotation * r, node.Scale * s);
            }
            else
                pose.Set(i,
                    (Set[i] & ChannelSet.Translation) != 0 ? t : node.Translation,
                    (Set[i] & ChannelSet.Rotation) != 0 ? r : node.Rotation,
                    (Set[i] & ChannelSet.Scale) != 0 ? s : node.Scale);
        }
        return pose;
    }

    Quaternion NormalizedRotation(int node)
    {
        double x = Rotation[4 * node], y = Rotation[4 * node + 1], z = Rotation[4 * node + 2], w = Rotation[4 * node + 3];
        var length = Math.Sqrt(x * x + y * y + z * z + w * w);
        // Blender's normalize_qt_qt: a zero quaternion becomes the unit one.
        return length > 1e-35 ? new Quaternion((float)(x / length), (float)(y / length), (float)(z / length), (float)(w / length)) : Quaternion.Identity;
    }
}

[Flags]
public enum ChannelSet : byte { None = 0, Translation = 1, Rotation = 2, Scale = 4 }

/// <summary>
/// A clip bound to a skeleton: each track found by its node's name (tracks for nodes the skeleton lacks are left out).
/// A clip's translations follow the body's size (GM2-P22, "the figure's ground"; Docs/Animation.md): a bone's place is
/// metres keyed on one body, and on a body whose measure (<see cref="AnimationClip.Measure"/>: its legs, for the
/// humanoids) is another length it is scaled by <see cref="Scale"/>, so the legs fold by that body's lengths under hips
/// that move by that body's too. A clip that keeps its contacts (GM2-P37, <see cref="AnimationClip.Contacts"/>) plays on a
/// body other than the one it was keyed on with its root's place and its chains' turns made again so that what touches the
/// ground on the keyed body touches it on this one (<see cref="Contacts"/>), worked out once per skeleton (its body and
/// worn set) and clip.
/// </summary>
public sealed class ClipBinding
{
    readonly (AnimationTrack Track, int Node, bool Measured)[] _tracks;

    public ClipBinding(AnimationClip clip, Skeleton skeleton) : this(clip, skeleton, contacts: true)
    {
    }

    /// <summary>A binding; with <paramref name="contacts"/> false the clip's own channels, as GM2-P22 scales them (what the contacts are worked out from).</summary>
    internal ClipBinding(AnimationClip clip, Skeleton skeleton, bool contacts)
    {
        Clip = clip;
        Skeleton = skeleton;
        _tracks = clip.Tracks.Select(t => (t, skeleton.Find(t.Node), true)).Where(t => t.Item2 >= 0).ToArray();
        Scale = clip.Measure is { } measure && skeleton.ChainLength(measure.Chain) is { } length and > 0 ? length / measure.Length : 1;
        if (contacts && clip.Contacts is { } kept && skeleton.ShapeFor(kept) is { } shape && shape.CorrectionOf(clip, kept) is { } correction)
        {
            Contacts = correction;
            // The corrected nodes play the correction's tracks: the root's place already in this body's measure.
            for (var i = 0; i < _tracks.Length; i++)
                if (correction.Tracks.TryGetValue(_tracks[i].Node, out var track))
                    _tracks[i] = (track, _tracks[i].Node, track.Translation == _tracks[i].Track.Translation);
            _tracks = [.. _tracks, .. correction.Tracks.Where(t => _tracks.All(x => x.Node != t.Key)).Select(t => (t.Value, t.Key, false))];
        }
    }

    public AnimationClip Clip { get; }
    public Skeleton Skeleton { get; }

    /// <summary>
    /// What keeping the clip's contacts changes on this body (GM2-P37), or null when it changes nothing: a clip without
    /// contacts, a body without a mesh or a joint they name, or the body the clip was keyed on (the same numbers: nothing
    /// moves, so its poses are the clip's to the bit).
    /// </summary>
    public ContactCorrection? Contacts { get; }

    /// <summary>
    /// What a bone's place keyed in the clip is multiplied by on this skeleton: the clip's measure's chain length here over
    /// its length on the body the clip was keyed on (exactly 1 on that body, whose chain is the same numbers); 1 when the
    /// clip has no measure or this skeleton hasn't its chain. Rotations and scales are angles and ratios, never scaled;
    /// nor is a plain node's place (its local transform itself, not a bone's place from its rest).
    /// </summary>
    public double Scale { get; }

    /// <summary>The skeleton's nodes whose given channel the clip drives.</summary>
    public IEnumerable<int> NodesDriving(ChannelSet channel) => _tracks.Where(t => channel switch
    {
        ChannelSet.Translation => t.Track.Translation != null,
        ChannelSet.Rotation => t.Track.Rotation != null,
        ChannelSet.Scale => t.Track.Scale != null,
        _ => false,
    }).Select(t => t.Node);

    /// <summary>Writes the clip's channels at <paramref name="time"/> (seconds) into the buffer, marking them set.</summary>
    public void Sample(double time, PoseBuffer into)
    {
        Span<double> value = stackalloc double[4];
        foreach (var (track, node, measured) in _tracks)
        {
            into.Relative[node] = track.Relative;
            if (track.Translation is { } translation)
            {
                translation.Evaluate(time, value);
                if (track.Relative && Scale != 1 && measured)
                    for (var c = 0; c < 3; c++)
                        value[c] *= Scale;
                value[..3].CopyTo(into.Translation.AsSpan(3 * node, 3));
                into.Set[node] |= ChannelSet.Translation;
            }
            if (track.Rotation is { } rotation)
            {
                rotation.Evaluate(time, value);
                // glTF keeps no quaternion's sign (Blender's exporter writes w ≥ 0), but Blender's NLA
                // blends the stored components: w ≥ 0 is what keys within a half turn of rest store.
                if (value[3] < 0)
                    for (var c = 0; c < 4; c++)
                        value[c] = -value[c];
                value.CopyTo(into.Rotation.AsSpan(4 * node, 4));
                into.Set[node] |= ChannelSet.Rotation;
            }
            if (track.Scale is { } scale)
            {
                scale.Evaluate(time, value);
                value[..3].CopyTo(into.Scale.AsSpan(3 * node, 3));
                into.Set[node] |= ChannelSet.Scale;
            }
        }
    }

    /// <summary>The model's pose with this clip alone at <paramref name="time"/>.</summary>
    public ModelPose Pose(double time)
    {
        var buffer = new PoseBuffer(Skeleton);
        Sample(time, buffer);
        return buffer.ToPose();
    }
}
