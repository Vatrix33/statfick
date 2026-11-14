using Statventure.Records;

// Shapes for RecordChecksPruningTests (GM1 step 6c): records whose checks sit only deep down (in a list of maps of unions,
// in one arm of a union only, in a recursive shape, through nullables and pairs), one recursive shape with nothing to check,
// open shapes (a union's subtype it doesn't list, a record type that isn't sealed), and a mark that doesn't fit below. In the
// variants' namespace, which the layout guard leaves out.
namespace Statventure.Tests.Records.Variants;

/// <summary>Nothing to check at any depth, recursive: never walked by the pruned check.</summary>
[Record("variant.prune-quiet", 1)]
[Kind("Prune quiet", "prune/quiet")]
public sealed record PruneQuiet
{
    [Field(1)] public string Name { get; init; } = "";
    [Field(2)] public double Weight { get; init; }
    [Field(3)] public IReadOnlyList<PruneQuiet> Below { get; init; } = [];
}

/// <summary>A union whose check sits in one subtype only (and one subtype in code it doesn't list).</summary>
[Record("variant.prune-arm", 1)]
[Subtype(1, typeof(PruneQuietArm))]
[Subtype(2, typeof(PruneLoudArm))]
public abstract record PruneArm;

[Record("variant.prune-quiet-arm", 1)]
public sealed record PruneQuietArm : PruneArm
{
    [Field(1)] public string Text { get; init; } = "";
}

[Record("variant.prune-loud-arm", 1)]
public sealed record PruneLoudArm : PruneArm
{
    [Field(1)] public double Level { get; init; }

    [Check]
    static void Loud(PruneLoudArm arm, CheckContext check)
    {
        if (arm.Level > 1)
            check.Report(RecordPath.Root.Field("level"), $"too loud ({arm.Level})");
    }
}

/// <summary>A subtype of <see cref="PruneArm"/> that the union doesn't list (no pack can hold it; code can make one).</summary>
[Record("variant.prune-unlisted-arm", 1)]
public sealed record PruneUnlistedArm : PruneArm
{
    [Field(1)] public string Note { get; init; } = "";

    [Check]
    static void Unlisted(PruneUnlistedArm arm, CheckContext check) => check.Report($"unlisted: {arm.Note}");
}

/// <summary>A union with a check of its own and a subtype with none.</summary>
[Record("variant.prune-checked-union", 1)]
[Subtype(1, typeof(PruneCheckedArm))]
public abstract record PruneCheckedUnion
{
    [Field(1)] public string Tag { get; init; } = "";

    [Check]
    static void Tagged(PruneCheckedUnion union, CheckContext check)
    {
        if (union.Tag.Length == 0)
            check.Report("no tag");
    }
}

[Record("variant.prune-checked-arm", 1)]
public sealed record PruneCheckedArm : PruneCheckedUnion;

/// <summary>A record type that isn't sealed, with nothing to check, and one deriving from it that has a check.</summary>
[Record("variant.prune-open-base", 1)]
public record PruneOpenBase
{
    [Field(1)] public string Name { get; init; } = "";
}

[Record("variant.prune-open-child", 1)]
public sealed record PruneOpenChild : PruneOpenBase
{
    [Check]
    static void Child(PruneOpenChild child, CheckContext check) => check.Report($"the child {child.Name}");
}

/// <summary>Single marks, each on a closed record of its own.</summary>
[Record("variant.prune-limited", 1)]
public sealed record PruneLimited
{
    [Field(1)] [Limits(0, 1)] public double Amount { get; init; }
}

[Record("variant.prune-chosen", 1)]
public sealed record PruneChosen
{
    [Field(1)] [Choices("red", "blue")] public string Colour { get; init; } = "red";
}

[Record("variant.prune-naming", 1)]
public sealed record PruneNaming
{
    [Field(1)] [References(typeof(PruneQuiet))] public string? Names { get; init; }
}

/// <summary>A leaf holding each single mark through a nullable.</summary>
[Record("variant.prune-leaf", 1)]
public sealed record PruneLeaf
{
    [Field(1)] public PruneLimited? Limited { get; init; }
    [Field(2)] public PruneChosen? Chosen { get; init; }
    [Field(3)] public PruneNaming? Naming { get; init; }
}

/// <summary>A recursive shape whose marks sit only in its leaves (the leaf field first, the recursion after it).</summary>
[Record("variant.prune-tree", 1)]
public sealed record PruneTree
{
    [Field(1)] public PruneLeaf? Leaf { get; init; }
    [Field(2)] public IReadOnlyList<PruneTree> Children { get; init; } = [];
    [Field(3)] public PruneQuiet? Quiet { get; init; }
}

/// <summary>Checks deep down: in a list of maps of unions, in pairs, under a union with a check of its own, under an open type.</summary>
[Record("variant.prune-deep", 1)]
public sealed record PruneDeep
{
    [Field(1)] public IReadOnlyList<IReadOnlyDictionary<string, PruneArm>> Groups { get; init; } = [];
    [Field(2)] public IReadOnlyList<KeyValuePair<string, PruneTree>> Pairs { get; init; } = [];
    [Field(3)] public PruneCheckedUnion? Checked { get; init; }
    [Field(4)] public PruneOpenBase? Open { get; init; }
    [Field(5)] public IReadOnlyList<PruneQuiet> Quiet { get; init; } = [];
}

/// <summary>Checks only below a union (in a list of maps of it): nothing else to check.</summary>
[Record("variant.prune-groups", 1)]
public sealed record PruneGroups
{
    [Field(1)] public IReadOnlyList<IReadOnlyDictionary<string, PruneArm>> Groups { get; init; } = [];
}

/// <summary>Checks only below a type that isn't sealed: nothing else to check.</summary>
[Record("variant.prune-open-holder", 1)]
public sealed record PruneOpenHolder
{
    [Field(1)] public PruneOpenBase? Item { get; init; }
}

/// <summary>A mark that doesn't fit (<see cref="LimitsOnText"/>) below a record with nothing else to check.</summary>
[Record("variant.prune-broken", 1)]
public sealed record PruneBroken
{
    [Field(1)] public LimitsOnText? Bad { get; init; }
}
