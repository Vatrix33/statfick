using System.Reflection;
using System.Runtime.CompilerServices;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Scene;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;
using static Statventure.Tests.Scene.FCurveScenes;

namespace Statventure.Tests.Scene;

/// <summary>
/// F-curves as records (S2-1): <c>scene.curve</c> and <c>scene.key</c> through the binary and the JSON
/// form are the curves made in C#, keys written by hand with only time and value read with Blender's
/// defaults, and a curve loads from a pack and from a loose folder, refused there with the file named
/// when it breaks the rules (S2-3).
/// </summary>
public class FCurveRecordTests
{
    /// <summary>A curve with a value other than the default in every field of both record types (the fixture's).</summary>
    internal static FCurve Full() => new(
    [
        FCurveKey.Free(F(-6), -0.5, (F(-9), -0.25), (F(-2), 0.75)),
        FCurveKey.Aligned(F(4.5), 1.25, (F(2), 1.5), (F(8), 1)),
        new FCurveKey(F(10), 2, KeyInterpolation.Elastic, KeyEasing.InOut, HandleType.Vector, HandleType.Auto, Back: 2.5, Amplitude: 1.75, Period: F(3)),
        new FCurveKey(F(22), 0.5, KeyInterpolation.Back, KeyEasing.Out, HandleType.Free, HandleType.AutoClamped, LeftTime: F(20), LeftValue: 0.25, Back: -1),
        FCurveKey.Constant(F(30), 1),
    ], CurveExtrapolation.Linear, CurveSmoothing.None);

    /// <summary>Every curve of the reference scenes and the full one.</summary>
    static IEnumerable<FCurve> Curves() => All.SelectMany(s => s.Curves).Select(c => c.Curve).Append(Full());

    [Fact]
    public void TheFixtureIsRecorded() => RecordFixtures.Ensure("every-field", Full());

    /// <summary>Both forms give the curve back field for field, and the read curve's handles and values are the made one's, bit for bit.</summary>
    [Fact]
    public void CurvesRoundTripThroughBothForms()
    {
        foreach (var curve in Curves())
        {
            var times = TimesFor(curve);
            foreach (var read in new[] { RecordSerializer.FromBinary<FCurve>(RecordSerializer.ToBinary(curve)), RecordSerializer.FromJson<FCurve>(RecordSerializer.ToJson(curve)) })
            {
                RecordAssert.Equal(curve, read);
                Assert.Equal(curve, read);
                Assert.Equal(curve.Handles, read.Handles);
                Assert.Equal(times.Select(curve.Evaluate), times.Select(read.Evaluate));
            }
        }
    }

    /// <summary>
    /// Keys written by hand with only their time and value are Blender's new keys (Bezier, auto easing,
    /// auto-clamped both sides, back 1.70158, amplitude 0.8, period 4.1 frames) on a curve of Blender's
    /// defaults (constant extrapolation, continuous acceleration); the positions of free sides are read
    /// in seconds.
    /// </summary>
    [Fact]
    public void AbsentKeysAreBlendersDefaults()
    {
        var read = RecordSerializer.FromJson<FCurve>("""
            {"$types": {"scene.curve": 1, "scene.key": 1},
             "keys": [{"time": 0, "value": 1}, {"time": 0.5, "value": 2, "left": "free", "leftTime": 0.25, "leftValue": 1.5, "right": "vector"}]}
            """);
        Assert.Equal(new FCurve([new FCurveKey(0, 1), new FCurveKey(0.5, 2, Left: HandleType.Free, Right: HandleType.Vector, LeftTime: 0.25, LeftValue: 1.5)]), read);
        Assert.Equal((CurveExtrapolation.Constant, CurveSmoothing.ContinuousAcceleration), (read.Extrapolation, read.Smoothing));
        var key = read.Keys[0];
        Assert.Equal((KeyInterpolation.Bezier, KeyEasing.Auto, HandleType.AutoClamped, HandleType.AutoClamped, 1.70158, 0.8, 4.1 / 60),
            (key.Interpolation, key.Easing, key.Left, key.Right, key.Back, key.Amplitude, key.Period));
        Assert.Equal((0.25, 1.5), (read.Handles[1].LeftTime, read.Handles[1].LeftValue));
    }

    /// <summary>
    /// A curve that breaks the rules, which no constructor makes (the keys set past them), for its
    /// forms: <see cref="Full"/> with its second key at the first's time.
    /// </summary>
    static FCurve Broken()
    {
        var keys = Full().Keys.ToArray();
        keys[1] = keys[1] with { Time = keys[0].Time };
        var curve = (FCurve)RuntimeHelpers.GetUninitializedObject(typeof(FCurve));
        typeof(FCurve).GetField("_keys", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(curve, keys);
        return curve;
    }

    /// <summary>
    /// A curve loads from a pack's record entry and from a loose JSON file; one that breaks the rules is
    /// refused with the pack and entry, or the file, named and the rule's message, the
    /// <see cref="ArgumentException"/> inside.
    /// </summary>
    [Fact]
    public void CurvesLoadFromPacksAndLooseFilesAndBrokenOnesAreRefusedNamingTheFile()
    {
        const string expected = "not a valid F-curve: key 1 (at -0.1 s): keys must be in increasing time order at least 0.01 frame (1/6000 s) apart, and key 0 is at -0.1 s";
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("world/curves/full", Full());
        writer.AddRecord("world/curves/broken", Broken());
        writer.Write(folder["world.pak"]);
        using (var packed = new VirtualFileSystem([Mount.Pack(folder["world.pak"])]))
        {
            RecordAssert.Equal(Full(), FCurve.Load(packed, "world/curves/full"));
            var problem = Assert.Throws<DataFileException>(() => FCurve.Load(packed, "world/curves/broken"));
            Assert.Equal((Path.GetFullPath(folder["world.pak"]), "world/curves/broken", expected), (problem.File, problem.Entry, problem.Problem));
            Assert.IsType<ArgumentException>(problem.InnerException);
        }
        folder.Write("dev/world/curves/full.json", RecordSerializer.ToJson(Full()));
        var file = folder.Write("dev/world/curves/broken.json", RecordSerializer.ToJson(Broken()));
        using var loose = new VirtualFileSystem([Mount.Folder(folder["dev"])]);
        RecordAssert.Equal(Full(), FCurve.Load(loose, "world/curves/full"));
        var looseProblem = Assert.Throws<DataFileException>(() => FCurve.Load(loose, "world/curves/broken"));
        Assert.Equal((Path.GetFullPath(file), (string?)null, expected), (looseProblem.File, looseProblem.Entry, looseProblem.Problem));
    }
}
