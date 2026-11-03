using System.Text;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Records;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>
/// Records in loose folders (Docs/Assets.md, Q7 and S3-1): a <c>.json</c> file that starts as a record's
/// JSON form is the record at its path without <c>.json</c>; any other file is a file; a start that
/// isn't valid JSON fails the mount. A pack and its extracted folder give the same entries.
/// </summary>
public class LooseRecordTests
{
    internal static Creature Ogre => new()
    {
        Name = "ogre",
        Limbs = [new Limb { Kind = LimbKind.Arm, Length = 2.5 }, new Limb { Kind = LimbKind.Leg }],
        Speed = 0.75,
        Title = "the Grim",
        Moves = [new Walk { Pace = 2, Cue = "stomp" }, new Leap { Height = 3, Spin = true }],
    };

    static string Json<T>(T value) where T : notnull => RecordSerializer.ToJson(value);

    static void AssertSameRecord<T>(T expected, T actual) where T : notnull => Assert.Equal(Json(expected), Json(actual));

    [Fact]
    public void APackAndItsExtractedFolderGiveTheSameEntries()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("data/ogre", Ogre);
        writer.AddRecord("data/horn", new Part { Name = "horn", Shade = Shade.Golden });
        writer.AddRecord<Shape>("data/square.json", new Square { Side = 2, Inner = new Circle { Radius = 1 } });
        writer.Add("data/notes.txt", Prose(300));
        writer.Add("data/plain.json", Encoding.UTF8.GetBytes("{\"name\": \"not a record\"}"));
        writer.Add("art/noise.bin", Noise(200));
        writer.Write(folder["p.pak"]);
        using (var pack = PackFile.Open(folder["p.pak"]))
            pack.ExtractTo(folder["loose"], TestTypes);

        // The records are their JSON forms at <path>.json; the files are as they are.
        Assert.Equal(Json(Ogre), File.ReadAllText(folder["loose/data/ogre.json"]));
        Assert.Equal(Json<Shape>(new Square { Side = 2, Inner = new Circle { Radius = 1 } }), File.ReadAllText(folder["loose/data/square.json.json"]));
        Assert.Equal(Prose(300), File.ReadAllBytes(folder["loose/data/notes.txt"]));
        Assert.False(File.Exists(folder["loose/data/ogre"]));

