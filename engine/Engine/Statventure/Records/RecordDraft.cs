namespace Statventure.Records;

/// <summary>
/// A value in a draft (P10; Docs/Editor.md, "Records: shapes, drafts and marks"): a typed tree of field
/// values that can hold what a record can't (an empty required field, a null where none is allowed, a
/// curve breaking its rules). Immutable: every change makes a new draft sharing what didn't change.
/// The nodes are <see cref="Missing"/> (not given), <see cref="Null"/>, a <see cref="DraftLeaf"/> (a
/// number, bool, string, enum, vector, byte array, or a bulk field's original object), a
/// <see cref="DraftList"/>, a <see cref="DraftMap"/> and a <see cref="RecordDraft"/>; which one a place
/// holds follows its <see cref="ValueShape"/> (a nullable's value is the inner node itself, a union's
/// is the record draft of its subtype).
/// </summary>
public abstract class DraftValue
{
    private protected DraftValue() { }

    /// <summary>Not given: a required field is a problem when built, any other means its default (as an absent JSON key).</summary>
    public static DraftValue Missing { get; } = new Special("missing");

    /// <summary>Null: a nullable's absent value; anywhere else a problem when built.</summary>
    public static DraftValue Null { get; } = new Special("null");

    /// <summary>A leaf holding <paramref name="value"/>.</summary>
    public static DraftLeaf Of(object value) => new(value);

    sealed class Special(string name) : DraftValue
    {
        public override string ToString() => name;
    }
}

/// <summary>A single value: a number, bool, string, enum member, vector, quaternion, matrix or byte array, or a bulk field's whole original value.</summary>
public sealed class DraftLeaf(object value) : DraftValue
{
    public object Value { get; } = value ?? throw new ArgumentNullException(nameof(value));

    public override string ToString() => Value.ToString() ?? "";
}

/// <summary>A list's elements, each a draft value.</summary>
public sealed class DraftList : DraftValue
{
    readonly DraftValue[] _items;

    public DraftList(IEnumerable<DraftValue> items) => _items = Checked(items);

    public static DraftList Empty { get; } = new([]);

    public IReadOnlyList<DraftValue> Items => _items;
    public int Count => _items.Length;
    public DraftValue this[int index] => _items[index];

    public DraftList With(int index, DraftValue item)
    {
        var items = (DraftValue[])_items.Clone();
        items[index] = item;
        return new(items);
    }

    public DraftList Insert(int index, DraftValue item) => new([.. _items[..index], item, .. _items[index..]]);

    public DraftList RemoveAt(int index) => new([.. _items[..index], .. _items[(index + 1)..]]);

    /// <summary>The element at <paramref name="from"/> moved to <paramref name="to"/> (its index afterwards).</summary>
    public DraftList Move(int from, int to)
    {
        var item = _items[from];
        return RemoveAt(from).Insert(to, item);
    }

    static DraftValue[] Checked(IEnumerable<DraftValue> items)
    {
        var array = items.ToArray();
        if (Array.IndexOf(array, null) >= 0)
            throw new ArgumentException("a draft list's element is null (DraftValue.Null is the null value)", nameof(items));
        return array;
    }
}

/// <summary>A map's or pairs' entry: its key (a string, an integer or an enum member) and its value's draft.</summary>
public readonly record struct DraftEntry(object Key, DraftValue Value);

/// <summary>A map's or pairs' entries in order. A key there twice is a problem when built.</summary>
public sealed class DraftMap : DraftValue
{
    readonly DraftEntry[] _entries;

    public DraftMap(IEnumerable<DraftEntry> entries)
    {
        _entries = entries.ToArray();
        foreach (var entry in _entries)
            if (entry.Key is null || entry.Value is null)
                throw new ArgumentException("a draft map's entry needs a key and a value", nameof(entries));
    }

    public static DraftMap Empty { get; } = new([]);

    public IReadOnlyList<DraftEntry> Entries => _entries;
    public int Count => _entries.Length;
    public DraftEntry this[int index] => _entries[index];

    public DraftMap With(int index, DraftEntry entry)
    {
        var entries = (DraftEntry[])_entries.Clone();
        entries[index] = entry;
        return new(entries);
    }

    public DraftMap Insert(int index, DraftEntry entry) => new([.. _entries[..index], entry, .. _entries[index..]]);

    public DraftMap RemoveAt(int index) => new([.. _entries[..index], .. _entries[(index + 1)..]]);

