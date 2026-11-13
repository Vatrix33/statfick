using Statventure.Core;
using Statventure.Game.Data.Saves;
using Statventure.Game.Settings;
using Statventure.Records;
using Statventure.Storage;

namespace Statventure.Game.Session;

/// <summary>Where a slot's or a damaged slot's name came from.</summary>
public enum SlotNameSource
{
    /// <summary>The slot's save.</summary>
    Save,
    /// <summary>The save can't be read; its backup's.</summary>
    Backup,
    /// <summary>Neither can be read: the slot's file name stands for it (the slot list's "damaged"; Q25: Start new's name).</summary>
    File,
}

/// <summary>A save slot as the slot list shows it.</summary>
/// <param name="Slot">The slot's name, its file <c>saves/&lt;slot&gt;.dat</c> (named from the character's name, Q1).</param>
/// <param name="Name">The character's name: the save's, else the backup's when it reads, else the slot's file name (<paramref name="NameFrom"/> says which).</param>
/// <param name="LastPlayed">When it was last written (UTC).</param>
/// <param name="Damaged">Whether its save can't be read (opening it asks the damaged-save question), whatever its backup is.</param>
/// <param name="NameFrom">Where <paramref name="Name"/> came from.</param>
public sealed record SlotInfo(string Slot, string Name, DateTimeOffset LastPlayed, bool Damaged, SlotNameSource NameFrom = SlotNameSource.Save);

/// <summary>A damaged slot's backup (Q17 of M9, Q21): missing, readable, or not readable (damaged, or unreadable here).</summary>
public enum BackupState
{
    Missing,
    Readable,
    Damaged,
}

/// <summary>What opening a slot gave: opened, or damaged with its backup's state and time (the damaged-save question).</summary>
/// <param name="Slot">The slot.</param>
/// <param name="Opened">Whether it opened (its save read).</param>
/// <param name="Backup">Damaged: its backup's state ("Use the backup" only when <see cref="BackupState.Readable"/>).</param>
/// <param name="BackupTime">Damaged: its backup file's last write time (UTC) when the file is there.</param>
/// <param name="Name">
/// The character's name: opened, the save's; damaged, the one the question names and "Start new" keeps (Q21, Q25): the
/// backup's when it reads, else the slot's file name.
/// </param>
/// <param name="NameFrom">Where <paramref name="Name"/> came from.</param>
public sealed record OpenResult(string Slot, bool Opened, BackupState Backup = BackupState.Missing, DateTimeOffset? BackupTime = null,
    string Name = "", SlotNameSource NameFrom = SlotNameSource.Save);

/// <summary>What loading a character file gave: the new slot it opened in, or the problem's words ("That file is not a valid character save", Q23).</summary>
public sealed record ImportResult(string? Slot, string? Problem);

/// <summary>
/// The save slots and the open character (Docs/Game/Foundation.md, "The program's flow"; step 5), on the engine's
/// <see cref="SaveSlots"/>: the slots listed with their characters' names, a new character made in a slot named from
/// its name (<see cref="SlotNames"/>), a slot opened or, when its save can't be read, the damaged-save question's facts
/// and its two answers that write (<see cref="UseBackup"/>, <see cref="StartNew"/>), every change saved (the engine's
/// background writer, the latest state winning), the character left (saved), character files written and loaded into a
/// new slot (Q9, Q24). <see cref="GameSettings.LastCharacter"/> names the slot last opened (Continue's). Every write of a
/// slot raises <see cref="SlotsChanged"/>; a write that changes the open character also <see cref="Saved"/> (the
/// autosave dot: making and loading a character too, as the old game's <c>saveCharacter</c>), after
/// <see cref="Opened"/> when it opens one. Used on the game's thread.
/// </summary>
public sealed class GameSession
{
    /// <summary>The old game's words for a file that isn't a character (Q23: every kind of wrong file, an old game's too).</summary>
    public const string NotACharacterSave = "That file is not a valid character save";

    readonly GameStorage _storage;
    readonly GameSettings _settings;

    /// <summary>The session over <paramref name="storage"/>'s slots; a <see cref="GameSettings.LastCharacter"/> that names no slot there is taken as none.</summary>
    public GameSession(GameStorage storage, GameSettings settings)
    {
        _storage = storage;
        _settings = settings;
        if (settings.LastCharacter is { } last && !storage.Saves.Exists(last))
        {
            Log.Info("Session", $"the last character's slot {last} isn't there: nothing to continue");
            settings.LastCharacter = null;
        }
    }

    SaveSlots Saves => _storage.Saves;

    /// <summary>The open character's save, or null.</summary>
    public SaveGame? Current { get; private set; }

    /// <summary>The open character's slot, or null.</summary>
    public string? CurrentSlot { get; private set; }

    /// <summary>A character was opened.</summary>
    public event Action? Opened;
    /// <summary>The open character was left.</summary>
    public event Action? Closed;
    /// <summary>A change of the open character was saved to its slot (the autosave dot).</summary>
    public event Action? Saved;
    /// <summary>A slot was made, written or deleted.</summary>
    public event Action? SlotsChanged;

