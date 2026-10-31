namespace Statventure.Audio;

/// <summary>How an input's channel count follows its connections (Web Audio's channelCountMode).</summary>
internal enum ChannelCountMode { Max, ClampedMax, Explicit }

/// <summary>
/// The private audio graph, Blink's Web Audio rendering ported (Chromium 152; Docs/Audio.md): a
/// context with its sample rate and frame clock, rendered in quanta of 128 frames by pulling the
/// destination's input, each node processed once per quantum. Single-threaded: connections and
/// automation take effect at once, and sources' ended callbacks run after the quantum in which
/// they finished (where Blink's main thread would see them a little later, at no fixed time).
/// </summary>
internal sealed class AudioGraph
{
    public const int RenderQuantumFrames = 128;

    readonly List<AudioScheduledSourceNode> _ended = [];
    readonly List<AudioScheduledSourceNode> _activeSources = [];

    public AudioGraph(float sampleRate, int channels = 2)
    {
        if (!(sampleRate >= 3000 && sampleRate <= 768000))
            throw new ArgumentOutOfRangeException(nameof(sampleRate));
        SampleRateF = sampleRate;
        Destination = new AudioDestinationNode(this, channels);
    }

    /// <summary>The rate as the context holds it (a float); <see cref="SampleRate"/> is the same as a double.</summary>
    public float SampleRateF { get; }
    public double SampleRate => SampleRateF;
    public long CurrentSampleFrame { get; private set; }
    public double CurrentTime => CurrentSampleFrame / SampleRate;
    public AudioDestinationNode Destination { get; }

    readonly Dictionary<OscillatorType, PeriodicWave> _basicWaves = [];

    /// <summary>BaseAudioContext::GetPeriodicWave: the built-in shape's tables, made once per context.</summary>
    internal PeriodicWave BasicWave(OscillatorType type)
    {
        if (!_basicWaves.TryGetValue(type, out var wave))
            _basicWaves[type] = wave = PeriodicWave.Basic(SampleRateF, type);
        return wave;
    }

    /// <summary>createPeriodicWave(real, imag, { disableNormalization }).</summary>
    public PeriodicWave CreatePeriodicWave(ReadOnlySpan<float> real, ReadOnlySpan<float> imag, bool disableNormalization = false) =>
        PeriodicWave.Create(SampleRateF, real, imag, disableNormalization);

    internal void NotifyEnded(AudioScheduledSourceNode source) => _ended.Add(source);
    internal void NotifyStarted(AudioScheduledSourceNode source) => _activeSources.Add(source);

    /// <summary>
    /// One render quantum into <paramref name="bus"/> (OfflineAudioDestinationHandler::RenderIfNotSuspended):
    /// the pre-render task finishing sources past their end (BaseAudioContext::HandleStoppableSourceNodes),
    /// the destination's input pulled, then the clock advances and finished sources are released.
    /// </summary>
    public void RenderQuantum(AudioBus bus)
    {
        if (bus.NumberOfChannels != Destination.ChannelCount || bus.Length != RenderQuantumFrames)
            throw new ArgumentException("the bus must have the destination's channels and a quantum's frames", nameof(bus));
        foreach (var source in _activeSources)
            source.HandleStoppableSourceNode();
        var rendered = Destination.Input(0).Pull(bus, RenderQuantumFrames);
        if (rendered != bus)
            bus.CopyFrom(rendered);
        CurrentSampleFrame += RenderQuantumFrames;
        _activeSources.RemoveAll(s => s.HasFinished);
        if (_ended.Count == 0)
            return;
        var ended = _ended.ToArray();
        _ended.Clear();
        foreach (var source in ended)
            source.RaiseEnded();
    }

    /// <summary>An OfflineAudioContext's render of <paramref name="length"/> frames from the current time: planar channels.</summary>
    public float[][] RenderOffline(int length)
    {
        var channels = Destination.ChannelCount;
        var result = new float[channels][];
        for (var c = 0; c < channels; c++)
            result[c] = new float[length];
        var bus = new AudioBus(channels, RenderQuantumFrames);
        for (var done = 0; done < length;)
        {
            RenderQuantum(bus);
            var count = Math.Min(RenderQuantumFrames, length - done);
            for (var c = 0; c < channels; c++)
                bus.Channel(c).Data.AsSpan(0, count).CopyTo(result[c].AsSpan(done));
            done += count;
        }
        return result;
    }
}

/// <summary>
/// A processing node (Blink's AudioHandler with its AudioNode): inputs summing their connections,
/// outputs holding a quantum's bus, processed at most once per quantum; when every input is silent
/// and the node's tail has passed, its outputs are silenced and only its parameters advance.
/// </summary>
internal abstract class AudioNode
{
    readonly List<AudioNodeInput> _inputs = [];
    readonly List<AudioNodeOutput> _outputs = [];
    double _lastProcessingTime = -1;
    double _lastNonSilentTime;
    int _channelCount = 2;
    ChannelCountMode _channelCountMode = ChannelCountMode.Max;

