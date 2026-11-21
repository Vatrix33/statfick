using Statventure.Core;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Data.Saves;
using Statventure.Game.Screens.Sheet;
using Statventure.Packs;
using Statventure.Render3D;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Races;

/// <summary>
/// The game's data as the new hero's flow reads it: the races in their order (<see cref="RaceCatalogue.Load"/>) and the
/// maker's records (<see cref="LookData.Load"/>); none without data.
/// </summary>
public sealed record HeroData(IReadOnlyList<RaceEntry> Races, LookData Looks)
{
    static readonly HeroData None = new([], LookData.Load(new VirtualFileSystem([])));

    VirtualFileSystem? _files;
    readonly Dictionary<string, IReadOnlySet<string>> _bodySlots = new(StringComparer.Ordinal);

    /// <summary>What <paramref name="data"/> (the game's) holds for the flow.</summary>
    public static HeroData Of(VirtualFileSystem? data) => data is null ? None : new(RaceCatalogue.Load(data), LookData.Load(data)) { _files = data };

    /// <summary>
    /// The palette slots the body <paramref name="body"/>'s model draws in (its materials' slots: what the body itself uses,
    /// GM2-P29's "the body uses its slot"; a part's are its record's <see cref="LookPart.Slots"/>), read from the model in
    /// the data once; none for a body the data hasn't or whose model doesn't read (its figure is refused anyway).
    /// </summary>
    public IReadOnlySet<string> SlotsOfBody(string body)
    {
        if (_bodySlots.TryGetValue(body, out var slots))
            return slots;
        slots = new HashSet<string>(StringComparer.Ordinal);
        if (_files is { } files && Looks.Bodies.TryGetValue(body, out var record))
        {
            try
            {
                slots = ModelData.Load(files, record.Model).Materials.Select(m => m.Slot).ToHashSet(StringComparer.Ordinal);
            }
            catch (DataFileException)
            {
            }
        }
        return _bodySlots[body] = slots;
    }

    /// <summary>The race <paramref name="id"/>, or null when the data hasn't it.</summary>
    public Race? Race(string id) => Races.FirstOrDefault(r => r.Id == id)?.Race;

    /// <summary>Whether the data has a look for <paramref name="race"/>: a body of it (the maker builds on one; none: nothing to choose).</summary>
    public bool HasLooks(string race) => Looks.Bodies.Values.Any(body => body.Race == race);

    /// <summary>The sheet's context with the races (the rules' <see cref="ActorRules.ChooseRace"/> reads them).</summary>
    public SheetContext Context => SheetContext.Plain.WithRaces(Races);
}

/// <summary>
/// The new hero's flow as the race carousel and the character maker share it (Q25–Q28, GM2-7; Docs/Game/Hero.md, "The new
/// hero's flow" and "How it works", step 8): name, race, maker, sheet. The commands it runs and answers; which hero still
/// needs it (<see cref="Needs"/>: the sheet opens only with a race and a look, Q26); which race can be taken
/// (<see cref="Refusal"/>); its two ends, the hero finished (<see cref="Finish"/>: the race chosen as the old
/// <c>selectRace</c> chooses it, the race and the look saved together) and the flow left (<see cref="LeaveCommand"/>: the
/// start screen for a hero not finished, else back to the sheet).
/// </summary>
public static class HeroFlow
{
    /// <summary>The race carousel's command (this step's): the carousel shown, on the race named by its argument if given.</summary>
    public const string RacesCommand = "open-races";

    /// <summary>The maker's command (this step's): the maker shown for the race its argument names.</summary>
    public const string MakerCommand = "open-maker";

    /// <summary>The sheet's command (GM2 step 7's, <see cref="SheetScreen"/>): the flow ends by asking for it, the hero saved.</summary>
    public const string SheetCommand = SheetScreen.OpenCommand;

