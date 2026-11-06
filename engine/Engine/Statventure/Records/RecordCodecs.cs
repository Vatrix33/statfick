using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace Statventure.Records;

// The codecs: one per kind of field, each with its binary form, its JSON form and its part of the
// layout signature (Docs/Assets.md, "Records (the serializer)" and "Format details", Q11 and the
// answers of 2026-11-03).

/// <summary>Reads and writes one kind of value in both forms.</summary>
abstract class Codec
{
    public abstract Type Type { get; }
    /// <summary>The value's kind in a layout signature (<c>float64</c>, <c>list&lt;string&gt;?</c>, <c>audio.section</c>).</summary>
    public abstract string Signature { get; }
    /// <summary>Whether a value may hold records (the visit before a write skips what can't).</summary>
    public virtual bool HoldsRecords => false;
    /// <summary>The record types this kind refers to, itself or through its elements.</summary>
    public virtual IEnumerable<RecordFamily> Families => [];
    /// <summary>Whether null is a value (a nullable field).</summary>
    public virtual bool AcceptsNull => false;
    /// <summary>Whether a value can take no bytes at all in the data's version (a record without fields).</summary>
    public virtual bool CanBeEmpty(ReadSession session) => false;

    public abstract void WriteObject(BinaryRecordWriter writer, object? value);
    public abstract object? ReadObject(ref BinaryRecordReader reader);
    public abstract void WriteJsonObject(JsonRecordWriter writer, object? value);
    public abstract object? ReadJsonObject(JsonRecordReader reader, JsonElement element);
    public abstract void VisitObject(WriteSession session, object? value);

    protected static RecordFormatException Null() => new("null in a field that can't be null");

    protected static RecordFormatException Expected(string what, JsonElement element) =>
        new($"expected {what}, found {Describe(element)}");

    static string Describe(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => "an object",
        JsonValueKind.Array => "an array",
        JsonValueKind.String => $"the string {Shorten(element.GetRawText())}",
        JsonValueKind.Number => $"the number {Shorten(element.GetRawText())}",
        JsonValueKind.True or JsonValueKind.False => element.GetRawText(),
        _ => "null",
    };

    static string Shorten(string text) => text.Length <= 40 ? text : text[..37] + "...";
}

/// <summary>A codec of values of <typeparamref name="T"/>; the typed methods refuse a null where the kind has none.</summary>
abstract class Codec<T> : Codec
{
    public override Type Type => typeof(T);

    protected abstract void WriteValue(BinaryRecordWriter writer, T value);
    protected abstract T ReadValue(ref BinaryRecordReader reader);
    protected abstract void WriteJsonValue(JsonRecordWriter writer, T value);
    protected abstract T ReadJsonValue(JsonRecordReader reader, JsonElement element);
    protected virtual void VisitValue(WriteSession session, T value) { }

    public void Write(BinaryRecordWriter writer, T value)
    {
        if (value is null && !AcceptsNull)
            throw Null();
        WriteValue(writer, value);
    }

    public T Read(ref BinaryRecordReader reader) => ReadValue(ref reader);

    public void WriteJson(JsonRecordWriter writer, T value)
    {
        if (value is null && !AcceptsNull)
            throw Null();
        WriteJsonValue(writer, value);
    }

    public T ReadJson(JsonRecordReader reader, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Null && !AcceptsNull)
            throw Null();
        return ReadJsonValue(reader, element);
    }

    public void Visit(WriteSession session, T value)
    {
        if (value is null && !AcceptsNull)
            throw Null();
        VisitValue(session, value);
    }

    public override void WriteObject(BinaryRecordWriter writer, object? value) => Write(writer, (T)value!);
    public override object? ReadObject(ref BinaryRecordReader reader) => Read(ref reader);
    public override void WriteJsonObject(JsonRecordWriter writer, object? value) => WriteJson(writer, (T)value!);
    public override object? ReadJsonObject(JsonRecordReader reader, JsonElement element) => ReadJson(reader, element);
    public override void VisitObject(WriteSession session, object? value) => Visit(session, (T)value!);
}

/// <summary>A kind that can be a dictionary's key: in JSON it is the property name's text.</summary>
interface IKeyCodec<T>
{
    string KeyText(T key);
    T ParseKey(string text);
}

