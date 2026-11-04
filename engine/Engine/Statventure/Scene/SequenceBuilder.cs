using Statventure.Animation;
using Statventure.Render3D;

namespace Statventure.Scene;

/// <summary>A cue's time for the builder: seconds on the timeline, or an offset from a stage's marker (S4-2).</summary>
public readonly record struct CueTime(double Seconds, string? Marker = null)
{
    public static implicit operator CueTime(double seconds) => new(seconds);

    /// <summary><paramref name="offset"/> seconds from the marker <c>"&lt;stage&gt;/&lt;marker&gt;"</c>.</summary>
    public static CueTime At(string marker, double offset = 0) => new(offset, marker);
}

/// <summary>
/// Builds a <see cref="Sequence"/> in C# as <see cref="StateMachineBuilder"/> builds a state machine
/// (Docs/Scene.md, "The builder"): fluent, everything by name, tracks in the order added;
/// <see cref="Build"/> checks the sequence by itself and against the stage models it was given
/// (<see cref="Sequence.Validate(IReadOnlyDictionary{string, ModelData})"/>).
/// </summary>
public sealed class SequenceBuilder(string name, double duration)
{
    readonly List<SequenceRole> _roles = [];
    readonly List<string> _stages = [];
    readonly Dictionary<string, ModelData> _stageModels = [];
    readonly List<SequenceTrack> _tracks = [];
    List<SequenceSpan>? _reduced;
    bool _skippable = true, _blocking = true;

    /// <summary>The skip refuses it; <see cref="SequencePlayer.Finish"/> still ends it (Q20).</summary>
    public SequenceBuilder Unskippable()
    {
        _skippable = false;
        return this;
    }

    /// <summary>The game doesn't hold its AI while it plays (a vignette, Q20).</summary>
    public SequenceBuilder NonBlocking()
    {
        _blocking = false;
        return this;
    }

    public SequenceBuilder Role(string role, RoleKind kind)
    {
        _roles.Add(new SequenceRole(role, kind));
        return this;
    }

    /// <summary>A stage the sequence brings (<c>models/stages/&lt;name&gt;</c>), with its model to check the names in it against.</summary>
    public SequenceBuilder Stage(string path, ModelData model)
    {
        _stages.Add(path);
        _stageModels[path] = model;
        return this;
    }

    /// <summary>Any track as its record.</summary>
    public SequenceBuilder Track(SequenceTrack track)
    {
        _tracks.Add(track);
        return this;
    }

    /// <summary>A stage's clip as a strip on the timeline.</summary>
    public SequenceBuilder Clip(string stage, SequenceStrip strip) => Track(new ClipTrack { Stage = stage, Strip = strip });

    /// <summary>A stage's clip from <paramref name="start"/>, played <paramref name="repeat"/> times (infinite: a loop).</summary>
    public SequenceBuilder Clip(string stage, string clip, double start = 0, double repeat = 1) => Clip(stage, new SequenceStrip(clip, start, Repeat: repeat));

    public SequenceBuilder Transform(string node, Action<TransformBuilder> build)
    {
        var builder = new TransformBuilder();
        build(builder);
        var c = builder.Curves;
        return Track(new TransformTrack
        {
            Node = node,
            LocationX = c[0], LocationY = c[1], LocationZ = c[2],
            RotationX = c[3], RotationY = c[4], RotationZ = c[5],
            ScaleX = c[6], ScaleY = c[7], ScaleZ = c[8],
        });
    }

    public SequenceBuilder Visibility(string node, Action<VisibilityBuilder> build)
    {
        var builder = new VisibilityBuilder();
        build(builder);
        return Track(new VisibilityTrack { Node = node, Cues = [.. builder.Cues] });
    }

    /// <summary>A bound figure on a mark node (null: where it stands), its strips and its parameter and trigger cues (Q16).</summary>
    public SequenceBuilder Animation(string actor, string? mark, Action<AnimationBuilder> build)
    {
        var builder = new AnimationBuilder();
        build(builder);
        return Track(new ActorTrack { Actor = actor, Mark = mark, Strips = [.. builder.Strips], Cues = [.. builder.Cues] });
    }

    /// <summary>Palette cues for an actor or model role or a stage (Q24, S4-3).</summary>
    public SequenceBuilder Palette(string target, Action<PaletteBuilder> build)
    {
        var builder = new PaletteBuilder();
        build(builder);
        return Track(new PaletteTrack { Target = target, Cues = [.. builder.Cues] });
    }

    public SequenceBuilder Events(Action<EventBuilder> build)
    {
        var builder = new EventBuilder();
        build(builder);
        return Track(new EventTrack { Cues = [.. builder.Cues] });
    }

    public SequenceBuilder Camera(Action<CameraBuilder> build)
    {
        var builder = new CameraBuilder();
        build(builder);
        return Track(new CameraTrack { Cuts = [.. builder.Cuts], FieldOfView = builder.FieldOfViewCurve });
    }

