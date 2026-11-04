using System.Numerics;
using System.Text.Json;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Animation;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Tests.Scene;

/// <summary>
/// A scene without a GPU to play the dolly stage's sequences on (Docs/Scene.md, step 5): its view never
/// renders, a document's frames drive it, a node "game" carries the game's own camera and sun (the
/// scene's when a sequence starts) and the node role "lens" another camera and sun.
/// </summary>
sealed class StageBench
{
    public const string DollyPath = "models/stages/dolly";

    public StageBench(bool reducedMotion = false)
    {
        View = new View3D(null!);
        Scene = new SceneView(View);
        Document = new UiDocument(TestFonts.Library);
        Document.SetViewport(300, 200, reducedMotion);
        Document.AddFrameClient(Scene);
        Document.Update(0);
        var game = Scene.Root.Add("game");
        GameCamera = game.Attach(new SceneCamera { VerticalFieldOfView = 1.1f });
        GameSun = game.Attach(new SceneSun { Strength = 1.5f, Angle = 2 });
        Scene.Camera = GameCamera;
        Scene.Sun = GameSun;
        Lens = Scene.Root.Add("lens");
        Lens.Translation = new(0, 2, 8);
        LensCamera = Lens.Attach(new SceneCamera { VerticalFieldOfView = 0.4f });
        LensSun = Lens.Attach(new SceneSun { Strength = 5, Angle = 1 });
    }

    public static ModelData Dolly { get; } = SceneNodeTests.LoadModel(DollyStage.File);

    public View3D View { get; }
    public SceneView Scene { get; }
    public UiDocument Document { get; }
    public SceneCamera GameCamera { get; }
    public SceneSun GameSun { get; }
    public SceneNode Lens { get; }
    public SceneCamera LensCamera { get; }
    public SceneSun LensSun { get; }
    public double Now { get; private set; }

    /// <summary>A sequence on the dolly stage (its clip from 0) with the node role "lens" and the tracks <paramref name="tracks"/> adds.</summary>
    public static Sequence Build(string name, Func<SequenceBuilder, SequenceBuilder> tracks, double duration = 1) =>
        tracks(new SequenceBuilder(name, duration).Role("lens", RoleKind.Node).Stage(DollyPath, Dolly).Clip("dolly", "Scene")).Build();

    public SequenceBindings Bindings() => new SequenceBindings().Node("lens", Lens).Stage(DollyPath, Dolly);

    public SequencePlayer Play(Sequence sequence, SequenceBindings? bindings = null) => Scene.Play(sequence, bindings ?? Bindings(), Document);

    public void Frame(double seconds)
    {
        Now += seconds;
        Document.Update(Now);
    }

    /// <summary>Jumps the player to τ (the full variant: its clock is τ) and frames there, so the state at τ is applied.</summary>
    public void At(SequencePlayer player, double tau)
    {
        player.Seek(tau);
        Frame(0);
    }

    /// <summary>The stage's node of this name, as the sequence instanced it.</summary>
    public static SceneNode Node(SequencePlayer player, string name) => player.Stages[0].NodeNamed(name)!;

    /// <summary>Blender's evaluation of the dolly stage (Golden/stage-dolly.json, SceneStageTests).</summary>
    public static JsonElement Golden { get; } = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Infrastructure.Golden.GoldenPath("x"))!, "stage-dolly.json"))).RootElement;

    /// <summary>The golden's times.</summary>
    public static double[] Times { get; } = [.. Golden.GetProperty("times"u8).EnumerateArray().Select(t => t.GetDouble())];

    /// <summary>Blender's world matrix (glTF's coordinates) of an object at the golden's time <paramref name="t"/>.</summary>
    public static Matrix4x4 World(string name, int t)
    {
        var m = Golden.GetProperty("nodes"u8).GetProperty(name)[t].EnumerateArray().Select(v => v.GetSingle()).ToArray();
        return new Matrix4x4(m[0], m[4], m[8], 0, m[1], m[5], m[9], 0, m[2], m[6], m[10], 0, m[3], m[7], m[11], 1);
    }
}

