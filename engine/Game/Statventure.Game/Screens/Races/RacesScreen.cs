using Statventure.Core;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Settings;
using Statventure.Game.Sounds;
using Statventure.Packs;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Races;

/// <summary>
/// The race carousel (Q28, Q36, Q41, Q27; Docs/Game/Hero.md, "The new hero's flow" and "How it works", step 8), the new
/// hero's second step after its name (Q25), in place of the old race dialog (<c>old-game/index.html</c> 302–331,
/// <c>renderRaces</c>): one race at a time, its figure large in a dark portrait panel (the race's first preset, idling in a
/// fixed three-quarter view: GM2 step 11) beside the old list's content for it (its emblem, live and slowly turning for
/// this race only, Q36; its name and kind; its eight modifiers; its passive's name and description), the previous and
/// next race by the stage's buttons and the arrow keys (round from the last to the first), every race of the list as a
/// small emblem under it to go to at once. The list is the playable races, and in Dev Mode every race (Q27), in the races'
/// order (Q43), following the setting while it shows. Its button takes the race: a playable race opens the maker
/// (<c>open-maker</c>; <c>race-chosen</c>, GM2-P17), an enemy-only one (Dev Mode) is chosen at once with no look and the
/// sheet opens (the old <c>selectRace</c>); a hero who has another race can change it only in Dev Mode ("Race locked", the
/// old refusal on its status line); a playable race with no look in the data is shown but not choosable ("No looks for
/// the &lt;race&gt; yet", the user's answer of 2026-11-16). A section on the page itself, as the start screen (no sheet under it: Q26); × leaves
/// the flow (<see cref="HeroFlow.LeaveCommand"/>). Its command: <c>open-races [race]</c>.
/// </summary>
public sealed class RacesScreen : IGameScreen
{
    /// <summary>The carousel's own sheet (<c>Assets/UI/styles/races.css</c>).</summary>
    public const string Stylesheet = "ui/styles/races";

    /// <summary>The carousel's markup: the old dialog's look (its leather header, "ANCESTRY", "Choose a <em>race</em>", the intro line), the stage and the strip filled for the race shown.</summary>
    internal const string Markup = """
        <section class="races-screen" id="races-screen" aria-labelledby="races-title" hidden>
          <div class="spell-picker-dialog race-dialog races-dialog">
            <header class="dialog-header compact-dialog-header">
              <div><p class="eyebrow">ANCESTRY</p><h2 id="races-title">Choose a <em>race</em></h2></div>
              <button class="modal-close races-leave" id="races-leave" type="button">×</button>
            </header>
            <p class="picker-intro race-intro races-intro"></p>
            <div class="races-stage">
              <button class="races-turn races-previous" id="races-previous" type="button" aria-label="Previous race" title="Previous race"></button>
              <article class="races-race" id="races-race">
                <div class="races-portrait"><surface class="races-figure" id="races-figure"></surface></div>
                <div class="races-details">
                  <header class="races-heading">
                    <span class="races-emblem" id="races-emblem"><surface class="races-emblem-view" id="races-emblem-view"></surface><span class="races-emblem-mark" aria-hidden="true">✦</span></span>
                    <div class="races-title"><h3 id="races-name"></h3><p class="races-kind" id="races-kind"></p></div>
                  </header>
                  <div class="race-stat-changes races-modifiers" id="races-modifiers"></div>
                  <p class="race-passive-preview races-passive" id="races-passive"></p>
                  <div class="races-actions"><button class="available-spell-add races-choose" id="races-choose" type="button"></button></div>
                </div>
              </article>
              <button class="races-turn races-next" id="races-next" type="button" aria-label="Next race" title="Next race"></button>
            </div>
            <div class="races-empty spell-list-empty" id="races-empty" hidden><span aria-hidden="true">✦</span><p>No races loaded.</p></div>
            <footer class="races-footer">
              <nav class="races-strip" id="races-strip" aria-label="Races"></nav>
              <p class="spellbook-status race-status races-status" id="races-status" role="status" aria-live="polite"></p>
            </footer>
          </div>
        </section>
        """;

