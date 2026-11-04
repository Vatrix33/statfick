using System.Globalization;
using System.Numerics;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Render3D;

// A model's data is a record (Docs/Assets.md, "How it works", "Models and state machines in packs"): one
// entry of models.pak per .glb holding what GltfImport reads from it (ModelImporter), its types model.*.

/// <summary>The four joints a skinned vertex follows (glTF's JOINTS_0), as indices into its skin's joints.</summary>
[Record("model.joints", 1)]
public readonly record struct JointIndices([property: Field(1)] ushort J0, [property: Field(2)] ushort J1, [property: Field(3)] ushort J2,
    [property: Field(4)] ushort J3);

/// <summary>Triangles with one material: vertex attributes as glTF gives them, 32-bit indices.</summary>
[Record("model.primitive", 1)]
public sealed class ModelPrimitive
{
    [Field(1)] public required Vector3[] Positions { get; init; }
    /// <summary>The file's normals, or flat face normals when it has none (as glTF asks).</summary>
    [Field(2)] public required Vector3[] Normals { get; init; }
    [Field(3)] public Vector2[]? TexCoords { get; init; }
    [Field(4)] public JointIndices[]? Joints { get; init; }
    [Field(5)] public Vector4[]? Weights { get; init; }
    /// <summary>Counter-clockwise triangles (strips and fans are turned into lists).</summary>
    [Field(6)] public required uint[] Indices { get; init; }
    /// <summary>An index into <see cref="ModelData.Materials"/>, or −1 for glTF's default material.</summary>
    [Field(7)] public int Material { get; init; } = -1;
    /// <summary>Vertices of flat faces given vertices of their own for skinning (Model), else null; not part of the record.</summary>
    internal uint[]? FlatVertices { get; init; }
}

[Record("model.mesh", 1)]
public sealed record ModelMesh([property: Field(1)] string Name, [property: Field(2)] IReadOnlyList<ModelPrimitive> Primitives);

/// <summary>
/// A material: its name, which names a palette slot (Docs/Renderer-3D.md, "Colours: palette slots"),
/// its base colour (linear, glTF's baseColorFactor) for when the palette has no such slot, and
/// whether back faces show. From the Blender shader (Docs/Renderer-3D.md, "Glow"): its emission
/// (linear, glTF's emissiveFactor times KHR_materials_emissive_strength), which makes it glow, and
/// whether it's unlit (a Background shader, KHR_materials_unlit). <see cref="Hatch"/> is false for
/// a material with the custom property <c>hatch = 0</c> (material extras).
/// </summary>
[Record("model.material", 1)]
public sealed record ModelMaterial([property: Field(1)] string Name, [property: Field(2)] Vector4 BaseColor, [property: Field(3)] bool DoubleSided)
{
    /// <summary>glTF's default material: white, one-sided.</summary>
    public static readonly ModelMaterial Default = new("", Vector4.One, false);

    public string Slot => Palette.SlotOf(Name);
    [Field(4)] public Vector3 Emission { get; init; }
    [Field(5)] public bool Unlit { get; init; }
    [Field(6)] public bool Hatch { get; init; } = true;

    /// <summary>An Emission shader alone: no base colour, only emission (drawn flat in it, glowing).</summary>
    public bool EmissionOnly => !Unlit && Emission != Vector3.Zero && BaseColor.X == 0 && BaseColor.Y == 0 && BaseColor.Z == 0;
}

