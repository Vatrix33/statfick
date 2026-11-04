using Statventure.Animation;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;

namespace Statventure.Scene;

// A cutscene as data (Docs/Scene.md, "The sequence record", "How it works", step 4): the records
// scene.*, version 1, every track kind declared (Q14, S4-1), made by SequenceBuilder or read from the
// game's data (world/cutscenes/<name>, M10-10), played by SequencePlayer. Times are seconds on the
// sequence's timeline; a cue may sit at a marker of a stage instead (S4-2: "<stage>/<marker>", its
// time then an offset from the marker). Names (Q15): roles the game binds when it plays the sequence;
// the nodes of its own stages as "<stage>/<Blender name>"; UI elements by id within a bound host.

/// <summary>What the game binds to a role when it plays a sequence (Q15).</summary>
public enum RoleKind
{
    /// <summary>A figure (<see cref="Scene.Actor"/>): its clock follows the sequence while it plays.</summary>
    Actor,
    /// <summary>A scene node the game provides.</summary>
    Node,
    /// <summary>A placed model or an instanced model (<see cref="PlacedModel"/>, <see cref="ModelInstance"/>): palette cues.</summary>
    Model,
    /// <summary>A UI host element: the UI track's elements are found by id within it.</summary>
    Element,
    /// <summary>A palette (palette and particle cues).</summary>
    Palette,
}

/// <summary>A role (record <c>scene.role</c>): a name the game binds something of its kind to.</summary>
[Record("scene.role", 1)]
public sealed record SequenceRole([property: Field(1)] string Name, [property: Field(2)] RoleKind Kind);

/// <summary>
/// A span of the reduced variant's cut list (record <c>scene.span</c>, Q19): the timeline from
/// <see cref="From"/> to <see cref="To"/> played at its own length, then <see cref="To"/> held for
/// <see cref="Hold"/> seconds (a span whose ends are equal holds that moment).
/// </summary>
[Record("scene.span", 1)]
public sealed record SequenceSpan([property: Field(1)] double From, [property: Field(2)] double To, [property: Field(3)] double Hold = 0)
{
    /// <summary>Seconds the span takes to play.</summary>
    public double Length => To - From + Hold;

    /// <summary>A span holding the moment <paramref name="at"/> for <paramref name="length"/> seconds.</summary>
    public static SequenceSpan Held(double at, double length) => new(at, at, length);
}

/// <summary>
/// A clip placed on the timeline (record <c>scene.strip</c>): an <see cref="AnimationStrip"/>'s settings
/// (Blender's NLA strip), its start in seconds on the sequence's timeline; <see cref="ClipEnd"/> null
/// plays to the clip's end; <see cref="Repeat"/> may be infinite (a loop).
/// </summary>
[Record("scene.strip", 1)]
public sealed record SequenceStrip(
    [property: Field(1)] string Clip,
    [property: Field(2)] double Start = 0,
    [property: Field(3)] double ClipStart = 0,
    [property: Field(4)] double? ClipEnd = null,
    [property: Field(5)] double Scale = 1,
    [property: Field(6)] double Repeat = 1,
    [property: Field(7)] bool Reversed = false,
    [property: Field(8)] double BlendIn = 0,
    [property: Field(9)] double BlendOut = 0,
    [property: Field(10)] double? Influence = null,
    [property: Field(11)] BlendMode Mode = BlendMode.Replace,
    [property: Field(12)] Extrapolation Extrapolation = Extrapolation.HoldForward)
{
    /// <summary>The strip on <paramref name="clip"/> (bound to its skeleton), starting at <paramref name="origin"/> + <see cref="Start"/> on the stack's clock.</summary>
    internal AnimationStrip ToStrip(ClipBinding clip, double origin) => new(clip)
    {
        Start = origin + Start,
        ClipStart = ClipStart,
        ClipEnd = ClipEnd ?? clip.Clip.Duration,
        Scale = Scale,
        Repeat = Repeat,
        Reversed = Reversed,
        BlendIn = BlendIn,
        BlendOut = BlendOut,
        Influence = Influence,
        Mode = Mode,
        Extrapolation = Extrapolation,
    };
}

/// <summary>A cue: a time in seconds, or an offset from a stage's marker (<c>"&lt;stage&gt;/&lt;marker&gt;"</c>, S4-2).</summary>
public interface ISequenceCue
{
    double Time { get; }
    string? Marker { get; }
}

/// <summary>A visibility cue (<c>scene.visibility-cue</c>, Q23): the node (with its subtree) shown or hidden from then on.</summary>
[Record("scene.visibility-cue", 1)]
public sealed record VisibilityCue([property: Field(1)] double Time, [property: Field(2)] bool Shown, [property: Field(3)] string? Marker = null) : ISequenceCue;

