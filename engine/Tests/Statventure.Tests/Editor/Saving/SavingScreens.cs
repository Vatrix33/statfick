using Statventure.Audio;
using Statventure.Packs;
using Statventure.Tests.Infrastructure;
using static Statventure.Tests.Editor.Opening.OpeningTesting;
using static Statventure.Tests.Editor.Saving.SavingTesting;

namespace Statventure.Tests.Editor.Saving;

/// <summary>
/// Step 11's screens against Chromium (P36; Docs/Editor.md, "How it works", step 11): the Save As dialog with its ticks over
/// a pack it can't fully read (the entries it reads ticked, one kept unread ticked with its note, the others with their
/// reasons, the damaged one not offered), the New mod dialog with its names and a problem on its line, and the mod's info in
/// its form of its own. Mods over the repo's packs (the harness's dev setup), written into the run's scratch folder; only
/// file names are in the markup. A dialog's focus is on its first button, moved there by code (a text field's caret
/// doesn't carry over to the capture).
/// </summary>
public sealed class SavingScreens : IEditorFixtures
{
    public IEnumerable<EditorFixture> Fixtures =>
    [
        new("saving-save-as", 1000, 800, SaveAs),
        new("saving-new-mod", 1000, 640, NewMod),
        new("saving-mod-info", 1000, 640, Info),
    ];

    /// <summary>The fixtures' scratch folder, emptied and made again each time (the fixtures are built at both scales).</summary>
    static string Folder(string name)
    {
        var folder = Path.Combine(PerUserFolders.Root, "editor-fixtures", "saving", name);
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);
        return folder;
    }

    static string Mod(string folder, string name, Action<PackWriter> fill)
    {
        var writer = new PackWriter();
        fill(writer);
        var file = Path.Combine(folder, "mods", name);
        writer.Write(file);
        return file;
    }

    static void SaveAs(EditorHarness editor)
    {
        var folder = Folder("save-as");
        editor.Session.PlayerFolder = folder;
        var mod = Mod(folder, "old-halls.pak", w =>
        {
            w.AddRecord("audio/rooms/crypt", new RoomSpec(2.5, 0.5, 0.25));
            w.Add("notes/readme.txt", "Old halls."u8);
            AddUnreadable(w, "parts/arm", "unknown kind");
            AddUnreadable(w, "parts/bad", "damaged");
            AddUnreadable(w, "parts/room", "newer version");
        });
        Damage(mod, "parts/bad");
        editor.Session.Open(mod);
        // The game's audio closed, so the entry pane doesn't scroll (a dialog over a composited scroller has grayscale
        // text at 1.65, an open item: Docs/Editor.md, step 12, "Found").
        editor.List().Toggle("audio");
        editor.Frame();
        _ = editor.Run(() => editor.Saving().SaveAs());
        editor.Saving().SaveAsShown!.Tick(editor.Session.Entry("parts/arm")!, true);
        editor.View.Dialogs.Top!.Buttons.ElementChildren.First().Focus();
        editor.Frame();
    }

    static void NewMod(EditorHarness editor)
    {
        var folder = Folder("new-mod");
        editor.Session.PlayerFolder = folder;
        Mod(folder, "brighter-halls.pak", w => w.AddRecord(ModInfo.Path, new ModInfo { Name = "Brighter Halls" }));
        _ = editor.Run(() => editor.Saving().NewMod());
        var dialog = editor.Saving().NewModShown!;
        dialog.NameField.Value = "Brighter Halls";
        dialog.FileField.Value = Statventure.Editor.Session.EditorSession.NewModFileName("Brighter Halls");
        dialog.Act();
        editor.View.Dialogs.Top!.Buttons.ElementChildren.First().Focus();
        editor.Frame();
    }

    static void Info(EditorHarness editor)
    {
        var folder = Folder("mod-info");
        editor.Session.PlayerFolder = folder;
        var mod = Mod(folder, "brighter-halls.pak", w =>
        {
            w.AddRecord(ModInfo.Path, new ModInfo
            {
                Name = "Brighter Halls",
                Author = "Vatrix",
                Version = "1.2",
                Description = "Every hall of the game echoes longer.\nTry the cathedral at night.",
            });
            w.AddRecord("audio/rooms/hall", new RoomSpec(4, 0.5, 0.35));
        });
        editor.Session.Open(mod);
        editor.Session.Select(editor.Session.Entry(ModInfo.Path));
        editor.Frame();
    }
}