/// <summary>Booleans and fixed-width numbers: little-endian, floats bit for bit; in JSON shortest round-trip numbers, NaN and the infinities as strings.</summary>
sealed class NumberCodec<T> : Codec<T>, IKeyCodec<T> where T : unmanaged
{
    public static readonly string Name =
        typeof(T) == typeof(bool) ? "bool" :
        typeof(T) == typeof(sbyte) ? "int8" :
        typeof(T) == typeof(byte) ? "uint8" :
        typeof(T) == typeof(short) ? "int16" :
        typeof(T) == typeof(ushort) ? "uint16" :
        typeof(T) == typeof(int) ? "int32" :
        typeof(T) == typeof(uint) ? "uint32" :
        typeof(T) == typeof(long) ? "int64" :
        typeof(T) == typeof(ulong) ? "uint64" :
        typeof(T) == typeof(float) ? "float32" :
        typeof(T) == typeof(double) ? "float64" :
        throw new InvalidOperationException($"{typeof(T).Name} isn't a number kind");

    public static readonly NumberCodec<T> Instance = new();

    public override string Signature => Name;

    static TTo As<TTo>(T value) where TTo : unmanaged => Unsafe.As<T, TTo>(ref value);
    static T From<TFrom>(TFrom value) where TFrom : unmanaged => Unsafe.As<TFrom, T>(ref value);

    protected override void WriteValue(BinaryRecordWriter writer, T value)
    {
        if (typeof(T) == typeof(bool)) writer.WriteBool(As<bool>(value));
        else if (typeof(T) == typeof(sbyte)) writer.WriteByte((byte)As<sbyte>(value));
        else if (typeof(T) == typeof(byte)) writer.WriteByte(As<byte>(value));
        else if (typeof(T) == typeof(short)) writer.WriteInt16(As<short>(value));
        else if (typeof(T) == typeof(ushort)) writer.WriteUInt16(As<ushort>(value));
        else if (typeof(T) == typeof(int)) writer.WriteInt32(As<int>(value));
        else if (typeof(T) == typeof(uint)) writer.WriteUInt32(As<uint>(value));
        else if (typeof(T) == typeof(long)) writer.WriteInt64(As<long>(value));
        else if (typeof(T) == typeof(ulong)) writer.WriteUInt64(As<ulong>(value));
        else if (typeof(T) == typeof(float)) writer.WriteSingle(As<float>(value));
        else writer.WriteDouble(As<double>(value));
    }

    protected override T ReadValue(ref BinaryRecordReader reader)
    {
        if (typeof(T) == typeof(bool)) return From(reader.ReadBool());
        if (typeof(T) == typeof(sbyte)) return From((sbyte)reader.ReadByte());
        if (typeof(T) == typeof(byte)) return From(reader.ReadByte());
        if (typeof(T) == typeof(short)) return From(reader.ReadInt16());
        if (typeof(T) == typeof(ushort)) return From(reader.ReadUInt16());
        if (typeof(T) == typeof(int)) return From(reader.ReadInt32());
        if (typeof(T) == typeof(uint)) return From(reader.ReadUInt32());
        if (typeof(T) == typeof(long)) return From(reader.ReadInt64());
        if (typeof(T) == typeof(ulong)) return From(reader.ReadUInt64());
        if (typeof(T) == typeof(float)) return From(reader.ReadSingle());
        return From(reader.ReadDouble());
    }

    protected override void WriteJsonValue(JsonRecordWriter writer, T value)
    {
        var json = writer.Json;
        if (typeof(T) == typeof(bool)) json.WriteBooleanValue(As<bool>(value));
        else if (typeof(T) == typeof(float))
        {
            var f = As<float>(value);
            if (float.IsFinite(f)) json.WriteNumberValue(f);
            else json.WriteStringValue(NonFinite(f));
        }
        else if (typeof(T) == typeof(double))
        {
            var d = As<double>(value);
            if (double.IsFinite(d)) json.WriteNumberValue(d);
            else json.WriteStringValue(NonFinite(d));
        }
        else if (typeof(T) == typeof(ulong)) json.WriteNumberValue(As<ulong>(value));
        else json.WriteNumberValue(ToInt64(value));
    }

