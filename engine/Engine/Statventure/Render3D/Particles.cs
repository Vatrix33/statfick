using System.Globalization;
using System.Numerics;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Render3D;

// Particle effects (Docs/Renderer-3D.md, "Particles"): what an effect's particles are, as records
// (particle.*, version 1; Docs/Scene.md, S5-1) in models.pak at models/effects/<name> (Q25), and
// emitters that release them, computed from age (closed form, deterministic, rewindable).

/// <summary>
/// A particle's shape (record union <c>particle.shape</c>; Docs/Renderer-3D.md, "Particles"): flat,
/// facing the camera, drawn in the shader, with the proportions its effect gives. "Size" is the
/// particle's size at that moment (metres).
/// </summary>
[Record("particle.shape", 1)]
[Subtype(1, typeof(ParticleShape.Disc))]
[Subtype(2, typeof(ParticleShape.Ring))]
[Subtype(3, typeof(ParticleShape.Spark))]
[Subtype(4, typeof(ParticleShape.Puff))]
[Subtype(5, typeof(ParticleShape.Shard))]
public abstract record ParticleShape
{
    ParticleShape() { }

    /// <summary>What's wrong with its proportions, or null (S5-1).</summary>
    internal abstract string? Problem();

    /// <summary>A filled circle the size across.</summary>
    [Record("particle.disc", 1)]
    public sealed record Disc : ParticleShape
    {
        internal override string? Problem() => null;
    }

    /// <summary>A ring the size across, its band <paramref name="Thickness"/> of the radius wide.</summary>
    [Record("particle.ring", 1)]
    public sealed record Ring([property: Field(1)] float Thickness) : ParticleShape
    {
        internal override string? Problem() => Share(Thickness, "a ring's thickness");
    }

    /// <summary>
    /// A capsule along the particle's motion across the view, <paramref name="Width"/> of the size
    /// wide and as long as the size plus the distance it moves across the view in
    /// <paramref name="Stretch"/> seconds.
    /// </summary>
    [Record("particle.spark", 1)]
    public sealed record Spark([property: Field(1)] float Width, [property: Field(2)] float Stretch) : ParticleShape
    {
        internal override string? Problem() => Share(Width, "a spark's width")
            ?? (float.IsFinite(Stretch) && Stretch >= 0 ? null : $"a spark's stretch ({Number(Stretch)} s) must be a finite number, 0 or more");
    }

    /// <summary>A disc the size across whose opacity eases to nothing over the outer <paramref name="Softness"/> of its radius.</summary>
    [Record("particle.puff", 1)]
    public sealed record Puff([property: Field(1)] float Softness) : ParticleShape
    {
        internal override string? Problem() => Share(Softness, "a puff's softness");
    }

    /// <summary>A rhombus the size long and <paramref name="Width"/> of that wide, turned by the particle's angle.</summary>
    [Record("particle.shard", 1)]
    public sealed record Shard([property: Field(1)] float Width) : ParticleShape
    {
        internal override string? Problem() => Share(Width, "a shard's width");
    }

    /// <summary>A proportion of the size: above 0, at most 1.</summary>
    static string? Share(float value, string what) => value > 0 && value <= 1 ? null : $"{what} ({Number(value)}) must be above 0 and at most 1";

    internal static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    internal static string Vector(Vector3 value) => $"({Number(value.X)}, {Number(value.Y)}, {Number(value.Z)})";
}

/// <summary>A value drawn for each particle, evenly between <see cref="Min"/> and <see cref="Max"/> (record <c>particle.range</c>).</summary>
[Record("particle.range", 1)]
public readonly record struct ParticleRange([property: Field(1)] float Min, [property: Field(2)] float Max)
{
    public static implicit operator ParticleRange(float value) => new(value, value);

    public float At(float share) => Min + (Max - Min) * share;

    /// <summary>What's wrong with it, or null: finite ends, the min not above the max, and with <paramref name="positive"/> neither below 0.</summary>
    internal string? Problem(string what, bool positive)
    {
        if (!float.IsFinite(Min) || !float.IsFinite(Max))
            return $"its {what} ({ParticleShape.Number(Min)}–{ParticleShape.Number(Max)}) must be finite numbers";
        if (Min > Max)
            return $"its {what}'s min ({ParticleShape.Number(Min)}) is above its max ({ParticleShape.Number(Max)})";
        if (positive && Min < 0)
            return $"its {what} ({ParticleShape.Number(Min)}–{ParticleShape.Number(Max)}) must be 0 or more";
        return null;
    }
}

