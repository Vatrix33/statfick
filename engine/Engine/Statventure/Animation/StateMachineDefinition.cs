using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Animation;

// A state machine as data (Docs/Animation.md, "State machines in detail"): plain records the C#
// builder makes, and records of their own in the packs (M9: Docs/Assets.md, "How it works", "Models and
// state machines in packs"; types animation.*), written as their JSON form; M11 gives them an editor.
// Everything is found by name.

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
[Record("animation.parameter", 1)]
public sealed record ParameterDefinition([property: Field(1)] string Name, [property: Field(2)] ParameterKind Kind, [property: Field(3)] double Default = 0);

/// <summary>A test of one parameter; for a trigger, whether it's set.</summary>
[Record("animation.condition", 1)]
public sealed record ConditionDefinition([property: Field(1)] string Parameter, [property: Field(2)] Compare Compare = Compare.Equal,
    [property: Field(3)] double Value = 1)
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
[Record("animation.state", 1)]
public sealed record StateDefinition([property: Field(1)] string Name, [property: Field(2)] string? Clip, [property: Field(3)] double Speed = 1,
    [property: Field(4)] string? SpeedParameter = null, [property: Field(5)] bool Loop = false);

/// <summary>
/// A transition from a state (null: from any state, checked before the current state's own) to
/// another: all <see cref="Conditions"/> must hold, and with an <see cref="ExitTime"/> only once the
/// state has played that many times through its clip (1: its end; 0.5: half way); it cross-fades
/// over <see cref="Duration"/> seconds. It enters the state its layer is already in only if
/// <see cref="MayRestart"/>.
/// </summary>
[Record("animation.transition", 1)]
public sealed record TransitionDefinition([property: Field(1)] string? From, [property: Field(2)] string To,
    [property: Field(3)] IReadOnlyList<ConditionDefinition> Conditions, [property: Field(4)] double? ExitTime = null, [property: Field(5)] double Duration = 0,
    [property: Field(6)] bool MayRestart = false);

/// <summary>A layer of states: its blend mode over the layers below, its entry state, states and transitions (in priority order).</summary>
[Record("animation.layer", 1)]
public sealed record LayerDefinition([property: Field(1)] string Name, [property: Field(2)] BlendMode Mode, [property: Field(3)] string Entry,
    [property: Field(4)] IReadOnlyList<StateDefinition> States, [property: Field(5)] IReadOnlyList<TransitionDefinition> Transitions);

