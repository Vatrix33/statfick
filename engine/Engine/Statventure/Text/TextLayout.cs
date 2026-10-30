using System.Globalization;
using System.Text;
using HarfBuzzSharp;
using Statventure.Core;
using HbBuffer = HarfBuzzSharp.Buffer;

namespace Statventure.Text;

public enum TextTransform
{
    None,
    Uppercase,
    Lowercase,
    Capitalize,
}

public enum WhiteSpace
{
    /// <summary>Spaces and newlines collapse; lines wrap.</summary>
    Normal,
    /// <summary>Spaces and newlines collapse; no wrapping.</summary>
    NoWrap,
    /// <summary>Spaces and newlines are kept; no wrapping.</summary>
    Pre,
    /// <summary>Spaces and newlines are kept; lines wrap.</summary>
    PreWrap,
    /// <summary>Spaces collapse, newlines are kept; lines wrap.</summary>
    PreLine,
}

public enum TextAlign
{
    Left,
    Center,
    Right,
}

public enum OverflowWrap
{
    Normal,
    /// <summary>Words that don't fit may break anywhere (and count so for min-content width).</summary>
    Anywhere,
}

/// <summary>How a run of text looks (resolved CSS values; lengths in logical pixels).</summary>
public sealed record TextStyle
{
    public string[] Families { get; init; } = ["Lato"];
    public float Size { get; init; } = 16f;
    public int Weight { get; init; } = 400;
    public bool Italic { get; init; }
    public Color Color { get; init; } = Color.Black;
    public float LetterSpacing { get; init; }
    public TextTransform Transform { get; init; }
    public bool Underline { get; init; }
    /// <summary>The line height in pixels; null for "normal" (the font's own ascent + descent + gap).</summary>
    public float? LineHeight { get; init; }
    /// <summary>Text is positioned at subpixels (Chromium's FontRenderStyle): font metrics round differently on Linux.</summary>
    public bool SubpixelPositioning { get; init; }
}

public readonly record struct TextSpan(string Text, TextStyle Style);

public sealed record ParagraphStyle
{
    public float MaxWidth { get; init; } = float.PositiveInfinity;
    public TextAlign Align { get; init; } = TextAlign.Left;
    public WhiteSpace WhiteSpace { get; init; } = WhiteSpace.Normal;
    public OverflowWrap OverflowWrap { get; init; } = OverflowWrap.Normal;
    /// <summary>Show "…" where text is cut off (text-overflow: ellipsis, or the last of <see cref="MaxLines"/>).</summary>
    public bool Ellipsis { get; init; }
    public int MaxLines { get; init; } = int.MaxValue;
    /// <summary>The block's own style (its "strut"); null to use the first span's.</summary>
    public TextStyle? Strut { get; init; }
}

public readonly record struct PositionedGlyph(uint Glyph, float X, float Y, int Cluster);

public sealed class GlyphRun
{
    public required ResolvedFont Font { get; init; }
    public required TextStyle Style { get; init; }
    public List<PositionedGlyph> Glyphs { get; } = [];
    /// <summary>Where the run starts on its line and how wide it is.</summary>
    public float X { get; set; }
    public float Width { get; set; }
}

public sealed class TextLine
{
    public float Top { get; set; }
    public float Baseline { get; set; }
    public float Height { get; set; }
    /// <summary>Width of the line's content, without hanging spaces.</summary>
    public float Width { get; set; }
    /// <summary>Offset of the line from the left edge (alignment).</summary>
    public float Left { get; set; }
    public List<GlyphRun> Runs { get; } = [];
    public int TextStart { get; set; }
    public int TextEnd { get; set; }
}

