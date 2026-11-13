using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Statventure.Core;
using Statventure.Storage;

namespace Statventure.Game.Settings;

/// <summary>The theme (GM1-4): dark by default, both kept.</summary>
public enum GameTheme
{
    Dark,
    Light,
}

/// <summary>Which setting <see cref="GameSettings.Changed"/> says changed.</summary>
public enum SettingKey
{
    MasterVolume,
    MusicVolume,
    EffectsVolume,
    Theme,
    ZoomLevel,
    Fullscreen,
    Window,
    DevMode,
    LastCharacter,
    Mods,
    RecentPlaces,
}

/// <summary>The window as the player left it (Q19): its size in logical units (the size it is restored to when maximized) and whether it is maximized.</summary>
public readonly record struct WindowState(int Width, int Height, bool Maximized);

/// <summary>
/// What the application class reads before <c>Run</c>, when the engine's storage isn't made yet (<see cref="GameSettings.Peek"/>):
/// the window's state and full screen (Q19) for its window, the mods' switches (Q3) for the mods' check.
/// </summary>
/// <param name="Window">The window as the player left it, or null the first time (1440 × 950 then).</param>
/// <param name="Fullscreen">Full screen.</param>
/// <param name="ModsOff">The mods' files switched off.</param>
public sealed record SettingsSnapshot(WindowState? Window, bool Fullscreen, IReadOnlySet<string> ModsOff)
{
    /// <summary>A first start's: nothing kept yet.</summary>
    public static SettingsSnapshot Defaults { get; } = new(null, false, new HashSet<string>());

    /// <summary>Whether the mod in <paramref name="file"/> (its file's name in <c>mods/</c>) is switched on.</summary>
    public bool IsModOn(string file) => !ModsOff.Contains(file);

    // A record's own equality compares the set by reference; two snapshots of the same file are equal.
    public bool Equals(SettingsSnapshot? other) =>
        other is not null && Window == other.Window && Fullscreen == other.Fullscreen && ModsOff.SetEquals(other.ModsOff);

    public override int GetHashCode() => HashCode.Combine(Window, Fullscreen, ModsOff.Count);
}

/// <summary>
/// The game's settings, <c>settings.json</c> in the per-user folder (Docs/Game/Foundation.md, "<c>settings.json</c>";
/// GM1-P6; step 5), version <see cref="Version"/>, through the engine's <see cref="Statventure.Storage.Settings"/>: each
/// key read with its default (a missing key, a value of the wrong type, and, logged here, one out of its range, give the
/// default, Q43 of M9; unknown keys kept; a file that doesn't parse or is newer kept aside by the engine, Q18, Q42). The
/// typed values have setters: a set that changes a value writes its key (the engine's background writer) and raises
/// <see cref="Changed"/>; one that doesn't change it does nothing. <see cref="Peek"/> reads the file the same way before
/// the engine's storage exists, writing nothing.
/// <para>
/// One exception, the engine's <see cref="Statventure.Storage.Settings"/> having no way to make a key absent again (and
/// logging a stored null as the wrong type): <see cref="LastCharacter"/> cleared changes in memory and says so, but
/// nothing is written; the file keeps the old slot's name, which names no slot, and the session treats a
/// <c>lastCharacter</c> that names no slot as none (Docs/Game/Foundation.md, "How it works", step 5).
/// </para>
/// </summary>
public sealed class GameSettings
{
    /// <summary>The settings' version (written into the file; a new one only when a key's meaning changes, with its upgrade).</summary>
    public const int Version = 1;

    /// <summary>The keys in <c>settings.json</c> (camelCase, as the engine writes them).</summary>
    public const string MasterVolumeKey = "masterVolume", MusicVolumeKey = "musicVolume", EffectsVolumeKey = "effectsVolume",
        ThemeKey = "theme", ZoomLevelKey = "zoomLevel", FullscreenKey = "fullscreen", WindowKey = "window", DevModeKey = "devMode",
        LastCharacterKey = "lastCharacter", ModsKey = "mods", RecentPlacesKey = "recentPlaces";

    /// <summary>The first start's volumes (Q22): the effects lower.</summary>
    public const double FirstMasterVolume = 1, FirstMusicVolume = 1, FirstEffectsVolume = 0.6;

    /// <summary>The page zoom's limits, Electron's default limits of a page's zoom (as the editor's).</summary>
    public const double MinZoom = 0.5, MaxZoom = 3;

    /// <summary>The largest side a kept window may have (larger ones aren't a window any screen shows; a swapchain's limit).</summary>
    public const int MaxWindowSide = 16384;

