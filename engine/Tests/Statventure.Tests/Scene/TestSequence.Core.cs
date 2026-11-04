using System.Reflection;
using Statventure.Animation;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Render3D;

namespace Statventure.Tests.Scene;

/// <summary>
/// The test sequence that uses every track kind (Docs/Scene.md, M10-2: "a test sequence that uses every
/// track"), a partial class with one file per step of M10: this one (step 4) has the roles, the stage
/// and the clip, transform, visibility, animation, palette and event tracks and the reduced variant;
/// step 5 adds the camera, light and particle tracks (<c>TestSequence.Camera.cs</c>), step 6 the audio
/// and UI tracks (<c>TestSequence.Sound.cs</c>).
/// </summary>
internal static partial class TestSequence
{
    public const string StagePath = "models/stages/hierarchy";
    public const double Duration = 2;

    /// <summary>
    /// The stage: the hierarchy test model (Blender objects Base over Post over Tip, Squash over Tilted,
    /// Mirror over Wing over Feather, the armature Rig with its bones arm and hand and Charm on hand;
    /// clips "base", 1 s, keying Base's location, rotation and scale, and "rig", 1 s, keying the bones'
    /// rotations), with timeline markers on its clip "base" as the stage's export puts them there (the
    /// scene property <c>events</c>, Docs/Scene.md, "How it works", step 4): "open" at 0.4 s, "reveal"
    /// at 0.75 s.
    /// </summary>
    public static ModelData Stage() => WithMarkers(ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, "hierarchy.glb")), "base",
        new("open", 0.4), new("reveal", 0.75));

    /// <summary>A copy of <paramref name="model"/> whose clip <paramref name="clip"/> has these markers (as clip events).</summary>
    public static ModelData WithMarkers(ModelData model, string clip, params ModelAnimationEvent[] markers)
    {
        var copy = RecordSerializer.FromBinary<ModelData>(RecordSerializer.ToBinary(model));
        var animations = copy.Animations.Select(a => a.Name == clip ? a with { Events = markers } : a).ToArray();
        typeof(ModelData).GetProperty(nameof(ModelData.Animations), BindingFlags.Public | BindingFlags.Instance)!.SetValue(copy, animations);
        return copy;
    }

    /// <summary>The whole test sequence, built and checked against <see cref="Stage"/>.</summary>
    public static Sequence Build(ModelData? stage = null)
    {
        var builder = new SequenceBuilder("test", Duration).Stage(StagePath, stage ?? Stage());
        Core(builder);
        Camera(builder);
        Sound(builder);
        return builder.Build();
    }

    static partial void Camera(SequenceBuilder builder);

    static partial void Sound(SequenceBuilder builder);

    /// <summary>
    /// Step 4's tracks: the stage's clips (base from 0, rig from 0.5 s), the bound node "pivot" moved and
    /// turned, the bone arm raised over the rig clip's rotation (S4-4), Mirror hidden at 0.5 s and shown
    /// again at the marker "reveal", the figure "hero" on the stage's Tip with a swing strip and cues for
    /// its machine, the hero's palette all ink until 0.1 s after "reveal" and then its own, the model
    /// "prop" in the bound palette "tint" from 1.5 s, and event cues at 0, at "open", at 1 s and at the end. The
    /// reduced variant plays 0–0.5 s, then holds 1.5 s for 1 s.
    /// </summary>
    static void Core(SequenceBuilder b) => b
        .Role("hero", RoleKind.Actor).Role("pivot", RoleKind.Node).Role("prop", RoleKind.Model).Role("tint", RoleKind.Palette)
        .Clip("hierarchy", "base")
        .Clip("hierarchy", "rig", start: 0.5)
        .Transform("pivot", t => t
            .Location(x: new FCurve([FCurveKey.Linear(0, 0), FCurveKey.Linear(2, 4)]))
            .Rotation(z: new FCurve([FCurveKey.Bezier(0, 0), FCurveKey.Bezier(2, Math.PI / 2)])))
        .Transform("hierarchy/arm", t => t.Location(y: new FCurve([FCurveKey.Linear(0, 0.5), FCurveKey.Linear(2, 1.5)])))
        .Visibility("hierarchy/Mirror", v => v.Hide(0.5).Show(CueTime.At("hierarchy/reveal")))
        .Animation("hero", "hierarchy/Tip", a => a
            .Strip("swing", start: 0.25, blendIn: 0.1)
            .Float(0, "swingSpeed", 1.5).Int(1.2, "combo", 2).Trigger(1, "attack").Bool(1.8, "dead", true))
        .Palette("hero", p => p.Fill(0, "#000000").Own(CueTime.At("hierarchy/reveal", 0.1)))
        .Palette("prop", p => p.Role(1.5, "tint"))
        .Events(e => e.Event(0, "start").Event(CueTime.At("hierarchy/open"), "open").Event(1, "middle").Event(Duration, "end"))
        .Reduced(r => r.Span(0, 0.5).Hold(1.5, 1));
}
