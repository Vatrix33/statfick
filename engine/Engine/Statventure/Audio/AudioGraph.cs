namespace Statventure.Audio;

/// <summary>How an input's channel count follows its connections (Web Audio's channelCountMode).</summary>
internal enum ChannelCountMode { Max, ClampedMax, Explicit }

/// <summary>
/// The private audio graph, Blink's Web Audio rendering ported (Chromium 152; Docs/Audio.md): a
/// context with its sample rate and frame clock, rendered in quanta of 128 frames by pulling the
/// destination's input, each node processed once per quantum. Single-threaded: connections and
/// automation take effect at once, and sources' ended callbacks run after the quantum in which
/// they finished (where Blink's main thread would see them a little later, at no fixed time).
/// Blink's connection counting is kept: a finished source releases its count after the quantum,
/// which disables its outputs (and, down the line, those of nodes left with nothing feeding them;
/// nodes that ring finish their tail first, then are disabled at once, where Blink's main thread
/// does it some time later).
/// </summary>
internal sealed class AudioGraph
{
    public const int RenderQuantumFrames = 128;

    readonly List<AudioScheduledSourceNode> _ended = [];
    readonly List<AudioScheduledSourceNode> _activeSources = [];
    readonly List<AudioNode> _tailProcessing = [];

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

    /// <summary>Sources started and not yet finished, for the tests.</summary>
    internal int ActiveSources => _activeSources.Count;

    /// <summary>
    /// For the tests only (Docs/Audio.md, step 6's fixture sums): when set before the graph is built,
    /// every node output gets a random key from it and inputs sum their connections in key order,
    /// as Chromium's pointer hash sets order them differently on every run.
    /// </summary>
    internal Random? SummingOrder { get; set; }

    /// <summary>
    /// The connections in the order they sum. A node input starts from zero, so two connections sum
    /// alike in either order; a parameter adds its connections to its own values, so two already don't.
    /// </summary>
    internal static IEnumerable<AudioNodeOutput> InSummingOrder(List<AudioNodeOutput> outputs, AudioGraph graph, int orderFree = 2) =>
        graph.SummingOrder != null && outputs.Count > orderFree ? outputs.OrderBy(o => o.OrderKey) : outputs;

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

    /// <summary>NotifySourceNodeStartedProcessing: a started source holds a connection count until it has finished.</summary>
    internal void NotifyStarted(AudioScheduledSourceNode source)
    {
        _activeSources.Add(source);
        source.MakeConnection();
    }

    internal void AddTailProcessing(AudioNode node)
    {
        if (!_tailProcessing.Contains(node))
            _tailProcessing.Add(node);
    }

    internal void RemoveTailProcessing(AudioNode node) => _tailProcessing.Remove(node);

    /// <summary>UpdateTailProcessingHandlers: nodes whose tail has passed are done (and disabled: Blink's main thread does that later).</summary>
    void UpdateTailProcessing()
    {
        for (var k = _tailProcessing.Count; k > 0; k--)
        {
            var node = _tailProcessing[k - 1];
            if (node.PropagatesSilenceNow)
            {
                _tailProcessing.RemoveAt(k - 1);
                node.DisableOutputs();
            }
        }
    }

