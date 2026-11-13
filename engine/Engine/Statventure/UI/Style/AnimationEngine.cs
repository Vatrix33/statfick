using System.Runtime.CompilerServices;
using Statventure.UI.Css;
using Statventure.UI.Dom;

namespace Statventure.UI.Style;

/// <summary>
/// CSS animations and transitions (CSS Animations 1, CSS Transitions 1): keyframe tracks per
/// property with per-keyframe easing, iterations, directions, fill modes and play state; transitions
/// started when a transitioned property's value changes, from the value before the change (a
/// reversal shortened as Blink does). Animated values are written into the element's visible style
/// (which children inherit from).
/// Their DOM events follow Blink: each animation and transition remembers the phase its events last
/// saw; a frame's timing update (<see cref="UpdateTiming"/>) queues the events its phase changes call
/// for, cancelling queues its event at once, and the document dispatches the queue after the frame's
/// scroll events. The CSS animations inside the UI hosts of a playing sequence run on the sequence's own
/// timeline (<see cref="AnimationTimeline"/>; Docs/Scene.md, S6-D), which its player holds while it is
/// paused or its document hidden; the rest, transitions included, run on the document's clock.
/// </summary>
public sealed class AnimationEngine
{
    readonly StyleEngine _styles;
    readonly Dictionary<(UiElement Element, string Pseudo), Target> _targets = [];
    /// <summary>Every animation and transition whose events may still come (Blink's timeline keeps an animation while it needs updates).</summary>
    readonly List<Effect> _effects = [];
    /// <summary>Events waiting for the frame's animation event step (Blink's scripted animation controller queue): at elements, or at script animations.</summary>
    readonly List<(object Target, UiEvent Event)> _events = [];
    /// <summary>Something was created since the last timing update (its events come with the next one).</summary>
    bool _pending;
    long _sequence;
    long _transitionGeneration;

    public AnimationEngine(StyleEngine styles)
    {
        _styles = styles;
    }

    /// <summary>The animation clock in seconds.</summary>
    public double Time { get; private set; }

    /// <summary>True while animated values change with time, or events wait for a frame.</summary>
    public bool HasActive => _pending || _events.Count > 0 || _targets.Values.Any(t => !Settled(t)) || _scriptChanged || _pendingScript.Count > 0 || _scriptTimeline.Exists(a => a.IsMoving);

    /// <summary>
    /// Whether a property of an element has a current animation or transition (Blink's
    /// HasCurrent*Animation: one that isn't finished, paused ones included), which composites it.
    /// </summary>
    public bool HasCurrentAnimation(UiElement element, string pseudo, string property)
    {
        if (!_targets.TryGetValue((element, pseudo), out var target))
            return false;
        if (target.Transitions.ContainsKey(property))
            return true;
        foreach (var animation in target.Animations)
            if (!animation.Finished && animation.Keyframes.Properties.Contains(property))
                return true;
        if (pseudo.Length == 0 && _scripted.TryGetValue(element, out var scripted))
            foreach (var animation in scripted)
                if (animation.Effect.Properties.Contains(property) && animation.PlayState != UiAnimationPlayState.Idle && animation.Effect.Sample(animation.CurrentTimeInternal()).Current)
                    return true;
        return false;
    }

    static readonly string[] TransformProperties = ["transform", "translate", "rotate", "scale"];

    /// <summary>
    /// Whether every animation of an element's transform properties (transform, translate, rotate,
    /// scale: CSS animations, transitions and script animations) has only keyframes the test passes
    /// (Blink's ElementAnimations::IsIdentityOrTranslation, with the test saying whether a style's
    /// transform is an identity or a translation): the painter asks it for a box with a transform
    /// animation, which keeps the fraction of its paint offset only then.
    /// </summary>
    internal bool TransformAnimationsAre(UiElement element, string pseudo, Func<ComputedStyle, bool> test)
    {
        if (!_targets.TryGetValue((element, pseudo), out var target))
            return true;
        foreach (var (property, transition) in target.Transitions)
            if (TransformProperties.Contains(property) && !(test(transition.From) && test(transition.To)))
                return false;
        foreach (var animation in target.Animations)
        {
            if (!TransformProperties.Any(animation.Keyframes.Properties.Contains))
                continue;
            animation.Tracks ??= BuildTracks(animation, target);
            foreach (var property in TransformProperties)
                if (animation.Tracks.TryGetValue(property, out var frames) && !frames.TrueForAll(f => test(f.Style)))
                    return false;
        }
        if (pseudo.Length == 0 && _scripted.TryGetValue(element, out var scripted))
            foreach (var animation in scripted)
            {
                if (animation.PlayState == UiAnimationPlayState.Idle || !TransformProperties.Any(animation.Effect.Properties.Contains))
                    continue;
                if (!target.ScriptTracks.TryGetValue(animation, out var tracks))
                    target.ScriptTracks[animation] = tracks = BuildScriptTracks(animation.Effect, target);
                foreach (var property in TransformProperties)
                    if (tracks.TryGetValue(property, out var records) && !records.TrueForAll(r => test(r.From ?? target.Base) && test(r.To ?? target.Base)))
                        return false;
            }
        return true;
    }

    /// <summary>
    /// Animated values are out of date: the frame's timing update or a script animation's change
    /// asks for a style update (Blink's SetNeedsAnimationStyleRecalc), so a forced style update
    /// (getComputedStyle in a frame callback) samples them at the frame's time.
    /// </summary>
    public bool NeedsTick { get; private set; }

    /// <summary>Sets the clock (animations and transitions started by the next restyle begin at this time).</summary>
    public void Advance(double time) => Time = time;

    /// <summary>The time of the last frame's timing update (a visible document's last frame; NaN before the first).</summary>
    internal double LastFrameTime { get; private set; } = double.NaN;

    // ---------- Sequences' timelines (Docs/Scene.md, S6-D) ----------

    readonly List<AnimationTimeline> _timelines = [];

    /// <summary>
    /// Puts <paramref name="root"/> on <paramref name="timeline"/> (its first root attaches the timeline): the
    /// CSS animations of the root, its descendants and their pseudo-elements, those running now and those
    /// that start later, run on the timeline's clock until it is detached.
    /// </summary>
    internal void Attach(AnimationTimeline timeline, UiElement root)
    {
        if (!_timelines.Contains(timeline))
            _timelines.Add(timeline);
        timeline.Roots.Add(root);
        foreach (var target in _targets.Values)
            if (Inside(target.Element, root))
                foreach (var animation in target.Animations)
                    if (animation.Timeline is null)
                        Move(animation, timeline);
    }

    /// <summary>The timeline ends (its sequence ended): its animations go on on the document's clock from where they stand.</summary>
    internal void Detach(AnimationTimeline timeline)
    {
        if (!_timelines.Remove(timeline))
            return;
        foreach (var target in _targets.Values)
            foreach (var animation in target.Animations)
                if (animation.Timeline == timeline)
                    Move(animation, null);
    }

    /// <summary>An attached timeline one of whose roots is <paramref name="element"/>, is inside it or holds it, or null.</summary>
    internal AnimationTimeline? TimelineAround(UiElement element) =>
        _timelines.FirstOrDefault(t => t.Roots.Any(root => Inside(element, root) || Inside(root, element)));