    /// <summary>
    /// <b>Q26's rule</b>, the one place the game asks it (the sheet on every open and on <c>open-sheet</c>, the flow's
    /// screens): whether the open hero still needs the flow, the sheet opening only with a race and a look. No race yet, or
    /// no look for a race <paramref name="context"/> has as playable. A Dev Mode hero of an enemy-only race has no look and
    /// doesn't (Q27); nor does one whose race the data doesn't have (a mod's, switched off: it can't be told from an
    /// enemy-only one, so it gets the sheet, "Unknown race"). A playable race the data has no looks for still needs it
    /// (Q26's words; the carousel shows it, not choosable, Q53: such a hero comes only from a file or a mod switched off).
    /// </summary>
    public static bool Needs(SaveGame save, SheetContext context) =>
        save.Hero.Race is not { Length: > 0 } race || (save.Look is null && context.Races.TryGetValue(race, out var known) && known.Playable);

    /// <summary><see cref="Needs(SaveGame, SheetContext)"/> on the flow's data.</summary>
    public static bool Needs(SaveGame save, HeroData data) => Needs(save, data.Context);

    /// <summary>Why the open hero can't take <paramref name="race"/> now, the old <c>selectRace</c>'s refusals (<see cref="ActorRules.ChooseRace"/>), or null.</summary>
    public static RaceRefusal? Refusal(GameParts parts, HeroData data, string race) =>
        parts.Session.Current is { } save ? ActorRules.ChooseRace(save.Hero, data.Context, race, parts.Settings.DevMode).Refused : RaceRefusal.NotPlayable;

    /// <summary>
    /// The open hero finished as <paramref name="race"/> with <paramref name="look"/> (null: an enemy-only race chosen in Dev
    /// Mode, Q27): the race chosen as the old game chooses it (<see cref="ActorRules.ChooseRace"/>: refused outside Dev Mode
    /// for an enemy-only race or a hero who has another race; HP and Mana filled to the new maxima; the old appearance step
    /// chose again only a race that changed, so a look for the hero's own race keeps HP and Mana), then the race and the
    /// look saved together (<see cref="Session.GameSession.Change"/>). Null when saved, else why the race was refused (nothing
    /// changed).
    /// </summary>
    public static RaceRefusal? Finish(GameParts parts, HeroData data, string race, LookHero? look)
    {
        var save = parts.Session.Current ?? throw new InvalidOperationException("no character is open");
        var sheet = save.Hero;
        if (look is null || sheet.Race != race)
        {
            var choice = ActorRules.ChooseRace(sheet, data.Context, race, parts.Settings.DevMode);
            if (choice.Refused is { } refused)
                return refused;
            sheet = choice.Sheet;
        }
        parts.Session.Change(current => current with { Hero = sheet, Look = look });
        Log.Info("Hero", $"{parts.Session.CurrentSlot}: the hero is {(look is null ? $"a {race} with no look" : $"a {race}, {look.Sex}, body {look.Body}")}");
        return null;
    }

    /// <summary>
    /// The command that leaves the flow (its screens' ×): the start screen while the hero still needs the flow (Q26: an
    /// unfinished hero opens on the flow again), else the sheet (Dev Mode's change of race, Q27, given up).
    /// </summary>
    public static string LeaveCommand(GameParts parts, HeroData data) =>
        parts.Session.Current is { } save && !Needs(save, data) ? SheetCommand : StatFickGame.StartCommand;

    /// <summary>The words of a × that runs <paramref name="command"/> (<see cref="LeaveCommand"/>'s): its label and tooltip.</summary>
    public static void LabelLeave(UiElement button, string command)
    {
        var words = command == SheetCommand ? LeaveToSheet : LeaveToStart;
        button.SetAttribute("aria-label", words);
        button.SetAttribute("title", words);
    }

    /// <summary>The × 's words when it goes back to the start screen (a hero not finished).</summary>
    public const string LeaveToStart = "Back to the start screen";

    /// <summary>The × 's words when it goes back to the sheet (Dev Mode's change given up).</summary>
    public const string LeaveToSheet = "Back to the character sheet";

    /// <summary>Whether the flow's screens can open now: a hero open that still needs the flow, or any hero in Dev Mode (Q27; outside it the look is final, GM2-7).</summary>
    public static bool CanOpen(GameParts parts, HeroData data) =>
        parts.Session.Current is { } save && (parts.Settings.DevMode || Needs(save, data));
}