/// <summary>
/// The camera track (Docs/Scene.md, "How it works", step 5; Q11, S5-5): cuts make the camera on the
/// named node the scene's, a stage's imported camera (S3-5) or a bound node's, the view then placed
/// and projecting as Blender's camera does at that time (checked against Blender's evaluation of the
/// dolly stage through the sequence player); the field-of-view curve sets the camera cut to from the
/// first cut on, every frame, a camera cut away from gets its own back, an orthographic one is left
/// alone; before the first cut and after a seek back the game's camera; the end state stays; a skip
/// ends there; a cut naming a node without a camera is refused, changing nothing.
/// </summary>
public class SequenceCameraTests
{
    static double Relative(double expected, double actual) => Math.Abs(actual - expected) / Math.Abs(expected);

    /// <summary>
    /// Cut to the stage's perspective camera at 0 and to its orthographic one at 0.5 s: at every one of
    /// the golden's times inside the timeline the view is where Blender's camera is (on the clip's frames
    /// within M7's on-frame tolerance; between them exactly where the stage played apart puts it,
    /// SceneStageTests judging that against Blender), its settings and its projection's scales at the
    /// golden's two aspects within float rounding of Blender's, the cut in effect from its own time.
    /// </summary>
    [Fact]
    public void CutsGiveTheImportedCamerasAsBlenderEvaluatesThem()
    {
        var bench = new StageBench();
        var sequence = StageBench.Build("cuts", b => b.Camera(c => c.Cut(0, "dolly/Camera").Cut(0.5, "dolly/Plan")));
        var player = bench.Play(sequence);
        bench.Frame(0);
        var cameras = StageBench.Golden.GetProperty("cameras"u8);
        var aspects = StageBench.Golden.GetProperty("aspects"u8).EnumerateArray().Select(a => a[0].GetDouble() / a[1].GetDouble()).ToArray();
        // The stage played apart as its clip track plays it: one strip of its clip from 0 (SequenceTrackTests' reference).
        var apart = new SceneNode("apart").Instance(StageBench.Dolly);
        var stack = new AnimationStack(apart.Skeleton);
        var layer = new AnimationLayer();
        layer.Strips.Add(new SequenceStrip("Scene").ToStrip(apart.Clip("Scene"), 0));
        stack.Layers.Add(layer);
        var (checkedOn, checkedBetween, worst) = (0, 0, 0.0);
        for (var t = 0; t < StageBench.Times.Length; t++)
        {
            var time = StageBench.Times[t];
            if (time < 0 || time > 1)
                continue;
            bench.At(player, time);
            var name = time < 0.5 ? "Camera" : "Plan";
            Assert.Same(StageBench.Node(player, name).Attachments.OfType<SceneCamera>().Single(), bench.Scene.Camera);
            var view = bench.View.Camera;
            if (AnimationScene.OnFrame(time))
            {
                var blender = StageBench.World(name, t);
                var forward = -Vector3.Normalize(new Vector3(blender.M31, blender.M32, blender.M33));
                var up = Vector3.Normalize(new Vector3(blender.M21, blender.M22, blender.M23));
                var error = Math.Max(Vector3.Distance(blender.Translation, view.Position),
                    Math.Max(Vector3.Distance(forward, Vector3.Normalize(view.Target - view.Position)), Vector3.Distance(up, view.Up)));
                worst = Math.Max(worst, error);
                checkedOn++;
            }
            else
            {
                apart.Apply(stack, time);
                var expected = new Camera();
                apart.Cameras.Single(c => c.Node!.Name == name).ApplyTo(expected);
                Assert.Equal((expected.Position, expected.Target, expected.Up), (view.Position, view.Target, view.Up));
                checkedBetween++;
            }
            var settings = cameras.GetProperty(name);
            Assert.Equal(settings.GetProperty("projection"u8).GetString(), view.Projection.ToString().ToLowerInvariant());
            double Value(ReadOnlySpan<byte> property) => settings.GetProperty(property)[t].GetDouble();
            Assert.True(Relative(view.Projection == Projection.Perspective ? Value("fieldOfView"u8) : Value("orthographicScale"u8),
                view.Projection == Projection.Perspective ? view.VerticalFieldOfView : view.OrthographicHeight) <= 2e-6);
            Assert.True(Relative(Value("near"u8), view.Near) <= 2e-6 && Relative(Value("far"u8), view.Far) <= 2e-6);
            var scales = settings.GetProperty("scales"u8)[t];
            for (var a = 0; a < aspects.Length; a++)
            {
                var projection = view.ProjectionMatrix((float)aspects[a]);
                Assert.True(Relative(scales[a][0].GetDouble(), projection.M11) <= 2e-6 && Relative(scales[a][1].GetDouble(), -projection.M22) <= 2e-6,
                    $"{name} at {time * 60:F2} frames: the projection's scales aren't Blender's");
            }
        }
        Assert.True(checkedOn >= 30 && checkedBetween >= 30, $"{checkedOn} times on frames, {checkedBetween} between");
        Assert.True(worst <= AnimationReferenceTests.OnFrameTolerance, $"the view is {worst:E2} off Blender's camera on a frame");
    }

