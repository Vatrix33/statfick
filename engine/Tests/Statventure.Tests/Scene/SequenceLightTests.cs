using System.Numerics;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Animation;

namespace Statventure.Tests.Scene;

/// <summary>
/// The light track (Docs/Scene.md, "How it works", step 5; Q12, S5-5): sun cues make the sun on the
/// named node the scene's, the stage's imported sun (S3-5) or a bound node's, the view's sun then
/// Blender's (its direction toward it, strength and angle as the stage's evaluation gives them); from
/// the first cue on the strength and angle curves set the sun cued every frame, a sun cued away from
/// gets its own back; before the first cue and after a seek back the game's sun; the end state stays,
/// a skip ends there as watching; a cue naming a node without a sun is refused, changing nothing.
/// </summary>
public class SequenceLightTests
{
    static SceneSun StageSun(SequencePlayer player) => StageBench.Node(player, "Sun").Attachments.OfType<SceneSun>().Single();

    /// <summary>Cued to the stage's sun at 0.25 s without curves: the view's sun is Blender's at every one of the golden's times from then on.</summary>
    [Fact]
    public void ASunCueGivesTheImportedSunAsBlenderEvaluatesIt()
    {
        var bench = new StageBench();
        var player = bench.Play(StageBench.Build("sun", b => b.Light(l => l.Sun(0.25, "dolly/Sun"))));
        bench.Frame(0);
        var suns = StageBench.Golden.GetProperty("suns"u8).GetProperty("Sun");
        var checkedTimes = 0;
        for (var t = 0; t < StageBench.Times.Length; t++)
        {
            var time = StageBench.Times[t];
            if (time < 0 || time > 1)
                continue;
            bench.At(player, time);
            if (time < 0.25)
            {
                Assert.Same(bench.GameSun, bench.Scene.Sun);
                Assert.Equal(bench.GameSun.Light, bench.View.Sun);
                continue;
            }
            Assert.Same(StageSun(player), bench.Scene.Sun);
            var toward = suns.GetProperty("direction"u8)[t].EnumerateArray().Select(v => v.GetSingle()).ToArray();
            Assert.True(Vector3.Distance(new Vector3(toward[0], toward[1], toward[2]), bench.View.Sun.Direction) <= AnimationReferenceTests.OnFrameTolerance);
            Assert.Equal(suns.GetProperty("strength"u8)[t].GetDouble(), bench.View.Sun.Strength, 2e-6);
            Assert.Equal(suns.GetProperty("angle"u8)[t].GetDouble(), bench.View.Sun.Angle, 2e-6);
            checkedTimes++;
        }
        Assert.True(checkedTimes >= 70);
    }

    /// <summary>
    /// The strength and angle curves (S5-5): nothing before the first cue; from it on the sun cued takes
    /// both curves' values every frame (its direction still Blender's); cued away to a bound node's sun,
    /// the stage's has its own back and the other takes them; a seek back before the first cue gives the
    /// game's sun back and every sun its own; at the end the last keeps the curves' end values. A track
    /// with only a strength curve leaves the angle alone.
    /// </summary>
    [Fact]
    public void TheCurvesSetTheSunCuedFromTheFirstCueOn()
    {
        var bench = new StageBench();
        var strength = new FCurve([FCurveKey.Linear(0, 1), FCurveKey.Linear(1, 5)]);
        var angle = new FCurve([FCurveKey.Linear(0, 2), FCurveKey.Linear(1, 10)]);
        var player = bench.Play(StageBench.Build("light", b => b.Light(l => l.Sun(0.25, "dolly/Sun").Sun(0.75, "lens").Strength(strength).Angle(angle))));
        var sun = StageSun(player);
        Assert.Equal((2.5f, 4f), (sun.Strength, sun.Angle));
        bench.Frame(0);
        bench.At(player, 0.125);
        Assert.Equal((bench.GameSun, 1.5f, 2f, 2.5f, 4f), (bench.Scene.Sun, bench.GameSun.Strength, bench.GameSun.Angle, sun.Strength, sun.Angle));
        foreach (var tau in new[] { 0.25, 0.4, 0.6 })
        {
            bench.At(player, tau);
            Assert.Same(sun, bench.Scene.Sun);
            Assert.Equal(((float)strength.Evaluate(tau), (float)angle.Evaluate(tau)), (sun.Strength, sun.Angle));
            Assert.Equal(new SunLight(sun.Light.Direction, (float)strength.Evaluate(tau)) { Angle = (float)angle.Evaluate(tau) }, bench.View.Sun);
        }
        Assert.Equal((5f, 1f), (bench.LensSun.Strength, bench.LensSun.Angle));
        bench.At(player, 0.8);
        Assert.Same(bench.LensSun, bench.Scene.Sun);
        Assert.Equal((2.5f, 4f), (sun.Strength, sun.Angle));
        Assert.Equal(((float)strength.Evaluate(0.8), (float)angle.Evaluate(0.8)), (bench.LensSun.Strength, bench.LensSun.Angle));
        Assert.Equal(bench.LensSun.Light, bench.View.Sun);
        bench.At(player, 0.5);
        Assert.Same(sun, bench.Scene.Sun);
        Assert.Equal((5f, 1f), (bench.LensSun.Strength, bench.LensSun.Angle));
        bench.At(player, 0);
        Assert.Same(bench.GameSun, bench.Scene.Sun);
        Assert.Equal((2.5f, 4f, 1.5f, 2f), (sun.Strength, sun.Angle, bench.GameSun.Strength, bench.GameSun.Angle));
        for (var frames = 0; !player.Ended; frames++)
        {
            Assert.True(frames < 100);
            bench.Frame(1 / 30.0);
        }
        Assert.Same(bench.LensSun, bench.Scene.Sun);
        Assert.Equal((5f, 10f), (bench.LensSun.Strength, bench.LensSun.Angle));

        // A track with one of the curves sets and gives back that one only: the game's change to the other stands.
        foreach (var strengthOnly in new[] { true, false })
        {
            var only = new StageBench();
            var played = only.Play(StageBench.Build("one", b => b.Light(l =>
            {
                l.Sun(0, "dolly/Sun").Sun(0.75, "lens");
                if (strengthOnly)
                    l.Strength(strength);
                else
                    l.Angle(angle);
            })));
            only.Frame(0);
            only.At(played, 0.5);
            var stage = StageSun(played);
            Assert.Equal(strengthOnly ? (3f, 4f) : (2.5f, 6f), (stage.Strength, stage.Angle));
            if (strengthOnly)
                stage.Angle = 7;
            else
                stage.Strength = 7;
            only.At(played, 0.8);
            Assert.Equal(strengthOnly ? (2.5f, 7f) : (7f, 4f), (stage.Strength, stage.Angle));
        }
    }