/// <summary>
/// A cue for a figure's state machine (<c>scene.parameter-cue</c>, Q16): a bool, int or float parameter
/// set to <see cref="Value"/> from then on (state), or a trigger set when the clock passes it (a moment).
/// </summary>
[Record("scene.parameter-cue", 1)]
public sealed record ParameterCue([property: Field(1)] double Time, [property: Field(2)] string Parameter, [property: Field(3)] ParameterKind Kind,
    [property: Field(4)] double Value = 1, [property: Field(5)] string? Marker = null) : ISequenceCue;

/// <summary>
/// A palette cue (<c>scene.palette-cue</c>, Q24, S4-3): from then on the target shows the palette bound
/// to the role <see cref="Palette"/>, or <see cref="Fill"/> ("#rrggbb") on every slot of its models
/// (the all-ink silhouette), or with neither its own palette (as it was when the sequence started).
/// </summary>
[Record("scene.palette-cue", 1)]
public sealed record PaletteCue([property: Field(1)] double Time, [property: Field(2)] string? Palette = null, [property: Field(3)] string? Fill = null,
    [property: Field(4)] string? Marker = null) : ISequenceCue;

/// <summary>An event cue (<c>scene.event-cue</c>): delivered to game code when the clock passes it, or at a skip marked as skipped (Q18).</summary>
[Record("scene.event-cue", 1)]
public sealed record EventCue([property: Field(1)] double Time, [property: Field(2)] string Name, [property: Field(3)] string? Marker = null) : ISequenceCue;

/// <summary>A camera cut (<c>scene.camera-cut</c>, Q11): the view looks through the camera on this node from then on.</summary>
[Record("scene.camera-cut", 1)]
public sealed record CameraCut([property: Field(1)] double Time, [property: Field(2)] string Camera, [property: Field(3)] string? Marker = null) : ISequenceCue;

/// <summary>A light cue (<c>scene.light-cue</c>, Q12): the view's sun is the sun on this node from then on.</summary>
[Record("scene.light-cue", 1)]
public sealed record LightCue([property: Field(1)] double Time, [property: Field(2)] string Sun, [property: Field(3)] string? Marker = null) : ISequenceCue;

/// <summary>
/// A particle cue (<c>scene.particle-cue</c>, Q25, Q26): an emitter of the effect at
/// <see cref="Effect"/> (<c>models/effects/&lt;name&gt;</c>) started on a node, with its seed, burst,
/// rate (particles a second) and duration, coloured by the palette role <see cref="Palette"/> (or none).
/// </summary>
[Record("scene.particle-cue", 1)]
public sealed record ParticleCue([property: Field(1)] double Time, [property: Field(2)] string Effect, [property: Field(3)] string Node,
    [property: Field(4)] int Seed = 0, [property: Field(5)] int Burst = 0, [property: Field(6)] double Rate = 0, [property: Field(7)] double Duration = 0,
    [property: Field(8)] string? Palette = null, [property: Field(9)] string? Marker = null) : ISequenceCue;

/// <summary>What an audio cue does.</summary>
public enum AudioCueKind
{
    /// <summary>Plays the sound effect <see cref="AudioCue.Name"/> with its volume, pan and pitch.</summary>
    Effect,
    /// <summary>Switches the music to the theme <see cref="AudioCue.Name"/> (music.js's fades).</summary>
    Music,
    /// <summary>Stops the music.</summary>
    StopMusic,
}

/// <summary>An audio cue (<c>scene.audio-cue</c>, M10-8): an effect, a music theme or the music's stop, scheduled on the audio clock (step 6).</summary>
[Record("scene.audio-cue", 1)]
public sealed record AudioCue([property: Field(1)] double Time, [property: Field(2)] AudioCueKind Kind, [property: Field(3)] string? Name = null,
    [property: Field(4)] double Volume = 1, [property: Field(5)] double Pan = 0, [property: Field(6)] double Pitch = 1,
    [property: Field(7)] string? Marker = null) : ISequenceCue;

/// <summary>What a UI cue does to its element.</summary>
public enum UiCueAction { SetText, AddClass, RemoveClass, SetAttribute, RemoveAttribute }

/// <summary>
/// A UI cue (<c>scene.ui-cue</c>, M10-9): on the element with the id <see cref="Element"/> within the
/// bound host (null: the host itself) it sets the text (<see cref="Value"/>), adds or removes the class
/// <see cref="Name"/>, or sets (to <see cref="Value"/>) or removes the attribute <see cref="Name"/>.
/// </summary>
[Record("scene.ui-cue", 1)]
public sealed record UiCue([property: Field(1)] double Time, [property: Field(2)] UiCueAction Action, [property: Field(3)] string? Element = null,
    [property: Field(4)] string? Name = null, [property: Field(5)] string? Value = null, [property: Field(6)] string? Marker = null) : ISequenceCue;

