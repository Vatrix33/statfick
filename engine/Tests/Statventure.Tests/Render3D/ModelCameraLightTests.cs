using System.Text;
using System.Text.Json.Nodes;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;

namespace Statventure.Tests.Render3D;

/// <summary>A model record with every camera and light field (Docs/Scene.md, step 3): the small model's nodes carrying two cameras and the sun.</summary>
public static class StageRecordSamples
{
    public static ModelData Stage()
    {
        var small = ModelRecordSamples.Small();
        return new ModelData
        {
            Nodes = [small.Nodes[0] with { Camera = 0 }, small.Nodes[1] with { Camera = 1, Light = 0 }, small.Nodes[2]],
            Meshes = small.Meshes,
            Materials = small.Materials,
            Skins = small.Skins,
            Animations = small.Animations,
            Roots = small.Roots,
            Cameras =
            [
                new ModelCamera { Name = "Lens", Projection = Projection.Perspective, VerticalFieldOfView = 0.6605947f, Near = 0.1f, Far = 40 },
                new ModelCamera { Name = "Plan", Projection = Projection.Orthographic, OrthographicHeight = 6, Near = 0, Far = 30 },
            ],
            Lights = [new ModelLight("Daylight", 3, 3.25f)],
        };
    }
}

/// <summary>
/// A stage's cameras and sun in the model (Docs/Scene.md, step 3; Q9, Q12, S3-1 to S3-4): what the
/// importer refuses (other kinds of light, a coloured sun, a sun without its angle, more than one sun,
/// an infinite perspective, values that make no projection or sun, cameras and lights that don't
/// exist), naming the file, the light or camera; through the models importer a
/// <see cref="DataFileException"/> naming the file; the rules <see cref="ModelData.Validate"/> adds for
/// them; and version 2 of the model record (a fixture holding every new field, the version 1 fixture
/// read as the model without cameras or lights).
/// </summary>
public class ModelCameraLightTests
{
    static string Dolly => Path.Combine(Render3DReferenceTests.ModelDirectory, "dolly.glb");

