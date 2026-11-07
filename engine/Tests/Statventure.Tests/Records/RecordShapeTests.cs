using System.Numerics;
using Statventure.Records;
using Statventure.Tests.Records.Variants;

namespace Statventure.Tests.Records;

/// <summary>
/// Record shapes (Docs/Editor.md, "Records: shapes, drafts and marks", step 1): every field kind's
/// value shape, the fields in number order with their names, defaults and required ones, display names,
/// a kind's name, folder and name rule, the marks read, the name rules, and marks that don't fit refused.
/// </summary>
public class RecordShapeTests
{
    /// <summary>A value shape in words: its kind, then what it is made of.</summary>
    static string Describe(ValueShape shape) => shape.Kind switch
    {
        ValueKind.Nullable or ValueKind.List => $"{shape.Kind}({Describe(shape.Element!)})",
        ValueKind.Map or ValueKind.Pairs => $"{shape.Kind}({Describe(shape.Key!)} → {Describe(shape.Element!)})",
        ValueKind.Record => $"Record({shape.Record!.Name})",
        ValueKind.Union => $"Union({shape.Record!.Name}: {string.Join(", ", shape.Record.Subtypes.Select(s => s.Name))})",
        ValueKind.Enum => $"Enum({shape.Type.Name})",
        _ => shape.Kind.ToString(),
    };

