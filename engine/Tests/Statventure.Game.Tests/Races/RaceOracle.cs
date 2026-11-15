using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Statventure.Game.Data.Races;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The race oracle's committed results (GM2 step 1; Docs/Game/Hero.md, "How it works", step 1), made from the old game by
/// <c>Tools/Scripts/make-race-references.sh</c> in its test client and read here so the tests run where there is no
/// Chromium: <c>Reference/race-cases.json</c> (the case list, with the crafted races in the old file's form),
/// <c>race-fold.json</c> (each case's <c>calculateRaceEffectsUncached</c> and <c>raceStatModifier</c>, step 2's case form)
/// and <c>race-rules.json</c> (<c>normalizeRaceDefinition</c>'s cases, <c>applyRaceText</c>'s refusals, the accents, the
/// normalized races). Both result files name the old game's sources by SHA-256.
/// </summary>
static class RaceOracle
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    public static string Folder => Path.Combine(ProjectDirectory(), "Reference");

    /// <summary>The engine folder of the repo (the committed data and packs are under it).</summary>
    public static string Engine => Path.GetFullPath(Path.Combine(ProjectDirectory(), "..", ".."));

    public static string RacesFile => Path.Combine(TestPaths.OldGame, "objects", "races.json");

    static readonly Lazy<JsonDocument> CasesDocument = new(() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Folder, "race-cases.json"))));
    static readonly Lazy<JsonDocument> FoldDocument = new(() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Folder, "race-fold.json"))));
    static readonly Lazy<JsonDocument> RulesDocument = new(() => JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Folder, "race-rules.json"))));

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
