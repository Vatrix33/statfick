using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Random;
using Statventure.Game.Screens.Maker;
using Statventure.Game.Screens.Races;
using Statventure.Game.Sounds;
using Statventure.Records;

namespace Statventure.Game.Tests;

/// <summary>
/// The character maker (GM2 step 8; GM2-1, GM2-6, Q13–Q24, Q21, the user's answers of 2026-11-16): the race's presets as a
/// list, the choices by row (one open at a time), a choice by click or by the arrow keys, the random button, "Begin as"
/// saving the race and the look together and asking for the sheet, "← All races" and ×; who may open it and for which
/// race; the sounds (GM2-P17).
/// </summary>
public class MakerScreenTests
{
    static HeroScreensHarness Opened(string race = HeroTestData.Human, Action<HeroScreensHarness>? before = null, IGameRandom? random = null)
    {
        var s = new HeroScreensHarness(random: random);
        s.NewHero();
        before?.Invoke(s);
        s.Run(HeroFlow.MakerCommand, race);
        return s;
    }

    static List<string> Presets(HeroScreensHarness s) => [.. s.Document.QuerySelectorAll("#maker-presets .maker-preset").Select(p => p.TextContent)];

    [Fact]
    public void ItOpensOnTheRacesFirstPresetWithEveryChoiceShown()
    {
        using var s = Opened();
        Assert.True(s.Maker.Showing);
        Assert.Equal("human", s.Maker.Race);
        Assert.Equal(HeroTestData.Presets["test-human-male-1"].Look with { Preset = "test-human-male-1" }, s.Maker.Look);
        Assert.Equal(("HUMAN · APPEARANCE", "Choose your look"), (s.Q("#maker-eyebrow").TextContent, s.Q("#maker-title").TextContent));
        Assert.Equal(HeroTexts.MakerIntro, s.Q("#maker-intro").TextContent);
        // The presets: the men's then the women's, by their labels, the first marked.
        Assert.Equal(["MALE", "FEMALE"], s.Document.QuerySelectorAll("#maker-presets .maker-preset-sex").Select(e => e.TextContent));
        Assert.Equal(["Test Wayfarer", "Test Sellsword", "Test Smith", "Test Courtier", "Test Ranger", "Test Duelist", "Test Healer", "Test Scholar"], Presets(s));
        Assert.Equal("test-human-male-1", s.Checked(s.Q("#maker-presets")));
        Assert.Same(s.Q("#maker-presets .maker-preset"), s.Document.FocusedElement);
        // The rows: the sex and the build in a line, then the categories and the colours, each with its choice.
        Assert.Equal(["sex", "build", "part:face", "part:brows", "part:hair", "part:beard", "part:marks", "part:head-piece", "colour:skin", "colour:hair",
            "colour:eyes", "colour:cloth"], s.Document.QuerySelectorAll("#maker-choices .maker-row").Select(r => r.GetAttribute("data-choice")));
        Assert.Equal(["Male", "Female"], s.Row("sex").Descendants().Where(e => e.GetAttribute("role") == "radio").Select(e => e.TextContent));
        Assert.Equal(("male", "average"), (s.Checked(s.Row("sex")), s.Checked(s.Row("build"))));
        Assert.Equal([("Face", "Test face 1"), ("Hair", "Test crop"), ("Beard", "Test clean-shaven"), ("Head piece", "Test none"), ("Skin", "Test fair"), ("Cloth", "Test moss")],
            new[] { "part:face", "part:hair", "part:beard", "part:head-piece", "colour:skin", "colour:cloth" }
                .Select(k => (s.Row(k).Q(".maker-row-label").TextContent, s.Row(k).Q(".maker-row-value").TextContent)));
        Assert.Equal("--swatch: #e8c3a0", s.Row("colour:skin").Q(".maker-swatch").GetAttribute("style"));
        Assert.Null(s.Maker.OpenRow);
        Assert.Equal(("Human · Male · Average · Test Wayfarer", "Begin as Human"), (s.Q("#maker-summary").TextContent, s.Q("#maker-begin").TextContent));
        Assert.Equal(HeroFlow.LeaveToStart, s.Q("#maker-leave").GetAttribute("aria-label"));
        Assert.Equal("--race-accent: #c7683f", s.Q("#maker-screen").GetAttribute("style"));
        Assert.Contains(MakerScreen.Stylesheet, s.H.Ui.Sheets);
        Assert.Null(s.Session.Current!.Hero.Race);
    }

