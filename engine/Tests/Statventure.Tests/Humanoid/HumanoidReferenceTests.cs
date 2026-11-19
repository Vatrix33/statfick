using System.Text.Json;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Sandbox.Figure;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Statventure.Tests.Sandbox;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// The humanoid family's models as the engine draws them against Blender's renders (GM2 step 4a; Docs/Game/Hero.md, "How
/// it works", step 4a): the human man and woman assembled as the game assembles a figure (GM2-P8: the body with its
/// plain outfit, the face and the hair as garments on its bones) in the game's look, from the four sides at rest and in a
/// close-up of the face; then wearing the sample gear (the chain shirt as a garment, the sword and the shield on their
/// sockets: "Rendered gear") posed by the clip library's test clip, at three-quarter and from the battle's side. Drawn by
/// the sandbox's figure mode (the engine's own view of a figure) and written as Blender's harness reads them
/// (Reference/humanoid-scenes.json; make-blender-references.sh renders them into Golden/r3d-humanoid-*.png and -ink.png);
/// held to the detailed figures' limits (<see cref="ReferenceLimits.DetailedFigure"/>, measured on these scenes and
/// approved by the user, 2026-11-16, Q55, and tightened after GM2's ink task, 2026-11-18, Q58 and Q60): the shading (lines off) by <see cref="Render3DReferenceTests.Compare"/>, the lines
/// by <see cref="Render3DReferenceTests.CompareInk"/>.
/// </summary>
[Collection("GPU")]
public class HumanoidReferenceTests(GpuFixture gpu)
{
    static FigureSpec Spec(string sex, string view, bool gear, int width, int height) => new()
    {
        Body = HumanoidFiles.Body(sex),
        Garments = gear ? [HumanoidFiles.Face(sex), HumanoidFiles.Hair(sex), HumanoidFiles.ChainShirt(sex)] : [HumanoidFiles.Face(sex), HumanoidFiles.Hair(sex)],
        Items = gear ? [(HumanoidFiles.Sword, "socket.hand.R"), (HumanoidFiles.Shield, "socket.shield.L")] : [],
        Clips = gear ? HumanoidFiles.TestClips : null,
        Look = HumanoidFiles.Ember,
        View = view,
        Width = width,
        Height = height,
    };

    public static IReadOnlyList<FigureReferenceTests.FigureScene> Scenes { get; } = MakeScenes();

    static List<FigureReferenceTests.FigureScene> MakeScenes()
    {
        var scenes = new List<FigureReferenceTests.FigureScene>();
        foreach (var sex in new[] { "male", "female" })
        {
            foreach (var view in new[] { "front", "three-quarter", "side", "back" })
                scenes.Add(new($"humanoid-{sex}-{view}", Spec(sex, view, gear: false, 360, 480), 0));
            scenes.Add(new($"humanoid-{sex}-close-up", Spec(sex, "close-up", gear: false, 320, 320), 0));
            scenes.Add(new($"humanoid-{sex}-gear", Spec(sex, "three-quarter", gear: true, 360, 480), 0.5));
            scenes.Add(new($"humanoid-{sex}-gear-side", Spec(sex, "side", gear: true, 360, 480), 1.25));
        }
        return scenes;
    }

    public static TheoryData<string> SceneNames => new(Scenes.Select(s => s.Name));

    internal static string SceneFile(GpuDevice device) =>
        FigureView.SceneFile(Scenes.Select(s =>
        {
            using var figure = new FigureView(device, s.Spec);
            figure.Seek(s.Time);
            return figure.SceneOf(s.Name, HumanoidFiles.Models);
        }).ToList(), HumanoidFiles.Models, FigureFiles.Reference);

