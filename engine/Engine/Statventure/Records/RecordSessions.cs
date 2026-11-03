using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Statventure.Records;

// The type table a record's data starts with (Docs/Assets.md, "Versions"): the record types it uses
// and their versions, the root's type first, in the order a writer meets them. Binary: a count, then
// each name and version. JSON: "$types", an object of name → version, at the top.

/// <summary>A table entry: a record type and the version the data holds it in.</summary>
sealed class TableEntry(RecordFamily family, int version)
{
    public RecordFamily Family { get; } = family;
    public int Version { get; } = version;
    /// <summary>Set once a value of the type has been read (for the fixtures' coverage check).</summary>
    public bool Used;
}

/// <summary>What a read knows: the root's type and the data's table.</summary>
sealed class ReadSession
{
    readonly Dictionary<RecordFamily, TableEntry> _entries = [];

    public ReadSession(RecordFamily root, IReadOnlyList<(string Name, int Version)> table)
    {
        Root = root;
        if (table.Count == 0)
            throw new RecordFormatException("the type table is empty");
        var known = root.Closure;
        foreach (var (name, version) in table)
        {
            if (!known.TryGetValue(name, out var family))
                throw new RecordFormatException($"the type table names \"{name}\", which isn't a record type a {root.Name} holds");
            if (version < 1)
                throw new RecordFormatException($"the type table gives {name} version {version}");
            if (version > family.CurrentVersion)
                throw new RecordFormatException($"{name} version {version} is newer than this engine reads (version {family.CurrentVersion})");
            if (!_entries.TryAdd(family, new TableEntry(family, version)))
                throw new RecordFormatException($"the type table names {name} twice");
            Entries.Add(_entries[family]);
        }
        if (Entries[0].Family != root)
            throw new RecordFormatException($"the data holds a {Entries[0].Family.Name}, not a {root.Name}");
    }

    public RecordFamily Root { get; }
    public List<TableEntry> Entries { get; } = [];

    /// <summary>The version of a type the data holds a value of.</summary>
    public int VersionOf(RecordFamily family)
    {
        var entry = Entry(family);
        entry.Used = true;
        return entry.Version;
    }

    /// <summary>The version the table gives a type, if it lists it, without counting it as read.</summary>
    public int? PeekVersion(RecordFamily family) => _entries.TryGetValue(family, out var entry) ? entry.Version : null;

    TableEntry Entry(RecordFamily family) =>
        _entries.TryGetValue(family, out var entry) ? entry : throw new RecordFormatException($"the type table doesn't list {family.Name}");
}

/// <summary>
/// Internal options of a write: the tests write fixtures of past versions by naming the version of a
/// type and giving the function that turns a current value into that version's frozen copy.
/// </summary>
sealed class RecordWriteOptions
{
    public IReadOnlyDictionary<string, int> Versions { get; init; } = new Dictionary<string, int>();
    public Func<object, int, object>? Downgrade { get; init; }
}

/// <summary>What a write knows: the versions it writes and (after the visit) the table.</summary>
sealed class WriteSession(RecordWriteOptions? options)
{
    readonly Dictionary<RecordFamily, int> _used = [];

    public List<(RecordFamily Family, int Version)> Table { get; } = [];

    public int VersionOf(RecordFamily family)
    {
        if (options == null || !options.Versions.TryGetValue(family.Name, out var version))
            return family.CurrentVersion;
        if (version < 1 || version > family.CurrentVersion)
            throw new ArgumentException($"{family.Name} has no version {version}");
        return version;
    }

    /// <summary>The value to write for a record of <paramref name="family"/> at <paramref name="version"/>: itself, or its past version's frozen copy.</summary>
    public object Prepare(RecordFamily family, object value, int version)
    {
        if (version == family.CurrentVersion || family.Layout(version).IsUnion)
            return value;
        var past = options!.Downgrade?.Invoke(value, version)
            ?? throw new ArgumentException($"no downgrade of {family.Name} to version {version}");
        if (past.GetType() != family.Layout(version).Type)
            throw new ArgumentException($"the downgrade of {family.Name} to version {version} gave a {past.GetType().Name}");
        return past;
    }

    /// <summary>Notes a type met by the visit before the write, for the table.</summary>
    public void Use(RecordFamily family, int version)
    {
        if (_used.TryAdd(family, version))
            Table.Add((family, version));
    }
}

/// <summary>Writes a record's JSON form: indented by two spaces, LF line ends, only the escapes JSON needs.</summary>
sealed class JsonRecordWriter : IDisposable
{
    public JsonRecordWriter(WriteSession session, IBufferWriter<byte> output, bool indented)
    {
        Session = session;
        Json = new Utf8JsonWriter(output, new JsonWriterOptions
        {
            Indented = indented,
            NewLine = "\n",
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            MaxDepth = 1000,
        });
    }

    public WriteSession Session { get; }
    public Utf8JsonWriter Json { get; }

    public void Dispose() => Json.Dispose();
}

/// <summary>What a JSON read knows: the data's table.</summary>
sealed class JsonRecordReader(ReadSession session)
{
    public ReadSession Session { get; } = session;
    public int Depth;
}
