namespace Statventure.Game.Random;

/// <summary>
/// The randomness every gameplay roll draws from (GM1-5, GM1-P11; Docs/Game/Foundation.md, "The randomness service";
/// step 5): <see cref="Next()"/> as the old game's <c>Math.random()</c>, so a port reads like the old line.
/// </summary>
public interface IGameRandom
{
    /// <summary>A draw in [0, 1).</summary>
    double Next();

    /// <summary>A whole number in [0, <paramref name="n"/>): <c>Math.floor(Math.random() * n)</c>.</summary>
    int Next(int n);
}

/// <summary>
/// The run's randomness (step 5: <c>xoshiro256**</c> seeded through SplitMix64 from <see cref="Seed"/>, <see cref="Next()"/>
/// the top 53 bits of a draw). <b>Step 2's stub:</b> .NET's generator from the seed, and <see cref="FromSystem"/> a fixed
/// seed (GM1 has no roll; the service is for GM2 on).
/// </summary>
public sealed class GameRandom : IGameRandom
{
    /// <summary>The stub's seed for <see cref="FromSystem"/>.</summary>
    public const ulong FixedSeed = 0x5EED;

    readonly System.Random _draws;

    public GameRandom(ulong seed)
    {
        Seed = seed;
        _draws = new System.Random(unchecked((int)(seed ^ (seed >> 32))));
    }

    /// <summary>The seed it was made from (logged at the start).</summary>
    public ulong Seed { get; }

    /// <summary>The run's randomness, seeded from the system (the stub: <see cref="FixedSeed"/>).</summary>
    public static GameRandom FromSystem() => new(FixedSeed);

    public double Next() => _draws.NextDouble();

    public int Next(int n) => (int)Math.Floor(Next() * n);
}

/// <summary>A scripted source for tests: hands the given draws in order and fails when it runs out.</summary>
public sealed class ScriptedRandom(IEnumerable<double> draws) : IGameRandom
{
    readonly Queue<double> _draws = new(draws);

    public double Next() => _draws.TryDequeue(out var draw) ? draw : throw new InvalidOperationException("the scripted draws ran out");

    public int Next(int n) => (int)Math.Floor(Next() * n);
}
