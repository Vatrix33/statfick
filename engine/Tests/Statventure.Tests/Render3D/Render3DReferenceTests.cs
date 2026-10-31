using System.Numerics;
using System.Text;
using System.Text.Json;
using Statventure.Imaging;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Render3D;

/// <summary>
/// The engine draws the 3D test scenes (Scenes3D) as Blender 5.2 rendered them
/// (Golden/r3d-&lt;scene&gt;.png, from Tools/Scripts/make-blender-references.sh, which renders
/// Reference/r3d-scenes.json). Pixels inside a shape or the background must match; pixels on an
/// edge, which MSAA 4× and Blender's filtered samples cover differently, only roughly.
/// </summary>
[Collection("GPU")]
public class Render3DReferenceTests(GpuFixture gpu)
{
    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");
    internal static string ModelDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Data", "Models");
    internal static string TerrainDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Data", "Terrain");

    public static TheoryData<string> SceneNames => new(Scenes3D.All.Select(s => s.Name));
    public static TheoryData<string> InkedSceneNames => new(Scenes3D.All.Where(s => s.Models.Length > 0 || s.Terrain != null).Select(s => s.Name));

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

    /// <summary>Line coverage 0..1: Freestyle's alpha, or the engine's white-on-black coverage (sRGB-encoded in the target).</summary>
    internal static float[] InkCoverage(RgbaImage image, bool alpha)
    {
        var coverage = new float[image.Width * image.Height];
        for (var i = 0; i < coverage.Length; i++)
            coverage[i] = alpha ? image.Pixels[i * 4 + 3] / 255f : Colors.ToLinear(image.Pixels[i * 4] / 255f);
        return coverage;
    }

