using System.Text.RegularExpressions;
using Statventure.Core;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// The models packs (GM2-P4; the user's Q77, a pack per race; Tools/Scripts/make-models-pack.sh): models.pak and a
/// models-&lt;race&gt;.pak for each race with models of its own, each exactly what the packer makes of its share of
/// engine/Assets/Models, byte for byte (so it is reproducible from the committed models), one model record per committed
/// .glb at models/&lt;its path&gt;; and the game finds the family's models in them when its packs are mounted as the game
/// mounts them.
/// </summary>
public class HumanoidPackTests
{
    /// <summary>
    /// The pack a file under Assets/Models goes in, by its path there (Q77; make-models-pack.sh's rule, Docs/Assets.md,
    /// "Models and state machines in packs"): a file named <c>&lt;race&gt;-&lt;sex&gt;-&lt;build&gt;.glb</c> (sex male or female,
    /// build average, slender or strong) is its race's, <c>models-&lt;race&gt;</c>, but the human's is <c>models</c>; every
    /// other file (the gear, the clips, the machines) is <c>models</c>.
    /// </summary>
    internal static string PackOf(string path)
    {
        var name = Regex.Match(Path.GetFileName(path), @"^(.+)-(male|female)-(average|slender|strong)\.glb$");
        return name.Success && name.Groups[1].Value != "human" ? Application.ModelsPackPrefix + name.Groups[1].Value : "models";
    }

    /// <summary>The files of Assets/Models by the pack they go in (<see cref="PackOf"/>), each by its path there.</summary>
    static Dictionary<string, List<string>> Shares() =>
        Directory.GetFiles(HumanoidFiles.Models, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(HumanoidFiles.Models, f).Replace('\\', '/'))
            .GroupBy(PackOf).ToDictionary(g => g.Key, g => g.Order(StringComparer.Ordinal).ToList());

    /// <summary>The committed models packs by name (models, models-&lt;race&gt;): what the game mounts of Assets/Packs that holds models.</summary>
    static List<string> CommittedModelsPacks() =>
        [.. Application.GamePackFiles(TestPaths.Packs).Select(Path.GetFileNameWithoutExtension).Where(n => n == "models" || Application.IsModelsPackName(n + ".pak")).Select(n => n!)];

    /// <summary>
    /// Each models pack is what the packer makes of its share of Assets/Models, packed over a copy of the committed pack so
    /// that every entry whose content is the committed one keeps its stored bytes (Assets.md, "Writing": checked by its
    /// hash, decoded) and only a changed or new model is compressed again: the same bytes as a fresh pack's when nothing
    /// changed, in seconds rather than a minute and more for the eight races (the races' packs, GM2 steps 9b–9e). A race's
    /// pack is there exactly when the race has models.
    /// </summary>
    [Fact]
    public void EachModelsPackIsWhatThePackerMakesOfItsShareOfAssetsModels()
    {
        using var folder = new TempFolder();
        var shares = Shares();
        Assert.Equal(shares.Keys.Order(StringComparer.Ordinal), CommittedModelsPacks().Order(StringComparer.Ordinal));
        foreach (var (pack, files) in shares)
        {
            foreach (var file in files)
            {
                var copy = folder[Path.Combine(pack, "models", file)];
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(Path.Combine(HumanoidFiles.Models, file), copy);
            }
            var writer = new PackWriter();
            writer.AddFolder(folder[pack], RecordTypes.Engine, Importers.Engine);
            var committedFile = Path.Combine(TestPaths.Packs, pack + ".pak");
            using var committed = PackFile.Open(committedFile);
            Assert.True(PackTestData.Bytes(writer, committed).AsSpan().SequenceEqual(File.ReadAllBytes(committedFile)),
                $"Assets/Packs/{pack}.pak isn't what the packer makes of its share of Assets/Models: run Tools/Scripts/make-models-pack.sh");
            Assert.Empty(committed.Verify());
            Assert.All(committed.Entries, e => Assert.True(e.IsRecord, e.Path));
        }
        // Every model of the family once, in the pack its name gives.
        var entries = shares.Keys.SelectMany(pack =>
        {
            using var file = PackFile.Open(Path.Combine(TestPaths.Packs, pack + ".pak"));
            return file.Entries.Select(e => (Pack: pack, e.Path)).ToList();
        }).ToList();
        Assert.Equal(HumanoidFiles.Names().Select(n => "models/humanoid/" + n).Concat(HumanoidFiles.MachineNames().Select(m => "models/humanoid/machines/" + m))
            .Order(StringComparer.Ordinal), entries.Select(e => e.Path).Order(StringComparer.Ordinal));
        Assert.All(entries, e => Assert.Equal(PackOf(e.Path + ".glb"), e.Pack));
        // The family's clip library (GM2 step 4b) and the figures' state machine are in models.pak; the frame's test clip
        // stays out of a pack a player gets (the manager, 2026-11-16).
        Assert.Contains(("models", "models/humanoid/clips"), entries);
        Assert.Contains(("models", "models/humanoid/machines/figure"), entries);
        Assert.DoesNotContain(entries, e => e.Path.Contains("test-clips", StringComparison.Ordinal));
        // The game ships the full level alone (GM2-P20): the battle's models are the tests' data until the battle comes.
        Assert.DoesNotContain(entries, e => e.Path.Contains("battle", StringComparison.Ordinal));
    }

