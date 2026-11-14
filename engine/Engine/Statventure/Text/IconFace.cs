using System.Numerics;
using HarfBuzzSharp;
using CanvasPath = Statventure.Canvas.Path;

namespace Statventure.Text;

/// <summary>
/// A symbol drawn as a vector icon wherever its character appears in text (GM1-7; Docs/Game/Foundation.md, "The symbols
/// as vector icons"): its outline, filled at the text's size in the text's colour as a glyph is, and its box, which lays
/// out lines as the glyph it stands for did. Added to a library with <see cref="FontLibrary.AddIcons"/>, which draws its
/// character with it before any font.
/// </summary>
/// <param name="CodePoint">The character the icon is drawn for.</param>
/// <param name="Outline">
/// SVG path data in units of 1/1000 em, x to the right of the pen position and y down from the baseline, filled with
/// the nonzero rule (holes wound against their outer rings).
/// </param>
/// <param name="Advance">How far the pen moves after the icon, in em.</param>
/// <param name="Ascent">The box's height above the baseline, in em: with the descent and the line gap, what a font's
/// ascent, descent and line gap are to its glyphs (a line of <c>line-height: normal</c> takes them).</param>
/// <param name="Descent">The box's depth below the baseline, in em.</param>
/// <param name="LineGap">The line gap, in em.</param>
public sealed record IconGlyph(int CodePoint, string Outline, double Advance, double Ascent, double Descent, double LineGap)
{
    /// <summary>The outlines' units: 1000 to the em.</summary>
    public const int UnitsPerEm = 1000;
}

/// <summary>
/// Icons that share one box (the same ascent, descent and line gap), and so one face: the icons of one font in the glyph
/// boxes they keep, so a run of them is one run as the font's glyphs were. Glyph <c>i + 1</c> is <see cref="Icons"/>[i].
/// </summary>
internal sealed class IconGroup
{
    public readonly int Index;
    public readonly double Ascent, Descent, LineGap;
    public readonly IconGlyph[] Icons;
    /// <summary>
    /// Each icon's outline in em (y down), parsed once: drawn at the text's size, its scale is the size in pixels, which
    /// the canvas's path cache keeps to a thousandth (in the outline's own 1/1000 em it would keep the size to 1/1000
    /// of its value in pixels, 1 % at 20 px).
    /// </summary>
    public readonly CanvasPath[] Outlines;
    /// <summary>Each outline's tight bounds in units, rounded out (HarfBuzz's glyph extents, y up).</summary>
    public readonly GlyphExtents[] Extents;
    public readonly Dictionary<int, uint> GlyphOf;

    public IconGroup(int index, IconGlyph[] icons)
    {
        Index = index;
        Icons = icons;
        (Ascent, Descent, LineGap) = (icons[0].Ascent, icons[0].Descent, icons[0].LineGap);
        Outlines = new CanvasPath[icons.Length];
        Extents = new GlyphExtents[icons.Length];
        GlyphOf = [];
        for (var i = 0; i < icons.Length; i++)
        {
            var units = CanvasPath.ParseSvg(icons[i].Outline);
            Outlines[i] = Scaled(units, 1f / IconGlyph.UnitsPerEm);
            var bounds = units.TightBounds;
            if (!bounds.IsEmpty)
            {
                int left = (int)MathF.Floor(bounds.Left), right = (int)MathF.Ceiling(bounds.Right);
                int top = (int)MathF.Floor(bounds.Top), bottom = (int)MathF.Ceiling(bounds.Bottom);
                // HarfBuzz's extents are y up: the bearing is the top above the baseline, the height negative.
                Extents[i] = new GlyphExtents { XBearing = left, YBearing = -top, Width = right - left, Height = top - bottom };
            }
            GlyphOf[icons[i].CodePoint] = (uint)(i + 1);
        }
    }

    /// <summary>A copy of <paramref name="path"/> with every point times <paramref name="factor"/>.</summary>
    static CanvasPath Scaled(CanvasPath path, float factor)
    {
        var scaled = new CanvasPath();
        var at = 0;
        foreach (var verb in path.Verbs)
            switch (verb)
            {
                case CanvasPath.Verb.Move: scaled.MoveTo(path.Points[at++] * factor); break;
                case CanvasPath.Verb.Line: scaled.LineTo(path.Points[at++] * factor); break;
                case CanvasPath.Verb.Quad: scaled.QuadTo(path.Points[at] * factor, path.Points[at + 1] * factor); at += 2; break;
                case CanvasPath.Verb.Cubic: scaled.CubicTo(path.Points[at] * factor, path.Points[at + 1] * factor, path.Points[at + 2] * factor); at += 3; break;
                case CanvasPath.Verb.Close: scaled.Close(); break;
            }
        return scaled;
    }

    public IconGlyph? Icon(uint glyph) => glyph >= 1 && glyph <= Icons.Length ? Icons[glyph - 1] : null;

    public CanvasPath? Outline(uint glyph) => glyph >= 1 && glyph <= Icons.Length ? Outlines[glyph - 1] : null;
}

