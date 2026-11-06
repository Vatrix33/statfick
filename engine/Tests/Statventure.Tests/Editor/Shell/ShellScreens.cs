namespace Statventure.Tests.Editor.Shell;

/// <summary>The shell's screens against Chromium (P36): the empty shell, and a dialog over it.</summary>
public sealed class ShellScreens : IEditorFixtures
{
    public IEnumerable<EditorFixture> Fixtures =>
    [
        new("shell", 1000, 640, _ => { }),
        new("dialog", 1000, 640, editor => _ = editor.View.Dialogs.Ask("Question",
            "Dialogs show over the editor, which waits until one is answered.", ("OK", "ok"), ("Cancel", null))),
    ];
}
