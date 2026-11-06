using Statventure.Storage;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Storage;

/// <summary>
/// The file browser's model (Docs/Editor.md, step 3, P26): its places, a folder's listing (folders first, names in
/// order, hidden ones when asked, files by their endings), the path field's reading and completion, and the recent
/// places; on scratch folders, this machine's places read through the run's redirected per-user folders
/// (<see cref="PerUserFolders"/>).
/// </summary>
public class FileBrowserTests
{
    static readonly char S = Path.DirectorySeparatorChar;

    /// <summary>Hides an entry: a dot name already is on Linux; Windows has the attribute.</summary>
    static void Hide(string path)
    {
        if (OperatingSystem.IsWindows())
            File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
    }

    static string[] Names(IEnumerable<FolderEntry> entries) => [.. entries.Select(e => e.Name)];

    [Fact]
    public void ThePlacesAreTheModsFolderDocumentsHomeTheRootsThenTheRecentFoldersThatAreThere()
    {
        using var temp = new StorageFolder();
        foreach (var folder in new[] { "mods", "docs", "home", "recent/one", "recent/two" })
            Directory.CreateDirectory(temp[folder]);
        var places = FileBrowser.Places(temp["mods"], temp["docs"], temp["home"], ["/r1", "/r2"],
            [temp["recent/two"], temp["recent/gone"], temp["recent/one"]]);
        BrowserPlace[] expected =
        [
            new(BrowserPlaceKind.Mods, temp["mods"]), new(BrowserPlaceKind.Documents, temp["docs"]), new(BrowserPlaceKind.Home, temp["home"]),
            new(BrowserPlaceKind.Root, "/r1"), new(BrowserPlaceKind.Root, "/r2"),
            new(BrowserPlaceKind.Recent, temp["recent/two"]), new(BrowserPlaceKind.Recent, temp["recent/one"]),
        ];
        Assert.Equal(expected, places);
        // The mods folder before it is made; Documents and home left out when not known; folders made full.
        var relative = Path.GetRelativePath(Environment.CurrentDirectory, temp["not made yet"]);
        BrowserPlace[] bare = [new(BrowserPlaceKind.Mods, temp["not made yet"]), new(BrowserPlaceKind.Root, "/")];
        Assert.Equal(bare, FileBrowser.Places(relative, null, "", ["/"], []));
        Assert.Equal(bare, FileBrowser.Places(relative, "", null, ["/"], []));
    }

    [Fact]
    public void ThisMachinesPlacesAreItsFoldersAndTheRecentOnes()
    {
        using var temp = new StorageFolder();
        Directory.CreateDirectory(temp["recent"]);
        var recent = new RecentPlaces([temp["recent"], temp["gone"]]);
        var places = FileBrowser.Places(temp["player/mods"], recent);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
        BrowserPlace[] expected =
        [
            new(BrowserPlaceKind.Mods, temp["player/mods"]), new(BrowserPlaceKind.Documents, DocumentsFolder.ForThisMachine()),
            new(BrowserPlaceKind.Home, home), .. FolderListing.Roots().Select(root => new BrowserPlace(BrowserPlaceKind.Root, root)),
            new(BrowserPlaceKind.Recent, temp["recent"]),
        ];
        Assert.Equal(expected, places);
        // In the run's scratch folder, as every per-user folder of the tests (Windows asks its known folders).
        if (!OperatingSystem.IsWindows())
            Assert.All(places.Where(p => p.Kind is BrowserPlaceKind.Documents or BrowserPlaceKind.Home),
                place => Assert.StartsWith(PerUserFolders.Root + S, place.Folder + S));
    }

    [Fact]
    public void AListingHasTheFoldersFirstThenTheFilesEachByNameWhateverTheCase()
    {
        using var temp = new StorageFolder();
        string[] files = OperatingSystem.IsWindows() ? ["b.txt", "A.txt", "c.txt", "_x.txt"] : ["readme.txt", "b.txt", "Readme.txt", "A.txt", "_x.txt"];
        foreach (var file in files)
            temp.Write(file, [1]);
        foreach (var folder in new[] { "zeta", "Beta", "alpha" })
            Directory.CreateDirectory(temp[folder]);
        var entries = FileBrowser.List(temp.Root);
        string[] expected = OperatingSystem.IsWindows()
            ? ["alpha", "Beta", "zeta", "A.txt", "b.txt", "c.txt", "_x.txt"]
            // Equal but for the case: ordinal ("R" before "r").
            : ["alpha", "Beta", "zeta", "A.txt", "b.txt", "Readme.txt", "readme.txt", "_x.txt"];
        Assert.Equal(expected, Names(entries));
        Assert.Equal([true, true, true, false, false], entries.Take(5).Select(e => e.IsFolder));
        // The entries are the listing's own (paths, sizes).
        Assert.Equal(FolderListing.List(temp.Root).OrderBy(e => e.Name, StringComparer.Ordinal), entries.OrderBy(e => e.Name, StringComparer.Ordinal));
        Assert.True(FileBrowser.NameOrder.Compare("a", "B") < 0);
        Assert.True(FileBrowser.NameOrder.Compare("B", "b") < 0);
        Assert.Equal(0, FileBrowser.NameOrder.Compare("b", "b"));
    }

