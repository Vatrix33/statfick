using System.Globalization;
using System.Numerics;
using System.Text;
using HarfBuzzSharp;
using Statventure.Core;
using Statventure.Text;
using Statventure.UI.Style;
using HbBuffer = HarfBuzzSharp.Buffer;
using TextAlignment = Statventure.UI.Style.TextAlignment;

namespace Statventure.UI.Layout;

/// <summary>
/// Inline formatting contexts (CSS 2 §9.4.2, §10.8; CSS Text 3): white-space collapsing across
/// inline boxes, shaping with HarfBuzz, soft wrap opportunities, greedy line breaking, inline box
/// fragments with their start and end edges, atomic inlines, vertical-align, text-align (with
/// justify), text-overflow: ellipsis and -webkit-line-clamp, and min-content / max-content widths.
/// Font metrics are rounded and the leading split as Blink does.
/// </summary>
internal sealed class InlineLayout
{
    readonly LayoutEngine _engine;

    public InlineLayout(LayoutEngine engine)
    {
        _engine = engine;
    }

    FontLibrary Fonts => _engine.Context.Fonts;

    // ---------- Fonts and metrics ----------

    internal ResolvedFont FontFor(ComputedStyle style) =>
        Fonts.Resolve(style.FontFamilies, style.FontWeight, style.FontItalic)
        ?? Fonts.ResolveDefault(style.FontWeight, style.FontItalic)
        ?? throw new InvalidOperationException("no fonts are loaded");

    /// <summary>A box's font metrics (rounded) and its line-height.</summary>
    internal readonly record struct Metrics(float Ascent, float Descent, float LineHeight, float XHeight)
    {
        /// <summary>Distance from the baseline up to the top of the box's layout bounds (Blink floors the top half-leading).</summary>
        public float Above => Ascent + MathF.Floor((LineHeight - (Ascent + Descent)) / 2f);
        public float Below => LineHeight - Above;
    }

    internal Metrics MetricsFor(ComputedStyle style)
    {
        var font = FontFor(style);
        var (ascent, descent, gap) = TextLayout.Metrics(font.Face, style.FontSize, style.SubpixelPositioning);
        // ComputedLineHeightAsFixed: a number line-height is a percentage of the font size rounded to
        // a LayoutUnit (ComputedFontSizeAsFixed), the result truncated; a length is rounded.
        var lineHeight = style.LineHeight.Kind switch
        {
            LineHeightKind.Number => LayoutUnit.Truncate(LayoutUnit.Round(style.FontSize) * (float)(style.LineHeight.Value * 100.0) / 100f),
            LineHeightKind.Length => LayoutUnit.Round(style.LineHeight.Value),
            _ => ascent + descent + gap,
        };
        return new Metrics(ascent, descent, lineHeight, font.Face.XHeight * style.FontSize / font.Face.UnitsPerEm);
    }

    (float Ascent, float Descent) PrimaryMetrics(ComputedStyle style)
    {
        var (ascent, descent, _) = TextLayout.Metrics(FontFor(style).Face, style.FontSize, style.SubpixelPositioning);
        return (ascent, descent);
    }

    internal static TextStyle TextStyleFor(ComputedStyle style) => new()
    {
        Families = style.FontFamilies,
        Size = style.FontSize,
        SubpixelPositioning = style.SubpixelPositioning,
        Weight = style.FontWeight,
        Italic = style.FontItalic,
        Color = style.Color,
        LetterSpacing = style.LetterSpacing,
    };

    // ---------- Shaping ----------

    /// <summary>One shaped character cluster.</summary>
    internal sealed class ShapedCluster
    {
        public int Start, End;
        public float Width;
        public int GlyphStart, GlyphCount;
    }

    internal sealed class ShapedText
    {
        public required ResolvedFont Font;
        public required List<PositionedGlyph> Glyphs;
        public required List<ShapedCluster> Clusters;
        public float Ascent, Descent;
    }

    readonly record struct ShapeKey(string Text, int Face, bool Bold, bool Oblique, float Size, float LetterSpacing, float WordSpacing, bool Tabular);
    readonly Dictionary<ShapeKey, ShapedText> _shapeCache = [];
    static readonly Feature[] TabularFeatures = [Feature.Parse("tnum")];
    // Non-zero letter-spacing turns optional ligatures off (as Blink does); kerning stays.
    static readonly Feature[] SpacedFeatures = [Feature.Parse("liga=0"), Feature.Parse("clig=0"), Feature.Parse("dlig=0"), Feature.Parse("hlig=0"), Feature.Parse("calt=0")];
    static readonly Feature[] SpacedTabularFeatures = [.. SpacedFeatures, Feature.Parse("tnum")];

    ShapedText Shape(string text, ResolvedFont font, ComputedStyle style)
    {
        var key = new ShapeKey(text, font.Face.Id, font.SyntheticBold, font.SyntheticOblique, style.FontSize, style.LetterSpacing, style.WordSpacing, style.TabularNumbers);
        if (_shapeCache.TryGetValue(key, out var cached))
            return cached;
        if (_shapeCache.Count > 50000)
            _shapeCache.Clear();
        var face = font.Face;
        var scale = TextLayout.AdvanceScale(face, style.FontSize);
        using var buffer = new HbBuffer();
        buffer.AddUtf16(text);
        buffer.GuessSegmentProperties();
        var spaced = style.LetterSpacing != 0;
        if (spaced || style.TabularNumbers)
            face.HbFont.Shape(buffer, spaced ? (style.TabularNumbers ? SpacedTabularFeatures : SpacedFeatures) : TabularFeatures);
        else
            face.HbFont.Shape(buffer);
        var infos = buffer.GetGlyphInfoSpan();
        var positions = buffer.GetGlyphPositionSpan();
        var glyphs = new List<PositionedGlyph>(infos.Length);
        var clusters = new List<ShapedCluster>();
        var pen = 0f;
        ShapedCluster? current = null;
        for (var i = 0; i < infos.Length; i++)
        {
            var clusterIndex = (int)infos[i].Cluster;
            if (current is null || clusterIndex != current.Start)
            {
                current = new ShapedCluster { Start = clusterIndex, GlyphStart = glyphs.Count };
                clusters.Add(current);
            }
            var advance = TextLayout.Advance(face, infos[i].Codepoint, positions[i].XAdvance, style.FontSize, scale);
            glyphs.Add(new PositionedGlyph(infos[i].Codepoint, pen + positions[i].XOffset * scale, -positions[i].YOffset * scale, clusterIndex));
            pen += advance;
            current.Width += advance;
            current.GlyphCount++;
        }
        for (var i = 0; i < clusters.Count; i++)
        {
            var cluster = clusters[i];
            cluster.End = i + 1 < clusters.Count ? clusters[i + 1].Start : text.Length;
            // Letter spacing after every character; word spacing after every space.
            if (style.LetterSpacing != 0)
                cluster.Width += style.LetterSpacing * Math.Max(1, cluster.End - cluster.Start);
            if (style.WordSpacing != 0 && text[cluster.Start] is ' ' or ' ')
                cluster.Width += style.WordSpacing;
        }
        var (ascent, descent, _) = TextLayout.Metrics(face, style.FontSize, style.SubpixelPositioning);
        var shaped = new ShapedText { Font = font, Glyphs = glyphs, Clusters = clusters, Ascent = ascent, Descent = descent };
        _shapeCache[key] = shaped;
        return shaped;
    }

