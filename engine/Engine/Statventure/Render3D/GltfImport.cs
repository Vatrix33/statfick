using System.Buffers.Binary;
using System.Numerics;
using System.Text.Json;
using Brutal.GltfApi;
using Brutal.Numerics;

namespace Statventure.Render3D;

/// <summary>
/// Reads a glTF 2.0 file (.glb or .gltf) into <see cref="ModelData"/>. BRUTAL's glTF package parses
/// the container and the JSON; this decodes the accessors (component types, normalisation, strides,
/// matrix column padding, sparse values) as the specification lays them out, and builds the
/// hierarchy, materials and skins. Points and lines are skipped (the engine draws its own ink lines).
/// BRUTAL drops extras, so the nodes' <c>ink</c> and the materials' <c>hatch</c> extras are read from
/// the JSON here.
/// </summary>
public static class GltfImport
{
    /// <summary>Required extensions this reader handles: quantized attributes decode like any other accessor.</summary>
    static readonly HashSet<string> SupportedRequired = ["KHR_mesh_quantization"];

    public static ModelData Load(string path)
    {
        using var loader = new GltfLoader(Path.GetFullPath(path));
        var gltf = loader.GltfJson;
        foreach (var extension in gltf.ExtensionsRequired ?? [])
            if (!SupportedRequired.Contains(extension))
                throw new NotSupportedException($"{path} requires the glTF extension {extension}");
        var reader = new AccessorReader(loader, path);
        var nodes = ReadNodes(gltf, path);
        var (inkless, unhatched) = SwitchedOff(path);
        for (var i = 0; i < nodes.Length; i++)
            if (inkless.Contains(i))
                nodes[i] = nodes[i] with { Ink = false };
        var materials = (gltf.Materials ?? []).Select(ReadMaterial).ToArray();
        for (var i = 0; i < materials.Length; i++)
            if (unhatched.Contains(i))
                materials[i] = materials[i] with { Hatch = false };
        return new ModelData
        {
            Nodes = nodes,
            Meshes = (gltf.Meshes ?? []).Select(m => ReadMesh(reader, m)).ToArray(),
            Materials = materials,
            Skins = (gltf.Skins ?? []).Select(s => ReadSkin(reader, s)).ToArray(),
            Animations = (gltf.Animations ?? []).Select((a, i) => ReadAnimation(reader, a, i, nodes.Length)).ToArray(),
            Roots = gltf.Scenes is { Length: > 0 } scenes
                ? scenes[gltf.Scene ?? 0].Nodes ?? []
                : Enumerable.Range(0, nodes.Length).Where(i => nodes[i].Parent < 0).ToArray(),
        };
    }

