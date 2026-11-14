using Statventure.Core;
using Statventure.Records;

namespace Statventure.Packs;

/// <summary>What a <see cref="Mount"/> mounts.</summary>
public enum MountKind
{
    /// <summary>One pack.</summary>
    Pack,
    /// <summary>A loose folder: its files as they are, at their paths below it (Q7, Q29).</summary>
    Folder,
    /// <summary>
    /// A mods folder: every file directly in it whose name ends in ".pak" in any case, in the ordinal order of the names (Q8,
    /// Q30); none when the folder is missing. With a <see cref="ModFilter"/> (the game's mod check, GM1-P13), only those it
    /// accepts.
    /// </summary>
    Mods,
    /// <summary>Entries an object gives (<see cref="IMountEntries"/>), read as a pack's are: an editor's unsaved work (Docs/Editor.md, P38).</summary>
    Custom,
}

/// <summary>
/// The entries of a custom mount (<see cref="Mount.Custom"/>; Docs/Editor.md, "Packs for the editor", P38):
/// paths, each a record or a file, and their bytes, given by an object (the editor's unsaved entries over
/// the game's data), so the engine's loaders read them as they read a pack's. Which paths there are and
/// whether each is a record is taken when the file system mounts it and on every
/// <see cref="VirtualFileSystem.Reload"/> (Reload after adding, removing or renaming one); the bytes are
/// read whenever a path is read, from any thread the data is read on.
/// </summary>
public interface IMountEntries
{
    /// <summary>Every path it gives, each once, under the path rule (<see cref="PackPath"/>).</summary>
    IEnumerable<string> Paths { get; }

    /// <summary>Whether the entry at <paramref name="path"/> is a record (its bytes a record's binary form, as in a pack) rather than a file.</summary>
    bool IsRecord(string path);

    /// <summary>
    /// The entry's content: a file's bytes, or a record's binary form (any version the engine reads). The
    /// file system gives the array to whoever reads it, so give one nothing else changes.
    /// </summary>
    byte[] Read(string path);
}

/// <summary>Something for a <see cref="VirtualFileSystem"/> to mount; looked at again on every Reload.</summary>
public sealed class Mount
{
    Mount(MountKind kind, string path, string at, IMountEntries? entries = null, ModFilter? filter = null)
    {
        Kind = kind;
        Path = path;
        At = at;
        Entries = entries;
        Filter = filter;
    }

    public MountKind Kind { get; }

    /// <summary>The pack file or the folder; for a custom mount its name.</summary>
    public string Path { get; }

    /// <summary>For a folder: the path its files appear below ("" for the top).</summary>
    public string At { get; }

    /// <summary>For a custom mount: what gives its entries.</summary>
    public IMountEntries? Entries { get; }

    /// <summary>For a mods folder: which of its mods mount (null: all of them).</summary>
    public ModFilter? Filter { get; }

    public static Mount Pack(string file) => new(MountKind.Pack, file, "");

    /// <summary>A loose folder, its files appearing below <paramref name="at"/> (e.g. the shipped classes/ folder at "classes").</summary>
    public static Mount Folder(string folder, string at = "")
    {
        if (at.Length > 0)
            PackPath.Check(at, nameof(at));
        return new Mount(MountKind.Folder, folder, at);
    }

    /// <summary>
    /// The mods in <paramref name="folder"/> (Q8, Q30). With <paramref name="filter"/> (the game's mod check, GM1-P13), each
    /// time the file system mounts it (made, and on every Reload) the filter is given the mods found and the mounts below
    /// this one, and only the mods it accepts mount, in their order; each mod mounted is logged ("Data": "mod &lt;file&gt;").
    /// </summary>
    public static Mount Mods(string folder, ModFilter? filter = null) => new(MountKind.Mods, folder, "", filter: filter);

