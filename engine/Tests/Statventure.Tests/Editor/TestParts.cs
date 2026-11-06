using Statventure.Editor.Shell;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor;

/// <summary>
/// A part that handles every command but the zoom and records what it was asked (<see cref="Acts"/>: whether it
/// says it acted), with a text field and a button in the entry pane.
/// </summary>
public sealed class RecordingPart : EditorPart
{
    public readonly List<CommandRequest> Requests = [];
    public bool Acts = true;
    public bool CanRun = true;
    public UiElement Field = null!;
    public UiElement Button = null!;

    public override EditorPane? Pane => EditorPane.Entries;

    protected internal override void Attach(UiElement? element)
    {
        Field = element!.Append("input");
        Button = element.Append("button", text: "Pane button");
        foreach (var command in Enum.GetValues<EditorCommand>())
            if (command is not (EditorCommand.ZoomIn or EditorCommand.ZoomOut))
                View.Commands.Handle(command, request =>
                {
                    Requests.Add(request);
                    return Acts;
                }, () => CanRun);
    }
}

/// <summary>Parts that note when they were attached, with what, and where (the order of attaching).</summary>
public abstract class NotingPart : EditorPart
{
    public static readonly List<string> Attached = [];
    public UiElement? Element;
    public bool HadSessionAndView;

    protected internal override void Attach(UiElement? element)
    {
        Element = element;
        HadSessionAndView = Session is not null && View is not null;
        Attached.Add(GetType().Name);
    }
}

public sealed class FormFirst : NotingPart
{
    public override EditorPane? Pane => EditorPane.Form;
    public override int Order => -1;
}

public sealed class FormSecond : NotingPart
{
    public override EditorPane? Pane => EditorPane.Form;
}

public sealed class FormSecondToo : NotingPart
{
    public override EditorPane? Pane => EditorPane.Form;
}

public sealed class NoPane : NotingPart;

public sealed class EntriesLate : NotingPart
{
    public override EditorPane? Pane => EditorPane.Entries;
    public override int Order => 5;
}

public sealed class PreviewPart : NotingPart
{
    public override EditorPane? Pane => EditorPane.Preview;
    public override int Order => -10;
}
