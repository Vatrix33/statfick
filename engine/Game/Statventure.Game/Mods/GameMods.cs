using Statventure.Core;
using Statventure.Game.Settings;

namespace Statventure.Game.Mods;

/// <summary>A mod's state at this start (GM1-10, Q3, Q4).</summary>
public enum ModState
{
    Loaded,
    Off,
    LeftOut,
}

/// <summary>A mod as the mod list shows it (Docs/Game/Foundation.md, "Mods: the list and the check at start").</summary>
/// <param name="File">Its file's name in <c>mods/</c>.</param>
/// <param name="Name">Its name: <c>mod/info</c>'s, else the file's name without <c>.pak</c>.</param>
/// <param name="Author"><c>mod/info</c>'s author, or null.</param>
/// <param name="Version"><c>mod/info</c>'s version, or null.</param>
/// <param name="Description"><c>mod/info</c>'s description, or null.</param>
/// <param name="State">Loaded, off or left out.</param>
/// <param name="Problem">Left out: its first problem in the editor's words.</param>
/// <param name="MoreProblems">Left out: how many more problems it has.</param>
/// <param name="SwitchedSinceStart">Its switch changed this run (it takes effect at the next start, Q18).</param>
public sealed record ModEntry(string File, string Name, string? Author, string? Version, string? Description, ModState State,
    string? Problem = null, int MoreProblems = 0, bool SwitchedSinceStart = false);

/// <summary>
/// The mods (GM1-10, Q3, Q4, Q10, Q18; step 6): before <c>Run</c>, <see cref="ApplyTo"/> gives the engine's options the
/// shared mod check (GM1-P13) with the switches the settings had then, so each mod is checked at the start and a broken
/// one left out; then <see cref="Entries"/> and <see cref="LeftOut"/> for the list and the start screen's line, and
/// <see cref="Switch"/> for the list's switches (kept in the settings once <see cref="Attach"/> gave them). <b>Step 2's
/// stub:</b> the options unchanged (every mod mounts as before GM1), no entries.
/// </summary>
public sealed class GameMods
{
    GameSettings? _settings;

    /// <summary>The engine's options with the mods' check set, the switches taken from <paramref name="settings"/> (the stub: <paramref name="options"/> as they are).</summary>
    public ApplicationOptions ApplyTo(ApplicationOptions options, SettingsSnapshot settings) => options;

    /// <summary>The run's settings, once <c>Load</c> has made them: where <see cref="Switch"/> keeps the switches.</summary>
    public void Attach(GameSettings settings) => _settings = settings;

    /// <summary>Every mod in <c>mods/</c> in mounting order, with its state at this start.</summary>
    public IReadOnlyList<ModEntry> Entries { get; } = [];

    /// <summary>The mods left out at this start.</summary>
    public IReadOnlyList<ModEntry> LeftOut => Entries.Where(e => e.State == ModState.LeftOut).ToList();

    /// <summary>Switches the mod in <paramref name="file"/> on or off for the next start.</summary>
    public void Switch(string file, bool on) => _settings?.SetModOn(file, on);
}
