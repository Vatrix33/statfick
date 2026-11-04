using Statventure.Animation;
using Statventure.Scene;
using Statventure.Tests.Records;

namespace Statventure.Tests.Scene;

/// <summary>
/// The sequence builder (Docs/Scene.md, "The builder"): each method makes its record (every track kind,
/// cue kind and flag), and <see cref="SequenceBuilder.Build"/> checks the sequence by itself and against
/// the stage models it was given.
/// </summary>
public class SequenceBuilderTests
{
    [Fact]
    public void EachMethodMakesItsRecord()
    {
        var stage = TestSequence.Stage();
        var curve = new FCurve([FCurveKey.Linear(0, 1)]);
        var other = new FCurve([FCurveKey.Linear(0, 2)]);
        var extra = new EventTrack { Cues = [new(1, "extra")] };
        var built = new SequenceBuilder("all", 2).Unskippable().NonBlocking()
            .Role("hero", RoleKind.Actor).Role("pivot", RoleKind.Node).Role("card", RoleKind.Element).Role("tint", RoleKind.Palette)
            .Stage(TestSequence.StagePath, stage)
            .Clip("hierarchy", "base", start: 0.5, repeat: 2)
            .Clip("hierarchy", new SequenceStrip("rig", Reversed: true))
            .Transform("pivot", t => t.Location(curve, null, other).Rotation(null, curve).Scale(z: other))
            .Visibility("pivot", v => v.Show(0.25).Hide(CueTime.At("hierarchy/open", 0.1)))
            .Animation("hero", "hierarchy/Tip", a => a.Strip("swing", 0.25, loop: true, blendIn: 0.1, blendOut: 0.2, mode: BlendMode.Combine).Strip(new SequenceStrip("idle", 3))
                .Bool(0, "dead", true).Int(0.5, "combo", 3).Float(1, "speed", 0.5).Trigger(CueTime.At("hierarchy/reveal"), "attack"))
            .Palette("hero", p => p.Fill(0, "#000000").Role(0.5, "tint").Own(1))
            .Events(e => e.Event(2, "end").Event(CueTime.At("hierarchy/open"), "open"))
            .Camera(c => c.Cut(0, "hierarchy/Base").FieldOfView(curve))
            .Light(l => l.Sun(0.5, "hierarchy/Post").Strength(curve).Angle(other))
            .Particles(p => p.Emit(0.5, "models/effects/mist", "pivot", 3, 4, 5, 6, "tint"))
            .Audio(a => a.Effect(0, "hit", 0.5, -0.5, 2).Music(0.5, "theme").StopMusic(1))
            .Ui("card", u => u.Text(0, "title", "Hi").AddClass(0, "on").RemoveClass(1, "on", "title").SetAttribute(0, "hidden", "", "title").RemoveAttribute(1, "hidden"))
            .Track(extra)
            .Reduced(r => r.Span(0, 0.5).Hold(1, 2).Span(1.5, 2, 0.25))
            .Build();
        var expected = new Sequence
        {
            Name = "all",
            Duration = 2,
            Skippable = false,
            Blocking = false,
            Roles = [new("hero", RoleKind.Actor), new("pivot", RoleKind.Node), new("card", RoleKind.Element), new("tint", RoleKind.Palette)],
            Stages = [TestSequence.StagePath],
            Tracks =
            [
                new ClipTrack { Stage = "hierarchy", Strip = new SequenceStrip("base", 0.5, Repeat: 2) },
                new ClipTrack { Stage = "hierarchy", Strip = new SequenceStrip("rig", Reversed: true) },
                new TransformTrack { Node = "pivot", LocationX = curve, LocationZ = other, RotationY = curve, ScaleZ = other },
                new VisibilityTrack { Node = "pivot", Cues = [new(0.25, true), new(0.1, false, "hierarchy/open")] },
                new ActorTrack
                {
                    Actor = "hero", Mark = "hierarchy/Tip",
                    Strips = [new SequenceStrip("swing", 0.25, Repeat: double.PositiveInfinity, BlendIn: 0.1, BlendOut: 0.2, Mode: BlendMode.Combine), new SequenceStrip("idle", 3)],
                    Cues = [new(0, "dead", ParameterKind.Bool, 1), new(0.5, "combo", ParameterKind.Int, 3), new(1, "speed", ParameterKind.Float, 0.5), new(0, "attack", ParameterKind.Trigger, 1, "hierarchy/reveal")],
                },
                new PaletteTrack { Target = "hero", Cues = [new(0, null, "#000000"), new(0.5, "tint"), new(1)] },
                new EventTrack { Cues = [new(2, "end"), new(0, "open", "hierarchy/open")] },
                new CameraTrack { Cuts = [new(0, "hierarchy/Base")], FieldOfView = curve },
                new LightTrack { Cues = [new(0.5, "hierarchy/Post")], Strength = curve, Angle = other },
                new ParticleTrack { Cues = [new(0.5, "models/effects/mist", "pivot", 3, 4, 5, 6, "tint")] },
                new AudioTrack { Cues = [new(0, AudioCueKind.Effect, "hit", 0.5, -0.5, 2), new(0.5, AudioCueKind.Music, "theme"), new(1, AudioCueKind.StopMusic)] },
                new UiTrack
                {
                    Host = "card",
                    Cues =
                    [
                        new(0, UiCueAction.SetText, "title", null, "Hi"), new(0, UiCueAction.AddClass, null, "on"), new(1, UiCueAction.RemoveClass, "title", "on"),
                        new(0, UiCueAction.SetAttribute, "title", "hidden", ""), new(1, UiCueAction.RemoveAttribute, null, "hidden"),
                    ],
                },
                extra,
            ],
            Reduced = [new(0, 0.5), new(1, 1, 2), new(1.5, 2, 0.25)],
        };
        RecordAssert.Equal(expected, built);
        var defaults = new SequenceBuilder("plain", 1).Build();
        Assert.Equal((true, true, null, 0, 0), (defaults.Skippable, defaults.Blocking, defaults.Reduced, defaults.Roles.Count, defaults.Tracks.Count));
    }

    /// <summary>Build checks the sequence alone (a cue outside the timeline) and against the stages it was given (a node, a clip, a marker they don't have).</summary>
    [Theory]
    [InlineData("time", "track 0 (event): cue 0 at 3 s is outside the timeline (0–2 s)")]
    [InlineData("node", "track 0 (visibility): its node hierarchy/Gate: the stage models/stages/hierarchy has no node Gate")]
    [InlineData("clip", "track 0 (clip): the stage models/stages/hierarchy has no clip sweep")]
    [InlineData("marker", "track 1 (event): cue 0's marker hierarchy/close: the stage models/stages/hierarchy's clip base has no marker close")]
    public void BuildChecksTheSequenceAndItsStages(string label, string problem)
    {
        var builder = new SequenceBuilder("bad", 2).Stage(TestSequence.StagePath, TestSequence.Stage());
        _ = label switch
        {
            "time" => builder.Events(e => e.Event(3, "late")),
            "node" => builder.Visibility("hierarchy/Gate", v => v.Hide(0)),
            "clip" => builder.Clip("hierarchy", "sweep"),
            _ => builder.Clip("hierarchy", "base").Events(e => e.Event(CueTime.At("hierarchy/close"), "close")),
        };
        Assert.Equal($"the sequence bad, {problem}", Assert.Throws<ArgumentException>(builder.Build).Message);
    }
}
