using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The old game's oracle results (GM2-P13; Docs/Game/Hero.md, "How it works", step 2): Reference/old-game/&lt;module&gt;.json,
/// written by Tools/Scripts/make-old-game-references.sh from the old game's own code on a module's case list
/// (Tools/OldGameOracle/modules/&lt;module&gt;.cjs) and committed, so the tests compare where there is no Node or Chromium.
/// Each case is its input and the old game's output, as JSON; numbers are doubles read back exactly, and the ones JSON
/// has no form for are the strings "Infinity", "-Infinity" and "NaN".
/// </summary>
sealed class OldGameReference
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    public static string Folder => Path.Combine(ProjectDirectory(), "Reference", "old-game");

    public static string PathOf(string module) => Path.Combine(Folder, module + ".json");

    public string Module { get; }
    public IReadOnlyDictionary<string, string> Sources { get; }
    public IReadOnlyList<(JsonElement Input, JsonElement Output)> Cases { get; }

    OldGameReference(JsonElement root)
    {
        Module = root.GetProperty("module").GetString()!;
        Sources = root.GetProperty("sources").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetString()!);
        Cases = [.. root.GetProperty("cases").EnumerateArray().Select(c => (c.GetProperty("input"), c.GetProperty("output")))];
    }

    static readonly Dictionary<string, OldGameReference> Loaded = [];

    public static OldGameReference Load(string module)
    {
        lock (Loaded)
        {
            if (!Loaded.TryGetValue(module, out var reference))
            {
                using var json = JsonDocument.Parse(File.ReadAllBytes(PathOf(module)));
                Loaded[module] = reference = new OldGameReference(json.RootElement.Clone());
            }
            return reference;
        }
    }

    /// <summary>Every module's results committed.</summary>
    public static IEnumerable<string> Modules() =>
        Directory.EnumerateFiles(Folder, "*.json").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal)!;

    /// <summary>The SHA-256 of an old-game file as it is now (to know the results are of today's old game).</summary>
    public static string Hash(string oldGameFile) =>
        Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(TestPaths.OldGame, oldGameFile))));

    /// <summary>A JSON number as the old game wrote it (the non-finite ones as strings).</summary>
    public static double Number(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number => value.GetDouble(),
        JsonValueKind.String => value.GetString() switch
        {
            "Infinity" => double.PositiveInfinity,
            "-Infinity" => double.NegativeInfinity,
            "NaN" => double.NaN,
            var text => throw new FormatException($"not a number: {text}"),
        },
        _ => throw new FormatException($"not a number: {value}"),
    };

    /// <summary>The same double (0 and −0 alike, as JSON writes −0 as 0).</summary>
    public static bool Same(double expected, double actual) => expected.Equals(actual);
}
