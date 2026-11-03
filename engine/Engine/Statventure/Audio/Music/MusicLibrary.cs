using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Audio;

/// <summary>
/// Where the audio data lives in the game's data (Docs/Assets.md, step 6; Q21, S6-1): one entry per
/// theme, instrument, percussion voice, room and effect patch, each at its folder and a name made from
/// what the music calls it, plus the theme order. Names are stored by one reversible rule: a capital
/// letter becomes '_' and the small letter ("lowSpiccato" → <c>low_spiccato</c>), '@' becomes '.'
/// ("combat@alder-vale" → <c>combat.alder-vale</c>); letters a–z, digits and '-' stay. A name with
/// any other character, or whose path the path rule refuses, can't be stored.
/// </summary>
public static class MusicNames
{
    public const string Themes = "audio/themes";
    public const string Instruments = "audio/instruments";
    public const string Percussion = "audio/percussion";
    public const string Rooms = "audio/rooms";
    public const string Effects = "audio/effects";
    /// <summary>The themes in their order (<see cref="ThemeOrder"/>).</summary>
    public const string ThemeOrder = "audio/theme-order";

    /// <summary>The path name stored for <paramref name="name"/> by the rule, or null when it can't be stored.</summary>
    public static string? ToPathName(string name)
    {
        var text = new System.Text.StringBuilder(name.Length + 4);
        foreach (var c in name)
        {
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')
                text.Append(c);
            else if (c is >= 'A' and <= 'Z')
                text.Append('_').Append((char)(c - 'A' + 'a'));
            else if (c == '@')
                text.Append('.');
            else
                return null;
        }
        var stored = text.ToString();
        return PackPath.IsValid(stored) ? stored : null;
    }

    /// <summary>The name a path name stores, or null when it isn't one the rule makes.</summary>
    public static string? FromPathName(string pathName)
    {
        if (!PackPath.IsValid(pathName) || pathName.Contains('/'))
            return null;
        var text = new System.Text.StringBuilder(pathName.Length);
        for (var i = 0; i < pathName.Length; i++)
        {
            var c = pathName[i];
            if (c == '_')
            {
                if (i + 1 >= pathName.Length || pathName[i + 1] is not (>= 'a' and <= 'z'))
                    return null;
                text.Append((char)(pathName[++i] - 'a' + 'A'));
            }
            else if (c == '.')
                text.Append('@');
            else
                text.Append(c);
        }
        return text.ToString();
    }

    /// <summary>The entry path of <paramref name="name"/> in <paramref name="folder"/>; an <see cref="ArgumentException"/> when the rule can't store it.</summary>
    public static string PathOf(string folder, string name) =>
        ToPathName(name) is { } stored
            ? folder + "/" + stored
            : throw new ArgumentException($"'{name}' can't be stored as a data path (names may hold a–z, A–Z, 0–9, '-' and '@')", nameof(name));
}

/// <summary>
/// The music and sound effects an <see cref="AudioSystem"/> plays (Docs/Assets.md, step 6): the themes
/// in their order, the composer's instruments, percussion voices and rooms, and the effect patches,
/// read from the game's data (<see cref="Load"/>) or given in code. Each theme is compiled once per
/// library, all of them the first time one is asked for.
/// </summary>
public sealed class MusicLibrary
{
    readonly Dictionary<string, ThemeSpec> _themes;
    readonly Lazy<Dictionary<string, CompiledTheme>> _compiled;

    internal MusicLibrary(IEnumerable<ThemeSpec> themes, IReadOnlyDictionary<string, InstrumentSpec> instruments,
        IReadOnlyDictionary<string, PercussionSpec> percussion, IReadOnlyDictionary<string, RoomSpec> rooms,
        IReadOnlyDictionary<string, EffectPatch> effects)
    {
        _themes = new Dictionary<string, ThemeSpec>(StringComparer.Ordinal);
        var order = new List<string>();
        foreach (var theme in themes)
        {
            if (!_themes.TryAdd(theme.Id, theme))
                throw new ArgumentException($"Two themes are called '{theme.Id}'", nameof(themes));
            order.Add(theme.Id);
        }
        Themes = order;
        Instruments = instruments;
        Percussion = percussion;
        Rooms = rooms;
        Effects = effects;
        _compiled = new(() => _themes.ToDictionary(t => t.Key, t => Score.Compile(t.Value), StringComparer.Ordinal));
    }

