using System.Numerics;
using Statventure.Canvas;
using Statventure.Core;
using Statventure.Text;
using Statventure.UI.Layout;
using Statventure.UI.Style;
using CanvasPath = Statventure.Canvas.Path;

namespace Statventure.UI.Paint;

/// <summary>
/// Text decorations as Blink paints them (TextDecorationInfo, TextDecorationOffset,
/// DecorationLinePainter, TextPainter::ClipDecorationLine): each decoration in effect on a text
/// fragment takes its decorating box's font (the line's for the block's decorations, an inline
/// box's for its own), so underlines line up across sizes; its thickness (a tenth of that font's
/// size by default), offset (just under the ascent plus a gap, from the font, or under the em box)
/// and style (solid and double lines on whole pixels, dotted and dashed strokes, a wavy ribbon);
/// underlines and overlines skip the glyphs' ink they cross, and text shadows shadow them too.
/// </summary>
public sealed partial class UiPainter
{
    // ---------- Text decorations ----------

    /// <summary>
    /// What a decoration takes from a font (Blink's UsedFont): the rounded ascent, the size, the
    /// font's underline (Skia's metrics: the line's top below the baseline, its thickness) and the
    /// descent of the normalized em box.
    /// </summary>
    readonly record struct UsedFont(float Ascent, float Descent, float Size, float UnderlinePosition, float UnderlineThickness, float EmDescent);

    /// <summary>A decoration's decorating box: where its content box's top is, and its font and style.</summary>
    readonly record struct DecoratingBox(float ContentTop, UsedFont Font, ComputedStyle Style);

    /// <summary>One line to draw (Blink's DecorationGeometry): its style, its rect before snapping, and the offsets of a second line or the wave.</summary>
    readonly record struct DecorationGeometry(DecorationStyle Style, RectF Line, float DoubleOffset, float WavyOffset)
    {
        public float Thickness => Line.Height;
    }

    /// <summary>The decorations of one text fragment, resolved for painting.</summary>
    sealed record FragmentDecorations
    {
        public required List<(AppliedDecoration Decoration, DecoratingBox Box, float Thickness)> Decorations;
        /// <summary>The fragment: its left, the top of its font box, its width and height.</summary>
        public required RectF Text;
        public required float Baseline;
        public required ComputedStyle Style;
        public required GlyphRun Run;
        public required Vector2 GlyphOrigin;
        /// <summary>Glyph clusters by text offset, for the ink skip exceptions.</summary>
        public required Func<PositionedGlyph, bool> CanSkipInk;
    }

    static float LayoutRound(float value) => MathF.Round(value * 64, MidpointRounding.AwayFromZero) / 64;

    UsedFont UsedFontOf(ComputedStyle style)
    {
        var size = style.FontSize;
        var font = _document.Fonts.Resolve(style.FontFamilies, style.FontWeight, style.FontItalic) ?? _document.Fonts.ResolveDefault(style.FontWeight, style.FontItalic);
        if (font is not { } resolved)
            return new UsedFont(size, 0, size, 0, 0, 0);
        var face = resolved.Face;
        var (ascent, descent, _) = TextLayout.Metrics(face, size, style.SubpixelPositioning);
        var unit = size / face.UnitsPerEm;
        // SkFontHost_FreeType: the post table's line centre moved up by half its thickness.
        var postPosition = -face.UnderlinePosition;
        var underlinePosition = -(postPosition + face.UnderlineThickness / 2) * unit;
        // SimpleFontData::NormalizedTypoAscentAndDescent: the em split as sTypoAscender to sTypoDescender, else as the ascent to the descent.
        bool EmDescent(float a, float d, out float result)
        {
            var height = a + d;
            result = height <= 0 || a < 0 || a > height ? 0 : LayoutRound(size) - LayoutRound(a * size / height);
            return height > 0 && a >= 0 && a <= height;
        }
        if (!(face.TypoAscender > 0 && EmDescent(face.TypoAscender, face.TypoDescender, out var emDescent)))
            EmDescent(ascent, descent, out emDescent);
        return new UsedFont(ascent, descent, size, underlinePosition, face.UnderlineThickness * unit, emDescent);
    }