    // ---------- Items ----------

    enum PieceKind { Cluster, Open, Close, Atomic, Break, Placeholder }

    sealed class Piece
    {
        public PieceKind Kind;
        public float Width;
        /// <summary>The box: the text box (clusters), inline box (open/close), atomic, placeholder or br.</summary>
        public LayoutBox Box = null!;
        /// <summary>The inline box the piece sits in (or the IFC root).</summary>
        public LayoutBox Parent = null!;
        public ComputedStyle Style => Box.Style;
        public ShapedText? Shaped;
        public ShapedCluster? Cluster;
        /// <summary>Clusters: the position of the cluster in the text box's processed text.</summary>
        public int TextStart, TextEnd;
        public char Char;
        public bool Space;
        public bool Collapsible;
        public bool BreakAfter;
        public bool Anywhere;
        public bool Wraps;
        /// <summary>Removed at a line start or end (collapsible spaces there).</summary>
        public bool Removed;
    }

    sealed class Paragraph
    {
        public readonly List<Piece> Pieces = [];
        public readonly Dictionary<LayoutBox, string> ProcessedText = [];
    }

    /// <summary>The processed (collapsed, transformed) text of each text box in the last layout, for painting and hit testing.</summary>
    internal static readonly System.Runtime.CompilerServices.ConditionalWeakTable<LayoutBox, string> TextOf = new();

    /// <summary>For each text box, the offset in its DOM text each processed character comes from (and one past the end), for selections.</summary>
    internal static readonly System.Runtime.CompilerServices.ConditionalWeakTable<LayoutBox, int[]> SourceOffsetsOf = new();

    /// <summary>
    /// The block's inline content as pieces (shaped clusters, inline box edges, atomics ...). Kept on
    /// the box until something in it changes; the parts that depend on the layout (inline box edges,
    /// removed spaces; atomics are sized by their users) are set again each time.
    /// </summary>
    Paragraph Collect(LayoutBox root, bool intrinsic)
    {
        if (root.Paragraph is not Paragraph paragraph)
        {
            paragraph = CollectPieces(root);
            root.Paragraph = paragraph;
        }
        foreach (var piece in paragraph.Pieces)
        {
            piece.Removed = false;
            if (piece.Kind == PieceKind.Open)
            {
                ResolveInlineEdges(piece.Box, root);
                piece.Width = piece.Box.Margin.Left + piece.Box.Border.Left + piece.Box.Padding.Left;
            }
            else if (piece.Kind == PieceKind.Close)
                piece.Width = piece.Box.Margin.Right + piece.Box.Border.Right + piece.Box.Padding.Right;
        }
        return paragraph;
    }

    Paragraph CollectPieces(LayoutBox root)
    {
        var paragraph = new Paragraph();
        var previousSpace = true; // spaces at the start of the paragraph go away
        void Walk(LayoutBox parent)
        {
            foreach (var child in parent.Children)
            {
                switch (child.Type)
                {
                    case BoxType.Text:
                        AddText(paragraph, child, parent, ref previousSpace);
                        break;
                    case BoxType.Inline:
                        paragraph.Pieces.Add(new Piece { Kind = PieceKind.Open, Box = child, Parent = parent });
                        Walk(child);
                        paragraph.Pieces.Add(new Piece { Kind = PieceKind.Close, Box = child, Parent = parent });
                        break;
                    case BoxType.LineBreak:
                        paragraph.Pieces.Add(new Piece { Kind = PieceKind.Break, Box = child, Parent = parent });
                        previousSpace = true;
                        break;
                    default:
                        if (child.IsOutOfFlow)
                            paragraph.Pieces.Add(new Piece { Kind = PieceKind.Placeholder, Box = child, Parent = parent });
                        else
                        {
                            paragraph.Pieces.Add(new Piece { Kind = PieceKind.Atomic, Box = child, Parent = parent, Char = '￼', Wraps = Wraps(parent.Style) });
                            previousSpace = false;
                        }
                        break;
                }
            }
        }
        Walk(root);
        MarkBreaks(paragraph.Pieces);
        return paragraph;
    }

    static bool Wraps(ComputedStyle style) => style.WhiteSpace is Text.WhiteSpace.Normal or Text.WhiteSpace.PreWrap or Text.WhiteSpace.PreLine;

    void ResolveInlineEdges(LayoutBox box, LayoutBox root)
    {
        LayoutEngine.ResolveEdges(box, root.ContentWidth);
        box.Container = root;
        box.InlineRoot = root;
    }

