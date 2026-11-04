using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;

namespace Statventure.Scene;

/// <summary>
/// A model's hierarchy instanced under a node (<see cref="SceneNode.Instance(Model, Palette?, bool)"/>;
/// Docs/Scene.md, Q2): a scene node for each model node the model's scene reaches, named as in the
/// file, with its local translation, rotation and scale (a node given as a matrix decomposed), the
/// model's roots under the node it hangs on. Its meshes are drawn from those nodes' world matrices
/// (a skinned mesh by its joints' nodes), each top-level object (a root and its descendants) a model
/// of its own: its own outline and glow mask, its hatching anchored at its origin, and picking giving
/// back its top-level node. Clips of the model (a Blender object's action: absolute local channels;
/// a bone's: on its rest) play on those nodes through <see cref="Skeleton"/>, the model's nodes by
/// name, as <see cref="ClipBinding"/> binds a clip to a model (<see cref="Apply(AnimationStack, double)"/>).
/// A stage's cameras and its sun (Docs/Scene.md, step 3, S3-5) hang on the nodes made for the nodes
/// that carry them, as a <see cref="SceneCamera"/> and a <see cref="SceneSun"/> with the model's settings
/// (<see cref="Cameras"/>, <see cref="Sun"/>); nothing makes them the scene's camera or sun by itself.
/// </summary>
public sealed class ModelInstance : SceneAttachment
{
    readonly SceneNode?[] _nodes;
    readonly int[] _top;
    readonly List<DrawItem> _scratch = [];
    readonly HashSet<int> _applied = [];
    readonly ModelPose _pose = new();
    Skeleton? _skeleton;
    PoseBuffer? _buffer;
    Palette? _palette;
    bool _scenery;

