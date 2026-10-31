namespace Statventure.Audio;

/// <summary>Sample data (Blink's AudioBuffer): planar float channels at a sample rate.</summary>
internal sealed class AudioBuffer
{
    readonly float[][] _channels;

    public AudioBuffer(int channels, int length, float sampleRate)
    {
        if (channels < 1 || channels > 2)
            throw new ArgumentOutOfRangeException(nameof(channels), "only mono and stereo are supported");
        if (length < 1)
            throw new ArgumentOutOfRangeException(nameof(length));
        if (!(sampleRate >= 3000 && sampleRate <= 768000))
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        _channels = new float[channels][];
        for (var c = 0; c < channels; c++)
            _channels[c] = new float[length];
        Length = length;
        SampleRate = sampleRate;
    }

    public int NumberOfChannels => _channels.Length;
    public int Length { get; }
    public float SampleRate { get; }
    public double Duration => Length / (double)SampleRate;

    /// <summary>getChannelData: the channel's samples, written in place.</summary>
    public float[] GetChannelData(int channel) => _channels[channel];
}

/// <summary>
/// A source started and stopped at times (Blink's AudioScheduledSourceHandler): its schedule turned
/// into the part of each quantum it plays, the frames before its start and after its stop zeroed.
/// </summary>
internal abstract class AudioScheduledSourceNode : AudioNode
{
    protected enum PlaybackState { Unscheduled, Scheduled, Playing, Finished }

    protected const double UnknownTime = -1;
    protected double StartTime;
    protected double EndTime = UnknownTime;

    protected AudioScheduledSourceNode(AudioGraph graph) : base(graph) { }

    protected PlaybackState State { get; set; }
    public bool HasFinished => State == PlaybackState.Finished;
    protected bool IsPlayingOrScheduled => State is PlaybackState.Playing or PlaybackState.Scheduled;

    /// <summary>onended: called after the quantum in which the source finished.</summary>
    public event Action<AudioScheduledSourceNode>? Ended;
    internal void RaiseEnded() => Ended?.Invoke(this);

    public virtual void Start(double when = 0)
    {
        if (State != PlaybackState.Unscheduled)
            throw new InvalidOperationException("cannot call start more than once");
        if (!(when >= 0))
            throw new ArgumentOutOfRangeException(nameof(when));
        StartTime = Math.Max(when, Graph.CurrentTime);
        State = PlaybackState.Scheduled;
        Graph.NotifyStarted(this);
    }

    public void Stop(double when = 0)
    {
        if (State == PlaybackState.Unscheduled)
            throw new InvalidOperationException("cannot call stop without calling start first");
        if (!(when >= 0))
            throw new ArgumentOutOfRangeException(nameof(when));
        EndTime = Math.Max(0.0, when);
    }

    /// <summary>A pre-render check finishing a source that can no longer sound (it may not be pulled at all).</summary>
    internal abstract void HandleStoppableSourceNode();

    /// <summary>kExtraStopFrames: how long past its computed end a source is kept before it's finished.</summary>
    protected const int ExtraStopFrames = 256;

    protected void Finish()
    {
        if (State == PlaybackState.Finished)
            return;
        State = PlaybackState.Finished;
        Graph.NotifyEnded(this);
    }

    internal enum FrameRounding { Nearest, Down, Up }

    /// <summary>audio_utilities::TimeToSampleFrame: the frame at 1024 times the rate rounded, then rounded as asked.</summary>
    internal static long TimeToSampleFrame(double time, double sampleRate, FrameRounding rounding = FrameRounding.Nearest)
    {
        const double Oversample = 1024;
        var frame = Math.Round(time * sampleRate * Oversample, MidpointRounding.AwayFromZero) / Oversample;
        frame = rounding switch
        {
            FrameRounding.Nearest => Math.Round(frame, MidpointRounding.AwayFromZero),
            FrameRounding.Down => Math.Floor(frame),
            _ => Math.Ceiling(frame),
        };
        return frame >= long.MaxValue ? long.MaxValue : (long)frame;
    }

