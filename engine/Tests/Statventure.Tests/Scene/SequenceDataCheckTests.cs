using Statventure.Audio;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;
using Statventure.Tests.Render3D;

namespace Statventure.Tests.Scene;

/// <summary>
/// A sequence checked against the game's data (M11 step 6; Docs/Editor.md, G5; the sequence's check given the
/// data, <see cref="RecordChecks.Check"/>): its stages read from it, the names in them, and its particle
/// effects and sounds as <see cref="SequencePlayer"/>'s start checks what is bound and registered (S4-6, S5-2,
/// S6-A): each particle cue's effect there and keeping its rules, each audio cue's effect patch and theme there
/// under the music's names; each problem at the cue's field. <see cref="Sequence.Load"/> runs them too (G5; the
/// user's decision of 2026-11-07, with the added loading time measured).
/// </summary>
public class SequenceDataCheckTests
{
    const string Mist = "models/effects/mist";

    /// <summary>A valid sequence over the test stage with these tracks after its clip track (track 0).</summary>
    static Sequence With(params SequenceTrack[] tracks) => new()
    {
        Name = "data",
        Duration = 2,
        Roles = [new("pivot", RoleKind.Node)],
        Stages = [TestSequence.StagePath],
        Tracks = [new ClipTrack { Stage = "hierarchy", Strip = new SequenceStrip("base") }, .. tracks],
    };

    static ParticleTrack Particles(params string[] effects) => new() { Cues = [.. effects.Select(e => new ParticleCue(0, e, "pivot"))] };

    static AudioTrack Sounds(params AudioCue[] cues) => new() { Cues = cues };

    /// <summary>A sound effect's patch, as the proof's data has one.</summary>
    static EffectPatch Patch() => JiruanIntro.SoundPatches()[JiruanIntro.FlashSound];

    /// <summary>The committed audio pack (the themes), mounted.</summary>
    static Mount AudioPack => Mount.Pack(Path.Combine(TestPaths.Packs, "audio.pak"));

    static IReadOnlyList<RecordProblem> Check(Sequence sequence, Action<PackWriter>? add = null, bool audio = true)
    {
        using var data = new SequenceRecordTests.StageData(add);
        Mount[] mounts = audio ? [AudioPack, Mount.Pack(data.Pack)] : [Mount.Pack(data.Pack)];
        using var files = new VirtualFileSystem(mounts);
        return RecordChecks.Check(sequence, files);
    }

    static string Text(IEnumerable<RecordProblem> problems) => string.Join("\n", problems);

    /// <summary>
    /// The proof (Ji'ruan's intro, the committed JSON form) against the data it is played with: its stage in a
    /// pack, its effects and sounds from its data folder and his theme from audio.pak: no problem. Without
    /// audio.pak his theme isn't there, so loading refuses it (why the sandbox and the proof's own load test mount
    /// the sound pack, as the game does: the user's decision of 2026-11-07).
    /// </summary>
    [Fact]
    public void TheProofKeepsEveryRuleAgainstItsData()
    {
        using var folder = new TempFolder();
        var models = new PackWriter();
        models.AddRecord(JiruanIntro.StagePath, ModelData.Load(JiruanIntro.StageFile));
        models.Write(folder["models.pak"]);
        using (var files = new VirtualFileSystem([AudioPack, Mount.Pack(folder["models.pak"]), Mount.Folder(JiruanIntro.DataFolder)]))
        {
            var proof = Sequence.Load(files, JiruanIntro.Path);
            Assert.Empty(RecordChecks.Check(proof, files));
        }
        using var withoutMusic = new VirtualFileSystem([Mount.Pack(folder["models.pak"]), Mount.Folder(JiruanIntro.DataFolder)]);
        var sequence = withoutMusic.Load<Sequence>(JiruanIntro.Path);
        var track = sequence.Tracks.ToList().FindIndex(t => t is AudioTrack);
        var expected = $"the sequence jiruan, track {track} (audio): cue 0 plays the theme great-imp-jiruan, which isn't in the game's data (audio/themes/great-imp-jiruan)";
        Assert.Equal([new RecordProblem(RecordPath.Root.Field("tracks").Index(track).Field("cues").Index(0).Field("name"), expected)],
            RecordChecks.Check(sequence, withoutMusic));
        var refused = Assert.Throws<DataFileException>(() => Sequence.Load(withoutMusic, JiruanIntro.Path));
        Assert.Equal((Path.GetFullPath(JiruanIntro.FixtureFile), (string?)null, "not a valid sequence: " + expected), (refused.File, refused.Entry, refused.Problem));
    }

