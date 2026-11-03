using System.Text;
using Statventure.Records;
using Statventure.Storage;
using Statventure.Tests.Records;

namespace Statventure.Tests.Storage;

/// <summary>
/// Character files (Docs/Assets.md, "Character files"; Q19): the save record's JSON form exported and
/// imported (export then import gives the same save), the committed files of past versions upgraded,
/// the old game's 10 MiB limit both ways, and failures that leave the old file.
/// </summary>
public class CharacterFileTests
{
    [Fact]
    public void TheDefaultNameIsTheOldGames() => Assert.Equal("stat-fick-character.json", CharacterFile.DefaultName);

    [Fact]
    public void AFileIsTheRecordsJsonForm()
    {
        using var temp = new StorageFolder();
        var file = temp["out/" + CharacterFile.DefaultName];
        Directory.CreateDirectory(temp["out"]);
        CharacterFile.Export(file, StorageTestData.Sample());
        Assert.Equal(RecordSerializer.ToJson(StorageTestData.Sample()), File.ReadAllText(file));
        Assert.StartsWith("{\n  \"$types\": {\n    \"test.creature\": 3,", File.ReadAllText(file));
        Assert.Equal(["out/stat-fick-character.json"], temp.Files());
        RecordAssert.Equal(StorageTestData.Sample(), CharacterFile.Import<Creature>(file));
        if (!OperatingSystem.IsWindows())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(file));
        // Exporting again replaces the file.
        CharacterFile.Export(file, new Creature { Name = "Other" });
        Assert.Equal("Other", CharacterFile.Import<Creature>(file).Name);
        Assert.Equal(["out/stat-fick-character.json"], temp.Files());
    }

    [Fact]
    public void ExportThenImportGivesTheSameSave()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp["game"]);
        storage.Saves.Save("hero", StorageTestData.Sample());
        var loaded = storage.Saves.Load<Creature>("hero").Value;
        CharacterFile.Export(temp["hero.json"], loaded);
        // Load Character makes it the current save, in the slot the player picks.
        storage.Saves.Save("copy", CharacterFile.Import<Creature>(temp["hero.json"]));
        storage.Flush();
        Assert.Equal(File.ReadAllBytes(temp["game/saves/hero.dat"]), File.ReadAllBytes(temp["game/saves/copy.dat"]));
    }

    [Fact]
    public void TheOldVersionCharacterFilesAreRecorded()
    {
        foreach (var (label, versions, _) in StorageTestData.Versions)
            StorageTestData.Ensure($"Characters/{label}.json", () => StorageTestData.Json(versions));
    }

    [Theory]
    [MemberData(nameof(SaveFileTests.Labels), MemberType = typeof(SaveFileTests))]
    public void ACharacterFileOfAPastVersionIsUpgraded(string label)
    {
        var (_, versions, expected) = StorageTestData.Versions.Single(v => v.Label == label);
        var path = StorageTestData.PathOf($"Characters/{label}.json");
        RecordAssert.Equal(expected, CharacterFile.Import<Creature>(path));
        Assert.Equal(StorageTestData.Json(versions), File.ReadAllBytes(path));
    }

    [Fact]
    public void TheOldGamesSizeLimitHoldsBothWays()
    {
        Assert.Equal(10 * 1024 * 1024, CharacterFile.MaxBytes);
        using var temp = new StorageFolder();
        var length = RecordSerializer.ToJsonUtf8(typeof(Creature), new Creature { Name = "" }).Length;
        var largest = new Creature { Name = new string('a', (int)CharacterFile.MaxBytes - length) };
        CharacterFile.Export(temp["largest.json"], largest);
        Assert.Equal(CharacterFile.MaxBytes, new FileInfo(temp["largest.json"]).Length);
        Assert.Equal(largest.Name, CharacterFile.Import<Creature>(temp["largest.json"]).Name);
        Assert.Throws<InvalidDataException>(() => CharacterFile.Export(temp["larger.json"], largest with { Name = largest.Name + "a" }));
        Assert.Equal(["largest.json"], temp.Files());
        // A larger file isn't read at all; one of the limit is (and then isn't a record).
        temp.Write("big.json", Encoding.ASCII.GetBytes(new string(' ', (int)CharacterFile.MaxBytes + 1)));
        Assert.Throws<InvalidDataException>(() => CharacterFile.Import<Creature>(temp["big.json"]));
        temp.Write("limit.json", Encoding.ASCII.GetBytes(new string(' ', (int)CharacterFile.MaxBytes)));
        Assert.Throws<RecordFormatException>(() => CharacterFile.Import<Creature>(temp["limit.json"]));
    }

    [Fact]
    public void AFileThatIsntACharacterIsRefused()
    {
        using var temp = new StorageFolder();
        temp.Write("text.json", "not json"u8.ToArray());
        Assert.Throws<RecordFormatException>(() => CharacterFile.Import<Creature>(temp["text.json"]));
        CharacterFile.Export(temp["part.json"], new Part { Name = "horn" });
        Assert.Contains("\"test.part\"", Assert.Throws<RecordFormatException>(() => CharacterFile.Import<Creature>(temp["part.json"])).Message);
        Assert.ThrowsAny<IOException>(() => CharacterFile.Import<Creature>(temp["missing.json"]));
        Assert.Throws<RecordFormatException>(() => CharacterFile.Export(temp["bad.json"], new Creature { Name = null! }));
        Assert.False(File.Exists(temp["bad.json"]));
    }

    [Fact]
    public void ExportingReplacesTheFileRatherThanRewritingIt()
    {
        // On Linux a reader of the old file keeps reading it: the new one was renamed over it.
        if (OperatingSystem.IsWindows())
            return;
        using var temp = new StorageFolder();
        CharacterFile.Export(temp["hero.json"], new Creature { Name = "Old" });
        var old = File.ReadAllBytes(temp["hero.json"]);
        using var reader = new FileStream(temp["hero.json"], FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        CharacterFile.Export(temp["hero.json"], new Creature { Name = "New" });
        var seen = new byte[old.Length + 100];
        Assert.Equal(old, seen[..reader.Read(seen)]);
        Assert.Equal("New", CharacterFile.Import<Creature>(temp["hero.json"]).Name);
    }

    [Fact]
    public void AFailedExportLeavesNothingBehind()
    {
        using var temp = new StorageFolder();
        Assert.ThrowsAny<IOException>(() => CharacterFile.Export(temp["no/such/folder.json"], StorageTestData.Sample()));
        Directory.CreateDirectory(temp["taken.json"]);
        temp.Write("taken.json/inside", [1]);
        Assert.ThrowsAny<Exception>(() => CharacterFile.Export(temp["taken.json"], StorageTestData.Sample()));
        Assert.Equal(["taken.json/inside"], temp.Files());
    }
}
