using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Scene;
using Xunit.Abstractions;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// A clip keeps its contacts with the ground on a body other than the one it was keyed on (GM2-P37; Docs/Animation.md, "A
/// clip keeps its contacts"; Docs/Game/Hero.md, "How it works", "Contacts kept"): what touches the ground on the man touches
/// it on the playing body, what is in the air stays as the measure (GM2-P22) puts it, nothing changes on the man, and the
/// correction never moves faster than the motion that drives it. Held on the repo's six human bodies and on made-up bodies
/// (the hobbit's proportions, a heavy one, a tall one).
/// </summary>
public class ContactsKeptTests(ITestOutputHelper output)
{
    const double Fps = 60;

    static ModelData Human(string body) => HumanoidFiles.Load($"bodies/human-{body}");

    public static readonly string[] Bodies = ["male-average", "female-average", "male-slender", "male-strong", "female-slender", "female-strong"];

    /// <summary>
    /// Every whole-body clip of the library keeps its contacts, keyed on the man with his outfit
    /// (<c>models/humanoid/bodies/human-male-average</c>, given by the library's folder), carried by the hips, with both
    /// legs and, where it turns them, both arms (flee and stand leave the hands alone); the stance loops and the frame's test
    /// clip keep none.
    /// </summary>
    [Fact]
    public void EveryWholeBodyClipKeepsItsContactsKeyedOnTheMan()
    {
        string[][] legs = [["thigh.L", "shin.L", "foot.L"], ["thigh.R", "shin.R", "foot.R"]];
        string[][] arms = [["upper_arm.L", "forearm.L", "hand.L"], ["upper_arm.R", "forearm.R", "hand.R"]];
        foreach (var entry in HumanoidClipList.All)
        {
            var contacts = HumanoidClipList.Clip(entry.Name).Contacts;
            if (entry.Stance)
            {
                Assert.True(contacts == null, $"{entry.Name} keeps contacts");
                continue;
            }
            Assert.True(contacts != null, $"{entry.Name} keeps no contacts");
            Assert.Equal(("models/humanoid/bodies/human-male-average", "hips"), (contacts.Data.KeyedOn, contacts.Root));
            string[][] chains = entry.Name is "flee" or "stand" ? legs : [.. legs, .. arms];
            Assert.Equal(chains.Select(c => string.Join(" ", c)), contacts.Chains.Select(c => string.Join(" ", c)));
            Assert.NotNull(contacts.KeyedBody);
            Assert.Equal(entry.Frames + 1, contacts.Times.Length);
        }
        Assert.All(AnimationClip.AllOf(HumanoidFiles.TestClips), c => Assert.Null(c.Contacts));
    }

    /// <summary>
    /// The rule's numbers (<see cref="ContactRule"/>): the weight 1 at a keyed clearance of 0 or under, a smoothstep down to
    /// 0 at 150 mm (half at 75 mm); the target the keyed clearance in the 2 mm band, under the ground included, and above
    /// it the band plus the ratio times the height over it, the ratio 1 giving the keyed clearance to the bit.
    /// </summary>
    [Fact]
    public void TheWeightAndTheTargetAreTheRulesNumbers()
    {
        Assert.Equal((0.150, 0.002, 4), (ContactRule.Fade, ContactRule.Band, ContactRule.Rounds));
        Assert.Equal(1.0, ContactRule.Weight(-0.002));
        Assert.Equal(1.0, ContactRule.Weight(0));
        Assert.Equal(0.5, ContactRule.Weight(0.075), 12);
        Assert.Equal(1 - (3 * 0.04 - 2 * 0.008), ContactRule.Weight(0.030), 12);
        Assert.Equal(0.0, ContactRule.Weight(0.150));
        Assert.Equal(0.0, ContactRule.Weight(0.3));
        Assert.Equal(-0.0021, ContactRule.Target(-0.0021, 0.48));
        Assert.Equal(0.0015, ContactRule.Target(0.0015, 1.21));
        Assert.Equal(0.002 + 0.48 * 0.298, ContactRule.Target(0.3, 0.48), 12);
        Assert.Equal(0.002 + 1.21 * 0.098, ContactRule.Target(0.1, 1.21), 12);
        foreach (var keyed in new[] { -0.002, 0.0013, 0.0371, 0.149, 0.7 })
            Assert.Equal(BitConverter.DoubleToInt64Bits(keyed), BitConverter.DoubleToInt64Bits(ContactRule.Target(keyed, 1)));
    }

