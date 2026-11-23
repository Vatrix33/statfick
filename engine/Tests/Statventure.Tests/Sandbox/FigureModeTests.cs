using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Statventure.Animation;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Platform;
using Statventure.Render3D;
using Statventure.Sandbox.Figure;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Render3D;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Sandbox;

/// <summary>The figure mode's files (GM2 step 3a): the humanoid frame's sample, the engine's test figure and its gear, the look files.</summary>
static class FigureFiles
{
    static string ProjectDirectory([CallerFilePath] string path = "") => Path.GetDirectoryName(Path.GetDirectoryName(path)!)!;

    public static string Models => Path.Combine(ProjectDirectory(), "Data", "Models");
    /// <summary>The tests' fonts (Lato among them, the caption's).</summary>
    public static string Fonts => Path.Combine(ProjectDirectory(), "Data", "Fonts");
    public static string Reference => Path.Combine(ProjectDirectory(), "Reference");
    /// <summary>The frame's sample (Tools/Models/humanoid/sample.py, made by make-humanoid.sh build): two clips, bow (0.75 s) and sway (1 s).</summary>
    public static string Sample => Path.Combine(Models, "humanoid", "sample.glb");
    /// <summary>The look files Tools/Models/humanoid/look.py writes.</summary>
    public static string Looks => Path.GetFullPath(Path.Combine(ProjectDirectory(), "..", "..", "Tools", "Models", "humanoid", "looks"));

    public static FigureLook Today => FigureLook.Load(Path.Combine(Looks, "today.json"));

    /// <summary>A look whose every number is its own (no two alike), so a number read under another key shows.</summary>
    public const string TestLookJson = """
        {
          "format": "statventure.look", "version": 1, "name": "test", "background": "#2c2a33",
          "sun": { "direction": [-0.8, 1.2, 0.9], "strength": 2.6, "angle": 4 },
          "style": {
            "ink": "#2a1838", "shadowMix": 0.55, "bandEdge": 0.42, "outlineWidth": 2.5, "innerWidth": 1.5, "creaseAngle": 100,
            "hatchSpacing": 5, "hatchWidth": 1.25, "hatchAngle": 50, "hatchInk": 0.5, "glowWide": 7, "glowCore": 2, "glowBoost": 1.8
          },
          "palette": { "cloth": "#3f6f5a", "skin": "#d0a080", "trousers": "#5a4a3a" },
          "shadows": { "cloth": "#1e3a30" }
        }
        """;

    public static FigureLook Test => FigureLook.Parse(TestLookJson, "test");

    /// <summary>A playing time: <paramref name="at"/> seconds into the named clip.</summary>
    public static double Into(string body, string clip, double at)
    {
        var clips = new FigureClips(AnimationClip.AllOf(body));
        var index = clips.Clips.Select(c => c.Name).ToList().IndexOf(clip);
        Assert.True(index >= 0, $"{body} has no clip {clip}");
        return clips.Starts[index] + at;
    }
}

/// <summary>
/// The look file (Tools/Models/humanoid/look.py; the sandbox's Figure/FigureLook.cs): today's look is the engine's own,
/// every number of CelStyle is in it and read from its own key, and a file that isn't one is refused.
/// </summary>
public class FigureLookTests
{
    [Fact]
    public void TodaysLookIsTheEnginesLookUnchanged()
    {
        var today = FigureFiles.Today;
        Assert.Equal("today", today.Name);
        Assert.Equal(CelStyle.Default, today.Style);
        // The lizardman test's sun (Scenes3D.Lizardman), SunLight's default angle.
        Assert.Equal(new SunLight(new Vector3(1, 1.1f, 0.7f), 3) { Angle = 3 }, today.Sun);
        Assert.Equal("#b8b0a0", today.Background);
        Assert.Empty(today.Colours);
        Assert.Empty(today.Shadows);
    }

    /// <summary>CelStyle's numbers by their properties, in camelCase (every one a look can set: Q7, today's rules, any numbers).</summary>
    static List<string> CelStyleNumbers() => typeof(CelStyle).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .OrderBy(p => p.MetadataToken).Select(p => char.ToLowerInvariant(p.Name[0]) + p.Name[1..]).ToList();

    [Fact]
    public void TheLookHoldsEveryNumberOfTheStyle()
    {
        Assert.Equal(CelStyleNumbers(), FigureLook.StyleKeys);
        using var today = JsonDocument.Parse(File.ReadAllText(Path.Combine(FigureFiles.Looks, "today.json")));
        Assert.Equal(FigureLook.StyleKeys, today.RootElement.GetProperty("style").EnumerateObject().Select(p => p.Name).ToList());
        Assert.Equal(["direction", "strength", "angle"], today.RootElement.GetProperty("sun").EnumerateObject().Select(p => p.Name).ToList());
    }

    [Fact]
    public void EveryNumberIsReadFromItsOwnKey()
    {
        var look = FigureFiles.Test;
        using var json = JsonDocument.Parse(FigureFiles.TestLookJson);
        var style = json.RootElement.GetProperty("style");
        foreach (var property in typeof(CelStyle).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var key = char.ToLowerInvariant(property.Name[0]) + property.Name[1..];
            object expected = key == "ink" ? Colors.FromHex(style.GetProperty(key).GetString()!) : style.GetProperty(key).GetSingle();
            Assert.Equal(expected, property.GetValue(look.Style));
        }
        Assert.Equal(new SunLight(new Vector3(-0.8f, 1.2f, 0.9f), 2.6f) { Angle = 4 }, look.Sun);
        Assert.Equal("#2c2a33", look.Background);
        Assert.Equal(Colors.LinearFromHex("#2c2a33"), look.BackgroundLinear);
        var palette = look.Palette;
        Assert.Equal(["cloth", "skin", "trousers"], palette.Slots.Keys.Order(StringComparer.Ordinal));
        Assert.Equal(new PaletteTone(Colors.LinearFromHex("#3f6f5a"), Colors.LinearFromHex("#1e3a30")), palette.Slots["cloth"]);
        Assert.Equal(new PaletteTone(Colors.LinearFromHex("#d0a080")), palette.Slots["skin"]);
        Assert.Equal(new PaletteTone(Colors.LinearFromHex("#5a4a3a")), palette.Slots["trousers"]);
    }

