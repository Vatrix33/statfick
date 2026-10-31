using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Tests.Render3D;

namespace Statventure.Tests.Animation;

/// <summary>
/// The layer stack's rules that the Blender scenes (AnimationReferenceTests, nla-*) don't reach:
/// Blender's limits on blend in and out, and a cross-fade made with <see cref="AnimationStack.Add"/>.
/// </summary>
public class AnimationStackTests
{
    static readonly ModelData Figure = ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, "figure.glb"));

    static ClipBinding Clip(string name, Skeleton skeleton) => new(AnimationClip.AllOf(Figure).Single(c => c.Name == name), skeleton);

    [Fact]
    public void BlendsAreLimitedAsBlendersSettersLimitThem()
    {
        var skeleton = new Skeleton(Figure);
        // The swing is 0.6 s long. A blend-in longer than the strip is the strip's length.
        var strip = new AnimationStrip(Clip("swing", skeleton)) { Start = 1, BlendIn = 2 };
        Assert.Equal(0.5, strip.InfluenceAt(1.3), 6);
        // A blend-out is limited to what the blend-in leaves: 0.6 − 0.4 = 0.2.
        strip.BlendIn = 0.4;
        strip.BlendOut = 0.5;
        Assert.Equal(1, strip.InfluenceAt(1.4), 6);
        Assert.Equal(0.5, strip.InfluenceAt(1.5), 5);
        // A fixed influence ignores both.
        strip.Influence = 0.25;
        Assert.Equal(0.25, strip.InfluenceAt(1.05));
    }

    [Fact]
    public void ACrossFadeIsAStripBlendingInOverTheOneBelow()
    {
        var skeleton = new Skeleton(Figure);
        var stack = new AnimationStack(skeleton);
        var idle = Clip("idle", skeleton);
        var swing = Clip("swing", skeleton);
        stack.Add(idle, 0, s => s.Repeat = 3);
        stack.Add(swing, 0.5, s => s.BlendIn = 0.2);
        // Half way through the fade: the swing's channels half way between the idle's and its own
        // (Replace, influence 0.5); channels only the idle keys are the idle's.
        var mixed = new PoseBuffer(skeleton);
        stack.Evaluate(0.6, mixed);
        PoseBuffer Alone(ClipBinding clip, double time)
        {
            var buffer = new PoseBuffer(skeleton);
            clip.Sample(time, buffer);
            return buffer;
        }
        var below = Alone(idle, 0.6);
        var above = Alone(swing, 0.1);
        var arm = skeleton.Find("upper_arm.R");
        for (var c = 0; c < 4; c++)
            Assert.Equal((below.Rotation[4 * arm + c] + above.Rotation[4 * arm + c]) / 2, mixed.Rotation[4 * arm + c], 12);
        var hips = skeleton.Find("hips");
        for (var c = 0; c < 3; c++)
            Assert.Equal(below.Translation[3 * hips + c], mixed.Translation[3 * hips + c], 12);
        // After the fade the swing alone sets its channels.
        stack.Evaluate(0.8, mixed);
        var after = Alone(swing, 0.3);
        for (var c = 0; c < 4; c++)
            Assert.Equal(after.Rotation[4 * arm + c], mixed.Rotation[4 * arm + c], 12);
        Assert.True(mixed.ToPose().TryGet(arm, out var local) && Math.Abs(local.Rotation.Length() - 1) < 1e-6);
    }
}