    /// <summary>Sun cues apply when the cue in effect changes (S4-8): the game's sun set between cues stands until the next.</summary>
    [Fact]
    public void SunCuesApplyWhenTheCueInEffectChanges()
    {
        var bench = new StageBench();
        var player = bench.Play(StageBench.Build("changes", b => b.Light(l => l.Sun(0.25, "dolly/Sun").Sun(0.75, "lens"))));
        bench.Frame(0);
        bench.Frame(0.3);
        Assert.Same(StageSun(player), bench.Scene.Sun);
        bench.Scene.Sun = bench.GameSun;
        bench.Frame(0.2);
        Assert.Same(bench.GameSun, bench.Scene.Sun);
        // A seek applies the cue in effect again, the same one too.
        bench.Scene.Sun = bench.GameSun;
        bench.At(player, 0.9);
        Assert.Same(bench.LensSun, bench.Scene.Sun);
        bench.Frame(0.05);
        Assert.Same(bench.LensSun, bench.Scene.Sun);
    }

    /// <summary>A skip ends on the last cue's sun at the curves' end values, as watching to the end does.</summary>
    [Fact]
    public void ASkipEndsOnTheLastSunAtTheCurvesEndAsWatching()
    {
        var strength = new FCurve([FCurveKey.Linear(0, 1), FCurveKey.Linear(1, 4)]);
        var sequence = StageBench.Build("skip", b => b.Light(l => l.Sun(0, "lens").Sun(0.5, "dolly/Sun").Strength(strength)));
        (float, float, SunLight) EndOf(bool skip)
        {
            var bench = new StageBench();
            var player = bench.Play(sequence);
            bench.Frame(0);
            bench.Frame(0.1);
            if (skip)
                Assert.True(player.Skip());
            else
                for (var frames = 0; !player.Ended; frames++)
                {
                    Assert.True(frames < 100);
                    bench.Frame(0.1);
                }
            bench.Scene.Refresh();
            Assert.Same(StageSun(player), bench.Scene.Sun);
            Assert.Equal(5f, bench.LensSun.Strength);
            return (StageSun(player).Strength, StageSun(player).Angle, bench.View.Sun);
        }
        var skipped = EndOf(skip: true);
        Assert.Equal((4f, 4f), (skipped.Item1, skipped.Item2));
        Assert.Equal(EndOf(skip: false), skipped);
    }

    public static TheoryData<string> BadCues => [.. Bad.Keys];

    static readonly Dictionary<string, (Func<Sequence> Sequence, string Problem)> Bad = new()
    {
        ["stage node"] = (() => StageBench.Build("bad", b => b.Light(l => l.Sun(0, "dolly/Sun").Sun(0.5, "dolly/Camera"))),
            "the sequence bad, track 1 (light): cue 1's sun dolly/Camera: the stage models/stages/dolly's node Camera carries no sun"),
        ["bound node"] = (() => StageBench.Build("bad", b => b.Role("bare", RoleKind.Node).Light(l => l.Sun(0, "bare"))),
            "the sequence bad, track 1 (light): cue 0's sun bare: the node carries no sun"),
    };

    /// <summary>A cue naming a node that carries no sun is refused at play (S4-6), naming the cue and the node, and nothing changes.</summary>
    [Theory]
    [MemberData(nameof(BadCues))]
    public void ACueToANodeWithoutASunIsRefused(string name)
    {
        var bench = new StageBench();
        var (sequence, problem) = Bad[name];
        var bindings = bench.Bindings();
        var bare = bench.Scene.Root.Add("bare");
        bare.Attach(new SceneCamera());
        if (sequence().Roles.Any(r => r.Name == "bare"))
            bindings.Node("bare", bare);
        var children = bench.Scene.Root.Children.ToArray();
        Assert.Equal(problem, Assert.Throws<ArgumentException>(() => bench.Play(sequence(), bindings)).Message);
        Assert.Equal(children, bench.Scene.Root.Children);
        Assert.Empty(bench.Scene.Playing);
        Assert.Same(bench.GameSun, bench.Scene.Sun);
    }
}
