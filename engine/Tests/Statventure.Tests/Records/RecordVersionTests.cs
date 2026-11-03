using System.Reflection;
using System.Text;
using Statventure.Records;
using Statventure.Tests.Records.Variants;

namespace Statventure.Tests.Records;

/// <summary>
/// Versions (Docs/Assets.md, "Versions"): the committed fixtures of the test family's past versions
/// (written when they were current, here through downgrades) read and upgraded step by step to the
/// expected current values, in both forms; a union reads the tag list of its version; declarations
/// that break the rules are refused.
/// </summary>
public class RecordVersionTests
{
    static Creature Sample(bool fly = true) => new()
    {
        Name = "Ogre",
        Speed = 1.5,
        Title = "the Grim",
        Limbs =
        [
            new() { Kind = LimbKind.Arm, Length = 0.75 }, new() { Kind = LimbKind.Leg, Length = 1.25 },
            new() { Kind = LimbKind.Leg, Length = 1.25 }, new() { Kind = LimbKind.Tail, Length = 2.1 },
        ],
        Moves = fly
            ? [new Walk { Cue = "step", Pace = 0.8 }, new Leap { Cue = "jump", Height = 3.3, Spin = true }, new Fly { Cue = "soar", Altitude = 12.5 }]
            : [new Walk { Cue = "step", Pace = 0.8 }, new Leap { Cue = "jump", Height = 3.3, Spin = true }],
    };

    /// <summary>What the frozen copies of the past versions held of a current value (the inverse of the upgrades, for writing the fixtures).</summary>
    static object Downgrade(object value, int version) => (value, version) switch
    {
        (Creature c, 1) => new Creature.V1 { Name = c.Name, Legs = c.Limbs.Count(l => l.Kind == LimbKind.Leg), Limbs = [.. c.Limbs.Where(l => l.Kind != LimbKind.Leg)] },
        (Creature c, 2) => new Creature.V2 { Name = c.Name, Limbs = c.Limbs, Speed = c.Speed * 2 },
        (Limb l, 1) => new Limb.V1 { Kind = l.Kind.ToString().ToLowerInvariant(), Length = (float)l.Length },
        (Leap l, 1) => new Leap.V1 { Cue = l.Cue, Height = (float)l.Height },
        _ => throw new ArgumentException($"no version {version} of a {value.GetType().Name}"),
    };

    /// <summary>The fixtures: the versions each was written in, the value it was written from, and what it reads as now.</summary>
    static readonly Dictionary<string, (Dictionary<string, int> Versions, Creature Written, Creature Expected)> Fixtures = new()
    {
        ["creature-1"] = (new() { ["test.creature"] = 1, ["test.limb"] = 1 }, Sample(), new Creature
        {
            // Legs came back as limbs of the default length; the speed is version 2's default, halved.
            Name = "Ogre",
            Limbs = [new() { Kind = LimbKind.Arm, Length = 0.75 }, new() { Kind = LimbKind.Tail, Length = 2.1f }, new() { Kind = LimbKind.Leg }, new() { Kind = LimbKind.Leg }],
            Speed = 0.5,
        }),
        ["creature-2"] = (new() { ["test.creature"] = 2, ["test.limb"] = 1 }, Sample(), new Creature
        {
            Name = "Ogre",
            Limbs =
            [
                new() { Kind = LimbKind.Arm, Length = 0.75 }, new() { Kind = LimbKind.Leg, Length = 1.25 },
                new() { Kind = LimbKind.Leg, Length = 1.25 }, new() { Kind = LimbKind.Tail, Length = 2.1f },
            ],
            Speed = 1.5,
        }),
        ["creature-2-limb-2"] = (new() { ["test.creature"] = 2 }, Sample(), Sample() with { Title = null, Moves = [] }),
        ["creature-3-move-1"] = (new() { ["test.move"] = 1, ["test.leap"] = 1 }, Sample(fly: false),
            Sample(fly: false) with { Moves = [new Walk { Cue = "step", Pace = 0.8 }, new Leap { Cue = "jump", Height = 3.3f }] }),
        ["creature-3"] = (new(), Sample(), Sample()),
    };

    public static TheoryData<string, string> FixtureForms()
    {
        var data = new TheoryData<string, string>();
        foreach (var label in Fixtures.Keys)
        {
            data.Add(label, ".rec");
            data.Add(label, ".json");
        }
        return data;
    }

    [Fact]
    public void TheFixturesAreRecorded()
    {
        foreach (var (label, (versions, written, _)) in Fixtures)
            RecordFixtures.Ensure(label, written, versions, Downgrade);
    }