/// <summary>Where an emitter's particles start and which way they go (record union <c>particle.spawn</c>).</summary>
[Record("particle.spawn", 1)]
[Subtype(1, typeof(ParticleSpawn.Point))]
[Subtype(2, typeof(ParticleSpawn.Sphere))]
[Subtype(3, typeof(ParticleSpawn.Cone))]
public abstract record ParticleSpawn
{
    ParticleSpawn() { }

    /// <summary>What's wrong with it, or null (S5-1).</summary>
    internal abstract string? Problem();

    /// <summary>From the emitter's point, in any direction.</summary>
    [Record("particle.point", 1)]
    public sealed record Point : ParticleSpawn
    {
        internal override string? Problem() => null;
    }

    /// <summary>From anywhere in a ball of <paramref name="Radius"/> metres about the emitter, outward from its centre.</summary>
    [Record("particle.sphere", 1)]
    public sealed record Sphere([property: Field(1)] float Radius) : ParticleSpawn
    {
        internal override string? Problem() => float.IsFinite(Radius) && Radius >= 0 ? null : $"a sphere's radius ({ParticleShape.Number(Radius)} m) must be a finite number, 0 or more";
    }

    /// <summary>From the emitter's point, within <paramref name="HalfAngle"/> degrees of <paramref name="Axis"/>.</summary>
    [Record("particle.cone", 1)]
    public sealed record Cone([property: Field(1)] Vector3 Axis, [property: Field(2)] float HalfAngle) : ParticleSpawn
    {
        internal override string? Problem() =>
            !float.IsFinite(Axis.X) || !float.IsFinite(Axis.Y) || !float.IsFinite(Axis.Z) || Axis == Vector3.Zero ? $"a cone's axis {ParticleShape.Vector(Axis)} must be a finite direction, not zero"
            : float.IsFinite(HalfAngle) && HalfAngle >= 0 && HalfAngle <= 180 ? null
            : $"a cone's half angle ({ParticleShape.Number(HalfAngle)}°) must be from 0 to 180";
    }
}

/// <summary>
/// What an effect's particles are (record <c>particle.effect</c>, Q25, S5-1; Docs/Renderer-3D.md,
/// "Particles"): their shape, colours (palette slots at birth and at the end of their life, mixed in
/// sRGB evenly over it), opacity, ink line (pixels times the device scale, centred on the shape's
/// edge; 0: none), whether they glow, and the ranges each particle's life (seconds), size (metres),
/// speed (metres a second), angle (degrees) and spin (degrees a second) are drawn from. They grow in
/// over the first <see cref="GrowIn"/> of their life and shrink and fade out over the last
/// <see cref="FadeOut"/>, eased (smoothstep). Gravity (metres a second squared) pulls them; drag (a
/// second⁻¹) slows them toward the speed gravity keeps. A trail's particles start with
/// <see cref="Inherit"/> of their emitter's velocity besides. In the game's data a record entry at
/// <c>models/effects/&lt;name&gt;</c>, shared by battles and cutscenes (<see cref="Load"/>); its rules
/// (<see cref="Problem"/>) are checked when it is loaded and when an emitter of it starts.
/// </summary>
[Record("particle.effect", 1)]
public sealed record ParticleEffect
{
    [Field(1)] public required ParticleShape Shape { get; init; }
    [Field(2)] public required string StartColor { get; init; }
    [Field(3)] public required string EndColor { get; init; }
    [Field(4)] public float Opacity { get; init; } = 1;
    [Field(5)] public float OutlineWidth { get; init; }
    [Field(6)] public bool Glow { get; init; }
    [Field(7)] public required ParticleRange Life { get; init; }
    [Field(8)] public required ParticleRange Size { get; init; }
    [Field(9)] public required ParticleRange Speed { get; init; }
    [Field(10)] public required ParticleSpawn Spawn { get; init; }
    [Field(11)] public float GrowIn { get; init; }
    [Field(12)] public float FadeOut { get; init; }
    [Field(13)] public Vector3 Gravity { get; init; }
    [Field(14)] public float Drag { get; init; }
    [Field(15)] public ParticleRange Angle { get; init; }
    [Field(16)] public ParticleRange Spin { get; init; }
    [Field(17)] public float Inherit { get; init; }

