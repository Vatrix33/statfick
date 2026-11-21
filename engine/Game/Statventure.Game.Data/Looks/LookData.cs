using Statventure.Game.Data.Races;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Game.Data.Looks;

/// <summary>
/// The maker's records in the game's data (Docs/Game/Hero.md, "How it works", step 6): the bodies, parts and presets
/// directly in their folders, by name (the entry's name). What the maker shows for a body is
/// <see cref="Offered"/> (a category's options) and <see cref="PresetsOf"/> (a race's list); the looks' checks read it
/// too. <see cref="Load"/> is the game's (an entry there of another kind is the data's error, as for the races);
/// <see cref="Read"/> the checks' (it passes over an entry that doesn't read, whose own problem is reported where it is).
/// </summary>
public sealed class LookData
{
    LookData(IReadOnlyDictionary<string, LookBody> bodies, IReadOnlyDictionary<string, LookPart> parts, IReadOnlyDictionary<string, LookPreset> presets)
    {
        Bodies = bodies;
        Parts = parts;
        Presets = presets;
    }

    /// <summary>The bodies by name, in the names' order.</summary>
    public IReadOnlyDictionary<string, LookBody> Bodies { get; }

    /// <summary>The parts by name, in the names' order.</summary>
    public IReadOnlyDictionary<string, LookPart> Parts { get; }

    /// <summary>The presets by name, in the names' order.</summary>
    public IReadOnlyDictionary<string, LookPreset> Presets { get; }

    /// <summary>The maker's records in <paramref name="data"/>; an entry in their folders that isn't of its folder's kind is the data's <see cref="DataFileException"/>.</summary>
    public static LookData Load(VirtualFileSystem data) =>
        new(Entries<LookBody>(data, LookNames.BodiesFolder, strict: true), Entries<LookPart>(data, LookNames.PartsFolder, strict: true),
            Entries<LookPreset>(data, LookNames.PresetsFolder, strict: true));

    /// <summary>The maker's records in <paramref name="data"/> as the checks read them: an entry that doesn't read as its folder's kind passed over.</summary>
    public static LookData Read(VirtualFileSystem data) =>
        new(BodiesIn(data), PartsIn(data), Entries<LookPreset>(data, LookNames.PresetsFolder, strict: false));

    /// <summary>
    /// The parts offered on the body <paramref name="name"/> (<paramref name="body"/>) with the face <paramref name="face"/>
    /// (the look's face part, null: none chosen; a part whose models name faces is offered only with one of them, Q65), by
    /// category in the maker's order (<see cref="LookNames.Categories"/>), each category's by its parts'
    /// <see cref="LookPart.Order"/>, the same place by name (GM2-P36); a category with none is left out.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Offered(string name, LookBody body, string? face = null) => Offered(Parts, name, body, face);

    /// <summary><see cref="Offered(string, LookBody, string?)"/> among <paramref name="parts"/>.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Offered(IReadOnlyDictionary<string, LookPart> parts, string name, LookBody body, string? face = null)
    {
        var offered = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        foreach (var category in LookNames.Categories)
        {
            var options = parts.Where(p => p.Value.Category == category && p.Value.IsFor(name, body, face))
                .OrderBy(p => p.Value.Order).ThenBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key).ToList();
            if (options.Count > 0)
                offered[category] = options;
        }
        return offered;
    }

    /// <summary>The face a look chose (its part of the category <see cref="LookNames.Face"/>), or null.</summary>
    public static string? FaceOf(LookHero look) => look.Parts.TryGetValue(LookNames.Face, out var face) ? face : null;

    /// <summary>The bodies in <paramref name="data"/> as the checks read them (one that doesn't read passed over).</summary>
    public static IReadOnlyDictionary<string, LookBody> BodiesIn(VirtualFileSystem data) => Entries<LookBody>(data, LookNames.BodiesFolder, strict: false);

    /// <summary>The parts in <paramref name="data"/> as the checks read them (one that doesn't read passed over).</summary>
    public static IReadOnlyDictionary<string, LookPart> PartsIn(VirtualFileSystem data) => Entries<LookPart>(data, LookNames.PartsFolder, strict: false);

    /// <summary>The body of <paramref name="race"/>, <paramref name="sex"/> and <paramref name="build"/> (its name), or null when the data has none.</summary>
    public string? BodyOf(string race, string sex, string build) =>
        Bodies.FirstOrDefault(b => b.Value.Race == race && b.Value.Sex == sex && b.Value.Build == build).Key;

    /// <summary>The presets of <paramref name="race"/> (their names), the maker's list: by <see cref="LookPreset.Order"/>, the same number by name.</summary>
    public IReadOnlyList<string> PresetsOf(string race) =>
        [.. Presets.Where(p => p.Value.Look.Race == race).OrderBy(p => p.Value.Order).ThenBy(p => p.Key, StringComparer.Ordinal).Select(p => p.Key)];

    /// <summary>The body named <paramref name="name"/> in <paramref name="data"/>, or null when none reads there (the checks' lookup).</summary>
    public static LookBody? Body(VirtualFileSystem data, string name) => Entry<LookBody>(data, LookNames.BodiesFolder, name);

    /// <summary>The part named <paramref name="name"/> in <paramref name="data"/>, or null when none reads there.</summary>
    public static LookPart? Part(VirtualFileSystem data, string name) => Entry<LookPart>(data, LookNames.PartsFolder, name);

    /// <summary>The race <paramref name="id"/> in <paramref name="data"/>, or null when none reads there.</summary>
    public static Race? RaceOf(VirtualFileSystem data, string id) => Entry<Race>(data, RaceCatalogue.Folder, id);

    static T? Entry<T>(VirtualFileSystem data, string folder, string name) where T : class
    {
        if (NameRule.Plain.PathOf(folder, name) is not { } path || !data.Exists(path))
            return null;
        try
        {
            return data.IsRecord(path) ? data.Load<T>(path) : null;
        }
        catch (DataFileException)
        {
            return null;
        }
    }

    static IReadOnlyDictionary<string, T> Entries<T>(VirtualFileSystem data, string folder, bool strict) where T : class
    {
        var entries = new SortedDictionary<string, T>(StringComparer.Ordinal);
        foreach (var path in data.List(folder))
        {
            if (NameRule.Plain.NameOf(folder, path) is not { } name)
                continue;
            if (strict)
                entries[name] = data.Load<T>(path);
            else if (Entry<T>(data, folder, name) is { } value)
                entries[name] = value;
        }
        return entries;
    }
}
