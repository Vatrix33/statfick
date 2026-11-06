using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using Statventure.Records;

namespace Statventure.Packs;

/// <summary>What writing a pack did.</summary>
/// <param name="Entries">The entries written.</param>
/// <param name="Compressed">How many of them are stored compressed.</param>
/// <param name="Reused">
/// How many kept their stored bytes rather than being compressed afresh: from the pack they replace (an
/// entry whose content it holds), or as they were added from a pack (<see cref="PackWriter.AddFrom(PackFile, PackEntry, string?)"/>).
/// </param>
/// <param name="DamagedInOld">Entries of the replaced pack whose stored bytes were damaged, so not kept.</param>
/// <param name="OldProblem">Why the replaced pack couldn't be read, when it couldn't.</param>
public sealed record PackWriteResult(int Entries, int Compressed, int Reused, IReadOnlyList<string> DamagedInOld, string? OldProblem = null);

/// <summary>What updating a pack did (<see cref="PackWriter.Update"/>, <c>packer update</c>).</summary>
/// <param name="Written">Writing the pack.</param>
/// <param name="Added">The folder's entries the pack didn't have, in path order.</param>
/// <param name="Replaced">The folder's entries that replaced the pack's, in path order.</param>
public sealed record PackUpdateResult(PackWriteResult Written, IReadOnlyList<string> Added, IReadOnlyList<string> Replaced);

/// <summary>
/// An entry importing one file made (<see cref="PackWriter.ImportFile"/>): its path, whether it is a record,
/// and its content: a file's bytes, or a record's binary form (in the current versions).
/// </summary>
public sealed record ImportedEntry(string Path, bool IsRecord, byte[] Content);

/// <summary>
/// Where an importer finds what it needs when one file is imported on its own (<see cref="PackWriter.ImportFile"/>;
/// Docs/Editor.md, P28), in place of a folder being packed.
/// </summary>
public sealed class ImportSources
{
    /// <summary>
    /// The file on disk for a data path the importer reads besides its source (a terrain's images), or null
    /// when there is none. Not given: the files beside the source on disk and in folders below it, by
    /// their paths relative to the source path's folder (<c>world/terrain/vale-height.png</c> for
    /// <c>world/terrain/vale.terrain.json</c> is <c>vale-height.png</c> beside the file imported).
    /// </summary>
    public Func<string, string?>? FileOf { get; init; }

    /// <summary>
    /// Whether the data the import's entries go into has a path (a stylesheet's urls are checked against
    /// it): for the editor, the data the mod sees, its base and its pack. Not given: none does.
    /// </summary>
    public Func<string, bool>? Exists { get; init; }
}

/// <summary>
/// Makes a pack (Docs/Assets.md, "Packs"): entries in path order, each Brotli-compressed (quality 11,
/// 22-bit window, so the same input gives the same bytes) unless that doesn't make it smaller, then
/// stored as it is; the index at the end. Re-packing keeps the stored bytes of every entry whose
/// content the pack it replaces already holds (after checking them), so a newer Brotli doesn't
/// change a committed pack's unchanged entries. An entry added from an open pack
/// (<see cref="AddFrom(PackFile, PackEntry, string?)"/>) keeps its stored bytes and flags wherever the pack
/// is written, so a pack is rewritten, or a part of it written elsewhere, with only the entries changed
/// (Docs/Editor.md, "Packs for the editor"; M11-17, P23).
/// </summary>
public sealed class PackWriter
{
    /// <summary>An entry to write: its content, whether it is a record, and the stored bytes it keeps when it came from a pack.</summary>
    sealed record Item(byte[] Content, bool Record, Kept? Kept);

    /// <summary>An entry's stored bytes as a pack holds them, with how they are stored and the content's hash (checked when read).</summary>
    sealed record Kept(byte[] Stored, PackEntryFlags Storage, byte[] Hash);

    readonly Dictionary<string, Item> _contents = new(StringComparer.Ordinal);
    /// <summary>Every folder of the entries, with how many entries are below it.</summary>
    readonly Dictionary<string, int> _folders = new(StringComparer.Ordinal);

    /// <summary>The entries added so far.</summary>
    public int Count => _contents.Count;

    /// <summary>Whether an entry is at <paramref name="path"/>.</summary>
    public bool Contains(string path)
    {
        PackPath.Check(path);
        return _contents.ContainsKey(path);
    }

