using System.Numerics;
using Statventure.Animation;
using Statventure.Editor.Preview;
using Statventure.Platform;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Editor.Form;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;

namespace Statventure.Tests.Editor.Preview;

using PreviewPart = Statventure.Editor.Preview.PreviewPart;

/// <summary>
/// The 3D previews (Docs/Editor.md, "The preview (step 13)"; P32, P33, P35) on the tests' headless GPU: a particle
/// effect loops at the origin in the preview's palette, with a seed and Restart; a model shows in its own colours with
/// its clips played as applying them by hand would, a stage's cameras looked through and the turnable camera dragged,
/// wheeled and reset; a valid edit is shown at once, an invalid one keeps the last valid state.
/// </summary>
[Collection("GPU")]
public class ScenePreviewTests(GpuFixture gpu)
{
    const string SparksPath = "models/effects/sparks", DollyPath = "models/stages/dolly", HierarchyPath = "models/hierarchy";

    static readonly ParticleEffect Sparks = new()
    {
        Shape = new ParticleShape.Disc(), StartColor = "spark", EndColor = "ash", Life = new(0.6f, 1), Size = 0.15f, Speed = new(1, 2),
        Spawn = new ParticleSpawn.Point(), FadeOut = 0.3f, Gravity = new Vector3(0, -2, 0),
    };

    /// <summary>A model's copy with its materials or roots replaced.</summary>
    static ModelData Copy(ModelData data, IReadOnlyList<ModelMaterial>? materials = null, IReadOnlyList<int>? roots = null) => new()
    {
        Nodes = data.Nodes, Meshes = data.Meshes, Materials = materials ?? data.Materials, Skins = data.Skins, Animations = data.Animations,
        Roots = roots ?? data.Roots, Cameras = data.Cameras, Lights = data.Lights,
    };

