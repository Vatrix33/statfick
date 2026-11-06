using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Records;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>
/// Rewriting a pack for the editor (Docs/Editor.md, "Packs for the editor", step 2; M11-16, M11-17, P23, P24):
/// a writer filled from an open pack keeps every entry as it was stored (its stored bytes, whether they are
/// compressed, whether it is a record, a record's binary form in its own version), with entries changed,
/// added, renamed and removed; a subset written elsewhere (Save As); the index's hash an open pack exposes.
/// </summary>
public class PackRewriteTests
{
    static Creature Imp => new() { Name = "imp", Speed = 4 };

    /// <summary>A creature whose binary form Brotli shrinks.</summary>
    static Creature Big => new() { Name = string.Concat(Enumerable.Repeat("ogre ", 200)), Title = "the Long" };

    /// <summary>
    /// A pack made by hand, each entry stored as the engine's writer wouldn't store it now, so only keeping
    /// the stored bytes gives them back: an older record version stored raw, text compressed at lower
    /// Brotli qualities, text Brotli would shrink stored raw, a record flagged entry that isn't any record
    /// the engine knows, noise.
    /// </summary>
    static byte[] Original()
    {
        var raw = new RawPack();
        raw.Raw("audio/old", RecordFixtures.Read("test.creature", "creature-1", ".rec"));
        raw.Entries[^1].Flags = 2;
        raw.Brotli("audio/quality5", Prose(3000, 1), quality: 5);
        raw.Raw("audio/raw-prose", Prose(2000, 2));
        raw.Raw("data/changed", Prose(100, 3));
        raw.Brotli("data/ogre", RecordSerializer.ToBinary(Big), quality: 3);
        raw.Entries[^1].Flags |= 2;
        raw.Raw("data/removed", Noise(50, 4));
        raw.Brotli("data/renamed", RecordSerializer.ToBinary(Big with { Title = "the Moved" }), quality: 4);
        raw.Entries[^1].Flags |= 2;
        raw.Raw("data/unknown", "a record of a kind no engine knows"u8.ToArray());
        raw.Entries[^1].Flags = 2;
        raw.Raw("ui/noise", Noise(300, 5));
        return raw.Build();
    }

    /// <summary>What each entry of a pack file holds: its flags, its content's hash and its stored bytes as they are in the file.</summary>
    static Dictionary<string, (PackEntryFlags Flags, string Hash, string Stored)> Stored(string file)
    {
        var bytes = File.ReadAllBytes(file);
        using var pack = PackFile.Open(file);
        return pack.Entries.ToDictionary(e => e.Path,
            e => (e.Flags, e.HashHex, Convert.ToHexStringLower(bytes.AsSpan((int)e.Offset, (int)e.StoredLength))), StringComparer.Ordinal);
    }

    [Fact]
    public void TheHandMadeEntriesAreStoredAsTheWriterWouldntStoreThem()
    {
        // So the tests below see kept bytes, not bytes the writer happens to make again.
        using var folder = new TempFolder();
        var file = folder.Write("original.pak", Original());
        using var pack = PackFile.Open(file);
        var again = new PackWriter();
        foreach (var entry in pack.Entries)
        {
            if (entry.IsRecord)
                again.AddRecordBinary(entry.Path, pack.Read(entry));
            else
                again.Add(entry.Path, pack.Read(entry));
        }
        var made = Stored(folder.Write("made.pak", Bytes(again)));
        var original = Stored(file);
        foreach (var path in new[] { "audio/quality5", "audio/raw-prose", "data/ogre", "data/renamed" })
            Assert.NotEqual(original[path].Stored, made[path].Stored);
        Assert.Equal(PackEntryFlags.Compressed, made["audio/raw-prose"].Flags);
        Assert.Equal(PackEntryFlags.None, original["audio/raw-prose"].Flags);
        // The old record is version 1; adding its value would write version 3.
        Assert.NotEqual(RecordFixtures.Read("test.creature", "creature-1", ".rec"),
            RecordSerializer.ToBinary(RecordSerializer.FromBinary<Creature>(RecordFixtures.Read("test.creature", "creature-1", ".rec"))));
    }

