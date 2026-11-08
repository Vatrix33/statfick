using Statventure.Editor.Browser;
using Statventure.Editor.Game;
using Statventure.Editor.Session;
using Statventure.Storage;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Game;

/// <summary>
/// The game's folder's screens against Chromium (M12 step 6): "Where is Stat Fick?" on a first start and with the
/// remembered folder gone (the window's smallest size), and Game folder… in the editor (an installed game's). Their
/// folders are the fixture's own (the browser's places and listing given), so no path of this machine's is in the markup.
/// </summary>
public sealed class GameScreens : IEditorFixtures
{
    public IEnumerable<EditorFixture> Fixtures =>
    [
        new("game-where", 1000, 640, editor => Screen(editor, null)),
        new("game-where-gone", 800, 500, editor => Screen(editor, new GameProblem(GameProblemKind.Missing, Games + "/stat-fick-2026.11.1.1"))),
        new("game-folder", 1000, 640, Folder, Game: () => GameData.Installed(Games + "/stat-fick-2026.11.8.1")),
    ];

    const string Home = "/home/player";
    const string Games = Home + "/Games";
    const string Player = Home + "/.local/share/stat-fick";

    static IReadOnlyList<BrowserPlaceItem> Places() =>
    [
        new("Mods", Player + "/mods", BrowserPlaceKind.Mods),
        new("Documents", Home + "/Documents", BrowserPlaceKind.Documents),
        new("Home", Home, BrowserPlaceKind.Home),
        new("/", "/", BrowserPlaceKind.Root),
        new("Games", Games, BrowserPlaceKind.Recent),
    ];

    static IReadOnlyList<FolderEntry> List(string folder, IReadOnlyCollection<string>? endings, bool hidden)
    {
        var time = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc);
        return folder == Games
            ? [
                new("stat-fick-2026.11.8.1", Games + "/stat-fick-2026.11.8.1", true, 0, time, false),
                new("stat-fick-2026.11.9.1", Games + "/stat-fick-2026.11.9.1", true, 0, time, false),
                new("stat-fick-2026.11.9.1-linux.tar.gz", Games + "/stat-fick-2026.11.9.1-linux.tar.gz", false, 47_290_112, time.AddHours(1), false),
            ]
            : [];
    }

    static void Screen(EditorHarness editor, GameProblem? problem)
    {
        editor.ShowScreen(new GameScreen(Player, problem, "dev", windows: false, s =>
        {
            s.TimeZone = TimeZoneInfo.Utc;
            s.PlacesOverride = Places;
            s.ListOverride = List;
            s.StartFolder = Games;
        }));
        editor.Screen!.Browser.Select(problem is null ? 1 : -1);
        editor.Frame();
    }

    static void Folder(EditorHarness editor)
    {
        var part = editor.Browser();
        part.TimeZone = TimeZoneInfo.Utc;
        part.PlacesOverride = () => [.. Places().Take(1), new("Game packs (read-only)", Games + "/stat-fick-2026.11.8.1/packs", null), .. Places().Skip(1)];
        part.ListOverride = List;
        _ = editor.Run(() => editor.View.Part<GamePart>().Change(Games));
        part.Current!.Select(1);
        editor.Frame();
    }
}
