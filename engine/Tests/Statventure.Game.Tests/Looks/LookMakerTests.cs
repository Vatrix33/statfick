using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Random;
using Statventure.Game.Screens.Maker;
using Statventure.Records;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The character maker's logic (GM2 step 8; GM2-1, GM2-6, Q13–Q24, Q21): what a race's data offers, where the maker starts,
/// each choice and its refits, the random roll draw for draw, and every look it makes passing every check of
/// <c>look.hero</c> with the data (G4: a look the maker makes is one a character file may hold), on the screens' test data.
/// </summary>
public class LookMakerTests : IDisposable
{
    readonly HeroTestData _data = new(Path.Combine(TestPaths.Packs, "data.pak"));

    LookData Looks => LookData.Load(_data.Data);

    Race RaceOf(string id) => _data.Data.Load<Race>(RaceCatalogue.PathOf(id));

    LookMaker Maker(string race, LookHero? start = null) => new(race, RaceOf(race), Looks, start);

    /// <summary>Every problem the look's checks find with the data and the references resolved over it.</summary>
    List<string> Problems(LookHero look) =>
        [.. RecordChecks.Check(look, _data.Data, TestLooks.Lookup(_data.Data)).Select(p => p.ToString())];

    [Fact]
    public void TheMakerOffersWhatTheRacesDataHas()
    {
        var human = Maker(HeroTestData.Human);
        Assert.Equal(["male", "female"], human.Sexes);
        Assert.Equal(["slender", "average", "strong"], human.BuildsOf("male"));
        Assert.Equal(["test-human-male-1", "test-human-male-2", "test-human-male-3", "test-human-male-4", "test-human-female-1", "test-human-female-2",
            "test-human-female-3", "test-human-female-4"], human.Presets);
        // The categories in the maker's order (LookNames.Categories), each in the parts' names' order; a man's body offers a beard.
        Assert.Equal(["face", "brows", "hair", "beard", "marks", "head-piece"], human.Categories.Keys);
        Assert.Equal(["test-hair-bald", "test-hair-braid", "test-hair-crop", "test-hair-long", "test-hair-topknot"], human.Categories["hair"]);
        // The slots the race has swatches of, in the slots' order; their swatches in the race's.
        Assert.Equal(["skin", "hair", "eyes", "cloth"], human.Slots);
        Assert.Equal(["Test fair", "Test olive", "Test umber", "Test deep"], human.SwatchesOf("skin").Select(s => s.Name));
        // The dwarf: one body, a man's average one; no swatches, so no colours.
        var dwarf = Maker(HeroTestData.Dwarf);
        Assert.Equal(["male"], dwarf.Sexes);
        Assert.Equal([], dwarf.BuildsOf("female"));
        Assert.Equal(["average"], dwarf.BuildsOf("male"));
        Assert.Empty(dwarf.Slots);
        Assert.Empty(dwarf.Look!.Colours);
        // The slots in the slots' order whatever the race lists first.
        Assert.Equal(["skin", "crest"], Maker(HeroTestData.Lizardman).Slots);
    }

    [Fact]
    public void TheMakerStartsOnTheRacesFirstPresetElseThePlainLookAndHasNoneWithNoBody()
    {
        var human = Maker(HeroTestData.Human);
        Assert.Equal(HeroTestData.Presets["test-human-male-1"].Look with { Preset = "test-human-male-1" }, human.Look);
        // No presets: the first sex, its average build (the Half-Beast's man has a slender one first), each category's first
        // option, each slot's first swatch.
        var beast = Maker(HeroTestData.HalfBeast);
        Assert.Equal(new LookHero
        {
            Race = HeroTestData.HalfBeast, Sex = "male", Body = "half-beast-male-average",
            Parts = new Dictionary<string, string> { ["face"] = "test-face-beast", ["hair"] = "test-hair-braid", ["ears"] = "test-ears-pointed", ["tail"] = "test-tail-bushy" },
            Colours = new Dictionary<string, string> { ["skin"] = "Test tawny", ["hair"] = "Test russet" },
        }, beast.Look);
        // A race with no body (the elf in the test data): no look, nothing to choose.
        var elf = Maker("elf");
        Assert.Null(elf.Look);
        Assert.Empty(elf.Sexes);
        Assert.Empty(elf.Categories);
        Assert.Null(elf.Build);
        var none = new ScriptedRandom([0.5]);
        elf.Roll(none);
        Assert.Equal((1, null), (none.Remaining, elf.Look));
    }