    /// <summary>The entries' paths in ordinal order.</summary>
    internal IEnumerable<string> Paths => _contents.Keys.Order(StringComparer.Ordinal);

    /// <summary>
    /// Adds a file entry (a copy of <paramref name="content"/>). A file named <c>.json</c> must start as
    /// JSON that isn't a record's form (S3-1: its loose form would be a record, or can't be told).
    /// </summary>
    public void Add(string path, ReadOnlySpan<byte> content)
    {
        PackPath.Check(path);
        CheckFile(path, content);
        AddEntry(path, new Item(content.ToArray(), false, null));
    }

    static void CheckFile(string path, ReadOnlySpan<byte> content)
    {
        if (!LooseRecord.MayBeRecord(path))
            return;
        var record = LooseRecord.StartsAsRecord(content, out var problem);
        if (record is null)
            throw new ArgumentException($"'{path}' is {problem}", nameof(path));
        if (record == true)
            throw new ArgumentException($"'{path}' holds a record's JSON form: add it as the record '{LooseRecord.Twin(path, record: false)}'", nameof(path));
    }

    /// <summary>Adds a record entry: <paramref name="value"/>'s binary form, in the current versions of its types.</summary>
    public void AddRecord<T>(string path, T value) where T : notnull => AddRecord(path, typeof(T), value);

    /// <summary>Adds a record entry: the binary form of <paramref name="value"/>, a record of type <paramref name="type"/>.</summary>
    public void AddRecord(string path, Type type, object value)
    {
        PackPath.Check(path);
        AddEntry(path, new Item(RecordSerializer.ToBinary(type, value), true, null));
    }

    /// <summary>
    /// Adds a record entry from its binary form as it is (a copy of <paramref name="binary"/>), not read: of
    /// any version, of a kind not known here (an imported entry, <see cref="ImportedEntry"/>, or a form kept
    /// from elsewhere).
    /// </summary>
    public void AddRecordBinary(string path, ReadOnlySpan<byte> binary) => AddEntry(path, new Item(binary.ToArray(), true, null));

    /// <summary>
    /// Adds an entry of an open pack as it is (Docs/Editor.md, P23): at its own path, or at
    /// <paramref name="path"/> (a rename). It stays a record or a file, its content is the pack's (a record's
    /// binary form as stored: an older version stays in it, a kind not known here is kept), and its stored
    /// bytes and whether they are compressed are kept in the pack this writes, wherever that is. The entry
    /// is read and checked first: a damaged one is a <see cref="DataFileException"/> naming the pack and
    /// entry, and isn't added. The writer's rules hold as for any entry (a path taken, a folder of others).
    /// </summary>
    public void AddFrom(PackFile pack, PackEntry entry, string? path = null)
    {
        path ??= entry.Path;
        PackPath.Check(path);
        var (content, stored) = pack.ReadWithStored(entry);
        if (!entry.IsRecord)
            CheckFile(path, content);
        AddEntry(path, new Item(content, entry.IsRecord, new Kept(stored, entry.Flags & PackEntryFlags.Compressed, entry.Hash.ToArray())));
    }

    /// <summary>
    /// Adds every entry of an open pack that <paramref name="include"/> accepts (all when not given) as
    /// <see cref="AddFrom(PackFile, PackEntry, string?)"/> does, in path order; when one can't be added, the
    /// ones before it stay added.
    /// </summary>
    public void AddFrom(PackFile pack, Func<PackEntry, bool>? include = null)
    {
        foreach (var entry in pack.Entries)
            if (include?.Invoke(entry) != false)
                AddFrom(pack, entry);
    }

    /// <summary>Removes the entry at <paramref name="path"/>; false when there is none.</summary>
    public bool Remove(string path)
    {
        PackPath.Check(path);
        if (!_contents.Remove(path))
            return false;
        for (var slash = path.IndexOf('/'); slash >= 0; slash = path.IndexOf('/', slash + 1))
        {
            var folder = path[..slash];
            if (--_folders[folder] == 0)
                _folders.Remove(folder);
        }
        return true;
    }

