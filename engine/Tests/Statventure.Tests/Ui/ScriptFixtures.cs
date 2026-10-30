using Statventure.UI.Dom;
using static Statventure.Tests.Ui.ScriptContext;

namespace Statventure.Tests.Ui;

/// <summary>
/// What a C# scenario runs with: its document, the log (each line tagged with the frame, as the
/// JavaScript twin's), the frame number and time, and what the harness does for the JavaScript
/// twin (a real click, a window resize).
/// </summary>
public sealed class ScriptContext(UiDocument document)
{
    public UiDocument Document { get; } = document;
    public List<string> Lines { get; } = [];
    public int Frame { get; internal set; }
    /// <summary>The time of the last frame (ms), as the JavaScript twin's __frameTime.</summary>
    public double FrameTime { get; internal set; }

    public void Log(string text) => Lines.Add($"f{Frame} {text}");

    /// <summary>
    /// promise.then(fulfilled, rejected) for a task: a microtask once it settles (at once when it
    /// already has), as a promise reaction (an await of a completed task wouldn't wait); rejected
    /// gets the error's name (a cancelled task is an AbortError).
    /// </summary>
    public static void Then<T>(Task<T> task, Action<T> fulfilled, Action<string>? rejected = null) =>
        task.ContinueWith(t =>
        {
            if (t.IsCanceled)
                rejected?.Invoke("AbortError");
            else if (t.IsFaulted)
                rejected?.Invoke(t.Exception!.InnerException!.GetType().Name);
            else
                fulfilled(t.Result);
        }, TaskScheduler.FromCurrentSynchronizationContext());

    /// <summary>Math.round(ms / frame): whole 60 Hz frames (Chromium's frame times jitter by a fraction of a millisecond).</summary>
    public static string Frames(double? time) => time is { } t ? Js(Math.Floor(t / (1000.0 / 60) + 0.5)) : "null";

    /// <summary>Number.prototype.toFixed.</summary>
    public static string Fixed(double value, int digits) => value.ToString("F" + digits, System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A number as JavaScript writes it (shortest round trip).</summary>
    public static string Js(double value) => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    public UiElement Get(string id) => Document.GetElementById(id)!;

    /// <summary>What the scenario asked the harness for, done after the frame that runs now (as the browser process answers between frames).</summary>
    internal List<Action> Requests { get; } = [];

    /// <summary>A real click at window coordinates ("__click x y").</summary>
    public void Click(float x, float y) => Requests.Add(() =>
    {
        var at = new System.Numerics.Vector2(x, y);
        Document.Input.PointerMove(at);
        Document.Input.PointerDown(at, Statventure.Platform.MouseButton.Left);
        Document.Input.PointerUp(at, Statventure.Platform.MouseButton.Left);
    });

    /// <summary>The window resized ("__resize w h").</summary>
    public void Resize(float width, float height) => Requests.Add(() => Document.SetViewport(width, height));
}

/// <summary>
/// A script scenario run in Chromium 152 (<see cref="Script"/>, by Tools/ScriptReference through
/// Tools/Scripts/make-script-references.sh) and in the engine (<see cref="Run"/>, its C# twin):
/// both log the order their callbacks ran in, tagged with frames, and the logs must be the same.
/// </summary>
public sealed record ScriptFixture(string Name, int Width, int Height, string Css, string Html, int Frames, string Script, Action<ScriptContext> Run);

public static class ScriptFixtures
{
    // (A property: static fields initialize in order, and Core comes after.)
    public static ScriptFixture[] All => s_all ??= [.. Core, .. AnimationScriptFixtures.All];

    static ScriptFixture[]? s_all;

