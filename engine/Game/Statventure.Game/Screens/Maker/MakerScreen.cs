using Statventure.Core;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Figures;
using Statventure.Game.Screens.Races;
using Statventure.Game.Sounds;
using Statventure.Packs;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Maker;

/// <summary>
/// The character maker (GM2-1, GM2-6, GM2-7; Q13–Q24, Q41; Docs/Game/Hero.md, "The new hero's flow", "The maker's parts
/// model" and "How it works", step 8), the new hero's third step after its race (Q25), in place of the old appearance
/// step (<c>old-game/index.html</c> 318–329, <c>app.js</c> 3070–3152): the race's presets as a list (Q21; the old labels,
/// Q20; male then female, as the old step's two rows) with the random button under them (every choice drawn, no locks;
/// <c>maker-random</c>), the hero's figure large in a dark portrait panel (idling in a fixed three-quarter view: GM2 step
/// 11), and the choices: the sex and the build, then each category the body offers (Q16, Q19, Q22–Q24) and each colour the
/// race has swatches for (Q17), each a row showing its choice that opens to its named options (one row open at a time);
/// a choice by click or by the arrow keys among its options (the old step's), <c>maker-choice</c> when it changes
/// (GM2-P17). "Begin as &lt;race&gt;" (the old words; "Use this look" for the hero's own race in Dev Mode) saves the race
/// and the look together and asks for the sheet (<c>hero-confirmed</c>); "← All races" goes back to the carousel; × leaves
/// the flow (<see cref="HeroFlow.LeaveCommand"/>). Its command: <c>open-maker &lt;race&gt;</c>.
/// </summary>
public sealed class MakerScreen : IGameScreen
{
    /// <summary>The maker's own sheet (<c>Assets/UI/styles/maker.css</c>).</summary>
    public const string Stylesheet = "ui/styles/maker";

    /// <summary>The maker's markup: the old dialogs' look, the old appearance step's heading, "← All races" and summary; the lists filled for the look.</summary>
    internal const string Markup = """
        <section class="maker-screen" id="maker-screen" aria-labelledby="maker-title" hidden>
          <div class="spell-picker-dialog maker-dialog">
            <header class="dialog-header compact-dialog-header">
              <div><p class="eyebrow" id="maker-eyebrow"></p><h2 id="maker-title">Choose your <em>look</em></h2></div>
              <button class="modal-close maker-leave" id="maker-leave" type="button" data-key="leave">×</button>
            </header>
            <div class="maker-toolbar">
              <button class="text-button maker-back" id="maker-back" type="button" data-key="back"><span class="maker-back-arrow" aria-hidden="true">←</span> All races</button>
              <p class="maker-intro" id="maker-intro"></p>
            </div>
            <div class="maker-body">
              <aside class="maker-presets" aria-labelledby="maker-presets-title">
                <p class="eyebrow maker-presets-title" id="maker-presets-title">PRESETS</p>
                <div class="maker-preset-list" id="maker-presets"></div>
                <button class="backup-button maker-random" id="maker-random" type="button" data-key="random">Random look</button>
              </aside>
              <div class="maker-portrait"><surface class="maker-figure" id="maker-figure"></surface></div>
              <div class="maker-choices" id="maker-choices"></div>
            </div>
            <footer class="maker-footer">
              <p class="maker-summary" id="maker-summary"></p>
              <button class="available-spell-add maker-begin" id="maker-begin" type="button" data-key="begin"></button>
            </footer>
            <p class="spellbook-status maker-status" id="maker-status" role="status" aria-live="polite"></p>
          </div>
        </section>
        """;

    GameParts? _parts;
    UiElement? _screen, _eyebrow, _presets, _figure, _choices, _summary, _begin, _status, _random, _leave;
    HeroData _data = HeroData.Of(null);
    LookMaker? _maker;
    string? _open;

    /// <summary>Tests: the game's data the maker reads (the races, bodies, parts and presets); the document's by default (the game's).</summary>
    internal Func<VirtualFileSystem?>? Data { get; set; }

    /// <summary>The 3D views: the game's live ones on its window (GM2 step 11, <see cref="LiveHeroViews"/>), none on a document without one; a test's own.</summary>
    internal IHeroViews Views { get; set; } = NoHeroViews.Instance;

