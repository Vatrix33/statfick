using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Statventure.Tests.Sandbox;
using Xunit.Abstractions;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// The figure's ground (GM2-P22; Docs/Animation.md, "A clip's translations follow the body's size" and "A figure stands
/// by its soles"; Docs/Game/Hero.md, "How it works", "The figure's ground"): a clip keyed on one body plays on a body with
/// shorter or longer legs with its hips' place scaled by the legs' length (the clip's measure: the left leg's thigh, shin
/// and foot), exactly as keyed on the body it was keyed on; and a figure stands by its soles, the lowest point at rest of
/// its body and its garments.
/// </summary>
public class FigureGroundTests(ITestOutputHelper output)
{
    const double Fps = 60;
    static readonly string[] Leg = ["thigh.L", "shin.L", "foot.L"];
    static readonly string[] RightLeg = ["thigh.R", "shin.R", "foot.R"];

    static ModelData Body(string sex) => HumanoidFiles.Load($"bodies/human-{sex}-average");

    /// <summary>A joint's place at rest in model space.</summary>
    static Vector3 Rest(ModelData model, string joint) => model.WorldMatrices()[new Skeleton(model).Find(joint)].Translation;

    /// <summary>
    /// Every clip of the family's library and of its test library carries its measure, the left leg's hip joint, knee and
    /// ankle (library.py's MEASURE), with its length on the man, the body they are keyed on: hip joint to knee plus knee to
    /// ankle at rest. On the woman the chain is 0.968 of his; the right leg's is the left's to the micrometre on both
    /// bodies (they are symmetric, so the one leg measures both).
    /// </summary>
    [Fact]
    public void EveryClipIsMeasuredByTheLeftLegOfTheManItWasKeyedOn()
    {
        var man = Body("male");
        double Length(ModelData model, string[] chain) =>
            Vector3.Distance(Rest(model, chain[0]), Rest(model, chain[1])) + Vector3.Distance(Rest(model, chain[1]), Rest(model, chain[2]));
        foreach (var clips in new[] { HumanoidClipList.Clips(), AnimationClip.AllOf(HumanoidFiles.Load("test-clips")) })
            foreach (var clip in clips)
            {
                Assert.True(clip.Measure is { } m && m.Chain.SequenceEqual(Leg), $"{clip.Name} isn't measured by the left leg");
                Assert.True(Math.Abs(clip.Measure!.Length - Length(man, Leg)) < 1e-6, $"{clip.Name}: {clip.Measure.Length}");
            }
        Assert.Equal(0.81515, HumanoidClipList.Clip("idle").Measure!.Length, 4);
        foreach (var sex in new[] { "male", "female" })
        {
            var body = Body(sex);
            Assert.True(Math.Abs(Length(body, Leg) - Length(body, RightLeg)) < 1e-6, $"{sex}: the legs differ");
            Assert.Equal(Length(body, Leg), new Skeleton(body).ChainLength(Leg)!.Value, 6);
        }
        var ratio = new ClipBinding(HumanoidClipList.Clip("idle"), new Skeleton(Body("female"))).Scale;
        output.WriteLine($"the woman's legs: {ratio:F6} of the man's");
        Assert.Equal(Length(Body("female"), Leg) / Length(man, Leg), ratio, 6);
        Assert.InRange(ratio, 0.96, 0.97);
    }

    /// <summary>
    /// On the body the clips were keyed on nothing changes: the man's chain is the library's skeleton's to the bit, so
    /// every clip's scale there is exactly 1, and the hips' place it gives is the clip's own sample, bit for bit, at every
    /// frame and between frames (the man's references, his poses and his pictures, as before).
    /// </summary>
    [Fact]
    public void TheManPlaysTheClipsExactlyAsKeyed()
    {
        var skeleton = new Skeleton(Body("male"));
        var hips = skeleton.Find("hips");
        var buffer = new PoseBuffer(skeleton);
        Span<double> raw = stackalloc double[4];
        foreach (var clip in HumanoidClipList.Clips().Concat(AnimationClip.AllOf(HumanoidFiles.Load("test-clips"))))
        {
            var binding = new ClipBinding(clip, skeleton);
            Assert.True(binding.Scale == 1.0, $"{clip.Name}: the man's scale is {binding.Scale:R}");
            if (clip.Tracks.SingleOrDefault(t => t.Node == "hips")?.Translation is not { } curve)
                continue;
            for (var t = 0.0; t <= clip.Duration + 1e-9; t += 0.5 / Fps)
            {
                buffer.Clear();
                binding.Sample(t, buffer);
                curve.Evaluate(t, raw);
                for (var c = 0; c < 3; c++)
                    Assert.True(BitConverter.DoubleToInt64Bits(raw[c]) == BitConverter.DoubleToInt64Bits(buffer.Translation[3 * hips + c]), $"{clip.Name} at {t}: the hips' place moved");
            }
        }
    }

