using Statventure.Animation;
using Statventure.Audio;
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
// Marks for tools (M11, Docs/Editor.md, step 6): help texts, units, the limits the checks already hold
// (G4), the references to stages, particle effects, effect patches and themes, the palette cue's colour; the sequence's checks
// report the path of what they name (SequenceChecks).

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
[Help("A name the game fills in when it plays the cutscene, such as the boss's figure or the card on the screen.")]
public sealed record SequenceRole(
    [property: Field(1), Help("The name the tracks use for it.")] string Name,
    [property: Field(2), Help("What the game puts there: a figure (actor), a scene node, a model, a screen element or a palette.")] RoleKind Kind);

/// <summary>
/// A span of the reduced variant's cut list (record <c>scene.span</c>, Q19): the timeline from
/// <see cref="From"/> to <see cref="To"/> played at its own length, then <see cref="To"/> held for
/// <see cref="Hold"/> seconds (a span whose ends are equal holds that moment).
/// </summary>
[Record("scene.span", 1)]
[Help("A part of the timeline the shorter version plays, then holds at its end.")]
public sealed record SequenceSpan(
    [property: Field(1), Help("Where the part starts on the timeline."), Unit("s"), Limits(0, double.PositiveInfinity)] double From,
    [property: Field(2), Help("Where the part ends on the timeline (the same as its start: only that moment, held)."), Unit("s"), Limits(0, double.PositiveInfinity)] double To,
    [property: Field(3), Help("How long the part's end is held after it plays."), Unit("s"), Limits(0, double.PositiveInfinity)] double Hold = 0)
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
[Help("A clip placed on the timeline, as a strip in Blender's NLA editor.")]
public sealed record SequenceStrip(
    [property: Field(1), Help("The clip's name, as it is called in its model.")] string Clip,
    [property: Field(2), Help("When the clip starts on the cutscene's timeline."), Unit("s")] double Start = 0,
    [property: Field(3), Help("Where in the clip it starts playing."), Unit("s"), Limits(0, double.PositiveInfinity)] double ClipStart = 0,
    [property: Field(4), Help("Where in the clip it stops playing (not set: the clip's end)."), Unit("s"), Limits(0, double.PositiveInfinity)] double? ClipEnd = null,
    [property: Field(5), Help("How much longer the clip takes to play: 2 plays it at half speed."), Limits(0, double.PositiveInfinity, MinExclusive = true)] double Scale = 1,
    [property: Field(6), Help("How many times the clip plays in a row; infinite loops it until the cutscene ends."), Limits(0, double.PositiveInfinity, MinExclusive = true)] double Repeat = 1,
    [property: Field(7), Help("Plays the clip backwards.")] bool Reversed = false,
    [property: Field(8), Help("How long the clip takes to fade in over what is below it."), Unit("s"), Limits(0, double.PositiveInfinity)] double BlendIn = 0,
    [property: Field(9), Help("How long the clip takes to fade out at its end."), Unit("s"), Limits(0, double.PositiveInfinity)] double BlendOut = 0,
    [property: Field(10), Help("How strongly the clip counts, from 0 (not at all) to 1 (fully; also when not set)."), Limits(0, 1)] double? Influence = null,
    [property: Field(11), Help("How the clip mixes with what is below it: replace it, or add to it.")] BlendMode Mode = BlendMode.Replace,
    [property: Field(12), Help("What the clip shows outside its time: nothing, its first and last frames (hold), or its last frame until the next clip (hold forward).")] Extrapolation Extrapolation = Extrapolation.HoldForward)
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

/// <summary>The help texts the cues share (their time and marker).</summary>
static class CueHelp
{
    public const string Time = "When the cue happens on the cutscene's timeline, or, with a marker, how long after the marker (before it when negative).";
    public const string Marker = "A marker of a stage the time counts from, written <stage>/<marker> (not set: the time counts from the start).";
}

/// <summary>A visibility cue (<c>scene.visibility-cue</c>, Q23): the node (with its subtree) shown or hidden from then on.</summary>
[Record("scene.visibility-cue", 1)]
[Help("Shows or hides the node from then on.")]
public sealed record VisibilityCue(
    [property: Field(1), Help(CueHelp.Time), Unit("s")] double Time,
    [property: Field(2), Help("Whether the node, with everything below it, shows from then on.")] bool Shown,
    [property: Field(3), Help(CueHelp.Marker)] string? Marker = null) : ISequenceCue;

/// <summary>
/// A cue for a figure's state machine (<c>scene.parameter-cue</c>, Q16): a bool, int or float parameter
/// set to <see cref="Value"/> from then on (state), or a trigger set when the clock passes it (a moment).
/// </summary>
[Record("scene.parameter-cue", 1)]
[Help("Sets a value of the figure's state machine from then on, or fires one of its triggers.")]
public sealed record ParameterCue(
    [property: Field(1), Help(CueHelp.Time), Unit("s")] double Time,
    [property: Field(2), Help("The state machine's parameter or trigger, by its name.")] string Parameter,
    [property: Field(3), Help("The parameter's kind, as the state machine has it.")] ParameterKind Kind,
    [property: Field(4), Help("The value it gets: a whole number for an int, 0 or 1 for a bool, 1 for a trigger.")] double Value = 1,
    [property: Field(5), Help(CueHelp.Marker)] string? Marker = null) : ISequenceCue;

/// <summary>
/// A palette cue (<c>scene.palette-cue</c>, Q24, S4-3): from then on the target shows the palette bound
/// to the role <see cref="Palette"/>, or <see cref="Fill"/> ("#rrggbb") on every slot of its models
/// (the all-ink silhouette), or with neither its own palette (as it was when the sequence started).
/// </summary>
[Record("scene.palette-cue", 1)]
[Help("Colours the target from then on: in a palette role's colours, all in one colour, or with neither in its own colours again.")]
public sealed record PaletteCue(
    [property: Field(1), Help(CueHelp.Time), Unit("s")] double Time,
    [property: Field(2), Help("A palette role whose colours the target takes.")] string? Palette = null,
    [property: Field(3), Help("One colour for every part of the target (#rrggbb), such as an all-ink silhouette."), Colour] string? Fill = null,
    [property: Field(4), Help(CueHelp.Marker)] string? Marker = null) : ISequenceCue;

/// <summary>An event cue (<c>scene.event-cue</c>): delivered to game code when the clock passes it, or at a skip marked as skipped (Q18).</summary>
[Record("scene.event-cue", 1)]
[Help("A moment the game's code is told about, also when the player skips past it.")]
public sealed record EventCue(
    [property: Field(1), Help(CueHelp.Time), Unit("s")] double Time,
    [property: Field(2), Help("The name the game's code gets.")] string Name,
    [property: Field(3), Help(CueHelp.Marker)] string? Marker = null) : ISequenceCue;

