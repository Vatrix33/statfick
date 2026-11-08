using Statventure.Editor.Browser;
using Statventure.Editor.Shell;
using static Statventure.Tests.Editor.Import.ImportTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Import;

/// <summary>
/// Enter in the import dialog (found by step 14; Docs/Editor.md, "How it works", "Fixes after step 14"): it imports, and
/// its keypress doesn't reach the Import button the focus goes back to once the dialog closes, which opened the file
/// browser again.
/// </summary>
public class ImportEnterTests
{
    [Fact]
    public void EnterImportsAndOpensNothingElse()
    {
        using var scratch = new Scratch();
        var file = scratch.Write("downloads/notes.txt", "Notes.");
        using var editor = scratch.Editor();
        var import = editor.View.Button(EditorCommand.Import)!;
        editor.Click(import);
        var browser = editor.View.Part<FileBrowserPart>().Current!;
        browser.PathField.Value = file;
        browser.PathField.Focus();
        editor.Key("Enter", "Enter");
        var dialog = editor.Import().Current!;
        Assert.Same(dialog.PathField, editor.Document.FocusedElement);
        editor.Key("Enter", "Enter");
        Assert.NotNull(editor.Session.Entry("notes.txt"));
        Assert.Null(editor.Import().Current);
        Assert.Null(editor.View.Part<FileBrowserPart>().Current);
        Assert.Null(editor.View.Dialogs.Top);
        // The focus went back to the Import button; nothing pressed it.
        Assert.Same(import, editor.Document.FocusedElement);
    }

    /// <summary>Enter with a path that can't be the entry's: the problem said, the dialog still open, nothing else opened.</summary>
    [Fact]
    public void EnterWithAProblemKeepsTheDialog()
    {
        using var scratch = new Scratch();
        var file = scratch.Write("downloads/notes.txt", "Notes.");
        using var editor = scratch.Editor();
        _ = editor.Run(() => editor.Import().ImportFiles([file]));
        var dialog = editor.Import().Current!;
        dialog.PathField.Value = "Notes.txt";
        dialog.PathField.Focus();
        editor.Key("Enter", "Enter");
        Assert.Same(dialog, editor.Import().Current);
        Assert.NotEqual("", dialog.Problem);
        Assert.Equal(1, editor.View.Dialogs.Open.Count);
    }
}
