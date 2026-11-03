using Statventure.Records;

// Record types that break a rule on purpose, or vary one detail of a layout, for the tests that check
// the rules and the layout signatures. The layout guard (RecordLayoutTests) leaves this namespace out.
namespace Statventure.Tests.Records.Variants;

[Record("variant.no-past-version", 2)]
public sealed record NoPastVersion
{
    [Field(1)] public int A { get; init; }
}

[Record("variant.past-too-new", 2)]
public sealed record PastTooNew
{
    [Field(1)] public int A { get; init; }

    [Record("variant.past-too-new", 2)]
    public sealed record V2
    {
        [Field(1)] public int A { get; init; }
    }
}

[Record("variant.no-upgrade", 2)]
public sealed record NoUpgrade
{
    [Field(1)] public int A { get; init; }

    [Record("variant.no-upgrade", 1)]
    public sealed record V1
    {
        [Field(1)] public int A { get; init; }
    }
}

[Record("variant.bad-upgrade", 2)]
public sealed record BadUpgrade
{
    [Field(1)] public int A { get; init; }

    [Record("variant.bad-upgrade", 1)]
    public sealed record V1
    {
        [Field(1)] public int A { get; init; }

        public int Upgrade() => A;
    }
}

[Record("variant.same-number", 1)]
public sealed record SameNumber
{
    [Field(1)] public int A { get; init; }
    [Field(1)] public int B { get; init; }
}

[Record("variant.same-json-name", 1)]
public sealed record SameJsonName
{
    [Field(1)] public int Value { get; init; }
    [Field(2)] public int value { get; init; }
}

[Record("variant.zero-number", 1)]
public sealed record ZeroNumber
{
    [Field(0)] public int A { get; init; }
}

[Flags]
public enum Senses { None = 0, Sight = 1, Smell = 2 }

[Record("variant.flags-field", 1)]
public sealed record FlagsField
{
    [Field(1)] public Senses A { get; init; }
}

public enum Alias { One = 1, Uno = 1 }

[Record("variant.alias-field", 1)]
public sealed record AliasField
{
    [Field(1)] public Alias A { get; init; } = Alias.One;
}

[Record("variant.object-field", 1)]
public sealed record ObjectField
{
    [Field(1)] public object A { get; init; } = 1;
}

[Record("variant.bool-keys", 1)]
public sealed record BoolKeys
{
    [Field(1)] public Dictionary<bool, int> A { get; init; } = [];
}

#nullable disable
[Record("variant.oblivious-field", 1)]
public sealed record ObliviousField
{
    [Field(1)] public string A { get; init; } = "";
}
#nullable restore

[Record("variant.no-setter", 1)]
public sealed class NoSetter(int x)
{
    [Field(1)] public int B { get; } = x;
}

[Record("variant.computed-field", 1)]
public sealed class ComputedField
{
    [Field(1)] public int A { get; init; }
    [Field(2)] public int B => A * 2;
}

public sealed record NotARecord
{
    [Field(1)] public int A { get; init; }
}

[Record("variant.Upper-Case", 1)]
public sealed record BadName
{
    [Field(1)] public int A { get; init; }
}

[Record("variant.concrete-union", 1)]
[Subtype(1, typeof(Member))]
public class ConcreteUnion
{
    [Record("variant.concrete-union.member", 1)]
    public sealed class Member : ConcreteUnion
    {
    }
}

[Record("variant.foreign-subtype", 1)]
[Subtype(1, typeof(Circle))]
public abstract record ForeignSubtype
{
    public sealed record Member : ForeignSubtype;
}

[Record("variant.same-tag", 1)]
[Subtype(1, typeof(A))]
[Subtype(1, typeof(B))]
public abstract record SameTag
{
    [Record("variant.same-tag.a", 1)]
    public sealed record A : SameTag;

    [Record("variant.same-tag.b", 1)]
    public sealed record B : SameTag;
}

[Record("variant.nested-union", 1)]
[Subtype(1, typeof(Inner))]
public abstract record NestedUnion
{
    [Record("variant.nested-union.inner", 1)]
    [Subtype(1, typeof(Member))]
    public abstract record Inner : NestedUnion;

    [Record("variant.nested-union.member", 1)]
    public sealed record Member : Inner;
}

[Record("variant.back-to-plain", 2)]
public sealed record BackToPlain
{
    [Field(1)] public int A { get; init; }

    [Record("variant.back-to-plain", 1)]
    [Subtype(1, typeof(BackToPlain))]
    public abstract record V1;
}

[Record("variant.abstract", 1)]
public abstract record Abstract
{
    public sealed record Member : Abstract;
}

[Record("variant.twin", 1)]
public sealed record TwinA;

[Record("variant.twin", 1)]
public sealed record TwinB;

/// <summary>A plain record (version 1) that became a union (version 2): its upgrade gives a subtype.</summary>
[Record("variant.became-union", 2)]
[Subtype(1, typeof(Round))]
public abstract record BecameUnion
{
    [Record("variant.became-union", 1)]
    public sealed record V1
    {
        [Field(1)] public double Radius { get; init; }

        public BecameUnion Upgrade() => new Round { Radius = Radius };
    }

    [Record("variant.became-union.round", 1)]
    public sealed record Round : BecameUnion
    {
        [Field(1)] public double Radius { get; init; }
    }
}

// Layout signature variants: each differs from Sig in one detail (RecordLayoutTests).

