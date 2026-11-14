using System.Runtime.InteropServices;
using System.Numerics;

namespace Statventure.Text;

/// <summary>
/// Glyph outlines for text decoration ink skipping, as Skia's text intercepts take them: unhinted
/// outlines at the text size (with fake bold and italic as drawn), flattened into polygons in
/// pixels (y down from the baseline), and the span of x a glyph's outline covers between two y.
/// </summary>
public static unsafe class GlyphOutlines
{
    readonly record struct Key(int Face, uint Glyph, float Size, bool Bold, bool Oblique);

    static readonly Dictionary<Key, List<Vector2[]>> Cache = [];
    const int MaxCached = 4096;

    /// <summary>
    /// The leftmost and rightmost x of a glyph's outline between <paramref name="top"/> and
    /// <paramref name="bottom"/> (pixels from its origin, y down from the baseline); null where it has none.
    /// </summary>
    public static (float Begin, float End)? Intercept(FontFace face, uint glyph, float size, bool syntheticBold, bool syntheticOblique, float top, float bottom)
    {
        float left = float.MaxValue, right = float.MinValue;
        foreach (var contour in Contours(face, glyph, size, syntheticBold, syntheticOblique))
            for (var i = 0; i < contour.Length; i++)
            {
                var a = contour[i];
                var b = contour[(i + 1) % contour.Length];
                // The part of the edge inside the band.
                float t0 = 0, t1 = 1;
                if (a.Y == b.Y)
                {
                    if (a.Y < top || a.Y > bottom)
                        continue;
                }
                else
                {
                    var ta = (top - a.Y) / (b.Y - a.Y);
                    var tb = (bottom - a.Y) / (b.Y - a.Y);
                    t0 = MathF.Max(0, MathF.Min(ta, tb));
                    t1 = MathF.Min(1, MathF.Max(ta, tb));
                    if (t0 > t1)
                        continue;
                }
                var x0 = a.X + (b.X - a.X) * t0;
                var x1 = a.X + (b.X - a.X) * t1;
                left = MathF.Min(left, MathF.Min(x0, x1));
                right = MathF.Max(right, MathF.Max(x0, x1));
            }
        return left <= right ? (left, right) : null;
    }

    /// <summary>A glyph's outline as closed polygons in pixels, y down (cached).</summary>
    public static List<Vector2[]> Contours(FontFace face, uint glyph, float size, bool syntheticBold, bool syntheticOblique)
    {
        var key = new Key(face.Id, glyph, size, syntheticBold, syntheticOblique);
        lock (Cache)
        {
            if (Cache.TryGetValue(key, out var cached))
                return cached;
        }
        var contours = Load(face, glyph, size, syntheticBold, syntheticOblique);
        lock (Cache)
        {
            if (Cache.Count >= MaxCached)
                Cache.Clear();
            Cache[key] = contours;
        }
        return contours;
    }

    static List<Vector2[]> Load(FontFace face, uint glyph, float size, bool syntheticBold, bool syntheticOblique)
    {
        // An icon's outline as drawn (no synthesis); a space has none.
        if (face.Icons is { } icons)
            return icons.Contours(glyph, size);
        // The face's size, transform and glyph slot are shared with whatever else uses it now.
        lock (face.FreeTypeLock)
            return LoadLocked(face, glyph, size, syntheticBold, syntheticOblique);
    }