    /// <summary>Each key taken out, an unknown one put in, and values of the wrong kind: refused, naming what is wrong.</summary>
    public static TheoryData<string, string, string> Broken()
    {
        var data = new TheoryData<string, string, string>();
        var test = FigureFiles.TestLookJson;
        foreach (var key in new[] { "format", "version", "name", "background", "sun", "style", "palette", "shadows" })
            data.Add($"no {key}", Without(test, null, key), key == "format" || key == "version" ? key : $"missing {key}");
        foreach (var key in new[] { "direction", "strength", "angle" })
            data.Add($"no sun.{key}", Without(test, "sun", key), $"missing {key}");
        foreach (var key in FigureLook.StyleKeys)
            data.Add($"no style.{key}", Without(test, "style", key), $"missing {key}");
        data.Add("an unknown key", test.Replace("\"name\": \"test\"", "\"name\": \"test\", \"lines\": 2"), "unknown lines");
        data.Add("an unknown style key", test.Replace("\"hatchInk\": 0.5", "\"hatchInk\": 0.5, \"hatchCount\": 2"), "unknown hatchCount");
        data.Add("another format", test.Replace("statventure.look", "statventure.style"), "not a statventure.look file");
        data.Add("another version", test.Replace("\"version\": 1", "\"version\": 2"), "not a statventure.look file");
        data.Add("a number as text", test.Replace("\"bandEdge\": 0.42", "\"bandEdge\": \"0.42\""), "bandEdge must be a number");
        data.Add("a colour without its hash", test.Replace("\"#2a1838\"", "\"12a1838\""), "ink must be an sRGB colour");
        data.Add("a short colour", test.Replace("\"#2c2a33\"", "\"#2c2\""), "background must be an sRGB colour");
        data.Add("a palette colour", test.Replace("\"#d0a080\"", "\"tan\""), "skin must be an sRGB colour");
        data.Add("a shadow colour", test.Replace("\"#1e3a30\"", "\"#1e3a3g\""), "cloth must be an sRGB colour");
        data.Add("two numbers for the sun", test.Replace("[-0.8, 1.2, 0.9]", "[-0.8, 1.2]"), "three numbers");
        data.Add("no sun at all", test.Replace("[-0.8, 1.2, 0.9]", "[0, 0, 0]"), "must not be 0");
        data.Add("a negative strength", test.Replace("\"strength\": 2.6", "\"strength\": -1"), "0 or more");
        data.Add("a negative angle", test.Replace("\"angle\": 4", "\"angle\": -4"), "0 or more");
        data.Add("an empty name", test.Replace("\"name\": \"test\"", "\"name\": \"\""), "name must be a text");
        data.Add("a palette that isn't one", test.Replace("\"shadows\": { \"cloth\": \"#1e3a30\" }", "\"shadows\": []"), "shadows must be an object");
        return data;
    }

    static string Without(string json, string? within, string key)
    {
        var node = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        var holder = within is null ? node : node[within]!.AsObject();
        Assert.True(holder.Remove(key), key);
        return node.ToJsonString();
    }

    [Theory]
    [MemberData(nameof(Broken))]
    public void ALookThatIsntOneIsRefused(string what, string json, string message)
    {
        var error = Assert.Throws<InvalidDataException>(() => FigureLook.Parse(json, what));
        Assert.Contains(message, error.Message);
    }
}

/// <summary>
/// The figure mode's views and its clips (Figure/FigureView.cs): the same views as Blender's renders (render.py's
/// cameras for the sample, Reference/figure-framing*-scenes.json, made by make-humanoid.sh framing), and every clip in
/// turn on one clock.
/// </summary>
public class FigureFramingTests
{
    [Fact]
    public void TheViewsTurnTheFigureAsRendersDo()
    {
        Assert.Equal(0, FigureFraming.TurnOf("front"));
        Assert.Equal(45, FigureFraming.TurnOf("three-quarter"));
        Assert.Equal(90, FigureFraming.TurnOf("side"));
        Assert.Equal(180, FigureFraming.TurnOf("back"));
        Assert.Equal(45, FigureFraming.TurnOf("close-up"));
        Assert.Equal(-30.5f, FigureFraming.TurnOf("-30.5"));
        Assert.Throws<ArgumentException>(() => FigureFraming.TurnOf("above"));
        Assert.Equal(["front", "three-quarter", "side", "back", "close-up"], FigureFraming.Views);
    }