public enum SigShade : byte { Pale, Dark }
public enum SigShadeRenamed : byte { Pale, Darker }
public enum SigShadeRenumbered : byte { Pale, Dark = 2 }
public enum SigShadeWider : ushort { Pale, Dark }
public enum SigShadeLonger : byte { Pale, Dark, Golden }

[Record("variant.sig", 1)]
public sealed record Sig
{
    [Field(1)] public int A { get; init; }
    [Field(2)] public SigShade B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-renamed", 1)]
public sealed record SigRenamed
{
    [Field(1)] public int Aa { get; init; }
    [Field(2)] public SigShade B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-renumbered", 1)]
public sealed record SigRenumbered
{
    [Field(1)] public int A { get; init; }
    [Field(5)] public SigShade B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-retyped", 1)]
public sealed record SigRetyped
{
    [Field(1)] public long A { get; init; }
    [Field(2)] public SigShade B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-nullable", 1)]
public sealed record SigNullable
{
    [Field(1)] public int? A { get; init; }
    [Field(2)] public SigShade B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-not-nullable", 1)]
public sealed record SigNotNullable
{
    [Field(1)] public int A { get; init; }
    [Field(2)] public SigShade B { get; init; }
    [Field(3)] public string C { get; init; } = "";
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-required", 1)]
public sealed record SigRequired
{
    [Field(1)] public required int A { get; init; }
    [Field(2)] public SigShade B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-default", 1)]
public sealed record SigDefault
{
    [Field(1)] public int A { get; init; }
    [Field(2)] public SigShade B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 2;
}

[Record("variant.sig-list", 1)]
public sealed record SigList
{
    [Field(1)] public int A { get; init; }
    [Field(2)] public SigShade B { get; init; }
    [Field(3)] public string[]? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-member-renamed", 1)]
public sealed record SigMemberRenamed
{
    [Field(1)] public int A { get; init; }
    [Field(2)] public SigShadeRenamed B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-member-renumbered", 1)]
public sealed record SigMemberRenumbered
{
    [Field(1)] public int A { get; init; }
    [Field(2)] public SigShadeRenumbered B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-enum-wider", 1)]
public sealed record SigEnumWider
{
    [Field(1)] public int A { get; init; }
    [Field(2)] public SigShadeWider B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-member-added", 1)]
public sealed record SigMemberAdded
{
    [Field(1)] public int A { get; init; }
    [Field(2)] public SigShadeLonger B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
}

[Record("variant.sig-field-added", 1)]
public sealed record SigFieldAdded
{
    [Field(1)] public int A { get; init; }
    [Field(2)] public SigShade B { get; init; }
    [Field(3)] public string? C { get; init; }
    [Field(4)] public double D { get; init; } = 1;
    [Field(6)] public bool E { get; init; }
}

[Record("variant.sig-same", 1)]
public sealed class SigSame
{
    // The same layout declared differently (a class, its properties in another order): the same signature.
    [Field(4)] public double D { get; init; } = 1;
    [Field(3)] public string? C { get; init; }
    [Field(2)] public SigShade B { get; init; }
    [Field(1)] public int A { get; init; }
}

// More rules: a frozen copy twice, tags below 1, a subtype listed twice, a past union version listing
// a type that isn't a subtype any more, a field whose absence has no meaning, empties at the end.

[Record("variant.two-past-ones", 2)]
public sealed record TwoPastOnes
{
    [Field(1)] public int A { get; init; }

    [Record("variant.two-past-ones", 1)]
    public sealed record V1
    {
        [Field(1)] public int A { get; init; }

        public TwoPastOnes Upgrade() => new() { A = A };
    }

    [Record("variant.two-past-ones", 1)]
    public sealed record Also
    {
        [Field(1)] public int A { get; init; }

        public TwoPastOnes Upgrade() => new() { A = A };
    }
}

[Record("variant.zero-tag", 1)]
[Subtype(0, typeof(Member))]
public abstract record ZeroTag
{
    [Record("variant.zero-tag.member", 1)]
    public sealed record Member : ZeroTag;
}

[Record("variant.listed-twice", 1)]
[Subtype(1, typeof(Member))]
[Subtype(2, typeof(Member))]
public abstract record ListedTwice
{
    [Record("variant.listed-twice.member", 1)]
    public sealed record Member : ListedTwice;
}

/// <summary>Version 1 listed <see cref="Stray"/>, which isn't a <see cref="Drifted"/>.</summary>
[Record("variant.drifted", 2)]
[Subtype(1, typeof(Kept))]
public abstract record Drifted
{
    [Record("variant.drifted", 1)]
    [Subtype(1, typeof(Kept))]
    [Subtype(2, typeof(Stray))]
    public abstract record V1;

    [Record("variant.drifted.kept", 1)]
    public sealed record Kept : Drifted;

    [Record("variant.drifted.stray", 1)]
    public sealed record Stray
    {
        [Field(1)] public int A { get; init; }
    }
}

[Record("variant.holder", 1)]
public sealed record Holder
{
    [Field(1)] public Drifted? Value { get; init; }
}

[Record("variant.no-default", 1)]
public sealed class NoDefault
{
    [Field(1)] public string Name { get; init; } = null!;
}

/// <summary>A positional record with a nullable parameter that has no default (as a state's clip).</summary>
[Record("variant.positional-nullable", 1)]
public sealed record PositionalNullable([property: Field(1)] string Name, [property: Field(2)] string? Clip);

[Record("variant.empties-at-end", 1)]
public sealed record EmptiesAtEnd
{
    [Field(1)] public Empty[] Items { get; init; } = [];
}
