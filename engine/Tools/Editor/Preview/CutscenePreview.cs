using System.Globalization;
using Statventure.Animation;
using Statventure.Audio;
using Statventure.Editor.Session;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.UI.Dom;

namespace Statventure.Editor.Preview;

/// <summary>
/// A cutscene's preview (P34): the sequence as the game loads it from the data as edited (<c>Sequence.Load</c>: its
/// stages, particle effects and sounds from the same data) played on the shared scene with stand-ins for its roles,
/// chosen per cutscene and remembered in <c>editor/stand-ins.json</c> (G6), not in the mod: each actor role a figure (a
/// model of the data) and a clip library (a model of the data with clips; none chosen: the figure's own), each model
/// role a model of the data, each palette role the preview's palette (a distinct colour per slot its cues use, editable,
/// Q6), each element role an empty host over the view holding an empty element for each id its UI cues name (no
/// stylesheet in M11, Q5), each node role an empty node at the origin. The tracks that can't be bound are left out of
/// a preview copy of the sequence (the record untouched), each listed with why: an actor or model role with no stand-in,
/// a clip its clip library hasn't, parameter cues (a stand-in has no state machine), a camera cut or light cue to a node
/// role (an empty node carries neither), sound when the sound is off or the music doesn't load. Play, Pause, a time
/// slider (<c>Seek</c>); it starts paused at its start. A valid change plays the new version from the same time, paused
/// as it was (P35); one that doesn't load or play keeps the last, saying why.
/// </summary>
sealed class CutscenePreview(PreviewPart part, EditorEntry entry) : PreviewView(part, entry, PreviewKind.Cutscene)
{
    Sequence? _last;
    SequencePlayer? _player;
    Played? _played;
    readonly Dictionary<string, (ModelData Data, Model Model)> _models = new(StringComparer.Ordinal);
    UiElement _surface = null!, _hosts = null!, _play = null!, _slider = null!, _time = null!, _standIns = null!, _leftOut = null!;
    bool _framed, _seeking;

    /// <summary>What one play put in the scene and the document, taken away when the next replaces it.</summary>
    sealed record Played(SequencePlayer Player, SceneNode Node, IReadOnlyList<UiElement> Hosts);

    protected override bool HasLast => _last is not null;

    /// <summary>The sequence shown: the last valid (null before one).</summary>
    public Sequence? Sequence => _last;

    /// <summary>The preview copy playing (null without a 3D view).</summary>
    public SequencePlayer? Player => _player;

    /// <summary>The tracks left out of the preview copy (their index in the record), each with why.</summary>
    public IReadOnlyList<(int Track, string Why)> LeftOut { get; private set; } = [];

    /// <summary>The roles bound to stand-ins in the copy, by name, with what they were bound to.</summary>
    public IReadOnlyDictionary<string, object> Bound { get; private set; } = new Dictionary<string, object>();

    protected override void BuildControls()
    {
        var stage = Root.Append("div", "preview-stage");
        _surface = Surface(stage);
        _hosts = stage.Append("div", "preview-hosts");
        var line = Root.Append("div", "preview-buttons");
        _play = Button(line, "play", "Play", () => { if (_player is { Paused: false, Ended: false }) Pause(); else Play(); });
        _slider = line.Append("input", "preview-slider preview-timeline");
        _slider.SetAttribute("type", "range");
        _slider.SetAttribute("data-preview", "time");
        _slider.SetAttribute("min", "0");
        _slider.SetAttribute("max", "0");
        _slider.SetAttribute("step", "0.01");
        _slider.Value = "0";
        // (A range's change always follows its input: input is enough.)
        _slider.AddEventListener("input", _ => Seek(_slider.ValueAsNumber));
        _time = line.Append("span", "preview-value", "");
        Root.Append("div", "preview-heading", "Stand-ins");
        _standIns = Root.Append("div", "preview-stand-ins");
        _leftOut = Root.Append("div", "preview-left-out");
        Controls();
    }