    /// <summary>
    /// On the body the clips were keyed on nothing changes: every clip's binding on the man has no correction, so its poses
    /// are the measured clip's to the bit, on frames and between them (his references and pictures as they were).
    /// </summary>
    [Fact]
    public void OnTheManNothingChanges()
    {
        var skeleton = new Skeleton(Human("male-average"));
        foreach (var entry in HumanoidClipList.All.Where(e => !e.Stance))
        {
            var kept = new ClipBinding(HumanoidClipList.Clip(entry.Name), skeleton);
            Assert.True(kept.Contacts == null, $"{entry.Name}: the man's contacts move {kept.Contacts?.Changed} keys");
            var measured = new ClipBinding(HumanoidClipList.Measured(entry.Name), skeleton);
            var (a, b) = (new PoseBuffer(skeleton), new PoseBuffer(skeleton));
            for (var t = 0.0; t <= entry.Seconds + 1e-9; t += 0.5 / Fps)
            {
                a.Clear();
                b.Clear();
                kept.Sample(t, a);
                measured.Sample(t, b);
                Assert.True(a.Translation.AsSpan().SequenceEqual(b.Translation) && a.Rotation.AsSpan().SequenceEqual(b.Rotation), $"{entry.Name} at {t}");
            }
        }
    }

    /// <summary>
    /// The repo's six human bodies keep the contacts (the ground's own bound is <see cref="HumanoidClipTests.BothBodiesStayOnTheGround"/>'s):
    /// a foot planted on the man is where his is (in the band as keyed), a fall's fist lies where his does, a limb in the
    /// air turns as the measure has it, and the correction moves no faster than the motion that drives it.
    /// </summary>
    [Theory]
    [MemberData(nameof(BodyNames))]
    public void EveryHumanBodyKeepsItsContacts(string body)
    {
        var report = Measure(Human(body), $"human-{body}");
        output.WriteLine(report.Summary);
        report.Hold();
    }

    public static TheoryData<string> BodyNames => new(Bodies);

    public static TheoryData<string> MadeUpNames => new("hobbit", "heavy", "tall");

    /// <summary>
    /// Made-up bodies: the hobbit's proportions (the man's legs 0.48 of his, with their skin, his feet 0.90, his hip joints
    /// as wide apart as his: his trunk on short legs, whose straight legs can't reach the ground in `stand`, so the reach
    /// pass brings its hips down), a heavy body (the man's skeleton, his skin half again as thick about every bone: lying,
    /// its thighs ask the hips up while a clamped heel asks them down, and up wins, leaving the heel at most a tenth of a
    /// millimetre off where it should be) and a tall one (legs, hips and feet 1.2): nothing of them further under the ground
    /// than the man's bound in any clip, death's fallen body lying on it, and the contacts kept as on the repo's bodies.
    /// </summary>
    [Theory]
    [MemberData(nameof(MadeUpNames))]
    public void MadeUpBodiesKeepTheirContacts(string name)
    {
        var man = Human("male-average");
        var body = name switch
        {
            "hobbit" => Proportioned(man, 0.48f, 0.90f, hips: 1),
            "heavy" => Inflated(man, 1.5f),
            _ => Proportioned(man, 1.2f, 1.2f, hips: 1.2f),
        };
        var report = Measure(body, name);
        output.WriteLine(report.Summary);
        report.Hold();
        // Each made-up body meets the part of the rule it was made for.
        if (name == "hobbit")
            Assert.True(report.LoweredIn.Contains("stand"), $"the hobbit's hips came down only in {string.Join(", ", report.LoweredIn)}");
        if (name == "heavy")
        {
            Assert.True(report.Conflicts > 0, "the heavy body's thighs and heels never asked the hips two ways");
            // What up winning costs it: a clamped heel left off where it should be, by no more than a planted foot may be.
            Assert.True(report.LeftOver > 0 && report.LeftOver <= PlantedTolerance, $"the heavy body's conflicts left a heel {report.LeftOver * 1000:F3} mm off");
        }
        Assert.True(report.Worst <= HumanoidClipTests.GroundDepth, $"{name}: {report.Worst * 1000:F2} mm under the ground ({report.WorstAt})");
        Assert.True(report.DeathEnd >= -HumanoidClipTests.GroundDepth && report.DeathEnd <= HumanoidClipTests.LyingHeight,
            $"{name}: death's fallen body {report.DeathEnd * 1000:F2} mm over the ground");
    }

