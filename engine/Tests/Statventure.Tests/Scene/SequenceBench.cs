using System.Numerics;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Scene;
using Statventure.Tests.Animation;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Statventure.UI.Dom;

namespace Statventure.Tests.Scene;

/// <summary>
/// A scene to play sequences on, driven by a document's frames: the test figure "hero" (with the
/// humanoid state machine, Animation/StateMachineTests) on a node of its own, a bystander figure no
/// sequence binds, the node "pivot" (carrying a camera and a sun for the test sequence's cuts,
/// TestSequence.Camera.cs), the placed model "prop", the palette "tint", and the test sequence's stage
/// data (TestSequence.Core.cs) and effect. Nothing is rendered: the tests read the scene.
/// </summary>
sealed class SequenceBench : IDisposable
{
    public SequenceBench(GpuFixture gpu, bool reducedMotion = false, bool machine = true)
    {
        Figure = Model.Load(gpu.Device, Path.Combine(Render3DReferenceTests.ModelDirectory, "figure.glb"));
        View = new View3D(gpu.Device);
        Scene = new SceneView(View);
        Document = new UiDocument(TestFonts.Library);
        Document.SetViewport(300, 200, reducedMotion);
        Markup.SetInnerHtml(Document.Body, """<div id="card"></div>""");
        Document.AddFrameClient(Scene);
        Document.Update(0);
        Hero = NewActor(machine);
        HeroNode = Scene.Root.Add("hero");
        HeroNode.Attach(Hero);
        Bystander = NewActor(machine);
        Scene.Root.Add("bystander").Attach(Bystander);
        Pivot = Scene.Root.Add("pivot");
        PivotCamera = Pivot.Attach(new SceneCamera { VerticalFieldOfView = 0.7f });
        PivotSun = Pivot.Attach(new SceneSun { Strength = 2.5f, Angle = 4 });
        Prop = Scene.Root.Add("prop").Attach(new PlacedModel(Figure));
        Tint = new Palette().Set("cloth", Colors.LinearFromHex("#336699"));
        StageData = TestSequence.Stage();
    }

    public Model Figure { get; }
    public View3D View { get; }
    public SceneView Scene { get; }
    public UiDocument Document { get; }
    public Actor Hero { get; }
    public SceneNode HeroNode { get; }
    public Actor Bystander { get; }
    public SceneNode Pivot { get; }
    public SceneCamera PivotCamera { get; }
    public SceneSun PivotSun { get; }
    public PlacedModel Prop { get; }
    public Palette Tint { get; }
    public ModelData StageData { get; }
    /// <summary>The document's time: its last frame's.</summary>
    public double Now { get; private set; }

    /// <summary>A figure playing its machine (or none) from time 0.</summary>
    public Actor NewActor(bool machine = true)
    {
        var skeleton = new Skeleton(Figure.Data);
        var stack = new AnimationStack(skeleton);
        var clips = AnimationClip.AllOf(Figure.Data);
        var states = machine ? new StateMachine(StateMachineTests.Humanoid(), stack, name => new ClipBinding(clips.Single(c => c.Name == name), skeleton)) : null;
        return new Actor(Figure, new Animator(stack, states));
    }

    /// <summary>A frame <paramref name="seconds"/> after the last.</summary>
    public void Frame(double seconds)
    {
        Now += seconds;
        Document.Update(Now);
    }

    /// <summary>
    /// Frames of <paramref name="step"/> until the player's τ reaches <paramref name="tau"/>, failing after
    /// 100 frames rather than looping for ever (a player that stops advancing fails the test).
    /// </summary>
    public void FrameUntil(SequencePlayer player, double tau, double step = 0.125)
    {
        for (var frames = 0; player.TimelineTime < tau; frames++)
        {
            Assert.True(frames < 100, $"τ didn't reach {tau:R} in 100 frames (it is {player.TimelineTime:R})");
            Frame(step);
        }
    }

    /// <summary>The test sequence's roles and stage bound.</summary>
    public SequenceBindings Bindings(ModelData? stage = null) =>
        new SequenceBindings().Actor("hero", Hero).Node("pivot", Pivot).Model("prop", Prop).Palette("tint", Tint).Stage(TestSequence.StagePath, stage ?? StageData)
            .Effect(TestSequence.EffectPath, TestSequence.Effect);

    public SequencePlayer Play(Sequence? sequence = null) => Scene.Play(sequence ?? TestSequence.Build(StageData), Bindings(), Document);

    /// <summary>The instanced stage's node of this name.</summary>
    public static SceneNode StageNode(SequencePlayer player, string name) => player.Stages[0].NodeNamed(name)!;

    /// <summary>A node's local transform, to compare.</summary>
    public static (Vector3, Quaternion, Vector3) Local(SceneNode node) => (node.Translation, node.Rotation, node.Scale);

    public void Dispose()
    {
        View.Dispose();
        Figure.Dispose();
    }
}
