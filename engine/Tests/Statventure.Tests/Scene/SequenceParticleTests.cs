using System.Numerics;
using Statventure.Imaging;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;

namespace Statventure.Tests.Scene;

/// <summary>Effects and emitters for the particle tests.</summary>
static class Emitters
{
    public static readonly Vector4 Grey = new(0.5f, 0.5f, 0.5f, 1);

    /// <summary>Discs that don't move, live <paramref name="life"/> seconds and keep their colour: each stays where it was born.</summary>
    public static ParticleEffect Still(float life = 10, string colour = "cloth") => new()
    {
        Shape = new ParticleShape.Disc(), StartColor = colour, EndColor = colour, Life = life, Size = 0.1f, Speed = 0, Spawn = new ParticleSpawn.Point(),
    };

    public static void Near(Vector3 expected, Vector3 actual, float tolerance = 1e-5f) =>
        Assert.True(Vector3.Distance(expected, actual) <= tolerance, $"expected {expected}, got {actual}");

    /// <summary>The particles of a still effect by their birth (a stream of <paramref name="rate"/> a second from 0): particle n born at n / rate.</summary>
    public static float[] Births(IEnumerable<Particle> particles, float rate) => [.. particles.Select((_, n) => n / rate)];
}

/// <summary>
/// Emitters on scene nodes (Docs/Scene.md, "How it works", step 5; S5-3, S5-4, S5-Q1): the game's,
/// started with <c>node.Attach(new SceneEmitter(effect) { … })</c>: their first frame at their start,
/// then on the scene's clock at its rate (the battle pace, as the figures); each particle born where
/// the node is at its birth (between frames from the world between theirs), its spawn in the node's
/// space, then moving in world space; time set is a jump; a hidden node's emitter draws nothing; one
/// whose last particle has died is taken off; the colours of its palette or of what its node belongs
/// to; the view's particles given only when they change, and left alone in a scene without emitters.
/// </summary>
public class SceneEmitterTests
{
    static SceneView NewScene() => new(new View3D(null!));

    [Fact]
    public void AnEmittersFirstFrameIsItsStartThenItRunsAtTheScenesRate()
    {
        var scene = NewScene();
        scene.Rate = 0.5;
        var emitter = scene.Root.Add("fx").Attach(new SceneEmitter(Emitters.Still()) { Burst = 3, Rate = 10, Duration = 1 });
        scene.Advance(0.1);
        Assert.Equal(0, emitter.Time);
        // The burst and the stream's first, born at 0.
        Assert.Equal(4, scene.View.Particles.Count);
        scene.Advance(0.2);
        Assert.Equal(0.1, emitter.Time, 1e-12);
        Assert.Equal(5, scene.View.Particles.Count);
        scene.Rate = 1.5;
        scene.Advance(0.2);
        Assert.Equal(0.4, emitter.Time, 1e-12);
        // Held at rate 0: the same particles, the view not given them again.
        scene.Rate = 0;
        var shown = scene.View.Particles;
        scene.Advance(0.5);
        Assert.Equal(0.4, emitter.Time, 1e-12);
        Assert.Same(shown, scene.View.Particles);
        // Particles that only move (none born or dying) are given again.
        var moving = NewScene();
        moving.Root.Add("fx").Attach(new SceneEmitter(Emitters.Still() with { Speed = 1 }) { Burst = 2 });
        moving.Advance(0);
        var first = moving.View.Particles;
        moving.Advance(0.1);
        Assert.Equal(2, moving.View.Particles.Count);
        Assert.NotSame(first, moving.View.Particles);
        Assert.NotEqual(first[0].Position, moving.View.Particles[0].Position);
        // Its time set before its first frame starts its clock there.
        var set = moving.Root.Add("set").Attach(new SceneEmitter(Emitters.Still()) { Burst = 1 });
        set.Time = 0.5;
        moving.Advance(0.1);
        Assert.Equal(0.6, set.Time, 1e-12);
    }

    /// <summary>
    /// A node moving along X at 2 m/s, a still stream of 25 a second (every 0.04 s, between the
    /// frames of 0.1 s): each particle where the node was at its birth, interpolated between the frames;
    /// moved away afterwards, the node takes none of them along. A node turning about Y with a cone
    /// along its +Z: each particle goes along the node's +Z at its birth; a node scaled 2 with a ball:
    /// offsets twice the radius.
    /// </summary>
    [Fact]
    public void ParticlesAreBornWhereTheNodeIsInItsSpaceAndKeepTheirWorldMotion()
    {
        var scene = NewScene();
        var node = scene.Root.Add("runner");
        scene.Advance(0);
        node.Attach(new SceneEmitter(Emitters.Still()) { Rate = 25, Duration = 2 });
        for (var k = 0; k <= 5; k++)
        {
            node.Translation = new(2 * k * 0.1f, 1, 0);
            scene.Advance(k == 0 ? 0 : 0.1);
        }
        var particles = scene.View.Particles.ToArray();
        Assert.Equal(13, particles.Length);
        for (var n = 0; n < particles.Length; n++)
            Emitters.Near(new Vector3(2 * n / 25f, 1, 0), particles[n].Position);
        node.Translation = new(-40, 5, 3);
        scene.Advance(0.1);
        for (var n = 0; n < 13; n++)
            Emitters.Near(new Vector3(2 * n / 25f, 1, 0), scene.View.Particles[n].Position);

        var turning = NewScene();
        var pivot = turning.Root.Add("pivot");
        pivot.Translation = new(0, 0, -2);
        var cone = Emitters.Still() with { Speed = 3, Spawn = new ParticleSpawn.Cone(Vector3.UnitZ, 0) };
        turning.Advance(0);
        pivot.Attach(new SceneEmitter(cone) { Rate = 10, Duration = 1 });
        for (var k = 0; k <= 4; k++)
        {
            pivot.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.4f * k);
            turning.Advance(k == 0 ? 0 : 0.1);
        }
        var turned = turning.View.Particles.ToArray();
        Assert.Equal(5, turned.Length);
        for (var n = 0; n < turned.Length; n++)
        {
            var along = Vector3.Transform(Vector3.UnitZ, Quaternion.CreateFromAxisAngle(Vector3.UnitY, 0.4f * n));
            Emitters.Near(along * 3, turned[n].Velocity, 1e-4f);
            Emitters.Near(new Vector3(0, 0, -2) + along * 3 * (0.4f - n * 0.1f), turned[n].Position, 1e-4f);
        }

