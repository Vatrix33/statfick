using System.Reflection;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Records;

namespace Statventure.Game.Tests;

/// <summary>
/// The character maker's records (GM2-P6, GM2-P7; Docs/Game/Hero.md, "How it works", step 6): their kinds, folders and
/// marks, one list of each set of choices, and every check against a case: the look's (G4: a body of another race or sex,
/// a part of another category, a part its body isn't offered by race, sex or build, a category the body offers left out,
/// a colour the race has no swatch of), the part's, the body's, the preset's, the race's swatches', and the references
/// (a missing model refused, GM2-P18).
/// </summary>
public class LookRecordTests
{
    [Fact]
    public void TheKindsTheirFoldersAndMarks()
    {
        Assert.Equal(("look.body", 1, "data/looks/bodies", true), Kind(typeof(LookBody)));
        Assert.Equal(("look.part", 1, "data/looks/parts", true), Kind(typeof(LookPart)));
        Assert.Equal(("look.preset", 1, "data/looks/presets", true), Kind(typeof(LookPreset)));
        // The look is a save's part (and a preset's), never an entry; a part's model is a part of a part.
        Assert.False(RecordShape.Of(typeof(LookHero)).IsPackKind);
        Assert.Null(typeof(LookHero).GetCustomAttribute<KindAttribute>());
        Assert.Null(typeof(LookPartModel).GetCustomAttribute<KindAttribute>());
        Assert.Equal(("look.hero", 1), (RecordShape.Of(typeof(LookHero)).Name, RecordShape.Of(typeof(LookHero)).Version));
        Assert.Equal(("look.part.model", 1), (RecordShape.Of(typeof(LookPartModel)).Name, RecordShape.Of(typeof(LookPartModel)).Version));
        Assert.Equal(["Race", "Sex", "Body", "Parts", "Colours", "Preset"], RecordShape.Of(typeof(LookHero)).Fields.Select(f => f.Name));
        Assert.Equal(["Race", "Sex", "Build", "Model"], RecordShape.Of(typeof(LookBody)).Fields.Select(f => f.Name));
        Assert.Equal(["Category", "Name", "Races", "Sexes", "Models", "Slots"], RecordShape.Of(typeof(LookPart)).Fields.Select(f => f.Name));
        Assert.Equal(["Model", "Bodies"], RecordShape.Of(typeof(LookPartModel)).Fields.Select(f => f.Name));
        Assert.Equal(["Label", "Order", "Look"], RecordShape.Of(typeof(LookPreset)).Fields.Select(f => f.Name));
        foreach (var type in new[] { typeof(LookHero), typeof(LookBody), typeof(LookPart), typeof(LookPartModel), typeof(LookPreset) })
        {
            Assert.NotNull(type.GetCustomAttribute<HelpAttribute>());
            foreach (var field in RecordShape.Of(type).Fields)
                Assert.False(string.IsNullOrEmpty(field.Help), $"{type.Name}.{field.Name} has no help");
        }

        static (string, int, string?, bool) Kind(Type type) =>
            (RecordShape.Of(type).Name, RecordShape.Of(type).Version, RecordShape.Of(type).Folder, RecordShape.Of(type).IsPackKind);
    }