    [Fact]
    public void EveryFieldKindHasItsValueShape()
    {
        var shape = RecordShape.Of<Kinds>();
        var described = shape.Fields.ToDictionary(f => f.Name, f => Describe(f.Value));
        Assert.Equal(new Dictionary<string, string>
        {
            ["Flag"] = "Bool", ["Int8"] = "Int8", ["UInt8"] = "UInt8", ["Int16"] = "Int16", ["UInt16"] = "UInt16", ["Int32"] = "Int32",
            ["UInt32"] = "UInt32", ["Int64"] = "Int64", ["UInt64"] = "UInt64", ["Float32"] = "Float32", ["Float64"] = "Float64",
            ["Text"] = "String", ["Note"] = "Nullable(String)", ["Count"] = "Nullable(Int32)", ["Ratio"] = "Nullable(Float64)",
            ["Shade"] = "Enum(Shade)", ["Pace"] = "Nullable(Enum(Pace))", ["Huge"] = "Enum(Huge)", ["Bytes"] = "Bytes",
            ["MaybeBytes"] = "Nullable(Bytes)", ["Ints"] = "List(Int32)", ["Floats"] = "List(Float32)", ["Doubles"] = "List(Float64)",
            ["Bools"] = "List(Bool)", ["Shorts"] = "List(UInt16)", ["Longs"] = "List(Int64)", ["V2"] = "Vector2", ["V3"] = "Vector3",
            ["V4"] = "Vector4", ["Rotation"] = "Quaternion", ["Matrix"] = "Matrix4x4", ["Points"] = "List(Vector3)",
            ["Matrices"] = "List(Matrix4x4)", ["MaybeRotation"] = "Nullable(Quaternion)", ["Words"] = "List(String)",
            ["Gaps"] = "List(Nullable(String))", ["Names"] = "List(String)", ["ReadOnlyInts"] = "List(Int32)",
            ["Jagged"] = "List(List(Float64))", ["MaybeInts"] = "Nullable(List(Int32))", ["FloatList"] = "List(Float32)",
            ["Scores"] = "Map(String → Int32)", ["Parts"] = "Map(String → Record(test.part))", ["Pairs"] = "Pairs(String → String)",
            ["ByNumber"] = "Map(Int32 → Nullable(String))", ["ByShade"] = "Map(Enum(Shade) → Float64)",
            ["Ordered"] = "Nullable(Map(String → Int32))", ["Sparse"] = "Map(UInt64 → Nullable(Record(test.part)))",
            ["Part"] = "Record(test.part)", ["MaybePart"] = "Nullable(Record(test.part))", ["Joint"] = "Record(test.joint)",
            ["Joints"] = "List(Record(test.joint))", ["MaybeJoint"] = "Nullable(Record(test.joint))", ["Point"] = "Record(test.point)",
            ["MaybeParts"] = "List(Nullable(Record(test.part)))", ["Shape"] = "Union(test.shape: test.circle, test.square, test.dot)",
            ["Shapes"] = "List(Union(test.shape: test.circle, test.square, test.dot))",
            ["MaybeShape"] = "Nullable(Union(test.shape: test.circle, test.square, test.dot))", ["Empties"] = "List(Record(test.empty))",
            ["ShapesByName"] = "Map(String → Union(test.shape: test.circle, test.square, test.dot))", ["Paces"] = "List(Enum(Pace))",
            ["ShadePairs"] = "Pairs(Enum(Shade) → Int32)", ["Motto"] = "String",
        }, described);

        // The CLR types as declared, and the signature as the layouts write it.
        Assert.Equal(typeof(List<string>), shape.Field("Names")!.Value.Type);
        Assert.Equal(typeof(IReadOnlyList<int>), shape.Field("ReadOnlyInts")!.Value.Type);
        Assert.Equal(typeof(int?), shape.Field("Count")!.Value.Type);
        Assert.Equal(typeof(int), shape.Field("Count")!.Value.Element!.Type);
        Assert.Equal(typeof(Dictionary<int, string?>), shape.Field("ByNumber")!.Value.Type);
        Assert.Equal("map<uint64, test.part?>", shape.Field("Sparse")!.Value.Signature);
        Assert.Equal("list<list<float64>>", shape.Field("Jagged")!.Value.ToString());
        Assert.Same(RecordShape.Of<Part>(), shape.Field("Part")!.Value.Record);
        Assert.Null(shape.Field("Text")!.Value.Element);
        Assert.Null(shape.Field("Words")!.Value.Key);
        Assert.Null(shape.Field("Words")!.Value.Record);

        // What may hold records.
        Assert.Equal(["Parts", "Sparse", "Part", "MaybePart", "Joint", "Joints", "MaybeJoint", "Point", "MaybeParts", "Shape", "Shapes", "MaybeShape", "Empties", "ShapesByName"],
            shape.Fields.Where(f => f.Value.HoldsRecords).Select(f => f.Name));

        // Numbers: integers and floats, not bools.
        foreach (var field in shape.Fields.Where(f => f.Value.Kind <= ValueKind.String))
        {
            var integer = field.Value.Kind is >= ValueKind.Int8 and <= ValueKind.UInt64;
            var floating = field.Value.Kind is ValueKind.Float32 or ValueKind.Float64;
            Assert.Equal((integer, floating, integer || floating), (field.Value.IsInteger, field.Value.IsFloat, field.Value.IsNumber));
        }
    }

    [Fact]
    public void EnumsListTheirMembersInDeclarationOrder()
    {
        var pace = RecordShape.Of<Kinds>().Field("Paces")!.Value.Element!;
        Assert.Equal(
        [
            new EnumMember("Slow", "slow", "Slow", Pace.Slow), new EnumMember("Steady", "steady", "Steady", Pace.Steady),
            new EnumMember("Fast", "fast", "Fast", Pace.Fast),
        ], pace.Members);
        var huge = RecordShape.Of<Kinds>().Field("Huge")!.Value;
        Assert.Equal(new object[] { Huge.Small, Huge.Big }, huge.Members.Select(m => m.Value));
        var smoothing = RecordShape.Of<Statventure.Scene.FCurve>().Field("Smoothing")!.Value;
        Assert.Equal(["None", "Continuous acceleration"], smoothing.Members.Select(m => m.DisplayName));
        Assert.Equal(["none", "continuousAcceleration"], smoothing.Members.Select(m => m.JsonName));
        Assert.Empty(RecordShape.Of<Kinds>().Field("Text")!.Value.Members);
        // Declared out of their numbers' order: as declared.
        Assert.Equal(["Third", "First", "Second"], RecordShape.Of<NestedMaps>().Field("Rank")!.Value.Members.Select(m => m.Name));
    }

