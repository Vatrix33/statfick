namespace Statventure.Text.Skia;

/// <summary>The blitter calls the analytic scan converter makes on its destination (SkBlitter's).</summary>
interface IBlitter
{
    void BlitH(int x, int y, int width);
    void BlitAntiH(int x, int y, byte[] alphas, int alphaStart, short[] runs, int runStart);
    void BlitV(int x, int y, int height, byte alpha);
    void BlitRect(int x, int y, int width, int height);
    void BlitAntiRect(int x, int y, int width, int height, byte leftAlpha, byte rightAlpha);
    void BlitAntiH2(int x, int y, byte a0, byte a1);
}

/// <summary>
/// An 8-bit coverage image drawn into as SkA8_Blitter draws with an opaque source over
/// (src-over; what Skia's glyph-from-path code draws with).
/// </summary>
sealed class A8Target(int width, int height) : IBlitter
{
    public readonly int Width = width;
    public readonly int Height = height;
    public readonly byte[] Pixels = new byte[width * height];

    static byte Div255(int product) => (byte)((product + 128) * 257 >> 16);

    // src-over with an opaque source scaled by the coverage: aa + (255 - aa) * dst / 255.
    static byte Over(byte aa, byte dst) => (byte)(aa + Div255((255 - aa) * dst));

    void Blend(int index, byte aa)
    {
        if (aa == 0xFF)
            Pixels[index] = 0xFF;
        else if (aa != 0)
            Pixels[index] = Over(Div255(255 * aa), Pixels[index]);
    }

    public void BlitH(int x, int y, int width) => Pixels.AsSpan(y * Width + x, width).Fill(0xFF);

    public void BlitAntiH(int x, int y, byte[] alphas, int alphaStart, short[] runs, int runStart)
    {
        var index = y * Width + x;
        for (;;)
        {
            int count = runs[runStart];
            if (count == 0)
                return;
            var aa = alphas[alphaStart];
            if (aa != 0)
                for (var i = 0; i < count; i++)
                    Blend(index + i, aa);
            runStart += count;
            alphaStart += count;
            index += count;
        }
    }

    public void BlitV(int x, int y, int height, byte alpha)
    {
        for (var i = 0; i < height; i++)
            Blend((y + i) * Width + x, alpha);
    }

    public void BlitRect(int x, int y, int width, int height)
    {
        for (var i = 0; i < height; i++)
            BlitH(x, y + i, width);
    }

    public void BlitAntiRect(int x, int y, int width, int height, byte leftAlpha, byte rightAlpha)
    {
        if (leftAlpha > 0)
            BlitV(x, y, height, leftAlpha);
        x++;
        if (width > 0)
        {
            BlitRect(x, y, width, height);
            x += width;
        }
        if (rightAlpha > 0)
            BlitV(x, y, height, rightAlpha);
    }

    public void BlitAntiH2(int x, int y, byte a0, byte a1) =>
        BlitAntiH(x, y, [a0, a1], 0, [1, 1, 0], 0);

    /// <summary>SkA8_Blitter::blitMask: lerp toward the source by the mask's coverage.</summary>
    public void BlitMask(byte[] mask, int maskOrigin, int rowBytes, int left, int top, int width, int height)
    {
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var m = mask[maskOrigin + y * rowBytes + x];
                var index = (top + y) * Width + left + x;
                var dst = Pixels[index];
                Pixels[index] = Div255((255 - m) * dst + m * 255);
            }
    }

    static byte ScalarToAlpha(float a)
    {
        var alpha = (byte)(int)(a * 255);
        return alpha > 247 ? (byte)0xFF : alpha < 8 ? (byte)0 : alpha;
    }

    /// <summary>SkBlitter::blitFatAntiRect: a rectangle at least three pixels wide.</summary>
    public void BlitFatAntiRect(SkRect rect)
    {
        var (left, top, right, bottom) = rect.RoundOut();
        var width = right - left;
        var height = bottom - top;
        if (height == 0)
            return;
        var runs = new short[width + 1];
        var alphas = new byte[width + 1];
        runs[0] = 1;
        runs[1] = (short)(width - 2);
        runs[width - 1] = 1;
        runs[width] = 0;
        var partialL = left + 1 - rect.Left;
        var partialR = rect.Right - (right - 1);
        var partialT = top + 1 - rect.Top;
        var partialB = rect.Bottom - (bottom - 1);
        if (height == 1)
            partialT = rect.Bottom - rect.Top;
        alphas[0] = ScalarToAlpha(partialL * partialT);
        alphas[1] = ScalarToAlpha(partialT);
        alphas[width - 1] = ScalarToAlpha(partialR * partialT);
        BlitAntiH(left, top, alphas, 0, runs, 0);
        if (height > 2)
            BlitAntiRect(left, top + 1, width - 2, height - 2, ScalarToAlpha(partialL), ScalarToAlpha(partialR));
        if (height > 1)
        {
            alphas[0] = ScalarToAlpha(partialL * partialB);
            alphas[1] = ScalarToAlpha(partialB);
            alphas[width - 1] = ScalarToAlpha(partialR * partialB);
            BlitAntiH(left, bottom - 1, alphas, 0, runs, 0);
        }
    }
}

/// <summary>SkScan_AAAPath's AdditiveBlitter: coverage that adds up before it reaches the real blitter.</summary>
abstract class AdditiveBlitter : IBlitter
{
    public abstract IBlitter RealBlitter { get; }
    public abstract void BlitAntiH(int x, int y, byte[] alphas, int start, int len);
    public abstract void BlitAntiH(int x, int y, byte alpha);
    public abstract void BlitAntiH(int x, int y, int width, byte alpha);
    public abstract void FlushIfYChanged(int y, int nextY);

    // As the real blitter these are SkDEBUGFAILs in Skia: nothing happens in a release build.
    public virtual void BlitH(int x, int y, int width) { }
    public void BlitAntiH(int x, int y, byte[] alphas, int alphaStart, short[] runs, int runStart) { }
    public virtual void BlitV(int x, int y, int height, byte alpha) { }
    public virtual void BlitRect(int x, int y, int width, int height) { }
    public virtual void BlitAntiRect(int x, int y, int width, int height, byte leftAlpha, byte rightAlpha) { }
    public void BlitAntiH2(int x, int y, byte a0, byte a1) { }

