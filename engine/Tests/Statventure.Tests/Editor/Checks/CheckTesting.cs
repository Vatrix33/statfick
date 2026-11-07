using Statventure.Audio;
using Statventure.Editor.Checks;
using Statventure.Editor.References;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Scene;

namespace Statventure.Tests.Editor.Checks;

/// <summary>
/// The editor's checks and references driven as the user drives them (Docs/Editor.md, "How it works", step 10): a mod
/// written into the run's scratch folder and opened over the repo's packs (the harness's dev setup), its entries
/// changed through the session or the form, and the pause of the checks across entries passed on the document's clock.
/// </summary>
static class CheckTesting
{
    /// <summary>A mod in the run's scratch folder holding what <paramref name="fill"/> writes (a fresh folder each time).</summary>
    public static string Mod(Action<PackWriter> fill, string name = "my-mod.pak")
    {
        var writer = new PackWriter();
        fill(writer);
        var folder = Path.Combine(PerUserFolders.Root, "editor-checks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, name);
        writer.Write(file);
        return file;
    }

    /// <summary>An editor with a mod holding what <paramref name="fill"/> writes opened over the repo's packs.</summary>
    public static EditorHarness OpenMod(Action<PackWriter> fill, float width = 1280, float height = 900)
    {
        var editor = new EditorHarness(width, height);
        editor.Session.Open(Mod(fill));
        editor.Frame();
        return editor;
    }

    /// <summary>A session (no view) with a mod holding what <paramref name="fill"/> writes opened over the repo's packs, its checks started.</summary>
    public static EditorSession Session(Action<PackWriter> fill)
    {
        var session = new EditorSession(GameData.Repo(EditorHarness.EngineFolder));
        _ = session.Checks;
        session.Open(Mod(fill));
        return session;
    }

    /// <summary>The entry at <paramref name="path"/> (it must be shown).</summary>
    public static EditorEntry At(this EditorSession session, string path) =>
        session.Entry(path) ?? throw new InvalidOperationException($"no entry at {path}");

    /// <summary>The problems an entry shows, as text ("message (at path)").</summary>
    public static string[] Shown(this EditorEntry entry) => [.. entry.Problems.Select(p => p.ToString())];

    /// <summary>A change of one field of the entry's draft, committed.</summary>
    public static void Change(this EditorSession session, EditorEntry entry, string field, object? value) =>
        Assert.True(session.Commit(entry, entry.Draft!.With(field, value is null ? DraftValue.Null : DraftValue.Of(value))));

    /// <summary>A change at a path of the entry's draft, committed.</summary>
    public static void Change(this EditorSession session, EditorEntry entry, RecordPath path, object value) =>
        Assert.True(session.Commit(entry, entry.Draft!.With(path, DraftValue.Of(value))));

    /// <summary>Frames until <paramref name="milliseconds"/> have passed on the document's clock.</summary>
    public static void Wait(this EditorHarness editor, double milliseconds)
    {
        var until = editor.Document.EventLoop.Time + milliseconds / 1000;
        while (editor.Document.EventLoop.Time < until)
            editor.Frame();
    }

    /// <summary>Frames past the pause of the checks across entries (P17).</summary>
    public static void Pause(this EditorHarness editor) => editor.Wait(EntryChecks.Pause + 20);

    public static ReferenceListPart References(this EditorHarness editor) => editor.View.Part<ReferenceListPart>();

    public static ChecksPart ChecksPart(this EditorHarness editor) => editor.View.Part<ChecksPart>();

    /// <summary>The repo's audio.pak's record at <paramref name="path"/>.</summary>
    public static T Game<T>(string path) => (T)Form.FormTesting.AudioPack()[path];

    /// <summary>A valid sound effect patch (one of the proof's).</summary>
    public static EffectPatch Patch() => JiruanIntro.SoundPatches()[JiruanIntro.FlashSound];

    /// <summary>A cutscene with no stage whose one track plays these audio cues.</summary>
    public static Sequence Cutscene(params AudioCue[] cues) => new()
    {
        Name = "intro",
        Duration = 2,
        Roles = [],
        Stages = [],
        Tracks = [new AudioTrack { Cues = cues }],
    };

    /// <summary>A sound effect cue at the start.</summary>
    public static AudioCue Sound(string name) => new(0, AudioCueKind.Effect, name);
}
