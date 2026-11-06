using System.Buffers;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Statventure.Records;

/// <summary>One version of a record type: a plain record's fields, or a union's subtypes.</summary>
abstract class RecordLayout(RecordFamily family, int version, Type type)
{
    public RecordFamily Family { get; } = family;
    public int Version { get; } = version;
    public Type Type { get; } = type;
    public abstract bool IsUnion { get; }
    /// <summary>A plain record without fields (it takes no bytes).</summary>
    public abstract bool IsEmpty { get; }
    /// <summary>The record types its fields or subtypes refer to.</summary>
    public abstract IEnumerable<RecordFamily> Families { get; }
    /// <summary>The layout signature: what the committed layouts list records for this version.</summary>
    public abstract string Signature { get; }

    public abstract object ReadObject(ref BinaryRecordReader reader);
    public abstract void WriteObject(BinaryRecordWriter writer, object value);
    public abstract object ReadJsonObject(JsonRecordReader reader, JsonElement element, ReservedKeys reserved);
    public abstract void WriteJsonMembers(JsonRecordWriter writer, object value);
    public abstract void VisitObject(WriteSession session, object value);

    // What a layout holds, for the record shapes and drafts (Records/RecordShape.cs, RecordDraft.cs).

    /// <summary>A plain record's fields in number order; none for a union.</summary>
    public virtual IReadOnlyList<FieldLayout> Fields => [];

    /// <summary>A union's subtypes by tag; none for a plain record.</summary>
    public virtual IReadOnlyDictionary<int, RecordFamily> Subtypes => new Dictionary<int, RecordFamily>();

    /// <summary>A plain record made by the compiled factory: an absent field keeps its declaration's default.</summary>
    public virtual object Make(object?[] values, bool[] present) =>
        throw new InvalidOperationException($"{Family.Name} is a union: its values are its subtypes'");

    public static RecordLayout Build(RecordFamily family, int version, Type type)
    {
        var subtypes = type.GetCustomAttributes<SubtypeAttribute>(false).ToArray();
        if (subtypes.Length > 0)
            return new UnionLayout(family, version, type, subtypes);
        if (type.IsAbstract)
            throw new InvalidOperationException($"{type.Name}: an abstract record type is a union and needs [Subtype]s");
        return (RecordLayout)Codecs.Create(typeof(PlainLayout<>).MakeGenericType(type), family, version);
    }
}

/// <summary>A serialized property: its number, names, codec, and whether it must be given or what it defaults to.</summary>
abstract class FieldLayout(PropertyInfo property, int number, string jsonName, Codec codec)
{
    public int Number { get; } = number;
    public PropertyInfo Property { get; } = property;
    public string JsonName { get; } = jsonName;
    public Codec Codec { get; } = codec;
    /// <summary>Must be present in the JSON form: a required property, a constructor parameter without a default, a non-nullable reference without a default.</summary>
    public bool Required { get; set; }
    /// <summary>The value an absent JSON key means (when not <see cref="Required"/>).</summary>
    public object? Default { get; set; }

    /// <summary>The field's value in <paramref name="record"/>, through the compiled getter.</summary>
    public abstract object? Get(object record);
}

/// <summary>A field of a record of type <typeparamref name="TRecord"/>, read through a compiled getter.</summary>
abstract class FieldLayout<TRecord>(PropertyInfo property, int number, string jsonName, Codec codec) : FieldLayout(property, number, jsonName, codec)
{
    public abstract void Write(BinaryRecordWriter writer, TRecord record);
    public abstract void WriteJson(JsonRecordWriter writer, TRecord record);
    public abstract void Visit(WriteSession session, TRecord record);

    public static FieldLayout<TRecord> Create(PropertyInfo property, int number, string jsonName, Codec codec) =>
        (FieldLayout<TRecord>)Codecs.Create(typeof(TypedField<,>).MakeGenericType(typeof(TRecord), property.PropertyType),
            property, number, jsonName, codec);
}

