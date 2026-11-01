using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Statventure.Audio;

/// <summary>
/// One AVX register of four complex floats (re, im interleaved), with the intrinsics RustFFT's AVX
/// code uses, lane for lane: adds, multiplies and fused multiply-adds round as the instructions do,
/// shuffles only move bits (Docs/Audio.md, "The FFT"). Uses AVX and FMA where the CPU has them,
/// the same results computed lane by lane where it doesn't.
/// </summary>
internal readonly struct V8(Vector256<float> v)
{
    public readonly Vector256<float> V = v;

    static readonly bool HasFma = Fma.IsSupported;

    public static V8 Load(ReadOnlySpan<float> data, int complexIndex) => new(Vector256.Create(data.Slice(2 * complexIndex, 8)));
    public void Store(Span<float> data, int complexIndex) => V.CopyTo(data.Slice(2 * complexIndex, 8));

    public static V8 Broadcast(float re, float im) => new(Vector256.Create(re, im, re, im, re, im, re, im));
    public static V8 FromComplex(ReadOnlySpan<float> fourComplex) => new(Vector256.Create(fourComplex[..8]));

    public static V8 operator +(V8 a, V8 b) => new(a.V + b.V);
    public static V8 operator -(V8 a, V8 b) => new(a.V - b.V);
    public static V8 operator *(V8 a, V8 b) => new(a.V * b.V);
    public static V8 Xor(V8 a, V8 b) => new(a.V ^ b.V);
    public V8 Neg() => Xor(this, new V8(Vector256.Create(-0.0f)));

    /// <summary>_mm256_fmaddsub_ps: a·b − c in the even lanes, a·b + c in the odd, each rounded once.</summary>
    public static V8 FmAddSub(V8 a, V8 b, V8 c)
    {
        if (HasFma)
            return new(Fma.MultiplyAddSubtract(a.V, b.V, c.V));
        Span<float> r = stackalloc float[8];
        for (var i = 0; i < 8; i++)
            r[i] = MathF.FusedMultiplyAdd(a.V[i], b.V[i], (i & 1) == 0 ? -c.V[i] : c.V[i]);
        return new(Vector256.Create((ReadOnlySpan<float>)r));
    }

    /// <summary>_mm256_fmsubadd_ps: a·b + c in the even lanes, a·b − c in the odd.</summary>
    public static V8 FmSubAdd(V8 a, V8 b, V8 c)
    {
        if (HasFma)
            return new(Fma.MultiplySubtractAdd(a.V, b.V, c.V));
        Span<float> r = stackalloc float[8];
        for (var i = 0; i < 8; i++)
            r[i] = MathF.FusedMultiplyAdd(a.V[i], b.V[i], (i & 1) == 0 ? c.V[i] : -c.V[i]);
        return new(Vector256.Create((ReadOnlySpan<float>)r));
    }

    /// <summary>_mm256_permute_ps(v, 0xB1): each complex's re and im swapped.</summary>
    public V8 SwapComplexComponents() => new(Vector256.Shuffle(V, Vector256.Create(1, 0, 3, 2, 5, 4, 7, 6)));

    /// <summary>_mm256_moveldup_ps and _mm256_movehdup_ps: the reals duplicated, the imaginaries duplicated.</summary>
    public (V8 Real, V8 Imag) DuplicateComplexComponents() =>
        (new(Vector256.Shuffle(V, Vector256.Create(0, 0, 2, 2, 4, 4, 6, 6))), new(Vector256.Shuffle(V, Vector256.Create(1, 1, 3, 3, 5, 5, 7, 7))));

    /// <summary>_mm256_permute2f128_ps: each half chosen from a's or b's halves (0 a.lo, 1 a.hi, 2 b.lo, 3 b.hi).</summary>
    public static V8 Permute2F128(V8 a, V8 b, int imm)
    {
        Vector128<float> Pick(int selector) => (selector & 3) switch
        {
            0 => a.V.GetLower(),
            1 => a.V.GetUpper(),
            2 => b.V.GetLower(),
            _ => b.V.GetUpper(),
        };
        return new(Vector256.Create(Pick(imm), Pick(imm >> 4)));
    }

    /// <summary>_mm256_unpacklo_pd on complex numbers: a0, b0, a2, b2.</summary>
    public static V8 UnpackLoComplex(V8 a, V8 b)
    {
        var x = a.V.AsDouble();
        var y = b.V.AsDouble();
        return new(Vector256.Create(x[0], y[0], x[2], y[2]).AsSingle());
    }

    /// <summary>_mm256_unpackhi_pd on complex numbers: a1, b1, a3, b3.</summary>
    public static V8 UnpackHiComplex(V8 a, V8 b)
    {
        var x = a.V.AsDouble();
        var y = b.V.AsDouble();
        return new(Vector256.Create(x[1], y[1], x[3], y[3]).AsSingle());
    }
}

