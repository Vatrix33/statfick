namespace Statventure.Imaging;

/// <summary>
/// Perlin turbulence and fractal noise exactly as SVG's feTurbulence defines them (Filter Effects 1
/// §15.21, with its reference implementation): lattice gradients from the seed, octaves, and
/// stitching for seamless tiles. Values come out as RGBA in 0..1, in linearRGB like the filter's
/// default color-interpolation-filters.
/// </summary>
public sealed class Turbulence
{
    const int BSize = 0x100;
    const int BM = 0xff;
    const int PerlinN = 0x1000;
    const long RandM = 2147483647;
    const long RandA = 16807;
    const long RandQ = 127773;
    const long RandR = 2836;

    readonly int[] _lattice = new int[BSize + BSize + 2];
    readonly double[,,] _gradient = new double[4, BSize + BSize + 2, 2];

    public Turbulence(long seed)
    {
        seed = SetupSeed(seed);
        int i, j, k;
        for (k = 0; k < 4; k++)
        {
            for (i = 0; i < BSize; i++)
            {
                _lattice[i] = i;
                for (j = 0; j < 2; j++)
                    _gradient[k, i, j] = (double)(((seed = Random(seed)) % (BSize + BSize)) - BSize) / BSize;
                var s = Math.Sqrt(_gradient[k, i, 0] * _gradient[k, i, 0] + _gradient[k, i, 1] * _gradient[k, i, 1]);
                _gradient[k, i, 0] /= s;
                _gradient[k, i, 1] /= s;
            }
        }
        for (i = BSize - 1; i > 0; i--)
        {
            k = _lattice[i];
            j = (int)((seed = Random(seed)) % BSize);
            _lattice[i] = _lattice[j];
            _lattice[j] = k;
        }
        for (i = 0; i < BSize + 2; i++)
        {
            _lattice[BSize + i] = _lattice[i];
            for (k = 0; k < 4; k++)
                for (j = 0; j < 2; j++)
                    _gradient[k, BSize + i, j] = _gradient[k, i, j];
        }
    }

    static long SetupSeed(long seed)
    {
        if (seed <= 0)
            seed = -(seed % (RandM - 1)) + 1;
        if (seed > RandM - 1)
            seed = RandM - 1;
        return seed;
    }

    static long Random(long seed)
    {
        var result = RandA * (seed % RandQ) - RandR * (seed / RandQ);
        if (result <= 0)
            result += RandM;
        return result;
    }

    struct Stitch
    {
        public int Width, Height, WrapX, WrapY;
    }

    static double SCurve(double t) => t * t * (3.0 - 2.0 * t);
    static double Lerp(double t, double a, double b) => a + t * (b - a);

    double Noise2(int channel, double x, double y, bool stitching, in Stitch stitch)
    {
        var t = x + PerlinN;
        var bx0 = (int)t & BM;
        var bx1 = (bx0 + 1) & BM;
        var rx0 = t - (int)t;
        var rx1 = rx0 - 1.0;
        t = y + PerlinN;
        var by0 = (int)t & BM;
        var by1 = (by0 + 1) & BM;
        var ry0 = t - (int)t;
        var ry1 = ry0 - 1.0;
        if (stitching)
        {
            // The spec compares the unmasked lattice values; recompute them for the comparison.
            var ix0 = (int)(x + PerlinN);
            var ix1 = ix0 + 1;
            var iy0 = (int)(y + PerlinN);
            var iy1 = iy0 + 1;
            if (ix0 >= stitch.WrapX) ix0 -= stitch.Width;
            if (ix1 >= stitch.WrapX) ix1 -= stitch.Width;
            if (iy0 >= stitch.WrapY) iy0 -= stitch.Height;
            if (iy1 >= stitch.WrapY) iy1 -= stitch.Height;
            bx0 = ix0 & BM;
            bx1 = ix1 & BM;
            by0 = iy0 & BM;
            by1 = iy1 & BM;
        }
        var i = _lattice[bx0];
        var j = _lattice[bx1];
        var b00 = _lattice[i + by0];
        var b10 = _lattice[j + by0];
        var b01 = _lattice[i + by1];
        var b11 = _lattice[j + by1];
        var sx = SCurve(rx0);
        var sy = SCurve(ry0);
        var u = rx0 * _gradient[channel, b00, 0] + ry0 * _gradient[channel, b00, 1];
        var v = rx1 * _gradient[channel, b10, 0] + ry0 * _gradient[channel, b10, 1];
        var a = Lerp(sx, u, v);
        u = rx0 * _gradient[channel, b01, 0] + ry1 * _gradient[channel, b01, 1];
        v = rx1 * _gradient[channel, b11, 0] + ry1 * _gradient[channel, b11, 1];
        var b = Lerp(sx, u, v);
        return Lerp(sy, a, b);
    }

