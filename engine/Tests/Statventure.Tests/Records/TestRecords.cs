using System.Numerics;
using Statventure.Records;

namespace Statventure.Tests.Records;

// Record types of the serializer's tests (Docs/Assets.md, step 2): one with every kind of field, a
// union, and a family with several versions (test.creature 1–3, test.limb 1–2, the union test.move
// 1–2 with test.walk, test.leap 1–2, test.fly) whose past versions have committed fixtures in
// Data/Records/Fixtures. Their layouts are recorded in Data/Records/Layouts like every record type's.
// They also carry marks and checks for the record shapes' tests (Docs/Editor.md, step 1): marks change
// no layout signature.

public enum Shade : byte { Pale, Dark = 5, Golden = 200 }

public enum Pace { Slow = -1, Steady, Fast }

public enum Huge : ulong { Small = 1, Big = 1UL << 63 }

[Kind("Body part", "parts")]
[Record("test.part", 1)]
public sealed record Part
{
    /// <summary>A creature's name when the part is dark or golden, else another part's.</summary>
    [Field(1)]
    [References(typeof(Part))]
    [References(typeof(Creature), When = nameof(Shade), Is = [nameof(Shade.Dark), nameof(Shade.Golden)])]
    public required string Name { get; init; }
    [Field(2)] [Unit("kg")] [Limits(0, double.PositiveInfinity, MinExclusive = true)] public double Weight { get; init; } = 1;
    [Field(3)] public Shade Shade { get; init; }
}

/// <summary>A name rule like the music's (S6-1): a capital letter is stored as '_' and the small letter.</summary>
public sealed class CapitalNames : NameRule
{
    public override string? PathOf(string folder, string name)
    {
        var stored = string.Concat(name.Select(c => char.IsAsciiLetterUpper(c) ? "_" + char.ToLowerInvariant(c) : c.ToString()));
        return name.Length > 0 && !name.Contains('/') && Statventure.Packs.PackPath.IsValid(folder + "/" + stored) ? folder + "/" + stored : null;
    }

    public override string? NameOf(string folder, string path)
    {
        if (!path.StartsWith(folder + "/", StringComparison.Ordinal))
            return null;
        var stored = path[(folder.Length + 1)..];
        var name = new System.Text.StringBuilder();
        for (var i = 0; i < stored.Length; i++)
            name.Append(stored[i] == '_' && i + 1 < stored.Length ? char.ToUpperInvariant(stored[++i]) : stored[i]);
        return name.ToString();
    }
}

/// <summary>A positional record struct (as the model's joint indices).</summary>
[Record("test.joint", 1)]
public readonly record struct Joint([property: Field(1)] ushort A, [property: Field(2)] ushort B);

/// <summary>A positional record with parameter defaults and an initialized property beside them.</summary>
[Record("test.point", 1)]
public sealed record Point([property: Field(1)] float X, [property: Field(2)] float Y = 2, [property: Field(4)] Pace Pace = Pace.Fast,
    [property: Field(5)] string? Label = null)
{
    [Field(3)] public bool Visible { get; init; } = true;
}

/// <summary>A record without fields.</summary>
[Record("test.empty", 1)]
public sealed record Empty;

/// <summary>A union whose base has a field every subtype shares.</summary>
[Record("test.shape", 1)]
[Subtype(1, typeof(Circle))]
[Subtype(2, typeof(Square))]
[Subtype(3, typeof(Dot))]
public abstract record Shape
{
    [Field(1)] public string Tint { get; init; } = "ink";
}

[Record("test.circle", 1)]
public sealed record Circle : Shape
{
    [Field(2)] public required double Radius { get; init; }
}

/// <summary>A subtype holding its own union (records nest).</summary>
[Record("test.square", 1)]
public sealed record Square : Shape
{
    [Field(2)] public double Side { get; init; }
    [Field(3)] public Shape? Inner { get; init; }
}

[Record("test.dot", 1)]
public sealed record Dot : Shape;

