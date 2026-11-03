using Statventure.Audio;

namespace Statventure.Tests.Audio;

/// <summary>
/// Whether the output is audible, as Chromium 152's audio service decides it (media::AudioPowerMonitor
/// with a 10 ms time constant, AudibilityHelperImpl's 15 polls a second, its −72.24719896 dBFS
/// threshold and 100 ms glitch tolerance; Docs/Audio.md, "Audibility"), and the audio system's use
/// of it: audible while it plays sound, silent at once when paused.
/// </summary>
public class AudibilityTests
{
    const int Rate = 48000;
    const double Poll = Audibility.PollingPeriod;

    /// <summary>Interleaved stereo frames of a constant value (the power monitor squares samples, so a constant gives its power exactly).</summary>
    static float[] Constant(float value, int frames) => Enumerable.Repeat(value, frames * 2).ToArray();

    [Fact]
    public void ThePowerIsAnAverageWithATenMillisecondTimeConstant()
    {
        var audibility = new Audibility(Rate);
        Assert.Equal(float.NegativeInfinity, audibility.PowerDbfs);
        // 10 ms of a full-scale signal from silence: 1 − 1/e of its power.
        audibility.Scan(Constant(1, Rate / 100), 2);
        Assert.Equal(1 - Math.Exp(-1), Math.Pow(10, audibility.PowerDbfs / 10), 3);
        // Long enough, the power of a constant 0.1: −20 dBFS.
        audibility.Scan(Constant(0.1f, Rate), 2);
        Assert.Equal(-20, audibility.PowerDbfs, 2);
    }

    [Fact]
    public void ThePowerAveragesTheChannels()
    {
        var audibility = new Audibility(Rate);
        var frames = new float[Rate * 2];
        for (var i = 0; i < Rate; i++)
            frames[2 * i] = 0.2f;
        audibility.Scan(frames, 2);
        // Left 0.04, right 0: 0.02.
        Assert.Equal(10 * Math.Log10(0.02), audibility.PowerDbfs, 2);
    }

    [Fact]
    public void AudibleFromTheThresholdAtTheNextPoll()
    {
        foreach (var (dbfs, audible) in new[] { (-72.2, true), (-72.3, false) })
        {
            var audibility = new Audibility(Rate);
            audibility.Start(0);
            audibility.Scan(Constant((float)Math.Pow(10, dbfs / 20), Rate), 2);
            // Not before the first poll.
            Assert.False(audibility.Poll(Poll * 0.9));
            Assert.False(audibility.Audible);
            Assert.Equal(audible, audibility.Poll(Poll));
            Assert.Equal(audible, audibility.Audible);
        }
    }

    [Fact]
    public void SilenceCountsOnlyAfterAHundredMilliseconds()
    {
        var audibility = new Audibility(Rate);
        audibility.Start(0);
        audibility.Scan(Constant(0.5f, Rate / 10), 2);
        audibility.Poll(Poll);
        Assert.True(audibility.Audible);
        // Silence: the power is below the threshold within half a second; polls go on 15 times a second.
        audibility.Scan(Constant(0, Rate / 2), 2);
        var firstSilentPoll = 2 * Poll;
        Assert.False(audibility.Poll(firstSilentPoll));
        Assert.False(audibility.Poll(firstSilentPoll + Poll));
        Assert.True(audibility.Audible);
        // More than 100 ms after the first silent poll: silent.
        Assert.True(audibility.Poll(firstSilentPoll + 2 * Poll));
        Assert.False(audibility.Audible);
    }

    [Fact]
    public void AStreamStartsSilentAndPausingSilencesItAtOnce()
    {
        var audibility = new Audibility(Rate);
        audibility.Start(0);
        // A silent new stream isn't "recently audible" (no 100 ms grace at the start).
        Assert.False(audibility.Poll(Poll));
        audibility.Scan(Constant(0.5f, Rate / 10), 2);
        audibility.Poll(2 * Poll);
        Assert.True(audibility.Audible);
        audibility.Stop();
        Assert.False(audibility.Audible);
        Assert.Equal(float.NegativeInfinity, audibility.PowerDbfs);
        // Stopped, it isn't polled.
        Assert.False(audibility.Poll(10));
        // Started again, the power starts from nothing (OutputController::Pause reset it).
        audibility.Start(11);
        audibility.Scan(Constant(0, 1), 2);
        Assert.Equal(float.NegativeInfinity, audibility.PowerDbfs);
    }

    /// <summary>An output that takes buffers at the real-time pace, as a device does (about 20 ms ahead), and drops them.</summary>
    sealed class Sink : IAudioOutput
    {
        readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
        long _frames;
        public int SampleRate => Rate;
        public int BufferFrames => OpenAlOutput.FramesPerBuffer(Rate);
        public int Writable => (double)Interlocked.Read(ref _frames) / Rate < _clock.Elapsed.TotalSeconds + 0.02 ? 1 : 0;
        public void Write(ReadOnlySpan<float> interleaved) => Interlocked.Add(ref _frames, interleaved.Length / 2);
        public void Maintain() { }
        public void Pause() { }
        public void Resume() { }
        public void Wait(int milliseconds) => Thread.Sleep(1);
        public bool HasDevice => true;
        public void Dispose() { }
    }

    [Fact]
    public void TheAudioSystemIsAudibleWhilePlayingAndSilentWhenPaused()
    {
        using var system = new AudioSystem(new Sink());
        static bool Within(Func<bool> condition)
        {
            for (var i = 0; i < 1000 && !condition(); i++)
                Thread.Sleep(10);
            return condition();
        }
        // Silence: never audible.
        Thread.Sleep(300);
        Assert.False(system.Audible);
        system.Music.Play("alder-vale");
        Assert.True(Within(() => system.Audible), "playing music didn't become audible");
        system.Pause();
        Assert.True(Within(() => !system.Audible), "pausing didn't silence it");
        system.Resume();
        Assert.True(Within(() => system.Audible), "resuming didn't make it audible again");
    }
}
