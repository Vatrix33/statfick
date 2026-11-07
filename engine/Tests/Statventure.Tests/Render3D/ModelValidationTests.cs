using System.Numerics;
using System.Text;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;

namespace Statventure.Tests.Render3D;

/// <summary>
/// Models from the game's data are checked when loaded (Docs/Assets.md, S5-1): what GltfImport checks
/// and the indices it leaves to BRUTAL. Each rule broken once in the small model gives its own problem;
/// through <see cref="ModelData.Load(VirtualFileSystem, string)"/> (and so <see cref="Model.Load(Statventure.Graphics.GpuDevice, VirtualFileSystem, string)"/>)
/// a <see cref="DataFileException"/> naming the pack and entry or the loose file; the importer checks
/// as well, so no pack holds a model that won't load.
/// </summary>
public class ModelValidationTests
{
    static readonly ModelData Small = ModelRecordSamples.Small();

    static ModelData With(IReadOnlyList<ModelNode>? nodes = null, IReadOnlyList<ModelMesh>? meshes = null, IReadOnlyList<ModelSkin>? skins = null,
        IReadOnlyList<ModelAnimation>? animations = null, IReadOnlyList<int>? roots = null) => new()
    {
        Nodes = nodes ?? Small.Nodes,
        Meshes = meshes ?? Small.Meshes,
        Materials = Small.Materials,
        Skins = skins ?? Small.Skins,
        Animations = animations ?? Small.Animations,
        Roots = roots ?? Small.Roots,
    };

    static ModelData Node(int index, Func<ModelNode, ModelNode> change) => With(nodes: [.. Small.Nodes.Select((n, i) => i == index ? change(n) : n)]);

    /// <summary>The small model with its first primitive changed (null: as it is).</summary>
    static ModelData Primitive(Vector3[]? normals = null, Vector2[]? texCoords = null, JointIndices[]? joints = null, Vector4[]? weights = null,
        uint[]? indices = null, int? material = null)
    {
        var p = Small.Meshes[0].Primitives[0];
        var changed = new ModelPrimitive
        {
            Positions = p.Positions,
            Normals = normals ?? p.Normals,
            TexCoords = texCoords ?? p.TexCoords,
            Joints = joints ?? p.Joints,
            Weights = weights ?? p.Weights,
            Indices = indices ?? p.Indices,
            Material = material ?? p.Material,
        };
        return With(meshes: [Small.Meshes[0] with { Primitives = [changed, Small.Meshes[0].Primitives[1]] }]);
    }

    static ModelData Skin(Func<ModelSkin, ModelSkin> change) => With(skins: [change(Small.Skins[0])]);

    static ModelData Channel(int index, Func<ModelAnimationChannel, ModelAnimationChannel> change) =>
        With(animations: [Small.Animations[0] with { Channels = [.. Small.Animations[0].Channels.Select((c, i) => i == index ? change(c) : c)] }, Small.Animations[1]]);

