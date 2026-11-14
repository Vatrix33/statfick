using Statventure.Audio;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Records;

namespace Statventure.Tests.Packs;

/// <summary>
/// The mod check's shortcuts (GM1 step 6b; <see cref="ModChecker"/>) give exactly the plain check's results
/// (<see cref="ModCheck.Check"/>): one checker over the game's data checks a set of mods in turn, each shortcut alone and
/// all of them, and every mod's report is the plain one's, problem for problem, in the same order and words. The set holds
/// mods that touch nothing the game's records ask the data about, mods that replace a game entry a game record's check
/// reads (an instrument the game's themes play, the room they sound in, a theme the theme order names) and mods that add
/// entries the game's records depend on (a file among the music, a part a game part names); on the committed packs and on
/// a small game of the tests' own whose records depend on the data only through their references.
/// </summary>
public class ModCheckerTests
{
    static readonly ModShortcuts[] Ways = [ModShortcuts.GameOnce, ModShortcuts.Independent, ModShortcuts.OneWalk, ModShortcuts.All];

    /// <summary>The engine's kinds and the tests' body parts and creatures (a part names another part, by a reference only).</summary>
    static RecordTypes PartKinds() => new(new Dictionary<string, Type>(RecordTypes.Engine.ByName)
    {
        ["test.part"] = typeof(Part),
        ["test.creature"] = typeof(Creature),
        ["variant.wants-data"] = typeof(Records.Variants.WantsData),
        ["variant.wants-lookup"] = typeof(Records.Variants.WantsLookup),
    });

    static string[] Text(ModReport report) => [.. report.Problems.Select(p => p.ToString())];

    /// <summary>
    /// Every mod checked by one checker per way, in the order given, against the plain check; gives how many game records
    /// each way checked again with a mod.
    /// </summary>
    static Dictionary<ModShortcuts, (int Rechecked, int WalkedOnce)> Agree(IReadOnlyList<Mount> game, RecordTypes kinds, IReadOnlyList<string> mods)
    {
        var plain = mods.Select(mod => ModCheck.Check(mod, game, kinds)).ToList();
        var rechecked = new Dictionary<ModShortcuts, (int, int)>();
        foreach (var way in Ways)
        {
            using var checker = new ModChecker(game, kinds, way);
            for (var i = 0; i < mods.Count; i++)
            {
                var fast = checker.Check(mods[i]);
                Assert.True(Text(plain[i]).SequenceEqual(Text(fast)),
                    $"{way}, {Path.GetFileName(mods[i])}: plain [{string.Join(" | ", Text(plain[i]))}] but [{string.Join(" | ", Text(fast))}]");
                Assert.Equal(plain[i].Info, fast.Info);
                Assert.Equal(plain[i].File, fast.File);
            }
            rechecked[way] = (checker.Rechecked, checker.WalkedOnce);
        }
        return rechecked;
    }

