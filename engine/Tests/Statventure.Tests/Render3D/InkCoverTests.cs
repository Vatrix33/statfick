using System.Numerics;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Render3D;

/// <summary>
/// What covers a feature edge (Docs/Renderer-3D.md, "Feature edges"; GM2's ink task): an edge lying under another surface
/// is hidden as Freestyle hides it (any other surface in front of it, however close), while against its own piece it keeps
/// the allowance the surface's coarse normals need; two pieces of one part meeting draw no line (an intersection), and a piece's
/// contour lying on another piece is drawn (a silhouette of its mesh). Small scenes built in code, the lines drawn alone
/// (<see cref="Renderer3D.InkCoverageOnly"/>), each checked where its rule decides, with a control that the line is drawn
/// there without what covers it.
/// </summary>
[Collection("GPU")]
public class InkCoverTests(GpuFixture gpu)
{
    /// <summary>Triangles with their face's normal (flat), as glTF gives a flat-shaded mesh.</summary>
    sealed class Piece
    {
        public readonly List<Vector3> Positions = [];
        public readonly List<Vector3> Normals = [];
        public readonly List<uint> Indices = [];

        public Piece Triangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
            foreach (var p in new[] { a, b, c })
            {
                Indices.Add((uint)Positions.Count);
                Positions.Add(p);
                Normals.Add(normal);
            }
            return this;
        }

