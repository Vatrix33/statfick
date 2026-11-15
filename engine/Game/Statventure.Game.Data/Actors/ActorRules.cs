using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;
using static Statventure.Game.Data.Actors.JsNumbers;

namespace Statventure.Game.Data.Actors;

/// <summary>
/// The sheet's rules (Docs/Game/Hero.md, "The actor record and its calculations"; GM2-P2): functions of an immutable
/// <see cref="ActorSheet"/> and a <see cref="SheetContext"/>, the values computed when asked and each action giving a
/// new sheet (the same instance when the old game refuses it). Each is the old function's arithmetic on doubles in its
/// order (<c>old-game/scripts/app.js</c>, lines named on each), checked against the old game's own code
/// (<c>Reference/old-game/sheet-values.json</c>, <c>sheet-actions.json</c>). Attributes and defences are named by the old
/// game's keys (<see cref="AttributeKeys"/>, <see cref="DefenseKeys"/>). Every action ends as the old handlers end, with the
/// sheet shown again (<c>updateDerivedValues</c>), which holds HP and Mana to their maxima (<see cref="Settle"/>).
/// </summary>
public static class ActorRules
{
    /// <summary>The level cap (<c>LimitlessRules.cap</c>): 100, 200 with Limitless.</summary>
    public static int LevelCap(SheetContext context) => Levelling.Cap(context.Limitless);

    /// <summary>The sheet's race, or null with none or one the data doesn't have (the old <c>getSelectedRace</c>).</summary>
    public static Race? RaceOf(ActorSheet sheet, SheetContext context) =>
        !string.IsNullOrEmpty(sheet.Race) && context.Races.TryGetValue(sheet.Race, out var race) ? race : null;

    /// <summary>The race's sheet side for the sheet: folded at its level with the battle's enemies (<see cref="RaceTotals.Of"/>).</summary>
    public static RaceTotals RaceTotalsOf(ActorSheet sheet, SheetContext context) =>
        RaceTotals.Of(RaceOf(sheet, context), sheet.Level, context.Battle.EnemyCount);

    static AttributeValues Each(Func<string, double> make) =>
        new(make(AttributeKeys.Strength), make(AttributeKeys.Vitality), make(AttributeKeys.Mind), make(AttributeKeys.Dexterity),
            make(AttributeKeys.Intelligence), make(AttributeKeys.Wisdom), make(AttributeKeys.Speed), make(AttributeKeys.Luck));

    /// <summary>
    /// The attributes' bases as the sheet shows them: each stored one plus the race's modifier at the level
    /// (<c>displayedBaseStat</c>, 1093; <see cref="RaceTotals.StatModifiers"/>).
    /// </summary>
    public static AttributeValues BaseAttributes(ActorSheet sheet, SheetContext context)
    {
        var modifiers = RaceTotals.StatModifiers(RaceOf(sheet, context), sheet.Level);
        return Each(key => (double)sheet.Attributes[key] + modifiers[key]);
    }

    /// <summary>
    /// The current attributes (<c>currentStatsUncached</c>, 1196–1211): the base plus the artifacts' flat amounts, times one
    /// plus the summed percentages (the permanent bonus, the artifacts' two, the classes', the race's, the battle's),
    /// floored, then the battle's value; with Peace and Balance only Speed's increases count.
    /// </summary>
    public static AttributeValues CurrentAttributes(ActorSheet sheet, SheetContext context)
    {
        var artifacts = context.Artifacts(sheet);
        var classes = context.Classes(sheet);
        var race = RaceTotalsOf(sheet, context);
        var bases = BaseAttributes(sheet, context);
        var speedLock = context.Equipment(sheet).SpeedLock;
        double Counted(string key, double amount) => speedLock && key == AttributeKeys.Speed ? Math.Max(0, amount) : amount;
        return Each(key =>
        {
            var baseValue = bases[key] + Counted(key, artifacts.StatFlat[key]);
            double[] parts = [JsValues.ToNumber(sheet.Bonuses[key]), artifacts.StatPercent[key], artifacts.StatPermanentPercent[key],
                classes.StatPermanentPercent[key], race.StatPermanentPercent[key], OrZero(context.Battle.PermanentStatPercent(key))];
            var percent = 0.0;
            foreach (var part in parts)
                percent += Counted(key, part);
            var value = Math.Floor(baseValue * (1 + (JsValues.ToNumber(percent) / 100)));
            return context.Battle.StatValue(key, value);
        });
    }

