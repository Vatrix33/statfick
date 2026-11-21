using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;

namespace Statventure.Game.Tests;

/// <summary>
/// The race carousel's and the character maker's test data (GM2 step 8): the game's 28 races from the committed
/// <c>data.pak</c> without its own looks (step 5's human bodies, parts and presets left out: <see cref="WithoutLooks"/>,
/// so the tests and fixtures don't move as steps 9 and 9a fill the real data), and over them, in a scratch folder mounted
/// on top, swatches for three races and the maker's records, every name reading as test data ("Test …"). The human has all six bodies (two sexes, three builds),
/// eight presets (four a sex, the old labels after "Test"), six categories (a required face, brows and hair; a beard for
/// men with a "none"; marks and a head piece each with a "none") and four colour slots; the Half-Beast has the ears and
/// the tail (required, no "none") and the lizardman the crest, so every category of the maker is somewhere; the dwarf
/// has one body (a man's, average) and no preset. Models: empty <c>model</c> records at the paths the bodies and parts
/// name (the checks ask only that one is there; GM2-P18); the stand-in figure the approval captures draw for a body is the
/// engine's test figure (<c>figure.glb</c>, <c>stocky.glb</c> for the strong builds).
/// </summary>
sealed class HeroTestData : IDisposable
{
    public const string Human = "human", Dwarf = "dwarf", HalfBeast = "half-beast", Lizardman = "lizardman";

    /// <summary>The models the bodies and parts name: the engine's test figure in two proportions.</summary>
    public const string Figure = "models/test/figure", Stocky = "models/test/stocky";

    readonly string _folder = Directory.CreateTempSubdirectory("statventure-hero-").FullName;
    readonly string _pack;
    VirtualFileSystem? _data;

    /// <summary>
    /// The test data over the committed <paramref name="dataPack"/> (the game's <c>data.pak</c>); <paramref name="races"/>
    /// gives a race of the pack's other swatches than <see cref="Swatches"/> (a test's own case).
    /// </summary>
    public HeroTestData(string dataPack, IReadOnlyDictionary<string, IReadOnlyList<RaceSwatch>>? races = null)
    {
        _pack = dataPack;
        using (var real = new VirtualFileSystem([Mount.Pack(dataPack)]))
            foreach (var (id, swatches) in races is null ? Swatches : Swatches.Where(s => !races.ContainsKey(s.Key)).Concat(races))
                Write(RaceCatalogue.PathOf(id), real.Load<Race>(RaceCatalogue.PathOf(id)) with { Swatches = swatches });
        // The bodies' models draw in the skin and the cloth (the maker offers a colour the body or a chosen part uses, GM2-P29).
        foreach (var model in new[] { Figure, Stocky })
            Write(model, new ModelData { Nodes = [], Meshes = [], Materials = [.. BodySlots.Select(s => new ModelMaterial(s, System.Numerics.Vector4.One, false))], Skins = [], Roots = [] });
        foreach (var (name, body) in Bodies)
            Write($"{LookNames.BodiesFolder}/{name}", body);
        foreach (var (name, part) in Parts)
            Write($"{LookNames.PartsFolder}/{name}", part);
        foreach (var (name, preset) in Presets)
            Write($"{LookNames.PresetsFolder}/{name}", preset);
    }

    /// <summary>The game's data with the test data on top.</summary>
    public VirtualFileSystem Data => _data ??= new VirtualFileSystem([Mount.Custom("data.pak without its looks", _withoutLooks ??= new WithoutLooks(_pack)), Mount.Folder(_folder)]);

    WithoutLooks? _withoutLooks;

    /// <summary>A pack's entries but the maker's records (<c>data/looks/</c>).</summary>
    sealed class WithoutLooks(string pack) : IMountEntries, IDisposable
    {
        readonly PackFile _pack = PackFile.Open(pack);

        public IEnumerable<string> Paths => _pack.Entries.Select(e => e.Path).Where(path => !path.StartsWith("data/looks/", StringComparison.Ordinal));

        public bool IsRecord(string path) => _pack.Find(path)!.IsRecord;

        public byte[] Read(string path) => _pack.Read(_pack.Find(path)!);

        public void Dispose() => _pack.Dispose();
    }

    /// <summary>The palette slots the stand-in models' materials are in: a body's model is what draws the skin and the cloth (GM2-P29: the maker offers those colours on every body).</summary>
    public static readonly string[] BodySlots = ["skin", "cloth"];