    /// <summary>
    /// The decorations of a text item (TextDecorationPainter::UpdateDecorationInfo with
    /// InlinePaintContext's decorating boxes): those the IFC root has belong to the line (an
    /// anonymous inline with the root's font on the line's baseline); the rest to the inline boxes
    /// that set them, outermost first, as they are on this line.
    /// </summary>
    FragmentDecorations? DecorationsOf(LayoutBox root, LineItem item, LineBox line, int lineIndex, Vector2 origin, Vector2 baseline)
    {
        if (item.Kind != LineItemKind.Text || item.Box is not { } textBox || item.Run is not { } run)
            return null;
        var applied = item.Style.AppliedDecorations;
        if (applied.Length == 0)
            return null;
        var rootStyle = root.Style;
        var rootFont = UsedFontOf(rootStyle);
        var lineBox = new DecoratingBox(origin.Y + line.Baseline - rootFont.Ascent, rootFont, rootStyle);
        var inlines = new List<LayoutBox>();
        for (var b = textBox.Type == BoxType.Text ? textBox.Parent : textBox; b is not null && b != root; b = b.Parent)
            if (b.Type == BoxType.Inline && b.Style.IsDecoratingBox)
                inlines.Add(b);
        inlines.Reverse();
        var onLine = Math.Max(0, applied.Length - inlines.Count);
        var decorations = new List<(AppliedDecoration, DecoratingBox, float)>(applied.Length);
        for (var i = 0; i < applied.Length; i++)
        {
            var box = lineBox;
            if (i >= onLine)
            {
                var inline = inlines[inlines.Count - (applied.Length - i)];
                foreach (var fragment in inline.Fragments ?? [])
                    if (fragment.Line == lineIndex)
                    {
                        var top = origin.Y + InlineOffset(inline, root).Y + fragment.Rect.Y + inline.Border.Top + inline.Padding.Top;
                        box = new DecoratingBox(top, UsedFontOf(inline.Style), inline.Style);
                        break;
                    }
            }
            decorations.Add((applied[i], box, DecorationThicknessOf(applied[i].Thickness, box.Font)));
        }
        var text = TextOf(textBox);
        return new FragmentDecorations
        {
            Decorations = decorations,
            Text = new RectF(baseline.X, baseline.Y - item.Ascent, item.Width, item.Ascent + item.Descent),
            Baseline = baseline.Y,
            Style = item.Style,
            Run = run,
            GlyphOrigin = baseline,
            CanSkipInk = glyph => glyph.Cluster < 0 || glyph.Cluster >= text.Length
                || CharacterProperties.CanDecorationSkipInk(char.IsSurrogatePair(text, glyph.Cluster) ? char.ConvertToUtf32(text, glyph.Cluster) : text[glyph.Cluster]),
        };
    }

    /// <summary>
    /// The decorations of a run of a field's own text (the inner editor's or the placeholder's
    /// lines): all belong to the line, with the text's font.
    /// </summary>
    FragmentDecorations? FieldRunDecorations(ComputedStyle style, TextLayout layout, GlyphRun run, Vector2 origin)
    {
        var applied = style.AppliedDecorations;
        if (applied.Length == 0 || run.Glyphs.Count == 0)
            return null;
        var font = UsedFontOf(style);
        var box = new DecoratingBox(origin.Y - font.Ascent, font, style);
        var text = layout.Text;
        return new FragmentDecorations
        {
            Decorations = applied.Select(d => (d, box, DecorationThicknessOf(d.Thickness, font))).ToList(),
            Text = new RectF(origin.X, origin.Y - font.Ascent, run.Width, font.Ascent + font.Descent),
            Baseline = origin.Y,
            Style = style,
            Run = run,
            GlyphOrigin = origin,
            CanSkipInk = glyph => glyph.Cluster < 0 || glyph.Cluster >= text.Length
                || CharacterProperties.CanDecorationSkipInk(char.IsSurrogatePair(text, glyph.Cluster) ? char.ConvertToUtf32(text, glyph.Cluster) : text[glyph.Cluster]),
        };
    }

    static string TextOf(LayoutBox textBox) => InlineLayout.TextOf.TryGetValue(textBox, out var processed) ? processed : textBox.Text;

