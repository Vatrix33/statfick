using System.Numerics;
using System.Text;
using Statventure.Records;

namespace Statventure.Tests.Records;

/// <summary>
/// The two forms of a record (Docs/Assets.md, "Records (the serializer)"): every kind of field round
/// trips in both, floats bit for bit; the bytes and the text are as the format says; nulls, defaults,
/// dictionary order and unions.
/// </summary>
public class RecordFormTests
{
    /// <summary>A value with every kind of field, extremes and specials included (the test.kinds fixture).</summary>
    internal static Kinds Sample() => new()
    {
        Flag = true,
        Int8 = sbyte.MinValue,
        UInt8 = byte.MaxValue,
        Int16 = short.MinValue,
        UInt16 = ushort.MaxValue,
        Int32 = int.MinValue,
        UInt32 = uint.MaxValue,
        Int64 = long.MinValue,
        UInt64 = ulong.MaxValue,
        Float32 = -0f,
        Float64 = double.Epsilon,
        Text = "Ünïcödé ✦ \"quoted\" \\ line\nbreak\ttab 😀",
        Note = "",
        Count = 0,
        Ratio = null,
        Shade = Shade.Golden,
        Pace = Pace.Slow,
        Huge = Huge.Big,
        Bytes = [0, 1, 2, 0xFE, 0xFF, 13, 10],
        MaybeBytes = [],
        Ints = [1, -1, int.MaxValue],
        Floats = [0.1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.Epsilon, float.MaxValue, -0f],
        Doubles = [0.1, 1e21, 1e-7, double.NaN, double.NegativeInfinity, double.MaxValue, double.Epsilon, -0.0],
        Bools = [true, false, true],
        Shorts = [0, 1, ushort.MaxValue],
        Longs = [long.MaxValue, -2],
        V2 = new(1, 2),
        V3 = new(0.5f, -1.5f, 3),
        V4 = new(1, 2, 3, float.NaN),
        Rotation = new(0, 0.70710677f, 0, 0.70710677f),
        Matrix = Matrix4x4.CreateScale(2) * Matrix4x4.CreateTranslation(1, 2, 3),
        Points = [new(1, 2, 3), new(-0f, float.Epsilon, 1e-30f)],
        Matrices = [Matrix4x4.Identity, Matrix4x4.CreateRotationX(0.5f)],
        MaybeRotation = null,
        Words = ["a", "", "β"],
        Gaps = ["x", null, ""],
        Names = ["first", "second"],
        ReadOnlyInts = [3, 2, 1],
        Jagged = [[1, 2], [], [double.NaN]],
        MaybeInts = null,
        FloatList = [1.5f, -2.25f],
        Scores = new() { ["zeta"] = 1, ["alpha"] = 2, ["mid"] = 3 },
        Parts = new Dictionary<string, Part> { ["head"] = new() { Name = "helm", Weight = 2.5, Shade = Shade.Dark }, ["arm"] = new() { Name = "glove" } },
        Pairs = [new("kick", "x...x..."), new("snare", "..x...x."), new("hat", "xxxxxxxx")],
        ByNumber = new() { [10] = "ten", [-3] = null, [0] = "zero" },
        ByShade = new() { [Shade.Golden] = 1, [Shade.Pale] = -1 },
        Ordered = new() { ["b"] = 2, ["a"] = 1 },
        Sparse = new() { [ulong.MaxValue] = null, [7] = new Part { Name = "seven" } },
        Part = new() { Name = "core", Weight = -0.0 },
        MaybePart = null,
        Joint = new(1, ushort.MaxValue),
        Joints = [new(0, 1), new(2, 3)],
        MaybeJoint = new(4, 5),
        Point = new(1.5f, Label: "here") { Visible = false },
        MaybeParts = [null, new Part { Name = "p" }],
        Shape = new Square { Tint = "red", Side = 2, Inner = new Circle { Radius = 0.5 } },
        Shapes = [new Dot(), new Circle { Radius = 3, Tint = "" }, new Square { Side = 1 }],
        MaybeShape = null,
        Empties = [new(), new(), new()],
        ShapesByName = new() { ["sun"] = new Circle { Radius = 10, Tint = "gold" } },
        Paces = [Pace.Fast, Pace.Slow, Pace.Steady],
        ShadePairs = [new(Shade.Dark, 1), new(Shade.Pale, 2)],
        Motto = "none",
    };