    /// <summary>An attribute's permanent bonus as its field shows it (<c>effectivePermanentBonus</c>, 1171).</summary>
    public static double ShownBonus(ActorSheet sheet, SheetContext context, string attribute) =>
        JsValues.ToNumber(sheet.Bonuses[attribute]) + context.Artifacts(sheet).StatPermanentPercent[attribute]
            + context.Classes(sheet).StatPermanentPercent[attribute] + RaceTotalsOf(sheet, context).StatPermanentPercent[attribute]
            + OrZero(context.Battle.PermanentStatPercent(attribute));

    /// <summary>Armor's or Evasion's permanent bonus as its field shows it (<c>effectivePermanentDefenseBonus</c>, 1178).</summary>
    public static double ShownDefenseBonus(ActorSheet sheet, SheetContext context, string defense) =>
        JsValues.ToNumber(sheet.Bonuses[defense]) + context.Classes(sheet).DefensePermanentPercent[defense]
            + RaceTotalsOf(sheet, context).DefensePermanentPercent[defense] + OrZero(context.Battle.PermanentDefensePercent(defense));

    /// <summary>Max HP of the formula: <c>max(0, floor((1.5 VIT + STR/2 + MND/2) × 1.25))</c> (<c>calculateMaxHp</c>, 428).</summary>
    public static double MaxHpFormula(AttributeValues values) =>
        Math.Max(0, Math.Floor(((1.5 * values.Vitality) + (values.Strength / 2) + (values.Mind / 2)) * 1.25));

    /// <summary>Max Mana of the formula: <c>max(0, floor(MND + INT/2 + WIS/2))</c> (<c>calculateMaxMana</c>, 432).</summary>
    public static double MaxManaFormula(AttributeValues values) =>
        Math.Max(0, Math.Floor(values.Mind + (values.Intelligence / 2) + (values.Wisdom / 2)));

    /// <summary>Evasion of the formula, not floored: <c>(10 + 1.5 DEX + 0.5 LCK + 0.5 WIS) / 2</c> (<c>calculateEvasion</c>, 436).</summary>
    public static double EvasionFormula(AttributeValues values) =>
        (BaseEvasion + (1.5 * values.Dexterity) + (0.5 * values.Luck) + (0.5 * values.Wisdom)) / 2;

    /// <summary>Base Armor (<c>BASE_ARMOR</c>) and Evasion's constant (<c>BASE_EVASION</c>).</summary>
    public const double BaseArmor = 5, BaseEvasion = 10;

    /// <summary>
    /// Max HP (<c>calculateMaxHpWithArtifacts</c>, 1213–1222): the battle's maximum in a battle; else the formula plus the
    /// artifacts' and the race's flat HP, times one plus the artifacts', classes', race's and worn armour's percentages,
    /// floored and held at 0 or more.
    /// </summary>
    public static double MaxHp(ActorSheet sheet, SheetContext context) => MaxHp(sheet, context, CurrentAttributes(sheet, context));

    static double MaxHp(ActorSheet sheet, SheetContext context, AttributeValues values)
    {
        if (context.Battle.ResourceMaximum(ActorResource.Hp) is { } battleMaximum)
            return battleMaximum;
        var artifacts = context.Artifacts(sheet);
        var race = RaceTotalsOf(sheet, context);
        var classes = context.Classes(sheet);
        var baseMaxHp = MaxHpFormula(values) + artifacts.MaxHpFlat + race.MaxHpFlat;
        var armorHpPercent = context.Equipment(sheet).HpPercent;
        return Math.Max(0, Math.Floor(baseMaxHp * (1 + ((artifacts.MaxHpPercent + classes.MaxHpPercent + race.MaxHpPercent + armorHpPercent) / 100))));
    }