    [Fact]
    public void FieldsAreInNumberOrderWithTheirNamesAndDefaults()
    {
        var point = RecordShape.Of<Point>();
        Assert.Equal(["X", "Y", "Visible", "Pace", "Label"], point.Fields.Select(f => f.Name));
        Assert.Equal(["x", "y", "visible", "pace", "label"], point.Fields.Select(f => f.JsonName));
        Assert.Equal(["X", "Y", "Visible", "Pace", "Label"], point.Fields.Select(f => f.DisplayName));
        Assert.Equal([1, 2, 3, 4, 5], point.Fields.Select(f => f.Number));
        Assert.Equal([0, 1, 2, 3, 4], point.Fields.Select(f => f.Index));
        Assert.Equal([true, false, false, false, false], point.Fields.Select(f => f.Required));
        Assert.Equal(new object?[] { null, 2f, true, Pace.Fast, null }, point.Fields.Select(f => f.Default));
        Assert.All(point.Fields, f => Assert.Same(point, f.Record));
        Assert.Equal("test.point.visible", point.Fields[2].ToString());

        var kinds = RecordShape.Of<Kinds>();
        Assert.Equal(["Text", "Part"], kinds.Fields.Where(f => f.Required).Select(f => f.Name));
        Assert.Equal(0.5, kinds.Field("ratio")!.Default);
        Assert.Equal(Huge.Small, kinds.Field("Huge")!.Default);
        Assert.Equal(Quaternion.Identity, kinds.Field("Rotation")!.Default);
        Assert.Equal("none", kinds.Field("Motto")!.Default);
        Assert.Equal(new Point(0), kinds.Field("Point")!.Default);
        Assert.IsType<Dot>(kinds.Field("Shape")!.Default);
        Assert.Empty((int[])kinds.Field("Ints")!.Default!);
        Assert.Null(kinds.Field("Note")!.Default);
        Assert.Equal(["Name", "Weight", "Shade"], RecordShape.Of<Part>().Fields.Select(f => f.Name));
        // A union's base field is each subtype's first.
        Assert.Equal(["Tint", "Side", "Inner"], RecordShape.Of<Square>().Fields.Select(f => f.Name));

        // Getters: a field's value in a record.
        var value = new Point(1.5f, Label: "here") { Visible = false };
        Assert.Equal(new object?[] { 1.5f, 2f, false, Pace.Fast, "here" }, point.Fields.Select(f => f.Get(value)));

        // Fields by JSON or code name.
        Assert.Same(kinds.Fields[1], kinds.Field("int8"));
        Assert.Same(kinds.Fields[1], kinds.Field("Int8"));
        Assert.Same(kinds.Fields[8], kinds.Field("uInt64"));
        Assert.Null(kinds.Field("nothing"));
    }

    [Fact]
    public void DisplayNamesAreMadeFromCodeNames()
    {
        foreach (var (code, shown) in new[]
        {
            ("MaxVoices", "Max voices"), ("maxVoices", "Max voices"), ("Level", "Level"), ("HDRFactor", "HDR factor"), ("ScaleX", "Scale X"),
            ("UInt8", "U int8"), ("V2", "V2"), ("FCurve", "F curve"), ("Int8Array", "Int8 array"), ("TexCoords", "Tex coords"),
            ("inverseBindMatrices", "Inverse bind matrices"), ("snake_case_name", "Snake case name"), ("A", "A"), ("ABC", "ABC"),
            ("UseHTTP", "Use HTTP"), ("Item2Count", "Item2 count"),
        })
            Assert.Equal(shown, DisplayNames.Of(code));
        Assert.Equal("Max voices", RecordShape.Of<Statventure.Audio.EffectPatch>().Field("MaxVoices")!.DisplayName);
        Assert.Equal("Inverse bind matrices", RecordShape.Of<Statventure.Render3D.ModelSkin>().Field("InverseBindMatrices")!.DisplayName);
    }

