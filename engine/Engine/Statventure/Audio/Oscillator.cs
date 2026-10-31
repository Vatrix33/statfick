namespace Statventure.Audio;

/// <summary>
/// A periodic wave played at a frequency (Blink's OscillatorHandler with its x86 SSE2 kernels,
/// oscillator_kernel_sse2.cc): frequency and detune a-rate, phase increments per sample when either
/// is automated, else one per quantum; the band-limited tables of the pitch's range interpolated
/// between two ranges; 2-, 3- or 5-point interpolation by the increment.
/// </summary>
internal sealed class OscillatorNode : AudioScheduledSourceNode
{
    const float Interpolate2Point = 0.3f;
    const float Interpolate3Point = 0.16f;

    PeriodicWave _wave;
    OscillatorType _type;
    double _virtualReadIndex;
    readonly float[] _phaseIncrements = new float[AudioGraph.RenderQuantumFrames];
    readonly float[] _detuneValues = new float[AudioGraph.RenderQuantumFrames];

    public OscillatorNode(AudioGraph graph, OscillatorType type = OscillatorType.Sine) : base(graph)
    {
        var nyquist = graph.SampleRateF / 2;
        Frequency = new AudioParam(graph, 440, -nyquist, nyquist);
        var detuneLimit = 1200 * Libm.Log2f(float.MaxValue);
        Detune = new AudioParam(graph, 0, -detuneLimit, detuneLimit);
        AddOutput(1);
        if (type == OscillatorType.Custom)
            throw new ArgumentException("a custom oscillator is made with SetPeriodicWave");
        _type = type;
        _wave = graph.BasicWave(type);
    }

    public AudioParam Frequency { get; }
    public AudioParam Detune { get; }

    public OscillatorType Type
    {
        get => _type;
        set
        {
            if (value == OscillatorType.Custom)
                throw new InvalidOperationException("'type' can't be set to custom: use SetPeriodicWave");
            _type = value;
            _wave = Graph.BasicWave(value);
        }
    }

    public void SetPeriodicWave(PeriodicWave wave)
    {
        if (wave.SampleRate != Graph.SampleRateF)
            throw new ArgumentException("the periodic wave was made for another sample rate");
        _wave = wave;
        _type = OscillatorType.Custom;
    }

    protected override bool PropagatesSilence => !IsPlayingOrScheduled || HasFinished;

    internal override void HandleStoppableSourceNode()
    {
        // kExtraStopFrames / sampleRate is a float division in Blink.
        if (EndTime != UnknownTime && IsPlayingOrScheduled && Graph.CurrentTime >= EndTime + ExtraStopFrames / Graph.SampleRateF)
            Finish();
    }

    /// <summary>DetuneToFrequencyMultiplier: exp2f(detune / 1200).</summary>
    static float DetuneMultiplier(float detune) => Libm.Exp2f(detune / 1200);

    static void ClampFrequency(Span<float> frequency, float nyquist)
    {
        for (var i = 0; i < frequency.Length; i++)
        {
            var f = frequency[i];
            frequency[i] = float.IsNaN(f) ? nyquist : Math.Clamp(f, -nyquist, nyquist);
        }
    }

    bool CalculateSampleAccuratePhaseIncrements(int frames)
    {
        var hasSampleAccurateValues = false;
        var hasFrequencyChanges = false;
        var phaseIncrements = _phaseIncrements.AsSpan(0, frames);
        var finalScale = _wave.RateScale;
        if (Frequency.HasSampleAccurateValues() && Frequency.IsAudioRate)
        {
            hasSampleAccurateValues = true;
            hasFrequencyChanges = true;
            Frequency.CalculateSampleAccurateValues(phaseIncrements);
        }
        else
            finalScale *= Frequency.FinalValue();
        if (Detune.HasSampleAccurateValues() && Detune.IsAudioRate)
        {
            hasSampleAccurateValues = true;
            var detuneValues = hasFrequencyChanges ? _detuneValues.AsSpan(0, frames) : phaseIncrements;
            Detune.CalculateSampleAccurateValues(detuneValues);
            var k = (float)(1.0 / 1200);
            VectorMath.Vsmul(detuneValues, k, detuneValues);
            for (var i = 0; i < frames; i++)
                detuneValues[i] = Libm.Exp2f(detuneValues[i]);
            if (hasFrequencyChanges)
                VectorMath.Vmul(detuneValues, phaseIncrements, phaseIncrements);
        }
        else
            finalScale *= DetuneMultiplier(Detune.FinalValue());
        if (hasSampleAccurateValues)
        {
            ClampFrequency(phaseIncrements, Graph.SampleRateF / 2);
            VectorMath.Vsmul(phaseIncrements, finalScale, phaseIncrements);
        }
        return hasSampleAccurateValues;
    }

