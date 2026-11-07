using System.Numerics;
using Statventure.Packs;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using static Statventure.Tests.Editor.Import.ImportTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Import;

/// <summary>
/// Step 12's screens against Chromium (P36; Docs/Editor.md, "Import (step 12)"; the manager's decision of 2026-11-07,
/// Q4): the import dialog with the path suggested for a model, the replace question for a file over the mod's own,
/// and the keep-or-take list of a model imported again over its form edits (a name, a colour with its swatches, a
/// flag). A mod over the repo's packs (the harness's dev setup); the mod and the files imported are written into the
/// run's scratch folder, and only the files' names are in the markup. The focus is on each dialog's first button (a
/// focused text field's caret doesn't carry over to the capture), moved there by the code, not by the pointer.
/// </summary>
public sealed class ImportScreens : IEditorFixtures
{
    public IEnumerable<EditorFixture> Fixtures =>
    [
        new("import-path", 1000, 640, Path),
        new("import-replace", 1000, 640, Replace),
        new("import-again", 1000, 640, Again),
    ];

    /// <summary>The fixtures' scratch folder (written again each time: the fixtures are built at both scales).</summary>
    static string Folder(string name)
    {
        var folder = System.IO.Path.Combine(PerUserFolders.Root, "editor-fixtures", "import", name);
        Directory.CreateDirectory(folder);
        return folder;
    }

    static string WriteFile(string folder, string name, byte[] content)
    {
        var file = System.IO.Path.Combine(Folder(folder), name);
        File.WriteAllBytes(file, content);
        return file;
    }

    static void OpenMod(EditorHarness editor, string folder, Action<PackWriter> fill)
    {
        var writer = new PackWriter();
        fill(writer);
        var file = System.IO.Path.Combine(Folder(folder), "my-mod.pak");
        writer.Write(file);
        editor.Session.Open(file);
        editor.Frame();
    }

    /// <summary>The first button of the open dialog focused (its look in the capture: the focus ring).</summary>
    static void FocusFirstButton(EditorHarness editor)
    {
        editor.View.Dialogs.Top!.Buttons.ElementChildren.First().Focus();
        editor.Frame();
    }

    static void Path(EditorHarness editor)
    {
        OpenMod(editor, "path", w => w.Add("notes/readme.txt", "A mod."u8));
        var file = WriteFile("path", "Arena Stage.GLB", File.ReadAllBytes(TestModelPack.SourceOf("blocks")));
        _ = editor.Run(() => editor.Import().ImportFiles([file]));
        editor.Frame();
        FocusFirstButton(editor);
    }

    static void Replace(EditorHarness editor)
    {
        OpenMod(editor, "replace", w => w.Add("notes/readme.txt", "A mod."u8));
        var file = WriteFile("replace", "readme.txt", "The new notes."u8.ToArray());
        _ = editor.Run(() => editor.Import().ImportFiles([file]));
        editor.Dialog().PathField.Value = "notes/readme.txt";
        editor.Dialog().Act();
        editor.Frame();
        FocusFirstButton(editor);
    }

    static void Again(EditorHarness editor)
    {
        var source = TestModelPack.SourceOf("blocks");
        var model = PackWriter.ImportFile(source, "models/blocks.glb")[0];
        OpenMod(editor, "again", w => w.AddRecordBinary(model.Path, model.Content));
        var entry = editor.Session.Entry("models/blocks")!;
        editor.Edit(entry, "$.nodes[0].name", "Flag");
        editor.Edit(entry, "$.materials[0].baseColor", new Vector4(0.8431373f, 0.47843137f, 0.3137255f, 1));
        editor.Edit(entry, "$.materials[1].hatch", false);
        editor.Frame();
        var file = WriteFile("again", "blocks.glb", File.ReadAllBytes(source));
        _ = editor.Run(() => editor.Import().ImportFiles([file]));
        // Import chosen from the code, not by a click: a click before the list's own focus would make it not :focus-visible
        // (Blink's rule for focus moved by a script after a pointer), which the capture can't show.
        editor.Dialog().Act();
        editor.Frame();
    }
}
