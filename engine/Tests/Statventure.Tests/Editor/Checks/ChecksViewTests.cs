using Statventure.Audio;
using Statventure.Editor.Checks;
using Statventure.Editor.Form;
using Statventure.Editor.Session;
using Statventure.Records;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Editor.Opening;
using Statventure.UI.Dom;
using static Statventure.Tests.Editor.Checks.CheckTesting;

namespace Statventure.Tests.Editor.Checks;

/// <summary>
/// The checks in the editor's view (M11-12, P16, P17): the checks across entries a pause after the last change, on the
/// document's clock (each change starting it again); their problems where step 9's form and step 8's list show an
/// entry's problems: under the field's row (the first per field), at the form's top, the list's mark.
/// </summary>
public class ChecksViewTests
{
    const string Vale = "audio/themes/alder-vale";

    static ThemeSpec Theme(string room) => Game<ThemeSpec>(Vale) with { Room = room };

    static string[] Marks(EditorHarness editor, string path)
    {
        var row = editor.List().Row(editor.Session.At(path)) ?? throw new InvalidOperationException($"no row for {path}");
        return [.. row.ElementChildren.First(c => c.HasClass("marks")).ElementChildren.Select(m => m.TextContent)];
    }

    /// <summary>
    /// The checks across entries run once the pause has passed since the last change (P17): opening a pack starts it;
    /// a change before it ends starts it again; until it ends, nothing across entries shows.
    /// </summary>
    [Fact]
    public void TheChecksAcrossEntriesRunAPauseAfterTheLastChange()
    {
        Assert.Equal(200, EntryChecks.Pause);
        using var editor = OpenMod(w => w.AddRecord(Vale, Theme("attic")));
        var entry = editor.Session.At(Vale);
        Assert.True(editor.ChecksPart().Waiting);
        editor.Wait(EntryChecks.Pause - 40);
        Assert.Empty(entry.Problems);
        Assert.True(editor.Session.Checks.Pending);
        // A change before the pause ends: it starts again.
        editor.Session.Change(entry, "Title", "Alder Vale, again");
        editor.Wait(EntryChecks.Pause - 40);
        Assert.Empty(entry.Problems);
        Assert.True(editor.Session.Checks.Pending);
        editor.Wait(60);
        Assert.False(editor.Session.Checks.Pending);
        Assert.False(editor.ChecksPart().Waiting);
        Assert.Equal(["no room \"attic\" (audio/rooms/attic) (at $.room)"], entry.Shown());
        // Nothing changed: no pause runs.
        editor.Wait(EntryChecks.Pause * 2);
        Assert.False(editor.ChecksPart().Waiting);
    }

    /// <summary>
    /// The problems show at their fields (P16): under the row of each field, the first problem only; the row's name red;
    /// the list marks the entry with their count; a problem of the whole entry at the form's top.
    /// </summary>
    [Fact]
    public void ProblemsShowAtTheirFieldsAndInTheList()
    {
        using var editor = OpenMod(w =>
        {
            w.AddRecord(Vale, Theme("attic") with { Id = "alder-vail" });
            w.AddRecord("audio/rooms/echo", Patch());
        });
        var session = editor.Session;
        var entry = session.At(Vale);
        session.Select(entry);
        editor.Frame();
        // Its own problem at once; the room's after the pause.
        Assert.Equal(["the theme's id is 'alder-vail', but its path holds 'alder-vale' (a theme's path is made from its id)"], editor.Row("$.id").Problems());
        Assert.Empty(editor.Row("$.room").Problems());
        Assert.Equal(["↑", "!"], Marks(editor, Vale));
        session.Change(entry, "Mode", "blues");
        editor.Frame();
        // Two problems at the mode (the kind's and the choice's): one shown, the kind's.
        Assert.Equal(["Unknown mode blues"], editor.Row("$.mode").Problems());
        Assert.True(editor.Row("$.mode").HasClass("has-problem"));
        editor.Pause();
        Assert.Equal(["no room \"attic\" (audio/rooms/attic)"], editor.Row("$.room").Problems());
        Assert.Equal("3 problems", editor.List().Row(entry)!.Descendants().First(e => e.HasClass("problem")).GetAttribute("title"));
        // Put right: the list's mark goes once the checks have run.
        session.Change(entry, "Id", "alder-vale");
        session.Change(entry, "Mode", "dorian");
        session.Change(entry, "Room", "cavern");
        editor.Frame();
        Assert.Equal(["no room \"attic\" (audio/rooms/attic)"], editor.Row("$.room").Problems());
        editor.Pause();
        Assert.Empty(entry.Problems);
        Assert.Empty(editor.Row("$.room").Problems());
        Assert.Equal(["↑", "•"], Marks(editor, Vale));
        // A problem of the whole entry (the game's music loading refuses an effect patch among the rooms): at the top.
        var echo = session.At("audio/rooms/echo");
        Assert.Single(echo.Problems);
        Assert.Equal(RecordPath.Root, echo.Problems[0].Path);
        session.Select(echo);
        editor.Frame();
        var top = editor.Q(".form-problems");
        Assert.Equal([echo.Problems[0].Message], top.ElementChildren.Where(c => c.HasClass("problem")).Select(c => c.TextContent));
        Assert.Equal(["!"], Marks(editor, "audio/rooms/echo"));
    }
}
