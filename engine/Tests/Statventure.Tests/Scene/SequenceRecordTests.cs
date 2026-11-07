using System.Reflection;
using System.Runtime.CompilerServices;
using Statventure.Animation;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;

namespace Statventure.Tests.Scene;

/// <summary>
/// Sequences as records (Docs/Scene.md, step 4, S4-1): the <c>scene.*</c> types through both forms,
/// a sequence written by hand in its JSON form read with the decided defaults, every rule
/// <see cref="Sequence.Validate()"/> checks alone and against its stages (names, kinds, numbers, times,
/// markers, S4-2), marker times mapped onto the timeline through the stage's clip track, and loading
/// from a pack and a loose folder, a broken one refused naming the file.
/// </summary>
public class SequenceRecordTests
{
    static FCurve Ramp(double from, double to) => new([FCurveKey.Linear(0, from), FCurveKey.Linear(1, to)]);

    /// <summary>A sequence with a value other than the default in every field of every scene.* sequence type (the fixture's).</summary>
    internal static Sequence EveryField()
    {
        var strip = new SequenceStrip("sweep", 0.5, 0.25, 1.75, 2, double.PositiveInfinity, true, 0.1, 0.2, 0.75, BlendMode.Combine, Extrapolation.Hold);
        return new Sequence
        {
            Name = "every-field",
            Duration = 3.5,
            Skippable = false,
            Blocking = false,
            Roles = [new("hero", RoleKind.Actor), new("pivot", RoleKind.Node), new("prop", RoleKind.Model), new("card", RoleKind.Element), new("tint", RoleKind.Palette)],
            Stages = ["models/stages/arena"],
            Tracks =
            [
                new ClipTrack { Stage = "arena", Strip = strip },
                new TransformTrack
                {
                    Node = "pivot",
                    LocationX = Ramp(0, 1), LocationY = Ramp(1, 2), LocationZ = Ramp(2, 3),
                    RotationX = Ramp(0, 0.5), RotationY = Ramp(0.5, 1), RotationZ = Ramp(1, 1.5),
                    ScaleX = Ramp(1, 2), ScaleY = Ramp(2, 1), ScaleZ = Ramp(1, 3),
                },
                new VisibilityTrack { Node = "arena/door", Cues = [new(0.5, true, "arena/open"), new(1, false)] },
                new ActorTrack
                {
                    Actor = "hero", Mark = "arena/mark.hero", Strips = [strip with { Clip = "swing" }],
                    Cues = [new(0.25, "speed", ParameterKind.Float, 1.5, "arena/open"), new(1, "attack", ParameterKind.Trigger)],
                },
                new PaletteTrack { Target = "prop", Cues = [new(0, "tint", null, "arena/open"), new(1, null, "#101010"), new(2)] },
                new EventTrack { Cues = [new(3.5, "end", "arena/open")] },
                new CameraTrack { Cuts = [new(0, "arena/camera.wide", "arena/open")], FieldOfView = Ramp(0.6, 0.4) },
                new LightTrack { Cues = [new(0, "arena/sun", "arena/open")], Strength = Ramp(3, 5), Angle = Ramp(3, 6) },
                new ParticleTrack { Cues = [new(1, "models/effects/mist", "arena/rift", 7, 12, 30, 2.5, "tint", "arena/open")] },
                new AudioTrack
                {
                    Cues = [new(0, AudioCueKind.Music, "great-imp-jiruan", 0.5, -0.25, 1.5, "arena/open"), new(2, AudioCueKind.StopMusic), new(1, AudioCueKind.Effect, "whoosh")],
                },
                new UiTrack
                {
                    Host = "card",
                    Cues = [new(0, UiCueAction.SetAttribute, "title", "data-boss", "jiruan", "arena/open"), new(3.5, UiCueAction.SetText, "label", null, "LORD OF THE IMP COURT")],
                },
            ],
            Reduced = [new(0, 0.5), new(1, 1, 2.4)],
        };
    }

    [Fact]
    public void TheSequenceRecordFixtureIsRecorded() => RecordFixtures.Ensure("every-field", EveryField());

    public static TheoryData<string> Sequences => ["every-field", "test"];

    static Sequence Named(string name) => name == "every-field" ? EveryField() : TestSequence.Build();

    /// <summary>A sequence comes back from both forms field by field, every track kind, cue kind and curve included.</summary>
    [Theory]
    [MemberData(nameof(Sequences))]
    public void SequencesComeBackFromBothForms(string name)
    {
        var sequence = Named(name);
        RecordAssert.Equal(sequence, RecordSerializer.FromBinary<Sequence>(RecordSerializer.ToBinary(sequence)));
        RecordAssert.Equal(sequence, RecordSerializer.FromJson<Sequence>(RecordSerializer.ToJson(sequence)));
    }

    [Fact]
    public void EveryFieldIsAValidSequenceByItself() => EveryField().Validate();

