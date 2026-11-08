using Statventure.Editor.Shell;

namespace Statventure.Editor.Browser;

/// <summary>
/// The file browser's dialog over the shell (<see cref="FileBrowserPart"/>; the manager's decision of 2026-11-07: the
/// rest inert, the focus kept inside, Escape cancels): the browser (<see cref="FileBrowserView"/>), the request's button
/// and Cancel. It closes with the path picked, or null when cancelled.
/// </summary>
public sealed class FileBrowserDialog : FileBrowserView
{
    readonly EditorDialog _dialog;

    internal FileBrowserDialog(FileBrowserPart part, BrowserRequest request) : this(part, request, Make(part, request))
    {
    }

    FileBrowserDialog(FileBrowserPart part, BrowserRequest request, EditorDialog dialog) : base(part, request, dialog.Content, dialog.Buttons)
    {
        _dialog = dialog;
        _dialog.AddButton("Cancel", null);
        _dialog.Show();
        if (request.SuggestedName is { } suggested)
            PathField.SetSelectionRange(0, Path.GetFileNameWithoutExtension(suggested).Length);
    }

    static EditorDialog Make(FileBrowserPart part, BrowserRequest request)
    {
        var dialog = part.View.Dialogs.Create(request.Title);
        dialog.Element.AddClass("browser-dialog");
        return dialog;
    }

    /// <summary>The dialog (its element, buttons, the value it closes with).</summary>
    public EditorDialog Dialog => _dialog;

    /// <summary>The path picked (a full path), or null when cancelled.</summary>
    public Task<string?> Closed => _dialog.Closed;

    protected override void Done(string path) => _dialog.Close(path);
}