    public static TheoryData<string, string> RenderedViews()
    {
        var data = new TheoryData<string, string>();
        foreach (var file in new[] { "figure-framing", "figure-framing-perspective", "figure-framing-narrow", "figure-framing-far" })
        {
            using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(FigureFiles.Reference, file + "-scenes.json")));
            foreach (var scene in json.RootElement.GetProperty("scenes").EnumerateArray())
                data.Add(file, scene.GetProperty("name").GetString()!);
        }
        return data;
    }

    /// <summary>The sample framed as make-humanoid.sh framing asked render.py to frame it (its four command lines).</summary>
    static FigureSpec SpecOf(string file, string view) => file switch
    {
        "figure-framing" => new FigureSpec { Body = FigureFiles.Sample, Look = FigureFiles.Today, View = view, Width = 300, Height = 400 },
        "figure-framing-perspective" => new FigureSpec
        {
            Body = FigureFiles.Sample, Look = FigureFiles.Today, View = view, Width = 320, Height = 240, Perspective = 30, Focus = "upper", Elevation = 12, Margin = 0.1f,
        },
        // So narrow the figure's width frames it.
        "figure-framing-narrow" => new FigureSpec { Body = FigureFiles.Sample, Look = FigureFiles.Today, View = view, Width = 100, Height = 800 },
        // So far the far plane moves out.
        _ => new FigureSpec { Body = FigureFiles.Sample, Look = FigureFiles.Today, View = view, Width = 320, Height = 240, Perspective = 20, Extent = 30 },
    };

    [Theory]
    [MemberData(nameof(RenderedViews))]
    public void TheFigureModeFramesAViewAsRendersFrameIt(string file, string name)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(FigureFiles.Reference, file + "-scenes.json")));
        var scene = json.RootElement.GetProperty("scenes").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
        var view = name[(file.Length + 1)..];
        var spec = SpecOf(file, view);
        Assert.Equal((spec.Width, spec.Height), (scene.GetProperty("width").GetInt32(), scene.GetProperty("height").GetInt32()));
        var model = ModelData.Load(spec.Body);
        var camera = FigureFraming.CameraFor(spec, view, FigureFraming.RestPoints(model, Matrix4x4.Identity), FigureFraming.FocusPoint(model, spec.Focus ?? "head"));
        var expected = scene.GetProperty("camera");
        static Vector3 V(JsonElement e) => new(e[0].GetSingle(), e[1].GetSingle(), e[2].GetSingle());
        Assert.Equal(expected.GetProperty("projection").GetString(), camera.Projection.ToString().ToLowerInvariant());
        Assert.True(Vector3.Distance(V(expected.GetProperty("target")), camera.Target) < 1e-4f, $"{name}: target {camera.Target}");
        Assert.True(Vector3.Distance(V(expected.GetProperty("position")), camera.Position) < 1e-4f, $"{name}: position {camera.Position}");
        Assert.Equal(Vector3.UnitY, camera.Up);
        Assert.Equal(expected.GetProperty("orthographicHeight").GetSingle(), camera.OrthographicHeight, 1e-4f);
        Assert.Equal(expected.GetProperty("fieldOfViewDegrees").GetSingle(), camera.VerticalFieldOfView * 180 / MathF.PI, 1e-3f);
        Assert.Equal(expected.GetProperty("near").GetSingle(), camera.Near, 1e-6f);
        Assert.Equal(expected.GetProperty("far").GetSingle(), camera.Far, 1e-4f);
        // The models placed turned by the view's turn.
        var matrix = scene.GetProperty("models")[0].GetProperty("matrix");
        var turn = Matrix4x4.CreateRotationY(FigureFraming.TurnOf(view) * MathF.PI / 180);
        Assert.Equal(turn.M13, matrix[0][2].GetSingle(), 1e-5f);
        Assert.Equal(turn.M31, matrix[2][0].GetSingle(), 1e-5f);
    }

    [Fact]
    public void AFigureWithoutTheFocusBoneIsFramedAtItsTop()
    {
        var model = ModelData.Load(FigureFiles.Sample);
        Assert.Null(FigureFraming.FocusPoint(model, "head"));
        // The upper bone has no child: its middle is 0.1 m up its own axis from its joint (0.9 m up).
        Assert.True(Vector3.Distance(new Vector3(0, 1.0f, 0), FigureFraming.FocusPoint(model, "upper")!.Value) < 1e-5f);
        // The lower bone's middle is halfway to the upper's joint.
        Assert.True(Vector3.Distance(new Vector3(0, 0.45f, 0), FigureFraming.FocusPoint(model, "lower")!.Value) < 1e-5f);
    }

    [Fact]
    public void TheClipsPlayInTurnOnOneClock()
    {
        var model = ModelData.Load(FigureFiles.Sample);
        var clips = new FigureClips(AnimationClip.AllOf(model));
        Assert.Equal(["bow", "sway"], clips.Clips.Select(c => c.Name));
        Assert.Equal([0, 0.75], clips.Starts.Select(s => Math.Round(s, 6)));
        Assert.Equal(1.75, clips.Cycle, 6);
        Assert.Equal((0, 0.0), clips.At(0));
        // At the meeting the earlier clip at its end, as the stack's layer picks it.
        var (atEnd, endTime) = clips.At(0.75);
        Assert.Equal((0, 0.75), (atEnd, Math.Round(endTime, 6)));
        // Within the stack's epsilon past its end, still the earlier clip, held at its end.
        Assert.Equal((0, 0.75), clips.At(0.75 + 1e-7));
        var (next, nextTime) = clips.At(0.8);
        Assert.Equal((1, 0.05), (next, Math.Round(nextTime, 6)));
        var (round, roundTime) = clips.At(1.75 + 0.1);
        Assert.Equal((0, 0.1), (round, Math.Round(roundTime, 6)));
        var (before, beforeTime) = clips.At(-0.25);
        Assert.Equal((1, 0.75), (before, Math.Round(beforeTime, 6)));
        Assert.Equal(1.6, clips.Wrap(3.35), 6);
        Assert.Equal((-1, 0.0), new FigureClips([]).At(1));
        Assert.Equal(0, new FigureClips([]).Wrap(5));
        // A still clip (no keys after 0) takes a frame on the cycle.
        var still = new FigureClips([new AnimationClip("still", []), clips.Clips[0]]);
        Assert.Equal([0, 1.0 / 60], still.Starts);
        Assert.Equal(1.0 / 60 + 0.75, still.Cycle, 9);
        Assert.Equal((0, 0.0), still.At(1.0 / 120));
        // The layer plays each clip at its own time: the pose at a playing time is the clip's at its time in it.
        var skeleton = new Skeleton(model);
        var stack = new AnimationStack(skeleton);
        stack.Layers.Add(clips.Layer(skeleton));
        var buffer = new PoseBuffer(skeleton);
        foreach (var t in new[] { 0.1, 0.37, 0.9, 1.3, 1.74 })
        {
            stack.Evaluate(t, buffer);
            var (clip, at) = clips.At(t);
            var expected = model.WorldMatrices(new ClipBinding(clips.Clips[clip], skeleton).Pose(at));
            var actual = model.WorldMatrices(buffer.ToPose());
            for (var n = 0; n < expected.Length; n++)
                Assert.True((expected[n] - actual[n]).Translation.Length() < 1e-6f && Math.Abs(expected[n].M11 - actual[n].M11) < 1e-6f
                    && Math.Abs(expected[n].M12 - actual[n].M12) < 1e-6f && Math.Abs(expected[n].M23 - actual[n].M23) < 1e-6f, $"node {n} at {t}");
        }
    }

    /// <summary>
    /// A play list (--play, GM2 step 4b): its turns in its order, each its clip as many times in a row as it says (round the
    /// clip as the strip's repeats play it, the turn's very end its clip's end), a clip laid over another replacing only
    /// what it keys for the turn's length and nothing outside it; a name the library hasn't is refused.
    /// </summary>
    [Fact]
    public void APlayListPlaysItsTurnsInItsOrder()
    {
        var model = ModelData.Load(FigureFiles.Sample);
        var library = AnimationClip.AllOf(model);
        var clips = new FigureClips(library, FigurePlay.Parse("sway:2,bow+sway,bow"), "sample.glb");
        Assert.Equal(["sway", "bow", "bow"], clips.Clips.Select(c => c.Name));
        Assert.Equal([2, 1, 1], clips.Repeats);
        Assert.Equal([null, "sway", null], clips.Overs.Select(o => o?.Name));
        Assert.Equal(["sway", "bow+sway", "bow"], Enumerable.Range(0, 3).Select(clips.NameOf));
        Assert.Equal([0, 2, 2.75], clips.Starts.Select(s => Math.Round(s, 6)));
        Assert.Equal(3.5, clips.Cycle, 6);
        Assert.Equal((0, 0.25), Rounded(clips.At(0.25)));
        // The second time round: the clip from its start again (a whole round in, as Blender's strip counts), its end at the turn's.
        Assert.Equal((0, 0.25), Rounded(clips.At(1.25)));
        Assert.Equal((0, 0.0), Rounded(clips.At(1.0)));
        Assert.Equal((0, 1.0), Rounded(clips.At(2.0)));
        Assert.Equal((1, 0.1), Rounded(clips.At(2.1)));
        Assert.Equal((2, 0.5), Rounded(clips.At(3.25)));
        Assert.Equal((0, 0.5), Rounded(clips.At(3.5 + 0.5)));
        // Without a list, every clip once; a name the library hasn't is refused, laid over or not.
        Assert.Equal(["bow", "sway"], new FigureClips(library, null, "sample.glb").Clips.Select(c => c.Name));
        Assert.Contains("no clip nod in sample.glb", Assert.Throws<ArgumentException>(() => new FigureClips(library, FigurePlay.Parse("sway,nod"), "sample.glb")).Message);
        Assert.Contains("no clip nod in sample.glb", Assert.Throws<ArgumentException>(() => new FigureClips(library, FigurePlay.Parse("sway+nod"), "sample.glb")).Message);

        // The stack: the turn's clip at its time; over the second turn sway turns the upper body (it keys "upper" alone) while
        // bow's dip of the hips ("lower"'s place) goes on beneath; outside its turn (the third, bow alone) it acts nothing.
        var skeleton = new Skeleton(model);
        var stack = new AnimationStack(skeleton);
        stack.Layers.Add(clips.Layer(skeleton));
        var over = clips.OverLayer(skeleton);
        Assert.NotNull(over);
        stack.Layers.Add(over);
        Assert.Null(new FigureClips(library).OverLayer(skeleton));
        var buffer = new PoseBuffer(skeleton);
        var (lower, upper) = (skeleton.Find("lower"), skeleton.Find("upper"));
        foreach (var t in new[] { 0.3, 1.3, 1.999, 2.1, 2.4, 2.7, 2.8, 3.1, 3.4 })
        {
            stack.Evaluate(t, buffer);
            var actual = model.WorldMatrices(buffer.ToPose());
            var (turn, at) = clips.At(t);
            var expected = model.WorldMatrices(new ClipBinding(clips.Clips[turn], skeleton).Pose(at));
            if (clips.Overs[turn] is { } laid)
            {
                var top = model.WorldMatrices(new ClipBinding(laid, skeleton).Pose((t - clips.Starts[turn]) % laid.Duration));
                // (its place is lower's, which bow moves: the turn alone is compared)
                Assert.True(SameTurn(top[upper], actual[upper]) && !SameTurn(expected[upper], actual[upper]), $"upper at {t}: not sway's");
                Assert.True(Same(expected[lower], actual[lower]), $"lower at {t}: not bow's");
            }
            else
                for (var n = 0; n < expected.Length; n++)
                    Assert.True(Same(expected[n], actual[n]), $"node {n} at {t}");
        }

        static (int, double) Rounded((int Clip, double Time) at) => (at.Clip, Math.Round(at.Time, 6));
        static bool Same(Matrix4x4 a, Matrix4x4 b) => (a - b).Translation.Length() < 1e-6f && SameTurn(a, b);
        static bool SameTurn(Matrix4x4 a, Matrix4x4 b)
        {
            for (var r = 0; r < 3; r++)
                for (var c = 0; c < 3; c++)
                    if (Math.Abs(a[r, c] - b[r, c]) > 1e-6f)
                        return false;
            return true;
        }
    }
}

