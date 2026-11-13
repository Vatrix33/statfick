using System.Text.RegularExpressions;
using Statventure.Game.Data.Saves;
using Statventure.Game.Settings;
using Statventure.Storage;

namespace Statventure.Game.Session;

/// <summary>A save slot as the slot list shows it.</summary>
/// <param name="Slot">The slot's name, its file <c>saves/&lt;slot&gt;.dat</c> (named from the character's name, Q1).</param>
/// <param name="Name">The character's name as the player gave it; null when the save can't be read.</param>
/// <param name="LastPlayed">When it was last written.</param>
/// <param name="Damaged">Whether its save can't be read (the slot list shows its file and "damaged").</param>
public sealed record SlotInfo(string Slot, string? Name, DateTimeOffset LastPlayed, bool Damaged);

/// <summary>A damaged slot's backup (Q17 of M9, Q21).</summary>
public enum BackupState
{
    Missing,
    Readable,
    Damaged,
}

/// <summary>What opening a slot gave: opened, or damaged with its backup's state and time (the damaged-save question).</summary>
public sealed record OpenResult(string Slot, bool Opened, BackupState Backup = BackupState.Missing, DateTimeOffset? BackupTime = null);

/// <summary>What loading a character file gave: the new slot it opened in, or the problem's words ("That file is not a valid character save", Q23).</summary>
public sealed record ImportResult(string? Slot, string? Problem);

/// <summary>
/// The save slots and the open character (Docs/Game/Foundation.md, "The program's flow"; step 5): the slots listed, a new
/// character made and saved, a slot opened (or the damaged-save question's facts), every change saved, the character
/// left (saved), character files written and read. Uses the engine's storage (<see cref="GameStorage.Saves"/>) and
/// <see cref="GameSettings.LastCharacter"/>. <b>Step 2's stub:</b> works in memory (no file is read or written); its
/// slot names are a placeholder for step 5's rule; character files are refused.
/// </summary>
public sealed class GameSession
{
    readonly GameSettings _settings;
    readonly Dictionary<string, (SaveGame Save, DateTimeOffset Played)> _slots = new(StringComparer.Ordinal);

    /// <summary>The session over <paramref name="storage"/>'s slots (the stub keeps its own in memory).</summary>
    public GameSession(GameStorage storage, GameSettings settings)
    {
        _settings = settings;
    }

    /// <summary>The open character's save, or null.</summary>
    public SaveGame? Current { get; private set; }

    /// <summary>The open character's slot, or null.</summary>
    public string? CurrentSlot { get; private set; }

    /// <summary>A character was opened.</summary>
    public event Action? Opened;
    /// <summary>The open character was left.</summary>
    public event Action? Closed;
    /// <summary>The open character was saved to its slot (the autosave dot).</summary>
    public event Action? Saved;
    /// <summary>A slot was made, written or deleted.</summary>
    public event Action? SlotsChanged;

    /// <summary>Every slot, the last played first.</summary>
    public IReadOnlyList<SlotInfo> Slots() =>
        _slots.Select(s => new SlotInfo(s.Key, s.Value.Save.Name, s.Value.Played, false)).OrderByDescending(s => s.LastPlayed).ToList();

    /// <summary>The name a new character is offered: the next free "Character &lt;n&gt;" (Q2).</summary>
    public string NextDefaultName()
    {
        for (var n = 1; ; n++)
            if (!_slots.Values.Any(s => s.Save.Name == $"Character {n}"))
                return $"Character {n}";
    }

    /// <summary>Makes a new character named <paramref name="name"/> in a new slot, saves it and opens it; gives the slot.</summary>
    public string New(string name)
    {
        var slot = FreeSlot(name);
        Leave();
        Write(slot, new SaveGame { Name = name });
        SlotsChanged?.Invoke();
        return Enter(slot);
    }

    /// <summary>Opens <paramref name="slot"/>: opened, or damaged (the stub's only damaged slot is one that isn't there).</summary>
    public OpenResult Open(string slot)
    {
        if (!_slots.ContainsKey(slot))
            return new OpenResult(slot, false);
        Leave();
        Enter(slot);
        return new OpenResult(slot, true);
    }

    /// <summary>The damaged slot's backup put in its place, then opened.</summary>
    public void UseBackup(string slot) => Open(slot);

    /// <summary>A new character in the damaged slot, the name kept, then opened.</summary>
    public void StartNew(string slot)
    {
        var name = _slots.TryGetValue(slot, out var held) ? held.Save.Name : slot;
        Leave();
        Write(slot, new SaveGame { Name = name });
        SlotsChanged?.Invoke();
        Enter(slot);
    }

    /// <summary>Removes <paramref name="slot"/>'s save and backup.</summary>
    public void Delete(string slot)
    {
        if (CurrentSlot == slot)
        {
            (Current, CurrentSlot) = (null, null);
            Closed?.Invoke();
        }
        if (_slots.Remove(slot))
            SlotsChanged?.Invoke();
        if (_settings.LastCharacter == slot)
            _settings.LastCharacter = null;
    }

    /// <summary>Changes the open character and saves it.</summary>
    public void Change(Func<SaveGame, SaveGame> update)
    {
        if (Current is null || CurrentSlot is null)
            throw new InvalidOperationException("no character is open");
        Current = update(Current);
        Write(CurrentSlot, Current);
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

    /// <summary>Writes the open character to a character file; whether it was written (the stub writes none).</summary>
    public bool Export(string file) => false;

    /// <summary>Reads a character file into a new slot and opens it (the stub reads none).</summary>
    public ImportResult Import(string file) => new(null, "That file is not a valid character save");

    string Enter(string slot)
    {
        (Current, CurrentSlot) = (_slots[slot].Save, slot);
        _settings.LastCharacter = slot;
        Opened?.Invoke();
        return slot;
    }

    void Write(string slot, SaveGame save) => _slots[slot] = (save, DateTimeOffset.Now);

    /// <summary>The stub's slot name: the name's letters and digits, lowercased, joined by '-'; a number added when taken (step 5 makes the rule).</summary>
    string FreeSlot(string name)
    {
        var words = Regex.Replace(name.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        var stem = words.Length > 0 ? words : "character";
        var slot = stem;
        for (var n = 2; _slots.ContainsKey(slot); n++)
            slot = $"{stem}-{n}";
        return slot;
    }
}
