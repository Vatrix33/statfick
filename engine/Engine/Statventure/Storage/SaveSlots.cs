using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Storage;

/// <summary>A save slot as the folder shows it: its name, which of its files are there, and when it was last written.</summary>
public sealed record SaveSlotInfo(string Name, bool HasSave, bool HasBackup, DateTime LastWrittenUtc);

/// <summary>
/// A slot read: the save's reading and, when the save isn't readable, the backup's (Docs/Assets.md,
/// "A damaged or unreadable save"; Q17): nothing is loaded then; the game asks the player whether to
/// use the backup (save <see cref="Backup"/>'s value into the slot) or start new (save a new record).
/// </summary>
public sealed class SaveSlotReading<T>(string name, SaveReading<T> save, SaveReading<T>? backup) where T : notnull
{
    public string Name { get; } = name;

    public SaveReading<T> Save { get; } = save;

    /// <summary>The backup's reading; null when the save was readable (the backup isn't read then).</summary>
    public SaveReading<T>? Backup { get; } = backup;

    /// <summary>Whether the save was read (else nothing is loaded).</summary>
    public bool Loaded => Save.State == SaveState.Readable;

    /// <summary>The slot's record, when <see cref="Loaded"/>.</summary>
    public T Value => Save.Value;
}

/// <summary>
/// The named save slots (Docs/Assets.md, "Save slots"; Q15–Q17, Q39–Q41): in the game folder's
/// <c>saves/</c>, each slot <c>&lt;name&gt;.dat</c> with its backup <c>&lt;name&gt;.dat.bak</c>; free
/// names the player gives, under the path rule and at most <see cref="MaxNameLength"/> characters.
/// The slots are what the folder holds (no fixed count); the slot screen is the port's.
/// <para>
/// <see cref="Save{T}"/> serializes on the caller's thread and leaves the writing to the background
/// writer (the latest state of a slot wins): the new file is written beside the save
/// (<c>.dat.tmp</c>, flushed to the disk), then put in place with <see cref="File.Replace(string, string, string?)"/>,
/// the previous save becoming the backup. A save or backup this engine can't read is never replaced:
/// it is kept as <c>&lt;file&gt;.damaged</c> (<c>-2</c>, <c>-3</c> … when taken) and the readable
/// backup stays (Q39). Loading only reports.
/// </para>
/// </summary>
public sealed class SaveSlots
{
    /// <summary>The longest slot name (Q40): every file of a slot then fits the systems' 255-character file names.</summary>
    public const int MaxNameLength = 100;

    public const string SaveExtension = ".dat";
    public const string BackupExtension = ".dat.bak";
    const string TemporaryExtension = ".dat.tmp";

    readonly BackgroundWriter _writer;

    internal SaveSlots(string folder, BackgroundWriter writer)
    {
        Folder = folder;
        _writer = writer;
    }

    /// <summary>The <c>saves/</c> folder (made by the first save).</summary>
    public string Folder { get; }

    /// <summary>What in <paramref name="name"/> breaks the slot name rule, or null: one name under the path rule (Q4, Q28), at most <see cref="MaxNameLength"/> characters.</summary>
    public static string? NameProblem(string name)
    {
        if (name.Contains('/'))
            return "a slot name can't hold '/'";
        if (name.Length > MaxNameLength)
            return $"a slot name has at most {MaxNameLength} characters";
        return PackPath.Problem(name);
    }

    public static bool IsValidName(string name) => NameProblem(name) is null;

    /// <summary>The slots in the folder (a slot is there when its save or its backup is), in ordinal order of their names; pending saves are written first.</summary>
    public IReadOnlyList<SaveSlotInfo> List()
    {
        _writer.Flush();
        var names = new SortedSet<string>(StringComparer.Ordinal);
        if (Directory.Exists(Folder))
            foreach (var file in Directory.EnumerateFiles(Folder))
            {
                var fileName = Path.GetFileName(file);
                foreach (var extension in new[] { SaveExtension, BackupExtension })
                    if (fileName.EndsWith(extension, StringComparison.Ordinal) && fileName[..^extension.Length] is var name && IsValidName(name))
                        names.Add(name);
            }
        return [.. names.Select(Info)];
    }

