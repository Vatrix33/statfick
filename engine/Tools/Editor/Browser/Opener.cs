using System.Globalization;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.UI.Dom;

namespace Statventure.Editor.Browser;

/// <summary>
/// Opening packs from the view (Docs/Editor.md, "Opening packs (step 8)"): the Open command (Ctrl+O and the top bar's
/// button) asks the file browser for a pack; files dropped on the window open (a <c>.pak</c>) or wait for import (any
/// other file, P27); a pack <c>Open</c> refuses shows why and nothing else changes; opening over unsaved work, and
/// closing the editor with unsaved work, ask first (M11-17: Discard or Cancel, and Save once step 11 gives
/// <see cref="SaveFirst"/>). Each starts as a task of the document's loop, so what follows a dialog runs there.
/// </summary>
public sealed class Opener : EditorPart
{
    bool _closing;

    protected internal override void Attach(UiElement? element)
    {
        View.Commands.Handle(EditorCommand.Open, request =>
        {
            View.Document.RunScript(() => _ = Browse());
            return true;
        });
        Session.PackOpened += () => View.Status = Opened(Session.Pack!);
    }

    /// <summary>
    /// Saving before the open pack is left (step 11 sets it): when set, the unsaved-work question offers Save first,
    /// and goes on when it gives true (saved); null: only Discard and Cancel.
    /// </summary>
    public Func<Task<bool>>? SaveFirst { get; set; }

    /// <summary>The browser part (<see cref="FileBrowserPart"/>).</summary>
    FileBrowserPart Browser => View.Part<FileBrowserPart>();

    /// <summary>Asks the file browser for a pack and opens it; gives whether one was opened.</summary>
    public async Task<bool> Browse()
    {
        var file = await Browser.PickPack();
        return file is not null && await OpenFile(file);
    }

    /// <summary>
    /// Opens <paramref name="file"/>: a pack <c>Open</c> refuses is said in a dialog with why (nothing else changes);
    /// one that opens replaces the open pack once the unsaved work there is saved or let go (asked first). Gives whether
    /// it was opened.
    /// </summary>
    public async Task<bool> OpenFile(string file)
    {
        PackFile pack;
        try
        {
            pack = PackFile.Open(file);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            var reason = problem switch
            {
                DataFileException data => data.Problem,
                FileNotFoundException or DirectoryNotFoundException => "the file isn't there",
                _ => problem.Message,
            };
            await View.Dialogs.Ask("The pack can't be opened", $"{Path.GetFileName(file)} can't be opened: {reason}.", ("OK", "ok"));
            return false;
        }
        if (!await LeaveUnsavedWork($"open {Path.GetFileName(pack.FilePath)}"))
        {
            pack.Dispose();
            return false;
        }
        Session.Open(pack);
        return true;
    }

    /// <summary>
    /// Whether the open pack's unsaved work may be left (to open another, to close): none, or the user chose to save it
    /// (when <see cref="SaveFirst"/> is set and saved) or to discard it; false when cancelled. <paramref name="action"/>
    /// says what follows ("open b.pak", "close the editor").
    /// </summary>
    public async Task<bool> LeaveUnsavedWork(string action)
    {
        if (!Session.HasUnsavedWork)
            return true;
        var pack = Session.PackName ?? "the open pack";
        var buttons = new List<(string, string?)>();
        if (SaveFirst is not null)
            buttons.Add(("Save", "save"));
        buttons.Add(("Discard", "discard"));
        buttons.Add(("Cancel", null));
        var answer = await View.Dialogs.Ask("Unsaved changes", $"{pack} has changes that aren't saved. Discard them and {action}?", [.. buttons]);
        return answer switch
        {
            "save" => await SaveFirst!(),
            "discard" => true,
            _ => false,
        };
    }

    /// <summary>
    /// The application's close (<c>Application.ConfirmClose</c>): true when there is no unsaved work; otherwise the
    /// question is asked (once, however often the window is closed meanwhile) and false given, the loop going on; when
    /// the user lets the work go, <paramref name="quit"/> ends the application.
    /// </summary>
    public bool ConfirmClose(Action quit)
    {
        if (!Session.HasUnsavedWork)
            return true;
        if (_closing)
            return false;
        _closing = true;
        View.Document.RunScript(() => _ = Close(quit));
        return false;
    }

    async Task Close(Action quit)
    {
        try
        {
            if (await LeaveUnsavedWork("close the editor"))
                quit();
        }
        finally
        {
            _closing = false;
        }
    }

    /// <summary>
    /// Files dropped on the window (P27), as the window gives them (<c>Window.PathsDropped</c>): the first <c>.pak</c>
    /// file opens (asking first over unsaved work), then the other files are queued for import in their order
    /// (<see cref="EditorSession.QueueImports"/>, step 12); further packs aren't opened (one pack at a time, P6), and
    /// what isn't a file here (a folder, a link of another kind) is passed over; the status line says so.
    /// </summary>
    public void Drop(IReadOnlyList<string> paths) => View.Document.RunScript(() => _ = DropAsync(paths));

    /// <summary>What <see cref="Drop"/> does, given back when done (tests).</summary>
    public async Task DropAsync(IReadOnlyList<string> paths)
    {
        string? pack = null;
        var imports = new List<string>();
        var passed = new List<string>();
        foreach (var path in paths)
        {
            if (!IsFile(path))
                passed.Add(path);
            else if (path.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
            {
                if (pack is null)
                    pack = path;
                else
                    passed.Add(path);
            }
            else
                imports.Add(path);
        }
        if (pack is not null && !await OpenFile(pack))
            pack = null;
        if (imports.Count > 0)
            Session.QueueImports(imports);
        var said = new List<string>();
        if (pack is not null)
            said.Add(Opened(Session.Pack!));
        if (imports.Count > 0 && !Session.CanImport)
            said.Add($"{Count(imports.Count, "file")} waiting for import");
        if (passed.Count > 0)
            said.Add($"not opened: {string.Join(", ", passed.Select(p => Path.GetFileName(Path.TrimEndingDirectorySeparator(p)) is { Length: > 0 } name ? name : p))}");
        if (said.Count > 0)
            View.Status = Capital(string.Join("; ", said)) + ".";
    }

    static bool IsFile(string path)
    {
        try
        {
            return Path.IsPathFullyQualified(path) && File.Exists(path);
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>The status line after a pack opened: its name, how many entries it holds, and why it is read-only.</summary>
    public string Opened(OpenPack pack)
    {
        var text = $"Opened {pack.Name}: {Count(Session.PackFile?.Entries.Count ?? 0, "entry", "entries")}";
        return pack.ReadOnlyReason is { } reason ? $"{text}, read-only: {reason}" : text;
    }

    static string Count(int n, string noun, string? nouns = null) =>
        n == 1 ? $"1 {noun}" : $"{n.ToString("N0", CultureInfo.InvariantCulture)} {nouns ?? noun + "s"}";

    static string Capital(string text) => text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];
}
