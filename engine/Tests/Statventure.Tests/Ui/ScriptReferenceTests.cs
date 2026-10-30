using System.Text;
using System.Text.Json;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// The script fixtures' C# twins run on the engine's event loop log what their JavaScript ran in
/// Chromium 152 logged (Reference/script-results.json, from Tools/Scripts/make-script-references.sh):
/// the same callbacks in the same order, in the same frames.
/// </summary>
public class ScriptReferenceTests
{
    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    public static TheoryData<string> Fixtures => new(ScriptFixtures.All.Select(f => f.Name));

    static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void TheFixtureFileIsUpToDate()
    {
        var path = Path.Combine(ReferenceDirectory, "script-fixtures.json");
        var fixtures = ScriptFixtures.All.Select(f => new { f.Name, f.Width, f.Height, f.Css, f.Html, f.Script, f.Frames });
        var text = JsonSerializer.Serialize(fixtures, Json) + "\n";
        if (!File.Exists(path) || File.ReadAllText(path) != text)
            File.WriteAllText(path, text);
        Assert.NotEmpty(ScriptFixtures.All);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheEngineRunsScriptsAsChromiumDid(string name)
    {
        var fixture = ScriptFixtures.All.Single(f => f.Name == name);
        var resultsPath = Path.Combine(ReferenceDirectory, "script-results.json");
        Assert.True(File.Exists(resultsPath), "no Chromium results: run Tools/Scripts/make-script-references.sh");
        using var results = JsonDocument.Parse(File.ReadAllText(resultsPath));
        Assert.True(results.RootElement.TryGetProperty(name, out var expected), $"no Chromium result for '{name}': run Tools/Scripts/make-script-references.sh");
        var want = expected.GetProperty("log").EnumerateArray().Select(e => e.GetString()!).ToList();
        var got = Run(fixture);
        // SCRIPT_LOG_DIR=/abs/dir writes the engine's log there (for comparing by hand).
        if (Environment.GetEnvironmentVariable("SCRIPT_LOG_DIR") is { Length: > 0 } dump)
            File.WriteAllLines(Path.Combine(dump, name + ".log"), got);
        if (!want.SequenceEqual(got))
            Assert.Fail("log differs:\n" + Diff(want, got));
    }

    /// <summary>
    /// Runs the fixture as the harness runs its JavaScript: the page laid out and shown, the scenario
    /// as a task of its own (a 0 ms timer) that first counts frames with a frame callback, frames at
    /// 60 Hz until the fixture's last, what the scenario asked the harness for done between frames.
    /// Returns the lines of the frames before the last.
    /// </summary>
    internal static List<string> Run(ScriptFixture fixture)
    {
        var document = new UiDocument(TestFonts.Library);
        document.SetViewport(fixture.Width, fixture.Height);
        document.AddStylesheet(fixture.Css, fixture.Name);
        Markup.SetInnerHtml(document.Body, fixture.Html);
        document.Update(0);
        var s = new ScriptContext(document);
        document.SetTimeout(() =>
        {
            void Tick(double time)
            {
                s.Frame++;
                s.FrameTime = time;
                document.RequestAnimationFrame(Tick);
            }
            document.RequestAnimationFrame(Tick);
            fixture.Run(s);
        });
        for (var frame = 1; s.Frame < fixture.Frames && frame < 600; frame++)
        {
            document.Update(frame / 60.0);
            var requests = s.Requests.ToArray();
            s.Requests.Clear();
            foreach (var request in requests)
                request();
        }
        return s.Lines.Where(line => int.Parse(line.AsSpan(1, line.IndexOf(' ') - 1)) < fixture.Frames).ToList();
    }

    static string Diff(List<string> want, List<string> got)
    {
        var text = new StringBuilder();
        for (var i = 0; i < Math.Max(want.Count, got.Count); i++)
        {
            var w = i < want.Count ? want[i] : "(none)";
            var g = i < got.Count ? got[i] : "(none)";
            text.AppendLine($"{(w == g ? "  " : "! ")}{w,-40} | {g}");
        }
        return text.ToString();
    }
}
