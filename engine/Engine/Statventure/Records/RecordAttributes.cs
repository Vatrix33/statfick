namespace Statventure.Records;

// How a type declares itself a record (Docs/Assets.md, "Records (the serializer)"): a stable name and
// version on the type, a number on each serialized property, and for a union the subtypes it lists.

/// <summary>
/// Marks a record type: its stable <see cref="Name"/> (e.g. <c>audio.theme</c>) and the
/// <see cref="Version"/> of its layout. The current type carries the highest version; each past
/// version is a frozen copy nested in it (e.g. <c>ThemeSpec.V1</c>) with the same name, its own
/// version and an instance method <c>Upgrade()</c> returning the next version's type. An abstract
/// type with <see cref="SubtypeAttribute"/>s is a union.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
public sealed class RecordAttribute(string name, int version) : Attribute
{
    public string Name { get; } = name;
    public int Version { get; } = version;
}

/// <summary>
/// A serialized property and its number: fields are written in the order of their numbers, by
/// position in the binary form and by the property's camelCase name in the JSON form. On a positional
/// record put it on the property: <c>[property: Field(1)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FieldAttribute(int number) : Attribute
{
    public int Number { get; } = number;
}

/// <summary>
/// A subtype of a union (an abstract record type) and its tag: the binary form writes the tag before
/// the subtype's fields, the JSON form the subtype's record name as <c>"$type"</c>. The tag list is
/// part of the union's layout: adding a subtype is a new version of the union.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class SubtypeAttribute(int tag, Type type) : Attribute
{
    public int Tag { get; } = tag;
    public Type Type { get; } = type;
}