    [Fact]
    public void TheMakerStartsOnTheHerosOwnLookOfItsRaceFittedToTheData()
    {
        var own = HeroTestData.Presets["test-human-female-3"].Look with { Preset = "test-human-female-3" };
        Assert.Equal(own, Maker(HeroTestData.Human, own).Look);
        // A look of another race is passed over: the race's first preset.
        Assert.Equal("test-human-male-1", Maker(HeroTestData.Human, Maker(HeroTestData.HalfBeast).Look).Look!.Preset);
        // A look naming what the data no longer has is fitted: the part and the colour taken from the first.
        var stale = own with
        {
            // A slot's save opens unchecked (step 6): its sex may disagree with its body; the body's is taken.
            Sex = "male",
            Parts = new Dictionary<string, string>(own.Parts) { ["hair"] = "hair-gone", ["beard"] = "test-beard-full" },
            Colours = new Dictionary<string, string>(own.Colours) { ["skin"] = "Gone" },
        };
        var fitted = Maker(HeroTestData.Human, stale).Look!;
        Assert.Equal("test-hair-braid", fitted.Parts["hair"]);
        Assert.False(fitted.Parts.ContainsKey("beard"));
        Assert.Equal("Test fair", fitted.Colours["skin"]);
        Assert.Equal("female", fitted.Sex);
        Assert.Empty(Problems(fitted));
    }

    [Fact]
    public void APresetSetsEveryChoiceAndIsKeptAsWhereTheLookStarted()
    {
        var maker = Maker(HeroTestData.Human);
        Assert.False(maker.UsePreset("test-human-male-1"));
        Assert.True(maker.UsePreset("test-human-female-2"));
        Assert.Equal(HeroTestData.Presets["test-human-female-2"].Look with { Preset = "test-human-female-2" }, maker.Look);
        Assert.Equal("slender", maker.Build);
        Assert.True(maker.SetPart("hair", "test-hair-long"));
        Assert.Equal("test-human-female-2", maker.Look!.Preset);
        Assert.Throws<ArgumentException>(() => maker.UsePreset("test-nothing"));
    }

    [Fact]
    public void AChangedSexKeepsTheBuildAndRefitsTheParts()
    {
        var maker = Maker(HeroTestData.Human);
        maker.UsePreset("test-human-male-3");
        Assert.True(maker.SetSex("female"));
        var look = maker.Look!;
        Assert.Equal(("female", "human-female-strong"), (look.Sex, look.Body));
        // The bald head and the beard are men's: the hair takes the category's first option, the beard goes; the rest stay.
        Assert.Equal("test-hair-braid", look.Parts["hair"]);
        Assert.False(look.Parts.ContainsKey("beard"));
        Assert.Equal(("test-face-3", "test-brows-arched"), (look.Parts["face"], look.Parts["brows"]));
        Assert.Equal(HeroTestData.Presets["test-human-male-3"].Look.Colours, look.Colours);
        Assert.Equal("test-human-male-3", look.Preset);
        Assert.False(maker.SetSex("female"));
        // Back to a man: the beard is a category again, its first option (by name: "forked").
        Assert.True(maker.SetSex("male"));
        Assert.Equal("test-beard-forked", maker.Look!.Parts["beard"]);
        Assert.Throws<ArgumentException>(() => Maker(HeroTestData.Dwarf).SetSex("female"));
    }

    [Fact]
    public void AChangedSexTakesTheFirstBuildOfThatSexWhenItLacksTheLooksBuild()
    {
        var maker = Maker(HeroTestData.Dwarf);
        Assert.Equal("dwarf-male-average", maker.Look!.Body);
        Assert.False(maker.SetSex("male"));
        Assert.False(maker.SetBuild("average"));
        Assert.Throws<ArgumentException>(() => maker.SetBuild("strong"));
    }

