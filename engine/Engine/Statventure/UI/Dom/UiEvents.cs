using System.Numerics;
using Statventure.Core;

namespace Statventure.UI.Dom;

public enum EventPhase
{
    None,
    Capturing,
    AtTarget,
    Bubbling,
}

/// <summary>
/// An event, as in the DOM: dispatched to a target element, it goes down through the target's
/// ancestors (capture), reaches the target, and goes back up (bubble) to the document.
/// </summary>
public class UiEvent
{
    public UiEvent(string type, bool bubbles = true, bool cancelable = true)
    {
        Type = type;
        Bubbles = bubbles;
        Cancelable = cancelable;
    }

    public string Type { get; }
    public bool Bubbles { get; }
    public bool Cancelable { get; }
    /// <summary>The element the event was dispatched to (null for document-level events).</summary>
    public UiElement? Target { get; internal set; }
    /// <summary>Whose listeners run now: an element, or the document.</summary>
    public object? CurrentTarget { get; internal set; }
    public EventPhase Phase { get; internal set; }
    public bool DefaultPrevented { get; private set; }
    /// <summary>Made by the engine from real input (not dispatched by the game).</summary>
    public bool IsTrusted { get; internal set; }
    /// <summary>Seconds, on the document's clock.</summary>
    public double TimeStamp { get; internal set; }

    internal bool PropagationStopped, ImmediatePropagationStopped;

    /// <summary>The engine already performed the event's default action (Blink's DefaultHandled): no further default handlers run.</summary>
    internal bool DefaultHandled { get; private set; }

    internal void SetDefaultHandled() => DefaultHandled = true;

    /// <summary>Cancels the default action (when <see cref="Cancelable"/>).</summary>
    public void PreventDefault()
    {
        if (Cancelable)
            DefaultPrevented = true;
    }

    /// <summary>No further elements get the event (the current one's other listeners still run).</summary>
    public void StopPropagation() => PropagationStopped = true;

    /// <summary>No further listeners get the event, not even the current element's.</summary>
    public void StopImmediatePropagation()
    {
        PropagationStopped = true;
        ImmediatePropagationStopped = true;
    }
}

/// <summary>Mouse events (mousedown, click, mouseover ...): positions in viewport units.</summary>
public class UiMouseEvent(string type, bool bubbles = true, bool cancelable = true) : UiEvent(type, bubbles, cancelable)
{
    /// <summary>The position in the viewport in CSS pixels, unrounded (Blink's client_x_, client_y_).</summary>
    public Vector2 Position { get; init; }
    /// <summary>DOM clientX: whole CSS pixels for mouse events (floored), as they are unrounded for pointer events.</summary>
    public virtual float ClientX => MathF.Floor(Position.X);
    public virtual float ClientY => MathF.Floor(Position.Y);
    /// <summary>The movement since the last move event, in whole window units.</summary>
    public Vector2 Movement { get; init; }
    /// <summary>The position in layout units (device pixels), for default actions (Blink's AbsoluteLocation).</summary>
    internal Vector2 LayoutPosition { get; init; }
    /// <summary>The button that changed: 0 main (left), 1 auxiliary (middle), 2 secondary (right), 3 back, 4 forward.</summary>
    public int Button { get; init; }
    /// <summary>The buttons held: 1 main, 2 secondary, 4 auxiliary, 8 back, 16 forward.</summary>
    public int Buttons { get; init; }
    public bool ShiftKey { get; init; }
    public bool CtrlKey { get; init; }
    public bool AltKey { get; init; }
    public bool MetaKey { get; init; }
    /// <summary>Clicks: how many in a row (2 on a double click's second click).</summary>
    public int Detail { get; init; }
    /// <summary>Over/out/enter/leave: the element the pointer comes from or goes to.</summary>
    public UiElement? RelatedTarget { get; init; }
}

/// <summary>Pointer events (pointerdown, pointermove ...): the mouse is pointer 1.</summary>
public class UiPointerEvent(string type, bool bubbles = true, bool cancelable = true) : UiMouseEvent(type, bubbles, cancelable)
{
    // Click, auxclick and contextmenu keep whole pixels (PointerEvent::ShouldHaveIntegerCoordinates).
    bool IntegerCoordinates => Type is "click" or "auxclick" or "contextmenu";
    public override float ClientX => IntegerCoordinates ? MathF.Floor(Position.X) : Position.X;
    public override float ClientY => IntegerCoordinates ? MathF.Floor(Position.Y) : Position.Y;
    public int PointerId { get; init; } = 1;
    public string PointerType { get; init; } = "mouse";
    public bool IsPrimary { get; init; } = true;
}

/// <summary>
/// A wheel turn or a touchpad scroll, in pixels. Only the first wheel event of a scroll is
/// cancelable once it wasn't cancelled (as in Chromium, the rest follow without waiting).
/// </summary>
public class UiWheelEvent(string type = "wheel", bool cancelable = true) : UiMouseEvent(type, true, cancelable)
{
    public float DeltaX { get; init; }
    public float DeltaY { get; init; }
    /// <summary>0: pixels (always, as Chromium reports wheels).</summary>
    public int DeltaMode { get; init; }
}

