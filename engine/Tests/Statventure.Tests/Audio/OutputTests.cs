using System.Diagnostics;
using Brutal.OpenAlApi;
using Statventure.Audio;

namespace Statventure.Tests.Audio;

/// <summary>
/// Step 7, the output (Docs/Audio.md): OpenAL Soft passes the samples through untouched (a loopback
/// device mixes into memory, no hardware needed), the audio thread renders exactly what the graph
/// renders without it, the game's calls land at the next quantum, pausing freezes the graph, and
/// without a device the graph keeps the real-time pace.
/// </summary>
public class OutputTests
{
    /// <summary>Takes every buffer the audio thread writes, as fast as it is given them, up to a count.</summary>
    sealed class RecordingOutput(int sampleRate, int buffers) : IAudioOutput
    {
        readonly List<float[]> _written = [];
        public int SampleRate { get; } = sampleRate;
        public int BufferFrames { get; } = OpenAlOutput.FramesPerBuffer(sampleRate);
        public bool Paused { get; private set; }
        /// <summary>Takes nothing while held (so calls can be queued before the first quantum).</summary>
        public volatile bool Held;
        public ManualResetEventSlim Full { get; } = new();
        public int Writable
        {
            get
            {
                lock (_written)
                    return Held || Paused || _written.Count >= buffers ? 0 : 1;
            }
        }
        public void Write(ReadOnlySpan<float> interleaved)
        {
            lock (_written)
            {
                _written.Add(interleaved.ToArray());
                if (_written.Count >= buffers)
                    Full.Set();
            }
        }
        public float[] Samples()
        {
            lock (_written)
                return [.. _written.SelectMany(b => b)];
        }
        public int Count
        {
            get
            {
                lock (_written)
                    return _written.Count;
            }
        }
        public void Maintain() { }
        public void Pause() => Paused = true;
        public void Resume() => Paused = false;
        public void Wait(int milliseconds) => Thread.Sleep(1);
        public bool HasDevice => true;
        public void Dispose() { }
    }

    [Theory]
    [InlineData(48000)]
    [InlineData(44100)]
    public void OpenAlPassesTheSamplesThroughUntouched(int rate)
    {
        using var loopback = LoopbackDevice.Open(null!);
        using var output = new OpenAlOutput(loopback, rate);
        var random = AudioFixtureRun.Mulberry32(7);
        var frames = output.BufferFrames * OpenAlOutput.BufferCount;
        var sent = new float[frames * 2];
        // Peaks past ±1 too: the mix may exceed it (step 7: no limiter), and OpenAL's limiter, were it on, would change them.
        for (var i = 0; i < sent.Length; i++)
            sent[i] = (float)((random() * 2 - 1) * 1.5);
        Assert.Equal(OpenAlOutput.BufferCount, output.Writable);
        for (var b = 0; b < OpenAlOutput.BufferCount; b++)
            output.Write(sent.AsSpan(b * output.BufferFrames * 2, output.BufferFrames * 2));
        output.Maintain();
        var mixed = new float[frames * 2];
        loopback.Render(mixed, frames);
        var differing = Enumerable.Range(0, sent.Length).Count(i => BitConverter.SingleToUInt32Bits(sent[i]) != BitConverter.SingleToUInt32Bits(mixed[i]));
        Assert.True(differing == 0, $"{differing} of {sent.Length} samples changed; first: sent {sent[0]:G9}/{sent[1]:G9}, mixed {mixed[0]:G9}/{mixed[1]:G9}");
    }

    /// <summary>HRTF is really off: direct channels would bypass it for this source anyway, so the sound alone doesn't show it.</summary>
    [Fact]
    public unsafe void OpenAlReportsHrtfOff()
    {
        using var loopback = LoopbackDevice.Open(null!);
        using var output = new OpenAlOutput(loopback, 48000);
        int status;
        Alc.PInvoke.GetIntegerv(loopback.Handle, (int)AlcInteger.HrtfStatus, 1, &status);
        Assert.Equal(AlcHrtfStatus.Disabled, (AlcHrtfStatus)status);
    }

