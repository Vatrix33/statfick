using System.Numerics;
using System.Runtime.InteropServices;
using Statventure.Core;

namespace Statventure.Text.Skia;

/// <summary>
/// Glyph images as Chromium 152's Skia makes them for web fonts. On Linux every web font goes
/// through Fontations (SkTypeface_Make_Fontations): the outline is scaled to the text size in
/// 26.6 (as FreeType scales it, so FreeType loads it here) and drawn as a path with Skia's analytic
/// anti-aliasing (SkScalerContext::GenerateImageFromPath): grayscale masks directly, LCD masks at
/// four times the width and filtered down to red, green and blue with Skia's FIR. Then Skia's mask
/// gamma (Linux: contrast 0.2, gamma 1.2) corrects the coverage for the text's colour, and LCD
/// masks keep 5 bits a channel (RGB565, as Skia's CPU blitter reads them).
/// </summary>
public static unsafe class SkiaGlyphs
{
    /// <summary>
    /// The colour bucket Skia's mask gamma depends on (SkMaskGamma::CanonicalColor): 3 bits of
    /// each of red, green and blue for LCD masks, 3 bits of the luminance for grayscale ones.
    /// </summary>
    public static int LuminanceKey(Color color, bool lcd)
    {
        static int Channel(float c) => Math.Clamp((int)MathF.Round(c * 255), 0, 255) >> 5;
        int r = Channel(color.R), g = Channel(color.G), b = Channel(color.B);
        if (lcd)
            return (r << 6) | (g << 3) | b;
        static int Canonical(int i) => (i << 5) | (i << 2) | (i >> 1);
        var lum = (Canonical(r) * 54 + Canonical(g) * 183 + Canonical(b) * 19) >> 8;
        return lum >> 5;
    }

    /// <summary>
    /// A glyph's image at <paramref name="size"/> pixels, <paramref name="subpixel"/> quarter pixels
    /// to the right, for text whose colour gives <paramref name="luminanceKey"/>. Grayscale coverage
    /// is 0–255; LCD coverage is 0–32 per channel (Skia's upscaled 5 bits).
    /// </summary>
    public static GlyphBitmap Render(FontFace face, uint glyph, float size, int subpixel, bool syntheticBold, bool syntheticOblique, FontRenderSettings settings, bool lcd, int luminanceKey)
    {
        if (face.IsIconFace)
            throw new InvalidOperationException($"{face} is an icon face: the canvas fills its outlines as paths (Canvas.DrawGlyphs)");
        lcd &= settings.IsSubpixel;
        var empty = new GlyphBitmap(0, 0, 0, 0, [], lcd, Preblended: true);
        var path = Outline(face, glyph, size, settings.Hinting);
        if (path is null || path.IsEmpty)
            return empty;
        if (syntheticOblique)
            path = path.Affine(1, -0.25f, 0, 0, 1, 0);
        if (subpixel != 0)
            path = path.Offset(Fixed.ToFloat(subpixel << 14), 0);
        // Synthetic bold strokes the outline (SkScalerContext::internalGetPath with the frame width
        // useStrokeForFakeBold sets), after the subpixel offset.
        if (syntheticBold)
            path = SkiaStroke.FakeBold(path, size);

        // SkScalerContext::GenerateMetricsFromPath: LCD masks get a pixel each side for the filter.
        var bounds = path.Bounds;
        var (left, top, right, bottom) = bounds.RoundOut();
        if (!bounds.IsEmpty && lcd)
        {
            left--;
            right++;
        }
        int width = right - left, height = bottom - top;
        if (width <= 0 || height <= 0)
            return empty;

        var gamma = MaskGamma.Tables;
        if (!lcd)
        {
            var target = new A8Target(width, height);
            AnalyticScan.FillPath(path.ScaleTranslate(1, 1, -left, -top), target);
            var table = gamma[luminanceKey & 7];
            var coverage = target.Pixels;
            for (var i = 0; i < coverage.Length; i++)
                coverage[i] = table[coverage[i]];
            return new GlyphBitmap(width, height, left, -top, coverage, Lcd: false, Preblended: true);
        }

        var sampleWidth = 4 * width - 8;
        var samples = new A8Target(sampleWidth, height);
        AnalyticScan.FillPath(path.ScaleTranslate(4, 1, -(left + 1) * 4, -top), samples);
        var bgr = settings.Antialias == TextAntialias.SubpixelBgr;
        var rgba = PackLcd(samples.Pixels, sampleWidth, width, height, bgr,
            gamma[(luminanceKey >> 6) & 7], gamma[(luminanceKey >> 3) & 7], gamma[luminanceKey & 7]);
        return new GlyphBitmap(width, height, left, -top, rgba, Lcd: true, Preblended: true);
    }

