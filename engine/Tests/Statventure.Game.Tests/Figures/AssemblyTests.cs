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
    /// <summary>The clip libraries a sample may be posed by, as the list names them (relative to Reference/): the clip frame's test library and the family's.</summary>
    const string TestLibrary = "../Data/Models/humanoid/test-clips.glb", GameLibrary = "../../../Assets/Models/humanoid/clips.glb";

    /// <summary>The view's background: the sheet's portrait panel's ground, the game's look file's.</summary>
    const string Background = "#2a1f1a";

    /// <summary>
    /// A sample: its scene's name, the look, the gear, the view and its size, a clip at a time (none: at rest) of a library
    /// (<see cref="TestLibrary"/> by default), and whether it is read against the game's data alone
    /// (<paramref name="GamesOwn"/>: the game's human and its swatches, GM2 step 9a) or with the tests' race over it.
    /// </summary>
    public sealed record Sample(string Name, LookHero Look, FigureGear Gear, string View, int Width, int Height, string? Clip = null, double Time = 0,
        string Library = TestLibrary, bool GamesOwn = false);

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

    /// <summary>
    /// The woman with the first humanoid's long tied hair, as step 5's close-up passed with (the manager's GM2-P35: the
    /// Ranger's long hair shows faint seams up close where its locks meet, an engine task's; the sample waits for it).
    /// </summary>
    static LookHero WomanLongTied(params (string Slot, string Swatch)[] colours) =>
        Woman(colours) with { Parts = new Dictionary<string, string> { ["face"] = "face-human-female-1", ["hair"] = "hair-long-tied", ["marks"] = "marks-none", ["head-piece"] = "head-piece-none" } };

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

    /// <summary>The races of GM2 steps 9b–9e in the samples, each held to Blender as step 9a held the human (GM2-P30): its eight presets and its two average bodies plain.</summary>
    internal static readonly string[] Races = ["elf", "dark-elf"];

    /// <summary>
    /// A race's average body plain (steps 9b–9e): the first face and the first humanoid's hair (swept back, long tied),
    /// nothing else, in the colours of the race's first preset of that sex (a look of its own swatches).
    /// </summary>
    static LookHero RacePlain(string race, string sex)
    {
        var parts = new Dictionary<string, string>
        {
            ["face"] = $"face-{race}-{sex}-1", ["hair"] = sex == "male" ? "hair-swept-back" : "hair-long-tied", ["marks"] = "marks-none", ["head-piece"] = "head-piece-none",
        };
        if (sex == "male")
            parts["beard"] = "beard-none";
        return Preset($"{race}-{sex}-1") with { Parts = parts };
    }

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
        new("assembly-human-female-red-close-up", WomanLongTied(Red), new(), "close-up", 320, 320),
        // Gear on it: a garment of the gear, two items on sockets, the gear's colours, posed.
        new("assembly-human-male-geared",
            Man(("skin", "Deep"), ("hair", "Ash"), ("eyes", "Blue"), ("brows", "Ember"), ("eye.white", "Ember"), ("cloth", "Green"), ("cloth.trim", "Ember"),
                ("trousers", "Tan"), ("leather", "Black"), ("buckle", "Ember")),
            Geared, "three-quarter", 360, 480, "rest-sway", 0.5),
        // The woman kneeling in meditate (the family's library, its marker "restore"): the clip's hips' place scaled by her
        // legs on both sides (GM2-P22; render.py's pose_at in Blender), her knee on the ground, standing by her soles.
        new("assembly-human-female-meditate", Woman([.. Ember, ("lips", "Ember")]), new(), "three-quarter", 360, 480, "meditate", 42 / 60.0, GameLibrary),
        // The geared man lying at death's end (GM2-P37, contacts kept): the clip was keyed on his outfit alone, so the
        // chain shirt he wears would lie 12 mm in the ground; worn, it meets the ground and lifts him out of it, on both
        // sides (the figure's skeleton wearing its garments; render.py's pose_at keeping the contacts with them).
        new("assembly-human-male-geared-death",
            Man(("skin", "Deep"), ("hair", "Ash"), ("eyes", "Blue"), ("brows", "Ember"), ("eye.white", "Ember"), ("cloth", "Green"), ("cloth.trim", "Ember"),
                ("trousers", "Tan"), ("leather", "Black"), ("buckle", "Ember")),
            Geared, "side", 720, 400, "death", 60 / 60.0, GameLibrary),
        // GM2 step 9a (GM2-P30), in the game's own data and swatches: the human's eight presets in the sheet's three-quarter
        // view; the four new bodies plain (the first face and hair, the approved look's own colours) from the front; and six
        // looks chosen for their demanding parts. (The twin braids' look at three-quarter: its close-up, and step 5's red
        // close-up with the Ranger's long hair, show faint seams where hair locks meet, an engine task's before GM2 is ticked,
        // GM2-P35; their files wait in claude-work/gm2/step9a-repo/pending-samples/.)
        .. new[] { "human-male-1", "human-male-2", "human-male-3", "human-male-4", "human-female-1", "human-female-2", "human-female-3", "human-female-4" }
            .Select(p => new Sample($"assembly-preset-{p}", Preset(p), new(), "three-quarter", 360, 480, GamesOwn: true)),
        .. new[] { ("male", "slender"), ("male", "strong"), ("female", "slender"), ("female", "strong") }
            .Select(b => new Sample($"assembly-human-{b.Item1}-{b.Item2}", Plain(b.Item1, b.Item2), new(), "front", 360, 480, GamesOwn: true)),
        new("assembly-human-male-strong-mane", Human("male", "strong", "face-human-male-2", "hair-mane", "beard-long", null, null,
            ("skin", "Bronze"), ("hair", "Auburn"), ("eyes", "Green"), ("cloth", "Forest"), ("cloth.trim", "Bone"), ("trousers", "Brown"), ("leather", "Tan")), new(), "three-quarter", 360, 480, GamesOwn: true),
        new("assembly-human-male-slender-mohawk", Human("male", "slender", "face-human-male-3", "hair-mohawk", "beard-none", "marks-scar-brow", "head-piece-circlet",
            ("skin", "Olive"), ("hair", "Black"), ("eyes", "Grey"), ("marking", "Pale"), ("cloth", "Black"), ("cloth.trim", "Silver"), ("buckle", "Steel")), new(), "close-up", 320, 320, GamesOwn: true),
        new("assembly-human-male-warrior-braids", Human("male", "average", "face-human-male-5", "hair-warrior-braids", "beard-braided", null, null,
            ("skin", "Fair"), ("hair", "Copper"), ("eyes", "Blue"), ("cloth", "Blue"), ("cloth.trim", "Crimson"), ("trousers", "Navy")), new(), "side", 360, 480, GamesOwn: true),
        new("assembly-human-female-strong-curls", Human("female", "strong", "face-human-female-2", "hair-curls", null, null, "head-piece-leaf-wreath",
            ("skin", "Deep"), ("hair", "Black"), ("eyes", "Amber"), ("lips", "Plum"), ("cloth", "Ochre"), ("cloth.trim", "Gold"), ("trousers", "Olive")), new(), "three-quarter", 360, 480, GamesOwn: true),
        new("assembly-human-female-slender-twin-braids", Human("female", "slender", "face-human-female-4", "hair-twin-braids", null, "marks-freckles", "head-piece-flower",
            ("skin", "Porcelain"), ("hair", "Ash blond"), ("hair.tie", "Blue"), ("eyes", "Green"), ("lips", "Rose"), ("marking", "Brown"), ("cloth", "Violet"),
            ("cloth.trim", "Bone")), new(), "three-quarter", 360, 480, GamesOwn: true),
        new("assembly-human-female-side-braid", Human("female", "average", "face-human-female-5", "hair-side-braid", null, "marks-scar-cheek", "head-piece-feather",
            ("skin", "Tan"), ("hair", "Chestnut"), ("hair.tie", "Gold"), ("eyes", "Hazel"), ("lips", "Red"), ("marking", "Rose"), ("cloth", "Teal"), ("cloth.trim", "Silver"),
            ("trousers", "Grey"), ("leather", "Black")), new(), "three-quarter", 360, 480, GamesOwn: true),
        // Steps 9b–9e, each race as the human: its eight presets in the sheet's view, its two average bodies plain from the front.
        .. Races.SelectMany(r => new[] { "male-1", "male-2", "male-3", "male-4", "female-1", "female-2", "female-3", "female-4" }
            .Select(p => new Sample($"assembly-preset-{r}-{p}", Preset($"{r}-{p}"), new(), "three-quarter", 360, 480, GamesOwn: true))),
        .. Races.SelectMany(r => new[] { "male", "female" }
            .Select(s => new Sample($"assembly-{r}-{s}-average", RacePlain(r, s), new(), "front", 360, 480, GamesOwn: true))),
    ];

    /// <summary>
    /// The colours of the approved look itself (the human's swatch of each slot whose colour is the game's look file's,
    /// GM2 step 9a's <c>swatches.DEFAULT</c>; the tie and the marking have none there: Red and Brown, the first of theirs
    /// the pictures used), with <paramref name="colours"/> over them: a colour for every slot the human has swatches of.
    /// </summary>
    static Dictionary<string, string> GameColours(params (string Slot, string Swatch)[] colours)
    {
        var all = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["skin"] = "Tan", ["hair"] = "Brown", ["hair.tie"] = "Red", ["eyes"] = "Dark", ["brows"] = "Dark", ["lips"] = "Natural", ["cloth"] = "Red", ["cloth.trim"] = "Gold",
            ["trousers"] = "Charcoal", ["marking"] = "Brown", ["eye.white"] = "Ivory", ["leather"] = "Brown", ["buckle"] = "Brass",
        };
        foreach (var (slot, swatch) in colours)
            all[slot] = swatch;
        return all;
    }

    /// <summary>A human look on <paramref name="sex"/>'s <paramref name="build"/> body with its face, hair, beard (a man's), marks and head piece (none: "None") in the game's swatches.</summary>
    static LookHero Human(string sex, string build, string face, string hair, string? beard, string? marks, string? head, params (string Slot, string Swatch)[] colours)
    {
        var parts = new Dictionary<string, string> { ["face"] = face, ["hair"] = hair, ["marks"] = marks ?? "marks-none", ["head-piece"] = head ?? "head-piece-none" };
        if (beard is not null)
            parts["beard"] = beard;
        return new LookHero { Race = "human", Sex = sex, Body = $"human-{sex}-{build}", Parts = parts, Colours = GameColours(colours) };
    }

    /// <summary>A new body plain: the first face and the first humanoid's hair (swept back, long tied), nothing else, in the approved look's own colours.</summary>
    static LookHero Plain(string sex, string build) =>
        Human(sex, build, $"face-human-{sex}-1", sex == "male" ? "hair-swept-back" : "hair-long-tied", sex == "male" ? "beard-none" : null, null, null);

    public static TheoryData<string> SampleNames => new(Samples.Select(s => s.Name));

    /// <summary>The list Blender's side reads: the data folders (the game's, then the tests'), the models' folder, the background and each sample.</summary>
    internal static string ListJson()
    {
        var scenes = new JsonArray();
        foreach (var s in Samples)
        {
            var scene = new JsonObject
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
                ["clips"] = s.Clip is null ? null : s.Library,
            };
            // A sample of the game's own data reads the game's folder alone (assemble.py: a scene's "data" over the list's).
            if (s.GamesOwn)
                scene["data"] = new JsonArray("../../../Assets/Data");
            scenes.Add(scene);
        }
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
    /// GM2 step 9a's samples (GM2-P30): the eight presets as the game's data holds them, the four new bodies and six looks,
    /// each a look the maker could make (every check of <c>look.hero</c> with the game's data passes), each new part's kind
    /// among them (every head piece and mark named in the hand-over's list of looks, the faces of both sexes).
    /// </summary>
    [Fact]
    public void TheGamesOwnSamplesAreTheHumansPresetsBodiesAndLooks()
    {
        using var folder = new ScratchFolder();
        using var data = Data(folder, gamesOwn: true);
        var own = Samples.Where(s => s.GamesOwn).ToList();
        Assert.Equal(18 + 10 * Races.Length, own.Count);
        var lookup = TestLooks.Lookup(data);
        foreach (var sample in own)
        {
            var problems = RecordChecks.Check(sample.Look, data, lookup);
            Assert.True(problems.Count == 0, $"{sample.Name}: {string.Join("; ", problems)}");
        }
        var looks = LookData.Load(data);
        // Each race's eight presets as its data holds them (the human's, then steps 9b–9e's), its average bodies plain.
        foreach (var race in Races.Prepend("human"))
            Assert.Equal(looks.PresetsOf(race).Select(p => looks.Presets[p].Look), own.Where(s => s.Name.StartsWith($"assembly-preset-{race}-", StringComparison.Ordinal)).Select(s => s.Look));
        Assert.Equal(own.Count(s => s.Name.StartsWith("assembly-preset-", StringComparison.Ordinal)), 8 * (1 + Races.Length));
        foreach (var race in Races)
            Assert.Equal([$"{race}-male-average", $"{race}-female-average"], own.Where(s => s.Name.StartsWith($"assembly-{race}-", StringComparison.Ordinal)).Select(s => s.Look.Body));
        Assert.All(own.Where(s => s.Name.StartsWith("assembly-preset-")), s => Assert.Equal("three-quarter", s.View));
        Assert.Equal(["human-female-slender", "human-female-strong", "human-male-slender", "human-male-strong"],
            own.Where(s => !s.Name.StartsWith("assembly-preset-")).Select(s => s.Look.Body).Where(b => !b.EndsWith("-average")).Distinct().Order(StringComparer.Ordinal));
        string[] demanding = ["hair-mane", "beard-long", "hair-mohawk", "head-piece-circlet", "marks-scar-brow", "hair-warrior-braids", "beard-braided", "hair-curls",
            "head-piece-leaf-wreath", "hair-twin-braids", "head-piece-flower", "marks-freckles", "hair-side-braid", "head-piece-feather", "marks-scar-cheek"];
        Assert.All(demanding, part => Assert.Contains(own, s => s.Look.Parts.Values.Contains(part)));
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

    /// <summary>The game's data with the tests' on top (the development folder's place: over the packs, as a mod's data would be), or the game's alone (<paramref name="gamesOwn"/>).</summary>
    internal static VirtualFileSystem Data(ScratchFolder folder, bool gamesOwn = false)
    {
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            if (!File.Exists(Path.Combine(folder["game/packs"], Path.GetFileName(pack))))
                File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        return new VirtualFileSystem(Application.DataMounts(new ApplicationOptions { DevelopmentFolder = gamesOwn ? null : Overlay }, folder["game"], _ => null));
    }

    /// <summary>A model's data path (<c>models/…</c>) as Blender's scene names its file (its .glb under the models' folder).</summary>
    static string FileOf(string model) => model["models/".Length..] + ".glb";

    /// <summary>The figure standing by its soles on its node's origin (GM2-P22; <see cref="Actor.Ground"/>): its model space raised by its ground.</summary>
    static Matrix4x4 Lift(HeroFigure figure) => Matrix4x4.CreateTranslation(0, -figure.Actor.Ground, 0);

    /// <summary>The figure's surface points at rest as it stands (render.py's framing): the body's, each garment's and each item's at its socket.</summary>
    static List<Vector3> RestPoints(HeroFigure figure)
    {
        var lift = Lift(figure);
        var points = FigureFraming.RestPoints(figure.Actor.Model.Data, lift);
        foreach (var garment in figure.Actor.Garments)
            points.AddRange(FigureFraming.RestPoints(garment.Model.Data, lift));
        var rest = figure.Actor.Model.Data.WorldMatrices();
        foreach (var item in figure.Actor.Items)
            points.AddRange(FigureFraming.RestPoints(item.Model.Data, item.Socket.ItemTransform(rest, lift)));
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
                var library = AnimationClip.AllOf(Path.GetFullPath(Path.Combine(Reference, sample.Library))).Single(c => c.Name == clip);
                Figure.Actor.Animator.Stack.Layers.Add(new FigureClips([library]).Layer(Figure.Skeleton));
            }
            Style = GameStyle.Load(data);
            View = new View3D(device) { Background = Colors.LinearFromHex(Background), Sun = Style.SunLight, Style = Style.Cel };
            var spec = new FigureSpec { Body = "", Look = GameStyleTests.Look, View = sample.View, Width = sample.Width, Height = sample.Height };
            var focus = FigureFraming.FocusPoint(Figure.Actor.Model.Data, "head") is { } head ? Vector3.Transform(head, Lift(Figure)) : (Vector3?)null;
            View.Camera = FigureFraming.CameraFor(spec, sample.View, RestPoints(Figure), focus);
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
        using var renderer = new Renderer3D(device) { InkCoverageOnly = inkOnly };
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
        using var tests = Data(folder);
        using var games = Data(folder, gamesOwn: true);
        foreach (var sample in Samples)
        {
            var scene = scenes.RootElement.GetProperty("scenes").EnumerateArray().Single(s => s.GetProperty("name").GetString() == sample.Name);
            using var shown = new Shown(gpu.Device, sample.GamesOwn ? games : tests, sample);
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
            // Each item where Blender put it: its socket's posed world, the figure standing by its soles, turned for the view.
            var turn = Matrix4x4.CreateRotationY(FigureFraming.TurnOf(sample.View) * MathF.PI / 180);
            var world = shown.Figure.Actor.Model.Data.WorldMatrices(shown.Figure.Actor.Animator.Pose());
            for (var i = 0; i < plan.Items.Count; i++)
            {
                var placed = shown.Figure.Actor.Items[i].Socket.ItemTransform(world, Lift(shown.Figure) * turn);
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
        using var data = Data(folder, sample.GamesOwn);
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
        using var data = Data(folder, sample.GamesOwn);
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
