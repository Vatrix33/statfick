using System.Text.Json.Nodes;
using Statventure.Audio;
using Statventure.Editor.Preview;
using Statventure.Editor.Settings;
using Statventure.Packs;
using Statventure.Tests.Editor.Form;
using static Statventure.Tests.Editor.Preview.PreviewEditor;

namespace Statventure.Tests.Editor.Preview;

using PreviewPart = Statventure.Editor.Preview.PreviewPart;

/// <summary>
/// The sound previews (Docs/Editor.md, "The preview (step 13)"; P31, P35), rendered offline (an audio system with no
/// device: nothing is heard): an effect plays as registering it and playing it by hand would, at the play's volume, pan and
/// pitch and the editor's volume, and the next play takes each valid edit; a theme plays from the data as edited as
/// swapping in that library and playing it by hand would, follows valid edits of itself and of the music it is made of
/// while it plays, and keeps the last library that loaded; Stop and Space; the editor's volume remembered.
/// </summary>
public class SoundPreviewTests
{
    const string ChimePath = "audio/effects/chime", Theme = "alder-vale", ThemePath = "audio/themes/alder-vale";

    static readonly EffectPatch Chime = new() { Layers = [new() { Source = EffectSource.Sine, Frequency = 660, Length = 0.15 }], Level = 0.8 };

    static EffectPatch Tone(double frequency) => Chime with { Layers = [Chime.Layers[0] with { Frequency = frequency }] };

    /// <summary>A system as the preview's starts: no music, the editor's volume (50 % when nothing is remembered).</summary>
    static AudioSystem ByHand(double volume = VolumeFile.Default)
    {
        var system = Offline();
        system.MasterVolume = volume;
        return system;
    }

    static PreviewEditor WithChime() => new(w => w.AddRecord(ChimePath, Chime));

    [Fact]
    public void AnEffectPlaysAsRegisteringAndPlayingItByHand()
    {
        using var p = WithChime();
        p.Select(ChimePath);
        Assert.IsType<EffectPreview>(p.Preview.Shown);
        // The play's own volume, pan and pitch (P31), by the sliders' keys.
        p.Key(p.Control("play-volume"), "End");
        p.Key(p.Control("pan"), "Home");
        p.Key(p.Control("pitch"), "ArrowRight", 7);
        Assert.Equal((2.0, -1.0, 7.0), (p.Preview.PlayVolume, p.Preview.PlayPan, p.Preview.PlaySemitones));
        string Shown(string name) => p.Control(name).Parent!.ElementChildren.Single(e => e.HasClass("preview-value")).TextContent;
        Assert.Equal(("× 2.00", "-1.00", "× 1.50"), (Shown("play-volume"), Shown("pan"), Shown("pitch")));
        p.Editor.Click(p.Control("play"));
        var heard = Render(p.Audio!, 40);
        var byHand = ByHand();
        byHand.Effects.Register("chime", Chime);
        byHand.Effects.Play("chime", 2, -1, Math.Pow(2, 7 / 12.0));
        Assert.Equal(Render(byHand, 40), heard);
        Assert.Contains(heard, s => s != 0);
    }

    [Fact]
    public void TheNextPlayTakesEachValidEditAndAnInvalidOneKeepsTheLast()
    {
        using var p = WithChime();
        p.Select(ChimePath);
        var heard = new List<float>();
        void Play()
        {
            p.Editor.Click(p.Control("play"));
            heard.AddRange(Render(p.Audio!, 40));
        }
        Play();
        p.Commit(ChimePath, Tone(880));
        Assert.Equal("", p.Note);
        Play();
        // Not built: the last valid patch plays, and the note says so.
        p.Break(ChimePath, "Layers");
        Assert.Equal("This version doesn't build: the preview keeps the last valid version.", p.Note);
        Play();
        // Built, but not playable (the engine's own check, which registering holds).
        p.Commit(ChimePath, Chime with { Layers = [Chime.Layers[0] with { Length = -1 }] });
        Assert.StartsWith("The sound effect isn't playable (", p.Note);
        Assert.EndsWith("): the preview keeps the last valid version.", p.Note);
        Play();
        p.Commit(ChimePath, Tone(440));
        Assert.Equal("", p.Note);
        Play();
        var byHand = ByHand();
        var expected = new List<float>();
        foreach (var patch in new[] { Chime, Tone(880), Tone(880), Tone(880), Tone(440) })
        {
            byHand.Effects.Register("chime", patch);
            byHand.Effects.Play("chime");
            expected.AddRange(Render(byHand, 40));
        }
        Assert.Equal(expected, heard);
    }

