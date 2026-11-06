using System.Numerics;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Records.Variants;

namespace Statventure.Tests.Records;

/// <summary>
/// Record drafts (Docs/Editor.md, "Records: shapes, drafts and marks", step 1): every fixture and every
/// committed pack's records read into drafts and built back to the same binary form; blank drafts of
/// every engine kind; what can't be built, at its path; drafts read through their shapes, bulk values
/// kept as they are; changes by field and by path.
/// </summary>
public class RecordDraftTests
{
    static object Build(RecordDraft draft)
    {
        var built = draft.Build();
        Assert.True(built.Built, string.Join("\n", built.Problems));
        Assert.Empty(built.Problems);
        return built.Value!;
    }

    static IReadOnlyList<string> Problems(RecordDraft draft)
    {
        var built = draft.Build();
        Assert.False(built.Built);
        Assert.Null(built.Value);
        return [.. built.Problems.Select(p => p.ToString())];
    }

    static Kinds Minimal() => new() { Text = "t", Part = new() { Name = "p" } };

    [Fact]
    public void EveryFixtureBuildsBackToTheSameBinaryForm()
    {
        var count = 0;
        foreach (var (type, label, form) in RecordFixtures.All())
        {
            var (value, _) = RecordFixtures.Load(type, label, form);
            var root = RecordFixtures.Types[type];
            var draft = RecordDraft.From(value);
            Assert.Same(RecordShape.Of(value.GetType()), draft.Shape);
            var built = Build(draft);
            Assert.NotSame(value, built);
            var expected = RecordSerializer.ToBinary(root, value);
            Assert.True(expected.AsSpan().SequenceEqual(RecordSerializer.ToBinary(root, built)), $"{type}/{label}{form} isn't built back to the same binary form");
            count++;
        }
        Assert.True(count >= 40, $"only {count} fixtures");
    }

    [Fact]
    public void EveryCommittedPacksRecordsBuildBackToTheirStoredForm()
    {
        var records = 0;
        foreach (var file in Directory.GetFiles(TestPaths.Packs, "*.pak"))
        {
            using var pack = PackFile.Open(file);
            foreach (var entry in pack.Entries.Where(e => e.IsRecord))
            {
                var content = pack.Read(entry);
                var type = RecordTypes.Engine.Find(RecordSerializer.RootTypeName(content))!;
                var built = Build(RecordDraft.From(RecordSerializer.FromBinary(type, content)));
                Assert.True(content.AsSpan().SequenceEqual(RecordSerializer.ToBinary(type, built)), $"{file}: {entry.Path} isn't built back to its stored form");
                records++;
            }
        }
        // audio.pak's 118 records (Docs/Assets.md, step 6).
        Assert.True(records >= 118, $"only {records} records");
    }

    /// <summary>A value for each required field, so the rest can be built: zeros, empty strings, the first member, one element of a list, a filled blank record.</summary>
    static DraftValue Filler(ValueShape shape) => shape.Kind switch
    {
        ValueKind.Bool => DraftValue.Of(false),
        ValueKind.String => DraftValue.Of(""),
        ValueKind.Enum => DraftValue.Of(shape.Members[0].Value),
        ValueKind.Bytes => DraftValue.Of(Array.Empty<byte>()),
        ValueKind.Nullable => DraftValue.Null,
        ValueKind.List => new DraftList([Filler(shape.Element!)]),
        ValueKind.Map or ValueKind.Pairs => DraftMap.Empty,
        ValueKind.Record => Filled(shape.Record!),
        ValueKind.Union => Filled(shape.Record!.Subtypes[0]),
        _ => DraftValue.Of(Activator.CreateInstance(shape.Type)!),
    };

    static RecordDraft Filled(RecordShape shape)
    {
        var blank = RecordDraft.Blank(shape);
        foreach (var field in shape.Fields.Where(f => f.Required))
            blank = blank.With(field, Filler(field.Value));
        return blank;
    }

    /// <summary>Every engine record type a draft can be of: the plain ones and every union's subtypes.</summary>
    public static TheoryData<string> EngineKinds()
    {
        var data = new TheoryData<string>();
        foreach (var (name, type) in RecordTypes.Engine.ByName)
            if (!RecordShape.Of(type).IsUnion)
                data.Add(name);
        return data;
    }

