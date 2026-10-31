namespace Statventure.Tests.Animation;

/// <summary>
/// An animation reference scene (Docs/Animation.md, "Method"): a test model (Data/Models, built by
/// Tools/BlenderReference/make_models.py), a clip of it, and the times (seconds from the clip's
/// start) at which Blender evaluates its action (Tools/BlenderReference/evaluate_animations.py,
/// Golden/anim-&lt;name&gt;.json) and the engine samples the exported clip.
/// </summary>
public sealed record AnimationScene(string Name, string Model, string Clip, double[] Times)
{
    /// <summary>A time that falls on one of the clip's 60 fps frames, where Blender's evaluation and the exported samples agree exactly.</summary>
    public static bool OnFrame(double time) => Math.Abs(time * AnimationScenes.Fps - Math.Round(time * AnimationScenes.Fps)) < 1e-6;
}

public static class AnimationScenes
{
    public const int Fps = 60;

    /// <summary>
    /// Times for a clip of <paramref name="frames"/> frames: every other frame (and the last), the
    /// middle of every frame and a quarter into every fifth (between the exported samples, where
    /// Blender evaluates its curves), and one time before the start and one after the end (clamped).
    /// </summary>
    static double[] TimesFor(int frames)
    {
        var times = new SortedSet<double> { -0.05, (frames + 3.0) / Fps, (double)frames / Fps };
        for (var k = 0; k <= frames; k += 2)
            times.Add((double)k / Fps);
        for (var k = 0; k < frames; k++)
            times.Add((k + 0.5) / Fps);
        for (var k = 0; k < frames; k += 5)
            times.Add((k + 0.25) / Fps);
        return [.. times];
    }

    public static readonly AnimationScene[] All =
    [
        // Translation, rotation and scale channels with Blender's eased keys, a two-second loop.
        new("figure-idle", "figure", "idle", TimesFor(120)),
        // Fast eased rotations (the arm turns 150° in 0.2 s). The exporter writes each quaternion with
        // w ≥ 0, so the upper arm's change sign where its local rotation passes 180°: only a slerp
        // that takes the short way follows them.
        new("figure-swing", "figure", "swing", TimesFor(36)),
        // A 270° turn about a bone's own axis (Blender interpolates the quaternion's components) and STEP keys.
        new("figure-twist", "figure", "twist", TimesFor(60)),
        // An IK constraint baked into the clip by the exporter's sampling.
        new("reach-reach", "reach", "reach", TimesFor(60)),
    ];
}
