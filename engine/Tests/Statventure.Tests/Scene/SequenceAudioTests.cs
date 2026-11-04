using Statventure.Audio;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Audio;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Tests.Scene;

/// <summary>
/// A scene in a document whose frames run its sequences, and an audio system on an offline graph rendered
/// <see cref="Lead"/> seconds ahead of the document's clock after each frame (behind it when negative), as
/// the audio thread renders ahead of the picture. <see cref="Anchor"/> is where a call made in the last
/// frame lands: the start of the next quantum (Q27).
/// </summary>
sealed class AudioBench
{
    public const float Rate = 22050;
    const int Q = AudioGraph.RenderQuantumFrames;
    readonly List<float> _rendered = [];

    /// <summary>A tone sweeping down and a noise burst (with ranges, so the draws show); a long hum of two voices at most.</summary>
    public static readonly EffectPatch Ping = new()
    {
        Layers =
        [
            new EffectLayer { Source = EffectSource.Triangle, Frequency = 880, FrequencyTo = 330, Attack = 0.003, Length = 0.12 },
            new EffectLayer { Source = EffectSource.Noise, Filter = new EffectFilter { Type = EffectFilterType.Bandpass, Frequency = 2400, Q = 3 }, Delay = 0.02, Level = 0.4, Length = 0.08 },
        ],
        Level = 0.6,
        Pitch = new EffectRange(0.8, 1.25),
        Volume = new EffectRange(0.5, 1),
    };

    public static readonly EffectPatch Hum = new() { Layers = [new EffectLayer { Source = EffectSource.Sine, Frequency = 300, Length = 0.4 }], MaxVoices = 2, Level = 0.3 };

    public AudioBench(bool reducedMotion = false, double lead = 0.02)
    {
        Lead = lead;
        Document = new UiDocument(TestFonts.Library);
        Document.SetViewport(300, 200, reducedMotion);
        Markup.SetInnerHtml(Document.Body, """<div id="card"><p id="line">Before</p></div>""");
        Scene = new SceneView(new View3D(null!));
        Document.AddFrameClient(Scene);
        Document.Update(0);
        Audio = NewAudio();
    }

    /// <summary>An audio system as the bench's (the same rate, music and patches), for the calls made by hand.</summary>
    public static AudioSystem NewAudio()
    {
        var audio = new AudioSystem(new AudioGraph(Rate, 2), TestMusic.Library);
        foreach (var (id, patch) in new[] { ("ping", Ping), ("hum", Hum), ("chime", Ping with { Level = 0.4 }), ("thud", Hum with { MaxVoices = 4 }) })
            audio.Effects.Register(id, patch);
        return audio;
    }

    public UiDocument Document { get; }
    public SceneView Scene { get; }
    public AudioSystem Audio { get; }
    public double Lead { get; }
    public double Now { get; private set; }
    /// <summary>The audio time a call made in the last frame is applied at (the next quantum's start).</summary>
    public double Anchor { get; private set; }
    public int Quanta => _rendered.Count / (2 * Q);
    public float[] Rendered => [.. _rendered];

    public SequencePlayer Play(Sequence sequence, SequenceBindings? bindings = null) =>
        Scene.Play(sequence, bindings ?? new SequenceBindings().Audio(Audio), Document);

    /// <summary>A frame <paramref name="seconds"/> after the last, then the audio rendered to <see cref="Lead"/> past it.</summary>
    public void Frame(double seconds)
    {
        Now += seconds;
        Document.Update(Now);
        Anchor = Audio.Time;
        RenderTo(Now + Lead);
    }

    /// <summary>Frames of <paramref name="seconds"/> until <paramref name="until"/> (after each, the check given).</summary>
    public void FramesUntil(double until, double seconds = 1 / 60.0, Action? each = null)
    {
        for (var frames = 0; Now < until - 1e-9; frames++)
        {
            Assert.True(frames < 2000, "too many frames");
            Frame(seconds);
            each?.Invoke();
        }
    }

    /// <summary><paramref name="count"/> frames of 1/60 s.</summary>
    public void Frames(int count)
    {
        for (var i = 0; i < count; i++)
            Frame(1 / 60.0);
    }

