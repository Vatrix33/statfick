using System.Text;
using Statventure.Audio;
using Statventure.Core;
using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Editor.Form;
using static Statventure.Tests.Editor.EndToEnd.EndToEndTesting;
using static Statventure.Tests.Editor.Import.ImportTesting;
using static Statventure.Tests.Editor.Opening.OpeningTesting;
using static Statventure.Tests.Editor.Saving.SavingTesting;

namespace Statventure.Tests.Editor.EndToEnd;

/// <summary>
/// A mod made from scratch through the editor's UI (M11 step 14; Docs/Editor.md, "Steps", 14): New mod and its info in
/// its own form; a game entry changed in the mod (its own copy, P7); a sound effect made with New (Ctrl+N) and filled in
/// the form; a particle effect (New entry's button); a stage imported from a test .glb through Import; a cutscene made
/// with New that uses the three, its references picked from the reference lists; Save refused while it has a problem,
/// the problem fixed until Save accepts; then the saved mod mounted as the game mounts mods over its packs
/// (<see cref="Application.DataMounts"/>) and loaded by the engine's own loaders (<see cref="Sequence.Load"/>,
/// <see cref="ParticleEffect.Load"/>, <see cref="MusicLibrary.Load"/>), each loaded value the one the editor showed;
/// and the saved mod opened again in the editor, equal.
/// </summary>
public class ModFromScratchTests
{
    public const string ModFile = "ember-rite.pak", Chime = "audio/effects/chime", Sparks = "models/effects/sparks", Stage = "models/stages/dolly",
        Rite = "world/cutscenes/rite", Hall = "audio/rooms/hall", Readme = "readme.txt";

    /// <summary>The mod's readme, a text file imported into it.</summary>
    const string ReadmeText = "Ember Rite: a chime, then sparks on the dolly stage.\n";

    /// <summary>The mod's info as the flow fills it in.</summary>
    static readonly ModInfo Info = new() { Name = "Ember Rite", Author = "Vatrix", Version = "1.0", Description = "A rite on the dolly stage: a chime, then sparks." };

