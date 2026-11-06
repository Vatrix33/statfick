using System.Globalization;
using System.Numerics;
using System.Reflection;

namespace Statventure.Records;

/// <summary>The kinds of value a record field holds (RecordCodecs.cs; the table in Docs/Editor.md, "What exists").</summary>
public enum ValueKind
{
    Bool,
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Float32,
    Float64,
    String,
    /// <summary>An enum: one of <see cref="ValueShape.Members"/>.</summary>
    Enum,
    /// <summary>A byte array.</summary>
    Bytes,
    Vector2,
    Vector3,
    Vector4,
    Quaternion,
    Matrix4x4,
    /// <summary>Null, or a value of <see cref="ValueShape.Element"/>.</summary>
    Nullable,
    /// <summary>Elements of <see cref="ValueShape.Element"/> (an array, a list or a list interface).</summary>
    List,
    /// <summary>A dictionary: keys of <see cref="ValueShape.Key"/> (strings, integers, enums), values of <see cref="ValueShape.Element"/>.</summary>
    Map,
    /// <summary>An ordered list of key-value pairs (as a map, but a list of <c>KeyValuePair</c>s).</summary>
    Pairs,
    /// <summary>A plain record of <see cref="ValueShape.Record"/>.</summary>
    Record,
    /// <summary>A union: a value of one of <see cref="ValueShape.Record"/>'s subtypes.</summary>
    Union,
}

/// <summary>An enum member: its code name, its JSON name (camelCase), its display name and its value.</summary>
public sealed record EnumMember(string Name, string JsonName, string DisplayName, object Value);

/// <summary>
/// The shape of a field's value (Docs/Editor.md, "Records: shapes, drafts and marks"), read from the
/// field's codec: its kind and CLR type, and what it is made of (a nullable's or list's element, a map's
/// key and value, a record's shape, an enum's members). Made with its field's shape.
/// </summary>
public sealed class ValueShape
{
    readonly Codec _codec;
    readonly Func<object, bool>? _isMember;
    readonly Func<object, string>? _jsonNameOf;
    Func<object, IReadOnlyList<object?>>? _readItems;
    Func<object?[], object>? _makeList;
    Func<object, IReadOnlyList<KeyValuePair<object, object?>>>? _readEntries;
    Func<IReadOnlyList<KeyValuePair<object, object?>>, object>? _makeMap;

    ValueShape(Codec codec, ValueKind kind)
    {
        _codec = codec;
        Kind = kind;
        Type = codec.Type;
        if (kind == ValueKind.Enum)
        {
            _isMember = codec.GetType().GetMethod("IsMember")!.CreateDelegate<Func<object, bool>>(codec);
            _jsonNameOf = codec.GetType().GetMethod("JsonNameOf")!.CreateDelegate<Func<object, string>>(codec);
            // In declaration order (a select lists them so), not by their numbers.
            Members = [.. Type.GetFields(BindingFlags.Public | BindingFlags.Static).Select(f =>
            {
                var value = f.GetValue(null)!;
                return new EnumMember(f.Name, _jsonNameOf(value), DisplayNames.Of(f.Name), value);
            })];
        }
    }

    public ValueKind Kind { get; }

    /// <summary>The CLR type of the values (<c>int</c>, <c>double?</c>, <c>string[]</c>, <c>IReadOnlyList&lt;Limb&gt;</c>).</summary>
    public Type Type { get; }

    /// <summary>A nullable's value, a list's elements, a map's or pairs' values; else null.</summary>
    public ValueShape? Element { get; private init; }

    /// <summary>A map's or pairs' keys; else null.</summary>
    public ValueShape? Key { get; private init; }

    /// <summary>A record's shape, or for a union the union's (its <see cref="RecordShape.Subtypes"/>); else null.</summary>
    public RecordShape? Record { get; private init; }

    /// <summary>An enum's members in declaration order; else none.</summary>
    public IReadOnlyList<EnumMember> Members { get; } = [];

    /// <summary>The kind as the layout signature writes it (<c>float64</c>, <c>list&lt;string&gt;?</c>, <c>audio.section</c>).</summary>
    public string Signature => _codec.Signature;

    /// <summary>Whether a value may hold records (in itself, its elements or its values).</summary>
    public bool HoldsRecords => _codec.HoldsRecords;

    public bool IsInteger => Kind is >= ValueKind.Int8 and <= ValueKind.UInt64;
    public bool IsFloat => Kind is ValueKind.Float32 or ValueKind.Float64;
    /// <summary>An integer or a float (not a bool).</summary>
    public bool IsNumber => IsInteger || IsFloat;

    public override string ToString() => Signature;

