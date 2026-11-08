using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.UI.Dom;

namespace Statventure.Editor.Saving;

/// <summary>
/// The New mod dialog (M11-5, M11-15, P25): the mod's name (focused) and its file's name, suggested from the name under the
/// path rule (<see cref="EditorSession.NewModFileName"/>) while it isn't edited by hand, a line for what is wrong, and that
/// mods load in file-name order; Create (or Enter in a field) checks the names (<see cref="EditorSession.NewModProblem"/>)
/// and closes, Cancel gives nothing.
/// </summary>
public sealed class NewModDialog
{
    readonly EditorSession _session;
    readonly EditorDialog _dialog;
    readonly UiElement _problem;
    bool _fileEdited;

    internal NewModDialog(SavingPart part)
    {
        _session = part.Session;
        _dialog = part.View.Dialogs.Create("New mod");
        _dialog.Element.AddClass("saving-new-mod");
        _dialog.Content.Append("p", text: "The mod is made in your mods folder: a pack whose entries add to the game's data or replace its entries.");
        NameField = Field("Name");
        FileField = Field("File");
        _problem = _dialog.Content.Append("div", "saving-problem");
        _dialog.Content.Append("p", "saving-about", "Mods load in file-name order: where two mods hold the same entry, the one whose file name comes later wins.");
        NameField.AddEventListener("input", _ =>
        {
            if (!_fileEdited)
                FileField.Value = EditorSession.NewModFileName(NameField.Value);
        });
        FileField.AddEventListener("input", _ => _fileEdited = true);
        var create = _dialog.Buttons.Append("button", "primary", "Create");
        create.SetAttribute("type", "button");
        create.AddEventListener("click", _ => Act());
        _dialog.AddButton("Cancel", null);
        _dialog.Show();
    }

    UiElement Field(string label)
    {
        var line = _dialog.Content.Append("label", "saving-field");
        line.Append("span", "saving-label", label);
        var input = line.Append("input");
        input.SetAttribute("type", "text");
        input.AddEventListener("input", _ => _problem.TextContent = "");
        input.AddEventListener<UiKeyboardEvent>("keydown", e =>
        {
            if (e.Key == "Enter")
            {
                // (Its keypress would otherwise reach what has the focus once the dialog closes: the New mod button.)
                e.PreventDefault();
                Act();
            }
        });
        return input;
    }

    public EditorDialog Dialog => _dialog;

    /// <summary>The mod's name.</summary>
    public UiElement NameField { get; }

    /// <summary>The mod's file name in the mods folder.</summary>
    public UiElement FileField { get; }

    /// <summary>What is wrong with the names ("" when nothing).</summary>
    public string Problem => _problem.TextContent;

    /// <summary>The names given when Create closed it.</summary>
    public (string Name, string File)? Asked { get; private set; }

    /// <summary>Create: the names checked; a problem shows on the line and the dialog stays, else it closes with them.</summary>
    public void Act()
    {
        if (_session.NewModProblem(NameField.Value, FileField.Value) is { } problem)
        {
            _problem.TextContent = problem;
            return;
        }
        Asked = (NameField.Value, FileField.Value);
        _dialog.Close("create");
    }

    /// <summary>The names, or null when cancelled.</summary>
    internal Task<(string Name, string File)?> Closed => Wait();

    async Task<(string Name, string File)?> Wait() => await _dialog.Closed == "create" ? Asked : null;
}
