using System.Text.Json;
using Statventure.Core;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The maker's data in the game (GM2 step 6; Docs/Game/Hero.md, "How it works", step 6): the old game's 72 looks, which
/// the presets are (<c>Reference/old-game/look-catalogue.json</c>, the oracle's run of <c>sprite-catalog.js</c>), wait
/// there until their bodies' models are in <c>models.pak</c> (the manager's GM2-P18: a look's models are references,
/// refused when missing, so bodies, parts and presets enter <c>data.pak</c> with their models). Moving them in is one step,
/// the writer here: with <c>STATVENTURE_LOOK_DATA=write</c> the tests write into <c>Assets/Data/data/looks/</c> a body
/// for each model <c>models/humanoid/bodies/&lt;race&gt;-&lt;sex&gt;-&lt;build&gt;</c> in <c>Assets/Packs/models.pak</c> and a preset
/// for each old look whose race and sex have their average body (its parts and colours kept from the preset's file when
/// there is one); then <c>Tools/Scripts/make-data-pack.sh</c>. What is committed is held to the catalogue (a preset's
/// name, label, order, race, sex and average body) and to its checks over the game's packs.
/// </summary>
public class LookDataTests
{
    /// <summary>Set to <c>write</c>: the writer's output (<see cref="Written"/>) is written into <c>Assets/Data/data/looks/</c>.</summary>
    public const string WriteVariable = "STATVENTURE_LOOK_DATA";

    static string DataFolder => Path.Combine(RaceOracle.Engine, "Assets", "Data");
    static string LooksFolder => Path.Combine(DataFolder, "data", "looks");

    /// <summary>One of the old game's looks: its race, sex, place among the sex's four, label and spec (every field the old rig drew).</summary>
    internal sealed record OldLook(string Race, string Sex, int Index, string Label, JsonElement Spec)
    {
        /// <summary>Its preset's name: <c>&lt;race&gt;-&lt;sex&gt;-&lt;n&gt;</c>, n from 1 in the old order.</summary>
        public string Name => $"{Race}-{Sex}-{Index + 1}";

        /// <summary>Its place in its race's list: the male looks 10–40, then the female 50–80 (the old dialog's MALE, then FEMALE; Q43's way of writing an order).</summary>
        public int Order => (Sex == LookNames.Male ? 0 : 40) + (Index + 1) * 10;
    }

    /// <summary>The catalogue's 72 looks, in its order (its races, male then female, each sex's four).</summary>
    internal static IReadOnlyList<OldLook> Catalogue { get; } =
        [.. OldGameReference.Load("look-catalogue").Cases.Select(c => new OldLook(c.Input.GetProperty("race").GetString()!, c.Input.GetProperty("sex").GetString()!,
            c.Input.GetProperty("index").GetInt32(), c.Output.GetProperty("label").GetString()!, c.Output.GetProperty("spec")))];

    /// <summary>
    /// The preset an old look is once its average body (<paramref name="body"/>) is in the data: its label and order, the
    /// race, the sex and that body; its parts and colours those of <paramref name="kept"/> (the committed preset's look,
    /// which the steps that make the parts and swatches fill: GM2 steps 4 and 9), none otherwise.
    /// </summary>
    internal static LookPreset Preset(OldLook old, string body, LookHero? kept) => new()
    {
        Label = old.Label,
        Order = old.Order,
        Look = new LookHero
        {
            Race = old.Race, Sex = old.Sex, Body = body,
            Parts = kept?.Parts ?? new Dictionary<string, string>(), Colours = kept?.Colours ?? new Dictionary<string, string>(),
        },
    };

