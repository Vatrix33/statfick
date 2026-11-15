// A look file (GM2 step 3a; Tools/Models/humanoid/look.py writes it): a look's numbers for the engine's view, read from
// the same file Blender's renders read (render.py through render_scenes.py), so one look file drives both. Its fields
// are the engine's own: the view's background and sun (SunLight), every number of CelStyle, and the palette's colours
// and shadow tones by slot. Read strictly, as look.py writes it: every key there, nothing else, each value of its kind
// (a key read under another name, or a number left at the engine's default, would show a look the file doesn't hold).
using System.Numerics;
using System.Text.Json;
using Statventure.Render3D;

namespace Statventure.Sandbox.Figure;

/// <summary>A look: the view's background (sRGB "#rrggbb"), its sun, the cel style, and the palette's colours and shadow tones by slot.</summary>
public sealed record FigureLook(string Name, string Background, SunLight Sun, CelStyle Style, IReadOnlyDictionary<string, string> Colours,
    IReadOnlyDictionary<string, string> Shadows)
{
    public const string Format = "statventure.look";
    public const int Version = 1;

    /// <summary>The style's keys, CelStyle's numbers by their properties' names in camelCase (look.py's STYLE_KEYS).</summary>
    public static readonly IReadOnlyList<string> StyleKeys =
    [
        "ink", "shadowMix", "bandEdge", "outlineWidth", "innerWidth", "creaseAngle",
        "hatchSpacing", "hatchWidth", "hatchAngle", "hatchInk", "glowWide", "glowCore", "glowBoost",
    ];

    static readonly string[] LookKeys = ["format", "version", "name", "background", "sun", "style", "palette", "shadows"];
    static readonly string[] SunKeys = ["direction", "strength", "angle"];

    /// <summary>The palette the look gives a figure.</summary>
    public Palette Palette => new(Colours, Shadows);

    /// <summary>The view's background, linear (as <see cref="View3D.Background"/> takes it).</summary>
    public Vector4 BackgroundLinear => Colors.LinearFromHex(Background);

    public static FigureLook Load(string path) => Parse(File.ReadAllText(path), path);

    /// <summary>A look file's text; an <see cref="InvalidDataException"/> names the first problem.</summary>
    public static FigureLook Parse(string json, string where)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Keys(root, LookKeys, where);
        if (root.GetProperty("format"u8).GetString() != Format || Number(root, "version", where) != Version)
            throw new InvalidDataException($"{where}: not a {Format} file of version {Version}");
        var name = Text(root, "name", where);
        var background = Colour(root, "background", where);
        var sun = root.GetProperty("sun"u8);
        Keys(sun, SunKeys, $"{where}: sun");
        var direction = sun.GetProperty("direction"u8);
        if (direction.ValueKind != JsonValueKind.Array || direction.GetArrayLength() != 3 || direction.EnumerateArray().Any(v => v.ValueKind != JsonValueKind.Number))
            throw new InvalidDataException($"{where}: sun.direction must be three numbers");
        var d = direction.EnumerateArray().Select(v => v.GetSingle()).ToArray();
        if (d.All(v => v == 0))
            throw new InvalidDataException($"{where}: sun.direction must not be 0");
        var strength = Number(sun, "strength", $"{where}: sun");
        var angle = Number(sun, "angle", $"{where}: sun");
        if (strength < 0 || angle < 0)
            throw new InvalidDataException($"{where}: the sun's strength and angle are 0 or more");
        var style = root.GetProperty("style"u8);
        Keys(style, StyleKeys, $"{where}: style");
        float S(string key) => Number(style, key, $"{where}: style");
        var cel = new CelStyle(Colors.FromHex(Colour(style, "ink", $"{where}: style")), S("shadowMix"), S("bandEdge"))
        {
            OutlineWidth = S("outlineWidth"),
            InnerWidth = S("innerWidth"),
            CreaseAngle = S("creaseAngle"),
            HatchSpacing = S("hatchSpacing"),
            HatchWidth = S("hatchWidth"),
            HatchAngle = S("hatchAngle"),
            HatchInk = S("hatchInk"),
            GlowWide = S("glowWide"),
            GlowCore = S("glowCore"),
            GlowBoost = S("glowBoost"),
        };
        return new FigureLook(name, background, new SunLight(new Vector3(d[0], d[1], d[2]), strength) { Angle = angle }, cel,
            Slots(root, "palette", where), Slots(root, "shadows", where));
    }

    static void Keys(JsonElement element, IReadOnlyList<string> keys, string where)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"{where}: not an object");
        var present = element.EnumerateObject().Select(p => p.Name).ToList();
        var missing = keys.Where(k => !present.Contains(k)).ToList();
        var unknown = present.Where(k => !keys.Contains(k)).ToList();
        if (missing.Count > 0 || unknown.Count > 0)
            throw new InvalidDataException($"{where}: " + string.Join("; ",
                (missing.Count > 0 ? [$"missing {string.Join(", ", missing)}"] : Array.Empty<string>())
                .Concat(unknown.Count > 0 ? [$"unknown {string.Join(", ", unknown)}"] : [])));
    }

    static float Number(JsonElement element, string key, string where)
    {
        var value = element.GetProperty(key);
        if (value.ValueKind != JsonValueKind.Number || !float.IsFinite(value.GetSingle()))
            throw new InvalidDataException($"{where}: {key} must be a number");
        return value.GetSingle();
    }

    static string Text(JsonElement element, string key, string where)
    {
        var value = element.GetProperty(key);
        if (value.ValueKind != JsonValueKind.String || value.GetString() is not { Length: > 0 } text)
            throw new InvalidDataException($"{where}: {key} must be a text");
        return text;
    }

    static string Colour(JsonElement element, string key, string where)
    {
        var value = element.GetProperty(key);
        if (value.ValueKind != JsonValueKind.String || !IsColour(value.GetString()!))
            throw new InvalidDataException($"{where}: {key} must be an sRGB colour #rrggbb");
        return value.GetString()!;
    }

    static bool IsColour(string text) => text.Length == 7 && text[0] == '#' && text[1..].All(char.IsAsciiHexDigit);

    static Dictionary<string, string> Slots(JsonElement root, string key, string where)
    {
        var element = root.GetProperty(key);
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException($"{where}: {key} must be an object");
        var slots = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var slot in element.EnumerateObject())
            slots[slot.Name] = Colour(element, slot.Name, $"{where}: {key}");
        return slots;
    }
}
