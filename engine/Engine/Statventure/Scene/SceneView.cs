using System.Numerics;
using Statventure.Animation;
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
/// Sequences play on it (<see cref="Play"/>, Docs/Scene.md, step 4): each frame first the sequences
/// playing (their state, the figures they hold), then the other figures at the battle pace and the
/// instanced models it animates (<see cref="Animate"/>), then one refresh; in the events step every
/// figure's events, then each sequence's event cues.
/// </summary>
public sealed class SceneView : IUiFrameClient
{
    readonly List<SceneAttachment> _attachments = [];
    readonly List<Actor> _actors = [];
    readonly List<DrawItem> _items = [];
    readonly List<SequencePlayer> _players = [];
    readonly Dictionary<Actor, SequencePlayer> _holders = [];
    readonly List<(ModelInstance Instance, AnimationStack Stack, double Time)> _animated = [];
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

    /// <summary>
    /// Moves the sequences playing on (their own clocks: their state, the figures they hold), then every
    /// other figure's clock and the clips of the models it animates by <paramref name="seconds"/> times
    /// the rate (the battle pace), then refreshes.
    /// </summary>
    public void Advance(double seconds)
    {
        Collect();
        foreach (var player in _players.ToArray())
            player.AdvanceFrame(seconds);
        foreach (var actor in _actors)
            if (!_holders.ContainsKey(actor))
                actor.Animator.Advance(seconds * Rate);
        for (var i = 0; i < _animated.Count; i++)
        {
            var (instance, stack, time) = _animated[i];
            time += seconds * Rate;
            _animated[i] = (instance, stack, time);
            instance.Apply(stack, time);
        }
        Refresh();
    }

    /// <summary>
    /// Plays <paramref name="sequence"/> on this scene with <paramref name="bindings"/> (Docs/Scene.md,
    /// step 4): everything is checked first, an <see cref="ArgumentException"/> naming the problem
    /// changing nothing (S4-6); then its stages are instanced under nodes named after them under the
    /// root, the figures it binds are held (their clocks follow it) and its tracks begin; its first frame
    /// is the next. Any number play at once (Q20). <paramref name="document"/> is the document whose
    /// frames run the scene: its media environment chooses the reduced variant (M10-4) and the time it
    /// spends hidden is dropped from the sequence's clock (M10-7); without one, the full variant plays.
    /// </summary>
    public SequencePlayer Play(Sequence sequence, SequenceBindings bindings, UiDocument? document = null) =>
        SequencePlayer.Start(this, sequence, bindings, document);

    /// <summary>The sequences playing, in the order they started.</summary>
    public IReadOnlyList<SequencePlayer> Playing => _players;

    /// <summary>
    /// Plays <paramref name="stack"/> (on <paramref name="instance"/>'s <see cref="ModelInstance.Skeleton"/>)
    /// on the instance's nodes from <paramref name="time"/> on, on the scene's clock at its battle pace
    /// (<see cref="Rate"/>), as the figures no sequence holds (S4-7: a stage's clips outside a sequence);
    /// null stops it (the nodes keep their pose).
    /// </summary>
    public void Animate(ModelInstance instance, AnimationStack? stack, double time = 0)
    {
        ArgumentNullException.ThrowIfNull(instance);
        _animated.RemoveAll(a => a.Instance == instance);
        if (stack == null)
            return;
        if (stack.Skeleton.Model != instance.Data)
            throw new ArgumentException("the stack is on another model's nodes: make it on the instance's Skeleton");
        _animated.Add((instance, stack, time));
    }

    /// <summary>The sequence playing that holds <paramref name="actor"/>, or null.</summary>
    internal SequencePlayer? HolderOf(Actor actor) => _holders.GetValueOrDefault(actor);

    internal void Hold(Actor actor, SequencePlayer player) => _holders[actor] = player;

    internal void Release(Actor actor) => _holders.Remove(actor);

    internal void AddPlayer(SequencePlayer player) => _players.Add(player);

    internal void RemovePlayer(SequencePlayer player) => _players.Remove(player);

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
        foreach (var player in _players.ToArray())
            player.DispatchFrame(loop);
    }
}