    protected override void Process(int frames)
    {
        var output = Output(0).Bus;
        var (quantumFrameOffset, nonSilentFrames, startFrameOffset) = UpdateSchedulingInfo(frames, output);
        if (nonSilentFrames == 0)
        {
            output.Zero();
            return;
        }
        var virtualReadIndex = _virtualReadIndex;
        var rateScale = _wave.RateScale;
        var hasSampleAccurateValues = CalculateSampleAccuratePhaseIncrements(frames);
        float frequency = 0;
        if (!hasSampleAccurateValues)
        {
            frequency = Frequency.FinalValue();
            frequency *= DetuneMultiplier(Detune.FinalValue());
            Span<float> one = [frequency];
            ClampFrequency(one, Graph.SampleRateF / 2);
            frequency = one[0];
        }
        var destinationIndex = quantumFrameOffset;
        var n = nonSilentFrames;
        if (startFrameOffset > 0)
        {
            destinationIndex++;
            n--;
            virtualReadIndex += (1 - startFrameOffset) * frequency * rateScale;
        }
        else if (startFrameOffset < 0)
            virtualReadIndex = -startFrameOffset * frequency * rateScale;
        var destination = output.Channel(0).Mutable.Slice(destinationIndex, n);
        // Blink passes the increments from the quantum's first frame, wherever the output starts.
        virtualReadIndex = hasSampleAccurateValues
            ? ProcessARate(n, destination, virtualReadIndex, _phaseIncrements)
            : ProcessKRate(n, destination, virtualReadIndex);
        _virtualReadIndex = virtualReadIndex;
        output.ClearSilentFlag();
    }

    double Wrap(double virtualReadIndex)
    {
        var size = _wave.Size;
        return virtualReadIndex - Math.Floor(virtualReadIndex * (1.0 / size)) * size;
    }

    double ProcessKRate(int n, Span<float> destination, double virtualReadIndex)
    {
        var size = _wave.Size;
        var mask = (uint)(size - 1);
        var frequency = Frequency.FinalValue();
        frequency *= DetuneMultiplier(Detune.FinalValue());
        Span<float> one = [frequency];
        ClampFrequency(one, Graph.SampleRateF / 2);
        frequency = one[0];
        var (lower, higher, tableFactor) = _wave.WaveDataFor(frequency);
        var rateScale = _wave.RateScale;
        var incr = frequency * rateScale;
        if (incr >= Interpolate2Point)
        {
            var (k, vIndex) = ProcessKRateVector(n, destination, virtualReadIndex, frequency, rateScale);
            if (k < n)
                ProcessKRateScalar(k, n, destination, vIndex, frequency, rateScale);
            // n · incr is a float product in Blink.
            virtualReadIndex += n * incr;
            return Wrap(virtualReadIndex);
        }
        for (var k = 0; k < n; k++)
        {
            destination[k] = DoInterpolation(virtualReadIndex, MathF.Abs(incr), mask, tableFactor, lower, higher);
            virtualReadIndex += incr;
            virtualReadIndex = Wrap(virtualReadIndex);
        }
        return virtualReadIndex;
    }

    /// <summary>WrapVirtualIndexVector: x − floor(x / size) · size in float lanes (truncation corrected for negatives).</summary>
    static float WrapLane(float x, float waveSize, float invWaveSize)
    {
        var r = x * invWaveSize;
        var f = Truncate(r);
        if (r < f)
            f -= 1;
        return x - (float)f * waveSize;
    }

    /// <summary>cvttps2dq: truncation toward zero, int.MinValue when out of range.</summary>
    static int Truncate(float x) => float.IsNaN(x) || x >= 2147483648f || x < -2147483648f ? int.MinValue : (int)x;
    static int Truncate(double x) => double.IsNaN(x) || x >= 2147483648.0 || x < -2147483648.0 ? int.MinValue : (int)x;