    internal ModelInstance(SceneNode under, ModelData data, Model? model)
    {
        Data = data;
        Model = model;
        _nodes = new SceneNode?[data.Nodes.Count];
        _top = new int[data.Nodes.Count];
        Array.Fill(_top, -1);
        var roots = new List<SceneNode>();
        var stack = new Stack<(int Node, SceneNode Parent, int Top)>();
        for (var r = data.Roots.Count - 1; r >= 0; r--)
            stack.Push((data.Roots[r], under, data.Roots[r]));
        while (stack.Count > 0)
        {
            var (index, parent, top) = stack.Pop();
            var source = data.Nodes[index];
            var (translation, rotation, scale) = LocalOf(source);
            var node = new SceneNode(source.Name);
            node.SetLocal(translation, rotation, scale);
            parent.Add(node);
            if (parent == under)
                roots.Add(node);
            _nodes[index] = node;
            _top[index] = top;
            for (var c = source.Children.Count - 1; c >= 0; c--)
                stack.Push((source.Children[c], node, top));
        }
        Roots = roots;
        var cameras = new List<SceneCamera>();
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i] is not { } node)
                continue;
            if (data.Nodes[i].Camera >= 0)
            {
                var camera = data.Cameras[data.Nodes[i].Camera];
                var attached = new SceneCamera { Projection = camera.Projection, Near = camera.Near, Far = camera.Far };
                if (camera.Projection == Projection.Perspective)
                    attached.VerticalFieldOfView = camera.VerticalFieldOfView;
                else
                    attached.OrthographicHeight = camera.OrthographicHeight;
                cameras.Add(node.Attach(attached));
            }
            if (data.Nodes[i].Light >= 0)
            {
                var light = data.Lights[data.Nodes[i].Light];
                Sun = node.Attach(new SceneSun { Strength = light.Strength, Angle = light.Angle });
            }
        }
        Cameras = cameras;
    }

    /// <summary>A node's local transform as translation, rotation and scale: the file's, or its matrix decomposed when it gave one.</summary>
    static (Vector3, Quaternion, Vector3) LocalOf(ModelNode node)
    {
        var composed = Matrix4x4.CreateScale(node.Scale) * Matrix4x4.CreateFromQuaternion(node.Rotation) * Matrix4x4.CreateTranslation(node.Translation);
        if (composed == node.Local)
            return (node.Translation, node.Rotation, node.Scale);
        if (!Matrix4x4.Decompose(node.Local, out var scale, out var rotation, out var translation))
            throw new InvalidDataException($"node {node.Name}'s matrix isn't a translation, rotation and scale");
        return (translation, rotation, scale);
    }

    public ModelData Data { get; }
    /// <summary>The model drawn, or null for a hierarchy only (<see cref="SceneNode.Instance(ModelData)"/>).</summary>
    public Model? Model { get; }
    public Palette? Palette { get => _palette; set => Set(ref _palette, value); }
    /// <summary>Whether it receives shadows (the stage).</summary>
    public bool Scenery { get => _scenery; set => Set(ref _scenery, value); }
    /// <summary>The scene nodes made for the model's roots: its top-level objects.</summary>
    public IReadOnlyList<SceneNode> Roots { get; }
    /// <summary>The cameras on its nodes (the model's nodes in file order; nodes its scene doesn't reach have none).</summary>
    public IReadOnlyList<SceneCamera> Cameras { get; }
    /// <summary>The sun on its node, or null (a model has one sun or none).</summary>
    public SceneSun? Sun { get; }

    /// <summary>The scene node made for the model's node <paramref name="index"/> (null for one the model's scene doesn't reach).</summary>
    public SceneNode? NodeOf(int index) => _nodes[index];

    /// <summary>The scene node made for the model's node of this name (a joint first), or null.</summary>
    public SceneNode? NodeNamed(string name) => Skeleton.Find(name) is var index and >= 0 ? _nodes[index] : null;

    /// <summary>The model's nodes by name, for binding its clips (<see cref="ClipBinding"/>, <see cref="AnimationStack"/>).</summary>
    public Skeleton Skeleton => _skeleton ??= new Skeleton(Data);

    /// <summary>One of the model's clips by name, bound to its nodes.</summary>
    public ClipBinding Clip(string name) =>
        new(AnimationClip.AllOf(Data).FirstOrDefault(c => c.Name == name) ?? throw new ArgumentException($"the model has no clip {name}"), Skeleton);

    /// <summary>Sets the nodes <paramref name="clip"/> keys to its channels at <paramref name="time"/> (seconds from its start).</summary>
    public void Apply(ClipBinding clip, double time)
    {
        CheckSkeleton(clip.Skeleton);
        var buffer = Buffer();
        buffer.Clear();
        clip.Sample(time, buffer);
        Apply(buffer);
    }

    /// <summary>
    /// Sets the nodes to what <paramref name="stack"/> (on <see cref="Skeleton"/>) gives at
    /// <paramref name="time"/>: each node a strip sets now takes its channels (the others of it its
    /// rest's, as a pose does); a node set by the last call that no strip sets now goes back to its
    /// rest (the model's own transform), as unset channels are rest (Docs/Animation.md).
    /// </summary>
    public void Apply(AnimationStack stack, double time)
    {
        CheckSkeleton(stack.Skeleton);
        var buffer = Buffer();
        stack.Evaluate(time, buffer);
        Apply(buffer);
    }

    void CheckSkeleton(Skeleton skeleton)
    {
        if (skeleton.Model != Data)
            throw new ArgumentException("the clip is bound to another model's nodes: bind it to the instance's Skeleton");
    }

    PoseBuffer Buffer() => _buffer ??= new PoseBuffer(Skeleton);

    void Apply(PoseBuffer buffer)
    {
        buffer.ToPose(_pose);
        for (var i = 0; i < _nodes.Length; i++)
        {
            if (_nodes[i] is not { } node)
                continue;
            if (buffer.Set[i] != ChannelSet.None && _pose.TryGet(i, out var local))
            {
                node.SetLocal(local.Translation, local.Rotation, local.Scale);
                _applied.Add(i);
            }
            else if (_applied.Remove(i))
            {
                var (translation, rotation, scale) = LocalOf(Data.Nodes[i]);
                node.SetLocal(translation, rotation, scale);
            }
        }
    }

    /// <summary>
    /// Adds its meshes' draw items from the nodes' world matrices (nodes taken out of the scene
    /// and hidden ones aren't drawn), each item in its top-level object's group and anchored at that
    /// object's origin.
    /// </summary>
    internal override void Draw(List<DrawItem> items, SceneDrawing drawing)
    {
        if (Model is not { } model || Node?.Root is not { } root)
            return;
        var world = new Matrix4x4[_nodes.Length];
        for (var i = 0; i < _nodes.Length; i++)
            world[i] = _nodes[i]?.World ?? Matrix4x4.Identity;
        _scratch.Clear();
        model.Draw(_scratch, Matrix4x4.Identity, _palette, drawing.Style, _scenery, world: world);
        foreach (var item in _scratch)
        {
            var node = item.Part - 1;
            if (_nodes[node] is not { } drawn || drawn.Root != root || !drawn.Shown)
                continue;
            var top = _nodes[_top[node]]!;
            items.Add(item with { Group = top, Anchor = top.World.Translation });
        }
    }
}
