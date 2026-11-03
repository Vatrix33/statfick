using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Statventure.Packs;

namespace Statventure.Tests.Packs;

/// <summary>
/// A pack's bytes made by hand, field by field, without the engine's writer: to pin version 1's layout
/// (Docs/Assets.md, "How it works") and to make badly formed packs whose index hash still matches.
/// </summary>
public sealed class RawPack
{
    public sealed class Entry
    {
        public required string Path;
        public required byte[] Stored;
        public required ulong ContentLength;
        public required byte[] Hash;
        public uint Flags;
        /// <summary>Overrides where the index says it starts (else where the one before ends).</summary>
        public ulong? Offset;
        /// <summary>Overrides the stored length the index gives (else the stored bytes' length).</summary>
        public ulong? StoredLength;
        /// <summary>Overrides the path's length prefix.</summary>
        public ushort? PathLength;
    }

    public List<Entry> Entries { get; } = [];
    public byte[] Magic = "SVPK"u8.ToArray();
    public uint Version = 1;
    public uint HeaderFlags;
    public uint? Count;
    public ulong? IndexOffset;
    public ulong? IndexLength;
    public byte[]? IndexHash;
    /// <summary>Bytes after the last entry in the index.</summary>
    public byte[] IndexTail = [];
    /// <summary>Bytes between the last entry's stored bytes and the index.</summary>
    public byte[] BeforeIndex = [];

    public RawPack Raw(string path, byte[] content)
    {
        Entries.Add(new Entry { Path = path, Stored = content, ContentLength = (ulong)content.Length, Hash = SHA256.HashData(content) });
        return this;
    }

    public RawPack Brotli(string path, byte[] content, int quality = 11, int window = 22)
    {
        Entries.Add(new Entry { Path = path, Stored = Compress(content, quality, window), ContentLength = (ulong)content.Length, Hash = SHA256.HashData(content), Flags = 1 });
        return this;
    }

    public static byte[] Compress(byte[] content, int quality = 11, int window = 22)
    {
        var output = new byte[BrotliEncoder.GetMaxCompressedLength(content.Length)];
        Assert.True(BrotliEncoder.TryCompress(content, output, out var written, quality, window));
        return output[..written];
    }

    public byte[] Build()
    {
        var index = new MemoryStream();
        var position = 64UL;
        foreach (var entry in Entries)
        {
            var path = Encoding.Latin1.GetBytes(entry.Path);
            U16(index, entry.PathLength ?? (ushort)path.Length);
            index.Write(path);
            U64(index, entry.Offset ?? position);
            U64(index, entry.StoredLength ?? (ulong)entry.Stored.Length);
            U64(index, entry.ContentLength);
            U32(index, entry.Flags);
            index.Write(entry.Hash);
            position += (ulong)entry.Stored.Length;
        }
        index.Write(IndexTail);
        var indexBytes = index.ToArray();
        var pack = new MemoryStream();
        pack.Write(Magic);
        U32(pack, Version);
        U32(pack, HeaderFlags);
        U32(pack, Count ?? (uint)Entries.Count);
        U64(pack, IndexOffset ?? position + (ulong)BeforeIndex.Length);
        U64(pack, IndexLength ?? (ulong)indexBytes.Length);
        pack.Write(IndexHash ?? SHA256.HashData(indexBytes));
        foreach (var entry in Entries)
            pack.Write(entry.Stored);
        pack.Write(BeforeIndex);
        pack.Write(indexBytes);
        return pack.ToArray();
    }

    static void U16(Stream s, ushort v) { Span<byte> b = stackalloc byte[2]; BinaryPrimitives.WriteUInt16LittleEndian(b, v); s.Write(b); }
    static void U32(Stream s, uint v) { Span<byte> b = stackalloc byte[4]; BinaryPrimitives.WriteUInt32LittleEndian(b, v); s.Write(b); }
    static void U64(Stream s, ulong v) { Span<byte> b = stackalloc byte[8]; BinaryPrimitives.WriteUInt64LittleEndian(b, v); s.Write(b); }
}

/// <summary>A folder of its own under the system's temporary folder, deleted afterwards.</summary>
sealed class TempFolder : IDisposable
{
    public TempFolder() => Root = Directory.CreateTempSubdirectory("statventure-packs-").FullName;

    public string Root { get; }

    public string this[string relative] => Path.Combine(Root, relative);

    /// <summary>Writes a file (its folders made), giving its full path.</summary>
    public string Write(string relative, byte[] content)
    {
        var file = this[relative];
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllBytes(file, content);
        return file;
    }

    public string Write(string relative, string text) => Write(relative, Encoding.UTF8.GetBytes(text));

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

static class PackTestData
{
    /// <summary>The record types of the engine and the tests (those the record guard covers), by name.</summary>
    public static RecordTypes TestTypes { get; } = new(Records.RecordFixtures.Types);

    /// <summary>Text-like bytes Brotli makes much smaller.</summary>
    public static byte[] Prose(int length, int seed = 0)
    {
        var words = new[] { "alder ", "vale ", "theme ", "combat ", "drums ", "harmony ", "room ", "lute ", "\n" };
        var random = new Random(seed);
        var bytes = new byte[length];
        for (var i = 0; i < length;)
            foreach (var c in words[random.Next(words.Length)])
                if (i < length)
                    bytes[i++] = (byte)c;
        return bytes;
    }

    /// <summary>Random bytes, which Brotli can't make smaller.</summary>
    public static byte[] Noise(int length, int seed = 0)
    {
        var bytes = new byte[length];
        new Random(seed).NextBytes(bytes);
        return bytes;
    }

    /// <summary>Writes a pack of these entries with the engine's writer, giving its full path.</summary>
    public static string MakePack(string file, params (string Path, byte[] Content)[] entries)
    {
        var writer = new PackWriter();
        foreach (var (path, content) in entries)
            writer.Add(path, content);
        writer.Write(file);
        return Path.GetFullPath(file);
    }

    public static (string, byte[]) Entry(string path, string text) => (path, Encoding.UTF8.GetBytes(text));

    public static string ReadText(this VirtualFileSystem data, string path) => Encoding.UTF8.GetString(data.ReadAllBytes(path));

    public static byte[] Bytes(PackWriter writer, PackFile? reuse = null)
    {
        var output = new MemoryStream();
        writer.WriteTo(output, reuse);
        return output.ToArray();
    }
}