    /// <summary>Renders quanta until the audio clock reaches <paramref name="time"/>.</summary>
    public void RenderTo(double time)
    {
        var bus = new AudioBus(2, Q);
        while (Audio.Graph.CurrentTime < time)
        {
            Audio.BeforeQuantum();
            Audio.Graph.RenderQuantum(bus);
            _rendered.AddRange(bus.Channel(0).Data);
            _rendered.AddRange(bus.Channel(1).Data);
        }
    }

    /// <summary><paramref name="hand"/> rendered for as many quanta as the bench has.</summary>
    public float[] RenderByHand(AudioSystem hand)
    {
        var bus = new AudioBus(2, Q);
        var result = new List<float>();
        for (var q = 0; q < Quanta; q++)
        {
            hand.BeforeQuantum();
            hand.Graph.RenderQuantum(bus);
            result.AddRange(bus.Channel(0).Data);
            result.AddRange(bus.Channel(1).Data);
        }
        return [.. result];
    }

    /// <summary>The bench's render equals the calls made by hand, sample for sample, and has sound.</summary>
    public void SoundsAs(AudioSystem hand)
    {
        var expected = RenderByHand(hand);
        var actual = Rendered;
        for (var i = 0; i < expected.Length; i++)
            if (expected[i] != actual[i])
                Assert.Fail($"quantum {i / (2 * Q)} ({(i % (2 * Q) < Q ? "left" : "right")}) frame {i % Q} differs: {expected[i]:R} by hand, {actual[i]:R} from the sequence " +
                    $"(by hand first sound at quantum {Array.FindIndex(expected, x => x != 0) / (2 * Q)}, from the sequence {Array.FindIndex(actual, x => x != 0) / (2 * Q)})");
        Assert.Contains(actual, s => s != 0);
    }
}

/// <summary>
/// The audio track (M10-8, Q27, Q28; Docs/Scene.md, "How it works", step 6, S6-A–S6-C): each cue renders sample
/// for sample as the same call made by hand at the anchor (the quantum the sequence's first call is applied
/// in) plus its time; a pause, a hidden document, a seek, a rate change cancel the pending cues and send them
/// again anchored anew; a skip cancels them and applies the end's music cue; the reduced variant's cut parts
/// are as at a skip.
/// </summary>
[Collection("GPU")]
public class SequenceAudioTests(GpuFixture gpu)
{
    const double Frame = 1 / 60.0;

    static Sequence Sounds(double duration, Action<SequenceBuilder.AudioBuilder> cues, Action<SequenceBuilder.ReducedBuilder>? reduced = null)
    {
        var builder = new SequenceBuilder("sounds", duration).Audio(cues);
        if (reduced != null)
            builder.Reduced(reduced);
        return builder.Build();
    }

    /// <summary>
    /// Effects (with their volume, pan and pitch), a theme and the music's stop at their times, the audio
    /// already a while along before the sequence starts and rendering behind the picture: due at the anchor
    /// plus their times; the end leaves the last one, still pending, to play.
    /// </summary>
    [Fact]
    public void EachCueSoundsAsTheSameCallAtTheAnchorPlusItsTime()
    {
        var bench = new AudioBench(lead: -0.03);
        bench.RenderTo(0.1);
        var player = bench.Play(Sounds(1.2, a => a.Effect(0, "ping").Effect(0.2345, "ping", 0.7, 0.3, 1.2).Effect(0.5, "hum").Music(0.3, "alder-vale")
            .Effect(0.71, "hum", 0.5, -1).StopMusic(1.0).Effect(1.2, "ping")));
        bench.Frame(Frame);
        var anchor = bench.Anchor;
        // The anchor is the quantum the first frame's call is applied in, not the start of the audio.
        Assert.True(anchor > 0.1 - 0.03);
        bench.FramesUntil(1.5);
        Assert.True(player.Ended);

        var hand = AudioBench.NewAudio();
        hand.Effects.PlayAt(anchor, "ping");
        hand.Effects.PlayAt(anchor + 0.2345, "ping", 0.7, 0.3, 1.2);
        hand.Effects.PlayAt(anchor + 0.5, "hum");
        hand.Music.PlayAt(anchor + 0.3, "alder-vale");
        hand.Effects.PlayAt(anchor + 0.71, "hum", 0.5, -1);
        hand.Music.StopAt(anchor + 1.0);
        hand.Effects.PlayAt(anchor + 1.2, "ping");
        bench.SoundsAs(hand);
    }