        // A short stream on fine frames: the node is recorded until its last birth (0.4 s) is covered.
        var fine = NewScene();
        var dart = fine.Root.Add("dart");
        fine.Advance(0);
        dart.Attach(new SceneEmitter(Emitters.Still()) { Rate = 10, Duration = 0.5f });
        for (var k = 0; k <= 30; k++)
        {
            dart.Translation = new(2 * k * 0.02f, 0, 0);
            fine.Advance(k == 0 ? 0 : 0.02);
        }
        Assert.Equal(5, fine.View.Particles.Count);
        for (var n = 0; n < 5; n++)
            Emitters.Near(new Vector3(2 * n / 10f, 0, 0), fine.View.Particles[n].Position, 1e-4f);
        // A trail carries its share of the node's velocity (2 m/s, half of it), from the worlds recorded.
        var trail = NewScene();
        var mover = trail.Root.Add("mover");
        trail.Advance(0);
        mover.Attach(new SceneEmitter(Emitters.Still() with { Inherit = 0.5f }) { Rate = 25, Duration = 1 });
        for (var k = 0; k <= 5; k++)
        {
            mover.Translation = new(2 * k * 0.1f, 0, 0);
            trail.Advance(k == 0 ? 0 : 0.1);
        }
        Assert.All(trail.View.Particles.Skip(1), p => Emitters.Near(new Vector3(1, 0, 0), p.Velocity, 1e-3f));
        // Before anything is recorded its particles are where its node is now.
        var early = new SceneNode("early") { Translation = new(3, 4, 5) }.Attach(new SceneEmitter(Emitters.Still()) { Burst = 2 });
        Assert.All(early.Emitter.At(0, new Palette()), p => Assert.Equal(new Vector3(3, 4, 5), p.Position));