    (int, double) ProcessKRateVector(int n, Span<float> destination, double virtualReadIndex, float frequency, float rateScale)
    {
        var size = _wave.Size;
        var invSize = 1.0 / size;
        var incr = frequency * rateScale;
        var (lower, higher, tableFactor) = _wave.WaveDataFor(frequency);
        float waveSize = size;
        var invWaveSize = 1.0f / size;
        var mask = (uint)(size - 1);
        var vIncr = 4 * incr;
        Span<float> lanes = stackalloc float[4];
        for (var j = 0; j < 4; j++)
            lanes[j] = WrapLane((float)(virtualReadIndex + j * incr), waveSize, invWaveSize);
        var k = 0;
        for (var loop = 0; loop < n / 4; loop++, k += 4)
        {
            for (var m = 0; m < 4; m++)
            {
                var r0 = (uint)Truncate(lanes[m]) & mask;
                var r1 = (r0 + 1) & mask;
                float s1Lower = lower[r0], s2Lower = lower[r1], s1Higher = higher[r0], s2Higher = higher[r1];
                var interpolationFactor = lanes[m] - (int)r0;
                var sampleHigher = s1Higher + interpolationFactor * (s2Higher - s1Higher);
                var sampleLower = s1Lower + interpolationFactor * (s2Lower - s1Lower);
                destination[k + m] = sampleHigher + tableFactor * (sampleLower - sampleHigher);
                lanes[m] = WrapLane(lanes[m] + vIncr, waveSize, invWaveSize);
            }
        }
        virtualReadIndex += k * incr;
        virtualReadIndex -= Math.Floor(virtualReadIndex * invSize) * size;
        return (k, virtualReadIndex);
    }

    double ProcessKRateScalar(int start, int n, Span<float> destination, double virtualReadIndex, float frequency, float rateScale)
    {
        var size = _wave.Size;
        var invSize = 1.0 / size;
        var mask = (uint)(size - 1);
        var (lower, higher, tableFactor) = _wave.WaveDataFor(frequency);
        var incr = frequency * rateScale;
        for (var k = start; k < n; k++)
        {
            var r0 = (uint)virtualReadIndex & mask;
            var r1 = (r0 + 1) & mask;
            float s1Lower = lower[r0], s2Lower = lower[r1], s1Higher = higher[r0], s2Higher = higher[r1];
            var interpolationFactor = (float)virtualReadIndex - r0;
            var sampleHigher = s1Higher + interpolationFactor * (s2Higher - s1Higher);
            var sampleLower = s1Lower + interpolationFactor * (s2Lower - s1Lower);
            destination[k] = sampleHigher + tableFactor * (sampleLower - sampleHigher);
            virtualReadIndex += incr;
            virtualReadIndex -= Math.Floor(virtualReadIndex * invSize) * size;
        }
        return virtualReadIndex;
    }

    /// <summary>DoInterpolation: linear between table samples for big increments, else 3-point or 5-point Lagrange, in double.</summary>
    static float DoInterpolation(double virtualReadIndex, float incr, uint mask, float tableFactor, float[] lower, float[] higher)
    {
        double sampleLower = 0, sampleHigher = 0;
        var readIndex0 = (uint)virtualReadIndex;
        if (incr >= Interpolate2Point)
        {
            var readIndex2 = readIndex0 + 1;
            readIndex0 &= mask;
            readIndex2 &= mask;
            float s1Lower = lower[readIndex0], s2Lower = lower[readIndex2], s1Higher = higher[readIndex0], s2Higher = higher[readIndex2];
            double interpolationFactor = (float)virtualReadIndex - readIndex0;
            sampleHigher = (1 - interpolationFactor) * s1Higher + interpolationFactor * s2Higher;
            sampleLower = (1 - interpolationFactor) * s1Lower + interpolationFactor * s2Lower;
        }
        else if (incr >= Interpolate3Point)
        {
            Span<uint> readIndex = stackalloc uint[3];
            for (var k = -1; k <= 1; k++)
                readIndex[k + 1] = unchecked((uint)(readIndex0 + k)) & mask;
            var t = virtualReadIndex - readIndex0;
            Span<double> a = [0.5 * t * (t - 1), 1 - t * t, 0.5 * t * (t + 1)];
            for (var k = 0; k < 3; k++)
            {
                sampleLower += a[k] * lower[readIndex[k]];
                sampleHigher += a[k] * higher[readIndex[k]];
            }
        }
        else
        {
            Span<uint> readIndex = stackalloc uint[5];
            for (var k = -2; k <= 2; k++)
                readIndex[k + 2] = unchecked((uint)(readIndex0 + k)) & mask;
            var t = virtualReadIndex - readIndex0;
            var t2 = t * t;
            Span<double> a =
            [
                t * (t2 - 1) * (t - 2) / 24,
                -t * (t - 1) * (t2 - 4) / 6,
                (t2 - 1) * (t2 - 4) / 4,
                -t * (t + 1) * (t2 - 4) / 6,
                t * (t2 - 1) * (t + 2) / 24,
            ];
            for (var k = 0; k < 5; k++)
            {
                sampleLower += a[k] * lower[readIndex[k]];
                sampleHigher += a[k] * higher[readIndex[k]];
            }
        }
        return (float)((1 - tableFactor) * sampleHigher + tableFactor * sampleLower);
    }

