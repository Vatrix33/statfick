namespace Statventure.Animation;

/// <summary>A layer's state was entered (when its transition started) or left (when its clip had faded out): the time is on the animator's clock.</summary>
public readonly record struct StateNotice(string Layer, string State, bool Entered, double Time);

/// <summary>
/// A state machine playing on an animation stack (Docs/Animation.md, "State machines in detail"):
/// each of its layers is a <see cref="FadeLayer"/> on top of the stack, starting in its entry
/// state. The game sets parameters; <see cref="Update"/> takes the transitions due in an interval
/// of the clock, each a new fade entry: the state's clip as a strip (its speed, looping or held)
/// cross-fading in over the layer's current mix for the transition's duration.
/// </summary>
public sealed class StateMachine
{
    readonly Dictionary<string, (ParameterKind Kind, double Value)> _parameters = [];
    readonly Layer[] _layers;
    readonly Func<string, ClipBinding> _clips;
    readonly Dictionary<string, ClipBinding> _bindings = [];

    sealed class Layer(LayerDefinition definition, FadeLayer fades)
    {
        public LayerDefinition Definition { get; } = definition;
        public FadeLayer Fades { get; } = fades;
        public Dictionary<FadeEntry, StateDefinition> StateOf { get; } = [];
        public StateDefinition Current { get; set; } = null!;
        public FadeEntry CurrentEntry { get; set; } = null!;
    }

    /// <param name="clips">The clip a state names, bound to the stack's skeleton.</param>
    /// <param name="time">The clock's time the entry states start at.</param>
    public StateMachine(StateMachineDefinition definition, AnimationStack stack, Func<string, ClipBinding> clips, double time = 0)
    {
        definition.Validate();
        Definition = definition;
        _clips = clips;
        foreach (var p in definition.Parameters)
            _parameters[p.Name] = (p.Kind, p.Default);
        _layers = definition.Layers.Select(l =>
        {
            var layer = new Layer(l, new FadeLayer());
            stack.Layers.Add(layer.Fades);
            Enter(layer, l.States.Single(s => s.Name == l.Entry), time, double.NegativeInfinity, 0);
            return layer;
        }).ToArray();
    }

    public StateMachineDefinition Definition { get; }

    public void SetBool(string name, bool value) => Set(name, ParameterKind.Bool, value ? 1 : 0);
    public void SetInt(string name, int value) => Set(name, ParameterKind.Int, value);
    public void SetFloat(string name, double value) => Set(name, ParameterKind.Float, value);
    /// <summary>Sets a trigger: it stays set until a transition uses it or <see cref="ResetTrigger"/>.</summary>
    public void SetTrigger(string name) => Set(name, ParameterKind.Trigger, 1);
    public void ResetTrigger(string name) => Set(name, ParameterKind.Trigger, 0);

    /// <summary>A parameter's value (bools and triggers 0 or 1).</summary>
    public double Get(string name) => _parameters.TryGetValue(name, out var p) ? p.Value : throw new ArgumentException($"{Definition.Name} has no parameter {name}");

    void Set(string name, ParameterKind kind, double value)
    {
        if (!_parameters.TryGetValue(name, out var p))
            throw new ArgumentException($"{Definition.Name} has no parameter {name}");
        if (p.Kind != kind)
            throw new ArgumentException($"{Definition.Name}: {name} is a {p.Kind} parameter, not a {kind}");
        _parameters[name] = (kind, value);
    }

    /// <summary>The state a layer is in (the target of its latest transition).</summary>
    public string StateOf(string layer) => LayerNamed(layer).Current.Name;

    /// <summary>The state a fade entry of one of the machine's layers plays.</summary>
    public string StateNameOf(FadeEntry entry)
    {
        foreach (var layer in _layers)
            if (layer.StateOf.TryGetValue(entry, out var state))
                return state.Name;
        throw new ArgumentException("the entry isn't one of the machine's");
    }

    /// <summary>The fade layer a machine layer plays on.</summary>
    public FadeLayer FadesOf(string layer) => LayerNamed(layer).Fades;

    Layer LayerNamed(string name) => _layers.FirstOrDefault(l => l.Definition.Name == name) ?? throw new ArgumentException($"{Definition.Name} has no layer {name}");

