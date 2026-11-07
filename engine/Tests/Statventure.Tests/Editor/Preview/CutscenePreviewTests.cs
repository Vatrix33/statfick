using Statventure.Animation;
using Statventure.Audio;
using Statventure.Editor.Preview;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Canvas;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Statventure.UI.Dom;
using static Statventure.Tests.Scene.JiruanIntro;

namespace Statventure.Tests.Editor.Preview;

using PreviewPart = Statventure.Editor.Preview.PreviewPart;

/// <summary>
/// The cutscene preview (Docs/Editor.md, "The preview (step 13)"; P34, P35) on the proof, Ji'ruan's intro, in a mod with
/// its stage, effects, sounds and the stand-in figure and clip library, on the tests' headless GPU and a silent sound: it
/// plays, pauses and seeks a preview copy with stand-ins chosen per cutscene and remembered, the tracks that can't be bound
/// left out and listed; a valid edit plays from the same time, an invalid one keeps the last.
/// </summary>
[Collection("GPU")]
public class CutscenePreviewTests(GpuFixture gpu)
{
    public const string CutscenePath = "world/cutscenes/jiruan", FigurePath = "models/figure", ActingPath = "models/jiruan-acting";

    static ModelData Glb(string name) => ModelData.Load(System.IO.Path.Combine(Render3DReferenceTests.ModelDirectory, name + ".glb"));

    /// <summary>The proof's data as a mod holds it: the sequence, its stage, effects and sounds, a figure and the acting clips.</summary>
    public static void Proof(PackWriter writer, Func<Sequence, Sequence>? change = null)
    {
        using var files = new VirtualFileSystem([Mount.Folder(DataFolder)]);
        var sequence = files.Load<Sequence>(CutscenePath);
        writer.AddRecord(CutscenePath, change?.Invoke(sequence) ?? sequence);
        writer.AddRecord(StagePath, ModelData.Load(StageFile));
        writer.AddRecord(MistPath, files.Load<ParticleEffect>(MistPath));
        writer.AddRecord(EmbersPath, files.Load<ParticleEffect>(EmbersPath));
        foreach (var (name, patch) in SoundPatches())
            writer.AddRecord("audio/effects/" + name, patch);
        writer.AddRecord(FigurePath, Glb("figure"));
        writer.AddRecord(ActingPath, ModelData.Load(ActingFile));
    }

    /// <summary>The proof's mod, written once for all the tests (compressing its stage takes seconds).</summary>
    static readonly Lazy<byte[]> ProofPack = new(() => PreviewEditor.PackBytes(w => Proof(w)));

    /// <summary>The editor over the proof's mod; <paramref name="change"/> the sequence changed in the session as the form changes it.</summary>
    PreviewEditor Open(bool sound = true, string? playerFolder = null, Func<Sequence, Sequence>? change = null)
    {
        var p = new PreviewEditor(ProofPack.Value, sound, gpu.Device, playerFolder: playerFolder);
        if (change is not null)
            p.Commit(CutscenePath, change((Sequence)p.Session.Entry(CutscenePath)!.LastBuilt!));
        return p;
    }

    static readonly string[] Actors = [King, .. Imps];

    /// <summary>Every figure's stand-in chosen (the figure and the acting clips), and the cutscene shown again.</summary>
    static CutscenePreview WithFigures(PreviewEditor p)
    {
        foreach (var role in Actors)
            p.Preview.SetStandIn(CutscenePath, role, new StandIn(FigurePath, ActingPath));
        p.Select(StagePath);
        p.Select(CutscenePath);
        return (CutscenePreview)p.Preview.Shown!;
    }

    static UiElement RoleSelect(PreviewEditor p, string role, string name) =>
        p.Document.QuerySelector($".preview-part .preview-stand-ins [data-role={role}] [data-preview={name}]")!;