/// <summary>
/// Lays out a paragraph of styled text as CSS does: white-space handling, shaping (HarfBuzz),
/// wrapping at break opportunities, line boxes from the half-leading model with rounded font
/// metrics, alignment, ellipsis and line clamping, and the intrinsic widths the UI layout needs.
/// </summary>
public sealed partial class TextLayout
{
    public IReadOnlyList<TextLine> Lines => _lines;
    public string Text { get; private set; } = "";
    public float Width { get; private set; }
    public float Height { get; private set; }
    public float MinContentWidth { get; private set; }
    public float MaxContentWidth { get; private set; }
    public float FirstBaseline => _lines.Count > 0 ? _lines[0].Baseline : 0;
    public float LastBaseline => _lines.Count > 0 ? _lines[^1].Baseline : 0;
    public bool Truncated { get; private set; }

    readonly List<TextLine> _lines = [];

    // One shaped character cluster: the unit of line breaking.
    sealed class Cluster
    {
        public int Start;
        public int End;
        public float Width;
        public GlyphRun Run = null!;
        public int GlyphStart;
        public int GlyphCount;
        public bool IsSpace;
        public bool IsNewline;
        public bool BreakAfter;
    }

    public static TextLayout Create(FontLibrary fonts, string text, TextStyle style, ParagraphStyle? paragraph = null) =>
        Create(fonts, [new TextSpan(text, style)], paragraph ?? new ParagraphStyle());

    public static TextLayout Create(FontLibrary fonts, IReadOnlyList<TextSpan> spans, ParagraphStyle paragraph)
    {
        var layout = new TextLayout();
        layout.Build(fonts, spans, paragraph);
        return layout;
    }

    static string ApplyTransform(string text, TextTransform transform) => transform switch
    {
        TextTransform.Uppercase => text.ToUpperInvariant(),
        TextTransform.Lowercase => text.ToLowerInvariant(),
        TextTransform.Capitalize => CultureInfo.InvariantCulture.TextInfo.ToTitleCase(text),
        _ => text,
    };

    void Build(FontLibrary fonts, IReadOnlyList<TextSpan> spans, ParagraphStyle paragraph)
    {
        var strut = paragraph.Strut ?? (spans.Count > 0 ? spans[0].Style : new TextStyle());

        // 1. White space and text-transform.
        var collapseSpaces = paragraph.WhiteSpace is WhiteSpace.Normal or WhiteSpace.NoWrap or WhiteSpace.PreLine;
        var keepNewlines = paragraph.WhiteSpace is WhiteSpace.Pre or WhiteSpace.PreWrap or WhiteSpace.PreLine;
        var text = new StringBuilder();
        var styles = new List<(int Start, int End, TextStyle Style)>();
        foreach (var span in spans)
        {
            var start = text.Length;
            foreach (var c in ApplyTransform(span.Text, span.Style.Transform))
            {
                if (c == '\r')
                    continue;
                if (c == '\n' && keepNewlines)
                {
                    // A newline also swallows spaces before it when spaces collapse.
                    if (collapseSpaces)
                        while (text.Length > 0 && text[^1] == ' ')
                            text.Length--;
                    text.Append('\n');
                    continue;
                }
                var ch = c is '\n' or '\t' ? ' ' : c;
                if (ch == ' ' && collapseSpaces && (text.Length == 0 || text[^1] is ' ' or '\n'))
                    continue;
                text.Append(ch);
            }
            if (text.Length > start)
                styles.Add((start, text.Length, span.Style));
        }
        // Collapsed trailing spaces at the end of the paragraph go away.
        if (collapseSpaces)
            while (text.Length > 0 && text[^1] == ' ')
            {
                text.Length--;
                if (styles.Count > 0)
                    styles[^1] = (styles[^1].Start, Math.Min(styles[^1].End, text.Length), styles[^1].Style);
            }
        Text = text.ToString();
        if (Text.Length == 0)
            return;

        // 2. Font runs and shaping.
        var clusters = new List<Cluster>();
        foreach (var (start, end, style) in styles)
        {
            if (end <= start)
                continue;
            // As in browsers, families nobody has fall back to the default font.
            var preferred = fonts.Resolve(style.Families, style.Weight, style.Italic) ?? fonts.ResolveDefault(style.Weight, style.Italic)
                ?? throw new InvalidOperationException("no font for " + string.Join(", ", style.Families));
            var runStart = start;
            ResolvedFont? runFont = null;
            for (var i = start; i <= end; i++)
            {
                ResolvedFont? font = null;
                if (i < end)
                {
                    var codepoint = char.IsHighSurrogate(Text[i]) && i + 1 < end ? char.ConvertToUtf32(Text[i], Text[i + 1]) : Text[i];
                    font = Text[i] is ' ' or '\n' ? runFont ?? preferred : fonts.ResolveFor(codepoint, preferred) ?? preferred;
                    if (char.IsLowSurrogate(Text[i]))
                        font = runFont;
                }
                if (i == end || (runFont is { } current && font is { } next && next.Face != current.Face))
                {
                    if (i > runStart)
                        Shape(clusters, runFont ?? preferred, style, runStart, i);
                    runStart = i;
                }
                if (font is not null)
                    runFont = font;
            }
        }

        // 3. Break opportunities.
        for (var i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];
            var c = Text[cluster.Start];
            cluster.IsSpace = c == ' ';
            cluster.IsNewline = c == '\n';
            var nextIsSpace = i + 1 < clusters.Count && Text[clusters[i + 1].Start] == ' ';
            cluster.BreakAfter = (cluster.IsSpace && !nextIsSpace) || cluster.IsNewline
                || ((c is '-' or '–' or '—') && i > 0 && i + 1 < clusters.Count && !clusters[i - 1].IsSpace && Text[clusters[i + 1].Start] != ' ');
        }

