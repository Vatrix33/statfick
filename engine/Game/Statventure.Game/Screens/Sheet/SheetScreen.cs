using System.Globalization;
using Statventure.Core;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;
using Statventure.Game.Data.Saves;
using Statventure.Game.Figures;
using Statventure.Game.Screens.Races;
using Statventure.Game.Settings;
using Statventure.Game.Sounds;
using Statventure.Game.Ui;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Sheet;

/// <summary>
/// The character's page: GM1's frame (the top bar, the status lines, ⚙ Settings; Docs/Game/Foundation.md, "How it works",
/// step 9) and GM2's sheet (Docs/Game/Hero.md, "The sheet with the live figure", step 7): the old page's markup
/// (<see cref="SheetMarkup"/>) with the hero's name as its heading (Q30), the race's emblem on the seal button, the paper
/// doll round the portrait panel (Q29, Q32), the whole Field tools rail and its drawer under 840 px (Q34,
/// <see cref="FieldTools"/>). Every number and control state is the sheet's rules' (<see cref="ActorRules.Evaluate"/>), every
/// control calls its rule's action and the result is saved (<see cref="Session.GameSession.Change"/>, which lights the
/// dot as the old <c>saveCharacter</c> did), with GM2-P17's sounds. Dev Mode's controls follow the setting live (GM1-12,
/// GM1-13). <b>The sheet opens only with a race and a look</b> (Q26, <see cref="HeroFlow.Needs(SaveGame, SheetContext)"/>,
/// the rule's one place): a character opened without them (a GM1 save, the new-hero flow left unfinished) asks for
/// <c>open-races</c>, the new-hero flow's (GM2 step 8), and the page stays hidden; a hero of an enemy-only race chosen in
/// Dev Mode has no look and opens on the sheet (Q27). <c>open-sheet</c> shows it
/// (the flow's end runs it). It shows while a character is open (<c>game-opened</c> on every open), the dot lights for
/// 350 ms on every save, and the status line says what <see cref="GameUi.Status"/> says.
/// </summary>
public sealed class SheetScreen : IGameScreen
{
    /// <summary>The page's own sheet (<c>Assets/UI/styles/sheet.css</c>).</summary>
    public const string Stylesheet = "ui/styles/sheet";

    /// <summary>The command that shows the sheet of the open character (the new-hero flow's end runs it).</summary>
    public const string OpenCommand = "open-sheet";

    /// <summary>The new-hero flow's first screen, the race carousel (GM2 step 8's), asked for a hero without a race or a look, and by the seal button in Dev Mode.</summary>
    public const string RacesCommand = HeroFlow.RacesCommand;

    /// <summary>How long the autosave dot stays lit after a save (the old <c>saveCharacter</c>'s 350 ms).</summary>
    public const double SavingMilliseconds = 350;

    /// <summary>The class the dot has while lit (the old game's).</summary>
    public const string SavingClass = "is-saving";

    /// <summary>The body's class while the sheet shows: the page keeps the rail's room on its left (the old body's <c>padding-left</c>, Q34; GM1's Q32 had it taken away).</summary>
    public const string RailRoomClass = "has-field-tools";

    /// <summary>The page's markup (<see cref="SheetMarkup.Markup"/>).</summary>
    internal static string Markup => SheetMarkup.Markup;

    GameParts? _parts;
    SheetContext _context = SheetContext.Plain;
    UiDocument? _document;
    UiElement? _page, _settingsButton, _toggle, _dot, _status, _figure;
    FieldTools? _tools;
    EmblemPictures? _pictures;
    int _dotTimer;
    bool _showing;

    /// <summary>
    /// The 3D view of the portrait panel (GM2 step 11): the game's live one on its window (<see cref="LiveHeroViews"/>,
    /// the hero's figure standing in <c>surface#hero-view</c>), none on a document without one; a test's own.
    /// </summary>
    internal IHeroViews Views { get; set; } = NoHeroViews.Instance;

