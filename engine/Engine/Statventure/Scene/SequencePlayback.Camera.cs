using Statventure.Render3D;

namespace Statventure.Scene;

public sealed partial record CameraTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context)
    {
        var cameras = new Func<SceneCamera>[Cuts.Count];
        for (var i = 0; i < Cuts.Count; i++)
            cameras[i] = CameraOn(context, context.Node(Cuts[i].Camera), $"cut {i}'s camera {Cuts[i].Camera}");
        return new CameraPlayback(context.Scene, cameras, context.Times(Cuts), FieldOfView);
    }

    /// <summary>The camera on a node a cut names: a bound node's first <see cref="SceneCamera"/>, or the one instancing hangs on a stage node that carries a camera (S3-5).</summary>
    static Func<SceneCamera> CameraOn(PlaybackContext context, NodeHandle handle, string what)
    {
        if (handle.Bound is { } node)
        {
            var camera = node.Attachments.OfType<SceneCamera>().FirstOrDefault() ?? throw context.Problem($"{what}: the node carries no camera");
            return () => camera;
        }
        var stage = handle.Stage!;
        if (stage.Data.Nodes[handle.Index].Camera < 0)
            throw context.Problem($"{what}: the stage {stage.Path}'s node {stage.Data.Nodes[handle.Index].Name} carries no camera");
        return () => handle.Node.Attachments.OfType<SceneCamera>().First();
    }
}

/// <summary>
/// The camera track's playback (Q11, S5-5): the scene's camera is the camera on the node the cut in
/// effect at τ names, set when that cut changes (S4-8; before the first cut the scene's camera as it
/// was when the sequence started); from the first cut on, the field-of-view curve (radians) sets the
/// field of view of the camera cut to every frame (an orthographic camera has none: the curve leaves
/// it), and a camera cut away from gets its own back, as it was when the sequence started. At the end
/// the last camera keeps the curve's value there (Q21).
/// </summary>
internal sealed class CameraPlayback(SceneView scene, Func<SceneCamera>[] cameras, CueTimes times, FCurve? fieldOfView) : TrackPlayback
{
    readonly Dictionary<SceneCamera, float> _own = [];
    SceneCamera[] _cameras = [];
    SceneCamera? _before, _active;
    int _current = -2;

    public override void Begin()
    {
        _cameras = [.. cameras.Select(c => c())];
        _before = scene.Camera;
        foreach (var camera in _cameras)
            _own.TryAdd(camera, camera.VerticalFieldOfView);
    }

    public override void Apply(double tau, bool force)
    {
        var cut = times.At(tau);
        if (cut != _current || force)
        {
            _current = cut;
            var next = cut < 0 ? null : _cameras[cut];
            if (_active != null && _active != next && fieldOfView != null)
                _active.VerticalFieldOfView = _own[_active];
            _active = next;
            scene.Camera = next ?? _before;
        }
        if (fieldOfView != null && _active is { Projection: Projection.Perspective } camera)
            camera.VerticalFieldOfView = (float)fieldOfView.Evaluate(tau);
    }
}
