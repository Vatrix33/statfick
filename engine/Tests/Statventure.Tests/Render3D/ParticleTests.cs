using System.Numerics;
using System.Text;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;

namespace Statventure.Tests.Render3D;

/// <summary>
/// Particles computed from their age (Docs/Renderer-3D.md, "Particles"): motion, emitters, curves,
/// colours and how they're drawn; effects as records (<c>particle.*</c>, Docs/Scene.md, S5-1) through
/// both forms, their rules and loading from the game's data (Q25); and emitters on a frame (a node's
/// world, S5-3), spawning in its space at each birth as Blender's emitter objects do.
/// </summary>
public class ParticleTests
{
    static readonly Palette Colours = new([new("ember", "#ffd040"), new("ash", "#401010"), new("spark", "#ffffff")]);

    static ParticleEffect Effect(ParticleSpawn spawn) => new()
    {
        Shape = new ParticleShape.Disc(),
        StartColor = "ember",
        EndColor = "ash",
        Life = new(1, 2),
        Size = new(0.1f, 0.3f),
        Speed = new(2, 4),
        Spawn = spawn,
    };

    static void Near(Vector3 expected, Vector3 actual, float tolerance) =>
        Assert.True(Vector3.Distance(expected, actual) <= tolerance, $"expected {expected}, got {actual}");

    [Theory]
    [InlineData(0f)]
    [InlineData(0.8f)]
    [InlineData(3f)]
    public void TheClosedFormFollowsGravityAndDrag(float drag)
    {
        // Small steps of dv/dt = g − k·v (fourth-order Runge-Kutta) land where the closed form says.
        Vector3 start = new(1, 2, 3), velocity = new(4, 5, -2), gravity = new(0, -9.81f, 0.5f);
        Vector3 x = start, v = velocity;
        const int steps = 4000;
        const float age = 1.7f, h = age / steps;
        Vector3 A(Vector3 at) => gravity - drag * at;
        for (var i = 0; i < steps; i++)
        {
            Vector3 k1v = A(v), k1x = v;
            Vector3 k2v = A(v + k1v * (h / 2)), k2x = v + k1v * (h / 2);
            Vector3 k3v = A(v + k2v * (h / 2)), k3x = v + k2v * (h / 2);
            Vector3 k4v = A(v + k3v * h), k4x = v + k3v * h;
            x += (k1x + 2 * k2x + 2 * k3x + k4x) * (h / 6);
            v += (k1v + 2 * k2v + 2 * k3v + k4v) * (h / 6);
        }
        var (position, now) = ParticleEmitter.Moved(start, velocity, gravity, drag, age);
        Near(x, position, 1e-3f);
        Near(v, now, 1e-3f);
    }

    [Fact]
    public void TheSameSeedGivesTheSameParticles()
    {
        var emitter = new ParticleEmitter { Effect = Effect(new ParticleSpawn.Point()), Seed = 7, Path = ParticleEmitter.At(Vector3.Zero), Burst = 20 };
        var first = emitter.At(0.5f, Colours).ToArray();
        Assert.Equal(first, emitter.At(0.5f, Colours).ToArray());
        Assert.NotEqual(first, (emitter with { Seed = 8 }).At(0.5f, Colours).ToArray());
    }

    [Fact]
    public void BurstsAndStreamsReleaseTheirCountsAndParticlesDieAtTheirLife()
    {
        var effect = Effect(new ParticleSpawn.Point()) with { Life = 1 };
        var emitter = new ParticleEmitter { Effect = effect, Seed = 1, Path = ParticleEmitter.At(Vector3.Zero), Start = 2, Burst = 12, Rate = 10, Duration = 3 };
        Assert.Empty(emitter.At(1.9f, Colours));
        // At 2.55 s: the burst and the stream's first six (born 2.0 … 2.5).
        Assert.Equal(12 + 6, emitter.At(2.55f, Colours).Count());
        // At 3.55 s the burst is gone; the stream's particles born after 2.55 live (2.6 … 3.5).
        Assert.Equal(10, emitter.At(3.55f, Colours).Count());
        // After the stream (30 particles, the last born at 4.9) every particle is gone by 5.9.
        Assert.Single(emitter.At(5.85f, Colours));
        Assert.Empty(emitter.At(5.95f, Colours));
    }

