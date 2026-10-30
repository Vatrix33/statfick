using Statventure.UI.Css;

namespace Statventure.UI.Dom;

/// <summary>
/// What game code schedules work with, as a page's window offers it in Chromium 152: timers,
/// microtasks and C# async/await on the document's event loop, frame callbacks
/// (requestAnimationFrame), media query lists (matchMedia) and the window's resize event, run at
/// their steps of the frame (PageAnimator::ServiceScriptedAnimations: the animation timing update
/// and a microtask checkpoint, resize events, scroll events, media query listeners, animation
/// events, frame callbacks; then style and layout and resize observers).
/// </summary>
public sealed partial class UiDocument
{
    /// <summary>The document's event loop (timers, microtasks, the SynchronizationContext game code runs in).</summary>
    public UiEventLoop EventLoop { get; } = new();

    /// <summary>window.setTimeout.</summary>
    public int SetTimeout(Action callback, double milliseconds = 0) => EventLoop.SetTimeout(callback, milliseconds);

    /// <summary>window.setInterval.</summary>
    public int SetInterval(Action callback, double milliseconds = 0) => EventLoop.SetInterval(callback, milliseconds);

    public void ClearTimeout(int id) => EventLoop.ClearTimeout(id);

    public void ClearInterval(int id) => EventLoop.ClearInterval(id);

    /// <summary>window.queueMicrotask.</summary>
    public void QueueMicrotask(Action microtask) => EventLoop.QueueMicrotask(microtask);

    /// <summary>A task that completes after <paramref name="milliseconds"/> (await it as the game awaited its wait(ms)).</summary>
    public Task Delay(double milliseconds) => EventLoop.Delay(milliseconds);

    /// <summary>performance.now(): milliseconds on the document's clock.</summary>
    public double PerformanceNow => EventLoop.Now;

    /// <summary>Runs <paramref name="script"/> as a task now (game code started from outside the loop: setup, the host).</summary>
    public void RunScript(Action script) => EventLoop.RunTask(script);

    // ---------- The animation clock ----------

    double _animationFrameTime;
    bool _inRendering;

    /// <summary>
    /// The document timeline's time (ms; Blink's AnimationClock): during a frame's rendering steps
    /// the frame's time; in a task between frames the last frame's time advanced by whole 1/60 s
    /// steps up to the task's time.
    /// </summary>
    internal double TimelineTime
    {
        get
        {
            if (_inRendering)
                return _animationFrameTime * 1000;
            var frames = Math.Floor((EventLoop.Time - _animationFrameTime) * 60 + 1e-6);
            return (_animationFrameTime + Math.Max(0, frames) / 60) * 1000;
        }
    }

    // ---------- Frame callbacks ----------

    sealed class FrameCallback
    {
        public required int Id;
        public required Action<double> Callback;
        public bool Cancelled;
    }

    List<FrameCallback> _frameCallbacks = [];
    List<FrameCallback> _frameCallbacksToInvoke = [];
    int _lastFrameCallbackId;

    /// <summary>window.requestAnimationFrame: <paramref name="callback"/> runs in the next frame's animation frame step with the frame's time (ms).</summary>
    public int RequestAnimationFrame(Action<double> callback)
    {
        var id = ++_lastFrameCallbackId;
        _frameCallbacks.Add(new FrameCallback { Id = id, Callback = callback });
        return id;
    }

    /// <summary>window.cancelAnimationFrame (also one already taken for the frame that runs now).</summary>
    public void CancelAnimationFrame(int id)
    {
        var index = _frameCallbacks.FindIndex(c => c.Id == id);
        if (index >= 0)
        {
            _frameCallbacks.RemoveAt(index);
            return;
        }
        if (_frameCallbacksToInvoke.Find(c => c.Id == id) is { } taken)
            taken.Cancelled = true;
    }

    /// <summary>FrameRequestCallbackCollection::ExecuteFrameCallbacks: the callbacks requested before this frame, in order.</summary>
    void RunFrameCallbacks(double time)
    {
        if (_frameCallbacks.Count == 0)
            return;
        (_frameCallbacksToInvoke, _frameCallbacks) = (_frameCallbacks, _frameCallbacksToInvoke);
        try
        {
            foreach (var callback in _frameCallbacksToInvoke)
            {
                if (callback.Cancelled)
                    continue;
                EventLoop.EnterScript();
                try
                {
                    callback.Callback(time * 1000);
                }
                catch (Exception exception)
                {
                    Statventure.Core.Log.Error("UI", $"A frame callback failed: {exception}");
                }
                finally
                {
                    EventLoop.ExitScript();
                }
            }
        }
        finally
        {
            _frameCallbacksToInvoke.Clear();
        }
    }

