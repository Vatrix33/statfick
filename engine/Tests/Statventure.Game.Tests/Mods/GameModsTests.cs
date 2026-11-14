using System.Runtime.ExceptionServices;
using Statventure.Audio;
using Statventure.Core;
using Statventure.Game.Data.StandIn;
using Statventure.Game.Mods;
using Statventure.Game.Settings;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Storage;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// A game laid out as the build lays it out (the committed packs in <c>game/packs/</c>) and a player's folder (<c>player/</c>)
/// whose <c>mods/</c> the tests fill: mods made by the pack writer or by hand, one per way a mod can break.
/// </summary>
sealed class ModsScratch : IDisposable
{
    public readonly ScratchFolder Folder = new();

    public ModsScratch()
    {
        foreach (var area in new[] { "audio", "ui" })
        {
            Directory.CreateDirectory(this["game/packs"]);
            File.Copy(Path.Combine(TestPaths.Packs, area + ".pak"), this[$"game/packs/{area}.pak"]);
        }
        Directory.CreateDirectory(Mods);
    }

    public string this[string relative] => Folder[relative];

    public string Player => this["player"];

    public string Mods => this["player/mods"];

    public static readonly ModInfo Info = new() { Name = "Ember Rite", Author = "Vatrix", Version = "1.0", Description = "A rite of embers." };

    public static readonly EffectPatch Chime = new() { Layers = [new() { Source = EffectSource.Sine, Frequency = 660, Length = 0.1 }] };

    /// <summary>A mod in <c>mods/</c> named <paramref name="file"/> holding what <paramref name="fill"/> writes.</summary>
    public string Mod(string file, Action<PackWriter> fill)
    {
        var writer = new PackWriter();
        fill(writer);
        var path = Path.Combine(Mods, file);
        writer.Write(path);
        return path;
    }

    /// <summary>A good mod: its info, a sound effect, an item of the game's data library's kind and a file.</summary>
    public string Good(string file, ModInfo? info = null) => Mod(file, w =>
    {
        var name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
        if (info is not null)
            w.AddRecord(ModInfo.Path, info);
        w.AddRecord($"audio/effects/{name}", Chime);
        w.AddRecord($"stand-in/items/{name}", new StandInItem { Title = "Lantern", Price = 12, Rarity = "Rare" });
        w.Add($"notes/{name}.txt", "notes"u8);
    });

    /// <summary>The game's record at <paramref name="path"/> in the committed audio.pak.</summary>
    public static T GameRecord<T>(string path) where T : notnull
    {
        using var audio = PackFile.Open(Path.Combine(TestPaths.Packs, "audio.pak"));
        return RecordSerializer.FromBinary<T>(audio.Read(audio.Find(path)!));
    }

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

    /// <summary>
    /// The game's data mounted as the engine mounts it at the start (<c>Application.DataMounts</c>) with <paramref name="mods"/>'
    /// options (<see cref="GameMods.ApplyTo"/>, the switches of <paramref name="settings"/>), and the log lines written meanwhile
    /// ("Level [Category] message", the scratch folder as &lt;root&gt;).
    /// </summary>
    public (VirtualFileSystem Data, List<string> Log) Mount(GameMods mods, SettingsSnapshot? settings = null)
    {
        var options = mods.ApplyTo(new ApplicationOptions(), settings ?? SettingsSnapshot.Defaults);
        VirtualFileSystem? data = null;
        var log = GameTesting.CaptureLog(() => data = new VirtualFileSystem(Application.DataMounts(options, this["game"], _ => null, Mods)));
        return (data!, [.. log.Select(line => line.Replace(Folder.Root, "<root>"))]);
    }

    public void Dispose() => Folder.Dispose();
}