/// <summary>
/// A track of a sequence (record <c>scene.track</c>, a union of the track kinds, M9 Q12). Each kind
/// checks its names and numbers (<see cref="Sequence.Validate"/>) and makes its own playback (one class
/// per kind behind one interface, Docs/Scene.md, "The player").
/// </summary>
[Record("scene.track", 1)]
[Subtype(1, typeof(ClipTrack))]
[Subtype(2, typeof(TransformTrack))]
[Subtype(3, typeof(VisibilityTrack))]
[Subtype(4, typeof(ActorTrack))]
[Subtype(5, typeof(PaletteTrack))]
[Subtype(6, typeof(EventTrack))]
[Subtype(7, typeof(CameraTrack))]
[Subtype(8, typeof(LightTrack))]
[Subtype(9, typeof(ParticleTrack))]
[Subtype(10, typeof(AudioTrack))]
[Subtype(11, typeof(UiTrack))]
public abstract partial record SequenceTrack
{
    /// <summary>The kind's name in messages.</summary>
    internal abstract string Kind { get; }

    /// <summary>Checks the track's names and numbers (<see cref="SequenceChecks"/> throws naming the problem).</summary>
    internal abstract void Validate(SequenceChecks checks);

    /// <summary>The track's playback, its names resolved and checked against the bindings (throws, changing nothing).</summary>
    internal abstract TrackPlayback CreatePlayback(PlaybackContext context);
}

/// <summary>A clip track (<c>scene.clip-track</c>): one of a stage's clips on that stage's nodes, as an NLA strip (Q13).</summary>
[Record("scene.clip-track", 1)]
public sealed partial record ClipTrack : SequenceTrack
{
    /// <summary>The stage's name (its path's last name).</summary>
    [Field(1)] public required string Stage { get; init; }
    [Field(2)] public required SequenceStrip Strip { get; init; }

    internal override string Kind => "clip";

    internal override void Validate(SequenceChecks checks)
    {
        checks.Stage(Stage);
        checks.Strip(Strip, "its strip");
        checks.StageClip(Stage, Strip.Clip);
    }
}

/// <summary>
/// A transform track (<c>scene.transform-track</c>): curves for a node's location, rotation (Euler
/// angles in radians, XYZ: about X, then Y, then Z, Q8) and scale, per component; at least one. A
/// channel with a curve for any component is the track's (its other components the node's values
/// when the sequence started); the later track in track order sets the channels it animates (S4-4).
/// </summary>
[Record("scene.transform-track", 1)]
public sealed partial record TransformTrack : SequenceTrack
{
    [Field(1)] public required string Node { get; init; }
    [Field(2)] public FCurve? LocationX { get; init; }
    [Field(3)] public FCurve? LocationY { get; init; }
    [Field(4)] public FCurve? LocationZ { get; init; }
    [Field(5)] public FCurve? RotationX { get; init; }
    [Field(6)] public FCurve? RotationY { get; init; }
    [Field(7)] public FCurve? RotationZ { get; init; }
    [Field(8)] public FCurve? ScaleX { get; init; }
    [Field(9)] public FCurve? ScaleY { get; init; }
    [Field(10)] public FCurve? ScaleZ { get; init; }

    internal override string Kind => "transform";

    /// <summary>The nine curves: location, rotation, scale, each x, y, z.</summary>
    internal FCurve?[] Curves => [LocationX, LocationY, LocationZ, RotationX, RotationY, RotationZ, ScaleX, ScaleY, ScaleZ];

    internal override void Validate(SequenceChecks checks)
    {
        checks.Node(Node, "its node");
        if (Curves.All(c => c == null))
            checks.Fail("it has no curve");
    }
}

/// <summary>A visibility track (<c>scene.visibility-track</c>, Q23): a node shown or hidden at cues.</summary>
[Record("scene.visibility-track", 1)]
public sealed partial record VisibilityTrack : SequenceTrack
{
    [Field(1)] public required string Node { get; init; }
    [Field(2)] public IReadOnlyList<VisibilityCue> Cues { get; init; } = [];

    internal override string Kind => "visibility";

    internal override void Validate(SequenceChecks checks)
    {
        checks.Node(Node, "its node");
        checks.Cues(Cues, (_, _) => { });
    }
}

/// <summary>
/// The animation track (<c>scene.animation-track</c>, Q16): a bound figure, the node it stands on
/// (<see cref="Mark"/>; null: it stays on its own), its clips as NLA strips on one layer on top of its
/// stack (in time order, not overlapping) and cues for its state machine's parameters and triggers.
/// </summary>
[Record("scene.animation-track", 1)]
public sealed partial record ActorTrack : SequenceTrack
{
    /// <summary>The actor role.</summary>
    [Field(1)] public required string Actor { get; init; }
    [Field(2)] public string? Mark { get; init; }
    [Field(3)] public IReadOnlyList<SequenceStrip> Strips { get; init; } = [];
    [Field(4)] public IReadOnlyList<ParameterCue> Cues { get; init; } = [];

    internal override string Kind => "animation";

