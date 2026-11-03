using System.Buffers.Binary;
using System.Security.Cryptography;
using Statventure.Packs;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>Packs written and read back (Docs/Assets.md, step 1): round trips, the no-shrink rule, the layout, determinism, re-pack reuse, folders.</summary>
public class PackFileTests
{
    static PackFile Open(TempFolder folder, byte[] bytes, string name = "test.pak") => PackFile.Open(folder.Write(name, bytes));

    static void AssertRoundTrip(TempFolder folder, IReadOnlyList<(string Path, byte[] Content)> entries)
    {
        var writer = new PackWriter();
        foreach (var (path, content) in entries)
            writer.Add(path, content);
        Assert.Equal(entries.Count, writer.Count);
        writer.Write(folder["round.pak"]);
        using var pack = PackFile.Open(folder["round.pak"]);
        Assert.Equal(folder["round.pak"], pack.FilePath);
        var expected = entries.OrderBy(e => e.Path, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected.Select(e => e.Path), pack.Entries.Select(e => e.Path));
        foreach (var (path, content) in expected)
        {
            var entry = pack.Find(path)!;
            Assert.Equal(content.Length, entry.ContentLength);
            Assert.Equal(SHA256.HashData(content), entry.Hash.ToArray());
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(content)), entry.HashHex);
            Assert.Equal(content, pack.Read(entry));
            Assert.Equal(content, pack.Read(path));
        }
    }

    [Fact]
    public void AnEmptyPackRoundTrips()
    {
        using var folder = new TempFolder();
        var bytes = Bytes(new PackWriter());
        Assert.Equal(64, bytes.Length);
        using var pack = Open(folder, bytes);
        Assert.Empty(pack.Entries);
        Assert.Null(pack.Find("a"));
        Assert.Empty(pack.Verify());
    }

    [Fact]
    public void OneEntryRoundTripsCompressedOrNot()
    {
        using var folder = new TempFolder();
        AssertRoundTrip(folder, [("audio/themes/alder-vale", Prose(5000))]);
        using (var pack = PackFile.Open(folder["round.pak"]))
            Assert.True(pack.Entries[0].Compressed);
        AssertRoundTrip(folder, [("ui/fonts/a.ttf", Noise(5000))]);
        using (var pack = PackFile.Open(folder["round.pak"]))
            Assert.False(pack.Entries[0].Compressed);
    }

    [Fact]
    public void ManyEntriesRoundTrip()
    {
        using var folder = new TempFolder();
        var random = new Random(3);
        var entries = new List<(string, byte[])>();
        var names = new HashSet<string>();
        for (var i = 0; i < 300; i++)
        {
            var path = $"{(char)('a' + random.Next(5))}/{(char)('a' + random.Next(26))}{random.Next(1000)}-{i}.bin";
            var length = random.Next(4) == 0 ? 0 : random.Next(3000);
            entries.Add((path, random.Next(2) == 0 ? Prose(length, i) : Noise(length, i)));
        }
        AssertRoundTrip(folder, entries);
        using var pack = PackFile.Open(folder["round.pak"]);
        Assert.Contains(pack.Entries, e => e.Compressed);
        Assert.Contains(pack.Entries, e => !e.Compressed && e.ContentLength > 0);
        Assert.Contains(pack.Entries, e => e.ContentLength == 0);
        Assert.Null(pack.Find("a/nothing"));
        Assert.Throws<FileNotFoundException>(() => pack.Read("a/nothing"));
    }

    [Fact]
    public void LargeEntriesRoundTrip()
    {
        using var folder = new TempFolder();
        // Longer than Brotli's 4 MiB window (repeated text, quick to compress), with a stretch of noise in it.
        var tile = Prose(64 << 10);
        var mixed = new byte[5 << 20];
        for (var at = 0; at < mixed.Length; at += tile.Length)
            tile.CopyTo(mixed, at);
        Noise(256 << 10).CopyTo(mixed, 3 << 20);
        AssertRoundTrip(folder, [("big/mixed", mixed), ("big/noise", Noise(1 << 20, 9)), ("small", Prose(100))]);
        using var pack = PackFile.Open(folder["round.pak"]);
        Assert.True(pack.Find("big/mixed")!.Compressed);
        Assert.False(pack.Find("big/noise")!.Compressed);
    }

    [Fact]
    public void AnEntryBrotliDoesntShrinkIsStoredAsItIs()
    {
        // Contents whose Brotli form is exactly as long as they are, and one byte shorter (searched, so a
        // newer Brotli doesn't take the case away).
        byte[]? equal = null, shorter = null;
        for (var prefix = 8; prefix < 40 && (equal is null || shorter is null); prefix++)
            for (var run = 0; run < 40; run++)
            {
                var content = Noise(prefix, prefix).Concat(Enumerable.Repeat((byte)'a', run)).ToArray();
                var difference = RawPack.Compress(content).Length - content.Length;
                if (difference == 0)
                    equal ??= content;
                if (difference == -1)
                    shorter ??= content;
            }
        Assert.NotNull(equal);
        Assert.NotNull(shorter);

        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.Add("equal", equal);
        writer.Add("shorter", shorter);
        writer.Add("noise", Noise(1000));
        writer.Add("one", [42]);
        writer.Add("empty", []);
        writer.Add("text", Prose(1000));
        using var pack = Open(folder, Bytes(writer));
        void Stored(string path, bool compressed, long length)
        {
            var entry = pack.Find(path)!;
            Assert.Equal(compressed, entry.Compressed);
            Assert.Equal(compressed ? PackEntryFlags.Compressed : PackEntryFlags.None, entry.Flags);
            Assert.Equal(length, entry.StoredLength);
        }
        Stored("equal", false, equal.Length);
        Stored("shorter", true, shorter.Length - 1);
        Stored("noise", false, 1000);
        Stored("one", false, 1);
        Stored("empty", false, 0);
        Stored("text", true, RawPack.Compress(Prose(1000)).Length);
        Assert.Equal(equal, pack.Read("equal"));
        Assert.Equal(shorter, pack.Read("shorter"));
    }

    [Fact]
    public void TheLayoutIsVersionOnes()
    {
        var text = Prose(2000);
        var noise = Noise(300);
        var writer = new PackWriter();
        writer.Add("ui/b", noise);
        writer.Add("audio/themes/a", text);
        writer.Add("empty", []);
        var bytes = Bytes(writer);

        // The same pack made field by field: entries in ordinal path order, Brotli at quality 11 with a 22-bit window.
        var expected = new RawPack().Brotli("audio/themes/a", text).Raw("empty", []).Raw("ui/b", noise).Build();
        Assert.Equal(expected, bytes);
        Assert.NotEqual(RawPack.Compress(text, 10), RawPack.Compress(text));
        Assert.NotEqual(RawPack.Compress(text, 11, 24), RawPack.Compress(text));

        // The header's fields where version 1 has them.
        Assert.Equal("SVPK"u8.ToArray(), bytes[..4]);
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4)));
        Assert.Equal(0u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8)));
        Assert.Equal(3u, BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12)));
        var indexOffset = (int)BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(16));
        var indexLength = (int)BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(24));
        Assert.Equal(64 + RawPack.Compress(text).Length + 300, indexOffset);
        Assert.Equal(bytes.Length, indexOffset + indexLength);
        Assert.Equal(SHA256.HashData(bytes.AsSpan(indexOffset)), bytes[32..64]);
        Assert.Equal(3 * 62 + "audio/themes/a".Length + "empty".Length + "ui/b".Length, indexLength);
        // The first index entry: its path, then offset, stored and content lengths, flags, hash.
        var index = bytes.AsSpan(indexOffset);
        Assert.Equal(14, BinaryPrimitives.ReadUInt16LittleEndian(index));
        Assert.Equal("audio/themes/a"u8.ToArray(), index[2..16].ToArray());
        Assert.Equal(64ul, BinaryPrimitives.ReadUInt64LittleEndian(index[16..]));
        Assert.Equal((ulong)RawPack.Compress(text).Length, BinaryPrimitives.ReadUInt64LittleEndian(index[24..]));
        Assert.Equal(2000ul, BinaryPrimitives.ReadUInt64LittleEndian(index[32..]));
        Assert.Equal(1u, BinaryPrimitives.ReadUInt32LittleEndian(index[40..]));
        Assert.Equal(SHA256.HashData(text), index[44..76].ToArray());
        Assert.Equal(RawPack.Compress(text), bytes[64..(64 + RawPack.Compress(text).Length)]);
    }

    [Fact]
    public void EntriesAreInOrdinalOrder()
    {
        // Orders that differ from a culture's (ICU puts '_' before '-', and punctuation before digits).
        using var folder = new TempFolder();
        string[] paths = ["ab", "a_b", "a0", "a/b", "a.b", "a-b"];
        var writer = new PackWriter();
        foreach (var path in paths)
            writer.Add(path, [1]);
        using var pack = Open(folder, Bytes(writer));
        Assert.Equal(["a-b", "a.b", "a/b", "a0", "a_b", "ab"], pack.Entries.Select(e => e.Path));
    }

    [Fact]
    public void PackingIsDeterministic()
    {
        using var folder = new TempFolder();
        var entries = Enumerable.Range(0, 40).Select(i => ($"e/{i:d3}", i % 2 == 0 ? Prose(i * 100, i) : Noise(i * 10, i))).ToArray();
        var forward = new PackWriter();
        foreach (var (path, content) in entries)
            forward.Add(path, content);
        var backward = new PackWriter();
        foreach (var (path, content) in entries.Reverse())
            backward.Add(path, content);
        var bytes = Bytes(forward);
        Assert.Equal(bytes, Bytes(backward));
        Assert.Equal(bytes, Bytes(forward));
        forward.Write(folder["a.pak"]);
        backward.Write(folder["b.pak"]);
        Assert.Equal(bytes, File.ReadAllBytes(folder["a.pak"]));
        Assert.Equal(bytes, File.ReadAllBytes(folder["b.pak"]));
    }

    [Fact]
    public void RepackingKeepsUnchangedEntriesStoredBytes()
    {
        using var folder = new TempFolder();
        var kept = Prose(3000, 1);
        var moved = Prose(3000, 2);
        // The old pack's Brotli bytes differ from today's (as a newer Brotli's could).
        var old = new RawPack().Brotli("changed", Prose(3000, 3), quality: 5).Brotli("kept", kept, quality: 5).Brotli("old-name", moved, quality: 5);
        var file = folder.Write("data.pak", old.Build());
        Assert.NotEqual(RawPack.Compress(kept, 5), RawPack.Compress(kept));

        var writer = new PackWriter();
        writer.Add("kept", kept);
        writer.Add("new-name", moved);
        writer.Add("changed", Prose(3000, 4));
        using (var reuse = PackFile.Open(file))
            Assert.Equal(new RawPack().Brotli("changed", Prose(3000, 4)).Brotli("kept", kept, quality: 5).Brotli("new-name", moved, quality: 5).Build(),
                Bytes(writer, reuse));
        Assert.Equal(new RawPack().Brotli("changed", Prose(3000, 4)).Brotli("kept", kept).Brotli("new-name", moved).Build(), Bytes(writer));

        var result = writer.Write(file);
        Assert.Equal((3, 3, 2, (string?)null), (result.Entries, result.Compressed, result.Reused, result.OldProblem));
        Assert.Empty(result.DamagedInOld);
        using var pack = PackFile.Open(file);
        Assert.Equal(RawPack.Compress(kept, 5), StoredBytes(file, pack.Find("kept")!));
        Assert.Equal(RawPack.Compress(moved, 5), StoredBytes(file, pack.Find("new-name")!));
        Assert.Equal(RawPack.Compress(Prose(3000, 4)), StoredBytes(file, pack.Find("changed")!));
        Assert.Equal(kept, pack.Read("kept"));
    }

    [Fact]
    public void ARawEntryStaysRawWhenKept()
    {
        using var folder = new TempFolder();
        var text = Prose(3000);
        var file = folder.Write("data.pak", new RawPack().Raw("a", text).Build());
        var writer = new PackWriter();
        writer.Add("a", text);
        Assert.Equal(1, writer.Write(file).Reused);
        using var pack = PackFile.Open(file);
        Assert.False(pack.Find("a")!.Compressed);
        Assert.Equal(text, pack.Read("a"));
    }

    [Fact]
    public void ADamagedEntryOfTheOldPackIsntKept()
    {
        using var folder = new TempFolder();
        var good = Prose(3000, 1);
        var bad = Prose(3000, 2);
        var old = new RawPack().Brotli("bad", bad, quality: 5).Brotli("good", good, quality: 5);
        old.Entries[0].Stored[^3] ^= 0x10;
        var file = folder.Write("data.pak", old.Build());
        var writer = new PackWriter();
        writer.Add("bad", bad);
        writer.Add("good", good);
        var result = writer.Write(file);
        Assert.Equal(["bad"], result.DamagedInOld);
        Assert.Equal(1, result.Reused);
        Assert.Null(result.OldProblem);
        using var pack = PackFile.Open(file);
        Assert.Equal(RawPack.Compress(bad), StoredBytes(file, pack.Find("bad")!));
        Assert.Equal(bad, pack.Read("bad"));
        Assert.Empty(pack.Verify());
    }

    [Fact]
    public void AnUnreadableOldPackIsReplaced()
    {
        using var folder = new TempFolder();
        var file = folder.Write("data.pak", Prose(200));
        var writer = new PackWriter();
        writer.Add("a", Prose(100));
        var result = writer.Write(file);
        Assert.Contains("not a pack", result.OldProblem);
        Assert.Equal(0, result.Reused);
        using var pack = PackFile.Open(file);
        Assert.Equal(Prose(100), pack.Read("a"));
    }

    [Fact]
    public void WritingLeavesOnlyThePack()
    {
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.Add("a", Prose(100));
        writer.Write(folder["sub/data.pak"]);
        writer.Write(folder["sub/data.pak"]);
        Assert.Equal([folder["sub/data.pak"]], Directory.GetFileSystemEntries(folder["sub"]));
    }

    [Fact]
    public void TheWriterRefusesBadEntries()
    {
        var writer = new PackWriter();
        writer.Add("a/b", [1]);
        Assert.Contains("'a/b' is already in the pack", Assert.Throws<ArgumentException>(() => writer.Add("a/b", [2])).Message);
        Assert.Throws<ArgumentException>(() => writer.Add("a", [2]));
        Assert.Throws<ArgumentException>(() => writer.Add("a/b/c", [2]));
        Assert.Throws<ArgumentException>(() => writer.Add("A", [2]));
        writer.Add("a/c", [3]);
        writer.Add("ab", [3]);
        writer.Add("a-b", [3]);
        Assert.Equal(4, writer.Count);
    }

    [Fact]
    public void AFolderPacksAndExtractsToTheSameFiles()
    {
        using var folder = new TempFolder();
        folder.Write("source/audio/themes/alder-vale", Prose(4000));
        folder.Write("source/audio/themes/combat", Prose(4000, 1));
        folder.Write("source/ui/fonts/x.ttf", Noise(2000));
        folder.Write("source/.gitkeep", []);
        folder.Write("source/top", "top");
        var writer = new PackWriter();
        writer.AddFolder(folder["source"]);
        writer.Write(folder["a.pak"]);
        using var pack = PackFile.Open(folder["a.pak"]);
        Assert.Equal([".gitkeep", "audio/themes/alder-vale", "audio/themes/combat", "top", "ui/fonts/x.ttf"], pack.Entries.Select(e => e.Path));

        pack.ExtractTo(folder["out"]);
        var files = Directory.EnumerateFiles(folder["source"], "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(folder["source"], f)).Order().ToArray();
        Assert.Equal(files, Directory.EnumerateFiles(folder["out"], "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(folder["out"], f)).Order());
        foreach (var file in files)
            Assert.Equal(File.ReadAllBytes(Path.Combine(folder["source"], file)), File.ReadAllBytes(Path.Combine(folder["out"], file)));

        var again = new PackWriter();
        again.AddFolder(folder["out"]);
        again.Write(folder["b.pak"]);
        Assert.Equal(File.ReadAllBytes(folder["a.pak"]), File.ReadAllBytes(folder["b.pak"]));

        Assert.Throws<IOException>(() => pack.ExtractTo(folder["out"]));
        pack.ExtractTo(folder["empty-out"]);
        Directory.CreateDirectory(folder["existing-empty"]);
        pack.ExtractTo(folder["existing-empty"]);
    }

    [Theory]
    [InlineData("Bad.txt")]
    [InlineData("sub/x.json~")]
    [InlineData("aux.json")]
    [InlineData("name.")]
    public void PackingAFolderRefusesAMisnamedFile(string name)
    {
        using var folder = new TempFolder();
        folder.Write("source/good", "x");
        var bad = folder.Write($"source/{name}", "x");
        var refused = Assert.Throws<DataFileException>(() => new PackWriter().AddFolder(folder["source"]));
        Assert.Equal(bad, refused.File);
        Assert.Null(refused.Entry);
        Assert.Contains("outside the path rule", refused.Message);
        Assert.Contains(bad, refused.Message);
    }

    [Fact]
    public void VerifyNamesEachDamagedEntry()
    {
        using var folder = new TempFolder();
        var file = MakePack(folder["a.pak"], ("a", Prose(1000)), ("b", Noise(100)), ("c", Prose(1000, 1)));
        using (var pack = PackFile.Open(file))
            Assert.Empty(pack.Verify());
        var bytes = File.ReadAllBytes(file);
        using (var pack = PackFile.Open(file))
        {
            bytes[pack.Find("b")!.Offset + 7] ^= 1;
            bytes[pack.Find("c")!.Offset + 3] ^= 1;
        }
        File.WriteAllBytes(file, bytes);
        using var damaged = PackFile.Open(file);
        var problems = damaged.Verify();
        Assert.Equal(["b", "c"], problems.Select(p => p.Entry));
        Assert.All(problems, p => Assert.Equal(file, p.File));
    }

    [Fact]
    public void SeveralThreadsReadAtOnce()
    {
        using var folder = new TempFolder();
        var entries = Enumerable.Range(0, 64).Select(i => ($"e{i}", i % 2 == 0 ? Prose(20000, i) : Noise(20000, i))).ToArray();
        using var pack = PackFile.Open(MakePack(folder["a.pak"], entries));
        Parallel.For(0, entries.Length * 8, i => Assert.Equal(entries[i % entries.Length].Item2, pack.Read(entries[i % entries.Length].Item1)));
    }

    [Fact]
    public void AnEntryOfAnotherPackIsRefused()
    {
        using var folder = new TempFolder();
        using var a = PackFile.Open(MakePack(folder["a.pak"], Entry("x", "a")));
        using var b = PackFile.Open(MakePack(folder["b.pak"], Entry("x", "b")));
        Assert.Throws<ArgumentException>(() => a.Read(b.Find("x")!));
    }

    static byte[] StoredBytes(string file, PackEntry entry) =>
        File.ReadAllBytes(file).AsSpan((int)entry.Offset, (int)entry.StoredLength).ToArray();
}
