using System.Numerics;

namespace Statventure.Render3D;

/// <summary>How a camera projects: orthographic (parallel, a fixed height of the world in view) or perspective (a vertical field of view).</summary>
public enum Projection
{
    Orthographic,
    Perspective,
}

/// <summary>
/// A camera in glTF's coordinates (right-handed, Y up, metres), looking from
/// <see cref="Position"/> at <see cref="Target"/>. Its matrices are for Vulkan: clip space Y points
/// down and depth is reversed (1 at the near plane, 0 at the far one) for precision. Both
/// projections keep the vertical extent fixed and widen with the aspect ratio, as a Blender camera
/// with its sensor fit set to vertical does.
/// </summary>
public sealed class Camera
{
    public Projection Projection { get; set; } = Projection.Perspective;
    public Vector3 Position { get; set; } = new(0, 0, 5);
    public Vector3 Target { get; set; }
    public Vector3 Up { get; set; } = Vector3.UnitY;
    /// <summary>Perspective: the vertical field of view in radians.</summary>
    public float VerticalFieldOfView { get; set; } = MathF.PI / 4;
    /// <summary>Orthographic: the height of the world in view (Blender's orthographic scale with a vertical sensor fit).</summary>
    public float OrthographicHeight { get; set; } = 10;
    public float Near { get; set; } = 0.1f;
    public float Far { get; set; } = 100;

    /// <summary>World to view space (the camera looks down its −Z axis with Y up). Row vectors, as System.Numerics.</summary>
    public Matrix4x4 View => Matrix4x4.CreateLookAt(Position, Target, Up);

    /// <summary>View to Vulkan clip space for a viewport of this aspect ratio (width over height).</summary>
    public Matrix4x4 ProjectionMatrix(float aspect)
    {
        float near = Near, far = Far;
        if (Projection == Projection.Orthographic)
        {
            var height = OrthographicHeight;
            var width = height * aspect;
            // Reversed depth: (z + far) / (far − near) is 1 at −near and 0 at −far.
            return new Matrix4x4(
                2 / width, 0, 0, 0,
                0, -2 / height, 0, 0,
                0, 0, 1 / (far - near), 0,
                0, 0, far / (far - near), 1);
        }
        var f = 1 / MathF.Tan(VerticalFieldOfView / 2);
        // Reversed depth: (near·z + near·far) / ((far − near)·−z) is 1 at −near and 0 at −far; w = −z.
        return new Matrix4x4(
            f / aspect, 0, 0, 0,
            0, -f, 0, 0,
            0, 0, near / (far - near), -1,
            0, 0, near * far / (far - near), 0);
    }

    /// <summary>World to clip space.</summary>
    public Matrix4x4 ViewProjection(float aspect) => View * ProjectionMatrix(aspect);
}