    /// <summary>
    /// What the figure wears meets the ground with its skin, and a change of it binds again: the man in his chain shirt lies
    /// at death's end on the shirt (without the contacts its rings sink 12 mm into the ground: the clip was keyed on his
    /// outfit alone), lifted out of it, while the same clip bound to his bare skeleton changes nothing.
    /// </summary>
    [Fact]
    public void WhatIsWornMeetsTheGround()
    {
        var man = Human("male-average");
        var shirt = ModelData.Load(HumanoidFiles.ChainShirt("male"));
        var bare = new Skeleton(man);
        var dressed = new Skeleton(man, [shirt]);
        Assert.Equal([shirt], dressed.Worn);
        var death = HumanoidClipList.Clip("death");
        Assert.Null(new ClipBinding(death, bare).Contacts);
        var kept = new ClipBinding(death, dressed);
        Assert.NotNull(kept.Contacts);
        var measured = new ClipBinding(HumanoidClipList.Measured("death"), dressed);
        var shape = ContactShape.Of(dressed, death.Contacts!)!;
        var (before, after) = (shape.Clearances(man.WorldMatrices(measured.Pose(death.Duration))), shape.Clearances(man.WorldMatrices(kept.Pose(death.Duration))));
        output.WriteLine($"death's end in the chain shirt: lowest {before.Min() * 1000:F2} mm over the ground, kept {after.Min() * 1000:F2}; the hips up {kept.Contacts!.LargestHipsMove * 1000:F2} mm");
        Assert.True(before.Min() < -0.010, $"the shirt lies {before.Min() * 1000:F2} mm over the ground without the contacts");
        Assert.True(after.Min() > -HumanoidClipTests.GroundDepth, $"the shirt lies {after.Min() * 1000:F2} mm over the ground with them");
    }

    /// <summary>
    /// A clip keeping contacts needs the body it was keyed on: loaded without it, binding it to a body with a mesh is refused
    /// (naming the clip and the body), and to a skeleton without a mesh (the library's own) it plays as measured.
    /// </summary>
    [Fact]
    public void AClipWithoutItsKeyedBodyIsRefusedOnABody()
    {
        var library = HumanoidFiles.Load("clips");
        var death = AnimationClip.AllOf(library).Single(c => c.Name == "death");
        Assert.NotNull(death.Contacts);
        Assert.Null(death.Contacts!.KeyedBody);
        var problem = Assert.Throws<InvalidOperationException>(() => new ClipBinding(death, new Skeleton(Human("female-average"))));
        Assert.Contains("death", problem.Message);
        Assert.Contains("models/humanoid/bodies/human-male-average", problem.Message);
        Assert.Null(new ClipBinding(death, new Skeleton(library)).Contacts);
    }

    /// <summary>
    /// The library's knees and elbows never pass straight (the man's knees at least 7° flexed, his elbows on the other side
    /// at least 1°), so the solve, which keeps the side a joint is on, never folds one the wrong way.
    /// </summary>
    [Fact]
    public void TheLibrarysKneesAndElbowsKeepTheirSides()
    {
        var man = Human("male-average");
        var skeleton = new Skeleton(man);
        var (knee, elbow) = (double.MaxValue, double.MinValue);
        foreach (var entry in HumanoidClipList.All)
        {
            var binding = new ClipBinding(HumanoidClipList.Clip(entry.Name), skeleton);
            for (var f = 0; f <= entry.Frames; f++)
            {
                var world = man.WorldMatrices(binding.Pose(f / Fps));
                foreach (var side in new[] { "L", "R" })
                {
                    knee = Math.Min(knee, Flexion(world, skeleton, $"thigh.{side}", $"shin.{side}", $"foot.{side}"));
                    elbow = Math.Max(elbow, Flexion(world, skeleton, $"upper_arm.{side}", $"forearm.{side}", $"hand.{side}"));
                }
            }
        }
        output.WriteLine($"the man's knees at least {knee:F2}° flexed, his elbows at most {elbow:F2}°");
        Assert.True(knee > 5, $"a knee {knee:F2}° from straight");
        Assert.True(elbow < -1, $"an elbow {elbow:F2}° from straight");
    }

