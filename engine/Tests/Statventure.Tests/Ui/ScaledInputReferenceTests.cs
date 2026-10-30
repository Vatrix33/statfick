using System.Globalization;
using System.Text.Json;

namespace Statventure.Tests.Ui;

/// <summary>
/// The input fixtures on a screen of device scale 1.65 see what Chromium 152 saw there
/// (Reference/scaled/input-results@1.65.json, from Tools/Scripts/make-input-references.sh 1.65):
/// the same input in window units, hit tested, scrolled and reported in Chromium's units.
/// </summary>
public class ScaledInputReferenceTests
{
    const float Scale = 1.65f;

    public static TheoryData<string> Fixtures => new(InputFixtures.All.Select(f => f.Name));

    static string ResultsPath => Path.Combine(Path.GetDirectoryName(Infrastructure.Golden.GoldenPath("x"))!, "..", "Reference", "scaled",
        $"input-results@{Scale.ToString(CultureInfo.InvariantCulture)}.json");

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheEngineSeesWhatChromiumSaw(string name)
    {
        var fixture = InputFixtures.All.Single(f => f.Name == name);
        if (!File.Exists(ResultsPath))
            throw new FileNotFoundException("run Tools/Scripts/make-input-references.sh 1.65 to play the fixtures in Chromium", ResultsPath);
        using var results = JsonDocument.Parse(File.ReadAllText(ResultsPath));
        Assert.True(results.RootElement.TryGetProperty(name, out var expected), $"no Chromium result for '{name}': run Tools/Scripts/make-input-references.sh 1.65");
        InputReferenceTests.Check(fixture, expected, Scale);
    }
}