    // Gaussian FIR taps (5 samples = 3 standard deviations, 0x110 "contrast"), one filter per
    // subpixel; red and blue are centred inside the first and last sample.
    static readonly int[][] Coefficients =
    [
        [0x03, 0x0b, 0x1c, 0x33, 0x40, 0x39, 0x24, 0x10, 0x05, 0x01, 0x00, 0x00],
        [0x00, 0x02, 0x08, 0x16, 0x2b, 0x3d, 0x3d, 0x2b, 0x16, 0x08, 0x02, 0x00],
        [0x00, 0x00, 0x01, 0x05, 0x10, 0x24, 0x39, 0x40, 0x33, 0x1c, 0x0b, 0x03],
    ];

    /// <summary>pack4xHToMask: four samples a pixel filtered to RGB, mask-gamma corrected, 565, upscaled to 0–32.</summary>
    static byte[] PackLcd(byte[] samples, int sampleWidth, int width, int height, bool bgr, byte[] tableR, byte[] tableG, byte[] tableB)
    {
        var rgba = new byte[width * height * 4];
        Span<int> fir = stackalloc int[3];
        for (var y = 0; y < height; y++)
        {
            var row = y * sampleWidth;
            var o = y * width * 4;
            for (var sampleX = -4; sampleX < sampleWidth + 4; sampleX += 4, o += 4)
            {
                fir.Clear();
                for (int s = Math.Max(0, sampleX - 4), c = s - (sampleX - 4); s < Math.Min(sampleX + 8, sampleWidth); s++, c++)
                {
                    int value = samples[row + s];
                    for (var k = 0; k < 3; k++)
                        fir[k] += Coefficients[k][c] * value;
                }
                for (var k = 0; k < 3; k++)
                    fir[k] = Math.Min(fir[k] / 0x100, 255);
                int r = bgr ? fir[2] : fir[0], g = fir[1], b = bgr ? fir[0] : fir[2];
                r = tableR[r];
                g = tableG[g];
                b = tableB[b];
                // RGB565, then each channel's top 5 bits as the blitter reads them, upscaled to 0..32.
                static int Upscale(int five) => five + (five >> 4);
                var mr = Upscale(r >> 3);
                var mg = Upscale((g >> 2) >> 1);
                var mb = Upscale(b >> 3);
                rgba[o] = (byte)mr;
                rgba[o + 1] = (byte)mg;
                rgba[o + 2] = (byte)mb;
                rgba[o + 3] = (byte)Math.Max(mr, Math.Max(mg, mb));
            }
        }
        return rgba;
    }

    static readonly Dictionary<nint, bool> TrueTypeFaces = [];

    /// <summary>Whether the face has TrueType outlines (a glyf table), which Fontations autohints at slight hinting.</summary>
    static bool HasGlyf(FontFace face)
    {
        lock (TrueTypeFaces)
        {
            if (TrueTypeFaces.TryGetValue((nint)face.FtFace, out var known))
                return known;
            CULong length = default;
            const uint glyf = ('g' << 24) | ('l' << 16) | ('y' << 8) | 'f';
            // Asking only for the length reads the face's table directory, which never changes (no
            // stream or slot): no need for the face's lock.
            var has = FreeType.LoadSfntTable(face.FtFace, new CULong(glyf), new CLong(0), null, &length) == 0;
            TrueTypeFaces[(nint)face.FtFace] = has;
            return has;
        }
    }