    GameParts? _parts;
    UiElement? _screen, _race, _figure, _emblem, _emblemView, _name, _kind, _modifiers, _passive, _choose, _strip, _status, _stage, _empty, _leave;
    HeroData _data = HeroData.Of(null);
    List<RaceEntry> _shown = [];
    int _at;

    /// <summary>Tests: the game's data the carousel reads (the races, the presets the figure shows); the document's by default (the game's).</summary>
    internal Func<VirtualFileSystem?>? Data { get; set; }

    /// <summary>The 3D views (GM2 step 11's); none until then.</summary>
    internal IHeroViews Views { get; set; } = NoHeroViews.Instance;

    public void Attach(GameParts parts)
    {
        _parts = parts;
        var ui = parts.Ui;
        ui.UseStylesheet(Stylesheet);
        foreach (var node in Statventure.UI.Dom.Markup.Parse(Markup))
            ui.Document.Body.Append(node);
        var document = ui.Document;
        UiElement Q(string selector) => document.QuerySelector(selector)!;
        _screen = Q("#races-screen");
        _race = Q("#races-race");
        _figure = Q("#races-figure");
        _emblem = Q("#races-emblem");
        _emblemView = Q("#races-emblem-view");
        _name = Q("#races-name");
        _kind = Q("#races-kind");
        _modifiers = Q("#races-modifiers");
        _passive = Q("#races-passive");
        _choose = Q("#races-choose");
        _strip = Q("#races-strip");
        _status = Q("#races-status");
        _stage = Q("#races-screen .races-stage");
        _empty = Q("#races-empty");
        _leave = Q("#races-leave");
        Q("#races-screen .races-intro").TextContent = HeroTexts.RacesIntro;
        Q("#races-previous").AddEventListener("click", _ => Turn(-1));
        Q("#races-next").AddEventListener("click", _ => Turn(1));
        _choose.AddEventListener("click", _ => Choose());
        _leave.AddEventListener("click", _ => Leave());
        // On the document, so the keys browse wherever the focus is while the carousel is the page (a button disabled
        // under the focus, "Race locked", drops it), and not while a window is over it (the menu takes its own arrows).
        document.Root.AddEventListener<UiKeyboardEvent>("keydown", Arrows);
        ui.Commands.Register(HeroFlow.RacesCommand, Open, () => HeroFlow.CanOpen(parts, HeroData.Of(GameData)));
        // A character opened while the carousel shows was opened after the shown one was left (Closed).
        parts.Session.Closed += Hide;
        parts.Settings.Changed += key =>
        {
            if (key == SettingKey.DevMode && Showing)
                Fill(_shown.Count > 0 ? _shown[_at].Id : null);
        };
    }

    /// <summary>Whether the carousel shows.</summary>
    public bool Showing => _screen is { Hidden: false };

    /// <summary>The race shown (its id), while the carousel shows.</summary>
    public string? Shown => Showing && _shown.Count > 0 ? _shown[_at].Id : null;

    /// <summary>The races the carousel goes through (their ids), in order.</summary>
    public IReadOnlyList<string> List => [.. _shown.Select(r => r.Id)];

    VirtualFileSystem? GameData => Data is { } data ? data() : _parts!.Ui.Document.Files;

    /// <summary>
    /// <c>open-races [race]</c>: the carousel for the open hero, on the race named (the maker's "← All races" names its
    /// race), else the hero's own, else the first (its status line as that race has it), the focus on its button. (Each of the flow's
    /// screens hides itself before it runs the next one's command.)
    /// </summary>
    void Open(string? race)
    {
        var parts = _parts!;
        _screen!.Hidden = false;
        Fill(race ?? parts.Session.Current?.Hero.Race);
        HeroFlow.LabelLeave(_leave!, HeroFlow.LeaveCommand(parts, _data));
        Log.Info("Races", $"the race carousel opened on {Shown ?? "no race"} ({_shown.Count} races{(parts.Settings.DevMode ? ", Dev Mode" : "")})");
        parts.Ui.Document.UpdateLayout();
        (_choose!.IsFocusable ? _choose : _leave)!.Focus();
    }