    [Fact]
    public void APresetPickedSetsEveryChoiceAndSoundsOnceWhenItChangesTheLook()
    {
        using var s = Opened();
        s.H.Sounds.Clear();
        s.Click(s.Q("#maker-presets .maker-preset[data-preset=\"test-human-female-3\"]"));
        Assert.Equal(HeroTestData.Presets["test-human-female-3"].Look with { Preset = "test-human-female-3" }, s.Maker.Look);
        Assert.Equal([SoundNames.MakerChoice], s.H.Sounds);
        Assert.Equal(("female", "average", "test-human-female-3"), (s.Checked(s.Row("sex")), s.Checked(s.Row("build")), s.Checked(s.Q("#maker-presets"))));
        Assert.Equal("Human · Female · Average · Test Healer", s.Q("#maker-summary").TextContent);
        // The same preset again: nothing changes, GM1's press only.
        s.H.Sounds.Clear();
        s.Click(s.Q("#maker-presets .maker-preset[data-preset=\"test-human-female-3\"]"));
        Assert.Equal([SoundNames.Press], s.H.Sounds);
    }

    [Fact]
    public void ARowOpensToItsOptionsOneAtATimeAndAnOptionIsChosenByClick()
    {
        using var s = Opened();
        Assert.Empty(s.Document.QuerySelectorAll("#maker-choices .maker-row:not(.maker-row-inline) .maker-options"));
        s.Click(s.Row("part:hair").ElementChildren.First());
        Assert.Equal("part:hair", s.Maker.OpenRow);
        Assert.Equal("true", s.Row("part:hair").ElementChildren.First().GetAttribute("aria-expanded"));
        Assert.True(s.Row("part:hair").HasClass("is-open"));
        Assert.Equal(["Test bald", "Test braid", "Test crop", "Test long", "Test topknot"],
            s.Row("part:hair").Descendants().Where(e => e.HasClass("maker-option")).Select(e => e.TextContent));
        Assert.Equal("test-hair-crop", s.Checked(s.Row("part:hair")));
        s.H.Sounds.Clear();
        s.Click(s.Row("part:hair").Descendants().First(e => e.GetAttribute("data-value") == "test-hair-long"));
        Assert.Equal("test-hair-long", s.Maker.Look!.Parts["hair"]);
        Assert.Equal("test-hair-long", s.Checked(s.Row("part:hair")));
        Assert.Equal([SoundNames.MakerChoice], s.H.Sounds);
        // Another row opened closes the first; its own head closes it.
        s.Click(s.Row("colour:skin").ElementChildren.First());
        Assert.Equal("colour:skin", s.Maker.OpenRow);
        Assert.Equal("false", s.Row("part:hair").ElementChildren.First().GetAttribute("aria-expanded"));
        Assert.Single(s.Document.QuerySelectorAll("#maker-choices .maker-row:not(.maker-row-inline) .maker-options"));
        Assert.Equal(["Test fair", "Test olive", "Test umber", "Test deep"], s.Row("colour:skin").Descendants().Where(e => e.HasClass("maker-colour")).Select(e => e.GetAttribute("title")));
        s.Click(s.Row("colour:skin").Descendants().First(e => e.GetAttribute("data-value") == "Test deep"));
        Assert.Equal("Test deep", s.Maker.Look!.Colours["skin"]);
        s.Click(s.Row("colour:skin").ElementChildren.First());
        Assert.Null(s.Maker.OpenRow);
        // The preset stays the one the look started from (the maker's display).
        Assert.Equal("test-human-male-1", s.Maker.Look!.Preset);
    }

