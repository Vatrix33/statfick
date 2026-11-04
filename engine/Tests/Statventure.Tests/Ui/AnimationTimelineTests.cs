using Statventure.Tests.Canvas;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// A sequence's timeline for the CSS animations inside its UI hosts (Docs/Scripting.md, "Sequences' CSS
/// animations"; Docs/Scene.md, S6-D): held and released at given document times, the animations attached
/// with their root (those already running and those that start later, pseudo-elements too) and back on the
/// document's clock from where they stand when it is detached; play-state pauses and events on its clock.
/// </summary>
public class AnimationTimelineTests
{
    const string Css = """
        .a, .b, .c, #root::before { opacity: 0.5; }
        .on .a, .on.b, .outside.on { animation: grow 1s linear both; }
        .on .c { animation: grow 0.2s linear both; }
        .on .a.still { animation-play-state: paused; }
        #root.on::before { content: "x"; animation: grow 1s linear both; }
        @keyframes grow { from { opacity: 0; } to { opacity: 1; } }
        """;

    sealed class Page
    {
        public Page()
        {
            Document = new UiDocument(TestFonts.Library);
            Document.SetViewport(300, 200, false);
            Document.AddStylesheet(Css, "timeline.css");
            Markup.SetInnerHtml(Document.Body, """<div id="root"><i class="a"></i><i class="c"></i></div><i class="b"></i><i class="outside"></i>""");
            Document.Update(0);
        }

        public UiDocument Document { get; }
        public AnimationEngine Animations => Document.Animations;
        public UiElement Root => Document.GetElementById("root")!;
        public UiElement Of(string className) => Document.Body.Descendants().First(e => e.HasClass(className));
        public float Opacity(string className) => Of(className).Style!.Opacity;
        public double Now { get; private set; }

        public void Frame(double seconds)
        {
            Now += seconds;
            Document.Update(Now);
        }
    }

    /// <summary>
    /// Held at a document time (its player's: the last frame, or a hiding between frames), an animation stands
    /// at its value then; released at another, it goes on from there. A second hold from an earlier time with
    /// nothing drawn between extends it back; a later one changes nothing; it never runs backwards.
    /// </summary>
    [Fact]
    public void AHeldTimelineStandsStillAndGoesOnWhereItStood()
    {
        var page = new Page();
        var timeline = new AnimationTimeline("owner");
        page.Animations.Attach(timeline, page.Root);
        page.Root.AddClass("on");
        page.Of("outside").AddClass("on");
        page.Frame(0.1);
        page.Frame(0.1);
        Assert.Equal(0.1f, page.Opacity("a"), 5);
        timeline.Hold(page.Now);
        Assert.True(timeline.Held);
        page.Frame(0.1);
        page.Frame(0.2);
        Assert.Equal((0.1f, 0.4f), (page.Opacity("a"), page.Opacity("outside")));
        timeline.Release(0.5);
        page.Frame(0.1);
        Assert.Equal((0.2f, 0.5f), (page.Opacity("a"), page.Opacity("outside")));
        // Hidden at 0.65, then paused from the last frame (0.6): held from 0.6; a hold from 0.7 changes nothing.
        timeline.Hold(0.65);
        timeline.Hold(0.6);
        timeline.Hold(0.7);
        timeline.Release(0.9);
        page.Frame(0.4);
        Assert.Equal(0.3f, page.Opacity("a"), 5);
        timeline.Release(1.2);
        timeline.Hold(1.05);
        timeline.Release(1.0);
        page.Frame(0.1);
        Assert.Equal(0.4f, page.Opacity("a"), 5);
        Assert.False(timeline.Held);
    }

