namespace Statventure.Scene;

/// <summary>
/// Blender's easing functions (blenlib/intern/easing.cc, 5.2.0, Robert Penner's equations, with its
/// USE_ELASTIC_BLEND), in double precision: each of (time into the segment, its first value, the
/// change to the next key's, the segment's duration); the elastic period in the same unit as the times.
/// </summary>
static class FCurveEasing
{
    public static double Linear(double time, double begin, double change, double duration) => change * time / duration + begin;

    public static double BackIn(double time, double begin, double change, double duration, double overshoot)
    {
        time /= duration;
        return change * time * time * ((overshoot + 1) * time - overshoot) + begin;
    }

    public static double BackOut(double time, double begin, double change, double duration, double overshoot)
    {
        time = time / duration - 1;
        return change * (time * time * ((overshoot + 1) * time + overshoot) + 1) + begin;
    }

    public static double BackInOut(double time, double begin, double change, double duration, double overshoot)
    {
        overshoot *= 1.525;
        if ((time /= duration / 2) < 1)
            return change / 2 * (time * time * ((overshoot + 1) * time - overshoot)) + begin;
        time -= 2;
        return change / 2 * (time * time * ((overshoot + 1) * time + overshoot) + 2) + begin;
    }

    public static double BounceOut(double time, double begin, double change, double duration)
    {
        time /= duration;
        if (time < 1 / 2.75)
            return change * (7.5625 * time * time) + begin;
        if (time < 2 / 2.75)
        {
            time -= 1.5 / 2.75;
            return change * (7.5625 * time * time + 0.75) + begin;
        }
        if (time < 2.5 / 2.75)
        {
            time -= 2.25 / 2.75;
            return change * (7.5625 * time * time + 0.9375) + begin;
        }
        time -= 2.625 / 2.75;
        return change * (7.5625 * time * time + 0.984375) + begin;
    }

    public static double BounceIn(double time, double begin, double change, double duration) =>
        change - BounceOut(duration - time, 0, change, duration) + begin;

    public static double BounceInOut(double time, double begin, double change, double duration) =>
        time < duration / 2
            ? BounceIn(time * 2, 0, change, duration) * 0.5 + begin
            : BounceOut(time * 2 - duration, 0, change, duration) * 0.5 + change * 0.5 + begin;

    public static double CircIn(double time, double begin, double change, double duration)
    {
        time /= duration;
        return -change * (Math.Sqrt(1 - time * time) - 1) + begin;
    }

    public static double CircOut(double time, double begin, double change, double duration)
    {
        time = time / duration - 1;
        return change * Math.Sqrt(1 - time * time) + begin;
    }

    public static double CircInOut(double time, double begin, double change, double duration)
    {
        if ((time /= duration / 2) < 1)
            return -change / 2 * (Math.Sqrt(1 - time * time) - 1) + begin;
        time -= 2;
        return change / 2 * (Math.Sqrt(1 - time * time) + 1) + begin;
    }

    public static double CubicIn(double time, double begin, double change, double duration)
    {
        time /= duration;
        return change * time * time * time + begin;
    }

    public static double CubicOut(double time, double begin, double change, double duration)
    {
        time = time / duration - 1;
        return change * (time * time * time + 1) + begin;
    }

    public static double CubicInOut(double time, double begin, double change, double duration)
    {
        if ((time /= duration / 2) < 1)
            return change / 2 * time * time * time + begin;
        time -= 2;
        return change / 2 * (time * time * time + 2) + begin;
    }

    /// <summary>
    /// elastic_blend: with an amplitude below the change the oscillation is scaled down by their
    /// ratio, and blended back to full within a quarter period of the segment's end.
    /// </summary>
    static double ElasticBlend(double time, double change, double duration, double amplitude, double s, double f)
    {
        if (change != 0)
        {
            var t = Math.Abs(s);
            f = amplitude != 0 ? f * amplitude / Math.Abs(change) : 0;
            if (Math.Abs(time * duration) < t)
            {
                var l = Math.Abs(time * duration) / t;
                f = f * l + (1 - l);
            }
        }
        return f;
    }

    /// <summary>The oscillation's phase offset s and the amplitude used (0, or one below the change, uses the change: Blender's rule).</summary>
    static (double S, double Amplitude, double F) ElasticShape(double time, double change, double duration, double amplitude, double period)
    {
        if (amplitude == 0 || amplitude < Math.Abs(change))
        {
            var s = period / 4;
            return (s, change, ElasticBlend(time, change, duration, amplitude, s, 1));
        }
        return (period / (2 * Math.PI) * Math.Asin(change / amplitude), amplitude, 1);
    }

