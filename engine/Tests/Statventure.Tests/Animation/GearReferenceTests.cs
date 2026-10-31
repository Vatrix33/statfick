using System.Numerics;
using System.Text.Json;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Xunit.Abstractions;

namespace Statventure.Tests.Animation;

/// <summary>
/// Gear follows its wearer as in Blender (Golden/anim-gear-*.json, from evaluate_animations.py): items
/// parented to socket bones (Blender places them by the bone, the exporter converts their axes but
/// not the bone's: <see cref="Socket"/>) and a garment skinned to the wearer by bone names (Blender's
/// Armature modifier on the wearer: <see cref="Garment"/>), on the figure it was made on and on the
/// stocky figure. Every item root's origin and axes and every garment vertex within the on-frame
/// tolerance.
/// </summary>
public class GearReferenceTests(ITestOutputHelper output)
{
    static string Golden(string name) => Path.Combine(Path.GetDirectoryName(Infrastructure.Golden.GoldenPath("x"))!, "anim-" + name + ".json");
    static ModelData Model(string name) => ModelData.Load(Path.Combine(Render3DReferenceTests.ModelDirectory, name + ".glb"));

    public static TheoryData<string> GearScenes => new(AnimationScenes.All.Where(s => s.Items != null || s.Clothing != null).Select(s => s.Name));

    [Theory]
    [MemberData(nameof(GearScenes))]
    public void GearFollowsItsWearerAsInBlender(string name)
    {
        var scene = AnimationScenes.All.Single(s => s.Name == name);
        using var document = JsonDocument.Parse(File.ReadAllBytes(Golden(name)));
        var root = document.RootElement;
        var source = Model(scene.Model);
        var wearer = scene.Target == null ? source : Model(scene.Target);
        var skeleton = new Skeleton(wearer);
        var binding = new ClipBinding(AnimationClip.AllOf(source).Single(c => c.Name == scene.Clip), skeleton);
        var rest = wearer.WorldMatrices();
        double worstItem = 0, worstCloth = 0;
        var items = scene.Items!.Select(i => (Model: Model(i.Model), i.Model, Socket: new Socket(skeleton, i.Socket))).ToArray();
        var garments = (scene.Clothing ?? []).Select(c => (Name: c, Garment: new Garment(Model(c), skeleton))).ToArray();
        // Each engine vertex of a garment matched to Blender's vertex at its rest position.
        var matches = garments.ToDictionary(g => g.Name, g => Match(g.Garment, root.GetProperty("clothing").GetProperty(g.Name).GetProperty("rest")));
        for (var t = 0; t < scene.Times.Length; t++)
        {
            var posed = wearer.WorldMatrices(binding.Pose(scene.Times[t]));
            foreach (var (model, itemName, socket) in items)
            {
                var transform = socket.ItemTransform(posed, Matrix4x4.Identity);
                var itemRest = model.WorldMatrices();
                foreach (var entry in root.GetProperty("items").EnumerateObject().Where(e => e.Name.StartsWith(itemName + "/")))
                {
                    var node = model.Nodes.Select((n, i) => (n, i)).Single(x => x.n.Name == entry.Name[(itemName.Length + 1)..] && x.n.Parent < 0).i;
                    var engine = itemRest[node] * transform;
                    var blender = entry.Value[t].EnumerateArray().Select(v => v.GetSingle()).ToArray();
                    foreach (var point in new[] { Vector3.Zero, Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ })
                    {
                        var expected = new Vector3(
                            blender[0] * point.X + blender[1] * point.Y + blender[2] * point.Z + blender[3],
                            blender[4] * point.X + blender[5] * point.Y + blender[6] * point.Z + blender[7],
                            blender[8] * point.X + blender[9] * point.Y + blender[10] * point.Z + blender[11]);
                        worstItem = Math.Max(worstItem, Vector3.Distance(Vector3.Transform(point, engine), expected));
                    }
                }
            }
            foreach (var (garmentName, garment) in garments)
            {
                var world = garment.WorldMatrices(rest, posed);
                var blender = root.GetProperty("clothing").GetProperty(garmentName).GetProperty("posed")[t];
                foreach (var (position, index) in Skinned(garment.Clothing, world).Zip(matches[garmentName]))
                {
                    var b = blender[index];
                    worstCloth = Math.Max(worstCloth, Vector3.Distance(position, new Vector3(b[0].GetSingle(), b[1].GetSingle(), b[2].GetSingle())));
                }
            }
        }
        output.WriteLine($"{name}: items within {worstItem * 1000:F5} mm, garment vertices within {worstCloth * 1000:F5} mm");
        Assert.True(worstItem <= AnimationReferenceTests.OnFrameTolerance, $"{name}: an item is {worstItem * 1000:F4} mm off");
        Assert.True(worstCloth <= AnimationReferenceTests.OnFrameTolerance, $"{name}: a garment vertex is {worstCloth * 1000:F4} mm off");
    }

    /// <summary>A garment's skinned vertices (CPU, as skin.comp blends: weights times inverse bind, then joint world), every primitive in order.</summary>
    static IEnumerable<Vector3> Skinned(ModelData model, Matrix4x4[] world)
    {
        foreach (var node in model.Nodes.Where(n => n.Mesh >= 0 && n.Skin >= 0))
        {
            var skin = model.Skins[node.Skin];
            foreach (var primitive in model.Meshes[node.Mesh].Primitives)
                for (var v = 0; v < primitive.Positions.Length; v++)
                {
                    var (j, w) = (primitive.Joints![v], primitive.Weights![v]);
                    var sum = Vector3.Zero;
                    foreach (var (joint, weight) in new[] { (j.J0, w.X), (j.J1, w.Y), (j.J2, w.Z), (j.J3, w.W) })
                        if (weight != 0)
                            sum += weight * Vector3.Transform(primitive.Positions[v], skin.InverseBindMatrices[joint] * world[skin.Joints[joint]]);
                    yield return sum;
                }
        }
    }

    /// <summary>For each engine vertex (at the garment's rest) the index of Blender's vertex at the same place.</summary>
    static int[] Match(Garment garment, JsonElement blenderRest)
    {
        var points = blenderRest.EnumerateArray().Select(p => new Vector3(p[0].GetSingle(), p[1].GetSingle(), p[2].GetSingle())).ToArray();
        return Skinned(garment.Clothing, garment.Clothing.WorldMatrices()).Select(p =>
        {
            var best = Enumerable.Range(0, points.Length).MinBy(i => Vector3.DistanceSquared(points[i], p));
            Assert.True(Vector3.Distance(points[best], p) < 1e-5f, $"a garment vertex at {p} has no Blender vertex at its rest place");
            return best;
        }).ToArray();
    }
}
