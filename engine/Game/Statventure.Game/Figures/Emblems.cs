using System.Globalization;
using System.Numerics;
using Statventure.Game.Data.Races;
using Statventure.Packs;
using Statventure.Render3D;

namespace Statventure.Game.Figures;

/// <summary>
/// The race emblems as the game draws them (GM2 step 10; Docs/Game/Hero.md, "The nine seals as 3D emblems" and "How it
/// works", step 10): a race's emblem (<see cref="Race.Emblem"/>, a name, Q44) is the medallion in relief (Q35) at
/// <see cref="ModelPath"/> in models.pak (made by the humanoid family's scripts, <c>Tools/Models/humanoid/emblems/</c>, on
/// the motif the user chose for the race, Q66; the manager's make, GM2-P32), coloured from the race's accent by one rule
/// (<see cref="Colours"/>, the manager's GM2-P31; the scripts' side is <c>emblems/palette.py</c>, held alike by the tests),
/// drawn in the game's style (<see cref="GameStyle"/>). The live emblem (the carousel's, Q36, Q67: GM2 step 11) takes its
/// model, palette and style from here; its pictures (<c>Screens/Sheet/EmblemPictures</c>) are Blender's renders of the same
/// model, palette and style from <see cref="PictureCamera"/>.
/// </summary>
public static class Emblems
{
    /// <summary>Where the emblems' models are: <c>models/humanoid/emblems/&lt;name&gt;</c> (models.pak; Assets/Models/humanoid/emblems/&lt;name&gt;.glb).</summary>
    public const string ModelFolder = "models/humanoid/emblems";

    /// <summary>The model of the emblem <paramref name="emblem"/> (a race's <c>emblem</c>).</summary>
    public static string ModelPath(string emblem) => $"{ModelFolder}/{emblem}";

    /// <summary>One unit of the medallion, metres: its field's radius, the motifs' unit (GM2-P32).</summary>
    public const float Unit = 0.08f;

    /// <summary>The medallion's radius, metres: its rim reaches 1.33 units.</summary>
    public const float Radius = 1.33f * Unit;

    /// <summary>The look's gold (the rim and the motif's metal).</summary>
    public const string Gold = "#bb8d3c";

    /// <summary>The bone (the motif's light parts).</summary>
    public const string Bone = "#e1d3ad";

    /// <summary>The ink the field and the inlays are mixed toward.</summary>
    public const string Ink = "#1a120d";

    /// <summary>
    /// The six palette slots of an emblem (the scripts' <c>look.EMBLEM_SLOTS</c>), each with its rule: the colour it is
    /// mixed from (<c>accent</c>: the race's), toward what (none: the colour itself) and how far.
    /// </summary>
    public static readonly IReadOnlyList<(string Slot, string From, string? Toward, double Amount)> Rule =
    [
        ("emblem.rim", Gold, null, 0),
        ("emblem.field", "accent", Ink, 0.58),
        ("emblem.high", Bone, null, 0),
        ("emblem.metal", Gold, null, 0),
        ("emblem.accent", "accent", Bone, 0.18),
        ("emblem.cut", "accent", Ink, 0.90),
    ];

    /// <summary>
    /// The emblem's colours for a race of accent <paramref name="accent"/> ("#rrggbb"): slot by slot as <see cref="Rule"/>
    /// says, a mix per sRGB channel (0–255) <c>a + (b − a) · t</c> rounded half to even (the approval renders' rule).
    /// </summary>
    public static IReadOnlyDictionary<string, string> Colours(string accent)
    {
        var a = Channels(accent);
        var colours = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (slot, from, toward, amount) in Rule)
        {
            var source = from == "accent" ? a : Channels(from);
            colours[slot] = Hex(toward is null ? source : Mix(source, Channels(toward), amount));
        }
        return colours;
    }

    /// <summary>The emblem's palette for <paramref name="race"/> (its accent's <see cref="Colours"/>): the six slots filled.</summary>
    public static Palette PaletteOf(Race race) => new(Colours(race.Accent));

    /// <summary>
    /// The emblem's palette for the race <paramref name="race"/> (its id) of the game's data <paramref name="data"/>: the six
    /// slots filled from its accent by the rule (the live emblem's seam, GM2 step 11). A race the data hasn't is refused.
    /// </summary>
    public static Palette PaletteOf(VirtualFileSystem data, string race) =>
        RaceCatalogue.Load(data).FirstOrDefault(r => r.Id == race) is { } entry
            ? PaletteOf(entry.Race)
            : throw new ArgumentException($"the game's data has no race {race}", nameof(race));

    /// <summary>One sRGB channel mixed toward another: <c>a + (b − a) · t</c>, rounded half to even.</summary>
    public static int Mix(int a, int b, double t) => (int)Math.Round(a + (b - a) * t, MidpointRounding.ToEven);

    static int[] Mix(int[] a, int[] b, double t) => [Mix(a[0], b[0], t), Mix(a[1], b[1], t), Mix(a[2], b[2], t)];

    static int[] Channels(string hex)
    {
        if (hex is not { Length: 7 } || hex[0] != '#' || !int.TryParse(hex.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
            throw new ArgumentException($"\"{hex}\" isn't a colour #rrggbb", nameof(hex));
        return [(value >> 16) & 255, (value >> 8) & 255, value & 255];
    }

    static string Hex(int[] c) => string.Create(CultureInfo.InvariantCulture, $"#{c[0]:x2}{c[1]:x2}{c[2]:x2}");

    /// <summary>
    /// The pictures' camera (the approval renders', the manager's hand-over): orthographic, straight on at the medallion's
    /// middle from about 5° up (0.35 m up for 4 m out, as the references look a little down), 3 units high, so the
    /// medallion (2.66 units across) fills the picture with a small margin.
    /// </summary>
    public static Camera PictureCamera() => new()
    {
        Projection = Projection.Orthographic,
        Position = new Vector3(0, 0.35f, 4),
        Target = Vector3.Zero,
        Up = Vector3.UnitY,
        OrthographicHeight = 3 * Unit,
        Near = 0.1f,
        Far = 50,
    };
}
