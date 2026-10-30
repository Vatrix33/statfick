using System.Runtime.InteropServices;

namespace Statventure.Text;

/// <summary>
/// Bindings for the part of FreeType 2.14 the engine uses (BRUTAL's FreeType package ships the
/// native library only). C "long" is 64-bit on Linux and 32-bit on Windows, hence CLong.
/// </summary>
internal static unsafe partial class FreeType
{
    const string Library = "freetype";

    public const int LoadDefault = 0;
    public const int LoadNoScale = 1 << 0;
    public const int LoadNoHinting = 1 << 1;
    public const int LoadNoBitmap = 1 << 3;
    public const int LoadForceAutohint = 1 << 5;
    public const int LoadTargetLight = 1 << 16;
    public const int RenderModeNormal = 0;
    public const int RenderModeLight = 1;
    public const int RenderModeLcd = 3;
    public const int LoadTargetNormal = 0;
    public const byte PixelModeLcd = 5;
    public const byte PixelModeGray = 2;

    [StructLayout(LayoutKind.Sequential)]
    public struct Generic
    {
        public void* Data;
        public void* Finalizer;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BBox
    {
        public CLong XMin, YMin, XMax, YMax;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Vector
    {
        public CLong X, Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Matrix
    {
        public CLong XX, XY, YX, YY;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FaceRec
    {
        public CLong NumFaces;
        public CLong FaceIndex;
        public CLong FaceFlags;
        public CLong StyleFlags;
        public CLong NumGlyphs;
        public byte* FamilyName;
        public byte* StyleName;
        public int NumFixedSizes;
        public void* AvailableSizes;
        public int NumCharmaps;
        public void* Charmaps;
        public Generic Generic;
        public BBox BBox;
        public ushort UnitsPerEM;
        public short Ascender;
        public short Descender;
        public short Height;
        public short MaxAdvanceWidth;
        public short MaxAdvanceHeight;
        public short UnderlinePosition;
        public short UnderlineThickness;
        public GlyphSlotRec* Glyph;
        public SizeRec* Size;
        public void* Charmap;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SizeMetrics
    {
        public ushort XPpem;
        public ushort YPpem;
        public CLong XScale;
        public CLong YScale;
        public CLong Ascender;
        public CLong Descender;
        public CLong Height;
        public CLong MaxAdvance;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SizeRec
    {
        public FaceRec* Face;
        public Generic Generic;
        public SizeMetrics Metrics;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphMetrics
    {
        public CLong Width, Height, HoriBearingX, HoriBearingY, HoriAdvance, VertBearingX, VertBearingY, VertAdvance;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Bitmap
    {
        public uint Rows;
        public uint Width;
        public int Pitch;
        public byte* Buffer;
        public ushort NumGrays;
        public byte PixelMode;
        public byte PaletteMode;
        public void* Palette;
    }

    /// <summary>FT_Outline: contours of points (26.6, y up), each tagged on-curve, conic or cubic control.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Outline
    {
        public ushort ContourCount;
        public ushort PointCount;
        public Vector* Points;
        public byte* Tags;
        public ushort* Contours;
        public int Flags;
    }

    /// <summary>The glyph slot up to and including the start of its outline.</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GlyphSlotRec
    {
        public void* Library;
        public FaceRec* Face;
        public GlyphSlotRec* Next;
        public uint GlyphIndex;
        public Generic Generic;
        public GlyphMetrics Metrics;
        public CLong LinearHoriAdvance;
        public CLong LinearVertAdvance;
        public Vector Advance;
        public uint Format;
        public Bitmap Bitmap;
        public int BitmapLeft;
        public int BitmapTop;
        public byte OutlineStart;
    }

    [LibraryImport(Library, EntryPoint = "FT_Init_FreeType")]
    public static partial int InitFreeType(out nint library);

    [LibraryImport(Library, EntryPoint = "FT_Done_FreeType")]
    public static partial int DoneFreeType(nint library);

    [LibraryImport(Library, EntryPoint = "FT_New_Memory_Face")]
    public static partial int NewMemoryFace(nint library, byte* data, CLong size, CLong faceIndex, out FaceRec* face);

    [LibraryImport(Library, EntryPoint = "FT_Done_Face")]
    public static partial int DoneFace(FaceRec* face);

    [LibraryImport(Library, EntryPoint = "FT_Set_Char_Size")]
    public static partial int SetCharSize(FaceRec* face, CLong width, CLong height, uint horizontalDpi, uint verticalDpi);

    [LibraryImport(Library, EntryPoint = "FT_Load_Glyph")]
    public static partial int LoadGlyph(FaceRec* face, uint glyphIndex, int flags);

    [LibraryImport(Library, EntryPoint = "FT_Render_Glyph")]
    public static partial int RenderGlyph(GlyphSlotRec* slot, int renderMode);

    [LibraryImport(Library, EntryPoint = "FT_Get_Char_Index")]
    public static partial uint GetCharIndex(FaceRec* face, CULong charcode);

    [LibraryImport(Library, EntryPoint = "FT_Set_Transform")]
    public static partial void SetTransform(FaceRec* face, Matrix* matrix, Vector* delta);

    [LibraryImport(Library, EntryPoint = "FT_Outline_EmboldenXY")]
    public static partial int OutlineEmboldenXY(void* outline, CLong xStrength, CLong yStrength);

    [LibraryImport(Library, EntryPoint = "FT_Library_SetLcdFilter")]
    public static partial int LibrarySetLcdFilter(nint library, int filter);

    [LibraryImport(Library, EntryPoint = "FT_Outline_Translate")]
    public static partial void OutlineTranslate(void* outline, CLong xOffset, CLong yOffset);

    [LibraryImport(Library, EntryPoint = "FT_Get_First_Char")]
    public static partial CULong GetFirstChar(FaceRec* face, uint* glyphIndex);

    [LibraryImport(Library, EntryPoint = "FT_Get_Next_Char")]
    public static partial CULong GetNextChar(FaceRec* face, CULong charcode, uint* glyphIndex);

    [LibraryImport(Library, EntryPoint = "FT_Load_Sfnt_Table")]
    public static partial int LoadSfntTable(FaceRec* face, CULong tag, CLong offset, byte* buffer, CULong* length);

    public static void Check(int error, string what)
    {
        if (error != 0)
            throw new InvalidOperationException($"FreeType {what} failed with error {error}");
    }
}