    public void Attach(GameParts parts)
    {
        _parts = parts;
        var ui = parts.Ui;
        ui.UseStylesheet(Stylesheet);
        var document = _document = ui.Document;
        if (Views is NoHeroViews)
            Views = LiveHeroViews.For(parts, () => document.Files);
        foreach (var node in Statventure.UI.Dom.Markup.Parse(Markup))
            document.Body.Append(node);
        _settingsButton = Q("#settings-open-button");
        _toggle = Q("#field-tools-toggle");
        _page = Q("main.game-page");
        _figure = Q($"#{SheetMarkup.ViewId}");
        _dot = Q("main.game-page .status-dot");
        _status = Q("#backup-status");
        _tools = new FieldTools(document, ui.Windows);
        _pictures = new EmblemPictures(document);
        _context = Context(document.Files);
        _settingsButton.AddEventListener("click", _ => ui.Commands.Run("open-settings"));
        Q("#menu-button").AddEventListener("click", _ => ui.Commands.Run(GameUi.MenuCommand));
        Wire();
        ui.Commands.Register(OpenCommand, _ => OpenSheet(), () => parts.Session.Current is not null);
        parts.Session.Opened += Opened;
        parts.Session.Closed += Closed;
        parts.Session.Saved += Saved;
        parts.Settings.Changed += SettingChanged;
        ui.StatusShown += StatusShown;
    }

    /// <summary>The sheet's rules' context: the game's races (<see cref="RaceCatalogue.Load"/>; Hero.md, "How it works", step 2).</summary>
    static SheetContext Context(Statventure.Packs.VirtualFileSystem? data) =>
        data is null ? SheetContext.Plain : SheetContext.Plain.WithRaces(RaceCatalogue.Load(data));

    UiElement Q(string selector) => _document!.QuerySelector(selector) ?? throw new InvalidOperationException($"the sheet has no {selector}");

    IEnumerable<UiElement> All(string selector) => _document!.QuerySelectorAll(selector);

    /// <summary>Whether the page shows (a character with a race and a look is open).</summary>
    public bool Showing => _showing;

    /// <summary>The drawer of the Field tools rail.</summary>
    internal FieldTools Tools => _tools!;

    /// <summary>A character opened: the sheet shown when its hero has a race and a look, else the new-hero flow asked for.</summary>
    void Opened()
    {
        var parts = _parts!;
        _status!.TextContent = "";
        _status.ClassName = "backup-status topbar-file-status";
        parts.Ui.Sound(SoundNames.Opened);
        OpenSheet();
    }

    /// <summary><c>open-sheet</c>, and every open: the open character's sheet, or the flow (<c>open-races</c>) for a hero that isn't ready.</summary>
    void OpenSheet()
    {
        if (_parts!.Session.Current is not { } save)
            return;
        if (!HeroFlow.Needs(save, _context))
        {
            Show();
            return;
        }
        Hide();
        Log.Info("Game", $"the hero has no {(save.Hero.Race is { Length: > 0 } ? "look" : "race")}: the new hero's flow asked for");
        _parts.Ui.Commands.Run(RacesCommand);
    }

    void Show()
    {
        if (!_showing)
        {
            _showing = true;
            _page!.Hidden = false;
            _settingsButton!.Hidden = false;
            _toggle!.Hidden = false;
            _document!.Body.AddClass(RailRoomClass);
        }
        Render();
    }

    void Hide()
    {
        _tools!.Close(restoreFocus: false);
        // The portrait's view let go (GM2-P16: a screen keeps only the views it shows).
        Views.Hide(_figure!);
        _showing = false;
        _page!.Hidden = true;
        _settingsButton!.Hidden = true;
        _toggle!.Hidden = true;
        _document!.Body.RemoveClass(RailRoomClass);
    }

    void Closed()
    {
        Hide();
        _dot!.RemoveClass(SavingClass);
    }

    /// <summary>A save of the open character: the dot lit ember for 350 ms, the timer started again (the old <c>saveCharacter</c>); the sheet shown again.</summary>
    void Saved()
    {
        var document = _document!;
        _dot!.AddClass(SavingClass);
        document.ClearTimeout(_dotTimer);
        _dotTimer = document.SetTimeout(() => _dot.RemoveClass(SavingClass), SavingMilliseconds);
        if (_showing)
            Render();
    }

    void SettingChanged(SettingKey key)
    {
        if (key == SettingKey.DevMode && _showing)
            Render();
    }

