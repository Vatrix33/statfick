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

    /// <summary>The fixture of <c>model.animation</c> version 2 (GM2-P22): a clip measured by a chain of joints.</summary>
    [Fact]
    public void TheMeasuredClipsFixtureIsRecorded() => RecordFixtures.Ensure("clips", ModelRecordSamples.Measured());

    /// <summary>The fixture of <c>model.animation</c> version 3 (GM2-P37): a clip keeping its contacts (<c>model.contacts</c>, <c>model.chain</c>).</summary>
    [Fact]
    public void TheKeptClipsFixtureIsRecorded() => RecordFixtures.Ensure("contacts", ModelRecordSamples.Kept());

    /// <summary>The clips fixture, written at <c>model.animation</c> version 2 (GM2-P22), reads upgraded to version 3 as the measured clips, their measures kept and no contacts.</summary>
    [Theory]
    [InlineData(".rec")]
    [InlineData(".json")]
    public void TheVersion2ClipsFixtureReadsAsTheMeasuredClipsWithoutContacts(string form)
    {
        var (value, table) = RecordFixtures.Load("model", "clips", form);
        Assert.Equal(2, table.First(e => e.Name == "model.animation").Version);
        var model = Assert.IsType<ModelData>(value);
        RecordAssert.Equal(ModelRecordSamples.Measured(), model);
        Assert.Contains(model.Animations, a => a.Measure != null);
        Assert.All(model.Animations, a => Assert.Null(a.Contacts));
    }

    [Fact]
    public void EachGlbBecomesOneModelRecordAtItsPathWithoutTheEnding()
    {
        using var file = PackFile.Open(pack.PackFile);
        Assert.Equal(TestModelPack.Names.Select(n => "models/" + n), file.Entries.Select(e => e.Path));
        Assert.All(file.Entries, e => Assert.True(e.IsRecord, $"{e.Path} isn't a record"));
        Assert.All(file.Entries, e => Assert.Equal("model", Statventure.Records.RecordSerializer.RootTypeName(file.Read(e))));
        Assert.Equal(16, TestModelPack.Names.Count);
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
        Assert.Equal(TestModelPack.Names.Select(n => n + ".json").Order(StringComparer.Ordinal), Directory.GetFiles(Path.Combine(loose, "models")).Select(Path.GetFileName).Order(StringComparer.Ordinal));
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
        foreach (var model in new[] { ModelRecordSamples.Small(), ModelRecordSamples.Measured(), ModelRecordSamples.Kept() })
        {
            RecordAssert.Equal(model, Statventure.Records.RecordSerializer.FromBinary<ModelData>(Statventure.Records.RecordSerializer.ToBinary(model)));
            RecordAssert.Equal(model, Statventure.Records.RecordSerializer.FromJson<ModelData>(Statventure.Records.RecordSerializer.ToJson(model)));
        }
        Assert.Equal(["Rig", "Bone"], Statventure.Records.RecordSerializer.FromJson<ModelData>(Statventure.Records.RecordSerializer.ToJson(ModelRecordSamples.Measured())).Animations[0].Measure);
        var kept = Statventure.Records.RecordSerializer.FromBinary<ModelData>(Statventure.Records.RecordSerializer.ToBinary(ModelRecordSamples.Kept())).Animations[0].Contacts!;
        Assert.Equal(("models/body", "Bone", "Rig Bone Body"), (kept.KeyedOn, kept.Root, string.Join(" ", kept.Chains[0].Joints)));
    }

    /// <summary>
    /// A clip's contacts (GM2-P37) come from its animation's extras ("contacts": the body it was keyed on, the root and the
    /// chains, as the family's clip library writes them): the library's whole-body clips keep them, its stance loops and
    /// the test library's clip don't, a test model's clips don't; anything but that object there is refused, naming the file.
    /// </summary>
    [Fact]
    public void AClipsContactsAreReadFromItsExtras()
    {
        var library = GltfImport.Load(Humanoid.HumanoidFiles.Glb("clips"));
        foreach (var animation in library.Animations)
        {
            var stance = animation.Name.StartsWith("hold-", StringComparison.Ordinal);
            Assert.True(stance == (animation.Contacts == null), $"{animation.Name}: contacts {animation.Contacts}");
            if (animation.Contacts is { } contacts)
                Assert.Equal(("models/humanoid/bodies/human-male-average", "hips", "thigh.L shin.L foot.L"), (contacts.KeyedOn, contacts.Root, string.Join(" ", contacts.Chains[0].Joints)));
        }
        Assert.All(GltfImport.Load(Humanoid.HumanoidFiles.TestClips).Animations, a => Assert.Null(a.Contacts));
        Assert.All(GltfImport.Load(TestModelPack.SourceOf("figure")).Animations, a => Assert.Null(a.Contacts));
        using var folder = new TempFolder();
        var source = File.ReadAllBytes(Humanoid.HumanoidFiles.TestClips);
        foreach (var (what, contacts) in new[]
        {
            ("a list", "[\"hips\"]"),
            ("a key too many", "{\"keyedOn\": \"models/a\", \"root\": \"hips\", \"chains\": [], \"hands\": []}"),
            ("no root", "{\"keyedOn\": \"models/a\", \"chains\": []}"),
            ("a root that isn't a name", "{\"keyedOn\": \"models/a\", \"root\": 3, \"chains\": []}"),
            ("a chain that isn't a list", "{\"keyedOn\": \"models/a\", \"root\": \"hips\", \"chains\": [\"thigh.L\"]}"),
            ("a number among a chain's names", "{\"keyedOn\": \"models/a\", \"root\": \"hips\", \"chains\": [[\"thigh.L\", 2, \"foot.L\"]]}"),
        })
        {
            var path = folder.Write($"broken-{what.Replace(' ', '-').Replace("'", "")}.glb", WithExtra(source, "contacts", contacts));
            var problem = Assert.Throws<InvalidDataException>(() => GltfImport.Load(path));
            Assert.Equal($"{path}: animation 0's contacts aren't {{\"keyedOn\": a model, \"root\": a joint, \"chains\": lists of joints' names}}", problem.Message);
        }
        var fine = folder.Write("kept.glb", WithExtra(source, "contacts", "{\"root\": \"hips\", \"chains\": [[\"thigh.R\", \"shin.R\", \"foot.R\"]], \"keyedOn\": \"models/b\"}"));
        var read = GltfImport.Load(fine).Animations[0].Contacts!;
        Assert.Equal(("models/b", "hips", "thigh.R shin.R foot.R"), (read.KeyedOn, read.Root, string.Join(" ", read.Chains.Single().Joints)));
    }

    /// <summary>
    /// A clip's measure (GM2-P22) comes from its animation's extras ("measure": the joints' names, as the family's clip
    /// library writes it), the family's library and its test library measured by the left leg; a model's other clips
    /// (no measure in their extras) have none; anything but a list of names there is refused, naming the file.
    /// </summary>
    [Fact]
    public void AClipsMeasureIsReadFromItsExtras()
    {
        string[] leg = ["thigh.L", "shin.L", "foot.L"];
        foreach (var library in new[] { Humanoid.HumanoidFiles.Glb("clips"), Humanoid.HumanoidFiles.TestClips })
        {
            var model = GltfImport.Load(library);
            Assert.NotEmpty(model.Animations);
            Assert.All(model.Animations, a => Assert.Equal(leg, a.Measure));
        }
        Assert.All(GltfImport.Load(TestModelPack.SourceOf("figure")).Animations, a => Assert.Empty(a.Measure));
        using var folder = new TempFolder();
        foreach (var (what, measure) in new[] { ("a name", "\"thigh.L\""), ("a number among the names", "[\"thigh.L\", 3]"), ("an object", "{\"chain\": []}") })
        {
            var path = folder.Write($"broken-{what.Replace(' ', '-')}.glb", WithMeasure(File.ReadAllBytes(Humanoid.HumanoidFiles.TestClips), measure));
            var problem = Assert.Throws<InvalidDataException>(() => GltfImport.Load(path));
            Assert.Equal($"{path}: animation 0's measure isn't a list of joints' names", problem.Message);
        }
        var fine = folder.Write("listed.glb", WithMeasure(File.ReadAllBytes(Humanoid.HumanoidFiles.TestClips), "[\"thigh.R\", \"shin.R\"]"));
        Assert.Equal(["thigh.R", "shin.R"], GltfImport.Load(fine).Animations[0].Measure);
    }

    /// <summary>A .glb with its first animation's measure in its extras replaced by <paramref name="json"/> (the JSON chunk rewritten, padded).</summary>
    static byte[] WithMeasure(byte[] glb, string json) => WithExtra(glb, "measure", json);

    /// <summary>A .glb with its first animation's extra <paramref name="key"/> set to <paramref name="json"/> (the JSON chunk rewritten, padded).</summary>
    static byte[] WithExtra(byte[] glb, string key, string json)
    {
        var length = (int)BitConverter.ToUInt32(glb, 12);
        var root = System.Text.Json.Nodes.JsonNode.Parse(glb.AsSpan(20, length))!;
        root["animations"]![0]!["extras"]![key] = System.Text.Json.Nodes.JsonNode.Parse(json);
        var text = System.Text.Encoding.UTF8.GetBytes(root.ToJsonString());
        var padded = (text.Length + 3) / 4 * 4;
        var chunk = new byte[padded];
        Array.Fill(chunk, (byte)' ');
        text.CopyTo(chunk, 0);
        var rest = glb.AsSpan(20 + length);
        var output = new byte[12 + 8 + padded + rest.Length];
        glb.AsSpan(0, 12).CopyTo(output);
        BitConverter.GetBytes((uint)output.Length).CopyTo(output, 8);
        BitConverter.GetBytes((uint)padded).CopyTo(output, 12);
        glb.AsSpan(16, 4).CopyTo(output.AsSpan(16));
        chunk.CopyTo(output, 20);
        rest.CopyTo(output.AsSpan(20 + padded));
        return output;
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
            using var renderer = new Renderer3D(device);
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

    /// <summary>
    /// The small model with its swing measured and keeping its contacts (<c>model.animation</c> version 3, GM2-P37): keyed on
    /// a body by its path, carried by the bone, one chain from the rig to the body's node, whose turns the swing keys too.
    /// </summary>
    public static ModelData Kept()
    {
        var measured = Measured();
        var swing = measured.Animations[0];
        return new ModelData
        {
            Nodes = measured.Nodes, Meshes = measured.Meshes, Materials = measured.Materials, Skins = measured.Skins, Roots = measured.Roots,
            Animations =
            [
                swing with
                {
                    Channels = [.. swing.Channels,
                        new ModelAnimationChannel(0, AnimationPath.Rotation, AnimationInterpolation.Linear, [0, 0.5f], [0, 0, 0, 1, 0, 0.1f, 0, 0.995f]),
                        new ModelAnimationChannel(2, AnimationPath.Rotation, AnimationInterpolation.Step, [0], [0, 0, 0, 1])],
                    Contacts = new ModelContacts("models/body", "Bone", [new(["Rig", "Bone", "Body"])]),
                },
                measured.Animations[1],
            ],
        };
    }

    /// <summary>The small model with its swing measured by the chain Rig → Bone (<c>model.animation</c> version 2, GM2-P22).</summary>
    public static ModelData Measured()
    {
        var small = Small();
        return new ModelData
        {
            Nodes = small.Nodes, Meshes = small.Meshes, Materials = small.Materials, Skins = small.Skins, Roots = small.Roots,
            Animations = [small.Animations[0] with { Measure = ["Rig", "Bone"] }, small.Animations[1]],
        };
    }
}
