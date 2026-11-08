using Statventure.Editor.Browser;
using Statventure.Editor.Shell;
using Statventure.Tests.Editor.Form;
using Statventure.Platform;
using static Statventure.Tests.Editor.EndToEnd.EndToEndTesting;
using static Statventure.Tests.Editor.Import.ImportTesting;

namespace Statventure.Tests.Editor.EndToEnd;

/// <summary>
/// What the end-to-end flow found in the editor (M11 step 14, 2026-11-08), each as the smallest flow a user takes that
/// shows it, kept as the end-to-end witness of its fix ("Fixes after step 14", Docs/Editor.md; the fix's own tests are
/// beside the code it changed). The other fault, New entry without a control, is witnessed by the end-to-end flow itself
/// (<see cref="ModFromScratchTests"/>: the top bar's New entry and Ctrl+N).
/// </summary>
public class FoundTests
{
    /// <summary>
    /// Enter in the import dialog's path field imports; its keypress must not reach what has the focus once the dialog
    /// closes (the top bar's Import button the focus goes back to, which opened the file browser again until it was
    /// fixed; step 11's New mod dialog had the same).
    /// </summary>
    [Fact]
    public void EnterInTheImportDialogImportsAndOpensNothingElse()
    {
        using var scratch = new Import.ImportTesting.Scratch();
        var file = scratch.Write("downloads/notes.txt", "Notes.");
        using var editor = scratch.Editor();
        editor.Click(EditorCommand.Import);
        var browser = editor.View.Part<FileBrowserPart>().Current!;
        editor.Press(browser.PathField);
        foreach (var c in file)
            editor.Key(c.ToString(), "", char.IsUpper(c) ? KeyModifiers.Shift : KeyModifiers.None);
        editor.Key("Enter", "Enter");
        var dialog = editor.Import().Current!;
        Assert.Same(dialog.PathField, editor.Document.FocusedElement);
        editor.Key("Enter", "Enter");
        Assert.NotNull(editor.Session.Entry("notes.txt"));
        Assert.Null(editor.Import().Current);
        Assert.True(editor.View.Dialogs.Top is null, $"a dialog opened after the import: {editor.View.Dialogs.Top?.Element.ElementChildren.First().TextContent}");
    }
}
