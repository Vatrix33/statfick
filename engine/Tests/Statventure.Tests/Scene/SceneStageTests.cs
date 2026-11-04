using System.Numerics;
using System.Text.Json;
using Statventure.Imaging;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Animation;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Xunit.Abstractions;

namespace Statventure.Tests.Scene;

/// <summary>
/// A stage scene (Docs/Scene.md, step 3): a stage model (Data/Models, built and exported as a stage by
/// Tools/BlenderReference/make_models.py), its clip (the whole timeline, the scene mode) played on the
/// instanced nodes from time 0 and held, the times (seconds) at which Blender evaluates the stage as it
/// built it (evaluate_animations.py, Golden/stage-&lt;name&gt;.json), and the render sizes (width,
/// height) at which its cameras' projections are compared.
/// </summary>
public sealed record StageScene(string Name, string Model, string Clip, double[] Times, int[][] Aspects);

/// <summary>
/// The dolly stage (make_models.py's <c>dolly</c>) as the render scene <c>scene-dolly</c> shows it: its
/// objects instanced as a scene instances a stage (each top-level object a model of its own, scenery),
/// seen through its perspective camera and lit by its sun as the import gives them 36 frames in
/// (Render3DReferenceTests renders it from these values, and Blender from the scene file;
/// SceneStageRenderTests through the scene's attachments).
/// </summary>
public static class DollyStage
{
    public const string File = "dolly.glb";
    public const double RenderTime = 36 / 60.0;

    public static readonly Dictionary<string, string> Palette = new()
    {
        ["ground"] = "#9a8a68", ["stone"] = "#7a6e60", ["metal.base"] = "#6f7a88", ["cloth"] = "#5d3f6f", ["metal.trim"] = "#c09a50",
    };

    /// <summary>The stage instanced (nodes only) with its clip at <paramref name="time"/>.</summary>
    public static ModelInstance At(ModelData data, double time)
    {
        var instance = new SceneNode().Instance(data);
        instance.Apply(instance.Clip("Scene"), time);
        return instance;
    }

    static readonly Lazy<(Camera3D Camera, SunLight Sun)> Seen = new(() =>
    {
        var instance = At(SceneNodeTests.LoadModel(File), RenderTime);
        var camera = new Camera();
        instance.Cameras.Single(c => c.Projection == Projection.Perspective).ApplyTo(camera);
        return (new Camera3D(camera.Projection, camera.Position, camera.Target, camera.VerticalFieldOfView * 180 / MathF.PI, Near: camera.Near, Far: camera.Far)
        {
            Up = camera.Up,
        }, instance.Sun!.Light);
    });

    public static Scene3D Scene => new("scene-dolly", 400, 280, "#cfd3cf", Seen.Value.Camera, [])
    {
        Models = [new(File, Vector3.Zero, Vector3.Zero, 1, Scenery: true) { Objects = true }],
        Palette = Palette,
        Sun = Seen.Value.Sun,
    };
}

/// <summary>
/// Cameras and the sun from glTF (Docs/Scene.md, step 3; Q9–Q13, S3-1 to S3-5): instancing a stage
/// hangs a <see cref="SceneCamera"/> on each node carrying a camera and a <see cref="SceneSun"/> on the
/// one carrying the sun, with the model's settings, making neither the scene's; and the stage played
/// on its instanced nodes is where Blender puts it: every node's world (the cameras and the sun
/// included, after the exporter's quarter turn) on the clip's 60 fps frames within M7's on-frame
/// tolerance and between frames within its allowance (as SceneReferenceTests judges a hierarchy); each
/// camera's vertical field of view or orthographic height, near and far as Blender's, and its
/// projection's x and y scales at the render's aspect and a portrait one as Blender's view plane
/// gives them (sensor fit Vertical: the vertical extent kept), within float rounding; the sun's
/// direction toward it, strength and angle as Blender's.
/// </summary>
public class SceneStageTests(ITestOutputHelper output)
{
    const int Fps = 60;

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");
    static string GoldenDirectory => Path.GetDirectoryName(Golden.GoldenPath("x"))!;

