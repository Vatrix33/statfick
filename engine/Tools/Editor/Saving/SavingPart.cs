using System.Globalization;
using Statventure.Editor.Browser;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Storage;
using Statventure.UI.Dom;

namespace Statventure.Editor.Saving;

/// <summary>
/// Saving and mods from the view (step 11; Docs/Editor.md, "Saving and mods (steps 7 and 11)", "How it works", step 11):
/// Save (Ctrl+S and the top bar's button) over the open pack, refused while an entry has a problem (M11-12, naming them)
/// and asking first when another program changed the file (P23: Overwrite, Save As, Cancel); Save As (Ctrl+Shift+S): the
/// entries ticked (M11-16, P24), then the file browser, the subset checked as the game would see it, written and opened
/// (the manager's decisions Q3–Q5 of 2026-11-08); New mod: its name and file name (P25), the mod written into the
/// player's mods folder at once and opened, its info selected (M11-5, M11-15, Q1, Q2); and the unsaved-changes question's
/// Save (step 8's <see cref="Opener.SaveFirst"/>). Each starts as a task of the document's loop.
/// </summary>
public sealed class SavingPart : EditorPart
{
    /// <summary>The most entries a refusal names (then "and N more").</summary>
    public const int Listed = 8;

    protected internal override void Attach(UiElement? element)
    {
        View.Commands.Handle(EditorCommand.Save, request =>
        {
            View.Document.RunScript(() => _ = Save());
            return true;
        }, () => Session.CanSave);
        View.Commands.Handle(EditorCommand.SaveAs, request =>
        {
            View.Document.RunScript(() => _ = SaveAs());
            return true;
        }, () => Session.CanSaveAs);
        View.Commands.Handle(EditorCommand.NewMod, request =>
        {
            View.Document.RunScript(() => _ = NewMod());
            return true;
        });
        View.Part<Opener>().SaveFirst = Save;
    }

    FileBrowserPart Browser => View.Part<FileBrowserPart>();

    /// <summary>The Save As dialog while it is open (tests, the screens' fixture), else null.</summary>
    public SaveAsDialog? SaveAsShown { get; private set; }

    /// <summary>The New mod dialog while it is open (tests, the screens' fixture), else null.</summary>
    public NewModDialog? NewModShown { get; private set; }

    // ---------- Save ----------

    /// <summary>
    /// Saves the open pack over its file (M11-17, P23): with no unsaved work nothing is written; refused while an entry has a
    /// problem (a dialog names them); a file another program changed asks first (Overwrite, Save As, Cancel). Gives whether
    /// the pack is saved now (true when there was nothing to save; Save As chosen instead gives its answer).
    /// </summary>
    public async Task<bool> Save()
    {
        if (!Session.CanSave)
            return false;
        CommitTyping();
        var name = Session.Pack!.Name;
        if (!Session.HasUnsavedWork)
        {
            View.Status = $"Nothing to save: {name} is as saved.";
            return true;
        }
        if (Session.SaveProblems() is { Count: > 0 } problems)
        {
            await Refuse($"{name} can't be saved until these are fixed:", problems);
            return false;
        }
        if (Session.ChangedOnDisk() is { } why)
        {
            var answer = await View.Dialogs.Ask("The file changed", $"{name} {why}. Overwrite it with the editor's version, save as another file, or cancel?",
                ("Overwrite", "overwrite"), ("Save As", "save-as"), ("Cancel", null));
            if (answer == "save-as")
                return await SaveAs();
            if (answer != "overwrite")
                return false;
        }
        try
        {
            Session.Save();
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            await View.Dialogs.Ask("Can't save", $"{name} couldn't be written: {Why(problem)}.", ("OK", null));
            return false;
        }
        View.Status = $"Saved {name}.";
        return true;
    }

    /// <summary>
    /// A text field being typed in commits first (blurred and focused again: its change event), so Ctrl+S saves what it
    /// shows, as the top bar's button does by taking the focus.
    /// </summary>
    void CommitTyping()
    {
        if (View.Document.FocusedElement is { } focused && EditorCommands.TakesText(focused))
        {
            focused.Blur();
            focused.Focus();
        }
    }

    /// <summary>The refusal (M11-12): <paramref name="lead"/>, then each entry with its first problem (the first <see cref="Listed"/>, then how many more).</summary>
    async Task Refuse(string lead, IReadOnlyList<SaveProblem> problems)
    {
        var dialog = View.Dialogs.Create("Can't save");
        dialog.Content.Append("p", text: lead);
        var list = dialog.Content.Append("ul", "saving-problems");
        foreach (var problem in problems.Take(Listed))
        {
            var item = list.Append("li");
            if (problem.Entry is { } entry)
            {
                item.Append("span", "saving-problem-entry", entry);
                item.Append("span", null, ": " + problem.Problem);
            }
            else
                item.TextContent = problem.Problem;
        }
        if (problems.Count > Listed)
            list.Append("li", "saving-more", $"and {(problems.Count - Listed).ToString("N0", CultureInfo.InvariantCulture)} more");
        dialog.AddButton("OK", "ok", primary: true);
        dialog.Show();
        await dialog.Closed;
    }