    [Fact]
    public void HiddenEntriesAreListedOnlyWhenAsked()
    {
        using var temp = new StorageFolder();
        Hide(temp.Write(".settings", [1]));
        temp.Write("seen.txt", [1]);
        Directory.CreateDirectory(temp[".cache"]);
        Hide(temp[".cache"]);
        Assert.Equal(["seen.txt"], Names(FileBrowser.List(temp.Root)));
        Assert.Equal([".cache", ".settings", "seen.txt"], Names(FileBrowser.List(temp.Root, showHidden: true)));
    }

    [Fact]
    public void FilesAreFilteredByTheirEndingsInAnyCaseAndEveryFolderIsListed()
    {
        using var temp = new StorageFolder();
        foreach (var file in new[] { "mod.pak", "OTHER.PAK", "notes.txt", "pak", "arena.glb", "hills.terrain.json", "terrain.json", "hills.json" })
            temp.Write(file, [1]);
        foreach (var folder in new[] { "folder.pak", "textures" })
            Directory.CreateDirectory(temp[folder]);
        Assert.Equal(["folder.pak", "textures", "mod.pak", "OTHER.PAK"], Names(FileBrowser.List(temp.Root, [".pak"])));
        Assert.Equal(["folder.pak", "textures", "arena.glb", "hills.terrain.json"], Names(FileBrowser.List(temp.Root, [".GLB", ".terrain.json"])));
        // No endings: every file.
        Assert.Equal(10, FileBrowser.List(temp.Root, []).Count);
        Assert.Equal(10, FileBrowser.List(temp.Root).Count);
        Assert.True(FileBrowser.Shows("A.Pak", [".pak"]));
        Assert.False(FileBrowser.Shows("a.pak.txt", [".pak"]));
        Assert.True(FileBrowser.Shows("anything", null));
        Assert.Throws<DirectoryNotFoundException>(() => FileBrowser.List(temp["missing"]));
    }

