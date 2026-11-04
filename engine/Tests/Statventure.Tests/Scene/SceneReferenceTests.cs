using System.Numerics;
using System.Text.Json;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Animation;
using Statventure.Tests.Infrastructure;
using Xunit.Abstractions;

namespace Statventure.Tests.Scene;

/// <summary>
/// A node scene (Docs/Scene.md, step 1, "Method"): a test model (Data/Models, built by
/// Tools/BlenderReference/make_models.py) instanced in a scene, its clips (by name) played together on
/// its nodes from time 0, held before and after, and the times (seconds) at which Blender evaluates
/// the same actions on the objects and armatures the script built (evaluate_animations.py,
/// Golden/nodes-&lt;name&gt;.json).
/// </summary>
public sealed record NodeScene(string Name, string Model, string[] Clips, double[] Times);

/// <summary>
/// Blender's object parenting as the exporter gives it back and the scene composes it (Docs/Scene.md,
/// "Blender's rule as glTF gives it back"): every node of the instanced hierarchy model (parent
/// inverses, a bone parent, non-uniform scale above a turned child, a mirrored branch, an animated
/// parent above still children; its object clip and its armature clip playing on the instanced
/// nodes) where Blender puts the object or bone of its name. On the clips' 60 fps frames each node's
/// world (its origin and the ends of its unit axes) within M7's on-frame tolerance of Blender's;
/// between frames, where Blender evaluates its curves and the engine interpolates the exported
/// samples, each node judged in its parent's frame by M7's rule (Docs/Animation.md, "Comparison"):
/// within 1.5/8 of the larger second difference of those points at the frames around, plus the
/// tolerance.
/// </summary>
public class SceneReferenceTests(ITestOutputHelper output)
{
    const int Fps = 60;

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");
    static string GoldenDirectory => Path.GetDirectoryName(Golden.GoldenPath("x"))!;

    /// <summary>Every other frame of <paramref name="frames"/> (and the last), the middle of every frame, a quarter into every fifth, one before the start and one after the end.</summary>
    static double[] TimesFor(int frames)
    {
        var times = new SortedSet<double> { -0.05, (frames + 3.0) / Fps, (double)frames / Fps };
        for (var k = 0; k <= frames; k += 2)
            times.Add((double)k / Fps);
        for (var k = 0; k < frames; k++)
            times.Add((k + 0.5) / Fps);
        for (var k = 0; k < frames; k += 5)
            times.Add((k + 0.25) / Fps);
        return [.. times];
    }

    public static readonly NodeScene[] All =
    [
        // The hierarchy model's two clips at once: "base" moves, turns and scales its top object
        // unevenly (its still children follow, the child parented in place sheared under it), "rig"
        // turns the armature's bones (the charm on the hand bone follows its tail).
        new("hierarchy", "hierarchy", ["base", "rig"], TimesFor(60)),
    ];

    public static TheoryData<string> SceneNames => new(All.Select(s => s.Name));