/// <summary>RustFFT's AvxVector operations on <see cref="V8"/> (avx_vector.rs), for f32.</summary>
internal static class AvxComplex
{
    /// <summary>0.5f32.sqrt().</summary>
    static readonly V8 HalfRoot2 = new(Vector256.Create(MathF.Sqrt(0.5f)));

    /// <summary>A Rotation90: forward negates the reals, inverse the imaginaries (before the swap).</summary>
    public static V8 MakeRotation90(bool inverse) => inverse ? V8.Broadcast(0.0f, -0.0f) : V8.Broadcast(-0.0f, 0.0f);

    public static V8 MulComplex(V8 left, V8 right)
    {
        var (leftReal, leftImag) = left.DuplicateComplexComponents();
        var rightShuffled = right.SwapComplexComponents();
        var outputRight = leftImag * rightShuffled;
        return V8.FmAddSub(leftReal, right, outputRight);
    }

    public static V8 Rotate90(V8 v, V8 rotation) => V8.Xor(v, rotation).SwapComplexComponents();

    public static (V8, V8) Butterfly2(V8 a, V8 b) => (a + b, a - b);

    public static void Butterfly4(ReadOnlySpan<V8> rows, V8 rotation, Span<V8> output)
    {
        var (mid0, mid2) = Butterfly2(rows[0], rows[2]);
        var (mid1, mid3) = Butterfly2(rows[1], rows[3]);
        var mid3Rotated = Rotate90(mid3, rotation);
        var (output0, output1) = Butterfly2(mid0, mid1);
        var (output2, output3) = Butterfly2(mid2, mid3Rotated);
        output[0] = output0;
        output[1] = output2;
        output[2] = output1;
        output[3] = output3;
    }

    public static V8 ButterflyTwiddle1(V8 v, V8 rotation) => HalfRoot2 * (Rotate90(v, rotation) + v);
    public static V8 ButterflyTwiddle3(V8 v, V8 rotation) => HalfRoot2 * (Rotate90(v, rotation) - v);

    public static void Butterfly8(ReadOnlySpan<V8> rows, V8 rotation, Span<V8> output)
    {
        Span<V8> mid0 = stackalloc V8[4];
        Span<V8> mid1 = stackalloc V8[4];
        Butterfly4([rows[0], rows[2], rows[4], rows[6]], rotation, mid0);
        Butterfly4([rows[1], rows[3], rows[5], rows[7]], rotation, mid1);
        mid1[1] = ButterflyTwiddle1(mid1[1], rotation);
        mid1[2] = Rotate90(mid1[2], rotation);
        mid1[3] = ButterflyTwiddle3(mid1[3], rotation);
        var (output0, output1) = Butterfly2(mid0[0], mid1[0]);
        var (output2, output3) = Butterfly2(mid0[1], mid1[1]);
        var (output4, output5) = Butterfly2(mid0[2], mid1[2]);
        var (output6, output7) = Butterfly2(mid0[3], mid1[3]);
        output[0] = output0;
        output[1] = output2;
        output[2] = output4;
        output[3] = output6;
        output[4] = output1;
        output[5] = output3;
        output[6] = output5;
        output[7] = output7;
    }

    public static void Transpose4Packed(ReadOnlySpan<V8> rows, Span<V8> output)
    {
        var permute0 = V8.Permute2F128(rows[0], rows[2], 0x20);
        var permute1 = V8.Permute2F128(rows[1], rows[3], 0x20);
        var permute2 = V8.Permute2F128(rows[0], rows[2], 0x31);
        var permute3 = V8.Permute2F128(rows[1], rows[3], 0x31);
        output[0] = V8.UnpackLoComplex(permute0, permute1);
        output[1] = V8.UnpackHiComplex(permute0, permute1);
        output[2] = V8.UnpackLoComplex(permute2, permute3);
        output[3] = V8.UnpackHiComplex(permute2, permute3);
    }

