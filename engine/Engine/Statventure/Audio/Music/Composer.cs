namespace Statventure.Audio;

/// <summary>music-composer.js's mulberry32 (and its FNV-1a string hash), the old music's only randomness.</summary>
internal sealed class Mulberry32(uint seed)
{
    uint _state = seed;

    public double Next()
    {
        _state += 0x6D2B79F5;
        var t = _state;
        t = (t ^ (t >> 15)) * (t | 1);
        t ^= t + (t ^ (t >> 7)) * (t | 61);
        return (t ^ (t >> 14)) / 4294967296.0;
    }

    /// <summary>hash(text): FNV-1a over the first UTF-16 unit of each code point, as [...String(text)] iterates.</summary>
    public static uint Hash(string text)
    {
        var value = 2166136261u;
        foreach (var rune in text.EnumerateRunes())
        {
            Span<char> units = stackalloc char[2];
            rune.EncodeToUtf16(units);
            value = (value ^ units[0]) * 16777619;
        }
        return value;
    }
}

/// <summary>A Karplus-Strong string's buffer and the playback rate that tunes its whole-sample period.</summary>
internal sealed record PluckTable(AudioBuffer Buffer, double Rate);

/// <summary>music-composer.js's per-context resources: two seconds of noise, periodic waves, plucked strings and rooms.</summary>
internal sealed class ComposerKit
{
    public required AudioBuffer Noise { get; init; }
    public Dictionary<string, PeriodicWave> Waves { get; } = [];
    public Dictionary<string, PluckTable> Plucks { get; } = [];
    public Dictionary<string, AudioBuffer> Rooms { get; } = [];
    /// <summary>The convolver kernels made for a room's buffer off the audio thread (step 7), shared by every performance in it.</summary>
    public Dictionary<AudioBuffer, Reverb.Prepared> PreparedResponses { get; } = new(ReferenceEqualityComparer.Instance);

    public static ComposerKit For(GraphBuilder builder)
    {
        if (builder.Kit != null)
            return builder.Kit;
        var noise = builder.Buffer(1, (int)(builder.SampleRate * 2));
        var data = noise.GetChannelData(0);
        var random = new Mulberry32(9001);
        for (var i = 0; i < data.Length; i++)
            data[i] = (float)(random.Next() * 2 - 1);
        return builder.Kit = new ComposerKit { Noise = noise };
    }
}

/// <summary>
/// music-composer.js's perform ported (Docs/Audio.md, step 6): a theme's EQ, dry path and convolution
/// room, a channel per instrument with its send, the per-note graphs of each instrument kind, the
/// percussion and textures, and the sequencer that turns a step of the compiled score into notes.
/// Every random draw, double computation and Web Audio call happens in the script's order, through
/// <see cref="GraphBuilder"/>.
/// </summary>
internal sealed class Performance
{
    readonly GraphBuilder _g;
    readonly CompiledTheme _compiled;
    readonly ComposerKit _kit;
    readonly Mulberry32 _random;
    readonly double _nyquist;
    readonly GainNode _dry, _send;
    readonly Dictionary<(string, double), GainNode> _channels = [];
    readonly Dictionary<CompiledLayer, double[]> _voicings = new(ReferenceEqualityComparer.Instance);
    readonly HashSet<string>? _only;

    IReadOnlyDictionary<string, InstrumentSpec> I => _g.Library.Instruments;
    IReadOnlyDictionary<string, PercussionSpec> P => _g.Library.Percussion;

    public Performance(GraphBuilder builder, CompiledTheme compiled, AudioNode destination, uint seed = 0, IEnumerable<string>? only = null)
    {
        _g = builder;
        _compiled = compiled;
        _kit = ComposerKit.For(builder);
        _random = new Mulberry32(Mulberry32.Hash(compiled.Id) ^ seed);
        _nyquist = builder.SampleRate / 2 - 200;
        Output = _g.Gain();
        _g.Connect(Output, destination);
        BiquadFilterNode lowCut = _g.Biquad(), lowShelf = _g.Biquad(), presence = _g.Biquad();
        _g.Type(lowCut, BiquadFilterType.Highpass);
        _g.Value(lowCut.Frequency, 34);
        _g.Value(lowCut.Q, 0.7);
        _g.Type(lowShelf, BiquadFilterType.Lowshelf);
        _g.Value(lowShelf.Frequency, 140);
        _g.Value(lowShelf.Gain, compiled.Spec.Eq?.Low ?? -3);
        _g.Type(presence, BiquadFilterType.Peaking);
        _g.Value(presence.Frequency, 2600);
        _g.Value(presence.Q, 0.8);
        _g.Value(presence.Gain, compiled.Spec.Eq?.Presence ?? 2);
        _g.Connect(lowCut, lowShelf);
        _g.Connect(lowShelf, presence);
        _g.Connect(presence, Output);
        _dry = _g.Gain();
        _g.Value(_dry.Gain, compiled.Gain);
        _g.Connect(_dry, lowCut);
        var room = builder.Library.Room(compiled.Room);
        var reverb = _g.Convolver();
        var response = Impulse(compiled.Room);
        _g.SetBuffer(reverb, response, _kit.PreparedResponses.GetValueOrDefault(response));
        var wet = _g.Gain();
        _g.Value(wet.Gain, room.Wet * compiled.Gain * 2.2);
        _send = _g.Gain();
        _g.Connect(_send, reverb);
        _g.Connect(reverb, wet);
        _g.Connect(wet, lowCut);
        _only = only != null ? [.. only] : null;
    }

    /// <summary>The performance's bus, into the destination it was given.</summary>
    public GainNode Output { get; }

    public double StepSeconds => _compiled.StepSeconds;
    public double TotalSteps => _compiled.TotalSteps;
    public double LoopStep => _compiled.LoopStep;

    static double Hz(double midi) => 440 * JsMath.Pow(2, (midi - 69) / 12);

    double ClampHz(double value) => Math.Max(30, Math.Min(_nyquist, value));

