using System.Collections.Concurrent;

namespace Statventure.Audio;

/// <summary>
/// The game's sound effects (Docs/Audio.md, step 8): patches registered by id and played one-shot on
/// the effects bus, dry, with a volume, pan and pitch from the caller and optional per-play ranges
/// drawn from a seeded mulberry32. Past a patch's voice limit, or the overall 32, the oldest voice
/// fades out over 5 ms and the new one plays. Calls are queued and applied by the audio thread before
/// its next quantum, like the music's; a finished voice's nodes are disconnected at the end of the
/// quantum its last layer ended in.
/// </summary>
public sealed class SoundEffects
{
    /// <summary>The most voices sounding at once, over all patches.</summary>
    public const int MaxVoicesOverall = 32;
    const double StealFade = 0.005;

    sealed record Registered(string Id, EffectPatch Patch, PeriodicWave?[] Waves);

    sealed class Voice(string patch, GainNode gain, List<AudioScheduledSourceNode> sources)
    {
        public string Patch { get; } = patch;
        public GainNode Gain { get; } = gain;
        public List<AudioScheduledSourceNode> Sources { get; } = sources;
    }

    readonly AudioGraph _graph;
    readonly AudioNode _bus;
    readonly AudioBuffer _noise;
    readonly Action<Action> _dispatch;
    readonly ConcurrentDictionary<string, Registered> _patches = new();
    readonly List<Voice> _voices = [];
    Mulberry32 _random = new(DefaultSeed);

    const uint DefaultSeed = 7001;

    internal SoundEffects(AudioGraph graph, AudioNode bus, AudioBuffer noise, Action<Action> dispatch)
    {
        _graph = graph;
        _bus = bus;
        _noise = noise;
        _dispatch = dispatch;
    }

    /// <summary>Makes <paramref name="patch"/> playable as <paramref name="id"/> (a patch registered again replaces the old one for later plays).</summary>
    public void Register(string id, EffectPatch patch)
    {
        if (patch.Layers.Length == 0)
            throw new ArgumentException($"Effect '{id}' has no layers", nameof(patch));
        if (patch.MaxVoices < 1)
            throw new ArgumentException($"Effect '{id}' allows no voices", nameof(patch));
        var waves = new PeriodicWave?[patch.Layers.Length];
        for (var i = 0; i < waves.Length; i++)
        {
            var layer = patch.Layers[i];
            if (!(layer.Length > 0) || !(layer.Attack >= 0) || !(layer.Delay >= 0))
                throw new ArgumentException($"Effect '{id}': layer {i} needs a length above 0, and no negative attack or delay", nameof(patch));
            if (layer.Source != EffectSource.Harmonics)
                continue;
            if (layer.Harmonics is not { Length: > 0 } harmonics)
                throw new ArgumentException($"Effect '{id}': layer {i} has no harmonics", nameof(patch));
            // Made here, not on the audio thread: a periodic wave's tables take a while.
            var imag = harmonics.Select(h => (float)h).ToArray();
            waves[i] = _graph.CreatePeriodicWave(new float[imag.Length], imag);
        }
        _patches[id] = new Registered(id, patch, waves);
    }

    /// <summary>Starts the generator behind the patches' ranges and noise offsets anew (for replays).</summary>
    public void Seed(uint seed) => _dispatch(() => _random = new Mulberry32(seed));

    /// <summary>Plays a registered patch once: <paramref name="volume"/> and <paramref name="pitch"/> as multipliers, <paramref name="pan"/> from −1 to 1.</summary>
    public void Play(string id, double volume = 1, double pan = 0, double pitch = 1)
    {
        if (!_patches.TryGetValue(id, out var registered))
            throw new ArgumentException($"No effect '{id}'", nameof(id));
        if (!double.IsFinite(volume) || !double.IsFinite(pan) || !double.IsFinite(pitch) || !(pitch > 0))
            throw new ArgumentOutOfRangeException(nameof(pitch), "the volume, pan and pitch must be finite, the pitch above 0");
        _dispatch(() => Start(registered, volume, pan, pitch));
    }

    double Draw(EffectRange? range) => range == null ? 1 : range.Min + _random.Next() * (range.Max - range.Min);

    double ClampHz(double value) => Math.Max(30, Math.Min(_graph.SampleRate / 2 - 200, value));

    /// <summary>How many voices are sounding (as the audio thread last applied the calls).</summary>
    internal int ActiveVoices => _voices.Count;

    /// <summary>The oldest voice fades out over 5 ms and stops; it no longer counts.</summary>
    void Steal(Voice voice)
    {
        var now = _graph.CurrentTime;
        voice.Gain.Gain.CancelAndHoldAtTime(now);
        voice.Gain.Gain.LinearRampToValueAtTime(0, now + StealFade);
        foreach (var source in voice.Sources)
            source.Stop(now + StealFade);
        _voices.Remove(voice);
    }