    protected AudioNode(AudioGraph graph) => Graph = graph;

    public AudioGraph Graph { get; }
    public int NumberOfInputs => _inputs.Count;
    public int NumberOfOutputs => _outputs.Count;
    public AudioNodeInput Input(int i) => _inputs[i];
    public AudioNodeOutput Output(int i) => _outputs[i];

    public int ChannelCount
    {
        get => _channelCount;
        set
        {
            if (value < 1 || value > 2)
                throw new ArgumentOutOfRangeException(nameof(value), "only mono and stereo are supported");
            _channelCount = value;
            UpdateChannelsForInputs();
        }
    }

    public ChannelCountMode ChannelCountMode
    {
        get => _channelCountMode;
        set
        {
            _channelCountMode = value;
            UpdateChannelsForInputs();
        }
    }

    public ChannelInterpretation ChannelInterpretation { get; set; } = ChannelInterpretation.Speakers;

    protected void AddInput() => _inputs.Add(new AudioNodeInput(this));
    protected void AddOutput(int channels) => _outputs.Add(new AudioNodeOutput(this, channels));

    // ---------- Connections ----------

    public AudioNode Connect(AudioNode destination, int output = 0, int input = 0)
    {
        if (destination.Graph != Graph)
            throw new ArgumentException("the nodes belong to different graphs");
        destination.Input(input).Connect(Output(output));
        return destination;
    }

    public void Connect(AudioParam destination, int output = 0)
    {
        if (destination.Graph != Graph)
            throw new ArgumentException("the parameter belongs to a different graph");
        if (destination.AddInput(Output(output)))
            Output(output).AddParam(destination);
    }

    /// <summary>disconnect(): every connection from every output, to nodes and parameters.</summary>
    public void Disconnect()
    {
        foreach (var output in _outputs)
            output.DisconnectAll();
    }

    public void Disconnect(AudioNode destination)
    {
        var found = false;
        foreach (var output in _outputs)
            for (var i = 0; i < destination.NumberOfInputs; i++)
                found |= destination.Input(i).Disconnect(output);
        if (!found)
            throw new InvalidOperationException("the node isn't connected to that destination");
    }

    public void Disconnect(AudioParam destination)
    {
        var found = false;
        foreach (var output in _outputs)
            if (destination.RemoveInput(output))
            {
                output.RemoveParam(destination);
                found = true;
            }
        if (!found)
            throw new InvalidOperationException("the node isn't connected to that parameter");
    }

    void UpdateChannelsForInputs()
    {
        foreach (var input in _inputs)
            CheckNumberOfChannelsForInput(input);
    }

    /// <summary>An input's channel count may have changed (AudioHandler::CheckNumberOfChannelsForInput).</summary>
    internal virtual void CheckNumberOfChannelsForInput(AudioNodeInput input) => input.UpdateInternalBus();

    // ---------- Rendering ----------

    /// <summary>The node's tail (seconds of output after silent input) and latency; both 0 unless the node rings or delays.</summary>
    protected virtual double TailTime => 0;
    protected virtual double LatencyTime => 0;

    protected virtual bool PropagatesSilence => _lastNonSilentTime + LatencyTime + TailTime < Graph.CurrentTime;

    internal void ProcessIfNecessary(int frames)
    {
        var currentTime = Graph.CurrentTime;
        if (_lastProcessingTime == currentTime)
            return;
        _lastProcessingTime = currentTime;
        PullInputs(frames);
        var silentInputs = InputsAreSilent();
        if (silentInputs && PropagatesSilence)
        {
            SilenceOutputs();
            ProcessOnlyAudioParams(frames);
        }
        else
        {
            UnsilenceOutputs();
            Process(frames);
        }
        if (!silentInputs)
            _lastNonSilentTime = (Graph.CurrentSampleFrame + frames) / (double)Graph.SampleRateF;
    }

    protected virtual void PullInputs(int frames)
    {
        foreach (var input in _inputs)
            input.Pull(null, frames);
    }

    bool InputsAreSilent()
    {
        foreach (var input in _inputs)
            if (!input.Bus.IsSilent)
                return false;
        return true;
    }

    void SilenceOutputs()
    {
        foreach (var output in _outputs)
            if (output.IsConnected)
                output.Bus.Zero();
    }

    void UnsilenceOutputs()
    {
        foreach (var output in _outputs)
            output.Bus.ClearSilentFlag();
    }

    protected abstract void Process(int frames);
    protected virtual void ProcessOnlyAudioParams(int frames) { }
}

/// <summary>A node's input (Blink's AudioNodeInput): its connections summed into a bus, or one connection passed through.</summary>
internal sealed class AudioNodeInput
{
    readonly List<AudioNodeOutput> _outputs = [];
    AudioBus _summingBus = new(1, AudioGraph.RenderQuantumFrames);