    public static byte CatchOverflow(int alpha) => (byte)(alpha - (alpha >> 8));
    public static void AddAlpha(byte[] buffer, int index, int delta) => buffer[index] = CatchOverflow(buffer[index] + delta);
    public static void SafelyAddAlpha(byte[] buffer, int index, int delta) => buffer[index] = (byte)Math.Min(0xFF, buffer[index] + delta);
}

/// <summary>
/// MaskAdditiveBlitter: coverage summed in a small mask (at most 32 wide and 1024 bytes) that
/// goes to the real blitter at the end. Its rows are packed with one spare byte at each end, so a
/// write just past a row lands in the next one, as in Skia.
/// </summary>
sealed class MaskAdditiveBlitter : AdditiveBlitter
{
    const int MaxWidth = 32;
    const int MaxStorage = 1024;

    readonly A8Target _real;
    readonly int _left, _top, _width, _height;
    public readonly byte[] Storage;

    public MaskAdditiveBlitter(A8Target real, int left, int top, int right, int bottom)
    {
        _real = real;
        _left = left;
        _top = top;
        _width = right - left;
        _height = bottom - top;
        // Skia's fStorage: (kMAX_STORAGE >> 2) + 2 words; the mask starts one byte in.
        Storage = new byte[MaxStorage + 8];
    }

    public static bool CanHandleRect(int left, int top, int right, int bottom)
    {
        var width = right - left;
        if (width > MaxWidth)
            return false;
        long rowBytes = (width + 3) & ~3;
        return rowBytes * (bottom - top) <= MaxStorage;
    }

    public override IBlitter RealBlitter => this;

    /// <summary>The index in <see cref="Storage"/> of (0, y): add x to reach a pixel.</summary>
    public int Row(int y) => 1 + (y - _top) * _width - _left;

    public override void BlitAntiH(int x, int y, byte[] alphas, int start, int len) =>
        throw new InvalidOperationException("Don't use this; directly add alphas to the mask.");

    public override void BlitAntiH(int x, int y, byte alpha) => AddAlpha(Storage, Row(y) + x, alpha);

    public override void BlitAntiH(int x, int y, int width, byte alpha)
    {
        var row = Row(y);
        for (var i = 0; i < width; i++)
            AddAlpha(Storage, row + x + i, alpha);
    }

    public override void BlitV(int x, int y, int height, byte alpha)
    {
        if (alpha == 0)
            return;
        var row = Row(y);
        for (var i = 0; i < height; i++)
        {
            Storage[row + x] = alpha;
            row += _width;
        }
    }

    public override void BlitRect(int x, int y, int width, int height)
    {
        var row = Row(y);
        for (var i = 0; i < height; i++)
        {
            Storage.AsSpan(row + x, width).Fill(0xFF);
            row += _width;
        }
    }

    public override void BlitAntiRect(int x, int y, int width, int height, byte leftAlpha, byte rightAlpha)
    {
        BlitV(x, y, height, leftAlpha);
        BlitV(x + 1 + width, y, height, rightAlpha);
        BlitRect(x + 1, y, width, height);
    }

    public override void FlushIfYChanged(int y, int nextY) { }

    /// <summary>The destructor's blitMask.</summary>
    public void Finish() => _real.BlitMask(Storage, 1, _width, _left, _top, _width, _height);
}

/// <summary>RunBasedAdditiveBlitter (and SafeRLEAdditiveBlitter, which clamps instead of wrapping): a row of SkAlphaRuns at a time.</summary>
sealed class RunBasedAdditiveBlitter : AdditiveBlitter
{
    readonly IBlitter _real;
    readonly bool _safe;
    readonly int _left, _width, _top;
    int _currY;
    readonly short[] _runs;
    readonly byte[] _alpha;
    int _offsetX;

    public RunBasedAdditiveBlitter(IBlitter real, int left, int top, int right, bool safe)
    {
        _real = real;
        _safe = safe;
        _left = left;
        _width = right - left;
        _top = top;
        _currY = top - 1;
        _runs = new short[_width + 1];
        _alpha = new byte[_width + 1];
        AdvanceRuns();
        _offsetX = 0;
    }

    public override IBlitter RealBlitter => _real;

    void AdvanceRuns()
    {
        _runs[0] = (short)_width;
        _runs[_width] = 0;
        _alpha[0] = 0;
    }

    bool Empty => _alpha[0] == 0 && _runs[_runs[0]] == 0;

    bool Check(int x, int width) => x >= 0 && x + width <= _width;

    static byte SnapAlpha(byte alpha) => alpha > 247 ? (byte)0xFF : alpha < 8 ? (byte)0 : alpha;

    public void Flush()
    {
        if (_currY >= _top)
        {
            for (var x = 0; _runs[x] != 0; x += _runs[x])
                _alpha[x] = SnapAlpha(_alpha[x]);
            if (!Empty)
            {
                _real.BlitAntiH(_left, _currY, _alpha, 0, _runs, 0);
                AdvanceRuns();
                _offsetX = 0;
            }
            _currY = _top - 1;
        }
    }

    void CheckY(int y)
    {
        if (y != _currY)
        {
            Flush();
            _currY = y;
        }
    }

    public override void FlushIfYChanged(int y, int nextY)
    {
        if (Fixed.FloorToInt(y) != Fixed.FloorToInt(nextY))
            Flush();
    }

    public override void BlitAntiH(int x, int y, byte[] antialias, int start, int len)
    {
        CheckY(y);
        x -= _left;
        if (x < 0)
        {
            len += x;
            start -= x;
            x = 0;
        }
        len = Math.Min(len, _width - x);
        if (x < _offsetX)
            _offsetX = 0;
        _offsetX = Add(x, 0, len, 0, 0, _offsetX);
        for (var i = 0; i < len; i += _runs[x + i])
        {
            for (var j = 1; j < _runs[x + i]; j++)
            {
                _runs[x + i + j] = 1;
                _alpha[x + i + j] = _alpha[x + i];
            }
            _runs[x + i] = 1;
        }
        for (var i = 0; i < len; i++)
        {
            if (_safe)
                SafelyAddAlpha(_alpha, x + i, antialias[start + i]);
            else
                AddAlpha(_alpha, x + i, antialias[start + i]);
        }
    }

