using Statventure.Records;

// Two kinds for the New entry list's test of NotAPackKind (the user's Q26 of GM1, 2026-11-13; SessionEntriesTests): one
// marked, one not, alike otherwise. In the variants' namespace, which the layout guard leaves out.
namespace Statventure.Tests.Records.Variants;

[NotAPackKind]
[Record("variant.not-a-pack-kind", 1)]
public sealed record NotAPackKindVariant
{
    [Field(1)] public string Name { get; init; } = "";
}

[Record("variant.a-pack-kind", 1)]
public sealed record APackKindVariant
{
    [Field(1)] public string Name { get; init; } = "";
}
