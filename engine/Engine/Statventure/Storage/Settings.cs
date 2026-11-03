using System.Buffers;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Statventure.Core;

namespace Statventure.Storage;

/// <summary>
/// The player's settings (Docs/Assets.md, "Settings"; Q18, Q42, Q43): <c>settings.json</c> in the
/// game folder, <c>{ "version": n, … }</c>, read with System.Text.Json into an object that keeps
/// every key, so keys this game doesn't know survive its writes. Typed <see cref="Get{T}"/> and
/// <see cref="Set{T}"/>: a missing key gives the default the game passes, as does a value that
/// doesn't convert (logged once; the other settings are used, Q43); enums are their camelCase names,
/// as in the records' JSON form. Which settings exist is the game's: it gives its current version and
/// an upgrade for each past one, applied in turn to an older file. A file that doesn't parse, isn't
/// an object with an integer <c>version</c> of at least 1, or was written by a newer game (Q42) gives
/// the defaults; it is kept as <c>settings.broken.json</c> and logged (Q18). A changed value is
/// written by the background writer (the latest state wins), into a file beside it that then takes
/// its place.
/// </summary>
public sealed class Settings
{
    /// <summary>The key of the file's version, kept by the engine (not a setting).</summary>
    public const string VersionKey = "version";

    static readonly JavaScriptEncoder Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

