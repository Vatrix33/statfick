using Statventure.Game.Data.Saves;
using Statventure.Game.Screens.Picker;
using Statventure.Game.Session;
using Statventure.Game.Ui;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// Save Character and Load Character through the in-game file picker (Q9, Q23, Q24, GM1-P15; step 10), on folders of the
/// test's own (a home with Documents in the harness's scratch folder; never the player's): Save in Documents with the
/// default name, a file that is there replaced only after the question, "Character saved" / "Character could not be
/// saved"; Load from the most recent place, the file's character a new slot that opens, "Loaded &lt;file&gt;" or the
/// session's problem; the places, the listing (folders and .json files; only the rows near the view made), its keys,
/// typed paths and completion; Ctrl+S and Ctrl+O; the sounds at each action.
/// </summary>
public class PickerScreenTests
{
    sealed class Scene : IDisposable
    {
        public Scene()
        {
            H = new GameUiHarness(1000, 860);
            S = ScreenParts.Attach(H);
            Home = Path.GetFullPath(Path.Combine(H.PlayerFolder, "..", "home"));
            Documents = Path.Combine(Home, "Documents");
            Directory.CreateDirectory(Path.Combine(Documents, "old saves"));
            Directory.CreateDirectory(Path.Combine(Documents, "Letters"));
            Directory.CreateDirectory(Path.Combine(Home, "Downloads"));
            S.Picker.Documents = () => Documents;
            S.Picker.Home = () => Home;
            S.Picker.Roots = () => ["/"];
            H.Ui.StatusShown += (text, kind) => Statuses.Add($"{kind}: {text}");
        }

        public GameUiHarness H { get; }
        public ScreenParts S { get; }
        public string Home { get; }
        public string Documents { get; }
        public List<string> Statuses { get; } = [];
        public FilePicker Picker => S.OpenPicker;

        /// <summary>A character file of <paramref name="name"/> at <paramref name="file"/> (the save's JSON form).</summary>
        public string CharacterFile(string file, string name)
        {
            var path = Path.Combine(Documents, file);
            Statventure.Storage.CharacterFile.Export(path, new SaveGame { Name = name });
            return path;
        }

        /// <summary>Waits the frames the picker's task and the document's microtasks need.</summary>
        public void Settle()
        {
            H.Frame();
            H.Frame();
        }

        public void Dispose() => H.Dispose();
    }

    static List<string> Names(FilePicker picker) => [.. picker.Rows.Select(r => r.Descendants().First(e => e.HasClass("picker-name-cell")).TextContent)];

    [Fact]
    public void SaveCharacterNeedsACharacterOpenAndStartsInDocumentsWithTheDefaultName()
    {
        using var scene = new Scene();
        var h = scene.H;
        Assert.False(h.Ui.Commands.IsEnabled(PickerScreen.SaveCommand));
        h.Key("s", "KeyS", KeyModifiers.Control);
        Assert.Null(scene.S.Picker.Open);
        scene.S.Session.New("Ser Alrik");
        File.WriteAllText(Path.Combine(scene.Documents, "notes.txt"), "not shown");
        scene.CharacterFile("wren.json", "Wren");
        h.Key("s", "KeyS", KeyModifiers.Control);
        var picker = scene.Picker;
        Assert.Equal(PickerMode.Save, picker.Mode);
        Assert.True(h.Ui.Windows.IsOpen(picker.Layer));
        Assert.Equal(scene.Documents, picker.Folder);
        Assert.Equal("Save character", h.Q("#picker-title").TextContent);
        Assert.Equal("Documents", h.Q("#picker-where").TextContent);
        Assert.Equal(["Documents", "Home", "/"], picker.PlaceButtons.Select(b => b.TextContent));
        Assert.True(picker.PlaceButtons[0].HasClass("current"));
        // Folders first, then the .json files (a .txt isn't shown).
        Assert.Equal(["Letters/", "old saves/", "wren.json"], Names(picker));
        // A folder's name bold, a file's not; the action in the ember of the dialogs' action, Cancel not (picker.css; too
        // few pixels for the screen comparison's limits to tell).
        UiElement NameCell(int row) => picker.Rows[row].Descendants().First(e => e.HasClass("picker-name-cell"));
        Assert.Equal((700, 400), (NameCell(0).Style!.FontWeight, NameCell(2).Style!.FontWeight));
        var accept = h.Q("#picker-accept").Style!;
        var cancel = h.Q(".picker-cancel").Style!;
        Assert.NotEqual(cancel.BorderTopColor, accept.BorderTopColor);
        Assert.NotEqual(cancel.Color, accept.Color);
        Assert.Equal("stat-fick-character.json", picker.NameField.Value);
        Assert.Same(picker.NameField, h.Document.FocusedElement);
        Assert.Equal((0, "stat-fick-character".Length), (picker.NameField.SelectionStart, picker.NameField.SelectionEnd));
        Assert.Equal(["ui-open"], h.Sounds);
    }