    /// <summary>
    /// The JSON form as a person writes it, the keys the defaults cover left out: skippable and blocking,
    /// no reduced variant, a strip's settings (Replace, held forward), a cue's marker, an audio cue's
    /// volume, pan and pitch, a parameter cue's value 1; it reads as the builder makes it.
    /// </summary>
    [Fact]
    public void ASequenceWrittenByHandIsTheBuiltOne()
    {
        const string json = """
            {
              "$types": {
                "scene.sequence": 1, "scene.role": 1, "scene.track": 1, "scene.clip-track": 1, "scene.strip": 1,
                "scene.animation-track": 1, "scene.parameter-cue": 1, "scene.event-track": 1, "scene.event-cue": 1,
                "scene.audio-track": 1, "scene.audio-cue": 1
              },
              "name": "door",
              "duration": 2,
              "roles": [ { "name": "hero", "kind": "actor" } ],
              "stages": [ "models/stages/hierarchy" ],
              "tracks": [
                { "$type": "scene.clip-track", "stage": "hierarchy", "strip": { "clip": "base" } },
                { "$type": "scene.animation-track", "actor": "hero", "strips": [ { "clip": "swing", "start": 0.5 } ],
                  "cues": [ { "time": 1, "parameter": "attack", "kind": "trigger" } ] },
                { "$type": "scene.event-track", "cues": [ { "time": 0.5, "name": "open", "marker": "hierarchy/open" } ] },
                { "$type": "scene.audio-track", "cues": [ { "time": 0, "kind": "music", "name": "great-imp-jiruan" } ] }
              ]
            }
            """;
        var built = new SequenceBuilder("door", 2).Role("hero", RoleKind.Actor).Stage(TestSequence.StagePath, TestSequence.Stage())
            .Clip("hierarchy", "base")
            .Animation("hero", null, a => a.Strip(new SequenceStrip("swing", 0.5)).Trigger(1, "attack"))
            .Events(e => e.Event(CueTime.At("hierarchy/open", 0.5), "open"))
            .Audio(a => a.Music(0, "great-imp-jiruan"))
            .Build();
        var read = RecordSerializer.FromJson<Sequence>(json);
        RecordAssert.Equal(built, read);
        Assert.Equal((true, true, null), (read.Skippable, read.Blocking, read.Reduced));
        var strip = ((ClipTrack)read.Tracks[0]).Strip;
        Assert.Equal((0, 0, null, 1, 1, false, 0, 0, null, BlendMode.Replace, Extrapolation.HoldForward),
            (strip.Start, strip.ClipStart, strip.ClipEnd, strip.Scale, strip.Repeat, strip.Reversed, strip.BlendIn, strip.BlendOut, strip.Influence, strip.Mode, strip.Extrapolation));
        var music = ((AudioTrack)read.Tracks[3]).Cues[0];
        Assert.Equal((1, 0, 1, null), (music.Volume, music.Pan, music.Pitch, music.Marker));
        Assert.Equal(1, ((ActorTrack)read.Tracks[1]).Cues[0].Value);
        read.Validate(new Dictionary<string, ModelData> { [TestSequence.StagePath] = TestSequence.Stage() });
    }

    /// <summary>A valid small sequence the rule cases break one way each.</summary>
    static Sequence Valid() => new()
    {
        Name = "valid",
        Duration = 2,
        Roles = [new("hero", RoleKind.Actor), new("pivot", RoleKind.Node), new("prop", RoleKind.Model), new("card", RoleKind.Element), new("tint", RoleKind.Palette)],
        Stages = [TestSequence.StagePath],
        Tracks = [new ClipTrack { Stage = "hierarchy", Strip = new SequenceStrip("base") }],
    };

    static Sequence With(params SequenceTrack[] tracks) => Valid() with { Tracks = [.. Valid().Tracks, .. tracks] };

