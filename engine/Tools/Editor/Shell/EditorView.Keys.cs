using Statventure.UI.Dom;

namespace Statventure.Editor.Shell;

public sealed partial class EditorView
{
    /// <summary>The page zoom's limits (Electron's default limits of a page's zoom, which the old game's View menu stays in).</summary>
    public const float MinZoom = 0.5f, MaxZoom = 3;

    double _zoomLevel;

    /// <summary>
    /// The page zoom: 1.2 to the power of the zoom level (Chromium's), the level moved by half a step by
    /// Ctrl+= and Ctrl+- as the old game's View menu does (Electron's zoomIn and zoomOut roles), within
    /// <see cref="MinZoom"/> and <see cref="MaxZoom"/>.
    /// </summary>
    public float Zoom => (float)Math.Pow(1.2, _zoomLevel);

    /// <summary>Puts a zoom into effect (the program: the window host's page zoom); none: the document's own.</summary>
    public Action<float>? ApplyZoom { get; set; }

    /// <summary>
    /// The shell's keys (Docs/Editor.md, "The shell (step 4)"), each a command given to the parts' handlers:
    /// Ctrl+S, Ctrl+Shift+S, Ctrl+O, Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z, Ctrl+F, F2, Delete, Ctrl+= (or +) and Ctrl+-.
    /// </summary>
    public static EditorCommand? CommandFor(UiKeyboardEvent e)
    {
        if (e.AltKey || e.MetaKey)
            return null;
        var key = e.Key.Length == 1 ? e.Key.ToLowerInvariant() : e.Key;
        if (!e.CtrlKey)
            return e.ShiftKey ? null : key switch
            {
                "F2" => EditorCommand.Rename,
                "Delete" => EditorCommand.Remove,
                _ => null,
            };
        return (key, e.ShiftKey) switch
        {
            ("s", false) => EditorCommand.Save,
            ("s", true) => EditorCommand.SaveAs,
            ("o", false) => EditorCommand.Open,
            ("z", false) => EditorCommand.Undo,
            ("z", true) or ("y", false) => EditorCommand.Redo,
            ("f", false) => EditorCommand.Filter,
            ("=" or "+", _) => EditorCommand.ZoomIn,
            ("-", false) => EditorCommand.ZoomOut,
            _ => null,
        };
    }

    void ListenToKeys()
    {
        Commands.Handle(EditorCommand.ZoomIn, _ => StepZoom(+0.5));
        Commands.Handle(EditorCommand.ZoomOut, _ => StepZoom(-0.5));
        Document.AddEventListener<UiKeyboardEvent>("keydown", KeyDown);
    }

    /// <summary>
    /// A key that reached the document unhandled: Escape cancels the topmost dialog; while one is open only
    /// the zoom keys are commands (the rest of the editor is inert). Delete in a field that takes text is the
    /// field's. A command that acted cancels the key's default action.
    /// </summary>
    void KeyDown(UiKeyboardEvent e)
    {
        if (e.DefaultPrevented)
            return;
        if (Dialogs.Top is { } dialog)
        {
            if (e.Key == "Escape" && !e.CtrlKey && !e.AltKey && !e.MetaKey && !e.ShiftKey)
            {
                dialog.Close(null);
                e.PreventDefault();
                return;
            }
        }
        if (CommandFor(e) is not { } command)
            return;
        if (Dialogs.Top is not null && command is not (EditorCommand.ZoomIn or EditorCommand.ZoomOut))
            return;
        if (command == EditorCommand.Remove && e.Target is { } target && EditorCommands.TakesText(target))
            return;
        if (Commands.Run(new CommandRequest(command, e, e.Target)))
            e.PreventDefault();
    }

    bool StepZoom(double step)
    {
        var level = _zoomLevel + step;
        var zoom = Math.Pow(1.2, level);
        if (zoom < MinZoom || zoom > MaxZoom)
            return true;
        _zoomLevel = level;
        if (ApplyZoom is { } apply)
            apply(Zoom);
        else
            Document.SetZoom(Document.DeviceScale, Zoom);
        return true;
    }
}
