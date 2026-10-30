using System.Runtime.InteropServices;
using HarfBuzzSharp;

namespace Statventure.Text;

/// <summary>
/// One font file (one face): FreeType draws its glyphs, HarfBuzz shapes text with it. The data stays
/// pinned in unmanaged memory for both libraries.
/// </summary>
public sealed unsafe class FontFace : IDisposable
{
    static int _nextId;
    readonly byte* _data;
    readonly Blob _blob;
    readonly Face _hbFace;
    internal readonly Font HbFont;
    internal readonly FreeType.FaceRec* FtFace;
    internal readonly nint FtLibrary;

    public int Id { get; }
    public string Family { get; }
    /// <summary>CSS weight (100..900).</summary>
    public int Weight { get; }
    /// <summary>The CSS weight range this face covers (e.g. 500-600 for a "500 600" @font-face).</summary>
    public int WeightMax { get; }
    public bool Italic { get; }
    public int UnitsPerEm { get; }
    /// <summary>Ascent and descent (both positive) and line gap in font units, as browsers read them (hhea through FreeType).</summary>
    public int Ascent { get; }
    public int Descent { get; }
    public int LineGap { get; }
    public int UnderlinePosition { get; }
    public int UnderlineThickness { get; }
    /// <summary>The x-height in font units (OS/2 sxHeight, else the height of "x").</summary>
    public int XHeight { get; }
    /// <summary>OS/2 xAvgCharWidth in font units (0 when the table is missing).</summary>
    public int AverageCharWidth { get; }
    /// <summary>OS/2 sTypoAscender and sTypoDescender (the latter positive below the baseline) in font units, 0 without the table.</summary>
    public int TypoAscender { get; }
    public int TypoDescender { get; }
    /// <summary>hhea advanceWidthMax in font units.</summary>
    public int MaxAdvance { get; }
    /// <summary>The width of the font's bounding box (head xMax - xMin) in font units.</summary>
    public int BoundsWidth { get; }

    internal FontFace(nint library, ReadOnlySpan<byte> data, string family, int weight, int weightMax, bool italic)
    {
        Id = Interlocked.Increment(ref _nextId);
        FtLibrary = library;
        Family = family;
        Weight = weight;
        WeightMax = Math.Max(weight, weightMax);
        Italic = italic;
        _data = (byte*)NativeMemory.Alloc((nuint)data.Length);
        data.CopyTo(new Span<byte>(_data, data.Length));
        FreeType.Check(FreeType.NewMemoryFace(library, _data, new CLong(data.Length), new CLong(0), out FtFace), "FT_New_Memory_Face");
        _blob = new Blob((nint)_data, data.Length, MemoryMode.ReadOnly);
        _hbFace = new Face(_blob, 0);
        HbFont = new Font(_hbFace);
        UnitsPerEm = FtFace->UnitsPerEM;
        HbFont.SetScale(UnitsPerEm, UnitsPerEm);
        Ascent = FtFace->Ascender;
        Descent = -FtFace->Descender;
        LineGap = Math.Max(0, FtFace->Height - FtFace->Ascender + FtFace->Descender);
        UnderlinePosition = -FtFace->UnderlinePosition;
        UnderlineThickness = FtFace->UnderlineThickness;
        if (HbFont.OpenTypeMetrics.TryGetPosition(OpenTypeMetricsTag.XHeight, out var xHeight) && xHeight > 0)
            XHeight = xHeight;
        else if (HbFont.TryGetGlyph('x', out var x) && HbFont.TryGetGlyphExtents(x, out var extents))
            XHeight = extents.YBearing;
        else
            XHeight = UnitsPerEm / 2;
        using (var os2 = _hbFace.ReferenceTable(Tag.Parse("OS/2")))
        {
            var bytes = os2.AsSpan();
            if (bytes.Length >= 4)
                AverageCharWidth = (short)(bytes[2] << 8 | bytes[3]);
            if (bytes.Length >= 72)
            {
                TypoAscender = (short)(bytes[68] << 8 | bytes[69]);
                TypoDescender = -(short)(bytes[70] << 8 | bytes[71]);
            }
        }
        using (var head = _hbFace.ReferenceTable(Tag.Parse("head")))
        {
            var bytes = head.AsSpan();
            if (bytes.Length >= 44)
                BoundsWidth = (short)(bytes[40] << 8 | bytes[41]) - (short)(bytes[36] << 8 | bytes[37]);
        }
        using (var hhea = _hbFace.ReferenceTable(Tag.Parse("hhea")))
        {
            var bytes = hhea.AsSpan();
            if (bytes.Length >= 12)
                MaxAdvance = bytes[10] << 8 | bytes[11];
        }
    }

    /// <summary>The glyph for a character, or 0 when the face doesn't have it.</summary>
    public uint GlyphFor(int codepoint) => HbFont.TryGetGlyph((uint)codepoint, out var glyph) ? glyph : 0;

    // Which BMP characters the face has, learned as they are asked for (0 unknown, 1 yes, 2 no).
    // Races only ever write the same answer.
    byte[]? _coverage;

    public bool Has(int codepoint)
    {
        if ((uint)codepoint > 0xFFFF)
            return GlyphFor(codepoint) != 0;
        var coverage = _coverage ??= new byte[0x10000];
        var known = coverage[codepoint];
        if (known != 0)
            return known == 1;
        var has = GlyphFor(codepoint) != 0;
        coverage[codepoint] = has ? (byte)1 : (byte)2;
        return has;
    }

    public override string ToString() => $"{Family} {Weight}{(Italic ? " italic" : "")}";

    public void Dispose()
    {
        HbFont.Dispose();
        _hbFace.Dispose();
        _blob.Dispose();
        FreeType.DoneFace(FtFace);
        NativeMemory.Free(_data);
    }
}