    /// <summary>The whole test sequence (every track kind) against data holding its stage, its effect, its two sounds and its theme.</summary>
    [Fact]
    public void TheTestSequenceKeepsEveryRuleAgainstItsData()
    {
        var sequence = TestSequence.Build();
        Assert.Empty(Check(sequence, w =>
        {
            w.AddRecord(TestSequence.EffectPath, TestSequence.Effect);
            w.AddRecord(MusicNames.Effects + "/chime", Patch());
            w.AddRecord(MusicNames.Effects + "/thud", Patch());
        }));
        // Without them: the effect at each of its three cues, each sound at each cue naming it.
        var problems = Check(sequence, audio: false);
        Assert.Equal(3, problems.Count(p => p.Message.Contains($"effect {TestSequence.EffectPath} isn't in the game's data", StringComparison.Ordinal)));
        Assert.Equal(2, problems.Count(p => p.Message.Contains("plays the effect chime, which isn't in the game's data (audio/effects/chime)", StringComparison.Ordinal)));
        Assert.Single(problems, p => p.Message.Contains("plays the effect thud, which isn't in the game's data (audio/effects/thud)", StringComparison.Ordinal));
        Assert.Single(problems, p => p.Message.Contains("plays the theme great-imp-jiruan, which isn't in the game's data", StringComparison.Ordinal));
        Assert.Equal(7, problems.Count);
    }

    public static TheoryData<string> Cases => [.. Broken.Keys];