    /// <summary>The floats in every place a float can be: fields, nullables, blocks, lists, vectors, nested lists.</summary>
    static Kinds WithFloats(float[] floats, double[] doubles) => new()
    {
        Text = "",
        Part = new() { Name = "", Weight = doubles[^1] },
        Float32 = floats[^1],
        Float64 = doubles[^1],
        Ratio = doubles[0],
        Floats = floats,
        Doubles = doubles,
        FloatList = [.. floats],
        Jagged = [doubles, doubles[..1]],
        Points = [.. Enumerable.Range(0, floats.Length / 3).Select(i => new Vector3(floats[3 * i], floats[3 * i + 1], floats[3 * i + 2]))],
        V4 = new(floats[0], floats[1], floats[2], floats[^1]),
        Matrices = [new Matrix4x4(floats[0], floats[1], floats[2], floats[3], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, floats[^2], floats[^1])],
        ByShade = new() { [Shade.Dark] = doubles[1], [Shade.Pale] = doubles[^1] },
        Point = new(floats[0], floats[^1]),
    };

    static T Binary<T>(T value) where T : notnull => RecordSerializer.FromBinary<T>(RecordSerializer.ToBinary(value));
    static T Json<T>(T value) where T : notnull => RecordSerializer.FromJson<T>(RecordSerializer.ToJson(value));

    static byte[] Bytes(params object[] parts) => [.. parts.SelectMany(p => p switch
    {
        string s => Encoding.UTF8.GetBytes(s),
        int b => [(byte)b],
        double d => BitConverter.GetBytes(d),
        float f => BitConverter.GetBytes(f),
        byte[] bytes => bytes,
        _ => throw new ArgumentException(p.GetType().Name),
    })];

    [Fact]
    public void TheKindsFixtureIsRecorded() => RecordFixtures.Ensure("kinds", Sample());

    [Fact]
    public void EveryKindRoundTripsInBothForms()
    {
        var sample = Sample();
        RecordAssert.Equal(sample, Binary(sample));
        RecordAssert.Equal(sample, Json(sample));
        // Twice through is the same bytes (nothing drifts).
        var bytes = RecordSerializer.ToBinary(sample);
        Assert.Equal(bytes, RecordSerializer.ToBinary(RecordSerializer.FromBinary<Kinds>(bytes)));
        var text = RecordSerializer.ToJson(sample);
        Assert.Equal(text, RecordSerializer.ToJson(RecordSerializer.FromJson<Kinds>(text)));
    }

    [Fact]
    public void FloatsKeepTheirBits()
    {
        float[] floats = [0f, -0f, float.Epsilon, -float.Epsilon, BitConverter.Int32BitsToSingle(0x007FFFFF), float.MaxValue, float.MinValue,
            float.PositiveInfinity, float.NegativeInfinity, 0.1f, 1f / 3, 16777217f, 1e-30f, 3.4028235E+38f];
        double[] doubles = [0, -0.0, double.Epsilon, -double.Epsilon, BitConverter.Int64BitsToDouble(0x000FFFFFFFFFFFFF), double.MaxValue, double.MinValue,
            double.PositiveInfinity, double.NegativeInfinity, 0.1, 1.0 / 3, 9007199254740993, 1e21, 1e-7, 123456789012345678];
        var random = new Random(9);
        for (var i = 0; i < 2000; i++)
        {
            float f;
            do f = BitConverter.Int32BitsToSingle(random.Next() ^ (random.Next() << 1));
            while (float.IsNaN(f));
            double d;
            do d = BitConverter.Int64BitsToDouble(random.NextInt64() ^ ((long)random.Next() << 63));
            while (double.IsNaN(d));
            floats = [.. floats, f];
            doubles = [.. doubles, d];
        }
        var value = WithFloats(floats, doubles);
        RecordAssert.Equal(value, Binary(value));
        RecordAssert.Equal(value, Json(value));

        // NaN payloads (quiet and signalling, either sign) survive the binary form; JSON keeps only NaN.
        float[] nans = [float.NaN, BitConverter.Int32BitsToSingle(0x7F800001), BitConverter.Int32BitsToSingle(0x7FC12345), BitConverter.Int32BitsToSingle(unchecked((int)0xFFBFFFFF))];
        double[] doubleNans = [double.NaN, BitConverter.Int64BitsToDouble(0x7FF0000000000001), BitConverter.Int64BitsToDouble(unchecked((long)0xFFF4000000000123))];
        var withNans = WithFloats(nans, doubleNans);
        RecordAssert.Equal(withNans, Binary(withNans));
        RecordAssert.Equal(withNans, Json(withNans), anyNaN: true);
    }