    [Theory]
    [MemberData(nameof(EngineKinds))]
    public void ABlankDraftOfEveryEngineKindHoldsItsDefaults(string name)
    {
        var shape = RecordShape.Of(RecordTypes.Engine.Find(name)!);
        var blank = RecordDraft.Blank(shape);
        Assert.Same(shape, blank.Shape);
        // Required fields not given, every other one its default.
        Assert.Equal(shape.Fields.Select(f => f.Required), blank.Fields.Select(f => f == DraftValue.Missing));
        var required = shape.Fields.Where(f => f.Required).Select(f => $"needs a value (at $.{f.JsonName})").ToList();
        if (required.Count > 0)
            Assert.Equal(required, Problems(blank));
        else
        {
            // As the JSON form without keys reads.
            var json = System.Text.Encoding.UTF8.GetBytes($"{{\"$types\": {{\"{shape.Name}\": {shape.Version}}}}}");
            var expected = RecordSerializer.FromJson(shape.Type, json);
            Assert.Equal(RecordSerializer.ToBinary(shape.Type, expected), RecordSerializer.ToBinary(shape.Type, Build(blank)));
        }
        var built = Build(Filled(shape));
        foreach (var field in shape.Fields.Where(f => !f.Required))
            RecordAssert.Equal(field.Default, field.Get(built));
    }

    [Fact]
    public void ABlankDraftWithoutRequiredFieldsBuildsTheDefaults()
    {
        RecordAssert.Equal(new Square(), Build(RecordDraft.Blank(typeof(Square))));
        RecordAssert.Equal(new Empty(), Build(RecordDraft.Blank(typeof(Empty))));
        // With the required fields given, the defaults the declarations give.
        var kinds = (Kinds)Build(RecordDraft.Blank(typeof(Kinds)).With("text", DraftValue.Of("t")).With("part", RecordDraft.Blank(typeof(Part)).With("name", DraftValue.Of("p"))));
        RecordAssert.Equal(Minimal(), kinds);
        var point = (Point)Build(RecordDraft.Blank(typeof(Point)).With("x", DraftValue.Of(4f)));
        RecordAssert.Equal(new Point(4), point);
        // A union has no blank of its own: its subtypes do.
        Assert.Contains("is a union", Assert.Throws<ArgumentException>(() => RecordDraft.Blank(typeof(Shape))).Message);
        Assert.Equal(["needs a value (at $.radius)"], Problems(RecordDraft.Blank(typeof(Circle))));
    }