    internal override void Validate(SequenceChecks checks)
    {
        checks.Role(Actor, "its actor", RoleKind.Actor);
        if (Mark != null)
            checks.Node(Mark, "its mark");
        for (var i = 0; i < Strips.Count; i++)
            checks.Strip(Strips[i] ?? throw checks.Problem($"strip {i} is null"), $"strip {i}");
        checks.Cues(Cues, (cue, where) =>
        {
            if (string.IsNullOrEmpty(cue.Parameter))
                checks.Fail($"{where} names no parameter");
            if (!Enum.IsDefined(cue.Kind))
                checks.Fail($"{where} has a kind that isn't one");
            if (!double.IsFinite(cue.Value))
                checks.Fail($"{where}'s value must be a finite number");
            if (cue.Kind == ParameterKind.Trigger && cue.Value != 1)
                checks.Fail($"{where}: a trigger cue's value must be 1");
            if (cue.Kind == ParameterKind.Bool && cue.Value is not (0 or 1))
                checks.Fail($"{where}: a bool cue's value must be 0 or 1");
            if (cue.Kind == ParameterKind.Int && cue.Value != Math.Round(cue.Value))
                checks.Fail($"{where}: an int cue's value must be a whole number");
        });
    }
}

/// <summary>
/// A palette track (<c>scene.palette-track</c>, Q24): what an actor role, a model role or a stage
/// (<see cref="Target"/>) is coloured in at cues (S4-3).
/// </summary>
[Record("scene.palette-track", 1)]
public sealed partial record PaletteTrack : SequenceTrack
{
    [Field(1)] public required string Target { get; init; }
    [Field(2)] public IReadOnlyList<PaletteCue> Cues { get; init; } = [];

    internal override string Kind => "palette";

    internal override void Validate(SequenceChecks checks)
    {
        checks.PaletteTarget(Target);
        checks.Cues(Cues, (cue, where) =>
        {
            if (cue.Palette != null && cue.Fill != null)
                checks.Fail($"{where} names a palette and a fill: one or neither");
            if (cue.Palette != null)
                checks.Role(cue.Palette, $"{where}'s palette", RoleKind.Palette);
            if (cue.Fill != null && !SequenceChecks.IsColour(cue.Fill))
                checks.Fail($"{where}'s fill {cue.Fill} isn't a colour (#rrggbb)");
        });
    }
}

/// <summary>An event track (<c>scene.event-track</c>): cues delivered to game code (Q17, Q18).</summary>
[Record("scene.event-track", 1)]
public sealed partial record EventTrack : SequenceTrack
{
    [Field(1)] public IReadOnlyList<EventCue> Cues { get; init; } = [];

    internal override string Kind => "event";

    internal override void Validate(SequenceChecks checks) => checks.Cues(Cues, (cue, where) =>
    {
        if (string.IsNullOrEmpty(cue.Name))
            checks.Fail($"{where} has no name");
    });
}

/// <summary>A camera track (<c>scene.camera-track</c>, Q11): cuts to camera nodes and a field-of-view curve (radians; null: the camera's own).</summary>
[Record("scene.camera-track", 1)]
public sealed partial record CameraTrack : SequenceTrack
{
    [Field(1)] public IReadOnlyList<CameraCut> Cuts { get; init; } = [];
    [Field(2)] public FCurve? FieldOfView { get; init; }

    internal override string Kind => "camera";

    internal override void Validate(SequenceChecks checks) => checks.Cues(Cuts, (cut, where) => checks.Node(cut.Camera, $"{where}'s camera"));
}

/// <summary>A light track (<c>scene.light-track</c>, Q12): the view's sun from sun nodes at cues, and curves for its strength and angle (degrees; null: the sun's own).</summary>
[Record("scene.light-track", 1)]
public sealed partial record LightTrack : SequenceTrack
{
    [Field(1)] public IReadOnlyList<LightCue> Cues { get; init; } = [];
    [Field(2)] public FCurve? Strength { get; init; }
    [Field(3)] public FCurve? Angle { get; init; }

    internal override string Kind => "light";

    internal override void Validate(SequenceChecks checks) => checks.Cues(Cues, (cue, where) => checks.Node(cue.Sun, $"{where}'s sun"));
}

/// <summary>A particle track (<c>scene.particle-track</c>, Q25, Q26): emitters started on nodes at cues.</summary>
[Record("scene.particle-track", 1)]
public sealed partial record ParticleTrack : SequenceTrack
{
    [Field(1)] public IReadOnlyList<ParticleCue> Cues { get; init; } = [];

    internal override string Kind => "particles";

    internal override void Validate(SequenceChecks checks) => checks.Cues(Cues, (cue, where) =>
    {
        if (cue.Effect is not { } effect || !effect.StartsWith("models/effects/", StringComparison.Ordinal) || !PackPath.IsValid(effect))
            checks.Fail($"{where}'s effect {cue.Effect} isn't a path under models/effects/");
        checks.Node(cue.Node, $"{where}'s node");
        if (cue.Burst < 0 || !double.IsFinite(cue.Rate) || cue.Rate < 0 || !double.IsFinite(cue.Duration) || cue.Duration < 0)
            checks.Fail($"{where}: its burst, rate and duration must be 0 or more");
        if (cue.Palette != null)
            checks.Role(cue.Palette, $"{where}'s palette", RoleKind.Palette);
    });
}

