namespace Statventure.Scene;

public sealed partial record LightTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context)
    {
        var suns = new Func<SceneSun>[Cues.Count];
        for (var i = 0; i < Cues.Count; i++)
            suns[i] = SunOn(context, context.Node(Cues[i].Sun), $"cue {i}'s sun {Cues[i].Sun}");
        return new LightPlayback(context.Scene, suns, context.Times(Cues), Strength, Angle);
    }

    /// <summary>The sun on a node a cue names: a bound node's <see cref="SceneSun"/>, or the one instancing hangs on the stage node that carries the sun (S3-5).</summary>
    static Func<SceneSun> SunOn(PlaybackContext context, NodeHandle handle, string what)
    {
        if (handle.Bound is { } node)
        {
            var sun = node.Attachments.OfType<SceneSun>().FirstOrDefault() ?? throw context.Problem($"{what}: the node carries no sun");
            return () => sun;
        }
        var stage = handle.Stage!;
        if (stage.Data.Nodes[handle.Index].Light < 0)
            throw context.Problem($"{what}: the stage {stage.Path}'s node {stage.Data.Nodes[handle.Index].Name} carries no sun");
        return () => handle.Node.Attachments.OfType<SceneSun>().First();
    }
}

/// <summary>
/// The light track's playback (Q12, S5-5): the scene's sun is the sun on the node the cue in effect at
/// τ names, set when that cue changes (S4-8; before the first cue the scene's sun as it was when the
/// sequence started); from the first cue on, the strength curve (Blender's raw units) and the angle
/// curve (degrees) set the strength and angle of the sun cued every frame, and a sun cued away from
/// gets its own back, as they were when the sequence started. At the end the last sun keeps the
/// curves' values there (Q21).
/// </summary>
internal sealed class LightPlayback(SceneView scene, Func<SceneSun>[] suns, CueTimes times, FCurve? strength, FCurve? angle) : TrackPlayback
{
    readonly Dictionary<SceneSun, (float Strength, float Angle)> _own = [];
    SceneSun[] _suns = [];
    SceneSun? _before, _active;
    int _current = -2;

    public override void Begin()
    {
        _suns = [.. suns.Select(s => s())];
        _before = scene.Sun;
        foreach (var sun in _suns)
            _own.TryAdd(sun, (sun.Strength, sun.Angle));
    }

    public override void Apply(double tau, bool force)
    {
        var cue = times.At(tau);
        if (cue != _current || force)
        {
            _current = cue;
            var next = cue < 0 ? null : _suns[cue];
            if (_active != null && _active != next)
            {
                var (ownStrength, ownAngle) = _own[_active];
                if (strength != null)
                    _active.Strength = ownStrength;
                if (angle != null)
                    _active.Angle = ownAngle;
            }
            _active = next;
            scene.Sun = next ?? _before;
        }
        if (_active is not { } sun)
            return;
        if (strength != null)
            sun.Strength = (float)strength.Evaluate(tau);
        if (angle != null)
            sun.Angle = (float)angle.Evaluate(tau);
    }
}
