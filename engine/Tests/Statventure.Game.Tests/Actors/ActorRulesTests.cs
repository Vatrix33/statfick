using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Effects;

namespace Statventure.Game.Tests;

/// <summary>
/// The sheet's rules on their own: the old checks that guard them (named after them, each expectation as the old check
/// has it), and what the old page decides by its controls rather than its handlers (Dev Mode's fields are disabled
/// outside it: the port's rules refuse there; Reference/old-game/sheet-values.json's "controls" show the old page's).
/// </summary>
public class ActorRulesTests
{
    static readonly SheetContext Plain = SheetContext.Plain;
    static SheetContext Shipped => SheetContext.Plain.WithRaces(SheetCases.ShippedRaces);

    [Fact]
    public void RunMapChecksLevelIcon()
    {
        // map-browser.js 15–22 (runMapChecks): a new character, outside Dev Mode.
        var hero = new ActorSheet() with { Experience = Levelling.ExperienceRequired(1) };
        Assert.True(ActorRules.LevelUpAlert(hero, Plain), "The Level icon flashes when enough EXP is available");
        Assert.True(ActorRules.Evaluate(hero, Plain, devMode: false).LevelUpAlert);
        hero = hero with { Experience = hero.Experience - 1 };
        Assert.False(ActorRules.LevelUpAlert(hero, Plain), "The Level icon stops flashing below the EXP requirement");
        hero = hero with { Experience = 0 };
        Assert.False(ActorRules.Evaluate(hero, Plain, devMode: true).LevelUpAlert, "Dev-mode does not create a false level-up alert");
        // …while Dev Mode's button still levels up.
        Assert.True(ActorRules.Evaluate(hero, Plain, devMode: true).LevelUpEnabled);
        Assert.Equal(2, ActorRules.LevelUp(hero, Plain, devMode: true).Level);
    }

    [Fact]
    public void CheckGiantsBlood()
    {
        // new-races-browser.js 71–88, its maximum HP on the sheet (the race's totals are step 1's CheckGiantsBlood; the
        // companion's battle values GM11's): a hero of a race at level 40, nothing else changed.
        var goliath = new ActorSheet { Race = "goliath", Level = 40 };
        var values = ActorRules.CurrentAttributes(goliath, Shipped);
        var totals = ActorRules.RaceTotalsOf(goliath, Shipped);
        var baseHp = ActorRules.MaxHpFormula(values) + ArtifactSheetTotals.None.MaxHpFlat + totals.MaxHpFlat;
        Assert.Equal(Math.Floor(baseHp * 1.1), ActorRules.MaxHp(goliath, Shipped));
        Assert.True(ActorRules.MaxHp(goliath, Shipped) > baseHp, "A Goliath's maximum HP is 10% above its base");
        Assert.Equal(0, ActorRules.RaceTotalsOf(goliath with { Race = "human" }, Shipped).MaxHpPercent);
        // Found when task 11 was built (old-game/Docs/Notes.md, "New Races"; GM2-4, GM17's): at Level 40 with the default
        // attributes a human has 48 HP, a Goliath 40.
        Assert.Equal((48.0, 40.0), (ActorRules.MaxHp(goliath with { Race = "human" }, Shipped), ActorRules.MaxHp(goliath, Shipped)));
    }

