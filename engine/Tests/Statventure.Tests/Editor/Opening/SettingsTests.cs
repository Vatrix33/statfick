using System.Text;
using Statventure.Editor;
using Statventure.Editor.Settings;
using Statventure.Editor.Shell;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Editor.Opening;

/// <summary>
/// The editor's settings files (G6; the manager's decisions of 2026-11-06 and 2026-11-07): <c>editor/shell.json</c>
/// (the page zoom level and the window's size) and <c>editor/recent-places.json</c>, read tolerantly and written whole,
/// in a scratch player folder; the zoom level the shell's keys set, settable at start; the window's options.
/// </summary>
public class SettingsTests
{
    [Fact]
    public void TheShellsSettingsAreWrittenWholeAndReadBack()
    {
        using var folder = new StorageFolder();
        var player = folder["player"];
        Assert.Equal(new ShellSettings(), ShellSettings.Read(player));
        Assert.False(Directory.Exists(player));
        Assert.True(new ShellSettings(1.5, 1400, 900).Write(player));
        var file = Path.Combine(player, "editor", "shell.json");
        Assert.Equal(file, ShellSettings.File(player));
        Assert.Equal("{\n  \"version\": 1,\n  \"zoomLevel\": 1.5,\n  \"width\": 1400,\n  \"height\": 900\n}\n", File.ReadAllText(file));
        Assert.Equal(new ShellSettings(1.5, 1400, 900), ShellSettings.Read(player));
        // Swapped in: nothing left beside it; on Linux the folder is the player's own.
        Assert.Equal(["player/editor/shell.json"], folder.Files());
        if (!OperatingSystem.IsWindows())
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute, File.GetUnixFileMode(Path.GetDirectoryName(file)!));
        // A size under the smallest is written and read as the smallest; none given, none written.
        new ShellSettings(-1, 300, 200).Write(player);
        Assert.Equal("{\n  \"version\": 1,\n  \"zoomLevel\": -1,\n  \"width\": 800,\n  \"height\": 500\n}\n", File.ReadAllText(file));
        Assert.Equal(new ShellSettings(-1, 800, 500), ShellSettings.Read(player));
        new ShellSettings(0).Write(player);
        Assert.Equal("{\n  \"version\": 1,\n  \"zoomLevel\": 0\n}\n", File.ReadAllText(file));
        Assert.Equal(new ShellSettings(), ShellSettings.Read(player));
    }

    [Theory]
    [InlineData("", 0, null, null)]
    [InlineData("not json", 0, null, null)]
    [InlineData("[1, 2]", 0, null, null)]
    [InlineData("{ \"zoomLevel\": 1, \"width\": 1000, \"height\": 700 }", 0, null, null)]
    [InlineData("{ \"version\": 2, \"zoomLevel\": 1, \"width\": 1000, \"height\": 700 }", 0, null, null)]
    [InlineData("{ \"version\": \"1\", \"zoomLevel\": 1 }", 0, null, null)]
    [InlineData("{ \"version\": 1, \"version\": 1, \"zoomLevel\": 1 }", 0, null, null)]
    [InlineData("{ \"version\": 1, \"zoomLevel\": 1, \"width\": 1000, \"height\": 700 }", 1, 1000, 700)]
    [InlineData("{ \"version\": 1.0, \"zoomLevel\": -3.5, \"width\": 1000.0 }", -3.5, 1000, null)]
    [InlineData("{ \"version\": 1, \"zoomLevel\": 6, \"other\": true }", 6, null, null)]
    [InlineData("{ \"version\": 1, \"zoomLevel\": 6.5 }", 0, null, null)]
    [InlineData("{ \"version\": 1, \"zoomLevel\": -4 }", 0, null, null)]
    [InlineData("{ \"version\": 1, \"zoomLevel\": 0.25 }", 0, null, null)]
    [InlineData("{ \"version\": 1, \"zoomLevel\": \"1\" }", 0, null, null)]
    [InlineData("{ \"version\": 1, \"width\": 1000.5, \"height\": \"700\" }", 0, null, null)]
    [InlineData("{ \"version\": 1, \"width\": 0, \"height\": -5 }", 0, null, null)]
    [InlineData("{ \"version\": 1, \"width\": 1, \"height\": 499 }", 0, 800, 500)]
    [InlineData("{ \"version\": 1, \"width\": 3000000000, \"height\": 1e3 }", 0, null, 1000)]
    public void AShellFileThatCantBeUsedIsIgnoredValueByValue(string text, double zoom, int? width, int? height)
    {
        using var folder = new StorageFolder();
        folder.Write("player/editor/shell.json", Encoding.UTF8.GetBytes(text));
        Assert.Equal(new ShellSettings(zoom, width, height), ShellSettings.Read(folder["player"]));
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("not json", false)]
    [InlineData("[1, 2]", false)]
    [InlineData("\"text\"", false)]
    [InlineData("{ \"zoomLevel\": 1 }", false)]
    [InlineData("{ \"version\": 2 }", false)]
    [InlineData("{ \"version\": 1, \"version\": 1 }", false)]
    [InlineData("{ \"version\": 1 }", true)]
    [InlineData("{ \"version\": 1.0, \"other\": [] }", true)]
    public void AFileIsReadOnlyAsAVersion1Object(string text, bool read)
    {
        using var folder = new StorageFolder();
        var file = folder.Write("editor/some.json", Encoding.UTF8.GetBytes(text));
        var values = EditorSettings.Read(file);
        Assert.Equal(read, values is not null);
        if (values is not null)
            Assert.False(values.ContainsKey("version"));
    }

    [Fact]
    public void AFileThatCantBeReadIsIgnoredAndAWriteThatFailsIsLogged()
    {
        using var folder = new StorageFolder();
        // A folder where the file goes: neither read nor written.
        Directory.CreateDirectory(folder["player/editor/shell.json"]);
        Assert.Equal(new ShellSettings(), ShellSettings.Read(folder["player"]));
        Assert.False(new ShellSettings(1).Write(folder["player"]));
        Assert.Null(EditorSettings.Read(folder["player/editor/missing.json"]));
        // A player folder that is a file: the settings folder can't be made.
        folder.Write("file", [1]);
        Assert.False(RecentPlacesFile.Write(folder["file"], new RecentPlaces(["/a"])));
    }

    [Fact]
    public void TheRecentPlacesAreKeptMostRecentFirst()
    {
        using var folder = new StorageFolder();
        var player = folder["player"];
        Assert.Empty(RecentPlacesFile.Read(player).Folders);
        var places = new RecentPlaces([folder["b"], folder["a"]]);
        Assert.True(RecentPlacesFile.Write(player, places));
        Assert.Equal(Path.Combine(player, "editor", "recent-places.json"), RecentPlacesFile.File(player));
        Assert.Equal($"{{\n  \"version\": 1,\n  \"folders\": [\n    \"{folder["b"]}\",\n    \"{folder["a"]}\"\n  ]\n}}\n", File.ReadAllText(RecentPlacesFile.File(player)));
        Assert.Equal([folder["b"], folder["a"]], RecentPlacesFile.Read(player).Folders);
        // Only strings are folders; a file that isn't the version's is none.
        File.WriteAllText(RecentPlacesFile.File(player), "{ \"version\": 1, \"folders\": [\"/x\", 3, null, [\"/y\"], \"/z\"] }");
        Assert.Equal(["/x", "/z"], RecentPlacesFile.Read(player).Folders);
        File.WriteAllText(RecentPlacesFile.File(player), "{ \"version\": 1, \"folders\": \"/x\" }");
        Assert.Empty(RecentPlacesFile.Read(player).Folders);
        File.WriteAllText(RecentPlacesFile.File(player), "{ \"version\": 3, \"folders\": [\"/x\"] }");
        Assert.Empty(RecentPlacesFile.Read(player).Folders);
    }

    [Fact]
    public void TheZoomLevelCanBeSetAtStartAndIsTold()
    {
        using var editor = new EditorHarness();
        var told = 0;
        editor.View.ZoomChanged += () => told++;
        Assert.Equal(0, editor.View.ZoomLevel);
        editor.View.ZoomLevel = 2;
        Assert.Equal(1, told);
        Assert.Equal((float)Math.Pow(1.2, 2), editor.View.Zoom);
        Assert.Equal(editor.View.Zoom, editor.Document.PageZoom);
        editor.Key("=", "Equal", KeyModifiers.Control);
        Assert.Equal((2.5, 2), (editor.View.ZoomLevel, told));
        editor.Key("-", "Minus", KeyModifiers.Control);
        Assert.Equal((2.0, 3), (editor.View.ZoomLevel, told));
        // The program's host takes it when there is one.
        var applied = new List<float>();
        editor.View.ApplyZoom = applied.Add;
        editor.View.ZoomLevel = -3.5;
        Assert.Equal([(float)Math.Pow(1.2, -3.5)], applied);
        // Past the keys' limits: no step and nothing told.
        editor.Key("-", "Minus", KeyModifiers.Control);
        Assert.Equal((-3.5, 4), (editor.View.ZoomLevel, told));
        foreach (var level in new[] { 6.5, -4, 0.25, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => editor.View.ZoomLevel = level);
        Assert.Equal(-3.5, editor.View.ZoomLevel);
        Assert.True(EditorView.IsZoomLevel(6));
        Assert.True(EditorView.IsZoomLevel(-3.5));
        Assert.False(EditorView.IsZoomLevel(-4));
        Assert.False(EditorView.IsZoomLevel(6.5));
        Assert.False(EditorView.IsZoomLevel(1.25));
    }

    [Fact]
    public void TheWindowsOptionsTakeTheRememberedSize()
    {
        var options = EditorApplication.OptionsFor(new ShellSettings(1, 1400, 900), "/players/one");
        Assert.Equal((EditorView.Name, 1400, 900, 800, 500, true, true), (options.Window.Title, options.Window.Width, options.Window.Height,
            options.Window.MinWidth, options.Window.MinHeight, options.Window.Resizable, options.Window.Visible));
        // The sound on for the preview (step 13).
        Assert.Equal(("/players/one", true, true), (options.GameFolder, options.GamePacksOnly, options.Audio));
        var none = EditorApplication.OptionsFor(new ShellSettings(), null, visible: false);
        Assert.Equal((1280, 800, false, null), (none.Window.Width, none.Window.Height, none.Window.Visible, none.GameFolder));
        Assert.True(none.GamePacksOnly);
    }
}