    /// <summary>The turbulence sum for one channel at a point (the spec's turbulence()).</summary>
    public double Sample(int channel, double x, double y, double baseFrequencyX, double baseFrequencyY, int octaves, bool fractalSum, bool stitchTiles, double tileX, double tileY, double tileWidth, double tileHeight)
    {
        var stitch = new Stitch();
        if (stitchTiles)
        {
            if (baseFrequencyX != 0.0)
            {
                var lo = Math.Floor(tileWidth * baseFrequencyX) / tileWidth;
                var hi = Math.Ceiling(tileWidth * baseFrequencyX) / tileWidth;
                baseFrequencyX = baseFrequencyX / lo < hi / baseFrequencyX ? lo : hi;
            }
            if (baseFrequencyY != 0.0)
            {
                var lo = Math.Floor(tileHeight * baseFrequencyY) / tileHeight;
                var hi = Math.Ceiling(tileHeight * baseFrequencyY) / tileHeight;
                baseFrequencyY = baseFrequencyY / lo < hi / baseFrequencyY ? lo : hi;
            }
            stitch.Width = (int)(tileWidth * baseFrequencyX + 0.5);
            stitch.WrapX = (int)(tileX * baseFrequencyX + PerlinN + stitch.Width);
            stitch.Height = (int)(tileHeight * baseFrequencyY + 0.5);
            stitch.WrapY = (int)(tileY * baseFrequencyY + PerlinN + stitch.Height);
        }
        var sum = 0.0;
        var vx = x * baseFrequencyX;
        var vy = y * baseFrequencyY;
        var ratio = 1.0;
        for (var octave = 0; octave < octaves; octave++)
        {
            var n = Noise2(channel, vx, vy, stitchTiles, stitch);
            sum += fractalSum ? n / ratio : Math.Abs(n) / ratio;
            vx *= 2;
            vy *= 2;
            ratio *= 2;
            if (stitchTiles)
            {
                stitch.Width *= 2;
                stitch.WrapX = 2 * stitch.WrapX - PerlinN;
                stitch.Height *= 2;
                stitch.WrapY = 2 * stitch.WrapY - PerlinN;
            }
        }
        return sum;
    }

    /// <summary>
    /// An RGBA image of the filter over a tile (pixel centres sampled), straight alpha in 0..1:
    /// fractalNoise maps the sum s to (s + 1) / 2, turbulence takes it as it is.
    /// </summary>
    public float[] Render(int width, int height, double baseFrequencyX, double baseFrequencyY, int octaves, bool fractalNoise, bool stitchTiles, double scale = 1)
    {
        var pixels = new float[width * height * 4];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var px = (x + 0.5) / scale;
                var py = (y + 0.5) / scale;
                for (var c = 0; c < 4; c++)
                {
                    var sum = Sample(c, px, py, baseFrequencyX, baseFrequencyY, octaves, fractalNoise, stitchTiles, 0, 0, width / scale, height / scale);
                    var value = fractalNoise ? (sum + 1) / 2 : sum;
                    pixels[(y * width + x) * 4 + c] = (float)Math.Clamp(value, 0, 1);
                }
            }
        return pixels;
    }
}