    /// <summary>ComputeDecorationThickness: auto is a tenth of the font size, from-font the font's own (else auto), a length rounded; at least 1 px.</summary>
    static float DecorationThicknessOf(DecorationThickness thickness, UsedFont font)
    {
        var auto = font.Size / 10f;
        var value = thickness.Kind switch
        {
            DecorationThicknessKind.FromFont => font.UnderlineThickness > 0 ? font.UnderlineThickness : auto,
            DecorationThicknessKind.Length => MathF.Round(thickness.Length.Pixels + thickness.Length.Percent / 100f * font.Size, MidpointRounding.AwayFromZero),
            _ => auto,
        };
        return MathF.Max(1, value);
    }

    /// <summary>text-underline-offset in pixels (a percentage of the decorating box's font size).</summary>
    static float UnderlineOffsetPixels(Length offset, UsedFont font) => offset.IsAuto ? 0 : offset.Pixels + offset.Percent / 100f * font.Size;

    /// <summary>
    /// The underline's top below its decorating box's content top (TextDecorationOffset::ComputeUnderlineOffset):
    /// auto, just below the ascent with a gap of half the thickness (none with an offset); from-font,
    /// at the font's own position; under, below the em box.
    /// </summary>
    static float UnderlineOffset(ComputedStyle boxStyle, UsedFont font, Length offset, float thickness)
    {
        var pixels = UnderlineOffsetPixels(offset, font);
        var position = boxStyle.TextUnderlinePosition;
        if ((position & UnderlinePosition.Under) != 0)
        {
            var under = LayoutRound(font.Ascent + font.EmDescent) + LayoutRound(pixels);
            return MathF.Floor(under) + 1;
        }
        if ((position & UnderlinePosition.FromFont) != 0)
            return MathF.Round(font.Ascent + font.UnderlinePosition + pixels, MidpointRounding.AwayFromZero);
        var gap = offset.IsAuto ? MathF.Max(1, MathF.Ceiling(thickness / 2)) : 0;
        return MathF.Truncate(font.Ascent + gap + MathF.Round(pixels, MidpointRounding.AwayFromZero));
    }

    enum DecorationLines { Underline, Overline, LineThrough }

    /// <summary>TextDecorationInfo::ComputeLineData: the line along the fragment at an offset below its top, with the second line's and the wave's offsets.</summary>
    static DecorationGeometry LineData(DecorationStyle style, DecorationLines kind, RectF text, float offset, float thickness)
    {
        var fromThickness = thickness + 1;
        var (doubleOffset, wavyOffset) = kind switch
        {
            DecorationLines.Underline => (fromThickness, fromThickness),
            DecorationLines.Overline => (-fromThickness, -fromThickness),
            _ => (MathF.Floor(fromThickness), 0f),
        };
        return new DecorationGeometry(style, new RectF(text.X, text.Y + offset, text.Width, thickness), doubleOffset, wavyOffset);
    }

    /// <summary>
    /// Paints a fragment's underlines and overlines (before its text) or its line-throughs (after):
    /// each decoration in order, its shadows first; underlines and overlines cut where glyphs cross
    /// them (text-decoration-skip-ink), the cuts staying for the lines after.
    /// </summary>
    void PaintDecorationLines(FragmentDecorations fragment, bool lineThrough, Color? colorOverride = null, RectF? clip = null)
    {
        if (clip is { } part)
            _canvas.PushClip(part);
        PaintDecorationLinesUnclipped(fragment, lineThrough, colorOverride);
        if (clip is not null)
            _canvas.PopClip();
    }