    [Fact]
    public void WithoutStandInsTheFiguresTracksAreLeftOutAndListed()
    {
        using var p = Open();
        p.Select(CutscenePath);
        var preview = Assert.IsType<CutscenePreview>(p.Preview.Shown);
        Assert.Equal("", p.Note);
        var player = preview.Player!;
        Assert.Equal((true, 0.0), (player.Paused, player.Time));
        Assert.Equal(
            [(3, "no figure chosen for king"), (4, "no figure chosen for imp-1"), (5, "no figure chosen for imp-2"), (6, "no figure chosen for imp-3"),
             (7, "no figure chosen for imp-4"), (8, "no figure chosen for king")], preview.LeftOut);
        // The copy: the record without those tracks and the roles left unbound; the record itself untouched.
        Assert.Equal([Card, Glow], player.Sequence.Roles.Select(r => r.Name));
        Assert.Equal(preview.Sequence!.Tracks.Where((_, i) => i is < 3 or > 8), player.Sequence.Tracks);
        Assert.Equal(13, preview.Sequence.Tracks.Count);
        Assert.Equal(7, ((Sequence)p.Session.Entry(CutscenePath)!.LastBuilt!).Roles.Count);
        Assert.Equal("Left out: track 3 (animation track): no figure chosen for king",
            p.Document.QuerySelector(".preview-part .preview-left-out")!.ElementChildren.First().TextContent);
        Assert.Equal(["Stand-ins"], p.Document.QuerySelectorAll(".preview-part .preview-heading").Select(e => e.TextContent));
        Assert.Equal([.. Actors, Card, Glow], p.Document.QuerySelectorAll(".preview-part .preview-stand-ins > .preview-line").Select(l => l.GetAttribute("data-role")));
    }

    [Fact]
    public void AStandInChosenBindsItsRoleAndIsRememberedPerCutscene()
    {
        using var p = Open();
        p.Select(CutscenePath);
        var preview = (CutscenePreview)p.Preview.Shown!;
        Assert.Equal(["(no figure)", FigurePath, ActingPath, StagePath], RoleSelect(p, King, "model").Options.Select(o => o.TextContent));
        Assert.Equal(["(its own clips)", FigurePath, ActingPath, StagePath], RoleSelect(p, King, "clips").Options.Select(o => o.TextContent));
        p.Editor.Choose(RoleSelect(p, King, "model"), FigurePath);
        Assert.IsType<Actor>(preview.Bound[King]);
        // Its own clips: the figure has none of the cutscene's.
        Assert.Contains((3, "king's clips have no clip rise"), preview.LeftOut);
        Assert.DoesNotContain(preview.LeftOut, l => l.Track == 8);
        p.Editor.Choose(RoleSelect(p, King, "clips"), ActingPath);
        Assert.DoesNotContain(preview.LeftOut, l => l.Track is 3 or 8);
        Assert.Contains(preview.Player!.Sequence.Tracks, t => t is ActorTrack { Actor: King });
        Assert.Equal(new StandIn(FigurePath, ActingPath), StandInsFile.Read(p.Session.PlayerFolder)[CutscenePath][King]);
        // Another editor on the player's folder has it.
        using var again = Open(playerFolder: p.Session.PlayerFolder);
        again.Select(CutscenePath);
        Assert.IsType<Actor>(((CutscenePreview)again.Preview.Shown!).Bound[King]);
        // None chosen again: left out again.
        p.Editor.Choose(RoleSelect(p, King, "model"), "(no figure)");
        Assert.Contains((3, "no figure chosen for king"), preview.LeftOut);
        Assert.Null(StandInsFile.Read(p.Session.PlayerFolder)[CutscenePath][King].Model);
    }

    [Fact]
    public void ItShowsWhatPlayingTheCopyByHandShows()
    {
        using var p = Open(sound: false);
        var preview = WithFigures(p);
        Assert.Equal([(10, "the sound is off")], preview.LeftOut);
        var player = preview.Player!;
        // Paused at its start, sought: the state there in the next frame.
        preview.Seek(3.4);
        p.Editor.Frame();
        Assert.Equal((3.4, true), (player.Time, player.Paused));
        // The card's host holds the ids its cues name, their texts set by the copy.
        var host = p.Document.QuerySelector(".preview-part .preview-host[data-role=card]")!;
        Assert.Equal(["boss-intro-location", "boss-intro-overture", "boss-intro-label", "boss-intro-title-name", "boss-intro-title-em", "boss-intro-description"],
            host.ElementChildren.Select(e => e.Id));
        Assert.Equal(Label, p.Document.GetElementById("boss-intro-label")!.TextContent);
        // What it shows is the copy played by hand with the same stand-ins, sought the same way.
        p.DrawViews();
        var shown = p.Preview.Viewport!.View.Picture!;
        Assert.Equal(ByHand(p, player.Sequence, 3.4, shown.Width, shown.Height), shown.ReadPixels());
    }