    [Fact]
    public void AnEntryWithProblemsKeepsTheLastValidPatch()
    {
        using var p = WithChime();
        // A check of the session's (step 10's kind) that finds a problem in a loud chime.
        p.Session.AddCheck((entry, value) => value is EffectPatch { Level: > 1 }
            ? [new Statventure.Records.RecordProblem(Statventure.Records.RecordPath.Root.Field("level"), "too loud for the test")] : []);
        p.Select(ChimePath);
        p.Commit(ChimePath, Chime with { Level = 2 });
        Assert.Equal("This version has problems (too loud for the test): the preview keeps the last valid version.", p.Note);
        Assert.Equal(0.8, ((EffectPreview)p.Preview.Shown!).Patch!.Level);
        p.Editor.Click(p.Control("play"));
        var byHand = ByHand();
        byHand.Effects.Register("chime", Chime);
        byHand.Effects.Play("chime");
        Assert.Equal(Render(byHand, 40), Render(p.Audio!, 40));
    }

    [Fact]
    public void ARenamedEffectPlaysUnderItsNewName()
    {
        using var p = WithChime();
        p.Select(ChimePath);
        p.Session.Rename(p.Session.Entry(ChimePath)!, "audio/effects/bell");
        p.Editor.Frame();
        Assert.Equal(RecordSerializerBinary(Chime), RecordSerializerBinary(p.Audio!.Effects.Find("bell")!.Patch));
        p.Editor.Click(p.Control("play"));
        var byHand = ByHand();
        byHand.Effects.Register("bell", Chime);
        byHand.Effects.Play("bell");
        Assert.Equal(Render(byHand, 40), Render(p.Audio, 40));
    }

    static byte[] RecordSerializerBinary(EffectPatch patch) => Statventure.Records.RecordSerializer.ToBinary(patch);

    [Fact]
    public void AnEffectThatNeverWasValidPlaysNothing()
    {
        using var p = new PreviewEditor(w => w.AddRecord(ChimePath, Chime with { Layers = [Chime.Layers[0] with { Length = 0 }] }));
        p.Select(ChimePath);
        Assert.StartsWith("The sound effect isn't playable (", p.Note);
        Assert.EndsWith("): nothing to preview yet.", p.Note);
        p.Editor.Click(p.Control("play"));
        Assert.All(Render(p.Audio!, 30), s => Assert.Equal(0, s));
    }

    [Fact]
    public void SpacePlaysTheEffectUnlessAControlTakesIt()
    {
        using var p = WithChime();
        p.Select(ChimePath);
        // In a field of the form, Space is the field's.
        var level = p.Editor.Row("$.level").Control("input", "number");
        p.Editor.Focus(level);
        p.Editor.Key(" ", "Space");
        Assert.All(Render(p.Audio!, 30), s => Assert.Equal(0, s));
        // Not with a modifier, nor while a dialog is open.
        p.Editor.Focus(p.Editor.View.Part<Statventure.Editor.Entries.EntryList>().Tree);
        p.Editor.Key(" ", "Space", Statventure.Platform.KeyModifiers.Shift);
        p.Editor.Key(" ", "Space", Statventure.Platform.KeyModifiers.Control);
        // (A dialog with nothing to focus but its box, which doesn't take Space.)
        var dialog = p.Editor.View.Dialogs.Create("A question");
        dialog.Show();
        p.Editor.Frame();
        p.Editor.Key(" ", "Space");
        dialog.Close(null);
        p.Editor.Frame();
        Assert.All(Render(p.Audio!, 30), s => Assert.Equal(0, s));
        // From the entry list (where the entry was chosen), Space plays it.
        p.Editor.Focus(p.Editor.View.Part<Statventure.Editor.Entries.EntryList>().Tree);
        Assert.True(p.Editor.Key(" ", "Space") is false, "Space played and was cancelled");
        var byHand = ByHand();
        byHand.Effects.Register("chime", Chime);
        Render(byHand, 60);
        byHand.Effects.Play("chime");
        Assert.Equal(Render(byHand, 40), Render(p.Audio!, 40));
        // Space typed ahead in the list is the list's (it looks for a name starting "c ").
        p.Editor.Key("c", "KeyC");
        Assert.False(p.Editor.Key(" ", "Space"), "the list takes (cancels) its Space");
        Assert.All(Render(p.Audio, 60), s => Assert.Equal(0, s));
    }

