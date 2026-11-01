using System.Numerics;

namespace Statventure.Audio;

/// <summary>The biquad filter types (Web Audio's BiquadFilterType).</summary>
internal enum BiquadFilterType { Lowpass, Highpass, Bandpass, Lowshelf, Highshelf, Peaking, Notch, Allpass }

/// <summary>
/// One channel's second-order filter (Blink's Biquad, platform/audio/biquad.cc): the Audio EQ
/// Cookbook coefficients with fdlibm's sin, cos and expf, per frame when the parameters move, the
/// direct form I recursion in double with float samples, and the tail length the coefficients ring
/// for. Chromium renders with denormals flushed to zero (DenormalDisabler, FTZ and DAZ): a denormal
/// input reads as zero and a result too small for a normal float becomes zero, as here.
/// </summary>
internal sealed class Biquad
{
    const float SmallestNormal = 1.17549435e-38f;

    readonly double[] _b0 = new double[AudioGraph.RenderQuantumFrames];
    readonly double[] _b1 = new double[AudioGraph.RenderQuantumFrames];
    readonly double[] _b2 = new double[AudioGraph.RenderQuantumFrames];
    readonly double[] _a1 = new double[AudioGraph.RenderQuantumFrames];
    readonly double[] _a2 = new double[AudioGraph.RenderQuantumFrames];
    double _x1, _x2, _y1, _y2;

    public Biquad() => SetNormalizedCoefficients(0, 1, 0, 0, 1, 0, 0);

    public bool HasSampleAccurateValues { get; set; }

    public void Reset() => _x1 = _x2 = _y1 = _y2 = 0;

    /// <summary>DAZ: a denormal float reads as a zero of its sign.</summary>
    static float Daz(float x) => MathF.Abs(x) < SmallestNormal ? (float.IsNegative(x) ? -0.0f : 0.0f) : x;

    /// <summary>FTZ on the double-to-float conversion: a result below the smallest normal becomes a zero of its sign.</summary>
    static float Ftz(double y)
    {
        var f = (float)y;
        return MathF.Abs(f) < SmallestNormal ? (double.IsNegative(y) ? -0.0f : 0.0f) : f;
    }

    /// <summary>Biquad::Process: source may be dest (in place); each sample is read before it's written.</summary>
    public void Process(ReadOnlySpan<float> source, Span<float> dest)
    {
        double x1 = _x1, x2 = _x2, y1 = _y1, y2 = _y2;
        if (HasSampleAccurateValues)
            for (var k = 0; k < source.Length; k++)
            {
                var x = Daz(source[k]);
                var y = Ftz(_b0[k] * x + _b1[k] * x1 + _b2[k] * x2 - _a1[k] * y1 - _a2[k] * y2);
                dest[k] = y;
                x2 = x1;
                x1 = x;
                y2 = y1;
                y1 = y;
            }
        else
        {
            double b0 = _b0[0], b1 = _b1[0], b2 = _b2[0], a1 = _a1[0], a2 = _a2[0];
            for (var k = 0; k < source.Length; k++)
            {
                var x = Daz(source[k]);
                var y = Ftz(b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2);
                dest[k] = y;
                x2 = x1;
                x1 = x;
                y2 = y1;
                y1 = y;
            }
        }
        // FlushDenormalFloatToZero (on x86 the identity: the state holds float values already).
        _x1 = (float)x1;
        _x2 = (float)x2;
        _y1 = (float)y1;
        _y2 = (float)y2;
    }

    /// <summary>pow10 in biquad.cc: fdlibm's expf of x · ln 10, in float.</summary>
    static double Pow10(double x) => Fdlibm.Expf((float)(x * 2.30258509299404568402));

    void SetNormalizedCoefficients(int index, double b0, double b1, double b2, double a0, double a1, double a2)
    {
        var a0Inverse = 1 / a0;
        _b0[index] = b0 * a0Inverse;
        _b1[index] = b1 * a0Inverse;
        _b2[index] = b2 * a0Inverse;
        _a1[index] = a1 * a0Inverse;
        _a2[index] = a2 * a0Inverse;
    }

