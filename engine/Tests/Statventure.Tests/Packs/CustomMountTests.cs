using System.Text;
using Statventure.Audio;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Records;
using Statventure.Tests.Render3D;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>Entries held in memory for a custom mount, as the editor's session holds its unsaved work.</summary>
sealed class MemoryEntries : IMountEntries
{
    public Dictionary<string, (bool Record, byte[] Content)> Items { get; } = new(StringComparer.Ordinal);

    /// <summary>When set, the paths given instead of the items' (to give one twice, or one outside the rule).</summary>
    public List<string>? GivenPaths { get; set; }

    public int Reads { get; private set; }

    public MemoryEntries File(string path, byte[] content)
    {
        Items[path] = (false, content);
        return this;
    }

    public MemoryEntries File(string path, string text) => File(path, Encoding.UTF8.GetBytes(text));

    public MemoryEntries Record(string path, byte[] binary)
    {
        Items[path] = (true, binary);
        return this;
    }

    public MemoryEntries Record<T>(string path, T value) where T : notnull => Record(path, RecordSerializer.ToBinary(value));

    public IEnumerable<string> Paths => GivenPaths ?? (IEnumerable<string>)Items.Keys;

    public bool IsRecord(string path) => Items[path].Record;

    public byte[] Read(string path)
    {
        Reads++;
        return Items[path].Content.ToArray();
    }
}

/// <summary>
/// The custom mount (Docs/Editor.md, "Packs for the editor", P38): entries an object gives, read as a pack's
/// are, below and above packs and folders, named in errors by the mount's name and the entry, its paths
/// taken when mounted and on Reload and checked as a pack's index is; the engine's loaders and cross-entry
/// checks read it unchanged.
/// </summary>
public class CustomMountTests
{
    const string Name = "/mods/unsaved.pak";

    static Creature Ogre => LooseRecordTests.Ogre;

    /// <summary>Entries of every kind a pack holds, and records that don't read as asked.</summary>
    static (string Path, bool Record, byte[] Content)[] Sample() =>
    [
        ("data/ogre", true, RecordSerializer.ToBinary(Ogre)),
        ("data/old", true, RecordFixtures.Read("test.creature", "creature-1", ".rec")),
        ("data/part", true, RecordSerializer.ToBinary(new Part { Name = "horn", Shade = Shade.Golden })),
        ("data/broken", true, RecordSerializer.ToBinary(Ogre)[..^1]),
        ("data/notes.txt", false, Prose(200)),
        ("data/plain.json", false, "{\"a\": 1}"u8.ToArray()),
        ("data/deeper/noise", false, Noise(40)),
        ("empty", false, []),
        ("top", true, RecordSerializer.ToBinary(new Empty())),
    ];

    static string Outcome(Func<object?> read, string file)
    {
        try
        {
            return read() switch
            {
                byte[] bytes => "bytes " + Convert.ToHexStringLower(bytes),
                Stream stream => "stream " + Convert.ToHexStringLower(((MemoryStream)stream).ToArray()) + (stream.CanWrite ? " writable" : ""),
                Creature creature => "creature " + RecordSerializer.ToJson(creature),
                Part part => "part " + RecordSerializer.ToJson(part),
                IReadOnlyList<string> paths => "paths " + string.Join(",", paths),
                DataFileException problem => $"problem {(problem.File == file ? "<file>" : problem.File)} {problem.Entry} {problem.Problem}",
                var other => other?.ToString() ?? "null",
            };
        }
        catch (DataFileException problem)
        {
            return $"error {(problem.File == file ? "<file>" : problem.File)} {problem.Entry} {problem.Problem} {problem.InnerException?.GetType().Name}";
        }
        catch (Exception problem)
        {
            return $"error {problem.GetType().Name}";
        }
    }

    /// <summary>Every read of every path, as text, with the pack's file or the mount's name as "&lt;file&gt;".</summary>
    static List<string> Reads(VirtualFileSystem data, string file, IEnumerable<string> paths)
    {
        var outcomes = new List<string>
        {
            Outcome(() => data.List(), file),
            Outcome(() => data.List("data"), file),
            Outcome(() => data.List("data/deeper"), file),
        };
        foreach (var path in paths.Append("missing"))
        {
            outcomes.Add(path + ": exists " + Outcome(() => data.Exists(path), file));
            outcomes.Add(path + ": record " + Outcome(() => data.IsRecord(path), file));
            outcomes.Add(path + ": bytes " + Outcome(() => data.ReadAllBytes(path), file));
            outcomes.Add(path + ": open " + Outcome(() => data.Open(path), file));
            outcomes.Add(path + ": creature " + Outcome(() => data.Load<Creature>(path), file));
            outcomes.Add(path + ": part " + Outcome(() => data.Load(typeof(Part), path), file));
            outcomes.Add(path + ": origin " + Outcome(() => data.Origin(path) == file ? "<file>" : data.Origin(path), file));
            outcomes.Add(path + ": problem " + Outcome(() => data.Problem(path, "a rule broken"), file));
        }
        return outcomes;
    }