    public static void Transpose8Packed(ReadOnlySpan<V8> rows, Span<V8> output)
    {
        Span<V8> output0 = stackalloc V8[4];
        Span<V8> output1 = stackalloc V8[4];
        Transpose4Packed(rows[..4], output0);
        Transpose4Packed(rows[4..8], output1);
        for (var i = 0; i < 4; i++)
        {
            output[2 * i] = output0[i];
            output[2 * i + 1] = output1[i];
        }
    }

    /// <summary>twiddles::compute_twiddle: e^(−2πi·index/len) in double (the C library's cos and sin), rounded to float; conjugated for inverse.</summary>
    public static (float Re, float Im) Twiddle(long index, long length, bool inverse)
    {
        var constant = -2.0 * Math.PI / length;
        var angle = constant * index;
        var re = (float)Math.Cos(angle);
        var im = (float)Math.Sin(angle);
        return (re, inverse ? -im : im);
    }

    public static V8 BroadcastTwiddle(int index, int length, bool inverse)
    {
        var (re, im) = Twiddle(index, length, inverse);
        return V8.Broadcast(re, im);
    }

    /// <summary>make_mixedradix_twiddle_chunk: the twiddles y·(x + i) for i = 0..3.</summary>
    public static V8 MixedRadixTwiddleChunk(int x, int y, int length, bool inverse)
    {
        Span<float> chunk = stackalloc float[8];
        for (var i = 0; i < 4; i++)
            (chunk[2 * i], chunk[2 * i + 1]) = Twiddle((long)y * (x + i), length, inverse);
        return V8.FromComplex(chunk);
    }

    /// <summary>gen_butterfly_twiddles_separated_columns!(rows, cols, 0, direction).</summary>
    public static V8[] SeparatedColumnTwiddles(int rows, int cols, bool inverse)
    {
        var twiddleRows = rows - 1;
        var count = cols / 4 * twiddleRows;
        var twiddles = new V8[count];
        for (var index = 0; index < count; index++)
            twiddles[index] = MixedRadixTwiddleChunk(index / twiddleRows * 4, index % twiddleRows + 1, rows * cols, inverse);
        return twiddles;
    }

    /// <summary>column_butterfly16_loadfn!: loads all sixteen, then stores.</summary>
    public static void Butterfly16(Func<int, V8> load, Action<V8, int> store, V8 twiddle1, V8 twiddle3, V8 rotation)
    {
        Span<V8> mid1 = stackalloc V8[4];
        Butterfly4([load(1), load(5), load(9), load(13)], rotation, mid1);
        mid1[1] = MulComplex(mid1[1], twiddle1);
        mid1[2] = ButterflyTwiddle1(mid1[2], rotation);
        mid1[3] = MulComplex(mid1[3], twiddle3);
        Span<V8> mid2 = stackalloc V8[4];
        Butterfly4([load(2), load(6), load(10), load(14)], rotation, mid2);
        mid2[1] = ButterflyTwiddle1(mid2[1], rotation);
        mid2[2] = Rotate90(mid2[2], rotation);
        mid2[3] = ButterflyTwiddle3(mid2[3], rotation);
        Span<V8> mid3 = stackalloc V8[4];
        Butterfly4([load(3), load(7), load(11), load(15)], rotation, mid3);
        mid3[1] = MulComplex(mid3[1], twiddle3);
        mid3[2] = ButterflyTwiddle3(mid3[2], rotation);
        mid3[3] = MulComplex(mid3[3], twiddle1.Neg());
        Span<V8> mid0 = stackalloc V8[4];
        Butterfly4([load(0), load(4), load(8), load(12)], rotation, mid0);
        Span<V8> output = stackalloc V8[4];
        for (var i = 0; i < 4; i++)
        {
            Butterfly4([mid0[i], mid1[i], mid2[i], mid3[i]], rotation, output);
            store(output[0], i);
            store(output[1], i + 4);
            store(output[2], i + 8);
            store(output[3], i + 12);
        }
    }

