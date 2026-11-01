using System.Text.Json;
using Statventure.Audio;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Audio;

/// <summary>
/// The engine's fdlibm exp, pow, sin, cos, log and expf give Blink's results bit for bit (Reference/fdlibm.json, from
/// Blink's own fdlibm built by Tools/Scripts/make-native-references.sh) on inputs like Web Audio's;
/// .NET's Math.Exp and Math.Pow don't, which is why the port exists.
/// </summary>
public class FdlibmTests
{
    static double FromHex(JsonElement e) => BitConverter.Int64BitsToDouble((long)Convert.ToUInt64(e.GetString()!, 16));

    static JsonDocument Reference()
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference", "fdlibm.json");
        return JsonDocument.Parse(File.ReadAllText(path));
    }

    [Fact]
    public void ExpIsBlinks()
    {
        using var reference = Reference();
        int cases = 0, mismatches = 0, libraryMismatches = 0;
        foreach (var c in reference.RootElement.GetProperty("exp"u8).EnumerateArray())
        {
            double x = FromHex(c[0]), expected = FromHex(c[1]);
            cases++;
            if (BitConverter.DoubleToInt64Bits(Fdlibm.Exp(x)) != BitConverter.DoubleToInt64Bits(expected))
                mismatches++;
            if (Math.Exp(x) != expected)
                libraryMismatches++;
        }
        Assert.Equal(1500, cases);
        Assert.Equal(0, mismatches);
        Assert.True(libraryMismatches > 0, "Math.Exp agrees with fdlibm everywhere here: the cases don't tell them apart");
    }

    [Fact]
    public void PowIsBlinks()
    {
        using var reference = Reference();
        int cases = 0, mismatches = 0, libraryMismatches = 0;
        foreach (var c in reference.RootElement.GetProperty("pow"u8).EnumerateArray())
        {
            double x = FromHex(c[0]), y = FromHex(c[1]), expected = FromHex(c[2]);
            cases++;
            if (BitConverter.DoubleToInt64Bits(Fdlibm.Pow(x, y)) != BitConverter.DoubleToInt64Bits(expected))
                mismatches++;
            if (Math.Pow(x, y) != expected)
                libraryMismatches++;
        }
        Assert.Equal(1500, cases);
        Assert.Equal(0, mismatches);
        Assert.True(libraryMismatches > 0, "Math.Pow agrees with fdlibm everywhere here: the cases don't tell them apart");
    }

    [Fact]
    public void SinAndCosAreBlinks()
    {
        using var reference = Reference();
        int cases = 0, mismatches = 0;
        foreach (var c in reference.RootElement.GetProperty("sin"u8).EnumerateArray())
        {
            var x = FromHex(c[0]);
            cases++;
            if (BitConverter.DoubleToInt64Bits(Fdlibm.Sin(x)) != BitConverter.DoubleToInt64Bits(FromHex(c[1])))
                mismatches++;
            if (BitConverter.DoubleToInt64Bits(Fdlibm.Cos(x)) != BitConverter.DoubleToInt64Bits(FromHex(c[2])))
                mismatches++;
        }
        Assert.Equal(1500, cases);
        Assert.Equal(0, mismatches);
    }

    [Fact]
    public void LogIsBlinks()
    {
        using var reference = Reference();
        int cases = 0, mismatches = 0;
        foreach (var c in reference.RootElement.GetProperty("log"u8).EnumerateArray())
        {
            cases++;
            if (BitConverter.DoubleToInt64Bits(Fdlibm.Log(FromHex(c[0]))) != BitConverter.DoubleToInt64Bits(FromHex(c[1])))
                mismatches++;
        }
        Assert.Equal(1500, cases);
        Assert.Equal(0, mismatches);
    }

    [Fact]
    public void ExpfIsBlinks()
    {
        using var reference = Reference();
        int cases = 0, mismatches = 0;
        foreach (var c in reference.RootElement.GetProperty("expf"u8).EnumerateArray())
        {
            cases++;
            var x = BitConverter.UInt32BitsToSingle(Convert.ToUInt32(c[0].GetString()!, 16));
            if (BitConverter.SingleToUInt32Bits(Fdlibm.Expf(x)) != Convert.ToUInt32(c[1].GetString()!, 16))
                mismatches++;
        }
        Assert.Equal(1500, cases);
        Assert.Equal(0, mismatches);
    }
}