    /// <summary>
    /// A scene with cast shadows (Docs/Renderer-3D.md, "Shadows"). Pixels on the geometry's edges (as
    /// the engine draws the scene without shadows) are compared as in <see cref="Compare"/>. Inside
    /// surfaces the shadows' soft fringes are a sampled average, Blender's of 64 jittered maps at its
    /// own resolution, the engine's of 16 maps at 1024²: single pixels in a fringe differ by up to
    /// about 10 levels (the engine's blend steps by sixteenths), and the thinnest shadows wander. So,
    /// inside surfaces: at most 0.2% of pixels differ by more than 16 levels (measured: at most
    /// 0.15%), and the difference left over is at most a tenth of the shadows' own (the summed
    /// difference from Blender over the summed difference between Blender and the engine without
    /// shadows; measured: at most 0.091, in the fringe scene). On the geometry's edges, one pixel in
    /// 10,000 may stray (measured: up to 7, on the terrain): scenery shades a sub-pixel sliver of a
    /// band edge or a contact shadow once per pixel where Blender shades each sample. Checked: hard
    /// shadows, a sun 30% too small or too large, shadows on characters and no shadows each fail.
    /// </summary>
    internal static string CompareShadowed(RgbaImage expected, RgbaImage actual, RgbaImage unshadowed)
    {
        int w = expected.Width, h = expected.Height;
        byte[] e = expected.Pixels, a = actual.Pixels, u = unshadowed.Pixels;
        var onGeometry = new bool[w * h];
        long inside = 0, far = 0;
        double left = 0, signal = 0;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                var i = (y * w + x) * 4;
                var uniform = true;
                for (var dy = -1; dy <= 1 && uniform; dy++)
                    for (var dx = -1; dx <= 1 && uniform; dx++)
                    {
                        var j = (Math.Clamp(y + dy, 0, h - 1) * w + Math.Clamp(x + dx, 0, w - 1)) * 4;
                        uniform = u[j] == u[i] && u[j + 1] == u[i + 1] && u[j + 2] == u[i + 2];
                    }
                if (!uniform)
                {
                    onGeometry[y * w + x] = true;
                    continue;
                }
                int difference = 0, shadow = 0;
                for (var c = 0; c < 3; c++)
                {
                    difference = Math.Max(difference, Math.Abs(e[i + c] - a[i + c]));
                    shadow = Math.Max(shadow, Math.Abs(e[i + c] - u[i + c]));
                }
                inside++;
                if (difference > 16)
                    far++;
                left += difference;
                signal += shadow;
            }
        var text = new StringBuilder(Compare(expected, actual, only: (x, y) => onGeometry[y * w + x], strays: w * h / 10000));
        if (far > 0.002 * inside)
            text.Append($" {far} of {inside} pixels inside surfaces differ by more than 16");
        if (signal > 0 && left / signal > 0.1)
            text.Append($" {left / signal:P1} of the shadows' difference left over");
        return text.ToString().Trim();
    }

    /// <summary>
    /// Ink lines against Freestyle's, which strokes edges where the engine finds them per pixel
    /// (Docs/Renderer-3D.md, "Ink lines"). Measured over the reference scenes: every Freestyle line
    /// pixel (coverage ≥ 0.5) has an engine line pixel within one pixel but for 0.15%; engine line
    /// pixels away from any Freestyle line are at most 2.9% (where surfaces meet seen almost edge-on,
    /// the fin on the bent tube or a hill's far slope going into the lake: there the engine can't tell
    /// two surfaces crossing from a silhouette; and a corner where a part without ink meets another);
    /// the total ink is within 2.4%; coverage differs by 0.03-0.08 on average over line pixels; around
    /// a Freestyle line pixel, the engine's ink in the 7×7 pixels about it is 0.75 to 1.33 times
    /// Freestyle's but for 1.1% (line ends, corners). Checked: missing creases, lines 20%
    /// thin, lines on parts without ink, and outlines only against the background (a model over
    /// another gets an inner line: 8.5% of line pixels out of the local range) each fail one of:
    /// misses ≤ 2%, extras ≤ 3%, total ink within 10%, mean coverage difference ≤ 0.12, local ink
    /// out of range ≤ 3%; and a crest before the slope behind it (a fold within a part seen almost
    /// edge-on) taken for a surface running on fails terrain-oblique (3.5% of its line pixels missed).
    /// </summary>
    internal static string CompareInk(float[] expected, float[] actual, int width)
    {
        var height = expected.Length / width;
        float Near(float[] map, int x, int y)
        {
            var best = 0f;
            for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                    best = Math.Max(best, map[Math.Clamp(y + dy, 0, height - 1) * width + Math.Clamp(x + dx, 0, width - 1)]);
            return best;
        }
        int lines = 0, engineLines = 0, missed = 0, extra = 0, covered = 0;
        double ink = 0, engineInk = 0, difference = 0;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var i = y * width + x;
                float e = expected[i], a = actual[i];
                ink += e;
                engineInk += a;
                if (e > 0.02f || a > 0.02f)
                {
                    covered++;
                    difference += Math.Abs(e - a);
                }
                if (e >= 0.5f)
                {
                    lines++;
                    if (Near(actual, x, y) < 0.5f)
                        missed++;
                }
                if (a >= 0.5f)
                {
                    engineLines++;
                    if (Near(expected, x, y) < 0.5f)
                        extra++;
                }
            }
        // The local width: ink in the 7×7 pixels about each Freestyle line pixel, from summed-area tables.
        double[] Sums(float[] map)
        {
            var sums = new double[(width + 1) * (height + 1)];
            for (var y = 0; y < height; y++)
            {
                double row = 0;
                for (var x = 0; x < width; x++)
                {
                    row += map[y * width + x];
                    sums[(y + 1) * (width + 1) + x + 1] = sums[y * (width + 1) + x + 1] + row;
                }
            }
            return sums;
        }
        var (expectedSums, actualSums) = (Sums(expected), Sums(actual));
        double Box(double[] sums, int x, int y)
        {
            int x0 = Math.Max(0, x - 3), y0 = Math.Max(0, y - 3), x1 = Math.Min(width, x + 4), y1 = Math.Min(height, y + 4);
            return sums[y1 * (width + 1) + x1] - sums[y0 * (width + 1) + x1] - sums[y1 * (width + 1) + x0] + sums[y0 * (width + 1) + x0];
        }
        var unlike = 0;
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                if (expected[y * width + x] >= 0.5f && Box(actualSums, x, y) / Box(expectedSums, x, y) is < 0.75 or > 1.33)
                    unlike++;
        var text = new StringBuilder();
        if (unlike > 0.03 * lines)
            text.Append($"{unlike} of {lines} line pixels with unlike ink about them ");
        if (missed > 0.02 * lines)
            text.Append($"{missed} of {lines} line pixels missed ");
        if (extra > 0.03 * engineLines)
            text.Append($"{extra} of {engineLines} line pixels where Freestyle has none ");
        if (ink == 0 ? engineInk > 0 : Math.Abs(engineInk / ink - 1) > 0.1)
            text.Append($"ink {engineInk:F0} against {ink:F0} ");
        if (covered > 0 && difference / covered > 0.12)
            text.Append($"coverage differs by {difference / covered:F3} on average ");
        return text.ToString().Trim();
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
            foreach (var placed in scene.Models)
            {
                var model = Model.Load(device, Path.Combine(ModelDirectory, placed.File));
                models.Add(model);
                model.Draw(items, placed.Model, palette, scene.Style, placed.Scenery, placed.PoseOf(model.Data));
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
            var renderer = new Renderer3D(device) { InkCoverageOnly = inkOnly };
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
    /// The frame's outermost pixels aren't compared: Blender's jittered samples reach past the frame,
    /// so an edge just outside shades them. Inside (the reference's 3×3 neighbourhood is one colour):
    /// every channel within 2. On an edge,
    /// MSAA 4× covers a pixel in quarters and resolves it alone, where Blender filters 64 samples
    /// across neighbouring pixels: they differ by up to 0.61 of the edge's local contrast (the 3×3
    /// neighbourhood's range), 3.1 levels on average. An edge half a pixel out differs by about 9 on
    /// average; a missing or extra feature by about the whole contrast. So each edge pixel within 0.7
    /// of its local contrast (or 8 levels), and all of them within 4 on average. Returns what failed,
    /// or "".
    /// </summary>
    internal static string Compare(RgbaImage expected, RgbaImage actual, int insideTolerance = 2, double edgeContrast = 0.7, int edgeFloor = 8, double edgeMean = 4,
        Func<int, int, bool>? only = null, int strays = 0)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
            return $"size {actual.Width}x{actual.Height}, reference {expected.Width}x{expected.Height}";
        int w = expected.Width, h = expected.Height;
        byte[] e = expected.Pixels, a = actual.Pixels;
        int insideBad = 0, edgeBad = 0, worstInside = 0, worstEdge = 0, edgeCount = 0;
        long edgeSum = 0;
        for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                if (only != null && !only(x, y))
                    continue;
                // The frame's outermost pixels: Blender's jittered samples reach past the frame (an
                // edge just outside shades them), the engine's stay inside.
                if (x == 0 || y == 0 || x == w - 1 || y == h - 1)
                    continue;
                var i = (y * w + x) * 4;
                Span<int> low = [255, 255, 255], high = [0, 0, 0];
                for (var dy = -1; dy <= 1; dy++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        int nx = Math.Clamp(x + dx, 0, w - 1), ny = Math.Clamp(y + dy, 0, h - 1);
                        var j = (ny * w + nx) * 4;
                        for (var c = 0; c < 3; c++)
                        {
                            low[c] = Math.Min(low[c], e[j + c]);
                            high[c] = Math.Max(high[c], e[j + c]);
                        }
                    }
                var contrast = Math.Max(high[0] - low[0], Math.Max(high[1] - low[1], high[2] - low[2]));
                var uniform = contrast == 0;
                var diff = 0;
                for (var c = 0; c < 3; c++)
                    diff = Math.Max(diff, Math.Abs(e[i + c] - a[i + c]));
                if (uniform)
                {
                    worstInside = Math.Max(worstInside, diff);
                    if (diff > insideTolerance)
                        insideBad++;
                }
                else
                {
                    worstEdge = Math.Max(worstEdge, diff);
                    edgeCount++;
                    edgeSum += diff;
                    if (diff > edgeFloor && diff > edgeContrast * contrast)
                        edgeBad++;
                }
            }
        var text = new StringBuilder();
        if (insideBad + edgeBad <= strays)
            insideBad = edgeBad = 0;
        if (insideBad > 0)
            text.Append($"{insideBad} inside pixels differ (worst {worstInside}) ");
        if (edgeBad > 0)
            text.Append($"{edgeBad} edge pixels differ beyond {edgeContrast} of their contrast (worst {worstEdge}) ");
        if (edgeCount > 0 && (double)edgeSum / edgeCount > edgeMean)
            text.Append($"edges differ by {(double)edgeSum / edgeCount:F1} on average ");
        return text.ToString().Trim();
    }

    /// <summary>
    /// What each joint of a posed model does to what it moves (glTF model space, columns): back from
    /// its rest transform, then its transform in the pose. The same for every skin that uses the joint
    /// (an inverse bind matrix also carries its own mesh's place); Blender poses its bones to deform the same.
    /// </summary>
    static Dictionary<string, float[]>? Deformations(ModelObject3D placed)
    {
        if (placed.Pose.Count == 0)
            return null;
        var data = ModelData.Load(Path.Combine(ModelDirectory, placed.File));
        var rest = data.WorldMatrices();
        var world = data.WorldMatrices(placed.PoseOf(data));
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
            models = s.Models.Select(m => new { file = m.File, matrix = Columns(m.Model), scenery = m.Scenery, pose = Deformations(m) }).ToArray(),
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