    public override void BlitAntiH(int x, int y, byte alpha)
    {
        CheckY(y);
        x -= _left;
        if (x < _offsetX)
            _offsetX = 0;
        if (!Check(x, 1))
            return;
        if (_safe)
        {
            _offsetX = Add(x, 0, 1, 0, 0, _offsetX);
            SafelyAddAlpha(_alpha, x, alpha);
        }
        else
            _offsetX = Add(x, 0, 1, 0, alpha, _offsetX);
    }

    public override void BlitAntiH(int x, int y, int width, byte alpha)
    {
        CheckY(y);
        x -= _left;
        if (x < _offsetX)
            _offsetX = 0;
        if (!Check(x, width))
            return;
        if (_safe)
        {
            _offsetX = Add(x, 0, width, 0, 0, _offsetX);
            for (var i = x; i < x + width; i += _runs[i])
                SafelyAddAlpha(_alpha, i, alpha);
        }
        else
            _offsetX = Add(x, 0, width, 0, alpha, _offsetX);
    }

    /// <summary>SkAlphaRuns::add.</summary>
    int Add(int x, int startAlpha, int middleCount, int stopAlpha, int maxValue, int offsetX)
    {
        var runs = offsetX;
        var alpha = offsetX;
        var lastAlpha = alpha;
        x -= offsetX;
        if (startAlpha != 0)
        {
            Break(runs, alpha, x, 1);
            var tmp = _alpha[alpha + x] + startAlpha;
            _alpha[alpha + x] = (byte)(tmp - (tmp >> 8));
            runs += x + 1;
            alpha += x + 1;
            x = 0;
        }
        if (middleCount != 0)
        {
            Break(runs, alpha, x, middleCount);
            alpha += x;
            runs += x;
            x = 0;
            do
            {
                _alpha[alpha] = CatchOverflow(_alpha[alpha] + maxValue);
                int n = _runs[runs];
                alpha += n;
                runs += n;
                middleCount -= n;
            } while (middleCount > 0);
            lastAlpha = alpha;
        }
        if (stopAlpha != 0)
        {
            Break(runs, alpha, x, 1);
            alpha += x;
            _alpha[alpha] = (byte)(_alpha[alpha] + stopAlpha);
            lastAlpha = alpha;
        }
        return lastAlpha;
    }

    /// <summary>SkAlphaRuns::Break: split the runs at x and x + count.</summary>
    void Break(int runs, int alpha, int x, int count)
    {
        var nextRuns = runs + x;
        var nextAlpha = alpha + x;
        while (x > 0)
        {
            int n = _runs[runs];
            if (x < n)
            {
                _alpha[alpha + x] = _alpha[alpha];
                _runs[runs] = (short)x;
                _runs[runs + x] = (short)(n - x);
                break;
            }
            runs += n;
            alpha += n;
            x -= n;
        }
        runs = nextRuns;
        alpha = nextAlpha;
        x = count;
        for (;;)
        {
            int n = _runs[runs];
            if (x < n)
            {
                _alpha[alpha + x] = _alpha[alpha];
                _runs[runs] = (short)x;
                _runs[runs + x] = (short)(n - x);
                break;
            }
            x -= n;
            if (x <= 0)
                break;
            runs += n;
            alpha += n;
        }
    }
}

/// <summary>
/// Skia's analytic anti-aliasing scan converter (SkScan_AAAPath.cpp) for a non-inverse path that
/// lies inside its clip: the coverage of each pixel is the area the path covers, computed from
/// trapezoids between scan lines at whole pixels, edge ends and edge crossings, with y snapped to
/// quarter pixels and 8-bit alpha arithmetic exactly as Skia's.
/// </summary>
static class AnalyticScan
{
    /// <summary>SkScan::AntiFillPath into <paramref name="target"/>, whose bounds contain the path's.</summary>
    public static void FillPath(SkiaPath path, A8Target target)
    {
        var bounds = path.Bounds;
        var (left, top, right, bottom) = bounds.RoundOut();
        if (left >= right || top >= bottom)
            return;
        // The clip is the whole target.
        left = Math.Max(left, 0);
        top = Math.Max(top, 0);
        right = Math.Min(right, target.Width);
        bottom = Math.Min(bottom, target.Height);
        if (left >= right || top >= bottom)
            return;
        if (MaskAdditiveBlitter.CanHandleRect(left, top, right, bottom))
        {
            if (TryBlitFatAntiRect(target, path))
                return;
            var mask = new MaskAdditiveBlitter(target, left, top, right, bottom);
            FillPath(path, target, mask, top, bottom, true);
            mask.Finish();
        }
        else
        {
            var convex = path.IsConvex;
            var blitter = new RunBasedAdditiveBlitter(target, left, top, right, safe: !convex);
            FillPath(path, target, blitter, top, bottom, false);
            blitter.Flush();
        }
    }

    static bool TryBlitFatAntiRect(A8Target target, SkiaPath path)
    {
        if (path.AsRect() is not { } rect)
            return false;
        var clipped = new SkRect(MathF.Max(rect.Left, 0), MathF.Max(rect.Top, 0), MathF.Min(rect.Right, target.Width), MathF.Min(rect.Bottom, target.Height));
        if (clipped.IsEmpty)
            return true;
        var (left, _, right, _) = clipped.RoundOut();
        if (right - left < 3)
            return false;
        target.BlitFatAntiRect(clipped);
        return true;
    }

