namespace Statventure.Animation;

// A state machine as data (Docs/Animation.md, "State machines in detail"): plain records the C#
// builder makes; M9 gives them a pack form and M11 an editor. Everything is found by name.

/// <summary>The kinds of parameters a machine's conditions test.</summary>
public enum ParameterKind
{
    Bool,
    Int,
    Float,
    /// <summary>A one-shot flag: set by the game, used up by the transition that uses it (kept until then).</summary>
    Trigger,
}

/// <summary>How a condition compares a parameter with its value.</summary>
public enum Compare { Equal, NotEqual, Less, Greater, LessOrEqual, GreaterOrEqual }

/// <summary>A parameter and its starting value (bools and triggers: 0 or 1).</summary>
public sealed record ParameterDefinition(string Name, ParameterKind Kind, double Default = 0);

/// <summary>A test of one parameter; for a trigger, whether it's set.</summary>
public sealed record ConditionDefinition(string Parameter, Compare Compare = Compare.Equal, double Value = 1)
{
    public static ConditionDefinition Trigger(string name) => new(name);
    public static ConditionDefinition Is(string name) => new(name, Compare.Equal, 1);
    public static ConditionDefinition IsNot(string name) => new(name, Compare.Equal, 0);
    public static ConditionDefinition Of(string name, Compare compare, double value) => new(name, compare, value);
}

/// <summary>
/// A state: its clip (null: an empty state, which plays nothing), its speed (a number, or a float
/// parameter's value when <see cref="SpeedParameter"/> names one; times the animator's rate) and
/// whether the clip loops (else it holds its last frame).
/// </summary>
public sealed record StateDefinition(string Name, string? Clip, double Speed = 1, string? SpeedParameter = null, bool Loop = false);

/// <summary>
/// A transition from a state (null: from any state, checked before the current state's own) to
/// another: all <see cref="Conditions"/> must hold, and with an <see cref="ExitTime"/> only once the
/// state has played that many times through its clip (1: its end; 0.5: half way); it cross-fades
/// over <see cref="Duration"/> seconds. It enters the state its layer is already in only if
/// <see cref="MayRestart"/>.
/// </summary>
public sealed record TransitionDefinition(string? From, string To, IReadOnlyList<ConditionDefinition> Conditions, double? ExitTime = null,
    double Duration = 0, bool MayRestart = false);

/// <summary>A layer of states: its blend mode over the layers below, its entry state, states and transitions (in priority order).</summary>
public sealed record LayerDefinition(string Name, BlendMode Mode, string Entry, IReadOnlyList<StateDefinition> States,
    IReadOnlyList<TransitionDefinition> Transitions);

/// <summary>A state machine: its parameters and layers (bottom first).</summary>
public sealed record StateMachineDefinition(string Name, IReadOnlyList<ParameterDefinition> Parameters, IReadOnlyList<LayerDefinition> Layers)
{
    /// <summary>Throws when a name doesn't resolve or a condition doesn't fit its parameter.</summary>
    public void Validate()
    {
        var parameters = new Dictionary<string, ParameterKind>();
        foreach (var p in Parameters)
            if (!parameters.TryAdd(p.Name, p.Kind))
                throw new ArgumentException($"{Name}: the parameter {p.Name} is defined twice");
        if (Layers.Count == 0)
            throw new ArgumentException($"{Name}: no layers");
        foreach (var layer in Layers)
        {
            var states = new HashSet<string>();
            foreach (var state in layer.States)
            {
                if (!states.Add(state.Name))
                    throw new ArgumentException($"{Name}/{layer.Name}: the state {state.Name} is defined twice");
                if (state.SpeedParameter is { } speed && (!parameters.TryGetValue(speed, out var kind) || kind != ParameterKind.Float))
                    throw new ArgumentException($"{Name}/{layer.Name}/{state.Name}: the speed parameter {speed} isn't a float parameter");
            }
            if (!states.Contains(layer.Entry))
                throw new ArgumentException($"{Name}/{layer.Name}: no entry state {layer.Entry}");
            foreach (var t in layer.Transitions)
            {
                if (t.From != null && !states.Contains(t.From) || !states.Contains(t.To))
                    throw new ArgumentException($"{Name}/{layer.Name}: a transition {t.From ?? "(any)"} → {t.To} names a state that doesn't exist");
                if (t.Duration < 0 || t.ExitTime < 0)
                    throw new ArgumentException($"{Name}/{layer.Name}: a transition to {t.To} has a negative duration or exit time");
                foreach (var c in t.Conditions)
                {
                    if (!parameters.TryGetValue(c.Parameter, out var kind))
                        throw new ArgumentException($"{Name}/{layer.Name}: a transition to {t.To} tests {c.Parameter}, which isn't a parameter");
                    if (kind == ParameterKind.Trigger && (c.Compare != Compare.Equal || c.Value != 1))
                        throw new ArgumentException($"{Name}/{layer.Name}: a trigger condition ({c.Parameter}) can only test that it's set");
                    if (kind == ParameterKind.Bool && c.Compare is not (Compare.Equal or Compare.NotEqual))
                        throw new ArgumentException($"{Name}/{layer.Name}: a bool condition ({c.Parameter}) can only test equality");
                }
                if (t.Conditions.Count == 0 && t.ExitTime == null)
                    throw new ArgumentException($"{Name}/{layer.Name}: a transition to {t.To} has neither conditions nor an exit time");
                // An exit time counts passes through the state's clip: an empty state has none, and
                // a transition from any state doesn't know whose.
                if (t.ExitTime != null && (t.From == null || layer.States.Single(st => st.Name == t.From).Clip == null))
                    throw new ArgumentException($"{Name}/{layer.Name}: a transition to {t.To} has an exit time but no clip to count it in");
            }
        }
    }
}