    /// <summary>The timeline whose roots hold <paramref name="element"/> (the latest attached), or null for the document's clock.</summary>
    AnimationTimeline? TimelineOf(UiElement element)
    {
        for (var i = _timelines.Count - 1; i >= 0; i--)
            if (_timelines[i].Roots.Any(root => Inside(element, root)))
                return _timelines[i];
        return null;
    }

    static bool Inside(UiElement element, UiElement root)
    {
        for (UiNode? node = element; node is not null; node = node.Parent)
            if (node == root)
                return true;
        return false;
    }

    /// <summary>An animation onto another clock, keeping where it stands (its start and pause time in the new clock's terms).</summary>
    void Move(RunningAnimation animation, AnimationTimeline? timeline)
    {
        var shift = (timeline?.Now(Time) ?? Time) - NowOf(animation);
        animation.Start += shift;
        if (animation.PausedAt is { } paused)
            animation.PausedAt = paused + shift;
        animation.Timeline = timeline;
    }

    /// <summary>An animation's clock now: its sequence's timeline's, or the document's.</summary>
    double NowOf(RunningAnimation animation) => animation.Timeline?.Now(Time) ?? Time;

    /// <summary>An effect's phase (Web Animations): before its delay is over, active, after its end, or none (cancelled).</summary>
    enum Phase { None, Before, Active, After }

    /// <summary>Blink's time tolerance at phase boundaries (1 µs).</summary>
    const double Tolerance = 1e-6;

    sealed class Frame
    {
        public required float Offset;
        public required ComputedStyle Style;
        public TimingFunction? Timing;
    }

    /// <summary>A keyframe of a custom property: its value (a registered one's computed value; null when invalid at computed-value time, or absent).</summary>
    sealed record CustomFrame(float Offset, List<CssToken>? Value, TimingFunction? Timing);

    abstract class Effect
    {
        public required Target Target;
        /// <summary>The frame time it was created at: timing updates see it from the next frame on (Blink starts it at the frame's commit).</summary>
        public required double Created;
        /// <summary>Creation order (the last tie-break of Blink's composite order).</summary>
        public required long Sequence;
        /// <summary>The phase its events last saw (Blink's event delegate's previous phase).</summary>
        public Phase PreviousPhase = Phase.None;
    }

    sealed class RunningAnimation : Effect
    {
        public required AnimationSpec Spec;
        public required KeyframesDefinition Keyframes;
        public required double Start;
        public double? PausedAt;
        /// <summary>The sequence timeline it runs on (its start and pause time are that clock's), or null for the document's clock.</summary>
        public AnimationTimeline? Timeline;
        public Dictionary<string, List<Frame>>? Tracks;
        /// <summary>Its custom properties' keyframes (their values as the target's base gives them).</summary>
        public Dictionary<string, List<CustomFrame>>? CustomTracks;
        public bool Finished;
        /// <summary>Its place in animation-name (the composite order among the element's animations).</summary>
        public int Index;
        public double? PreviousIteration;
    }

    sealed class RunningTransition : Effect
    {
        public required string Property;
        public required ComputedStyle From;
        public required ComputedStyle To;
        public required double Start;
        public required float Duration;
        public required float Delay;
        public required TimingFunction Timing;
        /// <summary>CSS Transitions §3: the start value a transition back to which is shortened, and this one's shortening factor.</summary>
        public required ComputedStyle ReversingAdjustedStart;
        public required double ShorteningFactor;
        /// <summary>Blink's transition generation (the style updates that started transitions, in order).</summary>
        public required long Generation;
    }

    sealed class Target
    {
        public required UiElement Element;
        public required string Pseudo;
        public required ComputedStyle Base;
        public required ComputedStyle Parent;
        public float RootFontSize;
        public IReadOnlySet<string> Important = ElementStyles.EmptySet;
        public readonly List<RunningAnimation> Animations = [];
        public readonly Dictionary<string, RunningTransition> Transitions = new(StringComparer.Ordinal);
        /// <summary>Properties written by animations or transitions last time (reset from the base first).</summary>
        public HashSet<string> Written = new(StringComparer.Ordinal);
        /// <summary>Properties animations wrote last time (transitions leave them alone).</summary>
        public HashSet<string> AnimationWritten = new(StringComparer.Ordinal);
        /// <summary>A transition left: the values need writing once more.</summary>
        public bool Stale;
        /// <summary>Recomputes the element's style with animated custom properties' values (null for a pseudo-element: its custom properties aren't animated).</summary>
        public Func<IReadOnlyDictionary<string, List<CssToken>?>, ComputedStyle>? Recompute;
        /// <summary>The animated custom properties' values the shown style was computed with (null: none).</summary>
        public Dictionary<string, List<CssToken>?>? CustomShown;
        /// <summary>What the animated values are written over: the base, or the base recomputed with the animated custom properties.</summary>
        public ComputedStyle? VisibleBase;

        /// <summary>Nothing here changes with time: only finished or paused animations (their values are in the visible style already).</summary>
        public bool Settled => !Stale && Transitions.Count == 0 && Animations.TrueForAll(a => a.Finished || a.PausedAt is not null || a.Timeline is { Held: true });
        /// <summary>Script animations' keyframes per property, made from this target's base style.</summary>
        public readonly Dictionary<UiAnimation, Dictionary<string, List<ScriptRecord>>> ScriptTracks = [];
    }

    /// <summary>Settled, and no script animation of the element moves.</summary>
    bool Settled(Target target) =>
        target.Settled && !(target.Pseudo.Length == 0 && _scripted.TryGetValue(target.Element, out var scripted) && scripted.Exists(a => a.IsMoving));

    bool HasScripted(UiElement element, string pseudo) => pseudo.Length == 0 && _scripted.TryGetValue(element, out var scripted) && scripted.Count > 0;

