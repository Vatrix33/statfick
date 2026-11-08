using Statventure.Core;
using Statventure.Editor.Shell;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Editor.About;

/// <summary>
/// The editor's credits (Q30; the BRUTAL License v1.0, 2.1; Docs/Packaging.md, "The BRUTAL logo and the credits"): the
/// top bar's About opens the engine's credits view (<see cref="Credits.Markup"/>: the editor's name and version, "Built
/// with BRUTAL" with BRUTAL's notice, every component with its licence and copyright lines, where the full texts are)
/// in a dialog over the shell, its content scrolling on its own; Close (or Escape) closes it.
/// </summary>
public sealed class AboutPart : EditorPart
{
    /// <summary>What the credits say of the editor: its own program file's (<see cref="ProgramInfo"/>; the version, no commit, Q3).</summary>
    public static ProgramInfo Program { get; } = ProgramInfo.Of(typeof(EditorView).Assembly);

    protected internal override void Attach(UiElement? element)
    {
        View.Commands.Handle(EditorCommand.About, request =>
        {
            View.Document.RunScript(() => _ = Show());
            return true;
        });
    }

    /// <summary>The About dialog while it is open, else null.</summary>
    public EditorDialog? Current { get; private set; }

    /// <summary>Shows the credits in a dialog; done when it is closed.</summary>
    public async Task Show()
    {
        if (Current is not null)
            return;
        var dialog = View.Dialogs.Create("About");
        dialog.Element.AddClass("about-dialog");
        Markup.SetInnerHtml(dialog.Content, Credits.Markup(EditorView.Name, Program));
        dialog.AddButton("Close", "close", primary: true);
        Current = dialog;
        dialog.Show();
        try
        {
            await dialog.Closed;
        }
        finally
        {
            Current = null;
        }
    }
}
