using System.Collections.Concurrent;

namespace Statventure.Audio;

/// <summary>
/// Blink's FFTFrame on its Rust FFT: a real signal's packed spectrum (real[0] the DC, imag[0] the
/// Nyquist), forward and inverse transforms, the spectra's product.
/// </summary>
internal sealed class FftFrame
{
    static readonly ConcurrentDictionary<int, RealFft> s_ffts = new();

    readonly RealFft _fft;

    public FftFrame(int size)
    {
        Size = size;
        _fft = s_ffts.GetOrAdd(size, n => new RealFft(n));
        Real = new float[size / 2];
        Imag = new float[size / 2];
    }

    public int Size { get; }
    public float[] Real { get; }
    public float[] Imag { get; }

    public void DoFft(ReadOnlySpan<float> data) => _fft.Forward(data[..Size], Real, Imag);
    public void DoInverseFft(Span<float> data) => _fft.Inverse(Real, Imag, data[..Size]);

    /// <summary>DoPaddedFFT: the data zero-padded to the frame's size.</summary>
    public void DoPaddedFft(ReadOnlySpan<float> data)
    {
        var padded = new float[Size];
        data.CopyTo(padded);
        DoFft(padded);
    }

    public void Scale(float factor)
    {
        VectorMath.Vsmul(Real, factor, Real);
        VectorMath.Vsmul(Imag, factor, Imag);
    }

    /// <summary>FFTFrame::Multiply: Zvmul over the packed bins (unfused), then the DC and Nyquist on their own.</summary>
    public void Multiply(FftFrame other)
    {
        float real0 = Real[0], imag0 = Imag[0];
        for (var i = 0; i < Real.Length; i++)
        {
            float r1 = Real[i], i1 = Imag[i], r2 = other.Real[i], i2 = other.Imag[i];
            Real[i] = r1 * r2 - i1 * i2;
            Imag[i] = r1 * i2 + i1 * r2;
        }
        Real[0] = real0 * other.Real[0];
        Imag[0] = imag0 * other.Imag[0];
    }
}

/// <summary>FFTConvolver: overlap-add convolution of blocks of half the FFT size with one kernel spectrum.</summary>
internal sealed class FftConvolver(int fftSize)
{
    readonly FftFrame _frame = new(fftSize);
    readonly float[] _input = new float[fftSize]; // the second half stays zero
    readonly float[] _output = new float[fftSize];
    readonly float[] _lastOverlap = new float[fftSize / 2];
    int _readWriteIndex;

    public void Process(FftFrame kernel, ReadOnlySpan<float> source, Span<float> dest)
    {
        var halfSize = fftSize / 2;
        var divisions = halfSize <= source.Length ? source.Length / halfSize : 1;
        var divisionSize = divisions == 1 ? source.Length : halfSize;
        for (var i = 0; i < divisions; i++)
        {
            var sourceSegment = source.Slice(i * divisionSize, divisionSize);
            var destSegment = dest.Slice(i * divisionSize, divisionSize);
            sourceSegment.CopyTo(_input.AsSpan(_readWriteIndex));
            _output.AsSpan(_readWriteIndex, divisionSize).CopyTo(destSegment);
            _readWriteIndex += divisionSize;
            if (_readWriteIndex == halfSize)
            {
                _frame.DoFft(_input);
                _frame.Multiply(kernel);
                _frame.DoInverseFft(_output);
                VectorMath.Vadd(_output.AsSpan(0, halfSize), _lastOverlap, _output.AsSpan(0, halfSize));
                _output.AsSpan(halfSize, halfSize).CopyTo(_lastOverlap);
                _readWriteIndex = 0;
            }
        }
    }

    public void Reset()
    {
        Array.Clear(_lastOverlap);
        _readWriteIndex = 0;
    }
}

/// <summary>DirectConvolver: time-domain convolution of the response's first frames (vector_math::Conv: each output summed in kernel order).</summary>
internal sealed class DirectConvolver
{
    readonly float[] _kernel;
    readonly float[] _buffer;

    public DirectConvolver(int inputBlockSize, float[] kernel)
    {
        _kernel = kernel;
        _buffer = new float[kernel.Length - 1 + inputBlockSize];
    }

