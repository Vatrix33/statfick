using System.Buffers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Statventure.Records;

/// <summary>
/// Records in their two forms (Docs/Assets.md, "Records (the serializer)"): the binary form of pack
/// entries and saves, and the JSON form of character files and the development folder. Both start
/// with the type table (the record types the value uses and their versions); an older version is read
/// by its own layout and upgraded to the current type; a newer one, damaged data, an unknown key or a
/// value its declaration doesn't allow throws a <see cref="RecordFormatException"/>.
/// </summary>
public static class RecordSerializer
{
    static readonly JsonReaderOptions JsonOptions = new() { MaxDepth = 1000 };

    /// <summary>The binary form of <paramref name="value"/>, a record of type <typeparamref name="T"/> (its current version).</summary>
    public static byte[] ToBinary<T>(T value) where T : notnull => ToBinary(typeof(T), value);

    public static byte[] ToBinary(Type type, object value) => WriteBinary(type, value, null);

    /// <summary>A record of type <typeparamref name="T"/> from its binary form, upgraded to the current version.</summary>
    public static T FromBinary<T>(ReadOnlySpan<byte> data) where T : notnull => (T)FromBinary(typeof(T), data);

    public static object FromBinary(Type type, ReadOnlySpan<byte> data) => ReadBinary(type, data, out _);

    /// <summary>The JSON form of <paramref name="value"/>: indented by two spaces, LF line ends, a final line end.</summary>
    public static string ToJson<T>(T value) where T : notnull => ToJson(typeof(T), value);

    public static string ToJson(Type type, object value) => Encoding.UTF8.GetString(ToJsonUtf8(type, value));

    /// <summary>The JSON form as UTF-8 bytes (without a byte order mark).</summary>
    public static byte[] ToJsonUtf8(Type type, object value) => WriteJson(type, value, null);

    /// <summary>A record of type <typeparamref name="T"/> from its JSON form, upgraded to the current version.</summary>
    public static T FromJson<T>(string json) where T : notnull => (T)FromJson(typeof(T), Encoding.UTF8.GetBytes(StringCodec.Checked(json)));

    public static T FromJson<T>(ReadOnlySpan<byte> utf8) where T : notnull => (T)FromJson(typeof(T), utf8);

    public static object FromJson(Type type, ReadOnlySpan<byte> utf8) => ReadJson(type, utf8, out _);

    /// <summary>The record type a binary form holds (the first name of its type table), to read data of a type not known in advance.</summary>
    public static string RootTypeName(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryRecordReader(data, null!);
        if (reader.ReadCount() == 0)
            throw new RecordFormatException("the type table is empty");
        return reader.ReadString();
    }

    /// <summary>The layout signatures of every version of a record type, oldest first: what the committed layouts list must hold.</summary>
    public static string LayoutText(Type type) => Root(type).LayoutText();

    /// <summary>The current record types of the assemblies (those <paramref name="include"/> accepts) by their names; past versions left out.</summary>
    public static IReadOnlyDictionary<string, Type> FindTypes(IEnumerable<Assembly> assemblies, Func<Type, bool>? include = null)
    {
        var found = new SortedDictionary<string, Type>(StringComparer.Ordinal);
        foreach (var type in assemblies.SelectMany(a => a.GetTypes()))
        {
            if (type.GetCustomAttribute<RecordAttribute>(false) is not { } record || include?.Invoke(type) == false)
                continue;
            if (type.DeclaringType?.GetCustomAttribute<RecordAttribute>(false) is { } outer && outer.Name == record.Name)
                continue;
            if (!found.TryAdd(record.Name, type))
                throw new InvalidOperationException($"{type.FullName} and {found[record.Name].FullName} are both named {record.Name}");
        }
        return found;
    }

    static RecordFamily Root(Type type) => RecordFamily.Of(type);

    internal static byte[] WriteBinary(Type type, object value, RecordWriteOptions? options)
    {
        ArgumentNullException.ThrowIfNull(value);
        var family = Root(type);
        var session = new WriteSession(options);
        family.Visit(session, value);
        var writer = new BinaryRecordWriter(session);
        writer.WriteCount(session.Table.Count);
        foreach (var (entry, version) in session.Table)
        {
            writer.WriteString(entry.Name);
            writer.WriteVarUInt((ulong)version);
        }
        family.Write(writer, value);
        return writer.Written.ToArray();
    }

    internal static object ReadBinary(Type type, ReadOnlySpan<byte> data, out ReadSession session)
    {
        var family = Root(type);
        var reader = new BinaryRecordReader(data, null!);
        var count = reader.ReadCount();
        var table = new List<(string, int)>(count);
        for (var i = 0; i < count; i++)
        {
            var name = reader.ReadString();
            table.Add((name, reader.ReadVarInt32("a version")));
        }
        session = new ReadSession(family, table);
        reader.Session = session;
        var value = family.Read(ref reader);
        if (reader.Remaining > 0)
            throw new RecordFormatException($"{reader.Remaining} bytes follow the record");
        return value;
    }

    internal static byte[] WriteJson(Type type, object value, RecordWriteOptions? options, bool indented = true)
    {
        ArgumentNullException.ThrowIfNull(value);
        var family = Root(type);
        var session = new WriteSession(options);
        family.Visit(session, value);
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new JsonRecordWriter(session, buffer, indented))
        {
            var json = writer.Json;
            json.WriteStartObject();
            json.WritePropertyName("$types"u8);
            json.WriteStartObject();
            foreach (var (entry, version) in session.Table)
                json.WriteNumber(entry.Name, version);
            json.WriteEndObject();
            family.WriteJsonMembers(writer, value);
            json.WriteEndObject();
        }
        if (indented)
            buffer.Write("\n"u8);
        return buffer.WrittenSpan.ToArray();
    }

    internal static object ReadJson(Type type, ReadOnlySpan<byte> utf8, out ReadSession session)
    {
        var family = Root(type);
        if (utf8.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
            utf8 = utf8[3..];
        JsonDocument document;
        try
        {
            var reader = new Utf8JsonReader(utf8, JsonOptions);
            document = JsonDocument.ParseValue(ref reader);
            // Only whitespace may follow the value: the reader throws at anything else.
            reader.Read();
        }
        catch (JsonException e)
        {
            throw new RecordFormatException($"not valid JSON: {e.Message}", e);
        }
        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw new RecordFormatException($"expected an object with \"$types\", found {root.ValueKind.ToString().ToLowerInvariant()}");
            JsonElement? types = null;
            foreach (var property in root.EnumerateObject())
                if (property.Name == "$types")
                    types = types == null ? property.Value : throw new RecordFormatException("the key \"$types\" is there twice");
            if (types is not { ValueKind: JsonValueKind.Object } table)
                throw new RecordFormatException("expected \"$types\", an object of record type names and their versions");
            var entries = new List<(string, int)>();
            foreach (var entry in table.EnumerateObject())
            {
                if (entry.Value.ValueKind != JsonValueKind.Number || !entry.Value.TryGetInt32(out var version))
                    throw new RecordFormatException($"the version of {entry.Name} in \"$types\" isn't an integer");
                entries.Add((entry.Name, version));
            }
            session = new ReadSession(family, entries);
            return family.ReadJson(new JsonRecordReader(session), root, ReservedKeys.Table);
        }
    }
}