    /// <summary>
    /// The glyph's outline at the text size as Fontations draws it into Skia's path (skrifa's
    /// FreeType-style contour walk, Skia's VerbsPointsPen), y down; null when it has none.
    /// </summary>
    static SkiaPath? Outline(FontFace face, uint glyph, float size, TextHinting hinting)
    {
        // Slight hinting of TrueType outlines is skrifa's autohinter in light mode (Skia forces it for glyf fonts).
        if (hinting == TextHinting.Slight && HasGlyf(face))
            return AutohintedOutline(face, glyph, size);
        var flags = FreeType.LoadNoBitmap | hinting switch
        {
            TextHinting.None => FreeType.LoadNoHinting,
            TextHinting.Slight => FreeType.LoadTargetLight,
            _ => FreeType.LoadTargetNormal | (HasGlyf(face) ? FreeType.LoadForceAutohint : 0),
        };
        ContourPoint[] points;
        int[] ends;
        // The face's size, transform and glyph slot are shared with whatever else uses it now.
        lock (face.FreeTypeLock)
        {
            var ft = face.FtFace;
            // skrifa's Size: (ppem * 64) as i32.
            FreeType.Check(FreeType.SetCharSize(ft, new CLong(0), new CLong((nint)(int)(size * 64f)), 72, 72), "FT_Set_Char_Size");
            FreeType.SetTransform(ft, null, null);
            if (FreeType.LoadGlyph(ft, glyph, flags) != 0)
                return null;
            var outline = (FreeType.Outline*)&ft->Glyph->OutlineStart;
            points = new ContourPoint[outline->PointCount];
            for (var i = 0; i < points.Length; i++)
                points[i] = PointAt(outline, i);
            ends = new int[outline->ContourCount];
            for (var c = 0; c < ends.Length; c++)
                ends[c] = outline->Contours[c];
        }
        return PathOf(points, ends);
    }

    /// <summary>The glyph's outline hinted by skrifa's autohinter in light mode (vertical hinting only, linear advances), y down.</summary>
    static SkiaPath? AutohintedOutline(FontFace face, uint glyph, float size)
    {
        if (Autohint.Autohinter.Draw(face, glyph, size, new Autohint.HintTarget(false, Autohint.SmoothMode.Light, false)) is not ({ } outline, _))
            return null;
        var points = new ContourPoint[outline.PointCount];
        for (var i = 0; i < points.Length; i++)
        {
            var p = outline.Points[i];
            var kind = (p.Flags & Autohint.PointFlags.OnCurve) != 0 ? PointKind.On : (p.Flags & Autohint.PointFlags.OffCurveCubic) != 0 ? PointKind.Cubic : PointKind.Quad;
            points[i] = new ContourPoint(p.X, p.Y, kind);
        }
        var ends = new int[outline.Contours.Count];
        for (var c = 0; c < ends.Length; c++)
            ends[c] = outline.Contours[c].Last;
        return PathOf(points, ends);
    }

    /// <summary>Contours of points in 26.6 (y up), each ending at its index in <paramref name="ends"/>, as a path; null when one doesn't convert.</summary>
    static SkiaPath? PathOf(ContourPoint[] points, int[] ends)
    {
        var pen = new Pen();
        var start = 0;
        foreach (var end in ends)
        {
            if (end < start || end >= points.Length)
                return null;
            if (!Contour(points, start, end, pen))
                return null;
            start = end + 1;
        }
        return pen.Path;
    }

    enum PointKind { On, Quad, Cubic }

