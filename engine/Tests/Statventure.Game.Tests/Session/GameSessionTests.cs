using System.Text;
using Statventure.Game.Data.Saves;
using Statventure.Game.Session;
using Statventure.Game.Settings;
using Statventure.Records;
using Statventure.Storage;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The session (Docs/Game/Foundation.md, "The program's flow": New character, Opening a character, Saving, The slot
/// list, The damaged-save question, Save Character and Load Character; Q1, Q2, Q9, Q17, Q21, Q23, Q24, Q25 and M9's
/// Q15–Q17, Q39–Q41) on the engine's storage in scratch folders: slots made from names, opened, damaged in every way
/// Docs/Assets.md's Q17 and Q39 name, answered, deleted, changed and left, character files written and loaded, and
/// <c>lastCharacter</c> kept. (In the "Game log" collection: a test reads the log.)
/// </summary>
[Collection("Game log")]
public class GameSessionTests
{
    /// <summary>A per-user folder of the test's own with the storage, the settings and a session on them, its events recorded.</summary>
    sealed class Player : IDisposable
    {
        readonly ScratchFolder _scratch = new();

        public Player() => Start();

        public GameStorage Storage { get; private set; } = null!;
        public GameSettings Settings { get; private set; } = null!;
        public GameSession Session { get; private set; } = null!;
        public List<string> Events { get; } = [];

        public string this[string relative] => _scratch[relative];

        public string Saves => Storage.Saves.Folder;

        public string SaveFile(string slot, string extension = ".dat") => Path.Combine(Saves, slot + extension);

        /// <summary>The game started (again) on the folder: what is pending written, the settings read, a new session.</summary>
        public void Start()
        {
            Storage?.Dispose();
            Storage = new GameStorage(_scratch["p"]);
            Settings = GameSettings.Load(Storage);
            Session = new GameSession(Storage, Settings);
            Session.Opened += () => Events.Add("opened");
            Session.Closed += () => Events.Add("closed");
            Session.Saved += () => Events.Add("saved");
            Session.SlotsChanged += () => Events.Add("slots");
        }

        /// <summary>A slot's file written as it is, by hand (the engine's writer flushed first).</summary>
        public void Put(string slot, string extension, byte[] bytes)
        {
            Storage.Flush();
            Directory.CreateDirectory(Saves);
            File.WriteAllBytes(SaveFile(slot, extension), bytes);
        }

        public byte[] Bytes(string slot, string extension = ".dat")
        {
            Storage.Flush();
            return File.ReadAllBytes(SaveFile(slot, extension));
        }

        public SaveReading<SaveGame> Read(string slot, string extension = ".dat")
        {
            Storage.Flush();
            return Statventure.Storage.SaveFile.Read<SaveGame>(SaveFile(slot, extension));
        }

        /// <summary>The files in <c>saves/</c>, by name.</summary>
        public string[] Files()
        {
            Storage.Flush();
            return Directory.Exists(Saves) ? [.. Directory.EnumerateFiles(Saves).Select(Path.GetFileName).Order(StringComparer.Ordinal)!] : [];
        }

        public void Dispose()
        {
            Storage.Dispose();
            _scratch.Dispose();
        }
    }

    static byte[] Good(string name) => Statventure.Storage.SaveFile.Write(new SaveGame { Name = name });

    /// <summary>A save file whose payload doesn't match its hash (Q38 of M9: damaged).</summary>
    static byte[] Damaged(string name)
    {
        var file = Good(name);
        file[^1] ^= 0x20;
        return file;
    }

    /// <summary>An intact save file this game can't read: its record a newer version of <c>save.game</c> (unreadable).</summary>
    static byte[] Newer(string name)
    {
        var payload = RecordSerializer.ToBinary(new SaveGame { Name = name });
        // The type table: count 1, "save.game" (length 9), then its version.
        Assert.Equal(1, payload[11]);
        payload[11] = 2;
        return Statventure.Storage.SaveFile.Wrap(payload);
    }

    [Fact]
    public void ANewCharacterGetsASlotNamedFromItsNameSavedAtOnceAndOpened()
    {
        using var player = new Player();
        Assert.Equal("ser-alrik", player.Session.New("Ser Alrik"));
        Assert.Equal(["slots", "opened", "saved"], player.Events);
        Assert.Equal(("ser-alrik", "Ser Alrik", "ser-alrik"), (player.Session.CurrentSlot, player.Session.Current!.Name, player.Settings.LastCharacter));
        Assert.Equal(["ser-alrik.dat"], player.Files());
        Assert.Equal("Ser Alrik", player.Read("ser-alrik").Value.Name);
        // A second one of the same name: a slot of its own; the first left, saved again (its backup now there).
        player.Events.Clear();
        Assert.Equal("ser-alrik-2", player.Session.New("Ser Alrik"));
        Assert.Equal(["slots", "closed", "slots", "opened", "saved"], player.Events);
        Assert.Equal(["ser-alrik-2.dat", "ser-alrik.dat", "ser-alrik.dat.bak"], player.Files());
        Assert.Equal("ser-alrik-2", player.Settings.LastCharacter);
        // The name kept exactly as typed (Q1).
        Assert.Equal("elise-the-ember", player.Session.New("  Élise «the Ember»  "));
        Assert.Equal("  Élise «the Ember»  ", player.Read("elise-the-ember").Value.Name);
    }

    [Theory]
    [InlineData("", "the name is empty")]
    [InlineData("   ", "the name is empty")]
    [InlineData("Ser\tAlrik", "the name holds a control character (U+0009)")]
    public void ANameTheChecksRefuseMakesNothing(string name, string problem)
    {
        using var player = new Player();
        player.Session.New("Ser Alrik");
        player.Events.Clear();
        Assert.Equal(problem, GameSession.NameProblem(name));
        Assert.StartsWith(problem, Assert.Throws<ArgumentException>(() => player.Session.New(name)).Message);
        Assert.Throws<ArgumentException>(() => player.Session.New(new string('a', 81)));
        Assert.Empty(player.Events);
        Assert.Equal("ser-alrik", player.Session.CurrentSlot);
        Assert.Equal(["ser-alrik.dat"], player.Files());
        Assert.Null(GameSession.NameProblem(new string('a', 80)));
    }

    [Fact]
    public void TheNextDefaultNameIsTheNextFreeCharacterN()
    {
        using var player = new Player();
        Assert.Equal("Character 1", player.Session.NextDefaultName());
        player.Session.New(player.Session.NextDefaultName());
        Assert.Equal("Character 2", player.Session.NextDefaultName());
        player.Session.New("Character 3");
        Assert.Equal("Character 2", player.Session.NextDefaultName());
        player.Session.New("Character 2");
        // Neither a name a slot's character has nor a slot that would be taken: "character 4" has another name but
        // character-4's file; a slot of only damaged files is named by its file.
        player.Session.New("character 4");
        player.Put("character-5", ".dat", [1, 2, 3]);
        Assert.Equal("Character 6", player.Session.NextDefaultName());
        // A backup's name counts when the save is damaged.
        player.Session.Leave();
        player.Put("ser-alrik", ".dat", Damaged("x"));
        player.Put("ser-alrik", ".dat.bak", Good("Character 6"));
        Assert.Equal("Character 7", player.Session.NextDefaultName());
        // A character renamed away frees its number for the name, not its slot: "Character 1" lives in character-1.
        player.Session.Open("character-1");
        player.Session.Change(s => s with { Name = "Ser Alrik" });
        Assert.Equal("Character 7", player.Session.NextDefaultName());
    }

    [Fact]
    public void TheSlotsAreListedTheLastPlayedFirstWithTheirNamesAndDamage()
    {
        using var player = new Player();
        player.Session.New("Ser Alrik");
        player.Session.New("Élise");
        player.Session.New("Bram");
        player.Session.Leave();
        player.Put("broken", ".dat", Damaged("Broken"));
        player.Put("broken", ".dat.bak", Good("Broken Bram"));
        player.Put("lost", ".dat", Damaged("Lost"));
        player.Put("lost", ".dat.bak", Newer("Lost"));
        player.Put("only-backup", ".dat.bak", Good("Only Backup"));
        // Not slots: files kept aside, a stale temporary file, a name outside the rule.
        player.Put("gone", ".dat.damaged", Good("Gone"));
        player.Put("gone", ".dat.tmp", Good("Gone"));
        player.Put("Ser Alrik", ".dat", Good("Ser Alrik"));
        var times = new Dictionary<string, DateTime>
        {
            ["ser-alrik"] = new(2026, 11, 13, 14, 3, 0, DateTimeKind.Utc),
            ["elise"] = new(2026, 11, 13, 16, 0, 0, DateTimeKind.Utc),
            ["bram"] = new(2026, 11, 12, 9, 30, 0, DateTimeKind.Utc),
            ["broken"] = new(2026, 11, 13, 14, 3, 0, DateTimeKind.Utc),
            ["lost"] = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            ["only-backup"] = new(2026, 11, 14, 8, 0, 0, DateTimeKind.Utc),
        };
        foreach (var (slot, time) in times)
            foreach (var extension in new[] { ".dat", ".dat.bak" })
                if (File.Exists(player.SaveFile(slot, extension)))
                    File.SetLastWriteTimeUtc(player.SaveFile(slot, extension), time);
        var slots = player.Session.Slots();
        Assert.Equal(
        [
            new SlotInfo("only-backup", "Only Backup", times["only-backup"], true, SlotNameSource.Backup),
            new SlotInfo("elise", "Élise", times["elise"], false, SlotNameSource.Save),
            new SlotInfo("broken", "Broken Bram", times["broken"], true, SlotNameSource.Backup),
            new SlotInfo("ser-alrik", "Ser Alrik", times["ser-alrik"], false, SlotNameSource.Save),
            new SlotInfo("bram", "Bram", times["bram"], false, SlotNameSource.Save),
            new SlotInfo("lost", "lost", times["lost"], true, SlotNameSource.File),
        ], slots);
        Assert.All(slots, s => Assert.Equal(TimeSpan.Zero, s.LastPlayed.Offset));
    }

    [Fact]
    public void OpeningAReadableSlotLeavesTheOpenCharacterAndOpensIt()
    {
        using var player = new Player();
        player.Session.New("Ser Alrik");
        player.Session.New("Élise");
        player.Events.Clear();
        var opened = player.Session.Open("ser-alrik");
        Assert.Equal(new OpenResult("ser-alrik", true, BackupState.Missing, null, "Ser Alrik", SlotNameSource.Save), opened);
        Assert.Equal(["slots", "closed", "opened"], player.Events);
        Assert.Equal(("ser-alrik", "Ser Alrik", "ser-alrik"), (player.Session.CurrentSlot, player.Session.Current!.Name, player.Settings.LastCharacter));
        // The one left was written back.
        Assert.Equal("Élise", player.Read("elise", ".dat.bak").Value.Name);
        // Continue's slot on the next start.
        player.Session.Leave();
        player.Start();
        Assert.Equal(("ser-alrik", "ser-alrik"), (player.Settings.LastCharacter, player.Session.ContinueSlot()));
        Assert.Null(player.Session.Current);
    }

    [Fact]
    public void ADamagedSaveWithAReadableBackupAsksAndUsingTheBackupKeepsTheDamagedFileAside()
    {
        using var player = new Player();
        player.Session.New("Élise");
        player.Put("ser-alrik", ".dat", Damaged("Ser Alrik the Second"));
        player.Put("ser-alrik", ".dat.bak", Good("Ser Alrik"));
        var backupTime = new DateTime(2026, 11, 13, 14, 3, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(player.SaveFile("ser-alrik", ".dat.bak"), backupTime);
        var damaged = player.Bytes("ser-alrik");
        player.Events.Clear();
        // Opening only reports: nothing changes, the open character stays ("Back" is doing nothing more).
        var result = player.Session.Open("ser-alrik");
        Assert.Equal(new OpenResult("ser-alrik", false, BackupState.Readable, backupTime, "Ser Alrik", SlotNameSource.Backup), result);
        Assert.Empty(player.Events);
        Assert.Equal(("elise", "elise"), (player.Session.CurrentSlot, player.Settings.LastCharacter));
        Assert.Equal(["elise.dat", "ser-alrik.dat", "ser-alrik.dat.bak"], player.Files());
        Assert.Equal(damaged, player.Bytes("ser-alrik"));
        // Use the backup: it becomes the save, the damaged save kept aside (Q39), the backup left as it is.
        player.Session.UseBackup("ser-alrik");
        Assert.Equal(["slots", "closed", "slots", "opened", "saved"], player.Events);
        Assert.Equal(("ser-alrik", "Ser Alrik", "ser-alrik"), (player.Session.CurrentSlot, player.Session.Current!.Name, player.Settings.LastCharacter));
        Assert.Equal(["elise.dat", "elise.dat.bak", "ser-alrik.dat", "ser-alrik.dat.bak", "ser-alrik.dat.damaged"], player.Files());
        Assert.Equal("Ser Alrik", player.Read("ser-alrik").Value.Name);
        Assert.Equal("Ser Alrik", player.Read("ser-alrik", ".dat.bak").Value.Name);
        Assert.Equal(damaged, player.Bytes("ser-alrik", ".dat.damaged"));
        Assert.True(player.Session.Open("ser-alrik").Opened);
    }

    [Fact]
    public void ADamagedSaveWithADamagedBackupStartsNewUnderItsFilesName()
    {
        using var player = new Player();
        player.Put("ser-alrik", ".dat", Damaged("Ser Alrik"));
        player.Put("ser-alrik", ".dat.bak", Damaged("Ser Alrik"));
        var (save, backup) = (player.Bytes("ser-alrik"), player.Bytes("ser-alrik", ".dat.bak"));
        var result = player.Session.Open("ser-alrik");
        Assert.Equal((false, BackupState.Damaged, "ser-alrik", SlotNameSource.File), (result.Opened, result.Backup, result.Name, result.NameFrom));
        Assert.NotNull(result.BackupTime);
        Assert.Throws<InvalidOperationException>(() => player.Session.UseBackup("ser-alrik"));
        Assert.Equal(save, player.Bytes("ser-alrik"));
        // Start new (Q21): a new character in this slot; its name the slot's file name, neither file being readable (Q25).
        player.Session.StartNew("ser-alrik");
        Assert.Equal(["slots", "opened", "saved"], player.Events);
        Assert.Equal(("ser-alrik", "ser-alrik", "ser-alrik"), (player.Session.CurrentSlot, player.Session.Current!.Name, player.Settings.LastCharacter));
        Assert.Equal("ser-alrik", player.Read("ser-alrik").Value.Name);
        Assert.Equal(save, player.Bytes("ser-alrik", ".dat.damaged"));
        Assert.Equal(backup, player.Bytes("ser-alrik", ".dat.bak"));
    }

    [Fact]
    public void ADamagedSaveWithNoBackupStartsNew()
    {
        using var player = new Player();
        player.Put("bram", ".dat", [(byte)'S', (byte)'V']);
        player.Session.New("Élise");
        player.Events.Clear();
        Assert.Equal(new OpenResult("bram", false, BackupState.Missing, null, "bram", SlotNameSource.File), player.Session.Open("bram"));
        // No backup to use: refused before anything changes (the open character stays open, unsaved).
        Assert.Throws<InvalidOperationException>(() => player.Session.UseBackup("bram"));
        Assert.Equal("elise", player.Session.CurrentSlot);
        Assert.Empty(player.Events);
        player.Session.Leave();
        player.Session.StartNew("bram");
        Assert.Equal("bram", player.Read("bram").Value.Name);
        Assert.Equal(["bram.dat", "bram.dat.damaged", "elise.dat", "elise.dat.bak"], player.Files());
        // Damaged again later: kept aside beside the first, never over it.
        player.Session.Leave();
        player.Put("bram", ".dat", Damaged("bram"));
        player.Session.StartNew("bram");
        Assert.Equal(["bram.dat", "bram.dat.bak", "bram.dat.damaged", "bram.dat.damaged-2", "elise.dat", "elise.dat.bak"], player.Files());
    }

    [Fact]
    public void AnUnreadableSaveWithAReadableBackupStartsNewUnderTheBackupsName()
    {
        using var player = new Player();
        player.Put("ser-alrik", ".dat", Newer("Ser Alrik of a newer game"));
        player.Put("ser-alrik", ".dat.bak", Good("Ser Alrik"));
        var newer = player.Bytes("ser-alrik");
        var result = player.Session.Open("ser-alrik");
        Assert.Equal((false, BackupState.Readable, "Ser Alrik", SlotNameSource.Backup), (result.Opened, result.Backup, result.Name, result.NameFrom));
        // Start new keeps the name (Q21): the backup's.
        player.Session.StartNew("ser-alrik");
        Assert.Equal("Ser Alrik", player.Session.Current!.Name);
        Assert.Equal(new SaveGame { Name = "Ser Alrik" }, player.Read("ser-alrik").Value);
        Assert.Equal(newer, player.Bytes("ser-alrik", ".dat.damaged"));
    }

    [Fact]
    public void AnUnreadableBackupIsADamagedOne()
    {
        using var player = new Player();
        player.Put("ser-alrik", ".dat", Damaged("Ser Alrik"));
        var newerFormat = Good("Ser Alrik");
        newerFormat[4] = 2;
        player.Put("ser-alrik", ".dat.bak", newerFormat);
        var result = player.Session.Open("ser-alrik");
        Assert.Equal((BackupState.Damaged, "ser-alrik", SlotNameSource.File), (result.Backup, result.Name, result.NameFrom));
        Assert.Throws<InvalidOperationException>(() => player.Session.UseBackup("ser-alrik"));
    }

    [Fact]
    public void AReadableSaveWithADamagedBackupOpensAndItsNextWriteKeepsTheBackupAside()
    {
        using var player = new Player();
        player.Put("ser-alrik", ".dat", Good("Ser Alrik"));
        player.Put("ser-alrik", ".dat.bak", Damaged("Ser Alrik"));
        var backup = player.Bytes("ser-alrik", ".dat.bak");
        // Only the save is read: the slot isn't damaged and opens.
        Assert.Equal((false, "Ser Alrik"), (player.Session.Slots().Single().Damaged, player.Session.Slots().Single().Name));
        Assert.True(player.Session.Open("ser-alrik").Opened);
        // The next write keeps the damaged backup aside (Q39 of M9) and the save before it becomes the backup.
        player.Session.Change(s => s with { Name = "Ser Alrik the Bold" });
        Assert.Equal(["ser-alrik.dat", "ser-alrik.dat.bak", "ser-alrik.dat.bak.damaged"], player.Files());
        Assert.Equal(backup, player.Bytes("ser-alrik", ".dat.bak.damaged"));
        Assert.Equal("Ser Alrik", player.Read("ser-alrik", ".dat.bak").Value.Name);
        Assert.Equal("Ser Alrik the Bold", player.Read("ser-alrik").Value.Name);
    }

    [Fact]
    public void ASaveTheSystemWontLetBeReadIsUnreadable()
    {
        if (!OperatingSystem.IsLinux())
            return;
        using var player = new Player();
        player.Put("ser-alrik", ".dat", Good("Ser Alrik the Bold"));
        player.Put("ser-alrik", ".dat.bak", Good("Ser Alrik"));
        var save = player.SaveFile("ser-alrik");
        File.SetUnixFileMode(save, UnixFileMode.None);
        try
        {
            // (Root reads any file.)
            if (Environment.UserName == "root")
                return;
            var result = player.Session.Open("ser-alrik");
            Assert.Equal((false, BackupState.Readable, "Ser Alrik", SlotNameSource.Backup), (result.Opened, result.Backup, result.Name, result.NameFrom));
            Assert.Equal((true, "Ser Alrik"), (player.Session.Slots().Single().Damaged, player.Session.Slots().Single().Name));
            Assert.Null(player.Session.Current);
        }
        finally
        {
            File.SetUnixFileMode(save, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    [Fact]
    public void ASaveMissingBesideItsBackupIsDamagedAndItsBackupCanBeUsed()
    {
        using var player = new Player();
        player.Put("ser-alrik", ".dat.bak", Good("Ser Alrik"));
        Assert.Equal(new SlotInfo("ser-alrik", "Ser Alrik", player.Session.Slots()[0].LastPlayed, true, SlotNameSource.Backup), player.Session.Slots().Single());
        var result = player.Session.Open("ser-alrik");
        Assert.Equal((false, BackupState.Readable, "Ser Alrik"), (result.Opened, result.Backup, result.Name));
        player.Session.UseBackup("ser-alrik");
        Assert.Equal(["ser-alrik.dat", "ser-alrik.dat.bak"], player.Files());
        Assert.Equal("Ser Alrik", player.Read("ser-alrik").Value.Name);
    }

    [Fact]
    public void AStaleTemporaryFileIsNeverReadAndTheNextSaveGoesOverIt()
    {
        using var player = new Player();
        player.Session.New("Ser Alrik");
        player.Session.Leave();
        player.Put("ser-alrik", ".dat.tmp", [1, 2, 3]);
        Assert.Equal(new SlotInfo("ser-alrik", "Ser Alrik", player.Session.Slots()[0].LastPlayed, false), player.Session.Slots().Single());
        Assert.True(player.Session.Open("ser-alrik").Opened);
        player.Session.Change(s => s with { Name = "Ser Alrik the Bold" });
        Assert.Equal(["ser-alrik.dat", "ser-alrik.dat.bak"], player.Files());
    }

    [Fact]
    public void TheDamagedSaveQuestionsAnswersNeedASlotThatDoesntRead()
    {
        using var player = new Player();
        player.Session.New("Ser Alrik");
        player.Session.Change(s => s with { Name = "Ser Alrik the Bold" });
        player.Events.Clear();
        Assert.Throws<InvalidOperationException>(() => player.Session.UseBackup("ser-alrik"));
        Assert.Throws<InvalidOperationException>(() => player.Session.StartNew("ser-alrik"));
        Assert.Empty(player.Events);
        Assert.Equal("Ser Alrik the Bold", player.Read("ser-alrik").Value.Name);
    }

    [Fact]
    public void StartingNewUnderALongFilesNameCutsItToANamesLength()
    {
        using var player = new Player();
        var slot = new string('x', 90) + "-y-zzzzzz";
        player.Put(slot, ".dat", Damaged("x"));
        player.Session.StartNew(slot);
        Assert.Equal(new string('x', 80), player.Session.Current!.Name);
        Assert.Null(SaveGame.NameProblem(player.Session.Current.Name));
        var dashed = new string('w', 79) + "-tail";
        player.Put(dashed, ".dat", Damaged("x"));
        player.Session.StartNew(dashed);
        Assert.Equal(new string('w', 79), player.Session.Current!.Name);
    }

    [Fact]
    public void DeletingASlotRemovesItsSaveAndBackupAndClosesItIfOpen()
    {
        using var player = new Player();
        player.Session.New("Élise");
        player.Session.New("Ser Alrik");
        player.Session.Change(s => s with { Name = "Ser Alrik the Bold" });
        player.Put("ser-alrik", ".dat.damaged", Good("kept aside"));
        player.Put("ser-alrik", ".dat.tmp", [1]);
        player.Events.Clear();
        player.Session.Delete("ser-alrik");
        Assert.Equal(["closed", "slots"], player.Events);
        Assert.Null(player.Session.Current);
        Assert.Null(player.Session.CurrentSlot);
        Assert.Null(player.Settings.LastCharacter);
        Assert.Null(player.Session.ContinueSlot());
        // Its save and backup and a stale temporary file go; files kept aside stay; it isn't written back.
        player.Session.Leave();
        Assert.Equal(["elise.dat", "elise.dat.bak", "ser-alrik.dat.damaged"], player.Files());
        // A slot not open: the open one stays.
        player.Session.Open("elise");
        player.Session.New("Bram");
        player.Events.Clear();
        player.Session.Delete("elise");
        Assert.Equal(["slots"], player.Events);
        Assert.Equal(("bram", "bram"), (player.Session.CurrentSlot, player.Settings.LastCharacter));
        Assert.Equal(["bram.dat", "ser-alrik.dat.damaged"], player.Files());
    }

    [Fact]
    public void ALastCharacterNamingNoSlotIsNoneEvenWhenTheFileKeepsIt()
    {
        using var player = new Player();
        player.Session.New("Ser Alrik");
        player.Session.Delete("ser-alrik");
        // The file keeps the deleted slot's name (nothing written for a cleared key); a later change of another
        // setting writes the file with it still there.
        player.Settings.Theme = GameTheme.Light;
        player.Storage.Flush();
        Assert.Contains("\"lastCharacter\": \"ser-alrik\"", File.ReadAllText(player.Storage.SettingsFile));
        // The next start: the settings read it, the session takes it as none; Continue has nothing.
        player.Start();
        Assert.Null(player.Settings.LastCharacter);
        Assert.Null(player.Session.ContinueSlot());
        Assert.Empty(player.Session.Slots());
        // Harmless when a slot of that name is made again: it is the last character then.
        player.Session.New("Ser Alrik");
        player.Session.Leave();
        player.Start();
        Assert.Equal(("ser-alrik", "ser-alrik"), (player.Settings.LastCharacter, player.Session.ContinueSlot()));
    }

    [Fact]
    public void ContinuesSlotIsTheLastCharactersWhileItIsThere()
    {
        using var player = new Player();
        Assert.Null(player.Session.ContinueSlot());
        player.Session.New("Ser Alrik");
        Assert.Equal("ser-alrik", player.Session.ContinueSlot());
        // Its files taken away by hand while the game runs: nothing to continue.
        player.Session.Leave();
        player.Storage.Flush();
        File.Delete(player.SaveFile("ser-alrik"));
        File.Delete(player.SaveFile("ser-alrik", ".dat.bak"));
        Assert.Null(player.Session.ContinueSlot());
        // Only its backup there: it is still a slot (opening it asks the damaged-save question).
        player.Put("ser-alrik", ".dat.bak", Good("Ser Alrik"));
        Assert.Equal("ser-alrik", player.Session.ContinueSlot());
    }

    [Fact]
    public void AChangeSavesTheOpenCharacter()
    {
        using var player = new Player();
        Assert.Throws<InvalidOperationException>(() => player.Session.Change(s => s));
        player.Session.New("Ser Alrik");
        player.Events.Clear();
        player.Session.Change(s => s with { Name = "Ser Alrik the Bold" });
        Assert.Equal(["slots", "saved"], player.Events);
        Assert.Equal("Ser Alrik the Bold", player.Session.Current!.Name);
        Assert.Equal("Ser Alrik the Bold", player.Read("ser-alrik").Value.Name);
        Assert.Equal("Ser Alrik", player.Read("ser-alrik", ".dat.bak").Value.Name);
        // A change its checks refuse changes nothing.
        player.Events.Clear();
        Assert.Equal("the name is empty", Assert.Throws<ArgumentException>(() => player.Session.Change(s => s with { Name = " " })).Message.Split(" (Parameter")[0]);
        Assert.Empty(player.Events);
        Assert.Equal("Ser Alrik the Bold", player.Session.Current!.Name);
        Assert.Equal("Ser Alrik the Bold", player.Read("ser-alrik").Value.Name);
    }

    [Fact]
    public void LeavingSavesAndClosesTheOpenCharacter()
    {
        using var player = new Player();
        player.Session.Leave();
        Assert.Empty(player.Events);
        player.Session.New("Ser Alrik");
        player.Events.Clear();
        player.Session.Leave();
        Assert.Equal(["slots", "closed"], player.Events);
        Assert.Null(player.Session.Current);
        Assert.Equal(["ser-alrik.dat", "ser-alrik.dat.bak"], player.Files());
        player.Events.Clear();
        player.Session.Leave();
        Assert.Empty(player.Events);
        // The last character stays Continue's.
        Assert.Equal("ser-alrik", player.Session.ContinueSlot());
    }

    [Fact]
    public void SaveCharacterWritesTheSavesJsonForm()
    {
        using var player = new Player();
        var file = player["documents/stat-fick-character.json"];
        Directory.CreateDirectory(player["documents"]);
        Assert.False(player.Session.Export(file));
        Assert.False(File.Exists(file));
        player.Session.New("Ser Alrik");
        Assert.True(player.Session.Export(file));
        Assert.Equal("{\n  \"$types\": {\n    \"save.game\": 1\n  },\n  \"name\": \"Ser Alrik\"\n}\n", File.ReadAllText(file));
        // An existing file replaced.
        player.Session.Change(s => s with { Name = "Ser Alrik the Bold" });
        Assert.True(player.Session.Export(file));
        Assert.Contains("\"Ser Alrik the Bold\"", File.ReadAllText(file));
        // A folder that isn't there: not written (the status line's "Character could not be saved").
        var log = GameTesting.CaptureLog(() => Assert.False(player.Session.Export(player["no-such-folder/x.json"])));
        Assert.Contains(log, l => l.StartsWith("Warning [Session] ser-alrik couldn't be saved to ", StringComparison.Ordinal));
        Assert.Equal(["stat-fick-character.json"], Directory.EnumerateFiles(player["documents"]).Select(Path.GetFileName));
    }

    [Fact]
    public void TheDocumentsFolderIsTheTestRunsOwn()
    {
        // Step 10's Save Character starts in the engine's Documents folder; in the tests it is the run's scratch folder,
        // never the player's real one.
        var documents = DocumentsFolder.ForThisMachine();
        Assert.StartsWith(PerUserFolders.Root + Path.DirectorySeparatorChar, documents);
        using var player = new Player();
        player.Session.New("Ser Alrik");
        var file = Path.Combine(documents, CharacterFile.DefaultName);
        Assert.True(player.Session.Export(file));
        File.Delete(file);
    }

    [Fact]
    public void LoadCharacterMakesANewSlotUnderTheNameItHoldsAndOpensIt()
    {
        using var player = new Player();
        player.Session.New("Ser Alrik");
        var file = player["stat-fick-character.json"];
        Assert.True(player.Session.Export(file));
        player.Session.New("Élise");
        player.Events.Clear();
        // Q9: a new slot, the name taken so its slot gets a number; the one open before left, saved.
        Assert.Equal(new ImportResult("ser-alrik-2", null), player.Session.Import(file));
        Assert.Equal(["slots", "closed", "slots", "opened", "saved"], player.Events);
        Assert.Equal(("ser-alrik-2", "Ser Alrik", "ser-alrik-2"), (player.Session.CurrentSlot, player.Session.Current!.Name, player.Settings.LastCharacter));
        Assert.Equal(new SaveGame { Name = "Ser Alrik" }, player.Read("ser-alrik-2").Value);
        Assert.Equal("Élise", player.Read("elise", ".dat.bak").Value.Name);
        // A name that isn't taken keeps its stem; the file is read as it is (with a byte order mark too).
        File.WriteAllText(player["bram.json"], "﻿{ \"$types\": { \"save.game\": 1 }, \"name\": \"Bram\" }");
        Assert.Equal(new ImportResult("bram", null), player.Session.Import(player["bram.json"]));
        Assert.Equal(["bram", "elise", "ser-alrik", "ser-alrik-2"], player.Session.Slots().Select(s => s.Slot).Order());
    }

    /// <summary>Files that aren't characters: each gets the old general words (Q23).</summary>
    public static TheoryData<string, byte[]> WrongFiles => new()
    {
        { "not JSON", "Ser Alrik"u8.ToArray() },
        { "empty", [] },
        { "JSON, not a record", "{ \"name\": \"Ser Alrik\" }"u8.ToArray() },
        { "an old game's character file", Encoding.UTF8.GetBytes("""
            {
              "format": "emberfall-stat-block",
              "version": 22,
              "exportedAt": "2026-11-01T12:00:00.000Z",
              "character": { "stats": { "strength": 5 }, "race": "human", "editingEnabled": true, "darkMode": false }
            }
            """) },
        { "another kind", "{ \"$types\": { \"mod.info\": 1 }, \"name\": \"Ser Alrik\", \"author\": \"\", \"version\": \"\", \"description\": \"\" }"u8.ToArray() },
        { "a newer save", "{ \"$types\": { \"save.game\": 2 }, \"name\": \"Ser Alrik\" }"u8.ToArray() },
        { "an unknown key", "{ \"$types\": { \"save.game\": 1 }, \"name\": \"Ser Alrik\", \"race\": \"elf\" }"u8.ToArray() },
        { "a wrong type", "{ \"$types\": { \"save.game\": 1 }, \"name\": 7 }"u8.ToArray() },
        { "a slot's binary save", Good("Ser Alrik") },
        { "an array", "[ 1, 2 ]"u8.ToArray() },
    };

    [Theory]
    [MemberData(nameof(WrongFiles))]
    public void AFileThatIsntACharacterGivesTheOldWordsAndChangesNothing(string what, byte[] content)
    {
        using var player = new Player();
        player.Session.New("Élise");
        player.Events.Clear();
        var file = player["file.json"];
        File.WriteAllBytes(file, content);
        Assert.Equal(new ImportResult(null, "That file is not a valid character save"), player.Session.Import(file));
        Assert.Equal(GameSession.NotACharacterSave, player.Session.Import(file).Problem);
        Assert.Empty(player.Events);
        Assert.Equal("elise", player.Session.CurrentSlot);
        Assert.Equal(["elise.dat"], player.Files());
        Assert.NotNull(what);
    }

    [Fact]
    public void AFileThatCantBeReadGivesTheOldWords()
    {
        using var player = new Player();
        Assert.Equal(GameSession.NotACharacterSave, player.Session.Import(player["missing.json"]).Problem);
        Directory.CreateDirectory(player["a-folder.json"]);
        Assert.Equal(GameSession.NotACharacterSave, player.Session.Import(player["a-folder.json"]).Problem);
        // Larger than the old game's 10 MiB.
        using (var big = File.Create(player["big.json"]))
            big.SetLength(CharacterFile.MaxBytes + 1);
        Assert.Equal(GameSession.NotACharacterSave, player.Session.Import(player["big.json"]).Problem);
        Assert.Empty(player.Files());
        Assert.Empty(player.Events);
    }

    [Theory]
    [InlineData("{ \"$types\": { \"save.game\": 1 }, \"name\": \"\" }", "the name is empty")]
    [InlineData("{ \"$types\": { \"save.game\": 1 } }", "the name is empty")]
    [InlineData("{ \"$types\": { \"save.game\": 1 }, \"name\": \"Ser\\u0007Alrik\" }", "the name holds a control character (U+0007)")]
    public void ACharacterFileItsChecksRefuseNamesTheProblem(string json, string problem)
    {
        using var player = new Player();
        File.WriteAllText(player["file.json"], json);
        Assert.Equal(new ImportResult(null, $"That file is not a valid character save: {problem}"), player.Session.Import(player["file.json"]));
        File.WriteAllText(player["long.json"], $"{{ \"$types\": {{ \"save.game\": 1 }}, \"name\": \"{new string('a', 81)}\" }}");
        Assert.Equal("That file is not a valid character save: the name has 81 characters, more than 80", player.Session.Import(player["long.json"]).Problem);
        Assert.Empty(player.Files());
        Assert.Null(player.Session.Current);
    }
}
