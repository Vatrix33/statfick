using Statventure.Animation;
using Statventure.Audio;

namespace Statventure.Scene;

public sealed partial record AudioTrack
{
    internal override TrackPlayback CreatePlayback(PlaybackContext context) => AudioPlayback.Create(context, Cues);
}

/// <summary>
/// The audio track's playback (M10-8, Q27, Q28; Docs/Scene.md, "How it works", step 6): its cues scheduled
/// ahead on the audio clock of the audio system the bindings give (S6-A; none: nothing plays). In the first
/// frame every cue is sent in one call, which the audio thread anchors to the quantum it applies it in:
/// a cue at clock time c is due at the anchor plus (c − the frame's clock) / the rate. A pause, the document
/// hidden, a seek or a rate change cancels the cues still pending; they are sent again in the next frame
/// the clock moves in, anchored anew (after a pause, those that hadn't happened; after a seek, every cue
/// after the new time: a seek fires nothing and re-arms what follows). At a skip the pending cues are
/// cancelled and the end's music cue, if it hadn't happened yet, is applied at once with music.js's fades
/// (Q28). In the reduced variant, cues cut out are treated as at a skip: effects are dropped, the last
/// music cue of each cut part is applied at the cut. The sounds aren't stretched by the rate. Every
/// decision that depends on what the audio thread did (which cues happened) is made on the audio thread.
/// </summary>
internal sealed class AudioPlayback : TrackPlayback
{
    /// <summary>A cue as it is sent: its time on the played variant's clock and what it plays.</summary>
    sealed record Cue(double Clock, AudioCueKind Kind, string? Name, SoundEffects.Registered? Effect, CompiledTheme? Theme, double Volume, double Pan, double Pitch);

    /// <summary>What the next frame the clock moves in sends: nothing, the cues that hadn't happened from a clock time on, or every cue after it (a seek).</summary>
    enum Sending { None, From, Rearm }

    readonly AudioSystem _audio;
    readonly SequencePlayer _player;
    readonly Cue[] _cues;
    /// <summary>The end's music cue (the last music or stop on the timeline), or −1.</summary>
    readonly int _endMusic;
    /// <summary>Each cue's last handle (on the audio thread only).</summary>
    readonly ScheduledSound?[] _handles;
    Sending _sending = Sending.From;
    double _from, _rate = 1;
    bool _paused, _ended;

    AudioPlayback(AudioSystem audio, SequencePlayer player, Cue[] cues, int endMusic)
    {
        _audio = audio;
        _player = player;
        _cues = cues;
        _endMusic = endMusic;
        _handles = new ScheduledSound?[cues.Length];
    }

    /// <summary>
    /// The playback, its effects and themes checked against the audio system (S4-6, S6-A): an effect it
    /// hasn't registered or a theme its music hasn't is an <see cref="ArgumentException"/>; without an
    /// audio system the track plays nothing.
    /// </summary>
    public static TrackPlayback Create(PlaybackContext context, IReadOnlyList<AudioCue> cues)
    {
        if (context.Bindings.AudioSystem is not { } audio)
            return NoPlayback.Instance;
        var effects = new SoundEffects.Registered?[cues.Count];
        var themes = new CompiledTheme?[cues.Count];
        for (var i = 0; i < cues.Count; i++)
        {
            var cue = cues[i];
            if (cue.Kind == AudioCueKind.Effect)
                effects[i] = audio.Effects.Find(cue.Name!) ?? throw context.Problem($"cue {i} plays the effect {cue.Name}, which the audio system hasn't registered");
            else if (cue.Kind == AudioCueKind.Music)
                themes[i] = audio.Music.Library.Compiled(cue.Name!) ?? throw context.Problem($"cue {i} plays the theme {cue.Name}, which the audio system's music hasn't");
        }

        // The cues in timeline order (equal times in record order), on the played variant's clock.
        var player = context.Player;
        var times = cues.Select(c => context.Time(c)).ToArray();
        var order = Enumerable.Range(0, cues.Count).OrderBy(i => times[i]).ThenBy(i => i).ToArray();
        var spans = player.Reduced ? context.Sequence.Reduced! : [new SequenceSpan(0, context.Sequence.Duration)];
        var starts = new double[spans.Count];
        for (var s = 1; s < spans.Count; s++)
            starts[s] = starts[s - 1] + spans[s - 1].Length;
        var sent = new List<(int Index, Cue Cue, bool Played)>();
        foreach (var i in order)
        {
            var (clock, played) = ClockOf(times[i], spans, starts, player.Duration);
            var cue = cues[i];
            sent.Add((i, new Cue(clock, cue.Kind, cue.Name, effects[i], themes[i], cue.Volume, cue.Pan, cue.Pitch), played));
        }
        // Cut out (Q19, as at a skip): no effect, and of a cut part's music cues only the last, at the cut.
        var kept = new List<Cue>();
        for (var k = 0; k < sent.Count; k++)
        {
            var (_, cue, played) = sent[k];
            if (played)
                kept.Add(cue);
            else if (cue.Kind != AudioCueKind.Effect && !sent.Skip(k + 1).Any(later => !later.Played && later.Cue.Kind != AudioCueKind.Effect && later.Cue.Clock == cue.Clock))
                kept.Add(cue);
        }
        var cueArray = kept.ToArray();
        var endMusic = Array.FindLastIndex(cueArray, c => c.Kind != AudioCueKind.Effect);
        return new AudioPlayback(audio, player, cueArray, endMusic);
    }

