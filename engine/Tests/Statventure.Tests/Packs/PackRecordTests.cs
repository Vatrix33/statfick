using System.Buffers.Binary;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Records;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>Record entries in packs (Docs/Assets.md, step 3): the record flag, Load from a pack, the errors it gives.</summary>
public class PackRecordTests
{
    static Creature Ogre => new()
    {
        Name = "ogre",
        Limbs = [new Limb { Kind = LimbKind.Arm, Length = 2.5 }, new Limb { Kind = LimbKind.Leg }],
        Speed = 0.75,
        Title = "the Grim",
        Moves = [new Walk { Pace = 2, Cue = "stomp" }, new Leap { Height = 3, Spin = true }],
    };

    static Creature Imp => new() { Name = "imp", Speed = 4 };

    /// <summary>A pack of these record and file entries, made with the engine's writer.</summary>
    static string MakeRecordPack(string file, IEnumerable<(string Path, object Value)> records, params (string Path, byte[] Content)[] files)
    {
        var writer = new PackWriter();
        foreach (var (path, value) in records)
            writer.AddRecord(path, value.GetType(), value);
        foreach (var (path, content) in files)
            writer.Add(path, content);
        writer.Write(file);
        return file;
    }

    static void AssertSame(Creature expected, Creature actual) => Assert.Equal(RecordSerializer.ToJson(expected), RecordSerializer.ToJson(actual));

