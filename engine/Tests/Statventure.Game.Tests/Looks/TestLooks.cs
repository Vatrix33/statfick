using System.Reflection;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Mods;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;

namespace Statventure.Game.Tests;

/// <summary>
/// The maker's records as the tests' own data (GM2 step 6; GM2-P18: nothing in a shipped pack names a model before its
/// file is there, so the tests make their own): two races with swatches, stand-in models (an empty <c>model</c> record at
/// each path a body or part names; the checks know only "a model that is there"), four bodies, parts of four categories
/// and a preset, in a scratch folder mounted as the game's data, with the references' lookup over it.
/// </summary>
sealed class TestLooks : IDisposable
{
    readonly ScratchFolder _folder = new();
    VirtualFileSystem? _data;

    public const string Human = "human";
    public const string Dwarf = "dwarf";
    public const string HumanMale = "human-male-average";
    public const string HumanMaleStrong = "human-male-strong";
    public const string HumanFemale = "human-female-average";
    public const string DwarfMale = "dwarf-male-average";

    /// <summary>The preset's look, which every check passes: the human man's average body, his face, the braid, no beard, fair skin and ash hair.</summary>
    public static LookHero WayfarerLook { get; } = new()
    {
        Race = Human,
        Sex = LookNames.Male,
        Body = HumanMale,
        Parts = new Dictionary<string, string> { ["face"] = "face-human-male-1", ["hair"] = "hair-braid", ["beard"] = "beard-none" },
        Colours = new Dictionary<string, string> { ["skin"] = "Fair", ["hair"] = "Ash" },
    };

    public static IReadOnlyDictionary<string, Race> Races { get; } = new Dictionary<string, Race>
    {
        [Human] = TestRaces.Good("Human") with
        {
            Swatches = [Swatch("skin", "Fair", "#f0c8a0"), Swatch("skin", "Umber", "#8a5a3c"), Swatch("hair", "Ash", "#8a8a8a"), Swatch("hair", "Raven", "#1f1a17")],
        },
        [Dwarf] = TestRaces.Good("Dwarf") with { Swatches = [Swatch("skin", "Ruddy", "#d49a74")] },
    };

    public static IReadOnlyDictionary<string, LookBody> Bodies { get; } = new Dictionary<string, LookBody>
    {
        [HumanMale] = Body(Human, LookNames.Male, "average"),
        [HumanMaleStrong] = Body(Human, LookNames.Male, "strong"),
        [HumanFemale] = Body(Human, LookNames.Female, "average"),
        [DwarfMale] = Body(Dwarf, LookNames.Male, "average"),
    };

    public static IReadOnlyDictionary<string, LookPart> Parts { get; } = new Dictionary<string, LookPart>
    {
        // A face for the human man, one model fitted to his two bodies.
        ["face-human-male-1"] = new() { Category = "face", Name = "Steady", Races = [Human], Sexes = [LookNames.Male], Slots = ["skin", "eyes"],
            Models = [new() { Model = "models/humanoid/parts/face/human-male-1/human-male", Bodies = [HumanMale, HumanMaleStrong] }] },
        // A braid for both sexes of the human, fitted to the average bodies only.
        ["hair-braid"] = new() { Category = "hair", Name = "Braid", Races = [Human], Slots = ["hair", "hair.tie"],
            Models = [new() { Model = "models/humanoid/parts/hair/braid/human-male", Bodies = [HumanMale] }, new() { Model = "models/humanoid/parts/hair/braid/human-female", Bodies = [HumanFemale] }] },
        // Options drawn as nothing: offered on every body of their races and sexes.
        ["hair-bald"] = new() { Category = "hair", Name = "Bald", Races = [Human, Dwarf], Sexes = [LookNames.Male] },
        ["beard-none"] = new() { Category = "beard", Name = "Clean-shaven", Races = [Human, Dwarf], Sexes = [LookNames.Male] },
        ["beard-full"] = new() { Category = "beard", Name = "Full", Races = [Dwarf], Sexes = [LookNames.Male], Slots = ["hair"],
            Models = [new() { Model = "models/humanoid/parts/beard/full/dwarf-male", Bodies = [DwarfMale] }] },
    };

