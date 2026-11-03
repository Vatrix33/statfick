using Statventure.Animation;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;
using Statventure.Tests.Render3D;
using static Statventure.Animation.ConditionDefinition;

namespace Statventure.Tests.Animation;

/// <summary>
/// State machines as records (M9 step 5; Docs/Assets.md, Q20): the M7 machines through the binary and
/// the JSON form are the built ones; a machine written by hand in its JSON form reads as the builder
/// makes it; machines are packed from their JSON form beside the models and load from the pack and
/// from a loose folder; a figure and its machine from a pack play as the built ones do.
/// </summary>
public class StateMachineRecordTests
{
    /// <summary>A machine with a value in every field of every state machine record type (the fixture's).</summary>
    internal static StateMachineDefinition Full() => new StateMachineBuilder("full")
        .Bool("dead", true).Int("combo", 2).Float("speed", 1.5).Trigger("attack")
        .Layer("base", BlendMode.Replace, "idle", l => l
            .State("idle", "idle", loop: true)
            .State("run", "twist", speed: 1.25, speedParameter: "speed", loop: true)
            .Transition("idle", "run", 0.25, Of("combo", Compare.NotEqual, 0), Of("speed", Compare.Greater, 0.5))
            .TransitionAfter("run", "idle", 0.75, 0.5, Of("speed", Compare.LessOrEqual, 0.5))
            .FromAny("idle", 0.1, true, Is("dead")))
        .Layer("upper", BlendMode.Combine, "none", l => l
            .Empty("none")
            .State("swing", "swing", speed: 2)
            .Transition("none", "swing", 0.1, Trigger("attack"), Of("combo", Compare.Less, 3), Of("combo", Compare.GreaterOrEqual, 1), Of("speed", Compare.Equal, 1.5))
            .TransitionAfter("swing", "none", 1, 0.15))
        .Build();

    /// <summary>A machine of defaults: no parameters, one empty state, no transitions.</summary>
    static StateMachineDefinition Still() => new StateMachineBuilder("still").Layer("base", BlendMode.Replace, "rest", l => l.Empty("rest")).Build();

    public static TheoryData<string> MachineNames => ["humanoid", "full", "still"];

    static StateMachineDefinition Machine(string name) => name switch
    {
        "humanoid" => StateMachineTests.Humanoid(),
        "full" => Full(),
        _ => Still(),
    };

    [Fact]
    public void TheStateMachineRecordFixtureIsRecorded() => RecordFixtures.Ensure("state-machine", Full());

    [Theory]
    [MemberData(nameof(MachineNames))]
    public void TheM7MachinesComeBackFromBothForms(string name)
    {
        var built = Machine(name);
        RecordAssert.Equal(built, RecordSerializer.FromBinary<StateMachineDefinition>(RecordSerializer.ToBinary(built)));
        RecordAssert.Equal(built, RecordSerializer.FromJson<StateMachineDefinition>(RecordSerializer.ToJson(built)));
    }

    /// <summary>The JSON form as a person writes it: keys the defaults cover left out, null for "from any state".</summary>
    [Fact]
    public void AMachineWrittenByHandIsTheBuiltOne()
    {
        const string json = """
            {
              "$types": {
                "animation.state-machine": 1,
                "animation.parameter": 1,
                "animation.layer": 1,
                "animation.state": 1,
                "animation.transition": 1,
                "animation.condition": 1
              },
              "name": "door",
              "parameters": [
                { "name": "open", "kind": "bool" },
                { "name": "knock", "kind": "trigger" },
                { "name": "pace", "kind": "float", "default": 0.5 }
              ],
              "layers": [
                {
                  "name": "base",
                  "mode": "replace",
                  "entry": "shut",
                  "states": [
                    { "name": "shut", "clip": "idle", "loop": true },
                    { "name": "opening", "clip": "swing", "speed": 2, "speedParameter": "pace" },
                    { "name": "gone", "clip": null }
                  ],
                  "transitions": [
                    { "from": "shut", "to": "opening", "conditions": [ { "parameter": "open" } ], "duration": 0.2 },
                    { "from": "opening", "to": "gone", "conditions": [ { "parameter": "pace", "compare": "lessOrEqual", "value": 0 } ], "exitTime": 0.5 },
                    { "from": null, "to": "shut", "conditions": [ { "parameter": "knock" } ], "mayRestart": true }
                  ]
                }
              ]
            }
            """;
        var built = new StateMachineBuilder("door").Bool("open").Trigger("knock").Float("pace", 0.5)
            .Layer("base", BlendMode.Replace, "shut", l => l
                .State("shut", "idle", loop: true)
                .State("opening", "swing", speed: 2, speedParameter: "pace")
                .Empty("gone")
                .Transition("shut", "opening", 0.2, Is("open"))
                .TransitionAfter("opening", "gone", 0.5, 0, Of("pace", Compare.LessOrEqual, 0))
                .FromAny("shut", 0, true, Trigger("knock")))
            .Build();
        var read = RecordSerializer.FromJson<StateMachineDefinition>(json);
        RecordAssert.Equal(built, read);
        read.Validate();
    }

