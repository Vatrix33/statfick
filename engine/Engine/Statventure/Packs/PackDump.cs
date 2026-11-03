using System.Buffers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Statventure.Packs;

/// <summary>
/// A pack as text for review (Docs/Assets.md, "Reviewing packs"; Q14, S3-2): what <c>packer dump</c>
/// prints and git's diff driver shows. One JSON document: the format version and the entry count,
/// then each entry in path order with its path, kind (<c>record</c> or <c>file</c>), content length,
/// stored form (<c>brotli</c> or <c>raw</c>) and stored length, and SHA-256; a record also its JSON form (in
/// the current versions, as <see cref="PackFile.ExtractTo"/> writes it); a file that is UTF-8 text
/// without control characters but tab, CR and LF also its text as an array of lines (split at LF);
/// other files nothing more. Offsets are left out (they move whenever an entry before them changes).
/// An entry that can't be read, or a record of a type not known here, shows its problem instead.
/// </summary>
public static class PackDump
{
    static readonly UTF8Encoding Strict = new(false, true);

    /// <summary>The dump as text (LF line ends, a final line end).</summary>
    public static string ToText(PackFile pack, RecordTypes? types = null) => Encoding.UTF8.GetString(ToUtf8(pack, types));

    /// <summary>The dump as UTF-8 bytes.</summary>
    public static byte[] ToUtf8(PackFile pack, RecordTypes? types = null)
    {
        types ??= RecordTypes.Engine;
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions
        {
            Indented = true,
            NewLine = "\n",
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }))
        {
            json.WriteStartObject();
            json.WriteNumber("format"u8, PackFormat.Version);
            json.WriteNumber("count"u8, pack.Entries.Count);
            json.WriteStartArray("entries"u8);
            foreach (var entry in pack.Entries)
                WriteEntry(json, pack, entry, types);
            json.WriteEndArray();
            json.WriteEndObject();
        }
        buffer.Write("\n"u8);
        return buffer.WrittenSpan.ToArray();
    }

    /// <summary>How deep an entry's members are indented: the top object, the entries array, the entry.</summary>
    const string EntryIndent = "      ";

    static void WriteEntry(Utf8JsonWriter json, PackFile pack, PackEntry entry, RecordTypes types)
    {
        json.WriteStartObject();
        json.WriteString("path"u8, entry.Path);
        json.WriteString("kind"u8, entry.IsRecord ? "record"u8 : "file"u8);
        json.WriteNumber("length"u8, entry.ContentLength);
        json.WriteString("stored"u8, entry.Compressed ? "brotli"u8 : "raw"u8);
        json.WriteNumber("storedLength"u8, entry.StoredLength);
        json.WriteString("sha256"u8, entry.HashHex);
        try
        {
            if (entry.IsRecord)
            {
                // The record's own JSON form, its lines indented to sit inside the entry.
                var text = Encoding.UTF8.GetString(pack.RecordJson(entry, types)).TrimEnd('\n');
                json.WritePropertyName("record"u8);
                json.WriteRawValue(text.Replace("\n", "\n" + EntryIndent), skipInputValidation: true);
            }
            else if (Lines(pack.Read(entry)) is { } lines)
            {
                json.WriteStartArray("text"u8);
                foreach (var line in lines)
                    json.WriteStringValue(line);
                json.WriteEndArray();
            }
        }
        catch (DataFileException problem)
        {
            json.WriteString("problem"u8, problem.Problem);
        }
        json.WriteEndObject();
    }

    /// <summary>The content's lines (split at LF) when it is UTF-8 text without control characters but tab, CR and LF; else null.</summary>
    static string[]? Lines(byte[] content)
    {
        string text;
        try
        {
            text = Strict.GetString(content);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
        foreach (var c in text)
            if (char.IsControl(c) && c is not ('\t' or '\r' or '\n'))
                return null;
        return text.Split('\n');
    }
}
