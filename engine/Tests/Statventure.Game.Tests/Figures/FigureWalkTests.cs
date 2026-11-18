using System.Numerics;
using Statventure.Core;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Figures;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Tests.Infrastructure;
using Xunit.Abstractions;

namespace Statventure.Game.Tests;

/// <summary>
/// The structural walk over every figure the data allows (GM2 step 5; Docs/Game/Hero.md, "The structural walk"), without
/// rendering one. A figure is its body, one option of each category its body offers and one swatch of each slot its race
/// has swatches for; whether one builds whole is decided by its body and each of its options on that body and each slot it
/// uses, apart (no option reaches into another), so walking every body with every option offered on it (each pair) and
/// every slot walks every combination: each option resolves (<see cref="HeroFigure.Resolve"/>), each model reads, each
/// part's model binds to the body's bones (every joint a bone of the body's, at the body's rest: made on that body, no
/// refit), no model is drawn twice, every material is a look's slot or a fixed colour and a part's are its <c>slots</c>,
/// every slot the body and its options use is filled by the race's swatches (a race with none draws its models' own colours:
/// listed apart), and every preset builds.
/// </summary>
public class FigureWalkTests(ITestOutputHelper output)
{
    /// <summary>Materials of a fixed colour in every look (look.py's FIXED: the pupils, the eyes' glints), in no slot.</summary>
    static readonly string[] Fixed = ["pupil", "glint"];

    /// <summary>What the walk found in some data: its problems, the races drawn in their models' own colours, the pairs walked and the combinations they stand for.</summary>
    internal sealed record Walk(List<string> Problems, SortedSet<string> Uncoloured, int Pairs, double Combinations);