    /// <summary>
    /// The field-of-view curve (S5-5): before the first cut nothing (the game's camera, its own field
    /// of view); from the cut on the camera cut to takes the curve's value every frame, the view's
    /// projection with it (on the frames, still where Blender's camera is); cut to the orthographic
    /// camera, the perspective one gets its own field of view back and the orthographic one keeps its
    /// height (the curve leaves it); cut back, the curve again; a bound node's camera takes it too; at
    /// the end the last keeps the curve's end value; a seek back before the first cut gives the game's
    /// camera back and every camera its own value.
    /// </summary>
    [Fact]
    public void TheFieldOfViewCurveSetsTheCameraCutToFromTheFirstCutOn()
    {
        var bench = new StageBench();
        var curve = new FCurve([FCurveKey.Linear(0, 0.3), FCurveKey.Linear(1, 1.3)]);
        var sequence = StageBench.Build("lens", b => b.Camera(c => c.Cut(0.25, "dolly/Camera").Cut(0.5, "dolly/Plan").Cut(0.75, "dolly/Camera").Cut(0.875, "lens").FieldOfView(curve)));
        var player = bench.Play(sequence);
        var lens = StageBench.Node(player, "Camera").Attachments.OfType<SceneCamera>().Single();
        var plan = StageBench.Node(player, "Plan").Attachments.OfType<SceneCamera>().Single();
        var own = (Lens: lens.VerticalFieldOfView, PlanHeight: plan.OrthographicHeight, PlanFov: plan.VerticalFieldOfView);
        Assert.Equal(0.660594702f, own.Lens, 1e-6f);
        bench.Frame(0);
        bench.At(player, 0.125);
        Assert.Same(bench.GameCamera, bench.Scene.Camera);
        Assert.Equal((1.1f, own.Lens), (bench.GameCamera.VerticalFieldOfView, lens.VerticalFieldOfView));
        Assert.Equal(1.1f, bench.View.Camera.VerticalFieldOfView);
        foreach (var tau in new[] { 0.25, 20 / 60.0, 0.4 })
        {
            bench.At(player, tau);
            Assert.Same(lens, bench.Scene.Camera);
            Assert.Equal((float)curve.Evaluate(tau), lens.VerticalFieldOfView);
            Assert.Equal((float)curve.Evaluate(tau), bench.View.Camera.VerticalFieldOfView);
            Assert.Equal(1 / MathF.Tan((float)curve.Evaluate(tau) / 2), -bench.View.Camera.ProjectionMatrix(1.5f).M22, 1e-5f);
            Assert.Equal(1.1f, bench.GameCamera.VerticalFieldOfView);
        }
        // Still where Blender's camera is on a frame (time index of 20/60 s in the golden).
        var frame = Array.FindIndex(StageBench.Times, t => Math.Abs(t - 20 / 60.0) < 1e-9);
        bench.At(player, StageBench.Times[frame]);
        Assert.True(Vector3.Distance(StageBench.World("Camera", frame).Translation, bench.View.Camera.Position) <= AnimationReferenceTests.OnFrameTolerance);
        // Cut to the orthographic camera: the perspective one has its own back, the orthographic one keeps what it had.
        bench.At(player, 0.6);
        Assert.Same(plan, bench.Scene.Camera);
        Assert.Equal((own.Lens, own.PlanHeight, own.PlanFov), (lens.VerticalFieldOfView, plan.OrthographicHeight, plan.VerticalFieldOfView));
        Assert.Equal(Projection.Orthographic, bench.View.Camera.Projection);
        Assert.Equal(own.PlanHeight, bench.View.Camera.OrthographicHeight);
        // Back to it, then to the bound node's camera.
        bench.At(player, 0.8);
        Assert.Equal((float)curve.Evaluate(0.8), lens.VerticalFieldOfView);
        bench.At(player, 0.9);
        Assert.Same(bench.LensCamera, bench.Scene.Camera);
        Assert.Equal(((float)curve.Evaluate(0.9), own.Lens), (bench.LensCamera.VerticalFieldOfView, lens.VerticalFieldOfView));
        Assert.Equal(bench.Lens.World.Translation, bench.View.Camera.Position);
        // Seek back before the first cut: the game's camera, every camera its own.
        bench.At(player, 0.1);
        Assert.Same(bench.GameCamera, bench.Scene.Camera);
        Assert.Equal((0.4f, own.Lens), (bench.LensCamera.VerticalFieldOfView, lens.VerticalFieldOfView));
        // Played to the end: the end state stays, the last camera at the curve's end value.
        for (var frames = 0; !player.Ended; frames++)
        {
            Assert.True(frames < 100);
            bench.Frame(1 / 30.0);
        }
        Assert.Same(bench.LensCamera, bench.Scene.Camera);
        Assert.Equal(1.3f, bench.LensCamera.VerticalFieldOfView);
        bench.Frame(1 / 30.0);
        Assert.Equal((1.3f, 1.3f), (bench.LensCamera.VerticalFieldOfView, bench.View.Camera.VerticalFieldOfView));
    }