    /// <summary>column_butterfly32_loadfn! (twiddles: 1, 2, 3, 5, 6, 7 of 32).</summary>
    public static void Butterfly32(Func<int, V8> load, Action<V8, int> store, V8[] t, V8 rotation)
    {
        Span<V8> mid1 = stackalloc V8[4];
        Butterfly4([load(1), load(9), load(17), load(25)], rotation, mid1);
        mid1[1] = MulComplex(mid1[1], t[0]);
        mid1[2] = MulComplex(mid1[2], t[1]);
        mid1[3] = MulComplex(mid1[3], t[2]);
        Span<V8> mid2 = stackalloc V8[4];
        Butterfly4([load(2), load(10), load(18), load(26)], rotation, mid2);
        mid2[1] = MulComplex(mid2[1], t[1]);
        mid2[2] = ButterflyTwiddle1(mid2[2], rotation);
        mid2[3] = MulComplex(mid2[3], t[4]);
        Span<V8> mid3 = stackalloc V8[4];
        Butterfly4([load(3), load(11), load(19), load(27)], rotation, mid3);
        mid3[1] = MulComplex(mid3[1], t[2]);
        mid3[2] = MulComplex(mid3[2], t[4]);
        mid3[3] = MulComplex(mid3[3], Rotate90(t[0], rotation));
        Span<V8> mid4 = stackalloc V8[4];
        Butterfly4([load(4), load(12), load(20), load(28)], rotation, mid4);
        mid4[1] = ButterflyTwiddle1(mid4[1], rotation);
        mid4[2] = Rotate90(mid4[2], rotation);
        mid4[3] = ButterflyTwiddle3(mid4[3], rotation);
        Span<V8> mid5 = stackalloc V8[4];
        Butterfly4([load(5), load(13), load(21), load(29)], rotation, mid5);
        mid5[1] = MulComplex(mid5[1], t[3]);
        mid5[2] = MulComplex(mid5[2], Rotate90(t[1], rotation));
        mid5[3] = MulComplex(mid5[3], Rotate90(t[5], rotation));
        Span<V8> mid6 = stackalloc V8[4];
        Butterfly4([load(6), load(14), load(22), load(30)], rotation, mid6);
        mid6[1] = MulComplex(mid6[1], t[4]);
        mid6[2] = ButterflyTwiddle3(mid6[2], rotation);
        mid6[3] = MulComplex(mid6[3], t[1].Neg());
        Span<V8> mid7 = stackalloc V8[4];
        Butterfly4([load(7), load(15), load(23), load(31)], rotation, mid7);
        mid7[1] = MulComplex(mid7[1], t[5]);
        mid7[2] = MulComplex(mid7[2], Rotate90(t[4], rotation));
        mid7[3] = MulComplex(mid7[3], t[3].Neg());
        Span<V8> mid0 = stackalloc V8[4];
        Butterfly4([load(0), load(8), load(16), load(24)], rotation, mid0);
        Span<V8> output = stackalloc V8[8];
        for (var i = 0; i < 4; i++)
        {
            Butterfly8([mid0[i], mid1[i], mid2[i], mid3[i], mid4[i], mid5[i], mid6[i], mid7[i]], rotation, output);
            for (var k = 0; k < 8; k++)
                store(output[k], i + 4 * k);
        }
    }
}

/// <summary>
/// A complex FFT as RustFFT 6.4.1's AVX planner builds it for f32 (avx_planner.rs, the power-of-two
/// plans): the 128-, 256- and 512-point AVX butterflies, and MixedRadix 2/4/8/16xn stages over them.
/// Data is interleaved complex floats.
/// </summary>
internal abstract class ComplexFft
{
    protected ComplexFft(int length, bool inverse)
    {
        Length = length;
        Inverse = inverse;
    }

    public int Length { get; }
    public bool Inverse { get; }

    /// <summary>The transform of <paramref name="input"/> into <paramref name="output"/> (different buffers, <see cref="Length"/> complex each).</summary>
    public abstract void Transform(ReadOnlySpan<float> input, Span<float> output);

    /// <summary>FftPlannerAvx's plan for a power of two of 64 or more (the lengths Web Audio needs).</summary>
    public static ComplexFft Plan(int length, bool inverse)
    {
        if (length < 64 || (length & (length - 1)) != 0)
            throw new NotSupportedException($"FFT length {length}: only powers of two from 64 are ported");
        var power2 = System.Numerics.BitOperations.Log2((uint)length);
        if (power2 <= 9)
            return Butterfly(length, inverse); // 64, 128, 256, 512: butterflies of their own
        // plan_mixed_radix_base: a 512 butterfly when the power of two is a multiple of 3, else 256.
        var basePower = power2 % 3 == 0 ? 9 : 8;
        var radixes = new List<int>();
        var remaining = power2 - basePower;
        if (remaining <= 4)
            radixes.Add(1 << remaining); // 2, 4, 8 or 16: a single step
        else
        {
            // plan_mixed_radix for 2^r (no factors of 3): a 16 when r % 3 == 1, then 8s, then a 4 or a 2.
            if (remaining % 3 == 1)
            {
                radixes.Add(16);
                remaining -= 4;
            }
            for (var i = 0; i < remaining / 3; i++)
                radixes.Add(8);
            if (remaining % 3 == 2)
                radixes.Add(4);
            if (remaining % 3 == 1)
                radixes.Add(2);
        }
        ComplexFft fft = Butterfly(1 << basePower, inverse);
        foreach (var radix in radixes)
            fft = new MixedRadixAvx(radix, fft);
        return fft;
    }

