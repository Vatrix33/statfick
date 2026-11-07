using System.Text.Json.Nodes;
using Statventure.Records;
using Statventure.Scene;
using static Statventure.Tests.Scene.FCurveScenes;

namespace Statventure.Tests.Scene;

/// <summary>
/// The F-curve's own rules, beside Blender's (FCurveReferenceTests): what a curve refuses when it is
/// made, copied or read (S2-3), the key helpers, equality by content, and handles worked out for each
/// copy (S2-2).
/// </summary>
public class FCurveTests
{
    static FCurveKey K(double frame, double value) => new(F(frame), value);

    public static TheoryData<string, FCurveKey[], string> Refused => new()
    {
        { "an F-curve needs at least one key", [], "$.keys" },
        { "key 1 (at 0.1 s): keys must be in increasing time order at least 0.01 frame (1/6000 s) apart, and key 0 is at 0.2 s", [K(12, 0), K(6, 1)], "$.keys[1].time" },
        { "key 1 (at 0.2 s): keys must be in increasing time order at least 0.01 frame (1/6000 s) apart, and key 0 is at 0.2 s", [K(12, 0), K(12, 1)], "$.keys[1].time" },
        { $"key 1 (at {F(12.005):R} s): keys must be in increasing time order at least 0.01 frame (1/6000 s) apart, and key 0 is at 0.2 s", [K(12, 0), K(12.005, 1)], "$.keys[1].time" },
        { "key 0: its time and value must be finite numbers", [new(double.NaN, 0)], "$.keys[0].time" },
        { "key 1: its time and value must be finite numbers", [K(0, 0), new(F(3), double.PositiveInfinity)], "$.keys[1].value" },
        { "key 0 (at 0 s): its left handle is free but has no position (time and value)", [new(0, 0, Left: HandleType.Free, Right: HandleType.Auto)], "$.keys[0].leftTime" },
        { "key 0 (at 0 s): its right handle is aligned but has no position (time and value)", [new(0, 0, Right: HandleType.Aligned, RightTime: 0.1)], "$.keys[0].rightValue" },
        { "key 0 (at 0 s): its left handle is auto-clamped, which is worked out, but a position is given", [new(0, 0, LeftTime: -0.1, LeftValue: 0)], "$.keys[0].leftTime" },
        { "key 0 (at 0 s): its right handle is vector, which is worked out, but a position is given", [new(0, 0, Right: HandleType.Vector, RightValue: 1)], "$.keys[0].rightValue" },
        { "key 0 (at 0 s): its left handle's position must be finite numbers", [FCurveKey.Free(0, 0, (double.NegativeInfinity, 0), (0.1, 0))], "$.keys[0].leftTime" },
        { "key 0 (at 0 s): back, amplitude and period must be finite numbers", [new(0, 0, Period: double.NaN)], "$.keys[0].period" },
        { "key 0 (at 0 s): a negative amplitude (-0.5; Blender's are 0 or more)", [new(0, 0, Amplitude: -0.5)], "$.keys[0].amplitude" },
        { "key 0 (at 0 s): an interpolation, easing or handle type that isn't one", [new(0, 0, (KeyInterpolation)13)], "$.keys[0].interpolation" },
        { "key 1 is null", [K(0, 0), null!], "$.keys[1]" },
    };