    readonly record struct ContourPoint(int X, int Y, PointKind Kind)
    {
        public Vector2 F32 => new(X * (1f / 64f), Y * (1f / 64f));

        // FreeType's (and skrifa's) integer midpoint in 26.6, truncating toward zero.
        public ContourPoint Midpoint(ContourPoint other) => new(unchecked(X + other.X) / 2, unchecked(Y + other.Y) / 2, other.Kind);
    }

    static ContourPoint PointAt(FreeType.Outline* outline, int i)
    {
        var tag = outline->Tags[i] & 3;
        var kind = tag switch { 1 => PointKind.On, 2 => PointKind.Cubic, _ => PointKind.Quad };
        return new ContourPoint((int)(long)outline->Points[i].X.Value, (int)(long)outline->Points[i].Y.Value, kind);
    }

    /// <summary>skrifa's contour_to_path_freetype.</summary>
    static bool Contour(ContourPoint[] points, int start, int end, Pen pen)
    {
        var first = points[start];
        var last = points[end];
        if (first.Kind == PointKind.Cubic)
            return false;
        var omitLast = false;
        ContourPoint startPoint;
        var from = start;
        if (first.Kind == PointKind.Quad)
        {
            if (last.Kind == PointKind.On)
            {
                omitLast = true;
                startPoint = last;
            }
            else
                startPoint = last.Midpoint(first);
        }
        else
        {
            startPoint = first;
            from = start + 1;
        }
        pen.MoveTo(startPoint.F32);
        var state = new PendingState();
        var to = omitLast ? end - 1 : end;
        for (var i = from; i <= to; i++)
            if (!state.Emit(points[i], pen))
                return false;
        if (state.Kind != PendingState.State.Empty && !state.Emit(startPoint with { Kind = PointKind.On }, pen))
            return false;
        pen.Close();
        return true;
    }

    struct PendingState
    {
        public enum State { Empty, Quad, Cubic, TwoCubics }

        public State Kind;
        ContourPoint _a, _b;

        public bool Emit(ContourPoint point, Pen pen)
        {
            switch (Kind)
            {
                case State.Empty:
                    if (point.Kind == PointKind.Quad)
                        (Kind, _a) = (State.Quad, point);
                    else if (point.Kind == PointKind.Cubic)
                        (Kind, _a) = (State.Cubic, point);
                    else
                        pen.LineTo(point.F32);
                    return true;
                case State.Quad:
                    if (point.Kind == PointKind.Quad)
                    {
                        pen.QuadTo(_a.F32, _a.Midpoint(point).F32);
                        _a = point;
                    }
                    else if (point.Kind == PointKind.Cubic)
                        return false;
                    else
                    {
                        pen.QuadTo(_a.F32, point.F32);
                        Kind = State.Empty;
                    }
                    return true;
                case State.Cubic:
                    if (point.Kind != PointKind.Cubic)
                        return false;
                    (Kind, _b) = (State.TwoCubics, point);
                    return true;
                default:
                    if (point.Kind == PointKind.Quad)
                        return false;
                    if (point.Kind == PointKind.Cubic)
                    {
                        pen.CubicTo(_a.F32, _b.F32, _b.Midpoint(point).F32);
                        (Kind, _a) = (State.Cubic, point);
                    }
                    else
                    {
                        pen.CubicTo(_a.F32, _b.F32, point.F32);
                        Kind = State.Empty;
                    }
                    return true;
            }
        }
    }

    /// <summary>Skia's VerbsPointsPen: y flipped, degenerate segments dropped, a move only once something is drawn.</summary>
    sealed class Pen
    {
        public readonly SkiaPath Path = new();
        bool _started;
        Vector2 _current;

        static Vector2 Flip(Vector2 p) => new(p.X, -p.Y);

        void GoingTo(Vector2 point)
        {
            if (!_started)
            {
                _started = true;
                Path.Verbs.Add(PathVerb.Move);
                Path.Points.Add(_current);
            }
            _current = point;
        }

