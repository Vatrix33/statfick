using System.Numerics;
using Statventure.Audio;
using Statventure.Editor;
using Statventure.Editor.Browser;
using Statventure.Editor.Entries;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Tests.Infrastructure;
using Statventure.UI.Dom;
using static Statventure.Tests.Editor.EndToEnd.EndToEndTesting;

namespace Statventure.Tests.Editor.EndToEnd;

/// <summary>
/// The dev setup (P5, M11-5; Docs/Editor.md, "How it works", step 14): the editor started with <c>--repo</c> on a scratch
/// copy of the repo's <c>engine/Assets/Packs</c> (never the real one) opens the game's <c>audio.pak</c> writable; an
/// instrument's value changed in the form and saved (Ctrl+S) writes the pack in place: its dump shows the change at that
/// entry and nowhere else, and every other entry keeps its stored bytes.
/// </summary>
public class DevSetupTests
{
    const string Harp = "audio/instruments/harp";

    /// <summary>The editor's view over a session of its own (the harness's is the repo's packs), driven through its document's input.</summary>
    sealed class Driver : IDisposable
    {
        readonly VirtualFileSystem _ui;
        double _time;

        public Driver(EditorArguments arguments)
        {
            _ui = new VirtualFileSystem([Mount.Pack(arguments.Game!.UiPack)]);
            Session = new EditorSession(arguments.Game) { PlayerFolder = arguments.PlayerFolder! };
            View = new EditorView(Session, _ui);
            View.Document.SetViewport(1280, 900);
            View.Document.SetZoom(1);
            View.Document.Update(0);
        }

        public EditorSession Session { get; }
        public EditorView View { get; }
        UiInput Input => View.Document.Input;

        public void Frame() => View.Document.Update(_time += 1 / 60.0);

        public void Key(string key, string code, KeyModifiers modifiers = KeyModifiers.None)
        {
            Input.KeyDown(key, code, modifiers);
            Input.KeyUp(key, code, modifiers);
            Frame();
        }

        public void Type(string text)
        {
            foreach (var c in text)
                Key(c.ToString(), "", char.IsUpper(c) ? KeyModifiers.Shift : KeyModifiers.None);
        }

        public void Click(UiElement element)
        {
            element.ScrollIntoView(ScrollAlign.Center);
            Frame();
            var rect = element.GetBoundingClientRect();
            var at = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
            Input.PointerMove(at);
            Input.PointerDown(at, MouseButton.Left);
            Input.PointerUp(at, MouseButton.Left);
            Frame();
        }

        public void Dispose()
        {
            View.Dispose();
            Session.Dispose();
            _ui.Dispose();
        }
    }

    [Fact]
    public void AnInstrumentEditedInTheDevSetupIsSavedIntoTheGamesPackInPlace()
    {
        using var scratch = new Folders();
        // The scratch copy of the repo's packs, as editor.sh's --repo names the engine folder.
        var packs = scratch["engine/Assets/Packs"];
        Directory.CreateDirectory(packs);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(packs, Path.GetFileName(pack)));
        var audio = Path.Combine(packs, "audio.pak");
        var realAudio = File.ReadAllBytes(Path.Combine(TestPaths.Packs, "audio.pak"));
        var others = Directory.GetFiles(packs).Where(f => f != audio).ToDictionary(f => f, File.ReadAllBytes);
        var arguments = EditorArguments.Parse(["--repo", scratch["engine"], "--player-folder", scratch.Player], scratch["program"]);
        Assert.Null(arguments.Problem);
        Assert.Equal(new GameData(packs, DevSetup: true), arguments.Game);
        var before = Saving.SavingTesting.Stored(audio);
        string dumpBefore;
        using (var file = PackFile.Open(audio))
            dumpBefore = PackDump.ToText(file);
        double level;