    static void FillPath(SkiaPath path, A8Target target, AdditiveBlitter blitter, int startY, int stopY, bool isUsingMask)
    {
        var list = AnalyticEdgeBuilder.Build(path);
        if (list.Count == 0)
            return;
        AnalyticEdgeBuilder.Sort(list);
        for (var i = 1; i < list.Count; i++)
        {
            list[i - 1].Next = list[i];
            list[i].Prev = list[i - 1];
        }
        var head = new AnalyticEdge
        {
            Next = list[0],
            UpperY = Fixed.MinS32,
            LowerY = Fixed.MinS32,
            X = Fixed.MinS32,
            DX = 0,
            DY = Fixed.MaxS32,
            UpperX = Fixed.MinS32,
        };
        list[0].Prev = head;
        var tail = new AnalyticEdge
        {
            Prev = list[^1],
            UpperY = Fixed.MaxS32,
            LowerY = Fixed.MaxS32,
            X = Fixed.MaxS32,
            DX = 0,
            DY = Fixed.MaxS32,
            UpperX = Fixed.MaxS32,
        };
        list[^1].Next = tail;

        var leftBound = Fixed.FromInt(0);
        var rightBound = Fixed.FromInt(target.Width);
        if (isUsingMask)
        {
            var (l, _, r, _) = path.Bounds.RoundOut();
            leftBound = Math.Max(leftBound, Fixed.FromInt(l));
            rightBound = Math.Min(rightBound, Fixed.FromInt(r));
        }
        if (path.IsConvex && list.Count >= 2)
            WalkConvexEdges(head, blitter, startY, stopY, leftBound, rightBound, isUsingMask);
        else
        {
            var skipIntersect = path.Points.Count > (stopY - startY) * 2;
            WalkEdges(head, tail, blitter, startY, stopY, leftBound, rightBound, isUsingMask, skipIntersect);
        }
    }

    // Alpha arithmetic.

    static byte TrapezoidToAlpha(int l1, int l2) => (byte)(((l1 + l2) / 2) >> 8);

    static byte PartialTriangleToAlpha(int a, int b)
    {
        var area = unchecked((a >> 11) * (a >> 11) * (b >> 11));
        return (byte)((area >> 8) & 0xFF);
    }

    static byte PartialAlpha(byte alpha, int partialHeight) => (byte)Fixed.RoundToInt(unchecked(alpha * partialHeight));

    static byte PartialAlphaOf(byte alpha, byte fullAlpha) => (byte)((alpha * fullAlpha) >> 8);

    static byte FixedToAlpha(int f) => PartialAlpha(0xFF, f);

    static int ApproximateIntersection(int l1, int r1, int l2, int r2)
    {
        if (l1 > r1)
            (l1, r1) = (r1, l1);
        if (l2 > r2)
            (l2, r2) = (r2, l2);
        return (Math.Max(l1, l2) + Math.Min(r1, r2)) / 2;
    }

    static void ComputeAlphaAboveLine(byte[] alphas, int start, int l, int r, int dY, byte fullAlpha)
    {
        var R = Fixed.CeilToInt(r);
        if (R == 0)
            return;
        if (R == 1)
        {
            alphas[start] = PartialAlphaOf((byte)(((R << 17) - l - r) >> 9), fullAlpha);
            return;
        }
        var first = Fixed.One - l;
        var last = r - ((R - 1) << 16);
        var firstH = Fixed.Mul(first, dY);
        alphas[start] = (byte)(Fixed.Mul(first, firstH) >> 9);
        var alpha16 = Fixed.SatAdd(firstH, dY >> 1);
        for (var i = 1; i < R - 1; i++)
        {
            alphas[start + i] = (byte)(alpha16 >> 8);
            alpha16 = Fixed.SatAdd(alpha16, dY);
        }
        alphas[start + R - 1] = (byte)(fullAlpha - PartialTriangleToAlpha(last, dY));
    }

    static void ComputeAlphaBelowLine(byte[] alphas, int start, int l, int r, int dY, byte fullAlpha)
    {
        var R = Fixed.CeilToInt(r);
        if (R == 0)
            return;
        if (R == 1)
        {
            alphas[start] = PartialAlphaOf(TrapezoidToAlpha(l, r), fullAlpha);
            return;
        }
        var first = Fixed.One - l;
        var last = r - ((R - 1) << 16);
        var lastH = Fixed.Mul(last, dY);
        alphas[start + R - 1] = (byte)(Fixed.Mul(last, lastH) >> 9);
        var alpha16 = Fixed.SatAdd(lastH, dY >> 1);
        for (var i = R - 2; i > 0; i--)
        {
            alphas[start + i] = (byte)((alpha16 >> 8) & 0xFF);
            alpha16 = Fixed.SatAdd(alpha16, dY);
        }
        alphas[start] = (byte)(fullAlpha - PartialTriangleToAlpha(first, dY));
    }

    // Mask rows are an index into the mask blitter's storage (x added), or NoMask without a mask.
    const int NoMask = int.MinValue;

    static void BlitSingleAlpha(AdditiveBlitter blitter, int y, int x, byte alpha, byte fullAlpha, int maskRow, bool noRealBlitter)
    {
        if (maskRow != NoMask)
        {
            var storage = ((MaskAdditiveBlitter)blitter).Storage;
            if (fullAlpha == 0xFF && !noRealBlitter)
                storage[maskRow + x] = alpha;
            else
                AdditiveBlitter.SafelyAddAlpha(storage, maskRow + x, PartialAlphaOf(alpha, fullAlpha));
        }
        else if (fullAlpha == 0xFF && !noRealBlitter)
            blitter.RealBlitter.BlitV(x, y, 1, alpha);
        else
            blitter.BlitAntiH(x, y, PartialAlphaOf(alpha, fullAlpha));
    }

    static void BlitTwoAlphas(AdditiveBlitter blitter, int y, int x, byte a1, byte a2, byte fullAlpha, int maskRow, bool noRealBlitter)
    {
        if (maskRow != NoMask)
        {
            var storage = ((MaskAdditiveBlitter)blitter).Storage;
            AdditiveBlitter.SafelyAddAlpha(storage, maskRow + x, a1);
            AdditiveBlitter.SafelyAddAlpha(storage, maskRow + x + 1, a2);
        }
        else if (fullAlpha == 0xFF && !noRealBlitter)
            blitter.RealBlitter.BlitAntiH2(x, y, a1, a2);
        else
        {
            blitter.BlitAntiH(x, y, a1);
            blitter.BlitAntiH(x + 1, y, a2);
        }
    }

