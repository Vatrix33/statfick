using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Scene;

/// <summary>
/// A particle emitter on a node (Docs/Scene.md, "How it works", step 5; S5-3): an effect's burst of
/// <see cref="Burst"/> particles at its start and a stream of <see cref="Rate"/> a second for
/// <see cref="Duration"/> seconds, each particle born where the node is at its birth, its spawn in the
/// node's space then (Blender's emitter object: <c>reset_particle</c>, <c>psys_get_birth_coords</c>) and
/// moving in world space after it, so moving the node leaves a trail and doesn't move what it released.
/// The scene can't evaluate a node's past (game code moves nodes), so it records the node's world
/// each frame: a particle born between two frames starts from the world between theirs
/// (interpolated), one born in a stretch the clock jumped over (a seek, a skip, a cut of the reduced
/// variant, <see cref="Time"/> set) from the world after the jump. Its clock is its own
/// (<see cref="Time"/>, seconds since its start): the scene advances it by the frame's time × its
/// <see cref="SceneView.Rate"/> (S5-Q1; its first frame at 0), a sequence that started it by its own
/// timeline (Q26). Its particles take the colours of <see cref="Palette"/>, or without one those of
/// what its node belongs to (S5-4: the nearest figure, placed model or instanced model on it or above
/// it, its palette and then its materials' own colours; a slot none has is M6's grey). On a hidden
/// node it draws nothing, its particles still ageing; once its last particle has died the scene takes
/// it off its node, unless a sequence playing holds it.
/// </summary>
public sealed class SceneEmitter : SceneAttachment
{
    readonly List<(double Time, Matrix4x4 World)> _samples = [];
    readonly int _burst;
    readonly float _rate, _duration;
    ParticleEmitter? _emitter;
    double _time;
    bool _jumped, _started;

    /// <summary>An emitter of <paramref name="effect"/> (an <see cref="ArgumentException"/> when it breaks its rules, <see cref="ParticleEffect.Problem"/>).</summary>
    public SceneEmitter(ParticleEffect effect)
    {
        ArgumentNullException.ThrowIfNull(effect);
        if (effect.Problem() is { } problem)
            throw new ArgumentException($"the effect isn't valid: {problem}", nameof(effect));
        Effect = effect;
    }

    public ParticleEffect Effect { get; }
    public int Seed { get; init; }

