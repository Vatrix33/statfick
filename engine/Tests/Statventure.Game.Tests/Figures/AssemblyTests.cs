using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Statventure.Animation;
using Statventure.Core;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Figures;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Sandbox.Figure;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;

namespace Statventure.Game.Tests;

/// <summary>
/// Looks the game builds into figures against the same looks assembled in Blender (GM2 step 5; Docs/Game/Hero.md, "Assembly
/// in Blender" and "How it works", step 5): sample looks (the parts of the first humanoid's presets as the game's data holds
/// them, in colours of a race of the tests' with swatches, the game's look's own among them; one wearing the sample gear and
/// posed; none in the models' own placeholder colours, which are no look) are written
/// with the data they are read against (<c>Reference/assembly-looks.json</c>, the tests' race beside it in
/// <c>Reference/assembly-data/</c>; <c>STATVENTURE_ASSEMBLY_LOOKS=write</c>); Blender's side reads them against the same data
/// on its own (Tools/Models/humanoid/assemble.py, run by make-blender-references.sh: <c>Reference/assembly-scenes.json</c>,
/// rendered into <c>Golden/r3d-assembly-*.png</c> and <c>-ink.png</c>); the game's figure builder (<see cref="HeroFigure"/>)
/// builds each in the game's style (data.pak's <c>render/styles/game</c>), framed as the figure mode frames a figure
/// (render.py's views), and is held to Blender's assembly: the same models and colours, then the shading and the lines
/// under the detailed figures' limits (<see cref="ReferenceLimits.DetailedFigure"/>, the user's Q55, tightened by Q58 and Q60).
/// </summary>
[Collection("GPU")]
public class AssemblyTests(GpuFixture gpu)
{
    /// <summary>Set to <c>write</c>: the list and the tests' data beside it are written.</summary>
    public const string WriteVariable = "STATVENTURE_ASSEMBLY_LOOKS";

    static string Reference => Path.Combine(RaceOracle.Engine, "Tests", "Statventure.Tests", "Reference");
    static string ListPath => Path.Combine(Reference, "assembly-looks.json");
    static string ScenesPath => Path.Combine(Reference, "assembly-scenes.json");
    /// <summary>The tests' own data on top of the game's (the human race with swatches of the tests'), as a mod's would be.</summary>
    static string Overlay => Path.Combine(Reference, "assembly-data");
    static string OverlayRace => Path.Combine(Overlay, "data", "races", "human.json");
    static string TestClips => Path.Combine(RaceOracle.Engine, "Tests", "Statventure.Tests", "Data", "Models", "humanoid", "test-clips.glb");

    /// <summary>The view's background: the sheet's portrait panel's ground, the game's look file's.</summary>
    const string Background = "#2a1f1a";

    /// <summary>A sample: its scene's name, the look, the gear, the view and its size, and a clip of the test library at a time (none: at rest).</summary>
    public sealed record Sample(string Name, LookHero Look, FigureGear Gear, string View, int Width, int Height, string? Clip = null, double Time = 0);

    /// <summary>The swatches the tests' human has (none of them the game's: the game's are the art steps', GM2 step 9).</summary>
    static readonly RaceSwatch[] Swatches =
    [
        Swatch("skin", "Ember", "#aa7a5a"), Swatch("skin", "Pale", "#e8c4a8"), Swatch("skin", "Deep", "#6a4430"),
        Swatch("hair", "Ember", "#3a2416"), Swatch("hair", "Red", "#a8452a"), Swatch("hair", "Ash", "#b8b0a0"),
        Swatch("eyes", "Ember", "#0a0706"), Swatch("eyes", "Green", "#3f7a3a"), Swatch("eyes", "Blue", "#3a5f9a"),
        Swatch("brows", "Ember", "#130c08"), Swatch("brows", "Red", "#6e2a18"),
        Swatch("lips", "Ember", "#7c4238"), Swatch("lips", "Rose", "#a85a5a"),
        Swatch("eye.white", "Ember", "#d2c7b2"),
        Swatch("cloth", "Ember", "#74211d"), Swatch("cloth", "Blue", "#2a4a7a"), Swatch("cloth", "Green", "#3a5a2a"),
        Swatch("cloth.trim", "Ember", "#bb8d3c"), Swatch("cloth.trim", "Silver", "#a0a4aa"),
        Swatch("trousers", "Ember", "#292320"), Swatch("trousers", "Tan", "#8a6a48"),
        Swatch("leather", "Ember", "#3a261b"), Swatch("leather", "Black", "#1a1614"),
        Swatch("buckle", "Ember", "#bb8d3c"), Swatch("buckle", "Silver", "#a0a4aa"),
    ];

