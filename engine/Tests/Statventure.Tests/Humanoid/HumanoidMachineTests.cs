using System.Numerics;
using Statventure.Animation;
using Statventure.Core;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// The figures' small state machine (GM2 step 4b; Docs/Game/Hero.md, "The humanoid rig family": "GM5 builds the battle's
/// state machine, GM2 a small one for its figures"): models.pak's models/humanoid/machines/figure, from its JSON form in
/// Assets/Models/humanoid/machines/figure.json (Docs/Animation.md's form, Docs/Assets.md's record). The sheet's, the
/// maker's and the carousel's figure stands relaxed (the user's Q57; Q31 and Q41: one fixed view, nothing else), so the
/// machine is one layer with one state looping the clip `stand`. Step 11's live figures play it.
/// </summary>
public class HumanoidMachineTests
{
    const string Path_ = "models/humanoid/machines/figure";

    /// <summary>The game's packs mounted as the game mounts them (packs/ beside it).</summary>
    static VirtualFileSystem GameData(TempFolder folder)
    {
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        return new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], _ => null));
    }

    /// <summary>
    /// The machine loads from models.pak (checked as every machine is, M7's rules): one layer, replacing, entering `stand`,
    /// its one state looping the library's `stand` at its own speed; no parameters, no transitions. Its file is the record's
    /// own JSON form (the serializer writes it back byte for byte), and it names only clips the family's library has.
    /// </summary>
    [Fact]
    public void TheFiguresMachineStandsRelaxed()
    {
        using var folder = new TempFolder();
        using var data = GameData(folder);
        Assert.Equal(folder["game/packs/models.pak"], data.Origin(Path_));
        var machine = StateMachineDefinition.Load(data, Path_);
        Assert.Equal("figure", machine.Name);
        Assert.Empty(machine.Parameters);
        var layer = Assert.Single(machine.Layers);
        Assert.Equal(("base", BlendMode.Replace, "stand"), (layer.Name, layer.Mode, layer.Entry));
        Assert.Equal([new StateDefinition("stand", "stand", 1, null, Loop: true)], layer.States);
        Assert.Empty(layer.Transitions);
        var file = Path.Combine(HumanoidFiles.Machines, "figure.json");
        Assert.Equal(File.ReadAllText(file).TrimEnd('\n'), RecordSerializer.ToJson(machine).TrimEnd('\n'));
        var clips = HumanoidClipList.Clips().Select(c => c.Name).ToHashSet();
        foreach (var name in HumanoidFiles.MachineNames())
        {
            var each = StateMachineDefinition.Load(data, "models/humanoid/machines/" + name);
            Assert.All(each.Layers.SelectMany(l => l.States).Where(s => s.Clip != null), s => Assert.True(clips.Contains(s.Clip!), $"{name}: {s.Name} plays {s.Clip}, which the library hasn't"));
        }
    }

    /// <summary>
    /// The machine plays on both bodies: an animator with it over the library's clips (bound by bone names to the body, as
    /// step 11's figures will be) poses the body at every time as `stand` poses it at that time in its round (4 s), past
    /// the first round too; the clock run in uneven steps as frames come.
    /// </summary>
    [Theory]
    [InlineData("male")]
    [InlineData("female")]
    public void TheMachinePlaysOnBothBodies(string sex)
    {
        using var folder = new TempFolder();
        using var data = GameData(folder);
        var machine = StateMachineDefinition.Load(data, Path_);
        var body = ModelData.Load(data, $"models/humanoid/bodies/human-{sex}-average");
        var library = AnimationClip.AllOf(data, "models/humanoid/clips");
        var skeleton = new Skeleton(body);
        var stack = new AnimationStack(skeleton);
        var animator = new Animator(stack, new StateMachine(machine, stack, name => new ClipBinding(library.Single(c => c.Name == name), skeleton)));
        var stand = new ClipBinding(library.Single(c => c.Name == "stand"), skeleton);
        Assert.Equal(4.0, stand.Clip.Duration, 6);
        Assert.Equal("stand", animator.Machine!.StateOf("base"));
        var steps = new[] { 0.0, 1 / 60.0, 0.3, 1.25, 2.0, 0.7, 1 / 30.0, 0.95, 1.6 };
        foreach (var step in steps)
        {
            animator.Advance(step);
            var round = animator.Time % stand.Clip.Duration;
            var (actual, expected) = (body.WorldMatrices(animator.Pose()), body.WorldMatrices(stand.Pose(round)));
            for (var n = 0; n < actual.Length; n++)
                Assert.True(Close(actual[n], expected[n]), $"{sex}: {body.Nodes[n].Name} at {animator.Time:F4} s isn't stand's at {round:F4} s");
        }
        Assert.True(animator.Time > stand.Clip.Duration, "the clock never passed the first round");
        // It moves: the pose at two times of the round differs.
        var (a, b) = (body.WorldMatrices(stand.Pose(0)), body.WorldMatrices(stand.Pose(1)));
        Assert.Contains(Enumerable.Range(0, a.Length), n => Vector3.Distance(a[n].Translation, b[n].Translation) > 0.002f);
    }

    static bool Close(Matrix4x4 a, Matrix4x4 b)
    {
        for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
                if (Math.Abs(a[r, c] - b[r, c]) > 2e-6f)
                    return false;
        return true;
    }
}
