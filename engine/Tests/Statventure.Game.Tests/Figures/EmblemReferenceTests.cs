using System.Numerics;
using System.Text.Json;
using Statventure.Animation;
using Statventure.Core;
using Statventure.Game.Data.Races;
using Statventure.Game.Figures;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;

namespace Statventure.Game.Tests;

/// <summary>
/// The race emblems as the engine draws them against Blender's renders (GM2 step 10; Docs/Game/Hero.md, "How it works",
/// step 10): each race's emblem (its model from models.pak, <see cref="Emblems.ModelPath"/>; its colours by the game's rule
/// from the race's accent, <see cref="Emblems.Colours"/>; the game's style from data.pak) from the pictures' camera
/// (<see cref="Emblems.PictureCamera"/>) at <see cref="Size"/> pixels, over black and over white. The scenes are written as
/// Blender's harness reads them (<c>Reference/emblem-scenes.json</c>, <c>STATVENTURE_EMBLEM_SCENES=write</c>);
/// make-blender-references.sh renders them into <c>Golden/r3d-emblem-*.png</c> and <c>-ink.png</c>, from which the game's
/// pictures are made (<see cref="EmblemPicturesTests"/>). The engine's shading (lines off) and lines are held to Blender's
/// under the emblems' limits (<see cref="ReferenceLimits.Emblem"/>: the detailed figures' with the extra lines, the total
/// ink and the coverage widened, the user's Q70 of 2026-11-21, until an engine task stops the line the engine draws where a
/// raised outline rises from the field).
/// </summary>
[Collection("GPU")]
public class EmblemReferenceTests(GpuFixture gpu)
{
    /// <summary>Set to <c>write</c>: the scene file is written.</summary>
    public const string WriteVariable = "STATVENTURE_EMBLEM_SCENES";

    /// <summary>The renders' size (the approval renders'): every picture is this render made smaller.</summary>
    public const int Size = 640;

    /// <summary>The two grounds the emblem is rendered over: its picture's colour from the black, its cover from both.</summary>
    public static readonly IReadOnlyList<(string Suffix, string Colour)> Grounds = [("", "#000000"), ("-white", "#ffffff")];

    static string Reference => Path.Combine(RaceOracle.Engine, "Tests", "Statventure.Tests", "Reference");
    internal static string ScenesPath => Path.Combine(Reference, "emblem-scenes.json");
    static string Models => Path.Combine(RaceOracle.Engine, "Assets", "Models");

    /// <summary>The game's committed packs, as the game reads them.</summary>
    internal static VirtualFileSystem Data() => new(
    [
        Mount.Pack(Path.Combine(TestPaths.Packs, "ui.pak")), Mount.Pack(Path.Combine(TestPaths.Packs, "data.pak")),
        Mount.Pack(Path.Combine(TestPaths.Packs, "models.pak")),
    ]);

    /// <summary>Every race of the game's data with an emblem, in the races' order.</summary>
    internal static IReadOnlyList<RaceEntry> Races(VirtualFileSystem data) => [.. RaceCatalogue.Load(data).Where(r => r.Race.Emblem is not null)];

    /// <summary>A scene's name: <c>emblem-&lt;emblem&gt;</c> over black, <c>-white</c> over white.</summary>
    internal static string SceneName(string emblem, string suffix) => $"emblem-{emblem}{suffix}";

    public static TheoryData<string> Names => new(EmblemNames());

    static IReadOnlyList<string> EmblemNames()
    {
        using var data = Data();
        return [.. Races(data).Select(r => r.Race.Emblem!)];
    }

    /// <summary>A race's emblem in its view: the model, the race's colours, the game's style, the pictures' camera, a ground.</summary>
    internal sealed class Shown : IDisposable
    {
        public Shown(GpuDevice device, VirtualFileSystem data, Race race, string ground)
        {
            Model = Model.Load(device, data, Emblems.ModelPath(race.Emblem!));
            Actor = new Actor(Model, new Animator(new AnimationStack(new Skeleton(Model.Data)))) { Palette = Emblems.PaletteOf(race) };
            Style = GameStyle.Load(data);
            View = new View3D(device) { Background = Colors.LinearFromHex(ground), Sun = Style.SunLight, Style = Style.Cel, Camera = Emblems.PictureCamera() };
            Scene = new SceneView(View) { Rate = 0 };
            Scene.Root.Add("emblem").Attach(Actor);
            Scene.Advance(0);
        }

