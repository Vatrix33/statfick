using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Saves;
using Statventure.Records;

namespace Statventure.Game.Tests;

/// <summary>
/// A loaded character's numbers corrected silently, as the old game (the user's Q45, 2026-11-16;
/// <see cref="SheetCorrection"/>): every case of <c>Reference/old-game/save-correction.json</c>, the old
/// <c>normalizeCharacter</c> run in its test client on sheets in the port's words (the level, the EXP at and below the
/// cap, the race through <c>slugify</c>, points, coins, HP and Mana, the bonuses that aren't finite, the attributes kept),
/// and the save's <see cref="SaveGame.Corrected"/>.
/// </summary>
public class SheetCorrectionTests
{
    [Fact]
    public void EachSheetIsCorrectedAsTheOldGamesNormalizeCharacter()
    {
        var reference = OldGameReference.Load("save-correction");
        Assert.True(reference.Cases.Count >= 100);
        var changed = 0;
        for (var i = 0; i < reference.Cases.Count; i++)
        {
            var (input, output) = reference.Cases[i];
            var sheet = SheetCases.Sheet(input);
            var corrected = SheetCorrection.Correct(sheet);
            Assert.True(SheetCases.Sheet(output) == corrected,
                $"case {i}: {input}\nthe old game: {RecordSerializer.ToJson(SheetCases.Sheet(output))}\nthe port: {RecordSerializer.ToJson(corrected)}");
            if (corrected != sheet)
                changed++;
        }
        // The list reaches what is corrected and what isn't.
        Assert.InRange(changed, 50, reference.Cases.Count - 10);
    }

    [Fact]
    public void EachRuleOnItsOwn()
    {
        var hero = new ActorSheet();
        Assert.Equal(hero, SheetCorrection.Correct(hero));
        Assert.Equal(1, SheetCorrection.Correct(hero with { Level = 0 }).Level);
        Assert.Equal(1, SheetCorrection.Correct(hero with { Level = int.MinValue }).Level);
        Assert.Equal(100, SheetCorrection.Correct(hero with { Level = 101 }).Level);
        // The EXP: none at the cap, else 0 or more and finite.
        Assert.Equal(0, SheetCorrection.Correct(hero with { Level = 100, Experience = 50 }).Experience);
        Assert.Equal(0, SheetCorrection.Correct(hero with { Level = 140, Experience = 50 }).Experience);
        Assert.Equal(50, SheetCorrection.Correct(hero with { Level = 99, Experience = 50 }).Experience);
        Assert.Equal(0, SheetCorrection.Correct(hero with { Experience = -0.5 }).Experience);
        Assert.Equal(0, SheetCorrection.Correct(hero with { Experience = double.PositiveInfinity }).Experience);
        Assert.Equal(0, SheetCorrection.Correct(hero with { Experience = double.NaN }).Experience);
        // The race's id as slugify makes it.
        Assert.Equal("dark-elf", SheetCorrection.Correct(hero with { Race = " Dark Elf! " }).Race);
        Assert.Equal("", SheetCorrection.Correct(hero with { Race = "!!!" }).Race);
        Assert.Null(SheetCorrection.Correct(hero).Race);
        // Points, coins, HP and Mana 0 or more; the attributes as they are; bonuses that aren't finite 0.
        var low = hero with
        {
            FreePoints = -1, ClassPoints = -1, Hp = -1, Mana = -1, SpentPoints = AttributeSet.All(-2), Attributes = AttributeSet.All(-3),
            Purse = new Purse { Silver = -1, Gold = -2, Platinum = -3 }, Bonuses = new ActorBonuses { Mind = double.NaN, Evasion = double.NegativeInfinity, Luck = -12.5 },
        };
        Assert.Equal(hero with
        {
            FreePoints = 0, ClassPoints = 0, Hp = 0, Mana = 0, SpentPoints = AttributeSet.All(0), Attributes = AttributeSet.All(-3),
            Purse = new Purse(), Bonuses = new ActorBonuses { Luck = -12.5 },
        }, SheetCorrection.Correct(low));
    }

    /// <summary>With Limitless (GM15's) the cap is 200, as the old game's <c>LimitlessRules.cap(true)</c>.</summary>
    [Fact]
    public void WithLimitlessTheCapIs200()
    {
        var hero = new ActorSheet { Level = 150, Experience = 12 };
        Assert.Equal((150, 12.0), (SheetCorrection.Correct(hero, limitless: true).Level, SheetCorrection.Correct(hero, limitless: true).Experience));
        Assert.Equal((200, 0.0), (SheetCorrection.Correct(hero with { Level = 250 }, limitless: true).Level, SheetCorrection.Correct(hero with { Level = 250 }, limitless: true).Experience));
        Assert.Equal((100, 0.0), (SheetCorrection.Correct(hero).Level, SheetCorrection.Correct(hero).Experience));
    }

    [Fact]
    public void ASaveIsCorrectedInItsHeroOnly()
    {
        var look = TestLooks.WayfarerLook;
        var save = new SaveGame { Name = " Ser\tAlrik ", Hero = new ActorSheet { Race = "Human", Level = -4, Hp = -9 }, Look = look with { Race = "x" } };
        var corrected = save.Corrected();
        Assert.Equal(save with { Hero = new ActorSheet { Race = "human", Level = 1, Hp = 0 } }, corrected);
        Assert.Same(save.Look, corrected.Look);
        Assert.Equal(new SaveGame { Name = "Mira" }, new SaveGame { Name = "Mira" }.Corrected());
    }
}