    GainNode Channel(string name, double sendLevel)
    {
        if (!_channels.TryGetValue((name, sendLevel), out var input))
        {
            input = _g.Gain();
            var toRoom = _g.Gain();
            _g.Value(toRoom.Gain, sendLevel);
            _g.Connect(input, _dry);
            _g.Connect(input, toRoom);
            _g.Connect(toRoom, _send);
            _channels[(name, sendLevel)] = input;
        }
        return input;
    }

    StereoPannerNode Panner(double value)
    {
        var node = _g.Panner();
        _g.Value(node.Pan, Math.Max(-1, Math.Min(1, value)));
        return node;
    }

    // ---------------- Resources ----------------

    PeriodicWave Periodic(double[] harmonics)
    {
        var key = string.Join(",", harmonics.Select(Score.JsString));
        if (!_kit.Waves.TryGetValue(key, out var wave))
            _kit.Waves[key] = wave = _g.Wave(harmonics);
        return wave;
    }

    /// <summary>A room's stereo impulse response: smoothed seeded noise decaying over its length, darker as it goes, and early reflections.</summary>
    AudioBuffer Impulse(string roomName)
    {
        if (_kit.Rooms.TryGetValue(roomName, out var cached))
            return cached;
        AudioBuffer buffer;
        if (_g.PreparedRooms?.Invoke(roomName) is { } prepared)
        {
            buffer = prepared.Buffer;
            _kit.PreparedResponses[buffer] = prepared.Response;
        }
        else
        {
            var room = _g.Library.Room(roomName);
            buffer = _g.Buffer(2, ResponseLength(room, _g.SampleRate));
            FillImpulse(roomName, room, buffer);
        }
        _kit.Rooms[roomName] = buffer;
        return buffer;
    }

    /// <summary>The frames of a room's response at <paramref name="rate"/>.</summary>
    internal static int ResponseLength(RoomSpec room, double rate) => (int)Math.Ceiling(room.Seconds * rate);

    /// <summary>
    /// impulse()'s samples for the room called <paramref name="roomName"/> (its noise seeded by the name),
    /// sounding as <paramref name="room"/> (the hall's values when the library has no such room), into a
    /// stereo buffer of <see cref="ResponseLength"/> frames at the buffer's rate.
    /// </summary>
    internal static void FillImpulse(string roomName, RoomSpec room, AudioBuffer buffer)
    {
        double rate = buffer.SampleRate;
        var length = buffer.Length;
        for (var channel = 0; channel < 2; channel++)
        {
            var samples = buffer.GetChannelData(channel);
            var random = new Mulberry32(77 + (uint)channel * 991 + Mulberry32.Hash(roomName));
            double smooth = 0;
            for (var i = 0; i < length; i++)
            {
                double t = i / rate, progress = t / room.Seconds;
                var coefficient = 1 - room.Dark * Math.Min(1, progress * 1.6) * 0.92;
                smooth += coefficient * ((random.Next() * 2 - 1) - smooth);
                samples[i] = (float)(smooth * JsMath.Exp(-6.9 * progress) * Math.Min(1, t / 0.012));
            }
            // A few early reflections in the first 70 ms give the room a shape.
            for (var k = 0; k < 7; k++)
            {
                var at = (int)Math.Floor((0.008 + random.Next() * 0.065) * rate);
                samples[at] = (float)(samples[at] + (random.Next() < 0.5 ? -1 : 1) * (0.5 - k * 0.05));
            }
        }
    }

    /// <summary>Karplus-Strong: a noise burst circulating in a damped delay line of one period.</summary>
    PluckTable PluckBuffer(string name, InstrumentSpec def, double midi)
    {
        var key = name + ":" + Score.JsString(midi);
        if (_kit.Plucks.TryGetValue(key, out var cached))
            return cached;
        if (_kit.Plucks.Count > 220)
            _kit.Plucks.Clear();
        var rate = _g.SampleRate;
        var frequency = Hz(midi);
        double damp = def.Damp ?? double.NaN, bright = def.Bright ?? double.NaN;
        var period = (int)Math.Max(2, Math.Floor(rate / frequency - damp));
        var actual = rate / (period + damp);
        var length = (int)Math.Ceiling((def.Length ?? double.NaN) * rate);
        var buffer = _g.Buffer(1, length);
        var y = buffer.GetChannelData(0);
        var random = new Mulberry32(Mulberry32.Hash(key));
        double low = 0;
        for (var i = 0; i < period; i++)
        {
            low += bright * ((random.Next() * 2 - 1) - low);
            y[i] = (float)low;
        }
        var loss = JsMath.Pow(0.001, 1 / ((def.Decay ?? double.NaN) * actual));
        for (var i = period; i < length; i++)
            y[i] = (float)(loss * ((1 - damp) * y[i - period] + damp * (i - period - 1 >= 0 ? y[i - period - 1] : 0)));
        double dc = 0, peak = 0;
        for (var i = 0; i < length; i++)
        {
            dc += 0.002 * (y[i] - dc);
            y[i] = (float)(y[i] - dc);
            peak = Math.Max(peak, Math.Abs(y[i]));
        }
        // A 2 ms onset ramp keeps the pick sharp without a raw click; the tail fades over 40 ms.
        double fade = Math.Floor(rate * 0.04), onset = Math.Floor(rate * 0.002);
        var divisor = peak != 0 ? peak : 1;
        for (var i = 0; i < length; i++)
            y[i] = (float)(y[i] / divisor * Math.Min(1, Math.Min((length - i) / fade, (i + 1) / onset)));
        var entry = new PluckTable(buffer, frequency / actual);
        _kit.Plucks[key] = entry;
        return entry;
    }

    // ---------------- Instruments ----------------

    double Envelope(AudioParam gain, double when, double length, double peak, InstrumentSpec def)
    {
        var attack = Math.Min(def.Attack ?? double.NaN, Math.Max(0.005, length * 0.7));
        var release = def.Release ?? double.NaN;
        _g.SetValueAtTime(gain, 0.0001, when);
        _g.LinearRamp(gain, peak, when + attack);
        var end = when + Math.Max(length, attack + 0.01);
        if (def.Decay is { } decay and not 0 && !double.IsNaN(decay))
            _g.SetTarget(gain, peak * 0.3, when + attack, decay);
        else
            _g.LinearRamp(gain, peak * 0.8, end);
        _g.SetTarget(gain, 0, end, Math.Max(0.02, release / 3));
        return end + release * 1.7 + 0.05;
    }