    /// <summary>
    /// AudioScheduledSourceHandler::UpdateSchedulingInfo: the quantum's frame where the source starts
    /// playing, how many frames it plays, and its start's fraction of a frame (≤ 0); finishes it at its stop.
    /// </summary>
    protected (int QuantumFrameOffset, int NonSilentFrames, double StartFrameOffset) UpdateSchedulingInfo(int quantumFrames, AudioBus output)
    {
        var sampleRate = Graph.SampleRate;
        var quantumStart = Graph.CurrentSampleFrame;
        var quantumEnd = quantumStart + quantumFrames;
        var startFrame = TimeToSampleFrame(StartTime, sampleRate, FrameRounding.Up);
        var endFrame = EndTime == UnknownTime ? 0 : TimeToSampleFrame(EndTime, sampleRate, FrameRounding.Up);
        if (EndTime != UnknownTime && endFrame <= quantumStart)
            Finish();
        if (State is PlaybackState.Unscheduled or PlaybackState.Finished || startFrame >= quantumEnd)
        {
            output.Zero();
            return (0, 0, 0);
        }
        double startFrameOffset = 0;
        if (State == PlaybackState.Scheduled)
        {
            State = PlaybackState.Playing;
            startFrameOffset = StartTime * sampleRate - startFrame;
        }
        var quantumFrameOffset = (int)Math.Min(startFrame > quantumStart ? startFrame - quantumStart : 0, quantumFrames);
        var nonSilent = quantumFrames - quantumFrameOffset;
        if (nonSilent == 0)
        {
            output.Zero();
            return (quantumFrameOffset, 0, startFrameOffset);
        }
        if (quantumFrameOffset > 0)
            for (var c = 0; c < output.NumberOfChannels; c++)
                output.Channel(c).Mutable[..quantumFrameOffset].Clear();
        if (EndTime != UnknownTime && endFrame >= quantumStart && endFrame <= quantumEnd)
        {
            if (endFrame < quantumEnd)
            {
                var zeroStart = (int)(endFrame - quantumStart);
                var toZero = quantumFrames - zeroStart;
                nonSilent = toZero > nonSilent ? 0 : nonSilent - toZero;
                for (var c = 0; c < output.NumberOfChannels; c++)
                    output.Channel(c).Mutable.Slice(zeroStart, toZero).Clear();
            }
            Finish();
        }
        return (quantumFrameOffset, nonSilent, startFrameOffset);
    }
}

/// <summary>Plays an <see cref="AudioBuffer"/> (Blink's AudioBufferSourceHandler): offsets, durations, loops, playback rate and detune (k-rate), linear interpolation.</summary>
internal sealed class AudioBufferSourceNode : AudioScheduledSourceNode
{
    const double MaxRate = 1024;
    const double DefaultGrainDuration = 0.020;

    AudioBuffer? _buffer;
    bool _bufferHasBeenSet;
    bool _isLooping, _didSetLooping;
    double _minPlaybackRate = 1.0;
    double _loopStart, _loopEnd;
    double _effectiveLoopStart, _effectiveLoopEnd;
    double _virtualReadIndex;
    double _bufferPlayedFrames;
    bool _isGrain, _isDurationGiven;
    double _grainOffset, _grainDuration = DefaultGrainDuration;
    float[][] _destination = [];

    public AudioBufferSourceNode(AudioGraph graph) : base(graph)
    {
        PlaybackRate = new AudioParam(graph, 1, audioRate: false);
        Detune = new AudioParam(graph, 0, audioRate: false);
        AddOutput(1);
    }

    public AudioParam PlaybackRate { get; }
    public AudioParam Detune { get; }

