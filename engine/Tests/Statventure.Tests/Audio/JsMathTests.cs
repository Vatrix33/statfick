using System.Text.Json;
using Statventure.Audio;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Audio;

/// <summary>
/// The engine's pow is this system's glibc pow (Reference/libm.json, from
/// Tools/Scripts/make-native-references.sh), its exp and JavaScript <c>**</c> are Electron's V8's
/// (Reference/v8-math.json, from Tools/Scripts/make-v8-math-reference.sh): hashes of every result
/// over argument families drawn from mulberry32 the same way in C, JavaScript and here
/// (<see cref="MathArguments"/>), the first results of each listed for diagnosis.
/// </summary>
public class JsMathTests
{
    static JsonElement Reference(string file, string function)
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference", file);
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty(function).Clone();
    }

    static void Check(JsonElement reference, string family, IEnumerable<double> results)
    {
        var expected = reference.GetProperty(family);
        var hash = MathArguments.Offset;
        var first = new List<string>();
        foreach (var result in results)
        {
            hash = MathArguments.Mix(hash, result);
            if (first.Count < 8)
                first.Add(MathArguments.Hex(result));
        }
        Assert.Equal(string.Join(",", expected.GetProperty("first"u8).EnumerateArray().Select(e => e.GetString())), string.Join(",", first));
        Assert.Equal(expected.GetProperty("hash"u8).GetString(), hash.ToString("x16"));
    }

    public static TheoryData<string> PowFamilies => new(MathArguments.PowFamilies);
    public static TheoryData<string> ExpFamilies => new(MathArguments.ExpFamilies);

    [Theory]
    [MemberData(nameof(PowFamilies))]
    public void PowIsGlibcs(string family) =>
        Check(Reference("libm.json", "pow"), family, MathArguments.Pow(family).Select(a => Libm.Pow(a.X, a.Y)));

    [Theory]
    [MemberData(nameof(PowFamilies))]
    public void JavaScriptPowIsV8s(string family) =>
        Check(Reference("v8-math.json", "pow"), family, MathArguments.Pow(family).Select(a => JsMath.Pow(a.X, a.Y)));

    [Theory]
    [MemberData(nameof(ExpFamilies))]
    public void ExpIsV8s(string family) =>
        Check(Reference("v8-math.json", "exp"), family, MathArguments.Exp(family).Select(JsMath.Exp));

    /// <summary>
    /// The double-double and 128-bit passes, which the fast pass rarely leaves to them, round to the
    /// same (correctly rounded) result on their own wherever the result is normal and finite.
    /// </summary>
    [Fact]
    public void ExpsSlowPassesRoundTheSame()
    {
        var differences = new List<string>();
        foreach (var x in MathArguments.Exp("range").Take(1 << 18))
        {
            if (x < -708 || x > 709.7)
                continue;
            var (doubleDouble, dyadic) = LlvmLibc.SlowPasses(x);
            var exp = LlvmLibc.Exp(x);
            if (doubleDouble != exp || dyadic != exp)
                differences.Add($"exp({x:R}) = {MathArguments.Hex(exp)}, double-double {MathArguments.Hex(doubleDouble)}, dyadic {MathArguments.Hex(dyadic)}");
        }
        Assert.True(differences.Count == 0, $"{differences.Count} differ: {string.Join("; ", differences.Take(5))}");
    }

    [Theory]
    [InlineData(2.5, 3)]
    [InlineData(-2.5, -2)]
    [InlineData(0.49999999999999994, 0)]
    [InlineData(-0.5, -0.0)]
    [InlineData(1.5, 2)]
    public void RoundIsJavaScripts(double x, double expected) =>
        Assert.Equal(BitConverter.DoubleToUInt64Bits(expected), BitConverter.DoubleToUInt64Bits(JsMath.Round(x)));
}

/// <summary>
/// Argument families for the math references, drawn from mulberry32 (the composer's generator) in
/// the same order in Tools/NativeReference/libm.c and Tools/AudioReference/v8-math.cjs.
/// </summary>
static class MathArguments
{
    public const ulong Offset = 0xcbf29ce484222325;
    public const int Count = 1 << 22;

    public static readonly string[] PowFamilies = ["random", "nearOne", "negative", "special", "composer"];
    public static readonly string[] ExpFamilies = ["range", "bits", "small", "subnormal", "rooms"];

    public static ulong Mix(ulong hash, double result)
    {
        var bits = double.IsNaN(result) ? 0x7ff8000000000000 : BitConverter.DoubleToUInt64Bits(result);
        for (var i = 0; i < 8; i++)
        {
            hash ^= (bits >> (8 * i)) & 0xff;
            hash *= 0x100000001b3;
        }
        return hash;
    }

    public static string Hex(double result) =>
        (double.IsNaN(result) ? 0x7ff8000000000000 : BitConverter.DoubleToUInt64Bits(result)).ToString("x16");

    sealed class Mulberry32(uint seed)
    {
        uint _state = seed;