sealed class TypedField<TRecord, TValue>(PropertyInfo property, int number, string jsonName, Codec codec)
    : FieldLayout<TRecord>(property, number, jsonName, codec)
{
    readonly Func<TRecord, TValue> _get = Getter(property);
    readonly Codec<TValue> _codec = (Codec<TValue>)codec;

    static Func<TRecord, TValue> Getter(PropertyInfo property)
    {
        var record = Expression.Parameter(typeof(TRecord), "record");
        return Expression.Lambda<Func<TRecord, TValue>>(Expression.Property(record, property), record).Compile();
    }

    public override object? Get(object record) => _get((TRecord)record);

    public override void Write(BinaryRecordWriter writer, TRecord record) => _codec.Write(writer, _get(record));

    public override void WriteJson(JsonRecordWriter writer, TRecord record)
    {
        var value = _get(record);
        // Left out: a null where an absent key means null (answer Q-B, 2026-11-03).
        if (value is null && !Required && Default is null)
            return;
        writer.Json.WritePropertyName(JsonName);
        _codec.WriteJson(writer, value);
    }

    public override void Visit(WriteSession session, TRecord record) => _codec.Visit(session, _get(record));
}

/// <summary>
/// A plain record: its <see cref="FieldAttribute"/> properties in number order, made through the
/// constructor whose parameters are fields (a positional record) or the parameterless one, then the
/// remaining fields' setters; compiled once.
/// </summary>
sealed class PlainLayout<T> : RecordLayout
{
    readonly FieldLayout<T>[] _fields;
    readonly Dictionary<string, int> _byJsonName = [];
    readonly Func<object?[], bool[], T> _make;
    readonly bool[] _allPresent;
    string? _signature;