    public void Process(ReadOnlySpan<float> source, Span<float> destination)
    {
        var frames = destination.Length;
        var kernelSize = _kernel.Length;
        var historySize = kernelSize - 1;
        source[..frames].CopyTo(_buffer.AsSpan(historySize));
        for (var n = 0; n < frames; n++)
        {
            var sum = 0f;
            for (var k = 0; k < kernelSize; k++)
                sum += _kernel[kernelSize - 1 - k] * _buffer[n + k];
            destination[n] = sum;
        }
        _buffer.AsSpan(frames, historySize).CopyTo(_buffer);
    }

    public void Reset() => Array.Clear(_buffer);
}

/// <summary>ReverbAccumulationBuffer: where every stage adds its delayed output, read and cleared a quantum at a time.</summary>
internal sealed class ReverbAccumulationBuffer(int length)
{
    readonly float[] _buffer = new float[length];
    int _readIndex;

    public void ReadAndClear(Span<float> destination)
    {
        var frames1 = Math.Min(destination.Length, _buffer.Length - _readIndex);
        var frames2 = destination.Length - frames1;
        _buffer.AsSpan(_readIndex, frames1).CopyTo(destination);
        _buffer.AsSpan(_readIndex, frames1).Clear();
        if (frames2 > 0)
        {
            _buffer.AsSpan(0, frames2).CopyTo(destination[frames1..]);
            _buffer.AsSpan(0, frames2).Clear();
        }
        _readIndex = (_readIndex + destination.Length) % _buffer.Length;
    }

    public void UpdateReadIndex(ref int readIndex, int frames) => readIndex = (readIndex + frames) % _buffer.Length;

    public void Accumulate(ReadOnlySpan<float> source, ref int readIndex, int delayFrames)
    {
        var writeIndex = (readIndex + delayFrames) % _buffer.Length;
        readIndex = (readIndex + source.Length) % _buffer.Length;
        var frames1 = Math.Min(source.Length, _buffer.Length - writeIndex);
        var frames2 = source.Length - frames1;
        var head = _buffer.AsSpan(writeIndex, frames1);
        VectorMath.Vadd(source[..frames1], head, head);
        if (frames2 > 0)
        {
            var wrapped = _buffer.AsSpan(0, frames2);
            VectorMath.Vadd(source.Slice(frames1, frames2), wrapped, wrapped);
        }
    }

    public void Reset()
    {
        Array.Clear(_buffer);
        _readIndex = 0;
    }
}

/// <summary>
/// ReverbConvolverStage: one partition of the response, convolved directly (the first) or by FFT,
/// its output added to the accumulation buffer after its delay; staggered by a render phase so
/// the stages' FFTs fall in different quanta.
/// </summary>
internal sealed class ReverbConvolverStage
{
    readonly ReverbAccumulationBuffer _accumulation;
    readonly FftFrame? _kernel;
    readonly FftConvolver? _fftConvolver;
    readonly DirectConvolver? _directConvolver;
    readonly float[] _temporary;
    readonly float[] _preDelay;
    readonly int _preDelayLength, _postDelayLength;
    int _accumulationReadIndex, _preReadWriteIndex;
    long _framesProcessed;

    public ReverbConvolverStage(ReadOnlySpan<float> impulseResponse, int totalLatency, int stageOffset, int stageLength, int fftSize,
        int renderPhase, int renderSliceSize, ReverbAccumulationBuffer accumulation, float scale, bool directMode)
    {
        _accumulation = accumulation;
        if (!directMode)
        {
            _kernel = new FftFrame(fftSize);
            _kernel.DoPaddedFft(impulseResponse.Slice(stageOffset, stageLength));
            if (scale != 1)
                _kernel.Scale(scale);
            _fftConvolver = new FftConvolver(fftSize);
        }
        else
        {
            var directKernel = new float[fftSize / 2];
            impulseResponse[..stageLength].CopyTo(directKernel);
            if (scale != 1)
                VectorMath.Vsmul(directKernel.AsSpan(0, stageLength), scale, directKernel.AsSpan(0, stageLength));
            _directConvolver = new DirectConvolver(renderSliceSize, directKernel);
        }
        _temporary = new float[renderSliceSize];
        var totalDelay = stageOffset + totalLatency;
        var halfSize = fftSize / 2;
        if (!directMode && totalDelay >= halfSize)
            totalDelay -= halfSize;
        var maxPreDelayLength = Math.Min(halfSize, totalDelay);
        _preDelayLength = totalDelay > 0 ? renderPhase % maxPreDelayLength : 0;
        if (_preDelayLength > totalDelay)
            _preDelayLength = 0;
        _postDelayLength = totalDelay - _preDelayLength;
        var delayBufferSize = _preDelayLength < fftSize ? fftSize : _preDelayLength;
        delayBufferSize = delayBufferSize < renderSliceSize ? renderSliceSize : delayBufferSize;
        _preDelay = new float[delayBufferSize];
    }

