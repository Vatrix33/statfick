using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using static Statventure.Tests.Scene.SequenceBench;

namespace Statventure.Tests.Scene;

/// <summary>
/// Each step-4 track kind's playback (Docs/Scene.md, "How it works", step 4): the stage's clips on its
/// nodes, a transform track's curves and the channels it shares with a clip (S4-4), Euler XYZ as
/// Blender's, visibility and palette cues applied when the cue in effect changes (S4-8, S4-3), the
/// figure on its mark with its strips and machine cues (Q16), and a stage's clips outside a sequence on
/// the scene's battle pace (S4-7).
/// </summary>
[Collection("GPU")]
public class SequenceTrackTests(GpuFixture gpu)
{
    const double Step = 0.125;

    /// <summary>Plays the bench's sequence and frames until τ is <paramref name="tau"/> (whole steps from 0).</summary>
    static SequencePlayer PlayTo(SequenceBench bench, double tau, Sequence? sequence = null, SequenceBindings? bindings = null)
    {
        var player = bindings == null ? bench.Play(sequence) : bench.Scene.Play(sequence!, bindings, bench.Document);
        bench.Frame(Step);
        bench.FrameUntil(player, tau - 1e-9);
        return player;
    }

    /// <summary>The stage's nodes as a stack of its clips gives them at <paramref name="tau"/>: a stage instanced apart, the same clips applied to it.</summary>
    static ModelInstance Reference(ModelData stage, double tau, params SequenceStrip[] strips)
    {
        var instance = new SceneNode("apart").Instance(stage);
        var stack = new AnimationStack(instance.Skeleton);
        foreach (var strip in strips)
        {
            var layer = new AnimationLayer();
            layer.Strips.Add(strip.ToStrip(instance.Clip(strip.Clip), 0));
            stack.Layers.Add(layer);
        }
        instance.Apply(stack, tau);
        return instance;
    }

    /// <summary>A stage's clip tracks play as one NLA stack on its nodes, a layer each in track order (Q13): its nodes as the stack applied to the model gives them.</summary>
    [Fact]
    public void ClipTracksPlayTheStagesClipsOnItsNodes()
    {
        using var bench = new SequenceBench(gpu);
        var player = PlayTo(bench, 0.75);
        var reference = Reference(bench.StageData, 0.75, new SequenceStrip("base"), new SequenceStrip("rig", 0.5));
        foreach (var name in new[] { "Base", "Post", "Tip", "hand", "Squash" })
            Assert.Equal(Local(reference.NodeNamed(name)!), Local(StageNode(player, name)));
        Assert.Equal(Local(reference.NodeNamed("arm")!).Item2, StageNode(player, "arm").Rotation);
        Assert.NotEqual(Local(new SceneNode().Instance(bench.StageData).NodeNamed("Base")!), Local(StageNode(player, "Base")));

        // Two clip tracks on one node: the later layer is on top (Replace at full influence: it wins).
        using var layered = new SequenceBench(gpu);
        var sequence = new SequenceBuilder("layers", 2).Stage(TestSequence.StagePath, layered.StageData)
            .Clip("hierarchy", "base").Clip("hierarchy", new SequenceStrip("base", 0.5, Extrapolation: Extrapolation.Nothing)).Build();
        var second = PlayTo(layered, 0.75, sequence, new SequenceBindings().Stage(TestSequence.StagePath, layered.StageData));
        var top = Reference(layered.StageData, 0.25, new SequenceStrip("base"));
        Assert.Equal(Local(top.NodeNamed("Base")!), Local(StageNode(second, "Base")));
    }

    /// <summary>A stage given as a model is drawn: instanced from the model, as scenery (it takes the figures' shadows).</summary>
    [Fact]
    public void AStageGivenAsAModelIsDrawnAsScenery()
    {
        using var bench = new SequenceBench(gpu);
        using var model = Model.Load(gpu.Device, Path.Combine(Render3D.Render3DReferenceTests.ModelDirectory, "hierarchy.glb"));
        var sequence = new SequenceBuilder("drawn", 1).Stage(TestSequence.StagePath, model.Data).Clip("hierarchy", "base").Build();
        var player = PlayTo(bench, 0.25, sequence, new SequenceBindings().Stage(TestSequence.StagePath, model));
        Assert.Same(model, player.Stages[0].Model);
        Assert.True(player.Stages[0].Scenery);
        Assert.Equal("hierarchy", player.Stages[0].Node!.Name);
    }

