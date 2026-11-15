using System.Globalization;
using System.Text.Json;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;

namespace Statventure.Game.Tests;

/// <summary>
/// The sheet side of the races' effects (GM2-P3; Docs/Game/Hero.md, "How it works", step 1): <see cref="RaceTotals.Of"/>
/// and <see cref="Race.StatModifier"/> against the old game's <c>calculateRaceEffectsUncached</c> and
/// <c>raceStatModifier</c>, measured by the oracle (<c>Reference/old-game/race-fold.json</c>) for every race of races.json and the
/// case list's crafted races at levels 0–101, 150 and 200 and enemy counts −1, 0, 1 and 3; which effect types each side
/// reads; and the old checks of <c>new-races-browser.js</c> that act on the sheet, named after them.
/// </summary>
public class RaceTotalsTests
{
    /// <summary>Every case of the oracle: each field of the totals and each attribute's modifier as the old game gave them.</summary>
    [Fact]
    public void TheFoldOfEveryRaceAtEveryCaseIsTheOldGames()
    {
        var cases = RaceOracle.Fold.GetProperty("cases").EnumerateArray().ToList();
        var races = new HashSet<string>();
        var problems = new List<string>();
        foreach (var c in cases)
        {
            var input = c.GetProperty("input");
            var id = input.GetProperty("race").GetString()!;
            var level = input.GetProperty("level").GetInt32();
            var enemies = input.GetProperty("enemyCount").GetInt32();
            races.Add(id);
            var race = RaceOracle.AllRaces[id];
            var output = c.GetProperty("output");
            var want = Old(output.GetProperty("totals"));
            var have = Port(RaceTotals.Of(race, level, enemies));
            foreach (var (field, value) in want)
                if (have[field] != value)
                    problems.Add($"{id} at level {level}, {enemies} enemies: {field} is {have[field]}, the old game's {value}");
            Assert.Equal(want.Keys.Order(StringComparer.Ordinal), have.Keys.Order(StringComparer.Ordinal));
            var modifiers = RaceTotals.StatModifiers(race, level);
            foreach (var attribute in AttributeKeys.All)
            {
                var old = output.GetProperty("statModifier").GetProperty(attribute).GetDouble();
                if (modifiers[attribute] != old || race.StatModifier(attribute, level) != old)
                    problems.Add($"{id} at level {level}: the {attribute} modifier is {modifiers[attribute]}, the old game's {old}");
            }
            // The passive the old totals list is the race's own.
            Assert.Equal([race.Passive.Id], output.GetProperty("totals").GetProperty("passives").EnumerateArray().Select(p => p.GetString()));
        }
        Assert.True(problems.Count == 0, $"{problems.Count} differences, the first: " + string.Join("\n", problems.Take(20)));
        Assert.Equal(RaceOracle.AllRaces.Keys.Order(StringComparer.Ordinal), races.Order(StringComparer.Ordinal));
        Assert.Equal(33 * 112, cases.Count);
    }

    /// <summary>No race: the empty totals (<c>emptyRaceEffects</c>) and no modifiers.</summary>
    [Fact]
    public void NoRaceIsTheEmptyTotals()
    {
        Assert.Same(RaceTotals.None, RaceTotals.Of(null, 40, 3));
        Assert.Equal(Port(new RaceTotals()), Port(RaceTotals.None));
        Assert.Equal(double.PositiveInfinity, RaceTotals.None.DefenseCap.Armor);
        Assert.Equal(AttributeValues.Zero, RaceTotals.StatModifiers(null, 40));
    }