    [Fact]
    public void ShapesKnowTheirKind()
    {
        var creature = RecordShape.Of<Creature>();
        Assert.Same(creature, RecordShape.Of(typeof(Creature)));
        Assert.Equal(typeof(Creature), creature.Type);
        Assert.Equal(("test.creature", 3, "Creature", "A creature of the tests.", "creatures"), (creature.Name, creature.Version, creature.DisplayName, creature.Help, creature.Folder));
        Assert.IsType<CapitalNames>(creature.Rule);
        Assert.False(creature.IsUnion);
        Assert.Empty(creature.Subtypes);
        Assert.Equal("test.creature", creature.ToString());
        Assert.Equal("creatures/_big_ogre", creature.PathOf("BigOgre"));
        Assert.Equal("BigOgre", creature.NameOf("creatures/_big_ogre"));

        // Without a kind mark: a name from the code name, no folder, the plain rule.
        var kinds = RecordShape.Of<Kinds>();
        Assert.Equal("Kinds", kinds.DisplayName);
        Assert.Null(kinds.Folder);
        Assert.Null(kinds.Help);
        Assert.Same(NameRule.Plain, kinds.Rule);
        Assert.Null(kinds.PathOf("x"));
        Assert.Null(kinds.NameOf("kinds/x"));
        Assert.Equal("Body part", RecordShape.Of<Part>().DisplayName);
        Assert.Same(NameRule.Plain, RecordShape.Of<Part>().Rule);
        Assert.Equal("parts/horn", RecordShape.Of<Part>().PathOf("horn"));
        Assert.Equal("horn", RecordShape.Of<Part>().NameOf("parts/horn"));
        // The engine's F-curve has its name from its mark since M11 step 6 (made from its code name it was "F curve").
        Assert.Equal("F-curve", RecordShape.Of<Statventure.Scene.FCurve>().DisplayName);
        // A kind without a folder makes no paths, whatever its rule would.
        var loose = RecordShape.Of<LooseKind>();
        Assert.IsType<AnyNames>(loose.Rule);
        Assert.Equal("any/x", loose.Rule.PathOf("", "x"));
        Assert.Null(loose.PathOf("x"));
        Assert.Null(loose.NameOf("any/x"));

        // A union: its subtypes in tag order, no fields; the subtypes are records of their own.
        var shape = RecordShape.Of<Shape>();
        Assert.True(shape.IsUnion);
        Assert.Empty(shape.Fields);
        Assert.Equal([typeof(Circle), typeof(Square), typeof(Dot)], shape.Subtypes.Select(s => s.Type));
        Assert.Same(RecordShape.Of<Circle>(), shape.Subtypes[0]);
        Assert.Equal(["test.walk", "test.leap", "test.fly"], RecordShape.Of<Move>().Subtypes.Select(s => s.Name));
        Assert.Equal(2, RecordShape.Of<Move>().Version);
        Assert.False(RecordShape.Of<Circle>().IsUnion);

        // Only current record types have shapes.
        Assert.Contains("isn't a record type", Assert.Throws<InvalidOperationException>(() => RecordShape.Of<string>()).Message);
        Assert.Contains("a past version", Assert.Throws<InvalidOperationException>(() => RecordShape.Of<Creature.V1>()).Message);
    }

