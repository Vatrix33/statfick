using Statventure.Game.Random;

namespace Statventure.Game.Tests;

/// <summary>
/// The randomness service (GM1-5, GM1-P11; Docs/Game/Foundation.md, "The randomness service"): SplitMix64 and
/// <c>xoshiro256**</c> against their published reference outputs (rand_xoshiro's reference tests, "produced with the
/// reference implementation", Vigna's <c>splitmix64.c</c> and <c>xoshiro256starstar.c</c>; both saved in
/// <c>~/.cache/stat-fick-build/reference-src/xoshiro/</c>), the seeding through SplitMix64, a draw's top 53 bits, the
/// whole numbers, seeded draws repeated, the system's seed, and the scripted source.
/// </summary>
public class GameRandomTests
{
    /// <summary>rand_xoshiro's <c>splitmix64.rs</c>, <c>reference()</c>: SplitMix64 from 1477776061723855037, its first 50 outputs.</summary>
    static readonly ulong[] SplitMix64Reference =
    [
        1985237415132408290, 2979275885539914483, 13511426838097143398, 8488337342461049707, 15141737807933549159,
        17093170987380407015, 16389528042912955399, 13177319091862933652, 10841969400225389492, 17094824097954834098,
        3336622647361835228, 9678412372263018368, 11111587619974030187, 7882215801036322410, 5709234165213761869,
        7799681907651786826, 4616320717312661886, 4251077652075509767, 7836757050122171900, 5054003328188417616,
        12919285918354108358, 16477564761813870717, 5124667218451240549, 18099554314556827626, 7603784838804469118,
        6358551455431362471, 3037176434532249502, 3217550417701719149, 9958699920490216947, 5965803675992506258,
        12000828378049868312, 12720568162811471118, 245696019213873792, 8351371993958923852, 14378754021282935786,
        5655432093647472106, 5508031680350692005, 8515198786865082103, 6287793597487164412, 14963046237722101617,
        3630795823534910476, 8422285279403485710, 10554287778700714153, 10871906555720704584, 8659066966120258468,
        9420238805069527062, 10338115333623340156, 13514802760105037173, 14635952304031724449, 15419692541594102413,
    ];

    /// <summary>rand_xoshiro's <c>xoshiro256starstar.rs</c>, <c>reference()</c>: the state 1, 2, 3, 4, its first 10 outputs.</summary>
    static readonly ulong[] Xoshiro256StarStarReference =
    [
        11520, 0, 1509978240, 1215971899390074240, 1216172134540287360, 607988272756665600, 16172922978634559625,
        8476171486693032832, 10595114339597558777, 2904607092377533576,
    ];

    [Fact]
    public void SplitMix64GivesItsPublishedReferenceOutputs()
    {
        var state = 1477776061723855037UL;
        Assert.Equal(SplitMix64Reference, SplitMix64Reference.Select(_ => GameRandom.SplitMix64(ref state)));
    }

    [Fact]
    public void Xoshiro256StarStarGivesItsPublishedReferenceOutputs()
    {
        var random = GameRandom.FromState(1, 2, 3, 4);
        Assert.Equal(Xoshiro256StarStarReference, Xoshiro256StarStarReference.Select(_ => random.NextBits()));
    }

    [Fact]
    public void ASeedFillsTheStateWithSplitMix64sFirstFourOutputs()
    {
        // The seed of SplitMix64's published outputs: the generator's state is their first four, so its outputs are
        // xoshiro256** from that state (an independent transcription of the reference code,
        // claude-work/gm1/step5/xoshiro/reference.py, checked against both published lists, gave the same).
        var seeded = new GameRandom(1477776061723855037);
        var state = GameRandom.FromState(SplitMix64Reference[0], SplitMix64Reference[1], SplitMix64Reference[2], SplitMix64Reference[3]);
        for (var i = 0; i < 100; i++)
            Assert.Equal(state.NextBits(), seeded.NextBits());
        Assert.Equal(
            [5157112160024420127UL, 1500938876607723663, 16125041519135623862, 10010941967492962366, 14230262757846880097, 12778055244858650149],
            Draws(new GameRandom(1477776061723855037), 6));
        // Other seeds, by the same transcription: 0 (SplitMix64's first output from 0 is the well-known 0xe220a8397b1dcdaf),
        // 1, the stub's 0x5EED and the largest.
        Assert.Equal([11091344671253066420UL, 13793997310169335082, 1900383378846508768], Draws(new GameRandom(0), 3));
        Assert.Equal([12966619160104079557UL, 9600361134598540522, 10590380919521690900], Draws(new GameRandom(1), 3));
        Assert.Equal([17236385663644093300UL, 16282079530828760347, 15612578460299724346], Draws(new GameRandom(0x5EED), 3));
        Assert.Equal([10328197420357168392UL, 14156678507024973869, 9357971779955476126], Draws(new GameRandom(ulong.MaxValue), 3));
        var zero = 0UL;
        Assert.Equal(0xE220A8397B1DCDAFUL, GameRandom.SplitMix64(ref zero));
    }