    [Fact]
    public void ItPlaysPausesAndSeeks()
    {
        using var p = Open(sound: false);
        var preview = WithFigures(p);
        var player = preview.Player!;
        // Play: its clock runs with the document's frames (the click's frame and 29 more).
        p.Editor.Click(p.Control("play"));
        Assert.Equal(("Pause", false), (p.Control("play").TextContent, player.Paused));
        for (var i = 0; i < 29; i++)
            p.Editor.Frame();
        Assert.Equal(0.5, player.Time, 6);
        Assert.Equal(0.5, p.Control("time").ValueAsNumber, 6);
        p.Editor.Click(p.Control("play"));
        Assert.Equal(("Play", true), (p.Control("play").TextContent, player.Paused));
        var paused = player.Time;
        p.Editor.Frame();
        Assert.Equal(paused, player.Time);
        // The time slider: a seek (nothing fires), by a tenth of the timeline and by its steps.
        p.Key(p.Control("time"), "PageUp", 4);
        Assert.Equal(3.3, player.Time, 6);
        p.Key(p.Control("time"), "ArrowRight", 10);
        Assert.Equal(3.4, player.Time, 6);
        Assert.Equal("3.40 / 7.00 s", p.Control("time").Parent!.ElementChildren.Single(e => e.HasClass("preview-value")).TextContent);
        Assert.Same(player, preview.Player);
        // To its end: the copy ends (its end state); Play then plays it again from its start.
        p.Key(p.Control("time"), "End");
        Assert.True(player.Ended);
        Assert.Equal("Play", p.Control("play").TextContent);
        p.Editor.Click(p.Control("play"));
        Assert.NotSame(player, preview.Player);
        Assert.False(preview.Player!.Paused);
        Assert.InRange(preview.Player.Time, 0, 0.05);
        // Sought back after its end: played again from there, paused.
        p.Key(p.Control("time"), "End");
        var ended = preview.Player;
        Assert.True(ended.Ended);
        p.Key(p.Control("time"), "Home");
        Assert.NotSame(ended, preview.Player);
        Assert.Equal((0.0, true), (preview.Player!.Time, preview.Player.Paused));
    }

    /// <summary>
    /// The picture of <paramref name="copy"/> played by hand on a view of its own: the same stand-ins the preview makes
    /// (the figure and its clip library for each actor, an empty host, the preview's palette), paused, sought to
    /// <paramref name="time"/>, a frame later.
    /// </summary>
    byte[] ByHand(PreviewEditor p, Sequence copy, double time, int width, int height)
    {
        var device = gpu.Device;
        using var view = new View3D(device) { Background = PreviewPart.BackgroundColour, Sun = Viewport.DefaultSun };
        var scene = new SceneView(view);
        var document = new UiDocument(TestFonts.Library);
        var host = document.Body.Append("div");
        foreach (var id in copy.Tracks.OfType<UiTrack>().SelectMany(t => t.Cues).Select(c => c.Element).OfType<string>().Distinct())
            host.Append("div").SetAttribute("id", id);
        document.AddFrameClient(scene);
        document.Update(0);
        var figureData = Glb("figure");
        using var figure = Model.Create(device, figureData);
        using var stage = Model.Create(device, ModelData.Load(StageFile));
        var acting = AnimationClip.AllOf(ModelData.Load(ActingFile));
        var node = scene.Root.Add("cutscene");
        var bindings = new SequenceBindings();
        foreach (var role in Actors)
        {
            var skeleton = new Skeleton(figureData);
            var actor = node.Add($"actor {role}").Attach(new Actor(figure, new Animator(new AnimationStack(skeleton))));
            bindings.Actor(role, actor, name => acting.FirstOrDefault(c => c.Name == name) is { } clip ? new ClipBinding(clip, skeleton) : null);
        }
        var (mist, embers) = Effects();
        var preview = (CutscenePreview)p.Preview.Shown!;
        bindings.Element(Card, host).Palette(Glow, PreviewColours.Palette(preview.PaletteOf(copy, Glow, p.Preview.StandInsOf(CutscenePath))))
            .Stage(StagePath, stage).Effect(MistPath, mist).Effect(EmbersPath, embers);
        var player = scene.Play(copy, bindings, document);
        player.Pause();
        document.Update(1 / 60.0);
        player.Seek(time);
        document.Update(2 / 60.0);
        var frame = device.BeginFrame();
        view.Update(frame, width, height, 1);
        device.EndFrame(frame);
        device.WaitIdle();
        return view.Picture!.ReadPixels();
    }