    static readonly Dictionary<string, (Func<Sequence> Make, string Problem, string Path)> Broken = new()
    {
        ["no name"] = (() => Valid() with { Name = "" }, "a sequence needs a name", "$.name"),
        ["no duration"] = (() => Valid() with { Duration = 0 }, "the sequence valid: its duration (0 s) must be a finite number above 0", "$.duration"),
        ["NaN duration"] = (() => Valid() with { Duration = double.NaN }, "its duration (NaN s) must be a finite number above 0", "$.duration"),
        ["role twice"] = (() => Valid() with { Roles = [new("a", RoleKind.Node), new("a", RoleKind.Actor)] }, "the role a is defined twice", "$.roles[1].name"),
        ["role with a slash"] = (() => Valid() with { Roles = [new("a/b", RoleKind.Node)] }, "the role \"a/b\" needs a name without /", "$.roles[0].name"),
        ["role kind"] = (() => Valid() with { Roles = [new("a", (RoleKind)9)] }, "the role a has a kind that isn't one", "$.roles[0].kind"),
        ["stage path"] = (() => Valid() with { Stages = ["models/hierarchy"], Tracks = [] }, "the stage models/hierarchy isn't a path models/stages/<name>", "$.stages[0]"),
        ["stage twice"] = (() => Valid() with { Stages = [TestSequence.StagePath, TestSequence.StagePath] }, "the stage hierarchy is named twice", "$.stages[1]"),
        ["stage folder"] = (() => Valid() with { Stages = ["world/stages/arena"], Tracks = [] }, "the stage world/stages/arena isn't a path models/stages/<name>", "$.stages[0]"),
        ["stage depth"] = (() => Valid() with { Stages = ["models/stages/a/b"], Tracks = [] }, "the stage models/stages/a/b isn't a path models/stages/<name>", "$.stages[0]"),
        ["stage as role"] = (() => Valid() with { Roles = [new("hierarchy", RoleKind.Node)] }, "the stage models/stages/hierarchy has the name of the role hierarchy", "$.stages[0]"),
        ["null track"] = (() => Valid() with { Tracks = [null!] }, "the sequence valid, track 0: it is null", "$.tracks[0]"),
        ["clip stage"] = (() => With(new ClipTrack { Stage = "arena", Strip = new("base") }), "track 1 (clip): arena isn't one of its stages", "$.tracks[1].stage"),
        ["strip scale"] = (() => With(new ClipTrack { Stage = "hierarchy", Strip = new("base", Scale: 0) }), "track 1 (clip): its strip (base): its scale and repeat must be above 0, its blends not negative", "$.tracks[1].strip.scale"),
        ["strip repeat"] = (() => With(new ClipTrack { Stage = "hierarchy", Strip = new("base", Repeat: 0) }), "its scale and repeat must be above 0", "$.tracks[1].strip.repeat"),
        ["strip blend"] = (() => With(new ClipTrack { Stage = "hierarchy", Strip = new("base", BlendOut: -1) }), "its scale and repeat must be above 0, its blends not negative", "$.tracks[1].strip.blendOut"),
        ["strip part"] = (() => With(new ClipTrack { Stage = "hierarchy", Strip = new("base", ClipStart: 0.5, ClipEnd: 0.25) }), "its part (0.5–0.25 s) must start at 0 or later and not end before it starts", "$.tracks[1].strip.clipEnd"),
        ["strip part start"] = (() => With(new ClipTrack { Stage = "hierarchy", Strip = new("base", ClipStart: -0.25) }), "its part (-0.25– s) must start at 0 or later and not end before it starts", "$.tracks[1].strip.clipStart"),
        ["strip influence"] = (() => With(new ClipTrack { Stage = "hierarchy", Strip = new("base", Influence: 1.5) }), "its influence must be from 0 to 1", "$.tracks[1].strip.influence"),
        ["strip NaN"] = (() => With(new ClipTrack { Stage = "hierarchy", Strip = new("base", Start: double.NaN) }), "its numbers must be finite (its repeat may be infinite)", "$.tracks[1].strip.start"),
        ["strip mode"] = (() => With(new ClipTrack { Stage = "hierarchy", Strip = new("base", Mode: (BlendMode)7) }), "a blend mode or extrapolation that isn't one", "$.tracks[1].strip.mode"),
        ["strip clip"] = (() => With(new ClipTrack { Stage = "hierarchy", Strip = new("") }), "its strip names no clip", "$.tracks[1].strip.clip"),
        ["no curve"] = (() => With(new TransformTrack { Node = "pivot" }), "track 1 (transform): it has no curve", "$.tracks[1]"),
        ["node kind"] = (() => With(new TransformTrack { Node = "hero", LocationX = Ramp(0, 1) }), "its node hero is a role of kind actor, not node", "$.tracks[1].node"),
        ["node role"] = (() => With(new TransformTrack { Node = "nobody", LocationX = Ramp(0, 1) }), "its node nobody isn't a role", "$.tracks[1].node"),
        ["node stage"] = (() => With(new TransformTrack { Node = "arena/Base", LocationX = Ramp(0, 1) }), "its node arena/Base isn't a node role or <stage>/<node> of one of its stages", "$.tracks[1].node"),
        ["node empty"] = (() => With(new TransformTrack { Node = "hierarchy/", LocationX = Ramp(0, 1) }), "isn't a node role or <stage>/<node> of one of its stages", "$.tracks[1].node"),
        ["visibility node"] = (() => With(new VisibilityTrack { Node = "nobody" }), "track 1 (visibility): its node nobody isn't a role", "$.tracks[1].node"),
        ["cue late"] =(() => With(new VisibilityTrack { Node = "pivot", Cues = [new(2.5, true)] }), "track 1 (visibility): cue 0 at 2.5 s is outside the timeline (0–2 s)", "$.tracks[1].cues[0].time"),
        ["cue early"] = (() => With(new VisibilityTrack { Node = "pivot", Cues = [new(-0.5, true)] }), "cue 0 at -0.5 s is outside the timeline", "$.tracks[1].cues[0].time"),
        ["cue NaN"] = (() => With(new VisibilityTrack { Node = "pivot", Cues = [new(double.NaN, true)] }), "cue 0: its time must be a finite number", "$.tracks[1].cues[0].time"),
        ["null cue"] = (() => With(new VisibilityTrack { Node = "pivot", Cues = [null!] }), "cue 0 is null", "$.tracks[1].cues[0]"),
        ["marker form"] = (() => With(new VisibilityTrack { Node = "pivot", Cues = [new(0, true, "open")] }), "cue 0's marker open isn't <stage>/<marker> of one of its stages", "$.tracks[1].cues[0].marker"),
        ["marker end"] = (() => With(new VisibilityTrack { Node = "pivot", Cues = [new(0, true, "hierarchy/")] }), "cue 0's marker hierarchy/ isn't <stage>/<marker> of one of its stages", "$.tracks[1].cues[0].marker"),
        ["marker stage"] = (() => With(new VisibilityTrack { Node = "pivot", Cues = [new(0, true, "arena/open")] }), "cue 0's marker arena/open isn't <stage>/<marker>", "$.tracks[1].cues[0].marker"),
        ["actor"] = (() => With(new ActorTrack { Actor = "pivot" }), "track 1 (animation): its actor pivot is a role of kind node, not actor", "$.tracks[1].actor"),
        ["mark"] = (() => With(new ActorTrack { Actor = "hero", Mark = "tint" }), "its mark tint is a role of kind palette, not node", "$.tracks[1].mark"),
        ["strip null"] = (() => With(new ActorTrack { Actor = "hero", Strips = [null!] }), "strip 0 is null", "$.tracks[1].strips[0]"),
        ["parameter"] = (() => With(new ActorTrack { Actor = "hero", Cues = [new(0, "", ParameterKind.Float)] }), "cue 0 names no parameter", "$.tracks[1].cues[0].parameter"),
        ["parameter kind"] = (() => With(new ActorTrack { Actor = "hero", Cues = [new(0, "a", (ParameterKind)9)] }), "cue 0 has a kind that isn't one", "$.tracks[1].cues[0].kind"),
        ["parameter value"] = (() => With(new ActorTrack { Actor = "hero", Cues = [new(0, "a", ParameterKind.Float, double.PositiveInfinity)] }), "cue 0's value must be a finite number", "$.tracks[1].cues[0].value"),
        ["trigger value"] = (() => With(new ActorTrack { Actor = "hero", Cues = [new(0, "a", ParameterKind.Trigger, 0)] }), "cue 0: a trigger cue's value must be 1", "$.tracks[1].cues[0].value"),
        ["bool value"] = (() => With(new ActorTrack { Actor = "hero", Cues = [new(0, "a", ParameterKind.Bool, 2)] }), "cue 0: a bool cue's value must be 0 or 1", "$.tracks[1].cues[0].value"),
        ["int value"] = (() => With(new ActorTrack { Actor = "hero", Cues = [new(0, "a", ParameterKind.Int, 1.5)] }), "cue 0: an int cue's value must be a whole number", "$.tracks[1].cues[0].value"),
        ["two marks"] = (() => With(new ActorTrack { Actor = "hero", Mark = "pivot" }, new ActorTrack { Actor = "hero", Mark = "hierarchy/Tip" }),
            "the sequence valid: the actor hero has a mark on 2 animation tracks (one at most: it stands on one node)", "$.tracks[2].mark"),
        ["palette target"] = (() => With(new PaletteTrack { Target = "pivot" }), "track 1 (palette): its target pivot is a role of kind node, not actor or model", "$.tracks[1].target"),
        ["palette both"] = (() => With(new PaletteTrack { Target = "prop", Cues = [new(0, "tint", "#000000")] }), "cue 0 names a palette and a fill: one or neither", "$.tracks[1].cues[0]"),
        ["palette role"] = (() => With(new PaletteTrack { Target = "prop", Cues = [new(0, "card")] }), "cue 0's palette card is a role of kind element, not palette", "$.tracks[1].cues[0].palette"),
        ["palette fill"] = (() => With(new PaletteTrack { Target = "hero", Cues = [new(0, null, "#00000g")] }), "cue 0's fill #00000g isn't a colour (#rrggbb)", "$.tracks[1].cues[0].fill"),
        ["event name"] = (() => With(new EventTrack { Cues = [new(0, "")] }), "track 1 (event): cue 0 has no name", "$.tracks[1].cues[0].name"),
        ["camera"] = (() => With(new CameraTrack { Cuts = [new(0, "hero")] }), "track 1 (camera): cue 0's camera hero is a role of kind actor, not node", "$.tracks[1].cuts[0].camera"),
        ["sun"] = (() => With(new LightTrack { Cues = [new(0, "card")] }), "track 1 (light): cue 0's sun card is a role of kind element, not node", "$.tracks[1].cues[0].sun"),
        ["effect"] = (() => With(new ParticleTrack { Cues = [new(0, "models/mist", "pivot")] }), "track 1 (particles): cue 0's effect models/mist isn't a path under models/effects/", "$.tracks[1].cues[0].effect"),
        ["burst"] = (() => With(new ParticleTrack { Cues = [new(0, "models/effects/mist", "pivot", Burst: -1)] }), "cue 0: its burst, rate and duration must be 0 or more", "$.tracks[1].cues[0].burst"),
        ["particle palette"] = (() => With(new ParticleTrack { Cues = [new(0, "models/effects/mist", "pivot", Palette: "prop")] }), "cue 0's palette prop is a role of kind model, not palette", "$.tracks[1].cues[0].palette"),
        ["particle node"] = (() => With(new ParticleTrack { Cues = [new(0, "models/effects/mist", "nobody")] }), "cue 0's node nobody isn't a role", "$.tracks[1].cues[0].node"),
        ["stop named"] = (() => With(new AudioTrack { Cues = [new(0, AudioCueKind.StopMusic, "theme")] }), "track 1 (audio): cue 0 stops the music but names theme", "$.tracks[1].cues[0].name"),
        ["music unnamed"] = (() => With(new AudioTrack { Cues = [new(0, AudioCueKind.Music)] }), "cue 0 names no theme", "$.tracks[1].cues[0].name"),
        ["effect unnamed"] = (() => With(new AudioTrack { Cues = [new(0, AudioCueKind.Effect)] }), "cue 0 names no effect", "$.tracks[1].cues[0].name"),
        ["pan"] = (() => With(new AudioTrack { Cues = [new(0, AudioCueKind.Effect, "hit", Pan: 2)] }), "cue 0: its volume must be 0 or more, its pan from −1 to 1 and its pitch above 0", "$.tracks[1].cues[0].pan"),
        ["pitch"] = (() => With(new AudioTrack { Cues = [new(0, AudioCueKind.Effect, "hit", Pitch: 0)] }), "its pitch above 0", "$.tracks[1].cues[0].pitch"),
        ["audio kind"] = (() => With(new AudioTrack { Cues = [new(0, (AudioCueKind)5, "hit")] }), "cue 0 has a kind that isn't one", "$.tracks[1].cues[0].kind"),
        ["host"] = (() => With(new UiTrack { Host = "prop" }), "track 1 (ui): its host prop is a role of kind model, not element", "$.tracks[1].host"),
        ["ui text named"] = (() => With(new UiTrack { Host = "card", Cues = [new(0, UiCueAction.SetText, "title", "x", "text")] }), "cue 0: SetText takes no name and a value", "$.tracks[1].cues[0].name"),
        ["ui class unnamed"] = (() => With(new UiTrack { Host = "card", Cues = [new(0, UiCueAction.AddClass)] }), "cue 0: AddClass takes a name and no value", "$.tracks[1].cues[0].name"),
        ["ui attribute value"] = (() => With(new UiTrack { Host = "card", Cues = [new(0, UiCueAction.SetAttribute, null, "hidden")] }), "cue 0: SetAttribute takes a name and a value", "$.tracks[1].cues[0].value"),
        ["ui class value"] = (() => With(new UiTrack { Host = "card", Cues = [new(0, UiCueAction.AddClass, null, "on", "x")] }), "cue 0: AddClass takes a name and no value", "$.tracks[1].cues[0].value"),
        ["ui element"] = (() => With(new UiTrack { Host = "card", Cues = [new(0, UiCueAction.AddClass, "", "on")] }), "cue 0 names an empty element id", "$.tracks[1].cues[0].element"),
        ["ui action"] = (() => With(new UiTrack { Host = "card", Cues = [new(0, (UiCueAction)9, null, "on")] }), "cue 0 has an action that isn't one", "$.tracks[1].cues[0].action"),
        ["no spans"] = (() => Valid() with { Reduced = [] }, "its reduced variant has no spans (null: none)", "$.reduced"),
        ["span order"] = (() => Valid() with { Reduced = [new(0.5, 1), new(0.75, 1.5)] },
            "reduced span 1 (0.75–1.5 s, held 0 s) must lie in the timeline after the span before, its end not before its start, its hold not negative", "$.reduced[1].from"),
        ["span late"] = (() => Valid() with { Reduced = [new(1, 2.5)] }, "reduced span 0 (1–2.5 s, held 0 s) must lie in the timeline", "$.reduced[0].to"),
        ["span backwards"] = (() => Valid() with { Reduced = [new(1, 0.5)] }, "reduced span 0 (1–0.5 s, held 0 s) must lie in the timeline", "$.reduced[0].to"),
        ["span hold"] = (() => Valid() with { Reduced = [new(1, 1, -1)] }, "reduced span 0 (1–1 s, held -1 s) must lie in the timeline", "$.reduced[0].hold"),
        ["span empty"] = (() => Valid() with { Reduced = [new(1, 1)] }, "reduced span 0 plays for no time", "$.reduced[0]"),
        ["span NaN"] = (() => Valid() with { Reduced = [new(double.NaN, 1)] }, "reduced span 0: its times must be finite numbers", "$.reduced[0].from"),
        ["span null"] = (() => Valid() with { Reduced = [null!] }, "reduced span 0 is null", "$.reduced[0]"),
    };

