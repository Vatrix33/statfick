using System.Numerics;
using Statventure.Animation;
using Statventure.Imaging;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;

namespace Statventure.Tests.Scene;

/// <summary>
/// Render scenes drawn through the scene (Docs/Scene.md, step 1) match Blender's renders of them as
/// the direct draws do (Render3DReferenceTests' comparisons, the shading without lines and the ink on
/// its own): the geared figure as an actor on a turned node, its sword and pennant on its sockets
/// (the pennant playing its own swing with the figure's state), its tabard on its bones; and the
/// terrain map with its terrain and model placed by nodes and the view's camera and sun from nodes.
/// (Instanced stages, each top-level object its own model, go through the scene in
/// Render3DReferenceTests' scene "scene-objects".)
/// </summary>
[Collection("GPU")]
public class SceneRenderTests(GpuFixture gpu)
{
    static string ModelPath(string file) => Path.Combine(Render3DReferenceTests.ModelDirectory, file);

    /// <summary>The scene's items rendered as Render3DReferenceTests renders a test scene: its camera and sun as the view has them.</summary>
    RgbaImage Render(Scene3D scene, View3D view, bool inkOnly = false, bool shadows = true)
    {
        var device = gpu.Device;
        var items = shadows ? view.Items : view.Items.Select(i => i with { ReceivesShadows = false }).ToList();
        var style = inkOnly ? scene.Style : scene.Style with { OutlineWidth = 0, InnerWidth = 0 };
        using var target = new RenderTarget3D(device, scene.Width, scene.Height);
        var renderer = new Renderer3D(device) { InkCoverageOnly = inkOnly };
        var frame = device.BeginFrame();
        renderer.Render(frame, target, view.Camera, items, Colors.LinearFromHex(scene.Background), view.Sun, style, scene.Scale, scene.Time, scene.ParticlesNow());
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(scene.Width, scene.Height, target.Resolved.ReadPixels());
    }

    /// <summary>Compares as TheEngineDrawsWhatBlenderDrew and TheEngineInksWhatFreestyleInked do.</summary>
    void MatchesBlender(Scene3D scene, View3D view, string name)
    {
        var shaded = Render(scene, view);
        var shadowed = scene.Models.Any(m => m.Scenery) || scene.Terrain != null;
        var expected = ImageFile.Load(Golden.GoldenPath("r3d-" + scene.Name));
        var unshadowed = shadowed ? Render(scene, view, shadows: false) : null;
        var problems = unshadowed != null && !unshadowed.Pixels.AsSpan().SequenceEqual(shaded.Pixels)
            ? Render3DReferenceTests.CompareShadowed(expected, shaded, unshadowed)
            : Render3DReferenceTests.Compare(expected, shaded);
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

    View3D ViewOf(Scene3D scene) => new(gpu.Device)
    {
        Camera = scene.Camera.ToCamera(), Sun = scene.Sun ?? default, Style = scene.Style, Background = Colors.LinearFromHex(scene.Background), Time = scene.Time,
    };

    [Fact]
    public void AGearedFigureOnANodeIsDrawnAsBlenderDrewIt()
    {
        var scene = Scenes3D.All.Single(s => s.Name == "gear-swing");
        using var figure = Model.Load(gpu.Device, ModelPath("figure.glb"));
        using var sword = Model.Load(gpu.Device, ModelPath("sword.glb"));
        using var pennant = Model.Load(gpu.Device, ModelPath("pennant.glb"));
        using var tabard = Model.Load(gpu.Device, ModelPath("tabard.glb"));
        using var view = ViewOf(scene);
        // The figure in its swing (a state machine's one state, so the pennant plays its own swing with it), 13 frames in.
        var skeleton = new Skeleton(figure.Data);
        var stack = new AnimationStack(skeleton);
        var clips = AnimationClip.AllOf(figure.Data);
        var machine = new StateMachine(new StateMachineBuilder("swing").Layer("base", BlendMode.Replace, "swing", l => l.State("swing", "swing")).Build(),
            stack, name => new ClipBinding(clips.Single(c => c.Name == name), skeleton));
        var animator = new Animator(stack, machine);
        animator.Seek(13 / 60.0);
        var world = new SceneView(view);
        var hero = world.Root.Add("hero");
        hero.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 40 * MathF.PI / 180);
        var actor = hero.Attach(new Actor(figure, animator) { Palette = new Palette(scene.Palette, scene.Shadows) });
        actor.Items.Add(new ActorItem(sword, new Socket(skeleton, "socket.hand.R")));
        actor.Items.Add(new ActorItem(pennant, new Socket(skeleton, "socket.back"), new ItemAnimation(pennant.Data, machine)));
        actor.Garments.Add(new ActorGarment(tabard, new Garment(tabard.Data, skeleton)));
        Assert.True(world.Refresh());
        Assert.All(view.Items, i => Assert.Same(actor, i.Group));
        MatchesBlender(scene, view, "scene-gear-swing");
    }