    // ---------- Resize observers ----------

    // In the order they were made (ResizeObserverController's observers_); their targets keep them alive.
    readonly List<WeakReference<UiResizeObserver>> _resizeObservers = [];
    bool _resizeObservationsScheduled;

    internal void AddResizeObserver(UiResizeObserver observer) => _resizeObservers.Add(new WeakReference<UiResizeObserver>(observer));

    /// <summary>A new observation (or one left over by the loop limit) wants a frame.</summary>
    internal void ScheduleResizeObservations() => _resizeObservationsScheduled = true;

    List<UiResizeObserver> LiveResizeObservers()
    {
        var live = new List<UiResizeObserver>(_resizeObservers.Count);
        _resizeObservers.RemoveAll(w => !w.TryGetTarget(out _));
        foreach (var weak in _resizeObservers)
            if (weak.TryGetTarget(out var observer))
                live.Add(observer);
        return live;
    }

    /// <summary>
    /// The frame's resize observer steps after layout (LocalFrameView::NotifyResizeObservers in the
    /// lifecycle loop): each round gathers the targets whose size changed and that are deeper than
    /// the shallowest of the round before, calls back, and lays out again; when only shallower ones
    /// are left, the window gets an error event and they wait for the next frame.
    /// </summary>
    void RunResizeObservers(double time)
    {
        _resizeObservationsScheduled = false;
        if (_resizeObservers.Count == 0)
            return;
        var minDepth = 0;
        var errorDispatched = false;
        while (true)
        {
            var observers = LiveResizeObservers();
            var shallowest = UiResizeObserver.DepthBottom;
            foreach (var observer in observers)
                shallowest = Math.Min(shallowest, observer.GatherObservations(minDepth));
            minDepth = shallowest;
            if (shallowest != UiResizeObserver.DepthBottom)
            {
                foreach (var observer in observers)
                    observer.DeliverObservations();
                UpdateStyleAndLayout(time);
                continue;
            }
            if (!errorDispatched && observers.Any(o => o.SkippedObservations))
            {
                foreach (var observer in observers)
                    observer.ClearObservations();
                var error = new UiErrorEvent("ResizeObserver loop completed with undelivered notifications.") { IsTrusted = true };
                Dispatch(error, null, this);
                if (!error.DefaultPrevented)
                    Statventure.Core.Log.Warning("UI", error.Message);
                // Delivered in the next frame.
                _resizeObservationsScheduled = true;
                errorDispatched = true;
                if (NeedsStyleOrLayout)
                {
                    UpdateStyleAndLayout(time);
                    continue;
                }
            }
            break;
        }
    }

    /// <summary>An SVG element's bounding box (SVGGraphicsElement::GetBBox, user units), the svg's zoom and the paint offset its device pixel size snaps from (none: SVG children paint through transforms).</summary>
    internal (System.Numerics.Vector2 Size, float Zoom, System.Numerics.Vector2 Origin)? SvgBoundingBox(UiElement element)
    {
        for (var ancestor = element.Parent; ancestor is not null; ancestor = ancestor.Parent)
            if (ancestor.Tag == "svg" && ancestor.Box is { Type: Statventure.UI.Layout.BoxType.Replaced } box)
            {
                _hitPainter ??= new Paint.UiPainter(this);
                return _hitPainter.SvgElementGeometry(box, element) is { } geometry
                    ? (new System.Numerics.Vector2(geometry.Box.Width, geometry.Box.Height), box.Style.Zoom, System.Numerics.Vector2.Zero)
                    : null;
            }
        return null;
    }

    // ---------- Media query lists and resize ----------

    readonly List<WeakReference<UiMediaQueryList>> _mediaQueryLists = [];
    // Lists with listeners stay alive (Blink's MediaQueryList::HasPendingActivity), even when game code drops them.
    readonly HashSet<UiMediaQueryList> _listenedMediaQueryLists = [];