    /// <summary>No music and no effects (the data has none).</summary>
    public static MusicLibrary Empty { get; } = new([], new Dictionary<string, InstrumentSpec>(), new Dictionary<string, PercussionSpec>(),
        new Dictionary<string, RoomSpec>(), new Dictionary<string, EffectPatch>());

    /// <summary>Every theme's id, in order (the theme order, then themes it doesn't name in path order).</summary>
    public IReadOnlyList<string> Themes { get; }

    /// <summary>The composer's instruments by name.</summary>
    public IReadOnlyDictionary<string, InstrumentSpec> Instruments { get; }

    /// <summary>The composer's percussion voices by name.</summary>
    public IReadOnlyDictionary<string, PercussionSpec> Percussion { get; }

    /// <summary>The rooms (reverbs) by name.</summary>
    public IReadOnlyDictionary<string, RoomSpec> Rooms { get; }

    /// <summary>The effect patches by name, which the audio system registers when it starts.</summary>
    public IReadOnlyDictionary<string, EffectPatch> Effects { get; }

    /// <summary>The theme called <paramref name="id"/>, or null.</summary>
    public ThemeSpec? Theme(string id) => _themes.GetValueOrDefault(id);

    /// <summary>The compiled theme called <paramref name="id"/>, or null (compiling every theme the first time).</summary>
    internal CompiledTheme? Compiled(string id) => _compiled.Value.GetValueOrDefault(id);

    /// <summary>The room called <paramref name="name"/>, or the hall, as the composer falls back on it.</summary>
    internal RoomSpec Room(string name) => Rooms.GetValueOrDefault(name) ?? Rooms["hall"];

    /// <summary>
    /// Whether the theme <paramref name="id"/> sounds the same from <paramref name="other"/> (S9-D): its record,
    /// and every instrument, percussion voice and room of both libraries, equal by their binary forms. False
    /// when either hasn't the theme.
    /// </summary>
    internal bool SameMusic(MusicLibrary other, string id) =>
        Theme(id) is { } mine && other.Theme(id) is { } theirs && SameRecord(mine, theirs)
        && SameRecords(Instruments, other.Instruments) && SameRecords(Percussion, other.Percussion) && SameRecords(Rooms, other.Rooms);

    /// <summary>Whether two records have the same binary form.</summary>
    internal static bool SameRecord<T>(T a, T b) where T : notnull =>
        RecordSerializer.ToBinary(a).AsSpan().SequenceEqual(RecordSerializer.ToBinary(b));

