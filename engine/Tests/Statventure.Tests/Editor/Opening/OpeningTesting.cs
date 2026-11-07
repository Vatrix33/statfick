using System.Numerics;
using Statventure.Audio;
using Statventure.Editor.Browser;
using Statventure.Editor.Entries;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Scene;
using Statventure.Tests.Storage;
using Statventure.UI.Dom;

namespace Statventure.Tests.Editor.Opening;

/// <summary>
/// Scratch packs and the editor's opening driven as the user drives it (Docs/Editor.md, "Opening packs (step 8)"): a
/// game of the test's own (a packs folder with an audio and a UI pack) or the repo's, mods made by the
/// <see cref="PackWriter"/>, and entries the engine can't read in each way it can't.
/// </summary>
static class OpeningTesting
{
    /// <summary>The rooms of the scratch game's audio pack.</summary>
    public static readonly RoomSpec Hall = new(1.5, 0.25, 0.5), Cave = new(3, 0.75, 0.25);

    /// <summary>The ways an entry can't be read (M11-16), each with the words its reason starts with.</summary>
    public static readonly (string How, string Reason)[] UnreadableWays =
    [
        ("damaged", "the content doesn't match its hash (damaged)"),
        ("not a record", "not a readable record: "),
        ("unknown kind", "a record of type test.part, which isn't a record type known here"),
        ("newer version", "not a readable audio.room record: audio.room version 2 is newer than this engine reads (version 1)"),
        ("refused values", "not a readable scene.curve record: "),
    ];

    /// <summary>A scratch folder holding a game's packs (<c>packs/</c>: audio.pak with two rooms, ui.pak with a text) and a player's folder (<c>player/</c>).</summary>
    public sealed class ScratchGame : IDisposable
    {
        public readonly StorageFolder Folder = new();

        public ScratchGame()
        {
            Directory.CreateDirectory(Packs);
            Directory.CreateDirectory(Player);
            Pack("packs/audio.pak", w =>
            {
                w.AddRecord("audio/rooms/hall", Hall);
                w.AddRecord("audio/rooms/cave", Cave);
            });
            Pack("packs/ui.pak", w => w.Add("ui/readme.txt", "The game's UI."u8));
        }

        public string Packs => Folder["packs"];

        public string Player => Folder["player"];

        public GameData Game(bool devSetup = false) => new(Packs, devSetup);

        /// <summary>A session on this game, its player folder the scratch one.</summary>
        public EditorSession Session(bool devSetup = false) => new(Game(devSetup)) { PlayerFolder = Player };

        /// <summary>A pack written at <paramref name="relative"/> (made by <paramref name="fill"/>).</summary>
        public string Pack(string relative, Action<PackWriter> fill)
        {
            var writer = new PackWriter();
            fill(writer);
            var file = Folder[relative];
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            writer.Write(file);
            return file;
        }

        public void Dispose() => Folder.Dispose();
    }

    /// <summary>Writes into <paramref name="writer"/> an entry at <paramref name="path"/> the engine can't read in the way <paramref name="how"/> says ("damaged" is damaged by <see cref="Damage"/> once written).</summary>
    public static void AddUnreadable(PackWriter writer, string path, string how)
    {
        switch (how)
        {
            case "damaged":
                writer.Add(path, "Stored as it is, damaged once written."u8);
                break;
            case "not a record":
                writer.AddRecordBinary(path, [0]);
                break;
            case "unknown kind":
                writer.AddRecord(path, new Records.Part { Name = "arm" });
                break;
            case "newer version":
            {
                var binary = RecordSerializer.ToBinary(new RoomSpec(1, 0, 0));
                // The type table: one type, its name's length and bytes, then its version.
                var version = 2 + "audio.room".Length;
                Assert.Equal(1, binary[version]);
                binary[version] = 2;
                writer.AddRecordBinary(path, binary);
                break;
            }
            case "refused values":
            {
                // A curve whose second key's time is made to come before the first's: FCurve refuses its keys when read.
                var binary = RecordSerializer.ToBinary(new FCurve([new FCurveKey(0, 0), new FCurveKey(2.5, 1)]));
                var time = BitConverter.GetBytes(2.5);
                var at = binary.AsSpan().IndexOf(time);
                Assert.True(at > 0);
                BitConverter.GetBytes(-2.5).CopyTo(binary, at);
                writer.AddRecordBinary(path, binary);
                break;
            }
            default:
                throw new ArgumentException(how);
        }
    }