    internal void ListenersChanged(UiMediaQueryList list, bool any)
    {
        if (any)
            _listenedMediaQueryLists.Add(list);
        else
            _listenedMediaQueryLists.Remove(list);
    }

    /// <summary>window.matchMedia.</summary>
    public UiMediaQueryList MatchMedia(string query)
    {
        var list = new UiMediaQueryList(this, MediaQuery.Parse(CssParser.Trim(CssTokenizer.Tokenize(query))));
        _mediaQueryLists.Add(new WeakReference<UiMediaQueryList>(list));
        return list;
    }

    MediaEnvironment? _reportedEnvironment;
    (float Width, float Height)? _resizeReported;

    /// <summary>
    /// The frame's resize step: when the viewport's CSS size changed since the last frame, the
    /// window's resize event (at the document; LocalFrameView::SendResizeEventIfNeeded).
    /// </summary>
    void DispatchResizeEvent()
    {
        var size = (Styles.Environment.Width, Styles.Environment.Height);
        var previous = _resizeReported;
        _resizeReported = size;
        if (previous is null || previous == size)
            return;
        Dispatch(new UiEvent("resize", bubbles: false, cancelable: false) { IsTrusted = true }, null, this);
    }

    /// <summary>
    /// CallMediaQueryListListeners: every media query list whose result changed since it was last
    /// reported gets its change event, in the order they were made.
    /// </summary>
    void NotifyMediaQueryLists()
    {
        var environment = Styles.Environment;
        if (_reportedEnvironment == environment)
            return;
        _reportedEnvironment = environment;
        _mediaQueryLists.RemoveAll(w => !w.TryGetTarget(out _));
        foreach (var weak in _mediaQueryLists.ToArray())
            if (weak.TryGetTarget(out var list))
                list.Reevaluate(environment);
    }
}

/// <summary>A MediaQueryList: whether the document matches a media query now, with a change event when that flips.</summary>
public sealed class UiMediaQueryList
{
    readonly UiDocument _document;
    readonly MediaQuery _query;
    bool _matches;
    readonly List<Action<UiMediaQueryListEvent>> _listeners = [];

    internal UiMediaQueryList(UiDocument document, MediaQuery query)
    {
        _document = document;
        _query = query;
        _matches = query.Matches(document.Styles.Environment);
    }

    /// <summary>The query as written.</summary>
    public string Media => _query.Text;

    /// <summary>Whether the document matches it now.</summary>
    public bool Matches => _query.Matches(_document.Styles.Environment);

    /// <summary>addEventListener("change", …) (a listener is added once).</summary>
    public void AddChangeListener(Action<UiMediaQueryListEvent> listener)
    {
        if (!_listeners.Contains(listener))
            _listeners.Add(listener);
        _document.ListenersChanged(this, any: true);
    }

    public void RemoveChangeListener(Action<UiMediaQueryListEvent> listener)
    {
        _listeners.Remove(listener);
        _document.ListenersChanged(this, any: _listeners.Count > 0);
    }

    internal void Reevaluate(Css.MediaEnvironment environment)
    {
        var matches = _query.Matches(environment);
        if (matches == _matches)
            return;
        _matches = matches;
        var e = new UiMediaQueryListEvent(Media, matches);
        foreach (var listener in _listeners.ToArray())
        {
            _document.EventLoop.EnterScript();
            try
            {
                listener(e);
            }
            catch (Exception exception)
            {
                Statventure.Core.Log.Error("UI", $"A media query change listener failed: {exception}");
            }
            finally
            {
                _document.EventLoop.ExitScript();
            }
        }
    }
}

/// <summary>A MediaQueryListEvent: the query and whether it matches now.</summary>
public sealed class UiMediaQueryListEvent(string media, bool matches) : UiEvent("change", bubbles: false, cancelable: false)
{
    public string Media { get; } = media;
    public bool Matches { get; } = matches;
}

/// <summary>An ErrorEvent at the window (an error the engine reports, such as the resize observer loop limit).</summary>
public sealed class UiErrorEvent(string message) : UiEvent("error", bubbles: false, cancelable: true)
{
    public string Message { get; } = message;
}

public partial class UiElement
{
    /// <summary>This element's resize observations, by observer (they keep their observers alive).</summary>
    internal Dictionary<UiResizeObserver, UiResizeObserver.Observation>? ResizeObservations;
}
