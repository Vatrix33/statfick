using System.Numerics;
using System.Text.Json;
using Statventure.Render3D;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// The race emblems' models (GM2 step 10; the manager's make, GM2-P32; Tools/Models/humanoid/emblems/medallion.py): one
/// for each race's emblem name in the game's data, made by the family's builder; each a rigid medallion (no skeleton, no
/// clips) whose materials are the emblems' palette slots (look.py's EMBLEM_SLOTS: the rim and the field always, the cut-in
/// inlays unhatched and drawn without ink, every other part lit, hatched and inked), in as few pieces as can be (GM2-P19),
/// its middle at the origin, its field facing the figures' front (glTF's +Z), 1.33 units of 0.08 m across from the middle,
/// its plain back 0.10 units behind the field.
/// </summary>
public class EmblemModelTests
{
    const float Unit = 0.08f;

    static readonly string[] Slots = ["emblem.rim", "emblem.field", "emblem.high", "emblem.metal", "emblem.accent", "emblem.cut"];

    /// <summary>The emblem names of the game's races (Assets/Data/data/races/*.json), those that have one.</summary>
    static IReadOnlyList<string> RaceEmblems() =>
        [.. Directory.GetFiles(Path.Combine(HumanoidFiles.Engine, "Assets", "Data", "data", "races"), "*.json")
            .Select(f => JsonDocument.Parse(File.ReadAllText(f)).RootElement)
            .Where(r => r.TryGetProperty("emblem", out var e) && e.ValueKind == JsonValueKind.String)
            .Select(r => r.GetProperty("emblem").GetString()!)
            .Order(StringComparer.Ordinal)];

    public static TheoryData<string> Emblems => new(HumanoidFiles.Emblems());

    /// <summary>The manifest makes exactly the races' emblems, each by the emblem builder with its name, at no detail level.</summary>
    [Fact]
    public void TheManifestMakesEveryRacesEmblem()
    {
        Assert.Equal(9, RaceEmblems().Count);
        Assert.Equal(RaceEmblems().Select(e => "emblems/" + e), HumanoidFiles.Emblems().Order(StringComparer.Ordinal));
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(HumanoidFiles.Scripts, "manifest.json")));
        foreach (var entry in manifest.RootElement.GetProperty("models").EnumerateArray().Where(m => m.GetProperty("name").GetString()!.StartsWith("emblems/")))
        {
            var name = entry.GetProperty("name").GetString()!;
            Assert.Equal("emblems.medallion:emblem_model", entry.GetProperty("builder").GetString());
            Assert.Equal([name["emblems/".Length..]], entry.GetProperty("args").EnumerateArray().Select(a => a.GetString()));
            Assert.False(entry.TryGetProperty("level", out _), $"{name} has a detail level");
            Assert.Equal($"Assets/Models/humanoid/{name}.glb", entry.GetProperty("out").GetString());
        }
    }

    [Theory]
    [MemberData(nameof(Emblems))]
    public void AMedallionIsRigidAndColouredByTheEmblemsSlots(string name)
    {
        var model = HumanoidFiles.Load(name);
        Assert.Empty(model.Skins);
        Assert.Empty(model.Animations);
        var slots = model.Materials.Select(m => m.Slot).ToList();
        Assert.All(slots, s => Assert.Contains(s, Slots));
        Assert.Contains("emblem.rim", slots);
        Assert.Contains("emblem.field", slots);
        foreach (var material in model.Materials)
        {
            Assert.False(material.Unlit, $"{name}: {material.Slot} is unlit");
            Assert.Equal(Vector3.Zero, material.Emission);
            Assert.True(material.Hatch == (material.Slot != "emblem.cut"), $"{name}: {material.Slot} {(material.Hatch ? "is" : "isn't")} hatched");
        }
        // One piece inked, named after the emblem, and the inlays (if any) one piece without ink; one primitive per
        // material and ink flag.
        var drawn = model.Nodes.Where(n => n.Mesh >= 0).ToList();
        var emblem = name["emblems/".Length..];
        Assert.Equal($"emblem-{emblem}", Assert.Single(drawn, n => n.Ink).Name);
        var inlays = drawn.Where(n => !n.Ink).ToList();
        Assert.True(inlays.Count <= 1);
        Assert.All(inlays, n =>
        {
            Assert.Equal($"emblem-{emblem}-unlined", n.Name);
            Assert.All(model.Meshes[n.Mesh].Primitives, p => Assert.Equal("emblem.cut", model.Materials[p.Material].Slot));
        });
        Assert.Equal(slots.Contains("emblem.cut"), inlays.Count == 1);
        var draws = drawn.SelectMany(n => model.Meshes[n.Mesh].Primitives.Select(p => (p.Material, n.Ink))).ToList();
        Assert.Equal(draws.Count, draws.Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Emblems))]
    public void AMedallionHasItsMiddleAtTheOriginAndItsFieldToTheFront(string name)
    {
        var model = HumanoidFiles.Load(name);
        var world = model.WorldMatrices();
        var points = new List<Vector3>();
        Vector3? fieldNormal = null;
        foreach (var (node, index) in model.Nodes.Select((n, i) => (n, i)).Where(p => p.n.Mesh >= 0))
            foreach (var primitive in model.Meshes[node.Mesh].Primitives)
            {
                var placed = primitive.Positions.Select(p => Vector3.Transform(p, world[index])).ToList();
                points.AddRange(placed);
                if (model.Materials[primitive.Material].Slot == "emblem.field")
                {
                    // The field: a flat disc in the plane z = 0 facing +Z.
                    Assert.All(placed, p => Assert.True(MathF.Abs(p.Z) < 1e-6f, $"{name}: the field isn't flat at z = 0 ({p})"));
                    var sum = primitive.Normals.Aggregate(Vector3.Zero, (a, n) => a + Vector3.TransformNormal(n, world[index]));
                    fieldNormal = Vector3.Normalize(sum);
                }
            }
        Assert.NotNull(fieldNormal);
        Assert.True(Vector3.Dot(fieldNormal!.Value, Vector3.UnitZ) > 0.9999f, $"{name}: the field faces {fieldNormal}");
        var radius = points.Max(p => MathF.Sqrt(p.X * p.X + p.Y * p.Y));
        Assert.True(MathF.Abs(radius - 1.33f * Unit) < 1e-5f, $"{name}: the rim reaches {radius} m");
        var (minX, maxX, minY, maxY) = (points.Min(p => p.X), points.Max(p => p.X), points.Min(p => p.Y), points.Max(p => p.Y));
        Assert.True(MathF.Abs(minX + maxX) < 1e-5f && MathF.Abs(minY + maxY) < 1e-5f, $"{name}: not centred ({minX}..{maxX}, {minY}..{maxY})");
        Assert.True(MathF.Abs(points.Min(p => p.Z) + 0.10f * Unit) < 1e-6f, $"{name}: the back is at {points.Min(p => p.Z)} m");
        // The motif stands off the field toward the front, within a few of its steps (0.07 units and 0.022 a step).
        Assert.InRange(points.Max(p => p.Z), 0.145f * Unit, 0.30f * Unit);
    }
}