    static void BlitFullAlpha(AdditiveBlitter blitter, int y, int x, int len, byte fullAlpha, int maskRow, bool noRealBlitter)
    {
        if (maskRow != NoMask)
        {
            var storage = ((MaskAdditiveBlitter)blitter).Storage;
            for (var i = 0; i < len; i++)
                AdditiveBlitter.SafelyAddAlpha(storage, maskRow + x + i, fullAlpha);
        }
        else if (fullAlpha == 0xFF && !noRealBlitter)
            blitter.RealBlitter.BlitH(x, y, len);
        else
            blitter.BlitAntiH(x, y, len, fullAlpha);
    }

    static void BlitAaaTrapezoidRow(AdditiveBlitter blitter, int y, int ul, int ur, int ll, int lr, int lDY, int rDY, byte fullAlpha, int maskRow, bool noRealBlitter)
    {
        int L = Fixed.FloorToInt(ul), R = Fixed.CeilToInt(lr);
        var len = R - L;
        if (len == 1)
        {
            var alpha = TrapezoidToAlpha(ur - ul, lr - ll);
            BlitSingleAlpha(blitter, y, L, alpha, fullAlpha, maskRow, noRealBlitter);
            return;
        }
        var alphas = new byte[len + 1];
        var tempAlphas = new byte[len + 2];
        var runs = new short[len + 1];
        for (var i = 0; i < len; i++)
        {
            runs[i] = 1;
            alphas[i] = fullAlpha;
        }
        runs[len] = 0;

        var uL = Fixed.FloorToInt(ul);
        var lL = Fixed.CeilToInt(ll);
        if (uL + 2 == lL)
        {
            var first = Fixed.FromInt(uL) + Fixed.One - ul;
            var second = ll - ul - first;
            var a1 = (byte)(fullAlpha - PartialTriangleToAlpha(first, lDY));
            var a2 = PartialTriangleToAlpha(second, lDY);
            alphas[0] = alphas[0] > a1 ? (byte)(alphas[0] - a1) : (byte)0;
            alphas[1] = alphas[1] > a2 ? (byte)(alphas[1] - a2) : (byte)0;
        }
        else
        {
            ComputeAlphaBelowLine(tempAlphas, uL - L, ul - Fixed.FromInt(uL), ll - Fixed.FromInt(uL), lDY, fullAlpha);
            for (var i = uL; i < lL; i++)
                alphas[i - L] = alphas[i - L] > tempAlphas[i - L] ? (byte)(alphas[i - L] - tempAlphas[i - L]) : (byte)0;
        }

        var uR = Fixed.FloorToInt(ur);
        var lR = Fixed.CeilToInt(lr);
        if (uR + 2 == lR)
        {
            var first = Fixed.FromInt(uR) + Fixed.One - ur;
            var second = lr - ur - first;
            var a1 = PartialTriangleToAlpha(first, rDY);
            var a2 = (byte)(fullAlpha - PartialTriangleToAlpha(second, rDY));
            alphas[len - 2] = alphas[len - 2] > a1 ? (byte)(alphas[len - 2] - a1) : (byte)0;
            alphas[len - 1] = alphas[len - 1] > a2 ? (byte)(alphas[len - 1] - a2) : (byte)0;
        }
        else
        {
            ComputeAlphaAboveLine(tempAlphas, uR - L, ur - Fixed.FromInt(uR), lr - Fixed.FromInt(uR), rDY, fullAlpha);
            for (var i = uR; i < lR; i++)
                alphas[i - L] = alphas[i - L] > tempAlphas[i - L] ? (byte)(alphas[i - L] - tempAlphas[i - L]) : (byte)0;
        }

        if (maskRow != NoMask)
        {
            var storage = ((MaskAdditiveBlitter)blitter).Storage;
            for (var i = 0; i < len; i++)
                AdditiveBlitter.SafelyAddAlpha(storage, maskRow + L + i, alphas[i]);
        }
        else if (fullAlpha == 0xFF && !noRealBlitter)
            blitter.RealBlitter.BlitAntiH(L, y, alphas, 0, runs, 0);
        else
            blitter.BlitAntiH(L, y, alphas, 0, len);
    }

    static void BlitTrapezoidRow(AdditiveBlitter blitter, int y, int ul, int ur, int ll, int lr, int lDY, int rDY, byte fullAlpha, int maskRow, bool noRealBlitter)
    {
        if (ul > ur)
            return;
        if (ll > lr)
            ll = lr = ApproximateIntersection(ul, ll, ur, lr);
        if (ul == ur && ll == lr)
            return;
        if (ul > ll)
            (ul, ll) = (ll, ul);
        if (ur > lr)
            (ur, lr) = (lr, ur);
        var joinLeft = Fixed.CeilToFixed(ll);
        var joinRite = Fixed.FloorToFixed(ur);
        if (joinLeft <= joinRite)
        {
            if (ul < joinLeft)
            {
                var len = Fixed.CeilToInt(joinLeft - ul);
                if (len == 1)
                {
                    var alpha = TrapezoidToAlpha(joinLeft - ul, joinLeft - ll);
                    BlitSingleAlpha(blitter, y, ul >> 16, alpha, fullAlpha, maskRow, noRealBlitter);
                }
                else if (len == 2)
                {
                    var first = joinLeft - Fixed.One - ul;
                    var second = ll - ul - first;
                    var a1 = PartialTriangleToAlpha(first, lDY);
                    var a2 = (byte)(fullAlpha - PartialTriangleToAlpha(second, lDY));
                    BlitTwoAlphas(blitter, y, ul >> 16, a1, a2, fullAlpha, maskRow, noRealBlitter);
                }
                else
                    BlitAaaTrapezoidRow(blitter, y, ul, joinLeft, ll, joinLeft, lDY, Fixed.MaxS32, fullAlpha, maskRow, noRealBlitter);
            }
            if (joinLeft < joinRite)
                BlitFullAlpha(blitter, y, Fixed.FloorToInt(joinLeft), Fixed.FloorToInt(joinRite - joinLeft), fullAlpha, maskRow, noRealBlitter);
            if (lr > joinRite)
            {
                var len = Fixed.CeilToInt(lr - joinRite);
                if (len == 1)
                {
                    var alpha = TrapezoidToAlpha(ur - joinRite, lr - joinRite);
                    BlitSingleAlpha(blitter, y, joinRite >> 16, alpha, fullAlpha, maskRow, noRealBlitter);
                }
                else if (len == 2)
                {
                    var first = joinRite + Fixed.One - ur;
                    var second = lr - ur - first;
                    var a1 = (byte)(fullAlpha - PartialTriangleToAlpha(first, rDY));
                    var a2 = PartialTriangleToAlpha(second, rDY);
                    BlitTwoAlphas(blitter, y, joinRite >> 16, a1, a2, fullAlpha, maskRow, noRealBlitter);
                }
                else
                    BlitAaaTrapezoidRow(blitter, y, joinRite, ur, joinRite, lr, Fixed.MaxS32, rDY, fullAlpha, maskRow, noRealBlitter);
            }
        }
        else
            BlitAaaTrapezoidRow(blitter, y, ul, ur, ll, lr, lDY, rDY, fullAlpha, maskRow, noRealBlitter);
    }