    [Fact]
    public void RangesHoldAndSpawnsGoTheirWays()
    {
        Vector3 place = new(3, 1, -2), axis = Vector3.Normalize(new Vector3(1, 2, 0));
        var cone = new ParticleEmitter { Effect = Effect(new ParticleSpawn.Cone(axis * 5, 25)) with { Life = 10 }, Seed = 3, Path = ParticleEmitter.At(place), Burst = 500 };
        foreach (var p in cone.At(0.25f, Colours))
        {
            Assert.InRange(p.Size, 0.1f, 0.3f);
            var travelled = p.Position - place;
            Assert.InRange(travelled.Length(), 2 * 0.25f - 1e-4f, 4 * 0.25f + 1e-4f);
            Assert.True(Vector3.Dot(Vector3.Normalize(travelled), axis) >= MathF.Cos(25 * MathF.PI / 180) - 1e-5f);
        }
        // Spread over the cone, not bunched at its axis: about half beyond 17.6° (the half-area angle of a 25° cap).
        var wide = cone.At(0.25f, Colours).Count(p => Vector3.Dot(Vector3.Normalize(p.Position - place), axis) < MathF.Cos(17.6f * MathF.PI / 180));
        Assert.InRange(wide, 200, 300);
        var ball = new ParticleEmitter { Effect = Effect(new ParticleSpawn.Sphere(0.5f)) with { Speed = 0, Life = 10 }, Seed = 4, Path = ParticleEmitter.At(place), Burst = 500 };
        var offsets = ball.At(0, Colours).Select(p => (p.Position - place).Length()).ToArray();
        Assert.All(offsets, d => Assert.InRange(d, 0, 0.5f));
        // Evenly through the ball: an eighth within half the radius.
        Assert.InRange(offsets.Count(d => d < 0.25f), 40, 85);
        var point = new ParticleEmitter { Effect = Effect(new ParticleSpawn.Point()) with { Life = 10 }, Seed = 5, Path = ParticleEmitter.At(place), Burst = 500 };
        // Every way: about half go up.
        Assert.InRange(point.At(0.1f, Colours).Count(p => p.Position.Y > place.Y), 200, 300);
    }

    [Fact]
    public void ATrailLeavesItsParticlesWhereItsEmitterWas()
    {
        Vector3 Path(float t) => new(2 * t, 1, -t * t);
        var effect = Effect(new ParticleSpawn.Point()) with { Speed = 0, Life = 5 };
        var trail = new ParticleEmitter { Effect = effect, Seed = 2, Path = Path, Rate = 4, Duration = 2 };
        var particles = trail.At(1.9f, Colours).ToArray();
        Assert.Equal(8, particles.Length);
        for (var i = 0; i < particles.Length; i++)
            Near(Path(i / 4f), particles[i].Position, 1e-5f);
        // Carrying half the emitter's velocity (2, 0, −2t at birth).
        var carrying = (trail with { Effect = effect with { Inherit = 0.5f } }).At(1.5f, Colours).ToArray();
        for (var i = 0; i < carrying.Length; i++)
        {
            var born = i / 4f;
            Near(Path(born) + new Vector3(1, 0, -born) * (1.5f - born), carrying[i].Position, 1e-3f);
        }
    }

    [Fact]
    public void ParticlesGrowInShrinkAndFadeEasedAndBlendTheirColoursInSrgb()
    {
        var effect = Effect(new ParticleSpawn.Point()) with { Life = 2, Size = 0.4f, Speed = 0, GrowIn = 0.25f, FadeOut = 0.5f, Opacity = 0.8f };
        var emitter = new ParticleEmitter { Effect = effect, Seed = 9, Path = ParticleEmitter.At(Vector3.Zero), Burst = 1 };
        Particle At(float t) => emitter.At(t, Colours).Single();
        Assert.Equal(0, At(0).Size);
        // A quarter into the growth: smoothstep(0.25) = 0.15625.
        Assert.Equal(0.4f * 0.15625f, At(0.125f).Size, 1e-5f);
        Assert.Equal(0.4f, At(0.75f).Size, 1e-5f);
        Assert.Equal(0.8f, At(0.75f).Opacity, 1e-5f);
        // Three quarters into the fade (1.75 s): 1 − smoothstep(0.75) = 0.15625, for size and opacity.
        Assert.Equal(0.4f * 0.15625f, At(1.75f).Size, 1e-5f);
        Assert.Equal(0.8f * 0.15625f, At(1.75f).Opacity, 1e-5f);
        // Halfway through its life the colour is halfway between the two in sRGB.
        var middle = Colors.ToSrgb(At(1).Color) * 255;
        Near(new Vector3((0xff + 0x40) / 2f, (0xd0 + 0x10) / 2f, (0x40 + 0x10) / 2f), new Vector3(middle.X, middle.Y, middle.Z), 0.01f);
    }

