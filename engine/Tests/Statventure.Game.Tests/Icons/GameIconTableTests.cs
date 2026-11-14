using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Statventure.Game.Icons;

namespace Statventure.Game.Tests.Icons;

/// <summary>
/// The game's icon table (GM1 step 7; Docs/Game/Foundation.md, "How it works", step 7) is the committed icons'
/// content: <c>Assets/UI/icons/icons.json</c> (the boxes, in code-point order) and the outline each names, one nonzero
/// path in units of 1/1000 em (step 3's format). <c>Game/Statventure.Game/Icons/GameIconTable.cs</c> is written from
/// them by <see cref="TheTableSourceIsWrittenFromTheFiles"/> with <c>STATVENTURE_ICON_TABLE=write</c>.
/// </summary>
public partial class GameIconTableTests
{
    static string EngineDirectory([CallerFilePath] string path = "") => Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, "..", "..", ".."));

    static string IconsDirectory => Path.Combine(EngineDirectory(), "Assets", "UI", "icons");

    static string TableSource => Path.Combine(EngineDirectory(), "Game", "Statventure.Game", "Icons", "GameIconTable.cs");

    /// <summary>One icon as the files have it: its numbers' text as icons.json writes them.</summary>
    sealed record FileIcon(int CodePoint, string Character, string Name, string Font, string Outline, string Advance, string Ascent, string Descent, string LineGap);

    [GeneratedRegex(@"<path\b([^>]*)/>")]
    private static partial Regex PathElement();

    [GeneratedRegex(@"\s([a-z-]+)=""([^""]*)""")]
    private static partial Regex Attribute();

    [GeneratedRegex(@"<(?!/?svg\b|path\b|!--)")]
    private static partial Regex OtherElement();

    /// <summary>The committed icons: each icons.json entry and the one path of its file.</summary>
    static List<FileIcon> ReadFiles()
    {
        using var table = JsonDocument.Parse(File.ReadAllText(Path.Combine(IconsDirectory, "icons.json")));
        var icons = new List<FileIcon>();
        foreach (var entry in table.RootElement.GetProperty("icons").EnumerateArray())
        {
            var codePoint = int.Parse(entry.GetProperty("codePoint").GetString()!["U+".Length..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var file = entry.GetProperty("file").GetString()!;
            var svg = File.ReadAllText(Path.Combine(IconsDirectory, file));
            // Step 3's format: one path, its fill rule nonzero (what the engine's icon face fills), nothing else drawn.
            var paths = PathElement().Matches(svg);
            Assert.True(paths.Count == 1, $"{file}: {paths.Count} paths, the table holds one outline an icon");
            var attributes = Attribute().Matches(paths[0].Value).ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
            Assert.True(attributes.Keys.Order().SequenceEqual(["d", "fill-rule"]), $"{file}: the path has {string.Join(", ", attributes.Keys)}, not only d and fill-rule");
            Assert.True(attributes["fill-rule"] == "nonzero", $"{file}: fill-rule {attributes["fill-rule"]}, the icon face fills nonzero");
            Assert.True(OtherElement().Matches(svg).Count == 0, $"{file}: holds something else than the svg and its path");
            icons.Add(new FileIcon(codePoint, entry.GetProperty("character").GetString()!, entry.GetProperty("name").GetString()!,
                entry.GetProperty("measured").GetProperty("font").GetString()!, attributes["d"],
                entry.GetProperty("advance").GetRawText(), entry.GetProperty("ascent").GetRawText(), entry.GetProperty("descent").GetRawText(), entry.GetProperty("lineGap").GetRawText()));
        }
        return icons;
    }

    static double Number(string text) => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture);

    /// <summary>The table's source made from the files.</summary>
    static string Render(List<FileIcon> icons)
    {
        var text = new StringBuilder();
        text.Append("""
            // The game's icon table (GM1 step 7; Docs/Game/Foundation.md, "How it works", step 7): Assets/UI/icons/ (icons.json and
            // the outline each icon's file holds), written by Tests/Statventure.Game.Tests/Icons/GameIconTableTests with
            // STATVENTURE_ICON_TABLE=write; that test checks the table is the files' content. Not edited by hand: the icons are
            // made by Tools/IconSheet/gen/, then this is written again.
            namespace Statventure.Game.Icons;

            public static partial class GameIcons
            {
                static Dictionary<int, GameIcon> Made() => new()
                {

            """);
        foreach (var icon in icons)
        {
            text.Append($"        // {icon.Character} U+{icon.CodePoint:X4} {icon.Name} ({icon.Font})\n");
            text.Append($"        [0x{icon.CodePoint:X4}] = new(\"{icon.Outline}\", {icon.Advance}, {icon.Ascent}, {icon.Descent}, {icon.LineGap}),\n");
        }
        text.Append("""
                };
            }

            """);
        return text.ToString();
    }

    [Fact]
    public void TheTableSourceIsWrittenFromTheFiles()
    {
        var expected = Render(ReadFiles());
        if (Environment.GetEnvironmentVariable("STATVENTURE_ICON_TABLE") == "write")
            File.WriteAllText(TableSource, expected);
        var actual = File.ReadAllText(TableSource);
        Assert.True(actual == expected, $"{TableSource} isn't what Assets/UI/icons/ gives: write it again with STATVENTURE_ICON_TABLE=write (and rebuild)");
    }

    [Fact]
    public void TheTableIsTheCommittedFilesContent()
    {
        var files = ReadFiles();
        Assert.Equal(28, files.Count);
        Assert.Equal(files.Select(i => i.CodePoint), GameIcons.Table.Keys);
        foreach (var file in files)
        {
            var icon = GameIcons.Table[file.CodePoint];
            var name = $"{file.Character} {file.Name}";
            Assert.True(icon.Outline == file.Outline, $"{name}: the outline isn't its file's");
            Assert.True(icon.Advance == Number(file.Advance), $"{name}: advance {icon.Advance}, icons.json {file.Advance}");
            Assert.True(icon.Ascent == Number(file.Ascent), $"{name}: ascent {icon.Ascent}, icons.json {file.Ascent}");
            Assert.True(icon.Descent == Number(file.Descent), $"{name}: descent {icon.Descent}, icons.json {file.Descent}");
            Assert.True(icon.LineGap == Number(file.LineGap), $"{name}: line gap {icon.LineGap}, icons.json {file.LineGap}");
        }
    }

    /// <summary>The set GM1-7 decided (Docs/Game/Foundation.md, "The symbols as vector icons"): the 28 symbols, ☾ and ＋ not among them.</summary>
    [Fact]
    public void TheTableHoldsTheTwentyEightSymbols()
    {
        Assert.Equal("↶↺↻⇧⌄⌖▣▲▼◆◇◈◌◎◫★☆☰♛♜♟♡⚔⚙✦✧✸✺", string.Concat(GameIcons.Table.Keys.Select(char.ConvertFromUtf32)));
    }
}
