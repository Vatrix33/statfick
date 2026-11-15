using Statventure.Game.Screens.Picker;
using Statventure.Game.Screens.Slots;
using Statventure.Game.Ui;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The slot list and the damaged-save question (Q1, Q9, Q17, Q21, Q24, Q25, Q29; step 10): every slot the last played
/// first with its name, when it was last played in the player's time zone and whether it is damaged; Open (a readable slot
/// opens and the list closes; a damaged one asks the question) and Delete (asked once, its action focused; the save and
/// backup removed); the question's answers (Back changes nothing; Start new and Use the backup write the slot and open
/// it, the damaged file kept aside); "Load Character…"; the list's own status line, which shows a message only while the
/// list is the topmost window (the manager's rule, 2026-11-14), a stand-in page line beside it showing what is said while
/// no window is open. The page's <c>game-opened</c> is step 9's: these screens never play it.
/// </summary>
public class SlotsScreenTests
{
    static GameUiHarness Harness() => new(1000, 860);

    /// <summary>A stand-in for the page's status line (step 9's): what is said while no window is open.</summary>
    static List<string> PageLine(GameUiHarness h)
    {
        var shown = new List<string>();
        h.Ui.StatusShown += (text, kind) =>
        {
            if (h.Ui.Windows.Layers.Count == 0)
                shown.Add($"{kind}: {text}");
        };
        return shown;
    }

    static List<UiElement> Rows(GameUiHarness h) => [.. h.Document.QuerySelectorAll(".slot-row")];

    static string Text(UiElement row, string selector) => row.Descendants().FirstOrDefault(e => e.Matches(selector))?.TextContent ?? "";