    [Fact]
    public void TheSexAndTheBuildChangeTheBodyAndRefitTheRows()
    {
        using var s = Opened();
        s.H.Sounds.Clear();
        s.Click(s.Row("sex").Descendants().First(e => e.GetAttribute("data-value") == "female"));
        Assert.Equal(("female", "human-female-average"), (s.Maker.Look!.Sex, s.Maker.Look.Body));
        Assert.Equal([SoundNames.MakerChoice], s.H.Sounds);
        Assert.Null(s.Find("#maker-choices .maker-row[data-choice=\"part:beard\"]"));
        s.Click(s.Row("build").Descendants().First(e => e.GetAttribute("data-value") == "slender"));
        Assert.Equal("human-female-slender", s.Maker.Look!.Body);
        Assert.Equal("Human · Female · Slender · Test Wayfarer", s.Q("#maker-summary").TextContent);
        // The same build again: nothing.
        s.H.Sounds.Clear();
        s.Click(s.Row("build").Descendants().First(e => e.GetAttribute("data-value") == "slender"));
        Assert.Equal([SoundNames.Press], s.H.Sounds);
    }

    [Fact]
    public void TheArrowKeysChooseAmongAGroupsOptionsRoundTheFocusFollowing()
    {
        using var s = Opened();
        var build = s.Row("build").Descendants().First(e => e.GetAttribute("data-value") == "average");
        build.Focus();
        s.H.Sounds.Clear();
        Assert.False(s.Key("ArrowRight"));
        Assert.Equal("human-male-strong", s.Maker.Look!.Body);
        Assert.Equal("strong", s.Document.FocusedElement!.GetAttribute("data-value"));
        Assert.Equal([SoundNames.MakerChoice], s.H.Sounds);
        s.Key("ArrowDown");
        Assert.Equal("human-male-slender", s.Maker.Look!.Body);
        s.Key("ArrowUp");
        s.Key("ArrowLeft");
        Assert.Equal("human-male-average", s.Maker.Look!.Body);
        Assert.Equal("average", s.Document.FocusedElement!.GetAttribute("data-value"));
        // The presets are a group too (the old step's looks): the arrows pick the next preset.
        s.Q("#maker-presets .maker-preset").Focus();
        s.Key("ArrowDown");
        Assert.Equal("test-human-male-2", s.Maker.Look!.Preset);
        Assert.Equal("test-human-male-2", s.Document.FocusedElement!.GetAttribute("data-preset"));
        // Not on a row's head, nor with a modifier.
        s.Row("part:face").ElementChildren.First().Focus();
        var look = s.Maker.Look;
        Assert.True(s.Key("ArrowRight"));
        foreach (var modifier in new[] { Platform.KeyModifiers.Shift, Platform.KeyModifiers.Control, Platform.KeyModifiers.Alt, Platform.KeyModifiers.Super })
        {
            s.Q("#maker-presets .maker-preset").Focus();
            Assert.True(s.H.Key("ArrowDown", "ArrowDown", modifier));
        }
        Assert.Equal(look, s.Maker.Look);
        // A key the page took already (from the slender build, the next would change the strong one).
        Assert.Equal("strong", s.Checked(s.Row("build")));
        s.Q("#maker-choices").AddEventListener<UI.Dom.UiKeyboardEvent>("keydown", e => e.PreventDefault());
        s.Row("build").Descendants().First(e => e.GetAttribute("data-value") == "slender").Focus();
        s.Key("ArrowRight");
        Assert.Equal(look, s.Maker.Look);
    }

