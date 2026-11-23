using System.Globalization;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Statventure.Core;
using Statventure.Game.Figures;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;

namespace Statventure.Game.Tests;

/// <summary>
/// The live views against Blender (GM2 step 11; Docs/Game/Hero.md, "How it works", step 11): the figure each screen shows
/// (the sheet's hero, the race carousel's first preset, the maker's look after a preset is picked), as the screen lays its
/// view out at the game's first window (1440 × 950) at device scales 1 and 1.65, and the carousel's figure of each other
/// race with a body of its own at 1 (GM2 step 9: <see cref="LiveViewData.Races"/>, a race pack's body), at a moment of
/// its stand, is written as a list (<c>Reference/live-view-looks.json</c>: the look, the view's size, ground, scale, turn
/// and sun, the clip's moment; <c>STATVENTURE_LIVE_VIEWS=write</c>) that the humanoid scripts' assemble.py assembles and
/// frames on its own from the data as step 5's looks are (<c>make-blender-references.sh</c>, step 7:
/// <c>Reference/live-view-scenes.json</c>, rendered into <c>Golden/r3d-live-*.png</c>); the view is held to it: the same
/// models, palette and camera, then the shading and the
/// lines under <see cref="ReferenceLimits.DetailedFigure"/> (the user's Q55, Q58, Q60; unchanged). The carousel's rocking
/// emblem of each of step 10's nine races, at 1 and 1.65, is written as its view holds it
/// (<c>Reference/live-emblem-scenes.json</c>) and held to Blender's render of that scene under the emblems' own limits
/// (<see cref="ReferenceLimits.LiveEmblem"/>: the emblems' with two widened, the user's Q81).
/// </summary>
[Collection("GPU")]
public class LiveViewReferenceTests(GpuFixture gpu, Xunit.Abstractions.ITestOutputHelper output)
{
    static string Reference => Path.Combine(RaceOracle.Engine, "Tests", "Statventure.Tests", "Reference");
    static string ListPath => Path.Combine(Reference, "live-view-looks.json");
    static string ScenesPath => Path.Combine(Reference, "live-view-scenes.json");
    static string EmblemScenesPath => Path.Combine(Reference, "live-emblem-scenes.json");

    /// <summary>The stand's moment the samples are drawn at (seconds into its loop: frame 60, a key of the clip).</summary>
    public const double Moment = 1;

    /// <summary>The emblem's moment (seconds of rocking: 35° × sin 45°, about 24.7° toward the screen's right).</summary>
    public const double EmblemMoment = 1;

    /// <summary>The ground the emblem's samples are drawn on (its view is transparent over the medallion; Blender's needs one).</summary>
    const string EmblemGround = "#2a1f1a";

    /// <summary>A screen's figure at a scale (the carousel's on <paramref name="Race"/>: its first race when null).</summary>
    public sealed record Sample(string Name, string Screen, float Scale, string? Race = null);

    public static IReadOnlyList<Sample> Samples { get; } =
    [
        new("live-sheet-1", "sheet", 1), new("live-sheet-1.65", "sheet", 1.65f),
        new("live-races-1", "races", 1), new("live-races-1.65", "races", 1.65f),
        new("live-maker-1", "maker", 1), new("live-maker-1.65", "maker", 1.65f),
        .. LiveViewData.Races.Select(race => new Sample($"live-races-{race}-1", "races", 1, race)),
    ];

    /// <summary>
    /// The size the live emblems' scenes are held to Blender at (the manager's GM2-P47, 2026-11-22): step 10's renders'
    /// (<see cref="EmblemReferenceTests.Size"/>), the one size at which an emblem's look was approved (its small pictures are
    /// that render scaled down, GM2-P34) and the emblems' limits measured (Q70); at the carousel's own sizes Blender's
    /// direct render was never a look anyone chose (<see cref="TheLiveEmblemsAtTheirOwnSizesAreMeasuredAgainstTheirLargeRenderScaledDown"/>
    /// keeps those as a record).
    /// </summary>
    public const int EmblemSize = EmblemReferenceTests.Size;

