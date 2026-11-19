using Statventure.Core;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Saves;
using Statventure.Game.Screens.Sheet;
using Statventure.Game.Settings;
using Statventure.Game.Sounds;
using Statventure.Platform;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The character sheet (GM2 step 7; Docs/Game/Hero.md, "The sheet with the live figure"): it opens only for a hero with a
/// race and a look (Q26; Dev Mode's enemy-only hero without one, Q27), else asks for the new hero's flow; every number is
/// the sheet's rules' (<see cref="ActorRules.Evaluate"/>) written as the old page writes it; every control calls its rule
/// and the result is saved, with GM2-P17's sounds; Dev Mode's controls follow the setting; the seal, the paper doll, the
/// rail and its drawer under 840 px (the old <c>interface.js</c>).
/// </summary>
public class SheetScreenTests
{
    static SheetContext Context(ScreensHarness s) => SheetHeroes.Context(s.H);

    static ActorSheet Hero(ScreensHarness s) => s.Session.Current!.Hero;

    /// <summary>The open hero as its slot holds it on disk.</summary>
    static ActorSheet OnDisk(ScreensHarness s)
    {
        s.H.Storage.Flush();
        return s.H.Storage.Saves.Load<SaveGame>(s.Session.CurrentSlot!).Value.Hero;
    }

    /// <summary>A click on <paramref name="element"/>, scrolled into the window first (the sheet is longer than the window).</summary>
    static void Press(ScreensHarness s, UiElement element)
    {
        element.ScrollIntoView(ScrollAlign.Center, ScrollAlign.Nearest, ScrollMode.Instant);
        s.H.Frame();
        s.H.Click(element);
    }

    /// <summary>Text typed into a field from empty, a character at a time (each an <c>input</c> event, as typing is).</summary>
    static void TypeInto(ScreensHarness s, UiElement field, string text)
    {
        field.ScrollIntoView(ScrollAlign.Center, ScrollAlign.Nearest, ScrollMode.Instant);
        field.Focus();
        field.Value = "";
        s.H.Type(text);
    }

    // ---------- Q26: the sheet only with a race and a look ----------

    [Fact]
    public void AHeroWithoutARaceOrALookGetsTheNewHerosFlowAndNotTheSheet()
    {
        using var s = new ScreensHarness(1440, 960);
        var log = GameTesting.CaptureLog(() => s.Session.New("Ser Alrik"));
        s.H.Frame();
        Assert.False(s.Sheet.Showing);
        Assert.True(s.Q("main.game-page").Hidden);
        Assert.True(s.Q("#field-tools-toggle").Hidden);
        Assert.False(s.Document.Body.HasClass(SheetScreen.RailRoomClass));
        Assert.Equal([SheetScreen.RacesCommand], s.Ran);
        Assert.Contains("Info [Game] the hero has no race: the new hero's flow asked for", log);
        // A race and no look (the flow left in the maker): the flow again.
        s.Ran.Clear();
        s.Session.Change(save => save with { Hero = SheetHeroes.New(Context(s), "elf") });
        log = GameTesting.CaptureLog(() => s.H.Ui.Commands.Run(SheetScreen.OpenCommand));
        Assert.False(s.Sheet.Showing);
        Assert.Equal([SheetScreen.RacesCommand], s.Ran);
        Assert.Contains("Info [Game] the hero has no look: the new hero's flow asked for", log);
        // The look given (the flow's end): open-sheet shows the sheet, nothing more asked.
        s.Ran.Clear();
        s.Session.Change(save => save with { Look = SheetHeroes.Look("elf") });
        s.H.Ui.Commands.Run(SheetScreen.OpenCommand);
        Assert.True(s.Sheet.Showing);
        Assert.False(s.Q("main.game-page").Hidden);
        Assert.False(s.Q("#settings-open-button").Hidden);
        Assert.False(s.Q("#field-tools-toggle").Hidden);
        Assert.Empty(s.Ran);
        // Opened again later (Continue, the slot list), it opens on its sheet at once.
        s.Session.Leave();
        Assert.False(s.Sheet.Showing);
        s.Session.Open("ser-alrik");
        Assert.True(s.Sheet.Showing);
        Assert.Empty(s.Ran);
        // A hero no longer ready while its sheet shows (its look taken away): open-sheet hides the page and asks for the flow.
        s.Session.Change(save => save with { Look = null });
        s.H.Ui.Commands.Run(SheetScreen.OpenCommand);
        Assert.False(s.Sheet.Showing);
        Assert.True(s.Q("main.game-page").Hidden);
        Assert.False(s.Document.Body.HasClass(SheetScreen.RailRoomClass));
        Assert.Equal([SheetScreen.RacesCommand], s.Ran);
    }

    [Fact]
    public void DevModesEnemyOnlyHeroHasNoLookAndGetsTheSheetAndSoDoesARaceTheDataLacks()
    {
        using var s = new ScreensHarness(1440, 960);
        s.Session.New("Grak");
        s.Ran.Clear();
        // Q27: an enemy-only race chosen at once in Dev Mode, no look.
        s.Session.Change(save => save with { Hero = SheetHeroes.New(Context(s), "goblin", devMode: true) });
        s.H.Ui.Commands.Run(SheetScreen.OpenCommand);
        Assert.True(s.Sheet.Showing);
        Assert.Equal("Goblin", s.Q("#hero-race-name").TextContent);
        // A race the data doesn't have (a mod's, switched off): it can't be told from an enemy-only one, so the sheet.
        s.Session.Change(save => save with { Hero = save.Hero with { Race = "moon-folk" } });
        s.Session.Leave();
        s.Session.Open("grak");
        Assert.True(s.Sheet.Showing);
        Assert.Equal(("Unknown race", "Unknown race · Level 1", "Race locked"),
            (s.Q("#hero-race-name").TextContent, s.Q("#hero-standing").TextContent, s.Q("#race-select-button").GetAttribute("title")));
        Assert.Empty(s.Ran);
        // Q26's rule, the sheet's and the flow's (one place: HeroFlow.Needs), on the game's races.
        var races = Context(s);
        bool ready(SaveGame save) => !Screens.Races.HeroFlow.Needs(save, races);
        Assert.False(ready(new SaveGame()));
        Assert.False(ready(new SaveGame { Hero = new ActorSheet { Race = "" } }));
        Assert.False(ready(new SaveGame { Hero = new ActorSheet { Race = "human" } }));
        Assert.True(ready(new SaveGame { Hero = new ActorSheet { Race = "human" }, Look = SheetHeroes.Look("human") }));
        Assert.True(ready(new SaveGame { Hero = new ActorSheet { Race = "goblin" } }));
        Assert.True(ready(new SaveGame { Hero = new ActorSheet { Race = "moon-folk" } }));
        Assert.True(races.Races["human"].Playable && !races.Races["goblin"].Playable);
    }

