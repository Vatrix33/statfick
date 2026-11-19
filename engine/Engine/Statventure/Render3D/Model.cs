using System.Numerics;
using System.Runtime.InteropServices;
using Brutal.VulkanApi;
using Statventure.Graphics;
using Statventure.Packs;

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
/// A primitive's edges for the ink (Docs/Renderer-3D.md, "Ink lines"), every one, as feature.vert reads them to find
/// the creases, open edges and silhouettes (vertex indices, the vertex opposite the edge in each of its two faces, the
/// cosine between the faces' normals, −2 for an open or non-manifold edge, and the island it lies on),
/// found at rest with the mesh's primitives welded by position.
/// </summary>
public sealed class FeatureEdges(GpuBuffer buffer, int count)
{
    internal GpuBuffer Buffer { get; } = buffer;
    public int Count { get; } = count;
}

/// <summary>
/// A primitive's islands (Docs/Renderer-3D.md, "Ink lines", "Feature edges"): each vertex's piece of its mesh, the
/// mesh's primitives welded by position and joined by their triangles, numbered from 0 in the order first met. The ink
/// tells an edge's own piece from another surface by it: a piece is one connected surface, so a model joined from many
/// pieces (GM2-P19) still has them apart. <see cref="Mesh"/> is shared by the primitives of one mesh.
/// </summary>
public sealed class SurfaceIslands(GpuBuffer buffer, object mesh)
{
    internal GpuBuffer Buffer { get; } = buffer;
    internal object Mesh { get; } = mesh;
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
struct FeatureEdge
{
    public uint A;
    public uint B;
    /// <summary>The vertex opposite the edge in its first face, and in its second (<see cref="uint.MaxValue"/>: none, an open edge or one whose second face lies in another primitive with no vertex there).</summary>
    public uint C;
    public uint D;
    public float Cosine;
    public uint Island;
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
    readonly SurfaceIslands[][] _islands;
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
        var surfaces = drawn.Select(mesh => FeaturesOf(device, mesh)).ToArray();
        _features = surfaces.Select(s => s.Features).ToArray();
        _islands = surfaces.Select(s => s.Islands).ToArray();
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
    /// test is the style's, at draw). Each edge goes with the primitive of its first face, with the vertex
    /// opposite it in each of its faces (by which the ink finds its silhouettes) and its island. And the mesh's
    /// islands (<see cref="SurfaceIslands"/>): the welded vertices joined by their triangles.
    /// </summary>
    static (FeatureEdges?[] Features, SurfaceIslands[] Islands) FeaturesOf(GpuDevice device, ModelMesh mesh)
    {
        var welded = new Dictionary<Vector3, int>();
        var weldOf = mesh.Primitives.Select(primitive => primitive.Positions.Select(position =>
            welded.TryGetValue(position, out var w) ? w : welded[position] = welded.Count).ToArray()).ToArray();
        // The islands: welded vertices joined by every triangle (union by the smaller id, so roots are the first met).
        var root = Enumerable.Range(0, welded.Count).ToArray();
        int Find(int w)
        {
            while (root[w] != w)
                w = root[w] = root[root[w]];
            return w;
        }
        for (var p = 0; p < mesh.Primitives.Count; p++)
            for (var t = 0; t + 2 < mesh.Primitives[p].Indices.Length; t += 3)
                for (var k = 1; k < 3; k++)
                {
                    int x = Find(weldOf[p][mesh.Primitives[p].Indices[t]]), y = Find(weldOf[p][mesh.Primitives[p].Indices[t + k]]);
                    if (x != y)
                        root[Math.Max(x, y)] = Math.Min(x, y);
                }
        var islandOf = new Dictionary<int, uint>();
        uint Island(int w) => islandOf.TryGetValue(Find(w), out var i) ? i : islandOf[Find(w)] = (uint)islandOf.Count;
        var islands = weldOf.Select(weld => weld.Select(Island).ToArray()).ToArray();
        // Each primitive's vertex at a welded position (the first), for a face's opposite vertex lying in another primitive.
        var local = weldOf.Select(weld =>
        {
            var map = new Dictionary<int, uint>();
            for (var i = 0; i < weld.Length; i++)
                map.TryAdd(weld[i], (uint)i);
            return map;
        }).ToArray();
        var edges = new Dictionary<(int, int), List<(int Primitive, uint A, uint B, uint Opposite, Vector3 Normal)>>();
        for (var p = 0; p < mesh.Primitives.Count; p++)
        {
            var primitive = mesh.Primitives[p];
            for (var t = 0; t + 2 < primitive.Indices.Length; t += 3)
            {
                uint i0 = primitive.Indices[t], i1 = primitive.Indices[t + 1], i2 = primitive.Indices[t + 2];
                var normal = Vector3.Cross(primitive.Positions[i1] - primitive.Positions[i0], primitive.Positions[i2] - primitive.Positions[i0]);
                if (normal.LengthSquared() == 0)
                    continue;
                normal = Vector3.Normalize(normal);
                foreach (var (a, b, c) in new[] { (i0, i1, i2), (i1, i2, i0), (i2, i0, i1) })
                {
                    int wa = weldOf[p][a], wb = weldOf[p][b];
                    var key = wa < wb ? (wa, wb) : (wb, wa);
                    if (!edges.TryGetValue(key, out var faces))
                        edges[key] = faces = [];
                    faces.Add((p, a, b, c, normal));
                }
            }
        }
        var lists = Enumerable.Range(0, mesh.Primitives.Count).Select(_ => new List<FeatureEdge>()).ToArray();
        foreach (var faces in edges.Values)
        {
            var first = faces[0];
            var cosine = faces.Count == 2 ? Vector3.Dot(faces[0].Normal, faces[1].Normal) : -2f;
            var d = uint.MaxValue;
            if (faces.Count >= 2)
                d = faces[1].Primitive == first.Primitive ? faces[1].Opposite
                    : local[first.Primitive].GetValueOrDefault(weldOf[faces[1].Primitive][faces[1].Opposite], uint.MaxValue);
            lists[first.Primitive].Add(new FeatureEdge { A = first.A, B = first.B, C = first.Opposite, D = d, Cosine = cosine, Island = islands[first.Primitive][first.A] });
        }
        var meshKey = new object();
        return (lists.Select(list => list.Count == 0 ? null
                : new FeatureEdges(GpuBuffer.CreateWithData<FeatureEdge>(device, list.ToArray(), VkBufferUsageFlags.StorageBufferBit), list.Count)).ToArray(),
            islands.Select(ids => new SurfaceIslands(GpuBuffer.CreateWithData<uint>(device, ids, VkBufferUsageFlags.StorageBufferBit), meshKey)).ToArray());
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

    /// <summary>A model from a .glb (or .gltf) file (<see cref="GltfImport"/>).</summary>
    public static Model Load(GpuDevice device, string path) => Create(device, ModelData.Load(path));

    /// <summary>A model from the game's data: the model record at <paramref name="path"/> (<see cref="ModelData.Load(VirtualFileSystem, string)"/>).</summary>
    public static Model Load(GpuDevice device, VirtualFileSystem files, string path) => Create(device, ModelData.Load(files, path));

    /// <summary>
    /// Adds a cel-shaded draw item per primitive of every node in the scene, placed by
    /// <paramref name="transform"/> (model to world), in the tones <paramref name="palette"/> and
    /// <paramref name="style"/> give its materials. The items make one model for the ink lines (its
    /// outline goes around all of them); each node is a part, with lines unless its <c>ink</c> is 0.
    /// Every model casts shadows; <paramref name="scenery"/> (the stage, terrain and its features)
    /// also receives them (Docs/Renderer-3D.md, "Shadows"). <paramref name="pose"/> moves nodes and
    /// joints; skinned meshes are posed on the GPU when drawn. <paramref name="world"/>, when given,
    /// is every node's world matrix instead (a garment deformed by its wearer's bones,
    /// Docs/Animation.md). <paramref name="owner"/> is the items' group, which a 3D view's picking
    /// gives back (a new object when none is given): gear drawn with its wearer's owner is one model
    /// with it (one outline, one glow mask). The hatching is anchored at <paramref name="anchor"/> on
    /// screen (world space; the transform's origin when none is given): gear takes its wearer's.
    /// </summary>
    public void Draw(List<DrawItem> items, Matrix4x4 transform, Palette? palette = null, CelStyle? style = null, bool scenery = false, ModelPose? pose = null,
        object? owner = null, Matrix4x4[]? world = null, Vector3? anchor = null)
    {
        palette ??= Empty;
        style ??= CelStyle.Default;
        world ??= pose == null ? _rest : Data.WorldMatrices(pose);
        var group = owner ?? new object();
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
                    CastsShadow = true, ReceivesShadows = scenery, Hatch = material.Hatch, Anchor = anchor ?? transform.Translation,
                    Glow = material.Emission, Features = _features[meshIndex][p], Islands = _islands[meshIndex][p],
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
        foreach (var islands in _islands.SelectMany(i => i))
            islands.Buffer.Dispose();
        foreach (var data in _skins.SelectMany(s => s))
        {
            data?.Influences.Dispose();
            data?.Fans.Dispose();
        }
    }
}
