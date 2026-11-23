using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Statventure.Animation;
using Statventure.Render3D;
using Statventure.Sandbox.Figure;
using Statventure.Tests.Sandbox;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// The humanoid family's committed models (GM2 step 4a; Docs/Game/Hero.md, "How it works", step 4a): engine/Assets/Models/
/// humanoid, made in Blender by Tools/Models/humanoid (make-humanoid.sh build), packed into models.pak, and the game's look.
/// </summary>
static class HumanoidFiles
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    public static string Engine => Path.GetFullPath(Path.Combine(ProjectDirectory(), "..", ".."));
    /// <summary>The folder models.pak is made of (its entries at models/&lt;path&gt;).</summary>
    public static string Models => Path.Combine(Engine, "Assets", "Models");
    public static string Family => Path.Combine(Models, "humanoid");
    public static string Scripts => Path.Combine(Engine, "Tools", "Models", "humanoid");

    /// <summary>Every model of the family's manifest by its name, with its file (relative to engine/).</summary>
    public static IReadOnlyDictionary<string, string> Manifest()
    {
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Scripts, "manifest.json")));
        return manifest.RootElement.GetProperty("models").EnumerateArray()
            .ToDictionary(m => m.GetProperty("name").GetString()!, m => m.GetProperty("out").GetString()!);
    }

    /// <summary>A model's file by its name in the manifest (a game model's is its path under Assets/Models/humanoid without .glb).</summary>
    public static string Glb(string name) => Path.GetFullPath(Path.Combine(Engine, Manifest()[name]));

    public static string Body(string sex) => Glb($"bodies/human-{sex}-average");
    public static string Face(string sex) => Glb($"parts/face/human-{sex}-1/human-{sex}-average");
    public static string Hair(string sex) => Glb(sex == "male" ? "parts/hair/swept-back/human-male-average" : "parts/hair/long-tied/human-female-average");
    public static string ChainShirt(string sex) => Glb($"gear/chain-shirt/human-{sex}-average");
    public static string Sword => Glb("gear/sword");
    public static string Shield => Glb("gear/shield");

    /// <summary>
    /// The geared man's models at the battle's level (Tools/Models/humanoid/levels.py, GM2-P20; the tests' data, never
    /// packed): the body, the face, the hair, the chain shirt, the sword and the shield, each named battle/ and the full
    /// model's name.
    /// </summary>
    public static readonly string[] BattleMan =
    [
        "battle/bodies/human-male-average", "battle/parts/face/human-male-1/human-male-average", "battle/parts/hair/swept-back/human-male-average",
        "battle/gear/chain-shirt/human-male-average", "battle/gear/sword", "battle/gear/shield",
    ];
    /// <summary>The clip frame's test library (the test clip rest-sway, from the tests' data; never packed).</summary>
    public static string TestClips => Glb("test-clips");

    /// <summary>The game's look, "Ember, hearth" (Tools/Models/humanoid/look.py's EMBER_HEARTH, written as looks/ember-hearth.json).</summary>
    public static FigureLook Ember => FigureLook.Load(Path.Combine(FigureFiles.Looks, "ember-hearth.json"));

    /// <summary>The game's models of the family by name (those the manifest puts in Assets/Models/humanoid; its test data left out): the figures' and the clip library, "clips".</summary>
    public static IReadOnlyList<string> Names() =>
        Manifest().Where(m => m.Value.StartsWith("Assets/Models/humanoid/", StringComparison.Ordinal)).Select(m => m.Key).ToList();

    /// <summary>Every figure model the manifest makes (the clip library and the race emblems left out): the game's and the battle's (the tests' data).</summary>
    public static IReadOnlyList<string> Figures() => [.. Names().Where(n => n != "clips" && !n.StartsWith("emblems/", StringComparison.Ordinal)), .. BattleMan];

    /// <summary>The race emblems the manifest makes (GM2 step 10): emblems/&lt;name&gt;, the name a race's emblem.</summary>
    public static IReadOnlyList<string> Emblems() => [.. Names().Where(n => n.StartsWith("emblems/", StringComparison.Ordinal))];

    /// <summary>
    /// The family's state machines' JSON forms (GM2 step 4b), Assets/Models/humanoid/machines/&lt;name&gt;.json, so models.pak's
    /// records models/humanoid/machines/&lt;name&gt;: the figures' small one, "figure".
    /// </summary>
    public static string Machines => Path.Combine(Family, "machines");

    /// <summary>The machines' names (their files' without .json).</summary>
    public static IReadOnlyList<string> MachineNames() =>
        Directory.GetFiles(Machines, "*.json").Select(f => Path.GetFileNameWithoutExtension(f)).Order(StringComparer.Ordinal).ToList();

    static readonly Dictionary<string, ModelData> Loaded = [];

    public static ModelData Load(string name)
    {
        lock (Loaded)
            return Loaded.TryGetValue(name, out var model) ? model : Loaded[name] = ModelData.Load(Glb(name));
    }
}

