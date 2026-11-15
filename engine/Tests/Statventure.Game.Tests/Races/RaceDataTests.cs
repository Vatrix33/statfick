using System.Text;
using System.Text.Json;
using Statventure.Core;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Races;
using Statventure.Game.Mods;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The races' data (GM2-P4; Docs/Game/Hero.md, "How it works", step 1): the converter's output from the old game's
/// <c>objects/races.json</c> is exactly the committed JSON forms in <c>Assets/Data/data/races/</c> (written with
/// <c>STATVENTURE_RACE_DATA=write</c>), <c>Assets/Packs/data.pak</c> is exactly what the packer makes of
/// <c>Assets/Data</c> (<c>Tools/Scripts/make-data-pack.sh</c>), and the game, mounting its packs as it does, has the 28
/// races in the old game's order, each passing its checks.
/// </summary>
public class RaceDataTests
{
    /// <summary>Set to <c>write</c>: the converter's output is written into <c>Assets/Data/data/races/</c> (the folder emptied first).</summary>
    public const string WriteVariable = "STATVENTURE_RACE_DATA";

    static string DataFolder => Path.Combine(RaceOracle.Engine, "Assets", "Data");
    static string RacesFolder => Path.Combine(DataFolder, "data", "races");
    static string DataPak => Path.Combine(TestPaths.Packs, "data.pak");

    /// <summary>The old game's race order (races.json's), the nine playable first in it but the Merfolk last.</summary>
    static readonly string[] OldOrder =
    [
        "human", "elf", "dark-elf", "dwarf", "hobbit", "lizardman", "half-beast", "goliath", "goblin", "impid", "wolf", "bear",
        "griffon", "orc", "demon", "devil", "elemental", "giant", "dragon", "troll", "siren", "harpy", "satyr", "lamia", "undead",
        "treant", "dryad", "merfolk",
    ];