    /// <summary>
    /// The man's skeleton with the legs (the hips' height, the hip joints' offsets, the thighs, shins and feet) made
    /// <paramref name="k"/> times as long, the trunk, arms and head as they are: a made-up body with shorter or longer legs.
    /// </summary>
    static ModelData Legs(ModelData man, float k)
    {
        string[] scaled = ["hips", .. Leg, .. RightLeg];
        return new ModelData
        {
            Nodes = [.. man.Nodes.Select(n => scaled.Contains(n.Name)
                ? n with { Translation = n.Translation * k, Local = Matrix4x4.CreateScale(n.Scale) * Matrix4x4.CreateFromQuaternion(n.Rotation) * Matrix4x4.CreateTranslation(n.Translation * k) }
                : n)],
            Meshes = man.Meshes, Materials = man.Materials, Skins = man.Skins, Roots = man.Roots,
        };
    }

    /// <summary>
    /// A body with legs 0.6 and 1.2 times the man's (the hobbit's and the Goliath's, near enough) plays every whole-body
    /// clip with its feet where the man's would be, in its proportion: its scale is the legs' ratio, and at every frame
    /// each leg joint (hip joint, knee, ankle) and the hips stand at <paramref name="k"/> times the man's place, within
    /// <see cref="ScaledPlace"/> (so a foot on the man's ground is on the made-up body's: its soles' level is k times his
    /// too). Unscaled, the hips would drop by the man's centimetres onto legs that fold by k of them: death's fallen body
    /// 0.3 m into the ground on the short legs.
    /// </summary>
    [Theory]
    [InlineData(0.6f)]
    [InlineData(1.2f)]
    public void ShorterOrLongerLegsKeepTheFeetWhereTheMansWouldBe(float k)
    {
        var man = Body("male");
        var other = Legs(man, k);
        var (mine, theirs) = (new Skeleton(man), new Skeleton(other));
        string[] joints = ["hips", .. Leg, .. RightLeg];
        var worst = 0.0;
        foreach (var entry in HumanoidClipList.All.Where(e => !e.Stance))
        {
            var clip = HumanoidClipList.Clip(entry.Name);
            var (keyed, played) = (new ClipBinding(clip, mine), new ClipBinding(clip, theirs));
            Assert.Equal(k, played.Scale, 5);
            for (var frame = 0; frame <= entry.Frames; frame++)
            {
                var (a, b) = (man.WorldMatrices(keyed.Pose(frame / Fps)), other.WorldMatrices(played.Pose(frame / Fps)));
                foreach (var joint in joints)
                {
                    var off = Vector3.Distance(k * a[mine.Find(joint)].Translation, b[theirs.Find(joint)].Translation);
                    worst = Math.Max(worst, off);
                    Assert.True(off <= ScaledPlace, $"legs of {k}: {entry.Name} frame {frame}, {joint} {off * 1000:F4} mm from {k} times the man's");
                }
            }
        }
        output.WriteLine($"legs of {k}: every leg joint within {worst * 1e6:F2} µm of {k} times the man's");
    }

    /// <summary>The bound for <see cref="ShorterOrLongerLegsKeepTheFeetWhereTheMansWouldBe"/>: float32's rounding of places near a metre (measured under 0.4 µm), a hundredth of a millimetre.</summary>
    public const double ScaledPlace = 0.00001;

