using System.Globalization;
using System.Numerics;
using Statventure.Editor.Browser;
using Statventure.Editor.Form;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Records;
using Statventure.Storage;
using Statventure.UI.Dom;

namespace Statventure.Editor.Import;

/// <summary>
/// Import (step 12; Docs/Editor.md, "Import (step 12)"; M11-8, M11-14, M11-18, P27–P29): the Import command (the top bar's
/// button) asks the file browser for a file of an ending the editor imports; files dropped on the window come through
/// step 8's queue in the order dropped. Each file in turn (the manager's decision of 2026-11-07, Q4): the import dialog
/// with the path suggested from the kind's folder and the file's name (editable); then, only when that path is taken,
/// the replace question, or for a record of the same kind whose values differ the keep-or-take list (P29); Cancel skips
/// only that file. A file that can't be imported is said in a dialog with why. The entries join the session as unsaved
/// work (the pack isn't written), the last imported selected.
/// </summary>
public sealed class ImportPart : EditorPart
{
    bool _running;

    protected internal override void Attach(UiElement? element)
    {
        View.Commands.Handle(EditorCommand.Import, request =>
        {
            View.Document.RunScript(() => _ = Browse());
            return true;
        }, () => Session.CanImportFiles);
        Session.ImportsQueued += () => View.Document.RunScript(() => _ = TakeQueued());
    }

    FileBrowserPart Browser => View.Part<FileBrowserPart>();

    /// <summary>Asks the file browser for a file to import (the endings the editor imports; from Documents unless a recent place is known) and imports it.</summary>
    public async Task<IReadOnlyList<EditorEntry>> Browse()
    {
        var file = await Browser.Pick(new BrowserRequest("Import a file", "Import", Session.ImportEndings, Fallback: BrowserPlaceKind.Documents));
        return file is null ? [] : await ImportFiles([file]);
    }

    /// <summary>Imports the files waiting in the session's queue (dropped on the window, P27), unless an import is running (it takes them when done).</summary>
    public async Task TakeQueued()
    {
        if (!_running && Session.PendingImports.Count > 0)
            await ImportFiles(Session.TakeImports());
    }

    /// <summary>
    /// Imports <paramref name="files"/> one at a time in their order, each asked about (Q4), then any queued meanwhile.
    /// Gives the entries imported (the main entry of each file). With no pack that takes changes open, one dialog says so
    /// and nothing is imported. While an import runs, files given are queued after it.
    /// </summary>
    public async Task<IReadOnlyList<EditorEntry>> ImportFiles(IReadOnlyList<string> files)
    {
        if (_running)
        {
            Session.QueueImports(files);
            return [];
        }
        _running = true;
        var made = new List<EditorEntry>();
        try
        {
            while (true)
            {
                if (!Session.CanImportFiles)
                {
                    var why = Session.Pack is { } pack ? $"{pack.Name} is read-only ({pack.ReadOnlyReason})" : "no pack is open";
                    await View.Dialogs.Ask("Nothing imported", $"{Names(files)} can't be imported: {why}. Open a mod or make one first.", ("OK", null));
                }
                else
                    foreach (var file in files)
                        if (await ImportOne(file) is { } entry)
                            made.Add(entry);
                if (Session.PendingImports.Count == 0)
                    break;
                files = Session.TakeImports();
            }
        }
        finally
        {
            _running = false;
        }
        return made;
    }

    /// <summary>Imports one file, asking (the import dialog, then the replace question or the keep-or-take list when its path is taken); gives the entry, or null when skipped or refused.</summary>
    async Task<EditorEntry?> ImportOne(string file)
    {
        ImportSource source;
        string suggested;
        RecordShape? kind;
        try
        {
            source = Session.ImportSourceOf(file);
            (suggested, kind) = Session.SuggestImportPath(source);
        }
        catch (ImportException problem)
        {
            await View.Dialogs.Ask("Can't import", $"{Path.GetFileName(file)} can't be imported: {Sentence(problem.Message)}", ("OK", null));
            return null;
        }
        var plan = await AskPath(source, suggested, kind);
        if (plan is null)
            return null;
        var take = await Confirm(plan);
        if (take is null)
            return null;
        var entries = Session.Import(plan, take);
        var main = entries[plan.Targets.ToList().IndexOf(plan.Main)];
        Session.Select(main);
        View.Status = plan.Main.Action switch
        {
            ImportAction.ImportAgain => $"Imported {source.Name} again into {main.Path}.",
            ImportAction.Replace => $"Imported {source.Name} as {main.Path}, replacing what was there.",
            _ => $"Imported {source.Name} as {main.Path}.",
        };
        return main;
    }