    void Vibrato(InstrumentSpec def, double when, double stop, List<AudioParam> targets, List<AudioNode> nodes)
    {
        if (def.Vibrato is not { } vib)
            return;
        double rate = vib[0], depth = vib[1], delay = vib[2];
        var lfo = _g.Oscillator();
        var amount = _g.Gain();
        _g.Value(lfo.Frequency, rate * (0.92 + _random.Next() * 0.16));
        _g.SetValueAtTime(amount.Gain, 0, when);
        _g.SetValueAtTime(amount.Gain, 0, when + delay);
        _g.LinearRamp(amount.Gain, depth, when + delay + 0.35);
        _g.Connect(lfo, amount);
        foreach (var param in targets)
            _g.Connect(amount, param);
        _g.Start(lfo, when);
        _g.Stop(lfo, stop);
        nodes.Add(lfo);
        nodes.Add(amount);
    }

    void Play(string? name, double midi, double when, double length, double velocity, double? pan)
    {
        if (name == null || !I.TryGetValue(name, out var def))
            throw new InvalidOperationException("Unknown instrument " + (name ?? "undefined"));
        var frequency = Hz(midi);
        var output = Channel(name, def.Send ?? 0.5);
        var pan2 = Panner((pan ?? def.Pan ?? 0) + (_random.Next() - 0.5) * 0.12);
        _g.Connect(pan2, output);
        var peak = def.Level * velocity;
        var nodes = new List<AudioNode> { pan2 };
        var keyTrack = Math.Min(1.7, Math.Max(0.55, JsMath.Pow(frequency / 330, 0.35)));

        if (def.Kind == "pluck")
        {
            var (buffer, rate) = PluckBuffer(name, def, midi);
            var source = _g.BufferSource();
            var gain = _g.Gain();
            _g.SetBuffer(source, buffer);
            _g.Value(source.PlaybackRate, rate);
            _g.Value(gain.Gain, peak);
            AudioNode head = source;
            if (def.Body is { } bodyHz and not 0)
            {
                var body = _g.Biquad();
                _g.Type(body, BiquadFilterType.Peaking);
                _g.Value(body.Frequency, bodyHz);
                _g.Value(body.Gain, 5);
                _g.Value(body.Q, 1.2);
                _g.Connect(head, body);
                head = body;
                nodes.Add(body);
            }
            if (def.Highpass is { } highpassHz and not 0)
            {
                var hp = _g.Biquad();
                _g.Type(hp, BiquadFilterType.Highpass);
                _g.Value(hp.Frequency, highpassHz);
                _g.Connect(head, hp);
                head = hp;
                nodes.Add(hp);
            }
            _g.Connect(head, gain);
            _g.Connect(gain, pan2);
            var ring = Math.Min(def.Length ?? double.NaN, Math.Max(length * 3, 0.4));
            _g.SetValueAtTime(gain.Gain, peak, when + ring - 0.12);
            _g.LinearRamp(gain.Gain, 0.0001, when + ring);
            _g.Start(source, when);
            _g.Stop(source, when + ring + 0.02);
            nodes.Add(source);
            nodes.Add(gain);
            _g.Cleanup(source, nodes);
            return;
        }
        if (def.Kind == "mallet")
        {
            var sum = _g.Gain();
            _g.Value(sum.Gain, peak);
            _g.Connect(sum, pan2);
            nodes.Add(sum);
            // The longest-ringing partial cleans up the whole note.
            OscillatorNode? longest = null;
            double longestDecay = 0;
            foreach (var partialSpec in def.Partials ?? [])
            {
                double ratio = partialSpec[0], amount = partialSpec[1], decay = partialSpec[2];
                var partial = frequency * ratio;
                if (partial >= _nyquist)
                    continue;
                var osc = _g.Oscillator();
                var gain = _g.Gain();
                _g.Value(osc.Frequency, partial);
                _g.SetValueAtTime(gain.Gain, 0.0001, when);
                _g.LinearRamp(gain.Gain, amount, when + 0.004);
                _g.SetTarget(gain.Gain, 0, when + 0.004, decay / 4.5);
                _g.Connect(osc, gain);
                _g.Connect(gain, sum);
                _g.Start(osc, when);
                _g.Stop(osc, when + decay + 0.1);
                nodes.Add(osc);
                nodes.Add(gain);
                if (decay > longestDecay)
                {
                    longest = osc;
                    longestDecay = decay;
                }
            }
            if (longest != null)
                _g.Cleanup(longest, nodes);
            return;
        }
        if (def.Kind == "glass")
        {
            GainNode sum = _g.Gain(), trem = _g.Gain();
            var lfo = _g.Oscillator();
            var depth = _g.Gain();
            _g.Value(sum.Gain, 0);
            var stop = Envelope(sum.Gain, when, length, peak, def);
            _g.Value(trem.Gain, 1);
            _g.Value(lfo.Frequency, def.Tremolo ?? double.NaN);
            _g.Value(depth.Gain, 0.25);
            _g.Connect(lfo, depth);
            _g.Connect(depth, trem.Gain);
            _g.Connect(sum, trem);
            _g.Connect(trem, pan2);
            var oscillators = new List<OscillatorNode>();
            foreach (var (ratio, amount) in new[] { (1.0, 1.0), (2.0, 0.12), (3.0, 0.05) })
            {
                var osc = _g.Oscillator();
                var gain = _g.Gain();
                _g.Value(osc.Frequency, frequency * ratio);
                _g.Value(gain.Gain, amount);
                _g.Connect(osc, gain);
                _g.Connect(gain, sum);
                _g.Start(osc, when);
                _g.Stop(osc, stop);
                nodes.Add(osc);
                nodes.Add(gain);
                oscillators.Add(osc);
            }
            _g.Start(lfo, when);
            _g.Stop(lfo, stop);
            nodes.AddRange([sum, trem, lfo, depth]);
            _g.Cleanup(oscillators[0], nodes);
            return;
        }
        if (def.Kind == "sub")
        {
            var osc = _g.Oscillator();
            var gain = _g.Gain();
            _g.Value(osc.Frequency, frequency);
            var stop = Envelope(gain.Gain, when, length, peak, def);
            _g.Connect(osc, gain);
            _g.Connect(gain, pan2);
            _g.Start(osc, when);
            _g.Stop(osc, stop);
            nodes.Add(osc);
            nodes.Add(gain);
            _g.Cleanup(osc, nodes);
            return;
        }

        // Sustained voices share an envelope after their own tone shaping.
        var amp = _g.Gain();
        _g.Value(amp.Gain, 0);
        var end = Envelope(amp.Gain, when, length, peak, def);
        _g.Connect(amp, pan2);
        nodes.Add(amp);
        var oscillatorList = new List<OscillatorNode>();
        var detunes = new List<AudioParam>();
        if (def.Kind is "bowed" or "brass")
        {
            var filter = _g.Biquad();
            _g.Type(filter, BiquadFilterType.Lowpass);
            _g.Value(filter.Q, def.Kind == "brass" ? 1.1 : 0.6);
            if (def.Kind == "brass")
            {
                double low = def.Low ?? double.NaN;
                var high = ClampHz((def.High ?? double.NaN) * keyTrack * (0.55 + 0.6 * velocity));
                _g.SetValueAtTime(filter.Frequency, low, when);
                _g.ExponentialRamp(filter.Frequency, high, when + 0.06 + (def.Attack ?? double.NaN) * 0.6);
                _g.ExponentialRamp(filter.Frequency, Math.Max(low * 1.2, high * 0.7), when + 0.5);
            }
            else
                _g.Value(filter.Frequency, ClampHz((def.Cutoff ?? double.NaN) * keyTrack * (0.7 + 0.45 * velocity)));
            AudioNode head = filter;
            if (def.Peak is { } peakSpec)
            {
                var peakNode = _g.Biquad();
                _g.Type(peakNode, BiquadFilterType.Peaking);
                _g.Value(peakNode.Frequency, peakSpec[0]);
                _g.Value(peakNode.Gain, peakSpec[1]);
                _g.Value(peakNode.Q, 1.3);
                _g.Connect(head, peakNode);
                head = peakNode;
                nodes.Add(peakNode);
            }
            if (def.Tremolo is { } tremRate and not 0)
            {
                var trem = _g.Gain();
                var lfo = _g.Oscillator();
                var depth = _g.Gain();
                _g.Value(trem.Gain, 0.7);
                _g.Value(lfo.Frequency, tremRate * (0.95 + _random.Next() * 0.1));
                _g.Value(depth.Gain, 0.3);
                _g.Connect(lfo, depth);
                _g.Connect(depth, trem.Gain);
                _g.Connect(head, trem);
                head = trem;
                _g.Start(lfo, when);
                _g.Stop(lfo, end);
                nodes.AddRange([trem, lfo, depth]);
            }
            _g.Connect(head, amp);
            nodes.Add(filter);
            foreach (var cents in def.Voices ?? [])
            {
                var osc = _g.Oscillator();
                _g.Type(osc, OscillatorType.Sawtooth);
                _g.Value(osc.Frequency, frequency);
                var detune = cents + (_random.Next() - 0.5) * 4;
                if (def.Kind == "brass")
                {
                    _g.SetValueAtTime(osc.Detune, detune - 28, when);
                    _g.LinearRamp(osc.Detune, detune, when + 0.07);
                }
                else
                    _g.Value(osc.Detune, detune);
                _g.Connect(osc, filter);
                oscillatorList.Add(osc);
                detunes.Add(osc.Detune);
            }
        }
        else if (def.Kind == "choir")
        {
            GainNode mix = _g.Gain(), sum = _g.Gain();
            var air = _g.Biquad();
            _g.Value(mix.Gain, 1);
            _g.Value(sum.Gain, 1);
            _g.Type(air, BiquadFilterType.Lowpass);
            _g.Value(air.Frequency, 4200);
            var vowel = def.Vowel ?? [];
            double[] qs = [5, 7, 9], levels = [1, 0.6, 0.25];
            for (var index = 0; index < vowel.Length; index++)
            {
                var band = _g.Biquad();
                var level = _g.Gain();
                _g.Type(band, BiquadFilterType.Bandpass);
                _g.Value(band.Frequency, vowel[index]);
                _g.Value(band.Q, index < 3 ? qs[index] : double.NaN);
                _g.Value(level.Gain, index < 3 ? levels[index] : double.NaN);
                _g.Connect(mix, band);
                _g.Connect(band, level);
                _g.Connect(level, sum);
                nodes.Add(band);
                nodes.Add(level);
            }
            _g.Connect(sum, air);
            _g.Connect(air, amp);
            nodes.AddRange([mix, sum, air]);
            foreach (var cents in def.Voices ?? [])
            {
                var osc = _g.Oscillator();
                _g.Type(osc, OscillatorType.Sawtooth);
                _g.Value(osc.Frequency, frequency);
                _g.Value(osc.Detune, cents + (_random.Next() - 0.5) * 5);
                _g.Connect(osc, mix);
                oscillatorList.Add(osc);
                detunes.Add(osc.Detune);
            }
        }
        else if (def.Kind == "organ")
        {
            var filter = _g.Biquad();
            _g.Type(filter, BiquadFilterType.Lowpass);
            _g.Value(filter.Frequency, ClampHz(5200));
            _g.Connect(filter, amp);
            nodes.Add(filter);
            foreach (var cents in new double[] { 0, 3 })
            {
                var osc = _g.Oscillator();
                _g.SetWave(osc, Periodic(def.Harmonics ?? []));
                _g.Value(osc.Frequency, frequency);
                _g.Value(osc.Detune, cents);
                _g.Connect(osc, filter);
                oscillatorList.Add(osc);
                detunes.Add(osc.Detune);
            }
        }
        else if (def.Kind == "wind")
        {
            var filter = _g.Biquad();
            _g.Type(filter, BiquadFilterType.Lowpass);
            _g.Value(filter.Frequency, ClampHz((def.Cutoff is { } cutoff and not 0 ? cutoff : 5200) * keyTrack));
            _g.Connect(filter, amp);
            nodes.Add(filter);
            var osc = _g.Oscillator();
            _g.SetWave(osc, Periodic(def.Harmonics ?? []));
            _g.Value(osc.Frequency, frequency);
            _g.Connect(osc, filter);
            oscillatorList.Add(osc);
            detunes.Add(osc.Detune);
            if (def.Breath is { } breathLevel and not 0)
            {
                var breath = _g.BufferSource();
                var band = _g.Biquad();
                var level = _g.Gain();
                _g.SetBuffer(breath, _kit.Noise);
                _g.Loop(breath);
                _g.Type(band, BiquadFilterType.Bandpass);
                _g.Value(band.Frequency, ClampHz(frequency * 1.02));
                _g.Value(band.Q, 9);
                _g.Value(level.Gain, breathLevel * 3);
                _g.Connect(breath, band);
                _g.Connect(band, level);
                _g.Connect(level, filter);
                _g.Start(breath, when, _random.Next() * 1.5);
                _g.Stop(breath, end);
                // A short chiff of air at the start of the note.
                var chiff = _g.BufferSource();
                var chiffBand = _g.Biquad();
                var chiffGain = _g.Gain();
                _g.SetBuffer(chiff, _kit.Noise);
                _g.Type(chiffBand, BiquadFilterType.Highpass);
                _g.Value(chiffBand.Frequency, 1800);
                _g.SetValueAtTime(chiffGain.Gain, breathLevel * 0.6, when);
                _g.SetTarget(chiffGain.Gain, 0, when + 0.01, 0.02);
                _g.Connect(chiff, chiffBand);
                _g.Connect(chiffBand, chiffGain);
                _g.Connect(chiffGain, amp);
                _g.Start(chiff, when, _random.Next());
                _g.Stop(chiff, when + 0.12);
                nodes.AddRange([breath, band, level, chiff, chiffBand, chiffGain]);
            }
        }
        Vibrato(def, when, end, detunes, nodes);
        foreach (var osc in oscillatorList)
        {
            _g.Start(osc, when);
            _g.Stop(osc, end);
        }
        nodes.AddRange(oscillatorList);
        if (oscillatorList.Count == 0)
            throw new InvalidOperationException($"Instrument {name} of kind {def.Kind} makes no sound");
        _g.Cleanup(oscillatorList[0], nodes);
    }

