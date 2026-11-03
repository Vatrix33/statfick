using System.Buffers.Binary;
using System.Security.Cryptography;
using Statventure.Records;

namespace Statventure.Storage;

/// <summary>What a save file (or its backup) turned out to be when read.</summary>
public enum SaveState
{
    /// <summary>There is no such file.</summary>
    Missing,

    /// <summary>Intact, and its record was read.</summary>
    Readable,

    /// <summary>Not as it was written: too short, another magic, a length or hash that doesn't match.</summary>
    Damaged,

    /// <summary>
    /// This engine can't read it though nothing shows it damaged: a newer format or record version, a
    /// record of another type, or a file the system won't let it read.
    /// </summary>
    Unreadable,
}

/// <summary>
/// The save file format (Docs/Assets.md, "A save file"; Q38): a 48-byte header, little-endian, then
/// exactly the payload, the record's binary form (its type table, then its fields) through the
/// serializer, not compressed.
/// <code>
/// 0–3    SVSV
/// 4–7    format version (u32), 1
/// 8–15   the payload's length (u64)
/// 16–47  SHA-256 of the payload
/// </code>
/// </summary>
public static class SaveFile
{
    public const int HeaderLength = 48;
    public const uint FormatVersion = 1;

    /// <summary>The first four bytes of every save file.</summary>
    public static ReadOnlySpan<byte> Magic => "SVSV"u8;

    /// <summary>A save file holding <paramref name="value"/> (a record of type <typeparamref name="T"/>, in its binary form).</summary>
    public static byte[] Write<T>(T value) where T : notnull => Wrap(RecordSerializer.ToBinary(value));

    /// <summary>A save file around a payload: the header, then the payload.</summary>
    public static byte[] Wrap(ReadOnlySpan<byte> payload)
    {
        var file = new byte[HeaderLength + payload.Length];
        Magic.CopyTo(file);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(4), FormatVersion);
        BinaryPrimitives.WriteUInt64LittleEndian(file.AsSpan(8), (ulong)payload.Length);
        SHA256.HashData(payload, file.AsSpan(16, 32));
        payload.CopyTo(file.AsSpan(HeaderLength));
        return file;
    }

    /// <summary>
    /// Checks a save file's header and hash: <see cref="SaveState.Readable"/> with the payload's place
    /// when it is intact, else <see cref="SaveState.Damaged"/> or (a newer format)
    /// <see cref="SaveState.Unreadable"/>, with what is wrong.
    /// </summary>
    public static SaveState Check(ReadOnlySpan<byte> file, out Range payload, out string? problem)
    {
        payload = default;
        if (file.Length < HeaderLength)
            return Fail(SaveState.Damaged, $"it has {file.Length} bytes, fewer than the {HeaderLength}-byte header", out problem);
        if (!file.StartsWith(Magic))
            return Fail(SaveState.Damaged, "it doesn't start with SVSV, so it isn't a save file", out problem);
        var version = BinaryPrimitives.ReadUInt32LittleEndian(file[4..]);
        if (version > FormatVersion)
            return Fail(SaveState.Unreadable, $"its format version {version} is newer than this engine reads ({FormatVersion})", out problem);
        if (version != FormatVersion)
            return Fail(SaveState.Damaged, $"its format version is {version}", out problem);
        var length = BinaryPrimitives.ReadUInt64LittleEndian(file[8..]);
        if (length != (ulong)(file.Length - HeaderLength))
            return Fail(SaveState.Damaged, $"its header gives a payload of {length} bytes, but {file.Length - HeaderLength} follow it", out problem);
        Span<byte> hash = stackalloc byte[32];
        SHA256.HashData(file[HeaderLength..], hash);
        if (!hash.SequenceEqual(file.Slice(16, 32)))
            return Fail(SaveState.Damaged, "its payload doesn't match its SHA-256", out problem);
        payload = HeaderLength..file.Length;
        problem = null;
        return SaveState.Readable;
    }

    /// <summary>
    /// Reads a save file as a record of type <typeparamref name="T"/>, upgraded to the current
    /// version: its state, and the value when it is <see cref="SaveState.Readable"/>. Never throws for
    /// what the file holds or whether it can be read.
    /// </summary>
    public static SaveReading<T> Read<T>(string file) where T : notnull
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(file);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return new SaveReading<T>(SaveState.Missing, default, null);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return new SaveReading<T>(SaveState.Unreadable, default, $"it can't be read: {e.Message}");
        }
        return Read<T>(bytes);
    }

    /// <summary>Reads a save file's bytes as a record of type <typeparamref name="T"/> (see <see cref="Read{T}(string)"/>).</summary>
    public static SaveReading<T> Read<T>(byte[] bytes) where T : notnull
    {
        var state = Check(bytes, out var payload, out var problem);
        if (state != SaveState.Readable)
            return new SaveReading<T>(state, default, problem);
        try
        {
            return new SaveReading<T>(SaveState.Readable, RecordSerializer.FromBinary<T>(bytes.AsSpan(payload)), null);
        }
        catch (RecordFormatException e)
        {
            // Intact (its hash matches), so it is as it was written: this engine can't read it.
            return new SaveReading<T>(SaveState.Unreadable, default, $"its record can't be read: {e.Message}");
        }
    }

    static SaveState Fail(SaveState state, string reason, out string? problem)
    {
        problem = reason;
        return state;
    }
}

/// <summary>One save file read: its <see cref="State"/>, the record when it is readable, else what is wrong.</summary>
public sealed class SaveReading<T>(SaveState state, T? value, string? problem) where T : notnull
{
    public SaveState State { get; } = state;

    /// <summary>The record, when <see cref="State"/> is <see cref="SaveState.Readable"/>.</summary>
    public T Value => State == SaveState.Readable ? value! : throw new InvalidOperationException($"the save is {State.ToString().ToLowerInvariant()}");

    /// <summary>What is wrong when it is damaged or unreadable; null otherwise.</summary>
    public string? Problem { get; } = problem;
}