    static ulong[] Draws(GameRandom random, int count) => [.. Enumerable.Range(0, count).Select(_ => random.NextBits())];

    [Fact]
    public void ADrawIsTheTop53BitsOfAnOutputInZeroToOne()
    {
        // The reference conversion (x >> 11) * 0x1.0p-53, bit for bit.
        var random = new GameRandom(0);
        Assert.Equal(0x3FE33D8BE6D96EBEUL, (ulong)BitConverter.DoubleToInt64Bits(random.Next()));
        Assert.Equal(0.7477740925472398, random.Next());
        Assert.Equal(0.10301998939503632, random.Next());
        // The state 1, 2, 3, 4 gives 11520 then 0: 11520 >> 11 is 5, so 5 · 2⁻⁵³; then exactly 0.
        var reference = GameRandom.FromState(1, 2, 3, 4);
        Assert.Equal(5 / 9007199254740992.0, reference.Next());
        Assert.Equal(0.0, reference.Next());
        // The largest output is just below 1: its low 11 bits are dropped, not rounded up.
        var sure = GameRandom.FromState(1, 2, 3, 4);
        Assert.Equal((1UL << 53) - 1, ulong.MaxValue >> 11);
        Assert.True(((1UL << 53) - 1) * (1.0 / (1UL << 53)) < 1);
        var draws = Enumerable.Range(0, 100_000).Select(_ => sure.Next()).ToList();
        Assert.All(draws, d => Assert.InRange(d, 0, Math.BitDecrement(1.0)));
        Assert.InRange(draws.Average(), 0.49, 0.51);
    }

    [Fact]
    public void AWholeNumberIsTheFloorOfADrawTimesN()
    {
        var random = new GameRandom(0);
        // 0.6012… · 10, 0.7477… · 6, 0.1030… · 100.
        Assert.Equal([6, 4, 10], new[] { random.Next(10), random.Next(6), random.Next(100) });
        Assert.Equal(0, new GameRandom(0).Next(1));
        var scripted = new ScriptedRandom([0, 0.5, 0.99999, 0.3]);
        Assert.Equal([0, 3, 5, 0], new[] { scripted.Next(6), scripted.Next(6), scripted.Next(6), scripted.Next(2) });
        // n below 1 is refused before any draw is used.
        var left = new ScriptedRandom([0.5]);
        Assert.Throws<ArgumentOutOfRangeException>(() => left.Next(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => left.Next(-3));
        Assert.Equal(1, left.Remaining);
        var seeded = new GameRandom(7);
        Assert.Throws<ArgumentOutOfRangeException>(() => seeded.Next(0));
        Assert.Equal(new GameRandom(7).Next(), seeded.Next());
    }

    [Fact]
    public void TheSameSeedGivesTheSameDraws()
    {
        var a = new GameRandom(0xC0FFEE);
        var b = new GameRandom(0xC0FFEE);
        Assert.Equal(Enumerable.Range(0, 50).Select(_ => a.Next()).ToList(), Enumerable.Range(0, 50).Select(_ => b.Next()).ToList());
        Assert.Equal(0xC0FFEEUL, a.Seed);
        Assert.NotEqual(new GameRandom(1).Next(), new GameRandom(2).Next());
    }

    [Fact]
    public void TheSystemsSeedIsReadableAndRepeatsItsDraws()
    {
        var run = GameRandom.FromSystem();
        var again = new GameRandom(run.Seed);
        Assert.Equal(Enumerable.Range(0, 20).Select(_ => again.Next()).ToList(), Enumerable.Range(0, 20).Select(_ => run.Next()).ToList());
        // Each run seeded afresh (GM1-5): eight starts, eight seeds (equal ones by chance: 2⁻⁶⁴ a pair), not the stub's 0x5EED.
        var seeds = Enumerable.Range(0, 8).Select(_ => GameRandom.FromSystem().Seed).ToList();
        Assert.Equal(8, seeds.Distinct().Count());
    }

    [Fact]
    public void TheScriptedSourceHandsItsDrawsInOrderAndFailsWhenTheyRunOut()
    {
        var scripted = new ScriptedRandom([0.25, 0.75, 0]);
        Assert.Equal(3, scripted.Remaining);
        Assert.Equal(0.25, scripted.Next());
        Assert.Equal(0.75, scripted.Next());
        Assert.Equal(1, scripted.Remaining);
        Assert.Equal(0, scripted.Next());
        Assert.Equal(0, scripted.Remaining);
        Assert.Equal("the scripted draws ran out", Assert.Throws<InvalidOperationException>(() => scripted.Next()).Message);
        Assert.Throws<InvalidOperationException>(() => scripted.Next(4));
        // As an old check's stub gives them, unchanged.
        IGameRandom source = new ScriptedRandom([0.1 + 0.2]);
        Assert.Equal(0.30000000000000004, source.Next());
    }
}
