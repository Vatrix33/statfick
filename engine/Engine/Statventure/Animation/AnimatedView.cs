using System.Numerics;
using Statventure.Audio;
using Statventure.Render3D;
using Statventure.UI.Dom;

namespace Statventure.Animation;

/// <summary>
/// A figure in an animated view (Docs/Animation.md, step 7): a model posed by its animator, placed
/// by <see cref="Transform"/>, coloured by <see cref="Palette"/>, with gear: items on its sockets
/// (with their own clips, if any) and garments on its bones. The figure and its gear are one model
/// for the renderer (one owner: one outline, one glow mask; the hatching anchored at its origin).
/// </summary>
public sealed class Actor
{
    readonly PoseBuffer _pose, _lastPose;
    readonly List<(PoseBuffer Now, PoseBuffer Last)> _itemPoses = [];
    Matrix4x4 _transform = Matrix4x4.Identity;
    bool _moved = true;

    public Actor(Model model, Animator animator)
    {
        Model = model;
        Animator = animator;
        _pose = new PoseBuffer(animator.Stack.Skeleton);
        _lastPose = new PoseBuffer(animator.Stack.Skeleton);
        animator.Event += clipEvent => Sounds?.OnEvent(clipEvent.Name);
    }

    /// <summary>What the figure's clip events sound like (Docs/Audio.md, step 8): played when its animator delivers them.</summary>
    public ActorSounds? Sounds { get; set; }

    public Model Model { get; }
    public Animator Animator { get; }
    /// <summary>The figure's group for the renderer (its picking owner).</summary>
    public object Owner { get; } = new();
    public Palette? Palette { get; set; }
    public List<ActorItem> Items { get; } = [];
    public List<ActorGarment> Garments { get; } = [];

    /// <summary>Model to world; setting it redraws the view.</summary>
    public Matrix4x4 Transform
    {
        get => _transform;
        set
        {
            _transform = value;
            _moved = true;
        }
    }

    /// <summary>
    /// Evaluates the figure's and its items' poses at the animator's time: whether anything differs
    /// from the last evaluation (a channel's value, the transform, gear added).
    /// </summary>
    public bool Refresh()
    {
        Animator.Stack.Evaluate(Animator.Time, _pose);
        var changed = _moved || !Same(_pose, _lastPose);
        _lastPose.CopyFrom(_pose);
        _moved = false;
        while (_itemPoses.Count < Items.Count)
        {
            var item = Items[_itemPoses.Count];
            var skeleton = item.Animation?.Skeleton ?? new Skeleton(item.Model.Data);
            _itemPoses.Add((new PoseBuffer(skeleton), new PoseBuffer(skeleton)));
            changed = true;
        }
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i].Animation is not { } animation)
                continue;
            var (now, last) = _itemPoses[i];
            animation.Evaluate(Animator.Time, now);
            changed |= !Same(now, last);
            last.CopyFrom(now);
        }
        return changed;
    }

    /// <summary>Adds the figure's and its gear's draw items for the last evaluated poses.</summary>
    public void Draw(List<DrawItem> items, CelStyle style)
    {
        var pose = _pose.ToPose();
        var data = Model.Data;
        Model.Draw(items, _transform, Palette, style, pose: pose, owner: Owner);
        if (Items.Count == 0 && Garments.Count == 0)
            return;
        var world = data.WorldMatrices(pose);
        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var itemPose = item.Animation != null ? _itemPoses[i].Now.ToPose() : null;
            item.Model.Draw(items, item.Socket.ItemTransform(world, _transform), Palette, style, pose: itemPose, owner: Owner, anchor: _transform.Translation);
        }
        var rest = data.WorldMatrices();
        foreach (var garment in Garments)
            garment.Model.Draw(items, _transform, Palette, style, owner: Owner, world: garment.Garment.WorldMatrices(rest, world), anchor: _transform.Translation);
    }

    static bool Same(PoseBuffer a, PoseBuffer b) =>
        a.Set.AsSpan().SequenceEqual(b.Set) && a.Translation.AsSpan().SequenceEqual(b.Translation)
        && a.Rotation.AsSpan().SequenceEqual(b.Rotation) && a.Scale.AsSpan().SequenceEqual(b.Scale);
}

/// <summary>An item on a socket of its wearer, with its own clips played with the wearer's states if <see cref="Animation"/> is given.</summary>
public sealed record ActorItem(Model Model, Socket Socket, ItemAnimation? Animation = null);

/// <summary>A garment on its wearer's bones.</summary>
public sealed record ActorGarment(Model Model, Garment Garment);

/// <summary>
/// A 3D view whose figures animate with the document's frames (Docs/Animation.md, step 7): added to
/// a document (<see cref="UiDocument.AddFrameClient"/>), each frame it advances its actors'
/// animators by the time since its last frame times its <see cref="Rate"/> (the battle pace: the old
/// game's "slow" is 1 / 1.5), in the frame's animation timing update, so clips run on the same clock
/// as CSS and Web Animations; it gives its view new draw items only when a figure's pose, placement
/// or gear changed (or the actors did), so a still scene isn't drawn again; and it dispatches the
/// animators' events and state notices with the frame's animation events.
/// </summary>
public sealed class AnimatedView(View3D view) : IUiFrameClient
{
    Actor[] _shown = [];

    public View3D View { get; } = view;
    public List<Actor> Actors { get; } = [];
    public double Rate { get; set; } = 1;

    /// <summary>Moves every actor's clock on by <paramref name="seconds"/> (times the rate), then refreshes.</summary>
    public void Advance(double seconds)
    {
        foreach (var actor in Actors)
            actor.Animator.Advance(seconds * Rate);
        Refresh();
    }

    /// <summary>Evaluates the actors and gives the view new draw items if anything changed; returns whether it did.</summary>
    public bool Refresh()
    {
        var changed = !_shown.SequenceEqual(Actors);
        foreach (var actor in Actors)
            changed |= actor.Refresh();
        if (!changed)
            return false;
        _shown = [.. Actors];
        var items = new List<DrawItem>();
        foreach (var actor in Actors)
            actor.Draw(items, View.Style);
        View.Items = items;
        return true;
    }

    void IUiFrameClient.AdvanceFrame(double seconds) => Advance(seconds);

    void IUiFrameClient.DispatchFrameEvents(UiEventLoop loop)
    {
        foreach (var actor in Actors)
            ((IUiFrameClient)actor.Animator).DispatchFrameEvents(loop);
    }
}