        using var packed = new VirtualFileSystem([Mount.Pack(folder["p.pak"])]);
        using var loose = new VirtualFileSystem([Mount.Folder(folder["loose"])]);
        Assert.Equal(["art/noise.bin", "data/horn", "data/notes.txt", "data/ogre", "data/plain.json", "data/square.json"], loose.List());
        Assert.Equal(packed.List(), loose.List());
        foreach (var path in packed.List())
        {
            Assert.Equal(packed.IsRecord(path), loose.IsRecord(path));
            if (!packed.IsRecord(path))
                Assert.Equal(packed.ReadAllBytes(path), loose.ReadAllBytes(path));
        }
        AssertSameRecord(Ogre, loose.Load<Creature>("data/ogre"));
        AssertSameRecord(packed.Load<Creature>("data/ogre"), loose.Load<Creature>("data/ogre"));
        Assert.Equal(packed.Load<Part>("data/horn"), loose.Load<Part>("data/horn"));
        AssertSameRecord(packed.Load<Shape>("data/square.json"), loose.Load<Shape>("data/square.json"));
        Assert.Equal(folder["loose/data/ogre.json"], loose.Origin("data/ogre"));
        Assert.Equal(folder["loose/data/plain.json"], loose.Origin("data/plain.json"));
    }

    [Fact]
    public void EveryFieldKindLoadsTheSameFromBothForms()
    {
        var kinds = RecordSerializer.FromJson<Kinds>(Encoding.UTF8.GetString(RecordFixtures.Read("test.kinds", "kinds", ".json")));
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("kinds", kinds);
        writer.Write(folder["k.pak"]);
        folder.Write("loose/kinds.json", RecordSerializer.ToJson(kinds));
        using var packed = new VirtualFileSystem([Mount.Pack(folder["k.pak"])]);
        using var loose = new VirtualFileSystem([Mount.Folder(folder["loose"])]);
        Assert.Equal(RecordSerializer.ToBinary(kinds), RecordSerializer.ToBinary(packed.Load<Kinds>("kinds")));
        Assert.Equal(RecordSerializer.ToBinary(kinds), RecordSerializer.ToBinary(loose.Load<Kinds>("kinds")));
    }

    public static TheoryData<string, string, bool> Starts => new()
    {
        { "record", """{"$types": {"test.part": 1}, "name": "x"}""", true },
        { "record after whitespace", " \r\n\t{ \"$types\" : {\"test.part\": 1}, \"name\": \"x\"}", true },
        { "record after a byte order mark", "\uFEFF{\"$types\": {\"test.part\": 1}, \"name\": \"x\"}", true },
        { "record with an escaped key", """{"\u0024types": {"test.part": 1}, "name": "x"}""", true },
        { "record after a long run of spaces", new string(' ', 5000) + """{"$types": {"test.part": 1}, "name": "x"}""", true },
        { "record with an odd table", """{"$types": 7}""", true },
        { "types not first", """{"name": "x", "$types": {"test.part": 1}}""", false },
        { "a near key", """{"$type": "test.part"}""", false },
        { "a longer key", """{"$typesx": {"test.part": 1}}""", false },
        { "a long first key", "{\"" + new string('a', 5000) + "\": 1, \"$types\": {}}", false },
        { "empty object", "{}", false },
        { "array", """[{"$types": {"test.part": 1}}]""", false },
        { "string", "\"$types\"", false },
        { "number", "12", false },
        { "only the start is read", """[1, 2, oops""", false },
        { "a class", """{"name": "Black Knight", "stats": {"str": 3}}""", false },
    };

    [Theory]
    [MemberData(nameof(Starts))]
    public void AJsonFileIsARecordWhenItStartsWithTypes(string name, string text, bool record)
    {
        using var folder = new TempFolder();
        folder.Write("data/x.json", text);
        folder.Write("data/x.txt", text);
        using var data = new VirtualFileSystem([Mount.Folder(folder.Root)]);
        Assert.Equal(record ? ["data/x", "data/x.txt"] : ["data/x.json", "data/x.txt"], data.List());
        Assert.True(record ? data.IsRecord("data/x") : !data.IsRecord("data/x.json"), name);
        Assert.False(data.IsRecord("data/x.txt"), name);
        // The writer takes the same view of a file entry of that name.
        if (record)
            Assert.Throws<ArgumentException>(() => new PackWriter().Add("x.json", Encoding.UTF8.GetBytes(text)));
        else
            new PackWriter().Add("x.json", Encoding.UTF8.GetBytes(text));
    }

    /// <summary>A stream that gives one byte per read, as a file system may give fewer bytes than asked for.</summary>
    sealed class Trickle(byte[] content) : MemoryStream(content)
    {
        public override int Read(byte[] buffer, int offset, int count) => base.Read(buffer, offset, Math.Min(count, 1));
        public override int Read(Span<byte> buffer) => base.Read(buffer[..Math.Min(buffer.Length, 1)]);
    }

    [Theory]
    [MemberData(nameof(Starts))]
    public void AFileGivenByteByByteIsToldTheSame(string name, string text, bool record)
    {
        Assert.True(LooseRecord.StartsAsRecord(new Trickle(Encoding.UTF8.GetBytes(text)), "x.json") == record, name);
        Assert.True(LooseRecord.StartsAsRecord(new MemoryStream(Encoding.UTF8.GetBytes(text)), "x.json") == record, name);
    }

    [Theory]
    [MemberData(nameof(BadStarts))]
    public void AStartThatIsntJsonIsAnErrorByteByByteToo(string name, byte[] content)
    {
        var refused = Assert.Throws<DataFileException>(() => LooseRecord.StartsAsRecord(new Trickle(content), "x.json"));
        Assert.True(refused.Problem.StartsWith("a .json file whose start isn't valid JSON", StringComparison.Ordinal), name);
        Assert.Equal("x.json", refused.File);
    }

    public static TheoryData<string, byte[]> BadStarts => new()
    {
        { "empty", [] },
        { "whitespace", " \n "u8.ToArray() },
        { "a byte order mark alone", [0xEF, 0xBB, 0xBF] },
        { "half a byte order mark", [0xEF, 0xBB] },
        { "an open object", "{"u8.ToArray() },
        { "a cut key", "{\"$ty"u8.ToArray() },
        { "a word", "nope"u8.ToArray() },
        { "single quotes", "{'a': 1}"u8.ToArray() },
        { "a comment", "// note\n{}"u8.ToArray() },
        { "a trailing comma", "{,}"u8.ToArray() },
        { "not UTF-8", [0xFF, 0xFE, 0x7B, 0x7D] },
    };

    [Theory]
    [MemberData(nameof(BadStarts))]
    public void AJsonFileWhoseStartIsntJsonFailsTheMount(string name, byte[] content)
    {
        using var folder = new TempFolder();
        var file = folder.Write("data/bad.json", content);
        folder.Write("data/good.json", "{}");
        var refused = Assert.Throws<DataFileException>(() => new VirtualFileSystem([Mount.Folder(folder.Root)]));
        Assert.Equal(file, refused.File);
        Assert.Null(refused.Entry);
        Assert.True(refused.Problem.StartsWith("a .json file whose start isn't valid JSON, so it can't be told whether it is a record (", StringComparison.Ordinal), $"{name}: {refused.Problem}");
        // Packing the folder is refused the same way, and so is the content as a file entry of a pack.
        Assert.Equal(file, Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder.Root, TestTypes, Importers.None)).File);
        Assert.StartsWith("'bad.json' is a .json file whose start isn't valid JSON", Assert.Throws<ArgumentException>(() => new PackWriter().Add("bad.json", content)).Message);
        // Another name is a file whatever it holds.
        folder.Write("data/bad.json", "{}");
        folder.Write("data/bad.txt", content);
        using var data = new VirtualFileSystem([Mount.Folder(folder.Root)]);
        Assert.Equal(content, data.ReadAllBytes("data/bad.txt"));
    }

    [Fact]
    public void TwoFilesGivingOneEntryFailTheMount()
    {
        using var folder = new TempFolder();
        var record = folder.Write("a.json", Json(Ogre));
        var file = folder.Write("a", "a file");
        var both = Assert.Throws<DataFileException>(() => new VirtualFileSystem([Mount.Folder(folder.Root)]));
        Assert.Equal((record, $"the record 'a', and the file {file} is that entry too"), (both.File, both.Problem));
        File.Delete(file);
        var inner = folder.Write("a/b", "inside");
        var folderToo = Assert.Throws<DataFileException>(() => new VirtualFileSystem([Mount.Folder(folder.Root)]));
        Assert.Equal((record, $"the record 'a', which is also the folder of {inner}"), (folderToo.File, folderToo.Problem));
        // Packing the folder is refused alike.
        Assert.Equal(record, Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder.Root, TestTypes, Importers.None)).File);
    }

    [Theory]
    [InlineData("a..json", "a.")]
    [InlineData("x/.json", "x/")]
    [InlineData(".json", "")]
    public void ARecordWhosePathBreaksTheRuleFailsTheMount(string name, string path)
    {
        using var folder = new TempFolder();
        var file = folder.Write(name, Json(Ogre));
        var refused = Assert.Throws<DataFileException>(() => new VirtualFileSystem([Mount.Folder(folder.Root)]));
        Assert.Equal((file, $"a record's JSON form, but '{path}' isn't a path under the rule"), (refused.File, refused.Problem));
        // The same file holding other JSON is a file.
        folder.Write(name, "[]");
        using var data = new VirtualFileSystem([Mount.Folder(folder.Root)]);
        Assert.Equal([name], data.List());
    }

    [Fact]
    public void ALooseRecordIsReadFromItsJsonForm()
    {
        using var folder = new TempFolder();
        var file = folder.Write("data/ogre.json", Json(Ogre));
        using var data = new VirtualFileSystem([Mount.Folder(folder.Root)]);
        AssertSameRecord(Ogre, data.Load<Creature>("data/ogre"));
        Assert.True(data.IsRecord("data/ogre"));
        var notFile = Assert.Throws<DataFileException>(() => data.ReadAllBytes("data/ogre"));
        Assert.Equal((file, (string?)null, "a record, not a file (records are read with Load)"), (notFile.File, notFile.Entry, notFile.Problem));
        Assert.Throws<FileNotFoundException>(() => data.ReadAllBytes("data/ogre.json"));

        var wrong = Assert.Throws<DataFileException>(() => data.Load<Part>("data/ogre"));
        Assert.Equal((file, (string?)null, "a record of type test.creature, not test.part"), (wrong.File, wrong.Entry, wrong.Problem));

        // An edit shows at once (the contents are read when asked for).
        File.WriteAllText(file, Json(Ogre with { Speed = 9 }));
        Assert.Equal(9, data.Load<Creature>("data/ogre").Speed);
        File.WriteAllText(file, Json(Ogre).Replace("\"speed\"", "\"sped\""));
        var unreadable = Assert.Throws<DataFileException>(() => data.Load<Creature>("data/ogre"));
        Assert.Equal(file, unreadable.File);
        Assert.StartsWith("not a readable test.creature record: ", unreadable.Problem);
        Assert.IsType<RecordFormatException>(unreadable.InnerException);
        // Its type is the first name of "$types" wherever that is in the object.
        File.WriteAllText(file, "{\"name\": \"horn\", \"$types\": {\"test.part\": 1}}");
        Assert.Equal("a record of type test.part, not test.creature", Assert.Throws<DataFileException>(() => data.Load<Creature>("data/ogre")).Problem);
        Assert.Equal("horn", data.Load<Part>("data/ogre").Name);
        File.WriteAllText(file, "{\"name\": {\"$types\": {\"test.creature\": 3}}, \"$types\": {\"test.part\": 1}}");
        Assert.Equal("a record of type test.part, not test.creature", Assert.Throws<DataFileException>(() => data.Load<Creature>("data/ogre")).Problem);
        // One that is no longer a record's form is still taken as one until Reload, which makes it a file.
        File.WriteAllText(file, "{\"speed\": 1}");
        Assert.Equal("not a readable test.creature record: expected an object with \"$types\", an object of record type names and their versions",
            Assert.Throws<DataFileException>(() => data.Load<Creature>("data/ogre")).Problem);
        data.Reload();
        Assert.Equal(["data/ogre.json"], data.List());
        Assert.Equal("{\"speed\": 1}", data.ReadText("data/ogre.json"));
    }

    [Fact]
    public void TheDevelopmentFolderOverridesAPacksRecordWithItsJsonForm()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("data/ogre", Ogre);
        writer.AddRecord("data/imp", new Creature { Name = "imp" });
        writer.Write(folder["base.pak"]);
        folder.Write("dev/data/ogre.json", Json(Ogre with { Title = "the Merry" }));
        // A .json that isn't a record's form is the file data/imp.json, so the pack's record shows.
        folder.Write("dev/data/imp.json", "{\"name\": \"imp\"}");
        using var data = new VirtualFileSystem([Mount.Pack(folder["base.pak"]), Mount.Folder(folder["dev"])]);
        Assert.Equal("the Merry", data.Load<Creature>("data/ogre").Title);
        Assert.Equal(folder["dev/data/ogre.json"], data.Origin("data/ogre"));
        Assert.Equal(folder["base.pak"], data.Origin("data/imp"));
        Assert.Equal("imp", data.Load<Creature>("data/imp").Name);
        Assert.Equal(["data/imp", "data/imp.json", "data/ogre"], data.List("data"));
        // A broken record file in the development folder is an error, never the pack's record.
        File.WriteAllText(folder["dev/data/ogre.json"], Json(Ogre).Replace("\"name\"", "\"nom\""));
        Assert.Equal(folder["dev/data/ogre.json"], Assert.Throws<DataFileException>(() => data.Load<Creature>("data/ogre")).File);
        File.WriteAllText(folder["dev/data/ogre.json"], "{\"$ty");
        Assert.Equal(folder["dev/data/ogre.json"], Assert.Throws<DataFileException>(() => data.Reload()).File);
        Assert.Equal(folder["dev/data/ogre.json"], data.Origin("data/ogre"));
    }

    [Fact]
    public void AClassesFolderMountsItsJsonAsFiles()
    {
        // The old game's class files aren't records' forms: they mount as files under classes/.
        using var folder = new TempFolder();
        folder.Write("classes/black-knight.json", File.ReadAllBytes(Path.Combine(Infrastructure.TestPaths.OldGame, "classes", "Black Knight.json")));
        using var data = new VirtualFileSystem([Mount.Folder(folder["classes"], "classes")]);
        Assert.Equal(["classes/black-knight.json"], data.List());
        Assert.False(data.IsRecord("classes/black-knight.json"));
    }
}
