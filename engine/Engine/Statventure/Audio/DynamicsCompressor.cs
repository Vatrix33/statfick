namespace Statventure.Audio;

/// <summary>
/// DynamicsCompressorNode (Blink's DynamicsCompressorHandler and DynamicsCompressor,
/// platform/audio/dynamics_compressor.cc): a 6 ms look-ahead, a detector and envelope in blocks of
/// 32 frames with adaptive release, a soft knee (exponential, its curvature found by bisection)
/// into the ratio's slope, makeup gain, the reduction metered. Parameters are k-rate. The decibel
/// conversions use glibc's powf and log10f, the curves fdlibm's powf and asinf, the knee and the
/// gain's warp the C library's exp and sin (fdlibm's give the same floats on every input: checked).
/// </summary>
internal sealed class DynamicsCompressorNode : AudioNode
{
    const float MeteringReleaseTimeConstant = 0.325f;
    const float UninitializedValue = -1;
    const float PreDelay = 0.006f;
    const float SatReleaseTime = 0.0025f;
    const int MaxPreDelayFrames = 1024;
    const int MaxPreDelayFramesMask = MaxPreDelayFrames - 1;
    const int DefaultPreDelayFrames = 256;
    const int NumberOfDivisionFrames = 32;
    const float PiOverTwoFloat = (float)(Math.PI / 2);

    // kABase ... kEBase: constexpr float expressions in Blink, folded in float.
    static readonly float ABase, BBase, CBase, DBase, EBase;

    static DynamicsCompressorNode()
    {
        float z1 = 0.09f, z2 = 0.16f, z3 = 0.42f, z4 = 0.98f;
        ABase = Sum(0.9999999999999998f * z1, 1.8432219684323923e-16f * z2, -(1.9373394351676423e-16f * z3), 8.824516011816245e-18f * z4);
        BBase = Sum(-1.5788320352845888f * z1, 2.3305837032074286f * z2, -(0.9141194204840429f * z3), 0.1623677525612032f * z4);
        CBase = Sum(0.5334142869106424f * z1, -(1.272736789213631f * z2), 0.9258856042207512f * z3, -(0.18656310191776226f * z4));
        DBase = Sum(0.08783463138207234f * z1, -(0.1694162967925622f * z2), 0.08588057951595272f * z3, -(0.00429891410546283f * z4));
        EBase = Sum(-0.042416883008123074f * z1, 0.1115693827987602f * z2, -(0.09764676325265872f * z3), 0.028494263462021576f * z4);
    }

    /// <summary>a + b + c + d left to right in float (a − x is a + (−x) exactly).</summary>
    static float Sum(float a, float b, float c, float d) => a + b + c + d;

    readonly float[] _paramValues = new float[AudioGraph.RenderQuantumFrames];
    readonly float[][] _preDelayBuffers = [new float[MaxPreDelayFrames], new float[MaxPreDelayFrames]];
    readonly float _sampleRate;
    readonly float _meteringReleaseK;
    float _detectorAverage, _compressorGain = 1, _meteringGain = 1;
    int _preDelayReadIndex, _preDelayWriteIndex = DefaultPreDelayFrames;
    float _dbMaxAttackCompressionDiff = -1;
    int _lastPreDelayFrames = DefaultPreDelayFrames;
    float _ratio = UninitializedValue, _slope = UninitializedValue, _linearThreshold = UninitializedValue, _dbThreshold = UninitializedValue,
        _dbKnee = UninitializedValue, _kneeThreshold = UninitializedValue, _dbKneeThreshold = UninitializedValue,
        _dbYKneeThreshold = UninitializedValue, _knee = UninitializedValue;

    public DynamicsCompressorNode(AudioGraph graph) : base(graph)
    {
        _sampleRate = graph.SampleRateF;
        Threshold = new AudioParam(graph, -24, -100, 0, audioRate: false);
        Knee = new AudioParam(graph, 30, 0, 40, audioRate: false);
        Ratio = new AudioParam(graph, 12, 1, 20, audioRate: false);
        Attack = new AudioParam(graph, 0.003f, 0, 1, audioRate: false);
        Release = new AudioParam(graph, 0.25f, 0, 1, audioRate: false);
        AddInput();
        AddOutput(2);
        ChannelCountMode = ChannelCountMode.ClampedMax;
        _meteringReleaseK = (float)AudioParam.DiscreteTimeConstant(MeteringReleaseTimeConstant, _sampleRate);
    }

    public AudioParam Threshold { get; }
    public AudioParam Knee { get; }
    public AudioParam Ratio { get; }
    public AudioParam Attack { get; }
    public AudioParam Release { get; }

    /// <summary>The gain reduction in dB (the metered value after the last quantum).</summary>
    public float Reduction { get; private set; }

    protected override double TailTime => 5 * MeteringReleaseTimeConstant;
    protected override double LatencyTime => _lastPreDelayFrames / (double)_sampleRate;
    protected override bool RequiresTailProcessing => true;

