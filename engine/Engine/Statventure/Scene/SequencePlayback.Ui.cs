using System.Runtime.CompilerServices;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Scene;

public sealed partial record UiTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context) => UiPlayback.Create(context, this);
}

/// <summary>
/// The UI track's playback (M10-9, Q29; Docs/Scene.md, "How it works", step 6): on the bound host and the
/// elements found by id among its descendants when the sequence starts, each thing a cue sets (an
/// element's text, each of its classes, each attribute) takes the cue in effect at τ, set when that cue
/// changes (S4-8; before the first, as it was when the sequence started), in the frame's animation timing
/// update, so a class added there starts its CSS animations at that frame's time. The CSS animations
/// inside the hosts run on the sequence's clock (S6-D, <see cref="UiClock"/>); seeking doesn't reach them.
/// </summary>
internal sealed class UiPlayback : TrackPlayback
{
    /// <summary>What a cue sets on an element: its text, a class or an attribute.</summary>
    enum Aspect { Text, Class, Attribute }

    /// <summary>One thing set on one element: its cues (record indices), their times, and what it was when the sequence started.</summary>
    sealed class Setting(UiElement element, Aspect aspect, string? name, int[] cues, CueTimes times)
    {
        public UiElement Element { get; } = element;
        public Aspect Aspect { get; } = aspect;
        public string? Name { get; } = name;
        public int[] Cues { get; } = cues;
        public CueTimes Times { get; } = times;
        public int Current = -2;
        public string? Text;
        /// <summary>The element's children when the sequence started, with their text (setting a text keeps a lone text node, changing its text).</summary>
        public (UiNode Node, string? Text)[] Children = [];
        public bool HadClass;
        public string? Attribute;
    }

    readonly SequencePlayer _player;
    readonly UiElement _host;
    readonly IReadOnlyList<UiCue> _cues;
    readonly Setting[] _settings;
    UiClock _clock = null!;

    UiPlayback(SequencePlayer player, UiElement host, IReadOnlyList<UiCue> cues, Setting[] settings)
    {
        _player = player;
        _host = host;
        _cues = cues;
        _settings = settings;
    }

    /// <summary>
    /// The playback, its names resolved (S4-6): every element id among the host's descendants, and the host
    /// not the host of another playing sequence, nor inside or around one (S6-D).
    /// </summary>
    public static UiPlayback Create(PlaybackContext context, UiTrack track)
    {
        var host = (UiElement)context.Role(track.Host);
        if (host.Document?.Animations.TimelineAround(host) is { Owner: SequencePlayer other } && other != context.Player)
            throw context.Problem($"its host {track.Host} is, holds or is inside a UI host of the sequence {other.Sequence.Name}, which is playing");
        var elements = new Dictionary<string, UiElement>();
        var groups = new List<(UiElement Element, Aspect Aspect, string? Name, List<int> Cues)>();
        for (var i = 0; i < track.Cues.Count; i++)
        {
            var cue = track.Cues[i];
            UiElement element;
            if (cue.Element is not { } id)
                element = host;
            else if (!elements.TryGetValue(id, out element!))
                elements[id] = element = host.Descendants().FirstOrDefault(e => e.Id == id)
                    ?? throw context.Problem($"its host {track.Host} has no element {id}");
            var (aspect, name) = cue.Action switch
            {
                UiCueAction.SetText => (Aspect.Text, (string?)null),
                UiCueAction.AddClass or UiCueAction.RemoveClass => (Aspect.Class, cue.Name),
                _ => (Aspect.Attribute, cue.Name),
            };
            var group = groups.FindIndex(g => g.Element == element && g.Aspect == aspect && g.Name == name);
            if (group < 0)
                groups.Add((element, aspect, name, [i]));
            else
                groups[group].Cues.Add(i);
        }
        var settings = groups.Select(g => new Setting(g.Element, g.Aspect, g.Name, [.. g.Cues], new CueTimes(g.Cues.Select(i => context.Time(track.Cues[i])).ToArray()))).ToArray();
        return new UiPlayback(context.Player, host, track.Cues, settings);
    }

    public override void Begin()
    {
        foreach (var setting in _settings)
            switch (setting.Aspect)
            {
                case Aspect.Text:
                    setting.Text = setting.Element.TextContent;
                    setting.Children = [.. setting.Element.Children.Select(c => (c, (c as UiText)?.Text))];
                    break;
                case Aspect.Class:
                    setting.HadClass = setting.Element.HasClass(setting.Name!);
                    break;
                default:
                    setting.Attribute = setting.Element.GetAttribute(setting.Name!);
                    break;
            }
        _clock = UiClock.Of(_player);
        _clock.Attach(_host);
    }

    public override void Apply(double tau, bool force)
    {
        List<(int Order, Setting Setting, int Cue)>? changed = null;
        foreach (var setting in _settings)
        {
            var at = setting.Times.At(tau);
            if (at == setting.Current && !force)
                continue;
            setting.Current = at;
            var cue = at < 0 ? -1 : setting.Cues[at];
            (changed ??= []).Add((cue, setting, cue));
        }
        if (changed is null)
            return;
        // In record order of the cues now in effect (what goes back to how it was first).
        foreach (var (_, setting, cue) in changed.OrderBy(c => c.Order))
            if (cue < 0)
                Restore(setting);
            else
                Set(setting.Element, _cues[cue]);
    }

