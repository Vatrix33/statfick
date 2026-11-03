using Statventure.Storage;

namespace Statventure.Tests.Storage;

/// <summary>The folder listings the port's file picker is built on (Docs/Assets.md, Q19).</summary>
public class FolderListingTests
{
    [Fact]
    public void AFoldersEntriesAreListedWithTheirKindSizeTimeAndHiddenFlag()
    {
        using var temp = new StorageFolder();
        temp.Write("b.json", new byte[5]);
        temp.Write("A.txt", new byte[3]);
        temp.Write("sub/deeper.json", new byte[9]);
        temp.Write(".hidden", new byte[1]);
        Directory.CreateDirectory(temp["Empty folder"]);
        File.SetLastWriteTimeUtc(temp["b.json"], new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc));
        var entries = FolderListing.List(temp.Root);
        Assert.Equal([".hidden", "A.txt", "Empty folder", "b.json", "sub"], entries.Select(e => e.Name));
        Assert.Equal([false, false, true, false, true], entries.Select(e => e.IsFolder));
        Assert.Equal([1L, 3, 0, 5, 0], entries.Select(e => e.Size));
        Assert.Equal(entries.Select(e => Path.Combine(temp.Root, e.Name)), entries.Select(e => e.FullPath));
        Assert.Equal(new DateTime(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc), entries[3].LastWrittenUtc);
        if (!OperatingSystem.IsWindows())
            Assert.Equal([true, false, false, false, false], entries.Select(e => e.IsHidden));
    }

    [Fact]
    public void AFolderThatIsntThereIsAnError()
    {
        using var temp = new StorageFolder();
        Assert.Throws<DirectoryNotFoundException>(() => FolderListing.List(temp["missing"]));
        Assert.Empty(FolderListing.List(temp.Root));
    }

    [Fact]
    public void TheRootsAreTheSystems()
    {
        var roots = FolderListing.Roots();
        if (OperatingSystem.IsWindows())
            Assert.All(roots, root => Assert.True(Directory.Exists(root)));
        else
            Assert.Equal(["/"], roots);
    }
}