        // 4. Intrinsic widths.
        float word = 0, line = 0, longestWord = 0, longestLine = 0;
        foreach (var cluster in clusters)
        {
            if (cluster.IsNewline)
            {
                longestLine = MathF.Max(longestLine, line);
                line = 0;
                longestWord = MathF.Max(longestWord, word);
                word = 0;
                continue;
            }
            line += cluster.Width;
            if (paragraph.OverflowWrap == OverflowWrap.Anywhere)
                longestWord = MathF.Max(longestWord, cluster.IsSpace ? 0 : cluster.Width);
            else if (!cluster.IsSpace)
                word += cluster.Width;
            if (cluster.BreakAfter || cluster.IsSpace)
            {
                longestWord = MathF.Max(longestWord, word);
                word = 0;
            }
        }
        longestWord = MathF.Max(longestWord, word);
        longestLine = MathF.Max(longestLine, TrimmedWidth(clusters, LastLineStart(clusters), clusters.Count));
        MinContentWidth = paragraph.WhiteSpace is WhiteSpace.NoWrap or WhiteSpace.Pre ? longestLine : longestWord;
        MaxContentWidth = longestLine;

        // 5. Lines.
        var wrap = paragraph.WhiteSpace is WhiteSpace.Normal or WhiteSpace.PreWrap or WhiteSpace.PreLine;
        var maxWidth = paragraph.MaxWidth;
        var lineRanges = new List<(int Start, int End)>();
        var lineStart = 0;
        var position = 0;
        while (lineStart < clusters.Count)
        {
            var width = 0f;
            var lastBreak = -1;
            var end = lineStart;
            var forced = false;
            for (position = lineStart; position < clusters.Count; position++)
            {
                var cluster = clusters[position];
                if (cluster.IsNewline)
                {
                    end = position + 1;
                    forced = true;
                    break;
                }
                width += cluster.Width;
                // Spaces at the end of a line hang over the edge, so they never cause a wrap.
                if (wrap && !cluster.IsSpace && width > maxWidth + 0.01f && position > lineStart)
                {
                    if (lastBreak >= lineStart)
                        end = lastBreak + 1;
                    else if (paragraph.OverflowWrap == OverflowWrap.Anywhere)
                        end = position;
                    else
                    {
                        // No opportunity yet: the word overflows to the next opportunity.
                        while (position + 1 < clusters.Count && !clusters[position].BreakAfter && !clusters[position + 1].IsNewline)
                            position++;
                        end = position + 1;
                    }
                    break;
                }
                if (cluster.BreakAfter)
                    lastBreak = position;
            }
            if (position >= clusters.Count && !forced)
                end = clusters.Count;
            if (end <= lineStart)
                end = lineStart + 1;
            lineRanges.Add((lineStart, end));
            lineStart = end;
        }
        if (clusters.Count > 0 && clusters[^1].IsNewline)
            lineRanges.Add((clusters.Count, clusters.Count));

