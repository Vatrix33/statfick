using System.Numerics;
using Statventure.Animation;
using Statventure.Audio;
using Statventure.Render3D;

namespace Statventure.Scene;

/// <summary>
/// A figure in a scene (Docs/Animation.md, "Animated views"; Docs/Scene.md, Q1): a model posed by its
/// animator, placed by the node it hangs on (its model space is the node's), coloured by
/// <see cref="Palette"/>, with gear: items on its sockets (with their own clips, if any) and garments
/// on its bones. The figure and its gear are one model for the renderer (one outline, one glow mask;
/// the hatching anchored at the node's origin); picking gives the actor back. Its bones aren't scene
/// nodes. The scene advances its animator by the frame's time times the scene's rate.
/// </summary>
public sealed class Actor : SceneAttachment
{
    readonly PoseBuffer _pose, _lastPose;
    readonly List<(PoseBuffer Now, PoseBuffer Last)> _itemPoses = [];
    ActorItem[] _shownItems = [];
    ActorGarment[] _shownGarments = [];
    Palette? _palette;

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
    /// <summary>The figure's group for the renderer: the actor itself, which picking gives back.</summary>
    public object Owner => this;
    public Palette? Palette { get => _palette; set => Set(ref _palette, value); }
    public List<ActorItem> Items { get; } = [];
    public List<ActorGarment> Garments { get; } = [];

    /// <summary>
    /// Evaluates the figure's and its items' poses at the animator's time: whether anything differs
    /// from the last evaluation (a channel's value, gear added or taken off).
    /// </summary>
    public bool Refresh()
    {
        Animator.Stack.Evaluate(Animator.Time, _pose);
        var changed = !Same(_pose, _lastPose);
        _lastPose.CopyFrom(_pose);
        if (!Items.SequenceEqual(_shownItems) || !Garments.SequenceEqual(_shownGarments))
        {
            changed = true;
            _shownItems = [.. Items];
            _shownGarments = [.. Garments];
            _itemPoses.Clear();
        }
        while (_itemPoses.Count < Items.Count)
        {
            var item = Items[_itemPoses.Count];
            var skeleton = item.Animation?.Skeleton ?? new Skeleton(item.Model.Data);
            _itemPoses.Add((new PoseBuffer(skeleton), new PoseBuffer(skeleton)));
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

    /// <summary>Adds the figure's and its gear's draw items for the last evaluated poses, placed by its node.</summary>
    internal override void Draw(List<DrawItem> items, SceneDrawing drawing)
    {
        var placement = Node?.World ?? Matrix4x4.Identity;
        var style = drawing.Style;
        var pose = _pose.ToPose();
        var data = Model.Data;
        Model.Draw(items, placement, _palette, style, pose: pose, owner: this);
        if (Items.Count == 0 && Garments.Count == 0)
            return;
        var world = data.WorldMatrices(pose);
        for (var i = 0; i < Items.Count; i++)
        {
            var item = Items[i];
            var itemPose = item.Animation != null ? _itemPoses[i].Now.ToPose() : null;
            item.Model.Draw(items, item.Socket.ItemTransform(world, placement), _palette, style, pose: itemPose, owner: this, anchor: placement.Translation);
        }
        var rest = data.WorldMatrices();
        foreach (var garment in Garments)
            garment.Model.Draw(items, placement, _palette, style, owner: this, world: garment.Garment.WorldMatrices(rest, world), anchor: placement.Translation);
    }

    static bool Same(PoseBuffer a, PoseBuffer b) =>
        a.Set.AsSpan().SequenceEqual(b.Set) && a.Translation.AsSpan().SequenceEqual(b.Translation)
        && a.Rotation.AsSpan().SequenceEqual(b.Rotation) && a.Scale.AsSpan().SequenceEqual(b.Scale);
}

/// <summary>An item on a socket of its wearer, with its own clips played with the wearer's states if <see cref="Animation"/> is given.</summary>
public sealed record ActorItem(Model Model, Socket Socket, ItemAnimation? Animation = null);

/// <summary>A garment on its wearer's bones.</summary>
public sealed record ActorGarment(Model Model, Garment Garment);
