using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Statventure.Core;
using Statventure.Editor.Session;
using Statventure.Storage;

namespace Statventure.Editor.Settings;

/// <summary>
/// The editor's own settings files (G6; the manager's decision of 2026-11-07): one small JSON file per concern in
/// <c>&lt;player folder&gt;/editor/</c>, <c>{ "version": 1, … }</c>. Read tolerantly: a file that is missing, can't be
/// read, isn't JSON, isn't an object or has another version is ignored (the defaults), and the next save writes it
/// whole again; a value of the wrong kind is ignored on its own. Written whole into a file beside it, flushed, then
/// moved over it, as the game writes its <c>settings.json</c>; a write that fails is logged and the editor goes on.
/// Later steps add their own files here (the cutscene stand-ins, the volume).
/// </summary>
public static class EditorSettings
{
    /// <summary>The version every file of this step is written with (and the only one read).</summary>
    public const int Version = 1;

    /// <summary>The settings folder of a player's game folder (<see cref="EditorSession.SettingsFolder"/>).</summary>
    public static string Folder(string playerFolder) => Path.Combine(Path.GetFullPath(playerFolder), EditorSession.SettingsFolderName);

    /// <summary>The file's values (without its version), or null when it can't be used (missing, damaged, another version).</summary>
    public static JsonObject? Read(string file)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(file);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            return null;
        }
        try
        {
            if (JsonNode.Parse(bytes, documentOptions: new JsonDocumentOptions { AllowDuplicateProperties = false }) is not JsonObject root)
                return null;
            if (Whole(root["version"]) != Version)
                return null;
            root.Remove("version");
            return root;
        }
        catch (Exception problem) when (problem is JsonException or ArgumentException or InvalidOperationException or FormatException)
        {
            return null;
        }
    }

    /// <summary>
    /// Writes <paramref name="values"/> as the whole file (the version first), into <c>&lt;file&gt;.tmp</c> flushed to
    /// the disk and moved over the file; the folder is made when missing. Gives whether it was written (a failure is
    /// logged).
    /// </summary>
    public static bool Write(string file, JsonObject values)
    {
        var root = new JsonObject { ["version"] = Version };
        foreach (var (key, value) in values)
            root[key] = value?.DeepClone();
        var text = root.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 2,
            NewLine = "\n",
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        }) + "\n";
        try
        {
            var folder = Path.GetDirectoryName(Path.GetFullPath(file))!;
            if (OperatingSystem.IsWindows())
                Directory.CreateDirectory(folder);
            else
                Directory.CreateDirectory(folder, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            var temporary = file + ".tmp";
            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(Encoding.UTF8.GetBytes(text));
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, file, overwrite: true);
            return true;
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            Log.Warning("Editor", $"the editor's settings {file} weren't written: {problem.Message}");
            return false;
        }
    }

    /// <summary>A whole number in a JSON value (a number with no fraction in the range of an int), else null.</summary>
    public static int? Whole(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() == JsonValueKind.Number && value.TryGetValue(out double number)
            && number == Math.Floor(number) && number is >= int.MinValue and <= int.MaxValue ? (int)number : null;

    /// <summary>A number in a JSON value, else null.</summary>
    public static double? Number(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() == JsonValueKind.Number && value.TryGetValue(out double number) ? number : null;
}

/// <summary>
/// The shell's remembered state (P4, G6; the manager's decisions of 2026-11-06 and 2026-11-07), <c>editor/shell.json</c>:
/// the page zoom level the shell's Ctrl+= and Ctrl+- set (<see cref="Shell.EditorView.ZoomLevel"/>) and the window's
/// last normal size (not maximized, not full screen), at least the shell's smallest (800 × 500). Read at start (the
/// size into the window's options before it opens, the zoom once the view is made) and written when the editor closes.
/// </summary>
public sealed record ShellSettings(double ZoomLevel = 0, int? Width = null, int? Height = null)
{
    public const string FileName = "shell.json";

    /// <summary>The window's size when none is remembered, and its smallest (step 4's).</summary>
    public const int DefaultWidth = 1280, DefaultHeight = 800, MinWidth = 800, MinHeight = 500;

    /// <summary>The file in a player's game folder.</summary>
    public static string File(string playerFolder) => Path.Combine(EditorSettings.Folder(playerFolder), FileName);

    /// <summary>
    /// The shell's settings in <paramref name="playerFolder"/>: a zoom level the shell can't have (not a half step, or
    /// past its limits) or a size that isn't a whole number from 1 is left out (the default); a size smaller than the
    /// smallest is made the smallest.
    /// </summary>
    public static ShellSettings Read(string playerFolder)
    {
        if (EditorSettings.Read(File(playerFolder)) is not { } values)
            return new();
        var zoom = EditorSettings.Number(values["zoomLevel"]) is { } level && Shell.EditorView.IsZoomLevel(level) ? level : 0;
        var width = EditorSettings.Whole(values["width"]) is { } w and >= 1 ? Math.Max(w, MinWidth) : (int?)null;
        var height = EditorSettings.Whole(values["height"]) is { } h and >= 1 ? Math.Max(h, MinHeight) : (int?)null;
        return new ShellSettings(zoom, width, height);
    }

    /// <summary>Writes these settings into <paramref name="playerFolder"/> (the size made at least the smallest).</summary>
    public bool Write(string playerFolder)
    {
        var values = new JsonObject { ["zoomLevel"] = ZoomLevel };
        if (Width is { } width)
            values["width"] = Math.Max(width, MinWidth);
        if (Height is { } height)
            values["height"] = Math.Max(height, MinHeight);
        return EditorSettings.Write(File(playerFolder), values);
    }
}

/// <summary>
/// The file browser's recent places (M11-9, P26; step 3's <see cref="RecentPlaces"/>), <c>editor/recent-places.json</c>:
/// <c>{ "version": 1, "folders": [ … ] }</c>, most recent first, what <see cref="RecentPlaces.Folders"/> holds. Read when
/// the browser first opens, written each time a file is picked.
/// </summary>
public static class RecentPlacesFile
{
    public const string FileName = "recent-places.json";

    /// <summary>The file in a player's game folder.</summary>
    public static string File(string playerFolder) => Path.Combine(EditorSettings.Folder(playerFolder), FileName);

    /// <summary>The recent places kept in <paramref name="playerFolder"/> (none when the file can't be used; strings only).</summary>
    public static RecentPlaces Read(string playerFolder)
    {
        if (EditorSettings.Read(File(playerFolder)) is not { } values || values["folders"] is not JsonArray folders)
            return new RecentPlaces();
        var kept = new List<string>();
        foreach (var folder in folders)
            if (folder is JsonValue value && value.GetValueKind() == JsonValueKind.String)
                kept.Add(value.GetValue<string>());
        return new RecentPlaces(kept);
    }

    /// <summary>Writes <paramref name="places"/> into <paramref name="playerFolder"/>.</summary>
    public static bool Write(string playerFolder, RecentPlaces places) =>
        EditorSettings.Write(File(playerFolder), new JsonObject { ["folders"] = new JsonArray([.. places.Folders.Select(f => (JsonNode)JsonValue.Create(f))]) });
}
