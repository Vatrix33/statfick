using Statventure.Text;

namespace Statventure.Game.Icons;

/// <summary>An icon (GM1-P10): its outline (SVG path data in em units) and its box, the glyph's that Chromium picked on the reference PC.</summary>
public sealed record GameIcon(string Outline, double Advance, double Ascent, double Descent);

/// <summary>
/// The symbols the fonts lack, drawn as vector icons in the text's colour (GM1-7, Q12, GM1-P10; Docs/Game/Foundation.md,
/// "The symbols as vector icons"; step 7): <see cref="Table"/> from <c>Assets/UI/icons/</c>, and <see cref="AddTo"/>
/// registering the engine's icon face as a font library's last character fallback. <c>GameUi</c> adds them to its fonts.
/// <b>Step 2's stub:</b> an empty table; <see cref="AddTo"/> does nothing.
/// </summary>
public static class GameIcons
{
    /// <summary>The icons by code point.</summary>
    public static IReadOnlyDictionary<int, GameIcon> Table { get; } = new Dictionary<int, GameIcon>();

    /// <summary>Adds the icons to <paramref name="fonts"/> as its last character fallback.</summary>
    public static void AddTo(FontLibrary fonts)
    {
    }
}
