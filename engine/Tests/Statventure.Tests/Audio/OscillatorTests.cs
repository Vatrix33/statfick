using Statventure.Audio;

namespace Statventure.Tests.Audio;

/// <summary>
/// Oscillators and the FFT beyond what Chromium's renders show (Docs/Audio.md, step 2): stopping
/// oscillators nothing pulls, the errors scripts would get, and the forward real FFT (the
/// convolver's, compared with Chromium from step 4) against a direct DFT.
/// </summary>
public class OscillatorTests
{
    [Fact]
    public void AnOscillatorNothingPullsFinishesAfterItsStop()
    {
        // OscillatorHandler::HandleStoppableSourceNode: 256 frames past the stop time.
        var graph = new AudioGraph(48000, 1);
        var osc = new OscillatorNode(graph);
        long? endedAt = null;
        osc.Ended += _ => endedAt = graph.CurrentSampleFrame;
        osc.Start(0);
        osc.Stop(0.01);
        graph.RenderOffline(4800);
        // 480 + 256 = 736: the first quantum starting at or after it is 768's.
        Assert.Equal(896, endedAt);
    }

    [Fact]
    public void ScriptErrorsAreRaised()
    {
        var graph = new AudioGraph(48000);
        Assert.Throws<ArgumentException>(() => new OscillatorNode(graph, OscillatorType.Custom));
        var osc = new OscillatorNode(graph);
        Assert.Throws<InvalidOperationException>(() => osc.Type = OscillatorType.Custom);
        Assert.Throws<ArgumentException>(() => graph.CreatePeriodicWave([0, 1], [0, 1, 2]));
        Assert.Throws<ArgumentException>(() => graph.CreatePeriodicWave([0], [0]));
        var other = new AudioGraph(44100);
        Assert.Throws<ArgumentException>(() => osc.SetPeriodicWave(other.CreatePeriodicWave([0, 1], [0, 1])));
        Assert.Throws<NotSupportedException>(() => new RealFft(96));
    }

    [Theory]
    [InlineData(256)]
    [InlineData(2048)]
    [InlineData(4096)]
    [InlineData(16384)]
    public void TheForwardRealFftIsTheDft(int size)
    {
        var random = AudioFixtureRun.Mulberry32(5);
        var data = new float[size];
        for (var i = 0; i < size; i++)
            data[i] = (float)(random() * 2 - 1);
        var fft = new RealFft(size);
        var real = new float[size / 2];
        var imag = new float[size / 2];
        fft.Forward(data, real, imag);
        // A float FFT's error grows with log2(size) roundings of values up to √size · |x| (random data).
        var bound = 4 * Math.Log2(size) * Math.Pow(2, -24) * size;
        foreach (var k in new[] { 0, 1, 3, size / 8 + 1, size / 4, size / 2 - 1 })
        {
            double re = 0, im = 0;
            for (var n = 0; n < size; n++)
            {
                var angle = -2 * Math.PI * k * (long)n / size;
                re += data[n] * Math.Cos(angle);
                im += data[n] * Math.Sin(angle);
            }
            Assert.True(Math.Abs(real[k] - re) <= bound, $"bin {k}: real {real[k]} vs {re}");
            if (k > 0)
                Assert.True(Math.Abs(imag[k] - im) <= bound, $"bin {k}: imag {imag[k]} vs {im}");
        }
        // imag[0] holds the Nyquist bin (real-valued).
        double nyquist = 0;
        for (var n = 0; n < size; n++)
            nyquist += (n % 2 == 0 ? 1 : -1) * data[n];
        Assert.True(Math.Abs(imag[0] - nyquist) <= bound);
        // And back (the half-size inverse scaled by 2 / size gives the samples again).
        var back = new float[size];
        fft.Inverse(real, imag, back);
        for (var n = 0; n < size; n++)
            Assert.True(Math.Abs(back[n] - data[n]) <= bound / size * 4, $"sample {n}: {back[n]} vs {data[n]}");
    }
}