    [Fact]
    public void SaveWritesTheOpenCharacterAndSaysSo()
    {
        using var scene = new Scene();
        var h = scene.H;
        scene.S.Session.New("Ser Alrik");
        scene.S.Run(PickerScreen.SaveCommand);
        h.Sounds.Clear();
        h.Click(h.Q("#picker-accept"));
        scene.Settle();
        var file = Path.Combine(scene.Documents, "stat-fick-character.json");
        Assert.Equal("Ser Alrik", CharacterFile.Import<SaveGame>(file).Name);
        Assert.Null(scene.S.Picker.Open);
        Assert.Empty(h.Ui.Windows.Layers);
        Assert.Equal(["Info: Character saved"], scene.Statuses);
        Assert.Equal(["game-saved"], h.Sounds);
        Assert.Equal(scene.Documents, h.Settings.RecentPlaces[0]);
        // A name typed (Enter in the field) in a folder gone into.
        scene.S.Run(PickerScreen.SaveCommand);
        scene.Picker.NameField.Value = "old saves";
        scene.Picker.NameField.Focus();
        h.Key("Enter", "Enter");
        Assert.Equal(Path.Combine(scene.Documents, "old saves"), scene.Picker.Folder);
        Assert.Equal("", scene.Picker.NameField.Value);
        h.Type("alrik.json");
        h.Key("Enter", "Enter");
        scene.Settle();
        Assert.True(File.Exists(Path.Combine(scene.Documents, "old saves", "alrik.json")));
        Assert.Equal(Path.Combine(scene.Documents, "old saves"), h.Settings.RecentPlaces[0]);
    }

    [Fact]
    public void AFileThatIsThereIsReplacedOnlyAfterTheQuestion()
    {
        using var scene = new Scene();
        var h = scene.H;
        var file = scene.CharacterFile("stat-fick-character.json", "Wren");
        scene.S.Session.New("Ser Alrik");
        scene.S.Run(PickerScreen.SaveCommand);
        h.Click(h.Q("#picker-accept"));
        scene.Settle();
        var question = h.Ui.Dialogs.Open!;
        Assert.Equal("stat-fick-character.json is already in this folder. Replace it?", question.Descendants().First(e => e.HasClass("game-dialog-question")).TextContent);
        Assert.Equal("Replace", h.Document.FocusedElement?.TextContent);
        // Cancel: the file as it was, the picker still open.
        h.Click(question.Descendants().Single(e => e.HasClass("game-dialog-cancel")));
        scene.Settle();
        Assert.Equal("Wren", CharacterFile.Import<SaveGame>(file).Name);
        Assert.NotNull(scene.S.Picker.Open);
        // From the listing too (the row selected puts its name in the field; Enter picks it): Replace writes it.
        scene.Picker.NameField.Value = "another.json";
        scene.Picker.Listing.Focus();
        h.Key("End", "End");
        Assert.Equal("stat-fick-character.json", scene.Picker.NameField.Value);
        h.Key("Enter", "Enter");
        scene.Settle();
        h.Key("Enter", "Enter");
        scene.Settle();
        Assert.Equal("Ser Alrik", CharacterFile.Import<SaveGame>(file).Name);
        Assert.Null(scene.S.Picker.Open);
        Assert.Equal(["Info: Character saved"], scene.Statuses);
    }