    /// <summary>The walk over <paramref name="data"/>'s bodies, parts, presets and races' swatches.</summary>
    internal static Walk Over(VirtualFileSystem data)
    {
        var problems = new List<string>();
        var uncoloured = new SortedSet<string>(StringComparer.Ordinal);
        var looks = LookData.Load(data);
        var races = RaceCatalogue.Load(data).ToDictionary(r => r.Id, r => r.Race, StringComparer.Ordinal);
        var pairs = 0;
        var combinations = 0.0;
        foreach (var (name, body) in looks.Bodies)
        {
            if (!races.TryGetValue(body.Race, out var race))
            {
                problems.Add($"{name}: no race {body.Race}");
                continue;
            }
            ModelData bodyModel;
            try
            {
                bodyModel = ModelData.Load(data, body.Model);
            }
            catch (DataFileException problem)
            {
                problems.Add($"{name}: its model doesn't read ({problem.Message})");
                continue;
            }
            var bodyRest = bodyModel.WorldMatrices();
            var bones = Enumerable.Range(0, bodyModel.Nodes.Count).ToDictionary(i => bodyModel.Nodes[i].Name, i => i, StringComparer.Ordinal);
            var used = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var slot in SlotsOf(bodyModel))
                if (Fixed.Contains(slot))
                    continue;
                else if (!LookNames.Slots.Contains(slot, StringComparer.Ordinal))
                    problems.Add($"{name}: a material in \"{slot}\", which is neither a look's slot nor a fixed colour");
                else
                    used.Add(slot);
            var drawn = new Dictionary<string, string>(StringComparer.Ordinal) { [body.Model] = "the body" };
            var product = 1.0;
            foreach (var (category, options) in looks.Offered(name, body))
            {
                product *= options.Count;
                foreach (var option in options)
                {
                    pairs++;
                    var at = $"{name} with {category} {option}";
                    FigurePlan plan;
                    try
                    {
                        plan = HeroFigure.Resolve(data, new LookHero { Race = body.Race, Sex = body.Sex, Body = name, Parts = new Dictionary<string, string> { [category] = option } });
                    }
                    catch (LookRefusedException refused)
                    {
                        problems.Add($"{at}: refused ({string.Join("; ", refused.Problems)})");
                        continue;
                    }
                    var part = looks.Parts[option];
                    if (plan.Parts.Single().Model is not { } path)
                    {
                        if (part.Slots.Count > 0)
                            problems.Add($"{at}: drawn as nothing, yet its slots are {string.Join(", ", part.Slots)}");
                        continue;
                    }
                    if (!drawn.TryAdd(path, $"{category} {option}"))
                        problems.Add($"{at}: its model {path} is drawn by {drawn[path]} too");
                    ModelData model;
                    try
                    {
                        model = ModelData.Load(data, path);
                    }
                    catch (DataFileException problem)
                    {
                        problems.Add($"{at}: its model doesn't read ({problem.Message})");
                        continue;
                    }
                    problems.AddRange(Binding(model, bodyRest, bones).Select(p => $"{at}: {p}"));
                    var slots = SlotsOf(model).Where(s => !Fixed.Contains(s)).ToHashSet(StringComparer.Ordinal);
                    foreach (var slot in slots.Where(s => !LookNames.Slots.Contains(s, StringComparer.Ordinal)).Order(StringComparer.Ordinal))
                        problems.Add($"{at}: a material in \"{slot}\", which is neither a look's slot nor a fixed colour");
                    if (!slots.SetEquals(part.Slots))
                        problems.Add($"{at}: its materials' slots are {string.Join(", ", slots.Order(StringComparer.Ordinal))}, its record's {string.Join(", ", part.Slots.Order(StringComparer.Ordinal))}");
                    used.UnionWith(slots.Where(s => LookNames.Slots.Contains(s, StringComparer.Ordinal)));
                }
            }
            var swatchSlots = race.Swatches.Select(s => s.Slot).Distinct(StringComparer.Ordinal).ToList();
            if (swatchSlots.Count == 0)
                uncoloured.Add(body.Race);
            else
                foreach (var slot in used.Where(s => !swatchSlots.Contains(s, StringComparer.Ordinal)))
                    problems.Add($"{name}: \"{slot}\" isn't filled (the {body.Race} has no {slot} swatches)");
            foreach (var slot in swatchSlots)
                product *= race.Swatches.Count(s => s.Slot == slot);
            combinations += product;
        }
        foreach (var (name, preset) in looks.Presets)
        {
            try
            {
                HeroFigure.Resolve(data, preset.Look);
            }
            catch (LookRefusedException refused)
            {
                problems.Add($"the preset {name}: refused ({string.Join("; ", refused.Problems)})");
            }
        }
        return new Walk(problems, uncoloured, pairs, combinations);
    }

    /// <summary>Every material's slot (a material's name without Blender's ".001").</summary>
    static IEnumerable<string> SlotsOf(ModelData model) => model.Materials.Select(m => m.Slot).Distinct(StringComparer.Ordinal);

    /// <summary>How a part's model fails to bind to the body: skinned at all, each joint a bone of the body's, at the body's rest.</summary>
    static IEnumerable<string> Binding(ModelData model, Matrix4x4[] bodyRest, Dictionary<string, int> bones)
    {
        if (model.Skins.Count == 0)
        {
            yield return "its model isn't bound to bones";
            yield break;
        }
        var rest = model.WorldMatrices();
        foreach (var joint in model.Skins.SelectMany(s => s.Joints).Distinct().Order())
        {
            var bone = model.Nodes[joint].Name;
            if (!bones.TryGetValue(bone, out var index))
                yield return $"its joint {bone} isn't a bone of the body's";
            else if (!Close(rest[joint], bodyRest[index]))
                yield return $"its joint {bone} isn't at the body's rest (made on another body)";
        }
    }

    static bool Close(Matrix4x4 a, Matrix4x4 b)
    {
        for (var r = 0; r < 4; r++)
            for (var c = 0; c < 4; c++)
                if (MathF.Abs(a[r, c] - b[r, c]) > 1e-5f)
                    return false;
        return true;
    }

    /// <summary>The game's packs mounted as the game mounts them, and a folder of the test's on top (none: none).</summary>
    static VirtualFileSystem Data(ScratchFolder folder, string? overlay = null)
    {
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        return new VirtualFileSystem(Application.DataMounts(new ApplicationOptions { DevelopmentFolder = overlay }, folder["game"], _ => null));
    }

    /// <summary>Every figure the game's data allows builds whole; the races not coloured yet are the human alone (its swatches are GM2 step 9a's, approved with its presets).</summary>
    [Fact]
    public void EveryFigureTheGamesDataAllowsBuildsWhole()
    {
        using var folder = new ScratchFolder();
        using var data = Data(folder);
        var walk = Over(data);
        Assert.True(walk.Problems.Count == 0, string.Join("\n", walk.Problems));
        // The expected state until GM2 step 9a gives the human its swatches (with its presets' colours): its figures draw
        // their models' own colours, and every slot is held to the swatches from then on (this list then empty).
        Assert.Equal(["human"], walk.Uncoloured);
        // The first humanoid at least: two bodies, a face and a hair style each.
        Assert.True(walk.Pairs >= 4 && walk.Combinations >= 2, $"{walk.Pairs} pairs, {walk.Combinations} combinations");
        output.WriteLine($"{LookData.Load(data).Bodies.Count} bodies, {walk.Pairs} pairs walked, {walk.Combinations} combinations");
    }

    /// <summary>A record of the test's over the game's data: its JSON form at its path in the overlay.</summary>
    static void Put(ScratchFolder folder, string path, object record)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(folder["overlay/" + path])!);
        File.WriteAllText(folder["overlay/" + path + ".json"], RecordSerializer.ToJson(record.GetType(), record));
    }

    static LookPart Hair(VirtualFileSystem data) => LookData.Load(data).Parts["hair-swept-back"];

    public static TheoryData<string> Breaks => new(["another body's model", "no bones", "a slot left out", "a slot too many", "drawn twice", "unfilled", "drawn as nothing with slots", "a preset naming nothing", "a missing model"]);

    /// <summary>Each rule of the walk finds its break: the game's data with one record of the test's over it.</summary>
    [Theory]
    [MemberData(nameof(Breaks))]
    public void EachBreakIsFound(string what)
    {
        using var folder = new ScratchFolder();
        using var gameFolder = new ScratchFolder();
        string expected;
        using (var game = Data(gameFolder))
        {
            var hair = Hair(game);
            var human = LookData.RaceOf(game, "human")!;
            switch (what)
            {
                case "another body's model":
                    // The woman's hair, made on her body, offered on the man's.
                    Put(folder, "data/looks/parts/hair-swept-back", hair with { Models = [new() { Model = "models/humanoid/parts/hair/long-tied/human-female-average", Bodies = ["human-male-average"] }] });
                    expected = "human-male-average with hair hair-swept-back: its joint * isn't at the body's rest (made on another body)";
                    break;
                case "no bones":
                    Put(folder, "data/looks/parts/hair-swept-back", hair with { Models = [new() { Model = "models/humanoid/gear/sword", Bodies = ["human-male-average"] }] });
                    expected = "human-male-average with hair hair-swept-back: its model isn't bound to bones";
                    break;
                case "a slot left out":
                    Put(folder, "data/looks/parts/hair-swept-back", hair with { Slots = [] });
                    expected = "human-male-average with hair hair-swept-back: its materials' slots are hair, its record's ";
                    break;
                case "a slot too many":
                    Put(folder, "data/looks/parts/hair-swept-back", hair with { Slots = ["hair", "hair.tie"] });
                    expected = "human-male-average with hair hair-swept-back: its materials' slots are hair, its record's hair, hair.tie";
                    break;
                case "drawn twice":
                    // A crest of the man's whose model is his face's.
                    Put(folder, "data/looks/parts/crest-face", new LookPart
                    {
                        Category = "crest", Name = "A face again", Races = ["human"], Sexes = ["male"], Slots = ["skin", "eyes", "brows", "eye.white"],
                        Models = [new() { Model = "models/humanoid/parts/face/human-male-1/human-male-average", Bodies = ["human-male-average"] }],
                    });
                    expected = "human-male-average with crest crest-face: its model models/humanoid/parts/face/human-male-1/human-male-average is drawn by face face-human-male-1 too";
                    break;
                case "unfilled":
                    // The human given swatches, but no cloth among them.
                    Put(folder, "data/races/human", human with { Swatches = [new() { Slot = "skin", Name = "Fair", Colour = "#f0c8a0" }] });
                    expected = "human-female-average: \"brows\" isn't filled (the human has no brows swatches)";
                    break;
                case "drawn as nothing with slots":
                    Put(folder, "data/looks/parts/hair-bald", new LookPart { Category = "hair", Name = "Bald", Races = ["human"], Sexes = ["male"], Slots = ["hair"] });
                    expected = "human-male-average with hair hair-bald: drawn as nothing, yet its slots are hair";
                    break;
                case "a preset naming nothing":
                    var preset = LookData.Load(game).Presets["human-male-3"];
                    Put(folder, "data/looks/presets/human-male-3", preset with { Look = preset.Look with { Parts = new Dictionary<string, string>(preset.Look.Parts) { ["hair"] = "hair-gone" } } });
                    expected = "the preset human-male-3: refused (no look part \"hair-gone\" (data/looks/parts/hair-gone))";
                    break;
                case "a missing model":
                    Put(folder, "data/looks/parts/hair-swept-back", hair with { Models = [new() { Model = "models/humanoid/parts/hair/gone", Bodies = ["human-male-average"] }] });
                    expected = "human-male-average with hair hair-swept-back: refused (\"hair-swept-back\": no model \"models/humanoid/parts/hair/gone\")";
                    break;
                default:
                    throw new ArgumentException(what);
            }
        }
        using var data = Data(folder, folder["overlay"]);
        var walk = Over(data);
        // A joint's name (*) is whichever joint comes first.
        var (start, end) = expected.Contains('*') ? (expected[..expected.IndexOf('*')], expected[(expected.IndexOf('*') + 1)..]) : (expected, "");
        Assert.True(walk.Problems.Any(p => expected.Contains('*') ? p.StartsWith(start, StringComparison.Ordinal) && p.EndsWith(end, StringComparison.Ordinal) : p == expected),
            $"{what}: expected \"{expected}\" among: {string.Join("; ", walk.Problems)}");
    }
}