    protected override string? Use(object value)
    {
        if (Part.Session.EditedData is not { } data)
            return $"The data as edited can't be read ({Part.Session.EditedDataProblem ?? "no pack is open"})";
        Sequence sequence;
        try
        {
            sequence = Sequence.Load(data, Entry.Path);
        }
        catch (IOException problem)
        {
            return problem.Message;
        }
        var why = Start(sequence, data, _player?.Time ?? 0, _player is null || _player.Paused || _player.Ended);
        if (why is not null)
            return why;
        _last = sequence;
        StandInControls();
        Controls();
        return null;
    }

    /// <summary>
    /// Plays a preview copy of <paramref name="sequence"/> with the stand-ins, from <paramref name="time"/>, paused or not; the
    /// copy before is ended and taken away only once the new one plays (its last state kept otherwise). Null when it plays,
    /// else why not.
    /// </summary>
    string? Start(Sequence sequence, VirtualFileSystem data, double time, bool paused)
    {
        var bindings = new SequenceBindings();
        var bound = new Dictionary<string, object>(StringComparer.Ordinal);
        var unbound = new Dictionary<string, string>(StringComparer.Ordinal);
        var clipSources = new Dictionary<string, Func<string, ClipBinding?>>(StringComparer.Ordinal);
        var used = new HashSet<string>(StringComparer.Ordinal);
        var viewport = Part.Viewport;
        var node = viewport is not null ? new SceneNode("cutscene") : null;
        var hosts = new List<UiElement>();
        var standIns = Part.StandInsOf(Entry.Path);

        // The roles' stand-ins.
        foreach (var role in sequence.Roles)
        {
            var standIn = standIns.GetValueOrDefault(role.Name) ?? StandIn.None;
            switch (role.Kind)
            {
                case RoleKind.Actor or RoleKind.Model:
                {
                    if (standIn.Model is not { } path)
                    {
                        unbound[role.Name] = $"no {(role.Kind == RoleKind.Actor ? "figure" : "model")} chosen for {role.Name}";
                        break;
                    }
                    if (ModelOf(path, out var why) is not { } figure)
                    {
                        unbound[role.Name] = $"{role.Name}'s stand-in {path} {why}";
                        break;
                    }
                    used.Add(path);
                    if (viewport is null)
                    {
                        bound[role.Name] = path;
                        break;
                    }
                    var model = GpuModel(path, figure);
                    var holder = node!.Add($"{role.Kind.ToString().ToLowerInvariant()} {role.Name}");
                    if (role.Kind == RoleKind.Model)
                    {
                        var instance = holder.Instance(model);
                        bindings.Model(role.Name, instance);
                        bound[role.Name] = instance;
                        break;
                    }
                    var skeleton = new Skeleton(figure);
                    var actor = holder.Attach(new Actor(model, new Animator(new AnimationStack(skeleton))));
                    Func<string, ClipBinding?>? clips = null;
                    if (standIn.Clips is { } library)
                    {
                        if (ModelOf(library, out var libraryProblem) is not { } source)
                        {
                            unbound[role.Name] = $"{role.Name}'s clip library {library} {libraryProblem}";
                            actor.Node!.Detach(actor);
                            break;
                        }
                        var all = AnimationClip.AllOf(source);
                        clips = name => all.FirstOrDefault(c => c.Name == name) is { } clip ? new ClipBinding(clip, skeleton) : null;
                    }
                    else
                    {
                        var own = AnimationClip.AllOf(figure);
                        clips = name => own.FirstOrDefault(c => c.Name == name) is { } clip ? new ClipBinding(clip, skeleton) : null;
                    }
                    clipSources[role.Name] = clips;
                    bindings.Actor(role.Name, actor, clips);
                    bound[role.Name] = actor;
                    break;
                }
                case RoleKind.Palette:
                {
                    var palette = PreviewColours.Palette(PaletteOf(sequence, role.Name, standIns));
                    bindings.Palette(role.Name, palette);
                    bound[role.Name] = palette;
                    break;
                }
                case RoleKind.Element:
                {
                    var host = Host(sequence, role.Name);
                    hosts.Add(host);
                    bindings.Element(role.Name, host);
                    bound[role.Name] = host;
                    break;
                }
                default:
                {
                    var empty = node?.Add($"node {role.Name}") ?? new SceneNode(role.Name);
                    bindings.Node(role.Name, empty);
                    bound[role.Name] = empty;
                    break;
                }
            }
        }

        // The sound: the data's music and effects given to the sound, or the audio tracks left out.
        string? silent = null;
        if (sequence.Tracks.OfType<AudioTrack>().Any())
        {
            if (Part.Audio is not { } audio)
                silent = "the sound is off";
            else if (!Part.Sound.Refresh() && Part.Sound.LibraryProblem is { } problem)
                silent = $"the music doesn't load ({problem})";
            else
                // The data's patches are the sound's: the swap registered those new or changed, and the effect preview
                // registers only what the data as edited holds.
                bindings.Audio(audio);
        }

        // The tracks that can't be bound, and the copy without them and the roles left unbound.
        var leftOut = new List<(int, string)>();
        var kept = new List<SequenceTrack>();
        for (var t = 0; t < sequence.Tracks.Count; t++)
        {
            var track = sequence.Tracks[t];
            var why = track switch
            {
                ActorTrack a when unbound.TryGetValue(a.Actor, out var reason) => reason,
                ActorTrack a when a.Cues.Count > 0 => $"{a.Actor}'s stand-in has no state machine for its parameter cues",
                ActorTrack a when clipSources.TryGetValue(a.Actor, out var clips) && a.Strips.FirstOrDefault(s => clips(s.Clip) is null) is { } strip =>
                    $"{a.Actor}'s clips have no clip {strip.Clip}",
                PaletteTrack p when unbound.TryGetValue(p.Target, out var reason) => reason,
                CameraTrack c when c.Cuts.FirstOrDefault(cut => !cut.Camera.Contains('/')) is { } cut => $"its camera {cut.Camera} is a node role, an empty node with no camera",
                LightTrack l when l.Cues.FirstOrDefault(cue => !cue.Sun.Contains('/')) is { } cue => $"its sun {cue.Sun} is a node role, an empty node with no sun",
                AudioTrack when silent is not null => silent,
                _ => null,
            };
            if (why is null)
                kept.Add(track);
            else
                leftOut.Add((t, why));
        }
        var copy = sequence with { Roles = [.. sequence.Roles.Where(r => !unbound.ContainsKey(r.Name))], Tracks = kept };

        if (viewport is null)
        {
            LeftOut = leftOut;
            Bound = bound;
            return null;
        }

        // The stages and particle effects from the data as edited.
        foreach (var path in sequence.Stages)
        {
            if (ModelOf(path, out var why) is not { } stage)
                return DropPending($"The stage {path} {why}");
            used.Add(path);
            bindings.Stage(path, GpuModel(path, stage));
        }
        foreach (var path in kept.OfType<ParticleTrack>().SelectMany(t => t.Cues).Select(c => c.Effect).Distinct(StringComparer.Ordinal))
        {
            try
            {
                bindings.Effect(path, ParticleEffect.Load(data, path));
            }
            catch (IOException problem)
            {
                return DropPending(problem.Message);
            }
        }

        // Played with the old copy still in place: if the new one is refused, nothing changed.
        // The preview's node (the view shown in the surface) the first time; the stand-ins under it.
        (viewport.Content ?? viewport.Begin(_surface)).Add(node!);
        foreach (var host in hosts)
            _hosts.Append(host);
        SequencePlayer player;
        try
        {
            player = viewport.Scene.Play(copy, bindings, Part.View.Document);
        }
        catch (ArgumentException refused)
        {
            node!.Remove();
            foreach (var host in hosts)
                host.Remove();
            return DropPending(refused.Message);
        }
        if (time > 0)
            player.Seek(Math.Min(time, player.Duration));
        if (paused)
            player.Pause();
        TakeAway();
        _player = player;
        _played = new Played(player, node!, hosts);
        LeftOut = leftOut;
        Bound = bound;
        if (!_framed && player.Stages.FirstOrDefault() is { } first)
        {
            _framed = true;
            viewport.Orbit.Frame(ModelPreview.Bounds(first.Data, out var radius), radius);
        }
        KeepModels(used);
        return null;
    }