    public static double ElasticIn(double time, double begin, double change, double duration, double amplitude, double period)
    {
        if (time == 0)
            return begin;
        if ((time /= duration) == 1)
            return begin + change;
        time -= 1;
        if (period == 0)
            period = duration * 0.3;
        var (s, a, f) = ElasticShape(time, change, duration, amplitude, period);
        return -f * (a * Math.Pow(2, 10 * time) * Math.Sin((time * duration - s) * (2 * Math.PI) / period)) + begin;
    }

    public static double ElasticOut(double time, double begin, double change, double duration, double amplitude, double period)
    {
        if (time == 0)
            return begin;
        if ((time /= duration) == 1)
            return begin + change;
        time = -time;
        if (period == 0)
            period = duration * 0.3;
        var (s, a, f) = ElasticShape(time, change, duration, amplitude, period);
        return f * (a * Math.Pow(2, 10 * time) * Math.Sin((time * duration - s) * (2 * Math.PI) / period)) + change + begin;
    }

    public static double ElasticInOut(double time, double begin, double change, double duration, double amplitude, double period)
    {
        if (time == 0)
            return begin;
        if ((time /= duration / 2) == 2)
            return begin + change;
        time -= 1;
        if (period == 0)
            period = duration * (0.3 * 1.5);
        var (s, a, f) = ElasticShape(time, change, duration, amplitude, period);
        if (time < 0)
        {
            f *= -0.5;
            return f * (a * Math.Pow(2, 10 * time) * Math.Sin((time * duration - s) * (2 * Math.PI) / period)) + begin;
        }
        time = -time;
        f *= 0.5;
        return f * (a * Math.Pow(2, 10 * time) * Math.Sin((time * duration - s) * (2 * Math.PI) / period)) + change + begin;
    }

    /// <summary>2⁻¹⁰, the exponential's value at the start, taken off so it starts at 0.</summary>
    const double PowMin = 0.0009765625;
    const double PowScale = 1 / (1 - PowMin);

    public static double ExpoIn(double time, double begin, double change, double duration) =>
        time == 0 ? begin : change * (Math.Pow(2, 10 * (time / duration - 1)) - PowMin) * PowScale + begin;

    public static double ExpoOut(double time, double begin, double change, double duration) =>
        time == 0 ? begin : change * (1 - (Math.Pow(2, -10 * time / duration) - PowMin) * PowScale) + begin;

    public static double ExpoInOut(double time, double begin, double change, double duration)
    {
        double halfDuration = duration / 2, halfChange = change / 2;
        return time <= halfDuration
            ? ExpoIn(time, begin, halfChange, halfDuration)
            : ExpoOut(time - halfDuration, begin + halfChange, halfChange, halfDuration);
    }

    public static double QuadIn(double time, double begin, double change, double duration)
    {
        time /= duration;
        return change * time * time + begin;
    }

    public static double QuadOut(double time, double begin, double change, double duration)
    {
        time /= duration;
        return -change * time * (time - 2) + begin;
    }

    public static double QuadInOut(double time, double begin, double change, double duration)
    {
        if ((time /= duration / 2) < 1)
            return change / 2 * time * time + begin;
        time -= 1;
        return -change / 2 * (time * (time - 2) - 1) + begin;
    }

    public static double QuartIn(double time, double begin, double change, double duration)
    {
        time /= duration;
        return change * time * time * time * time + begin;
    }

    public static double QuartOut(double time, double begin, double change, double duration)
    {
        time = time / duration - 1;
        return -change * (time * time * time * time - 1) + begin;
    }

    public static double QuartInOut(double time, double begin, double change, double duration)
    {
        if ((time /= duration / 2) < 1)
            return change / 2 * time * time * time * time + begin;
        time -= 2;
        return -change / 2 * (time * time * time * time - 2) + begin;
    }

    public static double QuintIn(double time, double begin, double change, double duration)
    {
        time /= duration;
        return change * time * time * time * time * time + begin;
    }

    public static double QuintOut(double time, double begin, double change, double duration)
    {
        time = time / duration - 1;
        return change * (time * time * time * time * time + 1) + begin;
    }

    public static double QuintInOut(double time, double begin, double change, double duration)
    {
        if ((time /= duration / 2) < 1)
            return change / 2 * time * time * time * time * time + begin;
        time -= 2;
        return change / 2 * (time * time * time * time * time + 2) + begin;
    }

    public static double SineIn(double time, double begin, double change, double duration) =>
        -change * Math.Cos(time / duration * (Math.PI / 2)) + change + begin;

    public static double SineOut(double time, double begin, double change, double duration) =>
        change * Math.Sin(time / duration * (Math.PI / 2)) + begin;

    public static double SineInOut(double time, double begin, double change, double duration) =>
        -change / 2 * (Math.Cos(Math.PI * time / duration) - 1) + begin;
}