    public static TheoryData<string> BrokenCases => [.. Broken.Keys];

    /// <summary>
    /// Each rule a sequence keeps by itself, broken one at a time: an <see cref="ArgumentException"/> naming
    /// the sequence, the track, the cue and the problem; the sequence's check (M11 step 6, P20) reports it with
    /// the same words at the path of what it names, first among its problems.
    /// </summary>
    [Theory]
    [MemberData(nameof(BrokenCases))]
    public void ValidateRefusesASequenceBreakingARule(string label)
    {
        var (make, problem, path) = Broken[label];
        var sequence = make();
        var thrown = Assert.Throws<ArgumentException>(sequence.Validate).Message;
        Assert.Contains(problem, thrown);
        Assert.Contains(problem, Assert.Throws<ArgumentException>(() => sequence.Validate(new Dictionary<string, ModelData> { [TestSequence.StagePath] = TestSequence.Stage() })).Message);
        var problems = RecordChecks.Check(sequence);
        Assert.Equal(thrown, problems[0].Message);
        Assert.Contains(problems, p => p.Path.ToString() == path && p.Message.Contains(problem, StringComparison.Ordinal));
    }

    [Fact]
    public void TheValidOneIsValid()
    {
        Valid().Validate();
        Valid().Validate(new Dictionary<string, ModelData> { [TestSequence.StagePath] = TestSequence.Stage() });
    }