    [Fact]
    public void OpenSheetIsPossibleOnlyWithACharacterOpen()
    {
        using var s = new ScreensHarness(1440, 960);
        Assert.True(s.H.Ui.Commands.IsRegistered(SheetScreen.OpenCommand));
        Assert.False(s.H.Ui.Commands.IsEnabled(SheetScreen.OpenCommand));
        Assert.False(s.H.Ui.Commands.Run(SheetScreen.OpenCommand));
        Assert.False(s.Sheet.Showing);
        SheetHeroes.OpenPlayed(s);
        Assert.True(s.H.Ui.Commands.IsEnabled(SheetScreen.OpenCommand));
    }

    // ---------- What the sheet shows ----------

    [Fact]
    public void TheSheetShowsTheRulesValuesAsTheOldPageWritesThem()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        var hero = Hero(s);
        var v = ActorRules.Evaluate(hero, Context(s), devMode: false);
        string T(string selector) => s.Q(selector).TextContent;
        // The heading (Q30) and the seal (Q27, Q36): locked outside Dev Mode, the race's emblem picture.
        Assert.Equal(("Ser Alrik", "Human · Level 30", "Human"), (T("#page-title"), T("#hero-standing"), T("#hero-race-name")));
        // Q47: the race and level in the lettering of the race's name under the seal (`--ember-dark`), not the heading's
        // ink; small text, which the pictures' 0.2 % can't hold.
        var standing = s.Q("#hero-standing").Style!;
        Assert.Equal(s.Q("#hero-race-name").Style!.Color, standing.Color);
        Assert.NotEqual(s.Q("#page-title").Style!.Color, standing.Color);
        var seal = s.Q("#race-select-button");
        Assert.Equal(("Human · Race locked", "Human · Race locked", true), (seal.GetAttribute("title"), seal.GetAttribute("aria-label"), seal.IsDisabled));
        Assert.Equal("#c7683f", seal.GetStyle("--race-accent"));
        // Q48: locked, but in full colour (not the old page's half opacity of a disabled button). The seal's picture is
        // masked in the comparisons (Q50), so its look is checked here.
        Assert.Equal(1f, seal.Style!.Opacity);
        Assert.Equal((EmblemPictures.Placeholder, false, true), (s.Q("#race-icon").GetAttribute("src"), s.Q("#race-icon").Hidden, s.Q(".hero-sigil-fallback").Hidden));
        // The EXP card.
        Assert.Equal(("1650", "/ 2790 EXP", "1140 EXP to reach Level 31"), (T("#exp-current"), T("#exp-threshold"), T("#exp-helper")));
        Assert.Equal(SheetTexts.Number(1650.0 / 2790 * 100) + "%", s.Q("#exp-bar").GetStyle("width"));
        Assert.Equal(("2790", "1650"), (s.Q("#exp-track").GetAttribute("aria-valuemax"), s.Q("#exp-track").GetAttribute("aria-valuenow")));
        // Level, HP and Mana.
        Assert.Equal(("30", "CAP 100"), (T("#level-value"), T(".level-card .level-cap")));
        Assert.True(s.Q("#level-up-button").IsDisabled);
        Assert.False(s.Q("#level-icon").HasClass("has-level-up"));
        Assert.Equal((SheetTexts.Value(v.Hp), SheetTexts.Value(v.MaxHp), SheetTexts.Value(v.Mana), SheetTexts.Value(v.MaxMana)), (T("#hp-current"), T("#hp-max"), T("#mana-current"), T("#mana-max")));
        Assert.Equal(("166", "227", "66", "80"), (T("#hp-current"), T("#hp-max"), T("#mana-current"), T("#mana-max")));
        Assert.Equal(SheetTexts.Meter(166, 227), s.Q("#hp-meter").GetStyle("transform"), ignoreCase: true);
        Assert.Equal(SheetTexts.Meter(66, 80), s.Q("#mana-meter").GetStyle("transform"), ignoreCase: true);
        // Armor and Evasion.
        Assert.Equal((SheetTexts.Value(v.Defenses.Armor.Base), SheetTexts.Value(v.Defenses.Armor.Modified), SheetTexts.Number(v.Defenses.Armor.Current)),
            (T("#armor-base-value"), T("#armor-modified-value"), T("#armor-value")));
        Assert.Equal((SheetTexts.Number(v.Defenses.Evasion.Base), SheetTexts.Number(v.Defenses.Evasion.Modified), SheetTexts.Number(v.Defenses.Evasion.Current)),
            (T("#evasion-base-value"), T("#evasion-modified-value"), T("#evasion-value")));
        Assert.Equal(("5", "79"), (T("#armor-value"), T("#evasion-value")));
        // The eight attributes: base with the race's modifier, current, the bar.
        foreach (var (key, _, _, _) in SheetMarkup.Attributes)
        {
            Assert.Equal(SheetTexts.Number(v.BaseAttributes[key]), s.Q($"#stat-{key}").Value);
            Assert.Equal(SheetTexts.Number(v.CurrentAttributes[key]), T($"#stat-current-{key}"));
            Assert.Equal(SheetTexts.Number(v.ShownBonuses[key]), s.Q($"#stat-permanent-bonus-{key}").Value);
            Assert.Equal(SheetTexts.StatBar(v.CurrentAttributes[key]), s.Q($"#stat-{key}").Ancestors().First(e => e.HasClass("stat-card")).Descendants().First(e => e.Matches(".stat-bar span")).GetStyle("width"));
        }
        Assert.Equal(("81", "81", "36"), (s.Q("#stat-strength").Value, T("#stat-current-strength"), s.Q("#stat-mind").Value));
        Assert.Equal(["Strength", "Vitality", "Mind", "Dexterity", "Intelligence", "Wisdom", "Speed", "Luck"], s.Document.QuerySelectorAll(".stat-name").Select(e => e.TextContent));
        Assert.Equal(("3", false), (T("#points-value"), s.Q("#respec-button").IsDisabled));
        // Outside Dev Mode: the "+" steppers work while a point is free, every other control is locked or hidden.
        Assert.All(s.Document.QuerySelectorAll(".stat-stepper[data-action=\"increase\"]"), b => Assert.False(b.IsDisabled));
        Assert.All(s.Document.QuerySelectorAll(".stat-stepper[data-action=\"decrease\"], .stat-input, .stat-bonus-input, .defense-bonus-input, .currency-input"), c => Assert.True(c.IsDisabled));
        Assert.All(s.Document.QuerySelectorAll("[data-dev-only]"), c => Assert.True(c.Hidden));
        Assert.True(s.Q("#stats-grid").HasClass("is-locked"));
        // The purse.
        Assert.Equal(("340", "27", "1"), (s.Q("#currency-silver").Value, s.Q("#currency-gold").Value, s.Q("#currency-platinum").Value));
        Assert.True(s.Q(".sidebar-purse").HasClass("is-locked"));
        Assert.All(s.Document.QuerySelectorAll(".sidebar-currency"), c => Assert.Equal("Enable Dev Mode to edit money", c.GetAttribute("title")));
        // The Level icon flashes once the EXP is there.
        s.Session.Change(save => save with { Hero = save.Hero with { Experience = 2790 } });
        Assert.True(s.Q("#level-icon").HasClass("has-level-up"));
        Assert.False(s.Q("#level-up-button").IsDisabled);
        // HP and Mana above their maxima are shown held (updateDerivedValues).
        s.Session.Change(save => save with { Hero = save.Hero with { Hp = 999, Mana = 999 } });
        Assert.Equal(("227", "80"), (T("#hp-current"), T("#mana-current")));
    }

    [Fact]
    public void TheRacesBonusesShowInTheBonusFields()
    {
        using var s = new ScreensHarness(1440, 960);
        // Mountainborn: the dwarf's Armor +8 % in Armor's field.
        SheetHeroes.Open(s, "Durin", SheetHeroes.New(Context(s), "dwarf"), SheetHeroes.Look("dwarf"));
        Assert.Equal(("8", "0"), (s.Q("#armor-permanent-bonus").Value, s.Q("#evasion-permanent-bonus").Value));
        // An enemy-only race's attribute percentage (Dev Mode's orc) in its attribute's field.
        s.Parts.Settings.DevMode = true;
        var orc = SheetHeroes.New(Context(s), "orc", devMode: true);
        SheetHeroes.Open(s, "Grak", orc, null);
        var shown = ActorRules.ShownBonus(orc, Context(s), AttributeKeys.Strength);
        Assert.NotEqual(0, shown);
        Assert.Equal(SheetTexts.Number(shown), s.Q("#stat-permanent-bonus-strength").Value);
        Assert.Equal(SheetTexts.Number(ActorRules.ShownDefenseBonus(orc, Context(s), DefenseKeys.Armor)), s.Q("#armor-permanent-bonus").Value);
    }

    [Fact]
    public void DevModesControlsFollowTheSettingLive()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        s.Session.Change(save => save with { Hero = save.Hero with { FreePoints = 0 } });
        Assert.All(s.Document.QuerySelectorAll(".stat-stepper"), b => Assert.True(b.IsDisabled));
        s.Parts.Settings.DevMode = true;
        Assert.All(s.Document.QuerySelectorAll("[data-dev-only]"), c => Assert.False(c.Hidden));
        Assert.All(s.Document.QuerySelectorAll(".stat-stepper, .stat-input, .stat-bonus-input, .defense-bonus-input, .currency-input"), c => Assert.False(c.IsDisabled));
        Assert.False(s.Q("#stats-grid").HasClass("is-locked"));
        Assert.False(s.Q(".sidebar-purse").HasClass("is-locked"));
        Assert.All(s.Document.QuerySelectorAll(".sidebar-currency"), c => Assert.Equal("", c.GetAttribute("title")));
        Assert.Equal("Dev-mode level up enabled · 2790 EXP target", s.Q("#exp-helper").TextContent);
        Assert.False(s.Q("#level-up-button").IsDisabled);
        var seal = s.Q("#race-select-button");
        Assert.Equal(("Human · Change race", false), (seal.GetAttribute("title"), seal.IsDisabled));
        // The Combat tool shows in Dev Mode, still disabled until GM5.
        Assert.False(s.Q("#combat-open-button").Hidden);
        Assert.True(s.Q("#combat-open-button").IsDisabled);
        s.Parts.Settings.DevMode = false;
        Assert.All(s.Document.QuerySelectorAll("[data-dev-only]"), c => Assert.True(c.Hidden));
        Assert.True(seal.IsDisabled);
        Assert.Equal("1140 EXP to reach Level 31", s.Q("#exp-helper").TextContent);
    }

    [Fact]
    public void TheSheetIsShownAgainOnEverySaveOfTheOpenCharacter()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        s.Session.Change(save => save with { Name = "Ser Alrik the Bold", Hero = save.Hero with { Level = 31, Purse = save.Hero.Purse with { Gold = 99 } } });
        Assert.Equal(("Ser Alrik the Bold", "Human · Level 31", "31", "99"),
            (s.Q("#page-title").TextContent, s.Q("#hero-standing").TextContent, s.Q("#level-value").TextContent, s.Q("#currency-gold").Value));
    }

    [Fact]
    public void ARaceWithNoEmblemShowsTheStarAndTheEmblemIsThePlaceholderUntilStepTen()
    {
        using var s = new ScreensHarness(1440, 960);
        s.Parts.Settings.DevMode = true;
        SheetHeroes.Open(s, "Grak", SheetHeroes.New(Context(s), "goblin", devMode: true), null);
        Assert.True(s.Q("#race-icon").Hidden);
        Assert.Null(s.Q("#race-icon").GetAttribute("src"));
        Assert.False(s.Q(".hero-sigil-fallback").Hidden);
        Assert.Equal("Goblin · Change race", s.Q("#race-select-button").GetAttribute("title"));
        Assert.Null(EmblemPictures.For(null));
        Assert.Null(EmblemPictures.For(Context(s).Races["goblin"]));
        Assert.Equal(EmblemPictures.Placeholder, EmblemPictures.For(Context(s).Races["dark-elf"]));
        Assert.StartsWith("data:image/svg+xml;base64,", EmblemPictures.Placeholder);
        var svg = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(EmblemPictures.Placeholder["data:image/svg+xml;base64,".Length..]));
        Assert.Equal(24, svg.Split("<path d=\"M64 9.5v4\"").Length - 1);
        Assert.DoesNotContain("TICKS", svg);
    }

    // ---------- Every control through its rule, saved ----------

    [Fact]
    public void ThePlusSpendsAFreePointWithItsInkDropAndIsSavedAndDevModesRaiseWithNoneLeftPresses()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        var before = Hero(s);
        s.H.Sounds.Clear();
        Press(s, s.Q(".stat-stepper[data-action=\"increase\"][data-stat=\"mind\"]"));
        var expected = ActorRules.Raise(before, Context(s), AttributeKeys.Mind, devMode: false);
        Assert.Equal(expected, Hero(s));
        Assert.Equal((2, 1, 40), (Hero(s).FreePoints, Hero(s).SpentPoints.Mind, Hero(s).Attributes.Mind));
        Assert.Equal(expected, OnDisk(s));
        Assert.Equal([SoundNames.PointSpent], s.H.Sounds);
        Assert.Equal(("37", "2"), (s.Q("#stat-mind").Value, s.Q("#points-value").TextContent));
        // One sound a point however fast: twice more, then none is left and the "+" is disabled.
        s.H.Sounds.Clear();
        Press(s, s.Q(".stat-stepper[data-action=\"increase\"][data-stat=\"mind\"]"));
        Press(s, s.Q(".stat-stepper[data-action=\"increase\"][data-stat=\"luck\"]"));
        Assert.Equal([SoundNames.PointSpent, SoundNames.PointSpent], s.H.Sounds);
        Assert.True(s.Q(".stat-stepper[data-action=\"increase\"]").IsDisabled);
        // Dev Mode's "+" with no point left raises it all the same, a press (GM1's), no point spent.
        s.Parts.Settings.DevMode = true;
        s.H.Sounds.Clear();
        before = Hero(s);
        Press(s, s.Q(".stat-stepper[data-action=\"increase\"][data-stat=\"luck\"]"));
        Assert.Equal(ActorRules.Raise(before, Context(s), AttributeKeys.Luck, devMode: true), Hero(s));
        Assert.Equal([SoundNames.Press], s.H.Sounds);
        // And Dev Mode's "−".
        s.H.Sounds.Clear();
        before = Hero(s);
        Press(s, s.Q(".stat-stepper[data-action=\"decrease\"][data-stat=\"luck\"]"));
        Assert.Equal(ActorRules.Lower(before, Context(s), AttributeKeys.Luck, devMode: true), Hero(s));
        Assert.Equal([SoundNames.Press], s.H.Sounds);
        Assert.Equal(Hero(s), OnDisk(s));
    }

    [Fact]
    public void RespecAndLevelUpCallTheirRulesWithTheirSounds()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        var before = Hero(s);
        s.H.Sounds.Clear();
        Press(s, s.Q("#respec-button"));
        Assert.Equal(ActorRules.Respec(before, Context(s)), Hero(s));
        Assert.Equal(145, Hero(s).FreePoints);
        Assert.Equal([SoundNames.Respec], s.H.Sounds);
        Assert.True(s.Q("#respec-button").IsDisabled);
        Assert.Equal(Hero(s), OnDisk(s));
        // Level up once the EXP is there.
        s.Session.Change(save => save with { Hero = save.Hero with { Experience = 3000 } });
        before = Hero(s);
        s.H.Sounds.Clear();
        Press(s, s.Q("#level-up-button"));
        Assert.Equal(ActorRules.LevelUp(before, Context(s), devMode: false), Hero(s));
        Assert.Equal((31, 210.0), (Hero(s).Level, Hero(s).Experience));
        Assert.Equal([SoundNames.LevelUp], s.H.Sounds);
        Assert.True(s.Q("#level-up-button").IsDisabled);
        // Dev Mode's level up without the EXP sounds the same.
        s.Parts.Settings.DevMode = true;
        before = Hero(s);
        s.H.Sounds.Clear();
        Press(s, s.Q("#level-up-button"));
        Assert.Equal(ActorRules.LevelUp(before, Context(s), devMode: true), Hero(s));
        Assert.Equal(32, Hero(s).Level);
        Assert.Equal([SoundNames.LevelUp], s.H.Sounds);
        Assert.Equal(Hero(s), OnDisk(s));
    }

    [Fact]
    public void HpAndManaByHandTakeTheAmountsWholeValueForEveryone()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        var field = s.Q("#hp-adjust");
        var before = Hero(s);
        TypeInto(s, field, "10.7");
        Press(s, s.Q(".resource-adjust-button[data-resource=\"hp\"][data-change=\"1\"]"));
        Assert.Equal(ActorRules.ChangeResource(before, Context(s), ActorResource.Hp, 10), Hero(s));
        Assert.Equal(176, Hero(s).Hp);
        Assert.Equal("", field.Value);
        TypeInto(s, s.Q("#mana-adjust"), "5");
        Press(s, s.Q(".resource-adjust-button[data-resource=\"mana\"][data-change=\"-1\"]"));
        Assert.Equal(61, Hero(s).Mana);
        Assert.Equal(Hero(s), OnDisk(s));
        // A negative amount counts by its size (the old Math.abs of toInteger, a floor): "-5.5" takes 6 off.
        before = Hero(s);
        TypeInto(s, field, "-5.5");
        Press(s, s.Q(".resource-adjust-button[data-resource=\"hp\"][data-change=\"-1\"]"));
        Assert.Equal(ActorRules.ChangeResource(before, Context(s), ActorResource.Hp, -6), Hero(s));
        Assert.Equal(170, Hero(s).Hp);
        // No amount, or nothing above 0: nothing done, nothing saved.
        before = Hero(s);
        var saves = 0;
        s.Session.Saved += () => saves++;
        Press(s, s.Q(".resource-adjust-button[data-resource=\"hp\"][data-change=\"1\"]"));
        TypeInto(s, field, "0");
        Press(s, s.Q(".resource-adjust-button[data-resource=\"hp\"][data-change=\"-1\"]"));
        Assert.Same(before, Hero(s));
        Assert.Equal((0, "0"), (saves, field.Value));
    }

    [Fact]
    public void DevModesFullButtonsTheExpFormAndTheFieldsCallTheirRules()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        s.Parts.Settings.DevMode = true;
        var context = Context(s);
        var before = Hero(s);
        Press(s, s.Q(".resource-restore-button[data-restore-resource=\"hp\"]"));
        Assert.Equal(ActorRules.RestoreResource(before, context, ActorResource.Hp), Hero(s));
        before = Hero(s);
        Press(s, s.Q(".resource-restore-button[data-restore-resource=\"mana\"]"));
        Assert.Equal(ActorRules.RestoreResource(before, context, ActorResource.Mana), Hero(s));
        s.Session.Change(save => save with { Hero = save.Hero with { Hp = 1, Mana = 1 } });
        before = Hero(s);
        Press(s, s.Q("#restore-button"));
        Assert.Equal(ActorRules.RestoreBoth(before, context), Hero(s));
        Assert.Equal((227, 80), (Hero(s).Hp, Hero(s).Mana));
        // The EXP form: a typed amount above 0 added and the field emptied; 0 does nothing (the field kept).
        before = Hero(s);
        TypeInto(s, s.Q("#exp-input"), "250.25");
        Press(s, s.Q("#add-exp-button"));
        Assert.Equal(ActorRules.AddExperience(before, context, 250.25, devMode: true), Hero(s));
        Assert.Equal((1900.25, "", "1900.3"), (Hero(s).Experience, s.Q("#exp-input").Value, s.Q("#exp-current").TextContent));
        before = Hero(s);
        TypeInto(s, s.Q("#exp-input"), "0");
        Press(s, s.Q("#add-exp-button"));
        Assert.Same(before, Hero(s));
        Assert.Equal("0", s.Q("#exp-input").Value);
        // The fields, as typed.
        before = Hero(s);
        TypeInto(s, s.Q("#stat-strength"), "90");
        Assert.Equal(ActorRules.SetBaseAttribute(ActorRules.SetBaseAttribute(before, context, AttributeKeys.Strength, 9, true), context, AttributeKeys.Strength, 90, true), Hero(s));
        Assert.Equal((88, "90"), (Hero(s).Attributes.Strength, s.Q("#stat-strength").Value));
        TypeInto(s, s.Q("#stat-permanent-bonus-speed"), "25");
        Assert.Equal(25, Hero(s).Bonuses.Speed);
        TypeInto(s, s.Q("#armor-permanent-bonus"), "20");
        Assert.Equal(20, Hero(s).Bonuses.Armor);
        TypeInto(s, s.Q("#evasion-permanent-bonus"), "10");
        Assert.Equal((20, 10), (Hero(s).Bonuses.Armor, Hero(s).Bonuses.Evasion));
        Assert.Equal("6", s.Q("#armor-value").TextContent);
        TypeInto(s, s.Q("#currency-platinum"), "42");
        Assert.Equal(42, Hero(s).Purse.Platinum);
        Assert.Equal(Hero(s), OnDisk(s));
    }

    [Fact]
    public void FieldsTypedIntoOutsideDevModeChangeNothing()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        var before = Hero(s);
        // The fields are disabled; their input events (as a script would send them) are refused by the rules.
        foreach (var selector in new[] { "#stat-strength", "#stat-permanent-bonus-speed", "#armor-permanent-bonus", "#currency-gold" })
        {
            var field = s.Q(selector);
            field.Value = "77";
            field.DispatchEvent(new UiEvent("input", bubbles: true, cancelable: false));
        }
        s.Q("#exp-input").Value = "500";
        s.Q("#exp-form").DispatchEvent(new UiEvent("submit", bubbles: true, cancelable: true));
        Assert.Same(before, Hero(s));
        // The purse's field is shown again as the hero has it.
        Assert.Equal("27", s.Q("#currency-gold").Value);
    }

    [Fact]
    public void TheSealOpensTheRaceCarouselOnlyInDevMode()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        Press(s, s.Q("#race-select-button"));
        Assert.Empty(s.Ran);
        s.Q("#race-select-button").DispatchEvent(new UiEvent("click"));
        Assert.Empty(s.Ran);
        s.Parts.Settings.DevMode = true;
        Press(s, s.Q("#race-select-button"));
        Assert.Equal([SheetScreen.RacesCommand], s.Ran);
    }

    [Fact]
    public void TheSheetsLowerPartsAreFixturesScrolledInTheWindow()
    {
        // The fixture list carries the page's own scroll, so Chromium shows the same part (the paper doll, the attributes).
        var doll = GameScreenFixtures.All.Single(f => f.Name == "sheet-doll-dark");
        using var list = System.Text.Json.JsonDocument.Parse(GameScreenFixtures.ListJson([doll]));
        var written = list.RootElement[0];
        Assert.Equal((0.0, 1000.0), (written.GetProperty("scrollX").GetDouble(), written.GetProperty("scrollY").GetDouble()));
        using var h = GameScreenFixtures.Build(doll, 1);
        Assert.Equal(1000f, h.Document.Root.ScrollTop);
        Assert.True(h.Q(".paper-doll").GetBoundingClientRect().Top is > 0 and < 960);
    }

    [Fact]
    public void TheSealsMaskCoversTheSealAndNothingElseOfThePage()
    {
        // The user's Q50: the seal is left out of the sheet's picture comparisons, in every fixture showing the sheet.
        Assert.All(GameScreenFixtures.All.Where(f => f.Name.StartsWith("sheet-", StringComparison.Ordinal) || f.Name.StartsWith("page-", StringComparison.Ordinal)
            || f.Name.StartsWith("menu-page-", StringComparison.Ordinal)), f => Assert.Equal([SheetScreenFixtures.SealMask], f.Masks));
        Assert.All(GameScreenFixtures.All.Where(f => f.Name.StartsWith("start-", StringComparison.Ordinal) || f.Name.StartsWith("dialog-", StringComparison.Ordinal)), f => Assert.Null(f.Masks));
        foreach (var scale in GameScreenFixtures.Scales)
            foreach (var name in new[] { "sheet-played-light", "sheet-drawer-closed-light" })
            {
                var fixture = GameScreenFixtures.All.Single(f => f.Name == name);
                using var h = GameScreenFixtures.Build(fixture, scale);
                var mask = Assert.Single(GameScreenFixtures.MaskBoxes(h.Document, fixture.Masks!));
                RectF Device(string selector)
                {
                    var r = h.Q(selector).GetBoundingClientRect();
                    return new RectF(r.X * scale, r.Y * scale, r.Width * scale, r.Height * scale);
                }
                // The seal: a circle turned in place, its 6 px ring of shadow round it, a pixel's margin.
                var seal = h.Q(SheetScreenFixtures.SealMask);
                Assert.True(seal.HasClass("hero-sigil"));
                var laid = seal.Box!.ViewportRect;
                var ring = 6 * scale;
                Assert.Equal(laid.X - ring - 1, mask.X, 0.01);
                Assert.Equal(laid.Y - ring - 1, mask.Y, 0.01);
                Assert.Equal(laid.Width + (2 * ring) + 2, mask.Width, 0.01);
                Assert.Equal(laid.Height + (2 * ring) + 2, mask.Height, 0.01);
                Assert.Equal(laid.Width, laid.Height);
                // Nothing else: inside the hero's frame, clear of its heading, its race and level line and the race's name.
                var hero = Device("section.hero");
                Assert.True(hero.Left <= mask.Left && mask.Right <= hero.Right && hero.Top <= mask.Top && mask.Bottom <= hero.Bottom, $"{name}@{scale}: the mask {mask} inside the hero {hero}");
                foreach (var other in new[] { ".hero-title", "#hero-race-name", ".hero .eyebrow", "#page-title", "#hero-standing" })
                    Assert.True(mask.Intersect(Device(other)).IsEmpty, $"{name}@{scale}: the mask {mask} over {other} {Device(other)}");
            }
    }

    [Fact]
    public void ALongWordInTheNameBreaksInsideTheHeading()
    {
        // A name is whatever the player typed: one word wider than the heading breaks inside it (`overflow-wrap:
        // anywhere`) instead of running past it under the seal or off the page.
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.Open(s, new string('W', 80), SheetHeroes.Played(SheetHeroes.Context(s.H), "human"), SheetHeroes.Look("human"));
        var name = s.Q("#page-title");
        s.Document.UpdateLayout();
        Assert.True(name.ScrollWidth <= name.ClientWidth, $"the name {name.ScrollWidth} px wide in a heading {name.ClientWidth} px wide");
    }

    // ---------- The paper doll and the rail ----------

    [Fact]
    public void ThePaperDollFoldsOnANarrowPage()
    {
        // Zoomed in, the page can be narrower than the window's least 820 px: under 760 px two columns, the panel first
        // across both, the hands side by side under it; under 470 px one column.
        using var s = new ScreensHarness(700, 700);
        SheetHeroes.OpenPlayed(s);
        var doll = s.Q(".paper-doll");
        RectF Box(UiElement e) => e.GetBoundingClientRect();
        var (panel, hand, off) = (Box(s.Q(".portrait-panel")), Box(doll.ElementChildren.ElementAt(0)), Box(doll.ElementChildren.ElementAt(2)));
        Assert.True(panel.Bottom <= hand.Top, $"the panel {panel} above the hands {hand}");
        Assert.Equal(Box(doll).Width, panel.Width, 0.5);
        Assert.Equal(hand.Top, off.Top, 0.5);
        Assert.True(hand.Right < off.Left, "the hands side by side");
        s.Document.SetViewport(440, 700);
        s.H.Frame();
        (hand, off) = (Box(doll.ElementChildren.ElementAt(0)), Box(doll.ElementChildren.ElementAt(2)));
        Assert.True(off.Top >= hand.Bottom, "one column");
        Assert.Equal(hand.Left, off.Left, 0.5);
    }

    [Fact]
    public void ThePaperDollHoldsThePortraitPanelBetweenTheHandsAndTheInertSlotsBelow()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        var doll = s.Q(".paper-doll");
        Assert.Equal(["primaryHand", null, "offHand", "armor", "artifact1", "artifact2"], doll.ElementChildren.Select(e => e.GetAttribute("data-equipment-slot")));
        Assert.Equal(["Primary hand", "Off-hand", "Armor", "Artifact I", "Artifact II"], doll.Descendants().Where(e => e.HasClass("equipment-slot-label")).Select(e => e.TextContent));
        // Q33: inert until GM3, the old "future" slot (nothing to press).
        Assert.All(doll.ElementChildren.Where(e => e.HasClass("equipment-slot")), slot =>
        {
            Assert.True(slot.HasClass("equipment-slot-future"));
            Assert.DoesNotContain(slot.Descendants(), e => e.IsButtonControl);
        });
        var panel = s.Q(".paper-doll > .portrait-panel");
        var view = s.Q($"#{SheetMarkup.ViewId}");
        Assert.Equal(("surface", panel), (view.Tag, view.Parent));
        var (hand, frame, off, armor) = (doll.ElementChildren.ElementAt(0).GetBoundingClientRect(), panel.GetBoundingClientRect(),
            doll.ElementChildren.ElementAt(2).GetBoundingClientRect(), doll.ElementChildren.ElementAt(3).GetBoundingClientRect());
        Assert.True(hand.Right < frame.Left && frame.Right < off.Left, "the hands at the panel's sides");
        Assert.True(armor.Top > frame.Bottom, "the armour below the panel");
        Assert.Equal(480, frame.Height, 0.5);
        Assert.Equal(hand.Top + hand.Height / 2, frame.Top + frame.Height / 2, 0.5);
        // The frame's four brass corner brackets over the leather's two gradients (thin lines, which the pictures' 0.2 %
        // can't hold).
        Assert.Equal(6, panel.Style!.BackgroundImages.Length);
        Assert.All(panel.Style.BackgroundImages.Take(4), image => Assert.StartsWith("data:image/svg+xml", image.Url));
        // The view fills the panel's window on its ground, the same in both themes (Q32).
        var ground = view.Style!.BackgroundColor;
        static int Byte(float channel) => (int)MathF.Round(channel * 255);
        Assert.Equal((0x2a, 0x1f, 0x1a, 255), (Byte(ground.R), Byte(ground.G), Byte(ground.B), Byte(ground.A)));
        // Its window: 20 px inside the panel's 1 px border all round; the border the frame's own brown (1 px, which the
        // pictures' 0.2 % can't hold), in both themes.
        Assert.Equal((frame.Width - 42, frame.Height - 42), (view.GetBoundingClientRect().Width, view.GetBoundingClientRect().Height));
        static (int, int, int, int) Rgba(Color c) => (Byte(c.R), Byte(c.G), Byte(c.B), Byte(c.A));
        Assert.Equal((0x6e, 0x55, 0x38, 255), Rgba(panel.Style!.BorderTopColor!.Value));
        s.Parts.Settings.Theme = GameTheme.Light;
        s.H.Frame();
        Assert.Equal(ground, view.Style!.BackgroundColor);
        Assert.Equal((0x6e, 0x55, 0x38, 255), Rgba(panel.Style!.BorderTopColor!.Value));
        // The page's end clears the Settings button at the window's bottom (its 16 px and 38 px).
        Assert.Equal(72, s.Q("main.game-page").Style!.PaddingBottom.ResolveOr(1440, -1));
        // The slots below keep one height when one of them holds more (GM3's items; all alike until then): each is
        // stretched to its row.
        var alike = armor.Height;
        s.Q("[data-equipment-slot=\"artifact1\"] .equipment-slot-label").TextContent =
            string.Join(" ", Enumerable.Repeat("a long item name", 20));
        s.H.Frame();
        var below = doll.ElementChildren.Skip(3).Select(e => e.GetBoundingClientRect().Height).ToList();
        Assert.True(below[1] > alike + 10, $"the long slot {below[1]} px against {alike}");
        Assert.Equal(below[1], below[0], 0.5);
        Assert.Equal(below[1], below[2], 0.5);
    }

    [Fact]
    public void TheRailHasEveryToolDisabledUntilItsMilestoneAndThePurseAndCompanions()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        var tools = s.Document.QuerySelectorAll(".main-tools-actions .spellbook-button").ToList();
        Assert.Equal(["map-open-button", "combat-open-button", "spellbook-open-button", "inventory-open-button", "companions-open-button", "classes-open-button", "skills-open-button", "passives-open-button"], tools.Select(t => t.Id));
        Assert.Equal(["Map", "Combat", "Spell book", "Inventory", "Companions", "Classes", "Skills", "Passives"], tools.Select(t => t.Descendants().First(e => e.Tag == "strong").TextContent));
        Assert.All(tools, t => Assert.True(t.IsDisabled));
        Assert.True(s.Q("#combat-open-button").HasAttribute("data-dev-only"));
        Assert.Equal(("0 / 3", "No companions recruited yet."), (s.Q("#companion-count").TextContent, s.Q(".companion-empty").TextContent));
        Assert.Equal(["Silver", "Gold", "Platinum"], s.Document.QuerySelectorAll(".currency-name").Select(e => e.TextContent));
        var rail = s.Q("#field-tools-menu");
        Assert.False(rail.Inert);
        Assert.Null(rail.GetAttribute("aria-hidden"));
        Assert.Equal("none", s.Q("#field-tools-toggle").Style!.Display.ToString().ToLowerInvariant());
        Assert.Equal(("STAT FICK / FIELD NOTES", "Every legend begins with a single point."),
            (s.Q("main.game-page > .footer").ElementChildren.First().TextContent, s.Q("main.game-page > .footer").ElementChildren.Last().TextContent));
    }

    [Fact]
    public void UnderEightHundredFortyPixelsTheRailIsADrawerAsTheOldInterfaceMadeIt()
    {
        using var s = new ScreensHarness(820, 700);
        s.Parts.Settings.DevMode = true;
        SheetHeroes.OpenPlayed(s);
        var (toggle, shade, rail) = (s.Q("#field-tools-toggle"), s.Q("#field-tools-shade"), s.Q("#field-tools-menu"));
        Assert.True(s.Sheet.Tools.Compact);
        Assert.True(rail.Inert);
        Assert.Equal("true", rail.GetAttribute("aria-hidden"));
        Assert.Equal(("false", true), (toggle.GetAttribute("aria-expanded"), shade.Hidden));
        Assert.NotEqual("none", toggle.Style!.Display.ToString().ToLowerInvariant());
        // The toggle opens it, the focus on its first control (the silver, Dev Mode's).
        s.H.Click(toggle);
        Assert.True(s.Sheet.Tools.IsOpen);
        Assert.True(s.Document.Body.HasClass(FieldTools.OpenClass));
        Assert.Equal(("true", false, false), (toggle.GetAttribute("aria-expanded"), shade.Hidden, rail.Inert));
        Assert.Same(s.Q("#currency-silver"), s.Document.FocusedElement);
        // Tab stays among its controls and the toggle: from the toggle to the first, back from the first to the toggle.
        toggle.Focus();
        s.H.Key("Tab", "Tab");
        Assert.Same(s.Q("#currency-silver"), s.Document.FocusedElement);
        s.H.Key("Tab", "Tab", KeyModifiers.Shift);
        Assert.Same(toggle, s.Document.FocusedElement);
        // Escape closes it, the focus back on the toggle from inside; the menu isn't opened.
        s.H.Key("Tab", "Tab");
        Assert.Same(s.Q("#currency-silver"), s.Document.FocusedElement);
        s.H.Key("Escape", "Escape");
        Assert.False(s.Sheet.Tools.IsOpen);
        Assert.True(rail.Inert);
        Assert.Same(toggle, s.Document.FocusedElement);
        Assert.Empty(s.H.Ui.Windows.Layers);
        // The toggle again closes it; the shade closes it.
        s.H.Click(toggle);
        s.H.Click(toggle);
        Assert.False(s.Sheet.Tools.IsOpen);
        s.H.Click(toggle);
        s.H.Click(new System.Numerics.Vector2(700, 400));
        Assert.False(s.Sheet.Tools.IsOpen);
        // Closed, Escape is the game's again (the menu).
        s.H.Key("Escape", "Escape");
        Assert.True(s.H.Ui.Windows.IsOpen(s.Menu.Layer));
        s.H.Ui.Windows.CloseTop();
        // A tool's click closes it without taking the focus back (its window takes it).
        s.H.Click(toggle);
        s.Q("#map-open-button").DispatchEvent(new UiEvent("click"));
        Assert.False(s.Sheet.Tools.IsOpen);
        // A window over the open drawer takes Escape.
        s.H.Click(toggle);
        var window = s.H.Layer("over");
        s.H.Ui.Windows.Open(window);
        s.H.Key("Escape", "Escape");
        Assert.False(s.H.Ui.Windows.IsOpen(window));
        Assert.True(s.Sheet.Tools.IsOpen);
        // Wider than 840 px: closed, the rail shown on the page again.
        s.Document.SetViewport(1000, 700);
        s.H.Frame();
        Assert.False(s.Sheet.Tools.Compact);
        Assert.False(s.Sheet.Tools.IsOpen);
        Assert.False(rail.Inert);
        Assert.False(s.Document.Body.HasClass(FieldTools.OpenClass));
        // The character left: the drawer and its toggle go with the page.
        s.Document.SetViewport(820, 700);
        s.H.Frame();
        s.H.Click(toggle);
        s.Session.Leave();
        s.H.Frame();
        Assert.False(s.Sheet.Tools.IsOpen);
        Assert.True(toggle.Hidden);
        Assert.Equal("none", toggle.Style!.Display.ToString().ToLowerInvariant());
    }

    [Fact]
    public void OutsideDevModeTheDrawersOnlyStopIsItsToggle()
    {
        using var s = new ScreensHarness(820, 700);
        SheetHeroes.OpenPlayed(s);
        var toggle = s.Q("#field-tools-toggle");
        // Every control in the rail is disabled (the purse outside Dev Mode, the tools): none is the drawer's to focus
        // (the old `button:not(:disabled), input:not(:disabled)`), so the toggle keeps the focus and Tab can't leave it.
        s.H.Click(toggle);
        Assert.True(s.Sheet.Tools.IsOpen);
        Assert.Same(toggle, s.Document.FocusedElement);
        s.H.Key("Tab", "Tab", KeyModifiers.Shift);
        Assert.Same(toggle, s.Document.FocusedElement);
        s.H.Key("Tab", "Tab");
        Assert.Same(toggle, s.Document.FocusedElement);
    }

    // ---------- The old page's words and numbers ----------

    [Fact]
    public void TheExpCardsTextsAreTheOldPagesOnEveryCase()
    {
        var cases = OldGameReference.Load("sheet-values").Cases;
        var wrong = new List<string>();
        foreach (var (input, output) in cases)
        {
            var sheet = SheetCases.Sheet(input.GetProperty("sheet"));
            var dev = SheetCases.DevMode(input);
            var values = ActorRules.Evaluate(sheet, SheetCases.Context(input), dev);
            var texts = output.GetProperty("texts");
            var ours = (SheetTexts.Cap(values), SheetTexts.Threshold(values), SheetTexts.Helper(sheet, values, dev), SheetTexts.BarWidth(values),
                SheetTexts.BarMaximum(values), SheetTexts.BarValue(sheet, values));
            var theirs = (texts.GetProperty("cap").GetString(), texts.GetProperty("threshold").GetString(), texts.GetProperty("helper").GetString(),
                texts.GetProperty("barWidth").GetString(), texts.GetProperty("valueMax").GetString(), texts.GetProperty("valueNow").GetString());
            if (ours != theirs)
                wrong.Add($"{input.GetRawText()}\n  old {theirs}\n  port {ours}");
        }
        Assert.True(cases.Count >= 300, $"{cases.Count} cases");
        Assert.True(wrong.Count == 0, $"{wrong.Count} of {cases.Count} differ:\n" + string.Join("\n", wrong.Take(10)));
    }

    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(-0.0, "0")]
    [InlineData(31.0, "31")]
    [InlineData(-7.0, "-7")]
    [InlineData(0.30000000000000004, "0.30000000000000004")]
    [InlineData(59.13978494623656, "59.13978494623656")]
    [InlineData(1e21, "1e+21")]
    [InlineData(1.5e21, "1.5e+21")]
    [InlineData(123456789012345680000.0, "123456789012345680000")]
    [InlineData(1e-6, "0.000001")]
    [InlineData(1.25e-6, "0.00000125")]
    [InlineData(1e-7, "1e-7")]
    [InlineData(-2.5e-8, "-2.5e-8")]
    [InlineData(5e-324, "5e-324")]
    [InlineData(1.7976931348623157e308, "1.7976931348623157e+308")]
    [InlineData(double.NaN, "NaN")]
    [InlineData(double.PositiveInfinity, "Infinity")]
    [InlineData(double.NegativeInfinity, "-Infinity")]
    public void ANumberIsWrittenAsJavaScriptsString(double value, string text) => Assert.Equal(text, SheetTexts.Number(value));

    [Theory]
    [InlineData(1650.0, "1650")]
    [InlineData(2.25, "2.3")]
    [InlineData(2.35, "2.4")]
    [InlineData(-1.25, "-1.2")]
    [InlineData(0.04, "0")]
    [InlineData(1900.5, "1900.5")]
    [InlineData(1e21, "1e+21")]
    public void AValueIsWrittenAsTheOldFormatValue(double value, string text) => Assert.Equal(text, SheetTexts.Value(value));

    [Fact]
    public void TheMetersBarsAndRaceWordsAreTheOldPagesPresentation()
    {
        Assert.Equal("scaleX(0.7312775330396476)", SheetTexts.Meter(166, 227));
        Assert.Equal("scaleX(1)", SheetTexts.Meter(300, 227));
        Assert.Equal("scaleX(0)", SheetTexts.Meter(-1, 227));
        Assert.Equal("scaleX(0)", SheetTexts.Meter(10, 0));
        Assert.Equal(("7%", "7%", "100%", "100%", "56.00000000000001%"),
            (SheetTexts.StatBar(0), SheetTexts.StatBar(1), SheetTexts.StatBar(25), SheetTexts.StatBar(-40), SheetTexts.StatBar(14)));
        Assert.Equal(("Unknown race", "Race locked", "Change race"), (SheetTexts.RaceName(null), SheetTexts.SealTitle(null, false), SheetTexts.SealTitle(null, true)));
        var spaced = new Data.Races.Race { Name = " Moon folk\u00a0", Passive = new Data.Races.RacePassive { Name = "Tides" } };
        Assert.Equal(("Moon folk", "Moon folk · Race locked", "Moon folk · Level 3"),
            (SheetTexts.RaceName(spaced), SheetTexts.SealTitle(spaced, false), SheetTexts.Standing(spaced, new ActorSheet { Level = 3 })));
        Assert.Equal(12, SheetScreen.NumberOf(" 12 "));
        Assert.Equal(0, SheetScreen.NumberOf(""));
        Assert.Equal(-1, SheetScreen.NumberOf("e", -1));
        Assert.Equal(1000, SheetScreen.NumberOf("1e3"));
        // JavaScript's Number: white space alone is 0, a no-break space is white space, and no infinity is a number.
        Assert.Equal(0, SheetScreen.NumberOf("   ", -1));
        Assert.Equal(12, SheetScreen.NumberOf(" 12", -1));
        Assert.Equal(-1, SheetScreen.NumberOf("Infinity", -1));
        Assert.Equal(-1, SheetScreen.NumberOf("1e999", -1));
    }
}