    /// <summary>Particles released at its start (0 or more).</summary>
    public int Burst
    {
        get => _burst;
        init => _burst = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(Burst), value, "an emitter's burst is 0 or more");
    }

    /// <summary>Particles a second in its stream (a finite number, 0 or more).</summary>
    public float Rate
    {
        get => _rate;
        init => _rate = float.IsFinite(value) && value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(Rate), value, "an emitter's rate is a finite number, 0 or more");
    }

    /// <summary>Seconds its stream lasts (a finite number, 0 or more).</summary>
    public float Duration
    {
        get => _duration;
        init => _duration = float.IsFinite(value) && value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(Duration), value, "an emitter's duration is a finite number, 0 or more");
    }

    /// <summary>The colours its effect's slots take; null: those of what its node belongs to (S5-4).</summary>
    public Palette? Palette { get; set; }

    /// <summary>
    /// Seconds on its clock since its start (its particles are those alive then). Setting it shows
    /// another moment, a jump: particles born in the time jumped over start where the node is after it.
    /// </summary>
    public double Time
    {
        get => _time;
        set
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "an emitter's time is a finite number");
            _time = value;
            _started = true;
            _jumped = true;
        }
    }

    /// <summary>Whether none of its particles lives any more, nor will (its time past its last birth plus the longest life).</summary>
    public bool Done => _time >= Emitter.End;

    /// <summary>A sequence playing holds it: its clock is the sequence's (Q26), and the scene neither advances it nor takes it off.</summary>
    internal bool Held { get; set; }

    /// <summary>Tests: the node's world recorded at each time.</summary>
    internal IReadOnlyList<(double Time, Matrix4x4 World)> Samples => _samples;

    /// <summary>The emitter computing the particles: born from the node's recorded worlds, from time 0 on its clock.</summary>
    internal ParticleEmitter Emitter => _emitter ??= new ParticleEmitter
    {
        Effect = Effect, Seed = Seed, Burst = _burst, Rate = _rate, Duration = _duration, Frame = WorldAt,
    };

    /// <summary>The scene's frame: its first at its start, then <paramref name="seconds"/> on (the frame's time × the scene's rate).</summary>
    internal void Step(double seconds)
    {
        if (!_started)
            _started = true;
        else
            _time += seconds;
    }

    /// <summary>A sequence's clock (Q26): <paramref name="time"/> on its own clock, after a jump when <paramref name="jump"/>.</summary>
    internal void Show(double time, bool jump)
    {
        _started = true;
        _time = time;
        _jumped |= jump;
    }

    /// <summary>The clock jumped (a seek, a skip): particles born in the time jumped over start where the node is after it.</summary>
    internal void Jumped() => _jumped = true;

    /// <summary>
    /// Records the node's world at its time (the scene's refresh, after the frame's state is applied). A
    /// jump makes the stretch since the last record start from this world (two records at the last one's
    /// time: a step). Time gone back forgets what lay after it: back into a stretch that was watched, the
    /// births between the record before and now come between that world and this one; back into one
    /// that was jumped over, from this world (the jump now is the seek back). Nothing more is kept once its
    /// last birth is covered.
    /// </summary>
    internal void Sample()
    {
        if (Node is not { } node)
            return;
        var time = _time;
        var jumped = _jumped;
        _jumped = false;
        if (_samples.Count > 0 && _samples[^1].Time > time)
        {
            while (_samples.Count > 0 && _samples[^1].Time > time)
                _samples.RemoveAt(_samples.Count - 1);
            // Into a stretch jumped over: its step goes, and a new one starts from the world now.
            jumped = _samples.Count >= 2 && _samples[^1].Time == _samples[^2].Time;
            if (jumped)
                _samples.RemoveAt(_samples.Count - 1);
        }
        if (_samples.Count > 0 && (_samples[^1].Time == time || _samples[^1].Time > LastBirth + Margin))
            return;
        var world = node.World;
        // Before its start nothing is born: the last world before it is all it needs.
        if (time < -Margin)
            _samples.Clear();
        else if (jumped && _samples.Count > 0)
            _samples.Add((_samples[^1].Time, world));
        _samples.Add((time, world));
    }

    /// <summary>How far around a birth the node's world is needed (a trail's velocity, <see cref="ParticleEmitter"/>: ±0.5 ms).</summary>
    const double Margin = 0.001;

    /// <summary>Its last particle's birth on its clock.</summary>
    double LastBirth => _rate > 0 && Emitter.Count > _burst ? (Emitter.Count - _burst - 1) / _rate : 0;

    /// <summary>The node's world at <paramref name="time"/>: recorded, interpolated between the frames around it, the first before them and the last after.</summary>
    internal Matrix4x4 WorldAt(float time)
    {
        if (_samples.Count == 0)
            return Node?.World ?? Matrix4x4.Identity;
        int low = 0, high = _samples.Count;
        while (low < high)
        {
            var middle = (low + high) / 2;
            if (_samples[middle].Time < time)
                low = middle + 1;
            else
                high = middle;
        }
        if (low == _samples.Count)
            return _samples[^1].World;
        var (t1, w1) = _samples[low];
        if (low == 0 || t1 == time)
            return w1;
        var (t0, w0) = _samples[low - 1];
        return Matrix4x4.Lerp(w0, w1, (float)((time - t0) / (t1 - t0)));
    }

    /// <summary>Its particles now, in its colours.</summary>
    internal IEnumerable<Particle> Particles() => Emitter.At((float)_time, Colours());

    /// <summary>The palette its particles take (S5-4).</summary>
    internal Palette Colours()
    {
        if (Palette is { } own)
            return own;
        var colours = new Palette();
        for (var node = Node; node != null; node = node.Parent)
            foreach (var attachment in node.Attachments)
            {
                if (Coloured(attachment) is not { } found)
                    continue;
                var (palette, models) = found;
                foreach (var slot in new[] { Effect.StartColor, Effect.EndColor })
                    if (palette != null && palette.Slots.TryGetValue(slot, out var tone))
                        colours.Set(slot, tone.Lit);
                    else if (models.SelectMany(m => m.Materials).FirstOrDefault(m => m.Slot == slot) is { } material)
                        colours.Set(slot, material.BaseColor);
                return colours;
            }
        return colours;
    }

    /// <summary>Whether two lists of particles are the same, particle for particle (the effect by reference: comparing records field by field each frame is work for nothing).</summary>
    internal static bool Same(IReadOnlyList<Particle> a, IReadOnlyList<Particle> b)
    {
        if (a.Count != b.Count)
            return false;
        for (var i = 0; i < a.Count; i++)
        {
            Particle p = a[i], q = b[i];
            if (!ReferenceEquals(p.Effect, q.Effect) || p.Position != q.Position || p.Velocity != q.Velocity || p.Size != q.Size || p.Angle != q.Angle
                || p.Color != q.Color || p.Opacity != q.Opacity)
                return false;
        }
        return true;
    }

    /// <summary>What a figure, a placed model or an instanced model is coloured with: its palette and its models (a figure's gear too); null for anything else.</summary>
    static (Palette? Palette, IEnumerable<ModelData> Models)? Coloured(SceneAttachment attachment) => attachment switch
    {
        Actor actor => (actor.Palette, actor.Items.Select(i => i.Model.Data).Concat(actor.Garments.Select(g => g.Model.Data)).Prepend(actor.Model.Data)),
        PlacedModel placed => (placed.Palette, new[] { placed.Model.Data }),
        ModelInstance instance => (instance.Palette, new[] { instance.Data }),
        _ => null,
    };
}
