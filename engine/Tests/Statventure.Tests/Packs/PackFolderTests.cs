using System.Text;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Packs.Importing;
using Statventure.Tests.Packs.Importing.Broken;
using Statventure.Tests.Records;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>
/// Packing a folder in the loose form (Docs/Assets.md, "The packer"; S3-1, S3-3): records from their
/// JSON forms, importers chosen by the endings of the files' names, and extracting a pack back.
/// </summary>
public class PackFolderTests
{
    static Creature Ogre => LooseRecordTests.Ogre;

    static Importers TestImporters => Importers.Of(typeof(LinesImporter), typeof(TextImporter), typeof(PairImporter), typeof(ReaderImporter), typeof(FailingImporter), typeof(CountingImporter));

    static PackFile Pack(TempFolder folder, string source, RecordTypes? types = null, Importers? importers = null)
    {
        var writer = new PackWriter();
        writer.AddFolder(folder[source], types ?? TestTypes, importers ?? TestImporters);
        return PackFile.Open(folder.Write(source + ".pak", Bytes(writer)));
    }

    static string[] Kinds(PackFile pack) => [.. pack.Entries.Select(e => $"{e.Path} {(e.IsRecord ? "record" : "file")}")];

    [Fact]
    public void RecordsArePackedFromTheirJsonForms()
    {
        using var folder = new TempFolder();
        folder.Write("src/data/ogre.json", RecordSerializer.ToJson(Ogre));
        var old = RecordFixtures.Read("test.creature", "creature-1", ".json");
        folder.Write("src/data/old.json", old);
        folder.Write("src/data/plain.json", "{\"name\": \"a file\"}");
        folder.Write("src/notes.txt.bin", Prose(100));
        using var pack = Pack(folder, "src");
        Assert.Equal(["data/ogre record", "data/old record", "data/plain.json file", "notes.txt.bin file"], Kinds(pack));
        Assert.Equal(RecordSerializer.ToBinary(Ogre), pack.Read("data/ogre"));
        // An older version is packed in the current versions.
        Assert.Equal(RecordSerializer.ToBinary(RecordSerializer.FromJson<Creature>(old)), pack.Read("data/old"));
        Assert.NotEqual(RecordFixtures.Read("test.creature", "creature-1", ".rec"), pack.Read("data/old"));
        Assert.Equal("{\"name\": \"a file\"}"u8.ToArray(), pack.Read("data/plain.json"));
    }

