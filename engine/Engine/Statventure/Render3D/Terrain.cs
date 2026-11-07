using System.Numerics;
using System.Text.Json;
using Brutal.StbApi;
using Brutal.VulkanApi;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Render3D;

/// <summary>
/// A region's terrain (Docs/Renderer-3D.md, "Terrain"): a grid of <see cref="Width"/> × <see cref="Depth"/>
/// points, each with a height and a cover (an index into <see cref="CoverSlots"/>, palette slots),
/// <see cref="CellSize"/> metres apart along X and Z, and the water level (none: no water). The heights
/// are 16-bit samples (0..65535 over <see cref="HeightMin"/>..<see cref="HeightMax"/>), turned into
/// metres when first read (<see cref="Heights"/>); points row by row, the first row at Z = 0.
/// Read from a description naming a height image and a cover image, one pixel a point
/// (<see cref="Load(string)"/>), or from the game's data (<see cref="Load(VirtualFileSystem, string)"/>):
/// the record a world.pak entry holds (<c>terrain</c>; <see cref="TerrainImporter"/>; Docs/Assets.md,
/// Q26, S8-2).
/// </summary>
[Record("terrain", 1)]
[Kind("Terrain", "world/terrain")]
[Help("A region's ground imported from a terrain description and its height and cover images: heights, the palette slot covering each point, and the water. Its heights and covers change only by importing again.")]
public sealed class TerrainData
{
    float[]? _heights;

    [Field(1), Limits(1, double.PositiveInfinity)] public required int Width { get; init; }
    [Field(2), Limits(1, double.PositiveInfinity)] public required int Depth { get; init; }
    [Field(3)] public required float CellSize { get; init; }
    /// <summary>The height of the sample 0.</summary>
    [Field(4)] public required float HeightMin { get; init; }
    /// <summary>The height of the sample 65535.</summary>
    [Field(5)] public required float HeightMax { get; init; }
    /// <summary>Each point's height as a 16-bit sample of the range (the height image's pixels).</summary>
    [Field(6), Bulk("{0:N0} height samples")] public required ushort[] HeightSamples { get; init; }
    /// <summary>Each point's cover: an index into <see cref="CoverSlots"/>.</summary>
    [Field(7), Bulk("{0:N0} covers")] public required byte[] Covers { get; init; }
    [Field(8)] public required string[] CoverSlots { get; init; }
    [Field(9)] public float? WaterLevel { get; init; }

    /// <summary>Each point's height in metres: <c>min + s / 65535 · (max − min)</c> in floats, as M6's loader turned the image's samples (not part of the record).</summary>
    public float[] Heights => _heights ??= ToHeights(HeightSamples, HeightMin, HeightMax);

    public float HeightAt(int i, int j) => Heights[Math.Clamp(j, 0, Depth - 1) * Width + Math.Clamp(i, 0, Width - 1)];

    static float[] ToHeights(ushort[] samples, float low, float high)
    {
        var heights = new float[samples.Length];
        for (var i = 0; i < heights.Length; i++)
            heights[i] = low + samples[i] / 65535f * (high - low);
        return heights;
    }

    /// <summary>
    /// Reads a terrain description (JSON: <c>width</c> and <c>depth</c> in points, <c>cellSize</c>,
    /// <c>heightMin</c> and <c>heightMax</c>, the <c>height</c> and <c>cover</c> images' file names,
    /// <c>covers</c>: the slot names, <c>waterLevel</c>: a number or none) and its two images, named
    /// relative to its folder. An <see cref="InvalidDataException"/> (a <see cref="JsonException"/>
    /// for text that isn't JSON) when it can't be used.
    /// </summary>
    public static TerrainData Load(string path)
    {
        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        try
        {
            return FromDescription(File.ReadAllBytes(path), name => File.ReadAllBytes(Path.Combine(folder, name)));
        }
        catch (InvalidDataException problem)
        {
            throw new InvalidDataException($"{path}: {problem.Message}", problem);
        }
    }

    /// <summary>
    /// The terrain at <paramref name="path"/> in the game's data (a world.pak entry such as
    /// <c>world/terrain/vale</c>, made from <c>world/terrain/vale.terrain.json</c> by
    /// <see cref="TerrainImporter"/>, or its JSON form in the development folder), checked as the
    /// description's loader checks its images (<see cref="Validate"/>): a <see cref="DataFileException"/>
    /// naming the pack and entry or the loose file when it isn't a readable terrain record or breaks a rule.
    /// </summary>
    public static TerrainData Load(VirtualFileSystem files, string path)
    {
        var terrain = files.Load<TerrainData>(path);
        try
        {
            terrain.Validate();
        }
        catch (InvalidDataException problem)
        {
            throw files.Problem(path, $"not a valid terrain: {problem.Message}", problem);
        }
        return terrain;
    }

    /// <summary>
    /// Throws an <see cref="InvalidDataException"/> unless the grid is at least 1 × 1 and has a height
    /// sample and a cover for each point: what the description's loader checks of its images (each
    /// <see cref="Width"/> × <see cref="Depth"/>). A terrain from a pack or a loose folder breaks it
    /// only if it was edited. Also the limits its fields declare (G4). The first of
    /// <see cref="RecordChecks.Check"/>'s problems.
    /// </summary>
    public void Validate()
    {
        if (RecordChecks.Check(this).FirstOrDefault() is { } problem)
            throw new InvalidDataException(problem.Message);
    }