    /// <summary>The slot Continue opens: <see cref="GameSettings.LastCharacter"/> while its slot is there, else null (Continue greyed, Q16b).</summary>
    public string? ContinueSlot() => _settings.LastCharacter is { } last && Saves.Exists(last) ? last : null;

    /// <summary>Every slot, the last played first (ties in their names' order).</summary>
    public IReadOnlyList<SlotInfo> Slots()
    {
        var slots = new List<SlotInfo>();
        foreach (var info in Saves.List())
        {
            var reading = Saves.Load<SaveGame>(info.Name);
            var (name, from) = NameOf(info.Name, reading);
            slots.Add(new SlotInfo(info.Name, name, new DateTimeOffset(DateTime.SpecifyKind(info.LastWrittenUtc, DateTimeKind.Utc)), !reading.Loaded, from));
        }
        return [.. slots.OrderByDescending(s => s.LastPlayed).ThenBy(s => s.Slot, StringComparer.Ordinal)];
    }

    /// <summary>
    /// The name a new character is offered (Q2): "Character &lt;n&gt;" for the smallest n from 1 that no slot's character
    /// has (a damaged slot's by its backup) and whose slot would be free (<c>character-n</c>).
    /// </summary>
    public string NextDefaultName()
    {
        var names = Slots().Select(s => s.Name).ToHashSet(StringComparer.Ordinal);
        for (var n = 1; ; n++)
        {
            var name = $"Character {n}";
            if (!names.Contains(name) && !Saves.Exists(SlotNames.Candidate(SlotNames.Stem(name), 1)))
                return name;
        }
    }

    /// <summary>What is wrong with <paramref name="name"/> as a new character's name (the save's check: empty, longer than 80 characters, a control character), or null.</summary>
    public static string? NameProblem(string name) => SaveGame.NameProblem(name);

    /// <summary>
    /// Makes a new character named <paramref name="name"/> (kept as typed) in a new slot named from it, saves it at once,
    /// leaves the open one (saved) and opens it; gives the slot.
    /// </summary>
    /// <exception cref="ArgumentException">The name is refused (<see cref="NameProblem"/>).</exception>
    public string New(string name)
    {
        if (NameProblem(name) is { } problem)
            throw new ArgumentException(problem, nameof(name));
        var save = new SaveGame { Name = name };
        var slot = FreeSlot(name);
        Leave();
        Log.Info("Session", $"a new character in {slot}");
        Make(slot, save);
        return slot;
    }

    /// <summary>
    /// Opens <paramref name="slot"/>: its save read, the open character left (saved) and it opened; or, when its save
    /// can't be read (damaged, unreadable here, or missing beside its backup), nothing changes and the result has what the
    /// damaged-save question needs: the backup's state and time, and the name.
    /// </summary>
    public OpenResult Open(string slot)
    {
        var reading = Saves.Load<SaveGame>(slot);
        var (name, from) = NameOf(slot, reading);
        if (reading.Loaded)
        {
            Leave();
            Enter(slot, reading.Value);
            Log.Info("Session", $"opened {slot}");
            return new OpenResult(slot, true, Name: name, NameFrom: from);
        }
        var backup = reading.Backup!.State switch
        {
            SaveState.Missing => BackupState.Missing,
            SaveState.Readable => BackupState.Readable,
            _ => BackupState.Damaged,
        };
        var backupFile = new FileInfo(Saves.BackupPath(slot));
        DateTimeOffset? time = backupFile.Exists ? new DateTimeOffset(backupFile.LastWriteTimeUtc) : null;
        Log.Info("Session", $"{slot} can't be opened: its save is {reading.Save.State.ToString().ToLowerInvariant()}" +
            $"{(reading.Save.Problem is { } why ? $" ({why})" : "")}, its backup {backup.ToString().ToLowerInvariant()}");
        return new OpenResult(slot, false, backup, time, name, from);
    }

    /// <summary>The damaged slot's readable backup saved in its place (the damaged save kept aside by the engine, Q39 of M9), then opened.</summary>
    /// <exception cref="InvalidOperationException">The slot's save reads (nothing to answer), or its backup doesn't.</exception>
    public void UseBackup(string slot)
    {
        var reading = Damaged(slot);
        if (reading.Backup is not { State: SaveState.Readable } backup)
            throw new InvalidOperationException($"{slot}'s backup can't be read");
        Leave();
        Log.Info("Session", $"{slot}: its backup used");
        Make(slot, backup.Value);
    }

    /// <summary>
    /// A new character in the damaged slot, its name kept (Q21): the backup's when it reads, else the slot's file name
    /// (Q25; cut to the 80 characters a name may have), then opened; the damaged save kept aside by the engine.
    /// </summary>
    /// <exception cref="InvalidOperationException">The slot's save reads (nothing to answer).</exception>
    public void StartNew(string slot)
    {
        var reading = Damaged(slot);
        var name = NameOf(slot, reading).Name;
        if (name.Length > SaveGame.MaxNameLength)
            name = name[..SaveGame.MaxNameLength].TrimEnd('-');
        Leave();
        Log.Info("Session", $"{slot}: started new");
        Make(slot, new SaveGame { Name = name });
    }

