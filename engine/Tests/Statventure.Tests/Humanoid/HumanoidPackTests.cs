using Statventure.Core;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// models.pak (GM2-P4; Tools/Scripts/make-models-pack.sh): exactly what the packer makes of engine/Assets/Models, byte for
/// byte (so it is reproducible from the committed models), one model record per committed .glb at models/&lt;its path&gt;;
/// and the game finds the family's models in it when its packs are mounted as the game mounts them.
/// </summary>
public class HumanoidPackTests
{
    static string ModelsPak => Path.Combine(TestPaths.Packs, "models.pak");

    [Fact]
    public void ModelsPakIsWhatThePackerMakesOfAssetsModels()
    {
        using var folder = new TempFolder();
        foreach (var file in Directory.GetFiles(HumanoidFiles.Models, "*", SearchOption.AllDirectories))
        {
            var copy = folder[Path.Combine("source", "models", Path.GetRelativePath(HumanoidFiles.Models, file))];
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(file, copy);
        }
        var writer = new PackWriter();
        writer.AddFolder(folder["source"], RecordTypes.Engine, Importers.Engine);
        writer.Write(folder["models.pak"]);
        Assert.True(File.ReadAllBytes(folder["models.pak"]).AsSpan().SequenceEqual(File.ReadAllBytes(ModelsPak)),
            "Assets/Packs/models.pak isn't what the packer makes of Assets/Models: run Tools/Scripts/make-models-pack.sh");
        using var pack = PackFile.Open(ModelsPak);
        Assert.Empty(pack.Verify());
        Assert.Equal(HumanoidFiles.Names().Select(n => "models/humanoid/" + n).Concat(HumanoidFiles.MachineNames().Select(m => "models/humanoid/machines/" + m))
            .Order(StringComparer.Ordinal), pack.Entries.Select(e => e.Path));
        Assert.All(pack.Entries, e => Assert.True(e.IsRecord, e.Path));
        // The family's clip library (GM2 step 4b) and the figures' state machine are in it; the frame's test clip stays out
        // of a pack a player gets (the manager, 2026-11-16).
        Assert.Contains("models/humanoid/clips", pack.Entries.Select(e => e.Path));
        Assert.Contains("models/humanoid/machines/figure", pack.Entries.Select(e => e.Path));
        Assert.DoesNotContain(pack.Entries, e => e.Path.Contains("test-clips", StringComparison.Ordinal));
        // The game ships the full level alone (GM2-P20): the battle's models are the tests' data until the battle comes.
        Assert.DoesNotContain(pack.Entries, e => e.Path.Contains("battle", StringComparison.Ordinal));
    }

    /// <summary>The game's packs mounted as the game mounts them (<c>packs/</c> beside it): each model of the family loads from models.pak as its .glb reads.</summary>
    [Fact]
    public void TheGameLoadsTheFamilysModelsFromModelsPak()
    {
        using var folder = new TempFolder();
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        using var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], _ => null));
        foreach (var name in HumanoidFiles.Names())
        {
            var path = "models/humanoid/" + name;
            Assert.Equal(folder["game/packs/models.pak"], data.Origin(path));
            var packed = ModelData.Load(data, path);
            var file = HumanoidFiles.Load(name);
            Assert.Equal(file.Nodes.Select(n => n.Name), packed.Nodes.Select(n => n.Name));
            Assert.Equal(file.Materials, packed.Materials);
            Assert.Equal(file.Meshes.SelectMany(m => m.Primitives).Sum(p => p.Positions.Length), packed.Meshes.SelectMany(m => m.Primitives).Sum(p => p.Positions.Length));
            Assert.Equal(file.Animations.Select(a => a.Name), packed.Animations.Select(a => a.Name));
        }
    }
}
