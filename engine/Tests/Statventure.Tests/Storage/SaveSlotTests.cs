using Statventure.Records;
using Statventure.Storage;
using Statventure.Tests.Records;

namespace Statventure.Tests.Storage;

/// <summary>
/// The save slots (Docs/Assets.md, "Save slots", "When a save is written", "A damaged or unreadable
/// save"; Q15–Q17, Q39–Q41): names, saving and loading through the background writer (the latest state
/// wins, flushed on request), the backup, damage reported and kept aside on the next write, interrupted
/// writes, and deleting.
/// </summary>
public class SaveSlotTests
{
    static Creature Named(string name, double speed = 1) => new() { Name = name, Speed = speed };

    static byte[] Bytes(StorageFolder temp, string relative) => File.ReadAllBytes(temp[relative]);

    /// <summary>Saves and waits until it is written (two saves in a row may write only the second: the latest state wins).</summary>
    static void SaveNow(GameStorage storage, string name, Creature value)
    {
        storage.Saves.Save(name, value);
        storage.Flush();
    }

    [Theory]
    [InlineData("hero")]
    [InlineData("slot-1")]
    [InlineData("the_second.save")]
    [InlineData("0")]
    [InlineData("a.b.c")]
    public void ValidNames(string name)
    {
        Assert.Null(SaveSlots.NameProblem(name));
        Assert.True(SaveSlots.IsValidName(name));
    }

    [Theory]
    [InlineData("", "empty")]
    [InlineData("Hero", "'H' isn't allowed")]
    [InlineData("my hero", "' ' isn't allowed")]
    [InlineData("a/b", "can't hold '/'")]
    [InlineData("..", "ends in a dot")]
    [InlineData("hero.", "ends in a dot")]
    [InlineData("con", "Windows keeps for a device")]
    [InlineData("lpt1.x", "Windows keeps for a device")]
    [InlineData("héros", "isn't allowed")]
    public void InvalidNames(string name, string problem)
    {
        Assert.Contains(problem, SaveSlots.NameProblem(name));
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        Assert.Throws<ArgumentException>(() => storage.Saves.Save(name, Named("x")));
        Assert.Throws<ArgumentException>(() => storage.Saves.Load<Creature>(name));
        Assert.Throws<ArgumentException>(() => storage.Saves.Delete(name));
        Assert.Throws<ArgumentException>(() => storage.Saves.Exists(name));
        Assert.Throws<ArgumentException>(() => storage.Saves.SavePath(name));
        Assert.Throws<ArgumentException>(() => storage.Saves.BackupPath(name));
        storage.Flush();
        Assert.False(Directory.Exists(storage.Saves.Folder));
    }

    [Fact]
    public void NamesHaveAtMostAHundredCharacters()
    {
        Assert.Equal(100, SaveSlots.MaxNameLength);
        var longest = new string('a', 100);
        Assert.Null(SaveSlots.NameProblem(longest));
        Assert.Contains("at most 100 characters", SaveSlots.NameProblem(longest + "a"));
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        SaveNow(storage, longest, Named("x"));
        SaveNow(storage, longest, Named("y"));
        storage.Flush();
        Assert.Equal([$"saves/{longest}.dat", $"saves/{longest}.dat.bak"], temp.Files());
    }

    [Fact]
    public void ASaveIsReadBackAndThePreviousOneIsItsBackup()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        var saves = storage.Saves;
        Assert.False(saves.Exists("hero"));
        saves.Save("hero", Named("first"));
        storage.Flush();
        Assert.Equal(["saves/hero.dat"], temp.Files());
        Assert.Equal(SaveFile.Write(Named("first")), Bytes(temp, "saves/hero.dat"));
        Assert.True(saves.Exists("hero"));
        var reading = saves.Load<Creature>("hero");
        Assert.True(reading.Loaded);
        Assert.Equal("hero", reading.Name);
        Assert.Null(reading.Backup);
        RecordAssert.Equal(Named("first"), reading.Value);

