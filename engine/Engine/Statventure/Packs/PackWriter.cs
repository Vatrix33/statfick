using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Statventure.Packs;

/// <summary>What writing a pack did.</summary>
/// <param name="Entries">The entries written.</param>
/// <param name="Compressed">How many of them are stored compressed.</param>
/// <param name="Reused">How many kept their stored bytes from the pack they replace.</param>
/// <param name="DamagedInOld">Entries of the replaced pack whose stored bytes were damaged, so not kept.</param>
/// <param name="OldProblem">Why the replaced pack couldn't be read, when it couldn't.</param>
public sealed record PackWriteResult(int Entries, int Compressed, int Reused, IReadOnlyList<string> DamagedInOld, string? OldProblem = null);

/// <summary>
/// Makes a pack (Docs/Assets.md, "Packs"): entries in path order, each Brotli-compressed (quality 11,
/// 22-bit window, so the same input gives the same bytes) unless that doesn't make it smaller, then
/// stored as it is; the index at the end. Re-packing keeps the stored bytes of every entry whose
/// content the pack it replaces already holds (after checking them), so a newer Brotli doesn't
/// change a committed pack's unchanged entries.
/// </summary>
public sealed class PackWriter
{
    readonly Dictionary<string, byte[]> _contents = new(StringComparer.Ordinal);
    readonly HashSet<string> _folders = new(StringComparer.Ordinal);

    /// <summary>The entries added so far.</summary>
    public int Count => _contents.Count;

    /// <summary>Adds an entry (a copy of <paramref name="content"/>).</summary>
    public void Add(string path, ReadOnlySpan<byte> content)
    {
        PackPath.Check(path);
        if (path.Length > ushort.MaxValue)
            throw new ArgumentException($"'{path[..40]}…' is too long for a pack's index", nameof(path));
        if (_contents.ContainsKey(path))
            throw new ArgumentException($"'{path}' is already in the pack", nameof(path));
        if (_folders.Contains(path))
            throw new ArgumentException($"'{path}' is already a folder of other entries", nameof(path));
        for (var slash = path.IndexOf('/'); slash >= 0; slash = path.IndexOf('/', slash + 1))
            if (_contents.ContainsKey(path[..slash]))
                throw new ArgumentException($"'{path[..slash]}' is an entry, so it can't be a folder of '{path}'", nameof(path));
        for (var slash = path.IndexOf('/'); slash >= 0; slash = path.IndexOf('/', slash + 1))
            _folders.Add(path[..slash]);
        _contents.Add(path, content.ToArray());
    }

    /// <summary>Adds every file under <paramref name="folder"/> as an entry at its path below it.</summary>
    public void AddFolder(string folder)
    {
        foreach (var (path, file) in LooseFolder.Scan(folder))
            Add(path, File.ReadAllBytes(file));
    }