/// <summary>
/// A node of the hierarchy: its local transform (as the file's matrix, or translation, rotation and
/// scale, which animations later drive), the mesh it shows and the skin that deforms it (−1: none).
/// <see cref="Ink"/> is false for parts drawn without ink lines (the Blender object's custom
/// property <c>ink = 0</c>, exported as node extras). Since version 2 (Docs/Scene.md, step 3, Q9) the
/// camera it carries and its light, the model's sun (indices into <see cref="ModelData.Cameras"/> and
/// <see cref="ModelData.Lights"/>; −1: none).
/// </summary>
[Record("model.node", 2)]
public sealed record ModelNode([property: Field(1)] string Name, [property: Field(2)] int Parent, [property: Field(3)] IReadOnlyList<int> Children,
    [property: Field(4)] Matrix4x4 Local, [property: Field(5)] Vector3 Translation, [property: Field(6)] Quaternion Rotation, [property: Field(7)] Vector3 Scale,
    [property: Field(8)] int Mesh, [property: Field(9)] int Skin)
{
    [Field(10)] public bool Ink { get; init; } = true;
    [Field(11)] public int Camera { get; init; } = -1;
    [Field(12)] public int Light { get; init; } = -1;

    /// <summary>Version 1 (M9): no camera or light.</summary>
    [Record("model.node", 1)]
    public sealed record V1([property: Field(1)] string Name, [property: Field(2)] int Parent, [property: Field(3)] IReadOnlyList<int> Children,
        [property: Field(4)] Matrix4x4 Local, [property: Field(5)] Vector3 Translation, [property: Field(6)] Quaternion Rotation, [property: Field(7)] Vector3 Scale,
        [property: Field(8)] int Mesh, [property: Field(9)] int Skin)
    {
        [Field(10)] public bool Ink { get; init; } = true;

        public ModelNode Upgrade() => new(Name, Parent, Children, Local, Translation, Rotation, Scale, Mesh, Skin) { Ink = Ink };
    }
}

/// <summary>
/// A camera (glTF's, Docs/Scene.md, step 3, S3-4), in the engine's terms (<see cref="Camera"/>): its
/// projection, the vertical field of view in radians (perspective: glTF's yfov, the exporter's vertical
/// angle for a camera with sensor fit Vertical, Q10) or the height of the world in view (orthographic:
/// twice glTF's ymag, which the stage export writes from the camera's own scale, S3-1), and the near
/// and far planes. glTF's aspect ratio and xmag aren't kept: the engine keeps the vertical extent and
/// widens with the view's aspect. A camera hangs on a node, which places it as glTF's cameras are
/// placed: looking down the node's −Z with +Y up.
/// </summary>
[Record("model.camera", 1)]
public sealed record ModelCamera
{
    [Field(1)] public required string Name { get; init; }
    [Field(2)] public required Projection Projection { get; init; }
    [Field(3)] public float VerticalFieldOfView { get; init; }
    [Field(4)] public float OrthographicHeight { get; init; }
    [Field(5)] public required float Near { get; init; }
    [Field(6)] public required float Far { get; init; }

    /// <summary>
    /// What makes no projection, or null: a perspective camera's field of view in (0, π) and its near
    /// plane beyond 0; an orthographic one's height above 0 and its near plane at 0 or beyond; the far
    /// plane finite and beyond the near one.
    /// </summary>
    public string? Problem()
    {
        var at = CultureInfo.InvariantCulture;
        if (Projection == Projection.Perspective)
        {
            if (!(VerticalFieldOfView > 0 && VerticalFieldOfView < MathF.PI))
                return string.Create(at, $"a vertical field of view of {VerticalFieldOfView} radians (more than 0 and less than π)");
            if (!(Near > 0))
                return string.Create(at, $"its near plane at {Near} (a perspective camera's is beyond 0)");
        }
        else
        {
            if (!(OrthographicHeight > 0))
                return string.Create(at, $"a height of {OrthographicHeight} in view (more than 0)");
            if (!(Near >= 0))
                return string.Create(at, $"its near plane at {Near} (at 0 or beyond)");
        }
        if (!(Far > Near) || !float.IsFinite(Far))
            return string.Create(at, $"its far plane at {Far}, not finite and beyond its near plane at {Near}");
        return null;
    }
}

