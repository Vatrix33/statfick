using System.Numerics;
using Statventure.Core;
using Statventure.Game.Data.StandIn;
using Statventure.Game.Mods;
using Statventure.Game.Random;
using Statventure.Game.Screens.Picker;
using Statventure.Game.Screens.Settings;
using Statventure.Game.Screens.Slots;
using Statventure.Game.Session;
using Statventure.Game.Settings;
using Statventure.Packs;
using Statventure.Storage;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// Step 10's three screens (the settings screen with the mod list, the slot list with the damaged-save question, the file
/// picker) attached to parts of the harness's own (<see cref="GameUiHarness"/>: its settings, a session on its scratch
/// per-user folder, mods checked there, its document), as the application attaches them; and the scenes their tests and
/// fixtures start from, made the same on every machine (fixed names, contents, times; the times shown in UTC).
/// </summary>
sealed class ScreenParts
{
    ScreenParts(GameUiHarness harness, GameParts parts, SettingsScreen settings, SlotsScreen slots, PickerScreen picker)
    {
        Harness = harness;
        Parts = parts;
        Settings = settings;
        Slots = slots;
        Picker = picker;
    }

    public GameUiHarness Harness { get; }
    public GameParts Parts { get; }
    public SettingsScreen Settings { get; }
    public SlotsScreen Slots { get; }
    public PickerScreen Picker { get; }
    public GameSession Session => Parts.Session;

    /// <summary>The screens attached to <paramref name="h"/>'s parts, their times in <paramref name="zone"/> (UTC by default).</summary>
    public static ScreenParts Attach(GameUiHarness h, TimeZoneInfo? zone = null)
    {
        var mods = new GameMods();
        mods.Attach(h.Settings);
        var parts = new GameParts(h.Settings, new GameSession(h.Storage, h.Settings), mods, h.Ui, new GameRandom(1), h.Game);
        var settings = new SettingsScreen();
        var slots = new SlotsScreen { TimeZone = zone ?? TimeZoneInfo.Utc };
        var picker = new PickerScreen { TimeZone = zone ?? TimeZoneInfo.Utc };
        foreach (var screen in new IGameScreen[] { settings, slots, picker })
            screen.Attach(parts);
        h.Frame();
        return new ScreenParts(h, parts, settings, slots, picker);
    }

    /// <summary>Runs a command as the keys and the screens run them (a task of the document's loop), then a frame.</summary>
    public bool Run(string command, string? argument = null)
    {
        var ran = false;
        Harness.Document.RunScript(() => ran = Parts.Ui.Commands.Run(command, argument));
        Harness.Frame();
        return ran;
    }

    /// <summary>The first element <paramref name="selector"/> matches.</summary>
    public UiElement Q(string selector) => Harness.Q(selector);

    /// <summary>The pointer put where no hover look shows (the page's corner, on a window's backdrop).</summary>
    public void PointerAway() => Harness.Move(new Vector2(4, 4));

    // ---------- Mods ----------

    /// <summary>The mods folder in the harness's per-user folder.</summary>
    public string ModsFolder => Path.Combine(Harness.PlayerFolder, "mods");

    /// <summary>A mod written into the mods folder as <paramref name="file"/>.</summary>
    public string Mod(string file, Action<PackWriter> fill)
    {
        Directory.CreateDirectory(ModsFolder);
        var writer = new PackWriter();
        fill(writer);
        var path = Path.Combine(ModsFolder, file);
        writer.Write(path);
        return path;
    }

    /// <summary>
    /// The mods of the "every state" scene: loaded with its info (Ember Rite, Frost Hollow), loaded without one (lanterns),
    /// off at the start (Quiet Nights), left out with two problems (Broken Vale).
    /// </summary>
    public void EveryState()
    {
        Mod("ember-rite.pak", w =>
        {
            w.AddRecord(ModInfo.Path, new ModInfo { Name = "Ember Rite", Author = "Vatrix", Version = "1.0", Description = "A rite of embers: a chime for the shrines and a lantern sold in Alderkeep." });
            w.AddRecord("stand-in/items/lantern", new StandInItem { Title = "Lantern", Price = 12, Rarity = "Rare" });
        });
        Mod("frost-hollow.pak", w =>
        {
            w.AddRecord(ModInfo.Path, new ModInfo { Name = "Frost Hollow", Author = "Maren", Version = "0.4.2", Description = "Colder nights in the hollow." });
            w.AddRecord("stand-in/items/fur-cloak", new StandInItem { Title = "Fur cloak", Price = 30 });
        });
        Mod("lanterns.pak", w => w.AddRecord("stand-in/items/paper-lantern", new StandInItem { Title = "Paper lantern", Price = 2 }));
        Mod("quiet-nights.pak", w =>
        {
            w.AddRecord(ModInfo.Path, new ModInfo { Name = "Quiet Nights", Author = "Vatrix", Version = "2.1" });
            w.AddRecord("stand-in/items/candle", new StandInItem { Title = "Candle", Price = 1 });
        });
        Mod("broken-vale.pak", w =>
        {
            w.AddRecord(ModInfo.Path, new ModInfo { Name = "Broken Vale", Author = "Someone", Version = "0.1", Description = "A test mod with values its checks refuse." });
            w.AddRecord("stand-in/items/lamp", new StandInItem { Title = "Lamp", Price = 3, Rarity = "Legendary" });
            w.AddRecord("stand-in/items/torch", new StandInItem { Title = "Torch", Price = -1 });
        });
        Start(off: ["quiet-nights.pak"]);
    }

