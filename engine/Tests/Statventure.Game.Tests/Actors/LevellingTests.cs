using Statventure.Game.Data.Actors;

namespace Statventure.Game.Tests;

/// <summary>
/// The levelling curves and the cap (<see cref="Levelling"/>) against the old game's pure modules in Node
/// (Reference/old-game/combat-rules-levels.json, limitless-cap.json), and the old check that guards them,
/// <c>runLevellingCurveChecks</c> (<c>old-game/test-client/tests/small-changes-browser.js</c> 102–118).
/// </summary>
public class LevellingTests
{
    [Fact]
    public void TheCurvesAreCombatRulesAtEveryLevel()
    {
        var reference = OldGameReference.Load("combat-rules-levels");
        Assert.True(reference.Cases.Count >= 200);
        var wrong = new List<string>();
        foreach (var (input, output) in reference.Cases)
        {
            var level = input.GetProperty("level").GetInt32();
            (string, double, double)[] checks =
            [
                ("experienceRequired", OldGameReference.Number(output.GetProperty("experienceRequired")), Levelling.ExperienceRequired(level)),
                ("sameLevelVictories", OldGameReference.Number(output.GetProperty("sameLevelVictories")), Levelling.SameLevelVictories(level)),
                ("enemyExperience", OldGameReference.Number(output.GetProperty("enemyExperience")), Levelling.EnemyExperience(level)),
            ];
            wrong.AddRange(checks.Where(c => !OldGameReference.Same(c.Item2, c.Item3)).Select(c => $"level {level} {c.Item1}: {c.Item2}, port {c.Item3}"));
        }
        Assert.True(wrong.Count == 0, string.Join("\n", wrong));
    }

    [Fact]
    public void TheCapIsLimitlessRules()
    {
        foreach (var (input, output) in OldGameReference.Load("limitless-cap").Cases)
        {
            var active = input.GetProperty("active").GetBoolean();
            Assert.Equal(output.GetProperty("cap").GetInt32(), Levelling.Cap(active));
            Assert.Equal(output.GetProperty("cap").GetInt32(), ActorRules.LevelCap(SheetContext.Plain with { Limitless = active }));
            Assert.Equal(output.GetProperty("baseCap").GetInt32(), Levelling.BaseCap);
            Assert.Equal(output.GetProperty("limitlessCap").GetInt32(), Levelling.LimitlessCap);
            Assert.Equal(output.GetProperty("pointsPerLevel").GetInt32(), Levelling.PointsPerLevel);
        }
    }

    [Fact]
    public void NoLevelsCurveLiesNearAHalf()
    {
        // The victories round 7 + 17 t^1.5 (V8's pow is the C library's; .NET's on Windows another): each level's value is
        // far from a half, so any pow within a few ulps rounds it the same.
        for (var level = 12; level <= 99; level++)
        {
            var t = Math.Min(1, (level - 12) / 87.0);
            var value = 7 + 17 * Math.Pow(t, 1.5);
            Assert.True(Math.Abs(value - Math.Floor(value) - 0.5) > 1e-9, $"level {level}: {value}");
        }
    }

    [Fact]
    public void RunLevellingCurveChecks()
    {
        // small-changes-browser.js 102–118, each expectation as the old check has it.
        var kills = 0;
        var reach = new Dictionary<int, int>();
        for (var level = 1; level < 100; level++)
        {
            Assert.Equal(Levelling.EnemyExperience(level) * Levelling.SameLevelVictories(level), Levelling.ExperienceRequired(level));
            kills += Levelling.SameLevelVictories(level);
            reach[level + 1] = kills;
        }
        Assert.Equal((51, 186, 388, 1265), (reach[12], reach[30], reach[50], reach[100]));
        Assert.Equal("3,4,6,7,9,12,17,24", string.Join(",", new[] { 1, 5, 11, 12, 30, 50, 75, 99 }.Select(Levelling.SameLevelVictories)));
        for (var level = 12; level < 99; level++)
            Assert.True(Levelling.SameLevelVictories(level + 1) >= Levelling.SameLevelVictories(level), "later levels never take fewer victories");
        Assert.Equal("40,150,163,310,425,834", string.Join(",", new[] { 1, 11, 12, 30, 50, 99 }.Select(l => Levelling.EnemyExperience(l))));
        Assert.Equal("120,1141,5100,20016", string.Join(",", new[] { 1, 12, 50, 99 }.Select(l => Levelling.ExperienceRequired(l))));
    }

    [Fact]
    public void JavaScriptsRoundTakesHalvesUp()
    {
        Assert.Equal((3.0, -2.0, 1.0, 0.0, 3.0, -3.0), (JsNumbers.Round(2.5), JsNumbers.Round(-2.5), JsNumbers.Round(0.5), JsNumbers.Round(-0.5), JsNumbers.Round(2.6), JsNumbers.Round(-2.6)));
        Assert.Equal((2.0, -3.0), (JsNumbers.Round(2.4999999999999996), JsNumbers.Round(-2.5000000000000004)));
        Assert.Equal((7.0, 0.0, 0.0), (JsNumbers.OrZero(7), JsNumbers.OrZero(double.NaN), JsNumbers.OrZero(-0.0)));
        Assert.True(double.IsPositive(JsNumbers.OrZero(-0.0)));
        Assert.Equal((int.MaxValue, int.MinValue, 0, 12), (JsNumbers.ToInt(1e20), JsNumbers.ToInt(-1e20), JsNumbers.ToInt(double.NaN), JsNumbers.ToInt(12)));
    }
}