        // Line clamp.
        var clampedAt = -1;
        if (lineRanges.Count > paragraph.MaxLines)
        {
            lineRanges.RemoveRange(paragraph.MaxLines, lineRanges.Count - paragraph.MaxLines);
            clampedAt = lineRanges.Count - 1;
            Truncated = true;
        }

        // 6. Line boxes.
        var top = 0f;
        for (var l = 0; l < lineRanges.Count; l++)
        {
            var (start, end) = lineRanges[l];
            var textLine = new TextLine { Top = top, TextStart = start < clusters.Count ? clusters[start].Start : Text.Length, TextEnd = end > start ? clusters[end - 1].End : Text.Length };
            var contentEnd = end;
            while (contentEnd > start && (clusters[contentEnd - 1].IsSpace || clusters[contentEnd - 1].IsNewline))
                contentEnd--;
            var needsEllipsis = paragraph.Ellipsis && (l == clampedAt || (!wrap && TrimmedWidth(clusters, start, end) > maxWidth + 0.01f));
            List<Cluster> lineClusters = clusters.GetRange(start, end - start);
            GlyphRun? ellipsisRun = null;
            if (needsEllipsis)
            {
                Truncated = true;
                var last = start < end ? clusters[Math.Max(start, contentEnd - 1)] : null;
                var style = last?.Run.Style ?? strut;
                var font = last?.Run.Font ?? ResolveStrut(fonts, strut);
                ellipsisRun = ShapeStandalone(font, style, "…");
                var available = maxWidth - ellipsisRun.Width;
                var kept = 0f;
                var keep = 0;
                for (var i = start; i < contentEnd; i++)
                {
                    if (kept + clusters[i].Width > available + 0.01f)
                        break;
                    kept += clusters[i].Width;
                    keep++;
                }
                // Chromium keeps a space before the ellipsis.
                lineClusters = clusters.GetRange(start, keep);
            }
            BuildLine(textLine, lineClusters, ellipsisRun, strut, fonts);
            textLine.Width = needsEllipsis ? textLine.Runs.Sum(r => r.Width) : TrimmedWidth(clusters, start, end);
            if (!float.IsInfinity(maxWidth))
            {
                var free = maxWidth - textLine.Width;
                textLine.Left = paragraph.Align switch
                {
                    TextAlign.Center => free / 2f,
                    TextAlign.Right => free,
                    _ => 0f,
                };
            }
            _lines.Add(textLine);
            top += textLine.Height;
        }
        Height = top;
        Width = _lines.Count > 0 ? _lines.Max(l => l.Width) : 0;
    }

    static int LastLineStart(List<Cluster> clusters)
    {
        for (var i = clusters.Count - 1; i >= 0; i--)
            if (clusters[i].IsNewline)
                return i + 1;
        return 0;
    }

    static float TrimmedWidth(List<Cluster> clusters, int start, int end)
    {
        while (end > start && (clusters[end - 1].IsSpace || clusters[end - 1].IsNewline))
            end--;
        var width = 0f;
        for (var i = start; i < end; i++)
            if (!clusters[i].IsNewline)
                width += clusters[i].Width;
        return width;
    }

    void Shape(List<Cluster> clusters, ResolvedFont font, TextStyle style, int start, int end)
    {
        // Advances stay fractional, as in Blink's layout; the canvas snaps glyphs when it draws them.
        var face = font.Face;
        var scale = AdvanceScale(face, style.Size);
        using var buffer = new HbBuffer();
        buffer.AddUtf16(Text.AsSpan(), start, end - start);
        buffer.GuessSegmentProperties();
        face.HbFont.Shape(buffer);
        var infos = buffer.GetGlyphInfoSpan();
        var positions = buffer.GetGlyphPositionSpan();
        var run = new GlyphRun { Font = font, Style = style };
        var pen = 0f;
        Cluster? current = null;
        for (var i = 0; i < infos.Length; i++)
        {
            var clusterIndex = (int)infos[i].Cluster;
            if (current is null || clusterIndex != current.Start)
            {
                current = new Cluster { Start = clusterIndex, Run = run, GlyphStart = run.Glyphs.Count };
                clusters.Add(current);
            }
            var advance = Advance(face, infos[i].Codepoint, positions[i].XAdvance, style.Size, scale);
            run.Glyphs.Add(new PositionedGlyph(infos[i].Codepoint, pen + positions[i].XOffset * scale, -positions[i].YOffset * scale, clusterIndex));
            pen += advance;
            current.Width += advance;
            current.GlyphCount++;
        }
        // Letter spacing goes after every character; cluster ends follow the text.
        var clusterList = clusters.Where(c => c.Run == run).ToList();
        for (var i = 0; i < clusterList.Count; i++)
        {
            clusterList[i].End = i + 1 < clusterList.Count ? clusterList[i + 1].Start : end;
            if (style.LetterSpacing != 0 && Text[clusterList[i].Start] != '\n')
                clusterList[i].Width += style.LetterSpacing * Math.Max(1, clusterList[i].End - clusterList[i].Start);
        }
    }

    /// <summary>
    /// Pixels per font unit for advances and offsets. Blink's font cache keeps sizes to 0.01 px,
    /// truncated in float arithmetic (FontCacheKey: 12 px at 1.65 is 19.79), and Skia sets
    /// FreeType's size in 26.6 fixed point, truncated (SkScalarToFDot6), taking linear advances at
    /// that size: a 26.4 px font advances as a 26.390625 px one.
    /// </summary>
    internal static float AdvanceScale(FontFace face, float size)
    {
        var hundredths = (int)(size * 100f);
        var keyed = hundredths / 100f;
        return MathF.Floor(keyed * 64) / 64 / face.UnitsPerEm;
    }

    /// <summary>
    /// A glyph's advance in pixels as Blink gets it on Linux: Skia's Fontations advance (skrifa
    /// scales the hmtx advance as FreeType does: a 16.16 factor from the size in 26.6 over the em,
    /// then FT_MulDiv into 1/65536 px), plus what shaping adjusted (kerning), scaled linearly.
    /// </summary>
    internal static float Advance(FontFace face, uint glyph, int xAdvanceUnits, float size, float scale)
    {
        var nominal = face.HbFont.GetHorizontalGlyphAdvance(glyph);
        var keyed = (int)(size * 100f) / 100f;
        var fixedScale = Autohint.Fixed.Div((int)(keyed * 64f), face.UnitsPerEm);
        var advance = Autohint.Fixed.MulDiv(fixedScale, nominal, 64) / 65536f;
        return advance + (xAdvanceUnits - nominal) * scale;
    }

    static GlyphRun ShapeStandalone(ResolvedFont font, TextStyle style, string text)
    {
        var scale = AdvanceScale(font.Face, style.Size);
        using var buffer = new HbBuffer();
        buffer.AddUtf16(text);
        buffer.GuessSegmentProperties();
        font.Face.HbFont.Shape(buffer);
        var run = new GlyphRun { Font = font, Style = style };
        var pen = 0f;
        var infos = buffer.GetGlyphInfoSpan();
        var positions = buffer.GetGlyphPositionSpan();
        for (var i = 0; i < infos.Length; i++)
        {
            run.Glyphs.Add(new PositionedGlyph(infos[i].Codepoint, pen + positions[i].XOffset * scale, -positions[i].YOffset * scale, 0));
            pen += Advance(font.Face, infos[i].Codepoint, positions[i].XAdvance, style.Size, scale) + style.LetterSpacing;
        }
        run.Width = pen;
        return run;
    }

    /// <summary>
    /// Font metrics at a size, rounded as Blink rounds them (FontMetrics::AscentDescentWithHacks).
    /// With text at subpixel positions on Linux, a descent that rounded down takes a pixel from the
    /// ascent (so descenders aren't cut off).
    /// </summary>
    public static (float Ascent, float Descent, float LineGap) Metrics(FontFace face, float size, bool subpixelPositioning = false)
    {
        var scale = size / face.UnitsPerEm;
        // Skia and Blink round halves up (SkScalarRoundToScalar, lroundf), not to even.
        var ascent = MathF.Round(face.Ascent * scale, MidpointRounding.AwayFromZero);
        var exactDescent = face.Descent * scale;
        var descent = MathF.Round(exactDescent, MidpointRounding.AwayFromZero);
        if (subpixelPositioning && !Platform.PlatformBehaviour.Windows && descent < exactDescent && ascent >= 1)
        {
            descent++;
            ascent--;
        }
        return (ascent, descent, MathF.Round(face.LineGap * scale, MidpointRounding.AwayFromZero));
    }

    static (float Above, float Below) InlineBox(FontFace face, TextStyle style)
    {
        var (ascent, descent, gap) = Metrics(face, style.Size, style.SubpixelPositioning);
        var lineHeight = style.LineHeight ?? ascent + descent + gap;
        // Blink floors the leading above the text to whole pixels; the rest goes below.
        var leading = lineHeight - (ascent + descent);
        var above = MathF.Floor(leading / 2f);
        return (ascent + above, descent + leading - above);
    }

    /// <summary>The block's own font (as for text, families nobody has fall back to the default font).</summary>
    static ResolvedFont ResolveStrut(FontLibrary fonts, TextStyle strut) =>
        fonts.Resolve(strut.Families, strut.Weight, strut.Italic) ?? fonts.ResolveDefault(strut.Weight, strut.Italic)
        ?? throw new InvalidOperationException("no font for " + string.Join(", ", strut.Families));

    static void BuildLine(TextLine line, List<Cluster> clusters, GlyphRun? ellipsis, TextStyle strut, FontLibrary fonts)
    {
        // The strut (the block's own font and line height) is always part of a line.
        var strutFont = ResolveStrut(fonts, strut);
        var (above, below) = InlineBox(strutFont.Face, strut);
        var x = 0f;
        GlyphRun? current = null;
        GlyphRun? source = null;
        foreach (var cluster in clusters)
        {
            if (cluster.IsNewline)
                continue;
            if (source != cluster.Run)
            {
                source = cluster.Run;
                current = new GlyphRun { Font = cluster.Run.Font, Style = cluster.Run.Style, X = x };
                line.Runs.Add(current);
                var box = InlineBox(current.Font.Face, current.Style);
                above = MathF.Max(above, box.Above);
                below = MathF.Max(below, box.Below);
            }
            var origin = cluster.Run.Glyphs[cluster.GlyphStart].X;
            for (var g = 0; g < cluster.GlyphCount; g++)
            {
                var glyph = cluster.Run.Glyphs[cluster.GlyphStart + g];
                current!.Glyphs.Add(glyph with { X = x - current.X + (glyph.X - origin) });
            }
            x += cluster.Width;
            current!.Width = x - current.X;
        }
        if (ellipsis is not null)
        {
            ellipsis.X = x;
            line.Runs.Add(ellipsis);
            var box = InlineBox(ellipsis.Font.Face, ellipsis.Style);
            above = MathF.Max(above, box.Above);
            below = MathF.Max(below, box.Below);
        }
        line.Baseline = line.Top + above;
        line.Height = above + below;
    }
}