    /// <summary>The model file the approval captures draw for a model path (the engine's test figure).</summary>
    public static string ModelFile(string engine, string model) =>
        Path.Combine(engine, "Tests", "Statventure.Tests", "Data", "Models", model == Stocky ? "stocky.glb" : "figure.glb");

    static RaceSwatch S(string slot, string name, string colour) => new() { Slot = slot, Name = name, Colour = colour };

    /// <summary>The swatches given to three races (their other fields the committed race's).</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<RaceSwatch>> Swatches { get; } = new Dictionary<string, IReadOnlyList<RaceSwatch>>
    {
        [Human] =
        [
            S("skin", "Test fair", "#e8c3a0"), S("skin", "Test olive", "#c49a6c"), S("skin", "Test umber", "#8a5a3c"), S("skin", "Test deep", "#5a3a28"),
            S("hair", "Test ash", "#9a978c"), S("hair", "Test raven", "#211b18"), S("hair", "Test chestnut", "#6b3a1e"), S("hair", "Test copper", "#b0562a"), S("hair", "Test flaxen", "#d6bd7a"),
            S("eyes", "Test brown", "#5a3a20"), S("eyes", "Test green", "#4a7a4a"), S("eyes", "Test grey", "#7d8a90"),
            S("cloth", "Test madder", "#8e2a20"), S("cloth", "Test woad", "#2b4a62"), S("cloth", "Test moss", "#556b2f"), S("cloth", "Test undyed", "#cdb88c"),
        ],
        [HalfBeast] = [S("skin", "Test tawny", "#c08a5a"), S("hair", "Test russet", "#9a4a22"), S("hair", "Test grey", "#8a8a84")],
        // The crest's first: the maker shows the slots in the slots' order, not the race's.
        [Lizardman] = [S("crest", "Test ochre", "#c08a2a"), S("skin", "Test jade", "#4e7a52"), S("skin", "Test sand", "#c2a46a")],
    };

    static LookBody Body(string race, string sex, string build) => new() { Race = race, Sex = sex, Build = build, Model = build == "strong" ? Stocky : Figure };

    /// <summary>The bodies: the human's six, the dwarf's man's average, the Half-Beast's two average and the man's slender, the lizardman's average two, an orc's.</summary>
    public static IReadOnlyDictionary<string, LookBody> Bodies { get; } = new Dictionary<string, LookBody>
    {
        ["human-male-slender"] = Body(Human, LookNames.Male, "slender"),
        ["human-male-average"] = Body(Human, LookNames.Male, "average"),
        ["human-male-strong"] = Body(Human, LookNames.Male, "strong"),
        ["human-female-slender"] = Body(Human, LookNames.Female, "slender"),
        ["human-female-average"] = Body(Human, LookNames.Female, "average"),
        ["human-female-strong"] = Body(Human, LookNames.Female, "strong"),
        ["dwarf-male-average"] = Body(Dwarf, LookNames.Male, "average"),
        ["half-beast-male-slender"] = Body(HalfBeast, LookNames.Male, "slender"),
        ["half-beast-male-average"] = Body(HalfBeast, LookNames.Male, "average"),
        ["half-beast-female-average"] = Body(HalfBeast, LookNames.Female, "average"),
        ["lizardman-male-average"] = Body(Lizardman, LookNames.Male, "average"),
        ["lizardman-female-average"] = Body(Lizardman, LookNames.Female, "average"),
        // An enemy-only race with a body (GM9's enemies may have them): its hero still needs no look (Q27).
        ["orc-male-average"] = Body("orc", LookNames.Male, "average"),
    };

    /// <summary>The bodies of <paramref name="race"/> (and of <paramref name="sex"/> when given).</summary>
    static IReadOnlyList<string> BodiesOf(string race, string? sex = null) =>
        [.. Bodies.Where(b => b.Value.Race == race && (sex is null || b.Value.Sex == sex)).Select(b => b.Key)];

    /// <summary>A part with one model fitted to <paramref name="bodies"/>, or none (an option drawn as nothing) when it is empty.</summary>
    static LookPart Part(string category, string name, string[] races, IReadOnlyList<string> bodies, string[]? sexes = null, string[]? slots = null) => new()
    {
        Category = category, Name = name, Races = races, Sexes = sexes ?? [LookNames.Male, LookNames.Female], Slots = slots ?? [],
        Models = bodies.Count == 0 ? [] : [new LookPartModel { Model = Figure, Bodies = bodies }],
    };