    /// <summary>The mods over the committed packs: untouched ones, ones that replace or add what the game's records read, broken ones.</summary>
    static List<string> AudioMods(TempFolder folder)
    {
        var theme = ModCheckTesting.GameRecord<ThemeSpec>("audio/themes/alder-vale");
        var instrumentName = theme.Sections.Values.SelectMany(s => s.Layers ?? []).First(l => l.Type == "pad").Instrument!;
        var instrumentPath = MusicNames.PathOf(MusicNames.Instruments, instrumentName);
        var instrument = ModCheckTesting.GameRecord<InstrumentSpec>(instrumentPath);
        var order = ModCheckTesting.GameRecord<ThemeOrder>("audio/theme-order");
        var mods = new List<string>
        {
            // Touching nothing a game record asks about.
            ModCheckTesting.Mod(folder, "mods/a-untouched.pak", w =>
            {
                w.AddRecord(ModInfo.Path, ModCheckTesting.Info);
                w.Add("notes/readme.txt", "notes"u8);
                w.AddRecord("sounds/chime", ModCheckTesting.Chime);
            }),
            // Replacing an instrument the game's themes play, made silent: the themes are refused.
            ModCheckTesting.Mod(folder, "mods/b-mute.pak", w => w.AddRecord(instrumentPath, instrument with { Voices = [] })),
            // The same again, untouched in between: nothing kept from the mod before.
            ModCheckTesting.Mod(folder, "mods/c-untouched.pak", w => w.Add("ui/images/x.svg", "<svg/>"u8)),
            // Replacing the room the game's themes sound in with an instrument: their reference and their check.
            ModCheckTesting.Mod(folder, "mods/d-room.pak", w => w.AddRecord(MusicNames.PathOf(MusicNames.Rooms, theme.Room!), instrument)),
            // Replacing a theme the theme order names with a room: the order's reference.
            ModCheckTesting.Mod(folder, "mods/e-theme-room.pak", w => w.AddRecord("audio/themes/alder-vale", new RoomSpec(1, 0, 0))),
            // Replacing the theme order (the mod's own record) and a theme with another tempo (passes).
            ModCheckTesting.Mod(folder, "mods/f-order.pak", w =>
            {
                w.AddRecord("audio/theme-order", order with { Themes = [.. order.Themes.Reverse()] });
                w.AddRecord("audio/themes/alder-vale", theme with { Tempo = theme.Tempo * 1.25 });
            }),
            // Adding an effect (the music lists them) and an instrument nobody plays.
            ModCheckTesting.Mod(folder, "mods/g-added.pak", w =>
            {
                w.AddRecord("audio/effects/new-chime", ModCheckTesting.Chime);
                w.AddRecord(MusicNames.PathOf(MusicNames.Instruments, "theremin"), instrument);
            }),
            // Adding a file among the instruments: the music's loading refuses the data.
            ModCheckTesting.Mod(folder, "mods/h-file.pak", w => w.Add("audio/instruments/notes", "Not an instrument."u8)),
            // Replacing an instrument with itself: checked again, the same.
            ModCheckTesting.Mod(folder, "mods/i-same.pak", w => w.AddRecord(instrumentPath, instrument)),
            // A file where the game's stylesheet is: the game's record gone under it.
            ModCheckTesting.Mod(folder, "mods/j-sheet.pak", w => w.Add("ui/styles/game", "Not a stylesheet."u8)),
            // Broken before the records: the pack, an entry.
            folder.Write("mods/k-header.pak", "Not a pack at all, just text, long enough to be read as a pack's header and more."u8.ToArray()),
            ModCheckTesting.Mod(folder, "mods/l-unknown.pak", w => w.AddRecord("parts/arm", new Part { Name = "arm" })),
            // A record of its own refused, nothing of the game's touched.
            ModCheckTesting.Mod(folder, "mods/m-voices.pak", w => w.AddRecord("audio/effects/zap", ModCheckTesting.Chime with { MaxVoices = 0 })),
            // A record whose problems with the data come before its own (the theme order: a theme that isn't there, then
            // one named twice): its own problems first, as the editor shows them.
            ModCheckTesting.Mod(folder, "mods/n-order.pak", w => w.AddRecord("audio/theme-order", order with { Themes = ["nowhere", .. order.Themes, order.Themes[0]] })),
        };
        return mods;
    }