    /// <summary>Each list of choices written once and held by every field that chooses from it; the palette slots are step 4a's final fifteen.</summary>
    [Fact]
    public void EachListOfChoicesIsOne()
    {
        Assert.Equal(["male", "female"], LookNames.Sexes);
        Assert.Equal(["slender", "average", "strong"], LookNames.Builds);
        Assert.Equal(["face", "brows", "hair", "beard", "crest", "ears", "tail", "marks", "head-piece"], LookNames.Categories);
        Assert.Equal(["skin", "hair", "hair.tie", "eyes", "brows", "lips", "cloth", "cloth.trim", "trousers", "crest", "marking", "claws", "eye.white", "leather", "buckle"],
            LookNames.Slots);
        Assert.Equal(LookNames.Sexes, LookNames.Choices(typeof(LookHero), nameof(LookHero.Sex)));
        Assert.Equal(LookNames.Sexes, LookNames.Choices(typeof(LookPart), nameof(LookPart.Sexes)));
        Assert.Equal(LookNames.Slots, LookNames.Choices(typeof(RaceSwatch), nameof(RaceSwatch.Slot)));
        Assert.Contains(LookNames.Average, LookNames.Builds);
        Assert.Equal([LookNames.Male, LookNames.Female], LookNames.Sexes);
        // The references: races, bodies, parts, presets, and models by their whole path under models/.
        Assert.Equal(("race", "data/races"), Reference(typeof(LookHero), "race"));
        Assert.Equal(("look.body", "data/looks/bodies"), Reference(typeof(LookHero), "body"));
        Assert.Equal(("look.part", "data/looks/parts"), Reference(typeof(LookHero), "parts"));
        Assert.Equal(("look.preset", "data/looks/presets"), Reference(typeof(LookHero), "preset"));
        Assert.Equal(("race", "data/races"), Reference(typeof(LookBody), "race"));
        Assert.Equal(("model", "models"), Reference(typeof(LookBody), "model"));
        Assert.Equal(("race", "data/races"), Reference(typeof(LookPart), "races"));
        Assert.Equal(("model", "models"), Reference(typeof(LookPartModel), "model"));
        Assert.Equal(("look.body", "data/looks/bodies"), Reference(typeof(LookPartModel), "bodies"));
        Assert.Null(RecordShape.Of(typeof(LookHero)).Field("colours")!.References.FirstOrDefault());

        static (string, string)? Reference(Type type, string field) =>
            RecordShape.Of(type).Field(field)!.References.SingleOrDefault() is { } r ? (r.Kind.Name, r.Folder) : null;
    }

    [Fact]
    public void TheTestDataPassesEveryCheck()
    {
        using var looks = new TestLooks();
        foreach (var path in looks.Data.List("data"))
            Assert.True(looks.Problems(looks.Data.Load(Mods.GameMods.GameKinds, path)) is [], $"{path}: {string.Join("; ", looks.Problems(looks.Data.Load(Mods.GameMods.GameKinds, path)))}");
        Assert.Empty(looks.Problems(TestLooks.WayfarerLook));
        // A woman's look with the braid, a dwarf's with a full beard: other bodies' offers.
        Assert.Empty(looks.Problems(new LookHero
        {
            Race = TestLooks.Human, Sex = LookNames.Female, Body = TestLooks.HumanFemale, Parts = new Dictionary<string, string> { ["hair"] = "hair-braid" },
            Colours = new Dictionary<string, string> { ["skin"] = "Umber", ["hair"] = "Raven" }, Preset = "human-male-1",
        }));
        Assert.Empty(looks.Problems(new LookHero
        {
            Race = TestLooks.Dwarf, Body = TestLooks.DwarfMale, Parts = new Dictionary<string, string> { ["hair"] = "hair-bald", ["beard"] = "beard-full" },
            Colours = new Dictionary<string, string> { ["skin"] = "Ruddy" },
        }));
    }