    static long ToInt64(T value) =>
        typeof(T) == typeof(sbyte) ? As<sbyte>(value) :
        typeof(T) == typeof(byte) ? As<byte>(value) :
        typeof(T) == typeof(short) ? As<short>(value) :
        typeof(T) == typeof(ushort) ? As<ushort>(value) :
        typeof(T) == typeof(int) ? As<int>(value) :
        typeof(T) == typeof(uint) ? As<uint>(value) :
        As<long>(value);

    static string NonFinite(double value) => double.IsNaN(value) ? "NaN" : value > 0 ? "Infinity" : "-Infinity";

    protected override T ReadJsonValue(JsonRecordReader reader, JsonElement element) => ReadJsonNumber(element);

    public static T ReadJsonNumber(JsonElement element)
    {
        if (typeof(T) == typeof(bool))
            return element.ValueKind switch
            {
                JsonValueKind.True => From(true),
                JsonValueKind.False => From(false),
                _ => throw Expected("true or false", element),
            };
        if (typeof(T) == typeof(float) || typeof(T) == typeof(double))
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                double special = element.GetString() switch
                {
                    "NaN" => double.NaN,
                    "Infinity" => double.PositiveInfinity,
                    "-Infinity" => double.NegativeInfinity,
                    _ => throw Expected($"a {Name} (a number, \"NaN\", \"Infinity\" or \"-Infinity\")", element),
                };
                return typeof(T) == typeof(float) ? From((float)special) : From(special);
            }
            if (element.ValueKind == JsonValueKind.Number)
            {
                // A finite number too large for the type is refused, not turned into an infinity.
                if (typeof(T) == typeof(float) && element.TryGetSingle(out var f) && float.IsFinite(f))
                    return From(f);
                if (typeof(T) == typeof(double) && element.TryGetDouble(out var d) && double.IsFinite(d))
                    return From(d);
            }
            throw Expected($"a {Name}", element);
        }
        if (element.ValueKind == JsonValueKind.Number)
        {
            if (typeof(T) == typeof(sbyte) && element.TryGetSByte(out var i8)) return From(i8);
            if (typeof(T) == typeof(byte) && element.TryGetByte(out var u8)) return From(u8);
            if (typeof(T) == typeof(short) && element.TryGetInt16(out var i16)) return From(i16);
            if (typeof(T) == typeof(ushort) && element.TryGetUInt16(out var u16)) return From(u16);
            if (typeof(T) == typeof(int) && element.TryGetInt32(out var i32)) return From(i32);
            if (typeof(T) == typeof(uint) && element.TryGetUInt32(out var u32)) return From(u32);
            if (typeof(T) == typeof(long) && element.TryGetInt64(out var i64)) return From(i64);
            if (typeof(T) == typeof(ulong) && element.TryGetUInt64(out var u64)) return From(u64);
        }
        throw Expected($"{(Name[0] == 'i' ? "an" : "a")} {Name}", element);
    }

    public string KeyText(T key) =>
        typeof(T) == typeof(ulong) ? As<ulong>(key).ToString(CultureInfo.InvariantCulture) :
        typeof(T) == typeof(bool) || typeof(T) == typeof(float) || typeof(T) == typeof(double) ? throw new InvalidOperationException($"{Name} can't be a key") :
        ToInt64(key).ToString(CultureInfo.InvariantCulture);

    public T ParseKey(string text)
    {
        // Integers only, in the one form KeyText gives (no sign on 0, no leading zeros, no spaces).
        using var document = JsonDocument.Parse(IsCanonicalInteger(text) ? text : "null");
        try
        {
            return ReadJsonNumber(document.RootElement);
        }
        catch (RecordFormatException)
        {
            throw new RecordFormatException($"the key \"{text}\" isn't an {Name}");
        }
    }

    static bool IsCanonicalInteger(string text)
    {
        var digits = text.StartsWith('-') ? text[1..] : text;
        return digits.Length > 0 && digits.Length <= 20 && digits.All(char.IsAsciiDigit)
            && (digits.Length == 1 || digits[0] != '0') && text != "-0";
    }

    public static bool IsNumberType(Type type) =>
        type == typeof(bool) || type == typeof(sbyte) || type == typeof(byte) || type == typeof(short) || type == typeof(ushort)
        || type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong)
        || type == typeof(float) || type == typeof(double);
}

