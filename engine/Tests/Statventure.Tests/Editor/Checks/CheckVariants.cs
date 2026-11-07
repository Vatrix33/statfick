using Statventure.Records;

// Record types the checks' tests need that the guarded kinds lack (Docs/Editor.md, step 10): a kind whose own check
// throws (a port kind's check with a bug), alone or only across entries; references in a map with enum keys. In the
// variants' namespace, which the layout guard leaves out.
namespace Statventure.Tests.Records.Variants;

[Record("variant.breaking-check", 1)]
public sealed record BreakingCheck
{
    /// <summary>"always": the check throws; "across": it throws when checked across entries (given the references); anything else: no problem.</summary>
    [Field(1)] public string Breaks { get; init; } = "";

    [Check]
    static void Breaking(BreakingCheck value, CheckContext check)
    {
        if (value.Breaks == "always" || value.Breaks == "across" && check.References is not null)
            throw new InvalidOperationException("the check broke");
    }
}

/// <summary>Parts by an enum's members: references in records held by a map with enum keys (paths name a key by its JSON name).</summary>
[Record("variant.parts-by-shade", 1)]
public sealed record PartsByShade
{
    [Field(1)] public Dictionary<Shade, Part> Parts { get; init; } = [];
}
