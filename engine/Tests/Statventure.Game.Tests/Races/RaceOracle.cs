using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Statventure.Game.Data.Races;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The race oracle's committed results (GM2 step 1; Docs/Game/Hero.md, "How it works", steps 1 and 2), made from the old
/// game in its test client by the oracle's modules <c>race-fold</c> and <c>race-rules</c> (<c>Tools/OldGameOracle/</c>,
/// <c>Tools/Scripts/make-old-game-references.sh</c>; step 1's own script folded in at step 2) and read here so the tests
/// run where there is no Chromium: <c>Tools/OldGameOracle/cases/race-cases.json</c> (the case list, with the crafted races
/// in the old file's form), <c>Reference/old-game/race-fold.json</c> (each case's <c>calculateRaceEffectsUncached</c> and
/// <c>raceStatModifier</c>) and <c>race-rules.json</c> (one case per part: <c>normalizeRaceDefinition</c>'s cases,
/// <c>applyRaceText</c>'s refusals, the accents, the normalized races, the crafted ones; read here as one object of the
/// parts). Both result files name the old game's sources by SHA-256.
/// </summary>
static class RaceOracle
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    public static string Folder => Path.Combine(ProjectDirectory(), "Reference", "old-game");

    /// <summary>The engine folder of the repo (the committed data and packs are under it).</summary>
    public static string Engine => Path.GetFullPath(Path.Combine(ProjectDirectory(), "..", ".."));

    public static string RacesFile => Path.Combine(TestPaths.OldGame, "objects", "races.json");

    static readonly Lazy<JsonDocument> CasesDocument = new(() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Engine, "Tools", "OldGameOracle", "cases", "race-cases.json"))));
    static readonly Lazy<JsonDocument> FoldDocument = new(() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Folder, "race-fold.json"))));
    static readonly Lazy<JsonDocument> RulesDocument = new(() =>
    {
        // { "sources", and each part's output by its name }, from the module's one case a part.
        using var results = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Folder, "race-rules.json")));
        var parts = results.RootElement.GetProperty("cases").EnumerateArray()
            .Select(c => $"\"{c.GetProperty("input").GetProperty("part").GetString()}\":{c.GetProperty("output").GetRawText()}");
        return JsonDocument.Parse($"{{\"sources\":{results.RootElement.GetProperty("sources").GetRawText()},{string.Join(",", parts)}}}");
    });

    public static JsonElement Cases => CasesDocument.Value.RootElement;
    public static JsonElement Fold => FoldDocument.Value.RootElement;
    public static JsonElement Rules => RulesDocument.Value.RootElement;

    /// <summary>The races of races.json converted, in its order.</summary>
    public static IReadOnlyList<RaceEntry> FileRaces => FileRacesValue.Value;
    static readonly Lazy<IReadOnlyList<RaceEntry>> FileRacesValue = new(() => OldRaceFile.Convert(File.ReadAllText(RacesFile)));

    /// <summary>The case list's crafted races converted, numbered after the file's 28 as the oracle numbered them.</summary>
    public static IReadOnlyList<RaceEntry> CraftedRaces => CraftedValue.Value;
    static readonly Lazy<IReadOnlyList<RaceEntry>> CraftedValue = new(() =>
        [.. Cases.GetProperty("crafted").EnumerateArray().Select((raw, i) => OldRaceFile.Normalize(raw, 28 + i))]);

    /// <summary>Every race the fold's cases name, by id.</summary>
    public static IReadOnlyDictionary<string, Race> AllRaces => AllValue.Value;
    static readonly Lazy<IReadOnlyDictionary<string, Race>> AllValue = new(() => FileRaces.Concat(CraftedRaces).ToDictionary(r => r.Id, r => r.Race, StringComparer.Ordinal));

    /// <summary>Each source the results were made from (an old-game path) with its SHA-256 then and now.</summary>
    public static IEnumerable<(string Path, string Then, string Now)> Sources(JsonElement results) =>
        results.GetProperty("sources").EnumerateObject().Select(s =>
            (s.Name, s.Value.GetString()!, Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(TestPaths.OldGame, s.Name))))));

    /// <summary>A number of the results: a JSON number, or "Infinity", "-Infinity", "NaN".</summary>
    public static double Number(JsonElement value) => value.ValueKind == JsonValueKind.String
        ? value.GetString() switch { "Infinity" => double.PositiveInfinity, "-Infinity" => double.NegativeInfinity, _ => double.NaN }
        : value.GetDouble();
}
