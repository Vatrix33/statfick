using System.Numerics;
using System.Runtime.InteropServices;
using Brutal.VulkanApi;
using Statventure.Graphics;

namespace Statventure.Render3D;

/// <summary>The four joints and weights of a skinned vertex as skin.comp reads them (24 bytes).</summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
struct SkinVertex
{
    public uint Joints01;
    public uint Joints23;
    public Vector4 Weights;
}

/// <summary>
/// A primitive's feature edges (Docs/Renderer-3D.md, "Ink lines"): its creases and open edges as
/// feature.vert reads them (vertex indices, and the cosine between the two faces' normals, −2 for an
/// open or non-manifold edge), found at rest with the mesh's primitives welded by position.
/// </summary>
public sealed class FeatureEdges(GpuBuffer buffer, int count)
{
    internal GpuBuffer Buffer { get; } = buffer;
    public int Count { get; } = count;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
struct FeatureEdge
{
    public uint A;
    public uint B;
    public float Cosine;
}

/// <summary>
/// One skinned primitive to pose this frame (Docs/Renderer-3D.md, "GPU skinning"): its rest
/// vertices, joints and weights and smoothing fans on the GPU, the joint matrices of the pose, and
/// the posed bounds (model space) for fitting shadows. The renderer runs skin.comp and normals.comp
/// into its own buffer before drawing.
/// </summary>
public sealed class SkinJob
{
    internal SkinJob(Mesh rest, GpuBuffer influences, GpuBuffer fans, Matrix4x4[] joints, Vector3 boundsMin, Vector3 boundsMax)
    {
        (Rest, Influences, Fans, Joints, BoundsMin, BoundsMax) = (rest, influences, fans, joints, boundsMin, boundsMax);
    }

    internal Mesh Rest { get; }
    internal GpuBuffer Influences { get; }
    internal GpuBuffer Fans { get; }
    internal Matrix4x4[] Joints { get; }
    public Vector3 BoundsMin { get; }
    public Vector3 BoundsMax { get; }
}

/// <summary>
/// A model on the GPU: one <see cref="Mesh"/> per primitive, placed by the node hierarchy in a pose
/// (the file's rest pose by default). A skinned mesh is placed by its joints instead (glTF ignores
/// its node's transform): the renderer skins it on the GPU each frame (Docs/Renderer-3D.md, "GPU
/// skinning"); animation comes in M7.
/// </summary>
public sealed class Model : IDisposable
{
    /// <summary>A skinned primitive's GPU data: its joints and weights, and its smoothing fans (count + 1 offsets, then triangle · 4 + corner entries).</summary>
    sealed record SkinData(GpuBuffer Influences, GpuBuffer Fans);

    readonly Mesh[][] _meshes;
    readonly FeatureEdges?[][] _features;
    /// <summary>Per mesh and primitive: its skinning data, or null when it has no joints.</summary>
    readonly SkinData?[][] _skins;
    readonly Matrix4x4[] _rest;
    readonly bool[] _reached;

    static readonly Palette Empty = new();

    public ModelData Data { get; }

    Model(GpuDevice device, ModelData data)
    {
        Data = data;
        _rest = data.WorldMatrices();
        _reached = new bool[data.Nodes.Count];
        var stack = new Stack<int>(data.Roots);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            _reached[node] = true;
            foreach (var child in data.Nodes[node].Children)
                stack.Push(child);
        }
        // Skinned primitives draw with their flat faces split off (FlatFacesApart); everything else as read.
        var drawn = data.Meshes.Select(mesh => new ModelMesh(mesh.Name, mesh.Primitives.Select(p => p.Joints != null ? FlatFacesApart(p) : p).ToArray())).ToArray();
        _meshes = drawn.Select(mesh => mesh.Primitives.Select(p => Upload(device, p)).ToArray()).ToArray();
        _skins = drawn.Select(mesh => mesh.Primitives.Select(p => SkinOf(device, p)).ToArray()).ToArray();
        _features = drawn.Select(mesh => FeaturesOf(device, mesh)).ToArray();
    }

