using System.Reflection;
using Statventure.Game.Data.Saves;
using Statventure.Records;

namespace Statventure.Game.Tests;

/// <summary>
/// The layout guard for the game data library's records (Docs/Assets.md, "Guarding layouts"; as the engine's
/// <c>RecordLayoutTests</c>): every record type of the library has each version's layout signature recorded in
/// Data/Records/Layouts, a changed layout without a new version fails, every version has a fixture in both forms, and a
/// fixture of the current versions is written back to the same bytes. GM1 has one such type, the save (<c>save.game</c>
/// version 1, GM1-P5); each later milestone's part comes as a new version of it with a fixture of each past one.
/// </summary>
public class LibraryRecordLayoutTests
{
    static List<string> Blocks(string text) =>
        [.. text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries).Select(b => b.TrimEnd('\n'))];

    static int CurrentVersion(Type type) => type.GetCustomAttribute<RecordAttribute>()!.Version;

    [Fact]
    public void TheGuardCoversTheSaveAndNotTheStandIn()
    {
        Assert.Equal(["save.game"], LibraryRecords.Types.Keys);
        Assert.Equal(typeof(SaveGame), LibraryRecords.Types["save.game"]);
    }

    [Fact]
    public void EveryVersionsLayoutIsTheRecordedOne()
    {
        var problems = new List<string>();
        foreach (var (name, type) in LibraryRecords.Types)
        {
            var now = RecordSerializer.LayoutText(type);
            var path = Path.Combine(LibraryRecords.LayoutFolder, name + ".txt");
            var recorded = File.Exists(path) ? File.ReadAllText(path) : "";
            if (recorded == now)
                continue;
            List<string> want = Blocks(now), have = Blocks(recorded);
            var changed = Enumerable.Range(0, have.Count).Where(i => i >= want.Count || have[i] != want[i]).Select(i => i + 1).ToList();
            if (changed.Count == 0 && LibraryRecords.Recording)
            {
                // Only versions that have none get their layout recorded; a recorded one never changes.
                Directory.CreateDirectory(LibraryRecords.LayoutFolder);
                File.WriteAllText(path, now);
                continue;
            }
            problems.Add(changed.Count > 0
                ? $"{name}: the layout of version {string.Join(", ", changed)} isn't the recorded one. A changed layout is a new version " +
                  $"(the old type kept as a frozen copy with an upgrade). Now:\n{now}\nRecorded ({path}):\n{recorded}"
                : $"{name}: no recorded layout for version {string.Join(", ", Enumerable.Range(have.Count + 1, want.Count - have.Count))} in {path} " +
                  $"(run the tests with STATVENTURE_RECORD_FIXTURES=1 to record it). Now:\n{now}");
        }
        var stale = Directory.Exists(LibraryRecords.LayoutFolder)
            ? Directory.EnumerateFiles(LibraryRecords.LayoutFolder).Select(Path.GetFileNameWithoutExtension).Where(n => !LibraryRecords.Types.ContainsKey(n!))
            : [];
        problems.AddRange(stale.Select(n => $"{n}: a recorded layout of a record type that no longer exists"));
        Assert.True(problems.Count == 0, string.Join("\n\n", problems));
    }

    [Fact]
    public void TheSavesVersion1LayoutIsItsName()
    {
        Assert.Equal("save.game 1\n  1 name: string = \"\"\n", RecordSerializer.LayoutText(typeof(SaveGame)));
        Assert.Equal(RecordSerializer.LayoutText(typeof(SaveGame)), File.ReadAllText(Path.Combine(LibraryRecords.LayoutFolder, "save.game.txt")));
    }

    [Fact]
    public void EveryVersionOfEveryTypeHasAFixtureInBothForms()
    {
        // The save's version-1 fixture (the design's example character file).
        LibraryRecords.Ensure("ser-alrik", new SaveGame { Name = "Ser Alrik" });
        var read = new Dictionary<string, HashSet<(string, int)>> { [".rec"] = [], [".json"] = [] };
        foreach (var (type, label, form) in LibraryRecords.All())
        {
            Assert.True(read.ContainsKey(form), $"{type}/{label}{form}: fixtures are .rec or .json");
            Assert.True(LibraryRecords.Types.ContainsKey(type), $"{type}/{label}{form}: a fixture of a type the library doesn't have");
            var table = LibraryRecords.Table(type, label, form);
            Assert.Equal(type, table[0].Name);
            foreach (var entry in table)
                read[form].Add(entry);
            LibraryRecords.Load(type, label, form);
        }
        var missing = (
            from entry in LibraryRecords.Types
            from version in Enumerable.Range(1, CurrentVersion(entry.Value))
            from form in read.Keys
            where !read[form].Contains((entry.Key, version))
            select $"{entry.Key} version {version} ({form})").ToList();
        Assert.True(missing.Count == 0, "no fixture holds a value of " + string.Join(", ", missing));
    }

    [Fact]
    public void BothFormsOfAFixtureHoldTheSameValue()
    {
        foreach (var fixture in LibraryRecords.All().GroupBy(f => (f.Type, f.Label)))
        {
            Assert.True(fixture.Count() == 2, $"{fixture.Key.Type}/{fixture.Key.Label} needs both forms, .rec and .json");
            Assert.Equal(LibraryRecords.Table(fixture.Key.Type, fixture.Key.Label, ".rec"), LibraryRecords.Table(fixture.Key.Type, fixture.Key.Label, ".json"));
            var type = LibraryRecords.Types[fixture.Key.Type];
            Assert.Equal(LibraryRecords.Json(type, LibraryRecords.Load(fixture.Key.Type, fixture.Key.Label, ".rec")),
                LibraryRecords.Json(type, LibraryRecords.Load(fixture.Key.Type, fixture.Key.Label, ".json")));
        }
    }

    [Fact]
    public void FixturesOfTheCurrentVersionsAreWrittenBackToTheSameBytes()
    {
        var current = 0;
        foreach (var (type, label, form) in LibraryRecords.All())
        {
            if (LibraryRecords.Table(type, label, form).Any(e => LibraryRecords.Types.TryGetValue(e.Name, out var t) && e.Version != CurrentVersion(t)))
                continue;
            current++;
            var root = LibraryRecords.Types[type];
            var value = LibraryRecords.Load(type, label, form);
            var bytes = form == ".rec" ? RecordSerializer.ToBinary(root, value) : RecordSerializer.ToJsonUtf8(root, value);
            Assert.True(LibraryRecords.Read(type, label, form).AsSpan().SequenceEqual(bytes), $"{type}/{label}{form} isn't written back to the same bytes");
        }
        Assert.True(current > 0);
    }

    [Fact]
    public void TheSavesFixtureIsTheDesignsCharacterFile()
    {
        // Docs/Game/Foundation.md, "The character file": GM1's file is { "$types": { "save.game": 1 }, "name": "Ser Alrik" }.
        Assert.Equal("{\n  \"$types\": {\n    \"save.game\": 1\n  },\n  \"name\": \"Ser Alrik\"\n}\n",
            System.Text.Encoding.UTF8.GetString(LibraryRecords.Read("save.game", "ser-alrik", ".json")));
        Assert.Equal(new SaveGame { Name = "Ser Alrik" }, LibraryRecords.Load("save.game", "ser-alrik", ".rec"));
        Assert.Equal(new SaveGame { Name = "Ser Alrik" }, LibraryRecords.Load("save.game", "ser-alrik", ".json"));
    }
}