        var scaled = NewScene();
        var big = scaled.Root.Add("big");
        big.Scale = new(2);
        big.Translation = new(1, 1, 1);
        big.Attach(new SceneEmitter(Emitters.Still() with { Spawn = new ParticleSpawn.Sphere(0.5f) }) { Burst = 300 });
        scaled.Advance(0);
        var offsets = scaled.View.Particles.Select(p => (p.Position - new Vector3(1)).Length()).ToArray();
        Assert.All(offsets, d => Assert.InRange(d, 0, 1 + 1e-5f));
        Assert.Contains(offsets, d => d > 0.6f);
    }

    /// <summary>Time set is a jump: particles born in the stretch jumped over start where the node is after it; set back, what lay after is forgotten and recorded again as the clock passes it.</summary>
    [Fact]
    public void SettingTheTimeIsAJump()
    {
        var scene = NewScene();
        var node = scene.Root.Add("runner");
        var emitter = new SceneEmitter(Emitters.Still()) { Rate = 10, Duration = 3 };
        node.Attach(emitter);
        for (var k = 0; k <= 5; k++)
        {
            node.Translation = new(k * 0.1f, 0, 0);
            scene.Advance(k == 0 ? 0 : 0.1);
        }
        node.Translation = new(10, 0, 0);
        emitter.Time = 1.5;
        scene.Advance(0);
        var births = scene.View.Particles.Select(p => p.Position.X).ToArray();
        Assert.Equal(16, births.Length);
        for (var n = 0; n <= 5; n++)
            Assert.Equal(n * 0.1f, births[n], 1e-5f);
        for (var n = 6; n < 16; n++)
            Assert.Equal(10, births[n], 1e-5f);
        // Back to 0.8: the births up to 0.5 as before, after it from the world now; on from there, recorded again.
        node.Translation = new(8, 0, 0);
        emitter.Time = 0.8;
        scene.Advance(0);
        node.Translation = new(9, 0, 0);
        scene.Advance(0.1);
        var again = scene.View.Particles.Select(p => p.Position.X).ToArray();
        Assert.Equal(10, again.Length);
        Assert.Equal(0.5f, again[5], 1e-5f);
        Assert.Equal(new[] { 8f, 8f, 8f, 9f }, again[6..], new FloatComparer(1e-4f));
        Assert.Throws<ArgumentOutOfRangeException>(() => emitter.Time = double.NaN);
    }

    /// <summary>Set back into a stretch that was watched, the births between the record before and now come between that world and the node's now.</summary>
    [Fact]
    public void GoingBackIntoAWatchedStretchInterpolatesFromTheRecordBefore()
    {
        var scene = NewScene();
        var node = scene.Root.Add("runner");
        var emitter = node.Attach(new SceneEmitter(Emitters.Still()) { Rate = 40, Duration = 1 });
        for (var k = 0; k <= 5; k++)
        {
            node.Translation = new(10 * k * 0.1f, 0, 0);
            scene.Advance(k == 0 ? 0 : 0.1);
        }
        node.Translation = new(2.5f, 0, 0);
        emitter.Time = 0.25;
        scene.Advance(0);
        var xs = scene.View.Particles.Select(p => p.Position.X).ToArray();
        Assert.Equal(11, xs.Length);
        Assert.Equal(new[] { 2f, 2.25f, 2.5f }, xs[8..], new FloatComparer(1e-4f));
    }

    /// <summary>On a hidden node (Q23) an emitter draws nothing while its particles age; shown again, they show as they are then.</summary>
    [Fact]
    public void AnEmitterOnAHiddenNodeDrawsNothingWhileItsParticlesAge()
    {
        var scene = NewScene();
        var holder = scene.Root.Add("holder");
        var node = holder.Add("fx");
        var emitter = node.Attach(new SceneEmitter(Emitters.Still(life: 1)) { Rate = 10, Duration = 2 });
        scene.Advance(0);
        scene.Advance(0.25);
        Assert.Equal(3, scene.View.Particles.Count);
        holder.Hidden = true;
        scene.Advance(0.5);
        Assert.Empty(scene.View.Particles);
        holder.Hidden = false;
        scene.Advance(0.5);
        Assert.Equal(1.25, emitter.Time, 1e-12);
        // Those born after 0.25 s that still live (life 1 s): 0.3 … 1.2.
        Assert.Equal(10, scene.View.Particles.Count);
    }

    /// <summary>
    /// Once its last particle has died the scene takes an emitter off its node (fire and forget), and the
    /// view's particles are emptied once; a scene without emitters leaves the view's particles as the
    /// game sets them, and doesn't give them again when they don't change.
    /// </summary>
    [Fact]
    public void AFinishedEmitterIsTakenOffAndAViewWithoutEmittersIsLeftAlone()
    {
        var scene = NewScene();
        var own = new[] { new Particle(Emitters.Still(), Vector3.One, Vector3.Zero, 1, 0, Vector4.One, 1) };
        scene.View.Particles = own;
        scene.Advance(0.1);
        Assert.Same(own, scene.View.Particles);
        var node = scene.Root.Add("fx");
        var emitter = node.Attach(new SceneEmitter(Emitters.Still(life: 0.5f)) { Burst = 2, Rate = 4, Duration = 0.5f });
        scene.Advance(0.1);
        Assert.Equal(3, scene.View.Particles.Count);
        // The last born at 0.25, dead at 0.75.
        scene.Advance(0.7);
        Assert.Same(node, emitter.Node);
        Assert.False(emitter.Done);
        scene.Advance(0.05);
        Assert.True(emitter.Done);
        Assert.Null(emitter.Node);
        Assert.Empty(node.Attachments);
        Assert.Empty(scene.View.Particles);
        var emptied = scene.View.Particles;
        scene.Advance(0.1);
        Assert.Same(emptied, scene.View.Particles);
        scene.View.Particles = own;
        scene.Advance(0.1);
        Assert.Same(own, scene.View.Particles);
        // One with nothing to release is done at once.
        var nothing = scene.Root.Add("none").Attach(new SceneEmitter(Emitters.Still()));
        scene.Advance(0.1);
        Assert.Null(nothing.Node);
    }

    /// <summary>
    /// Its colours (S5-4): its own palette (a slot it lacks M6's grey); without one, those of what its
    /// node belongs to, the nearest instanced model at or above it: its palette, then its materials' own
    /// colours, then grey; with nothing above it, grey.
    /// </summary>
    [Fact]
    public void ItsColoursAreItsPalettesOrThoseOfWhatItsNodeBelongsTo()
    {
        var scene = NewScene();
        var data = SceneNodeTests.LoadModel("hierarchy.glb");
        var instance = scene.Root.Add("stage").Instance(data);
        var cloth = data.Materials.First(m => m.Slot == "cloth").BaseColor;
        var emitter = instance.NodeNamed("Tip")!.Attach(new SceneEmitter(Emitters.Still() with { StartColor = "cloth", EndColor = "ember" }) { Burst = 1 });
        Assert.Equal(cloth, emitter.Colours().Resolve(new ModelMaterial("cloth", Emitters.Grey, false)));
        Assert.Empty(emitter.Colours().Slots.Where(s => s.Key == "ember"));
        scene.Advance(0);
        Assert.Equal(Colors.ToLinear(Colors.ToSrgb(cloth)) with { W = 1 }, scene.View.Particles[0].Color);
        var tint = new Palette().Set("cloth", new Vector4(0.1f, 0.2f, 0.3f, 1)).Set("ember", new Vector4(0.9f, 0.4f, 0.1f, 1));
        instance.Palette = tint;
        Assert.Equal((new Vector4(0.1f, 0.2f, 0.3f, 1), new Vector4(0.9f, 0.4f, 0.1f, 1)),
            (emitter.Colours().Slots["cloth"].Lit, emitter.Colours().Slots["ember"].Lit));
        // Its own palette: what it has, grey for what it lacks (not the stage's).
        var own = new Palette().Set("ember", Vector4.One);
        emitter.Palette = own;
        Assert.Same(own, emitter.Colours());
        // The nearest only: a model instanced on the Tip itself colours it, not the stage above.
        emitter.Palette = null;
        var inner = instance.NodeNamed("Tip")!.Instance(data);
        inner.Palette = new Palette().Set("cloth", new Vector4(1, 0, 0, 1));
        Assert.Equal((new Vector4(1, 0, 0, 1), false), (emitter.Colours().Slots["cloth"].Lit, emitter.Colours().Slots.ContainsKey("ember")));
        // Nothing above it to colour it: grey.
        var bare = scene.Root.Add("bare").Attach(new SceneEmitter(Emitters.Still()) { Burst = 1 });
        Assert.Empty(bare.Colours().Slots);
        scene.Advance(0);
        Assert.Contains(scene.View.Particles, p => Vector4.Distance(p.Color, Colors.ToLinear(Colors.ToSrgb(Emitters.Grey)) with { W = 1 }) < 1e-6f);
    }

    /// <summary>Its effect keeps its rules and its numbers are 0 or more and finite, else it isn't made.</summary>
    [Fact]
    public void ABrokenEffectOrNumberIsRefused()
    {
        Assert.Equal("the effect isn't valid: its opacity (2) must be from 0 to 1 (Parameter 'effect')",
            Assert.Throws<ArgumentException>(() => new SceneEmitter(Emitters.Still() with { Opacity = 2 })).Message);
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneEmitter(Emitters.Still()) { Burst = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneEmitter(Emitters.Still()) { Rate = float.NaN });
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneEmitter(Emitters.Still()) { Rate = -1 });
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneEmitter(Emitters.Still()) { Duration = float.PositiveInfinity });
        Assert.Throws<ArgumentOutOfRangeException>(() => new SceneEmitter(Emitters.Still()) { Duration = -0.5f });
        var edges = new SceneEmitter(Emitters.Still()) { Burst = 0, Rate = 0, Duration = 0 };
        Assert.True(edges.Done);
    }
}