    /// <summary>The entry at <paramref name="from"/> moved to <paramref name="to"/> (its index afterwards).</summary>
    public DraftMap Move(int from, int to)
    {
        var entry = _entries[from];
        return RemoveAt(from).Insert(to, entry);
    }
}

/// <summary>
/// A record's draft (P10): its shape (a plain record's, or a union subtype's) and a draft value per
/// field. Read from a record through the layouts' compiled getters (<see cref="From"/>), made blank
/// (<see cref="Blank(RecordShape)"/>: the defaults, required fields <see cref="DraftValue.Missing"/>),
/// changed by field or by path, and built back into a record through the compiled factories, bottom up
/// (<see cref="Build"/>), which reports what can't be built at its path.
/// </summary>
public sealed class RecordDraft : DraftValue
{
    readonly DraftValue[] _fields;

    public RecordDraft(RecordShape shape, IEnumerable<DraftValue> fields)
    {
        if (shape.IsUnion)
            throw new ArgumentException($"{shape.Name} is a union: a draft is of one of its subtypes", nameof(shape));
        _fields = fields.ToArray();
        if (_fields.Length != shape.Fields.Count)
            throw new ArgumentException($"{shape.Name} has {shape.Fields.Count} fields, not {_fields.Length}", nameof(fields));
        if (Array.IndexOf(_fields, null) >= 0)
            throw new ArgumentException("a field's draft is null (DraftValue.Missing and DraftValue.Null are the drafts of no value)", nameof(fields));
        Shape = shape;
    }

    public RecordShape Shape { get; }

    /// <summary>Each field's draft, in the order of <see cref="RecordShape.Fields"/>.</summary>
    public IReadOnlyList<DraftValue> Fields => _fields;

    /// <summary>A field's draft, by its JSON name or code name.</summary>
    public DraftValue this[string field] => _fields[FieldOf(field).Index];

    public DraftValue this[FieldShape field] => _fields[Own(field).Index];

    /// <summary>This draft with one field's draft replaced (by its JSON name or code name).</summary>
    public RecordDraft With(string field, DraftValue value) => With(FieldOf(field), value);

    public RecordDraft With(FieldShape field, DraftValue value)
    {
        var fields = (DraftValue[])_fields.Clone();
        fields[Own(field).Index] = value;
        return new(Shape, fields);
    }

    FieldShape FieldOf(string name) => Shape.Field(name) ?? throw new ArgumentException($"{Shape.Name} has no field {name}", nameof(name));

    FieldShape Own(FieldShape field) =>
        field.Record == Shape ? field : throw new ArgumentException($"{field} isn't a field of {Shape.Name}", nameof(field));

    /// <summary>The draft of a record (a value of a current, plain record type or a union's subtype).</summary>
    public static RecordDraft From(object record) => DraftReader.Record(record);

    /// <summary>A new record's draft: every field its default, required fields <see cref="DraftValue.Missing"/> (a union has none: its subtypes do).</summary>
    public static RecordDraft Blank(RecordShape shape) =>
        new(shape, shape.Fields.Select(f => f.Required ? Missing : DraftReader.Field(f, f.Default)));

    public static RecordDraft Blank(Type type) => Blank(RecordShape.Of(type));

    /// <summary>The record, or what keeps it from being built, each problem at its path.</summary>
    public DraftBuild Build()
    {
        var problems = new List<RecordProblem>();
        var value = DraftBuilder.Record(this, RecordPath.Root, problems);
        return new DraftBuild(problems.Count == 0 ? value : null, problems);
    }

    /// <summary>The draft at <paramref name="path"/> (fields, list indices, map keys as their text; a map's first entry with the key).</summary>
    public DraftValue At(RecordPath path) => DraftPaths.At(this, path);

    /// <summary>This draft with the draft at <paramref name="path"/> replaced by <paramref name="value"/>.</summary>
    public RecordDraft With(RecordPath path, DraftValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return (RecordDraft)DraftPaths.With(this, null, path, 0, value);
    }

    public override string ToString() => Shape.Name;
}

/// <summary>What building a draft gave: the record, or the problems that keep it from being built.</summary>
public sealed class DraftBuild(object? value, IReadOnlyList<RecordProblem> problems)
{
    /// <summary>The record, when it was built.</summary>
    public object? Value { get; } = value;

    /// <summary>What keeps it from being built, each at its path; none when built.</summary>
    public IReadOnlyList<RecordProblem> Problems { get; } = problems;

    public bool Built => Problems.Count == 0;
}

