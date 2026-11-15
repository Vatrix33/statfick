using System.Reflection;
using Statventure.Records;

namespace Statventure.Game.Data.Effects;

// The effect records (GM2-P3; Docs/Game/Hero.md, "The effect interpreter" and "How it works", step 1): one union,
// `effect`, with a subtype per type string of the old game, its record name `effect.<the old type>` and its fields the
// ones the old code reads for that type. GM2 brings the 22 types the races' passives use and `trap-limit-flat`, which the
// old race fold reads though no race uses it. The sheet side (what a race's effects add up to on the character sheet)
// is RaceTotals' fold; the battle side of every type is GM5's, so the seven battle-only types are records only, read by
// nothing yet. A later milestone that adds types makes a new version of the union (M9's rule); none changes what an
// existing type means. Numbers are the old game's JavaScript numbers (doubles); where the old code reads a number with
// toNumber or toInteger, the rules do the same with the field (JsValues), so a value the old code would have passed by
// (not finite) counts as the same default. No field has limits or choices the old loader didn't have (G4): a target the
// old code doesn't know is passed by, as there.

/// <summary>An effect of a passive (the old game's <c>{ type, … }</c> objects), one subtype per effect type.</summary>
[Record("effect", 1)]
[Kind("Effect")]
[Help("What a passive does: one effect type with its own values. A race's passive can hold several.")]
[Subtype(1, typeof(MaxHpPerLevels))]
[Subtype(2, typeof(MaxManaPerLevels))]
[Subtype(3, typeof(MaxHpPercent))]
[Subtype(4, typeof(UnarmedPowerPerLevels))]
[Subtype(5, typeof(UnarmedName))]
[Subtype(6, typeof(SpellCostPercent))]
[Subtype(7, typeof(TrapLimitFlat))]
[Subtype(8, typeof(HealingDisabled))]
[Subtype(9, typeof(SpellcastingDisabled))]
[Subtype(10, typeof(DebuffImmunity))]
[Subtype(11, typeof(StatPermanentPercent))]
[Subtype(12, typeof(DefensePermanentPercent))]
[Subtype(13, typeof(DefenseFlatPerEnemy))]
[Subtype(14, typeof(DefenseCap))]
[Subtype(15, typeof(EquipmentRestriction))]
[Subtype(16, typeof(AllStatsFlatPerLevelUp))]
[Subtype(17, typeof(IncomingSpellDamagePercent))]
[Subtype(18, typeof(TurnStartRestorePercent))]
[Subtype(19, typeof(SpellDamageManaBurnPercent))]
[Subtype(20, typeof(IncomingSpellMissChance))]
[Subtype(21, typeof(ConditionalStatsPercentVsHigherMaxHp))]
[Subtype(22, typeof(DeathSaveDecay))]
[Subtype(23, typeof(SpellPowerPercent))]
public abstract record Effect
{
    /// <summary>The record names' common start: a subtype is <c>effect.&lt;the old type&gt;</c>.</summary>
    public const string Prefix = "effect.";

    /// <summary>The old game's type string of this effect (<c>max-hp-per-levels</c> …): its record name without <see cref="Prefix"/>.</summary>
    public string Type => TypeOf(GetType());

    /// <summary>The old game's type string of the effect subtype <paramref name="type"/>.</summary>
    public static string TypeOf(Type type) => type.GetCustomAttribute<RecordAttribute>(false)!.Name[Prefix.Length..];

    /// <summary>Every subtype, in tag order, by the old game's type string.</summary>
    public static IReadOnlyDictionary<string, Type> Types { get; } =
        typeof(Effect).GetCustomAttributes<SubtypeAttribute>(false).OrderBy(s => s.Tag).ToDictionary(s => TypeOf(s.Type), s => s.Type);
}

/// <summary>Help texts the effects share.</summary>
static class EffectHelp
{
    public const string Interval = "How many character levels each step takes: the amount is added once for every full interval of the level (below 1: 1; a fraction rounded down).";
    public const string Element = "A spell element in small letters (fire, water, holy …); empty: every element.";
}

// ---------- The sheet side: what the old race fold reads (calculateRaceEffectsUncached, app.js 1101–1164) ----------

