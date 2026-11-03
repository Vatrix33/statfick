using System.Text.Json;
using Statventure.Records;

namespace Statventure.Packs;

/// <summary>
/// A record's loose form (Docs/Assets.md, Q7 and S3-1): its JSON form in the file <c>&lt;path&gt;.json</c>.
/// Which <c>.json</c> files are records is told by content, by one rule for the packer's input, mounted
/// loose folders and extracted packs: a top-level object whose first key is <c>"$types"</c> (as every
/// record's JSON form starts) is a record; any other JSON is a file; a start that isn't valid JSON is
/// an error, since it can't be told.
/// </summary>
internal static class LooseRecord
{
    public const string Extension = ".json";

    static ReadOnlySpan<byte> ByteOrderMark => [0xEF, 0xBB, 0xBF];

    /// <summary>
    /// The entry whose loose file would be the same as <paramref name="path"/>'s: for a record its file
    /// <c>&lt;path&gt;.json</c>, for a file named <c>&lt;x&gt;.json</c> the record <c>&lt;x&gt;</c>; null for other files.
    /// </summary>
    public static string? Twin(string path, bool record) =>
        record ? path + Extension
        : path.EndsWith(Extension, StringComparison.Ordinal) && path.Length > Extension.Length ? path[..^Extension.Length]
        : null;

    /// <summary>Whether a loose file at <paramref name="path"/> is looked at for a record: its name ends in ".json".</summary>
    public static bool MayBeRecord(string path) => path.EndsWith(Extension, StringComparison.Ordinal);

    /// <summary>Whether <paramref name="content"/> starts as a record's JSON form; null when its start isn't valid JSON (<paramref name="problem"/> says why).</summary>
    public static bool? StartsAsRecord(ReadOnlySpan<byte> content, out string? problem) => Decide(content, final: true, out problem);

    /// <summary>
    /// Whether the file starts as a record's JSON form, reading only as much of it as that takes; a
    /// start that isn't valid JSON throws a <see cref="DataFileException"/> naming the file.
    /// </summary>
    public static bool FileStartsAsRecord(string file)
    {
        using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1, FileOptions.SequentialScan);
        return StartsAsRecord(stream, file);
    }

    /// <summary>
    /// <see cref="FileStartsAsRecord"/> on a stream, which may give fewer bytes than asked for at a time
    /// (as some file systems do); <paramref name="file"/> names it in the error.
    /// </summary>
    internal static bool StartsAsRecord(Stream stream, string file)
    {
        var buffer = new byte[256];
        var length = 0;
        while (true)
        {
            if (length == buffer.Length)
                Array.Resize(ref buffer, buffer.Length * 2);
            var read = stream.Read(buffer, length, buffer.Length - length);
            length += read;
            var decided = Decide(buffer.AsSpan(0, length), final: read == 0, out var problem);
            if (decided is { } record)
                return record;
            if (problem is not null)
                throw new DataFileException(file, null, problem);
        }
    }

    /// <summary>
    /// Whether the data starts as a record's JSON form; null with no <paramref name="problem"/> when
    /// more of it is needed (only when not <paramref name="final"/>), null with a problem when its start
    /// isn't valid JSON.
    /// </summary>
    static bool? Decide(ReadOnlySpan<byte> data, bool final, out string? problem)
    {
        problem = null;
        if (data.StartsWith(ByteOrderMark))
            data = data[ByteOrderMark.Length..];
        else if (!final && data.Length < ByteOrderMark.Length && ByteOrderMark.StartsWith(data))
            return null;
        var reader = new Utf8JsonReader(data, isFinalBlock: final, state: default);
        try
        {
            if (!reader.Read())
            {
                if (final)
                    problem = Unclear("no JSON value in it");
                return null;
            }
            if (reader.TokenType != JsonTokenType.StartObject)
                return false;
            if (!reader.Read())
            {
                if (final)
                    problem = Unclear("an object that doesn't end");
                return null;
            }
            return reader.TokenType == JsonTokenType.PropertyName && reader.ValueTextEquals("$types"u8);
        }
        catch (JsonException e)
        {
            problem = Unclear(e.Message);
            return null;
        }
    }

    static string Unclear(string why) => $"a .json file whose start isn't valid JSON, so it can't be told whether it is a record ({why})";

    /// <summary>The record type a JSON form holds: the first name in its <c>"$types"</c>.</summary>
    public static string RootTypeName(ReadOnlySpan<byte> utf8)
    {
        if (utf8.StartsWith(ByteOrderMark))
            utf8 = utf8[ByteOrderMark.Length..];
        var reader = new Utf8JsonReader(utf8, new JsonReaderOptions { MaxDepth = 1000 });
        try
        {
            if (reader.Read() && reader.TokenType == JsonTokenType.StartObject)
            {
                while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                {
                    if (!reader.ValueTextEquals("$types"u8))
                    {
                        reader.Skip();
                        continue;
                    }
                    if (reader.Read() && reader.TokenType == JsonTokenType.StartObject
                        && reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
                        return reader.GetString()!;
                    break;
                }
            }
        }
        catch (JsonException e)
        {
            throw new RecordFormatException($"not valid JSON: {e.Message}", e);
        }
        throw new RecordFormatException("expected an object with \"$types\", an object of record type names and their versions");
    }
}