    /// <summary>
    /// Track order decides channel by channel (S4-4): the transform track after the rig clip sets the
    /// bone's location (the clip keys its rotation only, which stays the clip's); a transform track
    /// before the clip keeps the channels the clip doesn't set and loses the ones it does.
    /// </summary>
    [Fact]
    public void ALaterTrackSetsTheChannelsItAnimatesAndAnEarlierOneKeepsTheOthers()
    {
        using var bench = new SequenceBench(gpu);
        var player = PlayTo(bench, 1);
        var rest = Local(new SceneNode().Instance(bench.StageData).NodeNamed("arm")!);
        var clip = Local(Reference(bench.StageData, 1, new SequenceStrip("base"), new SequenceStrip("rig", 0.5)).NodeNamed("arm")!);
        var arm = StageNode(player, "arm");
        Assert.Equal(new Vector3(rest.Item1.X, 1, rest.Item1.Z), arm.Translation);
        Assert.Equal(clip.Item2, arm.Rotation);
        Assert.Equal(rest.Item3, arm.Scale);

        // A stage's clip tracks apply at the place of its first: a transform track between two of them sets what it animates after the clips.
        using var between = new SequenceBench(gpu);
        var middle = new SequenceBuilder("between", 2).Stage(TestSequence.StagePath, between.StageData)
            .Clip("hierarchy", "base")
            .Transform("hierarchy/Base", t => t.Location(x: new FCurve([FCurveKey.Linear(0, 5)])))
            .Clip("hierarchy", "rig")
            .Build();
        var third = PlayTo(between, 0.5, middle, new SequenceBindings().Stage(TestSequence.StagePath, between.StageData));
        Assert.Equal(5, StageNode(third, "Base").Translation.X);

        using var before = new SequenceBench(gpu);
        var sequence = new SequenceBuilder("before", 2).Stage(TestSequence.StagePath, before.StageData)
            .Transform("hierarchy/arm", t => t.Location(x: new FCurve([FCurveKey.Linear(0, 2)])).Rotation(z: new FCurve([FCurveKey.Linear(0, 1)])))
            .Clip("hierarchy", "rig")
            .Build();
        var second = PlayTo(before, 0.5, sequence, new SequenceBindings().Stage(TestSequence.StagePath, before.StageData));
        var arm2 = StageNode(second, "arm");
        Assert.Equal(new Vector3(2, rest.Item1.Y, rest.Item1.Z), arm2.Translation);
        Assert.Equal(Local(Reference(before.StageData, 0.5, new SequenceStrip("rig")).NodeNamed("arm")!).Item2, arm2.Rotation);
    }

    /// <summary>
    /// A channel a clip set that no strip sets any more goes back to the node's rest, unless a track
    /// before the clip set it in that frame (its value stays); channels no strip set are left alone.
    /// </summary>
    [Fact]
    public void AChannelAClipStopsSettingGoesBackToItsRestUnlessAnEarlierTrackSetsIt()
    {
        using var bench = new SequenceBench(gpu);
        var sequence = new SequenceBuilder("rest", 2).Stage(TestSequence.StagePath, bench.StageData)
            .Transform("hierarchy/Base", t => t.Location(y: new FCurve([FCurveKey.Linear(0, 0), FCurveKey.Linear(2, 2)])))
            .Clip("hierarchy", new SequenceStrip("base", Extrapolation: Extrapolation.Nothing))
            .Build();
        var player = PlayTo(bench, 0.5, sequence, new SequenceBindings().Stage(TestSequence.StagePath, bench.StageData));
        var node = StageNode(player, "Base");
        var rest = Local(new SceneNode().Instance(bench.StageData).NodeNamed("Base")!);
        var clip = Local(Reference(bench.StageData, 0.5, new SequenceStrip("base")).NodeNamed("Base")!);
        Assert.Equal(clip, Local(node));
        bench.FrameUntil(player, 1.1);
        // The first frame after the strip's end: its rotation and scale go back to rest, the location stays the transform's.
        Assert.Equal((new Vector3(rest.Item1.X, 1.125f, rest.Item1.Z), rest.Item2, rest.Item3), Local(node));
        bench.Frame(Step);
        Assert.Equal((new Vector3(rest.Item1.X, 1.25f, rest.Item1.Z), rest.Item2, rest.Item3), Local(node));
    }

