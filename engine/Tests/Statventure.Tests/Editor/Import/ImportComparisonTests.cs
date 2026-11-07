using System.Numerics;
using Statventure.Editor.Import;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Records;
using Statventure.Tests.Records.Variants;

namespace Statventure.Tests.Editor.Import;

/// <summary>
/// How an entry and a file imported again over it are compared (P29; the manager's decision of 2026-11-07, Q1), on the
/// test kinds' every shape of value: by path along the kind's shape, bulk fields never listed and always the file's, a
/// plain value compared bit for bit, records field by field, a list or map of the same length (keys) element by element
/// and otherwise as a whole, a nullable set on one side only and a union of another subtype as a whole, a list holding
/// bulk data with the file's length; the merge keeping every difference not taken.
/// </summary>
public class ImportComparisonTests
{
    static Kinds Base() => new()
    {
        Text = "t",
        Part = new Part { Name = "arm" },
        Note = "#102030",
        Count = 3,
        Ints = [1, 2, 3],
        Scores = new() { ["a"] = 1, ["b"] = 2 },
        Shape = new Circle { Radius = 1 },
        V3 = new Vector3(0.5f, 0.25f, 0),
        Bytes = [1, 2],
        Points = [Vector3.One],
        Parts = new Dictionary<string, Part> { ["x"] = new() { Name = "x" } },
        Matrix = Matrix4x4.Identity,
    };

    static ImportComparison Compare(Kinds present, Kinds file) => ImportComparison.Of(RecordDraft.From(present), RecordDraft.From(file));

    static string[] Paths(ImportComparison comparison) => [.. comparison.Differences.Select(d => d.Path.ToString())];

    [Fact]
    public void TheSameValuesDifferInNothing()
    {
        Assert.Empty(Compare(Base(), Base()).Differences);
    }

    [Fact]
    public void PlainValuesThatDifferAreListedInTheKindsOrderBulkNever()
    {
        var file = Base();
        file = new Kinds
        {
            Flag = true, Int8 = -1, Float32 = -0f, Text = "u", Note = "#102031", Shade = Shade.Golden, V3 = new Vector3(0.5f, 0.25f, 1),
            Matrix = Matrix4x4.CreateScale(2), Motto = "creature",
            // Bulk: never listed.
            Bytes = [9], Points = [Vector3.Zero], Parts = new Dictionary<string, Part>(), Rotation = Quaternion.CreateFromYawPitchRoll(1, 0, 0),
            Part = file.Part, Count = file.Count, Ints = file.Ints, Scores = file.Scores, Shape = file.Shape,
        };
        var comparison = Compare(Base(), file);
        Assert.Equal(["$.flag", "$.int8", "$.float32", "$.text", "$.note", "$.shade", "$.v3", "$.matrix", "$.motto"], Paths(comparison));
        Assert.Equal(["Flag", "Int8", "Float32", "Text", "Note", "Shade", "V3", "Matrix", "Motto"], comparison.Differences.Select(d => d.Name));
        Assert.All(comparison.Differences, d => Assert.False(d.IsWhole));
        // 0 and -0 differ bit for bit; the field is the one holding the value.
        Assert.Equal((0f, -0f), ((float)((DraftLeaf)comparison.Differences[2].Present).Value, (float)((DraftLeaf)comparison.Differences[2].File).Value));
        Assert.Equal("note", comparison.Differences[4].Field.JsonName);
        // Kept, every one: the present values over the file's bulk data; taken, every one: the file's draft.
        var kept = (Kinds)comparison.Merge().Build().Value!;
        Assert.Equal((false, (sbyte)0, "t", Shade.Pale, "none"), (kept.Flag, kept.Int8, kept.Text, kept.Shade, kept.Motto));
        Assert.Equal(file.Bytes, kept.Bytes);
        Assert.Same(file.Points, kept.Points);
        Assert.Equal(file.Rotation, kept.Rotation);
        Assert.Same(comparison.File, comparison.Merge(_ => true));
        var mixed = (Kinds)comparison.Merge(d => d.Name is "Text" or "Shade").Build().Value!;
        Assert.Equal(("u", Shade.Golden, false), (mixed.Text, mixed.Shade, mixed.Flag));
    }

