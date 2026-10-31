using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Animation;

/// <summary>
/// A socket bone of a wearer (Docs/Animation.md, "Gear in detail"): a non-deforming bone named
/// <c>socket.&lt;slot&gt;</c>; an item's origin is its grip and its axes line up with the socket's.
/// Blender's glTF exporter keeps a bone's own axes in its joint node but converts an object's axes
/// to glTF's (x, y, z) → (x, z, −y), so an item parented to a socket in Blender (its axes the bone's)
/// sits in glTF at the socket's world after that conversion is undone: item model space through C⁻¹
/// ((x, y, z) → (x, −z, y)), then the socket's posed world.
/// </summary>
public sealed class Socket
{
    /// <summary>C⁻¹ as a row-vector matrix: glTF object axes back to Blender's, as a bone's axes are.</summary>
    public static readonly Matrix4x4 ObjectAxesToBone = new(
        1, 0, 0, 0,
        0, 0, 1, 0,
        0, -1, 0, 0,
        0, 0, 0, 1);

    public Socket(Skeleton wearer, string name)
    {
        Node = wearer.Find(name);
        if (Node < 0)
            throw new ArgumentException($"the wearer has no socket {name}");
        Name = name;
    }

    public string Name { get; }
    public int Node { get; }

    /// <summary>The item's model-to-world transform: the socket's world in <paramref name="wearerWorld"/> (the wearer's posed node matrices), then the wearer's own transform.</summary>
    public Matrix4x4 ItemTransform(Matrix4x4[] wearerWorld, Matrix4x4 wearerTransform) => ObjectAxesToBone * wearerWorld[Node] * wearerTransform;
}

/// <summary>
/// Clothing skinned to a wearer's skeleton by bone names, deformed as Blender's Armature modifier
/// deforms a mesh bound to the wearer's armature (MOD_armature, armature_deform.cc): each joint moves
/// as the wearer's bone of its name moves from the wearer's rest to its pose (the deformation
/// rest⁻¹ · posed in model space), applied to the garment where it was made. On the body it was made
/// on this is glTF's skinning; on another body the garment doesn't refit (garments are made per body
/// type). Joints the wearer lacks stay at the garment's rest.
/// </summary>
public sealed class Garment
{
    readonly int[] _wearerNode;
    readonly Matrix4x4[] _rest;

    public Garment(ModelData clothing, Skeleton wearer)
    {
        Clothing = clothing;
        var joints = clothing.Skins.SelectMany(s => s.Joints).ToHashSet();
        _wearerNode = clothing.Nodes.Select((n, i) => joints.Contains(i) ? wearer.Find(n.Name) : -1).ToArray();
        _rest = clothing.WorldMatrices();
    }

    public ModelData Clothing { get; }

    /// <summary>
    /// The garment's node world matrices for the wearer's pose: a joint matched by name at its rest
    /// world times the wearer bone's deformation; any other node by the hierarchy over them.
    /// </summary>
    public Matrix4x4[] WorldMatrices(Matrix4x4[] wearerRest, Matrix4x4[] wearerPosed)
    {
        var nodes = Clothing.Nodes;
        var world = new Matrix4x4[nodes.Count];
        var stack = new Stack<(int Node, Matrix4x4 Parent)>();
        foreach (var root in Clothing.Roots)
            stack.Push((root, Matrix4x4.Identity));
        while (stack.Count > 0)
        {
            var (node, parent) = stack.Pop();
            var w = _wearerNode[node];
            if (w >= 0)
            {
                Matrix4x4.Invert(wearerRest[w], out var inverse);
                world[node] = _rest[node] * (inverse * wearerPosed[w]);
            }
            else
                world[node] = nodes[node].Local * parent;
            foreach (var child in nodes[node].Children)
                stack.Push((child, world[node]));
        }
        return world;
    }
}

/// <summary>
/// An item's own clips played with its wearer's state machine (Docs/Animation.md, "Gear in
/// detail"): the item mirrors each machine layer; whenever the wearer's layer has a state's entry,
/// the item has one with the same fade, playing its own clip of the state's name on the same clock
/// (start, speed and looping as the wearer's strip), or nothing when it has no such clip.
/// </summary>
public sealed class ItemAnimation
{
    readonly StateMachine _wearer;
    readonly Dictionary<string, ClipBinding> _clips;
    readonly (string Name, BlendMode Mode, FadeLayer Fades)[] _layers;
    readonly Dictionary<FadeEntry, FadeEntry> _mirror = [];
    readonly PoseBuffer _buffer;

    public ItemAnimation(ModelData item, StateMachine wearer)
    {
        _wearer = wearer;
        Skeleton = new Skeleton(item);
        _clips = AnimationClip.AllOf(item).ToDictionary(c => c.Name, c => new ClipBinding(c, Skeleton));
        Stack = new AnimationStack(Skeleton);
        _layers = wearer.Definition.Layers.Select(l => (l.Name, l.Mode, new FadeLayer())).ToArray();
        foreach (var (_, _, fades) in _layers)
            Stack.Layers.Add(fades);
        _buffer = new PoseBuffer(Skeleton);
    }

    public Skeleton Skeleton { get; }
    public AnimationStack Stack { get; }

    /// <summary>The item's pose at <paramref name="time"/> (the wearer's animator's clock), after bringing its layers in step with the wearer's.</summary>
    public ModelPose Pose(double time, ModelPose? into = null)
    {
        Evaluate(time, _buffer);
        return _buffer.ToPose(into);
    }

    /// <summary>The item's channels at <paramref name="time"/> into <paramref name="into"/>.</summary>
    public void Evaluate(double time, PoseBuffer into)
    {
        Sync();
        Stack.Evaluate(time, into);
    }

    void Sync()
    {
        var alive = new HashSet<FadeEntry>();
        foreach (var (name, mode, fades) in _layers)
        {
            fades.Entries.Clear();
            foreach (var entry in _wearer.FadesOf(name).Entries)
            {
                alive.Add(entry);
                if (!_mirror.TryGetValue(entry, out var mirrored))
                {
                    var clip = _clips.GetValueOrDefault(_wearer.StateNameOf(entry));
                    var strip = clip == null ? null : new AnimationStrip(clip) { Mode = mode, Extrapolation = Extrapolation.Hold };
                    _mirror[entry] = mirrored = new FadeEntry(strip, entry.FadeStart, entry.FadeDuration);
                }
                if (mirrored.Strip is { } own)
                {
                    // On the wearer's clock: its strip's start, speed (re-timed by a speed parameter)
                    // and looping; an empty wearer state has none, so the item's starts with the fade.
                    own.Start = entry.Strip?.Start ?? entry.FadeStart;
                    own.Scale = entry.Strip?.Scale ?? 1;
                    own.Repeat = entry.Strip?.Repeat ?? 1;
                }
                fades.Entries.Add(mirrored);
            }
        }
        foreach (var gone in _mirror.Keys.Where(k => !alive.Contains(k)).ToList())
            _mirror.Remove(gone);
    }
}
