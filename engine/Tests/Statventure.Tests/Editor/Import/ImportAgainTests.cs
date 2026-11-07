using System.Numerics;
using Statventure.Audio;
using Statventure.Editor.Form;
using Statventure.Editor.Session;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Records;
using Statventure.Tests.Render3D;
using static Statventure.Tests.Editor.Import.ImportTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Import;

/// <summary>
/// Importing a file again over a record of its kind (M11-14, P29; the manager's decisions of 2026-11-07, Q1 and Q3): the
/// bulk data comes from the file; the single values that differ between the file and the entry's present values are
/// listed (by path along the kind's shape: a list holding bulk data by index with the file's length, another list or a
/// nullable whose structure differs as a whole), each kept or taken, keep by default, Keep all and Take all; the
/// result one step of the entry's history; over a game entry in a mod, the mod's copy (P7); and a record's JSON form
/// exported from the JSON tab comes back in the same way (M11-18).
/// </summary>
public class ImportAgainTests
{
    static byte[] Model(string name) => File.ReadAllBytes(TestModelPack.SourceOf(name));

    /// <summary>The editor with a model imported at <c>models/blocks</c> from blocks.glb, then edited in three values.</summary>
    static EditorEntry Blocks(Scratch scratch, EditorHarness editor)
    {
        editor.ClickImport(scratch.Write("downloads/blocks.glb", Model("blocks")));
        editor.Answer("Import");
        var entry = editor.Session.Entry("models/blocks")!;
        editor.Edit(entry, "$.materials[0].baseColor", new Vector4(1, 0, 0, 1));
        editor.Edit(entry, "$.nodes[0].name", "Flag");
        editor.Edit(entry, "$.materials[1].hatch", false);
        return entry;
    }

    [Fact]
    public void TheValuesEditedInTheFormAreListedAndKeptOrTakenEach()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor(height: 720);
        var entry = Blocks(scratch, editor);
        var edited = entry.Draft!;
        var original = ((ModelData)entry.SavedDraft!.Build().Value!);
        editor.ClickImport(scratch["downloads/blocks.glb"]);
        Assert.Equal("models/blocks", editor.Dialog().PathField.Value);
        editor.Answer("Import");
        var list = editor.Import().CurrentList!;
        Assert.Equal(("Import again", "models/blocks is already a model: its bulk data comes from blocks.glb. 3 values differ between the entry and the file: keep the entry's or take the file's."),
            (editor.Title(), editor.Message()));
        Assert.Equal(["$.nodes[0].name", "$.materials[0].baseColor", "$.materials[1].hatch"], list.Rows.Select(r => r.Difference.Path.ToString()));
        Assert.Equal(["Nodes › 0 › Name", "Materials › 0 › Base color", "Materials › 1 › Hatch"],
            editor.Document.QuerySelectorAll(".import-row:not(.import-head) .import-name").Select(e => e.TextContent));
        Assert.Equal(["Flag", original.Nodes[0].Name, "#ff0000ff", ValueText.Hex(ValueText.Components(original.Materials[0].BaseColor)), "no", "yes"],
            editor.Document.QuerySelectorAll(".import-row:not(.import-head) .import-text").Select(e => e.TextContent));
        Assert.Equal(2, editor.Document.QuerySelectorAll(".import-row:nth-child(3) .import-swatch").Count());
        // Tooltips: a value's path, and its text (long ones are cut by their column).
        Assert.Equal(["$.nodes[0].name", "$.materials[0].baseColor", "$.materials[1].hatch"],
            editor.Document.QuerySelectorAll(".import-row:not(.import-head) .import-name").Select(e => e.GetAttribute("title")));
        Assert.All(editor.Document.QuerySelectorAll(".import-text"), e => Assert.Equal(e.TextContent, e.GetAttribute("title")));
        // Keep is chosen at first, the focus on Import again; the press of a row's choice takes the file's.
        Assert.All(list.Rows, r => Assert.True(r.Keep.Checked && !r.Take.Checked));
        Assert.Equal("Import again", editor.Document.FocusedElement!.TextContent);
        Assert.Equal(["Import again", "Cancel"], editor.Buttons());
        editor.Press(list.Rows[0].Take);
        Assert.True(list.Rows[0].Take.Checked && !list.Rows[0].Keep.Checked);
        // A press on the text of a choice chooses it too (the label holds its radio).
        editor.Press(editor.Document.QuerySelectorAll(".import-row:not(.import-head) .import-text").ElementAt(5));
        Assert.True(list.Rows[2].Take.Checked);
        editor.Press(list.Rows[2].Keep);
        editor.Answer("Import again");
        Assert.Null(editor.View.Dialogs.Top);
        var model = editor.Built<ModelData>();
        Assert.Equal((original.Nodes[0].Name, new Vector4(1, 0, 0, 1), false), (model.Nodes[0].Name, model.Materials[0].BaseColor, model.Materials[1].Hatch));
        Assert.Same(entry, editor.Session.Entry("models/blocks"));
        Assert.Equal("Imported blocks.glb again into models/blocks.", editor.View.Status);
        // One step of the entry's history.
        Assert.True(editor.Session.Undo(entry));
        Assert.Same(edited, entry.Draft);
        Assert.True(editor.Session.Redo(entry));

