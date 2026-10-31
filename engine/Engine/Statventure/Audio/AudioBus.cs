namespace Statventure.Audio;

/// <summary>How channels are mixed when two buses differ in count (Web Audio's channelInterpretation).</summary>
internal enum ChannelInterpretation { Speakers, Discrete }

/// <summary>
/// One channel of a render quantum (Blink's AudioChannel): its samples and whether it's silent. A
/// silent channel holds zeros; writing through <see cref="Mutable"/> clears the flag,
/// <see cref="Zero"/> sets it (and writes zeros only if it wasn't set).
/// </summary>
internal sealed class AudioChannel(int length)
{
    public float[] Data { get; } = new float[length];
    public int Length => Data.Length;
    public bool IsSilent { get; private set; } = true;

    public Span<float> Mutable
    {
        get
        {
            IsSilent = false;
            return Data;
        }
    }

    public void ClearSilentFlag() => IsSilent = false;

    public void Zero()
    {
        if (IsSilent)
            return;
        Array.Clear(Data);
        IsSilent = true;
    }

    public void CopyFrom(AudioChannel source)
    {
        if (source.IsSilent)
            Zero();
        else
            source.Data.AsSpan(0, Length).CopyTo(Mutable);
    }

    public void SumFrom(AudioChannel source)
    {
        if (source.IsSilent)
            return;
        if (IsSilent)
            CopyFrom(source);
        else
            VectorMath.Vadd(Data, source.Data, Mutable);
    }
}

/// <summary>
/// The channels of a render quantum (Blink's AudioBus): mono and stereo with Web Audio's speaker
/// mixing (mono to stereo copies, stereo to mono averages) or discrete mixing; other layouts are
/// refused (the game is stereo).
/// </summary>
internal sealed class AudioBus
{
    readonly AudioChannel[] _channels;

    public AudioBus(int channels, int length)
    {
        _channels = new AudioChannel[channels];
        for (var i = 0; i < channels; i++)
            _channels[i] = new AudioChannel(length);
        Length = length;
    }

    public int Length { get; }
    public int NumberOfChannels => _channels.Length;
    public AudioChannel Channel(int i) => _channels[i];

    public bool IsSilent
    {
        get
        {
            foreach (var channel in _channels)
                if (!channel.IsSilent)
                    return false;
            return true;
        }
    }

    public void Zero()
    {
        foreach (var channel in _channels)
            channel.Zero();
    }

    public void ClearSilentFlag()
    {
        foreach (var channel in _channels)
            channel.ClearSilentFlag();
    }

    public void CopyFrom(AudioBus source, ChannelInterpretation interpretation = ChannelInterpretation.Speakers)
    {
        if (source == this)
            return;
        Zero();
        SumFrom(source, interpretation);
    }

    /// <summary>AudioBus::SumFrom: channel by channel, else up- or down-mixed (speakers) or the common channels (discrete).</summary>
    public void SumFrom(AudioBus source, ChannelInterpretation interpretation = ChannelInterpretation.Speakers)
    {
        if (source == this)
            return;
        int from = source.NumberOfChannels, to = NumberOfChannels;
        if (from == to)
        {
            for (var i = 0; i < from; i++)
                _channels[i].SumFrom(source._channels[i]);
            return;
        }
        if (from > 2 || to > 2)
            throw new NotSupportedException($"mixing {from} channels into {to}: only mono and stereo are supported");
        if (interpretation == ChannelInterpretation.Discrete)
        {
            for (var i = 0; i < Math.Min(from, to); i++)
                _channels[i].SumFrom(source._channels[i]);
            return;
        }
        if (from == 1)
        {
            // Up-mixing mono to stereo: the one channel into both.
            _channels[0].SumFrom(source._channels[0]);
            _channels[1].SumFrom(source._channels[0]);
            return;
        }
        // Down-mixing stereo to mono: each side times 0.5 added on (Vsma), left first. Silent
        // sources still count here: Blink reads their (zero) samples.
        var destination = _channels[0].Mutable;
        VectorMath.Vsma(source._channels[0].Data, 0.5f, destination);
        VectorMath.Vsma(source._channels[1].Data, 0.5f, destination);
    }

    /// <summary>AudioBus::CopyWithGainFrom (same channel count).</summary>
    public void CopyWithGainFrom(AudioBus source, float gain)
    {
        if (source.IsSilent)
        {
            Zero();
            return;
        }
        if (source == this && gain == 1)
            return;
        for (var c = 0; c < NumberOfChannels; c++)
        {
            var destination = _channels[c].Mutable;
            if (gain == 1)
                source._channels[c].Data.AsSpan(0, Length).CopyTo(destination);
            else if (gain == 0)
                destination.Clear();
            else
                VectorMath.Vsmul(source._channels[c].Data, gain, destination);
        }
    }

    /// <summary>AudioBus::CopyWithSampleAccurateGainValuesFrom: each sample times its gain (a mono source feeds every channel).</summary>
    public void CopyWithSampleAccurateGainValuesFrom(AudioBus source, ReadOnlySpan<float> gains)
    {
        if (source.Length == gains.Length && source.Length == Length && source.IsSilent)
        {
            Zero();
            return;
        }
        var from = source._channels[0].Data;
        for (var c = 0; c < NumberOfChannels; c++)
        {
            if (source.NumberOfChannels == NumberOfChannels)
                from = source._channels[c].Data;
            VectorMath.Vmul(from, gains, _channels[c].Mutable[..gains.Length]);
        }
    }
}

/// <summary>Blink's vector_math operations the nodes use, element by element in float (the x86 SSE paths compute the same per element).</summary>
internal static class VectorMath
{
    public static void Vadd(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> destination)
    {
        for (var i = 0; i < destination.Length; i++)
            destination[i] = a[i] + b[i];
    }

    public static void Vsmul(ReadOnlySpan<float> source, float scale, Span<float> destination)
    {
        for (var i = 0; i < destination.Length; i++)
            destination[i] = source[i] * scale;
    }

    public static void Vmul(ReadOnlySpan<float> a, ReadOnlySpan<float> b, Span<float> destination)
    {
        for (var i = 0; i < destination.Length; i++)
            destination[i] = a[i] * b[i];
    }

    /// <summary>destination += source · scale (no fused multiply-add, as SSE computes it).</summary>
    public static void Vsma(ReadOnlySpan<float> source, float scale, Span<float> destination)
    {
        for (var i = 0; i < destination.Length; i++)
            destination[i] = destination[i] + source[i] * scale;
    }

    /// <summary>max(low, min(high, x)) as SSE computes it (minps/maxps: the second operand when either is NaN).</summary>
    public static void Vclip(Span<float> values, float low, float high)
    {
        for (var i = 0; i < values.Length; i++)
        {
            var v = values[i];
            v = high < v ? high : v;
            values[i] = low > v ? low : v;
        }
    }
}