/// <summary>
/// The figure mode's picture against Blender's (GM2-P14): the frame's sample and the engine's test figure with its gear,
/// in today's look and in a look of numbers of its own, posed by their clips, in four views. The scenes are written as
/// Blender's harness reads them (Reference/figure-scenes.json; make-blender-references.sh renders them into
/// Golden/r3d-figure-*.png and -ink.png), and the figure mode's view is held to them within M6's limits
/// (<see cref="ReferenceLimits.M6"/>): its shading (lines off) by <see cref="Render3DReferenceTests.Compare"/>, its
/// lines by <see cref="Render3DReferenceTests.CompareInk"/>.
/// </summary>
[Collection("GPU")]
public class FigureReferenceTests(GpuFixture gpu)
{
    public sealed record FigureScene(string Name, FigureSpec Spec, double Time);

    public static IReadOnlyList<FigureScene> Scenes { get; } =
    [
        new("figure-today", new FigureSpec { Body = FigureFiles.Sample, Look = FigureFiles.Today, View = "three-quarter", Width = 320, Height = 400 },
            FigureFiles.Into(FigureFiles.Sample, "sway", 0.25)),
        new("figure-test-side", new FigureSpec { Body = FigureFiles.Sample, Look = FigureFiles.Test, View = "side", Width = 320, Height = 400 },
            FigureFiles.Into(FigureFiles.Sample, "bow", 22 / 60.0)),
        new("figure-test-close-up", new FigureSpec
        {
            Body = FigureFiles.Sample, Look = FigureFiles.Test, View = "close-up", Width = 320, Height = 240, Perspective = 30, Focus = "upper", Elevation = 12,
        }, FigureFiles.Into(FigureFiles.Sample, "sway", 0.75)),
        // In front: in the three-quarter view a sliver of the lit thigh, under a pixel wide, shows between the leg's
        // shadow and the tabard, shaded once per pixel under MSAA where Blender shades each sample (one edge pixel
        // 37 levels off; Docs/Renderer-3D.md, "Freestyle's limits seen": test cameras avoid such slivers).
        new("figure-gear", new FigureSpec
        {
            Body = Path.Combine(FigureFiles.Models, "figure.glb"), Garments = [Path.Combine(FigureFiles.Models, "tabard.glb")],
            Items = [(Path.Combine(FigureFiles.Models, "sword.glb"), "socket.hand.R")], Look = FigureFiles.Test, View = "front", Width = 320, Height = 400,
        }, FigureFiles.Into(Path.Combine(FigureFiles.Models, "figure.glb"), "swing", 0.2)),
    ];

    public static TheoryData<string> SceneNames => new(Scenes.Select(s => s.Name));

    internal static string SceneFile(GpuDevice device) =>
        FigureView.SceneFile(Scenes.Select(s =>
        {
            using var figure = new FigureView(device, s.Spec);
            figure.Seek(s.Time);
            return figure.SceneOf(s.Name, FigureFiles.Models);
        }).ToList(), FigureFiles.Models, FigureFiles.Reference);

    /// <summary>
    /// The scene file Blender's references were rendered from is the one the figure mode writes now: a change to the
    /// scenes or to how the figure mode writes one (<see cref="FigureView.SceneOf"/>) fails here until the file is
    /// written again (STATVENTURE_FIGURE_SCENES=write) and the references rendered again from it
    /// (make-blender-references.sh figure-…). Not written by every run, as r3d-scenes.json is: a mutation run would leave
    /// its broken file behind for the next.
    /// </summary>
    [Fact]
    public void TheSceneFileIsUpToDate()
    {
        var path = Path.Combine(FigureFiles.Reference, "figure-scenes.json");
        var text = SceneFile(gpu.Device);
        if (Environment.GetEnvironmentVariable("STATVENTURE_FIGURE_SCENES") == "write")
            File.WriteAllText(path, text);
        Assert.True(File.Exists(path) && File.ReadAllText(path) == text,
            "Reference/figure-scenes.json isn't what the figure mode writes: run with STATVENTURE_FIGURE_SCENES=write, then make-blender-references.sh with the figure scenes' names");
        Assert.Equal(Scenes.Count, JsonDocument.Parse(text).RootElement.GetProperty("scenes").GetArrayLength());
    }