    [Fact]
    public void ARecordFormThatDoesntPackNamesItsFile()
    {
        using var folder = new TempFolder();
        var unknown = folder.Write("a/unknown.json", "{\"$types\": {\"test.nothing\": 1}}");
        var refused = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["a"], TestTypes, Importers.None));
        Assert.Equal((unknown, "a record of type test.nothing, which isn't a record type known here"), (refused.File, refused.Problem));
        // The engine's types don't include the tests'.
        var ogre = folder.Write("b/ogre.json", RecordSerializer.ToJson(Ogre));
        Assert.Equal("a record of type test.creature, which isn't a record type known here",
            Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["b"])).Problem);
        File.WriteAllText(ogre, RecordSerializer.ToJson(Ogre).Replace("\"title\"", "\"titel\""));
        var unreadable = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["b"], TestTypes, Importers.None));
        Assert.Equal(ogre, unreadable.File);
        Assert.StartsWith("not a readable test.creature record: ", unreadable.Problem);
        Assert.IsType<RecordFormatException>(unreadable.InnerException);
        File.WriteAllText(ogre, "{\"$types\": 3}");
        Assert.StartsWith("not a readable record: ", Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["b"], TestTypes, Importers.None)).Problem);
    }

    [Fact]
    public void ExtractingAndPackingAgainGivesTheSamePack()
    {
        using var folder = new TempFolder();
        var kinds = RecordSerializer.FromJson<Kinds>(Encoding.UTF8.GetString(RecordFixtures.Read("test.kinds", "kinds", ".json")));
        var writer = new PackWriter();
        writer.AddRecord("data/ogre", Ogre);
        writer.AddRecord("data/kinds", kinds);
        writer.AddRecord("data/kinds.json", new Part { Name = "a record named .json" });
        writer.Add("data/plain.json", "[1, 2]"u8);
        writer.Add("text/notes.txt", Prose(2000));
        writer.Add("art/noise.bin", Noise(300));
        writer.Add("empty", []);
        var bytes = Bytes(writer);
        using var pack = PackFile.Open(folder.Write("p.pak", bytes));
        pack.ExtractTo(folder["loose"], TestTypes);
        Assert.Equal(RecordSerializer.ToJson(kinds), File.ReadAllText(folder["loose/data/kinds.json"]));
        Assert.Equal(RecordSerializer.ToJson(new Part { Name = "a record named .json" }), File.ReadAllText(folder["loose/data/kinds.json.json"]));
        var again = new PackWriter();
        again.AddFolder(folder["loose"], TestTypes, Importers.None);
        Assert.Equal(bytes, Bytes(again));
    }

    [Fact]
    public void ExtractingWritesNothingWhenARecordCantBeWritten()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.Add("a/first", [1]);
        writer.AddRecord("b/ogre", Ogre);
        using var pack = PackFile.Open(folder.Write("p.pak", Bytes(writer)));
        // The engine's types don't include the tests'.
        var refused = Assert.Throws<DataFileException>(() => pack.ExtractTo(folder["out"]));
        Assert.Equal((pack.FilePath, "b/ogre", "a record of type test.creature, which isn't a record type known here"), (refused.File, refused.Entry, refused.Problem));
        Assert.False(Directory.Exists(folder["out"]));
        pack.ExtractTo(folder["out"], TestTypes);
        Assert.Equal(["a/first", "b/ogre.json"], Directory.EnumerateFiles(folder["out"], "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(folder["out"], f)).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void FilesGoThroughTheImporterOfTheirEnding()
    {
        using var folder = new TempFolder();
        folder.Write("src/a/horn.lines", "a horn");
        folder.Write("src/b/note.txt", "plain words");
        folder.Write("src/c/twins.pair.txt", "ignored");
        folder.Write("src/c/twins.left", "lef");
        folder.Write("src/c/twins.right", "tie");
        folder.Write("src/d/bytes.bin", Noise(10));
        folder.Write("src/e/ogre.json", RecordSerializer.ToJson(Ogre));
        folder.Write("src/f/.txt", "only the ending");
        using var pack = Pack(folder, "src");
        Assert.Equal([
            "a/horn record", "a/horn.upper file",
            "b/note.seen file",
            "c/twins record",
            "d/bytes.bin file",
            "e/ogre record",
            "f/.txt file",
        ], Kinds(pack));
        using var data = new VirtualFileSystem([Mount.Pack(pack.FilePath)]);
        Assert.Equal(new Part { Name = "a horn" }, data.Load<Part>("a/horn"));
        Assert.Equal("A HORN", data.ReadText("a/horn.upper"));
        Assert.Equal(".txt:plain words", data.ReadText("b/note.seen"));
        // The longer ending wins, and the files it read aren't packed themselves; it sees all 8 sources.
        Assert.Equal(new Part { Name = "leftie", Weight = 8 }, data.Load<Part>("c/twins"));
        Assert.Equal("only the ending", data.ReadText("f/.txt"));
    }

    [Fact]
    public void AnImportersFailureNamesTheSourceFile()
    {
        using var folder = new TempFolder();
        var source = folder.Write("src/bad.fail", "x");
        var refused = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["src"], TestTypes, TestImporters));
        Assert.Equal((source, "the test-failing importer: this source can't be imported"), (refused.File, refused.Problem));
        Assert.IsType<InvalidDataException>(refused.InnerException);

        // An entry an importer makes can't be made again by another file.
        File.Delete(source);
        folder.Write("src/a.lines", "x");
        var clash = folder.Write("src/a.upper", "already here");
        var twice = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["src"], TestTypes, TestImporters));
        Assert.Equal(clash, twice.File);
        Assert.StartsWith("'a.upper' is already in the pack", twice.Problem);
    }

    [Fact]
    public void AnImporterReadsOtherFilesButNotAnotherImportersSources()
    {
        using var folder = new TempFolder();
        folder.Write("src/sum.reads", "data/one\ndata/two\nsum.reads\n");
        folder.Write("src/data/one", Noise(5));
        folder.Write("src/data/two", Noise(7));
        folder.Write("src/data/three", Noise(9));
        using (var pack = Pack(folder, "src"))
        {
            // Its own source too: 5 + 7 + its 28 bytes.
            Assert.Equal(["data/three file", "sum record"], Kinds(pack));
            using var data = new VirtualFileSystem([Mount.Pack(pack.FilePath)]);
            Assert.Equal(40, data.Load<Part>("sum").Weight);
        }

        var source = folder.Write("src/sum.reads", "data/note.txt");
        folder.Write("src/data/note.txt", "taken");
        var taken = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["src"], TestTypes, TestImporters));
        Assert.Equal((source, "the test-reader importer: 'data/note.txt' is a source of the test-text importer, so the test-reader importer can't use it too"), (taken.File, taken.Problem));
        folder.Write("src/sum.reads", "data/four");
        Assert.Equal("the test-reader importer: No 'data/four' in the folder being packed",
            Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["src"], TestTypes, TestImporters)).Problem);
        folder.Write("src/sum.reads", "Data/One");
        Assert.StartsWith("the test-reader importer: 'Data/One' isn't a valid data path",
            Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["src"], TestTypes, TestImporters)).Problem);
    }

    [Fact]
    public void AnImporterIsMadeOncePerFolder()
    {
        using var folder = new TempFolder();
        // Made in a shuffled order, so the folder's listing isn't the path order (tmpfs lists the newest
        // first, ext4 by hash).
        string[] names = ["a", "b-c", "b.c", "b", "b0", "b_c", "c"];
        foreach (var name in (string[])["b0", "a", "c", "b.c", "b_c", "b", "b-c"])
            folder.Write($"src/{name}.count", "");
        var before = CountingImporter.Made;
        using var pack = Pack(folder, "src", importers: Importers.Of(typeof(CountingImporter)));
        Assert.Equal(before + 1, CountingImporter.Made);
        // In ordinal path order.
        Assert.Equal(Enumerable.Range(1, names.Length).Select(i => new[] { (byte)i }), names.Select(n => pack.Read($"{n}.counted")));
    }

    [Fact]
    public void ImportersAreFoundByTheirAttribute()
    {
        var found = Importers.Find([typeof(PackFolderTests).Assembly], t => t.Namespace == typeof(LinesImporter).Namespace);
        Assert.Equal(["test-counting", "test-failing", "test-lines", "test-pair", "test-reader", "test-text"], found.All.Select(i => i.Name).Order(StringComparer.Ordinal));
        var pair = found.All.Single(i => i.Name == "test-pair");
        Assert.Equal(typeof(PairImporter), pair.Type);
        Assert.Equal([".pair.txt", ".pair"], pair.Endings);
        Assert.Equal((pair, ".pair.txt"), found.For("c/x.pair.txt")!.Value);
        Assert.Equal((pair, ".pair"), found.For("x.pair")!.Value);
        Assert.Equal("test-text", found.For("c/x.txt")!.Value.Importer.Name);
        Assert.Equal("test-text", found.For("x.pair.txt.txt")!.Value.Importer.Name);
        Assert.Null(found.For("c/x.pair.txt/y"));
        Assert.Null(found.For(".txt"));
        Assert.Null(found.For("a/.pair"));
        Assert.Null(found.For("x.text"));
        // The longest ending wins whatever order the importers come in.
        Assert.Equal("test-pair", Importers.Of(typeof(PairImporter), typeof(TextImporter)).For("x.pair.txt")!.Value.Importer.Name);
        Assert.Equal("test-pair", Importers.Of(typeof(TextImporter), typeof(PairImporter)).For("x.pair.txt")!.Value.Importer.Name);
        Assert.Empty(Importers.None.All);
        // The engine's own importers are declared rightly (the later steps add them).
        Assert.All(Importers.Engine.All, i => Assert.Equal(typeof(Importers).Assembly, i.Type.Assembly));
        Assert.Equal(Importers.Find(typeof(Importers).Assembly).All.Select(i => i.Type), Importers.Engine.All.Select(i => i.Type));
    }

    public static TheoryData<Type[], string> BadDeclarations => new()
    {
        { [typeof(TextImporter), typeof(ClashImporter)], "both take files ending in .txt" },
        { [typeof(TextImporter), typeof(SameNameImporter)], "are both named test-text" },
        { [typeof(JsonImporter)], "\".json\" would take records' JSON forms" },
        { [typeof(UppercaseImporter)], "\".GLB\" isn't an ending of names under the path rule" },
        { [typeof(NoDotImporter)], "\"glb\" isn't an ending of names under the path rule" },
        { [typeof(DotImporter)], "\".\" isn't an ending of names under the path rule" },
        { [typeof(TrailingDotImporter)], "\".a.\" isn't an ending of names under the path rule" },
        { [typeof(SlashImporter)], "\".a/b\" isn't an ending of names under the path rule" },
        { [typeof(EmptyEndingImporter)], "\"\" isn't an ending of names under the path rule" },
        { [typeof(NoEndingImporter)], "an importer takes files of at least one ending" },
        { [typeof(NamelessImporter)], "an importer needs a name" },
        { [typeof(NotAnImporter)], "an importer is a class implementing IImporter with a public parameterless constructor" },
        { [typeof(ConstructorImporter)], "an importer is a class implementing IImporter with a public parameterless constructor" },
        { [typeof(AbstractImporter)], "an importer is a class implementing IImporter with a public parameterless constructor" },
        { [typeof(UnmarkedImporter)], "isn't an importer (it has no [Importer])" },
    };

    [Theory]
    [MemberData(nameof(BadDeclarations))]
    public void ABadImporterDeclarationIsRefused(Type[] types, string problem)
    {
        var refused = Assert.Throws<InvalidOperationException>(() => Importers.Of(types));
        Assert.Contains(problem, refused.Message);
    }
}