    // ---------- The import dialog ----------

    /// <summary>The import dialog while it is open (tests), else null.</summary>
    public ImportDialog? Current { get; private set; }

    /// <summary>
    /// The import dialog: what the file imports as, the path field with <paramref name="suggested"/> (P28: editable), a line
    /// for what is wrong with the path or the import there, Import and Cancel. Gives the plan, or null when cancelled.
    /// </summary>
    async Task<ImportPlan?> AskPath(ImportSource source, string suggested, RecordShape? kind)
    {
        var dialog = new ImportDialog(this, source, suggested, kind);
        Current = dialog;
        try
        {
            // (The plan is made only as Import closes the dialog: cancelled, there is none.)
            await dialog.Closed;
            return dialog.Plan;
        }
        finally
        {
            Current = null;
        }
    }

    /// <summary>The import dialog (<see cref="Current"/>): its parts, for tests and the screens' fixture.</summary>
    public sealed class ImportDialog
    {
        readonly ImportPart _part;
        readonly EditorDialog _dialog;
        readonly UiElement _problem;

        internal ImportDialog(ImportPart part, ImportSource source, string suggested, RecordShape? kind)
        {
            _part = part;
            Source = source;
            _dialog = part.View.Dialogs.Create("Import");
            var what = kind is null ? "a file" : Article(kind.DisplayName.ToLowerInvariant());
            _dialog.Content.Append("p", text: $"{source.Name} imports as {what}{(source.Way == ImportWay.Record ? " (its JSON form)" : "")}. Its entry's path:");
            var line = _dialog.Content.Append("label", "import-field");
            line.Append("span", "import-label", "Path");
            PathField = line.Append("input", "import-path");
            PathField.SetAttribute("type", "text");
            PathField.Value = suggested;
            _problem = _dialog.Content.Append("div", "import-problem");
            PathField.AddEventListener<UiKeyboardEvent>("keydown", e =>
            {
                if (e.Key == "Enter" && !e.CtrlKey && !e.AltKey && !e.MetaKey && !e.ShiftKey)
                    Act();
            });
            PathField.AddEventListener("input", _ => _problem.TextContent = "");
            var button = _dialog.Buttons.Append("button", "primary", "Import");
            button.SetAttribute("type", "button");
            button.AddEventListener("click", _ => Act());
            _dialog.AddButton("Cancel", null);
            _dialog.Show();
        }

        public ImportSource Source { get; }

        public EditorDialog Dialog => _dialog;

        public UiElement PathField { get; }

        /// <summary>What is wrong with the path or the import there ("" when nothing).</summary>
        public string Problem => _problem.TextContent;

        /// <summary>The plan made when Import was chosen.</summary>
        public ImportPlan? Plan { get; private set; }

        internal Task<string?> Closed => _dialog.Closed;

        // (Shown, the dialog focuses the path field: its first control, Chromium's rule for a modal dialog.)

        /// <summary>Import: the file imported at the path typed (<see cref="EditorSession.PlanImport"/>), or the line says why it can't.</summary>
        public void Act()
        {
            try
            {
                Plan = _part.Session.PlanImport(Source, PathField.Value);
            }
            catch (ImportException problem)
            {
                _problem.TextContent = Sentence(problem.Message);
                return;
            }
            _dialog.Close("import");
        }
    }

    // ---------- The replace question and the keep-or-take list ----------

    /// <summary>
    /// When the plan's paths are taken: the replace question for what it replaces or imports again without a difference,
    /// and for each record imported again whose values differ the keep-or-take list (Q4, P29). Gives which differences are
    /// taken (nothing asked: none), or null when cancelled.
    /// </summary>
    async Task<Func<ImportDifference, bool>?> Confirm(ImportPlan plan)
    {
        var asked = plan.Targets.Where(t => t.Action == ImportAction.Replace || t.Action == ImportAction.ImportAgain && t.Comparison!.Differences.Count == 0).ToList();
        if (asked.Count > 0 && !await AskReplace(plan, asked))
            return null;
        var taken = new HashSet<ImportDifference>();
        foreach (var target in plan.Targets.Where(t => t.Action == ImportAction.ImportAgain && t.Comparison!.Differences.Count > 0))
        {
            if (await AskKeepOrTake(plan, target) is not { } chosen)
                return null;
            taken.UnionWith(chosen);
        }
        return taken.Contains;
    }