    /// <summary>The figure mode's view drawn offscreen as the view draws it: its items, camera, sun, style and background.</summary>
    internal static RgbaImage Render(GpuDevice device, FigureView figure, bool lines = true, bool inkOnly = false)
    {
        var view = figure.View;
        var style = lines ? view.Style : view.Style with { OutlineWidth = 0, InnerWidth = 0 };
        int w = figure.Spec.Width, h = figure.Spec.Height;
        using var target = new RenderTarget3D(device, w, h);
        using var renderer = new Renderer3D(device) { InkCoverageOnly = inkOnly };
        var frame = device.BeginFrame();
        renderer.Render(frame, target, view.Camera, view.Items, view.Background ?? Vector4.Zero, view.Sun, style, 1, view.Time, view.Particles);
        device.EndFrame(frame);
        device.WaitIdle();
        return new RgbaImage(w, h, target.Resolved.ReadPixels());
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void TheFigureModeShadesWhatBlenderShaded(string name)
    {
        var scene = Scenes.Single(s => s.Name == name);
        var reference = Golden.GoldenPath("r3d-" + name);
        Assert.True(File.Exists(reference), $"no Blender reference for {name}: run the tests, then Tools/Scripts/make-blender-references.sh {name}");
        using var figure = new FigureView(gpu.Device, scene.Spec);
        figure.Seek(scene.Time);
        var actual = Render(gpu.Device, figure, lines: false);
        var problems = Render3DReferenceTests.Compare(ImageFile.Load(reference), actual);
        var actualPath = Golden.ActualPath("r3d-" + name);
        if (problems.Length > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        Assert.True(problems.Length == 0, $"{name}: {problems}; render in {actualPath}");
    }

    [Theory]
    [MemberData(nameof(SceneNames))]
    public void TheFigureModeInksWhatFreestyleInked(string name)
    {
        var scene = Scenes.Single(s => s.Name == name);
        var reference = Golden.GoldenPath("r3d-" + name + "-ink");
        Assert.True(File.Exists(reference), $"no Freestyle reference for {name}: run the tests, then Tools/Scripts/make-blender-references.sh {name}");
        using var figure = new FigureView(gpu.Device, scene.Spec);
        figure.Seek(scene.Time);
        var actual = Render(gpu.Device, figure, inkOnly: true);
        var problems = Render3DReferenceTests.CompareInk(Render3DReferenceTests.InkCoverage(ImageFile.Load(reference), alpha: true),
            Render3DReferenceTests.InkCoverage(actual, alpha: false), actual.Width);
        var actualPath = Golden.ActualPath("r3d-" + name + "-ink");
        if (problems.Length > 0 || Environment.GetEnvironmentVariable("STATVENTURE_KEEP_RENDERS") == "1")
            ImageFile.SavePng(actualPath, actual);
        Assert.True(problems.Length == 0, $"{name}: {problems}; lines in {actualPath}");
    }

    /// <summary>What the view holds is the look's and the scene's: the figure turned by the view, coloured by the look's palette.</summary>
    [Fact]
    public void TheViewTakesTheLookAndTheView()
    {
        var scene = Scenes.Single(s => s.Name == "figure-test-side");
        using var figure = new FigureView(gpu.Device, scene.Spec);
        Assert.Equal(FigureFiles.Test.Style, figure.View.Style);
        Assert.Equal(FigureFiles.Test.Sun, figure.View.Sun);
        Assert.Equal(Colors.LinearFromHex("#2c2a33"), figure.View.Background);
        Assert.Equal(FigureFiles.Test.Palette.Slots, figure.Actor.Palette!.Slots);
        Assert.Equal("side", figure.ViewName);
        Assert.Equal(90, figure.Turn);
        Assert.Equal(0, figure.Scene.Rate);
        figure.Turn = 105;
        figure.Seek(scene.Time);
        var world = figure.Actor.Node!.World;
        Assert.Equal(Matrix4x4.CreateRotationY(105 * MathF.PI / 180).M13, world.M13, 1e-6f);
        figure.ShowView("back");
        Assert.Equal(180, figure.Turn);
        Assert.Equal("back", figure.ViewName);
        // Played round: a time past the cycle poses the figure where the time in the cycle does.
        figure.Seek(scene.Time + 2 * figure.Clips.Cycle);
        Assert.Equal(scene.Time, figure.Actor.Animator.Time, 9);
        Assert.Equal(scene.Time + 2 * figure.Clips.Cycle, figure.Time, 9);
        Assert.Equal("bow 0.367 s", figure.Describe());
        // The scene at another device scale: the lines' widths scaled, as Blender's harness takes them.
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(figure.SceneOf("scaled", FigureFiles.Models, 2)));
        var style = json.RootElement.GetProperty("style");
        Assert.Equal((5f, 3f, 2f), (style.GetProperty("outlineWidth").GetSingle(), style.GetProperty("innerWidth").GetSingle(), json.RootElement.GetProperty("scale").GetSingle()));
        // A close-up's focus that the figure doesn't have is refused.
        var error = Assert.Throws<ArgumentException>(() => new FigureView(gpu.Device, scene.Spec with { View = "close-up", Focus = "nose" }));
        Assert.Contains("no bone nose", error.Message);
        // Without a focus a close-up frames the head (the sample has none: its top, as render.py frames it).
        var spec = scene.Spec with { View = "close-up" };
        using var closeUp = new FigureView(gpu.Device, spec);
        var model = ModelData.Load(FigureFiles.Sample);
        var expected = FigureFraming.CameraFor(spec, "close-up", FigureFraming.RestPoints(model, Matrix4x4.Identity), FigureFraming.FocusPoint(model, "head"));
        Assert.Equal((expected.Target, expected.OrthographicHeight), (closeUp.View.Camera.Target, closeUp.View.Camera.OrthographicHeight));
    }

    /// <summary>
    /// The view plays the spec's play list (--play): its turns' clips in their order on its clock, the clip laid over a turn
    /// on the layer above, named so in the caption; a play list naming a clip the library hasn't is refused.
    /// </summary>
    [Fact]
    public void TheViewPlaysItsPlayList()
    {
        var spec = new FigureSpec { Body = FigureFiles.Sample, Look = FigureFiles.Today, Width = 320, Height = 400, Play = FigurePlay.Parse("sway:2,bow+sway,bow") };
        using var figure = new FigureView(gpu.Device, spec);
        Assert.Equal(3.5, figure.Clips.Cycle, 6);
        var model = ModelData.Load(FigureFiles.Sample);
        var skeleton = new Skeleton(model);
        var (lower, upper) = (skeleton.Find("lower"), skeleton.Find("upper"));
        figure.Seek(2.1);
        Assert.Equal("bow+sway 0.100 s", figure.Describe());
        var posed = model.WorldMatrices(figure.Actor.Animator.Pose());
        var (sway, bow) = (model.WorldMatrices(new ClipBinding(figure.Clips.Overs[1]!, skeleton).Pose(0.1)), model.WorldMatrices(new ClipBinding(figure.Clips.Clips[1], skeleton).Pose(0.1)));
        Assert.True(Math.Abs(posed[upper].M12 - sway[upper].M12) < 1e-6f && Math.Abs(posed[upper].M12 - bow[upper].M12) > 1e-4f, "the upper body isn't sway's");
        Assert.True(Vector3.Distance(posed[lower].Translation, bow[lower].Translation) < 1e-6f, "the hips aren't bow's");
        figure.Seek(1.25);
        Assert.Equal("sway 0.250 s", figure.Describe());
        Assert.Contains("no clip nod in sample.glb", Assert.Throws<ArgumentException>(() => new FigureView(gpu.Device, spec with { Play = FigurePlay.Parse("bow,nod") })).Message);
    }

    /// <summary>
    /// The figure mode stands its figure by its soles on its node (GM2-P22), as the game's figures stand: the actor's ground
    /// is the lowest point at rest of the body and its garments (the human man's booted soles, 24.0 mm under his origin;
    /// the sword he holds left out), and the view frames the figure as it stands (the camera raised with it: the same
    /// picture), the scene file it writes for Blender placing the models, the items and the hatching's anchor raised alike.
    /// A body whose origin is its soles' level (the frame's sample) stands where it stood.
    /// </summary>
    [Fact]
    public void TheFigureStandsByItsSoles()
    {
        var spec = new FigureSpec
        {
            Body = Humanoid.HumanoidFiles.Body("male"), Garments = [Humanoid.HumanoidFiles.ChainShirt("male")], Items = [(Humanoid.HumanoidFiles.Sword, "socket.hand.R")],
            Look = FigureFiles.Today, View = "three-quarter", Width = 300, Height = 400,
        };
        using var figure = new FigureView(gpu.Device, spec);
        Assert.Equal(-0.023997f, figure.Actor.Ground, 6);
        // Its skeleton wears its garments (GM2-P37: they meet the ground with its skin), its items not.
        Assert.Equal(figure.Actor.Garments.Select(g => g.Model.Data), figure.Actor.Animator.Stack.Skeleton.Worn);
        Assert.Single(figure.Actor.Animator.Stack.Skeleton.Worn);
        var lift = new Vector3(0, 0.023997f, 0);
        Assert.Equal(Matrix4x4.CreateTranslation(lift), figure.Lift);
        Assert.True(Vector3.Distance(lift, figure.Actor.Placement.Translation) < 1e-7f);
        // Framed as it stands: the camera over the points at rest is raised by the figure's lift.
        var body = ModelData.Load(spec.Body);
        var points = FigureFraming.RestPoints(body, Matrix4x4.Identity);
        points.AddRange(FigureFraming.RestPoints(ModelData.Load(spec.Garments[0]), Matrix4x4.Identity));
        points.AddRange(FigureFraming.RestPoints(ModelData.Load(spec.Items[0].Model), new Statventure.Animation.Socket(new Skeleton(body), "socket.hand.R").ItemTransform(body.WorldMatrices(), Matrix4x4.Identity)));
        var unraised = FigureFraming.CameraFor(spec, "three-quarter", points, FigureFraming.FocusPoint(body, "head"));
        Assert.True(Vector3.Distance(unraised.Target + lift, figure.View.Camera.Target) < 1e-5f, $"the camera's target {figure.View.Camera.Target}, not {unraised.Target + lift}");
        Assert.True(Vector3.Distance(unraised.Position + lift, figure.View.Camera.Position) < 1e-5f);
        Assert.Equal(unraised.OrthographicHeight, figure.View.Camera.OrthographicHeight, 5);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(figure.SceneOf("standing", FigureFiles.Models)));
        foreach (var model in json.RootElement.GetProperty("models").EnumerateArray())
            Assert.Equal(0.023997f, model.GetProperty("anchor")[1].GetSingle(), 6);
        Assert.Equal(0.023997f, json.RootElement.GetProperty("models")[0].GetProperty("matrix")[3][1].GetSingle(), 6);
        var socket = figure.Actor.Items[0].Socket.ItemTransform(body.WorldMatrices(figure.Actor.Animator.Pose()), figure.Actor.Placement);
        Assert.Equal(socket.Translation.Y, json.RootElement.GetProperty("models")[2].GetProperty("matrix")[3][1].GetSingle(), 6);
        // The sample's origin is its soles' level: nothing raised.
        using var sample = new FigureView(gpu.Device, new FigureSpec { Body = FigureFiles.Sample, Look = FigureFiles.Today });
        Assert.Equal((0f, Matrix4x4.Identity), (sample.Actor.Ground, sample.Lift));
        // A garment reaching under the body's soles is what the figure stands on: M7's test figure (its feet 10 mm over its
        // origin) wearing the human man's booted body as a garment (bound by the bones' names) stands on the boots.
        var m7 = Path.Combine(FigureFiles.Models, "figure.glb");
        Assert.Equal(0.01f, Statventure.Scene.Actor.GroundOf(ModelData.Load(m7), []), 4);
        using var worn = new FigureView(gpu.Device, new FigureSpec { Body = m7, Garments = [Humanoid.HumanoidFiles.Body("male")], Look = FigureFiles.Today });
        Assert.Equal(-0.023997f, worn.Actor.Ground, 6);
    }

