using Statventure.Records;

// Record types the import's comparison tests need that the guarded kinds lack (Docs/Editor.md, step 12): bulk data under a
// nullable, two records deep, in a union's subtype and in a map's values; a nullable member of two words. In the variants' namespace, which the layout
// guard leaves out.
namespace Statventure.Tests.Records.Variants;

/// <summary>A record with bulk data of its own.</summary>
[Record("variant.import-leaf", 1)]
public sealed record ImportLeaf
{
    [Field(1)] public string Name { get; init; } = "";
    [Field(2)] [Bulk("{0} bytes")] public byte[] Data { get; init; } = [];
}

/// <summary>Bulk data one record down, under a nullable and in a map's values.</summary>
[Record("variant.import-box", 1)]
public sealed record ImportBox
{
    [Field(1)] public ImportLeaf Inner { get; init; } = new();
    [Field(2)] public ImportLeaf? Maybe { get; init; }
    [Field(3)] public Dictionary<string, ImportLeaf> ByName { get; init; } = [];
    [Field(4)] public ImportShape Shape { get; init; } = new ImportPlain();
}

/// <summary>Bulk data two records down.</summary>
[Record("variant.import-deep", 1)]
public sealed record ImportDeep
{
    [Field(1)] public ImportBox Box { get; init; } = new();
}

/// <summary>A union one of whose subtypes holds bulk data.</summary>
[Record("variant.import-shape", 1)]
[Subtype(1, typeof(ImportPlain))]
[Subtype(2, typeof(ImportHeavy))]
public abstract record ImportShape;

[Record("variant.import-plain", 1)]
public sealed record ImportPlain : ImportShape
{
    [Field(1)] public int Size { get; init; }
    /// <summary>A nullable member of two words (none of the guarded kinds has one).</summary>
    [Field(2)] public Statventure.Scene.KeyEasing? Easing { get; init; }
}

[Record("variant.import-heavy", 1)]
public sealed record ImportHeavy : ImportShape
{
    [Field(1)] public ImportLeaf Leaf { get; init; } = new();
}
