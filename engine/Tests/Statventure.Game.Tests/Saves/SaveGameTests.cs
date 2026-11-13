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
    public void TheSaveIsTheRecordSaveGameVersion1WithItsNamesHelpAndNoKind()
    {
        var shape = RecordShape.Of<SaveGame>();
        Assert.Equal(("save.game", 1), (shape.Name, shape.Version));
        Assert.Equal(["Name"], shape.Fields.Select(f => f.Name));
        Assert.Equal("The character's name, as the slot list shows it.", shape.Field("name")!.Help);
        // Not a kind of mods: no [Kind] (no folder, a name from the code's), and not offered by the editor's New entry.
        Assert.False(typeof(SaveGame).IsDefined(typeof(KindAttribute), false));
        Assert.Null(shape.Folder);
        Assert.False(shape.IsPackKind);
        Assert.Equal("", new SaveGame().Name);
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
        // Indented by two spaces, non-ASCII kept but a character outside the BMP escaped (the records' JSON form).
        Assert.Equal("{\n  \"$types\": {\n    \"save.game\": 1\n  },\n  \"name\": \"Ælfrida \\\"the Ember\\\" \\uD83D\\uDF02\"\n}\n", json);
        Assert.Equal(save, RecordSerializer.FromJson<SaveGame>(json));
        Assert.Equal("save.game", RecordSerializer.RootTypeName(RecordSerializer.ToBinary(save)));
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