/// <summary>Floats equal within a tolerance.</summary>
sealed class FloatComparer(float tolerance) : IEqualityComparer<float>
{
    public bool Equals(float a, float b) => Math.Abs(a - b) <= tolerance;

    public int GetHashCode(float value) => 0;
}

/// <summary>
/// The particle track (Docs/Scene.md, "How it works", step 5; Q25, Q26, S5-2, S5-3): a state, each
/// cue's emitter on its node from the start at τ less the cue's time; seeks, skips and frames across
/// a cut of the reduced variant are jumps; at a skip the particles alive at the end time; after the end
/// the emitters on the scene's clock until their last particle dies; the effects bound and checked at
/// play; the test sequence's particles.
/// </summary>
public class SequenceParticleTests
{
    const string Path = "models/effects/still";

    /// <summary>A sequence on the dolly stage moving the node role "lens" along X (x = 4τ over 2 s) with a still stream of 10 a second from it from 0 for 2 s.</summary>
    static Sequence Runner(Func<SequenceBuilder, SequenceBuilder>? more = null) => StageBench.Build("runner", b =>
    {
        b = b.Transform("lens", t => t.Location(x: new FCurve([FCurveKey.Linear(0, 0), FCurveKey.Linear(2, 8)])))
            .Particles(p => p.Emit(0, Path, "lens", rate: 10, duration: 2));
        return more?.Invoke(b) ?? b;
    }, duration: 2);

    static SequenceBindings Bindings(StageBench bench, float life = 10) => bench.Bindings().Effect(Path, Emitters.Still(life));

    static SceneEmitter EmitterOn(SceneNode node) => node.Attachments.OfType<SceneEmitter>().Single();

    static float[] Xs(StageBench bench) => [.. bench.View.Particles.Select(p => p.Position.X)];

    /// <summary>The track is a state: each emitter hangs on its node from the start, held, its clock τ less its cue's time; seeking back before a cue takes its particles away.</summary>
    [Fact]
    public void TheTrackIsAStateItsEmittersAtTauLessTheirCue()
    {
        var bench = new StageBench();
        var sequence = Runner(b => b.Particles(p => p.Emit(1, Path, "dolly/Rock", burst: 3)));
        var player = bench.Play(sequence, Bindings(bench));
        var runner = EmitterOn(bench.Lens);
        var rock = EmitterOn(StageBench.Node(player, "Rock"));
        Assert.True(runner.Held && rock.Held);
        // From the start, before its first frame.
        Assert.Equal((0.0, -1.0), (runner.Time, rock.Time));
        bench.Frame(0);
        bench.Frame(0.5);
        Assert.Equal((0.5, -0.5), (runner.Time, rock.Time));
        Assert.Equal(6, bench.View.Particles.Count);
        bench.Frame(0.5);
        Assert.Equal((1.0, 0.0), (runner.Time, rock.Time));
        Assert.Equal(11 + 3, bench.View.Particles.Count);
        bench.At(player, 0.25);
        Assert.Equal(3, bench.View.Particles.Count);
        Assert.Equal(-0.75, rock.Time, 1e-12);
    }

