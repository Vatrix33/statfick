using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;

namespace Statventure.Game.Data.Actors;

/// <summary>
/// What the sheet's rules read besides the sheet (Docs/Game/Hero.md, "The actor record and its calculations"): the
/// races (GM2 step 1's records, whose sheet side <see cref="RaceTotals"/> folds), the totals of the sources later
/// milestones bring (the artifacts and the worn equipment GM3, the classes GM8; zero in GM2), the battle's overrides (GM5;
/// none outside a battle) and whether Limitless is on (GM15's cap of 200; off in GM2). Each source is a function of the
/// sheet, so a rule that changes the sheet (a level up) reads them again for the sheet it makes, as the old functions read
/// the page's globals again. <see cref="Plain"/> is a sheet with no race data, nothing worn, no class and no battle.
/// </summary>
public sealed record SheetContext
{
    public static SheetContext Plain { get; } = new();

    /// <summary>The races by id (<see cref="RaceCatalogue.Load"/>'s entries: <see cref="WithRaces"/>).</summary>
    public IReadOnlyDictionary<string, Race> Races { get; init; } = new Dictionary<string, Race>();

    /// <summary>The worn artifacts' totals (GM3; the old <c>calculateArtifactEffects</c>).</summary>
    public Func<ActorSheet, ArtifactSheetTotals> Artifacts { get; init; } = _ => ArtifactSheetTotals.None;

    /// <summary>The classes' totals (GM8; the old <c>calculateClassEffects</c>).</summary>
    public Func<ActorSheet, ClassSheetTotals> Classes { get; init; } = _ => ClassSheetTotals.None;

    /// <summary>The worn weapons' and armour's part (GM3; the old <c>equippedDefenseItems</c> and its kin).</summary>
    public Func<ActorSheet, EquipmentSheetTotals> Equipment { get; init; } = _ => EquipmentSheetTotals.None;

    /// <summary>The battle's overrides of the sheet's values (GM5; the old <c>window.Combat</c>'s), none outside a battle.</summary>
    public IBattleSheet Battle { get; init; } = NoBattle.Instance;

    /// <summary>Whether Limitless is on, raising the level cap to 200 (GM15; <c>LimitlessRules.cap</c>).</summary>
    public bool Limitless { get; init; }

    /// <summary>This context with the races of <paramref name="races"/> (as <see cref="RaceCatalogue.Load"/> gives them) by id.</summary>
    public SheetContext WithRaces(IEnumerable<RaceEntry> races) =>
        this with { Races = races.ToDictionary(r => r.Id, r => r.Race, StringComparer.Ordinal) };
}

/// <summary>The worn artifacts' totals the sheet reads (the old <c>emptyArtifactEffects</c>' fields; GM3 fills them).</summary>
public sealed record ArtifactSheetTotals
{
    public static ArtifactSheetTotals None { get; } = new();

    public AttributeValues StatFlat { get; init; } = AttributeValues.Zero;
    public AttributeValues StatPercent { get; init; } = AttributeValues.Zero;
    public AttributeValues StatPermanentPercent { get; init; } = AttributeValues.Zero;
    public double MaxHpFlat { get; init; }
    public double MaxHpPercent { get; init; }
    public double MaxManaFlat { get; init; }
    public double MaxManaPercent { get; init; }
}

/// <summary>The classes' totals the sheet reads (the old <c>emptyClassEffects</c>' fields; GM8 fills them).</summary>
public sealed record ClassSheetTotals
{
    public static ClassSheetTotals None { get; } = new();

    public AttributeValues StatPermanentPercent { get; init; } = AttributeValues.Zero;
    public DefenseValues DefensePermanentPercent { get; init; } = DefenseValues.Zero;
    public double MaxHpPercent { get; init; }
    public double MaxManaPercent { get; init; }
    public DefenseValues DefenseFlat { get; init; } = DefenseValues.Zero;
    public DefenseValues DefenseCap { get; init; } = DefenseValues.Uncapped;
}

/// <summary>
/// The worn weapons' and armour's part of the sheet (GM3 fills it): Peace and Balance's lock on Speed
/// (<c>wieldsTurnAndSpeedLock</c>), the worn armour's HP percentages summed, its caps (<c>equippedDefenseCap</c>) and its
/// defence for the current attributes (<c>calculateEquipmentDefense</c>, which reads them for an item's power).
/// </summary>
public sealed record EquipmentSheetTotals
{
    public static EquipmentSheetTotals None { get; } = new();

    public bool SpeedLock { get; init; }
    public double HpPercent { get; init; }
    public DefenseValues DefenseCap { get; init; } = DefenseValues.Uncapped;
    public Func<AttributeValues, EquipmentDefense> DefenseFor { get; init; } = _ => default;
}

/// <summary>The worn equipment's defence (the old <c>calculateEquipmentDefense</c>'s result).</summary>
public readonly record struct EquipmentDefense(double Armor, double EvasionDebuff, double EvasionFlat, double EvasionPercent);

/// <summary>
/// The battle's overrides of an actor's sheet values (GM5 fills them; the old <c>window.Combat</c>'s
/// <c>resourceMaximum</c>, <c>permanentStatPercent</c>, <c>permanentDefensePercent</c>, <c>statValue</c> and
/// <c>defense</c>, and the battle's <c>enemyCount</c>), by the old game's attribute and defence keys.
/// </summary>
public interface IBattleSheet
{
    /// <summary>The battle's maximum of a resource, or null to compute it.</summary>
    double? ResourceMaximum(ActorResource resource);
    double PermanentStatPercent(string attribute);
    double PermanentDefensePercent(string defense);
    /// <summary>An attribute's value after the battle's effects.</summary>
    double StatValue(string attribute, double value);
    /// <summary>Armor's or Evasion's value after the battle's effects.</summary>
    double Defense(string defense, double value);
    /// <summary>The enemies in the battle (the race's <c>defense-flat-per-enemy</c>).</summary>
    int EnemyCount { get; }
}

/// <summary>
/// Outside a battle: what the old <c>window.Combat</c> gives with no battle running (<c>combat.js</c> 94–140): no
/// maximum, no percentages, the attributes as they are, and Armor and Evasion floored and held at 0 or more (its
/// <c>defense</c> with no effects: <c>max(0, floor(floor(value × 1) × 1) + 0)</c>).
/// </summary>
public sealed class NoBattle : IBattleSheet
{
    public static NoBattle Instance { get; } = new();
    NoBattle() { }
    public double? ResourceMaximum(ActorResource resource) => null;
    public double PermanentStatPercent(string attribute) => 0;
    public double PermanentDefensePercent(string defense) => 0;
    public double StatValue(string attribute, double value) => value;
    public double Defense(string defense, double value) => Math.Max(0, Math.Floor(value));
    public int EnemyCount => 0;
}