    /// <summary>Output buffers are left then right: a tone panned hard left comes out only in the even samples.</summary>
    [Fact]
    public void BuffersAreLeftThenRight()
    {
        var system = new AudioSystem(new RecordingOutput(48000, 0), TestMusic.Library, startThread: false);
        var tone = new OscillatorNode(system.Graph);
        var panner = new StereoPannerNode(system.Graph);
        panner.Pan.Value = -1;
        tone.Connect(panner);
        panner.Connect(system.EffectsBus);
        tone.Start(0);
        var buffer = new float[OpenAlOutput.FramesPerBuffer(48000) * 2];
        system.RenderBuffer(buffer);
        Assert.Contains(buffer.Where((_, i) => i % 2 == 0), v => v != 0);
        Assert.All(buffer.Where((_, i) => i % 2 == 1), v => Assert.Equal(0, v, 1e-6));
    }

    /// <summary>Pausing the OpenAL source keeps its place: after resuming, the samples go on from where it paused.</summary>
    [Fact]
    public void PausingOpenAlKeepsItsPlace()
    {
        using var loopback = LoopbackDevice.Open(null!);
        using var output = new OpenAlOutput(loopback, 48000);
        var frames = output.BufferFrames;
        var sent = new float[frames * 2 * OpenAlOutput.BufferCount];
        for (var i = 0; i < sent.Length; i++)
            sent[i] = i % 1000 / 1000f + 0.001f;
        for (var b = 0; b < OpenAlOutput.BufferCount; b++)
            output.Write(sent.AsSpan(b * frames * 2, frames * 2));
        output.Maintain();
        var first = new float[frames * 2];
        loopback.Render(first, frames);
        output.Pause();
        var paused = new float[frames * 2];
        loopback.Render(paused, frames);
        output.Resume();
        var second = new float[frames * 2];
        loopback.Render(second, frames);
        Assert.True(first.AsSpan().SequenceEqual(sent.AsSpan(0, frames * 2)));
        // OpenAL Soft declicks: pausing fades the next 64 frames out, resuming plays from the paused
        // place again, fading in over 64 frames; then the samples go on untouched.
        const int Fade = 64;
        for (var f = 0; f < frames; f++)
            for (var c = 0; c < 2; c++)
            {
                var due = sent[(frames + f) * 2 + c];
                Assert.Equal(f < Fade ? due * (Fade - f) / Fade : 0, paused[2 * f + c], 1e-6);
                Assert.Equal(f < Fade ? due * f / Fade : due, second[2 * f + c], f < Fade ? 1e-6 : 0);
            }
    }

    [Theory]
    [InlineData(44100, 256)]
    [InlineData(48000, 256)]
    [InlineData(96000, 512)]
    [InlineData(22050, 128)]
    public void BuffersAreWholeQuantaOfAboutFiveMilliseconds(int rate, int frames) => Assert.Equal(frames, OpenAlOutput.FramesPerBuffer(rate));

    /// <summary>
    /// Rooms prepared ahead and shared (step 7) sound as rooms made on the spot: two themes in the same
    /// room (the hall) cross-fading, then one in the cathedral, through the audio system and through a
    /// plain soundtrack, sample for sample.
    /// </summary>
    [Fact]
    public void SharedRoomsSoundAsRoomsMadeOnTheSpot()
    {
        float[][] Render(bool prepared)
        {
            var graph = new AudioGraph(22050, 2);
            Soundtrack music;
            Action beforeQuantum;
            if (prepared)
            {
                var system = new AudioSystem(graph, TestMusic.Library);
                music = system.Music;
                beforeQuantum = system.BeforeQuantum;
            }
            else
            {
                var master = new GainNode(graph);
                master.Connect(graph.Destination);
                music = new Soundtrack(graph, master, TestMusic.Library);
                beforeQuantum = music.Schedule;
            }
            var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
            var result = new[] { new List<float>(), new List<float>() };
            music.Play("alder-vale");
            for (var q = 0; q < 22050 * 4 / AudioGraph.RenderQuantumFrames; q++)
            {
                if (q == 22050 / AudioGraph.RenderQuantumFrames)
                    music.Play("crimson-divide");
                if (q == 2 * 22050 / AudioGraph.RenderQuantumFrames)
                    music.Play("stormwake-hills");
                beforeQuantum();
                graph.RenderQuantum(bus);
                for (var c = 0; c < 2; c++)
                    result[c].AddRange(bus.Channel(c).Data);
            }
            return [.. result.Select(r => r.ToArray())];
        }
        var shared = Render(true);
        var plain = Render(false);
        Assert.True(shared[0].AsSpan().SequenceEqual(plain[0]) && shared[1].AsSpan().SequenceEqual(plain[1]));
        Assert.Contains(shared[0], v => v != 0);
    }