    public AudioBuffer? Buffer
    {
        get => _buffer;
        set
        {
            if (value != null && _bufferHasBeenSet)
                throw new InvalidOperationException("a buffer source's buffer can be set only once");
            _buffer = value;
            if (value != null)
            {
                _bufferHasBeenSet = true;
                Output(0).SetNumberOfChannels(value.NumberOfChannels);
                if (_isGrain)
                    ClampGrainParameters();
            }
            UpdateEffectiveLoopPoints();
            _virtualReadIndex = 0;
            _bufferPlayedFrames = 0;
        }
    }

    public bool Loop
    {
        get => _isLooping;
        set
        {
            _isLooping = value;
            _didSetLooping |= value;
            UpdateEffectiveLoopPoints();
        }
    }

    public double LoopStart
    {
        get => _loopStart;
        set
        {
            _loopStart = value;
            UpdateEffectiveLoopPoints();
        }
    }

    public double LoopEnd
    {
        get => _loopEnd;
        set
        {
            _loopEnd = value;
            UpdateEffectiveLoopPoints();
        }
    }

    /// <summary>start(when, offset): from <paramref name="offset"/> seconds into the buffer to its end.</summary>
    public void Start(double when, double offset) => StartSource(when, offset, _buffer?.Duration ?? 0, false);

    /// <summary>start(when, offset, duration): at most <paramref name="duration"/> seconds of the buffer (loops included).</summary>
    public void Start(double when, double offset, double duration) => StartSource(when, offset, duration, true);

    void StartSource(double when, double offset, double duration, bool isDurationGiven)
    {
        if (State != PlaybackState.Unscheduled)
            throw new InvalidOperationException("cannot call start more than once");
        if (!(when >= 0))
            throw new ArgumentOutOfRangeException(nameof(when));
        if (!(offset >= 0))
            throw new ArgumentOutOfRangeException(nameof(offset));
        if (!(duration >= 0))
            throw new ArgumentOutOfRangeException(nameof(duration));
        _isDurationGiven = isDurationGiven;
        _isGrain = true;
        _grainOffset = offset;
        _grainDuration = duration;
        StartTime = Math.Max(when, Graph.CurrentTime);
        if (_buffer != null)
            ClampGrainParameters();
        State = PlaybackState.Scheduled;
        Graph.NotifyStarted(this);
    }

    void ClampGrainParameters()
    {
        var duration = _buffer!.Duration;
        _grainOffset = Math.Clamp(_grainOffset, 0.0, duration);
        _grainDuration = _isDurationGiven ? Math.Clamp(_grainDuration, 0.0, double.PositiveInfinity) : double.PositiveInfinity;
        // The parameters' values as the script sees them (main thread: the intrinsic values).
        _virtualReadIndex = PlaybackRate.IntrinsicValue == 1.0 && Detune.IntrinsicValue == 0.0
            ? TimeToSampleFrame(_grainOffset, _buffer.SampleRate)
            : _grainOffset * _buffer.SampleRate;
    }

    void UpdateEffectiveLoopPoints()
    {
        if (_buffer == null)
        {
            _effectiveLoopStart = _effectiveLoopEnd = 0;
            return;
        }
        var duration = _buffer.Duration;
        if (!_isLooping)
        {
            _effectiveLoopStart = 0;
            _effectiveLoopEnd = duration;
            return;
        }
        var start = Math.Clamp(_loopStart, 0.0, duration);
        var end = _loopEnd == 0 ? duration : Math.Clamp(_loopEnd, 0.0, duration);
        (_effectiveLoopStart, _effectiveLoopEnd) = start < end ? (start, end) : (0, duration);
    }

    protected override bool PropagatesSilence => !IsPlayingOrScheduled;

