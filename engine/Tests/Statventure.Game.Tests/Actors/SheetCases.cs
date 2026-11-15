using System.Globalization;
using System.Text.Json;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;
using Statventure.Packs;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The sheet modules' cases (Reference/old-game/sheet-values.json, sheet-actions.json; Tools/OldGameOracle/shared/
/// sheet-cases.cjs and sheet-page.js) read as the port's: the sheet (actor.sheet in its JSON names), Dev Mode, and the
/// context: the game's races as it ships them (<c>Assets/Packs/data.pak</c>, <see cref="RaceCatalogue.Load"/>), a case's
/// crafted race ("stubs.race", the old file's form, converted by step 1's <see cref="OldRaceFile"/> as the page's
/// <c>normalizeRaceDefinition</c> took it), and its hand-made totals ("stubs") as test doubles whose formulas are the
/// page's stubs'.
/// </summary>
static class SheetCases
{
    static double N(JsonElement e) => OldGameReference.Number(e);
    static double N(JsonElement e, string name, double fallback = 0) => e.TryGetProperty(name, out var v) ? N(v) : fallback;

    static AttributeValues SomeAttributes(JsonElement parent, string name) => parent.TryGetProperty(name, out var e)
        ? new(N(e, "strength"), N(e, "vitality"), N(e, "mind"), N(e, "dexterity"), N(e, "intelligence"), N(e, "wisdom"), N(e, "speed"), N(e, "luck"))
        : AttributeValues.Zero;

    static DefenseValues Defenses(JsonElement parent, string name, double fallback = 0) =>
        parent.TryGetProperty(name, out var e) ? new(N(e, "armor", fallback), N(e, "evasion", fallback)) : new(fallback, fallback);