    [Fact]
    public void QuadsFaceTheCameraAndSparksLieAlongTheirMotion()
    {
        var camera = new Camera { Projection = Projection.Perspective, Position = new(0, 0, 10), Target = Vector3.Zero, VerticalFieldOfView = MathF.PI / 2 };
        var effect = Effect(new ParticleSpawn.Point()) with { Shape = new ParticleShape.Spark(0.2f, 0.5f), OutlineWidth = 2 };
        // 5 m from the camera, a 90° view 200 px tall: a pixel is 2·5·tan 45° / 200 = 0.05 m.
        var spark = ParticleQuad.Of(new Particle(effect, new(0, 0, 5), new(3, 4, 7), 1, 0, Vector4.One, 1), camera, 200, 1.5f);
        Assert.Equal(5, spark.Depth, 1e-4f);
        Assert.Equal(0.05f, spark.Pixel, 1e-6f);
        // Across the view it moves (3, 4): 5 m/s; length 1 + 0.5·5 = 3.5 m, radius 0.1 m.
        Near(new Vector3(0.6f, 0.8f, 0), spark.AxisU, 1e-5f);
        Near(new Vector3(-0.8f, 0.6f, 0), spark.AxisV, 1e-5f);
        Assert.Equal(1.75f - 0.1f, spark.P0, 1e-5f);
        Assert.Equal(0.1f, spark.P1, 1e-5f);
        // The quad reaches half the line (3 px at scale 1.5) and a pixel beyond the shape.
        Assert.Equal(3, spark.Outline, 1e-5f);
        Assert.Equal(1.75f + 2.5f * 0.05f, spark.HalfU, 1e-5f);
        var map = new Camera { Projection = Projection.Orthographic, Position = new(0, 30, 0.001f), Target = Vector3.Zero, OrthographicHeight = 20 };
        var shard = ParticleQuad.Of(new Particle(effect with { Shape = new ParticleShape.Shard(0.25f) }, Vector3.Zero, Vector3.Zero, 2, 90, Vector4.One, 1), map, 400, 1);
        Assert.Equal(0.05f, shard.Pixel, 1e-6f);
        Assert.Equal((ParticleQuad.Shard, 1f, 0.25f), (shard.Shape, shard.P0, shard.P1));
        // Turned a quarter: its long axis runs up the view.
        Near(new Vector3((map.View.M12), map.View.M22, map.View.M32), shard.AxisU, 1e-5f);
    }

    // ---------- Effects as records (S5-1) ----------

    /// <summary>Every field of the effect away from its default, a spark in a cone (the fixture's).</summary>
    internal static ParticleEffect Full() => new()
    {
        Shape = new ParticleShape.Spark(0.35f, 0.125f), StartColor = "spark", EndColor = "ember", Opacity = 0.75f, OutlineWidth = 1.5f, Glow = true,
        Life = new(0.5f, 1.25f), Size = new(0.0625f, 0.125f), Speed = new(1.5f, 3), Spawn = new ParticleSpawn.Cone(new(0.25f, 1, -0.5f), 35),
        GrowIn = 0.125f, FadeOut = 0.375f, Gravity = new(0.5f, -4, 0.25f), Drag = 0.75f, Angle = new(-30, 45), Spin = new(-90, 120), Inherit = 0.5f,
    };

    /// <summary>Only the required fields, a disc from a point.</summary>
    internal static ParticleEffect Defaults() => new()
    {
        Shape = new ParticleShape.Disc(), StartColor = "spark", EndColor = "ash", Life = 1, Size = 0.1f, Speed = 0, Spawn = new ParticleSpawn.Point(),
    };

