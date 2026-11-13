using Statventure.Audio;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Packs;
using Statventure.Tests.Scene;

namespace Statventure.Tests.Audio;

/// <summary>
/// The audio kinds' marks and checks for tools (M11 step 5; Docs/Audio.md, "Checks and marks"): the kinds'
/// names, folders and the music's name rule, the strings the engine accepts, the names of other entries, units,
/// help texts, the limits the effect patch's check already holds (G4), the effect patch's and the theme order's
/// checks at the paths of what they name, and the game's data passing them all.
/// </summary>
public class AudioMarksTests
{
    static readonly Type[] AudioKinds =
    [
        typeof(ThemeSpec), typeof(ThemeEq), typeof(SectionSpec), typeof(LayerSpec), typeof(HarmonySpec), typeof(DoublingSpec), typeof(InstrumentSpec),
        typeof(PercussionSpec), typeof(RoomSpec), typeof(ThemeOrder), typeof(EffectPatch), typeof(EffectLayer), typeof(EffectFilter), typeof(EffectRange),
    ];

    static FieldShape F<T>(string name) => RecordShape.Of<T>().Field(name) ?? throw new ArgumentException(name);

    static List<string> Check(object record, VirtualFileSystem? data = null, IReferenceLookup? references = null) =>
        [.. RecordChecks.Check(record, data, references).Select(p => p.ToString())];

    /// <summary>A lookup over a pack's records: the kind of the record entry at a path (its binary form's root type).</summary>
    sealed class Kinds(PackFile pack) : IReferenceLookup
    {
        public string? KindAt(string path) => pack.Find(path) is { IsRecord: true } entry ? RecordSerializer.RootTypeName(pack.Read(entry)) : null;
    }

    // ---------------- Kinds, folders, the name rule ----------------

    [Fact]
    public void TheAudioKindsAreNamedAndFiled()
    {
        foreach (var (type, name, folder) in new (Type, string, string)[]
                 {
                     (typeof(ThemeSpec), "Theme", "audio/themes"), (typeof(InstrumentSpec), "Instrument", "audio/instruments"),
                     (typeof(PercussionSpec), "Percussion voice", "audio/percussion"), (typeof(RoomSpec), "Room", "audio/rooms"),
                     (typeof(EffectPatch), "Sound effect", "audio/effects"),
                 })
        {
            var shape = RecordShape.Of(type);
            Assert.Equal((name, folder), (shape.DisplayName, shape.Folder));
            Assert.IsType<MusicNameRule>(shape.Rule);
        }
        Assert.Equal(MusicNames.Themes, RecordShape.Of<ThemeSpec>().Folder);
        Assert.Equal(MusicNames.Effects, RecordShape.Of<EffectPatch>().Folder);
        // The theme order is one entry at a fixed path, not a kind of entries in a folder.
        Assert.Equal(("Theme order", null), (RecordShape.Of<ThemeOrder>().DisplayName, RecordShape.Of<ThemeOrder>().Folder));
        Assert.Equal(["EQ", "Section", "Layer", "Harmony", "Doubling", "Layer", "Filter", "Range"],
            new[] { typeof(ThemeEq), typeof(SectionSpec), typeof(LayerSpec), typeof(HarmonySpec), typeof(DoublingSpec), typeof(EffectLayer), typeof(EffectFilter), typeof(EffectRange) }
                .Select(t => RecordShape.Of(t).DisplayName));
        Assert.All(new[] { typeof(ThemeEq), typeof(SectionSpec), typeof(LayerSpec), typeof(EffectLayer) }, t => Assert.Null(RecordShape.Of(t).Folder));
    }

