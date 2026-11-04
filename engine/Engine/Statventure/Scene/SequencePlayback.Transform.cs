using System.Numerics;
using Statventure.Animation;

namespace Statventure.Scene;

public sealed partial record TransformTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context) => new TransformPlayback(context, context.Node(Node), Curves);
}

/// <summary>
/// A node's location, rotation and scale from curves at τ, every frame (S4-4): a channel with a curve
/// for any of its components is set whole, its components without one the node's values when the
/// sequence started (the rotation's as Euler angles XYZ); channels without curves are left to the
/// tracks before it.
/// </summary>
internal sealed class TransformPlayback(PlaybackContext context, NodeHandle handle, FCurve?[] curves) : TrackPlayback
{
    SceneNode _node = null!;
    Vector3 _location, _scale;
    (double X, double Y, double Z) _rotation;

    public override void Begin()
    {
        _node = handle.Node;
        _location = _node.Translation;
        _rotation = EulerXyz.FromQuaternion(_node.Rotation);
        _scale = _node.Scale;
    }

    public override void Apply(double tau, bool force)
    {
        var translation = _node.Translation;
        var rotation = _node.Rotation;
        var scale = _node.Scale;
        if (Animates(0))
            translation = new Vector3((float)Value(0, tau, _location.X), (float)Value(1, tau, _location.Y), (float)Value(2, tau, _location.Z));
        if (Animates(3))
            rotation = EulerXyz.ToQuaternion(Value(3, tau, _rotation.X), Value(4, tau, _rotation.Y), Value(5, tau, _rotation.Z));
        if (Animates(6))
            scale = new Vector3((float)Value(6, tau, _scale.X), (float)Value(7, tau, _scale.Y), (float)Value(8, tau, _scale.Z));
        _node.SetLocal(translation, rotation, scale);
        context.Wrote(_node, (Animates(0) ? ChannelSet.Translation : 0) | (Animates(3) ? ChannelSet.Rotation : 0) | (Animates(6) ? ChannelSet.Scale : 0));
    }

    bool Animates(int channel) => curves[channel] != null || curves[channel + 1] != null || curves[channel + 2] != null;

    double Value(int component, double tau, double start) => curves[component]?.Evaluate(tau) ?? start;
}

/// <summary>
/// Euler angles in Blender's XYZ order (Q8): rotate about X, then Y, then Z (the axes fixed), as
/// Blender's <c>eul_to_quat</c> and <c>quat_to_eul</c> (<c>blenlib/intern/math_rotation_c.cc</c>),
/// in double precision; angles in radians.
/// </summary>
internal static class EulerXyz
{
    /// <summary>The unit quaternion of the angles (eul_to_quat: q = q_z · q_y · q_x).</summary>
    public static Quaternion ToQuaternion(double x, double y, double z)
    {
        double ci = Math.Cos(x / 2), cj = Math.Cos(y / 2), ch = Math.Cos(z / 2);
        double si = Math.Sin(x / 2), sj = Math.Sin(y / 2), sh = Math.Sin(z / 2);
        double cc = ci * ch, cs = ci * sh, sc = si * ch, ss = si * sh;
        return new Quaternion((float)(cj * sc - sj * cs), (float)(cj * ss + sj * cc), (float)(cj * cs - sj * sc), (float)(cj * cc + sj * ss));
    }

    /// <summary>
    /// The angles of a rotation (quat_to_eul: the matrix, then mat3_normalized_to_eul, which of its two
    /// solutions has the smaller sum of absolute angles).
    /// </summary>
    public static (double X, double Y, double Z) FromQuaternion(Quaternion rotation)
    {
        var q = Quaternion.Normalize(rotation);
        // Blender's q is (w, x, y, z); m[i][j] is column i, row j.
        double q0 = Math.Sqrt(2) * q.W, q1 = Math.Sqrt(2) * q.X, q2 = Math.Sqrt(2) * q.Y, q3 = Math.Sqrt(2) * q.Z;
        double qda = q0 * q1, qdb = q0 * q2, qdc = q0 * q3, qaa = q1 * q1, qab = q1 * q2, qac = q1 * q3, qbb = q2 * q2, qbc = q2 * q3, qcc = q3 * q3;
        double m00 = 1 - qbb - qcc, m01 = qdc + qab, m02 = -qdb + qac;
        double m11 = 1 - qaa - qcc, m12 = qda + qbc;
        double m21 = -qda + qbc, m22 = 1 - qaa - qbb;
        var cy = Math.Sqrt(m00 * m00 + m01 * m01);
        if (!(cy > 0.0000375))
            return (Math.Atan2(-m21, m11), Math.Atan2(-m02, cy), 0);
        var a = (Math.Atan2(m12, m22), Math.Atan2(-m02, cy), Math.Atan2(m01, m00));
        var b = (Math.Atan2(-m12, -m22), Math.Atan2(-m02, -cy), Math.Atan2(-m01, -m00));
        return Math.Abs(a.Item1) + Math.Abs(a.Item2) + Math.Abs(a.Item3) > Math.Abs(b.Item1) + Math.Abs(b.Item2) + Math.Abs(b.Item3) ? b : a;
    }
}
