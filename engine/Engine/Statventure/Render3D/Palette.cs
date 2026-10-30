using System.Numerics;

namespace Statventure.Render3D;

/// <summary>A palette slot's tones (linear): lit, and shadow when the palette gives one (otherwise the cel style derives it).</summary>
public readonly record struct PaletteTone(Vector4 Lit, Vector4? Shadow = null);

/// <summary>
/// The colours a model's palette slots take (Docs/Renderer-3D.md, "Colours: palette slots"): a
/// material named <c>metal.base</c> shows the palette's <c>metal.base</c>, so one model shows in any
/// metal or cloth. A material whose slot the palette doesn't have keeps its own base colour. Shadow
/// tones come from the cel style unless the palette gives a slot its own.
/// </summary>
public sealed class Palette
{
    readonly Dictionary<string, PaletteTone> _tones = new(StringComparer.Ordinal);

    public Palette() { }

    /// <summary>A palette from slot names and "#rrggbb" colours, and shadow tones for some slots.</summary>
    public Palette(IEnumerable<KeyValuePair<string, string>> slots, IEnumerable<KeyValuePair<string, string>>? shadows = null)
    {
        var shadowTones = (shadows ?? []).ToDictionary(s => s.Key, s => Colors.LinearFromHex(s.Value));
        foreach (var (slot, hex) in slots)
            Set(slot, Colors.LinearFromHex(hex), shadowTones.TryGetValue(slot, out var shadow) ? shadow : null);
    }

    public IReadOnlyDictionary<string, PaletteTone> Slots => _tones;

    /// <summary>Sets a slot's lit tone and, if it has its own, its shadow tone (linear).</summary>
    public Palette Set(string slot, Vector4 lit, Vector4? shadow = null)
    {
        _tones[slot] = new PaletteTone(lit, shadow);
        return this;
    }

    /// <summary>The lit tone (linear) a material shows.</summary>
    public Vector4 Resolve(ModelMaterial material) =>
        _tones.TryGetValue(material.Slot, out var tone) ? tone.Lit : material.BaseColor;

    /// <summary>The lit and shadow tones (linear) a material shows under <paramref name="style"/>.</summary>
    public (Vector4 Lit, Vector4 Shadow) Resolve(ModelMaterial material, CelStyle style)
    {
        if (_tones.TryGetValue(material.Slot, out var tone))
            return (tone.Lit, tone.Shadow ?? style.ShadowOf(tone.Lit));
        return (material.BaseColor, style.ShadowOf(material.BaseColor));
    }

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