    [Fact]
    public void NullStaysDistinctFromEmpty()
    {
        foreach (var value in new[]
        {
            new Kinds { Text = "", Part = new() { Name = "" }, Note = null, MaybeBytes = null, MaybeInts = null, Ordered = null, MaybePart = null, Count = null, Ratio = null },
            new Kinds { Text = "", Part = new() { Name = "" }, Note = "", MaybeBytes = [], MaybeInts = [], Ordered = [], MaybePart = new() { Name = "" }, Count = 0, Ratio = 0 },
        })
        {
            foreach (var back in new[] { Binary(value), Json(value) })
            {
                RecordAssert.Equal(value, back);
                Assert.Equal(value.Note is null, back.Note is null);
                Assert.Equal(value.MaybeBytes is null, back.MaybeBytes is null);
                Assert.Equal(value.Ordered is null, back.Ordered is null);
                Assert.Equal(value.Ratio is null, back.Ratio is null);
            }
        }
    }

    [Fact]
    public void DictionariesAndPairsKeepTheirOrder()
    {
        var value = new Kinds
        {
            Text = "",
            Part = new() { Name = "" },
            Scores = new() { ["zeta"] = 1, ["alpha"] = 2, ["mid"] = 3, ["beta"] = 4 },
            Pairs = [new("snare", "a"), new("kick", "b"), new("hat", "c")],
            ByNumber = new() { [3] = "c", [1] = "a", [2] = "b" },
            Ordered = new() { ["z"] = 0, ["y"] = 1, ["a"] = 2 },
        };
        foreach (var back in new[] { Binary(value), Json(value) })
        {
            Assert.Equal(["zeta", "alpha", "mid", "beta"], back.Scores.Keys);
            Assert.Equal(["snare", "kick", "hat"], back.Pairs.Select(p => p.Key));
            Assert.Equal([3, 1, 2], back.ByNumber.Keys);
            Assert.Equal(["z", "y", "a"], back.Ordered!.Keys);
        }
        var text = RecordSerializer.ToJson(value);
        Assert.Contains("\"scores\": {\n    \"zeta\": 1,\n    \"alpha\": 2,\n    \"mid\": 3,\n    \"beta\": 4\n  }", text);
        Assert.Contains("\"pairs\": {\n    \"snare\": \"a\",\n    \"kick\": \"b\",\n    \"hat\": \"c\"\n  }", text);
        Assert.Contains("\"byNumber\": {\n    \"3\": \"c\",\n    \"1\": \"a\",\n    \"2\": \"b\"\n  }", text);
    }

    [Fact]
    public void UnionsKeepTheirSubtype()
    {
        Shape[] shapes = [new Dot { Tint = "x" }, new Circle { Radius = 1 }, new Square { Side = 2, Inner = new Square { Inner = new Dot() } }];
        foreach (var shape in shapes)
        {
            RecordAssert.Equal(shape, Binary(shape));
            RecordAssert.Equal(shape, Json(shape));
        }
        var value = new Kinds { Text = "", Part = new() { Name = "" }, Shapes = shapes, MaybeShape = new Circle { Radius = 4 }, Shape = new Dot() };
        RecordAssert.Equal(value, Binary(value));
        RecordAssert.Equal(value, Json(value));
        Assert.IsType<Square>(Json(value).Shapes[2]);
    }

