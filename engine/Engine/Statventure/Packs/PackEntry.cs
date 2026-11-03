namespace Statventure.Packs;

/// <summary>One entry of a pack's index.</summary>
public sealed class PackEntry
{
    readonly byte[] _hash;

    internal PackEntry(string path, long offset, long storedLength, long contentLength, PackEntryFlags flags, byte[] hash)
    {
        Path = path;
        Offset = offset;
        StoredLength = storedLength;
        ContentLength = contentLength;
        Flags = flags;
        _hash = hash;
    }

    public string Path { get; }

    /// <summary>Where its stored bytes start in the pack.</summary>
    public long Offset { get; }

    /// <summary>The bytes it takes in the pack (compressed or not).</summary>
    public long StoredLength { get; }

    /// <summary>The length of its content.</summary>
    public long ContentLength { get; }

    public PackEntryFlags Flags { get; }

    public bool Compressed => (Flags & PackEntryFlags.Compressed) != 0;

    /// <summary>Whether it is a record (its content a record's binary form) rather than a file.</summary>
    public bool IsRecord => (Flags & PackEntryFlags.Record) != 0;

    /// <summary>The SHA-256 of its content (uncompressed: the same however it is stored; Q5).</summary>
    public ReadOnlySpan<byte> Hash => _hash;

    /// <summary><see cref="Hash"/> as lowercase hexadecimal.</summary>
    public string HashHex => Convert.ToHexStringLower(_hash);
}