    /// <summary>The look's checks with the data, each on its own (and the marks' references where a name points at nothing).</summary>
    [Fact]
    public void ALookTheMakerCouldntMakeIsRefused()
    {
        using var looks = new TestLooks();
        var look = TestLooks.WayfarerLook;
        LookHero With(Action<Dictionary<string, string>>? parts = null, Action<Dictionary<string, string>>? colours = null)
        {
            var p = new Dictionary<string, string>(look.Parts);
            var c = new Dictionary<string, string>(look.Colours);
            parts?.Invoke(p);
            colours?.Invoke(c);
            return look with { Parts = p, Colours = c };
        }
        // A body of another race, of another sex.
        Assert.Equal(["the body \"human-female-average\" is a female body, not a male one (at $.body)", "\"face-human-male-1\" isn't for a female body (at $.parts[\"face\"])",
            "\"beard-none\" isn't for a female body (at $.parts[\"beard\"])"], looks.Problems(look with { Body = TestLooks.HumanFemale }));
        Assert.Equal(["the body \"dwarf-male-average\" is the dwarf's, not the human's (at $.body)", "\"face-human-male-1\" isn't for the dwarf (at $.parts[\"face\"])",
            "\"hair-braid\" isn't for the dwarf (at $.parts[\"hair\"])"], looks.Problems(look with { Body = TestLooks.DwarfMale }));
        // A part of another category; not for the race; not for the sex; not fitted to the body (its build).
        Assert.Equal(["\"beard-none\" is a beard, not a hair (at $.parts[\"hair\"])"], looks.Problems(With(p => p["hair"] = "beard-none")));
        Assert.Equal(["\"beard-full\" isn't for the human (at $.parts[\"beard\"])"], looks.Problems(With(p => p["beard"] = "beard-full")));
        Assert.Equal(["\"hair-braid\" has no model for the body \"human-male-strong\" (at $.parts[\"hair\"])"], looks.Problems(look with { Body = TestLooks.HumanMaleStrong }));
        Assert.Empty(looks.Problems(With(p => p["hair"] = "hair-bald") with { Body = TestLooks.HumanMaleStrong }));
        // A category the body offers left out; a category that isn't one.
        Assert.Equal(["no beard chosen (the body \"human-male-average\" has 1 to choose from) (at $.parts)"], looks.Problems(With(p => p.Remove("beard"))));
        Assert.Equal(["no hair chosen (the body \"human-male-average\" has 2 to choose from) (at $.parts)"], looks.Problems(With(p => p.Remove("hair"))));
        Assert.Equal(["\"wings\" isn't a category (face, brows, hair, beard, crest, ears, tail, marks, head-piece) (at $.parts[\"wings\"])", "no look part \"hair-braid-x\" (data/looks/parts/hair-braid-x) (at $.parts[\"wings\"])"],
            looks.Problems(With(p => p["wings"] = "hair-braid-x")));
        Assert.Equal(["\"wings\" isn't a category (face, brows, hair, beard, crest, ears, tail, marks, head-piece) (at $.parts[\"wings\"])"],
            looks.Problems(With(p => p["wings"] = "hair-bald")));
        // A part entry that isn't a part: the reference's problem only (the look's checks pass it by).
        looks.Write($"{LookNames.PartsFolder}/hair-odd", TestLooks.Races[TestLooks.Human]);
        Assert.Equal(["data/looks/parts/hair-odd isn't a look part: it holds a race record (at $.parts[\"hair\"])"], looks.Problems(With(p => p["hair"] = "hair-odd")));
        Assert.Equal(["hair-bald", "hair-braid"], LookData.Read(looks.Data).Parts.Keys.Where(k => k.StartsWith("hair", StringComparison.Ordinal)));
        Assert.Contains("not look.part", Assert.Throws<Statventure.Packs.DataFileException>(() => LookData.Load(looks.Data)).Message);
        looks.Delete($"{LookNames.PartsFolder}/hair-odd");
        // A category no part of the body's is: "crest" with a part that isn't one.
        Assert.Equal(["\"hair-bald\" is a hair, not a crest (at $.parts[\"crest\"])"], looks.Problems(With(p => p["crest"] = "hair-bald")));
        // Colours: a slot that isn't one, a slot the race has no swatches of, a swatch the race hasn't, a slot left out.
        Assert.Equal(["\"fur\" isn't a palette slot (skin, hair, hair.tie, eyes, brows, lips, cloth, cloth.trim, trousers, crest, marking, claws, eye.white, leather, buckle) (at $.colours[\"fur\"])"],
            looks.Problems(With(colours: c => c["fur"] = "Ash")));
        Assert.Equal(["the human has no cloth colours (at $.colours[\"cloth\"])"], looks.Problems(With(colours: c => c["cloth"] = "Ash")));
        Assert.Equal(["the human has no hair colour \"Ruddy\" (at $.colours[\"hair\"])"], looks.Problems(With(colours: c => c["hair"] = "Ruddy")));
        Assert.Equal(["the human has no hair colour \"Umber\" (at $.colours[\"hair\"])"], looks.Problems(With(colours: c => c["hair"] = "Umber")));
        Assert.Equal(["no skin colour chosen (the human has 2 to choose from) (at $.colours)"], looks.Problems(With(colours: c => c.Remove("skin"))));
        // Names that point at nothing: the references' problems (the look's own checks pass them by).
        Assert.Equal(["the body \"human-male-average\" is the human's, not the elf's (at $.body)", "no race \"elf\" (data/races/elf) (at $.race)"],
            looks.Problems(look with { Race = "elf" }));
        Assert.Equal(["no look body \"human-male-slender\" (data/looks/bodies/human-male-slender) (at $.body)"], looks.Problems(look with { Body = "human-male-slender" }));
        Assert.Equal(["no look preset \"human-male-9\" (data/looks/presets/human-male-9) (at $.preset)"], looks.Problems(look with { Preset = "human-male-9" }));
        Assert.Equal(["\"a/b\" can't name a look part in data/looks/parts (at $.parts[\"hair\"])"], looks.Problems(With(p => p["hair"] = "a/b")));
    }