    [Fact]
    public void ATypedPathIsReadAgainstTheFolderShown()
    {
        using var temp = new StorageFolder();
        temp.Write("sub/file.txt", [1]);
        Directory.CreateDirectory(temp["sub/deeper"]);
        Directory.CreateDirectory(temp["home/games"]);
        var at = temp["sub"];
        var home = temp["home"];
        TypedPath Read(string typed) => FileBrowser.ReadPath(at, typed, home);
        Assert.Equal(new TypedPath(temp["sub/deeper"], TypedPathKind.Folder), Read("deeper"));
        Assert.Equal(new TypedPath(temp["sub/deeper"], TypedPathKind.Folder), Read("deeper/"));
        Assert.Equal(new TypedPath(temp["sub/file.txt"], TypedPathKind.File), Read("file.txt"));
        Assert.Equal(new TypedPath(temp["sub/file.txt"], TypedPathKind.File), Read("deeper/../../sub/./file.txt"));
        Assert.Equal(new TypedPath(temp["sub/new.txt"], TypedPathKind.New), Read("new.txt"));
        Assert.Equal(new TypedPath(temp["sub/deeper/new.txt"], TypedPathKind.New), Read("deeper/new.txt"));
        // A file followed by a separator, or a name in a folder that isn't there: nothing.
        Assert.Equal(new TypedPath(temp["sub/file.txt"], TypedPathKind.Missing), Read("file.txt/"));
        Assert.Equal(new TypedPath(temp["sub/new"], TypedPathKind.Missing), Read("new/"));
        Assert.Equal(new TypedPath(temp["sub/nowhere/new.txt"], TypedPathKind.Missing), Read("nowhere/new.txt"));
        // A full path as it is; nothing typed is the folder itself.
        Assert.Equal(new TypedPath(temp["home/games"], TypedPathKind.Folder), Read(temp["home/games"]));
        Assert.Equal(new TypedPath(temp["sub"], TypedPathKind.Folder), Read(""));
        Assert.Equal(new TypedPath(temp.Root, TypedPathKind.Folder), Read(".."));
        // ~ is the home folder.
        Assert.Equal(new TypedPath(home, TypedPathKind.Folder), Read("~"));
        Assert.Equal(new TypedPath(temp["home/games"], TypedPathKind.Folder), Read("~/games"));
        Assert.Equal(new TypedPath(temp["home/games"], TypedPathKind.Folder), Read("~/games/"));
        Assert.Equal(new TypedPath(temp["home/save.json"], TypedPathKind.New), Read("~/save.json"));
        Assert.Equal(new TypedPath(temp["sub/~games"], TypedPathKind.New), Read("~games"));
        // With no home known, ~ is a name like any other.
        Assert.Equal(new TypedPath(temp["sub/~"], TypedPathKind.New), FileBrowser.ReadPath(at, "~", ""));
        // Not a path.
        Assert.Equal(new TypedPath("bad\0name", TypedPathKind.Invalid), Read("bad\0name"));
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(new TypedPath("/", TypedPathKind.Folder), Read("/"));
            Assert.Equal(new TypedPath("/", TypedPathKind.Folder), Read("/.."));
        }
        // This machine's home for ~ (the run's scratch home).
        Assert.Equal(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify),
            FileBrowser.ReadPath(at, "~").FullPath);
    }

    [Fact]
    public void TheLastTypedNameCompletesToWhatTheMatchesShare()
    {
        using var temp = new StorageFolder();
        foreach (var file in new[] { "mod-a.pak", "mod-b.pak", "music.txt", "mods/only.pak", ".secret.pak", "home/saves/one.json", "deep/a/x.pak" })
            temp.Write(file, [1]);
        foreach (var folder in new[] { "Models", ".config" })
            Directory.CreateDirectory(temp[folder]);
        Hide(temp[".secret.pak"]);
        Hide(temp[".config"]);
        PathCompletion Complete(string typed, IReadOnlyCollection<string>? endings = null, bool hidden = false) =>
            FileBrowser.Complete(temp.Root, typed, temp["home"], endings, hidden);
        string[] Matched(PathCompletion completion) => Names(completion.Matches);
        void Unchanged(string typed, PathCompletion completion)
        {
            Assert.Equal(typed, completion.Text);
            Assert.Empty(completion.Matches);
        }
        string[] pak = [".pak"];
        // Several matches: their shared start. One folder: its name and a separator. One file: its name.
        var mod = Complete("mo", pak);
        string[] modMatches = OperatingSystem.IsWindows() ? ["Models", "mods", "mod-a.pak", "mod-b.pak"] : ["mods", "mod-a.pak", "mod-b.pak"];
        Assert.Equal(OperatingSystem.IsWindows() ? "Mod" : "mod", mod.Text);
        Assert.Equal(modMatches, Matched(mod));
        Assert.Equal($"mods{S}", Complete("mods", pak).Text);
        Assert.Equal("mod-a.pak", Complete("mod-a", pak).Text);
        // Names as the system compares them: case matters on Linux.
        Assert.Equal(OperatingSystem.IsWindows() ? "Mod" : $"Models{S}", Complete("Mo", pak).Text);
        // Files the endings leave out don't match; without endings they do.
        Unchanged("mu", Complete("mu", pak));
        Assert.Equal("music.txt", Complete("mu").Text);
        // In the folder typed before it, made full as the path field reads it (~ too); the text keeps what was typed.
        Assert.Equal($"mods/only.pak", Complete("mods/o", pak).Text);
        Assert.Equal($"mods/only.pak", Complete("mods/", pak).Text);
        Assert.Equal($"deep/a{S}", Complete("deep/", pak).Text);
        Assert.Equal($"~/saves{S}", Complete("~/s").Text);
        Assert.Equal(temp["mod-a.pak"], Complete(temp["mod-a"], pak).Text);
        // Hidden names only when the name starts with a dot or hidden ones are shown.
        Assert.Equal([".config", ".secret.pak"], Matched(Complete(".", pak)));
        Assert.Equal(".", Complete(".", pak).Text);
        Assert.Equal($".config{S}", Complete(".c").Text);
        Assert.Contains(".config", Matched(Complete("", hidden: true)));
        Assert.DoesNotContain(".config", Matched(Complete("")));
        // Nothing matches, or the folder part isn't a folder: unchanged.
        Unchanged("zz", Complete("zz"));
        Unchanged("nowhere/mo", Complete("nowhere/mo"));
        Unchanged("music.txt/mo", Complete("music.txt/mo"));
        Unchanged("bad\0name/mo", Complete("bad\0name/mo"));
        // This machine's home for ~ (the run's scratch home, not the folder shown).
        var inHome = FileBrowser.Complete(temp.Root, "~/");
        Assert.StartsWith("~/", inHome.Text);
        Assert.DoesNotContain("mod-a.pak", Matched(inHome));
    }

    [Fact]
    public void RecentPlacesPutTheLastUsedFirstOnceAndKeepTen()
    {
        using var temp = new StorageFolder();
        var recent = new RecentPlaces();
        Assert.Empty(recent.Folders);
        recent.Add(temp["a"]);
        recent.Add(temp["b"] + S);
        recent.Add(temp["a"]);
        recent.Add("");
        Assert.Equal([temp["a"], temp["b"]], recent.Folders);
        // Made full.
        recent.Add(Path.GetRelativePath(Environment.CurrentDirectory, temp["c"]));
        Assert.Equal(temp["c"], recent.Folders[0]);
        for (var i = 0; i < 12; i++)
            recent.Add(temp[$"f{i}"]);
        Assert.Equal(RecentPlaces.Limit, recent.Folders.Count);
        Assert.Equal(10, RecentPlaces.Limit);
        Assert.Equal(Enumerable.Range(2, 10).Reverse().Select(i => temp[$"f{i}"]), recent.Folders);
        // Names as the system compares them.
        var cased = new RecentPlaces([temp["X"], temp["x"]]);
        string[] distinct = OperatingSystem.IsWindows() ? [temp["X"]] : [temp["X"], temp["x"]];
        Assert.Equal(distinct, cased.Folders);
        Assert.Throws<ArgumentException>(() => recent.Add("bad\0name"));
    }

    [Fact]
    public void StoredRecentPlacesComeBackAsTheyWereKept()
    {
        using var temp = new StorageFolder();
        var kept = new RecentPlaces([temp["a"], temp["b"], temp["c"]]);
        Assert.Equal([temp["a"], temp["b"], temp["c"]], kept.Folders);
        Assert.Equal(kept.Folders, new RecentPlaces(kept.Folders).Folders);
        // A stored list read as Add reads each: the later of two equal ones, past ten and what isn't a path dropped.
        var stored = new[] { temp["a"], "bad\0name", temp["b"] + S, temp["a"] }.Concat(Enumerable.Range(0, 12).Select(i => temp[$"f{i}"]));
        string[] read = [temp["a"], temp["b"], .. Enumerable.Range(0, 8).Select(i => temp[$"f{i}"])];
        Assert.Equal(read, new RecentPlaces(stored).Folders);
    }
}

