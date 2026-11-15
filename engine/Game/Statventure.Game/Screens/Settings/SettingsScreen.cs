using System.Globalization;
using Statventure.Core;
using Statventure.Game.Mods;
using Statventure.Game.Settings;
using Statventure.Game.Sounds;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Settings;

/// <summary>
/// The settings screen with the mod list (GM1-3, GM1-4, GM1-10, GM1-12, GM1-13, Q3, Q15, Q18, Q19, Q22, Q30;
/// Docs/Game/Foundation.md, "The settings screen" and "Mods: the list and the check at start"; step 10); its command
/// <c>open-settings</c>. The old settings window (<c>old-game/index.html</c> 26–62: its header, and its "Sound volume" row's
/// look for each volume) grown: the master, music and effects volumes (a slider applies at once while it moves; let go,
/// the press sounds once at the new level, Q13), the music row's line for the theme playing (none in GM1, Q15), the theme,
/// the zoom (the menu's zoom commands, Electron's half levels), full screen, Dev Mode (with what it unlocks) and the mods
/// (<see cref="GameMods.Entries"/>: each in mounting order with its name, version, author, description, file, state and
/// switch, <see cref="GameMods.Switch"/>; a switch changed since the start says when it takes effect). Every change goes
/// through <see cref="GameSettings"/> at once (written to <c>settings.json</c>) and the screen follows
/// <see cref="GameSettings.Changed"/>, so a value changed elsewhere (F11, the zoom keys) shows as it is.
/// </summary>
public sealed class SettingsScreen : IGameScreen
{
    /// <summary>The command that opens the screen.</summary>
    public const string OpenCommand = "open-settings";

    /// <summary>The screen's sheet in <c>ui.pak</c>.</summary>
    public const string Stylesheet = "ui/styles/settings";

    /// <summary>What the music row says while no theme plays (Q15: no music in GM1).</summary>
    public const string NoTheme = "Current theme: none playing";

    /// <summary>What a mod's row says beside a switch changed since the start (Q18: no Reload).</summary>
    public const string NextStart = "Takes effect when the game is next started.";

    /// <summary>What the mod list says when there are no mods.</summary>
    public const string NoMods = "No mods. A mod is a .pak file in the mods folder of the game folder (Menu › Open game folder).";

    /// <summary>What Dev Mode unlocks (GM1-3, GM1-12, GM1-13; the old <c>editingEnabled</c>'s uses), and that it is one for every character.</summary>
    public const string DevModeHelp = "Unlocks editing the attributes, bonuses and money, the EXP form, restoring HP and Mana, adding spells and items " +
        "from the catalogues, the combat test, levelling without EXP and any race. The same for every character.";

    GameParts? _parts;
    UiElement? _layer;

    /// <summary>The screen's window (made at <see cref="Attach"/>), for the tests.</summary>
    internal UiElement Layer => _layer ?? throw new InvalidOperationException("the screen isn't attached");

    public void Attach(GameParts parts)
    {
        _parts = parts;
        parts.Ui.UseStylesheet(Stylesheet);
        _layer = Markup.Element(Frame);
        foreach (var close in _layer.Descendants().Where(e => e.HasAttribute("data-settings-close")))
            close.AddEventListener("click", _ => parts.Ui.Windows.Close(_layer));
        Volume(parts, "master-volume", v => parts.Settings.MasterVolume = v);
        Volume(parts, "music-volume", v => parts.Settings.MusicVolume = v);
        Volume(parts, "effects-volume", v => parts.Settings.EffectsVolume = v);
        var theme = Find("game-theme");
        theme.AddEventListener("change", _ => parts.Settings.Theme = theme.Value == "light" ? GameTheme.Light : GameTheme.Dark);
        Find("zoom-out").AddEventListener("click", _ => parts.Ui.Commands.Run("zoom-out"));
        Find("zoom-reset").AddEventListener("click", _ => parts.Ui.Commands.Run("zoom-reset"));
        Find("zoom-in").AddEventListener("click", _ => parts.Ui.Commands.Run("zoom-in"));
        var fullscreen = Find("game-fullscreen");
        fullscreen.AddEventListener("change", _ => parts.Settings.Fullscreen = fullscreen.Checked);
        var devMode = Find("dev-mode");
        devMode.AddEventListener("change", _ => parts.Settings.DevMode = devMode.Checked);
        parts.Settings.Changed += Changed;
        parts.Ui.Commands.Register(OpenCommand, _ => Open());
    }

