using System.Numerics;
using System.Runtime.InteropServices;
using Brutal.VulkanApi;
using Statventure.Graphics;

namespace Statventure.Render3D;

/// <summary>A mesh vertex as the shaders read it (scalar layout, 32 bytes).</summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public struct Vertex3D(Vector3 position, Vector3 normal, Vector2 uv)
{
    public Vector3 Position = position;
    public Vector3 Normal = normal;
    public Vector2 Uv = uv;
}

/// <summary>Triangles on the GPU: vertices pulled by their buffer's address, 32-bit indices, and their bounds.</summary>
public sealed class Mesh : IDisposable
{
    public GpuBuffer Vertices { get; }
    public GpuBuffer Indices { get; }
    public int IndexCount { get; }
    public int VertexCount { get; }
    /// <summary>The smallest and largest vertex position (the mesh's own space).</summary>
    public Vector3 BoundsMin { get; }
    public Vector3 BoundsMax { get; }

    Mesh(GpuBuffer vertices, GpuBuffer indices, int vertexCount, int indexCount, Vector3 boundsMin, Vector3 boundsMax)
    {
        Vertices = vertices;
        Indices = indices;
        VertexCount = vertexCount;
        IndexCount = indexCount;
        BoundsMin = boundsMin;
        BoundsMax = boundsMax;
    }

    public static Mesh Create(GpuDevice device, ReadOnlySpan<Vertex3D> vertices, ReadOnlySpan<uint> indices)
    {
        if (vertices.Length == 0 || indices.Length == 0 || indices.Length % 3 != 0)
            throw new ArgumentException("a mesh needs vertices and whole triangles");
        var (min, max) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
        foreach (var vertex in vertices)
        {
            min = Vector3.Min(min, vertex.Position);
            max = Vector3.Max(max, vertex.Position);
        }
        var vertexBuffer = GpuBuffer.CreateWithData(device, vertices, VkBufferUsageFlags.StorageBufferBit);
        var indexBuffer = GpuBuffer.CreateWithData(device, indices, VkBufferUsageFlags.IndexBufferBit);
        return new Mesh(vertexBuffer, indexBuffer, vertices.Length, indices.Length, min, max);
    }

    /// <summary>
    /// A mesh with flat faces from positions and triangles: each triangle gets its own three
    /// vertices with the face's normal (counter-clockwise triangles face outward, as in glTF).
    /// </summary>
    public static Mesh FromTriangles(GpuDevice device, ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> triangles)
    {
        var vertices = new Vertex3D[triangles.Length];
        var indices = new uint[triangles.Length];
        for (var i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = positions[triangles[i]], b = positions[triangles[i + 1]], c = positions[triangles[i + 2]];
            var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            vertices[i] = new Vertex3D(a, normal, default);
            vertices[i + 1] = new Vertex3D(b, normal, default);
            vertices[i + 2] = new Vertex3D(c, normal, default);
            indices[i] = (uint)i;
            indices[i + 1] = (uint)(i + 1);
            indices[i + 2] = (uint)(i + 2);
        }
        return Create(device, vertices, indices);
    }

    public void Dispose()
    {
        Vertices.Dispose();
        Indices.Dispose();
    }
}