/// <summary>
/// The structure of the family's models (GM2-P8): every body and every part made on it carries the family's one skeleton
/// at that body's rest, every vertex follows at most four of its deforming bones with weights summing to one, the face and
/// the hair follow the head alone, every material is a palette slot the game's look colours or a fixed colour, the eyes are
/// unlit, the drawn lines (brows, lashes, mouth; the mail's rows; the sword's fuller) have no hatching and the ones made as
/// strips no outline, the items are rigid with their grip at the origin, and the clip library holds every clip of its
/// folder.
/// </summary>
public class HumanoidModelTests
{
    /// <summary>The skeleton (Tools/Models/humanoid/rig.py, BONES then SOCKETS) with each bone's parent.</summary>
    static readonly Dictionary<string, string?> Skeleton = SkeletonTable();

    static Dictionary<string, string?> SkeletonTable()
    {
        var table = new Dictionary<string, string?> { ["hips"] = null, ["spine"] = "hips", ["chest"] = "spine", ["neck"] = "chest", ["head"] = "neck" };
        foreach (var side in new[] { "L", "R" })
        {
            table[$"shoulder.{side}"] = "chest";
            table[$"upper_arm.{side}"] = $"shoulder.{side}";
            table[$"forearm.{side}"] = $"upper_arm.{side}";
            table[$"hand.{side}"] = $"forearm.{side}";
            table[$"fingers.{side}"] = $"hand.{side}";
            table[$"thumb.{side}"] = $"hand.{side}";
            table[$"thigh.{side}"] = "hips";
            table[$"shin.{side}"] = $"thigh.{side}";
            table[$"foot.{side}"] = $"shin.{side}";
        }
        foreach (var side in new[] { "L", "R" })
        {
            table[$"socket.hand.{side}"] = $"hand.{side}";
            table[$"socket.hip.{side}"] = "hips";
        }
        table["socket.shield.L"] = "forearm.L";
        table["socket.back"] = "chest";
        table["socket.head"] = "head";
        return table;
    }

    static bool IsSocket(string bone) => bone.StartsWith("socket.", StringComparison.Ordinal);

    /// <summary>A model's name without its level's prefix (battle/).</summary>
    static string Unlevelled(string name) => name.StartsWith("battle/", StringComparison.Ordinal) ? name["battle/".Length..] : name;

    /// <summary>The models with a skin: the bodies, the parts made on them and the chain shirts, at both levels.</summary>
    public static TheoryData<string> Skinned() =>
        new(HumanoidFiles.Figures().Where(n => Unlevelled(n) is var u && (u.StartsWith("bodies/") || u.StartsWith("parts/") || u.StartsWith("gear/chain-shirt/"))));

    /// <summary>The body a part or garment is made on: the last part of its name (parts/&lt;category&gt;/&lt;part&gt;/&lt;body&gt;), at its level.</summary>
    static string BodyOf(string name) => name[..(name.Length - Unlevelled(name).Length)] + "bodies/" + name.Split('/')[^1];

    /// <summary>Every file in Assets/Models/humanoid is a model of the manifest or, in machines/, a state machine's JSON form (GM2 step 4b).</summary>
    [Fact]
    public void TheManifestNamesEveryCommittedModel()
    {
        var committed = Directory.GetFiles(HumanoidFiles.Family, "*", SearchOption.AllDirectories)
            .Select(f => Path.GetRelativePath(HumanoidFiles.Family, f).Replace('\\', '/')).Order(StringComparer.Ordinal);
        Assert.Equal(HumanoidFiles.Names().Select(n => n + ".glb").Concat(HumanoidFiles.MachineNames().Select(m => $"machines/{m}.json"))
            .Order(StringComparer.Ordinal), committed);
        Assert.Equal(["figure"], HumanoidFiles.MachineNames());
        Assert.Contains("clips", HumanoidFiles.Names());
        Assert.All(HumanoidFiles.Names(), n => Assert.Equal(Path.Combine(HumanoidFiles.Family, n + ".glb"), HumanoidFiles.Glb(n)));
        // Each race's six bodies (GM2 step 9a the human's: the slender and strong builds beside step 4a's average ones; steps
        // 9b–9e each other race's).
        Assert.Equal(BodyRaces.SelectMany(r => new[] { "female", "male" }.SelectMany(s => new[] { "average", "slender", "strong" }.Select(b => $"bodies/{r}-{s}-{b}")))
            .Order(StringComparer.Ordinal), HumanoidFiles.Names().Where(n => n.StartsWith("bodies/")).Order(StringComparer.Ordinal));
    }