    public void SetParams(BiquadFilterType type, int index, double frequency, double q, double gain)
    {
        switch (type)
        {
            case BiquadFilterType.Lowpass:
                SetLowpassParams(index, frequency, q);
                break;
            case BiquadFilterType.Highpass:
                SetHighpassParams(index, frequency, q);
                break;
            case BiquadFilterType.Bandpass:
                SetBandpassParams(index, frequency, q);
                break;
            case BiquadFilterType.Lowshelf:
                SetLowShelfParams(index, frequency, gain);
                break;
            case BiquadFilterType.Highshelf:
                SetHighShelfParams(index, frequency, gain);
                break;
            case BiquadFilterType.Peaking:
                SetPeakingParams(index, frequency, q, gain);
                break;
            case BiquadFilterType.Notch:
                SetNotchParams(index, frequency, q);
                break;
            default:
                SetAllpassParams(index, frequency, q);
                break;
        }
    }

    void SetLowpassParams(int index, double cutoff, double resonance)
    {
        cutoff = Math.Clamp(cutoff, 0.0, 1.0);
        if (cutoff == 1)
            SetNormalizedCoefficients(index, 1, 0, 0, 1, 0, 0);
        else if (cutoff > 0)
        {
            resonance = Pow10(resonance / 20);
            var theta = Math.PI * cutoff;
            var alpha = Fdlibm.Sin(theta) / (2 * resonance);
            var cosw = Fdlibm.Cos(theta);
            var beta = (1 - cosw) / 2;
            SetNormalizedCoefficients(index, beta, 2 * beta, beta, 1 + alpha, -2 * cosw, 1 - alpha);
        }
        else
            SetNormalizedCoefficients(index, 0, 0, 0, 1, 0, 0);
    }

    void SetHighpassParams(int index, double cutoff, double resonance)
    {
        cutoff = Math.Clamp(cutoff, 0.0, 1.0);
        if (cutoff == 1)
            SetNormalizedCoefficients(index, 0, 0, 0, 1, 0, 0);
        else if (cutoff > 0)
        {
            resonance = Pow10(resonance / 20);
            var theta = Math.PI * cutoff;
            var alpha = Fdlibm.Sin(theta) / (2 * resonance);
            var cosw = Fdlibm.Cos(theta);
            var beta = (1 + cosw) / 2;
            SetNormalizedCoefficients(index, beta, -2 * beta, beta, 1 + alpha, -2 * cosw, 1 - alpha);
        }
        else
            SetNormalizedCoefficients(index, 1, 0, 0, 1, 0, 0);
    }

    void SetLowShelfParams(int index, double frequency, double dbGain)
    {
        frequency = Math.Clamp(frequency, 0.0, 1.0);
        var a = Pow10(dbGain / 40);
        if (frequency == 1)
            SetNormalizedCoefficients(index, a * a, 0, 0, 1, 0, 0);
        else if (frequency > 0)
        {
            var w0 = Math.PI * frequency;
            double s = 1;
            var alpha = 0.5 * Fdlibm.Sin(w0) * Math.Sqrt((a + 1 / a) * (1 / s - 1) + 2);
            var k = Fdlibm.Cos(w0);
            var k2 = 2 * Math.Sqrt(a) * alpha;
            var aPlusOne = a + 1;
            var aMinusOne = a - 1;
            SetNormalizedCoefficients(index,
                a * (aPlusOne - aMinusOne * k + k2),
                2 * a * (aMinusOne - aPlusOne * k),
                a * (aPlusOne - aMinusOne * k - k2),
                aPlusOne + aMinusOne * k + k2,
                -2 * (aMinusOne + aPlusOne * k),
                aPlusOne + aMinusOne * k - k2);
        }
        else
            SetNormalizedCoefficients(index, 1, 0, 0, 1, 0, 0);
    }

    void SetHighShelfParams(int index, double frequency, double dbGain)
    {
        frequency = Math.Clamp(frequency, 0.0, 1.0);
        var a = Pow10(dbGain / 40);
        if (frequency == 1)
            SetNormalizedCoefficients(index, 1, 0, 0, 1, 0, 0);
        else if (frequency > 0)
        {
            var w0 = Math.PI * frequency;
            double s = 1;
            var alpha = 0.5 * Fdlibm.Sin(w0) * Math.Sqrt((a + 1 / a) * (1 / s - 1) + 2);
            var k = Fdlibm.Cos(w0);
            var k2 = 2 * Math.Sqrt(a) * alpha;
            var aPlusOne = a + 1;
            var aMinusOne = a - 1;
            SetNormalizedCoefficients(index,
                a * (aPlusOne + aMinusOne * k + k2),
                -2 * a * (aMinusOne + aPlusOne * k),
                a * (aPlusOne + aMinusOne * k - k2),
                aPlusOne - aMinusOne * k + k2,
                2 * (aMinusOne - aPlusOne * k),
                aPlusOne - aMinusOne * k - k2);
        }
        else
            SetNormalizedCoefficients(index, a * a, 0, 0, 1, 0, 0);
    }

