using Statventure.Text;

namespace Statventure.Game.Icons;

/// <summary>
/// An icon (GM1-P10): its outline and its box, the glyph's that Chromium picked on the reference PC
/// (<c>Assets/UI/icons/icons.json</c>).
/// </summary>
/// <param name="Outline">The SVG path data of its file (<c>Assets/UI/icons/&lt;name&gt;.svg</c>), as written there: units of
/// 1/1000 em, x from the pen position, y down from the baseline, filled with the nonzero rule.</param>
/// <param name="Advance">The advance, in em.</param>
/// <param name="Ascent">The box above the baseline, in em (the font's ascent as Chromium read it).</param>
/// <param name="Descent">The box below the baseline, in em.</param>
/// <param name="LineGap">The font's line gap, in em.</param>
public sealed record GameIcon(string Outline, double Advance, double Ascent, double Descent, double LineGap);

/// <summary>
/// The symbols the fonts lack, drawn as vector icons in the text's colour (GM1-7, Q12, GM1-P10; Docs/Game/Foundation.md,
/// "The symbols as vector icons"; step 7): <see cref="Table"/>, compiled into the game from <c>Assets/UI/icons/</c>
/// (<c>GameIconTable.cs</c>, which the game's tests write from those files and check against them), and
/// <see cref="AddTo"/>, which gives a font library the engine's icon face: each icon's character is then drawn as the
/// icon wherever it appears in that library's text, before any font, at the text's size in the text's colour, laid
/// out by its box. <c>GameUi</c> adds them to its fonts.
/// </summary>
public static partial class GameIcons
{
    /// <summary>The icons by code point, in code-point order.</summary>
    public static IReadOnlyDictionary<int, GameIcon> Table { get; } = Made();

    /// <summary>
    /// Adds the icons to <paramref name="fonts"/> (<see cref="FontLibrary.AddIcons"/>): drawn before any of its fonts.
    /// Once per library: a library that has any of them already refuses them.
    /// </summary>
    public static void AddTo(FontLibrary fonts) =>
        fonts.AddIcons(Table.Select(entry => new IconGlyph(entry.Key, entry.Value.Outline, entry.Value.Advance, entry.Value.Ascent, entry.Value.Descent, entry.Value.LineGap)));
}