    /// <summary>The fixtures: every shape and spawn in one of them.</summary>
    static readonly (string Label, ParticleEffect Effect)[] Fixtures =
    [
        ("every-field", Full()),
        ("defaults", Defaults()),
        ("ring-sphere", Defaults() with { Shape = new ParticleShape.Ring(0.25f), Spawn = new ParticleSpawn.Sphere(0.5f) }),
        ("puff", Defaults() with { Shape = new ParticleShape.Puff(0.625f) }),
        ("shard", Defaults() with { Shape = new ParticleShape.Shard(0.3125f) }),
    ];

    [Fact]
    public void TheFixturesAreRecorded()
    {
        foreach (var (label, effect) in Fixtures)
            RecordFixtures.Ensure(label, effect);
    }

    /// <summary>The effects of the reference scenes (Scenes3D), each keeping the rules.</summary>
    static IEnumerable<ParticleEffect> SceneEffects() => Scenes3D.All.SelectMany(s => s.Particles).Select(e => e.Effect);

    /// <summary>Both forms give every effect back field for field (floats bit for bit), the fixtures' and the reference scenes'.</summary>
    [Fact]
    public void EffectsRoundTripThroughBothForms()
    {
        var effects = Fixtures.Select(f => f.Effect).Concat(SceneEffects()).ToArray();
        Assert.True(effects.Length >= 10);
        foreach (var effect in effects)
        {
            Assert.Null(effect.Problem());
            foreach (var read in new[] { RecordSerializer.FromBinary<ParticleEffect>(RecordSerializer.ToBinary(effect)), RecordSerializer.FromJson<ParticleEffect>(RecordSerializer.ToJson(effect)) })
            {
                RecordAssert.Equal(effect, read);
                Assert.Equal(effect, read);
            }
        }
    }

    /// <summary>Written with only the required fields, an effect has M6's defaults: opacity 1, no ink line or glow, no growing in or fading, no gravity, drag, angle, spin or inherited velocity.</summary>
    [Fact]
    public void AbsentFieldsAreM6sDefaults()
    {
        const string json = """
            {"$types": {"particle.effect": 1, "particle.shape": 1, "particle.disc": 1, "particle.range": 1, "particle.spawn": 1, "particle.point": 1},
             "shape": {"$type": "particle.disc"}, "startColor": "spark", "endColor": "ash",
             "life": {"min": 1, "max": 1}, "size": {"min": 0.1, "max": 0.1}, "speed": {"min": 0, "max": 0}, "spawn": {"$type": "particle.point"}}
            """;
        var read = RecordSerializer.FromJson<ParticleEffect>(Encoding.UTF8.GetBytes(json));
        RecordAssert.Equal(Defaults(), read);
        Assert.Equal((1f, 0f, false, 0f, 0f, Vector3.Zero, 0f, default(ParticleRange), default(ParticleRange), 0f),
            (read.Opacity, read.OutlineWidth, read.Glow, read.GrowIn, read.FadeOut, read.Gravity, read.Drag, read.Angle, read.Spin, read.Inherit));
    }

    public static TheoryData<string> BrokenEffects => [.. Broken.Keys];