    /// <summary>The rule names entries as the library reads them (S6-1): every name of the game's music, and names and paths it can't store.</summary>
    [Fact]
    public void TheMusicsNameRuleIsTheLibrarys()
    {
        var rule = RecordShape.Of<InstrumentSpec>().Rule;
        var library = TestMusic.Library;
        foreach (var (folder, names) in new (string, IEnumerable<string>)[]
                 {
                     (MusicNames.Themes, library.Themes), (MusicNames.Instruments, library.Instruments.Keys), (MusicNames.Percussion, library.Percussion.Keys),
                     (MusicNames.Rooms, library.Rooms.Keys), (MusicNames.Effects, ["runeSlot", "combat@hit", "a-1"]),
                 })
            foreach (var name in names)
            {
                var path = MusicNames.PathOf(folder, name);
                Assert.Equal(path, rule.PathOf(folder, name));
                Assert.Equal(name, rule.NameOf(folder, path));
            }
        Assert.Equal("audio/instruments/low_spiccato", RecordShape.Of<InstrumentSpec>().PathOf("lowSpiccato"));
        Assert.Equal("combat@alder-vale", RecordShape.Of<ThemeSpec>().NameOf("audio/themes/combat.alder-vale"));
        foreach (var name in new[] { "", "a b", "a_b", "a.b", "a/b", "ä", "a@", "con", new string('a', 256) })
            Assert.Null(rule.PathOf(MusicNames.Instruments, name));
        foreach (var path in new[] { "audio/instruments/a_", "audio/instruments/_1", "audio/instruments/x/y", "audio/instruments", "audio/instrumentsx/a",
                     "audio/rooms/hall", "audio/instruments/A", "audio/instruments/a b", "" })
            Assert.Null(rule.NameOf(MusicNames.Instruments, path));
        // A folder the path rule refuses makes no path, and holds no name.
        Assert.Null(rule.PathOf("Audio", "x"));
        Assert.Null(rule.NameOf("Audio", "Audio/x"));
    }

    // ---------------- Choices, references, units, help ----------------

    /// <summary>The strings with choices offer what the engine accepts: the score's modes, the composer's layer types, textures and instrument kinds, in their order.</summary>
    [Fact]
    public void TheChoicesAreWhatTheEngineAccepts()
    {
        Assert.Equal(Score.Modes.Keys, F<ThemeSpec>("mode").Choices!);
        Assert.Equal(ComposerNames.LayerTypes, F<LayerSpec>("type").Choices!);
        Assert.Equal(ComposerNames.Textures, F<LayerSpec>("kind").Choices!);
        Assert.Equal(ComposerNames.InstrumentKinds, F<InstrumentSpec>("kind").Choices!);
        var withChoices = AudioKinds.SelectMany(t => RecordShape.Of(t).Fields).Where(f => f.Choices != null).Select(f => f.ToString());
        Assert.Equal(["audio.theme.mode", "audio.layer.type", "audio.layer.kind", "audio.instrument.kind"], withChoices);
        Assert.Equal(["\"blues\" isn't one of the choices: ionian, dorian, phrygian, lydian, mixolydian, aeolian, locrian, harmonic, hijaz, hungarian (at $.mode)"],
            Check(TestMusic.Library.Theme("alder-vale")! with { Mode = "blues" }).Skip(1));
    }

    [Fact]
    public void TheNamesOfOtherEntriesAreMarked()
    {
        void Points<TKind>(ReferenceShape? reference, bool keys = false)
        {
            Assert.NotNull(reference);
            Assert.Same(RecordShape.Of<TKind>(), reference.Kind);
            Assert.Equal(RecordShape.Of<TKind>().Folder, reference.Folder);
            Assert.IsType<MusicNameRule>(reference.Rule);
            Assert.Equal(keys, reference.Keys);
        }
        Points<RoomSpec>(Assert.Single(F<ThemeSpec>("room").References));
        var instrument = F<LayerSpec>("instrument");
        Assert.Same(F<LayerSpec>("type"), instrument.ReferenceCondition);
        foreach (var type in ComposerNames.InstrumentLayers)
            Points<InstrumentSpec>(instrument.ReferenceFor(type));
        Points<PercussionSpec>(instrument.ReferenceFor("hit"));
        foreach (var type in new[] { "tex", "drums", "harp", null })
            Assert.Null(instrument.ReferenceFor(type));
        Assert.Equal(ComposerNames.LayerTypes.Except(["tex", "drums"]).Order(), instrument.References.SelectMany(r => r.Is).Order());
        Points<PercussionSpec>(Assert.Single(F<LayerSpec>("drums").References), keys: true);
        Points<InstrumentSpec>(Assert.Single(F<HarmonySpec>("instrument").References));
        Points<InstrumentSpec>(Assert.Single(F<DoublingSpec>("instrument").References));
        Points<ThemeSpec>(Assert.Single(F<ThemeOrder>("themes").References));
        var marked = AudioKinds.SelectMany(t => RecordShape.Of(t).Fields).Where(f => f.References.Count > 0).Select(f => f.ToString());
        Assert.Equal(["audio.theme.room", "audio.layer.instrument", "audio.layer.drums", "audio.harmony.instrument", "audio.doubling.instrument", "audio.theme-order.themes"], marked);
    }