    /// <summary>
    /// Watched, each particle is born where the node was (x = 4·birth); a seek forward is a jump (those
    /// born in the stretch jumped over at the node's place after it), then on between frames as
    /// before; a seek back forgets what lay after and records it again as the clock passes it, a stretch
    /// that was jumped over then from the node's place after the seek back.
    /// </summary>
    [Fact]
    public void ASeekIsAJump()
    {
        var bench = new StageBench();
        var player = bench.Play(Runner(), Bindings(bench));
        bench.Frame(0);
        for (var k = 0; k < 5; k++)
            bench.Frame(0.1);
        Assert.Equal([0, 0.4f, 0.8f, 1.2f, 1.6f, 2f], Xs(bench), new FloatComparer(1e-4f));
        bench.At(player, 1.5);
        var jumped = Xs(bench);
        Assert.Equal(16, jumped.Length);
        Assert.Equal(Enumerable.Repeat(6f, 10), jumped[6..], new FloatComparer(1e-4f));
        bench.Frame(0.1);
        Assert.Equal(6.4f, Xs(bench)[^1], 1e-4f);
        bench.At(player, 1.0);
        bench.Frame(0.1);
        bench.Frame(0.1);
        var again = Xs(bench);
        Assert.Equal(13, again.Length);
        Assert.Equal(new[] { 2f, 4f, 4f, 4f, 4f, 4f, 4.4f, 4.8f }, again[5..], new FloatComparer(1e-4f));
    }

    /// <summary>
    /// A skip (Q26): every emitter at the end time, those born in the stretch skipped where the node is
    /// at the end; a burst that died by then shows nothing; then the emitters play on, on the scene's
    /// clock at its rate, until their last particle has died, and are taken off.
    /// </summary>
    [Fact]
    public void ASkipShowsTheParticlesAliveAtTheEndAndTheyPlayOn()
    {
        var bench = new StageBench();
        bench.Scene.Rate = 0.5;
        var sequence = Runner(b => b.Particles(p => p.Emit(0.1, Path, "dolly/Rock", burst: 2)));
        var player = bench.Play(sequence, Bindings(bench, life: 1));
        var runner = EmitterOn(bench.Lens);
        var rock = EmitterOn(StageBench.Node(player, "Rock"));
        bench.Frame(0);
        for (var k = 0; k < 5; k++)
            bench.Frame(0.1);
        Assert.Equal(6 + 2, bench.View.Particles.Count);
        Assert.True(player.Skip());
        Assert.Equal((2.0, 1.9), (runner.Time, rock.Time));
        Assert.False(runner.Held || rock.Held);
        bench.Frame(0.1);
        // On the scene's clock: 2.05; alive: those born after 1.05 (1.1 … 1.9), all where the node is at the end.
        Assert.Equal(2.05, runner.Time, 1e-12);
        Assert.Equal(Enumerable.Repeat(8f, 9), Xs(bench), new FloatComparer(1e-4f));
        Assert.Null(rock.Node);
        for (var frames = 0; runner.Node != null; frames++)
        {
            Assert.True(frames < 100);
            bench.Frame(0.1);
        }
        Assert.Equal(2.9, runner.Time, 0.05 + 1e-9);
        Assert.Empty(bench.View.Particles);
    }

    /// <summary>Played to its end, the emitters go on from the end on the scene's clock at its rate (Q26); one whose particles died while the sequence played stays while it plays (a seek back shows them again) and is taken off after.</summary>
    [Fact]
    public void AfterTheEndTheEmittersPlayOnOnTheScenesClock()
    {
        var bench = new StageBench();
        bench.Scene.Rate = 2;
        var sequence = Runner(b => b.Particles(p => p.Emit(0, Path, "dolly/Rock", burst: 2).Emit(1.5, Path, "lens", rate: 10, duration: 1)));
        var player = bench.Play(sequence, Bindings(bench, life: 0.5f));
        var rock = EmitterOn(StageBench.Node(player, "Rock"));
        bench.Frame(0);
        bench.Frame(1);
        Assert.True(rock.Done);
        Assert.Same(StageBench.Node(player, "Rock"), rock.Node);
        bench.At(player, 0.25);
        Assert.Equal(2 + 3, bench.View.Particles.Count);
        bench.Frame(1);
        bench.Frame(0.75);
        Assert.True(player.Ended);
        var runner = bench.Lens.Attachments.OfType<SceneEmitter>().First();
        var late = bench.Lens.Attachments.OfType<SceneEmitter>().Last();
        Assert.Equal(2.0, runner.Time, 1e-9);
        Assert.Equal(0.5, late.Time, 1e-9);
        // The end isn't a jump: the late stream's births after it come between the worlds of the frames around them.
        bench.Lens.Translation = new(10, 2, 8);
        bench.Frame(0.1);
        Assert.Equal(2.2, runner.Time, 1e-9);
        Assert.Equal(0.7, late.Time, 1e-9);
        Assert.Null(rock.Node);
        Assert.False(runner.Done);
        // Alive (a life of 0.5 s): those born at 0.3 … 0.7 s; the one born at 0.6 s halfway between the end's world (x = 8) and this frame's (10).
        var lateParticles = late.Particles().ToArray();
        Assert.Equal(5, lateParticles.Length);
        Assert.Equal(9f, lateParticles[^2].Position.X, 1e-4f);
    }