    /// <summary>
    /// Numbers the old fold read through <c>toNumber</c> and <c>toInteger</c> (<c>app.js</c> 1618–1625): an amount that isn't
    /// finite counts 0, an interval 1 (JSON can't hold them, the port's JSON form can); a modifier likewise 0.
    /// </summary>
    [Fact]
    public void ANumberThatIsntFiniteCountsAsToNumberCountsIt()
    {
        foreach (var bad in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            var race = TestRaces.Good("x") with
            {
                StatModifiers = new RaceModifiers { Strength = bad, Mind = 2 },
                Passive = new RacePassive
                {
                    Name = "x",
                    Effects =
                    [
                        new MaxHpPerLevels { Amount = bad, Interval = 2 }, new MaxHpPerLevels { Amount = 3, Interval = bad },
                        new MaxManaPerLevels { Amount = bad }, new MaxManaPerLevels { Amount = 1, Interval = bad },
                        new UnarmedPowerPerLevels { Amount = 2, Interval = bad }, new UnarmedPowerPerLevels { Amount = bad },
                        new MaxHpPercent { Amount = bad }, new TrapLimitFlat { Amount = bad }, new SpellCostPercent { Amount = bad },
                        new SpellCostPercent { Amount = bad, Element = "fire" }, new StatPermanentPercent { Target = "luck", Amount = bad },
                        new DefensePermanentPercent { Target = "armor", Amount = bad }, new DefenseFlatPerEnemy { Target = "armor", Amount = bad },
                        new DefenseCap { Target = "evasion", Amount = bad }, new AllStatsFlatPerLevelUp { Amount = bad },
                    ],
                },
            };
            var totals = RaceTotals.Of(race, 7, 2);
            Assert.Equal((0.0, 21.0, 7.0, 14.0), (totals.MaxHpPercent, totals.MaxHpFlat, totals.MaxManaFlat, totals.UnarmedPowerFlat));
            Assert.Equal((0.0, 0.0, 0.0, 0.0), (totals.TrapLimitFlat, totals.SpellCostPercent, totals.StatPermanentPercent.Luck, totals.DefensePermanentPercent.Armor));
            Assert.Equal((0.0, 0.0), (totals.DefenseFlat.Armor, totals.DefenseCap.Evasion));
            Assert.Equal(new SpellCostModifier(0, null, "fire"), Assert.Single(totals.SpellCostPercentModifiers));
            Assert.Equal((0.0, 2.0), (race.StatModifier("strength", 7), race.StatModifier("mind", 7)));
        }
    }

    /// <summary>
    /// A race's growth counts from level 1; a key that isn't an attribute's has no modifier of its own (the old
    /// <c>toNumber(undefined, 0)</c>) and the growth added all the same, as <c>raceStatModifier</c> adds it.
    /// </summary>
    [Fact]
    public void AModifierIsTheRacesAndItsGrowthPerLevel()
    {
        var giant = RaceOracle.AllRaces["giant"];
        Assert.Equal(98, giant.StatModifier("armor", 50));
        Assert.Equal(98, giant.StatModifier("Strength", 50));
        Assert.Equal(0, RaceOracle.AllRaces["human"].StatModifier("armor", 50));
        Assert.Equal((8.0, 10.0, 206.0), (giant.StatModifier("strength", 1), giant.StatModifier("strength", 2), giant.StatModifier("strength", 100)));
    }

    /// <summary>
    /// The two sides (GM2-P3, Hero.md's table): the fold's fifteen types with <c>all-stats-flat-per-level-up</c> on the
    /// sheet, the seven the old battle reads from the race on the battle side, none on both, every subtype on one. A
    /// battle-side effect changes nothing the sheet reads.
    /// </summary>
    [Fact]
    public void EachEffectTypeIsOnTheSheetSideOrTheBattleSide()
    {
        Assert.Empty(RaceTotals.SheetSide.Intersect(RaceTotals.BattleSide));
        Assert.Equal(Effect.Types.Values.ToHashSet(), RaceTotals.SheetSide.Union(RaceTotals.BattleSide).ToHashSet());
        Assert.Equal((16, 7), (RaceTotals.SheetSide.Count, RaceTotals.BattleSide.Count));
        Assert.Equal(
            ["conditional-stats-percent-vs-higher-max-hp", "death-save-decay", "incoming-spell-damage-percent", "incoming-spell-miss-chance",
             "spell-damage-mana-burn-percent", "spell-power-percent", "turn-start-restore-percent"],
            RaceTotals.BattleSide.Select(Effect.TypeOf).Order(StringComparer.Ordinal));
        var battle = RaceRecordTests.EveryEffect().Passive.Effects.Where(e => RaceTotals.BattleSide.Contains(e.GetType())).ToList();
        Assert.Equal(7, battle.Count);
        var race = TestRaces.Good("x") with { Passive = new RacePassive { Name = "x", Effects = battle } };
        Assert.Equal(Port(RaceTotals.None), Port(RaceTotals.Of(race, 60, 4)));
        Assert.Equal(AttributeValues.Zero with { Strength = 1, Vitality = 1, Luck = -2 }, RaceTotals.StatModifiers(race, 60));
    }