    /// <summary>The scene file's colours are the look file's text again, every level (0–255) of each channel.</summary>
    [Fact]
    public void ColoursGoBackToTheirText()
    {
        for (var level = 0; level < 256; level++)
        {
            var hex = $"#{level:x2}{255 - level:x2}{level / 2:x2}";
            Assert.Equal(hex, FigureView.Hex(Colors.FromHex(hex)));
        }
        Assert.Equal("#2a1838", FigureView.Hex(FigureFiles.Test.Style.Ink));
    }

    /// <summary>The figure mode's keys (Figure/FigureControls.cs): play and pause, a frame, a clip, a turn, a view, quit.</summary>
    [Fact]
    public void TheKeysStepTurnAndShowViews()
    {
        using var figure = new FigureView(gpu.Device, new FigureSpec { Body = FigureFiles.Sample, Look = FigureFiles.Today, Width = 320, Height = 400 });
        var controls = new FigureControls(figure);
        Assert.True(controls.Playing);
        controls.Advance(0.105);
        Assert.Equal(0.105, controls.Time, 9);
        Assert.Equal(0.105, figure.Actor.Animator.Time, 9);
        controls.Press(Key.Space);
        Assert.False(controls.Playing);
        controls.Advance(0.1);
        Assert.Equal(0.105, controls.Time, 9);
        controls.Press(Key.Space);
        Assert.True(controls.Playing);
        // A frame on, from the whole frame nearest (0.105 s is nearest frame 6): 7/60, and paused.
        controls.Press(Key.Right);
        Assert.False(controls.Playing);
        Assert.Equal(7 / 60.0, controls.Time, 9);
        controls.Press(Key.Left);
        controls.Press(Key.Left);
        Assert.Equal(5 / 60.0, controls.Time, 9);
        // The next clip's start, the previous (round), in the same round of the cycle.
        controls.Press(Key.Down);
        Assert.Equal(0.75, controls.Time, 9);
        controls.Press(Key.Down);
        Assert.Equal(0, controls.Time, 9);
        controls.Press(Key.Up);
        Assert.Equal(0.75, controls.Time, 9);
        controls.Press(Key.Right);
        controls.Advance(0);
        Assert.Equal("sway 0.017 s", figure.Describe());
        controls.Press(Key.Left);
        for (var i = 0; i < 2 * 105; i++)
            controls.Press(Key.Right);
        Assert.Equal(0.75 + 3.5, controls.Time, 9);
        controls.Press(Key.Down);
        Assert.Equal(1.75 * 2, controls.Time, 9);
        // Turns of 15° each way, the camera staying the view's.
        var camera = figure.View.Camera.Position;
        controls.Press(Key.D);
        controls.Press(Key.D);
        controls.Press(Key.A);
        Assert.Equal(45 + 15, figure.Turn);
        Assert.Equal(camera, figure.View.Camera.Position);
        // 1–5: the views in order.
        Key[] keys = [Key.D1, Key.D2, Key.D3, Key.D4, Key.D5];
        for (var v = 0; v < keys.Length; v++)
        {
            controls.Press(keys[v]);
            Assert.Equal(FigureFraming.Views[v], figure.ViewName);
        }
        Assert.False(controls.Quit);
        controls.Press(Key.Escape);
        Assert.True(controls.Quit);

        // With more than two clips, Up goes back where Down goes on (the test figure's four).
        using var four = new FigureView(gpu.Device, new FigureSpec { Body = Path.Combine(FigureFiles.Models, "figure.glb"), Look = FigureFiles.Today, Width = 320, Height = 400 });
        var keys4 = new FigureControls(four);
        Assert.Equal(4, four.Clips.Clips.Count);
        keys4.Press(Key.Up);
        Assert.Equal(four.Clips.Starts[3], keys4.Time, 9);
        keys4.Press(Key.Down);
        Assert.Equal(0, keys4.Time, 9);
    }
}

