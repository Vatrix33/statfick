using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;

namespace Statventure.Game.Tests;

/// <summary>Races the tests put in mods and scenes (the game's own kind in place of GM1's stand-in item, GM1-P3).</summary>
static class TestRaces
{
    /// <summary>A playable race named <paramref name="name"/> with a passive of one effect: one its checks pass.</summary>
    public static Race Good(string name) => new()
    {
        Name = name,
        StatModifiers = new RaceModifiers { Strength = 1, Vitality = 1, Luck = -2 },
        Passive = new RacePassive
        {
            Id = "ember-blood",
            Name = "Ember Blood",
            Description = "Maximum HP +5%.",
            Effects = [new MaxHpPercent { Amount = 5 }],
        },
        Accent = "#9a6a3f",
        Order = 290,
    };

    /// <summary><see cref="Good"/> with a strength modifier that isn't a number: refused as the old game refused it ("strength modifier must be a number").</summary>
    public static Race NotANumber(string name) => Good(name) with { StatModifiers = new RaceModifiers { Strength = double.NaN } };

    /// <summary><see cref="Good"/> with an empty name: refused ("name must be a nonempty string").</summary>
    public static Race Nameless() => Good("x") with { Name = " " };
}