    /// <summary>
    /// A file message on the page's status line (the old <c>setBackupStatus</c>: "success" or "error") while the page is the
    /// topmost surface: it shows and no window is open (the manager's rule for the status lines, 2026-11-14).
    /// </summary>
    void StatusShown(string text, StatusKind kind)
    {
        if (!Showing || _parts!.Ui.Windows.Top is not null)
            return;
        _status!.TextContent = text;
        _status.ClassName = $"backup-status topbar-file-status {(kind == StatusKind.Problem ? "is-error" : "is-success")}";
    }

    // ---------- The sheet's numbers and controls ----------

    bool DevMode => _parts!.Settings.DevMode;

    /// <summary>
    /// The open hero shown (the old <c>updateDerivedValues</c>, <c>updateBonusInputs</c>, <c>updateEditingControls</c> and
    /// <c>updateRaceHero</c>): every number from <see cref="ActorRules.Evaluate"/>, written as the old page writes it
    /// (<see cref="SheetTexts"/>), every control's state, Dev Mode's controls shown or hidden.
    /// </summary>
    void Render()
    {
        if (_parts!.Session.Current is not { } save)
            return;
        var hero = save.Hero;
        var dev = DevMode;
        var values = ActorRules.Evaluate(hero, _context, dev);
        var race = ActorRules.RaceOf(hero, _context);

        // The heading (Q30) and the race's seal (updateRaceHero; Q27, Q36).
        Text("#page-title", save.Name);
        Text("#hero-standing", SheetTexts.Standing(race, hero));
        var seal = Q("#race-select-button");
        var title = SheetTexts.SealTitle(race, dev);
        seal.SetAttribute("title", title);
        seal.SetAttribute("aria-label", title);
        seal.ToggleAttribute("disabled", !dev);
        seal.SetStyle("--race-accent", race?.Accent ?? "var(--ember)");
        // The race's emblem picture (Q36, GM2 step 10), else the ✦.
        Q(".hero-sigil-fallback").Hidden = _pictures!.Show(Q("#race-icon"), race?.Emblem);
        Text("#hero-race-name", SheetTexts.RaceName(race));

        // The hero's figure standing in the portrait panel (GM2-2, Q31; GM2 step 11): built again only when the look
        // changes (a Dev Mode hero of an enemy-only race has none: the panel's ground alone).
        Views.ShowFigure(_figure!, hero.Race ?? "", save.Look);

        // The EXP card.
        Text("#exp-current", SheetTexts.Value(hero.Experience));
        Text("#exp-threshold", SheetTexts.Threshold(values));
        Text("#exp-helper", SheetTexts.Helper(hero, values, dev));
        Q("#exp-bar").SetStyle("width", SheetTexts.BarWidth(values));
        var track = Q("#exp-track");
        track.SetAttribute("aria-valuemax", SheetTexts.BarMaximum(values));
        track.SetAttribute("aria-valuenow", SheetTexts.BarValue(hero, values));

        // Level, HP and Mana.
        Text("#level-value", SheetTexts.Value(hero.Level));
        Text(".level-card .level-cap", SheetTexts.Cap(values));
        Q("#level-up-button").ToggleAttribute("disabled", !values.LevelUpEnabled);
        Q("#level-icon").ToggleClass("has-level-up", values.LevelUpAlert);
        Text("#hp-current", SheetTexts.Value(values.Hp));
        Text("#hp-max", SheetTexts.Value(values.MaxHp));
        Text("#mana-current", SheetTexts.Value(values.Mana));
        Text("#mana-max", SheetTexts.Value(values.MaxMana));
        Q("#hp-meter").SetStyle("transform", SheetTexts.Meter(values.Hp, values.MaxHp));
        Q("#mana-meter").SetStyle("transform", SheetTexts.Meter(values.Mana, values.MaxMana));

        // Armor and Evasion.
        Text("#armor-base-value", SheetTexts.Value(values.Defenses.Armor.Base));
        Text("#armor-modified-value", SheetTexts.Value(values.Defenses.Armor.Modified));
        Text("#armor-value", SheetTexts.Number(values.Defenses.Armor.Current));
        Text("#evasion-base-value", SheetTexts.Number(values.Defenses.Evasion.Base));
        Text("#evasion-modified-value", SheetTexts.Number(values.Defenses.Evasion.Modified));
        Text("#evasion-value", SheetTexts.Number(values.Defenses.Evasion.Current));
        foreach (var input in All(".defense-bonus-input"))
        {
            Field(input, SheetTexts.Number(values.ShownDefenseBonuses[input.GetAttribute("data-defense")!]));
            input.ToggleAttribute("disabled", !dev);
        }

        // The eight attributes, the free points and the respec.
        foreach (var (key, _, _, _) in SheetMarkup.Attributes)
        {
            var input = Q($"#stat-{key}");
            Field(input, SheetTexts.Number(values.BaseAttributes[key]));
            input.ToggleAttribute("disabled", !dev);
            var bonus = Q($"#stat-permanent-bonus-{key}");
            Field(bonus, SheetTexts.Number(values.ShownBonuses[key]));
            bonus.ToggleAttribute("disabled", !dev);
            var current = values.CurrentAttributes[key];
            Text($"#stat-current-{key}", SheetTexts.Number(current));
            var card = input.Ancestors().First(e => e.HasClass("stat-card"));
            card.Descendants().First(e => e.Matches(".stat-bar span")).SetStyle("width", SheetTexts.StatBar(current));
        }
        foreach (var stepper in All(".stat-stepper"))
            stepper.ToggleAttribute("disabled", stepper.GetAttribute("data-action") == "decrease" ? !dev : !values.RaiseEnabled);
        Text("#points-value", SheetTexts.Number(hero.FreePoints));
        Q("#respec-button").ToggleAttribute("disabled", !values.RespecEnabled);
        Q("#stats-grid").ToggleClass("is-locked", !dev);

        // The purse: only Dev Mode edits it by hand.
        foreach (var input in All(".currency-input"))
        {
            Field(input, SheetTexts.Number(hero.Purse[Coin(input)]));
            input.ToggleAttribute("disabled", !dev);
            input.Ancestors().First(e => e.HasClass("sidebar-currency")).SetAttribute("title", dev ? "" : "Enable Dev Mode to edit money");
        }
        Q(".sidebar-purse").ToggleClass("is-locked", !dev);

        // Dev Mode's own controls.
        foreach (var control in All("[data-dev-only]"))
            control.Hidden = !dev;
    }