/// <summary>A camera cut (<c>scene.camera-cut</c>, Q11): the view looks through the camera on this node from then on.</summary>
[Record("scene.camera-cut", 1)]
[Help("Cuts to a camera: the view looks through it from then on.")]
public sealed record CameraCut(
    [property: Field(1), Help(CueHelp.Time), Unit("s")] double Time,
    [property: Field(2), Help("The node of the camera: <stage>/<node> for a stage's camera, or a node role.")] string Camera,
    [property: Field(3), Help(CueHelp.Marker)] string? Marker = null) : ISequenceCue;

/// <summary>A light cue (<c>scene.light-cue</c>, Q12): the view's sun is the sun on this node from then on.</summary>
[Record("scene.light-cue", 1)]
[Help("Switches the sun: the scene is lit by it from then on.")]
public sealed record LightCue(
    [property: Field(1), Help(CueHelp.Time), Unit("s")] double Time,
    [property: Field(2), Help("The node of the sun: <stage>/<node> for a stage's sun, or a node role.")] string Sun,
    [property: Field(3), Help(CueHelp.Marker)] string? Marker = null) : ISequenceCue;

/// <summary>
/// A particle cue (<c>scene.particle-cue</c>, Q25, Q26): an emitter of the effect at
/// <see cref="Effect"/> (<c>models/effects/&lt;name&gt;</c>) started on a node, with its seed, burst,
/// rate (particles a second) and duration, coloured by the palette role <see cref="Palette"/> (or none).
/// </summary>
[Record("scene.particle-cue", 1)]
[Help("Starts a particle emitter on a node: a burst at once, then a stream for a while.")]
public sealed record ParticleCue(
    [property: Field(1), Help(CueHelp.Time), Unit("s")] double Time,
    [property: Field(2), Help("The particle effect, by its path models/effects/<name>."), References(typeof(ParticleEffect), Rule = typeof(PathNames))] string Effect,
    [property: Field(3), Help("The node the emitter sits on: <stage>/<node>, or a node role.")] string Node,
    [property: Field(4), Help("Picks each particle's random values: the same seed gives the same particles every time.")] int Seed = 0,
    [property: Field(5), Help("How many particles start at once."), Limits(0, double.PositiveInfinity)] int Burst = 0,
    [property: Field(6), Help("How many particles start each second after the burst."), Unit("/s"), Limits(0, double.PositiveInfinity)] double Rate = 0,
    [property: Field(7), Help("How long the stream of particles lasts."), Unit("s"), Limits(0, double.PositiveInfinity)] double Duration = 0,
    [property: Field(8), Help("The palette role the particles take their colours from (not set: none).")] string? Palette = null,
    [property: Field(9), Help(CueHelp.Marker)] string? Marker = null) : ISequenceCue;

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
[Help("Plays a sound effect, switches the music to a theme, or stops the music.")]
public sealed record AudioCue(
    [property: Field(1), Help(CueHelp.Time), Unit("s")] double Time,
    [property: Field(2), Help("What it does: play a sound effect, switch the music, or stop the music.")] AudioCueKind Kind,
    [property: Field(3), Help("The sound effect's or the music theme's name (none to stop the music)."),
     References(typeof(EffectPatch), When = "Kind", Is = ["Effect"]), References(typeof(ThemeSpec), When = "Kind", Is = ["Music"])] string? Name = null,
    [property: Field(4), Help("The sound effect's volume: 1 plays it as it is made."), Limits(0, double.PositiveInfinity)] double Volume = 1,
    [property: Field(5), Help("Where the sound effect is heard, from −1 (left) through 0 (the middle) to 1 (right)."), Limits(-1, 1)] double Pan = 0,
    [property: Field(6), Help("The sound effect's pitch as a factor: 2 is an octave higher, 0.5 an octave lower."), Limits(0, double.PositiveInfinity, MinExclusive = true)] double Pitch = 1,
    [property: Field(7), Help(CueHelp.Marker)] string? Marker = null) : ISequenceCue;

/// <summary>What a UI cue does to its element.</summary>
public enum UiCueAction { SetText, AddClass, RemoveClass, SetAttribute, RemoveAttribute }

/// <summary>
/// A UI cue (<c>scene.ui-cue</c>, M10-9): on the element with the id <see cref="Element"/> within the
/// bound host (null: the host itself) it sets the text (<see cref="Value"/>), adds or removes the class
/// <see cref="Name"/>, or sets (to <see cref="Value"/>) or removes the attribute <see cref="Name"/>.
/// </summary>
[Record("scene.ui-cue", 1)]
[Help("Changes an element on the screen: its text, a class or an attribute.")]
public sealed record UiCue(
    [property: Field(1), Help(CueHelp.Time), Unit("s")] double Time,
    [property: Field(2), Help("What it does: set the text, add or remove a class, set or remove an attribute.")] UiCueAction Action,
    [property: Field(3), Help("The id of the element inside the host (not set: the host itself).")] string? Element = null,
    [property: Field(4), Help("The class's or the attribute's name (none to set the text).")] string? Name = null,
    [property: Field(5), Help("The text, or the attribute's value (none to add or remove a class or remove an attribute).")] string? Value = null,
    [property: Field(6), Help(CueHelp.Marker)] string? Marker = null) : ISequenceCue;

/// <summary>
/// A track of a sequence (record <c>scene.track</c>, a union of the track kinds, M9 Q12). Each kind
/// checks its names and numbers (<see cref="Sequence.Validate()"/>) and makes its own playback (one class
/// per kind behind one interface, Docs/Scene.md, "The player").
/// </summary>
[Record("scene.track", 1)]
[Kind("Track")]
[Help("One kind of thing that happens on the timeline.")]
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

    /// <summary>Checks the track's names and numbers, each problem reported by <see cref="SequenceChecks"/> at its path.</summary>
    internal abstract void Validate(SequenceChecks checks);

    /// <summary>The track's playback, its names resolved and checked against the bindings (throws, changing nothing).</summary>
    internal abstract TrackPlayback CreatePlayback(PlaybackContext context);
}

/// <summary>A clip track (<c>scene.clip-track</c>): one of a stage's clips on that stage's nodes, as an NLA strip (Q13).</summary>
[Record("scene.clip-track", 1)]
[Help("Plays one of a stage's own clips (its animation from Blender) on the stage.")]
public sealed partial record ClipTrack : SequenceTrack
{
    /// <summary>The stage's name (its path's last name).</summary>
    [Field(1), Help("The stage, by its name (the last part of its path).")] public required string Stage { get; init; }
    [Field(2), Help("The clip and where it plays on the timeline.")] public required SequenceStrip Strip { get; init; }

    internal override string Kind => "clip";