/// <summary>Records into drafts, through the layouts' getters.</summary>
static class DraftReader
{
    public static RecordDraft Record(object record)
    {
        var shape = RecordShape.Of(record.GetType());
        return new RecordDraft(shape, shape.Fields.Select(f => Field(f, f.Get(record))));
    }

    /// <summary>A field's draft: a bulk field's original object as a leaf, any other by its shape.</summary>
    public static DraftValue Field(FieldShape field, object? value) =>
        !field.IsBulk ? Value(field.Value, value) : value is null ? DraftValue.Null : new DraftLeaf(value);

    static DraftValue Value(ValueShape shape, object? value)
    {
        if (value is null)
            return DraftValue.Null;
        return shape.Kind switch
        {
            ValueKind.Nullable => Value(shape.Element!, value),
            ValueKind.List => new DraftList(shape.ItemsOf(value).Select(item => Value(shape.Element!, item))),
            ValueKind.Map or ValueKind.Pairs => new DraftMap(shape.EntriesOf(value).Select(e => new DraftEntry(e.Key, Value(shape.Element!, e.Value)))),
            ValueKind.Record or ValueKind.Union => Record(value),
            _ => new DraftLeaf(value),
        };
    }
}

/// <summary>
/// Drafts into records, bottom up through the layouts' factories, collecting every problem at its
/// path: a required field not given, a null where none is allowed, a number that isn't an enum's member,
/// a map's key there twice, a string with a lone surrogate (what the forms can't write), and a record
/// whose type refuses its values (an <see cref="ArgumentException"/> from its constructor or setters, as
/// <c>FCurve</c>'s, at the record's path). A node of the wrong kind or type for its place is a mistake of
/// the caller's: an <see cref="InvalidOperationException"/>.
/// </summary>
static class DraftBuilder
{
    /// <summary>Stands for a value that couldn't be built (its problems are already listed).</summary>
    static readonly object Failed = new();

    const string NeedsAValue = "needs a value";
    const string NullNotAllowed = "null in a field that can't be null";

    public static object? Record(RecordDraft draft, RecordPath path, List<RecordProblem> problems)
    {
        var fields = draft.Shape.Fields;
        var values = new object?[fields.Count];
        var present = new bool[fields.Count];
        var built = true;
        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var node = draft.Fields[i];
            var at = path.Field(field.JsonName);
            if (node == DraftValue.Missing)
            {
                if (field.Required)
                {
                    problems.Add(new(at, NeedsAValue));
                    built = false;
                }
                continue;
            }
            var value = field.IsBulk ? Bulk(field.Value, node, at, problems) : Value(field.Value, node, at, problems);
            if (value == Failed)
                built = false;
            else
            {
                values[i] = value;
                present[i] = true;
            }
        }
        if (!built)
            return Failed;
        try
        {
            return draft.Shape.Make(values, present);
        }
        catch (ArgumentException refused)
        {
            problems.Add(new(path, refused.Message));
            return Failed;
        }
    }

    static object? Value(ValueShape shape, DraftValue node, RecordPath path, List<RecordProblem> problems)
    {
        if (node == DraftValue.Missing)
            return Fail(problems, path, NeedsAValue);
        if (node == DraftValue.Null)
            return shape.Kind == ValueKind.Nullable ? null : Fail(problems, path, NullNotAllowed);
        switch (shape.Kind)
        {
            case ValueKind.Nullable:
                return Value(shape.Element!, node, path, problems);
            case ValueKind.List:
            {
                var list = Expect<DraftList>(node, shape, path);
                var items = new object?[list.Count];
                var built = true;
                for (var i = 0; i < items.Length; i++)
                    built &= (items[i] = Value(shape.Element!, list[i], path.Index(i), problems)) != Failed;
                return built ? shape.MakeList(items) : Failed;
            }
            case ValueKind.Map or ValueKind.Pairs:
                return Map(shape, Expect<DraftMap>(node, shape, path), path, problems);
            case ValueKind.Record or ValueKind.Union:
            {
                var record = Expect<RecordDraft>(node, shape, path);
                if (shape.Kind == ValueKind.Record ? record.Shape != shape.Record : !shape.Record!.Subtypes.Contains(record.Shape))
                    throw new InvalidOperationException($"a draft of {record.Shape.Name} where a {shape} belongs (at {path})");
                return Record(record, path, problems);
            }
            default:
                return Leaf(shape, Expect<DraftLeaf>(node, shape, path).Value, path, problems);
        }
    }

    static object? Map(ValueShape shape, DraftMap map, RecordPath path, List<RecordProblem> problems)
    {
        var items = new KeyValuePair<object, object?>[map.Count];
        var seen = new HashSet<object>();
        var built = true;
        for (var i = 0; i < items.Length; i++)
        {
            var (key, node) = map[i];
            if (Leaf(shape.Key!, key, path, problems) == Failed)
            {
                built = false;
                continue;
            }
            if (!seen.Add(key))
            {
                built = false;
                problems.Add(new(path, $"the key {key} is there twice"));
                continue;
            }
            var value = Value(shape.Element!, node, path.Key(shape.Key!.TextOf(key)), problems);
            built &= value != Failed;
            items[i] = new(key, value);
        }
        return built ? shape.MakeMap(items) : Failed;
    }

    /// <summary>A single value, checked as the serializer would refuse to write it.</summary>
    static object? Leaf(ValueShape shape, object value, RecordPath path, List<RecordProblem> problems)
    {
        if (value.GetType() != shape.Type)
            throw new InvalidOperationException($"a {value.GetType().Name} where a {shape} belongs (at {path})");
        if (shape.Kind == ValueKind.Enum && !shape.IsMember(value))
            return Fail(problems, path, $"{value:D} isn't a member of {shape.Type.Name}");
        if (shape.Kind == ValueKind.String)
        {
            try
            {
                StringCodec.Checked((string)value);
            }
            catch (RecordFormatException invalid)
            {
                return Fail(problems, path, invalid.Reason);
            }
        }
        return value;
    }

    /// <summary>A bulk field: its original object as it is.</summary>
    static object? Bulk(ValueShape shape, DraftValue node, RecordPath path, List<RecordProblem> problems)
    {
        if (node == DraftValue.Null)
            return shape.Kind == ValueKind.Nullable ? null : Fail(problems, path, NullNotAllowed);
        var value = Expect<DraftLeaf>(node, shape, path).Value;
        return shape.Type.IsInstanceOfType(value) ? value : throw new InvalidOperationException($"a {value.GetType().Name} where a {shape} belongs (at {path})");
    }

    static T Expect<T>(DraftValue node, ValueShape shape, RecordPath path) where T : DraftValue =>
        node as T ?? throw new InvalidOperationException($"a {node.GetType().Name} where a {shape} belongs (at {path})");

    static object Fail(List<RecordProblem> problems, RecordPath path, string message)
    {
        problems.Add(new(path, message));
        return Failed;
    }
}