    /// <summary>Each rule broken alone, and the problem named.</summary>
    static readonly Dictionary<string, (Func<ParticleEffect, ParticleEffect> Break, string Problem)> Broken = new()
    {
        ["no shape"] = (e => e with { Shape = null! }, "it has no shape"),
        ["no spawn"] = (e => e with { Spawn = null! }, "it has no spawn"),
        ["no start colour"] = (e => e with { StartColor = "" }, "its start and end colours must name palette slots"),
        ["no end colour"] = (e => e with { EndColor = "" }, "its start and end colours must name palette slots"),
        ["ring 0"] = (e => e with { Shape = new ParticleShape.Ring(0) }, "a ring's thickness (0) must be above 0 and at most 1"),
        ["ring over 1"] = (e => e with { Shape = new ParticleShape.Ring(1.5f) }, "a ring's thickness (1.5) must be above 0 and at most 1"),
        ["spark width"] = (e => e with { Shape = new ParticleShape.Spark(0, 0.1f) }, "a spark's width (0) must be above 0 and at most 1"),
        ["spark stretch"] = (e => e with { Shape = new ParticleShape.Spark(0.5f, -0.1f) }, "a spark's stretch (-0.1 s) must be a finite number, 0 or more"),
        ["spark stretch infinite"] = (e => e with { Shape = new ParticleShape.Spark(0.5f, float.PositiveInfinity) }, "a spark's stretch (Infinity s) must be a finite number, 0 or more"),
        ["puff"] = (e => e with { Shape = new ParticleShape.Puff(float.NaN) }, "a puff's softness (NaN) must be above 0 and at most 1"),
        ["shard"] = (e => e with { Shape = new ParticleShape.Shard(-0.25f) }, "a shard's width (-0.25) must be above 0 and at most 1"),
        ["sphere"] = (e => e with { Spawn = new ParticleSpawn.Sphere(-1) }, "a sphere's radius (-1 m) must be a finite number, 0 or more"),
        ["sphere infinite"] = (e => e with { Spawn = new ParticleSpawn.Sphere(float.PositiveInfinity) }, "a sphere's radius (Infinity m) must be a finite number, 0 or more"),
        ["cone axis"] = (e => e with { Spawn = new ParticleSpawn.Cone(Vector3.Zero, 10) }, "a cone's axis (0, 0, 0) must be a finite direction, not zero"),
        ["cone axis NaN"] = (e => e with { Spawn = new ParticleSpawn.Cone(new(0, float.NaN, 1), 10) }, "a cone's axis (0, NaN, 1) must be a finite direction, not zero"),
        ["cone angle"] = (e => e with { Spawn = new ParticleSpawn.Cone(Vector3.UnitY, 181) }, "a cone's half angle (181°) must be from 0 to 180"),
        ["cone angle negative"] = (e => e with { Spawn = new ParticleSpawn.Cone(Vector3.UnitY, -1) }, "a cone's half angle (-1°) must be from 0 to 180"),
        ["life order"] = (e => e with { Life = new(2, 1) }, "its life's min (2) is above its max (1)"),
        ["life negative"] = (e => e with { Life = new(-1, 1) }, "its life (-1–1) must be 0 or more"),
        ["life infinite"] = (e => e with { Life = new(1, float.PositiveInfinity) }, "its life (1–Infinity) must be finite numbers"),
        ["size negative"] = (e => e with { Size = -0.5f }, "its size (-0.5–-0.5) must be 0 or more"),
        ["speed negative"] = (e => e with { Speed = new(-1, 2) }, "its speed (-1–2) must be 0 or more"),
        ["angle order"] = (e => e with { Angle = new(10, -10) }, "its angle's min (10) is above its max (-10)"),
        ["spin NaN"] = (e => e with { Spin = new(float.NaN, 1) }, "its spin (NaN–1) must be finite numbers"),
        ["opacity"] = (e => e with { Opacity = 1.25f }, "its opacity (1.25) must be from 0 to 1"),
        ["opacity negative"] = (e => e with { Opacity = -0.25f }, "its opacity (-0.25) must be from 0 to 1"),
        ["grow in"] = (e => e with { GrowIn = 1.5f }, "its grow-in (1.5) and fade-out (0) must be shares of its life, from 0 to 1"),
        ["fade out"] = (e => e with { FadeOut = -0.5f }, "its grow-in (0) and fade-out (-0.5) must be shares of its life, from 0 to 1"),
        ["outline"] = (e => e with { OutlineWidth = -1 }, "its ink line's width (-1) must be a finite number, 0 or more"),
        ["outline infinite"] = (e => e with { OutlineWidth = float.PositiveInfinity }, "its ink line's width (Infinity) must be a finite number, 0 or more"),
        ["drag"] = (e => e with { Drag = -0.5f }, "its drag (-0.5) must be a finite number, 0 or more"),
        ["drag NaN"] = (e => e with { Drag = float.NaN }, "its drag (NaN) must be a finite number, 0 or more"),
        ["drag infinite"] = (e => e with { Drag = float.PositiveInfinity }, "its drag (Infinity) must be a finite number, 0 or more"),
        ["gravity"] = (e => e with { Gravity = new(0, float.NegativeInfinity, 0) }, "its gravity (0, -Infinity, 0) must be finite"),
        ["inherit"] = (e => e with { Inherit = float.NaN }, "its inherited share of the emitter's velocity (NaN) must be a finite number"),
    };

