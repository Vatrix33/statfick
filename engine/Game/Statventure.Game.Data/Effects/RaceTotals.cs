using Statventure.Game.Data.Races;
using HealingDisabledEffect = Statventure.Game.Data.Effects.HealingDisabled;
using SpellcastingDisabledEffect = Statventure.Game.Data.Effects.SpellcastingDisabled;

namespace Statventure.Game.Data.Effects;

/// <summary>A spell cost change limited to spells of a name's start or an element (<see cref="SpellCostPercent"/>), both in small letters, null when not limited so.</summary>
public sealed record SpellCostModifier(double Amount, string? NamePrefix, string? Element);

/// <summary>Items of a category and type a race can't wear (<see cref="EquipmentRestriction"/>), both in small letters, empty when not given.</summary>
public sealed record EquipmentRestrictionTotal(string Category, string Type);

/// <summary>
/// What a race's passive adds up to on the character sheet (GM2-P3's sheet side; the old game's <c>emptyRaceEffects</c>
/// and <c>calculateRaceEffectsUncached</c>, <c>old-game/scripts/app.js</c> 1058–1078 and 1101–1164): every field the
/// old totals have, each the old fold's arithmetic in its order. The rules read these (Hero.md, "The actor record");
/// GM5's battle reads the battle-only effect types from the race itself (<see cref="BattleSide"/>). The old totals'
/// <c>passives</c> list (the race's one passive with its race's id and name, for the passives' screen) isn't kept: it is
/// the race's <see cref="Race.Passive"/>.
/// </summary>
public sealed record RaceTotals
{
    /// <summary>Maximum HP added (<c>max-hp-per-levels</c>).</summary>
    public double MaxHpFlat { get; init; }

    /// <summary>Maximum HP's percentage (<c>max-hp-percent</c>).</summary>
    public double MaxHpPercent { get; init; }

    /// <summary>Maximum Mana added (<c>max-mana-per-levels</c>).</summary>
    public double MaxManaFlat { get; init; }

    /// <summary>Unarmed attacks' base power added (<c>unarmed-power-per-levels</c>; read by GM5's attacks).</summary>
    public double UnarmedPowerFlat { get; init; }

    /// <summary>Unarmed attacks' name (<c>unarmed-name</c>, the last given), or null.</summary>
    public string? UnarmedName { get; init; }

    /// <summary>Every spell's Mana cost percentage (<c>spell-cost-percent</c> limited to nothing; read by GM7's costs).</summary>
    public double SpellCostPercent { get; init; }

    /// <summary>Spell cost percentages limited to a name's start or an element (<c>spell-cost-percent</c>), in order.</summary>
    public IReadOnlyList<SpellCostModifier> SpellCostPercentModifiers { get; init; } = [];

    /// <summary>Each attribute's permanent percentage (<c>stat-permanent-percent</c>).</summary>
    public AttributeValues StatPermanentPercent { get; init; } = AttributeValues.Zero;

    /// <summary>Armor's and Evasion's permanent percentages (<c>defense-permanent-percent</c>).</summary>
    public DefenseValues DefensePermanentPercent { get; init; } = DefenseValues.Zero;

    /// <summary>Armor and Evasion added (<c>defense-flat-per-enemy</c> times the fight's enemies; 0 outside a fight).</summary>
    public DefenseValues DefenseFlat { get; init; } = DefenseValues.Zero;

    /// <summary>The highest Armor and Evasion may be (<c>defense-cap</c>, the lowest given); infinite when none.</summary>
    public DefenseValues DefenseCap { get; init; } = DefenseValues.Uncapped;

    /// <summary>Items that can't be worn (<c>equipment-restriction</c>), in order (read by GM3's equipment).</summary>
    public IReadOnlyList<EquipmentRestrictionTotal> EquipmentRestrictions { get; init; } = [];

    /// <summary>Traps added to the limit (<c>trap-limit-flat</c>).</summary>
    public double TrapLimitFlat { get; init; }

    /// <summary>The character can't regain HP (<c>healing-disabled</c>).</summary>
    public bool HealingDisabled { get; init; }