    [Fact]
    public void ListsAndMapsOfOneLengthAreComparedElementByElementOthersAsAWhole()
    {
        var file = Base();
        file = new Kinds
        {
            Text = "t", Part = file.Part, Note = file.Note, Count = file.Count, V3 = file.V3, Bytes = file.Bytes, Points = file.Points, Parts = file.Parts, Shape = file.Shape,
            Ints = [1, 5, 3], Scores = new() { ["a"] = 1, ["b"] = 7 }, Floats = [1.5f], ShadePairs = [new(Shade.Dark, 1)],
        };
        var comparison = Compare(Base(), file);
        Assert.Equal(["$.ints[1]", "$.floats", "$.scores[\"b\"]", "$.shadePairs"], Paths(comparison));
        Assert.Equal(["Ints › 1", "Floats", "Scores › b", "Shade pairs"], comparison.Differences.Select(d => d.Name));
        Assert.Equal([false, true, false, true], comparison.Differences.Select(d => d.IsWhole));
        // A map of other keys (or of its keys in another order) is a whole value.
        var other = Base();
        other = new Kinds { Text = "t", Part = other.Part, Note = other.Note, Count = other.Count, V3 = other.V3, Ints = other.Ints, Shape = other.Shape,
            Scores = new() { ["b"] = 2, ["a"] = 1 } };
        Assert.Equal(["$.scores"], Paths(Compare(Base(), other)).Where(p => p.StartsWith("$.scores", StringComparison.Ordinal)));
        var kept = (Kinds)comparison.Merge().Build().Value!;
        Assert.Equal([1, 2, 3], kept.Ints);
        Assert.Equal(2, kept.Scores["b"]);
        Assert.Empty(kept.Floats);
        Assert.Empty(kept.ShadePairs);
        var taken = (Kinds)comparison.Merge(d => d.IsWhole).Build().Value!;
        Assert.Equal([1, 2, 3], taken.Ints);
        Assert.Equal([1.5f], taken.Floats);
        Assert.Equal(2, taken.Scores["b"]);
    }

    [Fact]
    public void RecordsFieldByFieldNullablesAndUnionsOfAnotherShapeAsAWhole()
    {
        var file = Base();
        file = new Kinds
        {
            Text = "t", Note = null, Ints = file.Ints, Scores = file.Scores, V3 = file.V3, Bytes = file.Bytes, Points = file.Points, Parts = file.Parts,
            Part = new Part { Name = "leg", Weight = 2 }, Count = 4, MaybePart = new Part { Name = "tail" },
            Shape = new Circle { Radius = 2, Tint = "ink" }, MaybeShape = new Dot(), Shapes = [new Circle { Radius = 1 }],
        };
        var present = Base();
        var comparison = Compare(present, file);
        Assert.Equal(["$.note", "$.count", "$.part.name", "$.part.weight", "$.maybePart", "$.shape.radius", "$.shapes", "$.maybeShape"], Paths(comparison));
        Assert.Equal(["Note", "Count", "Part › Name", "Part › Weight", "Maybe part", "Shape › Radius", "Shapes", "Maybe shape"], comparison.Differences.Select(d => d.Name));
        Assert.Equal([true, false, false, false, true, false, true, true], comparison.Differences.Select(d => d.IsWhole));
        // A difference's shape: a nullable's where it is set on one side only, its value's where both are.
        Assert.Equal([ValueKind.Nullable, ValueKind.Int32], comparison.Differences.Take(2).Select(d => d.Shape.Kind));
        // A union of another subtype.
        var square = new Kinds { Text = "t", Part = present.Part, Note = present.Note, Count = present.Count, Ints = present.Ints, Scores = present.Scores, V3 = present.V3,
            Shape = new Square { Side = 1 } };
        Assert.Contains("$.shape", Paths(Compare(present, square)));
        Assert.True(Compare(present, square).Differences.Single(d => d.Path.ToString() == "$.shape").IsWhole);
        var kept = (Kinds)comparison.Merge().Build().Value!;
        Assert.Equal(("#102030", 3, "arm", 1.0, null, 1.0, 0), (kept.Note, kept.Count, kept.Part.Name, kept.Part.Weight, kept.MaybePart, ((Circle)kept.Shape).Radius, kept.Shapes.Length));
        var taken = (Kinds)comparison.Merge(_ => true).Build().Value!;
        Assert.Equal((null, 4, "leg", "tail"), (taken.Note, taken.Count, taken.Part.Name, taken.MaybePart!.Name));
    }