    /// <summary>
    /// Max Mana (<c>calculateMaxManaWithEffects</c>, 1224–1230): the battle's maximum in a battle; else the formula plus the
    /// race's and the artifacts' flat Mana, times one plus the classes' and artifacts' percentages, floored, 0 or more.
    /// </summary>
    public static double MaxMana(ActorSheet sheet, SheetContext context) => MaxMana(sheet, context, CurrentAttributes(sheet, context));

    static double MaxMana(ActorSheet sheet, SheetContext context, AttributeValues values)
    {
        if (context.Battle.ResourceMaximum(ActorResource.Mana) is { } battleMaximum)
            return battleMaximum;
        var artifacts = context.Artifacts(sheet);
        var baseMaxMana = MaxManaFormula(values) + RaceTotalsOf(sheet, context).MaxManaFlat + artifacts.MaxManaFlat;
        return Math.Max(0, Math.Floor(baseMaxMana * (1 + ((context.Classes(sheet).MaxManaPercent + artifacts.MaxManaPercent) / 100))));
    }

    /// <summary>
    /// Armor and Evasion, base, modified and current (<c>updateDerivedValues</c>, 1762–1783): base Armor 5 and the floored
    /// Evasion formula; modified with the worn equipment's defence; current times one plus the permanent percentage (the
    /// bonus, the classes', the race's, the battle's), floored, plus the flat amounts (the classes', and for Evasion the
    /// race's), through the battle's value, then the lowest of the equipment's, classes' and race's caps.
    /// </summary>
    public static ActorDefenses Defenses(ActorSheet sheet, SheetContext context) => Defenses(sheet, context, CurrentAttributes(sheet, context));

    static ActorDefenses Defenses(ActorSheet sheet, SheetContext context, AttributeValues values)
    {
        var equipment = context.Equipment(sheet);
        var equipmentDefense = equipment.DefenseFor(values);
        var baseEvasion = Math.Floor(EvasionFormula(values));
        var modifiedArmor = BaseArmor + equipmentDefense.Armor;
        var modifiedEvasion = Math.Max(0, Math.Floor((baseEvasion - equipmentDefense.EvasionDebuff + equipmentDefense.EvasionFlat) * (1 + equipmentDefense.EvasionPercent / 100)));
        var classes = context.Classes(sheet);
        var race = RaceTotalsOf(sheet, context);
        double Bonus(string key) =>
            JsValues.ToNumber(sheet.Bonuses[key]) + (classes.DefensePermanentPercent[key] + race.DefensePermanentPercent[key] + OrZero(context.Battle.PermanentDefensePercent(key)));
        var rawArmor = Math.Floor(modifiedArmor * (1 + (Bonus(DefenseKeys.Armor) / 100))) + classes.DefenseFlat.Armor;
        var rawEvasion = Math.Floor(modifiedEvasion * (1 + (Bonus(DefenseKeys.Evasion) / 100))) + classes.DefenseFlat.Evasion + race.DefenseFlat.Evasion;
        var currentArmor = Math.Min(Math.Min(Math.Min(equipment.DefenseCap.Armor, classes.DefenseCap.Armor), race.DefenseCap.Armor), context.Battle.Defense(DefenseKeys.Armor, rawArmor));
        var currentEvasion = Math.Min(Math.Min(Math.Min(equipment.DefenseCap.Evasion, classes.DefenseCap.Evasion), race.DefenseCap.Evasion), context.Battle.Defense(DefenseKeys.Evasion, rawEvasion));
        return new(new(BaseArmor, modifiedArmor, currentArmor), new(baseEvasion, modifiedEvasion, currentEvasion));
    }

    /// <summary>The EXP the sheet's level needs (<c>CombatRules.experienceRequired</c>).</summary>
    public static double ExperienceRequired(ActorSheet sheet) => Levelling.ExperienceRequired(sheet.Level);

    /// <summary>Whether the sheet is at the level cap.</summary>
    public static bool AtLevelCap(ActorSheet sheet, SheetContext context) => sheet.Level >= LevelCap(context);

