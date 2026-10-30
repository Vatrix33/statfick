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

/// <summary>A test scene rendered by Blender (Tools/BlenderReference) and by the engine.</summary>
public sealed record Scene3D(string Name, int Width, int Height, string Background, Camera3D Camera, Object3D[] Objects);

public static class Scenes3D
{
    static Object3D Box(Vector3 at, Vector3 rotation, Vector3 scale, string color) => new(Shape3D.Cube, at, rotation, scale, color);

    static readonly Object3D[] Boxes =
    [
        Box(new(0, 0, 0), new(20, 35, 0), Vector3.One, "#8a6a4a"),
        Box(new(1.3f, -0.2f, -0.6f), new(0, -20, 10), new(0.6f, 1.6f, 0.6f), "#4a6a8a"),
        Box(new(-0.9f, -0.75f, 0.6f), new(0, 15, 0), new(1.8f, 0.4f, 0.9f), "#6a8a4a"),
    ];

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
    ];
}