    void PaintDecorationLinesUnclipped(FragmentDecorations fragment, bool lineThrough, Color? colorOverride)
    {
        var geometries = new List<(DecorationGeometry Geometry, Color Color)>();
        var cuts = new List<RectF>();
        var skipInk = fragment.Style.TextDecorationSkipInk;
        foreach (var (decoration, box, thickness) in fragment.Decorations)
        {
            var color = colorOverride ?? decoration.Color;
            if (lineThrough)
            {
                if (decoration.LineThrough)
                    geometries.Add((LineData(decoration.Style, DecorationLines.LineThrough, fragment.Text, 2 * box.Font.Ascent / 3 - thickness / 2, thickness), color));
                continue;
            }
            if (decoration.Underline)
            {
                // The offset is from the decorating box; the line is placed on this fragment.
                var offset = UnderlineOffset(box.Style, box.Font, decoration.UnderlineOffset, thickness) + (box.ContentTop - fragment.Text.Y);
                var geometry = LineData(decoration.Style, DecorationLines.Underline, fragment.Text, offset, thickness);
                cuts.AddRange(InkCuts(fragment, geometry, skipInk));
                geometries.Add((geometry, color));
            }
            if (decoration.Overline)
            {
                var geometry = LineData(decoration.Style, DecorationLines.Overline, fragment.Text, -MathF.Floor(thickness), thickness);
                cuts.AddRange(InkCuts(fragment, geometry, skipInk));
                geometries.Add((geometry, color));
            }
        }
        if (geometries.Count == 0)
            return;
        // Cuts made by a line apply to it and to those drawn after it (Blink clips the context as it goes).
        var shadows = fragment.Style.TextShadows;
        for (var i = shadows.Length - 1; i >= 0; i--)
        {
            var shadow = shadows[i];
            var at = new Vector2(shadow.OffsetX, shadow.OffsetY);
            var layered = shadow.Blur > 0;
            if (layered)
                _canvas.PushLayer(new LayerOptions { Filters = [Filter.Blur(shadow.Blur / 2)] });
            DrawDecorationGeometries(geometries, cuts, at, shadow.Color);
            if (layered)
                _canvas.PopLayer();
        }
        DrawDecorationGeometries(geometries, cuts, Vector2.Zero, null);
    }

    /// <summary>
    /// Draws the lines less their cuts; a shadow's offset moves everything as Blink's draw looper
    /// does, after the lines were put on whole pixels.
    /// </summary>
    void DrawDecorationGeometries(List<(DecorationGeometry Geometry, Color Color)> geometries, List<RectF> cuts, Vector2 shift, Color? color)
    {
        var shifted = shift != Vector2.Zero;
        if (shifted)
            _canvas.PushTransform(Matrix3x2.CreateTranslation(shift));
        foreach (var (geometry, lineColor) in geometries)
        {
            var bounds = DecorationBounds(geometry).Inflate(geometry.Thickness + 2);
            foreach (var piece in Uncut(bounds, cuts))
            {
                _canvas.PushClip(piece);
                DrawDecorationLine(geometry, color ?? lineColor);
                _canvas.PopClip();
            }
        }
        if (shifted)
            _canvas.PopTransform();
    }

    /// <summary>
    /// Selected text with decorations, as Blink's highlight overlay paints it (PaintHighlightOverlays
    /// with the originating and selection layers): the selection's highlight, then part by part
    /// (before, in and after the selection) the underlines and overlines (laid along the whole
    /// fragment, clipped to the part, in the selection's text colour where selected), the part's
    /// glyphs (clipped to it, the end parts out to the ink), and the line-throughs.
    /// </summary>
    void PaintOverlaySelection(FragmentDecorations decorations, Func<int, int, bool, GlyphRun> slice, Vector2 origin, Color color,
        int textStart, int textEnd, int from, int to, RectF highlight, Color background, Color selectedColor, Func<int, float> xOf)
    {
        if (background.A > 0 && highlight.Width > 0 && highlight.Height > 0)
            _canvas.FillRect(highlight, background);
        var ink = InkBounds(slice(textStart, textEnd, false), origin);
        var inkLeft = MathF.Min(decorations.Text.Left, ink.IsEmpty ? decorations.Text.Left : ink.Left);
        var inkRight = MathF.Max(decorations.Text.Right, ink.IsEmpty ? decorations.Text.Right : ink.Right);
        static float Floor64(float v) => MathF.Floor(v * 64) / 64;
        static float Ceil64(float v) => MathF.Ceiling(v * 64) / 64;
        const float Far = 1e6f;
        foreach (var (partFrom, partTo, selected) in new[] { (textStart, from, false), (from, to, true), (to, textEnd, false) })
        {
            if (partFrom >= partTo)
                continue;
            var (a, b) = (xOf(partFrom), xOf(partTo));
            var left = Floor64(MathF.Min(a, b));
            var right = Ceil64(MathF.Max(a, b));
            var partColor = selected ? selectedColor : color;
            var partDecorations = decorations with { Run = slice(partFrom, partTo, false) };
            var decorationClip = SnapClip(RectF.FromEdges(left, -Far, right, Far));
            PaintDecorationLines(partDecorations, lineThrough: false, selected ? selectedColor : null, decorationClip);
            var textLeft = partFrom == textStart ? MathF.Min(left, inkLeft) : left;
            var textRight = partTo == textEnd ? MathF.Max(right, inkRight) : right;
            _canvas.PushClip(RectF.FromEdges(MathF.Floor(textLeft), -Far, MathF.Ceiling(textRight), Far));
            _canvas.DrawGlyphs(slice(partFrom, partTo, true), origin, partColor);
            _canvas.PopClip();
            PaintDecorationLines(partDecorations, lineThrough: true, selected ? selectedColor : null, decorationClip);
        }
    }

