using Statventure.Game.Data.Looks;
using Statventure.Game.Figures;
using Statventure.Game.Random;
using Statventure.Game.Settings;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.UI;

namespace Statventure.Game.Tests;

/// <summary>
/// The live views' test data (GM2 step 11): the game's committed packs (<c>data.pak</c> without its presets, so the tests
/// don't move as GM2 step 9 changes the real presets; the models packs, <c>models.pak</c> and a pack per race as the game
/// mounts them (<see cref="TestPaths.ModelsPacks"/>): the humanoid's bodies, parts, clip library and the
/// figures' state machine and step 10's nine emblems), and the live views' own data (<c>Reference/live-view-data/</c>,
/// written with <c>STATVENTURE_LIVE_VIEWS=write</c> from the shipped data as GM2 step 9a left it): the human race with its
/// swatches and two presets, copies of the game's first man's and first woman's ("Live man", "Live woman"), so a later
/// change to the shipped human's swatches or presets doesn't move them; and, in a scratch folder, a test's own records.
/// The engine's test shapes (<see cref="StandInEmblem"/>), which stood in for the emblems until step 10's were in, are only
/// test data now: an emblem of a test's own by a name of its own.
/// </summary>
sealed class LiveViewData : IDisposable
{
    public const string WriteVariable = "STATVENTURE_LIVE_VIEWS";

    static string Reference => Path.Combine(RaceOracle.Engine, "Tests", "Statventure.Tests", "Reference");

    /// <summary>The live views' own data: the human race and the tests' two presets.</summary>
    public static string Overlay => Path.Combine(Reference, "live-view-data");

    /// <summary>A model of the tests' as an emblem of a test's own (the engine's test shapes).</summary>
    public static string StandInEmblem => Path.Combine(RaceOracle.Engine, "Tests", "Statventure.Tests", "Data", "Models", "shapes.glb");

    public const string Man = "live-human-male-1", Woman = "live-human-female-1";

    static string Shipped => Path.Combine(RaceOracle.Engine, "Assets", "Data");

    /// <summary>The overlay's files as the shipped data makes them (path under the overlay, JSON form): the human race, and the first man's and woman's presets renamed.</summary>
    public static IEnumerable<(string Path, string Json)> OverlayFiles()
    {
        LookPreset Preset(string name) => RecordSerializer.FromJson<LookPreset>(File.ReadAllBytes(Path.Combine(Shipped, LookNames.PresetsFolder, name + ".json")));
        yield return ("data/races/human.json", File.ReadAllText(Path.Combine(Shipped, "data", "races", "human.json")));
        yield return ($"{LookNames.PresetsFolder}/{Woman}.json", RecordSerializer.ToJson(Preset("human-female-1") with { Label = "Live woman", Order = 50 }));
        yield return ($"{LookNames.PresetsFolder}/{Man}.json", RecordSerializer.ToJson(Preset("human-male-1") with { Label = "Live man", Order = 10 }));
    }

    static readonly Lazy<IReadOnlyDictionary<string, LookPreset>> PresetsRead = new(() => new Dictionary<string, LookPreset>
    {
        [Man] = RecordSerializer.FromJson<LookPreset>(File.ReadAllBytes(Path.Combine(Overlay, LookNames.PresetsFolder, Man + ".json"))),
        [Woman] = RecordSerializer.FromJson<LookPreset>(File.ReadAllBytes(Path.Combine(Overlay, LookNames.PresetsFolder, Woman + ".json"))),
    });

    /// <summary>The tests' presets (as the overlay holds them): the man (first) and the woman.</summary>
    public static IReadOnlyDictionary<string, LookPreset> Presets => PresetsRead.Value;

    /// <summary>The man's look (the game's first man: the average body, a face, a crop and stubble, in the human's colours).</summary>
    public static LookHero ManLook() => Presets[Man].Look;

    /// <summary>The woman's look (the game's first woman).</summary>
    public static LookHero WomanLook() => Presets[Woman].Look;

    /// <summary>The colour of the human's swatch <paramref name="name"/> for <paramref name="slot"/> ("#rrggbb", as the overlay's race has it).</summary>
    public static string Swatch(string slot, string name) =>
        RecordSerializer.FromJson<Statventure.Game.Data.Races.Race>(File.ReadAllBytes(Path.Combine(Overlay, "data", "races", "human.json")))
            .Swatches.Single(s => s.Slot == slot && s.Name == name).Colour;

    readonly ScratchFolder _scratch = new();
    readonly PackWithout _pack;
    readonly List<PackWithout> _models = [];

    /// <param name="emblems">Whether step 10's emblems are in the data (else the models packs without them: no emblem has a model).</param>
    /// <param name="machine">Whether the figures' state machine is in the data (else the models packs without it).</param>
    /// <param name="extra">Records of the test's own on top (path → record).</param>
    public LiveViewData(bool emblems = true, bool machine = true, IReadOnlyDictionary<string, object>? extra = null)
    {
        foreach (var (path, record) in extra ?? new Dictionary<string, object>())
            Write(path, record);
        _pack = new PackWithout(Path.Combine(TestPaths.Packs, "data.pak"), path => path.StartsWith(LookNames.PresetsFolder + "/", StringComparison.Ordinal));
        // Every models pack as the game mounts them (models.pak, then a pack per race: the user's Q77 of GM2).
        var without = (machine, emblems) switch
        {
            (true, true) => "",
            (false, true) => " without the figures' machine",
            (true, false) => " without the emblems",
            _ => " without the figures' machine and the emblems",
        };
        var mounts = new List<Mount> { Mount.Custom("data.pak without its presets", _pack) };
        foreach (var file in TestPaths.ModelsPacks)
        {
            var pack = new PackWithout(file,
                path => (!machine && path == LiveHeroViews.MachinePath) || (!emblems && path.StartsWith(Emblems.ModelFolder + "/", StringComparison.Ordinal)));
            _models.Add(pack);
            mounts.Add(Mount.Custom(Path.GetFileName(file) + without, pack));
        }
        mounts.Add(Mount.Folder(Overlay));
        mounts.Add(Mount.Folder(_scratch.Root));
        Data = new VirtualFileSystem(mounts);
    }

