using System.Numerics;
using System.Security.Cryptography;

namespace Statventure.Game.Random;

/// <summary>
/// The randomness every gameplay roll draws from (GM1-5, GM1-P11; Docs/Game/Foundation.md, "The randomness service"):
/// <see cref="Next()"/> as the old game's <c>Math.random()</c>, so a port reads like the old line
/// (<c>Math.floor(Math.random() * n)</c> is <c>random.Next(n)</c>, or <c>(int)Math.Floor(random.Next() * n)</c>).
/// </summary>
public interface IGameRandom
{
    /// <summary>A draw in [0, 1).</summary>
    double Next();

    /// <summary>A whole number in [0, <paramref name="n"/>): <c>Math.floor(Math.random() * n)</c>, from one draw; <paramref name="n"/> at least 1.</summary>
    int Next(int n);
}

/// <summary>
/// The run's randomness (GM1-P11): <c>xoshiro256**</c> 1.0 (Blackman and Vigna), its 256-bit state the first four outputs
/// of SplitMix64 from one 64-bit <see cref="Seed"/> (as the generator's authors suggest for a 64-bit seed), each
/// <see cref="Next()"/> one 64-bit output's top 53 bits as a double in [0, 1) (<c>(x &gt;&gt; 11) · 2⁻⁵³</c>, the
/// authors' conversion). Its period is 2²⁵⁶ − 1. Seeded once a run from the system (<see cref="FromSystem"/>, the seed
/// logged); the state is never saved, so every load draws afresh (GM1-5). Not thread-safe: the game's thread draws.
/// </summary>
public sealed class GameRandom : IGameRandom
{
    ulong _s0, _s1, _s2, _s3;

    /// <summary>The generator seeded with <paramref name="seed"/> (any value: SplitMix64 never gives an all-zero state).</summary>
    public GameRandom(ulong seed)
    {
        Seed = seed;
        var mix = seed;
        _s0 = SplitMix64(ref mix);
        _s1 = SplitMix64(ref mix);
        _s2 = SplitMix64(ref mix);
        _s3 = SplitMix64(ref mix);
    }

    /// <summary>Tests: the generator in the state <paramref name="s0"/>–<paramref name="s3"/> (the reference implementation's <c>s[0]</c>–<c>s[3]</c>), as if seeded with 0.</summary>
    internal static GameRandom FromState(ulong s0, ulong s1, ulong s2, ulong s3) => new(0) { _s0 = s0, _s1 = s1, _s2 = s2, _s3 = s3 };

    /// <summary>The seed it was made from (the application logs it at the start).</summary>
    public ulong Seed { get; }

    /// <summary>The run's randomness, seeded from the system's cryptographic source (any of the 2⁶⁴ seeds).</summary>
    public static GameRandom FromSystem()
    {
        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        RandomNumberGenerator.Fill(bytes);
        return new GameRandom(BitConverter.ToUInt64(bytes));
    }

    /// <summary>SplitMix64 (Vigna's <c>splitmix64.c</c>): the state advanced by the golden gamma, then mixed.</summary>
    internal static ulong SplitMix64(ref ulong state)
    {
        var z = state += 0x9E3779B97F4A7C15;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EB;
        return z ^ (z >> 31);
    }

    /// <summary>The next 64-bit output of <c>xoshiro256**</c> (Vigna's <c>xoshiro256starstar.c</c>, <c>next()</c>).</summary>
    internal ulong NextBits()
    {
        var result = BitOperations.RotateLeft(_s1 * 5, 7) * 9;
        var t = _s1 << 17;
        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = BitOperations.RotateLeft(_s3, 45);
        return result;
    }

    /// <summary>A draw in [0, 1): the top 53 bits of one output, times 2⁻⁵³.</summary>
    public double Next() => (NextBits() >> 11) * (1.0 / (1UL << 53));

    public int Next(int n) => Draws.Whole(this, n);
}

/// <summary>
/// A scripted source for tests: hands the given draws in order, exactly as given (so a rule can be replayed draw for draw
/// against an old check's stubbed <c>Math.random</c>), and fails when they run out.
/// </summary>
public sealed class ScriptedRandom(IEnumerable<double> draws) : IGameRandom
{
    readonly Queue<double> _draws = new(draws);

    /// <summary>How many draws are left (a test can check every one was used).</summary>
    public int Remaining => _draws.Count;

    /// <exception cref="InvalidOperationException">The scripted draws ran out.</exception>
    public double Next() => _draws.TryDequeue(out var draw) ? draw : throw new InvalidOperationException("the scripted draws ran out");

    public int Next(int n) => Draws.Whole(this, n);
}

/// <summary>What the sources share.</summary>
static class Draws
{
    /// <summary><c>Math.floor(Math.random() * n)</c> from one draw of <paramref name="source"/>; an <paramref name="n"/> below 1 is refused before any draw.</summary>
    public static int Whole(IGameRandom source, int n)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(n, 1);
        return (int)Math.Floor(source.Next() * n);
    }
}