    /// <summary>
    /// Cuts apply when the cut in effect changes (S4-8): the game's own camera set between two cuts
    /// stands until the next; without a curve no camera's field of view is touched.
    /// </summary>
    [Fact]
    public void CutsApplyWhenTheCutInEffectChanges()
    {
        var bench = new StageBench();
        var sequence = StageBench.Build("changes", b => b.Camera(c => c.Cut(0.25, "dolly/Camera").Cut(0.75, "lens")));
        var player = bench.Play(sequence);
        var lens = StageBench.Node(player, "Camera").Attachments.OfType<SceneCamera>().Single();
        bench.Frame(0);
        bench.Frame(0.3);
        Assert.Same(lens, bench.Scene.Camera);
        bench.Scene.Camera = bench.GameCamera;
        bench.Frame(0.2);
        Assert.Same(bench.GameCamera, bench.Scene.Camera);
        // A seek applies the cut in effect again, the same one too.
        bench.At(player, 0.5);
        Assert.Same(lens, bench.Scene.Camera);
        // Without a curve the track leaves the fields of view alone: the game's change stands after a cut away.
        lens.VerticalFieldOfView = 0.9f;
        bench.Frame(0.3);
        Assert.Same(bench.LensCamera, bench.Scene.Camera);
        Assert.Equal((0.9f, 0.4f, 1.1f), (lens.VerticalFieldOfView, bench.LensCamera.VerticalFieldOfView, bench.GameCamera.VerticalFieldOfView), new FloatTolerance(1e-6f));
    }