    /// <summary>A frame across a cut of the reduced variant is a jump: the particles born in the part cut out start where the node is after it.</summary>
    [Fact]
    public void AFrameAcrossACutIsAJump()
    {
        var bench = new StageBench(reducedMotion: true);
        var sequence = Runner(b => b.Reduced(r => r.Span(0, 0.5).Span(1.5, 2)));
        var player = bench.Play(sequence, Bindings(bench));
        Assert.True(player.Reduced);
        bench.Frame(0);
        for (var k = 0; k < 5; k++)
            bench.Frame(0.1);
        Assert.Equal(0.5, player.TimelineTime, 1e-9);
        bench.Frame(0.1);
        Assert.Equal(1.6, player.TimelineTime, 1e-9);
        var xs = Xs(bench);
        Assert.Equal(17, xs.Length);
        Assert.Equal(new[] { 0f, 0.4f, 0.8f, 1.2f, 1.6f, 2f }, xs[..6], new FloatComparer(1e-4f));
        Assert.Equal(Enumerable.Repeat(6.4f, 11), xs[6..], new FloatComparer(1e-4f));
        // After the cut, between frames as before (the birth at 1.7 s between the frames at 1.6 and 1.75).
        bench.Frame(0.15);
        Assert.Equal(18, Xs(bench).Length);
        Assert.Equal(6.8f, Xs(bench)[17], 1e-4f);
        // Played in full, the cut's place is no jump.
        var full = new StageBench();
        var played = full.Play(sequence, Bindings(full));
        Assert.False(played.Reduced);
        full.Frame(0);
        for (var k = 0; k < 4; k++)
            full.Frame(0.1);
        full.Frame(0.15);
        Assert.Equal(new[] { 0f, 0.4f, 0.8f, 1.2f, 1.6f, 2f }, Xs(full), new FloatComparer(1e-4f));
    }

    public static TheoryData<string> BadEffects => [.. Bad.Keys];

    static readonly Dictionary<string, (Func<StageBench, SequenceBindings> Bindings, string Problem)> Bad = new()
    {
        ["missing"] = (b => b.Bindings(), "the sequence runner: its effect models/effects/still isn't given"),
        ["unused"] = (b => Bindings(b).Effect("models/effects/other", Emitters.Still()), "the sequence runner: an effect models/effects/other is given, which no particle cue names"),
        ["twice"] = (b => Bindings(b).Effect(Path, Emitters.Still()), "the sequence runner: its effect models/effects/still is given twice"),
        ["broken"] = (b => b.Bindings().Effect(Path, Emitters.Still() with { Life = new(1, 0) }),
            "the sequence runner: its effect models/effects/still isn't valid: its life's min (1) is above its max (0)"),
    };

    /// <summary>The effects the cues name are bound as stages are (S5-2): one missing, given but unused, given twice or breaking its rules is refused at play, changing nothing.</summary>
    [Theory]
    [MemberData(nameof(BadEffects))]
    public void TheEffectsAreBoundAndCheckedAtPlay(string name)
    {
        var bench = new StageBench();
        var (bindings, problem) = Bad[name];
        var children = bench.Scene.Root.Children.ToArray();
        Assert.Equal(problem, Assert.Throws<ArgumentException>(() => bench.Play(Runner(), bindings(bench))).Message);
        Assert.Equal(children, bench.Scene.Root.Children);
        Assert.Empty(bench.Scene.Root.Descendants().SelectMany(n => n.Attachments).OfType<SceneEmitter>());
        Assert.Empty(bench.Scene.Playing);
    }

    /// <summary>A cue whose numbers an emitter can't take (a rate beyond float's range) is refused at play naming the track and cue, changing nothing.</summary>
    [Fact]
    public void ACueAnEmitterCantTakeIsRefused()
    {
        var bench = new StageBench();
        var sequence = StageBench.Build("rates", b => b.Particles(p => p.Emit(0, Path, "lens", rate: 1e300)));
        var problem = Assert.Throws<ArgumentException>(() => bench.Play(sequence, Bindings(bench))).Message;
        Assert.StartsWith("the sequence rates, track 1 (particles): cue 0: an emitter's rate is a finite number, 0 or more", problem);
        Assert.Empty(bench.Lens.Attachments.OfType<SceneEmitter>());
        Assert.Empty(bench.Scene.Playing);
    }
}