    [Fact]
    public void AValidEditPlaysFromTheSameTimeAndAnInvalidOneKeepsTheLast()
    {
        using var p = Open(sound: false);
        var preview = WithFigures(p);
        preview.Seek(3);
        p.Editor.Frame();
        var first = preview.Player!;
        var sequence = (Sequence)p.Session.Entry(CutscenePath)!.LastBuilt!;
        p.Commit(CutscenePath, sequence with { Duration = 8 });
        Assert.Equal("", p.Note);
        var second = preview.Player!;
        Assert.NotSame(first, second);
        Assert.True(first.Ended);
        Assert.Equal((8.0, 3.0, true), (second.Sequence.Duration, second.Time, second.Paused));
        Assert.Equal("3.00 / 8.00 s", p.Control("time").Parent!.ElementChildren.Single(e => e.HasClass("preview-value")).TextContent);
        // The old copy's stages and stand-ins are gone: one stage in the scene.
        Assert.Single(p.Preview.Viewport!.Scene.Root.Children, n => n.Name == "jiruan");
        Assert.Single(p.Preview.Viewport.Scene.Root.Descendants(), n => n.Name == "cutscene");
        Assert.Single(p.Document.QuerySelectorAll(".preview-part .preview-host"));
        // Playing when edited: it plays on.
        p.Editor.Click(p.Control("play"));
        p.Commit(CutscenePath, sequence with { Duration = 7.5 });
        Assert.False(preview.Player!.Paused);
        // Not built: the last plays on.
        var playing = preview.Player;
        p.Break(CutscenePath, "Name");
        Assert.Equal("This version doesn't build: the preview keeps the last valid version.", p.Note);
        Assert.Same(playing, preview.Player);
        // Built, but with a problem (its own check: its tracks name a stage it no longer has).
        p.Commit(CutscenePath, sequence with { Stages = ["models/stages/nowhere"] });
        Assert.StartsWith("This version has problems (the sequence jiruan", p.Note);
        Assert.EndsWith(": the preview keeps the last valid version.", p.Note);
        Assert.Same(playing, preview.Player);
        Assert.Equal(7.5, preview.Sequence!.Duration);
    }

    [Fact]
    public void APaletteRoleTakesThePreviewsColoursEditableAndRemembered()
    {
        using var p = Open(sound: false);
        p.Select(CutscenePath);
        var preview = (CutscenePreview)p.Preview.Shown!;
        var slots = preview.PaletteOf(preview.Sequence!, Glow, p.Preview.StandInsOf(CutscenePath));
        Assert.Equal([("mist", "#d77a50"), ("mist.deep", "#5f9fd3"), ("ember", "#8fc85a"), ("ember.dim", "#e2c04a")], slots);
        var palette = (Palette)preview.Bound[Glow];
        Assert.Equal(Colors.LinearFromHex("#5f9fd3"), palette.Slots["mist.deep"].Lit);
        var field = p.Document.QuerySelector(".preview-part [data-role=glow] .preview-hex[data-slot=ember]")!;
        p.Editor.Enter(field, "#FFEEDD");
        Assert.Equal(Colors.LinearFromHex("#ffeedd"), ((Palette)preview.Bound[Glow]).Slots["ember"].Lit);
        Assert.Equal(new Dictionary<string, string> { ["ember"] = "#ffeedd" }, StandInsFile.Read(p.Session.PlayerFolder)[CutscenePath][Glow].Colours);
        Assert.Equal("#ffeedd", p.Document.QuerySelector(".preview-part [data-role=glow] .preview-hex[data-slot=ember]")!.Value);
        // A colour that isn't one: put back, nothing remembered.
        p.Editor.Enter(p.Document.QuerySelector(".preview-part [data-role=glow] .preview-hex[data-slot=mist]")!, "violet");
        Assert.Equal("#d77a50", p.Document.QuerySelector(".preview-part [data-role=glow] .preview-hex[data-slot=mist]")!.Value);
        Assert.False(StandInsFile.Read(p.Session.PlayerFolder)[CutscenePath][Glow].Colours!.ContainsKey("mist"));
    }