    // ---------------- Percussion ----------------

    void Hit(string? name, double when, double velocity, double? midi)
    {
        if (name == null || !P.TryGetValue(name, out var def))
            throw new InvalidOperationException("Unknown percussion " + (name ?? "undefined"));
        var output = Channel("perc:" + name, def.Send ?? 0.5);
        var gain = _g.Gain();
        _g.Value(gain.Gain, def.Level * velocity);
        _g.Connect(gain, output);
        var nodes = new List<AudioNode> { gain };
        AudioScheduledSourceNode? first = null;

        void NoiseBurst(BiquadFilterType type, double frequency, double q, double length, double level, double attack = 0.002)
        {
            var source = _g.BufferSource();
            var filter = _g.Biquad();
            var env = _g.Gain();
            _g.SetBuffer(source, _kit.Noise);
            _g.Type(filter, type);
            _g.Value(filter.Frequency, ClampHz(frequency));
            _g.Value(filter.Q, q);
            _g.SetValueAtTime(env.Gain, 0.0001, when);
            _g.LinearRamp(env.Gain, level, when + attack);
            _g.SetTarget(env.Gain, 0, when + attack, length / 4);
            _g.Connect(source, filter);
            _g.Connect(filter, env);
            _g.Connect(env, gain);
            _g.Start(source, when, _random.Next() * 1.5);
            _g.Stop(source, when + length + 0.05);
            nodes.AddRange([source, filter, env]);
            first ??= source;
        }

        void Sweep(double from, double to, double length, double level, OscillatorType type = OscillatorType.Sine)
        {
            var osc = _g.Oscillator();
            var env = _g.Gain();
            _g.Type(osc, type);
            _g.SetValueAtTime(osc.Frequency, from, when);
            _g.ExponentialRamp(osc.Frequency, to, when + length * 0.6);
            _g.SetValueAtTime(env.Gain, 0.0001, when);
            _g.LinearRamp(env.Gain, level, when + 0.004);
            _g.SetTarget(env.Gain, 0, when + 0.004, length / 4);
            _g.Connect(osc, env);
            _g.Connect(env, gain);
            _g.Start(osc, when);
            _g.Stop(osc, when + length + 0.05);
            nodes.AddRange([osc, env]);
            first ??= osc;
        }

        if (def.Body is { } body)
        {
            Sweep(body[0], body[1], body[2], 1);
            if (def.Noise is { } noise)
                NoiseBurst(BiquadFilterType.Lowpass, noise[0], 0.7, noise[1], 0.6);
            if (def.Click is { } click and not 0)
                NoiseBurst(BiquadFilterType.Highpass, 2400, 0.7, 0.012, click);
        }
        if (def.High is { } high)
            NoiseBurst(name is "snare" or "muffled" ? BiquadFilterType.Bandpass : BiquadFilterType.Highpass, high[0], high[1], high[2], 1,
                def.Attack is { } attack and not 0 ? attack : 0.002);
        if (def.Tone is { } tone)
            Sweep(tone[0], tone[0] * 0.85, tone[1], 0.6, OscillatorType.Triangle);
        if (def.Log)
        {
            var pitch = Hz(midi ?? 57);
            Sweep(pitch, pitch * 0.97, 0.45, 1);
            Sweep(pitch * 2.7, pitch * 2.6, 0.12, 0.25);
        }
        if (def.Timpani)
        {
            var pitch = Hz(midi ?? 41);
            foreach (var (ratio, level, length) in new[] { (1.0, 1.0, 1.8), (1.5, 0.45, 1.0), (1.98, 0.25, 0.6), (2.44, 0.12, 0.4) })
                Sweep(pitch * ratio * 1.02, pitch * ratio, length, level);
            NoiseBurst(BiquadFilterType.Lowpass, 320, 0.7, 0.12, 0.5);
        }
        if (def.Metal is { } metal)
        {
            var baseHz = midi is { } m && m != 0 && !double.IsNaN(m) ? Hz(m) : def.Base ?? double.NaN;
            foreach (var partialSpec in metal)
            {
                double ratio = partialSpec[0], level = partialSpec[1], length = partialSpec[2];
                var osc = _g.Oscillator();
                var env = _g.Gain();
                _g.Value(osc.Frequency, Math.Min(_nyquist, baseHz * ratio * (1 + (_random.Next() - 0.5) * 0.004)));
                _g.SetValueAtTime(env.Gain, 0.0001, when);
                _g.LinearRamp(env.Gain, level, when + (name == "gong" ? 0.06 : 0.003));
                _g.SetTarget(env.Gain, 0, when + 0.06, length / 4.5);
                _g.Connect(osc, env);
                _g.Connect(env, gain);
                _g.Start(osc, when);
                _g.Stop(osc, when + length + 0.1);
                nodes.AddRange([osc, env]);
                first ??= osc;
            }
            if (def.Noise is { } noise)
                NoiseBurst(BiquadFilterType.Bandpass, noise[0], 0.8, noise[1], 0.35, 0.05);
        }
        if (def.Swell)
        {
            var length = midi is { } m && m != 0 && !double.IsNaN(m) ? m : 1.5;
            var source = _g.BufferSource();
            var filter = _g.Biquad();
            var env = _g.Gain();
            _g.SetBuffer(source, _kit.Noise);
            _g.Loop(source);
            _g.Type(filter, BiquadFilterType.Highpass);
            _g.Value(filter.Frequency, 2600);
            _g.SetValueAtTime(env.Gain, 0.0001, when);
            _g.ExponentialRamp(env.Gain, 1, when + length);
            _g.SetTarget(env.Gain, 0, when + length, 0.18);
            _g.Connect(source, filter);
            _g.Connect(filter, env);
            _g.Connect(env, gain);
            _g.Start(source, when);
            _g.Stop(source, when + length + 1);
            nodes.AddRange([source, filter, env]);
            first = source;
        }
        if (def.Thunder)
        {
            for (var k = 0; k < 5; k++)
            {
                var at = when + _random.Next() * 0.9;
                var source = _g.BufferSource();
                var filter = _g.Biquad();
                var env = _g.Gain();
                _g.SetBuffer(source, _kit.Noise);
                _g.Loop(source);
                _g.Type(filter, BiquadFilterType.Lowpass);
                _g.Value(filter.Frequency, 140 + _random.Next() * 160);
                _g.SetValueAtTime(env.Gain, 0.0001, at);
                var peakLevel = 0.5 + _random.Next() * 0.5;
                _g.LinearRamp(env.Gain, peakLevel, at + 0.08 + _random.Next() * 0.3);
                _g.SetTarget(env.Gain, 0, at + 0.4, 0.6 + _random.Next() * 0.6);
                _g.Connect(source, filter);
                _g.Connect(filter, env);
                _g.Connect(env, gain);
                _g.Start(source, at, _random.Next());
                _g.Stop(source, at + 4);
                nodes.AddRange([source, filter, env]);
                first ??= source;
            }
        }
        if (first != null)
            _g.Cleanup(first, nodes);
    }