    [Fact]
    public void TheBinaryFormIsAsTheFormatSays()
    {
        // The type table (a count, then each name and version), then the fields in number order:
        // a string as its UTF-8 byte count and bytes, a double little-endian, an enum at its width.
        Assert.Equal(Bytes(1, 9, "test.part", 1, 2, "ab", 1.5, 5), RecordSerializer.ToBinary(new Part { Name = "ab", Weight = 1.5, Shade = Shade.Dark }));
        // Variable-length counts: 7 bits a byte, low first, the high bit on all but the last (127 is 7F, 128 is 80 01, 200 is C8 01).
        foreach (var (length, count) in new[] { (127, new byte[] { 0x7F }), (128, [0x80, 0x01]), (200, [0xC8, 0x01]), (16384, [0x80, 0x80, 0x01]) })
            Assert.Equal(Bytes(1, 9, "test.part", 1, count, new string('a', length), 1.0, 0), RecordSerializer.ToBinary(new Part { Name = new string('a', length) }));
        // A positional record: floats bit for bit, a bool as one byte, an int enum as four, a presence byte before a nullable.
        Assert.Equal(Bytes(1, 10, "test.point", 1, 1.5f, 2f, 0, 1, 0, 0, 0, 1, 1, "z"),
            RecordSerializer.ToBinary(new Point(1.5f, Label: "z") { Visible = false }));
        Assert.Equal(Bytes(1, 10, "test.point", 1, 0f, 2f, 1, 1, 0, 0, 0, 0), RecordSerializer.ToBinary(new Point(0)));
        // A union: the union's type, then its subtype's, in the table; its tag before the subtype's fields (the base's field first).
        Assert.Equal(Bytes(2, 10, "test.shape", 1, 11, "test.circle", 1, 1, 0, 2.0),
            RecordSerializer.ToBinary<Shape>(new Circle { Tint = "", Radius = 2 }));
        // The table lists the types in the order the value meets them; lists are a count, then the elements.
        Assert.Equal(Bytes(4, 13, "test.creature", 3, 9, "test.limb", 2, 9, "test.move", 2, 9, "test.walk", 1,
                1, "A", 1, 0, 0, 0, 0, 2.0, 0.5, 0, 1, 1, 0, 1.0),
            RecordSerializer.ToBinary(new Creature { Name = "A", Limbs = [new Limb { Kind = LimbKind.Arm, Length = 2 }], Moves = [new Walk()] }));
        // Only the types the value uses: no limb, no move.
        Assert.Equal(Bytes(1, 13, "test.creature", 3, 1, "B", 0, 0.5, 1, 1, "t", 0), RecordSerializer.ToBinary(new Creature { Name = "B", Title = "t" }));
    }

    static void Contains(byte[] data, byte[] part) =>
        Assert.True(data.AsSpan().IndexOf(part) >= 0, $"the bytes {Convert.ToHexString(part)} aren't in the binary form");

    [Fact]
    public void BlocksMapsAndUnionListsAreAsTheFormatSays()
    {
        var data = RecordSerializer.ToBinary(Sample());
        // A dictionary: its count, then key and value pairs in insertion order.
        Contains(data, Bytes(3, 4, "zeta", BitConverter.GetBytes(1), 5, "alpha", BitConverter.GetBytes(2), 3, "mid", BitConverter.GetBytes(3)));
        // Number and vector arrays: a count, then the elements one after another.
        Contains(data, Bytes(7, 0.1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.Epsilon, float.MaxValue, -0f));
        Contains(data, Bytes(2, 1f, 2f, 3f, -0f, float.Epsilon, 1e-30f));
        Contains(data, Bytes(3, 1, 0, 1));
        Contains(data, Bytes(3, 0, 0, 1, 0, 0xFF, 0xFF));
        // Record structs in a list: their fields, no per-element cost; a nullable one with its presence byte.
        Contains(data, Bytes(2, 0, 0, 1, 0, 2, 0, 3, 0, 1, 4, 0, 5, 0));
        // A list of unions: tags 3 (dot), 1 (circle), 2 (square), each before its fields.
        Contains(data, Bytes(3, 3, 3, "ink", 1, 0, 3.0, 2, 3, "ink", 1.0, 0));
        // Records without fields: only their count (after a null union, before a dictionary of one union).
        Contains(data, Bytes(0, 3, 1, 3, "sun", 1, 4, "gold", 10.0));
        // An ulong enum at eight bytes, a byte enum at one.
        Contains(data, Bytes(0xC8, 1, 0xFF, 0xFF, 0xFF, 0xFF, 0, 0, 0, 0, 0, 0, 0, 0x80));
    }