/// <summary>Every kind of field the serializer knows.</summary>
[Record("test.kinds", 1)]
public sealed class Kinds
{
    [Field(1)] [Help("A flag.")] public bool Flag { get; init; }
    [Field(2)] public sbyte Int8 { get; init; }
    [Field(3)] [Limits(1, 200, MaxExclusive = true)] public byte UInt8 { get; init; }
    [Field(4)] public short Int16 { get; init; }
    [Field(5)] public ushort UInt16 { get; init; }
    [Field(6)] public int Int32 { get; init; }
    [Field(7)] public uint UInt32 { get; init; }
    [Field(8)] public long Int64 { get; init; }
    [Field(9)] public ulong UInt64 { get; init; }
    [Field(10)] [Limits(-1, 1)] public float Float32 { get; init; }
    [Field(11)] public double Float64 { get; init; }
    /// <summary>A creature's name when the motto is "creature", a spare part's when "part" or "piece", else nothing.</summary>
    [Field(12)]
    [References(typeof(Creature), When = nameof(Motto), Is = ["creature"])]
    [References(typeof(Part), Folder = "spare-parts", When = nameof(Motto), Is = ["part", "piece"])]
    public required string Text { get; init; }
    [Field(13)] [Colour] public string? Note { get; init; }
    [Field(14)] public int? Count { get; init; }
    [Field(15)] [Limits(0, 1)] public double? Ratio { get; init; } = 0.5;
    [Field(16)] public Shade Shade { get; init; }
    [Field(17)] public Pace? Pace { get; init; }
    [Field(18)] public Huge Huge { get; init; } = Huge.Small;
    [Field(19)] [Bulk("{0} bytes")] public byte[] Bytes { get; init; } = [];
    [Field(20)] [Bulk] public byte[]? MaybeBytes { get; init; }
    [Field(21)] [Limits(-5, 5)] public int[] Ints { get; init; } = [];
    [Field(22)] public float[] Floats { get; init; } = [];
    [Field(23)] public double[] Doubles { get; init; } = [];
    [Field(24)] public bool[] Bools { get; init; } = [];
    [Field(25)] public ushort[] Shorts { get; init; } = [];
    [Field(26)] public long[] Longs { get; init; } = [];
    [Field(27)] public Vector2 V2 { get; init; }
    [Field(28)] [Colour] public Vector3 V3 { get; init; }
    [Field(29)] [Colour] public Vector4 V4 { get; init; }
    [Field(30)] [Bulk] public Quaternion Rotation { get; init; } = Quaternion.Identity;
    [Field(31)] public Matrix4x4 Matrix { get; init; } = Matrix4x4.Identity;
    [Field(32)] [Bulk("{0:N0} points")] [Unit("m")] public Vector3[] Points { get; init; } = [];
    [Field(33)] public Matrix4x4[] Matrices { get; init; } = [];
    [Field(34)] public Quaternion? MaybeRotation { get; init; }
    [Field(35)] [Choices("a", "", "β")] public string[] Words { get; init; } = [];
    [Field(36)] [Choices("x", "")] public string?[] Gaps { get; init; } = [];
    [Field(37)] [References(typeof(Creature))] public List<string> Names { get; init; } = [];
    [Field(38)] public IReadOnlyList<int> ReadOnlyInts { get; init; } = [];
    [Field(39)] [Limits(-10, 10)] public double[][] Jagged { get; init; } = [];
    [Field(40)] [Limits(0, 9)] public int[]? MaybeInts { get; init; }
    [Field(41)] public List<float> FloatList { get; init; } = [];
    [Field(42)] public Dictionary<string, int> Scores { get; init; } = [];
    [Field(43)] [Bulk("{0} parts")] public IReadOnlyDictionary<string, Part> Parts { get; init; } = new Dictionary<string, Part>();
    [Field(44)] [References(typeof(Part), Keys = true)] public IReadOnlyList<KeyValuePair<string, string>> Pairs { get; init; } = [];
    [Field(45)] [References(typeof(Kinds), Folder = "kinds/sets", Rule = typeof(PathNames))] public Dictionary<int, string?> ByNumber { get; init; } = [];
    [Field(46)] [Limits(-1, 1, MinExclusive = true)] public Dictionary<Shade, double> ByShade { get; init; } = [];
    [Field(47)] [References(typeof(Part), Keys = true)] public OrderedDictionary<string, int>? Ordered { get; init; }
    [Field(48)] public Dictionary<ulong, Part?> Sparse { get; init; } = [];
    [Field(49)] public required Part Part { get; init; }
    [Field(50)] public Part? MaybePart { get; init; }
    [Field(51)] public Joint Joint { get; init; }
    [Field(52)] public Joint[] Joints { get; init; } = [];
    [Field(53)] public Joint? MaybeJoint { get; init; }
    [Field(54)] public Point Point { get; init; } = new(0);
    [Field(55)] public IReadOnlyList<Part?> MaybeParts { get; init; } = [];
    [Field(56)] public Shape Shape { get; init; } = new Dot();
    [Field(57)] public Shape[] Shapes { get; init; } = [];
    [Field(58)] public Shape? MaybeShape { get; init; }
    [Field(59)] public Empty[] Empties { get; init; } = [];
    [Field(60)] public Dictionary<string, Shape> ShapesByName { get; init; } = [];
    [Field(61)] public Pace[] Paces { get; init; } = [];
    [Field(62)] public KeyValuePair<Shade, int>[] ShadePairs { get; init; } = [];
    [Field(63)] public string Motto { get; init; } = "none";
}

public enum LimbKind { Arm, Leg, Wing, Tail }

/// <summary>Version 2 turned the kind into an enum and the length into a double.</summary>
[Record("test.limb", 2)]
public sealed record Limb
{
    [Field(1)] public required LimbKind Kind { get; init; }
    [Field(2)] [Unit("m")] public double Length { get; init; } = 1;

