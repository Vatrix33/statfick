using System.Numerics;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;

namespace Statventure.Tests.Render3D;

/// <summary>
/// The test models (Data/Models/*.glb) packed as the packer packs a folder (Docs/Assets.md, "How it
/// works", "Models and state machines in packs"): copied to <c>models/</c> in a temporary folder, which
/// goes through the engine's importers into a models.pak there, mounted.
/// </summary>
public sealed class TestModelPack : IDisposable
{
    readonly TempFolder _folder = new();

    public TestModelPack()
    {
        foreach (var file in Directory.GetFiles(Render3DReferenceTests.ModelDirectory, "*.glb"))
            _folder.Write("source/models/" + Path.GetFileName(file), File.ReadAllBytes(file));
        var writer = new PackWriter();
        writer.AddFolder(_folder["source"]);
        writer.Write(PackFile);
        Files = new VirtualFileSystem([Mount.Pack(PackFile)]);
    }

    /// <summary>The test models' names (<c>figure</c> for figure.glb), in ordinal order.</summary>
    public static IReadOnlyList<string> Names { get; } =
        [.. Directory.GetFiles(Render3DReferenceTests.ModelDirectory, "*.glb").Select(f => Path.GetFileNameWithoutExtension(f)).Order(StringComparer.Ordinal)];

    public string PackFile => _folder["models.pak"];

    /// <summary>A folder of its own beside the pack.</summary>
    public string Scratch(string name) => _folder[name];

    public VirtualFileSystem Files { get; }

    public static string SourceOf(string name) => Path.Combine(Render3DReferenceTests.ModelDirectory, name + ".glb");

    public void Dispose()
    {
        Files.Dispose();
        _folder.Dispose();
    }
}

/// <summary>
/// Models in packs (M9 step 5; Docs/Assets.md, Q20): every test model packed by the models importer
/// loads from the pack, and from its JSON form in a loose folder, as <see cref="GltfImport"/> reads
/// the .glb, bit for bit; the importer's rules; the model record's fixture.
/// </summary>
public class ModelPackTests(TestModelPack pack) : IClassFixture<TestModelPack>
{
    [Fact]
    public void TheModelRecordFixtureIsRecorded() => RecordFixtures.Ensure("model", ModelRecordSamples.Small());

    [Fact]
    public void EachGlbBecomesOneModelRecordAtItsPathWithoutTheEnding()
    {
        using var file = PackFile.Open(pack.PackFile);
        Assert.Equal(TestModelPack.Names.Select(n => "models/" + n), file.Entries.Select(e => e.Path));
        Assert.All(file.Entries, e => Assert.True(e.IsRecord, $"{e.Path} isn't a record"));
        Assert.All(file.Entries, e => Assert.Equal("model", Statventure.Records.RecordSerializer.RootTypeName(file.Read(e))));
        Assert.Equal(12, TestModelPack.Names.Count);
    }

    [Fact]
    public void EveryTestModelFromThePackIsWhatGltfImportReadsBitForBit()
    {
        foreach (var name in TestModelPack.Names)
        {
            var expected = GltfImport.Load(TestModelPack.SourceOf(name));
            RecordAssert.Equal(expected, ModelData.Load(pack.Files, "models/" + name));
            Assert.Equal(expected.Animations.Count, pack.Files.Load<ModelData>("models/" + name).Animations.Count);
        }
    }

