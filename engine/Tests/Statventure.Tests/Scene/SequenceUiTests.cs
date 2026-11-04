using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Canvas;
using Statventure.UI.Dom;

namespace Statventure.Tests.Scene;

/// <summary>A document with a title card (the host <c>#card</c>) and things outside it, a scene in it, frames of the document's clock.</summary>
sealed class UiBench
{
    public const string Css = """
        .bar, .outside, .spin, .fade, .short { opacity: 0.5; }
        #card.is-playing .bar { animation: grow 1s linear both; }
        #card.is-playing .short { animation: grow 0.1s linear both; }
        .outside.on { animation: grow 1s linear both; }
        .spin.go { animation: grow 4s linear infinite; }
        .fade { transition: opacity 1s linear; }
        .fade.lit { opacity: 1; }
        @keyframes grow { from { opacity: 0; } to { opacity: 1; } }
        """;

    public UiBench()
    {
        Document = new UiDocument(TestFonts.Library);
        Document.SetViewport(400, 300, false);
        Document.AddStylesheet(Css, "bench.css");
        Markup.SetInnerHtml(Document.Body, """
            <div id="card" class="card" data-x="1"><h1 id="title">Great Imp <em>Ji'ruan</em></h1><p id="label">x</p><i class="bar"></i><i class="spin"></i><i class="fade"></i><i class="short"></i><div id="caption"><p id="line"></p></div></div>
            <i class="outside"></i><p id="away"></p>
            """);
        Scene = new SceneView(new View3D(null!));
        Document.AddFrameClient(Scene);
        Document.Update(0);
    }

    public UiDocument Document { get; }
    public SceneView Scene { get; }
    public double Now { get; private set; }
    public UiElement Card => Element("card");

    public UiElement Element(string id) => Document.GetElementById(id)!;

    public UiElement First(string className) => Document.Body.Descendants().First(e => e.HasClass(className));

    /// <summary>The element's opacity as shown (NaN when it has no style: under display none).</summary>
    public float Opacity(string className) => First(className).Style?.Opacity ?? float.NaN;

    public SequencePlayer Play(Sequence sequence, UiElement? host = null) =>
        Scene.Play(sequence, new SequenceBindings().Element("card", host ?? Card), Document);

    public void Frame(double seconds)
    {
        Now += seconds;
        Document.Update(Now);
    }

    public void Frames(int count, double seconds = 1 / 60.0, Action? each = null)
    {
        for (var i = 0; i < count; i++)
        {
            Frame(seconds);
            each?.Invoke();
        }
    }
}

/// <summary>
/// The UI track (M10-9, Q29; Docs/Scene.md, "How it works", step 6, S6-D): text, classes and attributes set in
/// the frame the clock passes their cues, each thing set as it was before its first cue, the end state at a
/// skip, ids found in the host when it plays, nested hosts refused; the CSS animations inside the hosts on
/// the sequence's clock (held by a pause, from the last frame its clock moved in, and by the hidden time),
/// the rest and transitions on the document's, seeking not reaching them.
/// </summary>
public class SequenceUiTests
{
    const double Frame = 1 / 60.0;

    static Sequence Card(double duration, Action<SequenceBuilder.UiBuilder> cues) =>
        new SequenceBuilder("card", duration).Role("card", RoleKind.Element).Ui("card", cues).Build();

    static Sequence Cues => Card(1, u => u
        .AddClass(0, "is-playing")
        .SetAttribute(0, "data-boss", "jiruan")
        .Text(0.3, "label", "LORD OF THE IMP COURT")
        .RemoveClass(0.4, "card")
        .RemoveAttribute(0.5, "data-x")
        .Text(0.6, "title", "Changed")
        .SetAttribute(0.7, "class", "card shown", "caption")
        .RemoveClass(1, "is-playing")
        .SetAttribute(1, "hidden", ""));

