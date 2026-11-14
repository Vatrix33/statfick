using Statventure.Audio;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Records;

namespace Statventure.Tests.Packs;

/// <summary>
/// The mods the tests check (GM1 step 6): the game's data the committed packs (read only), each mod made by the pack writer
/// or by hand in a scratch folder, one per way a mod can break.
/// </summary>
static class ModCheckTesting
{
    /// <summary>The committed packs as the game's data under the mods (the editor's base: the game's packs).</summary>
    public static IReadOnlyList<Mount> Game => [Mount.Pack(Path.Combine(TestPaths.Packs, "audio.pak")), Mount.Pack(Path.Combine(TestPaths.Packs, "ui.pak"))];

    /// <summary>A sound effect that plays.</summary>
    public static readonly EffectPatch Chime = new() { Layers = [new() { Source = EffectSource.Sine, Frequency = 660, Length = 0.1 }] };

    public static readonly ModInfo Info = new() { Name = "Ember Rite", Author = "Vatrix", Version = "1.0", Description = "A rite of embers." };

    /// <summary>The game's record at <paramref name="path"/> in the committed audio.pak.</summary>
    public static T GameRecord<T>(string path) where T : notnull
    {
        using var audio = PackFile.Open(Path.Combine(TestPaths.Packs, "audio.pak"));
        return RecordSerializer.FromBinary<T>(audio.Read(audio.Find(path)!));
    }

    /// <summary>A mod at <paramref name="relative"/> holding what <paramref name="fill"/> writes.</summary>
    public static string Mod(TempFolder folder, string relative, Action<PackWriter> fill)
    {
        var writer = new PackWriter();
        fill(writer);
        var file = folder[relative];
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        writer.Write(file);
        return file;
    }

    /// <summary>A byte flipped at <paramref name="offset"/> of <paramref name="file"/> (from its end when negative).</summary>
    public static void Flip(string file, long offset)
    {
        var bytes = File.ReadAllBytes(file);
        bytes[offset < 0 ? bytes.Length + offset : offset] ^= 0xFF;
        File.WriteAllBytes(file, bytes);
    }

    /// <summary>A byte of the entry's stored bytes flipped (stored raw: the pack opens, the entry is damaged).</summary>
    public static void DamageEntry(string file, string path)
    {
        long offset;
        using (var pack = PackFile.Open(file))
        {
            var entry = pack.Find(path)!;
            Assert.False(entry.Compressed);
            offset = entry.Offset;
        }
        Flip(file, offset);
    }

    /// <summary>A record's binary form with its root type's version raised to 2 (one type in its table).</summary>
    public static byte[] NewerVersion<T>(T value, string name) where T : notnull
    {
        var binary = RecordSerializer.ToBinary(value);
        var version = 2 + name.Length;
        Assert.Equal(1, binary[version]);
        binary[version] = 2;
        return binary;
    }

    /// <summary>A theme of the game's that doesn't compile: its first section's melody a bar the meter doesn't hold.</summary>
    public static ThemeSpec ThemeThatDoesntCompile()
    {
        var theme = GameRecord<ThemeSpec>("audio/themes/alder-vale");
        var first = theme.Form[0];
        return theme with { Sections = theme.Sections.ToDictionary(s => s.Key, s => s.Key == first ? s.Value with { Melody = "1/1 | 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1" } : s.Value) };
    }

    public static string[] Problems(ModReport report) => [.. report.Problems.Select(p => p.ToString())];
}

/// <summary>
/// The engine's shared mod check (GM1-P13; Docs/Game/Foundation.md, "Mods: the list and the check at start"): a mod alone over
/// the game's data passes when the editor would save it, and each way it can break is its problem in the editor's words: the
/// pack doesn't open; an entry doesn't read (damaged, not a record, an unknown kind, a newer version, values its type
/// refuses); a record its checks refuse, a reference to nothing, a theme that doesn't compile, a game record the mod breaks;
/// the music's loading refusing the data; a <c>mod/info</c> that isn't a mod's info. The first problem first: the pack's,
/// else the unreadable entries, else each record's first in path order, then the data's.
/// </summary>
public class ModCheckTests
{
    static ModReport Check(string mod, RecordTypes? kinds = null) => ModCheck.Check(mod, ModCheckTesting.Game, kinds ?? RecordTypes.Engine);

