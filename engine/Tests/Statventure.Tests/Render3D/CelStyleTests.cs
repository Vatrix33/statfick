using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Tests.Render3D;

/// <summary>The cel tones (Docs/Renderer-3D.md, "Cel shading"): the lizardman test's shadow rule and the palette's own shadow tones.</summary>
public class CelStyleTests
{
    static void Near(Vector3 expected, Vector4 actual) =>
        Assert.True(Vector3.Distance(expected, new Vector3(actual.X, actual.Y, actual.Z)) < 1e-3f, $"expected {expected}, got {actual}");

    [Fact]
    public void ShadowsLeanTowardTheInkInSrgb()
    {
        // #7b5a3a mixed 42% toward #130e0b in sRGB: (123 − 104·0.42, 90 − 76·0.42, 58 − 47·0.42).
        var shadow = CelStyle.Default.ShadowOf(Colors.LinearFromHex("#7b5a3a"));
        Near(new Vector3(79.32f, 58.08f, 38.26f), Colors.ToSrgb(shadow) * 255);
        Assert.Equal(1, shadow.W);
    }

    [Fact]
    public void PaletteShadowTonesWinOverTheStyle()
    {
        var palette = new Palette([new("metal.base", "#8a8f96"), new("cloth", "#7b5a3a")], [new("metal.base", "#2f3540")]);
        var metal = palette.Resolve(new ModelMaterial("metal.base.001", Vector4.One, false), CelStyle.Default);
        Assert.Equal(Colors.LinearFromHex("#8a8f96"), metal.Lit);
        Assert.Equal(Colors.LinearFromHex("#2f3540"), metal.Shadow);
        var cloth = palette.Resolve(new ModelMaterial("cloth", Vector4.One, false), CelStyle.Default);
        Assert.Equal(CelStyle.Default.ShadowOf(Colors.LinearFromHex("#7b5a3a")), cloth.Shadow);
        // A material no slot names keeps its base colour, and its shadow follows the style.
        var stone = new Vector4(0.147f, 0.122f, 0.098f, 1);
        Assert.Equal((stone, CelStyle.Default.ShadowOf(stone)), palette.Resolve(new ModelMaterial("stone", stone, false), CelStyle.Default));
    }

    [Fact]
    public void SrgbRoundTrips()
    {
        for (var i = 0; i <= 255; i++)
            Assert.Equal(i, (int)MathF.Round(Colors.ToSrgb(Colors.ToLinear(i / 255f)) * 255));
    }
}
