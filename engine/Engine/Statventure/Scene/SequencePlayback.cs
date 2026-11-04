using Statventure.Animation;
using Statventure.Render3D;

namespace Statventure.Scene;

/// <summary>
/// A track's playback (Docs/Scene.md, "The player": one class per track kind behind this interface,
/// so steps 5 and 6 add playback without editing the player). Made by its track with every name
/// resolved and checked against the bindings (throwing an <see cref="ArgumentException"/> and
/// changing nothing, S4-6); then, once every track's checks passed, <see cref="Begin"/> makes its
/// changes (figures on marks, layers on stacks, the state it starts from). Each frame, in the frame's
/// animation timing update and in track order, <see cref="Apply"/> gives the state at the timeline time
/// τ; the moments it lists are passed to it when the clock passes them (or at a skip or a cut, marked
/// skipped), in time order across tracks.
/// </summary>
internal abstract class TrackPlayback
{
    /// <summary>Makes the playback's changes (after every track's checks passed).</summary>
    public virtual void Begin()
    {
    }

    /// <summary>
    /// The state at <paramref name="tau"/>: curves and clips every frame; a track of state cues when
    /// the cue in effect changes or <paramref name="force"/> (the first frame, after a seek, at the
    /// end of a skip) (S4-8).
    /// </summary>
    public virtual void Apply(double tau, bool force)
    {
    }

    /// <summary>The moments it has: each one's time on the timeline and the cue's index in its list (record order).</summary>
    public virtual IReadOnlyList<(double Time, int Cue)> Moments => [];

    /// <summary>The clock passed moment <paramref name="cue"/> (skipped: at a skip, or cut out of the reduced variant, Q18, Q19).</summary>
    public virtual void Pass(int cue, bool skipped)
    {
    }

    /// <summary>The game paused (true) or resumed the sequence.</summary>
    public virtual void Paused(bool paused)
    {
    }

    /// <summary>The sequence's rate changed.</summary>
    public virtual void RateChanged(double rate)
    {
    }

    /// <summary>The clock jumped to <paramref name="tau"/> (a seek: nothing fires, S4-5).</summary>
    public virtual void Jumped(double tau)
    {
    }

    /// <summary>The document went hidden (true) or was shown again.</summary>
    public virtual void VisibilityChanged(bool hidden)
    {
    }

    /// <summary>The sequence ended (at its end, or by a skip or a finish): its end state stays (Q21).</summary>
    public virtual void Ended(bool skipped)
    {
    }
}

/// <summary>A playback that does nothing (a track another playback plays, or a kind whose playback a later step adds).</summary>
internal sealed class NoPlayback : TrackPlayback
{
    public static readonly NoPlayback Instance = new();
}

/// <summary>A stage the game gave for a sequence's stage path: its model's data, the GPU model when it is drawn, and its instance once it plays.</summary>
internal sealed class StageBinding(string path, ModelData data, Model? model)
{
    public string Path { get; } = path;
    public string Name { get; } = Sequence.StageName(path);
    public ModelData Data { get; } = data;
    public Model? Model { get; } = model;
    public ModelInstance? Instance { get; set; }
}

/// <summary>A node a track names, resolved: a node role's node, or a stage's node by its index in the stage model (a scene node once the stage is instanced).</summary>
internal readonly record struct NodeHandle(SceneNode? Bound, StageBinding? Stage, int Index)
{
    public SceneNode Node => Bound ?? Stage!.Instance!.NodeOf(Index)!;
}