    static (bool Playing, string? Boss, string Label, string? X, string Title, string? Caption, bool Hidden) State(UiBench bench) =>
        (bench.Card.HasClass("is-playing"), bench.Card.GetAttribute("data-boss"), bench.Element("label").TextContent, bench.Card.GetAttribute("data-x"),
            bench.Element("title").TextContent, bench.Element("caption").GetAttribute("class"), bench.Card.HasAttribute("hidden"));

    /// <summary>Each cue lands in the frame whose timing update passes it, and its style changes show in that frame (a class there starts its CSS animations at that frame's time).</summary>
    [Fact]
    public void CuesSetTheirStateInTheFrameTheClockPassesThem()
    {
        var bench = new UiBench();
        var player = bench.Play(Cues);
        Assert.Equal((false, null, "x", "1", "Great Imp Ji'ruan", null, false), State(bench));
        var seen = new List<(double Tau, (bool, string?, string, string?, string, string?, bool) State, float Bar)>();
        var own = new List<bool>();
        bench.Frames(12, 0.1, () =>
        {
            seen.Add((player.TimelineTime, State(bench), bench.Opacity("bar")));
            own.Add(bench.Card.HasClass("card"));
        });
        // A class the host had from the start, taken off at 0.4.
        Assert.Equal([true, true, true, true, false, false], own.Take(6));
        // The first frame is at 0: is-playing and data-boss, and the bar's animation starts there (opacity 0, not the 0.5 below it).
        Assert.Equal((0, (true, "jiruan", "x", "1", "Great Imp Ji'ruan", null, false), 0f), (seen[0].Tau, seen[0].State, seen[0].Bar));
        Assert.Equal((true, "jiruan", "x", "1", "Great Imp Ji'ruan", null, false), seen[2].State);
        Assert.Equal((true, "jiruan", "LORD OF THE IMP COURT", "1", "Great Imp Ji'ruan", null, false), seen[3].State);
        Assert.Equal((true, "jiruan", "LORD OF THE IMP COURT", null, "Great Imp Ji'ruan", null, false), seen[5].State);
        Assert.Equal((true, "jiruan", "LORD OF THE IMP COURT", null, "Changed", null, false), seen[6].State);
        Assert.Equal((true, "jiruan", "LORD OF THE IMP COURT", null, "Changed", "card shown", false), seen[7].State);
        Assert.Equal((false, "jiruan", "LORD OF THE IMP COURT", null, "Changed", "card shown", true), seen[10].State);
        Assert.True(player.Ended);
        // The bar ran with the frames (linear over 1 s from the first frame).
        Assert.Equal(0.3f, seen[3].Bar, 4);
    }

    /// <summary>
    /// Before its first cue a thing is as it was when the sequence started (S4-8): a seek back puts back the
    /// text with its children, the class and the attribute; the game's own change between cues stands until
    /// the next cue of that thing.
    /// </summary>
    [Fact]
    public void BeforeItsFirstCueAThingIsAsItWasAndTheGamesChangesStandUntilTheNextCue()
    {
        var bench = new UiBench();
        var em = bench.Element("title").Children[1];
        var player = bench.Play(Cues);
        bench.Frames(8, 0.1);
        Assert.Equal((true, "jiruan", "LORD OF THE IMP COURT", null, "Changed", "card shown", false), State(bench));
        player.Seek(0.2);
        bench.Frame(0);
        Assert.Equal((true, "jiruan", "x", "1", "Great Imp Ji'ruan", null, false), State(bench));
        Assert.Same(em, bench.Element("title").Children[1]);
        Assert.True(bench.Card.HasClass("card"));
        player.Seek(0);
        bench.Card.RemoveClass("is-playing");
        bench.Frame(0);
        // A seek applies the state there (the class back on).
        Assert.True(bench.Card.HasClass("is-playing"));
        // The game's change between cues stands: the label's text until its cue at 0.3.
        bench.Element("label").TextContent = "game";
        bench.Card.SetAttribute("data-boss", "other");
        bench.Frames(2, 0.1);
        Assert.Equal(("game", "other"), (bench.Element("label").TextContent, bench.Card.GetAttribute("data-boss")));
        bench.Frame(0.1);
        Assert.Equal(("LORD OF THE IMP COURT", "other"), (bench.Element("label").TextContent, bench.Card.GetAttribute("data-boss")));
        player.Seek(0);
        bench.Frame(0);
        Assert.Equal("jiruan", bench.Card.GetAttribute("data-boss"));
    }

