using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Statventure.Text.Autohint;

/// <summary>The kind of an outline point and the autohinter's markers on it (skrifa's glyf PointFlags).</summary>
[Flags]
internal enum PointFlags : byte
{
    None = 0,
    OnCurve = 1,
    OffCurveCubic = 2,
    Near = 4,
    WeakInterpolation = 8,
    TouchedX = 16,
    TouchedY = 32,
}

/// <summary>A point of an outline in font units, as skrifa's draw_unscaled gives it.</summary>
internal readonly record struct UnscaledPoint(int X, int Y, PointFlags Flags, bool IsContourStart)
{
    public bool IsOnCurve => (Flags & PointFlags.OnCurve) != 0;
}

/// <summary>
/// What the autohinter reads from a font (skrifa's FontRef as the autohinter uses it): units per
/// em, the glyph count, the Unicode character map, GSUB, whether the font is fixed pitch or
/// italic, advances, and outlines in font units (FreeType's unscaled TrueType loading, which
/// skrifa's FreeType-style glyf scaler matches when given no size). Also holds the per-font state
/// skrifa keeps in its hinting instance: the glyph styles and the unscaled style metrics.
/// </summary>
internal sealed unsafe class AutohintFont
{
    static readonly ConditionalWeakTable<FontFace, AutohintFont> Fonts = new();

    public static AutohintFont For(FontFace face) => Fonts.GetValue(face, f => new AutohintFont(f));

    readonly FontFace _face;
    public readonly int UnitsPerEm;
    public readonly int GlyphCount;
    /// <summary>The character map isn't Unicode (a symbol font): no metrics are computed.</summary>
    public readonly bool IsSymbol;
    public readonly bool IsFixedPitch;
    /// <summary>skrifa's attributes().style isn't normal: OS/2 fsSelection italic or oblique (head macStyle italic without OS/2).</summary>
    public readonly bool IsItalic;
    public readonly byte[]? Gsub;
    /// <summary>The character map's mappings in character order.</summary>
    public readonly (int Char, uint Glyph)[] Mappings;
    readonly Dictionary<int, uint> _map;

    GlyphStyleMap? _styles;
    UnscaledStyleMetrics?[]? _metrics;

    AutohintFont(FontFace face)
    {
        _face = face;
        var ft = face.FtFace;
        UnitsPerEm = ft->UnitsPerEM;
        GlyphCount = (int)ft->NumGlyphs.Value;
        IsSymbol = ft->Charmap is null || *(uint*)((byte*)ft->Charmap + sizeof(nint)) == Tag("symb");
        var mappings = new List<(int, uint)>();
        uint glyph;
        var code = FreeType.GetFirstChar(ft, &glyph);
        while (glyph != 0)
        {
            mappings.Add(((int)code.Value, glyph));
            code = FreeType.GetNextChar(ft, code, &glyph);
        }
        Mappings = [.. mappings];
        _map = new Dictionary<int, uint>(Mappings.Length);
        foreach (var (c, g) in Mappings)
            _map[c] = g;
        Gsub = Table("GSUB");
        var post = Table("post");
        IsFixedPitch = post is { Length: >= 16 } && (post[12] | post[13] | post[14] | post[15]) != 0;
        if (Table("OS/2") is { Length: >= 64 } os2)
            IsItalic = ((os2[62] << 8 | os2[63]) & 0x0201) != 0;
        else if (Table("head") is { Length: >= 46 } head)
            IsItalic = ((head[44] << 8 | head[45]) & 2) != 0;
    }

    static uint Tag(string tag) => (uint)(tag[0] << 24 | tag[1] << 16 | tag[2] << 8 | tag[3]);

    byte[]? Table(string tag)
    {
        CULong length = default;
        if (FreeType.LoadSfntTable(_face.FtFace, new CULong(Tag(tag)), new CLong(0), null, &length) != 0 || length.Value == 0)
            return null;
        var bytes = new byte[(int)length.Value];
        fixed (byte* p = bytes)
            if (FreeType.LoadSfntTable(_face.FtFace, new CULong(Tag(tag)), new CLong(0), p, &length) != 0)
                return null;
        return bytes;
    }

    /// <summary>The glyph a character maps to (charmap.map), or null.</summary>
    public uint? Map(int c) => _map.TryGetValue(c, out var glyph) ? glyph : null;

    /// <summary>
    /// The glyph's outline in font units (skrifa's draw_unscaled: each contour's first point marks
    /// its start) and its advance; false when it can't be loaded.
    /// </summary>
    public bool DrawUnscaled(uint glyph, List<UnscaledPoint> sink, out int advance)
    {
        advance = 0;
        if (glyph >= (uint)GlyphCount)
            return false;
        var ft = _face.FtFace;
        if (FreeType.LoadGlyph(ft, glyph, FreeType.LoadNoScale | FreeType.LoadNoBitmap) != 0)
            return false;
        var slot = ft->Glyph;
        advance = (int)slot->Metrics.HoriAdvance.Value;
        var outline = (FreeType.Outline*)&slot->OutlineStart;
        var start = 0;
        for (var c = 0; c < outline->ContourCount; c++)
        {
            int end = outline->Contours[c];
            for (var i = start; i <= end && i < outline->PointCount; i++)
            {
                var flags = (outline->Tags[i] & 3) switch
                {
                    1 => PointFlags.OnCurve,
                    2 => PointFlags.OffCurveCubic,
                    _ => PointFlags.None,
                };
                sink.Add(new UnscaledPoint((short)outline->Points[i].X.Value, (short)outline->Points[i].Y.Value, flags, i == start));
            }
            start = end + 1;
        }
        return true;
    }

    /// <summary>A glyph's advance width in font units (glyph_metrics(Size::unscaled()).advance_width), or null.</summary>
    public int? AdvanceWidth(uint glyph)
    {
        if (glyph >= (uint)GlyphCount)
            return null;
        var ft = _face.FtFace;
        if (FreeType.LoadGlyph(ft, glyph, FreeType.LoadNoScale | FreeType.LoadNoBitmap) != 0)
            return null;
        return (int)ft->Glyph->Metrics.HoriAdvance.Value;
    }

    /// <summary>Each glyph's style (skrifa's GlyphStyles, computed once per font).</summary>
    public GlyphStyleMap Styles
    {
        get
        {
            lock (this)
                return _styles ??= new GlyphStyleMap(GlyphCount, new Shaper(this));
        }
    }

    /// <summary>The unscaled metrics of a glyph's style (skrifa's lazily computed UnscaledStyleMetricsSet), or null.</summary>
    public UnscaledStyleMetrics? MetricsFor(GlyphStyle style)
    {
        var styles = Styles;
        if (styles.MetricsIndex(style) is not { } index || style.StyleClass is not { } styleClass)
            return null;
        lock (this)
        {
            _metrics ??= new UnscaledStyleMetrics?[styles.MetricsCount];
            return _metrics[index] ??= MetricsComputer.ComputeUnscaledStyleMetrics(new Shaper(this), styleClass, QuirksMode.Jit);
        }
    }
}

/// <summary>Which of FreeType's two flavours of a heuristic to follow: the runtime autohinter (Jit) or ttfautohint (Aot).</summary>
internal enum QuirksMode { Jit, Aot }
