using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Saves;
using Statventure.Game.Screens.Races;
using Statventure.Game.Sounds;
using Statventure.Packs;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The new hero's flow (GM2 step 8; Q25–Q28, GM2-7, the user's answers of 2026-11-16): name, race, maker, sheet, from
/// GM1's New character to the sheet asked for with the hero and its look saved together; which hero still needs it
/// (Q26, the one rule the sheet asks too); where it is left to; the log lines a later flow reads. The whole flow runs on the
/// game's own start screen and sheet (GM2 step 7's, which asks for <c>open-races</c> on every open of a hero that needs it
/// and shows on <c>open-sheet</c>); the other tests stand the sheet in.
/// </summary>
public class HeroFlowTests
{
    static HeroData Data(HeroScreensHarness s) => HeroData.Of(s.Data);

    [Fact]
    public void NameRaceMakerSheet()
    {
        using var s = new HeroScreensHarness(sheet: true);
        s.Run(StatFickGame.StartCommand);
        // GM1's New character: the name, then the character made and opened (no race, no look).
        s.Click(s.Q("#start-new"));
        s.H.Frame();
        s.Q(".game-prompt-field").Value = "Mira of the Vale";
        s.Click(s.Q(".game-dialog-accept"));
        s.H.Frame();
        var slot = s.Session.CurrentSlot;
        Assert.Equal("mira-of-the-vale", slot);
        Assert.True(HeroFlow.Needs(s.Session.Current!, Data(s)));
        // The sheet asks for the carousel (Q26) and stays hidden.
        Assert.True(s.Races.Showing);
        Assert.False(s.Sheet!.Showing);
        s.Click(s.Q("#races-choose"));
        Assert.True(s.Maker.Showing);
        s.Click(s.Q("#maker-presets .maker-preset[data-preset=\"test-human-male-4\"]"));
        s.Click(s.Q("#maker-begin"));
        // The sheet shows, the hero finished and saved in its slot.
        Assert.True(s.Sheet.Showing);
        Assert.Equal(("Mira of the Vale", "Human"), (s.Q("#page-title").TextContent, s.Q("#hero-race-name").TextContent));
        Assert.Empty(s.Ran);
        var save = s.Session.Current!;
        Assert.Equal(("Mira of the Vale", "human"), (save.Name, save.Hero.Race));
        Assert.Equal(HeroTestData.Presets["test-human-male-4"].Look with { Preset = "test-human-male-4" }, save.Look);
        Assert.False(HeroFlow.Needs(save, Data(s)));
        s.H.Storage.Flush();
        Assert.Equal(save, s.H.Storage.Saves.Load<SaveGame>(slot!).Value);
        Assert.False(s.Races.Showing || s.Maker.Showing || s.Start!.Showing);
    }

    [Fact]
    public void AHeroLeftUnfinishedOpensOnTheFlowAgainAndAGm1CharacterIsOne()
    {
        using var s = new HeroScreensHarness(sheet: true);
        s.Run(StatFickGame.StartCommand);
        s.NewHero();
        s.H.Frame();
        Assert.True(s.Races.Showing);
        s.Click(s.Q("#races-choose"));
        // × in the maker: the start screen, the hero saved as it was (no race, no look).
        s.Click(s.Q("#maker-leave"));
        Assert.Null(s.Session.Current);
        Assert.True(s.Start!.Showing);
        // Continue: the sheet asks for the flow again, on the carousel.
        s.Click(s.Q("#start-continue"));
        Assert.True(HeroFlow.Needs(s.Session.Current!, Data(s)));
        Assert.True(s.Races.Showing);
        Assert.False(s.Sheet!.Showing || s.Start.Showing);
        // A GM1 character (save.game version 1, a name only) upgrades to a hero with no race and no look: it needs the flow.
        var v1 = new SaveGame.V1 { Name = "Ser Alrik" }.Upgrade();
        Assert.True(HeroFlow.Needs(v1, Data(s)));
    }