    // Continuous beds of wind, rain, surf, embers and leaves for one section.
    static readonly Dictionary<string, (BiquadFilterType Type, double Frequency, double Q, double Rate, double Depth)> Textures = new()
    {
        ["wind"] = (BiquadFilterType.Bandpass, 520, 0.8, 0.07, 0.6), ["rain"] = (BiquadFilterType.Highpass, 3200, 0.5, 0.31, 0.12),
        ["surf"] = (BiquadFilterType.Lowpass, 760, 0.6, 0.085, 0.85), ["embers"] = (BiquadFilterType.Bandpass, 2400, 2, 1.7, 0.7),
        ["leaves"] = (BiquadFilterType.Bandpass, 3600, 1.2, 0.23, 0.55), ["drips"] = (BiquadFilterType.Bandpass, 1500, 6, 0.6, 0.9),
    };

    void Texture(string? kind, double when, double length, double level)
    {
        var output = Channel("tex:" + (kind ?? "undefined"), 0.4);
        var source = _g.BufferSource();
        var filter = _g.Biquad();
        var gain = _g.Gain();
        var lfo = _g.Oscillator();
        var depth = _g.Gain();
        _g.SetBuffer(source, _kit.Noise);
        _g.Loop(source);
        if (kind == null || !Textures.TryGetValue(kind, out var settings))
            throw new InvalidOperationException("Unknown texture " + (kind ?? "undefined"));
        _g.Type(filter, settings.Type);
        _g.Value(filter.Frequency, settings.Frequency);
        _g.Value(filter.Q, settings.Q);
        _g.Value(lfo.Frequency, settings.Rate);
        _g.Value(depth.Gain, level * settings.Depth);
        _g.SetValueAtTime(gain.Gain, 0.0001, when);
        _g.LinearRamp(gain.Gain, level, when + Math.Min(2, length * 0.3));
        _g.SetValueAtTime(gain.Gain, level, when + length - Math.Min(1.5, length * 0.25));
        _g.LinearRamp(gain.Gain, 0.0001, when + length);
        _g.Connect(lfo, depth);
        _g.Connect(depth, gain.Gain);
        if (kind == "wind")
        {
            var sweepDepth = _g.Gain();
            _g.Value(sweepDepth.Gain, 260);
            _g.Connect(lfo, sweepDepth);
            _g.Connect(sweepDepth, filter.Frequency);
        }
        _g.Connect(source, filter);
        _g.Connect(filter, gain);
        _g.Connect(gain, output);
        _g.Start(source, when, _random.Next() * 1.5);
        _g.Stop(source, when + length + 0.05);
        _g.Start(lfo, when);
        _g.Stop(lfo, when + length + 0.05);
        _g.Cleanup(source, [source, filter, gain, lfo, depth]);
    }