    /// <summary>
    /// Attaching takes the animations running inside the root (its pseudo-elements' too) onto the timeline
    /// where they stand, and later ones start on it; detaching gives them back to the document's clock
    /// where they stand. Outside the root nothing changes.
    /// </summary>
    [Fact]
    public void AttachedAnimationsRunOnTheTimelineAndDetachedOnTheDocumentsClock()
    {
        var page = new Page();
        page.Root.AddClass("on");
        page.Of("b").AddClass("on");
        page.Frame(0.1);
        page.Frame(0.1);
        var timeline = new AnimationTimeline("owner");
        page.Animations.Attach(timeline, page.Root);
        Assert.Same(timeline, page.Animations.TimelineAround(page.Root));
        Assert.Same(timeline, page.Animations.TimelineAround(page.Of("a")));
        Assert.Same(timeline, page.Animations.TimelineAround(page.Document.Body));
        Assert.Null(page.Animations.TimelineAround(page.Of("b")));
        timeline.Hold(page.Now);
        page.Frame(0.3);
        Assert.Equal((0.1f, 0.1f, 0.4f), (page.Opacity("a"), page.Root.BeforeStyle!.Opacity, page.Opacity("b")));
        // One starting on the held timeline starts held, at its beginning.
        page.Root.Append(page.Document.CreateElement("i")).AddClass("a");
        page.Frame(0.1);
        var late = page.Root.ElementChildren.Last();
        Assert.Equal(0f, late.Style!.Opacity, 5);
        timeline.Release(page.Now);
        page.Frame(0.1);
        Assert.Equal((0.2f, 0.2f, 0.1f), (page.Opacity("a"), page.Root.BeforeStyle!.Opacity, late.Style!.Opacity));
        // Held again, then detached: back on the document's clock from where they stood.
        timeline.Hold(page.Now);
        page.Frame(0.2);
        page.Animations.Detach(timeline);
        page.Frame(0.1);
        Assert.Equal((0.3f, 0.2f), (page.Opacity("a"), late.Style!.Opacity));
        Assert.Null(page.Animations.TimelineAround(page.Root));
        page.Frame(0.1);
        Assert.Equal(0.4f, page.Opacity("a"), 5);
    }

    /// <summary>
    /// A play-state pause on a timeline that was held before (so its time is behind the document's): paused
    /// at the timeline's time; held too, the animation stays still until both let it go, its time counted once.
    /// </summary>
    [Fact]
    public void APlayStatePauseAndAHoldTogetherCountTheTimeOnce()
    {
        var page = new Page();
        var timeline = new AnimationTimeline("owner");
        page.Animations.Attach(timeline, page.Root);
        page.Root.AddClass("on");
        page.Frame(0.1);
        page.Frame(0.1);
        timeline.Hold(page.Now);
        page.Frame(0.1);
        timeline.Release(page.Now);
        page.Frame(0.1);
        Assert.Equal(0.2f, page.Opacity("a"), 5);
        page.Of("a").AddClass("still");
        page.Frame(0.1);
        Assert.Equal(0.3f, page.Opacity("a"), 5);
        timeline.Hold(page.Now);
        page.Frame(0.1);
        page.Of("a").RemoveClass("still");
        page.Frame(0.1);
        // Paused at 0.3 into it, held from then: still 0.3.
        Assert.Equal(0.3f, page.Opacity("a"), 5);
        timeline.Release(page.Now);
        page.Frame(0.1);
        Assert.Equal(0.4f, page.Opacity("a"), 5);
        // Paused by its play-state when the timeline is detached: still where it stood, and going on from there.
        page.Of("a").AddClass("still");
        page.Frame(0.1);
        page.Animations.Detach(timeline);
        page.Of("a").RemoveClass("still");
        page.Frame(0.1);
        Assert.Equal(0.5f, page.Opacity("a"), 5);
        page.Frame(0.1);
        Assert.Equal(0.6f, page.Opacity("a"), 5);
    }

    /// <summary>Its events come on its clock: none while held; a cancel's elapsed time is the timeline's.</summary>
    [Fact]
    public void EventsComeOnTheTimelinesClock()
    {
        var page = new Page();
        var events = new List<(string Type, double Elapsed, double At)>();
        foreach (var type in new[] { "animationstart", "animationend", "animationcancel" })
            page.Document.AddEventListener(type, e => events.Add((e.Type, ((UiAnimationEvent)e).ElapsedTime, page.Now)));
        var timeline = new AnimationTimeline("owner");
        page.Animations.Attach(timeline, page.Root);
        page.Root.AddClass("on");
        page.Frame(0.05);
        page.Frame(0.05);
        timeline.Hold(page.Now);
        page.Frame(0.5);
        Assert.DoesNotContain(events, e => e.Type == "animationend");
        timeline.Release(page.Now);
        page.Frame(0.1);
        page.Frame(0.1);
        var end = Assert.Single(events, e => e.Type == "animationend");
        Assert.Equal(0.2, end.Elapsed, 6);
        Assert.Equal(0.8, end.At, 9);
        timeline.Hold(page.Now);
        page.Frame(0.3);
        page.Root.RemoveClass("on");
        // Cancelled in this frame's style update, the events come with the next frame's.
        page.Frame(0.1);
        page.Frame(0.1);
        // The element's and its ::before's, each at its timeline time when held (0.25 in), not the document's.
        var cancels = events.Where(e => e.Type == "animationcancel").ToList();
        Assert.Equal(2, cancels.Count);
        Assert.All(cancels, c => Assert.Equal(0.25, c.Elapsed, 9));
    }
}
