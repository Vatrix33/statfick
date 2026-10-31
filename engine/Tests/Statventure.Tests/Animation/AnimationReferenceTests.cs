using System.Numerics;
using System.Text.Json;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Xunit.Abstractions;

namespace Statventure.Tests.Animation;

/// <summary>
/// The engine's clips pose the test models as Blender 5.2 evaluates the same actions
/// (Golden/anim-&lt;scene&gt;.json, from Tools/Scripts/make-blender-references.sh, which evaluates
/// Reference/anim-scenes.json). On the clip's frames every bone's rest head and tail must land where
/// Blender's deformation (posed after the inverse of rest, model space) takes them, exactly: Blender
/// evaluates the keys the exporter sampled. Between frames Blender evaluates its curves while the
/// engine interpolates each channel's samples as glTF says, which errs by what interpolating samples
/// h apart errs: at most h²/8 times the curve's second derivative. So each bone is judged by its own
/// channels: its head and tail in its parent's posed frame (where only its own translation, rotation
/// and scale move them) may be off by 1.5/8 of the larger second difference of those points at the
/// frames around (h² f'' there), the 1.5 a margin for f'' changing within the frame. (Points in
/// model space would mix the bones of a chain, whose curvatures can cancel: an IK chain's end follows
/// its target smoothly while each bone's rotation bends.)
/// </summary>
public class AnimationReferenceTests(ITestOutputHelper output)
{
    /// <summary>Metres a bone's end may be off on a frame: float rounding (the exporter's float32 samples, Blender's float32 matrices).</summary>
    public const double OnFrameTolerance = 2e-5;
    /// <summary>Of the second difference over 8 that a point between frames may be off (Docs/Animation.md, "Status").</summary>
    public const double BetweenFramesMargin = 1.5;

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");
    static string GoldenDirectory => Path.GetDirectoryName(Golden.GoldenPath("x"))!;

    public static TheoryData<string> SceneNames => new(AnimationScenes.All.Select(s => s.Name));