/// <summary>
/// The game's mods (GM1-10, Q3, Q4, Q10, Q18; step 6; Docs/Game/Foundation.md, "Mods: the list and the check at start"): at the
/// start each mod in <c>mods/</c> is checked alone over the game's packs by the engine's shared check (what the editor refuses
/// to save) and mounted only when it passes, a broken one left out whole with its problem in the log and the model while the
/// others load; a mod switched off is neither checked nor mounted; the list's entries name each mod from its <c>mod/info</c>;
/// a switch is kept in the settings and marked until the next start; the real game runs the check at its start.
/// </summary>
[Collection("Game log")]
public class GameModsTests
{
    /// <summary>Each way a mod can break, with its problem as the log and the list say it (the editor's words) and how many more.</summary>
    public static TheoryData<string, string, int> Breaks() => new()
    {
        { "header", "not a pack (it doesn't start with SVPK)", 0 },
        { "index", "the index doesn't match its hash (damaged)", 0 },
        { "entry", "stand-in/items/lamp: the content doesn't match its hash (damaged)", 0 },
        { "unknown-kind", "parts/arm: a record of type test.part, which isn't a record type known here", 0 },
        { "newer-version", "stand-in/items/lamp: not a readable stand-in.item record: stand-in.item version 2 is newer than this engine reads (version 1)", 1 },
        { "not-a-record", "stand-in/items/lamp: not a readable record: the type table is empty", 0 },
        { "refused-value", "stand-in/items/lamp: \"Legendary\" isn't one of the choices: Common, Rare, Exotic (at $.rarity)", 1 },
        { "reference-to-nothing", "audio/theme-order: it names the theme 'nowhere', which has no entry 'audio/themes/nowhere' (at $.themes[55])", 0 },
        { "theme-doesnt-compile", "audio/themes/alder-vale: alder-vale/intro/mel: bar 1 has 1 steps, expected 6 (at $.sections[\"intro\"].melody)", 0 },
        { "info", "mod/info: a mod's info must be a mod.info record (this is a file)", 0 },
    };

    /// <summary>Writes the broken mod <paramref name="file"/> in the way <paramref name="how"/> says.</summary>
    static void Break(ModsScratch scratch, string file, string how)
    {
        var item = new StandInItem { Title = "Lamp", Price = 3 };
        switch (how)
        {
            case "header":
                File.WriteAllText(Path.Combine(scratch.Mods, file), "Not a pack at all, just text, long enough to be read as a pack's header and more.");
                break;
            case "index":
                ModsScratch.Flip(scratch.Mod(file, w => w.AddRecord("stand-in/items/lamp", item)), -1);
                break;
            case "entry":
                ModsScratch.DamageEntry(scratch.Mod(file, w => w.AddRecordBinary("stand-in/items/lamp", RecordSerializer.ToBinary(item))), "stand-in/items/lamp");
                break;
            case "unknown-kind":
                // A record of a kind neither the engine nor the data library has (its binary form made by hand: the
                // writer refuses no kind, the game knows none named so).
                scratch.Mod(file, w => w.AddRecordBinary("parts/arm", PartBinary()));
                break;
            case "newer-version":
            {
                var binary = RecordSerializer.ToBinary(item);
                var version = 2 + "stand-in.item".Length;
                Assert.Equal(1, binary[version]);
                binary[version] = 2;
                scratch.Mod(file, w =>
                {
                    w.AddRecordBinary("stand-in/items/lamp", binary);
                    w.AddRecordBinary("stand-in/items/torch", binary);
                });
                break;
            }
            case "not-a-record":
                scratch.Mod(file, w => w.AddRecordBinary("stand-in/items/lamp", [0]));
                break;
            case "refused-value":
                scratch.Mod(file, w =>
                {
                    w.AddRecord("stand-in/items/lamp", item with { Rarity = "Legendary" });
                    w.AddRecord("stand-in/items/torch", item with { Price = -1 });
                });
                break;
            case "reference-to-nothing":
            {
                var order = ModsScratch.GameRecord<ThemeOrder>("audio/theme-order");
                scratch.Mod(file, w => w.AddRecord("audio/theme-order", order with { Themes = [.. order.Themes, "nowhere"] }));
                break;
            }
            case "theme-doesnt-compile":
            {
                var theme = ModsScratch.GameRecord<ThemeSpec>("audio/themes/alder-vale");
                var first = theme.Form[0];
                scratch.Mod(file, w => w.AddRecord("audio/themes/alder-vale", theme with
                {
                    Sections = theme.Sections.ToDictionary(s => s.Key, s => s.Key == first ? s.Value with { Melody = "1/1 | 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1 1/1" } : s.Value),
                }));
                break;
            }
            case "info":
                scratch.Mod(file, w => w.Add(ModInfo.Path, "Ember Rite by Vatrix"u8));
                break;
            default:
                throw new ArgumentException(how);
        }
    }

