using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Tests.Render3D;

/// <summary>A test shape: positions and counter-clockwise (outward) triangles, in glTF's coordinates.</summary>
public sealed record Shape3D(string Name, Vector3[] Positions, int[] Triangles)
{
    public static readonly Shape3D Cube = new("cube",
        [new(-0.5f, -0.5f, -0.5f), new(0.5f, -0.5f, -0.5f), new(0.5f, 0.5f, -0.5f), new(-0.5f, 0.5f, -0.5f),
         new(-0.5f, -0.5f, 0.5f), new(0.5f, -0.5f, 0.5f), new(0.5f, 0.5f, 0.5f), new(-0.5f, 0.5f, 0.5f)],
        [0, 2, 1, 0, 3, 2, // back (−z)
         4, 5, 6, 4, 6, 7, // front (+z)
         0, 1, 5, 0, 5, 4, // bottom (−y)
         3, 7, 6, 3, 6, 2, // top (+y)
         0, 4, 7, 0, 7, 3, // left (−x)
         1, 2, 6, 1, 6, 5]); // right (+x)

    public static readonly Shape3D Tetrahedron = new("tetrahedron",
        [new(0, 0.9f, 0), new(-0.6f, -0.3f, 0.5f), new(0.65f, -0.3f, 0.45f), new(0, -0.35f, -0.7f)],
        [0, 1, 2, 0, 2, 3, 0, 3, 1, 1, 3, 2]);

    public static readonly Shape3D Prism = new("prism",
        [new(-0.6f, -0.4f, -0.5f), new(0.6f, -0.4f, -0.5f), new(0, 0.6f, -0.5f),
         new(-0.6f, -0.4f, 0.5f), new(0.6f, -0.4f, 0.5f), new(0, 0.6f, 0.5f)],
        [0, 2, 1, // back
         3, 4, 5, // front
         0, 1, 4, 0, 4, 3, // bottom
         1, 2, 5, 1, 5, 4, // right slope
         2, 0, 3, 2, 3, 5]); // left slope
}

/// <summary>A placed shape: scale, rotation (degrees about X, then Y, then Z) and translation, and an sRGB colour.</summary>
public sealed record Object3D(Shape3D Shape, Vector3 Translation, Vector3 RotationDegrees, Vector3 Scale, string Color)
{
    public Matrix4x4 Model =>
        Matrix4x4.CreateScale(Scale)
        * Matrix4x4.CreateRotationX(RotationDegrees.X * MathF.PI / 180)
        * Matrix4x4.CreateRotationY(RotationDegrees.Y * MathF.PI / 180)
        * Matrix4x4.CreateRotationZ(RotationDegrees.Z * MathF.PI / 180)
        * Matrix4x4.CreateTranslation(Translation);
}

/// <summary>A camera as the scene file gives it to Blender (field of view in degrees).</summary>
public sealed record Camera3D(Projection Projection, Vector3 Position, Vector3 Target, float FieldOfViewDegrees = 40, float OrthographicHeight = 4, float Near = 0.1f, float Far = 50)
{
    public Vector3 Up { get; init; } = Vector3.UnitY;

    public Camera ToCamera() => new()
    {
        Projection = Projection,
        Position = Position,
        Target = Target,
        Up = Up,
        VerticalFieldOfView = FieldOfViewDegrees * MathF.PI / 180,
        OrthographicHeight = OrthographicHeight,
        Near = Near,
        Far = Far,
    };
}

/// <summary>
/// A test model (Data/Models/&lt;File&gt;, made by Tools/BlenderReference/make_models.py) placed by a
/// rotation (degrees about X, then Y, then Z), a uniform scale and a translation; scenery receives
/// shadows.
/// </summary>
public sealed record ModelObject3D(string File, Vector3 Translation, Vector3 RotationDegrees, float Scale = 1, bool Scenery = false)
{
    public Matrix4x4 Model =>
        Matrix4x4.CreateScale(Scale)
        * Matrix4x4.CreateRotationX(RotationDegrees.X * MathF.PI / 180)
        * Matrix4x4.CreateRotationY(RotationDegrees.Y * MathF.PI / 180)
        * Matrix4x4.CreateRotationZ(RotationDegrees.Z * MathF.PI / 180)
        * Matrix4x4.CreateTranslation(Translation);
}