    /// <summary>The character can't cast spells (<c>spellcasting-disabled</c>; the old game folded it, and its battle read the race's effect instead).</summary>
    public bool SpellcastingDisabled { get; init; }

    /// <summary>Debuffs don't take hold (<c>debuff-immunity</c>; folded as <see cref="SpellcastingDisabled"/> is).</summary>
    public bool DebuffImmune { get; init; }

    /// <summary>The totals with no race (the old <c>emptyRaceEffects()</c>).</summary>
    public static RaceTotals None { get; } = new();

    /// <summary>
    /// The effect types whose sheet side this fold reads, the old fold's own fifteen (<c>trap-limit-flat</c> among them, which
    /// no race uses), and <c>all-stats-flat-per-level-up</c>, which the attributes' base reads
    /// (<see cref="Race.StatModifier"/>).
    /// </summary>
    public static IReadOnlySet<Type> SheetSide { get; } = new HashSet<Type>
    {
        typeof(MaxHpPerLevels), typeof(MaxManaPerLevels), typeof(MaxHpPercent), typeof(UnarmedPowerPerLevels), typeof(UnarmedName),
        typeof(SpellCostPercent), typeof(TrapLimitFlat), typeof(HealingDisabled), typeof(SpellcastingDisabled), typeof(DebuffImmunity),
        typeof(StatPermanentPercent), typeof(DefensePermanentPercent), typeof(DefenseFlatPerEnemy), typeof(DefenseCap),
        typeof(EquipmentRestriction), typeof(AllStatsFlatPerLevelUp),
    };

    /// <summary>The effect types only the battle reads (GM5's), which the sheet passes by: the old battle reads them from the race (<c>combat.js</c>).</summary>
    public static IReadOnlySet<Type> BattleSide { get; } = new HashSet<Type>
    {
        typeof(IncomingSpellDamagePercent), typeof(TurnStartRestorePercent), typeof(SpellDamageManaBurnPercent), typeof(IncomingSpellMissChance),
        typeof(ConditionalStatsPercentVsHigherMaxHp), typeof(DeathSaveDecay), typeof(SpellPowerPercent),
    };

