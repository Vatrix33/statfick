using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Races;
using Statventure.Game.Screens.Races;
using Statventure.Game.Sounds;
using Statventure.Platform;

namespace Statventure.Game.Tests;

/// <summary>
/// The race carousel (GM2 step 8; Q28, Q27, Q36, the user's answers of 2026-11-16): the playable races (every race in Dev
/// Mode, followed live) in the races' order, one at a time with the old list's content; browsing by the stage's buttons,
/// the arrow keys and the strip, round from one end to the other; the old refusal of another race outside Dev Mode
/// ("Race locked"); a playable race with no looks shown but not choosable; a playable race taken (the maker opened, nothing
/// saved yet) and an enemy-only one taken at once (saved, the sheet asked for); × leaving the flow; the sounds (GM2-P17);
/// who may open it (Q26, GM2-7); <c>checkNewRacePicker</c>'s expectation.
/// </summary>
public class RacesScreenTests
{
    static readonly string[] Playable = ["human", "elf", "dark-elf", "dwarf", "hobbit", "lizardman", "half-beast", "goliath", "merfolk"];

    /// <summary>The races the game's data has looks for: the human (GM2 step 9a) and each race steps 9b–9e have brought in.</summary>
    static readonly string[] Shipped = ["human", "elf", "dark-elf"];

    static HeroScreensHarness Opened(Action<HeroScreensHarness>? before = null, string? race = null, bool shipped = false)
    {
        var s = new HeroScreensHarness(shipped: shipped);
        s.NewHero();
        before?.Invoke(s);
        s.Run(HeroFlow.RacesCommand, race);
        return s;
    }

    [Fact]
    public void ANewHeroSeesThePlayableRacesInTheirOrderFromTheFirst()
    {
        using var s = Opened();
        Assert.True(s.Races.Showing);
        Assert.Equal(Playable, s.Races.List);
        Assert.Equal("human", s.Races.Shown);
        Assert.Equal(("Human", "Playable", "Choose Human", true, ""), s.Carousel());
        Assert.Equal(["STR +2", "VIT +1", "MND -3", "DEX -1", "INT +1", "WIS +0", "SPD -1", "LCK +1"],
            s.Document.QuerySelectorAll("#races-modifiers > span").Select(c => c.TextContent));
        Assert.Equal("Rulers of the World: Gain 2 additional maximum HP every fifth character level.", s.Q("#races-passive").TextContent);
        Assert.Equal(HeroTexts.RacesIntro, s.Q("#races-screen .races-intro").TextContent);
        Assert.Equal(("ANCESTRY", "Choose a race"), (s.Q("#races-screen .eyebrow").TextContent, s.Q("#races-title").TextContent));
        Assert.Same(s.Q("#races-choose"), s.Document.FocusedElement);
        Assert.Equal(HeroFlow.LeaveToStart, s.Q("#races-leave").GetAttribute("aria-label"));
        Assert.Equal("--race-accent: #c7683f", s.Q("#races-race").GetAttribute("style"));
        // The strip: a medallion a race, the shown one marked; no rule between kinds outside Dev Mode.
        var items = s.Document.QuerySelectorAll("#races-strip .races-strip-item").ToList();
        Assert.Equal(Playable, items.Select(i => i.GetAttribute("data-race")));
        Assert.Equal(["human"], items.Where(i => i.HasClass("is-current") && i.GetAttribute("aria-current") == "true").Select(i => i.GetAttribute("data-race")));
        Assert.Empty(s.Document.QuerySelectorAll("#races-strip .races-strip-divider"));
        Assert.Contains(RacesScreen.Stylesheet, s.H.Ui.Sheets);
        // Nothing is saved by opening it, and the maker doesn't show.
        Assert.Null(s.Session.Current!.Hero.Race);
        Assert.False(s.Maker.Showing);
    }

    [Fact]
    public void CheckNewRacePicker()
    {
        // new-races-browser.js checkNewRacePicker: "The race list offers <id> with its seal" for the three new races. The
        // port's seal is the race's emblem, by its name (Q44); its pictures are GM2 step 10's.
        using var s = Opened();
        foreach (var id in new[] { "half-beast", "goliath", "merfolk" })
        {
            Assert.Equal(id, s.Q($"#races-strip .races-strip-item[data-race=\"{id}\"]").GetAttribute("data-emblem"));
            s.Click(s.Q($"#races-strip .races-strip-item[data-race=\"{id}\"]"));
            Assert.Equal(id, s.Races.Shown);
            Assert.Equal(id, s.Q("#races-emblem").GetAttribute("data-emblem"));
        }
    }

