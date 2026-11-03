using System.Buffers.Binary;
using System.Security.Cryptography;
using Statventure.Packs;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>
/// Damaged packs (Docs/Assets.md, "Method"; Q6): each kind of damage gives the error it should, naming
/// the pack and the entry, and never wrong data. The index and header are checked when a pack is
/// opened, an entry's content on every read.
/// </summary>
public class PackDamageTests
{
    static byte[] SmallPack() => Bytes(SmallWriter());

    static PackWriter SmallWriter()
    {
        var writer = new PackWriter();
        writer.Add("audio/a", Prose(60));
        writer.Add("audio/b", Noise(40));
        writer.Add("ui/c", []);
        return writer;
    }

    static DataFileException Refused(TempFolder folder, byte[] bytes)
    {
        var file = folder.Write("damaged.pak", bytes);
        var refused = Assert.Throws<DataFileException>(() => PackFile.Open(file).Dispose());
        Assert.Equal(file, refused.File);
        Assert.StartsWith(file + ": ", refused.Message);
        return refused;
    }

    [Fact]
    public void ATruncatedPackIsRefusedAtEveryLength()
    {
        using var folder = new TempFolder();
        var bytes = SmallPack();
        for (var length = 0; length < bytes.Length; length++)
        {
            var refused = Refused(folder, bytes[..length]);
            Assert.Contains(length < 64 ? "too short for a pack" : "the index isn't at the end of the file", refused.Problem);
            Assert.Null(refused.Entry);
        }
        using var whole = PackFile.Open(folder.Write("whole.pak", bytes));
        Assert.Equal(3, whole.Entries.Count);
    }

    [Fact]
    public void ABitFlippedInTheHeaderIsCaught()
    {
        using var folder = new TempFolder();
        var bytes = SmallPack();
        for (var bit = 0; bit < 64 * 8; bit++)
        {
            var damaged = (byte[])bytes.Clone();
            damaged[bit / 8] ^= (byte)(1 << (bit % 8));
            var problem = Refused(folder, damaged).Problem;
            var expected = (bit / 8) switch
            {
                < 4 => "not a pack",
                < 8 => "format version",
                < 12 => "unknown header flags",
                < 16 => "entr",
                < 32 => "the index isn't at the end of the file",
                _ => "the index doesn't match its hash",
            };
            Assert.True(problem.Contains(expected), $"bit {bit}: {problem}");
        }
    }

    [Fact]
    public void ABitFlippedInTheIndexIsCaughtWhenOpened()
    {
        using var folder = new TempFolder();
        var bytes = SmallPack();
        var indexOffset = (int)BinaryPrimitives.ReadUInt64LittleEndian(bytes.AsSpan(16));
        for (var bit = indexOffset * 8; bit < bytes.Length * 8; bit++)
        {
            var damaged = (byte[])bytes.Clone();
            damaged[bit / 8] ^= (byte)(1 << (bit % 8));
            Assert.Contains("the index doesn't match its hash", Refused(folder, damaged).Problem);
        }
    }

    [Fact]
    public void ABitFlippedInAnEntryIsCaughtWhenItIsRead()
    {
        using var folder = new TempFolder();
        var bytes = SmallPack();
        var file = folder.Write("a.pak", bytes);
        PackEntry[] entries;
        using (var pack = PackFile.Open(file))
            entries = [.. pack.Entries];
        Assert.True(entries[0].Compressed);
        Assert.False(entries[1].Compressed);
        byte[][] contents;
        using (var pack = PackFile.Open(file))
            contents = [.. entries.Select(e => pack.Read(e.Path))];
        foreach (var (entry, content) in entries.Zip(contents).Where(e => e.First.StoredLength > 0))
        {
            var harmless = 0;
            for (var bit = 0; bit < entry.StoredLength * 8; bit++)
            {
                var damaged = (byte[])bytes.Clone();
                damaged[entry.Offset + bit / 8] ^= (byte)(1 << (bit % 8));
                File.WriteAllBytes(file, damaged);
                using var pack = PackFile.Open(file);
                try
                {
                    // A flip in a compressed stream may leave its content as it was (the window size of a
                    // short stream, say): then the content is right, which is all that counts.
                    Assert.Equal(content, pack.Read(entry.Path));
                    harmless++;
                }
                catch (DataFileException refused)
                {
                    Assert.Equal(file, refused.File);
                    Assert.Equal(entry.Path, refused.Entry);
                    Assert.Equal($"{file}: entry '{entry.Path}': {refused.Problem}", refused.Message);
                }
                foreach (var (other, otherContent) in entries.Zip(contents).Where(e => e.First.Path != entry.Path))
                    Assert.Equal(otherContent, pack.Read(other.Path));
            }
            Assert.True(entry.Compressed ? harmless < entry.StoredLength : harmless == 0, $"{entry.Path}: {harmless} harmless flips");
        }
    }