    async Task<bool> AskReplace(ImportPlan plan, IReadOnlyList<ImportTarget> targets)
    {
        var again = targets.All(t => t.Action == ImportAction.ImportAgain);
        var dialog = View.Dialogs.Create(again ? "Import again" : "Replace the entry");
        dialog.Element.AddClass("import-replace-dialog");
        foreach (var target in targets)
            dialog.Content.Append("p", text: target.Action == ImportAction.ImportAgain
                ? $"{target.Path} is already {Article(target.Kind!.DisplayName.ToLowerInvariant())}. Import {plan.Source.Name} again over it? Its bulk data comes from the file; no value edited in the form differs."
                : $"{target.Path} is already there ({Article(target.Existing!.KindName.ToLowerInvariant())}{(target.Existing.Source == EntrySource.Game ? " of the game's" : "")}). Replace it with the imported {(target.Kind?.DisplayName.ToLowerInvariant() ?? "file")}?"
                    + (target.Existing.Source == EntrySource.Game ? " The game's own entry shows again if the new one is removed." : "")
                    + " This can't be undone.");
        dialog.AddButton(again ? "Import again" : "Replace", "replace", primary: true);
        dialog.AddButton("Cancel", null);
        dialog.Show();
        return await dialog.Closed == "replace";
    }

    /// <summary>The keep-or-take list while it is open (tests), else null.</summary>
    public KeepOrTakeList? CurrentList { get; private set; }

    async Task<IReadOnlyList<ImportDifference>?> AskKeepOrTake(ImportPlan plan, ImportTarget target)
    {
        var list = new KeepOrTakeList(this, plan, target);
        CurrentList = list;
        try
        {
            return await list.Dialog.Closed == "import" ? list.Taken : null;
        }
        finally
        {
            CurrentList = null;
        }
    }

    /// <summary>
    /// The keep-or-take list (P29): a row per value that differs between the entry and the file (its name, the entry's
    /// value and the file's, each a choice; keep checked first), Keep all and Take all, then Import again or Cancel.
    /// </summary>
    public sealed class KeepOrTakeList
    {
        readonly List<(ImportDifference Difference, UiElement Keep, UiElement Take)> _rows = [];

        internal KeepOrTakeList(ImportPart part, ImportPlan plan, ImportTarget target)
        {
            Target = target;
            Dialog = part.View.Dialogs.Create("Import again");
            Dialog.Element.AddClass("import-again-dialog");
            var count = target.Comparison!.Differences.Count;
            Dialog.Content.Append("p", text: $"{target.Path} is already {Article(target.Kind!.DisplayName.ToLowerInvariant())}: its bulk data comes from {plan.Source.Name}. "
                + (count == 1 ? "One value differs" : $"{count} values differ") + " between the entry and the file: keep the entry's or take the file's.");
            var tools = Dialog.Content.Append("div", "import-tools");
            KeepAll = tools.Append("button", text: "Keep all");
            KeepAll.SetAttribute("type", "button");
            KeepAll.AddEventListener("click", _ => SetAll(take: false));
            TakeAll = tools.Append("button", text: "Take all");
            TakeAll.SetAttribute("type", "button");
            TakeAll.AddEventListener("click", _ => SetAll(take: true));
            var values = Dialog.Content.Append("div", "import-values");
            var head = values.Append("div", "import-row import-head");
            head.Append("span", "import-name", "Value");
            head.Append("span", "import-choice", "In the entry");
            head.Append("span", "import-choice", "In the file");
            var number = 0;
            foreach (var difference in target.Comparison.Differences)
            {
                var row = values.Append("div", "import-row");
                var name = row.Append("span", "import-name", difference.Name);
                name.SetAttribute("title", difference.Path.ToString());
                var group = $"import-value-{++number}";
                var keep = Choice(row, group, difference.Present, difference);
                var take = Choice(row, group, difference.File, difference);
                keep.Checked = true;
                _rows.Add((difference, keep, take));
            }
            var button = Dialog.AddButton("Import again", "import", primary: true);
            button.SetAttribute("autofocus", "");
            Dialog.AddButton("Cancel", null);
            Dialog.Show();
        }