/// <summary>The figure mode's command line (Figure/FigureCommandLine.cs), every option to its place, and what it refuses.</summary>
public class FigureCommandLineTests
{
    [Fact]
    public void EveryOptionGoesToItsPlace()
    {
        var models = FigureFiles.Models;
        string[] args =
        [
            "--figure", Path.Combine(models, "figure.glb"), "--clips", Path.Combine(models, "jiruan-acting.glb"), "--look", "today",
            "--garment", Path.Combine(models, "tabard.glb"), "--item", Path.Combine(models, "sword.glb") + "@socket.hand.R", "--item", "a@b@socket.back",
            "--view", "side", "--size", "640x360", "--elevation", "7.5", "--margin", "0.1", "--extent", "1.25", "--focus", "chest",
            "--perspective", "35", "--dump", "/tmp/frames", "--fps", "24", "--times", "0,0.5,1.25", "--scene-file", "/tmp/s.json",
            "--capture", "/tmp/c.png", "--frames", "12", "--play", "idle:2,swing,idle+hold-bow:3", "--label",
        ];
        var (spec, run) = FigureCommandLine.Parse(args, FigureFiles.Looks, "/fonts");
        Assert.Equal([new FigurePlay("idle", 2), new FigurePlay("swing"), new FigurePlay("idle", 3, "hold-bow")], spec.Play!);
        Assert.True(run.Label);
        Assert.Equal(Path.Combine(models, "figure.glb"), spec.Body);
        Assert.Equal(Path.Combine(models, "jiruan-acting.glb"), spec.Clips);
        Assert.Equal(("today", CelStyle.Default), (spec.Look.Name, spec.Look.Style));
        Assert.Equal([Path.Combine(models, "tabard.glb")], spec.Garments);
        Assert.Equal([(Path.Combine(models, "sword.glb"), "socket.hand.R"), ("a@b", "socket.back")], spec.Items);
        Assert.Equal(("side", 640, 360, 7.5f, 0.1f, (float?)1.25f, "chest", (float?)35f),
            (spec.View, spec.Width, spec.Height, spec.Elevation, spec.Margin, spec.Extent, spec.Focus, spec.Perspective));
        Assert.Equal(("/tmp/frames", 24.0, "/tmp/s.json", "/tmp/c.png", 12, "/fonts"), (run.Dump, run.Fps, run.SceneFile, run.Capture, run.CaptureFrame, run.Fonts));
        Assert.Equal([0, 0.5, 1.25], run.Times!);
    }

    [Fact]
    public void WithoutOptionsTheFigureIsInTodaysLookAtThreeQuarter()
    {
        var (spec, run) = FigureCommandLine.Parse(["--figure", FigureFiles.Sample], FigureFiles.Looks, null);
        Assert.Equal(("three-quarter", 600, 800, 5f, 0.06f, (float?)null, (string?)null, (float?)null, (string?)null),
            (spec.View, spec.Width, spec.Height, spec.Elevation, spec.Margin, spec.Extent, spec.Focus, spec.Perspective, spec.Clips));
        Assert.Equal("today", spec.Look.Name);
        Assert.Empty(spec.Garments);
        Assert.Empty(spec.Items);
        Assert.Equal(((string?)null, 30.0, (IReadOnlyList<double>?)null, (string?)null, (string?)null, 30), (run.Dump, run.Fps, run.Times, run.SceneFile, run.Capture, run.CaptureFrame));
        Assert.Null(spec.Play);
        Assert.False(run.Label);
        // A look file by its path, and by its name in the looks' folder.
        using var folder = new StorageFolder();
        var file = folder.Write("mine.json", System.Text.Encoding.UTF8.GetBytes(FigureFiles.TestLookJson));
        Assert.Equal("test", FigureCommandLine.Parse(["--figure", FigureFiles.Sample, "--look", file], FigureFiles.Looks, null).Spec.Look.Name);
        Assert.Equal("test", FigureCommandLine.Parse(["--figure", FigureFiles.Sample, "--look", "mine"], folder.Root, null).Spec.Look.Name);
    }

    public static TheoryData<string[], string> Refused => new()
    {
        { [], "--figure takes a body .glb" },
        { ["--figure", "look.json"], "--figure takes a body .glb" },
        { ["--figure"], "--figure needs a value" },
        { ["--figure", FigureFiles.Sample, "--size", "640"], "--size <width>x<height>" },
        { ["--figure", FigureFiles.Sample, "--size", "0x360"], "--size <width>x<height>" },
        { ["--figure", FigureFiles.Sample, "--item", "sword.glb"], "<glb>@<socket>" },
        { ["--figure", FigureFiles.Sample, "--item", "sword.glb@"], "<glb>@<socket>" },
        { ["--figure", FigureFiles.Sample, "--view", "above"], "no view above" },
        { ["--figure", FigureFiles.Sample, "--loop"], "no option --loop" },
        { ["--figure", FigureFiles.Sample, "--play"], "--play needs a value" },
        { ["--figure", FigureFiles.Sample, "--play", "swing:0"], "whole number from 1" },
        { ["--figure", FigureFiles.Sample, "--play", "swing:-1"], "whole number from 1" },
        { ["--figure", FigureFiles.Sample, "--play", "swing:2.5"], "whole number from 1" },
        { ["--figure", FigureFiles.Sample, "--play", "swing:"], "whole number from 1" },
        { ["--figure", FigureFiles.Sample, "--play", "idle,:2"], "<clip>[+<clip over it>][:times]" },
        { ["--figure", FigureFiles.Sample, "--play", "idle,,swing"], "<clip>[+<clip over it>][:times]" },
        { ["--figure", FigureFiles.Sample, "--play", "idle+"], "<clip>[+<clip over it>][:times]" },
        { ["--figure", FigureFiles.Sample, "--play", "+hold-bow"], "<clip>[+<clip over it>][:times]" },
        { ["--figure", FigureFiles.Sample, "--play", "a+b+c"], "<clip>[+<clip over it>][:times]" },
    };

    [Theory]
    [MemberData(nameof(Refused))]
    public void AWrongCommandLineIsRefused(string[] args, string message)
    {
        var error = Assert.Throws<ArgumentException>(() => FigureCommandLine.Parse(args, FigureFiles.Looks, null));
        Assert.Contains(message, error.Message);
    }
}