    /// <summary>
    /// Parts by face (the user's Q65, GM2 step 9a), written over the data only by the tests that ask
    /// (<see cref="WriteByFace"/>): a second face for the human man's average body, a scar made for each of his two faces
    /// (the first face's model fitted to his two bodies, the second's to the average one; each model names its face), and
    /// marks' "none".
    /// </summary>
    public static IReadOnlyDictionary<string, LookPart> ByFace { get; } = new Dictionary<string, LookPart>
    {
        ["face-human-male-2"] = new() { Category = "face", Name = "Weathered", Races = [Human], Sexes = [LookNames.Male], Slots = ["skin", "eyes"],
            Models = [new() { Model = "models/humanoid/parts/face/human-male-2/human-male", Bodies = [HumanMale] }] },
        ["marks-scar"] = new() { Category = "marks", Name = "Scar", Races = [Human], Sexes = [LookNames.Male], Slots = ["marking"], Order = 10,
            Models =
            [
                new() { Model = "models/humanoid/parts/marks/scar/human-male-1/human-male", Bodies = [HumanMale, HumanMaleStrong], Faces = ["face-human-male-1"] },
                new() { Model = "models/humanoid/parts/marks/scar/human-male-2/human-male", Bodies = [HumanMale], Faces = ["face-human-male-2"] },
            ] },
        ["marks-none"] = new() { Category = "marks", Name = "None", Races = [Human], Sexes = [LookNames.Male] },
    };

    /// <summary>The parts by face (<see cref="ByFace"/>) and their stand-in models written over the data.</summary>
    public void WriteByFace()
    {
        foreach (var model in ByFace.Values.SelectMany(p => p.Models.Select(m => m.Model)))
            Write(model, StandInModel());
        foreach (var (name, part) in ByFace)
            Write($"{LookNames.PartsFolder}/{name}", part);
    }

    public static IReadOnlyDictionary<string, LookPreset> Presets { get; } = new Dictionary<string, LookPreset>
    {
        ["human-male-1"] = new() { Label = "Wayfarer", Order = 10, Look = WayfarerLook },
    };

    /// <summary>Every model path the test data names.</summary>
    public static IEnumerable<string> Models =>
        Bodies.Values.Select(b => b.Model).Concat(Parts.Values.SelectMany(p => p.Models.Select(m => m.Model))).Distinct();

    public static LookBody Body(string race, string sex, string build) => new() { Race = race, Sex = sex, Build = build, Model = $"models/humanoid/bodies/{race}-{sex}-{build}" };

    public static RaceSwatch Swatch(string slot, string name, string colour) => new() { Slot = slot, Name = name, Colour = colour };

    /// <summary>A model record standing in for a model file: the checks ask only that one is there.</summary>
    public static ModelData StandInModel() => new() { Nodes = [], Meshes = [], Materials = [], Skins = [], Roots = [] };

    /// <summary>Every test record written (the ones <paramref name="leaveOut"/> names left out), then the data mounted.</summary>
    public TestLooks(params string[] leaveOut)
    {
        foreach (var (id, race) in Races)
            Write(RaceCatalogue.PathOf(id), race);
        foreach (var model in Models)
            Write(model, StandInModel());
        foreach (var (name, body) in Bodies)
            Write($"{LookNames.BodiesFolder}/{name}", body);
        foreach (var (name, part) in Parts)
            Write($"{LookNames.PartsFolder}/{name}", part);
        foreach (var (name, preset) in Presets)
            Write($"{LookNames.PresetsFolder}/{name}", preset);
        foreach (var path in leaveOut)
            Delete(path);
    }

    /// <summary>A record written (or written over) at <paramref name="path"/>; the data mounted again on its next use.</summary>
    public void Write(string path, object record)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_folder[path])!);
        File.WriteAllText(_folder[path + ".json"], RecordSerializer.ToJson(record.GetType(), record));
        _data?.Dispose();
        _data = null;
    }

    /// <summary>The record at <paramref name="path"/> removed; the data mounted again on its next use.</summary>
    public void Delete(string path)
    {
        File.Delete(_folder[path + ".json"]);
        _data?.Dispose();
        _data = null;
    }

    /// <summary>The test data as the game's data.</summary>
    public VirtualFileSystem Data => _data ??= new VirtualFileSystem([Mount.Folder(_folder.Root)]);

    /// <summary>Every problem of <paramref name="record"/> with the data and the references resolved over it, as the editor and the mod check report them.</summary>
    public IReadOnlyList<string> Problems(object record) =>
        [.. RecordChecks.Check(record, Data, Lookup(Data)).Select(p => p.ToString())];

    /// <summary>The references' lookup over <paramref name="data"/>: each record entry's kind.</summary>
    public static IReferenceLookup Lookup(VirtualFileSystem data) =>
        new ModCheck.KindLookup(data.List().Where(data.IsRecord).ToDictionary(p => p, p => KindOf(data, p), StringComparer.Ordinal));

    static string KindOf(VirtualFileSystem data, string path) =>
        data.Load(GameMods.GameKinds, path).GetType().GetCustomAttribute<RecordAttribute>()!.Name;

    public void Dispose()
    {
        _data?.Dispose();
        _folder.Dispose();
    }
}