    void Text(string selector, string text)
    {
        var element = Q(selector);
        if (element.TextContent != text)
            element.TextContent = text;
    }

    /// <summary>A field's value written (only when it differs, so a field being typed in keeps its caret).</summary>
    static void Field(UiElement input, string value)
    {
        if (input.Value != value)
            input.Value = value;
    }

    static Coin Coin(UiElement input) => input.GetAttribute("data-currency") switch
    {
        "silver" => Data.Actors.Coin.Silver,
        "gold" => Data.Actors.Coin.Gold,
        _ => Data.Actors.Coin.Platinum,
    };

    /// <summary>A field's text as the old page reads it (<c>toNumber</c>: JavaScript's <c>Number</c>, not a finite number: the fallback).</summary>
    internal static double NumberOf(string text, double fallback = 0)
    {
        var trimmed = JsValues.Trim(text);
        if (trimmed.Length == 0)
            return 0;
        return double.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number) ? number : fallback;
    }

    /// <summary>
    /// A sheet action: <paramref name="act"/> on the open hero, and, unless the rule refused it (the same sheet back), the
    /// result saved and <paramref name="sound"/> played (GM2-P17). Gives whether it was done.
    /// </summary>
    bool Act(Func<ActorSheet, ActorSheet> act, string? sound = null)
    {
        var session = _parts!.Session;
        if (session.Current is not { } save)
            return false;
        var next = act(save.Hero);
        if (ReferenceEquals(next, save.Hero))
            return false;
        session.Change(current => current with { Hero = next });
        if (sound is not null)
            _parts.Ui.Sound(sound);
        return true;
    }

    /// <summary>Every control to its rule (the old page's handlers, <c>app.js</c> 1674–1716 and 2008–2139).</summary>
    void Wire()
    {
        // The race's seal: in Dev Mode the race carousel (Q27); outside it the button is disabled ("Race locked").
        Q("#race-select-button").AddEventListener("click", _ =>
        {
            if (DevMode)
                _parts!.Ui.Commands.Run(RacesCommand);
        });

        // Dev Mode's EXP form (2037–2046).
        Q("#exp-form").AddEventListener("submit", e =>
        {
            e.PreventDefault();
            var input = Q("#exp-input");
            if (Act(hero => ActorRules.AddExperience(hero, _context, NumberOf(input.Value), DevMode)))
                input.Value = "";
        });

        // HP and Mana by hand, for everyone (2008–2019, 2048–2056): the amount's whole value, nothing when it isn't above 0.
        foreach (var button in All(".resource-adjust-button"))
            button.AddEventListener("click", _ =>
            {
                var resource = button.GetAttribute("data-resource") == "hp" ? ActorResource.Hp : ActorResource.Mana;
                var input = Q($"#{button.GetAttribute("data-resource")}-adjust");
                var amount = Math.Abs(JsValues.ToInteger(NumberOf(input.Value)));
                if (amount <= 0)
                    return;
                var change = int.Parse(button.GetAttribute("data-change")!, CultureInfo.InvariantCulture);
                Act(hero => ActorRules.ChangeResource(hero, _context, resource, JsNumbers.ToInt(amount * change)));
                input.Value = "";
            });
        Q("#restore-button").AddEventListener("click", _ => Act(hero => ActorRules.RestoreBoth(hero, _context)));
        foreach (var button in All(".resource-restore-button"))
            button.AddEventListener("click", _ => Act(hero => ActorRules.RestoreResource(hero, _context,
                button.GetAttribute("data-restore-resource") == "hp" ? ActorResource.Hp : ActorResource.Mana)));

        // Dev Mode's purse (2066–2073).
        foreach (var input in All(".currency-input"))
            input.AddEventListener("input", _ =>
            {
                Act(hero => ActorRules.SetPurse(hero, Coin(input), JsValues.ToInteger(NumberOf(input.Value)), DevMode));
                if (_parts!.Session.Current is { } save)
                    Field(input, SheetTexts.Number(save.Hero.Purse[Coin(input)]));
            });

        // Dev Mode's Armor and Evasion bonuses (2081–2090).
        foreach (var input in All(".defense-bonus-input"))
            input.AddEventListener("input", _ => Act(hero => ActorRules.SetDefenseBonus(hero, _context, input.GetAttribute("data-defense")!, NumberOf(input.Value), DevMode)));

        // The attributes: Dev Mode's fields and bonuses (1674–1693), the steppers (1696–1713).
        foreach (var input in All(".stat-input"))
            input.AddEventListener("input", _ => Act(hero => ActorRules.SetBaseAttribute(hero, _context, input.GetAttribute("data-stat")!, NumberOf(input.Value), DevMode)));
        foreach (var input in All(".stat-bonus-input"))
            input.AddEventListener("input", _ => Act(hero => ActorRules.SetBonus(hero, _context, input.GetAttribute("data-stat")!, NumberOf(input.Value), DevMode)));
        foreach (var stepper in All(".stat-stepper"))
            stepper.AddEventListener("click", _ =>
            {
                var key = stepper.GetAttribute("data-stat")!;
                if (stepper.GetAttribute("data-action") == "decrease")
                {
                    Act(hero => ActorRules.Lower(hero, _context, key, DevMode));
                    return;
                }
                // A free point spent sounds its ink drop (GM2-P17); Dev Mode's raise with none left keeps GM1's press.
                var spends = _parts!.Session.Current?.Hero.FreePoints > 0;
                Act(hero => ActorRules.Raise(hero, _context, key, DevMode), spends ? SoundNames.PointSpent : null);
            });

        Q("#respec-button").AddEventListener("click", _ => Act(hero => ActorRules.Respec(hero, _context), SoundNames.Respec));
        Q("#level-up-button").AddEventListener("click", _ => Act(hero => ActorRules.LevelUp(hero, _context, DevMode), SoundNames.LevelUp));
    }
}
