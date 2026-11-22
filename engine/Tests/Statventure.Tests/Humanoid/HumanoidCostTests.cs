using System.Diagnostics;
using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Xunit.Abstractions;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// What the family's figures cost (GM2 step 4a; Decisions for M6: a 2016-class GPU at 60 fps in 1080p with up to eight
/// figures a side): each model's vertices and triangles (both levels), and sixteen geared figures (eight men and eight
/// women, each the body, the face, the hair, the chain shirt, the sword and the shield, at the full level) playing the
/// clip library's test clip on a stage. A measurement, no bound (the manager, 2026-11-16): the frame's animation work, and
/// on request (STATVENTURE_MEASURE=1; slow on a CPU Vulkan) the GPU's share, a 1080p frame of the sixteen and a figure
/// alone at the figure mode's default size.
/// </summary>
[Collection("Speed")]
public class HumanoidCostTests(GpuFixture gpu, ITestOutputHelper output)
{
    static (int Vertices, int Triangles) Counts(ModelData model) =>
        (model.Meshes.SelectMany(m => m.Primitives).Sum(p => p.Positions.Length), model.Meshes.SelectMany(m => m.Primitives).Sum(p => p.Indices.Length / 3));

    /// <summary>
    /// What keeping the clips' contacts costs (GM2-P37; a measurement, no bound): the keyed body's clearances for the whole
    /// library (once per clip, shared by every body), then binding all 34 clips to one body (its 29 whole-body clips' contacts
    /// worked out once per body, worn set and clip) and what the bindings keep; a frame then costs what it did (the
    /// corrected channels are curves the binding plays instead of the clip's).
    /// </summary>
    [Fact]
    public void TheCostOfKeepingTheContactsIsMeasured()
    {
        var fresh = AnimationClip.AllOf(HumanoidClipList.Library);
        var watch = Stopwatch.StartNew();
        foreach (var clip in fresh.Where(c => c.Contacts != null))
            _ = clip.Contacts!.Keyed;
        output.WriteLine($"the man's clearances for the {fresh.Count(c => c.Contacts != null)} clips keeping contacts: {watch.Elapsed.TotalMilliseconds:F0} ms, {fresh.Where(c => c.Contacts != null).Sum(c => 8L * c.Contacts!.Keyed.Sum(k => k.Length)) / 1024} KiB");
        foreach (var body in new[] { "female-average", "male-strong" })
        {
            var skeleton = new Skeleton(HumanoidFiles.Load($"bodies/human-{body}"));
            watch.Restart();
            var bindings = fresh.Select(c => new ClipBinding(c, skeleton)).ToList();
            var elapsed = watch.Elapsed;
            var shape = ContactShape.Of(skeleton, fresh.First(c => c.Contacts != null).Contacts!)!;
            output.WriteLine($"human-{body}: {bindings.Count} clips bound in {elapsed.TotalMilliseconds:F0} ms on one thread, {bindings.Count(b => b.Contacts != null)} corrected, " +
                $"keeping {bindings.Sum(b => b.Contacts?.Bytes ?? 0) / 1024} KiB of curves and a shape of {shape.Count} vertices, {shape.Bytes / 1024} KiB");
            Assert.Equal(34, bindings.Count);
        }
    }