    /// <summary>
    /// A pause cancels the cues still pending (one the audio already started, ahead of the picture, plays
    /// once); resuming sends the rest again in the next frame the clock moves in, anchored to its quantum at
    /// that frame's clock.
    /// </summary>
    [Fact]
    public void APauseCancelsThePendingCuesAndResumingSendsThemAgainFromThere()
    {
        var bench = new AudioBench(lead: 0.05);
        var player = bench.Play(Sounds(1, a => a.Effect(0.1, "ping").Effect(0.27, "ping", 1, 0.5).Effect(0.45, "hum").Effect(0.6, "ping", 0.8, -0.5).Effect(0.8, "hum")));
        bench.Frame(Frame);
        var first = bench.Anchor;
        bench.FramesUntil(0.25);
        // The audio is at 0.30: the cue at 0.27 has started.
        player.Pause();
        var paused = player.Time;
        bench.FramesUntil(0.7);
        player.Resume();
        bench.Frame(Frame);
        var (second, clock) = (bench.Anchor, player.Time);
        Assert.Equal(paused + Frame, clock, 9);
        bench.FramesUntil(1.6);
        Assert.True(player.Ended);

        var hand = AudioBench.NewAudio();
        hand.Effects.PlayAt(first + 0.1, "ping");
        hand.Effects.PlayAt(first + 0.27, "ping", 1, 0.5);
        hand.Effects.PlayAt(second + (0.45 - clock), "hum");
        hand.Effects.PlayAt(second + (0.6 - clock), "ping", 0.8, -0.5);
        hand.Effects.PlayAt(second + (0.8 - clock), "hum");
        bench.SoundsAs(hand);
    }

    /// <summary>
    /// The hidden document: its time is dropped from the sequence's clock (M10-7), and the cues wait too
    /// even when the sound plays on while it is hidden (cancelled at the hiding, sent again after the
    /// showing); with the sound paused while hidden, as the application pauses it, the same.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CuesWaitWhileTheDocumentIsHidden(bool soundPaused)
    {
        var bench = new AudioBench(lead: 0.02);
        var player = bench.Play(Sounds(1, a => a.Effect(0.1, "ping").Effect(0.4, "hum").Effect(0.65, "ping", 1, 1)));
        bench.Frame(Frame);
        var first = bench.Anchor;
        bench.FramesUntil(0.3);
        var before = player.Time;
        // Hidden at the document's last update (a host hides it at its update's time).
        bench.Document.SetHidden(true);
        // 0.9 s hidden: no frames; the sound either plays on (rendering) or is paused (not).
        var shownAt = bench.Now + 0.9;
        if (!soundPaused)
            bench.RenderTo(shownAt + bench.Lead);
        bench.Document.SetHidden(false, shownAt);
        bench.Frame(shownAt + 0.006 - bench.Now);
        var (second, clock) = (bench.Anchor, player.Time);
        Assert.Equal(before + 0.006, clock, 9);
        bench.FramesUntil(bench.Now + 1);
        Assert.True(player.Ended);

        var hand = AudioBench.NewAudio();
        hand.Effects.PlayAt(first + 0.1, "ping");
        hand.Effects.PlayAt(second + (0.4 - clock), "hum");
        hand.Effects.PlayAt(second + (0.65 - clock), "ping", 1, 1);
        bench.SoundsAs(hand);
    }