/// <summary>System.Numerics' vectors, quaternion and matrix: their floats in memory order (X, Y, Z, W; M11, M12 … M44); in JSON an array of numbers on one line.</summary>
sealed class MathCodec<T> : Codec<T> where T : unmanaged
{
    public static readonly string Name =
        typeof(T) == typeof(Vector2) ? "vector2" :
        typeof(T) == typeof(Vector3) ? "vector3" :
        typeof(T) == typeof(Vector4) ? "vector4" :
        typeof(T) == typeof(Quaternion) ? "quaternion" :
        typeof(T) == typeof(Matrix4x4) ? "matrix4x4" :
        throw new InvalidOperationException($"{typeof(T).Name} isn't a vector kind");

    public static readonly MathCodec<T> Instance = new();
    static readonly int Count = Unsafe.SizeOf<T>() / sizeof(float);

    public static bool IsMathType(Type type) =>
        type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4) || type == typeof(Quaternion) || type == typeof(Matrix4x4);

    public override string Signature => Name;

    static Span<float> Floats(ref T value) => MemoryMarshal.Cast<T, float>(MemoryMarshal.CreateSpan(ref value, 1));

    protected override void WriteValue(BinaryRecordWriter writer, T value)
    {
        foreach (var f in Floats(ref value))
            writer.WriteSingle(f);
    }

    protected override T ReadValue(ref BinaryRecordReader reader)
    {
        T value = default;
        var floats = Floats(ref value);
        for (var i = 0; i < floats.Length; i++)
            floats[i] = reader.ReadSingle();
        return value;
    }

    protected override void WriteJsonValue(JsonRecordWriter writer, T value)
    {
        var text = new StringBuilder("[");
        foreach (var f in Floats(ref value))
        {
            if (text.Length > 1)
                text.Append(", ");
            text.Append(float.IsNaN(f) ? "\"NaN\"" : float.IsPositiveInfinity(f) ? "\"Infinity\"" : float.IsNegativeInfinity(f) ? "\"-Infinity\""
                : f.ToString(CultureInfo.InvariantCulture));
        }
        writer.Json.WriteRawValue(text.Append(']').ToString());
    }

    protected override T ReadJsonValue(JsonRecordReader reader, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() != Count)
            throw Expected($"a {Name} (an array of {Count} numbers)", element);
        T value = default;
        var floats = Floats(ref value);
        var i = 0;
        foreach (var item in element.EnumerateArray())
        {
            try
            {
                floats[i] = NumberCodec<float>.ReadJsonNumber(item);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{i}]");
            }
            i++;
        }
        return value;
    }
}

/// <summary>A string: UTF-8 in binary (a lone surrogate can't be written), a JSON string.</summary>
sealed class StringCodec : Codec<string>, IKeyCodec<string>
{
    public static readonly StringCodec Instance = new();
    static readonly UTF8Encoding Strict = new(false, true);

    public override string Signature => "string";

    protected override void WriteValue(BinaryRecordWriter writer, string value) => writer.WriteString(value);
    protected override string ReadValue(ref BinaryRecordReader reader) => reader.ReadString();

    protected override void WriteJsonValue(JsonRecordWriter writer, string value) => writer.Json.WriteStringValue(Checked(value));

    /// <summary>The JSON writer would turn a lone surrogate into U+FFFD silently.</summary>
    public static string Checked(string value)
    {
        try
        {
            Strict.GetByteCount(value);
        }
        catch (EncoderFallbackException)
        {
            throw new RecordFormatException("the string isn't valid UTF-16 (a lone surrogate)");
        }
        return value;
    }

    protected override string ReadJsonValue(JsonRecordReader reader, JsonElement element) => ReadString(element);

    public static string ReadString(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
            throw Expected("a string", element);
        try
        {
            return element.GetString()!;
        }
        catch (InvalidOperationException)
        {
            throw new RecordFormatException("the string isn't valid UTF-16 (a lone surrogate)");
        }
    }

    public string KeyText(string key) => Checked(key);
    public string ParseKey(string text) => text;
}

/// <summary>A byte array: its count and bytes in binary, base64 in JSON.</summary>
sealed class BytesCodec : Codec<byte[]>
{
    public static readonly BytesCodec Instance = new();

    public override string Signature => "bytes";