    [Fact]
    public void TheButtonsTheKeysAndTheStripBrowseRoundFromOneEndToTheOther()
    {
        using var s = Opened();
        s.Click(s.Q("#races-next"));
        Assert.Equal("elf", s.Races.Shown);
        s.Click(s.Q("#races-previous"));
        s.Click(s.Q("#races-previous"));
        Assert.Equal("merfolk", s.Races.Shown);
        s.Click(s.Q("#races-next"));
        Assert.Equal("human", s.Races.Shown);
        Assert.True(!s.Key("ArrowLeft"));
        Assert.Equal("merfolk", s.Races.Shown);
        s.Key("ArrowRight");
        s.Key("ArrowRight");
        Assert.Equal("elf", s.Races.Shown);
        // With a modifier, or another key, or a key the page took already, nothing moves.
        foreach (var modifier in new[] { KeyModifiers.Shift, KeyModifiers.Control, KeyModifiers.Alt, KeyModifiers.Super })
            Assert.True(s.H.Key("ArrowRight", "ArrowRight", modifier));
        Assert.True(s.Key("ArrowDown"));
        Assert.Equal("elf", s.Races.Shown);
        s.Document.Body.AddEventListener<UI.Dom.UiKeyboardEvent>("keydown", e => e.PreventDefault());
        s.Key("ArrowRight");
        Assert.Equal("elf", s.Races.Shown);
        s.Click(s.Q("#races-strip .races-strip-item[data-race=\"goliath\"]"));
        Assert.Equal(("Goliath", "Playable"), (s.Carousel().Name, s.Carousel().Kind));
        Assert.Equal(["goliath"], s.Document.QuerySelectorAll("#races-strip .is-current").Select(i => i.GetAttribute("data-race")));
        // Browsing sounds only GM1's press of its buttons, nothing for the keys.
        s.H.Sounds.Clear();
        s.Key("ArrowRight");
        s.H.Frame();
        Assert.Empty(s.H.Sounds);
        s.Click(s.Q("#races-next"));
        Assert.Equal([SoundNames.Press], s.H.Sounds);
    }

    [Fact]
    public void TheKeysDontBrowseUnderAWindowNorWhileTheCarouselIsHidden()
    {
        using var s = Opened();
        var window = s.H.Layer("over");
        s.H.Ui.Windows.Open(window);
        s.H.Frame();
        s.Key("ArrowRight");
        Assert.Equal("human", s.Races.Shown);
        s.H.Ui.Windows.Close(window);
        s.Session.Leave();
        Assert.False(s.Races.Showing);
        s.Key("ArrowRight");
        Assert.Null(s.Races.Shown);
    }

    [Fact]
    public void DevModeListsEveryRaceInTheRacesOrderAndIsFollowedLive()
    {
        using var s = Opened(before: h => h.H.Settings.DevMode = true, race: "goblin");
        var all = RaceCatalogue.Load(s.Data).Select(r => r.Id).ToList();
        Assert.Equal(28, all.Count);
        Assert.Equal(all, s.Races.List);
        Assert.Equal(("Goblin", "Non-playable · Dev Mode", "Choose Goblin", true, ""), s.Carousel());
        Assert.True(s.Q("#races-race").HasClass("is-enemy"));
        // A rule where the list goes from playable to enemy-only and back (the Merfolk last, as races.json has it).
        var strip = s.Q("#races-strip").ElementChildren.Select(e => e.HasClass("races-strip-divider") ? "|" : e.GetAttribute("data-race")).ToList();
        Assert.Equal([.. Playable[..8], "|", .. all[8..27], "|", "merfolk"], strip);
        Assert.True(s.Q("#races-strip .races-strip-item[data-race=\"goblin\"]").HasClass("is-enemy"));
        // An enemy-only race has no emblem: the medallion names none (the ✦).
        Assert.False(s.Q("#races-emblem").HasAttribute("data-emblem"));
        // Dev Mode turned off while it shows: the playable races only, the first shown in place of the enemy-only one.
        s.H.Settings.DevMode = false;
        s.H.Frame();
        Assert.Equal(Playable, s.Races.List);
        Assert.Equal("human", s.Races.Shown);
        s.Click(s.Q("#races-next"));
        s.H.Settings.DevMode = true;
        s.H.Frame();
        Assert.Equal("elf", s.Races.Shown);
        Assert.Equal(28, s.Races.List.Count);
        // From a race with an emblem to one without: the medallion names none any more.
        Assert.Equal("elf", s.Q("#races-emblem").GetAttribute("data-emblem"));
        s.Click(s.Q("#races-strip .races-strip-item[data-race=\"goblin\"]"));
        Assert.False(s.Q("#races-emblem").HasAttribute("data-emblem"));
    }