    [Fact]
    public void EveryTestModelFromItsJsonFormIsWhatGltfImportReadsBitForBit()
    {
        // The pack extracted is the loose form a development folder holds: models/<name>.json.
        var loose = pack.Scratch("loose");
        using (var file = PackFile.Open(pack.PackFile))
            file.ExtractTo(loose);
        Assert.Equal(TestModelPack.Names.Select(n => n + ".json"), Directory.GetFiles(Path.Combine(loose, "models")).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        using var files = new VirtualFileSystem([Mount.Folder(loose)]);
        foreach (var name in TestModelPack.Names)
        {
            Assert.True(files.IsRecord("models/" + name));
            RecordAssert.Equal(GltfImport.Load(TestModelPack.SourceOf(name)), ModelData.Load(files, "models/" + name));
        }
    }

    [Fact]
    public void TheModelsImporterTakesGlbFiles()
    {
        var (importer, ending) = Importers.Engine.For("models/figure.glb")!.Value;
        Assert.Equal((typeof(ModelImporter), "models", ".glb"), (importer.Type, importer.Name, ending));
        Assert.Equal([".glb"], importer.Endings);
        Assert.Null(Importers.Engine.For("models/figure.gltf"));
        Assert.Null(Importers.Engine.For("models/figure.bin"));
    }

    [Fact]
    public void AGlbTheImporterCantReadFailsThePackNamingIt()
    {
        using var folder = new TempFolder();
        var good = File.ReadAllBytes(TestModelPack.SourceOf("sword"));
        folder.Write("models/sword.glb", good);
        var broken = folder.Write("models/broken.glb", good[..(good.Length / 2)]);
        var problem = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder.Root));
        Assert.Equal(broken, problem.File);
        Assert.Null(problem.Entry);
        Assert.StartsWith("the models importer: ", problem.Problem);
    }

    [Fact]
    public void AnEntryThatIsntAModelIsRefusedNamingIt()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.Add("models/notes", "not a model"u8);
        writer.AddRecord("models/clip", new ModelAnimationEvent("hit", 0.5));
        writer.Write(folder["models.pak"]);
        using var files = new VirtualFileSystem([Mount.Pack(folder["models.pak"])]);
        var file = Assert.Throws<DataFileException>(() => ModelData.Load(files, "models/notes"));
        Assert.Equal(("models/notes", "a file, not a record (expected a model)"), (file.Entry, file.Problem));
        var other = Assert.Throws<DataFileException>(() => ModelData.Load(files, "models/clip"));
        Assert.Equal("a record of type model.event, not model", other.Problem);
        Assert.Throws<FileNotFoundException>(() => ModelData.Load(files, "models/missing"));
    }

    [Fact]
    public void TheSmallModelComesBackFromBothForms()
    {
        var model = ModelRecordSamples.Small();
        RecordAssert.Equal(model, Statventure.Records.RecordSerializer.FromBinary<ModelData>(Statventure.Records.RecordSerializer.ToBinary(model)));
        RecordAssert.Equal(model, Statventure.Records.RecordSerializer.FromJson<ModelData>(Statventure.Records.RecordSerializer.ToJson(model)));
    }
}

/// <summary>
/// A Blender scene drawn from pack-loaded models (<see cref="Model.Load(Statventure.Graphics.GpuDevice, VirtualFileSystem, string)"/>)
/// matches its golden render as the path-loaded models do (<see cref="Render3DReferenceTests.TheEngineDrawsWhatBlenderDrew"/>).
/// </summary>
[Collection("GPU")]
public class ModelPackRenderTests(GpuFixture gpu, TestModelPack pack) : IClassFixture<TestModelPack>
{
    /// <summary>Between them nine of the twelve test models: skinned, posed and geared (gear-swing), scenery with shadows (shadow-stage), glow (glow-figure), a hierarchy with mirrored and double-sided parts (model-blocks).</summary>
    [Theory]
    [InlineData("gear-swing")]
    [InlineData("shadow-stage")]
    [InlineData("glow-figure")]
    [InlineData("model-blocks")]
    public void ASceneOfPackLoadedModelsIsWhatBlenderDrew(string name)
    {
        var scene = Scenes3D.All.Single(s => s.Name == name);
        Assert.Null(scene.Terrain);
        var unlined = scene with { Style = scene.Style with { OutlineWidth = 0, InnerWidth = 0 } };
        var actual = Render(unlined, shadows: true);
        var expected = ImageFile.Load(Golden.GoldenPath("r3d-" + name));
        var unshadowed = scene.Models.Any(m => m.Scenery) ? Render(unlined, shadows: false) : null;
        var problems = unshadowed != null && !unshadowed.Pixels.AsSpan().SequenceEqual(actual.Pixels)
            ? Render3DReferenceTests.CompareShadowed(expected, actual, unshadowed)
            : Render3DReferenceTests.Compare(expected, actual);
        if (problems.Length > 0)
        {
            var actualPath = Golden.ActualPath("r3d-" + name + "-packed");
            ImageFile.SavePng(actualPath, actual);
            Assert.Fail($"{name}: {problems}; render in {actualPath}");
        }
    }