    /// <summary>
    /// A seek fires nothing (S4-5): the cues jumped over don't play, one at the very time sought counts as
    /// passed, those after are sent in the next frame the clock moves in; seeking back (here while paused)
    /// sends again every cue after it, those that played included, once it goes on.
    /// </summary>
    [Fact]
    public void ASeekPlaysNothingJumpedOverAndReArmsWhatFollows()
    {
        var bench = new AudioBench(lead: 0.02);
        var player = bench.Play(Sounds(1.5, a => a.Effect(0.1, "ping").Effect(0.35, "hum").Effect(0.6, "ping", 1, -1).Effect(0.9, "hum", 0.5)));
        bench.Frame(Frame);
        var first = bench.Anchor;
        bench.FramesUntil(0.2);
        player.Seek(0.35);
        bench.Frame(Frame);
        var (second, afterSeek) = (bench.Anchor, player.Time);
        bench.FramesUntil(0.65);
        Assert.True(player.TimelineTime > 0.6 && player.TimelineTime < 0.85);
        player.Seek(0.05);
        player.Pause();
        bench.FramesUntil(0.9);
        player.Resume();
        bench.Frame(Frame);
        var (third, afterBack) = (bench.Anchor, player.Time);
        Assert.Equal(0.05 + Frame, afterBack, 9);
        bench.FramesUntil(2.6);
        Assert.True(player.Ended);

        var hand = AudioBench.NewAudio();
        hand.Effects.PlayAt(first + 0.1, "ping");
        // After the seek to 0.35: the hum there never plays; 0.6 does, and 0.9 is cancelled by the seek back.
        hand.Effects.PlayAt(second + (0.6 - afterSeek), "ping", 1, -1);
        // After the seek back to 0.05: everything after it.
        hand.Effects.PlayAt(third + (0.1 - afterBack), "ping");
        hand.Effects.PlayAt(third + (0.35 - afterBack), "hum");
        hand.Effects.PlayAt(third + (0.6 - afterBack), "ping", 1, -1);
        hand.Effects.PlayAt(third + (0.9 - afterBack), "hum", 0.5);
        bench.SoundsAs(hand);
    }

    /// <summary>
    /// With the sound behind the picture, a cue the clock had passed but the sound hadn't reached when the
    /// pause came is dropped: the cues are sent again from the pause's time on.
    /// </summary>
    [Fact]
    public void ACueTheClockHadPassedButTheSoundHadntReachedIsDroppedAtAPause()
    {
        var bench = new AudioBench(lead: -0.05);
        var player = bench.Play(Sounds(1, a => a.Effect(0.1, "ping").Effect(0.22, "ping", 1, 0.5).Effect(0.4, "hum")));
        bench.Frame(Frame);
        var first = bench.Anchor;
        bench.FramesUntil(0.25);
        Assert.True(player.Time > 0.22);
        player.Pause();
        bench.Frames(5);
        player.Resume();
        bench.Frame(Frame);
        var (second, clock) = (bench.Anchor, player.Time);
        bench.FramesUntil(1.5);

        var hand = AudioBench.NewAudio();
        hand.Effects.PlayAt(first + 0.1, "ping");
        hand.Effects.PlayAt(second + (0.4 - clock), "hum");
        bench.SoundsAs(hand);
    }

    /// <summary>The end as played sends what was still to send (the frame resuming a pause reaches the end): the last cue plays then.</summary>
    [Fact]
    public void TheEndAsPlayedSendsWhatIsStillToSend()
    {
        var bench = new AudioBench(lead: 0.02);
        var player = bench.Play(Sounds(0.5, a => a.Effect(0.2, "ping").Effect(0.5, "hum")));
        bench.Frame(Frame);
        var first = bench.Anchor;
        bench.FramesUntil(0.45);
        player.Pause();
        bench.Frames(3);
        player.Resume();
        bench.Frame(0.1);
        Assert.True(player.Ended);
        var end = bench.Anchor;
        bench.FramesUntil(1.2);

        var hand = AudioBench.NewAudio();
        hand.Effects.PlayAt(first + 0.2, "ping");
        hand.Effects.PlayAt(end, "hum");
        bench.SoundsAs(hand);
    }

    /// <summary>The rate: cues due at the anchor plus their time over the rate; a change cancels and sends again at the new rate. The sounds aren't stretched.</summary>
    [Fact]
    public void TheRateSpacesTheCuesAndAChangeSendsThemAgain()
    {
        var bench = new AudioBench(lead: 0.01);
        var player = bench.Play(Sounds(2, a => a.Effect(0.2, "ping").Effect(0.5, "hum").Effect(1, "ping", 1, 0.5).Effect(1.6, "hum")));
        player.Rate = 2;
        bench.Frame(Frame);
        var first = bench.Anchor;
        bench.FramesUntil(0.35);
        player.Rate = 0.5;
        bench.Frame(Frame);
        var (second, clock) = (bench.Anchor, player.Time);
        bench.FramesUntil(4);
        Assert.True(player.Ended);

        var hand = AudioBench.NewAudio();
        hand.Effects.PlayAt(first + 0.2 / 2, "ping");
        hand.Effects.PlayAt(first + 0.5 / 2, "hum");
        hand.Effects.PlayAt(second + (1 - clock) / 0.5, "ping", 1, 0.5);
        hand.Effects.PlayAt(second + (1.6 - clock) / 0.5, "hum");
        bench.SoundsAs(hand);
    }