    /// <summary>
    /// What the writer makes of the models <paramref name="models"/> (the data's model paths), the races
    /// <paramref name="races"/> and the committed bodies and presets: by path under <c>data/looks/</c>, each record's JSON
    /// form. The bodies: the committed ones as they are, and one for each model <c>models/humanoid/bodies/&lt;race&gt;-&lt;sex&gt;-&lt;build&gt;</c>
    /// whose race is one of <paramref name="races"/>, sex one of the two and build one of the three (step 4a's names; a body
    /// named otherwise is written by hand). The presets: one for each old look whose race and sex have an average body.
    /// </summary>
    internal static IReadOnlyDictionary<string, byte[]> Written(IEnumerable<string> models, IReadOnlySet<string> races,
        IReadOnlyDictionary<string, LookBody> bodies, IReadOnlyDictionary<string, LookPreset> presets)
    {
        const string BodyModels = "models/humanoid/bodies/";
        var all = new SortedDictionary<string, LookBody>(bodies.ToDictionary(), StringComparer.Ordinal);
        foreach (var model in models.Where(m => m.StartsWith(BodyModels, StringComparison.Ordinal)))
        {
            var name = model[BodyModels.Length..];
            var parts = name.Split('-');
            if (parts.Length < 3 || name.Contains('/'))
                continue;
            var (race, sex, build) = (string.Join('-', parts[..^2]), parts[^2], parts[^1]);
            if (races.Contains(race) && LookNames.Sexes.Contains(sex) && LookNames.Builds.Contains(build))
                all.TryAdd(name, new LookBody { Race = race, Sex = sex, Build = build, Model = model });
        }
        var written = new SortedDictionary<string, byte[]>(StringComparer.Ordinal);
        foreach (var (name, body) in all)
            written[$"bodies/{name}.json"] = RecordSerializer.ToJsonUtf8(typeof(LookBody), body);
        foreach (var old in Catalogue)
        {
            var body = all.FirstOrDefault(b => b.Value.Race == old.Race && b.Value.Sex == old.Sex && b.Value.Build == LookNames.Average).Key;
            if (body is not null)
                written[$"presets/{old.Name}.json"] = RecordSerializer.ToJsonUtf8(typeof(LookPreset), Preset(old, body, presets.GetValueOrDefault(old.Name)?.Look));
        }
        return written;
    }

    /// <summary>The records of the committed folder <c>Assets/Data/data/looks/&lt;folder&gt;/</c>, by name.</summary>
    static IReadOnlyDictionary<string, T> Committed<T>(string folder)
    {
        var path = Path.Combine(LooksFolder, folder);
        return Directory.Exists(path)
            ? Directory.GetFiles(path, "*.json").ToDictionary(f => Path.GetFileNameWithoutExtension(f), f => RecordSerializer.FromJson<T>(File.ReadAllBytes(f)), StringComparer.Ordinal)
            : new Dictionary<string, T>();
    }

    static IReadOnlyList<string> CommittedModels()
    {
        var pak = Path.Combine(TestPaths.Packs, "models.pak");
        if (!File.Exists(pak))
            return [];
        using var pack = PackFile.Open(pak);
        return [.. pack.Entries.Select(e => e.Path)];
    }

    /// <summary>The 72 looks: nine races of the data's playable ones, four per sex, each a name, a label and a place of its own.</summary>
    [Fact]
    public void TheCatalogueIsTheOldGames72Looks()
    {
        Assert.Equal(72, Catalogue.Count);
        string[] races = ["human", "elf", "dark-elf", "dwarf", "hobbit", "lizardman", "half-beast", "merfolk", "goliath"];
        Assert.Equal(races, Catalogue.Select(l => l.Race).Distinct());
        Assert.Equal(races.Order(), RaceOracle.FileRaces.Where(r => r.Race.Playable).Select(r => r.Id).Order());
        foreach (var race in races)
        {
            var looks = Catalogue.Where(l => l.Race == race).ToList();
            Assert.Equal(["male", "male", "male", "male", "female", "female", "female", "female"], looks.Select(l => l.Sex));
            Assert.Equal([0, 1, 2, 3, 0, 1, 2, 3], looks.Select(l => l.Index));
            Assert.Equal([10, 20, 30, 40, 50, 60, 70, 80], looks.Select(l => l.Order));
            Assert.Equal(8, looks.Select(l => l.Label).Distinct().Count());
        }
        Assert.Equal(72, Catalogue.Select(l => l.Name).Distinct().Count());
        Assert.All(Catalogue, l => Assert.False(string.IsNullOrWhiteSpace(l.Label)));
        // The old dialog's labels (Hero.md, "The presets"; the design's inventory), two races in full.
        Assert.Equal(["Wayfarer", "Sellsword", "Smith", "Courtier", "Ranger", "Duelist", "Healer", "Scholar"], Catalogue.Where(l => l.Race == "human").Select(l => l.Label));
        Assert.Equal(["Greyhowl", "Emberbrush", "Bramblehide", "Tuftear", "Moonfang", "Russetail", "Honeypaw", "Snowpelt"], Catalogue.Where(l => l.Race == "half-beast").Select(l => l.Label));
        Assert.Equal(["human-male-1", "human-male-2"], Catalogue.Take(2).Select(l => l.Name));
        Assert.Equal("goliath-female-4", Catalogue[^1].Name);
        // The spec is the old game's, every field: the race, the sex, the label, the hair …
        var russetail = Catalogue.Single(l => l.Race == "half-beast" && l.Label == "Russetail").Spec;
        Assert.Equal(("half-beast", "female", "Russetail", "braid", "wolf"), (russetail.GetProperty("race").GetString(), russetail.GetProperty("sex").GetString(),
            russetail.GetProperty("label").GetString(), russetail.GetProperty("hair").GetProperty("style").GetString(), russetail.GetProperty("tail").GetProperty("style").GetString()));
    }

