namespace Statventure.Packs;

/// <summary>
/// What the game's data was asked while a recording ran (<see cref="During{T}"/>; GM1 step 6b, the mod check's shortcut,
/// <see cref="ModChecker"/>): the paths read or asked about, the folders listed, or everything (the mounts themselves were
/// asked for). Every <see cref="VirtualFileSystem"/> notes each question it is asked in the recordings running on the
/// calling flow (an <see cref="AsyncLocal{T}"/>, so a task or a thread the reader starts carries it), and so does the mod
/// check's reference lookup. Recordings nest: a question is noted in each one running. When a check is a function of its
/// value and of what it reads (the engine's and the data library's are), its result can't change unless one of the paths
/// it read, or one below a folder it listed, changes: <see cref="Touches"/>.
/// </summary>
public sealed class DataReads
{
    static readonly AsyncLocal<DataReads?> Current = new();

    readonly DataReads? _outer;
    readonly object _gate = new();
    readonly HashSet<string> _paths = new(StringComparer.Ordinal);
    readonly HashSet<string> _folders = new(StringComparer.Ordinal);
    bool _everything, _consulted;

    DataReads(DataReads? outer) => _outer = outer;

    /// <summary>The paths read or asked about (an entry's content, record, kind, existence, origin or problem).</summary>
    public IReadOnlyCollection<string> Paths
    {
        get
        {
            lock (_gate)
                return [.. _paths];
        }
    }

    /// <summary>The folders listed (<see cref="VirtualFileSystem.List"/>: every path below each; "" for every path).</summary>
    public IReadOnlyCollection<string> Folders
    {
        get
        {
            lock (_gate)
                return [.. _folders];
        }
    }

    /// <summary>Whether the reader asked for something that depends on every path (the file system's mounts).</summary>
    public bool Everything
    {
        get
        {
            lock (_gate)
                return _everything;
        }
    }

    /// <summary>Whether a record's check asked for the data or the reference lookup it was given (<see cref="CheckContext"/>), even without reading anything.</summary>
    public bool Consulted
    {
        get
        {
            lock (_gate)
                return _consulted;
        }
    }

    /// <summary>
    /// Whether the data was used at all: a check consulted it (or the lookup), or something was read, listed or asked of it.
    /// A record's check run with the data that didn't use it gave exactly the problems it gives without the data.
    /// </summary>
    public bool UsedTheData
    {
        get
        {
            lock (_gate)
                return _consulted || _everything || _paths.Count > 0 || _folders.Count > 0;
        }
    }

    /// <summary>Runs <paramref name="read"/> with a recording of its questions to the data (<paramref name="reads"/>).</summary>
    public static T During<T>(Func<T> read, out DataReads reads)
    {
        ArgumentNullException.ThrowIfNull(read);
        var outer = Current.Value;
        reads = new DataReads(outer);
        Current.Value = reads;
        try
        {
            return read();
        }
        finally
        {
            Current.Value = outer;
        }
    }

    /// <summary>
    /// Whether a mount on top holding entries at <paramref name="changed"/> (added, replaced or taken away there) could change
    /// what was read: the mounts themselves were asked for (one more mount, whatever it holds, even nothing), one of the
    /// paths was read or asked about, or one lies below a folder that was listed (any, for a listing of every path).
    /// </summary>
    public bool Touches(IReadOnlySet<string> changed)
    {
        ArgumentNullException.ThrowIfNull(changed);
        lock (_gate)
        {
            if (_everything)
                return true;
            foreach (var path in changed)
            {
                if (_paths.Contains(path) || _folders.Contains(""))
                    return true;
                for (var slash = path.IndexOf('/'); slash > 0; slash = path.IndexOf('/', slash + 1))
                    if (_folders.Contains(path[..slash]))
                        return true;
            }
            return false;
        }
    }

    /// <summary>Notes a question about <paramref name="path"/> in every recording running on this flow.</summary>
    public static void NotePath(string path)
    {
        for (var reads = Current.Value; reads is not null; reads = reads._outer)
            lock (reads._gate)
                reads._paths.Add(path);
    }

    /// <summary>Notes a listing of <paramref name="folder"/> ("" for every path) in every recording running on this flow.</summary>
    public static void NoteFolder(string folder)
    {
        for (var reads = Current.Value; reads is not null; reads = reads._outer)
            lock (reads._gate)
                reads._folders.Add(folder);
    }

    /// <summary>Notes that a record's check asked for its data or its lookup (<see cref="CheckContext"/>) in every recording running on this flow.</summary>
    public static void NoteConsulted()
    {
        for (var reads = Current.Value; reads is not null; reads = reads._outer)
            lock (reads._gate)
                reads._consulted = true;
    }

    /// <summary>Notes a question that depends on every path in every recording running on this flow.</summary>
    public static void NoteEverything()
    {
        for (var reads = Current.Value; reads is not null; reads = reads._outer)
            lock (reads._gate)
                reads._everything = true;
    }
}