/// <summary>
/// What a track's playback is made with: the sequence, its bound roles and stages, the cue times
/// (markers resolved, S4-2) and the player; names are resolved here, each failure an
/// <see cref="ArgumentException"/> naming the track (S4-6).
/// </summary>
internal sealed class PlaybackContext(SequencePlayer player, Sequence sequence, SceneView scene, SequenceTimes times,
    IReadOnlyDictionary<string, object> roles, IReadOnlyDictionary<string, StageBinding> stages,
    IReadOnlyDictionary<Actor, Func<string, ClipBinding?>> clips)
{
    public SequencePlayer Player { get; } = player;
    public Sequence Sequence { get; } = sequence;
    public SceneView Scene { get; } = scene;
    public IReadOnlyDictionary<string, StageBinding> Stages { get; } = stages;
    /// <summary>The index of the track being made.</summary>
    public int Track { get; set; }

    public ArgumentException Problem(string message) =>
        new($"the sequence {Sequence.Name}, track {Track} ({Sequence.Tracks[Track].Kind}): {message}");

    /// <summary>The object bound to a role.</summary>
    public object Role(string name) => roles[name];

    public Actor Actor(string role) => (Actor)roles[role];

    /// <summary>A node a track names (a node role, or <c>&lt;stage&gt;/&lt;Blender name&gt;</c>).</summary>
    public NodeHandle Node(string reference)
    {
        var slash = reference.IndexOf('/');
        if (slash < 0)
            return new NodeHandle((SceneNode)roles[reference], null, -1);
        var stage = Stages[reference[..slash]];
        var index = SequenceChecks.StageNodeIndex(stage.Data, reference[(slash + 1)..]);
        if (index < 0)
            throw Problem($"the stage {stage.Path} has no node {reference[(slash + 1)..]}");
        return new NodeHandle(null, stage, index);
    }

    /// <summary>A clip of an actor's, bound to its stack's skeleton (from the actor's binding, by default its model's own clips).</summary>
    public ClipBinding ActorClip(Actor actor, string role, string clip)
    {
        var binding = clips[actor](clip) ?? throw Problem($"the actor {role} has no clip {clip}");
        if (binding.Skeleton != actor.Animator.Stack.Skeleton)
            throw Problem($"the actor {role}'s clip {clip} is bound to another skeleton than its stack's");
        return binding;
    }

    /// <summary>A cue's time on the timeline (a marker's time plus its offset, S4-2).</summary>
    public double Time(ISequenceCue cue) => times.Resolve(cue.Time, cue.Marker);

    /// <summary>The cues' times on the timeline in time order (equal times in record order).</summary>
    public CueTimes Times<T>(IReadOnlyList<T> cues) where T : ISequenceCue => new(cues.Select(c => Time(c)).ToArray());

    /// <summary>Where an actor's clock was when the sequence started: its time is that plus τ while the sequence holds it.</summary>
    public double OriginOf(Actor actor) => Player.OriginOf(actor);

    readonly HashSet<(SceneNode, ChannelSet)> _written = [];

    /// <summary>A frame's state is about to be applied: no channel is written yet.</summary>
    public void BeginFrame() => _written.Clear();

    /// <summary>A track set these channels of a node in this frame (so a later clip that stops setting them leaves them, S4-4).</summary>
    public void Wrote(SceneNode node, ChannelSet channels)
    {
        foreach (var channel in (ChannelSet[])[ChannelSet.Translation, ChannelSet.Rotation, ChannelSet.Scale])
            if ((channels & channel) != 0)
                _written.Add((node, channel));
    }

    /// <summary>Whether a track before this one set the node's channel in this frame.</summary>
    public bool Written(SceneNode node, ChannelSet channel) => _written.Contains((node, channel));
}

/// <summary>
/// A list of cues' times on the timeline, in time order; cues within <see cref="AnimationStrip.Epsilon"/>
/// of a run's first are one moment, in record order (as the moments are, Q17), so the later in record
/// order is the one in effect.
/// </summary>
internal sealed class CueTimes
{
    readonly double[] _times;
    readonly (double First, int[] Cues)[] _runs;

    public CueTimes(double[] times)
    {
        _times = times;
        var order = Enumerable.Range(0, times.Length).OrderBy(i => times[i]).ToArray();
        var runs = new List<(double, int[])>();
        for (var run = 0; run < order.Length;)
        {
            var end = run + 1;
            while (end < order.Length && times[order[end]] - times[order[run]] <= AnimationStrip.Epsilon)
                end++;
            runs.Add((times[order[run]], order[run..end].Order().ToArray()));
            run = end;
        }
        _runs = [.. runs];
    }

    public int Count => _times.Length;

    /// <summary>The cue (its record index) in effect at <paramref name="tau"/>: the last of the last moment at or before it (within the epsilon), or −1.</summary>
    public int At(double tau)
    {
        var found = -1;
        foreach (var (first, cues) in _runs)
        {
            if (first > tau + AnimationStrip.Epsilon)
                break;
            found = cues[^1];
        }
        return found;
    }

    /// <summary>Every cue as a moment: its time and record index.</summary>
    public IReadOnlyList<(double Time, int Cue)> Moments => _times.Select((time, cue) => (time, cue)).ToArray();

    /// <summary>The time of the cue with record index <paramref name="cue"/>.</summary>
    public double TimeOf(int cue) => _times[cue];
}