        public Piece Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) => Triangle(a, b, c).Triangle(a, c, d);

        /// <summary>The piece moved by a matrix (squashed, too: each face's normal made again from its corners).</summary>
        public Piece Moved(Matrix4x4 matrix)
        {
            for (var i = 0; i < Positions.Count; i++)
                Positions[i] = Vector3.Transform(Positions[i], matrix);
            for (var t = 0; t < Indices.Count; t += 3)
            {
                int a = (int)Indices[t], b = (int)Indices[t + 1], c = (int)Indices[t + 2];
                Normals[a] = Normals[b] = Normals[c] = Vector3.Normalize(Vector3.Cross(Positions[b] - Positions[a], Positions[c] - Positions[a]));
            }
            return this;
        }

        /// <summary>Every other quad (pair of triangles) taken out into a piece of its own: one surface in two primitives.</summary>
        public Piece Halve()
        {
            var other = new Piece();
            var kept = new Piece();
            for (var t = 0; t < Indices.Count / 3; t++)
            {
                var into = t / 2 % 2 == 0 ? kept : other;
                into.Triangle(Positions[(int)Indices[t * 3]], Positions[(int)Indices[t * 3 + 1]], Positions[(int)Indices[t * 3 + 2]]);
            }
            Positions.Clear();
            Normals.Clear();
            Indices.Clear();
            Positions.AddRange(kept.Positions);
            Normals.AddRange(kept.Normals);
            Indices.AddRange(kept.Indices);
            return other;
        }

        /// <summary>A box, its faces wound outward.</summary>
        public Piece Box(Vector3 min, Vector3 max)
        {
            Vector3 c = (min + max) / 2, h = (max - min) / 2;
            Vector3 x = new(h.X, 0, 0), y = new(0, h.Y, 0), z = new(0, 0, h.Z);
            foreach (var (n, u, v) in new[] { (x, y, z), (-x, z, y), (y, z, x), (-y, x, z), (z, x, y), (-z, y, x) })
                Quad(c + n - u - v, c + n + u - v, c + n + u + v, c + n - u + v);
            return this;
        }

        /// <summary>
        /// A closed body of revolution about the axis from <paramref name="a"/> to <paramref name="b"/>: rings at the
        /// heights given (0 to 1) with their radii, <paramref name="segments"/> around, capped at both ends.
        /// </summary>
        public Piece Lathe(Vector3 a, Vector3 b, int segments, params (float At, float Radius)[] rings)
        {
            var d = Vector3.Normalize(b - a);
            var e1 = Vector3.Normalize(Vector3.Cross(d, MathF.Abs(d.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX));
            var e2 = Vector3.Cross(d, e1);
            Vector3 Point(int ring, int k)
            {
                var angle = 2 * MathF.PI * k / segments;
                return a + (b - a) * rings[ring].At + (e1 * MathF.Cos(angle) + e2 * MathF.Sin(angle)) * rings[ring].Radius;
            }
            for (var r = 0; r + 1 < rings.Length; r++)
                for (var k = 0; k < segments; k++)
                    Quad(Point(r, k), Point(r, k + 1), Point(r + 1, k + 1), Point(r + 1, k));
            Vector3 bottom = a + (b - a) * rings[0].At, top = a + (b - a) * rings[^1].At;
            for (var k = 0; k < segments; k++)
            {
                Triangle(bottom, Point(0, k + 1), Point(0, k));
                Triangle(top, Point(rings.Length - 1, k), Point(rings.Length - 1, k + 1));
            }
            return this;
        }
    }

    /// <summary>One node showing one mesh: each piece a primitive of it, or all the pieces one primitive (their islands apart).</summary>
    static ModelData Model(bool onePrimitive, params Piece[] pieces)
    {
        ModelPrimitive Primitive(IEnumerable<Piece> of)
        {
            var (positions, normals, indices) = (new List<Vector3>(), new List<Vector3>(), new List<uint>());
            foreach (var piece in of)
            {
                indices.AddRange(piece.Indices.Select(i => i + (uint)positions.Count));
                positions.AddRange(piece.Positions);
                normals.AddRange(piece.Normals);
            }
            return new ModelPrimitive { Positions = [.. positions], Normals = [.. normals], Indices = [.. indices] };
        }
        return new ModelData
        {
            Nodes = [new ModelNode("piece", -1, [], Matrix4x4.Identity, Vector3.Zero, Quaternion.Identity, Vector3.One, 0, -1)],
            Meshes = [new ModelMesh("pieces", onePrimitive ? [Primitive(pieces)] : pieces.Select(p => Primitive([p])).ToArray())],
            Materials = [], Skins = [], Roots = [0],
        };
    }

    /// <summary>The ink lines alone (coverage 0..1) of models drawn by the camera; each entry's owner groups its model with others.</summary>
    float[] Ink(Camera camera, int width, int height, params (ModelData Data, object Owner)[] models)
    {
        var device = gpu.Device;
        var loaded = models.Select(m => (Model: Statventure.Render3D.Model.Create(device, m.Data), m.Owner)).ToList();
        try
        {
            var items = new List<DrawItem>();
            foreach (var (model, owner) in loaded)
                model.Draw(items, Matrix4x4.Identity, owner: owner);
            using var target = new RenderTarget3D(device, width, height);
            using var renderer = new Renderer3D(device) { InkCoverageOnly = true };
            var frame = device.BeginFrame();
            renderer.Render(frame, target, camera, items, new Vector4(0.5f, 0.5f, 0.5f, 1), new SunLight(new(1, 1.1f, 0.7f), 3));
            device.EndFrame(frame);
            device.WaitIdle();
            var pixels = target.Resolved.ReadPixels();
            return Enumerable.Range(0, width * height).Select(i => Colors.ToLinear(pixels[i * 4] / 255f)).ToArray();
        }
        finally
        {
            foreach (var (model, _) in loaded)
                model.Dispose();
        }
    }

    /// <summary>The most ink within a pixel of where each world point lies on screen, the least of those over the points.</summary>
    static (float Least, float Most) InkAlong(float[] ink, Camera camera, int width, int height, IEnumerable<Vector3> points)
    {
        var (least, most) = (float.MaxValue, 0f);
        var viewProjection = camera.ViewProjection((float)width / height);
        foreach (var point in points)
        {
            var clip = Vector4.Transform(new Vector4(point, 1), viewProjection);
            int px = (int)MathF.Floor((clip.X / clip.W * 0.5f + 0.5f) * width), py = (int)MathF.Floor((clip.Y / clip.W * 0.5f + 0.5f) * height);
            var near = 0f;
            for (var dy = -1; dy <= 1; dy++)
                for (var dx = -1; dx <= 1; dx++)
                    near = MathF.Max(near, ink[Math.Clamp(py + dy, 0, height - 1) * width + Math.Clamp(px + dx, 0, width - 1)]);
            (least, most) = (MathF.Min(least, near), MathF.Max(most, near));
        }
        return (least, most);
    }

    static IEnumerable<Vector3> Line(Vector3 from, Vector3 to, int count = 40) =>
        Enumerable.Range(0, count).Select(i => Vector3.Lerp(from, to, i / (count - 1f)));

    // The plate the tests lay things on, its top at y = 0; seen from above, a little tilted, 2.5 cm a pixel (160 × 160).
    static Piece Plate() => new Piece().Box(new(-1.5f, -0.1f, -1.5f), new(1.5f, 0, 1.5f));
    static readonly Camera Above = new Camera3D(Projection.Orthographic, new(0.4f, 10, 1.2f), Vector3.Zero, OrthographicHeight: 4) { Up = -Vector3.UnitZ }.ToCamera();

    /// <summary>The plate's mesh on two nodes, the second moved by <paramref name="by"/> (a mesh instanced twice in a model).</summary>
    static ModelData Twice(Vector3 by)
    {
        var once = Model(true, Plate());
        return new ModelData
        {
            Nodes =
            [
                once.Nodes[0],
                new ModelNode("again", -1, [], Matrix4x4.CreateTranslation(by), by, Quaternion.Identity, Vector3.One, 0, -1),
            ],
            Meshes = once.Meshes, Materials = [], Skins = [], Roots = [0, 1],
        };
    }

    /// <summary>
    /// A sheet lying 4 mm over the plate's right edge, 2 mm thick (a quarter of a pixel's depth, under the half pixel the
    /// edge was once allowed): the plate's crease there is hidden under it, whether the sheet is another piece of the
    /// plate's own mesh, another mesh of its model, another model, or the plate's mesh again on another node 6 mm up. The
    /// plate's left crease, uncovered, is drawn, and so is the right one without the sheet. Checked: the sheets ignored, a
    /// mesh's pieces taken as one, a mesh's nodes taken as one, or the old allowance against another surface each draw the
    /// covered crease.
    /// </summary>
    [Theory]
    [InlineData("another piece of its mesh")]
    [InlineData("another mesh of its model")]
    [InlineData("another model")]
    [InlineData("its mesh on another node")]
    public void AnEdgeUnderAnotherSurfaceIsHidden(string cover)
    {
        var sheet = new Piece().Box(new(0.5f, 0.004f, -1.8f), new(1.8f, 0.006f, 1.8f));
        var owner = new object();
        (ModelData, object)[] models = cover switch
        {
            "another piece of its mesh" => [(Model(true, Plate(), sheet), owner)],
            "another mesh of its model" => [(Model(true, Plate()), owner), (Model(true, sheet), owner)],
            "another model" => [(Model(true, Plate()), owner), (Model(true, sheet), new object())],
            _ => [(Twice(new(2.3f, 0.006f, 0)), owner)],
        };
        var right = Line(new(1.5f, 0, -1.2f), new(1.5f, 0, 1.2f));
        var ink = Ink(Above, 160, 160, models);
        Assert.True(InkAlong(ink, Above, 160, 160, right).Most < 0.1f, $"{cover}: the plate's crease drawn under the sheet");
        Assert.True(InkAlong(ink, Above, 160, 160, Line(new(-1.5f, 0, -1.2f), new(-1.5f, 0, 1.2f))).Least >= 0.5f, $"{cover}: the uncovered crease missing");
        var bare = Ink(Above, 160, 160, (Model(true, Plate()), owner));
        Assert.True(InkAlong(bare, Above, 160, 160, right).Least >= 0.5f, "the crease missing without the sheet");
    }

    /// <summary>
    /// A drum's top rim (the lid meeting a waisted side in fine rings, a crease) seen from 6° above the lid, which runs
    /// nearly edge-on: the crease against its own faces is drawn all along the near half, with no break. Checked: no
    /// allowance against the edge's own surface breaks it.
    /// </summary>
    [Fact]
    public void AnEdgeOnItsOwnFacesIsDrawnAtGrazingAngles()
    {
        var rings = Enumerable.Range(0, 41).Select(i => (i / 40f, 1 - 0.06f * MathF.Sin(MathF.PI * i / 40))).ToArray();
        var drum = new Piece().Lathe(Vector3.Zero, new(0, 0.4f, 0), 64, rings);
        var elevation = 6 * MathF.PI / 180;
        var camera = new Camera3D(Projection.Orthographic, new Vector3(0, 0.4f, 0) + 10 * new Vector3(0, MathF.Sin(elevation), MathF.Cos(elevation)), new(0, 0.4f, 0),
            OrthographicHeight: 1.2f).ToCamera();
        var ink = Ink(camera, 360, 120, (Model(true, drum), new object()));
        var near = Enumerable.Range(0, 80).Select(i => (6 + 168 * i / 79f) * MathF.PI / 180).Select(a => new Vector3(MathF.Cos(a), 0.4f, MathF.Sin(a)));
        Assert.True(InkAlong(ink, camera, 360, 120, near).Least >= 0.5f, "the rim's crease broken");
    }

    /// <summary>
    /// A post pushed through the plate, both pieces of one mesh: where its sides meet the plate's top (at a right angle,
    /// without a break in depth) no line is drawn, as Freestyle draws no intersections; the post's own top crease is.
    /// Checked: the crease rule across pieces draws the meeting.
    /// </summary>
    [Fact]
    public void TwoPiecesMeetingDrawNoLine()
    {
        var post = new Piece().Box(new(-0.4f, -0.5f, -0.4f), new(0.4f, 0.8f, 0.4f));
        var camera = new Camera3D(Projection.Orthographic, new(0.3f, 6, 8), Vector3.Zero, OrthographicHeight: 4).ToCamera();
        var ink = Ink(camera, 160, 160, (Model(true, Plate(), post), new object()));
        Assert.True(InkAlong(ink, camera, 160, 160, Line(new(-0.25f, 0, 0.4f), new(0.25f, 0, 0.4f), 20)).Most < 0.1f, "a line where the post meets the plate");
        Assert.True(InkAlong(ink, camera, 160, 160, Line(new(-0.25f, 0.8f, 0.4f), new(0.25f, 0.8f, 0.4f), 20)).Least >= 0.5f, "the post's top crease missing");
    }

    /// <summary>
    /// A lock lying on the plate (a flattened rod 8 cm wide and 2 cm tall, smooth in 64 faces, its lathe's seam underneath,
    /// 1 mm over the plate; both pieces of one mesh), 1 cm a pixel: its contour, about a pixel's depth over the plate, is
    /// no break the per-pixel tests see, its faces turn too gently for a crease, and the pieces' meeting draws none; the
    /// lock's silhouette edges draw it along both sides, also with every other face of the lock in another primitive (a
    /// material's seam at each of its edges). Checked: no silhouettes, silhouettes dropped beside another piece, or an
    /// edge's second face in another primitive not found, lose it. (A round rod a few pixels across is no test of this:
    /// its per-pixel creases draw its sides by themselves.)
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void APiecesContourOnAnotherIsDrawn(bool twoPrimitives)
    {
        var flat = new Piece().Lathe(new(-1, 0, 0), new(1, 0, 0), 64, (0, 1), (1, 1))
            .Moved(Matrix4x4.CreateRotationX(MathF.PI / 2) * Matrix4x4.CreateScale(1, 0.01f, 0.04f) * Matrix4x4.CreateTranslation(0, 0.011f, 0));
        var model = twoPrimitives ? Model(false, Plate(), flat, flat.Halve()) : Model(true, Plate(), flat);
        var camera = new Camera3D(Projection.Orthographic, new(0.1f, 10, 0.3f), Vector3.Zero, OrthographicHeight: 1.6f) { Up = -Vector3.UnitZ }.ToCamera();
        var ink = Ink(camera, 160, 160, (model, new object()));
        foreach (var side in new[] { -1, 1 })
            Assert.True(InkAlong(ink, camera, 160, 160, Line(new(-0.6f, 0.011f, side * 0.04f), new(0.6f, 0.011f, side * 0.04f))).Least >= 0.5f,
                $"the lock's contour missing on its {(side < 0 ? "far" : "near")} side");
    }

    /// <summary>
    /// A rod 3 cm across lying on the plate, 1 cm a pixel (16 faces round; both pieces of one mesh): the lines along its
    /// sides are drawn. The faces seen in the pixels there are the rod's own but not the edges' (its curve turns a face
    /// or two a pixel): carried to an edge, their planes lie over it by up to half a pixel, which the edge's own sheet
    /// allows and another sheet's precision wouldn't. Checked: no allowance against the own sheet, a mesh's islands not
    /// joined, every edge on island 0, or the surface records read as sheets, lose them.
    /// </summary>
    [Fact]
    public void ARodsSidesAreDrawnOverItsOwnCurve()
    {
        const float Radius = 0.015f;
        var rod = new Piece().Lathe(new(-1, Radius + 0.001f, 0), new(1, Radius + 0.001f, 0), 16, (0, Radius), (1, Radius));
        var camera = new Camera3D(Projection.Orthographic, new(0.1f, 10, 0.3f), Vector3.Zero, OrthographicHeight: 1.6f) { Up = -Vector3.UnitZ }.ToCamera();
        var ink = Ink(camera, 160, 160, (Model(true, Plate(), rod), new object()));
        foreach (var side in new[] { -1, 1 })
            Assert.True(InkAlong(ink, camera, 160, 160, Line(new(-0.6f, Radius, side * Radius), new(0.6f, Radius, side * Radius))).Least >= 0.5f,
                $"the rod's line missing on its {(side < 0 ? "far" : "near")} side");
    }

    /// <summary>
    /// A rod 8 mm across on the plate under a sheet 1 mm over it, all three pieces of the plate's mesh: the rod's contour
    /// lies 7 mm under the sheet's face (under half a pixel's depth, 25 mm a pixel here) and is hidden; without the sheet
    /// it is drawn. Checked: the old half pixel against another surface draws it. (The first rule, the edge's own faces'
    /// planes compared with the cover's, drew it: those planes carried a pixel off the edge put the sheet behind them.)
    /// </summary>
    [Fact]
    public void AContourUnderAnotherSurfaceIsHidden()
    {
        const float Radius = 0.004f;
        Piece Rod() => new Piece().Lathe(new(-1, Radius + 0.001f, 0), new(1, Radius + 0.001f, 0), 16, (0, Radius), (1, Radius));
        var sheet = new Piece().Box(new(-1.2f, 2 * Radius + 0.002f, -0.3f), new(1.2f, 2 * Radius + 0.004f, 0.3f));
        var contour = new[] { -1, 1 }.SelectMany(side => Line(new(-0.6f, Radius + 0.001f, side * Radius), new(0.6f, Radius + 0.001f, side * Radius))).ToArray();
        var covered = Ink(Above, 160, 160, (Model(true, Plate(), Rod(), sheet), new object()));
        Assert.True(InkAlong(covered, Above, 160, 160, contour).Most < 0.1f, "the rod's contour drawn under the sheet");
        var bare = Ink(Above, 160, 160, (Model(true, Plate(), Rod()), new object()));
        Assert.True(InkAlong(bare, Above, 160, 160, contour).Least >= 0.5f, "the rod's contour missing without the sheet");
    }
}
