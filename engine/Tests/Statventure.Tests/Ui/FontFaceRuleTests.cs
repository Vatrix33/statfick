using Statventure.Packs;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;
using Statventure.Text;
using Statventure.UI.Css;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// Fonts registered from stylesheets' @font-face rules (Docs/Assets.md, "UI"; Q24, S7-4): the descriptors
/// the engine honours and refuses, the committed ui.pak's faces as the game's rules declare them (equal to
/// <see cref="TestFonts"/>' registration, which mirrors them in code), matching with @font-face families
/// before the library's own, faces shared and given back, a font that is missing or doesn't load named by
/// its sheet, and the document's <see cref="UiDocument.Files"/>.
/// </summary>
public class FontFaceRuleTests
{
    // ---------- Descriptors ----------

    static FontFaceDescriptors Of(string declarations) =>
        FontFaceDescriptors.Of(CssParser.ParseInlineStyle(declarations));

    public static TheoryData<string, FontFaceDescriptors> Honoured => new()
    {
        { "font-family: Lato; src: url(\"ui/fonts/a.ttf\")", new("Lato", "ui/fonts/a.ttf", 400, 400, false) },
        { "font-family: \"STIX Two Text\"; src: url(ui/fonts/a.otf) format(\"opentype\"); font-style: italic", new("STIX Two Text", "ui/fonts/a.otf", 400, 400, true) },
        { "font-family: Big   Old  Face; src: url(\"ui/fonts/a.ttf\") format(truetype)", new("Big Old Face", "ui/fonts/a.ttf", 400, 400, false) },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: 500 600; font-display: swap", new("A", "ui/fonts/a.ttf", 500, 600, false) },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: 900 700", new("A", "ui/fonts/a.ttf", 700, 900, false) },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: bold", new("A", "ui/fonts/a.ttf", 700, 700, false) },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: normal bold; font-style: NORMAL", new("A", "ui/fonts/a.ttf", 400, 700, false) },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: 1 1000; font-display: optional", new("A", "ui/fonts/a.ttf", 1, 1000, false) },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: 300.0", new("A", "ui/fonts/a.ttf", 300, 300, false) },
        { "font-family: A; font-family: B; src: url(\"ui/fonts/a.ttf\"); src: url(\"ui/fonts/b.ttf\")", new("B", "ui/fonts/b.ttf", 400, 400, false) },
    };

    [Theory]
    [MemberData(nameof(Honoured))]
    public void TheDescriptorsTheEngineHonours(string declarations, FontFaceDescriptors expected) => Assert.Equal(expected, Of(declarations));

    public static TheoryData<string, string> Refused => new()
    {
        { "src: url(\"ui/fonts/a.ttf\")", "@font-face without a font-family" },
        { "font-family: A", "@font-face for A without a src" },
        { "font-family: A; src: local(Arial)", "isn't a url() (the engine has no local() fonts)" },
        { "font-family: A; src: url(\"ui/fonts/a.woff2\") format(\"woff2\")", "the format 'woff2' isn't one the engine reads" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"), url(\"ui/fonts/b.ttf\")", "names several sources" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\") tech(variations)", "isn't a url() with an optional format()" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\") format(\"truetype\") format(\"truetype\")", "isn't a url() with an optional format()" },
        { "font-family: A; src: url(\"data:font/ttf;base64,AA==\")", "a data: url isn't taken as a font" },
        { "font-family: A; src: url(\"../a.ttf\")", "url(\"../a.ttf\") isn't a data path" },
        { "font-family: A; src: \"ui/fonts/a.ttf\"", "isn't a url()" },
        { "font-family: serif; src: url(\"ui/fonts/a.ttf\")", "can't be the generic family serif" },
        { "font-family: inherit; src: url(\"ui/fonts/a.ttf\")", "isn't a family name" },
        { "font-family: A 1; src: url(\"ui/fonts/a.ttf\")", "isn't a family name" },
        { "font-family: A, B; src: url(\"ui/fonts/a.ttf\")", "isn't a family name" },
        { "font-family: A,B; src: url(\"ui/fonts/a.ttf\")", "isn't a family name" },
        { "font-family: \"\"; src: url(\"ui/fonts/a.ttf\")", "font-family is an empty string" },
        { "font-family: ; src: url(\"ui/fonts/a.ttf\")", "font-family is empty" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-style: oblique", "isn't one the engine honours (normal, italic)" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-style: italic italic", "isn't one the engine honours (normal, italic)" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: 0", "isn't one or two weights" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: 1001", "isn't one or two weights" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: 450.5", "isn't one or two weights" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: 100 200 300", "isn't one or two weights" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: auto", "isn't one or two weights" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: 400px", "isn't one or two weights" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-weight: bolder", "isn't one or two weights" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-display: soon", "'font-display: soon' isn't a value the engine honours" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); unicode-range: U+0-7F", "the descriptor 'unicode-range' isn't one the engine honours" },
        { "font-family: A; src: url(\"ui/fonts/a.ttf\"); font-stretch: normal", "the descriptor 'font-stretch'" },
        { "font-family: A !important; src: url(\"ui/fonts/a.ttf\")", "'font-family' is !important" },
    };

    [Theory]
    [MemberData(nameof(Refused))]
    public void TheDescriptorsTheEngineRefuses(string declarations, string problem) =>
        Assert.Contains(problem, Assert.Throws<InvalidDataException>(() => Of(declarations)).Message);

    // ---------- The committed pack's faces ----------

    /// <summary>The game's five @font-face rules, naming the fonts by their data paths in ui.pak.</summary>
    static string GameFontFaces()
    {
        var lines = GameStylesheets.CompilableText.Split('\n').Where(l => l.StartsWith("@font-face", StringComparison.Ordinal)).ToList();
        Assert.Equal(5, lines.Count);
        return string.Join("\n", lines);
    }

    /// <summary>A pack holding <paramref name="css"/> compiled at ui/styles/fonts (and <paramref name="extra"/>'s entries), in <paramref name="folder"/>.</summary>
    static string StylePack(TempFolder folder, string css, Action<PackWriter>? extra = null, string name = "styles.pak")
    {
        var writer = new PackWriter();
        writer.AddRecord("ui/styles/fonts", Stylesheet.Compile(css, _ => true).Record);
        extra?.Invoke(writer);
        writer.Write(folder[name]);
        return folder[name];
    }

    static object Describe(FontFace f) =>
        (f.Family, f.Weight, f.WeightMax, f.Italic, f.UnitsPerEm, f.Ascent, f.Descent, f.LineGap, f.XHeight, f.AverageCharWidth,
            f.TypoAscender, f.TypoDescender, f.MaxAdvance, f.BoundsWidth, f.UnderlinePosition, f.UnderlineThickness);

    const string Sample = """
        <p style="font: 400 16px Lato">Regular text, 0123456789 — æøå</p>
        <p style="font: 500 16px Lato">Medium text</p><p style="font: 600 16px Lato">Semibold text</p>
        <p style="font: 700 16px Lato">Bold text</p><p style="font: 900 16px Lato">Black text</p>
        <p style="font: 300 16px Lato">Light text</p><p style="font: italic 400 16px Lato">Oblique Lato</p>
        <p style="font: 400 20px 'STIX Two Text'">A Title</p><p style="font: italic 400 20px 'STIX Two Text'">An italic title</p>
        <p style="font: bold 20px 'STIX Two Text'">A bold title</p><p style="font: 16px Unknown">Default family</p>
        """;

    /// <summary>Where every element's box and every text line is, laid out with <paramref name="fonts"/> (and a document set up by <paramref name="setUp"/>).</summary>
    static List<string> Layout(FontLibrary fonts, Action<UiDocument>? setUp = null)
    {
        var document = new UiDocument(fonts);
        setUp?.Invoke(document);
        Markup.SetInnerHtml(document.Body, Sample);
        document.Update();
        return [.. document.Body.Descendants().Select(e => $"{e.Tag} {e.Box?.ViewportRect}")];
    }

    [Fact]
    public void TheCommittedPacksFacesAreTheOnesTestFontsRegisters()
    {
        using var folder = new TempFolder();
        using var files = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "ui.pak")), Mount.Pack(StylePack(folder, GameFontFaces()))]);
        using var fonts = new FontLibrary();
        var setUp = (UiDocument document) =>
        {
            document.Files = files;
            document.AddStylesheet(Stylesheet.Load(files, "ui/styles/fonts"));
        };
        var laidOut = Layout(fonts, setUp);
        Assert.Empty(fonts.Faces);
        Assert.Equal(TestFonts.Library.Faces.Select(Describe), fonts.SheetFaces.Select(Describe));
        foreach (var family in new[] { "Lato", "lato", "STIX Two Text", "Unknown" })
            foreach (var weight in new[] { 100, 300, 400, 450, 500, 550, 600, 700, 800, 900 })
                foreach (var italic in new[] { false, true })
                {
                    var want = TestFonts.Library.Resolve(family, weight, italic);
                    var have = fonts.Resolve(family, weight, italic);
                    Assert.Equal(want is { } w ? $"{Describe(w.Face)} {w.SyntheticBold} {w.SyntheticOblique}" : "none", have is { } h ? $"{Describe(h.Face)} {h.SyntheticBold} {h.SyntheticOblique}" : "none");
                }
        // Text lays out the same with them as with the faces registered in code.
        Assert.Equal(Layout(TestFonts.Library), laidOut);
    }

    [Fact]
    public void TheGameStylesheetsFacesComeFromItsPack()
    {
        using var fonts = new FontLibrary();
        var document = new UiDocument(fonts) { Files = GameStylesheets.Files };
        document.AddStylesheet(GameStylesheets.Compiled);
        Assert.Equal(TestFonts.Library.Faces.Select(Describe), fonts.SheetFaces.Select(Describe));
        // Without a file system the rules aren't honoured (the game screens' tests take TestFonts' faces).
        using var other = new FontLibrary();
        new UiDocument(other).AddStylesheet(GameStylesheets.Compiled);
        Assert.Empty(other.SheetFaces);
    }

    // ---------- Matching and sharing ----------

    static byte[] Font(string file) => File.ReadAllBytes(GameStylesheets.FontFile(file));

    /// <summary>A loose data folder with the five fonts and a sheet, its own @font-face rules given.</summary>
    static VirtualFileSystem Data(TempFolder folder, string css, string at = "data")
    {
        foreach (var (_, path, file) in GameStylesheets.Fonts)
            folder.Write($"{at}/{path}", Font(file));
        folder.Write($"{at}/ui/styles/fonts.json", Statventure.Records.RecordSerializer.ToJson(Stylesheet.Compile(css, _ => true).Record));
        return new VirtualFileSystem([Mount.Folder(folder[at])]);
    }

    [Fact]
    public void AnAtFontFaceFamilyHidesTheLibrarysOwnFacesOfThatFamily()
    {
        using var folder = new TempFolder();
        using var files = Data(folder, GameFontFaces());
        using var fonts = new FontLibrary();
        // The library's own "Lato" is STIX (1000 units per em, Lato has 2000), its "Other" Lato.
        var own = fonts.Add(Font("STIXTwoText-Regular.otf"), "Lato", 100, weightMax: 900);
        var other = fonts.Add(Font("Lato-Regular.ttf"), "Other");
        var document = new UiDocument(fonts) { Files = files };
        var sheet = Stylesheet.Load(files, "ui/styles/fonts");
        Assert.Same(own, fonts.Resolve("Lato", 400, false)!.Value.Face);
        document.AddStylesheet(sheet);
        foreach (var weight in new[] { 100, 400, 900 })
            Assert.Contains(fonts.Resolve("Lato", weight, true)!.Value.Face, fonts.SheetFaces);
        Assert.Same(other, fonts.Resolve("Other", 400, false)!.Value.Face);
        Assert.Equal(2000, fonts.Resolve("LATO", 400, false)!.Value.Face.UnitsPerEm);
        document.RemoveStylesheet(sheet);
        Assert.Same(own, fonts.Resolve("Lato", 400, false)!.Value.Face);
        Assert.Empty(fonts.SheetFaces);
    }

    [Fact]
    public void TheLaterOfTwoEqualFacesWinsAndOneFontIsOneFace()
    {
        using var folder = new TempFolder();
        using var files = Data(folder, """
            @font-face { font-family: A; src: url("ui/fonts/lato-regular.ttf") }
            @font-face { font-family: A; src: url("ui/fonts/lato-bold.ttf") }
            @font-face { font-family: B; src: url("ui/fonts/lato-bold.ttf") }
            @font-face { font-family: A; src: url("ui/fonts/lato-bold.ttf") }
            """);
        using var fonts = new FontLibrary();
        new UiDocument(fonts) { Files = files }.AddStylesheet(Stylesheet.Load(files, "ui/styles/fonts"));
        // The third rule is the second's face again; the second is A's later face of weight 400.
        Assert.Equal(3, fonts.SheetFaces.Count);
        var bold = fonts.SheetFaces[1];
        Assert.Equal(("A", 400), (bold.Family, bold.Weight));
        // Made from lato-bold.ttf (Lato Bold's average width), not lato-regular.ttf.
        Assert.Equal(TestFonts.Library.Faces[2].AverageCharWidth, bold.AverageCharWidth);
        Assert.NotEqual(TestFonts.Library.Faces[0].AverageCharWidth, bold.AverageCharWidth);
        Assert.Same(bold, fonts.Resolve("A", 400, false)!.Value.Face);
        Assert.Equal("B", fonts.SheetFaces[2].Family);
    }

    [Fact]
    public void FacesAreSharedBySheetsAndGoWhenNoSheetUsesThem()
    {
        using var folder = new TempFolder();
        using var files = Data(folder, GameFontFaces());
        using var fonts = new FontLibrary();
        var sheet = Stylesheet.Load(files, "ui/styles/fonts");
        var first = new UiDocument(fonts) { Files = files };
        var second = new UiDocument(fonts) { Files = files };
        first.AddStylesheet(sheet);
        var faces = fonts.SheetFaces;
        second.AddStylesheet(sheet);
        second.AddStylesheet(Stylesheet.Load(files, "ui/styles/fonts"));
        Assert.Equal(faces, fonts.SheetFaces);
        // A sheet added twice to one document holds its faces once.
        first.AddStylesheet(sheet);
        first.RemoveStylesheet(sheet);
        Assert.Equal(faces, fonts.SheetFaces);
        second.RemoveStylesheet(sheet);
        Assert.Equal(faces, fonts.SheetFaces);
        second.RemoveStylesheet(second.Styles.Sheets[^1]);
        Assert.Empty(fonts.SheetFaces);
        // Made afresh when a sheet needs them again.
        first.AddStylesheet(sheet);
        Assert.Equal(5, fonts.SheetFaces.Count);
        Assert.DoesNotContain(fonts.SheetFaces[0], faces);
        Assert.Throws<InvalidOperationException>(() => fonts.ReleaseSheetFace(faces[0]));
    }

    [Fact]
    public void FacesAcquiredOnSeveralThreadsAtOnceAreOneFace()
    {
        using var fonts = new FontLibrary();
        var data = Font("Lato-Regular.ttf");
        var faces = new FontFace[16];
        Parallel.For(0, faces.Length, new ParallelOptions { MaxDegreeOfParallelism = 8 }, i => faces[i] = fonts.AcquireSheetFace(data, "A"));
        var face = Assert.Single(fonts.SheetFaces);
        Assert.All(faces, f => Assert.Same(face, f));
        for (var i = 0; i < faces.Length - 1; i++)
            fonts.ReleaseSheetFace(face);
        Assert.Single(fonts.SheetFaces);
        fonts.ReleaseSheetFace(face);
        Assert.Empty(fonts.SheetFaces);
        // Other descriptors or other data: another face.
        var a = fonts.AcquireSheetFace(data, "A");
        Assert.NotSame(a, fonts.AcquireSheetFace(data, "A", 700));
        Assert.NotSame(a, fonts.AcquireSheetFace(data, "A", 400, italic: true));
        Assert.NotSame(a, fonts.AcquireSheetFace(data, "A", 400, weightMax: 500));
        Assert.NotSame(fonts.AcquireSheetFace(data, "A", 400, weightMax: 700), fonts.AcquireSheetFace(data, "A", 500, weightMax: 700));
        Assert.NotSame(a, fonts.AcquireSheetFace(data, "B"));
        Assert.NotSame(a, fonts.AcquireSheetFace(Font("Lato-Bold.ttf"), "A"));
        Assert.Same(a, fonts.AcquireSheetFace(data, "A", 400, weightMax: 400));
        Assert.Equal(8, fonts.SheetFaces.Count);
    }

    [Fact]
    public void WithOnlySheetFacesTheDefaultFamilyIsTheFirstSheetFaces()
    {
        using var folder = new TempFolder();
        using var files = Data(folder, GameFontFaces().Replace("font-family: Lato", "font-family: \"Lato UI\""));
        using var fonts = new FontLibrary();
        Assert.Null(fonts.ResolveDefault(400, false));
        new UiDocument(fonts) { Files = files }.AddStylesheet(Stylesheet.Load(files, "ui/styles/fonts"));
        Assert.Equal(("Lato UI", 700), (fonts.ResolveDefault(800, false)!.Value.Face.Family, fonts.ResolveDefault(800, false)!.Value.Face.Weight));
        fonts.DefaultFamily = "STIX Two Text";
        Assert.Equal("STIX Two Text", fonts.ResolveDefault(400, false)!.Value.Face.Family);
        using var withOwn = new FontLibrary();
        withOwn.Add(Font("STIXTwoText-Regular.otf"), "Own");
        new UiDocument(withOwn) { Files = files }.AddStylesheet(Stylesheet.Load(files, "ui/styles/fonts"));
        Assert.Equal("Own", withOwn.ResolveDefault(400, false)!.Value.Face.Family);
    }

    // ---------- Fonts that can't be had ----------

    [Fact]
    public void AMissingFontIsAnErrorNamingTheSheetsPackAndEntry()
    {
        using var folder = new TempFolder();
        var css = """
            @font-face { font-family: A; src: url("ui/fonts/lato-regular.ttf") }
            @font-face { font-family: A; font-weight: 700; src: url("ui/fonts/gone.ttf") }
            """;
        var pack = StylePack(folder, css, writer => writer.Add("ui/fonts/lato-regular.ttf", Font("Lato-Regular.ttf")));
        using var files = new VirtualFileSystem([Mount.Pack(pack)]);
        using var fonts = new FontLibrary();
        var document = new UiDocument(fonts) { Files = files };
        var sheet = Stylesheet.Load(files, "ui/styles/fonts");
        var problem = Assert.Throws<DataFileException>(() => document.AddStylesheet(sheet));
        Assert.Equal((pack, "ui/styles/fonts"), (problem.File, problem.Entry));
        Assert.Equal("the @font-face font 'ui/fonts/gone.ttf' of A can't be read: No 'ui/fonts/gone.ttf' in the game's data", problem.Problem);
        Assert.IsType<FileNotFoundException>(problem.InnerException);
        // Nothing changed: the first face was given back, the sheet isn't there.
        Assert.Empty(fonts.SheetFaces);
        Assert.DoesNotContain(sheet, document.Styles.Sheets);
    }

    [Fact]
    public void AFontThatDoesntLoadOrIsARecordIsAnErrorNamingTheSheet()
    {
        using var folder = new TempFolder();
        var pack = StylePack(folder, """
            @font-face { font-family: A; src: url("ui/fonts/bad.ttf") }
            """, writer => writer.Add("ui/fonts/bad.ttf", "not a font"u8));
        using var files = new VirtualFileSystem([Mount.Pack(pack)]);
        using var fonts = new FontLibrary();
        var problem = Assert.Throws<DataFileException>(() => new UiDocument(fonts) { Files = files }.AddStylesheet(Stylesheet.Load(files, "ui/styles/fonts")));
        Assert.Equal((pack, "ui/styles/fonts"), (problem.File, problem.Entry));
        Assert.StartsWith("the @font-face font 'ui/fonts/bad.ttf' of A doesn't load: FreeType FT_New_Memory_Face failed", problem.Problem);
        var record = StylePack(folder, """
            @font-face { font-family: A; src: url("ui/fonts/record") }
            """, writer => writer.AddRecord("ui/fonts/record", new StylesheetRecord([], [], [])), "record.pak");
        using var withRecord = new VirtualFileSystem([Mount.Pack(record)]);
        var recordProblem = Assert.Throws<DataFileException>(() => new UiDocument(fonts) { Files = withRecord }.AddStylesheet(Stylesheet.Load(withRecord, "ui/styles/fonts")));
        Assert.Equal((record, "ui/styles/fonts"), (recordProblem.File, recordProblem.Entry));
        Assert.Contains("can't be read: ", recordProblem.Problem);
        Assert.Contains("a record, not a file", recordProblem.Problem);
        Assert.Empty(fonts.SheetFaces);
    }

    [Fact]
    public void ASheetParsedFromTextIsNamedByItsName()
    {
        using var folder = new TempFolder();
        using var files = Data(folder, "");
        using var fonts = new FontLibrary();
        var document = new UiDocument(fonts) { Files = files };
        document.AddStylesheet("@font-face { font-family: A; src: url(\"ui/fonts/lato-bold.ttf\") }", "mine");
        Assert.Equal("A", Assert.Single(fonts.SheetFaces).Family);
        var problem = Assert.Throws<InvalidDataException>(() => document.AddStylesheet("@font-face { font-family: B; src: url(\"ui/fonts/gone.ttf\") }", "theirs"));
        Assert.StartsWith("the stylesheet 'theirs': the @font-face font 'ui/fonts/gone.ttf' of B can't be read", problem.Message);
        var old = Assert.Throws<InvalidDataException>(() => document.AddStylesheet(GameStylesheets.Text, "styles.css"));
        Assert.StartsWith("the stylesheet 'styles.css': @font-face: url(\"objects/fonts/Lato-Regular.ttf\") isn't a data path", old.Message);
        Assert.Single(fonts.SheetFaces);
    }

    [Fact]
    public void AFontOfASheetWhoseDataIsGoneNamesItsPath()
    {
        using var folder = new TempFolder();
        var pack = StylePack(folder, "@font-face { font-family: A; src: url(\"ui/fonts/gone.ttf\") }");
        var files = new VirtualFileSystem([Mount.Pack(pack)]);
        var sheet = Stylesheet.Load(files, "ui/styles/fonts");
        files.Dispose();
        using var others = Data(folder, "");
        using var fonts = new FontLibrary();
        var problem = Assert.Throws<InvalidDataException>(() => new UiDocument(fonts) { Files = others }.AddStylesheet(sheet));
        Assert.StartsWith("the stylesheet 'ui/styles/fonts': the @font-face font 'ui/fonts/gone.ttf' of A can't be read", problem.Message);
    }

    // ---------- The document's file system ----------

    [Fact]
    public void FacesFollowTheDocumentsFiles()
    {
        using var folder = new TempFolder();
        using var files = Data(folder, GameFontFaces());
        folder.Write("empty/x", "x"u8.ToArray());
        using var empty = new VirtualFileSystem([Mount.Folder(folder["empty"])]);
        using var fonts = new FontLibrary();
        var document = new UiDocument(fonts);
        var sheet = Stylesheet.Load(files, "ui/styles/fonts");
        document.AddStylesheet(sheet);
        Assert.Empty(fonts.SheetFaces);
        document.Files = files;
        Assert.Equal(5, fonts.SheetFaces.Count);
        var faces = fonts.SheetFaces;
        document.Files = files;
        Assert.Equal(faces, fonts.SheetFaces);
        // A file system without the fonts: refused, and nothing changes.
        Assert.Throws<DataFileException>(() => document.Files = empty);
        Assert.Same(files, document.Files);
        Assert.Equal(faces, fonts.SheetFaces);
        var other = new UiDocument(fonts);
        other.AddStylesheet(sheet);
        other.AddStylesheet("@font-face { font-family: Z; src: url(\"ui/fonts/gone.ttf\") }", "gone");
        Assert.Throws<InvalidDataException>(() => other.Files = files);
        Assert.Null(other.Files);
        document.Files = null;
        Assert.Empty(fonts.SheetFaces);
        document.Files = files;
        Assert.Equal(5, fonts.SheetFaces.Count);
    }

    [Fact]
    public void TextIsLaidOutAgainWhenFacesComeAndGo()
    {
        using var folder = new TempFolder();
        using var files = Data(folder, GameFontFaces());
        using var fonts = new FontLibrary();
        // The library's own Lato is STIX: text in Lato measures differently once the sheet's Lato comes.
        fonts.Add(Font("STIXTwoText-Regular.otf"), "Lato", 100, weightMax: 900);
        var withOwn = Layout(fonts);
        var sheet = Stylesheet.Load(files, "ui/styles/fonts");
        var document = new UiDocument(fonts);
        document.AddStylesheet(sheet);
        Markup.SetInnerHtml(document.Body, Sample);
        document.Update();
        string[] Boxes() => [.. document.Body.Descendants().Select(e => $"{e.Tag} {e.Box?.ViewportRect}")];
        Assert.Equal(withOwn, Boxes());
        document.Files = files;
        document.Update();
        using var sheetOnly = new FontLibrary();
        var withSheet = Layout(sheetOnly, d =>
        {
            d.Files = files;
            d.AddStylesheet(sheet);
        });
        Assert.NotEqual(withOwn, withSheet);
        Assert.Equal(withSheet, Boxes());
        document.RemoveStylesheet(sheet);
        document.Update();
        Assert.Equal(withOwn, Boxes());
        document.AddStylesheet(sheet);
        document.Update();
        Assert.Equal(withSheet, Boxes());
    }
}