    /// <summary>Making a curve, copying one with other keys and reading one each refuse what breaks the rules, naming the key and the rule.</summary>
    [Theory]
    [MemberData(nameof(Refused))]
    public void CurvesThatBreakTheRulesAreRefused(string expected, FCurveKey[] keys, string path)
    {
        Assert.Equal(expected, Assert.ThrowsAny<ArgumentException>(() => new FCurve(keys)).Message);
        // The curve's rules for keys a form edits (M11 step 6): the same words first, at the path of what they name in the curve.
        var first = FCurve.ProblemsOf(keys)[0];
        Assert.Equal((path, expected), (first.Path.ToString(), first.Message));
        var good = new FCurve([K(0, 1)]);
        Assert.Equal(expected, Assert.ThrowsAny<ArgumentException>(() => good with { Keys = keys }).Message);
        if (keys.All(k => k != null) && keys.All(k => Enum.IsDefined(k.Interpolation)))
        {
            // The JSON form of a curve with these keys (written key by key, as no such curve exists).
            var json = new JsonObject
            {
                ["$types"] = new JsonObject { ["scene.curve"] = 1, ["scene.key"] = 1 },
                ["keys"] = new JsonArray(keys.Select(k =>
                {
                    var key = JsonNode.Parse(RecordSerializer.ToJson(k))!.AsObject();
                    key.Remove("$types");
                    return (JsonNode)key;
                }).ToArray()),
            }.ToJsonString();
            Assert.Equal(expected, Assert.ThrowsAny<ArgumentException>(() => RecordSerializer.FromJson<FCurve>(json)).Message);
        }
    }

    /// <summary>The curve's rules for keys a form edits name every key that breaks one, at each field it names; good keys have none.</summary>
    [Fact]
    public void ProblemsOfNamesEveryKeyAtItsFields()
    {
        FCurveKey[] keys = [new(0, double.NaN, Amplitude: -1), null!, new(F(1), 0, Left: HandleType.Free), K(0.5, 1)];
        Assert.Equal(["$.keys[0].value", "$.keys[0].amplitude", "$.keys[1]", "$.keys[2].leftTime", "$.keys[2].leftValue", "$.keys[3].time"],
            FCurve.ProblemsOf(keys).Select(p => p.Path.ToString()));
        Assert.Empty(FCurve.ProblemsOf([K(0, 0), FCurveKey.Free(F(1), 1, (F(0.5), 0), (F(1.5), 2)), K(2, 0)]));
        // Making one refuses it with the first.
        Assert.Equal("key 0: its time and value must be finite numbers", Assert.ThrowsAny<ArgumentException>(() => new FCurve(keys)).Message);
    }

    [Fact]
    public void ExtrapolationAndSmoothingMustBeMembers()
    {
        Assert.Throws<ArgumentException>(() => new FCurve([K(0, 1)], (CurveExtrapolation)2));
        Assert.Throws<ArgumentException>(() => new FCurve([K(0, 1)], smoothing: (CurveSmoothing)2));
    }

    /// <summary>Keys just over Blender's merge distance (0.01 frame) apart are two keys.</summary>
    [Fact]
    public void KeysJustOverTheMergeDistanceApartAreKept() => Assert.Equal(2, new FCurve([K(12, 0), K(12.0101, 1)]).Keys.Count);

    /// <summary>The helpers make the keys they say; the eased and computed ones refuse the types they can't make.</summary>
    [Fact]
    public void TheKeyHelpersMakeTheirKeys()
    {
        Assert.Equal(new FCurveKey(0.5, 2, KeyInterpolation.Constant), FCurveKey.Constant(0.5, 2));
        Assert.Equal(new FCurveKey(0.5, 2, KeyInterpolation.Linear), FCurveKey.Linear(0.5, 2));
        Assert.Equal(new FCurveKey(0.5, 2, Left: HandleType.Vector, Right: HandleType.Auto), FCurveKey.Bezier(0.5, 2, HandleType.Vector, HandleType.Auto));
        Assert.Equal(new FCurveKey(0.5, 2), FCurveKey.Bezier(0.5, 2));
        Assert.Equal(new FCurveKey(0.5, 2, KeyInterpolation.Elastic, KeyEasing.InOut, Back: 3, Amplitude: 1.5, Period: 0.25),
            FCurveKey.Ease(0.5, 2, KeyInterpolation.Elastic, KeyEasing.InOut, 3, 1.5, 0.25));
        Assert.Equal(new FCurveKey(0.5, 2, KeyInterpolation.Sine), FCurveKey.Ease(0.5, 2, KeyInterpolation.Sine));
        Assert.Equal(new FCurveKey(0.5, 2, Left: HandleType.Free, Right: HandleType.Free, LeftTime: 0.25, LeftValue: 1, RightTime: 0.75, RightValue: 3),
            FCurveKey.Free(0.5, 2, (0.25, 1), (0.75, 3)));
        Assert.Equal(new FCurveKey(0.5, 2, Left: HandleType.Aligned, Right: HandleType.Aligned, LeftTime: 0.25, LeftValue: 1, RightTime: 0.75, RightValue: 3),
            FCurveKey.Aligned(0.5, 2, (0.25, 1), (0.75, 3)));
        foreach (var type in new[] { KeyInterpolation.Constant, KeyInterpolation.Linear, KeyInterpolation.Bezier })
            Assert.Throws<ArgumentException>(() => FCurveKey.Ease(0, 0, type));
        Assert.Throws<ArgumentException>(() => FCurveKey.Bezier(0, 0, HandleType.Free));
        Assert.Throws<ArgumentException>(() => FCurveKey.Bezier(0, 0, right: HandleType.Aligned));
        Assert.Equal((1.70158, 0.8, 4.1 / 60), (FCurveKey.DefaultBack, FCurveKey.DefaultAmplitude, FCurveKey.DefaultPeriod));
    }