    public void Process(ReadOnlySpan<float> source)
    {
        var frames = source.Length;
        ReadOnlySpan<float> preDelayedSource;
        Span<float> temporary;
        if (_preDelayLength > 0)
        {
            if (_preReadWriteIndex + frames > _preDelay.Length)
                return;
            preDelayedSource = _preDelay.AsSpan(_preReadWriteIndex);
            temporary = _temporary;
        }
        else
        {
            preDelayedSource = source;
            temporary = _preDelay;
        }
        if (_framesProcessed < _preDelayLength)
            _accumulation.UpdateReadIndex(ref _accumulationReadIndex, frames);
        else
        {
            if (_directConvolver != null)
                _directConvolver.Process(preDelayedSource, temporary[..frames]);
            else
                _fftConvolver!.Process(_kernel!, preDelayedSource[..frames], temporary[..frames]);
            _accumulation.Accumulate(temporary[..frames], ref _accumulationReadIndex, _postDelayLength);
        }
        if (_preDelayLength > 0)
        {
            source[..frames].CopyTo(_preDelay.AsSpan(_preReadWriteIndex));
            _preReadWriteIndex += frames;
            if (_preReadWriteIndex >= _preDelayLength)
                _preReadWriteIndex = 0;
        }
        _framesProcessed += frames;
    }

    public void Reset()
    {
        _fftConvolver?.Reset();
        _directConvolver?.Reset();
        Array.Clear(_preDelay);
        _accumulationReadIndex = 0;
        _framesProcessed = 0;
    }
}

/// <summary>
/// ReverbConvolver: one response channel split into stages, a 64-frame direct stage then FFT stages
/// of 128, 256, ... up to 32768 points, the offline structure (user decision, Docs/Audio.md "The
/// convolver's partitioning").
/// </summary>
internal sealed class ReverbConvolver
{
    const int MinFftSize = 128;
    readonly List<ReverbConvolverStage> _stages = [];
    readonly ReverbAccumulationBuffer _accumulation;

    public ReverbConvolver(ReadOnlySpan<float> impulseResponse, int renderSliceSize, int maxFftSize, int renderPhaseStart, float scale)
    {
        _accumulation = new ReverbAccumulationBuffer(impulseResponse.Length + renderSliceSize);
        var totalLength = impulseResponse.Length;
        var stageOffset = 0;
        var i = 0;
        var fftSize = MinFftSize;
        while (stageOffset < totalLength)
        {
            var stageSize = fftSize / 2;
            if (stageSize + stageOffset > totalLength)
                stageSize = totalLength - stageOffset;
            var renderPhase = renderPhaseStart + i * renderSliceSize;
            var direct = stageOffset == 0;
            _stages.Add(new ReverbConvolverStage(impulseResponse, 0, stageOffset, stageSize, fftSize, renderPhase, renderSliceSize, _accumulation, scale, direct));
            stageOffset += stageSize;
            i++;
            if (!direct)
                fftSize *= 2;
            if (fftSize > maxFftSize)
                fftSize = maxFftSize;
        }
    }

    public void Process(ReadOnlySpan<float> source, Span<float> destination)
    {
        foreach (var stage in _stages)
            stage.Process(source);
        _accumulation.ReadAndClear(destination);
    }

    public void Reset()
    {
        foreach (var stage in _stages)
            stage.Reset();
        _accumulation.Reset();
    }
}

/// <summary>
/// Reverb: a convolver per response channel (at least two), the response normalized by its power
/// (calibrated to −58 dB at 44.1 kHz), and the ways inputs, responses and outputs pair up.
/// </summary>
internal sealed class Reverb
{
    const float GainCalibration = -58;
    const float GainCalibrationSampleRate = 44100;
    const float MinPower = 0.000125f;
    const int MaxFftSize = 32768;