/// <summary>Tests that change the process's per-user folders for a moment run alone.</summary>
[CollectionDefinition("Per-user folders changed", DisableParallelization = true)]
public sealed class PerUserFoldersChangedCollection;

/// <summary>
/// This machine's places when its Documents folder can't be made (<see cref="DocumentsFolder.ForThisMachine"/>
/// fails, as Electron's getPath does): left out. The run's config folder pointed for a moment at a scratch one whose
/// user-dirs file names a Documents folder under <c>/proc</c>, where nothing can be made.
/// </summary>
[Collection("Per-user folders changed")]
public class FileBrowserDocumentsTests
{
    [Fact]
    public void DocumentsThatCantBeMadeAreLeftOut()
    {
        // Windows asks its known folder.
        if (OperatingSystem.IsWindows())
            return;
        using var temp = new StorageFolder();
        temp.Write("config/user-dirs.dirs", "XDG_DOCUMENTS_DIR=\"/proc/statventure-cant-make/Documents\"\n"u8.ToArray());
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", temp["config"]);
        try
        {
            Assert.Throws<IOException>(DocumentsFolder.ForThisMachine);
            var places = FileBrowser.Places(temp["mods"]);
            Assert.Equal([BrowserPlaceKind.Mods, BrowserPlaceKind.Home, BrowserPlaceKind.Root], places.Select(p => p.Kind));
        }
        finally
        {
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", config);
        }
    }
}
