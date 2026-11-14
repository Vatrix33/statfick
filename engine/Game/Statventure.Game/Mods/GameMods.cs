using Statventure.Core;
using Statventure.Game.Data.Saves;
using Statventure.Game.Settings;
using Statventure.Packs;

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
/// <param name="Author"><c>mod/info</c>'s author, or null (none, or empty).</param>
/// <param name="Version"><c>mod/info</c>'s version, or null (none, or empty).</param>
/// <param name="Description"><c>mod/info</c>'s description, or null (none, or empty).</param>
/// <param name="State">Loaded, off or left out.</param>
/// <param name="Problem">Left out: its first problem in the editor's words (<see cref="ModProblem"/>: "entry: problem").</param>
/// <param name="MoreProblems">Left out: how many more problems it has.</param>
/// <param name="SwitchedSinceStart">Its switch changed this run (it takes effect at the next start, Q18).</param>
public sealed record ModEntry(string File, string Name, string? Author, string? Version, string? Description, ModState State,
    string? Problem = null, int MoreProblems = 0, bool SwitchedSinceStart = false);

/// <summary>
/// The mods (GM1-10, Q3, Q4, Q10, Q18; step 6; Docs/Game/Foundation.md, "Mods: the list and the check at start"). Before
/// <c>Run</c>, <see cref="ApplyTo"/> gives the engine's options the game's mod check (GM1-P13,
/// <see cref="ApplicationOptions.ModFilter"/>) with the switches the settings had then: at the start, as the engine mounts the
/// player's mods (in their files' name order, Q3), a mod switched off is neither checked nor mounted, and every other is
/// checked alone over the game's data by the engine's shared check (<see cref="ModCheck.Check"/>: what the editor refuses
/// to save, Q4) and mounted only when it passes, a broken one left out whole; each is logged
/// (<c>[Mods] &lt;file&gt;: loaded (&lt;name&gt; &lt;version&gt; by &lt;author&gt;)</c>, <c>: off</c>, <c>: left out:
/// &lt;problem&gt; (and n more)</c>). Then <see cref="Entries"/> and <see cref="LeftOut"/> for the list and the start
/// screen's line (Q10), and <see cref="Switch"/> for the list's switches, kept in the settings (<see cref="Attach"/>) and
/// taking effect at the next start (Q18). The game never reloads its data; were the engine to mount again, the check would
/// run again and the entries be that mount's.
/// </summary>
public sealed class GameMods
{
    /// <summary>The log's category for the mods' lines.</summary>
    public const string LogCategory = "Mods";

    /// <summary>The record kinds the game knows: the engine's and its data library's (the editor's, M12-9).</summary>
    public static RecordTypes GameKinds { get; } = RecordTypes.Of(typeof(RecordTypes).Assembly, typeof(SaveGame).Assembly);

    readonly RecordTypes _kinds;
    GameSettings? _settings;
    SettingsSnapshot _start = SettingsSnapshot.Defaults;
    IReadOnlyList<ModEntry> _entries = [];

    /// <summary>The game's mods, checked with the game's kinds (<see cref="GameKinds"/>).</summary>
    public GameMods()
        : this(GameKinds)
    {
    }

    /// <summary>Mods checked with <paramref name="kinds"/> (tests).</summary>
    internal GameMods(RecordTypes kinds) => _kinds = kinds;

    /// <summary>
    /// The engine's options with the game's mod check set (<see cref="ApplicationOptions.ModFilter"/>), the switches taken
    /// from <paramref name="settings"/> (what the settings said before <c>Run</c>: those of this start).
    /// </summary>
    public ApplicationOptions ApplyTo(ApplicationOptions options, SettingsSnapshot settings)
    {
        _start = settings;
        return options with { ModFilter = Filter };
    }

    /// <summary>The run's settings, once <c>Load</c> has made them: where <see cref="Switch"/> keeps the switches.</summary>
    public void Attach(GameSettings settings) => _settings = settings;