/// <summary>Keyboard events: <see cref="Key"/> is the DOM key value ("a", "A", "Enter", "ArrowLeft" ...), <see cref="Code"/> the physical key ("KeyA", "Enter" ...).</summary>
public class UiKeyboardEvent(string type, bool cancelable = true) : UiEvent(type, true, cancelable)
{
    public string Key { get; init; } = "";
    public string Code { get; init; } = "";
    public bool Repeat { get; init; }
    public bool ShiftKey { get; init; }
    public bool CtrlKey { get; init; }
    public bool AltKey { get; init; }
    public bool MetaKey { get; init; }
}

/// <summary>focus, blur (not bubbling), focusin, focusout (bubbling).</summary>
public class UiFocusEvent(string type, bool bubbles) : UiEvent(type, bubbles, cancelable: false)
{
    /// <summary>The element losing or gaining focus on the other side.</summary>
    public UiElement? RelatedTarget { get; init; }
}

/// <summary>beforeinput and input in editable fields.</summary>
public class UiInputEvent(string type, bool cancelable) : UiEvent(type, true, cancelable)
{
    /// <summary>The text inserted, if any.</summary>
    public string? Data { get; init; }
    /// <summary>insertText, deleteContentBackward, insertFromPaste ...</summary>
    public string InputType { get; init; } = "";
}

/// <summary>copy, cut and paste (cancelable): <see cref="Data"/> is the pasted text.</summary>
public class UiClipboardEvent(string type) : UiEvent(type, true, true)
{
    public string? Data { get; init; }
}

/// <summary>submit: the button that submitted the form (null for an implicit submission without a button, or requestSubmit()).</summary>
public class UiSubmitEvent(UiElement? submitter) : UiEvent("submit", true, true)
{
    public UiElement? Submitter { get; } = submitter;
}

/// <summary>toggle (details opening or closing): the states before and after, "open" or "closed".</summary>
public class UiToggleEvent(string oldState, string newState) : UiEvent("toggle", false, false)
{
    public string OldState { get; } = oldState;
    public string NewState { get; } = newState;
}

/// <summary>DOMActivate: a click's activation (buttons submit their forms, summaries toggle their details).</summary>
public class UiActivateEvent(UiEvent underlying, int detail, bool bubbles) : UiEvent("DOMActivate", bubbles, true)
{
    /// <summary>The click that activated.</summary>
    public UiEvent Underlying { get; } = underlying;
    public int Detail { get; } = detail;
}

/// <summary>animationstart, animationend, animationiteration.</summary>
public class UiAnimationEvent(string type) : UiEvent(type, true, false)
{
    public string AnimationName { get; init; } = "";
    public double ElapsedTime { get; init; }
    /// <summary>"" for the element, "::before" or "::after".</summary>
    public string PseudoElement { get; init; } = "";
}

/// <summary>transitionend and friends.</summary>
public class UiTransitionEvent(string type) : UiEvent(type, true, false)
{
    public string PropertyName { get; init; } = "";
    public double ElapsedTime { get; init; }
    public string PseudoElement { get; init; } = "";
}

/// <summary>The listeners of an element or the document, by event type and phase.</summary>
internal sealed class EventListeners
{
    readonly record struct Entry(Delegate Original, Action<UiEvent> Invoke, bool Capture);

    readonly Dictionary<string, List<Entry>> _byType = new(StringComparer.Ordinal);

    public bool Has(string type) => _byType.TryGetValue(type, out var list) && list.Count > 0;

    /// <summary>Adds a listener (a listener already there for that type and phase isn't added twice).</summary>
    public void Add(string type, Delegate original, Action<UiEvent> invoke, bool capture)
    {
        if (!_byType.TryGetValue(type, out var list))
            _byType[type] = list = [];
        foreach (var entry in list)
            if (entry.Original.Equals(original) && entry.Capture == capture)
                return;
        list.Add(new Entry(original, invoke, capture));
    }

    public void Remove(string type, Delegate original, bool capture)
    {
        if (_byType.TryGetValue(type, out var list))
            list.RemoveAll(e => e.Original.Equals(original) && e.Capture == capture);
    }

    /// <summary>
    /// Runs the listeners for the event's current phase (a copy: listeners added meanwhile wait for
    /// the next event). Each is game code the engine calls: when no other game code runs, the
    /// microtasks it queued run after it (as after each listener of a native event in a browser).
    /// </summary>
    public void Invoke(UiEvent e, UiEventLoop? loop)
    {
        if (!_byType.TryGetValue(e.Type, out var list) || list.Count == 0)
            return;
        foreach (var entry in list.ToArray())
        {
            if (e.ImmediatePropagationStopped)
                return;
            var phaseMatches = e.Phase == EventPhase.AtTarget || (e.Phase == EventPhase.Capturing) == entry.Capture;
            if (!phaseMatches || !list.Contains(entry))
                continue;
            loop?.EnterScript();
            try
            {
                entry.Invoke(e);
            }
            catch (Exception exception)
            {
                // As in browsers: a failing listener is reported, the others still run.
                Log.Error("UI", $"A '{e.Type}' listener failed: {exception}");
            }
            finally
            {
                loop?.ExitScript();
            }
        }
    }
}

/// <summary>pagehide and pageshow (PageTransitionEvent): <see cref="Persisted"/> when the page goes into or comes from the back-forward cache (never here).</summary>
public class UiPageTransitionEvent(string type, bool persisted) : UiEvent(type, true, true)
{
    public bool Persisted { get; } = persisted;
}
