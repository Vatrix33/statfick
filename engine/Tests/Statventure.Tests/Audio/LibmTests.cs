using System.Text.Json;
using Statventure.Audio;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Audio;

/// <summary>
/// The engine's exp2f and log2f give this system's glibc results (Reference/libm.json, from
/// Tools/Scripts/make-native-references.sh): hashes of every result over every 4099th float, or over
/// all 2³² with STATVENTURE_EXHAUSTIVE=1 (a few minutes). On Linux .NET's MathF calls glibc too;
/// the port is there so Windows (its C runtime differs) gives the same results.
/// </summary>
public class LibmTests
{
    static JsonElement Reference(string function)
    {
        var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference", "libm.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.GetProperty(function).Clone();
    }

    static ulong Hash(Func<float, float> function, uint stride)
    {
        var hash = 0xcbf29ce484222325ul;
        for (ulong b = 0; b < 0x100000000ul; b += stride)
        {
            var result = function(BitConverter.UInt32BitsToSingle((uint)b));
            var bits = float.IsNaN(result) ? 0x7fc00000u : BitConverter.SingleToUInt32Bits(result);
            for (var i = 0; i < 4; i++)
            {
                hash ^= (bits >> (8 * i)) & 0xff;
                hash *= 0x100000001b3ul;
            }
        }
        return hash;
    }

    static void Check(string name, Func<float, float> port)
    {
        var reference = Reference(name);
        Assert.Equal(reference.GetProperty("sample"u8).GetString(), Hash(port, 4099).ToString("x16"));
        // glibc's FMA and SSE2 variants agree on every float, so the CPU's choice doesn't matter.
        Assert.Equal(0, reference.GetProperty("unfusedDiffers"u8).GetInt64());
        if (Environment.GetEnvironmentVariable("STATVENTURE_EXHAUSTIVE") == "1")
            Assert.Equal(reference.GetProperty("all"u8).GetString(), Hash(port, 1).ToString("x16"));
    }

    [Fact]
    public void Exp2fIsGlibcs() => Check("exp2f", Libm.Exp2f);

    [Fact]
    public void Log2fIsGlibcs() => Check("log2f", Libm.Log2f);
}