        using (var editor = new Driver(arguments))
        {
            // Open (Ctrl+O): the browser's path field, the game's audio.pak, Enter. In the dev setup it takes changes.
            editor.Key("o", "KeyO", KeyModifiers.Control);
            var browser = editor.View.Part<FileBrowserPart>().Current ?? throw new InvalidOperationException("the browser didn't open");
            editor.Click(browser.PathField);
            Assert.Same(browser.PathField, editor.View.Document.FocusedElement);
            editor.Type(audio);
            editor.Key("Enter", "Enter");
            Assert.Equal($"Opened audio.pak: {before.Count} entries", editor.View.Status);
            Assert.True(editor.Session.Pack!.IsGamePack);
            Assert.True(editor.Session.CanSave);

            // The harp found with the list's filter and clicked; its level typed in the form, Enter.
            editor.Key("f", "KeyF", KeyModifiers.Control);
            editor.Type("harp");
            var harp = editor.Session.Entry(Harp)!;
            editor.Click(editor.View.Part<EntryList>().Row(harp)!);
            Assert.Same(harp, editor.Session.Selected);
            level = ((InstrumentSpec)harp.Saved!).Level;
            var field = editor.View.Document.Body.Descendants().First(e => e.GetAttribute("data-path") == "$.level")
                .ElementChildren.First().Descendants().First(e => e.Tag == "input" && e.InputType == "number");
            editor.Click(field);
            editor.Key("a", "KeyA", KeyModifiers.Control);
            editor.Type("0.375");
            editor.Key("Enter", "Enter");
            Assert.True(harp.IsChanged);
            Assert.Equal("• Statventure Editor dev — audio.pak", editor.View.Title);

            // Ctrl+S: written in place.
            editor.Key("s", "KeyS", KeyModifiers.Control);
            Assert.Null(editor.View.Dialogs.Top);
            Assert.Equal("Saved audio.pak.", editor.View.Status);
            Assert.False(editor.Session.HasUnsavedWork);
        }
        Assert.NotEqual(0.375, level);

        // Every other entry as it was stored, byte for byte; the harp's the record with the new level.
        var after = Saving.SavingTesting.Stored(audio);
        Assert.Equal(before.Keys.Order(StringComparer.Ordinal), after.Keys.Order(StringComparer.Ordinal));
        foreach (var path in before.Keys.Where(p => p != Harp))
            Assert.Equal(before[path], after[path]);
        Assert.NotEqual(before[Harp], after[Harp]);
        var harpBefore = (InstrumentSpec)Form.FormTesting.AudioPack()[Harp];
        Assert.Equal((true, Saving.SavingTesting.Binary(harpBefore with { Level = 0.375 })), Saving.SavingTesting.Contents(audio)[Harp]);
        foreach (var (file, bytes) in others)
            Assert.Equal(bytes, File.ReadAllBytes(file));

        // The dump (what textconv.sh prints for git, `packer dump`): only the harp's level and its entry's hashes changed.
        string dumpAfter;
        using (var file = PackFile.Open(audio))
            dumpAfter = PackDump.ToText(file);
        var changed = Changed(dumpBefore, dumpAfter);
        Assert.Contains(changed, line => line.After.Trim() == "\"level\": 0.375," && line.Before.Trim() == $"\"level\": {level.ToString(System.Globalization.CultureInfo.InvariantCulture)},");
        Assert.All(changed, line => Assert.Equal(Harp, line.Entry));
        Assert.Equal(["\"level\"", "\"sha256\"", "\"storedLength\""], changed.Select(l => l.After.Trim().Split(':')[0]).Distinct().Order(StringComparer.Ordinal).Where(k => k != "\"length\""));

        // The repo's own pack is untouched.
        Assert.Equal(realAudio, File.ReadAllBytes(Path.Combine(TestPaths.Packs, "audio.pak")));
    }

    /// <summary>The lines of two dumps of the same entries that differ, each with the entry it is in.</summary>
    static List<(string Entry, string Before, string After)> Changed(string before, string after)
    {
        var a = before.Split('\n');
        var b = after.Split('\n');
        Assert.Equal(a.Length, b.Length);
        var changed = new List<(string, string, string)>();
        var entry = "";
        for (var i = 0; i < a.Length; i++)
        {
            if (b[i].TrimStart().StartsWith("\"path\": \"", StringComparison.Ordinal))
                entry = b[i].Trim()["\"path\": \"".Length..].TrimEnd(',').TrimEnd('"');
            if (a[i] != b[i])
                changed.Add((entry, a[i], b[i]));
        }
        return changed;
    }
}