/// <summary>Builds a <see cref="StateMachineDefinition"/> in C# (the M7 form of a rig's graph).</summary>
public sealed class StateMachineBuilder(string name)
{
    readonly List<ParameterDefinition> _parameters = [];
    readonly List<LayerDefinition> _layers = [];

    public StateMachineBuilder Bool(string parameter, bool value = false) => Add(new(parameter, ParameterKind.Bool, value ? 1 : 0));
    public StateMachineBuilder Int(string parameter, int value = 0) => Add(new(parameter, ParameterKind.Int, value));
    public StateMachineBuilder Float(string parameter, double value = 0) => Add(new(parameter, ParameterKind.Float, value));
    public StateMachineBuilder Trigger(string parameter) => Add(new(parameter, ParameterKind.Trigger));

    StateMachineBuilder Add(ParameterDefinition parameter)
    {
        _parameters.Add(parameter);
        return this;
    }

    /// <summary>A layer of states, built by <paramref name="build"/>.</summary>
    public StateMachineBuilder Layer(string layer, BlendMode mode, string entry, Action<LayerBuilder> build)
    {
        var builder = new LayerBuilder();
        build(builder);
        _layers.Add(new LayerDefinition(layer, mode, entry, builder.States, builder.Transitions));
        return this;
    }

    /// <summary>The definition, validated.</summary>
    public StateMachineDefinition Build()
    {
        var definition = new StateMachineDefinition(name, _parameters.ToArray(), _layers.ToArray());
        definition.Validate();
        return definition;
    }

    public sealed class LayerBuilder
    {
        internal List<StateDefinition> States { get; } = [];
        internal List<TransitionDefinition> Transitions { get; } = [];

        public LayerBuilder State(string state, string clip, double speed = 1, string? speedParameter = null, bool loop = false)
        {
            States.Add(new StateDefinition(state, clip, speed, speedParameter, loop));
            return this;
        }

        /// <summary>A state that plays nothing (the layers below show).</summary>
        public LayerBuilder Empty(string state)
        {
            States.Add(new StateDefinition(state, null));
            return this;
        }

        public LayerBuilder Transition(string from, string to, double duration, params ConditionDefinition[] conditions) =>
            Add(new TransitionDefinition(from, to, conditions, null, duration));

        /// <summary>A transition once the state has played <paramref name="exitTime"/> times through its clip (and the conditions hold).</summary>
        public LayerBuilder TransitionAfter(string from, string to, double exitTime, double duration, params ConditionDefinition[] conditions) =>
            Add(new TransitionDefinition(from, to, conditions, exitTime, duration));

        /// <summary>A transition from any state, checked before the current state's.</summary>
        public LayerBuilder FromAny(string to, double duration, bool mayRestart, params ConditionDefinition[] conditions) =>
            Add(new TransitionDefinition(null, to, conditions, null, duration, mayRestart));

        LayerBuilder Add(TransitionDefinition transition)
        {
            Transitions.Add(transition);
            return this;
        }
    }
}