    /// <summary>The list made again (the playable races, every race in Dev Mode) and the race <paramref name="race"/> shown (else the first).</summary>
    void Fill(string? race)
    {
        _data = HeroData.Of(GameData);
        var dev = _parts!.Settings.DevMode;
        _shown = [.. _data.Races.Where(r => r.Race.Playable || dev)];
        _at = Math.Max(0, _shown.FindIndex(r => r.Id == race));
        _strip!.TextContent = "";
        var empty = _shown.Count == 0;
        _empty!.Hidden = !empty;
        _stage!.Hidden = empty;
        if (empty)
        {
            Views.Hide(_figure!);
            Views.Hide(_emblemView!);
            return;
        }
        for (var i = 0; i < _shown.Count; i++)
        {
            var entry = _shown[i];
            // A rule wherever the list goes from the playable races to the enemy-only ones or back (Dev Mode: the races'
            // order puts the Merfolk after them).
            if (i > 0 && entry.Race.Playable != _shown[i - 1].Race.Playable)
                _strip.Append("span", "races-strip-divider").SetAttribute("aria-hidden", "true");
            var item = _strip.Append("button", "races-strip-item" + (entry.Race.Playable ? "" : " is-enemy"));
            item.SetAttribute("type", "button");
            item.SetAttribute("data-race", entry.Id);
            item.SetAttribute("title", entry.Race.Name);
            item.SetAttribute("aria-label", entry.Race.Name);
            item.SetAttribute("style", $"--race-accent: {entry.Race.Accent}");
            item.Append("span", "races-strip-mark", "✦").SetAttribute("aria-hidden", "true");
            if (entry.Race.Emblem is { } emblem)
                item.SetAttribute("data-emblem", emblem);
            var index = i;
            item.AddEventListener("click", _ => Show(index));
        }
        Show(_at);
    }

    /// <summary>The race at <paramref name="index"/> shown: its content, its kind, its button, the strip's mark, the views.</summary>
    void Show(int index)
    {
        var parts = _parts!;
        _at = index;
        var (id, race) = _shown[index];
        var hero = parts.Session.Current?.Hero;
        var own = hero is not null && hero.Race == id;
        var refusal = hero is null ? null : HeroFlow.Refusal(parts, _data, id);
        _race!.SetAttribute("data-race", id);
        _race.SetAttribute("style", $"--race-accent: {race.Accent}");
        _race.ToggleClass("is-enemy", !race.Playable);
        _name!.TextContent = race.Name;
        _kind!.TextContent = HeroTexts.Kind(race, own);
        if (race.Emblem is { } emblem)
            _emblem!.SetAttribute("data-emblem", emblem);
        else
            _emblem!.RemoveAttribute("data-emblem");
        _modifiers!.TextContent = "";
        foreach (var (key, name) in HeroTexts.Attributes)
        {
            var chip = _modifiers.Append("span");
            chip.Append("b", text: name);
            chip.AppendText(" " + HeroTexts.Modifier(race.StatModifiers.Values[key]));
        }
        _passive!.TextContent = "";
        _passive.Append("strong", text: race.Passive.Name + ":");
        _passive.AppendText(" " + race.Passive.Description);
        var locked = refusal == RaceRefusal.Locked;
        // A playable race with no look in the data is shown but not choosable (the user, 2026-11-16).
        var noLooks = !locked && race.Playable && !_data.HasLooks(id);
        var document = parts.Ui.Document;
        // The focus on the button follows the browsing: lost when the button is disabled under it, it comes back to it at
        // the next race that can be taken.
        var focusBack = document.FocusedElement is null || document.FocusedElement == document.Body || document.FocusedElement == _choose;
        _choose!.TextContent = HeroTexts.Choose(race, locked);
        _choose.ToggleAttribute("disabled", locked || noLooks);
        if (focusBack && !locked && !noLooks && Showing && document.FocusedElement != _choose)
            _choose.Focus();
        _status!.TextContent = locked ? HeroTexts.Refusal(RaceRefusal.Locked) : noLooks ? HeroTexts.NoLooks(race) : "";
        _status.ClassName = "spellbook-status race-status races-status" + (locked || noLooks ? " is-error" : "");
        foreach (var item in _strip!.ElementChildren.Where(e => e.HasClass("races-strip-item")))
        {
            var current = item.GetAttribute("data-race") == id;
            item.ToggleClass("is-current", current);
            if (current)
                item.SetAttribute("aria-current", "true");
            else
                item.RemoveAttribute("aria-current");
        }
        Views.ShowFigure(_figure!, id, FigureLook(id, hero));
        _emblem.ToggleClass("is-live", Views.ShowEmblem(_emblemView!, id, race.Emblem));
    }