    void SetPeakingParams(int index, double frequency, double q, double dbGain)
    {
        frequency = Math.Clamp(frequency, 0.0, 1.0);
        q = StdMax0(q);
        var a = Pow10(dbGain / 40);
        if (frequency > 0 && frequency < 1)
        {
            if (q > 0)
            {
                var w0 = Math.PI * frequency;
                var alpha = Fdlibm.Sin(w0) / (2 * q);
                var k = Fdlibm.Cos(w0);
                SetNormalizedCoefficients(index, 1 + alpha * a, -2 * k, 1 - alpha * a, 1 + alpha / a, -2 * k, 1 - alpha / a);
            }
            else
                SetNormalizedCoefficients(index, a * a, 0, 0, 1, 0, 0);
        }
        else
            SetNormalizedCoefficients(index, 1, 0, 0, 1, 0, 0);
    }

    void SetAllpassParams(int index, double frequency, double q)
    {
        frequency = Math.Clamp(frequency, 0.0, 1.0);
        q = StdMax0(q);
        if (frequency > 0 && frequency < 1)
        {
            if (q > 0)
            {
                var w0 = Math.PI * frequency;
                var alpha = Fdlibm.Sin(w0) / (2 * q);
                var k = Fdlibm.Cos(w0);
                SetNormalizedCoefficients(index, 1 - alpha, -2 * k, 1 + alpha, 1 + alpha, -2 * k, 1 - alpha);
            }
            else
                SetNormalizedCoefficients(index, -1, 0, 0, 1, 0, 0);
        }
        else
            SetNormalizedCoefficients(index, 1, 0, 0, 1, 0, 0);
    }

    void SetNotchParams(int index, double frequency, double q)
    {
        frequency = Math.Clamp(frequency, 0.0, 1.0);
        q = StdMax0(q);
        if (frequency > 0 && frequency < 1)
        {
            if (q > 0)
            {
                var w0 = Math.PI * frequency;
                var alpha = Fdlibm.Sin(w0) / (2 * q);
                var k = Fdlibm.Cos(w0);
                SetNormalizedCoefficients(index, 1, -2 * k, 1, 1 + alpha, -2 * k, 1 - alpha);
            }
            else
                SetNormalizedCoefficients(index, 0, 0, 0, 1, 0, 0);
        }
        else
            SetNormalizedCoefficients(index, 1, 0, 0, 1, 0, 0);
    }

    void SetBandpassParams(int index, double frequency, double q)
    {
        frequency = StdMax0(frequency);
        q = StdMax0(q);
        if (frequency > 0 && frequency < 1)
        {
            var w0 = Math.PI * frequency;
            if (q > 0)
            {
                var alpha = Fdlibm.Sin(w0) / (2 * q);
                var k = Fdlibm.Cos(w0);
                SetNormalizedCoefficients(index, alpha, 0, -alpha, 1 + alpha, -2 * k, 1 - alpha);
            }
            else
                SetNormalizedCoefficients(index, 1, 0, 0, 1, 0, 0);
        }
        else
            SetNormalizedCoefficients(index, 0, 0, 0, 1, 0, 0);
    }

    /// <summary>std::max(0.0, x) is 0.0 &lt; x ? x : 0.0.</summary>
    static double StdMax0(double x) => 0.0 < x ? x : 0.0;

    // ---------- The tail (Biquad::TailFrame) ----------

    static double RepeatedRootResponse(double n, double c1, double c2, double r, double logEps) =>
        (n - 2) * Fdlibm.Log(Math.Abs(r)) + Fdlibm.Log(Math.Abs(c1 * (n + 1) * r * r + c2)) - logEps;