    [Fact]
    public void DraftsAreReadThroughTheirShapes()
    {
        var sample = RecordFormTests.Sample();
        var draft = RecordDraft.From(sample);
        Assert.Same(RecordShape.Of<Kinds>(), draft.Shape);
        Assert.Equal(sample.Text, Assert.IsType<DraftLeaf>(draft["text"]).Value);
        Assert.Equal("", Assert.IsType<DraftLeaf>(draft["Note"]).Value);
        Assert.Same(DraftValue.Null, draft["ratio"]);
        Assert.Same(DraftValue.Null, draft["maybeShape"]);
        Assert.Equal(Shade.Golden, Assert.IsType<DraftLeaf>(draft["shade"]).Value);
        Assert.Equal(Pace.Slow, Assert.IsType<DraftLeaf>(draft["pace"]).Value);
        Assert.Equal(sample.V3, Assert.IsType<DraftLeaf>(draft["v3"]).Value);
        Assert.Equal(["a", "", "β"], Assert.IsType<DraftList>(draft["words"]).Items.Select(i => ((DraftLeaf)i).Value));
        var gaps = Assert.IsType<DraftList>(draft["gaps"]);
        Assert.Same(DraftValue.Null, gaps[1]);
        var jagged = Assert.IsType<DraftList>(draft["jagged"]);
        Assert.Equal(2.0, ((DraftLeaf)((DraftList)jagged[0])[1]).Value);
        var scores = Assert.IsType<DraftMap>(draft["scores"]);
        Assert.Equal(["zeta", "alpha", "mid"], scores.Entries.Select(e => e.Key));
        Assert.Equal(2, ((DraftLeaf)scores[1].Value).Value);
        Assert.Equal(new object[] { Shade.Dark, Shade.Pale }, Assert.IsType<DraftMap>(draft["shadePairs"]).Entries.Select(e => e.Key));
        var part = Assert.IsType<RecordDraft>(draft["part"]);
        Assert.Equal("core", ((DraftLeaf)part["name"]).Value);
        var shape = Assert.IsType<RecordDraft>(draft["shape"]);
        Assert.Same(RecordShape.Of<Square>(), shape.Shape);
        Assert.Same(RecordShape.Of<Circle>(), ((RecordDraft)shape["inner"]).Shape);
        Assert.Equal((ushort)5, Assert.IsType<DraftLeaf>(((RecordDraft)draft["maybeJoint"])["b"]).Value);
        Assert.Empty(Assert.IsType<RecordDraft>(Assert.IsType<DraftList>(draft["empties"])[0]).Fields);

        // Bulk fields (and byte arrays) stay the original objects, in the draft and in the record built.
        Assert.Same(sample.Points, Assert.IsType<DraftLeaf>(draft["points"]).Value);
        Assert.Same(sample.Bytes, Assert.IsType<DraftLeaf>(draft["bytes"]).Value);
        Assert.Same(sample.MaybeBytes, Assert.IsType<DraftLeaf>(draft["maybeBytes"]).Value);
        Assert.Same(sample.Parts, Assert.IsType<DraftLeaf>(draft["parts"]).Value);
        Assert.Equal(sample.Rotation, Assert.IsType<DraftLeaf>(draft["rotation"]).Value);
        var built = (Kinds)Build(draft);
        Assert.Same(sample.Points, built.Points);
        Assert.Same(sample.Parts, built.Parts);
        Assert.Same(sample.Bytes, built.Bytes);
        // Other lists are new ones of their declared types.
        Assert.NotSame(sample.Names, built.Names);
        Assert.IsType<List<string>>(built.Names);
        Assert.IsType<OrderedDictionary<string, int>>(built.Ordered);
        Assert.IsType<Dictionary<string, int>>(built.Scores);
        RecordAssert.Equal(sample, built);

        // A bulk field's null.
        Assert.Same(DraftValue.Null, RecordDraft.From(new Kinds { Text = "", Part = new() { Name = "" }, Points = null! })["points"]);
    }