        public void MoveTo(Vector2 p)
        {
            if (_started)
            {
                Close();
                _started = false;
            }
            _current = Flip(p);
        }

        public void LineTo(Vector2 p)
        {
            var pt = Flip(p);
            if (_current != pt)
            {
                GoingTo(pt);
                Path.Verbs.Add(PathVerb.Line);
                Path.Points.Add(pt);
            }
        }

        public void QuadTo(Vector2 c, Vector2 p)
        {
            Vector2 pt0 = Flip(c), pt1 = Flip(p);
            if (_current != pt0 || _current != pt1)
            {
                GoingTo(pt1);
                Path.Verbs.Add(PathVerb.Quad);
                Path.Points.Add(pt0);
                Path.Points.Add(pt1);
            }
        }

        public void CubicTo(Vector2 c0, Vector2 c1, Vector2 p)
        {
            Vector2 pt0 = Flip(c0), pt1 = Flip(c1), pt2 = Flip(p);
            if (_current != pt0 || _current != pt1 || _current != pt2)
            {
                GoingTo(pt2);
                Path.Verbs.Add(PathVerb.Cubic);
                Path.Points.Add(pt0);
                Path.Points.Add(pt1);
                Path.Points.Add(pt2);
            }
        }

        public void Close()
        {
            if (Path.Verbs.Count > 0 && Path.Verbs[^1] is PathVerb.Quad or PathVerb.Cubic or PathVerb.Line or PathVerb.Move)
                Path.Verbs.Add(PathVerb.Close);
        }
    }
}

/// <summary>
/// SkTMaskGamma&lt;3,3,3&gt; with Chromium's Linux settings (SK_GAMMA_CONTRAST 0.2, SK_GAMMA_EXPONENT
/// 1.2, stored as 51/255 and 76/64): for each of eight colour buckets, the table that corrects a
/// glyph's coverage so blending in sRGB looks like blending at that gamma, with a little contrast.
/// </summary>
static class MaskGamma
{
    static byte[][]? _tables;

    public static byte[][] Tables => _tables ??= Build(51 / 255f, 76 / 64f);

    static byte[][] Build(float contrast, float gamma)
    {
        var tables = new byte[8][];
        for (var i = 0; i < 8; i++)
        {
            // sk_t_scale255<3>: the bucket spread over 0..255.
            var lum = (i << 5) | (i << 2) | (i >> 1);
            tables[i] = CorrectingLut(lum, contrast, gamma);
        }
        return tables;
    }

    static float ApplyContrast(float srca, float contrast) => srca + ((1.0f - srca) * contrast * srca);

    static int Round(float x) => (int)Math.Floor((double)x + 0.5);

    /// <summary>SkTMaskGamma_build_correcting_lut with SkGammaColorSpaceLuminance.</summary>
    static byte[] CorrectingLut(int srcI, float contrast, float gamma)
    {
        var table = new byte[256];
        var src = srcI / 255.0f;
        var linSrc = MathF.Pow(src, gamma);
        var dst = 1.0f - src;
        var linDst = MathF.Pow(dst, gamma);
        var adjustedContrast = contrast * linDst;
        if (MathF.Abs(src - dst) < 1.0f / 256.0f)
        {
            var ii = 0.0f;
            for (var i = 0; i < 256; i++, ii += 1.0f)
            {
                var srca = ApplyContrast(ii / 255.0f, adjustedContrast);
                table[i] = (byte)Round(255.0f * srca);
            }
            return table;
        }
        var inverse = 1 / gamma;
        var jj = 0.0f;
        for (var i = 0; i < 256; i++, jj += 1.0f)
        {
            var srca = ApplyContrast(jj / 255.0f, adjustedContrast);
            var dsta = 1.0f - srca;
            var linOut = linSrc * srca + dsta * linDst;
            var output = MathF.Pow(linOut, inverse);
            var result = (output - dst) / (src - dst);
            table[i] = (byte)Round(255.0f * result);
        }
        return table;
    }
}