    /// <summary>
    /// Every mod in <c>mods/</c> in mounting order (the files' name order), with its state at this start: loaded, off, or
    /// left out with its first problem and how many more; <see cref="ModEntry.SwitchedSinceStart"/> where its switch now
    /// differs from this start's. Empty before the data is mounted.
    /// </summary>
    public IReadOnlyList<ModEntry> Entries =>
        [.. _entries.Select(e => _settings is { } settings && settings.IsModOn(e.File) != _start.IsModOn(e.File) ? e with { SwitchedSinceStart = true } : e)];

    /// <summary>The mods left out at this start (the start screen names them, Q10).</summary>
    public IReadOnlyList<ModEntry> LeftOut => [.. Entries.Where(e => e.State == ModState.LeftOut)];

    /// <summary>
    /// Switches the mod in <paramref name="file"/> (its file's name in <c>mods/</c>) on or off for the next start (Q3, Q18),
    /// kept in the settings; its entry says it was switched since the start while the switch differs from this start's.
    /// Nothing is mounted or left out now.
    /// </summary>
    public void Switch(string file, bool on)
    {
        if (_settings is not { } settings)
            throw new InvalidOperationException("the mods' switches are kept in the settings, which Attach gives at Load");
        settings.SetModOn(file, on);
    }

    /// <summary>
    /// The game's mod check (<see cref="ApplicationOptions.ModFilter"/>), at each mount of the data: each mod in mounting
    /// order switched off (not checked), or checked alone over <paramref name="game"/> and kept when it passes; the entries
    /// made anew and each mod logged.
    /// </summary>
    IEnumerable<string> Filter(IReadOnlyList<string> mods, IReadOnlyList<Mount> game)
    {
        var entries = new List<ModEntry>();
        var accepted = new List<string>();
        foreach (var mod in mods)
        {
            var file = Path.GetFileName(mod);
            if (!_start.IsModOn(file))
            {
                entries.Add(Entry(file, InfoOf(mod), ModState.Off));
                Log.Info(LogCategory, $"{file}: off");
                continue;
            }
            var report = ModCheck.Check(mod, game, _kinds);
            if (report.Passed)
            {
                var entry = Entry(file, report.Info, ModState.Loaded);
                entries.Add(entry);
                accepted.Add(mod);
                Log.Info(LogCategory, $"{file}: loaded ({Named(entry)})");
            }
            else
            {
                var more = report.Problems.Count - 1;
                entries.Add(Entry(file, report.Info, ModState.LeftOut) with { Problem = report.Problems[0].ToString(), MoreProblems = more });
                Log.Warning(LogCategory, $"{file}: left out: {report.Problems[0]}{(more > 0 ? $" (and {more} more)" : "")}");
            }
        }
        _entries = entries;
        return accepted;
    }

    /// <summary>A mod's entry: named from its info (else its file's name without <c>.pak</c>), its author, version and description when given.</summary>
    static ModEntry Entry(string file, ModInfo? info, ModState state) =>
        new(file, Given(info?.Name) ?? Path.GetFileNameWithoutExtension(file), Given(info?.Author), Given(info?.Version), Given(info?.Description), state);

    /// <summary>The log's words for a loaded mod: its name, its version, "by" its author, those given.</summary>
    static string Named(ModEntry entry) =>
        string.Join(" ", new[] { entry.Name, entry.Version, entry.Author is { } author ? "by " + author : null }.Where(part => part is not null));

    static string? Given(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

    /// <summary>
    /// A switched-off mod's info for its name in the list (Q3: each mod named from its <c>mod/info</c>), read without
    /// checking the mod: null when its pack or its info doesn't read.
    /// </summary>
    ModInfo? InfoOf(string mod)
    {
        try
        {
            using var pack = PackFile.Open(mod);
            return pack.Find(ModInfo.Path) is { } entry ? ModCheck.Read(pack, entry, _kinds).Record as ModInfo : null;
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