    /// <summary>
    /// One render quantum into <paramref name="bus"/> (OfflineAudioDestinationHandler::RenderIfNotSuspended):
    /// the pre-render task finishing sources past their end (BaseAudioContext::HandleStoppableSourceNodes),
    /// the destination's input pulled, then the clock advances and finished sources are released.
    /// </summary>
    public void RenderQuantum(AudioBus bus)
    {
        if (bus.NumberOfChannels != Destination.ChannelCount || bus.Length != RenderQuantumFrames)
            throw new ArgumentException("the bus must have the destination's channels and a quantum's frames", nameof(bus));
        // HandlePreRenderTasks: deferred tasks (the tail list), then stoppable sources.
        UpdateTailProcessing();
        foreach (var source in _activeSources)
            source.HandleStoppableSourceNode();
        var rendered = Destination.Input(0).Pull(bus, RenderQuantumFrames);
        if (rendered != bus)
            bus.CopyFrom(rendered);
        // HandlePostRenderTasks: BreakConnections (finished sources release their count), deferred tasks.
        var ended = _ended.ToArray();
        _ended.Clear();
        foreach (var source in ended)
        {
            source.BreakConnection();
            _activeSources.Remove(source);
        }
        UpdateTailProcessing();
        CurrentSampleFrame += RenderQuantumFrames;
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
    int _connectionRefCount;
    bool _isDisabled;
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

    /// <summary>Nodes that ring on after their input stops (filters, convolvers, compressors) finish that tail before they're disabled.</summary>
    protected virtual bool RequiresTailProcessing => false;

    protected virtual bool PropagatesSilence => _lastNonSilentTime + LatencyTime + TailTime < Graph.CurrentTime;
    internal bool PropagatesSilenceNow => PropagatesSilence;

    // ---------- Connection counting and disabled outputs (AudioHandler) ----------

    /// <summary>MakeConnection: one more input connection (or a source started); re-enables the node's outputs.</summary>
    internal void MakeConnection()
    {
        _connectionRefCount++;
        Graph.RemoveTailProcessing(this);
        if (_isDisabled && _connectionRefCount > 0)
            EnableOutputs();
    }

    /// <summary>BreakConnectionWithLock: one connection fewer; at none, the outputs are disabled.</summary>
    internal void BreakConnection()
    {
        _connectionRefCount--;
        if (_connectionRefCount == 0)
            DisableOutputsIfNecessary();
    }

    void DisableOutputsIfNecessary()
    {
        if (_connectionRefCount <= 1 && !_isDisabled)
        {
            if (RequiresTailProcessing)
                Graph.AddTailProcessing(this);
            else
                DisableOutputs();
        }
    }

    /// <summary>DisableOutputs: this node's outputs, then those of the nodes they fed that are left with at most one connection.</summary>
    internal void DisableOutputs()
    {
        var worklist = new List<AudioNode>();
        DisableOutputsInternal(worklist);
        while (worklist.Count > 0)
        {
            var node = worklist[^1];
            worklist.RemoveAt(worklist.Count - 1);
            if (!node._isDisabled && node._connectionRefCount <= 1)
            {
                if (node.RequiresTailProcessing)
                    Graph.AddTailProcessing(node);
                else
                    node.DisableOutputsInternal(worklist);
            }
        }
    }

    void DisableOutputsInternal(List<AudioNode> worklist)
    {
        _isDisabled = true;
        foreach (var output in _outputs)
            output.DisableAndEnqueue(worklist);
    }

    void EnableOutputs()
    {
        var worklist = new List<AudioNode>();
        EnableOutputsInternal(worklist);
        while (worklist.Count > 0)
        {
            var node = worklist[^1];
            worklist.RemoveAt(worklist.Count - 1);
            Graph.RemoveTailProcessing(node);
            if (node._isDisabled && node._connectionRefCount > 0)
                node.EnableOutputsInternal(worklist);
        }
    }

    void EnableOutputsInternal(List<AudioNode> worklist)
    {
        _isDisabled = false;
        foreach (var output in _outputs)
            output.EnableAndEnqueue(worklist);
    }

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
    readonly List<AudioNodeOutput> _disabledOutputs = [];
    AudioBus _summingBus = new(1, AudioGraph.RenderQuantumFrames);

    internal AudioNodeInput(AudioNode node) => Node = node;

    public AudioNode Node { get; }
    /// <summary>The enabled connections (rendered and counted for channels); disabled ones stay connected but aren't heard.</summary>
    public IReadOnlyList<AudioNodeOutput> Connections => _outputs;
    public bool IsConnected => _outputs.Count > 0;

    /// <summary>AudioNodeWiring::Connect.</summary>
    internal void Connect(AudioNodeOutput output)
    {
        if (_outputs.Contains(output) || _disabledOutputs.Contains(output))
            return;
        (output.IsEnabled ? _outputs : _disabledOutputs).Add(output);
        output.AddInput(this);
        if (output.IsEnabled)
            ChangedOutputs();
        Node.MakeConnection();
    }

    /// <summary>AudioNodeWiring::Disconnect.</summary>
    internal bool Disconnect(AudioNodeOutput output)
    {
        var wasEnabled = _outputs.Remove(output);
        if (!wasEnabled && !_disabledOutputs.Remove(output))
            return false;
        output.RemoveInput(this);
        if (wasEnabled)
            ChangedOutputs();
        Node.BreakConnection();
        return true;
    }

    /// <summary>AudioNodeWiring::Disable: true when the connection was enabled.</summary>
    internal bool Disable(AudioNodeOutput output)
    {
        if (_disabledOutputs.Contains(output))
            return false;
        _disabledOutputs.Add(output);
        _outputs.Remove(output);
        ChangedOutputs();
        return true;
    }

    /// <summary>AudioNodeWiring::Enable: true when the connection was disabled.</summary>
    internal bool Enable(AudioNodeOutput output)
    {
        if (_outputs.Contains(output))
            return false;
        _outputs.Add(output);
        _disabledOutputs.Remove(output);
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
        foreach (var output in AudioGraph.InSummingOrder(_outputs, Node.Graph))
            _summingBus.SumFrom(output.Pull(null, frames), Node.ChannelInterpretation);
        return _summingBus;
    }
}

/// <summary>A node's output (Blink's AudioNodeOutput): its bus, its channel count, its connections to inputs and parameters.</summary>
internal sealed class AudioNodeOutput
{
    /// <summary>Its place in a shuffled summing order (<see cref="AudioGraph.SummingOrder"/>).</summary>
    internal long OrderKey { get; }

    readonly List<AudioNodeInput> _inputs = [];
    readonly List<AudioParam> _params = [];
    AudioBus _bus;
    AudioBus? _inPlaceBus;

    internal AudioNodeOutput(AudioNode node, int channels)
    {
        Node = node;
        OrderKey = node.Graph.SummingOrder?.NextInt64() ?? 0;
        NumberOfChannels = channels;
        _bus = new AudioBus(channels, AudioGraph.RenderQuantumFrames);
    }

    public AudioNode Node { get; }
    public int NumberOfChannels { get; private set; }
    public bool IsConnected => _inputs.Count > 0 || _params.Count > 0;
    public bool IsEnabled { get; private set; } = true;

    internal void DisableAndEnqueue(List<AudioNode> worklist)
    {
        if (!IsEnabled)
            return;
        IsEnabled = false;
        foreach (var input in _inputs)
            if (input.Disable(this))
                worklist.Add(input.Node);
    }

    internal void EnableAndEnqueue(List<AudioNode> worklist)
    {
        if (IsEnabled)
            return;
        IsEnabled = true;
        foreach (var input in _inputs)
            if (input.Enable(this))
                worklist.Add(input.Node);
    }

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