    protected override void WriteValue(BinaryRecordWriter writer, byte[] value) => writer.WriteBlock<byte>(value);
    protected override byte[] ReadValue(ref BinaryRecordReader reader) => reader.ReadBlock<byte>();
    protected override void WriteJsonValue(JsonRecordWriter writer, byte[] value) => writer.Json.WriteBase64StringValue(value);

    protected override byte[] ReadJsonValue(JsonRecordReader reader, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String || !element.TryGetBytesFromBase64(out var bytes))
            throw Expected("bytes in base64", element);
        return bytes;
    }
}

/// <summary>An enum: its number at the underlying type's width in binary, the member's camelCase name in JSON; a number or name that isn't a member is refused.</summary>
sealed class EnumCodec<TEnum, TNumber> : Codec<TEnum>, IKeyCodec<TEnum> where TEnum : struct, Enum where TNumber : unmanaged
{
    readonly Dictionary<TEnum, string> _names = [];
    readonly Dictionary<string, TEnum> _members = [];

    public EnumCodec()
    {
        if (typeof(TEnum).IsDefined(typeof(FlagsAttribute), false))
            throw new InvalidOperationException($"{typeof(TEnum).Name}: flags enums aren't supported in records");
        var members = new List<string>();
        foreach (var value in Enum.GetValues<TEnum>())
        {
            var name = System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(Enum.GetName(value)!);
            if (!_names.TryAdd(value, name))
                throw new InvalidOperationException($"{typeof(TEnum).Name}: two members have the value {value:D}");
            if (!_members.TryAdd(name, value))
                throw new InvalidOperationException($"{typeof(TEnum).Name}: two members are named {name}");
            members.Add($"{name} = {value:D}");
        }
        Signature = $"enum {NumberCodec<TNumber>.Name} {{{string.Join(", ", members)}}}";
    }

    public override string Signature { get; }

    // What the codec holds, for the record shapes (Records/ValueShape.cs): its members and their JSON names.

    /// <summary>Whether <paramref name="value"/> (a <typeparamref name="TEnum"/>) is a member.</summary>
    public bool IsMember(object value) => _names.ContainsKey((TEnum)value);

    /// <summary>A member's JSON name (its camelCase name).</summary>
    public string JsonNameOf(object value) => NameOf((TEnum)value);

    protected override void WriteValue(BinaryRecordWriter writer, TEnum value)
    {
        NameOf(value);
        NumberCodec<TNumber>.Instance.Write(writer, Unsafe.As<TEnum, TNumber>(ref value));
    }

    protected override TEnum ReadValue(ref BinaryRecordReader reader)
    {
        var number = NumberCodec<TNumber>.Instance.Read(ref reader);
        var value = Unsafe.As<TNumber, TEnum>(ref number);
        if (!_names.ContainsKey(value))
            throw new RecordFormatException($"{value:D} isn't a member of {typeof(TEnum).Name}");
        return value;
    }

    string NameOf(TEnum value) =>
        _names.TryGetValue(value, out var name) ? name : throw new RecordFormatException($"{value:D} isn't a member of {typeof(TEnum).Name}");

    protected override void WriteJsonValue(JsonRecordWriter writer, TEnum value) => writer.Json.WriteStringValue(NameOf(value));

    protected override TEnum ReadJsonValue(JsonRecordReader reader, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String && _members.TryGetValue(element.GetString()!, out var value))
            return value;
        throw Expected($"a member of {typeof(TEnum).Name} ({string.Join(", ", _members.Keys)})", element);
    }

    public string KeyText(TEnum key) => NameOf(key);

    public TEnum ParseKey(string text) =>
        _members.TryGetValue(text, out var value) ? value : throw new RecordFormatException($"the key \"{text}\" isn't a member of {typeof(TEnum).Name}");
}

/// <summary>A value type that may be null: a presence byte, then the value; null or absent in JSON.</summary>
sealed class NullableValueCodec<T>(Codec<T> inner) : Codec<T?> where T : struct
{
    /// <summary>The codec of the value when it isn't null (for the record shapes).</summary>
    public Codec Inner => inner;
    public override string Signature => inner.Signature + "?";
    public override bool AcceptsNull => true;
    public override bool HoldsRecords => inner.HoldsRecords;
    public override IEnumerable<RecordFamily> Families => inner.Families;

