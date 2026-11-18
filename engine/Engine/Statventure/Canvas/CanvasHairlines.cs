using System.Numerics;
using Statventure.Core;

namespace Statventure.Canvas;

public sealed partial class Canvas
{
    /// <summary>A hairline's masks: its stroke's rounded rect where the path is made (under a translation alone, relative to
    /// the whole pixel it is drawn at), the transform (none then) and the paint's alpha.</summary>
    readonly record struct HairlineKey(float L, float T, float R, float B, Vector2 R0, Vector2 R1, Vector2 R2, Vector2 R3, Matrix3x2 Map, int Alpha);

    /// <summary>A hairline drawn: its curved tiles in the hairline atlas and its straight runs as plain pixel rects (a
    /// coverage of 0-255), each relative to the origin.</summary>
    sealed record HairlineDraws(List<(AtlasEntry Entry, int X, int Y)> Tiles, List<(int X, int Y, int Width, int Height, byte Coverage)> Runs);

    readonly Dictionary<HairlineKey, (int Generation, HairlineDraws Draws)> _hairlineCache = [];

    /// <summary>
    /// The hairlines' own atlas: the shared one is cleared when it fills, which spoils the draws already made in that frame
    /// from it, so the hairlines don't add to it, and theirs is cleared only between frames (<see cref="Begin"/>) once
    /// half of it is used.
    /// </summary>
    TextureAtlas? _hairlineAtlas;

    internal TextureAtlas HairlineAtlas => _hairlineAtlas ??= new TextureAtlas(_device, pageSize: 1024, maxPages: 16);

    /// <summary>The side of the square tiles a hairline's mask is cut into: the curved ones go to the atlas, the straight
    /// ones (one value along every row, or down every column) become runs.</summary>
    const int HairlineTile = 16;

    /// <summary>A value Blink's layout keeps on 1/64 device px, put back on 1/1024 px (the canvas's float noise off).</summary>
    static float OnGrid(float v) => MathF.Round(v * 1024f) / 1024f;

    /// <summary>
    /// The ring of <paramref name="width"/> inside a rounded rect whose inner edge is its outer one moved in by the width at
    /// every corner, as Chromium 152 draws it (GraphicsContext::FillDRRect's simple case, IsSimpleDRRect): Skia's stroke of
    /// the rounded rect inset by half the width (SkRRect::inset), drawn by SkCanvas::drawRRect. A stroke that the transform
    /// keeps at most one device pixel wide along both axes (DrawTreatAAStrokeAsHairline, fast_len) is Skia's antialiased
    /// hairline (<see cref="SkiaHairline"/>), a narrower one with its paint's alpha scaled by the width; a wider one, or a
    /// stroke the inset leaves square (drawRect's frame), is the border band between the two rounded rects.
    /// </summary>
    /// <param name="outer">The outer rounded rect (a box's snapped border box), in this canvas's units.</param>
    public void StrokeRoundRect(RectF outer, CornerRadii radii, float width, Color color)
    {
        if (outer.IsEmpty || width <= 0 || color.A <= 0)
            return;
        var fitted = radii.Fit(outer.Size);
        if (HairlineCoverage(width) is not { } coverage || !DrawHairlineRRect(outer, fitted, width, color, coverage))
            DrawBox(outer, null, fitted, new Border(width, color));
    }

    /// <summary>
    /// DrawTreatAAStrokeAsHairline: the stroke's width in Blink's units (device pixels of the layout) mapped by the
    /// transform along x and along y, each measured by fast_len (the longer component plus half the shorter); a hairline
    /// when both are at most one pixel, its coverage their mean. Null when the stroke is wider.
    /// </summary>
    float? HairlineCoverage(float width)
    {
        var w = OnGrid(width * Scale);
        var m = _transform;
        var len0 = FastLength(new Vector2(w * m.M11, w * m.M12));
        var len1 = FastLength(new Vector2(w * m.M21, w * m.M22));
        if (len0 > 1 || len1 > 1)
            return null;
        return (float)(((double)len0 + len1) * 0.5);
    }

    static float FastLength(Vector2 v)
    {
        float x = MathF.Abs(v.X), y = MathF.Abs(v.Y);
        if (x < y)
            (x, y) = (y, x);
        return x + y / 2f;
    }