    [Fact]
    public void ARecordEntryIsItsBinaryFormFlaggedAsARecord()
    {
        var binary = RecordSerializer.ToBinary(Ogre);
        var notes = Prose(500);
        var writer = new PackWriter();
        writer.AddRecord("data/ogre", Ogre);
        writer.Add("data/notes", notes);
        var bytes = Bytes(writer);

        // The same pack made field by field: the record's binary form, stored as any entry is, flag bit 1 set.
        var raw = new RawPack().Brotli("data/notes", notes);
        if (RawPack.Compress(binary).Length < binary.Length)
            raw.Brotli("data/ogre", binary);
        else
            raw.Raw("data/ogre", binary);
        raw.Entries[1].Flags |= 2;
        Assert.Equal(raw.Build(), bytes);

        using var folder = new TempFolder();
        using var pack = PackFile.Open(folder.Write("records.pak", bytes));
        var record = pack.Find("data/ogre")!;
        Assert.True(record.IsRecord);
        Assert.Equal(PackEntryFlags.Record, record.Flags & ~PackEntryFlags.Compressed);
        Assert.False(pack.Find("data/notes")!.IsRecord);
        Assert.Equal(PackEntryFlags.Compressed, pack.Find("data/notes")!.Flags);
        Assert.Equal(binary, pack.Read(record));
        // The flags field of the record's index entry (after the notes' entry).
        var index = bytes.AsSpan((int)BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(16)));
        var second = 62 + "data/notes".Length;
        Assert.Equal((uint)record.Flags, BinaryPrimitives.ReadUInt32LittleEndian(index[(second + 2 + "data/ogre".Length + 24)..]));
        Assert.Equal(2u, BinaryPrimitives.ReadUInt32LittleEndian(index[(second + 2 + "data/ogre".Length + 24)..]) & 2);
    }

    [Fact]
    public void ACompressedRecordAndARawOneBothRead()
    {
        // A record Brotli shrinks and one it doesn't: flags 3 and 2.
        var big = new Creature { Name = string.Concat(Enumerable.Repeat("ogre ", 200)) };
        var small = new Empty();
        using var folder = new TempFolder();
        using var data = new VirtualFileSystem([Mount.Pack(MakeRecordPack(folder["r.pak"], [("big", big), ("small", small)]))]);
        using var pack = PackFile.Open(folder["r.pak"]);
        Assert.Equal(PackEntryFlags.Compressed | PackEntryFlags.Record, pack.Find("big")!.Flags);
        Assert.Equal(PackEntryFlags.Record, pack.Find("small")!.Flags);
        AssertSame(big, data.Load<Creature>("big"));
        Assert.Equal(new Empty(), data.Load<Empty>("small"));
    }

    [Fact]
    public void ARecordLoadsFromAPack()
    {
        using var folder = new TempFolder();
        using var data = new VirtualFileSystem([Mount.Pack(MakeRecordPack(folder["r.pak"], [("data/ogre", Ogre), ("data/part", new Part { Name = "horn", Shade = Shade.Golden })]))]);
        AssertSame(Ogre, data.Load<Creature>("data/ogre"));
        AssertSame(Ogre, (Creature)data.Load(typeof(Creature), "data/ogre"));
        Assert.Equal(new Part { Name = "horn", Shade = Shade.Golden }, data.Load<Part>("data/part"));
        Assert.True(data.IsRecord("data/ogre"));
        Assert.True(data.Exists("data/ogre"));
        Assert.Equal(["data/ogre", "data/part"], data.List("data"));
        Assert.Equal(folder["r.pak"], data.Origin("data/ogre"));
        // Read afresh every time: two loads are two values.
        Assert.NotSame(data.Load<Creature>("data/ogre"), data.Load<Creature>("data/ogre"));
    }

    [Fact]
    public void TheTopmostMountGivesTheRecord()
    {
        using var folder = new TempFolder();
        using var data = new VirtualFileSystem([
            Mount.Pack(MakeRecordPack(folder["base.pak"], [("data/ogre", Ogre), ("data/imp", Imp)])),
            Mount.Pack(MakeRecordPack(folder["mod.pak"], [("data/imp", Imp with { Speed = 9 })])),
        ]);
        AssertSame(Ogre, data.Load<Creature>("data/ogre"));
        AssertSame(Imp with { Speed = 9 }, data.Load<Creature>("data/imp"));
    }

    [Fact]
    public void AnOldVersionInAPackIsUpgraded()
    {
        var fixture = RecordFixtures.Read("test.creature", "creature-1", ".rec");
        var raw = new RawPack().Raw("old/creature", fixture);
        raw.Entries[0].Flags = 2;
        using var folder = new TempFolder();
        using var data = new VirtualFileSystem([Mount.Pack(folder.Write("old.pak", raw.Build()))]);
        var loaded = data.Load<Creature>("old/creature");
        AssertSame(RecordSerializer.FromBinary<Creature>(fixture), loaded);
        // Version 1 (legs as a count) upgraded: the current form is another.
        Assert.NotEqual(fixture, RecordSerializer.ToBinary(loaded));
        Assert.Contains(loaded.Limbs, limb => limb.Kind == LimbKind.Leg);
    }

    [Fact]
    public void AFileIsNotARecordAndARecordIsNotAFile()
    {
        using var folder = new TempFolder();
        var file = MakeRecordPack(folder["r.pak"], [("data/ogre", Ogre)], Entry("data/text", "words"));
        using var data = new VirtualFileSystem([Mount.Pack(file)]);
        Assert.False(data.IsRecord("data/text"));
        var notRecord = Assert.Throws<DataFileException>(() => data.Load<Creature>("data/text"));
        Assert.Equal((file, "data/text", "a file, not a record (expected a test.creature)"), (notRecord.File, notRecord.Entry, notRecord.Problem));
        foreach (var read in new Action[] { () => data.ReadAllBytes("data/ogre"), () => data.Open("data/ogre") })
        {
            var notFile = Assert.Throws<DataFileException>(read);
            Assert.Equal((file, "data/ogre", "a record, not a file (records are read with Load)"), (notFile.File, notFile.Entry, notFile.Problem));
        }
        Assert.Equal("words", data.ReadText("data/text"));
        using var pack = PackFile.Open(file);
        Assert.Throws<ArgumentException>(() => pack.RecordJson(pack.Find("data/text")!, PackTestData.TestTypes));
        Assert.Equal(RecordSerializer.ToJson(Ogre), System.Text.Encoding.UTF8.GetString(pack.RecordJson(pack.Find("data/ogre")!, PackTestData.TestTypes)));
    }

    [Fact]
    public void ARecordOfAnotherTypeIsAnError()
    {
        using var folder = new TempFolder();
        var file = MakeRecordPack(folder["r.pak"], [("data/ogre", Ogre)]);
        using var data = new VirtualFileSystem([Mount.Pack(file)]);
        var wrong = Assert.Throws<DataFileException>(() => data.Load<Part>("data/ogre"));
        Assert.Equal((file, "data/ogre", "a record of type test.creature, not test.part"), (wrong.File, wrong.Entry, wrong.Problem));
        // A record type a creature holds is still another type.
        Assert.Equal("a record of type test.creature, not test.limb", Assert.Throws<DataFileException>(() => data.Load<Limb>("data/ogre")).Problem);
    }

    [Fact]
    public void ARecordThatDoesntReadIsAnErrorNamingTheEntry()
    {
        // The pack is sound (the hash matches), the record in it isn't.
        var binary = RecordSerializer.ToBinary(Ogre);
        var cases = new (string Name, byte[] Content, string Problem)[]
        {
            ("truncated", binary[..^1], "not a readable test.creature record: "),
            ("trailing", [.. binary, 0], "not a readable test.creature record: 1 bytes follow the record"),
            ("empty", [], "not a readable test.creature record: "),
            ("no table", [0], "not a readable test.creature record: the type table is empty"),
        };
        using var folder = new TempFolder();
        foreach (var (name, content, problem) in cases)
        {
            var raw = new RawPack().Raw("data/bad", content);
            raw.Entries[0].Flags = 2;
            var file = folder.Write(name + ".pak", raw.Build());
            using var data = new VirtualFileSystem([Mount.Pack(file)]);
            var refused = Assert.Throws<DataFileException>(() => data.Load<Creature>("data/bad"));
            Assert.Equal((file, "data/bad"), (refused.File, refused.Entry));
            Assert.True(refused.Problem.StartsWith(problem, StringComparison.Ordinal), $"{name}: {refused.Problem}");
            Assert.IsType<RecordFormatException>(refused.InnerException);
        }
    }

    [Fact]
    public void ADamagedRecordEntryIsAnErrorEvenWithAGoodRecordBelow()
    {
        using var folder = new TempFolder();
        var good = MakeRecordPack(folder["base.pak"], [("data/ogre", Ogre)]);
        var raw = new RawPack().Raw("data/ogre", RecordSerializer.ToBinary(Imp));
        raw.Entries[0].Flags = 2;
        raw.Entries[0].Hash = new byte[32];
        // The index hash covers the damaged entry hash, so the pack still opens: only the read finds it.
        var bad = folder.Write("mod.pak", raw.Build());
        using var data = new VirtualFileSystem([Mount.Pack(good), Mount.Pack(bad)]);
        var refused = Assert.Throws<DataFileException>(() => data.Load<Creature>("data/ogre"));
        Assert.Equal((bad, "data/ogre", "the content doesn't match its hash (damaged)"), (refused.File, refused.Entry, refused.Problem));
        // A file above a record hides it: no falling back to the record below either.
        var file = MakePack(folder["file.pak"], Entry("data/ogre", "not a record"));
        using var hidden = new VirtualFileSystem([Mount.Pack(good), Mount.Pack(file)]);
        var notRecord = Assert.Throws<DataFileException>(() => hidden.Load<Creature>("data/ogre"));
        Assert.Equal((file, "data/ogre"), (notRecord.File, notRecord.Entry));
    }

    [Fact]
    public void LoadChecksItsArguments()
    {
        using var folder = new TempFolder();
        using var data = new VirtualFileSystem([Mount.Pack(MakeRecordPack(folder["r.pak"], [("data/ogre", Ogre)]))]);
        Assert.Throws<FileNotFoundException>(() => data.Load<Creature>("data/troll"));
        Assert.Throws<FileNotFoundException>(() => data.IsRecord("data/troll"));
        Assert.Throws<ArgumentException>(() => data.Load<Creature>("Data/Ogre"));
        Assert.Throws<ArgumentException>(() => data.IsRecord("data/"));
        Assert.Contains("isn't a record type", Assert.Throws<InvalidOperationException>(() => data.Load<string>("data/ogre")).Message);
    }

    [Fact]
    public void ARecordAndItsLooseFileCantShareAPack()
    {
        var both = new PackWriter();
        both.AddRecord("a", Imp);
        Assert.Equal("'a.json' can't be a file beside the record 'a': a loose folder holds the record as this file (Parameter 'path')",
            Assert.Throws<ArgumentException>(() => both.Add("a.json", "[1]"u8)).Message);
        var reversed = new PackWriter();
        reversed.Add("a.json", "[1]"u8);
        Assert.Equal("'a' can't be a record beside the file 'a.json': a loose folder holds the record as that file (Parameter 'path')",
            Assert.Throws<ArgumentException>(() => reversed.AddRecord("a", Imp)).Message);
        Assert.Equal(1, both.Count);
        Assert.Equal(1, reversed.Count);

        // Two records, or two files, of those names are fine: their loose files differ.
        var records = new PackWriter();
        records.AddRecord("a", Imp);
        records.AddRecord("a.json", Imp);
        records.AddRecord("b.json", Imp);
        records.AddRecord("b", Imp);
        var files = new PackWriter();
        files.Add("a", [1]);
        files.Add("a.json", "[2]"u8);
        files.Add("b.json", "[2]"u8);
        files.Add("b", [1]);
        using var folder = new TempFolder();
        using var recordPack = PackFile.Open(folder.Write("records.pak", Bytes(records)));
        using var filePack = PackFile.Open(folder.Write("files.pak", Bytes(files)));
        Assert.Equal(4, recordPack.Entries.Count(e => e.IsRecord));
        Assert.Equal(0, filePack.Entries.Count(e => e.IsRecord));
    }

    [Fact]
    public void ARecordsPathIsCheckedBeforeItIsSerialized()
    {
        var writer = new PackWriter();
        Assert.Throws<ArgumentException>(() => writer.AddRecord("A", Imp));
        Assert.Throws<ArgumentException>(() => writer.AddRecord("A", new Creature { Name = null! }));
        Assert.Equal(0, writer.Count);
        // A value its declaration doesn't allow is the serializer's error, and nothing is added.
        Assert.Throws<RecordFormatException>(() => writer.AddRecord("a", new Creature { Name = null! }));
        Assert.Equal(0, writer.Count);
    }

    [Fact]
    public void RepackingKeepsStoredBytesButTakesTheKindFromTheNewEntry()
    {
        var binary = RecordSerializer.ToBinary(new Creature { Name = string.Concat(Enumerable.Repeat("ogre ", 200)) });
        using var folder = new TempFolder();
        // A file whose content is a record's binary form, then the same content as a record elsewhere.
        var old = new PackWriter();
        old.Add("copy", binary);
        old.Write(folder["p.pak"]);
        var asRecord = new PackWriter();
        asRecord.AddRecord("data/big", RecordSerializer.FromBinary<Creature>(binary));
        var result = asRecord.Write(folder["p.pak"]);
        Assert.Equal(1, result.Reused);
        using (var pack = PackFile.Open(folder["p.pak"]))
            Assert.Equal(PackEntryFlags.Compressed | PackEntryFlags.Record, pack.Find("data/big")!.Flags);
        // And back: a record's stored bytes kept for a file.
        var asFile = new PackWriter();
        asFile.Add("copy", binary);
        Assert.Equal(1, asFile.Write(folder["p.pak"]).Reused);
        using (var pack = PackFile.Open(folder["p.pak"]))
            Assert.Equal(PackEntryFlags.Compressed, pack.Find("copy")!.Flags);
    }
}
