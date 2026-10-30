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
}

public sealed record ModelMesh(string Name, IReadOnlyList<ModelPrimitive> Primitives);

/// <summary>
/// A material: its name, which names a palette slot (Docs/Renderer-3D.md, "Colours: palette slots"),
/// its base colour (linear, glTF's baseColorFactor) for when the palette has no such slot, and
/// whether back faces show.
/// </summary>
public sealed record ModelMaterial(string Name, Vector4 BaseColor, bool DoubleSided)
{
    /// <summary>glTF's default material: white, one-sided.</summary>
    public static readonly ModelMaterial Default = new("", Vector4.One, false);

    public string Slot => Palette.SlotOf(Name);
}

/// <summary>
/// A node of the hierarchy: its local transform (as the file's matrix, or translation, rotation and
/// scale, which animations later drive), the mesh it shows and the skin that deforms it (−1: none).
/// </summary>
public sealed record ModelNode(string Name, int Parent, IReadOnlyList<int> Children, Matrix4x4 Local,
    Vector3 Translation, Quaternion Rotation, Vector3 Scale, int Mesh, int Skin);

/// <summary>A skin: the nodes acting as joints and their inverse bind matrices (model space to joint space).</summary>
public sealed record ModelSkin(string Name, IReadOnlyList<int> Joints, IReadOnlyList<Matrix4x4> InverseBindMatrices, int Skeleton);

/// <summary>A model as read from a .glb (<see cref="GltfImport"/>): in glTF's coordinates, right-handed, Y up, metres.</summary>
public sealed class ModelData
{
    public required IReadOnlyList<ModelNode> Nodes { get; init; }
    public required IReadOnlyList<ModelMesh> Meshes { get; init; }
    public required IReadOnlyList<ModelMaterial> Materials { get; init; }
    public required IReadOnlyList<ModelSkin> Skins { get; init; }
    /// <summary>The scene's root nodes.</summary>
    public required IReadOnlyList<int> Roots { get; init; }

    public static ModelData Load(string path) => GltfImport.Load(path);

    public ModelMaterial MaterialOf(ModelPrimitive primitive) =>
        primitive.Material < 0 ? ModelMaterial.Default : Materials[primitive.Material];

    /// <summary>Every node's transform to model space (its local transform after its ancestors').</summary>
    public Matrix4x4[] WorldMatrices()
    {
        var world = new Matrix4x4[Nodes.Count];
        var stack = new Stack<(int Node, Matrix4x4 Parent)>();
        foreach (var root in Roots)
            stack.Push((root, Matrix4x4.Identity));
        while (stack.Count > 0)
        {
            var (node, parent) = stack.Pop();
            world[node] = Nodes[node].Local * parent;
            foreach (var child in Nodes[node].Children)
                stack.Push((child, world[node]));
        }
        return world;
    }
}