    static bool IsSmoothEnough(AnalyticEdge thisEdge, AnalyticEdge nextEdge)
    {
        if (thisEdge.CurveCount < 0)
        {
            int ddshift = thisEdge.CurveShift;
            return Fixed.Abs(thisEdge.CDx) >> 1 >= Fixed.Abs(thisEdge.CDDx) >> ddshift
                && Fixed.Abs(thisEdge.CDy) >> 1 >= Fixed.Abs(thisEdge.CDDy) >> ddshift
                && (thisEdge.CDy - (thisEdge.CDDy >> ddshift)) >> thisEdge.ToFixedShift >= Fixed.One;
        }
        if (thisEdge.CurveCount > 0)
            return Fixed.Abs(thisEdge.QDx) >> 1 >= Fixed.Abs(thisEdge.QDDx)
                && Fixed.Abs(thisEdge.QDy) >> 1 >= Fixed.Abs(thisEdge.QDDy)
                && (thisEdge.QDy - thisEdge.QDDy) >> thisEdge.CurveShift >= Fixed.One;
        return Fixed.Abs(Fixed.SatSub(nextEdge.DX, thisEdge.DX)) <= Fixed.One
            && nextEdge.LowerY - nextEdge.UpperY >= Fixed.One;
    }

    static bool IsSmoothEnough(AnalyticEdge leftE, AnalyticEdge riteE, AnalyticEdge currE, int stopY)
    {
        if (currE.UpperY >= Fixed.LeftShift(stopY, 16))
            return false;
        if (leftE.LowerY + Fixed.One < riteE.LowerY)
            return IsSmoothEnough(leftE, currE);
        if (leftE.LowerY > riteE.LowerY + Fixed.One)
            return IsSmoothEnough(riteE, currE);
        var nextCurrE = currE.Next!;
        if (nextCurrE.UpperY >= stopY << 16)
            return false;
        if (nextCurrE.UpperX < currE.UpperX)
            (currE, nextCurrE) = (nextCurrE, currE);
        return IsSmoothEnough(leftE, currE) && IsSmoothEnough(riteE, nextCurrE);
    }

