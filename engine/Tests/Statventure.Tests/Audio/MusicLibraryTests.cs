using Statventure.Audio;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;

namespace Statventure.Tests.Audio;

/// <summary>
/// The music and effects as data (Docs/Assets.md, step 6; Q21, Q22, S6-1–S6-5): the name rule for
/// entry paths, loading a library from packs and loose folders with every problem it reports, the theme
/// order, a library with no music, effect patches registered by name, libraries side by side, the
/// committed audio.pak, and the audio records' layouts and fixtures.
/// </summary>
public class MusicLibraryTests
{
    // ---------------- Sample records (every field set; the fixtures hold them) ----------------

    internal static ThemeSpec SampleTheme(string id = "boss@sample-vale", string? room = "cavern") => new()
    {
        Id = id, Title = "Sample · Every Field", Tempo = 96.5, Meter = [3, 4], Tonic = 57, Mode = "dorian", Room = room, Gain = 1.25,
        Swing = 0.15, Eq = new() { Low = -4.5, Presence = 1.5 }, Form = ["A", "B", "A"], Loop = 1, Motif = "1 3 5",
        Sections = new Dictionary<string, SectionSpec>
        {
            ["A"] = new()
            {
                Chords = "1 | 4 | 5 | 1", Melody = "1/2 3/1 | 5/3", Dynamics = 0.8, DynamicsTo = 1.1,
                Layers =
                [
                    new() { Type = "mel", Instrument = "flute", Octave = 1, Velocity = 0.85, Pan = -0.2, Legato = 0.9, Role = "quote",
                        Harmony = new() { Instrument = "fiddle", Steps = -2, Velocity = 0.4, Octave = 0, Pan = 0.3 },
                        Doublings = [new() { Instrument = "oboe", Octave = -1, Velocity = 0.5, Pan = 0.1, Shift = 12 }] },
                    new() { Type = "drums", Velocity = 0.7, Drums = [new("frame", "x..o.."), new("shaker", ".x..x.")], TimpaniOctave = -1, LogOctave = 1 },
                    new() { Type = "drone", Instrument = "drone", Degrees = ["1", "5"], Every = 12, Bars = [0, 2], Cycle = 4 },
                    new() { Type = "hit", Instrument = "gong", Degree = "5", At = 3, Length = 6 },
                    new() { Type = "pad", Instrument = "strings", Voices = 4, Spread = 0.5 },
                    new() { Type = "arp", Instrument = "harp", Pattern = "158T85" },
                    new() { Type = "tex", Kind = "rain", Chance = 0.1 },
                    new() { Type = "line", Instrument = "cellos", Notes = "1/3 5/3" },
                ],
            },
            ["B"] = new() { Like = "A", Dynamics = 1.2 },
        },
    };

    /// <summary>
    /// The sample theme made playable, for the libraries that load (since M11 step 5 the data's themes are checked
    /// against the composer when they load, G5): its melody's and line's bars fill the meter's 12 steps (the sample's
    /// hold 3 and 6, which the score refuses), every field still set. <see cref="Music"/> gives the instruments and
    /// percussion voices it plays.
    /// </summary>
    internal static ThemeSpec PlayableTheme(string id = "boss@sample-vale", string? room = "cavern")
    {
        var theme = SampleTheme(id, room);
        var a = theme.Sections["A"];
        var layers = a.Layers!.Select(l => l.Notes != null ? l with { Notes = "1/6 5/6" } : l).ToArray();
        return theme with
        {
            Sections = new Dictionary<string, SectionSpec>(theme.Sections) { ["A"] = a with { Melody = "1/6 3/6 | 5/12", Layers = layers } },
        };
    }

    /// <summary>The instruments and percussion voices <see cref="SampleTheme"/> plays.</summary>
    static readonly string[] SampleInstruments = ["cellos", "drone", "fiddle", "flute", "harp", "oboe", "strings"], SampleVoices = ["gong", "shaker"];

    internal static readonly InstrumentSpec SampleInstrument = new()
    {
        Kind = "pluck", Voices = [-7, 0, 6], Cutoff = 2400, Attack = 0.01, Release = 0.3, Vibrato = [5.5, 8, 0.25], Level = 0.12, Pan = -0.3, Send = 0.5,
        Decay = 1.4, Tremolo = 6, Peak = [2600, 5], Low = 300, High = 2000, Vowel = [730, 1090, 2440], Harmonics = [0, 1, 0.5], Breath = 0.1,
        Bright = 0.6, Damp = 0.4, Length = 1.8, Body = 240, Highpass = 180, Partials = [[1, 1, 2.2], [2.76, 0.4, 0.5]],
    };

    internal static readonly PercussionSpec SamplePercussion = new()
    {
        Body = [95, 44, 0.55], Noise = [600, 0.12], Click = 0.25, Level = 0.13, Send = 0.5, High = [4200, 2.2, 0.045], Tone = [620, 0.02], Attack = 0.018,
        Log = true, Swell = true, Metal = [[1, 1, 5], [1.48, 0.55, 4]], Base = 58, Thunder = true, Timpani = true,
    };