    public void Attach(GameParts parts)
    {
        _parts = parts;
        if (Views is NoHeroViews)
            Views = LiveHeroViews.For(parts, () => GameData);
        var ui = parts.Ui;
        ui.UseStylesheet(Stylesheet);
        foreach (var node in Statventure.UI.Dom.Markup.Parse(Markup))
            ui.Document.Body.Append(node);
        var document = ui.Document;
        UiElement Q(string selector) => document.QuerySelector(selector)!;
        _screen = Q("#maker-screen");
        _eyebrow = Q("#maker-eyebrow");
        _presets = Q("#maker-presets");
        _figure = Q("#maker-figure");
        _choices = Q("#maker-choices");
        _summary = Q("#maker-summary");
        _begin = Q("#maker-begin");
        _status = Q("#maker-status");
        _random = Q("#maker-random");
        _leave = Q("#maker-leave");
        Q("#maker-intro").TextContent = HeroTexts.MakerIntro;
        Q("#maker-back").AddEventListener("click", _ => Back());
        _leave.AddEventListener("click", _ => Leave());
        _random.AddEventListener("click", _ => Roll());
        _begin.AddEventListener("click", _ => Begin());
        _screen.AddEventListener<UiKeyboardEvent>("keydown", Arrows);
        ui.Commands.Register(HeroFlow.MakerCommand, Open, () => HeroFlow.CanOpen(parts, HeroData.Of(GameData)));
        // A character opened while the maker shows was opened after the shown one was left (Closed).
        parts.Session.Closed += Hide;
    }

    /// <summary>Whether the maker shows.</summary>
    public bool Showing => _screen is { Hidden: false };

    /// <summary>The look as chosen now, while the maker shows (null: none, or the race has no body).</summary>
    public LookHero? Look => Showing ? _maker?.Look : null;

    /// <summary>The race the maker is for, while it shows.</summary>
    public string? Race => Showing ? _maker?.RaceId : null;

    /// <summary>The row open (its choice's key: "part:hair", "colour:skin"), if any.</summary>
    public string? OpenRow => _open;

    VirtualFileSystem? GameData => Data is { } data ? data() : _parts!.Ui.Document.Files;

    /// <summary>
    /// <c>open-maker &lt;race&gt;</c>: the maker for a playable race the hero can take and the data has looks for (else
    /// logged and nothing shown: no such race, an enemy-only one, one with no looks, the user's answer of 2026-11-16, and
    /// the old <c>selectRace</c>'s refusals), starting from the hero's own look for its own race (Dev Mode: the old "Use
    /// this look"), else the race's first preset, else a plain look; no row open; the focus on the first preset (or Begin).
    /// </summary>
    void Open(string? race)
    {
        var parts = _parts!;
        var data = HeroData.Of(GameData);
        var entry = data.Races.FirstOrDefault(r => r.Id == race);
        var problem = race is null ? "no race given" : entry is null ? $"no race {race}" : !entry.Race.Playable ? $"the {race} is no playable race"
            : !data.HasLooks(entry.Id) ? HeroTexts.NoLooks(entry.Race)
            : HeroFlow.Refusal(parts, data, entry.Id) is { } refused ? $"{entry.Id}: {HeroTexts.Refusal(refused)}" : null;
        if (problem is not null)
        {
            Log.Warning("Maker", $"open-maker: {problem}, nothing done");
            return;
        }
        _data = data;
        _maker = new LookMaker(entry!.Id, entry.Race, data.Looks, parts.Session.Current!.Look, data.SlotsOfBody);
        _open = null;
        _status!.TextContent = "";
        _status.ClassName = "spellbook-status maker-status";
        _eyebrow!.TextContent = HeroTexts.MakerEyebrow(entry.Race);
        _screen!.SetAttribute("style", $"--race-accent: {entry.Race.Accent}");
        _screen.SetAttribute("data-race", entry.Id);
        _screen.Hidden = false;
        HeroFlow.LabelLeave(_leave!, HeroFlow.LeaveCommand(parts, _data));
        Render();
        Log.Info("Maker", $"the maker opened for the {entry.Id} ({_maker.Look!.Body}{(_maker.Look.Preset is { } p ? $", preset {p}" : "")})");
        parts.Ui.Document.UpdateLayout();
        (_presets!.Descendants().FirstOrDefault(e => e.HasClass("maker-preset")) ?? _begin)!.Focus();
    }

    /// <summary>The presets, the choices, the summary, Begin and the figure as the look is now; the focus kept on the same control.</summary>
    void Render()
    {
        var maker = _maker!;
        var document = _parts!.Ui.Document;
        var focused = document.FocusedElement is { } f && f.IsDescendantOf(_screen!) ? f.GetAttribute("data-key") : null;
        // The maker opens only for a race with a body (Open), so there is always a look.
        var look = maker.Look!;
        RenderPresets(maker, look);
        RenderChoices(maker, look);
        _begin!.TextContent = HeroTexts.Begin(maker.Race, _parts.Session.Current?.Hero.Race == maker.RaceId && _parts.Session.Current?.Look is not null);
        _summary!.TextContent = HeroTexts.Summary(maker.Race, look, maker.Build!, look.Preset is { } preset && maker.Presets.Contains(preset) ? maker.Preset(preset).Label : null);
        Views.ShowFigure(_figure!, maker.RaceId, look);
        // The control that had the focus was made again (the rows and the presets are): the focus goes to its new self,
        // once laid out (a control not laid out can't take it).
        if (focused is null || document.FocusedElement?.GetAttribute("data-key") == focused)
            return;
        document.UpdateLayout();
        _screen!.Descendants().FirstOrDefault(e => e.GetAttribute("data-key") == focused && e.IsFocusable)?.Focus();
    }

