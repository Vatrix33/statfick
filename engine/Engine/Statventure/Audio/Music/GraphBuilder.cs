using System.Globalization;
using System.Runtime.CompilerServices;

namespace Statventure.Audio;

/// <summary>
/// The composer's way to the private nodes: every node, connection, parameter change and start the
/// old scripts made through Web Audio goes through here, as one call each, in the scripts' order.
/// With a log it also records each call as one line (parameter values as the float bits Web Audio's
/// IDL turned them into, times as double bits), the same lines the reference harness records from
/// the real composer on a stand-in context (Tools/AudioReference/music.cjs), so the whole schedule of
/// a theme is compared exactly (Docs/Audio.md, step 6). One builder per graph: it also holds the
/// composer's per-context resources (noise, periodic waves, plucked strings, rooms).
/// </summary>
internal sealed class GraphBuilder
{
    readonly List<string>? _log;
    readonly Dictionary<object, int> _ids = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<AudioParam, (int Node, string Name)> _params = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<AudioBuffer> _hashedBuffers = new(ReferenceEqualityComparer.Instance);
    int _nextNode = 1, _nextBuffer, _nextWave;

    public GraphBuilder(AudioGraph graph, List<string>? log = null)
    {
        Graph = graph;
        _log = log;
        _ids[graph.Destination] = 0;
    }

    public AudioGraph Graph { get; }

    /// <summary>ctx.sampleRate: the float rate, as JavaScript reads it.</summary>
    public double SampleRate => Graph.SampleRateF;

    /// <summary>The composer's per-context resources (music-composer.js's kit); made on first use.</summary>
    internal ComposerKit? Kit { get; set; }

    /// <summary>Rooms prepared off the audio thread (step 7): a room's response and its convolver kernels, or null to make them here.</summary>
    internal Func<string, PreparedRoom?>? PreparedRooms { get; set; }

    public bool Logging => _log != null;

    void Log(string line) => _log?.Add(line);

    static string F(double value) => BitConverter.SingleToUInt32Bits((float)value).ToString("x8");
    static string T(double time) => BitConverter.DoubleToUInt64Bits(time).ToString("x16");

    string Id(object node) => "n" + _ids[node].ToString(CultureInfo.InvariantCulture);

    T Register<T>(T node, string kind, params (AudioParam Param, string Name)[] parameters) where T : AudioNode
    {
        var id = _nextNode++;
        _ids[node] = id;
        foreach (var (param, name) in parameters)
            _params[param] = (id, name);
        Log($"n{id}={kind}");
        return node;
    }

    public GainNode Gain()
    {
        var node = new GainNode(Graph);
        return Register(node, "gain", (node.Gain, "gain"));
    }

    public OscillatorNode Oscillator()
    {
        var node = new OscillatorNode(Graph);
        return Register(node, "osc", (node.Frequency, "frequency"), (node.Detune, "detune"));
    }

    public BiquadFilterNode Biquad()
    {
        var node = new BiquadFilterNode(Graph);
        return Register(node, "biquad", (node.Frequency, "frequency"), (node.Q, "Q"), (node.Gain, "gain"), (node.Detune, "detune"));
    }

    public AudioBufferSourceNode BufferSource()
    {
        var node = new AudioBufferSourceNode(Graph);
        return Register(node, "source", (node.PlaybackRate, "playbackRate"), (node.Detune, "detune"));
    }

    public ConvolverNode Convolver() => Register(new ConvolverNode(Graph), "convolver");

    public StereoPannerNode Panner()
    {
        var node = new StereoPannerNode(Graph);
        return Register(node, "panner", (node.Pan, "pan"));
    }

    /// <summary>ctx.createBuffer(channels, length, ctx.sampleRate).</summary>
    public AudioBuffer Buffer(int channels, int length)
    {
        var buffer = new AudioBuffer(channels, length, Graph.SampleRateF);
        var id = _nextBuffer++;
        _ids[buffer] = id;
        Log($"b{id}={channels}x{length}");
        return buffer;
    }

    /// <summary>ctx.createPeriodicWave(new Float32Array(n), Float32Array.from(harmonics)).</summary>
    public PeriodicWave Wave(double[] harmonics)
    {
        var imag = harmonics.Select(h => (float)h).ToArray();
        var wave = Graph.CreatePeriodicWave(new float[imag.Length], imag);
        var id = _nextWave++;
        _ids[wave] = id;
        if (Logging)
            Log($"w{id}=wave {imag.Length} {HashFloats(imag):x16}");
        return wave;
    }

    internal static ulong HashFloats(ReadOnlySpan<float> values, ulong hash = 0xcbf29ce484222325)
    {
        foreach (var value in values)
        {
            var bits = BitConverter.SingleToUInt32Bits(value);
            for (var i = 0; i < 4; i++)
            {
                hash ^= (bits >> (8 * i)) & 0xff;
                hash *= 0x100000001b3;
            }
        }
        return hash;
    }

    string P(AudioParam param)
    {
        var (node, name) = _params[param];
        return $"n{node}.{name}";
    }

    /// <summary>param.value = value.</summary>
    public void Value(AudioParam param, double value)
    {
        param.Value = (float)value;
        if (Logging)
            Log($"{P(param)}={F(value)}");
    }

    public void SetValueAtTime(AudioParam param, double value, double time)
    {
        param.SetValueAtTime((float)value, time);
        if (Logging)
            Log($"{P(param)}.set({F(value)},{T(time)})");
    }

