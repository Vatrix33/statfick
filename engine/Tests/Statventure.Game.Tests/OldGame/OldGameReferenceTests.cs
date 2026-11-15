using System.Runtime.CompilerServices;

namespace Statventure.Game.Tests;

/// <summary>
/// The committed oracle results (Reference/old-game/) against their tooling and the old game: one file per module of
/// Tools/OldGameOracle/modules, each made from the old game as it is now (the SHA-256 of every old-game file it read).
/// A change to the old game fails here until Tools/Scripts/make-old-game-references.sh is run again, so the port's rules
/// are never checked against an old game that is gone.
/// </summary>
public class OldGameReferenceTests
{
    static string EngineDirectory([CallerFilePath] string path = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", ".."));

    static IEnumerable<string> ModuleNames() =>
        Directory.EnumerateFiles(Path.Combine(EngineDirectory(), "Tools", "OldGameOracle", "modules"), "*.cjs")
            .Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal)!;

    [Fact]
    public void EveryModuleHasItsResultsAndEveryResultsFileItsModule()
    {
        Assert.Equal(ModuleNames(), OldGameReference.Modules());
        foreach (var module in OldGameReference.Modules())
        {
            var reference = OldGameReference.Load(module);
            Assert.Equal(module, reference.Module);
            Assert.NotEmpty(reference.Cases);
            Assert.NotEmpty(reference.Sources);
        }
    }

    [Fact]
    public void TheResultsAreOfTheOldGameAsItIsNow()
    {
        var stale = (
            from module in OldGameReference.Modules()
            from source in OldGameReference.Load(module).Sources
            where OldGameReference.Hash(source.Key) != source.Value
            select $"{module}: old-game/{source.Key}").ToList();
        Assert.True(stale.Count == 0, "the old game changed since these results were made (run Tools/Scripts/make-old-game-references.sh " +
            "and check what moved): " + string.Join(", ", stale));
    }
}
