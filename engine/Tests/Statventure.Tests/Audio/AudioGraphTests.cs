using Statventure.Audio;

namespace Statventure.Tests.Audio;

/// <summary>
/// The private graph's rules that Chromium's renders don't show (Docs/Audio.md, step 1): when
/// sources report their end, sources nothing pulls, the errors scripts would get, disconnecting.
/// </summary>
public class AudioGraphTests
{
    static AudioBuffer Ones(AudioGraph graph, int length)
    {
        var buffer = new AudioBuffer(1, length, graph.SampleRateF);
        buffer.GetChannelData(0).AsSpan().Fill(1);
        return buffer;
    }

    [Fact]
    public void ASourceEndsAfterTheQuantumItFinishedIn()
    {
        var graph = new AudioGraph(48000, 1);
        var source = new AudioBufferSourceNode(graph) { Buffer = Ones(graph, 300) };
        source.Connect(graph.Destination);
        long? endedAt = null;
        source.Ended += _ => endedAt = graph.CurrentSampleFrame;
        source.Start(0);
        var bus = new AudioBus(1, AudioGraph.RenderQuantumFrames);
        graph.RenderQuantum(bus);
        graph.RenderQuantum(bus);
        Assert.Null(endedAt);
        // The buffer runs out in the third quantum (frame 300): ended once that quantum is rendered.
        graph.RenderQuantum(bus);
        Assert.Equal(384, endedAt);
        Assert.Equal(1f, bus.Channel(0).Data[300 - 257]);
        Assert.Equal(0f, bus.Channel(0).Data[300 - 256]);
        Assert.True(source.HasFinished);
    }

    [Fact]
    public void ASourceNothingPullsStillFinishes()
    {
        // Blink's pre-render task (HandleStoppableSourceNodes): past its stop time, or past its
        // buffer's end and 256 more frames, a source that isn't connected is finished.
        var graph = new AudioGraph(48000, 1);
        var stopped = new AudioBufferSourceNode(graph) { Buffer = Ones(graph, 128), Loop = true };
        stopped.Start(0);
        stopped.Stop(0.01);
        var played = new AudioBufferSourceNode(graph) { Buffer = Ones(graph, 1000) };
        played.Start(0);
        var looping = new AudioBufferSourceNode(graph) { Buffer = Ones(graph, 128), Loop = true };
        looping.Start(0);
        var ended = new Dictionary<AudioScheduledSourceNode, long>();
        foreach (var source in new[] { stopped, played, looping })
            source.Ended += s => ended[s] = graph.CurrentSampleFrame;
        graph.RenderOffline(48000 / 10);
        // 0.01 s is frame 480: the first quantum starting after it is 512's, ended after it.
        Assert.Equal(640, ended[stopped]);
        // 1000 frames + 256: past frame 1256 at the quantum starting at 1280.
        Assert.Equal(1408, ended[played]);
        Assert.False(ended.ContainsKey(looping));
    }

    [Fact]
    public void ScriptErrorsAreRaised()
    {
        var graph = new AudioGraph(48000);
        var gain = new GainNode(graph);
        Assert.Throws<ArgumentOutOfRangeException>(() => gain.Gain.ExponentialRampToValueAtTime(0, 0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => gain.Gain.SetValueAtTime(1, -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => gain.Gain.SetTargetAtTime(1, 0.1, -1));
        gain.Gain.SetValueCurveAtTime([0, 1], 0.1, 0.1);
        Assert.Throws<InvalidOperationException>(() => gain.Gain.SetValueCurveAtTime([0, 1], 0.15, 0.1));
        Assert.Throws<InvalidOperationException>(() => gain.Gain.LinearRampToValueAtTime(1, 0.15));
        gain.Gain.LinearRampToValueAtTime(1, 0.2); // the curve's end is free
        Assert.Throws<ArgumentException>(() => gain.Gain.SetValueCurveAtTime([1], 0.5, 0.1));
        var source = new AudioBufferSourceNode(graph) { Buffer = Ones(graph, 10) };
        Assert.Throws<InvalidOperationException>(() => source.Buffer = Ones(graph, 10));
        Assert.Throws<InvalidOperationException>(() => source.Stop(1));
        source.Start(0);
        Assert.Throws<InvalidOperationException>(() => source.Start(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new AudioBufferSourceNode(graph).Start(-1, 0));
    }

    [Fact]
    public void DisconnectedNodesAreNotHeard()
    {
        var graph = new AudioGraph(48000, 1);
        var source = new AudioBufferSourceNode(graph) { Buffer = Ones(graph, 128), Loop = true };
        var gain = new GainNode(graph);
        gain.Gain.Value = 0.5f;
        source.Connect(gain).Connect(graph.Destination);
        var lfo = new AudioBufferSourceNode(graph) { Buffer = Ones(graph, 128), Loop = true };
        lfo.Connect(gain.Gain);
        source.Start(0);
        lfo.Start(0);
        var bus = new AudioBus(1, AudioGraph.RenderQuantumFrames);
        graph.RenderQuantum(bus);
        Assert.Equal(1.5f, bus.Channel(0).Data[5]);
        lfo.Disconnect(gain.Gain);
        graph.RenderQuantum(bus);
        Assert.Equal(0.5f, bus.Channel(0).Data[5]);
        gain.Disconnect();
        graph.RenderQuantum(bus);
        Assert.True(bus.IsSilent);
        Assert.Throws<InvalidOperationException>(() => gain.Disconnect(graph.Destination));
    }

    [Fact]
    public void AnOfflineRenderEndsMidQuantum()
    {
        var graph = new AudioGraph(44100, 2);
        var source = new AudioBufferSourceNode(graph) { Buffer = Ones(graph, 128), Loop = true };
        source.Connect(graph.Destination);
        source.Start(0);
        var render = graph.RenderOffline(300);
        Assert.Equal(2, render.Length);
        Assert.All(render, c => Assert.Equal(300, c.Length));
        Assert.All(render, c => Assert.All(c, s => Assert.Equal(1f, s)));
        Assert.Equal(384, graph.CurrentSampleFrame);
    }
}