    public SequenceBuilder Light(Action<LightBuilder> build)
    {
        var builder = new LightBuilder();
        build(builder);
        return Track(new LightTrack { Cues = [.. builder.Cues], Strength = builder.StrengthCurve, Angle = builder.AngleCurve });
    }

    public SequenceBuilder Particles(Action<ParticleBuilder> build)
    {
        var builder = new ParticleBuilder();
        build(builder);
        return Track(new ParticleTrack { Cues = [.. builder.Cues] });
    }

    public SequenceBuilder Audio(Action<AudioBuilder> build)
    {
        var builder = new AudioBuilder();
        build(builder);
        return Track(new AudioTrack { Cues = [.. builder.Cues] });
    }

    /// <summary>UI cues on elements of the element role <paramref name="host"/> (by id within it; null: the host).</summary>
    public SequenceBuilder Ui(string host, Action<UiBuilder> build)
    {
        var builder = new UiBuilder();
        build(builder);
        return Track(new UiTrack { Host = host, Cues = [.. builder.Cues] });
    }

    /// <summary>The reduced variant's cut list (Q19).</summary>
    public SequenceBuilder Reduced(Action<ReducedBuilder> build)
    {
        var builder = new ReducedBuilder();
        build(builder);
        _reduced = builder.Spans;
        return this;
    }

    /// <summary>The sequence, checked by itself and against the stage models given (an <see cref="ArgumentException"/> naming the problem).</summary>
    public Sequence Build()
    {
        var sequence = new Sequence
        {
            Name = name,
            Duration = duration,
            Skippable = _skippable,
            Blocking = _blocking,
            Roles = [.. _roles],
            Stages = [.. _stages],
            Tracks = [.. _tracks],
            Reduced = _reduced is { } spans ? [.. spans] : null,
        };
        sequence.Validate(_stageModels);
        return sequence;
    }

    public sealed class TransformBuilder
    {
        internal FCurve?[] Curves { get; } = new FCurve?[9];

        /// <summary>Location curves (null: that component the node's own).</summary>
        public TransformBuilder Location(FCurve? x = null, FCurve? y = null, FCurve? z = null) => Set(0, x, y, z);

        /// <summary>Rotation curves, Euler angles XYZ in radians (Q8).</summary>
        public TransformBuilder Rotation(FCurve? x = null, FCurve? y = null, FCurve? z = null) => Set(3, x, y, z);

        public TransformBuilder Scale(FCurve? x = null, FCurve? y = null, FCurve? z = null) => Set(6, x, y, z);

        TransformBuilder Set(int channel, FCurve? x, FCurve? y, FCurve? z)
        {
            (Curves[channel], Curves[channel + 1], Curves[channel + 2]) = (x, y, z);
            return this;
        }
    }

    public sealed class VisibilityBuilder
    {
        internal List<VisibilityCue> Cues { get; } = [];

        public VisibilityBuilder Show(CueTime time) => Add(time, true);
        public VisibilityBuilder Hide(CueTime time) => Add(time, false);

        VisibilityBuilder Add(CueTime time, bool shown)
        {
            Cues.Add(new VisibilityCue(time.Seconds, shown, time.Marker));
            return this;
        }
    }

    public sealed class AnimationBuilder
    {
        internal List<SequenceStrip> Strips { get; } = [];
        internal List<ParameterCue> Cues { get; } = [];

        public AnimationBuilder Strip(SequenceStrip strip)
        {
            Strips.Add(strip);
            return this;
        }

        /// <summary>A clip of the figure's from <paramref name="start"/>, looping or played once, blending in and out (Blender's rule).</summary>
        public AnimationBuilder Strip(string clip, double start = 0, bool loop = false, double blendIn = 0, double blendOut = 0, BlendMode mode = BlendMode.Replace) =>
            Strip(new SequenceStrip(clip, start, Repeat: loop ? double.PositiveInfinity : 1, BlendIn: blendIn, BlendOut: blendOut, Mode: mode));

        public AnimationBuilder Bool(CueTime time, string parameter, bool value) => Add(time, parameter, ParameterKind.Bool, value ? 1 : 0);
        public AnimationBuilder Int(CueTime time, string parameter, int value) => Add(time, parameter, ParameterKind.Int, value);
        public AnimationBuilder Float(CueTime time, string parameter, double value) => Add(time, parameter, ParameterKind.Float, value);
        public AnimationBuilder Trigger(CueTime time, string parameter) => Add(time, parameter, ParameterKind.Trigger, 1);

        AnimationBuilder Add(CueTime time, string parameter, ParameterKind kind, double value)
        {
            Cues.Add(new ParameterCue(time.Seconds, parameter, kind, value, time.Marker));
            return this;
        }
    }

    public sealed class PaletteBuilder
    {
        internal List<PaletteCue> Cues { get; } = [];

        /// <summary>The palette bound to a palette role.</summary>
        public PaletteBuilder Role(CueTime time, string palette) => Add(new PaletteCue(time.Seconds, palette, null, time.Marker));