    static RaceSwatch Swatch(string slot, string name, string colour) => new() { Slot = slot, Name = name, Colour = colour };

    /// <summary>The game's human race (Assets/Data) with the tests' swatches.</summary>
    static Race TestHuman =>
        RecordSerializer.FromJson<Race>(File.ReadAllBytes(Path.Combine(RaceOracle.Engine, "Assets", "Data", "data", "races", "human.json"))) with { Swatches = Swatches };

    /// <summary>A preset's look as the game's data holds it (Assets/Data).</summary>
    static LookHero Preset(string name) =>
        RecordSerializer.FromJson<LookPreset>(File.ReadAllBytes(Path.Combine(RaceOracle.Engine, "Assets", "Data", "data", "looks", "presets", name + ".json"))).Look;

    static LookHero Man(params (string Slot, string Swatch)[] colours) => Preset("human-male-1") with { Colours = colours.ToDictionary(c => c.Slot, c => c.Swatch) };

    static LookHero Woman(params (string Slot, string Swatch)[] colours) => Preset("human-female-1") with { Colours = colours.ToDictionary(c => c.Slot, c => c.Swatch) };

    static readonly (string, string)[] Ember =
    [
        ("skin", "Ember"), ("hair", "Ember"), ("eyes", "Ember"), ("brows", "Ember"), ("eye.white", "Ember"), ("cloth", "Ember"), ("cloth.trim", "Ember"),
        ("trousers", "Ember"), ("leather", "Ember"), ("buckle", "Ember"),
    ];

    static readonly (string, string)[] Red =
    [
        ("skin", "Pale"), ("hair", "Red"), ("eyes", "Green"), ("brows", "Red"), ("lips", "Rose"), ("eye.white", "Ember"), ("cloth", "Blue"),
        ("cloth.trim", "Silver"), ("trousers", "Tan"), ("leather", "Black"), ("buckle", "Silver"),
    ];

    /// <summary>The sample gear (GM2's, Q9): the chain shirt over the outfit, the sword in the right hand, the shield on the left arm, in the game's look file's metals.</summary>
    static readonly FigureGear Geared = new()
    {
        Garments = ["models/humanoid/gear/chain-shirt/human-male-average"],
        Items = [new FigureItem("models/humanoid/gear/sword", "socket.hand.R"), new FigureItem("models/humanoid/gear/shield", "socket.shield.L")],
        Colours = new Dictionary<string, string> { ["metal.base"] = "#80848c", ["metal.trim"] = "#bb8d3c", ["metal.shade"] = "#3a3c42", ["wood"] = "#5a2a1e", ["grip"] = "#3a261b" },
    };

    // Every sample has colours of the tests' swatches, none the models' own: the game's presets have no colours until the
    // human has swatches (GM2 step 9a), and the models' placeholder colours are no look to hold a picture to. Each takes its
    // parts from the game's preset (the first humanoid's face and hair), so a preset changed by step 9 changes the list.
    public static IReadOnlyList<Sample> Samples { get; } =
    [
        // The game's look's own colours (as the approved figures), from two sides; others in every slot, from two more and
        // the face up close.
        new("assembly-human-male-ember", Man(Ember), new(), "three-quarter", 360, 480),
        new("assembly-human-female-ember", Woman([.. Ember, ("lips", "Ember")]), new(), "front", 360, 480),
        new("assembly-human-male-red-back", Man([.. Red.Where(c => c.Item1 != "lips")]), new(), "back", 360, 480),
        new("assembly-human-female-red", Woman(Red), new(), "side", 360, 480),
        new("assembly-human-female-red-close-up", Woman(Red), new(), "close-up", 320, 320),
        // Gear on it: a garment of the gear, two items on sockets, the gear's colours, posed.
        new("assembly-human-male-geared",
            Man(("skin", "Deep"), ("hair", "Ash"), ("eyes", "Blue"), ("brows", "Ember"), ("eye.white", "Ember"), ("cloth", "Green"), ("cloth.trim", "Ember"),
                ("trousers", "Tan"), ("leather", "Black"), ("buckle", "Ember")),
            Geared, "three-quarter", 360, 480, "rest-sway", 0.5),
    ];

    public static TheoryData<string> SampleNames => new(Samples.Select(s => s.Name));