    /// <summary>The races whose bodies the family makes: the human (GM2 steps 4a and 9a) and each race steps 9b–9e have brought in.</summary>
    public static readonly string[] BodyRaces = ["human", "elf", "dark-elf"];

    /// <summary>Each race's two sexes, for the tests of a race's bodies.</summary>
    public static TheoryData<string, string> RaceSexes()
    {
        var data = new TheoryData<string, string>();
        foreach (var race in BodyRaces)
            foreach (var sex in new[] { "male", "female" })
                data.Add(race, sex);
        return data;
    }

    /// <summary>
    /// The builds of a body (GM2 step 9a; Tools/Models/humanoid/bodies, as_build; GM2-P25): a slender and a strong body
    /// have their average body's skeleton, every deforming bone where the average has it (so its clips, grips and the parts
    /// made on the average body fit them as they fit it), only the hip and back sockets moved out or in with the build.
    /// </summary>
    [Theory]
    [MemberData(nameof(RaceSexes))]
    public void ABuildHasItsAverageBodysBones(string race, string sex)
    {
        var average = HumanoidFiles.Load($"bodies/{race}-{sex}-average");
        var rest = average.WorldMatrices();
        foreach (var build in new[] { "slender", "strong" })
        {
            var body = HumanoidFiles.Load($"bodies/{race}-{sex}-{build}");
            var other = body.WorldMatrices();
            var moved = new List<string>();
            foreach (var joint in average.Skins[0].Joints)
            {
                var bone = average.Nodes[joint].Name;
                var at = body.Skins[0].Joints.Single(j => body.Nodes[j].Name == bone);
                if (!Matrix4x4Close(rest[joint], other[at]))
                    moved.Add(bone);
            }
            Assert.Equal(["socket.back", "socket.hip.L", "socket.hip.R"], moved.Order(StringComparer.Ordinal));
            Assert.NotEqual(Triangles(average), Triangles(body));
        }
    }

    [Theory]
    [MemberData(nameof(Skinned))]
    public void EverySkinnedModelCarriesTheFamilysSkeleton(string name)
    {
        var model = HumanoidFiles.Load(name);
        var skin = Assert.Single(model.Skins);
        var joints = skin.Joints.Select(j => model.Nodes[j].Name).ToList();
        Assert.Equal(Skeleton.Keys.Order(StringComparer.Ordinal), joints.Order(StringComparer.Ordinal));
        foreach (var joint in skin.Joints)
        {
            var node = model.Nodes[joint];
            var parent = node.Parent >= 0 && skin.Joints.Contains(node.Parent) ? model.Nodes[node.Parent].Name : null;
            Assert.True(Skeleton[node.Name] == parent, $"{name}: {node.Name}'s parent is {parent ?? "none"}, not {Skeleton[node.Name] ?? "none"}");
        }
        Assert.Equal((23, 7), (Skeleton.Keys.Count(b => !IsSocket(b)), Skeleton.Keys.Count(IsSocket)));
    }

    /// <summary>A part (or garment) binds by bone names to the body it was made on, so its skeleton's rest is that body's.</summary>
    [Theory]
    [MemberData(nameof(Skinned))]
    public void APartIsMadeOnItsBodysRest(string name)
    {
        var body = BodyOf(name);
        var part = HumanoidFiles.Load(name);
        var wearer = HumanoidFiles.Load(body);
        var partRest = part.WorldMatrices();
        var bodyRest = wearer.WorldMatrices();
        var skeleton = new Skeleton(wearer);
        foreach (var joint in part.Skins[0].Joints)
        {
            var bone = part.Nodes[joint].Name;
            var at = skeleton.Find(bone);
            Assert.True(at >= 0, $"{body} has no bone {bone}");
            for (var r = 0; r < 4; r++)
                for (var c = 0; c < 4; c++)
                    Assert.True(Math.Abs(partRest[joint][r, c] - bodyRest[at][r, c]) <= 1e-6f, $"{name}: {bone} rests elsewhere than on {body}");
        }
    }

