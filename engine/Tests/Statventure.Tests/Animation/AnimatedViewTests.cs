using System.Diagnostics;
using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Statventure.UI.Dom;
using Xunit.Abstractions;

namespace Statventure.Tests.Animation;

/// <summary>
/// Animated views (Docs/Animation.md, step 7): figures whose animators run on the document's frame
/// clock (the same as Web Animations', at the view's rate), a view redrawn only when a pose,
/// placement or gear changed, its picture the figure at its clock's time, and the cost per figure.
/// </summary>
[Collection("GPU")]
public class AnimatedViewTests(GpuFixture gpu)
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
    public void AViewIsDrawnAgainOnlyWhenAPoseOrPlacementChanges()
    {
        using var model = Model.Load(gpu.Device, ModelPath("figure.glb"));
        using var view = NewView();
        var (animator, _) = Swinging(model.Data);
        var actor = new Actor(model, animator);
        var animated = new AnimatedView(view);
        animated.Actors.Add(actor);
        var document = Document();
        document.AddFrameClient(animated);
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
        // Moving the figure draws it again; a paused clock doesn't.
        actor.Transform = Matrix4x4.CreateTranslation(0.1f, 0, 0);
        document.Update(46 / 60.0);
        Assert.True(Render(view));
        animator.Seek(0.2);
        animated.Rate = 0;
        document.Update(47 / 60.0);
        Assert.True(Render(view));
        document.Update(48 / 60.0);
        Assert.False(Render(view));
    }

    [Fact]
    public void ThePictureIsTheFigureAtItsClocksTime()
    {
        using var model = Model.Load(gpu.Device, ModelPath("figure.glb"));
        using var view = NewView();
        var (animator, swing) = Swinging(model.Data);
        var animated = new AnimatedView(view);
        animated.Actors.Add(new Actor(model, animator));
        var document = Document();
        document.AddFrameClient(animated);
        for (var frame = 1; frame <= 15; frame++)
            document.Update(frame / 60.0);
        Render(view);
        // Added before frame 1, its clock reads 14 frames at frame 15: the swing's pose then, drawn directly.
        Assert.Equal(14 / 60.0, animator.Time, 9);
        using var direct = NewView();
        var items = new List<DrawItem>();
        model.Draw(items, Matrix4x4.Identity, pose: swing.Pose(14 / 60.0));
        direct.Items = items;
        Render(direct);
        Assert.Equal(direct.Picture!.ReadPixels(), view.Picture!.ReadPixels());
    }

    [Fact]
    public void ClipsRunOnTheDocumentsClockAtTheViewsRate()
    {
        using var model = Model.Load(gpu.Device, ModelPath("figure.glb"));
        using var view = NewView();
        var (animator, _) = Swinging(model.Data);
        var (slowAnimator, _) = Swinging(model.Data);
        var animated = new AnimatedView(view);
        animated.Actors.Add(new Actor(model, animator));
        var slow = new AnimatedView(view) { Rate = 1 / 1.5 };
        slow.Actors.Add(new Actor(model, slowAnimator));
        var document = Document();
        UiAnimation? animation = null;
        // An element animation started in the same task as the views join the document: Blink
        // starts it at the next frame's time, and the views' clocks start at that frame too.
        document.RunScript(() =>
        {
            animation = document.GetElementById("a")!.Animate([new Keyframe { ["opacity"] = "0" }, new Keyframe { ["opacity"] = "1" }], 2000);
            document.AddFrameClient(animated);
            document.AddFrameClient(slow);
        });
        for (var frame = 1; frame <= 40; frame++)
        {
            document.Update(frame / 60.0);
            Assert.Equal(animation!.CurrentTime!.Value / 1000, animator.Time, 6);
            Assert.Equal(animation.CurrentTime.Value / 1000 / 1.5, slowAnimator.Time, 6);
        }
    }

    /// <summary>The cost per figure, measured alone (the "Speed" collection).</summary>
    [Collection("Speed")]
    public class Speed(GpuFixture gpu, ITestOutputHelper output)
    {
        [Fact]
        public void TheCostOfAFigureIsMeasured()
        {
            // Two dozen geared figures with state machines (an idle loop and swings), as a battle might
            // hold: the frame's animation work (advancing, transitions, events, poses, draw items).
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
            var animated = new AnimatedView(view);
            var machines = new List<StateMachine>();
            for (var i = 0; i < 24; i++)
            {
                var skeleton = new Skeleton(figure.Data);
                var stack = new AnimationStack(skeleton);
                var machine = new StateMachine(definition, stack, name => new ClipBinding(clips.Single(c => c.Name == name), skeleton));
                machines.Add(machine);
                var actor = new Actor(figure, new Animator(stack, machine)) { Transform = Matrix4x4.CreateTranslation(i % 6 - 2.5f, 0, i / 6 * -1.5f) };
                actor.Items.Add(new ActorItem(sword, new Socket(skeleton, "socket.hand.R")));
                actor.Garments.Add(new ActorGarment(tabard, new Garment(tabard.Data, skeleton)));
                animated.Actors.Add(actor);
            }
            var watch = new Stopwatch();
            const int Frames = 240;
            for (var frame = 0; frame < Frames; frame++)
            {
                if (frame % 20 == 0)
                    machines[frame / 20 % machines.Count].SetTrigger("attack");
                watch.Start();
                animated.Advance(1 / 60.0);
                watch.Stop();
            }
            var perFigure = watch.Elapsed.TotalMilliseconds / Frames / animated.Actors.Count;
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
                animated.Advance(1 / 60.0);
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