    internal override void Validate(SequenceChecks checks)
    {
        checks.Stage(Stage, SequenceChecks.F("stage"));
        checks.Strip(Strip, "its strip", SequenceChecks.F("strip"));
        if (!string.IsNullOrEmpty(Strip?.Clip))
            checks.StageClip(Stage, Strip.Clip, SequenceChecks.F("strip").Field("clip"));
    }
}

/// <summary>
/// A transform track (<c>scene.transform-track</c>): curves for a node's location, rotation (Euler
/// angles in radians, XYZ: about X, then Y, then Z, Q8) and scale, per component; at least one. A
/// channel with a curve for any component is the track's (its other components the node's values
/// when the sequence started); the later track in track order sets the channels it animates (S4-4).
/// </summary>
[Record("scene.transform-track", 1)]
[Help("Moves, turns or scales a node over time with curves. A part without a curve keeps the node's own value.")]
public sealed partial record TransformTrack : SequenceTrack
{
    [Field(1), Help("The node it moves: <stage>/<node> for a stage's node, or a node role.")] public required string Node { get; init; }
    [Field(2), Help("The node's location along X over time."), Unit("m")] public FCurve? LocationX { get; init; }
    [Field(3), Help("The node's location along Y over time."), Unit("m")] public FCurve? LocationY { get; init; }
    [Field(4), Help("The node's location along Z over time."), Unit("m")] public FCurve? LocationZ { get; init; }
    [Field(5), Help("The node's turn about X over time (turned about X first, then Y, then Z)."), Unit("rad")] public FCurve? RotationX { get; init; }
    [Field(6), Help("The node's turn about Y over time."), Unit("rad")] public FCurve? RotationY { get; init; }
    [Field(7), Help("The node's turn about Z over time."), Unit("rad")] public FCurve? RotationZ { get; init; }
    [Field(8), Help("The node's scale along X over time (1: its size).")] public FCurve? ScaleX { get; init; }
    [Field(9), Help("The node's scale along Y over time.")] public FCurve? ScaleY { get; init; }
    [Field(10), Help("The node's scale along Z over time.")] public FCurve? ScaleZ { get; init; }

    internal override string Kind => "transform";

    /// <summary>The nine curves: location, rotation, scale, each x, y, z.</summary>
    internal FCurve?[] Curves => [LocationX, LocationY, LocationZ, RotationX, RotationY, RotationZ, ScaleX, ScaleY, ScaleZ];

    internal override void Validate(SequenceChecks checks)
    {
        checks.Node(Node, "its node", SequenceChecks.F("node"));
        if (Curves.All(c => c == null))
            checks.Report("it has no curve");
    }
}

/// <summary>A visibility track (<c>scene.visibility-track</c>, Q23): a node shown or hidden at cues.</summary>
[Record("scene.visibility-track", 1)]
[Help("Shows and hides a node, with everything below it.")]
public sealed partial record VisibilityTrack : SequenceTrack
{
    [Field(1), Help("The node: <stage>/<node> for a stage's node, or a node role.")] public required string Node { get; init; }
    [Field(2), Help("When it shows and hides.")] public IReadOnlyList<VisibilityCue> Cues { get; init; } = [];

    internal override string Kind => "visibility";

    internal override void Validate(SequenceChecks checks)
    {
        checks.Node(Node, "its node", SequenceChecks.F("node"));
        checks.Cues(Cues, "cues", (_, _) => { });
    }
}

/// <summary>
/// The animation track (<c>scene.animation-track</c>, Q16): a bound figure, the node it stands on
/// (<see cref="Mark"/>; null: it stays on its own), its clips as NLA strips on one layer on top of its
/// stack (in time order, not overlapping) and cues for its state machine's parameters and triggers.
/// </summary>
[Record("scene.animation-track", 1)]
[Kind("Animation track")]
[Help("Animates a figure: where it stands, the clips it plays and the values of its state machine.")]
public sealed partial record ActorTrack : SequenceTrack
{
    /// <summary>The actor role.</summary>
    [Field(1), Help("The figure, by its actor role.")] public required string Actor { get; init; }
    [Field(2), Help("A node the figure stands on while the cutscene plays (not set: it stays where it is).")] public string? Mark { get; init; }
    [Field(3), Help("The clips the figure plays, in time order and not overlapping.")] public IReadOnlyList<SequenceStrip> Strips { get; init; } = [];
    [Field(4), Help("Values for the figure's state machine.")] public IReadOnlyList<ParameterCue> Cues { get; init; } = [];

    internal override string Kind => "animation";

    internal override void Validate(SequenceChecks checks)
    {
        checks.Role(Actor, "its actor", SequenceChecks.F("actor"), RoleKind.Actor);
        if (Mark != null)
            checks.Node(Mark, "its mark", SequenceChecks.F("mark"));
        for (var i = 0; i < Strips.Count; i++)
            checks.Strip(Strips[i], $"strip {i}", SequenceChecks.F("strips").Index(i));
        checks.Cues(Cues, "cues", (cue, where) =>
        {
            if (string.IsNullOrEmpty(cue.Parameter))
                checks.Fail($"{where} names no parameter", SequenceChecks.F("parameter"));
            if (!Enum.IsDefined(cue.Kind))
                checks.Fail($"{where} has a kind that isn't one", SequenceChecks.F("kind"));
            if (!double.IsFinite(cue.Value))
                checks.Fail($"{where}'s value must be a finite number", SequenceChecks.F("value"));
            if (cue.Kind == ParameterKind.Trigger && cue.Value != 1)
                checks.Fail($"{where}: a trigger cue's value must be 1", SequenceChecks.F("value"));
            if (cue.Kind == ParameterKind.Bool && cue.Value is not (0 or 1))
                checks.Fail($"{where}: a bool cue's value must be 0 or 1", SequenceChecks.F("value"));
            if (cue.Kind == ParameterKind.Int && cue.Value != Math.Round(cue.Value))
                checks.Fail($"{where}: an int cue's value must be a whole number", SequenceChecks.F("value"));
        });
    }
}

/// <summary>
/// A palette track (<c>scene.palette-track</c>, Q24): what an actor role, a model role or a stage
/// (<see cref="Target"/>) is coloured in at cues (S4-3).
/// </summary>
[Record("scene.palette-track", 1)]
[Help("Changes the colours of a figure, a model or a stage.")]
public sealed partial record PaletteTrack : SequenceTrack
{
    [Field(1), Help("What it colours: an actor or model role, or a stage by its name.")] public required string Target { get; init; }
    [Field(2), Help("When its colours change.")] public IReadOnlyList<PaletteCue> Cues { get; init; } = [];

    internal override string Kind => "palette";