    /// <summary>
    /// The scene file Blender's references were rendered from is the one the figure mode writes now: a change to the
    /// scenes, the models or the look fails here until the file is written again (STATVENTURE_HUMANOID_SCENES=write) and
    /// the references rendered again from it (make-blender-references.sh humanoid-…).
    /// </summary>
    [Fact]
    public void TheSceneFileIsUpToDate()
    {
        var path = Path.Combine(FigureFiles.Reference, "humanoid-scenes.json");
        var text = SceneFile(gpu.Device);
        if (Environment.GetEnvironmentVariable("STATVENTURE_HUMANOID_SCENES") == "write")
            File.WriteAllText(path, text);
        Assert.True(File.Exists(path) && File.ReadAllText(path) == text,
            "Reference/humanoid-scenes.json isn't what the figure mode writes: run with STATVENTURE_HUMANOID_SCENES=write, then make-blender-references.sh with the humanoid scenes' names");
        Assert.Equal(Scenes.Count, JsonDocument.Parse(text).RootElement.GetProperty("scenes").GetArrayLength());
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void TheEngineShadesTheFigureAsBlenderShadedIt(string name)
    {
        var scene = Scenes.Single(s => s.Name == name);
        var reference = Golden.GoldenPath("r3d-" + name);
        Assert.True(File.Exists(reference), $"no Blender reference for {name}: run the tests, then Tools/Scripts/make-blender-references.sh {name}");
        using var figure = new FigureView(gpu.Device, scene.Spec);
        figure.Seek(scene.Time);
        var actual = FigureReferenceTests.Render(gpu.Device, figure, lines: false);
        var problems = Render3DReferenceTests.Compare(ImageFile.Load(reference), actual, ReferenceLimits.DetailedFigure);
        var actualPath = Golden.ActualPath("r3d-" + name);
        if (problems.Length > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        Assert.True(problems.Length == 0, $"{name}: {problems}; render in {actualPath}");
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void TheEngineInksTheFigureAsFreestyleInkedIt(string name)
    {
        var scene = Scenes.Single(s => s.Name == name);
        var reference = Golden.GoldenPath("r3d-" + name + "-ink");
        Assert.True(File.Exists(reference), $"no Freestyle reference for {name}: run the tests, then Tools/Scripts/make-blender-references.sh {name}");
        using var figure = new FigureView(gpu.Device, scene.Spec);
        figure.Seek(scene.Time);
        var actual = FigureReferenceTests.Render(gpu.Device, figure, inkOnly: true);
        var problems = Render3DReferenceTests.CompareInk(Render3DReferenceTests.InkCoverage(ImageFile.Load(reference), alpha: true),
            Render3DReferenceTests.InkCoverage(actual, alpha: false), actual.Width, ReferenceLimits.DetailedFigure);
        var actualPath = Golden.ActualPath("r3d-" + name + "-ink");
        if (problems.Length > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        Assert.True(problems.Length == 0, $"{name}: {problems}; lines in {actualPath}");
    }

    /// <summary>
    /// The detailed figures' limits are M6's but where these scenes were measured past them (the PC's GPU and lavapipe;
    /// each the worst plus about a tenth: ReferenceLimits.DetailedFigure's comments; the user's Q55, Q58 and Q60).
    /// </summary>
    [Fact]
    public void TheDetailedFiguresLimitsAreM6sWidenedWhereMeasured() =>
        Assert.Equal(ReferenceLimits.M6 with { InsideEvery = 4200, EdgeEvery = 1100, Unlike = 0.083 }, ReferenceLimits.DetailedFigure);

    /// <summary>
    /// Each widened limit bites: the scene that measured worst for it fails once that limit is set just under what the
    /// scene measured on both drivers (the man's close-up: 23 inside pixels, 86–88 edge pixels; the woman geared from the
    /// side: 7.56 % unlike), and passes under the limits as set.
    /// </summary>
    [Theory]
    [InlineData("inside", "humanoid-male-close-up")]
    [InlineData("edge", "humanoid-male-close-up")]
    [InlineData("unlike", "humanoid-female-gear-side")]
    public void EachWidenedLimitBites(string limit, string name)
    {
        var limits = ReferenceLimits.DetailedFigure;
        var tighter = limit switch
        {
            "inside" => limits with { InsideEvery = 4500 },          // allows 22 of 102,400
            "edge" => limits with { EdgeEvery = 1200 },              // allows 85
            "unlike" => limits with { Unlike = 0.075 },
            _ => throw new ArgumentException(limit),
        };
        var scene = Scenes.Single(s => s.Name == name);
        using var figure = new FigureView(gpu.Device, scene.Spec);
        figure.Seek(scene.Time);
        string Check(ReferenceLimits l) => limit is "inside" or "edge"
            ? Render3DReferenceTests.Compare(ImageFile.Load(Golden.GoldenPath("r3d-" + name)), FigureReferenceTests.Render(gpu.Device, figure, lines: false), l)
            : Render3DReferenceTests.CompareInk(Render3DReferenceTests.InkCoverage(ImageFile.Load(Golden.GoldenPath("r3d-" + name + "-ink")), alpha: true),
                Render3DReferenceTests.InkCoverage(FigureReferenceTests.Render(gpu.Device, figure, inkOnly: true), alpha: false), scene.Spec.Width, l);
        Assert.Equal("", Check(limits));
        Assert.NotEqual("", Check(tighter));
    }
}
