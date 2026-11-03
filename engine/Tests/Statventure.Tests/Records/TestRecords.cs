using System.Numerics;
using Statventure.Records;

namespace Statventure.Tests.Records;

// Record types of the serializer's tests (Docs/Assets.md, step 2): one with every kind of field, a
// union, and a family with several versions (test.creature 1–3, test.limb 1–2, the union test.move
// 1–2 with test.walk, test.leap 1–2, test.fly) whose past versions have committed fixtures in
// Data/Records/Fixtures. Their layouts are recorded in Data/Records/Layouts like every record type's.

public enum Shade : byte { Pale, Dark = 5, Golden = 200 }

public enum Pace { Slow = -1, Steady, Fast }

public enum Huge : ulong { Small = 1, Big = 1UL << 63 }

[Record("test.part", 1)]
public sealed record Part
{
    [Field(1)] public required string Name { get; init; }
    [Field(2)] public double Weight { get; init; } = 1;
    [Field(3)] public Shade Shade { get; init; }
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
    [Field(1)] public bool Flag { get; init; }
    [Field(2)] public sbyte Int8 { get; init; }
    [Field(3)] public byte UInt8 { get; init; }
    [Field(4)] public short Int16 { get; init; }
    [Field(5)] public ushort UInt16 { get; init; }
    [Field(6)] public int Int32 { get; init; }
    [Field(7)] public uint UInt32 { get; init; }
    [Field(8)] public long Int64 { get; init; }
    [Field(9)] public ulong UInt64 { get; init; }
    [Field(10)] public float Float32 { get; init; }
    [Field(11)] public double Float64 { get; init; }
    [Field(12)] public required string Text { get; init; }
    [Field(13)] public string? Note { get; init; }
    [Field(14)] public int? Count { get; init; }
    [Field(15)] public double? Ratio { get; init; } = 0.5;
    [Field(16)] public Shade Shade { get; init; }
    [Field(17)] public Pace? Pace { get; init; }
    [Field(18)] public Huge Huge { get; init; } = Huge.Small;
    [Field(19)] public byte[] Bytes { get; init; } = [];
    [Field(20)] public byte[]? MaybeBytes { get; init; }
    [Field(21)] public int[] Ints { get; init; } = [];
    [Field(22)] public float[] Floats { get; init; } = [];
    [Field(23)] public double[] Doubles { get; init; } = [];
    [Field(24)] public bool[] Bools { get; init; } = [];
    [Field(25)] public ushort[] Shorts { get; init; } = [];
    [Field(26)] public long[] Longs { get; init; } = [];
    [Field(27)] public Vector2 V2 { get; init; }
    [Field(28)] public Vector3 V3 { get; init; }
    [Field(29)] public Vector4 V4 { get; init; }
    [Field(30)] public Quaternion Rotation { get; init; } = Quaternion.Identity;
    [Field(31)] public Matrix4x4 Matrix { get; init; } = Matrix4x4.Identity;
    [Field(32)] public Vector3[] Points { get; init; } = [];
    [Field(33)] public Matrix4x4[] Matrices { get; init; } = [];
    [Field(34)] public Quaternion? MaybeRotation { get; init; }
    [Field(35)] public string[] Words { get; init; } = [];
    [Field(36)] public string?[] Gaps { get; init; } = [];
    [Field(37)] public List<string> Names { get; init; } = [];
    [Field(38)] public IReadOnlyList<int> ReadOnlyInts { get; init; } = [];
    [Field(39)] public double[][] Jagged { get; init; } = [];
    [Field(40)] public int[]? MaybeInts { get; init; }
    [Field(41)] public List<float> FloatList { get; init; } = [];
    [Field(42)] public Dictionary<string, int> Scores { get; init; } = [];
    [Field(43)] public IReadOnlyDictionary<string, Part> Parts { get; init; } = new Dictionary<string, Part>();
    [Field(44)] public IReadOnlyList<KeyValuePair<string, string>> Pairs { get; init; } = [];
    [Field(45)] public Dictionary<int, string?> ByNumber { get; init; } = [];
    [Field(46)] public Dictionary<Shade, double> ByShade { get; init; } = [];
    [Field(47)] public OrderedDictionary<string, int>? Ordered { get; init; }
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
    [Field(2)] public double Length { get; init; } = 1;

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
[Record("test.creature", 3)]
public sealed record Creature
{
    [Field(1)] public required string Name { get; init; }
    [Field(2)] public IReadOnlyList<Limb> Limbs { get; init; } = [];
    [Field(3)] public double Speed { get; init; } = 0.5;
    [Field(4)] public string? Title { get; init; }
    [Field(5)] public IReadOnlyList<Move> Moves { get; init; } = [];

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