    [Fact]
    public void ASaveThatCantBeWrittenSaysSo()
    {
        if (OperatingSystem.IsWindows())
            return; // A folder can't be made unwritable for its owner there: the rule is checked on Linux.
        using var scene = new Scene();
        var h = scene.H;
        var locked = Path.Combine(scene.Documents, "Letters");
        File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            scene.S.Session.New("Ser Alrik");
            scene.S.Run(PickerScreen.SaveCommand);
            scene.Picker.GoTo(locked);
            h.Sounds.Clear();
            h.Click(h.Q("#picker-accept"));
            scene.Settle();
            Assert.Null(scene.S.Picker.Open);
            Assert.Equal(["Problem: Character could not be saved"], scene.Statuses);
            Assert.Equal(["ui-refused"], h.Sounds);
            Assert.Empty(Directory.GetFiles(locked));
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Fact]
    public void WhatIsTypedIsReadAndWhatCantBeDoneIsSaid()
    {
        using var scene = new Scene();
        var h = scene.H;
        scene.S.Session.New("Ser Alrik");
        scene.S.Run(PickerScreen.SaveCommand);
        var picker = scene.Picker;
        picker.NameField.Value = "";
        h.Click(h.Q("#picker-accept"));
        Assert.Equal("Type a file name.", picker.Message);
        picker.NameField.Value = "nowhere/x.json";
        picker.ReadTyped();
        Assert.Equal("nowhere/x.json isn't there.", picker.Message);
        picker.NameField.Value = "a\0b";
        picker.ReadTyped();
        Assert.Equal("a\0b isn't a path here.", picker.Message);
        picker.GoTo(Path.Combine(scene.Documents, "gone"));
        Assert.Equal($"{Path.Combine(scene.Documents, "gone")} isn't there.", picker.Message);
        Assert.Equal(scene.Documents, picker.Folder);
        // Tab completes the last name typed (a folder with its separator), else moves on.
        picker.NameField.Value = "ol";
        picker.NameField.Focus();
        h.Key("Tab", "Tab");
        Assert.Equal("old saves" + Path.DirectorySeparatorChar, picker.NameField.Value);
        Assert.Same(picker.NameField, h.Document.FocusedElement);
        h.Key("Escape", "Escape");
        scene.Settle();
        Assert.Null(scene.S.Picker.Open);
        Assert.Empty(scene.Statuses);
        // To load, nothing chosen.
        scene.S.Run(PickerScreen.LoadCommand);
        h.Click(h.Q("#picker-accept"));
        Assert.Equal("Choose a file.", scene.Picker.Message);
        scene.Picker.NameField.Value = "missing.json";
        scene.Picker.ReadTyped();
        Assert.Equal("missing.json isn't there.", scene.Picker.Message);
    }