    [Fact]
    public void AnotherRaceThanTheHerosOwnIsLockedOutsideDevMode()
    {
        using var s = Opened(before: h => h.Session.Change(save => save with { Hero = save.Hero with { Race = "human" } }));
        Assert.Equal("human", s.Races.Shown);
        Assert.Equal(("Human", "Selected", "Choose Human", true, ""), s.Carousel());
        s.Key("ArrowRight");
        Assert.Equal(("Elf", "Playable", "Race locked", false, "A chosen race can only be changed in Dev Mode"), s.Carousel());
        Assert.True(s.Q("#races-status").HasClass("is-error"));
        // The focus, lost with the disabled button (the document's focus fixup blurs it a task later), comes back to it at
        // a race that can be taken.
        s.H.Frame();
        s.H.Frame();
        Assert.NotSame(s.Q("#races-choose"), s.Document.FocusedElement);
        s.Key("ArrowLeft");
        Assert.Same(s.Q("#races-choose"), s.Document.FocusedElement);
        Assert.Equal("", s.Carousel().Status);
        // A click on the locked button does nothing.
        s.Key("ArrowRight");
        s.H.Sounds.Clear();
        s.Click(s.Q("#races-choose"));
        Assert.True(s.Races.Showing);
        Assert.False(s.Maker.Showing);
        Assert.Empty(s.H.Sounds);
        // In Dev Mode it isn't locked.
        s.H.Settings.DevMode = true;
        s.H.Frame();
        Assert.Equal(("Elf", "Playable", "Choose Elf", false, "No looks for the Elf yet"), s.Carousel());
        s.Click(s.Q("#races-strip .races-strip-item[data-race=\"half-beast\"]"));
        Assert.Equal(("Half-Beast", "Playable", "Choose Half-Beast", true, ""), s.Carousel());
    }

    [Fact]
    public void APlayableRaceWithNoLooksIsShownButNotChoosable()
    {
        // The test data has looks for the human, the dwarf, the Half-Beast and the lizardman only.
        using var s = Opened(race: "elf");
        Assert.Equal(("Elf", "Playable", "Choose Elf", false, "No looks for the Elf yet"), s.Carousel());
        Assert.True(s.Q("#races-status").HasClass("is-error"));
        Assert.Same(s.Q("#races-leave"), s.Document.FocusedElement);
        s.Click(s.Q("#races-choose"));
        Assert.True(s.Races.Showing);
        Assert.False(s.Maker.Showing);
        Assert.Empty(s.Ran);
        // The game's data since GM2 step 5 has looks for the human, since steps 9b–9e for each race made (Shipped): every
        // playable race is shown, those alone can be taken, and the maker opens on the human's presets.
        using var shipped = Opened(shipped: true);
        Assert.Equal(Playable, shipped.Races.List);
        foreach (var id in Playable)
        {
            Assert.Equal(id, shipped.Races.Shown);
            Assert.Equal(Shipped.Contains(id), shipped.Carousel().Enabled);
            Assert.Equal(Shipped.Contains(id) ? "" : $"No looks for the {shipped.Carousel().Name} yet", shipped.Carousel().Status);
            shipped.Key("ArrowRight");
        }
        shipped.Click(shipped.Q("#races-choose"));
        Assert.True(shipped.Maker.Showing);
        Assert.Equal(8, shipped.Document.QuerySelectorAll("#maker-presets .maker-preset").Count());
    }

    [Fact]
    public void APlayableRaceTakenOpensTheMakerAndSavesNothingYet()
    {
        using var s = Opened();
        s.H.Sounds.Clear();
        s.Click(s.Q("#races-choose"));
        Assert.False(s.Races.Showing);
        Assert.True(s.Maker.Showing);
        Assert.Equal("human", s.Maker.Race);
        Assert.Equal([SoundNames.RaceChosen], s.H.Sounds);
        Assert.Null(s.Session.Current!.Hero.Race);
    }