    [Fact]
    public void WhatCantBeBuiltIsReportedAtItsPath()
    {
        var minimal = RecordDraft.From(Minimal());
        // Required fields not given, nested too: in a record, a list's element, a map's value, a union's subtype.
        Assert.Equal(["needs a value (at $.text)", "needs a value (at $.part)"], Problems(RecordDraft.Blank(typeof(Kinds))));
        Assert.Equal(["needs a value (at $.part.name)"], Problems(minimal.With("part", RecordDraft.Blank(typeof(Part)))));
        var creature = RecordDraft.From(new Creature { Name = "A", Limbs = [new Limb { Kind = LimbKind.Arm }] });
        Assert.Equal(["needs a value (at $.limbs[1].kind)"], Problems(creature.With("limbs", ((DraftList)creature["limbs"]).Insert(1, RecordDraft.Blank(typeof(Limb))))));
        Assert.Equal(["needs a value (at $.shapes[0].radius)"], Problems(minimal.With("shapes", new DraftList([RecordDraft.Blank(typeof(Circle))]))));
        Assert.Equal(["needs a value (at $.shapesByName[\"sun\"].radius)", "needs a value (at $.shapesByName[\"moon\"].radius)"],
            Problems(minimal.With("shapesByName", new DraftMap([new("sun", RecordDraft.Blank(typeof(Circle))), new("moon", RecordDraft.Blank(typeof(Circle)))]))));
        // Not given where no field is: a list's element, a map's value.
        Assert.Equal(["needs a value (at $.words[0])"], Problems(minimal.With("words", new DraftList([DraftValue.Missing]))));
        Assert.Equal(["needs a value (at $.scores[\"a\"])"], Problems(minimal.With("scores", new DraftMap([new("a", DraftValue.Missing)]))));

        // A null where none is allowed: a field, a list's element, a map's value, a record, a union.
        const string NoNull = "null in a field that can't be null";
        Assert.Equal([$"{NoNull} (at $.text)"], Problems(minimal.With("text", DraftValue.Null)));
        Assert.Equal([$"{NoNull} (at $.flag)"], Problems(minimal.With("flag", DraftValue.Null)));
        Assert.Equal([$"{NoNull} (at $.words[1])"], Problems(minimal.With("words", new DraftList([DraftValue.Of("a"), DraftValue.Null]))));
        Assert.Equal([$"{NoNull} (at $.scores[\"x\"])"], Problems(minimal.With("scores", new DraftMap([new("x", DraftValue.Null)]))));
        Assert.Equal([$"{NoNull} (at $.part)"], Problems(minimal.With("part", DraftValue.Null)));
        Assert.Equal([$"{NoNull} (at $.shape)"], Problems(minimal.With("shape", DraftValue.Null)));
        Assert.Equal([$"{NoNull} (at $.ints)"], Problems(minimal.With("ints", DraftValue.Null)));
        Assert.Equal([$"{NoNull} (at $.points)"], Problems(minimal.With("points", DraftValue.Null)));
        // Where null is allowed it builds.
        var nulls = (Kinds)Build(minimal.With("note", DraftValue.Null).With("gaps", new DraftList([DraftValue.Null])).With("maybePart", DraftValue.Null)
            .With("maybeBytes", DraftValue.Null).With("byNumber", new DraftMap([new(1, DraftValue.Null)])).With("pace", DraftValue.Null));
        Assert.Null(nulls.Note);
        Assert.Equal([null], nulls.Gaps);
        Assert.Null(nulls.ByNumber[1]);
        Assert.Null(nulls.Pace);

        // A number that isn't a member, as a value and as a key.
        Assert.Equal(["3 isn't a member of Shade (at $.shade)"], Problems(minimal.With("shade", DraftValue.Of((Shade)3))));
        Assert.Equal(["7 isn't a member of Shade (at $.byShade)"], Problems(minimal.With("byShade", new DraftMap([new((Shade)7, DraftValue.Of(1.0))]))));
        Assert.Equal(["9 isn't a member of Pace (at $.pace)"], Problems(minimal.With("pace", DraftValue.Of((Pace)9))));
        // A key there twice, in a dictionary and in a list of pairs.
        Assert.Equal(["the key a is there twice (at $.scores)"], Problems(minimal.With("scores", new DraftMap([new("a", DraftValue.Of(1)), new("a", DraftValue.Of(2))]))));
        Assert.Equal(["the key Dark is there twice (at $.shadePairs)"], Problems(minimal.With("shadePairs", new DraftMap([new(Shade.Dark, DraftValue.Of(1)), new(Shade.Dark, DraftValue.Of(1))]))));
        // A lone surrogate, in a string and in a key.
        Assert.Equal(["the string isn't valid UTF-16 (a lone surrogate) (at $.text)"], Problems(minimal.With("text", DraftValue.Of("a\uD800"))));
        Assert.Equal(["the string isn't valid UTF-16 (a lone surrogate) (at $.scores)"], Problems(minimal.With("scores", new DraftMap([new("\uDC00", DraftValue.Of(1))]))));

        // A record isn't made when one of its fields failed: a curve whose key needs its time is that problem only,
        // as is a positional record's required number given as null.
        var keyless = new DraftList([RecordDraft.Blank(typeof(FCurveKey)).With("value", DraftValue.Of(1.0))]);
        Assert.Equal(["needs a value (at $.keys[0].time)"], Problems(RecordDraft.From(new FCurve([FCurveKey.Linear(0, 0)])).With("keys", keyless)));
        Assert.Equal([$"{NoNull} (at $.x)"], Problems(RecordDraft.From(new Point(1)).With("x", DraftValue.Null)));

        // Every problem at once.
        Assert.Equal(["needs a value (at $.text)", $"{NoNull} (at $.words[0])", "needs a value (at $.part.name)"],
            Problems(RecordDraft.Blank(typeof(Kinds)).With("words", new DraftList([DraftValue.Null])).With("part", RecordDraft.Blank(typeof(Part)))));

        // A field not given that isn't required means its default.
        var defaults = (Kinds)Build(minimal.With("ratio", DraftValue.Missing).With("huge", DraftValue.Missing).With("motto", DraftValue.Missing));
        Assert.Equal((0.5, Huge.Small, "none"), (defaults.Ratio, defaults.Huge, defaults.Motto));
        Assert.Equal(2, ((Point)Build(RecordDraft.From(new Point(1, 5)).With("y", DraftValue.Missing))).Y);
    }

