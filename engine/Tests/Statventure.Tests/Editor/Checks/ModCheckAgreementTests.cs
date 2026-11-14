using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Scene;
using Statventure.Tests.Packs;
using static Statventure.Tests.Editor.Saving.SavingTesting;

namespace Statventure.Tests.Editor.Checks;

/// <summary>
/// The editor refuses to save exactly the mods the game leaves out (GM1-P13, the user's Q4; Docs/Game/Foundation.md, "Mods:
/// the list and the check at start"): the same mods opened in the editor over the repo's packs (the dev setup) and checked
/// by the engine's shared mod check over the same packs, the game's start check. A pack the editor can't open is the one
/// problem of the game's check, in the editor's words; one whose entries it can't read opens read-only (Save refused) and
/// the game names those entries, with the editor's reasons; any other is refused by Save with exactly the problems the
/// game's check gives, in the same order and words.
/// </summary>
public class ModCheckAgreementTests
{
    /// <summary>Each mod the agreement is shown on: good ones, and one per way a mod can break.</summary>
    public static TheoryData<string> Mods() =>
    [
        "good", "good-without-info", "empty", "short", "header", "index", "damaged-entry", "not-a-record", "unknown-kind",
        "newer-version", "refused-values", "voices", "two-records", "theme-order", "theme-doesnt-compile", "game-theme-broken",
        "file-among-themes", "theme-moved", "room-among-themes", "order-own-first", "info-file", "info-room", "info-newer",
        "info-damaged", "many",
    ];

    static string Make(Scratch scratch, string name)
    {
        var theme = ModCheckTesting.GameRecord<ThemeSpec>("audio/themes/alder-vale");
        Action<PackWriter> fill = name switch
        {
            "good" => w =>
            {
                w.AddRecord(ModInfo.Path, ModCheckTesting.Info);
                w.AddRecord("audio/effects/chime", ModCheckTesting.Chime);
                w.AddRecord("audio/themes/alder-vale", theme with { Tempo = theme.Tempo * 1.25 });
                w.Add("audio/readme.txt", "Embers."u8);
            },
            "good-without-info" => w => w.AddRecord("audio/effects/chime", ModCheckTesting.Chime),
            "empty" or "short" or "header" => _ => { },
            "index" => w => w.AddRecord("audio/effects/chime", ModCheckTesting.Chime),
            "damaged-entry" => w => w.Add("a/damaged", "Stored as it is, damaged once written."u8),
            "not-a-record" => w => w.AddRecordBinary("b/not-a-record", [0]),
            "unknown-kind" => w => w.AddRecord("c/unknown-kind", new Records.Part { Name = "arm" }),
            "newer-version" => w => w.AddRecordBinary("d/newer-version", ModCheckTesting.NewerVersion(new RoomSpec(1, 0, 0), "audio.room")),
            "refused-values" => w => w.AddRecordBinary("e/refused-values", RefusedCurve()),
            "voices" => w => w.AddRecord("audio/effects/chime", ModCheckTesting.Chime with { MaxVoices = 0 }),
            "two-records" => w =>
            {
                w.AddRecord("audio/effects/z-chime", ModCheckTesting.Chime with { MaxVoices = 0 });
                w.AddRecord("audio/effects/a-chime", ModCheckTesting.Chime with { MaxVoices = 0, Layers = [] });
            },
            "theme-order" => w =>
            {
                var order = ModCheckTesting.GameRecord<ThemeOrder>("audio/theme-order");
                w.AddRecord("audio/theme-order", order with { Themes = [.. order.Themes, "nowhere"] });
            },
            "theme-doesnt-compile" => w => w.AddRecord("audio/themes/alder-vale", ModCheckTesting.ThemeThatDoesntCompile()),
            "game-theme-broken" => w =>
            {
                var instrument = theme.Sections.Values.SelectMany(s => s.Layers ?? []).First(l => l.Type == "pad").Instrument!;
                var path = MusicNames.PathOf(MusicNames.Instruments, instrument);
                w.AddRecord(path, ModCheckTesting.GameRecord<InstrumentSpec>(path) with { Voices = [] });
            },
            "file-among-themes" => w => w.Add("audio/themes/notes", "A file among the themes."u8),
            "theme-moved" => w => w.AddRecord("audio/themes/alder-dale", theme),
            "room-among-themes" => w => w.AddRecord("audio/themes/vault", new RoomSpec(1, 0, 0)),
            "order-own-first" => w =>
            {
                var order = ModCheckTesting.GameRecord<ThemeOrder>("audio/theme-order");
                w.AddRecord("audio/theme-order", order with { Themes = ["nowhere", .. order.Themes, order.Themes[0]] });
            },
            "info-file" => w =>
            {
                w.Add(ModInfo.Path, "Ember Rite by Vatrix"u8);
                w.AddRecord("audio/effects/chime", ModCheckTesting.Chime with { MaxVoices = 0 });
                w.AddRecord("zz/chime", ModCheckTesting.Chime with { MaxVoices = 0 });
            },
            "info-room" => w => w.AddRecord(ModInfo.Path, new RoomSpec(1, 0, 0)),
            "info-newer" => w => w.AddRecordBinary(ModInfo.Path, ModCheckTesting.NewerVersion(ModCheckTesting.Info, "mod.info")),
            "info-damaged" => w => w.AddRecord(ModInfo.Path, ModCheckTesting.Info),
            "many" => w =>
            {
                w.AddRecord("audio/effects/b", ModCheckTesting.Chime with { MaxVoices = 0 });
                w.AddRecord("audio/effects/a", ModCheckTesting.Chime with { Layers = [] });
                w.AddRecord(ModInfo.Path, new RoomSpec(1, 0, 0));
                w.Add("audio/themes/notes", "A file among the themes."u8);
            },
            _ => throw new ArgumentException(name),
        };
        var file = scratch.Mod(fill, name + ".pak");
        switch (name)
        {
            case "short":
                File.WriteAllText(file, "Not a pack at all, just text.");
                break;
            case "header":
                File.WriteAllText(file, "Not a pack at all, just text, long enough to be read as a pack's header and more.");
                break;
            case "index":
                ModCheckTesting.Flip(file, -1);
                break;
            case "damaged-entry":
                ModCheckTesting.DamageEntry(file, "a/damaged");
                break;
            case "info-damaged":
                ModCheckTesting.DamageEntry(file, ModInfo.Path);
                break;
        }
        return file;
    }

