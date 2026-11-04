namespace Statventure.Scene;

public sealed partial record VisibilityTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context) => new VisibilityPlayback(context.Node(Node), Cues, context.Times(Cues));
}

/// <summary>
/// A node shown or hidden (with its subtree, Q23) by the cue in effect at τ, set when that cue changes
/// (S4-8); before the first cue as it was when the sequence started.
/// </summary>
internal sealed class VisibilityPlayback(NodeHandle handle, IReadOnlyList<VisibilityCue> cues, CueTimes times) : TrackPlayback
{
    SceneNode _node = null!;
    bool _hidden;
    int _current = -2;

    public override void Begin()
    {
        _node = handle.Node;
        _hidden = _node.Hidden;
    }

    public override void Apply(double tau, bool force)
    {
        var cue = times.At(tau);
        if (cue == _current && !force)
            return;
        _current = cue;
        _node.Hidden = cue < 0 ? _hidden : !cues[cue].Shown;
    }
}