    public void LinearRamp(AudioParam param, double value, double time)
    {
        param.LinearRampToValueAtTime((float)value, time);
        if (Logging)
            Log($"{P(param)}.lin({F(value)},{T(time)})");
    }

    public void ExponentialRamp(AudioParam param, double value, double time)
    {
        param.ExponentialRampToValueAtTime((float)value, time);
        if (Logging)
            Log($"{P(param)}.exp({F(value)},{T(time)})");
    }

    public void SetTarget(AudioParam param, double target, double time, double timeConstant)
    {
        param.SetTargetAtTime((float)target, time, timeConstant);
        if (Logging)
            Log($"{P(param)}.target({F(target)},{T(time)},{T(timeConstant)})");
    }

    public void Connect(AudioNode from, AudioNode to)
    {
        from.Connect(to);
        if (Logging)
            Log($"{Id(from)}>{Id(to)}");
    }

    public void Connect(AudioNode from, AudioParam to)
    {
        from.Connect(to);
        if (Logging)
            Log($"{Id(from)}>{P(to)}");
    }

    public void Start(AudioScheduledSourceNode source, double when)
    {
        source.Start(when);
        if (Logging)
            Log($"{Id(source)}.start({T(when)})");
    }

    public void Start(AudioBufferSourceNode source, double when, double offset)
    {
        source.Start(when, offset);
        if (Logging)
            Log($"{Id(source)}.start({T(when)},{T(offset)})");
    }

    public void Stop(AudioScheduledSourceNode source, double when)
    {
        source.Stop(when);
        if (Logging)
            Log($"{Id(source)}.stop({T(when)})");
    }

    static string Name(BiquadFilterType type) => type switch
    {
        BiquadFilterType.Lowpass => "lowpass", BiquadFilterType.Highpass => "highpass", BiquadFilterType.Bandpass => "bandpass",
        BiquadFilterType.Lowshelf => "lowshelf", BiquadFilterType.Highshelf => "highshelf", BiquadFilterType.Peaking => "peaking",
        BiquadFilterType.Notch => "notch", _ => "allpass",
    };

    static string Name(OscillatorType type) => type switch
    {
        OscillatorType.Sine => "sine", OscillatorType.Square => "square", OscillatorType.Sawtooth => "sawtooth", _ => "triangle",
    };

    public void Type(BiquadFilterNode filter, BiquadFilterType type)
    {
        filter.Type = type;
        if (Logging)
            Log($"{Id(filter)}.type={Name(type)}");
    }

    public void Type(OscillatorNode oscillator, OscillatorType type)
    {
        oscillator.Type = type;
        if (Logging)
            Log($"{Id(oscillator)}.type={Name(type)}");
    }

    public void SetWave(OscillatorNode oscillator, PeriodicWave wave)
    {
        oscillator.SetPeriodicWave(wave);
        if (Logging)
            Log($"{Id(oscillator)}.wave=w{_ids[wave]}");
    }

    string BufferLine(AudioBuffer buffer)
    {
        var line = "b" + _ids[buffer].ToString(CultureInfo.InvariantCulture);
        if (!_hashedBuffers.Add(buffer))
            return line;
        var hash = 0xcbf29ce484222325ul;
        for (var c = 0; c < buffer.NumberOfChannels; c++)
            hash = HashFloats(buffer.GetChannelData(c), hash);
        return $"{line} #{hash:x16}";
    }

    public void SetBuffer(AudioBufferSourceNode source, AudioBuffer buffer)
    {
        source.Buffer = buffer;
        if (Logging)
            Log($"{Id(source)}.buffer={BufferLine(buffer)}");
    }

    public void SetBuffer(ConvolverNode convolver, AudioBuffer buffer, Reverb.Prepared? prepared = null)
    {
        convolver.SetBuffer(buffer, prepared);
        if (Logging)
            Log($"{Id(convolver)}.buffer={BufferLine(buffer)}");
    }

    public void Loop(AudioBufferSourceNode source)
    {
        source.Loop = true;
        if (Logging)
            Log($"{Id(source)}.loop=true");
    }

    /// <summary>
    /// first.onended = () =&gt; nodes.forEach(node =&gt; node.disconnect()): run at the end of the render
    /// quantum in which the source ended (step 6's decision; Chromium runs it on the main thread later).
    /// </summary>
    public void Cleanup(AudioScheduledSourceNode first, List<AudioNode> nodes)
    {
        first.Ended += _ =>
        {
            foreach (var node in nodes)
                node.Disconnect();
        };
        if (Logging)
            Log($"{Id(first)}.onended={string.Join(",", nodes.Select(Id))}");
    }
}

/// <summary>A room made ahead: its impulse response and the convolver kernels for it (normalized, as the composer's convolvers are).</summary>
internal sealed record PreparedRoom(AudioBuffer Buffer, Reverb.Prepared Response)
{
    /// <summary>Makes <paramref name="roomName"/>'s response at <paramref name="sampleRate"/> and its kernels; safe on any thread.</summary>
    public static PreparedRoom Make(string roomName, float sampleRate)
    {
        var buffer = new AudioBuffer(2, Performance.ResponseLength(roomName, sampleRate), sampleRate);
        Performance.FillImpulse(roomName, buffer);
        return new PreparedRoom(buffer, Reverb.Prepare(buffer, normalize: true, responseAlignment: 0));
    }
}
