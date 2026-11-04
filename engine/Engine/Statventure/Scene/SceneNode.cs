using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Scene;

/// <summary>
/// A node of a scene's tree (Docs/Scene.md, "The node tree and the scene"): a name, a parent and
/// children, a local translation, rotation (a unit quaternion) and scale, and a world matrix, the
/// local transform after the parent's world (glTF's node rule, world = parent's world · T·R·S, which
/// is how Blender's object parenting comes back from its exporter: parent inverses, bone parents and
/// axis conversions are baked into each child's local transform). System.Numerics' row vectors:
/// <see cref="Local"/> is S·R·T and <see cref="World"/> is Local × the parent's World, so a sheared
/// world (non-uniform scale above a turned child) composes as the matrices do. The world matrix is
/// worked out when asked for, and again only after a local transform above it (or its own) changed
/// or it moved to another parent. Attachments (figures, models, terrain, a camera, a sun) are placed
/// by the node's world matrix; a hidden node hides its subtree. Paths name nodes from a node
/// (<c>stage/rift</c>).
/// </summary>
public sealed class SceneNode
{
    readonly List<SceneNode> _children = [];
    readonly List<SceneAttachment> _attachments = [];
    Vector3 _translation;
    Quaternion _rotation = Quaternion.Identity;
    Vector3 _scale = Vector3.One;
    Matrix4x4 _world;
    bool _hidden;
    // Invariant: a node whose world is stale has only descendants whose worlds are stale too.
    bool _stale = true;

    public SceneNode(string name = "") => Name = name;

    public string Name { get; set; }
    public SceneNode? Parent { get; private set; }
    public IReadOnlyList<SceneNode> Children => _children;
    /// <summary>What hangs on the node, in the order attached.</summary>
    public IReadOnlyList<SceneAttachment> Attachments => _attachments;

    /// <summary>The scene whose root this node is under (or is), or null.</summary>
    public SceneView? Scene => Root.Owner;

    /// <summary>The top of the node's tree: itself when it has no parent.</summary>
    public SceneNode Root
    {
        get
        {
            var node = this;
            while (node.Parent != null)
                node = node.Parent;
            return node;
        }
    }

    /// <summary>
    /// Whether the node is hidden, and with it its subtree (Docs/Scene.md, Q23): nothing hanging on it
    /// or below it is drawn (so nothing there is picked). Figures there go on animating and their
    /// events come; a camera or sun there still places the view.
    /// </summary>
    public bool Hidden
    {
        get => _hidden;
        set
        {
            if (value == _hidden)
                return;
            _hidden = value;
            Scene?.Changed(structure: false);
        }
    }

    /// <summary>Whether the node is shown: neither it nor a node above it is hidden.</summary>
    public bool Shown
    {
        get
        {
            for (var node = this; node != null; node = node.Parent)
                if (node._hidden)
                    return false;
            return true;
        }
    }

    /// <summary>The scene a root node belongs to.</summary>
    internal SceneView? Owner { get; set; }

    /// <summary>Tests: how many times this node's world matrix was worked out.</summary>
    internal int WorldComputations { get; private set; }

    public Vector3 Translation
    {
        get => _translation;
        set
        {
            if (value == _translation)
                return;
            _translation = value;
            Moved();
        }
    }

    public Quaternion Rotation
    {
        get => _rotation;
        set
        {
            if (value == _rotation)
                return;
            _rotation = value;
            Moved();
        }
    }

    public Vector3 Scale
    {
        get => _scale;
        set
        {
            if (value == _scale)
                return;
            _scale = value;
            Moved();
        }
    }

    /// <summary>Sets translation, rotation and scale at once (one change).</summary>
    public void SetLocal(Vector3 translation, Quaternion rotation, Vector3 scale)
    {
        if (translation == _translation && rotation == _rotation && scale == _scale)
            return;
        (_translation, _rotation, _scale) = (translation, rotation, scale);
        Moved();
    }

    /// <summary>The local transform: scale, then rotation, then translation (glTF's T·R·S for column vectors).</summary>
    public Matrix4x4 Local => Matrix4x4.CreateScale(_scale) * Matrix4x4.CreateFromQuaternion(_rotation) * Matrix4x4.CreateTranslation(_translation);

    /// <summary>Node to world: <see cref="Local"/> after the parent's world (the parent's world is the tree's root space for a root).</summary>
    public Matrix4x4 World
    {
        get
        {
            if (_stale)
            {
                _world = Parent == null ? Local : Local * Parent.World;
                _stale = false;
                WorldComputations++;
            }
            return _world;
        }
    }