    /// <summary>A byte of one entry's stored bytes flipped, the index left as it was (the pack opens, the entry is damaged).</summary>
    public static void Damage(string file, string path)
    {
        long offset;
        using (var pack = PackFile.Open(file))
        {
            var entry = pack.Find(path)!;
            Assert.False(entry.Compressed);
            offset = entry.Offset;
        }
        var bytes = File.ReadAllBytes(file);
        bytes[offset] ^= 0xFF;
        File.WriteAllBytes(file, bytes);
    }

    /// <summary>A pack whose header is damaged: <c>Open</c> refuses it.</summary>
    public static string Refused(StorageFolder folder, string relative)
    {
        var file = folder.Write(relative, "Not a pack at all, just text."u8.ToArray());
        return file;
    }

    public static EntryList List(this EditorHarness editor) => editor.View.Part<EntryList>();

    public static Opener Opener(this EditorHarness editor) => editor.View.Part<Opener>();

    public static FileBrowserPart Browser(this EditorHarness editor) => editor.View.Part<FileBrowserPart>();

    /// <summary>Runs <paramref name="act"/> as a task of the document's loop (as the editor's own listeners run) and gives its task.</summary>
    public static Task<T> Run<T>(this EditorHarness editor, Func<Task<T>> act)
    {
        Task<T> task = null!;
        editor.Document.RunScript(() => task = act());
        editor.Frame();
        return task;
    }

    /// <summary>Runs <paramref name="act"/> as a task of the document's loop and gives its task.</summary>
    public static Task Run(this EditorHarness editor, Func<Task> act)
    {
        Task task = null!;
        editor.Document.RunScript(() => task = act());
        editor.Frame();
        return task;
    }

    /// <summary>A task's result, once it has completed (never waited for: a broken rule may leave its dialog open).</summary>
    public static T Done<T>(this Task<T> task)
    {
        Assert.True(task.IsCompleted, "the task hasn't completed (a dialog still open?)");
        return task.Result;
    }

    /// <summary>The topmost dialog's button with this text, clicked (it must be open).</summary>
    public static void Answer(this EditorHarness editor, string button)
    {
        var dialog = editor.View.Dialogs.Top ?? throw new InvalidOperationException("no dialog is open");
        var element = dialog.Buttons.ElementChildren.FirstOrDefault(b => b.TextContent == button)
            ?? throw new InvalidOperationException($"the dialog \"{dialog.Element.ElementChildren.First().TextContent}\" has no button {button}");
        editor.Click(element);
    }

    /// <summary>The topmost dialog's title and message.</summary>
    public static (string Title, string Text) Asked(this EditorHarness editor)
    {
        var dialog = editor.View.Dialogs.Top ?? throw new InvalidOperationException("no dialog is open");
        return (dialog.Element.ElementChildren.First().TextContent, dialog.Content.TextContent);
    }

    /// <summary>The texts of the topmost dialog's buttons, in order.</summary>
    public static string[] Buttons(this EditorHarness editor) =>
        [.. (editor.View.Dialogs.Top ?? throw new InvalidOperationException("no dialog is open")).Buttons.ElementChildren.Select(b => b.TextContent)];

    /// <summary>Types <paramref name="text"/> into the focused element, key by key.</summary>
    public static void TypeKeys(this EditorHarness editor, string text)
    {
        foreach (var c in text)
            editor.Key(c.ToString(), "", char.IsUpper(c) ? Statventure.Platform.KeyModifiers.Shift : Statventure.Platform.KeyModifiers.None);
    }

    /// <summary>The value at <paramref name="name"/> of a draft as a leaf.</summary>
    public static RecordDraft Set(this RecordDraft draft, string name, object value) => draft.With(name, DraftValue.Of(value));

    /// <summary>A point inside the window, in window units (scale 1).</summary>
    public static Vector2 At(float x, float y) => new(x, y);

    /// <summary>The element's text, for messages.</summary>
    public static string Text(this UiElement element) => element.TextContent;
}