    /// <summary>The list Blender's side reads: the data folders (the game's, then the tests'), the models' folder, the background and each sample.</summary>
    internal static string ListJson()
    {
        var scenes = new JsonArray();
        foreach (var s in Samples)
            scenes.Add(new JsonObject
            {
                ["name"] = s.Name,
                ["look"] = JsonNode.Parse(RecordSerializer.ToJson(s.Look)),
                ["gear"] = new JsonObject
                {
                    ["garments"] = new JsonArray([.. s.Gear.Garments.Select(g => (JsonNode)g)]),
                    ["items"] = new JsonArray([.. s.Gear.Items.Select(i => (JsonNode)new JsonObject { ["model"] = i.Model, ["socket"] = i.Socket })]),
                    ["colours"] = new JsonObject(s.Gear.Colours.OrderBy(c => c.Key, StringComparer.Ordinal).Select(c => KeyValuePair.Create(c.Key, (JsonNode?)c.Value))),
                },
                ["view"] = s.View,
                ["width"] = s.Width,
                ["height"] = s.Height,
                ["clip"] = s.Clip,
                ["time"] = s.Time,
                ["clips"] = s.Clip is null ? null : "../Data/Models/humanoid/test-clips.glb",
            });
        var list = new JsonObject
        {
            ["format"] = "statventure.assembly",
            ["version"] = 1,
            ["data"] = new JsonArray("../../../Assets/Data", "assembly-data"),
            ["models"] = "../../../Assets/Models",
            ["background"] = Background,
            ["scenes"] = scenes,
        };
        return list.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }

    /// <summary>
    /// The list and the tests' data beside it are what the samples make (written with STATVENTURE_ASSEMBLY_LOOKS=write; then
    /// make-blender-references.sh assembles and renders them): a change to the samples, the presets or the swatches fails
    /// here until they are written and rendered again.
    /// </summary>
    [Fact]
    public void TheListIsUpToDate()
    {
        var list = ListJson();
        var race = RecordSerializer.ToJson(TestHuman);
        if (Environment.GetEnvironmentVariable(WriteVariable) == "write")
        {
            File.WriteAllText(ListPath, list);
            Directory.CreateDirectory(Path.GetDirectoryName(OverlayRace)!);
            File.WriteAllText(OverlayRace, race);
        }
        Assert.True(File.Exists(ListPath) && File.ReadAllText(ListPath) == list && File.Exists(OverlayRace) && File.ReadAllText(OverlayRace) == race,
            $"Reference/assembly-looks.json or assembly-data/ isn't what the samples make: run the game's tests with {WriteVariable}=write (AssemblyTests), then make-blender-references.sh with the assembly scenes' names");
        Assert.Equal(["human.json"], Directory.GetFiles(Overlay, "*", SearchOption.AllDirectories).Select(Path.GetFileName));
    }

    /// <summary>The game's data with the tests' on top (the development folder's place: over the packs, as a mod's data would be).</summary>
    internal static VirtualFileSystem Data(ScratchFolder folder)
    {
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        return new VirtualFileSystem(Application.DataMounts(new ApplicationOptions { DevelopmentFolder = Overlay }, folder["game"], _ => null));
    }

    /// <summary>A model's data path (<c>models/…</c>) as Blender's scene names its file (its .glb under the models' folder).</summary>
    static string FileOf(string model) => model["models/".Length..] + ".glb";

    /// <summary>The figure's surface points at rest (render.py's framing): the body's, each garment's and each item's at its socket.</summary>
    static List<Vector3> RestPoints(HeroFigure figure)
    {
        var points = FigureFraming.RestPoints(figure.Actor.Model.Data, Matrix4x4.Identity);
        foreach (var garment in figure.Actor.Garments)
            points.AddRange(FigureFraming.RestPoints(garment.Model.Data, Matrix4x4.Identity));
        var rest = figure.Actor.Model.Data.WorldMatrices();
        foreach (var item in figure.Actor.Items)
            points.AddRange(FigureFraming.RestPoints(item.Model.Data, item.Socket.ItemTransform(rest, Matrix4x4.Identity)));
        return points;
    }