        // Take all: the file's own entry, the packer's; Keep all: the edits.
        editor.ClickImport(scratch["downloads/blocks.glb"]);
        editor.Answer("Import");
        list = editor.Import().CurrentList!;
        Assert.Equal(2, list.Rows.Count);
        editor.Press(list.TakeAll);
        Assert.All(list.Rows, r => Assert.True(r.Take.Checked));
        editor.Press(list.KeepAll);
        Assert.All(list.Rows, r => Assert.True(r.Keep.Checked));
        editor.Press(list.TakeAll);
        editor.Answer("Import again");
        Assert.Equal(scratch.Packed(scratch["downloads/blocks.glb"], "models/blocks.glb", "models/blocks"), Content(entry));
        // Cancel: nothing changes.
        editor.Edit(entry, "$.nodes[1].ink", false);
        var before = entry.Draft;
        editor.ClickImport(scratch["downloads/blocks.glb"]);
        editor.Answer("Import");
        editor.Answer("Cancel");
        Assert.Same(before, entry.Draft);
    }

    [Fact]
    public void TheBulkDataComesFromTheFileAndListsHoldingItFollowItsLength()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor(height: 720);
        var entry = Blocks(scratch, editor);
        var blocks = (ModelData)entry.Build!.Value!;
        var sword = (ModelData)RecordSerializer.FromBinary(typeof(ModelData), Statventure.Packs.PackWriter.ImportFile(TestModelPack.SourceOf("sword"), "models/sword.glb")[0].Content);
        editor.ClickImport(scratch.Write("downloads/sword.glb", Model("sword")));
        editor.ImportAt("models/blocks");
        var list = editor.Import().CurrentList!;
        // The nodes hold the hierarchy (bulk): the file's three, their names compared by index; the materials hold none
        // and their counts differ: one difference as a whole.
        var paths = list.Rows.Select(r => r.Difference.Path.ToString()).ToList();
        Assert.Equal([.. Enumerable.Range(0, sword.Nodes.Count).Where(i => blocks.Nodes[i].Name != sword.Nodes[i].Name).Select(i => $"$.nodes[{i}].name"), "$.materials"],
            paths.Where(p => p.StartsWith("$.nodes", StringComparison.Ordinal) || p == "$.materials"));
        Assert.DoesNotContain(paths, p => p.StartsWith("$.meshes", StringComparison.Ordinal) || p.StartsWith("$.roots", StringComparison.Ordinal) || p.Contains(".parent") || p.Contains(".children"));
        var materials = list.Rows.Single(r => r.Difference.Path.ToString() == "$.materials");
        Assert.True(materials.Difference.IsWhole);
        Assert.Equal(("Materials", $"{blocks.Materials.Count} elements", $"{sword.Materials.Count} elements"),
            (materials.Difference.Name, ImportPartText(materials.Difference.Present, materials.Difference), ImportPartText(materials.Difference.File, materials.Difference)));
        // Kept: the entry's names and materials over the file's hierarchy and meshes.
        editor.Answer("Import again");
        var kept = editor.Built<ModelData>();
        Assert.Equal(sword.Nodes.Count, kept.Nodes.Count);
        Assert.Equal(blocks.Nodes.Take(sword.Nodes.Count).Select(n => n.Name), kept.Nodes.Select(n => n.Name));
        Assert.Equal(sword.Nodes.Select(n => n.Children), kept.Nodes.Select(n => n.Children));
        Assert.Equal(blocks.Materials, kept.Materials);
        Assert.Equal(sword.Meshes.Select(m => (m.Name, m.Primitives.Sum(p => p.Positions.Length))), kept.Meshes.Select(m => (m.Name, m.Primitives.Sum(p => p.Positions.Length))));
        // Taken: the file's own entry.
        editor.Session.Undo(entry);
        editor.ClickImport(scratch["downloads/sword.glb"]);
        editor.ImportAt("models/blocks");
        editor.Press(editor.Import().CurrentList!.TakeAll);
        editor.Answer("Import again");
        Assert.Equal(scratch.Packed(scratch["downloads/sword.glb"], "models/blocks.glb", "models/blocks"), Content(entry));
    }

    static string ImportPartText(DraftValue value, Statventure.Editor.Import.ImportDifference difference) => Statventure.Editor.Import.ImportPart.Describe(value, difference);

    [Fact]
    public void ARecordsJsonFormExportedFromTheJsonTabComesBackInThroughImport()
    {
        using var scratch = new Scratch();
        var patch = RecordFixtures.Read("audio.effect", "every-field", ".json");
        using var editor = scratch.Editor(w => w.AddRecord("audio/effects/zap", RecordSerializer.FromJson<EffectPatch>(patch)), height: 720);
        var picker = new ScratchExportPicker();
        editor.Form().ExportPicker = picker;
        var zap = editor.Session.Entry("audio/effects/zap")!;
        editor.Session.Select(zap);
        editor.Frame();
        var export = editor.Run(() => editor.Form().ExportJson());
        var file = export.Done()!;
        Assert.Equal("zap.json", Path.GetFileName(file));
        // Into a free path it is the packer's record; back over its entry, nothing differs.
        editor.ClickImport(file);
        Assert.Equal("audio/effects/zap", editor.Dialog().PathField.Value);
        editor.ImportAt("audio/effects/zap-2");
        Assert.Equal(scratch.Packed(file, "audio/effects/zap-2.json", "audio/effects/zap-2"), Content(editor.Session.Entry("audio/effects/zap-2")!));
        editor.ClickImport(file);
        editor.Answer("Import");
        Assert.Equal("Import again", editor.Title());
        Assert.Null(editor.Import().CurrentList);
        editor.Answer("Cancel");

        // Edited outside: two numbers, a third layer and no pitch range.
        var json = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(file))!;
        json["level"] = 0.5;
        json["maxVoices"] = 4;
        File.WriteAllText(file, json.ToJsonString());
        editor.ClickImport(file);
        editor.Answer("Import");
        var list = editor.Import().CurrentList!;
        Assert.Equal(["$.level", "$.maxVoices"], list.Rows.Select(r => r.Difference.Path.ToString()));
        Assert.Equal(["0.7", "0.5", "3", "4"], editor.Document.QuerySelectorAll(".import-row:not(.import-head) .import-text").Select(e => e.TextContent));
        editor.Press(list.Rows[0].Take);
        editor.Answer("Import again");
        var built = editor.Built<EffectPatch>();
        Assert.Equal((0.5, 3), (built.Level, built.MaxVoices));

        var layers = json["layers"]!.AsArray();
        layers.Add(layers[1]!.DeepClone());
        json.AsObject().Remove("pitch");
        File.WriteAllText(file, json.ToJsonString());
        editor.ClickImport(file);
        editor.Answer("Import");
        list = editor.Import().CurrentList!;
        Assert.Equal(["$.layers", "$.maxVoices", "$.pitch"], list.Rows.Select(r => r.Difference.Path.ToString()));
        Assert.Equal(["Layers", "Max voices", "Pitch"], list.Rows.Select(r => r.Difference.Name));
        Assert.Equal(["2 elements", "3 elements", "3", "4", "Range", "none"],
            editor.Document.QuerySelectorAll(".import-row:not(.import-head) .import-text").Select(e => e.TextContent));
        editor.Press(list.TakeAll);
        editor.Answer("Import again");
        Assert.Equal(scratch.Packed(file, "audio/effects/zap.json", "audio/effects/zap"), Content(zap));
    }

    [Fact]
    public void ImportingAgainOverAGameEntryInAModMakesTheModsCopy()
    {
        using var scratch = new Scratch();
        using var editor = scratch.Editor(height: 720);
        var room = editor.Session.Entries.First(e => e.Source == EntrySource.Game && e.Path.StartsWith("audio/rooms/", StringComparison.Ordinal));
        var spec = (Statventure.Audio.RoomSpec)room.Saved!;
        var file = scratch.Write($"downloads/{room.Name}.json", RecordSerializer.ToJson(spec with { Seconds = 4.5, Wet = spec.Wet / 2 }));
        editor.ClickImport(file);
        Assert.Equal(room.Path, editor.Dialog().PathField.Value);
        editor.Answer("Import");
        var list = editor.Import().CurrentList!;
        Assert.Equal(["$.seconds", "$.wet"], list.Rows.Select(r => r.Difference.Path.ToString()));
        // Numbers as the form writes them, with their unit.
        Assert.Equal([ValueText.Number(spec.Seconds) + " s", "4.5 s", ValueText.Number(spec.Wet), ValueText.Number(spec.Wet / 2)],
            editor.Document.QuerySelectorAll(".import-row:not(.import-head) .import-text").Select(e => e.TextContent));
        editor.Press(list.Rows[1].Take);
        editor.Answer("Import again");
        Assert.Same(room, editor.Session.Entry(room.Path));
        Assert.True(editor.Session.IsModCopy(room));
        Assert.Equal(spec with { Wet = spec.Wet / 2 }, room.Build!.Value);
        Assert.Equal(1, room.HistoryPosition);
    }
}
