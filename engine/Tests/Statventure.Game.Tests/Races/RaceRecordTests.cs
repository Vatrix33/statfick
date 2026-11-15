using System.Text.Json;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;
using Statventure.Records;

namespace Statventure.Game.Tests;

/// <summary>
/// The <c>race</c> kind and the <c>effect</c> union as records (GM2-P3, GM2-P4; Docs/Game/Hero.md, "How it works", step 1):
/// their fixtures for the layout guard (<see cref="LibraryRecordLayoutTests"/>), the kind as the editor and New entry
/// see it, each effect subtype named after the old game's type string, and the checks refusing what the old game's
/// <c>normalizeRaceDefinition</c> refused (its cases measured by the oracle, <c>Reference/old-game/race-rules.json</c>), in the
/// editor's words, with the record's own form refusing what a record can't hold.
/// </summary>
public class RaceRecordTests
{
    /// <summary>A race holding one effect of every subtype, each field away from its default, and a swatch.</summary>
    internal static Race EveryEffect() => new()
    {
        Name = "Every Effect",
        Playable = false,
        StatModifiers = new RaceModifiers { Strength = 1, Vitality = -2, Mind = 3.5, Dexterity = -4, Intelligence = 5, Wisdom = -6, Speed = 7, Luck = -8 },
        Passive = new RacePassive
        {
            Id = "all-at-once",
            Name = "All at Once",
            Condition = "While testing",
            Description = "One of each.",
            Effects =
            [
                new MaxHpPerLevels { Amount = 2, Interval = 5 },
                new MaxManaPerLevels { Amount = 1, Interval = 4 },
                new MaxHpPercent { Amount = 10 },
                new UnarmedPowerPerLevels { Amount = 1, Interval = 10 },
                new UnarmedName { Name = "Claws" },
                new SpellCostPercent { Amount = 80, NamePrefix = "Dragonbreath", Element = "fire" },
                new TrapLimitFlat { Amount = 1 },
                new HealingDisabled(),
                new SpellcastingDisabled(),
                new DebuffImmunity(),
                new StatPermanentPercent { Target = "strength", Amount = 15 },
                new DefensePermanentPercent { Target = "armor", Amount = 8 },
                new DefenseFlatPerEnemy { Target = "evasion", Amount = 2 },
                new DefenseCap { Target = "evasion", Amount = 0.5 },
                new EquipmentRestriction { Category = "armor", TypeName = "heavy", Target = "plate" },
                new AllStatsFlatPerLevelUp { Amount = 2 },
                new IncomingSpellDamagePercent { Element = "holy", Amount = 50 },
                new TurnStartRestorePercent { Hp = 5, UnlessDamagedByElement = "fire", LookbackTurns = 5 },
                new SpellDamageManaBurnPercent { Amount = 20 },
                new IncomingSpellMissChance { Amount = 5 },
                new ConditionalStatsPercentVsHigherMaxHp { Targets = ["strength", "intelligence"], Amount = 15 },
                new DeathSaveDecay { InitialChance = 80, Reduction = 20, ExcludedElements = ["holy", "nature", "blood"] },
                new SpellPowerPercent { Element = "nature", Amount = 20 },
            ],
        },
        Accent = "#4c9a8a",
        Emblem = "every-effect",
        Order = 285,
        Swatches = [new RaceSwatch { Slot = "skin", Name = "Ash grey", Colour = "#7d7a72" }],
    };

    [Fact]
    public void TheRacesFixturesExist()
    {
        // Version 1 of race, its parts and every effect subtype: the Goliath as converted, and a race holding one of each.
        LibraryRecords.Ensure("goliath", RaceOracle.FileRaces.Single(r => r.Id == "goliath").Race);
        LibraryRecords.Ensure("every-effect", EveryEffect());
        var table = LibraryRecords.Table("race", "every-effect", ".json").Select(e => e.Name).ToHashSet();
        Assert.Superset(table, LibraryRecords.Types.Keys.Where(k => k != "save.game").ToHashSet());
    }

