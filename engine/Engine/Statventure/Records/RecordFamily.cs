using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace Statventure.Records;

/// <summary>
/// A record type with all its versions (Docs/Assets.md, "Versions"): the current type and the frozen
/// copies nested in it, each version's layout, and the upgrades from each past version to the next.
/// Made once per type, on first use; a declaration that doesn't fit the rules throws then.
/// </summary>
sealed class RecordFamily
{
    static readonly ConcurrentDictionary<Type, RecordFamily> ByType = new();
    static readonly Dictionary<string, RecordFamily> ByName = [];
    static readonly Lock Gate = new();

    readonly Type[] _types;
    readonly Lazy<RecordLayout>[] _layouts;
    readonly Func<object, object>?[] _upgrades;
    readonly Lazy<IReadOnlyDictionary<string, RecordFamily>> _closure;

    RecordFamily(Type current, RecordAttribute record)
    {
        Name = record.Name;
        CurrentType = current;
        CurrentVersion = record.Version;
        if (record.Version < 1)
            throw new InvalidOperationException($"{current.Name}: record versions start at 1");
        if (!IsValidName(record.Name))
            throw new InvalidOperationException($"{current.Name}: the record name \"{record.Name}\" isn't lowercase ASCII letters, digits, '.', '-' and '_'");
        _types = new Type[record.Version];
        _types[^1] = current;
        foreach (var nested in current.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (nested.GetCustomAttribute<RecordAttribute>(false) is not { } past || past.Name != record.Name)
                continue;
            if (past.Version < 1 || past.Version >= record.Version)
                throw new InvalidOperationException($"{nested.FullName}: a past version of {Name} must be between 1 and {record.Version - 1}, not {past.Version}");
            if (_types[past.Version - 1] != null)
                throw new InvalidOperationException($"{Name} has two types for version {past.Version}");
            _types[past.Version - 1] = nested;
        }
        for (var v = 1; v < record.Version; v++)
            if (_types[v - 1] == null)
                throw new InvalidOperationException($"{Name} has no type for version {v} (a frozen copy nested in {current.Name})");
        var union = false;
        for (var v = 1; v <= record.Version; v++)
        {
            var isUnion = IsUnion(_types[v - 1]);
            if (union && !isUnion)
                throw new InvalidOperationException($"{Name} version {v}: a union can't become a plain record again");
            union |= isUnion;
        }
        _layouts = new Lazy<RecordLayout>[record.Version];
        _upgrades = new Func<object, object>?[record.Version - 1];
        for (var v = 1; v <= record.Version; v++)
        {
            var version = v;
            _layouts[v - 1] = new(() => RecordLayout.Build(this, version, _types[version - 1]));
            // A union's past version only lists other tags: its values are its subtypes', which upgrade themselves.
            if (v < record.Version && !IsUnion(_types[v - 1]))
                _upgrades[v - 1] = UpgradeOf(version);
        }
        _closure = new(FindClosure);
    }

    public string Name { get; }
    public Type CurrentType { get; }
    public int CurrentVersion { get; }

    static bool IsUnion(Type type) => type.GetCustomAttributes<SubtypeAttribute>(false).Any();

    static bool IsValidName(string name) =>
        name.Length > 0 && name.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9' or '.' or '-' or '_');

    /// <summary>The family of a current record type (a field's type, a value's type or the type asked for).</summary>
    public static RecordFamily Of(Type type)
    {
        if (ByType.TryGetValue(type, out var known))
            return known;
        lock (Gate)
        {
            if (ByType.TryGetValue(type, out known))
                return known;
            var record = type.GetCustomAttribute<RecordAttribute>(false)
                ?? throw new InvalidOperationException($"{type.FullName} isn't a record type (it has no [Record])");
            if (type.DeclaringType?.GetCustomAttribute<RecordAttribute>(false) is { } outer && outer.Name == record.Name)
                throw new InvalidOperationException($"{type.FullName} is version {record.Version} of {record.Name}, a past version: records are read and written as their current type");
            if (ByName.TryGetValue(record.Name, out var other))
                throw new InvalidOperationException($"{type.FullName} and {other.CurrentType.FullName} are both named {record.Name}");
            var family = new RecordFamily(type, record);
            ByName[record.Name] = family;
            ByType[type] = family;
            return family;
        }
    }

    /// <summary>The type of a version: a frozen copy, or the current type.</summary>
    public Type TypeOf(int version) => _types[version - 1];

    public RecordLayout Layout(int version) => _layouts[version - 1].Value;