/// <summary>
/// What makes a <see cref="FontFace"/> an icon face: its group of icons, and the font it was asked for in (the
/// "parent", the run's primary font), whose space it takes. Blink shapes a space with the first font of the style that
/// has one (HarfBuzzShaper: only the characters a font lacks go on to the next), and the engine's layout keeps a space
/// in the run before it (InlineLayout, TextLayout): so a space after an icon ("↻ Refresh file") has the parent's
/// glyph and advance, as Chromium's had the primary font's. An icon face is made per group and parent font.
/// </summary>
internal sealed class IconFaceData(IconGroup group, FontFace? parent)
{
    public readonly IconGroup Group = group;
    public readonly FontFace? Parent = parent;
    /// <summary>The parent's space glyph, 0 without one.</summary>
    public readonly uint ParentSpace = parent?.GlyphFor(' ') ?? 0;
    /// <summary>This face's glyph for a space: after the icons, while the parent has one.</summary>
    public uint SpaceGlyph => ParentSpace != 0 ? (uint)Group.Icons.Length + 1 : 0;

    // HarfBuzz reaches the face through these (one immutable set for every icon face; the face's data is the user data).
    static readonly Lazy<FontFunctions> Functions = new(() =>
    {
        var functions = new FontFunctions();
        functions.SetNominalGlyphDelegate((Font _, object data, uint unicode, out uint glyph) => ((IconFaceData)data).Nominal(unicode, out glyph));
        functions.SetHorizontalGlyphAdvanceDelegate((_, data, glyph) => ((IconFaceData)data).AdvanceUnits(glyph));
        functions.SetGlyphExtentsDelegate((Font _, object data, uint glyph, out GlyphExtents extents) => ((IconFaceData)data).GlyphExtents(glyph, out extents));
        functions.SetHorizontalFontExtentsDelegate((Font _, object data, out FontExtents extents) => ((IconFaceData)data).FontExtents(out extents));
        functions.MakeImmutable();
        return functions;
    });

    public static void Attach(Font font, IconFaceData data) => font.SetFontFunctions(Functions.Value, data);

    bool Nominal(uint unicode, out uint glyph)
    {
        if (Group.GlyphOf.TryGetValue((int)unicode, out glyph))
            return true;
        glyph = unicode == ' ' ? SpaceGlyph : 0;
        return glyph != 0;
    }

    /// <summary>The advance in the face's units (HarfBuzz's positions; the layout takes its pixels from <see cref="AdvancePixels"/>).</summary>
    int AdvanceUnits(uint glyph)
    {
        if (Group.Icon(glyph) is { } icon)
            return (int)Math.Round(icon.Advance * IconGlyph.UnitsPerEm);
        if (glyph != 0 && glyph == SpaceGlyph && Parent is { } parent)
            return (int)Math.Round(parent.HbFont.GetHorizontalGlyphAdvance(ParentSpace) * (double)IconGlyph.UnitsPerEm / parent.UnitsPerEm);
        return 0;
    }

    bool GlyphExtents(uint glyph, out GlyphExtents extents)
    {
        extents = glyph >= 1 && glyph <= Group.Icons.Length ? Group.Extents[glyph - 1] : default;
        return glyph != 0;
    }

    bool FontExtents(out FontExtents extents)
    {
        extents = new FontExtents
        {
            Ascender = (int)Math.Round(Group.Ascent * IconGlyph.UnitsPerEm),
            Descender = -(int)Math.Round(Group.Descent * IconGlyph.UnitsPerEm),
            LineGap = (int)Math.Round(Group.LineGap * IconGlyph.UnitsPerEm),
        };
        return true;
    }

    /// <summary>
    /// A glyph's advance in pixels at <paramref name="size"/>: an icon's is its advance in em times the size as a font's
    /// glyphs are scaled (Blink's font size kept to 0.01 px, truncated; Skia's size in 26.6, truncated; the result in
    /// 16.16, as skrifa gives a font's), the space's the parent's own (<see cref="TextLayout.Advance"/> for it).
    /// </summary>
    public float AdvancePixels(uint glyph, float size)
    {
        if (Group.Icon(glyph) is { } icon)
        {
            var keyed = (int)(size * 100f) / 100f;
            var size26Dot6 = (int)(keyed * 64f);
            return (float)(Math.Round(icon.Advance * size26Dot6 / 64.0 * 65536.0, MidpointRounding.AwayFromZero) / 65536.0);
        }
        if (glyph != 0 && glyph == SpaceGlyph && Parent is { } parent)
            return TextLayout.Advance(parent, ParentSpace, parent.HbFont.GetHorizontalGlyphAdvance(ParentSpace), size, TextLayout.AdvanceScale(parent, size));
        return 0;
    }

    /// <summary>An icon's outline flattened at <paramref name="size"/> pixels, y down from the baseline (for text decorations' ink skipping).</summary>
    public List<Vector2[]> Contours(uint glyph, float size)
    {
        var contours = new List<Vector2[]>();
        if (Group.Outline(glyph) is not { } outline)
            return contours;
        foreach (var (points, _) in outline.Flatten(Matrix3x2.CreateScale(size)))
            contours.Add([.. points]);
        return contours;
    }
}