    /// <summary>What changes in one frame changes in the record order of the cues now in effect: the class attribute set, then a class added on top.</summary>
    [Fact]
    public void ThingsChangingInOneFrameChangeInTheCuesRecordOrder()
    {
        var bench = new UiBench();
        bench.Play(Card(1, u => u
            .AddClass(0.1, "glow", "caption")
            .SetAttribute(0.5, "class", "card shown", "caption")
            .AddClass(0.5, "glow", "caption")
            .RemoveClass(0.5, "lit", "caption")
            .AddClass(0.2, "lit", "caption")));
        bench.Frames(3, 0.1);
        Assert.Equal("glow lit", bench.Element("caption").GetAttribute("class"));
        bench.Frames(4, 0.1);
        Assert.Equal("card shown glow", bench.Element("caption").GetAttribute("class"));
    }

    [Fact]
    public void ASkipLeavesTheEndState()
    {
        var bench = new UiBench();
        var player = bench.Play(Cues);
        bench.Frame(Frame);
        Assert.True(player.Skip());
        Assert.Equal((false, "jiruan", "LORD OF THE IMP COURT", null, "Changed", "card shown", true), State(bench));
        bench.Frames(3);
        Assert.Equal((false, "jiruan", "LORD OF THE IMP COURT", null, "Changed", "card shown", true), State(bench));
    }

    /// <summary>Elements are found by id among the host's descendants when it plays (S4-6): one missing, or outside the host, is refused, changing nothing.</summary>
    [Fact]
    public void ElementsAreFoundByIdInTheHostWhenItPlays()
    {
        var bench = new UiBench();
        Assert.Equal("the sequence card, track 0 (ui): its host card has no element nothing",
            Assert.Throws<ArgumentException>(() => bench.Play(Card(1, u => u.AddClass(0, "on").Text(0.5, "nothing", "x")))).Message);
        Assert.Equal("the sequence card, track 0 (ui): its host card has no element away",
            Assert.Throws<ArgumentException>(() => bench.Play(Card(1, u => u.Text(0.5, "away", "x")))).Message);
        Assert.Empty(bench.Scene.Playing);
        Assert.False(bench.Card.HasClass("on"));
        // Found once: an element of that id added later isn't the one the cues set.
        var player = bench.Play(Card(1, u => u.Text(0.5, "line", "now")));
        var first = bench.Element("line");
        first.Remove();
        var later = bench.Element("caption").Append(bench.Document.CreateElement("p"));
        later.Id = "line";
        bench.Frames(7, 0.2);
        Assert.Equal(("now", ""), (first.TextContent, later.TextContent));
        Assert.True(player.Ended);
    }

