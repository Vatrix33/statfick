using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Tests.Canvas;
using Statventure.Tests.Render3D;
using Statventure.UI.Dom;

namespace Statventure.Tests.Animation;

/// <summary>
/// Clip events (Docs/Animation.md, "Clip events"): the figure's pose markers come through the export
/// as the clips' events (checked against Blender's markers in AnimationReferenceTests), and strips
/// fire them by the user's rules: every acting strip (influence above 0, fading included; held
/// strips not), once per pass through its part, in time order; a long step fires all it passes, a
/// jump nothing; in a document they're dispatched after the frame's animation events, before its
/// frame callbacks, and awaits continue after each.
/// </summary>
public class AnimationEventTests
{
    static readonly ModelData Figure = ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, "figure.glb"));
    static readonly AnimationClip[] Clips = AnimationClip.AllOf(Figure);
    static readonly Skeleton Skeleton = new(Figure);

    static ClipBinding Clip(string name) => new(Clips.Single(c => c.Name == name), Skeleton);

    /// <summary>Plays the stack frame by frame (60 Hz) up to <paramref name="seconds"/>: every event with the frame it came in.</summary>
    static List<(int Frame, ClipEvent Event)> Play(AnimationStack stack, double seconds)
    {
        var animator = new Animator(stack);
        var fired = new List<(int, ClipEvent)>();
        var frame = 0;
        animator.Event += e => fired.Add((frame, e));
        for (frame = 1; frame <= Math.Round(seconds * 60); frame++)
        {
            animator.Advance(1 / 60.0);
            animator.DispatchEvents();
        }
        return fired;
    }

    static void Times(double[] expected, IEnumerable<ClipEvent> events) =>
        Assert.Equal(expected.Select(t => Math.Round(t, 6)), events.Select(e => Math.Round(e.Time, 6)));

    [Fact]
    public void TheClipsCarryTheFiguresMarkers()
    {
        Assert.Equal([("whoosh", 8 / 60.0), ("hit", 19 / 60.0)], Clips.Single(c => c.Name == "swing").Events.Select(e => (e.Name, e.Time)));
        // Two markers at one frame keep their order.
        Assert.Equal(["blink", "breath"], Clips.Single(c => c.Name == "idle").Events.Select(e => e.Name));
        Assert.Empty(Clips.Single(c => c.Name == "twist").Events);
    }

    [Fact]
    public void AStripFiresEachMarkerInTheFrameItIsPassed()
    {
        var stack = new AnimationStack(Skeleton);
        stack.Add(Clip("swing"), 0.5);
        var fired = Play(stack, 2);
        // 0.5 s + 8 and 19 frames: at frames 38 and 49, each in the frame whose time it is.
        Assert.Equal([(38, "whoosh"), (49, "hit")], fired.Select(f => (f.Frame, f.Event.Name)));
        Times([0.5 + 8 / 60.0, 0.5 + 19 / 60.0], fired.Select(f => f.Event));
        // Each fires where the strip's clip time is the marker's (Blender's strip time, nla-timing).
        foreach (var (_, e) in fired)
            Assert.Equal(Clips.Single(c => c.Name == "swing").Events.Single(m => m.Name == e.Name).Time, e.Strip.ClipTimeAt(e.Time), 6);
    }

    [Fact]
    public void LoopsFireOncePerPassAndALongStepFiresAllItPasses()
    {
        var stack = new AnimationStack(Skeleton);
        stack.Add(Clip("idle"), 0, s => s.Repeat = 3);
        var animator = new Animator(stack);
        var fired = new List<ClipEvent>();
        animator.Event += fired.Add;
        animator.Advance(4.5);
        animator.DispatchEvents();
        Assert.Equal(["blink", "breath", "blink", "breath"], fired.Select(e => e.Name));
        Times([1, 1, 3, 3], fired);
        animator.Advance(2);
        animator.DispatchEvents();
        Assert.Equal(6, fired.Count);
        // One step over two layers' events: in time order, not layer by layer.
        stack = new AnimationStack(Skeleton);
        stack.Add(Clip("swing"), 0);
        stack.Add(Clip("swing"), 0.05);
        animator = new Animator(stack);
        fired.Clear();
        animator.Event += fired.Add;
        animator.Advance(1);
        animator.DispatchEvents();
        Assert.Equal([("whoosh", 0), ("whoosh", 1), ("hit", 0), ("hit", 1)], fired.Select(e => (e.Name, e.Layer)));
    }

    [Fact]
    public void StripTimeMovesTheEvents()
    {
        // Reversed: the part backwards, so the hit (19 of 36 frames) before the whoosh (8).
        var stack = new AnimationStack(Skeleton);
        stack.Add(Clip("swing"), 1, s => s.Reversed = true);
        Times([1 + (36 - 19) / 60.0, 1 + (36 - 8) / 60.0], Play(stack, 2).Select(f => f.Event));
        // Half speed doubles the times into the strip.
        stack = new AnimationStack(Skeleton);
        stack.Add(Clip("swing"), 1, s => s.Scale = 2);
        Times([1 + 16 / 60.0, 1 + 38 / 60.0], Play(stack, 3).Select(f => f.Event));
        // A part with 1.5 passes: the pulse's markers on its first and last frame; at the end of the
        // first pass its end, then the second pass's start; the second pass ends before its end.
        stack = new AnimationStack(Skeleton);
        stack.Add(Clip("pulse"), 0.1, s => s.Repeat = 1.5);
        var fired = Play(stack, 3).Select(f => f.Event).ToList();
        Assert.Equal(["start", "end", "start"], fired.Select(e => e.Name));
        Times([0.1, 0.1 + 40 / 60.0, 0.1 + 40 / 60.0], fired);
        // The same in one step.
        var animator = new Animator(stack);
        fired.Clear();
        animator.Event += fired.Add;
        animator.Advance(3);
        animator.DispatchEvents();
        Assert.Equal(["start", "end", "start"], fired.Select(e => e.Name));
    }

    [Fact]
    public void OnlyActingStripsFire()
    {
        // Blending in: at its first frame its influence is 0, so the marker there doesn't fire.
        var stack = new AnimationStack(Skeleton);
        stack.Add(Clip("pulse"), 0.1, s => s.BlendIn = 0.1);
        Assert.Equal(["end"], Play(stack, 2).Select(f => f.Event.Name));
        // Fading out still fires (influence above 0): the hit lies in the blend-out.
        stack = new AnimationStack(Skeleton);
        stack.Add(Clip("swing"), 0, s => s.BlendOut = 0.5);
        Assert.Equal(["whoosh", "hit"], Play(stack, 2).Select(f => f.Event.Name));
        // A held strip fires nothing while held (before its start as a first strip, after its end).
        stack = new AnimationStack(Skeleton);
        stack.Add(Clip("pulse"), 0.5, s => s.Extrapolation = Extrapolation.Hold);
        Times([0.5, 0.5 + 40 / 60.0], Play(stack, 3).Select(f => f.Event));
        // A muted layer fires nothing; a strip under a full Replace strip still acts (influence 1).
        stack = new AnimationStack(Skeleton);
        stack.Add(Clip("swing"), 0);
        stack.Add(Clip("swing"), 1);
        stack.Layers[1].Muted = true;
        Assert.Equal(2, Play(stack, 2).Count);
        stack.Layers[1].Muted = false;
        ((AnimationLayer)stack.Layers[1]).Strips[0].Start = 0;
        // In time order, then bottom layer first.
        Assert.Equal([("whoosh", 0), ("whoosh", 1), ("hit", 0), ("hit", 1)], Play(stack, 2).Select(f => (f.Event.Name, f.Event.Layer)));
    }

    [Fact]
    public void EventsAtOneTimeComeBottomLayerFirstAndJumpsFireNothing()
    {
        var stack = new AnimationStack(Skeleton);
        stack.Add(Clip("swing"), 0.5);
        stack.Add(Clip("pulse"), 0.5 + 19 / 60.0, s => s.Mode = BlendMode.Combine);
        var fired = Play(stack, 2);
        Assert.Equal(["whoosh", "hit", "start", "end"], fired.Select(f => f.Event.Name));
        Assert.Equal(fired[1].Frame, fired[2].Frame);
        Assert.Equal([0, 1], new[] { fired[1].Event.Layer, fired[2].Event.Layer });

        var animator = new Animator(stack);
        var names = new List<string>();
        animator.Event += e => names.Add(e.Name);
        animator.Seek(0.9);
        animator.Advance(0.6);
        animator.DispatchEvents();
        // The jump passed the whoosh, the hit and the pulse's start: only the pulse's end fires.
        Assert.Equal(["end"], names);
        // The rate runs the clock faster: 0.35 s of real time at rate 2 reaches 0.7 s.
        animator.Seek(0);
        animator.Rate = 2;
        animator.Advance(0.35);
        animator.DispatchEvents();
        Assert.Equal(["end", "whoosh"], names);
    }

    [Fact]
    public void InADocumentEventsComeAfterTheFramesAnimationEventsAndBeforeItsFrameCallbacks()
    {
        // The frame a 100 ms element animation's finish event comes in.
        var finishFrame = FinishFrame();
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        Markup.SetInnerHtml(document.Body, """<div id="a"></div>""");
        document.Update(0);
        var log = new List<string>();
        var stack = new AnimationStack(Skeleton);
        // Added before frame 1, the animator's clock reads (frame − 1) / 60: the hit (19 frames into
        // the swing) comes in the finish frame.
        stack.Add(Clip("swing"), (finishFrame - 1 - 19) / 60.0);
        var animator = new Animator(stack);
        animator.Event += e => { if (e.Name == "hit") { log.Add("hit event"); document.QueueMicrotask(() => log.Add("microtask")); } };
        document.AddFrameClient(animator);
        document.RunScript(() =>
        {
            document.GetElementById("a")!.Animate([new Keyframe { ["opacity"] = "0" }, new Keyframe { ["opacity"] = "1" }], 100).OnFinish = _ => log.Add("finish event");
            Await();
        });
        for (var frame = 1; frame <= finishFrame + 2; frame++)
        {
            if (frame == finishFrame)
                document.RequestAnimationFrame(_ => log.Add("frame callback"));
            document.Update(frame / 60.0);
        }
        Assert.Equal(["finish event", "hit event", "microtask", "awaited hit", "frame callback"], log);

        async void Await()
        {
            var e = await animator.Next("hit");
            log.Add("awaited " + e.Name);
        }
    }

    static int FinishFrame()
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        Markup.SetInnerHtml(document.Body, """<div id="a"></div>""");
        document.Update(0);
        var finished = -1;
        var frame = 0;
        document.RunScript(() => document.GetElementById("a")!.Animate([new Keyframe { ["opacity"] = "0" }, new Keyframe { ["opacity"] = "1" }], 100).OnFinish = _ => finished = frame);
        for (frame = 1; frame <= 30 && finished < 0; frame++)
            document.Update(frame / 60.0);
        Assert.True(finished > 0);
        return finished;
    }
}