    [Fact]
    public void TheSceneFileIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "node-scenes.json");
        var text = JsonSerializer.Serialize(new { scenes = All.Select(s => new { name = s.Name, model = s.Model, clips = s.Clips, times = s.Times }) },
            new JsonSerializerOptions { WriteIndented = true }) + "\n";
        if (!File.Exists(path) || File.ReadAllText(path) != text)
            File.WriteAllText(path, text);
        Assert.NotEmpty(All);
    }

    /// <summary>The scene's clips as layers of a stack on the instance's nodes, each from time 0, held both ways.</summary>
    internal static AnimationStack Stack(ModelInstance instance, IEnumerable<string> clips)
    {
        var stack = new AnimationStack(instance.Skeleton);
        foreach (var clip in clips)
            stack.Add(instance.Clip(clip), 0, s => s.Extrapolation = Extrapolation.Hold);
        return stack;
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void NodesAreWhereBlenderPutsTheirObjects(string name)
    {
        var scene = All.Single(s => s.Name == name);
        var path = Path.Combine(GoldenDirectory, "nodes-" + name + ".json");
        Assert.True(File.Exists(path), $"no Blender reference for {name}: run Tools/Scripts/make-blender-references.sh");
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var golden = document.RootElement.GetProperty("nodes"u8);
        Assert.Equal(scene.Times, document.RootElement.GetProperty("times"u8).EnumerateArray().Select(t => t.GetDouble()));
        var data = SceneNodeTests.LoadModel(scene.Model + ".glb");
        var root = new SceneNode();
        var instance = root.Instance(data);
        var stack = Stack(instance, scene.Clips);
        var nodes = Enumerable.Range(0, data.Nodes.Count).Where(i => instance.NodeOf(i) != null).ToArray();
        // Blender's world matrix of a node at a time (3×4, rows, for column vectors) as a row-vector matrix.
        Matrix4x4 Blender(int node, int time)
        {
            Assert.True(golden.TryGetProperty(data.Nodes[node].Name, out var list), $"{name}: Blender has no object or bone {data.Nodes[node].Name}");
            var m = list[time].EnumerateArray().Select(v => v.GetSingle()).ToArray();
            return new Matrix4x4(m[0], m[4], m[8], 0, m[1], m[5], m[9], 0, m[2], m[6], m[10], 0, m[3], m[7], m[11], 1);
        }
        Matrix4x4 Inverse(Matrix4x4 m) => Matrix4x4.Invert(m, out var inverse) ? inverse : throw new InvalidOperationException("a singular world matrix");
        Vector3[] points = [Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ];
        double Distance(Matrix4x4 a, Matrix4x4 b) => points.Max(p => (double)Vector3.Distance(Vector3.Transform(p, a), Vector3.Transform(p, b)));
        // The engine's local transforms on the clips' frames, for the allowance between them.
        var lastFrame = (int)Math.Round(stack.Layers.OfType<AnimationLayer>().Max(l => l.Strips[0].End) * Fps);
        var frames = new Dictionary<int, Matrix4x4[]>();
        Matrix4x4[] Frame(int k)
        {
            k = Math.Clamp(k, 0, lastFrame);
            if (frames.TryGetValue(k, out var locals))
                return locals;
            instance.Apply(stack, (double)k / Fps);
            return frames[k] = nodes.Select(i => instance.NodeOf(i)!.Local).ToArray();
        }

        double onFrame = 0, ratio = 0, worstScore = -1;
        var worst = "";
        for (var t = 0; t < scene.Times.Length; t++)
        {
            var time = scene.Times[t];
            var exact = AnimationScene.OnFrame(time);
            var k = (int)Math.Floor(time * Fps);
            // The frames around worked out first (each applies the stack at its own time).
            if (!exact)
                for (var f = k - 1; f <= k + 2; f++)
                    Frame(f);
            instance.Apply(stack, time);
            for (var n = 0; n < nodes.Length; n++)
            {
                var node = instance.NodeOf(nodes[n])!;
                double error, score;
                if (exact)
                {
                    error = Distance(node.World, Blender(nodes[n], t));
                    onFrame = Math.Max(onFrame, error);
                    score = error / AnimationReferenceTests.OnFrameTolerance;
                }
                else
                {
                    // In the parent's frame: Blender's local is its world after the inverse of its parent's.
                    var parent = data.Nodes[nodes[n]].Parent;
                    var blenderLocal = parent >= 0 ? Blender(nodes[n], t) * Inverse(Blender(parent, t)) : Blender(nodes[n], t);
                    error = Distance(node.Local, blenderLocal);
                    var curvature = points.Max(p =>
                    {
                        Vector3 At(int f) => Vector3.Transform(p, Frame(f)[n]);
                        return Math.Max((At(k - 1) - 2 * At(k) + At(k + 1)).Length(), (At(k) - 2 * At(k + 1) + At(k + 2)).Length());
                    });
                    var allowed = AnimationReferenceTests.BetweenFramesMargin * curvature / 8 + AnimationReferenceTests.OnFrameTolerance;
                    score = error / allowed;
                    ratio = Math.Max(ratio, score);
                }
                if (score > worstScore)
                {
                    worstScore = score;
                    worst = $"worst: {node.Name} at {time * Fps:F2} frames, {error * 1000:F4} mm, {score:F2} of its allowance";
                }
            }
        }
        output.WriteLine($"{name}: on frames {onFrame * 1000:F5} mm; between frames at most {ratio:F2} of the allowance ({worst})");
        Assert.True(onFrame <= AnimationReferenceTests.OnFrameTolerance, $"{name}: a node is {onFrame * 1000:F4} mm off on a frame ({worst})");
        Assert.True(ratio <= 1, $"{name}: a node is off between frames by {ratio:F2} times what interpolating the samples allows ({worst})");
    }
}