    /// <summary>A host that is, holds or is inside a host of another playing sequence is refused (S6-D); hosts of one sequence may nest; once the other ends, it plays.</summary>
    [Fact]
    public void HostsNestedWithAnotherPlayingSequencesAreRefused()
    {
        var bench = new UiBench();
        var first = bench.Play(Card(1, u => u.AddClass(0, "on")), bench.Element("caption"));
        foreach (var host in new[] { bench.Element("caption"), bench.Card, bench.Element("line") })
            Assert.Equal("the sequence card, track 0 (ui): its host card is, holds or is inside a UI host of the sequence card, which is playing",
                Assert.Throws<ArgumentException>(() => bench.Play(Card(1, u => u.AddClass(0, "x")), host)).Message);
        Assert.Equal([first], bench.Scene.Playing);
        // Beside it: fine.
        var beside = bench.Play(Card(1, u => u.AddClass(0, "x")), bench.Element("away"));
        // One sequence's own hosts may nest.
        var nested = bench.Scene.Play(new SequenceBuilder("nested", 1).Role("a", RoleKind.Element).Role("b", RoleKind.Element)
            .Ui("a", u => u.AddClass(0, "x")).Ui("b", u => u.AddClass(0, "y")).Build(),
            new SequenceBindings().Element("a", bench.Element("title")).Element("b", bench.Element("title").Children.OfType<UiElement>().First()), bench.Document);
        first.Finish();
        var again = bench.Play(Card(1, u => u.AddClass(0, "x")), bench.Element("caption"));
        Assert.Equal([beside, nested, again], bench.Scene.Playing);
    }

    /// <summary>
    /// The CSS animations inside the host run on the sequence's clock (S6-D): with the frames while it plays;
    /// a pause from a task that runs before a frame holds them from the last frame its clock moved in, as
    /// the clock; resuming goes on from there. Outside the host, and the transitions inside it, keep the
    /// document's clock. One running inside the host before the sequence started is held too.
    /// </summary>
    [Fact]
    public void TheHostsAnimationsKeepTheSequencesClockThroughAPause()
    {
        var bench = new UiBench();
        bench.First("spin").AddClass("go");
        bench.Frames(6);
        var player = bench.Play(Card(2, u => u.AddClass(0, "is-playing")));
        bench.First("outside").AddClass("on");
        var spin = bench.Opacity("spin");
        bench.Frames(12, Frame, () => Assert.Equal((float)player.Time, bench.Opacity("bar"), 5));
        Assert.Equal(bench.Opacity("bar"), bench.Opacity("outside"), 5);
        bench.First("fade").AddClass("lit");
        // A timer 5 ms after the last frame runs before the next frame's steps: that frame doesn't move the
        // clock, so the bar is held from the last frame, not from the timer's time.
        bench.Document.SetTimeout(player.Pause, 5);
        var held = (bench.Opacity("bar"), bench.Opacity("spin"));
        var fade = bench.Opacity("fade");
        bench.Frames(15, Frame, () => Assert.Equal((float)player.Time, bench.Opacity("bar"), 5));
        Assert.Equal(held, (bench.Opacity("bar"), bench.Opacity("spin")));
        Assert.True(bench.Opacity("outside") > held.Item1 + 0.2f);
        Assert.True(bench.Opacity("fade") > fade + 0.05f);
        // Resumed by a timer 7 ms after the last frame: the next frame moves the clock by its whole time, and the bar with it.
        bench.Document.SetTimeout(player.Resume, 7);
        bench.Frames(10, Frame, () => Assert.Equal((float)player.Time, bench.Opacity("bar"), 5));
        Assert.Equal(held.Item2 + 10 * (float)Frame / 4, bench.Opacity("spin"), 5);
        Assert.True(spin < held.Item2);
        // Paused by a timer before a frame again, resumed between frames: held from the last frame before the timer.
        bench.Document.SetTimeout(player.Pause, 5);
        bench.Frames(6, Frame, () => Assert.Equal((float)player.Time, bench.Opacity("bar"), 5));
        player.Resume();
        bench.Frames(6, Frame, () => Assert.Equal((float)player.Time, bench.Opacity("bar"), 5));
    }

