using Statventure.Game.Data.Saves;
using Statventure.Records;
using Statventure.Storage;

namespace Statventure.Game.Tests;

/// <summary>
/// The save record (GM1-P5; Docs/Game/Foundation.md, "The records and files GM1 adds"): <c>save.game</c> version 1, the
/// name with its help and its check (not empty once trimmed, at most 80 characters, no control characters), no
/// <c>[Kind]</c> and marked not a pack kind (Q26); round trips of its binary form in a save file and of its JSON form in
/// a character file.
/// </summary>
public class SaveGameTests
{
    [Fact]
    public void TheSaveIsTheRecordSaveGameVersion2WithItsNamesHelpAndNoKind()
    {
        var shape = RecordShape.Of<SaveGame>();
        Assert.Equal(("save.game", 2), (shape.Name, shape.Version));
        Assert.Equal(["Name", "Hero", "Look"], shape.Fields.Select(f => f.Name));
        Assert.Equal("The character's name, as the slot list shows it.", shape.Field("name")!.Help);
        Assert.All(shape.Fields, f => Assert.False(string.IsNullOrEmpty(f.Help)));
        // Not a kind of mods: no [Kind] (no folder, a name from the code's), and not offered by the editor's New entry.
        Assert.False(typeof(SaveGame).IsDefined(typeof(KindAttribute), false));
        Assert.Null(shape.Folder);
        Assert.False(shape.IsPackKind);
        // A new save: no name yet, the old game's new hero (no race, HP 31, Mana 20, 5 gold), no look.
        Assert.Equal("", new SaveGame().Name);
        Assert.Equal(new Data.Actors.ActorSheet(), new SaveGame().Hero);
        Assert.Null(new SaveGame().Look);
    }

    /// <summary>Version 1 (GM1's: a name) upgrades to the old game's new hero with no race and no look, so a GM1 character opens on the new hero's flow (Q26).</summary>
    [Fact]
    public void Version1UpgradesToANewHeroWithNoRaceAndNoLook()
    {
        Assert.Equal(new SaveGame { Name = "Ser Alrik", Hero = new Data.Actors.ActorSheet(), Look = null }, new SaveGame.V1 { Name = "Ser Alrik" }.Upgrade());
        var hero = new SaveGame.V1 { Name = "x" }.Upgrade().Hero;
        Assert.Equal((null, 1, 31, 20, 5, 1), (hero.Race, hero.Level, hero.Hp, hero.Mana, hero.Purse.Gold, hero.ClassPoints));
        // Read from either form of version 1.
        Assert.Equal(new SaveGame { Name = "Ser Alrik" }, RecordSerializer.FromJson<SaveGame>("{ \"$types\": { \"save.game\": 1 }, \"name\": \"Ser Alrik\" }"));
        Assert.Equal(new SaveGame { Name = "Ser Alrik" }, RecordSerializer.FromBinary<SaveGame>(LibraryRecords.Read("save.game", "ser-alrik", ".rec")));
    }

    /// <summary>The look is the hero's race's: the race and the look are chosen together (Q25, Q26); no look with no race.</summary>
    [Fact]
    public void TheLookIsTheHerosRaces()
    {
        var hero = new Data.Actors.ActorSheet { Race = "human" };
        Assert.Empty(RecordChecks.Check(new SaveGame { Name = "Ser Alrik", Hero = hero, Look = TestLooks.WayfarerLook }));
        Assert.Empty(RecordChecks.Check(new SaveGame { Name = "Ser Alrik", Hero = hero }));
        Assert.Equal(["the look is a human's, but the hero is a dwarf (at $.look.race)"],
            RecordChecks.Check(new SaveGame { Name = "Ser Alrik", Hero = hero with { Race = "dwarf" }, Look = TestLooks.WayfarerLook }).Select(p => p.ToString()));
        Assert.Equal(["the look is a human's, but the hero has no race (at $.look.race)"],
            RecordChecks.Check(new SaveGame { Name = "Ser Alrik", Look = TestLooks.WayfarerLook }).Select(p => p.ToString()));
        // With the data the look's own checks run inside the save, at its path.
        using var looks = new TestLooks();
        Assert.Equal(["no beard chosen (the body \"human-male-average\" has 1 to choose from) (at $.look.parts)"],
            looks.Problems(new SaveGame { Name = "Ser Alrik", Hero = hero, Look = TestLooks.WayfarerLook with { Parts = new Dictionary<string, string> { ["face"] = "face-human-male-1", ["hair"] = "hair-bald" } } }));
        Assert.Empty(looks.Problems(new SaveGame { Name = "Ser Alrik", Hero = hero, Look = TestLooks.WayfarerLook }));
    }

    [Theory]
    [InlineData("Ser Alrik")]
    [InlineData("x")]
    [InlineData(" Ser Alrik ")]
    [InlineData("Ælfrida d'Œuvre-Ŝon «the Ember» 🜂")]
    [InlineData("Character 1")]
    public void AGoodNameHasNoProblem(string name)
    {
        Assert.Null(SaveGame.NameProblem(name));
        Assert.Empty(RecordChecks.Check(new SaveGame { Name = name }));
    }

