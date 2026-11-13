using System.Collections;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Statventure.Packs;

namespace Statventure.Records;

/// <summary>
/// What a record type is made of, for tools that edit records of any kind (the editor, M11:
/// Docs/Editor.md, "Records: shapes, drafts and marks"): its record name and current version, the name
/// shown for it, the folder and name rule of its entries (P9), its fields in number order (for a union,
/// its subtypes instead) and its own checks (P20). Read from the layouts the serializer builds and the
/// marks in the type's definition (<see cref="KindAttribute"/> and the others in RecordMarks.cs); the
/// forms and the layout signatures don't depend on it. Made once per current record type; a mark that
/// doesn't fit throws an <see cref="InvalidOperationException"/> naming the type and field when the
/// fields are first asked for.
/// </summary>
public sealed class RecordShape
{
    static readonly ConcurrentDictionary<Type, RecordShape> Shapes = new();

    readonly RecordLayout _layout;
    readonly Lazy<FieldShape[]> _fields;
    readonly Lazy<RecordShape[]> _subtypes;
    readonly Lazy<Action<object, CheckContext>[]> _checks;
    readonly Lazy<RecordShape[]> _checkedBy;

    RecordShape(Type type)
    {
        var family = RecordFamily.Of(type);
        _layout = family.Layout(family.CurrentVersion);
        Type = type;
        Name = family.Name;
        Version = family.CurrentVersion;
        var kind = type.GetCustomAttribute<KindAttribute>(false);
        DisplayName = kind?.DisplayName ?? DisplayNames.Of(type.Name);
        Help = type.GetCustomAttribute<HelpAttribute>(false)?.Text;
        IsPackKind = !type.IsDefined(typeof(NotAPackKindAttribute), false);
        if (kind?.Folder is { } folder && PackPath.Problem(folder) is { } problem)
            throw new InvalidOperationException($"{type.Name}: the kind's folder \"{folder}\" isn't a data path ({problem})");
        Folder = kind?.Folder;
        Rule = NameRule.Of(kind?.Rule, NameRule.Plain, type.Name);
        _fields = new(MakeFields);
        _subtypes = new(() => [.. _layout.Subtypes.Values.Select(f => Of(f.CurrentType))]);
        _checks = new(FindChecks);
        _checkedBy = new(() =>
        {
            // The unions this type is a subtype of, outermost first, then itself.
            var chain = new List<RecordShape> { this };
            for (var t = Type.BaseType; t != null; t = t.BaseType)
                if (t.GetCustomAttribute<RecordAttribute>(false) != null)
                    chain.Insert(0, Of(t));
            return [.. chain];
        });
    }

    /// <summary>The shape of a current record type (a plain record, a union or a union's subtype).</summary>
    public static RecordShape Of(Type type) => Shapes.GetOrAdd(type, t => new RecordShape(t));

    public static RecordShape Of<T>() => Of(typeof(T));

    public Type Type { get; }

    /// <summary>The record name (<c>audio.effect</c>).</summary>
    public string Name { get; }

    /// <summary>The current version.</summary>
    public int Version { get; }

    /// <summary>The name tools show: its <see cref="KindAttribute"/>'s, else made from the type's code name.</summary>
    public string DisplayName { get; }

    /// <summary>Its <see cref="HelpAttribute"/>'s text, or null.</summary>
    public string? Help { get; }

    /// <summary>Whether its values may be entries of the game's data: true unless it is marked <see cref="NotAPackKindAttribute"/> (the save).</summary>
    public bool IsPackKind { get; }

    /// <summary>The folder of the kind's entries, from its <see cref="KindAttribute"/>; null when it has none.</summary>
    public string? Folder { get; }

    /// <summary>How its entries are named in <see cref="Folder"/> (P9; <see cref="NameRule.Plain"/> unless its mark gives one).</summary>
    public NameRule Rule { get; }

    public bool IsUnion => _layout.IsUnion;

    /// <summary>A plain record's fields in number order; none for a union.</summary>
    public IReadOnlyList<FieldShape> Fields => _fields.Value;

    /// <summary>A union's subtypes in tag order; none for a plain record.</summary>
    public IReadOnlyList<RecordShape> Subtypes => _subtypes.Value;