        public Model Model { get; }
        public Actor Actor { get; }
        public RenderStyle Style { get; }
        public View3D View { get; }
        public SceneView Scene { get; }

        public void Dispose()
        {
            View.Dispose();
            Model.Dispose();
        }
    }

    /// <summary>The scenes as Blender's harness reads them (render_scenes.py's format), the models by their files under Assets/Models.</summary>
    internal static string SceneFile()
    {
        using var data = Data();
        var style = GameStyle.Load(data);
        var camera = Emblems.PictureCamera();
        static float[] V(Vector3 v) => [v.X, v.Y, v.Z];
        var scenes = new List<object>();
        foreach (var (_, race) in Races(data))
            foreach (var (suffix, ground) in Grounds)
                scenes.Add(new
                {
                    name = SceneName(race.Emblem!, suffix),
                    width = Size,
                    height = Size,
                    background = ground,
                    camera = new
                    {
                        projection = camera.Projection.ToString().ToLowerInvariant(),
                        position = V(camera.Position),
                        target = V(camera.Target),
                        up = V(camera.Up),
                        fieldOfViewDegrees = camera.VerticalFieldOfView * 180 / MathF.PI,
                        orthographicHeight = camera.OrthographicHeight,
                        near = camera.Near,
                        far = camera.Far,
                    },
                    objects = Array.Empty<object>(),
                    models = new[]
                    {
                        new
                        {
                            file = Emblems.ModelPath(race.Emblem!)["models/".Length..] + ".glb", matrix = new[] { new[] { 1f, 0, 0, 0 }, [0, 1f, 0, 0], [0, 0, 1f, 0], [0, 0, 0, 1f] },
                            scenery = false, pose = new Dictionary<string, float[]>(), group = "emblem", anchor = new[] { 0f, 0, 0 }, objects = (bool?)null,
                            clipTime = (double?)null, palette = (Dictionary<string, string>?)null,
                        },
                    },
                    palette = Emblems.Colours(race.Accent),
                    shadows = new Dictionary<string, string>(),
                    sun = new { direction = V(style.Sun.Direction), strength = style.Sun.Strength, angle = style.Sun.Angle },
                    style = new
                    {
                        ink = style.Ink, shadowMix = style.ShadowMix, bandEdge = style.BandEdge, outlineWidth = style.OutlineWidth, innerWidth = style.InnerWidth,
                        creaseAngle = style.CreaseAngle, hatchSpacing = style.HatchSpacing, hatchWidth = style.HatchWidth, hatchAngle = style.HatchAngle,
                        hatchInk = style.HatchInk, glowWide = style.GlowWide, glowCore = style.GlowCore, glowBoost = style.GlowBoost,
                    },
                    scale = 1f,
                    terrain = (object?)null,
                    time = 0,
                    particles = Array.Empty<object>(),
                });
        return JsonSerializer.Serialize(new
        {
            modelFolder = Path.GetRelativePath(Reference, Models).Replace('\\', '/'),
            terrainFolder = ".",
            shapes = new Dictionary<string, object>(),
            scenes,
        }, new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }

    /// <summary>
    /// The scene file Blender's references were rendered from is what the game's data makes now (the emblems, their races'
    /// accents and the rule, the game's style, the camera): a change fails here until it is written again
    /// (STATVENTURE_EMBLEM_SCENES=write) and rendered again (make-blender-references.sh with the emblem scenes' names), and
    /// the pictures made again from the renders (EmblemPicturesTests).
    /// </summary>
    [Fact]
    public void TheSceneFileIsUpToDate()
    {
        var text = SceneFile();
        if (Environment.GetEnvironmentVariable(WriteVariable) == "write")
            File.WriteAllText(ScenesPath, text);
        Assert.True(File.Exists(ScenesPath) && File.ReadAllText(ScenesPath) == text,
            $"Reference/emblem-scenes.json isn't what the game's emblems make: run the game's tests with {WriteVariable}=write (EmblemReferenceTests), then make-blender-references.sh with the emblem scenes' names");
        using var data = Data();
        Assert.Equal(Races(data).Count * Grounds.Count, JsonDocument.Parse(text).RootElement.GetProperty("scenes").GetArrayLength());
    }

    /// <summary>The view drawn at the renders' size: the shading with the lines off, or the lines alone (their coverage), as the engine's references are compared.</summary>
    internal static RgbaImage Render(GpuDevice device, View3D view, bool lines = true, bool inkOnly = false) =>
        AssemblyTests.Render(device, view, Size, Size, lines, inkOnly);

    static Race RaceOf(VirtualFileSystem data, string emblem) => Races(data).Single(r => r.Race.Emblem == emblem).Race;

    [Theory]
    [MemberData(nameof(Names))]
    public void TheEngineShadesTheEmblemAsBlenderShadedIt(string emblem)
    {
        using var data = Data();
        foreach (var (suffix, ground) in Grounds)
        {
            var name = SceneName(emblem, suffix);
            var reference = Golden.GoldenPath("r3d-" + name);
            Assert.True(File.Exists(reference), $"no Blender reference for {name}: run the tests, then Tools/Scripts/make-blender-references.sh {name}");
            using var shown = new Shown(gpu.Device, data, RaceOf(data, emblem), ground);
            var actual = Render(gpu.Device, shown.View, lines: false);
            var problems = Render3DReferenceTests.Compare(ImageFile.Load(reference), actual, ReferenceLimits.Emblem);
            var actualPath = Golden.ActualPath("r3d-" + name);
            if (problems.Length > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
                ImageFile.SavePng(actualPath, actual);
            Assert.True(problems.Length == 0, $"{name}: {problems}; render in {actualPath}");
        }
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void TheEngineInksTheEmblemAsFreestyleInkedIt(string emblem)
    {
        using var data = Data();
        var name = SceneName(emblem, "");
        var reference = Golden.GoldenPath("r3d-" + name + "-ink");
        Assert.True(File.Exists(reference), $"no Freestyle reference for {name}: run the tests, then Tools/Scripts/make-blender-references.sh {name}");
        using var shown = new Shown(gpu.Device, data, RaceOf(data, emblem), Grounds[0].Colour);
        var actual = Render(gpu.Device, shown.View, inkOnly: true);
        var problems = Render3DReferenceTests.CompareInk(Render3DReferenceTests.InkCoverage(ImageFile.Load(reference), alpha: true),
            Render3DReferenceTests.InkCoverage(actual, alpha: false), actual.Width, ReferenceLimits.Emblem);
        var actualPath = Golden.ActualPath("r3d-" + name + "-ink");
        if (problems.Length > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        Assert.True(problems.Length == 0, $"{name}: {problems}; lines in {actualPath}");
    }

    /// <summary>The emblems' limits are the detailed figures' with three widened where the emblems measured past them (the user's Q70).</summary>
    [Fact]
    public void TheEmblemsLimitsAreTheDetailedFiguresWidenedWhereMeasured() =>
        Assert.Equal(ReferenceLimits.DetailedFigure with { Extra = 0.143, TotalInk = 0.186, Coverage = 0.154 }, ReferenceLimits.Emblem);

    /// <summary>
    /// Each widened limit bites: the emblem that measured worst for it (the Half-Beast's claw marks: extra lines 12.77 % on the
    /// GPU and 13.02 % on lavapipe, ink 16.88–16.95 % over Freestyle's, coverage 0.139–0.140) fails once that limit is set just
    /// under what it measured on both drivers, and passes under the limits as set.
    /// </summary>
    [Theory]
    [InlineData("extra")]
    [InlineData("ink")]
    [InlineData("coverage")]
    public void EachWidenedLimitBites(string limit)
    {
        var limits = ReferenceLimits.Emblem;
        var tighter = limit switch
        {
            "extra" => limits with { Extra = 0.125 },
            "ink" => limits with { TotalInk = 0.165 },
            "coverage" => limits with { Coverage = 0.135 },
            _ => throw new ArgumentException(limit),
        };
        using var data = Data();
        using var shown = new Shown(gpu.Device, data, RaceOf(data, "half-beast"), Grounds[0].Colour);
        var expected = Render3DReferenceTests.InkCoverage(ImageFile.Load(Golden.GoldenPath("r3d-" + SceneName("half-beast", "") + "-ink")), alpha: true);
        var actual = Render3DReferenceTests.InkCoverage(Render(gpu.Device, shown.View, inkOnly: true), alpha: false);
        Assert.Equal("", Render3DReferenceTests.CompareInk(expected, actual, Size, limits));
        Assert.NotEqual("", Render3DReferenceTests.CompareInk(expected, actual, Size, tighter));
    }
}