    [Fact]
    public void TheNamesCheck()
    {
        // Not empty once trimmed.
        Assert.Equal("the name is empty", SaveGame.NameProblem(""));
        Assert.Equal("the name is empty", SaveGame.NameProblem("   "));
        Assert.Equal("the name is empty", SaveGame.NameProblem("　"));
        // At most 80 characters (UTF-16 units, as the old name field's maxlength).
        Assert.Null(SaveGame.NameProblem(new string('a', 80)));
        Assert.Equal("the name has 81 characters, more than 80", SaveGame.NameProblem(new string('a', 81)));
        Assert.Null(SaveGame.NameProblem(string.Concat(Enumerable.Repeat("🜂", 40))));
        Assert.Equal("the name has 82 characters, more than 80", SaveGame.NameProblem(string.Concat(Enumerable.Repeat("🜂", 41))));
        // No control characters: C0, DEL and C1, tab and line ends among them.
        Assert.Equal("the name holds a control character (U+0007)", SaveGame.NameProblem("Ser\aAlrik"));
        Assert.Equal("the name holds a control character (U+0009)", SaveGame.NameProblem("Ser\tAlrik"));
        Assert.Equal("the name holds a control character (U+000A)", SaveGame.NameProblem("Ser\nAlrik"));
        Assert.Equal("the name holds a control character (U+007F)", SaveGame.NameProblem("Ser\u007fAlrik"));
        Assert.Equal("the name holds a control character (U+0085)", SaveGame.NameProblem("Ser\u0085Alrik"));
        Assert.Equal("the name holds a control character (U+0000)", SaveGame.NameProblem("\0"));
        // Every problem reported, at the name.
        var problems = RecordChecks.Check(new SaveGame { Name = new string(' ', 79) + "\r\n" });
        Assert.Equal(["the name is empty (at $.name)", "the name has 81 characters, more than 80 (at $.name)", "the name holds a control character (U+000D) (at $.name)"],
            problems.Select(p => p.ToString()));
        Assert.Equal(["the name is empty", "the name has 81 characters, more than 80", "the name holds a control character (U+000D)"],
            SaveGame.NameProblems(new string(' ', 79) + "\r\n"));
    }

    [Fact]
    public void ItsFormsRoundTrip()
    {
        var save = new SaveGame { Name = "Ælfrida \"the Ember\" 🜂" };
        Assert.Equal(save, RecordSerializer.FromBinary<SaveGame>(RecordSerializer.ToBinary(save)));
        var json = RecordSerializer.ToJson(save);
        // Indented by two spaces, non-ASCII kept but a character outside the BMP escaped (the records' JSON form); the
        // new hero in full, no look (a null left out).
        Assert.StartsWith("{\n  \"$types\": {\n    \"save.game\": 2,\n    \"actor.sheet\": 1,", json);
        Assert.Contains("\n  \"name\": \"Ælfrida \\\"the Ember\\\" \\uD83D\\uDF02\",\n  \"hero\": {\n    \"level\": 1,", json);
        Assert.DoesNotContain("\"look\"", json);
        Assert.Equal(save, RecordSerializer.FromJson<SaveGame>(json));
        Assert.Equal("save.game", RecordSerializer.RootTypeName(RecordSerializer.ToBinary(save)));
        // A played hero with its look.
        var played = new SaveGame { Name = "Ser Alrik", Hero = ActorSheetTests.Played with { Race = "human" }, Look = TestLooks.WayfarerLook with { Preset = "human-male-1" } };
        Assert.Equal(played, RecordSerializer.FromBinary<SaveGame>(RecordSerializer.ToBinary(played)));
        Assert.Equal(played, RecordSerializer.FromJson<SaveGame>(RecordSerializer.ToJson(played)));
        Assert.Contains("\"look\": {\n    \"race\": \"human\",\n    \"sex\": \"male\",\n    \"body\": \"human-male-average\",\n    \"parts\": {\n      \"face\": \"face-human-male-1\",",
            RecordSerializer.ToJson(played));
    }

    [Fact]
    public void ASaveFileAndACharacterFileRoundTrip()
    {
        using var scratch = new ScratchFolder();
        var save = new SaveGame { Name = "Ser Alrik" };
        // The slot's file: the engine's header, then the binary form.
        var file = SaveFile.Write(save);
        Assert.Equal("SVSV"u8.ToArray(), file[..4]);
        var reading = SaveFile.Read<SaveGame>(file);
        Assert.Equal((SaveState.Readable, save), (reading.State, reading.Value));
        // The character file: the JSON form.
        CharacterFile.Export(scratch["stat-fick-character.json"], save);
        Assert.Equal(RecordSerializer.ToJson(save), File.ReadAllText(scratch["stat-fick-character.json"]));
        Assert.Equal(save, CharacterFile.Import<SaveGame>(scratch["stat-fick-character.json"]));
    }
}
