using System.Numerics;
using Statventure.Render3D;

namespace Statventure.Tests.Humanoid;

/// <summary>
/// The game's look as the user chose it (GM2-3, GM2-8; "Ember, hearth", 2026-11-15): looks/ember-hearth.json, written by
/// Tools/Models/humanoid/look.py from EMBER_HEARTH, holds the manager's numbers for it (the scene fields of
/// claude-work/gm2/step4/look-ember.json: its background, sun, style and palette), the crease angle and the glow, which
/// they don't set, as today's (CelStyle.Default); its palette names the step's slots (look.py: the buckle and the grip,
/// the scratch files' metal.trim and leather, in their colours; no smallclothes, which no model wears).
/// </summary>
public class EmberHearthLookTests
{
    [Fact]
    public void TheGamesLookIsTheChosenOne()
    {
        var look = HumanoidFiles.Ember;
        Assert.Equal("ember-hearth", look.Name);
        Assert.Equal("#2a1f1a", look.Background);
        Assert.Equal(new SunLight(new Vector3(-0.8f, 0.6f, 0.75f), 3) { Angle = 3 }, look.Sun);
        Assert.Equal(CelStyle.Default with
        {
            Ink = Colors.FromHex("#080605"), ShadowMix = 0.58f, BandEdge = 0.36f, OutlineWidth = 3.0f, InnerWidth = 1.8f,
            HatchSpacing = 5, HatchWidth = 1.2f, HatchAngle = 72, HatchInk = 0.5f,
        }, look.Style);
        Assert.Equal((120f, 8f, 2.5f, 2.2f), (look.Style.CreaseAngle, look.Style.GlowWide, look.Style.GlowCore, look.Style.GlowBoost));
        Assert.Equal(new Dictionary<string, string>
        {
            ["skin"] = "#aa7a5a", ["lips"] = "#7c4238", ["hair"] = "#3a2416", ["brows"] = "#130c08", ["eye.white"] = "#d2c7b2", ["eyes"] = "#0a0706",
            ["cloth"] = "#74211d", ["cloth.trim"] = "#bb8d3c", ["trousers"] = "#292320", ["leather"] = "#3a261b", ["buckle"] = "#bb8d3c",
            ["metal.base"] = "#80848c", ["metal.trim"] = "#bb8d3c", ["metal.shade"] = "#3a3c42", ["wood"] = "#5a2a1e", ["grip"] = "#3a261b",
        }.OrderBy(p => p.Key, StringComparer.Ordinal), look.Colours.OrderBy(p => p.Key, StringComparer.Ordinal));
        Assert.Empty(look.Shadows);
    }
}