    static readonly Dictionary<string, (Func<Sequence> Make, string Problem, string Path)> BrokenWithTheStage = new()
    {
        ["stage node"] = (() => With(new TransformTrack { Node = "hierarchy/Nope", LocationX = Ramp(0, 1) }),
            "track 1 (transform): its node hierarchy/Nope: the stage models/stages/hierarchy has no node Nope", "$.tracks[1].node"),
        ["mark node"] = (() => With(new ActorTrack { Actor = "hero", Mark = "hierarchy/Nope" }), "its mark hierarchy/Nope: the stage models/stages/hierarchy has no node Nope", "$.tracks[1].mark"),
        ["stage clip"] = (() => Valid() with { Tracks = [new ClipTrack { Stage = "hierarchy", Strip = new("sweep") }] }, "track 0 (clip): the stage models/stages/hierarchy has no clip sweep", "$.tracks[0].strip.clip"),
        ["marker name"] = (() => With(new EventTrack { Cues = [new(0, "x", "hierarchy/nope")] }),
            "track 1 (event): cue 0's marker hierarchy/nope: the stage models/stages/hierarchy's clip base has no marker nope", "$.tracks[1].cues[0].marker"),
        ["marker clip track"] = (() => Valid() with { Tracks = [new EventTrack { Cues = [new(0, "x", "hierarchy/open")] }] },
            "track 0 (event): cue 0's marker hierarchy/open: the stage models/stages/hierarchy has no clip track, so no markers on the timeline", "$.tracks[0].cues[0].marker"),
        ["marker outside"] = (() => With(new EventTrack { Cues = [new(1.7, "x", "hierarchy/open")] }),
            "cue 0 at 1.7 s from the marker hierarchy/open falls at 2.1 s, outside the timeline (0–2 s)", "$.tracks[1].cues[0].time"),
        ["marker before"] = (() => With(new EventTrack { Cues = [new(-0.5, "x", "hierarchy/open")] }), "falls at -0.09999999999999998 s, outside the timeline", "$.tracks[1].cues[0].time"),
        ["stage missing"] = (() => Valid() with { Stages = [TestSequence.StagePath, "models/stages/arena"] }, "the sequence valid: its stage models/stages/arena isn't given", "$.stages[1]"),
    };