    static ModelData Model(string name) => ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, name + ".glb"));

    PreviewEditor Open() => new(w =>
    {
        w.AddRecord(SparksPath, Sparks);
        w.AddRecord(DollyPath, Model("dolly"));
        w.AddRecord(HierarchyPath, Model("hierarchy"));
    }, gpu: gpu.Device);

    /// <summary><paramref name="count"/> frames of the document, a sixtieth of a second apart.</summary>
    static void Frames(PreviewEditor p, int count)
    {
        for (var i = 0; i < count; i++)
            p.Editor.Frame();
    }

    // ---------- Particles ----------

    [Fact]
    public void AParticleEffectLoopsAtTheOriginInThePreviewsPalette()
    {
        using var p = Open();
        p.Select(SparksPath);
        var preview = Assert.IsType<ParticlePreview>(p.Preview.Shown);
        var emitter = preview.Emitter!;
        Assert.Equal(Sparks, emitter.Effect);
        Assert.Equal((20, 20f, 2f, 1), (emitter.Burst, emitter.Rate, emitter.Duration, emitter.Seed));
        Assert.Equal(Matrix4x4.Identity, emitter.Node!.World);
        // A distinct colour per slot named, the start's first (P32).
        Assert.Equal(Colors.LinearFromHex(PreviewColours.Distinct[0]), emitter.Palette!.Slots["spark"].Lit);
        Assert.Equal(Colors.LinearFromHex(PreviewColours.Distinct[1]), emitter.Palette.Slots["ash"].Lit);
        Assert.Equal(["#d77a50", "#5f9fd3"], p.Document.QuerySelectorAll(".preview-part .preview-hex").Select(f => f.Value));
        // Its particles are the view's: the burst and the stream's first.
        Frames(p, 6);
        Assert.Equal(emitter.Particles().ToList(), p.Preview.Viewport!.View.Particles);
        Assert.InRange(p.Preview.Viewport.View.Particles.Count, 21, 23);
        // Started again once its last particle has died: its last birth (2 lives of 1 s, 20 a second: 1.95 s) and the longest life.
        Assert.Equal(2.95f, emitter.Emitter.End, 5);
        var last = 0.0;
        for (var i = 0; i < 400 && preview.Starts == 1; i++)
        {
            last = emitter.Time;
            p.Editor.Frame();
        }
        Assert.Equal(2, preview.Starts);
        // In the frame after the one its clock reached the end in (not a frame later, once the scene took it away).
        Assert.True(last >= emitter.Emitter.End && last < emitter.Emitter.End + 1 / 60.0, $"restarted at {last}");
        Assert.NotSame(emitter, preview.Emitter);
        Assert.False(preview.Emitter!.Done);
        Assert.Null(emitter.Node);
    }

    [Fact]
    public void TheSeedRestartAndColoursAreTheUsers()
    {
        using var p = Open();
        p.Select(SparksPath);
        var preview = (ParticlePreview)p.Preview.Shown!;
        p.Editor.Enter(p.Control("seed"), "5");
        Assert.Equal((5, 2), (preview.Emitter!.Seed, preview.Starts));
        Frames(p, 10);
        p.Editor.Click(p.Control("restart"));
        Assert.Equal(3, preview.Starts);
        Assert.Equal(5, preview.Emitter!.Seed);
        // The emitter before taken away: only the new one's particles.
        Assert.Single(preview.Emitter.Node!.Attachments.OfType<SceneEmitter>());
        p.Editor.Frame();
        Assert.Equal(preview.Emitter.Particles().ToList(), p.Preview.Viewport!.View.Particles);
        // A colour typed for a slot: the emitter's palette at once, kept while the editor runs (not in the pack).
        var ash = p.Document.QuerySelector(".preview-part .preview-hex[data-slot=ash]")!;
        p.Editor.Enter(ash, "#00FF00");
        Assert.Equal("#00ff00", ash.Value);
        Assert.Equal(Colors.LinearFromHex("#00ff00"), preview.Emitter.Palette!.Slots["ash"].Lit);
        p.Editor.Enter(ash, "green");
        Assert.Equal("#00ff00", ash.Value);
        p.Select(DollyPath);
        p.Select(SparksPath);
        Assert.Equal(Colors.LinearFromHex("#00ff00"), ((ParticlePreview)p.Preview.Shown!).Emitter!.Palette!.Slots["ash"].Lit);
    }

    [Fact]
    public void AValidEditStartsTheLoopWithItAndAnInvalidOneKeepsTheLast()
    {
        using var p = Open();
        p.Select(SparksPath);
        var preview = (ParticlePreview)p.Preview.Shown!;
        var longer = Sparks with { Life = new(2, 3) };
        p.Commit(SparksPath, longer);
        Assert.Equal((longer, 2, 6f), (preview.Effect, preview.Starts, preview.Emitter!.Duration));
        Assert.Equal("", p.Note);
        p.Break(SparksPath, "Shape");
        Assert.Equal("This version doesn't build: the preview keeps the last valid version.", p.Note);
        Assert.Equal((longer, 2), (preview.Effect, preview.Starts));
        p.Commit(SparksPath, Sparks with { Opacity = 2 });
        Assert.StartsWith("The particle effect isn't valid (its opacity (2) must be from 0 to 1", p.Note);
        var note = p.Document.QuerySelector(".preview-part .preview-note")!;
        Assert.False(note.HasAttribute("hidden"));
        Assert.Equal((longer, 2), (preview.Effect, preview.Starts));
        // A new slot named: its own row and colour.
        p.Commit(SparksPath, Sparks with { EndColor = "smoke" });
        // Valid again: the note goes.
        Assert.True(note.HasAttribute("hidden"));
        Assert.Equal(["spark", "smoke"], p.Document.QuerySelectorAll(".preview-part .preview-hex").Select(f => f.GetAttribute("data-slot")));
    }

    [Fact]
    public void TheCameraFramesHowFarTheParticlesGo()
    {
        // Without drag: 2 m/s for 1 s, gravity's 2 m/s² for 1 s (1 m), half the size: 3.075 m.
        Assert.Equal(3.075f, ParticlePreview.Reach(Sparks), 5);
        // With drag 0.5: (2 − 4)(1 − e^−0.5)/0.5 + 4·1 … each part at its greatest: 2(1 − e^−0.5)/0.5 + 2(1/0.5 − (1 − e^−0.5)/0.25).
        var dragged = Sparks with { Drag = 0.5f };
        var expected = 2 * (1 - Math.Exp(-0.5)) / 0.5 + 2 * (1 / 0.5 - (1 - Math.Exp(-0.5)) / 0.25) + 0.075;
        Assert.Equal(expected, ParticlePreview.Reach(dragged), 5);
        // A sphere's radius besides; nothing below a quarter metre.
        Assert.Equal(3.075f + 0.5f, ParticlePreview.Reach(Sparks with { Spawn = new ParticleSpawn.Sphere(0.5f) }), 5);
        Assert.Equal(0.25f, ParticlePreview.Reach(Sparks with { Speed = 0, Gravity = default, Size = 0 }));
        using var p = Open();
        p.Select(SparksPath);
        var orbit = p.Preview.Viewport!.Orbit;
        Assert.Equal(System.Numerics.Vector3.Zero, orbit.Target);
        Assert.Equal(3.075f * 1.25f / MathF.Sin(OrbitCamera.FieldOfView / 2), orbit.Distance, 4);
    }

    [Fact]
    public void TheOrbitCameraKeepsWithinItsLimits()
    {
        var orbit = new OrbitCamera();
        orbit.Frame(new System.Numerics.Vector3(1, 2, 3), 2);
        var framed = orbit.Distance;
        orbit.Turn(0, 10000);
        Assert.Equal(OrbitCamera.MaxPitch, orbit.Pitch);
        orbit.Turn(0, -20000);
        Assert.Equal(-OrbitCamera.MaxPitch, orbit.Pitch);
        orbit.Zoom(-120 * 100);
        Assert.Equal(framed / 100, orbit.Distance, 4);
        orbit.Zoom(120 * 100);
        Assert.Equal(framed * 100, orbit.Distance, 2);
        orbit.Reset();
        var camera = new Camera();
        orbit.ApplyTo(camera);
        Assert.Equal((Projection.Perspective, OrbitCamera.FieldOfView, System.Numerics.Vector3.UnitY), (camera.Projection, camera.VerticalFieldOfView, camera.Up));
        Assert.Equal(new System.Numerics.Vector3(1, 2, 3), camera.Target);
        Assert.Equal(framed, (camera.Position - camera.Target).Length(), 3);
        // What was framed in depth: from 2 radii in front of the target to 4 behind it.
        Assert.Equal(framed - 4, camera.Near, 3);
        Assert.Equal(framed + 8, camera.Far, 3);
        orbit.Zoom(-120 * 30);
        orbit.ApplyTo(camera);
        Assert.Equal(orbit.Distance / 1000, camera.Near, 6);
    }

    [Fact]
    public void TheParticlesShowInTheSurface()
    {
        using var p = Open();
        p.Select(SparksPath);
        Frames(p, 20);
        var surface = p.Document.QuerySelector(".preview-part surface")!;
        var box = surface.GetBoundingClientRect();
        Assert.Equal(200, box.Height);
        var image = p.Paint(1280, 900);
        var background = Colors.FromHex(PreviewPart.Background);
        var inside = 0;
        for (var y = (int)box.Y + 2; y < (int)(box.Y + box.Height) - 2; y++)
            for (var x = (int)box.X + 2; x < (int)(box.X + box.Width) - 2; x++)
                if (Math.Abs(image.Pixels[(y * image.Width + x) * 4] / 255f - background.X) > 0.05f)
                    inside++;
        Assert.True(inside > 100, $"{inside} pixels of particles");
    }

    // ---------- Models ----------

    [Fact]
    public void AModelsClipsPlayAsApplyingThemByHand()
    {
        using var p = Open();
        p.Select(HierarchyPath);
        var preview = Assert.IsType<ModelPreview>(p.Preview.Shown);
        var instance = preview.Instance!;
        Assert.Null(instance.Palette);
        Assert.Equal(["(none)", "base", "rig"], p.Control("clip").Options.Select(o => o.TextContent));
        Assert.True(p.Control("play").HasAttribute("disabled"));
        Assert.True(p.Control("time").HasAttribute("disabled"));
        // No cameras of its own: only the free one.
        Assert.True(p.Control("camera").HasAttribute("disabled"));
        p.Editor.Choose(p.Control("clip"), "rig");
        Assert.Equal(("rig", 0.0, false), (preview.Clip, preview.Time, preview.Playing));
        p.Editor.Click(p.Control("play"));
        Assert.Equal("Pause", p.Control("play").TextContent);
        // The click's own frame and 29 more: half a second.
        Frames(p, 29);
        Assert.Equal(0.5, preview.Time, 6);
        Assert.Equal(Nodes(By(HierarchyPath, "rig", 0.5)), Nodes(instance));
        // Looping past its end.
        Frames(p, 42);
        Assert.Equal(0.2, preview.Time, 6);
        Assert.True(preview.Playing);
        // Paused: it stays.
        p.Editor.Click(p.Control("play"));
        Frames(p, 10);
        Assert.Equal(0.2, preview.Time, 6);
        // The time slider: its end, then a step back by key.
        p.Key(p.Control("time"), "End");
        Assert.Equal(1, preview.Time, 6);
        p.Key(p.Control("time"), "ArrowLeft", 25);
        Assert.Equal(0.75, preview.Time, 6);
        Assert.False(p.Control("time").HasAttribute("disabled"));
        Assert.Equal("0.75 s", p.Control("time").Parent!.ElementChildren.Single(e => e.HasClass("preview-value")).TextContent);
        Assert.Equal(Nodes(By(HierarchyPath, "rig", 0.75)), Nodes(instance));
        // Not looping: it stops at its end.
        p.Editor.Click(p.Control("loop"));
        Assert.False(preview.Loop);
        p.Editor.Click(p.Control("play"));
        Frames(p, 30);
        Assert.Equal((1.0, false), (preview.Time, preview.Playing));
        // Played again from its end: from its start.
        p.Editor.Click(p.Control("play"));
        Assert.Equal(1 / 60.0, preview.Time, 6);
        p.Editor.Click(p.Control("play"));
        // Looping past its end mid-frame: the time over its end goes on from the start.
        p.Editor.Click(p.Control("loop"));
        Assert.True(preview.Loop);
        preview.Seek(0.93);
        p.Editor.Click(p.Control("play"));
        Frames(p, 5);
        Assert.Equal(0.03, preview.Time, 6);
        Assert.True(preview.Playing);
        // Another clip chosen: from its start, not playing.
        p.Editor.Choose(p.Control("clip"), "base");
        Assert.Equal(("base", 0.0, false), (preview.Clip, preview.Time, preview.Playing));
        // A time past its end is its end.
        preview.Seek(5);
        Assert.Equal(1, preview.Time);
        preview.Seek(-1);
        Assert.Equal(0, preview.Time);
        // Mid-clip, then no clip: the rest pose.
        preview.Seek(0.5);
        Assert.Equal(Nodes(By(HierarchyPath, "base", 0.5)), Nodes(instance));
        Assert.NotEqual(Nodes(new SceneNode().Instance(Model("hierarchy"))), Nodes(instance));
        // No clip: the rest pose.
        p.Editor.Choose(p.Control("clip"), "(none)");
        Assert.Equal(Nodes(new SceneNode().Instance(Model("hierarchy"))), Nodes(instance));
    }

    /// <summary>The model's nodes with <paramref name="clip"/> applied at <paramref name="time"/> by hand, on an instance of its own.</summary>
    static ModelInstance By(string path, string clip, double time)
    {
        var instance = new SceneNode().Instance(Model(path == HierarchyPath ? "hierarchy" : "dolly"));
        instance.Apply(instance.Clip(clip), time);
        return instance;
    }

    static List<(string, Matrix4x4)> Nodes(ModelInstance instance) =>
        [.. instance.Roots.SelectMany(r => r.Descendants().Prepend(r)).Select(n => (n.Name, n.Local))];

    [Fact]
    public void AModelIsFramedOnItsRestPose()
    {
        // Its meshes' points where their nodes put them; a skinned mesh's as they are (its joints place it).
        foreach (var name in new[] { "dolly", "rigged", "hierarchy" })
        {
            var data = Model(name);
            var worlds = data.WorldMatrices();
            var points = new List<Vector3>();
            for (var n = 0; n < data.Nodes.Count; n++)
                if (data.Nodes[n].Mesh >= 0)
                    foreach (var primitive in data.Meshes[data.Nodes[n].Mesh].Primitives)
                        points.AddRange(primitive.Joints is null ? primitive.Positions.Select(q => Vector3.Transform(q, worlds[n])) : primitive.Positions);
            var min = points.Aggregate(Vector3.Min);
            var max = points.Aggregate(Vector3.Max);
            Assert.Equal((min + max) / 2, ModelPreview.Bounds(data, out var radius));
            Assert.Equal((max - min).Length() / 2, radius, 5);
        }
        // (The rigged model's skinned meshes hang on a node 0.2 m along X, which a skinned mesh's points don't follow.)
        Assert.NotEqual(0.2f, Model("rigged").WorldMatrices()[0].M41 - 0.2f);
    }

    [Fact]
    public void AStagesCamerasAreLookedThroughAndItsSunLightsIt()
    {
        using var p = Open();
        p.Select(DollyPath);
        var preview = (ModelPreview)p.Preview.Shown!;
        var viewport = p.Preview.Viewport!;
        Assert.Same(preview.Instance!.Sun, viewport.Scene.Sun);
        Assert.Equal(["Free camera", "Camera", "Plan"], p.Control("camera").Options.Select(o => o.TextContent));
        p.Editor.Choose(p.Control("camera"), "Plan");
        Assert.Same(preview.Instance.Cameras.Single(c => c.Node!.Name == "Plan"), viewport.Scene.Camera);
        p.Editor.Frame();
        Assert.Equal(Projection.Orthographic, viewport.View.Camera.Projection);
        Assert.False(p.Control("camera").HasAttribute("disabled"));
        // Dragging and the wheel don't move a stage's camera.
        var (yaw, distance) = (viewport.Orbit.Yaw, viewport.Orbit.Distance);
        Drag(p, new Vector2(40, 0));
        var surface = p.Document.QuerySelector(".preview-part surface")!;
        p.Editor.Input.Wheel(p.Editor.Middle(surface), new Vector2(0, -120));
        p.Editor.Frame();
        Assert.Equal((yaw, distance), (viewport.Orbit.Yaw, viewport.Orbit.Distance));
        p.Editor.Choose(p.Control("camera"), "Free camera");
        Assert.Null(viewport.Scene.Camera);
        p.Editor.Frame();
        Assert.Equal(Projection.Perspective, viewport.View.Camera.Projection);
        Assert.Equal(viewport.Orbit.Position, viewport.View.Camera.Position);
    }

    static void Drag(PreviewEditor p, Vector2 by)
    {
        var surface = p.Document.QuerySelector(".preview-part surface")!;
        var from = p.Editor.Middle(surface);
        p.Editor.Drag(from, from + by / 2, from + by);
    }

    [Fact]
    public void TheDragHoldsThePointerUntilItsRelease()
    {
        using var p = Open();
        p.Select(DollyPath);
        var orbit = p.Preview.Viewport!.Orbit;
        var yaw = orbit.Yaw;
        // Dragged far out of the view, over the form: the pointer's capture keeps it turning.
        Drag(p, new Vector2(-500, 0));
        Assert.Equal(yaw + 500 * OrbitCamera.TurnPerPixel, orbit.Yaw, 5);
        // Released: moving over the view turns nothing.
        var surface = p.Document.QuerySelector(".preview-part surface")!;
        p.Editor.Input.PointerMove(p.Editor.Middle(surface));
        p.Editor.Frame();
        p.Editor.Input.PointerMove(p.Editor.Middle(surface) + new Vector2(40, 0));
        p.Editor.Frame();
        Assert.Equal(yaw + 500 * OrbitCamera.TurnPerPixel, orbit.Yaw, 5);
    }

    [Fact]
    public void TheTurnableCameraIsDraggedWheeledAndReset()
    {
        using var p = Open();
        p.Select(DollyPath);
        var viewport = p.Preview.Viewport!;
        var orbit = viewport.Orbit;
        var (yaw, pitch, distance) = (orbit.Yaw, orbit.Pitch, orbit.Distance);
        Assert.Equal((OrbitCamera.StartYaw, OrbitCamera.StartPitch), (yaw, pitch));
        Drag(p, new Vector2(60, -20));
        Assert.Equal(yaw - 60 * OrbitCamera.TurnPerPixel, orbit.Yaw, 5);
        Assert.Equal(pitch - 20 * OrbitCamera.TurnPerPixel, orbit.Pitch, 5);
        var surface = p.Document.QuerySelector(".preview-part surface")!;
        p.Editor.Input.PointerMove(p.Editor.Middle(surface));
        p.Editor.Input.Wheel(p.Editor.Middle(surface), new Vector2(0, -120));
        p.Editor.Frame();
        Assert.Equal(distance / 1.2f, orbit.Distance, 4);
        Assert.Equal(orbit.Position, viewport.View.Camera.Position);
        p.Editor.Click(p.Control("reset-view"));
        p.Editor.Frame();
        Assert.Equal((yaw, pitch, distance), (orbit.Yaw, orbit.Pitch, orbit.Distance));
        Assert.Equal(orbit.Position, viewport.View.Camera.Position);
        // Framed on the stage: its points within the view.
        var centre = ModelPreview.Bounds(Model("dolly"), out var radius);
        Assert.Equal(centre, orbit.Target);
        Assert.Equal(radius * 1.25f / MathF.Sin(OrbitCamera.FieldOfView / 2), distance, 4);
    }

    [Fact]
    public void AValidModelEditIsShownAndAnInvalidOneKeepsTheLast()
    {
        using var p = Open();
        p.Select(HierarchyPath);
        var preview = (ModelPreview)p.Preview.Shown!;
        p.Editor.Choose(p.Control("clip"), "base");
        p.Key(p.Control("time"), "End");
        var first = preview.Instance!;
        // The camera turned by the user stays where it is across edits.
        Drag(p, new Vector2(30, 0));
        var yaw = p.Preview.Viewport!.Orbit.Yaw;
        var data = Model("hierarchy");
        var recoloured = Copy(data, materials: [.. data.Materials.Select((m, i) => i == 0 ? m with { BaseColor = new Vector4(1, 0, 0, 1) } : m)]);
        p.Commit(HierarchyPath, recoloured);
        Assert.Equal("", p.Note);
        Assert.NotSame(first, preview.Instance);
        Assert.Null(first.Node!.Parent);
        Assert.Equal(new Vector4(1, 0, 0, 1), preview.Data!.Materials[0].BaseColor);
        // The clip and its time kept, and the camera.
        Assert.Equal(("base", 1.0), (preview.Clip, preview.Time));
        Assert.Equal(yaw, p.Preview.Viewport.Orbit.Yaw);
        Assert.Equal(Nodes(By(HierarchyPath, "base", 1)), Nodes(preview.Instance!));
        var shown = preview.Instance;
        p.Break(HierarchyPath, "Materials");
        Assert.Equal("This version doesn't build: the preview keeps the last valid version.", p.Note);
        Assert.Same(shown, preview.Instance);
        var broken = Copy(recoloured, roots: [99]);
        p.Commit(HierarchyPath, broken);
        Assert.StartsWith("The model isn't valid (", p.Note);
        Assert.Same(shown, preview.Instance);
    }

    [Fact]
    public void TheModelShowsInTheSurface()
    {
        using var p = Open();
        p.Select(DollyPath);
        p.Editor.Frame();
        var surface = p.Document.QuerySelector(".preview-part surface")!;
        var box = surface.GetBoundingClientRect();
        var image = p.Paint(1280, 900);
        var background = Colors.FromHex(PreviewPart.Background);
        var differ = 0;
        for (var y = (int)box.Y + 2; y < (int)(box.Y + box.Height) - 2; y++)
            for (var x = (int)box.X + 2; x < (int)(box.X + box.Width) - 2; x++)
                if (Math.Abs(image.Pixels[(y * image.Width + x) * 4 + 1] / 255f - background.Y) > 0.05f)
                    differ++;
        Assert.True(differ > box.Width * box.Height / 20, $"{differ} pixels of the model");
    }

    [Fact]
    public void LeavingThePreviewTakesItsThingsAway()
    {
        using var p = Open();
        p.Select(DollyPath);
        p.Editor.Choose(p.Control("camera"), "Camera");
        var viewport = p.Preview.Viewport!;
        Assert.NotNull(viewport.Content);
        Assert.NotNull(p.Views!.ViewOf(p.Document.QuerySelector(".preview-part surface")!));
        var surface = p.Document.QuerySelector(".preview-part surface")!;
        p.Session.Select(null);
        p.Editor.Frame();
        Assert.Null(viewport.Content);
        Assert.Empty(viewport.Scene.Root.Children);
        Assert.Null(viewport.Scene.Camera);
        Assert.Null(viewport.Scene.Sun);
        Assert.Equal(Viewport.DefaultSun, viewport.View.Sun);
        Assert.Null(p.Views.ViewOf(surface));
        Assert.Empty(p.Preview.Element.ElementChildren);
        // The session going takes the preview shown away too.
        p.Select(HierarchyPath);
        Assert.NotNull(viewport.Content);
        p.Session.Dispose();
        Assert.Null(p.Preview.Shown);
        Assert.Null(viewport.Content);
    }
}