    [Fact]
    public void ARewrittenPackKeepsEveryOtherEntryAsItWasStored()
    {
        using var folder = new TempFolder();
        var file = folder.Write("mod.pak", Original());
        var before = Stored(file);
        var hashBefore = "";
        PackWriteResult result;
        using (var pack = PackFile.Open(file))
        {
            hashBefore = pack.IndexHashHex;
            var writer = new PackWriter();
            writer.AddFrom(pack, e => e.Path is not ("data/changed" or "data/removed" or "data/renamed"));
            writer.AddFrom(pack, pack.Find("data/renamed")!, "moved/renamed");
            writer.Add("data/changed", Prose(500, 9));
            writer.AddRecord("data/added", Imp);
            // Written over the pack while it is still open, as the editor saves.
            result = writer.Write(file);
        }
        var after = Stored(file);
        Assert.Equal(["audio/old", "audio/quality5", "audio/raw-prose", "data/added", "data/changed", "data/ogre", "data/unknown", "moved/renamed", "ui/noise"],
            after.Keys.Order(StringComparer.Ordinal));
        foreach (var path in new[] { "audio/old", "audio/quality5", "audio/raw-prose", "data/ogre", "data/unknown", "ui/noise" })
            Assert.Equal(before[path], after[path]);
        Assert.Equal(before["data/renamed"], after["moved/renamed"]);
        // Seven kept their stored bytes; the changed and the added entries were compressed afresh.
        Assert.Equal((9, 7), (result.Entries, result.Reused));
        Assert.Null(result.OldProblem);

        using var data = new VirtualFileSystem([Mount.Pack(file)]);
        Assert.Equal(Prose(500, 9), data.ReadAllBytes("data/changed"));
        Assert.Equal(RecordSerializer.ToJson(Imp), RecordSerializer.ToJson(data.Load<Creature>("data/added")));
        Assert.Equal("the Moved", data.Load<Creature>("moved/renamed").Title);
        Assert.False(data.Exists("data/removed"));
        Assert.False(data.Exists("data/renamed"));
        // The old record stays in version 1 and still loads, upgraded.
        using (var pack = PackFile.Open(file))
        {
            Assert.Equal(RecordFixtures.Read("test.creature", "creature-1", ".rec"), pack.Read("audio/old"));
            Assert.NotEqual(hashBefore, pack.IndexHashHex);
        }
        Assert.Contains(data.Load<Creature>("audio/old").Limbs, limb => limb.Kind == LimbKind.Leg);
    }

    [Fact]
    public void APackRewrittenWithNothingChangedIsTheSameFile()
    {
        using var folder = new TempFolder();
        var original = Original();
        var file = folder.Write("mod.pak", original);
        using (var pack = PackFile.Open(file))
        {
            var writer = new PackWriter();
            writer.AddFrom(pack);
            Assert.Equal(9, writer.Count);
            Assert.Equal(9, writer.Write(file).Reused);
        }
        Assert.Equal(original, File.ReadAllBytes(file));
    }

    [Fact]
    public void ASubsetWrittenElsewhereKeepsItsEntriesAsTheyWereStored()
    {
        using var folder = new TempFolder();
        var file = folder.Write("mod.pak", Original());
        var before = Stored(file);
        string[] chosen = ["audio/old", "audio/quality5", "data/ogre", "data/unknown"];
        // Save As over a pack that holds one of the contents stored otherwise: the source's bytes win.
        var other = MakePack(folder["copy.pak"], ("audio/quality5", Prose(3000, 1)), ("x", [1]));
        Assert.NotEqual(before["audio/quality5"], Stored(other)["audio/quality5"]);
        PackWriteResult result;
        byte[] streamed;
        using (var pack = PackFile.Open(file))
        {
            var writer = new PackWriter();
            writer.AddFrom(pack, e => chosen.Contains(e.Path));
            result = writer.Write(other);
            streamed = Bytes(writer);
        }
        var after = Stored(other);
        Assert.Equal(chosen, after.Keys.Order(StringComparer.Ordinal));
        foreach (var path in chosen)
            Assert.Equal(before[path], after[path]);
        Assert.Equal(4, result.Reused);
        // A stream with no pack to reuse gets the same bytes.
        Assert.Equal(File.ReadAllBytes(other), streamed);
        Assert.Equal(before, Stored(file));
    }