    [Fact]
    public void WhatHoldsBulkDataFollowsTheFile()
    {
        var model = RecordShape.Of<ModelData>();
        Assert.True(ImportComparison.HoldsBulk(model.Field("nodes")!.Value));
        Assert.True(ImportComparison.HoldsBulk(model.Field("animations")!.Value));
        Assert.False(ImportComparison.HoldsBulk(model.Field("materials")!.Value));
        Assert.False(ImportComparison.HoldsBulk(model.Field("cameras")!.Value));
        Assert.True(ImportComparison.HoldsBulk(model));
        Assert.True(ImportComparison.HoldsBulk(RecordShape.Of<Kinds>()));
        Assert.False(ImportComparison.HoldsBulk(RecordShape.Of<Part>()));
        Assert.False(ImportComparison.HoldsBulk(RecordShape.Of<Shape>()));
        // A list holding bulk data: the file's length, the elements both have compared by index.
        var present = Model(["a", "b", "c"]);
        var file = Model(["a", "x"]);
        var comparison = ImportComparison.Of(RecordDraft.From(present), RecordDraft.From(file));
        Assert.Equal(["$.nodes[1].name"], Paths(comparison));
        var kept = (ModelData)comparison.Merge().Build().Value!;
        Assert.Equal(["a", "b"], kept.Nodes.Select(n => n.Name));
        // Longer in the file: the elements the entry has compared, the others the file's.
        var longer = ImportComparison.Of(RecordDraft.From(Model(["a"])), RecordDraft.From(Model(["b", "c"])));
        Assert.Equal(["$.nodes[0].name"], Paths(longer));
        Assert.Equal(["a", "c"], ((ModelData)longer.Merge().Build().Value!).Nodes.Select(n => n.Name));
        // A value holding bulk data whose structure differs (not given in the entry): the file's, not listed.
        Assert.Empty(ImportComparison.Of(RecordDraft.From(present).With("nodes", DraftValue.Missing), RecordDraft.From(file)).Differences);
        Assert.Equal(["a", "x"], ((ModelData)ImportComparison.Of(RecordDraft.From(present).With("nodes", DraftValue.Missing), RecordDraft.From(file)).Merge().Build().Value!).Nodes.Select(n => n.Name));
        // A nullable set on one side only, where it holds bulk data: the file's.
        Assert.Empty(Paths(Compare(Base(), new Kinds { Text = "t", Part = Base().Part, Note = Base().Note, Count = 3, Ints = [1, 2, 3], Scores = Base().Scores, Shape = Base().Shape,
            V3 = Base().V3, MaybeBytes = [1] })).Where(p => p.StartsWith("$.maybeBytes", StringComparison.Ordinal)));
    }

    static ModelData Model(IEnumerable<string> names) => new()
    {
        Nodes = [.. names.Select(n => new ModelNode(n, -1, [], Matrix4x4.Identity, Vector3.Zero, Quaternion.Identity, Vector3.One, -1, -1))],
        Meshes = [],
        Materials = [],
        Skins = [],
        Roots = [],
    };

    [Fact]
    public void ABulkHoldersStructureIsntListedAsAWhole()
    {
        // Two kinds of comparisons apart.
        Assert.StartsWith("a test.kinds can't be imported again over a test.part",
            Assert.Throws<ArgumentException>(() => ImportComparison.Of(RecordDraft.From(new Part { Name = "a" }), RecordDraft.From(Base()))).Message);
        Assert.True(ImportComparison.Same(new byte[] { 1, 2 }, new byte[] { 1, 2 }));
        Assert.False(ImportComparison.Same(new byte[] { 1, 2 }, new byte[] { 1, 3 }));
        Assert.True(ImportComparison.Same(double.NaN, double.NaN));
        Assert.False(ImportComparison.Same(0.0, -0.0));
        Assert.False(ImportComparison.Same(new Vector2(0, 1), new Vector2(-0f, 1)));
        Assert.False(ImportComparison.Same(new Vector4(0, 1, 2, 3), new Vector4(0, 1, 2, 4)));
        Assert.False(ImportComparison.Same(new Quaternion(0, 1, 2, 3), new Quaternion(0, 1, 2, 4)));
        Assert.False(ImportComparison.Same(Matrix4x4.Identity, Matrix4x4.CreateTranslation(0, 0, 1)));
        Assert.True(ImportComparison.Same("a", "a"));
    }