    [Fact]
    public void WhichHeroNeedsTheFlow()
    {
        using var s = new HeroScreensHarness();
        var data = Data(s);
        var hero = new SaveGame { Name = "A" };
        var look = HeroTestData.Presets["test-human-male-1"].Look;
        Assert.True(HeroFlow.Needs(hero, data));
        Assert.True(HeroFlow.Needs(hero with { Hero = hero.Hero with { Race = "" } }, data));
        // A playable race with looks and no look: the maker still to come.
        Assert.True(HeroFlow.Needs(hero with { Hero = hero.Hero with { Race = "human" } }, data));
        Assert.False(HeroFlow.Needs(hero with { Hero = hero.Hero with { Race = "human" }, Look = look }, data));
        // An enemy-only race (Dev Mode's, Q27), even one with a body, and a race the data hasn't: no look, the sheet.
        Assert.False(HeroFlow.Needs(hero with { Hero = hero.Hero with { Race = "goblin" } }, data));
        Assert.True(data.HasLooks("orc"));
        Assert.False(HeroFlow.Needs(hero with { Hero = hero.Hero with { Race = "orc" } }, data));
        Assert.False(HeroFlow.Needs(hero with { Hero = hero.Hero with { Race = "nowhere" } }, data));
        Assert.False(HeroFlow.Needs(hero with { Hero = hero.Hero with { Race = "human" } }, HeroData.Of(null)));
        // A playable race with no looks in the data: still the flow (Q26's words), where the carousel shows it not choosable (Q53).
        Assert.False(data.HasLooks("elf"));
        Assert.True(HeroFlow.Needs(hero with { Hero = hero.Hero with { Race = "elf" } }, data));
        Assert.False(HeroFlow.Needs(hero with { Hero = hero.Hero with { Race = "elf" }, Look = look with { Race = "elf" } }, data));
    }

    [Fact]
    public void TheFlowsDataIsTheRacesInTheirOrderAndTheMakersRecords()
    {
        using var s = new HeroScreensHarness();
        var data = Data(s);
        Assert.Equal(28, data.Races.Count);
        Assert.Equal("human", data.Races[0].Id);
        Assert.Equal("Human", data.Race("human")!.Name);
        Assert.Null(data.Race("nowhere"));
        Assert.True(data.HasLooks("human"));
        Assert.True(data.HasLooks("dwarf"));
        Assert.False(data.HasLooks("elf"));
        Assert.Equal(13, data.Looks.Bodies.Count);
        Assert.Equal(28, data.Context.Races.Count);
        var none = HeroData.Of(null);
        Assert.Empty(none.Races);
        Assert.Empty(none.Looks.Bodies);
        // The game's data: the human's looks since GM2 step 5, the test data's none of them (HeroTestData leaves them out).
        using var shipped = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "data.pak"))]);
        var game = HeroData.Of(shipped);
        Assert.Equal(["human"], game.Races.Where(r => game.HasLooks(r.Id)).Select(r => r.Id));
        Assert.NotEmpty(game.Looks.Presets);
        Assert.All(data.Looks.Presets.Keys.Concat(data.Looks.Parts.Keys), name => Assert.StartsWith("test-", name));
    }

    [Fact]
    public void TheFlowsCommandsAreRegisteredByItsScreens()
    {
        using var s = new HeroScreensHarness();
        Assert.True(s.H.Ui.Commands.IsRegistered(HeroFlow.RacesCommand));
        Assert.True(s.H.Ui.Commands.IsRegistered(HeroFlow.MakerCommand));
        Assert.Equal(("open-races", "open-maker", "open-sheet"), (HeroFlow.RacesCommand, HeroFlow.MakerCommand, HeroFlow.SheetCommand));
        // The game attaches both, and the sheet (GM2 step 7's) answers open-sheet.
        Assert.Contains(StatFickGame.NewScreens(), screen => screen is RacesScreen);
        Assert.Contains(StatFickGame.NewScreens(), screen => screen is Screens.Maker.MakerScreen);
    }

    [Fact]
    public void FinishingRefusedChangesNothing()
    {
        using var s = new HeroScreensHarness();
        s.NewHero();
        s.Session.Change(save => save with { Hero = save.Hero with { Race = "dwarf" } });
        var before = s.Session.Current;
        Assert.Equal(RaceRefusal.Locked, HeroFlow.Finish(s.Parts, Data(s), "human", HeroTestData.Presets["test-human-male-1"].Look));
        Assert.Equal(RaceRefusal.NotPlayable, HeroFlow.Finish(s.Parts, Data(s), "goblin", null));
        Assert.Same(before, s.Session.Current);
        Assert.Equal(RaceRefusal.Locked, HeroFlow.Refusal(s.Parts, Data(s), "human"));
        Assert.Null(HeroFlow.Refusal(s.Parts, Data(s), "dwarf"));
        // An enemy-only race taken again (Dev Mode) is chosen again, HP and Mana filled, as the old selectRace; a look for
        // the hero's own race keeps them (the old appearance step chose only a race that changed).
        s.H.Settings.DevMode = true;
        s.Session.Change(save => save with { Hero = save.Hero with { Race = "goblin", Hp = 3, Mana = 2 } });
        Assert.Null(HeroFlow.Finish(s.Parts, Data(s), "goblin", null));
        Assert.Equal(ActorRules.ChooseRace(before!.Hero with { Race = "goblin", Hp = 3, Mana = 2 }, Data(s).Context, "goblin", true).Sheet, s.Session.Current!.Hero);
        Assert.True(s.Session.Current!.Hero.Hp > 3);
        // With no character open nothing can be taken.
        using var none = new HeroScreensHarness();
        Assert.Equal(RaceRefusal.NotPlayable, HeroFlow.Refusal(none.Parts, Data(none), "human"));
    }
}