    /// <summary>Whether the level-up button works (1823): not at the cap and, outside Dev Mode, with the EXP the level needs.</summary>
    public static bool CanLevelUp(ActorSheet sheet, SheetContext context, bool devMode) =>
        !(AtLevelCap(sheet, context) || (!devMode && sheet.Experience < ExperienceRequired(sheet)));

    /// <summary>Whether the Level icon flashes (1824): the EXP is there and the cap not reached, Dev Mode or not.</summary>
    public static bool LevelUpAlert(ActorSheet sheet, SheetContext context) =>
        !AtLevelCap(sheet, context) && sheet.Experience >= ExperienceRequired(sheet);

    /// <summary>Whether a "+" stepper works (1731–1732): in Dev Mode, or with a free point to spend.</summary>
    public static bool CanRaise(ActorSheet sheet, bool devMode) => devMode || sheet.FreePoints > 0;

    /// <summary>Whether the respec works (1725): some free point is spent.</summary>
    public static bool CanRespec(ActorSheet sheet) => sheet.SpentPoints.Sum() != 0;

    /// <summary>Everything the sheet shows for a sheet (<c>updateDerivedValues</c> and <c>updateEditingControls</c>), its HP and Mana held to their maxima.</summary>
    public static SheetValues Evaluate(ActorSheet sheet, SheetContext context, bool devMode)
    {
        var values = CurrentAttributes(sheet, context);
        var maxHp = MaxHp(sheet, context, values);
        var maxMana = MaxMana(sheet, context, values);
        var required = ExperienceRequired(sheet);
        return new SheetValues
        {
            BaseAttributes = BaseAttributes(sheet, context),
            CurrentAttributes = values,
            ShownBonuses = Each(a => ShownBonus(sheet, context, a)),
            ShownDefenseBonuses = new(ShownDefenseBonus(sheet, context, DefenseKeys.Armor), ShownDefenseBonus(sheet, context, DefenseKeys.Evasion)),
            MaxHp = maxHp,
            MaxMana = maxMana,
            Defenses = Defenses(sheet, context, values),
            Hp = Math.Min(maxHp, Math.Max(0, sheet.Hp)),
            Mana = Math.Min(maxMana, Math.Max(0, sheet.Mana)),
            LevelCap = LevelCap(context),
            AtLevelCap = AtLevelCap(sheet, context),
            ExperienceRequired = required,
            ExperienceRemaining = Math.Max(0, required - sheet.Experience),
            ExperiencePercent = Math.Min(100, Math.Max(0, (sheet.Experience / required) * 100)),
            LevelUpEnabled = CanLevelUp(sheet, context, devMode),
            LevelUpAlert = LevelUpAlert(sheet, context),
            RaiseEnabled = CanRaise(sheet, devMode),
            DevModeControls = devMode,
            RespecEnabled = CanRespec(sheet),
        };
    }

    /// <summary>
    /// The sheet as it is once shown (the end of <c>updateDerivedValues</c>, 1784–1788): HP and Mana held between 0 and
    /// their maxima. Every action ends with it, as the old handlers end with <c>updateDerivedValues</c>.
    /// </summary>
    public static ActorSheet Settle(ActorSheet sheet, SheetContext context)
    {
        var values = CurrentAttributes(sheet, context);
        var hp = ToInt(Math.Min(MaxHp(sheet, context, values), Math.Max(0, sheet.Hp)));
        var mana = ToInt(Math.Min(MaxMana(sheet, context, values), Math.Max(0, sheet.Mana)));
        return hp == sheet.Hp && mana == sheet.Mana ? sheet : sheet with { Hp = hp, Mana = mana };
    }

    /// <summary>
    /// A "+" stepper (1696–1713): spends a free point on the attribute when one is left (the attribute +1 and the point
    /// counted as spent there); otherwise raises it by one only in Dev Mode, and outside it does nothing. (Above Level 100
    /// GM15 also counts the point for Limitless, <c>LimitlessRules.noteSpent</c>.)
    /// </summary>
    public static ActorSheet Raise(ActorSheet sheet, SheetContext context, string attribute, bool devMode) =>
        Step(sheet, context, attribute, 1, devMode);