/// <summary>
/// Particles through a sequence on the GPU bench and the renderer (Docs/Scene.md, step 5): the test
/// sequence's emitters (the stage's moving Tip, the pivot; the palette role "tint"), the figure's
/// colours for an emitter on it (S5-4), and the particle reference scene replayed through particle
/// cues on nodes (the trail's moved along its path by the game) giving the scene's particles and
/// Blender's render of them (Golden/r3d-particles).
/// </summary>
[Collection("GPU")]
public class SceneParticleRenderTests(GpuFixture gpu)
{
    /// <summary>
    /// The test sequence's camera, light and particle tracks (TestSequence.Camera.cs): at 0.75 s the
    /// pivot's camera and sun the scene's at the curves' values; the Tip's emitter (the marker "open",
    /// 0.4 s; the bound palette "tint") 0.35 s in, recording the moving Tip; the pivot's (1 s, 1.5 s) not
    /// yet started; at a skip the end state: the curves' end values, every emitter at the end time; the
    /// reduced variant's cut (0.5–1.5 s) a jump for the one at 1 s, so its burst starts where the pivot is
    /// at 1.5 s.
    /// </summary>
    [Fact]
    public void TheTestSequencesCameraLightAndParticleTracks()
    {
        using var bench = new SequenceBench(gpu);
        var player = bench.Play();
        var tip = SequenceBench.StageNode(player, "Tip");
        var emitters = new[] { EmitterOn(tip), bench.Pivot.Attachments.OfType<SceneEmitter>().First(), bench.Pivot.Attachments.OfType<SceneEmitter>().Last() };
        Assert.Same(bench.Tint, emitters[0].Colours());
        bench.Frame(0.125);
        bench.FrameUntil(player, 0.75 - 1e-9);
        Assert.Equal((0.35, -0.25, -0.75), (emitters[0].Time, emitters[1].Time, emitters[2].Time), new TupleComparer(1e-9));
        Assert.Equal(5 + 4, bench.View.Particles.Count);
        Assert.Equal((bench.PivotCamera, (float)TestSequence.FieldOfView.Evaluate(0.75), bench.PivotSun, (float)TestSequence.Strength.Evaluate(0.75), (float)TestSequence.Angle.Evaluate(0.75)),
            (bench.Scene.Camera, bench.PivotCamera.VerticalFieldOfView, bench.Scene.Sun, bench.PivotSun.Strength, bench.PivotSun.Angle));
        // The Tip's world recorded as it moves, the last where it is now.
        Assert.Equal(tip.World, emitters[0].Samples[^1].World);
        Assert.True(emitters[0].Samples.Count >= 3);
        Assert.True(player.Skip());
        Assert.Equal((1.6, 1.0, 0.5), (emitters[0].Time, emitters[1].Time, emitters[2].Time), new TupleComparer(1e-9));
        Assert.Equal((bench.PivotCamera, 0.9f, bench.PivotSun, 4f, 5f), (bench.Scene.Camera, bench.PivotCamera.VerticalFieldOfView, bench.Scene.Sun, bench.PivotSun.Strength, bench.PivotSun.Angle));

        using var reduced = new SequenceBench(gpu, reducedMotion: true);
        var short_ = reduced.Play();
        reduced.Frame(0.125);
        reduced.FrameUntil(short_, 1.5 - 1e-9);
        reduced.Frame(0.125);
        var cut = reduced.Pivot.Attachments.OfType<SceneEmitter>().First();
        Assert.Equal(0.5, cut.Time, 1e-9);
        var born = cut.Emitter.At(0, new Palette()).ToArray();
        Assert.Equal(4, born.Length);
        Assert.All(born, p => Emitters.Near(reduced.Pivot.World.Translation, p.Position, 1e-4f));
    }

    static SceneEmitter EmitterOn(SceneNode node) => node.Attachments.OfType<SceneEmitter>().Single();

    /// <summary>An emitter on a figure's node without a palette takes the figure's (S5-4): its palette's slots, then its model's materials' colours.</summary>
    [Fact]
    public void AnEmitterOnAFigureTakesTheFiguresColours()
    {
        using var bench = new SequenceBench(gpu);
        var emitter = bench.HeroNode.Add("hand").Attach(new SceneEmitter(TestSequence.Effect) { Burst = 1 });
        var cloth = bench.Figure.Data.Materials.First(m => m.Slot == "cloth").BaseColor;
        Assert.Equal(cloth, emitter.Colours().Slots["cloth"].Lit);
        Assert.False(emitter.Colours().Slots.ContainsKey("ember"));
        bench.Hero.Palette = bench.Tint;
        Assert.Equal(bench.Tint.Slots["cloth"].Lit, emitter.Colours().Slots["cloth"].Lit);
    }