    [Fact]
    public void AnOpenRowsHeadKeepsTheFocusWhenTheRowsAreMadeAgain()
    {
        using var s = Opened();
        var head = s.Row("part:hair").ElementChildren.First();
        head.Focus();
        s.Key("Enter");
        Assert.Equal("part:hair", s.Maker.OpenRow);
        Assert.Equal("head:part:hair", s.Document.FocusedElement!.GetAttribute("data-key"));
        var option = s.Row("part:hair").Descendants().First(e => e.GetAttribute("data-value") == "test-hair-braid");
        option.Focus();
        s.Key("Enter");
        Assert.Equal("test-hair-braid", s.Maker.Look!.Parts["hair"]);
        Assert.Equal("part:hair:test-hair-braid", s.Document.FocusedElement!.GetAttribute("data-key"));
    }

    [Fact]
    public void TheRandomButtonRollsEveryChoiceAndSoundsOnce()
    {
        var draws = new ScriptedRandom([0.5, 0.99, 0.25, 0.0, 0.7, 0.4, 0.99, 0.75, 0.2, 0.67, 0.0]);
        using var s = Opened(random: draws);
        s.H.Sounds.Clear();
        s.Click(s.Q("#maker-random"));
        Assert.Equal(0, draws.Remaining);
        Assert.Equal(("female", "human-female-strong", "test-hair-long", "Test deep", null),
            (s.Maker.Look!.Sex, s.Maker.Look.Body, s.Maker.Look.Parts["hair"], s.Maker.Look.Colours["skin"], s.Maker.Look.Preset));
        Assert.Equal([SoundNames.MakerRandom], s.H.Sounds);
        Assert.Null(s.Checked(s.Q("#maker-presets")));
        Assert.Equal("Human · Female · Strong", s.Q("#maker-summary").TextContent);
    }

    [Fact]
    public void BeginSavesTheRaceAndTheLookTogetherAndAsksForTheSheet()
    {
        using var s = Opened();
        s.Click(s.Q("#maker-presets .maker-preset[data-preset=\"test-human-female-1\"]"));
        s.Click(s.Row("part:marks").ElementChildren.First());
        s.Click(s.Row("part:marks").Descendants().First(e => e.GetAttribute("data-value") == "test-marks-scar"));
        var look = s.Maker.Look!;
        var before = s.Session.Current!.Hero;
        s.H.Sounds.Clear();
        s.Click(s.Q("#maker-begin"));
        var save = s.Session.Current!;
        Assert.Equal(look, save.Look);
        Assert.Equal("human", save.Hero.Race);
        Assert.Equal(ActorRules.ChooseRace(before, SheetContext.Plain.WithRaces(RaceCatalogue.Load(s.Data)), "human", false).Sheet, save.Hero);
        Assert.Equal([SoundNames.HeroConfirmed], s.H.Sounds);
        Assert.Equal([HeroFlow.SheetCommand], s.Ran);
        Assert.False(s.Maker.Showing);
        Assert.False(s.Races.Showing);
        // What was saved passes every check with the data (a character file of it loads), and the slot's file holds it.
        Assert.Empty(RecordChecks.Check(save, s.Data, TestLooks.Lookup(s.Data)));
        s.H.Storage.Flush();
        Assert.Equal(look, s.H.Storage.Saves.Load<Data.Saves.SaveGame>(s.Session.CurrentSlot!).Value.Look);
        // The hero is finished: the flow can't be opened again outside Dev Mode (GM2-7).
        Assert.False(s.H.Ui.Commands.IsEnabled(HeroFlow.RacesCommand));
        Assert.False(s.H.Ui.Commands.IsEnabled(HeroFlow.MakerCommand));
    }

