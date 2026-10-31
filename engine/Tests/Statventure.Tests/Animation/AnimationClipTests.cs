using System.Numerics;
using System.Text.Json;
using Statventure.Animation;
using Statventure.Render3D;

namespace Statventure.Tests.Animation;

/// <summary>
/// Clips read from a hand-written glTF file and sampled by glTF 2.0's Appendix C: what Blender's
/// sampled export doesn't produce (CUBICSPLINE, quantized rotations, channels on plain nodes) and the
/// formulas themselves (slerp the short way, tangents scaled by the segment's duration, clamping),
/// each checked against values worked out by hand.
/// </summary>
public class AnimationClipTests
{
    static readonly Quaternion Quarter = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathF.PI / 2);

    [Fact]
    public void StepKeysHoldUntilTheNextKey()
    {
        var (model, clips) = Load();
        var binding = new ClipBinding(clips.Single(c => c.Name == "step"), new Skeleton(model));
        var turned = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
        Quaternion At(double time) => binding.Pose(time).TryGet(1, out var local) ? local.Rotation : default;
        Near(Quaternion.Identity, At(-1));
        Near(Quaternion.Identity, At(0.99));
        Near(turned, At(1));
        Near(turned, At(1.5));
        Assert.Equal(1f, clips.Single(c => c.Name == "step").Duration);
    }

    [Fact]
    public void LinearRotationsTakeTheShortWay()
    {
        var (model, clips) = Load();
        // Keys: no turn, then 120° about Z stored with its sign flipped (w < 0, dot product −0.5). glTF's
        // slerp goes the short way: a quarter of the way is 30° about Z (the long way would be −60°).
        var pose = new ClipBinding(clips.Single(c => c.Name == "linear"), new Skeleton(model)).Pose(0.25);
        Assert.True(pose.TryGet(1, out var local));
        Near(Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 6), local.Rotation);
        // Translations lerp: a quarter of (2, 4, 6).
        Near(new Vector3(0.5f, 1, 1.5f), local.Translation);
        // An exact slerp, not a normalised lerp: a fifth of the way is 24°.
        Near(Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 24 * MathF.PI / 180), new ClipBinding(clips.Single(c => c.Name == "linear"), new Skeleton(model)).Pose(0.2).TryGet(1, out var fifth) ? fifth.Rotation : default);
    }

    [Fact]
    public void CubicSplinesScaleTheirTangentsByTheSegmentsDuration()
    {
        var (model, clips) = Load();
        var binding = new ClipBinding(clips.Single(c => c.Name == "cubic"), new Skeleton(model));
        // Keys at 0 s and 2 s: values (0, 0, 0) and (1, 0, 0), out-tangent (1, 0, 0) at the first,
        // in-tangent (0, 4, 0) at the second, per second. Half way (t = 0.5, t_d = 2):
        // 0.5·v0 + 2·0.125·b0 + 0.5·v1 + 2·(−0.125)·a1 = (0.75, −1, 0).
        Assert.True(binding.Pose(1).TryGet(1, out var half));
        Near(new Vector3(0.75f, -1, 0), half.Translation);
        // Rotations: no turn to 90° about X with zero tangents; half way the Hermite mix is half of each,
        // normalised: 45° about X.
        Near(Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 4), half.Rotation);
        // The curve itself gives a unit quaternion (glTF: normalised before it's used; layers blend it).
        var rotation = clips.Single(c => c.Name == "cubic").Tracks.Single(t => t.Node == "bone").Rotation!;
        Assert.Equal(1, Math.Sqrt(Value(rotation, 1).Sum(v => v * v)), 6);
        // Before the first key: its value, not its in-tangent (the in-tangent there is (9, 9, 9)).
        Assert.True(binding.Pose(-1).TryGet(1, out var before));
        Near(Vector3.Zero, before.Translation);
        Assert.True(binding.Pose(5).TryGet(1, out var after));
        Near(new Vector3(1, 0, 0), after.Translation);
    }

    [Fact]
    public void QuantizedRotationsDecodeAsNormalisedIntegers()
    {
        var (model, clips) = Load();
        // Signed shorts (0, 23170, 0, 23170) over 32767: 90° about Y.
        Assert.True(new ClipBinding(clips.Single(c => c.Name == "quantized"), new Skeleton(model)).Pose(0).TryGet(1, out var local));
        Assert.True(Math.Abs(Quaternion.Dot(Quarter, local.Rotation)) > 1 - 1e-7);
    }

    [Fact]
    public void ABonesChannelsAreRelativeToItsRestAndAPlainNodesAreItsOwn()
    {
        var (model, clips) = Load();
        var linear = clips.Single(c => c.Name == "linear");
        // The bone (node 1) rests at (0, 1, 0), turned 90° about Y, scaled 2: its first translation key
        // (0, 0, 0) is, relative to that rest, R⁻¹·(0 − (0, 1, 0)) / 2 = (0, −0.5, 0).
        var bone = linear.Tracks.Single(t => t.Node == "bone");
        Assert.True(bone.Relative);
        Near(new Vector3(0, -0.5f, 0), new Vector3((float)Value(bone.Translation!, 0)[0], (float)Value(bone.Translation!, 0)[1], (float)Value(bone.Translation!, 0)[2]));
        // Its scale key 4 is twice its rest scale.
        Assert.Equal(2, Value(bone.Scale!, 0)[0], 6);
        // Played back on the same model the pose is the file's own local transform.
        Assert.True(new ClipBinding(linear, new Skeleton(model)).Pose(0).TryGet(1, out var local));
        Near(Vector3.Zero, local.Translation);
        Near(new Vector3(4, 4, 4), local.Scale);
        // The root is no joint: its keys are its local transform as they stand, (1, 2, 3) to (3, 2, 3).
        var root = linear.Tracks.Single(t => t.Node == "root");
        Assert.False(root.Relative);
        Assert.True(new ClipBinding(linear, new Skeleton(model)).Pose(0.5).TryGet(0, out var rootLocal));
        Near(new Vector3(2, 2, 3), rootLocal.Translation);
        // Its rotation and scale aren't animated: they stay the file's.
        Near(Quaternion.Identity, rootLocal.Rotation);
    }

    [Fact]
    public void ChannelsForNodesAModelLacksAreLeftOut()
    {
        var (model, clips) = Load();
        var other = new ModelData
        {
            Nodes = [new ModelNode("elsewhere", -1, [], Matrix4x4.Identity, Vector3.Zero, Quaternion.Identity, Vector3.One, -1, -1)],
            Meshes = [], Materials = [], Skins = [], Roots = [0],
        };
        var pose = new ClipBinding(clips.Single(c => c.Name == "linear"), new Skeleton(other)).Pose(0.5);
        Assert.False(pose.TryGet(0, out _));
        Assert.NotNull(model);
    }

    static double[] Value(AnimationCurve curve, double time)
    {
        var value = new double[curve.Components];
        curve.Evaluate(time, value);
        return value;
    }

    static void Near(Vector3 expected, Vector3 actual) =>
        Assert.True(Vector3.Distance(expected, actual) < 1e-5f, $"expected {expected}, got {actual}");

    /// <summary>The same rotation (q and −q are one).</summary>
    static void Near(Quaternion expected, Quaternion actual) =>
        Assert.True(Math.Abs(Quaternion.Dot(Quaternion.Normalize(expected), Quaternion.Normalize(actual))) > 1 - 1e-6f && Math.Abs(actual.Length() - 1) < 1e-5f,
            $"expected {expected}, got {actual}");

    /// <summary>
    /// The hand-written file: node 0 "root" (a plain node at (1, 2, 3)) and its child node 1 "bone"
    /// (a skin's joint resting at (0, 1, 0), turned 90° about Y, scaled 2), and four animations.
    /// </summary>
    static (ModelData Model, AnimationClip[] Clips) Load()
    {
        var file = new HandWrittenGltf();
        var rest = Quarter;
        var turned = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI / 2);
        var flipped = -Quaternion.CreateFromAxisAngle(Vector3.UnitZ, 2 * MathF.PI / 3);
        var animations = new object[]
        {
            file.Animation("step", (1, "rotation", "STEP", [0, 1], Q(Quaternion.Identity, turned))),
            file.Animation("linear",
                (1, "rotation", "LINEAR", [0, 1], Q(Quaternion.Identity, flipped)),
                (1, "translation", "LINEAR", [0, 1], [0, 0, 0, 2, 4, 6]),
                (1, "scale", "LINEAR", [0, 1], [4, 4, 4, 4, 4, 4]),
                (0, "translation", "LINEAR", [0, 1], [1, 2, 3, 3, 2, 3])),
            file.Animation("cubic",
                (1, "translation", "CUBICSPLINE", [0, 2], [9, 9, 9, 0, 0, 0, 1, 0, 0, /**/ 0, 4, 0, 1, 0, 0, 0, 0, 0]),
                (1, "rotation", "CUBICSPLINE", [0, 2], [0, 0, 0, 0, .. Q(Quaternion.Identity), 0, 0, 0, 0, /**/ 0, 0, 0, 0, .. Q(turned), 0, 0, 0, 0])),
            file.Animation("quantized", (1, "rotation", "LINEAR", [0, 1], null, [0, 23170, 0, 23170, 0, 23170, 0, 23170])),
        };
        var gltf = new Dictionary<string, object>
        {
            ["asset"] = new { version = "2.0" },
            ["scene"] = 0,
            ["scenes"] = new[] { new { nodes = new[] { 0 } } },
            ["nodes"] = new object[]
            {
                new { name = "root", translation = new float[] { 1, 2, 3 }, children = new[] { 1 } },
                new { name = "bone", translation = new float[] { 0, 1, 0 }, rotation = Q(rest), scale = new float[] { 2, 2, 2 } },
            },
            ["skins"] = new[] { new { joints = new[] { 1 } } },
            ["animations"] = animations,
        };
        file.Finish(gltf);
        var folder = Directory.CreateTempSubdirectory("statventure-anim-");
        try
        {
            var path = Path.Combine(folder.FullName, "clips.gltf");
            File.WriteAllText(path, JsonSerializer.Serialize(gltf));
            var model = ModelData.Load(path);
            return (model, AnimationClip.AllOf(model));
        }
        finally
        {
            folder.Delete(true);
        }
    }

    static float[] Q(params Quaternion[] quaternions) => quaternions.SelectMany(q => new[] { q.X, q.Y, q.Z, q.W }).ToArray();

    /// <summary>Accessors, buffer views and one data-URI buffer for a hand-written .gltf.</summary>
    sealed class HandWrittenGltf
    {
        readonly MemoryStream _buffer = new();
        readonly List<object> _views = [];
        readonly List<object> _accessors = [];

        int Accessor(float[]? floats, short[]? shorts, int count, string type, bool times)
        {
            var offset = (int)_buffer.Length;
            var writer = new BinaryWriter(_buffer);
            if (floats != null)
                foreach (var f in floats)
                    writer.Write(f);
            else
                foreach (var s in shorts!)
                    writer.Write(s);
            _views.Add(new { buffer = 0, byteOffset = offset, byteLength = (int)_buffer.Length - offset });
            while (_buffer.Length % 4 != 0)
                _buffer.WriteByte(0);
            _accessors.Add(times
                ? new { bufferView = _views.Count - 1, componentType = 5126, count, type, min = new[] { floats!.Min() }, max = new[] { floats!.Max() } }
                : floats != null
                    ? new { bufferView = _views.Count - 1, componentType = 5126, count, type }
                    : (object)new { bufferView = _views.Count - 1, componentType = 5122, normalized = true, count, type });
            return _accessors.Count - 1;
        }

        public object Animation(string name, params (int Node, string Path, string Interpolation, float[] Times, float[]? Values, short[]? Shorts)[] channels)
        {
            var samplers = new List<object>();
            var list = new List<object>();
            foreach (var c in channels)
            {
                var type = c.Path == "rotation" ? "VEC4" : "VEC3";
                var components = c.Path == "rotation" ? 4 : 3;
                var input = Accessor(c.Times, null, c.Times.Length, "SCALAR", true);
                var count = (c.Values?.Length ?? c.Shorts!.Length) / components;
                var output = Accessor(c.Values, c.Shorts, count, type, false);
                samplers.Add(new { input, output, interpolation = c.Interpolation });
                list.Add(new { sampler = samplers.Count - 1, target = new { node = c.Node, path = c.Path } });
            }
            return new { name, channels = list, samplers };
        }

        public object Animation(string name, params (int Node, string Path, string Interpolation, float[] Times, float[] Values)[] channels) =>
            Animation(name, channels.Select(c => (c.Node, c.Path, c.Interpolation, c.Times, (float[]?)c.Values, (short[]?)null)).ToArray());

        public void Finish(Dictionary<string, object> gltf)
        {
            gltf["accessors"] = _accessors;
            gltf["bufferViews"] = _views;
            var bytes = _buffer.ToArray();
            gltf["buffers"] = new[] { new { byteLength = bytes.Length, uri = "data:application/octet-stream;base64," + Convert.ToBase64String(bytes) } };
        }
    }
}