/// <summary><c>max-hp-per-levels</c>: maximum HP + amount for every full interval of the level (Rulers of the World).</summary>
[Record("effect.max-hp-per-levels", 1)]
[Kind("Max HP per levels")]
[Help("Maximum HP rises by the amount every few character levels.")]
public sealed record MaxHpPerLevels : Effect
{
    [Field(1)] [Help("The maximum HP added for each full interval.")] [Unit("HP")] public double Amount { get; init; }
    [Field(2)] [Help(EffectHelp.Interval)] [Unit("levels")] public double Interval { get; init; } = 1;
}

/// <summary><c>max-mana-per-levels</c>: maximum Mana + amount for every full interval of the level (Sylvan Heritage).</summary>
[Record("effect.max-mana-per-levels", 1)]
[Kind("Max Mana per levels")]
[Help("Maximum Mana rises by the amount every few character levels.")]
public sealed record MaxManaPerLevels : Effect
{
    [Field(1)] [Help("The maximum Mana added for each full interval.")] [Unit("Mana")] public double Amount { get; init; }
    [Field(2)] [Help(EffectHelp.Interval)] [Unit("levels")] public double Interval { get; init; } = 1;
}

/// <summary><c>max-hp-percent</c>: maximum HP + amount % (Giant's Blood).</summary>
[Record("effect.max-hp-percent", 1)]
[Kind("Max HP percent")]
[Help("Maximum HP raised (or lowered) by a percentage.")]
public sealed record MaxHpPercent : Effect
{
    [Field(1)] [Help("The percentage added to maximum HP (below 0 lowers it).")] [Unit("%")] public double Amount { get; init; }
}

/// <summary><c>unarmed-power-per-levels</c>: unarmed attacks' base power + amount for every full interval of the level (Natural Claws).</summary>
[Record("effect.unarmed-power-per-levels", 1)]
[Kind("Unarmed power per levels")]
[Help("Unarmed attacks gain base power every few character levels.")]
public sealed record UnarmedPowerPerLevels : Effect
{
    [Field(1)] [Help("The base power added to unarmed attacks for each full interval.")] public double Amount { get; init; }
    [Field(2)] [Help(EffectHelp.Interval)] [Unit("levels")] public double Interval { get; init; } = 1;
}

/// <summary><c>unarmed-name</c>: what unarmed attacks are called (Natural Claws: "Claws").</summary>
[Record("effect.unarmed-name", 1)]
[Kind("Unarmed name")]
[Help("Names the character's unarmed attacks.")]
public sealed record UnarmedName : Effect
{
    [Field(1)] [Help("The unarmed attacks' name, as Claws (empty: no name; with several, the last one counts).")] public string Name { get; init; } = "";
}

/// <summary><c>spell-cost-percent</c>: spells' Mana cost + amount %, all spells or those of a name's start or an element.</summary>
[Record("effect.spell-cost-percent", 1)]
[Kind("Spell cost percent")]
[Help("Changes what spells cost in Mana by a percentage: every spell, or only spells whose name starts with a text or of one element.")]
public sealed record SpellCostPercent : Effect
{
    [Field(1)] [Help("The percentage added to the Mana cost (below 0 makes spells cheaper).")] [Unit("%")] public double Amount { get; init; }
    [Field(2)] [Help("Only spells whose name starts with this text, in any case (empty: not limited by name).")] public string NamePrefix { get; init; } = "";
    [Field(3)] [Help("Only spells of this element (empty: not limited by element).")] public string Element { get; init; } = "";
}

/// <summary><c>trap-limit-flat</c>: more traps active at once (no race uses it; the old race fold reads it).</summary>
[Record("effect.trap-limit-flat", 1)]
[Kind("Trap limit")]
[Help("Raises how many traps can be active at once.")]
public sealed record TrapLimitFlat : Effect
{
    [Field(1)] [Help("Traps added to the limit.")] public double Amount { get; init; }
}

/// <summary><c>healing-disabled</c>: the character can't be healed (Unliving).</summary>
[Record("effect.healing-disabled", 1)]
[Kind("Healing disabled")]
[Help("The character can't regain HP: no healing, no rest, no HP restored on level up.")]
public sealed record HealingDisabled : Effect;

/// <summary><c>spellcasting-disabled</c>: the character can't cast spells (Unliving).</summary>
[Record("effect.spellcasting-disabled", 1)]
[Kind("Spellcasting disabled")]
[Help("The character can't cast spells.")]
public sealed record SpellcastingDisabled : Effect;