    /// <summary>A "−" stepper (1696–1713): Dev Mode's, the attribute −1 (no point given back); outside Dev Mode nothing.</summary>
    public static ActorSheet Lower(ActorSheet sheet, SheetContext context, string attribute, bool devMode) =>
        Step(sheet, context, attribute, -1, devMode);

    static ActorSheet Step(ActorSheet sheet, SheetContext context, string attribute, int change, bool devMode)
    {
        var spendsPoint = change > 0 && sheet.FreePoints > 0;
        if (!devMode && !spendsPoint)
            return sheet;
        var next = sheet with { Attributes = sheet.Attributes.With(attribute, sheet.Attributes[attribute] + change) };
        if (spendsPoint)
            next = next with
            {
                FreePoints = next.FreePoints - 1,
                SpentPoints = next.SpentPoints.With(attribute, next.SpentPoints[attribute] + 1),
            };
        return Settle(next, context);
    }

    /// <summary>
    /// The respec (2102–2114): every free point spent given back, the attributes lowered by them; nothing when none is
    /// spent. (GM15 also clears Limitless's count of points spent; the church's class respec is GM13's.)
    /// </summary>
    public static ActorSheet Respec(ActorSheet sheet, SheetContext context)
    {
        var refund = sheet.SpentPoints.Sum();
        if (refund == 0)
            return sheet;
        var next = sheet with
        {
            Attributes = AttributeSet.From(a => sheet.Attributes[a] - sheet.SpentPoints[a]),
            SpentPoints = AttributeSet.All(0),
            FreePoints = ToInt(sheet.FreePoints + (double)refund),
        };
        return Settle(next, context);
    }

    /// <summary>
    /// The level up (2122–2139): refused at the cap and, outside Dev Mode, below the EXP the level needs; else the level
    /// +1, that EXP taken off (not below 0), 5 free points, a class point while the level is 100 or less, every attribute
    /// +1, then half of the new max HP (unless the race disables healing) and half of max Mana restored, floored and capped
    /// (<c>healResourcesOnLevelUp</c>, 2029–2035).
    /// </summary>
    public static ActorSheet LevelUp(ActorSheet sheet, SheetContext context, bool devMode)
    {
        if (sheet.Level >= LevelCap(context))
            return sheet;
        var required = ExperienceRequired(sheet);
        if (!devMode && sheet.Experience < required)
            return sheet;
        var level = sheet.Level + 1;
        var next = sheet with
        {
            Level = level,
            Experience = Math.Max(0, sheet.Experience - required),
            FreePoints = sheet.FreePoints + Levelling.PointsPerLevel,
            ClassPoints = level <= Levelling.BaseCap ? sheet.ClassPoints + 1 : sheet.ClassPoints,
            Attributes = AttributeSet.From(a => sheet.Attributes[a] + 1),
        };
        var values = CurrentAttributes(next, context);
        var maxHp = MaxHp(next, context, values);
        var maxMana = MaxMana(next, context, values);
        if (!RaceTotalsOf(next, context).HealingDisabled)
            next = next with { Hp = ToInt(Math.Min(maxHp, next.Hp + Math.Floor(maxHp * 0.5))) };
        next = next with { Mana = ToInt(Math.Min(maxMana, next.Mana + Math.Floor(maxMana * 0.5))) };
        return Settle(next, context);
    }

    /// <summary>
    /// HP or Mana changed by hand (the sheet's − and + buttons, for everyone: <c>adjustResource</c>, 2008–2019): held
    /// between 0 and the maximum; healing refused for a race that disables it.
    /// </summary>
    public static ActorSheet ChangeResource(ActorSheet sheet, SheetContext context, ActorResource resource, int amount)
    {
        var values = CurrentAttributes(sheet, context);
        if (resource == ActorResource.Hp)
        {
            if (amount > 0 && RaceTotalsOf(sheet, context).HealingDisabled)
                return sheet;
            var maxHp = MaxHp(sheet, context, values);
            return Settle(sheet with { Hp = ToInt(Math.Min(maxHp, Math.Max(0, sheet.Hp + (double)amount))) }, context);
        }
        var maxMana = MaxMana(sheet, context, values);
        return Settle(sheet with { Mana = ToInt(Math.Min(maxMana, Math.Max(0, sheet.Mana + (double)amount))) }, context);
    }