    /// <summary>A clip without antialiasing: the device pixels whose centres are inside.</summary>
    RectF SnapClip(RectF rect)
    {
        var scale = _canvas.Scale;
        float S(float v) => MathF.Abs(v) >= 1e5f ? v : MathF.Floor(v * scale + 0.5f) / scale;
        return RectF.FromEdges(S(rect.Left), S(rect.Top), S(rect.Right), S(rect.Bottom));
    }

    /// <summary>
    /// The parts of an area outside a set of cut rectangles (a clip-out), as rectangles on whole
    /// device pixels (Skia's clips without antialiasing take the pixels whose centres are inside).
    /// </summary>
    List<RectF> Uncut(RectF area, IEnumerable<RectF> cuts)
    {
        var scale = _canvas.Scale;
        float Snap1(float v) => MathF.Floor(v * scale + 0.5f) / scale;
        var holes = cuts.Select(r => RectF.FromEdges(Snap1(r.Left), Snap1(r.Top), Snap1(r.Right), Snap1(r.Bottom))).Where(r => !r.IsEmpty && !area.Intersect(r).IsEmpty).ToList();
        if (holes.Count == 0)
            return [area];
        var ys = holes.SelectMany(h => new[] { h.Top, h.Bottom }).Append(area.Top).Append(area.Bottom)
            .Select(y => Math.Clamp(y, area.Top, area.Bottom)).Distinct().Order().ToList();
        var pieces = new List<RectF>();
        for (var i = 0; i + 1 < ys.Count; i++)
        {
            var (top, bottom) = (ys[i], ys[i + 1]);
            var covered = holes.Where(h => h.Top <= top && h.Bottom >= bottom).Select(h => (h.Left, h.Right)).OrderBy(h => h.Left).ToList();
            var x = area.Left;
            foreach (var (left, right) in covered)
            {
                if (left > x)
                    pieces.Add(RectF.FromEdges(x, top, MathF.Min(left, area.Right), bottom));
                x = MathF.Max(x, right);
            }
            if (x < area.Right)
                pieces.Add(RectF.FromEdges(x, top, area.Right, bottom));
        }
        return pieces.Where(p => !p.IsEmpty).ToList();
    }

    /// <summary>
    /// TextPainter::ClipDecorationLine: where the fragment's glyphs (other than those Blink never
    /// skips) cross the line's band, less half a pixel at top and bottom, the cut widened by the
    /// thickness (at most 13 px) each side and a pixel up and down.
    /// </summary>
    static List<RectF> InkCuts(FragmentDecorations fragment, DecorationGeometry geometry, TextDecorationSkipInk skipInk)
    {
        var cuts = new List<RectF>();
        if (skipInk == TextDecorationSkipInk.None)
            return cuts;
        var bounds = DecorationBounds(geometry);
        bounds = RectF.FromEdges(bounds.Left, bounds.Top + 0.5f, bounds.Right, bounds.Bottom - 0.5f);
        var upper = bounds.Top - fragment.Baseline;
        var lower = upper + bounds.Height;
        var dilation = MathF.Min(geometry.Thickness, 13);
        var run = fragment.Run;
        foreach (var glyph in run.Glyphs)
        {
            if (skipInk == TextDecorationSkipInk.Auto && !fragment.CanSkipInk(glyph))
                continue;
            if (GlyphOutlines.Intercept(run.Font.Face, glyph.Glyph, run.Style.Size, run.Font.SyntheticBold, run.Font.SyntheticOblique, upper - glyph.Y, lower - glyph.Y) is not var (begin, end))
                continue;
            var x = fragment.GlyphOrigin.X + glyph.X;
            cuts.Add(RectF.FromEdges(x + begin - dilation, bounds.Top - 1, x + end + dilation, bounds.Bottom + 1));
        }
        return cuts;
    }

