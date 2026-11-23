using System.Numerics;
using System.Text;
using System.Text.Json;
using Statventure.Imaging;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Render3D;

/// <summary>
/// The engine draws the 3D test scenes (Scenes3D) as Blender 5.2 rendered them
/// (Golden/r3d-&lt;scene&gt;.png, from Tools/Scripts/make-blender-references.sh, which renders
/// Reference/r3d-scenes.json). Pixels inside a shape or the background must match; pixels on an
/// edge, which MSAA 4× and Blender's filtered samples cover differently, only roughly.
/// </summary>
[Collection("GPU")]
public partial class Render3DReferenceTests(GpuFixture gpu)
{
    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");
    internal static string ModelDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Data", "Models");
    internal static string TerrainDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Data", "Terrain");

    // Ji'ruan's scenes (Docs/Scene.md, step 7) are compared through the sequence that plays them (Scene/JiruanRenderTests).
    static IEnumerable<Scene3D> Compared => Scenes3D.All.Where(s => !s.Name.StartsWith("jiruan-"));
    public static TheoryData<string> SceneNames => new(Compared.Select(s => s.Name));
    public static TheoryData<string> InkedSceneNames => new(Compared.Where(s => s.Models.Length > 0 || s.Terrain != null).Select(s => s.Name));