/// <summary>
/// A test scene rendered by Blender (Tools/BlenderReference) and by the engine: shapes (flat
/// colours), models (cel-shaded), the palette slots (name to "#rrggbb") the models' materials take,
/// shadow tones the palette gives some slots, the sun and the cel style.
/// </summary>
public sealed record Scene3D(string Name, int Width, int Height, string Background, Camera3D Camera, Object3D[] Objects)
{
    public ModelObject3D[] Models { get; init; } = [];
    public Dictionary<string, string> Palette { get; init; } = [];
    public Dictionary<string, string> Shadows { get; init; } = [];
    public SunLight? Sun { get; init; }
    public CelStyle Style { get; init; } = CelStyle.Default;
    /// <summary>The device scale: ink line widths are the style's times it.</summary>
    public float Scale { get; init; } = 1;
}

public static class Scenes3D
{
    static Object3D Box(Vector3 at, Vector3 rotation, Vector3 scale, string color) => new(Shape3D.Cube, at, rotation, scale, color);

    static readonly Object3D[] Boxes =
    [
        Box(new(0, 0, 0), new(20, 35, 0), Vector3.One, "#8a6a4a"),
        Box(new(1.3f, -0.2f, -0.6f), new(0, -20, 10), new(0.6f, 1.6f, 0.6f), "#4a6a8a"),
        Box(new(-0.9f, -0.75f, 0.6f), new(0, 15, 0), new(1.8f, 0.4f, 0.9f), "#6a8a4a"),
    ];

    /// <summary>The lizardman test's sun: from the upper right, on the viewer's side (Blender's (1, −0.7, 1.1)), strength 3.</summary>
    static readonly SunLight Lizardman = new(new(1, 1.1f, 0.7f), 3);