    /// <summary>The same theme and calls rendered by the audio thread and directly, buffer by buffer.</summary>
    [Fact]
    public void TheAudioThreadRendersWhatTheGraphRenders()
    {
        const int Buffers = 400;
        var output = new RecordingOutput(48000, Buffers) { Held = true };
        using (var system = new AudioSystem(output, TestMusic.Library))
        {
            system.Music.Play("alder-vale");
            system.EffectsVolume = 0.5;
            output.Held = false;
            Assert.True(output.Full.Wait(TimeSpan.FromSeconds(30)));
        }
        var direct = new AudioSystem(new RecordingOutput(48000, 0), TestMusic.Library, startThread: false);
        direct.Music.Play("alder-vale");
        direct.EffectsVolume = 0.5;
        var expected = new float[Buffers * output.BufferFrames * 2];
        for (var b = 0; b < Buffers; b++)
            direct.RenderBuffer(expected.AsSpan(b * output.BufferFrames * 2, output.BufferFrames * 2));
        var actual = output.Samples();
        Assert.Equal(expected.Length, actual.Length);
        var first = Enumerable.Range(0, expected.Length).FirstOrDefault(i => BitConverter.SingleToUInt32Bits(expected[i]) != BitConverter.SingleToUInt32Bits(actual[i]), -1);
        Assert.True(first < 0, $"the thread's buffers differ from the direct render from sample {first} (frame {first / 2}): {(first >= 0 ? $"{expected[first]:G9} vs {actual[first]:G9}" : "")}");
        Assert.Contains(actual, v => v != 0);
    }

    /// <summary>A call made between buffers takes effect at the start of the next quantum the thread renders.</summary>
    [Fact]
    public void CallsLandAtTheNextQuantum()
    {
        var system = new AudioSystem(new RecordingOutput(48000, 0), TestMusic.Library, startThread: false);
        var source = new OscillatorNode(system.Graph);
        source.Connect(system.EffectsBus);
        source.Start(0);
        var buffer = new float[OpenAlOutput.FramesPerBuffer(48000) * 2];
        system.RenderBuffer(buffer);
        var start = system.Graph.CurrentTime;
        system.MasterVolume = 0;
        Assert.Equal(0, system.Graph.CurrentSampleFrame % AudioGraph.RenderQuantumFrames);
        system.RenderBuffer(buffer);
        // The ramp starts at the quantum's first frame (from 1) and is under way by its last.
        var reference = new AudioSystem(new AudioGraph(48000, 2), TestMusic.Library);
        var oscillator = new OscillatorNode(reference.Graph);
        oscillator.Connect(reference.EffectsBus);
        oscillator.Start(0);
        var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
        for (var q = 0; q < buffer.Length / 2 / AudioGraph.RenderQuantumFrames; q++)
            reference.Graph.RenderQuantum(bus);
        reference.MasterVolume = 0;
        reference.BeforeQuantum();
        reference.Graph.RenderQuantum(bus);
        for (var i = 0; i < AudioGraph.RenderQuantumFrames; i++)
            Assert.Equal(bus.Channel(0).Data[i], buffer[2 * i]);
        Assert.Equal(start, reference.Graph.CurrentTime - AudioGraph.RenderQuantumFrames / 48000.0, 9);
    }

