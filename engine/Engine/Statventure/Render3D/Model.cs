using System.Numerics;
using Statventure.Graphics;

namespace Statventure.Render3D;

/// <summary>
/// A model on the GPU at rest: one <see cref="Mesh"/> per primitive, placed by the node hierarchy.
/// A skinned mesh is placed by its joints instead (glTF ignores its node's transform): its vertices
/// are skinned once, on the CPU, by the joints at rest. Posing on the GPU is step 7, animation M7
/// (Docs/Renderer-3D.md).
/// </summary>
public sealed class Model : IDisposable
{
    readonly Mesh[][] _meshes;
    /// <summary>Per node: its skinned mesh's primitives, already in model space; null when it has no skin.</summary>
    readonly Mesh[]?[] _skinned;
    readonly Matrix4x4[] _world;
    readonly bool[] _reached;

    static readonly Palette Empty = new();

    public ModelData Data { get; }

    Model(GpuDevice device, ModelData data)
    {
        Data = data;
        _world = data.WorldMatrices();
        _reached = new bool[data.Nodes.Count];
        var stack = new Stack<int>(data.Roots);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            _reached[node] = true;
            foreach (var child in data.Nodes[node].Children)
                stack.Push(child);
        }
        _meshes = data.Meshes.Select(mesh => mesh.Primitives.Select(p => Upload(device, p, p.Positions, p.Normals)).ToArray()).ToArray();
        _skinned = new Mesh[]?[data.Nodes.Count];
        for (var node = 0; node < data.Nodes.Count; node++)
            if (data.Nodes[node] is { Mesh: >= 0, Skin: >= 0 } n && _reached[node])
                _skinned[node] = data.Meshes[n.Mesh].Primitives.Select(p => Skin(device, p, data.Skins[n.Skin])).ToArray();
    }

    public static Model Create(GpuDevice device, ModelData data) => new(device, data);

    static Mesh Upload(GpuDevice device, ModelPrimitive p, Vector3[] positions, Vector3[] normals)
    {
        var vertices = new Vertex3D[positions.Length];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = new Vertex3D(positions[i], normals[i], p.TexCoords?[i] ?? default);
        return Mesh.Create(device, vertices, p.Indices);
    }

    /// <summary>
    /// glTF skinning: each vertex goes through its joints' matrices (inverse bind matrix, then the
    /// joint's transform) mixed by its weights; normals through their inverse transposes.
    /// </summary>
    Mesh Skin(GpuDevice device, ModelPrimitive p, ModelSkin skin)
    {
        if (p.Joints is not { } joints || p.Weights is not { } weights)
            return Upload(device, p, p.Positions, p.Normals);
        var matrices = new Matrix4x4[skin.Joints.Count];
        var normalMatrices = new Matrix4x4[skin.Joints.Count];
        for (var j = 0; j < matrices.Length; j++)
        {
            matrices[j] = skin.InverseBindMatrices[j] * _world[skin.Joints[j]];
            normalMatrices[j] = Matrix4x4.Invert(matrices[j], out var inverse) ? Matrix4x4.Transpose(inverse) : matrices[j];
        }
        var positions = new Vector3[p.Positions.Length];
        var normals = new Vector3[p.Normals.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            var (indices, w) = (joints[i], weights[i]);
            Matrix4x4 m = matrices[indices.J0] * w.X + matrices[indices.J1] * w.Y + matrices[indices.J2] * w.Z + matrices[indices.J3] * w.W;
            Matrix4x4 n = normalMatrices[indices.J0] * w.X + normalMatrices[indices.J1] * w.Y + normalMatrices[indices.J2] * w.Z + normalMatrices[indices.J3] * w.W;
            positions[i] = Vector3.Transform(p.Positions[i], m);
            var normal = Vector3.TransformNormal(p.Normals[i], n);
            normals[i] = normal.LengthSquared() > 0 ? Vector3.Normalize(normal) : p.Normals[i];
        }
        return Upload(device, p, positions, normals);
    }

    public static Model Load(GpuDevice device, string path) => Create(device, ModelData.Load(path));

    /// <summary>
    /// Adds a cel-shaded draw item per primitive of every node in the scene, placed by
    /// <paramref name="transform"/> (model to world), in the tones <paramref name="palette"/> and
    /// <paramref name="style"/> give its materials. The items make one model for the ink lines (its
    /// outline goes around all of them); each node is a part, with lines unless its <c>ink</c> is 0.
    /// Every model casts shadows; <paramref name="scenery"/> (the stage, terrain and its features)
    /// also receives them (Docs/Renderer-3D.md, "Shadows").
    /// </summary>
    public void Draw(List<DrawItem> items, Matrix4x4 transform, Palette? palette = null, CelStyle? style = null, bool scenery = false)
    {
        palette ??= Empty;
        style ??= CelStyle.Default;
        var group = new object();
        for (var node = 0; node < Data.Nodes.Count; node++)
        {
            var meshIndex = Data.Nodes[node].Mesh;
            if (meshIndex < 0 || !_reached[node])
                continue;
            var primitives = Data.Meshes[meshIndex].Primitives;
            var skinned = _skinned[node];
            for (var p = 0; p < primitives.Count; p++)
            {
                var material = Data.MaterialOf(primitives[p]);
                var (lit, shadow) = palette.Resolve(material, style);
                var item = skinned != null
                    ? new DrawItem(skinned[p], transform, lit, material.DoubleSided)
                    : new DrawItem(_meshes[meshIndex][p], _world[node] * transform, lit, material.DoubleSided);
                item = item with
                {
                    Shading = Shading.Cel, Shadow = shadow, Group = group, Part = node + 1, Ink = Data.Nodes[node].Ink,
                    CastsShadow = true, ReceivesShadows = scenery, Hatch = material.Hatch, Anchor = transform.Translation,
                    Glow = material.Emission,
                };
                // Unlit (a Background shader): flat in its colour. An Emission shader alone: flat in
                // its emission, glowing. Neither is hatched (Docs/Renderer-3D.md, "Glow").
                if (material.Unlit)
                    item = item with { Shading = Shading.Flat, Hatch = false, Glow = Vector3.Zero };
                else if (material.EmissionOnly)
                    item = item with { Shading = Shading.Flat, Color = new Vector4(material.Emission, 1), Hatch = false };
                items.Add(item);
            }
        }
    }

    public void Dispose()
    {
        foreach (var mesh in _meshes.SelectMany(m => m).Concat(_skinned.SelectMany(m => m ?? [])))
            mesh.Dispose();
    }
}