    [Theory]
    [MemberData(nameof(BrokenEffects))]
    public void EachRuleRefusesWhatBreaksIt(string name)
    {
        var (breaking, problem) = Broken[name];
        Assert.Equal(problem, breaking(Defaults()).Problem());
    }

    /// <summary>The rules' edges are kept: proportions of exactly 1, a zero radius, stretch, life, size, speed, drag and ink line, a cone of 0° and of 180°, opacity and shares of 0 and 1, negative angles, spins, gravity and inherited velocity.</summary>
    [Fact]
    public void TheRulesEdgesAreKept()
    {
        var edges = new[]
        {
            Defaults() with { Shape = new ParticleShape.Ring(1) },
            Defaults() with { Shape = new ParticleShape.Spark(1, 0) },
            Defaults() with { Shape = new ParticleShape.Puff(1) },
            Defaults() with { Shape = new ParticleShape.Shard(1) },
            Defaults() with { Spawn = new ParticleSpawn.Sphere(0) },
            Defaults() with { Spawn = new ParticleSpawn.Cone(new(0, 0, -3), 0) },
            Defaults() with { Spawn = new ParticleSpawn.Cone(new(1e-30f, 0, 0), 180) },
            Defaults() with { Life = 0, Size = 0, Speed = 0, Drag = 0, OutlineWidth = 0, Opacity = 0, GrowIn = 0, FadeOut = 0 },
            Defaults() with { Opacity = 1, GrowIn = 1, FadeOut = 1, Angle = new(-180, -90), Spin = -30, Gravity = new(-1, -9.81f, -2), Inherit = -2 },
        };
        Assert.All(edges, e => Assert.Null(e.Problem()));
    }

    /// <summary>
    /// An effect loads from a pack's record entry (<c>models/effects/&lt;name&gt;</c> in models.pak, Q25) and
    /// from a loose JSON file; one that breaks a rule is refused with the pack and entry, or the file,
    /// named and the rule's message.
    /// </summary>
    [Fact]
    public void EffectsLoadFromPacksAndLooseFilesAndBrokenOnesAreRefusedNamingTheFile()
    {
        const string expected = "not a valid particle effect: its life's min (2) is above its max (1)";
        var broken = Defaults() with { Life = new(2, 1) };
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("models/effects/sparks", Full());
        writer.AddRecord("models/effects/broken", broken);
        writer.Write(folder["models.pak"]);
        using (var packed = new VirtualFileSystem([Mount.Pack(folder["models.pak"])]))
        {
            RecordAssert.Equal(Full(), ParticleEffect.Load(packed, "models/effects/sparks"));
            RecordAssert.Equal(Full(), packed.Load<ParticleEffect>("models/effects/sparks"));
            var problem = Assert.Throws<DataFileException>(() => ParticleEffect.Load(packed, "models/effects/broken"));
            Assert.Equal((Path.GetFullPath(folder["models.pak"]), "models/effects/broken", expected), (problem.File, problem.Entry, problem.Problem));
        }
        folder.Write("dev/models/effects/sparks.json", RecordSerializer.ToJson(Full()));
        var file = folder.Write("dev/models/effects/broken.json", RecordSerializer.ToJson(broken));
        using var loose = new VirtualFileSystem([Mount.Folder(folder["dev"])]);
        RecordAssert.Equal(Full(), ParticleEffect.Load(loose, "models/effects/sparks"));
        var looseProblem = Assert.Throws<DataFileException>(() => ParticleEffect.Load(loose, "models/effects/broken"));
        Assert.Equal((Path.GetFullPath(file), (string?)null, expected), (looseProblem.File, looseProblem.Entry, looseProblem.Problem));
    }

    // ---------- Emitters on a frame (S5-3) ----------

