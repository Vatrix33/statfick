using Statventure.Animation;
using Statventure.Audio;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;

namespace Statventure.Tests.Audio;

/// <summary>What the effect fixtures' renders can't show (Docs/Audio.md, step 8): errors, voices released, repeatable draws, an actor's table.</summary>
[Collection("GPU")]
public class EffectTests(GpuFixture gpu)
{
    static readonly EffectPatch Beep = new() { Layers = [new EffectLayer { Source = EffectSource.Sine, Length = 0.05 }] };

    static AudioSystem System() => new(new AudioGraph(22050, 2));

    static float[] Render(AudioSystem system, int quanta)
    {
        var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
        var result = new List<float>();
        for (var q = 0; q < quanta; q++)
        {
            system.BeforeQuantum();
            system.Graph.RenderQuantum(bus);
            result.AddRange(bus.Channel(0).Data);
            result.AddRange(bus.Channel(1).Data);
        }
        return [.. result];
    }

    [Fact]
    public void BadCallsAreRefused()
    {
        var effects = System().Effects;
        Assert.Throws<ArgumentException>(() => effects.Play("nothing"));
        Assert.Throws<ArgumentException>(() => effects.Register("a", new EffectPatch { Layers = [] }));
        Assert.Throws<ArgumentException>(() => effects.Register("a", Beep with { MaxVoices = 0 }));
        Assert.Throws<ArgumentException>(() => effects.Register("a", new EffectPatch { Layers = [new EffectLayer { Source = EffectSource.Sine, Length = 0 }] }));
        Assert.Throws<ArgumentException>(() => effects.Register("a", new EffectPatch { Layers = [new EffectLayer { Source = EffectSource.Harmonics, Length = 1 }] }));
        effects.Register("a", Beep);
        Assert.Throws<ArgumentOutOfRangeException>(() => effects.Play("a", pitch: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => effects.Play("a", volume: double.NaN));
    }

    [Fact]
    public void AFinishedVoiceIsReleased()
    {
        var system = System();
        system.Effects.Register("a", Beep);
        system.Effects.Play("a");
        system.Effects.Play("a");
        Render(system, 2);
        Assert.Equal(2, system.Effects.ActiveVoices);
        Render(system, 40);
        Assert.Equal(0, system.Effects.ActiveVoices);
    }

    [Fact]
    public void VoicesStayWithinTheLimits()
    {
        var system = System();
        var long_ = new EffectPatch { Layers = [new EffectLayer { Source = EffectSource.Sine, Length = 5 }], MaxVoices = 3 };
        system.Effects.Register("a", long_);
        for (var i = 0; i < 10; i++)
            system.Effects.Play("a");
        Render(system, 1);
        Assert.Equal(3, system.Effects.ActiveVoices);
        for (var i = 0; i < 40; i++)
        {
            system.Effects.Register($"p{i}", long_);
            system.Effects.Play($"p{i}");
        }
        Render(system, 1);
        Assert.Equal(SoundEffects.MaxVoicesOverall, system.Effects.ActiveVoices);
    }

    [Fact]
    public void APatchAllowsFourVoicesByDefault()
    {
        var system = System();
        system.Effects.Register("a", new EffectPatch { Layers = [new EffectLayer { Source = EffectSource.Sine, Length = 5 }] });
        for (var i = 0; i < 10; i++)
            system.Effects.Play("a");
        Render(system, 1);
        Assert.Equal(4, system.Effects.ActiveVoices);
    }

    /// <summary>A stolen voice's sources stop with its fade, so it costs nothing more (its gain alone would silence it).</summary>
    [Fact]
    public void AStolenVoiceStopsItsSources()
    {
        var system = System();
        system.Effects.Register("a", new EffectPatch { Layers = [new EffectLayer { Source = EffectSource.Sine, Length = 5 }], MaxVoices = 1 });
        system.Effects.Play("a");
        Render(system, 1);
        Assert.Equal(1, system.Graph.ActiveSources);
        system.Effects.Play("a");
        Render(system, 5);
        Assert.Equal(1, system.Graph.ActiveSources);
    }

    [Fact]
    public void TheSameSeedDrawsTheSame()
    {
        float[] Run(uint seed)
        {
            var system = System();
            system.Effects.Register("a", new EffectPatch
            {
                Layers = [new EffectLayer { Source = EffectSource.Noise, Length = 0.05 }],
                Pitch = new EffectRange(0.5, 2), Volume = new EffectRange(0.2, 1),
            });
            system.Effects.Seed(seed);
            system.Effects.Play("a");
            return Render(system, 20);
        }
        Assert.Equal(Run(5), Run(5));
        Assert.NotEqual(Run(5), Run(6));
    }

    /// <summary>A range-less patch plays alike each time: the draws are only for what asks for them.</summary>
    [Fact]
    public void NoRangeMeansNoVariation()
    {
        var system = System();
        system.Effects.Register("a", Beep);
        system.Effects.Play("a");
        var first = Render(system, 40);
        system.Effects.Play("a");
        var second = Render(system, 40);
        Assert.Equal(first.Max(), second.Max(), 3);
        Assert.True(first.Max() > 0);
    }

    /// <summary>The figure's swing has "whoosh" and "hit" markers: the actor's table plays what it names, at its pan and volume.</summary>
    [Fact]
    public void AnActorsClipEventsPlayItsTable()
    {
        using var model = Model.Load(gpu.Device, Path.Combine(Statventure.Tests.Render3D.Render3DReferenceTests.ModelDirectory, "figure.glb"));
        var skeleton = new Skeleton(model.Data);
        var stack = new AnimationStack(skeleton);
        stack.Add(new ClipBinding(AnimationClip.AllOf(model.Data).Single(c => c.Name == "swing"), skeleton), 0);
        var animator = new Animator(stack);
        var system = System();
        system.Effects.Register("swish", Beep with { Layers = [new EffectLayer { Source = EffectSource.Sine, Length = 5 }] });
        var actor = new Actor(model, animator) { Sounds = new ActorSounds(system.Effects) { Pan = -1, Volume = 0.5 } };
        actor.Sounds.Table["whoosh"] = "swish";
        // Before the marker nothing plays; "hit" isn't in the table, so one voice in all.
        animator.Advance(4 / 60.0);
        animator.DispatchEvents();
        system.BeforeQuantum();
        Assert.Equal(0, system.Effects.ActiveVoices);
        animator.Advance(0.5);
        animator.DispatchEvents();
        var rendered = Render(system, 8);
        Assert.Equal(1, system.Effects.ActiveVoices);
        // The same as playing it directly at the actor's pan and volume, on the same quantum.
        var direct = System();
        direct.Effects.Register("swish", Beep with { Layers = [new EffectLayer { Source = EffectSource.Sine, Length = 5 }] });
        direct.Effects.Play("swish", 0.5, -1);
        Assert.Equal(Render(direct, 8), rendered);
        Assert.Contains(rendered, v => v != 0);
    }

    /// <summary>Effects sound through their bus: its volume and the master's scale them, the music's doesn't.</summary>
    [Theory]
    [InlineData(0.5, 1, 1, 0.5)]
    [InlineData(1, 0.25, 1, 0.25)]
    [InlineData(1, 1, 0.1, 1)]
    public void EffectsGoThroughTheirBus(double effects, double master, double music, double expected)
    {
        var plain = System();
        var scaled = System();
        foreach (var system in new[] { plain, scaled })
            system.Effects.Register("a", new EffectPatch { Layers = [new EffectLayer { Source = EffectSource.Sine, Length = 5 }] });
        scaled.EffectsVolume = effects;
        scaled.MasterVolume = master;
        scaled.Music.Volume = music;
        plain.Effects.Play("a");
        scaled.Effects.Play("a");
        const int Q = AudioGraph.RenderQuantumFrames;
        var settled = (int)(0.05 * 22050 / Q) + 1;
        var a = Render(plain, settled + 8).Skip(settled * 2 * Q).ToArray();
        var b = Render(scaled, settled + 8).Skip(settled * 2 * Q).ToArray();
        for (var i = 0; i < a.Length; i++)
            Assert.Equal(a[i] * expected, b[i], 1e-5);
        Assert.Contains(a, v => Math.Abs(v) > 0.01);
    }
}