    /// <summary>A sample's figure in its view: built from the data, posed, turned and framed as the figure mode frames one, in the game's style.</summary>
    internal sealed class Shown : IDisposable
    {
        public Shown(GpuDevice device, VirtualFileSystem data, Sample sample)
        {
            Figure = HeroFigure.Build(device, data, sample.Look, sample.Gear);
            if (sample.Clip is { } clip)
            {
                var library = AnimationClip.AllOf(ModelData.Load(TestClips)).Single(c => c.Name == clip);
                Figure.Actor.Animator.Stack.Layers.Add(new FigureClips([library]).Layer(Figure.Skeleton));
            }
            Style = GameStyle.Load(data);
            View = new View3D(device) { Background = Colors.LinearFromHex(Background), Sun = Style.SunLight, Style = Style.Cel };
            var spec = new FigureSpec { Body = "", Look = GameStyleTests.Look, View = sample.View, Width = sample.Width, Height = sample.Height };
            View.Camera = FigureFraming.CameraFor(spec, sample.View, RestPoints(Figure), FigureFraming.FocusPoint(Figure.Actor.Model.Data, "head"));
            Scene = new SceneView(View) { Rate = 0 };
            var node = Scene.Root.Add("figure");
            node.Attach(Figure.Actor);
            node.SetLocal(Vector3.Zero, Quaternion.CreateFromAxisAngle(Vector3.UnitY, FigureFraming.TurnOf(sample.View) * MathF.PI / 180), Vector3.One);
            Figure.Actor.Animator.Seek(sample.Time);
            Scene.Advance(0);
        }

        public HeroFigure Figure { get; }
        public RenderStyle Style { get; }
        public View3D View { get; }
        public SceneView Scene { get; }

        public void Dispose()
        {
            View.Dispose();
            Figure.Dispose();
        }
    }

    /// <summary>The view drawn at its size: the shading with the lines off, or the lines alone (their coverage), as the engine's references are compared.</summary>
    internal static RgbaImage Render(GpuDevice device, View3D view, int width, int height, bool lines = true, bool inkOnly = false)
    {
        var style = lines ? view.Style : view.Style with { OutlineWidth = 0, InnerWidth = 0 };
        using var target = new RenderTarget3D(device, width, height);
        var renderer = new Renderer3D(device) { InkCoverageOnly = inkOnly };
        var frame = device.BeginFrame();
        renderer.Render(frame, target, view.Camera, view.Items, view.Background ?? Vector4.Zero, view.Sun, style, 1, view.Time, view.Particles);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(width, height, target.Resolved.ReadPixels());
    }

    /// <summary>
    /// Each figure holds what Blender's assembly of its look holds (Reference/assembly-scenes.json): the body's model, then
    /// the parts' and the gear's garments and the items, by their files; the palette, colour for colour; the camera the
    /// framing gives, number for number; each model's place (the items on their sockets) within a micrometre.
    /// </summary>
    [Fact]
    public void TheFiguresHoldWhatBlendersAssemblyHolds()
    {
        Assert.True(File.Exists(ScenesPath), "no Reference/assembly-scenes.json: run the tests, then make-blender-references.sh (it assembles the looks)");
        using var scenes = JsonDocument.Parse(File.ReadAllText(ScenesPath));
        Assert.Equal("../../../Assets/Models", scenes.RootElement.GetProperty("modelFolder").GetString());
        Assert.Equal(Samples.Select(s => s.Name), scenes.RootElement.GetProperty("scenes").EnumerateArray().Select(s => s.GetProperty("name").GetString()));
        using var folder = new ScratchFolder();
        using var data = Data(folder);
        foreach (var sample in Samples)
        {
            var scene = scenes.RootElement.GetProperty("scenes").EnumerateArray().Single(s => s.GetProperty("name").GetString() == sample.Name);
            using var shown = new Shown(gpu.Device, data, sample);
            var plan = shown.Figure.Plan;
            var models = scene.GetProperty("models").EnumerateArray().ToList();
            Assert.Equal([FileOf(plan.Body), .. plan.GarmentModels.Select(FileOf), .. plan.Items.Select(i => FileOf(i.Model))], models.Select(m => m.GetProperty("file").GetString()));
            Assert.Equal(plan.Colours.OrderBy(c => c.Key, StringComparer.Ordinal),
                scene.GetProperty("palette").EnumerateObject().Select(p => KeyValuePair.Create(p.Name, p.Value.GetString()!)).OrderBy(c => c.Key, StringComparer.Ordinal));
            var camera = scene.GetProperty("camera");
            var view = shown.View.Camera;
            Near(sample.Name, V(camera.GetProperty("position")), view.Position, 1e-4f);
            Near(sample.Name, V(camera.GetProperty("target")), view.Target, 1e-4f);
            Assert.True(MathF.Abs(camera.GetProperty("orthographicHeight").GetSingle() - view.OrthographicHeight) < 1e-4f, $"{sample.Name}: the framed height");
            Assert.Equal(view.Projection.ToString().ToLowerInvariant(), camera.GetProperty("projection").GetString());
            // Each item where Blender put it: its socket's posed world, turned for the view.
            var turn = Matrix4x4.CreateRotationY(FigureFraming.TurnOf(sample.View) * MathF.PI / 180);
            var world = shown.Figure.Actor.Model.Data.WorldMatrices(shown.Figure.Actor.Animator.Pose());
            for (var i = 0; i < plan.Items.Count; i++)
            {
                var placed = shown.Figure.Actor.Items[i].Socket.ItemTransform(world, turn);
                var matrix = models[1 + plan.GarmentModels.Count() + i].GetProperty("matrix").EnumerateArray().Select(c => c.EnumerateArray().Select(v => v.GetSingle()).ToArray()).ToArray();
                Near(sample.Name, new Vector3(matrix[3][0], matrix[3][1], matrix[3][2]), placed.Translation, 1e-5f);
                Near(sample.Name, new Vector3(matrix[1][0], matrix[1][1], matrix[1][2]), new Vector3(placed.M21, placed.M22, placed.M23), 1e-5f);
            }
        }
    }

