using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Render3D;

namespace Statventure.Tests.Scene;

/// <summary>
/// The node tree (Docs/Scene.md, "The node tree and the scene"): local transforms, world matrices as
/// glTF composes them, worked out again only when something above changed, paths, instancing a
/// model's hierarchy and its clips on the instanced nodes. (What Blender's parenting gives back is
/// checked against Blender in <see cref="SceneReferenceTests"/>.)
/// </summary>
public class SceneNodeTests
{
    internal static ModelData LoadModel(string file) => ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, file));

    static Quaternion Turn(Vector3 axis, float degrees) => Quaternion.CreateFromAxisAngle(Vector3.Normalize(axis), degrees * MathF.PI / 180);

    static void Near(Matrix4x4 expected, Matrix4x4 actual, float tolerance = 1e-5f)
    {
        for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
                Assert.True(MathF.Abs(expected[r, c] - actual[r, c]) <= tolerance, $"[{r},{c}]: {actual[r, c]} instead of {expected[r, c]}\n{actual}\n{expected}");
    }

    static void Near(Vector3 expected, Vector3 actual, float tolerance = 1e-5f) =>
        Assert.True(Vector3.Distance(expected, actual) <= tolerance, $"{actual} instead of {expected}");

    [Fact]
    public void ALocalTransformScalesThenTurnsThenMoves()
    {
        var node = new SceneNode("a") { Translation = new(1, 2, 3), Rotation = Turn(Vector3.UnitY, 90), Scale = new(2, 1, 1) };
        // (1, 0, 0) scaled to (2, 0, 0), turned a quarter about Y to (0, 0, −2), moved by (1, 2, 3).
        Near(new Vector3(1, 2, 1), Vector3.Transform(Vector3.UnitX, node.Local));
        Near(new Vector3(1, 3, 3), Vector3.Transform(Vector3.UnitY, node.Local));
        Assert.Equal(node.Local, node.World);
    }

    [Fact]
    public void AWorldMatrixIsTheLocalAfterTheParentsWorld()
    {
        // Non-uniform scale above a turned child: the child's world is sheared (its axes' images aren't
        // perpendicular), which no translation, rotation and scale of the child alone would give.
        var root = new SceneNode();
        var squash = root.Add("squash");
        squash.SetLocal(new(1, 0, 0), Turn(Vector3.UnitZ, 20), new(2, 0.5f, 1));
        var tilted = squash.Add("tilted");
        tilted.SetLocal(new(0, 1, 0), Turn(Vector3.UnitZ, 45), Vector3.One);
        var tip = tilted.Add("tip");
        tip.Translation = new(0.5f, 0, 0.25f);
        Near(tip.Local * tilted.Local * squash.Local, tip.World);
        var x = Vector3.TransformNormal(Vector3.UnitX, tilted.World);
        var y = Vector3.TransformNormal(Vector3.UnitY, tilted.World);
        Assert.True(MathF.Abs(Vector3.Dot(x, y)) > 0.5f, "the child's world should be sheared");
        // A point of the tip: its own translation, then the tilted node's turn and place, then the squash.
        var inTilted = Vector3.Transform(new Vector3(0.5f, 0, 0.25f), Matrix4x4.CreateFromQuaternion(Turn(Vector3.UnitZ, 45))) + new Vector3(0, 1, 0);
        var expected = Vector3.Transform(inTilted * new Vector3(2, 0.5f, 1), Matrix4x4.CreateFromQuaternion(Turn(Vector3.UnitZ, 20))) + new Vector3(1, 0, 0);
        Near(expected, tip.World.Translation);
    }

    [Fact]
    public void WorldMatricesFollowWhatChangesAboveThem()
    {
        var root = new SceneNode();
        var a = root.Add("a");
        var b = a.Add("b");
        var c = b.Add("c");
        c.Translation = new(0, 1, 0);
        Near(new Vector3(0, 1, 0), c.World.Translation);
        a.Translation = new(5, 0, 0);
        Near(new Vector3(5, 1, 0), c.World.Translation);
        a.Rotation = Turn(Vector3.UnitZ, 90);
        Near(new Vector3(4, 0, 0), c.World.Translation);
        a.Scale = new(3, 3, 3);
        Near(new Vector3(2, 0, 0), c.World.Translation);
        b.Translation = new(0, 0, 1);
        Near(new Vector3(2, 0, 3), c.World.Translation);
        // Moved to another parent, the node keeps its local transform and follows the new parent.
        var other = root.Add("other");
        other.Translation = new(0, 0, -10);
        other.Add(b);
        Assert.Same(other, b.Parent);
        Assert.Empty(a.Children);
        Near(new Vector3(0, 1, -9), c.World.Translation);
        // Taken out, its world is its own tree's.
        b.Remove();
        Assert.Null(b.Parent);
        Near(new Vector3(0, 1, 1), c.World.Translation);
        b.SetLocal(new(1, 1, 1), Quaternion.Identity, new(2, 2, 2));
        Near(new Vector3(1, 3, 1), c.World.Translation);
        // Its world read while it had no parent, then added: it follows its new parent.
        _ = b.World;
        a.Add(b);
        Near(Vector3.Transform(new Vector3(1, 3, 1), a.World), c.World.Translation);
        Near(b.Local * a.World, b.World);
    }

    [Fact]
    public void WorldMatricesAreWorkedOutOnlyWhenSomethingAboveChanged()
    {
        var root = new SceneNode();
        var a = root.Add("a");
        var b = a.Add("b");
        var c = b.Add("c");
        var d = a.Add("d");
        _ = c.World;
        _ = c.World;
        Assert.Equal((1, 1, 1), (a.WorldComputations, b.WorldComputations, c.WorldComputations));
        // A sibling's change reaches neither its sibling's subtree nor its parent.
        d.Translation = new(1, 0, 0);
        _ = c.World;
        Assert.Equal((1, 1, 1), (a.WorldComputations, b.WorldComputations, c.WorldComputations));
        // The same value again is no change.
        b.Translation = b.Translation;
        b.Rotation = b.Rotation;
        b.Scale = b.Scale;
        b.SetLocal(b.Translation, b.Rotation, b.Scale);
        _ = c.World;
        Assert.Equal((1, 1, 1), (a.WorldComputations, b.WorldComputations, c.WorldComputations));
        // A change works out the node and those below it again, not those above.
        b.Translation = new(0, 2, 0);
        _ = c.World;
        Assert.Equal((1, 2, 2), (a.WorldComputations, b.WorldComputations, c.WorldComputations));
        // Changes in a row make one computation when next asked for.
        a.Translation = new(1, 0, 0);
        b.Rotation = Turn(Vector3.UnitX, 10);
        c.Scale = new(2, 2, 2);
        _ = c.World;
        Assert.Equal((2, 3, 3), (a.WorldComputations, b.WorldComputations, c.WorldComputations));
        Assert.Equal(0, d.WorldComputations);
    }

    [Fact]
    public void PathsNameNodesBelowANode()
    {
        var root = new SceneNode("scene");
        var stage = root.Add("stage");
        var rift = stage.Add("rift");
        var glow = rift.Add("glow");
        var mark = stage.Add("mark.boss");
        var second = stage.Add("rift");
        Assert.Equal("", root.Path);
        Assert.Equal("stage/rift/glow", glow.Path);
        Assert.Same(glow, root.Find("stage/rift/glow"));
        Assert.Same(rift, stage.Find("rift"));
        Assert.Same(stage, stage.Find(""));
        Assert.Null(root.Find("stage/none"));
        Assert.Null(root.Find("rift"));
        Assert.Same(mark, root.Find("stage/mark.boss"));
        Assert.Same(glow, root.Descendant("glow"));
        Assert.Null(stage.Descendant("stage"));
        Assert.Equal([stage, rift, glow, mark, second], root.Descendants());
        Assert.Same(root, glow.Root);
    }

    [Fact]
    public void ANodeCantBeItsOwnAncestor()
    {
        var root = new SceneNode();
        var a = root.Add("a");
        var b = a.Add("b");
        Assert.Throws<ArgumentException>(() => a.Add(a));
        Assert.Throws<ArgumentException>(() => b.Add(a));
        Assert.Throws<ArgumentException>(() => b.Add(root));
        Assert.Same(a, b.Parent);
        Assert.Same(root, a.Parent);
    }

    [Fact]
    public void InstancingMakesANodeForEachOfTheModelsNodes()
    {
        foreach (var file in new[] { "blocks.glb", "hierarchy.glb", "figure.glb" })
        {
            var data = LoadModel(file);
            var root = new SceneNode();
            var place = root.Add("place");
            place.SetLocal(new(0.5f, -1, 2), Turn(new(1, 2, 0.5f), 35), new(1.2f, 0.8f, 1));
            var instance = place.Instance(data);
            var rest = data.WorldMatrices();
            Assert.Equal(data.Roots.Select(r => data.Nodes[r].Name), instance.Roots.Select(n => n.Name));
            Assert.Equal(instance.Roots, place.Children);
            var reached = 0;
            for (var i = 0; i < data.Nodes.Count; i++)
            {
                if (instance.NodeOf(i) is not { } node)
                    continue;
                reached++;
                var source = data.Nodes[i];
                Assert.Equal(source.Name, node.Name);
                Assert.Same(source.Parent >= 0 ? instance.NodeOf(source.Parent) : place, node.Parent);
                Assert.Equal(source.Children.Select(c => data.Nodes[c].Name), node.Children.Select(c => c.Name));
                Assert.Equal((source.Translation, source.Rotation, source.Scale), (node.Translation, node.Rotation, node.Scale));
                Near(rest[i] * place.World, node.World);
            }
            Assert.Equal(place.Descendants().Count(), reached);
            Assert.True(reached > 3, file);
        }
    }

    [Fact]
    public void ANodeGivenAsAMatrixIsDecomposed()
    {
        var turn = Turn(new(0, 1, 1), 50);
        var matrix = Matrix4x4.CreateScale(2, 1, 0.5f) * Matrix4x4.CreateFromQuaternion(turn) * Matrix4x4.CreateTranslation(1, 2, 3);
        var data = new ModelData
        {
            Nodes = [new ModelNode("m", -1, [1], matrix, Vector3.Zero, Quaternion.Identity, Vector3.One, -1, -1),
                     new ModelNode("child", 0, [], Matrix4x4.CreateTranslation(0, 1, 0), new(0, 1, 0), Quaternion.Identity, Vector3.One, -1, -1)],
            Meshes = [], Materials = [], Skins = [], Roots = [0],
        };
        var root = new SceneNode();
        var instance = root.Instance(data);
        var node = instance.NodeOf(0)!;
        Near(new Vector3(1, 2, 3), node.Translation);
        Near(new Vector3(2, 1, 0.5f), node.Scale);
        Assert.True(MathF.Abs(Quaternion.Dot(turn, node.Rotation)) > 0.99999f);
        Near(matrix, node.World);
        Near(Matrix4x4.CreateTranslation(0, 1, 0) * matrix, instance.NodeOf(1)!.World);
    }

    /// <summary>
    /// A clip of the model played on its instanced nodes, bound by name through the instance's
    /// skeleton: a bone's channels on its rest (the figure's swing), so every node's world is the
    /// model's at that pose; nodes the clip doesn't key keep their own transform.
    /// </summary>
    [Fact]
    public void AClipOfTheModelMovesItsInstancedNodes()
    {
        var data = LoadModel("figure.glb");
        var root = new SceneNode();
        var instance = root.Instance(data);
        var swing = instance.Clip("swing");
        foreach (var time in new[] { 0.0, 0.1, 13.5 / 60, 0.5 })
        {
            instance.Apply(swing, time);
            var posed = data.WorldMatrices(swing.Pose(time));
            for (var i = 0; i < data.Nodes.Count; i++)
                if (instance.NodeOf(i) is { } node)
                    Near(posed[i], node.World, 2e-5f);
        }
        var head = instance.NodeNamed("head")!;
        var source = data.Nodes[new Skeleton(data).Find("head")];
        Assert.Equal((source.Translation, source.Rotation, source.Scale), (head.Translation, head.Rotation, head.Scale));
        // A clip bound to another model's nodes is refused.
        Assert.Throws<ArgumentException>(() => instance.Apply(new ClipBinding(swing.Clip, new Skeleton(LoadModel("figure.glb"))), 0));
    }

    /// <summary>
    /// A stack on an instance: when no strip sets a node any more (a strip that extrapolates nothing,
    /// past its end) the node goes back to its rest, as a figure's unset channels are rest.
    /// </summary>
    [Fact]
    public void ANodeNoStripSetsGoesBackToItsRest()
    {
        var data = LoadModel("hierarchy.glb");
        var root = new SceneNode();
        var instance = root.Instance(data);
        var stack = new AnimationStack(instance.Skeleton);
        stack.Add(instance.Clip("base"), 0, s => s.Extrapolation = Extrapolation.Nothing);
        var baseNode = instance.NodeNamed("Base")!;
        var rest = (baseNode.Translation, baseNode.Rotation, baseNode.Scale);
        instance.Apply(stack, 0.5);
        Assert.NotEqual(rest, (baseNode.Translation, baseNode.Rotation, baseNode.Scale));
        var post = instance.NodeNamed("Post")!;
        var postRest = (post.Translation, post.Rotation, post.Scale);
        instance.Apply(stack, 2);
        Assert.Equal(rest, (baseNode.Translation, baseNode.Rotation, baseNode.Scale));
        // A node no strip ever set is left as it is.
        post.Translation += new Vector3(0, 1, 0);
        instance.Apply(stack, 0.5);
        Assert.Equal(postRest.Translation + new Vector3(0, 1, 0), post.Translation);
        // A clip of other nodes applied next: the node the stack set goes back to its rest.
        instance.Apply(instance.Clip("rig"), 0.5);
        Assert.Equal(rest, (baseNode.Translation, baseNode.Rotation, baseNode.Scale));
        Assert.NotEqual(data.Nodes[instance.Skeleton.Find("arm")].Rotation, instance.NodeNamed("arm")!.Rotation);
    }
}