    public VirtualFileSystem Data { get; }

    void Write(string path, object record)
    {
        var file = Path.Combine(_scratch.Root, path + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, RecordSerializer.ToJson(record.GetType(), record));
    }

    /// <summary>A pack's entries but those <paramref name="left"/> picks.</summary>
    sealed class PackWithout(string pack, Func<string, bool> left) : IMountEntries, IDisposable
    {
        readonly PackFile _pack = PackFile.Open(pack);

        public IEnumerable<string> Paths => _pack.Entries.Select(e => e.Path).Where(path => !left(path));

        public bool IsRecord(string path) => _pack.Find(path)!.IsRecord;

        public byte[] Read(string path) => _pack.Read(_pack.Find(path)!);

        public void Dispose() => _pack.Dispose();
    }

    public void Dispose()
    {
        Data.Dispose();
        _pack.Dispose();
        foreach (var pack in _models)
            pack.Dispose();
        _scratch.Dispose();
    }
}

/// <summary>
/// The hero screens with live views (GM2 step 11): the start screen, the sheet, the race carousel and the maker attached as
/// the game attaches them (<see cref="HeroScreensHarness"/> with the sheet) on the game's document without a window, each
/// screen given <see cref="LiveHeroViews"/> of its own on the test's GPU device and the document's own
/// <see cref="SurfaceViews"/> (as the program's window host has them), the carousel and the maker reading
/// <see cref="LiveViewData"/>.
/// </summary>
sealed class LiveScreens : IDisposable
{
    public LiveScreens(GpuDevice device, LiveViewData data, float width = 1440, float height = 950, float scale = 1, GameTheme? theme = null,
        PortraitLight light = Portraits.Light, IGameRandom? random = null)
    {
        Device = device;
        H = new GameUiHarness(width, height, scale, theme);
        S = new HeroScreensHarness(H, random: random, sheet: true);
        Surfaces = new SurfaceViews(H.Document);
        Func<VirtualFileSystem?> read = () => data.Data;
        S.Races.Data = read;
        S.Maker.Data = read;
        Sheet = new LiveHeroViews(device, Surfaces, H.Document, H.Ui.Windows, read, light);
        Races = new LiveHeroViews(device, Surfaces, H.Document, H.Ui.Windows, read, light);
        Maker = new LiveHeroViews(device, Surfaces, H.Document, H.Ui.Windows, read, light);
        S.Sheet!.Views = Sheet;
        S.Races.Views = Races;
        S.Maker.Views = Maker;
    }

    public GpuDevice Device { get; }
    public GameUiHarness H { get; }
    public HeroScreensHarness S { get; }
    public SurfaceViews Surfaces { get; }
    public LiveHeroViews Sheet { get; }
    public LiveHeroViews Races { get; }
    public LiveHeroViews Maker { get; }

    /// <summary>Every live view of the three screens, by its surface's id.</summary>
    public IReadOnlyList<string> Live => [.. new[] { Sheet, Races, Maker }.SelectMany(v => v.Live).Select(s => s.Id).Order(StringComparer.Ordinal)];

    /// <summary>The sheet's, the carousel's figure's, its emblem's and the maker's surfaces.</summary>
    public (Statventure.UI.Dom.UiElement Sheet, Statventure.UI.Dom.UiElement Races, Statventure.UI.Dom.UiElement Emblem, Statventure.UI.Dom.UiElement Maker) Surface =>
        (H.Q("#hero-view"), H.Q("#races-figure"), H.Q("#races-emblem-view"), H.Q("#maker-figure"));

    /// <summary>The views rendered as the window's host renders them before it paints (those laid out and changed).</summary>
    public void Render()
    {
        var frame = Device.BeginFrame();
        Surfaces.Update(frame);
        Device.EndFrame(frame);
        Device.WaitIdle();
    }

    /// <summary>
    /// A hero of the human race with <paramref name="look"/> made (the new hero's carousel shown, then let go as the
    /// character is left), then opened again onto its sheet, as Continue opens a finished hero; its slot.
    /// </summary>
    public string ReadyHero(LookHero? look, string race = "human")
    {
        var slot = S.Session.New("Ser Alrik");
        S.Session.Change(save => save with { Hero = save.Hero with { Race = race }, Look = look });
        S.Session.Leave();
        S.Session.Open(slot);
        H.Frame();
        return slot;
    }

    public void Dispose()
    {
        Sheet.Dispose();
        Races.Dispose();
        Maker.Dispose();
        S.Dispose();
        H.Dispose();
        // (A test that only refuses looks has no device.)
        if (Device is { } device)
        {
            device.WaitIdle();
            device.Collect();
        }
    }
}