    [Fact]
    public void TheCommittedRacesAreRacesJsonConverted()
    {
        var forms = OldRaceFile.Forms(RaceOracle.FileRaces);
        if (Environment.GetEnvironmentVariable(WriteVariable) == "write")
        {
            if (Directory.Exists(RacesFolder))
                Directory.Delete(RacesFolder, recursive: true);
            Directory.CreateDirectory(RacesFolder);
            foreach (var (id, bytes) in forms)
                File.WriteAllBytes(Path.Combine(RacesFolder, id + ".json"), bytes);
        }
        var committed = Directory.Exists(DataFolder)
            ? Directory.GetFiles(DataFolder, "*", SearchOption.AllDirectories).Select(f => Path.GetRelativePath(DataFolder, f).Replace('\\', '/')).Order(StringComparer.Ordinal).ToList()
            : [];
        Assert.Equal(forms.Keys.Select(id => $"data/races/{id}.json").Order(StringComparer.Ordinal), committed);
        foreach (var (id, bytes) in forms)
            Assert.True(bytes.AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(RacesFolder, id + ".json"))),
                $"Assets/Data/data/races/{id}.json isn't the converter's output from races.json: run the game's tests with {WriteVariable}=write, then Tools/Scripts/make-data-pack.sh");
    }

    [Fact]
    public void TheConvertedRacesAreTheOldGamesInItsOrder()
    {
        Assert.Equal(OldOrder, RaceOracle.FileRaces.Select(r => r.Id));
        Assert.Equal(Enumerable.Range(1, 28).Select(i => i * 10), RaceOracle.FileRaces.Select(r => r.Race.Order));
        Assert.Equal(["human", "elf", "dark-elf", "dwarf", "hobbit", "lizardman", "half-beast", "goliath", "merfolk"],
            RaceOracle.FileRaces.Where(r => r.Race.Playable).Select(r => r.Id));
    }

    /// <summary>The 28 races as the old game normalized them (the oracle's <c>raceCatalog</c>), field by field.</summary>
    [Fact]
    public void EachRaceIsTheOldGamesNormalizedRace()
    {
        var old = RaceOracle.Rules.GetProperty("races").EnumerateArray().ToList();
        Assert.Equal(28, old.Count);
        foreach (var (raw, (id, race)) in old.Zip(RaceOracle.FileRaces))
            Assert.Equal(NormalizedForm(raw), NormalizedForm(id, race));
    }

    /// <summary><c>raceAccent</c> of each race and of an id it doesn't know, as the oracle measured it.</summary>
    [Fact]
    public void EachRacesAccentIsRaceAccent()
    {
        foreach (var accent in RaceOracle.Rules.GetProperty("accents").EnumerateObject())
            Assert.Equal((accent.Name, accent.Value.GetString()), (accent.Name, OldRaceFile.AccentOf(accent.Name)));
        Assert.Equal(OldOrder.Select(id => OldRaceFile.AccentOf(id)), RaceOracle.FileRaces.Select(r => r.Race.Accent));
    }

    [Fact]
    public void TheResultsAreOfTheOldGameAsItIs()
    {
        foreach (var results in new[] { RaceOracle.Fold, RaceOracle.Rules })
            foreach (var (path, then, now) in RaceOracle.Sources(results))
                Assert.True(then == now, $"old-game/{path} changed since the race references were made: run Tools/Scripts/make-old-game-references.sh race-fold race-rules");
    }

    [Fact]
    public void DataPakIsWhatThePackerMakesOfAssetsData()
    {
        using var folder = new ScratchFolder();
        var writer = new PackWriter();
        writer.AddFolder(DataFolder, GameMods.GameKinds, Importers.Find(typeof(RecordTypes).Assembly, typeof(Race).Assembly));
        writer.Write(folder["data.pak"]);
        Assert.True(File.ReadAllBytes(folder["data.pak"]).AsSpan().SequenceEqual(File.ReadAllBytes(DataPak)),
            "Assets/Packs/data.pak isn't what the packer makes of Assets/Data: run Tools/Scripts/make-data-pack.sh");
        using var pack = PackFile.Open(DataPak);
        Assert.Empty(pack.Verify());
        Assert.Equal(OldOrder.Select(RaceCatalogue.PathOf).Order(StringComparer.Ordinal), pack.Entries.Select(e => e.Path));
        Assert.All(pack.Entries, e => Assert.True(e.IsRecord, e.Path));
    }

    /// <summary>The game's packs mounted as the game mounts them (<c>packs/</c> beside it): the 28 races load, in the old order, each passing its checks.</summary>
    [Fact]
    public void TheGameHasThe28RacesFromDataPak()
    {
        using var folder = new ScratchFolder();
        Directory.CreateDirectory(folder["game/packs"]);
        foreach (var pack in Directory.GetFiles(TestPaths.Packs, "*.pak"))
            File.Copy(pack, Path.Combine(folder["game/packs"], Path.GetFileName(pack)));
        using var data = new VirtualFileSystem(Application.DataMounts(new ApplicationOptions(), folder["game"], _ => null));
        Assert.Equal(folder["game/packs/data.pak"], data.Origin(RaceCatalogue.PathOf("human")));
        var races = RaceCatalogue.Load(data);
        Assert.Equal(OldOrder, races.Select(r => r.Id));
        foreach (var (id, race) in races)
        {
            Assert.Empty(RecordChecks.Check(race, data));
            Assert.Equal(RaceOracle.FileRaces.Single(r => r.Id == id).Race with { }, race, RaceComparer.Instance);
        }
    }

    /// <summary>
    /// The catalogue's order (Q43): by <see cref="Race.Order"/>, the same number by id (ordinal), whatever the paths' order;
    /// only the entries directly in <c>data/races</c>; an entry there that isn't a race the data's own refusal.
    /// </summary>
    [Fact]
    public void TheCatalogueIsInTheRacesOrderThenByTheirIds()
    {
        using var folder = new ScratchFolder();
        void Write(string path, object record)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(folder[path])!);
            File.WriteAllText(folder[path], RecordSerializer.ToJson(record.GetType(), record));
        }
        Write("data/races/a.json", TestRaces.Good("A") with { Order = 30 });
        Write("data/races/b.json", TestRaces.Good("B") with { Order = 10 });
        Write("data/races/c-2.json", TestRaces.Good("C2") with { Order = 20 });
        Write("data/races/c-10.json", TestRaces.Good("C10") with { Order = 20 });
        Write("data/races/d.json", TestRaces.Good("D") with { Order = -5 });
        Write("data/races/deeper/e.json", TestRaces.Good("E") with { Order = 0 });
        Write("data/other.json", TestRaces.Good("F") with { Order = 0 });
        using (var data = new VirtualFileSystem([Mount.Folder(folder.Root)]))
        {
            Assert.Equal(["d", "b", "c-10", "c-2", "a"], RaceCatalogue.Load(data).Select(r => r.Id));
            Assert.Equal(["D", "B", "C10", "C2", "A"], RaceCatalogue.Load(data).Select(r => r.Race.Name));
        }
        Write("data/races/z.json", new Data.Saves.SaveGame { Name = "Ser Alrik" });
        using (var data = new VirtualFileSystem([Mount.Folder(folder.Root)]))
            Assert.Contains("not race", Assert.Throws<DataFileException>(() => RaceCatalogue.Load(data)).Message);
    }

    // ---------- The form a race is compared in with the old game's normalized race ----------

    /// <summary>The old game's normalized race (<c>normalizeRaceDefinition</c>'s result) as text, its keys in a fixed order.</summary>
    internal static string NormalizedForm(JsonElement race)
    {
        var passive = race.GetProperty("passive");
        return Form(race.GetProperty("id").GetString()!, race.GetProperty("name").GetString()!, race.GetProperty("playable").GetBoolean(),
            race.GetProperty("icon").ValueKind == JsonValueKind.Null ? null : race.GetProperty("icon").GetString(),
            AttributeKeys.All.Select(k => race.GetProperty("statModifiers").GetProperty(k).GetDouble()),
            passive.GetProperty("id").GetString()!, passive.GetProperty("name").GetString()!, passive.GetProperty("condition").GetString()!,
            passive.GetProperty("description").GetString()!, passive.GetProperty("effects").EnumerateArray().Select(OldEffect));
    }

    /// <summary>The port's race in the same form: the emblem as the old seal's path, the effects in the old file's form.</summary>
    internal static string NormalizedForm(string id, Race race) =>
        Form(id, race.Name, race.Playable, race.Emblem is null ? null : $"objects/race-{race.Emblem}.svg", AttributeKeys.All.Select(k => race.StatModifiers.Values[k]),
            race.Passive.Id, race.Passive.Name, race.Passive.Condition, race.Passive.Description, race.Passive.Effects.Select(PortEffect));

    static string Form(string id, string name, bool playable, string? icon, IEnumerable<double> modifiers, string passiveId, string passiveName,
        string condition, string description, IEnumerable<string> effects) =>
        JsonSerializer.Serialize(new object?[] { id, name, playable, icon, modifiers.ToArray(), passiveId, passiveName, condition, description, effects.ToArray() });

    /// <summary>
    /// An old effect object as text: its type and each field of the port's subtype, the members the object has (null as
    /// not given) and the subtype's defaults for the rest.
    /// </summary>
    internal static string OldEffect(JsonElement effect)
    {
        var type = effect.GetProperty("type").GetString()!;
        var fields = Fields(Activator.CreateInstance(Effect.Types[type])!);
        foreach (var member in effect.EnumerateObject().Where(m => m.Name != "type" && m.Value.ValueKind != JsonValueKind.Null))
            fields[member.Name] = Canon(member.Value);
        return type + "{" + string.Join(",", fields.OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => $"{f.Key}={f.Value}")) + "}";
    }

    /// <summary>A port effect in the same form.</summary>
    internal static string PortEffect(Effect effect) =>
        effect.Type + "{" + string.Join(",", Fields(effect).OrderBy(f => f.Key, StringComparer.Ordinal).Select(f => $"{f.Key}={f.Value}")) + "}";

    /// <summary>An effect's fields by JSON name, each value in <see cref="Canon"/>'s form.</summary>
    static Dictionary<string, string> Fields(object effect) =>
        JsonDocument.Parse(RecordSerializer.ToJson(typeof(Effect), effect)).RootElement.EnumerateObject()
            .Where(m => !m.Name.StartsWith('$')).ToDictionary(m => m.Name, m => Canon(m.Value));

    /// <summary>A JSON value as text that doesn't depend on how it was written (escapes, number forms).</summary>
    static string Canon(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => JsonSerializer.Serialize(value.GetString()),
        JsonValueKind.Number => value.GetDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture),
        JsonValueKind.Array => "[" + string.Join(",", value.EnumerateArray().Select(Canon)) + "]",
        _ => value.GetRawText(),
    };
}

/// <summary>Two races equal by their JSON forms (records holding lists compare the lists by reference).</summary>
sealed class RaceComparer : IEqualityComparer<Race>
{
    public static RaceComparer Instance { get; } = new();

    public bool Equals(Race? a, Race? b) => a is null ? b is null : b is not null && RecordSerializer.ToJson(a) == RecordSerializer.ToJson(b);

    public int GetHashCode(Race race) => RecordSerializer.ToJson(race).GetHashCode(StringComparison.Ordinal);
}