    /// <summary>The names from the root down to this node, joined by <c>/</c> (the root's is empty).</summary>
    public string Path
    {
        get
        {
            var names = new List<string>();
            for (var node = this; node.Parent != null; node = node.Parent)
                names.Add(node.Name);
            names.Reverse();
            return string.Join('/', names);
        }
    }

    /// <summary>A new child of this name, added last.</summary>
    public SceneNode Add(string name) => Add(new SceneNode(name));

    /// <summary>
    /// Makes <paramref name="child"/> this node's last child, taking it from its parent (it keeps its
    /// local transform, so it now moves with this node). A node can't become its own descendant, and
    /// a scene's root can't become a child.
    /// </summary>
    public SceneNode Add(SceneNode child)
    {
        for (var node = this; node != null; node = node.Parent)
            if (node == child)
                throw new ArgumentException($"{child.Name} can't be a child of itself or of its descendant {Name}");
        if (child.Owner != null)
            throw new ArgumentException($"{child.Name} is a scene's root");
        child.Remove();
        child.Parent = this;
        _children.Add(child);
        child.Moved();
        Scene?.Changed(structure: true);
        return child;
    }

    /// <summary>Takes the node (with its subtree) out of its parent; its world is then its own tree's.</summary>
    public void Remove()
    {
        if (Parent is not { } parent)
            return;
        var scene = parent.Scene;
        parent._children.Remove(this);
        Parent = null;
        MarkStale();
        scene?.Changed(structure: true);
    }

    /// <summary>The first child of this name, or null.</summary>
    public SceneNode? Child(string name) => _children.Find(c => c.Name == name);

    /// <summary>
    /// The node at <paramref name="path"/> below this one: names joined by <c>/</c>, each the first
    /// child of that name of the node before (an empty path is this node), or null.
    /// </summary>
    public SceneNode? Find(string path)
    {
        if (path.Length == 0)
            return this;
        var node = this;
        foreach (var name in path.Split('/'))
            if ((node = node.Child(name)) == null)
                return null;
        return node;
    }

    /// <summary>The first node of this name below this one (depth first, children in order), or null.</summary>
    public SceneNode? Descendant(string name) => Descendants().FirstOrDefault(n => n.Name == name);

    /// <summary>Every node below this one, depth first (a node before its children, children in order).</summary>
    public IEnumerable<SceneNode> Descendants()
    {
        var stack = new Stack<SceneNode>();
        for (var i = _children.Count - 1; i >= 0; i--)
            stack.Push(_children[i]);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            for (var i = node._children.Count - 1; i >= 0; i--)
                stack.Push(node._children[i]);
        }
    }

    /// <summary>Hangs <paramref name="attachment"/> on this node (taking it from the node it was on).</summary>
    public T Attach<T>(T attachment) where T : SceneAttachment
    {
        attachment.Node?.Detach(attachment);
        attachment.Node = this;
        _attachments.Add(attachment);
        Scene?.Changed(structure: true);
        return attachment;
    }

    /// <summary>Takes <paramref name="attachment"/> off this node.</summary>
    public void Detach(SceneAttachment attachment)
    {
        if (attachment.Node != this)
            return;
        _attachments.Remove(attachment);
        attachment.Node = null;
        Scene?.Changed(structure: true);
    }

    /// <summary>
    /// Instances <paramref name="model"/> under this node (Docs/Scene.md, Q2): a scene node for each of
    /// the model's nodes the scene reaches (names, hierarchy and local transforms kept, the model's
    /// roots as this node's children), its meshes drawn from those nodes, each top-level object (a
    /// root with its descendants) a model of its own for the ink's outline, the glow's mask, the
    /// hatching's anchor and picking, as Blender renders a stage with a Freestyle contour per object.
    /// </summary>
    public ModelInstance Instance(Model model, Palette? palette = null, bool scenery = false) =>
        Attach(new ModelInstance(this, model.Data, model) { Palette = palette, Scenery = scenery });

    /// <summary>A model's hierarchy as scene nodes under this node, drawing nothing (its empties, marks and joints).</summary>
    public ModelInstance Instance(ModelData data) => Attach(new ModelInstance(this, data, null));

    /// <summary>Its world and those below it are to be worked out again; the scene draws again.</summary>
    void Moved()
    {
        MarkStale();
        Scene?.Changed(structure: false);
    }

    void MarkStale()
    {
        if (_stale)
            return;
        _stale = true;
        var stack = new Stack<SceneNode>(_children);
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (node._stale)
                continue;
            node._stale = true;
            foreach (var child in node._children)
                stack.Push(child);
        }
    }

    public override string ToString() => Parent == null ? $"(root {Name})" : Path;
}