    /// <summary>The shape of the values a codec reads and writes.</summary>
    internal static ValueShape Of(Codec codec)
    {
        var type = codec.GetType();
        var generic = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
        var arguments = type.IsGenericType ? type.GetGenericArguments() : [];
        if (generic == typeof(NumberCodec<>))
            return new(codec, NumberKind(arguments[0]));
        if (generic == typeof(MathCodec<>))
            return new(codec, MathKind(arguments[0]));
        if (codec is StringCodec)
            return new(codec, ValueKind.String);
        if (codec is BytesCodec)
            return new(codec, ValueKind.Bytes);
        if (generic == typeof(EnumCodec<,>))
            return new(codec, ValueKind.Enum);
        if (generic == typeof(NullableValueCodec<>) || generic == typeof(NullableReferenceCodec<>))
            return new(codec, ValueKind.Nullable) { Element = Of(Part(codec, "Inner")) };
        if (generic == typeof(BlockListCodec<,>) || generic == typeof(ListCodec<,>))
        {
            var helper = Helper(nameof(ReadList), arguments);
            return new(codec, ValueKind.List)
            {
                Element = Of(Part(codec, "Element")),
                _readItems = helper.CreateDelegate<Func<object, IReadOnlyList<object?>>>(),
                _makeList = Helper(nameof(MakeList), arguments).CreateDelegate<Func<object?[], object>>(),
            };
        }
        if (generic == typeof(MapCodec<,,>))
        {
            var pairs = (bool)type.GetProperty("IsPairs")!.GetValue(codec)!;
            var make = Helper(nameof(MakeMap), arguments).CreateDelegate<Func<Codec, IReadOnlyList<KeyValuePair<object, object?>>, object>>();
            return new(codec, pairs ? ValueKind.Pairs : ValueKind.Map)
            {
                Key = Of(Part(codec, "KeyCodec")),
                Element = Of(Part(codec, "ValueCodec")),
                _readEntries = Helper(nameof(ReadMap), arguments).CreateDelegate<Func<object, IReadOnlyList<KeyValuePair<object, object?>>>>(),
                _makeMap = items => make(codec, items),
            };
        }
        if (generic == typeof(RecordCodec<>))
        {
            var family = (RecordFamily)type.GetProperty("Family")!.GetValue(codec)!;
            var shape = RecordShape.Of(family.CurrentType);
            return new(codec, shape.IsUnion ? ValueKind.Union : ValueKind.Record) { Record = shape };
        }
        throw new InvalidOperationException($"no value shape for the codec {type.Name}");
    }

    static Codec Part(Codec codec, string name) => (Codec)codec.GetType().GetProperty(name)!.GetValue(codec)!;

    static MethodInfo Helper(string name, Type[] arguments) =>
        typeof(ValueShape).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(arguments);

    static ValueKind NumberKind(Type type) =>
        type == typeof(bool) ? ValueKind.Bool :
        type == typeof(sbyte) ? ValueKind.Int8 :
        type == typeof(byte) ? ValueKind.UInt8 :
        type == typeof(short) ? ValueKind.Int16 :
        type == typeof(ushort) ? ValueKind.UInt16 :
        type == typeof(int) ? ValueKind.Int32 :
        type == typeof(uint) ? ValueKind.UInt32 :
        type == typeof(long) ? ValueKind.Int64 :
        type == typeof(ulong) ? ValueKind.UInt64 :
        type == typeof(float) ? ValueKind.Float32 :
        ValueKind.Float64;

    static ValueKind MathKind(Type type) =>
        type == typeof(Vector2) ? ValueKind.Vector2 :
        type == typeof(Vector3) ? ValueKind.Vector3 :
        type == typeof(Vector4) ? ValueKind.Vector4 :
        type == typeof(Quaternion) ? ValueKind.Quaternion :
        ValueKind.Matrix4x4;

    // Lists and maps to and from boxed values, through the serializer's own list shapes.

    static IReadOnlyList<object?> ReadList<TList, TElement>(object list)
    {
        var items = ListShape<TList, TElement>.Items((TList)list);
        var boxed = new object?[items.Length];
        for (var i = 0; i < items.Length; i++)
            boxed[i] = items[i];
        return boxed;
    }

    static object MakeList<TList, TElement>(object?[] items)
    {
        var array = new TElement[items.Length];
        for (var i = 0; i < items.Length; i++)
            array[i] = (TElement)items[i]!;
        return ListShape<TList, TElement>.FromArray(array)!;
    }

    static IReadOnlyList<KeyValuePair<object, object?>> ReadMap<TMap, TKey, TValue>(object map) =>
        [.. ((IEnumerable<KeyValuePair<TKey, TValue>>)map).Select(p => new KeyValuePair<object, object?>(p.Key!, p.Value))];

    static object MakeMap<TMap, TKey, TValue>(Codec codec, IReadOnlyList<KeyValuePair<object, object?>> items) where TKey : notnull =>
        ((MapCodec<TMap, TKey, TValue>)codec).Make([.. items.Select(p => new KeyValuePair<TKey, TValue>((TKey)p.Key, (TValue)p.Value!))])!;

    /// <summary>A list's elements, boxed.</summary>
    internal IReadOnlyList<object?> ItemsOf(object list) => _readItems!(list);

    /// <summary>A list of this shape holding <paramref name="items"/> (each of the element's type, nulls only where it allows them).</summary>
    internal object MakeList(object?[] items) => _makeList!(items);

    /// <summary>A map's or pairs' entries in order, boxed.</summary>
    internal IReadOnlyList<KeyValuePair<object, object?>> EntriesOf(object map) => _readEntries!(map);

    /// <summary>A map or pairs of this shape holding <paramref name="items"/> in order (no key twice).</summary>
    internal object MakeMap(IReadOnlyList<KeyValuePair<object, object?>> items) => _makeMap!(items);

    /// <summary>Whether an enum value is one of the members.</summary>
    internal bool IsMember(object value) => _isMember!(value);

    /// <summary>
    /// A value as text: a map key as its path segment and the JSON form write it (a string as it is, an
    /// integer in decimal, an enum member by its JSON name; a number that isn't a member in decimal, as an
    /// enum writes itself).
    /// </summary>
    internal string TextOf(object value) => Kind switch
    {
        ValueKind.String => (string)value,
        ValueKind.Enum when IsMember(value) => _jsonNameOf!(value),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture)!,
    };
}
