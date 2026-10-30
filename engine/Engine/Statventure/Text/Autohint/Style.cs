namespace Statventure.Text.Autohint;

internal enum ScriptGroup { Default, Cjk, Indic }

/// <summary>The zones a blue value stands for (skrifa's BlueZones: FreeType's afblue.h flags as aflatin.h uses them).</summary>
[Flags]
internal enum BlueZones : ushort
{
    None = 0,
    Top = 1 << 1,
    SubTop = 1 << 2,
    Neutral = 1 << 3,
    Adjustment = 1 << 4,
    XHeight = 1 << 5,
    Long = 1 << 6,
    Horizontal = 1 << 2,
    Right = Top,
}

internal static class BlueZonesExtensions
{
    public static bool IsTopLike(this BlueZones z) => (z & (BlueZones.Top | BlueZones.SubTop)) != 0;
    public static bool IsTop(this BlueZones z) => (z & BlueZones.Top) != 0;
    public static bool IsSubTop(this BlueZones z) => (z & BlueZones.SubTop) != 0;
    public static bool IsNeutral(this BlueZones z) => (z & BlueZones.Neutral) != 0;
    public static bool IsXHeight(this BlueZones z) => (z & BlueZones.XHeight) != 0;
    public static bool IsLong(this BlueZones z) => (z & BlueZones.Long) != 0;
    public static bool IsHorizontal(this BlueZones z) => (z & BlueZones.Horizontal) != 0;
    public static bool IsRight(this BlueZones z) => (z & BlueZones.Right) != 0;
    public static BlueZones RetainTopLikeOrNeutral(this BlueZones z) => z & (BlueZones.Top | BlueZones.SubTop | BlueZones.Neutral);
}

/// <summary>A writing system's hinting data (FreeType's script classes): blue strings and standard characters.</summary>
internal sealed partial class ScriptClass(string name, ScriptGroup group, string tag, bool hintTopToBottom, string stdChars, (string Chars, BlueZones Zones)[] blues)
{
    public string Name { get; } = name;
    public ScriptGroup Group { get; } = group;
    /// <summary>The ISO 15924 script tag.</summary>
    public string Tag { get; } = tag;
    public bool HintTopToBottom { get; } = hintTopToBottom;
    public string StdChars { get; } = stdChars;
    public (string Chars, BlueZones Zones)[] Blues { get; } = blues;
}

/// <summary>A script, possibly with an OpenType feature (small capitals, superscripts…), that glyphs are hinted as.</summary>
internal sealed partial class StyleClass(string name, int index, ScriptClass script, string? feature)
{
    public string Name { get; } = name;
    public int Index { get; } = index;
    public ScriptClass Script { get; } = script;
    public string? Feature { get; } = feature;

    public static StyleClass? FromIndex(int index) => (uint)index < (uint)All.Length ? All[index] : null;
}

/// <summary>A glyph's style: its style class index and whether it's a non-base glyph or a digit (skrifa's GlyphStyle).</summary>
internal struct GlyphStyle(ushort bits)
{
    public const ushort StyleIndexMask = 0xFF;
    public const ushort Unassigned = StyleIndexMask;
    public const ushort NonBase = 0x100;
    public const ushort Digit = 0x200;
    public const ushort FromGsubOutput = 0x8000;

    public ushort Bits = bits;

    public GlyphStyle() : this(Unassigned)
    {
    }

    public readonly bool IsUnassigned => (Bits & StyleIndexMask) == Unassigned;
    public readonly bool IsNonBase => (Bits & NonBase) != 0;
    public readonly bool IsDigit => (Bits & Digit) != 0;
    public readonly int? StyleIndex => (Bits & StyleIndexMask) is var ix && ix != Unassigned ? ix : null;
    public readonly StyleClass? StyleClass => StyleIndex is { } ix ? StyleClass.FromIndex(ix) : null;

    /// <summary>Takes the other style's index (and flags) when its index is lower or equal.</summary>
    public void MaybeAssign(GlyphStyle other)
    {
        if ((other.Bits & StyleIndexMask) <= (Bits & StyleIndexMask))
            Bits = (ushort)((Bits & ~StyleIndexMask) | other.Bits);
    }

    public void SetFromGsubOutput() => Bits |= FromGsubOutput;

