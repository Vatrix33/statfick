using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Statventure.Text;

namespace Statventure.Tests.Text;

/// <summary>
/// The game's committed icons (GM1 step 3, <c>Assets/UI/icons/</c>: icons.json's boxes and the outline of each file) as
/// the engine's <see cref="IconGlyph"/>s, for the engine's icon tests and fixtures. The game compiles the same files into
/// its table (<c>GameIcons</c>, checked against them by the game's tests); the engine's tests can't reference the game.
/// </summary>
public static partial class IconFiles
{
    static string EngineDirectory([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", ".."));

    public static string Directory => Path.Combine(EngineDirectory(), "Assets", "UI", "icons");

    /// <summary>One committed icon: the engine's glyph, its file's name and the font Chromium drew its symbol with.</summary>
    public sealed record FileIcon(IconGlyph Glyph, string Name, string Font)
    {
        public string Character => char.ConvertFromUtf32(Glyph.CodePoint);
    }

    [GeneratedRegex(@"<path\b[^>]*\sd=""([^""]*)""")]
    private static partial Regex PathData();

    static readonly Lazy<List<FileIcon>> Icons = new(() =>
    {
        using var table = JsonDocument.Parse(File.ReadAllText(System.IO.Path.Combine(Directory, "icons.json")));
        var icons = new List<FileIcon>();
        foreach (var entry in table.RootElement.GetProperty("icons").EnumerateArray())
        {
            var codePoint = int.Parse(entry.GetProperty("codePoint").GetString()!["U+".Length..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var svg = File.ReadAllText(System.IO.Path.Combine(Directory, entry.GetProperty("file").GetString()!));
            var glyph = new IconGlyph(codePoint, PathData().Match(svg).Groups[1].Value, entry.GetProperty("advance").GetDouble(),
                entry.GetProperty("ascent").GetDouble(), entry.GetProperty("descent").GetDouble(), entry.GetProperty("lineGap").GetDouble());
            icons.Add(new FileIcon(glyph, entry.GetProperty("name").GetString()!, entry.GetProperty("measured").GetProperty("font").GetString()!));
        }
        return icons;
    });

    /// <summary>The 28 icons in code-point order.</summary>
    public static IReadOnlyList<FileIcon> All => Icons.Value;

    public static FileIcon Of(string character) => All.Single(i => i.Character == character);
}
