using Statventure.Records;

// Record types the form's tests need that the guarded kinds lack (Docs/Editor.md, step 9): a union whose subtypes
// share a field's name with different kinds of value. In the variants' namespace, which the layout guard leaves out.
namespace Statventure.Tests.Records.Variants;

[Record("variant.switch", 1)]
[Subtype(1, typeof(SwitchCount))]
[Subtype(2, typeof(SwitchName))]
public abstract record Switch
{
    [Field(1)] public string Tag { get; init; } = "";
}

/// <summary>A subtype whose <c>value</c> is a number.</summary>
[Record("variant.switch-count", 1)]
public sealed record SwitchCount : Switch
{
    [Field(2)] public int Value { get; init; }
}

/// <summary>A subtype whose <c>value</c> is a text.</summary>
[Record("variant.switch-name", 1)]
public sealed record SwitchName : Switch
{
    [Field(2)] public string Value { get; init; } = "";
}

/// <summary>A record holding the union (what the form shows).</summary>
[Record("variant.switch-holder", 1)]
public sealed record SwitchHolder
{
    [Field(1)] public Switch Switch { get; init; } = new SwitchCount();
}
