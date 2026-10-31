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

    public Skeleton(ModelData model)
    {
        Model = model;
        var joints = model.Skins.SelectMany(s => s.Joints).ToHashSet();
        // A name used twice (two armatures' bones, an object named like a bone) finds a joint first.
        foreach (var pass in new[] { true, false })
            for (var i = 0; i < model.Nodes.Count; i++)
                if (joints.Contains(i) == pass)
                    _byName.TryAdd(model.Nodes[i].Name, i);
    }

    public ModelData Model { get; }
    public int Count => Model.Nodes.Count;

    /// <summary>The node of this name (a joint before any other node), or −1.</summary>
    public int Find(string name) => _byName.TryGetValue(name, out var node) ? node : -1;

    public ModelNode this[int node] => Model.Nodes[node];
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

/// <summary>A clip bound to a skeleton: each track found by its node's name (tracks for nodes the skeleton lacks are left out).</summary>
public sealed class ClipBinding
{
    readonly (AnimationTrack Track, int Node)[] _tracks;

    public ClipBinding(AnimationClip clip, Skeleton skeleton)
    {
        Clip = clip;
        Skeleton = skeleton;
        _tracks = clip.Tracks.Select(t => (t, skeleton.Find(t.Node))).Where(t => t.Item2 >= 0).ToArray();
    }

    public AnimationClip Clip { get; }
    public Skeleton Skeleton { get; }

    /// <summary>Writes the clip's channels at <paramref name="time"/> (seconds) into the buffer, marking them set.</summary>
    public void Sample(double time, PoseBuffer into)
    {
        Span<double> value = stackalloc double[4];
        foreach (var (track, node) in _tracks)
        {
            into.Relative[node] = track.Relative;
            if (track.Translation is { } translation)
            {
                translation.Evaluate(time, value);
                value[..3].CopyTo(into.Translation.AsSpan(3 * node, 3));
                into.Set[node] |= ChannelSet.Translation;
            }
            if (track.Rotation is { } rotation)
            {
                rotation.Evaluate(time, value);
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
