using Statventure.Tests.Infrastructure;
using Statventure.Text;

namespace Statventure.Tests.Canvas;

/// <summary>The game's fonts (copied from the current game into Data/Fonts), registered as its @font-face rules do.</summary>
public static class TestFonts
{
    static readonly Lazy<FontLibrary> Instance = new(() =>
    {
        var fonts = new FontLibrary();
        var dir = Path.Combine(AppContext.BaseDirectory, "Data", "Fonts");
        fonts.AddFile(Path.Combine(dir, "Lato-Regular.ttf"), "Lato", 400);
        fonts.AddFile(Path.Combine(dir, "Lato-Semibold.ttf"), "Lato", 500, weightMax: 600);
        fonts.AddFile(Path.Combine(dir, "Lato-Bold.ttf"), "Lato", 700, weightMax: 900);
        fonts.AddFile(Path.Combine(dir, "STIXTwoText-Regular.otf"), "STIX Two Text", 400);
        fonts.AddFile(Path.Combine(dir, "STIXTwoText-Italic.otf"), "STIX Two Text", 400, italic: true);
        return fonts;
    });

    public static FontLibrary Library => Instance.Value;

    /// <summary>fontconfig's pick for a pattern, as on the PC the references come from (<see cref="SystemFonts"/>).</summary>
    static string FcMatch(string pattern) => SystemFonts.Match(pattern);

    static readonly Lazy<FontLibrary> WithSystem = new(NewLibraryWithSystemFallback);

    /// <summary>
    /// A library of its own as <see cref="LibraryWithSystemFallback"/> is (the game's fonts, the system's as Chromium falls
    /// back to them), for a test that adds to it (GM1 step 7's icons) without changing the shared one.
    /// </summary>
    public static FontLibrary NewLibraryWithSystemFallback()
    {
        var fonts = Create();
        // Generic families are the system's (fontconfig's sans-serif, serif and monospace), as in the browser.
        // Chromium's default generic fonts on Linux are Arial, Times New Roman and Monospace, through fontconfig.
        foreach (var (generic, name) in new[] { ("sans-serif", "Arial"), ("serif", "Times New Roman"), ("monospace", "Monospace") })
        {
            var family = "system-" + generic;
            foreach (var (weight, italic, pattern) in new[] { (400, false, ""), (700, false, ":weight=bold"), (400, true, ":slant=italic"), (700, true, ":weight=bold:slant=italic") })
            {
                var file = FcMatch(name + pattern);
                if (File.Exists(file))
                    fonts.AddFile(file, family, weight, italic, weightMax: weight == 400 ? 600 : 900);
            }
            fonts.SetGeneric(generic, family);
        }
        fonts.DefaultFamily = "Lato";
        var faces = new Dictionary<(int, int, bool), ResolvedFont?>();
        var files = new Dictionary<string, FontFace>();
        // Characters the game's fonts lack come from the system's fonts, picked by fontconfig as
        // Chromium on Linux picks them (sans-serif with the character, the wanted weight and slant).
        var guard = new Lock();
        fonts.CharacterFallback = (codepoint, preferred) =>
        {
            lock (guard)
                return Fallback(codepoint, preferred);
        };
        ResolvedFont? Fallback(int codepoint, ResolvedFont preferred)
        {
            var weight = preferred.Face.Weight + (preferred.SyntheticBold ? 300 : 0);
            var italic = preferred.Face.Italic || preferred.SyntheticOblique;
            var key = (codepoint, weight, italic);
            if (faces.TryGetValue(key, out var cached))
                return cached;
            // The fonts the game's Chromium was measured to fall back to for the game's symbols
            // (fontconfig alone sorts these differently); other characters ask fontconfig.
            // A file name is that file; a family goes through fontconfig with the weight and slant.
            string? symbolPath = null;
            if (SymbolFonts.TryGetValue(codepoint, out var symbolFont))
                symbolPath = symbolFont.Contains('.') ? SystemFontFile(symbolFont)
                    : FcMatch($"{symbolFont}:weight={(weight >= 700 ? "bold" : weight >= 600 ? "semibold" : weight >= 500 ? "medium" : "regular")}{(italic ? ":slant=italic" : "")}");
            if (symbolPath is { Length: > 0 } && File.Exists(symbolPath))
            {
                if (!files.TryGetValue(symbolPath, out var symbolFace))
                    files[symbolPath] = symbolFace = fonts.AddFile(symbolPath, "system:" + symbolPath, weight, italic);
                faces[key] = new ResolvedFont(symbolFace, false, false);
                return faces[key];
            }
            var fcWeight = weight >= 700 ? "bold" : weight >= 600 ? "semibold" : weight >= 500 ? "medium" : "regular";
            var generic = preferred.Face.Family == "system-serif" ? "serif" : preferred.Face.Family == "system-monospace" ? "monospace" : "sans-serif";
            var file = FcMatch($"{generic}:charset={codepoint:x}:weight={fcWeight}{(italic ? ":slant=italic" : "")}");
            ResolvedFont? result = null;
            if (File.Exists(file))
            {
                if (!files.TryGetValue(file, out var face))
                    files[file] = face = fonts.AddFile(file, "system:" + file, weight, italic);
                if (face.Has(codepoint))
                    result = new ResolvedFont(face, false, false);
            }
            faces[key] = result;
            return result;
        }
        return fonts;
    }