    static double RootFinder(double low, double high, double fLow, double fHigh, double logEps, double c1, double c2, double r)
    {
        const double AccuracyThreshold = 0.5;
        const int MaxIterations = 10;
        var side = 0;
        double root = 0;
        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            root = (fLow * high - fHigh * low) / (fLow - fHigh);
            if (Math.Abs(high - low) < AccuracyThreshold * Math.Abs(high + low))
                break;
            var fr = RepeatedRootResponse(root, c1, c2, r, logEps);
            if (fr * fHigh > 0)
            {
                high = root;
                fHigh = fr;
                side = -1;
            }
            else if (fLow * fr > 0)
            {
                low = root;
                fLow = fr;
                if (side == 1)
                    fHigh /= 2;
                side = 1;
            }
            else
                break;
        }
        return root;
    }

    /// <summary>
    /// How many frames the filter rings for after its input stops, until its response is below
    /// 1/32768 (from the poles of the coefficients at <paramref name="index"/>).
    /// </summary>
    public double TailFrame(int index, double maxFrame)
    {
        const double MaxTailAmplitude = 1 / 32768.0;
        double a1 = _a1[index], a2 = _a2[index], b0 = _b0[index], b1 = _b1[index], b2 = _b2[index];
        if (!double.IsFinite(a1) || !double.IsFinite(a2) || !double.IsFinite(b0) || !double.IsFinite(b1) || !double.IsFinite(b2))
            return 0;
        double tailFrame;
        var discrim = a1 * a1 - 4 * a2;
        if (discrim > 0)
        {
            var rplus = (-a1 + Math.Sqrt(discrim)) / 2;
            var rminus = (-a1 - Math.Sqrt(discrim)) / 2;
            var r1 = Math.Abs(rplus) >= Math.Abs(rminus) ? rplus : rminus;
            var r2 = a2 / r1;
            var c1 = (b0 * r1 * r1 + b1 * r1 + b2) / (r2 - r1);
            var c2 = (b0 * r2 * r2 + b1 * r2 + b2) / (r2 - r1);
            // ClampTo(x, 0): x < 0 ? 0 : x.
            var t = 1 + Fdlibm.Log(MaxTailAmplitude / (Math.Abs(c1) + Math.Abs(c2))) / Fdlibm.Log(Math.Abs(r1));
            tailFrame = t < 0 ? 0 : t;
        }
        else if (discrim < 0)
        {
            var x = -a1 / 2;
            var y = Math.Sqrt(-discrim) / 2;
            var r1 = new Complex(x, y);
            var r2 = new Complex(x, -y);
            var r = Hypot(x, y);
            if (r == 1)
                tailFrame = maxFrame;
            else
            {
                var c1 = ComplexAbs(Divide(Add(Add(Multiply(Scale(r1, b0), r1), Scale(r1, b1)), b2), Sub(r2, r1)));
                var c2 = ComplexAbs(Divide(Add(Add(Multiply(Scale(r2, b0), r2), Scale(r2, b1)), b2), Sub(r2, r1)));
                tailFrame = 1 + Fdlibm.Log(MaxTailAmplitude / (c1 + c2)) / Fdlibm.Log(r);
                if (c1 == 0 && c2 == 0)
                    tailFrame = 0;
            }
        }
        else
        {
            var r = -a1 / 2;
            if (r == 0)
                tailFrame = 2;
            else if (Math.Abs(r) >= 1)
                tailFrame = double.PositiveInfinity;
            else
            {
                var c1 = (b0 * r * r + b1 * r + b2) / (r * r);
                var c2 = b1 * r + 2 * b2;
                if (c1 == 0 && c2 == 0)
                    tailFrame = 0;
                else
                {
                    var logR = Fdlibm.Log(Math.Abs(r));
                    var low = Math.Clamp(-(1 + logR) / logR, 1.0, maxFrame - 1);
                    var high = maxFrame;
                    var logEps = Fdlibm.Log(MaxTailAmplitude);
                    var fLow = RepeatedRootResponse(low, c1, c2, r, logEps);
                    var fHigh = RepeatedRootResponse(high, c1, c2, r, logEps);
                    tailFrame = fLow <= 0 ? 0 : fHigh >= 0 ? high : RootFinder(low, high, fLow, fHigh, logEps, c1, c2, r);
                }
            }
        }
        return tailFrame;
    }

    // libc++'s std::complex<double> arithmetic, unfused (its scaled division rounds the same as the plain formula).
    static Complex Scale(Complex z, double s) => new(z.Real * s, z.Imaginary * s);
    static Complex Add(Complex a, Complex b) => new(a.Real + b.Real, a.Imaginary + b.Imaginary);
    static Complex Add(Complex a, double b) => new(a.Real + b, a.Imaginary);
    static Complex Sub(Complex a, Complex b) => new(a.Real - b.Real, a.Imaginary - b.Imaginary);
    static Complex Multiply(Complex a, Complex b) =>
        new(a.Real * b.Real - a.Imaginary * b.Imaginary, a.Real * b.Imaginary + a.Imaginary * b.Real);

    static Complex Divide(Complex z, Complex w)
    {
        double a = z.Real, b = z.Imaginary, c = w.Real, d = w.Imaginary;
        // libc++: scaled by 2^-logb(max(|c|, |d|)) when that's finite (exact, so the plain formula's rounding).
        var m = Math.Max(Math.Abs(c), Math.Abs(d));
        var logbw = m == 0 || !double.IsFinite(m) ? 0 : Math.ILogB(m);
        c = Math.ScaleB(c, -logbw);
        d = Math.ScaleB(d, -logbw);
        var denom = c * c + d * d;
        var x = Math.ScaleB((a * c + b * d) / denom, -logbw);
        var y = Math.ScaleB((b * c - a * d) / denom, -logbw);
        return new Complex(x, y);
    }

    static double ComplexAbs(Complex z) => Hypot(z.Real, z.Imaginary);

    /// <summary>hypot (the C library's in Blink; within an ulp here: the tail decides only which quantum the node stops in).</summary>
    static double Hypot(double x, double y) => double.Hypot(x, y);
}