    /// <summary>A preset made of an old look: its label, order, race, sex and average body; its parts and colours kept from the committed one, none before.</summary>
    [Fact]
    public void APresetIsItsOldLook()
    {
        var scholar = Catalogue.Single(l => l.Label == "Scholar");
        var preset = Preset(scholar, "human-female-average", null);
        Assert.Equal(new LookPreset { Label = "Scholar", Order = 80, Look = new LookHero { Race = "human", Sex = "female", Body = "human-female-average" } }, preset);
        Assert.Equal("human-female-4", scholar.Name);
        var kept = preset.Look with { Parts = new Dictionary<string, string> { ["hair"] = "braid" }, Colours = new Dictionary<string, string> { ["skin"] = "Fair" }, Preset = "x" };
        Assert.Equal(preset with { Look = preset.Look with { Parts = kept.Parts, Colours = kept.Colours } }, Preset(scholar, "human-female-average", kept));
    }

    /// <summary>The writer: bodies for the models named as step 4a names them, presets for the looks whose average body is there, the committed ones' parts and colours kept.</summary>
    [Fact]
    public void TheWriterWritesBodiesForTheModelsAndPresetsForTheirBodies()
    {
        string[] models =
        [
            "models/humanoid/bodies/human-male-average", "models/humanoid/bodies/human-female-average", "models/humanoid/bodies/dark-elf-male-strong",
            "models/humanoid/bodies/dark-elf-male-average", "models/humanoid/bodies/elf-male-average", "models/humanoid/bodies/human-other-average",
            "models/humanoid/bodies/human-male-tall", "models/humanoid/bodies/odd", "models/humanoid/bodies/deeper/human-male-slender",
            "models/humanoid/parts/hair/braid/human-male-average", "models/humanoid/clips",
        ];
        var kept = new LookPreset { Label = "old label", Order = 1, Look = new LookHero { Race = "human", Body = "x", Parts = new Dictionary<string, string> { ["hair"] = "braid" }, Colours = new Dictionary<string, string> { ["skin"] = "Fair" } } };
        var handMade = new LookBody { Race = "dwarf", Sex = "female", Build = "average", Model = "models/other/dwarf-woman" };
        var written = Written(models, new HashSet<string>(["human", "dark-elf", "dwarf"]),
            new Dictionary<string, LookBody> { ["dwarf-woman"] = handMade }, new Dictionary<string, LookPreset> { ["human-male-1"] = kept });
        Assert.Equal(["bodies/dark-elf-male-average.json", "bodies/dark-elf-male-strong.json", "bodies/dwarf-woman.json", "bodies/human-female-average.json", "bodies/human-male-average.json",
            .. new[] { "dark-elf-male", "dwarf-female", "human-female", "human-male" }.SelectMany(s => Enumerable.Range(1, 4).Select(n => $"presets/{s}-{n}.json"))], written.Keys);
        Assert.Equal(new LookBody { Race = "dark-elf", Sex = "male", Build = "strong", Model = "models/humanoid/bodies/dark-elf-male-strong" },
            RecordSerializer.FromJson<LookBody>(written["bodies/dark-elf-male-strong.json"]));
        Assert.Equal(handMade, RecordSerializer.FromJson<LookBody>(written["bodies/dwarf-woman.json"]));
        Assert.Equal(Preset(Catalogue[0], "human-male-average", kept.Look), RecordSerializer.FromJson<LookPreset>(written["presets/human-male-1.json"]));
        Assert.Equal(Preset(Catalogue[1], "human-male-average", null), RecordSerializer.FromJson<LookPreset>(written["presets/human-male-2.json"]));
        Assert.Equal(Preset(Catalogue.Single(l => l.Name == "dwarf-female-2"), "dwarf-woman", null), RecordSerializer.FromJson<LookPreset>(written["presets/dwarf-female-2.json"]));
        Assert.True(RecordSerializer.ToJsonUtf8(typeof(LookPreset), Preset(Catalogue[4], "human-female-average", null)).AsSpan().SequenceEqual(written["presets/human-female-1.json"]));
        // Nothing to write: no models, no presets.
        Assert.Empty(Written([], new HashSet<string>(["human"]), new Dictionary<string, LookBody>(), new Dictionary<string, LookPreset>()));
    }