    /// <summary>
    /// After an element's base style was recomputed: starts, keeps or cancels its animations and starts
    /// or cancels transitions for changed properties. Returns the style to show (the base, or a copy with animated values).
    /// </summary>
    /// <param name="recompute">
    /// Computes the element's style again with animated custom properties over its cascaded ones (they reach the
    /// properties that use them, as in Blink's cascade); null when they aren't applied (pseudo-elements).
    /// </param>
    public ComputedStyle Restyled(UiElement element, string pseudo, ComputedStyle? shown, ComputedStyle newBase, ComputedStyle parent, float rootFontSize, IReadOnlySet<string> important,
        Func<IReadOnlyDictionary<string, List<CssToken>?>, ComputedStyle>? recompute = null)
    {
        var key = (element, pseudo);
        _targets.TryGetValue(key, out var target);
        // Animations run while the element is rendered; transitions need a style it was rendered with
        // before (Blink has no old style for an element that wasn't).
        var displayed = newBase.Display != Display.None;
        var wasDisplayed = shown is not null && shown.Display != Display.None;
        var specs = displayed ? newBase.Animations : [];
        if (target is null && specs.Length == 0 && (!wasDisplayed || newBase.Transitions.Length == 0) && !HasScripted(element, pseudo))
            return newBase;
        // The style before the change: the old base (its transitions get sampled), or the shown one when nothing ran.
        var oldBase = target?.Base ?? shown;
        target ??= new Target { Element = element, Pseudo = pseudo, Base = newBase, Parent = parent };

        // Animations by name (and occurrence): new names start now, removed ones are cancelled, kept ones continue.
        var kept = new List<RunningAnimation>();
        for (var i = 0; i < specs.Length; i++)
        {
            var spec = specs[i];
            if (spec.Name == "none" || _styles.Keyframes(spec.Name) is not { } keyframes)
                continue;
            var existing = target.Animations.FirstOrDefault(a => a.Spec.Name == spec.Name && !kept.Contains(a));
            if (existing is not null)
            {
                // Paused/resumed: shift the start so the animation continues from where it stopped.
                if (spec.Paused && existing.PausedAt is null)
                    existing.PausedAt = NowOf(existing);
                else if (!spec.Paused && existing.PausedAt is { } pausedAt)
                {
                    existing.Start += NowOf(existing) - pausedAt;
                    existing.PausedAt = null;
                }
                existing.Spec = spec;
                existing.Keyframes = keyframes;
                existing.Index = i;
                existing.Tracks = null;
                existing.CustomTracks = null;
                kept.Add(existing);
            }
            else
            {
                var timeline = TimelineOf(element);
                var now = timeline?.Now(Time) ?? Time;
                var created = new RunningAnimation { Target = target, Created = Time, Sequence = _sequence++, Spec = spec, Keyframes = keyframes, Start = now, PausedAt = spec.Paused ? now : null, Timeline = timeline, Index = i };
                kept.Add(created);
                _effects.Add(created);
                _pending = true;
            }
        }
        // Blink cancels removed animations from the last one back.
        for (var i = target.Animations.Count - 1; i >= 0; i--)
            if (!kept.Contains(target.Animations[i]))
                Cancel(target.Animations[i]);
        target.Animations.Clear();
        target.Animations.AddRange(kept);

        if (wasDisplayed && displayed)
            UpdateTransitions(target, oldBase!, newBase);
        else
            foreach (var transition in target.Transitions.Values.ToList())
                Cancel(transition);

        target.Base = newBase;
        target.Parent = parent;
        target.RootFontSize = rootFontSize;
        target.Important = important;
        target.Recompute = recompute;
        target.ScriptTracks.Clear();
        if (target.Animations.Count == 0 && target.Transitions.Count == 0 && !HasScripted(element, pseudo))
        {
            _targets.Remove(key);
            return newBase;
        }
        _targets[key] = target;
        target.Written.Clear();
        // Animated custom properties are part of the cascade: the style is computed again with them.
        var custom = CustomOverrides(target);
        target.CustomShown = custom;
        target.VisibleBase = custom is not null && recompute is not null ? recompute(custom) : newBase;
        var visible = target.VisibleBase.Clone();
        Evaluate(target, visible, out _);
        return visible;
    }

    // ---------- Custom properties ----------

    /// <summary>Elements whose animated custom properties changed since their style was computed (the document restyles them).</summary>
    readonly List<(UiElement Element, string Pseudo)> _customRestyles = [];

    /// <summary>The elements whose animated custom properties changed at the last <see cref="Tick"/>: their styles must be computed again.</summary>
    public List<(UiElement Element, string Pseudo)> TakeCustomRestyles()
    {
        var restyles = _customRestyles.ToList();
        _customRestyles.Clear();
        return restyles;
    }

    /// <summary>
    /// The values of the target's animated custom properties now (Blink's active interpolations of custom properties,
    /// applied in the cascade): its transitions', then its animations' (later ones over earlier ones); an !important
    /// declaration of the property is left alone. Null when nothing animates a custom property.
    /// </summary>
    Dictionary<string, List<CssToken>?>? CustomOverrides(Target target)
    {
        if (target.Recompute is null)
            return null;
        Dictionary<string, List<CssToken>?>? values = null;
        foreach (var (property, transition) in target.Transitions)
        {
            if (!IsCustom(property) || target.Important.Contains(property) || TransitionProgress(transition) is not { } t)
                continue;
            (values ??= new(StringComparer.Ordinal))[property] = CustomValues.Blend(_styles.Registration(property), CustomOf(transition.From, property), CustomOf(transition.To, property), t, target.Base, target.Parent);
        }
        foreach (var animation in target.Animations)
        {
            if (Progress(animation, out _) is not { } p)
                continue;
            animation.CustomTracks ??= BuildCustomTracks(animation, target);
            foreach (var (property, frames) in animation.CustomTracks)
            {
                if (target.Important.Contains(property))
                    continue;
                var i = 0;
                while (i < frames.Count - 2 && p > frames[i + 1].Offset)
                    i++;
                var a = frames[i];
                var b = frames[Math.Min(i + 1, frames.Count - 1)];
                var span = b.Offset - a.Offset;
                var local = span > 0 ? Math.Clamp((p - a.Offset) / span, 0, 1) : 1;
                var eased = (a.Timing ?? animation.Spec.Timing).Apply(local);
                (values ??= new(StringComparer.Ordinal))[property] = CustomValues.Blend(_styles.Registration(property), a.Value, b.Value, eased, target.Base, target.Parent);
            }
        }
        return values;
    }

    static bool IsCustom(string property) => property.StartsWith("--", StringComparison.Ordinal);

    static List<CssToken>? CustomOf(ComputedStyle style, string property) => style.Custom.GetValueOrDefault(property);

    /// <summary>
    /// An animation's custom property tracks (CSSInterpolationType::MaybeConvertCustomPropertyDeclaration): each keyframe's
    /// value with var() substituted from the base, a CSS-wide keyword taken as one (initial: the registered initial value;
    /// inherit, and unset of a property that inherits: the parent's; unset otherwise: the initial value), a registered
    /// one computed (null when invalid); missing 0 % and 100 % keyframes the base's value (the neutral keyframes' underlying value).
    /// </summary>
    Dictionary<string, List<CustomFrame>> BuildCustomTracks(RunningAnimation animation, Target target)
    {
        var tracks = new Dictionary<string, List<CustomFrame>>(StringComparer.Ordinal);
        foreach (var property in animation.Keyframes.Properties)
        {
            if (!IsCustom(property))
                continue;
            var registration = _styles.Registration(property);
            var frames = new List<CustomFrame>();
            foreach (var frame in animation.Keyframes.Frames)
                if (frame.Declarations.LastOrDefault(d => d.Property == property) is { } declaration)
                    frames.Add(new CustomFrame(frame.Offset, CustomValues.OfKeyframe(registration, declaration, target.Base, target.Parent), frame.Timing));
            if (frames.Count == 0)
                continue;
            var underlying = CustomOf(target.Base, property);
            if (frames[0].Offset > 0)
                frames.Insert(0, new CustomFrame(0, underlying, null));
            if (frames[^1].Offset < 1)
                frames.Add(new CustomFrame(1, underlying, null));
            tracks[property] = frames;
        }
        return tracks;
    }

    static bool SameCustom(Dictionary<string, List<CssToken>?>? a, Dictionary<string, List<CssToken>?>? b)
    {
        if (a is null || b is null)
            return a is null && b is null;
        if (a.Count != b.Count)
            return false;
        foreach (var (name, value) in a)
            if (!b.TryGetValue(name, out var other) || !(value is null ? other is null : other is not null && value.SequenceEqual(other)))
                return false;
        return true;
    }