/// <summary>
/// The model's sun (glTF's directional light, KHR_lights_punctual; Docs/Scene.md, step 3, Q12, S3-2 to
/// S3-4): its strength as Blender's own (the exporter's lighting units raw) and its angle across in
/// degrees (Blender's sun angle, which the stage export copies into the light's extras). It hangs on a
/// node, which shines it down its −Z. Only suns: other kinds of light are refused at import.
/// </summary>
[Record("model.light", 1)]
public sealed record ModelLight([property: Field(1)] string Name, [property: Field(2)] float Strength, [property: Field(3)] float Angle)
{
    /// <summary>What no sun can be, or null: its strength finite and not below 0, its angle from 0 to less than 180°.</summary>
    public string? Problem() =>
        !(Strength >= 0) || !float.IsFinite(Strength) ? string.Create(CultureInfo.InvariantCulture, $"a strength of {Strength} (finite, 0 or more)")
        : !(Angle >= 0 && Angle < 180) ? string.Create(CultureInfo.InvariantCulture, $"an angle of {Angle}° (0 or more, less than 180)")
        : null;
}

/// <summary>
/// A pose (Docs/Renderer-3D.md, "GPU skinning"): local translation, rotation and scale for some
/// nodes; the rest keep the file's. Animations fill these (Docs/Animation.md).
/// </summary>
public sealed class ModelPose
{
    readonly Dictionary<int, (Vector3 Translation, Quaternion Rotation, Vector3 Scale)> _nodes = [];

    public ModelPose Set(int node, Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        _nodes[node] = (translation, rotation, scale);
        return this;
    }

    public bool TryGet(int node, out (Vector3 Translation, Quaternion Rotation, Vector3 Scale) local) => _nodes.TryGetValue(node, out local);
}

/// <summary>A skin: the nodes acting as joints and their inverse bind matrices (model space to joint space).</summary>
[Record("model.skin", 1)]
public sealed record ModelSkin([property: Field(1)] string Name, [property: Field(2)] IReadOnlyList<int> Joints,
    [property: Field(3)] IReadOnlyList<Matrix4x4> InverseBindMatrices, [property: Field(4)] int Skeleton);

/// <summary>The node property a glTF animation channel drives (morph target weights are not read: the engine animates bones only).</summary>
public enum AnimationPath { Translation, Rotation, Scale }

/// <summary>How a glTF animation sampler interpolates between its keys (glTF 2.0, Appendix C).</summary>
public enum AnimationInterpolation { Step, Linear, CubicSpline }

/// <summary>
/// A glTF animation channel with its sampler, as the file gives them: the node, the property, the
/// keys' times in seconds from the animation's start, and their values (3 components per key for
/// translation and scale, 4 for rotation as x, y, z, w; with CUBICSPLINE each key holds its
/// in-tangent, value and out-tangent in that order).
/// </summary>
[Record("model.channel", 1)]
public sealed record ModelAnimationChannel([property: Field(1)] int Node, [property: Field(2)] AnimationPath Path,
    [property: Field(3)] AnimationInterpolation Interpolation, [property: Field(4)] float[] Times, [property: Field(5)] float[] Values)
{
    /// <summary>Components of the property: 4 for rotation, 3 otherwise.</summary>
    public int Components => Path == AnimationPath.Rotation ? 4 : 3;
}

/// <summary>A clip event: a Blender pose marker's name and its time in seconds from the clip's start (Docs/Animation.md, "Clip events").</summary>
[Record("model.event", 1)]
public sealed record ModelAnimationEvent([property: Field(1)] string Name, [property: Field(2)] double Time);

/// <summary>
/// A glTF animation: one clip (a Blender action, Docs/Animation.md), with the events its extras
/// list (the action's pose markers, which the export script copies there), in time order.
/// </summary>
[Record("model.animation", 1)]
public sealed record ModelAnimation([property: Field(1)] string Name, [property: Field(2)] IReadOnlyList<ModelAnimationChannel> Channels)
{
    [Field(3)] public IReadOnlyList<ModelAnimationEvent> Events { get; init; } = [];
}