    static ComplexFft Butterfly(int length, bool inverse) => length switch
    {
        64 => new Butterfly64Avx(inverse),
        128 => new Butterfly128Avx(inverse),
        256 => new Butterfly256Avx(inverse),
        512 => new Butterfly512Avx(inverse),
        _ => throw new NotSupportedException($"butterfly {length}"),
    };
}

/// <summary>Butterfly64Avx: 8 × 8, columns of butterfly 8, twiddled, transposed, and butterfly 8 again.</summary>
internal sealed class Butterfly64Avx : ComplexFft
{
    readonly V8[] _twiddles;
    readonly V8 _rotation;

    public Butterfly64Avx(bool inverse) : base(64, inverse)
    {
        _twiddles = AvxComplex.SeparatedColumnTwiddles(8, 8, inverse);
        _rotation = AvxComplex.MakeRotation90(inverse);
    }

    public override void Transform(ReadOnlySpan<float> input, Span<float> output)
    {
        Span<V8> rows = stackalloc V8[8];
        Span<V8> mid0 = stackalloc V8[8];
        Span<V8> mid1 = stackalloc V8[8];
        for (var r = 0; r < 8; r++)
            rows[r] = V8.Load(input, 8 * r);
        AvxComplex.Butterfly8(rows, _rotation, mid0);
        for (var r = 1; r < 8; r++)
            mid0[r] = AvxComplex.MulComplex(mid0[r], _twiddles[r - 1]);
        for (var r = 0; r < 8; r++)
            rows[r] = V8.Load(input, 8 * r + 4);
        AvxComplex.Butterfly8(rows, _rotation, mid1);
        for (var r = 1; r < 8; r++)
            mid1[r] = AvxComplex.MulComplex(mid1[r], _twiddles[r - 1 + 7]);
        // transpose_8x8_f32
        Span<V8> transposed0 = stackalloc V8[8];
        Span<V8> transposed1 = stackalloc V8[8];
        AvxComplex.Transpose4Packed(mid0[..4], transposed0[..4]);
        AvxComplex.Transpose4Packed(mid1[..4], transposed0[4..]);
        AvxComplex.Transpose4Packed(mid0[4..], transposed1[..4]);
        AvxComplex.Transpose4Packed(mid1[4..], transposed1[4..]);
        Span<V8> result = stackalloc V8[8];
        AvxComplex.Butterfly8(transposed0, _rotation, result);
        for (var r = 0; r < 8; r++)
            result[r].Store(output, 8 * r);
        AvxComplex.Butterfly8(transposed1, _rotation, result);
        for (var r = 0; r < 8; r++)
            result[r].Store(output, 8 * r + 4);
    }
}

/// <summary>Butterfly128Avx: 8 × 16, columns of butterfly 8 then rows of butterfly 16.</summary>
internal sealed class Butterfly128Avx : ComplexFft
{
    readonly V8[] _twiddles;
    readonly V8 _twiddle16a, _twiddle16b, _rotation;

    public Butterfly128Avx(bool inverse) : base(128, inverse)
    {
        _twiddles = AvxComplex.SeparatedColumnTwiddles(8, 16, inverse);
        _twiddle16a = AvxComplex.BroadcastTwiddle(1, 16, inverse);
        _twiddle16b = AvxComplex.BroadcastTwiddle(3, 16, inverse);
        _rotation = AvxComplex.MakeRotation90(inverse);
    }

    public override void Transform(ReadOnlySpan<float> input, Span<float> output)
    {
        Span<V8> rows = stackalloc V8[8];
        Span<V8> mid = stackalloc V8[8];
        Span<V8> transposed = stackalloc V8[8];
        for (var columnset = 0; columnset < 4; columnset++)
        {
            for (var r = 0; r < 8; r++)
                rows[r] = V8.Load(input, columnset * 4 + 16 * r);
            AvxComplex.Butterfly8(rows, _rotation, mid);
            for (var r = 1; r < 8; r++)
                mid[r] = AvxComplex.MulComplex(mid[r], _twiddles[r - 1 + 7 * columnset]);
            AvxComplex.Transpose8Packed(mid, transposed);
            for (var i = 0; i < 4; i++)
            {
                transposed[i * 2].Store(output, columnset * 32 + i * 8);
                transposed[i * 2 + 1].Store(output, columnset * 32 + i * 8 + 4);
            }
        }
        var buffer = output.ToArray();
        for (var columnset = 0; columnset < 2; columnset++)
        {
            var c = columnset;
            AvxComplex.Butterfly16(i => V8.Load(buffer, c * 4 + i * 8), (d, i) => d.Store(buffer, c * 4 + i * 8), _twiddle16a, _twiddle16b, _rotation);
        }
        buffer.CopyTo(output);
    }
}

