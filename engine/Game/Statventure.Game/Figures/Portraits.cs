using System.Numerics;
using Statventure.Render3D;
using Statventure.Scene;

namespace Statventure.Game.Figures;

/// <summary>
/// How the portraits' figures (the sheet's, the race carousel's and the character maker's, GM2 step 11) face the screen
/// and are lit. Step 5 found that in the three-quarter view planned for them (Q31, Q41) the figure's face falls in
/// shadow: the game's sun comes from the screen's left while the figure's front is turned to its right. Step 11 put the
/// three ways below to the user (Docs/Game/Hero.md, "How it works", step 11); <see cref="Portraits.Light"/> holds the one
/// taken (the user's Q68, 2026-11-20: <see cref="FacingLeft"/>), so the answer is one line.
/// </summary>
public enum PortraitLight
{
    /// <summary>(a) As planned: the front turned toward the screen's right (render.py's three-quarter view, 45°), in the game's sun.</summary>
    AsPlanned,

    /// <summary>(b) The same facing, the game's sun mirrored left to right for the portraits only (its x negated), so the face is lit.</summary>
    SunMirrored,

    /// <summary>(c) The front turned toward the screen's left (−45°) in the game's sun: the figure shows its left side (the battle shows its right).</summary>
    FacingLeft,
}

/// <summary>
/// The portraits' view of a figure (GM2 step 11; Q31, Q41): the figure turned by a fixed angle under a fixed camera,
/// framed whole at rest as the humanoid scripts' render.py and the sandbox's figure mode frame a full view
/// (Tools/Models/humanoid/render.py, <c>view_spec</c>; Samples/Statventure.Sandbox/Figure/FigureView.cs,
/// <c>FigureFraming</c>), so <c>make-humanoid.sh assemble</c> and Blender draw the same scene: the figure's surface points
/// at rest as it stands by its soles (its body's, its garments' and its items' at their sockets) turned, their box's
/// middle the camera's target,
/// the box's height or its width over the picture's aspect, whichever is larger, with 6 % of room on each side, an
/// orthographic camera looking 5° down from 5 m beyond the figure's reach. Framed at rest, so a clip's frames share one
/// camera.
/// </summary>
public static class Portraits
{
    /// <summary>
    /// The portraits' facing and light: the user's answer to step 11's question (Q68, 2026-11-20, "Turned to the left"):
    /// the front turned to the screen's left in the game's own sun, so Q31's and Q41's fixed three-quarter view is this
    /// facing (one line to change).
    /// </summary>
    public const PortraitLight Light = PortraitLight.FacingLeft;

    /// <summary>The three-quarter view's turn of the figure about Y, in degrees (its front, glTF's +Z, toward the screen's right).</summary>
    public const float ThreeQuarter = 45;

    /// <summary>How far the camera looks down, in degrees (render.py's 5: a face exactly edge-on is where Freestyle and the engine part).</summary>
    public const float Elevation = 5;

    /// <summary>Room around the figure, a share of its framed size on each side (render.py's).</summary>
    public const float Margin = 0.06f;

    /// <summary>The figure's turn about Y in degrees under <paramref name="light"/>.</summary>
    public static float TurnOf(PortraitLight light) => light == PortraitLight.FacingLeft ? -ThreeQuarter : ThreeQuarter;

    /// <summary>The sun the portraits take under <paramref name="light"/> from the game's <paramref name="sun"/>.</summary>
    public static SunLight SunOf(SunLight sun, PortraitLight light) =>
        light == PortraitLight.SunMirrored ? sun with { Direction = sun.Direction * new Vector3(-1, 1, 1) } : sun;

    /// <summary>
    /// The figure's surface points at rest as it stands on its node (GM2-P22, "the figure's ground": its model space raised
    /// by <see cref="Actor.Ground"/>'s negation, so its soles are on the node's origin): the body's and each garment's meshes
    /// as their skins place them at rest (<see cref="ModelData.RestPoints"/>), and each item's at its socket on the body at
    /// rest.
    /// </summary>
    public static List<Vector3> RestPoints(Actor actor)
    {
        var lift = Matrix4x4.CreateTranslation(0, -actor.Ground, 0);
        var points = actor.Model.Data.RestPoints(lift);
        foreach (var garment in actor.Garments)
            points.AddRange(garment.Model.Data.RestPoints(lift));
        var rest = actor.Model.Data.WorldMatrices();
        foreach (var item in actor.Items)
            points.AddRange(item.Model.Data.RestPoints(item.Socket.ItemTransform(rest, lift)));
        return points;
    }

    /// <summary>
    /// The camera framing a figure whose rest points are <paramref name="restPoints"/> (its model space), turned by
    /// <paramref name="turn"/> degrees about Y, whole in a picture of <paramref name="width"/> × <paramref name="height"/>
    /// pixels (render.py's full views).
    /// </summary>
    public static Camera CameraFor(IReadOnlyList<Vector3> restPoints, float turn, int width, int height)
    {
        if (restPoints.Count == 0)
            throw new ArgumentException("a figure with no surface can't be framed", nameof(restPoints));
        var rotation = Matrix4x4.CreateRotationY(turn * MathF.PI / 180);
        Vector3 min = new(float.MaxValue), max = new(float.MinValue);
        foreach (var point in restPoints)
        {
            var turned = Vector3.Transform(point, rotation);
            min = Vector3.Min(min, turned);
            max = Vector3.Max(max, turned);
        }
        var size = max - min;
        var aspect = (float)width / height;
        var target = (min + max) / 2;
        var extent = MathF.Max(size.Y, size.X / aspect) * (1 + 2 * Margin);
        var reach = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        var elevation = Elevation * MathF.PI / 180;
        var distance = reach + 5;
        return new Camera
        {
            Projection = Projection.Orthographic,
            Target = target,
            Up = Vector3.UnitY,
            Near = 0.1f,
            Far = MathF.Max(50, distance + reach + 10),
            OrthographicHeight = extent,
            VerticalFieldOfView = 40 * MathF.PI / 180,
            Position = target + distance * new Vector3(0, MathF.Sin(elevation), MathF.Cos(elevation)),
        };
    }
}