    [Fact]
    public void StartAndEndAreTheFirstAndLastKeysTimes()
    {
        var curve = new FCurve([K(6, 0), K(15, 1), K(42, 0.5)]);
        Assert.Equal((F(6), F(42)), (curve.Start, curve.End));
    }

    /// <summary>Curves are equal by their content, whether their handles have been worked out or not; the keys are a copy of what was given.</summary>
    [Fact]
    public void CurvesAreEqualByContent()
    {
        FCurveKey[] keys = [K(0, 0), K(10, 1), K(25, -0.5)];
        var a = new FCurve(keys);
        var b = new FCurve(keys.ToList(), CurveExtrapolation.Constant, CurveSmoothing.ContinuousAcceleration);
        _ = a.Evaluate(0.1);
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
        Assert.NotEqual(a, b with { Extrapolation = CurveExtrapolation.Linear });
        Assert.NotEqual(a, b with { Smoothing = CurveSmoothing.None });
        Assert.NotEqual(a, new FCurve([K(0, 0), K(10, 1), K(25, -0.25)]));
        keys[1] = K(11, 1);
        Assert.Equal(b, a);
        Assert.Equal(F(10), a.Keys[1].Time);
    }

    /// <summary>A copy made with <c>with</c> works out its own handles: changing the keys, the smoothing or the extrapolation after the original was used.</summary>
    [Fact]
    public void ACopyWorksOutItsOwnHandles()
    {
        var curve = new FCurve([K(0, 0), K(10, 1), K(18, 0.25), K(31, 2)]);
        double[] times = [F(-6), F(4), F(14), F(25), F(37)];
        _ = curve.Handles;
        _ = curve.Evaluate(F(5));
        var copies = new[]
        {
            curve with { Keys = [K(0, 0), K(10, 1.5), K(18, 0.25), K(31, 2)] },
            curve with { Smoothing = CurveSmoothing.None },
            curve with { Extrapolation = CurveExtrapolation.Linear },
        };
        foreach (var copy in copies)
        {
            var fresh = new FCurve(copy.Keys, copy.Extrapolation, copy.Smoothing);
            Assert.Equal(fresh.Handles, copy.Handles);
            Assert.Equal(times.Select(fresh.Evaluate), times.Select(copy.Evaluate));
            Assert.NotEqual(times.Select(curve.Evaluate).Concat(curve.Handles.Select(h => h.LeftValue)), times.Select(copy.Evaluate).Concat(copy.Handles.Select(h => h.LeftValue)));
        }
    }

    /// <summary>Times are seconds: a key at a time gives its value there, and a linear curve is the line between its keys in seconds.</summary>
    [Fact]
    public void TimesAreSeconds()
    {
        var curve = new FCurve([FCurveKey.Linear(0.5, 1), FCurveKey.Linear(1.5, 3)]);
        Assert.Equal((1.0, 2.0, 3.0), (curve.Evaluate(0.5), curve.Evaluate(1), curve.Evaluate(1.5)));
    }
}
