namespace Statventure.UI.Dom;

/// <summary>
/// The page's visibility and its unloading, as Chromium 152 gives them to a page in its window
/// (Docs/Scripting.md, "Hidden documents"): a minimized window's document is hidden
/// (document.hidden, visibilityState, visibilitychange); a hidden document gets no frames, and its
/// timers are throttled as Blink's scheduler throttles a hidden page; closing the window unloads it
/// (pagehide, visibilitychange, unload).
/// </summary>
public sealed partial class UiDocument
{
    /// <summary>document.hidden: the window is minimized (or the document unloaded).</summary>
    public bool Hidden { get; private set; }

    /// <summary>document.visibilityState: "hidden" or "visible".</summary>
    public string VisibilityState => Hidden ? "hidden" : "visible";

    /// <summary>The document was unloaded (its window closed): no more events, timers or frames.</summary>
    public bool Unloaded { get; private set; }

    /// <summary>
    /// The time (seconds) of the document's first contentful paint: the first painted frame with
    /// text, an image, SVG or a surface (PaintTiming). Until then Blink's scheduler counts the page
    /// as loading, which lengthens the grace period of its intensive throttling.
    /// </summary>
    public double? FirstContentfulPaint { get; private set; }

    /// <summary>Called by the painter when a frame it painted had contentful content.</summary>
    internal void PaintedContent() => FirstContentfulPaint ??= EventLoop.Time;

    /// <summary>
    /// The window was minimized (true) or restored (Page::SetVisibilityState, then
    /// Document::DidChangeVisibilityState): the scheduler's state changes, then visibilitychange and
    /// webkitvisibilitychange at the document, bubbling, not cancelable. Hidden, the document gets no
    /// frames and its timers are throttled; shown, the next update is a frame again.
    /// </summary>
    public void SetHidden(bool hidden)
    {
        if (Unloaded || hidden == Hidden)
            return;
        Hidden = hidden;
        EventLoop.Throttling.SetHidden(hidden, UiTimerThrottling.Ticks(EventLoop.Time), loading: FirstContentfulPaint is null);
        DispatchVisibilityChange();
        if (!hidden)
            Repaint();
    }

    /// <summary>
    /// <see cref="SetHidden(bool)"/> happening at <paramref name="time"/> (seconds; the window system's
    /// moment, which a sleeping hidden document's clock may not have reached): shown, a hidden
    /// document first runs what its throttling allowed until then, so its timers late from the
    /// throttling run once when it's shown, not every beat they missed.
    /// </summary>
    public void SetHidden(bool hidden, double time)
    {
        if (!hidden && Hidden && !Unloaded && !_inFrameSteps && time > EventLoop.Time)
            UpdateHidden(time);
        SetHidden(hidden);
    }

    void DispatchVisibilityChange()
    {
        DispatchEvent(new UiEvent("visibilitychange", bubbles: true, cancelable: false) { IsTrusted = true });
        // The deprecated prefixed twin Blink still sends.
        DispatchEvent(new UiEvent("webkitvisibilitychange", bubbles: true, cancelable: false) { IsTrusted = true });
    }

    bool _audible;

    /// <summary>
    /// Whether the page's sound is audible (the host sets it from the sound output's level, as
    /// Chromium's audio service reports it): a hidden page isn't throttled while it is audible and
    /// for 30 s after it went silent.
    /// </summary>
    public bool Audible
    {
        get => _audible;
        set
        {
            if (value == _audible)
                return;
            _audible = value;
            EventLoop.Throttling.SetAudible(value, UiTimerThrottling.Ticks(EventLoop.Time));
        }
    }

    /// <summary>
    /// The window closes (Document::DispatchUnloadEvents, without beforeunload: the user's decision of
    /// 2026-11-03): pagehide (persisted false; at the window, which shares the document's listeners
    /// here), then, if the document was visible, it turns hidden with visibilitychange and
    /// webkitvisibilitychange, then unload. Its timers stop and it takes no more updates.
    /// </summary>
    public void Unload()
    {
        if (Unloaded)
            return;
        DispatchEvent(new UiPageTransitionEvent("pagehide", persisted: false) { IsTrusted = true });
        if (!Hidden)
        {
            Hidden = true;
            DispatchVisibilityChange();
        }
        DispatchEvent(new UiEvent("unload", bubbles: false, cancelable: false) { IsTrusted = true });
        Unloaded = true;
        EventLoop.ClearTimers();
    }

    /// <summary>
    /// The next time (seconds) a hidden document has work: a throttled timer allowed to run or a
    /// change of the throttling; null when none (the host may sleep until then, or until input).
    /// </summary>
    public double? NextWakeUp => Unloaded ? null : EventLoop.NextWakeUp;

    /// <summary>An update while hidden (no frame): the tasks and timers due by <paramref name="time"/>, throttled.</summary>
    void UpdateHidden(double time)
    {
        Animations.Advance(time);
        _inFrameSteps = true;
        try
        {
            EventLoop.RunUntil(time);
            RunTasks(late: false);
            RunTasks(late: true);
        }
        finally
        {
            _inFrameSteps = false;
        }
        EventLoop.PerformMicrotaskCheckpoint();
    }
}
