namespace Statventure.Text.Autohint;

/// <summary>
/// skrifa's automatic hinter (skrifa 0.44, the Fontations version Chromium 152 builds; a port of
/// FreeType's autofitter), as Skia's Fontations backend runs it for slight hinting: the outline of
/// a TrueType glyph scaled to the size and hinted (in light mode only vertically).
/// </summary>
internal static class Autohinter
{
    /// <summary>
    /// Hints a glyph at <paramref name="size"/> pixels per em (skrifa's autohint Instance::draw):
    /// the outline's points in 26.6 (y up) with each contour's last index; null when the glyph
    /// has no style or outline.
    /// </summary>
    public static (Outline Outline, int Advance)? Draw(FontFace face, uint glyph, float size, HintTarget target)
    {
        var font = AutohintFont.For(face);
        if (font.Styles.Style(glyph) is not { } style || font.MetricsFor(style) is not { } metrics)
            return null;
        var unitsPerEm = font.UnitsPerEm;
        var scale = new Scale(size, unitsPerEm, font.IsItalic, target, metrics.StyleClass.Script.Group);
        var outline = new Outline();
        if (!outline.Fill(font, glyph, QuirksMode.Jit))
            return null;
        var hinted = Hinter.HintOutline(outline, metrics, scale, style);
        var hAdvance = outline.Advance;
        var pp1x = 0;
        var pp2x = Fixed.Mul(hAdvance, hinted.XScale);
        var isLight = target.IsLight || target.KeepsLinearMetrics;
        if (!isLight && (scale.Flags & ScaleFlags.NoAdvance) == 0 && hinted.EdgeMetrics is { } edges)
        {
            var oldRsb = pp2x - edges.RightOpos;
            var oldLsb = edges.LeftOpos;
            var newLsb = edges.LeftPos;
            var pp1xUh = newLsb - oldLsb;
            var pp2xUh = edges.RightPos + oldRsb;
            if (oldLsb < 24)
                pp1xUh -= 8;
            if (oldRsb < 24)
                pp2xUh += 8;
            pp1x = Fixed.PixRound(pp1xUh);
            pp2x = Fixed.PixRound(pp2xUh);
            if (pp1x >= newLsb && oldLsb > 0)
                pp1x -= 64;
            if (pp2x <= edges.RightPos && oldRsb > 0)
                pp2x += 64;
        }
        else
        {
            pp1x = Fixed.PixRound(pp1x);
            pp2x = Fixed.PixRound(pp2x);
        }
        if (pp1x != 0)
            for (var i = 0; i < outline.PointCount; i++)
                outline.Points[i].X -= pp1x;
        int advance;
        if (!isLight && (font.IsFixedPitch || (metrics.DigitsHaveSameWidth && style.IsDigit)))
            advance = Fixed.Mul(hAdvance, scale.XScale);
        else
            advance = hAdvance != 0 ? pp2x - pp1x : 0;
        return (outline, Fixed.PixRound(advance));
    }
}
