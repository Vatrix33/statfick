using System.Globalization;
using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Render3D;

namespace Statventure.Tests.Scene;

/// <summary>
/// Ji'ruan's intro as render scenes at the proof's sampled times (Docs/Scene.md, step 7, phase 2; Q32), for
/// Blender's reference renders (Render3D/Scenes3D, r3d-scenes.json, Golden/r3d-jiruan-&lt;time&gt;.png): the
/// stage at that moment of its clip (each top-level object a model of its own, scenery), seen through its
/// camera and lit by its sun as the import gives them, and each stand-in on its mark in its clip's pose at
/// that time by the approved schedule (held before its first strip, its last frame after a strip, a loop
/// wrapped), in its palette (the king all ink before the stage's marker "reveal"). Made from the model data
/// alone; JiruanRenderTests renders the sequence playing on a scene at the same times against the same
/// references. A figure wholly hidden (under the ground, or not yet out of the rift) is left out: Freestyle
/// would ink it through what hides it. Particles aren't in these (they have no Blender reference).
/// </summary>
public static class JiruanStage
{
    public static readonly double[] Times = [0, 1.4, 2.5, 3.4, 4.6, 7];
    public const int Width = 600, Height = 340;
    public const string Background = "#06030b";
    /// <summary>The stage's marker "reveal" (frame 269 of its 60 fps timeline, from the first).</summary>
    public const double Reveal = 269 / 60.0;

    public static string NameAt(double time) => "jiruan-" + time.ToString("F2", CultureInfo.InvariantCulture);

    static readonly Lazy<Scene3D[]> All = new(() =>
    {
        var stage = ModelData.Load(JiruanIntro.StageFile);
        var acting = AnimationClip.AllOf(ModelData.Load(JiruanIntro.ActingFile));
        return [.. Times.Select(t => At(stage, acting, t, hidden: false))];
    });

    public static Scene3D[] Scenes => All.Value;

    /// <summary>The scene at <paramref name="time"/> with the hidden figures too (as the sequence draws them, under the ground or tiny in the rift): the engine's own comparison.</summary>
    public static Scene3D WithHidden(double time) =>
        At(ModelData.Load(JiruanIntro.StageFile), AnimationClip.AllOf(ModelData.Load(JiruanIntro.ActingFile)), time, hidden: true);

    /// <summary>The clip and the time into it a figure's strips give at <paramref name="time"/>.</summary>
    public static (AnimationClip Clip, double Time) PoseAt((string Clip, double Start, bool Loop)[] strips, double time, AnimationClip[] acting)
    {
        var index = Array.FindLastIndex(strips, s => s.Start <= time);
        var (name, start, loop) = strips[Math.Max(index, 0)];
        var clip = acting.Single(c => c.Name == name);
        var into = Math.Max(time - start, 0);
        return (clip, loop ? into % clip.Duration : Math.Min(into, clip.Duration));
    }

    static Scene3D At(ModelData stage, AnimationClip[] acting, double time, bool hidden)
    {
        var instance = new SceneNode().Instance(stage);
        instance.Apply(instance.Clip("Scene"), time);
        var camera = new Camera();
        instance.Cameras.Single().ApplyTo(camera);
        var view = new Camera3D(camera.Projection, camera.Position, camera.Target, camera.VerticalFieldOfView * 180 / MathF.PI, Near: camera.Near, Far: camera.Far)
        {
            Up = camera.Up,
        };
        var models = new List<ModelObject3D> { new("jiruan.glb", Vector3.Zero, Vector3.Zero, 1, Scenery: true) { Objects = true, ClipTime = time } };
        var silhouette = new Dictionary<string, string> { ["cloth"] = JiruanIntro.Silhouette, ["skin"] = JiruanIntro.Silhouette };
        var figures = new List<(string Mark, (string, double, bool)[] Strips, Dictionary<string, string> Palette)>
        {
            ("boss", JiruanIntro.KingStrips, time < Reveal ? silhouette : new() { ["cloth"] = "#3b2a44", ["skin"] = "#5a3566" }),
        };
        for (var i = 0; i < JiruanIntro.Imps.Length; i++)
            figures.Add((JiruanIntro.Imps[i], JiruanIntro.ImpStrips(i), new() { ["cloth"] = "#3a2448", ["skin"] = "#6a3d78" }));
        foreach (var (mark, strips, palette) in figures)
        {
            var world = instance.NodeNamed(mark)!.World;
            var scale = new Vector3(world.M11, world.M12, world.M13).Length();
            if (!hidden && (world.Translation.Y + 1.9f * scale < 0 || scale < 0.01f))
                continue;
            var (clip, into) = PoseAt(strips, time, acting);
            models.Add(new("figure.glb", Vector3.Zero, Vector3.Zero)
            {
                Placement = world,
                World = data =>
                {
                    var skeleton = new Skeleton(data);
                    var pose = new PoseBuffer(skeleton);
                    new ClipBinding(clip, skeleton).Sample(into, pose);
                    return data.WorldMatrices(pose.ToPose());
                },
                Group = mark,
                Palette = palette,
            });
        }
        return new Scene3D(NameAt(time), Width, Height, Background, view, []) { Models = [.. models], Sun = instance.Sun!.Light };
    }
}
