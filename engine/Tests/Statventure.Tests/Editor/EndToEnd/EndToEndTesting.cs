using Statventure.Editor.Browser;
using Statventure.Editor.Entries;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Platform;
using Statventure.Records;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Statventure.UI.Dom;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.EndToEnd;

/// <summary>
/// The whole editor driven as a modder drives it (Docs/Editor.md, "How it works", step 14): a folder of the test's own
/// in the run's scratch folder (<see cref="PerUserFolders.Root"/>, never the player's real one) holding the player's
/// folder (<c>player/</c>, its mods), an installed game's folder (<c>game/packs/</c>, copies of the repo's packs) and
/// the files the modder imports (<c>downloads/</c>); the editor's top bar, dialogs, entry list, form and reference
/// lists worked through the UI's input.
/// </summary>
static class EndToEndTesting
{
    public sealed class Folders : IDisposable
    {
        public Folders()
        {
            Root = Path.Combine(PerUserFolders.Root, "editor-end-to-end", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public string this[string relative] => Path.Combine(Root, relative);

        /// <summary>The player's game folder (the editor's and the game's per-user folder).</summary>
        public string Player => this["player"];

        /// <summary>The player's mods folder.</summary>
        public string Mods => this["player/mods"];

        /// <summary>An installed game's folder: the repo's packs copied into <c>game/packs/</c> (what the game mounts beside its program).</summary>
        public string Game()
        {
            var packs = this["game/packs"];
            Directory.CreateDirectory(packs);
            foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
                File.Copy(pack, Path.Combine(packs, Path.GetFileName(pack)), overwrite: true);
            return this["game"];
        }

        /// <summary>A text file written at <paramref name="relative"/> (its folders made), giving its full path.</summary>
        public string Write(string relative, string text)
        {
            var file = this[relative];
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, text);
            return file;
        }

        /// <summary>One of the tests' models (<c>Data/Models/&lt;model&gt;.glb</c>) copied into <c>downloads/</c> as <paramref name="name"/>.</summary>
        public string Download(string model, string name)
        {
            var file = this[$"downloads/{name}"];
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.Copy(Path.Combine(Render3DReferenceTests.ModelDirectory, model + ".glb"), file, overwrite: true);
            return file;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }

    /// <summary>The editor over the repo's packs (the harness's game data), the player's folder <paramref name="player"/>, nothing open.</summary>
    public static EditorHarness Modder(string player, float width = 1280, float height = 900)
    {
        var editor = new EditorHarness(width, height);
        editor.Session.PlayerFolder = player;
        editor.Frame();
        return editor;
    }

    /// <summary>A top bar's button clicked (it must be enabled).</summary>
    public static void Click(this EditorHarness editor, EditorCommand command)
    {
        var button = editor.View.Button(command) ?? throw new InvalidOperationException($"no button for {command}");
        Assert.False(button.HasAttribute("disabled"), $"{command} is disabled");
        editor.Click(button);
    }

    /// <summary>
    /// The entry at <paramref name="path"/> found in the entry list as a user finds one among the game's: Ctrl+F, its
    /// path typed into the filter, its row clicked (selected, as a click opens it), Escape in the filter to empty it.
    /// </summary>
    public static EditorEntry Pick(this EditorHarness editor, string path)
    {
        var entry = editor.Session.Entry(path) ?? throw new InvalidOperationException($"no entry {path}");
        var list = editor.View.Part<EntryList>();
        editor.Key("f", "KeyF", KeyModifiers.Control);
        Assert.Same(list.Filter, editor.Document.FocusedElement);
        foreach (var c in path)
            editor.Key(c.ToString(), "");
        var row = list.Row(entry) ?? throw new InvalidOperationException($"the list shows no row for {path}");
        editor.Press(row);
        Assert.Same(entry, editor.Session.Selected);
        editor.Press(list.Filter);
        editor.Key("Escape", "Escape");
        Assert.Equal("", list.Filter.Value);
        return entry;
    }

    /// <summary>How New entry (P15) is started: the top bar's button or Ctrl+N (the manager's decision of 2026-11-08, after step 14).</summary>
    public enum NewBy
    {
        Button,
        Key,
    }

    /// <summary>
    /// A new entry made: New entry started by the top bar's button or Ctrl+N, the dialog's kind chosen and the name
    /// typed, Enter; the entry made is new, selected and shown in the form.
    /// </summary>
    public static EditorEntry NewEntry(this EditorHarness editor, NewBy by, string kind, string name)
    {
        var before = editor.Session.Entries.ToHashSet();
        if (by == NewBy.Button)
            editor.Click(EditorCommand.NewEntry);
        else
            editor.Key("n", "KeyN", KeyModifiers.Control);
        Assert.Equal("New entry", editor.View.Dialogs.Top?.Element.ElementChildren.First().TextContent);
        editor.Choose(editor.DialogField("select"), kind);
        editor.Enter(editor.DialogField("input"), name);
        Assert.Null(editor.View.Dialogs.Top);
        var made = Assert.Single(editor.Session.Entries, e => !before.Contains(e));
        Assert.Equal(EntrySource.New, made.Source);
        Assert.Same(made, editor.Session.Selected);
        Assert.Equal(made.Path, editor.Document.QuerySelector(".form-path")?.TextContent);
        return made;
    }

    /// <summary>
    /// A file imported through the Import button: the file browser's path field clicked, the file's path typed, Enter;
    /// then the import dialog's path field (holding <paramref name="suggested"/>) typed over with <paramref name="path"/>, Enter.
    /// </summary>
    public static void ImportFile(this EditorHarness editor, string file, string suggested, string path)
    {
        editor.Click(EditorCommand.Import);
        var browser = editor.View.Part<FileBrowserPart>().Current ?? throw new InvalidOperationException("the browser didn't open");
        editor.Press(browser.PathField);
        foreach (var c in file)
            editor.Key(c.ToString(), "", char.IsUpper(c) ? KeyModifiers.Shift : KeyModifiers.None);
        editor.Key("Enter", "Enter");
        var dialog = editor.View.Part<Statventure.Editor.Import.ImportPart>().Current ?? throw new InvalidOperationException("the import dialog didn't open");
        Assert.Same(dialog.PathField, editor.Document.FocusedElement);
        Assert.Equal(suggested, dialog.PathField.Value);
        editor.Enter(dialog.PathField, path);
        Assert.Null(editor.View.Part<Statventure.Editor.Import.ImportPart>().Current);
        // Nothing else opens: the Enter that imported isn't taken by what has the focus once the dialog closes.
        Assert.True(editor.View.Dialogs.Top is null, $"a dialog opened after the import: {editor.View.Dialogs.Top?.Element.ElementChildren.First().TextContent} (the focus on {editor.Document.FocusedElement?.TextContent})");
    }

    /// <summary>A number or text typed into the row's field at <paramref name="path"/> and Enter pressed (one commit, P12).</summary>
    public static void Set(this EditorHarness editor, string path, string text, string tag = "input", string? type = null)
    {
        var row = editor.Row(path);
        var field = row.Own().First(e => e.Tag == tag && (type is null ? e.InputType is "text" or "number" : e.InputType == type));
        editor.Enter(field, text);
    }

    /// <summary>The option with this text chosen in the row's first select at <paramref name="path"/> (an enum's members, a union's subtypes).</summary>
    public static void Pick(this EditorHarness editor, string path, string option) =>
        editor.Choose(editor.Row(path).Control("select"), option);

    /// <summary>The mod info's own form's field labelled <paramref name="label"/> (Name, Author, Version, Description).</summary>
    public static UiElement ModInfoField(this EditorHarness editor, string label) =>
        editor.Document.QuerySelectorAll(".mod-info .mod-info-row")
            .First(row => row.ElementChildren.First().TextContent == label)
            .Descendants().First(e => e.HasClass("mod-info-field"));

    /// <summary>A row's button (Add, Make, Remove …) pressed.</summary>
    public static void Press(this EditorHarness editor, string path, string button) =>
        editor.Press(editor.Row(path).Button(button));

    /// <summary>A nullable's "set" box at <paramref name="path"/> ticked.</summary>
    public static void Tick(this EditorHarness editor, string path)
    {
        var box = editor.Row(path).Own().First(e => e.Tag == "input" && e.InputType == "checkbox");
        Assert.False(box.Checked);
        editor.Press(box);
        Assert.True(box.Checked);
    }

    /// <summary>
    /// A reference field at <paramref name="path"/> focused (its list opens, M11-11), <paramref name="typed"/> typed to
    /// filter it, and the option <paramref name="option"/> moved to with Down and picked with Enter (one commit).
    /// </summary>
    public static void Refer(this EditorHarness editor, string path, string typed, string option)
    {
        var field = editor.Row(path).Own().First(e => e.Tag == "input" && e.InputType == "text");
        editor.Press(field);
        var references = editor.View.Part<Statventure.Editor.References.ReferenceListPart>();
        Assert.NotNull(references.List);
        if (typed.Length > 0)
            editor.Type(field, typed);
        var options = references.List!.ElementChildren.Where(e => e.HasClass("reference-option")).Select(e => e.TextContent).ToList();
        var at = options.IndexOf(option);
        Assert.True(at >= 0, $"the list offers [{string.Join(", ", options)}], not {option}");
        for (var i = 0; i <= at; i++)
            editor.Key("ArrowDown", "ArrowDown");
        editor.Key("Enter", "Enter");
        Assert.Equal(option, field.Value);
    }

    /// <summary>The topmost dialog's list items' texts (a refusal's entries).</summary>
    public static string[] Listed(this EditorHarness editor) =>
        [.. (editor.View.Dialogs.Top ?? throw new InvalidOperationException("no dialog is open")).Content.Descendants().Where(e => e.Tag == "li").Select(e => e.TextContent)];

    /// <summary>A file opened through the Open button and the file browser: the path field clicked, the file's path typed, Enter.</summary>
    public static void OpenFile(this EditorHarness editor, string file)
    {
        editor.Click(EditorCommand.Open);
        var browser = editor.View.Part<FileBrowserPart>().Current ?? throw new InvalidOperationException("the browser didn't open");
        editor.Press(browser.PathField);
        Assert.Same(browser.PathField, editor.Document.FocusedElement);
        foreach (var c in file)
            editor.Key(c.ToString(), "", char.IsUpper(c) ? KeyModifiers.Shift : KeyModifiers.None);
        editor.Key("Enter", "Enter");
        editor.Frame();
    }

    /// <summary>The JSON tab chosen and its text read (what the editor shows of the entry's JSON form), then the Form tab again.</summary>
    public static string ShownJson(this EditorHarness editor)
    {
        var tabs = editor.Document.QuerySelectorAll(".form-tabs [data-tab]").ToList();
        editor.Press(tabs.Single(t => t.GetAttribute("data-tab") == "json"));
        var text = editor.Document.QuerySelector(".json-view .json-text")?.TextContent ?? throw new InvalidOperationException("the JSON tab shows no text");
        Assert.Null(editor.Document.QuerySelector(".json-view .json-more"));
        editor.Press(tabs.Single(t => t.GetAttribute("data-tab") == "form"));
        return text;
    }

    /// <summary>A record's binary form as text (records holding lists compare by it, not by their equality).</summary>
    public static string Binary(Type type, object value) => Convert.ToHexStringLower(RecordSerializer.ToBinary(type, value));

    /// <summary>The selected entry's current draft built (it must build).</summary>
    public static object Built(this EditorEntry entry)
    {
        var build = entry.Build!;
        Assert.True(build.Built, string.Join("; ", build.Problems));
        return build.Value!;
    }
}