    [Fact]
    public void TheTerrainMapPlacedByNodesIsDrawnAsBlenderDrewIt()
    {
        var scene = Scenes3D.All.Single(s => s.Name == "terrain-map");
        using var terrain = Terrain.Load(gpu.Device, Path.Combine(Render3DReferenceTests.TerrainDirectory, scene.Terrain!.Value.File));
        using var shapes = Model.Load(gpu.Device, ModelPath(scene.Models[0].File));
        using var view = new View3D(gpu.Device) { Style = scene.Style, Background = Colors.LinearFromHex(scene.Background), Time = scene.Time };
        var palette = new Palette(scene.Palette, scene.Shadows);
        var world = new SceneView(view);
        // The map under a moved node, moved back: terrain is placed by its node's world.
        var region = world.Root.Add("region");
        region.Translation = new(-3, 0, 2);
        var map = region.Add("map");
        map.Translation = scene.Terrain.Value.Translation + new Vector3(3, 0, -2);
        var ground = map.Attach(new PlacedTerrain(terrain) { Palette = palette });
        var placed = scene.Models[0];
        Assert.True(Matrix4x4.Decompose(placed.Model, out var scale, out var rotation, out var translation));
        var feature = map.Add("shapes");
        feature.SetLocal(translation, rotation, scale);
        feature.Attach(new PlacedModel(shapes) { Palette = palette });
        // The camera's node placed where the scene's look-at puts it: its −Z toward the target, its +Y up.
        var camera3D = scene.Camera;
        Matrix4x4.Invert(Matrix4x4.CreateLookAt(camera3D.Position, camera3D.Target, camera3D.Up), out var cameraWorld);
        var eye = world.Root.Add("eye");
        eye.SetLocal(camera3D.Position, Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(cameraWorld)), Vector3.One);
        world.Camera = eye.Attach(new SceneCamera
        {
            Projection = camera3D.Projection, OrthographicHeight = camera3D.OrthographicHeight, VerticalFieldOfView = camera3D.FieldOfViewDegrees * MathF.PI / 180,
            Near = camera3D.Near, Far = camera3D.Far,
        });
        // The sun's node turned so its +Z points toward the sun.
        var toSun = Vector3.Normalize(scene.Sun!.Value.Direction);
        var sun = world.Root.Add("sun");
        sun.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Normalize(Vector3.Cross(Vector3.UnitZ, toSun)), MathF.Acos(Vector3.Dot(Vector3.UnitZ, toSun)));
        world.Sun = sun.Attach(new SceneSun { Strength = scene.Sun.Value.Strength, Angle = scene.Sun.Value.Angle });
        Assert.True(world.Refresh());
        Assert.Contains(view.Items, i => i.Group == (object)ground);
        MatchesBlender(scene, view, "scene-terrain-map");
    }
}