    [Fact]
    public void ACurveRefusingItsKeysIsReportedAtTheCurve()
    {
        var keys = new DraftList([RecordDraft.From(FCurveKey.Linear(1, 0)), RecordDraft.From(FCurveKey.Linear(0.5, 1))]);
        var curve = RecordDraft.From(new FCurve([FCurveKey.Linear(0, 0)])).With("keys", keys);
        var refusal = Assert.Throws<ArgumentException>(() => new FCurve([FCurveKey.Linear(1, 0), FCurveKey.Linear(0.5, 1)])).Message;
        Assert.Equal([refusal], Problems(curve));
        var track = RecordDraft.Blank(typeof(TransformTrack)).With("node", DraftValue.Of("hero")).With("locationX", curve);
        var sequence = RecordDraft.Blank(typeof(Sequence)).With("name", DraftValue.Of("intro")).With("duration", DraftValue.Of(2.0))
            .With("tracks", new DraftList([track]));
        Assert.Equal([$"{refusal} (at $.tracks[0].locationX)"], Problems(sequence));
        // No keys: the curve's own rule too.
        Assert.Equal(["an F-curve needs at least one key (at $.tracks[0].locationX)"],
            Problems(sequence.With(RecordPath.Root.Field("tracks").Index(0).Field("locationX").Field("keys"), DraftList.Empty)));
        // Put right, it builds.
        var built = (Sequence)Build(sequence.With(RecordPath.Root.Field("tracks").Index(0).Field("locationX").Field("keys"), keys.Move(1, 0)));
        Assert.Equal(0.5, ((TransformTrack)built.Tracks[0]).LocationX!.Evaluate(0.75), 12);
    }

    [Fact]
    public void ANodeOfTheWrongKindIsTheCallersMistake()
    {
        var minimal = RecordDraft.From(Minimal());
        void Wrong(string field, DraftValue value) => Assert.Throws<InvalidOperationException>(() => minimal.With(field, value).Build());
        Wrong("text", DraftValue.Of(1));
        Wrong("float64", DraftValue.Of(1f));
        Wrong("count", DraftValue.Of(1L));
        Wrong("words", DraftValue.Of("a"));
        Wrong("words", new DraftList([DraftValue.Of(1)]));
        Wrong("scores", new DraftList([]));
        Wrong("scores", new DraftMap([new(1, DraftValue.Of(1))]));
        Wrong("text", new DraftList([]));
        Wrong("part", RecordDraft.From(new Point(1)));
        Wrong("part", DraftValue.Of(new Part { Name = "p" }));
        Wrong("shape", RecordDraft.From(new Part { Name = "p" }));
        Wrong("shapes", new DraftList([RecordDraft.From(new Walk())]));
        Wrong("points", DraftValue.Of(new Vector2[1]));
        Wrong("points", new DraftList([]));
        Assert.Contains("(at $.part.name)", Assert.Throws<InvalidOperationException>(() => minimal.With("part", RecordDraft.Blank(typeof(Part)).With("name", DraftValue.Of(1))).Build()).Message);
    }

