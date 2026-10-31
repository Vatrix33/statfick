using System.Numerics;
using System.Text.Json;
using Brutal.StbApi;
using Brutal.VulkanApi;
using Statventure.Graphics;

namespace Statventure.Render3D;

/// <summary>
/// A region's terrain (Docs/Renderer-3D.md, "Terrain"): a grid of points, each with a height and a
/// cover (an index into <see cref="CoverSlots"/>, palette slots), <see cref="CellSize"/> metres apart
/// along X and Z, and the water level (none: no water). Read from a metadata file naming a 16-bit
/// height image (0..65535 over HeightMin..HeightMax) and an 8-bit cover image, one pixel a point,
/// the image's first row at Z = 0.
/// </summary>
public sealed class TerrainData
{
    public required int Width { get; init; }
    public required int Depth { get; init; }
    public required float CellSize { get; init; }
    public required float[] Heights { get; init; }
    public required byte[] Covers { get; init; }
    public required string[] CoverSlots { get; init; }
    public float? WaterLevel { get; init; }

    public float HeightAt(int i, int j) => Heights[Math.Clamp(j, 0, Depth - 1) * Width + Math.Clamp(i, 0, Width - 1)];

    public static TerrainData Load(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var root = document.RootElement;
        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        int width = root.GetProperty("width"u8).GetInt32(), depth = root.GetProperty("depth"u8).GetInt32();
        float low = root.GetProperty("heightMin"u8).GetSingle(), high = root.GetProperty("heightMax"u8).GetSingle();
        var heightImage = Stb.Load16(Path.Combine(folder, root.GetProperty("height"u8).GetString()!), 1);
        if (heightImage.IsNull() || heightImage.Width != width || heightImage.Height != depth)
            throw new InvalidDataException($"{path}: the height image isn't {width}x{depth} 16-bit");
        var heights = new float[width * depth];
        try
        {
            var samples = heightImage.AsSpan();
            for (var i = 0; i < heights.Length; i++)
                heights[i] = low + samples[i] / 65535f * (high - low);
        }
        finally
        {
            heightImage.Dispose();
        }
        var coverImage = Stb.Load(Path.Combine(folder, root.GetProperty("cover"u8).GetString()!), 1);
        if (coverImage.IsNull() || coverImage.Width != width || coverImage.Height != depth)
            throw new InvalidDataException($"{path}: the cover image isn't {width}x{depth}");
        byte[] covers;
        try
        {
            covers = coverImage.AsSpan().ToArray();
        }
        finally
        {
            coverImage.Dispose();
        }
        return new TerrainData
        {
            Width = width,
            Depth = depth,
            CellSize = root.GetProperty("cellSize"u8).GetSingle(),
            Heights = heights,
            Covers = covers,
            CoverSlots = root.GetProperty("covers"u8).EnumerateArray().Select(c => c.GetString()!).ToArray(),
            WaterLevel = root.TryGetProperty("waterLevel"u8, out var water) && water.ValueKind == JsonValueKind.Number ? water.GetSingle() : null,
        };
    }
}

/// <summary>
/// A terrain's cover blending for terrain.frag: the grid's start (world X, Z), cell size, cover map
/// and size, and the covers' tones (lit, shadow; linear).
/// </summary>
public sealed record TerrainShade(Vector2 Origin, float CellSize, GpuImage Covers, int Width, int Depth, Vector4[] Tones);

/// <summary>
/// A terrain on the GPU (Docs/Renderer-3D.md, "Terrain"): the ground in chunks of 32 cells, each at
/// strides 1, 2, 4 and 8 (level of detail), every cell split along the same diagonal as the Blender
/// reference's mesh, with smooth normals weighted by corner angles (Blender's). Along its borders a
/// chunk keeps every grid point at every stride, so neighbouring chunks share all their border
/// vertices and meet without cracks whatever their strides (no vertex lies on another's edge, where
/// rounding leaves gaps). The water is a quad at its level.
/// Placed by a translation only (its cover blending works in world X, Z).
/// </summary>
public sealed class Terrain : IDisposable
{
    const int ChunkCells = 32;
    static readonly int[] Strides = [1, 2, 4, 8];

    sealed record Chunk(int I0, int J0, int I1, int J1, Mesh[] Levels);

    readonly Chunk[] _chunks;
    readonly Mesh? _water;
    readonly GpuImage _covers;

    public TerrainData Data { get; }

    /// <summary>For tests: the level (index into the strides 1, 2, 4, 8) each chunk is drawn at, by chunk (rows of chunks from the grid's origin), in place of its size on screen.</summary>
    internal Func<int, int>? ForcedLevel { get; set; }

