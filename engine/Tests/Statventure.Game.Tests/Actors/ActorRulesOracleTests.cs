using System.Globalization;
using System.Text.Json;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Effects;

namespace Statventure.Game.Tests;

/// <summary>
/// The sheet's rules (<see cref="ActorRules"/>) against the old game's own code on every case of the sheet modules
/// (Reference/old-game/sheet-values.json, sheet-actions.json: the old page's functions and handlers in its test client),
/// on the game's races as data.pak ships them, crafted races and hand-made totals (<see cref="SheetCases"/>).
/// </summary>
public class ActorRulesOracleTests
{
    static double N(JsonElement e) => OldGameReference.Number(e);

    static string JsString(double value) =>
        double.IsInteger(value) && Math.Abs(value) < 1e15 ? ((long)value).ToString(CultureInfo.InvariantCulture) : value.ToString("R", CultureInfo.InvariantCulture);

    [Fact]
    public void SheetValuesAreTheOldGames()
    {
        var cases = OldGameReference.Load("sheet-values").Cases;
        Assert.True(cases.Count >= 300, $"{cases.Count} cases");
        var wrong = new List<string>();
        foreach (var (input, output) in cases)
        {
            var sheet = SheetCases.Sheet(input.GetProperty("sheet"));
            var dev = SheetCases.DevMode(input);
            var context = SheetCases.Context(input);
            var values = ActorRules.Evaluate(sheet, context, dev);
            var problems = new List<string>();
            void Number(string what, double expected, double actual)
            {
                if (!OldGameReference.Same(expected, actual))
                    problems.Add($"{what}: {expected.ToString("R", CultureInfo.InvariantCulture)}, port {actual.ToString("R", CultureInfo.InvariantCulture)}");
            }
            void Same<T>(string what, T expected, T actual)
            {
                if (!EqualityComparer<T>.Default.Equals(expected, actual))
                    problems.Add($"{what}: {expected}, port {actual}");
            }
            foreach (var a in AttributeKeys.All)
            {
                Number($"base {a}", N(output.GetProperty("baseAttributes").GetProperty(a)), values.BaseAttributes[a]);
                Number($"current {a}", N(output.GetProperty("currentAttributes").GetProperty(a)), values.CurrentAttributes[a]);
                Number($"bonus {a}", N(output.GetProperty("bonuses").GetProperty(a)), values.ShownBonuses[a]);
            }
            foreach (var d in DefenseKeys.All)
            {
                Number($"bonus {d}", N(output.GetProperty("bonuses").GetProperty(d)), values.ShownDefenseBonuses[d]);
                var shown = d == DefenseKeys.Armor ? values.Defenses.Armor : values.Defenses.Evasion;
                var expected = output.GetProperty(d);
                Number($"{d} base", N(expected.GetProperty("base")), shown.Base);
                Number($"{d} modified", N(expected.GetProperty("modified")), shown.Modified);
                Number($"{d} current", N(expected.GetProperty("current")), shown.Current);
            }
            Number("max HP", N(output.GetProperty("maxHp")), values.MaxHp);
            Number("max Mana", N(output.GetProperty("maxMana")), values.MaxMana);
            Number("HP", N(output.GetProperty("hp")), values.Hp);
            Number("Mana", N(output.GetProperty("mana")), values.Mana);
            Same("cap", output.GetProperty("levelCap").GetInt32(), values.LevelCap);
            Number("EXP required", N(output.GetProperty("experienceRequired")), values.ExperienceRequired);
            Same("level up enabled", output.GetProperty("levelUpEnabled").GetBoolean(), values.LevelUpEnabled);
            Same("level up alert", output.GetProperty("levelUpAlert").GetBoolean(), values.LevelUpAlert);
            var controls = output.GetProperty("controls");
            Same("raise", controls.GetProperty("raise").GetBoolean(), values.RaiseEnabled);
            Same("respec", controls.GetProperty("respec").GetBoolean(), values.RespecEnabled);
            foreach (var devControl in new[] { "attributeFields", "lower", "bonusFields", "defenseBonusFields", "purse", "devOnly" })
                Same(devControl, controls.GetProperty(devControl).GetBoolean(), values.DevModeControls);
            // The EXP card's texts and bar from the values (the sheet's screen writes them, GM2 step 7).
            var texts = output.GetProperty("texts");
            var cap = values.LevelCap;
            Same("cap text", texts.GetProperty("cap").GetString(), $"CAP {cap}");
            Same("threshold", texts.GetProperty("threshold").GetString(), values.AtLevelCap ? "MAX LEVEL" : $"/ {SheetCases.FormatValue(values.ExperienceRequired)} EXP");
            Same("helper", texts.GetProperty("helper").GetString(), values.AtLevelCap ? $"Level cap reached · {cap}"
                : dev ? $"Dev-mode level up enabled · {SheetCases.FormatValue(values.ExperienceRequired)} EXP target"
                : $"{SheetCases.FormatValue(values.ExperienceRemaining)} EXP to reach Level {SheetCases.FormatValue(sheet.Level + 1)}");
            Same("bar", texts.GetProperty("barWidth").GetString(), values.AtLevelCap ? "100%" : JsString(values.ExperiencePercent) + "%");
            Same("bar maximum", texts.GetProperty("valueMax").GetString(), values.AtLevelCap ? "1" : JsString(values.ExperienceRequired));
            Same("bar value", texts.GetProperty("valueNow").GetString(), values.AtLevelCap ? "1" : JsString(Math.Min(sheet.Experience, values.ExperienceRequired)));
            if (problems.Count > 0)
                wrong.Add(input.GetRawText() + "\n  " + string.Join("\n  ", problems));
        }
        Assert.True(wrong.Count == 0, $"{wrong.Count} of {cases.Count} cases differ:\n" + string.Join("\n", wrong.Take(20)));
        // Every race of the data, crafted races and every kind of hand-made totals are among the cases.
        var races = cases.Select(c => c.Input.GetProperty("sheet").GetProperty("race").GetString()).ToHashSet();
        Assert.Superset(SheetCases.ShippedRaces.Select(r => (string?)r.Id).Append("stub-race").Append(null).ToHashSet(), races);
        foreach (var stub in new[] { "race", "artifacts", "classes", "equipment", "battle" })
            Assert.Contains(cases, c => c.Input.TryGetProperty("stubs", out var s) && s.TryGetProperty(stub, out _));
    }

