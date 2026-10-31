using System.Numerics;

namespace Statventure.Render3D;

/// <summary>The four joints a skinned vertex follows (glTF's JOINTS_0), as indices into its skin's joints.</summary>
public readonly record struct JointIndices(ushort J0, ushort J1, ushort J2, ushort J3);

/// <summary>Triangles with one material: vertex attributes as glTF gives them, 32-bit indices.</summary>
public sealed class ModelPrimitive
{
    public required Vector3[] Positions { get; init; }
    /// <summary>The file's normals, or flat face normals when it has none (as glTF asks).</summary>
    public required Vector3[] Normals { get; init; }
    public Vector2[]? TexCoords { get; init; }
    public JointIndices[]? Joints { get; init; }
    public Vector4[]? Weights { get; init; }
    /// <summary>Counter-clockwise triangles (strips and fans are turned into lists).</summary>
    public required uint[] Indices { get; init; }
    /// <summary>An index into <see cref="ModelData.Materials"/>, or −1 for glTF's default material.</summary>
    public int Material { get; init; } = -1;
    /// <summary>Vertices of flat faces given vertices of their own for skinning (Model), else null.</summary>
    internal uint[]? FlatVertices { get; init; }
}

public sealed record ModelMesh(string Name, IReadOnlyList<ModelPrimitive> Primitives);

/// <summary>
/// A material: its name, which names a palette slot (Docs/Renderer-3D.md, "Colours: palette slots"),
/// its base colour (linear, glTF's baseColorFactor) for when the palette has no such slot, and
/// whether back faces show. From the Blender shader (Docs/Renderer-3D.md, "Glow"): its emission
/// (linear, glTF's emissiveFactor times KHR_materials_emissive_strength), which makes it glow, and
/// whether it's unlit (a Background shader, KHR_materials_unlit). <see cref="Hatch"/> is false for
/// a material with the custom property <c>hatch = 0</c> (material extras).
/// </summary>
public sealed record ModelMaterial(string Name, Vector4 BaseColor, bool DoubleSided)
{
    /// <summary>glTF's default material: white, one-sided.</summary>
    public static readonly ModelMaterial Default = new("", Vector4.One, false);

    public string Slot => Palette.SlotOf(Name);
    public Vector3 Emission { get; init; }
    public bool Unlit { get; init; }
    public bool Hatch { get; init; } = true;

    /// <summary>An Emission shader alone: no base colour, only emission (drawn flat in it, glowing).</summary>
    public bool EmissionOnly => !Unlit && Emission != Vector3.Zero && BaseColor.X == 0 && BaseColor.Y == 0 && BaseColor.Z == 0;
}

/// <summary>
/// A node of the hierarchy: its local transform (as the file's matrix, or translation, rotation and
/// scale, which animations later drive), the mesh it shows and the skin that deforms it (−1: none).
/// <see cref="Ink"/> is false for parts drawn without ink lines (the Blender object's custom
/// property <c>ink = 0</c>, exported as node extras).
/// </summary>
public sealed record ModelNode(string Name, int Parent, IReadOnlyList<int> Children, Matrix4x4 Local,
    Vector3 Translation, Quaternion Rotation, Vector3 Scale, int Mesh, int Skin)
{
    public bool Ink { get; init; } = true;
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
public sealed record ModelSkin(string Name, IReadOnlyList<int> Joints, IReadOnlyList<Matrix4x4> InverseBindMatrices, int Skeleton);

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
public sealed record ModelAnimationChannel(int Node, AnimationPath Path, AnimationInterpolation Interpolation, float[] Times, float[] Values)
{
    /// <summary>Components of the property: 4 for rotation, 3 otherwise.</summary>
    public int Components => Path == AnimationPath.Rotation ? 4 : 3;
}

/// <summary>A clip event: a Blender pose marker's name and its time in seconds from the clip's start (Docs/Animation.md, "Clip events").</summary>
public sealed record ModelAnimationEvent(string Name, double Time);

/// <summary>
/// A glTF animation: one clip (a Blender action, Docs/Animation.md), with the events its extras
/// list (the action's pose markers, which the export script copies there), in time order.
/// </summary>
public sealed record ModelAnimation(string Name, IReadOnlyList<ModelAnimationChannel> Channels)
{
    public IReadOnlyList<ModelAnimationEvent> Events { get; init; } = [];
}

/// <summary>A model as read from a .glb (<see cref="GltfImport"/>): in glTF's coordinates, right-handed, Y up, metres.</summary>
public sealed class ModelData
{
    public required IReadOnlyList<ModelNode> Nodes { get; init; }
    public required IReadOnlyList<ModelMesh> Meshes { get; init; }
    public required IReadOnlyList<ModelMaterial> Materials { get; init; }
    public required IReadOnlyList<ModelSkin> Skins { get; init; }
    /// <summary>The file's animations (Docs/Animation.md), in file order.</summary>
    public IReadOnlyList<ModelAnimation> Animations { get; init; } = [];
    /// <summary>The scene's root nodes.</summary>
    public required IReadOnlyList<int> Roots { get; init; }

    public static ModelData Load(string path) => GltfImport.Load(path);

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