    /// <summary>The upgrade of each past version to the next (none yet: version 1 is the first).</summary>
    static readonly IReadOnlyDictionary<int, Action<JsonObject>> Upgrades = new Dictionary<int, Action<JsonObject>>();

    readonly Statventure.Storage.Settings _file;
    double _master, _music, _effects, _zoom;
    GameTheme _theme;
    bool _fullscreen, _devMode;
    WindowState? _window;
    string? _lastCharacter;
    SortedSet<string> _modsOff;
    IReadOnlyList<string> _recentPlaces;

    GameSettings(Statventure.Storage.Settings file)
    {
        _file = file;
        var values = new StoredValues(file);
        _master = Volume(values, MasterVolumeKey, FirstMasterVolume);
        _music = Volume(values, MusicVolumeKey, FirstMusicVolume);
        _effects = Volume(values, EffectsVolumeKey, FirstEffectsVolume);
        _theme = values.Get(ThemeKey, GameTheme.Dark);
        _zoom = values.Get(ZoomLevelKey, 0.0);
        if (!IsZoomLevel(_zoom))
            _zoom = values.OutOfRange(ZoomLevelKey, _zoom, $"a half step whose zoom is from {MinZoom} to {MaxZoom} times", 0.0);
        _fullscreen = values.Get(FullscreenKey, false);
        _window = ReadWindow(values);
        _devMode = values.Get(DevModeKey, false);
        _lastCharacter = values.Get<string?>(LastCharacterKey, null);
        if (_lastCharacter is { } slot && !SaveSlots.IsValidName(slot))
            _lastCharacter = values.OutOfRange<string?>(LastCharacterKey, slot, "a slot's name", null);
        _modsOff = ReadModsOff(values);
        _recentPlaces = new RecentPlaces(values.Get<List<string>?>(RecentPlacesKey, null)).Folders.ToList();
    }

    /// <summary>The settings in <paramref name="storage"/>'s folder (<c>settings.json</c>), for the run.</summary>
    public static GameSettings Load(GameStorage storage) => new(storage.LoadSettings(Version, Upgrades));

    /// <summary>
    /// What <paramref name="gameFolder"/>'s <c>settings.json</c> says of the window, full screen and the mods, read before
    /// <c>Run</c> without the engine's storage: by the same rules as <see cref="Load"/> (a file that can't be used gives
    /// <see cref="SettingsSnapshot.Defaults"/>), but writing nothing, making no folder and logging nothing (<see cref="Load"/>
    /// reads the file again once the log is open, and keeps a broken one aside).
    /// </summary>
    public static SettingsSnapshot Peek(string gameFolder)
    {
        if (PeekedValues.Read(Path.Combine(gameFolder, GameStorage.SettingsName)) is not { } values)
            return SettingsSnapshot.Defaults;
        return new SettingsSnapshot(ReadWindow(values), values.Get(FullscreenKey, false), ReadModsOff(values));
    }

    /// <summary>After a set changed a value: which one.</summary>
    public event Action<SettingKey>? Changed;

    /// <summary>The master volume, 0–1 (1 on a first start).</summary>
    public double MasterVolume { get => _master; set => Store(ref _master, CheckedVolume(value), MasterVolumeKey, SettingKey.MasterVolume); }

    /// <summary>The music's volume, 0–1 (1 on a first start; kept and applied though no theme plays in GM1, Q15).</summary>
    public double MusicVolume { get => _music; set => Store(ref _music, CheckedVolume(value), MusicVolumeKey, SettingKey.MusicVolume); }

    /// <summary>The effects' volume, 0–1 (0.6 on a first start, Q22).</summary>
    public double EffectsVolume { get => _effects; set => Store(ref _effects, CheckedVolume(value), EffectsVolumeKey, SettingKey.EffectsVolume); }