/// <summary>Butterfly256Avx: 8 × 32, columns of butterfly 8 then rows of butterfly 32.</summary>
internal sealed class Butterfly256Avx : ComplexFft
{
    readonly V8[] _twiddles;
    readonly V8[] _twiddles32;
    readonly V8 _rotation;

    public Butterfly256Avx(bool inverse) : base(256, inverse)
    {
        _twiddles = AvxComplex.SeparatedColumnTwiddles(8, 32, inverse);
        _twiddles32 = [.. new[] { 1, 2, 3, 5, 6, 7 }.Select(k => AvxComplex.BroadcastTwiddle(k, 32, inverse))];
        _rotation = AvxComplex.MakeRotation90(inverse);
    }

    public override void Transform(ReadOnlySpan<float> input, Span<float> output)
    {
        Span<V8> rows = stackalloc V8[8];
        Span<V8> mid = stackalloc V8[8];
        Span<V8> transposed = stackalloc V8[8];
        for (var columnset = 0; columnset < 8; columnset++)
        {
            for (var r = 0; r < 8; r++)
                rows[r] = V8.Load(input, columnset * 4 + 32 * r);
            AvxComplex.Butterfly8(rows, _rotation, mid);
            for (var r = 1; r < 8; r++)
                mid[r] = AvxComplex.MulComplex(mid[r], _twiddles[r - 1 + 7 * columnset]);
            AvxComplex.Transpose8Packed(mid, transposed);
            for (var i = 0; i < 4; i++)
            {
                transposed[i * 2].Store(output, columnset * 32 + i * 8);
                transposed[i * 2 + 1].Store(output, columnset * 32 + i * 8 + 4);
            }
        }
        var buffer = output.ToArray();
        for (var columnset = 0; columnset < 2; columnset++)
        {
            var c = columnset;
            AvxComplex.Butterfly32(i => V8.Load(buffer, c * 4 + i * 8), (d, i) => d.Store(buffer, c * 4 + i * 8), _twiddles32, _rotation);
        }
        buffer.CopyTo(output);
    }
}

/// <summary>Butterfly512Avx: 16 × 32, columns of butterfly 16 (twiddled four rows at a time) then rows of butterfly 32.</summary>
internal sealed class Butterfly512Avx : ComplexFft
{
    readonly V8[] _twiddles;
    readonly V8[] _twiddles32;
    readonly V8 _twiddle16a, _twiddle16b, _rotation;

    public Butterfly512Avx(bool inverse) : base(512, inverse)
    {
        _twiddles = AvxComplex.SeparatedColumnTwiddles(16, 32, inverse);
        _twiddles32 = [.. new[] { 1, 2, 3, 5, 6, 7 }.Select(k => AvxComplex.BroadcastTwiddle(k, 32, inverse))];
        _twiddle16a = AvxComplex.BroadcastTwiddle(1, 16, inverse);
        _twiddle16b = AvxComplex.BroadcastTwiddle(3, 16, inverse);
        _rotation = AvxComplex.MakeRotation90(inverse);
    }

