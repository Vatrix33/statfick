using Statventure.Editor.Entries;
using Statventure.Editor.Form;
using Statventure.Editor.Game;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Packs;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Editor.Game;

/// <summary>
/// The game's kinds from its data library (M12-9, Q19; M12 step 6), since GM2 step 1 the race (<c>race</c>, the game's
/// first real kind, in place of GM1's development-only stand-in item, GM1-P3): with the game's folder found, a race
/// entry reads as a record and opens in the generic form (its nested passive, its effects as a union), and New entry
/// offers the kind in its folder; without the library the same entry is a kind the editor doesn't know. The packer (a
/// repo tool referencing the library) dumps, extracts and packs it. The game's own races are in its data.pak.
/// </summary>
public class RaceKindTests
{
    const string Ember = "data/races/ember-folk";

    /// <summary>A game's folder and a mod in the player's mods folder holding a race.</summary>
    static (string Game, string Player, string Mod) Scratch(StorageFolder folder)
    {
        var game = GameTesting.MakeGame(folder["game"]);
        var writer = new PackWriter();
        writer.AddRecord(Ember, GameTesting.RaceType(), GameTesting.Race("Ember Folk"));
        writer.AddRecord("audio/rooms/crypt", new Statventure.Audio.RoomSpec(2, 0.5, 0.5));
        var mod = folder["player/mods/embers.pak"];
        Directory.CreateDirectory(Path.GetDirectoryName(mod)!);
        writer.Write(mod);
        return (game, folder["player"], mod);
    }

    [Fact]
    public void ARaceOpensInTheForm()
    {
        using var folder = new StorageFolder();
        var (game, player, mod) = Scratch(folder);
        using var editor = new EditorHarness(GameLookup.Find(game, EditorView.Version).Game!, 1280, 900, 1);
        editor.Session.PlayerFolder = player;
        editor.Session.Open(mod);
        var entry = editor.Session.Entry(Ember)!;
        Assert.Equal(("Race", null), (entry.KindName, entry.Reason));
        editor.Session.Select(entry);
        editor.Frame();
        string Label(string path) => editor.Row(path).ElementChildren.First().ElementChildren.First().TextContent;
        Assert.Equal(["Name", "Playable", "Stat modifiers", "Passive", "Accent", "Emblem", "Order", "Swatches"],
            new[] { "$.name", "$.playable", "$.statModifiers", "$.passive", "$.accent", "$.emblem", "$.order", "$.swatches" }.Select(Label));
        Assert.Equal("Ember Folk", editor.Row("$.name").Control("input").Value);
        Assert.Equal("0", editor.Row("$.statModifiers.strength").Control("input", "number").Value);
        Assert.Equal("Ember Blood", editor.Row("$.passive.name").Control("input").Value);
        Assert.Equal("Max HP percent", editor.Row("$.passive.effects[0]").Control("select").Value);
        Assert.Equal("5", editor.Row("$.passive.effects[0].amount").Control("input", "number").Value);
        // Edited as any record: a commit, the draft builds as the library's type.
        editor.Enter(editor.Row("$.statModifiers.strength").Control("input", "number"), "2");
        var built = editor.Session.Selected!.Build!;
        Assert.True(built.Built);
        Assert.Same(GameTesting.RaceType(), built.Value!.GetType());
        var modifiers = built.Value.GetType().GetProperty("StatModifiers")!.GetValue(built.Value)!;
        Assert.Equal(2.0, modifiers.GetType().GetProperty("Strength")!.GetValue(modifiers));
        // Its checks at their fields (the library's own): an empty name.
        editor.Enter(editor.Row("$.name").Control("input"), " ");
        Assert.Equal(["the name is empty"], editor.Row("$.name").Problems());
        // New entry offers the kind, in its folder.
        Assert.Contains(editor.Session.NewKinds(), k => (k.Name, k.DisplayName, k.Folder) == (GameTesting.RaceKind, "Race", "data/races"));
    }

