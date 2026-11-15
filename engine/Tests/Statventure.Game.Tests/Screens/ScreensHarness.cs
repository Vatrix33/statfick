using Statventure.Core;
using Statventure.Game.Mods;
using Statventure.Game.Random;
using Statventure.Game.Screens.About;
using Statventure.Game.Screens.Menu;
using Statventure.Game.Screens.Picker;
using Statventure.Game.Screens.Settings;
using Statventure.Game.Screens.Sheet;
using Statventure.Game.Screens.Slots;
using Statventure.Game.Screens.Start;
using Statventure.Game.Session;
using Statventure.Game.Settings;
using Statventure.Packs;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// Step 9's four screens (the start screen, the character's page, the menu, About) attached as the game attaches them, on
/// the game's document without a window (<see cref="GameUiHarness"/>) with the real parts on its scratch per-user folder:
/// the settings, a session on its storage, the mods (those named in <c>leftOut</c> left out at this start: a mod of that
/// name whose entry doesn't read, mounted through the game's check), the run's randomness. Step 10's commands
/// (<see cref="StandIns"/>) are registered as stand-ins recording their runs (<see cref="Ran"/>), so a test sees one
/// screen alone; or, with <c>stepTen</c>, step 10's three screens are attached after them as the game attaches all seven
/// (the picker's Documents, home and roots the scratch folder's own). The start runs <c>start-screen</c>, as the game's
/// start does.
/// </summary>
sealed class ScreensHarness : IDisposable
{
    /// <summary>Step 10's commands the screens run, as stand-ins where a test sees step 9's screens alone.</summary>
    public static readonly string[] StandIns = ["open-settings", "open-slots", "open-slot", "save-character", "load-character"];

    readonly bool _owned;

    /// <summary>The screens on a document of their own (<paramref name="width"/> × <paramref name="height"/> CSS px).</summary>
    public ScreensHarness(float width = 800, float height = 680, float scale = 1, GameTheme? theme = null, IReadOnlyList<string>? leftOut = null,
        Action<GameSession>? before = null, bool standIns = true, bool start = true, bool stepTen = false)
        : this(new GameUiHarness(width, height, scale, theme), leftOut, before, standIns, start, stepTen) => _owned = true;

    /// <summary>The screens on <paramref name="harness"/>'s document (a screen fixture's), which stays the caller's.</summary>
    public ScreensHarness(GameUiHarness harness, IReadOnlyList<string>? leftOut = null, Action<GameSession>? before = null, bool standIns = true, bool start = true,
        bool stepTen = false)
    {
        H = harness;
        Mods = new GameMods();
        if (leftOut is { Count: > 0 })
            MountLeftOut(leftOut);
        Mods.Attach(H.Settings);
        Session = new GameSession(H.Storage, H.Settings);
        before?.Invoke(Session);
        Parts = new GameParts(H.Settings, Session, Mods, H.Ui, new GameRandom(1), H.Game);
        foreach (var screen in new IGameScreen[] { Start, Sheet, Menu, About })
            screen.Attach(Parts);
        if (stepTen)
        {
            Documents = H.Scratch["home/Documents"];
            Directory.CreateDirectory(Documents);
            Settings = new SettingsScreen();
            Slots = new SlotsScreen { TimeZone = TimeZoneInfo.Utc };
            Picker = new PickerScreen
            {
                TimeZone = TimeZoneInfo.Utc,
                Documents = () => Documents,
                Home = () => H.Scratch["home"],
                Roots = () => [H.Scratch["home"]],
            };
            foreach (var screen in new IGameScreen[] { Settings, Slots, Picker })
                screen.Attach(Parts);
        }
        else if (standIns)
            H.Commands(Ran, StandIns);
        if (start)
        {
            H.Ui.Commands.Run(StatFickGame.StartCommand);
            H.Frame();
        }
    }

    public GameUiHarness H { get; }
    public GameMods Mods { get; }
    public GameSession Session { get; }
    public GameParts Parts { get; }
    public StartScreen Start { get; } = new();
    public SheetScreen Sheet { get; } = new();
    public MenuScreen Menu { get; } = new();
    public AboutScreen About { get; } = new();

    /// <summary>Step 10's screens, with <c>stepTen</c>.</summary>
    public SettingsScreen? Settings { get; }
    public SlotsScreen? Slots { get; }
    public PickerScreen? Picker { get; }

    /// <summary>The picker's Documents folder (in the scratch folder), with <c>stepTen</c>.</summary>
    public string? Documents { get; }

    /// <summary>The stand-ins' runs ("open-slot ser-alrik").</summary>
    public List<string> Ran { get; } = [];

    public UiDocument Document => H.Document;

    public UiElement Q(string selector) => H.Q(selector);

    public UiElement? Find(string selector) => H.Document.QuerySelector(selector);

    /// <summary>The menu's items as shown (words and keys), in order.</summary>
    public List<string> MenuItems() =>
        [.. Document.QuerySelectorAll("#game-menu .game-menu-item").Select(item => string.Join(" | ", item.ElementChildren.Select(c => c.TextContent)))];

    /// <summary>A save made in the folder before the screens are attached (a slot to continue), then left.</summary>
    public static Action<GameSession> WithCharacter(string name) => session =>
    {
        session.New(name);
        session.Leave();
    };

    /// <summary>A mod named <paramref name="name"/> (its <c>mod/info</c>) that the game's check leaves out (an entry that doesn't read).</summary>
    public static void BrokenMod(string mods, string name)
    {
        var writer = new PackWriter();
        writer.AddRecord(ModInfo.Path, new ModInfo { Name = name, Author = "Vatrix", Version = "1.0" });
        writer.AddRecordBinary("data/races/lamp", [0]);
        writer.Write(Path.Combine(mods, name.ToLowerInvariant().Replace(' ', '-') + ".pak"));
    }

    /// <summary>The mods <paramref name="names"/> made broken in a mods folder of the test's and mounted through the game's check (so they are left out at this start).</summary>
    void MountLeftOut(IReadOnlyList<string> names)
    {
        var folder = H.Scratch["mods"];
        Directory.CreateDirectory(folder);
        foreach (var name in names)
            BrokenMod(folder, name);
        var options = Mods.ApplyTo(new ApplicationOptions(), SettingsSnapshot.Defaults);
        using var mounted = new VirtualFileSystem([Mount.Mods(folder, options.ModFilter)]);
    }

    public void Dispose()
    {
        if (_owned)
            H.Dispose();
    }
}