    internal static readonly EffectPatch SampleEffect = new()
    {
        Layers =
        [
            new() { Source = EffectSource.Harmonics, Frequency = 660, FrequencyTo = 330, SweepTime = 0.1, Harmonics = [0, 1, 0.4], Delay = 0.01, Attack = 0.004, Level = 0.6, Length = 0.3,
                Filter = new() { Type = EffectFilterType.Peaking, Frequency = 1200, FrequencyTo = 800, SweepTime = 0.2, Q = 2, Gain = 6 } },
            new() { Source = EffectSource.Noise, Filter = new() { Type = EffectFilterType.Highpass, Frequency = 3000 }, Length = 0.05 },
        ],
        Level = 0.7, MaxVoices = 3, Pitch = new(0.9, 1.1), Volume = new(0.8, 1),
    };

    static readonly EffectPatch Chime = new() { Layers = [new() { Source = EffectSource.Sine, Frequency = 880, FrequencyTo = 440, Length = 0.08 }], Level = 0.5 };
    static readonly EffectPatch Thud = new() { Layers = [new() { Source = EffectSource.Triangle, Frequency = 110, Length = 0.06 }] };

    // ---------------- Fixtures and layouts (Docs/Assets.md, "Guarding layouts") ----------------

    [Fact]
    public void TheAudioRecordsHaveFixtures()
    {
        RecordFixtures.Ensure("every-field", SampleTheme());
        RecordFixtures.Ensure("every-field", SampleInstrument);
        RecordFixtures.Ensure("every-field", SamplePercussion);
        RecordFixtures.Ensure("cathedral", new RoomSpec(4.2, 0.6, 0.36));
        RecordFixtures.Ensure("two-themes", new ThemeOrder { Themes = ["alder-vale", "combat@alder-vale"] });
        RecordFixtures.Ensure("every-field", SampleEffect);
        RecordFixtures.Ensure("defaults", new EffectPatch { Layers = [new() { Source = EffectSource.Sine, Length = 0.1 }] });
    }

    /// <summary>The JSON names are the records' own (Q22), and a patch left on its defaults reads them back (the effect fixtures' JavaScript states the same defaults).</summary>
    [Fact]
    public void TheJsonFormsUseTheRecordsOwnNames()
    {
        var theme = RecordSerializer.ToJson(SampleTheme());
        foreach (var name in new[] { "\"timpaniOctave\"", "\"logOctave\"", "\"doublings\"", "\"harmony\"", "\"drums\": {", "\"degrees\"", "\"degree\"", "\"dynamicsTo\"", "\"melody\"" })
            Assert.Contains(name, theme);
        Assert.Contains("\"timpani\": true", RecordSerializer.ToJson(SamplePercussion));
        Assert.Contains("\"vibrato\"", RecordSerializer.ToJson(SampleInstrument));
        var patch = RecordSerializer.FromJson<EffectPatch>("""{ "$types": { "audio.effect": 1, "audio.effect-layer": 1 }, "layers": [{ "source": "noise", "length": 0.2 }] }""");
        Assert.Equal(1, patch.Level);
        Assert.Equal(4, patch.MaxVoices);
        Assert.Equal(440, patch.Layers[0].Frequency);
        Assert.Equal(0.002, patch.Layers[0].Attack);
        Assert.Equal(1, patch.Layers[0].Level);
        Assert.Equal(0, patch.Layers[0].Delay);
        Assert.Equal(1, RecordSerializer.FromJson<EffectFilter>("""{ "$types": { "audio.effect-filter": 1 }, "type": "allpass", "frequency": 500 }""").Q);
    }

    // ---------------- The name rule (S6-1) ----------------