    [Fact]
    public void EverySlotIsListedTheLastPlayedFirst()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.SeveralCharacters();
        Assert.True(s.Run(SlotsScreen.OpenCommand));
        Assert.True(h.Ui.Windows.IsOpen(s.Slots.Layer));
        var rows = Rows(h);
        Assert.Equal(["ser-alrik", "brother-cadfael", "mira-of-the-vale", "wren", "old-tom-bombadil-the-wanderer-of-the-hills"], rows.Select(r => r.GetAttribute("data-slot")));
        // A damaged slot by its backup's name when that reads, else by its file's name.
        Assert.Equal(["Ser Alrik", "Brother Cadfael", "mira-of-the-vale", "Wren", "Old Tom Bombadil the Wanderer of the Hills"], rows.Select(r => Text(r, "strong")));
        Assert.Equal(["", "Damaged", "Damaged", "", ""], rows.Select(r => Text(r, ".slot-damaged")));
        Assert.Equal(["13 November 2026, 14:03", "12 November 2026, 09:12", "10 November 2026, 21:47", "8 November 2026, 18:30", "30 October 2026, 07:05"],
            rows.Select(r => Text(r, ".slot-time")));
        Assert.Equal([false, true, true, false, false], rows.Select(r => r.HasClass("is-damaged")));
        Assert.Equal(["Open Ser Alrik", "Delete Ser Alrik"], rows[0].ElementChildren.Where(e => e.Tag == "button").Select(b => b.GetAttribute("aria-label")));
        Assert.Equal(["ui-open"], h.Sounds);
        Assert.Equal("", s.Q("#slots-status").TextContent);
        // A damaged row's stripe in the blood of its "Damaged" (slots.css), a readable one's not (too few pixels for the
        // screen comparison's limits to tell).
        h.Frame();
        Assert.Equal(rows[1].Descendants().Single(e => e.HasClass("slot-damaged")).Style!.Color, rows[1].Style!.BorderLeftColor);
        Assert.NotEqual(rows[1].Style!.BorderLeftColor, rows[0].Style!.BorderLeftColor);
    }

    [Fact]
    public void TheTimesAreThePlayersZones()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h, TimeZoneInfo.CreateCustomTimeZone("plus-five-thirty", TimeSpan.FromHours(5.5), "+05:30", "+05:30"));
        s.Character("Ser Alrik", ScreenParts.Utc(11, 13, 20, 3), ScreenParts.Utc(11, 13, 19, 0));
        s.Run(SlotsScreen.OpenCommand);
        Assert.Equal("14 November 2026, 01:33", Text(Rows(h)[0], ".slot-time"));
        // The damaged-save question's backup time too.
        ScreenParts.Damage(Path.Combine(s.Saves, "ser-alrik.dat"));
        s.Run(SlotsScreen.OpenSlotCommand, "ser-alrik");
        Assert.Equal("Use the backup (saved 14 November 2026, 00:30)", h.Q(".damaged-save-backup").TextContent);
        Assert.Equal("13 November 2026, 14:03", ScreenTimes.Write(new DateTimeOffset(ScreenParts.Utc(11, 13, 14, 3)), TimeZoneInfo.Utc));
        Assert.Equal("1 March 2026, 00:00", ScreenTimes.Write(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero), TimeZoneInfo.Utc));
    }

    [Fact]
    public void WithNoSlotsTheListSaysSo()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.Run(SlotsScreen.OpenCommand);
        Assert.Empty(Rows(h));
        Assert.Equal("No characters yet. New character on the start screen makes one, and Load Character… loads a character file.", s.Q(".slots-empty p").TextContent);
    }

    [Fact]
    public void OpenOpensAReadableSlotAndTheListCloses()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.SeveralCharacters();
        s.Run(SlotsScreen.OpenCommand);
        h.Sounds.Clear();
        h.Click(h.Q(".slot-row[data-slot=\"wren\"] .slot-open"));
        h.Frame();
        Assert.Equal(("wren", "Wren"), (s.Session.CurrentSlot, s.Session.Current?.Name));
        Assert.False(h.Ui.Windows.IsOpen(s.Slots.Layer));
        Assert.Equal("wren", h.Settings.LastCharacter);
        // The page plays game-opened (step 9's); the list's closing is what sounds here.
        Assert.Equal(["ui-close"], h.Sounds);
        // open-slot opens a slot without the list too.
        Assert.True(s.Run(SlotsScreen.OpenSlotCommand, "ser-alrik"));
        Assert.Equal("ser-alrik", s.Session.CurrentSlot);
        Assert.DoesNotContain("game-opened", h.Sounds);
        // Without a slot it does nothing.
        Assert.True(s.Run(SlotsScreen.OpenSlotCommand));
        Assert.Equal("ser-alrik", s.Session.CurrentSlot);
    }

    [Fact]
    public void ADamagedSlotWithABackupThatReadsAsksTheQuestionAndBackChangesNothing()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.SeveralCharacters();
        var before = Directory.GetFiles(s.Saves).Order().Select(f => (f, File.GetLastWriteTimeUtc(f))).ToList();
        s.Run(SlotsScreen.OpenCommand);
        h.Click(h.Q(".slot-row[data-slot=\"brother-cadfael\"] .slot-open"));
        h.Frame();
        var question = s.Slots.Question!;
        Assert.True(h.Ui.Windows.IsOpen(question));
        Assert.Equal("Brother Cadfael's save is damaged.", Text(question, ".game-dialog-question"));
        Assert.Equal("Whatever you choose, the damaged file is kept aside in the saves folder.", Text(question, ".damaged-save-note"));
        var buttons = question.Descendants().Where(e => e.Tag == "button").ToList();
        Assert.Equal(["Back", "Start new", "Use the backup (saved 12 November 2026, 08:30)"], buttons.Select(b => b.TextContent));
        Assert.Same(buttons[2], h.Document.FocusedElement);
        Assert.True(buttons[2].HasClass("game-dialog-accept"));
        Assert.False(buttons[1].HasClass("game-dialog-accept"));
        Assert.Null(s.Session.Current);
        h.Click(buttons[0]);
        Assert.False(h.Ui.Windows.IsOpen(question));
        Assert.True(h.Ui.Windows.IsOpen(s.Slots.Layer));
        Assert.Null(s.Session.Current);
        // Escape is Back as well.
        s.Run(SlotsScreen.OpenSlotCommand, "brother-cadfael");
        var again = s.Slots.Question!;
        h.Key("Escape", "Escape");
        Assert.False(h.Ui.Windows.IsOpen(again));
        Assert.True(h.Ui.Windows.IsOpen(s.Slots.Layer));
        Assert.Null(s.Session.Current);
        // The folder as it is once everything asked of the background writer is written.
        h.Storage.Flush();
        Assert.Equal(before, Directory.GetFiles(s.Saves).Order().Select(f => (f, File.GetLastWriteTimeUtc(f))));
    }

    [Fact]
    public void UseTheBackupWritesItIntoTheSlotAndOpensIt()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.SeveralCharacters();
        s.Run(SlotsScreen.OpenCommand);
        h.Click(h.Q(".slot-row[data-slot=\"brother-cadfael\"] .slot-open"));
        h.Sounds.Clear();
        h.Click(h.Q(".damaged-save-backup"));
        h.Frame();
        Assert.Equal(("brother-cadfael", "Brother Cadfael"), (s.Session.CurrentSlot, s.Session.Current?.Name));
        Assert.Empty(h.Ui.Windows.Layers);
        // The slot is written by the storage's background writer, which keeps the damaged save aside: written first.
        h.Storage.Flush();
        Assert.True(File.Exists(Path.Combine(s.Saves, "brother-cadfael.dat.damaged")));
        Assert.DoesNotContain("game-opened", h.Sounds);
    }

    [Fact]
    public void WithoutABackupThatReadsStartNewIsTheActionAndKeepsTheFilesName()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.SeveralCharacters();
        s.Run(SlotsScreen.OpenSlotCommand, "mira-of-the-vale");
        var question = s.Slots.Question!;
        Assert.Equal("mira-of-the-vale's save is damaged.", Text(question, ".game-dialog-question"));
        var buttons = question.Descendants().Where(e => e.Tag == "button").ToList();
        Assert.Equal(["Back", "Start new"], buttons.Select(b => b.TextContent));
        Assert.Same(buttons[1], h.Document.FocusedElement);
        Assert.True(buttons[1].HasClass("game-dialog-accept"));
        h.Key("Enter", "Enter");
        h.Frame();
        Assert.Equal(("mira-of-the-vale", "mira-of-the-vale"), (s.Session.CurrentSlot, s.Session.Current?.Name));
        Assert.Empty(h.Ui.Windows.Layers);
        // The slot is written by the storage's background writer, which keeps the damaged save aside: written first
        // (2026-11-15: read at once, the writer not yet run on a busy machine).
        h.Storage.Flush();
        Assert.True(File.Exists(Path.Combine(s.Saves, "mira-of-the-vale.dat.damaged")));
    }

    [Fact]
    public void DeleteAsksOnceThenRemovesTheSaveAndItsBackup()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.SeveralCharacters();
        s.Run(SlotsScreen.OpenCommand);
        h.Click(h.Q(".slot-row[data-slot=\"ser-alrik\"] .slot-delete"));
        var confirm = h.Ui.Dialogs.Open!;
        Assert.Equal("Delete Ser Alrik? Its save and its backup are removed.", Text(confirm, ".game-dialog-question"));
        Assert.Equal("Delete", h.Document.FocusedElement?.TextContent);
        // Cancel: nothing.
        h.Click(confirm.Descendants().Single(e => e.HasClass("game-dialog-cancel")));
        h.Frame();
        Assert.True(File.Exists(Path.Combine(s.Saves, "ser-alrik.dat")));
        Assert.Equal(5, Rows(h).Count);
        // Delete: the save and backup gone, the row with them, the focus on the row now in its place.
        h.Click(h.Q(".slot-row[data-slot=\"ser-alrik\"] .slot-delete"));
        h.Sounds.Clear();
        h.Key("Enter", "Enter");
        h.Frame();
        Assert.False(File.Exists(Path.Combine(s.Saves, "ser-alrik.dat")));
        Assert.False(File.Exists(Path.Combine(s.Saves, "ser-alrik.dat.bak")));
        Assert.Equal(["brother-cadfael", "mira-of-the-vale", "wren", "old-tom-bombadil-the-wanderer-of-the-hills"], Rows(h).Select(r => r.GetAttribute("data-slot")));
        Assert.Equal(["game-deleted"], h.Sounds);
        Assert.Same(h.Q(".slot-row[data-slot=\"brother-cadfael\"] .slot-open"), h.Document.FocusedElement);
        // The last row deleted: the focus on the row before it; the last of all: on "Load Character…".
        foreach (var slot in new[] { "old-tom-bombadil-the-wanderer-of-the-hills", "wren", "mira-of-the-vale", "brother-cadfael" })
        {
            h.Click(h.Q($".slot-row[data-slot=\"{slot}\"] .slot-delete"));
            h.Key("Enter", "Enter");
            h.Frame();
            if (slot == "old-tom-bombadil-the-wanderer-of-the-hills")
                Assert.Same(h.Q(".slot-row[data-slot=\"wren\"] .slot-open"), h.Document.FocusedElement);
        }
        Assert.Empty(Rows(h));
        Assert.Same(h.Q("#slots-load-file"), h.Document.FocusedElement);
        Assert.NotNull(h.Document.QuerySelector(".slots-empty"));
    }

    [Fact]
    public void TheListFollowsTheSlots()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.Run(SlotsScreen.OpenCommand);
        Assert.Empty(Rows(h));
        s.Session.New("Wren");
        s.Session.Leave();
        h.Frame();
        // A character opened closes the list (whoever opened it).
        Assert.False(h.Ui.Windows.IsOpen(s.Slots.Layer));
        s.Run(SlotsScreen.OpenCommand);
        Assert.Equal(["Wren"], Rows(h).Select(r => Text(r, "strong")));
        s.Session.Delete("wren");
        h.Frame();
        Assert.Empty(Rows(h));
    }

    [Fact]
    public void LoadCharacterRunsTheFilePickerAndItsRefusalShowsOnTheListsLineAlone()
    {
        using var h = Harness();
        var s = ScreenParts.Attach(h);
        s.SceneFolders();
        var page = PageLine(h);
        s.Run(SlotsScreen.OpenCommand);
        h.Click(h.Q("#slots-load-file"));
        h.Frame();
        Assert.Equal(PickerMode.Load, s.OpenPicker.Mode);
        // A file that isn't a character, picked: refused on the list's line (the list is the topmost window again).
        s.OpenPicker.GoTo(ScreenParts.Documents + "/old saves");
        s.OpenPicker.Activate(0);
        h.Frame();
        h.Frame();
        Assert.Null(s.Picker.Open);
        Assert.True(h.Ui.Windows.IsOpen(s.Slots.Layer));
        var status = s.Q("#slots-status");
        Assert.Equal("That file is not a valid character save", status.TextContent);
        Assert.True(status.HasClass("is-error"));
        Assert.Empty(page);
        Assert.Equal("ui-refused", h.Sounds[^1]);
        // Said while another window is over the list: not on its line; with no window open: the page's.
        h.Ui.Windows.Open(h.Layer("over"));
        h.Ui.Status("Over the list");
        Assert.Equal("That file is not a valid character save", status.TextContent);
        h.Ui.Windows.CloseTop();
        h.Ui.Status("News for the list", StatusKind.Info);
        Assert.Equal("News for the list", status.TextContent);
        Assert.True(status.HasClass("is-success"));
        h.Ui.Windows.CloseTop();
        h.Ui.Status("Character saved");
        Assert.Equal(["Info: Character saved"], page);
        Assert.Equal("News for the list", status.TextContent);
        // Opened again, its line starts empty.
        s.Run(SlotsScreen.OpenCommand);
        Assert.Equal("", status.TextContent);
    }
}