    /// <summary>The binary form of a record of the kind <c>test.part</c> (one string field), by hand.</summary>
    static byte[] PartBinary()
    {
        var name = "test.part"u8.ToArray();
        // The type table: one type (its name's length, the name, its version), then the root's fields.
        return [1, (byte)name.Length, .. name, 1, .. RecordSerializer.ToBinary(new ModInfo { Name = "arm" }).AsSpan(2 + "mod.info".Length + 1)];
    }

    [Theory]
    [MemberData(nameof(Breaks))]
    public void ABrokenModIsLeftOutWithItsProblemWhileTheOthersLoad(string how, string problem, int more)
    {
        using var scratch = new ModsScratch();
        scratch.Good("a-good.pak", ModsScratch.Info);
        Break(scratch, "b-broken.pak", how);
        scratch.Good("c-good.pak");
        var mods = new GameMods();
        var (data, log) = scratch.Mount(mods);
        using (data)
        {
            // The good ones mounted, the broken one not at all.
            Assert.Equal(scratch["player/mods/a-good.pak"], data.Origin("stand-in/items/a-good"));
            Assert.Equal(scratch["player/mods/c-good.pak"], data.Origin("stand-in/items/c-good"));
            Assert.DoesNotContain(data.List(), path => data.Origin(path) == scratch["player/mods/b-broken.pak"]);
            Assert.Equal("Lantern", data.Load<StandInItem>("stand-in/items/c-good").Title);
        }
        // Its problem in the model, and the others loaded, in mounting order.
        var entries = mods.Entries;
        Assert.Equal(["a-good.pak", "b-broken.pak", "c-good.pak"], entries.Select(e => e.File));
        Assert.Equal([ModState.Loaded, ModState.LeftOut, ModState.Loaded], entries.Select(e => e.State));
        var broken = entries[1];
        Assert.Equal(problem, broken.Problem);
        Assert.Equal(more, broken.MoreProblems);
        Assert.Equal([broken], mods.LeftOut);
        Assert.Equal((null, 0), (entries[0].Problem, entries[0].MoreProblems));
        // Named from its info when it reads, else from its file.
        Assert.Equal("b-broken", broken.Name);
        // The log: each mod's line, then the file system's line for each mod mounted.
        var lines = log.Where(l => l.Contains("[Mods]") || l.Contains("[Data] mod ")).ToList();
        Assert.Equal(5, lines.Count);
        Assert.Equal("Info [Mods] a-good.pak: loaded (Ember Rite 1.0 by Vatrix)", lines[0]);
        Assert.Equal($"Warning [Mods] b-broken.pak: left out: {problem}{(more > 0 ? $" (and {more} more)" : "")}", lines[1]);
        Assert.Equal("Info [Mods] c-good.pak: loaded (c-good)", lines[2]);
        Assert.Equal(["Info [Data] mod <root>/player/mods/a-good.pak", "Info [Data] mod <root>/player/mods/c-good.pak"], lines[3..]);
    }

    [Fact]
    public void ASwitchedOffModIsNeitherCheckedNorMounted()
    {
        using var scratch = new ModsScratch();
        scratch.Good("a-good.pak", ModsScratch.Info);
        Break(scratch, "b-broken.pak", "header");
        scratch.Good("c-good.pak");
        var mods = new GameMods();
        var off = new SettingsSnapshot(null, false, new HashSet<string> { "a-good.pak", "b-broken.pak" });
        var (data, log) = scratch.Mount(mods, off);
        using (data)
        {
            Assert.False(data.Exists("stand-in/items/a-good"));
            Assert.Equal(scratch["player/mods/c-good.pak"], data.Origin("stand-in/items/c-good"));
        }
        // Off: not checked (a broken one isn't left out), still named from its info.
        Assert.Equal(
            [
                new ModEntry("a-good.pak", "Ember Rite", "Vatrix", "1.0", "A rite of embers.", ModState.Off),
                new ModEntry("b-broken.pak", "b-broken", null, null, null, ModState.Off),
                new ModEntry("c-good.pak", "c-good", null, null, null, ModState.Loaded),
            ], mods.Entries);
        Assert.Empty(mods.LeftOut);
        Assert.Equal(
            ["Info [Mods] a-good.pak: off", "Info [Mods] b-broken.pak: off", "Info [Mods] c-good.pak: loaded (c-good)", "Info [Data] mod <root>/player/mods/c-good.pak"],
            log.Where(l => l.Contains("[Mods]") || l.Contains("[Data] mod ")));
    }

