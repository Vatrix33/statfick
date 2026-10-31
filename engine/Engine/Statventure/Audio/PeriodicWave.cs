namespace Statventure.Audio;

/// <summary>The oscillator's built-in shapes, and custom for a periodic wave.</summary>
internal enum OscillatorType { Sine, Square, Sawtooth, Triangle, Custom }

/// <summary>
/// Band-limited wave tables (Blink's PeriodicWaveImpl): one table per third of an octave, each the
/// inverse FFT (Blink's Rust FFT) of the harmonics a fundamental in that range can carry below
/// Nyquist, normalized by the first table's peak.
/// </summary>
internal sealed class PeriodicWave
{
    const int NumberOfOctaveBands = 3;
    const float CentsPerRange = 1200 / NumberOfOctaveBands;
    const int MaxPeriodicWaveSize = 16384;

    readonly float[][] _tables;

    PeriodicWave(float sampleRate)
    {
        SampleRate = sampleRate;
        Size = sampleRate <= 24000 ? 2048 : sampleRate <= 88200 ? 4096 : MaxPeriodicWaveSize;
        var nyquist = (float)(0.5 * sampleRate);
        LowestFundamentalFrequency = nyquist / MaxNumberOfPartials;
        RateScale = Size / sampleRate;
        NumberOfRanges = (int)(0.5 + NumberOfOctaveBands * Libm.Log2f(Size));
        _tables = new float[NumberOfRanges][];
    }

    public float SampleRate { get; }
    /// <summary>PeriodicWaveSize(): 2048 up to 24 kHz, 4096 up to 88.2 kHz, else 16384.</summary>
    public int Size { get; }
    public int NumberOfRanges { get; }
    public float RateScale { get; }
    public float LowestFundamentalFrequency { get; }
    int MaxNumberOfPartials => Size / 2;

    public float[] Table(int range) => _tables[range];

    /// <summary>createPeriodicWave(real, imag, { disableNormalization }).</summary>
    public static PeriodicWave Create(float sampleRate, ReadOnlySpan<float> real, ReadOnlySpan<float> imag, bool disableNormalization = false)
    {
        if (real.Length != imag.Length)
            throw new ArgumentException("the real and imaginary arrays must have the same length");
        if (real.Length < 2)
            throw new ArgumentException("a periodic wave needs at least 2 coefficients");
        var wave = new PeriodicWave(sampleRate);
        wave.CreateBandLimitedTables(real, imag, disableNormalization);
        return wave;
    }

    /// <summary>GenerateBasicWaveform: the Fourier series of the shape (sine terms only), band-limited like a custom wave.</summary>
    public static PeriodicWave Basic(float sampleRate, OscillatorType shape)
    {
        var wave = new PeriodicWave(sampleRate);
        var halfSize = wave.Size / 2;
        var real = new float[halfSize];
        var imag = new float[halfSize];
        const float PiFloat = (float)Math.PI;
        for (var n = 1; n < halfSize; n++)
        {
            var piFactor = 2 / (n * PiFloat);
            var b = shape switch
            {
                OscillatorType.Sine => n == 1 ? 1f : 0f,
                OscillatorType.Square => (n & 1) != 0 ? 2 * piFactor : 0f,
                OscillatorType.Sawtooth => piFactor * ((n & 1) != 0 ? 1 : -1),
                OscillatorType.Triangle => (n & 1) != 0 ? 2 * (piFactor * piFactor) * ((((n - 1) >> 1) & 1) != 0 ? -1 : 1) : 0f,
                _ => throw new ArgumentOutOfRangeException(nameof(shape)),
            };
            real[n] = 0;
            imag[n] = b;
        }
        wave.CreateBandLimitedTables(real, imag, false);
        return wave;
    }

    int NumberOfPartialsForRange(int rangeIndex)
    {
        var centsToCull = rangeIndex * CentsPerRange;
        // pow(2, x) in double (the C library's), as Blink's call to pow with an int and a float.
        var cullingScale = (float)Math.Pow(2, -centsToCull / 1200);
        return (int)(uint)(cullingScale * MaxNumberOfPartials);
    }

    void CreateBandLimitedTables(ReadOnlySpan<float> realData, ReadOnlySpan<float> imagData, bool disableNormalization)
    {
        var normalizationScale = 0.5f;
        var fftSize = Size;
        var halfSize = fftSize / 2;
        var components = Math.Min(realData.Length, halfSize);
        var fft = new RealFft(fftSize);
        // FFTFrame's real and imaginary arrays, reused from range to range.
        var real = new float[halfSize];
        var imag = new float[halfSize];
        for (var range = 0; range < NumberOfRanges; range++)
        {
            float scale = fftSize;
            VectorMath.Vsmul(realData[..components], scale, real.AsSpan(0, components));
            scale = -scale;
            VectorMath.Vsmul(imagData[..components], scale, imag.AsSpan(0, components));
            var partials = NumberOfPartialsForRange(range);
            for (var i = Math.Min(components, partials + 1); i < halfSize; i++)
                real[i] = imag[i] = 0;
            real[0] = 0;
            imag[0] = 0;
            var table = new float[fftSize];
            fft.Inverse(real, imag, table);
            if (!disableNormalization && range == 0)
            {
                var max = 0f;
                foreach (var v in table)
                    max = Math.Max(max, Math.Abs(v));
                if (max != 0)
                    normalizationScale = 1.0f / max;
            }
            VectorMath.Vsmul(table, normalizationScale, table);
            _tables[range] = table;
        }
    }

    /// <summary>WaveDataForFundamentalFrequency: the two tables around the frequency's range (lower: fewer partials) and where it lies between them.</summary>
    public (float[] Lower, float[] Higher, float Factor) WaveDataFor(float fundamentalFrequency)
    {
        fundamentalFrequency = MathF.Abs(fundamentalFrequency);
        var ratio = fundamentalFrequency > 0 ? fundamentalFrequency / LowestFundamentalFrequency : 0.5f;
        var centsAboveLowestFrequency = Libm.Log2f(ratio) * 1200;
        var pitchRange = 1 + centsAboveLowestFrequency / CentsPerRange;
        // std::max(a, b) is a < b ? b : a, std::min(a, b) is b < a ? b : a.
        pitchRange = pitchRange < 0.0f ? 0.0f : pitchRange;
        var top = (float)(NumberOfRanges - 1);
        pitchRange = top < pitchRange ? top : pitchRange;
        var rangeIndex1 = (int)(uint)pitchRange;
        var rangeIndex2 = rangeIndex1 < NumberOfRanges - 1 ? rangeIndex1 + 1 : rangeIndex1;
        return (_tables[rangeIndex2], _tables[rangeIndex1], pitchRange - rangeIndex1);
    }
}
