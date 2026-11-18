using System.Numerics;
using System.Reflection;
using System.Text.Json;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Render3D;
using Statventure.Sandbox.Figure;
using Statventure.Tests.Packs;
using Statventure.Tests.Records;
using Statventure.Tests.Sandbox;

namespace Statventure.Tests.Render3D;

/// <summary>
/// The look of the 3D views as data (<see cref="RenderStyle"/>, record <c>render.style</c>; GM2 step 5, GM2-P11): every
/// number of <see cref="CelStyle"/> and the sun's, under the names of a look file's <c>style</c> and <c>sun</c>
/// (Tools/Models/humanoid/look.py), the engine's own look by default, its rules the look file's.
/// </summary>
public class RenderStyleTests
{
    /// <summary>A cel style whose every number is its own (no two alike) and a sun likewise, so a number put in another's place shows.</summary>
    static readonly CelStyle Distinct = new(Colors.FromHex("#1a2b3c"), 0.11f, 0.22f)
    {
        OutlineWidth = 3.3f, InnerWidth = 4.4f, CreaseAngle = 105.5f, HatchSpacing = 6.6f, HatchWidth = 7.7f, HatchAngle = 88.8f, HatchInk = 0.99f,
        GlowWide = 10.1f, GlowCore = 12.2f, GlowBoost = 13.3f,
    };

    static readonly SunLight DistinctSun = new(new Vector3(-0.25f, 0.5f, 0.75f), 14.4f) { Angle = 15.5f };

    /// <summary>A new style is the engine's own look: <see cref="CelStyle.Default"/> under look.py's <c>today</c> sun.</summary>
    [Fact]
    public void ANewStyleIsTheEnginesOwnLook()
    {
        var style = new RenderStyle();
        Assert.Equal(CelStyle.Default, style.Cel);
        var today = FigureFiles.Today;
        Assert.Equal(today.Sun, style.SunLight);
        Assert.Equal(today.Style, style.Cel);
        Assert.Equal(new RenderStyle(), RenderStyle.Of(CelStyle.Default, today.Sun));
        Assert.Null(style.Problem());
    }