    /// <summary>
    /// Entries <paramref name="entries"/> gives, read as a pack's are (P38). <paramref name="name"/> stands where a
    /// pack's file would: <see cref="VirtualFileSystem.Origin"/> gives it, and errors about its entries name it
    /// with the entry (for an editor's unsaved work, the pack it will be saved into). Mounting checks the
    /// entries as opening a pack checks its index: paths under the rule, each once, none also a folder of
    /// another, no record beside the file <c>&lt;path&gt;.json</c>.
    /// </summary>
    public static Mount Custom(string name, IMountEntries entries)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(entries);
        return new Mount(MountKind.Custom, name, "", entries);
    }

    public override string ToString() => At.Length > 0 ? $"{Kind} {Path} at {At}" : $"{Kind} {Path}";
}

/// <summary>
/// The game's data as one tree of paths (Docs/Assets.md, "The virtual file system"): packs and loose
/// folders mounted bottom to top, each path resolving to the topmost mount that has it; listing a
/// folder gives the union of the mounts' paths. Mods add or replace entries, never delete one.
/// Paths follow <see cref="PackPath"/>'s rule. An entry is a file or a record: a pack flags its
/// records (their binary form), a loose folder holds a record <c>p</c> as <c>p.json</c> in its JSON form,
/// told by content when mounted (Q7, S3-1), and a custom mount (<see cref="Mount.Custom"/>) gives its entries
/// as a pack does; records are read with <see cref="Load{T}"/>, files with
/// <see cref="ReadAllBytes"/> and <see cref="Open"/>. Reads are safe from several threads, also while
/// <see cref="Reload"/> runs: it mounts everything afresh (packs re-read, folders rescanned, the mods
/// folder listed again) and only then puts the new mounts in place, so a failing Reload leaves the
/// old ones.
/// </summary>
public sealed class VirtualFileSystem : IDisposable
{
    const string LogCategory = "Data";

    readonly Mount[] _mounts;
    readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    Table _table;
    bool _disposed;

    /// <summary>
    /// Mounts <paramref name="mounts"/>, the first at the bottom; a damaged pack, a misnamed loose file
    /// or a loose <c>.json</c> file whose start isn't valid JSON throws a <see cref="DataFileException"/>.
    /// </summary>
    public VirtualFileSystem(IEnumerable<Mount> mounts)
    {
        _mounts = [.. mounts];
        _table = Table.Build(_mounts);
    }

    /// <summary>What is mounted, the first at the bottom.</summary>
    public IReadOnlyList<Mount> Mounts => _mounts;

    /// <summary>Raised after every <see cref="Reload"/>, for the systems that reload what they read.</summary>
    public event Action? Reloaded;

    /// <summary>Whether some mount has <paramref name="path"/>.</summary>
    public bool Exists(string path)
    {
        PackPath.Check(path);
        return Use(table => table.Sources.ContainsKey(path));
    }

    /// <summary>Whether <paramref name="path"/>, which some mount must have, is a record (read with <see cref="Load{T}"/>) rather than a file.</summary>
    public bool IsRecord(string path)
    {
        PackPath.Check(path);
        return Use(table => Find(table, path).IsRecord(path));
    }

    /// <summary>The content of the file at <paramref name="path"/> from the topmost mount that has it (a record there is an error).</summary>
    public byte[] ReadAllBytes(string path)
    {
        PackPath.Check(path);
        return Use(table =>
        {
            var source = Find(table, path);
            if (source.IsRecord(path))
                throw source.Problem(path, "a record, not a file (records are read with Load)");
            return source.Read(path);
        });
    }

    /// <summary>
    /// The record at <paramref name="path"/> from the topmost mount that has it: a pack's record entry
    /// (its binary form) or a loose folder's <c>&lt;path&gt;.json</c> (its JSON form), upgraded to the
    /// current version. A file there, a record of another type or one that doesn't read is a
    /// <see cref="DataFileException"/> naming the pack and entry or the loose file.
    /// </summary>
    public T Load<T>(string path) where T : notnull => (T)Load(typeof(T), path);