    /// <summary>The theme (dark on a first start, GM1-4); <c>"dark"</c> or <c>"light"</c> in the file.</summary>
    public GameTheme Theme
    {
        get => _theme;
        set => Store(ref _theme, Enum.IsDefined(value) ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "not a theme"), ThemeKey, SettingKey.Theme);
    }

    /// <summary>
    /// The page zoom's level (1.2 to its power is the zoom, as Chromium's), in Electron's half steps between
    /// <see cref="MinZoom"/> and <see cref="MaxZoom"/> (−3.5 to 6); 0 (100 %) on a first start. Only such a level can be set.
    /// </summary>
    public double ZoomLevel
    {
        get => _zoom;
        set => Store(ref _zoom, IsZoomLevel(value) ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "not a zoom level (a half step whose zoom is from 0.5 to 3 times)"),
            ZoomLevelKey, SettingKey.ZoomLevel);
    }

    /// <summary>Whether <paramref name="level"/> is a zoom level the game has: a half step, its zoom within <see cref="MinZoom"/> and <see cref="MaxZoom"/>.</summary>
    public static bool IsZoomLevel(double level) => level * 2 == Math.Round(level * 2) && Math.Pow(1.2, level) is >= MinZoom and <= MaxZoom;

    /// <summary>Full screen (off on a first start).</summary>
    public bool Fullscreen { get => _fullscreen; set => Store(ref _fullscreen, value, FullscreenKey, SettingKey.Fullscreen); }

    /// <summary>
    /// The window as the player left it (Q19), or null before it was ever kept. A size set is held within the window's
    /// smallest (<see cref="StatFickGame.MinWidth"/> × <see cref="StatFickGame.MinHeight"/>) and <see cref="MaxWindowSide"/>,
    /// so what is kept is always a window the next start can open; one read outside them gives null (logged). Once kept
    /// it can't be cleared (null is refused).
    /// </summary>
    public WindowState? Window
    {
        get => _window;
        set
        {
            if (value is not { } state)
                throw new ArgumentNullException(nameof(value), "the window's state, once kept, isn't cleared");
            WindowState? held = state with
            {
                Width = Math.Clamp(state.Width, StatFickGame.MinWidth, MaxWindowSide),
                Height = Math.Clamp(state.Height, StatFickGame.MinHeight, MaxWindowSide),
            };
            Store(ref _window, held, WindowKey, SettingKey.Window);
        }
    }

    /// <summary>Dev Mode (off by default, GM1-12; the same for every character, GM1-13).</summary>
    public bool DevMode { get => _devMode; set => Store(ref _devMode, value, DevModeKey, SettingKey.DevMode); }

    /// <summary>
    /// The slot Continue opens (a slot's name, <see cref="SaveSlots.IsValidName"/>), or null. Cleared (null), it changes
    /// in memory only: the engine's settings can't make the key absent again, so the file keeps the old name, which names
    /// no slot (the session takes such a name as none).
    /// </summary>
    public string? LastCharacter
    {
        get => _lastCharacter;
        set
        {
            if (value is not null && SaveSlots.NameProblem(value) is { } problem)
                throw new ArgumentException($"\"{value}\" isn't a slot's name: {problem}", nameof(value));
            if (value is null)
            {
                if (_lastCharacter is null)
                    return;
                _lastCharacter = null;
                Changed?.Invoke(SettingKey.LastCharacter);
                return;
            }
            Store(ref _lastCharacter, value, LastCharacterKey, SettingKey.LastCharacter);
        }
    }

    /// <summary>Whether the mod in <paramref name="file"/> is switched on (all are, until switched off: Q3).</summary>
    public bool IsModOn(string file) => !_modsOff.Contains(file);

    /// <summary>
    /// Switches the mod in <paramref name="file"/> (its file's name in <c>mods/</c>) on or off for the next start; kept in
    /// <c>mods</c> as <c>{ "&lt;file&gt;": { "on": false } }</c> for each one off, in the files' ordinal order.
    /// </summary>
    public void SetModOn(string file, bool on)
    {
        ArgumentException.ThrowIfNullOrEmpty(file);
        var off = new SortedSet<string>(_modsOff, StringComparer.Ordinal);
        if (!(on ? off.Remove(file) : off.Add(file)))
            return;
        _file.Set(ModsKey, off.ToDictionary(f => f, _ => new ModSwitch(false), StringComparer.Ordinal));
        _modsOff = off;
        Changed?.Invoke(SettingKey.Mods);
    }

    /// <summary>The file picker's recent folders, the latest first (ten at most: the engine's <see cref="Statventure.Storage.RecentPlaces"/>).</summary>
    public IReadOnlyList<string> RecentPlaces => _recentPlaces;

    /// <summary>Puts <paramref name="folder"/> (made a full path) first among the recent places, as <see cref="Statventure.Storage.RecentPlaces.Add"/> does.</summary>
    public void AddRecentPlace(string folder)
    {
        var places = new RecentPlaces(_recentPlaces);
        places.Add(folder);
        if (places.Folders.SequenceEqual(_recentPlaces, StringComparer.Ordinal))
            return;
        var list = places.Folders.ToList();
        _file.Set(RecentPlacesKey, list);
        _recentPlaces = list;
        Changed?.Invoke(SettingKey.RecentPlaces);
    }

    void Store<T>(ref T field, T value, string key, SettingKey changed)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        _file.Set(key, value);
        field = value;
        Changed?.Invoke(changed);
    }

    static double CheckedVolume(double value) =>
        value is >= 0 and <= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, "a volume is from 0 to 1");

    static double Volume(SettingValues values, string key, double first)
    {
        var volume = values.Get(key, first);
        return volume is >= 0 and <= 1 ? volume : values.OutOfRange(key, volume, "from 0 to 1", first);
    }

    static WindowState? ReadWindow(SettingValues values)
    {
        if (values.Get<WindowState?>(WindowKey, null) is not { } window)
            return null;
        return window.Width is >= StatFickGame.MinWidth and <= MaxWindowSide && window.Height is >= StatFickGame.MinHeight and <= MaxWindowSide
            ? window
            : values.OutOfRange<WindowState?>(WindowKey, window, $"a size from {StatFickGame.MinWidth} × {StatFickGame.MinHeight} to {MaxWindowSide} × {MaxWindowSide}", null);
    }

    static SortedSet<string> ReadModsOff(SettingValues values) =>
        new(values.Get<Dictionary<string, ModSwitch>?>(ModsKey, null)?.Where(m => !m.Value.On).Select(m => m.Key) ?? [], StringComparer.Ordinal);

    /// <summary>A mod's entry in <c>mods</c>: <c>{ "on": false }</c> (a missing <c>on</c> is on).</summary>
    sealed record ModSwitch(bool On = true);

    /// <summary>Where the typed values are read from: the engine's settings (<see cref="Load"/>) or the file peeked at (<see cref="Peek"/>).</summary>
    abstract class SettingValues
    {
        /// <summary>The key's value; <paramref name="fallback"/> when it is missing, null or of the wrong type.</summary>
        public abstract T Get<T>(string key, T fallback);

        /// <summary>A value of the right type outside its range: <paramref name="fallback"/>, the line logged by <see cref="Load"/>.</summary>
        public abstract T OutOfRange<T>(string key, object? value, string range, T fallback);
    }

    /// <summary>The engine's settings: its own rules for missing keys and wrong types (logged there), the ranges logged here.</summary>
    sealed class StoredValues(Statventure.Storage.Settings file) : SettingValues
    {
        public override T Get<T>(string key, T fallback) => file.Get(key, fallback);

        public override T OutOfRange<T>(string key, object? value, string range, T fallback)
        {
            Log.Warning("Settings", $"settings {file.File}: \"{key}\" holds {JsonSerializer.Serialize(value, PeekedValues.Options)}, not {range}; its default is used");
            return fallback;
        }
    }

    /// <summary>
    /// The file read as the engine's <see cref="Statventure.Storage.Settings"/> reads it (its JSON options; a byte order
    /// mark allowed; an object with a whole-number <c>version</c> from 1 to <see cref="Version"/>, upgraded), silently.
    /// </summary>
    sealed class PeekedValues(JsonObject values) : SettingValues
    {
        /// <summary>The engine's options for setting values (<c>Settings</c>' own): camelCase, enums by camelCase name, strict numbers.</summary>
        internal static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
            NumberHandling = JsonNumberHandling.Strict,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>The settings in <paramref name="file"/>, or null when there are none to use (missing, unreadable, broken, newer).</summary>
        public static PeekedValues? Read(string file)
        {
            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(file);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return null;
            }
            ReadOnlySpan<byte> text = bytes;
            if (text.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
                text = text[3..];
            try
            {
                if (JsonNode.Parse(text, documentOptions: new JsonDocumentOptions { AllowDuplicateProperties = false }) is not JsonObject root
                    || !root.TryGetPropertyValue(Statventure.Storage.Settings.VersionKey, out var version)
                    || version is not JsonValue number || number.GetValueKind() != JsonValueKind.Number
                    || !number.TryGetValue(out int found) || found < 1 || found > Version)
                    return null;
                root.Remove(Statventure.Storage.Settings.VersionKey);
                for (var past = found; past < Version; past++)
                    Upgrades[past](root);
                return new PeekedValues(root);
            }
            catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException or FormatException)
            {
                return null;
            }
        }

        public override T Get<T>(string key, T fallback)
        {
            if (!values.TryGetPropertyValue(key, out var node) || node is null)
                return fallback;
            try
            {
                return node.Deserialize<T>(Options) is { } value ? value : fallback;
            }
            catch (Exception e) when (e is JsonException or InvalidOperationException or NotSupportedException or FormatException or ArgumentException)
            {
                return fallback;
            }
        }

        public override T OutOfRange<T>(string key, object? value, string range, T fallback) => fallback;
    }
}