    /// <summary>The hairline of the rrect <paramref name="outer"/> inset by half the width; false when Skia wouldn't draw a
    /// hairline (the inset leaves a square rect: drawRect strokes it as a frame).</summary>
    bool DrawHairlineRRect(RectF outer, CornerRadii radii, float width, Color color, float coverage)
    {
        // Blink's rounded rect in its own units (device pixels of the layout), converted to an SkRRect (setRectRadii),
        // and the stroke's: inset by half the stroke.
        var k = Scale;
        Span<Vector2> r =
        [
            new(OnGrid(radii.TopLeft.X * k), OnGrid(radii.TopLeft.Y * k)), new(OnGrid(radii.TopRight.X * k), OnGrid(radii.TopRight.Y * k)),
            new(OnGrid(radii.BottomRight.X * k), OnGrid(radii.BottomRight.Y * k)), new(OnGrid(radii.BottomLeft.X * k), OnGrid(radii.BottomLeft.Y * k)),
        ];
        // Skia's matrix in those units: the canvas's linear part, its translation in device pixels.
        var m = _transform;
        var translation = new Vector2(OnGrid(m.M31 * k), OnGrid(m.M32 * k));
        var translationOnly = m.M11 == 1 && m.M12 == 0 && m.M21 == 0 && m.M22 == 1;
        float l = OnGrid(outer.Left * k), t = OnGrid(outer.Top * k), right = OnGrid(outer.Right * k), b = OnGrid(outer.Bottom * k);
        int originX = 0, originY = 0;
        var map = new Matrix3x2(m.M11, m.M12, m.M21, m.M22, translation.X, translation.Y);
        if (translationOnly)
        {
            // Where the rect lands (Skia adds the translation to the path's points; on the grid the sums are exact), drawn
            // relative to its whole pixel.
            l += translation.X;
            right += translation.X;
            t += translation.Y;
            b += translation.Y;
            originX = (int)MathF.Floor(l);
            originY = (int)MathF.Floor(t);
            (l, right, t, b) = (l - originX, right - originX, t - originY, b - originY);
            map = Matrix3x2.Identity;
        }
        var stroke = SkRRect.Make(l, t, right, b, r).Inset(OnGrid(width * k) / 2);
        if (stroke.IsEmpty)
            return true;
        if (stroke.IsRect)
            return false;
        // The paint's alpha, scaled by a stroke's width under a pixel (modifyPaintForHairlines' old technique).
        var alpha = (int)MathF.Round(color.A * 255);
        if (coverage < 1)
            alpha = alpha * (int)(coverage * 256) >> 8;
        if (alpha <= 0)
            return true;
        var key = new HairlineKey(stroke.Left, stroke.Top, stroke.Right, stroke.Bottom, stroke.Radius(0), stroke.Radius(1), stroke.Radius(2), stroke.Radius(3), map, alpha);
        var paint = (color with { A = alpha / 255f }).Premultiplied;
        var draws = HairlineDrawsOf(key, stroke);
        foreach (var (entry, x, y) in draws.Tiles)
        {
            var primitive = PixelPrimitive(new RectF(originX + x, originY + y, entry.Width, entry.Height), KindCoverage);
            primitive.Texture = (uint)entry.TextureIndex;
            primitive.Uv = new Vector4(entry.U0, entry.V0, entry.U1, entry.V1);
            primitive.ColorMatrix = CurrentMatrix();
            primitive.Color = paint;
            Add(primitive);
        }
        foreach (var run in draws.Runs)
        {
            // A plain box on whole pixels covers them wholly: the run's coverage rides in its colour.
            var primitive = PixelPrimitive(new RectF(originX + run.X, originY + run.Y, run.Width, run.Height), KindBox);
            primitive.ColorMatrix = CurrentMatrix();
            primitive.Color = paint * (run.Coverage / 255f);
            Add(primitive);
        }
        return true;
    }

    /// <summary>Clears the hairlines' atlas between frames once half of it is used (a frame never fills the other half).</summary>
    void TrimHairlineAtlas()
    {
        if (_hairlineAtlas is { } atlas && atlas.UsedPages * 2 >= atlas.MaxPages)
            atlas.Clear();
    }