    /// <summary>The dolly stage's .glb with its JSON changed, written into the folder (the container put together again).</summary>
    internal static string Patched(TempFolder folder, string name, Action<JsonNode> change)
    {
        var bytes = File.ReadAllBytes(Dolly);
        var length = BitConverter.ToInt32(bytes, 12);
        var json = JsonNode.Parse(bytes.AsSpan(20, length))!;
        change(json);
        var text = Encoding.UTF8.GetBytes(json.ToJsonString());
        var padded = text.Concat(Enumerable.Repeat((byte)' ', -text.Length & 3)).ToArray();
        var binary = bytes[(20 + length)..];
        using var output = new MemoryStream();
        using (var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true))
        {
            writer.Write("glTF"u8);
            writer.Write(2);
            writer.Write(20 + padded.Length + binary.Length);
            writer.Write(padded.Length);
            writer.Write("JSON"u8);
            writer.Write(padded);
            writer.Write(binary);
        }
        return folder.Write(name, output.ToArray());
    }

    static JsonNode Light(JsonNode gltf) => gltf["extensions"]!["KHR_lights_punctual"]!["lights"]![0]!;
    static JsonNode Node(JsonNode gltf, string name) => gltf["nodes"]!.AsArray().Single(n => (string?)n!["name"] == name)!;
    static JsonNode Camera(JsonNode gltf, int index) => gltf["cameras"]![index]!;

    static readonly Dictionary<string, (Action<JsonNode> Change, Type Error, string Problem)> Refused = new()
    {
        ["a point light"] = (g => Light(g)["type"] = "point", typeof(NotSupportedException),
            "light 0 (Daylight) is a point light: the engine draws one sun, a directional light, and no other kind"),
        ["a spot light"] = (g =>
        {
            Light(g)["type"] = "spot";
            Light(g)["spot"] = new JsonObject();
        }, typeof(NotSupportedException), "light 0 (Daylight) is a spot light: the engine draws one sun, a directional light, and no other kind"),
        ["a coloured sun"] = (g => Light(g)["color"] = new JsonArray(1, 0.75, 0.5), typeof(NotSupportedException),
            "light 0 (Daylight) has the colour (1, 0.75, 0.5): the engine's sun is white"),
        ["a sun not quite white"] = (g => Light(g)["color"] = new JsonArray(1, 1, 0.99), typeof(NotSupportedException),
            "light 0 (Daylight) has the colour (1, 1, 0.99): the engine's sun is white"),
        ["a sun without its angle"] = (g => Light(g).AsObject().Remove("extras"), typeof(InvalidDataException),
            "light 0 (Daylight) has no angle in its extras (the sun's size in degrees, which the stage export copies from Blender's sun)"),
        ["an angle that isn't a number"] = (g => Light(g)["extras"]!["angle"] = "3", typeof(InvalidDataException),
            "light 0 (Daylight) has no angle in its extras (the sun's size in degrees, which the stage export copies from Blender's sun)"),
        ["a sun 180° across"] = (g => Light(g)["extras"]!["angle"] = 180, typeof(InvalidDataException),
            "light 0 (Daylight) has an angle of 180° (0 or more, less than 180)"),
        ["two suns"] = (g => g["extensions"]!["KHR_lights_punctual"]!["lights"]!.AsArray().Add(Light(g).DeepClone()), typeof(NotSupportedException),
            "2 lights: a model has one sun or none"),
        ["two nodes carrying the sun"] = (g => Node(g, "Crate")["extensions"] = new JsonObject { ["KHR_lights_punctual"] = new JsonObject { ["light"] = 0 } },
            typeof(NotSupportedException), "nodes 0 (Crate) and 7 (Sun) both carry the sun: a model has one sun or none"),
        ["a light that doesn't exist"] = (g => Node(g, "Sun")["extensions"]!["KHR_lights_punctual"]!["light"] = 1, typeof(InvalidDataException),
            "node 7 (Sun) carries light 1, which doesn't exist"),
        ["a camera that doesn't exist"] = (g => Node(g, "Camera")["camera"] = 2, typeof(InvalidDataException), "node 1 (Camera) carries camera 2, which doesn't exist"),
        ["an infinite perspective"] = (g => Camera(g, 0)["perspective"]!.AsObject().Remove("zfar"), typeof(NotSupportedException),
            "camera 0 (Lens) has no far plane (an infinite projection), which the engine's cameras don't draw"),
        // (BRUTAL's parser refuses a yfov of 0 and a negative intensity itself.)
        ["a field of view past a half turn"] = (g => Camera(g, 0)["perspective"]!["yfov"] = 3.5, typeof(InvalidDataException),
            "camera 0 (Lens) has a vertical field of view of 3.5 radians (more than 0 and less than π)"),
        ["a far plane before the near one"] = (g => Camera(g, 0)["perspective"]!["zfar"] = 0.05, typeof(InvalidDataException),
            "camera 0 (Lens) has its far plane at 0.05, not finite and beyond its near plane at 0.1"),
        ["a flat orthographic camera"] = (g => Camera(g, 1)["orthographic"]!["ymag"] = 0, typeof(InvalidDataException), "camera 1 (Plan) has a height of 0 in view (more than 0)"),
    };

    public static TheoryData<string> RefusedNames => [.. Refused.Keys];

    [Theory]
    [MemberData(nameof(RefusedNames))]
    public void TheImportRefusesWhatTheEngineDoesntDraw(string name)
    {
        var (change, error, problem) = Refused[name];
        using var folder = new TempFolder();
        var path = Patched(folder, "stage.glb", change);
        var thrown = Assert.Throws(error, () => GltfImport.Load(path));
        Assert.Equal($"{path}: {problem}", thrown.Message);
    }

    [Fact]
    public void ThePatchedStageReadsWhenNothingIsChanged()
    {
        using var folder = new TempFolder();
        RecordAssert.Equal(GltfImport.Load(Dolly), GltfImport.Load(Patched(folder, "stage.glb", _ => { })));
    }

    [Fact]
    public void ThePackerRefusesAStageWithAPointLightNamingTheFileAndTheLight()
    {
        using var folder = new TempFolder();
        var source = Patched(folder, "models/stage.glb", g => Light(g)["type"] = "point");
        var problem = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder.Root));
        Assert.Equal((source, (string?)null), (problem.File, problem.Entry));
        Assert.Equal($"the models importer: {source}: light 0 (Daylight) is a point light: the engine draws one sun, a directional light, and no other kind", problem.Problem);
    }

    [Fact]
    public void ThePackerRefusesASunWithoutItsAngle()
    {
        using var folder = new TempFolder();
        var source = Patched(folder, "models/stage.glb", g => Light(g).AsObject().Remove("extras"));
        var problem = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder.Root));
        Assert.Equal(source, problem.File);
        Assert.Contains("light 0 (Daylight) has no angle in its extras", problem.Problem);
    }

    static readonly ModelData Stage = StageRecordSamples.Stage();

    static ModelData With(Func<ModelNode, int, ModelNode>? node = null, IReadOnlyList<ModelCamera>? cameras = null, IReadOnlyList<ModelLight>? lights = null) => new()
    {
        Nodes = node == null ? Stage.Nodes : [.. Stage.Nodes.Select((n, i) => node(n, i))],
        Meshes = Stage.Meshes,
        Materials = Stage.Materials,
        Skins = Stage.Skins,
        Animations = Stage.Animations,
        Roots = Stage.Roots,
        Cameras = cameras ?? Stage.Cameras,
        Lights = lights ?? Stage.Lights,
    };

    static ModelData Lens(Func<ModelCamera, ModelCamera> change) => With(cameras: [change(Stage.Cameras[0]), Stage.Cameras[1]]);
    static ModelData Plan(Func<ModelCamera, ModelCamera> change) => With(cameras: [Stage.Cameras[0], change(Stage.Cameras[1])]);
    static ModelData Sun(Func<ModelLight, ModelLight> change) => With(lights: [change(Stage.Lights[0])]);

    static readonly Dictionary<string, (Func<ModelData> Model, string Problem, string Path)> Broken = new()
    {
        ["a camera that doesn't exist"] = (() => With((n, i) => i == 0 ? n with { Camera = 2 } : n), "node 0 (Rig) carries camera 2, which doesn't exist", "$.nodes[0].camera"),
        ["a camera below -1"] = (() => With((n, i) => i == 2 ? n with { Camera = -2 } : n), "node 2 (Body) carries camera -2, which doesn't exist", "$.nodes[2].camera"),
        ["a light that doesn't exist"] = (() => With((n, i) => i == 1 ? n with { Light = 1 } : n), "node 1 (Bone) carries light 1, which doesn't exist", "$.nodes[1].light"),
        ["a light below -1"] = (() => With((n, i) => i == 2 ? n with { Light = -2 } : n), "node 2 (Body) carries light -2, which doesn't exist", "$.nodes[2].light"),
        ["two lights"] = (() => With(lights: [Stage.Lights[0], Stage.Lights[0]]), "2 lights: a model has one sun or none", "$.lights"),
        ["two nodes carrying the sun"] = (() => With((n, i) => i == 2 ? n with { Light = 0 } : n), "nodes 1 (Bone) and 2 (Body) both carry the sun: a model has one sun or none", "$.nodes[2].light"),
        ["no field of view"] = (() => Lens(c => c with { VerticalFieldOfView = 0 }), "camera 0 (Lens) has a vertical field of view of 0 radians (more than 0 and less than π)", "$.cameras[0].verticalFieldOfView"),
        ["a half turn of field of view"] = (() => Lens(c => c with { VerticalFieldOfView = MathF.PI }),
            "camera 0 (Lens) has a vertical field of view of 3.1415927 radians (more than 0 and less than π)", "$.cameras[0].verticalFieldOfView"),
        ["a field of view that isn't a number"] = (() => Lens(c => c with { VerticalFieldOfView = float.NaN }),
            "camera 0 (Lens) has a vertical field of view of NaN radians (more than 0 and less than π)", "$.cameras[0].verticalFieldOfView"),
        ["a perspective near plane at 0"] = (() => Lens(c => c with { Near = 0 }), "camera 0 (Lens) has its near plane at 0 (a perspective camera's is beyond 0)", "$.cameras[0].near"),
        ["a far plane at the near one"] = (() => Lens(c => c with { Far = 0.1f }), "camera 0 (Lens) has its far plane at 0.1, not finite and beyond its near plane at 0.1", "$.cameras[0].far"),
        ["an infinite far plane"] = (() => Lens(c => c with { Far = float.PositiveInfinity }), "camera 0 (Lens) has its far plane at Infinity, not finite and beyond its near plane at 0.1", "$.cameras[0].far"),
        ["a flat orthographic camera"] = (() => Plan(c => c with { OrthographicHeight = 0 }), "camera 1 (Plan) has a height of 0 in view (more than 0)", "$.cameras[1].orthographicHeight"),
        ["an orthographic near plane behind it"] = (() => Plan(c => c with { Near = -1 }), "camera 1 (Plan) has its near plane at -1 (at 0 or beyond)", "$.cameras[1].near"),
        ["an orthographic far plane at its near one"] = (() => Plan(c => c with { Far = 0 }), "camera 1 (Plan) has its far plane at 0, not finite and beyond its near plane at 0", "$.cameras[1].far"),
        ["a negative strength"] = (() => Sun(l => l with { Strength = -1 }), "light 0 (Daylight) has a strength of -1 (finite, 0 or more)", "$.lights[0].strength"),
        ["an infinite strength"] = (() => Sun(l => l with { Strength = float.PositiveInfinity }), "light 0 (Daylight) has a strength of Infinity (finite, 0 or more)", "$.lights[0].strength"),
        ["a negative angle"] = (() => Sun(l => l with { Angle = -1 }), "light 0 (Daylight) has an angle of -1° (0 or more, less than 180)", "$.lights[0].angle"),
        ["a sun 180° across"] = (() => Sun(l => l with { Angle = 180 }), "light 0 (Daylight) has an angle of 180° (0 or more, less than 180)", "$.lights[0].angle"),
        ["an angle that isn't a number"] = (() => Sun(l => l with { Angle = float.NaN }), "light 0 (Daylight) has an angle of NaN° (0 or more, less than 180)", "$.lights[0].angle"),
    };

    public static TheoryData<string> BrokenNames => [.. Broken.Keys];

    [Fact]
    public void TheStageSampleKeepsTheRules()
    {
        Stage.Validate();
        // At the edges: an orthographic camera's near plane at 0, a sun of no size and of no strength.
        Plan(c => c with { Near = 0 }).Validate();
        Sun(l => l with { Angle = 0, Strength = 0 }).Validate();
        With((n, i) => n with { Light = -1 }).Validate();
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

    [Fact]
    public void TheStageRecordFixtureIsRecorded() => RecordFixtures.Ensure("stage", StageRecordSamples.Stage());

    [Theory]
    [InlineData(".rec")]
    [InlineData(".json")]
    public void TheStageFixtureHoldsEveryNewFieldAtVersion2(string form)
    {
        var (value, table) = RecordFixtures.Load("model", "stage", form);
        RecordAssert.Equal(StageRecordSamples.Stage(), value);
        var versions = table.ToDictionary(e => e.Name, e => e.Version);
        Assert.Equal((2, 2, 1, 1), (versions["model"], versions["model.node"], versions["model.camera"], versions["model.light"]));
    }

    /// <summary>The model fixture written by M9 holds version 1 of the model and its nodes: upgraded, the same model without cameras or lights.</summary>
    [Theory]
    [InlineData(".rec")]
    [InlineData(".json")]
    public void TheVersion1ModelFixtureReadsAsTheModelWithoutCamerasOrLights(string form)
    {
        var (value, table) = RecordFixtures.Load("model", "model", form);
        var versions = table.ToDictionary(e => e.Name, e => e.Version);
        Assert.Equal((1, 1), (versions["model"], versions["model.node"]));
        var model = Assert.IsType<ModelData>(value);
        RecordAssert.Equal(ModelRecordSamples.Small(), model);
        Assert.Empty(model.Cameras);
        Assert.Empty(model.Lights);
        Assert.All(model.Nodes, n => Assert.Equal((-1, -1), (n.Camera, n.Light)));
        Assert.Equal([true, true, false], model.Nodes.Select(n => n.Ink));
    }
}