    protected override void Process(int frames)
    {
        var output = Output(0).Bus;
        if (_buffer == null)
        {
            output.Zero();
            if (State != PlaybackState.Unscheduled)
                Finish();
            return;
        }
        if (Output(0).NumberOfChannels != _buffer.NumberOfChannels)
        {
            output.Zero();
            return;
        }
        var (quantumFrameOffset, bufferFrames, startTimeOffset) = UpdateSchedulingInfo(frames, output);
        if (bufferFrames == 0)
        {
            output.Zero();
            return;
        }
        if (_destination.Length != output.NumberOfChannels)
            _destination = new float[output.NumberOfChannels][];
        for (var c = 0; c < output.NumberOfChannels; c++)
        {
            output.Channel(c).ClearSilentFlag();
            _destination[c] = output.Channel(c).Data;
        }
        if (!RenderFromBuffer(output, quantumFrameOffset, bufferFrames, startTimeOffset))
        {
            output.Zero();
            return;
        }
        output.ClearSilentFlag();
    }

    double ComputePlaybackRate()
    {
        var sampleRateFactor = _buffer!.SampleRate / (double)Graph.SampleRateF;
        double basePlaybackRate = PlaybackRate.FinalValue();
        var rate = sampleRateFactor * basePlaybackRate;
        // detune / 1200 is a float division in Blink.
        rate *= Fdlibm.Pow(2, Detune.FinalValue() / 1200);
        rate = Math.Clamp(rate, -MaxRate, MaxRate);
        if (rate < _minPlaybackRate)
            _minPlaybackRate = rate;
        return rate;
    }

    internal override void HandleStoppableSourceNode()
    {
        if (EndTime != UnknownTime && Graph.CurrentTime > EndTime)
        {
            Finish();
            return;
        }
        var isFinite = !_didSetLooping || _isDurationGiven;
        if (isFinite && _buffer != null && IsPlayingOrScheduled && _minPlaybackRate > 0)
        {
            var sourceDuration = _isDurationGiven ? _grainDuration : _buffer.Duration;
            var stopTime = StartTime + sourceDuration / _minPlaybackRate;
            stopTime += ExtraStopFrames / (double)Graph.SampleRateF;
            if (Graph.CurrentTime > stopTime)
                Finish();
        }
    }

    void RenderSilenceAndFinish(int index, int frames)
    {
        if (frames > 0)
            foreach (var destination in _destination)
                destination.AsSpan(index, frames).Clear();
        Finish();
    }

