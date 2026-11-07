using System.Numerics;
using Statventure.Editor.Form;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Records;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Records;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor.Form;

/// <summary>
/// The form driven as the user drives it (Docs/Editor.md, "Method": the UI's input, the session's data checked):
/// an editor on entries shown as step 8 shows them, and the presses, typing and choices of a user.
/// </summary>
static class FormTesting
{
    /// <summary>The test kinds the session knows besides the engine's (New offers them).</summary>
    public static readonly RecordTypes Types = new(RecordFixtures.Types);

    /// <summary>An editor (tall, so rows need little scrolling) with <paramref name="entries"/> shown, the pack writable, the first selected.</summary>
    public static EditorHarness Open(IEnumerable<EditorEntry> entries, float width = 1280, float height = 900, bool writable = true, params Type[] parts)
    {
        var editor = new EditorHarness(width, height, 1, parts);
        editor.Form().ExportPicker = new ScratchExportPicker();
        editor.Session.Types = Types;
        editor.Session.Writable = writable;
        var list = entries.ToList();
        editor.Session.ShowEntries(list);
        if (list.Count > 0)
            editor.Session.Select(editor.Session.Entries.First(e => e.Path == list[0].Path));
        editor.Frame();
        return editor;
    }

    public static EditorHarness Open(params EditorEntry[] entries) => Open(entries, 1280, 900);

    /// <summary>A record entry of the open pack.</summary>
    public static EditorEntry Entry(string path, object record, EntrySource source = EntrySource.Pack, bool readOnly = false) =>
        EditorEntry.ForRecord(path, record, source, readOnly);

    public static FormPart Form(this EditorHarness editor) => editor.View.Part<FormPart>();

    public static EntryActions Actions(this EditorHarness editor) => editor.View.Part<EntryActions>();

    /// <summary>The selected entry's current draft built (it must build).</summary>
    public static T Built<T>(this EditorHarness editor)
    {
        var build = editor.Session.Selected!.Build!;
        Assert.True(build.Built, string.Join("; ", build.Problems));
        return (T)build.Value!;
    }

    /// <summary>The row (or group) of the value at <paramref name="path"/> in the form.</summary>
    public static UiElement Row(this EditorHarness editor, string path) =>
        editor.Document.Body.Descendants().FirstOrDefault(e => e.GetAttribute("data-path") == path)
        ?? throw new InvalidOperationException($"no row at {path}");

    /// <summary>The row's own controls (its line, not its nested rows').</summary>
    public static IEnumerable<UiElement> Own(this UiElement row)
    {
        return row.ElementChildren.First().Descendants().Where(e => e.Tag is "input" or "select" or "button");
    }

    /// <summary>The row's first control of <paramref name="tag"/> (and input type).</summary>
    public static UiElement Control(this UiElement row, string tag, string? type = null) =>
        row.Own().FirstOrDefault(e => e.Tag == tag && (type is null || e.InputType == type))
        ?? throw new InvalidOperationException($"no {tag} {type} in the row {row.GetAttribute("data-path")}");

    /// <summary>The row's button with this text.</summary>
    public static UiElement Button(this UiElement row, string text) =>
        row.Own().FirstOrDefault(e => e.Tag == "button" && e.TextContent == text)
        ?? throw new InvalidOperationException($"no button {text} in the row {row.GetAttribute("data-path")}");

    /// <summary>The problem lines under the row (its own, not its nested rows').</summary>
    public static IReadOnlyList<string> Problems(this UiElement row) =>
        [.. row.ElementChildren.Where(c => c.HasClass("problem")).Select(c => c.TextContent)];

    /// <summary>A press of the main button on the element, scrolled into view first.</summary>
    public static void Press(this EditorHarness editor, UiElement element)
    {
        element.ScrollIntoView(ScrollAlign.Center);
        editor.Frame();
        editor.Click(editor.Middle(element));
    }

    /// <summary>Types <paramref name="text"/> into a field as keys (pressed into focus, its text selected first); no Enter.</summary>
    public static void Type(this EditorHarness editor, UiElement field, string text, bool replace = true)
    {
        if (editor.Document.FocusedElement != field)
            editor.Press(field);
        if (replace)
            editor.Key("a", "KeyA", KeyModifiers.Control);
        foreach (var c in text)
            editor.Key(c.ToString(), "", char.IsUpper(c) ? KeyModifiers.Shift : KeyModifiers.None);
    }