    /// <summary>
    /// Takes the transitions due after <paramref name="from"/> up to <paramref name="to"/>, layer by
    /// layer (bottom first; a trigger a transition uses is reset at once), each layer's in time
    /// order: a transition with an exit time at the moment its state reaches it (exactly, within the
    /// interval); one without, or whose exit time had already passed when its conditions came to
    /// hold, at <paramref name="to"/> (the conditions are seen at updates). Among those due at once
    /// the first in priority order wins (any-state transitions first, each list in order). A state's
    /// speed parameter takes effect at <paramref name="from"/>. Entered notices go to <paramref name="notices"/>.
    /// </summary>
    public void Update(double from, double to, List<StateNotice> notices)
    {
        foreach (var layer in _layers)
        {
            Reanchor(layer, from);
            var cursor = from;
            for (var step = 0; step < 64; step++)
            {
                TransitionDefinition? best = null;
                var bestTime = double.PositiveInfinity;
                foreach (var transition in Candidates(layer))
                {
                    if (!transition.Conditions.All(Holds))
                        continue;
                    var moment = to;
                    if (transition.ExitTime is { } exitTime && layer.CurrentEntry.Strip is { } strip)
                    {
                        var exit = strip.Start + exitTime * Period(strip);
                        if (exit > from + AnimationStrip.Epsilon && exit >= cursor - AnimationStrip.Epsilon)
                            moment = Math.Max(exit, cursor);
                    }
                    // Never before the transition just taken (an exit can land a few 10⁻⁸ s after the update's time).
                    moment = Math.Max(moment, cursor);
                    if (moment > to + AnimationStrip.Epsilon || moment >= bestTime - AnimationStrip.Epsilon)
                        continue;
                    best = transition;
                    bestTime = moment;
                }
                if (best == null)
                    break;
                foreach (var c in best.Conditions)
                    if (_parameters[c.Parameter].Kind == ParameterKind.Trigger)
                        _parameters[c.Parameter] = (ParameterKind.Trigger, 0);
                var state = layer.Definition.States.Single(s => s.Name == best.To);
                Enter(layer, state, bestTime, bestTime, best.Duration);
                notices.Add(new StateNotice(layer.Definition.Name, state.Name, true, bestTime));
                cursor = bestTime;
            }
        }
    }

    /// <summary>
    /// Drops the entries a later, fully faded-in entry covers at <paramref name="time"/>, each with a
    /// left notice at the moment it was first covered (the earliest such entry's fade end).
    /// </summary>
    public void Prune(double time, List<StateNotice> notices)
    {
        foreach (var layer in _layers)
        {
            var entries = layer.Fades.Entries;
            var last = -1;
            for (var i = entries.Count - 1; i >= 0 && last < 0; i--)
                if (entries[i].WeightAt(time) >= 1)
                    last = i;
            for (var i = 0; i < last; i++)
            {
                var covered = double.PositiveInfinity;
                for (var j = i + 1; j <= last; j++)
                    if (entries[j].WeightAt(time) >= 1)
                        covered = Math.Min(covered, entries[j].FullAt);
                notices.Add(new StateNotice(layer.Definition.Name, layer.StateOf[entries[i]].Name, false, covered));
                layer.StateOf.Remove(entries[i]);
            }
            if (last > 0)
                entries.RemoveRange(0, last);
        }
        // In time order, keeping the order found at one time (stable).
        var sorted = notices.OrderBy(n => n.Time).ToList();
        notices.Clear();
        notices.AddRange(sorted);
    }

    /// <summary>Any-state transitions (not into the current state unless they may restart it), then the current state's.</summary>
    IEnumerable<TransitionDefinition> Candidates(Layer layer)
    {
        foreach (var t in layer.Definition.Transitions)
            if (t.From == null && (t.To != layer.Current.Name || t.MayRestart))
                yield return t;
        foreach (var t in layer.Definition.Transitions)
            if (t.From == layer.Current.Name)
                yield return t;
    }

    bool Holds(ConditionDefinition c)
    {
        var value = _parameters[c.Parameter].Value;
        return c.Compare switch
        {
            Compare.Equal => value == c.Value,
            Compare.NotEqual => value != c.Value,
            Compare.Less => value < c.Value,
            Compare.Greater => value > c.Value,
            Compare.LessOrEqual => value <= c.Value,
            _ => value >= c.Value,
        };
    }

    /// <summary>One pass through a strip's clip part, in seconds of the clock.</summary>
    static double Period(AnimationStrip strip) => (strip.ClipEnd - strip.ClipStart is var part && part != 0 ? part : 1.0 / 60) * Math.Abs(strip.Scale);

    double SpeedOf(StateDefinition state) => Math.Max(state.SpeedParameter is { } p ? _parameters[p].Value : state.Speed, 1e-9);

    void Enter(Layer layer, StateDefinition state, double start, double fadeStart, double duration)
    {
        AnimationStrip? strip = null;
        if (state.Clip is { } clip)
        {
            if (!_bindings.TryGetValue(clip, out var binding))
                _bindings[clip] = binding = _clips(clip);
            strip = new AnimationStrip(binding)
            {
                Start = start,
                Scale = 1 / SpeedOf(state),
                Repeat = state.Loop ? double.PositiveInfinity : 1,
                Mode = layer.Definition.Mode,
                Extrapolation = Extrapolation.Hold,
            };
        }
        var entry = new FadeEntry(strip, fadeStart, duration);
        layer.Fades.Entries.Add(entry);
        layer.StateOf[entry] = state;
        layer.Current = state;
        layer.CurrentEntry = entry;
    }

    /// <summary>A changed speed parameter re-times the current state's strip from <paramref name="time"/> on, keeping its place in the clip.</summary>
    void Reanchor(Layer layer, double time)
    {
        if (layer.CurrentEntry.Strip is not { } strip || layer.Current.SpeedParameter == null)
            return;
        var scale = 1 / SpeedOf(layer.Current);
        if (scale == strip.Scale)
            return;
        var passes = (time - strip.Start) / Period(strip);
        strip.Scale = scale;
        strip.Start = time - passes * Period(strip);
    }
}