    protected override void WriteValue(BinaryRecordWriter writer, T? value)
    {
        writer.WriteBool(value.HasValue);
        if (value.HasValue)
            inner.Write(writer, value.Value);
    }

    protected override T? ReadValue(ref BinaryRecordReader reader) => reader.ReadPresence() ? inner.Read(ref reader) : null;

    protected override void WriteJsonValue(JsonRecordWriter writer, T? value)
    {
        if (value.HasValue)
            inner.WriteJson(writer, value.Value);
        else
            writer.Json.WriteNullValue();
    }

    protected override T? ReadJsonValue(JsonRecordReader reader, JsonElement element) =>
        element.ValueKind == JsonValueKind.Null ? null : inner.ReadJson(reader, element);

    protected override void VisitValue(WriteSession session, T? value)
    {
        if (value.HasValue)
            inner.Visit(session, value.Value);
    }
}

/// <summary>A reference that may be null (a nullable annotation): a presence byte, then the value; null or absent in JSON.</summary>
sealed class NullableReferenceCodec<T>(Codec<T> inner) : Codec<T> where T : class
{
    /// <summary>The codec of the value when it isn't null (for the record shapes).</summary>
    public Codec Inner => inner;
    public override string Signature => inner.Signature + "?";
    public override bool AcceptsNull => true;
    public override bool HoldsRecords => inner.HoldsRecords;
    public override IEnumerable<RecordFamily> Families => inner.Families;

    protected override void WriteValue(BinaryRecordWriter writer, T value)
    {
        writer.WriteBool(value != null);
        if (value != null)
            inner.Write(writer, value);
    }

    protected override T ReadValue(ref BinaryRecordReader reader) => reader.ReadPresence() ? inner.Read(ref reader) : null!;

    protected override void WriteJsonValue(JsonRecordWriter writer, T value)
    {
        if (value != null)
            inner.WriteJson(writer, value);
        else
            writer.Json.WriteNullValue();
    }

    protected override T ReadJsonValue(JsonRecordReader reader, JsonElement element) =>
        element.ValueKind == JsonValueKind.Null ? null! : inner.ReadJson(reader, element);

    protected override void VisitValue(WriteSession session, T value)
    {
        if (value != null)
            inner.Visit(session, value);
    }
}

/// <summary>How a list-like field's type is made from and read as an array (T[], List&lt;T&gt;, IReadOnlyList&lt;T&gt; and the like share one form).</summary>
static class ListShape<TList, TElement>
{
    public static TList FromArray(TElement[] items) =>
        typeof(TList) == typeof(List<TElement>) ? (TList)(object)new List<TElement>(items) : (TList)(object)items;

    public static ReadOnlySpan<TElement> Items(TList list) => list switch
    {
        TElement[] array => array,
        List<TElement> items => CollectionsMarshal.AsSpan(items),
        IEnumerable<TElement> other => other.ToArray(),
        _ => throw new InvalidOperationException($"{typeof(TList).Name} isn't a list"),
    };

    public static bool Fits(Type type) =>
        type == typeof(TElement[]) || type == typeof(List<TElement>) || type.IsAssignableFrom(typeof(TElement[]));
}

/// <summary>A list of numbers or vectors: its count, then the elements as one block.</summary>
sealed class BlockListCodec<TList, TElement>(Codec<TElement> element) : Codec<TList> where TElement : unmanaged
{
    /// <summary>The elements' codec (for the record shapes).</summary>
    public Codec Element => element;
    public override string Signature => $"list<{element.Signature}>";

    protected override void WriteValue(BinaryRecordWriter writer, TList value) => writer.WriteBlock(ListShape<TList, TElement>.Items(value));
    protected override TList ReadValue(ref BinaryRecordReader reader) => ListShape<TList, TElement>.FromArray(reader.ReadBlock<TElement>());

    protected override void WriteJsonValue(JsonRecordWriter writer, TList value)
    {
        writer.Json.WriteStartArray();
        foreach (var item in ListShape<TList, TElement>.Items(value))
            element.WriteJson(writer, item);
        writer.Json.WriteEndArray();
    }

    protected override TList ReadJsonValue(JsonRecordReader reader, JsonElement json) =>
        ListShape<TList, TElement>.FromArray(ListCodec<TList, TElement>.ReadItems(reader, json, element));
}

