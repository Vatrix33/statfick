using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Statventure.Records;

namespace Statventure.Packs;

/// <summary>
/// An open pack (Docs/Assets.md, "Packs"). Opening reads the header and the index and checks them:
/// the index's hash, and that the index describes a well-formed pack (paths under the rule, unique,
/// in order, no entry also another's folder, no record beside the file that would be its loose form;
/// the entries one after another from the header to the index; lengths that fit their flags). An
/// entry is a file or a record (its content a record's binary form). Entries are read on demand by positional reads, safe from
/// several threads, and every read checks the content's hash (Q6). The file is opened sharing read,
/// write and delete, so it can be rebuilt while it is open; the open file keeps what it was until
/// it is opened again.
/// </summary>
public sealed class PackFile : IDisposable
{
    readonly SafeFileHandle _handle;
    readonly PackEntry[] _entries;
    readonly Dictionary<string, PackEntry> _byPath;
    readonly Dictionary<string, PackEntry> _byHash;
    readonly byte[] _indexHash;

    PackFile(string file, SafeFileHandle handle, PackEntry[] entries, byte[] indexHash)
    {
        FilePath = file;
        _handle = handle;
        _entries = entries;
        _indexHash = indexHash;
        _byPath = entries.ToDictionary(e => e.Path, StringComparer.Ordinal);
        _byHash = entries.DistinctBy(e => e.HashHex).ToDictionary(e => e.HashHex);
    }

    /// <summary>The pack's file (a full path).</summary>
    public string FilePath { get; }

    /// <summary>Its entries in path order (ordinal).</summary>
    public IReadOnlyList<PackEntry> Entries => _entries;

    /// <summary>
    /// The SHA-256 of the index's bytes, as the header gives it and <see cref="Open"/> checked it (Q9): it
    /// changes whenever an entry is added, removed, renamed or changed, so an editor can tell whether
    /// another program rewrote the file since it was opened (Docs/Editor.md, P23).
    /// </summary>
    public ReadOnlySpan<byte> IndexHash => _indexHash;

    /// <summary><see cref="IndexHash"/> as lowercase hexadecimal.</summary>
    public string IndexHashHex => Convert.ToHexStringLower(_indexHash);