    [Fact]
    public void AModMadeFromScratchThroughTheUiLoadsInTheGameAsTheEditorShowedIt()
    {
        using var scratch = new Folders();
        Dictionary<string, (Type Type, string Binary, string Json)> shown;
        using (var editor = Modder(scratch.Player))
            shown = MakeMod(editor, scratch);

        // The game: its packs beside its program, the player's mods, mounted as it mounts them, read by its own loaders.
        var game = scratch.Game();
        using (var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), game, _ => null, scratch.Mods)))
        {
            void Same(string path, Type type, object loaded)
            {
                Assert.Equal(shown[path].Binary, Binary(type, loaded));
                if (shown[path].Json.Length > 0)
                    Assert.Equal(shown[path].Json, RecordSerializer.ToJson(type, loaded));
            }
            Same(Rite, typeof(Sequence), Sequence.Load(data, Rite));
            Same(Sparks, typeof(ParticleEffect), ParticleEffect.Load(data, Sparks));
            var music = MusicLibrary.Load(data);
            Same(Chime, typeof(EffectPatch), music.Effects["chime"]);
            // The mod's hall over the game's: the mod mounted above the game's packs.
            Same(Hall, typeof(RoomSpec), music.Rooms["hall"]);
            Same(Hall, typeof(RoomSpec), data.Load<RoomSpec>(Hall));
            Same(Stage, typeof(ModelData), ModelData.Load(data, Stage));
            Same(ModInfo.Path, typeof(ModInfo), data.Load<ModInfo>(ModInfo.Path));
            Assert.Equal(Info, data.Load<ModInfo>(ModInfo.Path));
            // The readme imported as a file, its bytes as they were.
            Assert.Equal(Encoding.UTF8.GetBytes(ReadmeText), data.ReadAllBytes(Readme));
            // The game's own music is all there, the mod's effect beside the game's.
            using var gamePacks = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), game, _ => null, scratch["no-mods"]));
            var gameMusic = MusicLibrary.Load(gamePacks);
            Assert.Equal(gameMusic.Themes, music.Themes);
            Assert.Equal(gameMusic.Rooms["hall"] with { Seconds = 4.5 }, music.Rooms["hall"]);
            Assert.Equal(gameMusic.Effects.Keys.Append("chime").Order(StringComparer.Ordinal), music.Effects.Keys.Order(StringComparer.Ordinal));
        }

        // The saved mod opened again in the editor (Open, the file browser): the same entries, as they were shown.
        using (var editor = Modder(scratch.Player))
        {
            editor.OpenFile(Path.Combine(scratch.Mods, ModFile));
            Assert.Equal("Opened ember-rite.pak: 7 entries", editor.View.Status);
            Assert.Equal([Chime, Hall, ModInfo.Path, Sparks, Stage, Readme, Rite], editor.Session.Entries.Where(e => e.Source == EntrySource.Pack).Select(e => e.Path));
            var readme = editor.Pick(Readme);
            Assert.Equal((EntryContent.File, (long)Encoding.UTF8.GetByteCount(ReadmeText)), (readme.Content, readme.FileSize));
            foreach (var (path, (type, binary, json)) in shown)
            {
                var entry = editor.Pick(path);
                Assert.Equal(binary, Binary(type, entry.Saved!));
                Assert.Empty(entry.Problems);
                Assert.False(entry.IsChanged);
                if (json.Length > 0)
                    Assert.Equal(json, editor.ShownJson());
            }
            Assert.False(editor.Session.HasUnsavedWork);
        }
    }

    /// <summary>
    /// The mod made through <paramref name="editor"/>'s UI and saved into the player's mods folder: what the editor showed
    /// of each entry once saved (its kind's type, its value's binary form and its JSON tab's text; none for the mod's info,
    /// which has a form of its own), by path.
    /// </summary>
    internal static Dictionary<string, (Type Type, string Binary, string Json)> MakeMod(EditorHarness editor, Folders scratch)
    {
        var download = scratch.Download("dolly", "Dolly Stage.glb");
        var shown = new Dictionary<string, (Type Type, string Binary, string Json)>(StringComparer.Ordinal);
        // New mod: its name, the file name suggested, Enter.
        editor.Click(EditorCommand.NewMod);
        editor.TypeKeys("Ember Rite");
        Assert.Equal(ModFile, editor.Saving().NewModShown!.FileField.Value);
        editor.Key("Enter", "Enter");
        Assert.Equal(Path.Combine(scratch.Mods, ModFile), editor.Session.Pack!.File);
        Assert.Equal("Made ember-rite.pak in the mods folder.", editor.View.Status);
        Assert.Same(editor.Session.Entry(ModInfo.Path), editor.Session.Selected);

        // The mod's info in its own form: the author and version (Enter commits), the description (leaving it commits).
        editor.Enter(editor.ModInfoField("Author"), "Vatrix");
        editor.Enter(editor.ModInfoField("Version"), "1.0");
        editor.Type(editor.ModInfoField("Description"), "A rite on the dolly stage: a chime, then sparks.");
        editor.Press(editor.ModInfoField("Author"));
        Assert.Equal(Info, editor.Session.Entry(ModInfo.Path)!.Built());

        // A game entry changed in the mod: the game's hall found in the list, its echo made longer (the mod's own copy, P7).
        var hall = editor.Pick(Hall);
        Assert.Equal(EntrySource.Game, hall.Source);
        editor.Set("$.seconds", "4.5");
        Assert.True(editor.Session.IsModCopy(hall));
        Assert.Equal(((RoomSpec)hall.Saved!) with { Seconds = 4.5 }, hall.Built());

        // A sound effect: New (Ctrl+N, from the mod info's field), then its one layer filled in the form.
        editor.Pick(ModInfo.Path);
        editor.Press(editor.ModInfoField("Version"));
        var effect = editor.NewEntry(NewBy.Key, "Sound effect (audio.effect)", "chime");
        Assert.Equal(Chime, effect.Path);
        Assert.Equal(["needs a value"], editor.Row("$.layers").Problems());
        editor.Press("$.layers", "Add");
        editor.Pick("$.layers[0].source", "Sine");
        editor.Set("$.layers[0].frequency", "660");
        editor.Set("$.layers[0].length", "0.4");
        editor.Set("$.level", "0.8");
        Assert.True(effect.Builds, string.Join("; ", effect.Problems));
        Assert.Empty(effect.Problems);
        var patch = (EffectPatch)effect.Built();
        Assert.Equal(Binary(typeof(EffectPatch), new EffectPatch { Layers = [new() { Source = EffectSource.Sine, Frequency = 660, Length = 0.4 }], Level = 0.8 }),
            Binary(typeof(EffectPatch), patch));

        // A particle effect: its shape and spawn chosen, its colours named, its ranges made and set.
        var particles = editor.NewEntry(NewBy.Button, "Particle effect (particle.effect)", "sparks");
        Assert.Equal(Sparks, particles.Path);
        editor.Pick("$.shape", "Disc");
        editor.Set("$.startColor", "spark");
        editor.Set("$.endColor", "ash");
        editor.Press("$.life", "Make");
        editor.Set("$.life.min", "0.6");
        editor.Set("$.life.max", "1");
        editor.Press("$.size", "Make");
        editor.Set("$.size.min", "0.15");
        editor.Set("$.size.max", "0.15");
        editor.Press("$.speed", "Make");
        editor.Set("$.speed.min", "1");
        editor.Set("$.speed.max", "2");
        editor.Pick("$.spawn", "Point");
        editor.Set("$.fadeOut", "0.3");
        var gravity = editor.Row("$.gravity").Own().Where(e => e.HasClass("component")).ToList();
        editor.Enter(gravity[1], "-2");
        Assert.True(particles.Builds, string.Join("; ", particles.Problems));
        Assert.Empty(particles.Problems);
        Assert.Equal(new ParticleEffect
        {
            Shape = new ParticleShape.Disc(), StartColor = "spark", EndColor = "ash", Life = new(0.6f, 1), Size = 0.15f, Speed = new(1, 2),
            Spawn = new ParticleSpawn.Point(), FadeOut = 0.3f, Gravity = new System.Numerics.Vector3(0, -2, 0),
        }, particles.Built());

        // A stage: the test .glb imported through the Import button and the file browser, at the stages' folder.
        editor.ImportFile(download, "models/dolly-stage", Stage);
        var stage = editor.Session.Entry(Stage)!;
        Assert.Same(stage, editor.Session.Selected);
        Assert.Equal("Imported Dolly Stage.glb as models/stages/dolly.", editor.View.Status);
        using (var packer = new Import.ImportTesting.Scratch())
            Assert.Equal(packer.Packed(download, "models/stages/dolly.glb", Stage), Content(stage));
        // The mod's readme: a text file imported at the path suggested (texts at the top of the data).
        editor.ImportFile(scratch.Write("downloads/readme.txt", ReadmeText), Readme, Readme);
        Assert.Equal("Imported readme.txt as readme.txt.", editor.View.Status);
        Assert.Equal(EntryContent.File, editor.Session.Entry(Readme)!.Content);

        // The cutscene: New, then its name and length, its stage and four tracks, the names picked from the lists.
        var rite = editor.NewEntry(NewBy.Button, "Cutscene (scene.sequence)", "rite");
        Assert.Equal(Rite, rite.Path);
        editor.Set("$.name", "rite");
        editor.Set("$.duration", "3");
        editor.Press("$.stages", "Add");
        editor.Refer("$.stages[0]", "", Stage);
        // A clip track (the first kind a track starts as): the stage's timeline.
        editor.Press("$.tracks", "Add");
        Assert.Equal("Clip track", editor.Row("$.tracks[0]").Control("select").SelectedOption!.TextContent);
        editor.Set("$.tracks[0].stage", "dolly");
        editor.Press("$.tracks[0].strip", "Make");
        editor.Set("$.tracks[0].strip.clip", "Scene");
        // A camera track: one cut, to a node typed wrong (the camera's name, not its node's).
        editor.Press("$.tracks", "Add");
        editor.Pick("$.tracks[1]", "Camera track");
        editor.Press("$.tracks[1].cuts", "Add");
        editor.Set("$.tracks[1].cuts[0].time", "0");
        editor.Set("$.tracks[1].cuts[0].camera", "dolly/Lens");
        // A particle track: the sparks on the crate.
        editor.Press("$.tracks", "Add");
        editor.Pick("$.tracks[2]", "Particle track");
        editor.Press("$.tracks[2].cues", "Add");
        editor.Set("$.tracks[2].cues[0].time", "0.5");
        editor.Refer("$.tracks[2].cues[0].effect", "spa", Sparks);
        editor.Set("$.tracks[2].cues[0].node", "dolly/Crate");
        editor.Set("$.tracks[2].cues[0].burst", "12");
        // An audio track: the chime.
        editor.Press("$.tracks", "Add");
        editor.Pick("$.tracks[3]", "Audio track");
        editor.Press("$.tracks[3].cues", "Add");
        editor.Set("$.tracks[3].cues[0].time", "0.25");
        editor.Pick("$.tracks[3].cues[0].kind", "Effect");
        editor.Tick("$.tracks[3].cues[0].name");
        editor.Refer("$.tracks[3].cues[0].name", "", "chime");
        Assert.True(rite.Builds, string.Join("; ", rite.Problems));

        // Save: refused while the camera cut names a node the stage hasn't; the problem fixed, Save accepts.
        editor.Key("s", "KeyS", KeyModifiers.Control);
        Assert.Equal("Can't save", editor.Asked().Title);
        Assert.Equal([$"{Rite}: " + rite.Problems[0].ToString()], editor.Listed());
        editor.Answer("OK");
        Assert.Equal("$.tracks[1].cuts[0].camera", rite.Problems[0].Path.ToString());
        Assert.Equal("the sequence rite, track 1 (camera): cue 0's camera dolly/Lens: the stage models/stages/dolly has no node Lens", rite.Problems[0].Message);
        Assert.Single(rite.Problems);
        Assert.Equal([rite.Problems[0].Message], editor.Row("$.tracks[1].cuts[0].camera").Problems());
        editor.Set("$.tracks[1].cuts[0].camera", "dolly/Camera");
        editor.Key("s", "KeyS", KeyModifiers.Control);
        Assert.Null(editor.View.Dialogs.Top);
        Assert.Equal("Saved ember-rite.pak.", editor.View.Status);
        Assert.False(editor.Session.HasUnsavedWork);
        Assert.Equal("Statventure Editor — ember-rite.pak", editor.View.Title);

        // What the editor shows of each entry: its value and its JSON tab.
        foreach (var path in new[] { ModInfo.Path, Hall, Chime, Sparks, Stage, Rite })
        {
            var entry = editor.Pick(path);
            Assert.Empty(entry.Problems);
            shown[path] = (entry.Kind!.Type, Binary(entry.Kind.Type, entry.Built()), path == ModInfo.Path ? "" : editor.ShownJson());
        }
        return shown;
    }
}