    [Fact]
    public void TheHashIsCheckedOnEveryRead()
    {
        using var folder = new TempFolder();
        var file = MakePack(folder["a.pak"], ("raw", Noise(100)), ("text", Prose(1000)));
        using var pack = PackFile.Open(file);
        foreach (var entry in pack.Entries)
        {
            var content = pack.Read(entry);
            using (var writer = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
            {
                writer.Position = entry.Offset + 5;
                var b = writer.ReadByte();
                writer.Position = entry.Offset + 5;
                writer.WriteByte((byte)(b ^ 0x20));
            }
            Assert.Equal(entry.Path, Assert.Throws<DataFileException>(() => pack.Read(entry)).Entry);
            Assert.Equal(entry.Path, Assert.Throws<DataFileException>(() => pack.Read(entry)).Entry);
            using (var writer = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
            {
                writer.Position = entry.Offset + 5;
                var b = writer.ReadByte();
                writer.Position = entry.Offset + 5;
                writer.WriteByte((byte)(b ^ 0x20));
            }
            Assert.Equal(content, pack.Read(entry));
        }
    }

    [Fact]
    public void AnEntryWhoseContentDoesntMatchItsHashIsRefused()
    {
        using var folder = new TempFolder();
        foreach (var compressed in new[] { false, true })
        {
            var raw = compressed ? new RawPack().Brotli("a", Prose(500)) : new RawPack().Raw("a", Noise(500));
            raw.Raw("b", Noise(10, 1));
            raw.Entries[0].Hash = SHA256.HashData(Prose(500, 1));
            using var pack = PackFile.Open(folder.Write($"{compressed}.pak", raw.Build()));
            var refused = Assert.Throws<DataFileException>(() => pack.Read("a"));
            Assert.Equal("a", refused.Entry);
            Assert.Contains("the content doesn't match its hash", refused.Problem);
            Assert.Equal(Noise(10, 1), pack.Read("b"));
        }
    }

    [Fact]
    public void ACompressedEntryMustGiveExactlyItsLength()
    {
        using var folder = new TempFolder();
        var content = Prose(500);
        void Bad(Action<RawPack.Entry> damage, string name)
        {
            var raw = new RawPack().Brotli("a", content);
            damage(raw.Entries[0]);
            using var pack = PackFile.Open(folder.Write($"{name}.pak", raw.Build()));
            var refused = Assert.Throws<DataFileException>(() => pack.Read("a"));
            Assert.Equal("a", refused.Entry);
            Assert.Contains("don't give its content's length", refused.Problem);
        }
        Bad(e => e.ContentLength--, "shorter");
        Bad(e => e.ContentLength++, "longer");
        Bad(e => e.Stored = [.. e.Stored, 0], "trailing");
        Bad(e => e.Stored = [.. Noise(e.Stored.Length - 1)], "not-brotli");
        Bad(e => e.Stored = e.Stored[..^1], "cut");
    }

    [Theory]
    [InlineData("SVPJ", 1u, 0u, "not a pack")]
    [InlineData("PKSV", 1u, 0u, "not a pack")]
    [InlineData("svpk", 1u, 0u, "not a pack")]
    [InlineData("SVPK", 0u, 0u, "format version 0; this engine reads version 1")]
    [InlineData("SVPK", 2u, 0u, "format version 2; this engine reads version 1")]
    [InlineData("SVPK", 1u, 1u, "unknown header flags 0x1")]
    [InlineData("SVPK", 1u, 0x80000000u, "unknown header flags 0x80000000")]
    public void AWrongHeaderIsRefused(string magic, uint version, uint flags, string problem)
    {
        using var folder = new TempFolder();
        var raw = new RawPack { Magic = System.Text.Encoding.ASCII.GetBytes(magic), Version = version, HeaderFlags = flags }.Raw("a", [1]);
        var refused = Refused(folder, raw.Build());
        Assert.StartsWith(problem, refused.Problem);
        Assert.Null(refused.Entry);
    }

    public static TheoryData<string, Func<RawPack>, string?, string> BadIndexes => new()
    {
        { "a second path", () => new RawPack().Raw("a", [1]).Raw("a", [2]), "a", "a second entry with this path" },
        { "out of order", () => new RawPack().Raw("b", [1]).Raw("a", [2]), "a", "the entries aren't in path order" },
        { "out of ordinal order", () => new RawPack().Raw("a/b", [1]).Raw("a-b", [2]), "a-b", "the entries aren't in path order" },
        { "uppercase", () => new RawPack().Raw("A", [1]), "A", "a path outside the rule: 'A' isn't allowed" },
        { "empty name", () => new RawPack().Raw("a//b", [1]), "a//b", "a path outside the rule: a name is empty" },
        { "empty path", () => new RawPack().Raw("", [1]), "", "a path outside the rule: the path is empty" },
        { "device name", () => new RawPack().Raw("aux", [1]), "aux", "a path outside the rule: 'aux' is a name Windows keeps" },
        { "dot", () => new RawPack().Raw("a/..", [1]), "a/..", "a path outside the rule: the name '..' ends in a dot" },
        { "not ASCII", () => new RawPack().Raw("café", [1]), "café", "a path outside the rule: 'é' isn't allowed" },
        { "entry and folder", () => new RawPack().Raw("a", [1]).Raw("a/b", [2]), "a", "an entry and also the folder of 'a/b'" },
        { "entry and folder apart", () => new RawPack().Raw("a", [1]).Raw("a-x", [2]).Raw("a/b/c", [3]), "a", "an entry and also the folder of 'a/b/c'" },
        { "entry and inner folder", () => new RawPack().Raw("a/b", [1]).Raw("a/b/c", [3]), "a/b", "an entry and also the folder of 'a/b/c'" },
        { "unknown entry flags", () => Edit(new RawPack().Raw("a", [1]), e => e.Flags = 4), "a", "unknown entry flags 0x4" },
        { "unknown entry flags with known ones", () => Edit(new RawPack().Raw("a", [1]), e => e.Flags = 0x80000002), "a", "unknown entry flags 0x80000002" },
        { "a record beside its loose file", () => Edit(new RawPack().Raw("a", [1]).Raw("a.json", [2]), e => e.Flags = 2), "a", "a record beside the file 'a.json', which is its loose form" },
        { "a gap", () => Edit(new RawPack().Raw("a", [1]).Raw("b", [2]), e => e.Offset = 66, 1), "b", "it doesn't start where the entry before it ends" },
        { "an overlap", () => Edit(new RawPack().Raw("a", [1, 2]).Raw("b", [2]), e => e.Offset = 65, 1), "b", "it doesn't start where the entry before it ends" },
        { "the first not after the header", () => Edit(new RawPack().Raw("a", [1]), e => e.Offset = 63), "a", "it doesn't start where the entry before it ends" },
        { "into the index", () => Edit(new RawPack().Raw("a", [1]), e => e.StoredLength = 2), "a", "it runs into the index" },
        { "far into the index", () => Edit(new RawPack().Raw("a", [1]), e => e.StoredLength = ulong.MaxValue), "a", "it runs into the index" },
        { "raw but longer", () => Edit(new RawPack().Raw("a", [1]), e => e.ContentLength = 2), "a", "stored as it is, but not as long as its content" },
        { "raw but shorter", () => Edit(new RawPack().Raw("a", [1, 2]), e => e.ContentLength = 1), "a", "stored as it is, but not as long as its content" },
        { "compressed but not smaller", () => Edit(new RawPack().Raw("a", [1, 2]), e => e.Flags = 1), "a", "compressed, but not smaller than its content" },
        { "compressed and empty", () => Edit(new RawPack().Raw("a", []), e => e.Flags = 1), "a", "compressed, but not smaller than its content" },
        { "too large", () => Edit(new RawPack().Brotli("a", Prose(100)), e => e.ContentLength = (ulong)Array.MaxLength + 1), "a", "its content is too large" },
        { "count too high", () => new RawPack { Count = 3 }.Raw("a", [1]).Raw("b", [2]), null, "the index ends inside entry 2 of 3" },
        { "count far too high", () => new RawPack { Count = uint.MaxValue }.Raw("a", [1]), null, "the index ends inside entry 1 of 4294967295" },
        { "count too low", () => new RawPack { Count = 1 }.Raw("a", [1]).Raw("b", [2]), null, "the index goes on after its 1 entries" },
        { "bytes after the last entry", () => new RawPack { IndexTail = [0] }.Raw("a", [1]), null, "the index goes on after its 1 entries" },
        { "a path running out of the index", () => Edit(new RawPack().Raw("a", [1]), e => e.PathLength = 30), null, "the index ends inside entry 0 of 1" },
        { "a path just past the index", () => Edit(new RawPack().Raw("a", [1]), e => e.PathLength = 2), null, "the index ends inside entry 0 of 1" },
        { "bytes before the index", () => new RawPack { BeforeIndex = [0] }.Raw("a", [1]), null, "the entries don't end where the index starts" },
    };

    static RawPack Edit(RawPack pack, Action<RawPack.Entry> edit, int entry = 0)
    {
        edit(pack.Entries[entry]);
        return pack;
    }

    [Theory]
    [MemberData(nameof(BadIndexes))]
    public void ABadlyFormedIndexIsRefusedWhenOpened(string name, Func<RawPack> make, string? entry, string problem)
    {
        using var folder = new TempFolder();
        var refused = Refused(folder, make().Build());
        Assert.True(refused.Problem.StartsWith(problem, StringComparison.Ordinal), $"{name}: {refused.Problem}");
        Assert.Equal(entry, refused.Entry);
    }

    [Fact]
    public void AnIndexOutsideItsPlaceIsRefusedBeforeItIsRead()
    {
        using var folder = new TempFolder();
        var length = (ulong)new RawPack().Raw("a", [1]).Build().Length;
        // Inside the header, its length reaching the file's end.
        Assert.Equal("the index isn't at the end of the file (truncated or damaged)",
            Refused(folder, new RawPack { IndexOffset = 0, IndexLength = length }.Raw("a", [1]).Build()).Problem);
        Assert.Equal("the index isn't at the end of the file (truncated or damaged)",
            Refused(folder, new RawPack { IndexOffset = 40, IndexLength = length - 40 }.Raw("a", [1]).Build()).Problem);
        // Past the file's end, offset and length adding up to the file's length around 2^64.
        Assert.Equal("the index isn't at the end of the file (truncated or damaged)",
            Refused(folder, new RawPack { IndexOffset = ulong.MaxValue, IndexLength = length + 1 }.Raw("a", [1]).Build()).Problem);
    }

    [Fact]
    public void AnIndexTooLargeToReadIsRefused()
    {
        // A file of 2 GiB and 64 bytes whose index claims all but the header: sparse, so it costs no space.
        if (!OperatingSystem.IsLinux())
            return;
        using var folder = new TempFolder();
        var header = new RawPack { Count = 0, IndexOffset = 64, IndexLength = 1ul << 31 }.Build()[..64];
        using (var file = new FileStream(folder["huge.pak"], FileMode.CreateNew))
        {
            file.Write(header);
            file.SetLength(64 + (1L << 31));
        }
        var refused = Assert.Throws<DataFileException>(() => PackFile.Open(folder["huge.pak"]).Dispose());
        Assert.Equal("the index is too large", refused.Problem);
    }

    [Fact]
    public void APackCutShortAfterItWasOpenedIsAnError()
    {
        using var folder = new TempFolder();
        var file = MakePack(folder["a.pak"], ("a", Noise(100)), ("b", Noise(100, 1)));
        using var pack = PackFile.Open(file);
        using (var writer = new FileStream(file, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
            writer.SetLength(64 + 150);
        Assert.Equal(Noise(100), pack.Read("a"));
        var refused = Assert.Throws<DataFileException>(() => pack.Read("b"));
        Assert.Equal("b", refused.Entry);
        Assert.Equal("the file ends too early (truncated, or changed since it was opened)", refused.Problem);
    }

    [Fact]
    public void TheIndexHashIsOverTheIndexOnly()
    {
        using var folder = new TempFolder();
        var good = new RawPack().Raw("a", [1]);
        using (var pack = PackFile.Open(folder.Write("good.pak", good.Build())))
            Assert.Equal([1], pack.Read("a"));
        var wrong = new RawPack { IndexHash = new byte[32] }.Raw("a", [1]);
        Assert.Contains("the index doesn't match its hash", Refused(folder, wrong.Build()).Problem);
    }
}