    /// <summary>
    /// The rule that puts a model in its pack (Q77): by the file's name, a race's own models in its pack and the human's,
    /// and all that isn't a race's body or part, in models.pak.
    /// </summary>
    [Fact]
    public void AModelsPackIsTheRaceTheFilesNameBeginsWith()
    {
        Assert.Equal("models", PackOf("humanoid/bodies/human-male-average.glb"));
        Assert.Equal("models", PackOf("humanoid/parts/marks/freckles/human-female-3/human-female-average.glb"));
        Assert.Equal("models-elf", PackOf("humanoid/bodies/elf-female-strong.glb"));
        Assert.Equal("models-dark-elf", PackOf("humanoid/parts/hair/long/dark-elf-male-average.glb"));
        Assert.Equal("models-half-beast", PackOf("humanoid/parts/tail/fox/half-beast-female-slender.glb"));
        Assert.Equal("models-goliath", PackOf("humanoid/gear/chain-shirt/goliath-male-average.glb"));
        Assert.Equal("models", PackOf("humanoid/clips.glb"));
        Assert.Equal("models", PackOf("humanoid/gear/sword.glb"));
        Assert.Equal("models", PackOf("humanoid/machines/figure.json"));
        Assert.Equal("models", PackOf("humanoid/bodies/elf-male-tall.glb"));
        Assert.Equal("models", PackOf("humanoid/bodies/elf-male-average.json"));
    }

    /// <summary>The game's packs mounted as the game mounts them (<c>packs/</c> beside it): each model of the family loads from its models pack as its .glb reads.</summary>
    [Fact]
    public void TheGameLoadsTheFamilysModelsFromTheirPacks()
    {
        using var folder = new TempFolder();
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        using var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], _ => null));
        foreach (var name in HumanoidFiles.Names())
        {
            var path = "models/humanoid/" + name;
            Assert.Equal(folder[$"game/packs/{PackOf(name + ".glb")}.pak"], data.Origin(path));
            var packed = ModelData.Load(data, path);
            var file = HumanoidFiles.Load(name);
            Assert.Equal(file.Nodes.Select(n => n.Name), packed.Nodes.Select(n => n.Name));
            Assert.Equal(file.Materials, packed.Materials);
            Assert.Equal(file.Meshes.SelectMany(m => m.Primitives).Sum(p => p.Positions.Length), packed.Meshes.SelectMany(m => m.Primitives).Sum(p => p.Positions.Length));
            Assert.Equal(file.Animations.Select(a => a.Name), packed.Animations.Select(a => a.Name));
        }
    }

    /// <summary>
    /// The mechanism on the tests' own small models (Q77): a game whose packs/ holds models.pak and a race's models pack
    /// made by the packer from one models folder by <see cref="PackOf"/>: every model loads from the pack its name gives, as
    /// its .glb reads, and a mod's model at a race's model's path is the one read.
    /// </summary>
    [Fact]
    public void ARacesModelsPackServesItsModelsAndAModsModelWinsOverIt()
    {
        using var folder = new TempFolder();
        var sample = HumanoidFiles.Glb("sample");
        var clips = HumanoidFiles.TestClips;
        (string Path, string File)[] models = [("humanoid/bodies/human-male-average.glb", sample), ("humanoid/bodies/test-male-average.glb", sample),
            ("humanoid/parts/hair/x/test-female-average.glb", sample), ("humanoid/clips.glb", clips)];
        foreach (var group in models.GroupBy(m => PackOf(m.Path)))
        {
            foreach (var (path, file) in group)
            {
                var copy = folder[Path.Combine("source", group.Key, "models", path)];
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Copy(file, copy);
            }
            var writer = new PackWriter();
            writer.AddFolder(folder[Path.Combine("source", group.Key)], RecordTypes.Engine, Importers.Engine);
            writer.Write(folder[$"game/packs/{group.Key}.pak"]);
        }
        Assert.Equal([folder["game/packs/models.pak"], folder["game/packs/models-test.pak"]], Application.GamePackFiles(folder["game/packs"]));
        var mod = new PackWriter();
        Directory.CreateDirectory(folder["mod/models/humanoid/bodies"]);
        File.Copy(clips, folder["mod/models/humanoid/bodies/test-male-average.glb"]);
        mod.AddFolder(folder["mod"], RecordTypes.Engine, Importers.Engine);
        mod.Write(folder["player/mods/a-mod.pak"]);
        using var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], _ => null, folder["player/mods"]));
        foreach (var (path, file) in models)
        {
            var entry = "models/" + path[..^".glb".Length];
            var expected = entry == "models/humanoid/bodies/test-male-average" ? folder["player/mods/a-mod.pak"] : folder[$"game/packs/{PackOf(path)}.pak"];
            Assert.Equal(expected, data.Origin(entry));
            var read = ModelData.Load(data, entry);
            var glb = GltfImport.Load(expected.EndsWith("a-mod.pak", StringComparison.Ordinal) ? clips : file);
            Assert.Equal(glb.Nodes.Select(n => n.Name), read.Nodes.Select(n => n.Name));
            Assert.Equal(glb.Animations.Select(a => a.Name), read.Animations.Select(a => a.Name));
        }
    }
}