    // ---------------- Sequencing ----------------

    double? ChordPitch(ChordEntry entry, string symbol, double octave)
    {
        var chord = entry.Chord ?? throw new InvalidOperationException($"{_compiled.Id}: a step plays a held chord with no chord before it");
        var baseNote = _compiled.Tonic + chord.Root + octave * 12;
        var tones = chord.Tones;
        return symbol switch
        {
            "1" => baseNote,
            "3" => baseNote + tones[1],
            "5" => baseNote + tones[2],
            "7" => baseNote + (tones.Length > 3 ? tones[3] : 12),
            "8" => baseNote + 12,
            "T" => baseNote + tones[1] + 12,
            "F" => baseNote + tones[2] + 12,
            "L" => baseNote - 12,
            "P" => baseNote + tones[2] - 12,
            _ => null,
        };
    }

    /// <summary>Pads move each voice to the nearest tone of the next chord.</summary>
    double[] VoiceLead(CompiledLayer key, ChordEntry entry, double octave, int count)
    {
        var chord = entry.Chord!;
        var center = _compiled.Tonic + octave * 12 + 4;
        var classes = chord.Tones.Take(count).Select(t => (chord.Root + t) % 12).ToList();
        while (classes.Count < count)
            classes.Add(classes[classes.Count % chord.Tones.Length]);
        var previous = _voicings.GetValueOrDefault(key);
        double[]? best = null;
        var bestCost = double.PositiveInfinity;
        for (var rotation = 0; rotation < classes.Count; rotation++)
        {
            var order = classes.Skip(rotation).Concat(classes.Take(rotation));
            var notes = new List<double>();
            var last = center - 9;
            foreach (var pc in order)
            {
                var pitch = _compiled.Tonic - _compiled.Tonic % 12 + octave * 12 + pc - 12;
                while (pitch <= last)
                    pitch += 12;
                notes.Add(pitch);
                last = pitch;
            }
            var shift = JsMath.Round((center - (notes[0] + notes[^1]) / 2) / 12) * 12;
            var placed = notes.Select(n => n + shift).ToArray();
            double cost;
            if (previous != null)
            {
                cost = 0;
                for (var i = 0; i < placed.Length; i++)
                    cost += Math.Abs(placed[i] - (i < previous.Length ? previous[i] : placed[i]));
            }
            else
                cost = Math.Abs((placed[0] + placed[^1]) / 2 - center);
            if (cost < bestCost)
            {
                bestCost = cost;
                best = placed;
            }
        }
        _voicings[key] = best!;
        return best!;
    }