/// <summary>
/// BiquadFilterNode (Blink's BiquadFilterHandler): one Biquad per channel, coefficients per frame while
/// a parameter moves (once when they hold still within the quantum), else recomputed only when a
/// value changed; the input pulled into the output's bus (in place); the tail from the coefficients
/// keeping the node processing after its input falls silent; a type or channel change resets the state.
/// </summary>
internal sealed class BiquadFilterNode : AudioNode
{
    const double MaxTailTime = 30.0;

    readonly float[] _frequencyValues = new float[AudioGraph.RenderQuantumFrames];
    readonly float[] _qValues = new float[AudioGraph.RenderQuantumFrames];
    readonly float[] _gainValues = new float[AudioGraph.RenderQuantumFrames];
    readonly float[] _detuneValues = new float[AudioGraph.RenderQuantumFrames];
    readonly List<Biquad> _biquads = [];
    readonly double _nyquist;
    BiquadFilterType _type;
    bool _hasJustReset = true;
    float _previousFrequency = float.NaN, _previousQ = float.NaN, _previousGain = float.NaN, _previousDetune = float.NaN;
    double _tailTime = double.PositiveInfinity;

    public BiquadFilterNode(AudioGraph graph, BiquadFilterType type = BiquadFilterType.Lowpass) : base(graph)
    {
        _nyquist = 0.5 * graph.SampleRateF;
        Frequency = new AudioParam(graph, 350, 0, graph.SampleRateF / 2);
        Q = new AudioParam(graph, 1);
        Gain = new AudioParam(graph, 0, float.MinValue, 40 * (float)Math.Log10(float.MaxValue));
        var detuneLimit = 1200 * Libm.Log2f(float.MaxValue);
        Detune = new AudioParam(graph, 0, -detuneLimit, detuneLimit);
        _type = type;
        AddInput();
        AddOutput(1);
        _biquads.Add(new Biquad());
    }

    public AudioParam Frequency { get; }
    public AudioParam Q { get; }
    public AudioParam Gain { get; }
    public AudioParam Detune { get; }

    public BiquadFilterType Type
    {
        get => _type;
        set
        {
            if (value == _type)
                return;
            _type = value;
            foreach (var biquad in _biquads)
                biquad.Reset();
            _hasJustReset = true;
        }
    }

    protected override double TailTime => _tailTime;

    internal override void CheckNumberOfChannelsForInput(AudioNodeInput input)
    {
        var channels = input.NumberOfChannels;
        if (channels != Output(0).NumberOfChannels)
        {
            // Uninitialize, then Initialize with new (cleared) filters.
            _biquads.Clear();
            Output(0).SetNumberOfChannels(channels);
            for (var i = 0; i < channels; i++)
                _biquads.Add(new Biquad());
            _hasJustReset = true;
        }
        base.CheckNumberOfChannelsForInput(input);
    }

    protected override void PullInputs(int frames) => Input(0).Pull(Output(0).Bus, frames);