    /// <summary>
    /// What breaks the effect's rules (S5-1), or null: its shape and spawn given and their proportions
    /// (a ring's thickness, a spark's width, a puff's softness, a shard's width above 0 and at most 1; a
    /// spark's stretch and a sphere's radius 0 or more; a cone's axis not zero and its half angle 0–180°);
    /// its colours naming slots; every number finite; each range's min not above its max, life, size and
    /// speed not below 0; opacity, grow-in and fade-out from 0 to 1; the ink line's width and drag 0 or more.
    /// </summary>
    public string? Problem()
    {
        static string N(float value) => ParticleShape.Number(value);
        static bool Share(float value) => value >= 0 && value <= 1;
        if (Shape is null)
            return "it has no shape";
        if (Spawn is null)
            return "it has no spawn";
        if (string.IsNullOrEmpty(StartColor) || string.IsNullOrEmpty(EndColor))
            return "its start and end colours must name palette slots";
        return Shape.Problem()
            ?? Spawn.Problem()
            ?? Life.Problem("life", positive: true)
            ?? Size.Problem("size", positive: true)
            ?? Speed.Problem("speed", positive: true)
            ?? Angle.Problem("angle", positive: false)
            ?? Spin.Problem("spin", positive: false)
            ?? (Share(Opacity) ? null : $"its opacity ({N(Opacity)}) must be from 0 to 1")
            ?? (Share(GrowIn) && Share(FadeOut) ? null : $"its grow-in ({N(GrowIn)}) and fade-out ({N(FadeOut)}) must be shares of its life, from 0 to 1")
            ?? (float.IsFinite(OutlineWidth) && OutlineWidth >= 0 ? null : $"its ink line's width ({N(OutlineWidth)}) must be a finite number, 0 or more")
            ?? (float.IsFinite(Drag) && Drag >= 0 ? null : $"its drag ({N(Drag)}) must be a finite number, 0 or more")
            ?? (float.IsFinite(Gravity.X) && float.IsFinite(Gravity.Y) && float.IsFinite(Gravity.Z) ? null : $"its gravity {ParticleShape.Vector(Gravity)} must be finite")
            ?? (float.IsFinite(Inherit) ? null : $"its inherited share of the emitter's velocity ({N(Inherit)}) must be a finite number");
    }

    /// <summary>
    /// The effect at <paramref name="path"/> in the game's data (a record entry at
    /// <c>models/effects/&lt;name&gt;</c> in models.pak, or its JSON form in the development folder), checked
    /// (<see cref="Problem"/>): a <see cref="DataFileException"/> naming the pack and entry or the loose
    /// file when it isn't a readable effect record or breaks a rule.
    /// </summary>
    public static ParticleEffect Load(VirtualFileSystem files, string path)
    {
        var effect = files.Load<ParticleEffect>(path);
        if (effect.Problem() is { } problem)
            throw files.Problem(path, $"not a valid particle effect: {problem}");
        return effect;
    }
}

/// <summary>A particle at a moment (world): where it is, how it moves, its size, angle, colour (linear) and opacity.</summary>
public readonly record struct Particle(ParticleEffect Effect, Vector3 Position, Vector3 Velocity, float Size, float Angle, Vector4 Color, float Opacity);