    /// <summary>
    /// Blender's importer shades a face flat when all its normals are the face's own; posed, such a
    /// face keeps its own normal while smooth faces average their fans (measured). So a skinned
    /// primitive's flat triangles get vertices of their own, and normals.comp gives each the normal of
    /// its one triangle; the smooth triangles keep sharing theirs.
    /// </summary>
    static ModelPrimitive FlatFacesApart(ModelPrimitive p)
    {
        var flat = new bool[p.Indices.Length / 3];
        for (var t = 0; t < flat.Length; t++)
        {
            Vector3 a = p.Positions[p.Indices[t * 3]], b = p.Positions[p.Indices[t * 3 + 1]], c = p.Positions[p.Indices[t * 3 + 2]];
            var face = Vector3.Cross(b - a, c - a);
            if (face.LengthSquared() == 0)
                continue;
            face = Vector3.Normalize(face);
            flat[t] = Enumerable.Range(0, 3).All(k => Vector3.Dot(p.Normals[p.Indices[t * 3 + k]], face) > 0.9999f);
        }
        if (!flat.Any(f => f))
            return p;
        var order = new List<uint>();
        var indices = new uint[p.Indices.Length];
        var kept = new Dictionary<uint, uint>();
        var flatVertices = new List<uint>();
        for (var c = 0; c < indices.Length; c++)
        {
            var source = p.Indices[c];
            if (flat[c / 3])
            {
                indices[c] = (uint)order.Count;
                flatVertices.Add((uint)order.Count);
                order.Add(source);
            }
            else
            {
                if (!kept.TryGetValue(source, out var index))
                {
                    kept[source] = index = (uint)order.Count;
                    order.Add(source);
                }
                indices[c] = index;
            }
        }
        T[] Take<T>(T[] values) => order.Select(i => values[i]).ToArray();
        return new ModelPrimitive
        {
            Positions = Take(p.Positions),
            Normals = Take(p.Normals),
            TexCoords = p.TexCoords == null ? null : Take(p.TexCoords),
            Joints = p.Joints == null ? null : Take(p.Joints),
            Weights = p.Weights == null ? null : Take(p.Weights),
            Indices = indices,
            Material = p.Material,
            FlatVertices = flatVertices.ToArray(),
        };
    }

    /// <summary>
    /// A mesh's feature edges, as Freestyle finds them on a mesh: its primitives welded by position
    /// (glTF splits a mesh by material and at seams), every edge with one face (open) or more than two
    /// kept, and every edge between two faces with the cosine of their normals' angle (the crease
    /// test is the style's, at draw). Each edge goes with the primitive of its first face.
    /// </summary>
    static FeatureEdges?[] FeaturesOf(GpuDevice device, ModelMesh mesh)
    {
        var welded = new Dictionary<Vector3, int>();
        var edges = new Dictionary<(int, int), List<(int Primitive, uint A, uint B, Vector3 Normal)>>();
        for (var p = 0; p < mesh.Primitives.Count; p++)
        {
            var primitive = mesh.Primitives[p];
            int Weld(uint index) => welded.TryGetValue(primitive.Positions[index], out var w) ? w : welded[primitive.Positions[index]] = welded.Count;
            for (var t = 0; t + 2 < primitive.Indices.Length; t += 3)
            {
                uint i0 = primitive.Indices[t], i1 = primitive.Indices[t + 1], i2 = primitive.Indices[t + 2];
                var normal = Vector3.Cross(primitive.Positions[i1] - primitive.Positions[i0], primitive.Positions[i2] - primitive.Positions[i0]);
                if (normal.LengthSquared() == 0)
                    continue;
                normal = Vector3.Normalize(normal);
                foreach (var (a, b) in new[] { (i0, i1), (i1, i2), (i2, i0) })
                {
                    int wa = Weld(a), wb = Weld(b);
                    var key = wa < wb ? (wa, wb) : (wb, wa);
                    if (!edges.TryGetValue(key, out var faces))
                        edges[key] = faces = [];
                    faces.Add((p, a, b, normal));
                }
            }
        }
        var lists = Enumerable.Range(0, mesh.Primitives.Count).Select(_ => new List<FeatureEdge>()).ToArray();
        foreach (var faces in edges.Values)
        {
            var first = faces[0];
            var cosine = faces.Count == 2 ? Vector3.Dot(faces[0].Normal, faces[1].Normal) : -2f;
            lists[first.Primitive].Add(new FeatureEdge { A = first.A, B = first.B, Cosine = cosine });
        }
        return lists.Select(list => list.Count == 0 ? null
            : new FeatureEdges(GpuBuffer.CreateWithData<FeatureEdge>(device, list.ToArray(), VkBufferUsageFlags.StorageBufferBit), list.Count)).ToArray();
    }

    public static Model Create(GpuDevice device, ModelData data) => new(device, data);

    static Mesh Upload(GpuDevice device, ModelPrimitive p)
    {
        var vertices = new Vertex3D[p.Positions.Length];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = new Vertex3D(p.Positions[i], p.Normals[i], p.TexCoords?[i] ?? default);
        return Mesh.Create(device, vertices, p.Indices);
    }