    /// <summary>
    /// A clip without a measure (M7's test figure's, Ji'ruan's acting) plays as keyed on every body, and so does a measured
    /// clip on a body that hasn't the chain (the frame's sample): the scale is 1 and the place the clip's own.
    /// </summary>
    [Fact]
    public void AClipWithoutAMeasureOrABodyWithoutTheChainPlaysAsKeyed()
    {
        var figure = ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, "figure.glb"));
        var stocky = new Skeleton(ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, "stocky.glb")));
        var idle = AnimationClip.AllOf(figure).Single(c => c.Name == "idle");
        Assert.Null(idle.Measure);
        // The stocky figure's legs are 0.64 of the figure's: had the idle a measure, its hips' bob would be scaled there.
        Assert.InRange(stocky.ChainLength(Leg)!.Value / new Skeleton(figure).ChainLength(Leg)!.Value, 0.6, 0.7);
        var binding = new ClipBinding(idle, stocky);
        Assert.Equal(1.0, binding.Scale);
        var buffer = new PoseBuffer(stocky);
        Span<double> raw = stackalloc double[4];
        binding.Sample(0.4, buffer);
        idle.Tracks.Single(t => t.Node == "hips").Translation!.Evaluate(0.4, raw);
        Assert.Equal(raw[1], buffer.Translation[3 * stocky.Find("hips") + 1]);
        Assert.NotEqual(0, raw[1]);
        Assert.Equal(1.0, new ClipBinding(HumanoidClipList.Clip("death"), new Skeleton(ModelData.Load(FigureFiles.Sample))).Scale);
        // A chain shorter than two joints, or naming one the skeleton hasn't, measures nothing.
        var woman = new Skeleton(Body("female"));
        Assert.Null(woman.ChainLength(["thigh.L"]));
        Assert.Null(woman.ChainLength(["thigh.L", "tail"]));
    }

    /// <summary>
    /// Only a bone's place is scaled: its rotation and scale are angles and ratios, the same on every body; a plain node's
    /// place is its local transform itself (Blender's object channels), not a bone's place from its rest, so it is left as
    /// keyed. (A hand-made clip measured as the library's, played on the woman: a bone and a plain node each moved, turned
    /// and scaled.)
    /// </summary>
    [Fact]
    public void OnlyABonesPlaceIsScaled()
    {
        var woman = new Skeleton(Body("female"));
        var keyed = new Skeleton(Body("male")).ChainLength(Leg)!.Value;
        static AnimationCurve Curve(AnimationPath path, float[] start, float[] end) => new(path, AnimationInterpolation.Linear, [0, 1], [.. start, .. end]);
        AnimationTrack Track(string node, bool relative) => new(node, relative,
            Curve(AnimationPath.Translation, [0.1f, -0.2f, 0.3f], [0.2f, -0.4f, 0.6f]),
            Curve(AnimationPath.Rotation, [0, 0, 0, 1], [0.2f, 0.1f, 0, 0.97467941f]),
            Curve(AnimationPath.Scale, [1, 1, 1], [1.5f, 0.5f, 2]));
        var plainNode = Body("female").Nodes.First(n => n.Mesh >= 0).Name;
        var clip = new AnimationClip("made", [Track("hips", true), Track(plainNode, false)], [], new ClipMeasure(Leg, keyed));
        var binding = new ClipBinding(clip, woman);
        Assert.InRange(binding.Scale, 0.96, 0.97);
        var buffer = new PoseBuffer(woman);
        binding.Sample(0.5, buffer);
        Span<double> raw = stackalloc double[4];
        var (hips, plain) = (woman.Find("hips"), woman.Find(plainNode));
        foreach (var (node, relative) in new[] { (hips, true), (plain, false) })
        {
            var track = clip.Tracks[relative ? 0 : 1];
            track.Translation!.Evaluate(0.5, raw);
            for (var c = 0; c < 3; c++)
                Assert.Equal(raw[c] * (relative ? binding.Scale : 1), buffer.Translation[3 * node + c], 12);
            track.Rotation!.Evaluate(0.5, raw);
            for (var c = 0; c < 4; c++)
                Assert.Equal(raw[c], buffer.Rotation[4 * node + c]);
            track.Scale!.Evaluate(0.5, raw);
            for (var c = 0; c < 3; c++)
                Assert.Equal(raw[c], buffer.Scale[3 * node + c]);
        }
    }

    /// <summary>
    /// The scale holds wherever a clip is bound: in a layer under another (a stance over the idle, the idle's hips scaled),
    /// in a cross-fade (the mix of the two scaled places), in the figures' state machine (stand), and for the gear bound to
    /// the bones (a garment's joints and an item's socket follow the scaled body: as an unscaled clip's pose with the hips'
    /// place scaled by hand gives them).
    /// </summary>
    [Fact]
    public void TheScaleHoldsInLayersFadesTheMachineAndForTheGear()
    {
        var body = Body("female");
        var skeleton = new Skeleton(body);
        var hips = skeleton.Find("hips");
        var ratio = new ClipBinding(HumanoidClipList.Clip("idle"), skeleton).Scale;
        Assert.InRange(ratio, 0.96, 0.97);
        double[] Raw(string clip, double t)
        {
            var v = new double[4];
            HumanoidClipList.Clip(clip).Tracks.Single(tr => tr.Node == "hips").Translation!.Evaluate(t, v);
            return v;
        }
        var buffer = new PoseBuffer(skeleton);
        void Near(double[] expected, string what)
        {
            for (var c = 0; c < 3; c++)
                Assert.True(Math.Abs(expected[c] - buffer.Translation[3 * hips + c]) < 1e-12, $"{what}: the hips' {c} is {buffer.Translation[3 * hips + c]}, not {expected[c]}");
        }
        // A stance laid over the idle: the hips are the idle's, scaled.
        var layered = new AnimationStack(skeleton);
        layered.Add(new ClipBinding(HumanoidClipList.Clip("idle"), skeleton), 0, s => s.Repeat = 10);
        layered.Add(new ClipBinding(HumanoidClipList.Clip("hold-bow"), skeleton), 0, s => s.Repeat = 10);
        layered.Evaluate(0.7, buffer);
        Near([.. Raw("idle", 0.7).Take(3).Select(v => v * ratio)], "idle+hold-bow");
        // A cross-fade from the idle into the swing, halfway: the mix of the two scaled places.
        var fade = new AnimationStack(skeleton);
        fade.Add(new ClipBinding(HumanoidClipList.Clip("idle"), skeleton), 0, s => s.Repeat = 10);
        fade.Add(new ClipBinding(HumanoidClipList.Clip("swing"), skeleton), 1, s => s.BlendIn = 0.2);
        fade.Evaluate(1.1, buffer);
        var (idle, swing) = (Raw("idle", 1.1), Raw("swing", 0.1));
        Near([.. Enumerable.Range(0, 3).Select(c => ratio * (0.5 * idle[c] + 0.5 * swing[c]))], "the fade");
        // The figures' machine, stand looped: the hips stand's, scaled.
        var machine = StateMachineDefinition.Load(new Statventure.Packs.VirtualFileSystem([Statventure.Packs.Mount.Pack(Path.Combine(HumanoidFiles.Engine, "Assets", "Packs", "models.pak"))]),
            "models/humanoid/machines/figure");
        var stack = new AnimationStack(skeleton);
        var animator = new Animator(stack, new StateMachine(machine, stack, name => new ClipBinding(HumanoidClipList.Clip(name), skeleton)));
        animator.Advance(1.25);
        stack.Evaluate(animator.Time, buffer);
        Near([.. Raw("stand", 1.25).Take(3).Select(v => v * ratio)], "the machine's stand");

        // The gear: the woman's chain shirt and a sword on her right hand at death's end, from the scaled clip, are where
        // an unscaled clip's pose with the hips' place scaled by hand puts them.
        var death = HumanoidClipList.Clip("death");
        var plain = new AnimationClip(death.Name, death.Tracks, death.Events);
        Assert.Null(plain.Measure);
        var by = new PoseBuffer(skeleton);
        new ClipBinding(plain, skeleton).Sample(death.Duration, by);
        for (var c = 0; c < 3; c++)
            by.Translation[3 * hips + c] *= ratio;
        var (expected, actual) = (body.WorldMatrices(by.ToPose()), body.WorldMatrices(new ClipBinding(death, skeleton).Pose(death.Duration)));
        var rest = body.WorldMatrices();
        var shirt = new Garment(HumanoidFiles.Load("gear/chain-shirt/human-female-average"), skeleton);
        var (wanted, got) = (shirt.WorldMatrices(rest, expected), shirt.WorldMatrices(rest, actual));
        for (var j = 0; j < wanted.Length; j++)
            Assert.True(Vector3.Distance(wanted[j].Translation, got[j].Translation) < 1e-6f, $"the chain shirt's joint {j}");
        var socket = new Socket(skeleton, "socket.hand.R");
        Assert.True(Vector3.Distance(socket.ItemTransform(expected, Matrix4x4.Identity).Translation, socket.ItemTransform(actual, Matrix4x4.Identity).Translation) < 1e-6f, "the sword");
        // ... and not where the unscaled clip puts them: her fallen hips 24 mm lower.
        var unscaled = body.WorldMatrices(new ClipBinding(plain, skeleton).Pose(death.Duration));
        Assert.True(Vector3.Distance(socket.ItemTransform(unscaled, Matrix4x4.Identity).Translation, socket.ItemTransform(actual, Matrix4x4.Identity).Translation) > 0.02f);
    }

    /// <summary>
    /// A body's ground is where its soles are at rest, its lowest point (<see cref="ModelData.LowestAtRest"/>, the skin and
    /// outfit skinned at rest as the renderer skins them): on both human bodies the booted soles, 24.0 mm under the origin's
    /// plane, rounded to the micrometre (<see cref="Actor.GroundOf"/>); their faces, hair and chain shirts reach no lower.
    /// </summary>
    [Fact]
    public void ABodysGroundIsWhereItsSolesRestUnderItsOrigin()
    {
        foreach (var sex in new[] { "male", "female" })
        {
            var body = Body(sex);
            var ground = Actor.GroundOf(body, []);
            output.WriteLine($"{sex}: the soles at rest {ground * 1000:F4} mm (lowest {body.LowestAtRest()!.Value * 1000:F5})");
            Assert.True(Math.Abs(ground + 0.024) < 0.00001, $"{sex}: the ground {ground * 1000:F4} mm, not -24.0");
            Assert.Equal((float)(Math.Round(body.LowestAtRest()!.Value * 1e6) / 1e6), ground);
            var worn = new[] { HumanoidFiles.Face(sex), HumanoidFiles.Hair(sex), HumanoidFiles.ChainShirt(sex) }.Select(ModelData.Load).ToList();
            Assert.Equal(ground, Actor.GroundOf(body, worn));
            Assert.All(worn, w => Assert.True(w.LowestAtRest() > 0.5f));
        }
    }

    /// <summary>A model of one mesh: a triangle at each height (made-up feet), on a skin of one joint at <paramref name="joint"/> when given.</summary>
    static ModelData Feet(Vector3? joint, params float[] heights)
    {
        var positions = heights.SelectMany(h => new[] { new Vector3(-0.1f, h, 0), new Vector3(0.1f, h, 0), new Vector3(0, h, 0.2f) }).ToArray();
        var skinned = joint is { } j;
        var nodes = new List<ModelNode> { new("Feet", -1, [], Matrix4x4.CreateTranslation(0, 0.5f, 0), new(0, 0.5f, 0), Quaternion.Identity, Vector3.One, 0, skinned ? 0 : -1) };
        if (skinned)
            nodes.Add(new("bone", -1, [], Matrix4x4.CreateTranslation(joint!.Value), joint.Value, Quaternion.Identity, Vector3.One, -1, -1));
        return new ModelData
        {
            Nodes = nodes,
            Meshes = [new ModelMesh("Feet", [new ModelPrimitive
            {
                Positions = positions, Normals = [.. positions.Select(_ => Vector3.UnitY)], Indices = [.. Enumerable.Range(0, positions.Length).Select(i => (uint)i)],
                Joints = skinned ? [.. positions.Select(_ => new JointIndices(0, 0, 0, 0))] : null,
                Weights = skinned ? [.. positions.Select(_ => new Vector4(1, 0, 0, 0))] : null,
            }])],
            Materials = [],
            Skins = skinned ? [new ModelSkin("Armature", [1], [Matrix4x4.CreateTranslation(-joint!.Value)], 1)] : [],
            Roots = skinned ? [0, 1] : [0],
        };
    }

    /// <summary>
    /// Whatever a body stands on is its ground, read from its shape: bare soles over the origin (the human's skin, half a
    /// millimetre up), claws reaching under the soles, hooves high over a model's origin; a skinned mesh placed by its joint
    /// at rest, any other by its node (here 0.5 m up); a garment reaching lower than the body's soles (a boot made apart)
    /// sets it; a model without a mesh says nothing (0 with nothing else).
    /// </summary>
    [Fact]
    public void BareSolesClawsAndHoovesStandOnWhatReachesLowest()
    {
        // Unskinned: its node lifts it 0.5 m.
        Assert.Equal(0.5005f, Actor.GroundOf(Feet(null, 0.0005f, 0.1f), []), 6);
        // Skinned on one joint (its inverse bind the joint's rest): placed as modelled.
        var bare = Feet(new Vector3(0, 0.08f, 0), 0.0005f, 0.12f);
        Assert.Equal(0.0005f, Actor.GroundOf(bare, []), 6);
        Assert.Equal(-0.012f, Actor.GroundOf(Feet(new Vector3(0, 0.08f, 0), 0.0005f, -0.012f, 0.05f), []), 6);
        Assert.Equal(0.031f, Actor.GroundOf(Feet(new Vector3(0, 0.3f, 0), 0.031f, 0.2f), []), 6);
        Assert.Equal(-0.024f, Actor.GroundOf(bare, [Feet(new Vector3(0, 0.08f, 0), -0.024f, 0.1f)]), 6);
        Assert.Equal(0.0005f, Actor.GroundOf(bare, [Feet(new Vector3(0, 0.08f, 0), 0.3f)]), 6);
        var empty = new ModelData { Nodes = [new("Rig", -1, [], Matrix4x4.Identity, Vector3.Zero, Quaternion.Identity, Vector3.One, -1, -1)], Meshes = [], Materials = [], Skins = [], Roots = [0] };
        Assert.Null(empty.LowestAtRest());
        Assert.Equal(0f, Actor.GroundOf(empty, []));
        Assert.Equal(-0.024f, Actor.GroundOf(empty, [Feet(new Vector3(0, 0.08f, 0), -0.024f)]), 6);
        // To the micrometre: float noise under it doesn't lift a figure.
        Assert.Equal(0f, Actor.GroundOf(Feet(new Vector3(0, 0.08f, 0), 3e-7f), []));
        Assert.Equal(0.000123f, Actor.GroundOf(Feet(new Vector3(0, 0.08f, 0), 0.0001234f), []), 9);
    }
}

