using System.Numerics;
using Statventure.Graphics;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Editor.Preview;

/// <summary>
/// The 3D view the previews share (the scene view M10 built, M11-3): one <see cref="View3D"/> on the editor's background
/// and its <see cref="SceneView"/>, a frame client of the editor's document, shown in the <c>surface</c> of the preview
/// shown; each preview puts its things under a node of its own (<see cref="Begin"/>) and takes them away when it goes.
/// Its own camera is <see cref="Orbit"/>, which the scene's camera (a stage's, a cutscene's cut) replaces while set.
/// </summary>
sealed class Viewport : IDisposable
{
    /// <summary>The preview's sun when the scene gives none: Blender's sun of the 3D references' scenes, from the upper front left.</summary>
    public static readonly SunLight DefaultSun = new(new Vector3(-0.6f, 1.4f, 0.8f), 3);

    readonly SurfaceViews _views;
    UiElement? _surface;
    bool _disposed;

    public Viewport(GpuDevice gpu, SurfaceViews views)
    {
        _views = views;
        View = new View3D(gpu) { Background = PreviewPart.BackgroundColour, Sun = DefaultSun };
        Scene = new SceneView(View);
    }

    public View3D View { get; }

    public SceneView Scene { get; }

    /// <summary>The turnable camera (P33): dragged, wheeled, reset.</summary>
    public OrbitCamera Orbit { get; } = new();

    /// <summary>The node the preview shown hangs its things on (null between previews).</summary>
    public SceneNode? Content { get; private set; }

    /// <summary>A new node for a preview's things under the scene's root, the view shown in <paramref name="surface"/>.</summary>
    public SceneNode Begin(UiElement surface)
    {
        End();
        _surface = surface;
        _views.Show(surface, View);
        Content = new SceneNode("preview");
        Scene.Root.Add(Content);
        return Content;
    }

    /// <summary>The preview's things taken away: its node, the scene's camera and sun, the view's sun as at the start.</summary>
    public void End()
    {
        Content?.Remove();
        Content = null;
        Scene.Camera = null;
        Scene.Sun = null;
        View.Sun = DefaultSun;
        Scene.Refresh();
        if (_surface is { } surface)
            _views.Hide(surface);
        _surface = null;
    }

    /// <summary>The orbit camera into the view, unless the scene's camera is set.</summary>
    public void ApplyOrbit()
    {
        if (Scene.Camera is null)
            Orbit.ApplyTo(View.Camera);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        End();
        View.Dispose();
    }
}

/// <summary>
/// The previews' turnable camera (P33): about a target at a distance, turned by dragging (a degree for every two CSS px)
/// and zoomed by the wheel (a notch of 120 px a fifth nearer or farther), from where <see cref="Frame"/> put it, which
/// Reset goes back to. Perspective, 40° from top to bottom.
/// </summary>
public sealed class OrbitCamera
{
    /// <summary>How far the camera turns per CSS px dragged (radians).</summary>
    public const float TurnPerPixel = MathF.PI / 360;

    /// <summary>The vertical field of view (radians).</summary>
    public const float FieldOfView = 40 * MathF.PI / 180;

    Vector3 _frameTarget;
    float _frameDistance = 5, _radius = 1;

    public Vector3 Target { get; private set; }
    public float Distance { get; private set; } = 5;
    /// <summary>The turn about the vertical (radians; 0 looks along −Z from +Z).</summary>
    public float Yaw { get; private set; }
    /// <summary>How far above the target it looks down (radians, within ±85°).</summary>
    public float Pitch { get; private set; }

    public const float StartYaw = 0.6f, StartPitch = 0.35f, MaxPitch = 85 * MathF.PI / 180;

    /// <summary>
    /// Frames a ball of <paramref name="radius"/> about <paramref name="centre"/> (its whole width in the view, with a
    /// margin), from the front left above; Reset comes back here.
    /// </summary>
    public void Frame(Vector3 centre, float radius)
    {
        radius = float.IsFinite(radius) && radius > 0 ? radius : 1;
        _radius = radius;
        _frameTarget = centre;
        _frameDistance = radius * 1.25f / MathF.Sin(FieldOfView / 2);
        Reset();
    }

    /// <summary>Back to where <see cref="Frame"/> put it.</summary>
    public void Reset()
    {
        Target = _frameTarget;
        Distance = _frameDistance;
        Yaw = StartYaw;
        Pitch = StartPitch;
    }

    /// <summary>Turned by a drag of <paramref name="dx"/>, <paramref name="dy"/> CSS px (right turns it about the target to the left, down looks from lower).</summary>
    public void Turn(float dx, float dy)
    {
        Yaw -= dx * TurnPerPixel;
        Pitch = Math.Clamp(Pitch + dy * TurnPerPixel, -MaxPitch, MaxPitch);
    }

    /// <summary>Nearer (negative) or farther by a wheel's <paramref name="deltaY"/> (CSS px; a notch is 120), within a hundredth and a hundred times the framing.</summary>
    public void Zoom(float deltaY)
    {
        Distance = Math.Clamp(Distance * MathF.Pow(1.2f, deltaY / 120), _frameDistance / 100, _frameDistance * 100);
    }

    /// <summary>Where the camera is now.</summary>
    public Vector3 Position => Target + Distance * new Vector3(MathF.Cos(Pitch) * MathF.Sin(Yaw), MathF.Sin(Pitch), MathF.Cos(Pitch) * MathF.Cos(Yaw));

    /// <summary>Writes the camera into <paramref name="camera"/> (the view compares its settings, so it draws again only when it moved).</summary>
    public void ApplyTo(Camera camera)
    {
        camera.Projection = Projection.Perspective;
        camera.Position = Position;
        camera.Target = Target;
        camera.Up = Vector3.UnitY;
        camera.VerticalFieldOfView = FieldOfView;
        // What was framed in depth from wherever the camera is (inside it, from very near).
        camera.Near = MathF.Max(Distance / 1000, Distance - 2 * _radius);
        camera.Far = Distance + 4 * _radius;
    }
}