    // ---------- The old checks of new-races-browser.js that act on the sheet (their battle parts are GM5's) ----------

    /// <summary>
    /// <c>checkNewRaceData</c> (<c>old-game/test-client/tests/new-races-browser.js</c> 44–69) on the port's races: the three
    /// new races playable, each with its emblem (the old: its seal's file), nine playable races; the Half-Beast's and the
    /// Goliath's stats summing to 0, the Merfolk's kept; Fishman Physiology, Natural Claws and Giant's Blood; each new race
    /// with an accent of its own. (Its last part, the seals marked as placeholder art, has no counterpart: GM2-5 remakes
    /// every seal as an emblem, step 10.)
    /// </summary>
    [Fact]
    public void CheckNewRaceData()
    {
        var races = RaceOracle.FileRaces.ToDictionary(r => r.Id, r => r.Race);
        string[] fresh = ["half-beast", "goliath", "merfolk"];
        Assert.All(fresh, id => Assert.Equal((true, id), (races[id].Playable, races[id].Emblem)));
        Assert.Equal(9, races.Values.Count(r => r.Playable));
        double Sum(string id) => AttributeKeys.All.Sum(k => races[id].StatModifiers.Values[k]);
        Assert.Equal(new AttributeValues(1, 1, -1, 1, -2, 1, 1, -2), races["half-beast"].StatModifiers.Values);
        Assert.Equal(0, Sum("half-beast"));
        Assert.Equal(new AttributeValues(3, 3, -2, -2, -1, 0, -2, 1), races["goliath"].StatModifiers.Values);
        Assert.Equal(0, Sum("goliath"));
        Assert.Equal(new AttributeValues(1, 3, 1, 2, -2, 0, -1, -2), races["merfolk"].StatModifiers.Values);
        var fish = races["merfolk"].Passive;
        Assert.Equal("fishman-physiology", fish.Id);
        Assert.Contains(fish.Effects, e => e is IncomingSpellDamagePercent { Element: "water", Amount: -20 });
        Assert.Contains(fish.Effects, e => e is IncomingSpellDamagePercent { Element: "fire", Amount: 10 });
        Assert.Equal(("natural-claws", "giants-blood"), (races["half-beast"].Passive.Id, races["goliath"].Passive.Id));
        Assert.All(fresh, id => Assert.NotEqual(Race.DefaultAccent, races[id].Accent));
    }

    /// <summary>
    /// <c>checkGiantsBlood</c>'s race effects (<c>new-races-browser.js</c> 71–88): a Goliath at level 40 has maximum HP
    /// +10 %, Armor +5 % and Evasion −10 % from its race; a human none. (Its maximum HP through the formula is the actor
    /// rules', step 2; its companion in battle GM11's.)
    /// </summary>
    [Fact]
    public void CheckGiantsBlood()
    {
        var goliath = RaceTotals.Of(RaceOracle.AllRaces["goliath"], 40, 0);
        Assert.Equal((10.0, 5.0, -10.0), (goliath.MaxHpPercent, goliath.DefensePermanentPercent.Armor, goliath.DefensePermanentPercent.Evasion));
        Assert.Equal(0, RaceTotals.Of(RaceOracle.AllRaces["human"], 40, 0).MaxHpPercent);
    }

    /// <summary>
    /// <c>checkNaturalClaws</c>' race totals (<c>new-races-browser.js</c> 90–117): at level 30 the Half-Beast's claws
    /// have 3 more base power, at level 100 10 more; its unarmed attacks are named Claws, a human's not. (The attacks
    /// themselves, an unarmed 1-AP attack, their damage, the Monk's bonus and a weapon in hand, are the battle's, GM5.)
    /// </summary>
    [Fact]
    public void CheckNaturalClaws()
    {
        var beast = RaceOracle.AllRaces["half-beast"];
        Assert.Equal(3, RaceTotals.Of(beast, 30, 0).UnarmedPowerFlat);
        Assert.Equal(10, RaceTotals.Of(beast, 100, 0).UnarmedPowerFlat);
        Assert.Equal("Claws", RaceTotals.Of(beast, 40, 0).UnarmedName);
        var human = RaceTotals.Of(RaceOracle.AllRaces["human"], 40, 0);
        Assert.Equal((null, 0.0), (human.UnarmedName, human.UnarmedPowerFlat));
    }