/// <summary>An audio track (<c>scene.audio-track</c>, M10-8): sounds and music cues scheduled on the audio clock (step 6).</summary>
[Record("scene.audio-track", 1)]
public sealed partial record AudioTrack : SequenceTrack
{
    [Field(1)] public IReadOnlyList<AudioCue> Cues { get; init; } = [];

    internal override string Kind => "audio";

    internal override void Validate(SequenceChecks checks) => checks.Cues(Cues, (cue, where) =>
    {
        if (!Enum.IsDefined(cue.Kind))
            checks.Fail($"{where} has a kind that isn't one");
        if (cue.Kind == AudioCueKind.StopMusic ? cue.Name != null : string.IsNullOrEmpty(cue.Name))
            checks.Fail(cue.Kind == AudioCueKind.StopMusic ? $"{where} stops the music but names {cue.Name}" : $"{where} names no {(cue.Kind == AudioCueKind.Music ? "theme" : "effect")}");
        if (!double.IsFinite(cue.Volume) || cue.Volume < 0 || !double.IsFinite(cue.Pan) || cue.Pan is < -1 or > 1 || !double.IsFinite(cue.Pitch) || cue.Pitch <= 0)
            checks.Fail($"{where}: its volume must be 0 or more, its pan from −1 to 1 and its pitch above 0");
    });
}

/// <summary>A UI track (<c>scene.ui-track</c>, M10-9): text, classes and attributes set at cues on elements of a bound host (step 6).</summary>
[Record("scene.ui-track", 1)]
public sealed partial record UiTrack : SequenceTrack
{
    /// <summary>The element role whose elements the cues name.</summary>
    [Field(1)] public required string Host { get; init; }
    [Field(2)] public IReadOnlyList<UiCue> Cues { get; init; } = [];

    internal override string Kind => "ui";

    internal override void Validate(SequenceChecks checks)
    {
        checks.Role(Host, "its host", RoleKind.Element);
        checks.Cues(Cues, (cue, where) =>
        {
            if (!Enum.IsDefined(cue.Action))
                checks.Fail($"{where} has an action that isn't one");
            if (cue.Element is "")
                checks.Fail($"{where} names an empty element id");
            var (name, value) = cue.Action switch
            {
                UiCueAction.SetText => (false, true),
                UiCueAction.AddClass or UiCueAction.RemoveClass or UiCueAction.RemoveAttribute => (true, false),
                _ => (true, true),
            };
            if (name != !string.IsNullOrEmpty(cue.Name) || value != (cue.Value != null))
                checks.Fail($"{where}: {cue.Action} takes {(name ? "a name" : "no name")} and {(value ? "a value" : "no value")}");
        });
    }
}

/// <summary>
/// A sequence (record <c>scene.sequence</c>, Q14, S4-1): a cutscene's tracks on a timeline of
/// <see cref="Duration"/> seconds, the roles the game binds (Q15), the stages it brings (models at
/// <c>models/stages/&lt;name&gt;</c>, instanced under the scene's root when it plays), whether it may be
/// skipped and whether the game holds its AI while it plays (Q20), and its reduced variant, a cut list
/// played when the system asks for reduced motion (null: none, Q19). In the game's data a record entry
/// at <c>world/cutscenes/&lt;name&gt;</c> (M10-10), packed from its JSON form.
/// </summary>
[Record("scene.sequence", 1)]
public sealed record Sequence
{
    [Field(1)] public required string Name { get; init; }
    /// <summary>Seconds; the end state is every track at this time (Q3).</summary>
    [Field(2)] public required double Duration { get; init; }
    /// <summary>False: <see cref="SequencePlayer.Skip"/> refuses; <see cref="SequencePlayer.Finish"/> still ends it (Q20).</summary>
    [Field(3)] public bool Skippable { get; init; } = true;
    /// <summary>A flag for the game, which holds its AI while a blocking sequence plays (Q20); a vignette is not blocking.</summary>
    [Field(4)] public bool Blocking { get; init; } = true;
    [Field(5)] public IReadOnlyList<SequenceRole> Roles { get; init; } = [];
    /// <summary>Paths of stage models (<c>models/stages/&lt;name&gt;</c>); a stage's name is its path's last name.</summary>
    [Field(6)] public IReadOnlyList<string> Stages { get; init; } = [];
    /// <summary>In track order: state tracks apply in this order each frame (Q17).</summary>
    [Field(7)] public IReadOnlyList<SequenceTrack> Tracks { get; init; } = [];
    /// <summary>The reduced variant's cut list (Q19): spans in time order; null when the sequence has none.</summary>
    [Field(8)] public IReadOnlyList<SequenceSpan>? Reduced { get; init; }