    [Fact]
    public void MarksAreRead()
    {
        var kinds = RecordShape.Of<Kinds>();
        FieldShape F(string name) => kinds.Field(name)!;
        Assert.Equal("A flag.", F("Flag").Help);
        Assert.Equal("What it is called.", RecordShape.Of<Creature>().Field("Name")!.Help);
        Assert.Equal("m/s", RecordShape.Of<Creature>().Field("Speed")!.Unit);
        Assert.Equal("m", F("Points").Unit);
        Assert.Equal(new FieldLimits(1, 200, false, true), F("UInt8").Limits);
        Assert.Equal(new FieldLimits(-1, 1, false, false), F("Float32").Limits);
        Assert.Equal(new FieldLimits(-1, 1, true, false), F("ByShade").Limits);
        Assert.Equal(new FieldLimits(0, double.PositiveInfinity, true, false), RecordShape.Of<Part>().Field("Weight")!.Limits);
        Assert.Equal(["a", "", "β"], F("Words").Choices);
        Assert.Equal(["Note", "V3", "V4"], kinds.Fields.Where(f => f.IsColour).Select(f => f.Name));
        Assert.Equal(["Bytes", "MaybeBytes", "Rotation", "Points", "Parts"], kinds.Fields.Where(f => f.IsBulk).Select(f => f.Name));
        Assert.Equal(new string?[] { "{0} bytes", null, null, "{0:N0} points", "{0} parts" }, kinds.Fields.Where(f => f.IsBulk).Select(f => f.BulkSummary));

        // Fields without marks have none.
        var plain = F("Int16");
        Assert.Null(plain.Help);
        Assert.Null(plain.Unit);
        Assert.Null(plain.Limits);
        Assert.Null(plain.Choices);
        Assert.False(plain.IsColour);
        Assert.False(plain.IsBulk);
        Assert.Null(plain.BulkSummary);
        Assert.Empty(plain.References);
        Assert.Null(plain.ReferenceCondition);

        // References: the kind, its folder and rule unless the mark gives them, keys or values.
        var names = Assert.Single(F("Names").References);
        Assert.Equal((RecordShape.Of<Creature>(), "creatures", false), (names.Kind, names.Folder, names.Keys));
        Assert.Null(names.When);
        Assert.IsType<CapitalNames>(names.Rule);
        Assert.Empty(names.Is);
        var pairs = Assert.Single(F("Pairs").References);
        Assert.Equal((RecordShape.Of<Part>(), "parts", true), (pairs.Kind, pairs.Folder, pairs.Keys));
        Assert.Same(NameRule.Plain, pairs.Rule);
        var sets = Assert.Single(F("ByNumber").References);
        Assert.Equal((RecordShape.Of<Kinds>(), "kinds/sets"), (sets.Kind, sets.Folder));
        Assert.Same(NameRule.FullPath, sets.Rule);
        Assert.Equal("kinds/sets/a/b", sets.PathOf("kinds/sets/a/b"));
        Assert.Equal("kinds/sets/a", sets.NameOf("kinds/sets/a"));

        // Conditional ones first, the condition's field resolved.
        var text = F("Text");
        Assert.Same(F("Motto"), text.ReferenceCondition);
        var byValue = text.References.OrderBy(r => r.Is[0], StringComparer.Ordinal).ToList();
        Assert.Equal(["creature"], byValue[0].Is);
        Assert.Equal(["part", "piece"], byValue[1].Is);
        Assert.Equal(["creatures", "spare-parts"], byValue.Select(r => r.Folder));
        Assert.All(text.References, r => Assert.Same(F("Motto"), r.When));
        var part = RecordShape.Of<Part>().Field("Name")!;
        Assert.Equal([RecordShape.Of<Creature>(), RecordShape.Of<Part>()], part.References.Select(r => r.Kind));
        Assert.Same(RecordShape.Of<Part>().Field("Shade"), part.References[0].When);
        Assert.Null(part.References[1].When);
    }

    [Fact]
    public void ReferencesFollowTheirCondition()
    {
        var text = RecordShape.Of<Kinds>().Field("Text")!;
        Assert.Equal("test.creature", text.ReferenceFor("creature")!.Kind.Name);
        Assert.Equal("spare-parts", text.ReferenceFor("part")!.Folder);
        Assert.Equal("spare-parts", text.ReferenceFor("piece")!.Folder);
        // No mark without a condition: other values name nothing.
        Assert.Null(text.ReferenceFor("none"));
        Assert.Null(text.ReferenceFor(null));
        Assert.Equal("spare-parts", text.ReferenceIn(new Kinds { Text = "t", Part = new() { Name = "p" }, Motto = "piece" })!.Folder);
        Assert.Null(text.ReferenceIn(new Kinds { Text = "t", Part = new() { Name = "p" } }));

        // An enum condition by its members' code names, and the mark without a condition otherwise.
        var name = RecordShape.Of<Part>().Field("Name")!;
        Assert.Equal("test.creature", name.ReferenceFor(Shade.Dark)!.Kind.Name);
        Assert.Equal("test.creature", name.ReferenceFor(Shade.Golden)!.Kind.Name);
        Assert.Equal("test.part", name.ReferenceFor(Shade.Pale)!.Kind.Name);
        Assert.Equal("test.part", name.ReferenceFor(null)!.Kind.Name);
        Assert.Equal("test.creature", name.ReferenceIn(new Part { Name = "x", Shade = Shade.Dark })!.Kind.Name);

        // A field without a condition: its one mark, whatever is given.
        var names = RecordShape.Of<Kinds>().Field("Names")!;
        Assert.Same(names.References[0], names.ReferenceFor("anything"));
        Assert.Null(RecordShape.Of<Kinds>().Field("Words")!.ReferenceFor("a"));
    }

