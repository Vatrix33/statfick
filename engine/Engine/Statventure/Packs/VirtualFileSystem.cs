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
    /// <summary>A mods folder: every file directly in it whose name ends in ".pak" in any case, in the ordinal order of the names (Q8, Q30); none when the folder is missing.</summary>
    Mods,
}

/// <summary>Something for a <see cref="VirtualFileSystem"/> to mount; looked at again on every Reload.</summary>
public sealed class Mount
{
    Mount(MountKind kind, string path, string at)
    {
        Kind = kind;
        Path = path;
        At = at;
    }

    public MountKind Kind { get; }

    /// <summary>The pack file or the folder.</summary>
    public string Path { get; }

    /// <summary>For a folder: the path its files appear below ("" for the top).</summary>
    public string At { get; }

    public static Mount Pack(string file) => new(MountKind.Pack, file, "");

    /// <summary>A loose folder, its files appearing below <paramref name="at"/> (e.g. the shipped classes/ folder at "classes").</summary>
    public static Mount Folder(string folder, string at = "")
    {
        if (at.Length > 0)
            PackPath.Check(at, nameof(at));
        return new Mount(MountKind.Folder, folder, at);
    }

    public static Mount Mods(string folder) => new(MountKind.Mods, folder, "");

    public override string ToString() => At.Length > 0 ? $"{Kind} {Path} at {At}" : $"{Kind} {Path}";
}

/// <summary>
/// The game's data as one tree of paths (Docs/Assets.md, "The virtual file system"): packs and loose
/// folders mounted bottom to top, each path resolving to the topmost mount that has it; listing a
/// folder gives the union of the mounts' paths. Mods add or replace entries, never delete one.
/// Paths follow <see cref="PackPath"/>'s rule. An entry is a file or a record: a pack flags its
/// records (their binary form), a loose folder holds a record <c>p</c> as <c>p.json</c> in its JSON form,
/// told by content when mounted (Q7, S3-1); records are read with <see cref="Load{T}"/>, files with
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

    static Source Find(Table table, string path) =>
        table.Sources.TryGetValue(path, out var source) ? source : throw new FileNotFoundException($"No '{path}' in the game's data", path);

    /// <summary>The content at <paramref name="path"/> as a read-only stream (read and checked whole first).</summary>
    public Stream Open(string path) => new MemoryStream(ReadAllBytes(path), writable: false);

    /// <summary>The file that gives <paramref name="path"/> (a pack, or a loose file), or null when no mount has it.</summary>
    public string? Origin(string path)
    {
        PackPath.Check(path);
        return Use(table => table.Sources.TryGetValue(path, out var source) ? source.Origin(path) : null);
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
                foreach (var mount in mounts)
                    Open(mount, sources);
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

        static void Open(Mount mount, List<Source> into)
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
                    foreach (var mod in ModFiles(mount.Path))
                    {
                        Log.Info(LogCategory, $"mod {mod}");
                        into.Add(new PackSource(PackFile.Open(mod)));
                    }
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
