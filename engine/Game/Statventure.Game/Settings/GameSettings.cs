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
}

/// <summary>
/// The game's settings, <c>settings.json</c> (Docs/Game/Foundation.md, "<c>settings.json</c>"; GM1-P6; step 5): typed values,
/// each set raising <see cref="Changed"/> when it changes the value. <b>Step 2's stub:</b> in memory, the first start's
/// defaults (Q22, GM1-4, GM1-12), nothing read or written; <see cref="Peek"/> gives the defaults.
/// </summary>
public sealed class GameSettings
{
    double _master = 1, _music = 1, _effects = 0.6, _zoom;
    GameTheme _theme = GameTheme.Dark;
    bool _fullscreen, _devMode;
    WindowState? _window;
    string? _lastCharacter;
    readonly HashSet<string> _modsOff = new(StringComparer.Ordinal);
    readonly List<string> _recentPlaces = [];

    GameSettings()
    {
    }

    /// <summary>The settings in <paramref name="storage"/>'s folder, for the run (the stub: the defaults, in memory).</summary>
    public static GameSettings Load(GameStorage storage) => new();

    /// <summary>
    /// What <paramref name="gameFolder"/>'s settings say of the window and the mods, read before <c>Run</c> without the
    /// engine's storage and without writing anything (the stub: <see cref="SettingsSnapshot.Defaults"/>).
    /// </summary>
    public static SettingsSnapshot Peek(string gameFolder) => SettingsSnapshot.Defaults;

    /// <summary>After a set changed a value: which one.</summary>
    public event Action<SettingKey>? Changed;

    /// <summary>The master volume, 0–1 (1 on a first start).</summary>
    public double MasterVolume { get => _master; set => Set(ref _master, value, SettingKey.MasterVolume); }

    /// <summary>The music's volume, 0–1 (1 on a first start; kept and applied though no theme plays in GM1, Q15).</summary>
    public double MusicVolume { get => _music; set => Set(ref _music, value, SettingKey.MusicVolume); }

    /// <summary>The effects' volume, 0–1 (0.6 on a first start, Q22).</summary>
    public double EffectsVolume { get => _effects; set => Set(ref _effects, value, SettingKey.EffectsVolume); }

    /// <summary>The theme (dark on a first start, GM1-4).</summary>
    public GameTheme Theme { get => _theme; set => Set(ref _theme, value, SettingKey.Theme); }

    /// <summary>The page zoom's level in Electron's half steps (0 is 100 %).</summary>
    public double ZoomLevel { get => _zoom; set => Set(ref _zoom, value, SettingKey.ZoomLevel); }

    /// <summary>Full screen (off on a first start).</summary>
    public bool Fullscreen { get => _fullscreen; set => Set(ref _fullscreen, value, SettingKey.Fullscreen); }

    /// <summary>The window as the player left it (Q19), or null before it was ever kept.</summary>
    public WindowState? Window { get => _window; set => Set(ref _window, value, SettingKey.Window); }

    /// <summary>Dev Mode (off by default, GM1-12; the same for every character, GM1-13).</summary>
    public bool DevMode { get => _devMode; set => Set(ref _devMode, value, SettingKey.DevMode); }

    /// <summary>The slot Continue opens, or null.</summary>
    public string? LastCharacter { get => _lastCharacter; set => Set(ref _lastCharacter, value, SettingKey.LastCharacter); }

    /// <summary>Whether the mod in <paramref name="file"/> is switched on (all are, until switched off: Q3).</summary>
    public bool IsModOn(string file) => !_modsOff.Contains(file);

    /// <summary>Switches the mod in <paramref name="file"/> on or off for the next start.</summary>
    public void SetModOn(string file, bool on)
    {
        if (on ? _modsOff.Remove(file) : _modsOff.Add(file))
            Changed?.Invoke(SettingKey.Mods);
    }

    /// <summary>The file picker's recent folders, the latest first (ten at most).</summary>
    public IReadOnlyList<string> RecentPlaces => _recentPlaces;

    /// <summary>Puts <paramref name="folder"/> first among the recent places.</summary>
    public void AddRecentPlace(string folder)
    {
        if (_recentPlaces.Count > 0 && _recentPlaces[0] == folder)
            return;
        _recentPlaces.Remove(folder);
        _recentPlaces.Insert(0, folder);
        if (_recentPlaces.Count > 10)
            _recentPlaces.RemoveAt(10);
        Changed?.Invoke(SettingKey.RecentPlaces);
    }

    void Set<T>(ref T field, T value, SettingKey key)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        Changed?.Invoke(key);
    }
}