/// <summary>
/// The figure mode's own window shows its view (GM1's lesson: a harness's picture doesn't prove the program's): the
/// sandbox's FigureSandbox in a hidden window dumping a frame (--dump with --times), against the same view's picture
/// drawn offscreen; and the dump's index and scene file are what the view holds. Needs a display (the suite runs in a
/// headless compositor); in the "Window" collection, alone.
/// </summary>
[Collection("Window")]
public class FigureWindowTests(GpuFixture gpu) : IClassFixture<GpuFixture>
{
    static bool HasDisplay => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));

    [Fact]
    public void TheWindowShowsTheFigureModesView()
    {
        if (!HasDisplay)
            return;
        var scene = FigureReferenceTests.Scenes.Single(s => s.Name == "figure-test-side");
        using var folder = new StorageFolder();
        var dump = folder["dump"];
        using (var program = new FigureSandbox(scene.Spec, new FigureRun
        {
            Dump = dump, Times = [scene.Time], SceneFile = Path.Combine(dump, "scenes.json"), Visible = false, GameFolder = folder["player"],
        }))
            program.Run();
        var frame = ImageFile.Load(Path.Combine(dump, "frame-0000.png"));
        Assert.Equal((scene.Spec.Width, scene.Spec.Height), (frame.Width, frame.Height));

        using var figure = new FigureView(gpu.Device, scene.Spec);
        figure.Seek(scene.Time);
        var drawn = FigureReferenceTests.Render(gpu.Device, figure);
        var differing = 0;
        for (var i = 0; i < drawn.Pixels.Length; i += 4)
            if (drawn.Pixels[i] != frame.Pixels[i] || drawn.Pixels[i + 1] != frame.Pixels[i + 1] || drawn.Pixels[i + 2] != frame.Pixels[i + 2])
                differing++;
        Assert.True(differing == 0, $"{differing} pixels of the window's frame differ from the view's picture");

        Assert.Equal([$"frame-0000.png {scene.Time.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)} bow 0.366667"],
            File.ReadAllLines(Path.Combine(dump, "frames.txt")));
        Assert.Equal(FigureView.SceneFile([figure.SceneOf("frame-0000", dump)], dump, dump), File.ReadAllText(Path.Combine(dump, "scenes.json")));
    }

    /// <summary>
    /// A dump writes a frame per time and its index; a window that came out of another size than the picture (a
    /// compositor sizing it: cage gives a window its output, less a title bar it draws itself) writes no frame and fails
    /// the run, as its frames would be another picture.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ADumpWritesItsFramesOnlyAtThePicturesSize(bool otherSize)
    {
        if (!HasDisplay)
            return;
        var scene = FigureReferenceTests.Scenes.Single(s => s.Name == "figure-today");
        using var folder = new StorageFolder();
        var dump = folder["dump"];
        var code = Environment.ExitCode;
        try
        {
            Environment.ExitCode = 0;
            using (var program = new FigureSandbox(scene.Spec, new FigureRun
            {
                Dump = dump, Times = [0, 0.5, 1], Visible = false, GameFolder = folder["player"], WindowSize = otherSize ? (320, 363) : null,
            }))
                program.Run();
            Assert.Equal(otherSize ? 1 : 0, Environment.ExitCode);
            Assert.Equal(otherSize ? [] : ["frame-0000.png", "frame-0001.png", "frame-0002.png", "frames.txt"],
                Directory.GetFiles(dump).Select(f => Path.GetFileName(f)).Order(StringComparer.Ordinal));
        }
        finally
        {
            Environment.ExitCode = code;
        }
    }

    /// <summary>A picture smaller than the engine's usual window minimum (320 × 240) keeps its size.</summary>
    [Fact]
    public void ASmallPictureKeepsItsSize()
    {
        if (!HasDisplay)
            return;
        var scene = FigureReferenceTests.Scenes.Single(s => s.Name == "figure-today");
        using var folder = new StorageFolder();
        var dump = folder["dump"];
        using (var program = new FigureSandbox(scene.Spec with { Width = 240, Height = 180 }, new FigureRun { Dump = dump, Times = [0], Visible = false, GameFolder = folder["player"] }))
            program.Run();
        var frame = ImageFile.Load(Path.Combine(dump, "frame-0000.png"));
        Assert.Equal((240, 180), (frame.Width, frame.Height));
    }

    /// <summary>
    /// A dump with --label (GM2 step 4b) names the clip, its time in it and the view in a caption at the top left
    /// (<see cref="FigureSandbox.Label"/>) and is the view's picture everywhere else; without it a dump has no caption even
    /// with the fonts there (the window's picture is the view's alone: <see cref="TheWindowShowsTheFigureModesView"/>).
    /// </summary>
    [Fact]
    public void ALabelledDumpNamesItsClipInACaption()
    {
        if (!HasDisplay)
            return;
        var scene = FigureReferenceTests.Scenes.Single(s => s.Name == "figure-test-side");
        RgbaImage drawn;
        using (var figure = new FigureView(gpu.Device, scene.Spec))
        {
            figure.Seek(scene.Time);
            Assert.Equal("bow 0.367 s · side", FigureSandbox.Label(figure));
            drawn = FigureReferenceTests.Render(gpu.Device, figure);
        }
        using var folder = new StorageFolder();
        RgbaImage Dump(string name, bool label)
        {
            using (var program = new FigureSandbox(scene.Spec, new FigureRun
            {
                Dump = folder[name], Times = [scene.Time], Visible = false, GameFolder = folder["player"], Fonts = FigureFiles.Fonts, Label = label,
            }))
                program.Run();
            return ImageFile.Load(Path.Combine(folder[name], "frame-0000.png"));
        }
        var (plain, labelled) = (Dump("plain", false), Dump("labelled", true));
        var (differing, outside, right) = (0, 0, 0);
        for (var y = 0; y < plain.Height; y++)
            for (var x = 0; x < plain.Width; x++)
            {
                var i = 4 * (y * plain.Width + x);
                Assert.True(plain.Pixels[i] == drawn.Pixels[i] && plain.Pixels[i + 1] == drawn.Pixels[i + 1] && plain.Pixels[i + 2] == drawn.Pixels[i + 2], $"the plain dump differs from the view at {x}, {y}");
                if (plain.Pixels[i] != labelled.Pixels[i] || plain.Pixels[i + 1] != labelled.Pixels[i + 1] || plain.Pixels[i + 2] != labelled.Pixels[i + 2])
                {
                    differing++;
                    right = Math.Max(right, x);
                    if (y >= 34 || x >= 200)
                        outside++;
                }
            }
        Assert.True(differing > 300, $"only {differing} pixels of the labelled frame differ: no caption");
        Assert.True(outside == 0, $"{outside} pixels outside the caption's box differ");
        // The caption holds its words (an empty one is a box 16 pixels wide).
        Assert.True(right > 100, $"the caption ends at x {right}: no words in it");
        Assert.Equal(File.ReadAllLines(Path.Combine(folder["plain"], "frames.txt")), File.ReadAllLines(Path.Combine(folder["labelled"], "frames.txt")));
    }

    /// <summary>Without times, a dump is one round of every clip in turn at its frame rate: the sample's 1.75 s at 4 fps, 7 frames.</summary>
    [Fact]
    public void ADumpIsOneRoundOfTheClipsAtItsRate()
    {
        if (!HasDisplay)
            return;
        var scene = FigureReferenceTests.Scenes.Single(s => s.Name == "figure-today");
        using var folder = new StorageFolder();
        var dump = folder["dump"];
        using (var program = new FigureSandbox(scene.Spec, new FigureRun { Dump = dump, Fps = 4, Visible = false, GameFolder = folder["player"] }))
            program.Run();
        Assert.Equal(["frame-0000.png 0 bow 0", "frame-0001.png 0.25 bow 0.25", "frame-0002.png 0.5 bow 0.5", "frame-0003.png 0.75 bow 0.75",
            "frame-0004.png 1 sway 0.25", "frame-0005.png 1.25 sway 0.5", "frame-0006.png 1.5 sway 0.75"], File.ReadAllLines(Path.Combine(dump, "frames.txt")));
        Assert.True(File.Exists(Path.Combine(dump, "frame-0006.png")));
        // A play list's round (--play, GM2 step 4b): sway twice, then bow with sway laid over it; 2.75 s at 4 fps, 11 frames.
        var listed = folder["listed"];
        using (var program = new FigureSandbox(scene.Spec with { Play = FigurePlay.Parse("sway:2,bow+sway") }, new FigureRun { Dump = listed, Fps = 4, Visible = false, GameFolder = folder["player"] }))
            program.Run();
        Assert.Equal(["frame-0000.png 0 sway 0", "frame-0001.png 0.25 sway 0.25", "frame-0002.png 0.5 sway 0.5", "frame-0003.png 0.75 sway 0.75",
            "frame-0004.png 1 sway 0", "frame-0005.png 1.25 sway 0.25", "frame-0006.png 1.5 sway 0.5", "frame-0007.png 1.75 sway 0.75",
            "frame-0008.png 2 sway 1", "frame-0009.png 2.25 bow+sway 0.25", "frame-0010.png 2.5 bow+sway 0.5"], File.ReadAllLines(Path.Combine(listed, "frames.txt")));
    }
}