    /// <summary>Every other frame of <paramref name="frames"/> (and the last), the middle of every frame, a quarter into every fifth, one before the start and one after the end.</summary>
    static double[] TimesFor(int frames)
    {
        var times = new SortedSet<double> { -0.05, (frames + 3.0) / Fps, (double)frames / Fps };
        for (var k = 0; k <= frames; k += 2)
            times.Add((double)k / Fps);
        for (var k = 0; k < frames; k++)
            times.Add((k + 0.5) / Fps);
        for (var k = 0; k < frames; k += 5)
            times.Add((k + 0.25) / Fps);
        return [.. times];
    }

    public static readonly StageScene[] All =
    [
        // The dolly moving and turning with the camera tilting and panning on it, the still orthographic
        // camera and the still sun; projections at the render scene's size and a portrait one.
        new("dolly", "dolly", "Scene", TimesFor(60), [[400, 280], [280, 400]]),
    ];

    public static TheoryData<string> SceneNames => new(All.Select(s => s.Name));

    [Fact]
    public void TheStageSceneFileIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "stage-scenes.json");
        var text = JsonSerializer.Serialize(new { scenes = All.Select(s => new { name = s.Name, model = s.Model, clip = s.Clip, times = s.Times, aspects = s.Aspects }) },
            new JsonSerializerOptions { WriteIndented = true }) + "\n";
        if (!System.IO.File.Exists(path) || System.IO.File.ReadAllText(path) != text)
            System.IO.File.WriteAllText(path, text);
        Assert.NotEmpty(All);
    }

    [Fact]
    public void InstancingHangsTheCamerasAndTheSunOnTheirNodes()
    {
        var data = SceneNodeTests.LoadModel(DollyStage.File);
        var view = new SceneView(new View3D(null!));
        var instance = view.Root.Instance(data);
        // On the nodes of the cameras' and the sun's names, the model's settings, in the model's node order.
        Assert.Equal(["Camera", "Plan"], instance.Cameras.Select(c => c.Node!.Name));
        Assert.All(instance.Cameras, c => Assert.Same(instance.NodeNamed(c.Node!.Name), c.Node));
        var (lens, plan) = (instance.Cameras[0], instance.Cameras[1]);
        var file = (Lens: data.Cameras[data.Nodes.Single(n => n.Name == "Camera").Camera], Plan: data.Cameras[data.Nodes.Single(n => n.Name == "Plan").Camera]);
        Assert.Equal((Projection.Perspective, file.Lens.VerticalFieldOfView, file.Lens.Near, file.Lens.Far), (lens.Projection, lens.VerticalFieldOfView, lens.Near, lens.Far));
        Assert.Equal((Projection.Orthographic, file.Plan.OrthographicHeight, file.Plan.Near, file.Plan.Far), (plan.Projection, plan.OrthographicHeight, plan.Near, plan.Far));
        // A perspective camera keeps the attachment's own orthographic height and the other way round.
        Assert.Equal((new SceneCamera().OrthographicHeight, new SceneCamera().VerticalFieldOfView), (lens.OrthographicHeight, plan.VerticalFieldOfView));
        var sun = Assert.IsType<SceneSun>(instance.Sun);
        Assert.Equal("Sun", sun.Node!.Name);
        Assert.Equal((data.Lights[0].Strength, data.Lights[0].Angle), (sun.Strength, sun.Angle));
        // Neither is the attachment's default, so they come from the file.
        Assert.Equal(2.5f, sun.Strength);
        Assert.Equal(4f, sun.Angle, 1e-5f);
        Assert.NotEqual((new SceneSun().Strength, new SceneSun().Angle), (sun.Strength, sun.Angle));
        // Each on its own node and nowhere else; nothing becomes the scene's camera or sun by itself.
        Assert.Equal(3, view.Root.Descendants().SelectMany(n => n.Attachments).Count(a => a is SceneCamera or SceneSun));
        Assert.Null(view.Camera);
        Assert.Null(view.Sun);
        // A model with neither gives none.
        var plain = new SceneNode().Instance(SceneNodeTests.LoadModel("hierarchy.glb"));
        Assert.Empty(plain.Cameras);
        Assert.Null(plain.Sun);
        Assert.DoesNotContain(plain.Roots.SelectMany(r => r.Descendants().Prepend(r)).SelectMany(n => n.Attachments), a => a is SceneCamera or SceneSun);
    }

    [Fact]
    public void ASceneTakesTheStagesCameraAndSunFromTheirNodes()
    {
        var data = SceneNodeTests.LoadModel(DollyStage.File);
        var view = new SceneView(new View3D(null!));
        var instance = view.Root.Instance(data);
        instance.Apply(instance.Clip("Scene"), DollyStage.RenderTime);
        view.Camera = instance.Cameras[0];
        view.Sun = instance.Sun;
        view.Refresh();
        var expected = DollyStage.Scene;
        Assert.Equal((expected.Camera.Position, expected.Camera.Target, expected.Camera.Up), (view.View.Camera.Position, view.View.Camera.Target, view.View.Camera.Up));
        Assert.Equal(expected.Sun, view.View.Sun);
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void TheStageIsWhereBlenderPutsIt(string name)
    {
        var scene = All.Single(s => s.Name == name);
        var path = Path.Combine(GoldenDirectory, "stage-" + name + ".json");
        Assert.True(System.IO.File.Exists(path), $"no Blender reference for {name}: run Tools/Scripts/make-blender-references.sh {name}");
        using var document = JsonDocument.Parse(System.IO.File.ReadAllBytes(path));
        var reference = document.RootElement;
        Assert.Equal(scene.Times, reference.GetProperty("times"u8).EnumerateArray().Select(t => t.GetDouble()));
        var golden = reference.GetProperty("nodes"u8);
        var data = SceneNodeTests.LoadModel(scene.Model + ".glb");
        var instance = new SceneNode().Instance(data);
        var clip = instance.Clip(scene.Clip);
        var nodes = Enumerable.Range(0, data.Nodes.Count).Where(i => instance.NodeOf(i) != null).ToArray();
        Matrix4x4 Blender(int node, int time)
        {
            Assert.True(golden.TryGetProperty(data.Nodes[node].Name, out var list), $"{name}: Blender has no object {data.Nodes[node].Name}");
            var m = list[time].EnumerateArray().Select(v => v.GetSingle()).ToArray();
            return new Matrix4x4(m[0], m[4], m[8], 0, m[1], m[5], m[9], 0, m[2], m[6], m[10], 0, m[3], m[7], m[11], 1);
        }
        Matrix4x4 Inverse(Matrix4x4 m) => Matrix4x4.Invert(m, out var inverse) ? inverse : throw new InvalidOperationException("a singular world matrix");
        Vector3[] points = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ];
        double Distance(Matrix4x4 a, Matrix4x4 b) => points.Max(p => (double)Vector3.Distance(Vector3.Transform(p, a), Vector3.Transform(p, b)));
        var lastFrame = (int)Math.Round(clip.Clip.Duration * Fps);
        var frames = new Dictionary<int, Matrix4x4[]>();
        Matrix4x4[] Frame(int k)
        {
            k = Math.Clamp(k, 0, lastFrame);
            if (frames.TryGetValue(k, out var locals))
                return locals;
            instance.Apply(clip, (double)k / Fps);
            return frames[k] = nodes.Select(i => instance.NodeOf(i)!.Local).ToArray();
        }
        var cameras = reference.GetProperty("cameras"u8);
        var suns = reference.GetProperty("suns"u8);
        Assert.Equal(instance.Cameras.Select(c => c.Node!.Name).Order(), cameras.EnumerateObject().Select(c => c.Name).Order());
        Assert.Equal([instance.Sun!.Node!.Name], suns.EnumerateObject().Select(s => s.Name));
        static double Relative(double expected, double actual) => Math.Abs(actual - expected) / Math.Abs(expected);

        double onFrame = 0, ratio = 0, worstScore = -1, worstLens = 0, worstSun = 0;
        var worst = "";
        for (var t = 0; t < scene.Times.Length; t++)
        {
            var time = scene.Times[t];
            var exact = AnimationScene.OnFrame(time);
            var k = (int)Math.Floor(time * Fps);
            if (!exact)
                for (var f = k - 1; f <= k + 2; f++)
                    Frame(f);
            instance.Apply(clip, time);
            for (var n = 0; n < nodes.Length; n++)
            {
                var node = instance.NodeOf(nodes[n])!;
                double error, score;
                if (exact)
                {
                    error = Distance(node.World, Blender(nodes[n], t));
                    onFrame = Math.Max(onFrame, error);
                    score = error / AnimationReferenceTests.OnFrameTolerance;
                }
                else
                {
                    var parent = data.Nodes[nodes[n]].Parent;
                    var blenderLocal = parent >= 0 ? Blender(nodes[n], t) * Inverse(Blender(parent, t)) : Blender(nodes[n], t);
                    error = Distance(node.Local, blenderLocal);
                    var curvature = points.Max(p =>
                    {
                        Vector3 At(int f) => Vector3.Transform(p, Frame(f)[n]);
                        return Math.Max((At(k - 1) - 2 * At(k) + At(k + 1)).Length(), (At(k) - 2 * At(k + 1) + At(k + 2)).Length());
                    });
                    score = error / (AnimationReferenceTests.BetweenFramesMargin * curvature / 8 + AnimationReferenceTests.OnFrameTolerance);
                    ratio = Math.Max(ratio, score);
                }
                if (score > worstScore)
                {
                    worstScore = score;
                    worst = $"worst: {node.Name} at {time * Fps:F2} frames, {error * 1000:F4} mm, {score:F2} of its allowance";
                }
            }
            // The cameras' settings and projections (Blender's view plane at each render size).
            foreach (var camera in instance.Cameras)
            {
                var blender = cameras.GetProperty(camera.Node!.Name);
                Assert.Equal(blender.GetProperty("projection"u8).GetString(), camera.Projection.ToString().ToLowerInvariant());
                double Value(ReadOnlySpan<byte> property) => blender.GetProperty(property)[t].GetDouble();
                var setting = camera.Projection == Projection.Perspective
                    ? Relative(Value("fieldOfView"u8), camera.VerticalFieldOfView)
                    : Relative(Value("orthographicScale"u8), camera.OrthographicHeight);
                worstLens = Math.Max(worstLens, Math.Max(setting, Math.Max(Relative(Value("near"u8), camera.Near), Relative(Value("far"u8), camera.Far))));
                var view = new Camera();
                camera.ApplyTo(view);
                var scales = blender.GetProperty("scales"u8)[t];
                for (var a = 0; a < scene.Aspects.Length; a++)
                {
                    var projection = view.ProjectionMatrix((float)scene.Aspects[a][0] / scene.Aspects[a][1]);
                    // Vulkan's clip space points Y down: the engine's y scale is Blender's negated.
                    worstLens = Math.Max(worstLens, Math.Max(Relative(scales[a][0].GetDouble(), projection.M11), Relative(scales[a][1].GetDouble(), -projection.M22)));
                }
            }
            // The sun: toward it, its strength and its angle.
            var sun = suns.GetProperty(instance.Sun.Node.Name);
            var light = instance.Sun.Light;
            var toward = sun.GetProperty("direction"u8)[t].EnumerateArray().Select(v => v.GetSingle()).ToArray();
            worstSun = Math.Max(worstSun, Vector3.Distance(new Vector3(toward[0], toward[1], toward[2]), light.Direction));
            worstLens = Math.Max(worstLens, Math.Max(Relative(sun.GetProperty("strength"u8)[t].GetDouble(), light.Strength),
                Relative(sun.GetProperty("angle"u8)[t].GetDouble(), light.Angle)));
            Assert.Equal("SUN", sun.GetProperty("type"u8).GetString());
        }
        output.WriteLine($"{name}: on frames {onFrame * 1000:F5} mm; between frames at most {ratio:F2} of the allowance ({worst}); " +
            $"cameras' and sun's settings within {worstLens:E2} of Blender's; the sun's direction within {worstSun:E2}");
        Assert.True(onFrame <= AnimationReferenceTests.OnFrameTolerance, $"{name}: a node is {onFrame * 1000:F4} mm off on a frame ({worst})");
        Assert.True(ratio <= 1, $"{name}: a node is off between frames by {ratio:F2} times what interpolating the samples allows ({worst})");
        // Float rounding: Blender works in float, and the exporter writes its float values as they are.
        Assert.True(worstLens <= 2e-6, $"{name}: a camera's or the sun's setting or projection is {worstLens:E2} off Blender's");
        Assert.True(worstSun <= AnimationReferenceTests.OnFrameTolerance, $"{name}: the sun's direction is {worstSun:E2} off Blender's");
    }
}