    [Fact]
    public void TheEditorsVolumeIsTheSoundsMasterVolumeAndIsRemembered()
    {
        using var p = WithChime();
        var player = p.Session.PlayerFolder;
        Assert.False(File.Exists(VolumeFile.File(player)));
        p.Select(ChimePath);
        Assert.Equal(0.5, p.Preview.Volume);
        Assert.Equal(0.5, p.Audio!.MasterVolume);
        Assert.Equal("50", p.Control("volume").Value);
        p.Key(p.Control("volume"), "ArrowLeft", 20);
        Assert.Equal(0.3, p.Preview.Volume, 9);
        Assert.Equal(0.3, p.Audio.MasterVolume, 9);
        Assert.Equal("30 %", p.Control("volume").Parent!.ElementChildren.Single(e => e.HasClass("preview-value")).TextContent);
        Assert.Equal(0.3, VolumeFile.Read(player), 9);
        // As the sound plays it: the 40 ms ramp to 30 %.
        p.Editor.Click(p.Control("play"));
        var byHand = ByHand(0.3);
        byHand.Effects.Register("chime", Chime);
        byHand.Effects.Play("chime");
        Assert.Equal(Render(byHand, 40), Render(p.Audio, 40));
        // Kept within its ends; a volume that isn't a number is the default.
        p.Preview.Volume = 1.5;
        Assert.Equal((1.0, 1.0, "100"), (p.Preview.Volume, p.Audio.MasterVolume, p.Control("volume").Value));
        p.Preview.Volume = double.NaN;
        Assert.Equal((0.5, 0.5), (p.Preview.Volume, VolumeFile.Read(player)));
        p.Preview.Volume = 0.3;
        // Remembered: another editor on the same player's folder starts at 30 %.
        using var again = new PreviewEditor(w => w.AddRecord(ChimePath, Chime), playerFolder: player);
        again.Select(ChimePath);
        Assert.Equal(0.3, again.Audio!.MasterVolume, 9);
        Assert.Equal("30", again.Control("volume").Value);
    }

    [Theory]
    [InlineData("missing", 0.5)]
    [InlineData("{ \"version\": 1, \"volume\": 0.25 }", 0.25)]
    [InlineData("{ \"version\": 1, \"volume\": 0 }", 0)]
    [InlineData("{ \"version\": 1, \"volume\": 1 }", 1)]
    [InlineData("{ \"version\": 1, \"volume\": 1.5 }", 0.5)]
    [InlineData("{ \"version\": 1, \"volume\": -0.1 }", 0.5)]
    [InlineData("{ \"version\": 1, \"volume\": \"0.25\" }", 0.5)]
    [InlineData("{ \"version\": 2, \"volume\": 0.25 }", 0.5)]
    [InlineData("not json", 0.5)]
    public void TheVolumeFileIsReadTolerantly(string text, double volume)
    {
        using var folder = new Statventure.Tests.Storage.StorageFolder();
        if (text != "missing")
            folder.Write("editor/" + VolumeFile.FileName, System.Text.Encoding.UTF8.GetBytes(text));
        Assert.Equal(volume, VolumeFile.Read(folder.Root));
        Assert.True(VolumeFile.Write(folder.Root, 0.75));
        Assert.Equal(0.75, (double)JsonNode.Parse(File.ReadAllText(VolumeFile.File(folder.Root)))!["volume"]!);
        Assert.Equal(1, (int)JsonNode.Parse(File.ReadAllText(VolumeFile.File(folder.Root)))!["version"]!);
    }

    // ---------- Themes ----------

    /// <summary>The library the preview makes: the data as edited, loaded as the game loads its music.</summary>
    static MusicLibrary Edited(PreviewEditor p) => MusicLibrary.Load(p.Session.EditedData!);