    /// <summary>The record of type <paramref name="type"/> at <paramref name="path"/> (see <see cref="Load{T}"/>).</summary>
    public object Load(Type type, string path)
    {
        PackPath.Check(path);
        var name = RecordTypes.NameOf(type);
        var (source, content) = Use(table =>
        {
            var source = Find(table, path);
            if (!source.IsRecord(path))
                throw source.Problem(path, $"a file, not a record (expected a {name})");
            return (source, source.Read(path));
        });
        try
        {
            var held = source.Json ? LooseRecord.RootTypeName(content) : RecordSerializer.RootTypeName(content);
            if (held != name)
                throw source.Problem(path, $"a record of type {held}, not {name}");
            return source.Json ? RecordSerializer.FromJson(type, content) : RecordSerializer.FromBinary(type, content);
        }
        catch (RecordFormatException problem)
        {
            throw source.Problem(path, $"not a readable {name} record: {problem.Message}", problem);
        }
    }

    /// <summary>
    /// The record at <paramref name="path"/> as <see cref="Load(Type, string)"/> reads it, of whichever of
    /// <paramref name="kinds"/>' types it holds (for a reader that doesn't know the type in advance: the mod check,
    /// <see cref="ModCheck"/>). A record of a type not among them is a <see cref="DataFileException"/> naming the pack and
    /// entry or the loose file ("a record of type …, which isn't a record type known here"), as one that doesn't read.
    /// </summary>
    public object Load(RecordTypes kinds, string path)
    {
        ArgumentNullException.ThrowIfNull(kinds);
        PackPath.Check(path);
        var (source, content) = Use(table =>
        {
            var source = Find(table, path);
            if (!source.IsRecord(path))
                throw source.Problem(path, "a file, not a record");
            return (source, source.Read(path));
        });
        string held;
        try
        {
            held = source.Json ? LooseRecord.RootTypeName(content) : RecordSerializer.RootTypeName(content);
        }
        catch (RecordFormatException problem)
        {
            throw source.Problem(path, $"not a readable record: {problem.Message}", problem);
        }
        if (kinds.Find(held) is not { } type)
            throw source.Problem(path, $"a record of type {held}, which isn't a record type known here");
        try
        {
            return source.Json ? RecordSerializer.FromJson(type, content) : RecordSerializer.FromBinary(type, content);
        }
        catch (RecordFormatException problem)
        {
            throw source.Problem(path, $"not a readable {held} record: {problem.Message}", problem);
        }
    }

    static Source Find(Table table, string path) =>
        table.Sources.TryGetValue(path, out var source) ? source : throw new FileNotFoundException($"No '{path}' in the game's data", path);

    /// <summary>The content at <paramref name="path"/> as a read-only stream (read and checked whole first).</summary>
    public Stream Open(string path) => new MemoryStream(ReadAllBytes(path), writable: false);

    /// <summary>The file that gives <paramref name="path"/> (a pack, or a loose file; a custom mount's name), or null when no mount has it.</summary>
    public string? Origin(string path)
    {
        PackPath.Check(path);
        return Use(table => table.Sources.TryGetValue(path, out var source) ? source.Origin(path) : null);
    }

    /// <summary>
    /// An error about what <paramref name="path"/> holds that the reader found itself (a record that reads but
    /// breaks its type's rules), naming the topmost mount's pack and entry, or its loose file, as the file
    /// system's own errors do; a <see cref="FileNotFoundException"/> when no mount has the path.
    /// </summary>
    public DataFileException Problem(string path, string problem, Exception? inner = null)
    {
        PackPath.Check(path);
        return Use(table => Find(table, path).Problem(path, problem, inner));
    }