/// <summary>
/// The dolly stage drawn through a scene (Docs/Scene.md, step 3): instanced as scenery, its clip at
/// the render time, the view's camera and sun taken from the <see cref="SceneCamera"/> and
/// <see cref="SceneSun"/> instancing hung on their nodes, matches Blender's render of the render scene
/// <c>scene-dolly</c> (the camera and sun as the import gives them) as Render3DReferenceTests compares
/// a scene with shadows: the shading without lines and the ink on its own.
/// </summary>
[Collection("GPU")]
public class SceneStageRenderTests(GpuFixture gpu)
{
    RgbaImage Render(Scene3D scene, View3D view, bool inkOnly = false, bool shadows = true)
    {
        var device = gpu.Device;
        var items = shadows ? view.Items : view.Items.Select(i => i with { ReceivesShadows = false }).ToList();
        var style = inkOnly ? scene.Style : scene.Style with { OutlineWidth = 0, InnerWidth = 0 };
        using var target = new RenderTarget3D(device, scene.Width, scene.Height);
        var renderer = new Renderer3D(device) { InkCoverageOnly = inkOnly };
        var frame = device.BeginFrame();
        renderer.Render(frame, target, view.Camera, items, Colors.LinearFromHex(scene.Background), view.Sun, style, scene.Scale, scene.Time);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(scene.Width, scene.Height, target.Resolved.ReadPixels());
    }