    /// <summary>
    /// Blink's transition update (CSSAnimations::CalculateTransitionUpdate): per transitioned property, a
    /// running transition to another value is cancelled; a changed value starts a transition from the
    /// value before the change (one going back to where the cancelled one began is shortened);
    /// transitions of properties no longer listed are cancelled.
    /// </summary>
    void UpdateTransitions(Target target, ComputedStyle oldBase, ComputedStyle newBase)
    {
        // Sampled before anything is cancelled (Blink samples the transitions it had).
        var beforeChange = BeforeChangeStyle(target, oldBase);
        // Properties animations affect take no transitions (Blink: CanCalculateTransitionUpdateForProperty).
        var animated = new HashSet<string>(target.AnimationWritten, StringComparer.Ordinal);
        foreach (var animation in target.Animations)
            if (Progress(animation, out _) is not null)
                animated.UnionWith(animation.Keyframes.Properties);
        // No transitions listed is "all 0s": running transitions to another value are cancelled; "none" cancels them all.
        var listsNone = newBase.TransitionProperty is ["none"];
        var listsAll = newBase.Transitions.Length == 0 ? !listsNone : newBase.Transitions.Any(t => t.Property == "all");
        var properties = newBase.Transitions.Length > 0
            ? TransitionedProperties(newBase).Distinct().ToList()
            : listsNone ? [] : target.Transitions.Keys.ToList();
        var started = false;
        foreach (var property in properties)
        {
            if (animated.Contains(property))
                continue;
            if (target.Transitions.TryGetValue(property, out var running))
            {
                if (ValuesEqual(property, running.To, newBase))
                {
                    running.To = newBase;
                    continue;
                }
                Cancel(running);
            }
            if (newBase.Transitions.Length == 0 || TransitionFor(newBase, property) is not { } spec)
                continue;
            if (ValuesEqual(property, beforeChange, newBase) || !IsInterpolable(property, beforeChange, newBase) || spec.Duration + spec.Delay <= 0)
                continue;
            var duration = spec.Duration;
            var delay = spec.Delay;
            var adjustedStart = beforeChange;
            var factor = 1.0;
            if (running is not null && ValuesEqual(property, newBase, running.ReversingAdjustedStart) && TransitionProgress(running) is { } progress)
            {
                // Interrupted and going back: it takes as long as the part of the way it had come.
                adjustedStart = running.To;
                factor = Math.Clamp(progress * running.ShorteningFactor + (1 - running.ShorteningFactor), 0, 1);
                duration = (float)(duration * factor);
                if (delay < 0)
                    delay = (float)(delay * factor);
            }
            if (!started)
            {
                started = true;
                _transitionGeneration++;
            }
            var transition = new RunningTransition
            {
                Target = target,
                Created = Time,
                Sequence = _sequence++,
                Property = property,
                From = beforeChange,
                To = newBase,
                Start = Time,
                Duration = duration,
                Delay = delay,
                Timing = spec.Timing,
                ReversingAdjustedStart = adjustedStart,
                ShorteningFactor = factor,
                Generation = _transitionGeneration,
            };
            target.Transitions[property] = transition;
            _effects.Add(transition);
            _pending = true;
        }
        if (!listsAll)
        {
            var listed = properties.ToHashSet(StringComparer.Ordinal);
            foreach (var (property, transition) in target.Transitions.ToList())
                if (!listed.Contains(property))
                    Cancel(transition);
        }
    }

    /// <summary>The old base with its transitions sampled now (Blink's before-change style leaves animations out).</summary>
    ComputedStyle BeforeChangeStyle(Target target, ComputedStyle oldBase)
    {
        if (target.Transitions.Count == 0)
            return oldBase;
        var style = oldBase.Clone();
        foreach (var (property, transition) in target.Transitions)
            if (TransitionProgress(transition) is { } progress)
            {
                if (IsCustom(property))
                {
                    style.Custom = new Dictionary<string, List<CssToken>>(style.Custom, StringComparer.Ordinal);
                    if (CustomValues.Blend(_styles.Registration(property), CustomOf(transition.From, property), CustomOf(transition.To, property), progress, target.Base, target.Parent) is { } value)
                        style.Custom[property] = value;
                    else
                        style.Custom.Remove(property);
                }
                else
                    Interpolation.Apply(property, transition.From, transition.To, (float)progress, style);
            }
        style.Finish();
        return style;
    }

    /// <summary>Do two styles agree on a property (a custom property: its tokens)?</summary>
    static bool ValuesEqual(string property, ComputedStyle a, ComputedStyle b)
    {
        if (!IsCustom(property))
            return Interpolation.ValuesEqual(property, a, b);
        var x = CustomOf(a, property);
        var y = CustomOf(b, property);
        return x is null ? y is null : y is not null && x.SequenceEqual(y);
    }

    /// <summary>
    /// Whether a change of the property transitions (a smooth interpolation exists: CSSAnimations::
    /// CalculateTransitionUpdateForPropertyHandle without allow-discrete): a custom property only when registered as a
    /// colour with both values colours (an unregistered one is discrete).
    /// </summary>
    bool IsInterpolable(string property, ComputedStyle before, ComputedStyle after) =>
        IsCustom(property)
            ? CustomValues.Smooth(_styles.Registration(property), CustomOf(before, property), CustomOf(after, property))
            : Interpolation.IsInterpolable(property);

    /// <summary>The element no longer has a style (removed, or display: none above it): its animations and transitions are cancelled.</summary>
    public void Forget(UiElement element)
    {
        Forget(element, "");
        Forget(element, "before");
        Forget(element, "after");
    }

    /// <summary>The element's pseudo-element (or the element, for "") went away: its animations and transitions are cancelled.</summary>
    public void Forget(UiElement element, string pseudo)
    {
        if (_targets.Remove((element, pseudo), out var target))
            CancelAll(target);
    }

    /// <summary>Blink's CSSAnimations::Cancel: the animations in order, then the transitions.</summary>
    void CancelAll(Target target)
    {
        foreach (var animation in target.Animations)
            Cancel(animation);
        target.Animations.Clear();
        foreach (var transition in target.Transitions.Values.ToList())
            Cancel(transition);
    }

    /// <summary>Cancels an animation: an animationcancel at its current time, unless its events saw it idle or done.</summary>
    void Cancel(RunningAnimation animation)
    {
        if (animation.PreviousPhase is not (Phase.None or Phase.After))
            Queue(animation.Target, new UiAnimationEvent("animationcancel")
            {
                AnimationName = animation.Spec.Name,
                ElapsedTime = (animation.PausedAt ?? NowOf(animation)) - animation.Start,
                PseudoElement = PseudoName(animation.Target.Pseudo),
            });
        animation.PreviousPhase = Phase.None;
        _effects.Remove(animation);
    }

    /// <summary>Cancels a transition: a transitioncancel at its active time (filling both ways), unless its events saw it idle or done.</summary>
    void Cancel(RunningTransition transition)
    {
        var target = transition.Target;
        if (target.Transitions.TryGetValue(transition.Property, out var current) && current == transition)
            target.Transitions.Remove(transition.Property);
        if (transition.PreviousPhase is not (Phase.None or Phase.After))
        {
            var local = Time - transition.Start;
            var elapsed = transition.PreviousPhase == Phase.Before ? Math.Max(local - transition.Delay, 0) : local - transition.Delay;
            Queue(target, new UiTransitionEvent("transitioncancel")
            {
                PropertyName = transition.Property,
                ElapsedTime = elapsed,
                PseudoElement = PseudoName(target.Pseudo),
            });
        }
        transition.PreviousPhase = Phase.None;
        _effects.Remove(transition);
    }

