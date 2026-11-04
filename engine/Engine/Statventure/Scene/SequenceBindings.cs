using Statventure.Animation;
using Statventure.Render3D;
using Statventure.UI.Dom;

namespace Statventure.Scene;

/// <summary>
/// What the game gives a sequence when it plays it (Q15): an object for each role (a figure, a node,
/// a model, a UI host, a palette) and each stage's model by its path. <see cref="SceneView.Play"/>
/// checks them all before it changes anything (S4-6): every role bound once, to something of its
/// kind, no binding the sequence has no role or stage for, every stage given.
/// </summary>
public sealed class SequenceBindings
{
    internal List<(string Role, object Value, Func<string, ClipBinding?>? Clips)> Roles { get; } = [];
    internal List<(string Path, ModelData Data, Model? Model)> Stages { get; } = [];

    /// <summary>
    /// A figure for an actor role: its clock follows the sequence while it plays. Its clips by name come
    /// from <paramref name="clips"/> (null for none of that name), by default its model's own clips bound
    /// to its stack's skeleton.
    /// </summary>
    public SequenceBindings Actor(string role, Actor actor, Func<string, ClipBinding?>? clips = null) => Add(role, actor, clips);

    /// <summary>A scene node for a node role.</summary>
    public SequenceBindings Node(string role, SceneNode node) => Add(role, node);

    /// <summary>A placed model for a model role (palette cues).</summary>
    public SequenceBindings Model(string role, PlacedModel model) => Add(role, model);

    /// <summary>An instanced model for a model role (palette cues).</summary>
    public SequenceBindings Model(string role, ModelInstance model) => Add(role, model);

    /// <summary>A UI host element for an element role: the UI track's elements are found by id within it.</summary>
    public SequenceBindings Element(string role, UiElement host) => Add(role, host);

    /// <summary>A palette for a palette role (palette and particle cues).</summary>
    public SequenceBindings Palette(string role, Palette palette) => Add(role, palette);

    /// <summary>A stage's model (its path in the sequence, <c>models/stages/&lt;name&gt;</c>), drawn as scenery.</summary>
    public SequenceBindings Stage(string path, Model model)
    {
        ArgumentNullException.ThrowIfNull(model);
        Stages.Add((path, model.Data, model));
        return this;
    }

    /// <summary>A stage's model data only: its nodes are made, nothing is drawn (its marks and empties, tests).</summary>
    public SequenceBindings Stage(string path, ModelData data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Stages.Add((path, data, null));
        return this;
    }

    SequenceBindings Add(string role, object value, Func<string, ClipBinding?>? clips = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        Roles.Add((role, value, clips));
        return this;
    }
}
