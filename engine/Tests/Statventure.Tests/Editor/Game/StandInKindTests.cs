using Statventure.Editor.Entries;
using Statventure.Editor.Form;
using Statventure.Editor.Game;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Packs;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Editor.Game;

/// <summary>
/// The game's kinds from its data library (M12-9, Q19; M12 step 6): with the game's folder found, an entry of the
/// stand-in library's kind reads as a record and opens in the generic form, and New entry offers the kind; without the
/// library the same entry is a kind the editor doesn't know. The packer (a repo tool referencing the library) dumps,
/// extracts and packs it.
/// </summary>
public class StandInKindTests
{
    const string Lantern = "stand-in/items/lantern";

    /// <summary>A game's folder and a mod in the player's mods folder holding a stand-in item.</summary>
    static (string Game, string Player, string Mod) Scratch(StorageFolder folder)
    {
        var game = GameTesting.MakeGame(folder["game"]);
        var writer = new PackWriter();
        writer.AddRecord(Lantern, GameTesting.StandInType(), GameTesting.StandIn("Lantern", 12, "Rare"));
        writer.AddRecord("audio/rooms/crypt", new Statventure.Audio.RoomSpec(2, 0.5, 0.5));
        var mod = folder["player/mods/lights.pak"];
        Directory.CreateDirectory(Path.GetDirectoryName(mod)!);
        writer.Write(mod);
        return (game, folder["player"], mod);
    }

    [Fact]
    public void AnEntryOfTheStandInKindOpensInTheForm()
    {
        using var folder = new StorageFolder();
        var (game, player, mod) = Scratch(folder);
        using var editor = new EditorHarness(GameLookup.Find(game, EditorView.Version).Game!, 1280, 900, 1);
        editor.Session.PlayerFolder = player;
        editor.Session.Open(mod);
        var entry = editor.Session.Entry(Lantern)!;
        Assert.Equal(("Stand-in item", null), (entry.KindName, entry.Reason));
        editor.Session.Select(entry);
        editor.Frame();
        Assert.Equal(["Title", "Price", "Rarity"], new[] { "$.title", "$.price", "$.rarity" }.Select(p => editor.Row(p).ElementChildren.First().ElementChildren.First().TextContent));
        Assert.Equal("Lantern", editor.Row("$.title").Control("input").Value);
        Assert.Equal("12", editor.Row("$.price").Control("input", "number").Value);
        Assert.Equal("Rare", editor.Row("$.rarity").Control("select").Value);
        // Edited as any record: a commit, the draft builds as the library's type.
        editor.Enter(editor.Row("$.price").Control("input", "number"), "15");
        var built = editor.Session.Selected!.Build!;
        Assert.True(built.Built);
        Assert.Same(GameTesting.StandInType(), built.Value!.GetType());
        Assert.Equal(15, built.Value.GetType().GetProperty("Price")!.GetValue(built.Value));
        // New entry offers the kind, in its folder.
        Assert.Contains(editor.Session.NewKinds(), k => (k.Name, k.DisplayName, k.Folder) == (GameTesting.StandInKind, "Stand-in item", "stand-in/items"));
    }

    [Fact]
    public void WithoutTheLibraryTheKindIsntKnown()
    {
        using var folder = new StorageFolder();
        var (game, player, mod) = Scratch(folder);
        using var session = new EditorSession(GameData.Installed(game)) { PlayerFolder = player };
        session.Open(mod);
        Assert.Equal("a record of type stand-in.item, which isn't a record type known here", session.Entry(Lantern)!.Reason);
        Assert.DoesNotContain(session.NewKinds(), k => k.Name == GameTesting.StandInKind);
    }

    [Fact]
    public void ThePackerKnowsTheGamesKinds()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord(Lantern, GameTesting.StandInType(), GameTesting.StandIn("Lantern", 12, "Rare"));
        var pak = folder["lights.pak"];
        writer.Write(pak);
        var packer = Path.Combine(EditorHarness.EngineFolder, "Tools", "Packer", "bin", GameLibrary.Configuration, GameLibrary.Target, "Packer.dll");
        Assert.True(File.Exists(packer), packer);
        // The dump (git's diff driver) shows the record's form.
        var (code, dump) = Core.ProgramBuildTests.Run(Dotnet(), folder.Root, packer, "dump", pak);
        Assert.Equal(0, code);
        Assert.Contains("\"kind\": \"record\"", dump);
        Assert.Contains("\"title\": \"Lantern\"", dump);
        Assert.DoesNotContain("\"problem\"", dump);
        // Extracted as its JSON form, packed again: the same record.
        Assert.Equal(0, Core.ProgramBuildTests.Run(Dotnet(), folder.Root, packer, "extract", pak, folder["loose"]).Code);
        Assert.Contains("\"Lantern\"", File.ReadAllText(folder["loose/stand-in/items/lantern.json"]));
        Assert.Equal(0, Core.ProgramBuildTests.Run(Dotnet(), folder.Root, packer, "pack", folder["loose"], folder["again.pak"]).Code);
        using (var again = new VirtualFileSystem([Mount.Pack(folder["again.pak"])]))
            Assert.Equal(GameTesting.StandIn("Lantern", 12, "Rare"), again.Load(GameTesting.StandInType(), Lantern));
        // Updated into a pack of the engine's records: the game's kind read from the folder's JSON form too.
        var rooms = new PackWriter();
        rooms.AddRecord("audio/rooms/crypt", new Statventure.Audio.RoomSpec(2, 0.5, 0.5));
        rooms.Write(folder["rooms.pak"]);
        Assert.Equal(0, Core.ProgramBuildTests.Run(Dotnet(), folder.Root, packer, "update", folder["loose"], folder["rooms.pak"]).Code);
        using (var updated = new VirtualFileSystem([Mount.Pack(folder["rooms.pak"])]))
        {
            Assert.Equal(GameTesting.StandIn("Lantern", 12, "Rare"), updated.Load(GameTesting.StandInType(), Lantern));
            Assert.True(updated.IsRecord("audio/rooms/crypt"));
        }
    }

    /// <summary>The <c>dotnet</c> of the runtime the tests run on.</summary>
    static string Dotnet() =>
        Path.Combine(Path.GetFullPath(Path.Combine(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "..", "..", "..")),
            OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");
}