    bool RenderFromBuffer(AudioBus bus, int destinationFrameOffset, int numberOfFrames, double startTimeOffset)
    {
        var channels = Output(0).NumberOfChannels;
        if (destinationFrameOffset > 0)
            foreach (var destination in _destination)
                destination.AsSpan(0, destinationFrameOffset).Clear();
        var writeIndex = destinationFrameOffset;
        var bufferLength = _buffer!.Length;
        double bufferSampleRate = _buffer.SampleRate;
        var virtualStartFrame = _effectiveLoopStart * bufferSampleRate;
        var virtualEndFrame = _effectiveLoopEnd * bufferSampleRate;
        var virtualDeltaFrames = virtualEndFrame - virtualStartFrame;
        var rate = ComputePlaybackRate();
        if (Math.Abs(rate) > virtualDeltaFrames)
            return false;
        var virtualReadIndex = _virtualReadIndex;
        var framesToProcess = numberOfFrames;
        if (rate >= 0)
        {
            if (_isLooping && virtualReadIndex >= virtualEndFrame)
            {
                virtualReadIndex = Math.Min(virtualStartFrame, (double)(bufferLength - 1));
                _virtualReadIndex = virtualReadIndex;
            }
            if (startTimeOffset < 0 && rate != 0)
            {
                var skipped = Math.Abs(startTimeOffset * rate);
                virtualReadIndex += skipped;
                _bufferPlayedFrames += skipped;
            }
        }
        else
        {
            if (_isLooping && virtualReadIndex < virtualStartFrame)
            {
                virtualReadIndex = virtualStartFrame;
                _virtualReadIndex = virtualReadIndex;
            }
            if (startTimeOffset < 0)
            {
                var skipped = Math.Abs(startTimeOffset * rate);
                virtualReadIndex -= skipped;
                _bufferPlayedFrames += skipped;
            }
            while (framesToProcess > 0 && virtualReadIndex >= bufferLength)
            {
                for (var c = 0; c < channels; c++)
                    _destination[c][writeIndex] = 0;
                writeIndex++;
                virtualReadIndex += rate;
                framesToProcess--;
            }
        }
        var stoppingThisQuantum = false;
        if (_isDurationGiven)
        {
            var maxSourceFrames = _grainDuration * bufferSampleRate;
            var sourceFramesLeft = maxSourceFrames - _bufferPlayedFrames;
            if (sourceFramesLeft <= 0.0)
            {
                framesToProcess = 0;
                stoppingThisQuantum = true;
            }
            else if (rate != 0.0)
            {
                var framesUntilLimit = Math.Ceiling(sourceFramesLeft / Math.Abs(rate));
                if (framesUntilLimit < framesToProcess)
                {
                    framesToProcess = (int)framesUntilLimit;
                    stoppingThisQuantum = true;
                }
            }
        }
        if (rate == 1 && virtualReadIndex == Math.Floor(virtualReadIndex) && virtualDeltaFrames == Math.Floor(virtualDeltaFrames)
            && virtualEndFrame == Math.Floor(virtualEndFrame))
            (writeIndex, virtualReadIndex) = ProcessFastPath(virtualDeltaFrames, virtualEndFrame, framesToProcess, writeIndex, virtualReadIndex);
        else
            (writeIndex, virtualReadIndex) = ProcessInterpolatedPath(virtualStartFrame, virtualDeltaFrames, virtualEndFrame, bufferLength, rate,
                framesToProcess, writeIndex, virtualReadIndex);
        bus.ClearSilentFlag();
        if (rate != 0)
        {
            var framesProcessed = (uint)(writeIndex - destinationFrameOffset);
            _bufferPlayedFrames += framesProcessed * Math.Abs(rate);
        }
        if (stoppingThisQuantum)
        {
            foreach (var destination in _destination)
                destination.AsSpan(writeIndex, destinationFrameOffset + numberOfFrames - writeIndex).Clear();
            Finish();
        }
        _virtualReadIndex = virtualReadIndex;
        return true;
    }

    (int, double) ProcessFastPath(double virtualDeltaFrames, double virtualEndFrame, int framesToProcess, int writeIndex, double virtualReadIndex)
    {
        var readIndex = (uint)virtualReadIndex;
        var deltaFrames = (uint)virtualDeltaFrames;
        var endFrame = (uint)virtualEndFrame;
        while (framesToProcess > 0)
        {
            var framesToEnd = (int)(endFrame - readIndex);
            var framesThisTime = Math.Max(0, Math.Min(framesToProcess, framesToEnd));
            for (var c = 0; c < _destination.Length; c++)
                _buffer!.GetChannelData(c).AsSpan((int)readIndex, framesThisTime).CopyTo(_destination[c].AsSpan(writeIndex));
            writeIndex += framesThisTime;
            readIndex += (uint)framesThisTime;
            framesToProcess -= framesThisTime;
            double tempReadIndex = readIndex;
            if (deltaFrames <= 0)
            {
                Finish();
                break;
            }
            if (tempReadIndex >= endFrame)
            {
                if (!_isLooping)
                {
                    RenderSilenceAndFinish(writeIndex, framesToProcess);
                    break;
                }
                var overflow = tempReadIndex - endFrame;
                tempReadIndex = endFrame - deltaFrames + (overflow % deltaFrames);
            }
            readIndex = (uint)tempReadIndex;
        }
        return (writeIndex, readIndex);
    }

