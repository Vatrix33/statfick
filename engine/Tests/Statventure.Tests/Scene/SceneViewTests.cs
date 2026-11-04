using System.Diagnostics;
using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Statventure.UI.Dom;
using Xunit.Abstractions;

namespace Statventure.Tests.Scene;

/// <summary>
/// Scenes and their views (Docs/Scene.md, "The node tree and the scene"; M7's animated views moved to
/// them, Q1): figures on nodes whose animators run on the document's frame clock (the same as Web
/// Animations', at the scene's rate), a view given new draw items only when a pose, a node, the tree
/// or an attachment changed, its picture the figure at its clock's time, the view's camera and sun
/// from their nodes, picking owners, and the cost per figure.
/// </summary>
[Collection("GPU")]
public class SceneViewTests(GpuFixture gpu)
{
    static string ModelPath(string file) => Path.Combine(Render3DReferenceTests.ModelDirectory, file);
    static readonly Camera3D Camera = new(Projection.Orthographic, new(2.4f, 1.6f, 3.2f), new(0, 1.05f, 0), OrthographicHeight: 2.5f);
    static readonly SunLight Sun = new(new(1, 1.1f, 0.7f), 3);

    static UiDocument Document()
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(300, 200);
        Markup.SetInnerHtml(document.Body, """<div id="a"></div>""");
        document.Update(0);
        return document;
    }

    static View3D NewView(Statventure.Graphics.GpuDevice device) => new(device) { Camera = Camera.ToCamera(), Sun = Sun, Background = Colors.LinearFromHex("#d8cbb0") };

    View3D NewView() => NewView(gpu.Device);

    bool Render(View3D view)
    {
        var frame = gpu.Device.BeginFrame();
        var rendered = view.Update(frame, 160, 200, 1);
        gpu.Device.EndFrame(frame);
        gpu.Device.WaitIdle();
        return rendered;
    }

    /// <summary>The figure playing its swing once (held at its end afterwards) from the animator's time 0.</summary>
    static (Animator Animator, ClipBinding Swing) Swinging(ModelData figure)
    {
        var skeleton = new Skeleton(figure);
        var stack = new AnimationStack(skeleton);
        var swing = new ClipBinding(AnimationClip.AllOf(figure).Single(c => c.Name == "swing"), skeleton);
        stack.Add(swing, 0);
        return (new Animator(stack), swing);
    }

    [Fact]
    public void AViewIsDrawnAgainOnlyWhenAPoseOrAPlacementChanges()
    {
        using var model = Model.Load(gpu.Device, ModelPath("figure.glb"));
        using var view = NewView();
        var (animator, _) = Swinging(model.Data);
        var scene = new SceneView(view);
        var stage = scene.Root.Add("stage");
        var mark = stage.Add("mark");
        var actor = mark.Attach(new Actor(model, animator));
        var document = Document();
        document.AddFrameClient(scene);
        var renders = new List<int>();
        for (var frame = 1; frame <= 45; frame++)
        {
            document.Update(frame / 60.0);
            renders.Add(Render(view) ? 1 : 0);
        }
        // Frame 1 shows it; the swing (36 frames) changes the pose on every frame until it holds its
        // last pose: from frame 38 on nothing changes.
        Assert.Equal(37, renders.Take(37).Sum());
        Assert.Equal(0, renders.Skip(37).Sum());
        // Moving the figure's node draws it again, and so does moving a node above it; setting the same
        // place again doesn't, nor does a paused clock.
        var frameTime = 46;
        bool Next()
        {
            document.Update(frameTime++ / 60.0);
            return Render(view);
        }
        mark.Translation = new(0.1f, 0, 0);
        Assert.True(Next());
        Assert.False(Next());
        stage.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.2f);
        Assert.True(Next());
        mark.Translation = mark.Translation;
        Assert.False(Next());
        animator.Seek(0.2);
        scene.Rate = 0;
        Assert.True(Next());
        Assert.False(Next());
        // Its palette, and gear put on or taken off, draw it again.
        actor.Palette = new Palette(new Dictionary<string, string> { ["cloth"] = "#336699" });
        Assert.True(Next());
        Assert.False(Next());
        using var sword = Model.Load(gpu.Device, ModelPath("sword.glb"));
        var item = new ActorItem(sword, new Socket(animator.Stack.Skeleton, "socket.hand.R"));
        actor.Items.Add(item);
        Assert.True(Next());
        Assert.False(Next());
        actor.Items.Remove(item);
        Assert.True(Next());
        // Taken off its node, it isn't drawn; on a node outside the tree neither (nothing the scene
        // shows changes); with that node added to the tree, it is again.
        mark.Detach(actor);
        Assert.True(Next());
        Assert.Empty(view.Items);
        Assert.Empty(scene.Actors);
        var other = new SceneNode("other");
        other.Attach(actor);
        Assert.False(Next());
        scene.Root.Add(other);
        Assert.True(Next());
        Assert.NotEmpty(view.Items);
        Assert.Equal([actor], scene.Actors);
        // Attached to another node, it leaves the one it was on; attached to a node already in the
        // tree, it's drawn there.
        var third = scene.Root.Add("third");
        Assert.True(Next());
        third.Attach(actor);
        Assert.True(Next());
        Assert.Empty(other.Attachments);
        Assert.Equal([actor], scene.Actors);
        Assert.Same(third, actor.Node);
        // Something new attached to a node in the tree is drawn.
        Assert.False(Next());
        var placed = third.Attach(new PlacedModel(sword));
        Assert.True(Next());
        Assert.Contains(view.Items, i => i.Group == (object)placed);
    }

    [Fact]
    public void AScenesRootStaysTheRoot()
    {
        using var view = NewView();
        var scene = new SceneView(view);
        Assert.Throws<ArgumentException>(() => new SceneNode().Add(scene.Root));
        Assert.Null(scene.Root.Parent);
        Assert.Same(scene, scene.Root.Add("a").Add("b").Scene);
        Assert.Null(new SceneNode().Scene);
    }

    /// <summary>
    /// Terrain's level of detail follows the view's camera and the height of its picture, so a scene
    /// with terrain gives new items when either changes; one without doesn't for a camera moved.
    /// </summary>
    [Fact]
    public void TerrainIsDrawnAgainWhenTheViewMovesOrResizes()
    {
        using var terrain = Terrain.Load(gpu.Device, Path.Combine(Render3DReferenceTests.TerrainDirectory, "test.json"));
        using var view = NewView();
        var scene = new SceneView(view);
        Assert.True(scene.Refresh());
        view.Camera.Position += new Vector3(0, 0.5f, 0);
        Assert.False(scene.Refresh());
        scene.Root.Add("map").Attach(new PlacedTerrain(terrain));
        Assert.True(scene.Refresh());
        Assert.False(scene.Refresh());
        view.Camera.Position += new Vector3(0, 0.5f, 0);
        Assert.True(scene.Refresh());
        Assert.False(scene.Refresh());
        // The first picture gives the view's height (the finest level until then).
        Render(view);
        Assert.True(scene.Refresh());
        Render(view);
        Assert.False(scene.Refresh());
    }

    [Fact]
    public void ThePictureIsTheFigureAtItsClocksTime()
    {
        using var model = Model.Load(gpu.Device, ModelPath("figure.glb"));
        using var view = NewView();
        var (animator, swing) = Swinging(model.Data);
        var scene = new SceneView(view);
        var mark = scene.Root.Add("stage").Add("mark");
        mark.Parent!.SetLocal(new(0.1f, 0, -0.2f), Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.3f), new(0.9f, 0.9f, 0.9f));
        mark.Translation = new(0, 0.05f, 0);
        mark.Attach(new Actor(model, animator));
        var document = Document();
        document.AddFrameClient(scene);
        for (var frame = 1; frame <= 15; frame++)
            document.Update(frame / 60.0);
        Render(view);
        // Added before frame 1, its clock reads 14 frames at frame 15: the swing's pose then, drawn
        // directly where its node places it.
        Assert.Equal(14 / 60.0, animator.Time, 9);
        using var direct = NewView();
        var items = new List<DrawItem>();
        model.Draw(items, mark.World, pose: swing.Pose(14 / 60.0));
        direct.Items = items;
        Render(direct);
        Assert.Equal(direct.Picture!.ReadPixels(), view.Picture!.ReadPixels());
    }

    [Fact]
    public void ClipsRunOnTheDocumentsClockAtTheScenesRate()
    {
        using var model = Model.Load(gpu.Device, ModelPath("figure.glb"));
        using var view = NewView();
        var (animator, _) = Swinging(model.Data);
        var (slowAnimator, _) = Swinging(model.Data);
        var scene = new SceneView(view);
        scene.Root.Add("a").Attach(new Actor(model, animator));
        var slow = new SceneView(view) { Rate = 1 / 1.5 };
        slow.Root.Add("b").Attach(new Actor(model, slowAnimator));
        var document = Document();
        UiAnimation? animation = null;
        // An element animation started in the same task as the scenes join the document: Blink
        // starts it at the next frame's time, and the scenes' clocks start at that frame too.
        document.RunScript(() =>
        {
            animation = document.GetElementById("a")!.Animate([new Keyframe { ["opacity"] = "0" }, new Keyframe { ["opacity"] = "1" }], 2000);
            document.AddFrameClient(scene);
            document.AddFrameClient(slow);
        });
        for (var frame = 1; frame <= 40; frame++)
        {
            document.Update(frame / 60.0);
            Assert.Equal(animation!.CurrentTime!.Value / 1000, animator.Time, 6);
            Assert.Equal(animation.CurrentTime.Value / 1000 / 1.5, slowAnimator.Time, 6);
        }
    }

    /// <summary>
    /// A figure's clip events are delivered with the frame's animation events, in the tree's order;
    /// one taken out of the tree isn't advanced or dispatched by the scene.
    /// </summary>
    [Fact]
    public void TheFiguresEventsComeWithTheFramesInTheTreesOrder()
    {
        using var model = Model.Load(gpu.Device, ModelPath("figure.glb"));
        using var view = NewView();
        var scene = new SceneView(view);
        var heard = new List<string>();
        var actors = new[] { "first", "second" }.Select(name =>
        {
            var (animator, _) = Swinging(model.Data);
            animator.Event += e => heard.Add($"{name} {e.Name}");
            return (Name: name, Actor: new Actor(model, animator));
        }).ToArray();
        // "second" is attached first, but "first" stands on the earlier node.
        var a = scene.Root.Add("a");
        var b = scene.Root.Add("b");
        b.Attach(actors[1].Actor);
        a.Attach(actors[0].Actor);
        var document = Document();
        document.AddFrameClient(scene);
        for (var frame = 1; frame <= 12; frame++)
            document.Update(frame / 60.0);
        // The swing's "whoosh" is at frame 8 of the clip.
        Assert.Equal(["first whoosh", "second whoosh"], heard);
        b.Remove();
        for (var frame = 13; frame <= 30; frame++)
            document.Update(frame / 60.0);
        Assert.Equal(["first whoosh", "second whoosh", "first hit"], heard);
        Assert.Equal(11 / 60.0, actors[1].Actor.Animator.Time, 9);
    }

    /// <summary>
    /// A stage instanced in the scene isn't drawn again while its clip holds still (the same values
    /// set again are no change), and is when a node of it moves or its palette changes.
    /// </summary>
    [Fact]
    public void AStillStageIsntDrawnAgain()
    {
        using var model = Model.Load(gpu.Device, ModelPath("hierarchy.glb"));
        using var view = NewView();
        var scene = new SceneView(view);
        var instance = scene.Root.Add("stage").Instance(model);
        var clip = instance.Clip("base");
        instance.Apply(clip, 0);
        Assert.True(scene.Refresh());
        for (var time = 2.0; time < 2.1; time += 0.02)
        {
            instance.Apply(clip, time);
            Assert.False(scene.Refresh());
        }
        instance.Apply(clip, 0.5);
        Assert.True(scene.Refresh());
        Assert.False(scene.Refresh());
        instance.NodeNamed("Tip")!.Scale = new(2, 2, 2);
        Assert.True(scene.Refresh());
        // A node taken out of the scene isn't drawn.
        var tip = instance.Skeleton.Find("Tip");
        Assert.Contains(view.Items, i => i.Part == tip + 1);
        instance.NodeOf(tip)!.Remove();
        Assert.True(scene.Refresh());
        Assert.DoesNotContain(view.Items, i => i.Part == tip + 1);
        Assert.Contains(view.Items, i => i.Part == instance.Skeleton.Find("Post") + 1);
        instance.Palette = new Palette(new Dictionary<string, string> { ["stone"] = "#445566" });
        Assert.True(scene.Refresh());
        Assert.False(scene.Refresh());
        // The style gives the shadow tones the palette doesn't: a new one draws again.
        view.Style = view.Style with { ShadowMix = 0.5f };
        Assert.True(scene.Refresh());
        Assert.False(scene.Refresh());
    }

    /// <summary>
    /// A hidden node hides its subtree (Docs/Scene.md, Q23): what hangs on it or below it isn't drawn,
    /// an instanced object's nodes included, while figures there go on animating; shown again, it's
    /// drawn as before.
    /// </summary>
    [Fact]
    public void AHiddenNodeHidesItsSubtree()
    {
        using var stage = Model.Load(gpu.Device, ModelPath("hierarchy.glb"));
        using var figure = Model.Load(gpu.Device, ModelPath("figure.glb"));
        using var view = NewView();
        var scene = new SceneView(view);
        var instance = scene.Root.Add("stage").Instance(stage);
        var (animator, _) = Swinging(figure.Data);
        var group = scene.Root.Add("group");
        var actor = group.Add("mark").Attach(new Actor(figure, animator));
        Assert.True(scene.Refresh());
        var all = view.Items.Count;
        int Part(string name) => instance.Skeleton.Find(name) + 1;
        // The post hides the tip with it; the base stays.
        var post = instance.NodeNamed("Post")!;
        post.Hidden = true;
        Assert.False(post.Parent!.Hidden);
        Assert.True(scene.Refresh());
        Assert.DoesNotContain(view.Items, i => i.Group is SceneNode && (i.Part == Part("Post") || i.Part == Part("Tip")));
        Assert.Contains(view.Items, i => i.Group is SceneNode && i.Part == Part("Base"));
        Assert.False(instance.NodeNamed("Tip")!.Shown);
        Assert.True(instance.NodeNamed("Base")!.Shown);
        post.Hidden = true;
        Assert.False(scene.Refresh());
        // A figure below a hidden node isn't drawn but goes on animating.
        group.Hidden = true;
        scene.Advance(0.1);
        Assert.DoesNotContain(view.Items, i => i.Group == (object)actor);
        Assert.Equal(0.1, animator.Time, 9);
        group.Hidden = false;
        post.Hidden = false;
        Assert.True(scene.Refresh());
        Assert.Equal(all, view.Items.Count);
        Assert.Contains(view.Items, i => i.Group == (object)actor);
    }

    /// <summary>
    /// The view's camera and sun come from the nodes they hang on, as glTF places them: a camera looks
    /// down its node's −Z with its +Y up, a sun shines down its node's −Z; without them the view keeps
    /// its own.
    /// </summary>
    [Fact]
    public void TheViewTakesItsCameraAndSunFromTheirNodes()
    {
        using var view = NewView();
        var scene = new SceneView(view);
        var own = view.Camera;
        var ownPosition = own.Position;
        scene.Refresh();
        Assert.Equal(ownPosition, view.Camera.Position);
        Assert.Equal(Sun, view.Sun);
        var rig = scene.Root.Add("rig");
        rig.Translation = new(0, 2, 0);
        var holder = rig.Add("camera");
        // A quarter turn about Y: −Z (forward) turns to −X, +Y (up) stays.
        holder.SetLocal(new(5, 0, 1), Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2), new(2, 2, 2));
        var camera = holder.Attach(new SceneCamera { Projection = Projection.Orthographic, OrthographicHeight = 3, VerticalFieldOfView = 0.5f, Near = 0.5f, Far = 40 });
        var light = scene.Root.Add("sun");
        // Turned so that −Z points down: +Z, toward the sun, is up.
        light.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, -MathF.PI / 2);
        var sun = light.Attach(new SceneSun { Strength = 2.5f, Angle = 5 });
        scene.Camera = camera;
        scene.Sun = sun;
        Assert.True(Render(view));
        scene.Refresh();
        Assert.Same(own, view.Camera);
        Assert.Equal(new Vector3(5, 2, 1), view.Camera.Position);
        Assert.True(Vector3.Distance(new Vector3(4, 2, 1), view.Camera.Target) < 1e-5f, $"{view.Camera.Target}");
        Assert.True(Vector3.Distance(Vector3.UnitY, view.Camera.Up) < 1e-5f, $"{view.Camera.Up}");
        Assert.Equal((Projection.Orthographic, 3f, 0.5f, 0.5f, 40f),
            (view.Camera.Projection, view.Camera.OrthographicHeight, view.Camera.VerticalFieldOfView, view.Camera.Near, view.Camera.Far));
        Assert.True(Vector3.Distance(Vector3.UnitY, view.Sun.Direction) < 1e-5f, $"{view.Sun.Direction}");
        Assert.Equal((2.5f, 5f), (view.Sun.Strength, view.Sun.Angle));
        Assert.True(Render(view));
        Assert.False(Render(view));
        // Moving a node above the camera moves the view.
        rig.Translation = new(0, 3, 0);
        scene.Refresh();
        Assert.Equal(new Vector3(5, 3, 1), view.Camera.Position);
        Assert.True(Render(view));
        // Without a scene camera the view keeps the camera it has.
        scene.Camera = null;
        rig.Translation = new(0, 4, 0);
        scene.Refresh();
        Assert.Equal(new Vector3(5, 3, 1), view.Camera.Position);
    }

    /// <summary>
    /// What picking gives back: an instanced model's items belong to their top-level object's node
    /// (each its own model), a figure's and its gear's to the actor, a placed model's to itself; and
    /// a pixel of a picture finds its owner.
    /// </summary>
    [Fact]
    public void PickingGivesBackTheObjectsTheFiguresAndTheModels()
    {
        using var stage = Model.Load(gpu.Device, ModelPath("hierarchy.glb"));
        using var figure = Model.Load(gpu.Device, ModelPath("figure.glb"));
        using var sword = Model.Load(gpu.Device, ModelPath("sword.glb"));
        using var view = NewView();
        view.Camera = new Camera3D(Projection.Perspective, new(0.3f, 2.2f, 4.6f), new(0, 0.5f, 0), FieldOfViewDegrees: 45).ToCamera();
        var scene = new SceneView(view);
        var instance = scene.Root.Add("stage").Instance(stage);
        var (animator, _) = Swinging(figure.Data);
        var actor = scene.Root.Add("hero").Attach(new Actor(figure, animator));
        actor.Node!.Translation = new(0, 0, 1.5f);
        actor.Items.Add(new ActorItem(sword, new Socket(animator.Stack.Skeleton, "socket.hand.R")));
        var placed = scene.Root.Add("block").Attach(new PlacedModel(stage));
        placed.Node!.Translation = new(0, 0, -3);
        scene.Refresh();
        var owners = view.Items.Select(i => i.Group).Distinct().ToArray();
        // The hierarchy's four top-level objects (the mirrored branch and the rig carry meshes below them), the figure with its sword, the placed model.
        Assert.Equal(instance.Roots.Cast<object>().Append(actor).Append(placed), owners);
        foreach (var item in view.Items.Where(i => i.Group is SceneNode))
        {
            var top = (SceneNode)item.Group!;
            Assert.Equal(top.World.Translation, item.Anchor);
        }
        Assert.All(view.Items.Where(i => i.Group == (object)actor), i => Assert.Equal(actor.Node.World.Translation, i.Anchor));
        // A pixel of the figure picks the actor.
        Assert.True(Render(view));
        var centre = Vector4.Transform(new Vector4(actor.Node.World.Translation + new Vector3(0, 1.2f, 0), 1), view.Camera.ViewProjection(160 / 200f));
        var pixel = new Vector2((centre.X / centre.W + 1) / 2 * 160, (centre.Y / centre.W + 1) / 2 * 200);
        Assert.Equal(actor, view.Pick(pixel)?.Owner);
    }

    /// <summary>The cost per figure, measured alone (the "Speed" collection).</summary>
    [Collection("Speed")]
    public class Speed(GpuFixture gpu, ITestOutputHelper output)
    {
        [Fact]
        public void TheCostOfAFigureIsMeasured()
        {
            // Two dozen geared figures with state machines (an idle loop and swings), as a battle might
            // hold, each on a node of a stage: the frame's animation work (advancing, transitions,
            // events, poses, draw items).
            using var figure = Model.Load(gpu.Device, ModelPath("figure.glb"));
            using var sword = Model.Load(gpu.Device, ModelPath("sword.glb"));
            using var tabard = Model.Load(gpu.Device, ModelPath("tabard.glb"));
            using var view = NewView(gpu.Device);
            view.Camera = new Camera3D(Projection.Orthographic, new(0, 4, 9), new(0, 0.8f, -2.2f), OrthographicHeight: 6).ToCamera();
            var definition = new StateMachineBuilder("humanoid").Trigger("attack")
                .Layer("base", BlendMode.Replace, "idle", l => l.State("idle", "idle", loop: true))
                .Layer("action", BlendMode.Replace, "none", l => l.Empty("none").State("swing", "swing")
                    .Transition("none", "swing", 0.1, ConditionDefinition.Trigger("attack")).TransitionAfter("swing", "none", 1, 0.15))
                .Build();
            var clips = AnimationClip.AllOf(figure.Data);
            var scene = new SceneView(view);
            var stage = scene.Root.Add("stage");
            var machines = new List<StateMachine>();
            for (var i = 0; i < 24; i++)
            {
                var skeleton = new Skeleton(figure.Data);
                var stack = new AnimationStack(skeleton);
                var machine = new StateMachine(definition, stack, name => new ClipBinding(clips.Single(c => c.Name == name), skeleton));
                machines.Add(machine);
                var mark = stage.Add($"mark.{i}");
                mark.Translation = new(i % 6 - 2.5f, 0, i / 6 * -1.5f);
                var actor = mark.Attach(new Actor(figure, new Animator(stack, machine)));
                actor.Items.Add(new ActorItem(sword, new Socket(skeleton, "socket.hand.R")));
                actor.Garments.Add(new ActorGarment(tabard, new Garment(tabard.Data, skeleton)));
            }
            var watch = new Stopwatch();
            const int Frames = 240;
            for (var frame = 0; frame < Frames; frame++)
            {
                if (frame % 20 == 0)
                    machines[frame / 20 % machines.Count].SetTrigger("attack");
                watch.Start();
                scene.Advance(1 / 60.0);
                watch.Stop();
            }
            var perFigure = watch.Elapsed.TotalMilliseconds / Frames / scene.Actors.Count;
            output.WriteLine($"animation work: {perFigure * 1000:F1} µs per figure per frame ({watch.Elapsed.TotalMilliseconds / Frames:F2} ms a frame for 24 geared figures)");
            // A loose bound (a Debug build on whatever machine runs the tests): a 60 fps frame is 16.7 ms.
            Assert.True(perFigure < 0.5, $"{perFigure:F3} ms per figure per frame");
            // The GPU's share, on request (slow on a CPU Vulkan): the 24 figures drawn at 1080p, each
            // frame a new pose (skinning, shading, ink, hatching), submitted and waited for.
            if (Environment.GetEnvironmentVariable("STATVENTURE_MEASURE") != "1")
                return;
            var gpuWatch = new Stopwatch();
            for (var frame = 0; frame < 30; frame++)
            {
                scene.Advance(1 / 60.0);
                if (frame == 10)
                    gpuWatch.Restart();
                var f = gpu.Device.BeginFrame();
                view.Update(f, 1920, 1080, 1);
                gpu.Device.EndFrame(f);
                gpu.Device.WaitIdle();
            }
            output.WriteLine($"1080p frame of 24 geared figures: {gpuWatch.Elapsed.TotalMilliseconds / 20:F2} ms (render, submit and wait)");
        }
    }
}