    /// <summary>The hidden document: the time it is hidden is dropped from the clock (M10-7) and held for the host's animations exactly, from the hiding to the showing; outside they jump.</summary>
    [Fact]
    public void TheHostsAnimationsWaitWhileTheDocumentIsHidden()
    {
        var bench = new UiBench();
        var player = bench.Play(Card(2, u => u.AddClass(0, "is-playing")));
        bench.First("outside").AddClass("on");
        bench.Frames(10);
        var outside = bench.Opacity("outside");
        bench.Document.SetHidden(true);
        bench.Document.SetHidden(false, bench.Now + 0.3);
        bench.Frame(0.3 + 0.004);
        Assert.Equal((float)player.Time, bench.Opacity("bar"), 5);
        Assert.Equal(outside + 0.304f, bench.Opacity("outside"), 4);
        bench.Frames(5, Frame, () => Assert.Equal((float)player.Time, bench.Opacity("bar"), 5));
        // Paused and hidden at once, and resumed while hidden: held through both, running between.
        player.Pause();
        bench.Frames(3);
        bench.Document.SetHidden(true);
        player.Resume();
        bench.Document.SetHidden(false, bench.Now + 0.5);
        bench.Frame(0.5 + Frame);
        Assert.Equal((float)player.Time, bench.Opacity("bar"), 5);
        bench.Frames(4, Frame, () => Assert.Equal((float)player.Time, bench.Opacity("bar"), 5));
        // Hidden, shown with no frame, hidden again (at the showing's time, after the last frame), shown: both hidden times held.
        bench.Document.SetHidden(true);
        bench.Document.SetHidden(false, bench.Now + 0.2);
        bench.Document.SetHidden(true);
        bench.Document.SetHidden(false, bench.Now + 0.5);
        bench.Frame(0.5 + Frame);
        Assert.Equal((float)player.Time, bench.Opacity("bar"), 5);
        // The same with a pause between, resumed while hidden again: the pause from the last frame, then the second hidden time.
        bench.Document.SetHidden(true);
        bench.Document.SetHidden(false, bench.Now + 0.1);
        player.Pause();
        bench.Document.SetHidden(true);
        player.Resume();
        bench.Document.SetHidden(false, bench.Now + 0.3);
        bench.Frame(0.3 + Frame);
        Assert.Equal((float)player.Time, bench.Opacity("bar"), 5);
        bench.Frames(3, Frame, () => Assert.Equal((float)player.Time, bench.Opacity("bar"), 5));
    }

    /// <summary>A seek doesn't reach the CSS animations (Q29): they go on by their own time.</summary>
    [Fact]
    public void ASeekDoesntReachTheHostsAnimations()
    {
        var bench = new UiBench();
        var player = bench.Play(Card(2, u => u.AddClass(0, "is-playing")));
        bench.Frames(6);
        var bar = bench.Opacity("bar");
        player.Seek(0.9);
        bench.Frame(Frame);
        Assert.Equal(bar + (float)Frame, bench.Opacity("bar"), 5);
        Assert.Equal(0.9 + Frame, player.Time, 9);
    }

    /// <summary>
    /// At the end the animations go on on the document's clock from where they stand: after a skip while
    /// paused they move again with the frames; their events, held with them, come then.
    /// </summary>
    [Fact]
    public void AtTheEndTheAnimationsGoOnOnTheDocumentsClock()
    {
        var bench = new UiBench();
        var ended = new List<double>();
        bench.First("short").AddEventListener("animationend", _ => ended.Add(bench.Now));
        var player = bench.Play(Card(2, u => u.AddClass(0, "is-playing")));
        bench.Frames(3);
        player.Pause();
        var held = bench.Opacity("bar");
        bench.Frames(10);
        Assert.Equal(held, bench.Opacity("bar"));
        Assert.Empty(ended);
        // Skipped by a timer that runs before the next frame's steps: released at the last frame, so that
        // frame moves the bar by its whole time.
        var skipped = false;
        bench.Document.SetTimeout(() => skipped = player.Skip(), 5);
        bench.Frame(Frame);
        Assert.True(skipped);
        Assert.Equal(held + (float)Frame, bench.Opacity("bar"), 5);
        bench.Frames(5);
        Assert.Single(ended);
        // The timeline is gone: what starts in the host now is the document's.
        Assert.Null(bench.Document.Animations.TimelineAround(bench.Card));
    }
}