    /// <summary>
    /// The particle reference scene (Scenes3D's <c>particles</c>, a second into its effects) replayed
    /// through a sequence: each emitter a particle cue at its start time on a node at its place, the
    /// trail's node moved by the game along the trail's path every frame (frames of 1/90 s, on its
    /// births), the palette bound as a role: at the end the view's particles are the scene's within float
    /// rounding, and drawn with the scene they match Blender's render as Render3DReferenceTests compares it.
    /// </summary>
    [Fact]
    public void TheParticleSceneReplayedThroughASequenceIsDrawnAsBlenderDrewIt()
    {
        var scene = Scenes3D.All.Single(s => s.Name == "particles");
        using var view = new View3D(gpu.Device);
        var world = new SceneView(view);
        var palette = new Palette(scene.Palette, scene.Shadows);
        var builder = new SequenceBuilder("particles", scene.Time).Role("fx", RoleKind.Palette);
        var bindings = new SequenceBindings().Palette("fx", palette);
        var nodes = new SceneNode[scene.Particles.Length];
        for (var i = 0; i < scene.Particles.Length; i++)
        {
            var emitter = scene.Particles[i];
            var (name, path) = ($"n{i}", $"models/effects/e{i}");
            nodes[i] = world.Root.Add(name);
            nodes[i].Translation = emitter.Path!(0);
            builder.Role(name, RoleKind.Node).Particles(p => p.Emit(emitter.Start, path, name, emitter.Seed, emitter.Burst, emitter.Rate, emitter.Duration, "fx"));
            bindings.Node(name, nodes[i]).Effect(path, emitter.Effect);
        }
        var player = world.Play(builder.Build(), bindings);
        var trail = scene.Particles[^1].Path!;
        Assert.NotEqual(trail(0), trail(0.5f));
        for (var k = 0; k <= 90; k++)
        {
            nodes[^1].Translation = trail(k / 90f);
            world.Advance(k == 0 ? 0 : 1 / 90.0);
        }
        Assert.Equal(1, player.TimelineTime, 1e-9);
        var expected = scene.ParticlesNow();
        var actual = view.Particles.ToArray();
        Assert.Equal(expected.Length, actual.Length);
        Assert.True(expected.Length > 30);
        for (var i = 0; i < expected.Length; i++)
        {
            Assert.Same(expected[i].Effect, actual[i].Effect);
            Emitters.Near(expected[i].Position, actual[i].Position, 1e-4f);
            Emitters.Near(expected[i].Velocity, actual[i].Velocity, 1e-3f);
            Assert.Equal(expected[i].Size, actual[i].Size, 1e-5f);
            Assert.Equal(expected[i].Angle, actual[i].Angle, 1e-3f);
            Assert.Equal(expected[i].Opacity, actual[i].Opacity, 1e-5f);
            Assert.True(Vector4.Distance(expected[i].Color, actual[i].Color) < 1e-5f);
        }

        var device = gpu.Device;
        var meshes = new Dictionary<Shape3D, Mesh>();
        try
        {
            var items = scene.Objects.Select(o =>
            {
                if (!meshes.TryGetValue(o.Shape, out var mesh))
                    meshes[o.Shape] = mesh = Mesh.FromTriangles(device, o.Shape.Positions, o.Shape.Triangles);
                return new DrawItem(mesh, o.Model, Colors.LinearFromHex(o.Color));
            }).ToList();
            using var target = new RenderTarget3D(device, scene.Width, scene.Height);
            var renderer = new Renderer3D(device);
            var frame = device.BeginFrame();
            renderer.Render(frame, target, scene.Camera.ToCamera(), items, Colors.LinearFromHex(scene.Background), scene.Sun ?? default,
                scene.Style with { OutlineWidth = 0, InnerWidth = 0 }, scene.Scale, scene.Time, view.Particles);
            device.EndFrame(frame);
            device.WaitIdle();
            var rendered = new RgbaImage(scene.Width, scene.Height, target.Resolved.ReadPixels());
            var problems = Render3DReferenceTests.Compare(ImageFile.Load(Golden.GoldenPath("r3d-" + scene.Name)), rendered);
            if (problems.Length > 0)
            {
                ImageFile.SavePng(Golden.ActualPath("r3d-particles-sequence"), rendered);
                Assert.Fail($"particles through a sequence: {problems}; render in {Golden.ActualPath("r3d-particles-sequence")}");
            }
        }
        finally
        {
            foreach (var mesh in meshes.Values)
                mesh.Dispose();
        }
    }

    /// <summary>
    /// The water's clock (S5-Q1): a scene holding terrain with water advances the view's time by the
    /// frame's time × its rate, from the time the game gave it; one without water leaves it alone (so a
    /// still scene isn't drawn again every frame).
    /// </summary>
    [Fact]
    public void TheWaterRunsAtTheScenesRate()
    {
        using var terrain = Terrain.Load(gpu.Device, System.IO.Path.Combine(Render3DReferenceTests.TerrainDirectory, "test.json"));
        Assert.NotNull(terrain.Data.WaterLevel);
        var dry = new SceneView(new View3D(null!));
        dry.View.Time = 3;
        dry.Advance(0.5);
        Assert.Equal(3, dry.View.Time);
        var scene = new SceneView(new View3D(null!));
        scene.View.Time = 1.5f;
        scene.Root.Add("map").Attach(new PlacedTerrain(terrain));
        scene.Rate = 0.5;
        scene.Advance(0.5);
        Assert.Equal(1.75f, scene.View.Time);
        scene.Rate = 1;
        scene.Advance(0.25);
        Assert.Equal(2f, scene.View.Time);
        scene.View.Time = 10;
        scene.Advance(0.5);
        Assert.Equal(10.5f, scene.View.Time);
        scene.Rate = 0;
        scene.Advance(0.5);
        Assert.Equal(10.5f, scene.View.Time);
        // Over a long while, kept in double: 60 fps for an hour lands on the hour.
        scene.Rate = 1;
        scene.View.Time = 0;
        for (var i = 0; i < 60 * 3600; i++)
            scene.Advance(1 / 60.0);
        Assert.Equal(3600f, scene.View.Time, 1e-3f);
    }
}

/// <summary>Tuples of doubles equal within a tolerance.</summary>
sealed class TupleComparer(double tolerance) : IEqualityComparer<(double, double, double)>
{
    public bool Equals((double, double, double) a, (double, double, double) b) =>
        Math.Abs(a.Item1 - b.Item1) <= tolerance && Math.Abs(a.Item2 - b.Item2) <= tolerance && Math.Abs(a.Item3 - b.Item3) <= tolerance;

    public int GetHashCode((double, double, double) value) => 0;
}