    /// <summary>
    /// The hairline's mask (cached) cut into tiles of <see cref="HairlineTile"/> pixels: a tile with nothing is dropped, a
    /// tile whose every row holds one value (a piece of a horizontal line) or whose every column does (a vertical one)
    /// becomes runs, merged with their neighbours along the line, and any other (a curve, a line's end) goes to the atlas.
    /// </summary>
    HairlineDraws HairlineDrawsOf(HairlineKey key, in SkRRect stroke)
    {
        var atlas = HairlineAtlas;
        if (_hairlineCache.TryGetValue(key, out var cached) && cached.Generation == atlas.Generation)
            return cached.Draws;
        var mask = SkiaHairline.RRect(stroke, key.Alpha / 255f, key.Map.IsIdentity ? null : key.Map);
        var tiles = new List<(AtlasEntry, int, int)>();
        var rows = new List<(int X, int Y, int Width, int Height, byte Coverage)>();
        var columns = new List<(int X, int Y, int Width, int Height, byte Coverage)>();
        if (mask is not null)
        {
            var values = new byte[mask.Width * mask.Height];
            for (var y = 0; y < mask.Height; y++)
                for (var x = 0; x < mask.Width; x++)
                    values[y * mask.Width + x] = (byte)MathF.Round(mask.Coverage(x, y) * 255);
            byte At(int x, int y) => values[y * mask.Width + x];
            for (var ty = 0; ty < mask.Height; ty += HairlineTile)
                for (var tx = 0; tx < mask.Width; tx += HairlineTile)
                {
                    var w = Math.Min(HairlineTile, mask.Width - tx);
                    var h = Math.Min(HairlineTile, mask.Height - ty);
                    bool any = false, rowsOne = true, columnsOne = true;
                    for (var y = 0; y < h; y++)
                        for (var x = 0; x < w; x++)
                        {
                            var c = At(tx + x, ty + y);
                            any |= c != 0;
                            rowsOne &= c == At(tx, ty + y);
                            columnsOne &= c == At(tx + x, ty);
                        }
                    if (!any)
                        continue;
                    if (rowsOne)
                    {
                        for (var y = 0; y < h; y++)
                            if (At(tx, ty + y) is var c and not 0)
                                rows.Add((mask.X + tx, mask.Y + ty + y, w, 1, c));
                    }
                    else if (columnsOne)
                    {
                        for (var x = 0; x < w; x++)
                            if (At(tx + x, ty) is var c and not 0)
                                columns.Add((mask.X + tx + x, mask.Y + ty, 1, h, c));
                    }
                    else
                    {
                        var bytes = new byte[w * h];
                        for (var y = 0; y < h; y++)
                            for (var x = 0; x < w; x++)
                                bytes[y * w + x] = At(tx + x, ty + y);
                        tiles.Add((atlas.Add(w, h, bytes), mask.X + tx, mask.Y + ty));
                    }
                }
        }
        var draws = new HairlineDraws(tiles, [.. Merge(rows, horizontal: true), .. Merge(columns, horizontal: false)]);
        _hairlineCache[key] = (atlas.Generation, draws);
        return draws;
    }

    /// <summary>Runs along one line of pixels with one coverage, each meeting the next, made one.</summary>
    static IEnumerable<(int X, int Y, int Width, int Height, byte Coverage)> Merge(List<(int X, int Y, int Width, int Height, byte Coverage)> runs, bool horizontal)
    {
        var sorted = horizontal ? runs.OrderBy(r => r.Y).ThenBy(r => r.X) : runs.OrderBy(r => r.X).ThenBy(r => r.Y);
        (int X, int Y, int Width, int Height, byte Coverage)? open = null;
        foreach (var run in sorted)
        {
            if (open is { } o && o.Coverage == run.Coverage
                && (horizontal ? o.Y == run.Y && o.X + o.Width == run.X : o.X == run.X && o.Y + o.Height == run.Y))
            {
                open = horizontal ? o with { Width = o.Width + run.Width } : o with { Height = o.Height + run.Height };
                continue;
            }
            if (open is { } done)
                yield return done;
            open = run;
        }
        if (open is { } last)
            yield return last;
    }
}
