using Statventure.Game.Data.Effects;

namespace Statventure.Game.Data.Actors;

/// <summary>
/// A sheet's numbers corrected as the old game corrected a character it loaded (the user's Q45, 2026-11-16: silently, as
/// the old game, with no message and without refusing the file; Docs/Game/Hero.md, "How it works", step 6): the old
/// <c>normalizeCharacter</c> (<c>old-game/scripts/app.js</c> 1399–1563) for the fields the actor record holds, in its
/// order of rules. What it does there and so here:
/// <list type="bullet">
/// <item>the race's id through <c>slugify</c> ("Dark Elf" is <c>dark-elf</c>; nothing kept is "");</item>
/// <item>the level into 1 to the cap (100; 200 with Limitless, GM15);</item>
/// <item>the EXP 0 at the cap (none is gained there, and none banked there is kept: the old L18), else 0 or more, a
/// number that isn't finite 0;</item>
/// <item>the free points, the class points, the points spent on each attribute, the coins, HP and Mana 0 or more;</item>
/// <item>a permanent bonus that isn't finite 0;</item>
/// <item>the attributes as they are (the old game kept any whole number, below 0 too).</item>
/// </list>
/// What the old function did beyond the record's fields isn't here: the fields of later milestones (their versions bring
/// theirs), the old save's other keys (<c>stats</c> at the top, <c>modifiers</c>, <c>defenseModifiers</c>, which no port
/// file has), <c>startingPerkPointGranted</c> (every port sheet has had its starting class point), the Barbarian's old
/// choice reward (GM8's). A value the record can't hold at all (a fraction in a whole-number field, text for a number) is
/// the record's form's refusal before this, and a field the file leaves out takes the record's default: HP 31 and Mana 20
/// where the old game took the maxima of the file's attributes. HP and Mana above their maxima are held by the sheet's
/// rules when shown and after every action (<see cref="ActorRules.Settle"/>), as the old <c>updateDerivedValues</c>, not
/// here. Checked against the old game's own function on every case of <c>Reference/old-game/save-correction.json</c>.
/// </summary>
public static class SheetCorrection
{
    /// <summary><paramref name="sheet"/> corrected (a sheet equal to it when nothing was out of range); <paramref name="limitless"/> the cap's (GM15's, off until then).</summary>
    public static ActorSheet Correct(ActorSheet sheet, bool limitless = false)
    {
        var cap = Levelling.Cap(limitless);
        var level = Math.Min(cap, Math.Max(1, sheet.Level));
        return sheet with
        {
            Race = sheet.Race is { } race ? JsValues.Slugify(race) : null,
            Level = level,
            Experience = level >= cap ? 0 : Math.Max(0, JsValues.ToNumber(sheet.Experience)),
            FreePoints = Math.Max(0, sheet.FreePoints),
            ClassPoints = Math.Max(0, sheet.ClassPoints),
            SpentPoints = AttributeSet.From(key => Math.Max(0, sheet.SpentPoints[key])),
            Bonuses = Finite(sheet.Bonuses),
            Hp = Math.Max(0, sheet.Hp),
            Mana = Math.Max(0, sheet.Mana),
            Purse = new Purse
            {
                Silver = Math.Max(0, sheet.Purse.Silver),
                Gold = Math.Max(0, sheet.Purse.Gold),
                Platinum = Math.Max(0, sheet.Purse.Platinum),
            },
        };
    }

    /// <summary>Each bonus through the old <c>toNumber(value, 0)</c>.</summary>
    static ActorBonuses Finite(ActorBonuses bonuses)
    {
        var finite = bonuses;
        foreach (var key in AttributeKeys.All.Concat(DefenseKeys.All))
            finite = finite.With(key, JsValues.ToNumber(bonuses[key]));
        return finite;
    }
}
