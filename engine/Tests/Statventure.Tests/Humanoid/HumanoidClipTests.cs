using System.Numerics;
using System.Text.Json;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Sandbox.Figure;
using Xunit.Abstractions;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// The humanoid family's clips (GM2 step 4b; Docs/Game/Hero.md, "How it works", step 4b, and its clip table): the 34
/// clips the manager keyed and the user approved (2026-11-17 and 18), their files in Tools/Models/humanoid/clips/ as the
/// hand-over gave them, their library Assets/Models/humanoid/clips.glb (models.pak's models/humanoid/clips).
/// </summary>
public static class HumanoidClipList
{
    /// <summary>A clip of the list: its frames at 60 fps, whether it loops, how many bones it turns, its markers (event, frame).</summary>
    public sealed record Entry(string Name, int Frames, bool Loop, int Bones, params (string Event, int Frame)[] Markers)
    {
        public double Seconds => Frames / 60.0;
        /// <summary>A stance loop of the arms alone (GM2-P21).</summary>
        public bool Stance => Name.StartsWith("hold-", StringComparison.Ordinal);
    }

    /// <summary>
    /// The hand-over's list (claude-work/gm2/step4/handover-clips/list.txt, the manager's, 2026-11-18), clip for clip: a clip
    /// added, dropped or renamed, or one whose length, loop, bones or markers moved, fails here until the list is changed
    /// with it.
    /// </summary>
    public static readonly Entry[] All =
    [
        new("idle", 120, true, 23),
        new("stunned", 90, true, 23),
        new("advance", 28, false, 21, ("step", 11), ("step2", 18)),
        new("return", 28, false, 21, ("land", 19)),
        new("flee", 32, true, 19, ("step", 0), ("step2", 16)),
        new("stand", 240, true, 21),
        new("hurt", 24, false, 23),
        new("critical-hurt", 44, false, 23),
        new("stumble", 46, false, 21, ("step", 8)),
        new("death", 60, false, 21, ("fall", 26), ("down", 34)),
        new("swing", 46, false, 21, ("hit", 17), ("step", 15)),
        new("thrust", 40, false, 21, ("hit", 14), ("step", 13)),
        new("stab", 30, false, 21, ("hit", 9), ("step", 8)),
        new("strike", 48, false, 21, ("hit", 18), ("step", 16)),
        new("punch", 32, false, 21, ("hit", 9), ("step", 8)),
        new("swing-off-hand", 40, false, 21, ("hit", 13), ("step", 12)),
        new("swing-two-handed", 48, false, 21, ("hit", 17), ("step", 15)),
        new("bow", 70, false, 21, ("shoot", 36)),
        new("crossbow", 58, false, 21, ("shoot", 26)),
        new("cast", 54, false, 21, ("cast", 24)),
        new("skill", 50, false, 21, ("skill", 17)),
        new("item", 46, false, 21, ("use", 23)),
        new("drink", 76, false, 21, ("drink", 32)),
        new("meditate", 86, false, 23, ("restore", 42)),
        new("guard", 14, false, 23, ("guard", 9)),
        new("guard-hold", 120, true, 23),
        new("evade", 30, false, 21, ("leap", 6), ("land", 15), ("evade", 19)),
        new("evade-hold", 96, true, 21),
        new("victory", 68, false, 21, ("cheer", 14), ("land", 22)),
        new("hold-two-handed", 120, true, 12),
        new("hold-staff", 120, true, 12),
        new("hold-unarmed", 120, true, 12),
        new("hold-bow", 120, true, 12),
        new("hold-crossbow", 120, true, 12),
    ];

    public static Entry Of(string name) => All.Single(e => e.Name == name);

    /// <summary>The family's clip files (library.py's form), Tools/Models/humanoid/clips/&lt;name&gt;.json.</summary>
    public static string Folder => Path.Combine(HumanoidFiles.Scripts, "clips");

    /// <summary>The family's clip library, models.pak's models/humanoid/clips.</summary>
    public static string Library => HumanoidFiles.Glb("clips");

    static AnimationClip[]? _clips;

    /// <summary>The library's clips.</summary>
    public static AnimationClip[] Clips() => _clips ??= AnimationClip.AllOf(HumanoidFiles.Load("clips"));

    public static AnimationClip Clip(string name) => Clips().Single(c => c.Name == name);