    [Fact]
    public void TracksThatCantBeBoundAreLeftOutEachWithWhy()
    {
        // A node role, with a camera cut and a sun to it and a particle cue on it; the king's track with a parameter cue.
        static Sequence Change(Sequence s) => s with
        {
            Roles = [.. s.Roles, new SequenceRole("spot", RoleKind.Node)],
            Tracks =
            [
                .. s.Tracks.Select(t => t is ActorTrack { Actor: King } a ? a with { Cues = [new ParameterCue(1, "mood", ParameterKind.Bool)] } : t),
                new CameraTrack { Cuts = [new CameraCut(0.5, "spot")] },
                new LightTrack { Cues = [new LightCue(0.5, "spot")] },
                new ParticleTrack { Cues = [new ParticleCue(1, MistPath, "spot", Burst: 4)] },
            ],
        };
        using var p = Open(sound: false, change: Change);
        var preview = WithFigures(p);
        Assert.Equal("", p.Note);
        Assert.Equal(
            [(3, "king's stand-in has no state machine for its parameter cues"), (10, "the sound is off"),
             (13, "its camera spot is a node role, an empty node with no camera"), (14, "its sun spot is a node role, an empty node with no sun")],
            preview.LeftOut);
        // The node role is an empty node at the origin, its particle cue kept.
        var spot = Assert.IsType<SceneNode>(preview.Bound["spot"]);
        Assert.Equal(System.Numerics.Matrix4x4.Identity, spot.World);
        Assert.Contains(preview.Player!.Sequence.Tracks, t => t is ParticleTrack { Cues: [{ Node: "spot" }] });
        Assert.Equal("An empty node at the origin", p.Document.QuerySelector(".preview-part [data-role=spot] .preview-about")!.TextContent);
    }

    [Fact]
    public void ItsSoundPlaysFromTheDataOrIsLeftOutWithWhy()
    {
        using var p = Open();
        var preview = WithFigures(p);
        Assert.Empty(preview.LeftOut);
        // The data's patches registered under their names, the music's library the data's.
        foreach (var (name, patch) in SoundPatches())
            Assert.Equal(RecordSerializer.ToBinary(patch), RecordSerializer.ToBinary(p.Audio!.Effects.Find(name)!.Patch));
        p.Editor.Click(p.Control("play"));
        p.Editor.Frame();
        PreviewEditor.Render(p.Audio!, 4);
        Assert.Equal(Theme, p.Audio!.Music.Theme);
        // Leaving it: the copy ended, its music stopped, its stages and hosts gone.
        p.Select(FigurePath);
        PreviewEditor.Render(p.Audio, 4);
        Assert.Null(p.Audio.Music.Theme);
        Assert.DoesNotContain(p.Preview.Viewport!.Scene.Root.Descendants(), n => n.Name is "jiruan" or "cutscene");
        Assert.Empty(p.Document.QuerySelectorAll(".preview-host"));
        // The music not loading (a theme broken): the audio track is left out, the rest plays.
        var theme = (ThemeSpec)p.Session.Entry("audio/themes/alder-vale")!.LastBuilt!;
        p.Commit("audio/themes/alder-vale", theme with { Mode = "blues" });
        p.Select(CutscenePath);
        preview = (CutscenePreview)p.Preview.Shown!;
        var (track, why) = Assert.Single(preview.LeftOut);
        Assert.Equal(10, track);
        Assert.StartsWith("the music doesn't load (", why);
        Assert.NotNull(preview.Player);
    }