    static readonly Dictionary<string, (Func<ModelData> Model, string Problem, string Path)> Broken = new()
    {
        ["a child that isn't a node"] = (() => Node(0, n => n with { Children = [1, 2, 3] }), "node 0 (Rig) has a child 3 that doesn't exist", "$.nodes[0].children[2]"),
        ["a negative child"] = (() => Node(0, n => n with { Children = [1, 2, -1] }), "node 0 (Rig) has a child -1 that doesn't exist", "$.nodes[0].children[2]"),
        ["its own child"] = (() => Node(0, n => n with { Children = [0, 1, 2] }), "node 0 (Rig) is its own child or the child of two nodes", "$.nodes[0].children[0]"),
        ["the child of two nodes"] = (() => Node(1, n => n with { Children = [2] }), "node 2 (Body) is its own child or the child of two nodes", "$.nodes[1].children[0]"),
        ["a parent that doesn't list it"] = (() => Node(1, n => n with { Parent = 2 }), "node 1 (Bone) has the parent 2, but node 0 lists it as a child", "$.nodes[1].parent"),
        ["a parent no node agrees with"] = (() => Node(0, n => n with { Children = [1] }), "node 2 (Body) has the parent 0, but no node lists it as a child", "$.nodes[2].parent"),
        ["a mesh that doesn't exist"] = (() => Node(2, n => n with { Mesh = 1 }), "node 2 (Body) shows mesh 1, which doesn't exist", "$.nodes[2].mesh"),
        ["a mesh below -1"] = (() => Node(2, n => n with { Mesh = -2 }), "node 2 (Body) shows mesh -2, which doesn't exist", "$.nodes[2].mesh"),
        ["a skin that doesn't exist"] = (() => Node(2, n => n with { Skin = 1 }), "node 2 (Body) has skin 1, which doesn't exist", "$.nodes[2].skin"),
        ["a skin below -1"] = (() => Node(2, n => n with { Skin = -2 }), "node 2 (Body) has skin -2, which doesn't exist", "$.nodes[2].skin"),
        ["a root that isn't a node"] = (() => With(roots: [0, 3]), "the root 3 isn't a node without a parent", "$.roots[1]"),
        ["a negative root"] = (() => With(roots: [-1]), "the root -1 isn't a node without a parent", "$.roots[0]"),
        ["a root with a parent"] = (() => With(roots: [0, 1]), "the root 1 isn't a node without a parent", "$.roots[1]"),
        ["normals for other vertices"] = (() => Primitive(normals: [Vector3.UnitZ, Vector3.UnitZ]), "mesh 0 (Body), primitive 0 has 2 normals for 3 vertices", "$.meshes[0].primitives[0].normals"),
        ["texture coordinates for other vertices"] = (() => Primitive(texCoords: new Vector2[4]), "mesh 0 (Body), primitive 0 has 4 texCoords for 3 vertices", "$.meshes[0].primitives[0].texCoords"),
        ["joints for other vertices"] = (() => Primitive(joints: new JointIndices[2]), "mesh 0 (Body), primitive 0 has 2 joints for 3 vertices", "$.meshes[0].primitives[0].joints"),
        ["weights for other vertices"] = (() => Primitive(weights: new Vector4[1]), "mesh 0 (Body), primitive 0 has 1 weights for 3 vertices", "$.meshes[0].primitives[0].weights"),
        ["a triangle cut short"] = (() => Primitive(indices: [0, 1, 2, 0]), "mesh 0 (Body), primitive 0 has 4 indices, not whole triangles", "$.meshes[0].primitives[0].indices"),
        ["an index past the vertices"] = (() => Primitive(indices: [0, 1, 3]), "mesh 0 (Body), primitive 0 has the index 3 beyond its 3 vertices", "$.meshes[0].primitives[0].indices[2]"),
        ["a material that doesn't exist"] = (() => Primitive(material: 2), "mesh 0 (Body), primitive 0 has material 2, which doesn't exist", "$.meshes[0].primitives[0].material"),
        ["a material below -1"] = (() => Primitive(material: -2), "mesh 0 (Body), primitive 0 has material -2, which doesn't exist", "$.meshes[0].primitives[0].material"),
        ["joint index 2 first"] = (() => Primitive(joints: [new(2, 0, 0, 0), default, default]), "node 2 (Body) shows mesh 0, whose primitive 0 has the joint index 2 beyond its skin's 2 joints", "$.meshes[0].primitives[0].joints[0]"),
        ["joint index 2 second"] = (() => Primitive(joints: [default, new(0, 2, 0, 0), default]), "node 2 (Body) shows mesh 0, whose primitive 0 has the joint index 2 beyond its skin's 2 joints", "$.meshes[0].primitives[0].joints[1]"),
        ["joint index 7 third"] = (() => Primitive(joints: [default, default, new(0, 0, 7, 0)]), "node 2 (Body) shows mesh 0, whose primitive 0 has the joint index 7 beyond its skin's 2 joints", "$.meshes[0].primitives[0].joints[2]"),
        ["joint index 2 fourth"] = (() => Primitive(joints: [default, default, new(0, 0, 0, 2)]), "node 2 (Body) shows mesh 0, whose primitive 0 has the joint index 2 beyond its skin's 2 joints", "$.meshes[0].primitives[0].joints[2]"),
        ["a joint that isn't a node"] = (() => Skin(s => s with { Joints = [0, 3] }), "skin 0 (Armature) has the joint 3, which isn't a node", "$.skins[0].joints[1]"),
        ["a negative joint"] = (() => Skin(s => s with { Joints = [-1, 1] }), "skin 0 (Armature) has the joint -1, which isn't a node", "$.skins[0].joints[0]"),
        ["too few inverse bind matrices"] = (() => Skin(s => s with { InverseBindMatrices = [Matrix4x4.Identity] }), "skin 0 (Armature) has 2 joints but 1 inverse bind matrices", "$.skins[0].inverseBindMatrices"),
        ["a skeleton that isn't a node"] = (() => Skin(s => s with { Skeleton = 3 }), "skin 0 (Armature) has the skeleton 3, which isn't a node", "$.skins[0].skeleton"),
        ["a skeleton below -1"] = (() => Skin(s => s with { Skeleton = -2 }), "skin 0 (Armature) has the skeleton -2, which isn't a node", "$.skins[0].skeleton"),
        ["a channel on a node that doesn't exist"] = (() => Channel(0, c => c with { Node = 3 }), "animation 0 (swing), channel 0 drives node 3, which doesn't exist", "$.animations[0].channels[0].node"),
        ["a channel on a negative node"] = (() => Channel(2, c => c with { Node = -1 }), "animation 0 (swing), channel 2 drives node -1, which doesn't exist", "$.animations[0].channels[2].node"),
        ["a channel without keys"] = (() => Channel(2, c => c with { Times = [], Values = [] }), "animation 0 (swing), channel 2 has 0 values for 0 keys", "$.animations[0].channels[2].values"),
        ["values for other keys"] = (() => Channel(0, c => c with { Values = [0, 0.75f, 0, 0, 1] }), "animation 0 (swing), channel 0 has 5 values for 2 keys", "$.animations[0].channels[0].values"),
        ["a cubic spline without tangents"] = (() => Channel(1, c => c with { Values = c.Values[3..11] }), "animation 0 (swing), channel 1 has 8 values for 2 keys", "$.animations[0].channels[1].values"),
        ["a cubic spline of one key"] = (() => Channel(1, c => c with { Times = [0], Values = c.Values[..12] }), "animation 0 (swing), channel 1 is a cubic spline with fewer than 2 keys", "$.animations[0].channels[1].times"),
        ["two keys at one time"] = (() => Channel(0, c => c with { Times = [0.5f, 0.5f] }), "animation 0 (swing), channel 0 has key times that don't increase", "$.animations[0].channels[0].times"),
        ["key times going back"] = (() => Channel(1, c => c with { Times = [0.5f, 0] }), "animation 0 (swing), channel 1 has key times that don't increase", "$.animations[0].channels[1].times"),
        ["a key time that isn't a number"] = (() => Channel(0, c => c with { Times = [0, float.NaN] }), "animation 0 (swing), channel 0 has key times that don't increase", "$.animations[0].channels[0].times"),
    };