    /// <summary>
    /// The fonts the game's Chromium was measured to fall back to for the game's symbols: a file, or a
    /// family whose file fontconfig picks by weight. (Adwaita Sans is a variable font; Chromium sets
    /// its weight axis, which the engine doesn't, so bold ↻ comes out 1/3 px wider here.)
    /// </summary>
    static readonly Dictionary<int, string> SymbolFonts = new()
    {
        ['↶'] = "NotoSansMath-Regular.ttf", ['▣'] = "NotoSansMath-Regular.ttf", ['◈'] = "NotoSansMath-Regular.ttf", ['◫'] = "NotoSansMath-Regular.ttf", ['☾'] = "NotoSansMath-Regular.ttf",
        ['↺'] = "Adwaita Sans", ['↻'] = "Adwaita Sans", ['◇'] = "Adwaita Sans", ['☆'] = "Adwaita Sans", ['♡'] = "Adwaita Sans",
        ['⌖'] = "Adwaita Mono", ['☰'] = "Adwaita Mono", ['⚙'] = "Adwaita Mono", ['✦'] = "Adwaita Mono", ['✧'] = "Adwaita Mono", ['✺'] = "Adwaita Mono",
        ['♜'] = "NotoSansSymbols2-Regular.ttf", ['♟'] = "NotoEmoji-Regular.ttf", ['⚔'] = "NotoEmoji-Regular.ttf", ['＋'] = "NotoSansCJK-Regular.ttc",
    };

    static string? SystemFontFile(string name) => SystemFonts.File(name);

    /// <summary>The game's fonts, with the system's as fallbacks the way the game's browser uses them (for comparing with it).</summary>
    public static FontLibrary LibraryWithSystemFallback => WithSystem.Value;

    static FontLibrary Create()
    {
        var fonts = new FontLibrary();
        var dir = Path.Combine(AppContext.BaseDirectory, "Data", "Fonts");
        fonts.AddFile(Path.Combine(dir, "Lato-Regular.ttf"), "Lato", 400);
        fonts.AddFile(Path.Combine(dir, "Lato-Semibold.ttf"), "Lato", 500, weightMax: 600);
        fonts.AddFile(Path.Combine(dir, "Lato-Bold.ttf"), "Lato", 700, weightMax: 900);
        fonts.AddFile(Path.Combine(dir, "STIXTwoText-Regular.otf"), "STIX Two Text", 400);
        fonts.AddFile(Path.Combine(dir, "STIXTwoText-Italic.otf"), "STIX Two Text", 400, italic: true);
        return fonts;
    }
}