    /// <summary>Without the data a look's checks can only see its keys (the session's <c>Change</c> checks a save so).</summary>
    [Fact]
    public void WithoutTheDataOnlyTheKeysAreChecked()
    {
        Assert.Empty(RecordChecks.Check(TestLooks.WayfarerLook with { Body = "nothing", Preset = "nothing" }));
        Assert.Equal(["\"wings\" isn't a category (face, brows, hair, beard, crest, ears, tail, marks, head-piece) (at $.parts[\"wings\"])",
            "\"fur\" isn't a palette slot (skin, hair, hair.tie, eyes, brows, lips, cloth, cloth.trim, trousers, crest, marking, claws, eye.white, leather, buckle) (at $.colours[\"fur\"])"],
            RecordChecks.Check(new LookHero { Parts = new Dictionary<string, string> { ["wings"] = "x" }, Colours = new Dictionary<string, string> { ["fur"] = "x" } }).Select(p => p.ToString()));
        Assert.Equal(["\"other\" isn't one of the choices: male, female (at $.sex)"], RecordChecks.Check(new LookHero { Sex = "other" }).Select(p => p.ToString()));
    }

    [Fact]
    public void APartsChecks()
    {
        using var looks = new TestLooks();
        var braid = TestLooks.Parts["hair-braid"];
        Assert.Equal(["the name is empty (at $.name)"], looks.Problems(braid with { Name = " " }));
        Assert.Equal(["a part is for one race at least (at $.races)", "the body \"human-male-average\" is the human's, and the part isn't for the human (at $.models[0].bodies[0])",
            "the body \"human-female-average\" is the human's, and the part isn't for the human (at $.models[1].bodies[0])"], looks.Problems(braid with { Races = [] }));
        Assert.Equal(["a part is for one sex at least (at $.sexes)", "the body \"human-male-average\" is a male body, and the part isn't for a male body (at $.models[0].bodies[0])",
            "the body \"human-female-average\" is a female body, and the part isn't for a female body (at $.models[1].bodies[0])"], looks.Problems(braid with { Sexes = [] }));
        Assert.Equal(["\"human\" is there twice (at $.races[1])", "\"male\" is there twice (at $.sexes[2])", "\"hair\" is there twice (at $.slots[2])"],
            looks.Problems(braid with { Races = [TestLooks.Human, TestLooks.Human], Sexes = ["male", "female", "male"], Slots = ["hair", "hair.tie", "hair"] }));
        Assert.Equal(["\"wig\" isn't one of the choices: face, brows, hair, beard, crest, ears, tail, marks, head-piece (at $.category)"], looks.Problems(braid with { Category = "wig" }));
        Assert.Equal(["\"fur\" isn't one of the choices: skin, hair, hair.tie, eyes, brows, lips, cloth, cloth.trim, trousers, crest, marking, claws, eye.white, leather, buckle (at $.slots[1])"],
            looks.Problems(braid with { Slots = ["hair", "fur"] }));
        // The models: each for a body at least, each body one model's, each body of the part's races and sexes.
        Assert.Equal(["a model is for one body at least (at $.models[1].bodies)"],
            looks.Problems(braid with { Models = [braid.Models[0], braid.Models[1] with { Bodies = [] }] }));
        Assert.Equal(["the body \"human-male-average\" has a model already (models[0]) (at $.models[1].bodies[1])"],
            looks.Problems(braid with { Models = [braid.Models[0], braid.Models[1] with { Bodies = [TestLooks.HumanFemale, TestLooks.HumanMale] }] }));
        Assert.Equal(["\"human-female-average\" is there twice (at $.models[1].bodies[1])"],
            looks.Problems(braid with { Models = [braid.Models[0], braid.Models[1] with { Bodies = [TestLooks.HumanFemale, TestLooks.HumanFemale] }] }));
        Assert.Equal(["the body \"dwarf-male-average\" is the dwarf's, and the part isn't for the dwarf (at $.models[1].bodies[0])"],
            looks.Problems(braid with { Models = [braid.Models[0], braid.Models[1] with { Bodies = [TestLooks.DwarfMale] }] }));
        Assert.Equal(["the body \"human-female-average\" is a female body, and the part isn't for a female body (at $.models[1].bodies[0])"],
            looks.Problems(braid with { Sexes = ["male"] }));
        // A missing model is refused, as every reference (GM2-P18); a body that isn't there too.
        Assert.Equal(["no model \"models/humanoid/parts/hair/braid/elf\" (models/humanoid/parts/hair/braid/elf) (at $.models[1].model)"],
            looks.Problems(braid with { Models = [braid.Models[0], braid.Models[1] with { Model = "models/humanoid/parts/hair/braid/elf" }] }));
        Assert.Equal(["no look body \"elf-female-average\" (data/looks/bodies/elf-female-average) (at $.models[1].bodies[0])"],
            looks.Problems(braid with { Models = [braid.Models[0], braid.Models[1] with { Bodies = ["elf-female-average"] }] }));
        Assert.Equal(["no race \"elf\" (data/races/elf) (at $.races[1])"], looks.Problems(braid with { Races = [TestLooks.Human, "elf"] }));
        Assert.Equal(["\"hair/braid\" can't name a model in models (at $.models[0].model)"],
            looks.Problems(braid with { Models = [braid.Models[0] with { Model = "hair/braid" }, braid.Models[1]] }));
        // Without the data: the record's own rules only.
        Assert.Empty(RecordChecks.Check(braid with { Races = ["elf"], Models = [braid.Models[0] with { Bodies = ["elf-male-average"] }] }));
    }

