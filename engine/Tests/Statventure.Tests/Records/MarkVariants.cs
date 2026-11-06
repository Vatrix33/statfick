using System.Numerics;
using Statventure.Records;

// Record types whose marks or checks break a rule on purpose, for RecordShapeTests (Docs/Editor.md,
// step 1). In the variants' namespace, which the layout guard leaves out.
namespace Statventure.Tests.Records.Variants;

[Record("variant.limits-on-text", 1)]
public sealed record LimitsOnText
{
    [Field(1)] [Limits(0, 1)] public string A { get; init; } = "";
}

[Record("variant.limits-backwards", 1)]
public sealed record LimitsBackwards
{
    [Field(1)] [Limits(2, 1)] public double A { get; init; }
}

[Record("variant.limits-nan", 1)]
public sealed record LimitsNaN
{
    [Field(1)] [Limits(0, double.NaN)] public double A { get; init; }
}

[Record("variant.limits-on-bools", 1)]
public sealed record LimitsOnBools
{
    [Field(1)] [Limits(0, 1)] public bool[] A { get; init; } = [];
}

[Record("variant.choices-on-numbers", 1)]
public sealed record ChoicesOnNumbers
{
    [Field(1)] [Choices("1")] public Dictionary<string, int> A { get; init; } = [];
}

[Record("variant.colour-on-number", 1)]
public sealed record ColourOnNumber
{
    [Field(1)] [Colour] public float A { get; init; }
}

[Record("variant.colour-in-map", 1)]
public sealed record ColourInMap
{
    [Field(1)] [Colour] public Dictionary<string, Vector3> A { get; init; } = [];
}

[Record("variant.references-on-number", 1)]
public sealed record ReferencesOnNumber
{
    [Field(1)] [References(typeof(Part))] public int A { get; init; }
}

[Record("variant.keys-of-a-list", 1)]
public sealed record KeysOfAList
{
    [Field(1)] [References(typeof(Part), Keys = true)] public string[] A { get; init; } = [];
}

[Record("variant.keys-not-strings", 1)]
public sealed record KeysNotStrings
{
    [Field(1)] [References(typeof(Part), Keys = true)] public Dictionary<int, string> A { get; init; } = [];
}

[Record("variant.reference-without-folder", 1)]
public sealed record ReferenceWithoutFolder
{
    [Field(1)] [References(typeof(Point))] public string A { get; init; } = "";
}

[Record("variant.reference-bad-folder", 1)]
public sealed record ReferenceBadFolder
{
    [Field(1)] [References(typeof(Part), Folder = "Parts")] public string A { get; init; } = "";
}

[Record("variant.reference-to-a-class", 1)]
public sealed record ReferenceToAClass
{
    [Field(1)] [References(typeof(string), Folder = "parts")] public string A { get; init; } = "";
}

[Record("variant.reference-bad-rule", 1)]
public sealed record ReferenceBadRule
{
    [Field(1)] [References(typeof(Part), Rule = typeof(string))] public string A { get; init; } = "";
}

/// <summary>An abstract name rule that has a public parameterless constructor.</summary>
public abstract class OpenRule : NameRule
{
    public OpenRule() { }
}

[Record("variant.rule-abstract", 1)]
public sealed record RuleAbstract
{
    [Field(1)] [References(typeof(Part), Rule = typeof(OpenRule))] public string A { get; init; } = "";
}

/// <summary>A name rule without a parameterless constructor.</summary>
public sealed class PrefixNames(string prefix) : NameRule
{
    public override string? PathOf(string folder, string name) => folder + "/" + prefix + name;
    public override string? NameOf(string folder, string path) => null;
}

[Record("variant.rule-without-constructor", 1)]
public sealed record RuleWithoutConstructor
{
    [Field(1)] [References(typeof(Part), Rule = typeof(PrefixNames))] public string A { get; init; } = "";
}

[Record("variant.when-without-is", 1)]
public sealed record WhenWithoutIs
{
    [Field(1)] [References(typeof(Part), When = nameof(B))] public string A { get; init; } = "";
    [Field(2)] public string B { get; init; } = "";
}

[Record("variant.is-without-when", 1)]
public sealed record IsWithoutWhen
{
    [Field(1)] [References(typeof(Part), Is = ["x"])] public string A { get; init; } = "";
}

[Record("variant.is-empty", 1)]
public sealed record IsEmpty
{
    [Field(1)] [References(typeof(Part), When = nameof(B), Is = [])] public string A { get; init; } = "";
    [Field(2)] public string B { get; init; } = "";
}