    /// <summary>The copy playing ended and taken away: its stages, stand-ins and hosts (the music it started stops).</summary>
    void TakeAway()
    {
        if (_played is not { } played)
            return;
        _played = null;
        _player = null;
        played.Player.Finish();
        foreach (var stage in played.Player.Stages)
            stage.Node?.Remove();
        played.Node.Remove();
        foreach (var host in played.Hosts)
            host.Remove();
    }

    /// <summary>The model at <paramref name="path"/> as edited (the session's entry, its last value that built), checked; null with why not.</summary>
    ModelData? ModelOf(string path, out string why)
    {
        why = "";
        if (Part.Session.Entry(path) is not { LastBuilt: ModelData data })
        {
            why = "isn't a model in the data";
            return null;
        }
        try
        {
            data.Validate();
        }
        catch (InvalidDataException problem)
        {
            why = $"isn't valid ({problem.Message})";
            return null;
        }
        return data;
    }

    /// <summary>
    /// The GPU's model of <paramref name="data"/> at <paramref name="path"/>: the one made before while the data is the same,
    /// else a new one, kept once the play it is made for starts (<see cref="KeepModels"/>), given back if it doesn't.
    /// </summary>
    Model GpuModel(string path, ModelData data)
    {
        if (_models.TryGetValue(path, out var made) && made.Data == data)
            return made.Model;
        if (_pending.TryGetValue(path, out var pending) && pending.Data == data)
            return pending.Model;
        var model = Model.Create(Part.Gpu!, data);
        _pending[path] = (data, model);
        return model;
    }