    [Fact]
    public void DraftsChangeByFieldAndByPath()
    {
        var sample = RecordDraft.From(RecordFormTests.Sample());
        var path = RecordPath.Root;
        Assert.Same(sample, sample.At(path));
        Assert.Equal("core", ((DraftLeaf)sample.At(path.Field("part").Field("name"))).Value);
        Assert.Equal("", ((DraftLeaf)sample.At(path.Field("words").Index(1))).Value);
        Assert.Equal(2, ((DraftLeaf)sample.At(path.Field("scores").Key("alpha"))).Value);
        Assert.Equal("ten", ((DraftLeaf)sample.At(path.Field("byNumber").Key("10"))).Value);
        Assert.Equal(1.0, ((DraftLeaf)sample.At(path.Field("byShade").Key("golden"))).Value);
        Assert.Equal(2, ((DraftLeaf)sample.At(path.Field("shadePairs").Key("pale"))).Value);
        Assert.Equal(0.5, ((DraftLeaf)sample.At(path.Field("shape").Field("inner").Field("radius"))).Value);
        Assert.Equal(3.0, ((DraftLeaf)sample.At(path.Field("shapes").Index(1).Field("radius"))).Value);
        Assert.Equal("seven", ((DraftLeaf)sample.At(path.Field("sparse").Key("7").Field("name"))).Value);
        Assert.Equal((ushort)5, ((DraftLeaf)sample.At(path.Field("maybeJoint").Field("b"))).Value);
        Assert.Equal(double.NaN, ((DraftLeaf)sample.At(path.Field("jagged").Index(2).Index(0))).Value);
        Assert.Equal("here", ((DraftLeaf)sample.At(path.Field("Point").Field("Label"))).Value);
        // Into a nullable list or map, as into one that isn't.
        Assert.Equal(1, ((DraftLeaf)sample.At(path.Field("ordered").Key("a"))).Value);
        Assert.Equal(1, ((DraftLeaf)RecordDraft.From(new Kinds { Text = "", Part = new() { Name = "" }, MaybeInts = [0, 1] }).At(path.Field("maybeInts").Index(1))).Value);
        foreach (var nowhere in new[]
        {
            path.Field("nothing"), path.Field("words").Index(3), path.Field("scores").Key("beta"), path.Field("text").Index(0),
            path.Field("part").Index(0), path.Field("words").Key("a"), path.Field("points").Index(0), path.Field("maybeShape").Field("tint"),
        })
            Assert.Contains($"no draft at {nowhere}", Assert.Throws<ArgumentException>(() => sample.At(nowhere)).Message);

        // A change makes a new draft; the old one keeps what it had.
        var changed = sample.With(path.Field("shapes").Index(1).Field("radius"), DraftValue.Of(9.0))
            .With(path.Field("scores").Key("mid"), DraftValue.Of(30))
            .With(path.Field("byShade").Key("pale"), DraftValue.Of(0.25))
            .With(path.Field("jagged").Index(0).Index(1), DraftValue.Of(-2.0))
            .With(path.Field("sparse").Key("7").Field("weight"), DraftValue.Of(7.5))
            .With(path.Field("maybeJoint").Field("a"), DraftValue.Of((ushort)40))
            .With(path.Field("Motto"), DraftValue.Of("changed"));
        Assert.Equal(3.0, ((DraftLeaf)sample.At(path.Field("shapes").Index(1).Field("radius"))).Value);
        var built = (Kinds)Build(changed);
        Assert.Equal(9, ((Circle)built.Shapes[1]).Radius);
        Assert.Equal(["zeta", "alpha", "mid"], built.Scores.Keys);
        Assert.Equal(30, built.Scores["mid"]);
        Assert.Equal(0.25, built.ByShade[Shade.Pale]);
        Assert.Equal([1.0, -2.0], built.Jagged[0]);
        Assert.Equal(7.5, built.Sparse[7]!.Weight);
        Assert.Equal(new Joint(40, 5), built.MaybeJoint);
        Assert.Equal("changed", built.Motto);
        // Only the path's records and lists are new; the rest is shared.
        Assert.Same(sample["part"], changed["part"]);
        Assert.NotSame(sample["shapes"], changed["shapes"]);
        Assert.Same(((DraftList)sample["shapes"])[0], ((DraftList)changed["shapes"])[0]);
        // Replacing the whole record at the root.
        var other = RecordDraft.From(Minimal());
        Assert.Same(other, sample.With(path, other));

        // By field: a JSON name, a code name or the field's shape; a field of another kind is refused.
        var shape = RecordShape.Of<Kinds>();
        Assert.Equal("x", ((DraftLeaf)sample.With("Motto", DraftValue.Of("x"))[shape.Field("motto")!]).Value);
        Assert.Equal("y", ((DraftLeaf)sample.With(shape.Field("motto")!, DraftValue.Of("y"))["motto"]).Value);
        Assert.Throws<ArgumentException>(() => sample.With("nothing", DraftValue.Null));
        Assert.Throws<ArgumentException>(() => sample[RecordShape.Of<Part>().Field("name")!]);
        Assert.Throws<ArgumentException>(() => sample.With(RecordShape.Of<Part>().Field("name")!, DraftValue.Null));
        Assert.Throws<ArgumentException>(() => sample.With("motto", null!));
        Assert.Throws<ArgumentNullException>(() => sample.With(path.Field("motto"), null!));
        Assert.Throws<ArgumentNullException>(() => sample.With(path, null!));
    }