[Record("variant.two-otherwise", 1)]
public sealed record TwoOtherwise
{
    [Field(1)] [References(typeof(Part))] [References(typeof(Creature))] public string A { get; init; } = "";
}

[Record("variant.two-conditions", 1)]
public sealed record TwoConditions
{
    [Field(1)]
    [References(typeof(Part), When = nameof(B), Is = ["x"])]
    [References(typeof(Creature), When = nameof(C), Is = ["y"])]
    public string A { get; init; } = "";
    [Field(2)] public string B { get; init; } = "";
    [Field(3)] public string C { get; init; } = "";
}

[Record("variant.when-not-a-field", 1)]
public sealed record WhenNotAField
{
    [Field(1)] [References(typeof(Part), When = "Missing", Is = ["x"])] public string A { get; init; } = "";
}

[Record("variant.when-a-number", 1)]
public sealed record WhenANumber
{
    [Field(1)] [References(typeof(Part), When = nameof(B), Is = ["1"])] public string A { get; init; } = "";
    [Field(2)] public int B { get; init; }
}

[Record("variant.is-not-a-member", 1)]
public sealed record IsNotAMember
{
    [Field(1)] [References(typeof(Part), When = nameof(B), Is = ["Pale", "Purple"])] public string A { get; init; } = "";
    [Field(2)] public Shade B { get; init; }
}

[Record("variant.is-twice", 1)]
public sealed record IsTwice
{
    [Field(1)]
    [References(typeof(Part), When = nameof(B), Is = ["x", "y"])]
    [References(typeof(Creature), When = nameof(B), Is = ["y"])]
    public string A { get; init; } = "";
    [Field(2)] public string? B { get; init; }
}

[Kind("Bad", "Bad Folder")]
[Record("variant.kind-bad-folder", 1)]
public sealed record KindBadFolder
{
    [Field(1)] public int A { get; init; }
}

// Shapes the test kinds don't have (they are in the layout guard, these aren't).

/// <summary>A rule that makes a path whatever the folder.</summary>
public sealed class AnyNames : NameRule
{
    public override string? PathOf(string folder, string name) => "any/" + name;
    public override string? NameOf(string folder, string path) => path;
}

/// <summary>A kind without a folder whose rule would make paths: it still has none.</summary>
[Kind("Loose", Rule = typeof(AnyNames))]
[Record("variant.loose-kind", 1)]
public sealed record LooseKind
{
    [Field(1)] public int A { get; init; }
}

/// <summary>Members declared out of their numbers' order.</summary>
public enum Rank { Third = 3, First = 1, Second = 2 }

/// <summary>Lists and maps of maps, and an enum declared out of order.</summary>
[Record("variant.nested-maps", 1)]
public sealed record NestedMaps
{
    [Field(1)] public IReadOnlyList<Dictionary<Shade, int>> Tables { get; init; } = [];
    [Field(2)] public Dictionary<string, Dictionary<Rank, string>> ByName { get; init; } = [];
    [Field(3)] public Rank Rank { get; init; } = Rank.First;
}

[Record("variant.check-not-static", 1)]
public sealed record CheckNotStatic
{
    [Field(1)] public int A { get; init; }

    [Check]
    void Wrong(CheckNotStatic value, CheckContext check) { }
}

[Record("variant.check-other-type", 1)]
public sealed record CheckOtherType
{
    [Field(1)] public int A { get; init; }

    [Check]
    static void Wrong(Part value, CheckContext check) { }
}

[Record("variant.check-no-context", 1)]
public sealed record CheckNoContext
{
    [Field(1)] public int A { get; init; }

    [Check]
    static void Wrong(CheckNoContext value) { }
}

[Record("variant.check-returns", 1)]
public sealed record CheckReturns
{
    [Field(1)] public int A { get; init; }

    [Check]
    static bool Wrong(CheckReturns value, CheckContext check) => true;
}

[Record("variant.check-other-context", 1)]
public sealed record CheckOtherContext
{
    [Field(1)] public int A { get; init; }

    [Check]
    static void Wrong(CheckOtherContext value, object check) { }
}

[Record("variant.check-generic", 1)]
public sealed record CheckGeneric
{
    [Field(1)] public int A { get; init; }

    [Check]
    static void Wrong<T>(CheckGeneric value, CheckContext check) { }
}
