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

    public static TheoryData<string> SceneNames => new(Scenes3D.All.Select(s => s.Name));

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
        var actual = Render(scene);
        var expected = ImageFile.Load(reference);
        var problems = Compare(expected, actual);
        if (problems.Length > 0)
        {
            ImageFile.SavePng(Golden.ActualPath("r3d-" + name), actual);
            Assert.Fail($"{name}: {problems}; render in {Golden.ActualPath("r3d-" + name)}");
        }
    }

    internal RgbaImage Render(Scene3D scene)
    {
        var device = gpu.Device;
        var meshes = new Dictionary<Shape3D, Mesh>();
        try
        {
            var items = scene.Objects.Select(o =>
            {
                if (!meshes.TryGetValue(o.Shape, out var mesh))
                    meshes[o.Shape] = mesh = Mesh.FromTriangles(device, o.Shape.Positions, o.Shape.Triangles);
                return new DrawItem(mesh, o.Model, Colors.FromHex(o.Color));
            }).ToList();
            using var target = new RenderTarget3D(device, scene.Width, scene.Height);
            var renderer = new Renderer3D(device);
            var frame = device.BeginFrame();
            renderer.Render(frame, target, scene.Camera.ToCamera(), items, Colors.FromHex(scene.Background));
            device.EndFrame(frame);
            device.WaitIdle();
            return new RgbaImage(scene.Width, scene.Height, target.Resolved.ReadPixels());
        }
        finally
        {
            foreach (var mesh in meshes.Values)
                mesh.Dispose();
        }
    }

    /// <summary>
    /// Inside (the reference's 3×3 neighbourhood is one colour): every channel within 2. On an edge,
    /// MSAA 4× covers a pixel in quarters where Blender filters 64 samples, which differ by up to 46
    /// levels (2.9 on average) in these scenes; an edge half a pixel out differs by 65 or more (8 on
    /// average). So each edge pixel within 56 and all of them within 4 on average. Returns what
    /// failed, or "".
    /// </summary>
    internal static string Compare(RgbaImage expected, RgbaImage actual, int insideTolerance = 2, int edgeTolerance = 56, double edgeMean = 4)
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
                var i = (y * w + x) * 4;
                var uniform = true;
                for (var dy = -1; dy <= 1 && uniform; dy++)
                    for (var dx = -1; dx <= 1 && uniform; dx++)
                    {
                        int nx = Math.Clamp(x + dx, 0, w - 1), ny = Math.Clamp(y + dy, 0, h - 1);
                        var j = (ny * w + nx) * 4;
                        uniform = e[j] == e[i] && e[j + 1] == e[i + 1] && e[j + 2] == e[i + 2];
                    }
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
                    if (diff > edgeTolerance)
                        edgeBad++;
                }
            }
        var text = new StringBuilder();
        if (insideBad > 0)
            text.Append($"{insideBad} inside pixels differ (worst {worstInside}) ");
        if (edgeBad > 0)
            text.Append($"{edgeBad} edge pixels differ beyond {edgeTolerance} (worst {worstEdge}) ");
        if (edgeCount > 0 && (double)edgeSum / edgeCount > edgeMean)
            text.Append($"edges differ by {(double)edgeSum / edgeCount:F1} on average ");
        return text.ToString().Trim();
    }

    /// <summary>The scenes for Tools/BlenderReference/render_scenes.py: shapes, each object's model matrix (column vectors) and colour, the camera.</summary>
    static string SceneFile()
    {
        static float[] V(Vector3 v) => [v.X, v.Y, v.Z];
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
        });
        return JsonSerializer.Serialize(new { shapes, scenes }, new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }
}