    /// <summary>
    /// Opens the screen (its values as they are now, the focus on the first slider as the old window's), or leaves it as it
    /// is when open. Logged with what it shows (GM1-P21), read from the window itself: <see cref="ShownLines"/>.
    /// </summary>
    public void Open()
    {
        var parts = _parts ?? throw new InvalidOperationException("the screen isn't attached");
        if (parts.Ui.Windows.IsOpen(Layer))
            return;
        Show();
        parts.Ui.Windows.Open(Layer, Find("master-volume"));
        Log.Info("Settings", "the settings screen opened");
        foreach (var line in ShownLines())
            Log.Info("Settings", line);
    }

    /// <summary>
    /// What the window shows, as the log says it, read from its elements (so the log can't say one thing and the screen
    /// another): the values ("master 100%, music 100%, effects 60%, theme Dark, zoom 100%, full screen off, Dev Mode off":
    /// the percentages' texts, the theme's option, the boxes), then the mod list: "the mod list: none" when it says there
    /// are none, else a line per row in its order, "mod &lt;file&gt;: &lt;its state's words&gt;", and "; &lt;the next-start
    /// line&gt;" where the row shows it.
    /// </summary>
    internal IEnumerable<string> ShownLines()
    {
        static string OnOff(bool on) => on ? "on" : "off";
        yield return $"master {Find("master-volume-value").TextContent}, music {Find("music-volume-value").TextContent}, " +
            $"effects {Find("effects-volume-value").TextContent}, theme {Find("game-theme").SelectedOption?.TextContent}, " +
            $"zoom {Find("zoom-value").TextContent}, full screen {OnOff(Find("game-fullscreen").Checked)}, Dev Mode {OnOff(Find("dev-mode").Checked)}";
        var list = Find("settings-mod-list");
        if (list.ElementChildren.Any(e => e.HasClass("settings-mods-empty")))
            yield return "the mod list: none";
        foreach (var row in list.ElementChildren.Where(e => e.HasClass("settings-mod")))
        {
            string? Text(string name) => row.Descendants().FirstOrDefault(e => e.HasClass(name))?.TextContent;
            var next = Text("settings-mod-next-start");
            yield return $"mod {row.GetAttribute("data-mod")}: {Text("settings-mod-state")}{(next is null ? "" : "; " + next)}";
        }
    }

    UiElement Find(string id) => Layer.Descendants().First(e => e.Id == id);