    Terrain(GpuDevice device, TerrainData data)
    {
        Data = data;
        var normals = Normals(data);
        var chunks = new List<Chunk>();
        for (var j0 = 0; j0 < data.Depth - 1; j0 += ChunkCells)
            for (var i0 = 0; i0 < data.Width - 1; i0 += ChunkCells)
            {
                int i1 = Math.Min(i0 + ChunkCells, data.Width - 1), j1 = Math.Min(j0 + ChunkCells, data.Depth - 1);
                chunks.Add(new Chunk(i0, j0, i1, j1, Strides.Select(s => ChunkMesh(device, data, normals, i0, j0, i1, j1, s)).ToArray()));
            }
        _chunks = chunks.ToArray();
        if (data.WaterLevel is { } level)
        {
            float x = (data.Width - 1) * data.CellSize, z = (data.Depth - 1) * data.CellSize;
            _water = Mesh.Create(device,
                [new Vertex3D(new(0, level, 0), Vector3.UnitY, default), new Vertex3D(new(0, level, z), Vector3.UnitY, default),
                 new Vertex3D(new(x, level, z), Vector3.UnitY, default), new Vertex3D(new(x, level, 0), Vector3.UnitY, default)],
                [0u, 1, 2, 0, 2, 3]);
        }
        _covers = GpuImage.CreateTexture(device, data.Width, data.Depth, data.Covers, VkFormat.R8UInt);
    }

    public static Terrain Create(GpuDevice device, TerrainData data) => new(device, data);

    public static Terrain Load(GpuDevice device, string path) => Create(device, TerrainData.Load(path));

    static Vector3 Point(TerrainData data, int i, int j) => new(i * data.CellSize, data.HeightAt(i, j), j * data.CellSize);

    /// <summary>Each point's smooth normal: its triangles' normals weighted by their angles at it (Blender's).</summary>
    static Vector3[] Normals(TerrainData data)
    {
        var sums = new Vector3[data.Width * data.Depth];
        void Add(int a, int b, int c, (int I, int J) pa, (int I, int J) pb, (int I, int J) pc)
        {
            Vector3 A = Point(data, pa.I, pa.J), B = Point(data, pb.I, pb.J), C = Point(data, pc.I, pc.J);
            var face = Vector3.Normalize(Vector3.Cross(B - A, C - A));
            float Angle(Vector3 at, Vector3 u, Vector3 v) => MathF.Acos(Math.Clamp(Vector3.Dot(Vector3.Normalize(u - at), Vector3.Normalize(v - at)), -1, 1));
            sums[a] += face * Angle(A, B, C);
            sums[b] += face * Angle(B, C, A);
            sums[c] += face * Angle(C, A, B);
        }
        for (var j = 0; j < data.Depth - 1; j++)
            for (var i = 0; i < data.Width - 1; i++)
            {
                int p00 = j * data.Width + i, p01 = (j + 1) * data.Width + i, p11 = (j + 1) * data.Width + i + 1, p10 = j * data.Width + i + 1;
                Add(p00, p01, p11, (i, j), (i, j + 1), (i + 1, j + 1));
                Add(p00, p11, p10, (i, j), (i + 1, j + 1), (i + 1, j));
            }
        return sums.Select(n => Vector3.Normalize(n)).ToArray();
    }

    /// <summary>
    /// A chunk's cells [i0, i1) × [j0, j1) at a stride. A cell on the chunk's border also has the grid
    /// points along it and is a fan about a point inside it, so every point of its outline is a
    /// corner of the triangles beside it: the grid point nearest its middle, or, in a cell one grid
    /// step across, its corners' mean. Cells without such points are split as at stride 1.
    /// </summary>
    static Mesh ChunkMesh(GpuDevice device, TerrainData data, Vector3[] normals, int i0, int j0, int i1, int j1, int stride)
    {
        var columns = new List<int>();
        for (var i = i0; i < i1; i += stride)
            columns.Add(i);
        columns.Add(i1);
        var rows = new List<int>();
        for (var j = j0; j < j1; j += stride)
            rows.Add(j);
        rows.Add(j1);
        var vertices = new List<Vertex3D>();
        var indexOf = new Dictionary<(int I, int J), uint>();
        uint At(int i, int j)
        {
            if (!indexOf.TryGetValue((i, j), out var index))
            {
                indexOf[(i, j)] = index = (uint)vertices.Count;
                vertices.Add(new Vertex3D(Point(data, i, j), normals[j * data.Width + i], default));
            }
            return index;
        }
        var indices = new List<uint>();
        var outline = new List<(int I, int J)>();
        for (var r = 0; r + 1 < rows.Count; r++)
            for (var c = 0; c + 1 < columns.Count; c++)
            {
                int ia = columns[c], ib = columns[c + 1], ja = rows[r], jb = rows[r + 1];
                // The outline in the order of the stride-1 split (p00, p01, p11, p10), with the
                // grid points of the sides on the chunk's border.
                outline.Clear();
                outline.Add((ia, ja));
                if (ia == i0)
                    for (var j = ja + 1; j < jb; j++)
                        outline.Add((ia, j));
                outline.Add((ia, jb));
                if (jb == j1)
                    for (var i = ia + 1; i < ib; i++)
                        outline.Add((i, jb));
                outline.Add((ib, jb));
                if (ib == i1)
                    for (var j = jb - 1; j > ja; j--)
                        outline.Add((ib, j));
                outline.Add((ib, ja));
                if (ja == j0)
                    for (var i = ib - 1; i > ia; i--)
                        outline.Add((i, ja));
                if (outline.Count == 4)
                {
                    uint p00 = At(ia, ja), p01 = At(ia, jb), p11 = At(ib, jb), p10 = At(ib, ja);
                    indices.AddRange([p00, p01, p11, p00, p11, p10]);
                    continue;
                }
                uint middle;
                int mi = (ia + ib) / 2, mj = (ja + jb) / 2;
                if (mi > ia && mi < ib && mj > ja && mj < jb)
                    middle = At(mi, mj);
                else
                {
                    (int I, int J)[] corners = [(ia, ja), (ia, jb), (ib, jb), (ib, ja)];
                    var position = corners.Aggregate(Vector3.Zero, (sum, p) => sum + Point(data, p.I, p.J)) / 4;
                    var normal = Vector3.Normalize(corners.Aggregate(Vector3.Zero, (sum, p) => sum + normals[p.J * data.Width + p.I]));
                    middle = (uint)vertices.Count;
                    vertices.Add(new Vertex3D(position, normal, default));
                }
                for (var k = 0; k < outline.Count; k++)
                {
                    var (from, to) = (outline[k], outline[(k + 1) % outline.Count]);
                    indices.AddRange([middle, At(from.I, from.J), At(to.I, to.J)]);
                }
            }
        return Mesh.Create(device, vertices.ToArray(), indices.ToArray());
    }