    /// <summary>Dev Mode's "Full HP" and "Full Mana" (<c>restoreResource</c>, 2021–2027): to the maximum; HP not for a race that disables healing.</summary>
    public static ActorSheet RestoreResource(ActorSheet sheet, SheetContext context, ActorResource resource)
    {
        var values = CurrentAttributes(sheet, context);
        if (resource == ActorResource.Hp && !RaceTotalsOf(sheet, context).HealingDisabled)
            sheet = sheet with { Hp = ToInt(MaxHp(sheet, context, values)) };
        if (resource == ActorResource.Mana)
            sheet = sheet with { Mana = ToInt(MaxMana(sheet, context, values)) };
        return Settle(sheet, context);
    }

    /// <summary>Dev Mode's "Restore both to full" (2058–2064): HP (unless the race disables healing) and Mana to their maxima.</summary>
    public static ActorSheet RestoreBoth(ActorSheet sheet, SheetContext context)
    {
        var values = CurrentAttributes(sheet, context);
        if (!RaceTotalsOf(sheet, context).HealingDisabled)
            sheet = sheet with { Hp = ToInt(MaxHp(sheet, context, values)) };
        sheet = sheet with { Mana = ToInt(MaxMana(sheet, context, values)) };
        return Settle(sheet, context);
    }

    /// <summary>
    /// Dev Mode's attribute field (1676–1681): the typed value (floored) is the base the sheet shows, so the stored
    /// attribute is it less the race's modifier. Outside Dev Mode the old field is disabled: nothing.
    /// </summary>
    public static ActorSheet SetBaseAttribute(ActorSheet sheet, SheetContext context, string attribute, double typed, bool devMode)
    {
        if (!devMode)
            return sheet;
        // Every race's modifier in the data is a whole number; a fraction would leave the old page a fraction until the
        // next load floored it (normalizeCharacter): the stored attribute is floored at once.
        var stored = JsValues.ToInteger(typed) - RaceTotals.StatModifiers(RaceOf(sheet, context), sheet.Level)[attribute];
        return Settle(sheet with { Attributes = sheet.Attributes.With(attribute, ToInt(Math.Floor(stored))) }, context);
    }

    /// <summary>
    /// Dev Mode's permanent bonus of an attribute (1684–1693): the typed value less the artifacts' and the classes'
    /// permanent percentages (not the race's, which the field still shows: the old game's own quirk, kept).
    /// </summary>
    public static ActorSheet SetBonus(ActorSheet sheet, SheetContext context, string attribute, double typed, bool devMode)
    {
        if (!devMode)
            return sheet;
        var bonus = JsValues.ToNumber(typed) - context.Artifacts(sheet).StatPermanentPercent[attribute] - context.Classes(sheet).StatPermanentPercent[attribute];
        return Settle(sheet with { Bonuses = sheet.Bonuses.With(attribute, bonus) }, context);
    }

    /// <summary>Dev Mode's permanent bonus of Armor or Evasion (2081–2090): the typed value less the classes' and the race's percentages.</summary>
    public static ActorSheet SetDefenseBonus(ActorSheet sheet, SheetContext context, string defense, double typed, bool devMode)
    {
        if (!devMode)
            return sheet;
        var bonus = JsValues.ToNumber(typed) - context.Classes(sheet).DefensePermanentPercent[defense] - RaceTotalsOf(sheet, context).DefensePermanentPercent[defense];
        return Settle(sheet with { Bonuses = sheet.Bonuses.With(defense, bonus) }, context);
    }

    /// <summary>Dev Mode's purse (2066–2073): the typed value floored, 0 or more. (The old handler doesn't show the sheet again.)</summary>
    public static ActorSheet SetPurse(ActorSheet sheet, Coin coin, double typed, bool devMode) =>
        devMode ? sheet with { Purse = sheet.Purse.With(coin, ToInt(Math.Max(0, JsValues.ToInteger(typed)))) } : sheet;