    [Fact]
    public void TheSwitchIsKeptInTheSettingsAndMarkedUntilTheNextStart()
    {
        using var scratch = new ModsScratch();
        scratch.Good("a-good.pak");
        scratch.Good("b-off.pak");
        var mods = new GameMods();
        var (data, _) = scratch.Mount(mods, new SettingsSnapshot(null, false, new HashSet<string> { "b-off.pak" }));
        data.Dispose();
        // Before the settings are attached there is nowhere to keep it.
        Assert.Throws<InvalidOperationException>(() => mods.Switch("a-good.pak", false));
        using var storage = new GameStorage(scratch.Player);
        var settings = GameSettings.Load(storage);
        settings.SetModOn("b-off.pak", false);
        mods.Attach(settings);
        var changed = new List<SettingKey>();
        settings.Changed += changed.Add;
        Assert.All(mods.Entries, e => Assert.False(e.SwitchedSinceStart));

        mods.Switch("a-good.pak", false);
        Assert.False(settings.IsModOn("a-good.pak"));
        Assert.Equal([SettingKey.Mods], changed);
        // Marked, its state the start's (it takes effect at the next start).
        Assert.Equal([(ModState.Loaded, true), (ModState.Off, false)], mods.Entries.Select(e => (e.State, e.SwitchedSinceStart)));
        mods.Switch("b-off.pak", true);
        Assert.True(settings.IsModOn("b-off.pak"));
        Assert.Equal([(ModState.Loaded, true), (ModState.Off, true)], mods.Entries.Select(e => (e.State, e.SwitchedSinceStart)));
        // Switched back: as at the start, not marked.
        mods.Switch("a-good.pak", true);
        Assert.Equal([(ModState.Loaded, false), (ModState.Off, true)], mods.Entries.Select(e => (e.State, e.SwitchedSinceStart)));
        Assert.Empty(mods.LeftOut);
    }

    /// <summary>
    /// The switch kept in the real <c>settings.json</c> (step 5's <see cref="GameSettings"/>): written, read back by
    /// <see cref="GameSettings.Peek"/> before the next start, which then neither checks nor mounts the mod; switched on
    /// again, the start after checks it again.
    /// </summary>
    [Fact]
    public void ASwitchIsKeptInSettingsJsonAndTheNextStartLeavesTheModOff()
    {
        using var scratch = new ModsScratch();
        Break(scratch, "a-broken.pak", "header");
        scratch.Good("b-good.pak", ModsScratch.Info);
        var first = new GameMods();
        scratch.Mount(first, GameSettings.Peek(scratch.Player)).Data.Dispose();
        Assert.Equal([ModState.LeftOut, ModState.Loaded], first.Entries.Select(e => e.State));
        using (var storage = new GameStorage(scratch.Player))
        {
            first.Attach(GameSettings.Load(storage));
            first.Switch("a-broken.pak", false);
            first.Switch("b-good.pak", false);
            Assert.All(first.Entries, e => Assert.True(e.SwitchedSinceStart));
        }
        var snapshot = GameSettings.Peek(scratch.Player);
        Assert.Equal((false, false, true), (snapshot.IsModOn("a-broken.pak"), snapshot.IsModOn("b-good.pak"), snapshot.IsModOn("c.pak")));

        var next = new GameMods();
        var (data, log) = scratch.Mount(next, snapshot);
        using (data)
            Assert.False(data.Exists("stand-in/items/b-good"));
        Assert.Equal([(ModState.Off, null), (ModState.Off, null)], next.Entries.Select(e => (e.State, e.Problem)));
        Assert.Equal(["Info [Mods] a-broken.pak: off", "Info [Mods] b-good.pak: off"], log.Where(l => l.Contains("[Mods]")));
        using (var storage = new GameStorage(scratch.Player))
        {
            next.Attach(GameSettings.Load(storage));
            next.Switch("b-good.pak", true);
            Assert.Equal([false, true], next.Entries.Select(e => e.SwitchedSinceStart));
        }

        var after = new GameMods();
        var (again, _) = scratch.Mount(after, GameSettings.Peek(scratch.Player));
        using (again)
            Assert.Equal(scratch["player/mods/b-good.pak"], again.Origin("stand-in/items/b-good"));
        Assert.Equal([ModState.Off, ModState.Loaded], after.Entries.Select(e => e.State));
    }