    /// <summary>A curve whose second key's time is made to come before the first's: FCurve refuses its keys when read.</summary>
    static byte[] RefusedCurve()
    {
        var binary = RecordSerializer.ToBinary(new FCurve([new FCurveKey(0, 0), new FCurveKey(2.5, 1)]));
        var at = binary.AsSpan().IndexOf(BitConverter.GetBytes(2.5));
        BitConverter.GetBytes(-2.5).CopyTo(binary, at);
        return binary;
    }

    /// <summary>Whether the editor refuses to save the mod and why, in its words: Open's refusal, the entries it can't read (read-only), or Save's problems.</summary>
    static (bool Refused, string[] Problems) Editor(Scratch scratch, string mod)
    {
        EditorSession session;
        try
        {
            session = scratch.Session(mod);
        }
        catch (DataFileException problem)
        {
            return (true, [new ModProblem(problem.Entry, problem.Problem).ToString()]);
        }
        using (session)
        {
            if (!session.CanSave)
            {
                Assert.Equal(session.Pack!.Unreadable.Count == 1 ? "an entry can't be read" : $"{session.Pack.Unreadable.Count} entries can't be read", session.Pack.ReadOnlyReason);
                return (true, [.. session.Pack.Unreadable.Select(e => $"{e.Path}: {e.Reason}")]);
            }
            var problems = session.SaveProblems();
            if (problems.Count == 0)
                // Saved over itself, as the user would: nothing refused.
                session.Save();
            else
                Assert.Throws<InvalidOperationException>(() => session.Save());
            return (problems.Count > 0, [.. problems.Select(p => p.ToString())]);
        }
    }

