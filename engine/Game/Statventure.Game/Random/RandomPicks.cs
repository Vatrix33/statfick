namespace Statventure.Game.Random;

/// <summary>
/// The picks the old code repeats, on any <see cref="IGameRandom"/> (GM2-P15, the first helpers, made with the character
/// maker's random button; Docs/Game/Hero.md, "How it works", step 8). Each reads as the old line it ports, one draw a
/// pick: the uniform pick is <c>list[Math.floor(Math.random() * list.length)]</c>, the weighted pick the old engines'
/// shared form (<c>weightedIndex</c> in <c>forge-engine.js</c> 305–310, <c>weightedPick</c> in <c>encounter-engine.js</c>
/// 929–938, <c>pickWeighted</c> in <c>alchemy-engine.js</c> 91–97): the roll a draw times the total of the weights (a
/// weight below 0 counting as 0), each weight taken off in turn, the first that takes the roll below 0 picked, the last
/// when none does.
/// </summary>
public static class RandomPicks
{
    /// <summary>One of <paramref name="items"/>, each as likely: <c>items[Math.floor(Math.random() * items.length)]</c>, one draw.</summary>
    /// <exception cref="ArgumentException"><paramref name="items"/> is empty (nothing is drawn).</exception>
    public static T Pick<T>(this IGameRandom random, IReadOnlyList<T> items)
    {
        if (items.Count == 0)
            throw new ArgumentException("nothing to pick from", nameof(items));
        return items[random.Next(items.Count)];
    }

    /// <summary>
    /// The index of one of <paramref name="weights"/>, each as likely as its weight (a weight below 0 or not a number
    /// counting as 0), one draw: the old engines' <c>weightedIndex</c>. With no weight above 0 the last index.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="weights"/> is empty (nothing is drawn).</exception>
    public static int WeightedIndex(this IGameRandom random, IReadOnlyList<double> weights)
    {
        if (weights.Count == 0)
            throw new ArgumentException("nothing to pick from", nameof(weights));
        var total = 0.0;
        foreach (var weight in weights)
            total += Weight(weight);
        var roll = random.Next() * total;
        for (var index = 0; index < weights.Count; index++)
        {
            roll -= Weight(weights[index]);
            if (roll < 0)
                return index;
        }
        return weights.Count - 1;
    }

    /// <summary>One of <paramref name="items"/>, each as likely as <paramref name="weightOf"/> says (<see cref="WeightedIndex"/>), one draw.</summary>
    /// <exception cref="ArgumentException"><paramref name="items"/> is empty (nothing is drawn).</exception>
    public static T PickWeighted<T>(this IGameRandom random, IReadOnlyList<T> items, Func<T, double> weightOf) =>
        items[random.WeightedIndex([.. items.Select(weightOf)])];

    /// <summary>A weight as the old code counts it: <c>Math.max(0, Number(weight) || 0)</c>.</summary>
    static double Weight(double weight) => double.IsNaN(weight) || weight < 0 ? 0 : weight;
}