    static readonly string[] Men = [LookNames.Male], Women = [LookNames.Female];

    /// <summary>The parts, by name.</summary>
    public static IReadOnlyDictionary<string, LookPart> Parts { get; } = new Dictionary<string, LookPart>
    {
        // The human: a required face, brows and hair; a beard for men with "none"; marks and a head piece with "none".
        ["test-face-1"] = Part("face", "Test face 1", [Human], BodiesOf(Human), slots: ["skin", "eyes"]),
        ["test-face-2"] = Part("face", "Test face 2", [Human], BodiesOf(Human), slots: ["skin", "eyes"]),
        ["test-face-3"] = Part("face", "Test face 3", [Human], BodiesOf(Human), slots: ["skin", "eyes"]),
        ["test-face-4"] = Part("face", "Test face 4", [Human], BodiesOf(Human), slots: ["skin", "eyes"]),
        ["test-brows-straight"] = Part("brows", "Test straight", [Human], BodiesOf(Human), slots: ["hair"]),
        ["test-brows-arched"] = Part("brows", "Test arched", [Human], BodiesOf(Human), slots: ["hair"]),
        ["test-hair-bald"] = Part("hair", "Test bald", [Human], [], sexes: Men),
        ["test-hair-braid"] = Part("hair", "Test braid", [Human, HalfBeast], [.. BodiesOf(Human), .. BodiesOf(HalfBeast)], slots: ["hair"]),
        ["test-hair-crop"] = Part("hair", "Test crop", [Human, HalfBeast], [.. BodiesOf(Human), .. BodiesOf(HalfBeast)], slots: ["hair"]),
        ["test-hair-long"] = Part("hair", "Test long", [Human], BodiesOf(Human), slots: ["hair"]),
        ["test-hair-topknot"] = Part("hair", "Test topknot", [Human], BodiesOf(Human), slots: ["hair"]),
        ["test-hair-curls"] = Part("hair", "Test curls", [Human], BodiesOf(Human, LookNames.Female), sexes: Women, slots: ["hair"]),
        ["test-beard-none"] = Part("beard", "Test clean-shaven", [Human, Dwarf], [], sexes: Men),
        ["test-beard-stubble"] = Part("beard", "Test stubble", [Human], BodiesOf(Human, LookNames.Male), sexes: Men, slots: ["hair"]),
        ["test-beard-full"] = Part("beard", "Test full", [Human, Dwarf], [.. BodiesOf(Human, LookNames.Male), .. BodiesOf(Dwarf)], sexes: Men, slots: ["hair"]),
        ["test-beard-forked"] = Part("beard", "Test forked", [Human], BodiesOf(Human, LookNames.Male), sexes: Men, slots: ["hair"]),
        ["test-marks-none"] = Part("marks", "Test none", [Human, Lizardman], []),
        ["test-marks-freckles"] = Part("marks", "Test freckles", [Human], BodiesOf(Human), slots: ["skin"]),
        ["test-marks-scar"] = Part("marks", "Test scar", [Human], BodiesOf(Human), slots: ["skin"]),
        ["test-head-none"] = Part("head-piece", "Test none", [Human], []),
        ["test-head-circlet"] = Part("head-piece", "Test circlet", [Human], BodiesOf(Human), slots: ["cloth"]),
        ["test-head-flower"] = Part("head-piece", "Test flower", [Human], BodiesOf(Human, LookNames.Female), sexes: Women, slots: ["cloth"]),
        // The Half-Beast: ears and a tail, required (no "none"); the lizardman: a crest and a tail.
        ["test-face-beast"] = Part("face", "Test beast face", [HalfBeast], BodiesOf(HalfBeast), slots: ["skin"]),
        ["test-ears-pointed"] = Part("ears", "Test pointed", [HalfBeast], BodiesOf(HalfBeast), slots: ["hair"]),
        ["test-ears-round"] = Part("ears", "Test round", [HalfBeast], BodiesOf(HalfBeast), slots: ["hair"]),
        ["test-tail-bushy"] = Part("tail", "Test bushy", [HalfBeast], BodiesOf(HalfBeast), slots: ["hair"]),
        ["test-tail-tufted"] = Part("tail", "Test tufted", [HalfBeast], BodiesOf(HalfBeast), slots: ["hair"]),
        ["test-face-lizard"] = Part("face", "Test lizard face", [Lizardman], BodiesOf(Lizardman), slots: ["skin"]),
        ["test-crest-spines"] = Part("crest", "Test spines", [Lizardman], BodiesOf(Lizardman), slots: ["crest"]),
        ["test-crest-frill"] = Part("crest", "Test frill", [Lizardman], BodiesOf(Lizardman), slots: ["crest"]),
        ["test-tail-lizard"] = Part("tail", "Test lizard tail", [Lizardman], BodiesOf(Lizardman), slots: ["skin"]),
    };