    public PlainLayout(RecordFamily family, int version) : base(family, version, typeof(T))
    {
        var type = typeof(T);
        var fields = new List<FieldLayout<T>>();
        var nullability = new NullabilityInfoContext();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (property.GetCustomAttribute<FieldAttribute>(true) is not { } field)
                continue;
            var where = $"{type.Name}.{property.Name}";
            if (field.Number < 1)
                throw new InvalidOperationException($"{where}: field numbers start at 1");
            if (property.GetMethod == null || property.GetIndexParameters().Length > 0)
                throw new InvalidOperationException($"{where}: a field needs a getter and no index");
            var jsonName = JsonNamingPolicy.CamelCase.ConvertName(property.Name);
            var codec = Codecs.For(property.PropertyType, nullability.Create(property), where);
            fields.Add(FieldLayout<T>.Create(property, field.Number, jsonName, codec));
        }
        fields.Sort((a, b) => a.Number.CompareTo(b.Number));
        for (var i = 0; i < fields.Count; i++)
        {
            if (i > 0 && fields[i].Number == fields[i - 1].Number)
                throw new InvalidOperationException($"{type.Name}: two fields are number {fields[i].Number}");
            if (!_byJsonName.TryAdd(fields[i].JsonName, i))
                throw new InvalidOperationException($"{type.Name}: two fields are named {fields[i].JsonName}");
        }
        _fields = [.. fields];
        _allPresent = Enumerable.Repeat(true, _fields.Length).ToArray();
        _make = Compile(type, _fields);
    }

    public override bool IsUnion => false;
    public override bool IsEmpty => _fields.Length == 0;
    public override IEnumerable<RecordFamily> Families => _fields.SelectMany(f => f.Codec.Families);
    public override IReadOnlyList<FieldLayout> Fields => _fields;
    public override object Make(object?[] values, bool[] present) => _make(values, present)!;

    /// <summary>
    /// The factory: <c>(values, present) => { var r = new T(…); r.F = …; return r; }</c>, where an
    /// absent field keeps the default its declaration gives (a parameter's default, an initializer).
    /// Also decides which fields are required and what each absent one means.
    /// </summary>
    static Func<object?[], bool[], T> Compile(Type type, FieldLayout<T>[] fields)
    {
        var byName = fields.Select((f, i) => (f, i)).ToDictionary(x => x.f.Property.Name, x => x.i, StringComparer.OrdinalIgnoreCase);
        var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .Where(c => c.GetParameters() is { Length: > 0 } ps
                && ps.All(p => byName.TryGetValue(p.Name!, out var i) && fields[i].Property.PropertyType == p.ParameterType))
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
        if (constructor == null && !type.IsValueType && type.GetConstructor(Type.EmptyTypes) == null)
            throw new InvalidOperationException($"{type.Name}: a record needs a public parameterless constructor or one whose parameters are its fields");

        var values = Expression.Parameter(typeof(object[]), "values");
        var present = Expression.Parameter(typeof(bool[]), "present");
        var record = Expression.Variable(type, "record");
        Expression Value(int i) => Expression.Convert(Expression.ArrayIndex(values, Expression.Constant(i)), fields[i].Property.PropertyType);
        Expression Present(int i) => Expression.ArrayIndex(present, Expression.Constant(i));

        var parameters = constructor?.GetParameters() ?? [];
        var byParameter = new HashSet<int>();
        var arguments = new List<Expression>();
        var placeholders = new object?[parameters.Length];
        for (var k = 0; k < parameters.Length; k++)
        {
            var parameter = parameters[k];
            var i = byName[parameter.Name!];
            if (!byParameter.Add(i))
                throw new InvalidOperationException($"{type.Name}: two constructor parameters are the field {fields[i].Property.Name}");
            var field = fields[i];
            if (parameter.HasDefaultValue)
            {
                field.Default = DefaultOf(parameter);
                placeholders[k] = field.Default;
                arguments.Add(Expression.Condition(Present(i), Value(i), Expression.Constant(field.Default, parameter.ParameterType)));
            }
            else
            {
                field.Required = true;
                placeholders[k] = parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
                arguments.Add(Value(i));
            }
        }

        var body = new List<Expression>
        {
            Expression.Assign(record, constructor != null ? Expression.New(constructor, arguments) : Expression.New(type)),
        };
        var setters = Enumerable.Range(0, fields.Length).Where(i => !byParameter.Contains(i)).ToArray();
        object? instance = null;
        foreach (var i in setters)
        {
            var field = fields[i];
            if (field.Property.SetMethod == null)
                throw new InvalidOperationException($"{type.Name}.{field.Property.Name}: a field that isn't a constructor parameter needs a setter");
            var assign = Expression.Assign(Expression.Property(record, field.Property), Value(i));
            if (field.Property.IsDefined(typeof(RequiredMemberAttribute), false))
            {
                field.Required = true;
                body.Add(assign);
                continue;
            }
            // The default is what the type's construction leaves there (an initializer).
            instance ??= DefaultInstance(type, constructor, placeholders);
            field.Default = field.Property.GetValue(instance);
            body.Add(Expression.IfThen(Present(i), assign));
        }
        foreach (var field in fields)
            if (!field.Required && field.Default is null && !field.Codec.AcceptsNull)
                field.Required = true;
        body.Add(record);
        return Expression.Lambda<Func<object?[], bool[], T>>(Expression.Block([record], body), values, present).Compile();
    }

    static object? DefaultOf(ParameterInfo parameter)
    {
        var value = parameter.DefaultValue;
        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        if (value == null)
            return parameter.ParameterType.IsValueType && Nullable.GetUnderlyingType(parameter.ParameterType) == null
                ? Activator.CreateInstance(parameter.ParameterType) : null;
        return type.IsEnum ? Enum.ToObject(type, value) : Convert.ChangeType(value, type, System.Globalization.CultureInfo.InvariantCulture);
    }

    static object DefaultInstance(Type type, ConstructorInfo? constructor, object?[] placeholders)
    {
        try
        {
            return constructor == null ? Activator.CreateInstance(type)! : constructor.Invoke(placeholders);
        }
        catch (TargetInvocationException e)
        {
            throw new InvalidOperationException($"{type.Name}: making an instance to learn the fields' defaults failed: {e.InnerException?.Message}", e);
        }
    }

    public override object ReadObject(ref BinaryRecordReader reader)
    {
        var values = new object?[_fields.Length];
        for (var i = 0; i < _fields.Length; i++)
        {
            try
            {
                values[i] = _fields[i].Codec.ReadObject(ref reader);
            }
            catch (RecordFormatException e)
            {
                throw e.Within("." + _fields[i].JsonName);
            }
        }
        return _make(values, _allPresent)!;
    }

    public override void WriteObject(BinaryRecordWriter writer, object value)
    {
        var record = (T)value;
        foreach (var field in _fields)
        {
            try
            {
                field.Write(writer, record);
            }
            catch (RecordFormatException e)
            {
                throw e.Within("." + field.JsonName);
            }
        }
    }

    public override object ReadJsonObject(JsonRecordReader reader, JsonElement element, ReservedKeys reserved)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new RecordFormatException($"expected a {Family.Name} (an object), found {element.ValueKind.ToString().ToLowerInvariant()}");
        var values = new object?[_fields.Length];
        var present = new bool[_fields.Length];
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name == "$type" && (reserved & ReservedKeys.Type) != 0 || property.Name == "$types" && (reserved & ReservedKeys.Table) != 0)
                continue;
            if (!_byJsonName.TryGetValue(property.Name, out var i))
                throw new RecordFormatException($"{Family.Name} (version {Version}) has no field \"{property.Name}\"");
            if (present[i])
                throw new RecordFormatException($"the key \"{property.Name}\" is there twice");
            present[i] = true;
            try
            {
                values[i] = _fields[i].Codec.ReadJsonObject(reader, property.Value);
            }
            catch (RecordFormatException e)
            {
                throw e.Within("." + property.Name);
            }
        }
        for (var i = 0; i < _fields.Length; i++)
            if (!present[i] && _fields[i].Required)
                throw new RecordFormatException($"{Family.Name} needs the key \"{_fields[i].JsonName}\"");
        return _make(values, present)!;
    }

    public override void WriteJsonMembers(JsonRecordWriter writer, object value)
    {
        var record = (T)value;
        foreach (var field in _fields)
        {
            try
            {
                field.WriteJson(writer, record);
            }
            catch (RecordFormatException e)
            {
                throw e.Within("." + field.JsonName);
            }
        }
    }

    public override void VisitObject(WriteSession session, object value)
    {
        var record = (T)value;
        foreach (var field in _fields)
        {
            if (!field.Codec.HoldsRecords)
                continue;
            try
            {
                field.Visit(session, record);
            }
            catch (RecordFormatException e)
            {
                throw e.Within("." + field.JsonName);
            }
        }
    }

    public override string Signature => _signature ??= MakeSignature();

    string MakeSignature()
    {
        var text = new StringBuilder($"{Family.Name} {Version}\n");
        foreach (var field in _fields)
        {
            text.Append($"  {field.Number} {field.JsonName}: {field.Codec.Signature}");
            text.Append(field.Required ? " (required)\n" : $" = {JsonText(field)}\n");
        }
        return text.ToString();
    }

    /// <summary>A default in compact JSON, as an absent key means it.</summary>
    static string JsonText(FieldLayout field)
    {
        if (field.Default == null)
            return "null";
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new JsonRecordWriter(new WriteSession(null), buffer, indented: false))
            field.Codec.WriteJsonObject(writer, field.Default);
        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}

