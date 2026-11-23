using Statventure.Graphics;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Render3D;

/// <summary>
/// What a 3D view holds on the device and gives back (GM2-P33, 2026-11-20; Docs/Renderer-3D.md, "3D views in the UI", "A
/// view's memory"): a view disposed gives back everything it made, its renderer's skinning arenas among them (GM2 step 11
/// found a view's renderer kept them: about 3.7 MiB a figure, a leak at every view made and let go); and the sun's 64 MiB
/// shadow atlas is made only by a view that runs a shadow pass (something in it receives shadows), and given back with it.
/// Measured as the GPU allocator's allocated bytes after the device has finished and destroyed what was released.
/// </summary>
[Collection("GPU")]
public class View3DMemoryTests(GpuFixture gpu)
{
    long Allocated()
    {
        gpu.Device.WaitIdle();
        gpu.Device.Collect();
        Brutal.VulkanApi.Vma.VmaAllocatorEx.CalculateStatistics(gpu.Device.Allocator, out var statistics);
        return (long)statistics.Total.Statistics.AllocationBytes;
    }

    /// <summary>A scene of the references' in a view of its own (its models loaded, drawn into its items).</summary>
    (View3D View, List<Model> Models) ViewOf(string name)
    {
        var scene = Scenes3D.All.Single(s => s.Name == name);
        var palette = new Palette(scene.Palette, scene.Shadows);
        var models = scene.Models.Select(m => Model.Load(gpu.Device, Path.Combine(Render3DReferenceTests.ModelDirectory, m.File))).ToList();
        var items = new List<DrawItem>();
        for (var i = 0; i < models.Count; i++)
            models[i].Draw(items, scene.Models[i].Model, palette, scene.Style, scene.Models[i].Scenery, scene.Models[i].PoseOf(models[i].Data));
        var view = new View3D(gpu.Device)
        {
            Items = items, Camera = scene.Camera.ToCamera(), Sun = scene.Sun ?? default, Style = scene.Style, Background = Colors.LinearFromHex(scene.Background),
        };
        return (view, models);
    }

    void Render(View3D view, int frames = 3)
    {
        // A few frames, so the frames in flight each use their own arena.
        for (var i = 0; i < frames; i++)
        {
            var frame = gpu.Device.BeginFrame();
            view.Invalidate();
            view.Update(frame, 260, 320, 1);
            gpu.Device.EndFrame(frame);
        }
        gpu.Device.WaitIdle();
    }

    /// <summary>
    /// A view of a skinned figure made, rendered and disposed many times gives its device memory back every time: its
    /// renderer's skinning arenas (in use while it lives) as well as its target, picture and records.
    /// </summary>
    [Fact]
    public void AViewMadeAndDisposedManyTimesGivesItsDeviceMemoryBack()
    {
        // Once first: the pipelines and the frames' own upload buffers stay the device's, no view's.
        var (first, firstModels) = ViewOf("pose-side");
        Render(first);
        first.Dispose();
        var baseline = Allocated();
        for (var round = 0; round < 6; round++)
        {
            var (view, models) = ViewOf("pose-side");
            Render(view);
            Assert.True(view.Renderer.ArenaBytes > 0, "the figure is skinned: its renderer posed it in its arenas");
            Assert.True(Allocated() - baseline > (long)view.Renderer.ArenaBytes, "the view holds its arenas and its target while it lives");
            view.Dispose();
            Assert.Equal(0UL, view.Renderer.ArenaBytes);
            foreach (var model in models)
                model.Dispose();
            var left = Allocated() - baseline;
            Assert.True(left == 0, $"round {round}: {left / 1024.0:F0} KiB not given back");
        }
        foreach (var model in firstModels)
            model.Dispose();
    }

    /// <summary>
    /// A view where nothing receives shadows (a figure alone) runs no shadow pass and never makes the 4096² atlas; one with
    /// scenery runs the pass, makes it (64 MiB) and gives it back when disposed.
    /// </summary>
    [Fact]
    public void OnlyAViewThatRunsAShadowPassMakesTheShadowAtlas()
    {
        const long Atlas = (long)RenderTarget3D.ShadowSize * RenderTarget3D.ShadowTiles * RenderTarget3D.ShadowSize * RenderTarget3D.ShadowTiles * 4;
        var (warm, warmModels) = ViewOf("pose-side");
        Render(warm);
        warm.Dispose();
        foreach (var model in warmModels)
            model.Dispose();
        var baseline = Allocated();

        var (figure, figureModels) = ViewOf("pose-side");
        Assert.DoesNotContain(figure.Items, item => item.ReceivesShadows);
        Render(figure);
        Assert.False(figure.Target!.HasShadowMap);
        Assert.True(Allocated() - baseline < Atlas / 2, $"a view with no shadow pass holds {(Allocated() - baseline) >> 20} MiB");
        figure.Dispose();

        var (stage, stageModels) = ViewOf("shadow-map");
        Assert.Contains(stage.Items, item => item.ReceivesShadows);
        var before = Allocated();
        Render(stage);
        Assert.True(stage.Target!.HasShadowMap);
        Assert.True(Allocated() - before >= Atlas, $"a view with a shadow pass holds only {(Allocated() - before) >> 20} MiB");
        stage.Dispose();
        foreach (var model in figureModels.Concat(stageModels))
            model.Dispose();
        var left = Allocated() - baseline;
        Assert.True(left == 0, $"{left / 1024.0:F0} KiB not given back");
    }
}