    /// <summary>
    /// The terrain's own check (P20): <see cref="Validate"/>'s rules with their words, at the path of what
    /// they name (the grid's width or depth; the height samples; the covers). Without a grid the counts
    /// aren't compared.
    /// </summary>
    [Check]
    static void Rules(TerrainData terrain, CheckContext check)
    {
        int width = terrain.Width, depth = terrain.Depth;
        if (width < 1 || depth < 1)
        {
            var message = $"the grid is {width}x{depth} points";
            if (width < 1)
                check.Report(RecordPath.Root.Field("width"), message);
            if (depth < 1)
                check.Report(RecordPath.Root.Field("depth"), message);
            return;
        }
        var points = (long)width * depth;
        if (terrain.HeightSamples.Length != points)
            check.Report(RecordPath.Root.Field("heightSamples"), $"{terrain.HeightSamples.Length} height samples for {width}x{depth} points");
        if (terrain.Covers.Length != points)
            check.Report(RecordPath.Root.Field("covers"), $"{terrain.Covers.Length} covers for {width}x{depth} points");
    }

    /// <summary>
    /// A terrain from its description's bytes, its images read by name through <paramref name="read"/>
    /// (decoded by stb_image: the height image at 16 bits, the cover image at 8, each as one grey
    /// channel, as M6's loader did). Keys other than the description's are ignored; a missing
    /// <c>waterLevel</c>, or one that isn't a number, is no water.
    /// </summary>
    internal static TerrainData FromDescription(byte[] description, Func<string, byte[]> read)
    {
        using var document = JsonDocument.Parse(description);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("the description isn't a JSON object");
        int width = Whole(root, "width"), depth = Whole(root, "depth");
        var cellSize = Get(root, "cellSize", JsonValueKind.Number).GetSingle();
        float low = Get(root, "heightMin", JsonValueKind.Number).GetSingle(), high = Get(root, "heightMax", JsonValueKind.Number).GetSingle();
        string heightFile = Get(root, "height", JsonValueKind.String).GetString()!, coverFile = Get(root, "cover", JsonValueKind.String).GetString()!;
        var slots = Get(root, "covers", JsonValueKind.Array).EnumerateArray().Select((slot, i) => slot.ValueKind == JsonValueKind.String
            ? slot.GetString()!
            : throw new InvalidDataException($"covers[{i}] isn't a string")).ToArray();
        float? waterLevel = root.TryGetProperty("waterLevel"u8, out var water) && water.ValueKind == JsonValueKind.Number ? water.GetSingle() : null;
        var heightImage = Stb.Load16FromMemory(read(heightFile), 1);
        if (heightImage.IsNull())
            throw new InvalidDataException($"the height image {heightFile} isn't an image stb_image reads");
        ushort[] samples;
        try
        {
            if (heightImage.Width != width || heightImage.Height != depth)
                throw new InvalidDataException($"the height image {heightFile} is {heightImage.Width}x{heightImage.Height}, not {width}x{depth}");
            samples = heightImage.AsSpan().ToArray();
        }
        finally
        {
            heightImage.Dispose();
        }
        var coverImage = Stb.LoadFromMemory(read(coverFile), 1);
        if (coverImage.IsNull())
            throw new InvalidDataException($"the cover image {coverFile} isn't an image stb_image reads");
        byte[] covers;
        try
        {
            if (coverImage.Width != width || coverImage.Height != depth)
                throw new InvalidDataException($"the cover image {coverFile} is {coverImage.Width}x{coverImage.Height}, not {width}x{depth}");
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
            CellSize = cellSize,
            HeightMin = low,
            HeightMax = high,
            HeightSamples = samples,
            Covers = covers,
            CoverSlots = slots,
            WaterLevel = waterLevel,
        };
    }

    static JsonElement Get(JsonElement root, string key, JsonValueKind kind) =>
        root.TryGetProperty(key, out var value) && value.ValueKind == kind
            ? value
            : throw new InvalidDataException($"the description has no {kind.ToString().ToLowerInvariant()} \"{key}\"");

    static int Whole(JsonElement root, string key) =>
        Get(root, key, JsonValueKind.Number).TryGetInt32(out var value) ? value : throw new InvalidDataException($"\"{key}\" isn't a whole number");
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

    /// <summary>A terrain from its description (<see cref="TerrainData.Load(string)"/>).</summary>
    public static Terrain Load(GpuDevice device, string path) => Create(device, TerrainData.Load(path));

    /// <summary>A terrain from the game's data: the terrain record at <paramref name="path"/> (<see cref="TerrainData.Load(VirtualFileSystem, string)"/>).</summary>
    public static Terrain Load(GpuDevice device, VirtualFileSystem files, string path) => Create(device, TerrainData.Load(files, path));

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
    /// water in the palette's <c>water</c> tones. <paramref name="owner"/> is the items' group, which a
    /// 3D view's picking gives back (a new object when none is given).
    /// </summary>
    public void Draw(List<DrawItem> items, Vector3 translation, Camera camera, int viewportHeight, Palette? palette = null, CelStyle? style = null,
        object? owner = null)
    {
        palette ??= new Palette();
        style ??= CelStyle.Default;
        var group = owner ?? new object();
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