    /// <summary>Given a lookup of the data's entries, a name with no entry of its kind is a problem at its path (the editor's flag, P18).</summary>
    [Fact]
    public void ANameWithNoEntryIsFlaggedGivenTheEntries()
    {
        using var pack = PackFile.Open(TestMusic.Pack);
        var kinds = new Kinds(pack);
        var theme = TestMusic.Library.Theme("alder-vale")!;
        var section = theme.Sections[theme.Form[0]];
        var layers = section.Layers!.ToList();
        layers[0] = layers[0] with { Type = "pad", Instrument = "theremin" };
        layers.Add(new LayerSpec { Type = "drums", Drums = [new("hat", "x"), new("cowbell", "x")] });
        layers.Add(new LayerSpec { Type = "hit", Instrument = "strings" });
        layers.Add(new LayerSpec { Type = "tex", Kind = "rain", Instrument = "theremin" });
        var broken = theme with
        {
            Room = "nowhere",
            Sections = new Dictionary<string, SectionSpec>(theme.Sections) { [theme.Form[0]] = section with { Layers = [.. layers] } },
        };
        var at = $"$.sections[\"{theme.Form[0]}\"].layers";
        var n = layers.Count;
        Assert.Equal(
        [
            "no room \"nowhere\" (audio/rooms/nowhere) (at $.room)",
            $"no instrument \"theremin\" (audio/instruments/theremin) (at {at}[0].instrument)",
            $"no percussion voice \"cowbell\" (audio/percussion/cowbell) (at {at}[{n - 3}].drums[\"cowbell\"])",
            $"no percussion voice \"strings\" (audio/percussion/strings) (at {at}[{n - 2}].instrument)",
        ], Check(broken, references: kinds).Where(p => p.StartsWith("no ", StringComparison.Ordinal)));
        Assert.Equal(["\"a b\" can't name a theme in audio/themes (at $.themes[1])", "no theme \"elsewhere\" (audio/themes/elsewhere) (at $.themes[2])"],
            Check(new ThemeOrder { Themes = ["alder-vale", "a b", "elsewhere"] }, references: kinds).Where(p => !p.StartsWith("it names", StringComparison.Ordinal)));
        Assert.Equal(["audio/instruments/hall isn't an instrument: it holds a audio.room record (at $.instrument)"],
            Check(new HarmonySpec { Instrument = "hall" }, references: new Fixed(new() { ["audio/instruments/hall"] = "audio.room" })));
    }

    sealed class Fixed(Dictionary<string, string> kinds) : IReferenceLookup
    {
        public string? KindAt(string path) => kinds.GetValueOrDefault(path);
    }

    [Fact]
    public void UnitsAreMarked()
    {
        var units = AudioKinds.SelectMany(t => RecordShape.Of(t).Fields).Where(f => f.Unit != null).Select(f => $"{f} {f.Unit}");
        Assert.Equal(
        [
            "audio.theme.tempo bpm", "audio.theme.swing steps", "audio.eq.low dB", "audio.eq.presence dB", "audio.layer.every steps", "audio.layer.length steps",
            "audio.doubling.shift semitones", "audio.instrument.voices cents", "audio.instrument.cutoff Hz", "audio.instrument.attack s",
            "audio.instrument.release s", "audio.instrument.decay s", "audio.instrument.tremolo Hz", "audio.instrument.low Hz", "audio.instrument.high Hz",
            "audio.instrument.vowel Hz", "audio.instrument.length s", "audio.instrument.body Hz", "audio.instrument.highpass Hz", "audio.percussion.attack s",
            "audio.percussion.base Hz", "audio.room.seconds s", "audio.effect-layer.frequency Hz", "audio.effect-layer.frequencyTo Hz",
            "audio.effect-layer.sweepTime s", "audio.effect-layer.delay s", "audio.effect-layer.attack s", "audio.effect-layer.length s",
            "audio.effect-filter.frequency Hz", "audio.effect-filter.frequencyTo Hz", "audio.effect-filter.sweepTime s", "audio.effect-filter.gain dB",
        ], units);
    }

