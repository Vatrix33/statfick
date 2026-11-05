using Statventure.Animation;
using Statventure.Audio;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Statventure.UI.Dom;

namespace Statventure.Tests.Scene;

/// <summary>
/// Ji'ruan's intro, M10's proof (Docs/Scene.md, step 7, phase 2; M10-11, Q30–Q33, S7-1–S7-5): the
/// sequence as the game would build it, over the stage <c>models/stages/jiruan</c> (Data/Models/jiruan.glb,
/// make_models.py's <c>jiruan</c>), the king and his four imps (the test figure as a stand-in) on the
/// stage's marks playing the clip library <c>jiruan-acting.glb</c> (S7-5) on the schedule approved with the
/// renders, the reveal as a palette cue from the ink fill to his own (Q24, S4-3), the camera and the sun
/// cut to the stage's, the mist and the embers as particle effects on the stage's emitter marks (effects
/// under Data/Cutscenes/models/effects), the event cues for the game, the UI track playing the old game's
/// title card (Data/Cutscenes/ui), and the reduced variant: the old game's still card, 2.4 s held at
/// 6.5 s (Q19, S4-9). Test data only (Q30): the record's JSON form is Data/Cutscenes/world/cutscenes/jiruan.json.
/// The audio track (S7-6): his theme from the start and three sounds at the stage's markers, their patches
/// as the user chose them (S7-9–S7-11) under Data/Cutscenes/audio/effects.
/// </summary>
public static class JiruanIntro
{
    public const string Name = "jiruan";
    public const string Path = "world/cutscenes/jiruan";
    public const string StagePath = "models/stages/jiruan";
    public const string MistPath = "models/effects/jiruan-mist";
    public const string EmbersPath = "models/effects/jiruan-embers";
    public const double Duration = 7.0;
    /// <summary>The roles: the king, his four imps, the card's host, the effects' palette.</summary>
    public const string King = "king", Card = "card", Glow = "glow";
    public static readonly string[] Imps = ["imp-1", "imp-2", "imp-3", "imp-4"];
    /// <summary>The imps' delays (the old CSS's animation-delay of bi-imp-fly).</summary>
    public static readonly double[] ImpDelays = [0, 0.15, 0.35, 0.5];
    /// <summary>His theme (audio.pak), cued at the start (S7-6: the old game started it with the battle).</summary>
    public const string Theme = "great-imp-jiruan";
    /// <summary>The three sounds at the stage's markers (S7-6; their patches, audio/effects/&lt;name&gt; in the proof's data, as the user chose them: S7-9–S7-11).</summary>
    public const string RiftSound = "jiruan-rift-opens", FlashSound = "jiruan-flash", TitleSound = "jiruan-title-card";
    public static readonly string[] Sounds = [RiftSound, FlashSound, TitleSound];

    /// <summary>The ink the king is a silhouette in until the reveal (the old brightness(0) filter's black, as the ink).</summary>
    public const string Silhouette = "#130e0b";

    /// <summary>The old INTROS table's texts for Ji'ruan (old-game/scripts/boss-intros.js).</summary>
    public const string Location = "THE GREAT ALDERWOOD · GREAT HUNT";
    public const string Overture = "The void opened beneath the Alderwood, and the imps came crawling after it.";
    public const string Label = "LORD OF THE IMP COURT";
    public const string TitleName = "Great Imp ";
    public const string TitleEm = "Ji'ruan";
    public const string Description = "Crown of the Void — while his court stands, no blade can reach him.";