    static float DecibelsToLinear(float decibels) => Libm.Powf(10, 0.05f * decibels);
    static float LinearToDecibels(float linear) => 20 * Libm.Log10f(linear);
    static float EnsureFinite(float x, float defaultValue) => float.IsFinite(x) ? x : defaultValue;

    protected override void Process(int frames)
    {
        var threshold = Threshold.FinalValue();
        var knee = Knee.FinalValue();
        var ratio = Ratio.FinalValue();
        var attack = Attack.FinalValue();
        var release = Release.FinalValue();
        Compress(Input(0).Bus, Output(0).Bus, frames, threshold, knee, ratio, attack, release);
        Reduction = _meteringGain;
    }

    protected override void ProcessOnlyAudioParams(int frames)
    {
        var values = _paramValues.AsSpan(0, frames);
        Threshold.CalculateSampleAccurateValues(values);
        Knee.CalculateSampleAccurateValues(values);
        Ratio.CalculateSampleAccurateValues(values);
        Attack.CalculateSampleAccurateValues(values);
        Release.CalculateSampleAccurateValues(values);
    }

    void Compress(AudioBus source, AudioBus destination, int frames, float dbThreshold, float dbKnee, float ratio, float attackTime, float releaseTime)
    {
        const int Channels = 2;
        var sourceChannels = source.NumberOfChannels;
        var sampleRate = _sampleRate;
        var k = UpdateStaticCurveParameters(dbThreshold, dbKnee, ratio);
        var linearPostGain = Fdlibm.Powf(1 / Saturate(1, k), 0.6f);
        var attackFrames = Math.Max(0.001f, attackTime) * sampleRate;
        var releaseFrames = sampleRate * releaseTime;
        var satReleaseFrames = SatReleaseTime * sampleRate;
        var a = releaseFrames * ABase;
        var b = releaseFrames * BBase;
        var c = releaseFrames * CBase;
        var d = releaseFrames * DBase;
        var e = releaseFrames * EBase;
        SetPreDelayTime(PreDelay);
        var divisions = frames / NumberOfDivisionFrames;
        var frameIndex = 0;
        for (var i = 0; i < divisions; i++)
        {
            _detectorAverage = EnsureFinite(_detectorAverage, 1);
            var desiredGain = _detectorAverage;
            var scaledDesiredGain = Fdlibm.Asinf(desiredGain) / PiOverTwoFloat;
            float envelopeRate;
            var isReleasing = scaledDesiredGain > _compressorGain;
            float dbCompressionDiff = scaledDesiredGain == 0
                ? (isReleasing ? -1 : 1)
                : LinearToDecibels(_compressorGain / scaledDesiredGain);
            if (isReleasing)
            {
                _dbMaxAttackCompressionDiff = -1;
                dbCompressionDiff = EnsureFinite(dbCompressionDiff, -1);
                var x = dbCompressionDiff;
                // ClampTo(x, -12.0f, 0.0f)
                x = x >= 0.0f ? 0.0f : x <= -12.0f ? -12.0f : x;
                x = 0.25f * (x + 12);
                var x2 = x * x;
                var x3 = x2 * x;
                var x4 = x2 * x2;
                var calcReleaseFrames = a + b * x + c * x2 + d * x3 + e * x4;
                const float DbSpacing = 5;
                var dbPerFrame = DbSpacing / calcReleaseFrames;
                envelopeRate = DecibelsToLinear(dbPerFrame);
            }
            else
            {
                dbCompressionDiff = EnsureFinite(dbCompressionDiff, 1);
                if (_dbMaxAttackCompressionDiff == -1 || _dbMaxAttackCompressionDiff < dbCompressionDiff)
                    _dbMaxAttackCompressionDiff = dbCompressionDiff;
                var dbEffAttenDiff = Math.Max(0.5f, _dbMaxAttackCompressionDiff);
                var x = 0.25f / dbEffAttenDiff;
                envelopeRate = 1 - Fdlibm.Powf(x, 1 / attackFrames);
            }
            var preDelayReadIndex = _preDelayReadIndex;
            var preDelayWriteIndex = _preDelayWriteIndex;
            var detectorAverage = _detectorAverage;
            var compressorGain = _compressorGain;
            for (var loop = 0; loop < NumberOfDivisionFrames; loop++)
            {
                float compressorInput = 0;
                for (var j = 0; j < Channels; j++)
                {
                    var undelayed = source.Channel(sourceChannels > j ? j : 0).Data[frameIndex];
                    _preDelayBuffers[j][preDelayWriteIndex] = undelayed;
                    var absUndelayed = undelayed > 0 ? undelayed : -undelayed;
                    if (compressorInput < absUndelayed)
                        compressorInput = absUndelayed;
                }
                var scaledInput = compressorInput;
                var absInput = scaledInput > 0 ? scaledInput : -scaledInput;
                var shapedInput = Saturate(absInput, k);
                var attenuation = absInput <= 0.0001f ? 1 : shapedInput / absInput;
                var dbAttenuation = Math.Max(2.0f, -LinearToDecibels(attenuation));
                var dbPerFrame = dbAttenuation / satReleaseFrames;
                var satReleaseRate = DecibelsToLinear(dbPerFrame) - 1;
                var isRelease = attenuation > detectorAverage;
                var rate = isRelease ? satReleaseRate : 1;
                detectorAverage += (attenuation - detectorAverage) * rate;
                detectorAverage = Math.Min(1.0f, detectorAverage);
                detectorAverage = EnsureFinite(detectorAverage, 1);
                if (envelopeRate < 1)
                    compressorGain += (scaledDesiredGain - compressorGain) * envelopeRate;
                else
                {
                    compressorGain *= envelopeRate;
                    compressorGain = Math.Min(1.0f, compressorGain);
                }
                var postWarpCompressorGain = (float)Fdlibm.Sin(PiOverTwoFloat * compressorGain);
                var totalGain = linearPostGain * postWarpCompressorGain;
                var dbRealGain = LinearToDecibels(postWarpCompressorGain);
                if (dbRealGain < _meteringGain)
                    _meteringGain = dbRealGain;
                else
                    _meteringGain += (dbRealGain - _meteringGain) * _meteringReleaseK;
                for (var j = 0; j < Channels; j++)
                    destination.Channel(j).Mutable[frameIndex] = _preDelayBuffers[j][preDelayReadIndex] * totalGain;
                frameIndex++;
                preDelayReadIndex = (preDelayReadIndex + 1) & MaxPreDelayFramesMask;
                preDelayWriteIndex = (preDelayWriteIndex + 1) & MaxPreDelayFramesMask;
            }
            _preDelayReadIndex = preDelayReadIndex;
            _preDelayWriteIndex = preDelayWriteIndex;
            // FlushDenormalFloatToZero is the identity on x86.
            _detectorAverage = detectorAverage;
            _compressorGain = compressorGain;
        }
    }

