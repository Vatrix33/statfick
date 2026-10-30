using Statventure.UI.Paint;
using Statventure.UI.Style;

namespace Statventure.UI.Dom;

/// <summary>
/// The states of control parts that have no elements of their own (Blink's shadow elements): the
/// slider thumb, the spin button and the search cancel button hovered or pressed, which the
/// painter shows; and the animated value of styled progress bars.
/// </summary>
public sealed partial class UiDocument
{
    UiElement? _rangeThumbHovered, _rangeThumbActive, _spinButtonHovered, _spinButtonPressed, _cancelButtonPressed;
    bool _spinButtonUpHalf;

    internal UiElement? RangeThumbHovered { get => _rangeThumbHovered; set => SetPartState(ref _rangeThumbHovered, value); }
    internal UiElement? RangeThumbActive { get => _rangeThumbActive; set => SetPartState(ref _rangeThumbActive, value); }
    internal UiElement? SpinButtonHovered { get => _spinButtonHovered; set => SetPartState(ref _spinButtonHovered, value); }
    internal UiElement? SpinButtonPressed { get => _spinButtonPressed; set => SetPartState(ref _spinButtonPressed, value); }
    internal UiElement? CancelButtonPressed { get => _cancelButtonPressed; set => SetPartState(ref _cancelButtonPressed, value); }

    /// <summary>The spin button's hovered or pressed half is the upper one.</summary>
    internal bool SpinButtonUpHalf
    {
        get => _spinButtonUpHalf;
        set
        {
            if (_spinButtonUpHalf == value)
                return;
            _spinButtonUpHalf = value;
            Repaint();
        }
    }

    void SetPartState(ref UiElement? field, UiElement? value)
    {
        if (field == value)
            return;
        field = value;
        Repaint();
    }

    // ---------- Progress transitions ----------

    sealed class ProgressTransition
    {
        public float From, To;
        public double Start;
        public float Duration, Delay;
        public required TimingFunction Timing;

        public float At(double time)
        {
            var t = Duration <= 0 ? 1 : (float)Math.Clamp((time - Start - Delay) / Duration, 0, 1);
            if (time < Start + Delay)
                return From;
            return From + (To - From) * Timing.Apply(t);
        }

        public bool Done(double time) => time >= Start + Delay + Duration;
    }

    readonly Dictionary<UiElement, ProgressTransition> _progressTransitions = [];

    /// <summary>
    /// The share of a styled progress bar its value shows now: Blink sets the value part's width
    /// in percent, so a transition on width (or all) in ::-webkit-progress-value animates it.
    /// </summary>
    internal float DisplayedProgress(UiElement element, ComputedStyle valueStyle)
    {
        var target = UiPainter.ProgressPosition(element);
        TransitionSpec? spec = null;
        foreach (var transition in valueStyle.Transitions)
            if (transition.Property is "width" or "all" or "inline-size")
                spec = transition;
        if (target < 0 || spec is null || spec.Duration + spec.Delay <= 0)
        {
            _progressTransitions.Remove(element);
            return target;
        }
        var now = Animations.Time;
        if (!_progressTransitions.TryGetValue(element, out var running))
        {
            _progressTransitions[element] = new ProgressTransition { From = target, To = target, Start = now, Timing = spec.Timing };
            return target;
        }
        if (running.To != target)
        {
            running.From = running.At(now);
            running.To = target;
            running.Start = now;
            running.Duration = spec.Duration;
            running.Delay = spec.Delay;
            running.Timing = spec.Timing;
        }
        return running.At(now);
    }

    /// <summary>A progress transition is still running (the next frame differs).</summary>
    bool ProgressAnimating
    {
        get
        {
            var now = Animations.Time;
            foreach (var (element, transition) in _progressTransitions.ToList())
            {
                if (element.Document != this)
                {
                    _progressTransitions.Remove(element);
                    continue;
                }
                if (!transition.Done(now))
                    return true;
            }
            return false;
        }
    }
}
