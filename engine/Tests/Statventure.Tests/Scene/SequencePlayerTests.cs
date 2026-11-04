using System.Numerics;
using Statventure.Animation;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using static Statventure.Tests.Scene.SequenceBench;

namespace Statventure.Tests.Scene;

/// <summary>
/// The sequence player's clock and ends (Docs/Scene.md, "The player", step 4): the first frame at the
/// start, real time on the frame clock at its own rate (M10-7), the pause, the hidden time dropped, the
/// seek as a jump (S4-5), the end state staying (Q21), the skip and finish to the end state with event
/// cues marked skipped (Q3, Q18, Q20), the reduced variant's cut list (Q19, S4-9), the cue order (Q17),
/// cues at markers following the stage (Q22), and the bindings checked before anything changes (S4-6).
/// </summary>
[Collection("GPU")]
public class SequencePlayerTests(GpuFixture gpu)
{
    const double Step = 0.125;

    static void Log(SequencePlayer player, List<string> log) => player.Cue += cue => log.Add($"{cue.Name}{(cue.Skipped ? " skipped" : "")}");

    [Fact]
    public void TheFirstFrameIsTheStartThenTheClockRunsOnTheFrameClock()
    {
        using var bench = new SequenceBench(gpu);
        bench.Frame(0.5);
        bench.Pivot.Translation = new(9, 1, 2);
        var player = bench.Play();
        Assert.Equal([player], bench.Scene.Playing);
        Assert.Equal((0, 0, 2, false), (player.Time, player.TimelineTime, player.Duration, player.Reduced));
        // Nothing is applied before its first frame, which is its start: the time since the scene's last frame doesn't count.
        Assert.Equal(new Vector3(9, 1, 2), bench.Pivot.Translation);
        bench.Frame(0.25);
        Assert.Equal((0, 0), (player.Time, player.TimelineTime));
        Assert.Equal(new Vector3(0, 1, 2), bench.Pivot.Translation);
        bench.Frame(0.25);
        Assert.Equal((0.25, 0.25), (player.Time, player.TimelineTime));
        Assert.Equal(0.5f, bench.Pivot.Translation.X, 6);
    }

    /// <summary>
    /// Its rate scales its clock and the clocks of the figures it holds (their time is their start's plus
    /// τ, their own rate aside), independent of the scene's battle pace, which the other figures follow.
    /// </summary>
    [Fact]
    public void ItsRateIsItsOwnAndTheFiguresItHoldsFollowIt()
    {
        using var bench = new SequenceBench(gpu);
        bench.Frame(1);
        var origin = bench.Hero.Animator.Time;
        var bystander = bench.Bystander.Animator.Time;
        var player = bench.Play();
        player.Rate = 2;
        bench.Scene.Rate = 0.25;
        bench.Hero.Animator.Rate = 3;
        bench.Frame(Step);
        bench.Frame(Step);
        bench.Frame(Step);
        Assert.Equal(0.5, player.Time, 12);
        Assert.Equal(origin + 0.5, bench.Hero.Animator.Time, 12);
        Assert.Equal(3, bench.Hero.Animator.Rate);
        Assert.Equal(bystander + 3 * Step * 0.25 * 1, bench.Bystander.Animator.Time, 12);
        Assert.Throws<ArgumentOutOfRangeException>(() => player.Rate = -1);
        Assert.Throws<ArgumentOutOfRangeException>(() => player.Rate = double.NaN);
        player.Rate = 0;
        bench.Frame(Step);
        Assert.Equal(0.5, player.Time, 12);
    }