    /// <summary>
    /// A transform track's curves at τ (Q8, S4-4): a channel with a curve for any component is set whole,
    /// its other components the node's own when the sequence started (the rotation's as Euler XYZ); a
    /// channel without curves is left to the game.
    /// </summary>
    [Fact]
    public void ATransformTracksMissingComponentsAreTheNodesOwn()
    {
        using var bench = new SequenceBench(gpu);
        bench.Pivot.SetLocal(new(1, 2, 3), EulerXyz.ToQuaternion(0.2, 0.3, 0.4), new(2, 2, 2));
        var sequence = new SequenceBuilder("own", 2).Role("pivot", RoleKind.Node)
            .Transform("pivot", t => t.Location(x: new FCurve([FCurveKey.Linear(0, 0), FCurveKey.Linear(2, 4)])).Rotation(z: new FCurve([FCurveKey.Linear(0, 1), FCurveKey.Linear(2, 3)])))
            .Build();
        var other = bench.Scene.Root.Add("other");
        other.Scale = new(2, 3, 4);
        sequence = sequence with
        {
            Roles = [.. sequence.Roles, new SequenceRole("other", RoleKind.Node)],
            Tracks = [.. sequence.Tracks, new TransformTrack { Node = "other", ScaleY = new FCurve([FCurveKey.Linear(0, 1), FCurveKey.Linear(2, 5)]) }],
        };
        PlayTo(bench, 0.5, sequence, new SequenceBindings().Node("pivot", bench.Pivot).Node("other", other));
        Assert.Equal(new Vector3(2, 2, 4), other.Scale);
        Assert.Equal(new Vector3(1, 2, 3), bench.Pivot.Translation);
        var expected = EulerXyz.ToQuaternion(0.2, 0.3, 1.5);
        Assert.True(Math.Abs(Quaternion.Dot(expected, bench.Pivot.Rotation)) > 1 - 1e-6, $"{bench.Pivot.Rotation} != {expected}");
        Assert.Equal(new Vector3(2, 2, 2), bench.Pivot.Scale);
        bench.Pivot.Scale = new(3, 3, 3);
        bench.Frame(Step);
        Assert.Equal((new Vector3(1.25f, 2, 3), new Vector3(3, 3, 3)), (bench.Pivot.Translation, bench.Pivot.Scale));
    }

    /// <summary>Euler angles XYZ as Blender 5.2's mathutils gives them (<c>Euler.to_quaternion</c>, <c>Quaternion.to_euler('XYZ')</c>; probed headless), within float32 rounding.</summary>
    [Theory]
    [InlineData(0.3, -1.1, 2.0, 0.3897218704223633, 0.5037217140197754, -0.1720348447561264, 0.7515220046043396)]
    [InlineData(1.5707963, 0.25, -0.5, 0.657968282699585, 0.7015897035598755, -0.08815833926200867, -0.25899380445480347)]
    [InlineData(-2.9, 1.4, 0.1, 0.06008754298090935, -0.7621997594833374, 0.03958534449338913, 0.6433303952217102)]
    [InlineData(0.0, 1.5707963267948966, 0.7, 0.6642367839813232, -0.24246536195278168, 0.664236843585968, 0.24246536195278168)]
    public void EulerXyzToAQuaternionIsBlenders(double x, double y, double z, double w, double qx, double qy, double qz)
    {
        var q = EulerXyz.ToQuaternion(x, y, z);
        Assert.Equal((w, qx, qy, qz), (q.W, q.X, q.Y, q.Z), (a, b) => Math.Abs(a.Item1 - b.Item1) < 3e-7 && Math.Abs(a.Item2 - b.Item2) < 3e-7 && Math.Abs(a.Item3 - b.Item3) < 3e-7 && Math.Abs(a.Item4 - b.Item4) < 3e-7);
    }

    [Theory]
    [InlineData(0.9233804941177368, 0.10259783267974854, -0.3077934980392456, 0.20519566535949707, 0.07983001321554184, -0.6567251086235046, 0.4101274609565735)]
    [InlineData(0.21081852912902832, 0.7378648519515991, 0.10540926456451416, -0.632455587387085, 2.1293957233428955, 1.3595854043960571, -0.5585992336273193)]
    [InlineData(0.5, -0.5, 0.5, 0.5, -1.5707963705062866, 1.5707963705062866, 0.0)]
    [InlineData(0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 3.1415927410125732)]
    public void AQuaternionToEulerXyzIsBlenders(double w, double qx, double qy, double qz, double x, double y, double z)
    {
        var (ex, ey, ez) = EulerXyz.FromQuaternion(new Quaternion((float)qx, (float)qy, (float)qz, (float)w));
        Assert.Equal(x, ex, 1e-6);
        Assert.Equal(y, ey, 1e-6);
        Assert.Equal(z, ez, 1e-6);
    }

