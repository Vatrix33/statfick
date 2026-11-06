using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Packs;
using Statventure.Tests.Records.Variants;

namespace Statventure.Tests.Records;

/// <summary>
/// The checks of a record (Docs/Editor.md, "Records: shapes, drafts and marks", step 1; P20): the
/// marks' generic checks (limits, choices, references given a lookup), each record type's own checks
/// wherever its values are held, with their paths, and checks that aren't declared right refused.
/// </summary>
public class RecordCheckTests
{
    /// <summary>A lookup over a fixed set of entries: path → record kind.</summary>
    sealed class Entries(Dictionary<string, string> kinds) : IReferenceLookup
    {
        public List<string> Asked { get; } = [];

        public string? KindAt(string path)
        {
            Asked.Add(path);
            return kinds.GetValueOrDefault(path);
        }
    }

    static IReadOnlyList<string> Check(object record, VirtualFileSystem? data = null, IReferenceLookup? references = null) =>
        [.. RecordChecks.Check(record, data, references).Select(p => p.ToString())];

    static Kinds Minimal() => new() { Text = "t", Part = new() { Name = "p" }, UInt8 = 1 };

    [Fact]
    public void NumbersAreWithinTheirLimits()
    {
        Assert.Empty(Check(new Creature { Name = "A", Speed = 0 }));
        Assert.Empty(Check(new Creature { Name = "A", Speed = 10 }));
        Assert.Equal(["must be 0 or more and at most 10 (it is 10.5) (at $.speed)"], Check(new Creature { Name = "A", Speed = 10.5 }));
        Assert.Equal(["must be 0 or more and at most 10 (it is -0.1) (at $.speed)"], Check(new Creature { Name = "A", Speed = -0.1 }));
        Assert.Equal(["must be 0 or more and at most 10 (it is NaN) (at $.speed)"], Check(new Creature { Name = "A", Speed = double.NaN }));
        // An exclusive end; an infinite one.
        Assert.Equal(["must be above 0 (it is 0) (at $.part.weight)"], Check(new Kinds { Text = "t", Part = new() { Name = "p", Weight = 0 }, UInt8 = 1 }));
        Assert.Empty(Check(new Kinds { Text = "t", Part = new() { Name = "p", Weight = double.PositiveInfinity }, UInt8 = 1 }));
        // A float as its own shortest form.
        Assert.Equal(["must be -1 or more and at most 1 (it is 1.1) (at $.float32)"], Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 1, Float32 = 1.1f }));
        // Integers; an exclusive maximum.
        Assert.Empty(Check(Minimal()));
        Assert.Equal(["must be 1 or more and below 200 (it is 200) (at $.uInt8)"], Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 200 }));
        Assert.Equal(["must be 1 or more and below 200 (it is 0) (at $.uInt8)"], Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 0 }));
        Assert.Empty(Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 199 }));
        // A nullable's value (null has none), a list's elements, a map's values, nested lists.
        Assert.Empty(Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 1, Ratio = null }));
        Assert.Equal(["must be 0 or more and at most 1 (it is 1.5) (at $.ratio)"], Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 1, Ratio = 1.5 }));
        Assert.Equal(["must be -5 or more and at most 5 (it is 6) (at $.ints[1])", "must be -5 or more and at most 5 (it is -9) (at $.ints[3])"],
            Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 1, Ints = [5, 6, -5, -9] }));
        Assert.Equal(["must be above -1 and at most 1 (it is -1) (at $.byShade[\"golden\"])"],
            Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 1, ByShade = new() { [Shade.Pale] = 1, [Shade.Golden] = -1 } }));
        Assert.Equal(["must be 0 or more and at most 9 (it is 10) (at $.maybeInts[1])"],
            Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 1, MaybeInts = [9, 10] }));
        Assert.Equal(["must be -10 or more and at most 10 (it is 11) (at $.jagged[1][0])"],
            Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 1, Jagged = [[10, -10], [11]] }));
        // A key that isn't a member (only a record made in code can hold one) by its number.
        Assert.Equal(["must be above -1 and at most 1 (it is 5) (at $.byShade[\"7\"])"],
            Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 1, ByShade = new() { [(Shade)7] = 5 } }));
    }

    [Fact]
    public void LimitsAreDescribedAndTested()
    {
        Assert.Equal("0 or more and at most 1", new FieldLimits(0, 1, false, false).Describe());
        Assert.Equal("above 0 and below 1", new FieldLimits(0, 1, true, true).Describe());
        Assert.Equal("above 0", new FieldLimits(0, double.PositiveInfinity, true, false).Describe());
        Assert.Equal("at most 0.5", new FieldLimits(double.NegativeInfinity, 0.5, false, false).Describe());
        Assert.Equal("a number", new FieldLimits(double.NegativeInfinity, double.PositiveInfinity, false, false).Describe());
        Assert.Equal("above -Infinity and below Infinity", new FieldLimits(double.NegativeInfinity, double.PositiveInfinity, true, true).Describe());
        var limits = new FieldLimits(0, 1, false, true);
        Assert.Equal([false, true, true, false, false], new[] { -0.1, 0, 0.5, 1, double.NaN }.Select(limits.Contains));
        Assert.Equal([false, true, false], new[] { 0, 0.5, 1.0 }.Select(new FieldLimits(0, 1, true, true).Contains));
        Assert.True(new FieldLimits(double.NegativeInfinity, double.PositiveInfinity, false, false).Contains(double.PositiveInfinity));
        Assert.False(new FieldLimits(double.NegativeInfinity, double.PositiveInfinity, false, false).Contains(double.NaN));
    }

    [Fact]
    public void StringsAreAmongTheirChoices()
    {
        Assert.Empty(Check(new Creature { Name = "A", Title = "the Grim" }));
        Assert.Empty(Check(new Creature { Name = "A", Title = null }));
        Assert.Equal(["\"the grim\" isn't one of the choices: the Grim, the Bold (at $.title)"], Check(new Creature { Name = "A", Title = "the grim" }));
        Assert.Equal(["\"b\" isn't one of the choices: a, , β (at $.words[1])"], Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 1, Words = ["a", "b", "", "β"] }));
        Assert.Equal(["\"y\" isn't one of the choices: x,  (at $.gaps[2])"], Check(new Kinds { Text = "t", Part = new() { Name = "p" }, UInt8 = 1, Gaps = ["x", null, "y", ""] }));
    }

    [Fact]
    public void NamesPointAtEntriesOfTheirKind()
    {
        var entries = new Entries(new()
        {
            ["creatures/_ogre"] = "test.creature", ["creatures/imp"] = "test.part", ["parts/kick"] = "test.part", ["parts/snare"] = "test.creature",
            ["kinds/sets/a/b"] = "test.kinds", ["spare-parts/gear"] = "test.part",
        });
        var kinds = new Kinds
        {
            Text = "t", Part = new() { Name = "kick" }, UInt8 = 1,
            Names = ["Ogre", "Troll", "imp", "an imp"],
            Pairs = [new("kick", "x"), new("snare", "x"), new("hat", "x")],
            ByNumber = new() { [1] = "kinds/sets/a/b", [2] = null, [3] = "kinds/a", [4] = "kinds/sets/c" },
            Ordered = new() { ["kick"] = 1, ["tom"] = 2 },
        };
        Assert.Equal(
        [
            "no creature \"Troll\" (creatures/_troll) (at $.names[1])",
            "creatures/imp isn't a creature: it holds a test.part record (at $.names[2])",
            "\"an imp\" can't name a creature in creatures (at $.names[3])",
            "parts/snare isn't a body part: it holds a test.creature record (at $.pairs[\"snare\"])",
            "no body part \"hat\" (parts/hat) (at $.pairs[\"hat\"])",
            "\"kinds/a\" can't name a kinds in kinds/sets (at $.byNumber[\"3\"])",
            "no kinds \"kinds/sets/c\" (kinds/sets/c) (at $.byNumber[\"4\"])",
            "no body part \"tom\" (parts/tom) (at $.ordered[\"tom\"])",
        ], Check(kinds, references: entries));
        // Without a lookup, references aren't checked.
        Assert.Empty(Check(kinds));

        // A condition on a string: a creature for "creature", a spare part for "part" and "piece", nothing else.
        Kinds Text(string text, string motto) => new() { Text = text, Part = new() { Name = "kick" }, UInt8 = 1, Motto = motto };
        Assert.Empty(Check(Text("Ogre", "creature"), references: entries));
        Assert.Equal(["no creature \"Imp\" (creatures/_imp) (at $.text)"], Check(Text("Imp", "creature"), references: entries));
        Assert.Empty(Check(Text("gear", "part"), references: entries));
        Assert.Equal(["no body part \"cog\" (spare-parts/cog) (at $.text)"], Check(Text("cog", "piece"), references: entries));
        Assert.Empty(Check(Text("anything at all", "none"), references: entries));
        // On an enum: a creature when dark or golden, a body part otherwise.
        Part Part(string name, Shade shade) => new() { Name = name, Shade = shade };
        Assert.Empty(Check(Part("Ogre", Shade.Dark), references: entries));
        Assert.Empty(Check(Part("Ogre", Shade.Golden), references: entries));
        Assert.Equal(["\"Ogre\" can't name a body part in parts (at $.name)"], Check(Part("Ogre", Shade.Pale), references: entries));
        Assert.Empty(Check(Part("kick", Shade.Pale), references: entries));
        // Each name asked of the lookup by its path.
        Assert.Equal(["parts/kick"], entries.Asked[^1..]);
    }

    [Fact]
    public void EachRecordGetsItsTypesOwnChecks()
    {
        var creature = new Creature
        {
            Name = "Ogre",
            Limbs = [.. Enumerable.Repeat(new Limb { Kind = LimbKind.Leg }, 8), new Limb { Kind = LimbKind.Tail, Length = 3.5 }],
            Moves = [new Walk { Cue = "step" }, new Leap { Height = 11 }, new Fly { Cue = "soar", Altitude = 1 }],
        };
        // Its own (in the order of their names), then its fields': a nested record's at its place, a
        // union's on its subtype's values before the subtype's own.
        Assert.Equal(
        [
            "has more than eight limbs (at $.limbs)",
            "a tail is at most 3 m long (at $.limbs[8].length)",
            "needs a cue (at $.moves[1].cue)",
            "leaps too high (at $.moves[1])",
        ], Check(creature));
        Assert.Empty(Check(creature with { Limbs = [new Limb { Kind = LimbKind.Tail, Length = 3 }], Moves = [new Leap { Cue = "jump", Height = 10 }] }));
        // A union's value checked alone, and a nested type alone.
        Assert.Equal(["needs a cue (at $.cue)", "leaps too high"], Check(new Leap { Height = 12 }));
        Assert.Equal(["a tail is at most 3 m long (at $.length)"], Check(new Limb { Kind = LimbKind.Tail, Length = 4 }));
        // Checks come before the fields' marks.
        Assert.Equal(["has more than eight limbs (at $.limbs)", "must be 0 or more and at most 10 (it is 11) (at $.speed)"],
            Check(creature with { Limbs = [.. Enumerable.Repeat(new Limb { Kind = LimbKind.Arm }, 9)], Moves = [], Speed = 11 }));
    }

    [Fact]
    public void ChecksAcrossEntriesAreGivenTheData()
    {
        using var folder = new TempFolder();
        folder.Write("roster/ogre", "x");
        using var data = new VirtualFileSystem([Mount.Folder(folder.Root)]);
        Assert.Empty(Check(new Creature { Name = "Ogre" }, data));
        Assert.Equal(["isn't on the roster (at $.name)"], Check(new Creature { Name = "Imp" }, data));
        Assert.Equal(["isn't on the roster (at $.name)"], Check(new Creature { Name = "Big Imp" }, data));
        // A record's own checks in the order of their names.
        Assert.Equal(["has more than eight limbs (at $.limbs)", "isn't on the roster (at $.name)"],
            Check(new Creature { Name = "Imp", Limbs = [.. Enumerable.Repeat(new Limb { Kind = LimbKind.Arm }, 9)] }, data));
        // Without the data, those checks don't run.
        Assert.Empty(Check(new Creature { Name = "Imp" }));
    }

    [Fact]
    public void KindsAreNamedInSentences()
    {
        Assert.Equal("sound effect", RecordChecks.Words("Sound effect"));
        Assert.Equal("HDR factor", RecordChecks.Words("HDR factor"));
        Assert.Equal("X", RecordChecks.Words("X"));
        Assert.Equal("x", RecordChecks.Words("x"));
        Assert.Equal(["an", "an", "an", "an", "an", "a", "a"], new[] { "effect", "imp", "omen", "Union", "army", "sound", "creature" }.Select(RecordChecks.Article));
    }

    [Fact]
    public void TheContextReportsWhereTheValueIs()
    {
        var problems = new List<RecordProblem>();
        var entries = new Entries([]);
        var context = new CheckContext(null, entries, problems);
        Assert.Null(context.Data);
        Assert.Same(entries, context.References);
        context.At = RecordPath.Root.Field("moves").Index(2);
        context.Report("whole");
        context.Report(RecordPath.Root.Field("cue"), "part");
        Assert.Equal(["whole (at $.moves[2])", "part (at $.moves[2].cue)"], problems.Select(p => p.ToString()));
        Assert.Equal(RecordPath.Root.Field("moves").Index(2), problems[0].Path);
    }

    [Fact]
    public void ChecksThatArentDeclaredRightAreRefused()
    {
        string Refused(Type type) => Assert.Throws<InvalidOperationException>(() => RecordShape.Of(type).Checks).Message;
        foreach (var (type, method) in new[]
        {
            (typeof(CheckNotStatic), "Wrong"), (typeof(CheckOtherType), "Wrong"), (typeof(CheckNoContext), "Wrong"), (typeof(CheckReturns), "Wrong"),
            (typeof(CheckOtherContext), "Wrong"), (typeof(CheckGeneric), "Wrong"),
        })
            Assert.Equal($"{type.Name}.{method}: a check is a static void method taking the record and a CheckContext", Refused(type));
        Assert.Equal(2, RecordShape.Of<Creature>().Checks.Count);
        Assert.Empty(RecordShape.Of<Kinds>().Checks);
        Assert.Equal([RecordShape.Of<Move>(), RecordShape.Of<Leap>()], RecordShape.Of<Leap>().CheckedBy);
        Assert.Equal([RecordShape.Of<Creature>()], RecordShape.Of<Creature>().CheckedBy);
    }

    [Fact]
    public void PathsAreWrittenAsTheSerializerWritesThem()
    {
        var root = RecordPath.Root;
        Assert.Equal("$", root.ToString());
        Assert.True(root.IsRoot);
        Assert.Equal(0, root.Depth);
        var path = root.Field("Layers").Index(0).Field("maxVoices").Key("kick \"x\"").Key("é");
        Assert.Equal("$.layers[0].maxVoices[\"kick \\u0022x\\u0022\"][\"\\u00E9\"]", path.ToString());
        Assert.False(path.IsRoot);
        Assert.Equal(5, path.Depth);
        Assert.Equal(
        [
            new RecordPathSegment(RecordPathKind.Field, "layers", 0), new(RecordPathKind.Index, null, 0), new(RecordPathKind.Field, "maxVoices", 0),
            new(RecordPathKind.Key, "kick \"x\"", 0), new(RecordPathKind.Key, "é", 0),
        ], path.Segments);
        Assert.Equal(root.Field("a").Index(1).Field("b"), root.Field("a").Append(root.Index(1).Field("b")));
        Assert.Same(root, root.Append(root));
        Assert.True(root.Field("a") == root.Field("A"));
        Assert.True(root.Field("a") != root.Key("a"));
        Assert.False(root.Field("a").Equals(root.Field("a").Index(0)));
        Assert.False(root.Equals(null));
        Assert.Equal(root.Field("a").GetHashCode(), root.Field("a").GetHashCode());
        Assert.Throws<ArgumentOutOfRangeException>(() => root.Index(-1));
        // As the serializer writes a key: a damaged map value's path.
        var error = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Kinds>("""{"$types": {"test.kinds": 1, "test.part": 1}, "text": "t", "part": {"name": "p"}, "scores": {"k\"é": "x"}}"""u8));
        Assert.Equal(error.Path, root.Field("scores").Key("k\"é").ToString());
        Assert.Equal("m (at $.a)", new RecordProblem(root.Field("a"), "m").ToString());
        Assert.Equal("m", new RecordProblem(root, "m").ToString());
    }
}