    static Vector3 V(JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle(), e[2].GetSingle());

    static void Near(string what, Vector3 expected, Vector3 actual, float tolerance) =>
        Assert.True(Vector3.Distance(expected, actual) <= tolerance * MathF.Max(1, expected.Length()), $"{what}: expected {expected}, got {actual}");

    [Theory]
    [MemberData(nameof(SampleNames))]
    public void TheEngineShadesTheAssembledLookAsBlenderDid(string name)
    {
        var sample = Samples.Single(s => s.Name == name);
        var reference = Golden.GoldenPath("r3d-" + name);
        Assert.True(File.Exists(reference), $"no Blender reference for {name}: run the tests, then Tools/Scripts/make-blender-references.sh {name}");
        using var folder = new ScratchFolder();
        using var data = Data(folder);
        using var shown = new Shown(gpu.Device, data, sample);
        var actual = Render(gpu.Device, shown.View, sample.Width, sample.Height, lines: false);
        var problems = Render3DReferenceTests.Compare(ImageFile.Load(reference), actual, ReferenceLimits.DetailedFigure);
        var actualPath = Golden.ActualPath("r3d-" + name);
        if (problems.Length > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        Assert.True(problems.Length == 0, $"{name}: {problems}; render in {actualPath}");
    }

    [Theory]
    [MemberData(nameof(SampleNames))]
    public void TheEngineInksTheAssembledLookAsFreestyleDid(string name)
    {
        var sample = Samples.Single(s => s.Name == name);
        var reference = Golden.GoldenPath("r3d-" + name + "-ink");
        Assert.True(File.Exists(reference), $"no Freestyle reference for {name}: run the tests, then Tools/Scripts/make-blender-references.sh {name}");
        using var folder = new ScratchFolder();
        using var data = Data(folder);
        using var shown = new Shown(gpu.Device, data, sample);
        var actual = Render(gpu.Device, shown.View, sample.Width, sample.Height, inkOnly: true);
        var problems = Render3DReferenceTests.CompareInk(Render3DReferenceTests.InkCoverage(ImageFile.Load(reference), alpha: true),
            Render3DReferenceTests.InkCoverage(actual, alpha: false), actual.Width, ReferenceLimits.DetailedFigure);
        var actualPath = Golden.ActualPath("r3d-" + name + "-ink");
        if (problems.Length > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        Assert.True(problems.Length == 0, $"{name}: {problems}; lines in {actualPath}");
    }

    /// <summary>assemble.py orders the parts as the game does: its list of categories is the maker's (LookNames.Categories, written once in LookPart's [Choices]).</summary>
    [Fact]
    public void TheAssemblysCategoriesAreTheMakers()
    {
        var script = File.ReadAllText(Path.Combine(RaceOracle.Engine, "Tools", "Models", "humanoid", "assemble.py"));
        var match = Regex.Match(script, @"^CATEGORIES = \[(.*)\]$", RegexOptions.Multiline);
        Assert.True(match.Success, "assemble.py has no CATEGORIES list");
        Assert.Equal(LookNames.Categories, Regex.Matches(match.Groups[1].Value, "\"([^\"]*)\"").Select(m => m.Groups[1].Value));
    }
}
