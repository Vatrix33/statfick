using Statventure.Packs;
using static Statventure.Tests.Packs.PackTestData;

namespace Statventure.Tests.Packs;

/// <summary>
/// The virtual file system (Docs/Assets.md, "The virtual file system"; Q6–Q8, Q29, Q30): mount order,
/// mods, loose folders, the union listing, Reload, and errors that never fall back to a lower mount.
/// </summary>
public class VirtualFileSystemTests
{
    [Fact]
    public void LaterMountsWin()
    {
        using var folder = new TempFolder();
        var a = MakePack(folder["a.pak"], Entry("x", "a"), Entry("y", "a"));
        var b = MakePack(folder["b.pak"], Entry("y", "b"), Entry("z", "b"));
        using (var data = new VirtualFileSystem([Mount.Pack(a), Mount.Pack(b)]))
        {
            Assert.Equal(["a", "b", "b"], new[] { "x", "y", "z" }.Select(data.ReadText));
            Assert.Equal([a, b, b], new[] { "x", "y", "z" }.Select(data.Origin));
            Assert.Equal(["x", "y", "z"], data.List());
            Assert.Equal(2, data.Mounts.Count);
        }
        using (var data = new VirtualFileSystem([Mount.Pack(b), Mount.Pack(a)]))
        {
            Assert.Equal(["a", "a", "b"], new[] { "x", "y", "z" }.Select(data.ReadText));
            Assert.Equal(a, data.Origin("y"));
        }
    }

    [Fact]
    public void AListingIsTheUnionInPathOrder()
    {
        using var folder = new TempFolder();
        var a = MakePack(folder["a.pak"], Entry("audio/themes/b", "a"), Entry("audio-x/c", "a"), Entry("audio.txt", "a"), Entry("ui/f", "a"));
        var b = MakePack(folder["b.pak"], Entry("audio/themes/a", "b"), Entry("audio/themes/b", "b"), Entry("audio/rooms/hall", "b"), Entry("audio0", "b"));
        folder.Write("loose/audio/z", "c");
        using var data = new VirtualFileSystem([Mount.Pack(a), Mount.Pack(b), Mount.Folder(folder["loose"])]);
        Assert.Equal(["audio-x/c", "audio.txt", "audio/rooms/hall", "audio/themes/a", "audio/themes/b", "audio/z", "audio0", "ui/f"], data.List());
        Assert.Equal(["audio/rooms/hall", "audio/themes/a", "audio/themes/b", "audio/z"], data.List("audio"));
        Assert.Equal(["audio/themes/a", "audio/themes/b"], data.List("audio/themes"));
        Assert.Equal(["ui/f"], data.List("ui"));
        Assert.Empty(data.List("audio/themes/a"));
        Assert.Empty(data.List("nothing"));
        Assert.Empty(data.List("zzz"));
    }

    [Fact]
    public void ModsMountInTheOrdinalOrderOfTheirNames()
    {
        using var folder = new TempFolder();
        var game = MakePack(folder["game.pak"], Entry("who", "game"), Entry("game-only", "game"));
        foreach (var name in new[] { "b.pak", "a.pak", "C.PAK", "d.Pak", "notes.txt", "e.pak.tmp", "sub/f.pak", "g.pak/inner.pak" })
            MakePack(folder[$"mods/{name}"], Entry("who", name), Entry($"only/{name.Replace('/', '-').ToLowerInvariant()}", name));
        Assert.Equal([folder["mods/C.PAK"], folder["mods/a.pak"], folder["mods/b.pak"], folder["mods/d.Pak"]], VirtualFileSystem.ModFiles(folder["mods"]));
        using var data = new VirtualFileSystem([Mount.Pack(game), Mount.Mods(folder["mods"])]);
        Assert.Equal("d.Pak", data.ReadText("who"));
        Assert.Equal("game", data.ReadText("game-only"));
        Assert.Equal(["only/a.pak", "only/b.pak", "only/c.pak", "only/d.pak"], data.List("only"));

        File.Delete(folder["mods/d.Pak"]);
        data.Reload();
        Assert.Equal("b.pak", data.ReadText("who"));
        File.Delete(folder["mods/b.pak"]);
        File.Delete(folder["mods/a.pak"]);
        data.Reload();
        Assert.Equal("C.PAK", data.ReadText("who"));
    }

