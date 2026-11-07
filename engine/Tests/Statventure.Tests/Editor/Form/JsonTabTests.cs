using Statventure.Audio;
using Statventure.Editor.Form;
using Statventure.Editor.Session;
using Statventure.Records;
using Statventure.Tests.Records;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor.Form;

/// <summary>
/// The JSON tab (M11-18, P22): the entry's JSON form, read-only, of its draft when it builds and else of the last
/// one that did (said so); copy to the clipboard (the document's, a stand-in in tests) and export to the file the
/// file browser picks (a scratch folder's here); at most the first 2,000 lines shown; the tabs by click and keys.
/// </summary>
public class JsonTabTests
{
    const string Harp = "audio/instruments/harp";

    static EditorHarness Open(params EditorEntry[] entries) =>
        FormTesting.Open(entries.Length > 0 ? entries : [FormTesting.Entry(Harp, FormTesting.AudioPack()[Harp])]);

    static UiElement Tab(EditorHarness editor, string name) => editor.Document.QuerySelector($".tab[data-tab={name}]")!;

    static string? Shown(EditorHarness editor) => editor.Document.QuerySelector(".json-text")?.TextContent;

    static string Note(EditorHarness editor) => editor.Document.QuerySelector(".json-note")!.TextContent;

    static UiElement Button(EditorHarness editor, string action) => editor.Document.QuerySelector($".json-bar [data-action={action}]")!;

    static ScratchExportPicker Picker(EditorHarness editor) => (ScratchExportPicker)editor.Form().ExportPicker!;

    [Fact]
    public void TheTabShowsTheEntrysJsonForm()
    {
        using var editor = Open();
        var harp = (InstrumentSpec)editor.Session.Selected!.Saved!;
        Assert.Equal("true", Tab(editor, "form").GetAttribute("aria-selected"));
        Assert.True(editor.Document.QuerySelector(".json-view")!.HasAttribute("hidden"));
        editor.Press(Tab(editor, "json"));
        Assert.Equal(FormTab.Json, editor.Form().Tab);
        Assert.Equal(("false", "true"), (Tab(editor, "form").GetAttribute("aria-selected"), Tab(editor, "json").GetAttribute("aria-selected")));
        Assert.True(editor.Document.QuerySelector(".form-panel")!.HasAttribute("hidden"));
        Assert.False(editor.Document.QuerySelector(".json-view")!.HasAttribute("hidden"));
        Assert.Equal(RecordSerializer.ToJson(harp), Shown(editor));
        Assert.Equal("The entry's JSON form, read-only. Import a JSON file to bring one in.", Note(editor));
        // It follows each commit.
        editor.Press(Tab(editor, "form"));
        editor.Enter(editor.Row("$.level").Control("input", "number"), "0.25");
        editor.Press(Tab(editor, "json"));
        Assert.Equal(RecordSerializer.ToJson(harp with { Level = 0.25 }), Shown(editor));
        editor.Session.Undo(editor.Session.Selected!);
        Assert.Equal(RecordSerializer.ToJson(harp), Shown(editor));
    }

    [Fact]
    public void ADraftThatDoesntBuildShowsTheLastThatDid()
    {
        using var editor = Open();
        var entry = editor.Session.Selected!;
        editor.Form().Tab = FormTab.Json;
        var json = Shown(editor);
        editor.Session.Commit(entry, entry.Draft!.With("kind", DraftValue.Missing));
        Assert.Equal(json, Shown(editor));
        Assert.Equal("The entry doesn't build now: this is the last version that did.", Note(editor));
        Assert.True(editor.Document.QuerySelector(".json-view")!.HasClass("stale"));
        Assert.False(Button(editor, "copy").HasAttribute("disabled"));
        // A new entry that never built has no JSON form: nothing to copy or export.
        var made = editor.Session.Add("audio/instruments/new", RecordDraft.Blank(typeof(InstrumentSpec)));
        editor.Session.Select(made);
        editor.Frame();
        Assert.Equal(FormTab.Json, editor.Form().Tab);
        Assert.Null(Shown(editor));
        Assert.Equal("The entry doesn't build yet, so it has no JSON form.", Note(editor));
        Assert.True(Button(editor, "copy").HasAttribute("disabled"));
        Assert.True(Button(editor, "export").HasAttribute("disabled"));
        Assert.False(editor.Form().CopyJson());
        Assert.Null(editor.Done(editor.Ask(_ => editor.Form().ExportJson())));
    }

    [Fact]
    public void CopyPutsItOnTheClipboard()
    {
        using var editor = Open();
        editor.Form().Tab = FormTab.Json;
        editor.Press(Button(editor, "copy"));
        Assert.Equal(Shown(editor), editor.Document.Clipboard.ReadText());
        Assert.Equal($"Copied the JSON form of {Harp}", editor.View.Status);
    }