    /// <summary>
    /// A skip cancels the pending cues and applies the end's music cue at once with the usual fades (Q28),
    /// only if it hadn't happened: a theme still to come is switched to at the skip, a stop still to come
    /// stops the music then, and a music cue already applied isn't applied again (the game's change after it
    /// stands).
    /// </summary>
    [Theory]
    [InlineData("theme to come")]
    [InlineData("stop to come")]
    [InlineData("theme applied")]
    public void ASkipCancelsThePendingCuesAndAppliesTheEndsMusicCue(string kind)
    {
        var bench = new AudioBench(lead: 0.02);
        var sequence = kind switch
        {
            "theme to come" => Sounds(2, a => a.Music(0, "alder-vale").Effect(0.1, "ping").Effect(0.5, "hum").Music(1.2, "combat")),
            "stop to come" => Sounds(2, a => a.Music(0, "alder-vale").Effect(0.1, "ping").Effect(0.5, "hum").StopMusic(1.5)),
            _ => Sounds(2, a => a.Effect(0.1, "ping").Music(0.2, "combat").Effect(0.5, "hum")),
        };
        var player = bench.Play(sequence);
        bench.Frame(Frame);
        var first = bench.Anchor;
        bench.FramesUntil(0.3);
        if (kind == "theme applied")
            bench.Audio.Music.Play("alder-vale");
        bench.Frame(Frame);
        // The game's own call made before that frame: applied in its quantum.
        var game = bench.Anchor;
        Assert.True(player.Skip());
        var skipped = bench.Audio.Time;
        bench.FramesUntil(1.2);

        var hand = AudioBench.NewAudio();
        hand.Effects.PlayAt(first + 0.1, "ping");
        switch (kind)
        {
            case "theme to come":
                hand.Music.PlayAt(first, "alder-vale");
                hand.Music.PlayAt(skipped, "combat");
                break;
            case "stop to come":
                hand.Music.PlayAt(first, "alder-vale");
                hand.Music.StopAt(skipped);
                break;
            default:
                hand.Music.PlayAt(first + 0.2, "combat");
                // The game's own switch, made before the frame before the skip.
                hand.Music.PlayAt(game, "alder-vale");
                break;
        }
        bench.SoundsAs(hand);
    }

    /// <summary>
    /// The reduced variant (M10-4, Q19): cues in its spans at their place on its clock, a cue at a held
    /// moment at the hold's start; in the parts cut out effects are dropped and only the last music cue of
    /// each is applied, at the cut (after the last span: at the end), as at a skip.
    /// </summary>
    [Fact]
    public void TheReducedVariantPlaysItsSpansCuesAndTheCutPartsAsAtASkip()
    {
        var bench = new AudioBench(reducedMotion: true, lead: 0.02);
        var player = bench.Play(Sounds(1.2, a => a.Effect(0.1, "ping").Music(0.35, "combat").Effect(0.5, "hum").Music(0.6, "alder-vale").Effect(0.8, "ping", 1, 0.5)
            .StopMusic(1.0).Effect(1.1, "hum"), r => r.Span(0, 0.3).Hold(0.8, 0.5)));
        Assert.True(player.Reduced);
        bench.Frame(Frame);
        var first = bench.Anchor;
        bench.FramesUntil(1.6);
        Assert.True(player.Ended);

        var hand = AudioBench.NewAudio();
        hand.Effects.PlayAt(first + 0.1, "ping");
        // Cut out (0.3–0.8): the hum dropped, of the two themes only the last, at the cut (clock 0.3).
        hand.Music.PlayAt(first + 0.3, "alder-vale");
        // The held moment 0.8 at the hold's start.
        hand.Effects.PlayAt(first + 0.3, "ping", 1, 0.5);
        // After the last span: the stop at the end (clock 0.8), the hum dropped.
        hand.Music.StopAt(first + 0.8);
        bench.SoundsAs(hand);
    }