    /// <summary>The proof's data folder: the effects, the sequence's JSON form, the card's markup and stylesheet.</summary>
    public static string DataFolder => System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "Cutscenes");
    public static string StageFile => System.IO.Path.Combine(Render3DReferenceTests.ModelDirectory, "jiruan.glb");
    public static string ActingFile => System.IO.Path.Combine(Render3DReferenceTests.ModelDirectory, "jiruan-acting.glb");
    public static string FixtureFile => System.IO.Path.Combine(DataFolder, "world", "cutscenes", "jiruan.json");
    /// <summary>The fixture as committed (the build output's copy is the source's).</summary>
    public static string SourceFixtureFile => System.IO.Path.GetFullPath(System.IO.Path.Combine(Golden.GoldenPath("x"), "..", "..", "Data", "Cutscenes", "world", "cutscenes", "jiruan.json"));
    public static string CardCss => File.ReadAllText(System.IO.Path.Combine(DataFolder, "ui", "jiruan-card.css"));
    public static string CardHtml => File.ReadAllText(System.IO.Path.Combine(DataFolder, "ui", "jiruan-card.html"));

    /// <summary>
    /// The acting as approved with the renders (Docs/Scene.md, step 7, "Phase 1b", the schedule): the king's
    /// rise as his mark lifts him (king-rises), his spread with its crown on the old flash's peak, his settle
    /// and breathing; each imp's fly-out with its mark leaving the rift (its delay) and its hover. Each
    /// clip starts in the pose the last one ends in, so the strips need no blending; the first holds back.
    /// </summary>
    public static readonly (string Clip, double Start, bool Loop)[] KingStrips = [("rise", 2.1, false), ("spread", 3.6, false), ("settle", 5.0, false), ("idle-king", 6.2, true)];

    public static (string Clip, double Start, bool Loop)[] ImpStrips(int imp) => [("fly-out", 1.68 + ImpDelays[imp], false), ("hover", 3.38 + ImpDelays[imp], true)];

    static void Strips(SequenceBuilder.AnimationBuilder a, (string Clip, double Start, bool Loop)[] strips)
    {
        for (var i = 0; i < strips.Length; i++)
            a.Strip(new SequenceStrip(strips[i].Clip, strips[i].Start, Repeat: strips[i].Loop ? double.PositiveInfinity : 1,
                Extrapolation: i == 0 ? Extrapolation.Hold : Extrapolation.HoldForward));
    }

    /// <summary>The sequence (its stage's model data given to check its names and markers against).</summary>
    public static Sequence Build(ModelData stage)
    {
        var builder = new SequenceBuilder(Name, Duration)
            .Role(King, RoleKind.Actor);
        foreach (var imp in Imps)
            builder.Role(imp, RoleKind.Actor);
        builder.Role(Card, RoleKind.Element).Role(Glow, RoleKind.Palette)
            .Stage(StagePath, stage)
            .Clip(Name, "Scene")
            .Camera(c => c.Cut(0, "jiruan/camera"))
            .Light(l => l.Sun(0, "jiruan/sun"))
            .Animation(King, "jiruan/boss", a => Strips(a, KingStrips));
        for (var i = 0; i < Imps.Length; i++)
        {
            var strips = ImpStrips(i);
            builder.Animation(Imps[i], "jiruan/" + Imps[i], a => Strips(a, strips));
        }
        return builder
            // The reveal (Q24): all ink, his own colours from the stage's marker "reveal" (4.48 s, under the old flash).
            .Palette(King, p => p.Fill(0, Silhouette).Own(CueTime.At("jiruan/reveal")))
            // The mist from the start (a burst so it is there at once, then a steady stream), the embers from the rift's opening.
            .Particles(p => p.Emit(0, MistPath, "jiruan/mist", seed: 7, burst: 28, rate: 10, duration: Duration, palette: Glow)
                .Emit(CueTime.At("jiruan/rift-opens"), EmbersPath, "jiruan/embers", seed: 11, rate: 16, duration: 6.58, palette: Glow))
            // The sound (S7-6): his theme from the start, a sound at the rift's opening, the flash and the title card.
            .Audio(a => a.Music(0, Theme).Effect(CueTime.At("jiruan/rift-opens"), RiftSound).Effect(CueTime.At("jiruan/flash"), FlashSound)
                .Effect(CueTime.At("jiruan/title-card"), TitleSound))
            .Events(e => e.Event(0, "intro-started").Event(CueTime.At("jiruan/title-card"), "title-card").Event(Duration, "intro-ended"))
            // The card: the old render() (texts, data-boss), shown and playing from the start (the old is-playing),
            // hidden at the end (the old 7000 ms timer's stopBossIntro).
            .Ui(Card, u => u
                .Text(0, "boss-intro-location", Location).Text(0, "boss-intro-overture", Overture).Text(0, "boss-intro-label", Label)
                .Text(0, "boss-intro-title-name", TitleName).Text(0, "boss-intro-title-em", TitleEm).Text(0, "boss-intro-description", Description)
                .SetAttribute(0, "data-boss", Name).RemoveAttribute(0, "hidden").AddClass(0, "is-playing")
                .SetAttribute(Duration, "hidden", "").RemoveClass(Duration, "is-playing"))
            // Reduced motion: the old still card, 2.4 s, the moment the card stands held.
            .Reduced(r => r.Hold(6.5, 2.4))
            .Build();
    }

    /// <summary>The proof's effects as the game loads them (S5-2), from the proof's data folder.</summary>
    public static (ParticleEffect Mist, ParticleEffect Embers) Effects()
    {
        using var files = new VirtualFileSystem([Mount.Folder(DataFolder)]);
        return (ParticleEffect.Load(files, MistPath), ParticleEffect.Load(files, EmbersPath));
    }

    /// <summary>The three sounds' patches as the game loads them (audio/effects/&lt;name&gt;), from the proof's data folder, by name.</summary>
    public static Dictionary<string, EffectPatch> SoundPatches()
    {
        using var files = new VirtualFileSystem([Mount.Folder(DataFolder)]);
        return Sounds.ToDictionary(name => name, name => files.Load<EffectPatch>("audio/effects/" + name));
    }

    /// <summary>The effects' colours (the palette role "glow"): the old mist gradient's violets, the rift's light.</summary>
    public static Palette GlowPalette() => new(new Dictionary<string, string>
    {
        ["mist"] = "#8a64c2", ["mist.deep"] = "#2a1a3c", ["ember"] = "#f1e6ff", ["ember.dim"] = "#c58cff",
    });

    /// <summary>The stand-ins' colours: the old sprites' (sprite-catalog.js): Ji'ruan's fur and skin, the Void Imps' wings and skin.</summary>
    public static Palette KingPalette() => new(new Dictionary<string, string> { ["cloth"] = "#3b2a44", ["skin"] = "#5a3566" });

    public static Palette ImpPalette() => new(new Dictionary<string, string> { ["cloth"] = "#3a2448", ["skin"] = "#6a3d78" });
}

