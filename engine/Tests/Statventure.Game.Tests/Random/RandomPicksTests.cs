using Statventure.Game.Random;

namespace Statventure.Game.Tests;

/// <summary>
/// GM2-P15's picks (GM2 step 8): the uniform pick as the old <c>list[Math.floor(Math.random() * list.length)]</c> and the
/// weighted pick as the old engines' <c>weightedIndex</c> (<c>forge-engine.js</c> 305–310; <c>encounter-engine.js</c>
/// 929–938's weight rule), each one draw, replayed draw for draw on a scripted source.
/// </summary>
public class RandomPicksTests
{
    static readonly string[] Five = ["a", "b", "c", "d", "e"];

    [Theory]
    [InlineData(0.0, "a")]
    [InlineData(0.19999999999999998, "a")]
    [InlineData(0.2, "b")]
    [InlineData(0.5, "c")]
    [InlineData(0.7999999999999999, "d")]
    [InlineData(0.8, "e")]
    [InlineData(0.9999999999999999, "e")]
    public void APickIsTheItemAtTheFloorOfTheDrawTimesTheCount(double draw, string expected)
    {
        var random = new ScriptedRandom([draw]);
        Assert.Equal(expected, random.Pick(Five));
        Assert.Equal(0, random.Remaining);
    }

    [Fact]
    public void APickFromOneItemStillTakesItsDrawAndNothingIsDrawnForNone()
    {
        var random = new ScriptedRandom([0.99, 0.5]);
        Assert.Equal("only", random.Pick(["only"]));
        Assert.Equal(1, random.Remaining);
        Assert.Throws<ArgumentException>(() => random.Pick(Array.Empty<string>()));
        Assert.Throws<ArgumentException>(() => random.WeightedIndex([]));
        Assert.Equal(1, random.Remaining);
    }

    [Theory]
    // Weights 1, 2, 3 (total 6): the roll is the draw times 6, each weight taken off in turn, the first taking it below 0.
    [InlineData(0.0, 0)]
    [InlineData(0.16666666666666666, 1)]
    [InlineData(0.16666666666666663, 0)]
    [InlineData(0.49999999999999994, 1)]
    [InlineData(0.5, 2)]
    [InlineData(0.9999999999999999, 2)]
    public void AWeightedPickTakesEachWeightOffTheRollInTurn(double draw, int expected)
    {
        var random = new ScriptedRandom([draw]);
        Assert.Equal(expected, random.WeightedIndex([1, 2, 3]));
        Assert.Equal(0, random.Remaining);
    }

    [Fact]
    public void AWeightBelowZeroOrNotANumberCountsAsZeroAndWithNoneAboveZeroTheLastIsPicked()
    {
        // -5 and NaN count as 0 (the old Math.max(0, Number(weight) || 0)): only the 4 can be picked.
        Assert.Equal(2, new ScriptedRandom([0.0]).WeightedIndex([-5, double.NaN, 4]));
        Assert.Equal(2, new ScriptedRandom([0.99]).WeightedIndex([-5, double.NaN, 4]));
        // A 0 weight is never picked while another is above 0.
        Assert.Equal(1, new ScriptedRandom([0.0]).WeightedIndex([0, 1, 0]));
        // A weight that isn't a number counts as 0 in the total too: with [1, NaN] the 1 takes every roll.
        Assert.Equal(0, new ScriptedRandom([0.5]).WeightedIndex([1, double.NaN]));
        // A weight below 0 is taken off as 0: with [-1, 1, 1] and the roll 0 the second takes it below 0.
        Assert.Equal(1, new ScriptedRandom([0.0]).WeightedIndex([-1, 1, 1]));
        // No weight above 0: the roll is 0, nothing takes it below 0, the last is picked (the old loop's fall-through).
        Assert.Equal(2, new ScriptedRandom([0.3]).WeightedIndex([0, -1, 0]));
    }

    [Fact]
    public void AWeightedPickOfItemsUsesTheirWeights()
    {
        var items = new[] { ("rare", 1.0), ("common", 9.0) };
        Assert.Equal("rare", new ScriptedRandom([0.05]).PickWeighted(items, i => i.Item2).Item1);
        Assert.Equal("common", new ScriptedRandom([0.1]).PickWeighted(items, i => i.Item2).Item1);
    }

    [Fact]
    public void ThePicksAreUniformOverManyDraws()
    {
        var random = new GameRandom(2026);
        var counts = new int[5];
        for (var i = 0; i < 50_000; i++)
            counts[Array.IndexOf(Five, random.Pick(Five))]++;
        Assert.All(counts, count => Assert.InRange(count, 9_500, 10_500));
    }
}