    void RenderPresets(LookMaker maker, LookHero look)
    {
        _presets!.TextContent = "";
        foreach (var sex in LookNames.Sexes)
        {
            var names = maker.Presets.Where(p => maker.Preset(p).Look.Sex == sex).ToList();
            if (names.Count == 0)
                continue;
            var group = _presets.Append("div", "maker-preset-group");
            group.SetAttribute("role", "radiogroup");
            group.SetAttribute("aria-label", $"{HeroTexts.Capital(sex)} presets");
            group.Append("p", "eyebrow maker-preset-sex", sex.ToUpperInvariant());
            foreach (var name in names)
            {
                var chosen = look.Preset == name;
                var button = Radio(group, "maker-preset", $"preset:{name}", chosen, maker.Preset(name).Label);
                button.SetAttribute("data-preset", name);
                button.AddEventListener("click", _ => Change(() => maker.UsePreset(name)));
            }
        }
        if (maker.Presets.Count == 0)
            _presets.Append("p", "maker-none", "No presets");
    }

    void RenderChoices(LookMaker maker, LookHero look)
    {
        _choices!.TextContent = "";
        Inline(_choices, "sex", "Sex", maker.Sexes.Select(s => (s, HeroTexts.Capital(s))), look.Sex, value => maker.SetSex(value));
        Inline(_choices, "build", "Build", maker.BuildsOf(look.Sex).Select(b => (b, HeroTexts.Capital(b))), maker.Build!, value => maker.SetBuild(value));
        foreach (var (category, options) in maker.Categories)
        {
            var chosen = look.Parts[category];
            var row = Row(_choices, $"part:{category}", HeroTexts.Category(category), maker.Part(chosen).Name, null);
            if (row is null)
                continue;
            foreach (var option in options)
            {
                var button = Radio(row, "maker-option", $"part:{category}:{option}", option == chosen, maker.Part(option).Name);
                button.SetAttribute("data-value", option);
                button.AddEventListener("click", _ => Change(() => maker.SetPart(category, option)));
            }
        }
        if (maker.OfferedSlots.Count == 0)
            return;
        _choices.Append("p", "eyebrow maker-colours-title", "COLOURS");
        foreach (var slot in maker.OfferedSlots)
        {
            var chosen = maker.SwatchesOf(slot).First(s => s.Name == look.Colours[slot]);
            var row = Row(_choices, $"colour:{slot}", HeroTexts.Slot(slot), chosen.Name, chosen.Colour);
            if (row is null)
                continue;
            row.AddClass("maker-swatches");
            foreach (var swatch in maker.SwatchesOf(slot))
            {
                var button = Radio(row, "maker-option maker-colour", $"colour:{slot}:{swatch.Name}", swatch.Name == chosen.Name, null);
                button.SetAttribute("data-value", swatch.Name);
                button.SetAttribute("title", swatch.Name);
                Swatch(button, swatch.Colour);
                button.AppendText(swatch.Name);
                button.AddEventListener("click", _ => Change(() => maker.SetColour(slot, swatch.Name)));
            }
        }
    }

    /// <summary>A choice shown whole on one line (the sex, the build): its label and its options side by side.</summary>
    void Inline(UiElement into, string key, string label, IEnumerable<(string Value, string Words)> options, string chosen, Func<string, bool> choose)
    {
        var row = into.Append("div", "maker-row maker-row-inline");
        row.SetAttribute("data-choice", key);
        row.Append("span", "maker-row-label", label);
        var group = row.Append("div", "maker-options");
        group.SetAttribute("role", "radiogroup");
        group.SetAttribute("aria-label", label);
        foreach (var (value, words) in options)
        {
            var button = Radio(group, "maker-option", $"{key}:{value}", value == chosen, words);
            button.SetAttribute("data-value", value);
            button.AddEventListener("click", _ => Change(() => choose(value)));
        }
    }