    static void Set(UiElement element, UiCue cue)
    {
        switch (cue.Action)
        {
            case UiCueAction.SetText:
                element.TextContent = cue.Value!;
                break;
            case UiCueAction.AddClass:
                element.AddClass(cue.Name!);
                break;
            case UiCueAction.RemoveClass:
                element.RemoveClass(cue.Name!);
                break;
            case UiCueAction.SetAttribute:
                element.SetAttribute(cue.Name!, cue.Value!);
                break;
            default:
                element.RemoveAttribute(cue.Name!);
                break;
        }
    }

    /// <summary>Before its first cue a setting is as it was when the sequence started (text: its children then, if the text differs).</summary>
    static void Restore(Setting setting)
    {
        var element = setting.Element;
        switch (setting.Aspect)
        {
            case Aspect.Text:
                if (element.TextContent == setting.Text)
                    return;
                element.ClearChildren();
                foreach (var (child, text) in setting.Children)
                {
                    if (child is UiText node)
                        node.Text = text!;
                    element.Append(child);
                }
                break;
            case Aspect.Class:
                element.ToggleClass(setting.Name!, setting.HadClass);
                break;
            default:
                if (setting.Attribute is { } value)
                    element.SetAttribute(setting.Name!, value);
                else
                    element.RemoveAttribute(setting.Name!);
                break;
        }
    }

    public override void Paused(bool paused) => _clock.Paused(paused);

    public override void VisibilityChanged(bool hidden) => _clock.VisibilityChanged(hidden);

    public override void Ended(bool skipped) => _clock.End();
}

/// <summary>
/// A playing sequence's clock for the CSS animations inside its UI hosts (S6-D; one per player, shared by
/// its UI tracks): an <see cref="AnimationTimeline"/> per document, held while the sequence's clock stands
/// still. A pause holds it from the document's last frame (the last its clock moved in) to the last frame
/// it was paused in (a resume's last frame); a hidden document from the moment it went hidden to the moment
/// it was shown, as the player drops that time (M10-7). Several reasons at once hold it through the union
/// of their times. At the end it is detached: the animations go on on the document's clock from where
/// they stand.
/// </summary>
internal sealed class UiClock
{
    static readonly ConditionalWeakTable<SequencePlayer, UiClock> s_clocks = [];

    /// <summary>A reason the clock stands still and the document time it began at.</summary>
    sealed class Held(UiDocument document, AnimationTimeline timeline)
    {
        public UiDocument Document { get; } = document;
        public AnimationTimeline Timeline { get; } = timeline;
        public readonly Dictionary<int, double> Since = [];

        public void Begin(int reason, double at)
        {
            if (!Since.TryAdd(reason, at))
                return;
            Timeline.Hold(at);
        }

        public void End(int reason, double at)
        {
            if (!Since.Remove(reason))
                return;
            if (Since.Count == 0)
            {
                Timeline.Release(at);
                return;
            }
            // The other reason began after this one ended: the animations ran in between.
            var next = Since.Values.Min();
            if (next > at)
            {
                Timeline.Release(at);
                Timeline.Hold(next);
            }
        }

        /// <summary>The document's last frame (before the first: its clock).</summary>
        public double LastFrame => double.IsNaN(Document.Animations.LastFrameTime) ? Document.Animations.Time : Document.Animations.LastFrameTime;

        public double Now => Document.EventLoop.Time;
    }

    const int Pause = 0, Hide = 1;
    readonly SequencePlayer _player;
    readonly List<Held> _held = [];
    bool _paused, _hidden, _ended;

    UiClock(SequencePlayer player) => _player = player;

    public static UiClock Of(SequencePlayer player) => s_clocks.GetValue(player, p => new UiClock(p));

    /// <summary>Puts a host's subtree on the clock (its document's timeline, made with the first host there).</summary>
    public void Attach(UiElement host)
    {
        if (host.Document is not { } document)
            return;
        var held = _held.FirstOrDefault(h => h.Document == document);
        if (held is null)
            _held.Add(held = new Held(document, new AnimationTimeline(_player)));
        document.Animations.Attach(held.Timeline, host);
    }

    public void Paused(bool paused)
    {
        if (_ended || paused == _paused)
            return;
        _paused = paused;
        foreach (var held in _held)
            if (paused)
                held.Begin(Pause, held.LastFrame);
            else
                held.End(Pause, held.LastFrame);
    }

    public void VisibilityChanged(bool hidden)
    {
        if (_ended || hidden == _hidden)
            return;
        _hidden = hidden;
        foreach (var held in _held)
            if (hidden)
                held.Begin(Hide, held.Now);
            else
                held.End(Hide, held.Now);
    }

    /// <summary>The sequence ended: released where it stands, then detached.</summary>
    public void End()
    {
        if (_ended)
            return;
        _ended = true;
        foreach (var held in _held)
        {
            held.End(Pause, held.LastFrame);
            held.End(Hide, held.Now);
            held.Document.Animations.Detach(held.Timeline);
        }
        s_clocks.Remove(_player);
    }
}
