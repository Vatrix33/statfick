using Statventure.Packs;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// The old game's stylesheet (the tests' copy, Data/Game/styles.css, which the captured screens were made
/// with) parsed from text, and compiled by the UI importer into a pack made in the test run (Docs/Assets.md,
/// "UI"; Q1: not committed). Its five @font-face urls name the old game's files (<c>objects/fonts/Lato-Regular.ttf</c>),
/// which aren't data paths (S7-1): the compiled copy names them as ui.pak does (<c>ui/fonts/lato-regular.ttf</c>),
/// and nothing else in it changes (font sources aren't part of any computed style). The pack holds the
/// sheet at <c>ui/styles/game</c> and the fonts (the tests' copies of them) at their data paths.
/// </summary>
public static class GameStylesheets
{
    /// <summary>The five fonts: the url the old stylesheet names, the data path ui.pak holds it at, the tests' copy.</summary>
    public static readonly (string Url, string Path, string File)[] Fonts =
    [
        ("objects/fonts/Lato-Regular.ttf", "ui/fonts/lato-regular.ttf", "Lato-Regular.ttf"),
        ("objects/fonts/Lato-Semibold.ttf", "ui/fonts/lato-semibold.ttf", "Lato-Semibold.ttf"),
        ("objects/fonts/Lato-Bold.ttf", "ui/fonts/lato-bold.ttf", "Lato-Bold.ttf"),
        ("objects/fonts/STIXTwoText-Regular.otf", "ui/fonts/stixtwotext-regular.otf", "STIXTwoText-Regular.otf"),
        ("objects/fonts/STIXTwoText-Italic.otf", "ui/fonts/stixtwotext-italic.otf", "STIXTwoText-Italic.otf"),
    ];

    /// <summary>The path of the compiled sheet in <see cref="Files"/>.</summary>
    public const string Path = "ui/styles/game";

    public static string FontFile(string name) => System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "Fonts", name);

    /// <summary>The stylesheet's text as the old game has it.</summary>
    public static string Text => File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "Game", "styles.css"));

    /// <summary>The text with the five font urls as data paths (each url occurs once).</summary>
    public static string CompilableText
    {
        get
        {
            var text = Text;
            foreach (var (url, path, _) in Fonts)
            {
                var quoted = $"url(\"{url}\")";
                Assert.True(text.Split(quoted).Length == 2, $"{quoted} isn't in the stylesheet once");
                text = text.Replace(quoted, $"url(\"{path}\")");
            }
            return text;
        }
    }

    static readonly Lazy<Stylesheet> ParsedSheet = new(() => Stylesheet.Parse(Text, "styles.css"));

    /// <summary>The stylesheet parsed from its text (as the game screens were tested before step 7).</summary>
    public static Stylesheet Parsed => ParsedSheet.Value;

    static readonly Lazy<VirtualFileSystem> Pack = new(() =>
    {
        var root = Directory.CreateTempSubdirectory("statventure-game-styles-").FullName;
        var folder = System.IO.Path.Combine(root, "source");
        Directory.CreateDirectory(System.IO.Path.Combine(folder, "ui", "styles"));
        Directory.CreateDirectory(System.IO.Path.Combine(folder, "ui", "fonts"));
        File.WriteAllText(System.IO.Path.Combine(folder, "ui", "styles", "game.css"), CompilableText);
        foreach (var (_, path, file) in Fonts)
            File.Copy(FontFile(file), System.IO.Path.Combine(folder, path));
        var writer = new PackWriter();
        writer.AddFolder(folder);
        var pack = System.IO.Path.Combine(root, "ui.pak");
        writer.Write(pack);
        Directory.Delete(folder, recursive: true);
        var files = new VirtualFileSystem([Mount.Pack(pack)]);
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            files.Dispose();
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch (IOException)
            {
            }
        };
        return files;
    });

    /// <summary>The pack made in the test run, mounted: the compiled sheet at <see cref="Path"/> and the fonts.</summary>
    public static VirtualFileSystem Files => Pack.Value;

    static readonly Lazy<Stylesheet> CompiledSheet = new(() => Stylesheet.Load(Files, Path));

    /// <summary>The stylesheet compiled and loaded from the pack (what the game screens are tested with since step 7).</summary>
    public static Stylesheet Compiled => CompiledSheet.Value;
}