    /// <summary>
    /// A row that opens (a category, a colour): its head (the label and the choice, a colour's swatch before it) toggling
    /// it; when it is the row open, its options' group (given back to be filled), else null.
    /// </summary>
    UiElement? Row(UiElement into, string key, string label, string value, string? colour)
    {
        var open = _open == key;
        var row = into.Append("div", "maker-row" + (open ? " is-open" : ""));
        row.SetAttribute("data-choice", key);
        var head = row.Append("button", "maker-row-head");
        head.SetAttribute("type", "button");
        head.SetAttribute("data-key", $"head:{key}");
        head.SetAttribute("aria-expanded", open ? "true" : "false");
        head.Append("span", "maker-row-label", label);
        var shown = head.Append("span", "maker-row-value");
        if (colour is not null)
            Swatch(shown, colour);
        shown.AppendText(value);
        head.Append("span", "maker-row-chevron").SetAttribute("aria-hidden", "true");
        head.AddEventListener("click", _ => Toggle(key));
        if (!open)
            return null;
        var group = row.Append("div", "maker-options");
        group.SetAttribute("role", "radiogroup");
        group.SetAttribute("aria-label", label);
        return group;
    }

    static UiElement Radio(UiElement into, string className, string key, bool chosen, string? words)
    {
        var button = into.Append("button", className, words);
        button.SetAttribute("type", "button");
        button.SetAttribute("role", "radio");
        button.SetAttribute("aria-checked", chosen ? "true" : "false");
        button.SetAttribute("data-key", key);
        return button;
    }

    static void Swatch(UiElement into, string colour) =>
        into.Append("i", "maker-swatch").SetAttribute("style", $"--swatch: {colour}");

    /// <summary>A row opened (the one open before closed), or closed when it is the one open.</summary>
    void Toggle(string key)
    {
        _open = _open == key ? null : key;
        Render();
    }

    /// <summary>A choice made: <c>maker-choice</c> and the maker shown again when it changed the look (nothing when it is the choice already).</summary>
    void Change(Func<bool> choose)
    {
        if (!choose())
            return;
        _parts!.Ui.Sound(SoundNames.MakerChoice);
        Render();
    }

    /// <summary>The random button: every choice rolled from the game's randomness (Q21), <c>maker-random</c> once a roll.</summary>
    void Roll()
    {
        var maker = _maker!;
        maker.Roll(_parts!.Random);
        _parts.Ui.Sound(SoundNames.MakerRandom);
        Log.Info("Maker", $"a random look: {maker.Look!.Body}, {string.Join(", ", maker.Look.Parts.Select(p => p.Value))}; {string.Join(", ", maker.Look.Colours.Select(c => $"{c.Key} {c.Value}"))}");
        Render();
    }

    /// <summary>
    /// Arrow keys among a group's options (the old appearance step's): Right and Down choose the next, Left and Up the
    /// previous, round from one end to the other, the focus following; the key is taken.
    /// </summary>
    void Arrows(UiKeyboardEvent e)
    {
        if (e.CtrlKey || e.AltKey || e.MetaKey || e.ShiftKey || e.DefaultPrevented)
            return;
        var step = e.Key switch { "ArrowRight" or "ArrowDown" => 1, "ArrowLeft" or "ArrowUp" => -1, _ => 0 };
        if (step == 0 || _parts!.Ui.Document.FocusedElement is not { Parent: { } group } focused)
            return;
        var options = group.ElementChildren.Where(c => c.GetAttribute("role") == "radio").ToList();
        var at = options.IndexOf(focused);
        // The focus isn't one of a group's options (a row's head, a button): nothing.
        if (at < 0)
            return;
        var next = options[(at + step + options.Count) % options.Count];
        var key = next.GetAttribute("data-key");
        next.Click();
        if (_screen!.Descendants().FirstOrDefault(el => el.GetAttribute("data-key") == key) is { } again)
            again.Focus();
        e.PreventDefault();
    }

    /// <summary>
    /// "Begin as &lt;race&gt;": the race and the look saved together (<see cref="HeroFlow.Finish"/>), <c>hero-confirmed</c>,
    /// the maker hidden and the sheet asked for; a refusal (Dev Mode turned off meanwhile) on the status line in the old
    /// words.
    /// </summary>
    void Begin()
    {
        // The maker shows only while a hero is open, and always with a look (Open).
        var parts = _parts!;
        if (HeroFlow.Finish(parts, _data, _maker!.RaceId, _maker.Look!) is { } refused)
        {
            _status!.TextContent = HeroTexts.Refusal(refused);
            _status.ClassName = "spellbook-status maker-status is-error";
            parts.Ui.Sound(SoundNames.Refused);
            return;
        }
        parts.Ui.Sound(SoundNames.HeroConfirmed);
        Hide();
        parts.Ui.Commands.Run(HeroFlow.SheetCommand);
    }

    /// <summary>"← All races": the carousel again, on this race.</summary>
    void Back()
    {
        var race = _maker!.RaceId;
        Hide();
        _parts!.Ui.Commands.Run(HeroFlow.RacesCommand, race);
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
    }
}