    void AddEntry(string path, Item item)
    {
        PackPath.Check(path);
        if (path.Length > ushort.MaxValue)
            throw new ArgumentException($"'{path[..40]}…' is too long for a pack's index", nameof(path));
        if (_contents.ContainsKey(path))
            throw new ArgumentException($"'{path}' is already in the pack", nameof(path));
        if (_folders.ContainsKey(path))
            throw new ArgumentException($"'{path}' is already a folder of other entries", nameof(path));
        for (var slash = path.IndexOf('/'); slash >= 0; slash = path.IndexOf('/', slash + 1))
            if (_contents.ContainsKey(path[..slash]))
                throw new ArgumentException($"'{path[..slash]}' is an entry, so it can't be a folder of '{path}'", nameof(path));
        // A record's loose form is the file <path>.json (Q7), so a pack can't hold a record and a file of that name.
        var record = item.Record;
        if (LooseRecord.Twin(path, record) is { } twin && _contents.TryGetValue(twin, out var other) && other.Record != record)
            throw new ArgumentException(record
                ? $"'{path}' can't be a record beside the file '{twin}': a loose folder holds the record as that file"
                : $"'{path}' can't be a file beside the record '{twin}': a loose folder holds the record as this file", nameof(path));
        for (var slash = path.IndexOf('/'); slash >= 0; slash = path.IndexOf('/', slash + 1))
            _folders[path[..slash]] = _folders.GetValueOrDefault(path[..slash]) + 1;
        _contents.Add(path, item);
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
    public void AddFolder(string folder, RecordTypes? types = null, Importers? importers = null) => AddFolder(folder, types, importers, null);

    /// <summary>
    /// <see cref="AddFolder(string, RecordTypes?, Importers?)"/>, the data the entries go into also having
    /// the paths <paramref name="alsoExists"/> accepts (a pack being updated: <see cref="ImportContext.Exists"/>).
    /// </summary>
    internal void AddFolder(string folder, RecordTypes? types, Importers? importers, Func<string, bool>? alsoExists)
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
        Func<string, bool> exists = alsoExists is null ? byPath.ContainsKey : path => byPath.ContainsKey(path) || alsoExists(path);
        foreach (var file in files)
        {
            if (!claimed.TryGetValue(file.Path, out var match))
                continue;
            var context = new ImportContext(this, match.Importer, file.Path, file.File, match.Ending, sources,
                path => byPath.GetValueOrDefault(path), ImportContext.InTheFolder,
                path => claimed.TryGetValue(path, out var other) ? other.Importer.Name : null, exists, used);
            RunImporter(() => made.TryGetValue(match.Importer, out var importer) ? importer : made[match.Importer] = match.Importer.Create(), context);
        }
        foreach (var entry in LooseFolder.Entries(files.Where(f => !claimed.ContainsKey(f.Path) && !used.Contains(f.Path))))
            AddLoose(entry, types);
    }

    /// <summary>
    /// Runs an importer (made or taken by <paramref name="importer"/>) on its context's source; a failure,
    /// its making's too, is a <see cref="DataFileException"/> naming the source file.
    /// </summary>
    static void RunImporter(Func<IImporter> importer, ImportContext context)
    {
        try
        {
            importer().Import(context);
        }
        catch (Exception problem) when (problem is not DataFileException)
        {
            throw new DataFileException(context.SourceFile, null, $"the {context.Importer.Name} importer: {problem.Message}", problem);
        }
    }

    /// <summary>A loose file no importer took: the record its JSON form holds, or the file as it is.</summary>
    void AddLoose(LooseEntry entry, RecordTypes types)
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

