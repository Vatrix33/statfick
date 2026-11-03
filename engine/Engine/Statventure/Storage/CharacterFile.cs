using Statventure.Records;

namespace Statventure.Storage;

/// <summary>
/// Character files (Docs/Assets.md, "Character files"; Q19): Save Character writes the save record's
/// JSON form (indented, with its type and version table), Load Character reads one back, upgrading
/// an older version; the game then saves it into the slot the player picks. The picker screen is the
/// port's; its default is <see cref="DefaultName"/> in the Documents folder
/// (<see cref="DocumentsFolder"/>), as the old game's dialog. As the old game, a character of more
/// than <see cref="MaxBytes"/> is refused both ways.
/// </summary>
public static class CharacterFile
{
    /// <summary>The old game's default file name.</summary>
    public const string DefaultName = "stat-fick-character.json";

    /// <summary>The old game's limit (<c>desktop/main.cjs</c>: 10 MiB, more is refused when saving and loading).</summary>
    public const long MaxBytes = 10 * 1024 * 1024;

    /// <summary>
    /// Writes <paramref name="value"/>'s JSON form to <paramref name="file"/>, replacing a file there: into a
    /// file beside it first, flushed to the disk, then renamed over it, so a failure leaves the old one.
    /// </summary>
    /// <exception cref="InvalidDataException">The JSON form is longer than <see cref="MaxBytes"/>.</exception>
    /// <exception cref="RecordFormatException">The value isn't one its declaration allows.</exception>
    public static void Export<T>(string file, T value) where T : notnull
    {
        var json = RecordSerializer.ToJsonUtf8(typeof(T), value);
        if (json.Length > MaxBytes)
            throw new InvalidDataException($"the character's JSON form has {json.Length} bytes, more than the {MaxBytes} a character file may have");
        var full = Path.GetFullPath(file);
        var temporary = Path.Combine(Path.GetDirectoryName(full)!, $"{Path.GetFileName(full)}.{Path.GetRandomFileName()}.tmp");
        try
        {
            StorageFiles.WriteFlushed(temporary, json);
            File.Move(temporary, full, overwrite: true);
        }
        catch
        {
            File.Delete(temporary);
            throw;
        }
    }

    /// <summary>Reads a character file as a record of type <typeparamref name="T"/>, upgraded to the current version.</summary>
    /// <exception cref="InvalidDataException">The file is larger than <see cref="MaxBytes"/>.</exception>
    /// <exception cref="RecordFormatException">It isn't a valid JSON form of the record.</exception>
    /// <exception cref="IOException">It can't be read.</exception>
    public static T Import<T>(string file) where T : notnull
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > MaxBytes)
            throw new InvalidDataException($"{file} has {stream.Length} bytes, more than the {MaxBytes} a character file may have");
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return RecordSerializer.FromJson<T>(bytes);
    }
}