    static bool SameRecords<T>(IReadOnlyDictionary<string, T> a, IReadOnlyDictionary<string, T> b) where T : notnull =>
        a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out var value) && SameRecord(pair.Value, value));

    /// <summary>
    /// The library in <paramref name="data"/> (S6-1–S6-4): the records in <c>audio/themes/</c>,
    /// <c>audio/instruments/</c>, <c>audio/percussion/</c>, <c>audio/rooms/</c> and
    /// <c>audio/effects/</c>, named by <see cref="MusicNames"/>' rule, and <c>audio/theme-order</c>. With
    /// no music entries at all the library has no music (its effects still load). Otherwise the theme
    /// order must be there, name each theme at most once and only themes that have entries; a theme's
    /// id must be the one its path stores, and its room (the hall when it names none) must be there or
    /// the hall be there to fall back on. Themes the order doesn't name follow it in path order. An effect
    /// patch must be playable (<see cref="SoundEffects.Register"/>'s checks). Every
    /// problem is a <see cref="DataFileException"/> naming the pack and entry, or the loose file.
    /// </summary>
    public static MusicLibrary Load(VirtualFileSystem data)
    {
        var instruments = Read<InstrumentSpec>(data, MusicNames.Instruments, "an instrument");
        var percussion = Read<PercussionSpec>(data, MusicNames.Percussion, "a percussion voice");
        var rooms = Read<RoomSpec>(data, MusicNames.Rooms, "a room");
        var effects = Read<EffectPatch>(data, MusicNames.Effects, "an effect patch");
        foreach (var (name, (path, patch)) in effects)
            if (patch.Problem() is { } unplayable)
                throw Problem(data, path, $"the effect patch '{name}' {unplayable}");
        var themes = Read<ThemeSpec>(data, MusicNames.Themes, "a theme");
        foreach (var (name, (path, theme)) in themes)
        {
            if (theme.Id != name)
                throw Problem(data, path, $"the theme's id is '{theme.Id}', but its path holds '{name}' (a theme's path is made from its id)");
            var room = theme.Room is { Length: > 0 } named ? named : "hall";
            if (!rooms.ContainsKey(room) && !rooms.ContainsKey("hall"))
                throw Problem(data, path, $"its room '{room}' isn't in the data, nor is the room 'hall' the composer falls back on");
        }
        var music = themes.Count + instruments.Count + percussion.Count + rooms.Count;
        if (!data.Exists(MusicNames.ThemeOrder))
        {
            if (music == 0)
                return new([], Values(instruments), Values(percussion), Values(rooms), Values(effects));
            var first = data.List(MusicNames.Themes).Concat(data.List(MusicNames.Instruments)).Concat(data.List(MusicNames.Percussion))
                .Concat(data.List(MusicNames.Rooms)).First();
            throw Problem(data, first, $"there is music in the data but no theme order ('{MusicNames.ThemeOrder}')");
        }
        var order = data.Load<ThemeOrder>(MusicNames.ThemeOrder);
        var listed = new HashSet<string>(StringComparer.Ordinal);
        var ordered = new List<ThemeSpec>();
        foreach (var id in order.Themes)
        {
            if (!listed.Add(id))
                throw Problem(data, MusicNames.ThemeOrder, $"it names the theme '{id}' twice");
            if (MusicNames.ToPathName(id) is not { } stored)
                throw Problem(data, MusicNames.ThemeOrder, $"it names the theme '{id}', which no data path can hold");
            if (!themes.TryGetValue(id, out var theme))
                throw Problem(data, MusicNames.ThemeOrder, $"it names the theme '{id}', which has no entry '{MusicNames.Themes}/{stored}'");
            ordered.Add(theme.Value);
        }
        // Themes the order doesn't name follow it, in path order (a mod may add a theme with its entry alone).
        foreach (var (id, (_, theme)) in themes)
            if (!listed.Contains(id))
                ordered.Add(theme);
        return new(ordered, Values(instruments), Values(percussion), Values(rooms), Values(effects));
    }

    /// <summary>Every record in <paramref name="folder"/> by the name its path stores, in path order.</summary>
    static OrderedDictionary<string, (string Path, T Value)> Read<T>(VirtualFileSystem data, string folder, string what) where T : notnull
    {
        var records = new OrderedDictionary<string, (string, T)>(StringComparer.Ordinal);
        foreach (var path in data.List(folder))
        {
            var pathName = path[(folder.Length + 1)..];
            if (MusicNames.FromPathName(pathName) is not { } name)
                throw Problem(data, path, $"not {what}'s path: names in {folder}/ are stored by the music's rule (a capital as '_' and the small letter, '@' as '.', no folders)");
            records.Add(name, (path, data.Load<T>(path)));
        }
        return records;
    }

    static IReadOnlyDictionary<string, T> Values<T>(OrderedDictionary<string, (string Path, T Value)> records) where T : notnull
    {
        var values = new Dictionary<string, T>(records.Count, StringComparer.Ordinal);
        foreach (var (name, (_, value)) in records)
            values.Add(name, value);
        return values;
    }

    /// <summary>A problem with the entry at <paramref name="path"/>, named as the file system names its own (the pack and entry, or the loose file).</summary>
    static DataFileException Problem(VirtualFileSystem data, string path, string problem) => data.Problem(path, problem);
}
