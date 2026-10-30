using System.Globalization;
using System.Numerics;

namespace Statventure.Core;

/// <summary>
/// A colour with straight (not premultiplied) alpha, components 0..1, in sRGB space, the space CSS
/// colours and browser blending use.
/// </summary>
public readonly record struct Color(float R, float G, float B, float A = 1f)
{
    public static readonly Color Transparent = new(0, 0, 0, 0);
    public static readonly Color Black = new(0, 0, 0);
    public static readonly Color White = new(1, 1, 1);

    public static Color FromBytes(byte r, byte g, byte b, byte a = 255) => new(r / 255f, g / 255f, b / 255f, a / 255f);

    /// <summary>Parses #rgb, #rgba, #rrggbb and #rrggbbaa.</summary>
    public static Color FromHex(string hex)
    {
        var h = hex.TrimStart('#');
        int Digit(int i) => int.Parse(h.AsSpan(i, 1), NumberStyles.HexNumber);
        int Pair(int i) => int.Parse(h.AsSpan(i, 2), NumberStyles.HexNumber);
        return h.Length switch
        {
            3 => FromBytes((byte)(Digit(0) * 17), (byte)(Digit(1) * 17), (byte)(Digit(2) * 17)),
            4 => FromBytes((byte)(Digit(0) * 17), (byte)(Digit(1) * 17), (byte)(Digit(2) * 17), (byte)(Digit(3) * 17)),
            6 => FromBytes((byte)Pair(0), (byte)Pair(2), (byte)Pair(4)),
            8 => FromBytes((byte)Pair(0), (byte)Pair(2), (byte)Pair(4), (byte)Pair(6)),
            _ => throw new FormatException("not a hex colour: " + hex),
        };
    }

    public Color WithAlpha(float alpha) => this with { A = alpha };

    /// <summary>The colour with its components multiplied by alpha, as the GPU blends it.</summary>
    public Vector4 Premultiplied => new(R * A, G * A, B * A, A);

    public Vector4 ToVector4() => new(R, G, B, A);

    /// <summary>Mixes in premultiplied space, as CSS gradients and transitions do.</summary>
    public static Color Lerp(Color a, Color b, float t)
    {
        var p = Vector4.Lerp(a.Premultiplied, b.Premultiplied, t);
        return p.W <= 0 ? Transparent : new Color(p.X / p.W, p.Y / p.W, p.Z / p.W, p.W);
    }

    public string ToHex() =>
        $"#{(int)MathF.Round(R * 255):x2}{(int)MathF.Round(G * 255):x2}{(int)MathF.Round(B * 255):x2}" + (A < 1 ? $"{(int)MathF.Round(A * 255):x2}" : "");

    public override string ToString() => ToHex();
}
