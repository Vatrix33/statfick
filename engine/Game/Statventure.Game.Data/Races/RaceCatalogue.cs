using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Game.Data.Races;

/// <summary>A race and its id (its entry's name in <see cref="RaceCatalogue.Folder"/>).</summary>
public sealed record RaceEntry(string Id, Race Race);

/// <summary>
/// The game's races from its data (the old game's race catalogue, <c>raceCatalog</c>): every entry directly in
/// <c>data/races</c>, in the races' order (Q43: <see cref="Race.Order"/>, then the id).
/// </summary>
public static class RaceCatalogue
{
    /// <summary>The folder of the races' entries (the kind's).</summary>
    public const string Folder = "data/races";

    /// <summary>The entry path of the race <paramref name="id"/>.</summary>
    public static string PathOf(string id) => NameRule.Plain.PathOf(Folder, id) ?? throw new ArgumentException($"\"{id}\" can't be a race's id", nameof(id));

    /// <summary>
    /// Every race in <paramref name="data"/>, ordered by <see cref="Race.Order"/> and then by id (ordinal). An entry there
    /// that isn't a race is the data's <see cref="DataFileException"/>, as any load of the wrong kind.
    /// </summary>
    public static IReadOnlyList<RaceEntry> Load(VirtualFileSystem data) =>
        [.. data.List(Folder)
            .Select(path => (Path: path, Id: NameRule.Plain.NameOf(Folder, path)))
            .Where(e => e.Id is not null)
            .Select(e => new RaceEntry(e.Id!, data.Load<Race>(e.Path)))
            .OrderBy(e => e.Race.Order)
            .ThenBy(e => e.Id, StringComparer.Ordinal)];
}