    /// <summary>A volume's slider: applied while it moves (the old slider's <c>input</c>), the press when it is let go (its <c>change</c>), at the new level.</summary>
    void Volume(GameParts parts, string id, Action<double> set)
    {
        var slider = Find(id);
        slider.AddEventListener("input", _ =>
        {
            if (double.TryParse(slider.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
                set(Math.Clamp(percent / 100, 0, 1));
        });
        slider.AddEventListener("change", _ => parts.Ui.Sound(SoundNames.Press));
    }

    /// <summary>A setting changed (here or elsewhere: F11, the zoom keys) while the window is open: its control shows it.</summary>
    void Changed(SettingKey key)
    {
        if (_layer is null || _parts is not { } parts || !parts.Ui.Windows.IsOpen(_layer))
            return;
        var settings = parts.Settings;
        switch (key)
        {
            case SettingKey.MasterVolume:
                ShowVolume("master-volume", settings.MasterVolume);
                break;
            case SettingKey.MusicVolume:
                ShowVolume("music-volume", settings.MusicVolume);
                break;
            case SettingKey.EffectsVolume:
                ShowVolume("effects-volume", settings.EffectsVolume);
                break;
            case SettingKey.Theme:
                ShowTheme();
                break;
            case SettingKey.ZoomLevel:
                ShowZoom();
                break;
            case SettingKey.Fullscreen:
                Find("game-fullscreen").Checked = settings.Fullscreen;
                break;
            case SettingKey.DevMode:
                Find("dev-mode").Checked = settings.DevMode;
                break;
            case SettingKey.Mods:
                ShowSwitches();
                break;
        }
    }

    /// <summary>Puts the settings' values, and the mods, into the window.</summary>
    void Show()
    {
        var settings = _parts!.Settings;
        ShowVolume("master-volume", settings.MasterVolume);
        ShowVolume("music-volume", settings.MusicVolume);
        ShowVolume("effects-volume", settings.EffectsVolume);
        Find("settings-current-track").TextContent = NoTheme;
        ShowTheme();
        ShowZoom();
        Find("game-fullscreen").Checked = settings.Fullscreen;
        Find("dev-mode").Checked = settings.DevMode;
        ShowMods();
    }

    void ShowTheme() => Find("game-theme").Value = _parts!.Settings.Theme == GameTheme.Light ? "light" : "dark";

    /// <summary>The zoom as a percentage; each button disabled where it would do nothing (zoomed in or out as far as it goes, or at the actual size).</summary>
    void ShowZoom()
    {
        var level = _parts!.Settings.ZoomLevel;
        Find("zoom-value").TextContent = ZoomText(level);
        Find("zoom-out").ToggleAttribute("disabled", level <= GameSettings.MinZoomLevel);
        Find("zoom-in").ToggleAttribute("disabled", level >= GameSettings.MaxZoomLevel);
        Find("zoom-reset").ToggleAttribute("disabled", level == 0);
    }

    void ShowVolume(string id, double volume)
    {
        var percent = (int)Math.Round(volume * 100, MidpointRounding.AwayFromZero);
        var slider = Find(id);
        // While the slider is dragged its value is the player's; written back it stays the same.
        if (slider.Value != percent.ToString(CultureInfo.InvariantCulture))
            slider.Value = percent.ToString(CultureInfo.InvariantCulture);
        slider.SetAttribute("aria-valuetext", percent + "%");
        Find(id + "-value").TextContent = percent + "%";
    }

    /// <summary>A zoom level as the screen says it: its zoom (1.2 to its power) in whole percent ("110%", "500%").</summary>
    public static string ZoomText(double level) =>
        ((int)Math.Round(Math.Pow(1.2, level) * 100, MidpointRounding.AwayFromZero)).ToString(CultureInfo.InvariantCulture) + "%";

    /// <summary>The mod list: each mod in mounting order (<see cref="GameMods.Entries"/>), or a line saying there are none.</summary>
    void ShowMods()
    {
        var list = Find("settings-mod-list");
        list.ClearChildren();
        _switches.Clear();
        var entries = _parts!.Mods.Entries;
        if (entries.Count == 0)
        {
            list.Append("p", "settings-mods-empty", NoMods);
            return;
        }
        foreach (var entry in entries)
            list.Append(ModRow(entry));
    }

    /// <summary>The mods' switches as the settings have them now, and the note beside each one changed since the start (in place: the focus stays on the switch).</summary>
    void ShowSwitches()
    {
        foreach (var entry in _parts!.Mods.Entries)
        {
            if (!_switches.TryGetValue(entry.File, out var shown))
                continue;
            shown.Box.Checked = _parts.Settings.IsModOn(entry.File);
            var note = shown.Side.ElementChildren.FirstOrDefault(e => e.HasClass("settings-mod-next-start"));
            if (entry.SwitchedSinceStart && note is null)
                shown.Side.Append("p", "settings-mod-next-start", NextStart);
            else if (!entry.SwitchedSinceStart)
                note?.Remove();
        }
    }

    readonly Dictionary<string, (UiElement Box, UiElement Side)> _switches = new(StringComparer.Ordinal);

    /// <summary>
    /// A mod's row (the old catalogue's row, <c>.available-spell</c>, its stripe the state's colour): its name, its version,
    /// author and file, its description, its state (loaded, off, left out with its first problem and how many more), and
    /// its switch; under the switch, when it was changed since the start, when that takes effect.
    /// </summary>
    UiElement ModRow(ModEntry entry)
    {
        var state = entry.State switch
        {
            ModState.Loaded => "is-loaded",
            ModState.Off => "is-off",
            _ => "is-left-out",
        };
        var row = new UiElement("article") { ClassName = $"available-spell settings-mod {state}" };
        row.SetAttribute("data-mod", entry.File);
        var text = row.Append("div", "settings-mod-text");
        text.Append("strong", text: entry.Name);
        var meta = text.Append("span", "settings-mod-meta");
        var parts = new List<string>();
        if (entry.Version is { } version)
            parts.Add(version);
        if (entry.Author is { } author)
            parts.Add("by " + author);
        if (parts.Count > 0)
            meta.Append("span", "settings-mod-by", string.Join(" · ", parts) + " · ");
        meta.Append("span", "settings-mod-file", entry.File);
        if (entry.Description is { } description)
            text.Append("p", "settings-mod-description", description);
        text.Append("p", "settings-mod-state", StateText(entry));
        var side = row.Append("div", "settings-mod-side");
        var label = side.Append("label", "settings-check settings-mod-switch");
        var box = label.Append("input");
        box.SetAttribute("type", "checkbox");
        box.SetAttribute("aria-label", $"{entry.Name} on");
        box.Checked = _parts!.Settings.IsModOn(entry.File);
        label.AppendText("On");
        box.AddEventListener("change", _ => _parts!.Mods.Switch(entry.File, box.Checked));
        if (entry.SwitchedSinceStart)
            side.Append("p", "settings-mod-next-start", NextStart);
        _switches[entry.File] = (box, side);
        return row;
    }

    /// <summary>A mod's state in the list's words: "Loaded", "Off", "Left out: &lt;problem&gt; (and n more)".</summary>
    public static string StateText(ModEntry entry) => entry.State switch
    {
        ModState.Loaded => "Loaded",
        ModState.Off => "Off",
        _ => $"Left out: {entry.Problem}{(entry.MoreProblems > 0 ? $" (and {entry.MoreProblems} more)" : "")}",
    };

    /// <summary>
    /// The window: the old settings window's (<c>old-game/index.html</c> 26–62) header and frame, its "Sound volume" row's
    /// look for each of the three volumes, its sections' look for the rest; the battle rows join it in GM5.
    /// </summary>
    const string Frame = $$"""
        <div class="modal-layer settings-layer" id="settings-modal" hidden>
          <button class="modal-backdrop" type="button" data-settings-close aria-label="Close settings"></button>
          <section class="spell-picker-dialog settings-dialog" role="dialog" aria-modal="true" aria-labelledby="settings-title">
            <header class="dialog-header compact-dialog-header">
              <div><p class="eyebrow">PREFERENCES</p><h2 id="settings-title">Game <em>settings</em></h2></div>
              <button class="modal-close" type="button" data-settings-close aria-label="Close settings">×</button>
            </header>
            <div class="settings-content">
              <div class="settings-volume-heading">
                <label for="master-volume">Master volume</label>
                <output id="master-volume-value" for="master-volume">100%</output>
              </div>
              <input id="master-volume" class="settings-volume-slider" type="range" min="0" max="100" step="1" value="100" aria-describedby="master-volume-help" />
              <p id="master-volume-help">Every sound of the game. 0% is silent.</p>
              <div class="settings-section settings-volume">
                <div class="settings-volume-heading">
                  <label for="music-volume">Music volume</label>
                  <output id="music-volume-value" for="music-volume">100%</output>
                </div>
                <input id="music-volume" class="settings-volume-slider" type="range" min="0" max="100" step="1" value="100" aria-describedby="music-volume-help" />
                <p id="music-volume-help">Adjust regional and battle music. 0% is silent.</p>
                <p class="settings-current-track" id="settings-current-track"></p>
              </div>
              <div class="settings-section settings-volume">
                <div class="settings-volume-heading">
                  <label for="effects-volume">Effects volume</label>
                  <output id="effects-volume-value" for="effects-volume">60%</output>
                </div>
                <input id="effects-volume" class="settings-volume-slider" type="range" min="0" max="100" step="1" value="60" aria-describedby="effects-volume-help" />
                <p id="effects-volume-help">Buttons, windows and the game's events. 0% is silent.</p>
              </div>
              <div class="settings-section">
                <label for="game-theme">Theme</label>
                <select id="game-theme">
                  <option value="dark">Dark</option>
                  <option value="light">Light</option>
                </select>
              </div>
              <div class="settings-section">
                <div class="settings-volume-heading settings-zoom-heading">
                  <h3 class="settings-heading">Zoom</h3>
                  <output id="zoom-value">100%</output>
                </div>
                <div class="settings-zoom" role="group" aria-label="Zoom">
                  <button class="backup-button" id="zoom-out" type="button">Zoom Out</button>
                  <button class="backup-button" id="zoom-reset" type="button">Actual Size</button>
                  <button class="backup-button" id="zoom-in" type="button">Zoom In</button>
                </div>
                <p>Ctrl+Plus or Ctrl+= zooms in, Ctrl+- zooms out, Ctrl+0 is the actual size.</p>
              </div>
              <div class="settings-section">
                <label class="settings-check" for="game-fullscreen"><input id="game-fullscreen" type="checkbox" aria-describedby="game-fullscreen-help" />Full screen</label>
                <p id="game-fullscreen-help">F11 switches it too.</p>
              </div>
              <div class="settings-section">
                <label class="settings-check" for="dev-mode"><input id="dev-mode" type="checkbox" aria-describedby="dev-mode-help" />Dev Mode</label>
                <p id="dev-mode-help">{{DevModeHelp}}</p>
              </div>
              <div class="settings-section settings-mods">
                <h3 class="settings-heading">Mods</h3>
                <p>The .pak files in the mods folder, in the order they load. Switching one on or off takes effect when the game is next started.</p>
                <div class="settings-mod-list" id="settings-mod-list"></div>
              </div>
            </div>
          </section>
        </div>
        """;
}