    internal override void Validate(SequenceChecks checks)
    {
        checks.PaletteTarget(Target, SequenceChecks.F("target"));
        checks.Cues(Cues, "cues", (cue, where) =>
        {
            if (cue.Palette != null && cue.Fill != null)
                checks.Fail($"{where} names a palette and a fill: one or neither");
            if (cue.Palette != null)
                checks.Role(cue.Palette, $"{where}'s palette", SequenceChecks.F("palette"), RoleKind.Palette);
            if (cue.Fill != null && !SequenceChecks.IsColour(cue.Fill))
                checks.Fail($"{where}'s fill {cue.Fill} isn't a colour (#rrggbb)", SequenceChecks.F("fill"));
        });
    }
}

/// <summary>An event track (<c>scene.event-track</c>): cues delivered to game code (Q17, Q18).</summary>
[Record("scene.event-track", 1)]
[Help("Tells the game's code when moments of the cutscene come.")]
public sealed partial record EventTrack : SequenceTrack
{
    [Field(1), Help("The moments, each with its name.")] public IReadOnlyList<EventCue> Cues { get; init; } = [];

    internal override string Kind => "event";

    internal override void Validate(SequenceChecks checks) => checks.Cues(Cues, "cues", (cue, where) =>
    {
        if (string.IsNullOrEmpty(cue.Name))
            checks.Fail($"{where} has no name", SequenceChecks.F("name"));
    });
}

/// <summary>A camera track (<c>scene.camera-track</c>, Q11): cuts to camera nodes and a field-of-view curve (radians; null: the camera's own).</summary>
[Record("scene.camera-track", 1)]
[Help("Cuts between cameras, and can change the camera's field of view over time.")]
public sealed partial record CameraTrack : SequenceTrack
{
    [Field(1), Help("The cuts, each to a camera.")] public IReadOnlyList<CameraCut> Cuts { get; init; } = [];
    [Field(2), Help("How wide the camera sees, from top to bottom, over time (not set: the camera's own)."), Unit("rad")] public FCurve? FieldOfView { get; init; }

    internal override string Kind => "camera";

    internal override void Validate(SequenceChecks checks) => checks.Cues(Cuts, "cuts", (cut, where) => checks.Node(cut.Camera, $"{where}'s camera", SequenceChecks.F("camera")));
}

/// <summary>A light track (<c>scene.light-track</c>, Q12): the view's sun from sun nodes at cues, and curves for its strength and angle (degrees; null: the sun's own).</summary>
[Record("scene.light-track", 1)]
[Help("Switches the sun that lights the scene, and can change its strength and angle over time.")]
public sealed partial record LightTrack : SequenceTrack
{
    [Field(1), Help("When the sun switches, each to a sun.")] public IReadOnlyList<LightCue> Cues { get; init; } = [];
    [Field(2), Help("The sun's strength over time, as Blender's (not set: the sun's own).")] public FCurve? Strength { get; init; }
    [Field(3), Help("How wide the sun is across over time, which softens its shadows' edges (not set: the sun's own)."), Unit("°")] public FCurve? Angle { get; init; }

    internal override string Kind => "light";

    internal override void Validate(SequenceChecks checks) => checks.Cues(Cues, "cues", (cue, where) => checks.Node(cue.Sun, $"{where}'s sun", SequenceChecks.F("sun")));
}

/// <summary>A particle track (<c>scene.particle-track</c>, Q25, Q26): emitters started on nodes at cues.</summary>
[Record("scene.particle-track", 1)]
[Help("Starts particle emitters on nodes.")]
public sealed partial record ParticleTrack : SequenceTrack
{
    [Field(1), Help("The emitters, each started at its time.")] public IReadOnlyList<ParticleCue> Cues { get; init; } = [];

    internal override string Kind => "particles";

    internal override void Validate(SequenceChecks checks) => checks.Cues(Cues, "cues", (cue, where) =>
    {
        if (cue.Effect is not { } effect || !effect.StartsWith("models/effects/", StringComparison.Ordinal) || !PackPath.IsValid(effect))
            checks.Fail($"{where}'s effect {cue.Effect} isn't a path under models/effects/", SequenceChecks.F("effect"));
        checks.Node(cue.Node, $"{where}'s node", SequenceChecks.F("node"));
        var rate = !double.IsFinite(cue.Rate) || cue.Rate < 0;
        var duration = !double.IsFinite(cue.Duration) || cue.Duration < 0;
        if (cue.Burst < 0 || rate || duration)
            checks.Fail($"{where}: its burst, rate and duration must be 0 or more",
                SequenceChecks.Where((cue.Burst < 0, "burst"), (rate, "rate"), (duration, "duration")));
        if (cue.Palette != null)
            checks.Role(cue.Palette, $"{where}'s palette", SequenceChecks.F("palette"), RoleKind.Palette);
    });
}

/// <summary>An audio track (<c>scene.audio-track</c>, M10-8): sounds and music cues scheduled on the audio clock (step 6).</summary>
[Record("scene.audio-track", 1)]
[Help("Plays sound effects and switches the music.")]
public sealed partial record AudioTrack : SequenceTrack
{
    [Field(1), Help("The sounds and music, each at its time.")] public IReadOnlyList<AudioCue> Cues { get; init; } = [];

    internal override string Kind => "audio";

    internal override void Validate(SequenceChecks checks) => checks.Cues(Cues, "cues", (cue, where) =>
    {
        if (!Enum.IsDefined(cue.Kind))
            checks.Fail($"{where} has a kind that isn't one", SequenceChecks.F("kind"));
        if (cue.Kind == AudioCueKind.StopMusic ? cue.Name != null : string.IsNullOrEmpty(cue.Name))
            checks.Fail(cue.Kind == AudioCueKind.StopMusic ? $"{where} stops the music but names {cue.Name}" : $"{where} names no {(cue.Kind == AudioCueKind.Music ? "theme" : "effect")}",
                SequenceChecks.F("name"));
        var volume = !double.IsFinite(cue.Volume) || cue.Volume < 0;
        var pan = !double.IsFinite(cue.Pan) || cue.Pan is < -1 or > 1;
        var pitch = !double.IsFinite(cue.Pitch) || cue.Pitch <= 0;
        if (volume || pan || pitch)
            checks.Fail($"{where}: its volume must be 0 or more, its pan from −1 to 1 and its pitch above 0",
                SequenceChecks.Where((volume, "volume"), (pan, "pan"), (pitch, "pitch")));
    });
}

/// <summary>A UI track (<c>scene.ui-track</c>, M10-9): text, classes and attributes set at cues on elements of a bound host (step 6).</summary>
[Record("scene.ui-track", 1)]
[Help("Changes texts, classes and attributes of elements on the screen, such as a title card.")]
public sealed partial record UiTrack : SequenceTrack
{
    /// <summary>The element role whose elements the cues name.</summary>
    [Field(1), Help("The screen element, by its element role, inside which the cues find their elements.")] public required string Host { get; init; }
    [Field(2), Help("The changes, each at its time.")] public IReadOnlyList<UiCue> Cues { get; init; } = [];

