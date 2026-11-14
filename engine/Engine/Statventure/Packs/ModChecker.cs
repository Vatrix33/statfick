using Statventure.Records;

namespace Statventure.Packs;

/// <summary>The shortcuts a <see cref="ModChecker"/> takes (each gives exactly the plain check's results).</summary>
[Flags]
internal enum ModShortcuts
{
    /// <summary>The plain check: every record of the data read and checked for each mod (<see cref="ModCheck.Check"/>).</summary>
    None = 0,
    /// <summary>The game's own records read, and checked alone (without data), once per checker, not once per mod.</summary>
    GameOnce = 1,
    /// <summary>
    /// A game record checked with the data (and the music loaded) once, over the game's data alone, its questions to the
    /// data recorded (<see cref="DataReads"/>); checked again with a mod only when the mod holds a path it read, or one
    /// below a folder it listed. Takes the game's side as <see cref="GameOnce"/> does.
    /// </summary>
    Independent = 2,
    /// <summary>
    /// A record checked with the data first, recorded; when that check didn't use the data (<see cref="DataReads.UsedTheData"/>)
    /// its problems are its problems alone too, so it isn't checked alone again (one walk of it, not two).
    /// </summary>
    OneWalk = 4,
    All = GameOnce | Independent | OneWalk,
}

/// <summary>
/// The shared mod check (<see cref="ModCheck"/>; GM1-P13, the user's Q4) for several mods over the same game's data (GM1 step
/// 6b; the manager's decision of 2026-11-14 after the user kept the full check, Q27): each mod gets exactly the problems
/// <see cref="ModCheck.Check"/> gives it, in the same order and words, while the game's side is done once: the game's
/// records read and checked alone once (<c>GameOnce</c>), and each checked with the game's data once, recording what it
/// asked the data (the paths it read or asked about, the folders it listed, the kinds the references looked up), so that
/// for a mod it is checked again with the mod's data only when the mod holds one of those paths or a path below one of
/// those folders (<c>Independent</c>); the music's loading the same way. A check is a function of its value and of what it
/// asks the data (the engine's and the data library's are), so a record whose questions the mod doesn't touch gets the
/// same answers and the same problems. A record whose check with the data never used it (never asked for the data or the
/// lookup, nothing read) has the same problems alone, so it is walked once, not twice (<c>OneWalk</c>). The game's mounts
/// are mounted once, at the first mod (when they can't be, every mod is checked plainly, as <see cref="ModCheck.Check"/>
/// would). One checker per mount of the game's data (the game's filter makes one each time the engine mounts the mods); not
/// for use from several threads.
/// </summary>
public sealed class ModChecker : IDisposable
{
    readonly IReadOnlyList<Mount> _game;
    readonly RecordTypes _kinds;
    GameSide? _side;
    bool _sideTried, _disposed;

    /// <summary>A checker of mods over <paramref name="game"/> (the mounts the mods go over), their kinds <paramref name="kinds"/>, with every shortcut.</summary>
    public ModChecker(IReadOnlyList<Mount> game, RecordTypes kinds)
        : this(game, kinds, ModShortcuts.All)
    {
    }

    internal ModChecker(IReadOnlyList<Mount> game, RecordTypes kinds, ModShortcuts shortcuts)
    {
        ArgumentNullException.ThrowIfNull(game);
        ArgumentNullException.ThrowIfNull(kinds);
        _game = [.. game];
        _kinds = kinds;
        Shortcuts = shortcuts;
    }

    /// <summary>The shortcuts it takes.</summary>
    internal ModShortcuts Shortcuts { get; }

    /// <summary>How many game records were checked again with a mod's data since the checker was made (tests).</summary>
    internal int Rechecked { get; private set; }

    /// <summary>How many records were walked once only (their check with the data didn't use it) since the checker was made (tests).</summary>
    internal int WalkedOnce { get; private set; }

    bool Has(ModShortcuts shortcut) => (Shortcuts & shortcut) != 0;