    internal AudioNodeInput(AudioNode node) => Node = node;

    public AudioNode Node { get; }
    public IReadOnlyList<AudioNodeOutput> Connections => _outputs;
    public bool IsConnected => _outputs.Count > 0;

    internal void Connect(AudioNodeOutput output)
    {
        if (_outputs.Contains(output))
            return;
        _outputs.Add(output);
        output.AddInput(this);
        ChangedOutputs();
    }

    internal bool Disconnect(AudioNodeOutput output)
    {
        if (!_outputs.Remove(output))
            return false;
        output.RemoveInput(this);
        ChangedOutputs();
        return true;
    }

    internal void ChangedOutputs() => Node.CheckNumberOfChannelsForInput(this);

    /// <summary>The channel count the input mixes to: the node's count when explicit, else the widest connection (clamped to the node's count).</summary>
    public int NumberOfChannels
    {
        get
        {
            if (Node.ChannelCountMode == ChannelCountMode.Explicit)
                return Node.ChannelCount;
            var max = 1;
            foreach (var output in _outputs)
                max = Math.Max(max, output.NumberOfChannels);
            if (Node.ChannelCountMode == ChannelCountMode.ClampedMax)
                max = Math.Min(max, Node.ChannelCount);
            return max;
        }
    }

    internal void UpdateInternalBus()
    {
        var channels = NumberOfChannels;
        if (channels != _summingBus.NumberOfChannels)
            _summingBus = new AudioBus(channels, AudioGraph.RenderQuantumFrames);
    }

    bool PassesThrough => _outputs.Count == 1 && Node.ChannelCountMode == ChannelCountMode.Max;

    /// <summary>The quantum's input: the one connection's bus when it passes through, else the summing bus.</summary>
    public AudioBus Bus => PassesThrough ? _outputs[0].Bus : _summingBus;

    public AudioBus Pull(AudioBus? inPlace, int frames)
    {
        if (PassesThrough)
            return _outputs[0].Pull(inPlace, frames);
        if (_outputs.Count == 0)
        {
            _summingBus.Zero();
            return _summingBus;
        }
        _summingBus.Zero();
        foreach (var output in _outputs)
            _summingBus.SumFrom(output.Pull(null, frames), Node.ChannelInterpretation);
        return _summingBus;
    }
}

/// <summary>A node's output (Blink's AudioNodeOutput): its bus, its channel count, its connections to inputs and parameters.</summary>
internal sealed class AudioNodeOutput
{
    readonly List<AudioNodeInput> _inputs = [];
    readonly List<AudioParam> _params = [];
    AudioBus _bus;
    AudioBus? _inPlaceBus;

    internal AudioNodeOutput(AudioNode node, int channels)
    {
        Node = node;
        NumberOfChannels = channels;
        _bus = new AudioBus(channels, AudioGraph.RenderQuantumFrames);
    }

    public AudioNode Node { get; }
    public int NumberOfChannels { get; private set; }
    public bool IsConnected => _inputs.Count > 0 || _params.Count > 0;

    internal void AddInput(AudioNodeInput input) => _inputs.Add(input);
    internal void RemoveInput(AudioNodeInput input) => _inputs.Remove(input);
    internal void AddParam(AudioParam param) => _params.Add(param);
    internal void RemoveParam(AudioParam param) => _params.Remove(param);

    internal void DisconnectAll()
    {
        foreach (var input in _inputs.ToArray())
            input.Disconnect(this);
        foreach (var param in _params.ToArray())
            param.RemoveInput(this);
        _params.Clear();
    }

    /// <summary>A new channel count: a new bus, and the connected inputs told (AudioNodeOutput::SetNumberOfChannels).</summary>
    internal void SetNumberOfChannels(int channels)
    {
        if (channels == NumberOfChannels)
            return;
        NumberOfChannels = channels;
        _bus = new AudioBus(channels, AudioGraph.RenderQuantumFrames);
        foreach (var input in _inputs.ToArray())
            input.ChangedOutputs();
    }

    /// <summary>The bus the node renders into: the puller's own when it renders in place (a single connection of matching channels).</summary>
    public AudioBus Bus => _inPlaceBus ?? _bus;

    public AudioBus Pull(AudioBus? inPlace, int frames)
    {
        _inPlaceBus = inPlace != null && inPlace.NumberOfChannels == NumberOfChannels && _inputs.Count + _params.Count == 1 ? inPlace : null;
        Node.ProcessIfNecessary(frames);
        return Bus;
    }
}

/// <summary>The graph's destination (an offline destination: explicit channel count, speakers).</summary>
internal sealed class AudioDestinationNode : AudioNode
{
    internal AudioDestinationNode(AudioGraph graph, int channels) : base(graph)
    {
        AddInput();
        ChannelCountMode = ChannelCountMode.Explicit;
        ChannelCount = channels;
    }

    protected override void Process(int frames) { }
}
