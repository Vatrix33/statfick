using Statventure.Editor.About;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.About;

/// <summary>
/// The About dialog against Chromium (Q30; M12 step 6): the engine's credits over the shell, at their top and scrolled to
/// their end. Opened as the command does (no pointer left over the top bar's button: hover doesn't carry over), so Close
/// has the focus by code, its ring shown as in Chromium.
/// </summary>
public sealed class AboutScreens : IEditorFixtures
{
    public IEnumerable<EditorFixture> Fixtures =>
    [
        new("about", 1000, 640, editor => Open(editor, end: false)),
        new("about-end", 1000, 640, editor => Open(editor, end: true)),
    ];

    static void Open(EditorHarness editor, bool end)
    {
        _ = editor.Run(() => editor.View.Part<AboutPart>().Show());
        if (end)
        {
            var content = editor.Q(".about-dialog .dialog-content");
            content.ScrollTop = content.ScrollHeight - content.ClientHeight;
        }
        editor.Frame();
    }
}