    [Fact]
    public void InDevModeTheHerosOwnLookComesBackAndUseThisLookKeepsHpAndMana()
    {
        using var s = Opened();
        s.Click(s.Q("#maker-presets .maker-preset[data-preset=\"test-human-male-2\"]"));
        s.Click(s.Q("#maker-begin"));
        s.Session.Change(save => save with { Hero = save.Hero with { Hp = 5 } });
        s.H.Settings.DevMode = true;
        s.Run(HeroFlow.MakerCommand, HeroTestData.Human);
        Assert.Equal(s.Session.Current!.Look, s.Maker.Look);
        Assert.Equal(("Use this look", HeroFlow.LeaveToSheet), (s.Q("#maker-begin").TextContent, s.Q("#maker-leave").GetAttribute("title")));
        // × gives the change up: back to the sheet, nothing changed.
        var before = s.Session.Current;
        s.Ran.Clear();
        s.Click(s.Q("#maker-leave"));
        Assert.Equal([HeroFlow.SheetCommand], s.Ran);
        Assert.Same(before, s.Session.Current);
        s.Run(HeroFlow.MakerCommand, HeroTestData.Human);
        s.Click(s.Q("#maker-presets .maker-preset[data-preset=\"test-human-male-4\"]"));
        s.Click(s.Q("#maker-begin"));
        Assert.Equal("test-human-male-4", s.Session.Current!.Look!.Preset);
        Assert.Equal(5, s.Session.Current.Hero.Hp);
    }

    [Fact]
    public void BeginIsRefusedInTheOldWordsWhenDevModeWentOffMeanwhile()
    {
        // A hero of the dwarf changing to the human in Dev Mode, Dev Mode turned off while the maker shows.
        using var s = Opened(before: h =>
        {
            h.H.Settings.DevMode = true;
            h.Session.Change(save => save with { Hero = save.Hero with { Race = HeroTestData.Dwarf } });
        });
        Assert.True(s.Maker.Showing);
        s.H.Settings.DevMode = false;
        s.H.Sounds.Clear();
        s.Click(s.Q("#maker-begin"));
        Assert.Equal("A chosen race can only be changed in Dev Mode", s.Q("#maker-status").TextContent);
        Assert.True(s.Q("#maker-status").HasClass("is-error"));
        Assert.Equal([SoundNames.Refused], s.H.Sounds);
        Assert.Equal((HeroTestData.Dwarf, null), (s.Session.Current!.Hero.Race, s.Session.Current.Look));
        Assert.True(s.Maker.Showing);
        Assert.Empty(s.Ran);
        // Opened again, its status line is empty.
        s.H.Settings.DevMode = true;
        s.Click(s.Q("#maker-back"));
        s.Click(s.Q("#races-choose"));
        Assert.True(s.Maker.Showing);
        Assert.Equal("", s.Q("#maker-status").TextContent);
        Assert.False(s.Q("#maker-status").HasClass("is-error"));
    }

    [Fact]
    public void AllRacesGoesBackToTheCarouselOnThisRaceAndTheCrossLeaves()
    {
        using var s = Opened(HeroTestData.HalfBeast);
        s.Click(s.Row("part:ears").ElementChildren.First());
        s.Click(s.Q("#maker-back"));
        Assert.False(s.Maker.Showing);
        Assert.True(s.Races.Showing);
        Assert.Equal(HeroTestData.HalfBeast, s.Races.Shown);
        s.Click(s.Q("#races-choose"));
        Assert.True(s.Maker.Showing);
        // Opened again: no row open.
        Assert.Null(s.Maker.OpenRow);
        s.Click(s.Q("#maker-leave"));
        Assert.False(s.Maker.Showing);
        Assert.Equal([StatFickGame.StartCommand], s.Ran);
        Assert.Null(s.Session.Current!.Hero.Race);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("nowhere", false)]
    [InlineData("goblin", true)]
    [InlineData("elf", false)]
    public void ItOpensOnlyForAPlayableRaceWithLooksTheHeroCanTake(string? race, bool devMode)
    {
        using var s = new HeroScreensHarness();
        s.H.Settings.DevMode = devMode;
        s.NewHero();
        s.Run(HeroFlow.MakerCommand, race);
        Assert.False(s.Maker.Showing);
        // Another race than the hero's own outside Dev Mode: refused as selectRace refuses it.
        s.Session.Change(save => save with { Hero = save.Hero with { Race = HeroTestData.Dwarf } });
        s.H.Settings.DevMode = false;
        s.Run(HeroFlow.MakerCommand, HeroTestData.Human);
        Assert.False(s.Maker.Showing);
        s.Run(HeroFlow.MakerCommand, HeroTestData.Dwarf);
        Assert.True(s.Maker.Showing);
        // Its own race with no look yet: the hero is made, not changed.
        Assert.Equal("Begin as Dwarf", s.Q("#maker-begin").TextContent);
    }