    [Fact]
    public void LoadStartsInTheMostRecentPlaceAndMakesTheFilesCharacterANewSlotThatOpens()
    {
        using var scene = new Scene();
        var h = scene.H;
        // No recent place: Documents.
        h.Key("o", "KeyO", KeyModifiers.Control);
        Assert.Equal((PickerMode.Load, scene.Documents), (scene.Picker.Mode, scene.Picker.Folder));
        Assert.Equal("Load character", h.Q("#picker-title").TextContent);
        Assert.Same(scene.Picker.Listing, h.Document.FocusedElement);
        h.Key("Escape", "Escape");
        scene.Settle();
        // The most recent place that is a folder now (one gone is passed by).
        var downloads = Path.Combine(scene.Home, "Downloads");
        h.Settings.AddRecentPlace(downloads);
        h.Settings.AddRecentPlace(Path.Combine(scene.Home, "gone"));
        scene.CharacterFile(Path.Combine("..", "Downloads", "ser alrik.json"), "Ser Alrik");
        scene.S.Session.New("Ser Alrik");
        h.Key("o", "KeyO", KeyModifiers.Control);
        var picker = scene.Picker;
        Assert.Equal(downloads, picker.Folder);
        Assert.Equal(["Documents", "Home", "/", "Downloads"], picker.PlaceButtons.Select(b => b.TextContent));
        Assert.Equal("Recent", h.Q(".picker-places-title").TextContent);
        Assert.Equal("Home / Downloads", h.Q("#picker-where").TextContent);
        // Picked by a double click: a new slot (the name taken: a number added), opened, said.
        h.Sounds.Clear();
        var row = picker.Rows.Single();
        h.Click(row);
        h.Click(row);
        scene.Settle();
        Assert.Null(scene.S.Picker.Open);
        Assert.Equal(("ser-alrik-2", "Ser Alrik"), (scene.S.Session.CurrentSlot, scene.S.Session.Current?.Name));
        Assert.Equal(["Info: Loaded ser alrik.json"], scene.Statuses);
        Assert.DoesNotContain("game-opened", h.Sounds);
        Assert.Equal(downloads, h.Settings.RecentPlaces[0]);
    }

    [Fact]
    public void AFileThatIsntACharacterIsRefusedInTheSessionsWords()
    {
        using var scene = new Scene();
        var h = scene.H;
        File.WriteAllText(Path.Combine(scene.Documents, "emberfall.json"), "{ \"format\": \"emberfall-stat-block\", \"version\": 22, \"character\": { \"stats\": {} } }");
        File.WriteAllText(Path.Combine(scene.Documents, "empty-name.json"), "{ \"$types\": { \"save.game\": 1 }, \"name\": \"\" }");
        scene.S.Run(PickerScreen.LoadCommand);
        h.Sounds.Clear();
        scene.Picker.NameField.Value = "emberfall.json";
        h.Click(h.Q("#picker-accept"));
        scene.Settle();
        Assert.Equal(["Problem: That file is not a valid character save"], scene.Statuses);
        Assert.Equal(["ui-refused"], h.Sounds);
        Assert.Null(scene.S.Session.Current);
        Assert.Empty(scene.S.Session.Slots());
        scene.S.Run(PickerScreen.LoadCommand);
        scene.Picker.NameField.Value = "empty-name.json";
        scene.Picker.ReadTyped();
        scene.Settle();
        Assert.Equal("Problem: That file is not a valid character save: the name is empty", scene.Statuses[^1]);
    }

    [Fact]
    public void ThePlacesAndUpGoToTheirFolders()
    {
        using var scene = new Scene();
        var h = scene.H;
        scene.S.Run(PickerScreen.LoadCommand);
        var picker = scene.Picker;
        h.Click(picker.PlaceButtons[1]);
        Assert.Equal(scene.Home, picker.Folder);
        Assert.Equal("Home", h.Q("#picker-where").TextContent);
        Assert.True(picker.PlaceButtons[1].HasClass("current"));
        Assert.False(picker.PlaceButtons[0].HasClass("current"));
        Assert.Equal(["Documents/", "Downloads/"], Names(picker));
        h.Click(h.Q("#picker-up"));
        Assert.Equal(Path.GetDirectoryName(scene.Home), picker.Folder);
        Assert.Equal(picker.Folder, h.Q("#picker-where").TextContent);
        h.Click(picker.PlaceButtons[2]);
        Assert.Equal("/", picker.Folder);
        Assert.True(h.Q("#picker-up").IsDisabled);
        // The listing's keys: arrows select, Enter goes into a folder, Backspace goes up.
        picker.GoTo(scene.Home);
        picker.Listing.Focus();
        // Up with nothing selected: the last; Up at the first: still the first.
        h.Key("ArrowUp", "ArrowUp");
        Assert.Equal(1, picker.Selected);
        picker.Select(-1);
        h.Key("ArrowDown", "ArrowDown");
        Assert.Equal(0, picker.Selected);
        h.Key("ArrowUp", "ArrowUp");
        Assert.Equal(0, picker.Selected);
        h.Key("ArrowDown", "ArrowDown");
        h.Key("ArrowUp", "ArrowUp");
        Assert.Equal(0, picker.Selected);
        Assert.Equal("true", picker.Rows[0].GetAttribute("aria-selected"));
        h.Key("Enter", "Enter");
        Assert.Equal(scene.Documents, picker.Folder);
        Assert.Equal(-1, picker.Selected);
        h.Key("Backspace", "Backspace");
        Assert.Equal(scene.Home, picker.Folder);
        h.Key("End", "End");
        Assert.Equal(1, picker.Selected);
        h.Key("Home", "Home");
        Assert.Equal(0, picker.Selected);
        // An empty folder says so.
        picker.GoTo(Path.Combine(scene.Documents, "Letters"));
        Assert.Equal("No folders or .json files here.", h.Q(".picker-empty").TextContent);
    }