/// <summary>
/// Releases an effect's particles (Docs/Renderer-3D.md, "Particles"): a burst of
/// <see cref="Burst"/> at <see cref="Start"/>, and a stream of <see cref="Rate"/> a second for
/// <see cref="Duration"/> seconds from then; from wherever <see cref="Path"/> puts the emitter at
/// each particle's birth (a fixed point, or a moving object, which leaves a trail), its spawn in world
/// space, or, given <see cref="Frame"/> instead (an emitter on a scene node, Docs/Scene.md, S5-3), in the
/// space of the emitter's world matrix at each particle's birth, as Blender places a particle from its
/// emitter object (<c>psys_get_birth_coords</c>). Every particle is computed from its age, its ranges
/// drawn from <see cref="Seed"/> and its number, so an effect looks the same each time and can be shown
/// at any moment.
/// </summary>
public sealed record ParticleEmitter
{
    public required ParticleEffect Effect { get; init; }
    public required int Seed { get; init; }
    /// <summary>Where the emitter is at a time (its spawn in world space); one of this and <see cref="Frame"/>.</summary>
    public Func<float, Vector3>? Path { get; init; }
    /// <summary>
    /// The emitter's world matrix at a time (one of this and <see cref="Path"/>): a particle born then
    /// starts at its spawn offset through the matrix (turned, scaled and placed) and goes along its spawn
    /// direction through the matrix's 3×3, normalized (none when that is nothing), at the effect's speed;
    /// after its birth it moves in world space (Blender's <c>psys_get_birth_coords</c>).
    /// </summary>
    public Func<float, Matrix4x4>? Frame { get; init; }
    public float Start { get; init; }
    public int Burst { get; init; }
    public float Rate { get; init; }
    public float Duration { get; init; }

    /// <summary>How many particles it releases: the burst and the stream's floor(rate × duration).</summary>
    public int Count => Burst + (Rate > 0 ? (int)MathF.Floor(Rate * Duration) : 0);

    /// <summary>The time (seconds) from which none of its particles lives any more: its last birth plus the longest life.</summary>
    public float End => Count == 0 ? Start : (Count > Burst ? Start + (Count - Burst - 1) / Rate : Start) + Effect.Life.Max;

    /// <summary>An emitter standing at <paramref name="place"/>.</summary>
    public static Func<float, Vector3> At(Vector3 place) => _ => place;

    /// <summary>The particles alive at <paramref name="time"/> (seconds), coloured from <paramref name="palette"/>.</summary>
    public IEnumerable<Particle> At(float time, Palette palette)
    {
        var start = Colors.ToSrgb(Tone(palette, Effect.StartColor));
        var end = Colors.ToSrgb(Tone(palette, Effect.EndColor));
        if (Path == null && Frame == null)
            throw new InvalidOperationException("an emitter needs a path or a frame");
        var count = Count;
        for (var n = 0; n < count; n++)
        {
            var born = n < Burst ? Start : Start + (n - Burst) / Rate;
            if (time < born)
                continue;
            if (Live(n, born, time, start, end) is { } particle)
                yield return particle;
        }
    }

    static Vector4 Tone(Palette palette, string slot) => palette.Resolve(new ModelMaterial(slot, new Vector4(0.5f, 0.5f, 0.5f, 1), false));

    Particle? Live(int n, float born, float time, Vector4 start, Vector4 end)
    {
        var effect = Effect;
        var life = effect.Life.At(Random(n, 0));
        var age = time - born;
        if (life <= 0 || age >= life)
            return null;
        var (offset, direction) = Spawned(n);
        Vector3 from;
        Vector3 carried;
        if (Frame is { } frame)
        {
            // In the emitter's space at its birth, as Blender's emitter object (psys_get_birth_coords:
            // the place through the world matrix, directions through its 3×3, normalized).
            var world = frame(born);
            from = Vector3.Transform(offset, world);
            var turned = Vector3.TransformNormal(direction, world);
            var length = turned.Length();
            direction = length > 1e-20f ? turned / length : Vector3.Zero;
            carried = effect.Inherit != 0 ? effect.Inherit * (frame(born + 0.0005f).Translation - frame(born - 0.0005f).Translation) / 0.001f : Vector3.Zero;
        }
        else
        {
            var path = Path!;
            from = path(born) + offset;
            // The emitter's own velocity, for a trail's particles to carry some of.
            carried = effect.Inherit != 0 ? effect.Inherit * (path(born + 0.0005f) - path(born - 0.0005f)) / 0.001f : Vector3.Zero;
        }
        var velocity = direction * effect.Speed.At(Random(n, 2)) + carried;
        var (position, now) = Moved(from, velocity, effect.Gravity, effect.Drag, age);
        var share = age / life;
        var fade = effect.FadeOut > 0 ? 1 - SmoothStep(1 - effect.FadeOut, 1, share) : 1;
        var grow = effect.GrowIn > 0 ? SmoothStep(0, effect.GrowIn, share) : 1;
        var color = Colors.ToLinear(Vector4.Lerp(start, end, share));
        var angle = effect.Angle.At(Random(n, 8)) + effect.Spin.At(Random(n, 9)) * age;
        return new Particle(effect, position, now, effect.Size.At(Random(n, 1)) * grow * fade, angle, color with { W = 1 }, effect.Opacity * fade);
    }