    public override void Transform(ReadOnlySpan<float> input, Span<float> output)
    {
        const int TwiddlesPerColumn = 15;
        var source = input.ToArray();
        var mid = new V8[16];
        Span<V8> twiddled = stackalloc V8[4];
        Span<V8> transposed = stackalloc V8[4];
        for (var columnset = 0; columnset < _twiddles.Length / TwiddlesPerColumn; columnset++)
        {
            var c = columnset;
            AvxComplex.Butterfly16(i => V8.Load(source, c * 4 + 32 * i), (d, i) => mid[i] = d, _twiddle16a, _twiddle16b, _rotation);
            var t = _twiddles.AsSpan(columnset * TwiddlesPerColumn, TwiddlesPerColumn);
            for (var chunk = 0; chunk < 4; chunk++)
            {
                twiddled[0] = chunk > 0 ? AvxComplex.MulComplex(mid[4 * chunk], t[4 * chunk - 1]) : mid[4 * chunk];
                twiddled[1] = AvxComplex.MulComplex(mid[4 * chunk + 1], t[4 * chunk]);
                twiddled[2] = AvxComplex.MulComplex(mid[4 * chunk + 2], t[4 * chunk + 1]);
                twiddled[3] = AvxComplex.MulComplex(mid[4 * chunk + 3], t[4 * chunk + 2]);
                AvxComplex.Transpose4Packed(twiddled, transposed);
                for (var k = 0; k < 4; k++)
                    transposed[k].Store(output, columnset * 64 + k * 16 + 4 * chunk);
            }
        }
        var buffer = output.ToArray();
        for (var columnset = 0; columnset < 4; columnset++)
        {
            var c = columnset;
            AvxComplex.Butterfly32(i => V8.Load(buffer, c * 4 + i * 16), (d, i) => d.Store(buffer, c * 4 + i * 16), _twiddles32, _rotation);
        }
        buffer.CopyTo(output);
    }
}

/// <summary>
/// MixedRadix{2,4,8,16}xnAvx: butterflies down the columns of a rows × (length / rows) layout, each
/// row but the first twiddled, the inner FFT along every row, then the transpose.
/// </summary>
internal sealed class MixedRadixAvx : ComplexFft
{
    readonly int _rows;
    readonly ComplexFft _inner;
    readonly V8[] _twiddles;
    readonly V8 _rotation;

    public MixedRadixAvx(int rows, ComplexFft inner) : base(rows * inner.Length, inner.Inverse)
    {
        if (rows is not (4 or 8))
            throw new NotSupportedException($"MixedRadix{rows}xnAvx isn't ported (Web Audio's lengths need 4 and 8)");
        _rows = rows;
        _inner = inner;
        _rotation = AvxComplex.MakeRotation90(Inverse);
        var lengthPerRow = inner.Length;
        var columns = lengthPerRow / 4;
        _twiddles = new V8[columns * (rows - 1)];
        var n = 0;
        for (var x = 0; x < columns; x++)
            for (var y = 1; y < rows; y++)
                _twiddles[n++] = AvxComplex.MixedRadixTwiddleChunk(x * 4, y, Length, Inverse);
    }

    public override void Transform(ReadOnlySpan<float> input, Span<float> output)
    {
        var rows = _rows;
        var lengthPerRow = _inner.Length;
        var columnsDone = new float[2 * Length];
        Span<V8> column = stackalloc V8[rows];
        Span<V8> butterflied = stackalloc V8[rows];
        for (var c = 0; c < lengthPerRow / 4; c++)
        {
            var indexBase = c * 4;
            for (var i = 0; i < rows; i++)
                column[i] = V8.Load(input, indexBase + lengthPerRow * i);
            if (rows == 4)
                AvxComplex.Butterfly4(column, _rotation, butterflied);
            else
                AvxComplex.Butterfly8(column, _rotation, butterflied);
            butterflied[0].Store(columnsDone, indexBase);
            for (var i = 1; i < rows; i++)
                AvxComplex.MulComplex(_twiddles[c * (rows - 1) + i - 1], butterflied[i]).Store(columnsDone, indexBase + lengthPerRow * i);
        }
        var rowsDone = new float[2 * Length];
        for (var r = 0; r < rows; r++)
            _inner.Transform(columnsDone.AsSpan(2 * r * lengthPerRow, 2 * lengthPerRow), rowsDone.AsSpan(2 * r * lengthPerRow, 2 * lengthPerRow));
        Span<V8> transposed = stackalloc V8[rows];
        for (var c = 0; c < lengthPerRow / 4; c++)
        {
            var inputBase = c * 4;
            var outputBase = inputBase * rows;
            for (var i = 0; i < rows; i++)
                column[i] = V8.Load(rowsDone, inputBase + lengthPerRow * i);
            if (rows == 4)
                AvxComplex.Transpose4Packed(column, transposed);
            else
                AvxComplex.Transpose8Packed(column, transposed);
            for (var k = 0; k < rows; k++)
                transposed[k].Store(output, outputBase + 4 * k);
        }
    }
}

