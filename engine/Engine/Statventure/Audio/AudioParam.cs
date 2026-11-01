namespace Statventure.Audio;

/// <summary>
/// A node's parameter with Blink's automation timeline (audio_param_handler.cc, Chromium 152), ported
/// in its float and double steps, the x86 SSE paths' included: its intrinsic value, automation
/// events, audio-rate inputs summed in, a-rate (per sample) or k-rate (per render quantum).
/// </summary>
internal sealed class AudioParam
{
    const float TimeConstantsToConverge = 10.0f;
    const float SetTargetThreshold = 4.539992976248485e-05f;
    /// <summary>std::numeric_limits<float>::min(), the smallest normal float.</summary>
    const float SmallestNormalFloat = 1.17549435e-38f;

    enum EventType { SetValue, LinearRampToValue, ExponentialRampToValue, SetTarget, SetValueCurve, SetValueCurveEnd, CancelValues, LastType }

    sealed class ParamEvent
    {
        public EventType Type;
        public float Value;
        public double Time;
        public float InitialValue;
        public double CallTime;
        public double TimeConstant;
        public double Duration;
        public float[] Curve = [];
        public double CurvePointsPerSecond;
        public float CurveEndValue;
        public ParamEvent? SavedEvent;
        public bool HasDefaultCancelledValue;

        public ParamEvent Copy() => (ParamEvent)MemberwiseClone();
        public void SetCancelledValue(float value)
        {
            Value = value;
            HasDefaultCancelledValue = true;
        }
    }

    readonly AudioGraph _graph;
    readonly List<ParamEvent> _events = [];
    readonly HashSet<ParamEvent> _newEvents = [];
    readonly List<AudioNodeOutput> _inputs = [];
    float _intrinsic;

    public AudioParam(AudioGraph graph, float defaultValue, float minValue = -float.MaxValue, float maxValue = float.MaxValue, bool audioRate = true)
    {
        _graph = graph;
        DefaultValue = defaultValue;
        MinValue = minValue;
        MaxValue = maxValue;
        IsAudioRate = audioRate;
        _intrinsic = defaultValue;
    }

    public AudioGraph Graph => _graph;
    public float DefaultValue { get; }
    public float MinValue { get; }
    public float MaxValue { get; }
    public bool IsAudioRate { get; set; }
    public float IntrinsicValue => _intrinsic;

    /// <summary>Node outputs connected into the parameter (mixed down to mono, summed in, in connection order).</summary>
    public IReadOnlyList<AudioNodeOutput> Inputs => _inputs;

    internal bool AddInput(AudioNodeOutput output)
    {
        if (_inputs.Contains(output))
            return false;
        _inputs.Add(output);
        return true;
    }

    internal bool RemoveInput(AudioNodeOutput output) => _inputs.Remove(output);

    void SetIntrinsic(float value) => _intrinsic = Math.Clamp(value, MinValue, MaxValue);

    // ---------- The script side (AudioParam.idl) ----------

    /// <summary>param.value = x: the intrinsic value, and a setValueAtTime at the current time (the setter raises exceptions, so Blink's two-step overload).</summary>
    public float Value
    {
        get => _intrinsic;
        set
        {
            SetIntrinsic(value);
            SetValueAtTime(_intrinsic, _graph.CurrentTime);
        }
    }

    public AudioParam SetValueAtTime(float value, double time)
    {
        RequireNonNegative(time);
        InsertEvent(new ParamEvent { Type = EventType.SetValue, Value = value, Time = ClampedToCurrentTime(time) });
        return this;
    }

    public AudioParam LinearRampToValueAtTime(float value, double time)
    {
        RequireNonNegative(time);
        InsertEvent(new ParamEvent
        {
            Type = EventType.LinearRampToValue, Value = value, Time = ClampedToCurrentTime(time), InitialValue = _intrinsic, CallTime = _graph.CurrentTime,
        });
        return this;
    }