    /// <summary>
    /// The rounds' order, at one key (<see cref="KeySolve"/>, the man lying at death's end, the groups given keyed clearances
    /// one case at a time, the others taking no part): a straight leg that can't reach down (its foot asked a metre into the
    /// ground) brings the hips down only as far as nothing goes under the ground or its goal, and where an upper part under
    /// the ground asks them up, up wins, in every round: the right leg lying 1.9 mm in the ground (its goal the ground) lifts
    /// them out of it; the rest of the body over the ground (its goal where it lies) lets them down until it meets the
    /// ground; the right leg on its goal (in the band as keyed) keeps them where they are. Each counted as a conflict.
    /// </summary>
    [Fact]
    public void UpWinsInEveryRound()
    {
        var (footL, legR) = (ContactShape.EndGroup(0), ContactShape.UpperGroup(1));
        const double Deep = -1.0;
        (KeySolve Solve, double[] Own, double[] After) Run(Action<double[], double[]> keys)
        {
            var man = Human("male-average");
            var skeleton = new Skeleton(man);
            var death = HumanoidClipList.Clip("death");
            var shape = ContactShape.Of(skeleton, death.Contacts!)!;
            var buffer = new PoseBuffer(skeleton);
            new ClipBinding(HumanoidClipList.Measured("death"), skeleton).Sample(death.Duration, buffer);
            var own = shape.Clearances(man.WorldMatrices(buffer.ToPose()));
            var keyed = Enumerable.Repeat(double.PositiveInfinity, shape.Groups).ToArray();
            keys(keyed, own);
            var solve = new KeySolve(shape, buffer, keyed, 1);
            solve.Run();
            Assert.True(solve.EndedClamped, "the left leg reached a metre into the ground");
            return (solve, own, shape.Clearances(man.WorldMatrices(buffer.ToPose())));
        }

        var up = Run((keyed, own) => (keyed[legR], keyed[footL]) = (0, Deep));
        output.WriteLine($"the right leg {up.Own[legR] * 1000:F2} mm over the ground: the hips moved {up.Solve.Hips * 1000:F3} mm, the leg {up.After[legR] * 1000:F3} mm over it");
        Assert.True(up.Own[legR] < -0.001, $"the right leg lies {up.Own[legR] * 1000:F2} mm over the ground");
        Assert.True(up.Solve.Conflicted && !up.Solve.Lowered, $"conflicted {up.Solve.Conflicted}, lowered {up.Solve.Lowered}");
        Assert.Equal(-up.Own[legR], up.Solve.Hips, 6);
        Assert.True(up.After[legR] >= -1e-6, $"the right leg {up.After[legR] * 1000:F3} mm over the ground");
        Assert.True(up.Solve.LeftOver > 0.1, $"the left foot, asked a metre under the ground, left {up.Solve.LeftOver * 1000:F1} mm off");

        var rest = Run((keyed, own) => (keyed[0], keyed[footL]) = (own[0], Deep));
        output.WriteLine($"the rest {rest.Own[0] * 1000:F2} mm over the ground: the hips moved {rest.Solve.Hips * 1000:F3} mm, the rest {rest.After[0] * 1000:F3} mm over it");
        Assert.True(rest.Own[0] > 0.010, $"the rest lies {rest.Own[0] * 1000:F2} mm over the ground");
        Assert.True(rest.Solve.Conflicted && rest.Solve.Lowered, $"conflicted {rest.Solve.Conflicted}, lowered {rest.Solve.Lowered}");
        Assert.Equal(-rest.Own[0], rest.Solve.Hips, 6);
        Assert.True(rest.After[0] >= -1e-6, $"the rest {rest.After[0] * 1000:F3} mm over the ground");

        var held = Run((keyed, own) => (keyed[legR], keyed[footL]) = (own[legR], Deep));
        Assert.True(held.Solve.Conflicted && !held.Solve.Lowered, $"conflicted {held.Solve.Conflicted}, lowered {held.Solve.Lowered}");
        Assert.Equal(0, held.Solve.Hips);
    }