    /// <summary>
    /// The carousel's live emblem of every race with one (step 10's nine): the scene its view holds (its own camera, its
    /// own light, Q79, the race's palette, the pose at <see cref="EmblemMoment"/> of its rocking) rendered at
    /// <see cref="EmblemSize"/> × <see cref="EmblemSize"/>, held to Blender's render of it under the live emblems' limits
    /// (<see cref="ReferenceLimits.LiveEmblem"/>, the user's Q81).
    /// </summary>
    public static IReadOnlyList<Sample> EmblemSamples { get; } = EmblemSamplesOf();

    static IReadOnlyList<Sample> EmblemSamplesOf()
    {
        using var data = EmblemReferenceTests.Data();
        return [.. EmblemReferenceTests.Races(data).Select(r => new Sample($"live-emblem-{r.Race.Emblem}", "races", 1, r.Id))];
    }

    public static TheoryData<string> SampleNames => new(Samples.Select(s => s.Name));
    public static TheoryData<string> EmblemSampleNames => new(EmblemSamples.Select(s => s.Name));

    /// <summary>A sample's screen with its live figure at the stand's moment.</summary>
    internal sealed class Shown : IDisposable
    {
        readonly LiveViewData _data = new();

        public Shown(GpuDevice device, Sample sample)
        {
            Sample = sample;
            Screens = new LiveScreens(device, _data, scale: sample.Scale);
            switch (sample.Screen)
            {
                case "sheet":
                    Screens.ReadyHero(LiveViewData.ManLook());
                    _figure = (LiveFigure?)Screens.Sheet.ViewIn(Screens.Surface.Sheet);
                    break;
                case "races":
                    Screens.S.NewHero();
                    Screens.H.Frame();
                    if (sample.Race is { } race)
                    {
                        Screens.S.Click(Screens.H.Q($"#races-strip .races-strip-item[data-race=\"{race}\"]"));
                        Screens.H.Frame();
                    }
                    _figure = (LiveFigure?)Screens.Races.ViewIn(Screens.Surface.Races);
                    Emblem = (LiveEmblem?)Screens.Races.ViewIn(Screens.Surface.Emblem);
                    break;
                default:
                    Screens.S.NewHero();
                    Screens.H.Q("#races-choose").Click();
                    Screens.H.Frame();
                    Screens.H.Q($"#maker-presets [data-preset=\"{LiveViewData.Woman}\"]").Click();
                    Screens.H.Frame();
                    _figure = (LiveFigure?)Screens.Maker.ViewIn(Screens.Surface.Maker);
                    break;
            }
            if (_figure is { } figure)
            {
                figure.Actor!.Animator.Seek(Moment);
                figure.Scene.Advance(0);
            }
            Emblem?.Seek(EmblemMoment);
        }

        readonly LiveFigure? _figure;

        public Sample Sample { get; }
        public LiveScreens Screens { get; }
        /// <summary>The screen's figure (a race with no look shows none).</summary>
        public LiveFigure Figure => _figure ?? throw new InvalidOperationException($"{Sample.Name}: no figure shows");
        public LiveEmblem? Emblem { get; }

        public void Dispose()
        {
            Screens.Dispose();
            _data.Dispose();
        }
    }

    static string Hex(Vector4 linear)
    {
        var srgb = Colors.ToSrgb(linear);
        return string.Create(CultureInfo.InvariantCulture, $"#{(int)MathF.Round(srgb.X * 255):x2}{(int)MathF.Round(srgb.Y * 255):x2}{(int)MathF.Round(srgb.Z * 255):x2}");
    }

    static JsonArray V(Vector3 v) => [v.X, v.Y, v.Z];

    /// <summary>render.py's name for a turn (its three-quarter, or degrees).</summary>
    static string ViewOf(float turn) => turn == Portraits.ThreeQuarter ? "three-quarter" : turn.ToString(CultureInfo.InvariantCulture);