    /// <summary>Checks of a nested record type run wherever it is held.</summary>
    [Check]
    static void TailsAreShort(Limb limb, CheckContext check)
    {
        if (limb.Kind == LimbKind.Tail && limb.Length > 3)
            check.Report(RecordPath.Root.Field(nameof(Length)), "a tail is at most 3 m long");
    }

    [Record("test.limb", 1)]
    public sealed record V1
    {
        [Field(1)] public required string Kind { get; init; }
        [Field(2)] public float Length { get; init; } = 1;

        public Limb Upgrade() => new()
        {
            Kind = Kind switch
            {
                "arm" => LimbKind.Arm,
                "leg" => LimbKind.Leg,
                "wing" => LimbKind.Wing,
                "tail" => LimbKind.Tail,
                _ => throw new InvalidDataException($"no limb kind {Kind}"),
            },
            Length = Length,
        };
    }
}

/// <summary>
/// Version 2 dropped the leg count (legs became limbs) and added the speed in steps per turn;
/// version 3 measures the speed in metres per second (half the steps) and adds the title and moves.
/// </summary>
[Kind("Creature", "creatures", Rule = typeof(CapitalNames))]
[Help("A creature of the tests.")]
[Record("test.creature", 3)]
public sealed record Creature
{
    [Field(1)] [Help("What it is called.")] public required string Name { get; init; }
    [Field(2)] public IReadOnlyList<Limb> Limbs { get; init; } = [];
    [Field(3)] [Unit("m/s")] [Limits(0, 10)] public double Speed { get; init; } = 0.5;
    [Field(4)] [Choices("the Grim", "the Bold")] public string? Title { get; init; }
    [Field(5)] public IReadOnlyList<Move> Moves { get; init; } = [];

    /// <summary>A check across entries: with the data, a creature's name is on its roster.</summary>
    [Check]
    static void OnTheRoster(Creature creature, CheckContext check)
    {
        var path = "roster/" + creature.Name.ToLowerInvariant();
        if (check.Data is { } data && (!Statventure.Packs.PackPath.IsValid(path) || !data.Exists(path)))
            check.Report(RecordPath.Root.Field(nameof(Name)), "isn't on the roster");
    }

    /// <summary>Declared after <see cref="OnTheRoster"/>, run before it: checks run in the order of their names.</summary>
    [Check]
    static void NotTooManyLimbs(Creature creature, CheckContext check)
    {
        if (creature.Limbs.Count > 8)
            check.Report(RecordPath.Root.Field(nameof(Limbs)), "has more than eight limbs");
    }

    [Record("test.creature", 1)]
    public sealed record V1
    {
        [Field(1)] public required string Name { get; init; }
        [Field(2)] public int Legs { get; init; }
        [Field(3)] public IReadOnlyList<Limb> Limbs { get; init; } = [];

        public V2 Upgrade() => new() { Name = Name, Limbs = [.. Limbs, .. Enumerable.Repeat(new Limb { Kind = LimbKind.Leg }, Legs)] };
    }

    [Record("test.creature", 2)]
    public sealed record V2
    {
        [Field(1)] public required string Name { get; init; }
        [Field(2)] public IReadOnlyList<Limb> Limbs { get; init; } = [];
        [Field(3)] public double Speed { get; init; } = 1;

        public Creature Upgrade() => new() { Name = Name, Limbs = Limbs, Speed = Speed / 2 };
    }
}

/// <summary>A union whose version 2 added a subtype (test.fly); version 1 is its old tag list.</summary>
[Record("test.move", 2)]
[Subtype(1, typeof(Walk))]
[Subtype(2, typeof(Leap))]
[Subtype(3, typeof(Fly))]
public abstract record Move
{
    [Field(1)] public string Cue { get; init; } = "";

    /// <summary>A union's check runs on each of its subtypes' values, before theirs.</summary>
    [Check]
    static void HasACue(Move move, CheckContext check)
    {
        if (move.Cue.Length == 0)
            check.Report(RecordPath.Root.Field(nameof(Cue)), "needs a cue");
    }

    [Record("test.move", 1)]
    [Subtype(1, typeof(Walk))]
    [Subtype(2, typeof(Leap))]
    public abstract record V1;
}

[Record("test.walk", 1)]
public sealed record Walk : Move
{
    [Field(2)] public double Pace { get; init; } = 1;
}

/// <summary>Version 2 made the height a double and added the spin.</summary>
[Record("test.leap", 2)]
public sealed record Leap : Move
{
    [Field(2)] public required double Height { get; init; }
    [Field(3)] public bool Spin { get; init; }

    [Check]
    static void NotTooHigh(Leap leap, CheckContext check)
    {
        if (leap.Height > 10)
            check.Report("leaps too high");
    }

    [Record("test.leap", 1)]
    public new sealed record V1
    {
        [Field(1)] public string Cue { get; init; } = "";
        [Field(2)] public required float Height { get; init; }

        public Leap Upgrade() => new() { Cue = Cue, Height = Height };
    }
}

[Record("test.fly", 1)]
public sealed record Fly : Move
{
    [Field(2)] public required double Altitude { get; init; }
}