    /// <summary>A record of a kind the game knows but no pack holds of its own (the save, step 5's Q26) is checked as any kind's: a mod holding one isn't broken for it.</summary>
    [Fact]
    public void AModHoldingASaveIsCheckedAsAnyKind()
    {
        using var scratch = new ModsScratch();
        scratch.Mod("a-save.pak", w => w.AddRecord("notes/save", new Data.Saves.SaveGame { Name = "Ser Alrik" }));
        scratch.Mod("b-unnamed.pak", w => w.AddRecord("notes/save", new Data.Saves.SaveGame { Name = " " }));
        var mods = new GameMods();
        scratch.Mount(mods).Data.Dispose();
        Assert.Equal([ModState.Loaded, ModState.LeftOut], mods.Entries.Select(e => e.State));
        Assert.StartsWith("notes/save: the name is empty", mods.Entries[1].Problem);
    }

    [Fact]
    public void EachModIsNamedFromItsInfo()
    {
        using var scratch = new ModsScratch();
        scratch.Good("a.pak", ModsScratch.Info);
        scratch.Good("b.pak", new ModInfo { Name = "Brighter Halls", Author = "Someone" });
        scratch.Good("c.pak", new ModInfo { Name = "Quiet Halls", Version = "beta 3" });
        scratch.Good("d.pak", new ModInfo { Author = "  ", Description = "No name given." });
        scratch.Good("e.pak");
        scratch.Good("F.PAK", new ModInfo { Name = "Capitals" });
        var mods = new GameMods();
        var (data, log) = scratch.Mount(mods);
        data.Dispose();
        Assert.Equal(
            [
                new ModEntry("F.PAK", "Capitals", null, null, null, ModState.Loaded),
                new ModEntry("a.pak", "Ember Rite", "Vatrix", "1.0", "A rite of embers.", ModState.Loaded),
                new ModEntry("b.pak", "Brighter Halls", "Someone", null, null, ModState.Loaded),
                new ModEntry("c.pak", "Quiet Halls", null, "beta 3", null, ModState.Loaded),
                new ModEntry("d.pak", "d", null, null, "No name given.", ModState.Loaded),
                new ModEntry("e.pak", "e", null, null, null, ModState.Loaded),
            ], mods.Entries);
        Assert.Equal(
            [
                "Info [Mods] F.PAK: loaded (Capitals)", "Info [Mods] a.pak: loaded (Ember Rite 1.0 by Vatrix)", "Info [Mods] b.pak: loaded (Brighter Halls by Someone)",
                "Info [Mods] c.pak: loaded (Quiet Halls beta 3)", "Info [Mods] d.pak: loaded (d)", "Info [Mods] e.pak: loaded (e)",
            ], log.Where(l => l.Contains("[Mods]")));
    }