    (int, double) ProcessInterpolatedPath(double virtualStartFrame, double virtualDeltaFrames, double virtualEndFrame, int bufferLength, double rate,
        int framesToProcess, int writeIndex, double virtualReadIndex)
    {
        for (var i = 0; i < framesToProcess; i++)
        {
            var framesRemaining = framesToProcess - i - 1;
            var readIndex = (uint)virtualReadIndex;
            var interpolationFactor = virtualReadIndex - readIndex;
            var readIndex2 = readIndex + 1;
            if (_isLooping && readIndex < virtualEndFrame && readIndex2 >= virtualEndFrame)
            {
                var nextIndex = virtualReadIndex + 1.0 - virtualDeltaFrames;
                readIndex2 = nextIndex >= 0 ? (uint)nextIndex : 0;
            }
            else if (readIndex2 >= bufferLength)
                readIndex2 = readIndex;
            if (readIndex >= bufferLength || readIndex2 >= bufferLength)
                break;
            for (var c = 0; c < _destination.Length; c++)
            {
                var source = _buffer!.GetChannelData(c);
                double sample;
                if (readIndex == readIndex2 && readIndex >= 1)
                {
                    double sample1 = source[readIndex - 1], sample2 = source[readIndex];
                    sample = sample2 + (sample2 - sample1) * interpolationFactor;
                }
                else
                {
                    double sample1 = source[readIndex], sample2 = source[readIndex2];
                    sample = (1.0 - interpolationFactor) * sample1 + interpolationFactor * sample2;
                }
                _destination[c][writeIndex] = (float)Math.Clamp(sample, -float.MaxValue, float.MaxValue);
            }
            writeIndex++;
            virtualReadIndex += rate;
            if (virtualDeltaFrames <= 0)
            {
                Finish();
                break;
            }
            if (rate >= 0 && virtualReadIndex >= virtualEndFrame)
            {
                if (!_isLooping)
                {
                    RenderSilenceAndFinish(writeIndex, framesRemaining);
                    break;
                }
                var overflow = virtualReadIndex - virtualEndFrame;
                virtualReadIndex = virtualEndFrame - virtualDeltaFrames + (overflow % virtualDeltaFrames);
            }
            else if (rate < 0 && virtualReadIndex < virtualStartFrame)
            {
                if (!_isLooping)
                {
                    RenderSilenceAndFinish(writeIndex, framesRemaining);
                    break;
                }
                var overflow = virtualStartFrame - virtualReadIndex;
                virtualReadIndex = virtualStartFrame + virtualDeltaFrames - (overflow % virtualDeltaFrames);
            }
        }
        return (writeIndex, virtualReadIndex);
    }
}

/// <summary>Multiplies its input by its gain parameter (Blink's GainHandler); the output takes the input's channel count.</summary>
internal sealed class GainNode : AudioNode
{
    readonly float[] _gainValues = new float[AudioGraph.RenderQuantumFrames];

    public GainNode(AudioGraph graph) : base(graph)
    {
        Gain = new AudioParam(graph, 1);
        AddInput();
        AddOutput(1);
    }

    public AudioParam Gain { get; }

    internal override void CheckNumberOfChannelsForInput(AudioNodeInput input)
    {
        Output(0).SetNumberOfChannels(input.NumberOfChannels);
        base.CheckNumberOfChannelsForInput(input);
    }

    protected override void Process(int frames)
    {
        var output = Output(0).Bus;
        if (!Input(0).IsConnected)
        {
            output.Zero();
            return;
        }
        var input = Input(0).Bus;
        var sampleAccurate = Gain.HasSampleAccurateValues();
        if (sampleAccurate && Gain.IsAudioRate)
        {
            var values = _gainValues.AsSpan(0, frames);
            Gain.CalculateSampleAccurateValues(values);
            output.CopyWithSampleAccurateGainValuesFrom(input, values);
            return;
        }
        var gain = sampleAccurate ? Gain.FinalValue() : Gain.ValueNow();
        if (gain == 0)
            output.Zero();
        else
            output.CopyWithGainFrom(input, gain);
    }

    protected override void ProcessOnlyAudioParams(int frames) => Gain.CalculateSampleAccurateValues(_gainValues.AsSpan(0, frames));
}