    static string Transform(string text, Text.TextTransform transform)
    {
        switch (transform)
        {
            case Text.TextTransform.Uppercase:
                return text.ToUpperInvariant();
            case Text.TextTransform.Lowercase:
                return text.ToLowerInvariant();
            case Text.TextTransform.Capitalize:
                var chars = text.ToCharArray();
                var startOfWord = true;
                for (var i = 0; i < chars.Length; i++)
                {
                    if (char.IsLetter(chars[i]))
                    {
                        if (startOfWord)
                            chars[i] = char.ToUpperInvariant(chars[i]);
                        startOfWord = false;
                    }
                    else if (char.IsWhiteSpace(chars[i]) || chars[i] is '-' or '(' or '"')
                        startOfWord = true;
                    else if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '\'')
                        startOfWord = true;
                    else
                        startOfWord = false;
                }
                return new string(chars);
            default:
                return text;
        }
    }

    /// <summary>White-space processing (CSS Text 3 §4.1) for one text box, then shaping into cluster pieces.</summary>
    void AddText(Paragraph paragraph, LayoutBox textBox, LayoutBox parent, ref bool previousSpace)
    {
        var style = textBox.Style;
        var whiteSpace = style.WhiteSpace;
        var collapse = whiteSpace is Text.WhiteSpace.Normal or Text.WhiteSpace.NoWrap or Text.WhiteSpace.PreLine;
        var keepNewlines = whiteSpace is Text.WhiteSpace.Pre or Text.WhiteSpace.PreWrap or Text.WhiteSpace.PreLine;
        var source = Transform(textBox.Text, style.TextTransform);
        var processed = new StringBuilder(source.Length);
        // Where each processed character comes from (a transform that changes the length maps proportionally).
        var origins = new List<int>(source.Length + 1);
        var scale = textBox.Text.Length == source.Length || source.Length == 0 ? 1.0 : (double)textBox.Text.Length / source.Length;
        var segmentStart = 0;
        void Flush(bool beforeBreak)
        {
            if (beforeBreak && collapse)
                while (processed.Length > segmentStart && processed[^1] == ' ')
                {
                    processed.Length--;
                    origins.RemoveAt(origins.Count - 1);
                }
            if (processed.Length > segmentStart)
                AddClusters(paragraph, textBox, parent, processed.ToString(segmentStart, processed.Length - segmentStart), segmentStart);
            segmentStart = processed.Length;
        }
        for (var index = 0; index < source.Length; index++)
        {
            var c = source[index];
            var origin = (int)(index * scale);
            if (c == '\r')
                continue;
            if (c == '\n' && keepNewlines)
            {
                Flush(beforeBreak: true);
                paragraph.Pieces.Add(new Piece { Kind = PieceKind.Break, Box = textBox, Parent = parent, TextStart = processed.Length });
                previousSpace = true;
                continue;
            }
            if (collapse && c is ' ' or '\t' or '\n' or '\f')
            {
                if (previousSpace)
                    continue;
                processed.Append(' ');
                origins.Add(origin);
                previousSpace = true;
                continue;
            }
            if (c == '\t')
            {
                // Tab stops every 8 spaces from the start of the text (an approximation of tab-size).
                var spaces = 8 - (processed.Length - segmentStart) % 8;
                processed.Append(' ', spaces);
                for (var k = 0; k < spaces; k++)
                    origins.Add(origin);
                previousSpace = false;
                continue;
            }
            processed.Append(c);
            origins.Add(origin);
            previousSpace = c == ' ' && collapse;
        }
        Flush(beforeBreak: false);
        origins.Add(textBox.Text.Length);
        paragraph.ProcessedText[textBox] = processed.ToString();
        TextOf.AddOrUpdate(textBox, processed.ToString());
        SourceOffsetsOf.AddOrUpdate(textBox, origins.ToArray());
    }

    void AddClusters(Paragraph paragraph, LayoutBox textBox, LayoutBox parent, string text, int offset)
    {
        var style = textBox.Style;
        var preferred = FontFor(style);
        var collapsible = style.WhiteSpace is Text.WhiteSpace.Normal or Text.WhiteSpace.NoWrap or Text.WhiteSpace.PreLine;
        var wraps = Wraps(style);
        var anywhere = style.OverflowWrap == Text.OverflowWrap.Anywhere;
        // Runs of characters one font can show.
        var runStart = 0;
        ResolvedFont? runFont = null;
        for (var i = 0; i <= text.Length; i++)
        {
            ResolvedFont? font = null;
            if (i < text.Length)
            {
                if (char.IsLowSurrogate(text[i]))
                    continue;
                var codepoint = char.IsHighSurrogate(text[i]) && i + 1 < text.Length ? char.ConvertToUtf32(text[i], text[i + 1]) : text[i];
                font = text[i] == ' ' ? runFont ?? preferred : Fonts.ResolveFor(codepoint, preferred) ?? preferred;
            }
            if (i == text.Length || (runFont is { } current && font is { } next && next.Face != current.Face))
            {
                if (i > runStart)
                {
                    var runText = text[runStart..i];
                    var shaped = Shape(runText, runFont ?? preferred, style);
                    foreach (var cluster in shaped.Clusters)
                    {
                        var ch = runText[cluster.Start];
                        paragraph.Pieces.Add(new Piece
                        {
                            Kind = PieceKind.Cluster,
                            Box = textBox,
                            Parent = parent,
                            Shaped = shaped,
                            Cluster = cluster,
                            TextStart = offset + runStart + cluster.Start,
                            TextEnd = offset + runStart + cluster.End,
                            Width = cluster.Width,
                            Char = ch,
                            Space = ch == ' ',
                            Collapsible = ch == ' ' && collapsible,
                            Wraps = wraps,
                            Anywhere = anywhere,
                        });
                    }
                }
                runStart = i;
            }
            if (font is not null)
                runFont = font;
        }
    }

    /// <summary>Soft wrap opportunities (UAX #14 for the scripts the game uses).</summary>
    static void MarkBreaks(List<Piece> pieces)
    {
        Piece? Next(int i, bool skipEdges)
        {
            for (var j = i + 1; j < pieces.Count; j++)
            {
                var p = pieces[j];
                if (skipEdges && p.Kind is PieceKind.Open or PieceKind.Close or PieceKind.Placeholder)
                    continue;
                return p;
            }
            return null;
        }
        Piece? Previous(int i)
        {
            for (var j = i - 1; j >= 0; j--)
                if (pieces[j].Kind is PieceKind.Cluster or PieceKind.Atomic)
                    return pieces[j];
            return null;
        }
        for (var i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            var next = Next(i, skipEdges: true);
            switch (piece.Kind)
            {
                case PieceKind.Cluster:
                    if (!piece.Wraps)
                        break;
                    if (piece.Space)
                        piece.BreakAfter = next is null || !next.Space;
                    else if (piece.Char is '-' or '‐' or '–' or '—' or '​' or '/' && piece.Char is not '/')
                    {
                        var previous = Previous(i);
                        piece.BreakAfter = piece.Char == '​' || piece.Char == '—'
                            || (previous is { Kind: PieceKind.Cluster } && char.IsLetterOrDigit(previous.Char) && next is { Kind: PieceKind.Cluster } && char.IsLetterOrDigit(next.Char));
                    }
                    else if (next is { Kind: PieceKind.Atomic, Wraps: true } && !char.IsPunctuation(piece.Char))
                        piece.BreakAfter = true;
                    break;
                case PieceKind.Atomic:
                    if (piece.Wraps && next is not null && !(next.Kind == PieceKind.Cluster && (next.Space || char.IsPunctuation(next.Char))))
                        piece.BreakAfter = true;
                    break;
            }
        }
    }

    // ---------- Atomic inlines ----------

    void LayoutAtomics(Paragraph paragraph, LayoutBox root, float availableWidth, float percentHeight)
    {
        foreach (var piece in paragraph.Pieces)
        {
            if (piece.Kind != PieceKind.Atomic)
                continue;
            var box = piece.Box;
            var space = Space.For(availableWidth, percentHeight);
            space.ShrinkToFit = true;
            box.Container = root;
            box.InlineRoot = root;
            _engine.LayoutAtomic(box, space);
            piece.Width = box.MarginBoxWidth;
        }
    }

    // ---------- Line breaking ----------

    readonly record struct LineRange(int Start, int End, bool Forced);

    static List<LineRange> BreakLines(List<Piece> pieces, float available)
    {
        var lines = new List<LineRange>();
        var start = 0;
        while (start < pieces.Count)
        {
            // Collapsible spaces at the start of a line are removed.
            var first = start;
            while (first < pieces.Count && (pieces[first].Collapsible || pieces[first].Kind is PieceKind.Open or PieceKind.Close or PieceKind.Placeholder))
            {
                if (pieces[first].Collapsible)
                    pieces[first].Removed = true;
                first++;
            }
            var width = 0f;
            var lastBreak = -1;
            var end = -1;
            var forced = false;
            var hasContent = false;
            for (var i = start; i < pieces.Count; i++)
            {
                var piece = pieces[i];
                if (piece.Kind == PieceKind.Break)
                {
                    end = i + 1;
                    forced = true;
                    break;
                }
                if (piece.Removed)
                    continue;
                width += piece.Width;
                var content = piece.Kind is PieceKind.Atomic || (piece.Kind == PieceKind.Cluster && !piece.Space);
                if (content && hasContent && width > available + 0.01f)
                {
                    if (lastBreak >= start)
                        end = lastBreak + 1;
                    else if (piece.Anywhere && piece.Kind == PieceKind.Cluster)
                        end = i;
                    else
                    {
                        // No opportunity on the line: the content overflows up to the next one.
                        end = pieces.Count;
                        for (var j = i; j < pieces.Count; j++)
                        {
                            if (pieces[j].Kind == PieceKind.Break)
                            {
                                end = j;
                                break;
                            }
                            if (pieces[j].BreakAfter)
                            {
                                end = j + 1;
                                break;
                            }
                        }
                    }
                    break;
                }
                if (content)
                    hasContent = true;
                if (piece.BreakAfter)
                    lastBreak = i;
            }
            if (end < 0)
                end = pieces.Count;
            if (!forced)
            {
                // The line keeps the ends of inline boxes that close right after the break.
                while (end < pieces.Count && pieces[end].Kind is PieceKind.Close or PieceKind.Placeholder)
                    end++;
                // A forced break right after the content also ends this line.
                if (end < pieces.Count && pieces[end].Kind == PieceKind.Break)
                {
                    end++;
                    forced = true;
                }
            }
            if (end <= start)
                end = start + 1;
            // Collapsible spaces at the end of the line are removed.
            for (var i = end - 1; i >= start; i--)
            {
                var piece = pieces[i];
                if (piece.Kind is PieceKind.Open or PieceKind.Close or PieceKind.Placeholder or PieceKind.Break)
                    continue;
                if (piece.Collapsible)
                {
                    piece.Removed = true;
                    continue;
                }
                break;
            }
            lines.Add(new LineRange(start, end, forced));
            start = end;
        }
        return lines;
    }

    // ---------- Layout ----------

    sealed class BoxState
    {
        public LayoutBox Box = null!;
        public BoxState? Parent;
        public Metrics Metrics;
        /// <summary>The box's baseline relative to the root's baseline (positive = lower).</summary>
        public float Shift;
        public bool ShiftDone;
        /// <summary>vertical-align: top (true) or bottom (false).</summary>
        public bool? AlignEdge;
        public bool HasContent;
        /// <summary>The tallest ascent of fallback fonts used in the box (line-height: normal).</summary>
        public float FallbackAscent;
        public float FragmentStart, FragmentEnd = float.NaN;
        public bool StartsHere, EndsHere;
        public LineItem? Item;
    }

    readonly record struct Placed(LineItem Item, BoxState Parent, float Ascent);

    /// <summary>Lays out the block's inline content in lines of the given width (its content width).</summary>
    public InlineContent Layout(LayoutBox root, float availableWidth, float percentHeight)
    {
        var paragraph = Collect(root, intrinsic: false);
        LayoutAtomics(paragraph, root, availableWidth, percentHeight);
        var pieces = paragraph.Pieces;
        var content = new InlineContent();
        var s = root.Style;
        foreach (var piece in pieces)
            if (piece.Kind == PieceKind.Open)
                piece.Box.Fragments = [];

        var ranges = BreakLines(pieces, availableWidth);
        // -webkit-line-clamp: lines after the clamp are laid out but hidden, and don't count for the height.
        var clamp = s.Display == Display.WebkitBox && s.LineClamp > 0 ? s.LineClamp : int.MaxValue;
        var clamped = ranges.Count > clamp;
        var visibleBottom = float.NaN;
        var ellipsisOverflow = s.TextOverflow == TextOverflow.Ellipsis && s.OverflowX != Overflow.Visible;
        var rootMetrics = MetricsFor(s);
        var top = root.ContentTop;
        var open = new List<LayoutBox>();

        for (var l = 0; l < ranges.Count; l++)
        {
            var range = ranges[l];
            var line = new LineBox { Top = top };

            // What's visible: everything that fits before an ellipsis.
            var fullWidth = LineWidth(pieces, range);
            GlyphRun? ellipsis = null;
            var limit = range.End;
            if ((clamped && l == clamp - 1) || (ellipsisOverflow && fullWidth > availableWidth + 0.01f))
            {
                ellipsis = EllipsisRun(s);
                limit = EllipsisCut(pieces, range, availableWidth - ellipsis.Width);
            }
            bool Visible(int i) => i < limit || pieces[i].Kind is PieceKind.Close or PieceKind.Placeholder;

            // The line's width, alignment and justification.
            // Spaces right before an ellipsis stay (Chromium keeps them); otherwise end-of-line spaces hang.
            var trailing = ellipsis is not null ? limit : TrailingStart(pieces, range.Start, Math.Min(limit, range.End));
            var lineWidth = SnappedWidth(pieces, range.Start, range.End, i => Visible(i) && !pieces[i].Removed && (i < trailing || pieces[i].Kind != PieceKind.Cluster));
            if (ellipsis is not null)
                lineWidth += ellipsis.Width;
            var free = availableWidth - lineWidth;
            var lastLine = l == ranges.Count - 1 || range.Forced || (clamped && l == clamp - 1);
            // Lines too long for the box are start-aligned and overflow at the end (CSS Text 3 §6.1).
            var offset = 0f;
            if (!float.IsInfinity(availableWidth) && free > 0)
                offset = s.TextAlign switch
                {
                    TextAlignment.Center => free / 2,
                    TextAlignment.Right or TextAlignment.End => free,
                    _ => 0f,
                };
            var justifyExtra = 0f;
            if (s.TextAlign == TextAlignment.Justify && !lastLine && free > 0 && ellipsis is null)
            {
                var spaces = 0;
                for (var i = range.Start; i < trailing; i++)
                    if (pieces[i].Kind == PieceKind.Cluster && pieces[i].Space && !pieces[i].Removed)
                        spaces++;
                if (spaces > 0)
                    justifyExtra = free / spaces;
            }
            line.Left = root.ContentLeft + offset;
            line.Width = lineWidth;

            // Boxes, text and atomics along the line.
            var states = new Dictionary<LayoutBox, BoxState>();
            var rootState = new BoxState { Box = root, Metrics = rootMetrics, ShiftDone = true };
            states[root] = rootState;
            BoxState StateOf(LayoutBox box)
            {
                if (box == root)
                    return rootState;
                if (states.TryGetValue(box, out var state))
                    return state;
                var parentState = box.Parent is { } parent ? StateOf(parent) : rootState;
                state = new BoxState { Box = box, Parent = parentState, Metrics = MetricsFor(box.Style) };
                states[box] = state;
                return state;
            }
            var items = new List<LineItem>();
            var placed = new List<Placed>();
            var x = line.Left;
            foreach (var box in open)
            {
                var state = StateOf(box);
                state.FragmentStart = x;
                state.Item = new LineItem { Kind = LineItemKind.InlineBox, Box = box };
                items.Add(state.Item);
            }
            LineItem? text = null;
            ShapedText? textShape = null;
            // A text item's advance is its width rounded up to a LayoutUnit (Blink's snapped width).
            void CloseText()
            {
                if (text is not null && !text.Hanging)
                {
                    x = text.X + LayoutUnit.Ceil(x - text.X);
                    text.Width = x - text.X;
                    text.Run!.Width = text.Width;
                }
                text = null;
            }
            var placeholders = new List<(LayoutBox Box, float X)>();
            var breaks = new List<(LayoutBox Box, float X, BoxState State)>();
            for (var i = range.Start; i < range.End; i++)
            {
                if (!Visible(i))
                    continue;
                var piece = pieces[i];
                switch (piece.Kind)
                {
                    case PieceKind.Open:
                    {
                        CloseText();
                        var box = piece.Box;
                        var state = StateOf(box);
                        x += box.Margin.Left;
                        state.FragmentStart = x;
                        state.StartsHere = true;
                        x += box.Border.Left + box.Padding.Left;
                        state.Item = new LineItem { Kind = LineItemKind.InlineBox, Box = box };
                        items.Add(state.Item);
                        open.Add(box);
                        if (box.Border.Horizontal + box.Padding.Horizontal + box.Margin.Horizontal != 0)
                            MarkContent(state);
                        break;
                    }
                    case PieceKind.Close:
                    {
                        CloseText();
                        var box = piece.Box;
                        var state = StateOf(box);
                        open.Remove(box);
                        x += box.Padding.Right + box.Border.Right;
                        state.FragmentEnd = x;
                        state.EndsHere = true;
                        x += box.Margin.Right;
                        if (box.Border.Horizontal + box.Padding.Horizontal + box.Margin.Horizontal != 0)
                            MarkContent(state);
                        break;
                    }
                    case PieceKind.Cluster:
                    {
                        if (piece.Removed || (i >= trailing && piece.Space && piece.Collapsible))
                            break;
                        // Preserved spaces at the end of a line hang past it, in a fragment of their own.
                        var hanging = i >= trailing && piece.Space && !piece.Collapsible;
                        var state = StateOf(piece.Parent);
                        MarkContent(state);
                        var shaped = piece.Shaped!;
                        if (text is null || text.Box != piece.Box || textShape != shaped || text.Hanging != hanging)
                        {
                            CloseText();
                            text = new LineItem
                            {
                                Kind = LineItemKind.Text,
                                Box = piece.Box,
                                Run = new GlyphRun { Font = shaped.Font, Style = TextStyleFor(piece.Style) },
                                X = x,
                                TextStart = piece.TextStart,
                                TextEnd = piece.TextEnd,
                                // A fragment's box uses the primary font's metrics, whatever font draws the glyphs.
                                Ascent = PrimaryMetrics(piece.Style).Ascent,
                                Descent = PrimaryMetrics(piece.Style).Descent,
                                Hanging = hanging,
                            };
                            text.Run.X = x;
                            textShape = shaped;
                            items.Add(text);
                            placed.Add(new Placed(text, state, 0));
                            // Fallback fonts with taller ascents enlarge a line-height: normal box.
                            if (shaped.Font.Face != FontFor(piece.Style).Face && piece.Style.LineHeight.Kind == LineHeightKind.Normal)
                                state.FallbackAscent = MathF.Max(state.FallbackAscent, shaped.Ascent);
                        }
                        var cluster = piece.Cluster!;
                        var origin = shaped.Glyphs[cluster.GlyphStart].X;
                        var glyphStart = text.Run!.Glyphs.Count;
                        for (var g = 0; g < cluster.GlyphCount; g++)
                        {
                            var glyph = shaped.Glyphs[cluster.GlyphStart + g];
                            text.Run!.Glyphs.Add(glyph with { X = x - text.X + (glyph.X - origin) });
                        }
                        var clusterX = x;
                        x += piece.Width;
                        if (piece.Space && justifyExtra > 0)
                            x += justifyExtra;
                        (text.Clusters ??= []).Add(new ItemCluster(piece.TextStart, piece.TextEnd, clusterX, x - clusterX, glyphStart, cluster.GlyphCount));
                        text.Width = x - text.X;
                        text.Run!.Width = text.Width;
                        text.TextEnd = piece.TextEnd;
                        break;
                    }
                    case PieceKind.Atomic:
                    {
                        CloseText();
                        var box = piece.Box;
                        var state = StateOf(piece.Parent);
                        MarkContent(state);
                        box.X = x + box.Margin.Left;
                        var item = new LineItem { Kind = LineItemKind.Atomic, Box = box, X = x, Width = piece.Width };
                        items.Add(item);
                        placed.Add(new Placed(item, state, AtomicMetrics(box).Ascent));
                        x += piece.Width;
                        break;
                    }
                    case PieceKind.Break:
                    {
                        CloseText();
                        var state = StateOf(piece.Parent);
                        MarkContent(state);
                        if (piece.Box.Type == BoxType.LineBreak)
                            breaks.Add((piece.Box, x, state));
                        else
                        {
                            // A preserved newline: an empty fragment of its text at the end of the line.
                            var font = FontFor(piece.Style);
                            var (ascent, descent, _) = TextLayout.Metrics(font.Face, piece.Style.FontSize, piece.Style.SubpixelPositioning);
                            var newline = new LineItem
                            {
                                Kind = LineItemKind.Text,
                                Box = piece.Box,
                                Run = new GlyphRun { Font = font, Style = TextStyleFor(piece.Style), X = x },
                                X = x,
                                TextStart = piece.TextStart,
                                TextEnd = piece.TextStart,
                                Ascent = ascent,
                                Descent = descent,
                            };
                            items.Add(newline);
                            placed.Add(new Placed(newline, state, 0));
                        }
                        break;
                    }
                    case PieceKind.Placeholder:
                        placeholders.Add((piece.Box, x));
                        break;
                }
            }
            CloseText();
            if (ellipsis is not null)
            {
                ellipsis.X = x;
                var item = new LineItem { Kind = LineItemKind.Generated, Owner = root, Run = ellipsis, X = x, Width = ellipsis.Width, Ascent = rootMetrics.Ascent, Descent = rootMetrics.Descent };
                items.Add(item);
                placed.Add(new Placed(item, rootState, 0));
                MarkContent(rootState);
                x += ellipsis.Width;
            }
            foreach (var box in open)
                StateOf(box).FragmentEnd = x;

            // Vertical alignment: the line's extent around the root's baseline.
            var hasContent = rootState.HasContent || states.Values.Any(st => st.HasContent);
            float minTop = -rootMetrics.Above, maxBottom = rootMetrics.Below;
            if (rootState.FallbackAscent > rootMetrics.Ascent)
                minTop = MathF.Min(minTop, -(rootState.FallbackAscent + rootMetrics.Above - rootMetrics.Ascent));
            var edgeAligned = new List<(float Height, bool Top)>();
            foreach (var state in states.Values)
            {
                if (state == rootState || !state.HasContent)
                    continue;
                ComputeShift(state);
                if (state.AlignEdge is { } isTop)
                {
                    edgeAligned.Add((state.Metrics.LineHeight, isTop));
                    continue;
                }
                var above = MathF.Max(state.Metrics.Above, state.FallbackAscent + state.Metrics.Above - state.Metrics.Ascent);
                minTop = MathF.Min(minTop, state.Shift - above);
                maxBottom = MathF.Max(maxBottom, state.Shift + state.Metrics.Below);
            }
            var atomicShift = new Dictionary<LineItem, float>();
            foreach (var (item, parent, ascent) in placed)
            {
                if (item.Kind != LineItemKind.Atomic)
                    continue;
                ComputeShift(parent);
                var box = item.Box!;
                var descent = box.MarginBoxHeight - ascent;
                var align = box.Style.VerticalAlign;
                var shift = align switch
                {
                    VerticalAlignKind.Middle => parent.Shift - parent.Metrics.XHeight / 2 - (descent - ascent) / 2,
                    VerticalAlignKind.TextTop => parent.Shift - parent.Metrics.Ascent + ascent,
                    VerticalAlignKind.TextBottom => parent.Shift + parent.Metrics.Descent - descent,
                    VerticalAlignKind.Sub => parent.Shift + SubShift(parent.Box.Style.FontSize),
                    VerticalAlignKind.Super => parent.Shift - SuperShift(parent.Box.Style.FontSize),
                    VerticalAlignKind.Length => parent.Shift - LayoutUnit.Truncate(box.Style.VerticalAlignLength.ResolveOr(MetricsFor(box.Style).LineHeight, 0)),
                    _ => parent.Shift,
                };
                if (align is VerticalAlignKind.Top or VerticalAlignKind.Bottom)
                {
                    edgeAligned.Add((box.MarginBoxHeight, align == VerticalAlignKind.Top));
                    continue;
                }
                atomicShift[item] = shift;
                minTop = MathF.Min(minTop, shift - ascent);
                maxBottom = MathF.Max(maxBottom, shift + descent);
            }
            var lineHeight = maxBottom - minTop;
            foreach (var (height, isTop) in edgeAligned)
            {
                if (height <= lineHeight)
                    continue;
                if (isTop)
                    maxBottom = minTop + height;
                else
                    minTop = maxBottom - height;
                lineHeight = height;
            }
            if (!hasContent)
            {
                // A line with nothing but empty inline boxes and placeholders takes no room.
                lineHeight = 0;
                minTop = 0;
            }
            line.Height = lineHeight;
            line.Baseline = top - minTop;

            // Final positions.
            foreach (var state in states.Values)
            {
                if (state == rootState || state.AlignEdge is not { } isTop)
                    continue;
                state.Shift = (isTop ? top + state.Metrics.Above : top + lineHeight - state.Metrics.Below) - line.Baseline;
            }
            foreach (var (item, parent, ascent) in placed)
            {
                switch (item.Kind)
                {
                    case LineItemKind.Text:
                    case LineItemKind.Generated:
                        item.Baseline = line.Baseline + parent.Shift;
                        break;
                    case LineItemKind.Atomic:
                    {
                        var box = item.Box!;
                        float y;
                        if (atomicShift.TryGetValue(item, out var shift))
                            y = line.Baseline + shift - ascent;
                        else
                            y = box.Style.VerticalAlign == VerticalAlignKind.Top ? top : top + lineHeight - box.MarginBoxHeight;
                        box.Y = y + box.Margin.Top;
                        item.Baseline = y + ascent;
                        box.RelativeOffset = LayoutEngine.RelativeOffset(box.Style, availableWidth, percentHeight);
                        break;
                    }
                }
            }
            foreach (var state in states.Values)
            {
                if (state == rootState || state.Item is null)
                    continue;
                var box = state.Box;
                var baseline = line.Baseline + state.Shift;
                var fragmentEnd = MathF.Max(state.FragmentStart, float.IsNaN(state.FragmentEnd) ? x : state.FragmentEnd);
                // On a line with no content the box has no height either.
                var rect = hasContent
                    ? RectF.FromEdges(state.FragmentStart, baseline - state.Metrics.Ascent - box.Padding.Top - box.Border.Top,
                        fragmentEnd, baseline + state.Metrics.Descent + box.Padding.Bottom + box.Border.Bottom)
                    : new RectF(state.FragmentStart, top, fragmentEnd - state.FragmentStart, 0);
                var fragment = new InlineFragment(rect, state.StartsHere, state.EndsHere, content.Lines.Count);
                state.Item.Fragment = fragment;
                (box.Fragments ??= []).Add(fragment);
            }
            foreach (var (box, bx, state) in breaks)
            {
                var m = MetricsFor(box.Style);
                var baseline = line.Baseline + state.Shift;
                box.Container = root;
                box.InlineRoot = root;
                box.Fragments = [new InlineFragment(new RectF(bx, baseline - m.Ascent, 0, m.Ascent + m.Descent), true, true, content.Lines.Count)];
            }
            foreach (var (box, px) in placeholders)
            {
                box.StaticContainer = root;
                box.StaticPosition = new Vector2(px, top);
            }
            if (!hasContent)
                continue;
            line.Hidden = clamped && l >= clamp;
            line.Items.AddRange(items);
            content.Lines.Add(line);
            content.Right = MathF.Max(content.Right, line.Left + MathF.Max(lineWidth, fullWidth));
            top += lineHeight;
            if (clamped && l == clamp - 1)
                visibleBottom = top;
        }
        content.OverflowHeight = top - root.ContentTop;
        content.Height = (float.IsNaN(visibleBottom) ? top : visibleBottom) - root.ContentTop;
        foreach (var piece in pieces)
            if (piece.Kind == PieceKind.Open)
                piece.Box.RelativeOffset = LayoutEngine.RelativeOffset(piece.Box.Style, availableWidth, percentHeight);
        return content;
    }

    /// <summary>Blink's sub/super baseline shifts: a fifth / third of the parent's font size in LayoutUnits (1/64 px, truncated), plus one.</summary>
    static float SubShift(float parentFontSize) => MathF.Floor(parentFontSize / 5 * 64) / 64 + 1;
    static float SuperShift(float parentFontSize) => MathF.Floor(parentFontSize / 3 * 64) / 64 + 1;

    static void MarkContent(BoxState state)
    {
        for (var s = state; s is not null && !s.HasContent; s = s.Parent)
            s.HasContent = true;
    }

    static void ComputeShift(BoxState state)
    {
        if (state.ShiftDone || state.Parent is null)
            return;
        var parent = state.Parent;
        ComputeShift(parent);
        var m = state.Metrics;
        var subShift = SubShift(parent.Box.Style.FontSize);
        var superShift = SuperShift(parent.Box.Style.FontSize);
        var p = parent.Metrics;
        var style = state.Box.Style;
        state.ShiftDone = true;
        state.Shift = style.VerticalAlign switch
        {
            VerticalAlignKind.Sub => parent.Shift + subShift,
            VerticalAlignKind.Super => parent.Shift - superShift,
            VerticalAlignKind.TextTop => parent.Shift - p.Ascent + m.Above,
            VerticalAlignKind.TextBottom => parent.Shift + p.Descent - m.Below,
            VerticalAlignKind.Middle => parent.Shift - p.XHeight / 2 - (m.Below - m.Above) / 2,
            VerticalAlignKind.Length => parent.Shift - LayoutUnit.Truncate(style.VerticalAlignLength.ResolveOr(m.LineHeight, 0)),
            _ => parent.Shift,
        };
        if (style.VerticalAlign is VerticalAlignKind.Top or VerticalAlignKind.Bottom)
            state.AlignEdge = style.VerticalAlign == VerticalAlignKind.Top;
    }

    /// <summary>An inline-block's baseline is its last line's, unless it clips its overflow or has no lines (then its bottom margin edge); replaced elements sit on their bottom margin edge.</summary>
    static (float Ascent, float Descent) AtomicMetrics(LayoutBox box)
    {
        var height = box.MarginBoxHeight;
        float? baseline = box.Type switch
        {
            BoxType.Replaced => box.LastBaseline,
            BoxType.Flex or BoxType.Grid => box.FirstBaseline,
            _ => box.Style.IsScrollContainer ? null : box.LastBaseline,
        };
        var ascent = baseline is { } b ? box.Margin.Top + b : height;
        return (ascent, height - ascent);
    }

    /// <summary>
    /// The width of the included pieces, with each text item (consecutive clusters of one text box and
    /// shaping run) rounded up to a LayoutUnit as Blink measures it.
    /// </summary>
    static float SnappedWidth(List<Piece> pieces, int start, int end, Func<int, bool> include)
    {
        float total = 0, group = 0;
        LayoutBox? groupBox = null;
        ShapedText? groupShape = null;
        for (var i = start; i < end; i++)
        {
            if (!include(i))
                continue;
            var piece = pieces[i];
            if (piece.Kind == PieceKind.Cluster)
            {
                if (groupBox != piece.Box || groupShape != piece.Shaped)
                {
                    total += LayoutUnit.Ceil(group);
                    group = 0;
                    groupBox = piece.Box;
                    groupShape = piece.Shaped;
                }
                group += piece.Width;
                continue;
            }
            total += LayoutUnit.Ceil(group) + piece.Width;
            group = 0;
            groupBox = null;
            groupShape = null;
        }
        return total + LayoutUnit.Ceil(group);
    }

    static float LineWidth(List<Piece> pieces, LineRange range)
    {
        var width = 0f;
        var trailing = TrailingStart(pieces, range.Start, range.End);
        for (var i = range.Start; i < range.End; i++)
            if (!pieces[i].Removed && (i < trailing || pieces[i].Kind != PieceKind.Cluster))
                width += pieces[i].Width;
        return width;
    }

    /// <summary>Where the spaces that hang at the end of a line begin (they don't count for its width).</summary>
    static int TrailingStart(List<Piece> pieces, int start, int end)
    {
        var trailing = end;
        for (var i = end - 1; i >= start; i--)
        {
            var piece = pieces[i];
            if (piece.Kind is PieceKind.Close or PieceKind.Open or PieceKind.Placeholder or PieceKind.Break || piece.Removed)
                continue;
            if (piece.Kind == PieceKind.Cluster && piece.Space)
            {
                trailing = i;
                continue;
            }
            break;
        }
        return trailing;
    }

    /// <summary>The first piece that no longer fits before an ellipsis at <paramref name="available"/>.</summary>
    static int EllipsisCut(List<Piece> pieces, LineRange range, float available)
    {
        var width = 0f;
        for (var i = range.Start; i < range.End; i++)
        {
            var piece = pieces[i];
            if (piece.Removed || piece.Kind is PieceKind.Close or PieceKind.Placeholder or PieceKind.Break)
                continue;
            if (width + piece.Width > available + 0.01f && piece.Kind is PieceKind.Cluster or PieceKind.Atomic)
                return i;
            width += piece.Width;
        }
        return range.End;
    }

    GlyphRun EllipsisRun(ComputedStyle style)
    {
        var font = FontFor(style);
        var shaped = Shape("\u2026", Fonts.ResolveFor(0x2026, font) ?? font, style);
        var run = new GlyphRun { Font = shaped.Font, Style = TextStyleFor(style) };
        run.Glyphs.AddRange(shaped.Glyphs);
        run.Width = shaped.Clusters.Sum(c => c.Width);
        return run;
    }

    // ---------- Markers ----------

    /// <summary>Lays out an outside list marker on its own (it is placed by the list item).</summary>
    public (InlineContent Content, float Width, float Height, float Baseline) LayoutMarker(LayoutBox marker)
    {
        LayoutEngine.ResolveEdges(marker, 0);
        var content = Layout(marker, float.PositiveInfinity, float.NaN);
        var width = content.Lines.Count > 0 ? content.Lines.Max(l => l.Width) : 0;
        var baseline = content.Lines.Count > 0 ? content.Lines[0].Baseline : 0;
        return (content, width, content.Height, baseline);
    }

    // ---------- Intrinsic widths ----------

    /// <summary>The min-content or max-content width of the block's inline content (CSS Sizing 3 §5.1).</summary>
    public float IntrinsicWidth(LayoutBox root, bool min)
    {
        var paragraph = Collect(root, intrinsic: true);
        var pieces = paragraph.Pieces;
        foreach (var piece in pieces)
        {
            if (piece.Kind == PieceKind.Atomic)
                piece.Width = _engine.Contribution(piece.Box, min);
            else if (piece.Kind is PieceKind.Open or PieceKind.Close)
            {
                // Percentage padding and margins count as zero here.
                var b = piece.Box;
                var st = b.Style;
                piece.Width = piece.Kind == PieceKind.Open
                    ? Fixed(st.MarginLeft) + st.BorderLeft + Fixed(st.PaddingLeft)
                    : Fixed(st.MarginRight) + st.BorderRight + Fixed(st.PaddingRight);
            }
        }
        var best = 0f;
        var width = 0f;
        var pendingSpaces = 0f;
        var atLineStart = true;
        // Text items round up to LayoutUnits: the open item's raw width is kept apart from the
        // closed width so that rounding it adds no float error (a 1/64 px short sum wraps the line).
        var itemWidth = 0f;
        LayoutBox? itemBox = null;
        ShapedText? itemShape = null;
        void CloseItem()
        {
            width += LayoutUnit.Ceil(itemWidth);
            itemWidth = 0;
            itemBox = null;
            itemShape = null;
        }
        foreach (var piece in pieces)
        {
            if (piece.Kind != PieceKind.Cluster || piece.Box != itemBox || piece.Shaped != itemShape)
                CloseItem();
            if (piece.Kind == PieceKind.Cluster)
            {
                itemBox = piece.Box;
                itemShape = piece.Shaped;
            }
            if (piece.Kind == PieceKind.Break)
            {
                CloseItem();
                best = MathF.Max(best, width);
                width = 0;
                pendingSpaces = 0;
                atLineStart = true;
                continue;
            }
            if (piece.Kind == PieceKind.Placeholder)
                continue;
            if (piece.Kind == PieceKind.Cluster && piece.Space)
            {
                if (atLineStart && piece.Collapsible)
                    continue;
                pendingSpaces += piece.Width;
            }
            else
            {
                if (piece.Kind == PieceKind.Cluster)
                    itemWidth += pendingSpaces + piece.Width;
                else
                    width += pendingSpaces + piece.Width;
                pendingSpaces = 0;
                if (piece.Kind is PieceKind.Cluster or PieceKind.Atomic)
                    atLineStart = false;
            }
            if (min && (piece.BreakAfter || (piece.Anywhere && piece.Kind == PieceKind.Cluster)))
            {
                CloseItem();
                best = MathF.Max(best, width);
                width = 0;
                pendingSpaces = 0;
                atLineStart = true;
            }
        }
        CloseItem();
        return MathF.Max(best, width);
    }

    static float Fixed(Length length) => length.IsValue && !length.HasPercent ? LayoutUnit.Truncate(length.Pixels) : 0;
}