    [Fact]
    public void OverTheCommittedPacksEveryModGetsThePlainChecksProblems()
    {
        using var folder = new TempFolder();
        var mods = AudioMods(folder);
        var rechecked = Agree(ModCheckTesting.Game, RecordTypes.Engine, mods);
        // And in the other order (what a mod before leaves for the next).
        mods.Reverse();
        Agree(ModCheckTesting.Game, RecordTypes.Engine, mods);
        // Only the game records a mod touches are checked again (none without the shortcut); records whose check with the
        // data didn't use it walked once (none without that shortcut).
        Assert.Equal((0, 0), rechecked[ModShortcuts.GameOnce]);
        Assert.True(rechecked[ModShortcuts.Independent].Rechecked > 0);
        Assert.Equal(0, rechecked[ModShortcuts.Independent].WalkedOnce);
        Assert.Equal(0, rechecked[ModShortcuts.OneWalk].Rechecked);
        Assert.True(rechecked[ModShortcuts.OneWalk].WalkedOnce > 0);
        Assert.True(rechecked[ModShortcuts.All].Rechecked > 0);
        Assert.True(rechecked[ModShortcuts.All].WalkedOnce > 0);
        using var checker = new ModChecker(ModCheckTesting.Game, RecordTypes.Engine);
        Assert.Equal(ModShortcuts.All, checker.Shortcuts);
        Assert.True(checker.Check(mods[^1]).Passed);
        Assert.Equal(0, checker.Rechecked);
        Assert.False(checker.Check(mods[^2]).Passed);
        Assert.True(checker.Rechecked > 0);
    }

    /// <summary>
    /// A game of the tests' own: body parts that name each other, checked against the data only through their references
    /// (the kinds the lookup gives): a mod replacing a named part with a file, one adding a part a game part names but the
    /// game lacks, one replacing it with a creature, one touching nothing.
    /// </summary>
    [Fact]
    public void AGameRecordDependingOnlyThroughItsReferencesIsCheckedAgain()
    {
        using var folder = new TempFolder();
        var game = ModCheckTesting.Mod(folder, "game/parts.pak", w =>
        {
            w.AddRecord("parts/arm", new Part { Name = "leg" });
            w.AddRecord("parts/leg", new Part { Name = "arm" });
            w.AddRecord("parts/foot", new Part { Name = "hand" });
            // Checks that only ask whether they are given the data or the lookup (they differ alone).
            w.AddRecord("asks/data", new Records.Variants.WantsData { Name = "game" });
            w.AddRecord("asks/lookup", new Records.Variants.WantsLookup { Name = "game" });
        });
        var mods = new List<string>
        {
            ModCheckTesting.Mod(folder, "mods/a-file.pak", w => w.Add("parts/leg", "Not a part."u8)),
            ModCheckTesting.Mod(folder, "mods/b-hand.pak", w => w.AddRecord("parts/hand", new Part { Name = "foot" })),
            ModCheckTesting.Mod(folder, "mods/c-none.pak", w => w.Add("notes/readme.txt", "notes"u8)),
            ModCheckTesting.Mod(folder, "mods/d-creature.pak", w => w.AddRecord("parts/leg", LooseRecordTests.Ogre)),
            ModCheckTesting.Mod(folder, "mods/e-asks.pak", w =>
            {
                w.AddRecord("asks/mine-data", new Records.Variants.WantsData { Name = "mod" });
                w.AddRecord("asks/mine-lookup", new Records.Variants.WantsLookup { Name = "mod" });
            }),
        };
        var kinds = PartKinds();
        Agree([Mount.Pack(game)], kinds, mods);
        mods.Reverse();
        Agree([Mount.Pack(game)], kinds, mods);
        // The game's own problem (a part naming one it lacks) is there without the hand, gone with it; the checks that ask
        // for the data or the lookup refuse their records alone (their own problems), the game's and the mod's.
        string[] asks = ["asks/data: checked without the data", "asks/lookup: checked without the lookup"];
        Assert.Equal([.. asks, "parts/foot: no body part \"hand\" (parts/hand) (at $.name)"], Text(ModCheck.Check(mods[2], [Mount.Pack(game)], kinds)));
        Assert.Equal(asks, Text(ModCheck.Check(mods[3], [Mount.Pack(game)], kinds)));
        Assert.Equal([.. asks, "parts/arm: no body part \"leg\" (parts/leg) (at $.name)", "parts/foot: no body part \"hand\" (parts/hand) (at $.name)"],
            Text(ModCheck.Check(mods[4], [Mount.Pack(game)], kinds)));
        Assert.Equal([.. asks, "asks/mine-data: checked without the data", "asks/mine-lookup: checked without the lookup", "parts/foot: no body part \"hand\" (parts/hand) (at $.name)"],
            Text(ModCheck.Check(mods[0], [Mount.Pack(game)], kinds)));
    }