    /// <summary>How many particles an emitter releases, and when the last of them has died.</summary>
    [Fact]
    public void AnEmittersCountAndEndAreItsLastParticles()
    {
        var effect = Effect(new ParticleSpawn.Point()) with { Life = new(1, 2) };
        var emitter = new ParticleEmitter { Effect = effect, Seed = 1, Path = ParticleEmitter.At(Vector3.Zero), Start = 0.5f, Burst = 3, Rate = 4, Duration = 1.1f };
        // floor(4 × 1.1) = 4 streamed, the last born at 0.5 + 3/4; its life at most 2.
        Assert.Equal((7, 0.5f + 0.75f + 2), (emitter.Count, emitter.End));
        Assert.Empty(emitter.At(emitter.End, Colours));
        // With every life the longest, the last lives until just before.
        var longest = emitter with { Effect = effect with { Life = 2 } };
        Assert.Equal(emitter.End, longest.End);
        Assert.Single(longest.At(longest.End - 0.01f, Colours));
        Assert.Empty(longest.At(longest.End, Colours));
        Assert.Equal((3, 2.5f), ((emitter with { Rate = 0 }).Count, (emitter with { Rate = 0 }).End));
        Assert.Equal((0, 0.5f), ((emitter with { Rate = 0, Burst = 0 }).Count, (emitter with { Rate = 0, Burst = 0 }).End));
        Assert.Throws<InvalidOperationException>(() => (emitter with { Path = null }).At(1, Colours).ToArray());
    }

    /// <summary>
    /// An emitter on a frame (a node's world at each time) spawns in its space at each particle's birth,
    /// as Blender's <c>psys_get_birth_coords</c>: the spawn's offset through the matrix (turned, scaled,
    /// placed), its direction through the matrix's 3×3, normalized, at the effect's speed (sizes and
    /// speeds in world units), and a trail's share of the frame's velocity; after its birth a particle
    /// moves in world space, whatever the frame does. A frame that maps directions to nothing gives no
    /// direction (Blender's normalize of a zero vector).
    /// </summary>
    [Fact]
    public void AnEmitterOnAFrameSpawnsInItsSpaceAtEachBirth()
    {
        // Turning about Y a quarter a second and moving along X, scaled 2.
        Matrix4x4 Frame(float t) => Matrix4x4.CreateScale(2) * Matrix4x4.CreateRotationY(MathF.PI / 2 * t) * Matrix4x4.CreateTranslation(3 * t, 1, 0);
        var axis = new Vector3(0, 0, 1);
        var effect = Effect(new ParticleSpawn.Cone(axis, 0)) with { Speed = 2, Life = 10 };
        var world = new ParticleEmitter { Effect = effect, Seed = 6, Path = ParticleEmitter.At(Vector3.Zero), Rate = 4, Duration = 1 };
        var framed = world with { Path = null, Frame = Frame };
        var particles = framed.At(1.5f, Colours).ToArray();
        Assert.Equal(4, particles.Length);
        for (var i = 0; i < particles.Length; i++)
        {
            var born = i / 4f;
            // A 0° cone along +Z in the frame: +Z turned by the frame at birth; the speed 2 as the effect says.
            var along = Vector3.Normalize(Vector3.TransformNormal(axis, Frame(born)));
            Near(Frame(born).Translation + along * 2 * (1.5f - born), particles[i].Position, 1e-4f);
            Near(along * 2, particles[i].Velocity, 1e-4f);
        }
        // In world space the same cone goes along +Z from the path.
        Assert.All(world.At(1.5f, Colours), p => Near(Vector3.UnitZ * 2, p.Velocity, 1e-5f));
        // A ball's offsets go through the matrix, its scale included: within twice the radius of the place.
        var ball = framed with { Effect = Effect(new ParticleSpawn.Sphere(0.5f)) with { Speed = 0, Life = 10 }, Rate = 0, Burst = 200 };
        var offsets = ball.At(0, Colours).Select(p => p.Position - Frame(0).Translation).ToArray();
        Assert.All(offsets, o => Assert.InRange(o.Length(), 0, 1 + 1e-5f));
        Assert.Contains(offsets, o => o.Length() > 0.5f);
        // A trail's share of the frame's velocity (3 along X, the frame's translation's).
        var trail = framed with { Effect = effect with { Speed = 0, Inherit = 0.5f } };
        Assert.All(trail.At(1.5f, Colours), p => Near(new Vector3(1.5f, 0, 0), p.Velocity, 1e-3f));
        // A frame scaled to nothing: no direction, the particles stay where they're born.
        var flat = framed with { Frame = t => Matrix4x4.CreateScale(0) * Matrix4x4.CreateTranslation(1, 2, 3) };
        Assert.All(flat.At(0.9f, Colours), p => Assert.Equal((new Vector3(1, 2, 3), Vector3.Zero), (p.Position, p.Velocity)));
    }
}