    /// <summary>A stage's name: its path's last name.</summary>
    public static string StageName(string path) => path[(path.LastIndexOf('/') + 1)..];

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> naming the problem when the record breaks a rule it
    /// can be checked for alone: its name and duration, roles and stages named once, every track's names
    /// resolving to roles of the right kind or to its stages, its numbers, cue times within the
    /// timeline, markers named as <c>&lt;stage&gt;/&lt;marker&gt;</c>, the cut list in order inside it.
    /// </summary>
    public void Validate() => new SequenceChecks(this, null).Run();

    /// <summary>
    /// <see cref="Validate"/>, and the names in its stages (Q15, S4-2): the nodes a track names, the
    /// clips its clip tracks play and the markers its cues sit at exist in the stage models
    /// (<paramref name="stages"/>, by path), and cues at markers fall within the timeline.
    /// </summary>
    public void Validate(IReadOnlyDictionary<string, ModelData> stages) => new SequenceChecks(this, stages).Run();

    /// <summary>
    /// The sequence at <paramref name="path"/> in the game's data (a record entry, or its JSON form in the
    /// development folder), checked by itself and against its stages, which are read from the same data
    /// (<see cref="ModelData.Load(VirtualFileSystem, string)"/>): a <see cref="DataFileException"/> naming
    /// the pack and entry or the loose file when it isn't a readable sequence record (a curve breaking its
    /// rules included), breaks a rule, or names a stage the data doesn't have (a broken stage's own error
    /// names the stage's entry).
    /// </summary>
    public static Sequence Load(VirtualFileSystem files, string path)
    {
        Sequence sequence;
        try
        {
            sequence = files.Load<Sequence>(path);
            sequence.Validate();
        }
        catch (ArgumentException problem)
        {
            throw files.Problem(path, $"not a valid sequence: {problem.Message}", problem);
        }
        var stages = new Dictionary<string, ModelData>();
        foreach (var stage in sequence.Stages)
        {
            try
            {
                stages[stage] = ModelData.Load(files, stage);
            }
            catch (FileNotFoundException missing)
            {
                throw files.Problem(path, $"not a valid sequence: its stage {stage} isn't in the game's data", missing);
            }
        }
        try
        {
            sequence.Validate(stages);
        }
        catch (ArgumentException problem)
        {
            throw files.Problem(path, $"not a valid sequence: {problem.Message}", problem);
        }
        return sequence;
    }
}

/// <summary>
/// The checks of a sequence (<see cref="Sequence.Validate()"/>), with its stages' models when given:
/// each problem an <see cref="ArgumentException"/> naming the sequence, the track and the cue.
/// </summary>
internal sealed class SequenceChecks
{
    readonly Sequence _sequence;
    readonly Dictionary<string, RoleKind> _roles = [];
    readonly Dictionary<string, string> _stagePaths = [];
    readonly SequenceTimes? _times;
    string _where = "";

    public SequenceChecks(Sequence sequence, IReadOnlyDictionary<string, ModelData>? stages)
    {
        _sequence = sequence;
        Stages = stages;
        _times = stages == null ? null : new SequenceTimes(sequence, stages, this);
    }

    /// <summary>The stage models by path, or null when the sequence is checked alone.</summary>
    public IReadOnlyDictionary<string, ModelData>? Stages { get; }

    public void Run()
    {
        var s = _sequence;
        _where = $"the sequence {s.Name}";
        if (string.IsNullOrEmpty(s.Name))
            throw new ArgumentException("a sequence needs a name");
        if (!double.IsFinite(s.Duration) || s.Duration <= 0)
            Fail($"its duration ({s.Duration:R} s) must be a finite number above 0");
        foreach (var role in s.Roles)
        {
            if (role == null)
                Fail("a role is null");
            if (string.IsNullOrEmpty(role!.Name) || role.Name.Contains('/'))
                Fail($"the role \"{role.Name}\" needs a name without /");
            if (!Enum.IsDefined(role.Kind))
                Fail($"the role {role.Name} has a kind that isn't one");
            if (!_roles.TryAdd(role.Name, role.Kind))
                Fail($"the role {role.Name} is defined twice");
        }
        foreach (var path in s.Stages)
        {
            if (path == null || !path.StartsWith("models/stages/", StringComparison.Ordinal) || !PackPath.IsValid(path) || path.Count(c => c == '/') != 2)
                Fail($"the stage {path} isn't a path models/stages/<name>");
            var name = Sequence.StageName(path!);
            if (_roles.ContainsKey(name))
                Fail($"the stage {path} has the name of the role {name}");
            if (!_stagePaths.TryAdd(name, path!))
                Fail($"the stage {name} is named twice");
            if (Stages != null && !Stages.ContainsKey(path!))
                Fail($"its stage {path} isn't given");
        }
        for (var t = 0; t < s.Tracks.Count; t++)
        {
            var track = s.Tracks[t];
            _where = $"the sequence {s.Name}, track {t}";
            if (track == null)
                Fail("it is null");
            _where = $"the sequence {s.Name}, track {t} ({track!.Kind})";
            track.Validate(this);
        }
        _where = $"the sequence {s.Name}";
        foreach (var marked in s.Tracks.OfType<ActorTrack>().Where(t => t.Mark != null).GroupBy(t => t.Actor))
            if (marked.Count() > 1)
                Fail($"the actor {marked.Key} has a mark on {marked.Count()} animation tracks (one at most: it stands on one node)");
        Spans(s.Reduced);
    }

