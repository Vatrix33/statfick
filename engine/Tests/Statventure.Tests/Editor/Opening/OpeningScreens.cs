using Statventure.Audio;
using Statventure.Editor.Browser;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Storage;
using Statventure.Tests.Infrastructure;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Opening;

/// <summary>
/// Step 8's screens against Chromium (P36; Docs/Editor.md, "Opening packs (step 8)"): the entry list with every mark
/// a pack that takes changes can have (a copy of a game entry, changed, added with problems, a game entry changed in the
/// session, the game's dimmed, the selection and the active item), the list of a pack it can't fully read (read-only,
/// each unreadable entry with its reason), and the file browser opening a pack (places, a folder's listing with sizes
/// and dates, a row selected). Over the repo's packs (the harness's dev setup); the mods are written into the run's
/// scratch folder, and the browser's folder is a listing of the fixture's own, so no path of this machine's is in the
/// markup.
/// </summary>
public sealed class OpeningScreens : IEditorFixtures
{
    public IEnumerable<EditorFixture> Fixtures =>
    [
        new("list-marks", 1000, 640, Marks),
        new("list-unreadable", 1000, 800, Unreadable),
        new("browser", 1000, 640, Browser),
    ];

    /// <summary>A pack in the run's scratch folder (written again each time: the fixtures are built at both scales).</summary>
    static string FixturePack(string name, Action<PackWriter> fill)
    {
        var writer = new PackWriter();
        fill(writer);
        var folder = Path.Combine(PerUserFolders.Root, "editor-fixtures", Path.GetFileNameWithoutExtension(name));
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, name);
        writer.Write(file);
        return file;
    }

    static void Marks(EditorHarness editor)
    {
        var session = editor.Session;
        session.Open(FixturePack("my-mod.pak", w =>
        {
            w.AddRecord("audio/rooms/hall", new RoomSpec(9, 0.5, 0.5));
            w.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5));
            w.Add("notes/todo.txt", "Tune the crypt."u8);
        }));
        var crypt = session.Entry("audio/rooms/crypt")!;
        session.Commit(crypt, crypt.Draft!.Set("Seconds", 4.0));
        session.Add("audio/rooms/new", RecordDraft.Blank(typeof(RoomSpec)));
        var room = session.Entries.First(e => e.Source == EntrySource.Game && e.Path.StartsWith("audio/rooms/", StringComparison.Ordinal));
        session.Commit(room, room.Draft!.Set("Wet", 0.75));
        session.Select(crypt);
        editor.List().Tree.Focus();
        editor.Frame();
    }

    static void Unreadable(EditorHarness editor)
    {
        var file = FixturePack("broken.pak", w =>
        {
            w.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5));
            foreach (var (how, _) in UnreadableWays)
                AddUnreadable(w, "bad/" + how.Replace(' ', '-'), how);
        });
        Damage(file, "bad/damaged");
        editor.Session.Open(file);
        // The game's audio closed, so the pack's unreadable entries show under the notice.
        editor.List().Toggle("audio");
        editor.Frame();
    }

    /// <summary>The fixture's mods folder (not on this machine: the browser lists it from <see cref="FileBrowserPart.ListOverride"/>).</summary>
    const string Mods = "/home/player/.local/share/stat-fick/mods";

    static void Browser(EditorHarness editor)
    {
        var part = editor.Browser();
        part.TimeZone = TimeZoneInfo.Utc;
        part.PlacesOverride = () =>
        [
            new("Mods", Mods, BrowserPlaceKind.Mods),
            new("Game packs", "/home/player/stat-fick/engine/Assets/Packs", null),
            new("Documents", "/home/player/Documents", BrowserPlaceKind.Documents),
            new("Home", "/home/player", BrowserPlaceKind.Home),
            new("/", "/", BrowserPlaceKind.Root),
            new("old", Mods + "/old", BrowserPlaceKind.Recent),
        ];
        var time = new DateTime(2026, 11, 7, 16, 50, 0, DateTimeKind.Utc);
        FolderEntry[] entries =
        [
            new("old", Mods + "/old", true, 0, time, false),
            new("alpha.pak", Mods + "/alpha.pak", false, 1536, time, false),
            new("My Mod.pak", Mods + "/My Mod.pak", false, 12, time.AddDays(-3).AddHours(-5), false),
            new("notes.txt", Mods + "/notes.txt", false, 5, time, false),
            new("zeta.pak", Mods + "/zeta.pak", false, 2_457_600, time.AddMonths(-2), false),
        ];
        part.ListOverride = (folder, endings, hidden) => folder == Mods
            ? [.. entries.Where(e => e.IsFolder || FileBrowser.Shows(e.Name, endings))]
            : [];
        _ = editor.Run(() => part.Pick(new BrowserRequest("Open a pack", "Open", FileBrowserPart.PackEndings, StartFolder: Mods)));
        part.Current!.Select(1);
        editor.Frame();
    }
}
