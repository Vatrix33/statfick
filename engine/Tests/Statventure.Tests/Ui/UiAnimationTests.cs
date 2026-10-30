using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// CSS animations and transitions over restyles as Chromium 152 runs them (the event order and
/// times are checked against Chromium by the "animation-events" input fixture): animations are kept
/// by name, a reversed transition is shortened, display: none cancels.
/// </summary>
public class UiAnimationTests
{
    const string Css = """
        @keyframes grow { from { width: 80px; } to { width: 90px; } }
        div { width: 50px; height: 20px; }
        """;

    static UiDocument Load(string css, string html)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(400, 300);
        document.AddStylesheet(Css + css);
        Markup.SetInnerHtml(document.Body, html);
        document.Update(0);
        return document;
    }

    static List<string> LogEvents(UiDocument document)
    {
        var log = new List<string>();
        foreach (var type in new[] { "animationstart", "animationiteration", "animationend", "animationcancel", "transitionrun", "transitionstart", "transitionend", "transitioncancel" })
            document.AddEventListener(type, e => log.Add(e switch
            {
                UiAnimationEvent a => $"{a.Type} {a.AnimationName} {a.ElapsedTime:0.###}",
                UiTransitionEvent t => $"{t.Type} {t.PropertyName} {t.ElapsedTime:0.###}",
                _ => e.Type,
            }));
        return log;
    }

    /// <summary>Runs 60 Hz frames from <paramref name="from"/> to <paramref name="to"/> seconds.</summary>
    static double Run(UiDocument document, double from, double to)
    {
        var time = from;
        while (time < to - 1e-9)
        {
            time = Math.Min(to, time + 1 / 60.0);
            document.Update(time);
        }
        return time;
    }

    [Fact]
    public void AFinishedAnimationIsNotRestartedByARestyle()
    {
        var document = Load(".x { color: red; }", """<div id="a" style="animation: grow 100ms"></div>""");
        var log = LogEvents(document);
        var a = document.QuerySelector("#a")!;
        var time = Run(document, 0, 0.3);
        a.SetAttribute("class", "x");
        Run(document, time, 0.5);
        Assert.Equal(["animationstart grow 0", "animationend grow 0.1"], log);
        Assert.Equal(50, a.Style!.Width.Pixels);
    }

    [Fact]
    public void AnAnimationStartsWhenItsElementIsShownAgain()
    {
        var document = Load(".hidden { display: none; }", """<div id="a" class="hidden" style="animation: grow 1s linear"></div>""");
        var log = LogEvents(document);
        var a = document.QuerySelector("#a")!;
        var time = Run(document, 0, 0.2);
        a.SetAttribute("class", "");
        time = Run(document, time, 0.7);
        Assert.Equal(["animationstart grow 0"], log);
        Assert.InRange(a.Style!.Width.Pixels, 84.5f, 85.5f);
        a.SetAttribute("class", "hidden");
        Run(document, time, 0.8);
        Assert.Equal(["animationstart grow 0", "animationcancel grow 0.5"], log);
    }

    [Fact]
    public void AReversedTransitionTakesAsLongAsTheWayItHadCome()
    {
        var document = Load("#a { transition: width 1s linear; } #a.on { width: 150px; }", """<div id="a"></div>""");
        var log = LogEvents(document);
        var a = document.QuerySelector("#a")!;
        // It starts with the next frame and goes back after 0.4 s, from 90px: the way back takes 0.4 s too.
        a.SetAttribute("class", "on");
        var time = Run(document, 0, 0.4);
        Assert.Equal(50 + 100 * (0.4f - 1 / 60f), a.Style!.Width.Pixels, 0.01f);
        a.SetAttribute("class", "");
        time = Run(document, time, 0.6 + 1 / 60.0);
        Assert.Equal(70, a.Style!.Width.Pixels, 0.01f);
        Run(document, time, 1);
        Assert.Equal(50, a.Style!.Width.Pixels);
        Assert.Equal(["transitionrun width 0", "transitionstart width 0", "transitioncancel width 0.4", "transitionrun width 0", "transitionstart width 0", "transitionend width 0.4"], log);
    }

    [Fact]
    public void ACompoundBeforeASiblingCombinatorIsNoAncestor()
    {
        // The ancestor filter must not ask for #go among #kid's ancestors.
        var document = Load("#go ~ #holder > #kid { width: 70px; }", """<div id="go"></div><div id="holder"><div id="kid"></div></div>""");
        Assert.Equal(70, document.QuerySelector("#kid")!.Style!.Width.Pixels);
    }
}
