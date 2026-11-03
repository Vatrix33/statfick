using System.Numerics;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Render3D;
using Statventure.Tests.Packs;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Render3D;

/// <summary>
/// The terrain scenes drawn with the terrain loaded from the pack
/// (<see cref="Terrain.Load(Statventure.Graphics.GpuDevice, Statventure.Packs.VirtualFileSystem, string)"/>)
/// match their golden renders as the description-loaded terrain does
/// (<see cref="Render3DReferenceTests.TheEngineDrawsWhatBlenderDrew"/>), and are the same pixels.
/// </summary>
[Collection("GPU")]
public class TerrainPackRenderTests(GpuFixture gpu, TestTerrainPack pack) : IClassFixture<TestTerrainPack>
{
    [Theory]
    [InlineData("terrain-map")]
    [InlineData("terrain-oblique")]
    [InlineData("terrain-water")]
    public void ATerrainSceneFromThePackIsWhatBlenderDrew(string name)
    {
        var scene = Scenes3D.All.Single(s => s.Name == name);
        Assert.Equal("test.json", scene.Terrain?.File);
        var unlined = scene with { Style = scene.Style with { OutlineWidth = 0, InnerWidth = 0 } };
        var actual = Render(unlined, shadows: true);
        var expected = ImageFile.Load(Golden.GoldenPath("r3d-" + name));
        var unshadowed = Render(unlined, shadows: false);
        var problems = !unshadowed.Pixels.AsSpan().SequenceEqual(actual.Pixels)
            ? Render3DReferenceTests.CompareShadowed(expected, actual, unshadowed)
            : Render3DReferenceTests.Compare(expected, actual);
        var actualPath = Golden.ActualPath("r3d-" + name + "-packed");
        if (problems.Length > 0)
        {
            ImageFile.SavePng(actualPath, actual);
            Assert.Fail($"{name}: {problems}; render in {actualPath}");
        }
        var described = new Render3DReferenceTests(gpu).Render(unlined);
        if (!described.Pixels.AsSpan().SequenceEqual(actual.Pixels))
        {
            ImageFile.SavePng(actualPath, actual);
            Assert.Fail($"{name}: the pack's terrain draws other pixels than the description's; render in {actualPath}");
        }
    }

    /// <summary>The GPU terrain is made only from data that passes the check at load (<see cref="TerrainData.Validate"/>).</summary>
    [Fact]
    public void ATerrainThatBreaksItsRulesIsRefusedBeforeItReachesTheGpu()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        foreach (var (name, terrain, _) in TerrainPackTests.BrokenTerrains())
            writer.AddRecord("world/terrain/" + name, terrain);
        writer.Write(folder["world.pak"]);
        using var files = new VirtualFileSystem([Mount.Pack(folder["world.pak"])]);
        foreach (var (name, _, expected) in TerrainPackTests.BrokenTerrains())
        {
            var problem = Assert.Throws<DataFileException>(() => Terrain.Load(gpu.Device, files, "world/terrain/" + name));
            Assert.Equal(("world/terrain/" + name, "not a valid terrain: " + expected), (problem.Entry, problem.Problem));
        }
    }

    /// <summary>As <see cref="Render3DReferenceTests.Render"/>, the terrain loaded from the pack (models from their .glb files).</summary>
    RgbaImage Render(Scene3D scene, bool shadows)
    {
        var device = gpu.Device;
        var meshes = new Dictionary<Shape3D, Mesh>();
        var models = new List<Model>();
        Terrain? terrain = null;
        try
        {
            var items = scene.Objects.Select(o =>
            {
                if (!meshes.TryGetValue(o.Shape, out var mesh))
                    meshes[o.Shape] = mesh = Mesh.FromTriangles(device, o.Shape.Positions, o.Shape.Triangles);
                return new DrawItem(mesh, o.Model, Colors.LinearFromHex(o.Color));
            }).ToList();
            var palette = new Palette(scene.Palette, scene.Shadows);
            var groups = new Dictionary<string, (object Owner, Vector3 Anchor)>();
            foreach (var placed in scene.Models)
            {
                var model = Model.Load(device, Path.Combine(Render3DReferenceTests.ModelDirectory, placed.File));
                models.Add(model);
                (object Owner, Vector3 Anchor)? group = placed.Group == null ? null
                    : groups.TryGetValue(placed.Group, out var known) ? known : groups[placed.Group] = (new object(), placed.Model.Translation);
                model.Draw(items, placed.Model, palette, scene.Style, placed.Scenery, placed.PoseOf(model.Data), group?.Owner, placed.World?.Invoke(model.Data), group?.Anchor);
            }
            var placedTerrain = scene.Terrain!.Value;
            terrain = Terrain.Load(device, pack.Files, TestTerrainPack.RecordPath);
            terrain.Draw(items, placedTerrain.Translation, scene.Camera.ToCamera(), scene.Height, palette, scene.Style);
            if (!shadows)
                items = items.Select(i => i with { ReceivesShadows = false }).ToList();
            using var target = new RenderTarget3D(device, scene.Width, scene.Height);
            var renderer = new Renderer3D(device);
            var frame = device.BeginFrame();
            renderer.Render(frame, target, scene.Camera.ToCamera(), items, Colors.LinearFromHex(scene.Background), scene.Sun ?? default, scene.Style, scene.Scale, scene.Time,
                scene.ParticlesNow());
            device.EndFrame(frame);
            device.WaitIdle();
            return new RgbaImage(scene.Width, scene.Height, target.Resolved.ReadPixels());
        }
        finally
        {
            foreach (var mesh in meshes.Values)
                mesh.Dispose();
            foreach (var model in models)
                model.Dispose();
            terrain?.Dispose();
        }
    }
}