    /// <summary>Which parts a body is offered: by race, sex and a model fitted to it; a part with no model on every body of its races and sexes.</summary>
    [Fact]
    public void WhatABodyIsOffered()
    {
        using var looks = new TestLooks();
        var data = LookData.Load(looks.Data);
        Assert.Equal(["dwarf-male-average", "human-female-average", "human-male-average", "human-male-strong"], data.Bodies.Keys);
        string Offer(string body) =>
            string.Join("; ", data.Offered(body, data.Bodies[body]).Select(c => $"{c.Key}: {string.Join(", ", c.Value)}"));
        Assert.Equal("face: face-human-male-1; hair: hair-bald, hair-braid; beard: beard-none", Offer(TestLooks.HumanMale));
        Assert.Equal("face: face-human-male-1; hair: hair-bald; beard: beard-none", Offer(TestLooks.HumanMaleStrong));
        Assert.Equal("hair: hair-braid", Offer(TestLooks.HumanFemale));
        Assert.Equal("hair: hair-bald; beard: beard-full, beard-none", Offer(TestLooks.DwarfMale));
        Assert.Equal("human-male-strong", data.BodyOf(TestLooks.Human, LookNames.Male, "strong"));
        Assert.Null(data.BodyOf(TestLooks.Human, LookNames.Female, "strong"));
        Assert.Equal(["human-male-1"], data.PresetsOf(TestLooks.Human));
        Assert.Empty(data.PresetsOf(TestLooks.Dwarf));
        // A race's list: by order, the same order by name.
        var preset = TestLooks.Presets["human-male-1"];
        looks.Write($"{LookNames.PresetsFolder}/human-z", preset with { Order = 5 });
        looks.Write($"{LookNames.PresetsFolder}/human-y", preset with { Order = 5 });
        looks.Write($"{LookNames.PresetsFolder}/human-a", preset with { Order = 90 });
        looks.Write($"{LookNames.PresetsFolder}/dwarf-a", preset with { Look = preset.Look with { Race = TestLooks.Dwarf } });
        Assert.Equal(["human-y", "human-z", "human-male-1", "human-a"], LookData.Load(looks.Data).PresetsOf(TestLooks.Human));
        Assert.Equal(["dwarf-a"], LookData.Load(looks.Data).PresetsOf(TestLooks.Dwarf));
        // The refusals' words, each reason first in its order: the race, the sex, the model.
        var braid = data.Parts["hair-braid"];
        Assert.Equal("isn't for the dwarf", braid.Refusal(TestLooks.DwarfMale, data.Bodies[TestLooks.DwarfMale]));
        Assert.Equal("isn't for a female body", data.Parts["beard-none"].Refusal(TestLooks.HumanFemale, data.Bodies[TestLooks.HumanFemale]));
        Assert.Equal("has no model for the body \"human-male-strong\"", braid.Refusal(TestLooks.HumanMaleStrong, data.Bodies[TestLooks.HumanMaleStrong]));
        Assert.Null(braid.Refusal(TestLooks.HumanFemale, data.Bodies[TestLooks.HumanFemale]));
        Assert.True(braid.IsFor(TestLooks.HumanMale, data.Bodies[TestLooks.HumanMale]));
        Assert.False(braid.IsFor(TestLooks.HumanMaleStrong, data.Bodies[TestLooks.HumanMaleStrong]));
    }

