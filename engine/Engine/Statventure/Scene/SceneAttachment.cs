using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Scene;

/// <summary>What a scene's view draws with, worked out once per refresh: the cel style, the view's camera and its picture's height.</summary>
public readonly record struct SceneDrawing(CelStyle Style, Camera Camera, int ViewportHeight);

/// <summary>
/// Something hanging on a <see cref="SceneNode"/> and placed by its world matrix (Docs/Scene.md,
/// "Attachments"): a figure (<see cref="Actor"/>), a model (<see cref="PlacedModel"/>, or a model's
/// hierarchy as nodes, <see cref="ModelInstance"/>), terrain (<see cref="PlacedTerrain"/>), a camera
/// (<see cref="SceneCamera"/>) or a sun (<see cref="SceneSun"/>). A node can carry several.
/// </summary>
public abstract class SceneAttachment
{
    /// <summary>The node it hangs on (<see cref="SceneNode.Attach"/>), or null.</summary>
    public SceneNode? Node { get; internal set; }

    /// <summary>Tells the scene the attachment changed in a way it can't see (a palette or pose edited in place): it's drawn again.</summary>
    public void Changed() => Node?.Scene?.Changed(structure: false);

    /// <summary>Adds its draw items, placed by its node.</summary>
    internal virtual void Draw(List<DrawItem> items, SceneDrawing drawing)
    {
    }

    /// <summary>Whether what it draws depends on the view's camera and size (terrain's level of detail).</summary>
    internal virtual bool FollowsTheView => false;

    /// <summary>Sets a property, telling the scene when it changed.</summary>
    protected void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;
        field = value;
        Changed();
    }
}

/// <summary>
/// A model placed by its node as one model (one outline, one glow mask, the hatching anchored at the
/// node's origin; its own hierarchy not scene nodes), in <see cref="Palette"/>'s tones,
/// <see cref="Scenery"/> receiving shadows, its nodes and joints moved by <see cref="Pose"/>.
/// Picking gives the placed model back.
/// </summary>
public sealed class PlacedModel(Model model) : SceneAttachment
{
    Palette? _palette;
    bool _scenery;
    ModelPose? _pose;

    public Model Model { get; } = model;
    public Palette? Palette { get => _palette; set => Set(ref _palette, value); }
    public bool Scenery { get => _scenery; set => Set(ref _scenery, value); }
    /// <summary>Local transforms for some of the model's nodes (edited in place: call <see cref="SceneAttachment.Changed"/>).</summary>
    public ModelPose? Pose { get => _pose; set => Set(ref _pose, value); }

    internal override void Draw(List<DrawItem> items, SceneDrawing drawing) =>
        Model.Draw(items, Node!.World, _palette, drawing.Style, _scenery, _pose, owner: this);
}

/// <summary>
/// Terrain placed by its node's translation (Docs/Scene.md, Q3; its cover blending works in world X
/// and Z, so the node's rotation and scale don't turn or size it), in <see cref="Palette"/>'s tones;
/// each chunk's level of detail from the view's camera and size. Picking gives the placed terrain back.
/// </summary>
public sealed class PlacedTerrain(Terrain terrain) : SceneAttachment
{
    Palette? _palette;

    public Terrain Terrain { get; } = terrain;
    public Palette? Palette { get => _palette; set => Set(ref _palette, value); }

    internal override bool FollowsTheView => true;

    internal override void Draw(List<DrawItem> items, SceneDrawing drawing) =>
        Terrain.Draw(items, Node!.World.Translation, drawing.Camera, drawing.ViewportHeight, _palette, drawing.Style, this);
}

/// <summary>
/// A camera on a node (glTF's camera: it looks down its node's local −Z with +Y up and +X to the
/// right): the view's position is the node's world origin, its target one unit along the node's −Z,
/// its up the node's +Y; the projection and its settings are the camera's own. The scene's view
/// takes it when it is the scene's <see cref="SceneView.Camera"/>.
/// </summary>
public sealed class SceneCamera : SceneAttachment
{
    public Projection Projection { get; set; } = Projection.Perspective;
    /// <summary>Perspective: the vertical field of view in radians.</summary>
    public float VerticalFieldOfView { get; set; } = MathF.PI / 4;
    /// <summary>Orthographic: the height of the world in view.</summary>
    public float OrthographicHeight { get; set; } = 10;
    public float Near { get; set; } = 0.1f;
    public float Far { get; set; } = 100;

    /// <summary>Sets <paramref name="camera"/> to this camera as its node places it now.</summary>
    public void ApplyTo(Camera camera)
    {
        var world = Node?.World ?? Matrix4x4.Identity;
        var position = world.Translation;
        camera.Projection = Projection;
        camera.Position = position;
        camera.Target = position + Vector3.Normalize(Vector3.TransformNormal(-Vector3.UnitZ, world));
        camera.Up = Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitY, world));
        camera.VerticalFieldOfView = VerticalFieldOfView;
        camera.OrthographicHeight = OrthographicHeight;
        camera.Near = Near;
        camera.Far = Far;
    }
}

/// <summary>
/// A sun on a node (glTF's directional light: it shines down its node's local −Z): its direction
/// toward the sun is the node's +Z, its strength and angle (degrees across) its own. The scene's view
/// takes it when it is the scene's <see cref="SceneView.Sun"/>.
/// </summary>
public sealed class SceneSun : SceneAttachment
{
    public float Strength { get; set; } = 3;
    /// <summary>The sun's size in degrees (Blender's sun angle), which softens shadows.</summary>
    public float Angle { get; set; } = 3;

    /// <summary>The light as its node places it now.</summary>
    public SunLight Light => new(Vector3.Normalize(Vector3.TransformNormal(Vector3.UnitZ, Node?.World ?? Matrix4x4.Identity)), Strength) { Angle = Angle };
}
