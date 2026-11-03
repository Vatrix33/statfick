namespace Statventure.Packs;

/// <summary>What an entry's flags in a pack's index say.</summary>
[Flags]
public enum PackEntryFlags : uint
{
    None = 0,
    /// <summary>Stored Brotli-compressed; otherwise stored as it is.</summary>
    Compressed = 1,
}

/// <summary>
/// The byte layout of a pack, format version 1 (Docs/Assets.md, "How it works"; Q9). Little-endian.
/// The header (64 bytes): "SVPK", the version (u32), flags (u32, none defined), the entry count
/// (u32), the index's offset and length (u64 each), the index's SHA-256. Then the entries' stored
/// bytes one after another in path order, then the index: per entry the path's length (u16) and its
/// ASCII bytes, the offset, stored length and content length (u64 each), the flags (u32) and the
/// SHA-256 of the content.
/// </summary>
internal static class PackFormat
{
    public static ReadOnlySpan<byte> Magic => "SVPK"u8;
    public const uint Version = 1;
    public const int HeaderSize = 64;
    public const int VersionAt = 4;
    public const int FlagsAt = 8;
    public const int CountAt = 12;
    public const int IndexOffsetAt = 16;
    public const int IndexLengthAt = 24;
    public const int IndexHashAt = 32;
    public const int HashSize = 32;
    /// <summary>An index entry's size without its path's bytes.</summary>
    public const int EntrySize = 2 + 8 + 8 + 8 + 4 + HashSize;
    public const PackEntryFlags KnownFlags = PackEntryFlags.Compressed;
    /// <summary>Brotli's settings: the best and slowest quality, a 4 MiB window; the output is the same every time.</summary>
    public const int BrotliQuality = 11;
    public const int BrotliWindow = 22;
}