/// <summary><c>debuff-immunity</c>: debuffs don't take hold (Creature of Sloth).</summary>
[Record("effect.debuff-immunity", 1)]
[Kind("Debuff immunity")]
[Help("Debuffs, stuns and poisons don't take hold on the character.")]
public sealed record DebuffImmunity : Effect;

/// <summary><c>stat-permanent-percent</c>: an attribute + amount % (Creature of Wrath).</summary>
[Record("effect.stat-permanent-percent", 1)]
[Kind("Stat permanent percent")]
[Help("Raises (or lowers) one attribute by a percentage.")]
public sealed record StatPermanentPercent : Effect
{
    [Field(1)] [Help("The attribute: strength, vitality, mind, dexterity, intelligence, wisdom, speed or luck (any other counts for nothing).")] public string Target { get; init; } = "";
    [Field(2)] [Help("The percentage added to the attribute.")] [Unit("%")] public double Amount { get; init; }
}

/// <summary><c>defense-permanent-percent</c>: Armor or Evasion + amount % (Mountainborn, Nimble Stature).</summary>
[Record("effect.defense-permanent-percent", 1)]
[Kind("Defense permanent percent")]
[Help("Raises (or lowers) Armor or Evasion by a percentage.")]
public sealed record DefensePermanentPercent : Effect
{
    [Field(1)] [Help("armor or evasion (any other counts for nothing).")] public string Target { get; init; } = "";
    [Field(2)] [Help("The percentage added.")] [Unit("%")] public double Amount { get; init; }
}

/// <summary><c>defense-flat-per-enemy</c>: Armor or Evasion + amount for each enemy in the fight (Mud Runner).</summary>
[Record("effect.defense-flat-per-enemy", 1)]
[Kind("Defense flat per enemy")]
[Help("Raises Armor or Evasion for each enemy in the fight.")]
public sealed record DefenseFlatPerEnemy : Effect
{
    [Field(1)] [Help("armor or evasion (any other counts for nothing).")] public string Target { get; init; } = "";
    [Field(2)] [Help("Added for each enemy in the fight.")] public double Amount { get; init; }
}

/// <summary><c>defense-cap</c>: Armor or Evasion never above the amount (Manaborn: Evasion 0).</summary>
[Record("effect.defense-cap", 1)]
[Kind("Defense cap")]
[Help("Armor or Evasion can't exceed a value (with several, the lowest counts).")]
public sealed record DefenseCap : Effect
{
    [Field(1)] [Help("armor or evasion (any other counts for nothing).")] public string Target { get; init; } = "";
    [Field(2)] [Help("The highest the defence can be (below 0: 0).")] public double Amount { get; init; }
}

/// <summary><c>equipment-restriction</c>: items of a category and type can't be worn (Nimble Stature: no heavy armour).</summary>
[Record("effect.equipment-restriction", 1)]
[Kind("Equipment restriction")]
[Help("Items of a category and type can't be worn.")]
public sealed record EquipmentRestriction : Effect
{
    [Field(1)] [Help("The items' category, as armor or weapons (in any case).")] public string Category { get; init; } = "";
    [Field(2)] [Help("The items' type, as heavy (in any case).")] public string TypeName { get; init; } = "";
    [Field(3)] [Help("The items' type when the type name is empty (the old game read either).")] public string Target { get; init; } = "";
}

/// <summary><c>all-stats-flat-per-level-up</c>: every attribute + amount for each level gained (Large Form), read by the attributes' base, not the fold.</summary>
[Record("effect.all-stats-flat-per-level-up", 1)]
[Kind("All stats flat per level up")]
[Help("Every attribute rises by the amount for each level the character has gained.")]
public sealed record AllStatsFlatPerLevelUp : Effect
{
    [Field(1)] [Help("Added to each attribute's base for every level above 1.")] public double Amount { get; init; }
}

// ---------- The battle side only (GM5's to interpret): records the sheet doesn't read ----------

/// <summary><c>incoming-spell-damage-percent</c>: spell damage taken + amount %, all elements or one (Fishman Physiology); read in battle (combat.js 1816–1819).</summary>
[Record("effect.incoming-spell-damage-percent", 1)]
[Kind("Incoming spell damage percent")]
[Help("Spells hurt the character more or less: a percentage added to the spell damage it takes, for one element or all.")]
public sealed record IncomingSpellDamagePercent : Effect
{
    [Field(1)] [Help("The element (empty or all: every element).")] public string Element { get; init; } = "";
    [Field(2)] [Help("The percentage added to spell damage taken (below 0: less damage).")] [Unit("%")] public double Amount { get; init; }
}