    /// <summary>NormalizeFrequency: frequency / Nyquist, times exp2f(detune / 1200) when detuned.</summary>
    double NormalizeFrequency(float frequency, float detune)
    {
        var normalized = frequency / _nyquist;
        return detune != 0 ? normalized * Libm.Exp2f(detune / 1200) : normalized;
    }

    static bool HasConstantValues(ReadOnlySpan<float> values)
    {
        for (var i = 1; i < values.Length; i++)
            if (values[i] != values[0])
                return false;
        return true;
    }

    protected override void Process(int frames)
    {
        var destination = Output(0).Bus;
        var source = Input(0).Bus;
        if (!Input(0).IsConnected)
            source.Zero();
        var dirty = false;
        var sampleAccurate = false;
        var audioRate = false;
        if (Frequency.HasSampleAccurateValues() || Q.HasSampleAccurateValues() || Gain.HasSampleAccurateValues() || Detune.HasSampleAccurateValues())
        {
            dirty = true;
            sampleAccurate = true;
            audioRate = Frequency.IsAudioRate || Q.IsAudioRate || Gain.IsAudioRate || Detune.IsAudioRate;
        }
        else if (_hasJustReset)
        {
            _previousFrequency = _previousQ = _previousGain = _previousDetune = float.NaN;
            dirty = true;
            _hasJustReset = false;
        }
        else
        {
            float frequency = Frequency.FinalValue(), q = Q.FinalValue(), gain = Gain.FinalValue(), detune = Detune.FinalValue();
            if (_previousFrequency != frequency || _previousQ != q || _previousGain != gain || _previousDetune != detune)
            {
                dirty = true;
                _previousFrequency = frequency;
                _previousQ = q;
                _previousGain = gain;
                _previousDetune = detune;
            }
        }
        if (dirty)
        {
            if (sampleAccurate && audioRate)
            {
                Frequency.CalculateSampleAccurateValues(_frequencyValues.AsSpan(0, frames));
                Q.CalculateSampleAccurateValues(_qValues.AsSpan(0, frames));
                Gain.CalculateSampleAccurateValues(_gainValues.AsSpan(0, frames));
                Detune.CalculateSampleAccurateValues(_detuneValues.AsSpan(0, frames));
                var constant = HasConstantValues(_frequencyValues.AsSpan(0, frames)) && HasConstantValues(_qValues.AsSpan(0, frames))
                    && HasConstantValues(_gainValues.AsSpan(0, frames)) && HasConstantValues(_detuneValues.AsSpan(0, frames));
                var neededFrames = constant ? 1 : frames;
                foreach (var biquad in _biquads)
                {
                    biquad.HasSampleAccurateValues = neededFrames > 1;
                    for (var k = 0; k < neededFrames; k++)
                        biquad.SetParams(_type, k, NormalizeFrequency(_frequencyValues[k], _detuneValues[k]), _qValues[k], _gainValues[k]);
                }
                SetTail(neededFrames - 1);
            }
            else
            {
                _frequencyValues[0] = Frequency.FinalValue();
                _qValues[0] = Q.FinalValue();
                _gainValues[0] = Gain.FinalValue();
                _detuneValues[0] = Detune.FinalValue();
                foreach (var biquad in _biquads)
                {
                    biquad.HasSampleAccurateValues = false;
                    biquad.SetParams(_type, 0, NormalizeFrequency(_frequencyValues[0], _detuneValues[0]), _qValues[0], _gainValues[0]);
                }
                SetTail(0);
            }
        }
        for (var i = 0; i < _biquads.Count; i++)
            _biquads[i].Process(source.Channel(i).Data.AsSpan(0, frames), destination.Channel(i).Mutable[..frames]);
    }

    void SetTail(int index)
    {
        var sampleRate = (double)Graph.SampleRateF;
        var tail = _biquads[0].TailFrame(index, MaxTailTime * sampleRate) / sampleRate;
        _tailTime = Math.Clamp(tail, 0.0, MaxTailTime);
    }

    protected override void ProcessOnlyAudioParams(int frames)
    {
        var values = _frequencyValues.AsSpan(0, frames);
        Frequency.CalculateSampleAccurateValues(values);
        Q.CalculateSampleAccurateValues(values);
        Gain.CalculateSampleAccurateValues(values);
        Detune.CalculateSampleAccurateValues(values);
    }
}