    /// <summary>A skip ends in the end state: the last cut's camera, at the curve's end value; watched to the end, the same.</summary>
    [Fact]
    public void ASkipEndsOnTheLastCutAtTheCurvesEndAsWatching()
    {
        var curve = new FCurve([FCurveKey.Linear(0, 0.5), FCurveKey.Linear(1, 0.8)]);
        var sequence = StageBench.Build("skip", b => b.Camera(c => c.Cut(0, "lens").Cut(0.5, "dolly/Camera").FieldOfView(curve)));
        (SceneCamera?, float, Vector3, float) EndOf(bool skip)
        {
            var bench = new StageBench();
            var player = bench.Play(sequence);
            bench.Frame(0);
            bench.Frame(0.1);
            if (skip)
                Assert.True(player.Skip());
            else
                for (var frames = 0; !player.Ended; frames++)
                {
                    Assert.True(frames < 100);
                    bench.Frame(0.1);
                }
            bench.Scene.Refresh();
            var lens = StageBench.Node(player, "Camera").Attachments.OfType<SceneCamera>().Single();
            Assert.Same(lens, bench.Scene.Camera);
            Assert.Equal(0.4f, bench.LensCamera.VerticalFieldOfView);
            return (bench.Scene.Camera, lens.VerticalFieldOfView, bench.View.Camera.Position, bench.View.Camera.VerticalFieldOfView);
        }
        var skipped = EndOf(skip: true);
        var watched = EndOf(skip: false);
        Assert.Equal((0.8f, 0.8f), (skipped.Item2, skipped.Item4));
        Assert.Equal((watched.Item2, watched.Item3, watched.Item4), (skipped.Item2, skipped.Item3, skipped.Item4));
    }

    public static TheoryData<string> BadCuts => [.. Bad.Keys];

    static readonly Dictionary<string, (Func<Sequence> Sequence, string Problem)> Bad = new()
    {
        ["stage node"] = (() => StageBench.Build("bad", b => b.Camera(c => c.Cut(0, "dolly/Camera").Cut(0.5, "dolly/Rock"))),
            "the sequence bad, track 1 (camera): cut 1's camera dolly/Rock: the stage models/stages/dolly's node Rock carries no camera"),
        ["sun node"] = (() => StageBench.Build("bad", b => b.Camera(c => c.Cut(0, "dolly/Sun"))),
            "the sequence bad, track 1 (camera): cut 0's camera dolly/Sun: the stage models/stages/dolly's node Sun carries no camera"),
        ["bound node"] = (() => StageBench.Build("bad", b => b.Role("bare", RoleKind.Node).Camera(c => c.Cut(0, "bare"))),
            "the sequence bad, track 1 (camera): cut 0's camera bare: the node carries no camera"),
    };

    /// <summary>A cut naming a node that carries no camera is refused at play (S4-6), naming the cut and the node, and nothing changes.</summary>
    [Theory]
    [MemberData(nameof(BadCuts))]
    public void ACutToANodeWithoutACameraIsRefused(string name)
    {
        var bench = new StageBench();
        var (sequence, problem) = Bad[name];
        var bindings = bench.Bindings();
        var bare = new SceneNode("bare");
        bench.Scene.Root.Add(bare);
        if (sequence().Roles.Any(r => r.Name == "bare"))
            bindings.Node("bare", bare);
        var children = bench.Scene.Root.Children.ToArray();
        Assert.Equal(problem, Assert.Throws<ArgumentException>(() => bench.Play(sequence(), bindings)).Message);
        Assert.Equal(children, bench.Scene.Root.Children);
        Assert.Empty(bench.Scene.Playing);
        Assert.Same(bench.GameCamera, bench.Scene.Camera);
    }
}

/// <summary>Floats equal within an absolute tolerance (for tuples of floats).</summary>
sealed class FloatTolerance(float tolerance) : IEqualityComparer<(float, float, float)>
{
    public bool Equals((float, float, float) a, (float, float, float) b) =>
        Math.Abs(a.Item1 - b.Item1) <= tolerance && Math.Abs(a.Item2 - b.Item2) <= tolerance && Math.Abs(a.Item3 - b.Item3) <= tolerance;

    public int GetHashCode((float, float, float) value) => 0;
}