    /// <summary>Every audio kind and field has a help text (M11-10), a sentence for someone making a mod; none is the code's summary.</summary>
    [Fact]
    public void EveryAudioKindAndFieldHasHelp()
    {
        foreach (var type in AudioKinds)
        {
            var shape = RecordShape.Of(type);
            Assert.False(string.IsNullOrWhiteSpace(shape.Help), shape.Name);
            Assert.EndsWith(".", shape.Help);
            foreach (var field in shape.Fields)
            {
                Assert.False(string.IsNullOrWhiteSpace(field.Help), field.ToString());
                Assert.True(char.IsUpper(field.Help![0]) && field.Help.EndsWith('.'), field.ToString());
            }
        }
    }

    // ---------------- Limits (G4) and the effect patch's check ----------------

    /// <summary>The audio kinds declare only the limits the effect patch's check holds today (G4), and each refuses what that check refuses, value for value.</summary>
    [Fact]
    public void TheLimitsAreThoseThePatchsCheckHolds()
    {
        var limited = AudioKinds.SelectMany(t => RecordShape.Of(t).Fields).Where(f => f.Limits != null).Select(f => $"{f} {f.Limits!.Describe()}");
        Assert.Equal(["audio.effect.maxVoices 1 or more", "audio.effect-layer.delay 0 or more", "audio.effect-layer.attack 0 or more", "audio.effect-layer.length above 0"], limited);
        var values = new[] { double.NegativeInfinity, -1, -double.Epsilon, -0.0, 0, double.Epsilon, 0.002, 1, double.MaxValue, double.PositiveInfinity, double.NaN };
        var layer = new EffectLayer { Source = EffectSource.Sine, Length = 0.1 };
        foreach (var value in values)
        {
            Agree(new EffectPatch { Layers = [layer with { Length = value }] }, F<EffectLayer>("length"), value);
            Agree(new EffectPatch { Layers = [layer with { Attack = value }] }, F<EffectLayer>("attack"), value);
            Agree(new EffectPatch { Layers = [layer with { Delay = value }] }, F<EffectLayer>("delay"), value);
        }
        foreach (var voices in new[] { int.MinValue, -1, 0, 1, 2, int.MaxValue })
            Agree(new EffectPatch { Layers = [layer], MaxVoices = voices }, F<EffectPatch>("maxVoices"), voices);

        static void Agree(EffectPatch patch, FieldShape field, double value) =>
            Assert.True(field.Limits!.Contains(value) == (patch.Problem() is null), $"{field} {value}: {patch.Problem()}");
    }

    public static TheoryData<string, EffectPatch, string[]> BrokenPatches
    {
        get
        {
            var tone = new EffectLayer { Source = EffectSource.Sine, Length = 0.1 };
            const string Length = "has a layer 1 that needs a length above 0, and no negative attack or delay";
            return new()
            {
                { "no-layers", new() { Layers = [] }, ["has no layers (at $.layers)"] },
                { "no-voices", new() { Layers = [tone], MaxVoices = 0 }, ["allows no voices (at $.maxVoices)", "must be 1 or more (it is 0) (at $.maxVoices)"] },
                { "no-length", new() { Layers = [tone, tone with { Length = 0 }] }, [$"{Length} (at $.layers[1].length)", "must be above 0 (it is 0) (at $.layers[1].length)"] },
                {
                    "negative-attack-and-delay", new() { Layers = [tone, tone with { Attack = -1, Delay = double.NaN }] },
                    [$"{Length} (at $.layers[1].attack)", $"{Length} (at $.layers[1].delay)", "must be 0 or more (it is NaN) (at $.layers[1].delay)", "must be 0 or more (it is -1) (at $.layers[1].attack)"]
                },
                { "no-harmonics", new() { Layers = [tone, tone with { Source = EffectSource.Harmonics }] }, ["has a layer 1 with no harmonics (at $.layers[1].harmonics)"] },
                { "empty-harmonics", new() { Layers = [tone, tone with { Source = EffectSource.Harmonics, Harmonics = [] }] }, ["has a layer 1 with no harmonics (at $.layers[1].harmonics)"] },
                {
                    "everything", new() { Layers = [tone with { Length = -1, Source = EffectSource.Harmonics }, tone with { Attack = -0.5 }], MaxVoices = -2 },
                    [
                        "allows no voices (at $.maxVoices)", "has a layer 0 that needs a length above 0, and no negative attack or delay (at $.layers[0].length)",
                        "has a layer 0 with no harmonics (at $.layers[0].harmonics)", $"{Length} (at $.layers[1].attack)",
                        "must be above 0 (it is -1) (at $.layers[0].length)", "must be 0 or more (it is -0.5) (at $.layers[1].attack)", "must be 1 or more (it is -2) (at $.maxVoices)",
                    ]
                },
            };
        }
    }