    /// <summary>As <see cref="Render3DReferenceTests.Render"/>, the models loaded from the pack (<c>models/&lt;name&gt;</c>).</summary>
    RgbaImage Render(Scene3D scene, bool shadows)
    {
        var device = gpu.Device;
        var meshes = new Dictionary<Shape3D, Mesh>();
        var models = new List<Model>();
        try
        {
            var items = scene.Objects.Select(o =>
            {
                if (!meshes.TryGetValue(o.Shape, out var mesh))
                    meshes[o.Shape] = mesh = Mesh.FromTriangles(device, o.Shape.Positions, o.Shape.Triangles);
                return new DrawItem(mesh, o.Model, Colors.LinearFromHex(o.Color));
            }).ToList();
            var palette = new Palette(scene.Palette, scene.Shadows);
            var groups = new Dictionary<string, (object Owner, Vector3 Anchor)>();
            foreach (var placed in scene.Models)
            {
                var model = Model.Load(device, pack.Files, "models/" + Path.GetFileNameWithoutExtension(placed.File));
                models.Add(model);
                (object Owner, Vector3 Anchor)? group = placed.Group == null ? null
                    : groups.TryGetValue(placed.Group, out var known) ? known : groups[placed.Group] = (new object(), placed.Model.Translation);
                model.Draw(items, placed.Model, palette, scene.Style, placed.Scenery, placed.PoseOf(model.Data), group?.Owner, placed.World?.Invoke(model.Data), group?.Anchor);
            }
            if (!shadows)
                items = items.Select(i => i with { ReceivesShadows = false }).ToList();
            using var target = new RenderTarget3D(device, scene.Width, scene.Height);
            var renderer = new Renderer3D(device);
            var frame = device.BeginFrame();
            renderer.Render(frame, target, scene.Camera.ToCamera(), items, Colors.LinearFromHex(scene.Background), scene.Sun ?? default, scene.Style, scene.Scale, scene.Time,
                scene.ParticlesNow());
            device.EndFrame(frame);
            device.WaitIdle();
            return new RgbaImage(scene.Width, scene.Height, target.Resolved.ReadPixels());
        }
        finally
        {
            foreach (var mesh in meshes.Values)
                mesh.Dispose();
            foreach (var model in models)
                model.Dispose();
        }
    }
}

/// <summary>A small, valid model with a value in every field of every model record type (the fixture's).</summary>
public static class ModelRecordSamples
{
    public static ModelData Small()
    {
        var rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 0.5f);
        var local = Matrix4x4.CreateScale(1, 2, 1) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(0.25f, 1.5f, -0f);
        return new ModelData
        {
            Nodes =
            [
                new ModelNode("Rig", -1, [1, 2], local, new(0.25f, 1.5f, -0f), rotation, new(1, 2, 1), -1, -1),
                new ModelNode("Bone", 0, [], Matrix4x4.CreateTranslation(0, 0.75f, 0), new(0, 0.75f, 0), Quaternion.Identity, Vector3.One, -1, -1),
                new ModelNode("Body", 0, [], Matrix4x4.Identity, Vector3.Zero, Quaternion.Identity, Vector3.One, 0, 0) { Ink = false },
            ],
            Meshes =
            [
                new ModelMesh("Body",
                [
                    new ModelPrimitive
                    {
                        Positions = [new(0, 0, 0), new(1, 0, 0), new(0, 1, 1e-40f)],
                        Normals = [Vector3.UnitZ, Vector3.UnitZ, new(0, 0.6f, 0.8f)],
                        TexCoords = [new(0, 0), new(1, 0), new(0, 1)],
                        Joints = [new(0, 0, 0, 0), new(0, 1, 0, 0), new(1, 0, 0, 1)],
                        Weights = [new(1, 0, 0, 0), new(0.5f, 0.5f, 0, 0), new(0.25f, 0.25f, 0.25f, 0.25f)],
                        Indices = [0, 1, 2],
                        Material = 0,
                    },
                    new ModelPrimitive { Positions = [new(2, 0, 0), new(3, 0, 0), new(2, 1, 0)], Normals = [-Vector3.UnitZ, -Vector3.UnitZ, -Vector3.UnitZ], Indices = [0, 2, 1] },
                ]),
            ],
            Materials =
            [
                new ModelMaterial("cloth.001", new(0.5f, 0.25f, 0.125f, 1), true) { Emission = new(0.75f, 0.5f, 0), Unlit = false, Hatch = false },
                new ModelMaterial("glow", new(0, 0, 0, 1), false) { Unlit = true },
            ],
            Skins = [new ModelSkin("Armature", [0, 1], [Matrix4x4.Identity, Matrix4x4.CreateTranslation(0, -0.75f, 0)], 0)],
            Animations =
            [
                new ModelAnimation("swing",
                [
                    new ModelAnimationChannel(1, AnimationPath.Translation, AnimationInterpolation.Linear, [0, 0.5f], [0, 0.75f, 0, 0, 1, 0]),
                    new ModelAnimationChannel(1, AnimationPath.Rotation, AnimationInterpolation.CubicSpline, [0, 0.5f],
                        [0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0.1f, 0, 0, 0, 0, 0, 0, 0, 0.2f, 0.98f, 0, 0, 0, 0]),
                    new ModelAnimationChannel(0, AnimationPath.Scale, AnimationInterpolation.Step, [0.25f], [1, 1.5f, 1]),
                ])
                {
                    Events = [new("hit", 0.25), new("end", 0.5)],
                },
                new ModelAnimation("rest", []),
            ],
            Roots = [0],
        };
    }
}
