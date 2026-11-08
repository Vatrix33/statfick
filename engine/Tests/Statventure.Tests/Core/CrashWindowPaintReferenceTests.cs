using System.Globalization;
using System.Text.Json;
using Statventure.Core;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Ui;
using Statventure.UI.Dom;

namespace Statventure.Tests.Core;

/// <summary>
/// The crash window and the plain message window (Docs/Packaging.md, "Crashes"; Q22, Q23, Q6; M12 step 5) as Chromium 152
/// shows the same markup and stylesheet (Golden/ui-crash-window*@1.png and @1.65.png, ui-message-window*, made by
/// Tools/Scripts/make-scaled-paint-references.sh 1 1 crash-window-fixtures.json and 1.65 1 crash-window-fixtures.json:
/// under X11, CPU raster, with the harness's Lato, the old game's files, the same bytes as the engine's own): the
/// engine's own documents (<see cref="CrashWindow"/>, <see cref="MessageWindow"/>) painted at the windows' size, at the
/// paint fixtures' limits (12 levels, 0.2 % of pixels). The crash window with the path focused and selected (Chromium's
/// page selects it with select() and focuses it, as the window does), a long error in its scrolling line with a Windows
/// path, and one without a crash file; the message window with the editor's usage.
/// </summary>
[Collection("GPU")]
public class CrashWindowPaintReferenceTests(GpuFixture gpu)
{
    static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    static readonly DateTimeOffset Time = new(2026, 11, 8, 14, 5, 10, TimeSpan.FromHours(1));

    /// <summary>A crash window's content: the program, the crash, the logs folder ("Open logs folder").</summary>
    sealed record CrashCase(string Program, CrashReport Crash, string? Logs);

    const string LongMessage = "Could not find file 'C:\\Users\\Player\\AppData\\Roaming\\Stat Fick\\mods\\my-mod.pak'. The pack the mods folder listed " +
        "went away while the game ran, so the entries it had listed couldn't be read: ui/frames/frame.png, ui/frames/frame-dark.png, " +
        "ui/sounds/click, ui/sounds/close, world/areas/gloamfen-marsh, world/areas/abyssal-rift and fourteen more, all of which the " +
        "game had already shown in its lists.";

    static readonly Dictionary<string, CrashCase> Crashes = new()
    {
        ["crash-window"] = new("Statventure Sandbox",
            new(new InvalidOperationException("the sandbox crashed on purpose (--crash)"), "on the main thread", Time,
                "/home/player/.local/share/stat-fick/logs/crash-2026-11-08_14-05-10.txt"),
            "/home/player/.local/share/stat-fick/logs"),
        ["crash-window-long"] = new("Statventure Editor",
            new(new FileNotFoundException(LongMessage), "on the main thread", Time,
                "C:\\Users\\Player\\AppData\\Roaming\\Stat Fick\\logs\\crash-2026-11-08_14-05-10.txt"),
            "C:\\Users\\Player\\AppData\\Roaming\\Stat Fick\\logs"),
        ["crash-window-no-file"] = new("Statventure Sandbox",
            new(new InvalidOperationException("the sandbox crashed on purpose (--crash)"), "on the main thread", Time, null),
            "/home/player/.local/share/stat-fick/logs"),
    };

    const string MessageHeading = "Statventure Editor can't start";
    const string MessageText = "'--bad' isn't an option\nusage: statventure-editor [--repo <engine folder> | --game <game folder>] [--player-folder <folder>]";

    /// <summary>Chromium's page: the window's markup, the path field focused (data-focus, the harness's) after select() chose its text.</summary>
    static string ChromiumMarkup(CrashCase c) =>
        CrashWindow.Markup(c.Program, c.Crash, c.Logs is not null).Replace("class=\"crash-file\"", "class=\"crash-file\" data-focus")
        + (c.Crash.File is null ? "" : "<script>document.querySelector(\".crash-file\").select()</script>");

    public static readonly PaintFixture[] All =
    [
        .. Crashes.Select(c => new PaintFixture(c.Key, EngineWindows.Width, EngineWindows.Height, EngineWindows.Css, ChromiumMarkup(c.Value))),
        new("message-window", EngineWindows.Width, EngineWindows.Height, EngineWindows.Css, MessageWindow.Markup(MessageHeading, MessageText)),
    ];

    static readonly float[] Scales = [1, 1.65f];

    public static TheoryData<string, float> Fixtures
    {
        get
        {
            var data = new TheoryData<string, float>();
            foreach (var fixture in All)
                foreach (var scale in Scales)
                    data.Add(fixture.Name, scale);
            return data;
        }
    }

    static string ReferenceDirectory => Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "..", "Reference");

    [Fact]
    public void TheFixtureListIsUpToDate()
    {
        // The windows' own stylesheet only (no paint fixtures' base sheet): Chromium gets what the engine shows.
        var path = Path.Combine(ReferenceDirectory, "crash-window-fixtures.json");
        var json = JsonSerializer.Serialize(All.Select(f => new
        {
            name = f.Name, width = f.Width, height = f.Height, css = f.Css, html = f.Html,
            scrollX = f.ScrollX, scrollY = f.ScrollY, windowFocused = f.WindowFocused,
        }));
        if (!File.Exists(path) || File.ReadAllText(path) != json)
            File.WriteAllText(path, json);
        Assert.NotEmpty(json);
    }

    /// <summary>The engine's own window document for a fixture, laid out at <paramref name="scale"/> as its window's host lays it out.</summary>
    static (UiDocument Document, IDisposable Owner) Build(PaintFixture fixture, float scale)
    {
        UiDocument document;
        IDisposable owner;
        if (Crashes.TryGetValue(fixture.Name, out var c))
        {
            var window = new CrashWindow(c.Program, c.Crash, c.Logs);
            (document, owner) = (window.Document, window);
        }
        else
        {
            var message = new MessageWindow(MessageHeading, MessageText);
            (document, owner) = (message.Document, message);
        }
        document.SetViewport(fixture.Width, fixture.Height);
        document.SetZoom(scale, 1);
        document.Update(0);
        return (document, owner);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void TheWindowLooksAsInChromium(string name, float scale)
    {
        var fixture = All.Single(f => f.Name == name);
        var (document, owner) = Build(fixture, scale);
        using (owner)
        {
            var image = EngineScreensPaintReferenceTests.Render(gpu.Device, document, (int)MathF.Round(fixture.Width * scale), (int)MathF.Round(fixture.Height * scale));
            Golden.Match($"ui-{name}@{Number(scale)}", image, tolerance: fixture.Tolerance, maxDifferentRatio: 0.002);
        }
    }
}