    void Spans(IReadOnlyList<SequenceSpan>? spans)
    {
        if (spans == null)
            return;
        if (spans.Count == 0)
            Fail("its reduced variant has no spans (null: none)");
        var end = 0.0;
        for (var i = 0; i < spans.Count; i++)
        {
            var span = spans[i] ?? throw Problem($"reduced span {i} is null");
            if (!double.IsFinite(span.From) || !double.IsFinite(span.To) || !double.IsFinite(span.Hold))
                Fail($"reduced span {i}: its times must be finite numbers");
            if (span.From < end || span.To < span.From || span.To > _sequence.Duration || span.Hold < 0)
                Fail($"reduced span {i} ({span.From:R}–{span.To:R} s, held {span.Hold:R} s) must lie in the timeline after the span before, its end not before its start, its hold not negative");
            if (!(span.Length > 0))
                Fail($"reduced span {i} plays for no time");
            end = span.To;
        }
    }

    public ArgumentException Problem(string message) => new($"{_where}: {message}");

    public void Fail(string message) => throw Problem(message);

    /// <summary>A role of one of the kinds.</summary>
    public void Role(string name, string what, params RoleKind[] kinds)
    {
        if (name == null || !_roles.TryGetValue(name, out var kind))
            Fail($"{what} {name} isn't a role");
        else if (!kinds.Contains(kind))
            Fail($"{what} {name} is a role of kind {kind.ToString().ToLowerInvariant()}, not {string.Join(" or ", kinds.Select(k => k.ToString().ToLowerInvariant()))}");
    }

    /// <summary>One of the sequence's stages, by name.</summary>
    public void Stage(string name)
    {
        if (name == null || !_stagePaths.ContainsKey(name))
            Fail($"{name} isn't one of its stages");
    }

    /// <summary>The model of a stage (by name), when the stages are given.</summary>
    public ModelData? StageModel(string name) => Stages?[_stagePaths[name]];

    /// <summary>
    /// A node: a node role's name, or <c>&lt;stage&gt;/&lt;Blender name&gt;</c>, which with the stages given
    /// must be a node of that stage's model its scene reaches.
    /// </summary>
    public void Node(string reference, string what)
    {
        if (reference == null)
        {
            Fail($"{what} is null");
            return;
        }
        var slash = reference.IndexOf('/');
        if (slash < 0)
        {
            Role(reference, what, RoleKind.Node);
            return;
        }
        var (stage, name) = (reference[..slash], reference[(slash + 1)..]);
        if (!_stagePaths.ContainsKey(stage) || name.Length == 0)
            Fail($"{what} {reference} isn't a node role or <stage>/<node> of one of its stages");
        if (StageModel(stage) is { } model && StageNodeIndex(model, name) < 0)
            Fail($"{what} {reference}: the stage {_stagePaths[stage]} has no node {name}");
    }

    /// <summary>The node of this name in a stage model (a joint first, as <see cref="Skeleton.Find"/>), or −1 when there is none or the model's scene doesn't reach it.</summary>
    public static int StageNodeIndex(ModelData model, string name)
    {
        var index = new Skeleton(model).Find(name);
        if (index < 0)
            return -1;
        var reached = new Stack<int>(model.Roots);
        while (reached.Count > 0)
        {
            var node = reached.Pop();
            if (node == index)
                return index;
            foreach (var child in model.Nodes[node].Children)
                reached.Push(child);
        }
        return -1;
    }

    /// <summary>A clip of a stage's model (with the stages given).</summary>
    public void StageClip(string stage, string clip)
    {
        if (StageModel(stage) is { } model && model.Animations.All(a => a.Name != clip))
            Fail($"the stage {_stagePaths[stage]} has no clip {clip}");
    }

    /// <summary>What a palette track colours: an actor or model role, or a stage.</summary>
    public void PaletteTarget(string target)
    {
        if (target != null && _stagePaths.ContainsKey(target))
            return;
        Role(target!, "its target", RoleKind.Actor, RoleKind.Model);
    }