    [Fact]
    public void AChangedBuildKeepsTheSexAndTheParts()
    {
        var maker = Maker(HeroTestData.Human);
        Assert.True(maker.SetBuild("strong"));
        Assert.Equal("human-male-strong", maker.Look!.Body);
        Assert.Equal(HeroTestData.Presets["test-human-male-1"].Look.Parts, maker.Look.Parts);
        Assert.False(maker.SetBuild("strong"));
    }

    [Fact]
    public void APartAndAColourAreChosenAmongWhatIsOfferedOnly()
    {
        var maker = Maker(HeroTestData.Human);
        Assert.True(maker.SetPart("hair", "test-hair-long"));
        Assert.False(maker.SetPart("hair", "test-hair-long"));
        Assert.Equal("test-hair-long", maker.Look!.Parts["hair"]);
        // The curls are women's; a dwarf's beard isn't the human's; a category the body doesn't offer.
        Assert.Throws<ArgumentException>(() => maker.SetPart("hair", "test-hair-curls"));
        Assert.Throws<ArgumentException>(() => maker.SetPart("crest", "test-crest-spines"));
        Assert.True(maker.SetColour("skin", "Test deep"));
        Assert.False(maker.SetColour("skin", "Test deep"));
        Assert.Throws<ArgumentException>(() => maker.SetColour("skin", "Test tawny"));
        Assert.Throws<ArgumentException>(() => maker.SetColour("crest", "Test ochre"));
        Assert.Empty(Problems(maker.Look!));
    }

    [Fact]
    public void TheRandomRollDrawsEveryChoiceUniformlyInItsOrderOneDrawAList()
    {
        // Sex (2), build (3), face (4), brows (2), hair (5: a woman has the curls, no bald), marks (3), head piece (3),
        // then skin (4), hair (5), eyes (3), cloth (4): the draws pick the second sex, the last build, and so on.
        var random = new ScriptedRandom([0.5, 0.99, 0.25, 0.0, 0.7, 0.4, 0.99, 0.75, 0.2, 0.67, 0.0]);
        var maker = Maker(HeroTestData.Human);
        maker.Roll(random);
        Assert.Equal(0, random.Remaining);
        Assert.Equal(new LookHero
        {
            Race = HeroTestData.Human, Sex = "female", Body = "human-female-strong",
            Parts = new Dictionary<string, string>
            {
                ["face"] = "test-face-2", ["brows"] = "test-brows-arched", ["hair"] = "test-hair-long", ["marks"] = "test-marks-none", ["head-piece"] = "test-head-none",
            },
            Colours = new Dictionary<string, string> { ["skin"] = "Test deep", ["hair"] = "Test raven", ["eyes"] = "Test grey", ["cloth"] = "Test madder" },
            Preset = null,
        }, maker.Look);
    }

    [Fact]
    public void EveryLookTheMakerMakesPassesItsChecks()
    {
        var random = new GameRandom(81);
        foreach (var race in new[] { HeroTestData.Human, HeroTestData.Dwarf, HeroTestData.HalfBeast, HeroTestData.Lizardman })
        {
            var maker = Maker(race);
            Assert.Empty(Problems(maker.Look!));
            foreach (var preset in maker.Presets)
            {
                maker.UsePreset(preset);
                Assert.Empty(Problems(maker.Look!));
                foreach (var sex in maker.Sexes)
                {
                    maker.SetSex(sex);
                    foreach (var build in maker.BuildsOf(sex))
                    {
                        maker.SetBuild(build);
                        Assert.Empty(Problems(maker.Look!));
                    }
                }
            }
            var looks = new HashSet<string>();
            for (var i = 0; i < 200; i++)
            {
                maker.Roll(random);
                Assert.Empty(Problems(maker.Look!));
                Assert.Null(maker.Look!.Preset);
                looks.Add(RecordSerializer.ToJson(maker.Look));
            }
            // Every combination comes up: the dwarf has 2 (the beards), the lizardman 8 (sex, crest, skin); the others many.
            Assert.True(race switch { HeroTestData.Dwarf => looks.Count == 2, HeroTestData.Lizardman => looks.Count == 8, _ => looks.Count > 20 }, $"{race}: {looks.Count} looks rolled");
        }
    }

    public void Dispose() => _data.Dispose();
}