    [Fact]
    public void BulkValuesAreSummarized()
    {
        var kinds = RecordShape.Of<Kinds>();
        Assert.Equal("1,234 points", kinds.Field("Points")!.Summarize(new Vector3[1234]));
        Assert.Equal("7 bytes", kinds.Field("Bytes")!.Summarize(new byte[7]));
        Assert.Equal("2 parts", kinds.Field("Parts")!.Summarize(new Dictionary<string, Part> { ["a"] = new() { Name = "a" }, ["b"] = new() { Name = "b" } }));
        Assert.Equal("{X:0 Y:0 Z:0 W:1}", kinds.Field("Rotation")!.Summarize(Quaternion.Identity));
        Assert.Null(kinds.Field("Points")!.Summarize(null));
    }

    [Fact]
    public void NameRulesMakePathsFromNamesAndBack()
    {
        var plain = NameRule.Plain;
        Assert.IsType<PlainNames>(plain);
        Assert.Equal("models/effects/sparks", plain.PathOf("models/effects", "sparks"));
        Assert.Equal("sparks", plain.NameOf("models/effects", "models/effects/sparks"));
        foreach (var bad in new[] { "", "a/b", "Sparks", "a b", "con", "x." })
            Assert.Null(plain.PathOf("models/effects", bad));
        foreach (var bad in new[] { "models/effects", "models/effects/", "models/effects/a/b", "models/effectsx/a", "models/effectz/a", "models/other/a", "models/effects/A", "models" })
            Assert.Null(plain.NameOf("models/effects", bad));

        var path = NameRule.FullPath;
        Assert.IsType<PathNames>(path);
        Assert.Equal("models/stages/cave", path.PathOf("models/stages", "models/stages/cave"));
        Assert.Equal("models/stages/deep/cave", path.NameOf("models/stages", "models/stages/deep/cave"));
        foreach (var bad in new[] { "cave", "models/stages", "models/stages/", "models/stagesx/cave", "models/stagez/cave", "models/stages/Cave", "models/stages//cave" })
        {
            Assert.Null(path.PathOf("models/stages", bad));
            Assert.Null(path.NameOf("models/stages", bad));
        }

        // A kind's own rule, made once.
        var capitals = RecordShape.Of<Creature>().Rule;
        Assert.Same(capitals, RecordShape.Of<Kinds>().Field("Names")!.References[0].Rule);
        Assert.Equal("creatures/_ogre", capitals.PathOf("creatures", "Ogre"));
        Assert.Null(capitals.PathOf("creatures", "an ogre"));
    }