    /// <summary>The new play started: its new models kept, those it doesn't use given back once nothing draws them.</summary>
    void KeepModels(HashSet<string> used)
    {
        foreach (var (path, made) in _pending)
        {
            if (_models.TryGetValue(path, out var old))
                _retired.Add(old.Model);
            _models[path] = made;
        }
        _pending.Clear();
        foreach (var path in _models.Keys.Where(p => !used.Contains(p)).ToList())
        {
            _models.Remove(path, out var unused);
            _retired.Add(unused.Model);
        }
    }

    /// <summary>The play wasn't started: the models made for it given back (nothing draws them).</summary>
    string DropPending(string why)
    {
        foreach (var (_, model) in _pending.Values)
            model.Dispose();
        _pending.Clear();
        return why;
    }

    readonly Dictionary<string, (ModelData Data, Model Model)> _pending = new(StringComparer.Ordinal);
    readonly List<Model> _retired = [];

    /// <summary>
    /// The preview's palette for the palette role <paramref name="role"/> (Q6): the slots its cues use (the start and end
    /// slots of the particle effects its particle cues name; the material slots of what its palette tracks colour), each
    /// in the colour the user gave it, else a distinct one.
    /// </summary>
    public IReadOnlyList<(string Slot, string Colour)> PaletteOf(Sequence sequence, string role, IReadOnlyDictionary<string, StandIn> standIns)
    {
        var slots = new List<string>();
        var data = Part.Session.EditedData;
        foreach (var cue in sequence.Tracks.OfType<ParticleTrack>().SelectMany(t => t.Cues).Where(c => c.Palette == role))
        {
            if (data is null)
                break;
            try
            {
                var effect = data.Load<ParticleEffect>(cue.Effect);
                slots.Add(effect.StartColor);
                slots.Add(effect.EndColor);
            }
            catch (IOException)
            {
            }
        }
        foreach (var track in sequence.Tracks.OfType<PaletteTrack>().Where(t => t.Cues.Any(c => c.Palette == role)))
        {
            var target = sequence.Roles.FirstOrDefault(r => r.Name == track.Target) is { } targetRole
                ? standIns.GetValueOrDefault(targetRole.Name)?.Model
                : sequence.Stages.FirstOrDefault(s => Sequence.StageName(s) == track.Target);
            if (target is not null && Part.Session.Entry(target)?.LastBuilt is ModelData model)
                slots.AddRange(model.Materials.Select(m => m.Slot).Where(s => s.Length > 0));
        }
        return PreviewColours.For(slots, standIns.GetValueOrDefault(role)?.Colours);
    }