    [Theory]
    [MemberData(nameof(Skinned))]
    public void EveryVertexFollowsItsDeformingBonesWithWeightsSummingToOne(string name)
    {
        var model = HumanoidFiles.Load(name);
        var joints = model.Skins[0].Joints.Select(j => model.Nodes[j].Name).ToList();
        var vertices = 0;
        foreach (var node in model.Nodes.Where(n => n.Mesh >= 0))
        {
            Assert.True(node.Skin == 0, $"{name}: {node.Name} isn't skinned");
            foreach (var primitive in model.Meshes[node.Mesh].Primitives)
            {
                Assert.NotNull(primitive.Joints);
                Assert.NotNull(primitive.Weights);
                for (var v = 0; v < primitive.Positions.Length; v++)
                {
                    var (j, w) = (primitive.Joints![v], primitive.Weights![v]);
                    var influences = new[] { (j.J0, w.X), (j.J1, w.Y), (j.J2, w.Z), (j.J3, w.W) };
                    Assert.All(influences, i => Assert.True(i.Item2 >= 0, $"{name}: a negative weight"));
                    Assert.True(Math.Abs(w.X + w.Y + w.Z + w.W - 1) <= 1e-5, $"{name}: {node.Name} vertex {v}'s weights sum to {w.X + w.Y + w.Z + w.W}");
                    foreach (var (joint, weight) in influences.Where(i => i.Item2 > 0))
                        Assert.False(IsSocket(joints[joint]), $"{name}: {node.Name} vertex {v} follows the socket {joints[joint]}");
                    vertices++;
                }
            }
        }
        Assert.True(vertices > 0, $"{name} has no vertices");
    }

    /// <summary>Every part (the faces, hair styles, beards, head pieces and marks, both levels): all of them lie on the head.</summary>
    public static TheoryData<string> Parts() => new(HumanoidFiles.Figures().Where(n => Unlevelled(n).StartsWith("parts/", StringComparison.Ordinal)));

    /// <summary>A part (a face, a hair style, a beard, a head piece, a mark) moves with the head as a whole (Q2: no face bones).</summary>
    [Theory]
    [MemberData(nameof(Parts))]
    public void APartOfTheHeadFollowsTheHeadAlone(string name)
    {
        var model = HumanoidFiles.Load(name);
        var joints = model.Skins[0].Joints.Select(j => model.Nodes[j].Name).ToList();
        foreach (var primitive in model.Meshes.SelectMany(m => m.Primitives))
            for (var v = 0; v < primitive.Positions.Length; v++)
            {
                var (j, w) = (primitive.Joints![v], primitive.Weights![v]);
                var followed = new[] { (j.J0, w.X), (j.J1, w.Y), (j.J2, w.Z), (j.J3, w.W) }.Where(i => i.Item2 > 0).Select(i => (joints[i.Item1], i.Item2)).ToList();
                Assert.True(followed is [("head", 1f)], $"{name}: vertex {v} follows {string.Join(", ", followed)}, not the head alone");
            }
    }

    static readonly Dictionary<string, string> Fixed = new() { ["pupil"] = "#060404", ["glint"] = "#efe6d0" };

    /// <summary>
    /// The look's slots of GM2 step 9a that the game's look file doesn't colour: the hair's tie and the marks (a figure shows
    /// them only in its race's swatches; the look file, made before them, colours the first humanoid's slots).
    /// </summary>
    static readonly string[] SwatchesOnly = ["hair.tie", "marking"];