    [Fact]
    public void AVersionThePlayerRefusesKeepsTheLast()
    {
        using var p = Open(sound: false);
        var preview = WithFigures(p);
        var playing = preview.Player!;
        // Strips that overlap once the clips' lengths are known (rise is 1.4 s long): the record's check can't tell, the
        // player refuses it.
        p.Commit(CutscenePath, ((Sequence)p.Session.Entry(CutscenePath)!.LastBuilt!) with
        {
            Tracks = [.. preview.Sequence!.Tracks.Select(t => t is ActorTrack { Actor: King } a ? a with { Strips = [new SequenceStrip("rise", 2.1), new SequenceStrip("spread", 2.2)] } : t)],
        });
        Assert.StartsWith("the sequence jiruan, track 3 (animation): strip 1 (spread, from 2.2 s) starts before strip 0 (rise) ends", p.Note);
        Assert.EndsWith(": the preview keeps the last valid version.", p.Note);
        Assert.Same(playing, preview.Player);
        Assert.False(playing.Ended);
        // What the refused play was given is taken away again: one host, one set of stand-ins.
        Assert.Single(p.Document.QuerySelectorAll(".preview-part .preview-host"));
        Assert.Single(p.Preview.Viewport!.Scene.Root.Descendants(), n => n.Name == "cutscene");
    }

    [Fact]
    public void AfterACutsceneAPreviewWithoutAViewLeavesNoSceneBehind()
    {
        using var p = Open(sound: false);
        WithFigures(p);
        p.Select("audio/effects/" + FlashSound);
        var viewport = p.Preview.Viewport!;
        Assert.Null(viewport.Content);
        Assert.Null(viewport.Scene.Camera);
        Assert.DoesNotContain(viewport.Scene.Root.Descendants(), n => n.Name is "jiruan" or "cutscene");
    }

    [Fact]
    public void EveryKindOfRoleHasItsStandIn()
    {
        using var p = new PreviewEditor(PreviewScreens.DemoMod, sound: false, gpu: gpu.Device);
        p.Preview.SetStandIn("world/cutscenes/demo", "hero", new StandIn(FigurePath));
        p.Select("world/cutscenes/demo");
        var preview = (CutscenePreview)p.Preview.Shown!;
        // A figure with its own clips, the preview's palette, an empty host, an empty node; the model role not chosen.
        var hero = Assert.IsType<Actor>(preview.Bound["hero"]);
        Assert.IsType<Palette>(preview.Bound["glow"]);
        Assert.Equal("card", Assert.IsType<UiElement>(preview.Bound["card"]).GetAttribute("data-role"));
        Assert.IsType<SceneNode>(preview.Bound["spot"]);
        Assert.False(preview.Bound.ContainsKey("crowd"));
        Assert.Equal([(3, "no model chosen for crowd")], preview.LeftOut);
        Assert.Equal(["(no model)", "models/figure", "models/stages/dolly"], RoleSelect(p, "crowd", "model").Options.Select(o => o.TextContent));
        Assert.Equal("An empty host with the elements its cues name", p.Document.QuerySelector(".preview-part [data-role=card] .preview-about")!.TextContent);
        // The palette's slots: its particle cue's effect's, then those of what its palette track colours (once chosen).
        Assert.Equal(["spark", "ash"], preview.PaletteOf(preview.Sequence!, "glow", p.Preview.StandInsOf("world/cutscenes/demo")).Select(s => s.Slot));
        // A stand-in chosen while sought: the copy plays again from there.
        preview.Seek(2);
        p.Editor.Frame();
        p.Editor.Choose(RoleSelect(p, "crowd", "model"), "models/figure");
        Assert.IsType<ModelInstance>(preview.Bound["crowd"]);
        Assert.Empty(preview.LeftOut);
        Assert.Equal((2.0, true), (preview.Player!.Time, preview.Player.Paused));
        Assert.Equal(["spark", "ash", "cloth", "skin"], preview.PaletteOf(preview.Sequence!, "glow", p.Preview.StandInsOf("world/cutscenes/demo")).Select(s => s.Slot));
        Assert.Equal(["spark", "ash", "cloth", "skin"], p.Document.QuerySelectorAll(".preview-part [data-role=glow] .preview-hex").Select(f => f.GetAttribute("data-slot")));
        preview.Seek(0);
        // Played: the figure's own clip, the particles on the node, the host's text.
        p.Editor.Click(p.Control("play"));
        for (var i = 0; i < 30; i++)
            p.Editor.Frame();
        // (The stand-ins made again when the model was chosen: a new figure.)
        Assert.NotSame(hero, preview.Bound["hero"]);
        Assert.Single(preview.Player!.LayersOf((Actor)preview.Bound["hero"]));
        Assert.NotEmpty(p.Preview.Viewport!.View.Particles);
        Assert.Equal("The dolly", p.Document.GetElementById("title")!.TextContent);
    }
}