    [Fact]
    public void WithoutTheLibraryTheKindIsntKnown()
    {
        using var folder = new StorageFolder();
        var (game, player, mod) = Scratch(folder);
        using var session = new EditorSession(GameData.Installed(game)) { PlayerFolder = player };
        session.Open(mod);
        Assert.Equal("a record of type race, which isn't a record type known here", session.Entry(Ember)!.Reason);
        Assert.DoesNotContain(session.NewKinds(), k => k.Name == GameTesting.RaceKind);
    }

    [Fact]
    public void ThePackerKnowsTheGamesKinds()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord(Ember, GameTesting.RaceType(), GameTesting.Race("Ember Folk"));
        var pak = folder["embers.pak"];
        writer.Write(pak);
        var packer = Path.Combine(EditorHarness.EngineFolder, "Tools", "Packer", "bin", GameLibrary.Configuration, GameLibrary.Target, "Packer.dll");
        Assert.True(File.Exists(packer), packer);
        // The dump (git's diff driver) shows the record's form.
        var (code, dump) = Core.ProgramBuildTests.Run(Dotnet(), folder.Root, packer, "dump", pak);
        Assert.Equal(0, code);
        Assert.Contains("\"kind\": \"record\"", dump);
        Assert.Contains("\"name\": \"Ember Folk\"", dump);
        Assert.Contains("\"$type\": \"effect.max-hp-percent\"", dump);
        Assert.DoesNotContain("\"problem\"", dump);
        // Extracted as its JSON form, packed again: the same record.
        Assert.Equal(0, Core.ProgramBuildTests.Run(Dotnet(), folder.Root, packer, "extract", pak, folder["loose"]).Code);
        Assert.Contains("\"Ember Folk\"", File.ReadAllText(folder["loose/data/races/ember-folk.json"]));
        Assert.Equal(0, Core.ProgramBuildTests.Run(Dotnet(), folder.Root, packer, "pack", folder["loose"], folder["again.pak"]).Code);
        using (var again = new VirtualFileSystem([Mount.Pack(folder["again.pak"])]))
            Assert.Equal(Json(GameTesting.Race("Ember Folk")), Json(again.Load(GameTesting.RaceType(), Ember)));
        // Updated into a pack of the engine's records: the game's kind read from the folder's JSON form too.
        var rooms = new PackWriter();
        rooms.AddRecord("audio/rooms/crypt", new Statventure.Audio.RoomSpec(2, 0.5, 0.5));
        rooms.Write(folder["rooms.pak"]);
        Assert.Equal(0, Core.ProgramBuildTests.Run(Dotnet(), folder.Root, packer, "update", folder["loose"], folder["rooms.pak"]).Code);
        using (var updated = new VirtualFileSystem([Mount.Pack(folder["rooms.pak"])]))
        {
            Assert.Equal(Json(GameTesting.Race("Ember Folk")), Json(updated.Load(GameTesting.RaceType(), Ember)));
            Assert.True(updated.IsRecord("audio/rooms/crypt"));
        }
    }

    /// <summary>The game's 28 races (its data.pak) are the game's entries beside a mod's own race, each a race the editor reads.</summary>
    [Fact]
    public void TheGamesRacesAreItsEntries()
    {
        using var folder = new StorageFolder();
        var (game, player, mod) = Scratch(folder);
        using var session = new EditorSession(GameLookup.Find(game, EditorView.Version).Game!) { PlayerFolder = player };
        session.Open(mod);
        var races = session.Entries.Where(e => e.Path.StartsWith("data/races/", StringComparison.Ordinal)).ToList();
        Assert.All(races, e => Assert.Equal(("Race", null), (e.KindName, e.Reason)));
        Assert.Equal(28, races.Count(e => e.Source == EntrySource.Game));
        Assert.Contains(races, e => e.Path == "data/races/goliath" && e.Source == EntrySource.Game);
        Assert.Equal([Ember], races.Where(e => e.Source != EntrySource.Game).Select(e => e.Path));
    }

    /// <summary>A race's JSON form (records holding lists compare those by reference).</summary>
    static string Json(object race) => RecordSerializer.ToJson(GameTesting.RaceType(), race);

    /// <summary>The <c>dotnet</c> of the runtime the tests run on.</summary>
    static string Dotnet() =>
        Path.Combine(Path.GetFullPath(Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..")),
            OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
}
