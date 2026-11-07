// The cutscene mode of the sandbox (Docs/Scene.md, step 7, phase 2; Q33): plays Ji'ruan's intro, the proof, in a
// window as the game would: the sequence record and its effects loaded from the game's data (here the
// tests' data folder, copied beside the sandbox: Data/Cutscenes, with the stage's record in a pack made at
// start), the stage and the stand-ins drawn in a surface element inside the old game's card, the card played
// by the sequence's UI track. Skip on the card's button and on Escape; R plays it again; reduced motion as
// the system sets it (the still card).
using Statventure.Animation;
using Statventure.Audio;
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Text;
using Statventure.UI;
using Statventure.UI.Dom;

sealed class CutsceneSandbox(string name, string? capture, int captureFrame) : Application(new ApplicationOptions
{
    Window = new WindowOptions { Title = "Statventure: Ji'ruan's intro (the M10 proof)", Width = 1200, Height = 680 },
})
{
    const string SurfaceCss = """

        body { margin: 0; }
        html { background: #05030a; }
        .boss-intro-scene surface { position: absolute; inset: 0; display: block; width: 100%; height: 100%; }
        """;

    UiHost _host = null!;
    UiDocument _document = null!;
    UiElement _surface = null!;
    Sequence _sequence = null!;
    Model _stage = null!, _figure = null!;
    AnimationClip[] _acting = [];
    ParticleEffect _mist = null!, _embers = null!;
    View3D _view = null!;
    SceneView _scene = null!;
    SequencePlayer? _player;
    long _replayAt = -1;
    bool _sound;
    string _pack = "";

    static string Data => Path.Combine(AppContext.BaseDirectory, "Data");

    protected override void Load()
    {
        if (name != "jiruan")
            throw new ArgumentException($"no cutscene {name}: the sandbox plays jiruan");
        // First, so the 40 ms ramp to it is over long before the sound starts.
        if (Audio is not null)
            Audio.MasterVolume = Volume;
        var fonts = new FontLibrary();
        var dir = Path.Combine(Data, "Fonts");
        fonts.AddFile(Path.Combine(dir, "Lato-Regular.ttf"), "Lato", 400);
        fonts.AddFile(Path.Combine(dir, "Lato-Semibold.ttf"), "Lato", 500, weightMax: 600);
        fonts.AddFile(Path.Combine(dir, "Lato-Bold.ttf"), "Lato", 700, weightMax: 900);
        fonts.AddFile(Path.Combine(dir, "STIXTwoText-Regular.otf"), "STIX Two Text", 400);
        fonts.AddFile(Path.Combine(dir, "STIXTwoText-Italic.otf"), "STIX Two Text", 400, italic: true);
        _document = new UiDocument(fonts);
        _document.AddStylesheet(File.ReadAllText(Path.Combine(Data, "Cutscenes", "ui", "jiruan-card.css")) + SurfaceCss, "jiruan-card");
        Markup.SetInnerHtml(_document.Body, File.ReadAllText(Path.Combine(Data, "Cutscenes", "ui", "jiruan-card.html")));
        var scene = _document.AllElements().Single(e => e.HasClass("boss-intro-scene"));
        Markup.SetInnerHtml(scene, "<surface></surface>");
        _document.Update(0);
        _surface = _document.AllElements().Single(e => e.Tag == "surface");
        _host = new UiHost(Window, Gpu, _document) { Audio = Audio };

        // The game's data: the sound pack beside the sandbox (packs/audio.pak, as the game mounts its packs; the
        // sequence's theme is checked against it when it loads: M11 step 6, the user's decision of 2026-11-07), the
        // stage's record in a pack (the models importer's, made here from its .glb), the sequence and the effects
        // from the proof's data folder.
        var stageFile = Path.Combine(Data, "Models", "jiruan.glb");
        _pack = Path.Combine(Path.GetTempPath(), $"statventure-cutscene-{Environment.ProcessId}.pak");
        var writer = new PackWriter();
        writer.AddRecord("models/stages/jiruan", ModelData.Load(stageFile));
        writer.Write(_pack);
        using (var files = new VirtualFileSystem([Mount.Pack(Path.Combine(AppContext.BaseDirectory, "packs", "audio.pak")), Mount.Pack(_pack),
                   Mount.Folder(Path.Combine(Data, "Cutscenes"))]))
        {
            _sequence = Sequence.Load(files, "world/cutscenes/jiruan");
            _mist = ParticleEffect.Load(files, "models/effects/jiruan-mist");
            _embers = ParticleEffect.Load(files, "models/effects/jiruan-embers");
            // The sound (S7-6, S7-9–S7-11): the audio track's patches from the proof's data, registered when all are
            // there and the music has the theme; else the proof plays silent and says why.
            var names = _sequence.Tracks.OfType<AudioTrack>().SelectMany(t => t.Cues).Where(c => c.Kind == AudioCueKind.Effect).Select(c => c.Name!).Distinct().ToArray();
            var missing = names.Where(n => !files.Exists("audio/effects/" + n)).ToArray();
            var theme = _sequence.Tracks.OfType<AudioTrack>().SelectMany(t => t.Cues).FirstOrDefault(c => c.Kind == AudioCueKind.Music)?.Name;
            if (Audio is null || missing.Length > 0 || theme is not null && !Audio.Music.Themes.Contains(theme))
                Log.Info("Cutscene", missing.Length > 0 ? $"silent: no patch yet for {string.Join(", ", missing)}" : "silent: no sound or no theme here");
            else
            {
                foreach (var name in names)
                    Audio.Effects.Register(name, files.Load<EffectPatch>("audio/effects/" + name));
                _sound = true;
                Log.Info("Cutscene", $"sound: {theme} and {string.Join(", ", names)} at volume {Volume * 100:0}");
            }
        }
        _stage = Model.Load(Gpu, stageFile);
        _figure = Model.Load(Gpu, Path.Combine(Data, "Models", "figure.glb"));
        _acting = AnimationClip.AllOf(ModelData.Load(Path.Combine(Data, "Models", "jiruan-acting.glb")));

        _view = new View3D(Gpu) { Background = Colors.LinearFromHex("#06030b") };
        _scene = new SceneView(_view);
        _document.AddFrameClient(_scene);
        _host.Views.Show(_surface, _view);
        _document.GetElementById("boss-intro-skip")!.AddEventListener("click", _ => Skip());
        Play();
    }

    void Skip()
    {
        if (_player is { } player)
            Log.Info("Cutscene", player.Skip() ? "skipped" : "not skippable now");
    }

    (Actor, Skeleton) NewActor(Dictionary<string, string> colours)
    {
        var skeleton = new Skeleton(_figure.Data);
        return (new Actor(_figure, new Animator(new AnimationStack(skeleton))) { Palette = new Palette(colours) }, skeleton);
    }

    Func<string, ClipBinding?> Clips(Skeleton skeleton) => clip => _acting.FirstOrDefault(c => c.Name == clip) is { } found ? new ClipBinding(found, skeleton) : null;

    /// <summary>Plays the proof from its start: the last play ended (finished to its end state) and its stage taken away.</summary>
    void Play()
    {
        if (_player is { } last)
        {
            last.Finish();
            foreach (var stage in last.Stages)
                stage.Node?.Remove();
        }
        var (king, kingSkeleton) = NewActor(new() { ["cloth"] = "#3b2a44", ["skin"] = "#5a3566" });
        var bindings = new SequenceBindings().Actor("king", king, Clips(kingSkeleton));
        foreach (var imp in new[] { "imp-1", "imp-2", "imp-3", "imp-4" })
        {
            var (actor, skeleton) = NewActor(new() { ["cloth"] = "#3a2448", ["skin"] = "#6a3d78" });
            bindings.Actor(imp, actor, Clips(skeleton));
        }
        bindings.Element("card", _document.GetElementById("boss-intro-cinematic")!)
            .Palette("glow", new Palette(new Dictionary<string, string> { ["mist"] = "#8a64c2", ["mist.deep"] = "#2a1a3c", ["ember"] = "#f1e6ff", ["ember.dim"] = "#c58cff" }))
            .Stage("models/stages/jiruan", _stage).Effect("models/effects/jiruan-mist", _mist).Effect("models/effects/jiruan-embers", _embers);
        if (_sound)
            bindings.Audio(Audio);
        _player = _scene.Play(_sequence, bindings, _document);
        _player.Cue += cue => Log.Info("Cutscene", $"{cue.Name} at {cue.Time:F3} s{(cue.Skipped ? " (skipped)" : "")}");
        Log.Info("Cutscene", $"playing {_sequence.Name}{(_player.Reduced ? " (reduced motion: the still card)" : "")}; Escape or the button skips, R plays it again");
    }

    protected override void Update(in GameTime time)
    {
        if (Input.WasPressed(Key.F11))
            Window.Fullscreen = !Window.Fullscreen;
        if (Input.WasPressed(Key.Escape))
            Skip();
        if (Input.WasPressed(Key.R))
        {
            // Ended now, played again a frame later, so the card's classes change in two style updates (its CSS
            // animations start again, as the old game forced a layout between).
            _player?.Finish();
            _replayAt = time.Frame + 1;
        }
        if (_replayAt == time.Frame)
        {
            _replayAt = -1;
            Play();
        }
        if (dump is not null)
        {
            Dump();
            return;
        }
        _host.Update(time.Total);
        if (capture is not null && time.Frame == captureFrame)
        {
            Log.Info("Cutscene", $"capturing frame {time.Frame} at {_player?.TimelineTime:F3} s of the timeline into {capture}");
            CaptureNextFrame(capture).ContinueWith(_ => Quit());
        }
    }

    /// <summary>
    /// --dump: the document on a clock of its own, frame by frame, each frame captured once drawn: every
    /// 1/12 s of the 7 s (frame-NNN.png) and the proof's sampled times (still-&lt;time&gt;.png), then it quits.
    /// </summary>
    void Dump()
    {
        if (_dumping is { IsCompleted: false })
        {
            _host.Update(_dumpTime);
            return;
        }
        if (_dumps.Count == 0)
        {
            if (_dumping is not null)
            {
                _dumping = null;
                Log.Info("Cutscene", $"dumped the frames into {dump}");
                Quit();
            }
            return;
        }
        var (at, file) = _dumps.Dequeue();
        _dumpTime = at;
        _host.Update(_dumpTime);
        _dumping = CaptureNextFrame(Path.Combine(dump!, file));
    }

    readonly Queue<(double Time, string File)> _dumps = new(Enumerable.Range(0, 85).Select(k => (k / 12.0, $"frame-{k:D3}.png"))
        .Concat(new[] { 0, 1.4, 2.5, 3.4, 4.6, 7.0 }.Select(t => (t, $"still-{t:F2}.png"))).OrderBy(d => d.Item1).ThenBy(d => d.Item2, StringComparer.Ordinal));
    Task<string>? _dumping;
    double _dumpTime;
    string? dump;

    /// <summary>The folder for --dump (null: play in real time).</summary>
    public string? DumpFolder { get => dump; init => dump = value; }

    /// <summary>The master volume, 0 to 1 (--volume 0-100).</summary>
    public double Volume { get; init; } = 1;

    protected override void Render(Frame frame, GpuImage target, in GameTime time) => _host.Render(frame, target);

    protected override void Unload()
    {
        _host.Dispose();
        _view.Dispose();
        _stage.Dispose();
        _figure.Dispose();
        File.Delete(_pack);
    }
}