        /// <summary>One colour ("#rrggbb") on every slot of the target's models (the all-ink silhouette).</summary>
        public PaletteBuilder Fill(CueTime time, string colour) => Add(new PaletteCue(time.Seconds, null, colour, time.Marker));

        /// <summary>The target's own palette (as it was when the sequence started).</summary>
        public PaletteBuilder Own(CueTime time) => Add(new PaletteCue(time.Seconds, null, null, time.Marker));

        PaletteBuilder Add(PaletteCue cue)
        {
            Cues.Add(cue);
            return this;
        }
    }

    public sealed class EventBuilder
    {
        internal List<EventCue> Cues { get; } = [];

        public EventBuilder Event(CueTime time, string name)
        {
            Cues.Add(new EventCue(time.Seconds, name, time.Marker));
            return this;
        }
    }

    public sealed class CameraBuilder
    {
        internal List<CameraCut> Cuts { get; } = [];
        internal FCurve? FieldOfViewCurve { get; private set; }

        public CameraBuilder Cut(CueTime time, string camera)
        {
            Cuts.Add(new CameraCut(time.Seconds, camera, time.Marker));
            return this;
        }

        /// <summary>The vertical field of view in radians.</summary>
        public CameraBuilder FieldOfView(FCurve curve)
        {
            FieldOfViewCurve = curve;
            return this;
        }
    }

    public sealed class LightBuilder
    {
        internal List<LightCue> Cues { get; } = [];
        internal FCurve? StrengthCurve { get; private set; }
        internal FCurve? AngleCurve { get; private set; }

        public LightBuilder Sun(CueTime time, string node)
        {
            Cues.Add(new LightCue(time.Seconds, node, time.Marker));
            return this;
        }

        public LightBuilder Strength(FCurve curve)
        {
            StrengthCurve = curve;
            return this;
        }

        /// <summary>The sun's angle in degrees.</summary>
        public LightBuilder Angle(FCurve curve)
        {
            AngleCurve = curve;
            return this;
        }
    }

    public sealed class ParticleBuilder
    {
        internal List<ParticleCue> Cues { get; } = [];

        public ParticleBuilder Emit(CueTime time, string effect, string node, int seed = 0, int burst = 0, double rate = 0, double duration = 0, string? palette = null)
        {
            Cues.Add(new ParticleCue(time.Seconds, effect, node, seed, burst, rate, duration, palette, time.Marker));
            return this;
        }
    }

    public sealed class AudioBuilder
    {
        internal List<AudioCue> Cues { get; } = [];

        public AudioBuilder Effect(CueTime time, string effect, double volume = 1, double pan = 0, double pitch = 1) =>
            Add(new AudioCue(time.Seconds, AudioCueKind.Effect, effect, volume, pan, pitch, time.Marker));

        public AudioBuilder Music(CueTime time, string theme) => Add(new AudioCue(time.Seconds, AudioCueKind.Music, theme, Marker: time.Marker));

        public AudioBuilder StopMusic(CueTime time) => Add(new AudioCue(time.Seconds, AudioCueKind.StopMusic, Marker: time.Marker));

        AudioBuilder Add(AudioCue cue)
        {
            Cues.Add(cue);
            return this;
        }
    }

    public sealed class UiBuilder
    {
        internal List<UiCue> Cues { get; } = [];

        /// <summary>The text of the element with this id (null: the host).</summary>
        public UiBuilder Text(CueTime time, string? element, string text) => Add(time, UiCueAction.SetText, element, null, text);
        public UiBuilder AddClass(CueTime time, string name, string? element = null) => Add(time, UiCueAction.AddClass, element, name, null);
        public UiBuilder RemoveClass(CueTime time, string name, string? element = null) => Add(time, UiCueAction.RemoveClass, element, name, null);
        public UiBuilder SetAttribute(CueTime time, string name, string value, string? element = null) => Add(time, UiCueAction.SetAttribute, element, name, value);
        public UiBuilder RemoveAttribute(CueTime time, string name, string? element = null) => Add(time, UiCueAction.RemoveAttribute, element, name, null);

        UiBuilder Add(CueTime time, UiCueAction action, string? element, string? name, string? value)
        {
            Cues.Add(new UiCue(time.Seconds, action, element, name, value, time.Marker));
            return this;
        }
    }

    public sealed class ReducedBuilder
    {
        internal List<SequenceSpan> Spans { get; } = [];

        /// <summary>The timeline from <paramref name="from"/> to <paramref name="to"/>, played at its own length.</summary>
        public ReducedBuilder Span(double from, double to, double hold = 0)
        {
            Spans.Add(new SequenceSpan(from, to, hold));
            return this;
        }

        /// <summary>The moment <paramref name="at"/> held for <paramref name="length"/> seconds.</summary>
        public ReducedBuilder Hold(double at, double length) => Span(at, at, length);
    }
}