    /// <summary>Every number of the cel style and the sun is a field of its own, each under its own name: a style and back give the same numbers.</summary>
    [Fact]
    public void EveryNumberOfTheCelStyleAndTheSunIsAField()
    {
        var style = RenderStyle.Of(Distinct, DistinctSun);
        Assert.Equal(Distinct, style.Cel);
        Assert.Equal(DistinctSun, style.SunLight);
        Assert.Equal("#1a2b3c", style.Ink);
        // Each of CelStyle's numbers has its field (a number added to CelStyle and not here fails), read from its own property.
        var celNames = typeof(CelStyle).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite || p.SetMethod != null).Select(p => p.Name).Order().ToList();
        Assert.Contains("Ink", celNames);
        var fields = RecordShape.Of<RenderStyle>().Fields.Select(f => f.Name).Where(n => n != nameof(RenderStyle.Sun)).Order().ToList();
        Assert.Equal(celNames, fields);
        foreach (var name in celNames.Where(n => n != "Ink"))
            Assert.Equal(typeof(CelStyle).GetProperty(name)!.GetValue(Distinct), typeof(RenderStyle).GetProperty(name)!.GetValue(style));
        Assert.Equal(["Angle", "Direction", "Strength"], RecordShape.Of<RenderSun>().Fields.Select(f => f.Name).Order());
        // An ink between levels (a style made in code, not read from text) to its nearest: 254.745, 127.5 and 0.5355 levels.
        Assert.Equal("#ff8001", RenderStyle.Of(Distinct with { Ink = new Vector4(0.999f, 0.5f, 0.0021f, 1) }, DistinctSun).Ink);
        // The ink back to its text at every level.
        for (var level = 0; level < 256; level++)
        {
            var hex = $"#{level:x2}{255 - level:x2}{level / 2:x2}";
            Assert.Equal(hex, RenderStyle.Of(new RenderStyle { Ink = hex }.Cel, DistinctSun).Ink);
        }
    }

    /// <summary>
    /// The JSON form holds a look file's <c>style</c> (look.py's keys, FigureLook.StyleKeys) and its <c>sun</c> (direction,
    /// strength, angle) under the same names: the game's look file's two read as a style give the numbers the figure mode
    /// reads from it.
    /// </summary>
    [Fact]
    public void TheJsonFormIsALookFilesStyleAndSun()
    {
        var path = Path.Combine(FigureFiles.Looks, "ember-hearth.json");
        using var file = JsonDocument.Parse(File.ReadAllText(path));
        var style = (Dictionary<string, object?>)JsonSerializer.Deserialize<Dictionary<string, object?>>(file.RootElement.GetProperty("style").GetRawText())!;
        style["$types"] = new Dictionary<string, int> { ["render.style"] = 1, ["render.style.sun"] = 1 };
        style["sun"] = file.RootElement.GetProperty("sun");
        var read = RecordSerializer.FromJson<RenderStyle>(JsonSerializer.SerializeToUtf8Bytes(style));
        var look = FigureLook.Load(path);
        Assert.Equal((look.Style, look.Sun), (read.Cel, read.SunLight));
        // And the form written has exactly those keys.
        using var written = JsonDocument.Parse(RecordSerializer.ToJson(RenderStyle.Of(look.Style, look.Sun)));
        Assert.Equal(["$types", .. FigureLook.StyleKeys, "sun"], written.RootElement.EnumerateObject().Select(p => p.Name));
        Assert.Equal(["direction", "strength", "angle"], written.RootElement.GetProperty("sun").EnumerateObject().Select(p => p.Name));
    }

    public static TheoryData<string, string, string> Broken => new()
    {
        { "ink", "{\"ink\": \"#12345\"}", "$.ink: \"#12345\" isn't a colour (#rrggbb)" },
        { "ink without #", "{\"ink\": \"1234567\"}", "$.ink: \"1234567\" isn't a colour (#rrggbb)" },
        { "ink not hex", "{\"ink\": \"#12g456\"}", "$.ink: \"#12g456\" isn't a colour (#rrggbb)" },
        { "shadowMix", "{\"shadowMix\": \"NaN\"}", "$.shadowMix: it must be a finite number (it is NaN)" },
        { "bandEdge", "{\"bandEdge\": \"Infinity\"}", "$.bandEdge: it must be a finite number (it is Infinity)" },
        { "outlineWidth", "{\"outlineWidth\": \"NaN\"}", "$.outlineWidth: it must be a finite number (it is NaN)" },
        { "innerWidth", "{\"innerWidth\": \"NaN\"}", "$.innerWidth: it must be a finite number (it is NaN)" },
        { "creaseAngle", "{\"creaseAngle\": \"NaN\"}", "$.creaseAngle: it must be a finite number (it is NaN)" },
        { "hatchSpacing", "{\"hatchSpacing\": \"NaN\"}", "$.hatchSpacing: it must be a finite number (it is NaN)" },
        { "hatchWidth", "{\"hatchWidth\": \"NaN\"}", "$.hatchWidth: it must be a finite number (it is NaN)" },
        { "hatchAngle", "{\"hatchAngle\": \"-Infinity\"}", "$.hatchAngle: it must be a finite number (it is -Infinity)" },
        { "hatchInk", "{\"hatchInk\": \"NaN\"}", "$.hatchInk: it must be a finite number (it is NaN)" },
        { "glowWide", "{\"glowWide\": \"NaN\"}", "$.glowWide: it must be a finite number (it is NaN)" },
        { "glowCore", "{\"glowCore\": \"NaN\"}", "$.glowCore: it must be a finite number (it is NaN)" },
        { "glowBoost", "{\"glowBoost\": \"NaN\"}", "$.glowBoost: it must be a finite number (it is NaN)" },
        { "direction 0", "{\"sun\": {\"direction\": [0, 0, 0]}}", "$.sun.direction: the direction must not be 0" },
        { "direction NaN", "{\"sun\": {\"direction\": [0, \"NaN\", 1]}}", "$.sun.direction: the direction must be three finite numbers" },
        { "strength below 0", "{\"sun\": {\"strength\": -0.5}}", "$.sun.strength: the strength must be a finite number, 0 or more (it is -0.5)" },
        { "strength NaN", "{\"sun\": {\"strength\": \"NaN\"}}", "$.sun.strength: the strength must be a finite number, 0 or more (it is NaN)" },
        { "angle below 0", "{\"sun\": {\"angle\": -1}}", "$.sun.angle: the angle must be a finite number, 0 or more (it is -1)" },
        { "angle infinite", "{\"sun\": {\"angle\": \"Infinity\"}}", "$.sun.angle: the angle must be a finite number, 0 or more (it is Infinity)" },
    };

    /// <summary>The look file's rules, each alone at its field: the ink a colour, every number finite, the sun's direction not 0, its strength and angle 0 or more.</summary>
    [Theory]
    [MemberData(nameof(Broken))]
    public void TheRulesRefuseWhatALookFileRefuses(string what, string json, string problem)
    {
        var style = Read(json);
        Assert.True(RecordChecks.Check(style).Select(p => $"{p.Path}: {p.Message}").SequenceEqual([problem]),
            $"{what}: {string.Join("; ", RecordChecks.Check(style))}");
        Assert.Equal(problem[(problem.IndexOf(": ", StringComparison.Ordinal) + 2)..], style.Problem());
    }

    /// <summary>What a look file allows passes: no width at all, a sun of no strength, an ink in capitals, a direction of any length.</summary>
    [Fact]
    public void WhatALookFileAllowsPasses()
    {
        foreach (var json in new[]
                 {
                     "{}", "{\"ink\": \"#ABCDEF\"}", "{\"outlineWidth\": 0, \"innerWidth\": 0, \"hatchInk\": 0}", "{\"shadowMix\": -2, \"bandEdge\": 7}",
                     "{\"sun\": {\"strength\": 0, \"angle\": 0, \"direction\": [0, 0, -1e-6]}}",
                 })
            Assert.Empty(RecordChecks.Check(Read(json)));
        Assert.Empty(RecordChecks.Check(RenderStyle.Of(Distinct, DistinctSun)));
    }

    /// <summary>A style's JSON form with the fields <paramref name="json"/> gives, the rest the defaults.</summary>
    static RenderStyle Read(string json)
    {
        using var given = JsonDocument.Parse(json);
        var form = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(RecordSerializer.ToJson(new RenderStyle()))!;
        foreach (var property in given.RootElement.EnumerateObject())
        {
            if (property.Name == "sun")
            {
                var sun = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(form["sun"].GetRawText())!;
                foreach (var part in property.Value.EnumerateObject())
                    sun[part.Name] = part.Value.Clone();
                form["sun"] = JsonSerializer.SerializeToElement(sun);
            }
            else
                form[property.Name] = property.Value.Clone();
        }
        return RecordSerializer.FromJson<RenderStyle>(JsonSerializer.SerializeToUtf8Bytes(form));
    }

    /// <summary>A style loads from a pack's record entry and from a loose JSON file; a broken one is refused naming the entry or the file and the rule.</summary>
    [Fact]
    public void StylesLoadFromPacksAndLooseFilesAndBrokenOnesAreRefused()
    {
        const string expected = "not a valid render style: the direction must not be 0";
        var good = RenderStyle.Of(Distinct, DistinctSun);
        var broken = good with { Sun = good.Sun with { Direction = Vector3.Zero } };
        using var folder = new TempFolder();
        var writer = new PackWriter();
        writer.AddRecord("render/styles/game", good);
        writer.AddRecord("render/styles/broken", broken);
        writer.Write(folder["data.pak"]);
        using (var packed = new VirtualFileSystem([Mount.Pack(folder["data.pak"])]))
        {
            Assert.Equal(good, RenderStyle.Load(packed, "render/styles/game"));
            var problem = Assert.Throws<DataFileException>(() => RenderStyle.Load(packed, "render/styles/broken"));
            Assert.Equal((Path.GetFullPath(folder["data.pak"]), "render/styles/broken", expected), (problem.File, problem.Entry, problem.Problem));
        }
        folder.Write("dev/render/styles/game.json", RecordSerializer.ToJson(good));
        var file = folder.Write("dev/render/styles/broken.json", RecordSerializer.ToJson(broken));
        using var loose = new VirtualFileSystem([Mount.Folder(folder["dev"])]);
        Assert.Equal(good, RenderStyle.Load(loose, "render/styles/game"));
        var looseProblem = Assert.Throws<DataFileException>(() => RenderStyle.Load(loose, "render/styles/broken"));
        Assert.Equal((Path.GetFullPath(file), (string?)null, expected), (looseProblem.File, looseProblem.Entry, looseProblem.Problem));
    }

    /// <summary>The kind: its record names, its display name and folder.</summary>
    [Fact]
    public void TheKindIsARenderStyleInRenderStyles()
    {
        var shape = RecordShape.Of<RenderStyle>();
        Assert.Equal(("render.style", "Render style", "render/styles"), (shape.Name, shape.DisplayName, shape.Folder));
        Assert.True(RecordShape.Of<RenderStyle>().Field("ink")!.IsColour);
    }

    [Fact]
    public void TheFixturesAreRecorded()
    {
        RecordFixtures.Ensure("defaults", new RenderStyle());
        RecordFixtures.Ensure("distinct", RenderStyle.Of(Distinct, DistinctSun));
    }
}