    public static TheoryData<string> BrokenWithTheStageCases => [.. BrokenWithTheStage.Keys];

    /// <summary>
    /// The names in its stages (Q15, S4-2): nodes, clips and markers, cues at markers within the timeline; a
    /// sequence that breaks none of them alone. Its check given the data the stage is in reports each at the
    /// path of what it names (a stage the data doesn't have in <see cref="Sequence.Load"/>'s words).
    /// </summary>
    [Theory]
    [MemberData(nameof(BrokenWithTheStageCases))]
    public void ValidateWithTheStagesRefusesNamesTheyDontHave(string label)
    {
        var (make, problem, path) = BrokenWithTheStage[label];
        var sequence = make();
        if (label != "stage missing")
            sequence.Validate();
        var stages = new Dictionary<string, ModelData> { [TestSequence.StagePath] = TestSequence.Stage() };
        Assert.Contains(problem, Assert.Throws<ArgumentException>(() => sequence.Validate(stages)).Message);
        Assert.Empty(RecordChecks.Check(sequence));
        using var data = new StageData();
        var problems = RecordChecks.Check(sequence, data.Files);
        var expected = label == "stage missing" ? "its stage models/stages/arena isn't in the game's data" : problem;
        Assert.Contains(problems, p => p.Path.ToString() == path && p.Message.Contains(expected, StringComparison.Ordinal));
    }

    /// <summary>
    /// The sequence's check goes on past a problem (M11 step 6): each role, stage, track field, strip, cue and span stops
    /// at its own first problem and the rest is checked on, each problem at its path; a message about several fields
    /// at each one that breaks it; nothing cascades from an empty clip.
    /// </summary>
    [Fact]
    public void TheCheckReportsEveryBrokenPartAtItsPath()
    {
        var sequence = Valid() with
        {
            Roles = [.. Valid().Roles, new("hero", RoleKind.Node)],
            Tracks =
            [
                new ClipTrack { Stage = "hierarchy", Strip = new("", Scale: 0, BlendIn: -1) },
                new TransformTrack { Node = "nobody" },
                new AudioTrack { Cues = [new(5, AudioCueKind.Effect, "hit", Volume: -1, Pitch: 0), new(0, AudioCueKind.Music)] },
                new ActorTrack { Actor = "pivot", Strips = [new("swing", Repeat: 0), new("swing")], Cues = [new(0, "", ParameterKind.Float)] },
            ],
            Reduced = [new(0.5, 0.25), new(1, 1)],
        };
        Assert.Equal([
                "$.roles[5].name", "$.tracks[0].strip.clip", "$.tracks[1].node", "$.tracks[1]",
                "$.tracks[2].cues[0].time", "$.tracks[2].cues[0].volume", "$.tracks[2].cues[0].pitch", "$.tracks[2].cues[1].name",
                "$.tracks[3].actor", "$.tracks[3].strips[0].repeat", "$.tracks[3].cues[0].parameter",
                "$.reduced[0].to", "$.reduced[1]",
            ],
            SequenceChecks.Of(sequence).Select(p => p.Path.ToString()));
        // With the stage given, the empty clip isn't looked for in it.
        Assert.DoesNotContain(SequenceChecks.Of(sequence, new Dictionary<string, ModelData> { [TestSequence.StagePath] = TestSequence.Stage() }),
            p => p.Message.Contains("has no clip", StringComparison.Ordinal));
    }

    /// <summary>
    /// A stage not given doesn't stop the check at its tracks: a clip track, a node and a cue at a marker of it are left
    /// to that problem (with the stages given, the first problem is the stage, as <see cref="Sequence.Validate(IReadOnlyDictionary{string, ModelData})"/> throws it).
    /// </summary>
    [Fact]
    public void AStageNotGivenLeavesItsNamesAlone()
    {
        var sequence = Valid() with
        {
            Stages = [TestSequence.StagePath, "models/stages/arena"],
            Tracks =
            [
                new ClipTrack { Stage = "hierarchy", Strip = new("base") }, new ClipTrack { Stage = "arena", Strip = new("sweep") },
                new VisibilityTrack { Node = "arena/door", Cues = [new(0, true, "arena/open")] },
            ],
        };
        var stages = new Dictionary<string, ModelData> { [TestSequence.StagePath] = TestSequence.Stage() };
        Assert.Equal(["$.stages[1]: the sequence valid: its stage models/stages/arena isn't given"],
            SequenceChecks.Of(sequence, stages).Select(p => $"{p.Path}: {p.Message}"));
        Assert.Equal("the sequence valid: its stage models/stages/arena isn't given", Assert.Throws<ArgumentException>(() => sequence.Validate(stages)).Message);
    }

    /// <summary>The test stage (<see cref="TestSequence.Stage"/>) at its path in a pack of a temporary folder, mounted.</summary>
    internal sealed class StageData : IDisposable
    {
        readonly TempFolder _folder = new();