        saves.Save("hero", Named("second"));
        storage.Flush();
        Assert.Equal(["saves/hero.dat", "saves/hero.dat.bak"], temp.Files());
        Assert.Equal(SaveFile.Write(Named("second")), Bytes(temp, "saves/hero.dat"));
        Assert.Equal(SaveFile.Write(Named("first")), Bytes(temp, "saves/hero.dat.bak"));
        saves.Save("hero", Named("third"));
        storage.Flush();
        Assert.Equal(SaveFile.Write(Named("third")), Bytes(temp, "saves/hero.dat"));
        Assert.Equal(SaveFile.Write(Named("second")), Bytes(temp, "saves/hero.dat.bak"));
        RecordAssert.Equal(Named("third"), saves.Load<Creature>("hero").Value);
        Assert.Equal(storage.Saves.SavePath("hero"), temp["saves/hero.dat"]);
        Assert.Equal(storage.Saves.BackupPath("hero"), temp["saves/hero.dat.bak"]);
    }

    [Fact]
    public void ASlotThatWasNeverSavedHasNothing()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        var reading = storage.Saves.Load<Creature>("nobody");
        Assert.False(reading.Loaded);
        Assert.Equal(SaveState.Missing, reading.Save.State);
        Assert.Equal(SaveState.Missing, reading.Backup!.State);
        Assert.Throws<InvalidOperationException>(() => reading.Value);
    }

    [Fact]
    public void AValueItsDeclarationRefusesThrowsOnTheCallersThread()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        Assert.Throws<RecordFormatException>(() => storage.Saves.Save("hero", new Creature { Name = null! }));
        storage.Flush();
        Assert.Empty(temp.Files());
    }

    [Fact]
    public void TheSlotsAreWhatTheFolderHolds()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        foreach (var name in new[] { "zed", "alpha", "b-2", "b_1", "b.0" })
            SaveNow(storage, name, Named(name));
        SaveNow(storage, "alpha", Named("again"));
        storage.Flush();
        // A slot left with only its backup is a slot; other files aren't.
        File.Move(temp["saves/zed.dat"], temp["saves/only-backup.dat.bak"]);
        foreach (var other in new[] { "Upper.dat", "con.dat", "x.dat.tmp", "y.dat.damaged", "y.dat.bak.damaged", "notes.txt", "dat", ".dat", "z.DAT" })
            temp.Write("saves/" + other, [1, 2, 3]);
        Directory.CreateDirectory(temp["saves/folder.dat.d"]);
        temp.Write("saves/inner/deep.dat", SaveFile.Write(Named("deep")));
        var slots = storage.Saves.List();
        Assert.Equal(["alpha", "b-2", "b.0", "b_1", "only-backup"], slots.Select(s => s.Name));
        Assert.Equal([true, true, true, true, false], slots.Select(s => s.HasSave));
        Assert.Equal([true, false, false, false, true], slots.Select(s => s.HasBackup));
        Assert.Equal(File.GetLastWriteTimeUtc(temp["saves/alpha.dat"]), slots[0].LastWrittenUtc);
        Assert.Equal(File.GetLastWriteTimeUtc(temp["saves/only-backup.dat.bak"]), slots[4].LastWrittenUtc);
        Assert.Equal(DateTimeKind.Utc, slots[0].LastWrittenUtc.Kind);
    }

    [Fact]
    public async Task ListingAndLoadingWaitForPendingSaves()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        using var held = new HeldWriter(storage);
        storage.Saves.Save("hero", Named("waited for"));
        held.WaitUntilHeld();
        var load = Task.Run(() => storage.Saves.Load<Creature>("hero"));
        var list = Task.Run(() => storage.Saves.List());
        var exists = Task.Run(() => storage.Saves.Exists("hero"));
        Thread.Sleep(200);
        Assert.False(load.IsCompleted || list.IsCompleted || exists.IsCompleted);
        held.Release();
        Assert.Equal("waited for", (await load).Value.Name);
        Assert.Equal(["hero"], (await list).Select(s => s.Name));
        Assert.True(await exists);
    }

    [Fact]
    public void TheLatestStateWins()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        var written = new List<string>();
        using var held = new HeldWriter(storage, seen: file =>
        {
            lock (written)
                written.Add(Path.GetFileName(file));
        });
        storage.Saves.Save("hero", Named("1"));
        held.WaitUntilHeld();
        // While the first is written, three more come: only the last of them is written after it.
        storage.Saves.Save("hero", Named("2"));
        storage.Saves.Save("other", Named("a"));
        storage.Saves.Save("hero", Named("3"));
        storage.Saves.Save("hero", Named("4"));
        held.Release();
        storage.Flush();
        Assert.Equal(["hero.dat", "hero.dat", "other.dat"], written);
        Assert.Equal("4", storage.Saves.Load<Creature>("hero").Value.Name);
        Assert.Equal(SaveFile.Write(Named("1")), Bytes(temp, "saves/hero.dat.bak"));
        Assert.Equal("a", storage.Saves.Load<Creature>("other").Value.Name);
    }

    [Fact]
    public void TheWriterIsAForegroundThreadThatEndsWhenIdle()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        bool? background = null;
        storage.Writer.BeforeWork = _ => background = Thread.CurrentThread.IsBackground;
        Assert.False(storage.Writer.IsRunning);
        storage.Saves.Save("hero", Named("x"));
        storage.Flush();
        Assert.False(background);
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (storage.Writer.IsRunning && DateTime.UtcNow < deadline)
            Thread.Sleep(1);
        Assert.False(storage.Writer.IsRunning);
        // It starts again for the next save.
        storage.Saves.Save("hero", Named("y"));
        storage.Flush();
        Assert.Equal("y", storage.Saves.Load<Creature>("hero").Value.Name);
    }

    [Fact]
    public async Task DisposingFlushesAndThenRefusesSaves()
    {
        using var temp = new StorageFolder();
        var storage = new GameStorage(temp.Root);
        using var held = new HeldWriter(storage);
        storage.Saves.Save("hero", Named("last"));
        held.WaitUntilHeld();
        var disposed = Task.Run(storage.Dispose);
        Thread.Sleep(100);
        Assert.False(disposed.IsCompleted);
        held.Release();
        await disposed;
        Assert.Equal(SaveFile.Write(Named("last")), Bytes(temp, "saves/hero.dat"));
        Assert.Throws<ObjectDisposedException>(() => storage.Saves.Save("hero", Named("late")));
    }

    [Fact]
    public void AFailedWriteIsReportedAndTheNextRequestTriesAgain()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        var failures = new List<(string, Exception)>();
        storage.WriteFailed += (file, error) =>
        {
            lock (failures)
                failures.Add((file, error));
        };
        // A file where the saves folder should be.
        temp.Write("saves", [0]);
        storage.Saves.Save("hero", Named("lost"));
        storage.Flush();
        var (file, error) = Assert.Single(failures);
        Assert.Equal(temp["saves/hero.dat"], file);
        Assert.IsAssignableFrom<IOException>(error);
        File.Delete(temp["saves"]);
        storage.Saves.Save("hero", Named("kept"));
        storage.Flush();
        Assert.Single(failures);
        Assert.Equal("kept", storage.Saves.Load<Creature>("hero").Value.Name);
    }

    [Fact]
    public void ADamagedSaveLoadsNothingAndReportsTheBackup()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        SaveNow(storage, "hero", Named("old"));
        SaveNow(storage, "hero", Named("new"));
        storage.Flush();
        var damaged = Bytes(temp, "saves/hero.dat");
        damaged[^1] ^= 0x10;
        File.WriteAllBytes(temp["saves/hero.dat"], damaged);
        var files = temp.Files();

        var reading = storage.Saves.Load<Creature>("hero");
        Assert.False(reading.Loaded);
        Assert.Equal(SaveState.Damaged, reading.Save.State);
        Assert.Contains("SHA-256", reading.Save.Problem);
        Assert.Throws<InvalidOperationException>(() => reading.Value);
        Assert.Equal(SaveState.Readable, reading.Backup!.State);
        Assert.Equal("old", reading.Backup.Value.Name);
        // Loading changes no file.
        Assert.Equal(files, temp.Files());
        Assert.Equal(damaged, Bytes(temp, "saves/hero.dat"));

        // The backup's state when it is damaged or missing too.
        File.WriteAllBytes(temp["saves/hero.dat.bak"], [1, 2, 3]);
        Assert.Equal(SaveState.Damaged, storage.Saves.Load<Creature>("hero").Backup!.State);
        File.Delete(temp["saves/hero.dat.bak"]);
        Assert.Equal(SaveState.Missing, storage.Saves.Load<Creature>("hero").Backup!.State);
    }

    [Fact]
    public void UsingTheBackupKeepsTheDamagedSaveAside()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        SaveNow(storage, "hero", Named("old"));
        SaveNow(storage, "hero", Named("new"));
        storage.Flush();
        File.WriteAllBytes(temp["saves/hero.dat"], [9, 9, 9]);
        var reading = storage.Saves.Load<Creature>("hero");
        // The player chose the backup: the game saves it into the slot.
        SaveNow(storage, "hero", reading.Backup!.Value);
        storage.Flush();
        Assert.Equal(["saves/hero.dat", "saves/hero.dat.bak", "saves/hero.dat.damaged"], temp.Files());
        Assert.Equal([9, 9, 9], Bytes(temp, "saves/hero.dat.damaged"));
        Assert.Equal(SaveFile.Write(Named("old")), Bytes(temp, "saves/hero.dat"));
        Assert.Equal(SaveFile.Write(Named("old")), Bytes(temp, "saves/hero.dat.bak"));
        Assert.True(storage.Saves.Load<Creature>("hero").Loaded);
    }

    [Fact]
    public void DamagedFilesAreKeptAsideAndNeverOverwritten()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        SaveNow(storage, "hero", Named("good"));
        SaveNow(storage, "hero", Named("better"));
        storage.Flush();
        // Each time the save is damaged, starting new keeps it under the next free name; the backup stays.
        for (var n = 1; n <= 3; n++)
        {
            File.WriteAllBytes(temp["saves/hero.dat"], [(byte)n]);
            SaveNow(storage, "hero", Named($"new {n}"));
            storage.Flush();
            Assert.Equal(SaveFile.Write(Named("good")), Bytes(temp, "saves/hero.dat.bak"));
            Assert.Equal($"new {n}", storage.Saves.Load<Creature>("hero").Value.Name);
        }
        Assert.Equal([1], Bytes(temp, "saves/hero.dat.damaged"));
        Assert.Equal([2], Bytes(temp, "saves/hero.dat.damaged-2"));
        Assert.Equal([3], Bytes(temp, "saves/hero.dat.damaged-3"));
        // A name something else has (a folder) is skipped too.
        Directory.CreateDirectory(temp["saves/hero.dat.damaged-4"]);
        File.WriteAllBytes(temp["saves/hero.dat"], [4]);
        SaveNow(storage, "hero", Named("new 4"));
        storage.Flush();
        Assert.Equal([4], Bytes(temp, "saves/hero.dat.damaged-5"));
        Assert.Equal(["hero"], storage.Saves.List().Select(s => s.Name));
    }

    [Fact]
    public void ASaveThisEngineCantReadIsKeptAsideToo()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        SaveNow(storage, "hero", Named("backup"));
        SaveNow(storage, "hero", Named("x"));
        storage.Flush();
        // Intact, but of another record type: unreadable, not damaged.
        var other = SaveFile.Write(new Part { Name = "horn" });
        File.WriteAllBytes(temp["saves/hero.dat"], other);
        Assert.Equal(SaveState.Unreadable, storage.Saves.Load<Creature>("hero").Save.State);
        SaveNow(storage, "hero", Named("new"));
        storage.Flush();
        Assert.Equal(other, Bytes(temp, "saves/hero.dat.damaged"));
        Assert.Equal(SaveFile.Write(Named("backup")), Bytes(temp, "saves/hero.dat.bak"));
        // A newer file format alike.
        var newer = SaveFile.Write(Named("future"));
        newer[4] = 2;
        File.WriteAllBytes(temp["saves/hero.dat"], newer);
        SaveNow(storage, "hero", Named("newer"));
        storage.Flush();
        Assert.Equal(newer, Bytes(temp, "saves/hero.dat.damaged-2"));
        Assert.Equal(SaveFile.Write(Named("backup")), Bytes(temp, "saves/hero.dat.bak"));
    }

    [Fact]
    public void ADamagedBackupIsKeptAsideWhenAWriteWouldReplaceIt()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        SaveNow(storage, "hero", Named("a"));
        SaveNow(storage, "hero", Named("b"));
        storage.Flush();
        File.WriteAllBytes(temp["saves/hero.dat.bak"], [7]);
        SaveNow(storage, "hero", Named("c"));
        storage.Flush();
        Assert.Equal(["saves/hero.dat", "saves/hero.dat.bak", "saves/hero.dat.bak.damaged"], temp.Files());
        Assert.Equal([7], Bytes(temp, "saves/hero.dat.bak.damaged"));
        Assert.Equal(SaveFile.Write(Named("b")), Bytes(temp, "saves/hero.dat.bak"));
        Assert.Equal(SaveFile.Write(Named("c")), Bytes(temp, "saves/hero.dat"));
        // An unreadable one (another type) alike; a readable one is just replaced.
        File.WriteAllBytes(temp["saves/hero.dat.bak"], SaveFile.Write(new Part { Name = "horn" }));
        SaveNow(storage, "hero", Named("d"));
        SaveNow(storage, "hero", Named("e"));
        storage.Flush();
        Assert.Equal(["saves/hero.dat", "saves/hero.dat.bak", "saves/hero.dat.bak.damaged", "saves/hero.dat.bak.damaged-2"], temp.Files());
        Assert.Equal(SaveFile.Write(Named("d")), Bytes(temp, "saves/hero.dat.bak"));
    }

    [Fact]
    public void AnInterruptedWriteLeavesAStaleTemporaryFileThatIsIgnored()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        storage.Saves.Save("hero", Named("saved"));
        storage.Flush();
        // A crash while the next save was written: half a file beside the save.
        temp.Write("saves/hero.dat.tmp", SaveFile.Write(Named("half"))[..30]);
        temp.Write("saves/lost.dat.tmp", SaveFile.Write(Named("never"))[..30]);
        Assert.Equal(["hero"], storage.Saves.List().Select(s => s.Name));
        Assert.Equal("saved", storage.Saves.Load<Creature>("hero").Value.Name);
        Assert.Equal(SaveState.Missing, storage.Saves.Load<Creature>("lost").Save.State);
        // The next save of the slot writes over it and puts it in place.
        storage.Saves.Save("hero", Named("next"));
        storage.Flush();
        Assert.Equal(["saves/hero.dat", "saves/hero.dat.bak", "saves/lost.dat.tmp"], temp.Files());
        Assert.Equal("next", storage.Saves.Load<Creature>("hero").Value.Name);
    }

    [Fact]
    public void ASaveMissingBesideItsBackupLoadsNothingAndTheNextSaveKeepsTheBackup()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        SaveNow(storage, "hero", Named("old"));
        SaveNow(storage, "hero", Named("new"));
        storage.Flush();
        File.Delete(temp["saves/hero.dat"]);
        var slot = Assert.Single(storage.Saves.List());
        Assert.Equal(("hero", false, true), (slot.Name, slot.HasSave, slot.HasBackup));
        Assert.True(storage.Saves.Exists("hero"));
        var reading = storage.Saves.Load<Creature>("hero");
        Assert.False(reading.Loaded);
        Assert.Equal(SaveState.Missing, reading.Save.State);
        Assert.Equal("old", reading.Backup!.Value.Name);
        SaveNow(storage, "hero", Named("fresh"));
        storage.Flush();
        Assert.Equal(SaveFile.Write(Named("fresh")), Bytes(temp, "saves/hero.dat"));
        Assert.Equal(SaveFile.Write(Named("old")), Bytes(temp, "saves/hero.dat.bak"));
    }

    [Fact]
    public void DeletingRemovesTheSaveAndBackupButNotWhatWasKeptAside()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        storage.Saves.Delete("nothing");
        SaveNow(storage, "hero", Named("a"));
        SaveNow(storage, "hero", Named("b"));
        SaveNow(storage, "other", Named("o"));
        storage.Flush();
        temp.Write("saves/hero.dat.damaged", [1]);
        temp.Write("saves/hero.dat.tmp", [2]);
        storage.Saves.Delete("hero");
        Assert.Equal(["saves/hero.dat.damaged", "saves/other.dat"], temp.Files());
        Assert.False(storage.Saves.Exists("hero"));
        Assert.Equal(["other"], storage.Saves.List().Select(s => s.Name));
        storage.Saves.Delete("hero");
    }

    [Fact]
    public async Task DeletingDropsAPendingSave()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        using var held = new HeldWriter(storage, which: file => file.EndsWith("first.dat"));
        storage.Saves.Save("first", Named("1"));
        held.WaitUntilHeld();
        storage.Saves.Save("hero", Named("pending"));
        storage.Saves.Delete("hero");
        // Deleting a slot being written waits for the write, then deletes it.
        var deleted = Task.Run(() => storage.Saves.Delete("first"));
        Thread.Sleep(100);
        Assert.False(deleted.IsCompleted);
        held.Release();
        await deleted;
        storage.Flush();
        Assert.Empty(temp.Files());
    }

    [Fact]
    public void AnOldVersionSaveInASlotIsUpgradedAndBecomesTheBackupAsItIs()
    {
        using var temp = new StorageFolder();
        using var storage = new GameStorage(temp.Root);
        var old = StorageTestData.Read("Saves/creature-1.dat");
        temp.Write("saves/hero.dat", old);
        var reading = storage.Saves.Load<Creature>("hero");
        RecordAssert.Equal(StorageTestData.Versions[0].Expected, reading.Value);
        storage.Saves.Save("hero", reading.Value);
        storage.Flush();
        Assert.Equal(old, Bytes(temp, "saves/hero.dat.bak"));
        Assert.Equal(SaveFile.Write(reading.Value), Bytes(temp, "saves/hero.dat"));
    }
}