    [Fact]
    public void AFolderIsNamedFromTheDeepestPlaceThatHoldsIt()
    {
        using var scene = new Scene();
        var h = scene.H;
        // Documents holding the home folder (a Documents folder named outside it): the home's folders by Home.
        var outer = Path.GetDirectoryName(scene.Home)!;
        scene.S.Picker.Documents = () => outer;
        scene.S.Run(PickerScreen.LoadCommand);
        var picker = scene.Picker;
        Assert.Equal(["Documents", "Home", "/"], picker.PlaceButtons.Select(b => b.TextContent));
        picker.GoTo(Path.Combine(scene.Home, "Downloads"));
        Assert.Equal("Home / Downloads", h.Q("#picker-where").TextContent);
        Directory.CreateDirectory(Path.Combine(outer, "letters"));
        picker.GoTo(Path.Combine(outer, "letters"));
        Assert.Equal("Documents / letters", h.Q("#picker-where").TextContent);
    }

    [Fact]
    public void OnlyTheRowsNearTheViewAreMade()
    {
        using var scene = new Scene();
        var h = scene.H;
        scene.S.SceneFolders();
        h.Settings.AddRecentPlace(ScreenParts.Documents + "/Stat Fick archive");
        scene.S.Run(PickerScreen.LoadCommand);
        var picker = scene.Picker;
        Assert.Equal(2000, picker.Entries.Count);
        var view = picker.Listing.ClientHeight;
        Assert.InRange(view, 200, 600);
        int[] Indexes() => [.. picker.Rows.Select(r => int.Parse(r.GetAttribute("data-index")!))];
        // The rows in the view (the last one partly) and the overscan after them.
        var inView = (int)Math.Ceiling(view / (double)FilePicker.RowHeight);
        Assert.Equal(Enumerable.Range(0, inView + FilePicker.Overscan), Indexes());
        // The listing as high as every row: the spacers stand for the rows not made.
        Assert.Equal(2000 * FilePicker.RowHeight, picker.Listing.ScrollHeight);
        // Scrolled: the rows near the new view, the overscan on both sides.
        picker.Listing.ScrollTop = FilePicker.RowHeight * 1000;
        h.Frame();
        h.Frame();
        Assert.Equal(Enumerable.Range(1000 - FilePicker.Overscan, inView + 2 * FilePicker.Overscan), Indexes());
        Assert.Equal(2000 * FilePicker.RowHeight, picker.Listing.ScrollHeight);
        // The last selected: scrolled to it, the rows there made, the row kept as the selection moves among them.
        picker.Listing.Focus();
        h.Key("End", "End");
        h.Frame();
        Assert.Equal(1999, Indexes()[^1]);
        Assert.Equal(1999 * FilePicker.RowHeight + FilePicker.RowHeight - view, (int)picker.Listing.ScrollTop);
        var row = picker.Rows.Single(r => r.GetAttribute("data-index") == "1998");
        h.Key("ArrowUp", "ArrowUp");
        Assert.Same(row, picker.Rows.Single(r => r.HasClass("selected")));
        Assert.Equal("character-1999.json", picker.Entries[picker.Selected].Name);
    }