    /// <summary>
    /// Visibility cues (Q23, S4-8): the node is hidden or shown from a cue's time, set when the cue in
    /// effect changes, so the game's change in between stands until the next cue; before the first cue
    /// (also after seeking back) the node is as it was when the sequence started.
    /// </summary>
    [Fact]
    public void VisibilityCuesApplyWhenTheCueInEffectChanges()
    {
        using var bench = new SequenceBench(gpu);
        var sequence = new SequenceBuilder("shown", 2).Role("pivot", RoleKind.Node).Visibility("pivot", v => v.Hide(0.5).Show(1).Hide(1.5).Show(1.5 - 5e-7)).Build();
        bench.Pivot.Hidden = true;
        var player = PlayTo(bench, 0.25, sequence, new SequenceBindings().Node("pivot", bench.Pivot));
        Assert.True(bench.Pivot.Hidden);
        bench.Pivot.Hidden = false;
        bench.Frame(Step);
        Assert.False(bench.Pivot.Hidden);
        bench.Frame(Step);
        bench.Frame(Step);
        Assert.True(bench.Pivot.Hidden);
        bench.Pivot.Hidden = false;
        bench.Frame(Step);
        Assert.False(bench.Pivot.Hidden);
        bench.FrameUntil(player, 1);
        Assert.False(bench.Pivot.Hidden);
        bench.Pivot.Hidden = true;
        bench.Frame(Step);
        Assert.True(bench.Pivot.Hidden);
        // Cues within 10⁻⁶ s count as one moment: the later in record order is the one in effect.
        bench.FrameUntil(player, 1.5);
        Assert.False(bench.Pivot.Hidden);
        player.Seek(0.25);
        bench.Frame(0);
        Assert.True(bench.Pivot.Hidden);
        player.Seek(0.75);
        bench.Pivot.Hidden = false;
        bench.Frame(0);
        Assert.True(bench.Pivot.Hidden);
    }

    /// <summary>
    /// The figure (Q16): on its mark from the start, its strips on one layer on top of its stack placed
    /// on its clock, its parameters set by the cue in effect (before the first, its own), the game's
    /// change standing until the next cue, a trigger set when the clock passes it (its machine takes it
    /// in that frame), and its clock its start's plus τ.
    /// </summary>
    [Fact]
    public void TheFigureStandsOnItsMarkWithItsStripsOnTopAndItsMachineCued()
    {
        using var bench = new SequenceBench(gpu);
        bench.Frame(0.5);
        var machine = bench.Hero.Animator.Machine!;
        machine.SetInt("combo", 1);
        var origin = bench.Hero.Animator.Time;
        var layers = bench.Hero.Animator.Stack.Layers.Count;
        var player = bench.Play();
        Assert.Same(StageNode(player, "Tip"), bench.Hero.Node);
        var layer = Assert.Single(player.LayersOf(bench.Hero));
        Assert.Equal(layers + 1, bench.Hero.Animator.Stack.Layers.Count);
        Assert.Same(layer, bench.Hero.Animator.Stack.Layers[^1]);
        var strip = Assert.Single(layer.Strips);
        Assert.Equal(("swing", origin + 0.25, 0.1), (strip.Clip.Clip.Name, strip.Start, strip.BlendIn));
        Assert.Empty(player.LayersOf(bench.Bystander));
        bench.Frame(Step);
        Assert.Equal((1.5, 1, 0, 0), (machine.Get("swingSpeed"), machine.Get("combo"), machine.Get("dead"), machine.Get("attack")));
        bench.FrameUntil(player, 1);
        Assert.Equal("swing", machine.StateOf("action"));
        Assert.Equal(0, machine.Get("attack"));
        bench.Frame(2 * Step);
        Assert.Equal(2, machine.Get("combo"));
        machine.SetInt("combo", 5);
        bench.Frame(Step);
        Assert.Equal(5, machine.Get("combo"));
        // A seek applies the cue in effect again, the same one too.
        player.Seek(1.5);
        bench.Frame(0);
        Assert.Equal(2, machine.Get("combo"));
        bench.FrameUntil(player, 1.875);
        Assert.Equal((1, 2), (machine.Get("dead"), machine.Get("combo")));
        player.Seek(1);
        bench.Frame(0);
        Assert.Equal((0, 1), (machine.Get("dead"), machine.Get("combo")));
        Assert.Equal(origin + 1, bench.Hero.Animator.Time, 12);
    }

