using System.Text;
using Statventure.Game.Data.Races;

namespace Statventure.Game.Screens.Sheet;

/// <summary>
/// The race's emblem picture on the sheet's seal button (Q36: pictures where the emblem is small). The nine emblems are
/// GM2 step 10's (3D medallions on nine new motifs, Q35, Q37, their pictures rendered by Blender into <c>ui.pak</c>, found
/// by the race's <see cref="Race.Emblem"/> name, Q44); until then every race with an emblem shows one placeholder: the old
/// seals' frame (a dark field in a brass rim with 24 ticks and an inner ring) round the game's four-pointed star, with no
/// race's motif. A race with no emblem shows the ✦ as the old page did. Step 10 replaces <see cref="For"/>.
/// </summary>
public static class EmblemPictures
{
    /// <summary>The placeholder's drawing (128 × 128, as the old seals), solid colours only.</summary>
    public const string PlaceholderSvg = """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128" height="128">
        <circle cx="64" cy="64" r="61" fill="#120c08"/>
        <circle cx="64" cy="64" r="58.5" fill="#241a12" stroke="#c49a56" stroke-width="5"/>
        <circle cx="64" cy="64" r="50" fill="none" stroke="#6e5232" stroke-width="1.4"/>
        <g stroke="#c49a56" stroke-width="1.6" stroke-linecap="round" opacity=".75">TICKS</g>
        <circle cx="64" cy="64" r="27" fill="none" stroke="#6e5232" stroke-width="1.2"/>
        <path d="M64 30L70.5 57.5L98 64L70.5 70.5L64 98L57.5 70.5L30 64L57.5 57.5Z" fill="#e4d3a8" stroke="#120c08" stroke-width="2.6" stroke-linejoin="round"/>
        <path d="M64 30L70.5 57.5L64 64ZM98 64L70.5 70.5L64 64ZM64 98L57.5 70.5L64 64ZM30 64L57.5 57.5L64 64Z" fill="#120c08" opacity=".28"/>
        <circle cx="64" cy="64" r="3.2" fill="#c49a56" stroke="#120c08" stroke-width="1.2"/>
        </svg>
        """;

    /// <summary>The placeholder as the image's source: an SVG data URL (drawn alike by the engine, its program and Chromium).</summary>
    public static string Placeholder { get; } = "data:image/svg+xml;base64," + Convert.ToBase64String(Encoding.UTF8.GetBytes(
        PlaceholderSvg.Replace("TICKS", string.Concat(Enumerable.Range(0, 24).Select(i => $"""<path d="M64 9.5v4" transform="rotate({i * 15} 64 64)"/>""")))));

    /// <summary>The picture of <paramref name="race"/>'s emblem (until step 10, the placeholder), or null for none: no race, or a race with no emblem (the ✦ shows).</summary>
    public static string? For(Race? race) => race?.Emblem is { Length: > 0 } ? Placeholder : null;
}