    /// <summary>The field with this JSON name or code name, or null.</summary>
    public FieldShape? Field(string name)
    {
        var json = JsonNamingPolicy.CamelCase.ConvertName(name);
        foreach (var field in Fields)
            if (field.JsonName == json)
                return field;
        return null;
    }

    /// <summary>The entry path of an entry of this kind named <paramref name="name"/>, or null when it has no folder or the rule can't store the name.</summary>
    public string? PathOf(string name) => Folder == null ? null : Rule.PathOf(Folder, name);

    /// <summary>The name of this kind's entry at <paramref name="path"/>, or null when it has no folder or the path isn't one of its rule's.</summary>
    public string? NameOf(string path) => Folder == null ? null : Rule.NameOf(Folder, path);

    public override string ToString() => Name;

    internal object Make(object?[] values, bool[] present) => _layout.Make(values, present);

    /// <summary>Its own checks (static methods marked <see cref="CheckAttribute"/>), in the order of their names.</summary>
    internal IReadOnlyList<Action<object, CheckContext>> Checks => _checks.Value;

    /// <summary>The shapes whose checks a value of this type gets: the unions it is a subtype of, outermost first, then its own.</summary>
    internal IReadOnlyList<RecordShape> CheckedBy => _checkedBy.Value;

    FieldShape[] MakeFields()
    {
        var layouts = _layout.Fields;
        var fields = new FieldShape[layouts.Count];
        for (var i = 0; i < fields.Length; i++)
            fields[i] = new FieldShape(this, i, layouts[i]);
        foreach (var field in fields)
            field.ResolveReferences(fields);
        return fields;
    }

    Action<object, CheckContext>[] FindChecks()
    {
        var checks = new List<(string Name, Action<object, CheckContext> Run)>();
        const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
        foreach (var method in Type.GetMethods(all))
        {
            if (!method.IsDefined(typeof(CheckAttribute), false))
                continue;
            var parameters = method.GetParameters();
            if (!method.IsStatic || method.ReturnType != typeof(void) || method.IsGenericMethodDefinition || parameters.Length != 2
                || !parameters[0].ParameterType.IsAssignableFrom(Type) || parameters[1].ParameterType != typeof(CheckContext))
                throw new InvalidOperationException($"{Type.Name}.{method.Name}: a check is a static void method taking the record and a CheckContext");
            var value = Expression.Parameter(typeof(object), "value");
            var context = Expression.Parameter(typeof(CheckContext), "check");
            var call = Expression.Call(method, Expression.Convert(value, parameters[0].ParameterType), context);
            checks.Add((method.Name, Expression.Lambda<Action<object, CheckContext>>(call, value, context).Compile()));
        }
        return [.. checks.OrderBy(c => c.Name, StringComparer.Ordinal).Select(c => c.Run)];
    }
}

/// <summary>The limits of a field's numbers (<see cref="LimitsAttribute"/>).</summary>
public sealed record FieldLimits(double Min, double Max, bool MinExclusive, bool MaxExclusive)
{
    /// <summary>Whether <paramref name="value"/> is within the limits (NaN never is).</summary>
    public bool Contains(double value) =>
        (MinExclusive ? value > Min : value >= Min) && (MaxExclusive ? value < Max : value <= Max);

    /// <summary>The limits in words: "above 0 and at most 1", "1 or more".</summary>
    public string Describe()
    {
        var parts = new List<string>();
        if (MinExclusive || !double.IsNegativeInfinity(Min))
            parts.Add(MinExclusive ? $"above {Number(Min)}" : $"{Number(Min)} or more");
        if (MaxExclusive || !double.IsPositiveInfinity(Max))
            parts.Add(MaxExclusive ? $"below {Number(Max)}" : $"at most {Number(Max)}");
        return parts.Count == 0 ? "a number" : string.Join(" and ", parts);
    }

    static string Number(double value) => value.ToString("R", CultureInfo.InvariantCulture);
}