/// <summary>
/// A union: the subtypes its version lists by tag. Binary: the tag, then the subtype's fields; JSON:
/// "$type" with the subtype's record name, then its fields.
/// </summary>
sealed class UnionLayout : RecordLayout
{
    readonly SortedDictionary<int, RecordFamily> _byTag = [];
    readonly Dictionary<RecordFamily, int> _tags = [];
    readonly Dictionary<string, RecordFamily> _byName = [];

    public UnionLayout(RecordFamily family, int version, Type type, SubtypeAttribute[] subtypes) : base(family, version, type)
    {
        if (!type.IsAbstract)
            throw new InvalidOperationException($"{type.Name}: a union's type must be abstract");
        foreach (var subtype in subtypes)
        {
            var where = $"{type.Name}, tag {subtype.Tag}";
            if (subtype.Tag < 1)
                throw new InvalidOperationException($"{where}: tags start at 1");
            if (subtype.Type.IsAbstract || subtype.Type.GetCustomAttributes<SubtypeAttribute>(false).Any())
                throw new InvalidOperationException($"{where}: a subtype is a plain record, not another union");
            if (version == family.CurrentVersion && !type.IsAssignableFrom(subtype.Type))
                throw new InvalidOperationException($"{where}: {subtype.Type.Name} doesn't derive from {type.Name}");
            var sub = RecordFamily.Of(subtype.Type);
            if (!_byTag.TryAdd(subtype.Tag, sub))
                throw new InvalidOperationException($"{type.Name}: two subtypes have the tag {subtype.Tag}");
            if (!_tags.TryAdd(sub, subtype.Tag))
                throw new InvalidOperationException($"{type.Name}: {sub.Name} is listed twice");
            _byName[sub.Name] = sub;
        }
    }