    [Fact]
    public void AnEnemyOnlyRaceIsTakenAtOnceWithNoLookAndTheSheetAskedFor()
    {
        using var s = Opened(before: h => h.H.Settings.DevMode = true, race: "goblin");
        var before = s.Session.Current!.Hero;
        s.H.Sounds.Clear();
        s.Click(s.Q("#races-choose"));
        var save = s.Session.Current!;
        Assert.Equal("goblin", save.Hero.Race);
        Assert.Null(save.Look);
        // HP and Mana filled to the new maxima, as selectRace (ActorRules.ChooseRace).
        Assert.Equal(ActorRules.ChooseRace(before, SheetContext.Plain.WithRaces(RaceCatalogue.Load(s.Data)), "goblin", true).Sheet, save.Hero);
        Assert.Equal([SoundNames.RaceChosen], s.H.Sounds);
        Assert.Equal([HeroFlow.SheetCommand], s.Ran);
        Assert.False(s.Races.Showing);
        Assert.False(s.Maker.Showing);
        // The saved file holds it.
        s.H.Storage.Flush();
        Assert.Equal("goblin", s.H.Storage.Saves.Load<Data.Saves.SaveGame>(s.Session.CurrentSlot!).Value.Hero.Race);
    }

    [Fact]
    public void TheCrossLeavesForTheStartScreenOrForAFinishedHeroBackToTheSheet()
    {
        using var s = Opened();
        s.Click(s.Q("#races-leave"));
        Assert.False(s.Races.Showing);
        Assert.Equal([StatFickGame.StartCommand], s.Ran);
        // A finished hero in Dev Mode (an enemy-only one has no look and needs none): back to the sheet.
        s.Ran.Clear();
        s.H.Settings.DevMode = true;
        s.Session.Change(save => save with { Hero = save.Hero with { Race = "goblin" } });
        s.Run(HeroFlow.RacesCommand);
        Assert.Equal(("goblin", HeroFlow.LeaveToSheet), (s.Races.Shown, s.Q("#races-leave").GetAttribute("title")));
        s.Click(s.Q("#races-leave"));
        Assert.Equal([HeroFlow.SheetCommand], s.Ran);
        Assert.Equal("goblin", s.Session.Current!.Hero.Race);
    }

    [Fact]
    public void ItHidesWhenTheCharacterIsLeftOrAnotherOpened()
    {
        using var s = Opened();
        s.Session.Leave();
        Assert.False(s.Races.Showing);
        s.NewHero("Mira");
        s.Run(HeroFlow.RacesCommand);
        Assert.True(s.Races.Showing);
        s.NewHero("Cadfael");
        Assert.False(s.Races.Showing);
    }

    [Fact]
    public void ItOpensOnlyForAHeroWhoNeedsTheFlowOrInDevMode()
    {
        using var s = new HeroScreensHarness();
        Assert.False(s.H.Ui.Commands.IsEnabled(HeroFlow.RacesCommand));
        Assert.False(s.Run(HeroFlow.RacesCommand));
        s.NewHero();
        Assert.True(s.H.Ui.Commands.IsEnabled(HeroFlow.RacesCommand));
        // A finished hero (a race and a look) outside Dev Mode: the look is final (GM2-7).
        s.Session.Change(save => save with { Hero = save.Hero with { Race = "human" }, Look = HeroTestData.Presets["test-human-male-1"].Look });
        Assert.False(s.Run(HeroFlow.RacesCommand));
        Assert.False(s.Races.Showing);
        s.H.Settings.DevMode = true;
        Assert.True(s.Run(HeroFlow.RacesCommand));
        Assert.Equal(("human", "Selected"), (s.Races.Shown, s.Carousel().Kind));
    }

    [Fact]
    public void WithoutRacesInTheDataTheOldEmptyListShows()
    {
        using var h = new GameUiHarness(1240, 900);
        var s = new HeroScreensHarness(h);
        s.Races.Data = () => null;
        s.NewHero();
        s.Run(HeroFlow.RacesCommand);
        Assert.True(s.Races.Showing);
        Assert.False(s.Q("#races-empty").Hidden);
        Assert.True(s.Q("#races-screen .races-stage").Hidden);
        Assert.Null(s.Races.Shown);
        s.Click(s.Q("#races-leave"));
        Assert.Equal([StatFickGame.StartCommand], s.Ran);
    }
}