    [Fact]
    public void AnEntryAddedFromAPackIsReadAndCheckedFirst()
    {
        using var folder = new TempFolder();
        var good = Original();
        var file = folder.Write("mod.pak", good);
        // The stored bytes of ui/noise (raw, the last entry) damaged: its index entry still matches.
        var damaged = (byte[])good.Clone();
        using (var pack = PackFile.Open(file))
            damaged[pack.Find("ui/noise")!.Offset] ^= 1;
        var bad = folder.Write("damaged.pak", damaged);
        using var damagedPack = PackFile.Open(bad);
        var writer = new PackWriter();
        var refused = Assert.Throws<DataFileException>(() => writer.AddFrom(damagedPack, damagedPack.Find("ui/noise")!));
        Assert.Equal((bad, "ui/noise", "the content doesn't match its hash (damaged)"), (refused.File, refused.Entry, refused.Problem));
        Assert.Equal(0, writer.Count);
        // All of a pack: the ones before the damaged entry stay added.
        Assert.Throws<DataFileException>(() => writer.AddFrom(damagedPack));
        Assert.Equal(8, writer.Count);
        Assert.False(writer.Contains("ui/noise"));

        // A path outside the rule is refused before the entry is read.
        Assert.Throws<ArgumentException>(() => writer.AddFrom(damagedPack, damagedPack.Find("ui/noise")!, "UI/noise"));

        // An entry of another pack, a path outside the rule, a path taken.
        using var pack2 = PackFile.Open(file);
        Assert.Throws<ArgumentException>(() => new PackWriter().AddFrom(pack2, damagedPack.Find("ui/noise")!));
        Assert.Throws<ArgumentException>(() => new PackWriter().AddFrom(pack2, pack2.Find("ui/noise")!, "UI/noise"));
        var taken = new PackWriter();
        taken.Add("ui/noise", [1]);
        Assert.Equal("'ui/noise' is already in the pack (Parameter 'path')", Assert.Throws<ArgumentException>(() => taken.AddFrom(pack2, pack2.Find("ui/noise")!)).Message);
        Assert.Equal("'ui/noise' is an entry, so it can't be a folder of 'ui/noise/deeper' (Parameter 'path')",
            Assert.Throws<ArgumentException>(() => taken.AddFrom(pack2, pack2.Find("ui/noise")!, "ui/noise/deeper")).Message);
        // A record renamed beside the file that would be its loose form.
        var twin = new PackWriter();
        twin.Add("data/ogre.json", "[1]"u8);
        Assert.StartsWith("'data/ogre' can't be a record beside the file 'data/ogre.json'",
            Assert.Throws<ArgumentException>(() => twin.AddFrom(pack2, pack2.Find("data/ogre")!)).Message);
        Assert.Equal(1, twin.Count);
    }

    [Fact]
    public void AFileAddedFromAPackFollowsTheRulesForJsonFiles()
    {
        // A hand-made pack whose file data/x.json holds a record's JSON form: the writer never makes one.
        using var folder = new TempFolder();
        var json = Encoding.UTF8.GetBytes(RecordSerializer.ToJson(Imp));
        var file = folder.Write("hand.pak", new RawPack().Raw("data/plain.json", "[1, 2]"u8.ToArray()).Raw("data/x.json", json).Build());
        using var pack = PackFile.Open(file);
        var writer = new PackWriter();
        writer.AddFrom(pack, pack.Find("data/plain.json")!);
        Assert.Equal("'data/x.json' holds a record's JSON form: add it as the record 'data/x' (Parameter 'path')",
            Assert.Throws<ArgumentException>(() => writer.AddFrom(pack, pack.Find("data/x.json")!)).Message);
        // Renamed away from .json it is a file like any other; a record's binary form isn't looked at.
        writer.AddFrom(pack, pack.Find("data/x.json")!, "data/x.txt");
        Assert.Equal(2, writer.Count);
        var record = new RawPack().Raw("data/r.json", json);
        record.Entries[0].Flags = 2;
        using var recordPack = PackFile.Open(folder.Write("record.pak", record.Build()));
        writer.AddFrom(recordPack, recordPack.Find("data/r.json")!);
        Assert.Equal(3, writer.Count);
    }