    [Fact]
    public void AMissingModsFolderMeansNoMods()
    {
        using var folder = new TempFolder();
        var game = MakePack(folder["game.pak"], Entry("who", "game"));
        using var data = new VirtualFileSystem([Mount.Pack(game), Mount.Mods(folder["mods"])]);
        Assert.Equal(["who"], data.List());
        Assert.Empty(VirtualFileSystem.ModFiles(folder["mods"]));

        MakePack(folder["mods/new.pak"], Entry("who", "mod"));
        Assert.Equal("game", data.ReadText("who"));
        data.Reload();
        Assert.Equal("mod", data.ReadText("who"));
        Assert.Equal(folder["mods/new.pak"], data.Origin("who"));
    }

    [Fact]
    public void TheDevelopmentFolderIsOnTopAndReadAsItIsNow()
    {
        using var folder = new TempFolder();
        var game = MakePack(folder["game.pak"], Entry("audio/a", "game"), Entry("audio/b", "game"));
        MakePack(folder["mods/m.pak"], Entry("audio/a", "mod"), Entry("audio/b", "mod"));
        var loose = folder.Write("dev/audio/a", "dev");
        using var data = new VirtualFileSystem([Mount.Pack(game), Mount.Mods(folder["mods"]), Mount.Folder(folder["dev"])]);
        Assert.Equal("dev", data.ReadText("audio/a"));
        Assert.Equal("mod", data.ReadText("audio/b"));
        Assert.Equal(loose, data.Origin("audio/a"));

        File.WriteAllText(loose, "edited");
        Assert.Equal("edited", data.ReadText("audio/a"));
        folder.Write("dev/audio/c", "new");
        Assert.False(data.Exists("audio/c"));
        data.Reload();
        Assert.Equal("new", data.ReadText("audio/c"));
        File.Delete(loose);
        Assert.ThrowsAny<IOException>(() => data.ReadAllBytes("audio/a"));
        data.Reload();
        Assert.Equal("mod", data.ReadText("audio/a"));
    }

    [Fact]
    public void AFolderMountsBelowItsPlace()
    {
        using var folder = new TempFolder();
        folder.Write("shipped/classes/black-knight.json", "{}");
        folder.Write("shipped/classes/sub/mage.json", "{}");
        using var data = new VirtualFileSystem([Mount.Folder(folder["shipped/classes"], "classes")]);
        Assert.Equal(["classes/black-knight.json", "classes/sub/mage.json"], data.List());
        Assert.Equal("{}", data.ReadText("classes/sub/mage.json"));
        Assert.Equal("classes", data.Mounts[0].At);
        Assert.Equal(MountKind.Folder, data.Mounts[0].Kind);
    }

    [Theory]
    [InlineData("Black Knight.json")]
    [InlineData("x.json~")]
    [InlineData("Thumbs.db")]
    [InlineData("sub/aux.json")]
    public void AMisnamedLooseFileStopsTheMount(string name)
    {
        using var folder = new TempFolder();
        folder.Write("dev/good", "x");
        var bad = folder.Write($"dev/{name}", "x");
        var refused = Assert.Throws<DataFileException>(() => new VirtualFileSystem([Mount.Folder(folder["dev"])]));
        Assert.Equal(bad, refused.File);
        Assert.Contains("outside the path rule", refused.Problem);
    }

    [Fact]
    public void AFailedReloadKeepsTheOldMounts()
    {
        using var folder = new TempFolder();
        var game = MakePack(folder["game.pak"], Entry("a", "game"));
        folder.Write("dev/b", "dev");
        using var data = new VirtualFileSystem([Mount.Pack(game), Mount.Folder(folder["dev"])]);
        var reloads = 0;
        data.Reloaded += () => reloads++;
        var bad = folder.Write("dev/Bad", "x");
        folder.Write("dev/c", "new");
        Assert.Equal(bad, Assert.Throws<DataFileException>(data.Reload).File);
        Assert.Equal(0, reloads);
        Assert.Equal("game", data.ReadText("a"));
        Assert.Equal("dev", data.ReadText("b"));
        Assert.False(data.Exists("c"));
        File.Delete(bad);
        data.Reload();
        Assert.Equal(1, reloads);
        Assert.Equal("new", data.ReadText("c"));
    }