    static string Why(Exception problem) => problem is DataFileException data ? data.Problem : problem.Message.TrimEnd('.');

    // ---------- Save As ----------

    /// <summary>
    /// Save As (M11-16, P24; Q3–Q5): the entries to keep ticked in the Save As dialog, then the new file from the file browser
    /// (<c>.pak</c> added to a name typed without it; the game's packs outside the dev setup and the open pack when it can't
    /// be saved over refused, the browser shown again; over the open pack a file another program changed asks first), the
    /// entries ticked checked as the game would see them (refused naming the problems), written and opened in place of the
    /// open pack. Gives whether it was saved.
    /// </summary>
    public async Task<bool> SaveAs()
    {
        if (!Session.CanSaveAs)
            return false;
        // (A field being typed in commits as the dialog takes the focus.)
        var name = Session.Pack!.Name;
        var dialog = new SaveAsDialog(this, Session.SaveAsChoices());
        SaveAsShown = dialog;
        IReadOnlyList<EditorEntry>? ticked;
        try
        {
            ticked = await dialog.Closed;
        }
        finally
        {
            SaveAsShown = null;
        }
        if (ticked is null)
            return false;
        while (true)
        {
            var picked = await Browser.Pick(new BrowserRequest("Save As", "Save", FileBrowserPart.PackEndings, NewFile: true, SuggestedName: name));
            if (picked is null)
                return false;
            var file = picked.EndsWith(".pak", StringComparison.OrdinalIgnoreCase) ? picked : picked + ".pak";
            if (file != picked && File.Exists(file)
                && await View.Dialogs.Ask("Replace the file?", $"{Path.GetFileName(file)} is there already. Replace it?", ("Replace", "replace"), ("Cancel", null)) is null)
                continue;
            if (Session.SaveAsFileProblem(file) is { } problem)
            {
                await View.Dialogs.Ask("Can't save there", $"{problem}. Choose another file.", ("OK", null));
                continue;
            }
            if (string.Equals(Path.GetFullPath(file), Session.Pack.File, FileBrowser.NameComparison) && Session.ChangedOnDisk() is { } why)
            {
                var answer = await View.Dialogs.Ask("The file changed", $"{name} {why}. Overwrite it with the entries ticked, choose another file, or cancel?",
                    ("Overwrite", "overwrite"), ("Another file", "another"), ("Cancel", null));
                if (answer == "another")
                    continue;
                if (answer != "overwrite")
                    return false;
            }
            if (Session.SaveAsProblems(ticked, file) is { Count: > 0 } problems)
            {
                await Refuse($"The entries ticked can't be saved as {Path.GetFileName(file)} until these are fixed:", problems);
                return false;
            }
            try
            {
                Session.SaveAs(file, ticked);
            }
            catch (Exception failed) when (failed is IOException or UnauthorizedAccessException)
            {
                await View.Dialogs.Ask("Can't save", $"{Path.GetFileName(file)} couldn't be written: {Why(failed)}.", ("OK", null));
                return false;
            }
            var count = ticked.Count == 1 ? "1 entry" : $"{ticked.Count.ToString("N0", CultureInfo.InvariantCulture)} entries";
            View.Status = $"Saved {count} as {Path.GetFileName(file)}" + (Session.Pack.ReadOnlyReason is { } readOnly ? $", open read-only: {readOnly}." : ".");
            return true;
        }
    }

    // ---------- New mod ----------

    /// <summary>
    /// New mod (M11-5, M11-15, P25, Q2): its name and file name asked (the New mod dialog), the open pack's unsaved work asked
    /// about (Save, Discard, Cancel), then the mod written into the player's mods folder holding its info and opened, its info
    /// selected (Q1: its own form). Gives the pack opened, or null.
    /// </summary>
    public async Task<OpenPack?> NewMod()
    {
        var dialog = new NewModDialog(this);
        NewModShown = dialog;
        (string Name, string File)? asked;
        try
        {
            asked = await dialog.Closed;
        }
        finally
        {
            NewModShown = null;
        }
        if (asked is not { } mod)
            return null;
        if (!await View.Part<Opener>().LeaveUnsavedWork($"make the mod {mod.File}"))
            return null;
        // (Asked again: a file of that name may have come meanwhile.)
        if (Session.NewModProblem(mod.Name, mod.File) is { } refused)
        {
            await View.Dialogs.Ask("Can't make the mod", refused, ("OK", null));
            return null;
        }
        OpenPack opened;
        try
        {
            opened = Session.NewMod(mod.Name, mod.File);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            await View.Dialogs.Ask("Can't make the mod", $"{mod.File} couldn't be made: {Why(problem)}.", ("OK", null));
            return null;
        }
        if (Session.Entry(ModInfo.Path) is { } info)
            Session.Select(info);
        View.Status = $"Made {opened.Name} in the mods folder.";
        return opened;
    }
}