    /// <summary>Each subtype's record name is <c>effect.</c> and the old game's type string, as the races' passives name them; the union is the 22 types the races use and <c>trap-limit-flat</c>.</summary>
    [Fact]
    public void EachEffectIsNamedAfterTheOldType()
    {
        foreach (var (type, subtype) in Effect.Types)
        {
            Assert.Equal("effect." + type, RecordShape.Of(subtype).Name);
            Assert.Equal(type, ((Effect)Activator.CreateInstance(subtype)!).Type);
        }
        var used = RaceOracle.Rules.GetProperty("races").EnumerateArray()
            .SelectMany(r => r.GetProperty("passive").GetProperty("effects").EnumerateArray()).Select(e => e.GetProperty("type").GetString()!).ToHashSet();
        Assert.Equal(22, used.Count);
        Assert.Equal(used.Append("trap-limit-flat").Order(StringComparer.Ordinal), Effect.Types.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void TheRaceKindIsAKindOfTheGamesData()
    {
        var shape = RecordShape.Of(typeof(Race));
        Assert.Equal(("race", "Race", "data/races", true), (shape.Name, shape.DisplayName, shape.Folder, shape.IsPackKind));
        Assert.Equal(["name", "playable", "statModifiers", "passive", "accent", "emblem", "order", "swatches"], shape.Fields.Select(f => f.JsonName));
        Assert.True(shape.Field("accent")!.IsColour);
        Assert.True(RecordShape.Of(typeof(RaceSwatch)).Field("colour")!.IsColour);
        Assert.Equal("data/races/dark-elf", RaceCatalogue.PathOf("dark-elf"));
        Assert.Throws<ArgumentException>(() => RaceCatalogue.PathOf("a/b"));
        // The union is shown by its subtypes' names: the old type strings in words.
        var union = RecordShape.Of(typeof(Effect));
        Assert.True(union.IsUnion);
        Assert.Equal("Max HP per levels", union.Subtypes[0].DisplayName);
        Assert.Equal(Effect.Types.Count, union.Subtypes.Select(s => s.DisplayName).Distinct().Count());
    }

    // ---------- The checks against normalizeRaceDefinition ----------

    /// <summary>
    /// The name, as <c>normalizeRaceDefinition</c> refused it on the oracle's cases ("name must be a nonempty string":
    /// empty once JavaScript's trim takes its white space, U+FEFF and the line and space separators among it, NEL not):
    /// the same raw name on a port race is refused exactly there, "the name is empty" at its field.
    /// </summary>
    [Fact]
    public void ANameIsRefusedWhereNormalizeRaceDefinitionRefusedIt()
    {
        var cases = 0;
        foreach (var (raw, outcome) in NormalizeCases())
        {
            if (raw.ValueKind != JsonValueKind.Object || !raw.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String)
                continue;
            var refused = outcome.TryGetProperty("error", out var error) && error.GetString()!.EndsWith("name must be a nonempty string", StringComparison.Ordinal);
            var problems = RecordChecks.Check(TestRaces.Good("x") with { Name = name.GetString()! });
            Assert.True((refused ? ["the name is empty (at $.name)"] : Array.Empty<string>()).SequenceEqual(problems.Select(p => p.ToString())),
                $"the name \"{name.GetString()}\": {(refused ? "refused by the old game" : "taken by the old game")}, the port's problems: {string.Join("; ", problems)}");
            cases++;
        }
        Assert.True(cases >= 10, $"only {cases} cases with a name");
    }

    /// <summary>
    /// A modifier that isn't a finite number refused at its field ("&lt;attribute&gt; modifier must be a number": the old
    /// loader's <c>Number.isFinite</c>; JSON can't hold NaN, the port's JSON form can), each attribute and only those.
    /// </summary>
    [Fact]
    public void AModifierThatIsntANumberIsRefused()
    {
        Assert.Empty(RecordChecks.Check(EveryEffect()));
        foreach (var attribute in AttributeKeys.All)
            foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                var modifiers = new RaceModifiers();
                modifiers = attribute switch
                {
                    AttributeKeys.Strength => modifiers with { Strength = value },
                    AttributeKeys.Vitality => modifiers with { Vitality = value },
                    AttributeKeys.Mind => modifiers with { Mind = value },
                    AttributeKeys.Dexterity => modifiers with { Dexterity = value },
                    AttributeKeys.Intelligence => modifiers with { Intelligence = value },
                    AttributeKeys.Wisdom => modifiers with { Wisdom = value },
                    AttributeKeys.Speed => modifiers with { Speed = value },
                    _ => modifiers with { Luck = value },
                };
                var written = double.IsNaN(value) ? "NaN" : value > 0 ? "Infinity" : "-Infinity";
                Assert.Equal([$"the {attribute} modifier must be a number (it is {written}) (at $.statModifiers.{attribute})"],
                    RecordChecks.Check(TestRaces.Good("x") with { StatModifiers = modifiers }).Select(p => p.ToString()));
            }
        // Several at once, in the attributes' order; the largest and smallest finite numbers taken.
        Assert.Equal(["$.statModifiers.mind", "$.statModifiers.luck"],
            RecordChecks.Check(TestRaces.Good("x") with { StatModifiers = new() { Luck = double.NaN, Mind = double.NegativeInfinity, Speed = double.MaxValue, Wisdom = -double.MaxValue } })
                .Select(p => p.Path.ToString()));
    }

    /// <summary>What a record can't hold the old loader refused too: a race with no name or passive, a passive with no name (the record's own form refuses them, "required").</summary>
    [Fact]
    public void ARaceWithoutANameOrAPassiveDoesntRead()
    {
        var good = RecordSerializer.ToJson(TestRaces.Good("Ember Folk"));
        Assert.Equal(TestRaces.Good("Ember Folk"), RecordSerializer.FromJson<Race>(good), RaceComparer.Instance);
        foreach (var without in new[] { "name", "passive" })
        {
            var json = JsonDocument.Parse(good).RootElement.EnumerateObject().Where(m => m.Name != without).ToDictionary(m => m.Name, m => m.Value);
            var problem = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Race>(JsonSerializer.Serialize(json)));
            Assert.Equal(("$", $"race needs the key \"{without}\""), (problem.Path, problem.Reason));
        }
        var passive = Assert.Throws<RecordFormatException>(() => RecordSerializer.FromJson<Race>(good.Replace("\"name\": \"Ember Blood\",", "")));
        Assert.Equal(("$.passive", "race.passive needs the key \"name\""), (passive.Path, passive.Reason));
        // An empty passive name is the old game's too ("passive must be an object with a name" asks only for text).
        Assert.Empty(RecordChecks.Check(TestRaces.Good("x") with { Passive = TestRaces.Good("x").Passive with { Name = "" } }));
    }