    /// <summary>
    /// The mods checked as at a start (<see cref="GameMods.ApplyTo"/>'s filter, the settings' switches those of
    /// <paramref name="off"/> then): each in the mods folder, over no game data (the check's game side is step 6's).
    /// </summary>
    public void Start(string[] off)
    {
        foreach (var file in off)
            Parts.Settings.SetModOn(file, false);
        var options = Parts.Mods.ApplyTo(new ApplicationOptions(), new SettingsSnapshot(null, false, new HashSet<string>(off)));
        var files = Directory.Exists(ModsFolder) ? Directory.GetFiles(ModsFolder, "*.pak").OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal).ToList() : [];
        _ = options.ModFilter!(files, []).ToList();
    }

    // ---------- Slots ----------

    /// <summary>A time of the scenes (UTC).</summary>
    public static DateTime Utc(int month, int day, int hour, int minute) => new(2026, month, day, hour, minute, 0, DateTimeKind.Utc);

    /// <summary>The saves folder of the harness's per-user folder.</summary>
    public string Saves => Path.Combine(Harness.PlayerFolder, "saves");

    /// <summary>A character made in its slot and left (so it has a backup), its save and backup written at the times given; its slot.</summary>
    public string Character(string name, DateTime saved, DateTime? backup = null)
    {
        var slot = Session.New(name);
        Harness.Storage.Flush();
        Session.Leave();
        Harness.Storage.Flush();
        File.SetLastWriteTimeUtc(Path.Combine(Saves, slot + ".dat"), saved);
        File.SetLastWriteTimeUtc(Path.Combine(Saves, slot + ".dat.bak"), backup ?? saved.AddMinutes(-42));
        return slot;
    }

    /// <summary>A byte of <paramref name="file"/> flipped (the save no longer reads), its time kept.</summary>
    public static void Damage(string file)
    {
        var time = File.GetLastWriteTimeUtc(file);
        var bytes = File.ReadAllBytes(file);
        bytes[^1] ^= 0xFF;
        File.WriteAllBytes(file, bytes);
        File.SetLastWriteTimeUtc(file, time);
    }

    /// <summary>
    /// The slot list's scene: five characters, the last played first; Brother Cadfael's save damaged (his backup reads,
    /// saved 08:30), Mira of the Vale's save and backup both damaged (nothing reads).
    /// </summary>
    public void SeveralCharacters()
    {
        Character("Ser Alrik", Utc(11, 13, 14, 3));
        Character("Brother Cadfael", Utc(11, 12, 9, 12), Utc(11, 12, 8, 30));
        Character("Mira of the Vale", Utc(11, 10, 21, 47));
        Character("Wren", Utc(11, 8, 18, 30));
        Character("Old Tom Bombadil the Wanderer of the Hills", Utc(10, 30, 7, 5));
        Damage(Path.Combine(Saves, "brother-cadfael.dat"));
        Damage(Path.Combine(Saves, "mira-of-the-vale.dat"));
        Damage(Path.Combine(Saves, "mira-of-the-vale.dat.bak"));
        Harness.Frame();
    }

    // ---------- The picker's listing of the scenes' own ----------

    /// <summary>The scenes' home and Documents (not on this machine: the picker lists them from its seams).</summary>
    public const string Home = "/home/player", Documents = Home + "/Documents";

    /// <summary>The scenes' folders and their entries (folders first, then the .json files, as the picker lists them).</summary>
    public static readonly Dictionary<string, FolderEntry[]> Folders = MakeFolders();

    static Dictionary<string, FolderEntry[]> MakeFolders()
    {
        static FolderEntry Folder(string parent, string name) => new(name, parent + "/" + name, true, 0, Utc(11, 1, 9, 0), false);
        static FolderEntry File(string parent, string name, long size, DateTime time) => new(name, parent + "/" + name, false, size, time, false);
        var archive = Documents + "/Stat Fick archive";
        return new(StringComparer.Ordinal)
        {
            [Documents] =
            [
                Folder(Documents, "Letters"), Folder(Documents, "old saves"), Folder(Documents, "Stat Fick archive"),
                File(Documents, "ser-alrik-before-the-crypt.json", 64, Utc(11, 11, 20, 15)),
                File(Documents, "stat-fick-character.json", 64, Utc(11, 13, 14, 3)),
                File(Documents, "wren.json", 59, Utc(11, 8, 18, 31)),
            ],
            [Documents + "/old saves"] = [File(Documents + "/old saves", "emberfall-stat-block.json", 51, Utc(9, 3, 12, 0))],
            [Documents + "/Letters"] = [],
            [Home + "/Downloads"] = [File(Home + "/Downloads", "character (1).json", 1536, Utc(10, 2, 17, 44))],
            [Home] = [Folder(Home, "Documents"), Folder(Home, "Downloads")],
            ["/"] = [Folder("", "home")],
            [archive] = [.. Enumerable.Range(1, 2000).Select(i => File(archive, $"character-{i:0000}.json", 64, Utc(6, 1, 9, 0).AddMinutes(i * 37)))],
        };
    }

    /// <summary>The picker on the scenes' folders: Documents and home as above, the root "/", the listing and what is there the scenes'.</summary>
    public void SceneFolders()
    {
        Picker.Documents = () => Documents;
        Picker.Home = () => Home;
        Picker.Roots = () => ["/"];
        Picker.Listing = folder => Folders.TryGetValue(folder, out var entries) ? entries : throw new DirectoryNotFoundException(folder);
        Picker.FolderIsThere = Folders.ContainsKey;
        Picker.FileIsThere = file => Folders.Values.Any(entries => entries.Any(e => !e.IsFolder && e.FullPath == file));
    }

    /// <summary>The picker open now.</summary>
    public FilePicker OpenPicker => Picker.Open ?? throw new InvalidOperationException("no picker is open");
}