    void Queue(Target target, UiEvent e)
    {
        e.IsTrusted = true;
        _events.Add((target.Element, e));
    }

    /// <summary>A script animation's finish or cancel event, for the frame's animation event step.</summary>
    internal void QueueEvent(UiAnimation animation, UiEvent e)
    {
        e.IsTrusted = true;
        _events.Add((animation, e));
    }

    static string PseudoName(string pseudo) => pseudo.Length == 0 ? "" : "::" + pseudo;

    /// <summary>The events queued so far, in order (the queue empties).</summary>
    public List<(object Target, UiEvent Event)> TakeEvents()
    {
        var events = _events.ToList();
        _events.Clear();
        return events;
    }

    static IEnumerable<string> TransitionedProperties(ComputedStyle style)
    {
        foreach (var transition in style.Transitions)
        {
            if (transition.Property == "all")
            {
                foreach (var property in AnimatableProperties)
                    yield return property;
                continue;
            }
            if (Properties.IsShorthand(transition.Property))
            {
                foreach (var longhand in Properties.LonghandsOf(transition.Property))
                    yield return longhand;
                continue;
            }
            yield return Properties.Canonical(transition.Property);
        }
    }

    static readonly string[] AnimatableProperties = Properties.Ordered.Select(p => p.Name).Where(Interpolation.IsInterpolable).ToArray();

    /// <summary>The last transition entry that names the property (or all, or a shorthand of it).</summary>
    static TransitionSpec? TransitionFor(ComputedStyle style, string property)
    {
        TransitionSpec? match = null;
        foreach (var transition in style.Transitions)
        {
            var name = Properties.Canonical(transition.Property);
            if (name == "all" || name == property || (Properties.IsShorthand(name) && Properties.LonghandsOf(name).Contains(property)))
                match = transition;
        }
        return match;
    }

    /// <summary>
    /// Advances the clock and writes the animated values into the visible styles. Returns, for each
    /// element whose values changed, whether layout (not only paint) is affected and whether children
    /// inherit a changed value.
    /// </summary>
    public List<(UiElement Element, string Pseudo, ComputedStyle Visible, bool Layout, bool Inherited)> Tick(double time, Func<UiElement, string, ComputedStyle?> visibleOf)
    {
        Time = time;
        NeedsTick = false;
        var changed = new List<(UiElement, string, ComputedStyle, bool, bool)>();
        foreach (var (key, target) in _targets.ToList())
        {
            if (Settled(target))
                continue;
            if (visibleOf(key.Element, key.Pseudo) is not { } visible)
            {
                _targets.Remove(key);
                CancelAll(target);
                continue;
            }
            // Animated custom properties that changed: the style is computed again (Restyled evaluates the rest).
            if (!SameCustom(CustomOverrides(target), target.CustomShown))
            {
                _customRestyles.Add(key);
                continue;
            }
            Evaluate(target, visible, out var written);
            var layout = written.Any(p => !Interpolation.PaintOnly.Contains(p));
            var inherited = written.Any(p => Properties.Get(p)?.Inherited == true);
            if (written.Count > 0)
                changed.Add((key.Element, key.Pseudo, visible, layout, inherited));
            if (target.Animations.Count == 0 && target.Transitions.Count == 0 && !HasScripted(key.Element, key.Pseudo))
                _targets.Remove(key);
        }
        return changed;
    }

    // ---------- Events ----------

    /// <summary>An effect's timing as Blink normalizes it: start delay, iteration duration and count, active duration, end time.</summary>
    readonly record struct EffectTiming(double Delay, double IterationDuration, double Iterations, double Active, double End);

    static EffectTiming TimingOf(RunningAnimation animation)
    {
        var spec = animation.Spec;
        var duration = Math.Max(0, (double)spec.Duration);
        var iterations = Math.Max(0, (double)spec.IterationCount);
        // Zero times anything (infinity too) is zero.
        var active = duration == 0 || iterations == 0 ? 0 : duration * iterations;
        return new EffectTiming(spec.Delay, duration, iterations, active, Math.Max(spec.Delay + active, 0));
    }

    static EffectTiming TimingOf(RunningTransition transition) =>
        new(transition.Delay, transition.Duration, 1, transition.Duration, Math.Max(transition.Delay + transition.Duration, 0));

    /// <summary>Blink's CalculatePhase (playing forwards): before until the delay is over, active until the end (exclusive), then after.</summary>
    static Phase PhaseAt(EffectTiming timing, ref double local)
    {
        var beforeBoundary = Math.Max(Math.Min(timing.Delay, timing.End), 0);
        if (Math.Abs(local - beforeBoundary) < Tolerance)
            local = beforeBoundary;
        if (local < beforeBoundary)
            return Phase.Before;
        var afterBoundary = Math.Max(Math.Min(timing.Delay + timing.Active, timing.End), 0);
        if (Math.Abs(local - afterBoundary) < Tolerance)
            local = afterBoundary;
        return local >= afterBoundary ? Phase.After : Phase.Active;
    }

    /// <summary>The current iteration (Web Animations), null when the effect has no active time.</summary>
    static double? CurrentIteration(EffectTiming timing, AnimationFillMode fill, Phase phase, double local)
    {
        double? activeTime = phase switch
        {
            Phase.Before when fill is AnimationFillMode.Backwards or AnimationFillMode.Both => Math.Max(local - timing.Delay, 0),
            Phase.Active => local - timing.Delay,
            Phase.After when fill is AnimationFillMode.Forwards or AnimationFillMode.Both => Math.Max(0, Math.Min(timing.Active, local - timing.Delay)),
            _ => null,
        };
        if (activeTime is not { } active)
            return null;
        if (phase == Phase.After && double.IsInfinity(timing.Iterations))
            return double.PositiveInfinity;
        var overall = Math.Abs(timing.IterationDuration) < Tolerance
            ? phase != Phase.Before ? timing.Iterations : 0
            : active / timing.IterationDuration;
        var simple = double.IsInfinity(overall) ? 0 : overall % 1.0;
        if (simple == 0 && phase is Phase.Active or Phase.After && Math.Abs(active - timing.Active) < Tolerance && timing.Iterations != 0)
            simple = 1;
        return simple == 1 ? Math.Max(0, Math.Floor(overall) - 1) : Math.Floor(overall);
    }

    /// <summary>
    /// Blink's timing update for an animation frame: every animation and transition created before
    /// this frame compares its phase now with the one its events last saw and queues the events the
    /// change calls for (CSSAnimations' event delegates), transitions first, then animations, each in
    /// their composite order. A transition seen to end leaves its element (its value went already).
    /// </summary>
    public void UpdateTiming(double time)
    {
        Time = time;
        LastFrameTime = time;
        _pending = false;
        ServiceScriptAnimations();
        if (_targets.Values.Any(t => !Settled(t)))
            NeedsTick = true;
        if (_effects.Count == 0)
            return;
        var produced = new List<(Effect Effect, UiEvent Event)>();
        foreach (var effect in _effects.ToList())
        {
            if (effect.Created >= time)
            {
                _pending = true;
                continue;
            }
            if (effect is RunningAnimation animation)
                AnimationEvents(animation, produced);
            else if (effect is RunningTransition transition && TransitionEvents(transition, produced) == Phase.After)
            {
                var target = transition.Target;
                if (target.Transitions.TryGetValue(transition.Property, out var current) && current == transition)
                {
                    target.Transitions.Remove(transition.Property);
                    target.Stale = true;
                }
                _effects.Remove(transition);
            }
        }
        if (produced.Count == 0)
            return;
        var paths = new Dictionary<UiElement, List<int>>();
        foreach (var (effect, e) in produced.OrderBy(p => p.Effect, Comparer<Effect>.Create((a, b) => CompositeOrder(a, b, paths))))
            Queue(effect.Target, e);
    }