/// <summary>
/// The proof on a scene and a document (1200 × 680, the old viewBox): the stage (drawn when a device is
/// given), the five stand-ins with their palettes, the card's host in the document, the bindings with the
/// clip library as the figures' clip source; frames advance the document, which drives the scene.
/// </summary>
sealed class JiruanBench : IDisposable
{
    public const int Width = 1200, Height = 680;

    public JiruanBench(GpuDevice device, bool reducedMotion = false, bool drawStage = true, string? extraCss = null, string? extraHtml = null)
    {
        StageData = ModelData.Load(JiruanIntro.StageFile);
        Stage = drawStage ? Model.Load(device, JiruanIntro.StageFile) : null;
        Figure = Model.Load(device, System.IO.Path.Combine(Render3DReferenceTests.ModelDirectory, "figure.glb"));
        Acting = AnimationClip.AllOf(ModelData.Load(JiruanIntro.ActingFile));
        (Mist, Embers) = JiruanIntro.Effects();
        View = new View3D(device) { Background = Colors.LinearFromHex("#06030b") };
        Scene = new SceneView(View);
        Document = new UiDocument(TestFonts.Library);
        Document.SetViewport(Width, Height, reducedMotion);
        Document.AddStylesheet("body { margin: 0; }\n" + JiruanIntro.CardCss + (extraCss ?? ""), "jiruan-card");
        Markup.SetInnerHtml(Document.Body, (extraHtml ?? "") + JiruanIntro.CardHtml);
        Document.AddFrameClient(Scene);
        Document.Update(0);
        (King, KingSkeleton) = NewActor(JiruanIntro.KingPalette());
        foreach (var _ in JiruanIntro.Imps)
            Imps.Add(NewActor(JiruanIntro.ImpPalette()));
        Glow = JiruanIntro.GlowPalette();
    }

    public ModelData StageData { get; }
    public Model? Stage { get; }
    public Model Figure { get; }
    public AnimationClip[] Acting { get; }
    public ParticleEffect Mist { get; }
    public ParticleEffect Embers { get; }
    public View3D View { get; }
    public SceneView Scene { get; }
    public UiDocument Document { get; }
    public Actor King { get; }
    public Skeleton KingSkeleton { get; }
    public List<(Actor Actor, Skeleton Skeleton)> Imps { get; } = [];
    public Palette Glow { get; }
    public UiElement Card => Document.GetElementById("boss-intro-cinematic")!;
    public double Now { get; private set; }

    (Actor, Skeleton) NewActor(Palette palette)
    {
        var skeleton = new Skeleton(Figure.Data);
        return (new Actor(Figure, new Animator(new AnimationStack(skeleton))) { Palette = palette }, skeleton);
    }

    /// <summary>The clip library's clips by name, bound to a figure's skeleton (S7-5), or null for a name it hasn't.</summary>
    public Func<string, ClipBinding?> Clips(Skeleton skeleton) => name => Acting.FirstOrDefault(c => c.Name == name) is { } clip ? new ClipBinding(clip, skeleton) : null;

    public SequenceBindings Bindings()
    {
        var bindings = new SequenceBindings().Actor(JiruanIntro.King, King, Clips(KingSkeleton));
        for (var i = 0; i < JiruanIntro.Imps.Length; i++)
            bindings.Actor(JiruanIntro.Imps[i], Imps[i].Actor, Clips(Imps[i].Skeleton));
        bindings.Element(JiruanIntro.Card, Card).Palette(JiruanIntro.Glow, Glow).Effect(JiruanIntro.MistPath, Mist).Effect(JiruanIntro.EmbersPath, Embers);
        return Stage is { } stage ? bindings.Stage(JiruanIntro.StagePath, stage) : bindings.Stage(JiruanIntro.StagePath, StageData);
    }

    public SequencePlayer Play(Sequence? sequence = null) => Scene.Play(sequence ?? JiruanIntro.Build(StageData), Bindings(), Document);

    /// <summary>A frame <paramref name="seconds"/> after the last.</summary>
    public void Frame(double seconds)
    {
        Now += seconds;
        Document.Update(Now);
    }

    /// <summary>Frames of <paramref name="step"/> (the last one shorter) until <paramref name="seconds"/> after the first frame of <paramref name="player"/>, which starts at the bench's time <paramref name="start"/>.</summary>
    public void FrameTo(double start, double seconds, double step = 1 / 60.0)
    {
        for (var frames = 0; Now < start + seconds - 1e-9; frames++)
        {
            Assert.True(frames < 2000, "too many frames");
            Frame(Math.Min(step, start + seconds - Now));
        }
    }

    public void Dispose()
    {
        View.Dispose();
        Figure.Dispose();
        Stage?.Dispose();
    }
}