    /// <summary>
    /// Nodes whose extras say <c>"ink": 0</c> and materials whose extras say <c>"hatch": 0</c> (or
    /// false): Blender's custom properties on objects and materials, exported with the extras.
    /// </summary>
    static (HashSet<int> Inkless, HashSet<int> Unhatched) SwitchedOff(string path)
    {
        var bytes = File.ReadAllBytes(path);
        ReadOnlySpan<byte> json = bytes;
        // A .glb: a 12-byte header, then the JSON chunk (length, type "JSON", data).
        if (bytes.Length >= 20 && BinaryPrimitives.ReadUInt32LittleEndian(bytes) == 0x46546C67)
            json = bytes.AsSpan(20, (int)BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12)));
        using var document = JsonDocument.Parse(json.ToArray());
        return (Off(document.RootElement, "nodes"u8, "ink"u8), Off(document.RootElement, "materials"u8, "hatch"u8));

        static HashSet<int> Off(JsonElement root, ReadOnlySpan<byte> list, ReadOnlySpan<byte> property)
        {
            var off = new HashSet<int>();
            if (!root.TryGetProperty(list, out var items))
                return off;
            var index = 0;
            foreach (var item in items.EnumerateArray())
            {
                if (item.TryGetProperty("extras"u8, out var extras) && extras.ValueKind == JsonValueKind.Object && extras.TryGetProperty(property, out var value)
                    && (value.ValueKind == JsonValueKind.False || value.ValueKind == JsonValueKind.Number && value.GetDouble() == 0))
                    off.Add(index);
                index++;
            }
            return off;
        }
    }

    static ModelNode[] ReadNodes(Gltf gltf, string path)
    {
        var source = gltf.Nodes ?? [];
        var parents = new int[source.Length];
        Array.Fill(parents, -1);
        for (var i = 0; i < source.Length; i++)
            foreach (var child in source[i].Children ?? [])
            {
                if (child < 0 || child >= source.Length || parents[child] >= 0 || child == i)
                    throw new InvalidDataException($"{path}: node {child} has more than one parent or none that exists");
                parents[child] = i;
            }
        var nodes = new ModelNode[source.Length];
        for (var i = 0; i < source.Length; i++)
        {
            var n = source[i];
            var translation = new Vector3(n.Translation.X, n.Translation.Y, n.Translation.Z);
            var rotation = new Quaternion(n.Rotation.X, n.Rotation.Y, n.Rotation.Z, n.Rotation.W);
            var scale = new Vector3(n.Scale.X, n.Scale.Y, n.Scale.Z);
            // A node has a matrix or translation, rotation and scale; the parser leaves the other at
            // identity. T·R·S for column vectors is S·R·T for System.Numerics' row vectors.
            var local = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(translation)
                * ToMatrix(n.Matrix);
            nodes[i] = new ModelNode(n.Name ?? "", parents[i], n.Children ?? [], local, translation, rotation, scale, n.Mesh ?? -1, n.Skin ?? -1);
        }
        return nodes;
    }

    /// <summary>
    /// glTF stores matrices by columns of the column-vector matrix; BRUTAL's float4x4 keeps them as X,
    /// Y, Z, W, which are System.Numerics' rows (row vectors): the same numbers in the same order.
    /// </summary>
    static Matrix4x4 ToMatrix(float4x4 m) => new(
        m.X.X, m.X.Y, m.X.Z, m.X.W,
        m.Y.X, m.Y.Y, m.Y.Z, m.Y.W,
        m.Z.X, m.Z.Y, m.Z.Z, m.Z.W,
        m.W.X, m.W.Y, m.W.Z, m.W.W);

    static ModelMaterial ReadMaterial(Material m)
    {
        var color = m.PbrMetallicRoughness?.BaseColorFactor ?? new float4(1, 1, 1, 1);
        var extensions = m.Extensions ?? [];
        var strength = extensions.TryGetValue("KHR_materials_emissive_strength", out var e) && e is JsonElement element
            && element.TryGetProperty("emissiveStrength"u8, out var value) ? value.GetSingle() : 1f;
        return new ModelMaterial(m.Name ?? "", new Vector4(color.X, color.Y, color.Z, color.W), m.DoubleSided)
        {
            Emission = new Vector3(m.EmissiveFactor.X, m.EmissiveFactor.Y, m.EmissiveFactor.Z) * strength,
            Unlit = extensions.ContainsKey("KHR_materials_unlit"),
        };
    }

    /// <summary>
    /// An animation's channels with their samplers' keys (glTF 2.0, "Animations"): times from the
    /// input accessor (scalars), values from the output accessor (normalised integer rotations
    /// decoded by glTF's formulas). Channels on morph target weights and channels without a node
    /// (left to extensions) are skipped.
    /// </summary>
    static ModelAnimation ReadAnimation(AccessorReader reader, Brutal.GltfApi.Animation a, int index, int nodeCount)
    {
        var samplers = a.Samplers ?? [];
        var channels = new List<ModelAnimationChannel>();
        foreach (var c in a.Channels ?? [])
        {
            if (c.Target.Node is not int node || c.Target.Path == AnimationChannelTarget.PathEnum.Weights)
                continue;
            if (node < 0 || node >= nodeCount || c.Sampler < 0 || c.Sampler >= samplers.Length)
                throw new InvalidDataException($"{reader.Path}: animation {index} has a channel on a node or sampler that doesn't exist");
            var sampler = samplers[c.Sampler];
            var path = c.Target.Path switch
            {
                AnimationChannelTarget.PathEnum.Translation => AnimationPath.Translation,
                AnimationChannelTarget.PathEnum.Rotation => AnimationPath.Rotation,
                _ => AnimationPath.Scale,
            };
            var interpolation = sampler.Interpolation switch
            {
                AnimationSampler.InterpolationEnum.Step => AnimationInterpolation.Step,
                AnimationSampler.InterpolationEnum.Cubicspline => AnimationInterpolation.CubicSpline,
                _ => AnimationInterpolation.Linear,
            };
            var times = reader.Floats(sampler.Input, Accessor.TypeEnum.Scalar);
            var values = reader.Floats(sampler.Output, path == AnimationPath.Rotation ? Accessor.TypeEnum.Vec4 : Accessor.TypeEnum.Vec3);
            var components = path == AnimationPath.Rotation ? 4 : 3;
            var perKey = interpolation == AnimationInterpolation.CubicSpline ? 3 * components : components;
            if (times.Length == 0 || values.Length != times.Length * perKey)
                throw new InvalidDataException($"{reader.Path}: animation {index} has a sampler whose output doesn't match its {times.Length} keys");
            if (interpolation == AnimationInterpolation.CubicSpline && times.Length < 2)
                throw new InvalidDataException($"{reader.Path}: animation {index} has a CUBICSPLINE sampler with fewer than 2 keys");
            for (var k = 1; k < times.Length; k++)
                if (!(times[k] > times[k - 1]))
                    throw new InvalidDataException($"{reader.Path}: animation {index} has key times that don't increase");
            channels.Add(new ModelAnimationChannel(node, path, interpolation, times, values));
        }
        return new ModelAnimation(a.Name ?? "", channels);
    }

    static ModelSkin ReadSkin(AccessorReader reader, Skin s)
    {
        var joints = s.Joints ?? [];
        Matrix4x4[] inverseBind;
        if (s.InverseBindMatrices is { } accessor)
        {
            var values = reader.Floats(accessor, Accessor.TypeEnum.Mat4);
            inverseBind = new Matrix4x4[values.Length / 16];
            for (var i = 0; i < inverseBind.Length; i++)
                inverseBind[i] = ToMatrix(values.AsSpan(i * 16, 16));
        }
        else
        {
            inverseBind = new Matrix4x4[joints.Length];
            Array.Fill(inverseBind, Matrix4x4.Identity);
        }
        if (inverseBind.Length < joints.Length)
            throw new InvalidDataException($"skin {s.Name}: {joints.Length} joints but {inverseBind.Length} inverse bind matrices");
        return new ModelSkin(s.Name ?? "", joints, inverseBind, s.Skeleton ?? -1);
    }

    static Matrix4x4 ToMatrix(ReadOnlySpan<float> m) => new(
        m[0], m[1], m[2], m[3], m[4], m[5], m[6], m[7], m[8], m[9], m[10], m[11], m[12], m[13], m[14], m[15]);

    static ModelMesh ReadMesh(AccessorReader reader, Brutal.GltfApi.Mesh mesh)
    {
        var primitives = new List<ModelPrimitive>();
        foreach (var p in mesh.Primitives ?? [])
            if (ReadPrimitive(reader, p) is { } primitive)
                primitives.Add(primitive);
        return new ModelMesh(mesh.Name ?? "", primitives);
    }

    static ModelPrimitive? ReadPrimitive(AccessorReader reader, MeshPrimitive p)
    {
        if (p.Mode is not (MeshPrimitive.ModeEnum.Triangles or MeshPrimitive.ModeEnum.TriangleStrip or MeshPrimitive.ModeEnum.TriangleFan))
            return null;
        var attributes = p.Attributes ?? [];
        if (!attributes.TryGetValue("POSITION", out var positionAccessor))
            return null;
        var positions = ToVector3(reader.Floats(positionAccessor, Accessor.TypeEnum.Vec3));
        var vertexCount = positions.Length;
        var order = p.Indices is { } indexAccessor ? reader.UInts(indexAccessor, Accessor.TypeEnum.Scalar) : Sequence(vertexCount);
        var indices = Triangles(order, p.Mode);
        foreach (var index in indices)
            if (index >= vertexCount)
                throw new InvalidDataException($"{reader.Path}: index {index} beyond {vertexCount} vertices");
        Vector3[]? normals = attributes.TryGetValue("NORMAL", out var n) ? ToVector3(reader.Floats(n, Accessor.TypeEnum.Vec3)) : null;
        Vector2[]? texCoords = attributes.TryGetValue("TEXCOORD_0", out var t) ? ToVector2(reader.Floats(t, Accessor.TypeEnum.Vec2)) : null;
        JointIndices[]? joints = attributes.TryGetValue("JOINTS_0", out var j) ? ToJoints(reader.UInts(j, Accessor.TypeEnum.Vec4)) : null;
        Vector4[]? weights = attributes.TryGetValue("WEIGHTS_0", out var w) ? ToVector4(reader.Floats(w, Accessor.TypeEnum.Vec4)) : null;
        foreach (var (name, length) in new[] { ("NORMAL", normals?.Length), ("TEXCOORD_0", texCoords?.Length), ("JOINTS_0", joints?.Length), ("WEIGHTS_0", weights?.Length) })
            if (length is { } l && l != vertexCount)
                throw new InvalidDataException($"{reader.Path}: {name} has {l} values for {vertexCount} vertices");
        if (normals == null)
        {
            // glTF: without normals, flat normals. Each triangle gets its own vertices.
            positions = Unweld(positions, indices);
            texCoords = texCoords == null ? null : Unweld(texCoords, indices);
            joints = joints == null ? null : Unweld(joints, indices);
            weights = weights == null ? null : Unweld(weights, indices);
            normals = new Vector3[positions.Length];
            for (var i = 0; i < positions.Length; i += 3)
            {
                var normal = Vector3.Cross(positions[i + 1] - positions[i], positions[i + 2] - positions[i]);
                normal = normal.LengthSquared() > 0 ? Vector3.Normalize(normal) : Vector3.UnitY;
                normals[i] = normals[i + 1] = normals[i + 2] = normal;
            }
            indices = Sequence(positions.Length);
        }
        return new ModelPrimitive
        {
            Positions = positions,
            Normals = normals,
            TexCoords = texCoords,
            Joints = joints,
            Weights = weights,
            Indices = indices,
            Material = p.Material ?? -1,
        };
    }

    static uint[] Sequence(int count)
    {
        var indices = new uint[count];
        for (var i = 0; i < count; i++)
            indices[i] = (uint)i;
        return indices;
    }

    /// <summary>Triangle lists from lists, strips and fans, as glTF's topologies define them.</summary>
    static uint[] Triangles(uint[] order, MeshPrimitive.ModeEnum mode)
    {
        switch (mode)
        {
            case MeshPrimitive.ModeEnum.Triangles:
                return order.Length % 3 == 0 ? order : order[..(order.Length / 3 * 3)];
            case MeshPrimitive.ModeEnum.TriangleStrip:
            {
                var list = new uint[Math.Max(0, order.Length - 2) * 3];
                for (var i = 0; i + 2 < order.Length; i++)
                {
                    // {p(i), p(i + 1 + i%2), p(i + 2 − i%2)}
                    list[i * 3] = order[i];
                    list[i * 3 + 1] = order[i + 1 + i % 2];
                    list[i * 3 + 2] = order[i + 2 - i % 2];
                }
                return list;
            }
            default:
            {
                // Fan: {p(i + 1), p(i + 2), p(0)}
                var list = new uint[Math.Max(0, order.Length - 2) * 3];
                for (var i = 0; i + 2 < order.Length; i++)
                {
                    list[i * 3] = order[i + 1];
                    list[i * 3 + 1] = order[i + 2];
                    list[i * 3 + 2] = order[0];
                }
                return list;
            }
        }
    }

    static T[] Unweld<T>(T[] values, uint[] indices)
    {
        var result = new T[indices.Length];
        for (var i = 0; i < indices.Length; i++)
            result[i] = values[indices[i]];
        return result;
    }

    static Vector2[] ToVector2(float[] v)
    {
        var result = new Vector2[v.Length / 2];
        for (var i = 0; i < result.Length; i++)
            result[i] = new Vector2(v[i * 2], v[i * 2 + 1]);
        return result;
    }

    static Vector3[] ToVector3(float[] v)
    {
        var result = new Vector3[v.Length / 3];
        for (var i = 0; i < result.Length; i++)
            result[i] = new Vector3(v[i * 3], v[i * 3 + 1], v[i * 3 + 2]);
        return result;
    }

    static Vector4[] ToVector4(float[] v)
    {
        var result = new Vector4[v.Length / 4];
        for (var i = 0; i < result.Length; i++)
            result[i] = new Vector4(v[i * 4], v[i * 4 + 1], v[i * 4 + 2], v[i * 4 + 3]);
        return result;
    }

    static JointIndices[] ToJoints(uint[] v)
    {
        var result = new JointIndices[v.Length / 4];
        for (var i = 0; i < result.Length; i++)
            result[i] = new JointIndices((ushort)v[i * 4], (ushort)v[i * 4 + 1], (ushort)v[i * 4 + 2], (ushort)v[i * 4 + 3]);
        return result;
    }

    /// <summary>Decodes accessors into flat arrays of components.</summary>
    sealed class AccessorReader(GltfLoader loader, string path)
    {
        public string Path => path;

        Accessor Get(int index, Accessor.TypeEnum type)
        {
            var accessors = loader.GltfJson.Accessors ?? [];
            if (index < 0 || index >= accessors.Length)
                throw new InvalidDataException($"{path}: accessor {index} doesn't exist");
            var accessor = accessors[index];
            if (accessor.Type != type)
                throw new InvalidDataException($"{path}: accessor {index} is {accessor.Type}, expected {type}");
            return accessor;
        }

        /// <summary>Components as floats: normalised integers to −1..1 or 0..1 (glTF's formulas), others as their value.</summary>
        public float[] Floats(int index, Accessor.TypeEnum type)
        {
            var accessor = Get(index, type);
            var values = new float[accessor.Count * Components(type)];
            Decode(accessor, (i, bytes) => values[i] = Component(bytes, accessor.ComponentType, accessor.Normalized));
            return values;
        }

        /// <summary>Unsigned integer components (indices, joints).</summary>
        public uint[] UInts(int index, Accessor.TypeEnum type)
        {
            var accessor = Get(index, type);
            if (accessor.ComponentType is not (Accessor.ComponentTypeEnum.UnsignedByte or Accessor.ComponentTypeEnum.UnsignedShort or Accessor.ComponentTypeEnum.UnsignedInt))
                throw new InvalidDataException($"{path}: accessor {index} has {accessor.ComponentType} components, expected unsigned integers");
            var values = new uint[accessor.Count * Components(type)];
            Decode(accessor, (i, bytes) => values[i] = accessor.ComponentType switch
            {
                Accessor.ComponentTypeEnum.UnsignedByte => bytes[0],
                Accessor.ComponentTypeEnum.UnsignedShort => BinaryPrimitives.ReadUInt16LittleEndian(bytes),
                _ => BinaryPrimitives.ReadUInt32LittleEndian(bytes),
            });
            return values;
        }

        delegate void ComponentSink(int index, ReadOnlySpan<byte> bytes);

        /// <summary>
        /// Calls <paramref name="sink"/> for every component, element by element: from the buffer
        /// view (its stride, or tightly packed; matrix columns start on 4-byte boundaries), zero
        /// without one, then the sparse substitutions (tightly packed values).
        /// </summary>
        void Decode(Accessor accessor, ComponentSink sink)
        {
            var componentSize = ComponentSize(accessor.ComponentType);
            var (columns, rows) = Shape(accessor.Type);
            var columnStride = columns > 1 ? (rows * componentSize + 3) & ~3 : rows * componentSize;
            var elementSize = columns * columnStride;
            var components = columns * rows;
            Span<byte> zero = stackalloc byte[4];
            zero.Clear();
            if (accessor.BufferView is { } viewIndex)
            {
                var view = loader.GltfJson.BufferViews[viewIndex];
                var stride = view.ByteStride ?? elementSize;
                var data = loader.GetBufferView(viewIndex);
                if (accessor.Count > 0 && accessor.ByteOffset + (long)stride * (accessor.Count - 1) + elementSize > data.Length)
                    throw new InvalidDataException($"{path}: an accessor runs past its buffer view");
                for (var e = 0; e < accessor.Count; e++)
                    ReadElement(data[(accessor.ByteOffset + e * stride)..], e, sink);
            }
            else
                for (var i = 0; i < accessor.Count * components; i++)
                    sink(i, zero);
            if (accessor.Sparse is { Count: > 0 } sparse)
            {
                var indexSize = sparse.Indices.ComponentType switch
                {
                    AccessorSparseIndices.ComponentTypeEnum.UnsignedByte => 1,
                    AccessorSparseIndices.ComponentTypeEnum.UnsignedShort => 2,
                    _ => 4,
                };
                var indexData = loader.GetBufferView(sparse.Indices.BufferView)[sparse.Indices.ByteOffset..];
                var valueData = loader.GetBufferView(sparse.Values.BufferView)[sparse.Values.ByteOffset..];
                if (indexData.Length < sparse.Count * indexSize || valueData.Length < (long)sparse.Count * elementSize)
                    throw new InvalidDataException($"{path}: sparse values run past their buffer views");
                for (var s = 0; s < sparse.Count; s++)
                {
                    var target = indexSize switch
                    {
                        1 => indexData[s],
                        2 => BinaryPrimitives.ReadUInt16LittleEndian(indexData[(s * 2)..]),
                        _ => (int)BinaryPrimitives.ReadUInt32LittleEndian(indexData[(s * 4)..]),
                    };
                    if (target < 0 || target >= accessor.Count)
                        throw new InvalidDataException($"{path}: sparse index {target} beyond {accessor.Count} elements");
                    ReadElement(valueData[(s * elementSize)..], target, sink);
                }
            }

            void ReadElement(ReadOnlySpan<byte> element, int elementIndex, ComponentSink sink)
            {
                for (var c = 0; c < columns; c++)
                    for (var r = 0; r < rows; r++)
                        sink(elementIndex * components + c * rows + r, element[(c * columnStride + r * componentSize)..]);
            }
        }

        static float Component(ReadOnlySpan<byte> bytes, Accessor.ComponentTypeEnum type, bool normalized) => type switch
        {
            Accessor.ComponentTypeEnum.Float => BinaryPrimitives.ReadSingleLittleEndian(bytes),
            Accessor.ComponentTypeEnum.UnsignedByte => normalized ? bytes[0] / 255f : bytes[0],
            Accessor.ComponentTypeEnum.Byte => normalized ? MathF.Max((sbyte)bytes[0] / 127f, -1) : (sbyte)bytes[0],
            Accessor.ComponentTypeEnum.UnsignedShort => normalized
                ? BinaryPrimitives.ReadUInt16LittleEndian(bytes) / 65535f
                : BinaryPrimitives.ReadUInt16LittleEndian(bytes),
            Accessor.ComponentTypeEnum.Short => normalized
                ? MathF.Max(BinaryPrimitives.ReadInt16LittleEndian(bytes) / 32767f, -1)
                : BinaryPrimitives.ReadInt16LittleEndian(bytes),
            _ => BinaryPrimitives.ReadUInt32LittleEndian(bytes),
        };

        static int ComponentSize(Accessor.ComponentTypeEnum type) => type switch
        {
            Accessor.ComponentTypeEnum.Byte or Accessor.ComponentTypeEnum.UnsignedByte => 1,
            Accessor.ComponentTypeEnum.Short or Accessor.ComponentTypeEnum.UnsignedShort => 2,
            _ => 4,
        };

        static int Components(Accessor.TypeEnum type)
        {
            var (columns, rows) = Shape(type);
            return columns * rows;
        }

        /// <summary>Matrices are columns of rows; vectors and scalars one column.</summary>
        static (int Columns, int Rows) Shape(Accessor.TypeEnum type) => type switch
        {
            Accessor.TypeEnum.Scalar => (1, 1),
            Accessor.TypeEnum.Vec2 => (1, 2),
            Accessor.TypeEnum.Vec3 => (1, 3),
            Accessor.TypeEnum.Vec4 => (1, 4),
            Accessor.TypeEnum.Mat2 => (2, 2),
            Accessor.TypeEnum.Mat3 => (3, 3),
            _ => (4, 4),
        };
    }
}
