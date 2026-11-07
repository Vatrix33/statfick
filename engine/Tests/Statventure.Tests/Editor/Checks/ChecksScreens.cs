using Statventure.Audio;
using Statventure.Packs;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Editor.Checks;

/// <summary>
/// Step 10's screens against Chromium (P36; Docs/Editor.md, "How it works", step 10): a form with problems (a mod's copy
/// of one of the game's themes with its id, mode and room wrong: its own problems, the first per field, and the room's
/// found across entries, under their rows; the list's mark) and a reference field's list open (the room's field
/// focused, "c" typed and Down pressed: the rooms holding it, the game's dimmed, the first moved to, and the flag). Over the repo's packs; the mod written into
/// the run's scratch folder.
/// </summary>
public sealed class ChecksScreens : IEditorFixtures
{
    const string Vale = "audio/themes/alder-vale";

    /// <summary>
    /// The fixtures' height (the status line's top on a whole device pixel at 1.65 too, (665 − 25) × 1.65 = 1056: tried
    /// against the status line's grayscale text there, which it didn't change; Docs/Editor.md, "How it works", step 10,
    /// "Found").
    /// </summary>
    public const int Height = 665;

    public IEnumerable<EditorFixture> Fixtures =>
    [
        new("checks-problems", 1280, Height, Problems),
        new("references-list", 1280, Height, List),
    ];

    /// <summary>The mod (written again each time: the fixtures are built at both scales).</summary>
    static string FixturePack(string name, ThemeSpec theme)
    {
        var writer = new PackWriter();
        writer.AddRecord(Vale, theme);
        var folder = Path.Combine(PerUserFolders.Root, "editor-fixtures", Path.GetFileNameWithoutExtension(name));
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, name);
        writer.Write(file);
        return file;
    }

    static ThemeSpec Theme() => (ThemeSpec)FormTesting.AudioPack()[Vale];

    static void Problems(EditorHarness editor)
    {
        var session = editor.Session;
        session.Open(FixturePack("bad.pak", Theme() with { Id = "alder-vail", Mode = "blues", Room = "attic" }));
        session.Select(session.Entry(Vale));
        session.Checks.Run();
        editor.Frame();
    }

    static void List(EditorHarness editor)
    {
        var session = editor.Session;
        session.Open(FixturePack("vale.pak", Theme() with { Room = "cathedral" }));
        session.Select(session.Entry(Vale));
        session.Checks.Run();
        editor.Frame();
        // Focused without a press (which would scroll the pane to the field), its text replaced by "c" typed.
        editor.Row("$.room").Control("input", "text").Focus();
        editor.Key("a", "KeyA", Statventure.Platform.KeyModifiers.Control);
        editor.Key("c", "KeyC");
        // The first name moved to.
        editor.Key("ArrowDown", "ArrowDown");
        Assert.Equal(0, editor.View.Pane(Statventure.Editor.Shell.EditorPane.Form).ScrollTop);
        Assert.NotNull(editor.References().List);
    }
}
