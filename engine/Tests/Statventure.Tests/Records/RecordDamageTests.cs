using System.Text;
using Statventure.Records;
using Statventure.Tests.Records.Variants;

namespace Statventure.Tests.Records;

/// <summary>
/// Damaged, truncated and foreign data (Docs/Assets.md, "Method"): each kind gives a
/// <see cref="RecordFormatException"/> saying what and where, never wrong data or another exception;
/// a version newer than the engine reads is refused, naming it; a value its declaration doesn't allow
/// isn't written.
/// </summary>
public class RecordDamageTests
{
    static byte[] PartBytes => RecordSerializer.ToBinary(new Part { Name = "ab", Weight = 1.5, Shade = Shade.Dark });

    static string BinaryError<T>(byte[] data) where T : notnull =>
        Assert.Throws<RecordFormatException>(() => RecordSerializer.FromBinary<T>(data)).Message;

    static string JsonError<T>(string json) where T : notnull =>
        Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<T>(json)).Message;

    static byte[] Edit(byte[] data, int at, params byte[] bytes)
    {
        var copy = data.ToArray();
        bytes.CopyTo(copy, at);
        return copy;
    }

    static byte[] Concat(params byte[][] parts) => [.. parts.SelectMany(p => p)];

    static byte[] Table(params (string Name, int Version)[] entries) =>
        [(byte)entries.Length, .. entries.SelectMany(e => (byte[])[(byte)e.Name.Length, .. Encoding.UTF8.GetBytes(e.Name), (byte)e.Version])];

    [Fact]
    public void TruncatedDataIsRefusedAtEveryLength()
    {
        foreach (var data in new[] { RecordSerializer.ToBinary(RecordFormTests.Sample()), RecordFixtures.Read("test.creature", "creature-1", ".rec") })
        {
            var type = RecordSerializer.RootTypeName(data) == "test.kinds" ? typeof(Kinds) : typeof(Creature);
            for (var length = 0; length < data.Length; length++)
                Assert.Throws<RecordFormatException>(() => RecordSerializer.FromBinary(type, data.AsSpan(0, length)));
        }
        Assert.Equal("the data ends early (byte 23 of at least 24) (at $.shade)", BinaryError<Part>(PartBytes[..^1]));
        Assert.Equal("the data ends early (byte 18) (at $.weight)", BinaryError<Part>(PartBytes[..18]));
        Assert.Equal("a count at byte 12 (2) is more than the 0 bytes left (at $.name)", BinaryError<Part>(PartBytes[..13]));
        var json = RecordSerializer.ToJson(RecordFormTests.Sample());
        for (var length = 0; length < json.Length; length += 7)
            Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Kinds>(json[..length]));
    }

    [Fact]
    public void DataAfterTheRecordIsRefused()
    {
        Assert.Equal("1 bytes follow the record", BinaryError<Part>([.. PartBytes, 0]));
        Assert.StartsWith("not valid JSON", JsonError<Point>("""{"$types": {"test.point": 1}, "x": 1} {}"""));
        // Whitespace after the value and a byte order mark before it (editors write one) are fine.
        RecordAssert.Equal(new Point(1), RecordSerializer.FromJson<Point>([0xEF, 0xBB, 0xBF, .. """{"$types": {"test.point": 1}, "x": 1}  """u8, (byte)'\n']));
    }

    [Fact]
    public void DamagedBytesGiveAFormatErrorOrAValueNeverAnotherException()
    {
        var data = RecordSerializer.ToBinary(RecordFormTests.Sample());
        var random = new Random(5);
        var refused = 0;
        for (var i = 0; i < 3000; i++)
        {
            var damaged = data.ToArray();
            for (var n = random.Next(1, 4); n > 0; n--)
                damaged[random.Next(damaged.Length)] = (byte)random.Next(256);
            try
            {
                RecordSerializer.FromBinary<Kinds>(damaged);
            }
            catch (RecordFormatException)
            {
                refused++;
            }
        }
        Assert.InRange(refused, 1, 3000);
    }

    [Fact]
    public void EachKindOfBadByteIsRefused()
    {
        var point = RecordSerializer.ToBinary(new Point(1, Label: "z"));
        var labelAt = point.Length - 3;
        // A bool and a presence byte are 0 or 1.
        Assert.Equal($"a bool at byte {labelAt - 5} is 2, not 0 or 1 (at $.visible)", BinaryError<Point>(Edit(point, labelAt - 5, 2)));
        Assert.Equal($"a presence byte at byte {labelAt} is 7, not 0 or 1 (at $.label)", BinaryError<Point>(Edit(point, labelAt, 7)));
        // An enum's number must be a member.
        Assert.Equal("9 isn't a member of Pace (at $.pace)", BinaryError<Point>(Edit(point, labelAt - 4, 9)));
        Assert.Equal("4 isn't a member of Shade (at $.shade)", BinaryError<Part>(Edit(PartBytes, PartBytes.Length - 1, 4)));
        // Variable-length integers: the shortest form only, at most 64 bits.
        Assert.Equal("a variable-length integer at byte 0 isn't in its shortest form", BinaryError<Part>([0x81, 0x00, .. PartBytes[1..]]));
        Assert.Equal("a variable-length integer at byte 0 is too long",
            BinaryError<Part>([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x02, .. PartBytes[1..]]));
        // A count can't promise more than the bytes left (no huge allocation from a damaged count).
        var nameAt = PartBytes.Length - 12;
        Assert.Equal($"a count at byte {nameAt} (100) is more than the 11 bytes left (at $.name)", BinaryError<Part>(Edit(PartBytes, nameAt, 100)));
        var block = RecordSerializer.ToBinary(new Kinds { Text = "", Part = new() { Name = "" }, Floats = [1, 2] });
        var floatsAt = block.AsSpan().IndexOf((byte[])[2, 0, 0, 0x80, 0x3F]);
        Assert.StartsWith($"a block at byte {floatsAt} (268435455 × 4 bytes) is longer than the", BinaryError<Kinds>(Edit(block, floatsAt, 0xFF, 0xFF, 0xFF, 0x7F)));
        // Strings are valid UTF-8.
        Assert.Equal($"the string at byte {nameAt} isn't valid UTF-8 (at $.name)", BinaryError<Part>(Edit(PartBytes, nameAt + 1, 0xC3, 0x28)));
        // A union's tag must be one its version lists.
        var circle = RecordSerializer.ToBinary<Shape>(new Circle { Radius = 1 });
        var tagAt = 1 + 12 + 13;
        Assert.Equal($"the tag 9 at byte {tagAt} isn't a subtype of test.shape version 1", BinaryError<Shape>(Edit(circle, tagAt, 9)));
        // A repeated dictionary key.
        var map = RecordSerializer.ToBinary(new Kinds { Text = "", Part = new() { Name = "" }, Scores = new() { ["a"] = 1, ["b"] = 2 } });
        var keyAt = map.AsSpan().IndexOf((byte[])[2, 1, (byte)'a', 1, 0, 0, 0, 1, (byte)'b']) + 8;
        Assert.Equal("the key a is there twice (at $.scores)", BinaryError<Kinds>(Edit(map, keyAt, (byte)'a')));
    }

    [Fact]
    public void CountsCantPromiseMoreThanTheBytesLeft()
    {
        // A string one byte longer than what is left; a list claiming two billion elements (no allocation for them).
        Assert.Equal("a count at byte 12 (12) is more than the 11 bytes left (at $.name)", BinaryError<Part>(Edit(PartBytes, 12, 12)));
        var words = RecordSerializer.ToBinary(new Kinds { Text = "", Part = new() { Name = "" }, Words = ["a"] });
        var at = words.AsSpan().IndexOf((byte[])[1, 1, (byte)'a']);
        Assert.StartsWith($"a count at byte {at} (2147483647) is more than the", BinaryError<Kinds>([.. words[..at], 0xFF, 0xFF, 0xFF, 0xFF, 0x07, .. words[(at + 1)..]]));
        // Records without fields take no bytes: a list of them can't be bounded by the bytes left, so by a count.
        var empties = RecordSerializer.ToBinary(new EmptiesAtEnd { Items = [new(), new()] });
        RecordAssert.Equal(new EmptiesAtEnd { Items = [new(), new()] }, RecordSerializer.FromBinary<EmptiesAtEnd>(empties));
        Assert.Equal("a list of 2147483647 values without fields is too long (at $.items)",
            BinaryError<EmptiesAtEnd>([.. empties[..^1], 0xFF, 0xFF, 0xFF, 0xFF, 0x07]));
        // Bools in a list are checked one by one.
        var bools = RecordSerializer.ToBinary(new Kinds { Text = "", Part = new() { Name = "" }, Bools = [true, false, true] });
        var boolAt = bools.AsSpan().IndexOf((byte[])[3, 1, 0, 1]) + 2;
        Assert.Equal($"a bool at byte {boolAt} is 2, not 0 or 1 (at $.bools[1])", BinaryError<Kinds>(Edit(bools, boolAt, 2)));
    }

    static Shape Nest(int squares)
    {
        Shape shape = new Dot();
        for (var i = 0; i < squares; i++)
            shape = new Square { Inner = shape };
        return shape;
    }

    [Fact]
    public void RecordsNestedTooDeepAreRefused()
    {
        // 255 squares around a dot: 256 records deep, the most.
        var deepest = Nest(255);
        RecordAssert.Equal(deepest, RecordSerializer.FromBinary<Shape>(RecordSerializer.ToBinary(deepest)));
        RecordAssert.Equal(deepest, RecordSerializer.FromJson<Shape>(RecordSerializer.ToJson(deepest)));
        var deeper = Nest(256);
        Assert.StartsWith("records are nested more than 256 deep",
            Assert.Throws<RecordFormatException>(() => RecordSerializer.FromBinary<Shape>(RecordSerializer.ToBinary(deeper))).Message);
        Assert.StartsWith("records are nested more than 256 deep",
            Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Shape>(RecordSerializer.ToJson(deeper))).Message);
        // Depth is nesting, not number: a thousand side by side are fine.
        var wide = new Kinds { Text = "", Part = new() { Name = "" }, Shapes = [.. Enumerable.Repeat<Shape>(new Dot(), 1000)] };
        RecordAssert.Equal(wide, RecordSerializer.FromBinary<Kinds>(RecordSerializer.ToBinary(wide)));
        RecordAssert.Equal(wide, RecordSerializer.FromJson<Kinds>(RecordSerializer.ToJson(wide)));
    }

    [Fact]
    public void AValueThatIsntTheUnionsTypeIsRefused()
    {
        // Version 1 of variant.drifted listed a type that isn't a subtype of the current union.
        var data = Concat(Table(("variant.holder", 1), ("variant.drifted", 1), ("variant.drifted.stray", 1)), [1, 2, 7, 0, 0, 0]);
        Assert.Equal("a Stray read as a variant.drifted isn't a Drifted (at $.value)", BinaryError<Holder>(data));
        Assert.Equal("a Stray read as a variant.drifted isn't a Drifted (at $.value)", JsonError<Holder>(
            """{"$types": {"variant.holder": 1, "variant.drifted": 1, "variant.drifted.stray": 1}, "value": {"$type": "variant.drifted.stray", "a": 7}}"""));
        var kept = Concat(Table(("variant.holder", 1), ("variant.drifted", 1), ("variant.drifted.kept", 1)), [1, 1]);
        Assert.IsType<Drifted.Kept>(RecordSerializer.FromBinary<Holder>(kept).Value);
    }

    [Fact]
    public void KeysWhoseAbsenceMeansNothingAreRequired()
    {
        // A non-nullable string whose initializer leaves null: an absent key can't mean null.
        Assert.Equal("variant.no-default needs the key \"name\"", JsonError<NoDefault>("""{"$types": {"variant.no-default": 1}}"""));
        Assert.Equal("x", RecordSerializer.FromJson<NoDefault>("""{"$types": {"variant.no-default": 1}, "name": "x"}"""u8).Name);
        // A positional parameter without a default has none, nullable or not: its key must be there, and its null is written.
        Assert.Equal("variant.positional-nullable needs the key \"clip\"",
            JsonError<PositionalNullable>("""{"$types": {"variant.positional-nullable": 1}, "name": "idle"}"""));
        var text = RecordSerializer.ToJson(new PositionalNullable("idle", null));
        Assert.Contains("\"clip\": null", text);
        RecordAssert.Equal(new PositionalNullable("idle", null), RecordSerializer.FromJson<PositionalNullable>(text));
    }

    [Fact]
    public void EachKindOfBadTableIsRefused()
    {
        var body = PartBytes[(1 + 1 + 9 + 1)..];
        Assert.Equal("the type table is empty", BinaryError<Part>([0, .. body]));
        Assert.Equal("the type table names \"test.parts\", which isn't a record type a test.part holds",
            BinaryError<Part>(Concat(Table(("test.parts", 1)), body)));
        Assert.Equal("the type table gives test.part version 0", BinaryError<Part>(Concat(Table(("test.part", 0)), body)));
        Assert.Equal("the type table names test.part twice", BinaryError<Part>(Concat(Table(("test.part", 1), ("test.part", 1)), body)));
        Assert.Equal("the data holds a test.limb, not a test.creature",
            BinaryError<Creature>(Concat(Table(("test.limb", 2), ("test.creature", 3)), [1, (byte)'A', 0, .. new byte[8], 0, 0])));
        // A nested type the table doesn't list.
        var creature = RecordSerializer.ToBinary(new Creature { Name = "A", Limbs = [new Limb { Kind = LimbKind.Arm }] });
        var limbs = creature[(1 + 1 + 13 + 1 + 1 + 9 + 1)..];
        Assert.Equal("the type table doesn't list test.limb (at $.limbs[0])", BinaryError<Creature>(Concat(Table(("test.creature", 3)), limbs)));
        // The same in JSON.
        Assert.Equal("the data holds a test.limb, not a test.creature", JsonError<Creature>("""{"$types": {"test.limb": 2, "test.creature": 3}, "name": "A"}"""));
        Assert.Equal("the type table doesn't list test.limb (at $.limbs[0])", JsonError<Creature>("""{"$types": {"test.creature": 3}, "name": "A", "limbs": [{"kind": "arm"}]}"""));
        Assert.Equal("the type table names test.creature twice", JsonError<Creature>("""{"$types": {"test.creature": 3, "test.creature": 3}, "name": "A"}"""));
    }

    [Fact]
    public void ANewerVersionIsRefusedNamingIt()
    {
        var creature = RecordSerializer.ToBinary(new Creature { Name = "A", Limbs = [new Limb { Kind = LimbKind.Arm }] });
        Assert.Equal("test.creature version 4 is newer than this engine reads (version 3)", BinaryError<Creature>(Edit(creature, 15, 4)));
        Assert.Equal("test.limb version 3 is newer than this engine reads (version 2)", BinaryError<Creature>(Edit(creature, 26, 3)));
        Assert.Equal("test.creature version 4 is newer than this engine reads (version 3)", JsonError<Creature>("""{"$types": {"test.creature": 4}, "name": "A"}"""));
        Assert.Equal("test.limb version 3 is newer than this engine reads (version 2)", JsonError<Creature>("""{"$types": {"test.creature": 3, "test.limb": 3}, "name": "A"}"""));
    }

    [Fact]
    public void EachKindOfBadJsonIsRefused()
    {
        const string types = "\"$types\": {\"test.kinds\": 1, \"test.part\": 1, \"test.shape\": 1, \"test.dot\": 1}";
        string Kinds(string fields) => JsonError<Kinds>("{" + types + """, "text": "t", "part": {"name": "p"}, """ + fields + "}");
        Assert.StartsWith("not valid JSON", JsonError<Point>("{\"$types\": {\"test.point\": 1}, \"x\": 1,}"));
        Assert.StartsWith("not valid JSON", JsonError<Point>("{\"$types\": {\"test.point\": 1}, // a comment\n\"x\": 1}"));
        Assert.Equal("expected an object with \"$types\", found array", JsonError<Point>("[]"));
        Assert.Equal("expected \"$types\", an object of record type names and their versions", JsonError<Point>("""{"x": 1}"""));
        Assert.Equal("expected \"$types\", an object of record type names and their versions", JsonError<Point>("""{"$types": [], "x": 1}"""));
        Assert.Equal("the key \"$types\" is there twice", JsonError<Point>("""{"$types": {"test.point": 1}, "$types": {"test.point": 1}, "x": 1}"""));
        Assert.Equal("the version of test.point in \"$types\" isn't an integer", JsonError<Point>("""{"$types": {"test.point": "1"}, "x": 1}"""));
        Assert.Equal("test.point (version 1) has no field \"z\"", JsonError<Point>("""{"$types": {"test.point": 1}, "x": 1, "z": 1}"""));
        Assert.Equal("test.point (version 1) has no field \"X\"", JsonError<Point>("""{"$types": {"test.point": 1}, "X": 1}"""));
        Assert.Equal("test.point (version 1) has no field \"$type\"", JsonError<Point>("""{"$types": {"test.point": 1}, "$type": "test.point", "x": 1}"""));
        Assert.Equal("the key \"x\" is there twice", JsonError<Point>("""{"$types": {"test.point": 1}, "x": 1, "x": 2}"""));
        Assert.Equal("test.part (version 1) has no field \"$types\" (at $.part)",
            JsonError<Kinds>("{" + types + """, "text": "t", "part": {"name": "p", "$types": {}}}"""));
        // Values of the wrong kind or range.
        Assert.Equal("null in a field that can't be null (at $.text)", JsonError<Kinds>("{" + types + """, "text": null, "part": {"name": "p"}}"""));
        Assert.Equal("expected an int32, found the string \"1\" (at $.int32)", Kinds("\"int32\": \"1\""));
        Assert.Equal("expected an int32, found the number 1.5 (at $.int32)", Kinds("\"int32\": 1.5"));
        Assert.Equal("expected an int32, found the number 2147483648 (at $.int32)", Kinds("\"int32\": 2147483648"));
        Assert.Equal("expected a uint8, found the number -1 (at $.uInt8)", Kinds("\"uInt8\": -1"));
        Assert.Equal("expected true or false, found the number 1 (at $.flag)", Kinds("\"flag\": 1"));
        Assert.Equal("expected a float32, found the number 1e39 (at $.float32)", Kinds("\"float32\": 1e39"));
        Assert.Equal("expected a float64, found the number 1e400 (at $.float64)", Kinds("\"float64\": 1e400"));
        Assert.Equal("expected a float64 (a number, \"NaN\", \"Infinity\" or \"-Infinity\"), found the string \"nan\" (at $.doubles[1])", Kinds("\"doubles\": [1, \"nan\"]"));
        Assert.Equal("expected a string, found the number 1 (at $.words[0])", Kinds("\"words\": [1]"));
        Assert.Equal("expected bytes in base64, found the string \"AA=A\" (at $.bytes)", Kinds("\"bytes\": \"AA=A\""));
        Assert.Equal("expected a member of Shade (pale, dark, golden), found the string \"Dark\" (at $.shade)", Kinds("\"shade\": \"Dark\""));
        Assert.Equal("expected a member of Shade (pale, dark, golden), found the number 5 (at $.shade)", Kinds("\"shade\": 5"));
        Assert.Equal("expected a vector3 (an array of 3 numbers), found an array (at $.v3)", Kinds("\"v3\": [1, 2]"));
        Assert.Equal("expected a vector3 (an array of 3 numbers), found an array (at $.v3)", Kinds("\"v3\": [1, 2, 3, 4]"));
        Assert.Equal("expected a float32 (a number, \"NaN\", \"Infinity\" or \"-Infinity\"), found the string \"x\" (at $.v3[2])", Kinds("\"v3\": [1, 2, \"x\"]"));
        Assert.Equal("expected an array, found an object (at $.ints)", Kinds("\"ints\": {}"));
        Assert.Equal("expected an object, found an array (at $.scores)", Kinds("\"scores\": []"));
        Assert.Equal("the key \"01\" isn't an int32 (at $.byNumber)", Kinds("\"byNumber\": {\"01\": \"x\"}"));
        Assert.Equal("the key \"pink\" isn't a member of Shade (at $.byShade)", Kinds("\"byShade\": {\"pink\": 1}"));
        Assert.Equal("the key a is there twice (at $.scores)", Kinds("\"scores\": {\"a\": 1, \"a\": 2}"));
        Assert.Equal("the key kick is there twice (at $.pairs)", Kinds("\"pairs\": {\"kick\": \"x\", \"kick\": \"y\"}"));
        Assert.Equal("the string isn't valid UTF-16 (a lone surrogate) (at $.note)", Kinds("\"note\": \"\\ud800\""));
        Assert.Equal("expected a test.part (an object), found array (at $.maybePart)", Kinds("\"maybePart\": []"));
        // Unions.
        Assert.Equal("a test.shape needs \"$type\", the name of its subtype (at $.shape)", Kinds("\"shape\": {\"radius\": 1}"));
        Assert.Equal("\"test.part\" isn't a subtype of test.shape version 1 (at $.shape)", Kinds("\"shape\": {\"$type\": \"test.part\"}"));
        Assert.Equal("the key \"$type\" is there twice (at $.shape)", Kinds("\"shape\": {\"$type\": \"test.dot\", \"$type\": \"test.dot\"}"));
        Assert.Equal("expected a string, found the number 1 (at $.shape)", Kinds("\"shape\": {\"$type\": 1}"));
    }

    [Fact]
    public void WhatADeclarationDoesntAllowIsntWritten()
    {
        var error = Assert.Throws<RecordFormatException>(() => RecordSerializer.ToBinary(new Part { Name = null! }));
        Assert.Equal("null in a field that can't be null (at $.name)", error.Message);
        error = Assert.Throws<RecordFormatException>(() => RecordSerializer.ToJson(new Creature { Name = "A", Limbs = [new Limb { Kind = LimbKind.Arm }, null!] }));
        Assert.Equal("null in a field that can't be null (at $.limbs[1])", error.Message);
        error = Assert.Throws<RecordFormatException>(() => RecordSerializer.ToBinary(new Part { Name = "a\ud800" }));
        Assert.Equal("the string isn't valid UTF-16 (a lone surrogate) (at $.name)", error.Message);
        error = Assert.Throws<RecordFormatException>(() => RecordSerializer.ToJson(new Part { Name = "a\udc00b" }));
        Assert.Equal("the string isn't valid UTF-16 (a lone surrogate) (at $.name)", error.Message);
        var pairs = new Kinds { Text = "", Part = new() { Name = "" }, Pairs = [new("a", "1"), new("a", "2")] };
        Assert.Equal("the key a is there twice (at $.pairs)", Assert.Throws<RecordFormatException>(() => RecordSerializer.ToBinary(pairs)).Message);
        Assert.Equal("the key a is there twice (at $.pairs)", Assert.Throws<RecordFormatException>(() => RecordSerializer.ToJson(pairs)).Message);
        var shade = new Part { Name = "", Shade = (Shade)3 };
        Assert.Equal("3 isn't a member of Shade (at $.shade)", Assert.Throws<RecordFormatException>(() => RecordSerializer.ToBinary(shade)).Message);
        Assert.Equal("3 isn't a member of Shade (at $.shade)", Assert.Throws<RecordFormatException>(() => RecordSerializer.ToJson(shade)).Message);
    }
}
