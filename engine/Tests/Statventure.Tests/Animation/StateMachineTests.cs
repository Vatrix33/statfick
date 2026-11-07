using Statventure.Animation;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Render3D;
using static Statventure.Animation.ConditionDefinition;

namespace Statventure.Tests.Animation;

/// <summary>
/// State machines (Docs/Animation.md, "State machines in detail"): the user's rules for layers of
/// states, conditions, exit times, triggers, priority, restarts, speeds, interrupted fades and
/// notices. Blender has no state machines; the cross-fade's mixing rule is checked against
/// Blender's transition strips (nla-transition scenes), the rest here.
/// </summary>
public class StateMachineTests
{
    static readonly ModelData Figure = ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, "figure.glb"));
    static readonly AnimationClip[] Clips = AnimationClip.AllOf(Figure);
    const double Frame = 1 / 60.0;

    /// <summary>A humanoid-like machine on the figure's clips: idle (2 s loop), swing (0.6 s), pulse (0.67 s), twist (1 s).</summary>
    internal static StateMachineDefinition Humanoid() => new StateMachineBuilder("humanoid")
        .Trigger("attack").Trigger("hurt").Bool("dead").Float("swingSpeed", 1).Int("combo")
        .Layer("base", BlendMode.Replace, "idle", l => l
            .State("idle", "idle", loop: true)
            .State("down", "twist")
            .FromAny("down", 0.2, false, Is("dead")))
        .Layer("action", BlendMode.Replace, "none", l => l
            .Empty("none")
            .State("swing", "swing", speedParameter: "swingSpeed")
            .State("hurt", "pulse")
            .State("combo", "swing")
            .Transition("none", "swing", 0.1, Trigger("attack"))
            .Transition("swing", "combo", 0.1, Trigger("attack"), Of("combo", Compare.GreaterOrEqual, 2))
            .TransitionAfter("swing", "none", 1, 0.15)
            .TransitionAfter("combo", "none", 1, 0.15)
            .TransitionAfter("hurt", "none", 1, 0.1)
            .FromAny("hurt", 0.05, true, Trigger("hurt")))
        .Build();

    sealed class Rig
    {
        public Rig(StateMachineDefinition? definition = null)
        {
            var skeleton = new Skeleton(Figure);
            Stack = new AnimationStack(skeleton);
            Machine = new StateMachine(definition ?? Humanoid(), Stack, name => new ClipBinding(Clips.Single(c => c.Name == name), skeleton));
            Animator = new Animator(Stack, Machine);
            Animator.StateChanged += n => Notices.Add(n);
            Animator.Event += e => Events.Add(e);
        }

        public AnimationStack Stack { get; }
        public StateMachine Machine { get; }
        public Animator Animator { get; }
        public List<StateNotice> Notices { get; } = [];
        public List<ClipEvent> Events { get; } = [];

        /// <summary>Plays <paramref name="frames"/> 60 Hz frames.</summary>
        public void Play(int frames)
        {
            for (var i = 0; i < frames; i++)
            {
                Animator.Advance(Frame);
                Animator.DispatchEvents();
            }
        }

        public string Log => string.Join(", ", Notices.Select(n => $"{(n.Entered ? "+" : "-")}{n.State}@{Math.Round(n.Time * 60, 2)}"));
    }

    [Fact]
    public void DefinitionsThatDontResolveAreRefused()
    {
        StateMachineBuilder Base() => new StateMachineBuilder("m").Trigger("go").Bool("on").Float("f");
        Assert.Throws<ArgumentException>(() => Base().Layer("l", BlendMode.Replace, "missing", l => l.State("a", "idle")).Build());
        Assert.Throws<ArgumentException>(() => Base().Layer("l", BlendMode.Replace, "a", l => l.State("a", "idle").State("a", "swing")).Build());
        Assert.Throws<ArgumentException>(() => Base().Layer("l", BlendMode.Replace, "a", l => l.State("a", "idle").Transition("a", "b", 0, Trigger("go"))).Build());
        Assert.Throws<ArgumentException>(() => Base().Layer("l", BlendMode.Replace, "a", l => l.State("a", "idle").State("b", "swing").Transition("a", "b", 0, Of("go", Compare.Less, 1))).Build());
        Assert.Throws<ArgumentException>(() => Base().Layer("l", BlendMode.Replace, "a", l => l.State("a", "idle").State("b", "swing").Transition("a", "b", 0, Of("on", Compare.Greater, 0))).Build());
        Assert.Throws<ArgumentException>(() => Base().Layer("l", BlendMode.Replace, "a", l => l.State("a", "idle").State("b", "swing").Transition("a", "b", 0)).Build());
        Assert.Throws<ArgumentException>(() => Base().Layer("l", BlendMode.Replace, "a", l => l.Empty("a").State("b", "swing").TransitionAfter("a", "b", 1, 0)).Build());
        Assert.Throws<ArgumentException>(() => Base().Layer("l", BlendMode.Replace, "a", l => l.State("a", "idle", speedParameter: "on")).Build());
        Assert.Throws<ArgumentException>(() => Base().Layer("l", BlendMode.Replace, "a", l => l.State("a", "idle").Transition("a", "a", 0, Trigger("nope"))).Build());
        new Rig().Machine.SetFloat("swingSpeed", 2);
        Assert.Throws<ArgumentException>(() => new Rig().Machine.SetBool("swingSpeed", true));
    }

    static readonly ParameterDefinition[] Parameters = [new("go", ParameterKind.Trigger), new("on", ParameterKind.Bool), new("f", ParameterKind.Float)];

    /// <summary>A machine "m" with the parameters go (trigger), on (bool) and f (float) and one layer "l" entering "a", with the states and transitions given.</summary>
    static StateMachineDefinition Machine(StateDefinition[] states, params TransitionDefinition[] transitions) =>
        new("m", Parameters, [new LayerDefinition("l", BlendMode.Replace, "a", states, transitions)]);

    static readonly StateDefinition[] AB = [new("a", "idle"), new("b", "swing")];

    static readonly Dictionary<string, (Func<StateMachineDefinition> Machine, string Problem, string Path)> Broken = new()
    {
        ["parameter twice"] = (() => new("m", [.. Parameters, new("go", ParameterKind.Bool)], Machine(AB).Layers), "m: the parameter go is defined twice", "$.parameters[3].name"),
        ["no layers"] = (() => new("m", Parameters, []), "m: no layers", "$.layers"),
        ["state twice"] = (() => Machine([new("a", "idle"), new("a", "swing")]), "m/l: the state a is defined twice", "$.layers[0].states[1].name"),
        ["speed parameter"] = (() => Machine([new("a", "idle", SpeedParameter: "on")]), "m/l/a: the speed parameter on isn't a float parameter", "$.layers[0].states[0].speedParameter"),
        ["no speed parameter"] = (() => Machine([new("a", "idle", SpeedParameter: "nope")]), "m/l/a: the speed parameter nope isn't a float parameter", "$.layers[0].states[0].speedParameter"),
        ["no entry"] = (() => Machine([new("b", "idle")]), "m/l: no entry state a", "$.layers[0].entry"),
        ["no target"] = (() => Machine([new("a", "idle")], new TransitionDefinition("a", "b", [ConditionDefinition.Trigger("go")])),
            "m/l: a transition a → b names a state that doesn't exist", "$.layers[0].transitions[0].to"),
        ["no source"] = (() => Machine(AB, new TransitionDefinition("x", "b", [ConditionDefinition.Trigger("go")])),
            "m/l: a transition x → b names a state that doesn't exist", "$.layers[0].transitions[0].from"),
        ["negative duration"] = (() => Machine(AB, new TransitionDefinition("a", "b", [ConditionDefinition.Trigger("go")], Duration: -1)),
            "m/l: a transition to b has a negative duration or exit time", "$.layers[0].transitions[0].duration"),
        ["negative exit time"] = (() => Machine(AB, new TransitionDefinition("a", "b", [], ExitTime: -1)),
            "m/l: a transition to b has a negative duration or exit time", "$.layers[0].transitions[0].exitTime"),
        ["not a parameter"] = (() => Machine(AB, new TransitionDefinition("a", "b", [ConditionDefinition.Trigger("nope")])),
            "m/l: a transition to b tests nope, which isn't a parameter", "$.layers[0].transitions[0].conditions[0].parameter"),
        ["trigger compared"] = (() => Machine(AB, new TransitionDefinition("a", "b", [ConditionDefinition.Of("go", Compare.Less, 1)])),
            "m/l: a trigger condition (go) can only test that it's set", "$.layers[0].transitions[0].conditions[0].compare"),
        ["trigger value"] = (() => Machine(AB, new TransitionDefinition("a", "b", [ConditionDefinition.Of("go", Compare.Equal, 0)])),
            "m/l: a trigger condition (go) can only test that it's set", "$.layers[0].transitions[0].conditions[0].value"),
        ["bool compared"] = (() => Machine(AB, new TransitionDefinition("a", "b", [ConditionDefinition.Of("on", Compare.Greater, 0)])),
            "m/l: a bool condition (on) can only test equality", "$.layers[0].transitions[0].conditions[0].compare"),
        ["neither"] = (() => Machine(AB, new TransitionDefinition("a", "b", [])),
            "m/l: a transition to b has neither conditions nor an exit time", "$.layers[0].transitions[0]"),
        ["exit time without a clip"] = (() => Machine([new("a", null), new("b", "swing")], new TransitionDefinition("a", "b", [], ExitTime: 1)),
            "m/l: a transition to b has an exit time but no clip to count it in", "$.layers[0].transitions[0].exitTime"),
        ["exit time from any"] = (() => Machine(AB, new TransitionDefinition(null, "b", [], ExitTime: 1)),
            "m/l: a transition to b has an exit time but no clip to count it in", "$.layers[0].transitions[0].exitTime"),
    };

    public static TheoryData<string> BrokenNames => [.. Broken.Keys];

    /// <summary>Each rule broken alone: <see cref="StateMachineDefinition.Validate"/>'s problem, which the machine's check (M11 step 6, P20) reports first at the path of what it names.</summary>
    [Theory]
    [MemberData(nameof(BrokenNames))]
    public void EachRuleIsNamedAtItsPath(string name)
    {
        var (machine, problem, path) = Broken[name];
        Assert.Equal(problem, Assert.Throws<ArgumentException>(machine().Validate).Message);
        var first = RecordChecks.Check(machine())[0];
        Assert.Equal((path, problem), (first.Path.ToString(), first.Message));
    }

    /// <summary>The check reports every rule a machine breaks at its path, and doesn't stumble on what an earlier rule found broken (a state defined twice that a transition with an exit time comes from).</summary>
    [Fact]
    public void TheCheckReportsEveryBrokenRule()
    {
        var machine = Machine([new("a", "idle"), new("a", null), new("b", "swing", SpeedParameter: "go")],
            new TransitionDefinition("a", "c", [ConditionDefinition.Of("go", Compare.Less, 0), ConditionDefinition.Of("on", Compare.Greater, 1)], ExitTime: 1));
        Assert.Equal(["$.layers[0].states[1].name", "$.layers[0].states[2].speedParameter", "$.layers[0].transitions[0].to",
                "$.layers[0].transitions[0].conditions[0].compare", "$.layers[0].transitions[0].conditions[0].value", "$.layers[0].transitions[0].conditions[1].compare"],
            RecordChecks.Check(machine).Select(p => p.Path.ToString()));
        Machine(AB, new TransitionDefinition("a", "b", [], ExitTime: 1), new TransitionDefinition(null, "a", [ConditionDefinition.Is("on")], MayRestart: true)).Validate();
        // An unknown parameter isn't tested for its kind, and an unknown source isn't looked up for its clip.
        Assert.Equal(["$.layers[0].transitions[0].conditions[0].parameter", "$.layers[0].transitions[1].from"],
            RecordChecks.Check(Machine(AB, new TransitionDefinition("a", "b", [ConditionDefinition.Of("nope", Compare.Greater, 2)]),
                new TransitionDefinition("x", "b", [], ExitTime: 1))).Select(p => p.Path.ToString()));
    }

    [Fact]
    public void LayersStartInTheirEntryStatesAndAnEmptyStateShowsTheLayersBelow()
    {
        var rig = new Rig();
        Assert.Equal(("idle", "none"), (rig.Machine.StateOf("base"), rig.Machine.StateOf("action")));
        rig.Play(30);
        var idleAlone = new AnimationStack(rig.Stack.Skeleton);
        idleAlone.Add(new ClipBinding(Clips.Single(c => c.Name == "idle"), rig.Stack.Skeleton), 0, s => s.Repeat = double.PositiveInfinity);
        Same(idleAlone, rig.Stack, rig.Animator.Time);
        Assert.Empty(rig.Notices);
    }

    [Fact]
    public void TriggersStartTransitionsAtTheUpdateAndExitTimesAtTheirMoment()
    {
        var rig = new Rig();
        rig.Play(10);
        rig.Machine.SetTrigger("attack");
        rig.Play(1);
        // Seen at frame 11's update: the swing enters then and fades in over 0.1 s (6 frames).
        Assert.Equal("swing", rig.Machine.StateOf("action"));
        Assert.Equal(0, rig.Machine.Get("attack"));
        rig.Play(80);
        // Its clip (0.6 s = 36 frames) ends at frame 47 exactly: the exit transition to none starts
        // there and fades for 0.15 s (9 frames), so the swing is left at 56.
        Assert.Equal("+swing@11, -none@17, +none@47, -swing@56", rig.Log);
        // The swing's markers fired while it acted: 8 and 19 frames into it.
        Assert.Equal([("whoosh", 19.0), ("hit", 30.0)], rig.Events.Where(e => e.Strip.Clip.Clip.Name == "swing").Select(e => (e.Name, Math.Round(e.Time * 60, 6))));
    }

    [Fact]
    public void ATriggerNoTransitionCanUseIsKeptUntilOneDoes()
    {
        var rig = new Rig();
        rig.Machine.SetTrigger("attack");
        rig.Play(1);
        rig.Play(5);
        // Attacking again during the swing: no swing → swing transition (the combo wants combo ≥ 2),
        // so the trigger waits; the exit to none comes at frame 37, and none → swing takes it in the
        // same update, at that moment (it falls on the update's time).
        rig.Machine.SetTrigger("attack");
        rig.Play(40);
        Assert.Equal(0, rig.Machine.Get("attack"));
        Assert.StartsWith("+swing@1, -none@7, +none@37, +swing@37", rig.Log);
    }

    [Fact]
    public void AnyStateTransitionsComeFirstAndRestartOnlyWhenTheyMay()
    {
        var rig = new Rig();
        // Both due at the same update: hurt (any state) wins; the attack trigger stays set.
        rig.Machine.SetTrigger("attack");
        rig.Machine.SetTrigger("hurt");
        rig.Play(1);
        Assert.Equal("hurt", rig.Machine.StateOf("action"));
        Assert.Equal(1, rig.Machine.Get("attack"));
        // Hurt again during hurt: it may restart, so a new hurt fades in over the current one.
        rig.Play(10);
        rig.Machine.SetTrigger("hurt");
        rig.Play(1);
        Assert.Equal(2, rig.Notices.Count(n => n.Entered && n.State == "hurt"));
        // dead (a held bool, may not restart): death enters once and stays.
        rig.Machine.SetBool("dead", true);
        rig.Play(120);
        Assert.Equal(1, rig.Notices.Count(n => n.Entered && n.State == "down"));
        Assert.Equal("down", rig.Machine.StateOf("base"));
    }

    [Fact]
    public void ConditionsMustAllHold()
    {
        var rig = new Rig();
        rig.Machine.SetTrigger("attack");
        rig.Play(3);
        rig.Machine.SetTrigger("attack");
        rig.Machine.SetInt("combo", 1);
        rig.Play(1);
        Assert.Equal("swing", rig.Machine.StateOf("action"));
        rig.Machine.SetInt("combo", 2);
        rig.Play(1);
        Assert.Equal("combo", rig.Machine.StateOf("action"));
    }

    [Fact]
    public void ASpeedParameterRetimesTheStateKeepingItsPlace()
    {
        var rig = new Rig();
        rig.Machine.SetFloat("swingSpeed", 2);
        rig.Machine.SetTrigger("attack");
        rig.Play(1);
        // At double speed the 36-frame clip lasts 18 frames: the exit is at frame 19.
        rig.Play(30);
        Assert.Contains("+none@19", rig.Log);
        // At 1.25 times the clip lasts 28.8 frames: the exit falls inside frame 30's update, exactly
        // at 29.8, and the swing is left when none's 9-frame fade ends, at 38.8.
        rig = new Rig();
        rig.Machine.SetFloat("swingSpeed", 1.25);
        rig.Machine.SetTrigger("attack");
        rig.Play(50);
        Assert.Equal("+swing@1, -none@7, +none@29.8, -swing@38.8", rig.Log);

        rig = new Rig();
        rig.Machine.SetTrigger("attack");
        rig.Play(13);
        var strip = rig.Machine.FadesOf("action").Entries.Last().Strip!;
        var before = strip.ClipTimeAt(rig.Animator.Time);
        // Halving the speed after 12 frames into the swing: the clip goes on from where it was, and
        // the remaining 24 frames of it take 48.
        rig.Machine.SetFloat("swingSpeed", 0.5);
        rig.Play(1);
        Assert.Equal(before + Frame * 0.5, strip.ClipTimeAt(rig.Animator.Time), 9);
        rig.Play(60);
        Assert.Contains("+none@61", rig.Log);
    }

    [Fact]
    public void AnInterruptingFadeGoesOverTheCurrentMixWhileBothClipsPlay()
    {
        var rig = new Rig();
        rig.Machine.SetTrigger("attack");
        rig.Play(1);
        rig.Play(3);
        // Half way through the swing's fade-in (frames 1 to 7), hurt: a third entry fading over
        // none and the swing; all three play on.
        rig.Machine.SetTrigger("hurt");
        rig.Play(1);
        var fades = rig.Machine.FadesOf("action");
        Assert.Equal(3, fades.Entries.Count);
        var time = rig.Animator.Time + Frame;
        Assert.Equal(1.0, fades.ShareAt(0, time) + fades.ShareAt(1, time) + fades.ShareAt(2, time), 9);
        Assert.True(fades.ShareAt(0, time) > 0 && fades.ShareAt(1, time) > 0);
        rig.Play(1);
        // The pose is the nested mix of the three sides, each over the base layer, each clip at its
        // own time (the swing 5 frames in, not frozen at the interruption).
        var sides = fades.Entries.Select(e => e.Strip).ToArray();
        Assert.Equal(5 * Frame, sides[1]!.ClipTimeAt(rig.Animator.Time), 9);
        // None is gone once the swing's own fade ends (frame 7: its share is 0 from then on, the
        // hurt still fading in over both); the swing once the hurt is fully in (frame 8).
        rig.Play(3);
        Assert.Equal("+swing@1, +hurt@5, -none@7, -swing@8", rig.Log);
        Assert.Single(fades.Entries);
    }

    [Fact]
    public void NoticesComeLeftThenEnteredThenClipEventsAndCanBeAwaited()
    {
        // A fade of no length: the entered and the left notice come at one moment.
        var definition = new StateMachineBuilder("m").Trigger("go")
            .Layer("l", BlendMode.Replace, "a", l => l.State("a", "idle", loop: true).State("b", "pulse").Transition("a", "b", 0, Trigger("go")))
            .Build();
        var rig = new Rig(definition);
        var order = new List<string>();
        rig.Animator.StateChanged += n => order.Add((n.Entered ? "entered " : "left ") + n.State);
        rig.Animator.Event += e => order.Add("event " + e.Name);
        var entered = rig.Animator.Entered("l", "b");
        var left = rig.Animator.Left("l", "a");
        rig.Play(2);
        rig.Machine.SetTrigger("go");
        rig.Play(1);
        // The pulse's "start" marker is on its first frame, at the moment it enters.
        Assert.Equal(["left a", "entered b", "event start"], order);
        Assert.True(entered.IsCompletedSuccessfully && left.IsCompletedSuccessfully);
        Assert.Equal(3 * Frame, entered.Result.Time, 9);
    }

    [Fact]
    public void AnItemPlaysItsClipOfTheStatesNameWithTheWearersFades()
    {
        var rig = new Rig();
        var pennant = ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, "pennant.glb"));
        var item = new ItemAnimation(pennant, rig.Machine);
        var flagSwing = new ClipBinding(AnimationClip.AllOf(pennant).Single(c => c.Name == "swing"), item.Skeleton);
        var flag = item.Skeleton.Find("flag");
        // Idle and none: the pennant has no such clips, so it rests.
        rig.Play(3);
        Assert.False(item.Pose(rig.Animator.Time).TryGet(flag, out _));
        // The wearer enters "swing" at frame 4 with a 0.1 s fade: the pennant's "swing" fades in with it,
        // on the wearer's clock and speed (double here: the swing speed parameter).
        rig.Machine.SetFloat("swingSpeed", 2);
        rig.Machine.SetTrigger("attack");
        rig.Play(4);
        double t = rig.Animator.Time, start = 4 * Frame, weight = (t - start) / 0.1;
        Expect(item, flagSwing, (t - start) * 2, weight, t);
        // After the fade the pennant's clip alone.
        rig.Play(3);
        t = rig.Animator.Time;
        Expect(item, flagSwing, (t - start) * 2, 1, t);
        // Hurt (no pennant clip): the pennant fades back to rest as the wearer fades into hurt.
        rig.Machine.SetTrigger("hurt");
        rig.Play(2);
        t = rig.Animator.Time;
        var hurtStart = t - Frame;
        Expect(item, flagSwing, (t - start) * 2, 1 - (t - hurtStart) / 0.05, t);
    }

    /// <summary>The item's pose at <paramref name="time"/> is its rest mixed toward its clip at <paramref name="clipTime"/> by <paramref name="weight"/>.</summary>
    static void Expect(ItemAnimation item, ClipBinding clip, double clipTime, double weight, double time)
    {
        var expected = new PoseBuffer(item.Skeleton);
        var side = new PoseBuffer(item.Skeleton);
        clip.Sample(clipTime, side);
        expected.MixToward(side, weight);
        var pose = item.Pose(time);
        var want = expected.ToPose();
        var node = item.Skeleton.Find("flag");
        Assert.True(pose.TryGet(node, out var got));
        Assert.True(want.TryGet(node, out var wanted));
        Assert.True(Math.Abs(System.Numerics.Quaternion.Dot(got.Rotation, wanted.Rotation)) > 1 - 1e-9, $"flag at {time * 60:F2}: {got.Rotation} instead of {wanted.Rotation}");
    }

    static void Same(AnimationStack expected, AnimationStack actual, double time)
    {
        var a = new PoseBuffer(expected.Skeleton);
        var b = new PoseBuffer(actual.Skeleton);
        expected.Evaluate(time, a);
        actual.Evaluate(time, b);
        for (var i = 0; i < a.Rotation.Length; i++)
            Assert.Equal(a.Rotation[i], b.Rotation[i], 12);
        for (var i = 0; i < a.Translation.Length; i++)
            Assert.Equal(a.Translation[i], b.Translation[i], 12);
    }
}
