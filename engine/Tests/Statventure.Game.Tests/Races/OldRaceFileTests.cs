using System.Text.Json;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;

namespace Statventure.Game.Tests;

/// <summary>
/// The converter from races.json (<see cref="OldRaceFile"/>) against the old game: each of the oracle's
/// <c>normalizeRaceDefinition</c> cases made into the same race or refused with the same message, each of
/// <c>applyRaceText</c>'s refusals of a whole file the same, <c>slugify</c>'s ids the old game's; and the converter's own
/// refusals of what the port's records can't hold as the old game held it.
/// </summary>
public class OldRaceFileTests
{
    [Fact]
    public void EachRaceIsNormalizedAsNormalizeRaceDefinitionNormalizedIt()
    {
        var (made, refused) = (0, 0);
        foreach (var (raw, outcome) in RaceRecordTests.NormalizeCases())
        {
            var index = outcome.GetProperty("index").GetInt32();
            if (outcome.TryGetProperty("error", out var error))
            {
                var problem = Assert.Throws<OldRaceFileException>(() => OldRaceFile.Normalize(raw, index));
                Assert.Equal(error.GetString(), problem.Message);
                refused++;
            }
            else
            {
                var (id, race) = OldRaceFile.Normalize(raw, index);
                Assert.Equal(RaceDataTests.NormalizedForm(outcome.GetProperty("value")), RaceDataTests.NormalizedForm(id, race));
                Assert.Equal((index + 1) * 10, race.Order);
                Assert.Equal(OldRaceFile.AccentOf(id), race.Accent);
                made++;
            }
        }
        Assert.True(made >= 25 && refused >= 20, $"{made} made, {refused} refused");
    }

    [Fact]
    public void AFileIsRefusedAsApplyRaceTextRefusedIt()
    {
        foreach (var (entry, outcome) in RaceOracle.Cases.GetProperty("catalogue").EnumerateArray().Zip(RaceOracle.Rules.GetProperty("catalogue").EnumerateArray()))
        {
            Assert.Equal(entry.GetProperty("name").GetString(), outcome.GetProperty("name").GetString());
            var problem = Assert.Throws<OldRaceFileException>(() => OldRaceFile.Convert(entry.GetProperty("text").GetString()!));
            Assert.Equal(outcome.GetProperty("error").GetString(), problem.Message);
        }
        Assert.Empty(OldRaceFile.Convert("[]"));
    }

    /// <summary><c>slugify</c>: JavaScript's lower case (İ two characters, K the Kelvin sign a k), every run of others one "-", none at the ends.</summary>
    [Fact]
    public void AnIdIsSlugifiedAsTheOldGameSlugifiedIt()
    {
        Assert.Equal("dark-elf", OldRaceFile.Slugify("  Dark__Elf! "));
        Assert.Equal("i-nce", OldRaceFile.Slugify("\x0130nce"));
        Assert.Equal("kelvin-k", OldRaceFile.Slugify("Kelvin \x212A"));
        Assert.Equal("clan-7-of-9", OldRaceFile.Slugify("Clan 7 of 9"));
        Assert.Equal("", OldRaceFile.Slugify("!!! ???"));
        Assert.Equal("a-b", OldRaceFile.Slugify("-a--b-"));
        Assert.Equal("a", OldRaceFile.Slugify("a-"));
        Assert.Equal("a", OldRaceFile.Slugify("-a"));
        Assert.Equal("", OldRaceFile.Slugify("-"));
        Assert.Equal("caf", OldRaceFile.Slugify("Café"));
    }