    /// <summary>Dev Mode's EXP form (2037–2046): a typed amount above 0 added; nothing otherwise.</summary>
    public static ActorSheet AddExperience(ActorSheet sheet, SheetContext context, double typed, bool devMode)
    {
        if (!devMode)
            return sheet;
        var amount = JsValues.ToNumber(typed);
        if (amount <= 0)
            return sheet;
        return Settle(sheet with { Experience = sheet.Experience + amount }, context);
    }

    /// <summary>
    /// Choosing a race (<c>selectRace</c>, 3038–3068, as the carousel's confirmation and Dev Mode's change use it): refused
    /// for a race the data doesn't have or, outside Dev Mode, an enemy-only one ("Enable Dev Mode to select a
    /// non-playable race"), and outside Dev Mode for a hero who has another race ("A chosen race can only be changed in
    /// Dev Mode"); else the race set and HP and Mana filled to the new maxima (healing or not). (Taking off what the race
    /// can't wear is GM3's.)
    /// </summary>
    public static RaceChoice ChooseRace(ActorSheet sheet, SheetContext context, string race, bool devMode)
    {
        if (!context.Races.TryGetValue(race, out var chosen) || (!chosen.Playable && !devMode))
            return new(sheet, RaceRefusal.NotPlayable);
        if (!string.IsNullOrEmpty(sheet.Race) && sheet.Race != race && !devMode)
            return new(sheet, RaceRefusal.Locked);
        var next = sheet with { Race = race };
        var values = CurrentAttributes(next, context);
        next = next with { Hp = ToInt(MaxHp(next, context, values)), Mana = ToInt(MaxMana(next, context, values)) };
        return new(Settle(next, context), null);
    }
}

/// <summary>Why a race couldn't be chosen.</summary>
public enum RaceRefusal
{
    /// <summary>No such race, or an enemy-only race outside Dev Mode (the old "Enable Dev Mode to select a non-playable race").</summary>
    NotPlayable,
    /// <summary>The hero has another race and Dev Mode is off (the old "A chosen race can only be changed in Dev Mode").</summary>
    Locked,
}

/// <summary>A race chosen (<see cref="Refused"/> null) or refused (the sheet unchanged).</summary>
public sealed record RaceChoice(ActorSheet Sheet, RaceRefusal? Refused);

/// <summary>Armor's or Evasion's three values as the sheet shows them.</summary>
public readonly record struct DefenseShown(double Base, double Modified, double Current);

/// <summary>Armor and Evasion.</summary>
public readonly record struct ActorDefenses(DefenseShown Armor, DefenseShown Evasion);

/// <summary>What the sheet shows for a sheet (<see cref="ActorRules.Evaluate"/>).</summary>
public sealed record SheetValues
{
    public required AttributeValues BaseAttributes { get; init; }
    public required AttributeValues CurrentAttributes { get; init; }
    /// <summary>The attributes' permanent bonuses' fields: the sheet's own plus every source's.</summary>
    public required AttributeValues ShownBonuses { get; init; }
    public required DefenseValues ShownDefenseBonuses { get; init; }
    public required double MaxHp { get; init; }
    public required double MaxMana { get; init; }
    public required ActorDefenses Defenses { get; init; }
    /// <summary>HP and Mana held to their maxima (<see cref="ActorRules.Settle"/>).</summary>
    public required double Hp { get; init; }
    public required double Mana { get; init; }
    public required int LevelCap { get; init; }
    public required bool AtLevelCap { get; init; }
    public required double ExperienceRequired { get; init; }
    /// <summary>The EXP still to gather: 0 or more.</summary>
    public required double ExperienceRemaining { get; init; }
    /// <summary>The EXP bar's share, 0–100.</summary>
    public required double ExperiencePercent { get; init; }
    public required bool LevelUpEnabled { get; init; }
    public required bool LevelUpAlert { get; init; }
    /// <summary>Whether the "+" steppers work.</summary>
    public required bool RaiseEnabled { get; init; }
    /// <summary>Whether Dev Mode's controls work and show: the attribute and bonus fields, the "−" steppers, the purse, the EXP form, the full buttons.</summary>
    public required bool DevModeControls { get; init; }
    public required bool RespecEnabled { get; init; }
}