    [Fact]
    public void ACustomMountReadsAsAPackReads()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        var entries = new MemoryEntries();
        foreach (var (path, record, content) in Sample())
        {
            if (record)
            {
                writer.AddRecordBinary(path, content);
                entries.Record(path, content);
            }
            else
            {
                writer.Add(path, content);
                entries.File(path, content);
            }
        }
        var file = folder.Write("same.pak", Bytes(writer));
        using var packed = new VirtualFileSystem([Mount.Pack(file)]);
        using var custom = new VirtualFileSystem([Mount.Custom(Name, entries)]);
        var paths = Sample().Select(e => e.Path).ToArray();
        var fromPack = Reads(packed, file, paths);
        var fromCustom = Reads(custom, Name, paths);
        Assert.Equal(fromPack, fromCustom);
        // What the comparison covers: a record of each kind and version, records that don't read, files.
        Assert.Contains("data/ogre: creature creature " + RecordSerializer.ToJson(Ogre), fromCustom);
        Assert.Contains("data/old: creature creature " + RecordSerializer.ToJson(RecordSerializer.FromBinary<Creature>(RecordFixtures.Read("test.creature", "creature-1", ".rec"))), fromCustom);
        Assert.Contains("data/ogre: part error <file> data/ogre a record of type test.creature, not test.part ", fromCustom);
        Assert.Contains("data/notes.txt: creature error <file> data/notes.txt a file, not a record (expected a test.creature) ", fromCustom);
        Assert.Contains("data/ogre: bytes error <file> data/ogre a record, not a file (records are read with Load) ", fromCustom);
        Assert.Single(fromCustom, o => o.StartsWith("data/broken: creature error <file> data/broken not a readable test.creature record: ", StringComparison.Ordinal)
            && o.EndsWith(" RecordFormatException", StringComparison.Ordinal));
        Assert.Contains("data/plain.json: bytes bytes " + Convert.ToHexStringLower("{\"a\": 1}"u8), fromCustom);
        Assert.Contains("top: problem problem <file> top a rule broken", fromCustom);
        Assert.Contains("missing: bytes error FileNotFoundException", fromCustom);
        Assert.Equal(MountKind.Custom, custom.Mounts[0].Kind);
        Assert.Equal((Name, "", entries), (custom.Mounts[0].Path, custom.Mounts[0].At, custom.Mounts[0].Entries));
        Assert.Equal($"Custom {Name}", custom.Mounts[0].ToString());
        Assert.Null(packed.Mounts[0].Entries);
    }

    [Fact]
    public void ACustomMountSitsBelowAndAbovePacksAndFolders()
    {
        using var folder = new TempFolder();
        var pack = MakePack(folder["base.pak"], Entry("a", "pack"), Entry("b", "pack"), Entry("c", "pack"));
        folder.Write("loose/c", "folder");
        folder.Write("loose/d", "folder");
        var entries = new MemoryEntries().File("b", "custom").File("c", "custom").File("d", "custom").File("e", "custom");
        string[] paths = ["a", "b", "c", "d", "e"];
        using (var data = new VirtualFileSystem([Mount.Pack(pack), Mount.Custom(Name, entries), Mount.Folder(folder["loose"])]))
        {
            Assert.Equal(["pack", "custom", "folder", "folder", "custom"], paths.Select(data.ReadText));
            Assert.Equal([pack, Name, folder["loose/c"], folder["loose/d"], Name], paths.Select(data.Origin));
            Assert.Equal(paths, data.List());
        }
        using (var data = new VirtualFileSystem([Mount.Folder(folder["loose"]), Mount.Custom(Name, entries), Mount.Pack(pack)]))
        {
            Assert.Equal(["pack", "pack", "pack", "custom", "custom"], paths.Select(data.ReadText));
            Assert.Equal([pack, pack, pack, Name, Name], paths.Select(data.Origin));
        }
        // No falling back below it: a record there hides a file below, and one that doesn't read is its own error.
        entries.Record("a", RecordSerializer.ToBinary(Ogre)[..^1]);
        using (var data = new VirtualFileSystem([Mount.Pack(pack), Mount.Custom(Name, entries)]))
        {
            var problem = Assert.Throws<DataFileException>(() => data.ReadAllBytes("a"));
            Assert.Equal((Name, "a", "a record, not a file (records are read with Load)"), (problem.File, problem.Entry, problem.Problem));
            var unreadable = Assert.Throws<DataFileException>(() => data.Load<Creature>("a"));
            Assert.Equal((Name, "a"), (unreadable.File, unreadable.Entry));
            Assert.StartsWith("not a readable test.creature record: ", unreadable.Problem);
        }
        // Two custom mounts: the upper one wins.
        var upper = new MemoryEntries().File("b", "upper");
        using (var data = new VirtualFileSystem([Mount.Custom(Name, entries), Mount.Custom("upper", upper)]))
            Assert.Equal(("upper", "upper", "custom"), (data.ReadText("b"), data.Origin("b"), data.ReadText("c")));
    }

    [Fact]
    public void TheEnginesLoadersReadItAndNameItInTheirErrors()
    {
        // A loader's own check (a terrain whose samples don't fit its grid) names the mount and the entry.
        var terrain = TerrainSamples.Small();
        var entries = new MemoryEntries()
            .Record("world/terrain/good", terrain)
            .Record("world/terrain/bad", new TerrainData
            {
                Width = terrain.Width, Depth = terrain.Depth, CellSize = terrain.CellSize, HeightMin = terrain.HeightMin, HeightMax = terrain.HeightMax,
                HeightSamples = terrain.HeightSamples[..^1], Covers = terrain.Covers, CoverSlots = terrain.CoverSlots,
            });
        using (var data = new VirtualFileSystem([Mount.Custom(Name, entries)]))
        {
            RecordAssert.Equal(terrain, TerrainData.Load(data, "world/terrain/good"));
            var problem = Assert.Throws<DataFileException>(() => TerrainData.Load(data, "world/terrain/bad"));
            Assert.Equal((Name, "world/terrain/bad", "not a valid terrain: 5 height samples for 3x2 points"), (problem.File, problem.Entry, problem.Problem));
        }

        // A cross-entry check over the game's data: the music library with a theme of the committed audio.pak
        // edited in the mount above it.
        var audio = Path.Combine(TestPaths.Packs, "audio.pak");
        ThemeSpec theme;
        using (var game = new VirtualFileSystem([Mount.Pack(audio)]))
            theme = game.Load<ThemeSpec>("audio/themes/alder-vale");
        var edited = new MemoryEntries().Record("audio/themes/alder-vale", theme with { Tempo = theme.Tempo + 7 });
        using (var data = new VirtualFileSystem([Mount.Pack(audio), Mount.Custom(Name, edited)]))
        {
            var library = MusicLibrary.Load(data);
            Assert.Equal(theme.Tempo + 7, library.Theme("alder-vale")!.Tempo);
            Assert.Equal(55, library.Themes.Count);
        }
        edited.Record("audio/themes/alder-vale", theme with { Id = "elder-vale" });
        using (var data = new VirtualFileSystem([Mount.Pack(audio), Mount.Custom(Name, edited)]))
        {
            var problem = Assert.Throws<DataFileException>(() => MusicLibrary.Load(data));
            Assert.Equal((Name, "audio/themes/alder-vale"), (problem.File, problem.Entry));
            Assert.StartsWith("the theme's id is 'elder-vale', but its path holds 'alder-vale'", problem.Problem);
        }
    }

    [Fact]
    public void ItsPathsAreTakenWhenMountedAndOnReloadItsBytesWhenRead()
    {
        var entries = new MemoryEntries().File("a", "one").Record("r", new Part { Name = "horn" });
        using var data = new VirtualFileSystem([Mount.Custom(Name, entries)]);
        Assert.Equal(0, entries.Reads);
        // A change of content shows at once.
        entries.File("a", "two");
        Assert.Equal("two", data.ReadText("a"));
        Assert.Equal(1, entries.Reads);
        // A new path, or a file become a record, only after a Reload.
        entries.File("b", "new");
        entries.Record("a", RecordSerializer.ToBinary(new Part { Name = "now a record" }));
        Assert.False(data.Exists("b"));
        Assert.False(data.IsRecord("a"));
        var reloaded = 0;
        data.Reloaded += () => reloaded++;
        data.Reload();
        Assert.Equal(1, reloaded);
        Assert.Equal("new", data.ReadText("b"));
        Assert.Equal("now a record", data.Load<Part>("a").Name);
        // A path it no longer has, before a Reload, is its own error; after it, gone.
        entries.Items.Remove("b");
        Assert.Throws<KeyNotFoundException>(() => data.ReadAllBytes("b"));
        data.Reload();
        Assert.False(data.Exists("b"));
        Assert.Equal(["a", "r"], data.List());
    }

    public static TheoryData<string[], string, string> BadEntries => new()
    {
        { ["data/Ogre"], "data/Ogre", "a path outside the rule: 'O' isn't allowed (only a–z, 0–9, '-', '_', '.' and '/')" },
        { ["a", "data//x"], "data//x", "a path outside the rule: a name is empty (a leading, trailing or doubled '/')" },
        { ["a", "b", "a"], "a", "a second entry with this path" },
        { ["a/b/c", "a/b"], "a/b", "an entry and also the folder of 'a/b/c'" },
        // Two, given out of order: the first in path order is named, as a pack's index is read.
        { ["z/x", "z", "a", "a/x"], "a", "an entry and also the folder of 'a/x'" },
        { ["x", "x.json"], "x", "a record beside the file 'x.json', which is its loose form" },
    };

    [Theory]
    [MemberData(nameof(BadEntries))]
    public void ItsEntriesAreCheckedAsAPacksIndexIs(string[] paths, string entry, string problem)
    {
        // Records end in nothing, files in ".json" (so "x" is a record beside the file "x.json").
        var entries = new MemoryEntries { GivenPaths = [.. paths] };
        foreach (var path in paths.Distinct())
        {
            if (path.EndsWith(".json", StringComparison.Ordinal))
                entries.File(path, "[1]");
            else
                entries.Record(path, RecordSerializer.ToBinary(new Empty()));
        }
        var refused = Assert.Throws<DataFileException>(() => new VirtualFileSystem([Mount.Custom(Name, entries)]));
        Assert.Equal((Name, entry, problem), (refused.File, refused.Entry, refused.Problem));

        // The same in a pack's index is refused with the same words.
        var raw = new RawPack();
        foreach (var path in paths.Order(StringComparer.Ordinal))
        {
            raw.Raw(path, []);
            raw.Entries[^1].Flags = path.EndsWith(".json", StringComparison.Ordinal) ? 0u : 2u;
        }
        using var folder = new TempFolder();
        var file = folder.Write("p.pak", raw.Build());
        Assert.Equal(problem, Assert.Throws<DataFileException>(() => PackFile.Open(file)).Problem);
    }

    [Fact]
    public void ARecordBesideARecordOrAFileBesideAFileIsFine()
    {
        var entries = new MemoryEntries()
            .Record("x", RecordSerializer.ToBinary(new Empty())).Record("x.json", RecordSerializer.ToBinary(new Empty()))
            .File("y", "[1]").File("y.json", "[2]")
            .File("z.json", "[3]").Record("w.json", RecordSerializer.ToBinary(new Empty()));
        using var data = new VirtualFileSystem([Mount.Custom(Name, entries)]);
        Assert.Equal(["w.json", "x", "x.json", "y", "y.json", "z.json"], data.List());
    }

    [Fact]
    public void AReloadThatFailsLeavesTheMountAsItWas()
    {
        var entries = new MemoryEntries().File("a", "one");
        using var data = new VirtualFileSystem([Mount.Custom(Name, entries)]);
        entries.File("a/b", "a folder of the entry a");
        var refused = Assert.Throws<DataFileException>(data.Reload);
        Assert.Equal((Name, "a"), (refused.File, refused.Entry));
        Assert.Equal(["a"], data.List());
        Assert.Equal("one", data.ReadText("a"));
        entries.Items.Remove("a/b");
        data.Reload();
        Assert.Equal(["a"], data.List());
    }

    [Fact]
    public void ACustomMountNeedsANameAndEntries()
    {
        Assert.Throws<ArgumentException>(() => Mount.Custom("", new MemoryEntries()));
        Assert.Throws<ArgumentNullException>(() => Mount.Custom(null!, new MemoryEntries()));
        Assert.Throws<ArgumentNullException>(() => Mount.Custom(Name, null!));
        using var data = new VirtualFileSystem([Mount.Custom(Name, new MemoryEntries())]);
        Assert.Empty(data.List());
    }
}