    [Fact]
    public void AGoodModPassesWithItsInfo()
    {
        using var folder = new TempFolder();
        var theme = ModCheckTesting.GameRecord<ThemeSpec>("audio/themes/alder-vale");
        var mod = ModCheckTesting.Mod(folder, "mods/ember-rite.pak", w =>
        {
            w.AddRecord(ModInfo.Path, ModCheckTesting.Info);
            w.AddRecord("audio/effects/chime", ModCheckTesting.Chime);
            w.AddRecord("audio/themes/alder-vale", theme with { Tempo = theme.Tempo * 1.25 });
            w.Add("audio/readme.txt", "Embers."u8);
        });
        var report = Check(mod);
        Assert.True(report.Passed);
        Assert.Empty(report.Problems);
        Assert.Equal(mod, report.File);
        Assert.Equal(ModCheckTesting.Info, report.Info);

        // Without an info, empty, and through a path not made full: each passes, with no info.
        var plain = ModCheckTesting.Mod(folder, "mods/plain.pak", w => w.Add("ui/notes.txt", "notes"u8));
        var empty = ModCheckTesting.Mod(folder, "mods/empty.pak", _ => { });
        Assert.Equal((true, null), (Check(plain).Passed, Check(plain).Info));
        Assert.True(Check(empty).Passed);
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, plain);
        Assert.Equal(plain, Check(relative).File);
        // Nothing written beside the mods.
        Assert.Equal(new[] { empty, mod, plain }.Order(StringComparer.Ordinal), Directory.GetFiles(folder["mods"]).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void APackThatDoesntOpenIsItsOneProblem()
    {
        using var folder = new TempFolder();
        var header = folder.Write("mods/header.pak", "Not a pack at all, just text, long enough to be read as a pack's header and more."u8.ToArray());
        var shortFile = folder.Write("mods/short.pak", "Not a pack at all, just text."u8.ToArray());
        Assert.Equal(["29 bytes is too short for a pack"], ModCheckTesting.Problems(Check(shortFile)));
        var index = ModCheckTesting.Mod(folder, "mods/index.pak", w => w.AddRecord("audio/effects/chime", ModCheckTesting.Chime));
        // The index's last byte (a byte of its last entry's hash).
        ModCheckTesting.Flip(index, -1);
        var badPath = folder.Write("mods/path.pak", new RawPack().Raw("Audio/notes.txt", "x"u8.ToArray()).Build());
        Assert.Equal(["not a pack (it doesn't start with SVPK)"], ModCheckTesting.Problems(Check(header)));
        Assert.Equal(["the index doesn't match its hash (damaged)"], ModCheckTesting.Problems(Check(index)));
        // An entry named in a refused index is named with it.
        Assert.Equal(["Audio/notes.txt: a path outside the rule: 'A' isn't allowed (only a–z, 0–9, '-', '_', '.' and '/')"], ModCheckTesting.Problems(Check(badPath)));
        Assert.Equal(["the file isn't there"], ModCheckTesting.Problems(Check(folder["mods/gone.pak"])));
        Assert.All(new[] { header, index, badPath }, mod => Assert.Null(Check(mod).Info));
    }

    /// <summary>Each way an entry can't be read (M11-16), in the words the editor lists it with; the records aren't checked then.</summary>
    [Fact]
    public void AnEntryThatDoesntReadIsAProblemAndStopsTheCheck()
    {
        using var folder = new TempFolder();
        var mod = ModCheckTesting.Mod(folder, "mods/broken.pak", w =>
        {
            w.AddRecord(ModInfo.Path, ModCheckTesting.Info);
            w.Add("a/damaged", "Stored as it is, damaged once written."u8);
            w.AddRecordBinary("b/not-a-record", [0]);
            w.AddRecord("c/unknown-kind", new Records.Part { Name = "arm" });
            w.AddRecordBinary("d/newer-version", ModCheckTesting.NewerVersion(new RoomSpec(1, 0, 0), "audio.room"));
            w.AddRecordBinary("e/refused-values", RefusedCurve());
            // A record its check refuses: not reached while an entry can't be read.
            w.AddRecord("audio/effects/chime", ModCheckTesting.Chime with { MaxVoices = 0 });
        });
        ModCheckTesting.DamageEntry(mod, "a/damaged");
        var report = Check(mod);
        Assert.False(report.Passed);
        var problems = ModCheckTesting.Problems(report);
        Assert.Equal(5, problems.Length);
        Assert.Equal("a/damaged: the content doesn't match its hash (damaged)", problems[0]);
        Assert.StartsWith("b/not-a-record: not a readable record: ", problems[1]);
        Assert.Equal("c/unknown-kind: a record of type test.part, which isn't a record type known here", problems[2]);
        Assert.Equal("d/newer-version: not a readable audio.room record: audio.room version 2 is newer than this engine reads (version 1)", problems[3]);
        Assert.StartsWith("e/refused-values: not a readable scene.curve record: ", problems[4]);
        // Its info read all the same.
        Assert.Equal(ModCheckTesting.Info, report.Info);
        // The same kind known: no longer a problem.
        var withParts = new RecordTypes(new Dictionary<string, Type>(RecordTypes.Engine.ByName) { ["test.part"] = typeof(Records.Part) });
        Assert.DoesNotContain(ModCheckTesting.Problems(Check(mod, withParts)), p => p.StartsWith("c/", StringComparison.Ordinal));
    }

    /// <summary>A curve whose second key's time is made to come before the first's: FCurve refuses its keys when read.</summary>
    static byte[] RefusedCurve()
    {
        var binary = RecordSerializer.ToBinary(new FCurve([new FCurveKey(0, 0), new FCurveKey(2.5, 1)]));
        var at = binary.AsSpan().IndexOf(BitConverter.GetBytes(2.5));
        Assert.True(at > 0);
        BitConverter.GetBytes(-2.5).CopyTo(binary, at);
        return binary;
    }

    [Fact]
    public void ARecordItsChecksRefuseIsItsFirstProblem()
    {
        using var folder = new TempFolder();
        // Its own rule first, then the declared limit at the same path (the editor shows the first at each path).
        var mod = ModCheckTesting.Mod(folder, "mods/voices.pak", w => w.AddRecord("audio/effects/chime", ModCheckTesting.Chime with { MaxVoices = 0 }));
        Assert.Equal(["audio/effects/chime: allows no voices (at $.maxVoices)"], ModCheckTesting.Problems(Check(mod)));
        // Two problems in one record: the first; two records: each one's first, in path order.
        var two = ModCheckTesting.Mod(folder, "mods/two.pak", w =>
        {
            w.AddRecord("audio/effects/z-chime", ModCheckTesting.Chime with { MaxVoices = 0 });
            w.AddRecord("audio/effects/a-chime", ModCheckTesting.Chime with { MaxVoices = 0, Layers = [] });
        });
        Assert.Equal(["audio/effects/a-chime: has no layers (at $.layers)", "audio/effects/z-chime: allows no voices (at $.maxVoices)"], ModCheckTesting.Problems(Check(two)));
    }

    [Fact]
    public void AReferenceToNothingIsAProblem()
    {
        using var folder = new TempFolder();
        var order = ModCheckTesting.GameRecord<ThemeOrder>("audio/theme-order");
        var mod = ModCheckTesting.Mod(folder, "mods/order.pak", w => w.AddRecord("audio/theme-order", order with { Themes = [.. order.Themes, "nowhere"] }));
        var problems = ModCheckTesting.Problems(Check(mod));
        Assert.Single(problems);
        Assert.StartsWith("audio/theme-order: ", problems[0]);
        Assert.EndsWith($"(at $.themes[{order.Themes.Length}])", problems[0]);
        Assert.Contains("nowhere", problems[0]);
    }

    /// <summary>The theme checked against the composer with the game's data (G5).</summary>
    [Fact]
    public void AThemeThatDoesntCompileIsAProblem()
    {
        using var folder = new TempFolder();
        var mod = ModCheckTesting.Mod(folder, "mods/theme.pak", w => w.AddRecord("audio/themes/alder-vale", ModCheckTesting.ThemeThatDoesntCompile()));
        var problems = ModCheckTesting.Problems(Check(mod));
        Assert.Single(problems);
        Assert.StartsWith("audio/themes/alder-vale: ", problems[0]);
        Assert.Contains("melody", problems[0]);
    }

    /// <summary>
    /// A mod is checked with the game's records whose data it changes: the game's themes playing an instrument the mod made
    /// silent (an instrument has no check of its own: the themes' checks against the composer refuse it, G5).
    /// </summary>
    [Fact]
    public void AGameRecordTheModBreaksIsAProblem()
    {
        using var folder = new TempFolder();
        var theme = ModCheckTesting.GameRecord<ThemeSpec>("audio/themes/alder-vale");
        var name = theme.Sections.Values.SelectMany(s => s.Layers ?? []).First(l => l.Type == "pad").Instrument!;
        var instrument = ModCheckTesting.GameRecord<InstrumentSpec>(MusicNames.PathOf(MusicNames.Instruments, name));
        var mod = ModCheckTesting.Mod(folder, "mods/mute.pak", w => w.AddRecord(MusicNames.PathOf(MusicNames.Instruments, name), instrument with { Voices = [] }));
        var report = Check(mod);
        Assert.False(report.Passed);
        Assert.All(report.Problems, p => Assert.StartsWith("audio/themes/", p.Entry));
        Assert.Contains(report.Problems, p => p.Entry == "audio/themes/alder-vale");
        Assert.StartsWith($"Instrument {name} of kind {instrument.Kind} makes no sound (at $.sections", report.Problems[0].Problem);
        // In path order, each theme once.
        Assert.Equal(report.Problems.Select(p => p.Entry).Order(StringComparer.Ordinal).Distinct(), report.Problems.Select(p => p.Entry));
    }

    /// <summary>What only the music's loading refuses, on no record: the data's problem, naming the pack and entry.</summary>
    [Fact]
    public void TheMusicsLoadingProblemIsTheDatas()
    {
        using var folder = new TempFolder();
        var mod = ModCheckTesting.Mod(folder, "mods/notes.pak", w => w.Add("audio/themes/notes", "A file among the themes."u8));
        var report = Check(mod);
        Assert.Single(report.Problems);
        Assert.Null(report.Problems[0].Entry);
        Assert.StartsWith($"{mod}: entry 'audio/themes/notes': ", report.Problems[0].Problem);
        // On a record: its problem (a theme whose id isn't the one its path holds: a path rule).
        var theme = ModCheckTesting.GameRecord<ThemeSpec>("audio/themes/alder-vale");
        var moved = ModCheckTesting.Mod(folder, "mods/moved.pak", w => w.AddRecord("audio/themes/alder-dale", theme));
        Assert.Equal(["audio/themes/alder-dale: the theme's id is 'alder-vale', but its path holds 'alder-dale' (a theme's path is made from its id) (at $.id)"],
            ModCheckTesting.Problems(Check(moved)));
    }

    /// <summary>A mod's info, when there, is a <c>mod.info</c> record that reads (the game's list names the mod from it).</summary>
    [Fact]
    public void AModInfoThatIsntOneIsAProblem()
    {
        using var folder = new TempFolder();
        var file = ModCheckTesting.Mod(folder, "mods/file.pak", w =>
        {
            w.Add(ModInfo.Path, "Ember Rite by Vatrix"u8);
            w.AddRecord("audio/effects/z-chime", ModCheckTesting.Chime with { MaxVoices = 0 });
            w.AddRecord("audio/effects/a-chime", ModCheckTesting.Chime with { MaxVoices = 0 });
        });
        // Among the records' problems in path order.
        Assert.Equal(["audio/effects/a-chime: allows no voices (at $.maxVoices)", "audio/effects/z-chime: allows no voices (at $.maxVoices)", "mod/info: a mod's info must be a mod.info record (this is a file)"],
            ModCheckTesting.Problems(Check(file)));
        var first = ModCheckTesting.Mod(folder, "mods/first.pak", w =>
        {
            w.Add(ModInfo.Path, "Ember Rite by Vatrix"u8);
            w.AddRecord("zz/chime", ModCheckTesting.Chime with { MaxVoices = 0 });
            w.AddRecord("audio/effects/a-chime", ModCheckTesting.Chime with { MaxVoices = 0 });
        });
        Assert.Equal(["audio/effects/a-chime: allows no voices (at $.maxVoices)", "mod/info: a mod's info must be a mod.info record (this is a file)", "zz/chime: allows no voices (at $.maxVoices)"],
            ModCheckTesting.Problems(Check(first)));
        var other = ModCheckTesting.Mod(folder, "mods/other.pak", w => w.AddRecord(ModInfo.Path, new RoomSpec(1, 0, 0)));
        Assert.Equal(["mod/info: a mod's info must be a mod.info record (this is a audio.room record)"], ModCheckTesting.Problems(Check(other)));
        Assert.Null(Check(other).Info);
        var newer = ModCheckTesting.Mod(folder, "mods/newer.pak", w => w.AddRecordBinary(ModInfo.Path, ModCheckTesting.NewerVersion(ModCheckTesting.Info, "mod.info")));
        Assert.Equal(["mod/info: not a readable mod.info record: mod.info version 2 is newer than this engine reads (version 1)"], ModCheckTesting.Problems(Check(newer)));
        var damaged = ModCheckTesting.Mod(folder, "mods/damaged.pak", w => w.AddRecord(ModInfo.Path, ModCheckTesting.Info));
        using (var pack = PackFile.Open(damaged))
            Assert.False(pack.Find(ModInfo.Path)!.Compressed);
        ModCheckTesting.DamageEntry(damaged, ModInfo.Path);
        Assert.Equal(["mod/info: the content doesn't match its hash (damaged)"], ModCheckTesting.Problems(Check(damaged)));
        // A mod's info elsewhere isn't the mod's, and passes.
        var elsewhere = ModCheckTesting.Mod(folder, "mods/elsewhere.pak", w => w.AddRecord("notes/info", ModCheckTesting.Info));
        Assert.Equal((true, null), (Check(elsewhere).Passed, Check(elsewhere).Info));
    }

    /// <summary>The music's loading isn't tried while a record under <c>audio/</c> has a problem (its first problem is that record's).</summary>
    [Fact]
    public void TheMusicIsHeldBackWhileAMusicRecordHasAProblem()
    {
        using var folder = new TempFolder();
        var mod = ModCheckTesting.Mod(folder, "mods/held.pak", w =>
        {
            w.AddRecord("audio/effects/chime", ModCheckTesting.Chime with { MaxVoices = 0 });
            w.Add("audio/themes/notes", "A file among the themes."u8);
        });
        Assert.Equal(["audio/effects/chime: allows no voices (at $.maxVoices)"], ModCheckTesting.Problems(Check(mod)));
        // A record elsewhere doesn't hold it back.
        var elsewhere = ModCheckTesting.Mod(folder, "mods/elsewhere.pak", w =>
        {
            w.AddRecord("sounds/chime", ModCheckTesting.Chime with { MaxVoices = 0 });
            w.Add("audio/themes/notes", "A file among the themes."u8);
        });
        var problems = ModCheckTesting.Problems(Check(elsewhere));
        Assert.Equal(2, problems.Length);
        Assert.Equal("sounds/chime: allows no voices (at $.maxVoices)", problems[0]);
        Assert.StartsWith($"{elsewhere}: entry 'audio/themes/notes': ", problems[1]);
    }

    /// <summary>What only the music's loading refuses, on a record it names: that record's problem (a music folder holding another kind).</summary>
    [Fact]
    public void TheMusicsLoadingProblemOnARecordIsThatRecords()
    {
        using var folder = new TempFolder();
        var mod = ModCheckTesting.Mod(folder, "mods/room.pak", w => w.AddRecord("audio/themes/vault", new RoomSpec(1, 0, 0)));
        Assert.Equal(["audio/themes/vault: a record of type audio.room, not audio.theme"], ModCheckTesting.Problems(Check(mod)));
    }

    /// <summary>
    /// A record's first problem is the first of its own check (without the data), then what the data adds: the editor's
    /// form's order (the theme order's duplicate, found alone, before the theme that isn't there, found with the data).
    /// </summary>
    [Fact]
    public void ARecordsOwnProblemsComeBeforeWhatTheDataAdds()
    {
        using var folder = new TempFolder();
        var order = ModCheckTesting.GameRecord<ThemeOrder>("audio/theme-order");
        var mod = ModCheckTesting.Mod(folder, "mods/order.pak", w => w.AddRecord("audio/theme-order", order with { Themes = ["nowhere", .. order.Themes, order.Themes[0]] }));
        using var game = new VirtualFileSystem(ModCheckTesting.Game);
        var withData = ModCheck.Problems("audio/theme-order", order with { Themes = ["nowhere", .. order.Themes, order.Themes[0]] }, game, null);
        Assert.Equal("$.themes[0]", withData[0].Path.ToString());
        var problems = ModCheckTesting.Problems(Check(mod));
        var problem = Assert.Single(problems);
        Assert.EndsWith($"(at $.themes[{order.Themes.Length + 1}])", problem);
    }

    /// <summary>
    /// The game's data as the mounts give it: a game record the data can't give (of a kind not known here) isn't checked, as
    /// the editor shows it unreadable and doesn't check it; data that can't be mounted with the mod (a game pack damaged) is
    /// the data's problem; a check that fails for another reason (a game pack not there) is a problem, never an exception.
    /// </summary>
    [Fact]
    public void WhatTheGamesDataCantGiveIsSaidNotThrown()
    {
        using var folder = new TempFolder();
        var mod = ModCheckTesting.Mod(folder, "mods/good.pak", w => w.AddRecord("audio/effects/chime", ModCheckTesting.Chime));
        var unknown = ModCheckTesting.Mod(folder, "game/unknown.pak", w => w.AddRecord("parts/arm", new Records.Part { Name = "arm" }));
        Assert.True(ModCheck.Check(mod, [.. ModCheckTesting.Game, Mount.Pack(unknown)], RecordTypes.Engine).Passed);

        var damaged = ModCheckTesting.Mod(folder, "game/damaged.pak", w => w.Add("notes.txt", "notes"u8));
        ModCheckTesting.Flip(damaged, -1);
        var broken = ModCheck.Check(mod, [Mount.Pack(damaged)], RecordTypes.Engine);
        Assert.Equal([new ModProblem(null, $"{damaged}: the index doesn't match its hash (damaged)")], broken.Problems);

        var gone = ModCheck.Check(mod, [Mount.Pack(folder["game/gone.pak"])], RecordTypes.Engine);
        var problem = Assert.Single(gone.Problems);
        Assert.Null(problem.Entry);
        Assert.StartsWith("its check failed: ", problem.Problem);
        Assert.Contains("gone.pak", problem.Problem);
    }

    /// <summary>The order of the problems: each record's first in path order, the data's last; the count is one per entry.</summary>
    [Fact]
    public void TheProblemsComeInPathOrderTheDatasLast()
    {
        using var folder = new TempFolder();
        var mod = ModCheckTesting.Mod(folder, "mods/many.pak", w =>
        {
            w.AddRecord("audio/effects/b", ModCheckTesting.Chime with { MaxVoices = 0 });
            w.AddRecord("audio/effects/a", ModCheckTesting.Chime with { Layers = [] });
            w.AddRecord(ModInfo.Path, new RoomSpec(1, 0, 0));
        });
        Assert.Equal(["audio/effects/a: has no layers (at $.layers)", "audio/effects/b: allows no voices (at $.maxVoices)", "mod/info: a mod's info must be a mod.info record (this is a audio.room record)"],
            ModCheckTesting.Problems(Check(mod)));
    }

    /// <summary>The game's data under the mod is what <c>game</c> gives: a mod over no data is checked alone.</summary>
    [Fact]
    public void TheGamesDataIsWhatTheMountsGive()
    {
        using var folder = new TempFolder();
        var theme = ModCheckTesting.GameRecord<ThemeSpec>("audio/themes/alder-vale");
        var mod = ModCheckTesting.Mod(folder, "mods/theme.pak", w => w.AddRecord("audio/themes/alder-vale", theme));
        Assert.True(Check(mod).Passed);
        // Alone, the theme's instruments and room aren't there, and there is music without a theme order.
        var alone = ModCheck.Check(mod, [], RecordTypes.Engine);
        Assert.False(alone.Passed);
        Assert.All(alone.Problems.SkipLast(1), p => Assert.Equal("audio/themes/alder-vale", p.Entry));
    }

    [Fact]
    public void VirtualFileSystemLoadsARecordOfAnyKnownKind()
    {
        using var folder = new TempFolder();
        var mod = ModCheckTesting.Mod(folder, "mods/kinds.pak", w =>
        {
            w.AddRecord("audio/effects/chime", ModCheckTesting.Chime);
            w.AddRecord("parts/arm", new Records.Part { Name = "arm" });
            w.Add("notes.txt", "notes"u8);
        });
        using var data = new VirtualFileSystem([Mount.Pack(mod)]);
        RecordAssert.Equal(ModCheckTesting.Chime, (EffectPatch)data.Load(RecordTypes.Engine, "audio/effects/chime"));
        var unknown = Assert.Throws<DataFileException>(() => data.Load(RecordTypes.Engine, "parts/arm"));
        Assert.Equal((mod, "parts/arm", "a record of type test.part, which isn't a record type known here"), (unknown.File, unknown.Entry, unknown.Problem));
        Assert.Equal("a file, not a record", Assert.Throws<DataFileException>(() => data.Load(RecordTypes.Engine, "notes.txt")).Problem);
        Assert.Throws<FileNotFoundException>(() => data.Load(RecordTypes.Engine, "nowhere"));
    }
}