    /// <summary>
    /// Every material is a slot the game's look colours (so a figure shows in the look's colours, a hero's swatches over
    /// them), a slot only the swatches colour (<see cref="SwatchesOnly"/>) or a fixed colour (the pupils and the glints,
    /// their own colour in every look); the eyes are unlit, the drawn lines (brows, lashes and mouth; the mail's rows and the
    /// sword's fuller) and the marks (a mark is its colour alone) unhatched, everything else lit and hatched.
    /// </summary>
    [Fact]
    public void EveryMaterialIsASlotOfTheLookOrAFixedColour()
    {
        var look = HumanoidFiles.Ember;
        var seen = new HashSet<string>();
        foreach (var name in HumanoidFiles.Figures())
            foreach (var material in HumanoidFiles.Load(name).Materials)
            {
                var slot = material.Slot;
                seen.Add(slot);
                if (Fixed.TryGetValue(slot, out var colour))
                {
                    Assert.False(look.Colours.ContainsKey(slot), $"{slot} is a fixed colour, which no look may name");
                    Assert.True(Vector4.Distance(Colors.LinearFromHex(colour), material.BaseColor) < 1e-6f, $"{name}: {slot} isn't {colour}");
                }
                else
                    Assert.True(look.Colours.ContainsKey(slot) != SwatchesOnly.Contains(slot), $"{name}: the material {material.Name} is no slot of the game's look and no fixed colour");
                Assert.True(material.Unlit == slot is "eye.white" or "eyes" or "pupil" or "glint", $"{name}: {slot} {(material.Unlit ? "is" : "isn't")} unlit");
                Assert.True(material.Hatch == slot is not ("brows" or "metal.shade" or "marking"), $"{name}: {slot} {(material.Hatch ? "is" : "isn't")} hatched");
                Assert.Equal(Vector3.Zero, material.Emission);
            }
        Assert.Equal(look.Colours.Keys.Concat(Fixed.Keys).Concat(SwatchesOnly).Order(StringComparer.Ordinal), seen.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// The drawn lines made as thin strips (the mail's rows of rings, the sword's fuller, which the battle's sword hasn't)
    /// have no outline of their own: they are their model's one piece without ink, named so (-unlined). A mark (GM2 step
    /// 9a: a freckle, a scar) is a flat piece on the skin with no outline at all: its model's one piece, without ink, in the
    /// slot <c>marking</c>.
    /// </summary>
    [Fact]
    public void TheStripsOfDrawnLinesHaveNoInk()
    {
        foreach (var name in HumanoidFiles.Figures())
        {
            var model = HumanoidFiles.Load(name);
            var inkless = model.Nodes.Where(n => n.Mesh >= 0 && !n.Ink).ToList();
            var mark = Unlevelled(name).StartsWith("parts/marks/", StringComparison.Ordinal);
            var strips = Unlevelled(name).StartsWith("gear/chain-shirt/") || name == "gear/sword" || mark;
            Assert.True(inkless.Count == (strips ? 1 : 0), $"{name}: {string.Join(", ", inkless.Select(n => n.Name))} without ink");
            Assert.Equal(model.Nodes.Where(n => n.Mesh >= 0 && n.Name.EndsWith("-unlined", StringComparison.Ordinal)).Select(n => n.Name), inkless.Select(n => n.Name));
            Assert.All(inkless.SelectMany(n => model.Meshes[n.Mesh].Primitives), p => Assert.Equal(mark ? "marking" : "metal.shade", model.Materials[p.Material].Slot));
            if (mark)
                Assert.Equal(inkless, model.Nodes.Where(n => n.Mesh >= 0));
        }
    }

    /// <summary>
    /// A model is drawn in as few pieces as it can be (GM2-P19: its pieces joined per ink flag at the build): no two of its
    /// primitives have the same material and the same ink flag.
    /// </summary>
    [Fact]
    public void AModelHasOnePrimitivePerMaterialAndInkFlag()
    {
        foreach (var name in HumanoidFiles.Figures())
        {
            var model = HumanoidFiles.Load(name);
            var draws = model.Nodes.Where(n => n.Mesh >= 0).SelectMany(n => model.Meshes[n.Mesh].Primitives.Select(p => (model.Materials[p.Material].Name, n.Ink))).ToList();
            var twice = draws.GroupBy(d => d).Where(g => g.Count() > 1).Select(g => $"{g.Key.Name} ({(g.Key.Ink ? "inked" : "no ink")}) {g.Count()} times").ToList();
            Assert.True(twice.Count == 0, $"{name}: {string.Join(", ", twice)}");
        }
    }

    /// <summary>
    /// The battle's level (levels.py, GM2-P20; the geared man, the tests' data) is the full figure lighter: the same
    /// skeleton at the same rest, the same materials (but the fuller's), and at most the triangles and primitives measured when it was made
    /// and a tenth (2026-11-16: 7,692 and 6, 1,880 and 6, 1,366 and 1, 4,158 and 3, 358 and 3, 1,232 and 3), each model
    /// with fewer triangles than its full one.
    /// </summary>
    [Fact]
    public void TheBattleLevelIsTheFullFigureLighter()
    {
        var bounds = new Dictionary<string, (int Triangles, int Primitives)>
        {
            ["bodies/human-male-average"] = (8461, 6), ["parts/face/human-male-1/human-male-average"] = (2068, 6),
            ["parts/hair/swept-back/human-male-average"] = (1502, 1), ["gear/chain-shirt/human-male-average"] = (4573, 3),
            ["gear/sword"] = (393, 3), ["gear/shield"] = (1355, 3),
        };
        Assert.Equal(bounds.Keys.Select(n => "battle/" + n), HumanoidFiles.BattleMan);
        foreach (var (full, bound) in bounds)
        {
            var (light, heavy) = (HumanoidFiles.Load("battle/" + full), HumanoidFiles.Load(full));
            Assert.Equal(heavy.Skins.Count, light.Skins.Count);
            if (heavy.Skins.Count > 0)
            {
                Assert.Equal(heavy.Skins[0].Joints.Select(j => heavy.Nodes[j].Name), light.Skins[0].Joints.Select(j => light.Nodes[j].Name));
                var (a, b) = (heavy.WorldMatrices(), light.WorldMatrices());
                foreach (var (x, y) in heavy.Skins[0].Joints.Zip(light.Skins[0].Joints))
                    Assert.True(Matrix4x4Close(a[x], b[y]), $"{full}: {heavy.Nodes[x].Name} rests elsewhere at the battle's level");
            }
            // the same materials, but the battle's sword has no fuller (its lines' metal.shade)
            var slots = heavy.Materials.Select(m => m.Slot).Where(s => full != "gear/sword" || s != "metal.shade");
            Assert.Equal(slots.Order(StringComparer.Ordinal), light.Materials.Select(m => m.Slot).Order(StringComparer.Ordinal));
            var (triangles, fullTriangles) = (Triangles(light), Triangles(heavy));
            var primitives = light.Nodes.Where(n => n.Mesh >= 0).Sum(n => light.Meshes[n.Mesh].Primitives.Count);
            Assert.True(triangles <= bound.Triangles, $"battle/{full}: {triangles} triangles, over its bound {bound.Triangles}");
            Assert.True(primitives <= bound.Primitives, $"battle/{full}: {primitives} primitives, over its bound {bound.Primitives}");
            Assert.True(triangles < fullTriangles, $"battle/{full}: {triangles} triangles, the full model {fullTriangles}");
        }
    }

    /// <summary>
    /// The plain outfit stays under the chain shirt where the shirt covers it, in a deep forward lean with a twist (the
    /// spine and the chest each bent 30° forward and turned 20°, a lunge, the arms raised: <see cref="Lean"/>): a garment cut from the body takes the body's weights
    /// at the nearest point of its surface and every skirt the body's own heights (rig.bind_like, Maker.skirt_weights;
    /// the manager, 2026-11-16: with the nearest vertex's weights and each skirt's own length the tunic showed through
    /// the mail). An outfit vertex facing out from the body and sideways (not a piece's inner side, not a hem's top or
    /// bottom) is covered where the line out of it along its normal meets the mail (or the belt worn over it) within
    /// 40 mm at rest; in the lean it shows through where that line meets nothing within 50 mm in front of it (from half
    /// a millimetre behind it) and the mail lies behind it within 50 mm. Both levels.
    /// </summary>
    [Theory]
    [InlineData("male", "")]
    [InlineData("female", "")]
    [InlineData("male", "battle/")]
    public void TheOutfitStaysUnderTheMailInALeanAndATwist(string sex, string level)
    {
        var body = HumanoidFiles.Load($"{level}bodies/human-{sex}-average");
        var mail = HumanoidFiles.Load($"{level}gear/chain-shirt/human-{sex}-average");
        var skeleton = new Skeleton(body);
        var garment = new Garment(mail, skeleton);
        var pose = new ModelPose();
        static Quaternion Turn(float x, float y, float z) => Quaternion.CreateFromAxisAngle(Vector3.UnitX, x * MathF.PI / 180)
            * Quaternion.CreateFromAxisAngle(Vector3.UnitY, y * MathF.PI / 180) * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, z * MathF.PI / 180);
        foreach (var (bone, turn) in Lean)
        {
            var node = skeleton.Find(bone);
            var rest = body.Nodes[node];
            pose.Set(node, rest.Translation, rest.Rotation * Turn(turn.X, turn.Y, turn.Z), rest.Scale);
        }
        var (restWorld, posedWorld) = (body.WorldMatrices(), body.WorldMatrices(pose));
        var head = skeleton.Find("head");
        Assert.True(posedWorld[head].Translation.Z - restWorld[head].Translation.Z > 0.2f, "the pose isn't a forward lean");

        var outfitRest = SkinnedSurface(body, restWorld, slot => slot != "skin");
        var outfitPosed = SkinnedSurface(body, posedWorld, slot => slot != "skin");
        static bool Belt(string slot) => slot is "leather" or "buckle";
        var mailRest = new MailSurface(SkinnedSurface(mail, garment.WorldMatrices(restWorld, restWorld), _ => true), SkinnedSurface(body, restWorld, Belt));
        var mailPosed = new MailSurface(SkinnedSurface(mail, garment.WorldMatrices(restWorld, posedWorld), _ => true), SkinnedSurface(body, posedWorld, Belt));
        var (covered, through) = (0, new List<Vector3>());
        for (var v = 0; v < outfitRest.Points.Count; v++)
        {
            var (point, normal) = (outfitRest.Points[v], outfitRest.Normals[v]);
            var outward = normal.X * point.X + normal.Z * point.Z > 0;
            if (!outward || Math.Abs(normal.Y) > 0.7f || mailRest.Nearest(point, normal) > 0.04f)
                continue;
            covered++;
            var (posed, turned) = (outfitPosed.Points[v], outfitPosed.Normals[v]);
            if (float.IsInfinity(mailPosed.Nearest(posed, turned)) && !float.IsInfinity(mailPosed.Nearest(posed, -turned)))
                through.Add(outfitRest.Points[v]);
        }
        Assert.True(covered > 500, $"{level}{sex}: only {covered} outfit vertices under the mail at rest");
        Assert.True(through.Count == 0, $"{level}{sex}: {through.Count} of {covered} covered outfit vertices outside the mail in the lean (at rest: {string.Join(", ", through.Take(5))})");
    }

    /// <summary>The lean (each bone's turn in its own axes, XYZ, degrees): the spine and the chest bent forward and turned, a lunge, the arms raised.</summary>
    static readonly (string Bone, Vector3 Turn)[] Lean =
    [
        ("spine", new(-30, 20, 0)), ("chest", new(-30, 20, 0)), ("thigh.L", new(-35, 0, 0)), ("thigh.R", new(20, 0, 0)),
        ("upper_arm.L", new(0, 0, -40)), ("upper_arm.R", new(0, 0, 40)),
    ];

    /// <summary>A model's vertices and their normals (CPU skinning, as skin.comp blends), and its triangles, of the primitives whose material slot is wanted.</summary>
    static (List<Vector3> Points, List<Vector3> Normals, List<(int, int, int)> Triangles) SkinnedSurface(ModelData model, Matrix4x4[] world, Func<string, bool> wanted)
    {
        var (points, normals, triangles) = (new List<Vector3>(), new List<Vector3>(), new List<(int, int, int)>());
        foreach (var node in model.Nodes.Where(n => n.Mesh >= 0 && n.Skin >= 0))
        {
            var skin = model.Skins[node.Skin];
            foreach (var primitive in model.Meshes[node.Mesh].Primitives.Where(p => wanted(model.Materials[p.Material].Slot)))
            {
                var first = points.Count;
                for (var v = 0; v < primitive.Positions.Length; v++)
                {
                    var (j, w) = (primitive.Joints![v], primitive.Weights![v]);
                    var matrix = new Matrix4x4();
                    foreach (var (joint, weight) in new[] { (j.J0, w.X), (j.J1, w.Y), (j.J2, w.Z), (j.J3, w.W) })
                        if (weight != 0)
                            matrix += skin.InverseBindMatrices[joint] * world[skin.Joints[joint]] * weight;
                    points.Add(Vector3.Transform(primitive.Positions[v], matrix));
                    normals.Add(Vector3.Normalize(Vector3.TransformNormal(primitive.Normals[v], matrix)));
                }
                for (var i = 0; i < primitive.Indices.Length; i += 3)
                    triangles.Add((first + (int)primitive.Indices[i], first + (int)primitive.Indices[i + 1], first + (int)primitive.Indices[i + 2]));
            }
        }
        return (points, normals, triangles);
    }

    /// <summary>The covering surfaces' triangles by 5 cm cells (each in every cell within 50 mm of it), for the lines out of the outfit's vertices.</summary>
    sealed class MailSurface
    {
        const float Cell = 0.05f, Reach = 0.05f, Behind = 0.0005f;
        readonly List<(Vector3 A, Vector3 B, Vector3 C)> _faces = [];
        readonly Dictionary<(int, int, int), List<int>> _cells = [];

        public MailSurface(params (List<Vector3> Points, List<Vector3> Normals, List<(int, int, int)> Triangles)[] meshes)
        {
            foreach (var mesh in meshes)
            foreach (var (a, b, c) in mesh.Triangles)
            {
                var (pa, pb, pc) = (mesh.Points[a], mesh.Points[b], mesh.Points[c]);
                var index = _faces.Count;
                _faces.Add((pa, pb, pc));
                var (low, high) = (Vector3.Min(pa, Vector3.Min(pb, pc)) - new Vector3(Reach), Vector3.Max(pa, Vector3.Max(pb, pc)) + new Vector3(Reach));
                for (var x = Key(low.X); x <= Key(high.X); x++)
                    for (var y = Key(low.Y); y <= Key(high.Y); y++)
                        for (var z = Key(low.Z); z <= Key(high.Z); z++)
                        {
                            if (!_cells.TryGetValue((x, y, z), out var list))
                                _cells[(x, y, z)] = list = [];
                            list.Add(index);
                        }
            }
        }

        static int Key(float value) => (int)MathF.Floor(value / Cell);

        /// <summary>How far along its normal the line from the point first meets a triangle (from half a millimetre behind it; +∞: not within 50 mm).</summary>
        public float Nearest(Vector3 point, Vector3 normal)
        {
            var nearest = float.PositiveInfinity;
            if (!_cells.TryGetValue((Key(point.X), Key(point.Y), Key(point.Z)), out var list))
                return nearest;
            foreach (var index in list)
            {
                // Möller–Trumbore
                var (a, b, c) = _faces[index];
                var (e1, e2) = (b - a, c - a);
                var p = Vector3.Cross(normal, e2);
                var determinant = Vector3.Dot(e1, p);
                if (Math.Abs(determinant) < 1e-12f)
                    continue;
                var s = point - a;
                var u = Vector3.Dot(s, p) / determinant;
                if (u < 0 || u > 1)
                    continue;
                var q = Vector3.Cross(s, e1);
                var v = Vector3.Dot(normal, q) / determinant;
                if (v < 0 || u + v > 1)
                    continue;
                var t = Vector3.Dot(e2, q) / determinant;
                if (t >= -Behind && t <= Reach)
                    nearest = Math.Min(nearest, t);
            }
            return nearest;
        }
    }

    static int Triangles(ModelData model) => model.Nodes.Where(n => n.Mesh >= 0).Sum(n => model.Meshes[n.Mesh].Primitives.Sum(p => p.Indices.Length / 3));

    static bool Matrix4x4Close(Matrix4x4 a, Matrix4x4 b)
    {
        for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
                if (Math.Abs(a[r, c] - b[r, c]) > 1e-6f)
                    return false;
        return true;
    }

    /// <summary>
    /// The sword and the shield are rigid items (no skin) in their own space: the grip at the origin and the axes the
    /// socket's (Docs/Animation.md, "Gear in detail"; read back through <see cref="Socket.ObjectAxesToBone"/> to the bone's):
    /// the sword's blade runs up the socket's Y to its point 0.88 m out, its pommel just below the grip; the shield lies
    /// all on the socket's outer side (+Z: it stands off the arm, as its approval asked), its point toward −X.
    /// </summary>
    [Fact]
    public void TheItemsAreRigidWithTheirGripAtTheOrigin()
    {
        static List<Vector3> Points(ModelData model)
        {
            var world = model.WorldMatrices();
            return model.Nodes.Select((n, i) => (n, i)).Where(x => x.n.Mesh >= 0)
                .SelectMany(x => model.Meshes[x.n.Mesh].Primitives.SelectMany(p => p.Positions.Select(q => Vector3.Transform(Vector3.Transform(q, world[x.i]), Socket.ObjectAxesToBone))))
                .ToList();
        }
        var sword = HumanoidFiles.Load("gear/sword");
        var shield = HumanoidFiles.Load("gear/shield");
        Assert.Empty(sword.Skins);
        Assert.Empty(shield.Skins);
        var blade = Points(sword);
        Assert.Equal(0.88f, blade.Max(p => p.Y), 3);
        Assert.InRange(blade.Min(p => p.Y), -0.1f, -0.09f);
        Assert.True(blade.Max(p => Math.Abs(p.X)) < 0.02f && blade.Max(p => Math.Abs(p.Z)) < 0.11f, "the sword is wider than its guard");
        var face = Points(shield);
        Assert.True(face.Min(p => p.Z) > 0, $"the shield reaches {face.Min(p => p.Z)} m into the arm");
        var (low, high) = (face.Min(p => p.X), face.Max(p => p.X));
        Assert.True(-low > 2 * high, $"the shield's point isn't toward −X ({low} to {high})");
    }

    /// <summary>
    /// A clip library holds every clip of its folder (Tools/Models/humanoid/clips/library.py): the test library, every clip
    /// of the tests' test-clips/ folder, the frame's test clip as its data says.
    /// </summary>
    [Fact]
    public void TheClipLibraryHoldsEveryClipOfItsFolder()
    {
        var library = HumanoidFiles.Load("test-clips");
        Assert.Empty(library.Meshes);
        var clips = AnimationClip.AllOf(library);
        Assert.Equal(Directory.GetFiles(Path.ChangeExtension(HumanoidFiles.TestClips, null), "*.json").Select(Path.GetFileNameWithoutExtension).Order(StringComparer.Ordinal),
            clips.Select(c => c.Name).Order(StringComparer.Ordinal));
        var sway = clips.Single(c => c.Name == "rest-sway");
        Assert.Equal(2.0, sway.Duration, 6);
        Assert.Equal([("inhale", 0.0), ("exhale", 1.0)], sway.Events.Select(e => (e.Name, Math.Round(e.Time, 6))));
        Assert.Equal(["chest", "head", "hips", "spine"], sway.Tracks.Select(t => t.Node).Order(StringComparer.Ordinal));
        Assert.All(sway.Tracks, t => Assert.True(t.Relative && t.Scale == null && (t.Node == "hips" ? t.Rotation == null && t.Translation != null : t.Rotation != null && t.Translation == null), t.Node));
        // A loop: the last frame's pose is the first's.
        var pose = new ClipBinding(sway, new Skeleton(HumanoidFiles.Load("bodies/human-male-average")));
        var (first, last) = (pose.Pose(0), pose.Pose(2));
        var body = HumanoidFiles.Load("bodies/human-male-average");
        var (a, b) = (body.WorldMatrices(first), body.WorldMatrices(last));
        for (var i = 0; i < a.Length; i++)
            Assert.True(Vector3.Distance(a[i].Translation, b[i].Translation) < 1e-6f, body.Nodes[i].Name);
    }
}
