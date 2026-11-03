using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;

namespace Statventure.Tests.Render3D;

/// <summary>
/// The test terrain (Data/Terrain/test.json and its two images) packed as the packer packs a folder
/// (Docs/Assets.md, "How it works", "Terrain in packs"): the description copied as
/// <c>world/terrain/test.terrain.json</c> with its images, and the Blender-only one-hot cover image
/// beside them, into a temporary folder that goes through the engine's importers into a world.pak
/// there, mounted.
/// </summary>
public sealed class TestTerrainPack : IDisposable
{
    readonly TempFolder _folder = new();

    public TestTerrainPack()
    {
        Copy(_folder, "source/world/terrain", "test");
        _folder.Write("source/world/terrain/test-cover-onehot.png", File.ReadAllBytes(Path.Combine(Render3DReferenceTests.TerrainDirectory, "test-cover-onehot.png")));
        var writer = new PackWriter();
        writer.AddFolder(_folder["source"]);
        writer.Write(PackFile);
        Files = new VirtualFileSystem([Mount.Pack(PackFile)]);
    }

    /// <summary>The committed test terrain's description (its name since M6; the importer's sources are <c>&lt;name&gt;.terrain.json</c>).</summary>
    public static string Description => Path.Combine(Render3DReferenceTests.TerrainDirectory, "test.json");

    /// <summary>The record's path in the pack.</summary>
    public const string RecordPath = "world/terrain/test";

    public string PackFile => _folder["world.pak"];

    /// <summary>A folder of its own beside the pack.</summary>
    public string Scratch(string name) => _folder[name];

    public VirtualFileSystem Files { get; }

    /// <summary>The test terrain's description as a JSON object, to be changed.</summary>
    internal static JsonObject DescriptionObject() => JsonNode.Parse(File.ReadAllText(Description))!.AsObject();

    /// <summary>
    /// Writes the test terrain into <paramref name="folder"/> at <paramref name="at"/>: its description as
    /// <c>&lt;name&gt;.terrain.json</c> (changed by <paramref name="change"/>) and its two images; gives the description's full path.
    /// </summary>
    internal static string Copy(TempFolder folder, string at, string name, Action<JsonObject>? change = null)
    {
        var description = DescriptionObject();
        change?.Invoke(description);
        foreach (var image in new[] { "test-height.png", "test-cover.png" })
            folder.Write($"{at}/{image}", File.ReadAllBytes(Path.Combine(Render3DReferenceTests.TerrainDirectory, image)));
        return folder.Write($"{at}/{name}.terrain.json", description.ToJsonString());
    }

    public void Dispose()
    {
        Files.Dispose();
        _folder.Dispose();
    }
}

/// <summary>
/// Terrain in packs (M9 step 8; Docs/Assets.md, Q26, S8-1, S8-2): the test terrain loads from its
/// description as M6's loader read it, bit for bit; from the pack the terrain importer made, and from
/// its JSON form in a loose folder, it is the description's terrain bit for bit; the importer's rules;
/// the checks at load; the record's fixture.
/// </summary>
public class TerrainPackTests(TestTerrainPack pack) : IClassFixture<TestTerrainPack>
{
    // The SHA-256 of the test terrain's heights (floats, little-endian) and covers as M6's loader gave
    // them (main at 286169c, before the record): the heights are converted at load exactly as before (Q26).
    const string M6HeightsSha256 = "09bfcb4d86c60b84b844d771e8791e544713f7bb2b10a89abad3887aab33083d";
    const string M6CoversSha256 = "88262672117a876692c29b65e3d900e2065e650e1839ca3a430c553023cdaba7";

