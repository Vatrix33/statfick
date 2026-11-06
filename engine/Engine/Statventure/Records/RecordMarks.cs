namespace Statventure.Records;

// Marks: attributes in a record's definition that tell tools how to show and check its fields (the
// editor, M11: Docs/Editor.md, "Records: shapes, drafts and marks"; M11-10, M11-11, P9, P20, P21). None
// of them changes the data, the forms or the layout signatures. Read into RecordShape and FieldShape;
// the checks they make (limits, choices, references) are RecordChecks'. A mark on a field that can't
// hold what it is about is refused when the type's shape is first made, naming the type and field.

/// <summary>
/// On a record type: the name tools show for it and, for a kind of entry, the folder its entries are in
/// and the rule that makes an entry's name in it (P9): <c>[Kind("Sound effect", "audio/effects")]</c>.
/// Without it the name is made from the type's code name and the kind has no folder.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class KindAttribute(string displayName, string? folder = null) : Attribute
{
    public string DisplayName { get; } = displayName;
    /// <summary>The folder of the kind's entries (a data path, without a slash at the end), or null.</summary>
    public string? Folder { get; } = folder;
    /// <summary>A <see cref="NameRule"/> type with a public parameterless constructor; null: <see cref="PlainNames"/>.</summary>
    public Type? Rule { get; set; }
}

/// <summary>A short help text: the row's tooltip in the editor (on a field), or the kind's (on a record type).</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class HelpAttribute(string text) : Attribute
{
    public string Text { get; } = text;
}

/// <summary>The unit of a field's numbers, shown after them: "Hz", "s", "m", "°".</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class UnitAttribute(string unit) : Attribute
{
    public string Unit { get; } = unit;
}

/// <summary>
/// The numbers a field holds (itself, or each of a list's elements or a map's values) are between
/// <see cref="Min"/> and <see cref="Max"/>, each end included unless marked exclusive; NaN never is.
/// The editor's slider ends, and part of the engine's own check: the game refuses data outside them
/// too (G4), so a limit is declared only where the game's checks already enforce it.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class LimitsAttribute(double min, double max) : Attribute
{
    public double Min { get; } = min;
    public double Max { get; } = max;
    /// <summary>The minimum itself isn't allowed ("above 0").</summary>
    public bool MinExclusive { get; set; }
    /// <summary>The maximum itself isn't allowed ("below 1").</summary>
    public bool MaxExclusive { get; set; }
}

/// <summary>The strings a field holds (itself, or each of a list's elements or a map's values) are one of these: a select in the editor.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ChoicesAttribute(params string[] choices) : Attribute
{
    public IReadOnlyList<string> Choices { get; } = choices;
}

/// <summary>A field shown as a colour: a <c>Vector3</c> or <c>Vector4</c> (or a string), or a list of them.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ColourAttribute : Attribute;

/// <summary>
/// Bulk data (P21): kept as the original object in a draft, shown read-only as its summary,
/// <see cref="Summary"/> formatted with the count of a list, map or byte array, or with the value itself
/// otherwise: <c>[Bulk("{0:N0} vertices")]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class BulkAttribute(string? summary = null) : Attribute
{
    public string? Summary { get; } = summary;
}

/// <summary>
/// The strings a field holds (itself, each of a list's elements or a map's values, or with
/// <see cref="Keys"/> a map's keys) name entries of the record kind <see cref="Kind"/> (M11-11): each
/// string is made into an entry path by <see cref="Rule"/> in <see cref="Folder"/> (both the kind's own
/// unless given). With <see cref="When"/> and <see cref="Is"/> the mark holds only while another field
/// of the same record (named by its code name; a string or an enum, given by its member's code name)
/// has one of those values: <c>AudioCue.Name</c> names an effect when its kind is <c>Effect</c>, a
/// theme when it is <c>Music</c>. A field may have several conditional marks, all on the same field
/// and none sharing a value, and one without a condition, which holds when none of them does.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class ReferencesAttribute(Type kind) : Attribute
{
    /// <summary>The record type the names point at.</summary>
    public Type Kind { get; } = kind;
    /// <summary>The folder the names are in; null: the kind's folder (<see cref="KindAttribute"/>).</summary>
    public string? Folder { get; set; }
    /// <summary>A <see cref="NameRule"/> type; null: the kind's rule.</summary>
    public Type? Rule { get; set; }
    /// <summary>A map's keys are the names, not its values.</summary>
    public bool Keys { get; set; }
    /// <summary>The code name of the field of the same record whose value decides whether the mark holds.</summary>
    public string? When { get; set; }
    /// <summary>The values of <see cref="When"/>'s field for which the mark holds.</summary>
    public string[]? Is { get; set; }
}

/// <summary>
/// A record type's own check (P20): a static method of the type, <c>static void Name(T value,
/// CheckContext check)</c>, that reports what is wrong with a value at the path of what it names.
/// <see cref="RecordChecks"/> runs it on every value of the type, wherever it is held (a union's checks
/// on each of its subtypes' values too), with the data for checks across entries.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CheckAttribute : Attribute;
