using System.Text.Json;
using System.Text.Json.Nodes;
using Statventure.Editor.Settings;

namespace Statventure.Editor.Preview;

/// <summary>
/// The preview's volume (P31, G6), <c>editor/volume.json</c>: <c>{ "version": 1, "volume": 0.5 }</c>, the editor's master
/// volume from 0 to 1, 50 % when nothing is remembered (the manager's decision Q3, 2026-11-07). Read tolerantly (a
/// value that isn't a number from 0 to 1 is the default), written whole each time the volume is set.
/// </summary>
public static class VolumeFile
{
    public const string FileName = "volume.json";

    /// <summary>The editor's volume when none is remembered.</summary>
    public const double Default = 0.5;

    public static string File(string playerFolder) => Path.Combine(EditorSettings.Folder(playerFolder), FileName);

    public static double Read(string playerFolder) =>
        EditorSettings.Read(File(playerFolder)) is { } values && EditorSettings.Number(values["volume"]) is { } volume and >= 0 and <= 1 ? volume : Default;

    public static bool Write(string playerFolder, double volume) =>
        EditorSettings.Write(File(playerFolder), new JsonObject { ["volume"] = Math.Clamp(volume, 0, 1) });
}

/// <summary>
/// One role's stand-in in a cutscene's preview (P34): for an actor role a model and a clip library (paths of model
/// entries; null: none chosen), for a model role a model, for a palette role its colours as edited (slot → "#rrggbb";
/// the slots not given take the preview's own distinct colours, Q6). Element and node roles have nothing to choose.
/// </summary>
public sealed record StandIn(string? Model = null, string? Clips = null, IReadOnlyDictionary<string, string>? Colours = null)
{
    public static readonly StandIn None = new();
}

/// <summary>
/// The stand-ins chosen per cutscene (P34, G6), <c>editor/stand-ins.json</c>: <c>{ "version": 1, "cutscenes": { "&lt;the
/// cutscene's path&gt;": { "&lt;role&gt;": { "model": …, "clips": …, "colours": { "&lt;slot&gt;": "#rrggbb" } } } } }</c>, kept by the
/// editor, not in the mod. Read tolerantly (what isn't of its kind is left out: a path that isn't a string, a colour
/// that isn't #rrggbb), written whole each time a stand-in is chosen.
/// </summary>
public static class StandInsFile
{
    public const string FileName = "stand-ins.json";

    public static string File(string playerFolder) => Path.Combine(EditorSettings.Folder(playerFolder), FileName);

    /// <summary>Every cutscene's stand-ins by the cutscene's path, each by role.</summary>
    public static Dictionary<string, Dictionary<string, StandIn>> Read(string playerFolder)
    {
        var all = new Dictionary<string, Dictionary<string, StandIn>>(StringComparer.Ordinal);
        if (EditorSettings.Read(File(playerFolder)) is not { } values || values["cutscenes"] is not JsonObject cutscenes)
            return all;
        foreach (var (path, roles) in cutscenes)
        {
            if (roles is not JsonObject byRole)
                continue;
            var kept = new Dictionary<string, StandIn>(StringComparer.Ordinal);
            foreach (var (role, standIn) in byRole)
            {
                if (standIn is not JsonObject fields)
                    continue;
                Dictionary<string, string>? colours = null;
                if (fields["colours"] is JsonObject slots)
                {
                    colours = new(StringComparer.Ordinal);
                    foreach (var (slot, colour) in slots)
                        if (Text(colour) is { } hex && PreviewColours.IsHex(hex))
                            colours[slot] = hex;
                }
                kept[role] = new StandIn(Text(fields["model"]), Text(fields["clips"]), colours);
            }
            all[path] = kept;
        }
        return all;
    }

    public static bool Write(string playerFolder, IReadOnlyDictionary<string, Dictionary<string, StandIn>> all)
    {
        var cutscenes = new JsonObject();
        foreach (var (path, roles) in all.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            var byRole = new JsonObject();
            foreach (var (role, standIn) in roles.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                var fields = new JsonObject();
                if (standIn.Model is { } model)
                    fields["model"] = model;
                if (standIn.Clips is { } clips)
                    fields["clips"] = clips;
                if (standIn.Colours is { Count: > 0 } colours)
                    fields["colours"] = new JsonObject(colours.OrderBy(c => c.Key, StringComparer.Ordinal).Select(c => KeyValuePair.Create(c.Key, (JsonNode?)c.Value)));
                byRole[role] = fields;
            }
            cutscenes[path] = byRole;
        }
        return EditorSettings.Write(File(playerFolder), new JsonObject { ["cutscenes"] = cutscenes });
    }

    static string? Text(JsonNode? node) =>
        node is JsonValue value && value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : null;
}