/// <summary>
/// What a field's strings name (<see cref="ReferencesAttribute"/>, M11-11): entries of <see cref="Kind"/>
/// in <see cref="Folder"/>, each string made into a path by <see cref="Rule"/>; with a condition, only
/// while <see cref="When"/>'s value is one of <see cref="Is"/>.
/// </summary>
public sealed class ReferenceShape
{
    internal ReferenceShape(RecordShape kind, string folder, NameRule rule, bool keys, string? when, IReadOnlyList<string> @is)
    {
        Kind = kind;
        Folder = folder;
        Rule = rule;
        Keys = keys;
        WhenName = when;
        Is = @is;
    }

    /// <summary>The kind of the entries named.</summary>
    public RecordShape Kind { get; }
    public string Folder { get; }
    public NameRule Rule { get; }
    /// <summary>A map's keys are the names, not its values.</summary>
    public bool Keys { get; }
    /// <summary>The field of the same record the condition is on, or null when the mark has no condition.</summary>
    public FieldShape? When { get; internal set; }
    /// <summary>The values of <see cref="When"/>'s field (strings, or enum members' code names) for which it holds.</summary>
    public IReadOnlyList<string> Is { get; }

    internal string? WhenName { get; }

    /// <summary>The entry path a name points at, or null when the rule can't store it.</summary>
    public string? PathOf(string name) => Rule.PathOf(Folder, name);

    /// <summary>The name that points at the entry at <paramref name="path"/>, or null when no name does.</summary>
    public string? NameOf(string path) => Rule.NameOf(Folder, path);
}

/// <summary>
/// A field of a record type (Docs/Editor.md, "Records: shapes, drafts and marks"): its number, code
/// name, JSON name and display name, its value's shape, whether it must be given or what it defaults
/// to, and its marks.
/// </summary>
public sealed class FieldShape
{
    readonly FieldLayout _layout;
    readonly ReferenceShape[] _references;

    internal FieldShape(RecordShape record, int index, FieldLayout layout)
    {
        Record = record;
        Index = index;
        _layout = layout;
        var property = layout.Property;
        var where = $"{record.Type.Name}.{property.Name}";
        Value = ValueShape.Of(layout.Codec);
        DisplayName = DisplayNames.Of(property.Name);
        Help = property.GetCustomAttribute<HelpAttribute>(true)?.Text;
        Unit = property.GetCustomAttribute<UnitAttribute>(true)?.Unit;
        if (property.GetCustomAttribute<LimitsAttribute>(true) is { } limits)
        {
            if (!(limits.Min <= limits.Max))
                throw new InvalidOperationException($"{where}: limits need a min at most their max, not {limits.Min} and {limits.Max}");
            if (!Holds(Value, s => s.IsNumber))
                throw new InvalidOperationException($"{where}: [Limits] is for a field of numbers (or lists or maps of them), not {Value}");
            Limits = new FieldLimits(limits.Min, limits.Max, limits.MinExclusive, limits.MaxExclusive);
        }
        if (property.GetCustomAttribute<ChoicesAttribute>(true) is { } choices)
        {
            if (!Holds(Value, s => s.Kind == ValueKind.String))
                throw new InvalidOperationException($"{where}: [Choices] is for a field of strings (or lists or maps of them), not {Value}");
            Choices = choices.Choices;
        }
        if (property.IsDefined(typeof(ColourAttribute), true))
        {
            if (!Holds(Value, s => s.Kind is ValueKind.Vector3 or ValueKind.Vector4 or ValueKind.String, maps: false))
                throw new InvalidOperationException($"{where}: [Colour] is for a Vector3, a Vector4 or a string (or a list of them), not {Value}");
            IsColour = true;
        }
        if (property.GetCustomAttribute<BulkAttribute>(true) is { } bulk)
        {
            IsBulk = true;
            BulkSummary = bulk.Summary;
        }
        _references = [.. property.GetCustomAttributes<ReferencesAttribute>(true).Select(mark => Reference(mark, where))];
    }

    /// <summary>The record type it belongs to.</summary>
    public RecordShape Record { get; }

    /// <summary>Its place in <see cref="RecordShape.Fields"/>.</summary>
    public int Index { get; }

    /// <summary>Its field number.</summary>
    public int Number => _layout.Number;

    /// <summary>The property's code name (<c>MaxVoices</c>).</summary>
    public string Name => _layout.Property.Name;