    /// <summary>A case's action done by the port's rule.</summary>
    static (ActorSheet Sheet, RaceRefusal? Refused) Act(ActorSheet sheet, SheetContext context, JsonElement action, bool dev)
    {
        string Text(string name) => action.GetProperty(name).GetString()!;
        ActorResource Resource() => Enum.Parse<ActorResource>(Text("resource"), ignoreCase: true);
        double Value() => N(action.GetProperty("value"));
        return Text("kind") switch
        {
            "spend" => (ActorRules.Raise(sheet, context, Text("attribute"), dev), null),
            "lower" => (ActorRules.Lower(sheet, context, Text("attribute"), dev), null),
            "respec" => (ActorRules.Respec(sheet, context), null),
            "levelUp" => (ActorRules.LevelUp(sheet, context, dev), null),
            "changeResource" => (ActorRules.ChangeResource(sheet, context, Resource(), action.GetProperty("amount").GetInt32()), null),
            "restoreResource" => (ActorRules.RestoreResource(sheet, context, Resource()), null),
            "restoreBoth" => (ActorRules.RestoreBoth(sheet, context), null),
            "setAttribute" => (ActorRules.SetBaseAttribute(sheet, context, Text("attribute"), Value(), dev), null),
            "setBonus" => (ActorRules.SetBonus(sheet, context, Text("attribute"), Value(), dev), null),
            "setDefenseBonus" => (ActorRules.SetDefenseBonus(sheet, context, Text("defense"), Value(), dev), null),
            "setPurse" => (ActorRules.SetPurse(sheet, Enum.Parse<Coin>(Text("currency"), ignoreCase: true), Value(), dev), null),
            "addExperience" => (ActorRules.AddExperience(sheet, context, Value(), dev), null),
            "chooseRace" => ActorRules.ChooseRace(sheet, context, Text("race"), dev) is var choice ? (choice.Sheet, choice.Refused) : default,
            var kind => throw new InvalidOperationException("no action " + kind),
        };
    }

    [Fact]
    public void SheetActionsAreTheOldGames()
    {
        var cases = OldGameReference.Load("sheet-actions").Cases;
        Assert.True(cases.Count >= 450, $"{cases.Count} cases");
        var wrong = new List<string>();
        var kinds = new Dictionary<string, int>();
        foreach (var (input, output) in cases)
        {
            var sheet = SheetCases.Sheet(input.GetProperty("sheet"));
            var dev = SheetCases.DevMode(input);
            var context = SheetCases.Context(input);
            var action = input.GetProperty("action");
            var kind = action.GetProperty("kind").GetString()!;
            kinds[kind] = kinds.GetValueOrDefault(kind) + 1;
            var settled = ActorRules.Settle(sheet, context);
            var problems = new List<string>();
            if (settled != SheetCases.Sheet(output.GetProperty("settled")))
                problems.Add($"settled: {output.GetProperty("settled").GetRawText()}\n    port {settled}");
            var (after, refused) = Act(settled, context, action, dev);
            if (after != SheetCases.Sheet(output.GetProperty("after")))
                problems.Add($"after: {output.GetProperty("after").GetRawText()}\n    port {after}");
            if (output.TryGetProperty("status", out var status))
            {
                var expected = status.GetProperty("text").GetString() switch
                {
                    "Enable Dev Mode to select a non-playable race" => RaceRefusal.NotPlayable,
                    "A chosen race can only be changed in Dev Mode" => RaceRefusal.Locked,
                    var text when text!.EndsWith(" selected", StringComparison.Ordinal) => (RaceRefusal?)null,
                    var text => throw new InvalidOperationException("a status the test doesn't know: " + text),
                };
                if (expected != refused)
                    problems.Add($"refused: {expected?.ToString() ?? "no"}, port {refused?.ToString() ?? "no"}");
            }
            if (problems.Count > 0)
                wrong.Add(input.GetRawText() + "\n  " + string.Join("\n  ", problems));
        }
        Assert.True(wrong.Count == 0, $"{wrong.Count} of {cases.Count} cases differ:\n" + string.Join("\n", wrong.Take(15)));
        // Every action of the module is checked on cases of its own.
        Assert.Equal(["addExperience", "changeResource", "chooseRace", "levelUp", "lower", "respec", "restoreBoth", "restoreResource",
            "setAttribute", "setBonus", "setDefenseBonus", "setPurse", "spend"], kinds.Keys.Order(StringComparer.Ordinal));
    }
}
