using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Screens.Sheet;

namespace Statventure.Game.Tests;

/// <summary>
/// The heroes the sheet's tests and fixtures open (GM2 step 7), made by the sheet's own rules on the game's races as
/// <c>data.pak</c> ships them: a new hero (a race chosen, HP and Mana full) and a played one (Level 30 by level ups with the
/// EXP each needs, 142 of 145 free points spent, some HP and Mana lost, EXP toward 31, a purse), opened as the new hero's
/// flow (GM2 step 8) ends: made, given its race and look, then <c>open-sheet</c>.
/// </summary>
static class SheetHeroes
{
    /// <summary>The sheet's rules' context on the harness's data (its races).</summary>
    public static SheetContext Context(GameUiHarness h) => SheetContext.Plain.WithRaces(RaceCatalogue.Load(h.Document.Files!));

    /// <summary>A look of <paramref name="race"/> (its average body; the maker's data isn't in <c>data.pak</c> yet, GM2-P18).</summary>
    public static LookHero Look(string race, string sex = LookNames.Male) => new() { Race = race, Sex = sex, Body = $"{race}-{sex}-average" };

    /// <summary>A new hero of <paramref name="race"/> (the old <c>selectRace</c>: HP and Mana filled).</summary>
    public static ActorSheet New(SheetContext context, string race, bool devMode = false) =>
        ActorRules.ChooseRace(new ActorSheet(), context, race, devMode).Sheet;

    /// <summary>A played hero of <paramref name="race"/>: Level 30, three free points left, HP 61 and Mana 14 below their maxima, 1650 EXP, a purse.</summary>
    public static ActorSheet Played(SheetContext context, string race)
    {
        var hero = New(context, race);
        for (var level = 1; level < 30; level++)
            hero = ActorRules.LevelUp(hero with { Experience = hero.Experience + ActorRules.ExperienceRequired(hero) }, context, devMode: false);
        foreach (var (key, points) in new[] { (AttributeKeys.Strength, 40), (AttributeKeys.Vitality, 36), (AttributeKeys.Dexterity, 26), (AttributeKeys.Wisdom, 10), (AttributeKeys.Speed, 15), (AttributeKeys.Luck, 15) })
            for (var i = 0; i < points; i++)
                hero = ActorRules.Raise(hero, context, key, devMode: false);
        hero = ActorRules.RestoreBoth(hero, context);
        hero = ActorRules.ChangeResource(hero, context, ActorResource.Hp, -61);
        hero = ActorRules.ChangeResource(hero, context, ActorResource.Mana, -14);
        return hero with { Experience = 1650, Purse = new Purse { Silver = 340, Gold = 27, Platinum = 1 } };
    }

    /// <summary>A character named <paramref name="name"/> made, given <paramref name="hero"/> and <paramref name="look"/>, and its sheet asked for (<c>open-sheet</c>), as the new hero's flow ends; its slot.</summary>
    public static string Open(ScreensHarness s, string name, ActorSheet hero, LookHero? look)
    {
        var slot = s.Session.New(name);
        s.Session.Change(save => save with { Hero = hero, Look = look });
        s.H.Ui.Commands.Run(SheetScreen.OpenCommand);
        s.H.Frame();
        s.Ran.Remove(SheetScreen.RacesCommand);
        return slot;
    }

    /// <summary>Ser Alrik, the played human, opened.</summary>
    public static string OpenPlayed(ScreensHarness s) => Open(s, "Ser Alrik", Played(Context(s.H), "human"), Look("human"));

    /// <summary>Mira of the Vale, a new elf, opened.</summary>
    public static string OpenNew(ScreensHarness s) => Open(s, "Mira of the Vale", New(Context(s.H), "elf"), Look("elf", LookNames.Female));
}