    static string Sha256(ReadOnlySpan<byte> bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    [Fact]
    public void TheTerrainRecordFixtureIsRecorded() => RecordFixtures.Ensure("terrain", TerrainSamples.Small());

    [Fact]
    public void TheTestTerrainLoadsFromItsDescriptionAsM6sLoaderReadIt()
    {
        var terrain = TerrainData.Load(TestTerrainPack.Description);
        Assert.Equal((64, 48, 0.5f, -1f, 5f, (float?)0.4f), (terrain.Width, terrain.Depth, terrain.CellSize, terrain.HeightMin, terrain.HeightMax, terrain.WaterLevel));
        Assert.Equal(["grass", "rock", "sand"], terrain.CoverSlots);
        Assert.Equal(64 * 48, terrain.HeightSamples.Length);
        Assert.Equal(M6HeightsSha256, Sha256(MemoryMarshal.AsBytes(terrain.Heights.AsSpan())));
        Assert.Equal(M6CoversSha256, Sha256(terrain.Covers));
    }

    [Fact]
    public void TheTestTerrainFromThePackIsTheDescriptionsBitForBit() =>
        RecordAssert.Equal(TerrainData.Load(TestTerrainPack.Description), TerrainData.Load(pack.Files, TestTerrainPack.RecordPath));

    [Fact]
    public void TheTestTerrainFromItsJsonFormIsTheDescriptionsBitForBit()
    {
        // The pack extracted is the loose form a development folder holds: world/terrain/test.json.
        var loose = pack.Scratch("loose");
        using (var file = PackFile.Open(pack.PackFile))
            file.ExtractTo(loose);
        Assert.True(File.Exists(Path.Combine(loose, "world", "terrain", "test.json")));
        using var files = new VirtualFileSystem([Mount.Folder(loose)]);
        Assert.True(files.IsRecord(TestTerrainPack.RecordPath));
        RecordAssert.Equal(TerrainData.Load(TestTerrainPack.Description), TerrainData.Load(files, TestTerrainPack.RecordPath));
    }

    [Fact]
    public void ADescriptionBecomesOneTerrainRecordAtItsPathWithoutTheEndingAndTakesItsImages()
    {
        using var file = PackFile.Open(pack.PackFile);
        // The one-hot image no description names is packed as a file; the two the description names aren't.
        Assert.Equal(["world/terrain/test", "world/terrain/test-cover-onehot.png"], file.Entries.Select(e => e.Path));
        Assert.Equal([true, false], file.Entries.Select(e => e.IsRecord));
        Assert.Equal("terrain", RecordSerializer.RootTypeName(file.Read(file.Entries[0])));
    }

    [Fact]
    public void TheWorldImporterTakesTerrainDescriptions()
    {
        var (importer, ending) = Importers.Engine.For("world/terrain/vale.terrain.json")!.Value;
        Assert.Equal((typeof(TerrainImporter), "world", ".terrain.json"), (importer.Type, importer.Name, ending));
        Assert.Equal([".terrain.json"], importer.Endings);
        Assert.Null(Importers.Engine.For("world/terrain/vale.json"));
        Assert.Null(Importers.Engine.For("world/terrain/vale.terrain"));
        Assert.Null(Importers.Engine.For("world/terrain/.terrain.json"));
    }

    [Fact]
    public void TheImagesAreNamedRelativeToTheDescriptionsFolder()
    {
        using var folder = new TempFolder();
        // One description at the top of the folder packed naming images in a folder below it, one
        // deeper naming its own beside it.
        TestTerrainPack.Copy(folder, "source/maps", "unused");
        File.Delete(folder["source/maps/unused.terrain.json"]);
        folder.Write("source/top.terrain.json", Json(d =>
        {
            d["height"] = "maps/test-height.png";
            d["cover"] = "maps/test-cover.png";
        }));
        TestTerrainPack.Copy(folder, "source/world/vale", "deep");
        var writer = new PackWriter();
        writer.AddFolder(folder["source"]);
        writer.Write(folder["world.pak"]);
        using var file = PackFile.Open(folder["world.pak"]);
        Assert.Equal(["top", "world/vale/deep"], file.Entries.Select(e => e.Path));
        using var files = new VirtualFileSystem([Mount.Pack(folder["world.pak"])]);
        var expected = TerrainData.Load(TestTerrainPack.Description);
        RecordAssert.Equal(expected, TerrainData.Load(files, "top"));
        RecordAssert.Equal(expected, TerrainData.Load(files, "world/vale/deep"));
    }

    static string Json(Action<JsonObject> change)
    {
        var description = TestTerrainPack.DescriptionObject();
        change(description);
        return description.ToJsonString();
    }

    /// <summary>A grey image (8 bits, as ImageFile writes PNGs: RGBA) of the given levels, row by row.</summary>
    static byte[] GreyPng(TempFolder folder, int width, int height, Func<int, byte> level)
    {
        var image = new RgbaImage(width, height, new byte[width * height * 4]);
        for (var i = 0; i < width * height; i++)
            image.Pixels[i * 4] = image.Pixels[i * 4 + 1] = image.Pixels[i * 4 + 2] = level(i);
        for (var i = 0; i < width * height; i++)
            image.Pixels[i * 4 + 3] = 255;
        var file = folder[$"grey-{width}x{height}-{Guid.NewGuid():N}.png"];
        ImageFile.SavePng(file, image);
        var bytes = File.ReadAllBytes(file);
        File.Delete(file);
        return bytes;
    }

    /// <summary>
    /// What makes a description unusable stops the packing with a <see cref="DataFileException"/> naming
    /// the description ("the world importer: …"): a key missing or of the wrong kind, a size that isn't
    /// a whole number, an image the folder hasn't, a name outside the path rule, a file that isn't an
    /// image, an image of another size than the grid (each side alone), text that isn't JSON.
    /// </summary>
    [Fact]
    public void AnUnusableDescriptionStopsThePackingNamingIt()
    {
        using var images = new TempFolder();
        var extra = new Dictionary<string, byte[]>
        {
            ["notes.txt"] = "not an image"u8.ToArray(),
            ["narrow.png"] = GreyPng(images, 63, 48, i => (byte)i),
            ["short.png"] = GreyPng(images, 64, 47, i => (byte)i),
        };
        (string Name, Action<JsonObject> Change, string Problem)[] cases =
        [
            ("no width", d => d.Remove("width"), "the description has no number \"width\""),
            ("depth as text", d => d["depth"] = "48", "the description has no number \"depth\""),
            ("width not whole", d => d["width"] = 64.5, "\"width\" isn't a whole number"),
            ("depth not whole", d => d["depth"] = 1e10, "\"depth\" isn't a whole number"),
            ("no cell size", d => d.Remove("cellSize"), "the description has no number \"cellSize\""),
            ("heightMin null", d => d["heightMin"] = null, "the description has no number \"heightMin\""),
            ("no heightMax", d => d.Remove("heightMax"), "the description has no number \"heightMax\""),
            ("height a number", d => d["height"] = 5, "the description has no string \"height\""),
            ("no cover", d => d.Remove("cover"), "the description has no string \"cover\""),
            ("covers a string", d => d["covers"] = "grass", "the description has no array \"covers\""),
            ("a cover slot a number", d => d["covers"] = new JsonArray("grass", 1), "covers[1] isn't a string"),
            ("a cover slot null", d => d["covers"] = new JsonArray("grass", "rock", null), "covers[2] isn't a string"),
            ("height image missing", d => d["height"] = "missing.png", "No 'world/terrain/missing.png' in the folder being packed"),
            ("cover image outside the folder", d => d["cover"] = "../test-cover.png", "'world/terrain/../test-cover.png' isn't a valid data path"),
            ("height not an image", d => d["height"] = "notes.txt", "the height image notes.txt isn't an image stb_image reads"),
            ("cover not an image", d => d["cover"] = "notes.txt", "the cover image notes.txt isn't an image stb_image reads"),
            ("grid narrower", d => d["width"] = 32, "the height image test-height.png is 64x48, not 32x48"),
            ("grid shorter", d => d["depth"] = 47, "the height image test-height.png is 64x48, not 64x47"),
            ("height image narrower", d => d["height"] = "narrow.png", "the height image narrow.png is 63x48, not 64x48"),
            ("cover image narrower", d => d["cover"] = "narrow.png", "the cover image narrow.png is 63x48, not 64x48"),
            ("cover image shorter", d => d["cover"] = "short.png", "the cover image short.png is 64x47, not 64x48"),
        ];
        var failures = new List<string>();
        foreach (var (name, change, expected) in cases)
        {
            using var folder = new TempFolder();
            var description = TestTerrainPack.Copy(folder, "world/terrain", "broken", change);
            foreach (var (file, bytes) in extra)
                folder.Write("world/terrain/" + file, bytes);
            var problem = Record.Exception(() => new PackWriter().AddFolder(folder.Root));
            if (problem is not DataFileException data || data.File != description || data.Entry != null
                || !data.Problem.StartsWith("the world importer: " + expected, StringComparison.Ordinal))
                failures.Add($"{name}: {problem?.GetType().Name}: {problem?.Message ?? "no error"}");
        }
        using (var folder = new TempFolder())
        {
            var description = folder.Write("world/terrain/broken.terrain.json", "{\"width\": 64,");
            var problem = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder.Root));
            Assert.Equal((description, null), (problem.File, problem.Entry));
            Assert.IsAssignableFrom<JsonException>(problem.InnerException);
        }
        using (var folder = new TempFolder())
        {
            var description = folder.Write("world/terrain/list.terrain.json", "[64, 48]");
            var problem = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder.Root));
            Assert.Equal((description, null, "the world importer: the description isn't a JSON object"), (problem.File, problem.Entry, problem.Problem));
        }
        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void TheDescriptionsLoaderNamesTheDescriptionInItsProblems()
    {
        using var folder = new TempFolder();
        var description = TestTerrainPack.Copy(folder, "world", "broken", d => d["width"] = 32);
        var problem = Assert.Throws<InvalidDataException>(() => TerrainData.Load(description));
        Assert.Equal($"{description}: the height image test-height.png is 64x48, not 32x48", problem.Message);
    }

    /// <summary>
    /// What the description names and stb_image reads is taken as M6's loader took it: an 8-bit or a
    /// colour height image too (widened to 16 bits as v · 257, colours to grey), any other keys ignored, a
    /// water level that is missing or isn't a number meaning no water; the description's own height
    /// and cover sizes need only match the images.
    /// </summary>
    [Fact]
    public void ADescriptionIsReadAsM6sLoaderReadIt()
    {
        using var folder = new TempFolder();
        folder.Write("source/a/h.png", GreyPng(folder, 2, 2, i => new byte[] { 0, 1, 128, 255 }[i]));
        folder.Write("source/a/c.png", GreyPng(folder, 2, 2, i => (byte)(3 - i)));
        folder.Write("source/a/eight.terrain.json",
            """{"height": "h.png", "cover": "c.png", "width": 2, "depth": 2, "cellSize": 2, "heightMin": 0, "heightMax": 1, "covers": ["a"], "waterLevel": "high", "extra": [1]}""");
        folder.Write("source/a/dry.terrain.json",
            """{"height": "h.png", "cover": "c.png", "width": 2, "depth": 2, "cellSize": 2, "heightMin": 0, "heightMax": 1, "covers": []}""");
        var writer = new PackWriter();
        writer.AddFolder(folder["source"]);
        writer.Write(folder["world.pak"]);
        using var files = new VirtualFileSystem([Mount.Pack(folder["world.pak"])]);
        foreach (var name in new[] { "a/eight", "a/dry" })
        {
            var terrain = TerrainData.Load(files, name);
            Assert.Equal(new ushort[] { 0, 257, 32896, 65535 }, terrain.HeightSamples);
            Assert.Equal(new byte[] { 3, 2, 1, 0 }, terrain.Covers);
            Assert.Null(terrain.WaterLevel);
            RecordAssert.Equal(TerrainData.Load(folder[$"source/{name}.terrain.json"]), terrain);
        }
    }

    [Fact]
    public void ATerrainThatBreaksItsRulesIsRefusedAtLoadNamingThePackAndEntry()
    {
        using var folder = new TempFolder();
        var cases = BrokenTerrains();
        var writer = new PackWriter();
        foreach (var (name, terrain, _) in cases)
            writer.AddRecord("world/terrain/" + name, terrain);
        writer.Write(folder["world.pak"]);
        using var files = new VirtualFileSystem([Mount.Pack(folder["world.pak"])]);
        foreach (var (name, _, expected) in cases)
        {
            var problem = Assert.Throws<DataFileException>(() => TerrainData.Load(files, "world/terrain/" + name));
            Assert.Equal((folder["world.pak"], "world/terrain/" + name, "not a valid terrain: " + expected), (problem.File, problem.Entry, problem.Problem));
            Assert.IsType<InvalidDataException>(problem.InnerException);
        }
        // Its JSON form in a loose folder is named as the loose file.
        using (var file = PackFile.Open(folder["world.pak"]))
            file.ExtractTo(folder["loose"]);
        using var loose = new VirtualFileSystem([Mount.Folder(folder["loose"])]);
        foreach (var (name, _, expected) in cases)
        {
            var problem = Assert.Throws<DataFileException>(() => TerrainData.Load(loose, "world/terrain/" + name));
            Assert.Equal((Path.Combine(folder["loose"], "world", "terrain", name + ".json"), (string?)null, "not a valid terrain: " + expected),
                (problem.File, problem.Entry, problem.Problem));
        }
    }

    internal static List<(string Name, TerrainData Terrain, string Problem)> BrokenTerrains()
    {
        static TerrainData Grid(int width, int depth, int samples, int covers) => new()
        {
            Width = width, Depth = depth, CellSize = 1, HeightMin = 0, HeightMax = 1,
            HeightSamples = new ushort[samples], Covers = new byte[covers], CoverSlots = ["grass"],
        };
        return
        [
            ("no-columns", Grid(0, 1, 0, 0), "the grid is 0x1 points"),
            ("no-rows", Grid(1, 0, 0, 0), "the grid is 1x0 points"),
            ("negative", Grid(-1, -1, 1, 1), "the grid is -1x-1 points"),
            ("samples", Grid(2, 2, 3, 4), "3 height samples for 2x2 points"),
            ("covers", Grid(2, 2, 4, 5), "5 covers for 2x2 points"),
            // 65536 × 65537 points wrap to 65536 in 32 bits.
            ("overflow", Grid(65536, 65537, 65536, 65536), "65536 height samples for 65536x65537 points"),
        ];
    }

    [Fact]
    public void TheSmallestTerrainsAndTheFixturesAreValid()
    {
        new TerrainData { Width = 1, Depth = 1, CellSize = 1, HeightMin = 0, HeightMax = 1, HeightSamples = [7], Covers = [0], CoverSlots = ["grass"] }.Validate();
        new TerrainData { Width = 1, Depth = 3, CellSize = 1, HeightMin = 0, HeightMax = 1, HeightSamples = [1, 2, 3], Covers = [0, 0, 0], CoverSlots = [] }.Validate();
        TerrainSamples.Small().Validate();
        TerrainData.Load(TestTerrainPack.Description).Validate();
    }

    [Fact]
    public void AnEntryThatIsntATerrainIsRefusedNamingIt()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.Add("world/notes", "not a terrain"u8);
        writer.AddRecord("world/point", new Point(1));
        writer.Write(folder["world.pak"]);
        using var files = new VirtualFileSystem([Mount.Pack(folder["world.pak"])]);
        var file = Assert.Throws<DataFileException>(() => TerrainData.Load(files, "world/notes"));
        Assert.Equal(("world/notes", "a file, not a record (expected a terrain)"), (file.Entry, file.Problem));
        var other = Assert.Throws<DataFileException>(() => TerrainData.Load(files, "world/point"));
        Assert.Equal("a record of type test.point, not terrain", other.Problem);
        Assert.Throws<FileNotFoundException>(() => TerrainData.Load(files, "world/missing"));
    }

    [Fact]
    public void TheSmallTerrainComesBackFromBothFormsWithAndWithoutWater()
    {
        foreach (var terrain in new[] { TerrainSamples.Small(), TerrainSamples.Small(water: false) })
        {
            RecordAssert.Equal(terrain, RecordSerializer.FromBinary<TerrainData>(RecordSerializer.ToBinary(terrain)));
            RecordAssert.Equal(terrain, RecordSerializer.FromJson<TerrainData>(RecordSerializer.ToJson(terrain)));
        }
        Assert.DoesNotContain("waterLevel", RecordSerializer.ToJson(TerrainSamples.Small(water: false)));
    }
}

/// <summary>A small, valid terrain with a value in every field (the fixture's).</summary>
public static class TerrainSamples
{
    public static TerrainData Small(bool water = true) => new()
    {
        Width = 3,
        Depth = 2,
        CellSize = 0.5f,
        HeightMin = -1,
        HeightMax = 5,
        HeightSamples = [0, 65535, 1, 32768, 12345, 65534],
        Covers = [0, 1, 2, 1, 0, 2],
        CoverSlots = ["grass", "rock", "sand"],
        WaterLevel = water ? 0.4f : null,
    };
}