    /// <summary>
    /// Adds the terrain's draw items, placed by <paramref name="translation"/>: scenery (it casts and
    /// receives shadows, is hatched in shadow, has ink lines), one model; the ground in the covers'
    /// tones, each chunk at the coarsest stride whose cells are still about 2 pixels on screen
    /// (<paramref name="camera"/> on a target <paramref name="viewportHeight"/> pixels tall); the
    /// water in the palette's <c>water</c> tones.
    /// </summary>
    public void Draw(List<DrawItem> items, Vector3 translation, Camera camera, int viewportHeight, Palette? palette = null, CelStyle? style = null)
    {
        palette ??= new Palette();
        style ??= CelStyle.Default;
        var group = new object();
        var tones = Data.CoverSlots.SelectMany(slot =>
        {
            var (lit, shadow) = palette.Resolve(new ModelMaterial(slot, new Vector4(0.5f, 0.5f, 0.5f, 1), false), style);
            return new[] { lit, shadow };
        }).ToArray();
        var transform = Matrix4x4.CreateTranslation(translation);
        var levels = _chunks.Select((chunk, c) => ForcedLevel?.Invoke(c) ?? LevelFor(camera, viewportHeight, translation + new Vector3((chunk.I0 + chunk.I1) * 0.5f * Data.CellSize,
            Data.HeightAt((chunk.I0 + chunk.I1) / 2, (chunk.J0 + chunk.J1) / 2), (chunk.J0 + chunk.J1) * 0.5f * Data.CellSize))).ToArray();
        var shade = new TerrainShade(new Vector2(translation.X, translation.Z), Data.CellSize, _covers, Data.Width, Data.Depth, tones);
        for (var c = 0; c < _chunks.Length; c++)
        {
            items.Add(new DrawItem(_chunks[c].Levels[levels[c]], transform, Vector4.One)
            {
                Shading = Shading.Terrain, Group = group, Part = 1, Ink = true, CastsShadow = true, ReceivesShadows = true,
                Hatch = true, Anchor = translation, Terrain = shade,
            });
        }
        if (_water != null)
        {
            var (lit, shadow) = palette.Resolve(new ModelMaterial("water", new Vector4(0.2f, 0.35f, 0.5f, 1), false), style);
            items.Add(new DrawItem(_water, transform, lit)
            {
                Shading = Shading.Water, Shadow = shadow, Group = group, Part = 2, Ink = true, CastsShadow = false, ReceivesShadows = true,
                Hatch = true, Anchor = translation,
            });
        }
    }

    /// <summary>The level whose cells stay about 2 pixels or more on screen at a point.</summary>
    int LevelFor(Camera camera, int viewportHeight, Vector3 point)
    {
        var metres = camera.Projection == Projection.Orthographic
            ? camera.OrthographicHeight
            : 2 * MathF.Max(-Vector3.Transform(point, camera.View).Z, camera.Near) * MathF.Tan(camera.VerticalFieldOfView / 2);
        var cellPixels = Data.CellSize * viewportHeight / metres;
        var level = 0;
        while (level + 1 < Strides.Length && cellPixels * Strides[level + 1] <= 2)
            level++;
        return level;
    }

    public void Dispose()
    {
        foreach (var mesh in _chunks.SelectMany(c => c.Levels))
            mesh.Dispose();
        _water?.Dispose();
        _covers.Dispose();
    }
}
