using Statventure.Editor.Browser;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Storage;
using Statventure.UI.Dom;

namespace Statventure.Editor.Game;

/// <summary>
/// Choosing another game's folder from the editor (Q16; M12 step 6): the top bar's "Game folder…" (not in the dev setup,
/// whose game is the repo) shows the file browser picking a folder, "Where is Stat Fick?", starting beside the game's
/// folder; a folder the editor can't work on is said under the listing (<see cref="GameLookup"/>: not the game, another
/// version, its library unusable). A game whose data library is the build this run has loaded becomes the editor's at
/// once, once the open pack's unsaved work is saved or let go (<see cref="Chosen"/>; the program makes the session and
/// view anew); one of another build can only be used by starting the editor again, which it says (the manager's
/// decision Q-C). Either way the folder is remembered for the next start (<see cref="RememberedGame"/>).
/// </summary>
public sealed class GamePart : EditorPart
{
    protected internal override void Attach(UiElement? element)
    {
        View.Commands.Handle(EditorCommand.GameFolder, request =>
        {
            View.Document.RunScript(() => _ = Change());
            return true;
        }, () => !Session.Game.DevSetup);
        View.Button(EditorCommand.GameFolder)?.SetAttribute("title", Session.Game.Folder is { } folder
            ? $"Stat Fick's folder: {folder}"
            : "Stat Fick's folder (the dev setup's game is the repo)");
    }

    /// <summary>A game was chosen that this run can work on at once: the program shows it (a new session and view).</summary>
    public event Action<GameData>? Chosen;

    /// <summary>
    /// Asks for the game's folder and goes over to the game found there; gives whether it did (false: cancelled, the
    /// unsaved work kept, or only a new start can use it).
    /// </summary>
    public Task<bool> Change()
    {
        var current = Session.Game.Folder;
        return Change(current is not null && Path.GetDirectoryName(current) is { } beside && Directory.Exists(beside) ? beside : null);
    }

    /// <summary><see cref="Change()"/>, the browser starting in <paramref name="startFolder"/> (null: the most recent place, else home; the screens' fixture gives its own).</summary>
    internal async Task<bool> Change(string? startFolder)
    {
        GameFound? found = null;
        var current = Session.Game.Folder;
        var request = new BrowserRequest(GameScreen.Heading, "Use this folder", Folder: true, StartFolder: startFolder,
            Fallback: BrowserPlaceKind.Home,
            Lead: GameScreen.LeadLine(OperatingSystem.IsWindows()) + (current is not null ? $" The editor works on the game in {current} now." : ""),
            Accept: folder =>
            {
                found = GameLookup.Find(folder, EditorView.Version);
                return found.Problem is { Kind: not GameProblemKind.AnotherBuild } problem ? problem.Sentence(EditorView.Version) : null;
            });
        var chosen = await View.Part<FileBrowserPart>().Pick(request);
        if (chosen is null || found is null)
            return false;
        if (found.Game is not { } game)
        {
            RememberedGame.Write(Session.PlayerFolder, chosen);
            await View.Dialogs.Ask("Start the editor again", $"Start the editor again to work on the game in {chosen}: {found.Problem!.Text(EditorView.Version)}. " +
                "The folder is remembered for the next start.", ("OK", "ok"));
            return false;
        }
        if (!await View.Part<Opener>().LeaveUnsavedWork($"work on the game in {chosen}"))
            return false;
        RememberedGame.Write(Session.PlayerFolder, chosen);
        Chosen?.Invoke(game);
        return true;
    }
}