    [Fact]
    public void DevModesEditsAreRefusedOutsideIt()
    {
        var sheet = ActorSheetTests.Played with { Race = null, FreePoints = 0 };
        Assert.Same(sheet, ActorRules.SetBaseAttribute(sheet, Plain, AttributeKeys.Mind, 33, devMode: false));
        Assert.Same(sheet, ActorRules.SetBonus(sheet, Plain, AttributeKeys.Mind, 40, devMode: false));
        Assert.Same(sheet, ActorRules.SetDefenseBonus(sheet, Plain, DefenseKeys.Evasion, 40, devMode: false));
        Assert.Same(sheet, ActorRules.SetPurse(sheet, Coin.Gold, 40, devMode: false));
        Assert.Same(sheet, ActorRules.AddExperience(sheet, Plain, 40, devMode: false));
        Assert.Same(sheet, ActorRules.Lower(sheet, Plain, AttributeKeys.Mind, devMode: false));
        Assert.Same(sheet, ActorRules.Raise(sheet, Plain, AttributeKeys.Mind, devMode: false));
        // In Dev Mode each takes.
        Assert.Equal(33, ActorRules.SetBaseAttribute(sheet, Plain, AttributeKeys.Mind, 33, devMode: true).Attributes.Mind);
        Assert.Equal(40, ActorRules.SetBonus(sheet, Plain, AttributeKeys.Mind, 40, devMode: true).Bonuses.Mind);
        Assert.Equal(40, ActorRules.SetDefenseBonus(sheet, Plain, DefenseKeys.Evasion, 40, devMode: true).Bonuses.Evasion);
        Assert.Equal(40, ActorRules.SetPurse(sheet, Coin.Gold, 40, devMode: true).Purse.Gold);
        Assert.Equal(1274.5, ActorRules.AddExperience(sheet, Plain, 40, devMode: true).Experience);
        Assert.Equal(39, ActorRules.Lower(sheet, Plain, AttributeKeys.Mind, devMode: true).Attributes.Mind);
        Assert.Equal(41, ActorRules.Raise(sheet, Plain, AttributeKeys.Mind, devMode: true).Attributes.Mind);
    }

    [Fact]
    public void ARefusedActionGivesTheSameSheet()
    {
        var hero = new ActorSheet();
        Assert.Same(hero, ActorRules.Respec(hero, Plain));
        Assert.Same(hero, ActorRules.LevelUp(hero, Plain, devMode: false));
        Assert.Same(hero, ActorRules.Settle(hero, Plain));
        var capped = hero with { Level = 100, Experience = 1e9 };
        Assert.Same(capped, ActorRules.LevelUp(capped, Plain, devMode: true));
        Assert.Equal(101, ActorRules.LevelUp(capped, Plain with { Limitless = true }, devMode: false).Level);
        var choice = ActorRules.ChooseRace(hero, Plain, "human", devMode: true);
        Assert.Same(hero, choice.Sheet);
        Assert.Equal(RaceRefusal.NotPlayable, choice.Refused);
        Assert.Equal("human", ActorRules.ChooseRace(hero, Shipped, "human", devMode: false).Sheet.Race);
    }

    [Fact]
    public void ASheetsRaceTheDataDoesntHaveGivesNothing()
    {
        var hero = new ActorSheet() with { Race = "no-such-race", Attributes = AttributeSet.All(12) };
        Assert.Null(ActorRules.RaceOf(hero, Shipped));
        Assert.Equal(RaceTotals.None, ActorRules.RaceTotalsOf(hero, Shipped));
        Assert.Null(ActorRules.RaceOf(hero with { Race = "" }, Shipped));
        Assert.Equal(12, ActorRules.BaseAttributes(hero, Shipped).Luck);
        Assert.Equal(ActorRules.Evaluate(hero with { Race = null }, Shipped, false), ActorRules.Evaluate(hero, Shipped, false));
        Assert.NotNull(ActorRules.RaceOf(hero with { Race = "elf" }, Shipped));
    }

    [Fact]
    public void TheContextTakesTheCataloguesRaces()
    {
        var races = SheetCases.ShippedRaces;
        Assert.Equal(28, races.Count);
        var context = SheetContext.Plain.WithRaces(races);
        Assert.Equal(races.Select(r => r.Id).Order(StringComparer.Ordinal), context.Races.Keys.Order(StringComparer.Ordinal));
        Assert.All(races, r => Assert.Same(r.Race, context.Races[r.Id]));
        Assert.Empty(SheetContext.Plain.Races);
    }
}