    /// <summary>Each rule of the patch's check at the path of what it names (P20), in its order, before the declared limits' own words; the first is the message the game gives.</summary>
    [Theory]
    [MemberData(nameof(BrokenPatches))]
    public void APatchsRulesAreReportedAtTheirPaths(string name, EffectPatch patch, string[] expected)
    {
        _ = name;
        Assert.Equal(expected, Check(patch));
        Assert.Equal(RecordChecks.Check(patch)[0].Message, patch.Problem());
    }

    [Fact]
    public void APlayablePatchHasNoProblem()
    {
        Assert.Empty(Check(MusicLibraryTests.SampleEffect));
        Assert.Null(MusicLibraryTests.SampleEffect.Problem());
        Assert.Empty(Check(new EffectPatch { Layers = [new() { Source = EffectSource.Noise, Length = double.PositiveInfinity, Attack = 0, Delay = 0 }], MaxVoices = 1 }));
    }

    // ---------------- The theme order's check ----------------

    [Fact]
    public void TheThemeOrdersRulesAreReportedAtTheIdsTheyName()
    {
        var order = new ThemeOrder { Themes = ["alder-vale", "combat", "alder-vale", "b c", "elsewhere"] };
        Assert.Equal(["it names the theme 'alder-vale' twice (at $.themes[2])", "it names the theme 'b c', which no data path can hold (at $.themes[3])"], Check(order));
        using var data = new VirtualFileSystem([Mount.Pack(TestMusic.Pack)]);
        Assert.Equal(
        [
            "it names the theme 'alder-vale' twice (at $.themes[2])", "it names the theme 'b c', which no data path can hold (at $.themes[3])",
            "it names the theme 'elsewhere', which has no entry 'audio/themes/elsewhere' (at $.themes[4])",
        ], Check(order, data));
        Assert.Empty(Check(data.Load<ThemeOrder>(MusicNames.ThemeOrder), data));
    }

    // ---------------- The game's data ----------------

    /// <summary>Every record of the committed audio.pak passes its kind's check, the marks' and its references against the pack.</summary>
    [Fact]
    public void TheCommittedPackPasses()
    {
        using var data = new VirtualFileSystem([Mount.Pack(TestMusic.Pack)]);
        using var pack = PackFile.Open(TestMusic.Pack);
        var kinds = new Kinds(pack);
        Assert.Equal(126, pack.Entries.Count);
        foreach (var entry in pack.Entries)
        {
            Assert.True(entry.IsRecord, entry.Path);
            var record = data.Load(RecordTypes.Engine.Find(kinds.KindAt(entry.Path)!)!, entry.Path);
            Assert.Empty(Check(record, data, kinds));
        }
    }

    /// <summary>The proof cutscene's three sound patches (Data/Cutscenes/audio/effects) and the effect fixtures' pass.</summary>
    [Fact]
    public void TheTestsPatchesPass()
    {
        using var data = new VirtualFileSystem([Mount.Folder(JiruanIntro.DataFolder)]);
        var library = MusicLibrary.Load(data);
        Assert.Equal(["jiruan-flash", "jiruan-rift-opens", "jiruan-title-card"], library.Effects.Keys.Order());
        foreach (var patch in library.Effects.Values)
            Assert.Empty(Check(patch, data));
    }
}
