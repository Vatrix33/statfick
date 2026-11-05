using System.Numerics;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;
using Statventure.Animation;
using Statventure.Audio;

namespace Statventure.Tests.Scene;

/// <summary>
/// Ji'ruan's intro, the proof (Docs/Scene.md, step 7, phase 2): its record is the committed JSON form and
/// loads with its stage and effects from the game's data; it binds the king, the imps, the card, the
/// effects' palette and the clip library; it plays the approved schedule on the stage's marks, reveals the
/// king at the stage's marker, cuts to the stage's camera and sun, emits the mist and embers, delivers
/// its events, plays the card, skips to the same end it plays to, plays the old still card under reduced
/// motion, and stops for a pause and a hidden document.
/// </summary>
[Collection("GPU")]
public class JiruanIntroTests(GpuFixture gpu)
{
    const double Step = 1 / 60.0;

    /// <summary>
    /// The record the builder makes is the committed JSON form (Data/Cutscenes/world/cutscenes/jiruan.json),
    /// and <see cref="Sequence.Load"/> reads that file from the proof's data folder with the stage from a
    /// pack beside it. A missing fixture is written from the builder (the test then fails, to be run again).
    /// </summary>
    [Fact]
    public void TheSequenceIsItsCommittedRecordAndLoadsWithItsStage()
    {
        var stage = ModelData.Load(JiruanIntro.StageFile);
        var built = JiruanIntro.Build(stage);
        var json = RecordSerializer.ToJson(built);
        if (!File.Exists(JiruanIntro.SourceFixtureFile))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(JiruanIntro.SourceFixtureFile)!);
            File.WriteAllText(JiruanIntro.SourceFixtureFile, json);
            Assert.Fail($"wrote {JiruanIntro.SourceFixtureFile}: run again");
        }
        Assert.Equal(File.ReadAllText(JiruanIntro.SourceFixtureFile), json);
        using var folder = new TempFolder();
        var models = new PackWriter();
        models.AddRecord(JiruanIntro.StagePath, stage);
        models.Write(folder["models.pak"]);
        using var files = new VirtualFileSystem([Mount.Pack(folder["models.pak"]), Mount.Folder(JiruanIntro.DataFolder)]);
        RecordAssert.Equal(built, Sequence.Load(files, JiruanIntro.Path));
        // What the proof is: seven seconds, skippable and blocking, with its reduced variant the still card.
        Assert.Equal((7.0, true, true), (built.Duration, built.Skippable, built.Blocking));
        Assert.Equal([new SequenceSpan(6.5, 6.5, 2.4)], built.Reduced!);
    }

    /// <summary>The effects load from the proof's data and keep their rules; the mist rises slowly and lingers, the embers rise and glow.</summary>
    [Fact]
    public void TheEffectsLoadAndKeepTheirRules()
    {
        var (mist, embers) = JiruanIntro.Effects();
        Assert.Null(mist.Problem());
        Assert.Null(embers.Problem());
        Assert.IsType<ParticleShape.Puff>(mist.Shape);
        Assert.IsType<ParticleShape.Spark>(embers.Shape);
        Assert.True(embers.Glow && !mist.Glow);
        Assert.True(mist.Gravity.Y > 0 && embers.Gravity.Y > mist.Gravity.Y);
    }

    /// <summary>
    /// Played: each figure stands on its mark of the stage and plays the clip library's clips (S7-5) on the
    /// schedule, the stage's camera and sun are the scene's, the king is all ink until the stage's marker
    /// "reveal" (4.4833 s) and his own after it, the mist emits from the start and the embers from the
    /// rift's opening, and the card's texts and classes are set on the first frame.
    /// </summary>
    [Fact]
    public void PlayedTheFiguresStandOnTheirMarksAndTheStageLightsAndFramesIt()
    {
        using var bench = new JiruanBench(gpu.Device, drawStage: false);
        var own = bench.King.Palette;
        var player = bench.Play();
        bench.Frame(Step);
        var stage = player.Stages[0];
        Assert.Same(stage.NodeNamed("boss"), bench.King.Node);
        for (var i = 0; i < JiruanIntro.Imps.Length; i++)
            Assert.Same(stage.NodeNamed(JiruanIntro.Imps[i]), bench.Imps[i].Actor.Node);
        Assert.Same(stage.Cameras.Single(), bench.Scene.Camera);
        Assert.Same(stage.Sun, bench.Scene.Sun);
        // The card on the first frame.
        var card = bench.Card;
        Assert.False(card.HasAttribute("hidden"));
        Assert.True(card.HasClass("is-playing"));
        Assert.Equal("jiruan", card.GetAttribute("data-boss"));
        Assert.Equal(JiruanIntro.Location, bench.Document.GetElementById("boss-intro-location")!.TextContent);
        Assert.Equal(JiruanIntro.TitleName + JiruanIntro.TitleEm, bench.Document.GetElementById("boss-intro-title")!.TextContent);
        // The silhouette: one ink on every slot.
        Assert.NotSame(own, bench.King.Palette);
        Assert.Equal(Colors.LinearFromHex(JiruanIntro.Silhouette), bench.King.Palette!.Resolve(bench.Figure.Data.Materials[0]));
        // The mist at once; the embers from the rift's opening (each emitter hangs on its mark from the start,
        // its clock τ less its cue's time).
        Assert.Equal(0, Emitters(bench).Single(e => e.Effect == bench.Mist).Time, 9);
        Assert.Equal(-25 / 60.0, Emitters(bench).Single(e => e.Effect == bench.Embers).Time, 9);
        Assert.Same(player.Stages[0].NodeNamed("embers"), Emitters(bench).Single(e => e.Effect == bench.Embers).Node);
        bench.FrameTo(Step, 0.5);
        Assert.Equal(0.5 - 25 / 60.0, Emitters(bench).Single(e => e.Effect == bench.Embers).Time, 9);
        // Before the reveal still ink, after it his own.
        bench.FrameTo(Step, 4.47);
        Assert.NotSame(own, bench.King.Palette);
        bench.FrameTo(Step, 4.5);
        Assert.Same(own, bench.King.Palette);
    }

    /// <summary>
    /// The figures play the schedule's clips: the king's layer plays "rise" from 2.1 s, then "spread",
    /// "settle" and "idle-king"; each imp's "fly-out" from 1.68 s plus its delay, then "hover"; a figure's
    /// pose at a time is its clip's at the time into it (or the held first frame before its first strip).
    /// </summary>
    [Fact]
    public void TheFiguresPlayTheClipLibraryOnTheSchedule()
    {
        using var bench = new JiruanBench(gpu.Device, drawStage: false);
        var player = bench.Play();
        var layer = player.LayersOf(bench.King).Single();
        Assert.Equal(JiruanIntro.KingStrips.Select(s => (s.Clip, s.Start)), layer.Strips.Select(s => (s.Clip.Clip.Name, s.Start)));
        for (var i = 0; i < JiruanIntro.Imps.Length; i++)
            Assert.Equal(JiruanIntro.ImpStrips(i).Select(s => (s.Clip, s.Start)),
                player.LayersOf(bench.Imps[i].Actor).Single().Strips.Select(s => (s.Clip.Clip.Name, s.Start)));
        // At 3 s the king is 0.9 s into his rise: his chest where the clip puts it.
        bench.Frame(Step);
        bench.FrameTo(Step, 3.0);
        var rise = bench.Acting.Single(c => c.Name == "rise");
        var expected = new PoseBuffer(bench.KingSkeleton);
        new ClipBinding(rise, bench.KingSkeleton).Sample(0.9, expected);
        var actual = new PoseBuffer(bench.KingSkeleton);
        bench.King.Animator.Stack.Evaluate(bench.King.Animator.Time, actual);
        Assert.Equal(bench.Figure.Data.WorldMatrices(expected.ToPose()), bench.Figure.Data.WorldMatrices(actual.ToPose()));
    }

    /// <summary>
    /// Watched to the end, the events come in order, none skipped (the title card's at the stage's marker,
    /// 4.0667 s); the end leaves the card hidden and not playing, the king on his mark in his own colours.
    /// A skip at 1 s ends in the same state, the events not yet delivered delivered at once, marked
    /// skipped (Q18).
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WatchedOrSkippedItEndsTheSameWithItsEvents(bool skip)
    {
        using var bench = new JiruanBench(gpu.Device, drawStage: false);
        var own = bench.King.Palette;
        var player = bench.Play();
        var cues = new List<SequenceCue>();
        player.Cue += cues.Add;
        bench.Frame(Step);
        if (skip)
        {
            bench.FrameTo(Step, 1.0);
            Assert.True(player.Skip());
        }
        else
            bench.FrameTo(Step, 7.1);
        bench.Frame(Step);
        Assert.True(player.Ended);
        Assert.True(player.Finished.IsCompletedSuccessfully);
        Assert.Equal(skip, player.Finished.Result);
        var expected = new[] { new SequenceCue("intro-started", 0, false), new SequenceCue("title-card", 244 / 60.0, skip), new SequenceCue("intro-ended", 7, skip) };
        Assert.Equal(expected.Select(c => (c.Name, c.Skipped)), cues.Select(c => (c.Name, c.Skipped)));
        Assert.Equal(expected.Select(c => c.Time), cues.Select(c => c.Time), new ToleranceComparer(1e-9));
        Assert.True(bench.Card.HasAttribute("hidden"));
        Assert.False(bench.Card.HasClass("is-playing"));
        Assert.Same(own, bench.King.Palette);
        var mark = player.Stages[0].NodeNamed("boss")!;
        Assert.Same(mark, bench.King.Node);
        Assert.Equal(0, mark.World.Translation.Y, 4);
    }

    /// <summary>
    /// Under reduced motion the old still card plays (Q19): 2.4 s holding the timeline at 6.5 s, the card
    /// shown with its texts, the king revealed and standing; the moments before the hold and after it are
    /// cut out, so they arrive marked skipped (S4-9) though nobody skipped, and the end hides the card.
    /// </summary>
    [Fact]
    public void ReducedMotionPlaysTheStillCard()
    {
        using var bench = new JiruanBench(gpu.Device, reducedMotion: true, drawStage: false);
        var own = bench.King.Palette;
        var player = bench.Play();
        var cues = new List<SequenceCue>();
        player.Cue += cues.Add;
        Assert.True(player.Reduced);
        Assert.Equal(2.4, player.Duration, 9);
        bench.Frame(Step);
        bench.FrameTo(Step, 1.2);
        Assert.Equal(6.5, player.TimelineTime, 9);
        Assert.False(bench.Card.HasAttribute("hidden"));
        Assert.Equal(JiruanIntro.Description, bench.Document.GetElementById("boss-intro-description")!.TextContent);
        Assert.Same(own, bench.King.Palette);
        Assert.Equal(["intro-started", "title-card"], cues.Select(c => c.Name));
        Assert.All(cues, c => Assert.True(c.Skipped));
        bench.FrameTo(Step, 2.5);
        Assert.True(player.Ended);
        Assert.False(player.Finished.Result);
        Assert.Equal(("intro-ended", true), (cues[^1].Name, cues[^1].Skipped));
        Assert.True(bench.Card.HasAttribute("hidden"));
    }

    /// <summary>The game's pause holds the timeline, and so does a hidden document (the hidden time dropped, M10-7).</summary>
    [Fact]
    public void APauseAndAHiddenDocumentHoldIt()
    {
        using var bench = new JiruanBench(gpu.Device, drawStage: false);
        var player = bench.Play();
        bench.Frame(Step);
        bench.FrameTo(Step, 1.0);
        player.Pause();
        bench.FrameTo(Step, 2.0);
        Assert.Equal(1.0, player.TimelineTime, 9);
        player.Resume();
        bench.Frame(0.25);
        Assert.Equal(1.25, player.TimelineTime, 9);
        bench.Document.SetHidden(true, bench.Now);
        bench.Document.SetHidden(false, bench.Now + 3);
        bench.Frame(3 + 0.25);
        Assert.Equal(1.5, player.TimelineTime, 9);
    }

    /// <summary>
    /// The bindings are checked before anything changes (S4-6): the card's host missing, a figure whose clip
    /// source lacks a clip of the schedule, the effects' palette missing; each refused naming the problem,
    /// the scene left as it was.
    /// </summary>
    [Fact]
    public void BindingsMissingARoleOrAClipAreRefused()
    {
        using var bench = new JiruanBench(gpu.Device, drawStage: false);
        var sequence = JiruanIntro.Build(bench.StageData);
        SequenceBindings Without(string role)
        {
            var bindings = new SequenceBindings();
            bindings.Actor(JiruanIntro.King, bench.King, role == "spread" ? name => name == "spread" ? null : bench.Clips(bench.KingSkeleton)(name) : bench.Clips(bench.KingSkeleton));
            for (var i = 0; i < JiruanIntro.Imps.Length; i++)
                bindings.Actor(JiruanIntro.Imps[i], bench.Imps[i].Actor, bench.Clips(bench.Imps[i].Skeleton));
            if (role != JiruanIntro.Card)
                bindings.Element(JiruanIntro.Card, bench.Card);
            if (role != JiruanIntro.Glow)
                bindings.Palette(JiruanIntro.Glow, bench.Glow);
            return bindings.Effect(JiruanIntro.MistPath, bench.Mist).Effect(JiruanIntro.EmbersPath, bench.Embers).Stage(JiruanIntro.StagePath, bench.StageData);
        }
        foreach (var (role, words) in new[] { (JiruanIntro.Card, "card"), (JiruanIntro.Glow, "glow"), ("spread", "spread") })
        {
            var problem = Assert.Throws<ArgumentException>(() => bench.Scene.Play(sequence, Without(role), bench.Document));
            Assert.Contains(words, problem.Message);
            Assert.Empty(bench.Scene.Playing);
            Assert.Null(bench.King.Node);
            Assert.True(bench.Card.HasAttribute("hidden"));
        }
    }

    static IEnumerable<SceneEmitter> Emitters(JiruanBench bench) => bench.Scene.Root.Descendants().SelectMany(n => n.Attachments).OfType<SceneEmitter>();

    /// <summary>
    /// The sound (S7-6): the theme cued at the start and the three sounds at the stage's markers; with an
    /// audio system bound the sequence needs the theme in its music and the three patches registered (S4-6:
    /// refused naming the first missing, nothing changed), and plays with the proof's own patches (its data,
    /// as the game loads them): each sound starts a voice on the effects as the timeline reaches its marker
    /// (the audio rendered with the frames, 20 ms past each as a device runs ahead; within 3 frames).
    /// </summary>
    [Fact]
    public void TheAudioTrackCuesTheThemeAndTheThreeSounds()
    {
        using var bench = new JiruanBench(gpu.Device, drawStage: false);
        var sequence = JiruanIntro.Build(bench.StageData);
        var cues = sequence.Tracks.OfType<AudioTrack>().Single().Cues;
        Assert.Equal([(AudioCueKind.Music, JiruanIntro.Theme, 0.0, (string?)null), (AudioCueKind.Effect, JiruanIntro.RiftSound, 0.0, "jiruan/rift-opens"),
            (AudioCueKind.Effect, JiruanIntro.FlashSound, 0.0, "jiruan/flash"), (AudioCueKind.Effect, JiruanIntro.TitleSound, 0.0, "jiruan/title-card")],
            cues.Select(c => (c.Kind, c.Name, c.Time, c.Marker)));
        var audio = AudioBench.NewAudio();
        var problem = Assert.Throws<ArgumentException>(() => bench.Scene.Play(sequence, bench.Bindings().Audio(audio), bench.Document));
        Assert.Contains(JiruanIntro.RiftSound, problem.Message);
        Assert.Empty(bench.Scene.Playing);
        foreach (var (sound, patch) in JiruanIntro.SoundPatches())
            audio.Effects.Register(sound, patch);
        var player = bench.Scene.Play(sequence, bench.Bindings().Audio(audio), bench.Document);
        var times = new SequenceTimes(sequence, new Dictionary<string, ModelData> { [JiruanIntro.StagePath] = bench.StageData });
        var markers = new[] { "rift-opens", "flash", "title-card" }.Select(m => times.Resolve(0, "jiruan/" + m)).ToArray();
        var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
        var starts = new List<double>();
        var voices = 0;
        for (var frames = 0; player.TimelineTime < markers[^1] + 0.2; frames++)
        {
            Assert.True(frames < 2000, "too many frames");
            bench.Frame(Step);
            while (audio.Graph.CurrentTime < bench.Now + 0.02)
            {
                audio.BeforeQuantum();
                audio.Graph.RenderQuantum(bus);
            }
            if (audio.Effects.ActiveVoices > voices)
                starts.Add(player.TimelineTime);
            voices = audio.Effects.ActiveVoices;
        }
        Assert.Equal(markers.Length, starts.Count);
        for (var i = 0; i < markers.Length; i++)
            Assert.InRange(starts[i], markers[i] - 3 * Step, markers[i] + 3 * Step);
        Assert.False(player.Ended);
    }

    /// <summary>
    /// The three sounds are the user's choices (S7-9–S7-11), from the proof's data as the game loads them, each
    /// playable (registering checks it): the rift's "glassy crack and sub drop" (noise high-passed at 2,500 Hz, a
    /// sine falling from B♭2 to B♭1, a triangle at F5), the flash's "thunderclap" (noise low-passed from 1,200 Hz
    /// down to 200 Hz, a sine at B♭1, a square at A3) and the title card's "deep toll" (a triangle at B♭2, sines at
    /// B♭3 and 291 Hz, a high-passed noise strike). What is tuned by ear in the sandbox (levels, lengths, attacks,
    /// delays, sweep times, the filters' Q) isn't checked.
    /// </summary>
    [Fact]
    public void TheSoundsAreTheOnesTheUserChose()
    {
        var patches = JiruanIntro.SoundPatches();
        var audio = AudioBench.NewAudio();
        foreach (var (sound, patch) in patches)
            audio.Effects.Register(sound, patch);
        static string Hz(double value) => value.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        static string Layer(EffectLayer layer) =>
            (layer.Source == EffectSource.Noise ? "noise" : $"{layer.Source.ToString().ToLowerInvariant()} {Hz(layer.Frequency)}{(layer.FrequencyTo is { } to ? " to " + Hz(to) : "")}")
            + (layer.Filter is { } filter ? $" {filter.Type.ToString().ToLowerInvariant()} {Hz(filter.Frequency)}{(filter.FrequencyTo is { } down ? " to " + Hz(down) : "")}" : "");
        Assert.Equal(["noise highpass 2500", "sine 116.54 to 58.27", "triangle 698.46"], patches[JiruanIntro.RiftSound].Layers.Select(Layer));
        Assert.Equal(["noise lowpass 1200 to 200", "sine 58.27", "square 220"], patches[JiruanIntro.FlashSound].Layers.Select(Layer));
        Assert.Equal(["triangle 116.54", "sine 233.08", "sine 291", "noise highpass 3000"], patches[JiruanIntro.TitleSound].Layers.Select(Layer));
    }

    sealed class ToleranceComparer(double tolerance) : IEqualityComparer<double>
    {
        public bool Equals(double x, double y) => Math.Abs(x - y) <= tolerance;
        public int GetHashCode(double obj) => 0;
    }
}