    [Fact]
    public void TheSceneFileIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "r3d-scenes.json");
        var text = SceneFile();
        if (!File.Exists(path) || File.ReadAllText(path) != text)
            File.WriteAllText(path, text);
        Assert.NotEmpty(Scenes3D.All);
    }

    [Fact]
    public void TheTestShapesFaceOutward()
    {
        foreach (var shape in new[] { Shape3D.Cube, Shape3D.Tetrahedron, Shape3D.Prism })
        {
            var centre = shape.Positions.Aggregate(Vector3.Zero, (a, p) => a + p) / shape.Positions.Length;
            for (var i = 0; i < shape.Triangles.Length; i += 3)
            {
                Vector3 a = shape.Positions[shape.Triangles[i]], b = shape.Positions[shape.Triangles[i + 1]], c = shape.Positions[shape.Triangles[i + 2]];
                var normal = Vector3.Cross(b - a, c - a);
                Assert.True(Vector3.Dot(normal, (a + b + c) / 3 - centre) > 0, $"{shape.Name}: triangle {i / 3} faces inward");
            }
        }
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void TheEngineDrawsWhatBlenderDrew(string name)
    {
        var scene = Scenes3D.All.Single(s => s.Name == name);
        var reference = Golden.GoldenPath("r3d-" + name);
        Assert.True(File.Exists(reference), $"no Blender reference for {name}: run Tools/Scripts/make-blender-references.sh");
        var shadowed = scene.Models.Any(m => m.Scenery) || scene.Terrain != null;
        // The shading alone: the lines are compared with Freestyle's on their own (TheEngineInksWhatFreestyleInked).
        var unlined = scene with { Style = scene.Style with { OutlineWidth = 0, InnerWidth = 0 } };
        var actual = Render(unlined);
        var expected = ImageFile.Load(reference);
        // Scenes with scenery receive shadows: their soft fringes are compared on their own terms, where
        // one falls in view (else the scene is one without shadows).
        var unshadowed = shadowed ? Render(unlined, shadows: false) : null;
        var problems = unshadowed != null && !unshadowed.Pixels.AsSpan().SequenceEqual(actual.Pixels)
            ? CompareShadowed(expected, actual, unshadowed)
            : Compare(expected, actual);
        var actualPath = Golden.ActualPath("r3d-" + name);
        if (problems.Length > 0)
        {
            ImageFile.SavePng(actualPath, actual);
            Assert.Fail($"{name}: {problems}; render in {actualPath}");
        }
        if (Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        else if (File.Exists(actualPath))
            File.Delete(actualPath);
    }

    [Theory]
    [MemberData(nameof(InkedSceneNames))]
    public void TheEngineInksWhatFreestyleInked(string name)
    {
        var scene = Scenes3D.All.Single(s => s.Name == name);
        var reference = Golden.GoldenPath("r3d-" + name + "-ink");
        Assert.True(File.Exists(reference), $"no Freestyle reference for {name}: run Tools/Scripts/make-blender-references.sh");
        var actual = Render(scene, inkOnly: true);
        var actualPath = Golden.ActualPath("r3d-" + name + "-ink");
        if (Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        var problems = CompareInk(InkCoverage(ImageFile.Load(reference), alpha: true), InkCoverage(actual, alpha: false), actual.Width);
        if (problems.Length > 0)
        {
            ImageFile.SavePng(actualPath, actual);
            Assert.Fail($"{name}: {problems}; lines in {actualPath}");
        }
        if (Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") != "1" && File.Exists(actualPath))
            File.Delete(actualPath);
    }

    /// <summary>
    /// The terrain's chunks meet without cracks whatever their levels (Docs/Renderer-3D.md, "Terrain"):
    /// drawn with its four chunks at strides 1, 2, 4 and 8, each next to chunks of two others, no
    /// background shows where the terrain at full resolution covers a pixel and the pixels about it.
    /// Without the water, which would fill a crack from below. (The reference scenes draw every chunk
    /// at stride 1: their cells are 4-6 pixels on screen.) Checked: chunk borders at the chunk's
    /// stride fail (370 and 34 pixels), as does a border cell cut so that its border points lie on
    /// another triangle's edge (one pixel, where rounding opens the edge).
    /// </summary>
    [Theory]
    [InlineData("terrain-map")]
    [InlineData("terrain-oblique")]
    public void TheTerrainsLevelsMeetWithoutCracks(string name)
    {
        var scene = Scenes3D.All.Single(s => s.Name == name);
        scene = scene with { Background = "#ff00ff", Models = [], Style = scene.Style with { OutlineWidth = 0, InnerWidth = 0 } };
        var full = Render(scene, shadows: false, water: false);
        var mixed = Render(scene, shadows: false, terrainLevels: chunk => chunk % 4, water: false);
        int w = full.Width, h = full.Height;
        // How magenta a pixel is: the background's share shows as red and blue above green.
        int Background(byte[] p, int x, int y)
        {
            var i = (y * w + x) * 4;
            return Math.Min(p[i], p[i + 2]) - p[i + 1];
        }
        var cracks = 0;
        for (var y = 1; y < h - 1; y++)
            for (var x = 1; x < w - 1; x++)
            {
                var covered = true;
                for (var dy = -1; dy <= 1 && covered; dy++)
                    for (var dx = -1; dx <= 1 && covered; dx++)
                        covered = Background(full.Pixels, x + dx, y + dy) <= 0;
                if (covered && Background(mixed.Pixels, x, y) > 8)
                    cracks++;
            }
        Assert.True(cracks == 0, $"{name}: {cracks} pixels show the background between chunks");
    }

    /// <summary>
    /// Particles lie over the ink lines behind them (Docs/Renderer-3D.md, "Particles"; the references
    /// can't tell, as Blender's shading renders have no lines): an opaque disc in front of a pixel of
    /// a model's outline shows its own colour there, not the ink. Checked: particles drawn before the
    /// lines fail.
    /// </summary>
    [Fact]
    public void ParticlesCoverTheInkBehindThem()
    {
        var scene = Scenes3D.All.Single(s => s.Name == "cel-shapes");
        var plain = Render(scene);
        int w = plain.Width, h = plain.Height;
        var ink = scene.Style.Ink * 255;
        // The darkest pixel near the middle: on a line.
        var (x, y) = Enumerable.Range(h / 4, h / 2).SelectMany(py => Enumerable.Range(w / 4, w / 2).Select(px => (px, py)))
            .MinBy(p => plain.Pixels[(p.py * w + p.px) * 4] + plain.Pixels[(p.py * w + p.px) * 4 + 1] + plain.Pixels[(p.py * w + p.px) * 4 + 2]);
        Assert.True(Math.Abs(plain.Pixels[(y * w + x) * 4] - ink.X) < 12, "no ink line found");
        // A disc 12 pixels across, just past the near plane on that pixel's ray.
        var camera = scene.Camera.ToCamera();
        Matrix4x4.Invert(camera.ViewProjection((float)w / h), out var unproject);
        var near = Vector4.Transform(new Vector4((x + 0.5f) / w * 2 - 1, (y + 0.5f) / h * 2 - 1, 0.95f, 1), unproject);
        var place = new Vector3(near.X, near.Y, near.Z) / near.W;
        var depth = -Vector3.Transform(place, camera.View).Z;
        var size = 12 * 2 * depth * MathF.Tan(camera.VerticalFieldOfView / 2) / h;
        var disc = new ParticleEmitter
        {
            Effect = new()
            {
                Shape = new ParticleShape.Disc(), StartColor = "glint", EndColor = "glint", Life = 10, Size = size, Speed = 0, Spawn = new ParticleSpawn.Point(),
            },
            Seed = 1, Path = ParticleEmitter.At(place), Burst = 1,
        };
        var covered = Render(scene with { Palette = new(scene.Palette) { ["glint"] = "#20c0a0" }, Particles = [disc] });
        var i = (y * w + x) * 4;
        Assert.Equal([0x20, 0xc0, 0xa0], new[] { (int)covered.Pixels[i], covered.Pixels[i + 1], covered.Pixels[i + 2] });
    }

    internal RgbaImage Render(Scene3D scene, bool inkOnly = false, bool shadows = true, Func<int, int>? terrainLevels = null, bool water = true)
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
                var model = Model.Load(device, Path.Combine(ModelDirectory, placed.File));
                models.Add(model);
                if (placed.Objects)
                {
                    // Through a scene: the model instanced under a node placed as the scene places it, each
                    // top-level object its own model (Docs/Scene.md, Q2).
                    Assert.True(Matrix4x4.Decompose(placed.Model, out var scale, out var rotation, out var translation));
                    var holder = new SceneNode().Add("placement");
                    holder.SetLocal(translation, rotation, scale);
                    var instance = holder.Instance(model, placed.Palette is { } own ? new Palette(own) : palette, placed.Scenery);
                    if (placed.ClipTime is { } clipTime)
                        instance.Apply(instance.Clip(model.Data.Animations[0].Name), clipTime);
                    instance.Draw(items, new SceneDrawing(scene.Style, scene.Camera.ToCamera(), scene.Height));
                    continue;
                }
                // A group (a character with its gear) is one model: one owner, the first one's origin anchoring the hatching.
                (object Owner, Vector3 Anchor)? group = placed.Group == null ? null
                    : groups.TryGetValue(placed.Group, out var known) ? known : groups[placed.Group] = (new object(), placed.Model.Translation);
                model.Draw(items, placed.Model, placed.Palette is { } ownPalette ? new Palette(ownPalette, scene.Shadows) : palette, scene.Style, placed.Scenery, placed.PoseOf(model.Data), group?.Owner, placed.World?.Invoke(model.Data), group?.Anchor);
            }
            if (scene.Terrain is { } placedTerrain)
            {
                terrain = Terrain.Load(device, Path.Combine(TerrainDirectory, placedTerrain.File));
                terrain.ForcedLevel = terrainLevels;
                terrain.Draw(items, placedTerrain.Translation, scene.Camera.ToCamera(), scene.Height, palette, scene.Style);
            }
            if (!water)
                items.RemoveAll(i => i.Shading == Shading.Water);
            // Without shadows: nothing receives them (the geometry's edges for CompareShadowed).
            if (!shadows)
                items = items.Select(i => i with { ReceivesShadows = false }).ToList();
            using var target = new RenderTarget3D(device, scene.Width, scene.Height);
            using var renderer = new Renderer3D(device) { InkCoverageOnly = inkOnly };
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

    /// <summary>
    /// What each joint of a posed model does to what it moves (glTF model space, columns): back from
    /// its rest transform, then its transform in the pose. The same for every skin that uses the joint
    /// (an inverse bind matrix also carries its own mesh's place); Blender poses its bones to deform the same.
    /// </summary>
    static Dictionary<string, float[]>? Deformations(ModelObject3D placed)
    {
        if (placed.Pose.Count == 0 && placed.World == null)
            return null;
        var data = ModelData.Load(Path.Combine(ModelDirectory, placed.File));
        var rest = data.WorldMatrices();
        var world = placed.World?.Invoke(data) ?? data.WorldMatrices(placed.PoseOf(data));
        var result = new Dictionary<string, float[]>();
        foreach (var skin in data.Skins)
            for (var j = 0; j < skin.Joints.Count; j++)
            {
                Matrix4x4.Invert(rest[skin.Joints[j]], out var back);
                var m = back * world[skin.Joints[j]];
                result[data.Nodes[skin.Joints[j]].Name] = [m.M11, m.M12, m.M13, m.M14, m.M21, m.M22, m.M23, m.M24, m.M31, m.M32, m.M33, m.M34, m.M41, m.M42, m.M43, m.M44];
            }
        return result;
    }

    /// <summary>
    /// The scenes for Tools/BlenderReference/render_scenes.py: shapes, each object's model matrix
    /// (columns) and colour, models (files in <c>modelFolder</c>, relative to this file) with their
    /// matrices, the palette and shadow tones, the sun, the cel style, the camera.
    /// </summary>
    static string SceneFile()
    {
        static float[] V(Vector3 v) => [v.X, v.Y, v.Z];
        static string Hex(Vector4 srgb) => $"#{(int)MathF.Round(srgb.X * 255):x2}{(int)MathF.Round(srgb.Y * 255):x2}{(int)MathF.Round(srgb.Z * 255):x2}";
        // A System.Numerics row (row vectors) is a column of the same matrix for column vectors, as glTF stores them.
        static float[][] Columns(Matrix4x4 m) =>
        [
            [m.M11, m.M12, m.M13, m.M14],
            [m.M21, m.M22, m.M23, m.M24],
            [m.M31, m.M32, m.M33, m.M34],
            [m.M41, m.M42, m.M43, m.M44],
        ];
        var shapes = Scenes3D.All.SelectMany(s => s.Objects).Select(o => o.Shape).Distinct()
            .ToDictionary(s => s.Name, s => new { positions = s.Positions.Select(V).ToArray(), triangles = s.Triangles });
        var scenes = Scenes3D.All.Select(s => new
        {
            name = s.Name,
            width = s.Width,
            height = s.Height,
            background = s.Background,
            camera = new
            {
                projection = s.Camera.Projection.ToString().ToLowerInvariant(),
                position = V(s.Camera.Position),
                target = V(s.Camera.Target),
                up = V(s.Camera.Up),
                fieldOfViewDegrees = s.Camera.FieldOfViewDegrees,
                orthographicHeight = s.Camera.OrthographicHeight,
                near = s.Camera.Near,
                far = s.Camera.Far,
            },
            objects = s.Objects.Select(o => new { shape = o.Shape.Name, matrix = Columns(o.Model), color = o.Color }).ToArray(),
            models = s.Models.Select(m => new
            {
                file = m.File, matrix = Columns(m.Model), scenery = m.Scenery, pose = Deformations(m), group = m.Group,
                anchor = m.Group == null ? null : V(s.Models.First(o => o.Group == m.Group).Model.Translation),
                objects = m.Objects ? true : (bool?)null,
                clipTime = m.ClipTime,
                palette = m.Palette,
            }).ToArray(),
            palette = s.Palette,
            shadows = s.Shadows,
            sun = s.Sun is { } sun ? new { direction = V(sun.Direction), strength = sun.Strength, angle = sun.Angle } : null,
            style = new
            {
                ink = Hex(s.Style.Ink), shadowMix = s.Style.ShadowMix, bandEdge = s.Style.BandEdge,
                outlineWidth = s.Style.OutlineWidth * s.Scale, innerWidth = s.Style.InnerWidth * s.Scale, creaseAngle = s.Style.CreaseAngle,
                hatchSpacing = s.Style.HatchSpacing, hatchWidth = s.Style.HatchWidth, hatchAngle = s.Style.HatchAngle, hatchInk = s.Style.HatchInk,
                glowWide = s.Style.GlowWide, glowCore = s.Style.GlowCore, glowBoost = s.Style.GlowBoost,
            },
            scale = s.Scale,
            terrain = s.Terrain is { } t ? new { file = t.File, translation = V(t.Translation) } : null,
            time = s.Time,
            // Each particle as the engine draws it (ParticleQuad): Blender lays a quad for each.
            particles = s.ParticlesNow().Select(p => ParticleQuad.Of(p, s.Camera.ToCamera(), s.Height, s.Scale)).Select(q => new
            {
                centre = V(q.Centre),
                axisU = V(q.AxisU),
                axisV = V(q.AxisV),
                halfU = q.HalfU,
                halfV = q.HalfV,
                pixel = q.Pixel,
                shape = q.Shape,
                p0 = q.P0,
                p1 = q.P1,
                color = new[] { q.Color.X, q.Color.Y, q.Color.Z },
                opacity = q.Opacity,
                outline = q.Outline,
                glow = q.Glow,
            }).ToArray(),
        });
        return JsonSerializer.Serialize(new { modelFolder = "../Data/Models", terrainFolder = "../Data/Terrain", shapes, scenes }, new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }
}