    static readonly JsonSerializerOptions ValueOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
        NumberHandling = JsonNumberHandling.Strict,
        Encoder = Encoder,
    };

    readonly object _gate = new();
    readonly JsonObject _values;
    readonly HashSet<string> _warned = new(StringComparer.Ordinal);
    readonly BackgroundWriter _writer;

    Settings(string file, int version, JsonObject values, string? problem, BackgroundWriter writer)
    {
        File = file;
        Version = version;
        _values = values;
        Problem = problem;
        _writer = writer;
    }

    /// <summary>The settings file.</summary>
    public string File { get; }

    /// <summary>The game's current settings version (written into the file).</summary>
    public int Version { get; }

    /// <summary>Why the file's settings weren't used (it was then kept as <c>settings.broken.json</c>), or null.</summary>
    public string? Problem { get; }

    /// <summary>
    /// Reads the settings file (a missing one gives the defaults and is written on the first change).
    /// <paramref name="upgrades"/> holds, for each version from 1 to <paramref name="version"/> − 1, the
    /// change that makes such settings the next version's (without the version key, which the engine keeps).
    /// </summary>
    internal static Settings Load(string file, int version, IReadOnlyDictionary<int, Action<JsonObject>>? upgrades, BackgroundWriter writer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, 1);
        upgrades ??= new Dictionary<int, Action<JsonObject>>();
        for (var past = 1; past < version; past++)
            if (!upgrades.ContainsKey(past))
                throw new ArgumentException($"no upgrade of settings version {past} to {past + 1}", nameof(upgrades));
        byte[] bytes;
        try
        {
            bytes = System.IO.File.ReadAllBytes(file);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return new Settings(file, version, [], null, writer);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return Broken(file, version, $"it can't be read: {e.Message}", writer);
        }
        if (Parse(bytes, version, upgrades, out var values) is { } problem)
            return Broken(file, version, problem, writer);
        return new Settings(file, version, values, null, writer);
    }

    /// <summary>The settings in <paramref name="bytes"/>, upgraded to <paramref name="version"/>; or why they can't be used.</summary>
    static string? Parse(ReadOnlySpan<byte> bytes, int version, IReadOnlyDictionary<int, Action<JsonObject>> upgrades, out JsonObject values)
    {
        values = [];
        if (bytes.StartsWith((ReadOnlySpan<byte>)[0xEF, 0xBB, 0xBF]))
            bytes = bytes[3..];
        int found;
        try
        {
            // A repeated key isn't JSON this file may hold. (JsonObject refuses one too when it is first
            // read; the option says so at the parse.)
            var options = new JsonDocumentOptions { AllowDuplicateProperties = false };
            if (JsonNode.Parse(bytes, documentOptions: options) is not JsonObject root)
                return "it isn't a JSON object";
            if (!root.TryGetPropertyValue(VersionKey, out var versionNode))
                return "it has no \"version\"";
            if (versionNode is not JsonValue number || number.GetValueKind() != JsonValueKind.Number || !number.TryGetValue(out found) || found < 1)
                return "its \"version\" isn't a whole number from 1";
            if (found > version)
                return $"its version {found} is newer than this game's ({version})";
            root.Remove(VersionKey);
            values = root;
        }
        catch (Exception e) when (e is JsonException or ArgumentException or InvalidOperationException or FormatException)
        {
            return $"it isn't valid JSON: {e.Message}";
        }
        for (var past = found; past < version; past++)
            try
            {
                upgrades[past](values);
            }
            catch (Exception e)
            {
                return $"its upgrade from version {past} failed: {e.Message}";
            }
        return null;
    }

    static Settings Broken(string file, int version, string problem, BackgroundWriter writer)
    {
        var kept = BrokenPath(file);
        try
        {
            System.IO.File.Move(file, kept, overwrite: true);
            Log.Warning("Storage", $"settings {file}: {problem}; the defaults are used, the file is kept as {kept}");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Log.Warning("Storage", $"settings {file}: {problem}; the defaults are used, and it couldn't be kept as {kept}: {e.Message}");
        }
        return new Settings(file, version, [], problem, writer);
    }

    /// <summary>Where a settings file that can't be used is kept: <c>settings.json</c> → <c>settings.broken.json</c>.</summary>
    public static string BrokenPath(string file) =>
        Path.Combine(Path.GetDirectoryName(file) ?? "", Path.GetFileNameWithoutExtension(file) + ".broken" + Path.GetExtension(file));

    /// <summary>Whether the settings hold <paramref name="key"/>.</summary>
    public bool Contains(string key)
    {
        lock (_gate)
            return _values.ContainsKey(key);
    }

    /// <summary>
    /// The setting's value as a <typeparamref name="T"/>; <paramref name="defaultValue"/> when it is
    /// missing, null, or doesn't convert (logged the first time: Q43).
    /// </summary>
    public T Get<T>(string key, T defaultValue)
    {
        lock (_gate)
        {
            if (!_values.TryGetPropertyValue(key, out var node))
                return defaultValue;
            if (node != null)
                try
                {
                    if (node.Deserialize<T>(ValueOptions) is { } value)
                        return value;
                }
                catch (Exception e) when (e is JsonException or InvalidOperationException or NotSupportedException or FormatException or ArgumentException)
                {
                }
            if (_warned.Add(key))
                Log.Warning("Storage", $"settings {File}: \"{key}\" holds {node?.ToJsonString(ValueOptions) ?? "null"}, not a {typeof(T).Name}; its default is used");
            return defaultValue;
        }
    }

    /// <summary>Sets the setting; when its value changes the file is written (by the background writer).</summary>
    public void Set<T>(string key, T value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        if (key == VersionKey)
            throw new ArgumentException($"\"{VersionKey}\" is the file's version, not a setting", nameof(key));
        // A value JSON can't hold (NaN) is refused here, before anything changes.
        var node = JsonSerializer.SerializeToNode(value, ValueOptions);
        byte[] bytes;
        lock (_gate)
        {
            if (_values.TryGetPropertyValue(key, out var old) && JsonNode.DeepEquals(old, node))
                return;
            _values[key] = node;
            _warned.Remove(key);
            bytes = Serialize();
        }
        _writer.Enqueue(File, () =>
        {
            StorageFiles.CreateFolder(Path.GetDirectoryName(Path.GetFullPath(File))!);
            var temporary = File + ".tmp";
            StorageFiles.WriteFlushed(temporary, bytes);
            System.IO.File.Move(temporary, File, overwrite: true);
        });
    }

    /// <summary>The file's text: the version first, then the settings in their order; indented by two spaces, LF line ends, a final line end.</summary>
    byte[] Serialize()
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var json = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true, NewLine = "\n", Encoder = Encoder }))
        {
            json.WriteStartObject();
            json.WriteNumber(VersionKey, Version);
            foreach (var (key, node) in _values)
            {
                json.WritePropertyName(key);
                if (node is null)
                    json.WriteNullValue();
                else
                    node.WriteTo(json);
            }
            json.WriteEndObject();
        }
        buffer.Write("\n"u8);
        return buffer.WrittenSpan.ToArray();
    }
}