/// <summary><c>turn-start-restore-percent</c>: HP restored at each turn's start unless recently hurt by an element (Creature of Gluttony); read in battle (combat.js 978–987).</summary>
[Record("effect.turn-start-restore-percent", 1)]
[Kind("Turn start restore percent")]
[Help("At the start of each of its turns in battle the character regains a share of its maximum HP, unless an element hurt it lately.")]
public sealed record TurnStartRestorePercent : Effect
{
    [Field(1)] [Help("The share of maximum HP regained.")] [Unit("%")] public double Hp { get; init; }
    [Field(2)] [Help("Damage of this element stops it for a while (empty: nothing stops it).")] public string UnlessDamagedByElement { get; init; } = "";
    [Field(3)] [Help("How many turns that damage stops it.")] [Unit("turns")] public double LookbackTurns { get; init; }
}

/// <summary><c>spell-damage-mana-burn-percent</c>: the target loses Mana as a share of the character's spell damage (Creature of Lust); read in battle (combat.js 4136).</summary>
[Record("effect.spell-damage-mana-burn-percent", 1)]
[Kind("Spell damage mana burn percent")]
[Help("The character's spell damage also burns the target's Mana, a share of the damage.")]
public sealed record SpellDamageManaBurnPercent : Effect
{
    [Field(1)] [Help("The share of the spell damage the target loses as Mana.")] [Unit("%")] public double Amount { get; init; }
}

/// <summary><c>incoming-spell-miss-chance</c>: spells aimed at the character miss more often (Creature of Pride); read in battle (combat.js 2845).</summary>
[Record("effect.incoming-spell-miss-chance", 1)]
[Kind("Incoming spell miss chance")]
[Help("Spells aimed at the character miss more often.")]
public sealed record IncomingSpellMissChance : Effect
{
    [Field(1)] [Help("The chance added that a spell aimed at the character misses.")] [Unit("%")] public double Amount { get; init; }
}

/// <summary><c>conditional-stats-percent-vs-higher-max-hp</c>: attributes + amount % while an enemy has more maximum HP (Creature of Envy); read in battle (combat.js 239–244).</summary>
[Record("effect.conditional-stats-percent-vs-higher-max-hp", 1)]
[Kind("Conditional stats percent vs higher max HP")]
[Help("While an enemy has more maximum HP than the character, some of its attributes rise by a percentage.")]
public sealed record ConditionalStatsPercentVsHigherMaxHp : Effect
{
    [Field(1)] [Help("The attributes that rise (strength, intelligence …).")] public IReadOnlyList<string> Targets { get; init; } = [];
    [Field(2)] [Help("The percentage added to each.")] [Unit("%")] public double Amount { get; init; }
}

/// <summary><c>death-save-decay</c>: a chance to defy death at 1 HP, smaller each time (Unliving); read in battle (combat.js 1920–1932).</summary>
[Record("effect.death-save-decay", 1)]
[Kind("Death save decay")]
[Help("A blow that would kill the character may leave it at 1 HP instead; the chance falls each time it does, and some elements kill it outright.")]
public sealed record DeathSaveDecay : Effect
{
    [Field(1)] [Help("The first chance to defy death (not set: none, as the old game read it).")] [Unit("%")] public double? InitialChance { get; init; }
    [Field(2)] [Help("How much the chance falls each time.")] [Unit("%")] public double Reduction { get; init; }
    [Field(3)] [Help("Damage of these elements kills at once, with no chance (holy, nature …).")] public IReadOnlyList<string> ExcludedElements { get; init; } = [];
}

/// <summary>
/// <c>spell-power-percent</c>: spells of an element + amount % power (Spirit of the Grove). In the old game nothing reads it
/// from a race: the battle's spell power takes timed effects and classes' passives, the passives' list only shows it.
/// </summary>
[Record("effect.spell-power-percent", 1)]
[Kind("Spell power percent")]
[Help("Spells of an element (or all) are stronger by a percentage.")]
public sealed record SpellPowerPercent : Effect
{
    [Field(1)] [Help(EffectHelp.Element)] public string Element { get; init; } = "";
    [Field(2)] [Help("The percentage added to spell power.")] [Unit("%")] public double Amount { get; init; }
}
