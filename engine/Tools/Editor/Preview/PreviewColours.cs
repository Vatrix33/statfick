using Statventure.Render3D;

namespace Statventure.Editor.Preview;

/// <summary>
/// The preview's own palettes (P32 for particle effects, Q6 for a cutscene's palette roles): each slot named gets a
/// distinct colour of <see cref="Distinct"/>, in the order the slots are first named (past the list, round again),
/// unless the user gave it one. Not saved in the pack.
/// </summary>
public static class PreviewColours
{
    /// <summary>The preview's distinct colours (sRGB), the editor's ember first.</summary>
    public static readonly string[] Distinct = ["#d77a50", "#5f9fd3", "#8fc85a", "#e2c04a", "#b46ad1", "#4fc1b0", "#e06a8c", "#c8c8c0"];

    /// <summary>
    /// Each of <paramref name="slots"/> (in order, each once) with its colour: the one <paramref name="chosen"/> gives,
    /// else the next of <see cref="Distinct"/>.
    /// </summary>
    public static IReadOnlyList<(string Slot, string Colour)> For(IEnumerable<string> slots, IReadOnlyDictionary<string, string>? chosen = null)
    {
        var result = new List<(string, string)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var slot in slots)
        {
            if (!seen.Add(slot))
                continue;
            var colour = chosen != null && chosen.TryGetValue(slot, out var own) ? own : Distinct[(seen.Count - 1) % Distinct.Length];
            result.Add((slot, colour));
        }
        return result;
    }

    /// <summary>A palette of the slots and colours given.</summary>
    public static Palette Palette(IEnumerable<(string Slot, string Colour)> slots)
    {
        var palette = new Palette();
        foreach (var (slot, colour) in slots)
            palette.Set(slot, Colors.LinearFromHex(colour));
        return palette;
    }

    /// <summary>Whether <paramref name="text"/> is a colour as the preview writes it: "#rrggbb".</summary>
    public static bool IsHex(string text) => text.Length == 7 && text[0] == '#' && text.Skip(1).All(char.IsAsciiHexDigit);

    /// <summary>A colour typed (with or without its "#", any case) as "#rrggbb" in lower case, or null.</summary>
    public static string? Read(string text)
    {
        var hex = text.Trim();
        if (!hex.StartsWith('#'))
            hex = "#" + hex;
        return IsHex(hex) ? hex.ToLowerInvariant() : null;
    }
}