    /// <summary>The look the carousel's figure wears: the hero's own for its race (Dev Mode), else the race's first preset (the design's), else none.</summary>
    LookHero? FigureLook(string race, ActorSheet? hero)
    {
        if (hero is not null && hero.Race == race && _parts!.Session.Current?.Look is { } own)
            return own;
        var looks = _data.Looks;
        return looks.PresetsOf(race) is [var first, ..] ? looks.Presets[first].Look with { Preset = first } : null;
    }

    /// <summary>The previous or next race, round from one end to the other.</summary>
    void Turn(int step)
    {
        if (_shown.Count == 0)
            return;
        Show((_at + step + _shown.Count) % _shown.Count);
    }

    /// <summary>Left and Right browse the races while the carousel shows and no window is over it; the key is taken.</summary>
    void Arrows(UiKeyboardEvent e)
    {
        if (!Showing || _parts!.Ui.Windows.Top is not null || e.CtrlKey || e.AltKey || e.MetaKey || e.ShiftKey || e.DefaultPrevented)
            return;
        var step = e.Key switch { "ArrowLeft" => -1, "ArrowRight" => 1, _ => 0 };
        if (step == 0)
            return;
        Turn(step);
        e.PreventDefault();
    }

    /// <summary>
    /// The race taken: a playable one opens the maker for it (<c>race-chosen</c>: the race is saved with the look), an
    /// enemy-only one (Dev Mode) is chosen at once with no look, saved, and the sheet asked for. The button can't be pressed
    /// for a race it refuses: it is disabled then ("Race locked", or no looks in the data), the carousel shows only while a
    /// hero is open, and Dev Mode is followed live, so the list never offers a race the old <c>selectRace</c> refuses
    /// otherwise.
    /// </summary>
    void Choose()
    {
        var parts = _parts!;
        var (id, race) = _shown[_at];
        parts.Ui.Sound(SoundNames.RaceChosen);
        Hide();
        if (race.Playable)
        {
            Log.Info("Races", $"{id} chosen: its look next");
            parts.Ui.Commands.Run(HeroFlow.MakerCommand, id);
            return;
        }
        HeroFlow.Finish(parts, _data, id, null);
        parts.Ui.Commands.Run(HeroFlow.SheetCommand);
    }

    /// <summary>× : the flow left (the start screen for a hero not finished, else the sheet).</summary>
    void Leave()
    {
        var command = HeroFlow.LeaveCommand(_parts!, _data);
        Hide();
        _parts!.Ui.Commands.Run(command);
    }

    void Hide()
    {
        if (_screen is not { Hidden: false } screen)
            return;
        screen.Hidden = true;
        Views.Hide(_figure!);
        Views.Hide(_emblemView!);
    }
}