    readonly List<ReverbConvolver> _convolvers = [];
    readonly AudioBus? _temp;

    public Reverb(AudioBuffer response, bool normalize, int responseAlignment)
    {
        ResponseLength = response.Length;
        ResponseChannels = response.NumberOfChannels;
        var scale = normalize ? NormalizationScale(response, responseAlignment) : 1;
        var count = Math.Max(ResponseChannels, 2);
        var renderPhase = 0;
        for (var i = 0; i < count; i++)
        {
            _convolvers.Add(new ReverbConvolver(response.GetChannelData(Math.Min(i, ResponseChannels - 1)), AudioGraph.RenderQuantumFrames, MaxFftSize, renderPhase, scale));
            renderPhase += AudioGraph.RenderQuantumFrames;
        }
        if (ResponseChannels == 4)
            _temp = new AudioBus(2, AudioGraph.RenderQuantumFrames);
    }

    public int ResponseLength { get; }
    public int ResponseChannels { get; }

    /// <summary>
    /// CalculateNormalizationScale: 1 / the response's RMS power (at least 0.000125), times fdlibm's
    /// powf(10, −58 · 0.05) and 44100 / the rate. The power is Blink's vector_math::Vsvesq, whose
    /// summing order follows the data's address within 32 bytes (<paramref name="alignment"/>, in floats).
    /// </summary>
    static float NormalizationScale(AudioBuffer response, int alignment)
    {
        var channels = response.NumberOfChannels;
        var length = response.Length;
        float power = 0;
        for (var i = 0; i < channels; i++)
            power += SumOfSquares(response.GetChannelData(i), alignment);
        power = MathF.Sqrt(power / (channels * length));
        if (!float.IsFinite(power) || power < MinPower)
            power = MinPower;
        var scale = 1 / power;
        scale *= (float)Fdlibm.Pow(10, GainCalibration * 0.05f);
        scale *= GainCalibrationSampleRate / response.SampleRate;
        if (channels == 4)
            scale *= 0.5f;
        return scale;
    }

    /// <summary>
    /// The x86 Vsvesq with AVX: scalar then SSE frames up to a 32-byte boundary, AVX lanes, SSE
    /// lanes, a scalar tail; each vector part summed in lanes, its lanes then added in order.
    /// </summary>
    internal static float SumOfSquares(ReadOnlySpan<float> source, int alignment)
    {
        var frames = source.Length;
        var totalForAlignment = (8 - alignment) & 7;
        var scalarForAlignment = totalForAlignment & 3;
        var sseForAlignment = totalForAlignment & ~3;
        var useAvx = frames >= scalarForAlignment + sseForAlignment + 8;
        var useSse = useAvx || frames >= scalarForAlignment + 4;
        int scalarHead = 0, sseHead = 0, avx = 0, sse = 0;
        if (useSse)
        {
            scalarHead = scalarForAlignment;
            frames -= scalarHead;
            if (useAvx)
            {
                sseHead = sseForAlignment;
                frames -= sseHead;
                avx = frames & ~7;
                frames -= avx;
            }
            sse = frames & ~3;
            frames -= sse;
        }
        var sum = 0f;
        var i = 0;
        for (var end = i + scalarHead; i < end; i++)
            sum += source[i] * source[i];
        i = Lanes(source, i, sseHead, 4, ref sum);
        i = Lanes(source, i, avx, 8, ref sum);
        i = Lanes(source, i, sse, 4, ref sum);
        for (; i < source.Length; i++)
            sum += source[i] * source[i];
        return sum;
    }

    static int Lanes(ReadOnlySpan<float> source, int start, int count, int width, ref float sum)
    {
        if (count == 0)
            return start;
        Span<float> lanes = stackalloc float[width];
        for (var i = 0; i < count; i += width)
            for (var j = 0; j < width; j++)
                lanes[j] += source[start + i + j] * source[start + i + j];
        foreach (var lane in lanes)
            sum += lane;
        return start + count;
    }

