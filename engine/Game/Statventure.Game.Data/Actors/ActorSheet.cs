using Statventure.Records;

namespace Statventure.Game.Data.Actors;

/// <summary>
/// The actor record (<c>actor.sheet</c>; Docs/Game/Hero.md, "The actor record and its calculations", GM2-P1): what an
/// actor has, the hero now, companions (GM11) and every enemy (GM9) later, with no field asking which it is. The old
/// game's character record held it in about 45 globals and evaluated any actor by swapping them (<c>withActor</c>);
/// here it is one immutable value, and its rules are functions of it (<see cref="ActorRules"/>, GM2-P2). A new sheet is
/// the old <c>defaultCharacter</c>'s new hero: no race, Level 1, 10 in each attribute, one class point, 5 gold, HP 31
/// and Mana 20 (the maxima of those attributes: ⌊(15 + 5 + 5) × 1.25⌋ = 31; Hero.md's "HP 30" was wrong, the old
/// game's own code says 31: Reference/old-game/new-hero.json). Later milestones add their parts (equipment GM3, spells GM7, class
/// levels GM8, Limitless GM15) as new versions. Held in the save (<c>save.game</c> v2, GM2 step 6), never an entry of
/// its own (<see cref="NotAPackKindAttribute"/>).
/// </summary>
[NotAPackKind]
[Help("A character's sheet: race, level, EXP, points, attributes, bonuses, HP, Mana and purse.")]
[Record("actor.sheet", 1)]
public sealed record ActorSheet
{
    /// <summary>The race's id (its entry <c>data/races/&lt;id&gt;</c>), or null while none is chosen.</summary>
    [Field(1)] [Help("The race's id, or none while the hero hasn't chosen one (the old raceId).")]
    public string? Race { get; init; }

    [Field(2)] [Help("The level, from 1 to the cap: 100, 200 with Limitless (the old level).")]
    public int Level { get; init; } = 1;

    [Field(3)] [Help("The EXP gathered toward the next level (the old exp).")]
    public double Experience { get; init; }

    [Field(4)] [Help("The free points not spent yet: 5 each level (the old availablePoints).")]
    public int FreePoints { get; init; }

    [Field(5)] [Help("The class points not spent yet: one to start and one each level up to 100 (the old perkPoints).")]
    public int ClassPoints { get; init; } = 1;

    [Field(6)] [Help("The stored attributes, before the race's modifiers (the old stats).")]
    public AttributeSet Attributes { get; init; } = AttributeSet.All(10);

    [Field(7)] [Help("The free points spent on each attribute, which a respec gives back (the old manualAllocations).")]
    public AttributeSet SpentPoints { get; init; } = AttributeSet.All(0);

    [Field(8)] [Help("The permanent percentage bonuses (the old permanentBonuses and permanentDefenseBonuses).")]
    public ActorBonuses Bonuses { get; init; } = new();

    [Field(9)] [Help("The current HP (the old currentHp).")]
    public int Hp { get; init; } = 31;

    [Field(10)] [Help("The current Mana (the old currentMana).")]
    public int Mana { get; init; } = 20;

    [Field(11)] [Help("The purse (the old currencies).")]
    public Purse Purse { get; init; } = new() { Gold = 5 };
}