/// <summary>A list: its count, then each element.</summary>
sealed class ListCodec<TList, TElement>(Codec<TElement> element) : Codec<TList>
{
    /// <summary>The most elements a list of values that may take no bytes can claim.</summary>
    const int MaxEmptyElements = 1 << 16;

    /// <summary>The elements' codec (for the record shapes).</summary>
    public Codec Element => element;
    public override string Signature => $"list<{element.Signature}>";
    public override bool HoldsRecords => element.HoldsRecords;
    public override IEnumerable<RecordFamily> Families => element.Families;

    protected override void WriteValue(BinaryRecordWriter writer, TList value)
    {
        var items = ListShape<TList, TElement>.Items(value);
        writer.WriteCount(items.Length);
        for (var i = 0; i < items.Length; i++)
        {
            try
            {
                element.Write(writer, items[i]);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{i}]");
            }
        }
    }

    protected override TList ReadValue(ref BinaryRecordReader reader)
    {
        int count;
        if (element.CanBeEmpty(reader.Session))
        {
            count = reader.ReadVarInt32("a count");
            if (count > MaxEmptyElements)
                throw new RecordFormatException($"a list of {count} values without fields is too long");
        }
        else
            count = reader.ReadCount();
        var items = new TElement[count];
        for (var i = 0; i < count; i++)
        {
            try
            {
                items[i] = element.Read(ref reader);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{i}]");
            }
        }
        return ListShape<TList, TElement>.FromArray(items);
    }

    protected override void WriteJsonValue(JsonRecordWriter writer, TList value)
    {
        var items = ListShape<TList, TElement>.Items(value);
        writer.Json.WriteStartArray();
        for (var i = 0; i < items.Length; i++)
        {
            try
            {
                element.WriteJson(writer, items[i]);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{i}]");
            }
        }
        writer.Json.WriteEndArray();
    }

    protected override TList ReadJsonValue(JsonRecordReader reader, JsonElement json) =>
        ListShape<TList, TElement>.FromArray(ReadItems(reader, json, element));

    public static TElement[] ReadItems(JsonRecordReader reader, JsonElement json, Codec<TElement> element)
    {
        if (json.ValueKind != JsonValueKind.Array)
            throw Expected("an array", json);
        var items = new TElement[json.GetArrayLength()];
        var i = 0;
        foreach (var item in json.EnumerateArray())
        {
            try
            {
                items[i] = element.ReadJson(reader, item);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{i}]");
            }
            i++;
        }
        return items;
    }

    protected override void VisitValue(WriteSession session, TList value)
    {
        var items = ListShape<TList, TElement>.Items(value);
        for (var i = 0; i < items.Length; i++)
        {
            try
            {
                element.Visit(session, items[i]);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{i}]");
            }
        }
    }
}

/// <summary>
/// A dictionary, or an ordered list of key-value pairs (the same form): its count, then each key and
/// value, in enumeration order; in JSON an object with the keys in that order. A repeated key is
/// refused in both forms.
/// </summary>
sealed class MapCodec<TMap, TKey, TValue>(Codec<TKey> key, Codec<TValue> value, bool pairs) : Codec<TMap> where TKey : notnull
{
    readonly IKeyCodec<TKey> _keys = (IKeyCodec<TKey>)key;

    // What the codec holds, for the record shapes (Records/ValueShape.cs).
    public Codec KeyCodec => key;
    public Codec ValueCodec => value;
    /// <summary>An ordered list of pairs rather than a dictionary.</summary>
    public bool IsPairs => pairs;

    public override string Signature => $"map<{key.Signature}, {value.Signature}>";
    public override bool HoldsRecords => value.HoldsRecords;
    public override IEnumerable<RecordFamily> Families => value.Families;

    IEnumerable<KeyValuePair<TKey, TValue>> Pairs(TMap map)
    {
        var seen = pairs ? new HashSet<TKey>() : null;
        foreach (var pair in (IEnumerable<KeyValuePair<TKey, TValue>>)map!)
        {
            if (seen != null && !seen.Add(pair.Key))
                throw new RecordFormatException($"the key {pair.Key} is there twice");
            yield return pair;
        }
    }