    [Fact]
    public void ABodysChecks()
    {
        using var looks = new TestLooks();
        var body = TestLooks.Bodies[TestLooks.HumanMale];
        Assert.Equal(["\"tall\" isn't one of the choices: slender, average, strong (at $.build)"], looks.Problems(body with { Build = "tall" }));
        Assert.Equal(["\"other\" isn't one of the choices: male, female (at $.sex)"], looks.Problems(body with { Sex = "other" }));
        Assert.Equal(["no race \"elf\" (data/races/elf) (at $.race)"], looks.Problems(body with { Race = "elf" }));
        Assert.Equal(["no model \"models/humanoid/bodies/elf-male-average\" (models/humanoid/bodies/elf-male-average) (at $.model)"],
            looks.Problems(body with { Model = "models/humanoid/bodies/elf-male-average" }));
        Assert.Equal(["\"\" can't name a model in models (at $.model)"], looks.Problems(body with { Model = "" }));
        // One body per race, sex and build: a second one in the data is a problem of both.
        looks.Write($"{LookNames.BodiesFolder}/human-male-average-2", body);
        Assert.Equal(["2 bodies are the human male average body (human-male-average, human-male-average-2): the maker takes one"], looks.Problems(body));
        Assert.Empty(RecordChecks.Check(body));
    }

    [Fact]
    public void APresetsChecks()
    {
        using var looks = new TestLooks();
        var preset = TestLooks.Presets["human-male-1"];
        Assert.Equal(["the label is empty (at $.label)"], looks.Problems(preset with { Label = "" }));
        Assert.Equal(["a preset's look names no preset (at $.look.preset)"], looks.Problems(preset with { Look = preset.Look with { Preset = "human-male-1" } }));
        // Its look is checked as a hero's.
        Assert.Equal(["no beard chosen (the body \"human-male-average\" has 1 to choose from) (at $.look.parts)"],
            looks.Problems(preset with { Look = preset.Look with { Parts = new Dictionary<string, string> { ["face"] = "face-human-male-1", ["hair"] = "hair-braid" } } }));
    }