    /// <summary>
    /// Where on the played variant's clock the timeline time <paramref name="tau"/> is reached: in the first
    /// span that plays it, its start plus the time into it; in a part cut out, the start of the span after
    /// it (the cut), or the end after the last span (not played).
    /// </summary>
    static (double Clock, bool Played) ClockOf(double tau, IReadOnlyList<SequenceSpan> spans, double[] starts, double duration)
    {
        for (var s = 0; s < spans.Count; s++)
            if (tau >= spans[s].From - AnimationStrip.Epsilon && tau <= spans[s].To + AnimationStrip.Epsilon)
                return (starts[s] + Math.Clamp(tau - spans[s].From, 0, spans[s].To - spans[s].From), true);
        for (var s = 0; s < spans.Count; s++)
            if (spans[s].From > tau)
                return (starts[s], false);
        return (duration, false);
    }

    public override void Begin()
    {
        // The themes' rooms ready now (on the game's thread), not when the audio thread starts them.
        foreach (var cue in _cues)
            if (cue.Kind == AudioCueKind.Music)
                _audio.Music.Prepared(cue.Name!);
    }

    public override void Apply(double tau, bool force)
    {
        // No frame comes while the document is hidden. The end's frame sends at the end instead (Ended): a skip
        // cancels, the end as played sends (sending here, the audio thread could start what the skip cancels).
        if (_ended || _sending == Sending.None || _paused || !(_rate > 0) || _player.Time >= _player.Duration)
            return;
        SendPending();
    }

    void SendPending()
    {
        var (sending, from, clock, rate) = (_sending, _from, _player.Time, _rate);
        _sending = Sending.None;
        _audio.Post(() => Send(sending, from, clock, rate));
    }

    public override void Paused(bool paused)
    {
        if (_ended)
            return;
        _paused = paused;
        if (paused)
            Hold();
    }

    public override void VisibilityChanged(bool hidden)
    {
        if (!_ended && hidden)
            Hold();
    }

    public override void RateChanged(double rate)
    {
        if (_ended)
            return;
        _rate = rate;
        Hold();
    }

    public override void Jumped(double tau)
    {
        if (_ended)
            return;
        _audio.Post(CancelPending);
        _sending = Sending.Rearm;
        _from = _player.Time;
    }

    public override void Ended(bool skipped)
    {
        if (_ended)
            return;
        _ended = true;
        if (skipped)
            _audio.Post(Skip);
        else if (_sending != Sending.None)
            SendPending();
    }

    /// <summary>The clock stops (a pause, the document hidden) or changes pace: the pending cues are cancelled, to be sent again from here.</summary>
    void Hold()
    {
        _audio.Post(CancelPending);
        if (_sending == Sending.None)
        {
            _sending = Sending.From;
            _from = _player.Time;
        }
    }

    // ---------- On the audio thread ----------

    /// <summary>
    /// The cues sent, anchored to the start of this quantum, which is the clock time <paramref name="clock"/>:
    /// after a pause those from <paramref name="from"/> on that hadn't happened, after a seek every one after it.
    /// </summary>
    void Send(Sending sending, double from, double clock, double rate)
    {
        var anchor = _audio.Graph.CurrentTime;
        for (var i = 0; i < _cues.Length; i++)
        {
            var cue = _cues[i];
            var skip = sending == Sending.Rearm
                ? cue.Clock <= from + AnimationStrip.Epsilon
                : cue.Clock < from - AnimationStrip.Epsilon || _handles[i] is { State: ScheduledSoundState.Done };
            if (skip)
                continue;
            var time = anchor + (cue.Clock - clock) / rate;
            switch (cue.Kind)
            {
                case AudioCueKind.Effect:
                    var effect = _handles[i] = _audio.Effects.NewSound(time);
                    _audio.Effects.Schedule(effect, cue.Effect!, cue.Volume, cue.Pan, cue.Pitch);
                    break;
                case AudioCueKind.Music:
                    var music = _handles[i] = _audio.Music.NewSound(time);
                    _audio.Music.Schedule(music, cue.Name!, cue.Theme!);
                    break;
                default:
                    var stop = _handles[i] = _audio.Music.NewSound(time);
                    _audio.Music.ScheduleStop(stop);
                    break;
            }
        }
    }

    void CancelPending()
    {
        foreach (var handle in _handles)
            handle?.CancelNow();
    }

    /// <summary>A skip: the pending cues cancelled, the end's music cue applied now if it hadn't happened (Q28).</summary>
    void Skip()
    {
        CancelPending();
        if (_endMusic < 0 || _handles[_endMusic] is { State: ScheduledSoundState.Done })
            return;
        var cue = _cues[_endMusic];
        _audio.Music.ApplyNow(cue.Kind == AudioCueKind.Music ? cue.Name : null, cue.Theme);
    }
}