    [Fact]
    public void TheSceneFileIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "anim-scenes.json");
        var text = JsonSerializer.Serialize(new
        {
            scenes = AnimationScenes.All.Select(s => new { name = s.Name, model = s.Model, clip = s.Clip, target = s.Target, times = s.Times }),
        }, new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull }) + "\n";
        if (!File.Exists(path) || File.ReadAllText(path) != text)
            File.WriteAllText(path, text);
        Assert.NotEmpty(AnimationScenes.All);
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void ClipsPoseTheModelAsBlenderEvaluatesItsActions(string name)
    {
        var scene = AnimationScenes.All.Single(s => s.Name == name);
        var result = Compare(scene);
        output.WriteLine($"{name}: on frames {result.OnFrame * 1000:F5} mm; between frames {result.Between * 1000:F4} mm, at most {result.Ratio:F2} of the allowance ({result.Worst})");
        Assert.True(result.OnFrame <= OnFrameTolerance, $"{name}: a bone is {result.OnFrame * 1000:F4} mm off on a frame ({result.Worst})");
        Assert.True(result.Ratio <= 1, $"{name}: a bone is off between frames by {result.Ratio:F2} times what interpolating the samples allows ({result.Worst})");
    }

    internal sealed record Comparison(double OnFrame, double Between, double Ratio, string Worst);

    /// <summary>
    /// How far bones land from Blender's: on frames the largest distance of a rest head or tail in
    /// model space; between frames the largest distance of a head or tail in its parent's frame, and
    /// the largest share of its allowance.
    /// </summary>
    internal static Comparison Compare(AnimationScene scene)
    {
        var reference = Load(scene.Name);
        var source = ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, scene.Model + ".glb"));
        var clip = AnimationClip.AllOf(source).Single(c => c.Name == scene.Clip);
        var model = scene.Target == null ? source : ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, scene.Target + ".glb"));
        var skeleton = new Skeleton(model);
        var binding = new ClipBinding(clip, skeleton);
        var rest = model.WorldMatrices();
        var inverseRest = rest.Select(Inverse).ToArray();
        var lastFrame = (int)Math.Round(clip.Duration * AnimationScenes.Fps);
        var frames = new Dictionary<int, Matrix4x4[]>();
        Matrix4x4[] Frame(int k) => frames.TryGetValue(k = Math.Clamp(k, 0, lastFrame), out var m) ? m : frames[k] = Deformations(binding.Pose((double)k / AnimationScenes.Fps));
        Matrix4x4[] Deformations(ModelPose pose) => model.WorldMatrices(pose).Select((m, i) => inverseRest[i] * m).ToArray();
        // A point of a bone in its parent's posed frame: through the bone's deformation, back through
        // the parent's, then into the parent's rest frame. Only the bone's own channels move it.
        Vector3 InParent(Vector3 point, Matrix4x4 bone, Matrix4x4 parent, int parentNode) =>
            Vector3.Transform(point, bone * Inverse(parent) * (parentNode >= 0 ? inverseRest[parentNode] : Matrix4x4.Identity));

        double onFrame = 0, between = 0, ratio = 0, worstScore = -1;
        var worst = "";
        for (var i = 0; i < scene.Times.Length; i++)
        {
            var time = scene.Times[i];
            var deformations = Deformations(binding.Pose(time));
            var frame = time * AnimationScenes.Fps;
            var exact = AnimationScene.OnFrame(time);
            var k = (int)Math.Floor(frame);
            foreach (var (bone, ends) in reference.Bones)
            {
                var node = skeleton.Find(bone);
                Assert.True(node >= 0, $"{scene.Name}: the model has no node {bone}");
                var blender = Deformation(reference.Poses[i][bone]);
                var parent = model.Nodes[node].Parent;
                var parentName = parent >= 0 ? model.Nodes[parent].Name : null;
                var blenderParent = parentName != null && reference.Poses[i].TryGetValue(parentName, out var p) ? Deformation(p) : Matrix4x4.Identity;
                foreach (var end in ends)
                {
                    double error, score;
                    if (exact)
                    {
                        error = Vector3.Distance(Vector3.Transform(end, deformations[node]), Vector3.Transform(end, blender));
                        onFrame = Math.Max(onFrame, error);
                        score = error / OnFrameTolerance;
                    }
                    else
                    {
                        Vector3 At(int f) => InParent(end, Frame(f)[node], parent >= 0 ? Frame(f)[parent] : Matrix4x4.Identity, parent);
                        error = Vector3.Distance(InParent(end, deformations[node], parent >= 0 ? deformations[parent] : Matrix4x4.Identity, parent),
                            InParent(end, blender, blenderParent, parent));
                        var curvature = Math.Max((At(k - 1) - 2 * At(k) + At(k + 1)).Length(), (At(k) - 2 * At(k + 1) + At(k + 2)).Length());
                        var allowed = BetweenFramesMargin * curvature / 8 + OnFrameTolerance;
                        between = Math.Max(between, error);
                        score = error / allowed;
                        ratio = Math.Max(ratio, score);
                    }
                    if (score > worstScore)
                    {
                        worstScore = score;
                        worst = $"worst: {bone} at {frame:F2} frames, {error * 1000:F4} mm, {score:F2} of its allowance";
                    }
                }
            }
        }
        return new Comparison(onFrame, between, ratio, worst);
    }

    static Matrix4x4 Inverse(Matrix4x4 m) => Matrix4x4.Invert(m, out var inverse) ? inverse : throw new InvalidOperationException("a singular transform");

    /// <summary>A golden deformation (3×4, rows, for column vectors) as System.Numerics' row-vector matrix.</summary>
    static Matrix4x4 Deformation(float[] d) => new(
        d[0], d[4], d[8], 0,
        d[1], d[5], d[9], 0,
        d[2], d[6], d[10], 0,
        d[3], d[7], d[11], 1);

    internal sealed record Reference(Dictionary<string, Vector3[]> Bones, double[] Times, List<Dictionary<string, float[]>> Poses);

    internal static Reference Load(string scene)
    {
        var path = Path.Combine(GoldenDirectory, "anim-" + scene + ".json");
        Assert.True(File.Exists(path), $"no Blender reference for {scene}: run Tools/Scripts/make-blender-references.sh");
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var root = document.RootElement;
        var bones = new Dictionary<string, Vector3[]>();
        foreach (var bone in root.GetProperty("bones").EnumerateObject())
            bones[bone.Name] = [Point(bone.Value.GetProperty("head")), Point(bone.Value.GetProperty("tail"))];
        var times = root.GetProperty("times").EnumerateArray().Select(t => t.GetDouble()).ToArray();
        var poses = root.GetProperty("poses").EnumerateArray()
            .Select(p => p.EnumerateObject().ToDictionary(b => b.Name, b => b.Value.EnumerateArray().Select(v => v.GetSingle()).ToArray())).ToList();
        return new Reference(bones, times, poses);

        static Vector3 Point(JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle(), e[2].GetSingle());
    }
}