    [Fact]
    public void PathsGoThroughListsAndMapsOfMaps()
    {
        var draft = RecordDraft.From(new NestedMaps
        {
            Tables = [new() { [Shade.Pale] = 1 }, new() { [Shade.Dark] = 2, [Shade.Golden] = 3 }],
            ByName = new() { ["a"] = new() { [Rank.Second] = "two" } },
        });
        var root = RecordPath.Root;
        Assert.Equal(3, ((DraftLeaf)draft.At(root.Field("tables").Index(1).Key("golden"))).Value);
        Assert.Equal("two", ((DraftLeaf)draft.At(root.Field("byName").Key("a").Key("second"))).Value);
        var built = (NestedMaps)Build(draft.With(root.Field("tables").Index(1).Key("dark"), DraftValue.Of(20))
            .With(root.Field("byName").Key("a").Key("second"), DraftValue.Of("deux")));
        Assert.Equal(20, built.Tables[1][Shade.Dark]);
        Assert.Equal("deux", built.ByName["a"][Rank.Second]);
    }

    [Fact]
    public void ListsAndMapsChangeByIndex()
    {
        DraftValue A = DraftValue.Of("a"), B = DraftValue.Of("b"), C = DraftValue.Of("c");
        var list = new DraftList([A, B, C]);
        Assert.Equal([A, C, B], list.Move(1, 2).Items);
        Assert.Equal([C, A, B], list.Move(2, 0).Items);
        Assert.Equal([A, B, C], list.Move(1, 1).Items);
        Assert.Equal([A, C], list.RemoveAt(1).Items);
        Assert.Equal([B, C], list.RemoveAt(0).Items);
        Assert.Equal([A, B], list.RemoveAt(2).Items);
        Assert.Equal([A, C, B, C], list.Insert(1, C).Items);
        Assert.Equal([A, B, C, A], list.Insert(3, A).Items);
        Assert.Equal([C, A, B, C], list.Insert(0, C).Items);
        Assert.Equal([A, A, C], list.With(1, A).Items);
        Assert.Equal([A, B, C], list.Items);
        Assert.Equal(3, list.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(4, A));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, A));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(3));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
        Assert.Throws<ArgumentException>(() => list.Insert(0, null!));
        Assert.Throws<ArgumentException>(() => list.With(0, null!));
        Assert.Throws<ArgumentException>(() => new DraftList([A, null!]));
        Assert.Empty(DraftList.Empty.Items);

        DraftEntry X = new("x", A), Y = new("y", B), Z = new("z", C);
        var map = new DraftMap([X, Y, Z]);
        Assert.Equal([X, Z, Y], map.Move(1, 2).Entries);
        Assert.Equal([Z, X, Y], map.Move(2, 0).Entries);
        Assert.Equal([X, Z], map.RemoveAt(1).Entries);
        Assert.Equal([Y, Z], map.RemoveAt(0).Entries);
        Assert.Equal([X, Y], map.RemoveAt(2).Entries);
        Assert.Equal([X, Z, Y, Z], map.Insert(1, Z).Entries);
        Assert.Equal([X, Y, Z, X], map.Insert(3, X).Entries);
        Assert.Equal([X, X, Z], map.With(1, X).Entries);
        Assert.Equal([X, Y, Z], map.Entries);
        Assert.Equal((3, Y), (map.Count, map[1]));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.Insert(4, X));
        Assert.Throws<ArgumentOutOfRangeException>(() => map.RemoveAt(3));
        Assert.Throws<ArgumentException>(() => new DraftMap([new(null!, A)]));
        Assert.Throws<ArgumentException>(() => new DraftMap([new("k", null!)]));
        Assert.Empty(DraftMap.Empty.Entries);

        // A record draft's fields: one per field, none null; no draft of a union itself.
        Assert.Throws<ArgumentException>(() => new RecordDraft(RecordShape.Of<Part>(), [A]));
        Assert.Throws<ArgumentException>(() => new RecordDraft(RecordShape.Of<Part>(), [A, null!, DraftValue.Missing]));
        Assert.Throws<ArgumentException>(() => new RecordDraft(RecordShape.Of<Shape>(), []));
        Assert.Throws<ArgumentNullException>(() => DraftValue.Of(null!));
        Assert.Equal("test.part", new RecordDraft(RecordShape.Of<Part>(), [A, DraftValue.Missing, DraftValue.Null]).ToString());
        Assert.Equal(("missing", "null", "a"), (DraftValue.Missing.ToString(), DraftValue.Null.ToString(), A.ToString()));
    }
}