        public ImportTarget Target { get; }

        public EditorDialog Dialog { get; }

        public UiElement KeepAll { get; }

        public UiElement TakeAll { get; }

        /// <summary>The rows: each difference with its two choices (the radio buttons).</summary>
        public IReadOnlyList<(ImportDifference Difference, UiElement Keep, UiElement Take)> Rows => _rows;

        /// <summary>The differences whose file value is chosen.</summary>
        public IReadOnlyList<ImportDifference> Taken => [.. _rows.Where(r => r.Take.Checked).Select(r => r.Difference)];

        void SetAll(bool take)
        {
            foreach (var (_, keep, takeIt) in _rows)
                (take ? takeIt : keep).Checked = true;
        }

        static UiElement Choice(UiElement row, string group, DraftValue shown, ImportDifference difference)
        {
            var label = row.Append("label", "import-choice");
            var radio = label.Append("input");
            radio.SetAttribute("type", "radio");
            radio.SetAttribute("name", group);
            if (Swatch(shown, difference) is { } colour)
                label.Append("span", "import-swatch").SetAttribute("style", $"background: {colour}");
            var text = Describe(shown, difference);
            label.Append("span", "import-text", text).SetAttribute("title", text);
            return radio;
        }
    }

    // ---------- Words ----------

    /// <summary>A value as the keep-or-take list shows it (P29): as the form writes it, a structure by its size or subtype.</summary>
    public static string Describe(DraftValue value, ImportDifference difference)
    {
        if (value == DraftValue.Missing)
            return "not given";
        if (value == DraftValue.Null)
            return "none";
        var shape = difference.Shape.Kind == ValueKind.Nullable ? difference.Shape.Element! : difference.Shape;
        return value switch
        {
            DraftList list => Count(list.Count, "element"),
            DraftMap map => Count(map.Count, "entry", "entries"),
            RecordDraft record => record.Shape.DisplayName,
            DraftLeaf leaf => leaf.Value switch
            {
                bool b => b ? "yes" : "no",
                string s => s.Length == 0 ? "(empty)" : s,
                Enum e => shape.Members.FirstOrDefault(m => m.Value.Equals(e))?.DisplayName ?? e.ToString(),
                Vector2 or Vector3 or Vector4 when Swatch(value, difference) is { } hex => hex,
                Vector2 or Vector3 or Vector4 => "(" + string.Join(", ", ValueText.Components(leaf.Value).Select(c => ValueText.Number(c))) + ")",
                float or double or sbyte or byte or short or ushort or int or uint or long or ulong => ValueText.Number(leaf.Value) + (difference.Field.Unit is { } unit ? " " + unit : ""),
                _ => ValueText.ReadOnly(leaf.Value),
            },
            _ => value.ToString() ?? "",
        };
    }

    /// <summary>The swatch's colour of a value under a colour mark (a vector's <c>#rrggbb</c> or <c>#rrggbbaa</c>, a text that is one), or null.</summary>
    public static string? Swatch(DraftValue value, ImportDifference difference)
    {
        if (!difference.Field.IsColour || value is not DraftLeaf leaf)
            return null;
        return leaf.Value switch
        {
            Vector3 or Vector4 => ValueText.Hex(ValueText.Components(leaf.Value)),
            string s when s.Length > 0 && ValueText.TryHex(s, 4, out _) => s,
            _ => null,
        };
    }

    static string Count(int n, string noun, string? nouns = null) =>
        n == 0 ? $"no {nouns ?? noun + "s"}" : n == 1 ? $"1 {noun}" : $"{n.ToString("N0", CultureInfo.InvariantCulture)} {nouns ?? noun + "s"}";

    /// <summary>"a model", "an instrument".</summary>
    static string Article(string noun) => (noun.Length > 0 && "aeiou".Contains(noun[0]) ? "an " : "a ") + noun;

    /// <summary>A problem as a sentence: ending with a full stop.</summary>
    static string Sentence(string text) => text.EndsWith('.') ? text : text + ".";

    static string Names(IReadOnlyList<string> files)
    {
        var names = files.Select(f => Path.GetFileName(f)).ToList();
        return names.Count == 1 ? names[0] : string.Join(", ", names.SkipLast(1)) + " and " + names[^1];
    }
}
