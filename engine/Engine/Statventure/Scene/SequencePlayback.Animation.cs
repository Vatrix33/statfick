using Statventure.Animation;

namespace Statventure.Scene;

public sealed partial record ActorTrack
{
    /// <summary>
    /// Checks the figure's side (S4-6): its clips (bound to its stack's skeleton), its strips in time
    /// order without overlapping (one NLA layer), and for parameter cues a state machine with each
    /// parameter of the cue's kind.
    /// </summary>
    internal override TrackPlayback CreatePlayback(PlaybackContext context)
    {
        var actor = context.Actor(Actor);
        NodeHandle? mark = Mark == null ? null : context.Node(Mark);
        var clips = Strips.Select(s => context.ActorClip(actor, Actor, s.Clip)).ToArray();
        var placed = Strips.Select((s, i) => s.ToStrip(clips[i], 0)).ToArray();
        for (var i = 1; i < placed.Length; i++)
            if (placed[i].Start < placed[i - 1].End - AnimationStrip.Epsilon)
                throw context.Problem($"strip {i} ({Strips[i].Clip}, from {placed[i].Start:R} s) starts before strip {i - 1} ({Strips[i - 1].Clip}) ends at {placed[i - 1].End:R} s: the strips of one track are one layer, in time order");
        if (Cues.Count > 0)
        {
            var machine = actor.Animator.Machine ?? throw context.Problem($"the actor {Actor} has no state machine for its parameter cues");
            foreach (var cue in Cues)
            {
                var parameter = machine.Definition.Parameters.FirstOrDefault(p => p.Name == cue.Parameter)
                    ?? throw context.Problem($"the actor {Actor}'s state machine {machine.Definition.Name} has no parameter {cue.Parameter}");
                if (parameter.Kind != cue.Kind)
                    throw context.Problem($"the actor {Actor}'s parameter {cue.Parameter} is a {parameter.Kind.ToString().ToLowerInvariant()} parameter, not a {cue.Kind.ToString().ToLowerInvariant()}");
            }
        }
        return new ActorPlayback(context, actor, mark, Strips, clips, Cues);
    }
}

/// <summary>
/// A bound figure (Q16): on its mark node (attached there when the sequence starts, and left there,
/// Q21), its strips on one NLA layer on top of its stack (placed on its clock: its time when the
/// sequence started plus their start), its bool, int and float parameters set by the cue in effect at
/// τ when that cue changes (before the first, as they were when the sequence started; S4-8), its
/// triggers set when the clock passes their cues (and those not yet set at a skip or a cut).
/// </summary>
internal sealed class ActorPlayback(PlaybackContext context, Actor actor, NodeHandle? mark, IReadOnlyList<SequenceStrip> strips,
    ClipBinding[] clips, IReadOnlyList<ParameterCue> cues) : TrackPlayback
{
    sealed class Parameter(string name, ParameterKind kind, int[] cues, CueTimes times)
    {
        public string Name { get; } = name;
        public ParameterKind Kind { get; } = kind;
        /// <summary>The track's cue indices for this parameter, in record order (the times' indices point into it).</summary>
        public int[] Cues { get; } = cues;
        public CueTimes Times { get; } = times;
        public double Start { get; set; }
        public int Current { get; set; } = -2;
    }

    readonly List<Parameter> _parameters = [];
    readonly List<(double Time, int Cue)> _triggers = [];
    StateMachine? _machine;

    public override void Begin()
    {
        if (mark is { } node)
            node.Node.Attach(actor);
        var layer = new AnimationLayer();
        var origin = context.OriginOf(actor);
        for (var i = 0; i < strips.Count; i++)
            layer.Strips.Add(strips[i].ToStrip(clips[i], origin));
        actor.Animator.Stack.Layers.Add(layer);
        context.Player.AddLayer(actor, layer);
        _machine = actor.Animator.Machine;
        foreach (var group in Enumerable.Range(0, cues.Count).Where(i => cues[i].Kind != ParameterKind.Trigger).GroupBy(i => cues[i].Parameter))
        {
            var indices = group.ToArray();
            _parameters.Add(new Parameter(group.Key, cues[indices[0]].Kind, indices, new CueTimes(indices.Select(i => context.Time(cues[i])).ToArray()))
            {
                Start = _machine!.Get(group.Key),
            });
        }
        for (var i = 0; i < cues.Count; i++)
            if (cues[i].Kind == ParameterKind.Trigger)
                _triggers.Add((context.Time(cues[i]), i));
    }

    public override void Apply(double tau, bool force)
    {
        foreach (var parameter in _parameters)
        {
            var at = parameter.Times.At(tau);
            if (at == parameter.Current && !force)
                continue;
            parameter.Current = at;
            var value = at < 0 ? parameter.Start : cues[parameter.Cues[at]].Value;
            switch (parameter.Kind)
            {
                case ParameterKind.Bool: _machine!.SetBool(parameter.Name, value != 0); break;
                case ParameterKind.Int: _machine!.SetInt(parameter.Name, (int)value); break;
                default: _machine!.SetFloat(parameter.Name, value); break;
            }
        }
    }

    public override IReadOnlyList<(double Time, int Cue)> Moments => _triggers;

    public override void Pass(int cue, bool skipped) => _machine!.SetTrigger(cues[cue].Parameter);
}