    public override bool IsUnion => true;
    public override bool IsEmpty => false;
    public override IEnumerable<RecordFamily> Families => _byTag.Values;
    public override IReadOnlyDictionary<int, RecordFamily> Subtypes => _byTag;

    public override string Signature =>
        $"{Family.Name} {Version} union\n" + string.Concat(_byTag.Select(t => $"  {t.Key} {t.Value.Name}\n"));

    RecordFamily SubtypeOf(object value)
    {
        RecordFamily sub;
        try
        {
            sub = RecordFamily.Of(value.GetType());
        }
        catch (InvalidOperationException)
        {
            throw new RecordFormatException($"a {value.GetType().Name} isn't a subtype of {Family.Name}");
        }
        return _tags.ContainsKey(sub) ? sub : throw new RecordFormatException($"{sub.Name} isn't a subtype of {Family.Name} version {Version}");
    }

    object Checked(object value) =>
        Family.CurrentType.IsInstanceOfType(value) ? value
            : throw new RecordFormatException($"a {value.GetType().Name} read as a {Family.Name} isn't a {Family.CurrentType.Name}");

    public override object ReadObject(ref BinaryRecordReader reader)
    {
        var at = reader.Position;
        var tag = reader.ReadVarInt32("a union's tag");
        if (!_byTag.TryGetValue(tag, out var sub))
            throw new RecordFormatException($"the tag {tag} at byte {at} isn't a subtype of {Family.Name} version {Version}");
        return Checked(sub.Read(ref reader));
    }

    public override void WriteObject(BinaryRecordWriter writer, object value)
    {
        var sub = SubtypeOf(value);
        writer.WriteVarUInt((ulong)_tags[sub]);
        sub.Write(writer, value);
    }

    public override object ReadJsonObject(JsonRecordReader reader, JsonElement element, ReservedKeys reserved)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new RecordFormatException($"expected a {Family.Name} (an object with \"$type\"), found {element.ValueKind.ToString().ToLowerInvariant()}");
        string? name = null;
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name != "$type")
                continue;
            if (name != null)
                throw new RecordFormatException("the key \"$type\" is there twice");
            name = StringCodec.ReadString(property.Value);
        }
        if (name == null)
            throw new RecordFormatException($"a {Family.Name} needs \"$type\", the name of its subtype");
        if (!_byName.TryGetValue(name, out var sub))
            throw new RecordFormatException($"\"{name}\" isn't a subtype of {Family.Name} version {Version}");
        return Checked(sub.ReadJson(reader, element, reserved | ReservedKeys.Type));
    }

    public override void WriteJsonMembers(JsonRecordWriter writer, object value)
    {
        var sub = SubtypeOf(value);
        writer.Json.WriteString("$type"u8, sub.Name);
        sub.WriteJsonMembers(writer, value);
    }

    public override void VisitObject(WriteSession session, object value) => SubtypeOf(value).Visit(session, value);
}

/// <summary>Picks the codec for a field's type and nullability.</summary>
static class Codecs
{
    public static Codec For(Type type, NullabilityInfo? nullability, string where)
    {
        if (Nullable.GetUnderlyingType(type) is { } inner)
            return Make(typeof(NullableValueCodec<>), [inner], NotNull(inner, null, where));
        if (!type.IsValueType)
        {
            switch (nullability?.ReadState)
            {
                case NullabilityState.Nullable:
                    return Make(typeof(NullableReferenceCodec<>), [type], NotNull(type, nullability, where));
                case NullabilityState.NotNull:
                    break;
                default:
                    throw new InvalidOperationException($"{where}: the nullability of {type.Name} isn't known (records need nullable annotations)");
            }
        }
        return NotNull(type, nullability, where);
    }

    static Codec Make(Type generic, Type[] arguments, params object[] parameters) =>
        (Codec)Create(generic.MakeGenericType(arguments), parameters);