    [Fact]
    public void SeveralProblemsAreCountedAndTheGamesKindsAreKnown()
    {
        Assert.Equal(typeof(StandInItem), GameMods.GameKinds.Find("stand-in.item"));
        Assert.Equal(typeof(ModInfo), GameMods.GameKinds.Find("mod.info"));
        Assert.Equal(typeof(EffectPatch), GameMods.GameKinds.Find("audio.effect"));
        using var scratch = new ModsScratch();
        scratch.Mod("many.pak", w =>
        {
            w.AddRecord("stand-in/items/c", new StandInItem { Title = "c", Rarity = "Legendary" });
            w.AddRecord("stand-in/items/a", new StandInItem { Title = "a", Price = -1 });
            w.AddRecord("stand-in/items/b", new StandInItem { Title = "b", Price = -2 });
            w.AddRecord(ModInfo.Path, ModsScratch.Info);
        });
        var mods = new GameMods();
        var (data, log) = scratch.Mount(mods);
        data.Dispose();
        var entry = Assert.Single(mods.Entries);
        Assert.Equal(("Ember Rite", ModState.LeftOut, 2), (entry.Name, entry.State, entry.MoreProblems));
        Assert.Equal("stand-in/items/a: must be 0 or more (it is -1) (at $.price)", entry.Problem);
        Assert.Equal($"Warning [Mods] many.pak: left out: {entry.Problem} (and 2 more)", Assert.Single(log, l => l.Contains("[Mods]")));
        // No mods: no entries.
        File.Delete(scratch["player/mods/many.pak"]);
        var none = new GameMods();
        scratch.Mount(none).Data.Dispose();
        Assert.Empty(none.Entries);
    }

    /// <summary>Notes the mods and where a mod's file comes from at <c>Load</c>, and quits in the first update.</summary>
    sealed class Probe(List<ModEntry> seen, List<string?> origins) : IGameScreen
    {
        public void Attach(GameParts parts)
        {
            seen.AddRange(parts.Mods.Entries);
            origins.Add(parts.Game.Data.Origin("notes/ember.txt"));
            parts.Ui.Document.SetTimeout(parts.Game.Quit);
        }
    }

    /// <summary>The game run for real (a hidden window, no log, no sound): its start checks the player's mods and mounts the good ones.</summary>
    [Fact]
    public void TheGameChecksTheModsAtItsStart()
    {
        using var scratch = new ScratchFolder();
        Directory.CreateDirectory(scratch["p/mods"]);
        var good = new PackWriter();
        good.AddRecord(ModInfo.Path, ModsScratch.Info);
        good.Add("notes/ember.txt", "embers"u8);
        good.Write(scratch["p/mods/a-good.pak"]);
        File.WriteAllText(scratch["p/mods/b-broken.pak"], "Not a pack at all, just text, long enough to be read as a pack's header and more.");
        var seen = new List<ModEntry>();
        var origins = new List<string?>();
        var game = GameTesting.Game(scratch["p"], screens: [new Probe(seen, origins)]);
        List<string> log = [];
        Exception? failed = null;
        // On a thread of its own with a time limit, as GameRunTests runs the game (a run that doesn't end fails the test).
        var run = new Thread(() =>
        {
            try
            {
                log = GameTesting.CaptureLog(game.Run);
            }
            catch (Exception exception)
            {
                failed = exception;
            }
        })
        {
            Name = "Stat Fick mods test run",
            IsBackground = true,
        };
        run.Start();
        Assert.True(run.Join(TimeSpan.FromSeconds(60)), "the run didn't end within 60 s");
        using var ended = game;
        if (failed is not null)
            ExceptionDispatchInfo.Capture(failed).Throw();
        Assert.Equal([ModState.Loaded, ModState.LeftOut], seen.Select(e => e.State));
        Assert.Equal(("Ember Rite", "not a pack (it doesn't start with SVPK)"), (seen[0].Name, seen[1].Problem));
        Assert.Equal([scratch["p/mods/a-good.pak"]], origins);
        Assert.Contains("Info [Mods] a-good.pak: loaded (Ember Rite 1.0 by Vatrix)", log);
        Assert.Contains("Warning [Mods] b-broken.pak: left out: not a pack (it doesn't start with SVPK)", log);
        Assert.Contains($"Info [Data] mod {scratch["p/mods/a-good.pak"]}", log);
        Assert.DoesNotContain($"Info [Data] mod {scratch["p/mods/b-broken.pak"]}", log);
    }
}