    /// <summary>The list Blender's side assembles: each sample's look with its view as the screen shows it.</summary>
    internal static string ListJson(GpuDevice device)
    {
        var scenes = new JsonArray();
        foreach (var sample in Samples)
        {
            using var shown = new Shown(device, sample);
            var figure = shown.Figure;
            var style = GameStyle.Load(shown.Screens.H.Document.Files!);
            var scene = new JsonObject
            {
                ["name"] = sample.Name,
                ["look"] = JsonNode.Parse(RecordSerializer.ToJson(figure.Look!)),
                ["gear"] = new JsonObject { ["garments"] = new JsonArray(), ["items"] = new JsonArray(), ["colours"] = new JsonObject() },
                ["view"] = ViewOf(Portraits.TurnOf(Portraits.Light)),
                ["width"] = figure.Size.Width,
                ["height"] = figure.Size.Height,
                ["clip"] = "stand",
                ["time"] = Moment,
                ["clips"] = "../../../Assets/Models/humanoid/clips.glb",
                ["background"] = Hex(figure.View.Background!.Value),
                ["scale"] = sample.Scale,
            };
            // The portraits' own sun only where it isn't the game's (Portraits.Light).
            if (figure.View.Sun != style.SunLight)
                scene["sun"] = new JsonObject { ["direction"] = V(figure.View.Sun.Direction), ["strength"] = figure.View.Sun.Strength, ["angle"] = figure.View.Sun.Angle };
            scenes.Add(scene);
        }
        var list = new JsonObject
        {
            ["format"] = "statventure.assembly",
            ["version"] = 1,
            ["data"] = new JsonArray("../../../Assets/Data", "live-view-data"),
            ["models"] = "../../../Assets/Models",
            ["background"] = "#2a1f1a",
            ["scenes"] = scenes,
        };
        return list.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }

    static float[][] Columns(Matrix4x4 m) => [[m.M11, m.M12, m.M13, m.M14], [m.M21, m.M22, m.M23, m.M24], [m.M31, m.M32, m.M33, m.M34], [m.M41, m.M42, m.M43, m.M44]];