    [Fact]
    public void MarksThatDontFitAreRefused()
    {
        string Refused(Type type) => Assert.Throws<InvalidOperationException>(() => RecordShape.Of(type).Fields).Message;
        Assert.Equal("LimitsOnText.A: [Limits] is for a field of numbers (or lists or maps of them), not string", Refused(typeof(LimitsOnText)));
        Assert.Equal("LimitsOnBools.A: [Limits] is for a field of numbers (or lists or maps of them), not list<bool>", Refused(typeof(LimitsOnBools)));
        Assert.Equal("LimitsBackwards.A: limits need a min at most their max, not 2 and 1", Refused(typeof(LimitsBackwards)));
        Assert.Equal("LimitsNaN.A: limits need a min at most their max, not 0 and NaN", Refused(typeof(LimitsNaN)));
        Assert.Equal("ChoicesOnNumbers.A: [Choices] is for a field of strings (or lists or maps of them), not map<string, int32>", Refused(typeof(ChoicesOnNumbers)));
        Assert.Equal("ColourOnNumber.A: [Colour] is for a Vector3, a Vector4 or a string (or a list of them), not float32", Refused(typeof(ColourOnNumber)));
        Assert.Equal("ColourInMap.A: [Colour] is for a Vector3, a Vector4 or a string (or a list of them), not map<string, vector3>", Refused(typeof(ColourInMap)));
        Assert.Equal("ReferencesOnNumber.A: [References] is for a field of strings (or lists or maps of them), not int32", Refused(typeof(ReferencesOnNumber)));
        Assert.Equal("KeysOfAList.A: [References] on keys is for a map with string keys, not list<string>", Refused(typeof(KeysOfAList)));
        Assert.Equal("KeysNotStrings.A: [References] on keys is for a map with string keys, not map<int32, string>", Refused(typeof(KeysNotStrings)));
        Assert.Equal("ReferenceWithoutFolder.A: [References] to test.point needs a folder (the kind has none)", Refused(typeof(ReferenceWithoutFolder)));
        Assert.Equal("ReferenceBadFolder.A: the folder \"Parts\" isn't a data path ('P' isn't allowed (only a–z, 0–9, '-', '_', '.' and '/'))", Refused(typeof(ReferenceBadFolder)));
        Assert.Equal("System.String isn't a record type (it has no [Record])", Refused(typeof(ReferenceToAClass)));
        Assert.Equal("ReferenceBadRule.A: String isn't a name rule (a NameRule type with a public parameterless constructor)", Refused(typeof(ReferenceBadRule)));
        Assert.Equal("RuleAbstract.A: OpenRule isn't a name rule (a NameRule type with a public parameterless constructor)", Refused(typeof(RuleAbstract)));
        Assert.Equal("RuleWithoutConstructor.A: PrefixNames isn't a name rule (a NameRule type with a public parameterless constructor)", Refused(typeof(RuleWithoutConstructor)));
        Assert.Equal("WhenWithoutIs.A: a condition needs both When and Is (with at least one value)", Refused(typeof(WhenWithoutIs)));
        Assert.Equal("IsWithoutWhen.A: a condition needs both When and Is (with at least one value)", Refused(typeof(IsWithoutWhen)));
        Assert.Equal("IsEmpty.A: a condition needs both When and Is (with at least one value)", Refused(typeof(IsEmpty)));
        Assert.Equal("TwoOtherwise.A: more than one [References] without a condition", Refused(typeof(TwoOtherwise)));
        Assert.Equal("TwoConditions.A: [References] conditions on different fields", Refused(typeof(TwoConditions)));
        Assert.Equal("WhenNotAField.A: [References] has a condition on Missing, which isn't a field of WhenNotAField", Refused(typeof(WhenNotAField)));
        Assert.Equal("WhenANumber.A: [References] has a condition on B, which is a int32, not a string or an enum", Refused(typeof(WhenANumber)));
        Assert.Equal("IsNotAMember.A: [References] has a condition on B being Purple, which isn't a member of Shade", Refused(typeof(IsNotAMember)));
        Assert.Equal("IsTwice.A: two [References] hold when B is y", Refused(typeof(IsTwice)));
        Assert.Equal("KindBadFolder: the kind's folder \"Bad Folder\" isn't a data path ('B' isn't allowed (only a–z, 0–9, '-', '_', '.' and '/'))",
            Assert.Throws<InvalidOperationException>(() => RecordShape.Of<KindBadFolder>()).Message);
        // A shape whose fields are refused stays refused.
        Assert.Throws<InvalidOperationException>(() => RecordShape.Of<LimitsOnText>().Fields);
    }
}