    /// <summary>The game's races as data.pak holds them, by id.</summary>
    public static IReadOnlyList<RaceEntry> ShippedRaces => Shipped.Value;
    static readonly Lazy<IReadOnlyList<RaceEntry>> Shipped = new(() =>
    {
        using var data = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "data.pak"))]);
        return RaceCatalogue.Load(data);
    });

    public static ActorSheet Sheet(JsonElement e)
    {
        var bonuses = e.GetProperty("bonuses");
        AttributeSet Set(string name) => AttributeSet.From(key => e.GetProperty(name).GetProperty(key).GetInt32());
        var purse = e.GetProperty("purse");
        return new ActorSheet
        {
            Race = e.GetProperty("race").GetString(),
            Level = e.GetProperty("level").GetInt32(),
            Experience = N(e.GetProperty("experience")),
            FreePoints = e.GetProperty("freePoints").GetInt32(),
            ClassPoints = e.GetProperty("classPoints").GetInt32(),
            Attributes = Set("attributes"),
            SpentPoints = Set("spentPoints"),
            Bonuses = new ActorBonuses
            {
                Strength = N(bonuses, "strength"), Vitality = N(bonuses, "vitality"), Mind = N(bonuses, "mind"), Dexterity = N(bonuses, "dexterity"),
                Intelligence = N(bonuses, "intelligence"), Wisdom = N(bonuses, "wisdom"), Speed = N(bonuses, "speed"), Luck = N(bonuses, "luck"),
                Armor = N(bonuses, "armor"), Evasion = N(bonuses, "evasion"),
            },
            Hp = e.GetProperty("hp").GetInt32(),
            Mana = e.GetProperty("mana").GetInt32(),
            Purse = new Purse { Silver = purse.GetProperty("silver").GetInt32(), Gold = purse.GetProperty("gold").GetInt32(), Platinum = purse.GetProperty("platinum").GetInt32() },
        };
    }

    public static bool DevMode(JsonElement input) => input.TryGetProperty("devMode", out var d) && d.GetBoolean();

    /// <summary>The context of a case: the shipped races (and its crafted one), its stubs as test doubles.</summary>
    public static SheetContext Context(JsonElement input)
    {
        var stubs = input.TryGetProperty("stubs", out var s) ? s : default;
        bool Has(string name, out JsonElement e)
        {
            e = default;
            return stubs.ValueKind == JsonValueKind.Object && stubs.TryGetProperty(name, out e);
        }
        // The page puts the crafted race after the file's 28 (normalizeRaceDefinition's index, which only its messages read).
        IEnumerable<RaceEntry> races = Has("race", out var crafted) ? ShippedRaces.Append(OldRaceFile.Normalize(crafted, ShippedRaces.Count)) : ShippedRaces;
        var context = SheetContext.Plain.WithRaces(races) with
        {
            Limitless = input.TryGetProperty("limitless", out var l) && l.GetBoolean(),
        };
        if (Has("artifacts", out var artifacts))
        {
            var totals = new ArtifactSheetTotals
            {
                StatFlat = SomeAttributes(artifacts, "statFlat"),
                StatPercent = SomeAttributes(artifacts, "statPercent"),
                StatPermanentPercent = SomeAttributes(artifacts, "statPermanentPercent"),
                MaxHpFlat = N(artifacts, "maxHpFlat"), MaxHpPercent = N(artifacts, "maxHpPercent"),
                MaxManaFlat = N(artifacts, "maxManaFlat"), MaxManaPercent = N(artifacts, "maxManaPercent"),
            };
            context = context with { Artifacts = _ => totals };
        }
        if (Has("classes", out var classes))
        {
            var totals = new ClassSheetTotals
            {
                StatPermanentPercent = SomeAttributes(classes, "statPermanentPercent"),
                DefensePermanentPercent = Defenses(classes, "defensePermanentPercent"),
                MaxHpPercent = N(classes, "maxHpPercent"), MaxManaPercent = N(classes, "maxManaPercent"),
                DefenseFlat = Defenses(classes, "defenseFlat"),
                DefenseCap = Defenses(classes, "defenseCap", double.PositiveInfinity),
            };
            context = context with { Classes = _ => totals };
        }
        if (Has("equipment", out var equipment))
        {
            var totals = new EquipmentSheetTotals
            {
                SpeedLock = equipment.TryGetProperty("speedLock", out var lockValue) && lockValue.GetBoolean(),
                HpPercent = 0 + N(equipment, "hpPercent"),
                DefenseCap = Defenses(equipment, "defenseCap", double.PositiveInfinity),
                DefenseFor = values => new EquipmentDefense(N(equipment, "armor") + Math.Floor(values.Strength * N(equipment, "armorPerStrength")),
                    N(equipment, "evasionDebuff"), N(equipment, "evasionFlat"), N(equipment, "evasionPercent")),
            };
            context = context with { Equipment = _ => totals };
        }
        var enemies = input.TryGetProperty("enemyCount", out var count) ? count.GetInt32() : 0;
        if (Has("battle", out var battle) || enemies != 0)
            context = context with { Battle = new StubBattle(battle, enemies) };
        return context;
    }

    /// <summary>The page's battle stub (sheet-page.js); with none, the old game's no battle with the enemies it names.</summary>
    sealed class StubBattle(JsonElement stub, int enemies) : IBattleSheet
    {
        JsonElement Part(string name) => stub.ValueKind == JsonValueKind.Object && stub.TryGetProperty(name, out var e) ? e : default;
        double In(string name, string key) => Part(name) is { ValueKind: JsonValueKind.Object } e ? N(e, key) : 0;
        public double? ResourceMaximum(ActorResource resource) =>
            Part("resourceMaximum") is { ValueKind: JsonValueKind.Object } e && e.TryGetProperty(resource == ActorResource.Hp ? "hp" : "mana", out var v) ? N(v) : null;
        public double PermanentStatPercent(string attribute) => In("permanentStatPercent", attribute);
        public double PermanentDefensePercent(string defense) => In("permanentDefensePercent", defense);
        public double StatValue(string attribute, double value) => value + In("statPlus", attribute);
        public double Defense(string defense, double value) =>
            stub.ValueKind == JsonValueKind.Object ? value * 2 + In("defensePlus", defense) : NoBattle.Instance.Defense(defense, value);
        public int EnemyCount => enemies;
    }

    /// <summary>The old game's <c>formatValue</c> (<c>app.js</c> 1627): one decimal at most, a whole number without one.</summary>
    public static string FormatValue(double value)
    {
        var rounded = JsNumbers.Round(value * 10) / 10;
        return rounded == Math.Floor(rounded) ? rounded.ToString("R", CultureInfo.InvariantCulture) : rounded.ToString("F1", CultureInfo.InvariantCulture);
    }
}