    double Position(double global) =>
        global < _compiled.TotalSteps ? global : _compiled.LoopStep + (global - _compiled.LoopStep) % (_compiled.TotalSteps - _compiled.LoopStep);

    /// <summary>Plays step <paramref name="global"/> of the form (looping after its end) at <paramref name="when"/>.</summary>
    public void Step(double global, double when)
    {
        var at = Position(global);
        var index = _compiled.Timeline.Count - 1;
        while (index > 0 && _compiled.Timeline[index].Start > at)
            index--;
        var (section, start) = _compiled.Timeline[index];
        var local = at - start;
        var stepSeconds = _compiled.StepSeconds;
        var time = when + (_compiled.Swing != 0 && local % 2 == 1 ? _compiled.Swing * stepSeconds : 0);
        var entry = local >= 0 && local < section.ChordAt.Length ? section.ChordAt[(int)local] : null;
        var barStep = local % _compiled.Steps;
        var bar = Math.Floor(local / _compiled.Steps);
        var dyn = section.Dynamics + (section.DynamicsTo - section.Dynamics) * (local / Math.Max(1, section.Steps - 1));
        double Human(double spread = 0.009) => time + (_random.Next() - 0.5) * spread;
        double Vel(double v, bool accent = false) => v * dyn * (0.93 + _random.Next() * 0.14) * (accent ? 1.22 : 1);

        foreach (var layer in section.Layers)
        {
            var spec = layer.Spec;
            if (_only != null && !_only.Contains(spec.Role is { Length: > 0 } role ? role : spec.Type))
                continue;
            if (spec.Bars != null && !spec.Bars.Contains((int)(bar % (spec.Cycle is { } cycle and not 0 ? cycle : section.Bars))))
                continue;
            var v = spec.Velocity ?? 0.7;
            var octave = spec.Octave ?? 0;
            switch (spec.Type)
            {
                case "pad":
                {
                    if (entry == null || entry.Start != local)
                        break;
                    var notes = VoiceLead(layer, entry, octave, spec.Voices is { } n and not 0 ? n : 3);
                    var length = entry.Length * stepSeconds * (spec.Legato ?? 1.04);
                    for (var i = 0; i < notes.Length; i++)
                    {
                        var noteTime = Human(0.02);
                        var velocity = Vel(v) * (i == 0 ? 1 : 0.85);
                        var pan = (spec.Pan ?? I[spec.Instrument!].Pan ?? double.NaN) + (i - (notes.Length - 1) / 2.0) * (spec.Spread ?? 0.35);
                        Play(spec.Instrument, notes[i], noteTime, length, velocity, pan);
                    }
                    break;
                }
                case "drone":
                {
                    var every = spec.Every is { } e and not 0 ? e : section.Steps;
                    if (local % every != 0)
                        break;
                    var length = every * stepSeconds * 1.05;
                    foreach (var d in spec.Degrees ?? ["1", "5"])
                        Play(spec.Instrument, _compiled.Tonic + Score.Degree(d, _compiled.Scale) + octave * 12, time, length, Vel(v), spec.Pan);
                    break;
                }
                case "arp" or "bass" or "pulse":
                {
                    if (entry == null)
                        break;
                    var pattern = spec.Pattern ?? throw new InvalidOperationException($"{_compiled.Id}: a {spec.Type} layer without a pattern");
                    var symbol = pattern[(int)(local % pattern.Length)];
                    if (symbol is '.' or '-')
                        break;
                    var length = 1;
                    while (pattern[(int)((local + length) % pattern.Length)] == '-' && length < pattern.Length)
                        length++;
                    if (ChordPitch(entry, symbol.ToString(), octave) is not { } pitch)
                        break;
                    var downbeat = barStep % _compiled.Sub == 0 ? 1 : 0.86;
                    var noteTime = Human(0.006);
                    Play(spec.Instrument, pitch, noteTime, length * stepSeconds * (spec.Legato ?? 0.95), Vel(v * downbeat), spec.Pan);
                    if (spec.Doublings != null)
                        // layer.dbl.i of an array is undefined in the old script.
                        throw new InvalidOperationException("Unknown instrument undefined");
                    break;
                }
                case "strum":
                {
                    if (entry == null)
                        break;
                    var pattern = spec.Pattern!;
                    var symbol = pattern[(int)(local % pattern.Length)];
                    if (symbol is not ('d' or 'u' or 'D'))
                        break;
                    var notes = new[] { ChordPitch(entry, "1", octave), ChordPitch(entry, "5", octave), ChordPitch(entry, "8", octave), ChordPitch(entry, "T", octave) };
                    if (symbol == 'u')
                        Array.Reverse(notes);
                    for (var i = 0; i < notes.Length; i++)
                        Play(spec.Instrument, notes[i]!.Value, time + i * 0.018, stepSeconds * 2, Vel(v * (symbol == 'D' ? 1.15 : symbol == 'u' ? 0.7 : 0.9)), spec.Pan);
                    break;
                }
                case "mel" or "line":
                {
                    var span = spec.Type == "line" ? layer.Span is { } s and not 0 ? s : section.Steps : section.Steps;
                    if (!layer.ByStep.TryGetValue(spec.Type == "line" ? local % span : local, out var events))
                        break;
                    foreach (var e in events)
                    {
                        var pitch = _compiled.Tonic + e.Pitch + octave * 12;
                        var length = e.Length * stepSeconds * (spec.Legato ?? 0.98);
                        var noteTime = Human(0.01);
                        Play(spec.Instrument, pitch, noteTime, length, Vel(v, e.Accent), spec.Pan);
                        foreach (var extra in spec.Doublings ?? [])
                        {
                            var extraPitch = pitch + (extra.Octave is { } o and not 0 ? o : 0) * 12 + (extra.Shift is { } sh and not 0 ? sh : 0);
                            var extraTime = Human(0.012);
                            Play(extra.Instrument, extraPitch, extraTime, length, Vel(extra.Velocity ?? v * 0.5, e.Accent), extra.Pan);
                        }
                        if (spec.Harmony is { } harm)
                        {
                            var steps = harm.Steps ?? -2;
                            var degreeIndex = Array.FindIndex(_compiled.Scale, sc => (e.Pitch % 12 + 12) % 12 == sc);
                            if (degreeIndex >= 0)
                            {
                                var target = degreeIndex + steps;
                                var octaves = Math.Floor(target / 7);
                                var harmony = e.Pitch - _compiled.Scale[degreeIndex] + _compiled.Scale[(int)((target % 7 + 7) % 7)] + octaves * 12;
                                var harmonyPitch = _compiled.Tonic + harmony + (harm.Octave ?? octave) * 12;
                                var harmonyTime = Human(0.012);
                                Play(harm.Instrument, harmonyPitch, harmonyTime, length, Vel(harm.Velocity ?? v * 0.5, e.Accent), harm.Pan);
                            }
                        }
                    }
                    break;
                }
                case "drums":
                {
                    foreach (var (name, pattern) in spec.Drums ?? [])
                    {
                        if (pattern.Length == 0)
                            continue;
                        var symbol = pattern[(int)(local % pattern.Length)];
                        if (symbol == '.')
                            continue;
                        var strength = symbol == 'X' ? 1.25 : symbol == 'o' ? 0.45 : 1;
                        double? tuning = entry == null ? null
                            : name == "timp" ? ChordPitch(entry, "1", -2 + (spec.TimpaniOctave is { } to and not 0 ? to : 0))
                            : name == "log" ? ChordPitch(entry, symbol == '5' ? "5" : "1", spec.LogOctave ?? -1)
                            : null;
                        if (symbol == 'r')
                        {
                            for (var k = 0; k < 4; k++)
                                Hit(name, time + k * stepSeconds / 4, v * (0.35 + k * 0.08), tuning);
                        }
                        else if (symbol == 'f')
                        {
                            Hit(name, time - 0.025, v * 0.45, tuning);
                            Hit(name, time, v, tuning);
                        }
                        else
                        {
                            var hitTime = Human(0.004);
                            Hit(name, hitTime, Vel(v * strength), tuning);
                        }
                    }
                    break;
                }
                case "hit":
                {
                    if (barStep != (spec.At is { } a and not 0 ? a : 0))
                        break;
                    double? tuning = spec.Degree is { Length: > 0 } deg ? _compiled.Tonic + Score.Degree(deg, _compiled.Scale) + octave * 12
                        : spec.Instrument == "timp" ? ChordPitch(entry ?? throw new InvalidOperationException($"{_compiled.Id}: a timpani hit without a chord"), "1", -2)
                        : null;
                    var velocity = Vel(v);
                    Hit(spec.Instrument, time, velocity, spec.Instrument == "swell" ? (spec.Length is { } len and not 0 ? len : _compiled.Steps) * stepSeconds : tuning);
                    break;
                }
                case "tex":
                {
                    if (local != 0)
                        break;
                    Texture(spec.Kind, time, section.Steps * stepSeconds + 0.6, v * 0.06);
                    break;
                }
                case "drips":
                {
                    if (_random.Next() > (spec.Chance ?? 0.08))
                        break;
                    var pitch = _compiled.Tonic + 24 + _compiled.Scale[(int)Math.Floor(_random.Next() * 7)] + octave * 12;
                    var pan = (_random.Next() - 0.5) * 1.4;
                    Play(spec.Instrument is { Length: > 0 } instrument ? instrument : "kalimba", pitch, time, 0.4, v * 0.5, pan);
                    break;
                }
                default:
                    throw new InvalidOperationException("Unknown layer " + spec.Type);
            }
        }
    }
}