    static void WalkConvexEdges(AnalyticEdge prevHead, AdditiveBlitter blitter, int startY, int stopY, int leftBound, int riteBound, bool isUsingMask)
    {
        var leftE = prevHead.Next!;
        var riteE = leftE.Next!;
        var currE = riteE.Next!;
        var y = Math.Max(leftE.UpperY, riteE.UpperY);
        var mask = blitter as MaskAdditiveBlitter;
        for (;;)
        {
            while (leftE.LowerY <= y)
            {
                if (!leftE.Update(y))
                {
                    if (Fixed.FloorToInt(currE.UpperY) >= stopY)
                        return;
                    leftE = currE;
                    currE = currE.Next!;
                }
            }
            while (riteE.LowerY <= y)
            {
                if (!riteE.Update(y))
                {
                    if (Fixed.FloorToInt(currE.UpperY) >= stopY)
                        return;
                    riteE = currE;
                    currE = currE.Next!;
                }
            }
            if (Fixed.FloorToInt(y) >= stopY)
                break;
            leftE.GoY(y);
            riteE.GoY(y);
            if (leftE.X > riteE.X || (leftE.X == riteE.X && leftE.DX > riteE.DX))
                (leftE, riteE) = (riteE, leftE);
            var localBotFixed = Math.Min(leftE.LowerY, riteE.LowerY);
            if (IsSmoothEnough(leftE, riteE, currE, stopY))
                localBotFixed = Fixed.CeilToFixed(localBotFixed);
            localBotFixed = Math.Min(localBotFixed, Fixed.FromInt(stopY));
            var left = Math.Max(leftBound, leftE.X);
            var dLeft = leftE.DX;
            var rite = Math.Min(riteBound, riteE.X);
            var dRite = riteE.DX;
            if ((dLeft | dRite) == 0)
            {
                var fullLeft = Fixed.CeilToInt(left);
                var fullRite = Fixed.FloorToInt(rite);
                var partialLeft = Fixed.FromInt(fullLeft) - left;
                var partialRite = rite - Fixed.FromInt(fullRite);
                var fullTop = Fixed.CeilToInt(y);
                var fullBot = Fixed.FloorToInt(localBotFixed);
                var partialTop = Fixed.FromInt(fullTop) - y;
                var partialBot = localBotFixed - Fixed.FromInt(fullBot);
                if (fullTop > fullBot)
                {
                    partialTop -= Fixed.One - partialBot;
                    partialBot = 0;
                }
                if (fullRite >= fullLeft)
                {
                    if (partialTop > 0)
                    {
                        if (partialLeft > 0)
                            blitter.BlitAntiH(fullLeft - 1, fullTop - 1, FixedToAlpha(Fixed.Mul(partialTop, partialLeft)));
                        blitter.BlitAntiH(fullLeft, fullTop - 1, fullRite - fullLeft, FixedToAlpha(partialTop));
                        if (partialRite > 0)
                            blitter.BlitAntiH(fullRite, fullTop - 1, FixedToAlpha(Fixed.Mul(partialTop, partialRite)));
                        blitter.FlushIfYChanged(y, y + partialTop);
                    }
                    if (fullBot > fullTop && (fullRite > fullLeft || FixedToAlpha(partialLeft) > 0 || FixedToAlpha(partialRite) > 0))
                        blitter.RealBlitter.BlitAntiRect(fullLeft - 1, fullTop, fullRite - fullLeft, fullBot - fullTop, FixedToAlpha(partialLeft), FixedToAlpha(partialRite));
                    if (partialBot > 0)
                    {
                        if (partialLeft > 0)
                            blitter.BlitAntiH(fullLeft - 1, fullBot, FixedToAlpha(Fixed.Mul(partialBot, partialLeft)));
                        blitter.BlitAntiH(fullLeft, fullBot, fullRite - fullLeft, FixedToAlpha(partialBot));
                        if (partialRite > 0)
                            blitter.BlitAntiH(fullRite, fullBot, FixedToAlpha(Fixed.Mul(partialBot, partialRite)));
                    }
                }
                else
                {
                    var width = rite - left;
                    if (width > 0)
                    {
                        if (partialTop > 0)
                        {
                            blitter.BlitAntiH(fullLeft - 1, fullTop - 1, 1, FixedToAlpha(Fixed.Mul(partialTop, width)));
                            blitter.FlushIfYChanged(y, y + partialTop);
                        }
                        if (fullBot > fullTop)
                            blitter.RealBlitter.BlitV(fullLeft - 1, fullTop, fullBot - fullTop, FixedToAlpha(width));
                        if (partialBot > 0)
                            blitter.BlitAntiH(fullLeft - 1, fullBot, 1, FixedToAlpha(Fixed.Mul(partialBot, width)));
                    }
                }
                y = localBotFixed;
            }
            else
            {
                const int snapDigit = Fixed.One >> 4;
                const int snapHalf = snapDigit >> 1;
                const int snapMask = -1 ^ (snapDigit - 1);
                left += snapHalf;
                rite += snapHalf;
                var count = Fixed.CeilToInt(localBotFixed) - Fixed.FloorToInt(y);
                var maskRow = isUsingMask ? mask!.Row(y >> 16) : NoMask;
                if (count > 1)
                {
                    if ((int)(y & 0xFFFF0000) != y)
                    {
                        count--;
                        var nextY = Fixed.CeilToFixed(y + 1);
                        var dY = nextY - y;
                        var nextLeft = left + Fixed.Mul(dLeft, dY);
                        var nextRite = rite + Fixed.Mul(dRite, dY);
                        BlitTrapezoidRow(blitter, y >> 16, left & snapMask, rite & snapMask, nextLeft & snapMask, nextRite & snapMask,
                            leftE.DY, riteE.DY, PartialAlpha(0xFF, dY), maskRow, false);
                        blitter.FlushIfYChanged(y, nextY);
                        left = nextLeft;
                        rite = nextRite;
                        y = nextY;
                    }
                    while (count > 1)
                    {
                        count--;
                        if (isUsingMask)
                            maskRow = mask!.Row(y >> 16);
                        int nextY = y + Fixed.One, nextLeft = left + dLeft, nextRite = rite + dRite;
                        BlitTrapezoidRow(blitter, y >> 16, left & snapMask, rite & snapMask, nextLeft & snapMask, nextRite & snapMask,
                            leftE.DY, riteE.DY, 0xFF, maskRow, false);
                        blitter.FlushIfYChanged(y, nextY);
                        left = nextLeft;
                        rite = nextRite;
                        y = nextY;
                    }
                }
                if (isUsingMask)
                    maskRow = mask!.Row(y >> 16);
                {
                    var dY = localBotFixed - y;
                    var nextLeft = Math.Max(left + Fixed.Mul(dLeft, dY), leftBound + snapHalf);
                    var nextRite = Math.Min(rite + Fixed.Mul(dRite, dY), riteBound + snapHalf);
                    BlitTrapezoidRow(blitter, y >> 16, left & snapMask, rite & snapMask, nextLeft & snapMask, nextRite & snapMask,
                        leftE.DY, riteE.DY, PartialAlpha(0xFF, dY), maskRow, false);
                    blitter.FlushIfYChanged(y, localBotFixed);
                    left = nextLeft;
                    rite = nextRite;
                    y = localBotFixed;
                    left -= snapHalf;
                    rite -= snapHalf;
                }
            }
            leftE.X = left;
            riteE.X = rite;
            leftE.Y = riteE.Y = y;
        }
    }

    static void UpdateNextNextY(int y, int nextY, ref int nextNextY)
    {
        nextNextY = y > nextY && y < nextNextY ? y : nextNextY;
    }

    static void CheckIntersection(AnalyticEdge edge, int nextY, ref int nextNextY)
    {
        if (edge.Prev!.Prev != null && edge.Prev.X + edge.Prev.DX > edge.X + edge.DX)
            nextNextY = nextY + (Fixed.One >> AnalyticEdge.DefaultAccuracy);
    }

    static void CheckIntersectionForward(AnalyticEdge edge, int nextY, ref int nextNextY)
    {
        if (edge.Next!.Next != null && edge.X + edge.DX > edge.Next.X + edge.Next.DX)
            nextNextY = nextY + (Fixed.One >> AnalyticEdge.DefaultAccuracy);
    }

    static void RemoveEdge(AnalyticEdge edge)
    {
        edge.Prev!.Next = edge.Next;
        edge.Next!.Prev = edge.Prev;
    }

    static void InsertEdgeAfter(AnalyticEdge edge, AnalyticEdge afterMe)
    {
        edge.Prev = afterMe;
        edge.Next = afterMe.Next;
        afterMe.Next!.Prev = edge;
        afterMe.Next = edge;
    }

    static void BackwardInsertEdgeBasedOnX(AnalyticEdge edge)
    {
        var x = edge.X;
        var prev = edge.Prev!;
        while (prev.Prev != null && prev.X > x)
            prev = prev.Prev;
        if (prev.Next != edge)
        {
            RemoveEdge(edge);
            InsertEdgeAfter(edge, prev);
        }
    }

    static AnalyticEdge BackwardInsertStart(AnalyticEdge prev, int x)
    {
        while (prev.Prev != null && prev.X > x)
            prev = prev.Prev;
        return prev;
    }