        public StageData(Action<PackWriter>? add = null)
        {
            var writer = new PackWriter();
            writer.AddRecord(TestSequence.StagePath, TestSequence.Stage());
            add?.Invoke(writer);
            writer.Write(_folder["models.pak"]);
            Files = new VirtualFileSystem([Mount.Pack(_folder["models.pak"])]);
        }

        public VirtualFileSystem Files { get; }

        public string Pack => _folder["models.pak"];

        public void Dispose()
        {
            Files.Dispose();
            _folder.Dispose();
        }
    }

    /// <summary>A stage's node its scene doesn't reach (no root above it) is no node of the stage: it is never instanced.</summary>
    [Fact]
    public void ANodeTheStagesSceneDoesntReachIsRefused()
    {
        var stage = TestSequence.Stage();
        var squash = stage.Nodes.ToList().FindIndex(n => n.Name == "Squash");
        typeof(ModelData).GetProperty(nameof(ModelData.Roots))!.SetValue(stage, stage.Roots.Where(r => r != squash).ToArray());
        var sequence = With(new VisibilityTrack { Node = "hierarchy/Tilted", Cues = [new(0, false)] });
        sequence.Validate(new Dictionary<string, ModelData> { [TestSequence.StagePath] = TestSequence.Stage() });
        Assert.Contains("its node hierarchy/Tilted: the stage models/stages/hierarchy has no node Tilted",
            Assert.Throws<ArgumentException>(() => sequence.Validate(new Dictionary<string, ModelData> { [TestSequence.StagePath] = stage })).Message);
    }

    /// <summary>A strip of the record is an <see cref="AnimationStrip"/> with its settings, its start on the stack's clock, a part without an end the clip's whole length.</summary>
    [Fact]
    public void AStripIsAnAnimationStripOnTheClock()
    {
        var model = TestSequence.Stage();
        var skeleton = new Skeleton(model);
        var clip = new ClipBinding(AnimationClip.AllOf(model).Single(c => c.Name == "base"), skeleton);
        var strip = new SequenceStrip("base", 0.5, 0.25, 0.75, 2, 3, true, 0.1, 0.2, 0.6, BlendMode.Combine, Extrapolation.Hold).ToStrip(clip, 10);
        Assert.Equal((10.5, 0.25, 0.75, 2, 3, true, 0.1, 0.2, (double?)0.6, BlendMode.Combine, Extrapolation.Hold),
            (strip.Start, strip.ClipStart, strip.ClipEnd, strip.Scale, strip.Repeat, strip.Reversed, strip.BlendIn, strip.BlendOut, strip.Influence, strip.Mode, strip.Extrapolation));
        Assert.Same(clip, strip.Clip);
        var whole = new SequenceStrip("base").ToStrip(clip, 0);
        Assert.Equal((0, 0, (double)clip.Clip.Duration, 1, 1, false, 0, 0, (double?)null, BlendMode.Replace, Extrapolation.HoldForward),
            (whole.Start, whole.ClipStart, whole.ClipEnd, whole.Scale, whole.Repeat, whole.Reversed, whole.BlendIn, whole.BlendOut, whole.Influence, whole.Mode, whole.Extrapolation));
    }

    /// <summary>
    /// A marker's time on the timeline (S4-2) is where the stage's first clip track reaches it: its start
    /// plus the marker's time into the strip's part times its scale (reversed: counted from the part's
    /// end), plus the cue's offset; the stage's later clip tracks don't move it.
    /// </summary>
    [Theory]
    [InlineData(0, 0, null, 1, false, 0, 0.4)]
    [InlineData(1, 0.2, null, 2, false, 0, 1.4)]
    [InlineData(1, 0.2, null, 2, false, 0.1, 1.5)]
    [InlineData(1, 0, null, 2, true, 0, 2.2)]
    [InlineData(1, 0, 0.5, 2, true, -0.25, 0.95)]
    public void MarkersArePlacedByTheStagesClipTrack(double start, double clipStart, double? clipEnd, double scale, bool reversed, double offset, double expected)
    {
        var sequence = Valid() with
        {
            Duration = 4,
            Tracks =
            [
                new ClipTrack { Stage = "hierarchy", Strip = new("base", start, clipStart, clipEnd, scale, Reversed: reversed) },
                new ClipTrack { Stage = "hierarchy", Strip = new("base", 2) },
            ],
        };
        var times = new SequenceTimes(sequence, new Dictionary<string, ModelData> { [TestSequence.StagePath] = TestSequence.Stage() });
        Assert.Equal(expected, times.Resolve(offset, "hierarchy/open"), 12);
        Assert.Equal(offset, times.Resolve(offset, null));
    }