    [Fact]
    public void TheRacesSwatchesAreNamedOncePerSlot()
    {
        var race = TestLooks.Races[TestLooks.Human];
        Assert.Empty(RecordChecks.Check(race));
        Assert.Equal(["the swatch's name is empty (at $.swatches[1].name)"],
            RecordChecks.Check(race with { Swatches = [race.Swatches[0], race.Swatches[1] with { Name = " " }] }).Select(p => p.ToString()));
        Assert.Equal(["the race has another skin colour \"Fair\" (at $.swatches[1].name)"],
            RecordChecks.Check(race with { Swatches = [race.Swatches[0], race.Swatches[1] with { Name = "Fair" }] }).Select(p => p.ToString()));
        // The same name in two slots is two swatches.
        Assert.Empty(RecordChecks.Check(race with { Swatches = [race.Swatches[0], race.Swatches[2] with { Name = "Fair" }] }));
        Assert.Equal(["\"fur\" isn't one of the choices: skin, hair, hair.tie, eyes, brows, lips, cloth, cloth.trim, trousers, crest, marking, claws, eye.white, leather, buckle (at $.swatches[0].slot)"],
            RecordChecks.Check(race with { Swatches = [race.Swatches[0] with { Slot = "fur" }] }).Select(p => p.ToString()));
    }

    [Fact]
    public void ALooksMapsCompareByTheirContents()
    {
        var look = TestLooks.WayfarerLook;
        var reordered = look with
        {
            Parts = new Dictionary<string, string> { ["beard"] = "beard-none", ["hair"] = "hair-braid", ["face"] = "face-human-male-1" },
            Colours = new Dictionary<string, string> { ["hair"] = "Ash", ["skin"] = "Fair" },
        };
        Assert.Equal(look, reordered);
        Assert.Equal(look.GetHashCode(), reordered.GetHashCode());
        Assert.NotEqual(look, look with { Parts = new Dictionary<string, string>(look.Parts) { ["hair"] = "hair-bald" } });
        Assert.NotEqual(look, look with { Colours = new Dictionary<string, string> { ["skin"] = "Fair" } });
        Assert.NotEqual(look, look with { Colours = new Dictionary<string, string> { ["skin"] = "Fair", ["hairs"] = "Ash" } });
        Assert.NotEqual(look, look with { Colours = new Dictionary<string, string>(look.Colours) { ["eyes"] = "Ash" } });
        Assert.NotEqual(look, look with { Preset = "human-male-1" });
        Assert.NotEqual(look, look with { Sex = LookNames.Female });
        Assert.NotEqual(look, look with { Race = TestLooks.Dwarf });
        Assert.NotEqual(look, look with { Body = TestLooks.HumanMaleStrong });
        Assert.NotEqual(look, null);
        // Both forms keep every choice.
        Assert.Equal(look, RecordSerializer.FromBinary<LookHero>(RecordSerializer.ToBinary(look)));
        Assert.Equal(look, RecordSerializer.FromJson<LookHero>(RecordSerializer.ToJson(look)));
    }

    [Fact]
    public void TheLooksFixturesAreThere()
    {
        LibraryRecords.Ensure("human-male-average", TestLooks.Bodies[TestLooks.HumanMale]);
        LibraryRecords.Ensure("hair-braid", TestLooks.Parts["hair-braid"]);
        LibraryRecords.Ensure("human-male-1", TestLooks.Presets["human-male-1"]);
        Assert.Equal(TestLooks.Bodies[TestLooks.HumanMale], LibraryRecords.Load("look.body", "human-male-average", ".json"));
        Assert.Equal(TestLooks.Presets["human-male-1"], LibraryRecords.Load("look.preset", "human-male-1", ".rec"));
        var braid = (LookPart)LibraryRecords.Load("look.part", "hair-braid", ".json");
        Assert.Equal(LibraryRecords.Json(typeof(LookPart), TestLooks.Parts["hair-braid"]), LibraryRecords.Json(typeof(LookPart), braid));
    }
}