    /// <summary>
    /// The hips move straight up or down in model space by what the rule asks, whatever the root's turn at rest (its channel
    /// is in its rest's frame): the rest of the body asked half a metre into the ground brings them down by that and its
    /// clearance, on the man (his hips' rest a half turn), and with his hips' rest turned a quarter turn about X and a third
    /// of a turn about a slanted axis.
    /// </summary>
    [Fact]
    public void TheHipsMoveStraightUpWhateverTheirRestTurn()
    {
        var man = Human("male-average");
        var contacts = HumanoidClipList.Clip("death").Contacts!;
        foreach (var turn in new[] { Quaternion.Identity, Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2), Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(0.3f, 0.2f, 1)), MathF.PI / 1.5f) })
        {
            var body = new ModelData
            {
                Nodes = [.. man.Nodes.Select(n => n.Name != "hips" ? n : n with
                {
                    Rotation = n.Rotation * turn,
                    Local = Matrix4x4.CreateScale(n.Scale) * Matrix4x4.CreateFromQuaternion(n.Rotation * turn) * Matrix4x4.CreateTranslation(n.Translation),
                })],
                Meshes = man.Meshes, Materials = man.Materials, Skins = man.Skins, Roots = man.Roots,
            };
            var skeleton = new Skeleton(body);
            var shape = ContactShape.Of(skeleton, contacts)!;
            var buffer = new PoseBuffer(skeleton);
            var keyed = Enumerable.Repeat(double.PositiveInfinity, shape.Groups).ToArray();
            keyed[0] = -0.5;
            var hips = skeleton.Find("hips");
            var before = body.WorldMatrices(buffer.ToPose())[hips].Translation;
            var solve = new KeySolve(shape, buffer, keyed, 1);
            solve.Run();
            var moved = body.WorldMatrices(buffer.ToPose())[hips].Translation - before;
            Assert.True(solve.Hips <= -0.5, $"the hips moved {solve.Hips * 1000:F2} mm");
            Assert.True(Vector3.Distance(moved, new Vector3(0, (float)solve.Hips, 0)) < 1e-5f, $"turned {turn}: the hips moved by {moved * 1000} mm, asked {solve.Hips * 1000:F2} mm down");
        }
    }

    /// <summary>
    /// Each vertex goes with the bone it follows most, wherever the file lists it among its four (the exporter lists the
    /// heaviest first): the man with every vertex's joints and weights listed the other way round gives every group the same
    /// clearance, standing, kneeling and lying.
    /// </summary>
    [Fact]
    public void EachVertexGoesWithTheBoneItFollowsMost()
    {
        var man = Human("male-average");
        var meshes = man.Meshes.ToArray();
        foreach (var node in man.Nodes.Where(n => n.Mesh >= 0 && n.Skin >= 0))
            meshes[node.Mesh] = meshes[node.Mesh] with
            {
                Primitives = [.. meshes[node.Mesh].Primitives.Select(p => p.Joints == null || p.Weights == null ? p : new ModelPrimitive
                {
                    Positions = p.Positions, Normals = p.Normals, TexCoords = p.TexCoords, Indices = p.Indices, Material = p.Material,
                    Joints = [.. p.Joints.Select(j => new JointIndices(j.J3, j.J2, j.J1, j.J0))],
                    Weights = [.. p.Weights.Select(w => new Vector4(w.W, w.Z, w.Y, w.X))],
                })],
            };
        var reversed = new ModelData { Nodes = man.Nodes, Meshes = meshes, Materials = man.Materials, Skins = man.Skins, Roots = man.Roots };
        var skeleton = new Skeleton(man);
        var contacts = HumanoidClipList.Clip("death").Contacts!;
        var (listed, other) = (ContactShape.Of(skeleton, contacts)!, ContactShape.Of(new Skeleton(reversed), contacts)!);
        foreach (var (clip, frame) in new[] { ("stand", 0), ("meditate", 22), ("death", 60) })
        {
            var world = man.WorldMatrices(new ClipBinding(HumanoidClipList.Measured(clip), skeleton).Pose(frame / Fps));
            var (a, b) = (listed.Clearances(world), other.Clearances(world));
            for (var g = 0; g < a.Length; g++)
                Assert.True(Math.Abs(a[g] - b[g]) <= 1e-6, $"{clip} frame {frame}: group {g} {a[g] * 1000:F4} mm, listed the other way {b[g] * 1000:F4} mm");
        }
    }

    /// <summary>The middle joint's turn from straight about its own X (degrees), as the solve measures it: a knee's flexion positive.</summary>
    static double Flexion(Matrix4x4[] world, Skeleton skeleton, string top, string middle, string end)
    {
        var (h, k, a) = (world[skeleton.Find(top)].Translation, world[skeleton.Find(middle)].Translation, world[skeleton.Find(end)].Translation);
        var m = world[skeleton.Find(middle)];
        var x = Vector3.Normalize(new Vector3(m.M11, m.M12, m.M13));
        var (kh, p) = (k - h, a - k);
        return -Math.Atan2(Vector3.Dot(kh, Vector3.Cross(x, p)), Vector3.Dot(kh, p)) * 180 / Math.PI;
    }

    /// <summary>
    /// The man with his legs <paramref name="legs"/> times his (the hips' height, and the thighs scaled with everything under
    /// them, their skin included), his hip joints <paramref name="hips"/> times as far apart and his feet
    /// <paramref name="feet"/> times his; the trunk, the arms and the head as they are.
    /// </summary>
    internal static ModelData Proportioned(ModelData man, float legs, float feet, float hips)
    {
        static ModelNode With(ModelNode n, Vector3 translation, Vector3 scale) => n with
        {
            Translation = translation, Scale = scale,
            Local = Matrix4x4.CreateScale(scale) * Matrix4x4.CreateFromQuaternion(n.Rotation) * Matrix4x4.CreateTranslation(translation),
        };
        return new ModelData
        {
            Nodes = [.. man.Nodes.Select(n => n.Name switch
            {
                "hips" => With(n, n.Translation * legs, n.Scale),
                "thigh.L" or "thigh.R" => With(n, n.Translation * hips, n.Scale * legs),
                "foot.L" or "foot.R" => With(n, n.Translation, n.Scale * (feet / legs)),
                _ => n,
            })],
            Meshes = man.Meshes, Materials = man.Materials, Skins = man.Skins, Roots = man.Roots,
        };
    }

    /// <summary>The man with his skin <paramref name="factor"/> times as thick about every bone: each vertex moved away from the nearest point of the bone it follows most (its joint to its first child joint).</summary>
    internal static ModelData Inflated(ModelData man, float factor)
    {
        var meshes = man.Meshes.ToArray();
        foreach (var node in man.Nodes.Where(n => n.Mesh >= 0 && n.Skin >= 0))
        {
            var skin = man.Skins[node.Skin];
            var joints = skin.Joints.ToList();
            Vector3 Bind(int j) => Matrix4x4.Invert(skin.InverseBindMatrices[j], out var m) ? m.Translation : Vector3.Zero;
            var tails = joints.Select((joint, j) => man.Nodes[joint].Children.Where(c => joints.Contains(c) && !man.Nodes[c].Name.StartsWith("socket.", StringComparison.Ordinal))
                .Select(c => Bind(joints.IndexOf(c))).DefaultIfEmpty(Bind(j)).First()).ToArray();
            meshes[node.Mesh] = meshes[node.Mesh] with
            {
                Primitives = [.. meshes[node.Mesh].Primitives.Select(p => new ModelPrimitive
                {
                    Positions = [.. p.Positions.Select((position, v) =>
                    {
                        var w = p.Weights![v];
                        var js = p.Joints![v];
                        float[] ws = [w.X, w.Y, w.Z, w.W];
                        int[] jj = [js.J0, js.J1, js.J2, js.J3];
                        var most = Array.IndexOf(ws, ws.Max());
                        var (head, tail) = (Bind(jj[most]), tails[jj[most]]);
                        var along = tail - head;
                        var s = along.LengthSquared() > 1e-12f ? Math.Clamp(Vector3.Dot(position - head, along) / along.LengthSquared(), 0, 1) : 0;
                        var nearest = head + s * along;
                        return nearest + factor * (position - nearest);
                    })],
                    Normals = p.Normals, TexCoords = p.TexCoords, Joints = p.Joints, Weights = p.Weights, Indices = p.Indices, Material = p.Material,
                })],
            };
        }
        return new ModelData { Nodes = man.Nodes, Meshes = meshes, Materials = man.Materials, Skins = man.Skins, Roots = man.Roots };
    }

    /// <summary>What a body's play of every whole-body clip shows, measured by the contacts' own groups (<see cref="ContactShape"/>).</summary>
    internal sealed class Report
    {
        public string Name = "";
        public double Worst, DeathEnd, PlantedOff, PlantedReach, FistOff, AirTurned, StepOverMotion = double.MinValue, Step, Ratio, LeftOver, FeetUp, FeetDown, PlantedOver = double.MinValue, PlantedOverClamped = double.MinValue;
        public string WorstAt = "", PlantedAt = "", StepAt = "", FistAt = "", AirAt = "", FeetUpAt = "", FeetDownAt = "", PlantedOverAt = "", PlantedOverClampedAt = "";
        public int Clamped, Conflicts, Changed, Lowered;
        public List<string> LoweredIn { get; } = [];
        public List<string> Lines { get; } = [];

        public string Summary => $"{Name}: worst {Worst * 1000:F2} mm under ({WorstAt}), death's end {DeathEnd * 1000:F2} mm over; planted feet off the man's by {PlantedOff * 1000:F3} mm ({PlantedAt}), " +
            $"at most {PlantedReach * 1000:F2} mm from the ground and {PlantedOver * 1000:F4} mm over it ({PlantedOverAt}; in a clip with clamped keys {(PlantedOverClamped > double.MinValue ? $"{PlantedOverClamped * 1000:F4} mm, {PlantedOverClampedAt}" : "none")}); the fallen fist off his by {FistOff * 1000:F3} mm ({FistAt}); a limb in the air turned {AirTurned:E1} ({AirAt}); " +
            $"the largest step {Step * 1000:F2} mm ({StepAt}), at most {Ratio:F2} of the motion, never past it by more than {StepOverMotion * 1000:F3} mm; {Changed} keys changed, {Clamped} clamped, {Lowered} lowered ({string.Join(", ", LoweredIn)}), {Conflicts} conflicts (a clamped end left at most {LeftOver * 1000:F3} mm off); " +
            $"on its feet (every clip but death and meditate) the hips at most {FeetUp * 1000:F2} mm up ({FeetUpAt}) and {FeetDown * 1000:F2} mm down ({FeetDownAt})\n" + string.Join("\n", Lines);

        /// <summary>The contacts' own bounds (the ground's are the caller's).</summary>
        public void Hold()
        {
            Assert.True(PlantedOver <= PlantedOverHeight, $"{Name}: a foot planted on the man is left {PlantedOver * 1000:F4} mm over the ground ({PlantedOverAt})");
            Assert.True(PlantedOff <= PlantedTolerance, $"{Name}: a foot planted on the man is {PlantedOff * 1000:F3} mm off his ({PlantedAt})");
            Assert.True(PlantedReach <= PlantedHeight, $"{Name}: a foot planted on the man is {PlantedReach * 1000:F2} mm from the ground ({PlantedAt})");
            Assert.True(FistOff <= PlantedTolerance, $"{Name}: the fallen fist is {FistOff * 1000:F3} mm off the man's ({FistAt})");
            Assert.True(AirTurned <= AirTolerance, $"{Name}: a limb in the air turned {AirTurned:E2} off the measure's ({AirAt})");
            Assert.True(StepOverMotion <= StepSlack, $"{Name}: the correction stepped {StepOverMotion * 1000:F3} mm past the motion that drives it ({StepAt})");
            Assert.True(Step <= LargestStep, $"{Name}: the correction stepped {Step * 1000:F2} mm between frames ({StepAt})");
        }
    }

    /// <summary>How far a foot planted on the man (its clearance in the band) may be from his clearance on another body: what the rounds leave (measured at most 0.025 mm, the made-up heavy body's foot in punch), a tenth of a millimetre.</summary>
    public const double PlantedTolerance = 0.0001;

    /// <summary>How far from the ground a foot planted on the man may be on any body: 2 mm (the manager's, 2026-11-21) and what the rounds leave.</summary>
    public const double PlantedHeight = 0.002 + PlantedTolerance;

    /// <summary>
    /// How far over the ground a foot planted on the man may be left on any body, the reach pass's short straight legs
    /// included (the manager's, 2026-11-22): 2 mm, the brief's bound, and 5 µm of what the rounds leave where the man himself
    /// keys a foot at the band's top (cast's frame 46, his right foot 1.9995 mm over the ground; the others within 1.7 µm of
    /// him, the most 2.0012 mm).
    /// </summary>
    public const double PlantedOverHeight = ContactRule.Band + 0.000005;

    /// <summary>How far a limb in the air may turn off the measure's (a quaternion's components): float rounding.</summary>
    public const double AirTolerance = 1e-6;

    /// <summary>How far past the motion that drives it the correction may step between frames (metres): a tenth of a millimetre of float noise and the rounds' convergence.</summary>
    public const double StepSlack = 0.0001;

    /// <summary>The largest step of the correction between frames at 60 fps (metres): measured at most 29.04 mm (the made-up heavy body's foot in flee's push-off, frame 14, where the man's toe rises 53 mm in that frame), the bound its worst plus about a tenth.</summary>
    public const double LargestStep = 0.032;

    /// <summary>
    /// Plays every whole-body clip on <paramref name="body"/> (wearing <paramref name="worn"/>) and measures at every frame,
    /// by the contacts' groups: the lowest point; each leg's foot where the man's is planted (in the band); the right fist
    /// at death's end; each chain whose end is beyond the fade on the man (in the air): its joints' turns against the
    /// measure's; and the correction's step between frames (the hips' and each end's move off the measured pose) against
    /// the motion (each group's measured clearance's change plus the ratio times the man's).
    /// </summary>
    internal static Report Measure(ModelData body, string name, IReadOnlyList<ModelData>? worn = null)
    {
        var report = new Report { Name = name, Worst = double.MinValue, DeathEnd = double.NaN };
        var skeleton = new Skeleton(body, worn);
        foreach (var entry in HumanoidClipList.All.Where(e => !e.Stance))
        {
            var clip = HumanoidClipList.Clip(entry.Name);
            var contacts = clip.Contacts!;
            var shape = ContactShape.Of(skeleton, contacts)!;
            var kept = new ClipBinding(clip, skeleton);
            var measured = new ClipBinding(HumanoidClipList.Measured(entry.Name), skeleton);
            var ratio = kept.Scale;
            report.Changed += kept.Contacts?.Changed ?? 0;
            report.Clamped += kept.Contacts?.Clamped ?? 0;
            report.Conflicts += kept.Contacts?.Conflicts ?? 0;
            report.Lowered += kept.Contacts?.Lowered ?? 0;
            report.LeftOver = Math.Max(report.LeftOver, kept.Contacts?.LeftOver ?? 0);
            var onFeet = entry.Name is not ("death" or "meditate");
            if (kept.Contacts?.Lowered > 0)
                report.LoweredIn.Add(entry.Name);
            var nodes = new[] { skeleton.Find(contacts.Root) }.Concat(contacts.Chains.Select(c => skeleton.Find(c[2]))).ToArray();
            double[]? lastBefore = null;
            Vector3[]? lastShift = null;
            var worstHere = double.MaxValue;
            for (var f = 0; f <= entry.Frames; f++)
            {
                var keyed = contacts.Keyed[f];
                var (plain, posed) = (body.WorldMatrices(measured.Pose(f / Fps)), body.WorldMatrices(kept.Pose(f / Fps)));
                var (before, after) = (shape.Clearances(plain), shape.Clearances(posed));
                var low = after.Min();
                worstHere = Math.Min(worstHere, low);
                if (-low > report.Worst)
                    (report.Worst, report.WorstAt) = (-low, $"{entry.Name} frame {f}");
                for (var c = 0; c < contacts.Chains.Count; c++)
                {
                    var end = ContactShape.EndGroup(c);
                    if (contacts.Chains[c][2].StartsWith("foot.", StringComparison.Ordinal) && keyed[end] <= ContactRule.Band)
                    {
                        var off = Math.Abs(after[end] - keyed[end]);
                        if (off > report.PlantedOff)
                            (report.PlantedOff, report.PlantedAt) = (off, $"{entry.Name} frame {f} {contacts.Chains[c][2]}");
                        report.PlantedReach = Math.Max(report.PlantedReach, Math.Abs(after[end]));
                        if (after[end] > report.PlantedOver)
                            (report.PlantedOver, report.PlantedOverAt) = (after[end], $"{entry.Name} frame {f} {contacts.Chains[c][2]}");
                        if (kept.Contacts?.Clamped > 0 && after[end] > report.PlantedOverClamped)
                            (report.PlantedOverClamped, report.PlantedOverClampedAt) = (after[end], $"{entry.Name} frame {f} {contacts.Chains[c][2]}");
                    }
                    if (keyed[end] >= ContactRule.Fade)
                    {
                        var (a, b) = (new PoseBuffer(skeleton), new PoseBuffer(skeleton));
                        kept.Sample(f / Fps, a);
                        measured.Sample(f / Fps, b);
                        foreach (var joint in contacts.Chains[c].Select(skeleton.Find))
                            for (var k = 0; k < 4; k++)
                                if (Math.Abs(a.Rotation[4 * joint + k] - b.Rotation[4 * joint + k]) is var turned && turned > report.AirTurned)
                                    (report.AirTurned, report.AirAt) = (turned, $"{entry.Name} frame {f} {contacts.Chains[c][2]}");
                    }
                }
                if (entry.Name == "death" && f == entry.Frames)
                {
                    report.DeathEnd = low;
                    var fist = contacts.Chains.Select((c, i) => (c, i)).Single(x => x.c[2] == "hand.R").i;
                    var off = Math.Abs(after[ContactShape.EndGroup(fist)] - keyed[ContactShape.EndGroup(fist)]);
                    (report.FistOff, report.FistAt) = (off, $"{after[ContactShape.EndGroup(fist)] * 1000:F2} mm against {keyed[ContactShape.EndGroup(fist)] * 1000:F2}");
                    report.Lines.Add($"  death's end: the trunk {after[0] * 1000:F2} mm over the ground (the man's {keyed[0] * 1000:F2}), its lowest point {low * 1000:F2}");
                }
                var shift = nodes.Select(n => posed[n].Translation - plain[n].Translation).ToArray();
                if (onFeet && shift[0].Y > report.FeetUp)
                    (report.FeetUp, report.FeetUpAt) = (shift[0].Y, $"{entry.Name} frame {f}");
                if (onFeet && -shift[0].Y > report.FeetDown)
                    (report.FeetDown, report.FeetDownAt) = (-shift[0].Y, $"{entry.Name} frame {f}");
                if (lastShift != null)
                {
                    // What the rule's inputs can drive a group's correction (its goal less its clearance) by between the two
                    // frames: its clearance's change twice (its own and its goal's share of it), its target's change and its
                    // weight's change times how far its target is from it.
                    var motion = 0.0;
                    for (var g = 0; g < before.Length; g++)
                    {
                        var (k0, k1, c0, c1) = (contacts.Keyed[f - 1][g], keyed[g], lastBefore![g], before[g]);
                        if (double.IsInfinity(c0) || double.IsInfinity(c1) || double.IsInfinity(k0) || double.IsInfinity(k1))
                            continue;
                        var (t0, t1) = (ContactRule.Target(k0, ratio), ContactRule.Target(k1, ratio));
                        motion = Math.Max(motion, 2 * Math.Abs(c1 - c0) + Math.Abs(t1 - t0)
                            + Math.Abs(ContactRule.Weight(k1) - ContactRule.Weight(k0)) * Math.Max(Math.Abs(t0 - c0), Math.Abs(t1 - c1)));
                    }
                    var step = shift.Select((s, i) => (double)(s - lastShift[i]).Length()).Max();
                    if (step > report.Step)
                        (report.Step, report.StepAt) = (step, $"{entry.Name} frame {f}");
                    report.StepOverMotion = Math.Max(report.StepOverMotion, step - motion);
                    report.Ratio = Math.Max(report.Ratio, step / (motion + 0.0005));
                }
                (lastBefore, lastShift) = (before, shift);
            }
            report.Lines.Add($"  {entry.Name}: lowest {worstHere * 1000:F2} mm over the ground; {kept.Contacts?.Changed ?? 0} keys changed, {kept.Contacts?.Clamped ?? 0} clamped, {kept.Contacts?.Conflicts ?? 0} conflicts, the hips moved up to {(kept.Contacts?.LargestHipsMove ?? 0) * 1000:F1} mm");
        }
        return report;
    }
}