/// <summary>
/// Blink's real FFT (rustfft_ffi.rs, even sizes): a half-size complex FFT and the packing around it;
/// the spectrum is packed as Blink's FFTFrame keeps it (real[0] the DC, imag[0] the Nyquist).
/// </summary>
internal sealed class RealFft
{
    readonly int _halfSize, _limit;
    readonly int? _middle;
    readonly float[] _twiddles; // interleaved: 0.5 · e^(−2πik/size)
    readonly ComplexFft _forward, _inverse;
    readonly float _scale;

    public RealFft(int size)
    {
        if (size % 2 != 0)
            throw new NotSupportedException("odd FFT sizes aren't ported");
        Size = size;
        _halfSize = size / 2;
        _forward = ComplexFft.Plan(_halfSize, false);
        _inverse = ComplexFft.Plan(_halfSize, true);
        _scale = 1.0f / _halfSize;
        _twiddles = new float[2 * _halfSize];
        for (var k = 0; k < _halfSize; k++)
        {
            var angle = -2.0 * Math.PI * k / size;
            _twiddles[2 * k] = (float)(0.5 * Math.Cos(angle));
            _twiddles[2 * k + 1] = (float)(0.5 * Math.Sin(angle));
        }
        _limit = (_halfSize - 1) / 2;
        _middle = _halfSize % 2 == 0 ? _halfSize / 2 : null;
    }

    public int Size { get; }

    /// <summary>do_fft_even: the half-size complex FFT of the samples, then the spectrum reconstructed.</summary>
    public void Forward(ReadOnlySpan<float> data, Span<float> real, Span<float> imag)
    {
        var z = new float[Size];
        _forward.Transform(data[..Size], z);
        real[0] = z[0] + z[1];
        imag[0] = z[0] - z[1];
        for (var k = 1; k <= _limit; k++)
        {
            float zkRe = z[2 * k], zkIm = z[2 * k + 1];
            int m = _halfSize - k;
            float zmRe = z[2 * m], zmIm = z[2 * m + 1];
            float evenRe = 0.5f * (zkRe + zmRe), evenIm = 0.5f * (zkIm - zmIm);
            float oddRe = zkIm + zmIm, oddIm = -(zkRe - zmRe);
            float tRe = _twiddles[2 * k], tIm = _twiddles[2 * k + 1];
            // twiddles[k] * f_odd_unscaled (num-complex: re·re − im·im, re·im + im·re; no fusing)
            float wRe = tRe * oddRe - tIm * oddIm, wIm = tRe * oddIm + tIm * oddRe;
            real[k] = evenRe + wRe;
            imag[k] = evenIm + wIm;
            real[m] = evenRe - wRe;
            imag[m] = -(evenIm - wIm);
        }
        if (_middle is { } middle)
        {
            real[middle] = z[2 * middle];
            imag[middle] = -z[2 * middle + 1];
        }
    }

    /// <summary>do_inverse_fft_even: the half-size complex input prepared, its inverse FFT, scaled by 2 / size.</summary>
    public void Inverse(ReadOnlySpan<float> real, ReadOnlySpan<float> imag, Span<float> data)
    {
        var z = new float[Size];
        z[0] = 0.5f * (real[0] + imag[0]);
        z[1] = 0.5f * (real[0] - imag[0]);
        for (var k = 1; k <= _limit; k++)
        {
            int m = _halfSize - k;
            float rk = real[k], ik = imag[k], rm = real[m], im = imag[m];
            float xkRe = rk, xkIm = ik, xmRe = rm, xmIm = -im;
            // (x_k + x_mk_conj) * 0.5
            float evenRe = (xkRe + xmRe) * 0.5f, evenIm = (xkIm + xmIm) * 0.5f;
            float diffRe = xkRe - xmRe, diffIm = xkIm - xmIm;
            // twiddles[k].conj() * x_diff
            float tRe = _twiddles[2 * k], tIm = -_twiddles[2 * k + 1];
            float oddRe = tRe * diffRe - tIm * diffIm, oddIm = tRe * diffIm + tIm * diffRe;
            float iOddRe = -oddIm, iOddIm = oddRe;
            z[2 * k] = evenRe + iOddRe;
            z[2 * k + 1] = evenIm + iOddIm;
            // (f_even - i_f_odd).conj()
            z[2 * m] = evenRe - iOddRe;
            z[2 * m + 1] = -(evenIm - iOddIm);
        }
        if (_middle is { } middle)
        {
            z[2 * middle] = real[middle];
            z[2 * middle + 1] = -imag[middle];
        }
        var result = new float[Size];
        _inverse.Transform(z, result);
        for (var i = 0; i < Size; i++)
            data[i] = result[i] * _scale;
    }
}