    /// <summary>
    /// Imports one file on its own as packing it in a folder at <paramref name="sourcePath"/> would
    /// (Docs/Editor.md, P28): the importer whose ending its source path has (of <paramref name="importers"/>,
    /// the engine's by default) runs on it, reading the other files it needs and checking the data paths it
    /// names through <paramref name="sources"/>; with none, a <c>&lt;path&gt;.json</c> that starts as a record's
    /// JSON form is the record <c>&lt;path&gt;</c> (its type found by name in <paramref name="types"/>, the
    /// engine's by default) and any other file is the file at <paramref name="sourcePath"/>. Gives the entries
    /// made, in path order; a problem is a <see cref="DataFileException"/> naming <paramref name="file"/> (or
    /// a file it read), as packing the folder would give.
    /// </summary>
    /// <param name="file">The file on disk.</param>
    /// <param name="sourcePath">The data path it is imported as, with its ending (<c>models/stages/arena.glb</c> makes <c>models/stages/arena</c>).</param>
    public static IReadOnlyList<ImportedEntry> ImportFile(string file, string sourcePath, ImportSources? sources = null, RecordTypes? types = null, Importers? importers = null)
    {
        PackPath.Check(sourcePath, nameof(sourcePath));
        types ??= RecordTypes.Engine;
        importers ??= Importers.Engine;
        var full = Path.GetFullPath(file);
        if (!File.Exists(full))
            throw new FileNotFoundException($"No file {full}", full);
        var writer = new PackWriter();
        if (importers.For(sourcePath) is { } match)
        {
            var context = new ImportContext(writer, match.Importer, sourcePath, full, match.Ending, [sourcePath],
                sources?.FileOf ?? Beside(full, sourcePath), ImportContext.ForTheImport,
                path => importers.For(path)?.Importer.Name, sources?.Exists ?? (_ => false), []);
            RunImporter(match.Importer.Create, context);
        }
        else
        {
            foreach (var entry in LooseFolder.Entries([new LooseFile(sourcePath, full)]))
                writer.AddLoose(entry, types);
        }
        return [.. writer.Paths.Select(path => new ImportedEntry(path, writer._contents[path].Record, writer._contents[path].Content))];
    }

    /// <summary>The files beside <paramref name="file"/> on disk and below it, by their data paths below <paramref name="sourcePath"/>'s folder.</summary>
    static Func<string, string?> Beside(string file, string sourcePath)
    {
        var dataFolder = sourcePath[..(sourcePath.LastIndexOf('/') + 1)];
        var diskFolder = Path.GetDirectoryName(file)!;
        return path =>
        {
            if (!path.StartsWith(dataFolder, StringComparison.Ordinal))
                return null;
            var candidate = Path.Combine(diskFolder, path[dataFolder.Length..]);
            return File.Exists(candidate) ? candidate : null;
        };
    }

    /// <summary>
    /// Updates a pack with a folder's entries (<c>packer update</c>; Docs/Assets.md, "Later needs"; Docs/Editor.md,
    /// P30): the folder packed as <see cref="AddFolder(string, RecordTypes?, Importers?)"/> packs it, its
    /// entries added to the pack or replacing the pack's at their paths, every other entry kept as it is
    /// (<see cref="AddFrom(PackFile, PackEntry, string?)"/>: its stored bytes and flags, a record in its
    /// version); the data an import checks against is the folder's files and the pack's entries (a
    /// stylesheet's url may name an image the pack already has). Then written over the pack. A pack that
    /// isn't there, or that <see cref="PackFile.Open"/> refuses, a kept entry that is damaged, and a kept
    /// entry the folder's entries leave no room for (a folder of one of them, or a record beside its loose
    /// file) are errors, and the pack is left as it was.
    /// </summary>
    public static PackUpdateResult Update(string pack, string folder, RecordTypes? types = null, Importers? importers = null)
    {
        var full = Path.GetFullPath(pack);
        if (!File.Exists(full))
            throw new FileNotFoundException($"No pack {full} to update", full);
        var writer = new PackWriter();
        var added = new List<string>();
        var replaced = new List<string>();
        using (var old = PackFile.Open(full))
        {
            writer.AddFolder(folder, types, importers, path => old.Find(path) is not null);
            foreach (var path in writer.Paths)
                (old.Find(path) is null ? added : replaced).Add(path);
            foreach (var entry in old.Entries)
            {
                if (writer._contents.ContainsKey(entry.Path))
                    continue;
                try
                {
                    writer.AddFrom(old, entry);
                }
                catch (ArgumentException problem)
                {
                    throw new DataFileException(full, entry.Path, $"can't be kept beside the folder's entries: {problem.Message}", problem);
                }
            }
        }
        return new PackUpdateResult(writer.Write(full), added, replaced);
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
        var paths = Paths.ToArray();
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

    static Prepared Prepare(Item item, PackFile? reuse)
    {
        var (content, record, fromPack) = item;
        var kind = record ? PackEntryFlags.Record : PackEntryFlags.None;
        // An entry added from a pack keeps the stored bytes it had there (read and checked when added).
        if (fromPack is not null)
            return new Prepared(fromPack.Hash, fromPack.Stored, content.Length, fromPack.Storage | kind, true, null);
        var hash = SHA256.HashData(content);
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