    /// <summary>The bones of the arms a stance loop turns (GM2-P21): shoulder, upper arm, forearm, hand, fingers and thumb, each side.</summary>
    public static readonly string[] Arms =
        [.. new[] { "L", "R" }.SelectMany(side => new[] { "shoulder", "upper_arm", "forearm", "hand", "fingers", "thumb" }.Select(b => $"{b}.{side}"))];

    /// <summary>What every clip but the stance loops turns, at the least (the whole body): the trunk and the head, both legs, both arms.</summary>
    public static readonly string[] WholeBody =
    [
        "hips", "spine", "chest", "neck", "head",
        .. new[] { "L", "R" }.SelectMany(side => new[] { "thigh", "shin", "foot", "upper_arm", "forearm" }.Select(b => $"{b}.{side}")),
    ];
}

/// <summary>
/// The clip library against the hand-over's list and the rules the clips were keyed by (GM2 step 4b): every clip of the
/// list with its frames, loop and markers; a loop's end its start; the stance loops the arms alone, every other clip
/// the whole body; guard and evade ending where their holds begin; the man's lowest point never under the ground.
/// </summary>
public class HumanoidClipTests(ITestOutputHelper output)
{
    const double Fps = 60;

    static JsonElement File(string name)
    {
        using var document = JsonDocument.Parse(System.IO.File.ReadAllText(Path.Combine(HumanoidClipList.Folder, name + ".json")));
        return document.RootElement.Clone();
    }