    /// <summary>
    /// Of a cut part's music cues only the last is applied, at the cut (Q19, as at a skip): here the theme
    /// playing then, which does nothing, though an earlier cue of the cut part names another theme (applied
    /// too, it would have the playing theme start again from its start).
    /// </summary>
    [Fact]
    public void OnlyTheLastMusicCueOfACutPartIsApplied()
    {
        var bench = new AudioBench(reducedMotion: true, lead: 0.02);
        var player = bench.Play(Sounds(1.2, a => a.Music(0, "alder-vale").Effect(0.1, "ping").Music(0.4, "combat").Music(0.6, "alder-vale"),
            r => r.Span(0, 0.2).Hold(0.8, 0.4)));
        bench.Frame(Frame);
        var first = bench.Anchor;
        bench.FramesUntil(1.2);
        Assert.True(player.Ended);
        Assert.Equal("alder-vale", bench.Audio.Music.Theme);

        var hand = AudioBench.NewAudio();
        hand.Music.PlayAt(first, "alder-vale");
        hand.Effects.PlayAt(first + 0.1, "ping");
        bench.SoundsAs(hand);
    }

    /// <summary>Without an audio system the audio track plays nothing (S6-A); with one, an effect it hasn't or a theme its music hasn't is refused, changing nothing (S4-6).</summary>
    [Fact]
    public void AudioTracksNeedTheirEffectsAndThemesOnlyWithAnAudioSystem()
    {
        var bench = new AudioBench();
        var sequence = Sounds(1, a => a.Effect(0, "bell").Music(0.5, "no-such-theme"));
        var silent = bench.Play(sequence, new SequenceBindings());
        var alsoSilent = bench.Play(sequence, new SequenceBindings().Audio(null));
        bench.FramesUntil(1.2);
        Assert.True(silent.Ended && alsoSilent.Ended);
        Assert.DoesNotContain(bench.Rendered, s => s != 0);

        var problem = Assert.Throws<ArgumentException>(() => bench.Play(sequence)).Message;
        Assert.Equal("the sequence sounds, track 0 (audio): cue 0 plays the effect bell, which the audio system hasn't registered", problem);
        bench.Audio.Effects.Register("bell", AudioBench.Ping);
        problem = Assert.Throws<ArgumentException>(() => bench.Play(sequence)).Message;
        Assert.Equal("the sequence sounds, track 0 (audio): cue 1 plays the theme no-such-theme, which the audio system's music hasn't", problem);
        Assert.Empty(bench.Scene.Playing);
    }

    /// <summary>The test sequence (TestSequence.Sound.cs) with the bench's audio: its music, its effects (one at the stage's marker "open") as made by hand.</summary>
    [Fact]
    public void TheTestSequencesAudioTrackSoundsAsMadeByHand()
    {
        using var scene = new SequenceBench(gpu);
        var audio = new AudioBench(lead: 0.02);
        var player = scene.Scene.Play(TestSequence.Build(scene.StageData), scene.Bindings().Audio(audio.Audio), scene.Document);
        var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
        var rendered = new List<float>();
        void Render(double until)
        {
            while (audio.Audio.Graph.CurrentTime < until)
            {
                audio.Audio.BeforeQuantum();
                audio.Audio.Graph.RenderQuantum(bus);
                rendered.AddRange(bus.Channel(0).Data);
                rendered.AddRange(bus.Channel(1).Data);
            }
        }
        scene.Frame(Frame);
        var anchor = audio.Audio.Time;
        Render(scene.Now + 0.02);
        while (!player.Ended)
        {
            scene.Frame(Frame);
            Render(scene.Now + 0.02);
        }
        Render(scene.Now + 0.5);

        var hand = AudioBench.NewAudio();
        hand.Music.PlayAt(anchor, "great-imp-jiruan");
        hand.Effects.PlayAt(anchor + 0.25, "chime", 0.8, -0.5, 1.25);
        hand.Effects.PlayAt(anchor + 0.4, "thud");
        hand.Music.StopAt(anchor + 1.6);
        hand.Effects.PlayAt(anchor + TestSequence.Duration, "chime", 0.5);
        var expected = new List<float>();
        while (expected.Count < rendered.Count)
        {
            hand.BeforeQuantum();
            hand.Graph.RenderQuantum(bus);
            expected.AddRange(bus.Channel(0).Data);
            expected.AddRange(bus.Channel(1).Data);
        }
        Assert.Equal(expected, rendered);
        Assert.Contains(rendered, s => s != 0);
    }
}