    [Fact]
    public void ARaceWithoutPresetsStartsOnAPlainLook()
    {
        using var s = Opened(HeroTestData.HalfBeast);
        Assert.Equal("No presets", s.Q("#maker-presets .maker-none").TextContent);
        Assert.Empty(s.Document.QuerySelectorAll("#maker-presets .maker-preset-group"));
        // (Its one skin swatch is no choice: no row for it, the user's Q73.)
        Assert.Equal(["sex", "build", "part:face", "part:hair", "part:ears", "part:tail", "colour:hair"],
            s.Document.QuerySelectorAll("#maker-choices .maker-row").Select(r => r.GetAttribute("data-choice")));
        Assert.Same(s.Q("#maker-begin"), s.Document.FocusedElement);
        Assert.Equal("Half-Beast · Male · Average", s.Q("#maker-summary").TextContent);
    }

    [Fact]
    public void ARaceWithoutSwatchesHasNoColours()
    {
        // The dwarf of the test data: one body, a beard to choose, no swatches.
        using var s = Opened(HeroTestData.Dwarf);
        Assert.Equal(["sex", "build", "part:beard"], s.Document.QuerySelectorAll("#maker-choices .maker-row").Select(r => r.GetAttribute("data-choice")));
        Assert.Null(s.Find("#maker-choices .maker-colours-title"));
        Assert.Equal(["Male"], s.Row("sex").Descendants().Where(e => e.GetAttribute("role") == "radio").Select(e => e.TextContent));
        Assert.Equal(["Average"], s.Row("build").Descendants().Where(e => e.GetAttribute("role") == "radio").Select(e => e.TextContent));
        using var human = Opened();
        Assert.Equal("COLOURS", human.Q("#maker-choices .maker-colours-title").TextContent);
        // A race whose swatches neither its body nor any part it offers uses (the dwarf given two crest colours, GM2-P29):
        // the look holds the colour, the maker offers none and shows no colours' title.
        using var unused = new HeroScreensHarness(races: new Dictionary<string, IReadOnlyList<RaceSwatch>>
        {
            [HeroTestData.Dwarf] = [new RaceSwatch { Slot = "crest", Name = "Test ochre", Colour = "#c08a2a" }, new RaceSwatch { Slot = "crest", Name = "Test moss", Colour = "#556b2f" }],
        });
        unused.NewHero();
        unused.Run(HeroFlow.MakerCommand, HeroTestData.Dwarf);
        Assert.Equal("Test ochre", unused.Maker.Look!.Colours["crest"]);
        Assert.Equal(["sex", "build", "part:beard"], unused.Document.QuerySelectorAll("#maker-choices .maker-row").Select(r => r.GetAttribute("data-choice")));
        Assert.Null(unused.Find("#maker-choices .maker-colours-title"));
    }

