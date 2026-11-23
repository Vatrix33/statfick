using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Random;
using Statventure.Game.Screens.Maker;
using Statventure.Packs;
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

    /// <summary>The game's own data (its packs as the game mounts them) as the maker's screen reads it.</summary>
    static (VirtualFileSystem Data, Screens.Races.HeroData Hero) GameData(ScratchFolder folder)
    {
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        var data = new VirtualFileSystem(Core.Application.DataMounts(new Core.ApplicationOptions(), folder["game"], _ => null));
        return (data, Screens.Races.HeroData.Of(data));
    }

    /// <summary>
    /// The colours the maker offers (GM2-P29, on the game's human): the slots the body's model or a chosen part uses, the
    /// look still holding a colour of every slot the race has swatches of (kept while hidden): a man shows no lips, a tie
    /// comes with a tied style, a mark's colour with a mark.
    /// </summary>
    [Fact]
    public void TheMakerOffersAColourOnlyWhereTheBodyOrAChosenPartUsesIt()
    {
        using var folder = new ScratchFolder();
        var (data, hero) = GameData(folder);
        using var _ = data;
        Assert.Equal(["skin", "cloth", "cloth.trim", "trousers", "leather", "buckle"], hero.SlotsOfBody("human-male-average").Order(StringComparer.Ordinal).ToList() is var body
            ? LookNames.Slots.Where(body.Contains) : []);
        Assert.Empty(hero.SlotsOfBody("nobody"));
        var maker = new LookMaker("human", hero.Race("human")!, hero.Looks, null, hero.SlotsOfBody);
        Assert.Equal(13, maker.Slots.Count);
        // The Wayfarer: a man with the crop and stubble, no mark, no head piece.
        Assert.Equal("human-male-1", maker.Look!.Preset);
        // (The brows and the eye whites, one swatch each, are no choice: never offered, the user's Q73.)
        Assert.Equal(["skin", "hair", "eyes", "cloth", "cloth.trim", "trousers", "leather", "buckle"], maker.OfferedSlots);
        Assert.Equal(maker.Slots.Order(StringComparer.Ordinal), maker.Look.Colours.Keys.Order(StringComparer.Ordinal));
        // The Duelist: a woman's lips, the high ponytail's tie.
        maker.UsePreset("human-female-2");
        Assert.Equal(["skin", "hair", "hair.tie", "eyes", "lips", "cloth", "cloth.trim", "trousers", "leather", "buckle"], maker.OfferedSlots);
        // The Scholar's freckles: the marking; no mark, no marking, its colour kept.
        maker.UsePreset("human-female-4");
        Assert.Contains("marking", maker.OfferedSlots);
        maker.SetPart("marks", "marks-none");
        Assert.DoesNotContain("marking", maker.OfferedSlots);
        Assert.Equal("Brown", maker.Look.Colours["marking"]);
        // A head piece of the outfit's slots adds none (they are the body's); the hair "None" takes the hair colour away.
        maker.SetPart("head-piece", "head-piece-circlet");
        maker.SetPart("hair", "hair-none");
        Assert.Equal(["skin", "eyes", "lips", "cloth", "cloth.trim", "trousers", "leather", "buckle"], maker.OfferedSlots);
        Assert.Empty(RecordChecks.Check(maker.Look, data, TestLooks.Lookup(data)));
        // Without the bodies' slots (none given) every slot of two swatches or more is offered; with no look (no body for
        // the race) none.
        Assert.Equal(maker.Slots.Where(s => s is not ("brows" or "eye.white")), new LookMaker("human", hero.Race("human")!, hero.Looks).OfferedSlots);
        using var empty = new VirtualFileSystem([]);
        var bodiless = new LookMaker("human", hero.Race("human")!, LookData.Load(empty), null, hero.SlotsOfBody);
        Assert.Null(bodiless.Look);
        Assert.Equal(13, bodiless.Slots.Count);
        Assert.Empty(bodiless.OfferedSlots);
    }

    /// <summary>
    /// Each category's options in the approved lists' order, "None" first (the manager's GM2-P36: a part's
    /// <see cref="LookPart.Order"/>, on the game's human): the man's and the woman's hair in Q61's lists' order, the beards,
    /// head pieces and marks; a hero switched to a man takes the beard's first option, "None"; the random roll draws over
    /// the same lists.
    /// </summary>
    [Fact]
    public void TheOptionsComeInTheApprovedOrderNoneFirst()
    {
        using var folder = new ScratchFolder();
        var (data, hero) = GameData(folder);
        using var _ = data;
        var maker = new LookMaker("human", hero.Race("human")!, hero.Looks, null, hero.SlotsOfBody);
        string Names(string category) => string.Join(", ", maker.Categories[category].Select(p => maker.Part(p).Name));
        Assert.Equal("Face 1, Face 2, Face 3, Face 4, Face 5, Face 6", Names("face"));
        Assert.Equal("None, Crop, Swept back, Spiky, Curly, Messy, Shaggy, Long, Ponytail, Topknot, Buzz cut, Side part, Fringe, Slicked back, Undercut, Mohawk, "
            + "Warrior braids, Half-tied, Mane, Receding, Curtains", Names("hair"));
        Assert.Equal("None, Stubble, Short trimmed, Full, Goatee, Moustache, Moustache with chin tuft, Mutton chops, Chin strap, Long, Braided, Forked, Twin braids",
            Names("beard"));
        Assert.Equal("None, Circlet, Flower, Headband, Hair beads, Feather, Leaf wreath, Hairpin, Earrings", Names("head-piece"));
        Assert.Equal("None, Freckles, Beauty mark, Scar over the brow, Scar on the cheek, Scar across the nose, Scar on the lip", Names("marks"));
        maker.UsePreset("human-female-1");
        Assert.Equal("None, Long, Long tied, Braid, Curls, Bun, High ponytail, Twin braids, Wavy, Bob, Curly, Pixie crop, Side braid, Crown braid, Half-up, "
            + "Long with fringe, Top bun, Shaggy, Undercut, Twin buns, Buzz cut", Names("hair"));
        Assert.False(maker.Categories.ContainsKey("beard"));
        // The Ranger made a man: a part the new body offers is kept, else the category's first: the beard "None", the hair
        // "None" (her long hair is the woman's part; the man's long hair is a part of its own), the first face.
        maker.SetSex("male");
        Assert.Equal("beard-none", maker.Look!.Parts["beard"]);
        Assert.Equal("hair-none", maker.Look.Parts["hair"]);
        Assert.Equal("face-human-male-1", maker.Look.Parts["face"]);
        Assert.Equal("marks-none", maker.Look.Parts["marks"]);
        Assert.Empty(RecordChecks.Check(maker.Look, data, TestLooks.Lookup(data)));
    }

    /// <summary>The races of the game's data beside the human (GM2 steps 9b–9e), each brought in with its lists and presets.</summary>
    static readonly string[] Races = ["elf", "dark-elf"];

    /// <summary>
    /// Every race of the game's own data is playable in the maker (steps 9b–9e, the hand-over's point 4): its eight presets
    /// in their order, each, with each sex and build, a look that passes every check; its lists per sex in the approved
    /// order (parts-by-race.md: the elf's every shared list, as the human's); 200 rolls each a valid look, many different.
    /// </summary>
    [Fact]
    public void EveryRaceOfTheGamesDataIsPlayableInTheMaker()
    {
        using var folder = new ScratchFolder();
        var (data, hero) = GameData(folder);
        using var _ = data;
        var human = new LookMaker("human", hero.Race("human")!, hero.Looks, null, hero.SlotsOfBody);
        var random = new GameRandom(9);
        // The references' lookup made once (it reads every record of the packs, every race's models among them).
        var lookup = TestLooks.Lookup(data);
        foreach (var race in Races)
        {
            var maker = new LookMaker(race, hero.Race(race)!, hero.Looks, null, hero.SlotsOfBody);
            List<string> Problems(LookHero look) => [.. RecordChecks.Check(look, data, lookup).Select(p => p.ToString())];
            Assert.Equal([.. new[] { "male", "female" }.SelectMany(s => Enumerable.Range(1, 4).Select(n => $"{race}-{s}-{n}"))], maker.Presets);
            Assert.Equal($"{race}-male-1", maker.Look!.Preset);
            foreach (var preset in maker.Presets)
            {
                maker.UsePreset(preset);
                Assert.True(Problems(maker.Look!) is [], $"{preset}: {string.Join("; ", Problems(maker.Look!))}");
                foreach (var sex in maker.Sexes)
                {
                    maker.SetSex(sex);
                    foreach (var build in maker.BuildsOf(sex))
                    {
                        maker.SetBuild(build);
                        Assert.True(Problems(maker.Look!) is [], $"{preset} as {sex} {build}: {string.Join("; ", Problems(maker.Look!))}");
                    }
                }
            }
            // The lists of each sex: the same names in the same order as the human's (faces their own: Face 1–6).
            foreach (var sex in new[] { "male", "female" })
            {
                maker.UsePreset($"{race}-{sex}-1");
                human.UsePreset($"human-{sex}-1");
                Assert.Equal(human.Categories.Keys.Order(StringComparer.Ordinal), maker.Categories.Keys.Order(StringComparer.Ordinal));
                foreach (var (category, options) in human.Categories)
                    Assert.Equal(options.Select(p => human.Part(p).Name), maker.Categories[category].Select(p => maker.Part(p).Name));
                Assert.All(maker.Categories["face"], p => Assert.StartsWith($"face-{race}-{sex}-", p, StringComparison.Ordinal));
            }
            var looks = new HashSet<string>();
            for (var i = 0; i < 200; i++)
            {
                maker.Roll(random);
                Assert.True(Problems(maker.Look!) is [], $"{race} roll {i}: {string.Join("; ", Problems(maker.Look!))}");
                looks.Add(RecordSerializer.ToJson(maker.Look!));
            }
            Assert.True(looks.Count > 190, $"{race}: {looks.Count} different looks in 200 rolls");
        }
    }

    /// <summary>
    /// The marks are made for each face (Q65, on the game's human): the maker offers a mark with the chosen face, keeps it
    /// when the face changes (every human face has every mark) and draws it by the face's model; the roll draws the face
    /// before the marks.
    /// </summary>
    [Fact]
    public void AMarkFollowsTheChosenFace()
    {
        using var folder = new ScratchFolder();
        var (data, hero) = GameData(folder);
        using var _ = data;
        var maker = new LookMaker("human", hero.Race("human")!, hero.Looks, null, hero.SlotsOfBody);
        Assert.Equal(["marks-none", "marks-freckles", "marks-beauty-mark", "marks-scar-brow", "marks-scar-cheek", "marks-scar-nose", "marks-scar-lip"], maker.Categories["marks"]);
        maker.SetPart("marks", "marks-scar-brow");
        maker.SetPart("face", "face-human-male-4");
        Assert.Equal("marks-scar-brow", maker.Look!.Parts["marks"]);
        Assert.Equal("models/humanoid/parts/marks/scar-brow/human-male-4/human-male-average",
            Figures.HeroFigure.Resolve(data, maker.Look).Parts.Single(p => p.Category == "marks").Model);
        // A face without that mark (as a mod's face with no marks: here the brow scar without the fourth face's model, a
        // mod's part over the game's): the mark isn't offered with it, and choosing that face takes the marks' first option ("None", GM2-P36).
        var scar = hero.Looks.Parts["marks-scar-brow"];
        Directory.CreateDirectory(folder["dev/data/looks/parts"]);
        File.WriteAllText(folder["dev/data/looks/parts/marks-scar-brow.json"],
            RecordSerializer.ToJson(scar with { Models = [.. scar.Models.Where(m => !m.Faces.Contains("face-human-male-4"))] }));
        using (var modded = new VirtualFileSystem(Core.Application.DataMounts(new Core.ApplicationOptions { DevelopmentFolder = folder["dev"] }, folder["game"], _ => null)))
        {
            var other = Screens.Races.HeroData.Of(modded);
            var withMod = new LookMaker("human", other.Race("human")!, other.Looks, maker.Look with { Parts = new Dictionary<string, string>(maker.Look.Parts) { ["face"] = "face-human-male-1" } }, other.SlotsOfBody);
            Assert.Equal("marks-scar-brow", withMod.Look!.Parts["marks"]);
            withMod.SetPart("face", "face-human-male-4");
            Assert.Equal("marks-none", withMod.Look!.Parts["marks"]);
            Assert.DoesNotContain("marks-scar-brow", withMod.Categories["marks"]);
            Assert.Empty(RecordChecks.Check(withMod.Look, modded, TestLooks.Lookup(modded)));
        }
        // Every rolled look's marks fit its face (the references' lookup made once: it reads every record of the packs).
        var random = new GameRandom(9);
        var lookup = TestLooks.Lookup(data);
        for (var i = 0; i < 100; i++)
        {
            maker.Roll(random);
            var look = maker.Look!;
            Assert.Empty(RecordChecks.Check(look, data, lookup));
            if (look.Parts["marks"] != "marks-none")
                Assert.Contains($"/{look.Parts["face"]["face-".Length..]}/", Figures.HeroFigure.Resolve(data, look).Parts.Single(p => p.Category == "marks").Model);
        }
    }

    public void Dispose() => _data.Dispose();
}