    /// <summary>
    /// Writes the pack to <paramref name="destination"/>: into a temporary file beside it, flushed to
    /// disk, then renamed over it. When the destination is a readable pack, its unchanged entries'
    /// stored bytes are kept.
    /// </summary>
    public PackWriteResult Write(string destination)
    {
        var full = Path.GetFullPath(destination);
        PackFile? old = null;
        string? oldProblem = null;
        if (File.Exists(full))
        {
            try
            {
                old = PackFile.Open(full);
            }
            catch (DataFileException problem)
            {
                oldProblem = problem.Message;
            }
        }
        var bytes = new MemoryStream();
        PackWriteResult result;
        using (old)
            result = WriteTo(bytes, old) with { OldProblem = oldProblem };
        var folder = Path.GetDirectoryName(full)!;
        Directory.CreateDirectory(folder);
        var temporary = Path.Combine(folder, $"{Path.GetFileName(full)}.{Path.GetRandomFileName()}.tmp");
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                bytes.Position = 0;
                bytes.CopyTo(file);
                file.Flush(flushToDisk: true);
            }
            File.Move(temporary, full, overwrite: true);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
        return result;
    }

    /// <summary>Writes the pack to <paramref name="output"/>, keeping the stored bytes of entries <paramref name="reuse"/> holds unchanged.</summary>
    public PackWriteResult WriteTo(Stream output, PackFile? reuse = null)
    {
        var paths = _contents.Keys.Order(StringComparer.Ordinal).ToArray();
        var prepared = new Prepared[paths.Length];
        Parallel.For(0, paths.Length, i => prepared[i] = Prepare(_contents[paths[i]], reuse));

        var index = new MemoryStream();
        Span<byte> length = stackalloc byte[2];
        Span<byte> fields = stackalloc byte[PackFormat.EntrySize - 2];
        long position = PackFormat.HeaderSize;
        for (var i = 0; i < paths.Length; i++)
        {
            var entry = prepared[i];
            BinaryPrimitives.WriteUInt16LittleEndian(length, (ushort)paths[i].Length);
            index.Write(length);
            index.Write(Encoding.ASCII.GetBytes(paths[i]));
            BinaryPrimitives.WriteUInt64LittleEndian(fields, (ulong)position);
            BinaryPrimitives.WriteUInt64LittleEndian(fields[8..], (ulong)entry.Stored.Length);
            BinaryPrimitives.WriteUInt64LittleEndian(fields[16..], (ulong)entry.ContentLength);
            BinaryPrimitives.WriteUInt32LittleEndian(fields[24..], (uint)entry.Flags);
            entry.Hash.CopyTo(fields[28..]);
            index.Write(fields);
            position += entry.Stored.Length;
        }
        var indexBytes = index.ToArray();

        Span<byte> header = stackalloc byte[PackFormat.HeaderSize];
        header.Clear();
        PackFormat.Magic.CopyTo(header);
        BinaryPrimitives.WriteUInt32LittleEndian(header[PackFormat.VersionAt..], PackFormat.Version);
        BinaryPrimitives.WriteUInt32LittleEndian(header[PackFormat.FlagsAt..], 0);
        BinaryPrimitives.WriteUInt32LittleEndian(header[PackFormat.CountAt..], (uint)paths.Length);
        BinaryPrimitives.WriteUInt64LittleEndian(header[PackFormat.IndexOffsetAt..], (ulong)position);
        BinaryPrimitives.WriteUInt64LittleEndian(header[PackFormat.IndexLengthAt..], (ulong)indexBytes.Length);
        SHA256.HashData(indexBytes, header.Slice(PackFormat.IndexHashAt, PackFormat.HashSize));
        output.Write(header);
        foreach (var entry in prepared)
            output.Write(entry.Stored);
        output.Write(indexBytes);

        return new PackWriteResult(
            paths.Length,
            prepared.Count(p => (p.Flags & PackEntryFlags.Compressed) != 0),
            prepared.Count(p => p.Reused),
            prepared.Select(p => p.DamagedInOld).OfType<string>().Distinct().Order(StringComparer.Ordinal).ToArray());
    }

    sealed record Prepared(byte[] Hash, byte[] Stored, long ContentLength, PackEntryFlags Flags, bool Reused, string? DamagedInOld);

    static Prepared Prepare(byte[] content, PackFile? reuse)
    {
        var hash = SHA256.HashData(content);
        string? damaged = null;
        if (reuse?.FindStored(hash, out damaged) is { } kept)
            return new Prepared(hash, kept.Stored, content.Length, kept.Entry.Flags, true, null);
        var compressed = new byte[BrotliEncoder.GetMaxCompressedLength(content.Length)];
        if (BrotliEncoder.TryCompress(content, compressed, out var written, PackFormat.BrotliQuality, PackFormat.BrotliWindow)
            && written < content.Length)
            return new Prepared(hash, compressed[..written], content.Length, PackEntryFlags.Compressed, false, damaged);
        return new Prepared(hash, content, content.Length, PackEntryFlags.None, false, damaged);
    }
}