    /// <summary>
    /// The colours a look shows (GM2-P29, on the shipped human, GM2 step 9a): a row for each slot its body or a chosen part
    /// uses, made again on each change: the man has no lips, the Duelist's high ponytail brings its tie, the Scholar's
    /// freckles their colour, gone with the mark, the colour kept.
    /// </summary>
    [Fact]
    public void AColourRowShowsOnlyWhereTheBodyOrAChosenPartUsesItsSlot()
    {
        using var s = new HeroScreensHarness(shipped: true);
        s.NewHero();
        s.Run(HeroFlow.MakerCommand, "human");
        List<string> Colours() => [.. s.Document.QuerySelectorAll("#maker-choices .maker-row").Select(r => r.GetAttribute("data-choice")!).Where(c => c.StartsWith("colour:"))];
        Assert.Equal("human-male-1", s.Maker.Look!.Preset);
        Assert.Equal(["colour:skin", "colour:hair", "colour:eyes", "colour:cloth", "colour:cloth.trim", "colour:trousers", "colour:leather", "colour:buckle"], Colours());
        s.Click(s.Q("#maker-presets .maker-preset[data-preset=\"human-female-2\"]"));
        Assert.Equal(["colour:skin", "colour:hair", "colour:hair.tie", "colour:eyes", "colour:lips", "colour:cloth", "colour:cloth.trim", "colour:trousers",
            "colour:leather", "colour:buckle"], Colours());
        Assert.Equal(("Hair ties", "Red"), (s.Row("colour:hair.tie").Q(".maker-row-label").TextContent, s.Row("colour:hair.tie").Q(".maker-row-value").TextContent));
        s.Click(s.Q("#maker-presets .maker-preset[data-preset=\"human-female-4\"]"));
        Assert.Contains("colour:marking", Colours());
        s.Click(s.Row("part:marks").Q(".maker-row-head"));
        s.Click(s.Row("part:marks").Descendants().First(e => e.GetAttribute("data-value") == "marks-none"));
        Assert.DoesNotContain("colour:marking", Colours());
        Assert.Equal("Brown", s.Maker.Look!.Colours["marking"]);
        Assert.Equal(13, s.Maker.Look.Colours.Count);
    }

    /// <summary>
    /// A colour with one swatch is no choice (the user's Q73, 2026-11-21): its row hidden, the look holding the swatch. The
    /// shipped human's brows and eye whites; the test data's Half-Beast's one skin, whose row comes back when the race has
    /// two.
    /// </summary>
    [Fact]
    public void AColourWithOneSwatchShowsNoRow()
    {
        using var shipped = new HeroScreensHarness(shipped: true);
        shipped.NewHero();
        shipped.Run(HeroFlow.MakerCommand, "human");
        Assert.Null(shipped.Find("#maker-choices .maker-row[data-choice=\"colour:brows\"]"));
        Assert.Null(shipped.Find("#maker-choices .maker-row[data-choice=\"colour:eye.white\"]"));
        Assert.Equal(("Dark", "Ivory"), (shipped.Maker.Look!.Colours["brows"], shipped.Maker.Look.Colours["eye.white"]));
        using var one = Opened(HeroTestData.HalfBeast);
        Assert.Null(one.Find("#maker-choices .maker-row[data-choice=\"colour:skin\"]"));
        Assert.Equal("Test tawny", one.Maker.Look!.Colours["skin"]);
        using var two = new HeroScreensHarness(races: new Dictionary<string, IReadOnlyList<RaceSwatch>>
        {
            [HeroTestData.HalfBeast] =
            [
                new RaceSwatch { Slot = "skin", Name = "Test tawny", Colour = "#c08a5a" }, new RaceSwatch { Slot = "skin", Name = "Test ash", Colour = "#9a978c" },
                new RaceSwatch { Slot = "hair", Name = "Test russet", Colour = "#9a4a22" }, new RaceSwatch { Slot = "hair", Name = "Test grey", Colour = "#8a8a84" },
            ],
        });
        two.NewHero();
        two.Run(HeroFlow.MakerCommand, HeroTestData.HalfBeast);
        Assert.Equal(["colour:skin", "colour:hair"], two.Document.QuerySelectorAll("#maker-choices .maker-row").Select(r => r.GetAttribute("data-choice")!).Where(c => c.StartsWith("colour:")));
    }

    [Fact]
    public void ItHidesWhenTheCharacterIsLeft()
    {
        using var s = Opened();
        s.Session.Leave();
        Assert.False(s.Maker.Showing);
        Assert.Null(s.Maker.Look);
    }
}

static class ElementQueries
{
    /// <summary>The first element under <paramref name="element"/> <paramref name="selector"/> matches.</summary>
    public static UI.Dom.UiElement Q(this UI.Dom.UiElement element, string selector) =>
        element.Descendants().FirstOrDefault(e => e.Matches(selector)) ?? throw new InvalidOperationException($"nothing under {element} matches {selector}");
}