    double ProcessARate(int n, Span<float> destination, double virtualReadIndex, ReadOnlySpan<float> phaseIncrements)
    {
        var (framesProcessed, index) = ProcessARateVector(n, destination, virtualReadIndex, phaseIncrements);
        return ProcessARateScalar(framesProcessed, n, destination, index, phaseIncrements);
    }

    (int, double) ProcessARateVector(int n, Span<float> destination, double virtualReadIndex, ReadOnlySpan<float> phaseIncrements)
    {
        var rateScale = _wave.RateScale;
        var invRateScale = 1 / rateScale;
        var size = _wave.Size;
        var invSize = 1.0 / size;
        var mask = (uint)(size - 1);
        var lower = new float[4][];
        var higher = new float[4][];
        Span<float> tableFactor = stackalloc float[4];
        var k = 0;
        for (var loop = 0; loop < n / 4; loop++, k += 4)
        {
            var isBigIncrement = true;
            for (var m = 0; m < 4; m++)
            {
                var phaseIncrement = phaseIncrements[k + m];
                isBigIncrement = isBigIncrement && MathF.Abs(phaseIncrement) >= Interpolate2Point;
                (lower[m], higher[m], tableFactor[m]) = _wave.WaveDataFor(invRateScale * phaseIncrement);
            }
            if (isBigIncrement)
                virtualReadIndex = ProcessARateVectorKernel(destination[k..], virtualReadIndex, phaseIncrements[k..], size, lower, higher, tableFactor);
            else
                for (var m = 0; m < 4; m++)
                {
                    destination[k + m] = DoInterpolation(virtualReadIndex, MathF.Abs(phaseIncrements[k + m]), mask, tableFactor[m], lower[m], higher[m]);
                    virtualReadIndex += phaseIncrements[k + m];
                    virtualReadIndex -= Math.Floor(virtualReadIndex * invSize) * size;
                }
        }
        return (k, virtualReadIndex);
    }

    /// <summary>The SSE2 ProcessARateVectorKernel: four read positions in double (wrapped), samples interpolated in float lanes.</summary>
    static double ProcessARateVectorKernel(Span<float> destination, double virtualReadIndex, ReadOnlySpan<float> phaseIncrements, int size,
        float[][] lower, float[][] higher, ReadOnlySpan<float> tableFactor)
    {
        var invSize = 1.0 / size;
        var mask = (uint)(size - 1);
        Span<double> incrSum = stackalloc double[4];
        incrSum[0] = phaseIncrements[0];
        for (var m = 1; m < 4; m++)
            incrSum[m] = incrSum[m - 1] + phaseIncrements[m];
        Span<double> readIndex = [virtualReadIndex, virtualReadIndex + incrSum[0], virtualReadIndex + incrSum[1], virtualReadIndex + incrSum[2]];
        for (var m = 0; m < 4; m++)
        {
            // WrapVirtualIndexVectorPd
            var r = readIndex[m] * invSize;
            double f = Truncate(r);
            if (r < f)
                f -= 1;
            readIndex[m] -= f * size;
        }
        for (var m = 0; m < 4; m++)
        {
            var read0 = (uint)Truncate(readIndex[m]) & mask;
            var read1 = (read0 + 1) & mask;
            float s1Lower = lower[m][read0], s2Lower = lower[m][read1], s1Higher = higher[m][read0], s2Higher = higher[m][read1];
            var factor = (float)readIndex[m] - (int)read0;
            var sampleHigher = s1Higher + factor * (s2Higher - s1Higher);
            var sampleLower = s1Lower + factor * (s2Lower - s1Lower);
            destination[m] = sampleHigher + tableFactor[m] * (sampleLower - sampleHigher);
        }
        virtualReadIndex += incrSum[3];
        virtualReadIndex -= Math.Floor(virtualReadIndex * invSize) * size;
        return virtualReadIndex;
    }

    double ProcessARateScalar(int start, int n, Span<float> destination, double virtualReadIndex, ReadOnlySpan<float> phaseIncrements)
    {
        var rateScale = _wave.RateScale;
        var invRateScale = 1 / rateScale;
        var size = _wave.Size;
        var invSize = 1.0 / size;
        var mask = (uint)(size - 1);
        for (var m = start; m < n; m++)
        {
            var incr = phaseIncrements[m];
            var (lower, higher, tableFactor) = _wave.WaveDataFor(invRateScale * incr);
            destination[m] = DoInterpolation(virtualReadIndex, MathF.Abs(incr), mask, tableFactor, lower, higher);
            virtualReadIndex += incr;
            virtualReadIndex -= Math.Floor(virtualReadIndex * invSize) * size;
        }
        return virtualReadIndex;
    }
}