    /// <summary>DecorationLinePainter::Bounds: what a line covers (both lines of a double, the wave's tile height).</summary>
    static RectF DecorationBounds(DecorationGeometry geometry)
    {
        switch (geometry.Style)
        {
            case DecorationStyle.Dotted:
            case DecorationStyle.Dashed:
            {
                var thickness = MathF.Round(geometry.Thickness, MidpointRounding.AwayFromZero);
                var (start, end, y) = SnappedLinePoints(geometry.Line);
                return new RectF(start, y - thickness / 2, end - start, thickness);
            }
            case DecorationStyle.Wavy:
            {
                var (_, top, height) = Wave(geometry.Thickness);
                return new RectF(geometry.Line.X, geometry.Line.Y + geometry.WavyOffset + top, geometry.Line.Width, height);
            }
            case DecorationStyle.Double:
            {
                var rect = geometry.Line;
                return RectF.FromEdges(rect.X, rect.Y + MathF.Min(0, geometry.DoubleOffset), rect.Right, rect.Bottom + MathF.Max(0, geometry.DoubleOffset));
            }
            default:
                return geometry.Line;
        }
    }

    /// <summary>GetSnappedPointsForTextLine: the line's ends on whole pixels (x truncated) through the middle row.</summary>
    static (int Start, int End, int Y) SnappedLinePoints(RectF line) =>
        ((int)line.X, (int)line.Right, (int)MathF.Floor(line.Y + MathF.Max(line.Height / 2, 0.5f)));

    /// <summary>
    /// MakeWave and ComputeWavyPatternRect: the wavelength and control point distance (on half
    /// pixels), and the tile's top and height around the centreline (whole pixels around the stroke).
    /// </summary>
    static (float Wavelength, float Top, float Height) Wave(float thickness) => Wave(thickness, out _);

    static (float Wavelength, float Top, float Height) Wave(float thickness, out float controlDistance)
    {
        var clamped = MathF.Max(1, thickness);
        var wavelength = 1 + 2 * MathF.Round(2 * clamped + 0.5f, MidpointRounding.AwayFromZero);
        controlDistance = 0.5f + MathF.Round(3 * clamped + 0.5f, MidpointRounding.AwayFromZero);
        // The cubic's peak is √3/6 of the control point distance from the centreline (at y = 0.5).
        var amplitude = controlDistance * MathF.Sqrt(3) / 6;
        var top = MathF.Floor(0.5f - amplitude - thickness / 2);
        var bottom = MathF.Ceiling(0.5f + amplitude + thickness / 2);
        return (wavelength, top, bottom - top);
    }

    /// <summary>DecorationLinePainter::Paint for one line.</summary>
    void DrawDecorationLine(DecorationGeometry geometry, Color color)
    {
        if (geometry.Line.Width <= 0)
            return;
        switch (geometry.Style)
        {
            case DecorationStyle.Solid:
            case DecorationStyle.Double:
                // DrawLineAsRect: on whole pixels vertically, the thickness rounded down (at least 1).
                _canvas.FillRect(SnapLineRect(geometry.Line), color);
                if (geometry.Style == DecorationStyle.Double)
                    _canvas.FillRect(SnapLineRect(geometry.Line.Offset(0, geometry.DoubleOffset)), color);
                break;
            case DecorationStyle.Dotted:
            case DecorationStyle.Dashed:
                DrawDecorationStroke(geometry, color);
                break;
            case DecorationStyle.Wavy:
                DrawWavyDecoration(geometry, color);
                break;
        }
    }

    RectF SnapLineRect(RectF line)
    {
        var scale = _canvas.Scale;
        var y = MathF.Floor(line.Y * scale + 0.5f) / scale;
        var height = MathF.Max(MathF.Floor(line.Height * scale), 1) / scale;
        return new RectF(line.X, y, line.Width, height);
    }