    [Fact]
    public void TheJsonFormIsAsTheFormatSays()
    {
        // camelCase keys in field order after the "$types" table; enum members by their camelCase names;
        // a null whose default is null is left out.
        Assert.Equal("""
            {
              "$types": {
                "test.point": 1
              },
              "x": 1.5,
              "y": 2,
              "visible": false,
              "pace": "fast",
              "label": "here"
            }

            """.Replace("\r", ""), RecordSerializer.ToJson(new Point(1.5f, Label: "here") { Visible = false }));
        Assert.Equal("{\n  \"$types\": {\n    \"test.point\": 1\n  },\n  \"x\": 0,\n  \"y\": 2,\n  \"visible\": true,\n  \"pace\": \"fast\"\n}\n",
            RecordSerializer.ToJson(new Point(0)));
        // A union: "$type" names the subtype; the table lists the union and the subtype.
        Assert.Equal("{\n  \"$types\": {\n    \"test.shape\": 1,\n    \"test.circle\": 1\n  },\n  \"$type\": \"test.circle\",\n  \"tint\": \"ink\",\n  \"radius\": 2\n}\n",
            RecordSerializer.ToJson<Shape>(new Circle { Radius = 2 }));
        var text = RecordSerializer.ToJson(Sample());
        // Shortest round-trip numbers; NaN and the infinities as strings; vectors and matrices as arrays on one line.
        Assert.Contains("\"float32\": -0,", text);
        Assert.Contains("\"float64\": 5E-324,", text);
        Assert.Contains("\"floats\": [\n    0.1,\n    \"NaN\",\n    \"Infinity\",\n    \"-Infinity\",\n    1E-45,\n    3.4028235E+38,\n    -0\n  ]", text);
        Assert.Contains("\"doubles\": [\n    0.1,\n    1E+21,\n    1E-07,", text);
        Assert.Contains("\"v3\": [0.5, -1.5, 3],", text);
        Assert.Contains("\"v4\": [1, 2, 3, \"NaN\"],", text);
        Assert.Contains("\"matrix\": [2, 0, 0, 0, 0, 2, 0, 0, 0, 0, 2, 0, 1, 2, 3, 1],", text);
        // Bytes in base64; integers whole; strings with only JSON's escapes.
        Assert.Contains("\"bytes\": \"AAEC/v8NCg==\",", text);
        Assert.Contains("\"uInt64\": 18446744073709551615,", text);
        Assert.Contains("\"text\": \"Ünïcödé ✦ \\\"quoted\\\" \\\\ line\\nbreak\\ttab \\uD83D\\uDE00\",", text);
        Assert.Contains("\"shade\": \"golden\",", text);
        Assert.Contains("\"huge\": \"big\",", text);
        // A null whose default isn't null is written; one whose default is null isn't; a value equal to its default is.
        Assert.Contains("\"ratio\": null,", text);
        Assert.DoesNotContain("\"maybeRotation\"", text);
        Assert.Contains("\"motto\": \"none\"\n}\n", text);
        Assert.Contains("\"empties\": [\n    {},\n    {},\n    {}\n  ],", text);
    }

    [Fact]
    public void AbsentKeysTakeTheirDefaults()
    {
        var kinds = RecordSerializer.FromJson<Kinds>("""{"$types": {"test.kinds": 1, "test.part": 1}, "text": "t", "part": {"name": "p"}}"""u8);
        var expected = new Kinds { Text = "t", Part = new() { Name = "p" } };
        RecordAssert.Equal(expected, kinds);
        Assert.Equal(0.5, kinds.Ratio);
        Assert.Equal(Huge.Small, kinds.Huge);
        Assert.Equal(Quaternion.Identity, kinds.Rotation);
        Assert.Equal(1, kinds.Part.Weight);
        // A positional record: the parameters' defaults and the initializer of the property beside them.
        var point = RecordSerializer.FromJson<Point>("""{"$types": {"test.point": 1}, "x": 4}"""u8);
        RecordAssert.Equal(new Point(4), point);
        Assert.True(point.Visible);
        Assert.Equal(Pace.Fast, point.Pace);
        // Present keys win over the defaults, null included.
        var given = RecordSerializer.FromJson<Point>("""{"$types": {"test.point": 1}, "x": 4, "y": 0, "pace": "slow", "visible": false, "label": null}"""u8);
        RecordAssert.Equal(new Point(4, 0, Pace.Slow) { Visible = false }, given);
        Assert.Null(RecordSerializer.FromJson<Kinds>("""{"$types": {"test.kinds": 1, "test.part": 1}, "text": "t", "part": {"name": "p"}, "ratio": null}"""u8).Ratio);
    }

    [Fact]
    public void RequiredKeysMustBeThere()
    {
        var error = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Kinds>("""{"$types": {"test.kinds": 1, "test.part": 1}, "part": {"name": "p"}}"""u8));
        Assert.Equal("test.kinds needs the key \"text\"", error.Message);
        error = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Kinds>("""{"$types": {"test.kinds": 1, "test.part": 1}, "text": "t", "part": {}}"""u8));
        Assert.Equal("test.part needs the key \"name\" (at $.part)", error.Message);
        // A positional parameter without a default.
        error = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Point>("""{"$types": {"test.point": 1}, "y": 4}"""u8));
        Assert.Equal("test.point needs the key \"x\"", error.Message);
        // A required member of a subtype.
        error = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Shape>("""{"$types": {"test.shape": 1, "test.circle": 1}, "$type": "test.circle"}"""u8));
        Assert.Equal("test.circle needs the key \"radius\"", error.Message);
    }
}