    internal override string Kind => "ui";

    internal override void Validate(SequenceChecks checks)
    {
        checks.Role(Host, "its host", SequenceChecks.F("host"), RoleKind.Element);
        checks.Cues(Cues, "cues", (cue, where) =>
        {
            if (!Enum.IsDefined(cue.Action))
                checks.Fail($"{where} has an action that isn't one", SequenceChecks.F("action"));
            if (cue.Element is "")
                checks.Fail($"{where} names an empty element id", SequenceChecks.F("element"));
            var (name, value) = cue.Action switch
            {
                UiCueAction.SetText => (false, true),
                UiCueAction.AddClass or UiCueAction.RemoveClass or UiCueAction.RemoveAttribute => (true, false),
                _ => (true, true),
            };
            var nameWrong = name != !string.IsNullOrEmpty(cue.Name);
            var valueWrong = value != (cue.Value != null);
            if (nameWrong || valueWrong)
                checks.Fail($"{where}: {cue.Action} takes {(name ? "a name" : "no name")} and {(value ? "a value" : "no value")}",
                    SequenceChecks.Where((nameWrong, "name"), (valueWrong, "value")));
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
[Kind("Cutscene", "world/cutscenes")]
[Help("A cutscene: tracks on a timeline that move the stage and the figures, cut between cameras, light the scene, start particles, play sound and change the screen.")]
public sealed record Sequence
{
    [Field(1), Help("The cutscene's name, used in the messages about it.")] public required string Name { get; init; }
    /// <summary>Seconds; the end state is every track at this time (Q3).</summary>
    [Field(2), Help("How long the cutscene lasts. When it ends, or the player skips it, every track is as it is at this time."), Unit("s"),
     Limits(0, double.PositiveInfinity, MinExclusive = true)]
    public required double Duration { get; init; }
    /// <summary>False: <see cref="SequencePlayer.Skip"/> refuses; <see cref="SequencePlayer.Finish"/> still ends it (Q20).</summary>
    [Field(3), Help("Whether the player can skip the cutscene.")] public bool Skippable { get; init; } = true;
    /// <summary>A flag for the game, which holds its AI while a blocking sequence plays (Q20); a vignette is not blocking.</summary>
    [Field(4), Help("Whether the game holds the battle while the cutscene plays. Leave it off for a short scene that plays beside the battle.")]
    public bool Blocking { get; init; } = true;
    [Field(5), Help("The names the game fills in when it plays the cutscene: figures, scene nodes, models, screen elements and palettes.")]
    public IReadOnlyList<SequenceRole> Roles { get; init; } = [];
    /// <summary>Paths of stage models (<c>models/stages/&lt;name&gt;</c>); a stage's name is its path's last name.</summary>
    [Field(6), Help("The stages the cutscene brings, each a model at models/stages/<name>. The tracks call a stage by its name, the last part of its path."),
     References(typeof(ModelData), Folder = "models/stages", Rule = typeof(PathNames))]
    public IReadOnlyList<string> Stages { get; init; } = [];
    /// <summary>In track order: state tracks apply in this order each frame (Q17).</summary>
    [Field(7), Help("What happens on the timeline. Where two tracks set the same thing, the later one wins.")]
    public IReadOnlyList<SequenceTrack> Tracks { get; init; } = [];
    /// <summary>The reduced variant's cut list (Q19): spans in time order; null when the sequence has none.</summary>
    [Field(8), Help("A shorter version, played when the player's system asks for reduced motion: parts of the timeline in order, each played and then held (not set: none).")]
    public IReadOnlyList<SequenceSpan>? Reduced { get; init; }

    /// <summary>A stage's name: its path's last name.</summary>
    public static string StageName(string path) => path[(path.LastIndexOf('/') + 1)..];

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> naming the problem when the record breaks a rule it
    /// can be checked for alone: its name and duration, roles and stages named once, every track's names
    /// resolving to roles of the right kind or to its stages, its numbers, cue times within the
    /// timeline, markers named as <c>&lt;stage&gt;/&lt;marker&gt;</c>, the cut list in order inside it; and
    /// the limits its fields declare (G4). The first of <see cref="RecordChecks.Check"/>'s problems.
    /// </summary>
    public void Validate()
    {
        if (RecordChecks.Check(this).FirstOrDefault() is { } problem)
            throw new ArgumentException(problem.Message);
    }

    /// <summary>
    /// <see cref="Validate"/>, and the names in its stages (Q15, S4-2): the nodes a track names, the
    /// clips its clip tracks play and the markers its cues sit at exist in the stage models
    /// (<paramref name="stages"/>, by path), and cues at markers fall within the timeline.
    /// </summary>
    public void Validate(IReadOnlyDictionary<string, ModelData> stages)
    {
        if ((SequenceChecks.Of(this, stages).FirstOrDefault() ?? RecordChecks.Check(this).FirstOrDefault()) is { } problem)
            throw new ArgumentException(problem.Message);
    }

    /// <summary>
    /// The sequence's own check (P20; Docs/Editor.md, step 6), each problem at the path of what it names
    /// with <see cref="Validate()"/>'s words: the rules it keeps alone; then, given the data, its stages
    /// read from it (<see cref="Load"/>'s words for a stage the data doesn't have, a broken stage's own
    /// error), the names in them (<see cref="Validate(IReadOnlyDictionary{string, ModelData})"/>), and its
    /// particle effects and sounds against the data (<see cref="SequenceChecks.AgainstData"/>, G5).
    /// </summary>
    [Check]
    static void Rules(Sequence sequence, CheckContext check)
    {
        var problems = SequenceChecks.Of(sequence);
        if (problems.Count == 0 && check.Data is { } data)
            problems = SequenceChecks.WithData(sequence, data);
        foreach (var problem in problems)
            check.Report(problem.Path, problem.Message);
    }

    /// <summary>
    /// The sequence at <paramref name="path"/> in the game's data (a record entry, or its JSON form in the
    /// development folder), checked by itself and against its stages, which are read from the same data
    /// (<see cref="ModelData.Load(VirtualFileSystem, string)"/>), and its particle effects and sounds against
    /// the same data (<see cref="SequenceChecks.AgainstData"/>; G5, the user's decision of 2026-11-07): a
    /// <see cref="DataFileException"/> naming the pack and entry or the loose file when it isn't a readable
    /// sequence record (a curve breaking its rules included), breaks a rule or a limit, names a stage the data
    /// doesn't have, or names a particle effect, effect patch or theme the data doesn't have (a broken stage,
    /// effect, patch or theme is its own error naming its entry).
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
                throw files.Problem(path, $"not a valid sequence: {SequenceChecks.StageNotInData(stage)}", missing);
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
        if (SequenceChecks.AgainstData(sequence, files, reportBroken: false).FirstOrDefault() is { } against)
            throw files.Problem(path, $"not a valid sequence: {against.Message}");
        return sequence;
    }
}

/// <summary>
/// The checks of a sequence (<see cref="Sequence.Validate()"/>), with its stages' models when given: each
/// problem the words of the sequence, the track and the cue, at the path of what it names (P20). A part
/// of the record (a role, a stage, a check of a track's field, a cue, a strip, a span) stops at its first
/// problem and the rest is checked on, so the first problem is the one the checks met first.
/// </summary>
internal sealed class SequenceChecks
{
    /// <summary>Ends the part being checked after its problem.</summary>
    sealed class Stop : Exception;

    readonly Sequence _sequence;
    readonly Dictionary<string, RoleKind> _roles = [];
    readonly Dictionary<string, string> _stagePaths = [];
    readonly SequenceTimes? _times;
    readonly List<RecordProblem> _problems = [];
    string _where = "";
    RecordPath _at = RecordPath.Root;

    SequenceChecks(Sequence sequence, IReadOnlyDictionary<string, ModelData>? stages)
    {
        _sequence = sequence;
        Stages = stages;
        _times = stages == null ? null : new SequenceTimes(sequence, stages, this);
    }

    /// <summary>The problems of the sequence checked alone, or with its stages' models when given, in the order met.</summary>
    public static IReadOnlyList<RecordProblem> Of(Sequence sequence, IReadOnlyDictionary<string, ModelData>? stages = null)
    {
        var checks = new SequenceChecks(sequence, stages);
        checks.Run();
        return checks._problems;
    }

    /// <summary>
    /// The problems of a sequence that keeps its own rules, given the data: its stages read from it (a stage
    /// it doesn't have in <see cref="Sequence.Load"/>'s words, a broken one with its own error), then, with
    /// all of them there, the names in them, and its particle effects and sounds against the data.
    /// </summary>
    public static IReadOnlyList<RecordProblem> WithData(Sequence sequence, VirtualFileSystem data)
    {
        var problems = new List<RecordProblem>();
        var stages = new Dictionary<string, ModelData>();
        for (var i = 0; i < sequence.Stages.Count; i++)
        {
            var stage = sequence.Stages[i];
            var at = F("stages").Index(i);
            try
            {
                stages[stage] = ModelData.Load(data, stage);
            }
            catch (FileNotFoundException)
            {
                problems.Add(new(at, StageNotInData(stage)));
            }
            catch (DataFileException broken)
            {
                problems.Add(new(at, broken.Message));
            }
        }
        if (problems.Count == 0)
            problems.AddRange(Of(sequence, stages));
        problems.AddRange(AgainstData(sequence, data, reportBroken: true));
        return problems;
    }

    /// <summary><see cref="Sequence.Load"/>'s words for a stage the data doesn't have.</summary>
    internal static string StageNotInData(string stage) => $"its stage {stage} isn't in the game's data";

    /// <summary>
    /// The particle effects and sounds a sequence names, against the game's data, as
    /// <see cref="SequencePlayer"/>'s start checks them against what is bound and registered (S4-6, S5-2,
    /// S6-A; G5): each particle cue's effect is there and keeps its rules
    /// (<see cref="ParticleEffect.Load"/>), each audio cue's effect patch (<c>audio/effects/&lt;name&gt;</c>)
    /// and theme (<c>audio/themes/&lt;id&gt;</c>), named by the music's rule (<see cref="MusicNames"/>), are
    /// there as records of their kind. An entry that is there but broken is its own error: reported at the
    /// cue with <paramref name="reportBroken"/>, else thrown as it is (naming its own entry, as a broken
    /// stage's is). Cues the sequence's own rules refuse (no name, an effect not under models/effects/) are
    /// left to those rules.
    /// </summary>
    public static IReadOnlyList<RecordProblem> AgainstData(Sequence sequence, VirtualFileSystem data, bool reportBroken)
    {
        var checks = new SequenceChecks(sequence, null);
        checks.Data(data, reportBroken);
        return checks._problems;
    }

    public IReadOnlyDictionary<string, ModelData>? Stages { get; }

    /// <summary>A field of the record being checked, as a path relative to it.</summary>
    public static RecordPath F(string name) => RecordPath.Root.Field(name);

    /// <summary>The fields among <paramref name="fields"/> that break a rule.</summary>
    public static RecordPath[] Where(params (bool Broken, string Field)[] fields) => [.. fields.Where(f => f.Broken).Select(f => F(f.Field))];

    void Run()
    {
        var s = _sequence;
        _where = $"the sequence {s.Name}";
        if (string.IsNullOrEmpty(s.Name))
            _problems.Add(new(F("name"), "a sequence needs a name"));
        Part(RecordPath.Root, () =>
        {
            if (!double.IsFinite(s.Duration) || s.Duration <= 0)
                Fail($"its duration ({s.Duration:R} s) must be a finite number above 0", F("duration"));
        });
        for (var i = 0; i < s.Roles.Count; i++)
        {
            var role = s.Roles[i];
            Part(F("roles").Index(i), () =>
            {
                if (role == null)
                    Fail("a role is null");
                if (string.IsNullOrEmpty(role!.Name) || role.Name.Contains('/'))
                    Fail($"the role \"{role.Name}\" needs a name without /", F("name"));
                if (!Enum.IsDefined(role.Kind))
                    Fail($"the role {role.Name} has a kind that isn't one", F("kind"));
                if (!_roles.TryAdd(role.Name, role.Kind))
                    Fail($"the role {role.Name} is defined twice", F("name"));
            });
        }
        for (var i = 0; i < s.Stages.Count; i++)
        {
            var path = s.Stages[i];
            Part(F("stages").Index(i), () =>
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
            });
        }
        for (var t = 0; t < s.Tracks.Count; t++)
        {
            var track = s.Tracks[t];
            _where = $"the sequence {s.Name}, track {t}";
            Part(F("tracks").Index(t), () =>
            {
                if (track == null)
                    Fail("it is null");
                _where = $"the sequence {s.Name}, track {t} ({track!.Kind})";
                track.Validate(this);
            });
        }
        _where = $"the sequence {s.Name}";
        var tracks = s.Tracks.Select((track, index) => (Track: track as ActorTrack, Index: index)).Where(t => t.Track?.Mark != null);
        foreach (var marked in tracks.GroupBy(t => t.Track!.Actor))
            if (marked.Count() > 1)
                Part(RecordPath.Root, () => Fail($"the actor {marked.Key} has a mark on {marked.Count()} animation tracks (one at most: it stands on one node)",
                    [.. marked.Select(t => F("tracks").Index(t.Index).Field("mark"))]));
        Spans(s.Reduced);
    }

    void Spans(IReadOnlyList<SequenceSpan>? spans)
    {
        if (spans == null)
            return;
        if (spans.Count == 0)
            Part(RecordPath.Root, () => Fail("its reduced variant has no spans (null: none)", F("reduced")));
        var end = 0.0;
        for (var i = 0; i < spans.Count; i++)
        {
            var span = spans[i];
            Part(F("reduced").Index(i), () =>
            {
                if (span == null)
                    Fail($"reduced span {i} is null");
                if (!double.IsFinite(span!.From) || !double.IsFinite(span.To) || !double.IsFinite(span.Hold))
                    Fail($"reduced span {i}: its times must be finite numbers",
                        Where((!double.IsFinite(span.From), "from"), (!double.IsFinite(span.To), "to"), (!double.IsFinite(span.Hold), "hold")));
                if (span.From < end || span.To < span.From || span.To > _sequence.Duration || span.Hold < 0)
                    Fail($"reduced span {i} ({span.From:R}–{span.To:R} s, held {span.Hold:R} s) must lie in the timeline after the span before, its end not before its start, its hold not negative",
                        Where((span.From < end, "from"), (span.To < span.From || span.To > _sequence.Duration, "to"), (span.Hold < 0, "hold")));
                if (!(span.Length > 0))
                    Fail($"reduced span {i} plays for no time");
            });
            if (span != null)
                end = span.To;
        }
    }

    /// <summary>Checks one part of the record at <paramref name="at"/> (relative paths below are relative to it); its first problem ends it.</summary>
    void Part(RecordPath at, Action check)
    {
        var saved = _at;
        _at = at;
        try
        {
            check();
        }
        catch (Stop)
        {
        }
        finally
        {
            _at = saved;
        }
    }

    /// <summary>A part below the one being checked.</summary>
    void Below(RecordPath relative, Action check) => Part(_at.Append(relative), check);

    /// <summary>Reports the problem (at the part being checked, or at each of <paramref name="fields"/> within it) and gives what ends the part.</summary>
    public Exception Problem(string message, params RecordPath[] fields)
    {
        var text = $"{_where}: {message}";
        if (fields.Length == 0)
            _problems.Add(new(_at, text));
        foreach (var field in fields)
            _problems.Add(new(_at.Append(field), text));
        return new Stop();
    }

    /// <summary>Reports the problem and ends the part being checked.</summary>
    public void Fail(string message, params RecordPath[] fields) => throw Problem(message, fields);

    /// <summary>Reports the problem and goes on with the part (the check's last).</summary>
    public void Report(string message, params RecordPath[] fields) => Problem(message, fields);

    /// <summary>A role of one of the kinds.</summary>
    public void Role(string name, string what, RecordPath field, params RoleKind[] kinds) => Part(_at, () =>
    {
        if (name == null || !_roles.TryGetValue(name, out var kind))
            Fail($"{what} {name} isn't a role", field);
        else if (!kinds.Contains(kind))
            Fail($"{what} {name} is a role of kind {kind.ToString().ToLowerInvariant()}, not {string.Join(" or ", kinds.Select(k => k.ToString().ToLowerInvariant()))}", field);
    });

    /// <summary>One of the sequence's stages, by name.</summary>
    public void Stage(string name, RecordPath field) => Part(_at, () =>
    {
        if (name == null || !_stagePaths.ContainsKey(name))
            Fail($"{name} isn't one of its stages", field);
    });

    /// <summary>The model of a stage (by name), when the stages are given and hold it (a stage not given is <see cref="Run"/>'s problem).</summary>
    public ModelData? StageModel(string name) => Stages != null && Stages.TryGetValue(_stagePaths[name], out var model) ? model : null;

    /// <summary>
    /// A node: a node role's name, or <c>&lt;stage&gt;/&lt;Blender name&gt;</c>, which with the stages given
    /// must be a node of that stage's model its scene reaches.
    /// </summary>
    public void Node(string reference, string what, RecordPath field) => Part(_at, () =>
    {
        if (reference == null)
            Fail($"{what} is null", field);
        var slash = reference!.IndexOf('/');
        if (slash < 0)
        {
            Role(reference, what, field, RoleKind.Node);
            return;
        }
        var (stage, name) = (reference[..slash], reference[(slash + 1)..]);
        if (!_stagePaths.ContainsKey(stage) || name.Length == 0)
            Fail($"{what} {reference} isn't a node role or <stage>/<node> of one of its stages", field);
        if (StageModel(stage) is { } model && StageNodeIndex(model, name) < 0)
            Fail($"{what} {reference}: the stage {_stagePaths[stage]} has no node {name}", field);
    });

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

    /// <summary>A clip of a stage's model (with the stages given; a stage that isn't one is <see cref="Stage"/>'s problem).</summary>
    public void StageClip(string stage, string clip, RecordPath field) => Part(_at, () =>
    {
        if (stage != null && _stagePaths.ContainsKey(stage) && StageModel(stage) is { } model && model.Animations.All(a => a.Name != clip))
            Fail($"the stage {_stagePaths[stage]} has no clip {clip}", field);
    });

    /// <summary>What a palette track colours: an actor or model role, or a stage.</summary>
    public void PaletteTarget(string target, RecordPath field)
    {
        if (target != null && _stagePaths.ContainsKey(target))
            return;
        Role(target!, "its target", field, RoleKind.Actor, RoleKind.Model);
    }

    /// <summary>A strip at <paramref name="at"/> (relative to the part being checked).</summary>
    public void Strip(SequenceStrip strip, string what, RecordPath at) => Below(at, () =>
    {
        if (strip == null)
            Fail($"{what} is null");
        if (string.IsNullOrEmpty(strip!.Clip))
            Fail($"{what} names no clip", F("clip"));
        if (!double.IsFinite(strip.Start) || !double.IsFinite(strip.ClipStart) || strip.ClipEnd is { } e && !double.IsFinite(e)
            || !double.IsFinite(strip.Scale) || double.IsNaN(strip.Repeat) || !double.IsFinite(strip.BlendIn) || !double.IsFinite(strip.BlendOut))
            Fail($"{what} ({strip.Clip}): its numbers must be finite (its repeat may be infinite)",
                Where((!double.IsFinite(strip.Start), "start"), (!double.IsFinite(strip.ClipStart), "clipStart"),
                    (strip.ClipEnd is { } end && !double.IsFinite(end), "clipEnd"), (!double.IsFinite(strip.Scale), "scale"),
                    (double.IsNaN(strip.Repeat), "repeat"), (!double.IsFinite(strip.BlendIn), "blendIn"), (!double.IsFinite(strip.BlendOut), "blendOut")));
        if (strip.ClipStart < 0 || strip.ClipEnd < strip.ClipStart)
            Fail($"{what} ({strip.Clip}): its part ({strip.ClipStart:R}–{strip.ClipEnd:R} s) must start at 0 or later and not end before it starts",
                Where((strip.ClipStart < 0, "clipStart"), (strip.ClipEnd < strip.ClipStart, "clipEnd")));
        if (!(strip.Scale > 0) || !(strip.Repeat > 0) || strip.BlendIn < 0 || strip.BlendOut < 0)
            Fail($"{what} ({strip.Clip}): its scale and repeat must be above 0, its blends not negative",
                Where((!(strip.Scale > 0), "scale"), (!(strip.Repeat > 0), "repeat"), (strip.BlendIn < 0, "blendIn"), (strip.BlendOut < 0, "blendOut")));
        if (strip.Influence is { } influence && !(influence >= 0 && influence <= 1))
            Fail($"{what} ({strip.Clip}): its influence must be from 0 to 1", F("influence"));
        if (!Enum.IsDefined(strip.Mode) || !Enum.IsDefined(strip.Extrapolation))
            Fail($"{what} ({strip.Clip}): a blend mode or extrapolation that isn't one",
                Where((!Enum.IsDefined(strip.Mode), "mode"), (!Enum.IsDefined(strip.Extrapolation), "extrapolation")));
    });

    /// <summary>Each cue of the list <paramref name="field"/> (its time or marker, S4-2, then <paramref name="check"/>), a part of its own.</summary>
    public void Cues<T>(IReadOnlyList<T> cues, string field, Action<T, string> check) where T : ISequenceCue
    {
        for (var i = 0; i < cues.Count; i++)
        {
            var cue = cues[i];
            var where = $"cue {i}";
            Below(F(field).Index(i), () =>
            {
                if (cue == null)
                    Fail($"{where} is null");
                Time(cue!, where);
                check(cue!, where);
            });
        }
    }

    /// <summary>A cue's time: within the timeline, or an offset from a marker <c>&lt;stage&gt;/&lt;marker&gt;</c> (resolved and checked with the stages given).</summary>
    public void Time(ISequenceCue cue, string where) => Part(_at, () =>
    {
        if (!double.IsFinite(cue.Time))
            Fail($"{where}: its time must be a finite number", F("time"));
        if (cue.Marker is not { } marker)
        {
            if (cue.Time < 0 || cue.Time > _sequence.Duration)
                Fail($"{where} at {cue.Time:R} s is outside the timeline (0–{_sequence.Duration:R} s)", F("time"));
            return;
        }
        var slash = marker.IndexOf('/');
        if (slash <= 0 || slash == marker.Length - 1 || !_stagePaths.ContainsKey(marker[..slash]))
            Fail($"{where}'s marker {marker} isn't <stage>/<marker> of one of its stages", F("marker"));
        if (_times != null && StageModel(marker[..slash]) != null)
        {
            var time = _times.Resolve(cue.Time, marker, where);
            if (time < 0 || time > _sequence.Duration)
                Fail($"{where} at {cue.Time:R} s from the marker {marker} falls at {time:R} s, outside the timeline (0–{_sequence.Duration:R} s)", F("time"));
        }
    });

    /// <summary>Whether <paramref name="text"/> is "#rrggbb".</summary>
    public static bool IsColour(string text) => text.Length == 7 && text[0] == '#' && text.Skip(1).All(char.IsAsciiHexDigit);

    /// <summary>The stage's path by its name.</summary>
    public string StagePath(string name) => _stagePaths[name];

    /// <summary>See <see cref="AgainstData"/>: each cue a part of its own, every entry read once.</summary>
    void Data(VirtualFileSystem data, bool reportBroken)
    {
        var s = _sequence;
        var read = new Dictionary<string, Exception?>(StringComparer.Ordinal);
        Exception? Read(string path, Action load)
        {
            if (read.TryGetValue(path, out var outcome))
                return outcome;
            try
            {
                load();
            }
            catch (Exception problem) when (problem is FileNotFoundException or DataFileException or ArgumentException)
            {
                outcome = problem;
            }
            return read[path] = outcome;
        }
        void Entry(string path, Action load, string missing, RecordPath field)
        {
            switch (Read(path, load))
            {
                case null:
                    return;
                case FileNotFoundException:
                    Fail(missing, field);
                    return;
                case var broken when reportBroken:
                    _problems.Add(new(_at.Append(field), broken.Message));
                    return;
                case var broken:
                    throw broken;
            }
        }
        for (var t = 0; t < s.Tracks.Count; t++)
        {
            _where = $"the sequence {s.Name}, track {t} ({s.Tracks[t]?.Kind})";
            var at = F("tracks").Index(t);
            if (s.Tracks[t] is ParticleTrack particles)
                for (var i = 0; i < particles.Cues.Count; i++)
                {
                    var cue = particles.Cues[i];
                    if (cue?.Effect is not { } effect || !effect.StartsWith("models/effects/", StringComparison.Ordinal) || !PackPath.IsValid(effect))
                        continue;
                    Part(at.Field("cues").Index(i), () =>
                        Entry(effect, () => ParticleEffect.Load(data, effect), $"cue {i}'s effect {effect} isn't in the game's data", F("effect")));
                }
            else if (s.Tracks[t] is AudioTrack audio)
                for (var i = 0; i < audio.Cues.Count; i++)
                {
                    var cue = audio.Cues[i];
                    if (cue is not { Kind: AudioCueKind.Effect or AudioCueKind.Music, Name: { Length: > 0 } name })
                        continue;
                    var (folder, what) = cue.Kind == AudioCueKind.Effect ? (MusicNames.Effects, "effect") : (MusicNames.Themes, "theme");
                    Part(at.Field("cues").Index(i), () =>
                    {
                        var stored = MusicNames.ToPathName(name) ?? throw Problem($"cue {i} plays the {what} {name}, which no data path can hold", F("name"));
                        var path = folder + "/" + stored;
                        Action load = cue.Kind == AudioCueKind.Effect ? () => data.Load<EffectPatch>(path) : () => data.Load<ThemeSpec>(path);
                        Entry(path, load, $"cue {i} plays the {what} {name}, which isn't in the game's data ({path})", F("name"));
                    });
                }
        }
    }
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

    /// <summary>A problem with the cue's marker: reported by the sequence's checks (at the cue's marker) when they resolve it, else an <see cref="ArgumentException"/>.</summary>
    Exception Problem(string message) => checks?.Problem(message, SequenceChecks.F("marker")) ?? new ArgumentException($"the sequence {sequence.Name}: {message}");
}