    /// <summary>
    /// DrawLineAsStroke: a dashed or dotted stroke along the middle row (a half pixel down for odd
    /// thicknesses), with Blink's dash and gap lengths; dots wider than 3 px are round-capped zero dashes.
    /// </summary>
    void DrawDecorationStroke(DecorationGeometry geometry, Color color)
    {
        var (start, end, midY) = SnappedLinePoints(geometry.Line);
        var width = geometry.Thickness;
        var thickness = (int)MathF.Round(width, MidpointRounding.AwayFromZero);
        float y = midY + (thickness % 2 != 0 ? 0.5f : 0);
        float x1 = start, x2 = end;
        var dashed = geometry.Style == DecorationStyle.Dashed || thickness <= 3;
        if (!dashed)
        {
            x1 += thickness / 2f;
            x2 -= thickness / 2f;
        }
        var pattern = DashPattern(geometry.Style, thickness, end - start);
        if (pattern is not var (on, off, round))
        {
            _canvas.FillRect(RectF.FromEdges(x1, y - width / 2, x2, y + width / 2), color);
            return;
        }
        for (var x = x1; x <= x2 && on + off > 0; x += on + off)
        {
            if (round && on == 0)
                _canvas.DrawBox(new RectF(x - width / 2, y - width / 2, width, width), Brush.Solid(color), new CornerRadii(width / 2), Border.None);
            else if (x < x2)
                _canvas.FillRect(RectF.FromEdges(x, y - width / 2, MathF.Min(x + on, x2), y + width / 2), color);
        }
    }

    /// <summary>StyledStrokeData's dash effect: the dash and gap (dots as zero dashes with round caps), or none to draw solid.</summary>
    static (float On, float Off, bool Round)? DashPattern(DecorationStyle style, int dashWidth, float length)
    {
        static float BestGap(float stroke, float dash, float gap)
        {
            var available = stroke + gap;
            var minDashes = MathF.Floor(available / (dash + gap));
            var maxDashes = minDashes + 1;
            var minGap = (stroke - minDashes * dash) / (minDashes - 1);
            var maxGap = (stroke - maxDashes * dash) / (maxDashes - 1);
            return maxGap <= 0 || MathF.Abs(minGap - gap) < MathF.Abs(maxGap - gap) ? minGap : maxGap;
        }
        if (style == DecorationStyle.Dashed || dashWidth <= 3)
        {
            float dash = dashWidth, gap = dashWidth;
            if (style == DecorationStyle.Dashed)
            {
                dash *= dashWidth >= 3 ? 2 : 3;
                gap *= dashWidth >= 3 ? 1 : 2;
            }
            if (length <= dash * 2)
                return null;
            var twoDashes = 2 * dash + gap;
            if (length <= twoDashes)
            {
                var multiplier = length / twoDashes;
                return (dash * multiplier, gap * multiplier, false);
            }
            return (dash, style == DecorationStyle.Dashed ? BestGap(length, dash, gap) : gap, false);
        }
        var perDot = dashWidth * 2f;
        if (length < perDot)
            return (0, perDot, true);
        return (0, BestGap(length, dashWidth, dashWidth) + dashWidth - 1e-2f, true);
    }

    /// <summary>
    /// The wavy line (DecorationLinePainter's wavy tile): cubic waves a wavelength apart from a
    /// wavelength before the start, centred half a pixel below the offset line, stroked with the
    /// thickness and clipped to the decoration's width and the tile's height. cc rasterizes the
    /// tile as an image sampled without filtering, so the waves move to the nearest whole pixel.
    /// </summary>
    void DrawWavyDecoration(DecorationGeometry geometry, Color color)
    {
        var (wavelength, top, height) = Wave(geometry.Thickness, out var control);
        var line = geometry.Line;
        var scale = _canvas.Scale;
        float Nearest(float v) => MathF.Ceiling(v * scale - 0.5f) / scale - v;
        var tileTop = line.Y + geometry.WavyOffset + top;
        var (shiftX, shiftY) = (Nearest(line.X), Nearest(tileTop));
        var y = line.Y + geometry.WavyOffset + 0.5f + shiftY;
        var path = new CanvasPath();
        var x = line.X - wavelength + shiftX;
        path.MoveTo(new Vector2(x, y));
        while (x < line.Right + wavelength)
        {
            path.CubicTo(new Vector2(x + wavelength / 2, y + control), new Vector2(x + wavelength / 2, y - control), new Vector2(x + wavelength, y));
            x += wavelength;
        }
        _canvas.PushClip(new RectF(line.X, tileTop + shiftY, line.Width, height));
        _canvas.StrokePath(path, Brush.Solid(color), new StrokeStyle(MathF.Max(1, geometry.Thickness)));
        _canvas.PopClip();
    }
}