    /// <summary>Blink's AnimationEventDelegate::OnEventCondition.</summary>
    void AnimationEvents(RunningAnimation animation, List<(Effect, UiEvent)> produced)
    {
        var timing = TimingOf(animation);
        var local = (animation.PausedAt ?? NowOf(animation)) - animation.Start;
        var phase = PhaseAt(timing, ref local);
        var iteration = CurrentIteration(timing, animation.Spec.FillMode, phase, local);
        var previous = animation.PreviousPhase;
        void Add(string type, double elapsed) => produced.Add((animation, new UiAnimationEvent(type)
        {
            AnimationName = animation.Spec.Name,
            ElapsedTime = elapsed,
            PseudoElement = PseudoName(animation.Target.Pseudo),
        }));
        // The elapsed times at the start and the end of the active interval.
        var intervalStart = Math.Max(Math.Min(-timing.Delay, timing.Active), 0);
        var intervalEnd = Math.Max(Math.Min(timing.End - timing.Delay, timing.Active), 0);
        var change = previous != phase;
        if (change && ((previous is Phase.None or Phase.Before && phase is Phase.Active or Phase.After) || (previous == Phase.After && phase is Phase.Active or Phase.Before)))
            Add("animationstart", previous == Phase.After ? intervalEnd : intervalStart);
        if (change && (phase == Phase.After || (previous is Phase.Active or Phase.After && phase == Phase.Before)))
            Add("animationend", phase == Phase.After ? intervalEnd : intervalStart);
        if (!change && phase == Phase.Active && animation.PreviousIteration is { } last && iteration is { } now && last != now)
        {
            // One event for all the iterations since the last frame, with the first one's time.
            var boundary = last > now ? now + 1 : now;
            Add("animationiteration", timing.IterationDuration * boundary);
        }
        animation.PreviousIteration = iteration;
        animation.PreviousPhase = phase;
    }

    /// <summary>Blink's TransitionEventDelegate::OnEventCondition; returns the phase now.</summary>
    Phase TransitionEvents(RunningTransition transition, List<(Effect, UiEvent)> produced)
    {
        var timing = TimingOf(transition);
        var local = Time - transition.Start;
        var phase = PhaseAt(timing, ref local);
        var previous = transition.PreviousPhase;
        if (phase == previous)
            return phase;
        void Add(string type, double elapsed) => produced.Add((transition, new UiTransitionEvent(type)
        {
            PropertyName = transition.Property,
            ElapsedTime = elapsed,
            PseudoElement = PseudoName(transition.Target.Pseudo),
        }));
        // A negative delay starts it part of the way in.
        var startTime = timing.Delay < 0 ? -timing.Delay : 0;
        if (previous == Phase.None)
            Add("transitionrun", startTime);
        if (phase is Phase.Active or Phase.After && previous is Phase.None or Phase.Before)
            Add("transitionstart", startTime);
        else if (phase is Phase.Active or Phase.Before && previous == Phase.After)
            Add("transitionstart", timing.IterationDuration);
        if (phase == Phase.After && previous is Phase.Active or Phase.Before or Phase.None)
            Add("transitionend", timing.IterationDuration);
        else if (phase == Phase.Before && previous is Phase.Active or Phase.After)
            Add("transitionend", startTime);
        transition.PreviousPhase = phase;
        return phase;
    }

    /// <summary>
    /// Blink's composite order (Animation::HasLowerCompositeOrdering): transitions before animations;
    /// then the owning elements in tree order (Blink's timeline compares them by address, which follows
    /// creation); an element before its ::before before its ::after; transitions by generation and
    /// property name, animations by their place in animation-name; finally creation order.
    /// </summary>
    static int CompositeOrder(Effect a, Effect b, Dictionary<UiElement, List<int>> paths)
    {
        var kind = (a is RunningAnimation ? 1 : 0).CompareTo(b is RunningAnimation ? 1 : 0);
        if (kind != 0)
            return kind;
        if (a.Target.Element != b.Target.Element)
        {
            var order = ComparePaths(PathOf(a.Target.Element, paths), PathOf(b.Target.Element, paths));
            if (order != 0)
                return order;
        }
        var pseudo = PseudoPriority(a.Target.Pseudo).CompareTo(PseudoPriority(b.Target.Pseudo));
        if (pseudo != 0)
            return pseudo;
        if (a is RunningTransition ta && b is RunningTransition tb)
        {
            var generation = ta.Generation.CompareTo(tb.Generation);
            if (generation != 0)
                return generation;
            var name = string.CompareOrdinal(ta.Property, tb.Property);
            if (name != 0)
                return name;
        }
        else if (a is RunningAnimation aa && b is RunningAnimation ab && aa.Index != ab.Index)
            return aa.Index.CompareTo(ab.Index);
        return a.Sequence.CompareTo(b.Sequence);
    }

    static int PseudoPriority(string pseudo) => pseudo switch
    {
        "" => 0,
        "before" => 1,
        _ => 2,
    };

    static List<int> PathOf(UiElement element, Dictionary<UiElement, List<int>> paths)
    {
        if (paths.TryGetValue(element, out var path))
            return path;
        path = [];
        for (UiNode node = element; node.Parent is { } parent; node = parent)
            path.Add(UiDocument.IndexOf(node));
        path.Reverse();
        paths[element] = path;
        return path;
    }

    static int ComparePaths(List<int> a, List<int> b)
    {
        for (var i = 0; i < Math.Min(a.Count, b.Count); i++)
            if (a[i] != b[i])
                return a[i].CompareTo(b[i]);
        return a.Count.CompareTo(b.Count);
    }

    // ---------- Script animations (element.animate) ----------

    /// <summary>Each element's script animations in creation order (weak: an element that's gone takes them along).</summary>
    readonly ConditionalWeakTable<UiElement, List<UiAnimation>> _scripted = new();
    /// <summary>The script animations the frames' timing update services (Blink's animations needing update), in creation order.</summary>
    readonly List<UiAnimation> _scriptTimeline = [];
    /// <summary>Pending plays and pauses for the frame's commit (PendingAnimations).</summary>
    readonly List<UiAnimation> _pendingScript = [];
    long _scriptSequence;
    /// <summary>A script animation changed since the last frame (its values need one more evaluation).</summary>
    bool _scriptChanged;

    /// <summary>The frame's commit is under way: no game code runs (promises are settled by tasks).</summary>
    internal bool Committing { get; private set; }

    /// <summary>A new script animation of <paramref name="element"/> (idle until played).</summary>
    internal UiAnimation CreateScriptAnimation(UiDocument document, UiElement element, ScriptEffect effect)
    {
        var animation = new UiAnimation(document, element, effect, _scriptSequence++);
        _scripted.GetOrCreateValue(element).Add(animation);
        return animation;
    }