    [Theory]
    [MemberData(nameof(FixtureForms))]
    public void EachFixtureReadsAsItsExpectedCurrentValue(string label, string form)
    {
        var (versions, _, expected) = Fixtures[label];
        var (value, table) = RecordFixtures.Load("test.creature", label, form);
        RecordAssert.Equal(expected, value);
        // The fixture holds the versions it should (else it tests no upgrade).
        foreach (var (name, version, _) in table)
            Assert.Equal(versions.GetValueOrDefault(name, RecordFixtures.Types[name].GetCustomAttribute<RecordAttribute>()!.Version), version);
    }

    [Fact]
    public void AFailedUpgradeIsRefused()
    {
        var error = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Creature>(
            """{"$types": {"test.creature": 2, "test.limb": 1}, "name": "A", "limbs": [{"kind": "arm"}, {"kind": "fin"}]}"""u8));
        Assert.Equal("the upgrade of test.limb from version 1 failed: no limb kind fin (at $.limbs[1])", error.Message);
    }

    [Fact]
    public void AUnionReadsTheTagListOfItsVersion()
    {
        var current = RecordSerializer.ToBinary(new Creature { Name = "A", Moves = [new Fly { Altitude = 1 }] });
        Assert.Equal([1, 3, 0], current[^11..^8]);
        var old = RecordSerializer.WriteBinary(typeof(Creature), new Creature { Name = "A", Moves = [new Walk()] },
            new RecordWriteOptions { Versions = new Dictionary<string, int> { ["test.move"] = 1 }, Downgrade = Downgrade });
        // Version 1 of test.move has no tag 3.
        var flyAt = old.Length - 10;
        var error = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromBinary<Creature>([.. old[..flyAt], 3, .. old[(flyAt + 1)..]]));
        Assert.Equal($"the tag 3 at byte {flyAt} isn't a subtype of test.move version 1 (at $.moves[0])", error.Message);
        error = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Creature>(
            """{"$types": {"test.creature": 3, "test.move": 1, "test.fly": 1}, "name": "A", "moves": [{"$type": "test.fly", "altitude": 1}]}"""u8));
        Assert.Equal("\"test.fly\" isn't a subtype of test.move version 1 (at $.moves[0])", error.Message);
        error = Assert.Throws<RecordFormatException>(() => RecordSerializer.WriteBinary(typeof(Creature), new Creature { Name = "A", Moves = [new Fly { Altitude = 1 }] },
            new RecordWriteOptions { Versions = new Dictionary<string, int> { ["test.move"] = 1 } }));
        Assert.Equal("test.fly isn't a subtype of test.move version 1 (at $.moves[0])", error.Message);
    }

    [Fact]
    public void PastVersionsAreReadAndWrittenAsTheCurrentType()
    {
        var error = Assert.Throws<InvalidOperationException>(() => RecordSerializer.FromJson<Creature.V1>("""{"$types": {"test.creature": 1}, "name": "A"}"""u8));
        Assert.Contains("is version 1 of test.creature, a past version", error.Message);
        Assert.Throws<InvalidOperationException>(() => RecordSerializer.ToBinary(new Limb.V1 { Kind = "arm" }));
    }

    [Fact]
    public void BadDeclarationsAreRefused()
    {
        string Refused<T>(T value) where T : notnull => Assert.Throws<InvalidOperationException>(() => RecordSerializer.ToBinary(value)).Message;
        Assert.Equal("variant.no-past-version has no type for version 1 (a frozen copy nested in NoPastVersion)", Refused(new NoPastVersion()));
        Assert.Equal($"{typeof(PastTooNew.V2).FullName}: a past version of variant.past-too-new must be between 1 and 1, not 2", Refused(new PastTooNew()));
        Assert.Equal($"{typeof(NoUpgrade.V1).FullName} (version 1 of variant.no-upgrade) has no Upgrade() method", Refused(new NoUpgrade()));
        Assert.Equal($"{typeof(BadUpgrade.V1).FullName}.Upgrade() returns Int32, not version 2's BadUpgrade", Refused(new BadUpgrade()));
        Assert.Equal("SameNumber: two fields are number 1", Refused(new SameNumber()));
        Assert.Equal("SameJsonName: two fields are named value", Refused(new SameJsonName()));
        Assert.Equal("ZeroNumber.A: field numbers start at 1", Refused(new ZeroNumber()));
        Assert.Equal("Senses: flags enums aren't supported in records", Refused(new FlagsField()));
        Assert.Equal("Alias: two members have the value 1", Refused(new AliasField()));
        Assert.Equal("ObjectField.A: Object isn't a kind of field records have", Refused(new ObjectField()));
        Assert.Equal("BoolKeys.A: a dictionary's key is a string, an integer or an enum, not a Boolean", Refused(new BoolKeys()));
        Assert.Equal("ObliviousField.A: the nullability of String isn't known (records need nullable annotations)", Refused(new ObliviousField()));
        Assert.Equal("NoSetter: a record needs a public parameterless constructor or one whose parameters are its fields", Refused(new NoSetter(1)));
        Assert.Equal("ComputedField.B: a field that isn't a constructor parameter needs a setter", Refused(new ComputedField()));
        Assert.Equal($"{typeof(NotARecord).FullName} isn't a record type (it has no [Record])", Refused(new NotARecord()));
        Assert.Equal("BadName: the record name \"variant.Upper-Case\" isn't lowercase ASCII letters, digits, '.', '-' and '_'", Refused(new BadName()));
        Assert.Equal("ConcreteUnion: a union's type must be abstract", Refused<ConcreteUnion>(new ConcreteUnion()));
        Assert.Equal("ForeignSubtype, tag 1: Circle doesn't derive from ForeignSubtype", Refused<ForeignSubtype>(new ForeignSubtype.Member()));
        Assert.Equal("SameTag: two subtypes have the tag 1", Refused<SameTag>(new SameTag.A()));
        Assert.Equal("NestedUnion, tag 1: a subtype is a plain record, not another union", Refused<NestedUnion>(new NestedUnion.Member()));
        Assert.Equal("variant.back-to-plain version 2: a union can't become a plain record again", Refused(new BackToPlain()));
        Assert.Equal("Abstract: an abstract record type is a union and needs [Subtype]s", Refused<Abstract>(new Abstract.Member()));
        Assert.Equal("variant.two-past-ones has two types for version 1", Refused(new TwoPastOnes()));
        Assert.Equal("ZeroTag, tag 0: tags start at 1", Refused<ZeroTag>(new ZeroTag.Member()));
        Assert.Equal("ListedTwice: variant.listed-twice.member is listed twice", Refused<ListedTwice>(new ListedTwice.Member()));
        RecordSerializer.ToBinary(new TwinA());
        Assert.Equal($"{typeof(TwinB).FullName} and {typeof(TwinA).FullName} are both named variant.twin", Refused(new TwinB()));
        Assert.EndsWith("are both named variant.twin", Assert.Throws<InvalidOperationException>(() => RecordSerializer.FindTypes([typeof(TwinA).Assembly])).Message);
    }

    [Fact]
    public void FixturesAreWrittenOnlyInVersionsThatExist()
    {
        // The tests' hook for writing past versions: a version the type has, through a downgrade to its frozen copy.
        Assert.Equal("test.creature has no version 4", Assert.Throws<ArgumentException>(() => RecordSerializer.WriteBinary(typeof(Creature), Sample(),
            new RecordWriteOptions { Versions = new Dictionary<string, int> { ["test.creature"] = 4 }, Downgrade = Downgrade })).Message);
        Assert.Equal("test.creature has no version 0", Assert.Throws<ArgumentException>(() => RecordSerializer.WriteBinary(typeof(Creature), Sample(),
            new RecordWriteOptions { Versions = new Dictionary<string, int> { ["test.creature"] = 0 }, Downgrade = Downgrade })).Message);
        Assert.Equal("no downgrade of test.creature to version 1", Assert.Throws<ArgumentException>(() => RecordSerializer.WriteBinary(typeof(Creature), Sample(),
            new RecordWriteOptions { Versions = new Dictionary<string, int> { ["test.creature"] = 1 } })).Message);
        Assert.Equal("the downgrade of test.creature to version 1 gave a Creature", Assert.Throws<ArgumentException>(() => RecordSerializer.WriteBinary(typeof(Creature),
            Sample(), new RecordWriteOptions { Versions = new Dictionary<string, int> { ["test.creature"] = 1 }, Downgrade = (value, _) => value })).Message);
    }

    [Fact]
    public void ARecordThatBecameAUnionUpgradesIntoItsSubtypes()
    {
        var json = Encoding.UTF8.GetBytes("""{"$types": {"variant.became-union": 1}, "radius": 2}""");
        var value = Assert.IsType<BecameUnion.Round>(RecordSerializer.FromJson<BecameUnion>(json));
        Assert.Equal(2, value.Radius);
        RecordAssert.Equal(value, RecordSerializer.FromBinary<BecameUnion>(RecordSerializer.ToBinary<BecameUnion>(value)));
    }
}