    /// <summary>
    /// A sequence that breaks no rule but whose curve does: the record's binary and JSON forms made past
    /// the curve's checks (its second key moved onto the first).
    /// </summary>
    static Sequence WithBrokenCurve()
    {
        var keys = new[] { FCurveKey.Linear(0, 0), FCurveKey.Linear(0, 1) };
        var curve = (FCurve)RuntimeHelpers.GetUninitializedObject(typeof(FCurve));
        typeof(FCurve).GetField("_keys", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(curve, keys);
        return With(new TransformTrack { Node = "pivot", LocationX = curve });
    }

    /// <summary>
    /// <see cref="Sequence.Load"/> reads a sequence from a pack and from a loose folder with its stage from
    /// the same data; a broken one is a <see cref="DataFileException"/> naming the pack and entry or the
    /// file: a curve that breaks its rules, a rule of the sequence, a name its stage doesn't have, a stage
    /// the data doesn't have (each with the exception inside), a sound the data doesn't have (M11 step 6). The
    /// data holds the test sequence's particle effect and sounds, and the sound pack for its theme.
    /// </summary>
    [Fact]
    public void SequencesLoadFromPacksAndLooseFilesAndBrokenOnesAreRefusedNamingTheFile()
    {
        var stage = TestSequence.Stage();
        var patch = JiruanIntro.SoundPatches()[JiruanIntro.FlashSound];
        var broken = new Dictionary<string, (Sequence Sequence, string Problem, Type? Inner)>
        {
            ["world/cutscenes/sound"] = (With(new AudioTrack { Cues = [new(0, AudioCueKind.Effect, "boom")] }),
                "not a valid sequence: the sequence valid, track 1 (audio): cue 0 plays the effect boom, which isn't in the game's data (audio/effects/boom)", null),
            // The names in its stages come before the effects and sounds.
            ["world/cutscenes/name-and-sound"] = (With(new AudioTrack { Cues = [new(0, AudioCueKind.Effect, "boom")] }, new VisibilityTrack { Node = "hierarchy/Nope", Cues = [new(0, true)] }),
                "not a valid sequence: the sequence valid, track 2 (visibility): its node hierarchy/Nope: the stage models/stages/hierarchy has no node Nope", typeof(ArgumentException)),
            ["world/cutscenes/curve"] = (WithBrokenCurve(), "not a valid sequence: key 1 (at 0 s): keys must be in increasing time order at least 0.01 frame (1/6000 s) apart, and key 0 is at 0 s", typeof(ArgumentException)),
            ["world/cutscenes/rule"] = (Valid() with { Duration = -1 }, "not a valid sequence: the sequence valid: its duration (-1 s) must be a finite number above 0", typeof(ArgumentException)),
            ["world/cutscenes/name"] = (With(new VisibilityTrack { Node = "hierarchy/Nope", Cues = [new(0, true)] }),
                "not a valid sequence: the sequence valid, track 1 (visibility): its node hierarchy/Nope: the stage models/stages/hierarchy has no node Nope", typeof(ArgumentException)),
            ["world/cutscenes/path"] = (Valid() with { Stages = ["models/hierarchy"], Tracks = [] }, "not a valid sequence: the sequence valid: the stage models/hierarchy isn't a path models/stages/<name>", typeof(ArgumentException)),
            ["world/cutscenes/stage"] = (Valid() with { Stages = ["models/stages/arena"], Tracks = [] }, "not a valid sequence: its stage models/stages/arena isn't in the game's data", typeof(FileNotFoundException)),
        };
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("world/cutscenes/test", TestSequence.Build(stage));
        foreach (var (path, (sequence, _, _)) in broken)
            writer.AddRecord(path, sequence);
        writer.Write(folder["world.pak"]);
        var models = new PackWriter();
        models.AddRecord(TestSequence.StagePath, stage);
        models.AddRecord(TestSequence.EffectPath, TestSequence.Effect);
        models.AddRecord("audio/effects/chime", patch);
        models.AddRecord("audio/effects/thud", patch);
        models.Write(folder["models.pak"]);
        using (var packed = new VirtualFileSystem([Mount.Pack(JiruanIntro.SoundPack), Mount.Pack(folder["models.pak"]), Mount.Pack(folder["world.pak"])]))
        {
            RecordAssert.Equal(TestSequence.Build(stage), Sequence.Load(packed, "world/cutscenes/test"));
            foreach (var (path, (_, expected, inner)) in broken)
            {
                var problem = Assert.Throws<DataFileException>(() => Sequence.Load(packed, path));
                Assert.Equal((Path.GetFullPath(folder["world.pak"]), path, expected), (problem.File, problem.Entry, problem.Problem));
                if (inner == null)
                    Assert.Null(problem.InnerException);
                else
                    Assert.IsType(inner, problem.InnerException);
            }
        }
        folder.Write("dev/world/cutscenes/test.json", RecordSerializer.ToJson(TestSequence.Build(stage)));
        var file = folder.Write("dev/world/cutscenes/curve.json", RecordSerializer.ToJson(WithBrokenCurve()));
        folder.Write("dev/models/stages/hierarchy.json", RecordSerializer.ToJson(stage));
        folder.Write("dev/models/effects/sparks.json", RecordSerializer.ToJson(TestSequence.Effect));
        folder.Write("dev/audio/effects/chime.json", RecordSerializer.ToJson(patch));
        folder.Write("dev/audio/effects/thud.json", RecordSerializer.ToJson(patch));
        using var loose = new VirtualFileSystem([Mount.Pack(JiruanIntro.SoundPack), Mount.Folder(folder["dev"])]);
        RecordAssert.Equal(TestSequence.Build(stage), Sequence.Load(loose, "world/cutscenes/test"));
        var looseProblem = Assert.Throws<DataFileException>(() => Sequence.Load(loose, "world/cutscenes/curve"));
        Assert.Equal((Path.GetFullPath(file), (string?)null, broken["world/cutscenes/curve"].Problem), (looseProblem.File, looseProblem.Entry, looseProblem.Problem));
    }

    /// <summary>A broken stage's own error names the stage's entry, not the sequence's.</summary>
    [Fact]
    public void ABrokenStageIsNamedByItsOwnEntry()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("world/cutscenes/test", TestSequence.Build());
        writer.Add(TestSequence.StagePath, [1, 2, 3]);
        writer.Write(folder["world.pak"]);
        using var packed = new VirtualFileSystem([Mount.Pack(folder["world.pak"])]);
        var problem = Assert.Throws<DataFileException>(() => Sequence.Load(packed, "world/cutscenes/test"));
        Assert.Equal(TestSequence.StagePath, problem.Entry);
    }
}