/// <summary>
/// An actor stands by its soles on its node (GM2-P22): it is drawn raised by its ground's negation, the body, its garments
/// and its items alike, its hatching anchored at its raised origin; with a ground of 0 exactly as before.
/// </summary>
[Collection("GPU")]
public class ActorGroundTests(GpuFixture gpu)
{
    [Fact]
    public void AnActorIsDrawnStandingOnItsNodeBySoles()
    {
        using var body = Model.Load(gpu.Device, HumanoidFiles.Body("male"));
        using var shirt = Model.Load(gpu.Device, HumanoidFiles.ChainShirt("male"));
        using var sword = Model.Load(gpu.Device, HumanoidFiles.Sword);
        var skeleton = new Skeleton(body.Data);
        var actor = new Actor(body, new Animator(new AnimationStack(skeleton)));
        actor.Garments.Add(new ActorGarment(shirt, new Garment(shirt.Data, skeleton)));
        actor.Items.Add(new ActorItem(sword, new Socket(skeleton, "socket.hand.R")));
        actor.Refresh();
        var drawing = new SceneDrawing(CelStyle.Default, new Camera(), 400);
        var before = new List<DrawItem>();
        Assert.Equal(Matrix4x4.Identity, actor.Placement);
        actor.Draw(before, drawing);
        actor.Ground = Actor.GroundOf(body.Data, actor.Garments.Select(g => g.Model.Data));
        Assert.Equal(-0.023997f, actor.Ground, 6);
        Assert.Equal(Matrix4x4.CreateTranslation(0, 0.023997f, 0), actor.Placement);
        var after = new List<DrawItem>();
        actor.Draw(after, drawing);
        Assert.Equal(before.Count, after.Count);
        Assert.True(before.Count >= 3);
        for (var i = 0; i < before.Count; i++)
        {
            Assert.True(Vector3.Distance(before[i].Model.Translation + new Vector3(0, 0.023997f, 0), after[i].Model.Translation) < 1e-6f, $"item {i} isn't raised by the ground");
            Assert.True(Vector3.Distance(before[i].Anchor + new Vector3(0, 0.023997f, 0), after[i].Anchor) < 1e-6f, $"item {i}'s hatching isn't anchored at the raised origin");
        }
        actor.Ground = 0;
        var again = new List<DrawItem>();
        actor.Draw(again, drawing);
        Assert.Equal(before.Select(i => (i.Model, i.Anchor)), again.Select(i => (i.Model, i.Anchor)));
    }
}