    // ---------- The totals as text, field by field, as the oracle wrote the old game's ----------

    /// <summary>The old totals' fields as text (the oracle's form: non-finite numbers as strings), leaving out <c>passives</c>.</summary>
    static Dictionary<string, string> Old(JsonElement totals)
    {
        var fields = new Dictionary<string, string>();
        foreach (var field in totals.EnumerateObject())
        {
            switch (field.Name)
            {
                case "passives":
                    break;
                case "statPermanentPercent":
                    // Only the eight count (a target the old `in` let through, as toString, lands beside them).
                    foreach (var key in AttributeKeys.All)
                        fields[$"{field.Name}.{key}"] = Number(RaceOracle.Number(field.Value.GetProperty(key)));
                    break;
                case "defensePermanentPercent" or "defenseFlat" or "defenseCap":
                    Assert.Equal(DefenseKeys.All, field.Value.EnumerateObject().Select(p => p.Name));
                    foreach (var key in DefenseKeys.All)
                        fields[$"{field.Name}.{key}"] = Number(RaceOracle.Number(field.Value.GetProperty(key)));
                    break;
                case "spellCostPercentModifiers":
                    fields[field.Name] = string.Join("; ", field.Value.EnumerateArray().Select(m =>
                        $"{Number(m.GetProperty("amount").GetDouble())} {Text(m.GetProperty("namePrefix"))} {Text(m.GetProperty("element"))}"));
                    break;
                case "equipmentRestrictions":
                    fields[field.Name] = string.Join("; ", field.Value.EnumerateArray().Select(m => $"{Text(m.GetProperty("category"))} {Text(m.GetProperty("type"))}"));
                    break;
                case "unarmedName":
                    fields[field.Name] = Text(field.Value);
                    break;
                case "healingDisabled" or "spellcastingDisabled" or "debuffImmune":
                    fields[field.Name] = field.Value.GetBoolean() ? "true" : "false";
                    break;
                default:
                    fields[field.Name] = Number(RaceOracle.Number(field.Value));
                    break;
            }
        }
        return fields;
    }

    /// <summary>The port's totals in the same form.</summary>
    static Dictionary<string, string> Port(RaceTotals t)
    {
        var fields = new Dictionary<string, string>
        {
            ["maxHpFlat"] = Number(t.MaxHpFlat), ["maxHpPercent"] = Number(t.MaxHpPercent), ["maxManaFlat"] = Number(t.MaxManaFlat),
            ["unarmedPowerFlat"] = Number(t.UnarmedPowerFlat), ["unarmedName"] = Text(t.UnarmedName), ["spellCostPercent"] = Number(t.SpellCostPercent),
            ["spellCostPercentModifiers"] = string.Join("; ", t.SpellCostPercentModifiers.Select(m => $"{Number(m.Amount)} {Text(m.NamePrefix)} {Text(m.Element)}")),
            ["equipmentRestrictions"] = string.Join("; ", t.EquipmentRestrictions.Select(r => $"{Text(r.Category)} {Text(r.Type)}")),
            ["trapLimitFlat"] = Number(t.TrapLimitFlat), ["healingDisabled"] = t.HealingDisabled ? "true" : "false",
            ["spellcastingDisabled"] = t.SpellcastingDisabled ? "true" : "false", ["debuffImmune"] = t.DebuffImmune ? "true" : "false",
        };
        foreach (var key in AttributeKeys.All)
            fields[$"statPermanentPercent.{key}"] = Number(t.StatPermanentPercent[key]);
        foreach (var key in DefenseKeys.All)
        {
            fields[$"defensePermanentPercent.{key}"] = Number(t.DefensePermanentPercent[key]);
            fields[$"defenseFlat.{key}"] = Number(t.DefenseFlat[key]);
            fields[$"defenseCap.{key}"] = Number(t.DefenseCap[key]);
        }
        return fields;
    }

    /// <summary>A number as text: its shortest round-trip form, 0 and −0 alike (JSON writes −0 as 0).</summary>
    static string Number(double value) => value == 0 ? "0" : value.ToString("R", CultureInfo.InvariantCulture);

    static string Text(JsonElement value) => value.ValueKind == JsonValueKind.Null ? "null" : JsonSerializer.Serialize(value.GetString());

    static string Text(string? value) => value is null ? "null" : JsonSerializer.Serialize(value);
}
