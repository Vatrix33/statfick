namespace Statventure.Audio;

/// <summary>
/// StereoPannerNode (Blink's StereoPannerHandler and StereoPanner): the equal-power law with fdlibm's
/// cos and sin in double, per sample while the pan moves, else one value a quantum; a mono input
/// spread over both sides, a stereo one's far side folded into the near one. Always stereo out.
/// </summary>
internal sealed class StereoPannerNode : AudioNode
{
    const double PiOverTwo = Math.PI / 2;

    readonly float[] _panValues = new float[AudioGraph.RenderQuantumFrames];

    public StereoPannerNode(AudioGraph graph) : base(graph)
    {
        Pan = new AudioParam(graph, 0, -1, 1);
        AddInput();
        AddOutput(2);
        ChannelCount = 2;
        ChannelCountMode = ChannelCountMode.ClampedMax;
    }

    public AudioParam Pan { get; }

    /// <summary>ClampTo(value, −1.0, 1.0) in double.</summary>
    static double ClampPan(double pan) => pan >= 1.0 ? 1.0 : pan <= -1.0 ? -1.0 : pan;

    protected override void Process(int frames)
    {
        var output = Output(0).Bus;
        if (!Input(0).IsConnected)
        {
            output.Zero();
            return;
        }
        var input = Input(0).Bus;
        if (Pan.HasSampleAccurateValues() && Pan.IsAudioRate)
        {
            var values = _panValues.AsSpan(0, frames);
            Pan.CalculateSampleAccurateValues(values);
            PanWithSampleAccurateValues(input, output, values);
            return;
        }
        var pan = Pan.HasSampleAccurateValues() ? Pan.FinalValue() : Pan.ValueNow();
        PanToTargetValue(input, output, pan, frames);
    }

    protected override void ProcessOnlyAudioParams(int frames) => Pan.CalculateSampleAccurateValues(_panValues.AsSpan(0, frames));

    static void PanWithSampleAccurateValues(AudioBus input, AudioBus output, ReadOnlySpan<float> panValues)
    {
        var frames = panValues.Length;
        var sourceL = input.Channel(0).Data.AsSpan(0, frames);
        var sourceR = (input.NumberOfChannels > 1 ? input.Channel(1) : input.Channel(0)).Data.AsSpan(0, frames);
        var destinationL = output.Channel(0).Mutable[..frames];
        var destinationR = output.Channel(1).Mutable[..frames];
        if (input.NumberOfChannels == 1)
            for (var i = 0; i < frames; i++)
            {
                var inputL = sourceL[i];
                var pan = ClampPan(panValues[i]);
                var panRadian = (pan * 0.5 + 0.5) * PiOverTwo;
                double gainL = Fdlibm.Cos(panRadian), gainR = Fdlibm.Sin(panRadian);
                destinationL[i] = (float)(inputL * gainL);
                destinationR[i] = (float)(inputL * gainR);
            }
        else
            for (var i = 0; i < frames; i++)
            {
                float inputL = sourceL[i], inputR = sourceR[i];
                var pan = ClampPan(panValues[i]);
                var panRadian = (pan <= 0 ? pan + 1 : pan) * PiOverTwo;
                double gainL = Fdlibm.Cos(panRadian), gainR = Fdlibm.Sin(panRadian);
                if (pan <= 0)
                {
                    destinationL[i] = (float)(inputL + inputR * gainL);
                    destinationR[i] = (float)(inputR * gainR);
                }
                else
                {
                    destinationL[i] = (float)(inputL * gainL);
                    destinationR[i] = (float)(inputR + inputL * gainR);
                }
            }
    }

    static void PanToTargetValue(AudioBus input, AudioBus output, float panValue, int frames)
    {
        var sourceL = input.Channel(0).Data.AsSpan(0, frames);
        var sourceR = (input.NumberOfChannels > 1 ? input.Channel(1) : input.Channel(0)).Data.AsSpan(0, frames);
        var destinationL = output.Channel(0).Mutable[..frames];
        var destinationR = output.Channel(1).Mutable[..frames];
        var targetPan = (float)ClampPan(panValue);
        if (input.NumberOfChannels == 1)
        {
            var panRadian = (targetPan * 0.5 + 0.5) * PiOverTwo;
            double gainL = Fdlibm.Cos(panRadian), gainR = Fdlibm.Sin(panRadian);
            for (var i = 0; i < frames; i++)
            {
                var inputL = sourceL[i];
                destinationL[i] = (float)(inputL * gainL);
                destinationR[i] = (float)(inputL * gainR);
            }
        }
        else
        {
            var panRadian = (targetPan <= 0 ? targetPan + 1 : targetPan) * PiOverTwo;
            double gainL = Fdlibm.Cos(panRadian), gainR = Fdlibm.Sin(panRadian);
            for (var i = 0; i < frames; i++)
            {
                float inputL = sourceL[i], inputR = sourceR[i];
                if (targetPan <= 0)
                {
                    destinationL[i] = (float)(inputL + inputR * gainL);
                    destinationR[i] = (float)(inputR * gainR);
                }
                else
                {
                    destinationL[i] = (float)(inputL * gainL);
                    destinationR[i] = (float)(inputR + inputL * gainR);
                }
            }
        }
    }
}