    void Start(Registered registered, double volume, double pan, double pitch)
    {
        var patch = registered.Patch;
        while (_voices.Count(v => v.Patch == registered.Id) >= patch.MaxVoices)
            Steal(_voices.First(v => v.Patch == registered.Id));
        while (_voices.Count >= MaxVoicesOverall)
            Steal(_voices[0]);

        pitch *= Draw(patch.Pitch);
        var level = patch.Level * volume * Draw(patch.Volume);
        var when = _graph.CurrentTime;
        var gain = new GainNode(_graph);
        gain.Gain.Value = (float)level;
        var panner = new StereoPannerNode(_graph);
        panner.Pan.Value = (float)Math.Max(-1, Math.Min(1, pan));
        gain.Connect(panner);
        panner.Connect(_bus);
        var nodes = new List<AudioNode> { gain, panner };
        var sources = new List<AudioScheduledSourceNode>();
        AudioScheduledSourceNode? last = null;
        var lastStop = double.NegativeInfinity;
        for (var i = 0; i < patch.Layers.Length; i++)
        {
            var layer = patch.Layers[i];
            var start = when + layer.Delay;
            var stop = start + layer.Attack + layer.Length + 0.05;
            AudioScheduledSourceNode source;
            if (layer.Source == EffectSource.Noise)
            {
                var noise = new AudioBufferSourceNode(_graph) { Buffer = _noise, Loop = true };
                noise.Start(start, _random.Next() * 1.5);
                source = noise;
            }
            else
            {
                var oscillator = new OscillatorNode(_graph);
                if (registered.Waves[i] is { } wave)
                    oscillator.SetPeriodicWave(wave);
                else
                    oscillator.Type = layer.Source switch
                    {
                        EffectSource.Square => OscillatorType.Square,
                        EffectSource.Sawtooth => OscillatorType.Sawtooth,
                        EffectSource.Triangle => OscillatorType.Triangle,
                        _ => OscillatorType.Sine,
                    };
                oscillator.Frequency.SetValueAtTime((float)ClampHz(layer.Frequency * pitch), start);
                if (layer.FrequencyTo is { } to)
                    oscillator.Frequency.ExponentialRampToValueAtTime((float)ClampHz(to * pitch), start + (layer.SweepTime ?? layer.Length * 0.6));
                oscillator.Start(start);
                source = oscillator;
            }
            source.Stop(stop);
            AudioNode head = source;
            if (layer.Filter is { } spec)
            {
                var filter = new BiquadFilterNode(_graph) { Type = (BiquadFilterType)(int)spec.Type };
                filter.Frequency.SetValueAtTime((float)ClampHz(spec.Frequency * pitch), start);
                if (spec.FrequencyTo is { } filterTo)
                    filter.Frequency.ExponentialRampToValueAtTime((float)ClampHz(filterTo * pitch), start + (spec.SweepTime ?? layer.Length * 0.6));
                filter.Q.Value = (float)spec.Q;
                if (spec.Gain is { } decibels)
                    filter.Gain.Value = (float)decibels;
                head.Connect(filter);
                head = filter;
                nodes.Add(filter);
            }
            var envelope = new GainNode(_graph);
            envelope.Gain.SetValueAtTime(0.0001f, start);
            envelope.Gain.LinearRampToValueAtTime((float)layer.Level, start + layer.Attack);
            envelope.Gain.SetTargetAtTime(0, start + layer.Attack, layer.Length / 4);
            head.Connect(envelope);
            envelope.Connect(gain);
            nodes.Add(source);
            nodes.Add(envelope);
            sources.Add(source);
            if (stop > lastStop)
            {
                last = source;
                lastStop = stop;
            }
        }
        var voice = new Voice(registered.Id, gain, sources);
        _voices.Add(voice);
        last!.Ended += _ =>
        {
            _voices.Remove(voice);
            foreach (var node in nodes)
                node.Disconnect();
        };
    }
}

/// <summary>
/// An actor's sounds (step 8): a table from clip event name to effect, played when the actor's animator
/// delivers the event, at the pan and volume the game sets here.
/// </summary>
public sealed class ActorSounds(SoundEffects effects)
{
    /// <summary>Event name → effect id; events not in the table play nothing.</summary>
    public Dictionary<string, string> Table { get; } = [];
    public double Pan { get; set; }
    public double Volume { get; set; } = 1;

    internal void OnEvent(string name)
    {
        if (Table.TryGetValue(name, out var effect))
            effects.Play(effect, Volume, Pan);
    }
}