    /// <summary>
    /// The emblems' scenes as their views hold them (render_scenes.py's format: step 10's model by its file under
    /// <c>Assets/Models</c>, the palette the view gives it, the camera, sun and style), on a ground of the tests'.
    /// </summary>
    internal static string EmblemScenesJson(GpuDevice device)
    {
        var scenes = new List<object>();
        foreach (var sample in EmblemSamples)
        {
            using var shown = new Shown(device, sample);
            var emblem = shown.Emblem!;
            var placed = emblem.Node.Children.Single();
            var model = (PlacedModel)placed.Attachments.Single();
            var camera = emblem.View.Camera;
            var style = emblem.View.Style;
            var sun = emblem.View.Sun;
            scenes.Add(new
            {
                name = sample.Name,
                width = EmblemSize,
                height = EmblemSize,
                background = EmblemGround,
                camera = new
                {
                    projection = camera.Projection.ToString().ToLowerInvariant(),
                    position = new[] { camera.Position.X, camera.Position.Y, camera.Position.Z },
                    target = new[] { camera.Target.X, camera.Target.Y, camera.Target.Z },
                    up = new[] { camera.Up.X, camera.Up.Y, camera.Up.Z },
                    fieldOfViewDegrees = camera.VerticalFieldOfView * 180 / MathF.PI,
                    orthographicHeight = camera.OrthographicHeight,
                    near = camera.Near,
                    far = camera.Far,
                },
                objects = Array.Empty<object>(),
                models = new object[]
                {
                    new
                    {
                        file = "Assets/Models/" + Emblems.ModelPath(emblem.Emblem)["models/".Length..] + ".glb", matrix = Columns(placed.World), scenery = false, pose = (object?)null,
                        group = "emblem", anchor = new[] { placed.World.Translation.X, placed.World.Translation.Y, placed.World.Translation.Z },
                        objects = (bool?)null, clipTime = (double?)null, palette = (Dictionary<string, string>?)null,
                    },
                },
                palette = model.Palette is { } palette
                    ? palette.Slots.OrderBy(p => p.Key, StringComparer.Ordinal).ToDictionary(p => p.Key, p => Hex(p.Value.Lit))
                    : new Dictionary<string, string>(),
                shadows = new Dictionary<string, string>(),
                sun = new { direction = new[] { sun.Direction.X, sun.Direction.Y, sun.Direction.Z }, strength = sun.Strength, angle = sun.Angle },
                style = new
                {
                    ink = Hex(Colors.ToLinear(style.Ink)),
                    shadowMix = style.ShadowMix, bandEdge = style.BandEdge,
                    outlineWidth = style.OutlineWidth * sample.Scale, innerWidth = style.InnerWidth * sample.Scale, creaseAngle = style.CreaseAngle,
                    hatchSpacing = style.HatchSpacing, hatchWidth = style.HatchWidth, hatchAngle = style.HatchAngle, hatchInk = style.HatchInk,
                    glowWide = style.GlowWide, glowCore = style.GlowCore, glowBoost = style.GlowBoost,
                },
                scale = sample.Scale,
                terrain = (object?)null,
                time = 0,
                particles = Array.Empty<object>(),
            });
        }
        return JsonSerializer.Serialize(new { modelFolder = "../../..", terrainFolder = ".", shapes = new Dictionary<string, object>(), scenes },
            new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }

    /// <summary>
    /// The list and the emblem's scenes are what the screens show (written with STATVENTURE_LIVE_VIEWS=write; then
    /// make-blender-references.sh renders them): a change of a screen's layout, the samples, the look, the portraits' light or
    /// the data fails here until they are written and rendered again.
    /// </summary>
    [Fact]
    public void TheListIsUpToDate()
    {
        var list = ListJson(gpu.Device);
        var emblems = EmblemScenesJson(gpu.Device);
        if (Environment.GetEnvironmentVariable(LiveViewData.WriteVariable) == "write")
        {
            File.WriteAllText(ListPath, list);
            File.WriteAllText(EmblemScenesPath, emblems);
        }
        Assert.True(File.Exists(ListPath) && File.ReadAllText(ListPath) == list && File.Exists(EmblemScenesPath) && File.ReadAllText(EmblemScenesPath) == emblems,
            $"Reference/live-view-looks.json or live-emblem-scenes.json isn't what the screens show: run the game's tests with {LiveViewData.WriteVariable}=write (LiveViewReferenceTests), then make-blender-references.sh with the live scenes' names");
    }

    static string FileOf(string model) => model["models/".Length..] + ".glb";

    static Vector3 V(JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle(), e[2].GetSingle());

    static void Near(string what, Vector3 expected, Vector3 actual, float tolerance) =>
        Assert.True(Vector3.Distance(expected, actual) <= tolerance * MathF.Max(1, expected.Length()), $"{what}: expected {expected}, got {actual}");

    /// <summary>
    /// Each live figure holds what Blender's assembly of its look holds (Reference/live-view-scenes.json): the body's model
    /// and the parts' by their files, the palette colour for colour, the camera assemble.py framed for the view's size number
    /// for number (so the portraits frame as render.py does), the sun and the ground.
    /// </summary>
    [Fact]
    public void TheViewsHoldWhatBlendersAssemblyHolds()
    {
        Assert.True(File.Exists(ScenesPath), "no Reference/live-view-scenes.json: run the tests, then make-blender-references.sh (it assembles the looks)");
        using var scenes = JsonDocument.Parse(File.ReadAllText(ScenesPath));
        Assert.Equal("../../../Assets/Models", scenes.RootElement.GetProperty("modelFolder").GetString());
        Assert.Equal(Samples.Select(s => s.Name), scenes.RootElement.GetProperty("scenes").EnumerateArray().Select(s => s.GetProperty("name").GetString()));
        foreach (var sample in Samples)
        {
            var scene = scenes.RootElement.GetProperty("scenes").EnumerateArray().Single(s => s.GetProperty("name").GetString() == sample.Name);
            using var shown = new Shown(gpu.Device, sample);
            var figure = shown.Figure;
            var plan = figure.Figure!.Plan;
            Assert.Equal([FileOf(plan.Body), .. plan.GarmentModels.Select(FileOf)], scene.GetProperty("models").EnumerateArray().Select(m => m.GetProperty("file").GetString()));
            Assert.Equal(plan.Colours.OrderBy(c => c.Key, StringComparer.Ordinal),
                scene.GetProperty("palette").EnumerateObject().Select(p => KeyValuePair.Create(p.Name, p.Value.GetString()!)).OrderBy(c => c.Key, StringComparer.Ordinal));
            var camera = scene.GetProperty("camera");
            var view = figure.View.Camera;
            Near(sample.Name, V(camera.GetProperty("position")), view.Position, 1e-4f);
            Near(sample.Name, V(camera.GetProperty("target")), view.Target, 1e-4f);
            Assert.True(MathF.Abs(camera.GetProperty("orthographicHeight").GetSingle() - view.OrthographicHeight) < 1e-4f, $"{sample.Name}: the framed height");
            Assert.Equal(view.Projection.ToString().ToLowerInvariant(), camera.GetProperty("projection").GetString());
            Assert.Equal((figure.Size.Width, figure.Size.Height), (scene.GetProperty("width").GetInt32(), scene.GetProperty("height").GetInt32()));
            Near(sample.Name, V(scene.GetProperty("sun").GetProperty("direction")), figure.View.Sun.Direction, 1e-6f);
            Assert.Equal(Hex(figure.View.Background!.Value), scene.GetProperty("background").GetString());
            // The turn and the stand: each model turned as the figure's node, raised by its ground as the actor draws it
            // (its Placement, the node's world after the lift; GM2-P22: standing by its soles).
            var matrix = scene.GetProperty("models")[0].GetProperty("matrix").EnumerateArray().Select(c => c.EnumerateArray().Select(v => v.GetSingle()).ToArray()).ToArray();
            var placed = figure.Actor!.Placement;
            Near(sample.Name, new Vector3(matrix[0][0], matrix[0][1], matrix[0][2]), new Vector3(placed.M11, placed.M12, placed.M13), 1e-6f);
            Near(sample.Name + " (its place)", new Vector3(matrix[3][0], matrix[3][1], matrix[3][2]), placed.Translation, 1e-5f);
            Assert.NotEqual(0, figure.Actor.Ground);
        }
    }

    /// <summary>The view's picture as its surface shows it at its scale: the shading with the lines off, or the lines alone (their coverage).</summary>
    internal static RgbaImage Render(GpuDevice device, View3D view, int width, int height, float scale, Vector4? ground = null, bool lines = true, bool inkOnly = false)
    {
        var style = lines ? view.Style : view.Style with { OutlineWidth = 0, InnerWidth = 0 };
        using var target = new RenderTarget3D(device, width, height);
        using var renderer = new Renderer3D(device) { InkCoverageOnly = inkOnly };
        var frame = device.BeginFrame();
        renderer.Render(frame, target, view.Camera, view.Items, ground ?? view.Background ?? Vector4.Zero, view.Sun, style, scale, view.Time, view.Particles);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(width, height, target.Resolved.ReadPixels());
    }

    void Compare(string name, View3D view, (int Width, int Height) size, float scale, ReferenceLimits limits, Vector4? ground = null)
    {
        var reference = Golden.GoldenPath("r3d-" + name);
        Assert.True(File.Exists(reference), $"no Blender reference for {name}: run the tests, then Tools/Scripts/make-blender-references.sh {name}");
        var actual = Render(gpu.Device, view, size.Width, size.Height, scale, ground, lines: false);
        var problems = Render3DReferenceTests.Compare(ImageFile.Load(reference), actual, limits);
        var actualPath = Golden.ActualPath("r3d-" + name);
        if (problems.Length > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        Assert.True(problems.Length == 0, $"{name}: {problems}; render in {actualPath}");
        var inkReference = Golden.GoldenPath("r3d-" + name + "-ink");
        Assert.True(File.Exists(inkReference), $"no Freestyle reference for {name}: run the tests, then Tools/Scripts/make-blender-references.sh {name}");
        var lines = Render(gpu.Device, view, size.Width, size.Height, scale, ground, inkOnly: true);
        var inkProblems = Render3DReferenceTests.CompareInk(Render3DReferenceTests.InkCoverage(ImageFile.Load(inkReference), alpha: true),
            Render3DReferenceTests.InkCoverage(lines, alpha: false), lines.Width, limits);
        var linesPath = Golden.ActualPath("r3d-" + name + "-ink");
        if (inkProblems.Length > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(linesPath, lines);
        Assert.True(inkProblems.Length == 0, $"{name}: {inkProblems}; lines in {linesPath}");
    }

    /// <summary>Each screen's figure shades and inks as Blender's assembly of the same look in the same view.</summary>
    [Theory]
    [MemberData(nameof(SampleNames))]
    public void TheLiveFigureDrawsAsBlendersAssembly(string name)
    {
        var sample = Samples.Single(s => s.Name == name);
        using var shown = new Shown(gpu.Device, sample);
        Compare(name, shown.Figure.View, shown.Figure.Size, sample.Scale, ReferenceLimits.DetailedFigure);
    }

    /// <summary>The carousel's rocking emblem of each race (step 10's) draws as Blender's render of its scene, under the live emblems' limits (Q81).</summary>
    [Theory]
    [MemberData(nameof(EmblemSampleNames))]
    public void TheLiveEmblemDrawsAsBlenderRendersItsScene(string name)
    {
        var sample = EmblemSamples.Single(s => s.Name == name);
        using var shown = new Shown(gpu.Device, sample);
        Compare(name, shown.Emblem!.View, (EmblemSize, EmblemSize), sample.Scale, ReferenceLimits.LiveEmblem, Colors.LinearFromHex(EmblemGround));
    }

    /// <summary>The live emblems' limits are the emblems' with two widened where the turned medallions measured past them (the user's Q81).</summary>
    [Fact]
    public void TheLiveEmblemsLimitsAreTheEmblemsWidenedWhereMeasured() =>
        Assert.Equal(ReferenceLimits.Emblem with { Missed = 0.036, InsideEvery = 3100 }, ReferenceLimits.LiveEmblem);

    /// <summary>
    /// Each widened limit bites: the live emblem that measured worst for it fails once that limit is set back to the
    /// emblems' (<see cref="ReferenceLimits.Emblem"/>) and passes under the live emblems' as set: the dark elf's missed
    /// lines (its eclipse crescent's contour, 3.28 % on both drivers, against the emblems' 2 %); the elf's inside pixels
    /// (97 on the GPU, 120 on lavapipe), held under one in 4,300 (95 at 640 px) on both drivers, the emblems' one in 4,200
    /// letting the GPU's 97 through.
    /// </summary>
    [Theory]
    [InlineData("missed", "dark-elf")]
    [InlineData("inside", "elf")]
    public void EachOfTheLiveEmblemsWidenedLimitsBites(string limit, string emblem)
    {
        var limits = ReferenceLimits.LiveEmblem;
        var tighter = limit switch
        {
            "missed" => limits with { Missed = ReferenceLimits.Emblem.Missed },
            "inside" => limits with { InsideEvery = 4300 },
            _ => throw new ArgumentException(limit),
        };
        var sample = EmblemSamples.Single(s => s.Name == $"live-emblem-{emblem}");
        using var shown = new Shown(gpu.Device, sample);
        var view = shown.Emblem!.View;
        var ground = Colors.LinearFromHex(EmblemGround);
        string Problems(ReferenceLimits set)
        {
            if (limit == "inside")
                return Render3DReferenceTests.Compare(ImageFile.Load(Golden.GoldenPath("r3d-" + sample.Name)), Render(gpu.Device, view, EmblemSize, EmblemSize, 1, ground, lines: false), set);
            var lines = Render(gpu.Device, view, EmblemSize, EmblemSize, 1, ground, inkOnly: true);
            return Render3DReferenceTests.CompareInk(Render3DReferenceTests.InkCoverage(ImageFile.Load(Golden.GoldenPath("r3d-" + sample.Name + "-ink")), alpha: true),
                Render3DReferenceTests.InkCoverage(lines, alpha: false), EmblemSize, set);
        }
        Assert.Equal("", Problems(limits));
        Assert.NotEqual("", Problems(tighter));
    }

    /// <summary>An opaque render scaled down to <paramref name="size"/> by GM2-P34's filter (EmblemPicturesTests.Picture: Lanczos of three lobes, premultiplied).</summary>
    static RgbaImage ScaledDown(RgbaImage render, int size)
    {
        var master = new double[render.Width * render.Height * 4];
        for (var i = 0; i < render.Width * render.Height; i++)
        {
            for (var c = 0; c < 3; c++)
                master[i * 4 + c] = render.Pixels[i * 4 + c] / 255.0;
            master[i * 4 + 3] = 1;
        }
        return EmblemPicturesTests.Picture(master, render.Width, render.Height, size);
    }

    /// <summary>The engine's line coverage of a render (white on black) scaled down to <paramref name="size"/> by the same filter, as Freestyle's alpha.</summary>
    static RgbaImage InkScaledDown(RgbaImage lines, int size)
    {
        var coverage = Render3DReferenceTests.InkCoverage(lines, alpha: false);
        var master = new double[coverage.Length * 4];
        for (var i = 0; i < coverage.Length; i++)
            master[i * 4 + 3] = coverage[i];
        return EmblemPicturesTests.Picture(master, lines.Width, lines.Height, size);
    }

    /// <summary>The carousel's own sizes of the live emblems: 94 px at device scale 1 and 156 at 1.65 (the medallion's inside).</summary>
    public static IReadOnlyList<(int Size, float Scale)> EmblemOwnSizes { get; } = [(94, 1), (156, 1.65f)];

    /// <summary>
    /// The live emblems at the carousel's own sizes, a measured record (the manager's GM2-P47, 2026-11-22; Docs/Game/Hero.md,
    /// "How it works", step 11, has the table as measured): each of the nine drawn at 94 px (scale 1) and 156 px (1.65)
    /// against its own <see cref="EmblemSize"/> px render (the same view: camera, light, palette, pose) scaled down by
    /// GM2-P34's filter, as step 10's pictures are made: the shading (lines off) and the lines, measured with no limit
    /// (the edges' mean, the edge pixels past 0.7 of their contrast, the missed and the extra line pixels) and written to
    /// the test's output (and to STATVENTURE_LIVE_EMBLEM_RECORD's file when set). Not held to a limit: of the project's limit sets
    /// none is met by all eighteen (<see cref="ReferenceLimits.M6"/>, <see cref="ReferenceLimits.DetailedStage"/>,
    /// <see cref="ReferenceLimits.DetailedFigure"/> and <see cref="ReferenceLimits.Emblem"/> each tried and named below).
    /// </summary>
    [Fact]
    public void TheLiveEmblemsAtTheirOwnSizesAreMeasuredAgainstTheirLargeRenderScaledDown()
    {
        var none = ReferenceLimits.M6 with { Missed = 0, Extra = 0, TotalInk = 0, Coverage = 0, Unlike = 0, EdgeMean = 0 };
        var sets = new (string Name, ReferenceLimits Limits)[]
        {
            ("M6", ReferenceLimits.M6), ("DetailedStage", ReferenceLimits.DetailedStage), ("DetailedFigure", ReferenceLimits.DetailedFigure),
            ("Emblem", ReferenceLimits.Emblem),
        };
        var met = sets.ToDictionary(s => s.Name, _ => 0);
        var lines = new List<string> { $"device: {gpu.Device.Name}" };
        var ground = Colors.LinearFromHex(EmblemGround);
        foreach (var sample in EmblemSamples)
        {
            using var shown = new Shown(gpu.Device, sample);
            var view = shown.Emblem!.View;
            var large = Render(gpu.Device, view, EmblemSize, EmblemSize, 1, ground, lines: false);
            var largeInk = Render(gpu.Device, view, EmblemSize, EmblemSize, 1, ground, inkOnly: true);
            foreach (var (size, scale) in EmblemOwnSizes)
            {
                var expected = ScaledDown(large, size);
                var expectedInk = Render3DReferenceTests.InkCoverage(InkScaledDown(largeInk, size), alpha: true);
                var small = Render(gpu.Device, view, size, size, scale, ground, lines: false);
                var smallInk = Render3DReferenceTests.InkCoverage(Render(gpu.Device, view, size, size, scale, ground, inkOnly: true), alpha: false);
                var shade = Render3DReferenceTests.Compare(expected, small, edgeMean: 0);
                var ink = Render3DReferenceTests.CompareInk(expectedInk, smallInk, size, none);
                lines.Add($"{sample.Name} at {size} px ({scale.ToString(CultureInfo.InvariantCulture)}): SHADE {shade} | INK {ink}");
                foreach (var (name, limits) in sets)
                    if (Render3DReferenceTests.Compare(expected, small, limits) == "" && Render3DReferenceTests.CompareInk(expectedInk, smallInk, size, limits) == "")
                        met[name]++;
            }
        }
        var count = EmblemSamples.Count * EmblemOwnSizes.Count;
        lines.Add("met by: " + string.Join(", ", sets.Select(s => $"{s.Name} {met[s.Name]} of {count}")));
        foreach (var line in lines)
            output.WriteLine(line);
        if (Environment.GetEnvironmentVariable("STATVENTURE_LIVE_EMBLEM_RECORD") is { Length: > 0 } file)
            File.WriteAllLines(file, lines);
        Assert.Equal(18, count);
    }
}