    /// <summary>An instance of a generic codec or layout, with its constructor's own exception rather than reflection's wrapper.</summary>
    public static object Create(Type type, params object[] parameters)
    {
        try
        {
            return Activator.CreateInstance(type, parameters)!;
        }
        catch (TargetInvocationException e) when (e.InnerException is { } inner)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(inner);
            throw;
        }
    }

    static Codec Instance(Type generic, Type argument) =>
        (Codec)generic.MakeGenericType(argument).GetField("Instance")!.GetValue(null)!;

    static NullabilityInfo? Argument(NullabilityInfo? info, int index) =>
        info != null && info.GenericTypeArguments.Length > index ? info.GenericTypeArguments[index] : null;

    static readonly Type[] ListTypes =
        [typeof(List<>), typeof(IReadOnlyList<>), typeof(IList<>), typeof(IReadOnlyCollection<>), typeof(ICollection<>), typeof(IEnumerable<>)];

    static readonly Type[] MapTypes =
        [typeof(Dictionary<,>), typeof(IReadOnlyDictionary<,>), typeof(IDictionary<,>), typeof(OrderedDictionary<,>)];

    static Codec NotNull(Type type, NullabilityInfo? nullability, string where)
    {
        if (NumberCodec<int>.IsNumberType(type))
            return Instance(typeof(NumberCodec<>), type);
        if (MathCodec<float>.IsMathType(type))
            return Instance(typeof(MathCodec<>), type);
        if (type == typeof(string))
            return StringCodec.Instance;
        if (type == typeof(byte[]))
            return BytesCodec.Instance;
        if (type.IsEnum)
            return Make(typeof(EnumCodec<,>), [type, Enum.GetUnderlyingType(type)]);
        if (type.IsSZArray)
            return List(type, type.GetElementType()!, nullability?.ElementType, where);
        if (type.IsGenericType && ListTypes.Contains(type.GetGenericTypeDefinition()))
            return List(type, type.GetGenericArguments()[0], Argument(nullability, 0), where);
        if (type.IsGenericType && MapTypes.Contains(type.GetGenericTypeDefinition()))
        {
            var arguments = type.GetGenericArguments();
            return Map(type, arguments[0], arguments[1], Argument(nullability, 0), Argument(nullability, 1), pairs: false, where);
        }
        if (type.IsDefined(typeof(RecordAttribute), false))
            return Make(typeof(RecordCodec<>), [type], RecordFamily.Of(type));
        throw new InvalidOperationException($"{where}: {type.Name} isn't a kind of field records have");
    }

    static Codec List(Type type, Type element, NullabilityInfo? elementNullability, string where)
    {
        if (element.IsGenericType && element.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            var arguments = element.GetGenericArguments();
            return Map(type, arguments[0], arguments[1], Argument(elementNullability, 0), Argument(elementNullability, 1), pairs: true, where);
        }
        if (!(bool)typeof(ListShape<,>).MakeGenericType(type, element).GetMethod("Fits")!.Invoke(null, [type])!)
            throw new InvalidOperationException($"{where}: a {type.Name} can't be made from an array");
        var codec = For(element, elementNullability, where + "[]");
        return element.IsValueType && (NumberCodec<int>.IsNumberType(element) && element != typeof(bool) || MathCodec<float>.IsMathType(element))
            ? Make(typeof(BlockListCodec<,>), [type, element], codec)
            : Make(typeof(ListCodec<,>), [type, element], codec);
    }

    static Codec Map(Type type, Type key, Type value, NullabilityInfo? keyNullability, NullabilityInfo? valueNullability, bool pairs, string where)
    {
        var keyCodec = For(key, keyNullability, where + " key");
        if (keyCodec.AcceptsNull || !typeof(IKeyCodec<>).MakeGenericType(key).IsInstanceOfType(keyCodec)
            || key == typeof(bool) || key == typeof(float) || key == typeof(double))
            throw new InvalidOperationException($"{where}: a dictionary's key is a string, an integer or an enum, not a {key.Name}");
        var valueCodec = For(value, valueNullability, where + " value");
        return Make(typeof(MapCodec<,,>), [type, key, value], keyCodec, valueCodec, pairs);
    }
}