    /// <summary>Every path below <paramref name="folder"/> ("" for all), at any depth, in ordinal order, each once.</summary>
    public IReadOnlyList<string> List(string folder = "")
    {
        if (folder.Length > 0)
            PackPath.Check(folder, nameof(folder));
        return Use(table =>
        {
            if (folder.Length == 0)
                return table.Paths.ToArray();
            var prefix = folder + "/";
            var start = Array.BinarySearch(table.Paths, prefix, StringComparer.Ordinal);
            if (start < 0)
                start = ~start;
            var end = start;
            while (end < table.Paths.Length && table.Paths[end].StartsWith(prefix, StringComparison.Ordinal))
                end++;
            return table.Paths[start..end];
        });
    }

    /// <summary>Mounts everything afresh, then tells <see cref="Reloaded"/>'s listeners; when mounting fails the old mounts stay and it throws.</summary>
    public void Reload()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var fresh = Table.Build(_mounts);
        Table old;
        _lock.EnterWriteLock();
        try
        {
            if (_disposed)
            {
                fresh.Dispose();
                throw new ObjectDisposedException(GetType().FullName);
            }
            old = _table;
            _table = fresh;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
        old.Dispose();
        Reloaded?.Invoke();
    }

    public void Dispose()
    {
        _lock.EnterWriteLock();
        try
        {
            if (_disposed)
                return;
            _disposed = true;
            _table.Dispose();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    T Use<T>(Func<Table, T> read)
    {
        _lock.EnterReadLock();
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return read(_table);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>One mounted pack or folder.</summary>
    abstract class Source : IDisposable
    {
        public abstract IEnumerable<string> Paths { get; }
        /// <summary>Whether its records are in the JSON form (a loose folder) rather than the binary form (a pack).</summary>
        public abstract bool Json { get; }
        public abstract bool IsRecord(string path);
        /// <summary>The content as it holds it: a file's bytes, or a record's form.</summary>
        public abstract byte[] Read(string path);
        public abstract string Origin(string path);
        /// <summary>An error about one of its paths, naming the pack and entry or the loose file.</summary>
        public abstract DataFileException Problem(string path, string problem, Exception? inner = null);
        public virtual void Dispose() { }
    }

    sealed class PackSource(PackFile pack) : Source
    {
        public override IEnumerable<string> Paths => pack.Entries.Select(e => e.Path);
        public override bool Json => false;
        public override bool IsRecord(string path) => pack.Find(path)!.IsRecord;
        public override byte[] Read(string path) => pack.Read(pack.Find(path)!);
        public override string Origin(string path) => pack.FilePath;
        public override DataFileException Problem(string path, string problem, Exception? inner) => new(pack.FilePath, path, problem, inner);
        public override void Dispose() => pack.Dispose();
    }

    sealed class FolderSource(Dictionary<string, LooseEntry> entries) : Source
    {
        public override IEnumerable<string> Paths => entries.Keys;
        public override bool Json => true;
        public override bool IsRecord(string path) => entries[path].Record;
        public override byte[] Read(string path) => File.ReadAllBytes(entries[path].File);
        public override string Origin(string path) => entries[path].File;
        public override DataFileException Problem(string path, string problem, Exception? inner) => new(entries[path].File, null, problem, inner);
    }

    /// <summary>A custom mount (P38): its paths and kinds taken now, read as a pack's entries are, named by the mount's name and the entry.</summary>
    sealed class CustomSource : Source
    {
        readonly string _name;
        readonly IMountEntries _entries;
        readonly Dictionary<string, bool> _records = new(StringComparer.Ordinal);

        public CustomSource(string name, IMountEntries entries)
        {
            _name = name;
            _entries = entries;
            // As opening a pack checks its index (PackFile.ParseIndex).
            foreach (var path in entries.Paths)
            {
                if (PackPath.Problem(path) is { } problem)
                    throw new DataFileException(name, path, $"a path outside the rule: {problem}");
                if (!_records.TryAdd(path, entries.IsRecord(path)))
                    throw new DataFileException(name, path, "a second entry with this path");
            }
            foreach (var (path, record) in _records.OrderBy(e => e.Key, StringComparer.Ordinal))
            {
                for (var slash = path.IndexOf('/'); slash >= 0; slash = path.IndexOf('/', slash + 1))
                    if (_records.ContainsKey(path[..slash]))
                        throw new DataFileException(name, path[..slash], $"an entry and also the folder of '{path}'");
                if (record && _records.TryGetValue(LooseRecord.Twin(path, record: true)!, out var twin) && !twin)
                    throw new DataFileException(name, path, $"a record beside the file '{LooseRecord.Twin(path, record: true)}', which is its loose form");
            }
        }

        public override IEnumerable<string> Paths => _records.Keys;
        public override bool Json => false;
        public override bool IsRecord(string path) => _records[path];
        public override byte[] Read(string path) => _entries.Read(path);
        public override string Origin(string path) => _name;
        public override DataFileException Problem(string path, string problem, Exception? inner) => new(_name, path, problem, inner);
    }

    /// <summary>The mounts as one lookup: each path's topmost source, and every path in order.</summary>
    sealed class Table(List<Source> sources, Dictionary<string, Source> byPath) : IDisposable
    {
        public Dictionary<string, Source> Sources { get; } = byPath;
        public string[] Paths { get; } = [.. byPath.Keys.Order(StringComparer.Ordinal)];

        public static Table Build(IReadOnlyList<Mount> mounts)
        {
            var sources = new List<Source>();
            try
            {
                for (var i = 0; i < mounts.Count; i++)
                    Open(mounts[i], sources, mounts, i);
            }
            catch
            {
                foreach (var source in sources)
                    source.Dispose();
                throw;
            }
            var byPath = new Dictionary<string, Source>(StringComparer.Ordinal);
            foreach (var source in sources)
                foreach (var path in source.Paths)
                    byPath[path] = source;
            return new Table(sources, byPath);
        }

        /// <summary>Opens <paramref name="mount"/>, the <paramref name="index"/>th of <paramref name="mounts"/> (those before it are below it: a mods mount's filter is given them).</summary>
        static void Open(Mount mount, List<Source> into, IReadOnlyList<Mount> mounts, int index)
        {
            switch (mount.Kind)
            {
                case MountKind.Pack:
                    into.Add(new PackSource(PackFile.Open(mount.Path)));
                    break;
                case MountKind.Folder:
                    into.Add(new FolderSource(LooseFolder.Scan(mount.Path, mount.At).ToDictionary(e => e.Path, StringComparer.Ordinal)));
                    break;
                case MountKind.Mods:
                    var mods = ModFiles(mount.Path);
                    if (mount.Filter is { } filter)
                    {
                        // Only the mods it accepts, in the order they were found (GM1-P13).
                        var accepted = new HashSet<string>(filter(mods, [.. mounts.Take(index)]), StringComparer.Ordinal);
                        mods = [.. mods.Where(accepted.Contains)];
                    }
                    foreach (var mod in mods)
                    {
                        Log.Info(LogCategory, $"mod {mod}");
                        into.Add(new PackSource(PackFile.Open(mod)));
                    }
                    break;
                case MountKind.Custom:
                    into.Add(new CustomSource(mount.Path, mount.Entries!));
                    break;
            }
        }

        public void Dispose()
        {
            foreach (var source in sources)
                source.Dispose();
        }
    }

    /// <summary>The mods in <paramref name="folder"/> in the order they mount (Q30).</summary>
    internal static string[] ModFiles(string folder)
    {
        if (!Directory.Exists(folder))
            return [];
        var options = new EnumerationOptions { RecurseSubdirectories = false, AttributesToSkip = 0, MatchType = MatchType.Simple };
        return Directory.EnumerateFiles(Path.GetFullPath(folder), "*", options)
            .Where(file => Path.GetFileName(file).EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ToArray();
    }
}