    /// <summary>Each way a sequence's effects and sounds miss the data, at the cue's field, in the sequence's words or the entry's own error.</summary>
    static readonly Dictionary<string, (SequenceTrack Track, Action<PackWriter>? Add, string Path, string Problem)> Broken = new()
    {
        ["effect missing"] = (Particles(Mist), null, "$.tracks[1].cues[0].effect",
            "the sequence data, track 1 (particles): cue 0's effect models/effects/mist isn't in the game's data"),
        ["effect deeper"] = (Particles("models/effects/rift/mist"), w => w.AddRecord(Mist, ParticleTests.Full()), "$.tracks[1].cues[0].effect",
            "cue 0's effect models/effects/rift/mist isn't in the game's data"),
        ["effect broken"] = (Particles(Mist), w => w.AddRecord(Mist, ParticleTests.Defaults() with { Opacity = 2 }), "$.tracks[1].cues[0].effect",
            "entry 'models/effects/mist': not a valid particle effect: its opacity (2) must be from 0 to 1"),
        ["effect of another kind"] = (Particles(Mist), w => w.AddRecord(Mist, Patch()), "$.tracks[1].cues[0].effect", "entry 'models/effects/mist'"),
        ["effect a file"] = (Particles(Mist), w => w.Add(Mist, [1, 2, 3]), "$.tracks[1].cues[0].effect", "entry 'models/effects/mist'"),
        ["second cue"] = (new ParticleTrack { Cues = [new(0, Mist, "pivot"), new(1, "models/effects/ash", "pivot")] }, w => w.AddRecord(Mist, ParticleTests.Full()),
            "$.tracks[1].cues[1].effect", "cue 1's effect models/effects/ash isn't in the game's data"),
        ["sound missing"] = (Sounds(new AudioCue(0, AudioCueKind.Effect, "boom")), null, "$.tracks[1].cues[0].name",
            "the sequence data, track 1 (audio): cue 0 plays the effect boom, which isn't in the game's data (audio/effects/boom)"),
        ["sound by the music's names"] = (Sounds(new AudioCue(0, AudioCueKind.Effect, "bigBoom")), w => w.AddRecord("audio/effects/bigboom", Patch()), "$.tracks[1].cues[0].name",
            "cue 0 plays the effect bigBoom, which isn't in the game's data (audio/effects/big_boom)"),
        ["sound no path holds"] = (Sounds(new AudioCue(0, AudioCueKind.Effect, "boom!")), null, "$.tracks[1].cues[0].name",
            "cue 0 plays the effect boom!, which no data path can hold"),
        ["sound of another kind"] = (Sounds(new AudioCue(0, AudioCueKind.Effect, "boom")), w => w.AddRecord("audio/effects/boom", ParticleTests.Full()), "$.tracks[1].cues[0].name",
            "entry 'audio/effects/boom'"),
        ["theme missing"] = (Sounds(new AudioCue(0, AudioCueKind.Music, "great-ogre")), null, "$.tracks[1].cues[0].name",
            "cue 0 plays the theme great-ogre, which isn't in the game's data (audio/themes/great-ogre)"),
        ["theme no path holds"] = (Sounds(new AudioCue(1, AudioCueKind.Effect, "boom"), new AudioCue(0, AudioCueKind.Music, "Ogre Theme")), w => w.AddRecord("audio/effects/boom", Patch()),
            "$.tracks[1].cues[1].name", "cue 1 plays the theme Ogre Theme, which no data path can hold"),
        ["theme of another kind"] = (Sounds(new AudioCue(0, AudioCueKind.Music, "jiruan-flash")), w => w.AddRecord("audio/themes/jiruan-flash", Patch()), "$.tracks[1].cues[0].name",
            "entry 'audio/themes/jiruan-flash'"),
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void EachEffectAndSoundIsCheckedAgainstTheData(string label)
    {
        var (track, add, path, problem) = Broken[label];
        var sequence = With(track);
        Assert.Empty(RecordChecks.Check(sequence));
        var problems = Check(sequence, add);
        Assert.True(problems.Count == 1, Text(problems));
        Assert.Equal(path, problems[0].Path.ToString());
        Assert.Contains(problem, problems[0].Message);
    }

    /// <summary>What the data has passes: an effect, a sound named by the music's rule (a capital as '_' and its small letter), a theme, and a stop that names nothing.</summary>
    [Fact]
    public void EffectsAndSoundsTheDataHasPass()
    {
        var sequence = With(Particles(Mist, Mist), Sounds(new AudioCue(0, AudioCueKind.Effect, "bigBoom"), new AudioCue(0.5, AudioCueKind.Music, "great-imp-jiruan"), new AudioCue(1, AudioCueKind.StopMusic)));
        Assert.Empty(Check(sequence, w =>
        {
            w.AddRecord(Mist, ParticleTests.Full());
            w.AddRecord("audio/effects/big_boom", Patch());
        }));
    }

    /// <summary>A sequence that breaks its own rules isn't checked against the data (as loading stops at them): only its own problems.</summary>
    [Fact]
    public void ASequenceBreakingItsOwnRulesIsntCheckedAgainstTheData()
    {
        var sequence = With(Particles(Mist)) with { Duration = -1 };
        var problems = Check(sequence);
        // Its own: the duration (and so its cue's time), then the duration's limit, which its own rule holds (G4).
        Assert.Equal(["$.duration", "$.tracks[1].cues[0].time", "$.duration"], problems.Select(p => p.Path.ToString()));
        Assert.Equal("must be above 0 (it is -1)", problems[2].Message);
    }

    /// <summary>
    /// Its stages from the data: one the data hasn't is <see cref="Sequence.Load"/>'s words at the stage, a broken one its own
    /// error naming its entry; then the names in the stages aren't checked, but its effects and sounds are.
    /// </summary>
    [Fact]
    public void StagesComeFromTheData()
    {
        var sequence = With(Particles(Mist)) with { Stages = [TestSequence.StagePath, "models/stages/arena"] };
        var problems = Check(sequence);
        Assert.Equal(["$.stages[1]", "$.tracks[1].cues[0].effect"], problems.Select(p => p.Path.ToString()));
        Assert.Equal("its stage models/stages/arena isn't in the game's data", problems[0].Message);
        var broken = Check(With(new TransformTrack { Node = "hierarchy/Nope", LocationX = new FCurve([FCurveKey.Linear(0, 0)]) }) with
        {
            Stages = [TestSequence.StagePath, "models/stages/arena"],
        }, w => w.Add("models/stages/arena", [1, 2, 3]));
        var stage = Assert.Single(broken);
        Assert.Equal("$.stages[1]", stage.Path.ToString());
        Assert.Contains("entry 'models/stages/arena'", stage.Message);
        // With the stages there, the names in them.
        Assert.Equal(["$.tracks[1].node"], Check(With(new TransformTrack { Node = "hierarchy/Nope", LocationX = new FCurve([FCurveKey.Linear(0, 0)]) }))
            .Select(p => p.Path.ToString()));
    }

    /// <summary>Not reporting (the game's loading, once it runs these), a broken entry is its own error as it is, naming its entry: an effect, a sound, a theme.</summary>
    [Fact]
    public void ABrokenEntryIsItsOwnErrorWhenNotReported()
    {
        foreach (var (track, add, entry) in new (SequenceTrack, Action<PackWriter>, string)[]
                 {
                     (Particles(Mist), w => w.AddRecord(Mist, ParticleTests.Defaults() with { Opacity = 2 }), Mist),
                     (Sounds(new AudioCue(0, AudioCueKind.Effect, "boom")), w => w.Add("audio/effects/boom", [1, 2, 3]), "audio/effects/boom"),
                     (Sounds(new AudioCue(0, AudioCueKind.Music, "ogre")), w => w.AddRecord("audio/themes/ogre", Patch()), "audio/themes/ogre"),
                 })
        {
            using var data = new SequenceRecordTests.StageData(add);
            var problem = Assert.Throws<DataFileException>(() => SequenceChecks.AgainstData(With(track), data.Files, reportBroken: false));
            Assert.Equal((Path.GetFullPath(data.Pack), entry), (problem.File, problem.Entry));
        }
        // A missing one is a problem either way; cues the sequence's own rules refuse are left to them (an effect not
        // under models/effects/, a sound without a name).
        using var empty = new SequenceRecordTests.StageData();
        Assert.Empty(SequenceChecks.AgainstData(With(Particles("models/mist"), Sounds(new AudioCue(0, AudioCueKind.Effect, ""))), empty.Files, reportBroken: true));
        Assert.Equal(["$.tracks[1].cues[0].effect"], SequenceChecks.AgainstData(With(Particles(Mist)), empty.Files, reportBroken: false).Select(p => p.Path.ToString()));
    }

    /// <summary>
    /// <see cref="Sequence.Load"/> checks the effects and sounds against the data too (G5; the user's decision of
    /// 2026-11-07, with the added loading time measured): the first problem after its own rules and its stages', named
    /// as the sequence's are (the pack and the sequence's entry); a broken effect, sound or theme is its own error
    /// naming its entry, as a broken stage's is; with them all there it loads.
    /// </summary>
    [Fact]
    public void LoadingChecksThemAgainstTheData()
    {
        var sequence = With(Particles(Mist), Sounds(new AudioCue(0, AudioCueKind.Effect, "boom"), new AudioCue(0, AudioCueKind.Music, "great-imp-jiruan")));
        using (var data = new SequenceRecordTests.StageData(w => w.AddRecord("world/cutscenes/data", sequence)))
        {
            var problem = Assert.Throws<DataFileException>(() => Sequence.Load(data.Files, "world/cutscenes/data"));
            Assert.Equal((Path.GetFullPath(data.Pack), "world/cutscenes/data",
                    "not a valid sequence: the sequence data, track 1 (particles): cue 0's effect models/effects/mist isn't in the game's data"),
                (problem.File, problem.Entry, problem.Problem));
        }
        using (var data = new SequenceRecordTests.StageData(w =>
               {
                   w.AddRecord("world/cutscenes/data", sequence);
                   w.AddRecord(Mist, ParticleTests.Full());
               }))
        {
            Assert.Equal("not a valid sequence: the sequence data, track 2 (audio): cue 0 plays the effect boom, which isn't in the game's data (audio/effects/boom)",
                Assert.Throws<DataFileException>(() => Sequence.Load(data.Files, "world/cutscenes/data")).Problem);
        }
        using (var data = new SequenceRecordTests.StageData(w =>
               {
                   w.AddRecord("world/cutscenes/data", sequence);
                   w.AddRecord(Mist, ParticleTests.Defaults() with { Opacity = 2 });
               }))
        {
            var problem = Assert.Throws<DataFileException>(() => Sequence.Load(data.Files, "world/cutscenes/data"));
            Assert.Equal((Mist, "not a valid particle effect: its opacity (2) must be from 0 to 1"), (problem.Entry, problem.Problem));
        }
        using (var data = new SequenceRecordTests.StageData(w =>
               {
                   w.AddRecord("world/cutscenes/data", sequence);
                   w.AddRecord(Mist, ParticleTests.Full());
                   w.AddRecord("audio/effects/boom", Patch());
               }))
        {
            using var files = new VirtualFileSystem([AudioPack, Mount.Pack(data.Pack)]);
            RecordAssert.Equal(sequence, Sequence.Load(files, "world/cutscenes/data"));
            Assert.Equal("not a valid sequence: the sequence data, track 2 (audio): cue 1 plays the theme great-imp-jiruan, which isn't in the game's data (audio/themes/great-imp-jiruan)",
                Assert.Throws<DataFileException>(() => Sequence.Load(data.Files, "world/cutscenes/data")).Problem);
        }
    }
}
