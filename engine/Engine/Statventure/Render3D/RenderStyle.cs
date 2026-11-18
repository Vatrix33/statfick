using System.Globalization;
using System.Numerics;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Render3D;

/// <summary>
/// A look of the 3D views as data (record <c>render.style</c>, the kind at <c>render/styles</c>; GM2-P11, the user's Q12
/// of GM2: the game's look is a record of its data, which a mod can restyle): every number of <see cref="CelStyle"/> (the
/// ink, how far a shadow tone leans toward it, the band edge, the lines' widths and the crease angle, the hatching, the
/// glow) and the sun (<see cref="SunLight"/>: its direction, strength and angle), the numbers a look file of the
/// humanoid scripts holds (<c>Tools/Models/humanoid/look.py</c>: its <c>style</c> and <c>sun</c>, under the same names),
/// so one look reads the same on both sides. A view takes <see cref="Cel"/> and <see cref="SunLight"/>; the view's
/// background and the figures' colours are not a style's (the background is the view's own, the colours the palettes').
/// The game's one entry is its look (Docs/Game/Hero.md, "How it works", step 5); the engine's test scenes keep
/// <see cref="CelStyle.Default"/> (Q11). A new style starts as the engine's own look: <see cref="CelStyle.Default"/>
/// under the sun its numbers were measured under (the lizardman test's, look.py's <c>today</c>). Its rules are the look
/// file's (the sandbox's figure mode reads them, GM2 step 3a): the ink a colour, every number finite, the sun's direction
/// not 0, its strength and angle 0 or more.
/// </summary>
[Record("render.style", 1)]
[Kind("Render style", "render/styles")]
[Help("How the 3D views look: the ink, the lit and shadow tones, the lines, the hatching, the glow and the sun. The figures' colours are their palettes'.")]
public sealed record RenderStyle
{
    [Field(1)] [Help("The ink's colour (#rrggbb): the lines and the hatching, and what the shadow tones lean toward.")] [Colour]
    public string Ink { get; init; } = "#130e0b";

    [Field(2)] [Help("How far a shadow tone leans from its lit tone toward the ink (0: not at all, 1: the ink itself).")]
    public float ShadowMix { get; init; } = 0.42f;

    [Field(3)] [Help("How much of the sun's light a surface needs to take its lit tone; with less it takes its shadow tone.")]
    public float BandEdge { get; init; } = 0.3f;

    [Field(4)] [Help("The outline's width around each model (0: none), at a device scale of 1.")] [Unit("px")]
    public float OutlineWidth { get; init; } = 3.5f;

    [Field(5)] [Help("The inner lines' width (creases, open edges and where surfaces cross; 0: none), at a device scale of 1.")] [Unit("px")]
    public float InnerWidth { get; init; } = 2;

    [Field(6)] [Help("Faces of a model meeting at less than this angle get an inner line.")] [Unit("°")]
    public float CreaseAngle { get; init; } = 120;

    [Field(7)] [Help("How far apart the hatching's lines are in shadow, at a device scale of 1.")] [Unit("px")]
    public float HatchSpacing { get; init; } = 6;

    [Field(8)] [Help("How wide the hatching's lines are, at a device scale of 1.")] [Unit("px")]
    public float HatchWidth { get; init; } = 1;

    [Field(9)] [Help("The hatching's angle on screen, clockwise from the right.")] [Unit("°")]
    public float HatchAngle { get; init; } = 72;

    [Field(10)] [Help("How far the hatching's lines lean from the shadow tone toward the ink (0: unseen, 1: the ink).")]
    public float HatchInk { get; init; } = 0.35f;

    [Field(11)] [Help("The glow's wide halo: how far it spreads (its blur's radius), at a device scale of 1.")] [Unit("px")]
    public float GlowWide { get; init; } = 8;

    [Field(12)] [Help("The glow's bright core: how far it spreads (its blur's radius), at a device scale of 1.")] [Unit("px")]
    public float GlowCore { get; init; } = 2.5f;

    [Field(13)] [Help("How strongly the glow's wide halo shows.")]
    public float GlowBoost { get; init; } = 2.2f;

    [Field(14)] [Help("The sun: the one white light, far away.")]
    public RenderSun Sun { get; init; } = new();

    /// <summary>The cel style the style gives a view (<see cref="View3D.Style"/>).</summary>
    public CelStyle Cel => new(Colors.FromHex(Ink), ShadowMix, BandEdge)
    {
        OutlineWidth = OutlineWidth,
        InnerWidth = InnerWidth,
        CreaseAngle = CreaseAngle,
        HatchSpacing = HatchSpacing,
        HatchWidth = HatchWidth,
        HatchAngle = HatchAngle,
        HatchInk = HatchInk,
        GlowWide = GlowWide,
        GlowCore = GlowCore,
        GlowBoost = GlowBoost,
    };

    /// <summary>The sun the style gives a view (<see cref="View3D.Sun"/>).</summary>
    public SunLight SunLight => new(Sun.Direction, Sun.Strength) { Angle = Sun.Angle };

