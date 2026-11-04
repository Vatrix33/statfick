using Statventure.Render3D;
using Statventure.Scene;

namespace Statventure.Tests.Scene;

/// <summary>
/// Step 5's part of the test sequence (Docs/Scene.md, "How it works", step 5): the camera, light and
/// particle tracks. The bench's node role "pivot" (moved and turned by the transform track) carries a
/// camera and a sun (SequenceBench), so the cut and the sun cue name a moving node; the particle cues
/// start the test effect on the stage's Tip (which the clip "base" moves) at the marker "open" and on
/// the pivot at 1 s (cut out of the reduced variant) and 1.5 s (the moment it holds).
/// </summary>
internal static partial class TestSequence
{
    public const string EffectPath = "models/effects/sparks";

    /// <summary>The test effect: sparks in the palette slots "cloth" (the bound palette "tint" has it) and "ember" (it doesn't), thrown up in a cone, falling and fading.</summary>
    public static ParticleEffect Effect { get; } = new()
    {
        Shape = new ParticleShape.Spark(0.3f, 0.05f), StartColor = "cloth", EndColor = "ember", Glow = true,
        Life = new(0.6f, 0.9f), Size = new(0.05f, 0.08f), Speed = new(1, 2), Spawn = new ParticleSpawn.Cone(new(0, 1, 0), 25),
        Gravity = new(0, -3, 0), FadeOut = 0.4f,
    };

    /// <summary>The field of view after the cut: 0.6 rad at 0.25 s widening linearly to 0.9 rad at the end.</summary>
    public static FCurve FieldOfView { get; } = new([FCurveKey.Linear(0.25, 0.6), FCurveKey.Linear(Duration, 0.9)]);
    /// <summary>The sun's strength after its cue: 2 at 0.25 s to 4 at the end.</summary>
    public static FCurve Strength { get; } = new([FCurveKey.Linear(0.25, 2), FCurveKey.Linear(Duration, 4)]);
    /// <summary>The sun's angle after its cue: 3° at 0.25 s to 5° at the end.</summary>
    public static FCurve Angle { get; } = new([FCurveKey.Linear(0.25, 3), FCurveKey.Linear(Duration, 5)]);

    static partial void Camera(SequenceBuilder builder) => builder
        .Camera(c => c.Cut(0.25, "pivot").FieldOfView(FieldOfView))
        .Light(l => l.Sun(0.25, "pivot").Strength(Strength).Angle(Angle))
        .Particles(p => p
            .Emit(CueTime.At("hierarchy/open"), EffectPath, "hierarchy/Tip", seed: 7, burst: 5, rate: 10, duration: 0.5, palette: "tint")
            .Emit(1, EffectPath, "pivot", seed: 8, burst: 4)
            .Emit(1.5, EffectPath, "pivot", seed: 9, burst: 3));
}