    SaveSlotInfo Info(string name)
    {
        var save = new FileInfo(SavePath(name));
        var backup = new FileInfo(BackupPath(name));
        return new SaveSlotInfo(name, save.Exists, backup.Exists, save.Exists ? save.LastWriteTimeUtc : backup.LastWriteTimeUtc);
    }

    /// <summary>Whether the slot has a save or a backup (after its pending save is written).</summary>
    public bool Exists(string name)
    {
        var save = SavePath(name);
        _writer.WaitFor(save);
        return File.Exists(save) || File.Exists(BackupPath(name));
    }

    /// <summary>
    /// Reads the slot as a record of type <typeparamref name="T"/> (after its pending save is written):
    /// the save, or when it isn't readable (missing, damaged or unreadable) nothing, with the backup's
    /// reading (Q17). No file is changed.
    /// </summary>
    public SaveSlotReading<T> Load<T>(string name) where T : notnull
    {
        var save = SavePath(name);
        _writer.WaitFor(save);
        var reading = SaveFile.Read<T>(save);
        return new SaveSlotReading<T>(name, reading, reading.State == SaveState.Readable ? null : SaveFile.Read<T>(BackupPath(name)));
    }

    /// <summary>
    /// Saves <paramref name="value"/> into the slot: serialized now (a value its declaration doesn't
    /// allow throws a <see cref="RecordFormatException"/> here), written by the background writer.
    /// </summary>
    public void Save<T>(string name, T value) where T : notnull
    {
        var save = SavePath(name);
        var file = SaveFile.Write(value);
        _writer.Enqueue(save, () => Write<T>(name, file));
    }

    /// <summary>
    /// Deletes the slot's save and backup (Q41), dropping its pending save; files kept aside stay.
    /// Done at once, on the caller's thread.
    /// </summary>
    public void Delete(string name)
    {
        var save = SavePath(name);
        _writer.Discard(save);
        if (!Directory.Exists(Folder))
            return;
        File.Delete(save);
        File.Delete(BackupPath(name));
        File.Delete(Path.Combine(Folder, name + TemporaryExtension));
    }

    /// <summary>The save's file (<c>saves/&lt;name&gt;.dat</c>).</summary>
    public string SavePath(string name) => Path.Combine(Folder, Checked(name) + SaveExtension);

    /// <summary>The backup's file (<c>saves/&lt;name&gt;.dat.bak</c>).</summary>
    public string BackupPath(string name) => Path.Combine(Folder, Checked(name) + BackupExtension);

    static string Checked(string name) =>
        NameProblem(name) is { } problem ? throw new ArgumentException($"'{name}' isn't a valid slot name: {problem}", nameof(name)) : name;

    /// <summary>The background writer's work for one save: the new file beside the save, then put in place.</summary>
    void Write<T>(string name, byte[] file) where T : notnull
    {
        StorageFiles.CreateFolder(Folder);
        var save = SavePath(name);
        var backup = BackupPath(name);
        var temporary = Path.Combine(Folder, name + TemporaryExtension);
        StorageFiles.WriteFlushed(temporary, file);
        switch (SaveFile.Read<T>(save).State)
        {
            case SaveState.Missing:
                // A first save, or a slot left with only its backup: the backup stays.
                File.Move(temporary, save);
                break;
            case SaveState.Readable:
                if (SaveFile.Read<T>(backup).State is SaveState.Damaged or SaveState.Unreadable)
                    File.Move(backup, StorageFiles.KeptAsideName(backup));
                File.Replace(temporary, save, backup, ignoreMetadataErrors: true);
                break;
            default:
                // The save can't be read: kept aside (the replaced file's "backup"), the backup left as it is.
                File.Replace(temporary, save, StorageFiles.KeptAsideName(save), ignoreMetadataErrors: true);
                break;
        }
    }
}
