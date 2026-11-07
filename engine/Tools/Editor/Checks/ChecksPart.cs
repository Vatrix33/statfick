using Statventure.Editor.Shell;
using Statventure.UI.Dom;

namespace Statventure.Editor.Checks;

/// <summary>
/// Starts the session's checks with the view (step 10) and runs the checks across entries a pause after the last change
/// (P17: <see cref="EntryChecks.Pause"/>, on the document's clock, as a task of its loop): every change starts the pause
/// again. Their problems show where the form and the list show an entry's problems (P16; steps 8 and 9).
/// </summary>
public sealed class ChecksPart : EditorPart
{
    int _timer;

    protected internal override void Attach(UiElement? element)
    {
        var checks = Session.Checks;
        checks.Due += () =>
        {
            if (_timer != 0)
                View.Document.EventLoop.ClearTimeout(_timer);
            _timer = View.Document.SetTimeout(() =>
            {
                _timer = 0;
                checks.RunPending();
            }, EntryChecks.Pause);
        };
    }

    /// <summary>Whether the pause before the checks across entries is running.</summary>
    internal bool Waiting => _timer != 0;
}