    [Fact]
    public void PausingFreezesTheGraphAndResumingGoesOn()
    {
        var output = new RecordingOutput(48000, 100000);
        using var system = new AudioSystem(output, TestMusic.Library);
        system.Music.Play("combat");
        SpinWait.SpinUntil(() => output.Count > 20, TimeSpan.FromSeconds(10));
        system.Pause();
        // The thread pauses after the buffer it is rendering (a long one in Debug when a big convolver stage runs).
        Assert.True(SpinWait.SpinUntil(() => output.Paused, TimeSpan.FromSeconds(10)));
        var frozen = output.Count;
        var frame = system.Graph.CurrentSampleFrame;
        Thread.Sleep(100);
        Assert.Equal(frozen, output.Count);
        Assert.Equal(frame, system.Graph.CurrentSampleFrame);
        system.Resume();
        Assert.True(SpinWait.SpinUntil(() => output.Count > frozen + 20, TimeSpan.FromSeconds(10)));
        Assert.False(output.Paused);
    }

    /// <summary>
    /// Without a device, buffers are taken at the real-time pace, 20 ms ahead. Each time the output
    /// is drained, the frames taken lie between the clock before and after it, plus what's kept ahead,
    /// however late this thread gets to run.
    /// </summary>
    [Fact]
    public void WithoutADeviceTheGraphKeepsTheRealTimePace()
    {
        using var output = new OpenAlOutput(silent: true);
        Assert.False(output.HasDevice);
        var clock = Stopwatch.StartNew();
        var frames = 0L;
        var buffer = new float[output.BufferFrames * 2];
        var ahead = OpenAlOutput.BufferCount * output.BufferFrames;
        while (clock.Elapsed.TotalSeconds < 0.3)
        {
            var before = clock.Elapsed.TotalSeconds * output.SampleRate;
            output.Maintain();
            for (int n; (n = output.Writable) > 0;)
                for (; n > 0; n--)
                {
                    output.Write(buffer);
                    frames += output.BufferFrames;
                }
            var after = clock.Elapsed.TotalSeconds * output.SampleRate;
            // The output's clock started a moment before this one's, so the upper end has a buffer to spare.
            Assert.InRange(frames, before + ahead - output.BufferFrames, after + ahead + 2 * output.BufferFrames);
            Thread.Sleep(1);
        }
    }

    /// <summary>
    /// The real default device (or none, silently): the device's use of the buffers paces the thread, so
    /// the graph moves and never runs far ahead of the clock (under load it may fall behind, which
    /// an underrun is; `audio-benchmark.sh --live` measures how close it stays), also after a reopening.
    /// </summary>
    [Fact]
    public void TheDefaultDevicePacesTheGraph()
    {
        const double Ahead = 0.5;
        var clock = Stopwatch.StartNew();
        using var system = new AudioSystem(TestMusic.Library);
        Assert.True(SpinWait.SpinUntil(() => system.Graph.CurrentTime > 0.1, TimeSpan.FromSeconds(30)));
        Thread.Sleep(600);
        Assert.InRange(system.Graph.CurrentTime, 0.1, clock.Elapsed.TotalSeconds + Ahead);
        if (!system.HasDevice)
            return;
        // The reopening path, on the same device (nothing plays): streaming goes on from the same graph.
        system.OpenAl!.SimulateDefaultDeviceChange();
        Assert.True(SpinWait.SpinUntil(() => system.OpenAl.Reopens == 1, TimeSpan.FromSeconds(30)));
        var reopened = system.Graph.CurrentTime;
        Assert.True(SpinWait.SpinUntil(() => system.Graph.CurrentTime > reopened + 0.1, TimeSpan.FromSeconds(30)));
        Assert.True(system.HasDevice);
        Assert.InRange(system.Graph.CurrentTime, reopened, clock.Elapsed.TotalSeconds + Ahead);
    }
}