    /// <summary>
    /// A primitive's joints and weights, and its smoothing fans: for each vertex, the triangle
    /// corners at vertices with its rest position and normal (a vertex split at a UV seam shares its
    /// fan; at a sharp edge the normals differ, so the fans stay apart), as normals.comp sums them.
    /// </summary>
    static SkinData? SkinOf(GpuDevice device, ModelPrimitive p)
    {
        if (p.Joints is not { } joints || p.Weights is not { } weights)
            return null;
        var influences = new SkinVertex[p.Positions.Length];
        for (var i = 0; i < influences.Length; i++)
            influences[i] = new SkinVertex
            {
                Joints01 = joints[i].J0 | (uint)joints[i].J1 << 16,
                Joints23 = joints[i].J2 | (uint)joints[i].J3 << 16,
                Weights = weights[i],
            };
        // A flat face's vertices are fans of their own (FlatFacesApart); smooth ones share by rest position and normal.
        var flatVertices = new HashSet<uint>(p.FlatVertices ?? []);
        var keys = new Dictionary<(Vector3, Vector3), int>();
        var fanOf = new int[p.Positions.Length];
        var fanCount = 0;
        for (var i = 0; i < fanOf.Length; i++)
            if (flatVertices.Contains((uint)i))
                fanOf[i] = fanCount++;
            else if (!keys.TryGetValue((p.Positions[i], p.Normals[i]), out fanOf[i]))
                keys[(p.Positions[i], p.Normals[i])] = fanOf[i] = fanCount++;
        var corners = new List<uint>[fanCount];
        for (var f = 0; f < corners.Length; f++)
            corners[f] = [];
        for (var c = 0; c < p.Indices.Length; c++)
            corners[fanOf[p.Indices[c]]].Add((uint)(c / 3 * 4 + c % 3));
        var fans = new List<uint>(p.Positions.Length + 1);
        var entries = new List<uint>();
        for (var i = 0; i < p.Positions.Length; i++)
        {
            fans.Add((uint)entries.Count);
            entries.AddRange(corners[fanOf[i]]);
        }
        fans.Add((uint)entries.Count);
        fans.AddRange(entries);
        return new SkinData(
            GpuBuffer.CreateWithData<SkinVertex>(device, influences, VkBufferUsageFlags.StorageBufferBit),
            GpuBuffer.CreateWithData<uint>(device, fans.ToArray(), VkBufferUsageFlags.StorageBufferBit));
    }

    public static Model Load(GpuDevice device, string path) => Create(device, ModelData.Load(path));

    /// <summary>
    /// Adds a cel-shaded draw item per primitive of every node in the scene, placed by
    /// <paramref name="transform"/> (model to world), in the tones <paramref name="palette"/> and
    /// <paramref name="style"/> give its materials. The items make one model for the ink lines (its
    /// outline goes around all of them); each node is a part, with lines unless its <c>ink</c> is 0.
    /// Every model casts shadows; <paramref name="scenery"/> (the stage, terrain and its features)
    /// also receives them (Docs/Renderer-3D.md, "Shadows"). <paramref name="pose"/> moves nodes and
    /// joints; skinned meshes are posed on the GPU when drawn.
    /// </summary>
    public void Draw(List<DrawItem> items, Matrix4x4 transform, Palette? palette = null, CelStyle? style = null, bool scenery = false, ModelPose? pose = null)
    {
        palette ??= Empty;
        style ??= CelStyle.Default;
        var world = pose == null ? _rest : Data.WorldMatrices(pose);
        var group = new object();
        for (var node = 0; node < Data.Nodes.Count; node++)
        {
            var meshIndex = Data.Nodes[node].Mesh;
            if (meshIndex < 0 || !_reached[node])
                continue;
            var primitives = Data.Meshes[meshIndex].Primitives;
            // glTF skinning: each joint's matrix is its inverse bind matrix, then its transform in the pose.
            var skin = Data.Nodes[node].Skin >= 0 ? Data.Skins[Data.Nodes[node].Skin] : null;
            var joints = skin?.Joints.Select((joint, j) => skin.InverseBindMatrices[j] * world[joint]).ToArray();
            for (var p = 0; p < primitives.Count; p++)
            {
                var material = Data.MaterialOf(primitives[p]);
                var (lit, shadow) = palette.Resolve(material, style);
                var mesh = _meshes[meshIndex][p];
                var item = joints != null && _skins[meshIndex][p] is { } data
                    ? new DrawItem(mesh, transform, lit, material.DoubleSided) { Skin = Job(mesh, data, joints) }
                    : new DrawItem(mesh, world[node] * transform, lit, material.DoubleSided);
                item = item with
                {
                    Shading = Shading.Cel, Shadow = shadow, Group = group, Part = node + 1, Ink = Data.Nodes[node].Ink,
                    CastsShadow = true, ReceivesShadows = scenery, Hatch = material.Hatch, Anchor = transform.Translation,
                    Glow = material.Emission, Features = _features[meshIndex][p],
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

    /// <summary>A skinning job: the posed bounds are the rest bounds' corners through every joint's matrix.</summary>
    static SkinJob Job(Mesh mesh, SkinData data, Matrix4x4[] joints)
    {
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var joint in joints)
            for (var corner = 0; corner < 8; corner++)
            {
                var point = Vector3.Transform(new Vector3((corner & 1) == 0 ? mesh.BoundsMin.X : mesh.BoundsMax.X,
                    (corner & 2) == 0 ? mesh.BoundsMin.Y : mesh.BoundsMax.Y, (corner & 4) == 0 ? mesh.BoundsMin.Z : mesh.BoundsMax.Z), joint);
                (min, max) = (Vector3.Min(min, point), Vector3.Max(max, point));
            }
        return new SkinJob(mesh, data.Influences, data.Fans, joints, min, max);
    }

    public void Dispose()
    {
        foreach (var mesh in _meshes.SelectMany(m => m))
            mesh.Dispose();
        foreach (var features in _features.SelectMany(f => f))
            features?.Buffer.Dispose();
        foreach (var data in _skins.SelectMany(s => s))
        {
            data?.Influences.Dispose();
            data?.Fans.Dispose();
        }
    }
}