/// <summary>
/// A model as read from a .glb (<see cref="GltfImport"/>): in glTF's coordinates, right-handed, Y up,
/// metres. Also the record a models.pak entry holds (<c>model</c>; <see cref="ModelImporter"/>). Since
/// version 2 (Docs/Scene.md, step 3, Q9) a stage's cameras and its sun, carried by nodes.
/// </summary>
[Record("model", 2)]
public sealed class ModelData
{
    [Field(1)] public required IReadOnlyList<ModelNode> Nodes { get; init; }
    [Field(2)] public required IReadOnlyList<ModelMesh> Meshes { get; init; }
    [Field(3)] public required IReadOnlyList<ModelMaterial> Materials { get; init; }
    [Field(4)] public required IReadOnlyList<ModelSkin> Skins { get; init; }
    /// <summary>The file's animations (Docs/Animation.md), in file order.</summary>
    [Field(5)] public IReadOnlyList<ModelAnimation> Animations { get; init; } = [];
    /// <summary>The scene's root nodes.</summary>
    [Field(6)] public required IReadOnlyList<int> Roots { get; init; }
    /// <summary>The file's cameras, in file order (<see cref="ModelNode.Camera"/> places them).</summary>
    [Field(7)] public IReadOnlyList<ModelCamera> Cameras { get; init; } = [];
    /// <summary>The file's light, the model's sun, or none (<see cref="ModelNode.Light"/> places it).</summary>
    [Field(8)] public IReadOnlyList<ModelLight> Lights { get; init; } = [];

    /// <summary>Version 1 (M9): no cameras or lights.</summary>
    [Record("model", 1)]
    public sealed class V1
    {
        [Field(1)] public required IReadOnlyList<ModelNode> Nodes { get; init; }
        [Field(2)] public required IReadOnlyList<ModelMesh> Meshes { get; init; }
        [Field(3)] public required IReadOnlyList<ModelMaterial> Materials { get; init; }
        [Field(4)] public required IReadOnlyList<ModelSkin> Skins { get; init; }
        [Field(5)] public IReadOnlyList<ModelAnimation> Animations { get; init; } = [];
        [Field(6)] public required IReadOnlyList<int> Roots { get; init; }

        public ModelData Upgrade() => new() { Nodes = Nodes, Meshes = Meshes, Materials = Materials, Skins = Skins, Animations = Animations, Roots = Roots };
    }

    /// <summary>Reads a .glb (or .gltf) file (<see cref="GltfImport"/>).</summary>
    public static ModelData Load(string path) => GltfImport.Load(path);

    /// <summary>
    /// The model at <paramref name="path"/> in the game's data (a models.pak entry such as
    /// <c>models/figure</c>, made from <c>models/figure.glb</c> by <see cref="ModelImporter"/>, or its
    /// JSON form in the development folder), checked (<see cref="Validate"/>; S5-1): a
    /// <see cref="DataFileException"/> naming the pack and entry or the loose file when it isn't a
    /// readable model record or breaks a rule.
    /// </summary>
    public static ModelData Load(VirtualFileSystem files, string path)
    {
        var model = files.Load<ModelData>(path);
        try
        {
            model.Validate();
        }
        catch (InvalidDataException problem)
        {
            throw files.Problem(path, $"not a valid model: {problem.Message}", problem);
        }
        return model;
    }

