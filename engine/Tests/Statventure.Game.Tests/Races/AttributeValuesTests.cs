using Statventure.Game.Data.Effects;

namespace Statventure.Game.Tests;

/// <summary>
/// The attributes' and defences' keys and values (<see cref="AttributeKeys"/>, <see cref="AttributeValues"/>,
/// <see cref="DefenseValues"/>): the old game's keys in its order (<c>statDefinitions</c>, as the oracle's results list
/// them), each key reading and setting its own value only, any other key refused.
/// </summary>
public class AttributeValuesTests
{
    [Fact]
    public void TheKeysAreTheOldGamesInItsOrder()
    {
        var first = RaceOracle.Fold.GetProperty("cases").EnumerateArray().First().GetProperty("output");
        Assert.Equal(AttributeKeys.All, first.GetProperty("statModifier").EnumerateObject().Select(p => p.Name));
        Assert.Equal(AttributeKeys.All, first.GetProperty("totals").GetProperty("statPermanentPercent").EnumerateObject().Select(p => p.Name));
        Assert.Equal(DefenseKeys.All, first.GetProperty("totals").GetProperty("defenseCap").EnumerateObject().Select(p => p.Name));
    }

    [Fact]
    public void EachKeyIsItsOwnValue()
    {
        var values = new AttributeValues(1, 2, 3, 4, 5, 6, 7, 8);
        Assert.Equal([1.0, 2, 3, 4, 5, 6, 7, 8], AttributeKeys.All.Select(k => values[k]));
        foreach (var (key, i) in AttributeKeys.All.Select((k, i) => (k, i)))
        {
            var set = values.With(key, 100);
            Assert.Equal(AttributeKeys.All.Select((k, j) => j == i ? 100.0 : j + 1), AttributeKeys.All.Select(k => set[k]));
            Assert.True(AttributeValues.Has(key));
        }
        Assert.False(AttributeValues.Has("Strength"));
        Assert.Throws<ArgumentException>(() => values["armor"]);
        Assert.Throws<ArgumentException>(() => values.With("armor", 1));
        var defences = new DefenseValues(1, 2);
        Assert.Equal((1.0, 2.0), (defences["armor"], defences["evasion"]));
        Assert.Equal((5.0, 2.0), (defences.With("armor", 5).Armor, defences.With("armor", 5).Evasion));
        Assert.Equal((1.0, 5.0), (defences.With("evasion", 5).Armor, defences.With("evasion", 5).Evasion));
        Assert.True(DefenseValues.Has("armor") && DefenseValues.Has("evasion") && !DefenseValues.Has("Armor") && !DefenseValues.Has("luck"));
        Assert.Throws<ArgumentException>(() => defences["luck"]);
        Assert.Throws<ArgumentException>(() => defences.With("luck", 1));
    }
}
