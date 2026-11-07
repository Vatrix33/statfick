using System.Numerics;
using Statventure.Audio;
using Statventure.Editor.Preview;
using Statventure.Packs;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;

namespace Statventure.Tests.Editor.Preview;

/// <summary>
/// Step 13's screens against Chromium (P36; M11-20: the preview's content, its 3D view, left out: the fixture writer puts
/// an empty box of the surface's size in its place): a sound effect's preview (Play, Stop, the editor's volume, the play's
/// volume, pan and pitch), a model's (the camera, clip, loop and time controls) and a cutscene's with a stand-in for every
/// kind of role and a track left out. Over the repo's packs (the harness's dev setup), the mods and the player's folder in
/// the run's scratch folder; no GPU and no sound connected (what they would change is the 3D view, left out).
/// </summary>
public sealed class PreviewScreens : IEditorFixtures
{
    public IEnumerable<EditorFixture> Fixtures =>
    [
        new("preview-effect", 1000, 640, Effect),
        new("preview-model", 1000, 640, ModelScreen),
        new("preview-cutscene", 1000, 800, Cutscene),
    ];

    /// <summary>A folder of the fixture's own in the run's scratch folder, emptied (the fixtures are built at both scales).</summary>
    static string Folder(string name)
    {
        var folder = Path.Combine(PerUserFolders.Root, "editor-fixtures", name);
        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>The fixture's mod opened, its player's folder the fixture's own.</summary>
    static void Open(EditorHarness editor, string name, Action<PackWriter> fill, Action<string>? player = null)
    {
        var folder = Folder(name);
        editor.Session.PlayerFolder = Path.Combine(folder, "player");
        player?.Invoke(editor.Session.PlayerFolder);
        var writer = new PackWriter();
        fill(writer);
        var file = Path.Combine(folder, "player", "mods", name + ".pak");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        writer.Write(file);
        editor.Session.Open(file);
    }

    static ModelData Glb(string name) => ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, name + ".glb"));

    static void Effect(EditorHarness editor)
    {
        Open(editor, "preview-effect", w => w.AddRecord("audio/effects/chime",
            new EffectPatch { Layers = [new() { Source = EffectSource.Sine, Frequency = 660, Length = 0.15 }], Level = 0.8 }));
        editor.Session.Select(editor.Session.Entry("audio/effects/chime"));
        editor.Frame();
    }

    static void ModelScreen(EditorHarness editor)
    {
        Open(editor, "preview-model", w => w.AddRecord("models/stages/dolly", Glb("dolly")));
        editor.Session.Select(editor.Session.Entry("models/stages/dolly"));
        editor.Frame();
    }

    /// <summary>A small cutscene with a role of every kind on the dolly stage (the model role's palette track left out: no model chosen).</summary>
    public static Sequence Demo => new()
    {
        Name = "demo",
        Duration = 4,
        Roles =
        [
            new("hero", RoleKind.Actor), new("crowd", RoleKind.Model), new("glow", RoleKind.Palette), new("card", RoleKind.Element), new("spot", RoleKind.Node),
        ],
        Stages = ["models/stages/dolly"],
        Tracks =
        [
            new ClipTrack { Stage = "dolly", Strip = new SequenceStrip("Scene") },
            new CameraTrack { Cuts = [new CameraCut(0, "dolly/Camera")] },
            new ActorTrack { Actor = "hero", Strips = [new SequenceStrip("idle", Repeat: double.PositiveInfinity)] },
            new PaletteTrack { Target = "crowd", Cues = [new PaletteCue(1, Palette: "glow")] },
            new ParticleTrack { Cues = [new ParticleCue(0, "models/effects/sparks", "spot", Burst: 10, Palette: "glow")] },
            new UiTrack { Host = "card", Cues = [new UiCue(0, UiCueAction.SetText, "title", Value: "The dolly")] },
        ],
    };

    public static readonly ParticleEffect Sparks = new()
    {
        Shape = new ParticleShape.Disc(), StartColor = "spark", EndColor = "ash", Life = new(0.6f, 1), Size = 0.15f, Speed = new(1, 2),
        Spawn = new ParticleSpawn.Point(), FadeOut = 0.3f, Gravity = new Vector3(0, -2, 0),
    };

    /// <summary>The demo cutscene's data as a mod holds it.</summary>
    public static void DemoMod(PackWriter writer)
    {
        writer.AddRecord("world/cutscenes/demo", Demo);
        writer.AddRecord("models/stages/dolly", Glb("dolly"));
        writer.AddRecord("models/figure", Glb("figure"));
        writer.AddRecord("models/effects/sparks", Sparks);
    }

    static void Cutscene(EditorHarness editor)
    {
        Open(editor, "preview-cutscene", DemoMod, player => StandInsFile.Write(player, new Dictionary<string, Dictionary<string, StandIn>>
        {
            ["world/cutscenes/demo"] = new() { ["hero"] = new StandIn("models/figure"), ["glow"] = new StandIn(Colours: new Dictionary<string, string> { ["ash"] = "#8a64c2" }) },
        }));
        editor.Session.Select(editor.Session.Entry("world/cutscenes/demo"));
        editor.Frame();
        // The form's roles and tracks folded: their selects are wider in the engine than in Chromium (step 9's known
        // difference, the form's own), and this screen is the preview's.
        foreach (var path in new[] { "$.roles", "$.tracks" })
            editor.Document.QuerySelector($".form-part details[data-path='{path}']")!.RemoveAttribute("open");
        editor.Frame();
    }
}