    static void InsertNewEdges(AnalyticEdge newEdge, int y, ref int nextNextY)
    {
        if (newEdge.UpperY > y)
        {
            UpdateNextNextY(newEdge.UpperY, y, ref nextNextY);
            return;
        }
        var prev = newEdge.Prev!;
        if (prev.X <= newEdge.X)
        {
            while (newEdge.UpperY <= y)
            {
                CheckIntersection(newEdge, y, ref nextNextY);
                UpdateNextNextY(newEdge.LowerY, y, ref nextNextY);
                newEdge = newEdge.Next!;
            }
            UpdateNextNextY(newEdge.UpperY, y, ref nextNextY);
            return;
        }
        var start = BackwardInsertStart(prev, newEdge.X);
        do
        {
            var next = newEdge.Next!;
            var skip = false;
            for (;;)
            {
                if (start.Next == newEdge)
                {
                    skip = true;
                    break;
                }
                var after = start.Next!;
                if (after.X >= newEdge.X)
                    break;
                start = after;
            }
            if (!skip)
            {
                RemoveEdge(newEdge);
                InsertEdgeAfter(newEdge, start);
            }
            CheckIntersection(newEdge, y, ref nextNextY);
            CheckIntersectionForward(newEdge, y, ref nextNextY);
            UpdateNextNextY(newEdge.LowerY, y, ref nextNextY);
            start = newEdge;
            newEdge = next;
        } while (newEdge.UpperY <= y);
        UpdateNextNextY(newEdge.UpperY, y, ref nextNextY);
    }

    static bool EdgesTooClose(AnalyticEdge? prev, AnalyticEdge? next, int lowerY)
    {
        const int slack = Fixed.One;
        return next != null && prev != null && next.UpperY < lowerY && prev.X + slack >= next.X - Fixed.Abs(next.DX);
    }

    static bool EdgesTooClose(int prevRite, int ul, int ll) =>
        prevRite > Fixed.FloorToInt(ul) || prevRite > Fixed.FloorToInt(ll);

    static void WalkEdges(AnalyticEdge prevHead, AnalyticEdge nextTail, AdditiveBlitter blitter, int startY, int stopY, int leftClip, int rightClip, bool isUsingMask, bool skipIntersect)
    {
        prevHead.X = prevHead.UpperX = leftClip;
        nextTail.X = nextTail.UpperX = rightClip;
        var y = Math.Max(prevHead.Next!.UpperY, Fixed.FromInt(startY));
        var nextNextY = Fixed.MaxS32;
        {
            AnalyticEdge edge;
            for (edge = prevHead.Next!; edge.UpperY <= y; edge = edge.Next!)
            {
                edge.GoY(y);
                UpdateNextNextY(edge.LowerY, y, ref nextNextY);
            }
            UpdateNextNextY(edge.UpperY, y, ref nextNextY);
        }
        var mask = blitter as MaskAdditiveBlitter;
        // Nonzero winding, not inverse.
        const int windingMask = -1;
        for (;;)
        {
            var w = 0;
            var inInterval = false;
            var prevX = prevHead.X;
            var nextY = Math.Min(nextNextY, Fixed.CeilToFixed(y + 1));
            var currE = prevHead.Next!;
            var leftE = prevHead;
            var left = leftClip;
            var leftDY = 0;
            var prevRite = Fixed.FloorToInt(leftClip);
            nextNextY = Fixed.MaxS32;
            var yShift = 0;
            if (((nextY - y) & (Fixed.One >> 2)) != 0)
            {
                yShift = 2;
                nextY = y + (Fixed.One >> 2);
            }
            else if (((nextY - y) & (Fixed.One >> 1)) != 0)
                yShift = 1;
            var fullAlpha = FixedToAlpha(nextY - y);
            var maskRow = isUsingMask ? mask!.Row(Fixed.FloorToInt(y)) : NoMask;
            const bool noRealBlitter = false;
            while (currE.UpperY <= y)
            {
                w += currE.Winding;
                var prevInInterval = inInterval;
                inInterval = (w & windingMask) != 0;
                var isLeft = inInterval && !prevInInterval;
                var isRite = !inInterval && prevInInterval;
                if (isRite)
                {
                    var rite = currE.X;
                    currE.GoY(nextY, yShift);
                    var nextLeft = Math.Max(leftClip, leftE.X);
                    rite = Math.Min(rightClip, rite);
                    var nextRite = Math.Min(rightClip, currE.X);
                    BlitTrapezoidRow(blitter, y >> 16, left, rite, nextLeft, nextRite, leftDY, currE.DY, fullAlpha, maskRow,
                        noRealBlitter || (fullAlpha == 0xFF && (EdgesTooClose(prevRite, left, leftE.X) || EdgesTooClose(currE, currE.Next, nextY))));
                    prevRite = Fixed.CeilToInt(Math.Max(rite, currE.X));
                }
                else
                {
                    if (isLeft)
                    {
                        left = Math.Max(currE.X, leftClip);
                        leftDY = currE.DY;
                        leftE = currE;
                    }
                    currE.GoY(nextY, yShift);
                }
                var next = currE.Next!;
                while (currE.LowerY <= nextY)
                {
                    if (currE.CurveCount < 0)
                    {
                        currE.KeepContinuousCubic();
                        if (!currE.UpdateCubic())
                            break;
                    }
                    else if (currE.CurveCount > 0)
                    {
                        currE.KeepContinuousQuad();
                        if (!currE.UpdateQuadratic())
                            break;
                    }
                    else
                        break;
                }
                if (currE.LowerY <= nextY)
                    RemoveEdge(currE);
                else
                {
                    UpdateNextNextY(currE.LowerY, nextY, ref nextNextY);
                    var newX = currE.X;
                    if (newX < prevX)
                        BackwardInsertEdgeBasedOnX(currE);
                    else
                        prevX = newX;
                    if (!skipIntersect)
                        CheckIntersection(currE, nextY, ref nextNextY);
                }
                currE = next;
            }
            if (inInterval)
                BlitTrapezoidRow(blitter, y >> 16, left, rightClip, Math.Max(leftClip, leftE.X), rightClip, leftDY, 0, fullAlpha, maskRow,
                    noRealBlitter || (fullAlpha == 0xFF && EdgesTooClose(leftE.Prev, leftE, nextY)));
            y = nextY;
            if (y >= Fixed.FromInt(stopY))
                break;
            InsertNewEdges(currE, y, ref nextNextY);
        }
    }
}
