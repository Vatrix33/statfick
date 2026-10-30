using System.Numerics;
using Statventure.Core;
using Statventure.UI.Dom;
using Statventure.UI.Paint;
using static Statventure.Tests.Ui.ScriptContext;

namespace Statventure.Tests.Ui;

/// <summary>
/// Web Animations (element.animate) scenarios for <see cref="ScriptReferenceTests"/>. Chromium's
/// frame times jitter by a fraction of a millisecond, so playback logs whole frames and values to
/// two decimals; effects are sampled exactly by pausing and seeking (currentTime) and reading the
/// computed style. Durations keep frame boundaries and rounding edges at least half a millisecond
/// away (119 ms: every frame's opacity is 0.55 ms from a 2-decimal edge; 40 ms ends between frames).
/// </summary>
public static class AnimationScriptFixtures
{
    static Keyframe[] TranslateX(double from, double to) =>
    [
        new() { ["transform"] = $"translateX({Js(from)}px)" },
        new() { ["transform"] = $"translateX({Js(to)}px)" },
    ];

    public static readonly ScriptFixture[] All =
    [
        // Playback: pending until the frame after the call (its start time is that frame's), ready
        // in a task after it, values per frame, Finished before the finish event, a cancel's
        // AbortError and cancel event.
        new("animate-playback", 400, 300, "#a, #b { width: 50px; height: 20px; }", """<div id="a"></div><div id="b"></div>""", 12, """
            const a = document.getElementById("a"), b = document.getElementById("b");
            const frames = t => t === null ? "null" : String(Math.round(t / (1000 / 60)));
            const anim = a.animate([{ opacity: 0 }, { opacity: 1 }], { duration: 119, fill: "both" });
            log("animate " + anim.playState + " pending " + anim.pending + " start " + anim.startTime + " current " + anim.currentTime + " same " + (anim.finished === anim.finished));
            anim.ready.then(() => log("ready " + anim.playState + " pending " + anim.pending + " start at frame " + (Math.abs(anim.startTime - __frameTime) < 1) + " current " + frames(anim.currentTime)));
            anim.finished.then(() => log("finished " + anim.playState + " current " + anim.currentTime + " opacity " + Number(getComputedStyle(a).opacity).toFixed(2)));
            anim.onfinish = e => log("onfinish current " + e.currentTime + " timeline " + frames(e.timelineTime - __frameTime));
            const tick = () => { log("raf " + anim.playState + " " + frames(anim.currentTime) + " opacity " + Number(getComputedStyle(a).opacity).toFixed(2)); if (__frame < 11) requestAnimationFrame(tick); };
            requestAnimationFrame(tick);
            const anim2 = b.animate([{ transform: "translateX(0)" }, { transform: "translateX(10px)" }], 200);
            anim2.finished.then(() => log("b finished"), e => log("b rejected " + e.name));
            anim2.oncancel = e => log("b oncancel current " + e.currentTime);
            setTimeout(() => { log("cancel b " + anim2.playState); anim2.cancel(); log("b " + anim2.playState + " current " + anim2.currentTime + " pending " + anim2.pending); }, 0);
            """, s =>
        {
            var d = s.Document;
            UiElement a = s.Get("a"), b = s.Get("b");
            string Opacity() => Fixed(a.GetComputedStyle()!.Opacity, 2);
            var anim = a.Animate([new Keyframe { ["opacity"] = "0" }, new Keyframe { ["opacity"] = "1" }], new AnimateOptions { Duration = 119, Fill = FillMode.Both });
            s.Log($"animate {State(anim)} pending {Bool(anim.Pending)} start {Js(anim.StartTime)} current {Js(anim.CurrentTime)} same {Bool(ReferenceEquals(anim.Finished, anim.Finished))}");
            Then(anim.Ready, _ => s.Log($"ready {State(anim)} pending {Bool(anim.Pending)} start at frame {Bool(Math.Abs(anim.StartTime!.Value - s.FrameTime) < 1)} current {Frames(anim.CurrentTime)}"));
            Then(anim.Finished, _ => s.Log($"finished {State(anim)} current {Js(anim.CurrentTime)} opacity {Opacity()}"));
            anim.OnFinish = e => s.Log($"onfinish current {Js(e.CurrentTime)} timeline {Frames(e.TimelineTime - s.FrameTime)}");
            void Tick(double _)
            {
                s.Log($"raf {State(anim)} {Frames(anim.CurrentTime)} opacity {Opacity()}");
                if (s.Frame < 11)
                    d.RequestAnimationFrame(Tick);
            }
            d.RequestAnimationFrame(Tick);
            var anim2 = b.Animate([new Keyframe { ["transform"] = "translateX(0)" }, new Keyframe { ["transform"] = "translateX(10px)" }], 200);
            Then(anim2.Finished, _ => s.Log("b finished"), name => s.Log("b rejected " + name));
            anim2.OnCancel = e => s.Log($"b oncancel current {Js(e.CurrentTime)}");
            d.SetTimeout(() =>
            {
                s.Log($"cancel b {State(anim2)}");
                anim2.Cancel();
                s.Log($"b {State(anim2)} current {Js(anim2.CurrentTime)} pending {Bool(anim2.Pending)}");
            });
        }),
        // The effect sampled at chosen times: keyframe offsets (given, spaced, a missing first one
        // from the underlying value), keyframe and effect easings (overshooting, steps), delay, end
        // delay, iterations, directions, fill modes, zero and infinite durations, and the game's
        // own battle keyframes.
        new("animate-sampling", 400, 300, "#m { width: 100px; height: 50px; margin-left: 20px; }", """<div id="m"></div>""", 1, """
            const el = document.getElementById("m");
            const tx = () => new DOMMatrix(getComputedStyle(el).transform).m41.toFixed(2);
            const op = () => Number(getComputedStyle(el).opacity).toFixed(3);
            const rect = () => { const r = el.getBoundingClientRect(); return r.x.toFixed(2) + "," + r.y.toFixed(2) + "," + r.width.toFixed(2) + "x" + r.height.toFixed(2); };
            const run = (name, keyframes, options, times, read) => {
              const anim = el.animate(keyframes, options);
              anim.pause();
              log(name + " " + times.map(t => { anim.currentTime = t; return t + ":" + read(); }).join(" ") + " " + anim.playState);
              anim.cancel();
            };
            const kf = [{ transform: "translateX(0px)" }, { transform: "translateX(100px)" }];
            run("offsets", [{ transform: "translateX(0px)", easing: "ease-in" }, { transform: "translateX(100px)", offset: 0.3 }, { transform: "translateX(40px)" }], { duration: 1000, easing: "cubic-bezier(.2,1.4,.4,1)" }, [0, 100, 250, 300, 500, 700, 900, 999, 1000], tx);
            run("spaced", [{ transform: "translateX(0px)" }, { transform: "translateX(100px)" }, { transform: "translateX(50px)", offset: 0.8 }, { transform: "translateX(200px)" }], { duration: 1000 }, [0, 200, 400, 600, 800, 900, 1000], tx);
            run("neutral", [{ transform: "translateX(100px)" }], { duration: 1000, fill: "forwards" }, [0, 250, 500, 1000, 1500], tx);
            run("timing", kf, { duration: 400, delay: 100, endDelay: 50, iterations: 2.5, direction: "alternate", fill: "both", easing: "ease-in-out" }, [0, 50, 100, 200, 500, 550, 900, 1100, 1149, 1150, 1500], tx);
            run("reverse", kf, { duration: 400, delay: 100, direction: "reverse", fill: "backwards" }, [0, 100, 300, 500, 600], tx);
            run("alternate-reverse", kf, { duration: 200, iterations: 3, direction: "alternate-reverse", fill: "forwards" }, [0, 100, 200, 300, 500, 600, 700], tx);
            run("steps", kf, { duration: 400, delay: 100, fill: "both", easing: "steps(4)" }, [0, 100, 150, 200, 399, 500, 600], tx);
            run("steps-start", kf, { duration: 400, fill: "both", easing: "steps(2, start)" }, [0, 100, 199, 200, 300, 400, 500], tx);
            run("keyframe-steps", [{ transform: "translateX(0px)", easing: "step-end" }, { transform: "translateX(100px)" }], { duration: 400 }, [0, 100, 399, 400], tx);
            run("zero-duration", kf, { duration: 0, delay: 100, fill: "both" }, [0, 99, 100, 200], tx);
            run("infinite", kf, { duration: 300, iterations: Infinity, fill: "both" }, [0, 150, 300, 450, 3000, 3150], tx);
            run("opacity-overshoot", [{ opacity: 0 }, { opacity: 1 }], { duration: 1000, easing: "cubic-bezier(.2,1.4,.4,1)" }, [100, 400, 600, 800], op);
            run("summon", [{ transform: "translateY(-14%) scale(0.6)", opacity: 0, filter: "brightness(2.5)" }, { transform: "translateY(0) scale(1)", opacity: 1, filter: "brightness(1)" }], { duration: 520, easing: "cubic-bezier(.2,1.4,.4,1)" }, [0, 100, 200, 300, 400, 519], rect);
            run("fall", [{ transform: "translateY(0) rotate(0deg)", opacity: 1 }, { transform: "translateY(-6%) rotate(-12deg)", opacity: 1, offset: 0.3 }, { transform: "translateY(18%) rotate(-82deg)", opacity: 0.75 }], { duration: 620, easing: "ease-in", fill: "none" }, [0, 100, 186, 300, 500, 619, 620], rect);
            run("hop", [{ transform: "translate(0,0)" }, { transform: "translate(-18px, -6px)", offset: 0.4 }, { transform: "translate(0,0)" }], { duration: 380, easing: "ease-out" }, [0, 76, 152, 250, 379], rect);
            run("retreat", [{ transform: "translateX(0) scaleX(1)" }, { transform: "translateX(-10px) scaleX(-1)", offset: 0.2 }, { transform: "translateX(-60vw) scaleX(-1)" }], { duration: 900, easing: "ease-in", fill: "forwards" }, [0, 90, 180, 500, 900, 1000], rect);
            """, s =>
        {
            var d = s.Document;
            var el = s.Get("m");
            string Tx()
            {
                var style = el.GetComputedStyle()!;
                var box = el.Box!;
                return Fixed(UiPainter.CssTransform(style, new RectF(0, 0, box.Width, box.Height)).M31, 2);
            }
            string Op() => Fixed(el.GetComputedStyle()!.Opacity, 3);
            string Rect()
            {
                var r = d.BoundingClientRect(el);
                return $"{Fixed(r.X, 2)},{Fixed(r.Y, 2)},{Fixed(r.Width, 2)}x{Fixed(r.Height, 2)}";
            }
            void Run(string name, Keyframe[] keyframes, AnimateOptions options, double[] times, Func<string> read)
            {
                var anim = el.Animate(keyframes, options);
                anim.Pause();
                var values = times.Select(t =>
                {
                    anim.CurrentTime = t;
                    return $"{Js(t)}:{read()}";
                }).ToList();
                s.Log($"{name} {string.Join(" ", values)} {State(anim)}");
                anim.Cancel();
            }
            Run("offsets", [new Keyframe { ["transform"] = "translateX(0px)", Easing = "ease-in" }, new Keyframe { ["transform"] = "translateX(100px)", Offset = 0.3 }, new Keyframe { ["transform"] = "translateX(40px)" }],
                new AnimateOptions { Duration = 1000, Easing = "cubic-bezier(.2,1.4,.4,1)" }, [0, 100, 250, 300, 500, 700, 900, 999, 1000], Tx);
            Run("spaced", [new Keyframe { ["transform"] = "translateX(0px)" }, new Keyframe { ["transform"] = "translateX(100px)" }, new Keyframe { ["transform"] = "translateX(50px)", Offset = 0.8 }, new Keyframe { ["transform"] = "translateX(200px)" }],
                new AnimateOptions { Duration = 1000 }, [0, 200, 400, 600, 800, 900, 1000], Tx);
            Run("neutral", [new Keyframe { ["transform"] = "translateX(100px)" }], new AnimateOptions { Duration = 1000, Fill = FillMode.Forwards }, [0, 250, 500, 1000, 1500], Tx);
            Run("timing", TranslateX(0, 100), new AnimateOptions { Duration = 400, Delay = 100, EndDelay = 50, Iterations = 2.5, Direction = PlaybackDirection.Alternate, Fill = FillMode.Both, Easing = "ease-in-out" },
                [0, 50, 100, 200, 500, 550, 900, 1100, 1149, 1150, 1500], Tx);
            Run("reverse", TranslateX(0, 100), new AnimateOptions { Duration = 400, Delay = 100, Direction = PlaybackDirection.Reverse, Fill = FillMode.Backwards }, [0, 100, 300, 500, 600], Tx);
            Run("alternate-reverse", TranslateX(0, 100), new AnimateOptions { Duration = 200, Iterations = 3, Direction = PlaybackDirection.AlternateReverse, Fill = FillMode.Forwards }, [0, 100, 200, 300, 500, 600, 700], Tx);
            Run("steps", TranslateX(0, 100), new AnimateOptions { Duration = 400, Delay = 100, Fill = FillMode.Both, Easing = "steps(4)" }, [0, 100, 150, 200, 399, 500, 600], Tx);
            Run("steps-start", TranslateX(0, 100), new AnimateOptions { Duration = 400, Fill = FillMode.Both, Easing = "steps(2, start)" }, [0, 100, 199, 200, 300, 400, 500], Tx);
            Run("keyframe-steps", [new Keyframe { ["transform"] = "translateX(0px)", Easing = "step-end" }, new Keyframe { ["transform"] = "translateX(100px)" }], new AnimateOptions { Duration = 400 }, [0, 100, 399, 400], Tx);
            Run("zero-duration", TranslateX(0, 100), new AnimateOptions { Duration = 0, Delay = 100, Fill = FillMode.Both }, [0, 99, 100, 200], Tx);
            Run("infinite", TranslateX(0, 100), new AnimateOptions { Duration = 300, Iterations = double.PositiveInfinity, Fill = FillMode.Both }, [0, 150, 300, 450, 3000, 3150], Tx);
            Run("opacity-overshoot", [new Keyframe { ["opacity"] = "0" }, new Keyframe { ["opacity"] = "1" }], new AnimateOptions { Duration = 1000, Easing = "cubic-bezier(.2,1.4,.4,1)" }, [100, 400, 600, 800], Op);
            Run("summon", [new Keyframe { ["transform"] = "translateY(-14%) scale(0.6)", ["opacity"] = "0", ["filter"] = "brightness(2.5)" }, new Keyframe { ["transform"] = "translateY(0) scale(1)", ["opacity"] = "1", ["filter"] = "brightness(1)" }],
                new AnimateOptions { Duration = 520, Easing = "cubic-bezier(.2,1.4,.4,1)" }, [0, 100, 200, 300, 400, 519], Rect);
            Run("fall", [new Keyframe { ["transform"] = "translateY(0) rotate(0deg)", ["opacity"] = "1" }, new Keyframe { ["transform"] = "translateY(-6%) rotate(-12deg)", ["opacity"] = "1", Offset = 0.3 }, new Keyframe { ["transform"] = "translateY(18%) rotate(-82deg)", ["opacity"] = "0.75" }],
                new AnimateOptions { Duration = 620, Easing = "ease-in", Fill = FillMode.None }, [0, 100, 186, 300, 500, 619, 620], Rect);
            Run("hop", [new Keyframe { ["transform"] = "translate(0,0)" }, new Keyframe { ["transform"] = "translate(-18px, -6px)", Offset = 0.4 }, new Keyframe { ["transform"] = "translate(0,0)" }],
                new AnimateOptions { Duration = 380, Easing = "ease-out" }, [0, 76, 152, 250, 379], Rect);
            Run("retreat", [new Keyframe { ["transform"] = "translateX(0) scaleX(1)" }, new Keyframe { ["transform"] = "translateX(-10px) scaleX(-1)", Offset = 0.2 }, new Keyframe { ["transform"] = "translateX(-60vw) scaleX(-1)" }],
                new AnimateOptions { Duration = 900, Easing = "ease-in", Fill = FillMode.Forwards }, [0, 90, 180, 500, 900, 1000], Rect);
        }),
        // Control: pause (pending until the next frame), play again (the start time moves),
        // finish() (Finished at once, the event with the next frame), play after finishing (a new
        // Finished), cancel (cancel event), seeking an idle animation; a zero-duration animation
        // and one with an end delay finishing.
        new("animate-control", 400, 300, "#a, #b, #c { width: 50px; height: 20px; }", """<div id="a"></div><div id="b"></div><div id="c"></div>""", 12, """
            const a = document.getElementById("a"), b = document.getElementById("b"), c = document.getElementById("c");
            const frames = t => t === null ? "null" : String(Math.round(t / (1000 / 60)));
            const kf = [{ transform: "translateX(0px)" }, { transform: "translateX(100px)" }];
            const anim = a.animate(kf, 1000);
            const first = anim.finished;
            anim.onfinish = e => log("a onfinish " + e.currentTime);
            anim.oncancel = () => log("a oncancel");
            first.then(() => log("a first finished " + anim.playState), e => log("a first rejected " + e.name));
            let step = 0;
            const tick = () => {
              step++;
              if (step === 2) { anim.pause(); log("a pause " + anim.playState + " pending " + anim.pending); anim.ready.then(() => log("a paused ready " + anim.playState + " " + frames(anim.currentTime))); }
              if (step === 4) { log("a still " + frames(anim.currentTime)); anim.play(); log("a play " + anim.playState + " pending " + anim.pending); anim.ready.then(() => log("a resumed ready " + frames(anim.currentTime))); }
              if (step === 6) { log("a before finish " + frames(anim.currentTime)); anim.finish(); log("a finish " + anim.playState + " " + anim.currentTime + " same promise " + (anim.finished === first)); }
              if (step === 7) { anim.play(); log("a replay " + anim.playState + " " + anim.currentTime + " new promise " + (anim.finished !== first)); anim.finished.then(() => log("a second finished"), e => log("a second rejected " + e.name)); }
              if (step === 8) { anim.cancel(); log("a cancel " + anim.playState + " " + anim.currentTime + " start " + anim.startTime); }
              if (step === 9) { anim.currentTime = 500; log("a seek " + anim.playState + " " + anim.currentTime + " start " + anim.startTime); }
              if (step < 11) requestAnimationFrame(tick);
            };
            requestAnimationFrame(tick);
            const zero = b.animate(kf, { duration: 0 });
            zero.finished.then(() => log("b finished " + zero.playState + " " + zero.currentTime));
            zero.ready.then(() => log("b ready"));
            zero.onfinish = () => log("b onfinish");
            const ending = c.animate(kf, { duration: 40, endDelay: 50 });
            ending.finished.then(() => log("c finished " + frames(ending.currentTime)));
            """, s =>
        {
            var d = s.Document;
            UiElement a = s.Get("a"), b = s.Get("b"), c = s.Get("c");
            var anim = a.Animate(TranslateX(0, 100), 1000);
            var first = anim.Finished;
            anim.OnFinish = e => s.Log($"a onfinish {Js(e.CurrentTime)}");
            anim.OnCancel = _ => s.Log("a oncancel");
            Then(first, _ => s.Log($"a first finished {State(anim)}"), name => s.Log("a first rejected " + name));
            var step = 0;
            void Tick(double _)
            {
                step++;
                if (step == 2)
                {
                    anim.Pause();
                    s.Log($"a pause {State(anim)} pending {Bool(anim.Pending)}");
                    Then(anim.Ready, _ => s.Log($"a paused ready {State(anim)} {Frames(anim.CurrentTime)}"));
                }
                if (step == 4)
                {
                    s.Log($"a still {Frames(anim.CurrentTime)}");
                    anim.Play();
                    s.Log($"a play {State(anim)} pending {Bool(anim.Pending)}");
                    Then(anim.Ready, _ => s.Log($"a resumed ready {Frames(anim.CurrentTime)}"));
                }
                if (step == 6)
                {
                    s.Log($"a before finish {Frames(anim.CurrentTime)}");
                    anim.Finish();
                    s.Log($"a finish {State(anim)} {Js(anim.CurrentTime)} same promise {Bool(ReferenceEquals(anim.Finished, first))}");
                }
                if (step == 7)
                {
                    anim.Play();
                    s.Log($"a replay {State(anim)} {Js(anim.CurrentTime)} new promise {Bool(!ReferenceEquals(anim.Finished, first))}");
                    Then(anim.Finished, _ => s.Log("a second finished"), name => s.Log("a second rejected " + name));
                }
                if (step == 8)
                {
                    anim.Cancel();
                    s.Log($"a cancel {State(anim)} {Js(anim.CurrentTime)} start {Js(anim.StartTime)}");
                }
                if (step == 9)
                {
                    anim.CurrentTime = 500;
                    s.Log($"a seek {State(anim)} {Js(anim.CurrentTime)} start {Js(anim.StartTime)}");
                }
                if (step < 11)
                    d.RequestAnimationFrame(Tick);
            }
            d.RequestAnimationFrame(Tick);
            var zero = b.Animate(TranslateX(0, 100), new AnimateOptions { Duration = 0 });
            Then(zero.Finished, _ => s.Log($"b finished {State(zero)} {Js(zero.CurrentTime)}"));
            Then(zero.Ready, _ => s.Log("b ready"));
            zero.OnFinish = _ => s.Log("b onfinish");
            var ending = c.Animate(TranslateX(0, 100), new AnimateOptions { Duration = 40, EndDelay = 50 });
            Then(ending.Finished, _ => s.Log($"c finished {Frames(ending.CurrentTime)}"));
        }),
        // Composite order: script animations after CSS animations (a neutral keyframe starts from
        // the CSS animation's value), later script animations over earlier ones, !important over
        // both; animations of a hidden element and of a removed one still run and finish.
        new("animate-composite", 400, 300, """
            @keyframes slide { from { transform: translateX(0px); } to { transform: translateX(200px); } }
            #k { width: 50px; height: 20px; animation: slide 1s linear -0.5s paused; }
            #two, #h, #r { width: 50px; height: 20px; }
            #imp { width: 50px; height: 20px; transform: translateX(7px) !important; }
            """, """<div id="k"></div><div id="two"></div><div id="imp"></div><div id="h"></div><div id="r"></div>""", 7, """
            const tx = el => new DOMMatrix(getComputedStyle(el).transform).m41.toFixed(2);
            const k = document.getElementById("k"), two = document.getElementById("two"), imp = document.getElementById("imp"), h = document.getElementById("h"), r = document.getElementById("r");
            const seek = (anim, t) => { anim.pause(); anim.currentTime = t; };
            log("css " + tx(k));
            const over = k.animate([{ transform: "translateX(50px)" }], 1000); seek(over, 500);
            log("over css " + tx(k));
            const first = two.animate([{ transform: "translateX(0px)" }, { transform: "translateX(100px)" }], 1000); seek(first, 500);
            const second = two.animate([{ transform: "translateX(300px)" }], 1000); seek(second, 500);
            log("stacked " + tx(two));
            second.cancel(); log("first only " + tx(two));
            const important = imp.animate([{ transform: "translateX(0px)" }, { transform: "translateX(100px)" }], 1000); seek(important, 500);
            log("important " + tx(imp));
            h.style.display = "none";
            const hidden = h.animate([{ opacity: 0 }, { opacity: 1 }], { duration: 40, fill: "forwards" });
            hidden.finished.then(() => { log("hidden finished " + hidden.playState); h.style.display = "block"; log("shown opacity " + getComputedStyle(h).opacity); });
            const removed = r.animate([{ opacity: 0 }, { opacity: 1 }], 40);
            r.remove();
            removed.finished.then(() => log("removed finished " + removed.playState));
            removed.onfinish = () => log("removed onfinish");
            """, s =>
        {
            var d = s.Document;
            UiElement k = s.Get("k"), two = s.Get("two"), imp = s.Get("imp"), h = s.Get("h"), r = s.Get("r");
            static string Tx(UiElement el)
            {
                var style = el.GetComputedStyle()!;
                var box = el.Box!;
                return Fixed(UiPainter.CssTransform(style, new RectF(0, 0, box.Width, box.Height)).M31, 2);
            }
            static void Seek(UiAnimation animation, double time)
            {
                animation.Pause();
                animation.CurrentTime = time;
            }
            s.Log("css " + Tx(k));
            var over = k.Animate([new Keyframe { ["transform"] = "translateX(50px)" }], 1000);
            Seek(over, 500);
            s.Log("over css " + Tx(k));
            var first = two.Animate(TranslateX(0, 100), 1000);
            Seek(first, 500);
            var second = two.Animate([new Keyframe { ["transform"] = "translateX(300px)" }], 1000);
            Seek(second, 500);
            s.Log("stacked " + Tx(two));
            second.Cancel();
            s.Log("first only " + Tx(two));
            var important = imp.Animate(TranslateX(0, 100), 1000);
            Seek(important, 500);
            s.Log("important " + Tx(imp));
            h.SetStyle("display", "none");
            var hidden = h.Animate([new Keyframe { ["opacity"] = "0" }, new Keyframe { ["opacity"] = "1" }], new AnimateOptions { Duration = 40, Fill = FillMode.Forwards });
            Then(hidden.Finished, _ =>
            {
                s.Log($"hidden finished {State(hidden)}");
                h.SetStyle("display", "block");
                s.Log("shown opacity " + ScriptContext.Js(h.GetComputedStyle()!.Opacity));
            });
            var removed = r.Animate([new Keyframe { ["opacity"] = "0" }, new Keyframe { ["opacity"] = "1" }], 40);
            r.Remove();
            Then(removed.Finished, _ => s.Log($"removed finished {State(removed)}"));
            removed.OnFinish = _ => s.Log("removed onfinish");
        }),
    ];

    static string State(UiAnimation animation) => animation.PlayState.ToString().ToLowerInvariant();

    static string Bool(bool value) => value ? "true" : "false";

    static string Js(double? value) => value is { } v ? ScriptContext.Js(v) : "null";
}