    /// <summary>
    /// Palette cues (Q24, S4-3): <c>fill</c> puts one colour on every slot of the figure's models, the
    /// target's own palette comes back at "own" (and before the first cue), a palette role gives the bound
    /// palette; a stage is a target too (every slot of its model).
    /// </summary>
    [Fact]
    public void PaletteCuesFillGiveTheBoundPaletteOrTheTargetsOwn()
    {
        using var bench = new SequenceBench(gpu);
        using var sword = Model.Load(gpu.Device, Path.Combine(Render3D.Render3DReferenceTests.ModelDirectory, "sword.glb"));
        bench.Hero.Items.Add(new ActorItem(sword, new Socket(bench.Hero.Animator.Stack.Skeleton, "socket.hand.R")));
        var own = new Palette().Set("skin", Colors.LinearFromHex("#c08060"));
        bench.Hero.Palette = own;
        var player = PlayTo(bench, 0.5);
        var fill = bench.Hero.Palette!;
        Assert.Equal(new[] { "cloth", "skin" }.Concat(sword.Data.Materials.Select(m => m.Slot)).Distinct().Order(), fill.Slots.Keys.Order());
        Assert.True(sword.Data.Materials.Count > 0);
        Assert.All(fill.Slots.Values, t => Assert.Equal(new PaletteTone(Colors.LinearFromHex("#000000")), t));
        Assert.Null(bench.Prop.Palette);
        // The game's change stands until the next cue; a seek applies the cue in effect again.
        bench.Hero.Palette = bench.Tint;
        bench.Frame(Step);
        Assert.Same(bench.Tint, bench.Hero.Palette);
        player.Seek(0.625);
        bench.Frame(0);
        Assert.Same(fill, bench.Hero.Palette);
        bench.FrameUntil(player, 0.875);
        Assert.Same(own, bench.Hero.Palette);
        bench.FrameUntil(player, 1.5);
        Assert.Same(bench.Tint, bench.Prop.Palette);
        player.Seek(0.5);
        bench.Frame(0);
        Assert.Same(fill, bench.Hero.Palette);
        Assert.Null(bench.Prop.Palette);

        using var stage = new SequenceBench(gpu);
        var sequence = new SequenceBuilder("stage", 1).Stage(TestSequence.StagePath, stage.StageData).Palette("hierarchy", p => p.Own(0.75).Fill(0.25, "#336699")).Build();
        var played = PlayTo(stage, 0.25, sequence, new SequenceBindings().Stage(TestSequence.StagePath, stage.StageData));
        var stageFill = played.Stages[0].Palette!;
        Assert.Equal(stage.StageData.Materials.Select(m => m.Slot).Distinct().Order(), stageFill.Slots.Keys.Order());
        Assert.All(stageFill.Slots.Values, t => Assert.Equal(new PaletteTone(Colors.LinearFromHex("#336699")), t));
        stage.FrameUntil(played, 0.75);
        Assert.Null(played.Stages[0].Palette);
    }

    /// <summary>
    /// A stage's clips outside a sequence (S4-7): <see cref="SceneView.Animate"/> plays a stack on an
    /// instanced model at the scene's battle pace, from the time given; null stops it, the nodes keeping
    /// their pose; a stack on another model's nodes is refused.
    /// </summary>
    [Fact]
    public void AnimatedModelsRunOnTheScenesBattlePace()
    {
        using var bench = new SequenceBench(gpu);
        var instance = bench.Scene.Root.Add("stage").Instance(bench.StageData);
        var stack = new AnimationStack(instance.Skeleton);
        stack.Add(instance.Clip("base"), 0);
        bench.Scene.Rate = 0.5;
        bench.Scene.Animate(instance, stack, 0.25);
        bench.Frame(Step);
        Assert.Equal(Local(Reference(bench.StageData, 0.25 + Step * 0.5, new SequenceStrip("base")).NodeNamed("Base")!), Local(instance.NodeNamed("Base")!));
        bench.Frame(Step);
        var pose = Local(instance.NodeNamed("Base")!);
        Assert.Equal(Local(Reference(bench.StageData, 0.25 + Step, new SequenceStrip("base")).NodeNamed("Base")!), pose);
        bench.Scene.Animate(instance, null);
        bench.Frame(Step);
        Assert.Equal(pose, Local(instance.NodeNamed("Base")!));
        var other = new SceneNode().Instance(bench.StageData);
        Assert.Throws<ArgumentException>(() => bench.Scene.Animate(instance, new AnimationStack(new Skeleton(bench.Figure.Data))));
        bench.Scene.Animate(instance, new AnimationStack(other.Skeleton));
    }
}