    /// <summary>Where particle <paramref name="n"/> starts from the emitter and which way it goes.</summary>
    (Vector3 Offset, Vector3 Direction) Spawned(int n)
    {
        // Evenly over the sphere of directions: the height uniform, the turn about it uniform.
        Vector3 Around(float height, float turn)
        {
            var across = MathF.Sqrt(MathF.Max(0, 1 - height * height));
            var phi = 2 * MathF.PI * turn;
            return new Vector3(across * MathF.Cos(phi), across * MathF.Sin(phi), height);
        }
        switch (Effect.Spawn)
        {
            case ParticleSpawn.Sphere sphere:
            {
                var direction = Around(1 - 2 * Random(n, 3), Random(n, 4));
                // Evenly through the ball: the radius as the cube root.
                return (direction * sphere.Radius * MathF.Cbrt(Random(n, 5)), direction);
            }
            case ParticleSpawn.Cone cone:
            {
                var lowest = MathF.Cos(cone.HalfAngle * MathF.PI / 180);
                var local = Around(1 - (1 - lowest) * Random(n, 3), Random(n, 4));
                return (Vector3.Zero, Vector3.Transform(local, Turning(Vector3.UnitZ, Vector3.Normalize(cone.Axis))));
            }
            default:
                return (Vector3.Zero, Around(1 - 2 * Random(n, 3), Random(n, 4)));
        }
    }

    /// <summary>The rotation taking <paramref name="from"/> to <paramref name="to"/> (unit vectors).</summary>
    static Quaternion Turning(Vector3 from, Vector3 to)
    {
        var cos = Vector3.Dot(from, to);
        if (cos > 0.999999f)
            return Quaternion.Identity;
        if (cos < -0.999999f)
            return Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
        var axis = Vector3.Normalize(Vector3.Cross(from, to));
        return Quaternion.CreateFromAxisAngle(axis, MathF.Acos(cos));
    }

    /// <summary>
    /// Where a particle is after <paramref name="age"/> seconds, and its velocity: under gravity g and
    /// drag k (dv/dt = g − k·v), v = g/k + (v₀ − g/k)·e^(−k·t) and x = x₀ + g/k·t + (v₀ − g/k)·(1 − e^(−k·t))/k;
    /// without drag, x₀ + v₀·t + g·t²/2.
    /// </summary>
    internal static (Vector3 Position, Vector3 Velocity) Moved(Vector3 start, Vector3 velocity, Vector3 gravity, float drag, float age)
    {
        if (drag * age < 1e-4f)
            return (start + velocity * age + gravity * (age * age / 2), velocity + gravity * age);
        var drift = gravity / drag;
        var decay = MathF.Exp(-drag * age);
        return (start + drift * age + (velocity - drift) * ((1 - decay) / drag), drift + (velocity - drift) * decay);
    }