    [Fact]
    public void TheStageThroughItsOwnCameraAndSunIsDrawnAsBlenderDrewIt()
    {
        var scene = Scenes3D.All.Single(s => s.Name == "scene-dolly");
        using var stage = Model.Load(gpu.Device, Path.Combine(Render3DReferenceTests.ModelDirectory, DollyStage.File));
        using var view = new View3D(gpu.Device) { Style = scene.Style, Background = Colors.LinearFromHex(scene.Background) };
        var world = new SceneView(view);
        var instance = world.Root.Add("stage").Instance(stage, new Palette(scene.Palette, scene.Shadows), scenery: true);
        instance.Apply(instance.Clip("Scene"), DollyStage.RenderTime);
        world.Camera = instance.Cameras.Single(c => c.Projection == Projection.Perspective);
        world.Sun = instance.Sun;
        Assert.True(world.Refresh());
        var name = "scene-dolly-stage";
        var shaded = Render(scene, view);
        var expected = ImageFile.Load(Golden.GoldenPath("r3d-" + scene.Name));
        var problems = Render3DReferenceTests.CompareShadowed(expected, shaded, Render(scene, view, shadows: false));
        if (problems.Length > 0)
        {
            ImageFile.SavePng(Golden.ActualPath(name), shaded);
            Assert.Fail($"{name}: {problems}; render in {Golden.ActualPath(name)}");
        }
        var ink = Render(scene, view, inkOnly: true);
        var inkProblems = Render3DReferenceTests.CompareInk(Render3DReferenceTests.InkCoverage(ImageFile.Load(Golden.GoldenPath("r3d-" + scene.Name + "-ink")), alpha: true),
            Render3DReferenceTests.InkCoverage(ink, alpha: false), ink.Width);
        if (inkProblems.Length > 0)
        {
            ImageFile.SavePng(Golden.ActualPath(name + "-ink"), ink);
            Assert.Fail($"{name}: {inkProblems}; lines in {Golden.ActualPath(name + "-ink")}");
        }
    }
}
