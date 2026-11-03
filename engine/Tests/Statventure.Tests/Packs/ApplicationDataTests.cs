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
        MakePack(folder["game/packs/models-old.pak"], Entry("models/x", "not this"));
        folder.Write("game/packs/notes.txt", "notes");
        Directory.CreateDirectory(folder["game/packs/ui.pak.d"]);
        var mounts = Application.DataMounts(new ApplicationOptions(), folder["game"], NoVariables);
        Assert.Equal(["Pack packs/models.pak", "Pack packs/audio.pak", "Pack packs/ui.pak", "Pack packs/world.pak", "Pack packs/data.pak"], Describe(mounts, folder["game"]));
        Assert.Equal(["models", "audio", "ui", "world", "data"], Application.AreaPacks);
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