    public AudioParam ExponentialRampToValueAtTime(float value, double time)
    {
        RequireNonNegative(time);
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "an exponential ramp's target can't be 0");
        InsertEvent(new ParamEvent
        {
            Type = EventType.ExponentialRampToValue, Value = value, Time = ClampedToCurrentTime(time), InitialValue = _intrinsic, CallTime = _graph.CurrentTime,
        });
        return this;
    }

    public AudioParam SetTargetAtTime(float target, double time, double timeConstant)
    {
        RequireNonNegative(time);
        if (!(timeConstant >= 0))
            throw new ArgumentOutOfRangeException(nameof(timeConstant));
        time = ClampedToCurrentTime(time);
        InsertEvent(timeConstant == 0
            ? new ParamEvent { Type = EventType.SetValue, Value = target, Time = time }
            : new ParamEvent { Type = EventType.SetTarget, Value = target, Time = time, TimeConstant = timeConstant });
        return this;
    }

    public AudioParam SetValueCurveAtTime(float[] curve, double time, double duration)
    {
        RequireNonNegative(time);
        if (!(duration > 0))
            throw new ArgumentOutOfRangeException(nameof(duration));
        if (curve.Length < 2)
            throw new ArgumentException("a value curve needs at least 2 values");
        time = ClampedToCurrentTime(time);
        var copy = (float[])curve.Clone();
        if (!InsertEvent(new ParamEvent
            {
                Type = EventType.SetValueCurve, Time = time, Duration = duration, Curve = copy,
                CurvePointsPerSecond = (copy.Length - 1) / duration, CurveEndValue = copy[^1],
            }))
            return this;
        InsertEvent(new ParamEvent { Type = EventType.SetValueCurveEnd, Value = copy[^1], Time = time + duration });
        return this;
    }

    public AudioParam CancelScheduledValues(double cancelTime)
    {
        RequireNonNegative(cancelTime);
        cancelTime = ClampedToCurrentTime(cancelTime);
        for (var i = 0; i < _events.Count; i++)
        {
            var start = _events[i].Time;
            if (start >= cancelTime || (_events[i].Type == EventType.SetValueCurve && start <= cancelTime && start + _events[i].Duration > cancelTime))
            {
                RemoveCancelledEvents(i);
                break;
            }
        }
        return this;
    }

    public AudioParam CancelAndHoldAtTime(double cancelTime)
    {
        RequireNonNegative(cancelTime);
        cancelTime = ClampedToCurrentTime(cancelTime);
        int i;
        for (i = 0; i < _events.Count; i++)
            if (_events[i].Time > cancelTime)
                break;
        var cancelledIndex = i;
        if (i > 0 && (_events[i - 1].Type == EventType.SetTarget || _events[i - 1].Type == EventType.SetValueCurve))
            cancelledIndex = i - 1;
        else if (i >= _events.Count)
            return this;
        var cancelled = _events[cancelledIndex];
        ParamEvent? newEvent = null, newSetValue = null;
        switch (cancelled.Type)
        {
            case EventType.LinearRampToValue:
            case EventType.ExponentialRampToValue:
                var saved = cancelled.Copy();
                saved.SavedEvent = null;
                saved.HasDefaultCancelledValue = false;
                newEvent = new ParamEvent { Type = EventType.CancelValues, Time = cancelTime, SavedEvent = saved };
                break;
            case EventType.SetTarget:
                if (cancelled.Time < cancelTime)
                {
                    cancelledIndex++;
                    newEvent = new ParamEvent { Type = EventType.CancelValues, Time = cancelTime };
                }
                break;
            case EventType.SetValueCurve:
                if (cancelled.Time < cancelTime)
                {
                    if (cancelTime > cancelled.Time + cancelled.Duration)
                        cancelledIndex++;
                    else
                    {
                        var newDuration = cancelTime - cancelled.Time;
                        var endValue = ValueCurveAtTime(cancelTime, cancelled.Time, cancelled.Duration, cancelled.Curve);
                        newEvent = cancelled.Copy();
                        newEvent.Duration = newDuration;
                        newEvent.CurveEndValue = endValue;
                        newEvent.SavedEvent = null;
                        newEvent.HasDefaultCancelledValue = false;
                        newSetValue = new ParamEvent { Type = EventType.SetValue, Value = endValue, Time = cancelled.Time + newDuration };
                    }
                }
                break;
        }
        if (cancelledIndex < _events.Count)
            RemoveCancelledEvents(cancelledIndex);
        if (newEvent != null)
        {
            InsertEvent(newEvent);
            if (newSetValue != null)
                InsertEvent(newSetValue);
        }
        return this;
    }

    static void RequireNonNegative(double time)
    {
        if (!(time >= 0))
            throw new ArgumentOutOfRangeException(nameof(time), "a time must be a finite non-negative number");
    }

    double ClampedToCurrentTime(double time) => Math.Max(time, _graph.CurrentTime);

    bool InsertEvent(ParamEvent e)
    {
        var insertTime = e.Time;
        var count = _events.Count;
        if (count > 0)
        {
            var currentFrame = _graph.CurrentSampleFrame;
            var rate = _graph.SampleRate;
            var skipped = 0;
            for (var i = 0; i < count; i++)
            {
                var next = i < count - 1 ? _events[i + 1] : null;
                if (IsEventCurrent(_events[i], next, currentFrame, rate))
                    break;
                skipped++;
            }
            if (skipped > 0)
                RemoveOldEvents(skipped);
        }
        if (_events.Count == 0 && (e.Type == EventType.LinearRampToValue || e.Type == EventType.ExponentialRampToValue))
        {
            var start = new ParamEvent { Type = EventType.SetValue, Value = e.InitialValue, Time = _graph.CurrentTime };
            _events.Insert(0, start);
            _newEvents.Add(start);
        }
        if (_events.Count == 0)
        {
            _events.Insert(0, e);
            _newEvents.Add(e);
            return true;
        }
        var insertionIndex = _events.Count;
        var upper = insertionIndex - 1;
        if (_events[^1].Time > insertTime)
        {
            // upper_bound: the first event whose time is greater than the new one's.
            insertionIndex = 0;
            while (insertionIndex < _events.Count && !(insertTime < _events[insertionIndex].Time))
                insertionIndex++;
            upper = insertionIndex;
        }
        if (e.Type == EventType.SetValueCurve)
        {
            var endTime = e.Time + e.Duration;
            for (var i = Math.Min(upper, _events.Count - 1); i >= 0; i--)
            {
                var type = _events[i].Type;
                if (type == EventType.SetValueCurveEnd || type == EventType.CancelValues)
                    continue;
                if (type == EventType.SetValueCurve)
                {
                    var testEnd = _events[i].Time + _events[i].Duration;
                    if (e.Time < testEnd && _events[i].Time < endTime)
                        throw new InvalidOperationException("a value curve overlaps another");
                }
                else if (_events[i].Time > e.Time && _events[i].Time < endTime)
                    throw new InvalidOperationException("a value curve overlaps an event");
                if (_events[i].Time < insertTime)
                    break;
            }
        }
        else
        {
            for (var i = Math.Min(upper, _events.Count - 1); i >= 0; i--)
            {
                var type = _events[i].Type;
                if (type == EventType.SetValueCurveEnd || type == EventType.CancelValues)
                    continue;
                if (type == EventType.SetValueCurve)
                {
                    var end = _events[i].Time + _events[i].Duration;
                    if (e.Type != EventType.SetValueCurveEnd && e.Time >= _events[i].Time && e.Time < end)
                        throw new InvalidOperationException("an event overlaps a value curve");
                }
                if (_events[i].Time < insertTime)
                    break;
            }
        }
        _events.Insert(insertionIndex, e);
        _newEvents.Add(e);
        return true;
    }

    // ---------- The render side ----------

    /// <summary>AudioParamHandler::HasSampleAccurateValues.</summary>
    public bool HasSampleAccurateValues()
    {
        if (_inputs.Count > 0)
            return true;
        var n = _events.Count;
        if (n == 0)
            return false;
        var currentFrame = _graph.CurrentSampleFrame;
        var rate = _graph.SampleRate;
        var quantum = AudioGraph.RenderQuantumFrames;
        if (_events[0].Time > (currentFrame + quantum) / rate)
        {
            switch (_events[0].Type)
            {
                case EventType.SetTarget:
                case EventType.SetValue:
                case EventType.SetValueCurve:
                    return false;
            }
        }
        if (n >= 2)
            return true;
        switch (_events[0].Type)
        {
            case EventType.SetTarget:
                return _events[0].Time <= (currentFrame + quantum) / rate;
            case EventType.SetValueCurve:
            {
                var curveEnd = _events[0].Time + _events[0].Duration;
                var currentTime = currentFrame / rate;
                return _events[0].Time <= currentTime && currentTime < curveEnd;
            }
            default:
                return !(_events[0].Time < currentFrame / rate);
        }
    }

    /// <summary>AudioParamHandler::Value on the audio thread: the timeline's value at the quantum's start (k-rate), stored as the intrinsic value.</summary>
    public float ValueNow()
    {
        var v = _intrinsic;
        var (has, timeline) = ValueForContextTime(v);
        if (has)
            v = timeline;
        SetIntrinsic(v);
        return v;
    }

    /// <summary>AudioParamHandler::FinalValue: one value, the k-rate timeline plus the inputs.</summary>
    public float FinalValue()
    {
        Span<float> value = [_intrinsic];
        CalculateFinalValues(value, false);
        return value[0];
    }

    /// <summary>AudioParamHandler::CalculateSampleAccurateValues: per sample when a-rate, else the quantum's value repeated.</summary>
    public void CalculateSampleAccurateValues(Span<float> values) => CalculateFinalValues(values, IsAudioRate);

    void CalculateFinalValues(Span<float> values, bool sampleAccurate)
    {
        if (sampleAccurate)
        {
            var rate = _graph.SampleRate;
            var start = _graph.CurrentSampleFrame;
            SetIntrinsic(ValuesForFrameRange(start, start + values.Length, _intrinsic, values, rate, rate));
        }
        else
        {
            var value = _intrinsic;
            var (has, timeline) = ValueForContextTime(value);
            if (has)
                value = timeline;
            values.Fill(value);
            SetIntrinsic(value);
        }
        if (_inputs.Count == 0)
            return;
        // A unity-gain summing junction over the inputs (Blink's summing bus with the values as its
        // channel memory), mixed down to mono; k-rate takes only the first value.
        var junction = new SpanJunction(values[..(sampleAccurate ? values.Length : 1)]);
        foreach (var input in AudioGraph.InSummingOrder(_inputs, Graph, orderFree: 1))
            junction.Add(input.Pull(null, AudioGraph.RenderQuantumFrames));
        if (!sampleAccurate)
            values.Fill(values[0]);
        for (var i = 0; i < values.Length; i++)
            if (float.IsNaN(values[i]))
                values[i] = DefaultValue;
        VectorMath.Vclip(values, MinValue, MaxValue);
    }

    /// <summary>Blink's summing bus over the parameter's values (its channel memory is the values: never silent, plain additions).</summary>
    readonly ref struct SpanJunction(Span<float> values)
    {
        readonly Span<float> _values = values;

        public void Add(AudioBus bus)
        {
            if (bus.NumberOfChannels == 1)
            {
                if (!bus.Channel(0).IsSilent)
                    VectorMath.Vadd(_values, bus.Channel(0).Data.AsSpan(0, _values.Length), _values);
                return;
            }
            // Stereo into the mono junction: down-mixed (speakers), 0.5 per side, left first.
            VectorMath.Vsma(bus.Channel(0).Data.AsSpan(0, _values.Length), 0.5f, _values);
            VectorMath.Vsma(bus.Channel(1).Data.AsSpan(0, _values.Length), 0.5f, _values);
        }
    }

    (bool, float) ValueForContextTime(float defaultValue)
    {
        if (_events.Count == 0 || _graph.CurrentTime < _events[0].Time)
            return (false, defaultValue);
        var rate = _graph.SampleRate;
        var start = _graph.CurrentSampleFrame;
        Span<float> value = [0];
        var result = ValuesForFrameRange(start, start + 1, defaultValue, value, rate, rate / AudioGraph.RenderQuantumFrames);
        return (true, result);
    }

    float ValuesForFrameRange(long startFrame, long endFrame, float defaultValue, Span<float> values, double sampleRate, double controlRate)
    {
        var last = ValuesForFrameRangeImpl(startFrame, endFrame, defaultValue, values, sampleRate, controlRate);
        VectorMath.Vclip(values, MinValue, MaxValue);
        return last;
    }

    float ValuesForFrameRangeImpl(long startFrame, long endFrame, float defaultValue, Span<float> values, double sampleRate, double controlRate)
    {
        if (_events.Count == 0 || endFrame / sampleRate <= _events[0].Time)
        {
            values.Fill(defaultValue);
            return defaultValue;
        }
        var numberOfEvents = _events.Count;
        if (_newEvents.Count > 0)
            ClampNewEventsToCurrentTime(startFrame / sampleRate);
        if (numberOfEvents > 0 && HandleAllEventsInThePast(startFrame / sampleRate, sampleRate, ref defaultValue, values))
            return defaultValue;
        var (currentFrame, writeIndex) = HandleFirstEvent(values, defaultValue, startFrame, endFrame, sampleRate, startFrame, 0);
        var value = defaultValue;
        var skipped = 0;
        for (var i = 0; i < numberOfEvents && writeIndex < values.Length; i++)
        {
            var e = _events[i];
            var next = i < numberOfEvents - 1 ? _events[i + 1] : null;
            if (!IsEventCurrent(e, next, currentFrame, sampleRate))
            {
                skipped++;
                continue;
            }
            ProcessSetTargetFollowedByRamp(i, ref e, next?.Type ?? EventType.LastType, currentFrame, sampleRate, controlRate, ref value);
            var value1 = e.Value;
            var time1 = e.Time;
            var (value2, time2, nextType) = HandleCancelValues(e, next, next?.Value ?? value1, next?.Time ?? endFrame / sampleRate + 1);
            var fillToEndFrame = endFrame > time2 * sampleRate ? (long)Math.Ceiling(time2 * sampleRate) : endFrame;
            var fillToFrame = (int)Math.Min(fillToEndFrame - startFrame, values.Length);
            if (nextType == EventType.LinearRampToValue)
                (currentFrame, value, writeIndex) = ProcessLinearRamp(fillToFrame, time1, time2, value1, value2, sampleRate, values, currentFrame, value, writeIndex);
            else if (nextType == EventType.ExponentialRampToValue)
                (currentFrame, value, writeIndex) = ProcessExponentialRamp(fillToFrame, time1, time2, value1, value2, sampleRate, values, currentFrame, value, writeIndex);
            else
            {
                switch (e.Type)
                {
                    case EventType.SetValue:
                    case EventType.SetValueCurveEnd:
                    case EventType.LinearRampToValue:
                    case EventType.ExponentialRampToValue:
                        currentFrame = fillToEndFrame;
                        value = e.Value;
                        values[writeIndex..fillToFrame].Fill(value);
                        writeIndex = fillToFrame;
                        break;
                    case EventType.CancelValues:
                        (currentFrame, value, writeIndex) = ProcessCancelValues(fillToFrame, time1, sampleRate, controlRate, fillToEndFrame, e, i, values, currentFrame, value, writeIndex);
                        break;
                    case EventType.SetTarget:
                        (currentFrame, value, writeIndex) = ProcessSetTarget(fillToFrame, time1, value1, sampleRate, controlRate, fillToEndFrame, e, values, currentFrame, value, writeIndex);
                        break;
                    case EventType.SetValueCurve:
                        (currentFrame, value, writeIndex) = ProcessSetValueCurve(fillToFrame, time1, sampleRate, startFrame, endFrame, fillToEndFrame, e, values, currentFrame, value, writeIndex);
                        break;
                }
            }
        }
        if (skipped > 0)
            RemoveOldEvents(skipped);
        values[writeIndex..].Fill(value);
        return values[^1];
    }

    (long, int) HandleFirstEvent(Span<float> values, float defaultValue, long startFrame, long endFrame, double sampleRate, long currentFrame, int writeIndex)
    {
        var firstTime = _events[0].Time;
        if (firstTime > startFrame / sampleRate)
        {
            var fillToEndFrame = endFrame;
            var firstFrame = Math.Ceiling(firstTime * sampleRate);
            if (endFrame > firstFrame)
                fillToEndFrame = (long)firstFrame;
            var fillToFrame = (int)Math.Min(fillToEndFrame - startFrame, values.Length);
            values[writeIndex..fillToFrame].Fill(defaultValue);
            writeIndex = fillToFrame;
            currentFrame += fillToFrame;
        }
        return (currentFrame, writeIndex);
    }

    static bool IsEventCurrent(ParamEvent e, ParamEvent? next, long currentFrame, double sampleRate)
    {
        if (next == null)
            return true;
        if (next.Time * sampleRate >= currentFrame)
            return true;
        var eventFrame = e.Time * sampleRate;
        return (e.Type == EventType.SetValue || e.Type == EventType.SetValueCurveEnd) && eventFrame <= currentFrame && currentFrame < eventFrame + 1;
    }

    void ClampNewEventsToCurrentTime(double currentTime)
    {
        var clamped = false;
        foreach (var e in _newEvents)
            if (e.Time < currentTime)
            {
                e.Time = currentTime;
                clamped = true;
            }
        if (clamped)
        {
            // std::stable_sort by time.
            var sorted = _events.OrderBy(e => e.Time).ToList();
            _events.Clear();
            _events.AddRange(sorted);
        }
        _newEvents.Clear();
    }

    bool HandleAllEventsInThePast(double currentTime, double sampleRate, ref float defaultValue, Span<float> values)
    {
        var last = _events[^1];
        if (last.Time + 1.5 * AudioGraph.RenderQuantumFrames / sampleRate < currentTime)
        {
            if (last.Type == EventType.SetTarget)
            {
                if (HasSetTargetConverged(defaultValue, last.Value, currentTime, last.Time, last.TimeConstant))
                    defaultValue = last.Value;
                else
                    return false;
            }
            values.Fill(defaultValue);
            RemoveOldEvents(_events.Count);
            return true;
        }
        return false;
    }

    static bool HasSetTargetConverged(float value, float target, double currentTime, double startTime, double timeConstant)
    {
        if (currentTime > startTime + TimeConstantsToConverge * timeConstant)
            return true;
        if (target == 0 && Math.Abs(value) < SetTargetThreshold)
            return true;
        return target != 0 && Math.Abs(target - value) < SetTargetThreshold * Math.Abs(value);
    }

    void ProcessSetTargetFollowedByRamp(int index, ref ParamEvent e, EventType nextType, long currentFrame, double sampleRate, double controlRate, ref float value)
    {
        if (e.Type != EventType.SetTarget || (nextType != EventType.LinearRampToValue && nextType != EventType.ExponentialRampToValue))
            return;
        if (Math.Abs(2 * sampleRate * e.Time - 2 * currentFrame + 1) <= 1)
            value = (float)(e.Value + (value - e.Value) * Fdlibm.Exp(-(currentFrame / sampleRate - e.Time) / e.TimeConstant));
        else
        {
            var discrete = (float)DiscreteTimeConstant(e.TimeConstant, controlRate);
            value += (e.Value - value) * discrete;
        }
        _events[index] = new ParamEvent { Type = EventType.SetValue, Value = value, Time = currentFrame / sampleRate };
        e = _events[index];
    }

    /// <summary>audio_utilities::DiscreteTimeConstantForSampleRate: 1 − exp(−1 / (rate · τ)).</summary>
    internal static double DiscreteTimeConstant(double timeConstant, double sampleRate) => 1 - Fdlibm.Exp(-1 / (sampleRate * timeConstant));

    (float, double, EventType) HandleCancelValues(ParamEvent current, ParamEvent? next, float value2, double time2)
    {
        var nextType = next?.Type ?? EventType.LastType;
        if (next is { Type: EventType.CancelValues, SavedEvent: { } saved })
        {
            var value1 = current.Value;
            var time1 = current.Time;
            switch (current.Type)
            {
                case EventType.CancelValues:
                case EventType.LinearRampToValue:
                case EventType.ExponentialRampToValue:
                case EventType.SetValueCurveEnd:
                case EventType.SetValue:
                    time2 = next.Time;
                    nextType = saved.Type;
                    if (next.HasDefaultCancelledValue)
                        value2 = next.Value;
                    else
                    {
                        value2 = saved.Type == EventType.LinearRampToValue
                            ? LinearRampAtTime(next.Time, value1, time1, saved.Value, saved.Time)
                            : ExponentialRampAtTime(next.Time, value1, time1, saved.Value, saved.Time);
                        next.SetCancelledValue(value2);
                    }
                    break;
            }
        }
        return (value2, time2, nextType);
    }

    static float LinearRampAtTime(double t, float value1, double time1, float value2, double time2) =>
        (float)(value1 + (value2 - value1) * (t - time1) / (time2 - time1));

    static float ExponentialRampAtTime(double t, float value1, double time1, float value2, double time2) =>
        value1 == 0.0f || float.IsNegative(value1) != float.IsNegative(value2)
            ? value1
            : (float)(value1 * Fdlibm.Pow(value2 / value1, (t - time1) / (time2 - time1)));

    static float ValueCurveAtTime(double t, double time1, double duration, float[] curve)
    {
        var curveIndex = (curve.Length - 1) / duration * (t - time1);
        var k = (int)Math.Min((ulong)curveIndex, (ulong)(curve.Length - 1));
        var k1 = Math.Min(k + 1, curve.Length - 1);
        float c0 = curve[k], c1 = curve[k1];
        var delta = (float)Math.Min(curveIndex - k, 1.0);
        return c0 + (c1 - c0) * delta;
    }

    /// <summary>ProcessLinearRamp, its SSE path in float as Blink's x86 build computes it, then the scalar tail.</summary>
    static (long, float, int) ProcessLinearRamp(int fillToFrame, double time1, double time2, float value1, float value2, double sampleRate,
        Span<float> values, long currentFrame, float value, int writeIndex)
    {
        var deltaTime = time2 - time1;
        var k = deltaTime <= SmallestNormalFloat ? 0f : (float)(1 / deltaTime);
        var valueDelta = value2 - value1;
        if (fillToFrame > writeIndex)
        {
            var step = (float)(1 / sampleRate);
            var offset = (float)(currentFrame / sampleRate - time1);
            var slope = k * valueDelta;
            Span<float> lanes = [step * 0f, step * 1f, step * 2f, step * 3f];
            for (var j = 0; j < 4; j++)
                lanes[j] = (lanes[j] + offset) * slope + value1;
            var increment = (float)(4 / sampleRate * k * valueDelta);
            var truncated = writeIndex + (fillToFrame - writeIndex) / 4 * 4;
            currentFrame += truncated - writeIndex;
            for (; writeIndex < truncated; writeIndex += 4)
                for (var j = 0; j < 4; j++)
                {
                    values[writeIndex + j] = lanes[j];
                    lanes[j] += increment;
                }
        }
        if (writeIndex >= 1)
            value = values[writeIndex - 1];
        for (; writeIndex < fillToFrame; writeIndex++)
        {
            var x = (float)((currentFrame / sampleRate - time1) * k);
            value = value1 + x * valueDelta;
            currentFrame++;
            values[writeIndex] = value;
        }
        return (currentFrame, value, fillToFrame);
    }

    static (long, float, int) ProcessExponentialRamp(int fillToFrame, double time1, double time2, float value1, float value2, double sampleRate,
        Span<float> values, long currentFrame, float value, int writeIndex)
    {
        if (value1 * value2 <= 0 || time1 >= time2)
        {
            value = value1;
            values[writeIndex..fillToFrame].Fill(value);
            writeIndex = fillToFrame;
        }
        else
        {
            var deltaTime = time2 - time1;
            var sampleFrames = deltaTime * sampleRate;
            // value2 / value1 is a float division here, as in Blink.
            var multiplier = Fdlibm.Pow(value2 / value1, 1.0 / sampleFrames);
            value = (float)(value1 * Fdlibm.Pow(value2 / (double)value1, (currentFrame / sampleRate - time1) / deltaTime));
            double accumulator = value;
            for (; writeIndex < fillToFrame; writeIndex++)
            {
                value = (float)accumulator;
                accumulator *= multiplier;
                currentFrame++;
                values[writeIndex] = value;
            }
            if (currentFrame > time2 * sampleRate - 0.5)
                value = value2;
        }
        return (currentFrame, value, writeIndex);
    }

    static (long, float, int) ProcessSetTarget(int fillToFrame, double time1, float value1, double sampleRate, double controlRate, long fillToEndFrame,
        ParamEvent e, Span<float> values, long currentFrame, float value, int writeIndex)
    {
        var target = value1;
        var timeConstant = (float)e.TimeConstant;
        var discrete = (float)DiscreteTimeConstant(timeConstant, controlRate);
        var rampStartFrame = time1 * sampleRate;
        if (rampStartFrame <= currentFrame && currentFrame < rampStartFrame + 1)
            value = (float)(target + (value - target) * Fdlibm.Exp(-(currentFrame / sampleRate - time1) / timeConstant));
        else
            value += (target - value) * discrete;
        if (HasSetTargetConverged(value, target, currentFrame / sampleRate, time1, timeConstant))
        {
            currentFrame += fillToFrame - writeIndex;
            values[writeIndex..fillToFrame].Fill(target);
            writeIndex = fillToFrame;
        }
        else
        {
            if (fillToFrame > writeIndex)
            {
                var c0 = discrete;
                var c1 = c0 * (2 - c0);
                var c2 = c0 * ((c0 - 3) * c0 + 3);
                var c3 = c0 * (c0 * ((4 - c0) * c0 - 6) + 4);
                var truncated = writeIndex + (fillToFrame - writeIndex) / 4 * 4;
                for (; writeIndex < truncated; writeIndex += 4)
                {
                    var delta = target - value;
                    values[writeIndex] = value + delta * 0f;
                    values[writeIndex + 1] = value + delta * c0;
                    values[writeIndex + 2] = value + delta * c1;
                    values[writeIndex + 3] = value + delta * c2;
                    value += delta * c3;
                }
            }
            for (; writeIndex < fillToFrame; writeIndex++)
            {
                var v = value;
                value += (target - value) * discrete;
                values[writeIndex] = v;
            }
            if (writeIndex >= 1)
                value = values[writeIndex - 1];
            currentFrame = fillToEndFrame;
        }
        return (currentFrame, value, writeIndex);
    }

    static (long, float, int) ProcessSetValueCurve(int fillToFrame, double time1, double sampleRate, long startFrame, long endFrame, long fillToEndFrame,
        ParamEvent e, Span<float> values, long currentFrame, float value, int writeIndex)
    {
        var curve = e.Curve;
        var curveEndValue = e.CurveEndValue;
        var duration = e.Duration;
        var pointsPerFrame = e.CurvePointsPerSecond / sampleRate;
        if (curve.Length == 0 || duration <= 0 || sampleRate <= 0)
        {
            currentFrame = fillToEndFrame;
            values[writeIndex..fillToFrame].Fill(value);
            return (currentFrame, value, fillToFrame);
        }
        var nextEventFillToFrame = fillToFrame;
        var curveEndFrame = Math.Ceiling(sampleRate * (time1 + duration));
        fillToEndFrame = endFrame > curveEndFrame ? (long)curveEndFrame : endFrame;
        fillToFrame = fillToEndFrame < startFrame ? 0 : (int)Math.Min(fillToEndFrame - startFrame, values.Length);
        double virtualIndex = 0;
        if (time1 < currentFrame / sampleRate)
            virtualIndex = pointsPerFrame * (currentFrame - time1 * sampleRate);
        value = curveEndValue;
        long k = 0;
        if (fillToFrame > writeIndex)
        {
            var vIndex = (float)virtualIndex;
            var vPerFrame = (float)pointsPerFrame;
            var last = (float)(curve.Length - 1);
            Span<float> lane = [0, 1, 2, 3];
            var steps = (fillToFrame - writeIndex) / 4 * 4;
            var truncated = writeIndex + steps;
            for (; writeIndex < truncated; writeIndex += 4)
                for (var j = 0; j < 4; j++)
                {
                    var current = vIndex + lane[j] * vPerFrame;
                    lane[j] += 4;
                    // minps(a, b) is a < b ? a : b.
                    var index0 = (int)(current < last ? current : last);
                    var plus = current + 1;
                    var index1 = (int)(plus < last ? plus : last);
                    float c0 = curve[index0], c1 = curve[index1];
                    var d = current - index0;
                    var delta = d < 1f ? d : 1f;
                    values[writeIndex + j] = c0 + (c1 - c0) * delta;
                }
            k = steps;
        }
        if (writeIndex >= 1)
            value = values[writeIndex - 1];
        for (; writeIndex < fillToFrame; writeIndex++)
        {
            var current = virtualIndex + k * pointsPerFrame;
            var index0 = current < curve.Length ? (int)current : curve.Length - 1;
            var index1 = Math.Min(index0 + 1, curve.Length - 1);
            float c0 = curve[index0], c1 = curve[index1];
            var delta = Math.Min(current - index0, 1.0);
            k++;
            value = (float)(c0 + (c1 - c0) * delta);
            values[writeIndex] = value;
        }
        if (writeIndex < nextEventFillToFrame)
        {
            value = curveEndValue;
            values[writeIndex..nextEventFillToFrame].Fill(value);
            writeIndex = nextEventFillToFrame;
        }
        currentFrame += nextEventFillToFrame;
        return (currentFrame, value, writeIndex);
    }

    (long, float, int) ProcessCancelValues(int fillToFrame, double time1, double sampleRate, double controlRate, long fillToEndFrame, ParamEvent e,
        int eventIndex, Span<float> values, long currentFrame, float value, int writeIndex)
    {
        if (e.HasDefaultCancelledValue)
            value = e.Value;
        else
        {
            var cancelFrame = time1 * sampleRate;
            if (eventIndex >= 1 && cancelFrame <= currentFrame && currentFrame < cancelFrame + 1 && _events[eventIndex - 1].Type == EventType.SetTarget)
            {
                var target = _events[eventIndex - 1].Value;
                var timeConstant = (float)_events[eventIndex - 1].TimeConstant;
                var discrete = (float)DiscreteTimeConstant(timeConstant, controlRate);
                value += (target - value) * discrete;
            }
        }
        values[writeIndex..fillToFrame].Fill(value);
        return (fillToEndFrame, value, fillToFrame);
    }

    void RemoveCancelledEvents(int first)
    {
        for (var k = first; k < _events.Count; k++)
            _newEvents.Remove(_events[k]);
        _events.RemoveRange(first, _events.Count - first);
    }

    void RemoveOldEvents(int count)
    {
        var n = _events.Count;
        if (n <= 1)
            return;
        var remove = Math.Min(count, n - 1);
        for (var k = 0; k < remove; k++)
            _newEvents.Remove(_events[k]);
        _events.RemoveRange(0, remove);
    }
}
