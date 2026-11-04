using System.Numerics;
using Statventure.Render3D;
using Statventure.UI.Dom;

namespace Statventure.Scene;

/// <summary>
/// A scene (Docs/Scene.md, "The node tree and the scene"; it replaces M7's animated view, Q1): a
/// root node whose tree places everything the 3D view shows, and the <see cref="View3D"/> it fills.
/// Added to a document (<see cref="UiDocument.AddFrameClient"/>), each frame, in the frame's
/// animation timing update (the clock CSS and Web Animations run on), it advances its figures'
/// animators by the time since its last frame times its <see cref="Rate"/> (the battle pace: the old
/// game's "slow" is 1 / 1.5); it gives the view new draw items only when something it draws changed
/// (a figure's pose or gear, a node's transform, the tree, an attachment; terrain also with the
/// view's camera and size), so a still scene isn't drawn again; it sets the view's camera and sun
/// from <see cref="Camera"/> and <see cref="Sun"/> when they're set (what hangs below a hidden node
/// isn't drawn); and it dispatches the animators'
/// events and state notices with the frame's animation events. The view is shown in a
/// <c>surface</c> element as any view (<see cref="UI.SurfaceViews"/>). Figures, attachments and
/// events go in the tree's order (depth first, a node's attachments before its children's).
/// </summary>
public sealed class SceneView : IUiFrameClient
{
    readonly List<SceneAttachment> _attachments = [];
    readonly List<Actor> _actors = [];
    readonly List<DrawItem> _items = [];
    bool _structureChanged = true, _changed = true;
    (Projection, Vector3, Vector3, Vector3, float, float, float, float, int) _viewDrawn;
    CelStyle? _styleDrawn;

    public SceneView(View3D view)
    {
        View = view;
        Root = new SceneNode() { Owner = this };
    }

    public View3D View { get; }
    public SceneNode Root { get; }
    /// <summary>How fast the figures' clocks run against the document's (the battle pace); 0 holds them.</summary>
    public double Rate { get; set; } = 1;
    /// <summary>The camera the view takes (a <see cref="SceneCamera"/> on a node); null leaves the view's own.</summary>
    public SceneCamera? Camera { get; set; }
    /// <summary>The sun the view takes (a <see cref="SceneSun"/> on a node); null leaves the view's own.</summary>
    public SceneSun? Sun { get; set; }

    /// <summary>The figures in the tree, in its order.</summary>
    public IReadOnlyList<Actor> Actors
    {
        get
        {
            Collect();
            return _actors;
        }
    }

    /// <summary>A node, an attachment or the tree changed: the view gets new draw items at the next refresh.</summary>
    internal void Changed(bool structure)
    {
        _changed = true;
        _structureChanged |= structure;
    }

    void Collect()
    {
        if (!_structureChanged)
            return;
        _structureChanged = false;
        _attachments.Clear();
        _actors.Clear();
        foreach (var node in Root.Descendants().Prepend(Root))
            foreach (var attachment in node.Attachments)
            {
                _attachments.Add(attachment);
                if (attachment is Actor actor)
                    _actors.Add(actor);
            }
    }

    /// <summary>Moves every figure's clock on by <paramref name="seconds"/> (times the rate), then refreshes.</summary>
    public void Advance(double seconds)
    {
        Collect();
        foreach (var actor in _actors)
            actor.Animator.Advance(seconds * Rate);
        Refresh();
    }

    /// <summary>
    /// Brings the view up to date: its camera and sun from their nodes, and new draw items if
    /// anything drawn changed; returns whether the items were given again.
    /// </summary>
    public bool Refresh()
    {
        Collect();
        Camera?.ApplyTo(View.Camera);
        if (Sun is { } sun && sun.Light is var light && light != View.Sun)
            View.Sun = light;
        var changed = _changed;
        foreach (var actor in _actors)
            changed |= actor.Refresh();
        // Terrain's level of detail follows the view's camera and the height of its last picture
        // (the finest level before the first).
        var camera = View.Camera;
        var height = View.Picture?.Height ?? int.MaxValue;
        var viewNow = (camera.Projection, camera.Position, camera.Target, camera.Up, camera.VerticalFieldOfView, camera.OrthographicHeight, camera.Near, camera.Far, height);
        if (viewNow != _viewDrawn && _attachments.Any(a => a.FollowsTheView))
            changed = true;
        // The style gives the shadow tones the palettes don't.
        if (View.Style != _styleDrawn)
            changed = true;
        if (!changed)
            return false;
        _changed = false;
        _viewDrawn = viewNow;
        _styleDrawn = View.Style;
        _items.Clear();
        var drawing = new SceneDrawing(View.Style, camera, height);
        foreach (var attachment in _attachments)
            if (attachment.Node!.Shown)
                attachment.Draw(_items, drawing);
        View.Items = [.. _items];
        return true;
    }

    void IUiFrameClient.AdvanceFrame(double seconds) => Advance(seconds);

    void IUiFrameClient.DispatchFrameEvents(UiEventLoop loop)
    {
        Collect();
        foreach (var actor in _actors)
            ((IUiFrameClient)actor.Animator).DispatchFrameEvents(loop);
    }
}