    /// <summary>
    /// A game of the tests' own whose records ask the data in each of its ways (<see cref="Records.Variants.AsksData"/>):
    /// whether a path is there, whether it is a record, its length, a record loaded as one kind and as any, where a path
    /// comes from, the pack a problem names, a folder's paths two levels down, the mounts; each mod changes what one of them
    /// asks about (the mounts: a mod with no entries at all), one touches none of their paths.
    /// </summary>
    [Fact]
    public void AGameRecordAskingTheDataInAnyWayIsCheckedAgain()
    {
        using var folder = new TempFolder();
        static Records.Variants.AsksData Asks(string way, string at = "") => new() { Way = way, At = at };
        var game = ModCheckTesting.Mod(folder, "game/asks.pak", w =>
        {
            w.Add("data/a.txt", "a"u8);
            w.Add("data/b.txt", "b"u8);
            w.Add("deep/x/y.txt", "y"u8);
            w.AddRecord("asks/exists", Asks("exists", "data/new.txt"));
            w.AddRecord("asks/record", Asks("record", "data/a.txt"));
            w.AddRecord("asks/bytes", Asks("bytes", "data/b.txt"));
            w.AddRecord("asks/load", Asks("load", "targets/one"));
            w.AddRecord("asks/kinds", Asks("kinds", "targets/two"));
            w.AddRecord("asks/origin", Asks("origin", "data/a.txt"));
            w.AddRecord("asks/problem", Asks("problem", "data/b.txt"));
            w.AddRecord("asks/list", Asks("list", "deep"));
            w.AddRecord("asks/mounts", Asks("mounts"));
            w.AddRecord("targets/one", Asks("first"));
            w.AddRecord("targets/two", Asks("second"));
        });
        var mods = new List<string>
        {
            ModCheckTesting.Mod(folder, "mods/a-nothing.pak", _ => { }),
            ModCheckTesting.Mod(folder, "mods/b-exists.pak", w => w.Add("data/new.txt", "new"u8)),
            ModCheckTesting.Mod(folder, "mods/c-record.pak", w => w.AddRecord("data/a.txt", Asks("third"))),
            ModCheckTesting.Mod(folder, "mods/d-bytes.pak", w => w.Add("data/b.txt", "longer"u8)),
            ModCheckTesting.Mod(folder, "mods/e-load.pak", w => w.AddRecord("targets/one", Asks("replaced"))),
            ModCheckTesting.Mod(folder, "mods/f-kinds.pak", w => w.AddRecord("targets/two", ModCheckTesting.Chime)),
            ModCheckTesting.Mod(folder, "mods/g-deep.pak", w => w.Add("deep/x/z.txt", "z"u8)),
            ModCheckTesting.Mod(folder, "mods/h-other.pak", w => w.Add("other/q.txt", "q"u8)),
        };
        var kinds = Records.Variants.AsksData.Kinds;
        Agree([Mount.Pack(game)], kinds, mods);
        mods.Reverse();
        Agree([Mount.Pack(game)], kinds, mods);
        // What the game's records answer under the mod with no entries: only the mounts differ from the game's alone.
        Assert.Equal(
            [
                "asks/bytes: bytes data/b.txt: 1", "asks/exists: exists data/new.txt: False", "asks/kinds: kinds targets/two: variant.asks-data",
                "asks/list: list deep: deep/x/y.txt", "asks/load: load targets/one: first", "asks/mounts: mounts : 2",
                "asks/origin: origin data/a.txt: asks.pak", "asks/problem: problem data/b.txt: asks.pak", "asks/record: record data/a.txt: False",
            ],
            Text(ModCheck.Check(mods[^1], [Mount.Pack(game)], kinds)).Where(p => p.StartsWith("asks/", StringComparison.Ordinal)));
    }

