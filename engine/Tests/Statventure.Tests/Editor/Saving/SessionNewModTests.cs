using Statventure.Audio;
using Statventure.Core;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using static Statventure.Tests.Editor.Checks.CheckTesting;
using static Statventure.Tests.Editor.Saving.SavingTesting;

namespace Statventure.Tests.Editor.Saving;

/// <summary>
/// New mod (M11-5, M11-15, P25; the manager's decision Q2 of 2026-11-08; Docs/Editor.md, "How it works", step 11): the file
/// name suggested from the mod's name under the path rule, the names it refuses, the mod written at once into the player's
/// mods folder holding its info and opened; then, edited and saved, mounted as the game mounts mods
/// (<see cref="Application.DataMounts"/>) over the game's packs and read back equal.
/// </summary>
public class SessionNewModTests
{
    [Theory]
    [InlineData("My Mod", "my-mod.pak")]
    [InlineData("  Ünïcode Tales!  ", "unicode-tales.pak")]
    [InlineData("Brighter Halls (v2)", "brighter-halls-v2.pak")]
    [InlineData("con", "con-1.pak")]
    [InlineData("???", "mod.pak")]
    [InlineData("imported", "imported.pak")]
    [InlineData("Imported", "imported.pak")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void TheFileNameIsSuggestedFromTheModsNameUnderThePathRule(string name, string file) =>
        Assert.Equal(file, EditorSession.NewModFileName(name));

    [Fact]
    public void NewModRefusesWhatCantBeAModsName()
    {
        using var scratch = new Scratch();
        using var session = new EditorSession(GameData.Repo(EditorHarness.EngineFolder)) { PlayerFolder = scratch.Player };
        // The mods folder isn't there yet: nothing is in it.
        Assert.False(Directory.Exists(scratch.Mods));
        Assert.Null(session.NewModProblem("My mod", "my-mod.pak"));
        Assert.Equal("Give the mod a name.", session.NewModProblem("", "my-mod.pak"));
        Assert.Equal("Give the mod a name.", session.NewModProblem("  ", "my-mod.pak"));
        Assert.Equal("Give the mod's file a name.", session.NewModProblem("My mod", ""));
        Assert.Equal("A mod's file name ends in .pak.", session.NewModProblem("My mod", "my-mod"));
        Assert.Equal("A mod's file name ends in .pak.", session.NewModProblem("My mod", "my-mod.PAK"));
        Assert.Equal("That can't be the file's name: it holds a '/'.", session.NewModProblem("My mod", "mods/my-mod.pak"));
        Assert.Equal("That can't be the file's name: ' ' isn't allowed (only a–z, 0–9, '-', '_', '.' and '/').", session.NewModProblem("My mod", "my mod.pak"));
        Assert.Equal("That can't be the file's name: 'M' isn't allowed (only a–z, 0–9, '-', '_', '.' and '/').", session.NewModProblem("My mod", "My-mod.pak"));
        Assert.Equal("That can't be the file's name: 'nul.pak' is a name Windows keeps for a device.", session.NewModProblem("My mod", "nul.pak"));
        Assert.Throws<ArgumentException>(() => session.NewMod("", "my-mod.pak"));
        Assert.False(Directory.Exists(scratch.Mods));
        // A mod already there, in any case, is never replaced.
        scratch.Mod(w => w.Add("notes/a.txt", "A"u8), "My-Mod.pak");
        Assert.Equal("There is already My-Mod.pak in the mods folder.", session.NewModProblem("My mod", "my-mod.pak"));
        Directory.CreateDirectory(Path.Combine(scratch.Mods, "other.pak"));
        Assert.Equal("There is already other.pak in the mods folder.", session.NewModProblem("Other", "other.pak"));
        Assert.Null(session.NewModProblem("Third", "third.pak"));
    }

    [Fact]
    public void NewModWritesTheModAtOnceHoldingItsInfoAndOpensIt()
    {
        using var scratch = new Scratch();
        using var session = new EditorSession(GameData.Repo(EditorHarness.EngineFolder)) { PlayerFolder = scratch.Player };
        var opened = session.NewMod("  Brighter Halls ", "brighter-halls.pak");
        var file = Path.Combine(scratch.Mods, "brighter-halls.pak");
        Assert.Equal(file, opened.File);
        Assert.Same(opened, session.Pack);
        Assert.True(session.CanSave);
        Assert.False(session.HasUnsavedWork);
        var contents = Contents(file);
        Assert.Equal([ModInfo.Path], contents.Keys);
        Assert.Equal((true, Binary(new ModInfo { Name = "Brighter Halls" })), contents[ModInfo.Path]);
        Assert.Equal(new ModInfo { Name = "Brighter Halls" }, session.At(ModInfo.Path).Saved);
        Assert.Equal(EntrySource.Pack, session.At(ModInfo.Path).Source);
        Assert.Equal(["brighter-halls.pak"], Directory.GetFiles(scratch.Mods).Select(Path.GetFileName));
    }

    /// <summary>
    /// A mod made from scratch, its info and a room edited and saved, mounted as the game mounts mods over its packs: the
    /// game reads back what the editor shows, and the music loads with it.
    /// </summary>
    [Fact]
    public void ANewModSavedIsReadBackAsTheGameMountsIt()
    {
        using var scratch = new Scratch();
        // The game's folder: its packs (copies of the repo's) in packs/ beside the program.
        var game = scratch["game"];
        Directory.CreateDirectory(Path.Combine(game, "packs"));
        foreach (var pack in Directory.GetFiles(Infrastructure.TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(game, "packs", Path.GetFileName(pack)));
        using var session = new EditorSession(new GameData(Path.Combine(game, "packs"), DevSetup: false)) { PlayerFolder = scratch.Player };
        _ = session.Checks;
        session.NewMod("Brighter Halls", "brighter-halls.pak");
        var info = session.At(ModInfo.Path);
        session.Change(info, "Author", "Vatrix");
        session.Change(info, "Version", "1.0");
        session.Change(info, "Description", "Every hall echoes longer.\nTry the cathedral.");
        session.Change(session.At(GameRoom), "Seconds", 8.0);
        var attic = session.Add("audio/rooms/attic", RecordDraft.From(new RoomSpec(0.75, 0.25, 0.5)));
        session.Save();
        using var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), game, _ => null, scratch.Mods));
        Assert.Equal(info.Build!.Value, data.Load<ModInfo>(ModInfo.Path));
        Assert.Equal(new ModInfo { Name = "Brighter Halls", Author = "Vatrix", Version = "1.0", Description = "Every hall echoes longer.\nTry the cathedral." },
            data.Load<ModInfo>(ModInfo.Path));
        Assert.Equal(session.At(GameRoom).Build!.Value, data.Load<RoomSpec>(GameRoom));
        Assert.Equal(attic.Build!.Value, data.Load<RoomSpec>("audio/rooms/attic"));
        // What the game reads elsewhere is the game's own, as the editor's data as edited has it.
        Assert.Equal(session.EditedData!.Load<RoomSpec>("audio/rooms/cavern"), data.Load<RoomSpec>("audio/rooms/cavern"));
        var music = MusicLibrary.Load(data);
        Assert.Equal(MusicLibrary.Load(session.EditedData!).Themes, music.Themes);
    }
}