/// <summary>Finding and replacing the draft at a path, the shapes known from the root down.</summary>
static class DraftPaths
{
    public static DraftValue At(RecordDraft root, RecordPath path)
    {
        DraftValue node = root;
        ValueShape? shape = null;
        foreach (var segment in path.Segments)
            (node, shape, _) = Step(node, shape, segment, path);
        return node;
    }

    /// <summary><paramref name="node"/> (of <paramref name="shape"/>; null at a record) with the draft at the rest of the path replaced.</summary>
    public static DraftValue With(DraftValue node, ValueShape? shape, RecordPath path, int depth, DraftValue value)
    {
        var segments = path.Segments;
        if (depth == segments.Count)
            return value;
        var (child, childShape, index) = Step(node, shape, segments[depth], path);
        var changed = With(child, childShape, path, depth + 1, value);
        return node switch
        {
            RecordDraft record => record.With(record.Shape.Fields[index], changed),
            DraftList list => list.With(index, changed),
            _ => ((DraftMap)node).With(index, ((DraftMap)node)[index] with { Value = changed }),
        };
    }

    /// <summary>One step down: the child's draft, its shape (null for a record's or a bulk field's) and its index among the node's fields, elements or entries.</summary>
    static (DraftValue Node, ValueShape? Shape, int Index) Step(DraftValue node, ValueShape? shape, RecordPathSegment segment, RecordPath path)
    {
        if (shape?.Kind == ValueKind.Nullable)
            shape = shape.Element;
        switch (segment.Kind)
        {
            case RecordPathKind.Field when node is RecordDraft record && record.Shape.Field(segment.Name!) is { } field:
                return (record.Fields[field.Index], field.Value, field.Index);
            case RecordPathKind.Index when node is DraftList list && segment.Index < list.Count:
                return (list[segment.Index], shape!.Element, segment.Index);
            case RecordPathKind.Key when node is DraftMap map:
                for (var i = 0; i < map.Count; i++)
                    if (shape!.Key!.TextOf(map[i].Key) == segment.Name)
                        return (map[i].Value, shape.Element, i);
                break;
        }
        throw new ArgumentException($"no draft at {path}: {segment} doesn't lead into {node}", nameof(path));
    }
}