    /// <summary>
    /// The committed maker's data (none yet: GM2-P18) is what the writer makes of it: each body as written, each preset the
    /// catalogue's look for its name with the average body of its race and sex. With <c>STATVENTURE_LOOK_DATA=write</c> the
    /// writer's whole output is written first (bodies for the models in models.pak, presets for their bodies).
    /// </summary>
    [Fact]
    public void TheCommittedLooksAreTheWritersOutput()
    {
        var races = new HashSet<string>(Directory.GetFiles(Path.Combine(DataFolder, "data", "races"), "*.json").Select(f => Path.GetFileNameWithoutExtension(f)), StringComparer.Ordinal);
        var bodies = Committed<LookBody>("bodies");
        var presets = Committed<LookPreset>("presets");
        var written = Written(CommittedModels(), races, bodies, presets);
        if (Environment.GetEnvironmentVariable(WriteVariable) == "write")
        {
            foreach (var (path, bytes) in written)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(LooksFolder, path))!);
                File.WriteAllBytes(Path.Combine(LooksFolder, path), bytes);
            }
            (bodies, presets) = (Committed<LookBody>("bodies"), Committed<LookPreset>("presets"));
            written = Written(CommittedModels(), races, bodies, presets);
        }
        var names = Catalogue.Select(l => l.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var name in presets.Keys)
            Assert.True(names.Contains(name), $"Assets/Data/data/looks/presets/{name}.json isn't one of the old game's looks (the game's presets are the catalogue's 72; a mod adds its own)");
        foreach (var folder in new[] { "bodies", "presets" })
            foreach (var file in Directory.Exists(Path.Combine(LooksFolder, folder)) ? Directory.GetFiles(Path.Combine(LooksFolder, folder)) : [])
            {
                var path = $"{folder}/{Path.GetFileName(file)}";
                Assert.True(written.TryGetValue(path, out var bytes) && bytes.AsSpan().SequenceEqual(File.ReadAllBytes(file)),
                    $"Assets/Data/data/looks/{path} isn't what the writer makes of it: run the game's tests with {WriteVariable}=write (LookDataTests), then Tools/Scripts/make-data-pack.sh");
            }
    }

    /// <summary>Assets/Data holds the races and the maker's records only, each kind in its folder, and the game's style (GM2 step 5, GameStyleTests).</summary>
    [Fact]
    public void AssetsDataHoldsTheRacesAndTheMakersRecords()
    {
        foreach (var file in Directory.GetFiles(DataFolder, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(DataFolder, f).Replace('\\', '/')))
            Assert.True(file.StartsWith("data/races/", StringComparison.Ordinal) || file.StartsWith("data/looks/bodies/", StringComparison.Ordinal)
                || file.StartsWith("data/looks/parts/", StringComparison.Ordinal) || file.StartsWith("data/looks/presets/", StringComparison.Ordinal)
                || file == "render/styles/game.json", file);
    }

    /// <summary>
    /// The maker's records the game ships (in data.pak, over the game's packs as the game mounts them: its models from
    /// models.pak) pass every check with the data and its references, a body's or part's model among them (GM2-P18): a
    /// shipped entry with a problem would leave out every mod (the mod check checks the game's records under a mod too).
    /// </summary>
    [Fact]
    public void TheShippedLooksPassTheirChecks()
    {
        using var folder = new ScratchFolder();
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        using var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], _ => null));
        var lookup = TestLooks.Lookup(data);
        var looks = LookData.Load(data);
        foreach (var path in data.List("data/looks"))
            Assert.True(RecordChecks.Check(data.Load(Mods.GameMods.GameKinds, path), data, lookup) is [], $"{path}: {string.Join("; ", RecordChecks.Check(data.Load(Mods.GameMods.GameKinds, path), data, lookup))}");
        Assert.Equal(data.List("data/looks").Count, looks.Bodies.Count + looks.Parts.Count + looks.Presets.Count);
        // And the check finds a shipped body whose model isn't there.
        using var looksData = new TestLooks($"models/humanoid/bodies/{TestLooks.HumanMale}");
        Assert.Contains("no model \"models/humanoid/bodies/human-male-average\" (models/humanoid/bodies/human-male-average) (at $.model)", looksData.Problems(TestLooks.Bodies[TestLooks.HumanMale]));
    }
}