    [Fact]
    public void OnePickerAtATimeAndAPickerClosedElsewhereIsCancelled()
    {
        using var scene = new Scene();
        var h = scene.H;
        scene.S.Session.New("Ser Alrik");
        scene.S.Run(PickerScreen.LoadCommand);
        var first = scene.Picker;
        scene.S.Run(PickerScreen.LoadCommand);
        scene.S.Run(PickerScreen.SaveCommand);
        Assert.Same(first, scene.S.Picker.Open);
        Assert.Single(h.Ui.Windows.Layers);
        h.Ui.Windows.CloseTop();
        scene.Settle();
        Assert.Null(scene.S.Picker.Open);
        Assert.Empty(scene.Statuses);
        // Its × and its Cancel cancel.
        scene.S.Run(PickerScreen.SaveCommand);
        h.Click(h.Q(".picker-layer .modal-close"));
        scene.Settle();
        Assert.Null(scene.S.Picker.Open);
        scene.S.Run(PickerScreen.SaveCommand);
        h.Click(h.Q(".picker-cancel"));
        scene.Settle();
        Assert.Null(scene.S.Picker.Open);
        Assert.Empty(scene.Statuses);
        Assert.False(File.Exists(Path.Combine(scene.Documents, "stat-fick-character.json")));
    }

    [Fact]
    public void TheScreensRegisterTheDesignsCommandsAndTheirSheets()
    {
        using var scene = new Scene();
        var commands = scene.H.Ui.Commands;
        // "The commands and who registers them" (Docs/Game/Foundation.md, "The seams"): step 10's five.
        Assert.All(new[] { "open-settings", "open-slots", "open-slot", "save-character", "load-character" }, name => Assert.True(commands.IsRegistered(name), name));
        // Ctrl+S and Ctrl+O (bound by the document, step 8) reach them.
        Assert.Equal(("save-character", "load-character"), (commands.Bindings.Single(b => b.Keys == "Ctrl+S").Command, commands.Bindings.Single(b => b.Keys == "Ctrl+O").Command));
        Assert.Equal(["ui/styles/settings", "ui/styles/slots", "ui/styles/picker"], scene.H.Ui.Sheets.Skip(GameUi.Stylesheets.Count));
    }

    [Theory]
    [InlineData(0, "0 bytes")]
    [InlineData(1, "1 byte")]
    [InlineData(1023, "1023 bytes")]
    [InlineData(1024, "1 KB")]
    [InlineData(1536, "1.5 KB")]
    [InlineData(10 * 1024, "10 KB")]
    [InlineData(15770, "15 KB")]
    [InlineData(5 * 1024 * 1024 + 1, "5 MB")]
    public void ASizeAsThePickerWritesIt(long bytes, string text) => Assert.Equal(text, FilePicker.Size(bytes));

    [Fact]
    public void ThePickersDatesAreThePlayersZones()
    {
        using var scene = new Scene();
        var h = scene.H;
        scene.S.Picker.TimeZone = TimeZoneInfo.CreateCustomTimeZone("minus-three", TimeSpan.FromHours(-3), "-03:00", "-03:00");
        var file = scene.CharacterFile("wren.json", "Wren");
        File.SetLastWriteTimeUtc(file, ScreenParts.Utc(11, 13, 1, 30));
        scene.S.Run(PickerScreen.LoadCommand);
        var row = scene.Picker.Rows.Single(r => !r.HasClass("is-folder"));
        Assert.Equal("12 November 2026, 22:30", row.Descendants().First(e => e.HasClass("picker-date-cell")).TextContent);
        Assert.Equal($"{new FileInfo(file).Length} bytes", row.Descendants().First(e => e.HasClass("picker-size-cell")).TextContent);
    }
}
