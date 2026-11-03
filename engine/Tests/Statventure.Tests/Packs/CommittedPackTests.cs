using Statventure.Core;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Records;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>
/// The packs committed in engine/Assets/Packs/ (<see cref="TestPaths.Packs"/>; Docs/Assets.md, "Packs
/// are the source of truth"; Q1): each is one of the area packs, opens, every entry matches its hash
/// and every record reads with the engine's record types.
/// </summary>
public class CommittedPackTests
{
    /// <summary>What is wrong with a pack: it doesn't open, an entry is damaged, a record doesn't read with <paramref name="types"/>.</summary>
    static List<string> Problems(string file, RecordTypes types)
    {
        PackFile pack;
        try
        {
            pack = PackFile.Open(file);
        }
        catch (DataFileException problem)
        {
            return [problem.Message];
        }
        using (pack)
        {
            var problems = pack.Verify().Select(p => p.Message).ToList();
            foreach (var entry in pack.Entries.Where(e => e.IsRecord))
            {
                try
                {
                    pack.RecordJson(entry, types);
                }
                catch (DataFileException problem)
                {
                    problems.Add(problem.Message);
                }
            }
            return problems;
        }
    }

    [Fact]
    public void EveryCommittedPackIsSound()
    {
        var packs = Directory.Exists(TestPaths.Packs) ? Directory.GetFiles(TestPaths.Packs) : [];
        foreach (var file in packs)
        {
            Assert.Contains(Path.GetFileNameWithoutExtension(file), Application.AreaPacks);
            Assert.Equal(".pak", Path.GetExtension(file));
            Assert.Empty(Problems(file, RecordTypes.Engine));
        }
    }

    [Fact]
    public void TheCheckFindsEachProblem()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("data/part", new Part { Name = "horn" });
        writer.Add("data/file", [1, 2, 3]);
        writer.Write(folder["sound.pak"]);
        Assert.Empty(Problems(folder["sound.pak"], TestTypes));
        Assert.Equal([$"{folder["sound.pak"]}: entry 'data/part': a record of type test.part, which isn't a record type known here"], Problems(folder["sound.pak"], RecordTypes.Engine));

        var damaged = new RawPack().Raw("a", [1]);
        damaged.Entries[0].Hash = new byte[32];
        Assert.Equal([$"{folder["damaged.pak"]}: entry 'a': the content doesn't match its hash (damaged)"], Problems(folder.Write("damaged.pak", damaged.Build()), TestTypes));

        var unreadable = new RawPack().Raw("a", [0]);
        unreadable.Entries[0].Flags = 2;
        Assert.Equal([$"{folder["unreadable.pak"]}: entry 'a': not a readable record: the type table is empty"], Problems(folder.Write("unreadable.pak", unreadable.Build()), TestTypes));

        Assert.Equal([$"{folder["text.pak"]}: 4 bytes is too short for a pack"], Problems(folder.Write("text.pak", "text"), TestTypes));
    }

    [Fact]
    public void TheCommittedPacksAreTheReposAssetsFolder()
    {
        Assert.EndsWith(Path.Combine("engine", "Assets", "Packs"), TestPaths.Packs);
        Assert.True(Path.IsPathFullyQualified(TestPaths.Packs));
        Assert.True(Directory.Exists(Path.Combine(TestPaths.Packs, "..", "..", "Engine", "Statventure")));
    }
}
