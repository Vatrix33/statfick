namespace Statventure.Game.Data.Actors;

/// <summary>
/// The levelling curves (the old <c>CombatRules</c>, <c>old-game/scripts/combat-engine.js</c> 89–108, the user's
/// decision of 2026-11-03) and the level cap (<c>LimitlessRules.cap</c>, <c>limitless-engine.js</c> 10, 29). Checked
/// against the old modules in Node for every level 1–200 (<c>Reference/old-game/combat-rules-levels.json</c>,
/// <c>limitless-cap.json</c>).
/// </summary>
public static class Levelling
{
    /// <summary>The cap without Limitless (<c>BASE_CAP</c>), and the last level that gives a class point.</summary>
    public const int BaseCap = 100;

    /// <summary>The cap with Limitless (<c>LIMITLESS_CAP</c>, GM15).</summary>
    public const int LimitlessCap = 200;

    /// <summary>The free points a level up gives (<c>POINTS_PER_LEVEL</c>).</summary>
    public const int PointsPerLevel = 5;

    static readonly int[] EarlyVictories = [3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6];

    /// <summary>The level cap: 100, or 200 while Limitless is on.</summary>
    public static int Cap(bool limitless) => limitless ? LimitlessCap : BaseCap;

    /// <summary>
    /// Victories over enemies of one's own level a level up takes: 3–6 to Level 11, then <c>round(7 + 17 t^1.5)</c> with
    /// <c>t = min(1, (level − 12) / 87)</c>, 24 from Level 99. Levels below 1 count as 1.
    /// </summary>
    public static int SameLevelVictories(int level)
    {
        var enemyLevel = Math.Max(1, level);
        if (enemyLevel <= EarlyVictories.Length)
            return EarlyVictories[enemyLevel - 1];
        var t = Math.Min(1, (enemyLevel - 12) / 87.0);
        // Math.Pow is the C library's pow, V8's too on Linux; the rounded result is the same for any pow within an ulp
        // (no level's value lies near a half: LevellingTests).
        return (int)JsNumbers.Round(7 + 17 * Math.Pow(t, 1.5));
    }

    /// <summary>An enemy's EXP: <c>ceil((level + 1) × 100 / share)</c>, the share 5 at Level 1, 6 at 2, then 8 rising by one every ten levels to 12.</summary>
    public static double EnemyExperience(int level)
    {
        var enemyLevel = Math.Max(1, level);
        var share = enemyLevel == 1 ? 5 : enemyLevel == 2 ? 6 : Math.Min(12, 8 + (enemyLevel - 3) / 10);
        return Math.Ceiling((enemyLevel + 1.0) * 100 / share);
    }

    /// <summary>The EXP a level needs: an enemy of that level's EXP times the victories it takes.</summary>
    public static double ExperienceRequired(int level) => EnemyExperience(level) * SameLevelVictories(level);
}