    /// <summary>Removes <paramref name="slot"/>'s save and backup (Q41 of M9; files kept aside stay), closing it first if it is open (not saved).</summary>
    public void Delete(string slot)
    {
        if (CurrentSlot == slot)
        {
            (Current, CurrentSlot) = (null, null);
            Closed?.Invoke();
        }
        Saves.Delete(slot);
        Log.Info("Session", $"deleted {slot}");
        SlotsChanged?.Invoke();
        if (_settings.LastCharacter == slot)
            _settings.LastCharacter = null;
    }

    /// <summary>Changes the open character and saves it.</summary>
    /// <exception cref="InvalidOperationException">No character is open.</exception>
    /// <exception cref="ArgumentException">The changed save is one its checks refuse (nothing changes).</exception>
    public void Change(Func<SaveGame, SaveGame> update)
    {
        if (Current is null || CurrentSlot is null)
            throw new InvalidOperationException("no character is open");
        var changed = update(Current);
        if (RecordChecks.Check(changed) is [var problem, ..])
            throw new ArgumentException(problem.Message, nameof(update));
        Current = changed;
        Write(CurrentSlot, changed);
        Saved?.Invoke();
    }

    /// <summary>Saves and leaves the open character, if one is open.</summary>
    public void Leave()
    {
        if (Current is null || CurrentSlot is null)
            return;
        Write(CurrentSlot, Current);
        (Current, CurrentSlot) = (null, null);
        Closed?.Invoke();
    }

    /// <summary>
    /// Writes the open character to the character file <paramref name="file"/> (the save's JSON form, replacing a file
    /// there); whether it was written ("Character saved" or "Character could not be saved"). False with no character open.
    /// </summary>
    public bool Export(string file)
    {
        if (Current is null)
            return false;
        try
        {
            CharacterFile.Export(file, Current);
            Log.Info("Session", $"{CurrentSlot} saved to {file}");
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or RecordFormatException
            or ArgumentException or NotSupportedException)
        {
            Log.Warning("Session", $"{CurrentSlot} couldn't be saved to {file}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Loads the character file <paramref name="file"/> into a new slot under the name it holds (a number added to the
    /// slot when taken, Q9), leaves the open character (saved) and opens it; or nothing changes and the problem's words:
    /// <see cref="NotACharacterSave"/> for any file that isn't a <c>save.game</c> this game reads (an old game's file
    /// too, Q23), with the check's problem after it when its checks refuse it.
    /// </summary>
    public ImportResult Import(string file)
    {
        SaveGame save;
        try
        {
            save = CharacterFile.Import<SaveGame>(file);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or InvalidDataException or RecordFormatException
            or ArgumentException or NotSupportedException)
        {
            Log.Info("Session", $"{file} isn't a character: {e.Message}");
            return new ImportResult(null, NotACharacterSave);
        }
        if (RecordChecks.Check(save) is [var problem, ..])
        {
            Log.Info("Session", $"{file} refused: {problem}");
            return new ImportResult(null, $"{NotACharacterSave}: {problem.Message}");
        }
        var slot = FreeSlot(save.Name);
        Leave();
        Log.Info("Session", $"{file} loaded into {slot}");
        Make(slot, save);
        return new ImportResult(slot, null);
    }

    /// <summary>A slot's name and where it came from: the save's, else the backup's when it reads, else the slot's file name.</summary>
    static (string Name, SlotNameSource From) NameOf(string slot, SaveSlotReading<SaveGame> reading) =>
        reading.Loaded ? (reading.Value.Name, SlotNameSource.Save)
        : reading.Backup is { State: SaveState.Readable } backup ? (backup.Value.Name, SlotNameSource.Backup)
        : (slot, SlotNameSource.File);

    /// <summary>The slot read for the damaged-save question's answers; refused when its save reads.</summary>
    SaveSlotReading<SaveGame> Damaged(string slot)
    {
        var reading = Saves.Load<SaveGame>(slot);
        if (reading.Loaded)
            throw new InvalidOperationException($"{slot}'s save reads: it opens, there is nothing to answer");
        return reading;
    }

    /// <summary>A slot made or rewritten with <paramref name="save"/> and opened (the open character already left).</summary>
    void Make(string slot, SaveGame save)
    {
        Write(slot, save);
        Enter(slot, save);
        Saved?.Invoke();
    }

    void Enter(string slot, SaveGame save)
    {
        (Current, CurrentSlot) = (save, slot);
        _settings.LastCharacter = slot;
        Opened?.Invoke();
    }

    void Write(string slot, SaveGame save)
    {
        Saves.Save(slot, save);
        SlotsChanged?.Invoke();
    }

    /// <summary>The slot for a character named <paramref name="name"/>: <see cref="SlotNames.Free"/>, a slot taken when its save or backup is there.</summary>
    string FreeSlot(string name) => SlotNames.Free(name, Saves.Exists);
}