    /// <summary>The name in the JSON form (<c>maxVoices</c>).</summary>
    public string JsonName => _layout.JsonName;

    /// <summary>The name a form shows: the code name's words, the first capitalized ("Max voices").</summary>
    public string DisplayName { get; }

    public ValueShape Value { get; }

    /// <summary>Must be given: a required property, a constructor parameter without a default, a non-nullable reference without one.</summary>
    public bool Required => _layout.Required;

    /// <summary>
    /// What it is when not given (its declaration's default; null when <see cref="Required"/>). A shared
    /// instance the serializer made to learn it: not to be changed.
    /// </summary>
    public object? Default => _layout.Default;

    public string? Help { get; }
    public string? Unit { get; }
    public FieldLimits? Limits { get; }
    public IReadOnlyList<string>? Choices { get; }
    public bool IsColour { get; }

    /// <summary>Bulk data (P21): a draft keeps the original object, a form shows <see cref="Summarize"/>.</summary>
    public bool IsBulk { get; }

    public string? BulkSummary { get; }

    /// <summary>The field's <see cref="ReferencesAttribute"/>s: the conditional ones first, then the one without a condition.</summary>
    public IReadOnlyList<ReferenceShape> References => _references;

    /// <summary>The field the references' condition is on, or null when none has one.</summary>
    public FieldShape? ReferenceCondition => _references.FirstOrDefault(r => r.When != null)?.When;

    /// <summary>The field's value in <paramref name="record"/> (a value of <see cref="Record"/>'s type).</summary>
    public object? Get(object record) => _layout.Get(record);

    /// <summary>
    /// The reference that holds when the condition's field (<see cref="ReferenceCondition"/>) has
    /// <paramref name="conditionValue"/>: the conditional mark whose values include it, else the one
    /// without a condition, else null (the strings name nothing then).
    /// </summary>
    public ReferenceShape? ReferenceFor(object? conditionValue)
    {
        var text = ConditionText(conditionValue);
        foreach (var reference in _references)
            if (reference.When != null && reference.Is.Contains(text, StringComparer.Ordinal))
                return reference;
        return _references.FirstOrDefault(r => r.When == null);
    }

    /// <summary>The reference that holds in <paramref name="record"/> (see <see cref="ReferenceFor(object?)"/>).</summary>
    public ReferenceShape? ReferenceIn(object record) => ReferenceFor(ReferenceCondition?.Get(record));

    /// <summary>A bulk value's summary: <see cref="BulkSummary"/> with the count of a list, map or byte array, or the value itself; null for null.</summary>
    public string? Summarize(object? value)
    {
        if (value == null)
            return null;
        return string.Format(CultureInfo.InvariantCulture, BulkSummary ?? "{0}", value is ICollection collection ? collection.Count : value);
    }

    public override string ToString() => $"{Record.Name}.{JsonName}";

    /// <summary>The text a condition compares: a string as it is, an enum member by its code name.</summary>
    static string? ConditionText(object? value) => value is Enum member ? member.ToString() : value as string;

    ReferenceShape Reference(ReferencesAttribute mark, string where)
    {
        if (!(mark.Keys ? IsMapWithStringKeys(Value) : Holds(Value, s => s.Kind == ValueKind.String)))
            throw new InvalidOperationException(mark.Keys
                ? $"{where}: [References] on keys is for a map with string keys, not {Value}"
                : $"{where}: [References] is for a field of strings (or lists or maps of them), not {Value}");
        var kind = RecordShape.Of(mark.Kind);
        var folder = mark.Folder ?? kind.Folder
            ?? throw new InvalidOperationException($"{where}: [References] to {kind.Name} needs a folder (the kind has none)");
        if (PackPath.Problem(folder) is { } problem)
            throw new InvalidOperationException($"{where}: the folder \"{folder}\" isn't a data path ({problem})");
        if ((mark.When == null) != (mark.Is == null) || mark.Is is { Length: 0 })
            throw new InvalidOperationException($"{where}: a condition needs both When and Is (with at least one value)");
        return new ReferenceShape(kind, folder, NameRule.Of(mark.Rule, kind.Rule, where), mark.Keys, mark.When, mark.Is ?? []);
    }

