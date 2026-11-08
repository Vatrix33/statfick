using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Platform;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Form;

/// <summary>
/// New entry from the top bar and Ctrl+N (P15; the manager's decision of 2026-11-08 after step 14 found nothing in the UI
/// started it; Docs/Editor.md, "How it works", "Fixes after step 14"): enabled while entries can be added, both open step
/// 9's New entry dialog, also from a field of the mod info's own form.
/// </summary>
public class NewEntryCommandTests
{
    static void CtrlN(EditorHarness editor) => editor.Key("n", "KeyN", KeyModifiers.Control);

    static string? Title(EditorHarness editor) => editor.View.Dialogs.Top?.Element.ElementChildren.First().TextContent;

    [Fact]
    public void NewEntryIsTheTopBarsButtonAndCtrlN()
    {
        using var scratch = new Saving.SavingTesting.Scratch();
        using var editor = scratch.Editor();
        var button = editor.View.Button(EditorCommand.NewEntry)!;
        Assert.Equal(("New entry", "New entry (Ctrl+N)"), (button.TextContent, button.GetAttribute("title")));
        // Nothing open: nothing can be added.
        Assert.True(button.IsDisabled);
        CtrlN(editor);
        Assert.Null(editor.View.Dialogs.Top);
        editor.Session.Open(scratch.Mod(w =>
        {
            w.AddRecord(ModInfo.Path, new ModInfo { Name = "Halls" });
            w.AddRecord("audio/rooms/crypt", new RoomSpec(2.5, 0.5, 0.25));
        }));
        editor.Frame();
        Assert.False(button.IsDisabled);
        // The button: step 9's dialog; the entry made and selected.
        editor.Click(button);
        Assert.Equal("New entry", Title(editor));
        editor.Choose(editor.DialogField("select"), "Room (audio.room)");
        editor.Enter(editor.DialogField("input"), "attic");
        Assert.Null(editor.View.Dialogs.Top);
        var attic = editor.Session.Entry("audio/rooms/attic")!;
        Assert.Equal(EntrySource.New, attic.Source);
        Assert.Same(attic, editor.Session.Selected);
        // Ctrl+N: the same dialog, the selected entry's kind first; Escape makes nothing.
        CtrlN(editor);
        Assert.Equal("New entry", Title(editor));
        Assert.Equal("Room (audio.room)", editor.DialogField("select").SelectedOption!.TextContent);
        editor.Key("Escape", "Escape");
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Equal(3, editor.Session.Entries.Count(e => e.Source != EntrySource.Game));
        // While the mod info's own form shows, from one of its fields.
        editor.Session.Select(editor.Session.Entry(ModInfo.Path));
        editor.Frame();
        var field = editor.View.Part<Statventure.Editor.Saving.ModInfoForm>().Field("Author");
        editor.Press(field);
        CtrlN(editor);
        Assert.Equal("New entry", Title(editor));
        editor.Choose(editor.DialogField("select"), "Room (audio.room)");
        editor.Enter(editor.DialogField("input"), "vault");
        Assert.NotNull(editor.Session.Entry("audio/rooms/vault"));
    }

    [Fact]
    public void NewEntryWaitsForAPackThatTakesChanges()
    {
        using var scratch = new Saving.SavingTesting.Scratch();
        using var editor = scratch.Editor(scratch.Mod(w =>
        {
            w.AddRecord("audio/rooms/crypt", new RoomSpec(2.5, 0.5, 0.25));
            AddUnreadable(w, "parts/arm", "unknown kind");
        }));
        Assert.True(editor.View.Button(EditorCommand.NewEntry)!.IsDisabled);
        CtrlN(editor);
        Assert.Null(editor.View.Dialogs.Top);
    }
}