    static List<Vector2[]> LoadLocked(FontFace face, uint glyph, float size, bool syntheticBold, bool syntheticOblique)
    {
        var ft = face.FtFace;
        FreeType.Check(FreeType.SetCharSize(ft, new CLong(0), new CLong((nint)MathF.Round(size * 64f)), 72, 72), "FT_Set_Char_Size");
        var matrix = new FreeType.Matrix
        {
            XX = new CLong(0x10000),
            XY = new CLong(syntheticOblique ? 0x4000 : 0),
            YX = new CLong(0),
            YY = new CLong(0x10000),
        };
        var delta = new FreeType.Vector { X = new CLong(0), Y = new CLong(0) };
        FreeType.SetTransform(ft, &matrix, &delta);
        var contours = new List<Vector2[]>();
        try
        {
            if (FreeType.LoadGlyph(ft, glyph, FreeType.LoadNoBitmap | FreeType.LoadNoHinting) != 0)
                return contours;
            var slot = ft->Glyph;
            if (syntheticBold)
            {
                var strength = (long)MathF.Round(size * 64f / 24f);
                FreeType.OutlineEmboldenXY(&slot->OutlineStart, new CLong((nint)strength), new CLong((nint)strength));
                FreeType.OutlineTranslate(&slot->OutlineStart, new CLong((nint)(-strength / 2)), new CLong((nint)(-strength / 2)));
            }
            var outline = (FreeType.Outline*)&slot->OutlineStart;
            var start = 0;
            for (var c = 0; c < outline->ContourCount; c++)
            {
                var end = outline->Contours[c];
                if (end >= start)
                    contours.Add(Flatten(outline, start, end));
                start = end + 1;
            }
        }
        finally
        {
            FreeType.SetTransform(ft, null, null);
        }
        return contours;
    }

    /// <summary>One contour (FT_Outline_Decompose's rules: conic controls imply on-points between them) flattened finely.</summary>
    static Vector2[] Flatten(FreeType.Outline* outline, int first, int last)
    {
        Vector2 Point(int i) => new((float)(long)outline->Points[i].X.Value / 64f, -(float)(long)outline->Points[i].Y.Value / 64f);
        int Tag(int i) => outline->Tags[i] & 3;
        var count = last - first + 1;
        var points = new List<Vector2>();
        // Start at an on-curve point (or the midpoint of two conic controls).
        int startIndex;
        Vector2 startPoint;
        if (Tag(first) == 1)
            (startIndex, startPoint) = (0, Point(first));
        else if (Tag(last) == 1)
            (startIndex, startPoint) = (-1, Point(last));
        else
            (startIndex, startPoint) = (-1, (Point(first) + Point(last)) / 2);
        points.Add(startPoint);
        var current = startPoint;
        Vector2? conic = null;
        var cubic = new List<Vector2>();
        for (var k = startIndex + 1; k <= startIndex + count; k++)
        {
            var index = first + ((k % count) + count) % count;
            var p = Point(index);
            var tag = Tag(index);
            var closing = k == startIndex + count;
            if (tag == 1)
            {
                if (conic is { } control)
                    AddQuad(points, current, control, p);
                else if (cubic.Count == 2)
                    AddCubic(points, current, cubic[0], cubic[1], p);
                else
                    points.Add(p);
                conic = null;
                cubic.Clear();
                current = p;
            }
            else if (tag == 0)
            {
                if (conic is { } control)
                {
                    var middle = (control + p) / 2;
                    AddQuad(points, current, control, middle);
                    current = middle;
                }
                conic = p;
            }
            else
                cubic.Add(p);
            if (closing)
                break;
        }
        // Close back to the start through any pending control points.
        if (conic is { } pending)
            AddQuad(points, current, pending, startPoint);
        else if (cubic.Count == 2)
            AddCubic(points, current, cubic[0], cubic[1], startPoint);
        return [.. points];
    }

    const int Steps = 16;

    static void AddQuad(List<Vector2> points, Vector2 p0, Vector2 c, Vector2 p1)
    {
        for (var i = 1; i <= Steps; i++)
        {
            var t = i / (float)Steps;
            var u = 1 - t;
            points.Add(u * u * p0 + 2 * u * t * c + t * t * p1);
        }
    }

    static void AddCubic(List<Vector2> points, Vector2 p0, Vector2 c0, Vector2 c1, Vector2 p1)
    {
        for (var i = 1; i <= Steps; i++)
        {
            var t = i / (float)Steps;
            var u = 1 - t;
            points.Add(u * u * u * p0 + 3 * u * u * t * c0 + 3 * u * t * t * c1 + t * t * t * p1);
        }
    }
}