    /// <summary>Types <paramref name="text"/> into a field and presses Enter (a commit, P12).</summary>
    public static void Enter(this EditorHarness editor, UiElement field, string text)
    {
        editor.Type(field, text);
        editor.Key("Enter", "Enter");
    }

    /// <summary>Chooses the option with this text in a select: the press opens its list, arrows move to it, Enter picks it.</summary>
    public static void Choose(this EditorHarness editor, UiElement select, string option)
    {
        var options = select.Options.Select(o => o.TextContent).ToList();
        var target = options.IndexOf(option);
        Assert.True(target >= 0, $"no option {option} in [{string.Join(", ", options)}]");
        editor.Press(select);
        Assert.NotNull(editor.Input.SelectPopup);
        var from = Math.Max(select.SelectedIndex, 0);
        for (var i = from; i < target; i++)
            editor.Key("ArrowDown", "ArrowDown");
        for (var i = from; i > target; i--)
            editor.Key("ArrowUp", "ArrowUp");
        editor.Key("Enter", "Enter");
        Assert.Null(editor.Input.SelectPopup);
    }

    /// <summary>Ctrl+Z (Undo) and Ctrl+Y (Redo) as keys.</summary>
    public static void Undo(this EditorHarness editor) => editor.Key("z", "KeyZ", KeyModifiers.Control);

    public static void Redo(this EditorHarness editor) => editor.Key("y", "KeyY", KeyModifiers.Control);

    /// <summary>
    /// Starts an asking action (New, Duplicate, Rename, Remove, Revert, an export) as the editor's own listeners do,
    /// in a task of the document's loop, so what it does after its dialog closes runs on the loop too.
    /// </summary>
    public static Task<T> Ask<T>(this EditorHarness editor, Func<EntryActions, Task<T>> ask)
    {
        Task<T>? task = null;
        editor.Document.RunScript(() => task = ask(editor.Actions()));
        return task!;
    }

    /// <summary>
    /// What an asking action gave, once its dialog closed: a frame first (its continuations run on the document's
    /// loop), then asserted complete (a test never waits on an open dialog).
    /// </summary>
    public static T Done<T>(this EditorHarness editor, Task<T> task)
    {
        editor.Frame();
        Assert.True(task.IsCompleted, editor.View.Dialogs.Top is null ? "the action didn't finish" : "the dialog is still open");
        return task.Result;
    }

    /// <summary>The topmost dialog's field (input or select) after its label.</summary>
    public static UiElement DialogField(this EditorHarness editor, string tag) =>
        editor.View.Dialogs.Top!.Content.Descendants().First(e => e.Tag == tag);

    public static UiElement DialogButton(this EditorHarness editor, string text) =>
        editor.View.Dialogs.Top!.Buttons.ElementChildren.First(b => b.TextContent == text);

    public static string DialogProblem(this EditorHarness editor) =>
        editor.View.Dialogs.Top!.Content.Descendants().First(e => e.HasClass("dialog-problem")).TextContent;

    /// <summary>The records of the repo's audio.pak, by path.</summary>
    public static IReadOnlyDictionary<string, object> AudioPack()
    {
        using var pack = PackFile.Open(Path.Combine(TestPaths.Packs, "audio.pak"));
        var records = new SortedDictionary<string, object>(StringComparer.Ordinal);
        foreach (var entry in pack.Entries.Where(e => e.IsRecord))
        {
            var content = pack.Read(entry);
            records[entry.Path] = RecordSerializer.FromBinary(RecordTypes.Engine.Find(RecordSerializer.RootTypeName(content))!, content);
        }
        return records;
    }
}

/// <summary>Stands in for the file browser's export picking (step 8): gives a file in a scratch folder of the test's, or what <see cref="Next"/> says.</summary>
public sealed class ScratchExportPicker : IExportFilePicker
{
    /// <summary>The folder files are picked in (made for each editor, in the run's scratch folder).</summary>
    public string Folder { get; } = Path.Combine(PerUserFolders.Root, "exports", Guid.NewGuid().ToString("N"));

    /// <summary>What the next pick gives: a file name in <see cref="Folder"/> (null: the suggested name), or cancelled.</summary>
    public string? Next;
    public bool Cancel;
    public readonly List<(string Title, string Suggested)> Asked = [];

    public Task<string?> PickExportFile(string title, string suggestedName)
    {
        Asked.Add((title, suggestedName));
        if (Cancel)
            return Task.FromResult<string?>(null);
        Directory.CreateDirectory(Folder);
        return Task.FromResult<string?>(Path.Combine(Folder, Next ?? suggestedName));
    }
}