    public static readonly Scene3D[] All =
    [
        // Depth between overlapping boxes, the camera above and to the side, orthographic.
        new("ortho-boxes", 320, 240, "#d8cbb0", new Camera3D(Projection.Orthographic, new(4, 3, 6), new(0, 0, 0), OrthographicHeight: 3.6f), Boxes),
        // The same boxes in perspective, close, one box passing through another.
        new("perspective-boxes", 320, 240, "#d8cbb0", new Camera3D(Projection.Perspective, new(2.6f, 2.0f, 3.4f), new(0.1f, -0.1f, 0), FieldOfViewDegrees: 45),
            [.. Boxes, Box(new(0.4f, 0.1f, 0.2f), new(45, 0, 30), new(0.3f, 0.3f, 1.6f), "#8a4a6a")]),
        // Slanted faces, a wide aspect, a rolled camera (up isn't Y), shapes cut by the frame's edge.
        new("shapes-roll", 400, 200, "#b8b0a0", new Camera3D(Projection.Perspective, new(-1.5f, 1.2f, 3.2f), new(0.3f, 0, 0), FieldOfViewDegrees: 50) { Up = Vector3.Normalize(new(0.3f, 1, 0)) },
            [new(Shape3D.Tetrahedron, new(-0.6f, 0, 0), new(0, 30, 0), Vector3.One, "#a05a3c"),
             new(Shape3D.Prism, new(1.0f, -0.1f, -0.3f), new(10, -40, 5), new(1.2f, 1, 1.4f), "#3c6aa0"),
             new(Shape3D.Cube, new(2.6f, 0.4f, -0.5f), new(0, 20, 0), new(1, 1.5f, 1), "#5a8a5a")]),
        // A glTF model: a node hierarchy, two materials on one mesh, a mesh used by two nodes (one
        // mirrored, both sunk into the slab), a double-sided banner seen from behind, palette slots
        // (cloth.001 takes cloth) next to materials the palette doesn't name (stone, glow).
        new("model-blocks", 360, 270, "#d8cbb0", new Camera3D(Projection.Perspective, new(-1.2f, 2.6f, -3.6f), new(0, 0.7f, 0), FieldOfViewDegrees: 40), [])
        {
            Models = [new("blocks.glb", Vector3.Zero, Vector3.Zero)],
            Palette = new() { ["metal.base"] = "#5f6a78", ["metal.trim"] = "#b08a3c", ["cloth"] = "#3f4f8f" },
            Sun = new SunLight(new(-0.8f, 1.2f, -0.5f), 3),
        },
        // A skinned model in its rest pose (placed by its joints, not its node), side view, orthographic,
        // from somewhat above: Freestyle can't tell whether a face seen exactly edge-on (the tube's
        // flat top under a level side view) is a contour and draws it thin with odd corners, and a
        // cap seen as a sliver a pixel tall is shaded once per pixel under MSAA (Blender per sample).
        new("model-rigged", 240, 320, "#b8b0a0", new Camera3D(Projection.Orthographic, new(0.3f, 2.6f, 5), new(0.3f, 1.1f, 0), OrthographicHeight: 2.8f), [])
        {
            Models = [new("rigged.glb", new(-0.1f, 0, 0), new(0, 30, 0))],
            Palette = new() { ["skin"] = "#8a6a52" },
            Sun = Lizardman,
        },
        // Cel shading: band edges across smooth surfaces (a sphere scaled unevenly, a torus), flat
        // faces, a slot whose shadow tone the palette gives (metal.base), a colour no palette names
        // (stone), a double-sided card seen from behind, a mirrored smooth cone; the lizardman's sun.
        new("cel-shapes", 420, 240, "#d8cbb0", new Camera3D(Projection.Perspective, new(0.2f, 1.5f, 4.4f), new(0.1f, 0.45f, 0), FieldOfViewDegrees: 38), [])
        {
            Models = [new("shapes.glb", Vector3.Zero, Vector3.Zero)],
            Palette = new() { ["cloth"] = "#7b5a3a", ["metal.base"] = "#8a8f96", ["metal.trim"] = "#c9a46a" },
            Shadows = new() { ["metal.base"] = "#2f3540" },
            Sun = Lizardman,
        },
        // Close up, the band edge runs across large smooth surfaces, where a small error in where it
        // falls moves it by pixels (a band edge 5% off shifts it about 2 pixels here).
        new("cel-closeup", 320, 320, "#d8cbb0", new Camera3D(Projection.Perspective, new(-1.05f, 0.75f, 1.55f), new(-0.95f, 0.5f, 0), FieldOfViewDegrees: 40), [])
        {
            Models = [new("shapes.glb", Vector3.Zero, Vector3.Zero)],
            Palette = new() { ["cloth"] = "#7b5a3a", ["metal.base"] = "#8a8f96", ["metal.trim"] = "#c9a46a" },
            Sun = Lizardman,
        },
        // Two models: the rigged tube in front of the shapes gets its outline against them (each
        // model is outlined where it meets anything else), the shapes theirs against the background.
        new("ink-models", 400, 260, "#d8cbb0", new Camera3D(Projection.Perspective, new(0.4f, 1.3f, 4.6f), new(0.2f, 0.6f, 0), FieldOfViewDegrees: 40), [])
        {
            Models = [new("shapes.glb", Vector3.Zero, Vector3.Zero), new("rigged.glb", new(0.2f, -0.2f, 0.9f), new(0, 20, -12), 0.7f)],
            Palette = new() { ["cloth"] = "#7b5a3a", ["metal.base"] = "#8a8f96", ["metal.trim"] = "#c9a46a", ["skin"] = "#a9805e" },
            Sun = Lizardman,
        },
        // On a screen with twice the pixel density: twice the size and twice the ink widths (lines are a
        // fixed width on screen, times the device scale).
        new("cel-shapes-2x", 840, 480, "#d8cbb0", new Camera3D(Projection.Perspective, new(0.2f, 1.5f, 4.4f), new(0.1f, 0.45f, 0), FieldOfViewDegrees: 38), [])
        {
            Models = [new("shapes.glb", Vector3.Zero, Vector3.Zero)],
            Palette = new() { ["cloth"] = "#7b5a3a", ["metal.base"] = "#8a8f96", ["metal.trim"] = "#c9a46a" },
            Shadows = new() { ["metal.base"] = "#2f3540" },
            Sun = Lizardman,
            Scale = 2,
        },
        // Shadows on scenery (Docs/Renderer-3D.md, "Shadows"): the stage receives the shadows of the
        // shapes and the tube standing on it, and its own (the ring's has a hole); the shapes and the
        // tube receive none. The sun is 3° across, softening shadows further from their casters.
        new("shadow-stage", 420, 280, "#b8b0a0", new Camera3D(Projection.Perspective, new(0.6f, 3.2f, 5.2f), new(0.2f, 0.2f, -0.3f), FieldOfViewDegrees: 44), [])
        {
            Models = [new("stage.glb", Vector3.Zero, Vector3.Zero, Scenery: true), new("shapes.glb", Vector3.Zero, Vector3.Zero), new("rigged.glb", new(-1.2f, 0, 0.6f), new(0, 20, 0), 0.8f)],
            Palette = new() { ["cloth"] = "#7b5a3a", ["metal.base"] = "#8a8f96", ["metal.trim"] = "#c9a46a", ["skin"] = "#a9805e" },
            Sun = new SunLight(new(0.9f, 1.4f, 0.8f), 3),
        },
        // The battle view: orthographic from the side, a little from above, the sun from the left. (A
        // lower sun casts long thin shadows whose soft edges sit right at the two-tone threshold: both
        // renderers break them into dashes, in different places.)
        new("shadow-side", 420, 220, "#b8b0a0", new Camera3D(Projection.Orthographic, new(0, 1.6f, 6), new(0, 0.5f, 0), OrthographicHeight: 2.6f), [])
        {
            Models = [new("stage.glb", Vector3.Zero, Vector3.Zero, Scenery: true), new("shapes.glb", Vector3.Zero, Vector3.Zero), new("rigged.glb", new(-1.2f, 0, 0.6f), new(0, 20, 0), 0.8f)],
            Palette = new() { ["cloth"] = "#3f4f8f", ["metal.base"] = "#5f6a78", ["metal.trim"] = "#b08a3c", ["skin"] = "#a9805e" },
            Sun = new SunLight(new(-1, 1, 0.5f), 3),
        },
        // The map view: orthographic, from above (up is −Z), the sun from the south-west. Tilted 4° off
        // straight down: seen exactly from above every vertical face is edge-on, which Freestyle can't
        // place (it draws the slab's sides thin and the upright card as a stray line).
        new("shadow-map", 360, 260, "#b8b0a0", new Camera3D(Projection.Orthographic, new(0, 8, 0.56f), new(0, 0, 0), OrthographicHeight: 4.2f) { Up = -Vector3.UnitZ },
            [])
        {
            Models = [new("stage.glb", Vector3.Zero, Vector3.Zero, Scenery: true), new("shapes.glb", Vector3.Zero, Vector3.Zero), new("rigged.glb", new(-1.2f, 0, 0.6f), new(0, 20, 0), 0.8f)],
            Palette = new() { ["cloth"] = "#7b5a3a", ["metal.base"] = "#8a8f96", ["metal.trim"] = "#c9a46a", ["skin"] = "#a9805e" },
            Sun = new SunLight(new(-0.7f, 1.3f, 0.9f), 3),
        },
        // Wide fringes: a pole and a bar floating 1.5 m up cast long shadows seen from (nearly) above at
        // about 200 px/m, where a 3° sun spreads the fringe over 15 pixels or so: the sun's size, the
        // blend across the fringe and light passing under a floating part all show.
        new("shadow-fringe", 400, 320, "#b8b0a0", new Camera3D(Projection.Orthographic, new(0.2f, 6, 0.42f), new(0.2f, 0, 0), OrthographicHeight: 1.6f) { Up = -Vector3.UnitZ }, [])
        {
            Models = [new("fringe.glb", Vector3.Zero, Vector3.Zero, Scenery: true)],
            Sun = new SunLight(new(-0.55f, 1.3f, -0.35f), 3),
        },
        // Glow (Docs/Renderer-3D.md, "Glow"): a figure whose blade (cel-shaded) and badge (flat) glow
        // and whose pupil is flat without glow, in front of the shapes: the halo lies over them and the
        // background but behind the figure.
        new("glow-figure", 360, 300, "#b8b0a0", new Camera3D(Projection.Orthographic, new(0.3f, 1.4f, 5), new(0.3f, 0.85f, 0), OrthographicHeight: 2.0f), [])
        {
            Models = [new("shapes.glb", new(0.2f, 0.2f, -1.6f), Vector3.Zero), new("glowing.glb", new(0.1f, 0, 0.3f), new(0, -15, 0))],
            Palette = new() { ["cloth"] = "#7b5a3a", ["metal.base"] = "#8a8f96", ["metal.trim"] = "#c9a46a" },
            Sun = Lizardman,
        },
        // The same shapes in the battle view (orthographic, from the side) under a low sun from the left: other band positions.
        new("cel-side", 420, 200, "#b8b0a0", new Camera3D(Projection.Orthographic, new(0.1f, 0.5f, 6), new(0.1f, 0.5f, 0), OrthographicHeight: 1.9f), [])
        {
            Models = [new("shapes.glb", Vector3.Zero, new(0, -15, 0))],
            Palette = new() { ["cloth"] = "#3f4f8f", ["metal.base"] = "#5f6a78", ["metal.trim"] = "#b08a3c" },
            Sun = new SunLight(new(-1, 0.35f, 0.6f), 3),
        },
    ];
}