    /// <summary>
    /// The race's effects folded at <paramref name="level"/> with <paramref name="enemyCount"/> enemies in the fight (the old
    /// <c>calculateRaceEffectsUncached</c>): each effect of its passive in order, its amount read as <c>toNumber(amount,
    /// 0)</c> and an interval as <c>max(1, toInteger(interval, 1))</c>; a target the totals don't have passed by. No race: <see cref="None"/>.
    /// </summary>
    public static RaceTotals Of(Race? race, int level, int enemyCount)
    {
        if (race is null)
            return None;
        double maxHpFlat = 0, maxHpPercent = 0, maxManaFlat = 0, unarmedPowerFlat = 0, spellCostPercent = 0, trapLimitFlat = 0;
        string? unarmedName = null;
        bool healingDisabled = false, spellcastingDisabled = false, debuffImmune = false;
        var spellCostModifiers = new List<SpellCostModifier>();
        var restrictions = new List<EquipmentRestrictionTotal>();
        var statPercent = AttributeValues.Zero;
        var defensePercent = DefenseValues.Zero;
        var defenseFlat = DefenseValues.Zero;
        var defenseCap = DefenseValues.Uncapped;
        foreach (var effect in race.Passive.Effects)
        {
            switch (effect)
            {
                case MaxHpPerLevels e:
                    maxHpFlat += Math.Floor(level / Interval(e.Interval)) * JsValues.ToNumber(e.Amount);
                    break;
                case MaxManaPerLevels e:
                    maxManaFlat += Math.Floor(level / Interval(e.Interval)) * JsValues.ToNumber(e.Amount);
                    break;
                case MaxHpPercent e:
                    maxHpPercent += JsValues.ToNumber(e.Amount);
                    break;
                case UnarmedPowerPerLevels e:
                    unarmedPowerFlat += Math.Floor(level / Interval(e.Interval)) * JsValues.ToNumber(e.Amount);
                    break;
                case UnarmedName e:
                    if (e.Name.Length > 0)
                        unarmedName = e.Name;
                    break;
                case SpellCostPercent e:
                    if (e.NamePrefix.Length > 0 || e.Element.Length > 0)
                        spellCostModifiers.Add(new(JsValues.ToNumber(e.Amount),
                            e.NamePrefix.Length > 0 ? JsValues.ToLowerCase(e.NamePrefix) : null,
                            e.Element.Length > 0 ? JsValues.ToLowerCase(e.Element) : null));
                    else
                        spellCostPercent += JsValues.ToNumber(e.Amount);
                    break;
                case TrapLimitFlat e:
                    trapLimitFlat += JsValues.ToNumber(e.Amount);
                    break;
                case HealingDisabledEffect:
                    healingDisabled = true;
                    break;
                case SpellcastingDisabledEffect:
                    spellcastingDisabled = true;
                    break;
                case DebuffImmunity:
                    debuffImmune = true;
                    break;
                case StatPermanentPercent e when AttributeValues.Has(e.Target):
                    statPercent = statPercent.With(e.Target, statPercent[e.Target] + JsValues.ToNumber(e.Amount));
                    break;
                case DefensePermanentPercent e when DefenseValues.Has(e.Target):
                    defensePercent = defensePercent.With(e.Target, defensePercent[e.Target] + JsValues.ToNumber(e.Amount));
                    break;
                case DefenseFlatPerEnemy e when DefenseValues.Has(e.Target):
                    defenseFlat = defenseFlat.With(e.Target, defenseFlat[e.Target] + JsValues.ToNumber(e.Amount) * Math.Max(0, enemyCount));
                    break;
                case DefenseCap e when DefenseValues.Has(e.Target):
                    defenseCap = defenseCap.With(e.Target, Math.Min(defenseCap[e.Target], Math.Max(0, JsValues.ToNumber(e.Amount))));
                    break;
                case EquipmentRestriction e:
                    restrictions.Add(new(JsValues.ToLowerCase(e.Category), JsValues.ToLowerCase(e.TypeName.Length > 0 ? e.TypeName : e.Target)));
                    break;
            }
        }
        return new RaceTotals
        {
            MaxHpFlat = maxHpFlat,
            MaxHpPercent = maxHpPercent,
            MaxManaFlat = maxManaFlat,
            UnarmedPowerFlat = unarmedPowerFlat,
            UnarmedName = unarmedName,
            SpellCostPercent = spellCostPercent,
            SpellCostPercentModifiers = spellCostModifiers,
            StatPermanentPercent = statPercent,
            DefensePermanentPercent = defensePercent,
            DefenseFlat = defenseFlat,
            DefenseCap = defenseCap,
            EquipmentRestrictions = restrictions,
            TrapLimitFlat = trapLimitFlat,
            HealingDisabled = healingDisabled,
            SpellcastingDisabled = spellcastingDisabled,
            DebuffImmune = debuffImmune,
        };
    }

    /// <summary>
    /// The race's modifier of each attribute at <paramref name="level"/> (<see cref="Race.StatModifier"/>, the old
    /// <c>raceStatModifier</c>); 0 for each with no race.
    /// </summary>
    public static AttributeValues StatModifiers(Race? race, int level) =>
        race is null ? AttributeValues.Zero : new(
            race.StatModifier(AttributeKeys.Strength, level), race.StatModifier(AttributeKeys.Vitality, level),
            race.StatModifier(AttributeKeys.Mind, level), race.StatModifier(AttributeKeys.Dexterity, level),
            race.StatModifier(AttributeKeys.Intelligence, level), race.StatModifier(AttributeKeys.Wisdom, level),
            race.StatModifier(AttributeKeys.Speed, level), race.StatModifier(AttributeKeys.Luck, level));

    /// <summary>An interval as the fold reads it: <c>max(1, toInteger(interval, 1))</c>.</summary>
    static double Interval(double interval) => Math.Max(1, JsValues.ToInteger(interval, 1));
}
