using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Tests.Render3D;

/// <summary>Particles computed from their age (Docs/Renderer-3D.md, "Particles"): motion, emitters, curves, colours and how they're drawn.</summary>
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
}