    [Fact]
    public void RemovingAnEntryFreesItsPathAndItsFolders()
    {
        var writer = new PackWriter();
        writer.Add("a/b/c", [1]);
        writer.Add("a/b/d", [2]);
        writer.Add("a/e", [3]);
        Assert.True(writer.Contains("a/b/c"));
        Assert.False(writer.Contains("a/b"));
        Assert.True(writer.Remove("a/b/c"));
        Assert.False(writer.Contains("a/b/c"));
        Assert.False(writer.Remove("a/b/c"));
        Assert.False(writer.Remove("a/b"));
        Assert.Equal(2, writer.Count);
        // a/b is still the folder of a/b/d, and a of both.
        Assert.Throws<ArgumentException>(() => writer.Add("a/b", [4]));
        Assert.Throws<ArgumentException>(() => writer.Add("a", [4]));
        Assert.True(writer.Remove("a/b/d"));
        writer.Add("a/b", [4]);
        Assert.Throws<ArgumentException>(() => writer.Add("a", [5]));
        Assert.True(writer.Remove("a/e"));
        Assert.True(writer.Remove("a/b"));
        writer.Add("a", [5]);
        // Taken again at a path removed, with other content.
        writer.Add("b/c", [6]);
        writer.Remove("b/c");
        writer.Add("b/c", [7]);
        Assert.Throws<ArgumentException>(() => writer.Remove("A"));
        Assert.Throws<ArgumentException>(() => writer.Contains("a/"));
        using var folder = new TempFolder();
        using var pack = PackFile.Open(folder.Write("p.pak", Bytes(writer)));
        Assert.Equal(["a", "b/c"], pack.Entries.Select(e => e.Path));
        Assert.Equal([7], pack.Read("b/c"));
    }

    [Fact]
    public void ARecordAddedFromItsBinaryFormIsStoredAsItIs()
    {
        var old = RecordFixtures.Read("test.creature", "creature-1", ".rec");
        var writer = new PackWriter();
        writer.AddRecordBinary("data/old", old);
        writer.AddRecordBinary("data/opaque", "not read at all"u8);
        writer.Add("data/twin.json", "[1]"u8);
        Assert.Throws<ArgumentException>(() => writer.AddRecordBinary("data/twin", old));
        Assert.Throws<ArgumentException>(() => writer.AddRecordBinary("Data/x", old));
        Assert.Throws<ArgumentException>(() => writer.AddRecordBinary("data/old", old));
        using var folder = new TempFolder();
        var file = folder.Write("p.pak", Bytes(writer));
        using (var pack = PackFile.Open(file))
        {
            Assert.Equal(["data/old", "data/opaque", "data/twin.json"], pack.Entries.Select(e => e.Path));
            Assert.Equal([true, true, false], pack.Entries.Select(e => e.IsRecord));
            Assert.Equal(old, pack.Read("data/old"));
            Assert.Equal("not read at all"u8.ToArray(), pack.Read("data/opaque"));
        }
        using var data = new VirtualFileSystem([Mount.Pack(file)]);
        Assert.Contains(data.Load<Creature>("data/old").Limbs, limb => limb.Kind == LimbKind.Leg);
    }

    [Fact]
    public void TheIndexHashIsTheHeadersAsOpenCheckedIt()
    {
        using var folder = new TempFolder();
        var file = MakePack(folder["p.pak"], Entry("a", "one"), Entry("b/c", "two"));
        var bytes = File.ReadAllBytes(file);
        var offset = (int)BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(16));
        string first;
        using (var pack = PackFile.Open(file))
        {
            Assert.Equal(SHA256.HashData(bytes.AsSpan(offset)), pack.IndexHash.ToArray());
            Assert.Equal(bytes[32..64], pack.IndexHash.ToArray());
            Assert.Equal(Convert.ToHexStringLower(bytes[32..64]), pack.IndexHashHex);
            first = pack.IndexHashHex;
        }
        // The same entries again: the same hash; one changed, renamed or added: another.
        MakePack(file, Entry("a", "one"), Entry("b/c", "two"));
        using (var pack = PackFile.Open(file))
            Assert.Equal(first, pack.IndexHashHex);
        foreach (var entries in new[]
        {
            new[] { Entry("a", "One"), Entry("b/c", "two") },
            [Entry("a", "one"), Entry("b/d", "two")],
            [Entry("a", "one"), Entry("b/c", "two"), Entry("c", "")],
        })
        {
            MakePack(file, entries);
            using var pack = PackFile.Open(file);
            Assert.NotEqual(first, pack.IndexHashHex);
            Assert.Equal(SHA256.HashData(File.ReadAllBytes(file).AsSpan((int)BinaryPrimitives.ReadUInt64LittleEndian(File.ReadAllBytes(file).AsSpan(16)))),
                pack.IndexHash.ToArray());
        }
        // An empty pack's index is empty.
        using var empty = PackFile.Open(MakePack(folder["empty.pak"]));
        Assert.Equal(SHA256.HashData([]), empty.IndexHash.ToArray());
    }
}