    /// <summary>A state's clip and a transition's source state must be given (null for none and for any).</summary>
    [Fact]
    public void AStatesClipAndATransitionsSourceMustBeGiven()
    {
        var state = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<StateDefinition>(
            "{ \"$types\": { \"animation.state\": 1 }, \"name\": \"a\" }"));
        Assert.Contains("needs the key \"clip\"", state.Message);
        var transition = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<TransitionDefinition>(
            "{ \"$types\": { \"animation.transition\": 1, \"animation.condition\": 1 }, \"to\": \"a\", \"conditions\": [] }"));
        Assert.Contains("needs the key \"from\"", transition.Message);
    }

    [Fact]
    public void MachinesArePackedFromTheirJsonFormBesideTheModels()
    {
        using var folder = new TempFolder();
        folder.Write("source/models/figure.glb", File.ReadAllBytes(TestModelPack.SourceOf("figure")));
        folder.Write("source/models/machines/humanoid.json", RecordSerializer.ToJson(StateMachineTests.Humanoid()));
        folder.Write("source/models/machines/full.json", RecordSerializer.ToJson(Full()));
        var writer = new PackWriter();
        writer.AddFolder(folder["source"]);
        writer.Write(folder["models.pak"]);
        using (var file = PackFile.Open(folder["models.pak"]))
        {
            Assert.Equal(["models/figure", "models/machines/full", "models/machines/humanoid"], file.Entries.Select(e => e.Path));
            Assert.All(file.Entries, e => Assert.True(e.IsRecord));
            Assert.Equal(["model", "animation.state-machine", "animation.state-machine"], file.Entries.Select(e => RecordSerializer.RootTypeName(file.Read(e))));
        }
        using var packed = new VirtualFileSystem([Mount.Pack(folder["models.pak"])]);
        using var loose = new VirtualFileSystem([Mount.Folder(folder["source"])]);
        foreach (var files in new[] { packed, loose })
        {
            RecordAssert.Equal(StateMachineTests.Humanoid(), StateMachineDefinition.Load(files, "models/machines/humanoid"));
            RecordAssert.Equal(Full(), StateMachineDefinition.Load(files, "models/machines/full"));
        }
        var notMachine = Assert.Throws<DataFileException>(() => StateMachineDefinition.Load(packed, "models/figure"));
        Assert.Equal(("models/figure", "a record of type model, not animation.state-machine"), (notMachine.Entry, notMachine.Problem));
    }

    /// <summary>
    /// A machine from the game's data is validated when loaded (Docs/Assets.md, S5-1): one that reads but
    /// doesn't validate is a <see cref="DataFileException"/> naming the pack and entry, or the loose file,
    /// with what <see cref="StateMachineDefinition.Validate"/> found.
    /// </summary>
    [Fact]
    public void AMachineThatDoesntValidateIsAProblemNamingWhereItIs()
    {
        var broken = new StateMachineDefinition("broken", [],
            [new LayerDefinition("base", BlendMode.Replace, "shut", [new StateDefinition("shut", "idle")], [new TransitionDefinition("shut", "nowhere", [], 1)])]);
        const string expected = "not a valid state machine: broken/base: a transition shut → nowhere names a state that doesn't exist";
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("models/machines/broken", broken);
        writer.AddRecord("models/machines/full", Full());
        writer.Write(folder["models.pak"]);
        using (var packed = new VirtualFileSystem([Mount.Pack(folder["models.pak"])]))
        {
            var problem = Assert.Throws<DataFileException>(() => StateMachineDefinition.Load(packed, "models/machines/broken"));
            Assert.Equal((Path.GetFullPath(folder["models.pak"]), "models/machines/broken", expected), (problem.File, problem.Entry, problem.Problem));
            Assert.IsType<ArgumentException>(problem.InnerException);
            RecordAssert.Equal(Full(), StateMachineDefinition.Load(packed, "models/machines/full"));
        }
        var file = folder.Write("dev/models/machines/broken.json", RecordSerializer.ToJson(broken));
        using var loose = new VirtualFileSystem([Mount.Folder(folder["dev"])]);
        var looseProblem = Assert.Throws<DataFileException>(() => StateMachineDefinition.Load(loose, "models/machines/broken"));
        Assert.Equal((Path.GetFullPath(file), (string?)null, expected), (looseProblem.File, looseProblem.Entry, looseProblem.Problem));
    }

    /// <summary>
    /// The figure and the humanoid machine from a pack drive an animator as the .glb and the builder do:
    /// the same notices and clip events at the same moments, the same pose channel for channel.
    /// </summary>
    [Fact]
    public void AFigureAndItsMachineFromAPackPlayAsTheBuiltOnes()
    {
        using var folder = new TempFolder();
        folder.Write("source/models/figure.glb", File.ReadAllBytes(TestModelPack.SourceOf("figure")));
        folder.Write("source/models/machines/humanoid.json", RecordSerializer.ToJson(StateMachineTests.Humanoid()));
        var writer = new PackWriter();
        writer.AddFolder(folder["source"]);
        writer.Write(folder["models.pak"]);
        using var files = new VirtualFileSystem([Mount.Pack(folder["models.pak"])]);
        var built = new Player(ModelData.Load(TestModelPack.SourceOf("figure")), StateMachineTests.Humanoid());
        var packed = new Player(ModelData.Load(files, "models/figure"), StateMachineDefinition.Load(files, "models/machines/humanoid"));
        foreach (var player in new[] { built, packed })
        {
            player.Play(5);
            player.Machine.SetTrigger("attack");
            player.Play(20);
            player.Machine.SetInt("combo", 2);
            player.Machine.SetFloat("swingSpeed", 1.5);
            player.Machine.SetTrigger("attack");
            player.Play(20);
            player.Machine.SetTrigger("hurt");
            player.Play(30);
            player.Machine.SetBool("dead", true);
            player.Play(25);
        }
        Assert.Equal(built.Log, packed.Log);
        Assert.Contains("+combo", built.Log);
        Assert.Contains("hit@", built.Log);
        Assert.Equal(built.Poses.Count, packed.Poses.Count);
        for (var i = 0; i < built.Poses.Count; i++)
            RecordAssert.Equal(built.Poses[i], packed.Poses[i]);
    }

    /// <summary>A figure's animator on its machine, logging notices and clip events and keeping each frame's pose.</summary>
    sealed class Player
    {
        readonly AnimationStack _stack;
        readonly Animator _animator;
        readonly List<string> _log = [];

        public Player(ModelData figure, StateMachineDefinition definition)
        {
            var skeleton = new Skeleton(figure);
            var clips = AnimationClip.AllOf(figure);
            _stack = new AnimationStack(skeleton);
            Machine = new StateMachine(definition, _stack, name => new ClipBinding(clips.Single(c => c.Name == name), skeleton));
            _animator = new Animator(_stack, Machine);
            _animator.StateChanged += n => _log.Add($"{(n.Entered ? "+" : "-")}{n.State}@{n.Time:R}");
            _animator.Event += e => _log.Add($"{e.Name}@{e.Time:R}");
        }

        public StateMachine Machine { get; }
        public List<(double[] Translation, double[] Rotation, double[] Scale)> Poses { get; } = [];
        public string Log => string.Join(", ", _log);

        public void Play(int frames)
        {
            for (var i = 0; i < frames; i++)
            {
                _animator.Advance(1 / 60.0);
                _animator.DispatchEvents();
                var pose = new PoseBuffer(_stack.Skeleton);
                _stack.Evaluate(_animator.Time, pose);
                Poses.Add(([.. pose.Translation], [.. pose.Rotation], [.. pose.Scale]));
            }
        }
    }
}