    /// <summary>
    /// The library holds the list's clips and no others, as the clips' folder does; each its length (frames at 60 fps),
    /// its markers as events at their frames, its bones turned, no scale, a location on the hips alone (none in a stance
    /// loop); each file's own length, loop and markers the list's.
    /// </summary>
    [Fact]
    public void TheLibraryHoldsEveryClipOfTheList()
    {
        var names = HumanoidClipList.All.Select(e => e.Name).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(34, names.Count);
        Assert.Equal(names, HumanoidClipList.Clips().Select(c => c.Name).Order(StringComparer.Ordinal));
        Assert.Equal(names, Directory.GetFiles(HumanoidClipList.Folder, "*.json").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal));
        Assert.Empty(HumanoidFiles.Load("clips").Meshes);
        foreach (var entry in HumanoidClipList.All)
        {
            var clip = HumanoidClipList.Clip(entry.Name);
            Assert.True(Math.Abs(clip.Duration - entry.Seconds) < 1e-6, $"{entry.Name}: {clip.Duration * Fps} frames, not {entry.Frames}");
            var markers = entry.Markers.OrderBy(m => m.Frame).ThenBy(m => m.Event, StringComparer.Ordinal).Select(m => (m.Event, (double)m.Frame)).ToList();
            Assert.Equal(markers, clip.Events.Select(e => (e.Name, Math.Round(e.Time * Fps, 6))));
            Assert.True(clip.Tracks.Count(t => t.Rotation != null) == entry.Bones, $"{entry.Name}: {clip.Tracks.Count(t => t.Rotation != null)} bones turned, not {entry.Bones}");
            Assert.All(clip.Tracks, t => Assert.True(t.Relative && t.Scale == null, $"{entry.Name}: {t.Node}"));
            Assert.Equal(entry.Stance ? Array.Empty<string>() : ["hips"], clip.Tracks.Where(t => t.Translation != null).Select(t => t.Node));
            var file = File(entry.Name);
            Assert.Equal((entry.Name, entry.Frames, entry.Loop), (file.GetProperty("name").GetString(), file.GetProperty("frames").GetInt32(), file.GetProperty("loop").GetBoolean()));
            Assert.Equal(entry.Markers.Select(m => (m.Event, m.Frame)).Order(), file.GetProperty("markers").EnumerateObject().Select(m => (m.Name, m.Value.GetInt32())).Order());
        }
    }

    /// <summary>
    /// A loop's last key is its first: in its file (every bone's value, as library.py checks it before keying) and in the
    /// library (every channel's sample at its end the one at its start), so it plays round without a jump. The list's loops
    /// are idle, stunned, flee, stand, guard-hold, evade-hold and the five stance loops.
    /// </summary>
    [Fact]
    public void ALoopEndsWhereItStarts()
    {
        Assert.Equal(["evade-hold", "flee", "guard-hold", "hold-bow", "hold-crossbow", "hold-staff", "hold-two-handed", "hold-unarmed", "idle", "stand", "stunned"],
            HumanoidClipList.All.Where(e => e.Loop).Select(e => e.Name).Order(StringComparer.Ordinal));
        Span<double> a = stackalloc double[4], b = stackalloc double[4];
        foreach (var entry in HumanoidClipList.All.Where(e => e.Loop))
        {
            var keys = File(entry.Name).GetProperty("keys");
            var (first, last) = (keys[0], keys[keys.GetArrayLength() - 1]);
            Assert.Equal((0, entry.Frames), (first.GetProperty("frame").GetInt32(), last.GetProperty("frame").GetInt32()));
            foreach (var channel in new[] { "rotations", "locations" })
                Assert.Equal(first.TryGetProperty(channel, out var f) ? f.GetRawText() : "", last.TryGetProperty(channel, out var l) ? l.GetRawText() : "");
            var clip = HumanoidClipList.Clip(entry.Name);
            foreach (var track in clip.Tracks)
                foreach (var curve in new[] { track.Translation, track.Rotation }.OfType<AnimationCurve>())
                {
                    curve.Evaluate(0, a);
                    curve.Evaluate(clip.Duration, b);
                    var gap = curve.Path == AnimationPath.Rotation
                        ? Angle(a, b)
                        : Math.Sqrt((a[0] - b[0]) * (a[0] - b[0]) + (a[1] - b[1]) * (a[1] - b[1]) + (a[2] - b[2]) * (a[2] - b[2]));
                    Assert.True(gap < 1e-6, $"{entry.Name}: {track.Node}'s {curve.Path} ends {gap} from where it starts");
                }
        }
    }

    /// <summary>
    /// The five stance loops (GM2-P21) turn the arms alone, the 12 bones of the shoulders, upper arms, forearms, hands,
    /// fingers and thumbs, and move nothing; every other clip turns the whole body (the trunk and the head, both legs, both
    /// arms) and moves the hips.
    /// </summary>
    [Fact]
    public void TheStanceLoopsTurnTheArmsAloneAndEveryOtherClipTheWholeBody()
    {
        Assert.Equal(["hold-bow", "hold-crossbow", "hold-staff", "hold-two-handed", "hold-unarmed"],
            HumanoidClipList.All.Where(e => e.Stance).Select(e => e.Name).Order(StringComparer.Ordinal));
        Assert.Equal(12, HumanoidClipList.Arms.Length);
        foreach (var entry in HumanoidClipList.All)
        {
            var clip = HumanoidClipList.Clip(entry.Name);
            var turned = clip.Tracks.Where(t => t.Rotation != null).Select(t => t.Node).ToHashSet();
            var moved = clip.Tracks.Where(t => t.Translation != null).Select(t => t.Node).ToList();
            if (entry.Stance)
            {
                Assert.True(turned.SetEquals(HumanoidClipList.Arms), $"{entry.Name} turns {string.Join(", ", turned.Order(StringComparer.Ordinal))}");
                Assert.True(moved.Count == 0, $"{entry.Name} moves {string.Join(", ", moved)}");
            }
            else
            {
                var missing = HumanoidClipList.WholeBody.Where(b => !turned.Contains(b)).ToList();
                Assert.True(missing.Count == 0, $"{entry.Name} doesn't turn {string.Join(", ", missing)}");
                Assert.True(moved is ["hips"], $"{entry.Name} moves {string.Join(", ", moved)}");
            }
        }
    }

    /// <summary>
    /// Every clip of the list is held to Blender's evaluation on both bodies (Animation/AnimationReferenceTests, the scenes
    /// humanoid-&lt;clip&gt;-male and -female: the library played by bone names on the man's and the woman's), at times that
    /// hold each of its marker's frames, its first and last frame and a time between frames, Blender's reference
    /// evaluated at those very times.
    /// </summary>
    [Fact]
    public void EveryClipIsHeldToBlenderOnBothBodiesAtItsMarkers()
    {
        foreach (var entry in HumanoidClipList.All)
            foreach (var sex in new[] { "male", "female" })
            {
                var name = $"humanoid-{entry.Name}-{sex}";
                var scene = Animation.AnimationScenes.All.SingleOrDefault(s => s.Name == name);
                Assert.True(scene != null, $"no scene {name}");
                Assert.Equal(("humanoid/clips", entry.Name, $"humanoid/bodies/human-{sex}-average"), (scene.Model, scene.Clip, scene.Target));
                var frames = scene.Times.Select(t => t * Fps).ToList();
                foreach (var frame in entry.Markers.Select(m => m.Frame).Append(0).Append(entry.Frames))
                    Assert.True(frames.Any(f => Math.Abs(f - frame) < 1e-6), $"{name}: no time at frame {frame}");
                Assert.Contains(frames, f => Math.Abs(f - Math.Round(f)) > 0.25);
                using var golden = JsonDocument.Parse(System.IO.File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Infrastructure.Golden.GoldenPath("x"))!, $"anim-{name}.json")));
                Assert.Equal(scene.Times, golden.RootElement.GetProperty("times").EnumerateArray().Select(t => t.GetDouble()));
            }
    }

    /// <summary>
    /// A stance loop laid over the idle (GM2-P21; the figure mode's "idle+hold-…", a layer above replacing only the
    /// channels its clip keys, Blender's NLA rule, no engine code of its own) turns the arms as the stance does and leaves
    /// the rest of the body to the idle, on both bodies, each of the five stances at times through its round. (Over clips
    /// that key the arms themselves the battle needs a bone mask: GM5's.)
    /// </summary>
    [Theory]
    [InlineData("male")]
    [InlineData("female")]
    public void AStanceLaidOverTheIdleTurnsTheArmsAlone(string sex)
    {
        var body = HumanoidFiles.Load($"bodies/human-{sex}-average");
        var skeleton = new Skeleton(body);
        var arms = HumanoidClipList.Arms.Select(skeleton.Find).ToHashSet();
        foreach (var stance in HumanoidClipList.All.Where(e => e.Stance))
        {
            var clips = new FigureClips(HumanoidClipList.Clips(), FigurePlay.Parse($"idle+{stance.Name}"), "clips.glb");
            var stack = new AnimationStack(skeleton);
            stack.Layers.Add(clips.Layer(skeleton));
            stack.Layers.Add(clips.OverLayer(skeleton)!);
            var buffer = new PoseBuffer(skeleton);
            var (idle, held) = (new ClipBinding(HumanoidClipList.Clip("idle"), skeleton), new ClipBinding(HumanoidClipList.Clip(stance.Name), skeleton));
            foreach (var t in new[] { 0.0, 0.35, 1.0, 1.72 })
            {
                stack.Evaluate(t, buffer);
                var actual = buffer.ToPose();
                var (under, over) = (idle.Pose(t), held.Pose(t));
                for (var n = 0; n < skeleton.Count; n++)
                {
                    var name = $"{sex} {stance.Name} at {t} s: {body.Nodes[n].Name}";
                    var source = arms.Contains(n) ? over : under;
                    Assert.True(source.TryGet(n, out var wanted) == actual.TryGet(n, out var got), $"{name} set or not as its clip");
                    if (source.TryGet(n, out _))
                        Assert.True(Math.Abs(Quaternion.Dot(wanted.Rotation, got.Rotation)) > 1 - 1e-6f && Vector3.Distance(wanted.Translation, got.Translation) < 1e-6f,
                            $"{name} isn't {(arms.Contains(n) ? stance.Name : "idle")}'s");
                }
                // The stance does turn the arms away from the idle's.
                Assert.Contains(arms, n => under.TryGet(n, out var a) && over.TryGet(n, out var b) && Math.Abs(Quaternion.Dot(a.Rotation, b.Rotation)) < 0.999f);
            }
        }
    }

    /// <summary>
    /// A clip entered and then held ends where its hold begins (guard then guard-hold, evade then evade-hold): every bone
    /// either turns within <see cref="HoldJoinDegrees"/> of the other's (a bone one of them leaves alone at rest there), the
    /// hips' place within <see cref="HoldJoinMetres"/>.
    /// </summary>
    [Theory]
    [InlineData("guard", "guard-hold")]
    [InlineData("evade", "evade-hold")]
    public void AClipEndsWhereItsHoldBegins(string clip, string hold)
    {
        var (end, start) = (HumanoidClipList.Clip(clip), HumanoidClipList.Clip(hold));
        var (angle, bone, gap) = Join(end, end.Duration, start, 0);
        output.WriteLine($"{clip} → {hold}: the largest turn apart {angle:F3}° ({bone}), the hips {gap * 1000:F3} mm apart");
        Assert.True(angle <= HoldJoinDegrees, $"{clip} ends {angle:F3}° from where {hold} begins ({bone})");
        Assert.True(gap <= HoldJoinMetres, $"{clip} ends with the hips {gap * 1000:F3} mm from where {hold} begins");
    }

    /// <summary>
    /// The bounds for <see cref="AClipEndsWhereItsHoldBegins"/> (2026-11-18, the clips as re-baked that day): measured at most
    /// 0.097° for guard and 0.369° for evade (both the left upper arm), the hips' place the same to the micrometre; the
    /// angle's bound the worst rounded up to a tenth of a degree (the manager's check had found them within 0.5°), the
    /// place's a tenth of a millimetre.
    /// </summary>
    public const double HoldJoinDegrees = 0.4, HoldJoinMetres = 0.0001;

    /// <summary>The largest angle between two clips' turns of one bone at two times (a bone one clip leaves alone at rest), and the hips' distance.</summary>
    static (double Degrees, string Bone, double Metres) Join(AnimationClip a, double at, AnimationClip b, double bt)
    {
        static double[] Turn(AnimationClip clip, string node, double time)
        {
            var v = new double[] { 0, 0, 0, 1 };
            clip.Tracks.SingleOrDefault(t => t.Node == node)?.Rotation?.Evaluate(time, v);
            return v;
        }
        static double[] Place(AnimationClip clip, double time)
        {
            var v = new double[4];
            clip.Tracks.SingleOrDefault(t => t.Node == "hips")?.Translation?.Evaluate(time, v);
            return v;
        }
        var (worst, which) = (0.0, "");
        foreach (var node in a.Tracks.Concat(b.Tracks).Where(t => t.Rotation != null).Select(t => t.Node).Distinct())
        {
            var angle = Angle(Turn(a, node, at), Turn(b, node, bt)) * 180 / Math.PI;
            if (angle > worst)
                (worst, which) = (angle, node);
        }
        var (pa, pb) = (Place(a, at), Place(b, bt));
        return (worst, which, Math.Sqrt((pa[0] - pb[0]) * (pa[0] - pb[0]) + (pa[1] - pb[1]) * (pa[1] - pb[1]) + (pa[2] - pb[2]) * (pa[2] - pb[2])));
    }

    /// <summary>
    /// The angle (radians) between two rotations given as quaternions (x, y, z, w; not quite unit length, as sampled float32
    /// keys are), each normalised in double: twice the angle of the one that takes the first to the second, which atan2
    /// keeps exact near 0 where an arccosine of their dot product can't (a float32 key's length a few 10⁻⁸ off 1 is already
    /// 0.02° there).
    /// </summary>
    static double Angle(ReadOnlySpan<double> p, ReadOnlySpan<double> q)
    {
        var (lp, lq) = (Math.Sqrt(p[0] * p[0] + p[1] * p[1] + p[2] * p[2] + p[3] * p[3]), Math.Sqrt(q[0] * q[0] + q[1] * q[1] + q[2] * q[2] + q[3] * q[3]));
        double px = p[0] / lp, py = p[1] / lp, pz = p[2] / lp, pw = p[3] / lp, qx = q[0] / lq, qy = q[1] / lq, qz = q[2] / lq, qw = q[3] / lq;
        // conj(p) · q
        var w = pw * qw + px * qx + py * qy + pz * qz;
        var x = pw * qx - px * qw - py * qz + pz * qy;
        var y = pw * qy + px * qz - py * qw - pz * qx;
        var z = pw * qz - px * qy + py * qx - pz * qw;
        return 2 * Math.Atan2(Math.Sqrt(x * x + y * y + z * z), Math.Abs(w));
    }

    /// <summary>
    /// The man (the body the clips are keyed on) never goes under the ground: the ground is where his soles are at rest
    /// (his body's lowest point there, 24.0 mm under the origin's plane: the boots' soles, which reach under the bare feet;
    /// the manager, 2026-11-18), and in every clip that moves the whole body, at every frame, the lowest point of his body
    /// (its skin and plain outfit as the engine skins them) is at most <see cref="GroundDepth"/> under it; death's fallen
    /// body lies on it (its last frame's lowest point from <see cref="GroundDepth"/> under it to <see cref="LyingHeight"/>
    /// over it). Measured on the woman too and written out, with no bound: her legs are another length and a clip's hips'
    /// place isn't scaled by it yet (GM2-P22).
    /// </summary>
    [Fact]
    public void TheManStaysOnTheGround()
    {
        var lines = new List<string>();
        foreach (var sex in new[] { "male", "female" })
        {
            var body = HumanoidFiles.Load($"bodies/human-{sex}-average");
            var skin = new Skinner(body);
            var ground = skin.LowestWhere(body.WorldMatrices()).Y;
            lines.Add($"{sex}: the soles at rest {ground * 1000:F2} mm (the ground)");
            if (sex == "male")
                Assert.True(Math.Abs(ground + 0.024) < 0.0005, $"the man's soles rest at {ground * 1000:F2} mm, not -24.0");
            var skeleton = new Skeleton(body);
            foreach (var entry in HumanoidClipList.All.Where(e => !e.Stance))
            {
                var binding = new ClipBinding(HumanoidClipList.Clip(entry.Name), skeleton);
                var (lowest, at, where, last) = (double.MaxValue, -1, "", 0.0);
                for (var frame = 0; frame <= entry.Frames; frame++)
                {
                    var (y, bone) = skin.LowestWhere(body.WorldMatrices(binding.Pose(frame / Fps)));
                    if (y < lowest)
                        (lowest, at, where) = (y, frame, bone);
                    last = y;
                }
                var (depth, end) = (ground - lowest, last - ground);
                lines.Add($"{sex} {entry.Name}: at most {depth * 1000:F2} mm under the ground (frame {at}, {where}), at its end {end * 1000:F2} mm over it");
                if (sex != "male")
                    continue;
                Assert.True(depth <= GroundDepth, $"{entry.Name}: the man's lowest point {depth * 1000:F2} mm under the ground at frame {at} ({where})");
                if (entry.Name == "death")
                    Assert.True(end >= -GroundDepth && end <= LyingHeight, $"death: the fallen man's lowest point {end * 1000:F2} mm over the ground");
            }
        }
        foreach (var line in lines)
            output.WriteLine(line);
    }

    /// <summary>
    /// How far under the ground the man's lowest point may go (2026-11-18, the clips as re-baked that day, the manager's
    /// keying raising a foot by what its boot would dip past 2 mm): measured at most 2.21 mm (meditate's kneeling knee,
    /// frame 22), 2.00–2.01 in the clips the re-bake raised a foot in, death's end 1.92 mm under; the bound 3 mm.
    /// </summary>
    public const double GroundDepth = 0.003;

    /// <summary>How far over the ground death's fallen body may end (measured 1.92 mm under it; the bound the manager set, 5 mm).</summary>
    public const double LyingHeight = 0.005;

    /// <summary>A body's vertices skinned on the CPU as skin.comp skins them (linear blend, four joints), for its lowest point.</summary>
    sealed class Skinner
    {
        readonly ModelData _model;
        readonly List<(Vector3 Position, int[] Joints, float[] Weights, int Skin)> _vertices = [];

        public Skinner(ModelData model)
        {
            _model = model;
            foreach (var node in model.Nodes.Where(n => n.Mesh >= 0 && n.Skin >= 0))
                foreach (var primitive in model.Meshes[node.Mesh].Primitives)
                    for (var v = 0; v < primitive.Positions.Length; v++)
                    {
                        var (j, w) = (primitive.Joints![v], primitive.Weights![v]);
                        _vertices.Add((primitive.Positions[v], [j.J0, j.J1, j.J2, j.J3], [w.X, w.Y, w.Z, w.W], node.Skin));
                    }
            Assert.True(_vertices.Count > 1000, "no skinned body");
        }

        /// <summary>The lowest Y of the skinned vertices and the bone its vertex follows most.</summary>
        public (double Y, string Bone) LowestWhere(Matrix4x4[] world)
        {
            var matrices = _model.Skins.Select(s => s.Joints.Select((joint, i) => s.InverseBindMatrices[i] * world[joint]).ToArray()).ToArray();
            var (lowest, bone) = (double.MaxValue, "");
            foreach (var (position, joints, weights, skin) in _vertices)
            {
                var y = 0.0;
                for (var k = 0; k < 4; k++)
                    if (weights[k] != 0)
                        y += weights[k] * Vector3.Transform(position, matrices[skin][joints[k]]).Y;
                if (y < lowest)
                {
                    lowest = y;
                    var most = Array.IndexOf(weights, weights.Max());
                    bone = _model.Nodes[_model.Skins[skin].Joints[joints[most]]].Name;
                }
            }
            return (lowest, bone);
        }
    }
}