/// <summary>
/// A state machine: its parameters and layers (bottom first). In the game's data it is a record entry of
/// its own (<c>animation.state-machine</c>), packed from its JSON form.
/// </summary>
[Record("animation.state-machine", 1)]
[Kind("State machine")]
[Help("How a figure's clips follow each other: layers of states, each playing a clip, and the transitions between them that the game's values start.")]
public sealed record StateMachineDefinition([property: Field(1)] string Name, [property: Field(2)] IReadOnlyList<ParameterDefinition> Parameters,
    [property: Field(3)] IReadOnlyList<LayerDefinition> Layers)
{
    /// <summary>
    /// The state machine at <paramref name="path"/> in the game's data (a record entry, or its JSON form in
    /// the development folder), validated (<see cref="Validate"/>; S5-1): a <see cref="DataFileException"/>
    /// naming the pack and entry or the loose file when it isn't a readable state machine record or doesn't
    /// validate.
    /// </summary>
    public static StateMachineDefinition Load(VirtualFileSystem files, string path)
    {
        var definition = files.Load<StateMachineDefinition>(path);
        try
        {
            definition.Validate();
        }
        catch (ArgumentException problem)
        {
            throw files.Problem(path, $"not a valid state machine: {problem.Message}", problem);
        }
        return definition;
    }

    /// <summary>
    /// Throws when a name doesn't resolve or a condition doesn't fit its parameter: the first of
    /// <see cref="RecordChecks.Check"/>'s problems (its own rules, <see cref="Rules"/>).
    /// </summary>
    public void Validate()
    {
        if (RecordChecks.Check(this).FirstOrDefault() is { } problem)
            throw new ArgumentException(problem.Message);
    }

    /// <summary>
    /// The machine's own check (P20): <see cref="Validate"/>'s rules, in their order and with their words, at
    /// the path of what they name (a parameter's or state's name defined twice, the layers, a speed
    /// parameter, a layer's entry, a transition's states, duration, exit time, a condition's parameter,
    /// comparison or value, a transition with neither conditions nor an exit time). A rule that needs what
    /// an earlier one found broken skips it.
    /// </summary>
    [Check]
    static void Rules(StateMachineDefinition machine, CheckContext check)
    {
        static RecordPath F(string field) => RecordPath.Root.Field(field);
        var name = machine.Name;
        var parameters = new Dictionary<string, ParameterKind>();
        for (var i = 0; i < machine.Parameters.Count; i++)
        {
            var p = machine.Parameters[i];
            if (!parameters.TryAdd(p.Name, p.Kind))
                check.Report(F("parameters").Index(i).Field("name"), $"{name}: the parameter {p.Name} is defined twice");
        }
        if (machine.Layers.Count == 0)
            check.Report(F("layers"), $"{name}: no layers");
        for (var l = 0; l < machine.Layers.Count; l++)
        {
            var layer = machine.Layers[l];
            var at = F("layers").Index(l);
            var states = new HashSet<string>();
            for (var s = 0; s < layer.States.Count; s++)
            {
                var state = layer.States[s];
                if (!states.Add(state.Name))
                    check.Report(at.Field("states").Index(s).Field("name"), $"{name}/{layer.Name}: the state {state.Name} is defined twice");
                if (state.SpeedParameter is { } speed && (!parameters.TryGetValue(speed, out var kind) || kind != ParameterKind.Float))
                    check.Report(at.Field("states").Index(s).Field("speedParameter"), $"{name}/{layer.Name}/{state.Name}: the speed parameter {speed} isn't a float parameter");
            }
            if (!states.Contains(layer.Entry))
                check.Report(at.Field("entry"), $"{name}/{layer.Name}: no entry state {layer.Entry}");
            for (var n = 0; n < layer.Transitions.Count; n++)
            {
                var t = layer.Transitions[n];
                var transition = at.Field("transitions").Index(n);
                var from = t.From == null || states.Contains(t.From);
                if (!from || !states.Contains(t.To))
                {
                    var message = $"{name}/{layer.Name}: a transition {t.From ?? "(any)"} → {t.To} names a state that doesn't exist";
                    if (!from)
                        check.Report(transition.Field("from"), message);
                    if (!states.Contains(t.To))
                        check.Report(transition.Field("to"), message);
                }
                if (t.Duration < 0 || t.ExitTime < 0)
                {
                    var message = $"{name}/{layer.Name}: a transition to {t.To} has a negative duration or exit time";
                    if (t.Duration < 0)
                        check.Report(transition.Field("duration"), message);
                    if (t.ExitTime < 0)
                        check.Report(transition.Field("exitTime"), message);
                }
                for (var c = 0; c < t.Conditions.Count; c++)
                {
                    var condition = t.Conditions[c];
                    var where = transition.Field("conditions").Index(c);
                    if (!parameters.TryGetValue(condition.Parameter, out var kind))
                    {
                        check.Report(where.Field("parameter"), $"{name}/{layer.Name}: a transition to {t.To} tests {condition.Parameter}, which isn't a parameter");
                        continue;
                    }
                    if (kind == ParameterKind.Trigger && (condition.Compare != Compare.Equal || condition.Value != 1))
                    {
                        var message = $"{name}/{layer.Name}: a trigger condition ({condition.Parameter}) can only test that it's set";
                        if (condition.Compare != Compare.Equal)
                            check.Report(where.Field("compare"), message);
                        if (condition.Value != 1)
                            check.Report(where.Field("value"), message);
                    }
                    if (kind == ParameterKind.Bool && condition.Compare is not (Compare.Equal or Compare.NotEqual))
                        check.Report(where.Field("compare"), $"{name}/{layer.Name}: a bool condition ({condition.Parameter}) can only test equality");
                }
                if (t.Conditions.Count == 0 && t.ExitTime == null)
                    check.Report(transition, $"{name}/{layer.Name}: a transition to {t.To} has neither conditions nor an exit time");
                // An exit time counts passes through the state's clip: an empty state has none, and
                // a transition from any state doesn't know whose (a state that isn't one is the first rule's).
                if (t.ExitTime != null && (t.From == null || from && layer.States.First(st => st.Name == t.From).Clip == null))
                    check.Report(transition.Field("exitTime"), $"{name}/{layer.Name}: a transition to {t.To} has an exit time but no clip to count it in");
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