    /// <summary>Checks the references' conditions against the record's other fields, once all are made.</summary>
    internal void ResolveReferences(FieldShape[] fields)
    {
        var where = $"{Record.Type.Name}.{Name}";
        var conditional = _references.Where(r => r.WhenName != null).ToList();
        if (_references.Count(r => r.WhenName == null) > 1)
            throw new InvalidOperationException($"{where}: more than one [References] without a condition");
        if (conditional.Count == 0)
            return;
        if (conditional.Select(r => r.WhenName).Distinct().Count() > 1)
            throw new InvalidOperationException($"{where}: [References] conditions on different fields");
        var name = conditional[0].WhenName!;
        var when = fields.FirstOrDefault(f => f.Name == name)
            ?? throw new InvalidOperationException($"{where}: [References] has a condition on {name}, which isn't a field of {Record.Type.Name}");
        var shape = when.Value.Kind == ValueKind.Nullable ? when.Value.Element! : when.Value;
        if (shape.Kind is not (ValueKind.String or ValueKind.Enum))
            throw new InvalidOperationException($"{where}: [References] has a condition on {name}, which is a {shape}, not a string or an enum");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var reference in conditional)
        {
            foreach (var value in reference.Is)
            {
                if (shape.Kind == ValueKind.Enum && !shape.Members.Any(m => m.Name == value))
                    throw new InvalidOperationException($"{where}: [References] has a condition on {name} being {value}, which isn't a member of {shape.Type.Name}");
                if (!seen.Add(value))
                    throw new InvalidOperationException($"{where}: two [References] hold when {name} is {value}");
            }
            reference.When = when;
        }
        // The conditional ones first, in the order read, then the one without a condition.
        var ordered = conditional.Concat(_references.Where(r => r.WhenName == null)).ToArray();
        ordered.CopyTo(_references, 0);
    }

    /// <summary>Whether the values a field holds (itself, through nullables and lists, and with <paramref name="maps"/> a map's values) are of a kind.</summary>
    static bool Holds(ValueShape shape, Func<ValueShape, bool> kind, bool maps = true) => shape.Kind switch
    {
        ValueKind.Nullable or ValueKind.List => Holds(shape.Element!, kind, maps),
        ValueKind.Map or ValueKind.Pairs => maps && Holds(shape.Element!, kind, maps),
        _ => kind(shape),
    };

    static bool IsMapWithStringKeys(ValueShape shape)
    {
        if (shape.Kind == ValueKind.Nullable)
            shape = shape.Element!;
        return shape.Kind is ValueKind.Map or ValueKind.Pairs && shape.Key!.Kind == ValueKind.String;
    }
}

/// <summary>
/// Names shown for code names (M11-10): the words split where a capital starts one (after a small
/// letter or a digit, or as the last capital of a run of them followed by a small letter), the first
/// word capitalized, the others in small letters unless they have none (an acronym, a single capital):
/// <c>MaxVoices</c> "Max voices", <c>HDRFactor</c> "HDR factor", <c>ScaleX</c> "Scale X".
/// </summary>
static class DisplayNames
{
    public static string Of(string codeName)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        for (var i = 0; i < codeName.Length; i++)
        {
            var c = codeName[i];
            if (c == '_')
            {
                Flush();
                continue;
            }
            if (char.IsUpper(c) && word.Length > 0)
            {
                var previous = codeName[i - 1];
                var next = i + 1 < codeName.Length ? codeName[i + 1] : '\0';
                if (char.IsLower(previous) || char.IsDigit(previous) || char.IsUpper(previous) && char.IsLower(next))
                    Flush();
            }
            word.Append(c);
        }
        Flush();
        for (var i = 0; i < words.Count; i++)
        {
            var w = words[i];
            if (i == 0)
                words[i] = char.ToUpperInvariant(w[0]) + w[1..];
            else if (w.Any(char.IsLower))
                words[i] = w.ToLowerInvariant();
        }
        return string.Join(' ', words);

        void Flush()
        {
            if (word.Length > 0)
                words.Add(word.ToString());
            word.Clear();
        }
    }
}