    public void Strip(SequenceStrip strip, string what)
    {
        if (strip == null)
            Fail($"{what} is null");
        if (string.IsNullOrEmpty(strip!.Clip))
            Fail($"{what} names no clip");
        if (!double.IsFinite(strip.Start) || !double.IsFinite(strip.ClipStart) || strip.ClipEnd is { } e && !double.IsFinite(e)
            || !double.IsFinite(strip.Scale) || double.IsNaN(strip.Repeat) || !double.IsFinite(strip.BlendIn) || !double.IsFinite(strip.BlendOut))
            Fail($"{what} ({strip.Clip}): its numbers must be finite (its repeat may be infinite)");
        if (strip.ClipStart < 0 || strip.ClipEnd < strip.ClipStart)
            Fail($"{what} ({strip.Clip}): its part ({strip.ClipStart:R}–{strip.ClipEnd:R} s) must start at 0 or later and not end before it starts");
        if (!(strip.Scale > 0) || !(strip.Repeat > 0) || strip.BlendIn < 0 || strip.BlendOut < 0)
            Fail($"{what} ({strip.Clip}): its scale and repeat must be above 0, its blends not negative");
        if (strip.Influence is { } influence && !(influence >= 0 && influence <= 1))
            Fail($"{what} ({strip.Clip}): its influence must be from 0 to 1");
        if (!Enum.IsDefined(strip.Mode) || !Enum.IsDefined(strip.Extrapolation))
            Fail($"{what} ({strip.Clip}): a blend mode or extrapolation that isn't one");
    }

    /// <summary>Each cue's time (or marker, S4-2), then <paramref name="check"/> on it.</summary>
    public void Cues<T>(IReadOnlyList<T> cues, Action<T, string> check) where T : ISequenceCue
    {
        for (var i = 0; i < cues.Count; i++)
        {
            var cue = cues[i];
            var where = $"cue {i}";
            if (cue == null)
                Fail($"{where} is null");
            Time(cue!, where);
            check(cue!, where);
        }
    }

    /// <summary>A cue's time: within the timeline, or an offset from a marker <c>&lt;stage&gt;/&lt;marker&gt;</c> (resolved and checked with the stages given).</summary>
    public void Time(ISequenceCue cue, string where)
    {
        if (!double.IsFinite(cue.Time))
            Fail($"{where}: its time must be a finite number");
        if (cue.Marker is not { } marker)
        {
            if (cue.Time < 0 || cue.Time > _sequence.Duration)
                Fail($"{where} at {cue.Time:R} s is outside the timeline (0–{_sequence.Duration:R} s)");
            return;
        }
        var slash = marker.IndexOf('/');
        if (slash <= 0 || slash == marker.Length - 1 || !_stagePaths.ContainsKey(marker[..slash]))
            Fail($"{where}'s marker {marker} isn't <stage>/<marker> of one of its stages");
        if (_times != null)
        {
            var time = _times.Resolve(cue.Time, marker, where);
            if (time < 0 || time > _sequence.Duration)
                Fail($"{where} at {cue.Time:R} s from the marker {marker} falls at {time:R} s, outside the timeline (0–{_sequence.Duration:R} s)");
        }
    }

    /// <summary>Whether <paramref name="text"/> is "#rrggbb".</summary>
    public static bool IsColour(string text) => text.Length == 7 && text[0] == '#' && text.Skip(1).All(char.IsAsciiHexDigit);

    /// <summary>The stage's path by its name.</summary>
    public string StagePath(string name) => _stagePaths[name];
}

/// <summary>
/// Cue times on the timeline (S4-2): a cue's own time, or a stage marker's time plus the cue's offset.
/// A stage's markers are the events of the clip its first clip track plays (the Blender scene's
/// custom property <c>events</c>, Docs/Scene.md, "How it works", step 4), placed on the timeline by
/// that track's strip (its start, part and scale; reversed, counted back from the part's end).
/// </summary>
internal sealed class SequenceTimes(Sequence sequence, IReadOnlyDictionary<string, ModelData> stages, SequenceChecks? checks = null)
{
    public double Resolve(double time, string? marker, string where = "a cue")
    {
        if (marker == null)
            return time;
        var slash = marker.IndexOf('/');
        var (stage, name) = (marker[..slash], marker[(slash + 1)..]);
        var path = sequence.Stages.First(p => Sequence.StageName(p) == stage);
        var track = sequence.Tracks.OfType<ClipTrack>().FirstOrDefault(c => c.Stage == stage);
        if (track == null)
            throw Problem($"{where}'s marker {marker}: the stage {path} has no clip track, so no markers on the timeline");
        var model = stages[path];
        var animation = model.Animations.FirstOrDefault(a => a.Name == track.Strip.Clip)
            ?? throw Problem($"the stage {path} has no clip {track.Strip.Clip}");
        var at = animation.Events.FirstOrDefault(e => e.Name == name)
            ?? throw Problem($"{where}'s marker {marker}: the stage {path}'s clip {animation.Name} has no marker {name}");
        var strip = track.Strip;
        var end = strip.ClipEnd ?? AnimationClip.From(model, animation).Duration;
        var offset = strip.Reversed ? end - at.Time : at.Time - strip.ClipStart;
        return strip.Start + offset * strip.Scale + time;
    }

    ArgumentException Problem(string message) => checks?.Problem(message) ?? new ArgumentException($"the sequence {sequence.Name}: {message}");
}