    [Fact]
    public void ReloadPicksUpARebuiltPack()
    {
        using var folder = new TempFolder();
        var file = MakePack(folder["game.pak"], Entry("x", "1"));
        using var data = new VirtualFileSystem([Mount.Pack(file)]);
        var reloads = 0;
        data.Reloaded += () => reloads++;
        MakePack(file, Entry("x", "2"), Entry("y", "2"));
        // The open pack keeps what it was until Reload.
        Assert.Equal("1", data.ReadText("x"));
        Assert.False(data.Exists("y"));
        data.Reload();
        Assert.Equal(1, reloads);
        Assert.Equal("2", data.ReadText("x"));
        Assert.Equal("2", data.ReadText("y"));
    }

    [Fact]
    public void AMountedPackCanBeWrittenAndDeleted()
    {
        using var folder = new TempFolder();
        var file = MakePack(folder["game.pak"], Entry("x", "1"));
        using var data = new VirtualFileSystem([Mount.Pack(file)]);
        using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete))
        {
        }
        File.Delete(file);
        Assert.Equal("1", data.ReadText("x"));
        Assert.Throws<FileNotFoundException>(data.Reload);
        Assert.Equal("1", data.ReadText("x"));
    }

    [Fact]
    public void ADamagedEntryIsAnErrorNotTheLowerMounts()
    {
        using var folder = new TempFolder();
        var lower = MakePack(folder["lower.pak"], ("x", Prose(1000)));
        var upper = MakePack(folder["upper.pak"], ("x", Prose(1000, 1)));
        var bytes = File.ReadAllBytes(upper);
        bytes[64 + 10] ^= 4;
        File.WriteAllBytes(upper, bytes);
        using var data = new VirtualFileSystem([Mount.Pack(lower), Mount.Pack(upper)]);
        var refused = Assert.Throws<DataFileException>(() => data.ReadAllBytes("x"));
        Assert.Equal(upper, refused.File);
        Assert.Equal("x", refused.Entry);
        Assert.Throws<DataFileException>(() => data.Open("x"));
        Assert.True(data.Exists("x"));
    }

    [Fact]
    public void ADamagedPackStopsTheMount()
    {
        using var folder = new TempFolder();
        var game = MakePack(folder["game.pak"], Entry("x", "1"));
        var mod = MakePack(folder["mods/m.pak"], Entry("x", "2"));
        var bytes = File.ReadAllBytes(mod);
        bytes[^1] ^= 1;
        File.WriteAllBytes(mod, bytes);
        var refused = Assert.Throws<DataFileException>(() => new VirtualFileSystem([Mount.Pack(game), Mount.Mods(folder["mods"])]));
        Assert.Equal(mod, refused.File);
        Assert.Contains("the index doesn't match its hash", refused.Problem);
    }

    [Fact]
    public void MissingThingsAreErrors()
    {
        using var folder = new TempFolder();
        var game = MakePack(folder["game.pak"], Entry("x", "1"));
        using var data = new VirtualFileSystem([Mount.Pack(game)]);
        Assert.False(data.Exists("y"));
        Assert.Null(data.Origin("y"));
        Assert.Equal("y", Assert.Throws<FileNotFoundException>(() => data.ReadAllBytes("y")).FileName);
        Assert.Throws<FileNotFoundException>(() => new VirtualFileSystem([Mount.Pack(folder["none.pak"])]));
        Assert.Throws<DirectoryNotFoundException>(() => new VirtualFileSystem([Mount.Folder(folder["none"])]));
        using var empty = new VirtualFileSystem([]);
        Assert.Empty(empty.List());
    }

    [Fact]
    public void OpenGivesTheContentAsAStream()
    {
        using var folder = new TempFolder();
        var game = MakePack(folder["game.pak"], ("x", Prose(5000)));
        using var data = new VirtualFileSystem([Mount.Pack(game)]);
        using var stream = data.Open("x");
        Assert.False(stream.CanWrite);
        var copy = new MemoryStream();
        stream.CopyTo(copy);
        Assert.Equal(Prose(5000), copy.ToArray());
    }

    [Fact]
    public void ReadsGoOnSafelyWhileItReloads()
    {
        using var folder = new TempFolder();
        var entries = Enumerable.Range(0, 16).Select(i => ($"e{i}", Noise(64, i))).ToArray();
        var game = MakePack(folder["game.pak"], entries);
        folder.Write("dev/loose", "x");
        using var data = new VirtualFileSystem([Mount.Pack(game), Mount.Folder(folder["dev"])]);
        using var stop = new CancellationTokenSource();
        using var running = new CountdownEvent(4);
        var failures = new System.Collections.Concurrent.ConcurrentQueue<Exception>();
        var reads = 0L;
        // Threads of their own, all reading before the first Reload and until the last.
        var readers = Enumerable.Range(0, 4).Select(r => new Thread(() =>
        {
            running.Signal();
            try
            {
                for (var i = r; !stop.IsCancellationRequested; i++)
                {
                    var (path, content) = entries[i % entries.Length];
                    Assert.Equal(content, data.ReadAllBytes(path));
                    Assert.Equal(17, data.List().Count);
                    Interlocked.Increment(ref reads);
                }
            }
            catch (Exception failure)
            {
                failures.Enqueue(failure);
            }
        })).ToArray();
        foreach (var reader in readers)
            reader.Start();
        running.Wait();
        for (var i = 0; i < 300; i++)
            data.Reload();
        stop.Cancel();
        foreach (var reader in readers)
            reader.Join();
        Assert.Empty(failures);
        Assert.True(Interlocked.Read(ref reads) > 300);
    }

    [Fact]
    public void ADisposedFileSystemRefusesCalls()
    {
        using var folder = new TempFolder();
        var game = MakePack(folder["game.pak"], Entry("x", "1"));
        var data = new VirtualFileSystem([Mount.Pack(game)]);
        data.Dispose();
        data.Dispose();
        Assert.Throws<ObjectDisposedException>(() => data.ReadAllBytes("x"));
        Assert.Throws<ObjectDisposedException>(() => data.Exists("x"));
        Assert.Throws<ObjectDisposedException>(() => data.List());
        Assert.Throws<ObjectDisposedException>(data.Reload);
    }

    [Fact]
    public void PacksAreClosedWhenReplacedOrDisposed()
    {
        if (!OperatingSystem.IsLinux())
            return;
        // The process's open files, from /proc.
        static int OpenCount(string file) => Directory.GetFiles("/proc/self/fd").Count(fd =>
        {
            try
            {
                return new FileInfo(fd).LinkTarget == file;
            }
            catch (IOException)
            {
                return false;
            }
        });
        using var folder = new TempFolder();
        var game = MakePack(folder["game.pak"], Entry("x", "1"));
        var mod = MakePack(folder["mods/m.pak"], Entry("y", "1"));
        folder.Write("dev/z", "1");
        var data = new VirtualFileSystem([Mount.Pack(game), Mount.Mods(folder["mods"]), Mount.Folder(folder["dev"])]);
        Assert.Equal(1, OpenCount(game));
        for (var i = 0; i < 5; i++)
            data.Reload();
        Assert.Equal(1, OpenCount(game));
        Assert.Equal(1, OpenCount(mod));
        // A Reload that fails closes what it had opened.
        var bad = folder.Write("dev/Bad", "x");
        Assert.Throws<DataFileException>(data.Reload);
        Assert.Equal(1, OpenCount(game));
        Assert.Equal(1, OpenCount(mod));
        data.Dispose();
        Assert.Equal(0, OpenCount(game));
        Assert.Equal(0, OpenCount(mod));
        File.Delete(bad);
    }
}