    /// <summary>The field's value from its pairs in order (no key twice); also for the record drafts.</summary>
    public TMap Make(List<KeyValuePair<TKey, TValue>> items)
    {
        if (pairs)
            return ListShape<TMap, KeyValuePair<TKey, TValue>>.FromArray([.. items]);
        if (typeof(TMap) == typeof(OrderedDictionary<TKey, TValue>))
        {
            var ordered = new OrderedDictionary<TKey, TValue>(items.Count);
            foreach (var (k, v) in items)
                ordered.Add(k, v);
            return (TMap)(object)ordered;
        }
        var dictionary = new Dictionary<TKey, TValue>(items.Count);
        foreach (var (k, v) in items)
            dictionary.Add(k, v);
        return (TMap)(object)dictionary;
    }

    static void Add(List<KeyValuePair<TKey, TValue>> items, HashSet<TKey> seen, TKey k, TValue v)
    {
        if (!seen.Add(k))
            throw new RecordFormatException($"the key {k} is there twice");
        items.Add(new(k, v));
    }

    protected override void WriteValue(BinaryRecordWriter writer, TMap map)
    {
        var items = Pairs(map).ToList();
        writer.WriteCount(items.Count);
        foreach (var (k, v) in items)
        {
            key.Write(writer, k);
            try
            {
                value.Write(writer, v);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{Quote(k)}]");
            }
        }
    }

    protected override TMap ReadValue(ref BinaryRecordReader reader)
    {
        var count = reader.ReadCount();
        var items = new List<KeyValuePair<TKey, TValue>>(count);
        var seen = new HashSet<TKey>(count);
        for (var i = 0; i < count; i++)
        {
            var k = key.Read(ref reader);
            TValue v;
            try
            {
                v = value.Read(ref reader);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{Quote(k)}]");
            }
            Add(items, seen, k, v);
        }
        return Make(items);
    }

    string Quote(TKey k) => JsonSerializer.Serialize(_keys.KeyText(k));

    protected override void WriteJsonValue(JsonRecordWriter writer, TMap map)
    {
        writer.Json.WriteStartObject();
        foreach (var (k, v) in Pairs(map))
        {
            writer.Json.WritePropertyName(_keys.KeyText(k));
            try
            {
                value.WriteJson(writer, v);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{Quote(k)}]");
            }
        }
        writer.Json.WriteEndObject();
    }

    protected override TMap ReadJsonValue(JsonRecordReader reader, JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Object)
            throw Expected("an object", json);
        var items = new List<KeyValuePair<TKey, TValue>>();
        var seen = new HashSet<TKey>();
        foreach (var property in json.EnumerateObject())
        {
            var k = _keys.ParseKey(property.Name);
            TValue v;
            try
            {
                v = value.ReadJson(reader, property.Value);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{JsonSerializer.Serialize(property.Name)}]");
            }
            Add(items, seen, k, v);
        }
        return Make(items);
    }

    protected override void VisitValue(WriteSession session, TMap map)
    {
        foreach (var (k, v) in Pairs(map))
        {
            try
            {
                value.Visit(session, v);
            }
            catch (RecordFormatException e)
            {
                throw e.Within($"[{Quote(k)}]");
            }
        }
    }
}

/// <summary>A record (or union) of a type, read in the version the data's table gives and upgraded to the current one.</summary>
sealed class RecordCodec<T>(RecordFamily family) : Codec<T> where T : notnull
{
    /// <summary>The record type (for the record shapes).</summary>
    public RecordFamily Family => family;
    public override string Signature => family.Name;
    public override bool HoldsRecords => true;
    public override IEnumerable<RecordFamily> Families => [family];
    // A type the table doesn't list has no values here: a list of it can only be empty.
    public override bool CanBeEmpty(ReadSession session) => session.PeekVersion(family) is { } version && family.Layout(version).IsEmpty;

    protected override void WriteValue(BinaryRecordWriter writer, T value) => family.Write(writer, value);
    protected override T ReadValue(ref BinaryRecordReader reader) => (T)family.Read(ref reader);
    protected override void WriteJsonValue(JsonRecordWriter writer, T value) => family.WriteJson(writer, value);
    protected override T ReadJsonValue(JsonRecordReader reader, JsonElement element) => (T)family.ReadJson(reader, element);
    protected override void VisitValue(WriteSession session, T value) => family.Visit(session, value);
}