    [Fact]
    public void ExportWritesTheFileTheBrowserPicks()
    {
        using var editor = Open();
        editor.Form().Tab = FormTab.Json;
        var picker = Picker(editor);
        editor.Press(Button(editor, "export"));
        editor.Frame();
        Assert.Equal([($"Export {Harp}", "harp.json")], picker.Asked);
        var file = Path.Combine(picker.Folder, "harp.json");
        Assert.Equal(Shown(editor), File.ReadAllText(file));
        Assert.Equal($"Exported {Harp} to {file}", editor.View.Status);
        // What is exported reads back as the record (a JSON file comes back through Import).
        var read = RecordSerializer.FromJson<InstrumentSpec>(File.ReadAllText(file));
        Assert.Equal(RecordSerializer.ToBinary((InstrumentSpec)editor.Session.Selected!.Saved!), RecordSerializer.ToBinary(read));
        // Cancelled: nothing written.
        picker.Cancel = true;
        picker.Next = "other.json";
        Assert.Null(editor.Done(editor.Ask(_ => editor.Form().ExportJson())));
        Assert.False(File.Exists(Path.Combine(picker.Folder, "other.json")));
        // None set: the file browser (step 8) picks.
        editor.Form().ExportPicker = null;
        Assert.Same(editor.View.Part<Statventure.Editor.Browser.FileBrowserPart>(), editor.Form().ExportPicker);
        Assert.False(Button(editor, "export").HasAttribute("disabled"));
    }

    [Fact]
    public void AnExportThatCantBeWrittenSaysWhy()
    {
        using var editor = Open();
        var picker = Picker(editor);
        // The picked file is a folder: the write fails.
        Directory.CreateDirectory(Path.Combine(picker.Folder, "taken.json"));
        picker.Next = "taken.json";
        var exported = editor.Ask(_ => editor.Form().ExportJson());
        editor.Frame();
        var dialog = editor.View.Dialogs.Top!;
        Assert.Equal("Export failed", dialog.Element.ElementChildren.First().TextContent);
        Assert.StartsWith($"The JSON form of {Harp} couldn't be written to {Path.Combine(picker.Folder, "taken.json")}: ", dialog.Content.TextContent);
        editor.Press(editor.DialogButton("OK"));
        Assert.Null(editor.Done(exported));
    }

    [Fact]
    public void ALongJsonFormShowsItsFirstLines()
    {
        var sample = FormScreens.Sample();
        // Many parts in a bulk field: a long JSON form, a short form (its summary).
        sample = new Kinds { Text = "t", Part = sample.Part, Parts = Enumerable.Range(0, 600).ToDictionary(i => $"p{i}", i => new Part { Name = $"n{i}" }) };
        using var editor = Open(FormTesting.Entry("kinds/long", sample));
        editor.Form().Tab = FormTab.Json;
        var json = RecordSerializer.ToJson(sample);
        var lines = json.Split('\n');
        Assert.True(lines.Length > FormPart.JsonLines);
        Assert.Equal(string.Join('\n', lines.Take(FormPart.JsonLines)), Shown(editor));
        Assert.Equal($"{lines.Length - FormPart.JsonLines:N0} more lines: copy or export the entry for all of it.", editor.Document.QuerySelector(".json-more")!.TextContent);
        editor.Form().CopyJson();
        Assert.Equal(json, editor.Document.Clipboard.ReadText());
    }

    [Fact]
    public void TheTabsGoByKeysAndStayChosen()
    {
        using var editor = Open(FormTesting.Entry(Harp, FormTesting.AudioPack()[Harp]), FormTesting.Entry("audio/instruments/bell", FormTesting.AudioPack()["audio/instruments/bell"]));
        editor.Press(Tab(editor, "form"));
        Assert.Equal(("0", "-1"), (Tab(editor, "form").GetAttribute("tabindex"), Tab(editor, "json").GetAttribute("tabindex")));
        editor.Key("ArrowRight", "ArrowRight");
        Assert.Equal(FormTab.Json, editor.Form().Tab);
        Assert.Same(Tab(editor, "json"), editor.Document.FocusedElement);
        editor.Key("ArrowLeft", "ArrowLeft");
        Assert.Equal(FormTab.Form, editor.Form().Tab);
        Assert.Same(Tab(editor, "form"), editor.Document.FocusedElement);
        editor.Key("ArrowLeft", "ArrowLeft");
        Assert.Equal(FormTab.Json, editor.Form().Tab);
        // The tab stays when another entry is selected.
        editor.Session.Select(editor.Session.Entry("audio/instruments/bell"));
        editor.Frame();
        Assert.Equal(FormTab.Json, editor.Form().Tab);
        Assert.Equal(RecordSerializer.ToJson((InstrumentSpec)editor.Session.Selected!.Saved!), Shown(editor));
    }

    [Fact]
    public void FilesAndUnreadableEntriesSayWhatTheyAre()
    {
        using var editor = Open(EditorEntry.ForFile("ui/fonts/lato.ttf", 123456, EntrySource.Game), EditorEntry.ForUnreadable("zz/broken", "its content is damaged", EntrySource.Pack));
        Assert.Equal("A file of 123,456 bytes. It changes only by importing it again.", editor.Document.QuerySelector(".form-about")!.TextContent);
        Assert.Null(editor.Document.QuerySelector(".form-tabs"));
        Assert.Equal("File", editor.Document.QuerySelector(".form-kind")!.TextContent);
        Assert.Null(editor.Form().JsonOf(editor.Session.Selected!));
        editor.Session.Select(editor.Session.Entry("zz/broken"));
        editor.Frame();
        Assert.Equal("This entry can't be read: its content is damaged", editor.Document.QuerySelector(".form-about")!.TextContent);
        Assert.True(editor.Document.QuerySelector(".form-head [data-action=duplicate]")!.HasAttribute("disabled"));
        // Nothing selected, nothing shown (the empty form pane).
        editor.Session.Select(null);
        editor.Frame();
        Assert.Empty(editor.Document.QuerySelector(".form-part")!.Children);
    }
}