    static readonly ScriptFixture[] Core =
    [
        // Microtasks after each task and callback, promise reactions and async continuations in
        // order, 0 ms timers before the next frame, frame callbacks requested in a frame callback
        // for the next frame.
        new("microtasks", 400, 300, "", "", 3, """
            log("task");
            queueMicrotask(() => log("micro1"));
            Promise.resolve().then(() => { log("then1"); queueMicrotask(() => log("micro-nested")); });
            setTimeout(() => { log("timeout0"); queueMicrotask(() => log("micro-in-timeout")); }, 0);
            setTimeout(() => log("timeout0b"), 0);
            requestAnimationFrame(() => { log("raf"); setTimeout(() => log("timeout-from-raf"), 0); requestAnimationFrame(() => log("raf2")); Promise.resolve().then(() => log("micro-in-raf")); });
            requestAnimationFrame(() => log("raf-b"));
            (async () => { log("async-start"); await null; log("after-await1"); await null; log("after-await2"); })();
            log("task-end");
            """, s =>
        {
            var d = s.Document;
            s.Log("task");
            d.QueueMicrotask(() => s.Log("micro1"));
            Task.CompletedTask.ContinueWith(_ => { s.Log("then1"); d.QueueMicrotask(() => s.Log("micro-nested")); }, TaskScheduler.FromCurrentSynchronizationContext());
            d.SetTimeout(() => { s.Log("timeout0"); d.QueueMicrotask(() => s.Log("micro-in-timeout")); });
            d.SetTimeout(() => s.Log("timeout0b"));
            d.RequestAnimationFrame(_ =>
            {
                s.Log("raf");
                d.SetTimeout(() => s.Log("timeout-from-raf"));
                d.RequestAnimationFrame(_ => s.Log("raf2"));
                d.QueueMicrotask(() => s.Log("micro-in-raf"));
            });
            d.RequestAnimationFrame(_ => s.Log("raf-b"));
            Async(s);
            s.Log("task-end");

            static async void Async(ScriptContext s)
            {
                s.Log("async-start");
                await Yield();
                s.Log("after-await1");
                await Yield();
                s.Log("after-await2");
            }
        }),
        // A native click runs the microtasks after each listener; a script's click after all of them.
        new("listeners", 400, 300, "#b { position: absolute; left: 0; top: 0; width: 60px; height: 40px; }", """<button id="b">x</button>""", 4, """
            const b = document.getElementById("b");
            b.addEventListener("click", () => { log("L1"); queueMicrotask(() => log("M1")); });
            b.addEventListener("click", () => { log("L2"); queueMicrotask(() => log("M2")); });
            log("script click"); b.click(); log("after script click");
            requestAnimationFrame(() => { requestAnimationFrame(() => { log("native click request"); console.log("__click 20 20"); }); });
            """, s =>
        {
            var d = s.Document;
            var b = s.Get("b");
            b.AddEventListener("click", _ => { s.Log("L1"); d.QueueMicrotask(() => s.Log("M1")); });
            b.AddEventListener("click", _ => { s.Log("L2"); d.QueueMicrotask(() => s.Log("M2")); });
            s.Log("script click");
            b.Click();
            s.Log("after script click");
            d.RequestAnimationFrame(_ => d.RequestAnimationFrame(_ => { s.Log("native click request"); s.Click(20, 20); }));
        }),
        // An interval until it's cleared, a cleared timeout, cancelled frame callbacks (one cancelled
        // by an earlier callback of the same frame).
        new("timers", 400, 300, "", "", 3, """
            let n = 0;
            const id = setInterval(() => { n++; log("interval " + n); if (n === 3) clearInterval(id); }, 0);
            const t = setTimeout(() => log("cleared timeout"), 0); clearTimeout(t);
            log("ids " + (t > id));
            const r = requestAnimationFrame(() => log("cancelled raf")); cancelAnimationFrame(r);
            let second = 0;
            requestAnimationFrame(() => { log("first"); cancelAnimationFrame(second); });
            second = requestAnimationFrame(() => log("second (cancelled)"));
            requestAnimationFrame(time => log("third " + (time === __frameTime)));
            """, s =>
        {
            var d = s.Document;
            var n = 0;
            var id = 0;
            id = d.SetInterval(() => { n++; s.Log("interval " + n); if (n == 3) d.ClearInterval(id); });
            var t = d.SetTimeout(() => s.Log("cleared timeout"));
            d.ClearTimeout(t);
            s.Log("ids " + (t > id).ToString().ToLowerInvariant());
            var r = d.RequestAnimationFrame(_ => s.Log("cancelled raf"));
            d.CancelAnimationFrame(r);
            var second = 0;
            d.RequestAnimationFrame(_ => { s.Log("first"); d.CancelAnimationFrame(second); });
            second = d.RequestAnimationFrame(_ => s.Log("second (cancelled)"));
            d.RequestAnimationFrame(time => s.Log("third " + (time == s.FrameTime).ToString().ToLowerInvariant()));
        }),
        // A window resize: the resize event, then the media query list's change, then frame callbacks.
        new("matchmedia", 400, 300, "", "", 6, """
            const mql = matchMedia("(max-width: 350px)");
            log("matches " + mql.matches);
            mql.addEventListener("change", e => log("change " + e.matches + " " + e.media));
            window.addEventListener("resize", () => log("resize " + innerWidth));
            requestAnimationFrame(() => requestAnimationFrame(() => { log("request resize"); console.log("__resize 300 300"); }));
            const tick = () => { log("raf"); if (__frame < 5) requestAnimationFrame(tick); }; requestAnimationFrame(tick);
            """, s =>
        {
            var d = s.Document;
            var mql = d.MatchMedia("(max-width: 350px)");
            s.Log("matches " + mql.Matches.ToString().ToLowerInvariant());
            mql.AddChangeListener(e => s.Log("change " + e.Matches.ToString().ToLowerInvariant() + " " + e.Media));
            d.AddEventListener("resize", _ => s.Log("resize " + d.Styles.Environment.Width));
            d.RequestAnimationFrame(_ => d.RequestAnimationFrame(_ => { s.Log("request resize"); s.Resize(300, 300); }));
            void Tick(double _)
            {
                s.Log("raf");
                if (s.Frame < 5)
                    d.RequestAnimationFrame(Tick);
            }
            d.RequestAnimationFrame(Tick);
        }),
        // A frame's steps: scroll events, animation events (with their microtasks), frame callbacks.
        new("frame-order", 400, 300, """
            #s { height: 100px; overflow: auto; } #s div { height: 400px; }
            @keyframes k { from { opacity: 0; } to { opacity: 1; } }
            #a { width: 50px; height: 20px; } .go { animation: k 1s; }
            """, """<div id="s"><div></div></div><div id="a"></div>""", 3, """
            const s = document.getElementById("s"), a = document.getElementById("a");
            s.addEventListener("scroll", () => log("scroll"));
            document.addEventListener("animationstart", () => { log("animationstart"); queueMicrotask(() => log("micro-after-animationstart")); });
            requestAnimationFrame(() => {
              log("raf A");
              s.scrollTop = 50;
              a.classList.add("go");
              requestAnimationFrame(() => log("raf B"));
              Promise.resolve().then(() => log("micro-in-raf A"));
            });
            """, s =>
        {
            var d = s.Document;
            var scroller = s.Get("s");
            var a = s.Get("a");
            scroller.AddEventListener("scroll", _ => s.Log("scroll"));
            d.AddEventListener("animationstart", _ => { s.Log("animationstart"); d.QueueMicrotask(() => s.Log("micro-after-animationstart")); });
            d.RequestAnimationFrame(_ =>
            {
                s.Log("raf A");
                scroller.ScrollTop = 50;
                a.AddClass("go");
                d.RequestAnimationFrame(_ => s.Log("raf B"));
                d.QueueMicrotask(() => s.Log("micro-in-raf A"));
            });
        }),
        // Resize observers: the first delivery reports every target (0×0 too) after the frame's
        // callbacks, observers in the order they were made, entries in observation order, a
        // microtask checkpoint after each callback, the boxes' sizes (a fractional paint offset
        // for the device pixel size), changes and unobserve.
        new("resize-observer", 400, 300, """
            #a { width: 100.3px; height: 50px; padding: 5px 7px; border: 2px solid; margin-left: 0.4px; }
            #b { width: 30.3px; height: 20px; }
            #h { display: none; width: 10px; }
            """, """<div id="a"></div><div id="b"></div><div id="h"></div><span id="s">text</span>""", 5, """
            const a = document.getElementById("a"), b = document.getElementById("b"), h = document.getElementById("h"), s = document.getElementById("s");
            const describe = e => e.target.id + " " + e.contentRect.x + "," + e.contentRect.y + " " + e.contentRect.width + "x" + e.contentRect.height
              + " content " + e.contentBoxSize[0].inlineSize + "x" + e.contentBoxSize[0].blockSize
              + " border " + e.borderBoxSize[0].inlineSize + "x" + e.borderBoxSize[0].blockSize
              + " dev " + e.devicePixelContentBoxSize[0].inlineSize + "x" + e.devicePixelContentBoxSize[0].blockSize;
            const first = new ResizeObserver(entries => { log("first " + entries.map(describe).join(" | ")); });
            const ro = new ResizeObserver((entries, observer) => { log("ro " + entries.map(describe).join(" | ") + " same " + (observer === ro)); queueMicrotask(() => log("micro after ro")); });
            ro.observe(a); ro.observe(h); ro.observe(s); ro.observe(b, { box: "border-box" });
            first.observe(b, { box: "device-pixel-content-box" });
            requestAnimationFrame(() => log("raf"));
            requestAnimationFrame(() => requestAnimationFrame(() => { log("resize a"); a.style.width = "120.5px"; }));
            requestAnimationFrame(() => requestAnimationFrame(() => requestAnimationFrame(() => { log("pad b, show h"); b.style.padding = "3px"; ro.unobserve(h); h.style.display = "block"; })));
            requestAnimationFrame(() => requestAnimationFrame(() => requestAnimationFrame(() => requestAnimationFrame(() => { log("same box again, disconnect first"); ro.observe(a); first.disconnect(); b.style.width = "40px"; }))));
            """, s =>
        {
            var d = s.Document;
            UiElement a = s.Get("a"), b = s.Get("b"), h = s.Get("h"), span = s.Get("s");
            static string Describe(UiResizeObserverEntry e) =>
                $"{e.Target.Id} {Js(e.ContentRect.X)},{Js(e.ContentRect.Y)} {Js(e.ContentRect.Width)}x{Js(e.ContentRect.Height)}"
                + $" content {Js(e.ContentBoxSize.InlineSize)}x{Js(e.ContentBoxSize.BlockSize)}"
                + $" border {Js(e.BorderBoxSize.InlineSize)}x{Js(e.BorderBoxSize.BlockSize)}"
                + $" dev {Js(e.DevicePixelContentBoxSize.InlineSize)}x{Js(e.DevicePixelContentBoxSize.BlockSize)}";
            var first = new UiResizeObserver(d, entries => s.Log("first " + string.Join(" | ", entries.Select(Describe))));
            UiResizeObserver? ro = null;
            ro = new UiResizeObserver(d, (entries, observer) =>
            {
                s.Log("ro " + string.Join(" | ", entries.Select(Describe)) + " same " + (observer == ro).ToString().ToLowerInvariant());
                d.QueueMicrotask(() => s.Log("micro after ro"));
            });
            ro.Observe(a);
            ro.Observe(h);
            ro.Observe(span);
            ro.Observe(b, ResizeObserverBox.BorderBox);
            first.Observe(b, ResizeObserverBox.DevicePixelContentBox);
            d.RequestAnimationFrame(_ => s.Log("raf"));
            d.RequestAnimationFrame(_ => d.RequestAnimationFrame(_ => { s.Log("resize a"); a.SetStyle("width", "120.5px"); }));
            d.RequestAnimationFrame(_ => d.RequestAnimationFrame(_ => d.RequestAnimationFrame(_ =>
            {
                s.Log("pad b, show h");
                b.SetStyle("padding", "3px");
                ro.Unobserve(h);
                h.SetStyle("display", "block");
            })));
            d.RequestAnimationFrame(_ => d.RequestAnimationFrame(_ => d.RequestAnimationFrame(_ => d.RequestAnimationFrame(_ =>
            {
                s.Log("same box again, disconnect first");
                ro.Observe(a);
                first.Disconnect();
                b.SetStyle("width", "40px");
            }))));
        }),
        // The depth rule: each round in a frame reports only targets deeper than the shallowest of
        // the round before; the shallower ones left give the window an error event and are reported
        // in the next frame.
        new("resize-observer-depth", 400, 300, """
            #outer { width: 100px; } #inner { width: 80px; } #deep { width: 60px; height: 10px; }
            """, """<div id="outer"><div id="inner"><div id="deep"></div></div></div>""", 4, """
            const outer = document.getElementById("outer"), inner = document.getElementById("inner"), deep = document.getElementById("deep");
            window.addEventListener("error", e => log("error " + e.message));
            let step = 0;
            const ro = new ResizeObserver(entries => {
              step++;
              log("ro " + step + " " + entries.map(e => e.target.id + "=" + e.contentRect.width).join(" "));
              if (step === 1) deep.style.width = "20px";
              if (step === 2) { outer.style.width = "300px"; inner.style.width = "200px"; deep.style.width = "30px"; }
            });
            ro.observe(outer); ro.observe(inner); ro.observe(deep);
            requestAnimationFrame(() => log("raf"));
            const tick = () => { log("tick"); if (__frame < 3) requestAnimationFrame(tick); }; requestAnimationFrame(tick);
            """, s =>
        {
            var d = s.Document;
            UiElement outer = s.Get("outer"), inner = s.Get("inner"), deep = s.Get("deep");
            d.AddEventListener<UiErrorEvent>("error", e => s.Log("error " + e.Message));
            var step = 0;
            var ro = new UiResizeObserver(d, entries =>
            {
                step++;
                s.Log($"ro {step} " + string.Join(" ", entries.Select(e => e.Target.Id + "=" + Js(e.ContentRect.Width))));
                if (step == 1)
                    deep.SetStyle("width", "20px");
                if (step == 2)
                {
                    outer.SetStyle("width", "300px");
                    inner.SetStyle("width", "200px");
                    deep.SetStyle("width", "30px");
                }
            });
            ro.Observe(outer);
            ro.Observe(inner);
            ro.Observe(deep);
            d.RequestAnimationFrame(_ => s.Log("raf"));
            void Tick(double _)
            {
                s.Log("tick");
                if (s.Frame < 3)
                    d.RequestAnimationFrame(Tick);
            }
            d.RequestAnimationFrame(Tick);
        }),
        // A window resize: resize event, media query change, frame callbacks, then resize observers.
        new("resize-observer-window", 400, 300, "#half { width: 50%; height: 10px; }", """<div id="half"></div>""", 5, """
            const half = document.getElementById("half");
            const mql = matchMedia("(max-width: 350px)");
            mql.addEventListener("change", e => log("change " + e.matches));
            window.addEventListener("resize", () => log("resize " + innerWidth + "x" + innerHeight));
            new ResizeObserver(entries => log("ro " + entries[0].contentRect.width)).observe(half);
            requestAnimationFrame(() => requestAnimationFrame(() => { log("request resize"); console.log("__resize 300 200"); }));
            const tick = () => { log("raf"); if (__frame < 4) requestAnimationFrame(tick); }; requestAnimationFrame(tick);
            """, s =>
        {
            var d = s.Document;
            var mql = d.MatchMedia("(max-width: 350px)");
            mql.AddChangeListener(e => s.Log("change " + e.Matches.ToString().ToLowerInvariant()));
            d.AddEventListener("resize", _ => s.Log($"resize {Js(d.Styles.Environment.Width)}x{Js(d.Styles.Environment.Height)}"));
            new UiResizeObserver(d, entries => s.Log("ro " + Js(entries[0].ContentRect.Width))).Observe(s.Get("half"));
            d.RequestAnimationFrame(_ => d.RequestAnimationFrame(_ => { s.Log("request resize"); s.Resize(300, 200); }));
            void Tick(double _)
            {
                s.Log("raf");
                if (s.Frame < 4)
                    d.RequestAnimationFrame(Tick);
            }
            d.RequestAnimationFrame(Tick);
        }),
    ];

    /// <summary>A microtask's worth of waiting, as `await null` in JavaScript (an awaited completed task wouldn't yield).</summary>
    public static System.Runtime.CompilerServices.YieldAwaitable Yield() => Task.Yield();
}