    /// <summary>A glyph only GSUB reaches (and no style has claimed yet) takes the style.</summary>
    public bool MaybeAssignGsubOutputStyle(StyleClass style)
    {
        if ((Bits & FromGsubOutput) != 0 && IsUnassigned)
        {
            Bits &= unchecked((ushort)~FromGsubOutput);
            Bits = (ushort)((Bits & ~StyleIndexMask) | style.Index);
            return true;
        }
        return false;
    }
}

internal readonly record struct StyleRange(int First, int Last, GlyphStyle Style)
{
    public bool Contains(int c) => c >= First && c <= Last;
}

/// <summary>
/// Every glyph's style and which styles need metrics (skrifa's GlyphStyleMap, FreeType's
/// af_face_globals_compute_style_coverage): styles with features from GSUB first, then the
/// character map's Unicode ranges, then GSUB outputs of the plain styles, then the default
/// script's; what's left is CJK; digits are marked.
/// </summary>
internal sealed class GlyphStyleMap
{
    const byte Unmapped = 0xFF;

    readonly GlyphStyle[] _styles;
    readonly byte[] _metricsMap = new byte[StyleClass.All.Length];
    public int MetricsCount { get; private set; }

    public GlyphStyleMap(int glyphCount, Shaper shaper)
    {
        Array.Fill(_metricsMap, Unmapped);
        _styles = new GlyphStyle[glyphCount];
        Array.Fill(_styles, new GlyphStyle());
        var visited = new VisitedLookupSet(shaper.LookupCount);
        foreach (var style in StyleClass.All)
            if (style.Feature is not null && shaper.ComputeCoverage(style, ShaperCoverageKind.Script, _styles, visited))
                UseStyle(style.Index);
        StyleRange? lastRange = null;
        foreach (var (c, glyph) in shaper.Font.Mappings)
        {
            if (glyph >= (uint)_styles.Length)
                continue;
            ref var style = ref _styles[glyph];
            if (lastRange is { } last && last.Contains(c))
            {
                style.MaybeAssign(last.Style);
                continue;
            }
            var ix = BinarySearchFirst(c);
            if ((uint)ix >= (uint)StyleRanges.All.Length)
                continue;
            var range = StyleRanges.All[ix];
            if (range.Contains(c))
            {
                style.MaybeAssign(range.Style);
                if (range.Style.StyleIndex is { } styleIx)
                    UseStyle(styleIx);
                lastRange = range;
            }
        }
        foreach (var style in StyleClass.All)
            if (style.Feature is null && shaper.ComputeCoverage(style, ShaperCoverageKind.Script, _styles, visited))
                UseStyle(style.Index);
        var defaultStyle = StyleClass.All[StyleClass.Latn];
        if (shaper.ComputeCoverage(defaultStyle, ShaperCoverageKind.Default, _styles, visited))
            UseStyle(defaultStyle.Index);
        var needHani = false;
        for (var i = 0; i < _styles.Length; i++)
            if (_styles[i].IsUnassigned)
            {
                _styles[i].Bits = (ushort)((_styles[i].Bits & ~GlyphStyle.StyleIndexMask) | StyleClass.Hani);
                needHani = true;
            }
        if (needHani)
            UseStyle(StyleClass.Hani);
        for (var digit = '0'; digit <= '9'; digit++)
            if (shaper.Font.Map(digit) is { } glyph && glyph < (uint)_styles.Length)
                _styles[glyph].Bits |= GlyphStyle.Digit;
    }

    /// <summary>The range whose first character is at or before c (Rust's binary_search_by on first, then one back).</summary>
    static int BinarySearchFirst(int c)
    {
        var ranges = StyleRanges.All;
        int lo = 0, hi = ranges.Length;
        // binary_search_by returns any match; firsts are unique, so this is the match or the insertion point.
        while (lo < hi)
        {
            var mid = lo + (hi - lo) / 2;
            var first = ranges[mid].First;
            if (first == c)
                return mid;
            if (first < c)
                lo = mid + 1;
            else
                hi = mid;
        }
        return Math.Max(0, lo - 1);
    }

    public GlyphStyle? Style(uint glyph) => glyph < (uint)_styles.Length ? _styles[glyph] : null;

    public int? MetricsIndex(GlyphStyle style)
    {
        if (style.StyleIndex is not { } ix || (uint)ix >= (uint)_metricsMap.Length)
            return null;
        var mapped = _metricsMap[ix];
        return mapped != Unmapped ? mapped : null;
    }

    void UseStyle(int styleIx)
    {
        if (_metricsMap[styleIx] == Unmapped)
            _metricsMap[styleIx] = (byte)MetricsCount++;
    }
}