    internal static float SmoothStep(float from, float to, float x)
    {
        var t = Math.Clamp((x - from) / (to - from), 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>A number in [0, 1) for particle <paramref name="n"/>'s <paramref name="channel"/>, from the seed (SplitMix64's mixing).</summary>
    internal float Random(int n, int channel)
    {
        static ulong Mix(ulong z)
        {
            z += 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
        var bits = Mix(Mix(Mix((ulong)(uint)Seed) ^ (ulong)(uint)n) ^ (ulong)(uint)channel);
        return (bits >> 40) / 16777216f;
    }
}

/// <summary>
/// A particle as drawn (particle.vert): a quad in the view plane about its centre (world), along
/// <see cref="AxisU"/> and <see cref="AxisV"/> to <see cref="HalfU"/> and <see cref="HalfV"/> metres,
/// a pixel's width there (metres), its shape's measures in metres (<see cref="P0"/>, <see cref="P1"/>:
/// a disc's radius; a ring's outer and inner radius; a puff's radius and where its fade starts; a
/// spark's half length between its round ends and its radius; a shard's half length and half
/// width), its colour (linear), opacity, ink line width (pixels) and view depth.
/// </summary>
public readonly record struct ParticleQuad(Vector3 Centre, Vector3 AxisU, Vector3 AxisV, float HalfU, float HalfV, float Pixel, int Shape,
    float P0, float P1, Vector4 Color, float Opacity, float Outline, bool Glow, float Depth)
{
    public const int Disc = 0, Ring = 1, Spark = 2, Puff = 3, Shard = 4;

    /// <summary>
    /// <paramref name="particle"/> as <paramref name="camera"/> sees it on a target
    /// <paramref name="viewportHeight"/> pixels tall (aspect <paramref name="aspect"/>), its ink line
    /// at <paramref name="scale"/> (the device scale) times its effect's width.
    /// </summary>
    public static ParticleQuad Of(Particle particle, Camera camera, int viewportHeight, float scale)
    {
        var view = camera.View;
        Vector3 right = new(view.M11, view.M21, view.M31), up = new(view.M12, view.M22, view.M32);
        var depth = -Vector3.Transform(particle.Position, view).Z;
        var pixel = (camera.Projection == Projection.Orthographic ? camera.OrthographicHeight : 2 * MathF.Max(depth, 1e-6f) * MathF.Tan(camera.VerticalFieldOfView / 2)) / viewportHeight;
        var outline = particle.Effect.OutlineWidth * scale;
        var margin = (outline / 2 + 1) * pixel;
        var radius = particle.Size / 2;
        var angle = particle.Angle * MathF.PI / 180;
        Vector3 u = right * MathF.Cos(angle) + up * MathF.Sin(angle), v = -right * MathF.Sin(angle) + up * MathF.Cos(angle);
        (int Shape, float P0, float P1, float HalfU, float HalfV) measures;
        switch (particle.Effect.Shape)
        {
            case ParticleShape.Ring ring:
                measures = (Ring, radius, radius * (1 - ring.Thickness), radius, radius);
                break;
            case ParticleShape.Puff puff:
                measures = (Puff, radius, radius * (1 - puff.Softness), radius, radius);
                break;
            case ParticleShape.Shard shard:
                measures = (Shard, radius, radius * shard.Width, radius, radius * shard.Width);
                break;
            case ParticleShape.Spark spark:
            {
                // Along its motion across the view (the view plane's right and up).
                Vector2 across = new(Vector3.Dot(particle.Velocity, right), Vector3.Dot(particle.Velocity, up));
                var speed = across.Length();
                var along = speed > 1e-6f ? across / speed : Vector2.UnitX;
                u = right * along.X + up * along.Y;
                v = -right * along.Y + up * along.X;
                var round = particle.Size * spark.Width / 2;
                var half = MathF.Max(0, (particle.Size + spark.Stretch * speed) / 2 - round);
                measures = (Spark, half, round, half + round, round);
                break;
            }
            default:
                measures = (Disc, radius, 0, radius, radius);
                break;
        }
        return new ParticleQuad(particle.Position, u, v, measures.HalfU + margin, measures.HalfV + margin, pixel, measures.Shape,
            measures.P0, measures.P1, particle.Color, particle.Opacity, outline, particle.Effect.Glow, depth);
    }
}
