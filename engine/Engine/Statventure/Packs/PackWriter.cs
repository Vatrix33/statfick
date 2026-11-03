using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Statventure.Records;

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
    readonly Dictionary<string, (byte[] Content, bool Record)> _contents = new(StringComparer.Ordinal);
    readonly HashSet<string> _folders = new(StringComparer.Ordinal);

    /// <summary>The entries added so far.</summary>
    public int Count => _contents.Count;

    /// <summary>
    /// Adds a file entry (a copy of <paramref name="content"/>). A file named <c>.json</c> must start as
    /// JSON that isn't a record's form (S3-1: its loose form would be a record, or can't be told).
    /// </summary>
    public void Add(string path, ReadOnlySpan<byte> content)
    {
        PackPath.Check(path);
        if (LooseRecord.MayBeRecord(path))
        {
            var record = LooseRecord.StartsAsRecord(content, out var problem);
            if (record is null)
                throw new ArgumentException($"'{path}' is {problem}", nameof(path));
            if (record == true)
                throw new ArgumentException($"'{path}' holds a record's JSON form: add it as the record '{LooseRecord.Twin(path, record: false)}'", nameof(path));
        }
        AddEntry(path, content.ToArray(), record: false);
    }

    /// <summary>Adds a record entry: <paramref name="value"/>'s binary form, in the current versions of its types.</summary>
    public void AddRecord<T>(string path, T value) where T : notnull => AddRecord(path, typeof(T), value);

    /// <summary>Adds a record entry: the binary form of <paramref name="value"/>, a record of type <paramref name="type"/>.</summary>
    public void AddRecord(string path, Type type, object value)
    {
        PackPath.Check(path);
        AddEntry(path, RecordSerializer.ToBinary(type, value), record: true);
    }

    void AddEntry(string path, byte[] content, bool record)
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
        // A record's loose form is the file <path>.json (Q7), so a pack can't hold a record and a file of that name.
        if (LooseRecord.Twin(path, record) is { } twin && _contents.TryGetValue(twin, out var other) && other.Record != record)
            throw new ArgumentException(record
                ? $"'{path}' can't be a record beside the file '{twin}': a loose folder holds the record as that file"
                : $"'{path}' can't be a file beside the record '{twin}': a loose folder holds the record as this file", nameof(path));
        for (var slash = path.IndexOf('/'); slash >= 0; slash = path.IndexOf('/', slash + 1))
            _folders.Add(path[..slash]);
        _contents.Add(path, (content, record));
    }

    /// <summary>
    /// Adds a folder in the loose form (Docs/Assets.md, "The packer"; S3-1, S3-3): first each file an
    /// importer takes (by the ending of its name) goes through it, in path order; then every file no
    /// importer took or read is an entry at its path below the folder, a <c>&lt;path&gt;.json</c> that starts
    /// as a record's JSON form being the record <c>&lt;path&gt;</c> in its binary form (in the current
    /// versions; its type found by name in <paramref name="types"/>, the engine's by default). Importers are
    /// <paramref name="importers"/>, the engine's by default. A problem with a file throws a
    /// <see cref="DataFileException"/> naming it.
    /// </summary>
    public void AddFolder(string folder, RecordTypes? types = null, Importers? importers = null)
    {
        types ??= RecordTypes.Engine;
        importers ??= Importers.Engine;
        var files = LooseFolder.Files(folder);
        var byPath = files.ToDictionary(f => f.Path, f => f.File, StringComparer.Ordinal);
        var sources = files.Select(f => f.Path).ToArray();
        var claimed = new Dictionary<string, (ImporterInfo Importer, string Ending)>(StringComparer.Ordinal);
        foreach (var file in files)
            if (importers.For(file.Path) is { } match)
                claimed[file.Path] = match;
        var used = new HashSet<string>(StringComparer.Ordinal);
        var made = new Dictionary<ImporterInfo, IImporter>();
        foreach (var file in files)
        {
            if (!claimed.TryGetValue(file.Path, out var match))
                continue;
            var context = new ImportContext(this, match.Importer, file.Path, match.Ending, byPath, sources,
                path => claimed.TryGetValue(path, out var other) ? other.Importer.Name : null, used);
            try
            {
                if (!made.TryGetValue(match.Importer, out var importer))
                    made[match.Importer] = importer = match.Importer.Create();
                importer.Import(context);
            }
            catch (Exception problem) when (problem is not DataFileException)
            {
                throw new DataFileException(file.File, null, $"the {match.Importer.Name} importer: {problem.Message}", problem);
            }
        }
        foreach (var entry in LooseFolder.Entries(files.Where(f => !claimed.ContainsKey(f.Path) && !used.Contains(f.Path))))
        {
            try
            {
                if (entry.Record)
                    AddLooseRecord(entry, types);
                else
                    Add(entry.Path, File.ReadAllBytes(entry.File));
            }
            catch (ArgumentException problem)
            {
                throw new DataFileException(entry.File, null, problem.Message, problem);
            }
        }
    }

    void AddLooseRecord(LooseEntry entry, RecordTypes types)
    {
        var json = File.ReadAllBytes(entry.File);
        string name;
        try
        {
            name = LooseRecord.RootTypeName(json);
        }
        catch (RecordFormatException problem)
        {
            throw new DataFileException(entry.File, null, $"not a readable record: {problem.Message}", problem);
        }
        var type = types.Find(name)
            ?? throw new DataFileException(entry.File, null, $"a record of type {name}, which isn't a record type known here");
        object value;
        try
        {
            value = RecordSerializer.FromJson(type, json);
        }
        catch (RecordFormatException problem)
        {
            throw new DataFileException(entry.File, null, $"not a readable {name} record: {problem.Message}", problem);
        }
        AddRecord(entry.Path, type, value);
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
        Parallel.For(0, paths.Length, i => prepared[i] = Prepare(_contents[paths[i]].Content, _contents[paths[i]].Record, reuse));

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

    static Prepared Prepare(byte[] content, bool record, PackFile? reuse)
    {
        var hash = SHA256.HashData(content);
        var kind = record ? PackEntryFlags.Record : PackEntryFlags.None;
        string? damaged = null;
        // Kept bytes come with how they are stored; whether the entry is a record is the new entry's own.
        if (reuse?.FindStored(hash, out damaged) is { } kept)
            return new Prepared(hash, kept.Stored, content.Length, kept.Entry.Flags & PackEntryFlags.Compressed | kind, true, null);
        var compressed = new byte[BrotliEncoder.GetMaxCompressedLength(content.Length)];
        if (BrotliEncoder.TryCompress(content, compressed, out var written, PackFormat.BrotliQuality, PackFormat.BrotliWindow)
            && written < content.Length)
            return new Prepared(hash, compressed[..written], content.Length, PackEntryFlags.Compressed | kind, false, damaged);
        return new Prepared(hash, content, content.Length, kind, false, damaged);
    }
}
