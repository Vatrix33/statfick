using System.Reflection;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Effects;
using Statventure.Records;

namespace Statventure.Game.Tests;

/// <summary>
/// The actor record (<c>actor.sheet</c> and its parts, GM2-P1): a new sheet is the old game's new hero
/// (Reference/old-game/new-hero.json), its marks, its two forms, and its fixtures for the library's layout guard
/// (<see cref="LibraryRecordLayoutTests"/>).
/// </summary>
public class ActorSheetTests
{
    /// <summary>A sheet that holds something in every field: a race, fractions, Dev Mode's negatives, a full purse.</summary>
    internal static ActorSheet Played { get; } = new()
    {
        Race = "goliath",
        Level = 40,
        Experience = 1234.5,
        FreePoints = 3,
        ClassPoints = 2,
        Attributes = new AttributeSet { Strength = 52, Vitality = 61, Mind = 40, Dexterity = 45, Intelligence = 49, Wisdom = 49, Speed = 48, Luck = -2 },
        SpentPoints = new AttributeSet { Strength = 3, Vitality = 12, Dexterity = 1 },
        Bonuses = new ActorBonuses { Strength = 12.5, Luck = -7.25, Armor = 1.0 / 3, Evasion = -10 },
        Hp = 118,
        Mana = 41,
        Purse = new Purse { Silver = 99, Gold = 1205, Platinum = 2 },
    };

    [Fact]
    public void ANewSheetIsTheOldGamesNewHero()
    {
        var (_, output) = Assert.Single(OldGameReference.Load("new-hero").Cases);
        Assert.Equal(SheetCases.Sheet(output), new ActorSheet());
        // Its HP and Mana are the maxima of its attributes (and Settle leaves it as it is).
        Assert.Equal((31.0, 20.0), (ActorRules.MaxHp(new ActorSheet(), SheetContext.Plain), ActorRules.MaxMana(new ActorSheet(), SheetContext.Plain)));
        Assert.Same(new ActorSheet() is var hero ? hero : null, ActorRules.Settle(hero, SheetContext.Plain));
    }

    [Fact]
    public void TheRecordsArePartsOfASaveNotEntries()
    {
        foreach (var type in new[] { typeof(ActorSheet), typeof(AttributeSet), typeof(ActorBonuses), typeof(Purse) })
        {
            Assert.True(type.IsDefined(typeof(NotAPackKindAttribute), false), type.Name);
            Assert.False(RecordShape.Of(type).IsPackKind, type.Name);
            Assert.Null(type.GetCustomAttribute<KindAttribute>());
        }
        Assert.Equal(("actor.sheet", 1), (typeof(ActorSheet).GetCustomAttribute<RecordAttribute>()!.Name, typeof(ActorSheet).GetCustomAttribute<RecordAttribute>()!.Version));
        // Each of the sheet's fields says which old field it is.
        foreach (var field in RecordShape.Of(typeof(ActorSheet)).Fields)
            Assert.Contains("(the old ", field.Help ?? "");
    }

    [Fact]
    public void ASheetKeepsEveryFieldInBothForms()
    {
        foreach (var sheet in new[] { new ActorSheet(), Played })
        {
            Assert.Equal(sheet, RecordSerializer.FromBinary<ActorSheet>(RecordSerializer.ToBinary(sheet)));
            Assert.Equal(sheet, RecordSerializer.FromJson<ActorSheet>(RecordSerializer.ToJson(sheet)));
        }
        Assert.Contains("\"freePoints\": 3", RecordSerializer.ToJson(Played));
        Assert.Contains("\"race\": \"goliath\"", RecordSerializer.ToJson(Played));
        // No race: the key left out (a null is written so).
        Assert.DoesNotContain("\"race\"", RecordSerializer.ToJson(new ActorSheet()));
    }

    [Fact]
    public void TheSheetsFixturesAreThere()
    {
        LibraryRecords.Ensure("new-hero", new ActorSheet());
        LibraryRecords.Ensure("played", Played);
        Assert.Equal(new ActorSheet(), LibraryRecords.Load("actor.sheet", "new-hero", ".json"));
        Assert.Equal(Played, LibraryRecords.Load("actor.sheet", "played", ".rec"));
    }

    [Fact]
    public void ThePartsReadAndChangeByTheOldKeys()
    {
        static int Index(string key) => AttributeKeys.All.ToList().IndexOf(key);
        var set = AttributeSet.From(k => Index(k) * 3 - 4);
        Assert.Equal([-4, -1, 2, 5, 8, 11, 14, 17], AttributeKeys.All.Select(k => set[k]));
        Assert.Equal((-4, -1, 2, 5, 8, 11, 14, 17), (set.Strength, set.Vitality, set.Mind, set.Dexterity, set.Intelligence, set.Wisdom, set.Speed, set.Luck));
        Assert.Equal(52, set.Sum());
        Assert.Equal(set with { Wisdom = 99 }, set.With(AttributeKeys.Wisdom, 99));
        Assert.Equal(AttributeSet.From(_ => 7), AttributeSet.All(7));
        Assert.Throws<ArgumentException>(() => set.With(DefenseKeys.Armor, 1));
        Assert.Throws<ArgumentException>(() => set["Strength"]);
        var bonuses = new ActorBonuses();
        foreach (var k in AttributeKeys.All)
        {
            bonuses = bonuses.With(k, Index(k) + 0.5);
            Assert.Equal(Index(k) + 0.5, bonuses[k]);
        }
        bonuses = bonuses.With(DefenseKeys.Armor, -1).With(DefenseKeys.Evasion, 2.25);
        Assert.Equal((0.5, 1.5, 2.5, 3.5, 4.5, 5.5, 6.5, 7.5, -1.0, 2.25),
            (bonuses.Strength, bonuses.Vitality, bonuses.Mind, bonuses.Dexterity, bonuses.Intelligence, bonuses.Wisdom, bonuses.Speed, bonuses.Luck, bonuses.Armor, bonuses.Evasion));
        Assert.Equal((-1.0, 2.25), (bonuses[DefenseKeys.Armor], bonuses[DefenseKeys.Evasion]));
        Assert.Throws<ArgumentException>(() => bonuses.With("hp", 1));
        var purse = new Purse().With(Coin.Silver, 1).With(Coin.Gold, 2).With(Coin.Platinum, 3);
        Assert.Equal((1, 2, 3), (purse[Coin.Silver], purse[Coin.Gold], purse[Coin.Platinum]));
        Assert.Equal((1, 2, 3), (purse.Silver, purse.Gold, purse.Platinum));
    }
}