    /// <summary>Every record type a value of this one can hold, in any version, itself included, by name.</summary>
    public IReadOnlyDictionary<string, RecordFamily> Closure => _closure.Value;

    IReadOnlyDictionary<string, RecordFamily> FindClosure()
    {
        var found = new Dictionary<string, RecordFamily>();
        var queue = new Queue<RecordFamily>([this]);
        while (queue.TryDequeue(out var family))
        {
            if (!found.TryAdd(family.Name, family))
                continue;
            for (var v = 1; v <= family.CurrentVersion; v++)
                foreach (var next in family.Layout(v).Families)
                    queue.Enqueue(next);
        }
        return found;
    }

    Func<object, object> UpgradeOf(int version)
    {
        var type = _types[version - 1];
        // A record that became a union upgrades straight into the union's current subtypes.
        var next = IsUnion(_types[version]) ? CurrentType : _types[version];
        var method = type.GetMethod("Upgrade", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes)
            ?? throw new InvalidOperationException($"{type.FullName} (version {version} of {Name}) has no Upgrade() method");
        if (method.ReturnType != next)
            throw new InvalidOperationException($"{type.FullName}.Upgrade() returns {method.ReturnType.Name}, not version {version + 1}'s {next.Name}");
        return value => method.Invoke(value, null)
            ?? throw new InvalidOperationException($"{type.FullName}.Upgrade() returned null");
    }

    /// <summary>A value of <paramref name="version"/> upgraded step by step to the current version.</summary>
    object Upgrade(object value, int version)
    {
        for (var v = version; v < CurrentVersion && _upgrades[v - 1] is { } upgrade; v++)
        {
            try
            {
                value = upgrade(value);
            }
            catch (TargetInvocationException e) when (e.InnerException is { } inner)
            {
                throw new RecordFormatException($"the upgrade of {Name} from version {v} failed: {inner.Message}", inner);
            }
        }
        return value;
    }

    /// <summary>Records nested deeper than this are refused, so damaged data can't overflow the stack (a union's value counts once, as its subtype).</summary>
    public const int MaxDepth = 256;

    static RecordFormatException TooDeep() => new($"records are nested more than {MaxDepth} deep");

    public object Read(ref BinaryRecordReader reader)
    {
        var version = reader.Session.VersionOf(this);
        var layout = Layout(version);
        if (!layout.IsUnion && ++reader.Depth > MaxDepth)
            throw TooDeep();
        var value = Upgrade(layout.ReadObject(ref reader), version);
        if (!layout.IsUnion)
            reader.Depth--;
        return value;
    }

    public void Write(BinaryRecordWriter writer, object value)
    {
        var version = writer.Session.VersionOf(this);
        Layout(version).WriteObject(writer, writer.Session.Prepare(this, value, version));
    }

    public void Visit(WriteSession session, object value)
    {
        var version = session.VersionOf(this);
        session.Use(this, version);
        Layout(version).VisitObject(session, session.Prepare(this, value, version));
    }

    public void WriteJson(JsonRecordWriter writer, object value)
    {
        writer.Json.WriteStartObject();
        WriteJsonMembers(writer, value);
        writer.Json.WriteEndObject();
    }

    /// <summary>The members of the value's JSON object (a union's "$type" first), without the braces: the root adds "$types" before them.</summary>
    public void WriteJsonMembers(JsonRecordWriter writer, object value)
    {
        var version = writer.Session.VersionOf(this);
        Layout(version).WriteJsonMembers(writer, writer.Session.Prepare(this, value, version));
    }

    public object ReadJson(JsonRecordReader reader, JsonElement element, ReservedKeys reserved = ReservedKeys.None)
    {
        var version = reader.Session.VersionOf(this);
        var layout = Layout(version);
        if (!layout.IsUnion && ++reader.Depth > MaxDepth)
            throw TooDeep();
        var value = Upgrade(layout.ReadJsonObject(reader, element, reserved), version);
        if (!layout.IsUnion)
            reader.Depth--;
        return value;
    }

    /// <summary>Every version's layout signature, oldest first, as the committed layouts list has them.</summary>
    public string LayoutText() =>
        string.Join("\n", Enumerable.Range(1, CurrentVersion).Select(v => Layout(v).Signature));
}

/// <summary>Keys of a JSON object that belong to the form rather than the record's fields.</summary>
[Flags]
enum ReservedKeys
{
    None = 0,
    /// <summary>"$type": a union's subtype.</summary>
    Type = 1,
    /// <summary>"$types": the type table, at the root.</summary>
    Table = 2,
}
