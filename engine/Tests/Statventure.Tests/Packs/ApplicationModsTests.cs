using Statventure.Core;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Records;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>
/// The game's data as shipped (Docs/Assets.md, step 9; Q2, Q8, Q30, S3-4, S9-A): the packs and classes/ beside
/// the executable, the mods in the player's game folder above them, the development folder on top; Reload
/// picking up a rebuilt pack and mods added or taken away.
/// </summary>
public class ApplicationModsTests
{
    static readonly Func<string, string?> NoVariables = _ => null;

    static string[] Describe(IEnumerable<Mount> mounts, string root) =>
        [.. mounts.Select(m => $"{m.Kind} {Path.GetRelativePath(root, m.Path).Replace('\\', '/')}{(m.At.Length > 0 ? " at " + m.At : "")}")];

    /// <summary>A game folder laid out as the build makes it, a player's game folder with mods, and a development folder.</summary>
    static List<Mount> Shipped(TempFolder folder, bool development = true) =>
        Application.DataMounts(new ApplicationOptions { DevelopmentFolder = development ? folder["dev"] : null }, folder["game"], NoVariables, folder["player/mods"]);

    [Fact]
    public void TheModsMountBetweenTheClassesAndTheDevelopmentFolder()
    {
        using var folder = new TempFolder();
        MakePack(folder["game/packs/data.pak"], Entry("data/x", "pack"));
        MakePack(folder["game/packs/audio.pak"], Entry("audio/x", "pack"));
        folder.Write("game/classes/black-knight.json", "{}");
        Directory.CreateDirectory(folder["dev"]);
        Assert.Equal(["Pack packs/audio.pak", "Pack packs/data.pak", "Folder classes at classes", "Mods ../player/mods", "Folder ../dev"],
            Describe(Shipped(folder), folder["game"]));
        // Without a development folder the mods are on top; without a game folder's mods, no mods mount.
        Assert.Equal(["Pack packs/audio.pak", "Pack packs/data.pak", "Folder classes at classes", "Mods ../player/mods"],
            Describe(Shipped(folder, development: false), folder["game"]));
        Assert.DoesNotContain(Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables), m => m.Kind == MountKind.Mods);
        // The mods folder mounts even when it isn't there (no mods until one is put there and the data reloaded), as a full path.
        Assert.False(Directory.Exists(folder["player/mods"]));
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, folder["player/mods"]);
        var mods = Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables, relative).Single(m => m.Kind == MountKind.Mods);
        Assert.Equal(folder["player/mods"], mods.Path);
        using var data = new VirtualFileSystem(Shipped(folder));
        Assert.Equal(["audio/x", "classes/black-knight.json", "data/x"], data.List());
    }

    [Fact]
    public void ModsOverrideThePacksAndTheDevelopmentFolderOverridesThem()
    {
        using var folder = new TempFolder();
        var packed = new PackWriter();
        packed.Add("audio/a", "pack"u8.ToArray());
        packed.Add("audio/b", "pack"u8.ToArray());
        packed.Add("data/c", "pack"u8.ToArray());
        packed.AddRecord("data/part", new Part { Name = "packed" });
        packed.Write(folder["game/packs/data.pak"]);
        folder.Write("game/classes/black-knight.json", "{\"name\": \"shipped\"}");
        // Two mods in the ordinal order of their names (Q30): the later one wins.
        var first = new PackWriter();
        first.Add("audio/a", "first mod"u8.ToArray());
        first.Add("audio/b", "first mod"u8.ToArray());
        first.Add("classes/black-knight.json", "{\"name\": \"first mod\"}"u8.ToArray());
        first.AddRecord("data/part", new Part { Name = "modded" });
        first.Write(folder["player/mods/1-first.pak"]);
        MakePack(folder["player/mods/2-second.pak"], Entry("audio/b", "second mod"), Entry("audio/new", "second mod"));
        // Not mods: another ending, a pack in a folder below.
        MakePack(folder["player/mods/3-third.pak.off"], Entry("audio/b", "not a mod"));
        MakePack(folder["player/mods/later/4-fourth.pak"], Entry("audio/b", "not a mod"));
        folder.Write("dev/audio/a", "development");
        folder.Write("dev/data/part.json", RecordSerializer.ToJson(new Part { Name = "developed" }));

        using var data = new VirtualFileSystem(Shipped(folder));
        Assert.Equal("development", data.ReadText("audio/a"));
        Assert.Equal("second mod", data.ReadText("audio/b"));
        Assert.Equal("pack", data.ReadText("data/c"));
        Assert.Equal("second mod", data.ReadText("audio/new"));
        Assert.Equal("{\"name\": \"first mod\"}", data.ReadText("classes/black-knight.json"));
        Assert.Equal("developed", data.Load<Part>("data/part").Name);
        Assert.Equal(folder["player/mods/2-second.pak"], data.Origin("audio/b"));
        Assert.Equal(folder["player/mods/1-first.pak"], data.Origin("classes/black-knight.json"));
        Assert.Equal(["audio/a", "audio/b", "audio/new"], data.List("audio"));

        using var withoutDevelopment = new VirtualFileSystem(Shipped(folder, development: false));
        Assert.Equal("first mod", withoutDevelopment.ReadText("audio/a"));
        Assert.Equal("modded", withoutDevelopment.Load<Part>("data/part").Name);
    }

    [Fact]
    public void ReloadPicksUpARebuiltGamePack()
    {
        using var folder = new TempFolder();
        MakePack(folder["game/packs/audio.pak"], Entry("audio/a", "old"), Entry("audio/b", "old"), Entry("audio/gone", "old"));
        MakePack(folder["player/mods/mod.pak"], Entry("audio/b", "mod"));
        folder.Write("dev/audio/c", "development");
        using var data = new VirtualFileSystem(Shipped(folder));
        // Rebuilt as the build copies a pack: beside it, then renamed over it.
        MakePack(folder["game/packs/audio.pak.copy"], Entry("audio/a", "new"), Entry("audio/b", "new"), Entry("audio/c", "new"), Entry("audio/added", "new"));
        File.Move(folder["game/packs/audio.pak.copy"], folder["game/packs/audio.pak"], overwrite: true);
        // Until the data is reloaded, the pack that was opened.
        Assert.Equal("old", data.ReadText("audio/a"));
        Assert.True(data.Exists("audio/gone"));
        data.Reload();
        Assert.Equal("new", data.ReadText("audio/a"));
        Assert.Equal("new", data.ReadText("audio/added"));
        Assert.False(data.Exists("audio/gone"));
        // The mod and the development folder stay above it.
        Assert.Equal("mod", data.ReadText("audio/b"));
        Assert.Equal("development", data.ReadText("audio/c"));
    }

    [Fact]
    public void ReloadPicksUpModsAddedAndTakenAway()
    {
        using var folder = new TempFolder();
        MakePack(folder["game/packs/audio.pak"], Entry("audio/a", "pack"), Entry("audio/b", "pack"));
        folder.Write("dev/audio/b", "development");
        using var data = new VirtualFileSystem(Shipped(folder));
        Assert.Equal("pack", data.ReadText("audio/a"));
        // The first mod, in a mods folder made after the start.
        MakePack(folder["player/mods/b.pak"], Entry("audio/a", "mod b"), Entry("audio/b", "mod b"), Entry("audio/new", "mod b"));
        Assert.Equal("pack", data.ReadText("audio/a"));
        Assert.False(data.Exists("audio/new"));
        data.Reload();
        Assert.Equal("mod b", data.ReadText("audio/a"));
        Assert.Equal("mod b", data.ReadText("audio/new"));
        Assert.Equal("development", data.ReadText("audio/b"));
        // A second one named after it wins; one named before it doesn't.
        MakePack(folder["player/mods/c.pak"], Entry("audio/a", "mod c"));
        MakePack(folder["player/mods/a.pak"], Entry("audio/a", "mod a"), Entry("audio/new", "mod a"));
        data.Reload();
        Assert.Equal("mod c", data.ReadText("audio/a"));
        Assert.Equal("mod b", data.ReadText("audio/new"));
        // Taken away again.
        File.Delete(folder["player/mods/c.pak"]);
        File.Delete(folder["player/mods/b.pak"]);
        data.Reload();
        Assert.Equal("mod a", data.ReadText("audio/a"));
        Assert.Equal("mod a", data.ReadText("audio/new"));
        Directory.Delete(folder["player/mods"], recursive: true);
        data.Reload();
        Assert.Equal("pack", data.ReadText("audio/a"));
        Assert.False(data.Exists("audio/new"));
    }
}
