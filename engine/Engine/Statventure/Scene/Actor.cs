using System.Numerics;
using Statventure.Animation;
using Statventure.Audio;
using Statventure.Render3D;

namespace Statventure.Scene;

/// <summary>
/// A figure in a scene (Docs/Animation.md, "Animated views"; Docs/Scene.md, Q1): a model posed by its
/// animator, placed by the node it hangs on (its model space is the node's, raised by its
/// <see cref="Ground"/>), coloured by <see cref="Palette"/>, with gear: items on its sockets (with their own clips, if
/// any) and garments on its bones. The figure and its gear are one model for the renderer (one outline, one glow mask;
/// the hatching anchored at its placement's origin); picking gives the actor back. Its bones aren't scene
/// nodes. The scene advances its animator by the frame's time times the scene's rate.
/// </summary>
public sealed class Actor : SceneAttachment
{
    readonly PoseBuffer _pose, _lastPose;
    readonly List<(PoseBuffer Now, PoseBuffer Last)> _itemPoses = [];
    ActorItem[] _shownItems = [];
    ActorGarment[] _shownGarments = [];
    Palette? _palette;
    float _ground;

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
    /// Where the figure's ground is in its model space (GM2-P22, "the figure's ground"; Docs/Animation.md): the height of
    /// its soles at rest (<see cref="GroundOf"/>; the human bodies' booted soles 24 mm under their origin). The figure is
    /// drawn raised by it (<see cref="Placement"/>), so the node it hangs on is where it stands: a node on a ground puts its
    /// soles on that ground, whatever the body's origin. 0 (the default): it stands by its model's origin.
    /// </summary>
    public float Ground { get => _ground; set => Set(ref _ground, value); }

    /// <summary>
    /// A figure's ground as its models say it: the lowest point at rest (<see cref="ModelData.LowestAtRest"/>) of its body
    /// and of the garments it wears (footwear among them; a bare sole, a claw or a hoof alike: whatever reaches lowest), its
    /// rigid items left out (a staff or a blade may reach lower than the feet without the figure standing on it); 0 when
    /// none has a mesh. To the micrometre (the vertices skinned at rest carry float noise far under it; Blender's side,
    /// Tools/Models/humanoid/render.py's figure_ground, rounds alike, so both raise a figure by the same number).
    /// </summary>
    public static float GroundOf(ModelData body, IEnumerable<ModelData> garments)
    {
        float? lowest = body.LowestAtRest();
        foreach (var garment in garments)
            if (garment.LowestAtRest() is { } y && (lowest is not { } low || y < low))
                lowest = y;
        return lowest is { } ground ? (float)(Math.Round(ground * 1e6) / 1e6) : 0;
    }

    /// <summary>
    /// What the figure is drawn by: its node's world (identity without a node), its model space first raised by
    /// <see cref="Ground"/>'s negation so its soles are on the node's origin. Its hatching is anchored at its origin.
    /// </summary>
    public Matrix4x4 Placement
    {
        get
        {
            var world = Node?.World ?? Matrix4x4.Identity;
            return _ground == 0 ? world : Matrix4x4.CreateTranslation(0, -_ground, 0) * world;
        }
    }

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
        var placement = Placement;
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