    /// <summary>A script animation's state changed: the timeline services it, its target is evaluated again; a pending one waits for the commit.</summary>
    internal void ScriptAnimationChanged(UiAnimation animation, bool pending)
    {
        if (!_scriptTimeline.Contains(animation))
        {
            var index = _scriptTimeline.FindIndex(a => a.Sequence > animation.Sequence);
            _scriptTimeline.Insert(index < 0 ? _scriptTimeline.Count : index, animation);
        }
        if (pending && !_pendingScript.Contains(animation))
            _pendingScript.Add(animation);
        if (_targets.TryGetValue((animation.Target, ""), out var target))
        {
            target.Stale = true;
            NeedsTick = true;
        }
        _scriptChanged = true;
    }

    /// <summary>
    /// The timeline's part of the frame's timing update (DocumentTimeline::ServiceAnimations): each
    /// script animation, in composite order, updates its finished state (a finish notification goes
    /// to a microtask); idle and finished ones leave the timeline until they change again.
    /// </summary>
    void ServiceScriptAnimations()
    {
        _scriptChanged = false;
        foreach (var animation in _scriptTimeline.ToList())
        {
            animation.UpdateForAnimationFrame();
            // Its time moved (or it just finished): its target is sampled again.
            if (_targets.TryGetValue((animation.Target, ""), out var target))
            {
                target.Stale = true;
                NeedsTick = true;
            }
            if (animation.PlayState is UiAnimationPlayState.Idle or UiAnimationPlayState.Finished)
                _scriptTimeline.Remove(animation);
        }
    }

    /// <summary>After the frame (PendingAnimations::Update): pending plays and pauses take effect at the frame's timeline time (ms).</summary>
    internal void CommitPending(double timelineTime)
    {
        if (_pendingScript.Count == 0)
            return;
        var pending = _pendingScript.ToList();
        _pendingScript.Clear();
        Committing = true;
        try
        {
            foreach (var animation in pending)
                if (animation.Pending)
                    animation.NotifyReady(timelineTime);
        }
        finally
        {
            Committing = false;
        }
    }

    /// <summary>
    /// One of a property's interpolation records (Blink's InterpolationEffect): it applies while the
    /// eased progress is in [ApplyFrom, ApplyTo) (the first and last reach out to infinity), and
    /// interpolates from <see cref="From"/> to <see cref="To"/> (null: the underlying value) over
    /// [Start, End] with the first keyframe's easing.
    /// </summary>
    internal sealed record ScriptRecord(double ApplyFrom, double ApplyTo, double Start, double End, TimingFunction Easing, ComputedStyle? From, ComputedStyle? To);

    /// <summary>Applies a script animation's effect on top of what's in <paramref name="visible"/> (composite replace).</summary>
    void ApplyScriptAnimation(Target target, UiAnimation animation, ComputedStyle visible, HashSet<string> now, HashSet<string> animatedNow)
    {
        var local = animation.PlayState == UiAnimationPlayState.Idle ? null : animation.CurrentTimeInternal();
        var (progress, _, before, _, _) = animation.Effect.Sample(local);
        if (progress is not { } fraction)
            return;
        if (!target.ScriptTracks.TryGetValue(animation, out var tracks))
            target.ScriptTracks[animation] = tracks = BuildScriptTracks(animation.Effect, target);
        // The underlying value (for neutral keyframes): what's there before this animation.
        ComputedStyle? underlying = null;
        foreach (var (property, records) in tracks)
        {
            if (target.Important.Contains(property))
                continue;
            foreach (var record in records)
            {
                if (!(fraction >= record.ApplyFrom && fraction < record.ApplyTo))
                    continue;
                var length = record.End - record.Start;
                var local01 = length != 0 ? (fraction - record.Start) / length : 0.0;
                var eased = record.Easing.Evaluate(local01, before);
                var from = record.From ?? (underlying ??= visible.Clone());
                var to = record.To ?? (underlying ??= visible.Clone());
                Interpolation.Apply(property, from, to, (float)eased, visible);
                now.Add(property);
                animatedNow.Add(property);
                break;
            }
        }
    }

    /// <summary>
    /// KeyframeEffectModel's keyframe groups and interpolation effect: per property its keyframes
    /// (neutral ones added at 0 and 1 when missing, the one at 0 with the easing of the last
    /// keyframe at offset 0), keyframes between two at the same offset dropped, then a record per
    /// pair of neighbours.
    /// </summary>
    Dictionary<string, List<ScriptRecord>> BuildScriptTracks(ScriptEffect effect, Target target)
    {
        var styles = new Dictionary<ScriptEffect.EffectKeyframe, ComputedStyle>();
        ComputedStyle StyleOf(ScriptEffect.EffectKeyframe keyframe)
        {
            if (styles.TryGetValue(keyframe, out var style))
                return style;
            style = target.Base.Clone();
            foreach (var declaration in keyframe.Declarations)
                _styles.ApplyDeclaration(style, target.Parent, declaration, target.RootFontSize);
            style.Finish();
            styles[keyframe] = style;
            return style;
        }
        var zeroOffsetEasing = TimingFunction.Linear;
        var groups = new Dictionary<string, List<(double Offset, TimingFunction Easing, ComputedStyle? Style)>>(StringComparer.Ordinal);
        foreach (var keyframe in effect.Keyframes)
        {
            if (keyframe.Offset == 0)
                zeroOffsetEasing = keyframe.Easing;
            if (!double.IsFinite(keyframe.Offset))
                continue;
            // A keyframe has one value per property (a shorthand and its longhand set it once).
            foreach (var property in keyframe.Declarations.Select(d => d.Property).Distinct())
            {
                if (property.StartsWith("--", StringComparison.Ordinal))
                    continue;
                if (!groups.TryGetValue(property, out var group))
                    groups[property] = group = [];
                group.Add((keyframe.Offset, keyframe.Easing, StyleOf(keyframe)));
            }
        }
        var tracks = new Dictionary<string, List<ScriptRecord>>(StringComparer.Ordinal);
        foreach (var (property, keyframes) in groups)
        {
            if (keyframes[0].Offset > 0)
                keyframes.Insert(0, (0, zeroOffsetEasing, null));
            if (keyframes[^1].Offset < 1)
                keyframes.Add((1, TimingFunction.Linear, null));
            for (var i = keyframes.Count - 2; i > 0; i--)
                if (keyframes[i - 1].Offset == keyframes[i].Offset && keyframes[i + 1].Offset == keyframes[i].Offset)
                    keyframes.RemoveAt(i);
            var records = new List<ScriptRecord>();
            for (var i = 0; i < keyframes.Count - 1; i++)
            {
                int start = i, end = i + 1;
                var applyFrom = keyframes[start].Offset;
                var applyTo = keyframes[end].Offset;
                if (i == 0)
                {
                    applyFrom = double.NegativeInfinity;
                    if (keyframes[end].Offset == 0)
                        end = start;
                }
                if (i == keyframes.Count - 2)
                {
                    applyTo = double.PositiveInfinity;
                    if (keyframes[start].Offset == 1)
                        start = end;
                }
                if (applyFrom != applyTo)
                    records.Add(new ScriptRecord(applyFrom, applyTo, keyframes[start].Offset, keyframes[end].Offset, keyframes[start].Easing, keyframes[start].Style, keyframes[end].Style));
            }
            tracks[property] = records;
        }
        return tracks;
    }