    /// <summary>An empty host for the element role <paramref name="role"/> (Q5): an empty element for each id its UI cues name, in the order named.</summary>
    static UiElement Host(Sequence sequence, string role)
    {
        var host = new UiElement("div");
        host.AddClass("preview-host");
        host.SetAttribute("data-role", role);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var cue in sequence.Tracks.OfType<UiTrack>().Where(t => t.Host == role).SelectMany(t => t.Cues))
            if (cue.Element is { } id && ids.Add(id))
                host.Append("div").SetAttribute("id", id);
        return host;
    }

    public void Play()
    {
        if (_player is not { } player)
            return;
        if (player.Ended || player.Time >= player.Duration)
        {
            Restart(0, paused: false);
            return;
        }
        player.Resume();
        Controls();
    }

    public void Pause()
    {
        _player?.Pause();
        Controls();
    }

    /// <summary>Jumps to <paramref name="time"/> seconds (<c>Seek</c>: nothing fires; a copy that ended is played again from there, paused).</summary>
    public void Seek(double time)
    {
        if (_player is not { } player || !double.IsFinite(time))
            return;
        _seeking = true;
        if (player.Ended)
            Restart(time, paused: true);
        else
            player.Seek(time);
        _seeking = false;
        Controls(slider: false);
    }

    void Restart(double time, bool paused)
    {
        if (_last is { } sequence && Part.Session.EditedData is { } data)
            Note(Start(sequence, data, time, paused));
        Controls();
    }

    /// <summary>A stand-in chosen: remembered, and the copy played again from the same time.</summary>
    void Choose(string role, StandIn standIn)
    {
        Part.SetStandIn(Entry.Path, role, standIn);
        if (_last is not null)
        {
            Restart(_player?.Time ?? 0, _player is null || _player.Paused || _player.Ended);
            StandInControls();
        }
    }

    public override void Frame(double seconds)
    {
        if (Part.Viewport is { } viewport)
            viewport.ApplyOrbit();
        if (_retired.Count > 0)
        {
            Part.Viewport?.Scene.Refresh();
            foreach (var model in _retired)
                model.Dispose();
            _retired.Clear();
        }
    }

    /// <summary>After the frame's scene moved the copy on: the controls show where it is now.</summary>
    public override void AfterFrame()
    {
        if (_player is not null && !_seeking)
            Controls();
    }

    /// <summary>Play or Pause, the time slider and the time as the copy is.</summary>
    void Controls(bool slider = true)
    {
        var duration = _player?.Duration ?? _last?.Duration ?? 0;
        var time = _player?.Time ?? 0;
        _play.TextContent = _player is { Paused: false, Ended: false } ? "Pause" : "Play";
        _play.ToggleAttribute("disabled", _last is null);
        _slider.SetAttribute("max", Number(duration));
        _slider.ToggleAttribute("disabled", _last is null);
        if (slider)
            _slider.Value = Number(time);
        _time.TextContent = $"{time.ToString("0.00", CultureInfo.InvariantCulture)} / {Seconds(duration)}";
    }

    /// <summary>The stand-ins' rows (one per role, in the record's order) and the tracks left out.</summary>
    void StandInControls()
    {
        if (_last is not { } sequence)
            return;
        _standIns.ClearChildren();
        var standIns = Part.StandInsOf(Entry.Path);
        var models = Part.Session.Entries.Where(e => e.LastBuilt is ModelData).Select(e => e.Path).ToList();
        var libraries = Part.Session.Entries.Where(e => e.LastBuilt is ModelData { Animations.Count: > 0 }).Select(e => e.Path).ToList();
        foreach (var role in sequence.Roles)
        {
            var standIn = standIns.GetValueOrDefault(role.Name) ?? StandIn.None;
            var line = Line(_standIns, role.Name, $"The {role.Kind.ToString().ToLowerInvariant()} role {role.Name}");
            line.SetAttribute("data-role", role.Name);
            switch (role.Kind)
            {
                case RoleKind.Actor:
                    Select(line, "model", [("", "(no figure)"), .. models.Select(p => (p, p))], standIn.Model ?? "",
                        value => Choose(role.Name, standIn with { Model = value.Length > 0 ? value : null }));
                    Select(line, "clips", [("", "(its own clips)"), .. libraries.Select(p => (p, p))], standIn.Clips ?? "",
                        value => Choose(role.Name, standIn with { Clips = value.Length > 0 ? value : null }));
                    break;
                case RoleKind.Model:
                    Select(line, "model", [("", "(no model)"), .. models.Select(p => (p, p))], standIn.Model ?? "",
                        value => Choose(role.Name, standIn with { Model = value.Length > 0 ? value : null }));
                    break;
                case RoleKind.Palette:
                    var slots = line.Append("div", "preview-slots");
                    foreach (var (slot, colour) in PaletteOf(sequence, role.Name, standIns))
                    {
                        var item = slots.Append("span", "preview-slot");
                        item.SetAttribute("title", $"The preview's colour for the slot {slot} (remembered by the editor, not saved in the pack)");
                        item.Append("span", "preview-swatch").SetStyle("background-color", colour);
                        item.Append("span", "preview-slot-name", slot);
                        var field = item.Append("input", "preview-hex");
                        field.SetAttribute("type", "text");
                        field.SetAttribute("data-slot", slot);
                        field.Value = colour;
                        field.AddEventListener("change", _ =>
                        {
                            if (PreviewColours.Read(field.Value) is not { } hex)
                            {
                                field.Value = colour;
                                return;
                            }
                            var colours = new Dictionary<string, string>(standIn.Colours ?? new Dictionary<string, string>(), StringComparer.Ordinal) { [slot] = hex };
                            Choose(role.Name, standIn with { Colours = colours });
                        });
                    }
                    break;
                case RoleKind.Element:
                    line.Append("span", "preview-about", "An empty host with the elements its cues name");
                    break;
                default:
                    line.Append("span", "preview-about", "An empty node at the origin");
                    break;
            }
        }
        _leftOut.ClearChildren();
        foreach (var (track, why) in LeftOut)
        {
            var kind = RecordShape.Of(sequence.Tracks[track].GetType()).DisplayName;
            _leftOut.Append("div", "preview-about", $"Left out: track {track} ({char.ToLowerInvariant(kind[0])}{kind[1..]}): {why}");
        }
    }

    public override bool Space() => false;

    public override void Dispose()
    {
        var hadSound = _player is not null && _last?.Tracks.OfType<AudioTrack>().Any() == true;
        TakeAway();
        if (hadSound)
            Part.Audio?.Music.Stop();
        Part.Viewport?.End();
        Part.Viewport?.Scene.Refresh();
        foreach (var (_, model) in _models.Values)
            model.Dispose();
        _models.Clear();
        foreach (var model in _retired)
            model.Dispose();
        _retired.Clear();
        DropPending("");
    }
}