    [Fact]
    public void TheAccentAndASwatchAreColoursAndTheEmblemAName()
    {
        string[] Problems(Race race) => [.. RecordChecks.Check(race).Select(p => p.ToString())];
        Assert.Empty(Problems(TestRaces.Good("x") with { Accent = "#A0b1C2", Emblem = "dark-elf_2.b", Swatches = [new() { Colour = "#ffffff" }] }));
        foreach (var accent in new[] { "red", "#12345", "#1234567", "12345678", "#12345g", "x123456", "" })
            Assert.Equal([$"\"{accent}\" isn't a colour (#rrggbb) (at $.accent)"], Problems(TestRaces.Good("x") with { Accent = accent }));
        Assert.Equal(["\"#fff\" isn't a colour (#rrggbb) (at $.swatches[1].colour)"],
            Problems(TestRaces.Good("x") with { Swatches = [new() { Colour = "#000000" }, new() { Colour = "#fff" }] }));
        foreach (var emblem in new[] { "", "Human", "dark elf", "race/human", "human.", "..", "a/" })
            Assert.Equal([$"\"{emblem}\" isn't an emblem's name (one name of small letters a–z, digits, '-', '_' and '.') (at $.emblem)"],
                Problems(TestRaces.Good("x") with { Emblem = emblem }));
        Assert.Empty(Problems(TestRaces.Good("x") with { Emblem = null }));
    }

    /// <summary>The condition shown as the old game made it: "Always active" when empty.</summary>
    [Fact]
    public void AnEmptyConditionIsShownAsAlwaysActive()
    {
        Assert.Equal("Always active", new RacePassive { Name = "x", Condition = "" }.ShownCondition);
        Assert.Equal("Always active", new RacePassive { Name = "x" }.ShownCondition);
        Assert.Equal("While burning", new RacePassive { Name = "x", Condition = "While burning" }.ShownCondition);
    }

    /// <summary>The oracle's <c>normalizeRaceDefinition</c> cases: each raw race with what the old game made of it (<c>value</c> or <c>error</c>).</summary>
    internal static IEnumerable<(JsonElement Raw, JsonElement Outcome)> NormalizeCases() =>
        RaceOracle.Cases.GetProperty("normalize").EnumerateArray().Zip(RaceOracle.Rules.GetProperty("normalize").EnumerateArray())
            .Select(pair =>
            {
                Assert.Equal(pair.First.GetProperty("name").GetString(), pair.Second.GetProperty("name").GetString());
                return (pair.First.GetProperty("raw"), pair.Second);
            });
}
