using Statventure.Animation;

namespace Statventure.Tests.Animation;

/// <summary>
/// A strip of a layered scene, as an NLA strip in Blender (Docs/Animation.md): times in seconds on
/// the scene's clock, the clip's part in seconds of the clip.
/// </summary>
public sealed record StripScene(string Clip, double Start, double ClipStart = 0, double? ClipEnd = null, double Scale = 1, double Repeat = 1,
    double BlendIn = 0, double BlendOut = 0, double? Influence = null, BlendMode Mode = BlendMode.Replace,
    Extrapolation Extrapolation = Extrapolation.HoldForward, bool Reversed = false);

/// <summary>
/// An animation reference scene (Docs/Animation.md, "Method"): a test model (Data/Models, built by
/// Tools/BlenderReference/make_models.py), a clip of it, and the times (seconds from the clip's
/// start) at which Blender evaluates its action (Tools/BlenderReference/evaluate_animations.py,
/// Golden/anim-&lt;name&gt;.json) and the engine samples the exported clip. With a
/// <see cref="Target"/> the clip plays on that model (found by bone names, on its own rest pose), as
/// Blender plays the action on the target's armature. With <see cref="Layers"/> the scene is an NLA
/// stack instead (layers bottom first, each a list of strips), and <see cref="Clip"/> is unused; the
/// layers <see cref="Transitions"/> lists have a transition strip in each gap between their strips
/// in Blender, a fade layer in the engine (each strip fading in over the gap before it, strips held
/// outside their ranges). <see cref="Items"/> are item models on socket bones of the wearer (the
/// target, else the model) and <see cref="Clothing"/> garments made on the figure, skinned to the
/// wearer by bone names (Docs/Animation.md, "Gear in detail").
/// </summary>
public sealed record AnimationScene(string Name, string Model, string Clip, double[] Times, string? Target = null, StripScene[][]? Layers = null,
    int[]? Transitions = null, (string Model, string Socket)[]? Items = null, string[]? Clothing = null)
{
    /// <summary>
    /// A time that falls on one of the clip's 60 fps frames, where Blender's evaluation and the
    /// exported samples agree exactly (within 10⁻⁴ of a frame: clip lengths come as float32 seconds,
    /// 0.6 s being 36.0000014 frames).
    /// </summary>
    public static bool OnFrame(double time) => Math.Abs(time * AnimationScenes.Fps - Math.Round(time * AnimationScenes.Fps)) < 1e-4;
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

    /// <summary>Every other frame of a clip of <paramref name="frames"/> frames.</summary>
    static double[] EveryOtherFrame(int frames) => Enumerable.Range(0, frames / 2 + 1).Select(k => 2.0 * k / Fps).ToArray();

    /// <summary>Every frame from 0 to <paramref name="seconds"/>.</summary>
    static double[] Frames(double seconds) => Enumerable.Range(0, (int)Math.Round(seconds * Fps) + 1).Select(k => (double)k / Fps).ToArray();

    /// <summary>
    /// An idle loop under a swing that blends in and out over it (Replace): the swing replaces only
    /// the channels it keys (the idle's hips location, chest scale and legs go on), its influence
    /// ramps linearly, and after its end with a blend-out and no extrapolation it's gone.
    /// </summary>
    static readonly StripScene[][] ReplaceLayers =
    [
        [new("idle", 0, Repeat: 2, Extrapolation: Extrapolation.Hold)],
        [new("swing", 0.5, BlendIn: 0.15, BlendOut: 0.1, Extrapolation: Extrapolation.Nothing)],
    ];

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
        // The figure's clips on the stocky figure: other proportions, rolled upper arms, no neck (the
        // idle's neck channel is left out), a beard bone no clip moves.
        new("stocky-idle", "figure", "idle", TimesFor(120), Target: "stocky"),
        new("stocky-swing", "figure", "swing", TimesFor(36), Target: "stocky"),
        new("nla-replace", "figure", "", Frames(2), Layers: ReplaceLayers),
        // An idle added over a held swing (Combine) at a fixed influence of 0.6, and a pulse added over
        // both at 0.5: locations added, scales multiplied by their powers, rotations multiplied (in
        // order) by the added ones raised to the influence.
        new("nla-combine", "figure", "", Frames(2), Layers:
        [
            [new("swing", 0.2, Extrapolation: Extrapolation.Hold)],
            [new("idle", 0, Influence: 0.6, Mode: BlendMode.Combine, Extrapolation: Extrapolation.Hold)],
            [new("pulse", 0.3, Influence: 0.5, Mode: BlendMode.Combine, Extrapolation: Extrapolation.Nothing)],
        ]),
        // Strip time: a part of the idle (0.5 s to 1.5 s) at half speed, 1.5 times, blending in, held
        // before it (its layer's first strip; held at its start, where its influence is 0); above, a
        // swing at double speed, twice and backwards, blending out, held forward over the gap to the
        // next strip (held at its end, where its influence is 0 again), then a part of a swing (0.1 s
        // to 0.4 s, ends unlike each other: at a strip's end with whole repeats the part's end) that
        // extrapolates nothing.
        new("nla-timing", "figure", "", Frames(3.2), Layers:
        [
            [new("idle", 0.1, ClipStart: 0.5, ClipEnd: 1.5, Scale: 2, Repeat: 1.5, BlendIn: 0.2, Extrapolation: Extrapolation.Hold)],
            [new("swing", 0.25, Scale: 0.5, Repeat: 2, Reversed: true, BlendOut: 0.1),
             new("swing", 1.5, ClipStart: 0.1, ClipEnd: 0.4, Extrapolation: Extrapolation.Nothing)],
        ]),
        // Blender's transition rule (the state machines' cross-fades): over an idle, the first half of
        // a swing (held mid-swing, not at rest) then a pulse with a transition between them: both
        // sides over the idle (the swing alone keys the forearm, the pulse alone the head's scale and
        // the hips' location, both the chest's rotation), mixed over every channel by the
        // transition's progress.
        new("nla-transition", "figure", "", Frames(2), Layers:
        [
            [new("idle", 0, Repeat: 2, Extrapolation: Extrapolation.Hold)],
            [new("swing", 0.2, ClipEnd: 0.3, Extrapolation: Extrapolation.Hold), new("pulse", 0.8)],
        ], Transitions: [1]),
        // The same with Combine strips on both sides over a held swing.
        new("nla-transition-combine", "figure", "", Frames(2), Layers:
        [
            [new("swing", 0, Extrapolation: Extrapolation.Hold)],
            [new("idle", 0, ClipEnd: 0.8, Mode: BlendMode.Combine, Extrapolation: Extrapolation.Hold), new("pulse", 1.2, Mode: BlendMode.Combine)],
        ], Transitions: [1]),
        // Gear: a sword in the hand socket, a pennant (an item with bones) on the back socket and the
        // tabard, on the figure (the body the tabard is made on) and on the stocky figure (where
        // Blender's Armature modifier deforms it by the stocky bones relative to their rest).
        new("gear-figure", "figure", "swing", EveryOtherFrame(36), Items: [("sword", "socket.hand.R"), ("pennant", "socket.back")], Clothing: ["tabard"]),
        new("gear-stocky", "figure", "idle", EveryOtherFrame(120), Target: "stocky", Items: [("sword", "socket.hand.R")], Clothing: ["tabard"]),
        // The Replace stack on the stocky figure: clips found by bone names, then layered.
        new("stocky-nla", "figure", "", Frames(2), Target: "stocky", Layers: ReplaceLayers),
        // Ji'ruan's acting clips (Docs/Scene.md, step 7, phase 1b): a clip library (jiruan-acting.glb, the
        // armature alone) played on the test figure by bone names, as the sequence plays them.
        new("acting-rise", "jiruan-acting", "rise", TimesFor(84), Target: "figure"),
        new("acting-spread", "jiruan-acting", "spread", TimesFor(72), Target: "figure"),
        new("acting-settle", "jiruan-acting", "settle", TimesFor(72), Target: "figure"),
        new("acting-idle-king", "jiruan-acting", "idle-king", TimesFor(144), Target: "figure"),
        new("acting-fly-out", "jiruan-acting", "fly-out", TimesFor(102), Target: "figure"),
        new("acting-hover", "jiruan-acting", "hover", TimesFor(48), Target: "figure"),
        new("acting-idle-imp", "jiruan-acting", "idle-imp", TimesFor(96), Target: "figure"),
    ];
}