        public uint Next()
        {
            _state += 0x6D2B79F5;
            var t = _state;
            t = (t ^ (t >> 15)) * (t | 1);
            t ^= t + (t ^ (t >> 7)) * (t | 61);
            return t ^ (t >> 14);
        }

        public double Uniform() => Next() / 4294967296.0;

        public double Bits()
        {
            ulong hi = Next();
            ulong lo = Next();
            return BitConverter.UInt64BitsToDouble(hi << 32 | lo);
        }
    }

    static readonly double[] SpecialX =
    [
        0.0, -0.0, double.PositiveInfinity, double.NegativeInfinity, double.NaN, 1, -1, 2, -2, 0.5,
        double.Epsilon, -double.Epsilon, 2.2250738585072014e-308, double.MaxValue, -double.MaxValue,
        BitConverter.UInt64BitsToDouble(0x3fefffffffffffff), BitConverter.UInt64BitsToDouble(0x3ff0000000000001), 10, -10, 3,
    ];

    static readonly double[] SpecialY =
    [
        0.0, -0.0, double.PositiveInfinity, double.NegativeInfinity, double.NaN, 0.5, -0.5, 1, -1, 2, -2, 3, -3,
        BitConverter.UInt64BitsToDouble(0x3b90000000000000), BitConverter.UInt64BitsToDouble(0xbb90000000000000),
        BitConverter.UInt64BitsToDouble(0x4450000000000000), BitConverter.UInt64BitsToDouble(0xc450000000000000),
        1075, -1075, 0.35, 1e-300,
    ];

    static double Special(Mulberry32 random, double[] specials)
    {
        var index = random.Next() % (uint)(specials.Length + 4);
        return index < specials.Length ? specials[index] : random.Bits();
    }

    public static IEnumerable<(double X, double Y)> Pow(string family)
    {
        var random = new Mulberry32((uint)(Array.IndexOf(PowFamilies, family) + 1) * 7919);
        for (var i = 0; i < Count; i++)
        {
            double x, y;
            switch (family)
            {
                case "random":
                {
                    ulong hi = random.Next(), lo = random.Next();
                    x = BitConverter.UInt64BitsToDouble((hi << 32 | lo) & 0x7fefffffffffffff);
                    y = (random.Uniform() * 2 - 1) * 8;
                    break;
                }
                case "nearOne":
                    x = 0.5 + random.Uniform() * 1.5;
                    y = (random.Uniform() * 2 - 1) * 2048;
                    break;
                case "negative":
                {
                    x = -(1e-3 + random.Uniform() * 1e3);
                    var coin = random.Uniform();
                    var u = random.Uniform();
                    y = coin < 0.5 ? Math.Floor(u * 81) - 40 : (u * 2 - 1) * 40;
                    break;
                }
                case "special":
                    x = Special(random, SpecialX);
                    y = Special(random, SpecialY);
                    break;
                default:
                {
                    // The composer's: 2 ** ((midi − 69) / 12), (frequency / 330) ** 0.35, Math.pow(0.001, 1 / (decay · actual)).
                    var m = (int)(random.Next() % 700) - 300;
                    switch (i % 3)
                    {
                        case 0:
                            x = 2;
                            y = (m - 69) / 12.0;
                            break;
                        case 1:
                            x = (20 + random.Uniform() * 20000) / 330;
                            y = 0.35;
                            break;
                        default:
                        {
                            var d = 0.3 + random.Uniform() * 4.7;
                            var a = 20 + random.Uniform() * 20000;
                            x = 0.001;
                            y = 1 / (d * a);
                            break;
                        }
                    }
                    break;
                }
            }
            yield return (x, y);
        }
    }

    static readonly double[] RoomSeconds = [1.4, 2.4, 1.8, 1.6, 4.2, 3.4, 4.8];

    public static IEnumerable<double> Exp(string family)
    {
        if (family == "rooms")
        {
            // Every Math.exp the composer's room impulse responses call, at 44.1 and 48 kHz.
            foreach (var rate in new[] { 44100.0, 48000.0 })
                foreach (var seconds in RoomSeconds)
                {
                    var length = Math.Ceiling(seconds * rate);
                    for (var i = 0; i < length; i++)
                    {
                        var t = i / rate;
                        var progress = t / seconds;
                        yield return -6.9 * progress;
                    }
                }
            yield break;
        }
        var random = new Mulberry32((uint)(Array.IndexOf(ExpFamilies, family) + 1) * 104729);
        for (var i = 0; i < Count; i++)
        {
            switch (family)
            {
                case "range":
                    yield return -745 + random.Uniform() * 1455;
                    break;
                case "bits":
                    yield return random.Bits();
                    break;
                case "small":
                {
                    var e = random.Next() % 61;
                    var x = random.Uniform() * 2 - 1;
                    for (var k = 0; k < e; k++)
                        x *= 0.5;
                    yield return x;
                    break;
                }
                default:
                    yield return -745.2 + random.Uniform() * 37;
                    break;
            }
        }
    }
}