    [Fact]
    public void TheCostOfTheFamilysFiguresIsMeasured()
    {
        foreach (var name in HumanoidFiles.Figures())
        {
            var (vertices, triangles) = Counts(HumanoidFiles.Load(name));
            output.WriteLine($"{name}: {vertices} vertices, {triangles} triangles, {new FileInfo(HumanoidFiles.Glb(name)).Length} bytes");
        }
        var models = new Dictionary<string, Model>();
        Model Of(string path) => models.TryGetValue(path, out var model) ? model : models[path] = Model.Load(gpu.Device, path);
        var clip = AnimationClip.AllOf(HumanoidFiles.Load("test-clips")).Single(c => c.Name == "rest-sway");
        using var view = new View3D(gpu.Device)
        {
            Camera = new Camera3D(Projection.Orthographic, new(0, 4, 12), new(0, 0.9f, -1.5f), OrthographicHeight: 6).ToCamera(),
            Sun = HumanoidFiles.Ember.Sun, Style = HumanoidFiles.Ember.Style, Background = HumanoidFiles.Ember.BackgroundLinear,
        };
        var scene = new SceneView(view);
        var stage = scene.Root.Add("stage");
        try
        {
            for (var i = 0; i < 16; i++)
            {
                var sex = i % 2 == 0 ? "male" : "female";
                var body = Of(HumanoidFiles.Body(sex));
                var skeleton = new Skeleton(body.Data);
                var stack = new AnimationStack(skeleton);
                var layer = new AnimationLayer();
                layer.Strips.Add(new AnimationStrip(new ClipBinding(clip, skeleton)) { Start = -i * 0.13, Repeat = 1000 });
                stack.Layers.Add(layer);
                var mark = stage.Add($"mark.{i}");
                mark.Translation = new(i % 8 - 3.5f, 0, i / 8 * -1.5f);
                var actor = mark.Attach(new Actor(body, new Animator(stack)) { Palette = HumanoidFiles.Ember.Palette });
                foreach (var garment in new[] { HumanoidFiles.Face(sex), HumanoidFiles.Hair(sex), HumanoidFiles.ChainShirt(sex) })
                    actor.Garments.Add(new ActorGarment(Of(garment), new Garment(Of(garment).Data, skeleton)));
                actor.Items.Add(new ActorItem(Of(HumanoidFiles.Sword), new Socket(skeleton, "socket.hand.R")));
                actor.Items.Add(new ActorItem(Of(HumanoidFiles.Shield), new Socket(skeleton, "socket.shield.L")));
            }
            var watch = new Stopwatch();
            const int Frames = 240;
            for (var frame = 0; frame < Frames; frame++)
            {
                watch.Start();
                scene.Advance(1 / 60.0);
                watch.Stop();
            }
            var perFigure = watch.Elapsed.TotalMilliseconds / Frames / scene.Actors.Count;
            output.WriteLine($"animation work: {perFigure * 1000:F1} µs per figure per frame ({watch.Elapsed.TotalMilliseconds / Frames:F2} ms a frame for 16 geared figures)");
            if (Environment.GetEnvironmentVariable("STATVENTURE_MEASURE") == "1")
                MeasureTheGpu(view, scene);
            Assert.Equal(16, scene.Actors.Count);
        }
        finally
        {
            foreach (var model in models.Values)
                model.Dispose();
        }
    }

    /// <summary>The GPU's share: a 1080p frame of the sixteen, and the first of them alone at the figure mode's default size.</summary>
    void MeasureTheGpu(View3D view, SceneView scene)
    {
        output.WriteLine($"1080p frame of 16 geared figures: {GpuFrame(view, scene, 1920, 1080):F2} ms (render, submit and wait)");
        using var single = new View3D(gpu.Device) { Sun = view.Sun, Style = view.Style, Background = view.Background };
        single.Camera = new Camera3D(Projection.Orthographic, new(0, 1.0f, 6), new(0, 0.9f, 0), OrthographicHeight: 2.1f).ToCamera();
        var alone = new SceneView(single);
        var first = scene.Actors.First();
        var one = alone.Root.Add("figure").Attach(new Actor(first.Model, new Animator(first.Animator.Stack)) { Palette = first.Palette });
        one.Garments.AddRange(first.Garments);
        one.Items.AddRange(first.Items);
        output.WriteLine($"600 × 800 frame of one geared figure: {GpuFrame(single, alone, 600, 800):F2} ms (render, submit and wait)");
    }

    /// <summary>
    /// A frame's time on the GPU: each frame a new pose (not timed), rendered, submitted and waited for (the mean of 20
    /// after 10).
    /// </summary>
    double GpuFrame(View3D view, SceneView scene, int width, int height)
    {
        var watch = new Stopwatch();
        for (var frame = 0; frame < 30; frame++)
        {
            scene.Advance(1 / 60.0);
            if (frame == 10)
                watch.Reset();
            watch.Start();
            var f = gpu.Device.BeginFrame();
            view.Update(f, width, height, 1);
            gpu.Device.EndFrame(f);
            gpu.Device.WaitIdle();
            watch.Stop();
        }
        return watch.Elapsed.TotalMilliseconds / 20;
    }
}