    static LookPreset Preset(string label, int order, string sex, string build, string face, string brows, string hair, string? beard, string marks, string head,
        string skin, string hairColour, string eyes, string cloth)
    {
        var parts = new Dictionary<string, string> { ["face"] = face, ["brows"] = brows, ["hair"] = hair, ["marks"] = marks, ["head-piece"] = head };
        if (beard is not null)
            parts["beard"] = beard;
        return new LookPreset
        {
            Label = label,
            Order = order,
            Look = new LookHero
            {
                Race = Human, Sex = sex, Body = $"human-{sex}-{build}", Parts = parts,
                Colours = new Dictionary<string, string> { ["skin"] = skin, ["hair"] = hairColour, ["eyes"] = eyes, ["cloth"] = cloth },
            },
        };
    }

    /// <summary>The human's eight presets (the old labels after "Test", the men 10–40, the women 50–80, Q43's way).</summary>
    public static IReadOnlyDictionary<string, LookPreset> Presets { get; } = new Dictionary<string, LookPreset>
    {
        ["test-human-male-1"] = Preset("Test Wayfarer", 10, LookNames.Male, "average", "test-face-1", "test-brows-straight", "test-hair-crop", "test-beard-none", "test-marks-none", "test-head-none", "Test fair", "Test chestnut", "Test brown", "Test moss"),
        ["test-human-male-2"] = Preset("Test Sellsword", 20, LookNames.Male, "strong", "test-face-2", "test-brows-straight", "test-hair-topknot", "test-beard-stubble", "test-marks-scar", "test-head-none", "Test olive", "Test raven", "Test grey", "Test woad"),
        ["test-human-male-3"] = Preset("Test Smith", 30, LookNames.Male, "strong", "test-face-3", "test-brows-arched", "test-hair-bald", "test-beard-full", "test-marks-none", "test-head-none", "Test umber", "Test raven", "Test brown", "Test madder"),
        ["test-human-male-4"] = Preset("Test Courtier", 40, LookNames.Male, "slender", "test-face-4", "test-brows-arched", "test-hair-long", "test-beard-forked", "test-marks-none", "test-head-circlet", "Test fair", "Test flaxen", "Test green", "Test undyed"),
        ["test-human-female-1"] = Preset("Test Ranger", 50, LookNames.Female, "average", "test-face-1", "test-brows-straight", "test-hair-braid", null, "test-marks-freckles", "test-head-none", "Test fair", "Test copper", "Test green", "Test moss"),
        ["test-human-female-2"] = Preset("Test Duelist", 60, LookNames.Female, "slender", "test-face-2", "test-brows-arched", "test-hair-topknot", null, "test-marks-none", "test-head-none", "Test olive", "Test raven", "Test brown", "Test madder"),
        ["test-human-female-3"] = Preset("Test Healer", 70, LookNames.Female, "average", "test-face-3", "test-brows-straight", "test-hair-curls", null, "test-marks-none", "test-head-flower", "Test deep", "Test raven", "Test brown", "Test undyed"),
        ["test-human-female-4"] = Preset("Test Scholar", 80, LookNames.Female, "strong", "test-face-4", "test-brows-arched", "test-hair-long", null, "test-marks-scar", "test-head-circlet", "Test fair", "Test ash", "Test grey", "Test woad"),
    };

    void Write(string path, object record)
    {
        var file = Path.Combine(_folder, path + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, RecordSerializer.ToJson(record.GetType(), record));
    }

    public void Dispose()
    {
        _data?.Dispose();
        _withoutLooks?.Dispose();
        try
        {
            Directory.Delete(_folder, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
