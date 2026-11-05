using Statventure.Imaging;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;

namespace Statventure.Tests.Scene;

/// <summary>
/// Ji'ruan's intro drawn by the engine as Blender drew it (Docs/Scene.md, step 7, phase 2; Q32): the
/// sequence playing on a scene (the stage instanced and its clip played, the camera and sun cut to, the
/// stand-ins on their marks playing the clip library, the king's palette cued) is rendered at each
/// sampled time and compared with Blender's render of the same moment (JiruanStage's render scenes,
/// Golden/r3d-jiruan-&lt;time&gt;.png and its ink) by M6's comparisons, shading with shadows then the ink lines,
/// under the detailed stages' limits (S7-12).
/// Particles are left out (they have no Blender reference).
/// </summary>
[Collection("GPU")]
public class JiruanRenderTests(GpuFixture gpu)
{
    const double Step = 1 / 60.0;

    public static TheoryData<double> Times => new(JiruanStage.Times);

    RgbaImage Render(Scene3D scene, View3D view, bool inkOnly = false, bool shadows = true)
    {
        var device = gpu.Device;
        var items = shadows ? view.Items : view.Items.Select(i => i with { ReceivesShadows = false }).ToList();
        var style = inkOnly ? scene.Style : scene.Style with { OutlineWidth = 0, InnerWidth = 0 };
        using var target = new RenderTarget3D(device, scene.Width, scene.Height);
        var renderer = new Renderer3D(device) { InkCoverageOnly = inkOnly };
        var frame = device.BeginFrame();
        renderer.Render(frame, target, view.Camera, items, Colors.LinearFromHex(scene.Background), view.Sun, style, scene.Scale, scene.Time);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(scene.Width, scene.Height, target.Resolved.ReadPixels());
    }

    /// <summary>
    /// The sequence draws exactly what its reference scene describes (JiruanStage: the stage at its clip's
    /// moment, its camera and sun, each stand-in on its mark in its clip's pose by the schedule, in its
    /// palette), made from the model data alone and drawn as Render3DReferenceTests draws any scene: the
    /// same pixels, shaded and inked. With TheStageIsWhereBlenderPutsIt (the stage's nodes, camera and sun
    /// against Blender's evaluation) and the acting clips' scenes (AnimationReferenceTests, acting-*), the
    /// sequence's picture is what Blender's stage and clips give, drawn by the engine's rules.
    /// </summary>
    [Theory]
    [MemberData(nameof(Times))]
    public void TheSequenceDrawsWhatItsReferenceSceneDescribes(double time)
    {
        var scene = JiruanStage.Scenes.Single(s => s.Name == JiruanStage.NameAt(time));
        using var bench = new JiruanBench(gpu.Device);
        var player = bench.Play();
        bench.Frame(Step);
        // Exactly at the time (frames of 1/60 s sum to it only within rounding): a seek, applied in the next frame.
        player.Seek(time);
        bench.Frame(0);
        Assert.Equal(time, player.TimelineTime);
        var reference = new Render3DReferenceTests(gpu);
        scene = JiruanStage.WithHidden(time);
        // M6's comparison: the scene draws each figure in the tree's order among the stage's objects and the
        // reference after them, which settles a few fragments at equal depth (the fissures and plates on the
        // floor) the other way; anything placed, posed, cut or coloured otherwise differs far beyond it.
        var problems = Render3DReferenceTests.Compare(reference.Render(scene with { Style = scene.Style with { OutlineWidth = 0, InnerWidth = 0 } }), Render(scene, bench.View));
        Assert.True(problems.Length == 0, $"{scene.Name} shaded: {problems}");
        var ink = Render3DReferenceTests.CompareInk(Render3DReferenceTests.InkCoverage(reference.Render(scene, inkOnly: true), alpha: false),
            Render3DReferenceTests.InkCoverage(Render(scene, bench.View, inkOnly: true), alpha: false), scene.Width);
        Assert.True(ink.Length == 0, $"{scene.Name} ink: {ink}");
    }

    /// <summary>
    /// The sequence's picture against Blender's at each sampled time (the harness following the engine's halo
    /// rule, S7-8), shading with shadows then the ink lines, held to the detailed stages' limits
    /// (<see cref="ReferenceLimits.DetailedStage"/>, S7-12): on this stage's fine scenery Freestyle drops the far
    /// trees' sub-pixel detail and draws no line where models rest in or on each other, so the engine draws
    /// about a tenth of its line pixels where Freestyle has none, and a few hundred edge pixels differ (the ground
    /// at trunk bases, the plates); a line the engine misses fails as under M6.
    /// </summary>
    [Theory]
    [MemberData(nameof(Times))]
    public void TheSequenceDrawsTheStageAsBlenderDrewIt(double time)
    {
        var scene = JiruanStage.Scenes.Single(s => s.Name == JiruanStage.NameAt(time));
        using var bench = new JiruanBench(gpu.Device);
        bench.Play();
        bench.Frame(Step);
        bench.FrameTo(Step, time);
        var name = "r3d-" + scene.Name;
        var shaded = Render(scene, bench.View);
        var problems = Render3DReferenceTests.CompareShadowed(ImageFile.Load(Golden.GoldenPath(name)), shaded, Render(scene, bench.View, shadows: false), ReferenceLimits.DetailedStage);
        if (problems.Length > 0)
        {
            ImageFile.SavePng(Golden.ActualPath(name + "-sequence"), shaded);
            Assert.Fail($"{scene.Name}: {problems}; render in {Golden.ActualPath(name + "-sequence")}");
        }
        var ink = Render(scene, bench.View, inkOnly: true);
        var inkProblems = Render3DReferenceTests.CompareInk(Render3DReferenceTests.InkCoverage(ImageFile.Load(Golden.GoldenPath(name + "-ink")), alpha: true),
            Render3DReferenceTests.InkCoverage(ink, alpha: false), ink.Width, ReferenceLimits.DetailedStage);
        if (inkProblems.Length > 0)
        {
            ImageFile.SavePng(Golden.ActualPath(name + "-sequence-ink"), ink);
            Assert.Fail($"{scene.Name}: {inkProblems}; lines in {Golden.ActualPath(name + "-sequence-ink")}");
        }
    }
}