    [Fact]
    public void AThemePlaysFollowsValidEditsAndKeepsTheLastLibraryThatLoaded()
    {
        using var p = new PreviewEditor(_ => { });
        p.Select(ThemePath);
        var preview = Assert.IsType<ThemePreview>(p.Preview.Shown);
        var byHand = ByHand();
        byHand.Reload(Edited(p));
        p.Editor.Click(p.Control("play"));
        byHand.Music.Play(Theme);
        Assert.True(preview.Playing);
        Assert.Equal("Playing", p.Document.QuerySelector(".preview-part .preview-state")!.TextContent);
        var heard = Render(p.Audio!, 120);
        Assert.Equal(Render(byHand, 120), heard);
        Assert.Contains(heard, s => s != 0);
        Assert.Equal(Theme, p.Audio!.Music.Theme);

        // A valid edit while it plays: the library swapped in, the theme restarting through its fades (its music changed).
        var theme = (ThemeSpec)p.Session.Entry(ThemePath)!.LastBuilt!;
        p.Commit(ThemePath, theme with { Tempo = theme.Tempo + 20 });
        byHand.Reload(Edited(p));
        Assert.Equal(Render(byHand, 120), Render(p.Audio, 120));
        Assert.Equal("", p.Note);

        // Not built: nothing changes for the sound.
        p.Break(ThemePath, "Title");
        Assert.Equal("This version doesn't build: the preview keeps the last valid version.", p.Note);
        Assert.Equal(Render(byHand, 60), Render(p.Audio, 60));

        // Built, but the music doesn't load (the theme's own check refuses the library): the last library plays on.
        p.Commit(ThemePath, theme with { Mode = "blues" });
        Assert.StartsWith("The music doesn't load (", p.Note);
        Assert.Contains("blues", p.Note);
        Assert.EndsWith("): the preview keeps the last valid version.", p.Note);
        Assert.Equal(Render(byHand, 60), Render(p.Audio, 60));

        // Stop: music.js's fade out.
        p.Editor.Click(p.Control("stop"));
        byHand.Music.Stop();
        Assert.False(preview.Playing);
        Assert.Equal("Stopped", p.Document.QuerySelector(".preview-part .preview-state")!.TextContent);
        Assert.Equal(Render(byHand, 120), Render(p.Audio, 120));
        Assert.Null(p.Audio.Music.Theme);
        // Played again while the music still doesn't load: the last library's theme plays.
        p.Editor.Click(p.Control("play"));
        byHand.Music.Play(Theme);
        Assert.Equal(Render(byHand, 60), Render(p.Audio, 60));
        Assert.Equal(Theme, p.Audio.Music.Theme);
    }

    [Fact]
    public void SpacePlaysAndStopsATheme()
    {
        using var p = new PreviewEditor(_ => { });
        p.Select(ThemePath);
        p.Editor.Key(" ", "Space");
        Render(p.Audio!, 2);
        Assert.Equal(Theme, p.Audio!.Music.Theme);
        p.Editor.Key(" ", "Space");
        Render(p.Audio, 2);
        Assert.Null(p.Audio.Music.Theme);
    }

    [Fact]
    public void AThemePlayingTakesTheEditsOfItsMusicAndStopsWhenAnythingElseIsSelected()
    {
        using var p = new PreviewEditor(w => w.AddRecord(ChimePath, Chime));
        p.Select(ThemePath);
        var byHand = ByHand();
        byHand.Reload(Edited(p));
        p.Editor.Click(p.Control("play"));
        byHand.Music.Play(Theme);
        Assert.Equal(Render(byHand, 60), Render(p.Audio!, 60));
        // An instrument selected: no preview of its own; the theme plays on, said so, and takes the instrument's edits.
        var harp = "audio/instruments/harp";
        p.Select(harp);
        Assert.IsType<MusicPartPreview>(p.Preview.Shown);
        Assert.Equal($"Playing {Theme}, which takes this entry's changes", p.Document.QuerySelector(".preview-part .preview-state")!.TextContent);
        Assert.Equal(Render(byHand, 30), Render(p.Audio!, 30));
        var instrument = (InstrumentSpec)p.Session.Entry(harp)!.LastBuilt!;
        p.Commit(harp, instrument with { Level = instrument.Level * 0.5 });
        byHand.Reload(Edited(p));
        Assert.Equal(Render(byHand, 60), Render(p.Audio, 60));
        // A room and back to the theme: it plays on.
        p.Select("audio/rooms/hall");
        p.Select(ThemePath);
        Assert.True(Assert.IsType<ThemePreview>(p.Preview.Shown).Playing);
        Assert.Equal(Render(byHand, 30), Render(p.Audio, 30));
        // Anything else selected stops it.
        p.Select(ChimePath);
        byHand.Music.Stop();
        Assert.Null(p.Preview.Sound.PlayingTheme);
        Assert.Equal(Render(byHand, 60), Render(p.Audio, 60));
        // With no theme playing, an instrument shows nothing.
        p.Select(harp);
        Assert.True(p.Preview.Shown!.Root().HasAttribute("hidden"));
    }
}

static class PreviewViewExtensions
{
    /// <summary>The preview's element.</summary>
    public static Statventure.UI.Dom.UiElement Root(this PreviewView view) => view.Part.Element.ElementChildren.Single(e => e.HasClass("preview"));
}