    [Fact]
    public void TheListShowsEachValueAsTheFormWritesIt()
    {
        var present = Base();
        var file = new Kinds
        {
            Text = "", Note = "#a0b0c0", Count = null, V3 = new Vector3(1, 0, 0), V2 = new Vector2(0.5f, 2), Shade = Shade.Golden, Pace = Pace.Slow,
            Matrix = Matrix4x4.CreateScale(2), MaybeRotation = Quaternion.Identity, Ints = [1], Floats = [], Scores = new() { ["a"] = 1 },
            Part = new Part { Name = "arm", Weight = 2.5 }, Shape = new Square(), UInt8 = 7, Huge = Huge.Big,
            Bytes = present.Bytes, Points = present.Points, Parts = present.Parts,
        };
        var comparison = Compare(present, file);
        string Text(string path, bool fromFile)
        {
            var difference = comparison.Differences.Single(d => d.Path.ToString() == path);
            return ImportPart.Describe(fromFile ? difference.File : difference.Present, difference);
        }
        Assert.Equal(("t", "(empty)"), (Text("$.text", false), Text("$.text", true)));
        Assert.Equal(("#102030", "#a0b0c0"), (Text("$.note", false), Text("$.note", true)));
        Assert.Equal(("3", "none"), (Text("$.count", false), Text("$.count", true)));
        Assert.Equal(("#804000", "#ff0000"), (Text("$.v3", false), Text("$.v3", true)));
        Assert.Equal(("(0, 0)", "(0.5, 2)"), (Text("$.v2", false), Text("$.v2", true)));
        Assert.Equal(("Pale", "Golden"), (Text("$.shade", false), Text("$.shade", true)));
        Assert.Equal(("none", "Slow"), (Text("$.pace", false), Text("$.pace", true)));
        Assert.Equal(("Small", "Big"), (Text("$.huge", false), Text("$.huge", true)));
        Assert.Equal(("0", "7"), (Text("$.uInt8", false), Text("$.uInt8", true)));
        Assert.Equal(("1 kg", "2.5 kg"), (Text("$.part.weight", false), Text("$.part.weight", true)));
        Assert.Equal("(2, 0, 0, 0; 0, 2, 0, 0; 0, 0, 2, 0; 0, 0, 0, 1)", Text("$.matrix", true));
        Assert.Equal(("none", "(0, 0, 0, 1)"), (Text("$.maybeRotation", false), Text("$.maybeRotation", true)));
        Assert.Equal(("3 elements", "1 element"), (Text("$.ints", false), Text("$.ints", true)));
        Assert.Equal(("2 entries", "1 entry"), (Text("$.scores", false), Text("$.scores", true)));
        Assert.Equal(("Circle", "Square"), (Text("$.shape", false), Text("$.shape", true)));
        var floats = ImportComparison.Of(RecordDraft.From(present), RecordDraft.From(new Kinds { Text = "t", Part = present.Part, Floats = [1, 2],
            Scores = new(), Ints = present.Ints, Note = present.Note, Count = 3, V3 = present.V3, Shape = present.Shape })).Differences;
        var none = floats.Single(d => d.Path.ToString() == "$.floats");
        Assert.Equal(("no elements", "2 elements"), (ImportPart.Describe(none.Present, none), ImportPart.Describe(none.File, none)));
        var noScores = floats.Single(d => d.Path.ToString() == "$.scores");
        Assert.Equal("no entries", ImportPart.Describe(noScores.File, noScores));
        // A record not given yet, and a value no rule shows otherwise.
        var missing = new ImportDifference(RecordPath.Root.Field("part"), "Part", DraftValue.Missing, RecordDraft.From(present.Part), RecordShape.Of<Kinds>().Field("part")!.Value, RecordShape.Of<Kinds>().Field("part")!);
        Assert.Equal(("not given", "Body part"), (ImportPart.Describe(missing.Present, missing), ImportPart.Describe(missing.File, missing)));
    }

    [Fact]
    public void AColourValuesSwatch()
    {
        var present = Base();
        var file = new Kinds { Text = "t", Part = present.Part, Note = "ink", Count = 3, Ints = present.Ints, Scores = present.Scores, Shape = present.Shape,
            V3 = new Vector3(1, 0, 0), V4 = new Vector4(0, 1, 0, 0.5f), Flag = true };
        var differences = Compare(present, file).Differences;
        string? Swatch(string path, bool fromFile)
        {
            var d = differences.Single(d => d.Path.ToString() == path);
            return ImportPart.Swatch(fromFile ? d.File : d.Present, d);
        }
        Assert.Equal(("#102030", null), (Swatch("$.note", false), Swatch("$.note", true)));
        Assert.Equal(("#804000", "#ff0000"), (Swatch("$.v3", false), Swatch("$.v3", true)));
        Assert.Equal(("#00000000", "#00ff0080"), (Swatch("$.v4", false), Swatch("$.v4", true)));
        Assert.Null(Swatch("$.flag", true));
        var empty = new ImportDifference(RecordPath.Root.Field("note"), "Note", DraftValue.Of(""), DraftValue.Null, RecordShape.Of<Kinds>().Field("note")!.Value, RecordShape.Of<Kinds>().Field("note")!);
        Assert.Equal((null, null), (ImportPart.Swatch(empty.Present, empty), ImportPart.Swatch(empty.File, empty)));
    }

