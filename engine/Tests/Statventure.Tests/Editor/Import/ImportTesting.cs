using Statventure.Editor.Browser;
using Statventure.Editor.Import;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Packs;
using static Statventure.Tests.Editor.Opening.OpeningTesting;

namespace Statventure.Tests.Editor.Import;

/// <summary>
/// Importing driven as the user drives it (Docs/Editor.md, "Import (step 12)"): a scratch folder holding the files to
/// import (<c>downloads/</c>, copies of the tests' data), the player's folder (<c>player/</c>, its mods) and folders the
/// packer packs to compare with (<c>src-…/</c>); a mod opened over the repo's packs (the harness's dev setup), and the
/// Import button, the file browser and drops worked through UI events.
/// </summary>
static class ImportTesting
{
    public sealed class Scratch : IDisposable
    {
        public readonly TempFolder Folder = new();
        int _packed;

        public string this[string relative] => Folder[relative];

        public string Write(string relative, byte[] content) => Folder.Write(relative, content);

        public string Write(string relative, string text) => Folder.Write(relative, text);

        /// <summary>A mod in the player's mods folder, filled by <paramref name="fill"/> (a text file when nothing is given: a pack holds at least one entry).</summary>
        public string Mod(string name = "my-mod.pak", Action<PackWriter>? fill = null)
        {
            var writer = new PackWriter();
            if (fill is null)
                writer.Add("notes/readme.txt", "A mod."u8);
            else
                fill(writer);
            var file = Folder[$"player/mods/{name}"];
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            writer.Write(file);
            return file;
        }

        /// <summary>The editor over the repo's packs (the dev setup), its player folder the scratch one, the mod made by <paramref name="fill"/> open.</summary>
        public EditorHarness Editor(Action<PackWriter>? fill = null, float width = 1000, float height = 640)
        {
            var editor = new EditorHarness(width, height);
            editor.Session.PlayerFolder = Folder["player"];
            editor.Session.Open(Mod(fill: fill));
            editor.Frame();
            return editor;
        }

        /// <summary>
        /// The entry the packer makes of <paramref name="file"/> packed at <paramref name="sourcePath"/> in a folder with
        /// <paramref name="beside"/> (data paths and contents), at <paramref name="entry"/>: whether a record, and its content.
        /// </summary>
        public (bool Record, string Content) Packed(string file, string sourcePath, string entry, params (string Path, byte[] Content)[] beside)
        {
            var folder = $"src-{++_packed}";
            Folder.Write($"{folder}/{sourcePath}", File.ReadAllBytes(file));
            foreach (var (path, content) in beside)
                Folder.Write($"{folder}/{path}", content);
            var writer = new PackWriter();
            writer.AddFolder(Folder[folder]);
            var pak = Folder[$"{folder}.pak"];
            writer.Write(pak);
            using var pack = PackFile.Open(pak);
            var found = pack.Find(entry) ?? throw new InvalidOperationException($"the packer made no {entry}: {string.Join(", ", pack.Entries.Select(e => e.Path))}");
            return (found.IsRecord, Convert.ToHexStringLower(pack.Read(found)));
        }

        public void Dispose() => Folder.Dispose();
    }

    public static ImportPart Import(this EditorHarness editor) => editor.View.Part<ImportPart>();

    /// <summary>What saving gives for an entry: a record's binary form (its draft built, in the current versions) or an imported file's bytes.</summary>
    public static (bool Record, string Content) Content(EditorEntry entry)
    {
        if (entry.IsRecord)
        {
            Assert.True(entry.Builds, string.Join("; ", entry.Problems));
            return (true, Convert.ToHexStringLower(RecordSerializer.ToBinary(entry.Kind!.Type, entry.Build!.Value!)));
        }
        return (false, Convert.ToHexStringLower(entry.ImportedFile!.Value.ToArray()));
    }

    /// <summary>The Import button clicked and <paramref name="file"/> picked in the file browser (its path typed, Enter): the import dialog opens.</summary>
    public static void ClickImport(this EditorHarness editor, string file)
    {
        editor.Click(editor.View.Button(EditorCommand.Import)!);
        var browser = editor.View.Part<FileBrowserPart>().Current ?? throw new InvalidOperationException("the browser didn't open");
        browser.PathField.Value = file;
        browser.PathField.Focus();
        editor.Key("Enter", "Enter");
        editor.Frame();
    }

    /// <summary>Files dropped on the window (step 8's drop, P27): the import dialogs follow.</summary>
    public static void Drop(this EditorHarness editor, params string[] files)
    {
        editor.Document.RunScript(() => _ = editor.View.Part<Opener>().DropAsync(files));
        editor.Frame();
        editor.Frame();
    }

    /// <summary>The import dialog open now (it must be).</summary>
    public static ImportPart.ImportDialog Dialog(this EditorHarness editor) =>
        editor.Import().Current ?? throw new InvalidOperationException($"no import dialog is open ({editor.View.Dialogs.Top?.Element.TextContent ?? "no dialog"})");

    /// <summary>The import dialog's path set (typed over) and Import pressed.</summary>
    public static void ImportAt(this EditorHarness editor, string path)
    {
        var dialog = editor.Dialog();
        dialog.PathField.Value = path;
        editor.Answer("Import");
    }

    /// <summary>The open dialog's message: its paragraphs' text.</summary>
    public static string Message(this EditorHarness editor) =>
        string.Join(" ", (editor.View.Dialogs.Top ?? throw new InvalidOperationException("no dialog is open")).Content.Descendants().Where(e => e.Tag == "p").Select(p => p.TextContent));

    /// <summary>The topmost dialog's title.</summary>
    public static string Title(this EditorHarness editor) =>
        (editor.View.Dialogs.Top ?? throw new InvalidOperationException("no dialog is open")).Element.ElementChildren.First().TextContent;

    /// <summary>A record entry's draft with the value at <paramref name="path"/> set (a commit through the session, as the form's).</summary>
    public static void Edit(this EditorHarness editor, EditorEntry entry, string path, object value)
    {
        var draft = entry.Draft!.With(RecordPathOf(entry.Draft!, path), DraftValue.Of(value));
        Assert.True(editor.Session.Commit(entry, draft));
    }

    /// <summary>A path like <c>$.materials[0].name</c> as a <see cref="RecordPath"/>.</summary>
    public static RecordPath RecordPathOf(RecordDraft draft, string text)
    {
        var path = RecordPath.Root;
        var rest = text[1..];
        while (rest.Length > 0)
        {
            if (rest[0] == '.')
            {
                var end = rest.IndexOfAny(['.', '['], 1);
                end = end < 0 ? rest.Length : end;
                path = path.Field(rest[1..end]);
                rest = rest[end..];
            }
            else
            {
                var end = rest.IndexOf(']');
                var inside = rest[1..end];
                path = inside.StartsWith('"') ? path.Key(System.Text.Json.JsonSerializer.Deserialize<string>(inside)!) : path.Index(int.Parse(inside));
                rest = rest[(end + 1)..];
            }
        }
        Assert.Equal(text, path.ToString());
        return path;
    }
}