    public void Process(AudioBus source, AudioBus destination, int frames)
    {
        if (destination.NumberOfChannels > 2)
        {
            destination.Zero();
            return;
        }
        var sourceL = source.Channel(0).Data.AsSpan(0, frames);
        var destinationL = destination.Channel(0);
        int inputs = source.NumberOfChannels, outputs = destination.NumberOfChannels, responses = ResponseChannels;
        if (inputs == 2 && (responses == 1 || responses == 2) && outputs == 2)
        {
            _convolvers[0].Process(sourceL, destinationL.Mutable[..frames]);
            _convolvers[1].Process(source.Channel(1).Data.AsSpan(0, frames), destination.Channel(1).Mutable[..frames]);
        }
        else if (inputs == 1 && outputs == 2 && responses == 2)
        {
            for (var i = 0; i < 2; i++)
                _convolvers[i].Process(sourceL, destination.Channel(i).Mutable[..frames]);
        }
        else if (inputs == 1 && responses == 1 && outputs == 1)
            _convolvers[0].Process(sourceL, destinationL.Mutable[..frames]);
        else if (responses == 4 && outputs == 2)
        {
            var sourceR = inputs == 2 ? source.Channel(1).Data.AsSpan(0, frames) : sourceL;
            _convolvers[0].Process(sourceL, destinationL.Mutable[..frames]);
            _convolvers[1].Process(sourceL, destination.Channel(1).Mutable[..frames]);
            _convolvers[2].Process(sourceR, _temp!.Channel(0).Mutable[..frames]);
            _convolvers[3].Process(sourceR, _temp.Channel(1).Mutable[..frames]);
            destination.SumFrom(_temp);
        }
        else
            destination.Zero();
    }

    public void Reset()
    {
        foreach (var convolver in _convolvers)
            convolver.Reset();
    }
}

/// <summary>
/// ConvolverNode (Blink's ConvolverHandler): a Reverb over its buffer (normalized unless turned
/// off before the buffer is set), the output stereo unless both input and response are mono, its
/// tail the response's length.
/// </summary>
internal sealed class ConvolverNode : AudioNode
{
    Reverb? _reverb;
    AudioBuffer? _buffer;

    public ConvolverNode(AudioGraph graph) : base(graph)
    {
        AddInput();
        AddOutput(1);
        ChannelCount = 2;
        ChannelCountMode = ChannelCountMode.ClampedMax;
        // Disabled until something connects into it (ConvolverHandler's constructor).
        DisableOutputs();
    }

    protected override bool RequiresTailProcessing => true;

    /// <summary>normalize: read when the buffer is set.</summary>
    public bool Normalize { get; set; } = true;

    /// <summary>
    /// Where Chromium's copy of the response sits within 32 bytes, in floats (its normalization's
    /// summing order depends on it): at a multiple of 32, as the renders show (an offset of 4 fails them).
    /// </summary>
    const int ResponseAlignment = 0;

    public AudioBuffer? Buffer
    {
        get => _buffer;
        set
        {
            if (value == null)
            {
                _reverb = null;
                _buffer = null;
                return;
            }
            if (value.SampleRate != Graph.SampleRateF)
                throw new NotSupportedException($"the buffer's rate ({value.SampleRate}) isn't the context's ({Graph.SampleRateF})");
            _reverb = new Reverb(value, Normalize, ResponseAlignment);
            _buffer = value;
            Output(0).SetNumberOfChannels(NumberOfOutputChannels(Input(0).NumberOfChannels, value.NumberOfChannels));
        }
    }

    static int NumberOfOutputChannels(int inputChannels, int responseChannels) => Math.Clamp(Math.Max(inputChannels, responseChannels), 1, 2);

    protected override double TailTime => _reverb != null ? _reverb.ResponseLength / (double)Graph.SampleRateF : 0;

    internal override void CheckNumberOfChannelsForInput(AudioNodeInput input)
    {
        var responseChannels = _buffer?.NumberOfChannels ?? 1;
        Output(0).SetNumberOfChannels(NumberOfOutputChannels(input.NumberOfChannels, responseChannels));
        base.CheckNumberOfChannelsForInput(input);
    }

    protected override void Process(int frames)
    {
        var output = Output(0).Bus;
        if (_reverb == null)
        {
            output.Zero();
            return;
        }
        _reverb.Process(Input(0).Bus, output, frames);
    }
}