    [Theory]
    [InlineData("alder-vale", "alder-vale")]
    [InlineData("combat@alder-vale", "combat.alder-vale")]
    [InlineData("lowSpiccato", "low_spiccato")]
    [InlineData("choirOo", "choir_oo")]
    [InlineData("ABC", "_a_b_c")]
    [InlineData("x9-0", "x9-0")]
    [InlineData("a@@b", "a..b")]
    [InlineData("@a", ".a")]
    public void NamesAreStoredByTheRule(string name, string stored)
    {
        Assert.Equal(stored, MusicNames.ToPathName(name));
        Assert.Equal(name, MusicNames.FromPathName(stored));
        Assert.Equal("audio/themes/" + stored, MusicNames.PathOf(MusicNames.Themes, name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("a b")]
    [InlineData("a_b")]
    [InlineData("a.b")]
    [InlineData("a/b")]
    [InlineData("ä")]
    [InlineData("a@")]
    [InlineData("con")]
    [InlineData("aux@x")]
    [InlineData("lpt1")]
    [InlineData("com0@a-b")]
    public void NamesTheRuleCantStoreAreRefused(string name)
    {
        Assert.Null(MusicNames.ToPathName(name));
        Assert.Throws<ArgumentException>(() => MusicNames.PathOf(MusicNames.Rooms, name));
    }

    [Fact]
    public void ANameMayBeAsLongAsAPathNameMay()
    {
        Assert.Equal(new string('a', 255), MusicNames.ToPathName(new string('a', 255)));
        Assert.Null(MusicNames.ToPathName(new string('a', 256)));
        // A capital takes two characters of the path name.
        Assert.Null(MusicNames.ToPathName(new string('a', 254) + "B"));
        Assert.Equal(new string('a', 253) + "B", MusicNames.FromPathName(MusicNames.ToPathName(new string('a', 253) + "B")!));
        // Names of devices are refused only as the path rule refuses them: "Con" is "_con".
        Assert.Equal("_con", MusicNames.ToPathName("Con"));
    }

    [Theory]
    [InlineData("a_")]
    [InlineData("_1")]
    [InlineData("__a")]
    [InlineData("a_-b")]
    [InlineData("a/b")]
    [InlineData("A")]
    [InlineData("a.")]
    [InlineData("")]
    [InlineData("nul")]
    public void PathNamesTheRuleDoesntMakeStoreNoName(string pathName) => Assert.Null(MusicNames.FromPathName(pathName));

    [Fact]
    public void EveryNameOfTheGamesMusicGoesThereAndBack()
    {
        var library = TestMusic.Library;
        foreach (var name in library.Themes.Concat(library.Instruments.Keys).Concat(library.Percussion.Keys).Concat(library.Rooms.Keys))
            Assert.Equal(name, MusicNames.FromPathName(MusicNames.ToPathName(name)!));
        var random = new Random(6);
        const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-@";
        for (var i = 0; i < 2000; i++)
        {
            var name = new string([.. Enumerable.Range(0, random.Next(1, 12)).Select(_ => alphabet[random.Next(alphabet.Length)])]);
            if (MusicNames.ToPathName(name) is { } stored)
            {
                Assert.True(PackPath.IsValid(stored), stored);
                Assert.Equal(name, MusicNames.FromPathName(stored));
            }
            else
                Assert.True(name.EndsWith('@') || name.Split('@')[0] is "con" or "prn" or "aux" or "nul" or ['c', 'o', 'm', >= '0' and <= '9'] or ['l', 'p', 't', >= '0' and <= '9'], name);
        }
    }

    // ---------------- Loading (S6-1–S6-4) ----------------

    /// <summary>A pack of the given records at their music paths, written into <paramref name="folder"/>; null values are left out.</summary>
    static string Pack(TempFolder folder, string name, Action<PackWriter> add)
    {
        var writer = new PackWriter();
        add(writer);
        writer.Write(folder[name]);
        return folder[name];
    }

    static void Music(PackWriter writer, IEnumerable<ThemeSpec> themes, string[]? order = null, bool rooms = true)
    {
        foreach (var theme in themes)
            writer.AddRecord(MusicNames.PathOf(MusicNames.Themes, theme.Id), theme);
        if (order is not null)
            writer.AddRecord(MusicNames.ThemeOrder, new ThemeOrder { Themes = order });
        if (rooms)
        {
            writer.AddRecord(MusicNames.PathOf(MusicNames.Rooms, "hall"), new RoomSpec(2.4, 0.55, 0.3));
            writer.AddRecord(MusicNames.PathOf(MusicNames.Rooms, "cavern"), new RoomSpec(3.4, 0.75, 0.36));
        }
        writer.AddRecord(MusicNames.PathOf(MusicNames.Instruments, "lowSpiccato"), SampleInstrument);
        writer.AddRecord(MusicNames.PathOf(MusicNames.Percussion, "frame"), SamplePercussion);
        // What the sample theme plays, so its libraries pass the check at load (G5).
        foreach (var name in SampleInstruments)
            writer.AddRecord(MusicNames.PathOf(MusicNames.Instruments, name), SampleInstrument);
        foreach (var name in SampleVoices)
            writer.AddRecord(MusicNames.PathOf(MusicNames.Percussion, name), SamplePercussion);
    }

    static MusicLibrary Load(params Mount[] mounts)
    {
        using var data = new VirtualFileSystem(mounts);
        return MusicLibrary.Load(data);
    }

    static DataFileException Fails(params Mount[] mounts) => Assert.Throws<DataFileException>(() => Load(mounts));

    [Fact]
    public void ALibraryLoadsFromAPack()
    {
        using var folder = new TempFolder();
        var pack = Pack(folder, "audio.pak", w =>
        {
            Music(w, [PlayableTheme("combat@alder-vale"), PlayableTheme("alder-vale")], ["combat@alder-vale", "alder-vale"]);
            w.AddRecord(MusicNames.PathOf(MusicNames.Effects, "runeSlot"), SampleEffect);
        });
        var library = Load(Mount.Pack(pack));
        Assert.Equal(["combat@alder-vale", "alder-vale"], library.Themes);
        RecordAssert.Equal(PlayableTheme("alder-vale"), library.Theme("alder-vale"));
        Assert.Null(library.Theme("nothing"));
        Assert.Equal(["cellos", "drone", "fiddle", "flute", "harp", "lowSpiccato", "oboe", "strings"], library.Instruments.Keys);
        RecordAssert.Equal(SampleInstrument, library.Instruments["lowSpiccato"]);
        Assert.Equal(["frame", "gong", "shaker"], library.Percussion.Keys);
        Assert.Equal(["cavern", "hall"], library.Rooms.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(["runeSlot"], library.Effects.Keys);
        RecordAssert.Equal(SampleEffect, library.Effects["runeSlot"]);
    }

    /// <summary>The development folder's JSON forms (S3-1) load as the pack's records do, and one replaces one theme alone (Q21).</summary>
    [Fact]
    public void ALooseFolderReplacesOneTheme()
    {
        using var folder = new TempFolder();
        var pack = Pack(folder, "audio.pak", w => Music(w, [PlayableTheme("a"), PlayableTheme("b")], ["a", "b"]));
        using (var original = PackFile.Open(pack))
            original.ExtractTo(folder["extracted"]);
        var fromFolder = Load(Mount.Folder(folder["extracted"]));
        var fromPack = Load(Mount.Pack(pack));
        Assert.Equal(fromPack.Themes, fromFolder.Themes);
        foreach (var id in fromPack.Themes)
            RecordAssert.Equal(fromPack.Theme(id), fromFolder.Theme(id));
        RecordAssert.Equal(fromPack.Instruments.ToArray(), fromFolder.Instruments.ToArray());

        folder.Write("dev/audio/themes/b.json", RecordSerializer.ToJson(PlayableTheme("b") with { Tempo = 140 }));
        var modded = Load(Mount.Pack(pack), Mount.Folder(folder["dev"]));
        Assert.Equal(["a", "b"], modded.Themes);
        Assert.Equal(140, modded.Theme("b")!.Tempo);
        Assert.Equal(96.5, modded.Theme("a")!.Tempo);
    }

    /// <summary>S6-2: the order sets the order; themes it doesn't name follow in path order (a mod adds a theme with its entry alone).</summary>
    [Fact]
    public void ThemesTheOrderDoesntNameFollowInPathOrder()
    {
        using var folder = new TempFolder();
        var pack = Pack(folder, "audio.pak", w => Music(w, [PlayableTheme("m"), PlayableTheme("z"), PlayableTheme("a"), PlayableTheme("combat@b"), PlayableTheme("combatB")], ["z", "m"]));
        // Path order: a, combat.b, combat_b ('.' before '_').
        Assert.Equal(["z", "m", "a", "combat@b", "combatB"], Load(Mount.Pack(pack)).Themes);
        var mod = Pack(folder, "mod.pak", w => w.AddRecord(MusicNames.PathOf(MusicNames.Themes, "new-boss"), PlayableTheme("new-boss")));
        Assert.Equal(["z", "m", "a", "combat@b", "combatB", "new-boss"], Load(Mount.Pack(pack), Mount.Pack(mod)).Themes);
        Assert.Empty(Load(Mount.Pack(Pack(folder, "empty-order.pak", w => Music(w, [], [])))).Themes);
    }

    [Fact]
    public void TheOrderMustNameEachThemeOnceAndOnlyThemesThatAreThere()
    {
        using var folder = new TempFolder();
        var twice = Pack(folder, "twice.pak", w => Music(w, [PlayableTheme("a"), PlayableTheme("b")], ["a", "b", "a"]));
        var problem = Fails(Mount.Pack(twice));
        Assert.Equal((twice, MusicNames.ThemeOrder), (problem.File, problem.Entry));
        Assert.Equal("it names the theme 'a' twice", problem.Problem);

        var missing = Pack(folder, "missing.pak", w => Music(w, [PlayableTheme("a")], ["a", "combat@b"]));
        problem = Fails(Mount.Pack(missing));
        Assert.Equal((missing, MusicNames.ThemeOrder), (problem.File, problem.Entry));
        Assert.Equal("it names the theme 'combat@b', which has no entry 'audio/themes/combat.b'", problem.Problem);

        var unstorable = Pack(folder, "unstorable.pak", w => Music(w, [PlayableTheme("a")], ["a", "b c"]));
        Assert.Equal("it names the theme 'b c', which no data path can hold", Fails(Mount.Pack(unstorable)).Problem);
    }

    [Fact]
    public void AThemesIdMustBeTheOneItsPathHolds()
    {
        using var folder = new TempFolder();
        var pack = Pack(folder, "audio.pak", w =>
        {
            Music(w, [PlayableTheme("a")], ["a"]);
            w.AddRecord("audio/themes/combat.b", PlayableTheme("combatB"));
        });
        var problem = Fails(Mount.Pack(pack));
        Assert.Equal((pack, "audio/themes/combat.b"), (problem.File, problem.Entry));
        Assert.Equal("the theme's id is 'combatB', but its path holds 'combat@b' (a theme's path is made from its id)", problem.Problem);

        // A loose file is named as the file system names its own: the file, no entry.
        folder.Write("dev/audio/themes/a.json", RecordSerializer.ToJson(PlayableTheme("b")));
        problem = Fails(Mount.Pack(Pack(folder, "good.pak", w => Music(w, [PlayableTheme("a")], ["a"]))), Mount.Folder(folder["dev"]));
        Assert.Equal((folder["dev/audio/themes/a.json"], null), (problem.File, problem.Entry));
        Assert.StartsWith("the theme's id is 'b', but its path holds 'a'", problem.Problem);
    }

    [Theory]
    [InlineData("audio/themes/a_", "a theme")]
    [InlineData("audio/instruments/x/y", "an instrument")]
    [InlineData("audio/percussion/_1", "a percussion voice")]
    [InlineData("audio/rooms/__a", "a room")]
    [InlineData("audio/effects/a_-b", "an effect patch")]
    public void APathTheRuleDoesntMakeIsAnError(string path, string what)
    {
        using var folder = new TempFolder();
        var pack = Pack(folder, "audio.pak", w =>
        {
            Music(w, [PlayableTheme("a")], ["a"]);
            w.AddRecord(path, new RoomSpec(1, 0.5, 0.2));
        });
        var problem = Fails(Mount.Pack(pack));
        Assert.Equal((pack, path), (problem.File, problem.Entry));
        Assert.StartsWith($"not {what}'s path: names in {path[..path.IndexOf('/', 6)]}/ are stored by the music's rule", problem.Problem);
    }

    [Fact]
    public void AnEntryOfAnotherKindIsTheFileSystemsError()
    {
        using var folder = new TempFolder();
        var pack = Pack(folder, "audio.pak", w =>
        {
            Music(w, [PlayableTheme("a")], ["a"]);
            w.AddRecord(MusicNames.PathOf(MusicNames.Instruments, "hall"), new RoomSpec(1, 0.5, 0.2));
        });
        var problem = Fails(Mount.Pack(pack));
        Assert.Equal("a record of type audio.room, not audio.instrument", problem.Problem);
        var file = Pack(folder, "file.pak", w =>
        {
            Music(w, [PlayableTheme("a")], ["a"]);
            w.Add("audio/effects/readme", "text"u8);
        });
        Assert.Equal("a file, not a record (expected a audio.effect)", Fails(Mount.Pack(file)).Problem);
    }

    /// <summary>A theme's room, or the hall the composer falls back on (music-composer.js's rooms[spec.room] || rooms.hall), must be there.</summary>
    [Fact]
    public void AThemesRoomOrTheHallMustBeThere()
    {
        using var folder = new TempFolder();
        var noHall = Pack(folder, "no-hall.pak", w =>
        {
            Music(w, [PlayableTheme("a", room: "cavern"), PlayableTheme("b", room: "nowhere")], ["a", "b"], rooms: false);
            w.AddRecord(MusicNames.PathOf(MusicNames.Rooms, "cavern"), new RoomSpec(3.4, 0.75, 0.36));
        });
        var problem = Fails(Mount.Pack(noHall));
        Assert.Equal((noHall, "audio/themes/b"), (problem.File, problem.Entry));
        Assert.Equal("its room 'nowhere' isn't in the data, nor is the room 'hall' the composer falls back on", problem.Problem);
        var noRoom = Pack(folder, "no-room.pak", w => Music(w, [PlayableTheme("a", room: null)], ["a"], rooms: false));
        Assert.Equal("its room 'hall' isn't in the data, nor is the room 'hall' the composer falls back on", Fails(Mount.Pack(noRoom)).Problem);
        // An empty room is no room, as the score compiles it (spec.room || "hall").
        var emptyRoom = Pack(folder, "empty-room.pak", w => Music(w, [PlayableTheme("a", room: "")], ["a"], rooms: false));
        Assert.Equal("its room 'hall' isn't in the data, nor is the room 'hall' the composer falls back on", Fails(Mount.Pack(emptyRoom)).Problem);

        // With the hall there, an unknown room sounds as the hall, as in the old composer.
        var library = Load(Mount.Pack(Pack(folder, "hall.pak", w => Music(w, [PlayableTheme("b", room: "nowhere"), PlayableTheme("c", room: "")], ["b", "c"]))));
        Assert.Equal(library.Rooms["hall"], library.Room("nowhere"));
        Assert.Equal(library.Rooms["cavern"], library.Room("cavern"));
    }

    // ---------------- Themes checked against the composer as they load (G5; M11 step 5) ----------------

    /// <summary>
    /// A theme the composer can't play is refused when the data loads (G5, the user's decision of 2026-11-07), naming
    /// the pack and entry, and the field: a score that doesn't compile (the record fixtures' sample theme, whose bars
    /// hold 3 of the meter's 12 steps), a layer playing an instrument that isn't there, a mode the score doesn't know.
    /// </summary>
    [Fact]
    public void AThemeTheComposerCantPlayIsRefusedWhenTheDataLoads()
    {
        using var folder = new TempFolder();
        var sample = Pack(folder, "sample.pak", w => Music(w, [PlayableTheme("a"), SampleTheme("b")], ["a", "b"]));
        var problem = Fails(Mount.Pack(sample));
        Assert.Equal((sample, "audio/themes/b"), (problem.File, problem.Entry));
        Assert.Equal("b/A/mel: bar 1 has 3 steps, expected 12 (at $.sections[\"A\"].melody)", problem.Problem);

        var theme = PlayableTheme("b");
        var a = theme.Sections["A"];
        var unknown = theme with
        {
            Sections = new Dictionary<string, SectionSpec>(theme.Sections) { ["A"] = a with { Layers = [.. a.Layers!, new LayerSpec { Type = "pad", Instrument = "theremin" }] } },
        };
        var pack = Pack(folder, "unknown.pak", w => Music(w, [PlayableTheme("a"), unknown], ["a", "b"]));
        problem = Fails(Mount.Pack(pack));
        Assert.Equal((pack, "audio/themes/b"), (problem.File, problem.Entry));
        Assert.Equal($"Unknown instrument theremin (at $.sections[\"A\"].layers[{a.Layers!.Length}].instrument)", problem.Problem);

        // A loose file is named as the file system names its own; a mod's theme as its pack.
        folder.Write("dev/audio/themes/a.json", RecordSerializer.ToJson(PlayableTheme("a") with { Mode = "blues" }));
        var good = Pack(folder, "good.pak", w => Music(w, [PlayableTheme("a")], ["a"]));
        problem = Fails(Mount.Pack(good), Mount.Folder(folder["dev"]));
        Assert.Equal((folder["dev/audio/themes/a.json"], null), (problem.File, problem.Entry));
        Assert.Equal("Unknown mode blues (at $.mode)", problem.Problem);
        var mod = Pack(folder, "mod.pak", w => w.AddRecord(MusicNames.PathOf(MusicNames.Themes, "new-boss"), unknown with { Id = "new-boss" }));
        problem = Fails(Mount.Pack(good), Mount.Pack(mod));
        Assert.Equal((mod, "audio/themes/new-boss"), (problem.File, problem.Entry));
        Assert.StartsWith("Unknown instrument theremin (at ", problem.Problem);
        Assert.Equal(["a"], Load(Mount.Pack(good)).Themes);
    }

    /// <summary>The checks made before the theme's come first, so what they refuse keeps their words.</summary>
    [Fact]
    public void TheChecksMadeBeforeComeFirst()
    {
        using var folder = new TempFolder();
        // "a" fails the composer's check, "b" (after it) the id rule: the id rule's words.
        var id = Pack(folder, "id.pak", w =>
        {
            Music(w, [SampleTheme("a")], ["a"]);
            w.AddRecord("audio/themes/b", PlayableTheme("c"));
        });
        Assert.Equal("the theme's id is 'c', but its path holds 'b' (a theme's path is made from its id)", Fails(Mount.Pack(id)).Problem);
        Assert.Equal("its room 'nowhere' isn't in the data, nor is the room 'hall' the composer falls back on",
            Fails(Mount.Pack(Pack(folder, "room.pak", w => Music(w, [SampleTheme("a", room: "nowhere")], ["a"], rooms: false)))).Problem);
        Assert.Equal("it names the theme 'a' twice", Fails(Mount.Pack(Pack(folder, "order.pak", w => Music(w, [SampleTheme("a")], ["a", "a"])))).Problem);
        var patch = Pack(folder, "patch.pak", w =>
        {
            Music(w, [SampleTheme("a")], ["a"]);
            w.AddRecord(MusicNames.PathOf(MusicNames.Effects, "broken"), Chime with { Layers = [] });
        });
        Assert.Equal("the effect patch 'broken' has no layers", Fails(Mount.Pack(patch)).Problem);
    }

    /// <summary>The themes compiled when the data's were checked are the library's: none is compiled again when it plays.</summary>
    [Fact]
    public void ALoadedLibrarysThemesAreCompiledByItsCheck()
    {
        using var data = new VirtualFileSystem([Mount.Pack(TestMusic.Pack)]);
        var library = MusicLibrary.Load(data);
        Assert.True(library.ThemesCompiled);
        foreach (var id in library.Themes)
            Assert.Equal(MusicTests.Digest(Score.Compile(library.Theme(id)!)), MusicTests.Digest(library.Compiled(id)!));
        // A library given in code compiles its themes the first time one is asked for, as before.
        var given = new MusicLibrary([library.Theme("alder-vale")!], library.Instruments, library.Percussion, library.Rooms, library.Effects);
        Assert.False(given.ThemesCompiled);
        Assert.NotNull(given.Compiled("alder-vale"));
        Assert.True(given.ThemesCompiled);
    }

    /// <summary>S6-3: no music in the data, no music (effects still load); some music without the theme order is an error.</summary>
    [Fact]
    public void NoMusicEntriesMakeALibraryWithoutMusic()
    {
        using var folder = new TempFolder();
        var nothing = Load(Mount.Pack(Pack(folder, "nothing.pak", w => w.Add("ui/readme", "text"u8))));
        Assert.Empty(nothing.Themes);
        Assert.Empty(nothing.Instruments);
        Assert.Empty(nothing.Rooms);
        Assert.Empty(nothing.Effects);
        Assert.Empty(Load().Themes);

        var effectsOnly = Load(Mount.Pack(Pack(folder, "effects.pak", w => w.AddRecord(MusicNames.PathOf(MusicNames.Effects, "chime"), Chime))));
        Assert.Empty(effectsOnly.Themes);
        Assert.Equal(["chime"], effectsOnly.Effects.Keys);

        foreach (var (name, path) in new[] { ("theme", "audio/themes/a"), ("instrument", "audio/instruments/a"), ("percussion", "audio/percussion/a"), ("room", "audio/rooms/a") })
        {
            var pack = Pack(folder, name + ".pak", w =>
            {
                switch (name)
                {
                    case "theme": w.AddRecord(path, PlayableTheme("a", room: null)); w.AddRecord(MusicNames.PathOf(MusicNames.Rooms, "hall"), new RoomSpec(2.4, 0.55, 0.3)); break;
                    case "instrument": w.AddRecord(path, SampleInstrument); break;
                    case "percussion": w.AddRecord(path, SamplePercussion); break;
                    default: w.AddRecord(path, new RoomSpec(1, 0.5, 0.2)); break;
                }
            });
            var problem = Fails(Mount.Pack(pack));
            Assert.Equal(pack, problem.File);
            Assert.Equal("there is music in the data but no theme order ('audio/theme-order')", problem.Problem);
        }
    }

    public static TheoryData<string, string> UnplayablePatches => new()
    {
        { "no-layers", "has no layers" },
        { "no-voices", "allows no voices" },
        { "no-length", "has a layer 1 that needs a length above 0, and no negative attack or delay" },
        { "negative-attack", "has a layer 1 that needs a length above 0, and no negative attack or delay" },
        { "negative-delay", "has a layer 1 that needs a length above 0, and no negative attack or delay" },
        { "no-harmonics", "has a layer 1 with no harmonics" },
        { "empty-harmonics", "has a layer 1 with no harmonics" },
    };

    static EffectPatch Unplayable(string what)
    {
        var good = Chime.Layers[0];
        EffectPatch With(EffectLayer layer) => Chime with { Layers = [good, layer] };
        return what switch
        {
            "no-layers" => Chime with { Layers = [] },
            "no-voices" => Chime with { MaxVoices = 0 },
            "no-length" => With(good with { Length = 0 }),
            "negative-attack" => With(good with { Attack = -0.001 }),
            "negative-delay" => With(good with { Delay = -0.001 }),
            "no-harmonics" => With(good with { Source = EffectSource.Harmonics }),
            _ => With(good with { Source = EffectSource.Harmonics, Harmonics = [] }),
        };
    }

    /// <summary>A patch the data holds is checked as Register checks one (S6-4), and the problem names its entry.</summary>
    [Theory]
    [MemberData(nameof(UnplayablePatches))]
    public void AnUnplayableEffectPatchIsAnError(string what, string message)
    {
        using var folder = new TempFolder();
        var pack = Pack(folder, "audio.pak", w => w.AddRecord(MusicNames.PathOf(MusicNames.Effects, "broken"), Unplayable(what)));
        var problem = Fails(Mount.Pack(pack));
        Assert.Equal((pack, "audio/effects/broken"), (problem.File, problem.Entry));
        Assert.Equal("the effect patch 'broken' " + message, problem.Problem);
        var effects = new AudioSystem(new AudioGraph(22050, 2), MusicLibrary.Empty).Effects;
        Assert.Equal($"Effect 'broken' {message} (Parameter 'patch')", Assert.Throws<ArgumentException>(() => effects.Register("broken", Unplayable(what))).Message);
        effects.Register("fine", Chime with { Layers = [Chime.Layers[0], Chime.Layers[0] with { Attack = 0, Delay = 0 }] });
    }

    // ---------------- The audio system on a library ----------------

    static float[] Render(AudioSystem system, int quanta)
    {
        var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
        var result = new List<float>();
        for (var q = 0; q < quanta; q++)
        {
            system.BeforeQuantum();
            system.Graph.RenderQuantum(bus);
            result.AddRange(bus.Channel(0).Data);
            result.AddRange(bus.Channel(1).Data);
        }
        return [.. result];
    }

    static MusicLibrary Effects(params (string Name, EffectPatch Patch)[] patches) =>
        new([], new Dictionary<string, InstrumentSpec>(), new Dictionary<string, PercussionSpec>(), new Dictionary<string, RoomSpec>(),
            patches.ToDictionary(p => p.Name, p => p.Patch));

    /// <summary>S6-4: every patch of the library plays by its name at once; Register still adds and replaces.</summary>
    [Fact]
    public void TheLibrarysEffectsAreRegisteredByName()
    {
        var fromData = new AudioSystem(new AudioGraph(22050, 2), Effects(("chime", Chime), ("thud", Thud)));
        fromData.Effects.Play("chime");
        fromData.Effects.Play("thud", 0.5);
        var inCode = new AudioSystem(new AudioGraph(22050, 2), MusicLibrary.Empty);
        Assert.Throws<ArgumentException>(() => inCode.Effects.Play("chime"));
        inCode.Effects.Register("chime", Chime);
        inCode.Effects.Register("thud", Thud);
        inCode.Effects.Play("chime");
        inCode.Effects.Play("thud", 0.5);
        var heard = Render(fromData, 40);
        Assert.Contains(heard, s => s != 0);
        Assert.Equal(Render(inCode, 40), heard);

        var replaced = new AudioSystem(new AudioGraph(22050, 2), Effects(("chime", Chime)));
        replaced.Effects.Register("chime", Thud);
        replaced.Effects.Register("new", Chime);
        replaced.Effects.Play("chime");
        replaced.Effects.Play("new");
        var expected = new AudioSystem(new AudioGraph(22050, 2), MusicLibrary.Empty);
        expected.Effects.Register("chime", Thud);
        expected.Effects.Register("new", Chime);
        expected.Effects.Play("chime");
        expected.Effects.Play("new");
        Assert.Equal(Render(expected, 40), Render(replaced, 40));
    }

    /// <summary>S6-3: an audio system on a library without music runs; every theme id is refused as an unknown one is.</summary>
    [Fact]
    public void WithoutMusicEveryThemeIsUnknown()
    {
        var system = new AudioSystem(new AudioGraph(22050, 2), MusicLibrary.Empty);
        Assert.Empty(system.Music.Themes);
        Assert.Equal("No theme 'alder-vale' (Parameter 'themeId')", Assert.Throws<ArgumentException>(() => system.Music.Play("alder-vale")).Message);
        system.RoomsReady.Wait();
        system.WarmUp();
        Assert.All(Render(system, 8), s => Assert.Equal(0, s));
    }

    /// <summary>Libraries side by side (Q3's reason): each system plays its own themes and rooms; prepared rooms are shared only when name, values and rate agree.</summary>
    [Fact]
    public void LibrariesAreIndependent()
    {
        var game = TestMusic.Library;
        var small = new MusicLibrary([game.Theme("alder-vale")! with { Tempo = 150 }], game.Instruments, game.Percussion,
            new Dictionary<string, RoomSpec> { ["hall"] = new(1.2, 0.5, 0.3) }, new Dictionary<string, EffectPatch>());
        var a = new AudioSystem(new AudioGraph(22050, 2), game);
        var b = new AudioSystem(new AudioGraph(22050, 2), small);
        Assert.Equal(55, a.Music.Themes.Count);
        Assert.Equal(["alder-vale"], b.Music.Themes);
        b.Music.Play("alder-vale");
        Assert.Throws<ArgumentException>(() => b.Music.Play("combat"));
        a.Music.Play("combat");

        var gameHall = PreparedRoom.Get(game, "hall", 22050)!;
        Assert.Same(gameHall, PreparedRoom.Get(game, "hall", 22050));
        var smallHall = PreparedRoom.Get(small, "hall", 22050)!;
        Assert.NotSame(gameHall, smallHall);
        Assert.Equal(Performance.ResponseLength(game.Rooms["hall"], 22050), gameHall.Buffer.Length);
        Assert.Equal(Performance.ResponseLength(small.Rooms["hall"], 22050), smallHall.Buffer.Length);
        var copy = new MusicLibrary([], game.Instruments, game.Percussion, new Dictionary<string, RoomSpec> { ["hall"] = game.Rooms["hall"] with { } }, new Dictionary<string, EffectPatch>());
        Assert.Same(gameHall, PreparedRoom.Get(copy, "hall", 22050));
        Assert.NotSame(gameHall, PreparedRoom.Get(game, "hall", 44100));
        Assert.Null(PreparedRoom.Get(small, "cavern", 22050));

        var theme = game.Theme("combat")!;
        Assert.Equal("Two themes are called 'combat' (Parameter 'themes')", Assert.Throws<ArgumentException>(() => new MusicLibrary([theme, theme], game.Instruments, game.Percussion,
            game.Rooms, new Dictionary<string, EffectPatch>())).Message);
    }

    // ---------------- The committed audio.pak ----------------

    /// <summary>
    /// The committed pack: the old game's 55 themes in MusicThemes.all()'s order, 34 instruments, 21 percussion voices,
    /// 7 rooms (Q1, Q21), and since GM1 step 4 the game's 8 sound effects (GameSoundsTests).
    /// </summary>
    [Fact]
    public void TheCommittedPackHoldsTheGamesMusic()
    {
        var library = TestMusic.Library;
        Assert.Equal(55, library.Themes.Count);
        Assert.Equal(["alder-vale", "stoneveil-foothills"], library.Themes.Take(2));
        Assert.Equal(["combat", "combat@alder-vale"], library.Themes.Skip(17).Take(2));
        Assert.Equal("nygheir-the-scarred", library.Themes[^1]);
        Assert.Equal(34, library.Instruments.Count);
        Assert.Contains("lowSpiccato", library.Instruments.Keys);
        Assert.Equal(21, library.Percussion.Count);
        Assert.Equal(["cathedral", "cavern", "chamber", "forest", "hall", "mountain", "open"], library.Rooms.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(GameSounds.Names.Order(StringComparer.Ordinal), library.Effects.Keys.Order(StringComparer.Ordinal));
        using var pack = PackFile.Open(TestMusic.Pack);
        Assert.All(pack.Entries, e => Assert.True(e.IsRecord, e.Path));
        Assert.Equal(55 + 34 + 21 + 7 + 1 + 8, pack.Entries.Count);
        Assert.Contains(pack.Entries, e => e.Path == "audio/themes/combat.alder-vale");
        Assert.Contains(pack.Entries, e => e.Path == "audio/instruments/low_spiccato");
    }

    /// <summary>
    /// Packing the committed pack's extraction again gives the same bytes when the old pack's stored
    /// entries are reused (as make-music-data.sh's packer does), and the same entries when every one is
    /// compressed afresh: the pack changes only where its data does.
    /// </summary>
    [Fact]
    public void TheCommittedPackRepacksToTheSameBytes()
    {
        using var folder = new TempFolder();
        using var committed = PackFile.Open(TestMusic.Pack);
        committed.ExtractTo(folder["extracted"]);
        var reused = new PackWriter();
        reused.AddFolder(folder["extracted"]);
        using (var output = new MemoryStream())
        {
            reused.WriteTo(output, committed);
            Assert.Equal(File.ReadAllBytes(TestMusic.Pack), output.ToArray());
        }
        var fresh = new PackWriter();
        fresh.AddFolder(folder["extracted"]);
        fresh.Write(folder["fresh.pak"]);
        using var again = PackFile.Open(folder["fresh.pak"]);
        Assert.Equal(committed.Entries.Select(e => (e.Path, e.IsRecord, e.ContentLength, e.HashHex)), again.Entries.Select(e => (e.Path, e.IsRecord, e.ContentLength, e.HashHex)));
    }
}