    /// <summary>Opens and checks a pack; a damaged one or one of another format version throws a <see cref="DataFileException"/>.</summary>
    public static PackFile Open(string file)
    {
        var full = System.IO.Path.GetFullPath(file);
        var handle = File.OpenHandle(full, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, FileOptions.RandomAccess);
        try
        {
            var entries = ReadIndex(full, handle, out var indexHash);
            return new PackFile(full, handle, entries, indexHash);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    /// <summary>The entry at <paramref name="path"/>, or null.</summary>
    public PackEntry? Find(string path)
    {
        PackPath.Check(path);
        return _byPath.GetValueOrDefault(path);
    }

    /// <summary>The content at <paramref name="path"/>, checked against its hash.</summary>
    public byte[] Read(string path) =>
        Read(Find(path) ?? throw new FileNotFoundException($"{FilePath}: no entry '{path}'", path));

    /// <summary>The content of <paramref name="entry"/>, checked against its hash.</summary>
    public byte[] Read(PackEntry entry) => ReadWithStored(entry).Content;

    /// <summary>The content of <paramref name="entry"/>, checked against its hash, and the stored bytes it came from.</summary>
    internal (byte[] Content, byte[] Stored) ReadWithStored(PackEntry entry)
    {
        if (!_byPath.TryGetValue(entry.Path, out var own) || !ReferenceEquals(own, entry))
            throw new ArgumentException($"'{entry.Path}' isn't an entry of {FilePath}", nameof(entry));
        var stored = ReadStored(entry);
        var content = Decode(entry, stored);
        if (!SHA256.HashData(content).AsSpan().SequenceEqual(entry.Hash))
            throw new DataFileException(FilePath, entry.Path, "the content doesn't match its hash (damaged)");
        return (content, stored);
    }

    /// <summary>Reads every entry; gives what is wrong with each damaged one (nothing for a sound pack).</summary>
    public IReadOnlyList<DataFileException> Verify()
    {
        var problems = new List<DataFileException>();
        foreach (var entry in _entries)
        {
            try
            {
                Read(entry);
            }
            catch (DataFileException problem)
            {
                problems.Add(problem);
            }
        }
        return problems;
    }

    /// <summary>
    /// Writes every entry in its loose form under <paramref name="folder"/> (made if missing; it must be
    /// empty): a file as it is, a record as its JSON form at <c>&lt;path&gt;.json</c> (S3-1; its type found by
    /// name in <paramref name="types"/>, the engine's by default). So the folder mounts, and packs, as the
    /// same entries. Every entry is read and converted before anything is written.
    /// </summary>
    public void ExtractTo(string folder, RecordTypes? types = null)
    {
        types ??= RecordTypes.Engine;
        var root = System.IO.Path.GetFullPath(folder);
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
            throw new IOException($"{root} isn't empty");
        var forms = _entries.Select(entry => entry.IsRecord
            ? (entry.Path + LooseRecord.Extension, RecordJson(entry, types))
            : (entry.Path, Read(entry))).ToArray();
        Directory.CreateDirectory(root);
        foreach (var (path, content) in forms)
        {
            var target = System.IO.Path.Combine(root, path);
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, content);
        }
    }

    /// <summary>
    /// A record entry's JSON form (UTF-8, as the serializer writes it, in the current versions): its
    /// type found by name in <paramref name="types"/>. A type not there, or a record that doesn't read,
    /// throws a <see cref="DataFileException"/> naming the entry.
    /// </summary>
    public byte[] RecordJson(PackEntry entry, RecordTypes types)
    {
        if (!entry.IsRecord)
            throw new ArgumentException($"'{entry.Path}' isn't a record", nameof(entry));
        var binary = Read(entry);
        string name;
        try
        {
            name = RecordSerializer.RootTypeName(binary);
        }
        catch (RecordFormatException problem)
        {
            throw new DataFileException(FilePath, entry.Path, $"not a readable record: {problem.Message}", problem);
        }
        var type = types.Find(name)
            ?? throw new DataFileException(FilePath, entry.Path, $"a record of type {name}, which isn't a record type known here");
        try
        {
            return RecordSerializer.ToJsonUtf8(type, RecordSerializer.FromBinary(type, binary));
        }
        catch (RecordFormatException problem)
        {
            throw new DataFileException(FilePath, entry.Path, $"not a readable {name} record: {problem.Message}", problem);
        }
    }

    /// <summary>
    /// An entry whose content has this hash and whose stored bytes give it, with those bytes; null when
    /// there is none or its stored bytes are damaged (<paramref name="damaged"/> then names it).
    /// </summary>
    internal (PackEntry Entry, byte[] Stored)? FindStored(ReadOnlySpan<byte> hash, out string? damaged)
    {
        damaged = null;
        if (!_byHash.TryGetValue(Convert.ToHexStringLower(hash), out var entry))
            return null;
        try
        {
            var stored = ReadStored(entry);
            if (SHA256.HashData(Decode(entry, stored)).AsSpan().SequenceEqual(hash))
                return (entry, stored);
        }
        catch (DataFileException)
        {
        }
        damaged = entry.Path;
        return null;
    }

    public void Dispose() => _handle.Dispose();

    byte[] ReadStored(PackEntry entry)
    {
        var stored = new byte[entry.StoredLength];
        ReadExactly(FilePath, entry.Path, _handle, stored, entry.Offset);
        return stored;
    }

    byte[] Decode(PackEntry entry, byte[] stored)
    {
        if (!entry.Compressed)
            return stored;
        var content = new byte[entry.ContentLength];
        using var decoder = new BrotliDecoder();
        var status = decoder.Decompress(stored, content, out var consumed, out var written);
        if (status != OperationStatus.Done || consumed != stored.Length || written != content.Length)
            throw new DataFileException(FilePath, entry.Path, "its compressed bytes don't give its content's length (damaged)");
        return content;
    }

    static void ReadExactly(string file, string? entry, SafeFileHandle handle, Span<byte> buffer, long offset)
    {
        while (buffer.Length > 0)
        {
            var read = RandomAccess.Read(handle, buffer, offset);
            if (read == 0)
                throw new DataFileException(file, entry, "the file ends too early (truncated, or changed since it was opened)");
            buffer = buffer[read..];
            offset += read;
        }
    }

    static PackEntry[] ReadIndex(string file, SafeFileHandle handle, out byte[] indexHash)
    {
        var length = RandomAccess.GetLength(handle);
        if (length < PackFormat.HeaderSize)
            throw new DataFileException(file, null, $"{length} bytes is too short for a pack");
        Span<byte> header = stackalloc byte[PackFormat.HeaderSize];
        ReadExactly(file, null, handle, header, 0);
        if (!header[..PackFormat.Magic.Length].SequenceEqual(PackFormat.Magic))
            throw new DataFileException(file, null, "not a pack (it doesn't start with SVPK)");
        var version = BinaryPrimitives.ReadUInt32LittleEndian(header[PackFormat.VersionAt..]);
        if (version != PackFormat.Version)
            throw new DataFileException(file, null, $"format version {version}; this engine reads version {PackFormat.Version}");
        var flags = BinaryPrimitives.ReadUInt32LittleEndian(header[PackFormat.FlagsAt..]);
        if (flags != 0)
            throw new DataFileException(file, null, $"unknown header flags 0x{flags:x}");
        var count = BinaryPrimitives.ReadUInt32LittleEndian(header[PackFormat.CountAt..]);
        var indexOffset = BinaryPrimitives.ReadUInt64LittleEndian(header[PackFormat.IndexOffsetAt..]);
        var indexLength = BinaryPrimitives.ReadUInt64LittleEndian(header[PackFormat.IndexLengthAt..]);
        if (indexOffset < PackFormat.HeaderSize || indexOffset > (ulong)length || indexLength != (ulong)length - indexOffset)
            throw new DataFileException(file, null, "the index isn't at the end of the file (truncated or damaged)");
        if (indexLength > (ulong)Array.MaxLength)
            throw new DataFileException(file, null, "the index is too large");
        var index = new byte[indexLength];
        ReadExactly(file, null, handle, index, (long)indexOffset);
        if (!SHA256.HashData(index).AsSpan().SequenceEqual(header.Slice(PackFormat.IndexHashAt, PackFormat.HashSize)))
            throw new DataFileException(file, null, "the index doesn't match its hash (damaged)");
        var entries = ParseIndex(file, index, count, (long)indexOffset);
        indexHash = header.Slice(PackFormat.IndexHashAt, PackFormat.HashSize).ToArray();
        return entries;
    }

    static PackEntry[] ParseIndex(string file, byte[] index, uint count, long indexOffset)
    {
        var entries = new List<PackEntry>((int)Math.Min(count, (uint)(index.Length / PackFormat.EntrySize)));
        var at = 0;
        long position = PackFormat.HeaderSize;
        string? previous = null;
        for (var i = 0u; i < count; i++)
        {
            if (index.Length - at < PackFormat.EntrySize)
                throw new DataFileException(file, null, $"the index ends inside entry {i} of {count}");
            var pathLength = BinaryPrimitives.ReadUInt16LittleEndian(index.AsSpan(at));
            at += 2;
            if (index.Length - at < pathLength + PackFormat.EntrySize - 2)
                throw new DataFileException(file, null, $"the index ends inside entry {i} of {count}");
            var path = Encoding.Latin1.GetString(index, at, pathLength);
            at += pathLength;
            if (PackPath.Problem(path) is { } problem)
                throw new DataFileException(file, path, $"a path outside the rule: {problem}");
            if (previous is not null && string.CompareOrdinal(previous, path) >= 0)
                throw new DataFileException(file, path, previous == path ? "a second entry with this path" : "the entries aren't in path order");
            var offset = BinaryPrimitives.ReadUInt64LittleEndian(index.AsSpan(at));
            var stored = BinaryPrimitives.ReadUInt64LittleEndian(index.AsSpan(at + 8));
            var content = BinaryPrimitives.ReadUInt64LittleEndian(index.AsSpan(at + 16));
            var flags = (PackEntryFlags)BinaryPrimitives.ReadUInt32LittleEndian(index.AsSpan(at + 24));
            var hash = index.AsSpan(at + 28, PackFormat.HashSize).ToArray();
            at += PackFormat.EntrySize - 2;
            if (offset != (ulong)position)
                throw new DataFileException(file, path, "it doesn't start where the entry before it ends");
            if (stored > (ulong)(indexOffset - position))
                throw new DataFileException(file, path, "it runs into the index");
            if ((flags & ~PackFormat.KnownFlags) != 0)
                throw new DataFileException(file, path, $"unknown entry flags 0x{(uint)flags:x}");
            if (content > (ulong)Array.MaxLength)
                throw new DataFileException(file, path, "its content is too large");
            if ((flags & PackEntryFlags.Compressed) != 0 ? stored >= content : stored != content)
                throw new DataFileException(file, path, (flags & PackEntryFlags.Compressed) != 0
                    ? "compressed, but not smaller than its content"
                    : "stored as it is, but not as long as its content");
            entries.Add(new PackEntry(path, position, (long)stored, (long)content, flags, hash));
            position += (long)stored;
            previous = path;
        }
        if (at != index.Length)
            throw new DataFileException(file, null, $"the index goes on after its {count} entries");
        if (position != indexOffset)
            throw new DataFileException(file, null, "the entries don't end where the index starts");
        var paths = entries.ToDictionary(e => e.Path, StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            for (var slash = entry.Path.IndexOf('/'); slash >= 0; slash = entry.Path.IndexOf('/', slash + 1))
                if (paths.ContainsKey(entry.Path[..slash]))
                    throw new DataFileException(file, entry.Path[..slash], $"an entry and also the folder of '{entry.Path}'");
            if (entry.IsRecord && paths.TryGetValue(LooseRecord.Twin(entry.Path, record: true)!, out var twin) && !twin.IsRecord)
                throw new DataFileException(file, entry.Path, $"a record beside the file '{twin.Path}', which is its loose form");
        }
        return [.. entries];
    }
}