    void SetPreDelayTime(float preDelayTime)
    {
        var preDelayFrames = (int)(uint)(preDelayTime * _sampleRate);
        if (preDelayFrames > MaxPreDelayFrames - 1)
            preDelayFrames = MaxPreDelayFrames - 1;
        if (_lastPreDelayFrames != preDelayFrames)
        {
            _lastPreDelayFrames = preDelayFrames;
            foreach (var buffer in _preDelayBuffers)
                Array.Clear(buffer);
            _preDelayReadIndex = 0;
            _preDelayWriteIndex = preDelayFrames;
        }
    }

    float KneeCurve(float x, float k)
    {
        if (x < _linearThreshold)
            return x;
        return _linearThreshold + (1 - (float)Fdlibm.Exp(-k * (x - _linearThreshold))) / k;
    }

    float Saturate(float x, float k)
    {
        if (x < _kneeThreshold)
            return KneeCurve(x, k);
        var dbX = LinearToDecibels(x);
        var dbY = _dbYKneeThreshold + _slope * (dbX - _dbKneeThreshold);
        return DecibelsToLinear(dbY);
    }

    float KAtSlope(float desiredSlope)
    {
        var dbX = _dbThreshold + _dbKnee;
        var x = DecibelsToLinear(dbX);
        float x2 = 1, dbX2 = 0;
        if (!(x < _linearThreshold))
        {
            x2 = (float)(x * 1.001);
            dbX2 = LinearToDecibels(x2);
        }
        float minK = 0.1f, maxK = 10000, k = 5;
        float slope = 1;
        for (var i = 0; i < 15; i++)
        {
            if (!(x < _linearThreshold))
            {
                var dbY = LinearToDecibels(KneeCurve(x, k));
                var dbY2 = LinearToDecibels(KneeCurve(x2, k));
                slope = (dbY2 - dbY) / (dbX2 - dbX);
            }
            if (slope < desiredSlope)
                maxK = k;
            else
                minK = k;
            k = MathF.Sqrt(minK * maxK);
        }
        return k;
    }

    float UpdateStaticCurveParameters(float dbThreshold, float dbKnee, float ratio)
    {
        if (dbThreshold != _dbThreshold || dbKnee != _dbKnee || ratio != _ratio)
        {
            _dbThreshold = dbThreshold;
            _linearThreshold = DecibelsToLinear(dbThreshold);
            _dbKnee = dbKnee;
            _ratio = ratio;
            _slope = 1 / _ratio;
            var k = KAtSlope(1 / _ratio);
            _dbKneeThreshold = dbThreshold + dbKnee;
            _kneeThreshold = DecibelsToLinear(_dbKneeThreshold);
            _dbYKneeThreshold = LinearToDecibels(KneeCurve(_kneeThreshold, k));
            _knee = k;
        }
        return _knee;
    }
}