    public static TheoryData<string> BrokenNames => [.. Broken.Keys];

    [Fact]
    public void TheSmallModelAndEveryTestModelKeepTheRules()
    {
        Small.Validate();
        foreach (var name in TestModelPack.Names)
            GltfImport.Load(TestModelPack.SourceOf(name)).Validate();
    }

    [Theory]
    [MemberData(nameof(BrokenNames))]
    public void EachBrokenRuleIsItsOwnProblem(string name)
    {
        var (model, problem, path) = Broken[name];
        Assert.Equal(problem, Assert.Throws<InvalidDataException>(() => model().Validate()).Message);
        // The model's check (M11 step 6, P20) reports it first, at the path of what it names.
        var first = RecordChecks.Check(model())[0];
        Assert.Equal((path, problem), (first.Path.ToString(), first.Message));
    }

    /// <summary>
    /// The model's check reports every rule a model breaks, each once at its path, and skips what reading past
    /// a broken index would need (a node showing a mesh that isn't there, a joint index of a skin that isn't).
    /// </summary>
    [Fact]
    public void TheCheckReportsEveryBrokenRuleAndSkipsWhatItCantRead()
    {
        var p = Small.Meshes[0].Primitives[0];
        var primitive = new ModelPrimitive
        {
            Positions = p.Positions, Normals = [Vector3.UnitZ], TexCoords = p.TexCoords, Joints = [new(9, 0, 0, 0), new(9, 0, 0, 0), default],
            Weights = p.Weights, Indices = [0, 1, 5, 7], Material = 4,
        };
        var model = With(
            nodes: [.. Small.Nodes.Select((n, i) => i == 2 ? n with { Mesh = 3 } : n)],
            meshes: [Small.Meshes[0] with { Primitives = [primitive, Small.Meshes[0].Primitives[1]] }],
            skins: [Small.Skins[0] with { Joints = [7, 8] }],
            roots: [0, 5]);
        Assert.Equal(["$.nodes[2].mesh", "$.roots[1]", "$.meshes[0].primitives[0].normals", "$.meshes[0].primitives[0].indices",
                "$.meshes[0].primitives[0].indices[2]", "$.meshes[0].primitives[0].material", "$.skins[0].joints[0]", "$.skins[0].joints[1]"],
            RecordChecks.Check(model).Select(problem => problem.Path.ToString()));
        // A primitive's joints: the first index past its skin (the message names one); a channel's times: once, however many go back.
        var joints = RecordChecks.Check(Primitive(joints: [new(5, 0, 0, 0), new(6, 0, 0, 0), default])).Select(problem => problem.Path.ToString()).ToList();
        Assert.Contains("$.meshes[0].primitives[0].joints[0]", joints);
        Assert.DoesNotContain("$.meshes[0].primitives[0].joints[1]", joints);
        var times = Channel(0, c => c with { Times = [0.5f, 0.25f, 0f], Values = new float[c.Values.Length / c.Times.Length * 3] });
        Assert.Equal(["$.animations[0].channels[0].times"], RecordChecks.Check(times).Select(problem => problem.Path.ToString()));
    }