    /// <summary>What the converter can't carry over refused with its own message, never guessed.</summary>
    [Fact]
    public void WhatThePortCantHoldIsRefused()
    {
        string Refusal(string race) => Assert.Throws<OldRaceFileException>(() => OldRaceFile.Convert($"[{race}]")).Message;
        const string Passive = "\"passive\": { \"name\": \"x\"";
        Assert.Equal("Race 1: an id that isn't text", Refusal($"{{ \"id\": 5, \"name\": \"A\", {Passive} }} }}"));
        Assert.Equal("Race 1: a passive's id that isn't text", Refusal($"{{ \"name\": \"A\", {Passive}, \"id\": true }} }}"));
        Assert.Equal("Race 1: a passive's condition that isn't text", Refusal($"{{ \"name\": \"A\", {Passive}, \"condition\": 5 }} }}"));
        Assert.Equal("Race 1: a passive's description that isn't text", Refusal($"{{ \"name\": \"A\", {Passive}, \"description\": [\"x\"] }} }}"));
        Assert.Equal("Race 1: effect 1: an effect without a type (the converter reads type, not kind)", Refusal($"{{ \"name\": \"A\", {Passive}, \"effects\": [{{ \"kind\": \"healing-disabled\" }}] }} }}"));
        Assert.Equal("Race 1: effect 2: an effect without a type (the converter reads type, not kind)", Refusal($"{{ \"name\": \"A\", {Passive}, \"effects\": [{{ \"type\": \"healing-disabled\" }}, [1]] }} }}"));
        Assert.Equal("Race 1: effect 1: an effect type the port doesn't have: fly", Refusal($"{{ \"name\": \"A\", {Passive}, \"effects\": [{{ \"type\": \"fly\" }}] }} }}"));
        Assert.Equal("Race 1: effect 1: max-hp-percent has no field interval", Refusal($"{{ \"name\": \"A\", {Passive}, \"effects\": [{{ \"type\": \"max-hp-percent\", \"interval\": 2 }}] }} }}"));
        Assert.Equal("Race 1: effect 1: max-hp-percent's amount isn't the kind of value the port holds there: \"5\"", Refusal($"{{ \"name\": \"A\", {Passive}, \"effects\": [{{ \"type\": \"max-hp-percent\", \"amount\": \"5\" }}] }} }}"));
        Assert.Equal("Race 1: effect 1: unarmed-name's name isn't the kind of value the port holds there: 5", Refusal($"{{ \"name\": \"A\", {Passive}, \"effects\": [{{ \"type\": \"unarmed-name\", \"name\": 5 }}] }} }}"));
        Assert.Equal("Race 1: effect 1: death-save-decay's excludedElements isn't the kind of value the port holds there: [1]", Refusal($"{{ \"name\": \"A\", {Passive}, \"effects\": [{{ \"type\": \"death-save-decay\", \"excludedElements\": [1] }}] }} }}"));
        Assert.Equal("Race 1: effect 1: max-hp-percent's amount isn't the kind of value the port holds there: 1e999", Refusal($"{{ \"name\": \"A\", {Passive}, \"effects\": [{{ \"type\": \"max-hp-percent\", \"amount\": 1e999 }}] }} }}"));
        Assert.Equal("Race 1: a seal the converter can't name: race-a.svg", Refusal($"{{ \"name\": \"A\", \"icon\": \"race-a.svg\", {Passive} }} }}"));
        Assert.Equal("Race 1: a seal the converter can't name: objects/race-.svg", Refusal($"{{ \"name\": \"A\", \"icon\": \"objects/race-.svg\", {Passive} }} }}"));
        Assert.Equal("Race 1: a seal the converter can't name: objects/race-a.png", Refusal($"{{ \"name\": \"A\", \"icon\": \"objects/race-a.png\", {Passive} }} }}"));
        // A modifier JSON writes past the largest number is infinite to JavaScript too: refused as the old game refused it.
        Assert.Equal("Race 1: strength modifier must be a number", Refusal($"{{ \"name\": \"A\", \"statModifiers\": {{ \"strength\": 1e999 }}, {Passive} }} }}"));
    }

    /// <summary>What it carries over: each kind of field of an effect, a null as not given, the seal's name, the file's order.</summary>
    [Fact]
    public void WhatThePortHoldsIsCarriedOver()
    {
        var races = OldRaceFile.Convert("""
            [
              { "name": "A", "icon": "objects/race-stone-kin.svg", "passive": { "name": "x", "effects": [
                { "type": "death-save-decay", "initialChance": 80, "reduction": null, "excludedElements": ["holy"] },
                { "type": "spell-cost-percent", "amount": 3, "element": null },
                { "type": "debuff-immunity" } ] } },
              { "name": "B", "passive": { "name": "y" } }
            ]
            """);
        Assert.Equal(["a", "b"], races.Select(r => r.Id));
        Assert.Equal((10, 20), (races[0].Race.Order, races[1].Race.Order));
        Assert.Equal(("stone-kin", null), (races[0].Race.Emblem, races[1].Race.Emblem));
        var effects = races[0].Race.Passive.Effects;
        var save = Assert.IsType<DeathSaveDecay>(effects[0]);
        Assert.Equal((80.0, 0.0), (save.InitialChance, save.Reduction));
        Assert.Equal(["holy"], save.ExcludedElements);
        Assert.Equal(new SpellCostPercent { Amount = 3 }, effects[1]);
        Assert.IsType<DebuffImmunity>(effects[2]);
        Assert.Null(Assert.IsType<DeathSaveDecay>(OldRaceFile.Convert("""[{ "name": "A", "passive": { "name": "x", "effects": [{ "type": "death-save-decay" }] } }]""")[0].Race.Passive.Effects[0]).InitialChance);
        Assert.Equal(Race.DefaultAccent, OldRaceFile.AccentOf("stone-kin"));
    }
}
