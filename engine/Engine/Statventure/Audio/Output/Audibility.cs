namespace Statventure.Audio;

/// <summary>
/// Whether the output is audible, as Chromium 152's audio service decides it for a page's stream
/// (Docs/Audio.md, "Audibility"): media::AudioPowerMonitor averages the power of what goes to the
/// device (a first-order low-pass, 10 ms time constant, the channels averaged), and
/// AudibilityHelperImpl polls it 15 times a second: audible at once at −72.24719896 dBFS or more,
/// silent only after 100 ms below; a stream that starts is silent until a poll says otherwise, and
/// one that pauses is silent at once. Blink's scheduler doesn't throttle an audible page
/// (<see cref="UI.Dom.UiDocument.Audible"/>).
/// </summary>
internal sealed class Audibility
{
    // AudibilityHelperImpl's constants.
    internal const float SilenceThresholdDbfs = -72.24719896f;
    internal const double GlitchTolerance = 0.1;
    internal const double PollingPeriod = 1.0 / 15;
    // OutputController's kPowerMeasurementTimeConstantMillis.
    const float TimeConstant = 0.010f;

    readonly float _weight;
    float _averagePower;
    volatile float _reading;
    double? _firstSilent;
    double _nextPoll;
    bool _polling;
    volatile bool _audible;

    public Audibility(float sampleRate) =>
        // AudioPowerMonitor's sample weight.
        _weight = 1f - MathF.Exp(-1f / (sampleRate * TimeConstant));

    /// <summary>Whether the output counts as audible now (the last poll's answer).</summary>
    public bool Audible => _audible;

    /// <summary>AudioPowerMonitor::Scan over interleaved frames: each channel's EWMA from the shared average, then their mean.</summary>
    public void Scan(ReadOnlySpan<float> interleaved, int channels)
    {
        var frames = interleaved.Length / channels;
        if (frames <= 0 || channels <= 0)
            return;
        var sum = 0f;
        var previous = 1f - _weight;
        for (var c = 0; c < channels; c++)
        {
            // vector_math::EWMAAndMaxPower (the plain C form).
            var power = _averagePower;
            for (var i = 0; i < frames; i++)
            {
                var sample = interleaved[i * channels + c];
                power *= previous;
                power += sample * sample * _weight;
            }
            sum += float.IsFinite(power) ? power : _averagePower;
        }
        _averagePower = Math.Clamp(sum / channels, 0f, 1f);
        _reading = _averagePower;
    }

    /// <summary>AudioPowerMonitor::ReadCurrentPowerAndClip's power in dBFS (−∞ below −100 dBFS).</summary>
    public float PowerDbfs => _reading < 1.0e-10f ? float.NegativeInfinity : 10f * MathF.Log10(_reading);

    /// <summary>The stream plays (StartPolling): silent now, polled from <paramref name="now"/> (seconds) on.</summary>
    public void Start(double now)
    {
        _polling = true;
        _audible = false;
        // kForcedSilenceStartTime: a silent new stream doesn't count as recently audible.
        _firstSilent = double.NegativeInfinity;
        _nextPoll = now + PollingPeriod;
    }

    /// <summary>The stream pauses (StopPolling): silent at once; the power monitor starts over (OutputController::Pause resets it).</summary>
    public void Stop()
    {
        _polling = false;
        _audible = false;
        _averagePower = 0;
        _reading = 0;
    }

    /// <summary>Polls when a polling period has passed by <paramref name="now"/> (seconds); returns true when the answer changed.</summary>
    public bool Poll(double now)
    {
        if (!_polling || now < _nextPoll)
            return false;
        // A repeating timer: the next poll a period after this one.
        _nextPoll = Math.Max(_nextPoll + PollingPeriod, now);
        var was = _audible;
        _audible = ComputeAudible(now);
        return _audible != was;
    }

    bool ComputeAudible(double now)
    {
        if (PowerDbfs >= SilenceThresholdDbfs)
        {
            _firstSilent = null;
            return true;
        }
        _firstSilent ??= now;
        return now - _firstSilent.Value <= GlitchTolerance;
    }
}