    [Fact]
    public void APauseHoldsItsClockAndTheFiguresItHolds()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Play();
        bench.Frame(Step);
        bench.Frame(Step);
        var hero = bench.Hero.Animator.Time;
        player.Pause();
        Assert.True(player.Paused);
        bench.Frame(Step);
        bench.Frame(Step);
        Assert.Equal((Step, hero), (player.Time, bench.Hero.Animator.Time));
        player.Resume();
        Assert.False(player.Paused);
        bench.Frame(Step);
        Assert.Equal(2 * Step, player.Time, 12);
        Assert.Equal(hero + Step, bench.Hero.Animator.Time, 12);
    }

    /// <summary>
    /// While the document is hidden it gets no frames; the first frame after it is shown again would bring
    /// the whole hidden time: the sequence drops it (M10-7), the figures it doesn't hold take it (as the CSS
    /// animations do).
    /// </summary>
    [Fact]
    public void TheTimeTheDocumentIsHiddenIsDropped()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Play();
        bench.Frame(Step);
        bench.Frame(Step);
        var hero = bench.Hero.Animator.Time;
        var bystander = bench.Bystander.Animator.Time;
        bench.Document.SetHidden(true, bench.Now);
        bench.Document.Update(bench.Now + 1);
        bench.Document.Update(bench.Now + 3);
        bench.Document.SetHidden(false, bench.Now + 5);
        bench.Frame(5 + Step);
        Assert.Equal(2 * Step, player.Time, 9);
        Assert.Equal(hero + Step, bench.Hero.Animator.Time, 9);
        Assert.Equal(bystander + 5 + Step, bench.Bystander.Animator.Time, 9);
        bench.Frame(Step);
        Assert.Equal(3 * Step, player.Time, 9);
    }

    /// <summary>
    /// A seek is a jump (S4-5): nothing it jumps over fires (event cues, triggers), the figures' clocks
    /// jump at once, the state there is applied in the next frame; seeking back re-arms the cues after the
    /// new time. A seek past the end ends it in the next frame, as played; a seek after the end is refused.
    /// </summary>
    [Fact]
    public async Task ASeekIsAJumpThatFiresNothingAndSeekingBackRearms()
    {
        using var bench = new SequenceBench(gpu);
        var sequence = new SequenceBuilder("seek", 2).Role("hero", RoleKind.Actor).Role("pivot", RoleKind.Node)
            .Transform("pivot", t => t.Location(x: new FCurve([FCurveKey.Linear(0, 0), FCurveKey.Linear(2, 4)])))
            .Animation("hero", null, a => a.Trigger(0.75, "attack"))
            .Events(e => e.Event(0.5, "a").Event(1, "b"))
            .Build();
        var player = bench.Scene.Play(sequence, new SequenceBindings().Actor("hero", bench.Hero).Node("pivot", bench.Pivot), bench.Document);
        var log = new List<string>();
        Log(player, log);
        var origin = bench.Hero.Animator.Time;
        var machine = bench.Hero.Animator.Machine!;
        bench.Frame(Step);
        // Onto a cue's time: it counts as passed.
        player.Seek(0.5);
        bench.Frame(Step);
        Assert.Empty(log);
        player.Seek(1.25);
        Assert.Equal((1.25, 1.25), (player.Time, player.TimelineTime));
        Assert.Equal(origin + 1.25, bench.Hero.Animator.Time, 12);
        Assert.Equal(1.25f, bench.Pivot.Translation.X, 5);
        bench.Frame(Step);
        Assert.Equal(2.75f, bench.Pivot.Translation.X, 5);
        Assert.Empty(log);
        Assert.Equal(0, machine.Get("attack"));
        Assert.Equal("none", machine.StateOf("action"));
        player.Seek(0.25);
        bench.Frame(Step);
        Assert.Empty(log);
        bench.Frame(2 * Step);
        Assert.Equal(["a"], log);
        bench.Frame(2 * Step);
        Assert.Equal(["a"], log);
        Assert.Equal("swing", machine.StateOf("action"));
        bench.Frame(2 * Step);
        Assert.Equal(["a", "b"], log);
        player.Seek(5);
        Assert.Equal((2, 2), (player.Time, player.TimelineTime));
        Assert.False(player.Ended);
        bench.Frame(Step);
        Assert.True(player.Ended);
        Assert.True(player.Finished.IsCompleted);
        Assert.False(await player.Finished);
        Assert.Equal(["a", "b"], log);
        Assert.Throws<InvalidOperationException>(() => player.Seek(1));
    }

    /// <summary>
    /// At its end (Q21) the end state stays: the nodes, the stage, the figure on its mark with the layer
    /// the sequence gave it, the palettes; <see cref="SequencePlayer.Finished"/> completes (not skipped) in
    /// the frame's events step; the figures go back to the scene's clock; later frames and the game's own
    /// changes are left alone.
    /// </summary>
    [Fact]
    public async Task AtTheEndTheEndStateStaysAndTheFiguresGoBackToTheScenesClock()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Play();
        var endedAtTheEndCue = new List<bool>();
        player.Cue += cue =>
        {
            if (cue.Name == "end")
                endedAtTheEndCue.Add(player.Ended);
        };
        var origin = bench.Hero.Animator.Time;
        bench.Frame(Step);
        for (var i = 0; i < 15; i++)
            bench.Frame(Step);
        Assert.False(player.Ended);
        Assert.Equal(1.875, player.Time);
        bench.Frame(0.3);
        Assert.Equal((2, 2), (player.Time, player.TimelineTime));
        Assert.True(player.Ended);
        Assert.Equal([false], endedAtTheEndCue);
        Assert.True(player.Finished.IsCompleted);
        Assert.False(await player.Finished);
        player.Pause();
        Assert.False(player.Paused);
        Assert.Empty(bench.Scene.Playing);
        Assert.Null(bench.Scene.HolderOf(bench.Hero));
        Assert.Equal(origin + 2, bench.Hero.Animator.Time, 9);
        var mark = StageNode(player, "Tip");
        Assert.Same(mark, bench.Hero.Node);
        var layer = Assert.Single(player.LayersOf(bench.Hero));
        Assert.Same(layer, bench.Hero.Animator.Stack.Layers[^1]);
        Assert.Equal(new Vector3(4, 0, 0), bench.Pivot.Translation);
        bench.Frame(Step);
        var later = bench.Hero.Animator.Time;
        player.Finish();
        Assert.False(player.Skip());
        Assert.Equal(later, bench.Hero.Animator.Time);
        var stage = player.Stages[0];
        Assert.Contains(stage.Node, bench.Scene.Root.Children);
        var basePose = Local(StageNode(player, "Base"));
        bench.Scene.Rate = 0.5;
        bench.Frame(Step);
        Assert.Equal(later + Step * 0.5, bench.Hero.Animator.Time, 9);
        bench.Pivot.Translation = new(1, 1, 1);
        bench.Frame(Step);
        Assert.Equal(new Vector3(1, 1, 1), bench.Pivot.Translation);
        Assert.Equal(basePose, Local(StageNode(player, "Base")));
        Assert.Same(mark, bench.Hero.Node);
        Assert.Same(layer, bench.Hero.Animator.Stack.Layers[^1]);
    }

    /// <summary>
    /// A skip goes to the end state at once (Q3): every track at its end, the figures' clocks at the end;
    /// the event cues not yet delivered are delivered inside the skip, in order, marked as skipped (Q18),
    /// then <see cref="SequencePlayer.Finished"/> says skipped.
    /// </summary>
    [Fact]
    public async Task ASkipGoesToTheEndStateDeliveringTheCuesLeftMarkedSkipped()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Play();
        var log = new List<string>();
        Log(player, log);
        var origin = bench.Hero.Animator.Time;
        bench.Frame(Step);
        bench.Frame(2 * Step);
        Assert.Equal(["start"], log);
        Assert.True(player.Skip());
        Assert.Equal(["start", "open skipped", "middle skipped", "end skipped"], log);
        Assert.True(player.Ended);
        Assert.True(player.Finished.IsCompleted);
        Assert.True(await player.Finished);
        Assert.Equal((2, 2), (player.Time, player.TimelineTime));
        Assert.Equal(origin + 2, bench.Hero.Animator.Time, 12);
        Assert.Equal(new Vector3(4, 0, 0), bench.Pivot.Translation);
        var machine = bench.Hero.Animator.Machine!;
        Assert.Equal((1, 2, 1.5, 1), (machine.Get("dead"), machine.Get("combo"), machine.Get("swingSpeed"), machine.Get("attack")));
        Assert.Null(bench.Hero.Palette);
        Assert.Same(bench.Tint, bench.Prop.Palette);
        Assert.False(StageNode(player, "Mirror").Hidden);
        Assert.False(player.Skip());
        player.Finish();
        Assert.Equal(4, log.Count);
    }

    /// <summary>
    /// A skip from a cue's handler: the cues left come after the handler, in order, marked skipped, then
    /// <see cref="SequencePlayer.Finished"/>; a handler that throws is logged and the others still come;
    /// <see cref="SequencePlayer.Next"/> completes with the cue, skipped or not.
    /// </summary>
    [Fact]
    public async Task ASkipFromACueHandlerDeliversTheRestAfterIt()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Play();
        var log = new List<string>();
        var open = player.Next("open");
        var start = player.Next("start");
        player.Cue += cue =>
        {
            log.Add($"{cue.Name}{(cue.Skipped ? " skipped" : "")}");
            if (cue.Name == "start")
            {
                Assert.True(player.Skip());
                log.Add("skipped");
            }
            if (cue.Name == "open")
                throw new InvalidOperationException("a handler failing");
        };
        player.Cue += cue => log.Add("also " + cue.Name);
        bench.Frame(Step);
        Assert.Equal(["start", "skipped", "also start", "open skipped", "also open", "middle skipped", "also middle", "end skipped", "also end"], log);
        Assert.True(player.Finished.IsCompleted && start.IsCompleted && open.IsCompleted);
        Assert.True(await player.Finished);
        Assert.Equal(new SequenceCue("start", 0, false), await start);
        Assert.Equal(new SequenceCue("open", 0.4, true), await open);
    }

    /// <summary>A skipped and a watched sequence leave the same scene behind (M10-3): nodes, the stage's nodes, visibility, palettes, parameters, the figures' clocks and marks.</summary>
    [Fact]
    public void ASkippedAndAWatchedSequenceLeaveTheSameSceneBehind()
    {
        using var watched = new SequenceBench(gpu);
        using var skipped = new SequenceBench(gpu);
        var a = watched.Play();
        var b = skipped.Play();
        watched.Frame(Step);
        skipped.Frame(Step);
        for (var i = 0; i < 16; i++)
            watched.Frame(Step);
        skipped.Frame(Step);
        Assert.True(b.Skip());
        Assert.True(a.Ended);
        static string State(SequenceBench bench, SequencePlayer player)
        {
            var machine = bench.Hero.Animator.Machine!;
            var stage = string.Join("; ", player.Stages[0].Node!.Descendants().Select(n => $"{n.Name} {Local(n)} {n.Hidden}"));
            return $"{Local(bench.Pivot)} | {stage} | {bench.Hero.Palette == null} {bench.Prop.Palette == bench.Tint} {bench.Hero.Node!.Name} {player.LayersOf(bench.Hero).Count} | "
                + $"{machine.Get("dead")} {machine.Get("combo")} {machine.Get("swingSpeed")} | {bench.Hero.Animator.Time:R}";
        }
        Assert.Equal(State(watched, a), State(skipped, b));
    }

    /// <summary>A skip applies every track at its end, also one whose cue in effect is already the last (S4-8): what the game changed since is set again.</summary>
    [Fact]
    public void ASkipAppliesEveryTrackAtItsEnd()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Play();
        bench.Frame(Step);
        bench.FrameUntil(player, 1);
        var mirror = StageNode(player, "Mirror");
        Assert.False(mirror.Hidden);
        mirror.Hidden = true;
        bench.Frame(Step);
        Assert.True(mirror.Hidden);
        player.Skip();
        Assert.False(mirror.Hidden);
    }

    /// <summary>Each cue is delivered as game code: microtasks it queues run right after its handler, in a frame and at a skip.</summary>
    [Fact]
    public void CueHandlersRunAsGameCode()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Play();
        var log = new List<string>();
        player.Cue += cue =>
        {
            log.Add(cue.Name);
            bench.Document.EventLoop.QueueMicrotask(() => log.Add("after " + cue.Name));
        };
        bench.Frame(Step);
        bench.FrameUntil(player, 0.5);
        Assert.Equal(["start", "after start", "open", "after open"], log);
        player.Skip();
        Assert.Equal(["start", "after start", "open", "after open", "middle", "after middle", "end", "after end"], log);
    }

    /// <summary>
    /// Game code that runs in the end's frame between its timing update and its events step (a resize
    /// listener) sees the end coming: a skip refuses (it has ended), a seek back takes it back from the end.
    /// </summary>
    [Fact]
    public void InTheEndsFrameASkipRefusesAndASeekTakesItBack()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Play();
        bench.Frame(Step);
        bench.FrameUntil(player, 1.875);
        var skips = new List<bool>();
        bench.Document.AddEventListener("resize", _ =>
        {
            skips.Add(player.Skip());
            player.Finish();
            player.Seek(1);
        });
        bench.Document.SetViewport(320, 200);
        bench.Frame(Step);
        Assert.Equal([false], skips);
        Assert.False(player.Ended);
        Assert.Equal(1, player.Time);
        bench.Frame(Step);
        Assert.Equal(1.125, player.Time);
    }

    /// <summary>
    /// The cut list maps the played clock onto the timeline (Q19): a hold first (τ stays), a span played at
    /// its own length, a span ending at a hold, and the duration at the end; the starts of later spans count
    /// the holds before them.
    /// </summary>
    [Fact]
    public void TheCutListMapsThePlayedClockOntoTheTimeline()
    {
        using var bench = new SequenceBench(gpu, reducedMotion: true);
        var sequence = new SequenceBuilder("cuts", 2).Role("pivot", RoleKind.Node)
            .Transform("pivot", t => t.Location(x: new FCurve([FCurveKey.Linear(0, 0), FCurveKey.Linear(2, 2)])))
            .Reduced(r => r.Hold(0.25, 0.5).Span(1, 1.5).Span(1.5, 1.5, 0.25))
            .Build();
        var player = bench.Scene.Play(sequence, new SequenceBindings().Node("pivot", bench.Pivot), bench.Document);
        Assert.Equal(1.25, player.Duration);
        var seen = new List<(double, double, float)>();
        bench.Frame(Step);
        for (var i = 0; i <= 10; i++)
        {
            seen.Add((player.Time, player.TimelineTime, bench.Pivot.Translation.X));
            if (player.Ended)
                break;
            bench.Frame(Step);
        }
        Assert.Equal([(0, 0.25, 0.25f), (0.125, 0.25, 0.25f), (0.25, 0.25, 0.25f), (0.375, 0.25, 0.25f), (0.5, 0.25, 0.25f),
            (0.625, 1.125, 1.125f), (0.75, 1.25, 1.25f), (0.875, 1.375, 1.375f), (1, 1.5, 1.5f), (1.125, 1.5, 1.5f), (1.25, 2, 2f)], seen);
    }

    /// <summary>A figure that is in no scene may be bound when the sequence puts it on a mark of its stage.</summary>
    [Fact]
    public void AFigureOutsideTheSceneStandsOnItsMark()
    {
        using var bench = new SequenceBench(gpu);
        bench.HeroNode.Detach(bench.Hero);
        var player = bench.Play();
        Assert.Same(StageNode(player, "Tip"), bench.Hero.Node);
        Assert.Same(bench.Scene, bench.Hero.Node!.Scene);
    }

    /// <summary>An unskippable sequence (Q20): <see cref="SequencePlayer.Skip"/> refuses and changes nothing; <see cref="SequencePlayer.Finish"/> ends it as a skip does.</summary>
    [Fact]
    public async Task AnUnskippableSequenceRefusesASkipButFinishEndsIt()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Play(TestSequence.Build(bench.StageData) with { Skippable = false, Blocking = false });
        Assert.Equal((false, false), (player.Skippable, player.Blocking));
        var log = new List<string>();
        Log(player, log);
        bench.Frame(Step);
        bench.Frame(Step);
        Assert.False(player.Skip());
        Assert.Equal((Step, false), (player.Time, player.Ended));
        Assert.Equal(["start"], log);
        player.Finish();
        Assert.True(player.Ended);
        Assert.True(player.Finished.IsCompleted);
        Assert.True(await player.Finished);
        Assert.Equal(["start", "open skipped", "middle skipped", "end skipped"], log);
        Assert.Equal(new Vector3(4, 0, 0), bench.Pivot.Translation);
    }

    /// <summary>
    /// The reduced variant (M10-4, Q19), chosen from the document's media environment when it starts: the
    /// cut list 0–0.5 s, then 1.5 s held for 1 s, so 1.5 s long; τ follows the spans and is the duration at
    /// the end. Moments in the parts cut out come marked skipped (S4-9: after the last span too), triggers
    /// there are set, the figures' clocks jump across the cuts (firing no clip events there) and stand
    /// still in the hold; it ends as played, not skipped.
    /// </summary>
    [Fact]
    public async Task TheReducedVariantPlaysTheCutList()
    {
        using var bench = new SequenceBench(gpu, reducedMotion: true);
        bench.Frame(0.5);
        var player = bench.Play();
        Assert.Equal((true, 1.5), (player.Reduced, player.Duration));
        var log = new List<string>();
        Log(player, log);
        bench.Hero.Animator.Event += e => log.Add("clip " + e.Name);
        var origin = bench.Hero.Animator.Time;
        var taus = new List<double>();
        var heroes = new List<double>();
        bench.Frame(Step);
        for (var i = 0; i < 12; i++)
        {
            taus.Add(player.TimelineTime);
            heroes.Add(bench.Hero.Animator.Time - origin);
            bench.Frame(Step);
        }
        Assert.Equal([0, 0.125, 0.25, 0.375, 0.5, 1.5, 1.5, 1.5, 1.5, 1.5, 1.5, 1.5], taus);
        Assert.Equal([0, 0.125, 0.25, 0.375, 0.5, 1.5, 1.5, 1.5, 1.5, 1.5, 1.5, 1.5], heroes.Select(h => Math.Round(h, 9)));
        Assert.True(player.Ended);
        Assert.Equal(2, player.TimelineTime);
        Assert.Equal(origin + 2, bench.Hero.Animator.Time, 9);
        Assert.True(player.Finished.IsCompleted);
        Assert.False(await player.Finished);
        // The figure's own idle clip has its events at 1 s on its clock, which τ 0.5 reaches (its origin is 0.5).
        Assert.Equal(["start", "clip whoosh", "clip blink", "clip breath", "open", "middle skipped", "end skipped"], log);
        // The trigger cut out was set; the machine takes it when the figure next moves, on the scene's clock.
        var machine = bench.Hero.Animator.Machine!;
        Assert.Equal((1, "none", 2), (machine.Get("attack"), machine.StateOf("action"), machine.Get("combo")));
        bench.Frame(Step);
        Assert.Equal("swing", machine.StateOf("action"));
    }

    /// <summary>Without reduced motion, without a reduced variant, or without a document, the whole sequence plays.</summary>
    [Fact]
    public void TheFullVariantPlaysUnlessReducedMotionAsksForTheCutList()
    {
        using var reduced = new SequenceBench(gpu, reducedMotion: true);
        using var full = new SequenceBench(gpu);
        Assert.False(full.Play().Reduced);
        var none = reduced.Play(TestSequence.Build(reduced.StageData) with { Reduced = null });
        Assert.False(none.Reduced);
        none.Skip();
        var player = reduced.Scene.Play(TestSequence.Build(reduced.StageData), reduced.Bindings(), null);
        Assert.Equal((false, 2), (player.Reduced, player.Duration));
    }

    /// <summary>
    /// The order within a frame (Q17): the state tracks are applied in the frame's animation timing update
    /// (a frame callback and a cue handler see them); the figures' clip events come first, then the
    /// sequence's moments in time order, those within 10⁻⁶ s of each other in record order (track, then
    /// cue), each delivered as game code; <see cref="SequencePlayer.Next"/> completes with its cue.
    /// </summary>
    [Fact]
    public async Task CuesComeInTimeOrderThenRecordOrderAfterTheFiguresClipEvents()
    {
        using var bench = new SequenceBench(gpu);
        var sequence = new SequenceBuilder("order", 1).Role("hero", RoleKind.Actor).Role("pivot", RoleKind.Node)
            .Animation("hero", null, a => a.Strip("swing"))
            .Events(e => e.Event(0.5 + 5e-7, "late"))
            .Events(e => e.Event(0.5, "early").Event(0.25, "first"))
            .Transform("pivot", t => t.Location(x: new FCurve([FCurveKey.Linear(0, 0), FCurveKey.Linear(1, 1)])))
            .Build();
        var player = bench.Scene.Play(sequence, new SequenceBindings().Actor("hero", bench.Hero).Node("pivot", bench.Pivot), bench.Document);
        var log = new List<string>();
        player.Cue += cue => log.Add($"{cue.Name} {bench.Pivot.Translation.X:R}");
        bench.Hero.Animator.Event += e => log.Add("clip " + e.Name);
        var next = player.Next("early");
        bench.Frame(Step);
        bench.Document.RequestAnimationFrame(_ => log.Add($"frame {bench.Pivot.Translation.X:R}"));
        bench.Frame(0.5);
        Assert.Equal(["clip whoosh", "clip hit", "first 0.5", "late 0.5", "early 0.5", "frame 0.5"], log);
        Assert.True(next.IsCompleted);
        var cue = await next;
        Assert.Equal(new SequenceCue("early", 0.5, false), cue);
    }

    /// <summary>
    /// A cue at a marker follows it (Q22, S4-2): the same record played with a stage whose marker was moved
    /// (the stage retimed and exported again) delivers the cue at the marker's new time.
    /// </summary>
    [Fact]
    public void ACueAtAMarkerFollowsTheStage()
    {
        using var bench = new SequenceBench(gpu);
        var moved = TestSequence.WithMarkers(bench.StageData, "base", new("open", 0.9), new("reveal", 0.75));
        Assert.Equal(RecordSerializer.ToJson(TestSequence.Build(bench.StageData)), RecordSerializer.ToJson(TestSequence.Build(moved)));
        foreach (var (stage, expected) in new[] { (bench.StageData, 0.4), (moved, 0.9) })
        {
            using var run = new SequenceBench(gpu);
            var player = run.Scene.Play(TestSequence.Build(stage), run.Bindings(stage), run.Document);
            var times = new List<(string, double, double)>();
            player.Cue += cue => times.Add((cue.Name, cue.Time, player.TimelineTime));
            run.Frame(Step);
            for (var i = 0; i < 8; i++)
                run.Frame(Step);
            var open = times.Single(t => t.Item1 == "open");
            Assert.Equal(expected, open.Item2, 12);
            Assert.InRange(open.Item3, expected, expected + Step);
        }
    }

    /// <summary>Any number of sequences play at once and none stops another (Q20).</summary>
    [Fact]
    public void SequencesPlayTogether()
    {
        using var bench = new SequenceBench(gpu);
        var first = bench.Play();
        var other = bench.Scene.Root.Add("other");
        var second = bench.Scene.Play(new SequenceBuilder("second", 1).Role("pivot", RoleKind.Node)
            .Transform("pivot", t => t.Location(y: new FCurve([FCurveKey.Linear(0, 0), FCurveKey.Linear(1, 3)])))
            .Build(), new SequenceBindings().Node("pivot", other), bench.Document);
        Assert.Equal([first, second], bench.Scene.Playing);
        bench.Frame(Step);
        bench.Frame(4 * Step);
        Assert.Equal((0.5, 0.5), (first.Time, second.Time));
        Assert.Equal(1.5f, other.Translation.Y, 5);
        bench.Frame(4 * Step);
        Assert.True(second.Ended);
        Assert.False(first.Ended);
        Assert.Equal([first], bench.Scene.Playing);
    }

    /// <summary>A sequence with UI tracks plays at rate 1 only (Q29).</summary>
    [Fact]
    public void ASequenceWithUiTracksPlaysAtRateOneOnly()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Scene.Play(new SequenceBuilder("card", 1).Role("card", RoleKind.Element).Ui("card", u => u.AddClass(0, "on")).Build(),
            new SequenceBindings().Element("card", bench.Document.GetElementById("card")!), bench.Document);
        Assert.Throws<InvalidOperationException>(() => player.Rate = 2);
        player.Rate = 1;
        Assert.Equal(1, player.Rate);
    }

    // ---------- Bindings (S4-6) ----------

    static Sequence TwoActors(ModelData stage) => new SequenceBuilder("two", 1).Role("hero", RoleKind.Actor).Role("foe", RoleKind.Actor).Stage(TestSequence.StagePath, stage)
        .Animation("foe", "hierarchy/Tip", a => a.Strip("swing")).Build();

    static readonly Dictionary<string, (Func<SequenceBench, (Sequence, SequenceBindings)> Make, string Problem)> BadBindings = new()
    {
        ["unbound"] = (b => (TestSequence.Build(b.StageData), new SequenceBindings().Actor("hero", b.Hero).Node("pivot", b.Pivot).Palette("tint", b.Tint).Stage(TestSequence.StagePath, b.StageData)),
            "the sequence test: the role prop (model) isn't bound"),
        ["twice"] = (b => (TestSequence.Build(b.StageData), b.Bindings().Node("pivot", b.Pivot)), "the sequence test: the role pivot is bound twice"),
        ["kind"] = (b => (TestSequence.Build(b.StageData), new SequenceBindings().Node("hero", b.HeroNode).Stage(TestSequence.StagePath, b.StageData)),
            "the sequence test: the role hero is an actor role, bound to a node"),
        ["kind palette"] = (b => (TestSequence.Build(b.StageData), new SequenceBindings().Actor("tint", b.Hero).Stage(TestSequence.StagePath, b.StageData)),
            "the role tint is a palette role, bound to an actor"),
        ["kind model"] = (b => (TestSequence.Build(b.StageData), new SequenceBindings().Actor("hero", b.Hero).Node("pivot", b.Pivot).Node("prop", b.Pivot).Palette("tint", b.Tint).Stage(TestSequence.StagePath, b.StageData)),
            "the sequence test: the role prop is a model role, bound to a node"),
        ["no such role"] = (b => (TestSequence.Build(b.StageData), b.Bindings().Node("extra", b.Pivot)), "the sequence test: extra is bound, but it has no such role"),
        ["stage missing"] = (b => (TestSequence.Build(b.StageData), new SequenceBindings().Actor("hero", b.Hero)), "the sequence test: its stage models/stages/hierarchy isn't given"),
        ["stage extra"] = (b => (TestSequence.Build(b.StageData), b.Bindings().Stage("models/stages/arena", b.StageData)), "the sequence test: a stage models/stages/arena is given, which it doesn't bring"),
        ["stage twice"] = (b => (TestSequence.Build(b.StageData), b.Bindings().Stage(TestSequence.StagePath, b.StageData)), "the sequence test: its stage models/stages/hierarchy is given twice"),
        ["stage nodes"] = (b => (TestSequence.Build(b.StageData), b.Bindings(ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, "stage.glb")))),
            "the sequence test, track 0 (clip): the stage models/stages/hierarchy has no clip base"),
        ["one actor two roles"] = (b => (TwoActors(b.StageData), new SequenceBindings().Actor("hero", b.Hero).Actor("foe", b.Hero).Stage(TestSequence.StagePath, b.StageData)),
            "the sequence two: the actor bound to foe is also bound to hero"),
        ["not in the scene"] = (b =>
        {
            b.HeroNode.Detach(b.Hero);
            return (TwoActors(b.StageData), new SequenceBindings().Actor("hero", b.Hero).Actor("foe", b.Bystander).Stage(TestSequence.StagePath, b.StageData));
        }, "the sequence two: the actor bound to hero isn't in the scene and stands on no mark of the sequence"),
        ["clip"] = (b => (new SequenceBuilder("clip", 1).Role("hero", RoleKind.Actor).Animation("hero", null, a => a.Strip("fly")).Build(), new SequenceBindings().Actor("hero", b.Hero)),
            "the sequence clip, track 0 (animation): the actor hero has no clip fly"),
        ["clip skeleton"] = (b => (new SequenceBuilder("clip", 1).Role("hero", RoleKind.Actor).Animation("hero", null, a => a.Strip("swing")).Build(),
            new SequenceBindings().Actor("hero", b.Hero, name => new ClipBinding(AnimationClip.AllOf(b.Figure.Data).Single(c => c.Name == name), new Skeleton(b.Figure.Data)))),
            "the actor hero's clip swing is bound to another skeleton than its stack's"),
        ["overlap"] = (b => (new SequenceBuilder("strips", 2).Role("hero", RoleKind.Actor).Animation("hero", null, a => a.Strip("swing").Strip("idle", start: 0.5)).Build(),
            new SequenceBindings().Actor("hero", b.Hero)),
            "track 0 (animation): strip 1 (idle, from 0.5 s) starts before strip 0 (swing) ends at 0.6000000238418579 s: the strips of one track are one layer, in time order"),
        ["order"] = (b => (new SequenceBuilder("strips", 2).Role("hero", RoleKind.Actor).Animation("hero", null, a => a.Strip("swing", start: 1).Strip("idle")).Build(),
            new SequenceBindings().Actor("hero", b.Hero)), "strip 1 (idle, from 0 s) starts before strip 0 (swing) ends"),
        ["parameter"] = (b => (new SequenceBuilder("cues", 1).Role("hero", RoleKind.Actor).Animation("hero", null, a => a.Float(0, "pace", 1)).Build(), new SequenceBindings().Actor("hero", b.Hero)),
            "the sequence cues, track 0 (animation): the actor hero's state machine humanoid has no parameter pace"),
        ["parameter kind"] = (b => (new SequenceBuilder("cues", 1).Role("hero", RoleKind.Actor).Animation("hero", null, a => a.Float(0, "dead", 1)).Build(), new SequenceBindings().Actor("hero", b.Hero)),
            "the actor hero's parameter dead is a bool parameter, not a float"),
    };

    public static TheoryData<string> BadBindingCases => [.. BadBindings.Keys];

    /// <summary>
    /// <see cref="SceneView.Play"/> checks everything before it changes anything (S4-6): a failure is an
    /// <see cref="ArgumentException"/> naming the problem, and the scene, the figures, their stacks and
    /// nodes are as they were; a valid play afterwards works.
    /// </summary>
    [Theory]
    [MemberData(nameof(BadBindingCases))]
    public void PlayChecksEverythingFirstAndChangesNothing(string label)
    {
        using var bench = new SequenceBench(gpu);
        var (make, problem) = BadBindings[label];
        var (sequence, bindings) = make(bench);
        object Snapshot() => (bench.Scene.Root.Children.Count, bench.Hero.Node, bench.Bystander.Node, bench.Hero.Animator.Stack.Layers.Count,
            bench.Bystander.Animator.Stack.Layers.Count, bench.Scene.Playing.Count, bench.Scene.HolderOf(bench.Hero), bench.Scene.HolderOf(bench.Bystander), Local(bench.Pivot));
        var before = Snapshot();
        var thrown = Assert.Throws<ArgumentException>(() => bench.Scene.Play(sequence, bindings, bench.Document));
        Assert.Contains(problem, thrown.Message);
        Assert.Equal(before, Snapshot());
        if (bench.Hero.Node == null)
            bench.HeroNode.Attach(bench.Hero);
        bench.Play();
    }

    [Fact]
    public void AFigureHeldByAPlayingSequenceCantBeBoundAgain()
    {
        using var bench = new SequenceBench(gpu);
        var first = bench.Play();
        var problem = Assert.Throws<ArgumentException>(() => bench.Play());
        Assert.Equal("the sequence test: the actor bound to hero is held by the sequence test, which is playing", problem.Message);
        Assert.Equal([first], bench.Scene.Playing);
        first.Skip();
        bench.Play();
    }

    [Fact]
    public void AFigureWithoutAMachineCantTakeParameterCues()
    {
        using var bench = new SequenceBench(gpu, machine: false);
        var problem = Assert.Throws<ArgumentException>(() => bench.Play());
        Assert.Contains("track 5 (animation): the actor hero has no state machine for its parameter cues", problem.Message);
    }
}