    // ---------- Values ----------

    /// <summary>A transition's eased progress now (its start value during its delay); null after its end.</summary>
    double? TransitionProgress(RunningTransition transition)
    {
        var elapsed = Time - transition.Start - transition.Delay;
        if (elapsed < 0)
            return 0;
        if (transition.Duration <= 0 || elapsed >= transition.Duration)
            return null;
        return transition.Timing.Apply((float)(elapsed / transition.Duration));
    }

    /// <summary>Resets what was animated to the base values, then applies transitions and animations.</summary>
    void Evaluate(Target target, ComputedStyle visible, out HashSet<string> written)
    {
        foreach (var property in target.Written)
            Properties.Get(property)?.Copy(visible, target.VisibleBase ?? target.Base);
        written = new HashSet<string>(target.Written, StringComparer.Ordinal);
        var now = new HashSet<string>(StringComparer.Ordinal);
        var animatedNow = new HashSet<string>(StringComparer.Ordinal);
        target.Stale = false;

        // A transition past its end shows nothing (it leaves once a frame's timing update saw it end).
        foreach (var (property, transition) in target.Transitions)
        {
            // (Custom properties are in the visible base already: CustomOverrides.)
            if (IsCustom(property) || target.Important.Contains(property) || TransitionProgress(transition) is not { } t)
                continue;
            Interpolation.Apply(property, transition.From, transition.To, (float)t, visible);
            now.Add(property);
        }

        foreach (var animation in target.Animations)
        {
            var progress = Progress(animation, out var finished);
            animation.Finished = finished;
            if (progress is not { } p)
                continue;
            animation.Tracks ??= BuildTracks(animation, target);
            foreach (var (property, frames) in animation.Tracks)
            {
                if (target.Important.Contains(property))
                    continue;
                var i = 0;
                while (i < frames.Count - 2 && p > frames[i + 1].Offset)
                    i++;
                var a = frames[i];
                var b = frames[Math.Min(i + 1, frames.Count - 1)];
                var span = b.Offset - a.Offset;
                var local = span > 0 ? Math.Clamp((p - a.Offset) / span, 0, 1) : 1;
                var eased = (a.Timing ?? animation.Spec.Timing).Apply(local);
                Interpolation.Apply(property, a.Style, b.Style, eased, visible);
                now.Add(property);
                animatedNow.Add(property);
            }
        }
        if (target.Pseudo.Length == 0 && _scripted.TryGetValue(target.Element, out var scripted))
            foreach (var animation in scripted)
                ApplyScriptAnimation(target, animation, visible, now, animatedNow);

        foreach (var property in now)
            written.Add(property);
        target.Written = now;
        target.AnimationWritten = animatedNow;
        visible.Finish();
    }

    /// <summary>The iteration progress (0..1, direction applied) at the current time; null when the animation has no effect.</summary>
    float? Progress(RunningAnimation animation, out bool finished)
    {
        finished = false;
        var spec = animation.Spec;
        var time = animation.PausedAt ?? NowOf(animation);
        var elapsed = time - animation.Start - spec.Delay;
        var duration = Math.Max(0, spec.Duration);
        var iterations = Math.Max(0, spec.IterationCount);
        var active = duration * iterations;
        double overall;
        if (elapsed < 0)
        {
            if (spec.FillMode is not (AnimationFillMode.Backwards or AnimationFillMode.Both))
                return null;
            overall = 0;
        }
        else if (elapsed >= active && !double.IsInfinity(iterations))
        {
            finished = true;
            if (spec.FillMode is not (AnimationFillMode.Forwards or AnimationFillMode.Both))
                return null;
            overall = iterations;
        }
        else
            overall = duration > 0 ? elapsed / duration : iterations;
        // The iteration and its progress; an animation ending on a whole iteration shows that iteration's end.
        var iteration = Math.Floor(overall);
        var progress = overall - iteration;
        if (progress == 0 && overall > 0 && (finished || duration <= 0))
        {
            iteration -= 1;
            progress = 1;
        }
        var reversed = spec.Direction switch
        {
            AnimationDirection.Reverse => true,
            AnimationDirection.Alternate => iteration % 2 == 1,
            AnimationDirection.AlternateReverse => iteration % 2 == 0,
            _ => false,
        };
        return (float)(reversed ? 1 - progress : progress);
    }

    /// <summary>Per animated property: its keyframes' values, with the base style standing in for missing 0% and 100%.</summary>
    Dictionary<string, List<Frame>> BuildTracks(RunningAnimation animation, Target target)
    {
        var styles = new Dictionary<KeyframesDefinition.Frame, ComputedStyle>();
        ComputedStyle StyleOf(KeyframesDefinition.Frame frame)
        {
            if (styles.TryGetValue(frame, out var style))
                return style;
            style = target.Base.Clone();
            foreach (var declaration in frame.Declarations)
                _styles.ApplyDeclaration(style, target.Parent, declaration, target.RootFontSize);
            style.Finish();
            styles[frame] = style;
            return style;
        }
        var tracks = new Dictionary<string, List<Frame>>(StringComparer.Ordinal);
        foreach (var property in animation.Keyframes.Properties)
        {
            if (property.StartsWith("--", StringComparison.Ordinal))
                continue;
            var frames = new List<Frame>();
            foreach (var frame in animation.Keyframes.Frames)
                if (frame.Declarations.Any(d => d.Property == property))
                    frames.Add(new Frame { Offset = frame.Offset, Style = StyleOf(frame), Timing = frame.Timing });
            if (frames.Count == 0)
                continue;
            if (frames[0].Offset > 0)
                frames.Insert(0, new Frame { Offset = 0, Style = target.Base });
            if (frames[^1].Offset < 1)
                frames.Add(new Frame { Offset = 1, Style = target.Base });
            tracks[property] = frames;
        }
        return tracks;
    }
}

/// <summary>
/// A playing sequence's clock for the CSS animations inside its UI hosts (Docs/Scene.md, S6-D): the
/// document's time less the time it was held, held while the sequence's clock stands still (its player
/// holds and releases it at the document times that keep the two in step). Seeking doesn't reach it (Q29).
/// </summary>
internal sealed class AnimationTimeline(object owner)
{
    double _offset;
    double? _heldAt;

    /// <summary>Whose timeline it is (a sequence's player).</summary>
    public object Owner { get; } = owner;

    /// <summary>The elements whose subtrees run on it.</summary>
    public List<UiElement> Roots { get; } = [];

    public bool Held => _heldAt is not null;

    /// <summary>Its time at the document's time <paramref name="time"/>.</summary>
    public double Now(double time) => _heldAt ?? time - _offset;

    /// <summary>Stands still from the document's time <paramref name="at"/> (held already: from the earlier of the two; nothing was drawn in between).</summary>
    public void Hold(double at)
    {
        var value = at - _offset;
        _heldAt = _heldAt is { } held ? Math.Min(held, value) : value;
    }

    /// <summary>Goes on from the document's time <paramref name="at"/>, where it stood.</summary>
    public void Release(double at)
    {
        if (_heldAt is not { } held)
            return;
        _offset = Math.Max(_offset, at - held);
        _heldAt = null;
    }
}