    /// <summary>
    /// Throws an <see cref="InvalidDataException"/> when the model breaks a rule a model from
    /// <see cref="GltfImport"/> keeps (Docs/Assets.md, S5-1): each node the child of at most one node,
    /// never its own, its parent the node that lists it; nodes' meshes and skins, primitives' materials,
    /// roots (nodes without a parent), skins' joints and skeletons and channels' nodes that exist; a
    /// primitive's attributes one per vertex, whole triangles, indices within its vertices, and joint
    /// indices within the skin of every node that shows it; a skin's inverse bind matrices for all its
    /// joints; a channel's values as many as its keys need, a cubic spline at least two keys, and key
    /// times that increase; nodes' cameras and lights that exist, one light at most and on one node at
    /// most (one sun per view), and cameras and lights whose values make a projection and a sun
    /// (<see cref="ModelCamera.Problem"/>, <see cref="ModelLight.Problem"/>; Docs/Scene.md, step 3). A model
    /// from a pack or a loose folder breaks them only if it was edited.
    /// </summary>
    public void Validate()
    {
        static void Fail(string problem) => throw new InvalidDataException(problem);
        var count = Nodes.Count;
        var parents = new int[count];
        Array.Fill(parents, -1);
        for (var i = 0; i < count; i++)
            foreach (var child in Nodes[i].Children)
            {
                if (child < 0 || child >= count)
                    Fail($"node {i} ({Nodes[i].Name}) has a child {child} that doesn't exist");
                if (child == i || parents[child] >= 0)
                    Fail($"node {child} ({Nodes[child].Name}) is its own child or the child of two nodes");
                parents[child] = i;
            }
        for (var i = 0; i < count; i++)
        {
            var node = Nodes[i];
            if (node.Parent != parents[i])
                Fail($"node {i} ({node.Name}) has the parent {node.Parent}, but {(parents[i] < 0 ? "no node" : $"node {parents[i]}")} lists it as a child");
            if (node.Mesh < -1 || node.Mesh >= Meshes.Count)
                Fail($"node {i} ({node.Name}) shows mesh {node.Mesh}, which doesn't exist");
            if (node.Skin < -1 || node.Skin >= Skins.Count)
                Fail($"node {i} ({node.Name}) has skin {node.Skin}, which doesn't exist");
            if (node.Camera < -1 || node.Camera >= Cameras.Count)
                Fail($"node {i} ({node.Name}) carries camera {node.Camera}, which doesn't exist");
            if (node.Light < -1 || node.Light >= Lights.Count)
                Fail($"node {i} ({node.Name}) carries light {node.Light}, which doesn't exist");
        }
        // One sun per view (Docs/Scene.md, Q12): a model has one light or none, on one node.
        if (Lights.Count > 1)
            Fail($"{Lights.Count} lights: a model has one sun or none");
        if (Enumerable.Range(0, count).Where(i => Nodes[i].Light >= 0).Take(2).ToArray() is [var first, var second])
            Fail($"nodes {first} ({Nodes[first].Name}) and {second} ({Nodes[second].Name}) both carry the sun: a model has one sun or none");
        for (var c = 0; c < Cameras.Count; c++)
            if (Cameras[c].Problem() is { } problem)
                Fail($"camera {c} ({Cameras[c].Name}) has {problem}");
        for (var l = 0; l < Lights.Count; l++)
            if (Lights[l].Problem() is { } problem)
                Fail($"light {l} ({Lights[l].Name}) has {problem}");
        foreach (var root in Roots)
            if (root < 0 || root >= count || Nodes[root].Parent >= 0)
                Fail($"the root {root} isn't a node without a parent");
        for (var m = 0; m < Meshes.Count; m++)
            for (var p = 0; p < Meshes[m].Primitives.Count; p++)
            {
                var primitive = Meshes[m].Primitives[p];
                var where = $"mesh {m} ({Meshes[m].Name}), primitive {p}";
                var vertices = primitive.Positions.Length;
                foreach (var (name, length) in new[] { ("normals", (int?)primitive.Normals.Length), ("texCoords", primitive.TexCoords?.Length),
                             ("joints", primitive.Joints?.Length), ("weights", primitive.Weights?.Length) })
                    if (length is { } l && l != vertices)
                        Fail($"{where} has {l} {name} for {vertices} vertices");
                if (primitive.Indices.Length % 3 != 0)
                    Fail($"{where} has {primitive.Indices.Length} indices, not whole triangles");
                foreach (var index in primitive.Indices)
                    if (index >= vertices)
                        Fail($"{where} has the index {index} beyond its {vertices} vertices");
                if (primitive.Material < -1 || primitive.Material >= Materials.Count)
                    Fail($"{where} has material {primitive.Material}, which doesn't exist");
            }
        for (var s = 0; s < Skins.Count; s++)
        {
            var skin = Skins[s];
            foreach (var joint in skin.Joints)
                if (joint < 0 || joint >= count)
                    Fail($"skin {s} ({skin.Name}) has the joint {joint}, which isn't a node");
            if (skin.InverseBindMatrices.Count < skin.Joints.Count)
                Fail($"skin {s} ({skin.Name}) has {skin.Joints.Count} joints but {skin.InverseBindMatrices.Count} inverse bind matrices");
            if (skin.Skeleton < -1 || skin.Skeleton >= count)
                Fail($"skin {s} ({skin.Name}) has the skeleton {skin.Skeleton}, which isn't a node");
        }
        // Joint indices index the skin of each node showing the mesh: the GPU reads its joint matrices by them.
        for (var i = 0; i < count; i++)
        {
            if (Nodes[i].Mesh < 0 || Nodes[i].Skin < 0)
                continue;
            var joints = Skins[Nodes[i].Skin].Joints.Count;
            var primitives = Meshes[Nodes[i].Mesh].Primitives;
            for (var p = 0; p < primitives.Count; p++)
                foreach (var j in primitives[p].Joints ?? [])
                    if (Math.Max(Math.Max(j.J0, j.J1), Math.Max(j.J2, j.J3)) is var highest && highest >= joints)
                        Fail($"node {i} ({Nodes[i].Name}) shows mesh {Nodes[i].Mesh}, whose primitive {p} has the joint index {highest} beyond its skin's {joints} joints");
        }
        for (var a = 0; a < Animations.Count; a++)
            for (var c = 0; c < Animations[a].Channels.Count; c++)
            {
                var channel = Animations[a].Channels[c];
                var where = $"animation {a} ({Animations[a].Name}), channel {c}";
                if (channel.Node < 0 || channel.Node >= count)
                    Fail($"{where} drives node {channel.Node}, which doesn't exist");
                var cubic = channel.Interpolation == AnimationInterpolation.CubicSpline;
                var perKey = channel.Components * (cubic ? 3 : 1);
                if (channel.Times.Length == 0 || channel.Values.Length != channel.Times.Length * perKey)
                    Fail($"{where} has {channel.Values.Length} values for {channel.Times.Length} keys");
                if (cubic && channel.Times.Length < 2)
                    Fail($"{where} is a cubic spline with fewer than 2 keys");
                for (var k = 1; k < channel.Times.Length; k++)
                    if (!(channel.Times[k] > channel.Times[k - 1]))
                        Fail($"{where} has key times that don't increase");
            }
    }

    public ModelMaterial MaterialOf(ModelPrimitive primitive) =>
        primitive.Material < 0 ? ModelMaterial.Default : Materials[primitive.Material];

    /// <summary>
    /// Every node's transform to model space (its local transform after its ancestors'), in
    /// <paramref name="pose"/> where it gives a node's local transform.
    /// </summary>
    public Matrix4x4[] WorldMatrices(ModelPose? pose = null)
    {
        var world = new Matrix4x4[Nodes.Count];
        var stack = new Stack<(int Node, Matrix4x4 Parent)>();
        foreach (var root in Roots)
            stack.Push((root, Matrix4x4.Identity));
        while (stack.Count > 0)
        {
            var (node, parent) = stack.Pop();
            var local = pose != null && pose.TryGet(node, out var posed)
                ? Matrix4x4.CreateScale(posed.Scale) * Matrix4x4.CreateFromQuaternion(posed.Rotation) * Matrix4x4.CreateTranslation(posed.Translation)
                : Nodes[node].Local;
            world[node] = local * parent;
            foreach (var child in Nodes[node].Children)
                stack.Push((child, world[node]));
        }
        return world;
    }
}