    [Theory]
    [MemberData(nameof(Mods))]
    public void TheEditorRefusesExactlyWhatTheGameLeavesOut(string name)
    {
        using var scratch = new Scratch();
        var mod = Make(scratch, name);
        // The game's check over the packs the editor sees the mod over, with the editor's kinds.
        List<Mount> game;
        using (var packs = new EditorSession(GameData.Repo(EditorHarness.EngineFolder)))
            game = [.. packs.GamePacks.Select(Mount.Pack)];
        var report = ModCheck.Check(mod, game, RecordTypes.Engine);
        var (refused, problems) = Editor(scratch, mod);
        Assert.Equal(refused, !report.Passed);
        Assert.Equal(problems, ModCheckTesting.Problems(report));
        Assert.Equal(name is "good" or "good-without-info" or "empty", report.Passed);
    }

    /// <summary>
    /// A music entry whose draft doesn't build holds the music's loading back (the shared check's rule, as before the move:
    /// an editor's state only, a pack on disk always builds), as one with a problem does.
    /// </summary>
    [Fact]
    public void AMusicEntryThatDoesntBuildHoldsTheMusicBack()
    {
        using var session = CheckTesting.Session(w => w.Add("audio/rooms/echo", "Not a room."u8));
        session.Checks.Run();
        Assert.Contains("audio/rooms/echo", session.Checks.DataProblem);
        var blank = session.Add("audio/rooms/new", RecordDraft.Blank(typeof(RoomSpec)));
        Assert.False(blank.Builds);
        session.Checks.Run();
        Assert.Null(session.Checks.DataProblem);
        Assert.Equal(["audio/rooms/new"], session.SaveProblems().Select(p => p.Entry));
        session.Remove(blank);
        session.Checks.Run();
        Assert.Contains("audio/rooms/echo", session.Checks.DataProblem);
    }

    /// <summary>Save As refuses a new pack the game would leave out for its info: a file where a mod keeps its info, ticked.</summary>
    [Fact]
    public void SaveAsRefusesAFileWhereAModKeepsItsInfo()
    {
        using var scratch = new Scratch();
        var mod = scratch.Mod(w =>
        {
            w.Add(ModInfo.Path, "Ember Rite by Vatrix"u8);
            w.AddRecord("audio/effects/chime", ModCheckTesting.Chime);
        });
        using var session = scratch.Session(mod);
        var file = scratch["player/mods/copy.pak"];
        Assert.Equal(["mod/info: a mod's info must be a mod.info record (this is a file)"], session.SaveAsProblems(session.SaveEntries, file).Select(p => p.ToString()));
        Assert.Throws<InvalidOperationException>(() => session.SaveAs(file, session.SaveEntries));
        Assert.False(File.Exists(file));
        // Without it ticked, the chime alone is saved.
        var chime = session.SaveEntries.Where(e => e.Path != ModInfo.Path).ToList();
        Assert.Empty(session.SaveAsProblems(chime, file));
    }

    /// <summary>A record of another kind at <c>mod/info</c> shows its problem in the editor's form, as the check's path rule (the editor's own check).</summary>
    [Fact]
    public void AnInfoOfAnotherKindShowsItsProblemOnTheEntry()
    {
        using var session = CheckTesting.Session(w => w.AddRecord(ModInfo.Path, new RoomSpec(1, 0, 0)));
        Assert.Equal(["a mod's info must be a mod.info record (this is a audio.room record)"], session.At(ModInfo.Path).Shown());
        Assert.Equal(["mod/info: a mod's info must be a mod.info record (this is a audio.room record)"], session.SaveProblems().Select(p => p.ToString()));
        // Renamed elsewhere: no problem; back: the problem again (its own check reads its path).
        session.Rename(session.At(ModInfo.Path), "notes/room");
        Assert.Empty(session.At("notes/room").Shown());
        Assert.Empty(session.SaveProblems());
        session.Rename(session.At("notes/room"), ModInfo.Path);
        Assert.Equal(["a mod's info must be a mod.info record (this is a audio.room record)"], session.At(ModInfo.Path).Shown());
    }
}