    /// <summary>When the game's data can't be mounted alone, every mod is checked plainly (the same problems as the plain check).</summary>
    [Fact]
    public void AGameThatCantBeMountedIsCheckedPlainly()
    {
        using var folder = new TempFolder();
        var mod = ModCheckTesting.Mod(folder, "mods/good.pak", w => w.AddRecord("audio/effects/chime", ModCheckTesting.Chime));
        var damaged = ModCheckTesting.Mod(folder, "game/damaged.pak", w => w.Add("notes.txt", "notes"u8));
        ModCheckTesting.Flip(damaged, -1);
        Agree([Mount.Pack(damaged)], RecordTypes.Engine, [mod]);
        Agree([Mount.Pack(folder["game/gone.pak"])], RecordTypes.Engine, [mod]);
        using var checker = new ModChecker([Mount.Pack(damaged)], RecordTypes.Engine);
        checker.Dispose();
        Assert.Throws<ObjectDisposedException>(() => checker.Check(mod));
    }

    /// <summary>A recording notes each question to the data: paths, folders listed, the mounts; nested ones each; outside one, nothing.</summary>
    [Fact]
    public void ARecordingNotesTheDatasQuestions()
    {
        using var folder = new TempFolder();
        var pack = ModCheckTesting.Mod(folder, "game/a.pak", w =>
        {
            w.AddRecord("audio/effects/a", ModCheckTesting.Chime);
            w.AddRecord("audio/effects/b", ModCheckTesting.Chime);
            w.AddRecord("audio/effects/c", ModCheckTesting.Chime);
            w.Add("notes/a.txt", "a"u8);
            w.Add("notes/c.txt", "c"u8);
        });
        using var data = new VirtualFileSystem([Mount.Pack(pack)]);
        DataReads inner = null!;
        DataReads.During(() =>
        {
            _ = data.Exists("x/y");
            _ = data.IsRecord("audio/effects/a");
            _ = data.Load<EffectPatch>("audio/effects/b");
            _ = data.Load(RecordTypes.Engine, "audio/effects/c");
            _ = data.ReadAllBytes("notes/a.txt");
            _ = data.Origin("notes/b.txt");
            _ = data.Problem("notes/c.txt", "a problem");
            return DataReads.During(() =>
            {
                _ = data.Exists("inner/p");
                return data.List("notes");
            }, out inner);
        }, out var outer);
        // Each question in the recordings running: the inner one's in the outer one too.
        Assert.Equal(["audio/effects/a", "audio/effects/b", "audio/effects/c", "inner/p", "notes/a.txt", "notes/b.txt", "notes/c.txt", "x/y"],
            outer.Paths.Order(StringComparer.Ordinal));
        Assert.Equal(["notes"], outer.Folders);
        Assert.Equal(["notes"], inner.Folders);
        Assert.Equal(["inner/p"], inner.Paths);
        Assert.False(inner.Everything);
        Assert.False(outer.Everything);
        DataReads.During(() => data.Mounts, out var mounts);
        Assert.True(mounts.Everything);
        // Touches: a path read, one below a folder listed (two levels down too), the mounts (with a mount holding nothing as
        // well); not a path beside them, nor the folder itself.
        Assert.True(outer.Touches(new HashSet<string> { "x/y" }));
        Assert.True(outer.Touches(new HashSet<string> { "notes/c/d.txt" }));
        Assert.False(outer.Touches(new HashSet<string> { "notes" }));
        Assert.False(outer.Touches(new HashSet<string> { "x/y/z", "x", "audio/effects/boom" }));
        Assert.False(outer.Touches(new HashSet<string>()));
        Assert.True(mounts.Touches(new HashSet<string> { "anything" }));
        Assert.True(mounts.Touches(new HashSet<string>()));
        DataReads.During(() => data.List(), out var all);
        Assert.True(all.Touches(new HashSet<string> { "anything" }));
        Assert.False(all.Touches(new HashSet<string>()));
        // Outside a recording nothing is kept.
        _ = data.Exists("z");
        Assert.DoesNotContain("z", outer.Paths);
    }
}
