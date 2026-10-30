using System.Numerics;

namespace Statventure.Render3D;

/// <summary>
/// The colours a model's palette slots take (Docs/Renderer-3D.md, "Colours: palette slots"): a
/// material named <c>metal.base</c> shows the palette's <c>metal.base</c>, so one model shows in any
/// metal or cloth. A material whose slot the palette doesn't have keeps its own base colour.
/// </summary>
public sealed class Palette
{
    readonly Dictionary<string, Vector4> _colors = new(StringComparer.Ordinal);

    public Palette() { }

    /// <summary>A palette from slot names and "#rrggbb" colours.</summary>
    public Palette(IEnumerable<KeyValuePair<string, string>> slots)
    {
        foreach (var (slot, hex) in slots)
            Set(slot, Colors.LinearFromHex(hex));
    }

    public IReadOnlyDictionary<string, Vector4> Slots => _colors;

    /// <summary>Sets a slot's colour (linear).</summary>
    public Palette Set(string slot, Vector4 linear)
    {
        _colors[slot] = linear;
        return this;
    }

    /// <summary>The colour (linear) a material shows.</summary>
    public Vector4 Resolve(ModelMaterial material) =>
        _colors.TryGetValue(material.Slot, out var color) ? color : material.BaseColor;

    /// <summary>
    /// The slot a material's name names: the name without the ".001"-style suffix Blender gives a
    /// second material of the same name.
    /// </summary>
    public static string SlotOf(string materialName)
    {
        var dot = materialName.Length - 4;
        if (dot > 0 && materialName[dot] == '.' && char.IsAsciiDigit(materialName[dot + 1]) && char.IsAsciiDigit(materialName[dot + 2]) && char.IsAsciiDigit(materialName[dot + 3]))
            return materialName[..dot];
        return materialName;
    }
}
