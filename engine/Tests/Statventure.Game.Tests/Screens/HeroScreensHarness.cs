using System.Numerics;
using Statventure.Game.Data.Races;
using Statventure.Game.Mods;
using Statventure.Game.Random;
using Statventure.Game.Screens.Maker;
using Statventure.Game.Screens.Races;
using Statventure.Game.Session;
using Statventure.Game.Settings;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The race carousel and the character maker (GM2 step 8) attached as the game attaches them, on the game's document
/// without a window (<see cref="GameUiHarness"/>) with the real parts on its scratch per-user folder: the settings, a
/// session on its storage, the mods, a randomness of the test's (seeded, or scripted). Their data is the screens' test
/// data over the committed <c>data.pak</c> (<see cref="HeroTestData"/>), or the shipped data alone (<c>models.pak</c> and <c>data.pak</c>). The game's sheets are
/// asked in the game's order before theirs, and the commands the flow hands over to are stand-ins recording their runs
/// (<see cref="Ran"/>): <c>open-sheet</c> (GM2 step 7's) and <c>start-screen</c>, unless the test attaches the real start
/// screen (<c>start: true</c>) or the real start screen and sheet (<c>sheet: true</c>: the whole flow as the game runs it,
/// the sheet asking for <c>open-races</c> on every open of a hero that needs it, Q26).
/// </summary>
sealed class HeroScreensHarness : IDisposable
{
    /// <summary>The game's screens' sheets in the order the game asks them (its screens' <c>Attach</c>, <c>StatFickGame.NewScreens</c>), before the flow's.</summary>
    public static readonly string[] GameSheets =
        ["ui/styles/start", "ui/styles/sheet", "ui/styles/menu", "ui/styles/about", "ui/styles/settings", "ui/styles/slots", "ui/styles/picker"];

    /// <summary>The test data the screen fixtures share (one for the process; its reads are safe from several threads).</summary>
    public static HeroTestData Shared => SharedData.Value;

    static readonly Lazy<HeroTestData> SharedData = new(() =>
    {
        var data = new HeroTestData(Path.Combine(TestPaths.Packs, "data.pak"));
        AppDomain.CurrentDomain.ProcessExit += (_, _) => data.Dispose();
        return data;
    });

    readonly bool _owned;
    readonly HeroTestData? _ownData;
    readonly VirtualFileSystem? _shipped;

    /// <summary>The screens on a document of their own (<paramref name="width"/> × <paramref name="height"/> CSS px).</summary>
    public HeroScreensHarness(float width = 1240, float height = 900, float scale = 1, GameTheme? theme = null, Action<GameSettings>? before = null,
        bool shipped = false, IGameRandom? random = null, bool start = false, bool sheet = false, IReadOnlyDictionary<string, IReadOnlyList<RaceSwatch>>? races = null)
        : this(new GameUiHarness(width, height, scale, theme, before: before), shipped: shipped, random: random, start: start, sheet: sheet, own: true, races: races) => _owned = true;

    /// <summary>The screens on <paramref name="harness"/>'s document (a screen fixture's, which stays the caller's), on the shared test data.</summary>
    public HeroScreensHarness(GameUiHarness harness, bool shipped = false, IGameRandom? random = null, bool start = false, bool sheet = false, bool own = false,
        IReadOnlyDictionary<string, IReadOnlyList<RaceSwatch>>? races = null)
    {
        start |= sheet;
        H = harness;
        if (shipped)
            _shipped = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "models.pak")), Mount.Pack(Path.Combine(TestPaths.Packs, "data.pak"))]);
        else if (own)
            _ownData = new HeroTestData(Path.Combine(TestPaths.Packs, "data.pak"), races);
        Data = _shipped ?? (_ownData ?? Shared).Data;
        foreach (var stylesheet in GameSheets)
            H.Ui.UseStylesheet(stylesheet);
        Mods = new GameMods();
        Mods.Attach(H.Settings);
        Session = new GameSession(H.Storage, H.Settings);
        Random = random ?? new GameRandom(8);
        Parts = new GameParts(H.Settings, Session, Mods, H.Ui, Random, H.Game);
        if (start)
            Start = new Screens.Start.StartScreen();
        Start?.Attach(Parts);
        if (sheet)
            Sheet = new Screens.Sheet.SheetScreen();
        Sheet?.Attach(Parts);
        Races = new RacesScreen { Data = () => Data };
        Maker = new MakerScreen { Data = () => Data };
        Races.Attach(Parts);
        Maker.Attach(Parts);
        var standIns = new List<string>();
        if (!sheet)
            standIns.Add(HeroFlow.SheetCommand);
        if (!start)
            standIns.Add(StatFickGame.StartCommand);
        H.Commands(Ran, [.. standIns]);
    }

    public GameUiHarness H { get; }
    public VirtualFileSystem Data { get; }
    public GameMods Mods { get; }
    public GameSession Session { get; }
    public IGameRandom Random { get; }
    public GameParts Parts { get; }
    public Screens.Start.StartScreen? Start { get; }
    public Screens.Sheet.SheetScreen? Sheet { get; }
    public RacesScreen Races { get; }
    public MakerScreen Maker { get; }

    /// <summary>The stand-ins' runs ("open-sheet").</summary>
    public List<string> Ran { get; } = [];

    public UiDocument Document => H.Document;

    /// <summary>A new character made and opened (as New character's OK makes one): no race, no look.</summary>
    public void NewHero(string name = "Ser Alrik") => Session.New(name);

    /// <summary>A command run as a click runs it, then a frame; whether it ran.</summary>
    public bool Run(string command, string? argument = null)
    {
        var ran = H.Ui.Commands.Run(command, argument);
        H.Frame();
        return ran;
    }

    public UiElement Q(string selector) => H.Q(selector);

    public UiElement? Find(string selector) => H.Document.QuerySelector(selector);

    /// <summary>A click on <paramref name="element"/>, then the pointer moved where no hover look shows.</summary>
    public void Click(UiElement element)
    {
        H.Click(element);
        H.Move(new Vector2(2, 2));
    }

    /// <summary>A key pressed and released with nothing held.</summary>
    public bool Key(string key) => H.Key(key, key);

    /// <summary>The carousel's shown words: its race's name, its kind, its button and its status line.</summary>
    public (string Name, string Kind, string Button, bool Enabled, string Status) Carousel() =>
        (Q("#races-name").TextContent, Q("#races-kind").TextContent, Q("#races-choose").TextContent, !Q("#races-choose").HasAttribute("disabled"), Q("#races-status").TextContent);

    /// <summary>The maker's row of a choice ("sex", "part:hair", "colour:skin").</summary>
    public UiElement Row(string choice) => Q($"#maker-screen .maker-row[data-choice=\"{choice}\"]");

    /// <summary>The checked option of a group of the maker's (its <c>data-value</c> or <c>data-preset</c>).</summary>
    public string? Checked(UiElement group) =>
        group.Descendants().FirstOrDefault(e => e.GetAttribute("aria-checked") == "true") is { } option ? option.GetAttribute("data-value") ?? option.GetAttribute("data-preset") : null;

    public void Dispose()
    {
        if (_owned)
            H.Dispose();
        _ownData?.Dispose();
        _shipped?.Dispose();
    }
}