    /// <summary>The style of a cel style and a sun (the ink as "#rrggbb", each channel rounded to its nearest level).</summary>
    public static RenderStyle Of(CelStyle style, SunLight sun) => new()
    {
        Ink = Hex(style.Ink),
        ShadowMix = style.ShadowMix,
        BandEdge = style.BandEdge,
        OutlineWidth = style.OutlineWidth,
        InnerWidth = style.InnerWidth,
        CreaseAngle = style.CreaseAngle,
        HatchSpacing = style.HatchSpacing,
        HatchWidth = style.HatchWidth,
        HatchAngle = style.HatchAngle,
        HatchInk = style.HatchInk,
        GlowWide = style.GlowWide,
        GlowCore = style.GlowCore,
        GlowBoost = style.GlowBoost,
        Sun = new RenderSun { Direction = sun.Direction, Strength = sun.Strength, Angle = sun.Angle },
    };

    static string Hex(Vector4 srgb) =>
        string.Create(CultureInfo.InvariantCulture, $"#{Level(srgb.X):x2}{Level(srgb.Y):x2}{Level(srgb.Z):x2}");

    static int Level(float channel) => (int)MathF.Round(Math.Clamp(channel, 0, 1) * 255);

    /// <summary>Whether <paramref name="text"/> is a colour "#rrggbb".</summary>
    public static bool IsColour(string? text) => text is { Length: 7 } && text[0] == '#' && !text.AsSpan(1).ContainsAnyExcept("0123456789abcdefABCDEF");

    /// <summary>What breaks the style's rules, or null (the first of <see cref="RecordChecks.Check"/>'s problems).</summary>
    public string? Problem() => RecordChecks.Check(this).FirstOrDefault()?.Message;

    /// <summary>The style's rules (the look file's): the ink a colour, every number finite; the sun's in <see cref="RenderSun"/>.</summary>
    [Check]
    static void Rules(RenderStyle style, CheckContext check)
    {
        if (!IsColour(style.Ink))
            check.Report(RecordPath.Root.Field("ink"), $"\"{style.Ink}\" isn't a colour (#rrggbb)");
        foreach (var (field, value) in new (string, float)[]
                 {
                     ("shadowMix", style.ShadowMix), ("bandEdge", style.BandEdge), ("outlineWidth", style.OutlineWidth), ("innerWidth", style.InnerWidth),
                     ("creaseAngle", style.CreaseAngle), ("hatchSpacing", style.HatchSpacing), ("hatchWidth", style.HatchWidth),
                     ("hatchAngle", style.HatchAngle), ("hatchInk", style.HatchInk), ("glowWide", style.GlowWide), ("glowCore", style.GlowCore),
                     ("glowBoost", style.GlowBoost),
                 })
            if (!float.IsFinite(value))
                check.Report(RecordPath.Root.Field(field), $"it must be a finite number (it is {Number(value)})");
    }

    internal static string Number(float value) => value.ToString(CultureInfo.InvariantCulture);

    /// <summary>
    /// The style at <paramref name="path"/> in the game's data (a record entry under <c>render/styles</c> in a pack, or its
    /// JSON form in the development folder), checked (<see cref="Problem"/>): a <see cref="DataFileException"/> naming the
    /// pack and entry or the loose file when it isn't a readable style or breaks a rule.
    /// </summary>
    public static RenderStyle Load(VirtualFileSystem files, string path)
    {
        var style = files.Load<RenderStyle>(path);
        if (style.Problem() is { } problem)
            throw files.Problem(path, $"not a valid render style: {problem}");
        return style;
    }
}

/// <summary>
/// A render style's sun (record <c>render.style.sun</c>; <see cref="SunLight"/>): the direction toward it, in the view's
/// world (glTF's: x right, y up, z toward the front a model faces), its strength as a Blender sun's, and its angular
/// diameter, which softens shadows. Its rules: the direction finite and not 0, the strength and the angle finite and 0 or
/// more.
/// </summary>
[Record("render.style.sun", 1)]
[Help("The one white light of a render style, far away.")]
public sealed record RenderSun
{
    [Field(1)] [Help("The direction toward the sun (x right, y up, z toward the front a model faces); any length but 0.")]
    public Vector3 Direction { get; init; } = new(1, 1.1f, 0.7f);

    [Field(2)] [Help("How strong the sun is, as a Blender sun's strength (a white surface facing it shows the strength divided by π).")]
    public float Strength { get; init; } = 3;

    [Field(3)] [Help("The sun's size in the sky: the larger, the softer the shadows' edges.")] [Unit("°")]
    public float Angle { get; init; } = 3;

    [Check]
    static void Rules(RenderSun sun, CheckContext check)
    {
        var d = sun.Direction;
        if (!(float.IsFinite(d.X) && float.IsFinite(d.Y) && float.IsFinite(d.Z)))
            check.Report(RecordPath.Root.Field("direction"), "the direction must be three finite numbers");
        else if (d == Vector3.Zero)
            check.Report(RecordPath.Root.Field("direction"), "the direction must not be 0");
        if (!(float.IsFinite(sun.Strength) && sun.Strength >= 0))
            check.Report(RecordPath.Root.Field("strength"), $"the strength must be a finite number, 0 or more (it is {RenderStyle.Number(sun.Strength)})");
        if (!(float.IsFinite(sun.Angle) && sun.Angle >= 0))
            check.Report(RecordPath.Root.Field("angle"), $"the angle must be a finite number, 0 or more (it is {RenderStyle.Number(sun.Angle)})");
    }
}
