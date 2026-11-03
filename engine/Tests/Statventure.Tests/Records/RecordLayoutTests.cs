using System.Reflection;
using Statventure.Records;
using Statventure.Tests.Records.Variants;

namespace Statventure.Tests.Records;

/// <summary>
/// The layout guard (Docs/Assets.md, "Guarding layouts"): every record type of the engine and the
/// tests has each version's layout signature recorded in Data/Records/Layouts, a changed layout
/// without a new version fails, every version has a fixture in both forms, and a fixture of the
/// current versions is written back to the same bytes.
/// </summary>
public class RecordLayoutTests
{
    static List<string> Blocks(string text) =>
        [.. text.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries).Select(b => b.TrimEnd('\n'))];

    static int CurrentVersion(Type type) => type.GetCustomAttribute<RecordAttribute>()!.Version;

    [Fact]
    public void EveryVersionsLayoutIsTheRecordedOne()
    {
        var problems = new List<string>();
        foreach (var (name, type) in RecordFixtures.Types)
        {
            var now = RecordSerializer.LayoutText(type);
            var path = Path.Combine(RecordFixtures.LayoutFolder, name + ".txt");
            var recorded = File.Exists(path) ? File.ReadAllText(path) : "";
            if (recorded == now)
                continue;
            List<string> want = Blocks(now), have = Blocks(recorded);
            var changed = Enumerable.Range(0, have.Count).Where(i => i >= want.Count || have[i] != want[i]).Select(i => i + 1).ToList();
            if (changed.Count == 0 && RecordFixtures.Recording)
            {
                // Only versions that have none get their layout recorded; a recorded one never changes.
                Directory.CreateDirectory(RecordFixtures.LayoutFolder);
                File.WriteAllText(path, now);
                continue;
            }
            problems.Add(changed.Count > 0
                ? $"{name}: the layout of version {string.Join(", ", changed)} isn't the recorded one. A changed layout is a new version " +
                  $"(the old type kept as a frozen copy with an upgrade). Now:\n{now}\nRecorded ({path}):\n{recorded}"
                : $"{name}: no recorded layout for version {string.Join(", ", Enumerable.Range(have.Count + 1, want.Count - have.Count))} in {path} " +
                  $"(run the tests with STATVENTURE_RECORD_FIXTURES=1 to record it). Now:\n{now}");
        }
        var stale = Directory.Exists(RecordFixtures.LayoutFolder)
            ? Directory.EnumerateFiles(RecordFixtures.LayoutFolder).Select(Path.GetFileNameWithoutExtension).Where(n => !RecordFixtures.Types.ContainsKey(n!))
            : [];
        problems.AddRange(stale.Select(n => $"{n}: a recorded layout of a record type that no longer exists"));
        Assert.True(problems.Count == 0, string.Join("\n\n", problems));
    }

    [Fact]
    public void EveryVersionOfEveryTypeHasAFixtureInBothForms()
    {
        var read = new Dictionary<string, HashSet<(string, int)>> { [".rec"] = [], [".json"] = [] };
        foreach (var (type, label, form) in RecordFixtures.All())
        {
            Assert.True(read.ContainsKey(form), $"{type}/{label}{form}: fixtures are .rec or .json");
            foreach (var (name, version, used) in RecordFixtures.Load(type, label, form).Table)
                if (used)
                    read[form].Add((name, version));
        }
        var missing = (
            from entry in RecordFixtures.Types
            from version in Enumerable.Range(1, CurrentVersion(entry.Value))
            from form in read.Keys
            where !read[form].Contains((entry.Key, version))
            select $"{entry.Key} version {version} ({form})").ToList();
        Assert.True(missing.Count == 0, "no fixture holds a value of " + string.Join(", ", missing));
    }

    [Fact]
    public void BothFormsOfAFixtureHoldTheSameValue()
    {
        foreach (var fixture in RecordFixtures.All().GroupBy(f => (f.Type, f.Label)))
        {
            Assert.True(fixture.Count() == 2, $"{fixture.Key.Type}/{fixture.Key.Label} needs both forms, .rec and .json");
            var binary = RecordFixtures.Load(fixture.Key.Type, fixture.Key.Label, ".rec");
            var json = RecordFixtures.Load(fixture.Key.Type, fixture.Key.Label, ".json");
            Assert.Equal(binary.Table, json.Table);
            RecordAssert.Equal(binary.Value, json.Value, anyNaN: true);
        }
    }

    [Fact]
    public void FixturesOfTheCurrentVersionsAreWrittenBackToTheSameBytes()
    {
        var current = 0;
        foreach (var (type, label, form) in RecordFixtures.All())
        {
            var (value, table) = RecordFixtures.Load(type, label, form);
            if (table.Any(e => e.Version != CurrentVersion(RecordFixtures.Types[e.Name])))
                continue;
            current++;
            var root = RecordFixtures.Types[type];
            var bytes = form == ".rec" ? RecordSerializer.ToBinary(root, value) : RecordSerializer.ToJsonUtf8(root, value);
            var expected = RecordFixtures.Read(type, label, form);
            Assert.True(expected.AsSpan().SequenceEqual(bytes), $"{type}/{label}{form} isn't written back to the same bytes"
                + (form == ".json" ? $":\n{RecordFixtures.Text(bytes)}" : $": {Convert.ToHexString(bytes)}"));
        }
        Assert.True(current > 0);
    }

    static string Body(Type type) => RecordSerializer.LayoutText(type).Split('\n', 2)[1];

    [Fact]
    public void EachKindOfLayoutChangeChangesTheSignature()
    {
        Assert.Equal("""
            variant.sig 1
              1 a: int32 = 0
              2 b: enum uint8 {pale = 0, dark = 1} = "pale"
              3 c: string? = null
              4 d: float64 = 1

            """.Replace("\r", ""), RecordSerializer.LayoutText(typeof(Sig)));
        Type[] variants =
        [
            typeof(SigRenamed), typeof(SigRenumbered), typeof(SigRetyped), typeof(SigNullable), typeof(SigNotNullable), typeof(SigRequired),
            typeof(SigDefault), typeof(SigList), typeof(SigMemberRenamed), typeof(SigMemberRenumbered), typeof(SigEnumWider),
            typeof(SigMemberAdded), typeof(SigFieldAdded),
        ];
        foreach (var variant in variants)
            Assert.True(Body(typeof(Sig)) != Body(variant), $"{variant.Name} has Sig's signature");
        // The same layout declared another way (a class, its properties in another order) has the same signature.
        Assert.Equal(Body(typeof(Sig)), Body(typeof(SigSame)));
        // A union's tag list: version 2 of test.move added a subtype.
        Assert.Equal("test.move 1 union\n  1 test.walk\n  2 test.leap\n\ntest.move 2 union\n  1 test.walk\n  2 test.leap\n  3 test.fly\n",
            RecordSerializer.LayoutText(typeof(Move)));
        // A nested record by its name only: its own versions don't change the holder's layout.
        Assert.Contains("  2 limbs: list<test.limb> = []\n", RecordSerializer.LayoutText(typeof(Creature)));
    }
}