    [Fact]
    public void ABrokenModelInAPackIsAProblemNamingThePackAndEntry()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("models/broken", Broken["an index past the vertices"].Model());
        writer.AddRecord("models/small", Small);
        writer.Write(folder["models.pak"]);
        using var files = new VirtualFileSystem([Mount.Pack(folder["models.pak"])]);
        var problem = Assert.Throws<DataFileException>(() => ModelData.Load(files, "models/broken"));
        Assert.Equal((Path.GetFullPath(folder["models.pak"]), "models/broken", "not a valid model: mesh 0 (Body), primitive 0 has the index 3 beyond its 3 vertices"),
            (problem.File, problem.Entry, problem.Problem));
        Assert.IsType<InvalidDataException>(problem.InnerException);
        // The GPU model is made from the checked data: the problem comes before the device is used.
        Assert.Equal(problem.Message, Assert.Throws<DataFileException>(() => Model.Load(null!, files, "models/broken")).Message);
        RecordAssert.Equal(Small, ModelData.Load(files, "models/small"));
    }

    [Fact]
    public void ABrokenModelInALooseFolderIsAProblemNamingTheFile()
    {
        using var folder = new TempFolder();
        var file = folder.Write("dev/models/broken.json", RecordSerializer.ToJson(Broken["a skin that doesn't exist"].Model()));
        using var files = new VirtualFileSystem([Mount.Folder(folder["dev"])]);
        var problem = Assert.Throws<DataFileException>(() => ModelData.Load(files, "models/broken"));
        Assert.Equal((Path.GetFullPath(file), (string?)null, "not a valid model: node 2 (Body) has skin 1, which doesn't exist"), (problem.File, problem.Entry, problem.Problem));
    }

    /// <summary>
    /// <see cref="VirtualFileSystem.Problem"/> names a path's place as the file system's own errors do: the
    /// topmost mount's pack and entry, or its loose file without an entry; any path a mount has, record or
    /// file; none when no mount has it.
    /// </summary>
    [Fact]
    public void TheFileSystemNamesAProblemsPlaceAsItsOwnErrorsDo()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("models/small", Small);
        writer.Add("models/notes", "notes"u8);
        writer.Add("models/covered", "under"u8);
        writer.Write(folder["models.pak"]);
        var looseFile = folder.Write("dev/models/covered", "over");
        var looseRecord = folder.Write("dev/models/loose.json", RecordSerializer.ToJson(Small));
        using var files = new VirtualFileSystem([Mount.Pack(folder["models.pak"]), Mount.Folder(folder["dev"])]);
        var pack = Path.GetFullPath(folder["models.pak"]);
        var inner = new InvalidDataException("inside");
        foreach (var (path, file, entry) in new[]
                 {
                     ("models/small", pack, "models/small"), ("models/notes", pack, "models/notes"),
                     ("models/covered", Path.GetFullPath(looseFile), null), ("models/loose", Path.GetFullPath(looseRecord), (string?)null),
                 })
        {
            var problem = files.Problem(path, "broken here", inner);
            Assert.Equal((file, entry, "broken here"), (problem.File, problem.Entry, problem.Problem));
            Assert.Same(inner, problem.InnerException);
            Assert.Equal(entry == null ? $"{file}: broken here" : $"{file}: entry '{entry}': broken here", problem.Message);
            // The file system's own error for the same path names the same place.
            var own = Assert.Throws<DataFileException>(() => files.Load<ModelAnimationEvent>(path));
            Assert.Equal((own.File, own.Entry), (problem.File, problem.Entry));
        }
        Assert.Null(files.Problem("models/notes", "no inner").InnerException);
        Assert.Throws<FileNotFoundException>(() => files.Problem("models/missing", "gone"));
        Assert.Throws<ArgumentException>(() => files.Problem("Models/Small", "not a path"));
    }

    /// <summary>A .glb GltfImport reads but whose model breaks a rule (a primitive's material past the file's materials) isn't packed.</summary>
    [Fact]
    public void TheImporterChecksTheModelsItPacks()
    {
        var bytes = File.ReadAllBytes(TestModelPack.SourceOf("sword"));
        var json = Encoding.UTF8.GetString(bytes, 20, BitConverter.ToInt32(bytes, 12));
        Assert.Equal(1, json.Split("\"material\":2}").Length - 1);
        // The same length, so the container's chunk lengths still hold.
        var patched = Encoding.UTF8.GetBytes(json.Replace("\"material\":2}", "\"material\":9}"));
        patched.CopyTo(bytes, 20);
        using var folder = new TempFolder();
        var source = folder.Write("models/sword.glb", bytes);
        var mesh = GltfImport.Load(source).Meshes.Select((m, i) => (m, i)).Single(x => x.m.Primitives.Any(p => p.Material == 9));
        var problem = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder.Root));
        Assert.Equal((source, $"the models importer: mesh {mesh.i} ({mesh.m.Name}), primitive 0 has material 9, which doesn't exist"), (problem.File, problem.Problem));
    }
}