    /// <summary>The mod in <paramref name="file"/> checked alone over the game's data: <see cref="ModCheck.Check"/>'s report, exactly.</summary>
    public ModReport Check(string file)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        file = Path.GetFullPath(file);
        PackFile pack;
        try
        {
            pack = PackFile.Open(file);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            // (An entry the pack's index names, when the index refuses one.)
            return new ModReport(file, null, [new ModProblem((problem as DataFileException)?.Entry, ModCheck.OpenProblem(problem))]);
        }
        using (pack)
        {
            ModInfo? info = null;
            try
            {
                var reads = pack.Entries.Select(entry => ModCheck.Read(pack, entry, _kinds)).ToList();
                info = reads.FirstOrDefault(r => r.Entry.Path == ModInfo.Path)?.Record as ModInfo;
                var unreadable = reads.Where(r => r.Problem is not null).Select(r => new ModProblem(r.Entry.Path, r.Problem!)).ToList();
                if (unreadable.Count > 0)
                    return new ModReport(file, info, unreadable);
                return new ModReport(file, info, Checked(file, reads));
            }
            catch (Exception problem) when (problem is not OutOfMemoryException)
            {
                // (A file that can't be read on, the system's error; anything else that throws.)
                return new ModReport(file, info, [new ModProblem(null, $"its check failed: {problem.Message}")]);
            }
        }
    }

    /// <summary>The record checks of a mod whose entries all read: the data the game would see with it, every record it shows checked.</summary>
    List<ModProblem> Checked(string file, List<PackEntryRead> reads)
    {
        // The game's side first: when the game's data can't be mounted alone, the mod is checked plainly (with the mod over
        // it, that is the data's problem below).
        var side = Side();
        VirtualFileSystem data;
        try
        {
            data = new VirtualFileSystem([.. _game, Mount.Pack(file)]);
        }
        catch (DataFileException problem)
        {
            // The mod can't be mounted over the game's data (the editor's "entries that can't be mounted together").
            return [new ModProblem(null, problem.Message)];
        }
        using (data)
        {
            var mine = reads.ToDictionary(r => r.Entry.Path, StringComparer.Ordinal);
            var changed = new HashSet<string>(mine.Keys, StringComparer.Ordinal);
            // The records the data shows, in path order: the mod's own (on top) and the game's below.
            var shown = new List<(string Path, object Value, GameRecord? Game)>();
            var kindsAt = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var path in data.List())
            {
                object? value;
                GameRecord? game = null;
                if (mine.TryGetValue(path, out var read))
                    value = read.Record;
                else if (side is not null)
                    // (A game path the game's data gives no record at, a file or one it can't read, isn't checked.)
                    value = side.Records.TryGetValue(path, out game) ? game.Value : null;
                else
                    value = ModCheck.GameRecord(data, path, _kinds);
                if (value is null)
                    continue;
                kindsAt[path] = RecordTypes.NameOf(value.GetType());
                shown.Add((path, value, game));
            }
            var lookup = new ModCheck.KindLookup(kindsAt);
            var records = new List<RecordToCheck>(shown.Count);
            var known = new IReadOnlyList<RecordProblem>?[shown.Count];
            for (var i = 0; i < shown.Count; i++)
            {
                var (path, value, game) = shown[i];
                IReadOnlyList<RecordProblem> own;
                if (game is not null)
                {
                    own = OwnOf(side!, game);
                    known[i] = Known(side!, game, changed);
                }
                else if (Has(ModShortcuts.OneWalk))
                    (own, known[i]) = Walked(path, value, data, lookup);
                else
                    own = ModCheck.Problems(path, value);
                records.Add(new RecordToCheck(path, value, own));
            }
            var result = ModCheck.CheckRecords(records, data, lookup, i => known[i],
                side is not null && Has(ModShortcuts.Independent) ? () => Music(side, data, changed) : null);
            var problems = new List<ModProblem>();
            for (var i = 0; i < records.Count; i++)
                if (ModCheck.FirstPerPath([.. records[i].Own, .. result.Added[i]]) is { Count: > 0 } first)
                    problems.Add(new ModProblem(records[i].Path, first[0].ToString()));
            // A file where a mod keeps its info (a record of another kind there is a path rule).
            if (mine.TryGetValue(ModInfo.Path, out var info) && info.IsFile)
            {
                var at = problems.FindIndex(p => string.CompareOrdinal(p.Entry, ModInfo.Path) > 0);
                problems.Insert(at < 0 ? problems.Count : at, new ModProblem(ModInfo.Path, ModCheck.InfoProblem(null)));
            }
            if (result.DataProblem is { } dataProblem)
                problems.Add(new ModProblem(null, dataProblem));
            return problems;
        }
    }

    /// <summary>
    /// A record's own problems and what the data adds, from one check with the data when it doesn't use the data (its own
    /// problems are then the same), else from that check and one alone (<c>OneWalk</c>).
    /// </summary>
    (IReadOnlyList<RecordProblem> Own, IReadOnlyList<RecordProblem> Added) Walked(string path, object value, VirtualFileSystem data, IReferenceLookup lookup)
    {
        var with = DataReads.During(() => ModCheck.Problems(path, value, data, lookup), out var reads);
        if (!reads.UsedTheData)
        {
            WalkedOnce++;
            return (with, []);
        }
        var own = ModCheck.Problems(path, value);
        return (own, ModCheck.Without(with, own));
    }

    /// <summary>A game record's own problems, once (with <c>OneWalk</c>, from its check over the game's data when that didn't use it).</summary>
    IReadOnlyList<RecordProblem> OwnOf(GameSide side, GameRecord game)
    {
        if (game.Own is null)
        {
            if (Has(ModShortcuts.OneWalk))
                OverTheGame(side, game);
            else
                game.Own = ModCheck.Problems(game.Path, game.Value);
        }
        return game.Own!;
    }

    /// <summary>The game record checked over the game's data alone, once, its questions recorded (and its own problems, when not known yet).</summary>
    void OverTheGame(GameSide side, GameRecord game)
    {
        if (game.Reads is not null)
            return;
        var with = DataReads.During(() => ModCheck.Problems(game.Path, game.Value, side.Data, side.Lookup), out var reads);
        if (game.Own is null)
        {
            if (Has(ModShortcuts.OneWalk) && !reads.UsedTheData)
            {
                WalkedOnce++;
                game.Own = with;
            }
            else
                game.Own = ModCheck.Problems(game.Path, game.Value);
        }
        game.Added = ModCheck.Without(with, game.Own);
        game.Reads = reads;
    }

    /// <summary>
    /// What checking <paramref name="game"/> with a mod's data adds to its own problems, when the mod holding
    /// <paramref name="changed"/> can't change it (<c>Independent</c>): its check over the game's data alone; null when the mod
    /// holds a path it asked about, or one below a folder it listed (it is checked again), or without the shortcut.
    /// </summary>
    IReadOnlyList<RecordProblem>? Known(GameSide side, GameRecord game, IReadOnlySet<string> changed)
    {
        if (!Has(ModShortcuts.Independent))
            return null;
        OverTheGame(side, game);
        if (game.Reads!.Touches(changed))
        {
            Rechecked++;
            return null;
        }
        return game.Added;
    }

    /// <summary>The music's loading with a mod's data: over the game's data alone (once, recording its questions) unless the mod touches them.</summary>
    DataFileException? Music(GameSide side, VirtualFileSystem data, IReadOnlySet<string> changed)
    {
        if (side.MusicReads is null)
        {
            side.Music = DataReads.During(() => ModCheck.Music(side.Data), out var reads);
            side.MusicReads = reads;
        }
        return side.MusicReads.Touches(changed) ? ModCheck.Music(data) : side.Music;
    }

    /// <summary>The game's side, made at the first mod when a shortcut is taken; null without one, or when the game's data can't be mounted alone (then every mod is checked plainly).</summary>
    GameSide? Side()
    {
        if (Shortcuts == ModShortcuts.None || _sideTried)
            return _side;
        _sideTried = true;
        VirtualFileSystem? data = null;
        try
        {
            data = new VirtualFileSystem(_game);
            var records = new Dictionary<string, GameRecord>(StringComparer.Ordinal);
            var kinds = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var path in data.List())
                if (ModCheck.GameRecord(data, path, _kinds) is { } value)
                {
                    records[path] = new GameRecord(path, value);
                    kinds[path] = RecordTypes.NameOf(value.GetType());
                }
            _side = new GameSide(data, records, new ModCheck.KindLookup(kinds));
        }
        catch (Exception problem) when (problem is not OutOfMemoryException)
        {
            data?.Dispose();
            _side = null;
        }
        return _side;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _side?.Data.Dispose();
    }

    /// <summary>The game's data alone, its records and their kinds; the music's loading over it once done.</summary>
    sealed class GameSide(VirtualFileSystem data, Dictionary<string, GameRecord> records, ModCheck.KindLookup lookup)
    {
        public VirtualFileSystem Data { get; } = data;
        public Dictionary<string, GameRecord> Records { get; } = records;
        public ModCheck.KindLookup Lookup { get; } = lookup;
        public DataFileException? Music { get; set; }
        public DataReads? MusicReads { get; set; }
    }

    /// <summary>A record of the game's data: its value; once known, its own problems; once checked over the game's data, what that added and what it asked.</summary>
    sealed class GameRecord(string path, object value)
    {
        public string Path { get; } = path;
        public object Value { get; } = value;
        public IReadOnlyList<RecordProblem>? Own { get; set; }
        public IReadOnlyList<RecordProblem> Added { get; set; } = [];
        public DataReads? Reads { get; set; }
    }
}