    [Fact]
    public void BulkDataUnderANullableARecordAUnionOrAMapIsTheFiles()
    {
        // Two records deep, under a nullable, in a union's subtype, in a map's values.
        Assert.True(ImportComparison.HoldsBulk(RecordShape.Of<ImportDeep>()));
        var box = RecordShape.Of<ImportBox>();
        Assert.True(ImportComparison.HoldsBulk(box.Field("maybe")!.Value));
        Assert.True(ImportComparison.HoldsBulk(box.Field("byName")!.Value));
        Assert.True(ImportComparison.HoldsBulk(box.Field("shape")!.Value));
        Assert.True(ImportComparison.HoldsBulk(RecordShape.Of<ImportShape>()));
        Assert.False(ImportComparison.HoldsBulk(RecordShape.Of<ImportPlain>()));
        // Such a value whose structure differs isn't listed: the file's; what lies beside the bulk data is compared.
        var present = new ImportDeep { Box = new ImportBox { Inner = new ImportLeaf { Name = "a", Data = [1] }, Maybe = null,
            ByName = new() { ["x"] = new ImportLeaf() }, Shape = new ImportPlain { Size = 1 } } };
        var file = new ImportDeep { Box = new ImportBox { Inner = new ImportLeaf { Name = "b", Data = [2] }, Maybe = new ImportLeaf { Data = [3] },
            ByName = new() { ["y"] = new ImportLeaf() }, Shape = new ImportHeavy() } };
        var comparison = ImportComparison.Of(RecordDraft.From(present), RecordDraft.From(file));
        Assert.Equal(["$.box.inner.name"], Paths(comparison));
        var kept = (ImportDeep)comparison.Merge().Build().Value!;
        Assert.Equal(("a", "y"), (kept.Box.Inner.Name, kept.Box.ByName.Keys.Single()));
        Assert.Equal([2], kept.Box.Inner.Data);
        Assert.Equal([3], kept.Box.Maybe!.Data);
        Assert.IsType<ImportHeavy>(kept.Box.Shape);
    }

    [Fact]
    public void AMemberIsShownByItsDisplayNameAndATextNotMarkedAColourHasNoSwatch()
    {
        var curve = (KeyEasing easing) => new Statventure.Scene.FCurve([Statventure.Scene.FCurveKey.Ease(0, 0, KeyInterpolation.Sine, easing), new Statventure.Scene.FCurveKey(1, 1)]);
        var easing = ImportComparison.Of(RecordDraft.From(curve(KeyEasing.InOut)), RecordDraft.From(curve(KeyEasing.In))).Differences.Single();
        Assert.Equal(("$.keys[0].easing", "In out", "In"), (easing.Path.ToString(), ImportPart.Describe(easing.Present, easing), ImportPart.Describe(easing.File, easing)));
        var present = Base();
        var text = Compare(present, new Kinds { Text = "#203040", Part = present.Part, Note = present.Note, Count = 3, Ints = present.Ints, Scores = present.Scores,
            Shape = present.Shape, V3 = present.V3 }).Differences.Single(d => d.Path.ToString() == "$.text");
        Assert.Equal(((string?)null, (string?)null, "#203040"), (ImportPart.Swatch(text.Present, text), ImportPart.Swatch(text.File, text), ImportPart.Describe(text.File, text)));
        // A nullable member, by its display name too: set on both sides (the member's shape), on one side (the nullable's).
        var both = ImportComparison.Of(RecordDraft.From(new ImportPlain { Easing = KeyEasing.InOut }), RecordDraft.From(new ImportPlain { Easing = KeyEasing.Out })).Differences.Single();
        Assert.Equal(("In out", "Out"), (ImportPart.Describe(both.Present, both), ImportPart.Describe(both.File, both)));
        var one = ImportComparison.Of(RecordDraft.From(new ImportPlain { Easing = KeyEasing.InOut }), RecordDraft.From(new ImportPlain())).Differences.Single();
        Assert.Equal((ValueKind.Nullable, "In out", "none"), (one.Shape.Kind, ImportPart.Describe(one.Present, one), ImportPart.Describe(one.File, one)));
    }
}