/// <summary>The flow's log lines (GM2 step 13's end-to-end flow reads them); alone, as every test reading the log.</summary>
[Collection("Game log")]
public class HeroFlowLogTests
{
    [Fact]
    public void TheFlowSaysWhatItDoes()
    {
        using var s = new HeroScreensHarness();
        s.NewHero();
        var log = GameTesting.CaptureLog(() =>
        {
            s.Run(HeroFlow.RacesCommand, "dwarf");
            s.H.Settings.DevMode = true;
            s.Run(HeroFlow.RacesCommand);
            s.H.Settings.DevMode = false;
            s.Click(s.Q("#races-choose"));
            s.Click(s.Q("#maker-random"));
            s.Click(s.Q("#maker-begin"));
        });
        Assert.Contains("Info [Races] the race carousel opened on dwarf (9 races)", log);
        Assert.Contains("Info [Races] the race carousel opened on human (28 races, Dev Mode)", log);
        Assert.Contains("Info [Races] human chosen: its look next", log);
        Assert.Contains("Info [Maker] the maker opened for the human (human-male-average, preset test-human-male-1)", log);
        Assert.Contains(log, line => line.StartsWith("Info [Maker] a random look: human-", StringComparison.Ordinal));
        Assert.Contains(log, line => line.StartsWith("Info [Hero] ser-alrik: the hero is a human, ", StringComparison.Ordinal));
    }

    [Fact]
    public void TheMakersRefusalsAreLoggedAndNothingShown()
    {
        using var s = new HeroScreensHarness();
        s.NewHero();
        s.Session.Change(save => save with { Hero = save.Hero with { Race = "dwarf" } });
        var log = GameTesting.CaptureLog(() =>
        {
            foreach (var race in new[] { "", "nowhere", "goblin", "elf", "human" })
                s.Run(HeroFlow.MakerCommand, race.Length == 0 ? null : race);
        });
        Assert.False(s.Maker.Showing);
        Assert.Equal(
        [
            "Warning [Maker] open-maker: no race given, nothing done",
            "Warning [Maker] open-maker: no race nowhere, nothing done",
            "Warning [Maker] open-maker: the goblin is no playable race, nothing done",
            "Warning [Maker] open-maker: No looks for the Elf yet, nothing done",
            "Warning [Maker] open-maker: human: A chosen race can only be changed in Dev Mode, nothing done",
        ], log.Where(line => line.Contains("[Maker]")));
    }

    [Fact]
    public void AnArrowKeyOffTheOptionsDoesNothingAndFailsNothing()
    {
        using var s = new HeroScreensHarness();
        s.NewHero();
        s.Run(HeroFlow.MakerCommand, HeroTestData.Human);
        var look = s.Maker.Look;
        var log = GameTesting.CaptureLog(() =>
        {
            foreach (var selector in new[] { "#maker-random", "#maker-begin", "#maker-back" })
            {
                s.Q(selector).Focus();
                Assert.True(s.Key("ArrowRight"));
            }
            s.Row("part:face").ElementChildren.First().Focus();
            Assert.True(s.Key("ArrowDown"));
        });
        Assert.Equal(look, s.Maker.Look);
        Assert.DoesNotContain(log, line => line.StartsWith("Error", StringComparison.Ordinal));
    }

    [Fact]
    public void AnEnemyOnlyHeroIsSaidSo()
    {
        using var s = new HeroScreensHarness();
        s.H.Settings.DevMode = true;
        s.NewHero();
        s.Run(HeroFlow.RacesCommand, "goblin");
        var log = GameTesting.CaptureLog(() => s.Click(s.Q("#races-choose")));
        Assert.Contains("Info [Hero] ser-alrik: the hero is a goblin with no look", log);
        Assert.Contains(SoundNames.RaceChosen, s.H.Sounds);
    }
}
