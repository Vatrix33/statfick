using Statventure.Core;
using Statventure.Packs;
using Statventure.Tests.Records;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>
/// What <see cref="Application.Data"/> mounts (Docs/Assets.md, "The virtual file system"; Q2, Q3,
/// Q27, S3-4, S3-5): the area packs of packs/ by name, then classes/, then the development folder.
/// </summary>
public class ApplicationDataTests
{
    static readonly Func<string, string?> NoVariables = _ => null;

    static string[] Describe(IEnumerable<Mount> mounts, string root) =>
        [.. mounts.Select(m => $"{m.Kind} {Path.GetRelativePath(root, m.Path).Replace('\\', '/')}{(m.At.Length > 0 ? " at " + m.At : "")}")];

    [Fact]
    public void TheAreaPacksMountByNameInTheirOrder()
    {
        using var folder = new TempFolder();
        foreach (var name in new[] { "world", "data", "ui", "audio", "models" })
            MakePack(folder[$"game/packs/{name}.pak"], Entry($"{name}/x", name));
        // Other files in packs/ aren't the game's packs.
        MakePack(folder["game/packs/extra.pak"], Entry("extra/x", "extra"));
        MakePack(folder["game/packs/models_old.pak"], Entry("models/x", "not this"));
        folder.Write("game/packs/notes.txt", "notes");
        Directory.CreateDirectory(folder["game/packs/ui.pak.d"]);
        var mounts = Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables);
        Assert.Equal(["Pack packs/models.pak", "Pack packs/audio.pak", "Pack packs/ui.pak", "Pack packs/world.pak", "Pack packs/data.pak"], Describe(mounts, folder["game"]));
        Assert.Equal(["models", "audio", "ui", "world", "data"], Application.AreaPacks);
    }

    /// <summary>
    /// The user's Q77 (GM2, 2026-11-22: a pack per race), S3-4 as it reads since: right after <c>models.pak</c> every
    /// <c>models-&lt;name&gt;.pak</c> in <c>packs/</c>, in the ordinal order of the names, then the other area packs; a
    /// name only so (lower case, a name after the dash, the ending <c>.pak</c> exactly), files only. Without
    /// <c>models.pak</c> they come first all the same.
    /// </summary>
    [Fact]
    public void TheModelsPacksMountRightAfterModelsPakInTheirNamesOrder()
    {
        using var folder = new TempFolder();
        foreach (var name in new[] { "data", "audio", "models" })
            MakePack(folder[$"game/packs/{name}.pak"], Entry($"{name}/x", name));
        // Ordinal, not a culture's order: the capital C before every small letter, a dash before a dot.
        foreach (var name in new[] { "models-elf", "models-dark-elf", "models-b", "models-a-b", "models-a", "models-C" })
            MakePack(folder[$"game/packs/{name}.pak"], Entry($"models/{name.ToLowerInvariant()}/x", name));
        // Not models packs: no name after the dash, other cases, other endings, another joint, a folder.
        foreach (var name in new[] { "models-.pak", "Models-y.pak", "models-x.PAK", "models-x.pak.tmp", "modelsx.pak", "models_x.pak", "models-x.pa" })
            MakePack(folder[$"game/packs/{name}"], Entry("models/no/x", name));
        Directory.CreateDirectory(folder["game/packs/models-folder.pak"]);
        string[] expected = ["Pack packs/models.pak", "Pack packs/models-C.pak", "Pack packs/models-a-b.pak", "Pack packs/models-a.pak", "Pack packs/models-b.pak",
            "Pack packs/models-dark-elf.pak", "Pack packs/models-elf.pak", "Pack packs/audio.pak", "Pack packs/data.pak"];
        Assert.Equal(expected, Describe(Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables), folder["game"]));
        Assert.Equal(expected, Describe(Application.DataMounts(new ApplicationOptions { GamePacksOnly = true }, folder["game"], NoVariables), folder["game"]));
        Assert.Equal(expected.Select(e => folder["game/" + e["Pack ".Length..]]), Application.GamePackFiles(folder["game/packs"]));
        File.Delete(folder["game/packs/models.pak"]);
        Assert.Equal(expected[1..], Describe(Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables), folder["game"]));
        Assert.Empty(Application.GamePackFiles(folder["nowhere"]));
        Assert.Equal((true, true, false, false, false), (Application.IsModelsPackName("models-x.pak"), Application.IsModelsPackName("models-a.b.pak"),
            Application.IsModelsPackName("models-.pak"), Application.IsModelsPackName("models.pak"), Application.IsModelsPackName("models-x.Pak")));
    }

    /// <summary>
    /// A models pack is a pack as the others (Q77): its entries read through the data, a mod's entry at its path wins over
    /// it (Q8), and Reload reads its new contents; a models pack put in <c>packs/</c> after the start mounts with the next
    /// start, as an area pack does (which packs mount is decided once, S9-C).
    /// </summary>
    [Fact]
    public void AModelsPackReadsUnderTheModsAndReloadReadsItAgain()
    {
        using var folder = new TempFolder();
        MakePack(folder["game/packs/models.pak"], Entry("models/humanoid/clips", "clips"));
        MakePack(folder["game/packs/models-elf.pak"], Entry("models/humanoid/bodies/elf-male-average", "the elf"), Entry("models/humanoid/parts/x/elf-male-average", "part"));
        MakePack(folder["player/mods/a-mod.pak"], Entry("models/humanoid/bodies/elf-male-average", "the mod's elf"));
        using var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables, folder["player/mods"]));
        Assert.Equal(("the mod's elf", folder["player/mods/a-mod.pak"]), (data.ReadText("models/humanoid/bodies/elf-male-average"), data.Origin("models/humanoid/bodies/elf-male-average")));
        Assert.Equal(("part", folder["game/packs/models-elf.pak"]), (data.ReadText("models/humanoid/parts/x/elf-male-average"), data.Origin("models/humanoid/parts/x/elf-male-average")));
        Assert.Equal("clips", data.ReadText("models/humanoid/clips"));
        using (var shipped = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables)))
            Assert.Equal(("the elf", folder["game/packs/models-elf.pak"]), (shipped.ReadText("models/humanoid/bodies/elf-male-average"), shipped.Origin("models/humanoid/bodies/elf-male-average")));
        // Reload: the models pack made again with another entry; one more models pack put there.
        MakePack(folder["game/packs/models-elf.pak.new"], Entry("models/humanoid/parts/x/elf-male-average", "part 2"), Entry("models/humanoid/parts/y/elf-female-average", "new part"));
        File.Move(folder["game/packs/models-elf.pak.new"], folder["game/packs/models-elf.pak"], overwrite: true);
        MakePack(folder["game/packs/models-goliath.pak"], Entry("models/humanoid/bodies/goliath-male-average", "late"));
        data.Reload();
        Assert.Equal("part 2", data.ReadText("models/humanoid/parts/x/elf-male-average"));
        Assert.Equal(("new part", folder["game/packs/models-elf.pak"]), (data.ReadText("models/humanoid/parts/y/elf-female-average"), data.Origin("models/humanoid/parts/y/elf-female-average")));
        Assert.Equal("the mod's elf", data.ReadText("models/humanoid/bodies/elf-male-average"));
        Assert.False(data.Exists("models/humanoid/bodies/goliath-male-average"));
    }

    [Fact]
    public void AMissingPackIsLeftOut()
    {
        using var folder = new TempFolder();
        MakePack(folder["game/packs/ui.pak"], Entry("ui/x", "ui"));
        MakePack(folder["game/packs/audio.pak"], Entry("audio/x", "audio"));
        // A folder named as a pack isn't one.
        Directory.CreateDirectory(folder["game/packs/models.pak"]);
        Assert.Equal(["Pack packs/audio.pak", "Pack packs/ui.pak"], Describe(Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables), folder["game"]));
        // No packs/ (the engine's tests, the sandbox): no packs; nothing at all here.
        Directory.CreateDirectory(folder["empty"]);
        Assert.Empty(Application.DataMounts(new ApplicationOptions(), folder["empty"], NoVariables));
        Assert.Empty(Application.DataMounts(new ApplicationOptions(), folder["missing"], NoVariables));
    }

    [Fact]
    public void ClassesMountAboveThePacksAndTheDevelopmentFolderOnTop()
    {
        using var folder = new TempFolder();
        MakePack(folder["game/packs/data.pak"], Entry("data/x", "pack"));
        folder.Write("game/classes/black-knight.json", "{\"name\": \"Black Knight\"}");
        folder.Write("dev/classes/black-knight.json", "{\"name\": \"Grey Knight\"}");
        folder.Write("dev/data/x", "development");
        var mounts = Application.DataMounts(new ApplicationOptions { DevelopmentFolder = folder["dev"] }, folder["game"], NoVariables);
        Assert.Equal(["Pack packs/data.pak", "Folder classes at classes", "Folder ../dev"], Describe(mounts, folder["game"]));
        using var data = new VirtualFileSystem(mounts);
        Assert.Equal("{\"name\": \"Grey Knight\"}", data.ReadText("classes/black-knight.json"));
        Assert.Equal("development", data.ReadText("data/x"));
        using var withoutDevelopment = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables));
        Assert.Equal("{\"name\": \"Black Knight\"}", withoutDevelopment.ReadText("classes/black-knight.json"));
        Assert.Equal("pack", withoutDevelopment.ReadText("data/x"));
    }

    [Fact]
    public void TheDevelopmentFolderComesFromTheOptionsElseTheVariable()
    {
        using var folder = new TempFolder();
        Directory.CreateDirectory(folder["option"]);
        Directory.CreateDirectory(folder["variable"]);
        string? Variable(string name) => name == "STATVENTURE_DEV_FOLDER" ? folder["variable"] : null;
        Assert.Equal("STATVENTURE_DEV_FOLDER", Application.DevelopmentFolderVariable);
        var fromOption = Application.DataMounts(new ApplicationOptions { DevelopmentFolder = folder["option"] }, folder["game"], Variable);
        Assert.Equal([(MountKind.Folder, folder["option"], "")], fromOption.Select(m => (m.Kind, m.Path, m.At)));
        var fromVariable = Application.DataMounts(new ApplicationOptions(), folder["game"], Variable);
        Assert.Equal([(MountKind.Folder, folder["variable"], "")], fromVariable.Select(m => (m.Kind, m.Path, m.At)));
        // An empty variable names nothing; another variable isn't looked at.
        Assert.Empty(Application.DataMounts(new ApplicationOptions(), folder["game"], name => name == "STATVENTURE_DEV_FOLDER" ? "" : folder["variable"]));
        // A relative folder is taken from the current folder, once.
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, folder["option"]);
        Assert.Equal(folder["option"], Application.DataMounts(new ApplicationOptions { DevelopmentFolder = relative }, folder["game"], NoVariables).Single().Path);
    }

    [Fact]
    public void AMissingDevelopmentFolderStopsTheStart()
    {
        using var folder = new TempFolder();
        var mounts = Application.DataMounts(new ApplicationOptions { DevelopmentFolder = folder["nowhere"] }, folder["game"], NoVariables);
        var missing = Assert.Throws<DirectoryNotFoundException>(() => new VirtualFileSystem(mounts));
        Assert.Contains(folder["nowhere"], missing.Message);
    }

    [Fact]
    public void TheDevelopmentFolderOverridesAPacksRecord()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("data/part", new Part { Name = "packed" });
        writer.Write(folder["game/packs/data.pak"]);
        folder.Write("dev/data/part.json", Statventure.Records.RecordSerializer.ToJson(new Part { Name = "developed" }));
        using var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions { DevelopmentFolder = folder["dev"] }, folder["game"], NoVariables));
        Assert.Equal("developed", data.Load<Part>("data/part").Name);
        using var shipped = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables));
        Assert.Equal("packed", shipped.Load<Part>("data/part").Name);
    }
}
