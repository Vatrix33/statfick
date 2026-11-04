using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;

namespace Statventure.Scene;

public sealed partial record ClipTrack
{
    /// <summary>
    /// A stage's clip tracks play as one NLA stack on its nodes (a layer each, in track order), applied
    /// at the place of the stage's first clip track in track order; the stage's other clip tracks play
    /// nothing of their own.
    /// </summary>
    internal override TrackPlayback CreatePlayback(PlaybackContext context)
    {
        var tracks = context.Sequence.Tracks;
        var first = Enumerable.Range(0, tracks.Count).First(i => tracks[i] is ClipTrack c && c.Stage == Stage);
        if (first != context.Track)
            return NoPlayback.Instance;
        return new ClipPlayback(context, context.Stages[Stage], tracks.OfType<ClipTrack>().Where(c => c.Stage == Stage).Select(c => c.Strip).ToArray());
    }
}

/// <summary>
/// A stage's clips on its nodes (Q13): evaluated as an NLA stack at τ every frame. Each node takes the
/// channels (location, rotation, scale) a strip sets now, so a later track in track order can set
/// other channels or override these (S4-4); a channel set before that no strip sets now goes back to
/// the node's rest (its local when the stage was instanced), as unset channels are rest, unless a track
/// before it set the channel in this frame; channels no strip has set are left alone.
/// </summary>
internal sealed class ClipPlayback(PlaybackContext context, StageBinding stage, SequenceStrip[] strips) : TrackPlayback
{
    AnimationStack _stack = null!;
    PoseBuffer _buffer = null!;
    readonly ModelPose _pose = new();
    ChannelSet[] _set = [];
    (Vector3 Translation, Quaternion Rotation, Vector3 Scale)[] _rest = [];
    ModelInstance _instance = null!;

    public override void Begin()
    {
        _instance = stage.Instance!;
        _stack = new AnimationStack(_instance.Skeleton);
        foreach (var strip in strips)
        {
            var layer = new AnimationLayer();
            layer.Strips.Add(strip.ToStrip(_instance.Clip(strip.Clip), 0));
            _stack.Layers.Add(layer);
        }
        _buffer = new PoseBuffer(_instance.Skeleton);
        var count = _instance.Data.Nodes.Count;
        _set = new ChannelSet[count];
        _rest = new (Vector3, Quaternion, Vector3)[count];
        for (var i = 0; i < count; i++)
            if (_instance.NodeOf(i) is { } node)
                _rest[i] = (node.Translation, node.Rotation, node.Scale);
    }

    public override void Apply(double tau, bool force)
    {
        _stack.Evaluate(tau, _buffer);
        _buffer.ToPose(_pose);
        for (var i = 0; i < _set.Length; i++)
        {
            if (_instance.NodeOf(i) is not { } node)
                continue;
            var set = _buffer.Set[i];
            var was = _set[i];
            if (set == ChannelSet.None && was == ChannelSet.None)
                continue;
            var local = (node.Translation, node.Rotation, node.Scale);
            if (set != ChannelSet.None)
                _pose.TryGet(i, out local);
            var rest = _rest[i];
            node.SetLocal(
                Channel(node, set, was, ChannelSet.Translation, local.Translation, rest.Translation, node.Translation),
                Channel(node, set, was, ChannelSet.Rotation, local.Rotation, rest.Rotation, node.Rotation),
                Channel(node, set, was, ChannelSet.Scale, local.Scale, rest.Scale, node.Scale));
            context.Wrote(node, set);
            _set[i] = set;
        }
    }

    /// <summary>The clip's value of a channel it sets now; its rest if it set it before (unless a track before it set it in this frame); otherwise what the node has.</summary>
    T Channel<T>(SceneNode node, ChannelSet set, ChannelSet was, ChannelSet channel, T clip, T rest, T current) =>
        (set & channel) != 0 ? clip : (was & channel) != 0 && !context.Written(node, channel) ? rest : current;
}
