using System.Text;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Records;
using Statventure.Tests.Canvas;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Packs;
using Statventure.UI;
using Statventure.UI.Css;
using Statventure.UI.Dom;
using Statventure.UI.Style;

namespace Statventure.Tests.Ui;

/// <summary>
/// The images and SVG text a window's document reads by url (Docs/Assets.md, "UI"; S7-1, S7-4, Q25): the
/// game's callbacks first, else the document's data, a PNG decoded and an SVG read as UTF-8 text; what isn't
/// there or doesn't decode shows nothing and is reported once; damaged data stays an error. And the
/// committed ui.pak (Q1, S7-5, S7-6).
/// </summary>
public class UiResourcesTests
{
    static byte[] Png(TempFolder folder)
    {
        var image = new RgbaImage(3, 2);
        for (var i = 0; i < image.Pixels.Length; i++)
            image.Pixels[i] = (byte)(i * 11);
        ImageFile.SavePng(folder["made.png"], image);
        return File.ReadAllBytes(folder["made.png"]);
    }

    sealed class Setup : IDisposable
    {
        public readonly TempFolder Folder = new();
        public readonly VirtualFileSystem Files;
        public readonly UiDocument Document = new(TestFonts.Library);
        public readonly List<string> Reports = [];
        public readonly UiResources Resources;
        public readonly byte[] PngBytes;

        public Setup()
        {
            PngBytes = Png(Folder);
            Folder.Write("data/ui/images/a.png", PngBytes);
            Folder.Write("data/ui/images/broken.png", PngBytes[..40]);
            Folder.Write("data/ui/images/jpeg.png", [0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3]);
            Folder.Write("data/ui/images/a.svg", "<svg width=\"4\" height=\"2\"></svg>");
            Folder.Write("data/ui/images/bom.svg", [0xEF, 0xBB, 0xBF, .. "<svg>ä</svg>"u8]);
            Folder.Write("data/ui/images/latin1.svg", [.. "<svg>"u8, 0xE4, .. "</svg>"u8]);
            Folder.Write("data/ui/images/record.json", RecordSerializer.ToJson(new StylesheetRecord([], [], [])));
            Files = new VirtualFileSystem([Mount.Folder(Folder["data"])]);
            Document.Files = Files;
            Resources = new UiResources(Document, Reports.Add);
        }

        public void Dispose()
        {
            Files.Dispose();
            Folder.Dispose();
        }
    }

    [Fact]
    public void APngIsDecodedFromTheDocumentsData()
    {
        using var setup = new Setup();
        var image = setup.Resources.Image("ui/images/a.png")!;
        var expected = ImageFile.Load(setup.PngBytes);
        Assert.Equal((3, 2), (image.Width, image.Height));
        Assert.Equal(expected.Pixels, image.Pixels);
        Assert.Equal(Enumerable.Range(0, 24).Select(i => (byte)(i * 11)), image.Pixels);
        Assert.Empty(setup.Reports);
    }

    [Fact]
    public void AnSvgIsReadAsUtf8Text()
    {
        using var setup = new Setup();
        Assert.Equal("<svg width=\"4\" height=\"2\"></svg>", setup.Resources.Text("ui/images/a.svg"));
        Assert.Equal("<svg>ä</svg>", setup.Resources.Text("ui/images/bom.svg"));
        // An SVG isn't an image to decode: the painter draws it from its text.
        Assert.Null(setup.Resources.Image("ui/images/a.svg"));
        Assert.Null(setup.Resources.Image("data:image/svg+xml,%3Csvg%3E%3C/svg%3E"));
        Assert.Empty(setup.Reports);
    }

    [Fact]
    public void TheGamesCallbacksComeFirst()
    {
        using var setup = new Setup();
        var given = new RgbaImage(1, 1);
        setup.Resources.LoadImage = url => url is "ui/images/a.png" or "game:cursor" ? given : null;
        setup.Resources.LoadText = url => url == "ui/images/a.svg" ? "given" : null;
        Assert.Same(given, setup.Resources.Image("ui/images/a.png"));
        Assert.Same(given, setup.Resources.Image("game:cursor"));
        Assert.Equal("given", setup.Resources.Text("ui/images/a.svg"));
        // Nothing given: the data.
        Assert.Equal("<svg>ä</svg>", setup.Resources.Text("ui/images/bom.svg"));
        setup.Folder.Write("data/ui/images/b.png", setup.PngBytes);
        setup.Files.Reload();
        Assert.Equal(3, setup.Resources.Image("ui/images/b.png")!.Width);
        // Without data only the callbacks.
        var bare = new UiResources(new UiDocument(TestFonts.Library), setup.Reports.Add) { LoadImage = _ => given };
        Assert.Same(given, bare.Image("anything"));
        Assert.Null(new UiResources(new UiDocument(TestFonts.Library), setup.Reports.Add).Image("ui/images/a.png"));
        Assert.Null(new UiResources(new UiDocument(TestFonts.Library), setup.Reports.Add).Text("ui/images/a.svg"));
        Assert.Empty(setup.Reports);
    }

    public static TheoryData<string, bool, string> Unusable => new()
    {
        { "ui/images/gone.png", false, "'ui/images/gone.png' isn't in the game's data: it shows nothing" },
        { "ui/images/broken.png", false, "'ui/images/broken.png' doesn't decode as a PNG image: it shows nothing" },
        { "ui/images/jpeg.png", false, "'ui/images/jpeg.png' isn't a PNG image: it shows nothing" },
        { "ui/images/a.svg.png", false, "'ui/images/a.svg.png' isn't in the game's data: it shows nothing" },
        { "ui/images/record", false, "'ui/images/record' is a record, not a file: it shows nothing" },
        { "../images/a.png", false, "'../images/a.png' isn't a data path (the name '..' ends in a dot): it shows nothing" },
        { "objects/Race-Elf.png", false, "'objects/Race-Elf.png' isn't a data path ('R' isn't allowed" },
        { "data:image/png;base64,AA==", false, "'data:image/png;base64,AA==' is a data: url of a kind the engine doesn't decode (only SVG): it shows nothing" },
        { "ui/images/gone.svg", true, "'ui/images/gone.svg' isn't in the game's data: it shows nothing" },
        { "ui/images/latin1.svg", true, "'ui/images/latin1.svg' isn't UTF-8 text: it shows nothing" },
        { "ui/images/record", true, "'ui/images/record' is a record, not a file: it shows nothing" },
    };

    [Theory]
    [MemberData(nameof(Unusable))]
    public void WhatCantBeShownIsReportedOnce(string url, bool text, string report)
    {
        using var setup = new Setup();
        for (var i = 0; i < 3; i++)
            Assert.Null(text ? setup.Resources.Text(url) : setup.Resources.Image(url));
        var line = Assert.Single(setup.Reports);
        Assert.StartsWith(report, line);
    }

    [Fact]
    public void DamagedDataIsAnError()
    {
        using var folder = new TempFolder();
        var png = Png(folder);
        var writer = new PackWriter();
        writer.Add("ui/images/a.png", png);
        writer.Write(folder["ui.pak"]);
        using (var pack = PackFile.Open(folder["ui.pak"]))
        {
            var bytes = File.ReadAllBytes(folder["ui.pak"]);
            bytes[pack.Entries[0].Offset + pack.Entries[0].StoredLength / 2] ^= 0x10;
            File.WriteAllBytes(folder["damaged.pak"], bytes);
        }
        using var files = new VirtualFileSystem([Mount.Pack(folder["damaged.pak"])]);
        var reports = new List<string>();
        var resources = new UiResources(new UiDocument(TestFonts.Library) { Files = files }, reports.Add);
        Assert.Equal("ui/images/a.png", Assert.Throws<DataFileException>(() => resources.Image("ui/images/a.png")).Entry);
        Assert.Empty(reports);
    }

    // ---------- The committed ui.pak ----------

    static string CommittedPack => Path.Combine(TestPaths.Packs, "ui.pak");

    /// <summary>The pack's files and the old game's files they are (S7-5, S7-6).</summary>
    public static readonly (string Path, string File)[] Contents =
    [
        ("ui/fonts/lato-bold.ttf", "Lato-Bold.ttf"),
        ("ui/fonts/lato-ofl.txt", "Lato-OFL.txt"),
        ("ui/fonts/lato-regular.ttf", "Lato-Regular.ttf"),
        ("ui/fonts/lato-semibold.ttf", "Lato-Semibold.ttf"),
        ("ui/fonts/stix-ofl.txt", "STIX-OFL.txt"),
        ("ui/fonts/stixtwotext-italic.otf", "STIXTwoText-Italic.otf"),
        ("ui/fonts/stixtwotext-regular.otf", "STIXTwoText-Regular.otf"),
    ];

    /// <summary>The port's stylesheets in the pack (GM1-P8): each compiled from engine/Assets/UI/styles/&lt;name&gt;.css.</summary>
    public static readonly string[] Stylesheets = ["game"];

    static string StylesheetSource(string name) => Path.Combine(TestPaths.Packs, "..", "UI", "styles", name + ".css");

    [Fact]
    public void TheCommittedPackHoldsTheOldGamesFiveFontsTheirLicencesAndThePortsStylesheets()
    {
        using var pack = PackFile.Open(CommittedPack);
        Assert.Equal(Contents.Select(c => (c.Path, false)).Concat(Stylesheets.Select(s => ($"ui/styles/{s}", true))).Order(),
            pack.Entries.Select(e => (e.Path, e.IsRecord)).Order());
        foreach (var (path, file) in Contents)
        {
            var content = pack.Read(pack.Find(path)!);
            Assert.Equal(File.ReadAllBytes(Path.Combine(TestPaths.OldGame, "objects", "fonts", file)), content);
            // The tests' own copies are the same files.
            Assert.Equal(File.ReadAllBytes(GameStylesheets.FontFile(file)), content);
        }
        // The licences are text the dump shows (S3-2).
        Assert.Contains("SIL Open Font License", Encoding.UTF8.GetString(pack.Read(pack.Find("ui/fonts/lato-ofl.txt")!)));
        Assert.Contains("\"text\"", PackDump.ToText(pack, RecordTypes.Engine));
    }

    /// <summary>Packing the committed pack's extraction again gives the same bytes reusing its entries, and the same entries compressed afresh.</summary>
    [Fact]
    public void TheCommittedPackRepacksToTheSameBytes()
    {
        using var folder = new TempFolder();
        using var committed = PackFile.Open(CommittedPack);
        committed.ExtractTo(folder["extracted"]);
        var reused = new PackWriter();
        reused.AddFolder(folder["extracted"]);
        using (var output = new MemoryStream())
        {
            reused.WriteTo(output, committed);
            Assert.Equal(File.ReadAllBytes(CommittedPack), output.ToArray());
        }
        var fresh = new PackWriter();
        fresh.AddFolder(folder["extracted"]);
        fresh.Write(folder["fresh.pak"]);
        using var again = PackFile.Open(folder["fresh.pak"]);
        Assert.Equal(committed.Entries.Select(e => (e.Path, e.IsRecord, e.ContentLength, e.HashHex)), again.Entries.Select(e => (e.Path, e.IsRecord, e.ContentLength, e.HashHex)));
    }

    /// <summary>
    /// The port's stylesheet (GM1 step 1a): <c>ui/styles/game</c> loads from the committed pack and is the compile of the
    /// committed source, engine/Assets/UI/styles/game.css, byte for byte in its record and part for part as a sheet
    /// (Tools/Scripts/make-ui-pack.sh made the pack from it; run it again when the source changes).
    /// </summary>
    [Fact]
    public void ThePortsStylesheetLoadsFromThePackAndIsItsSourcesCompile()
    {
        using var files = new VirtualFileSystem([Mount.Pack(CommittedPack)]);
        foreach (var name in Stylesheets)
        {
            var loaded = Stylesheet.Load(files, $"ui/styles/{name}");
            var compiled = Stylesheet.Compile(File.ReadAllText(StylesheetSource(name)), path => files.Exists(path));
            Assert.Empty(compiled.Problems);
            Assert.Equal(RecordSerializer.ToBinary(compiled.Record), RecordSerializer.ToBinary(files.Load<StylesheetRecord>($"ui/styles/{name}")));
            Assert.Equal(StylesheetText.Of(Stylesheet.FromRecord(compiled.Record)), StylesheetText.Of(loaded));
        }
    }

    /// <summary>The game sheet's source is today's old stylesheet with its five @font-face urls as ui.pak's data paths (when it was made: the port's own file since).</summary>
    [Fact]
    public void TheGameSheetsSourceStartedAsTheOldGamesStylesheet()
    {
        var source = File.ReadAllText(StylesheetSource("game"));
        foreach (var (url, path, _) in GameStylesheets.Fonts)
        {
            Assert.DoesNotContain(url, source);
            Assert.Single(source.Split($"url(\"{path}\")").Skip(1));
        }
        Assert.Contains("@property --exotic-tone", source);
    }
}

/// <summary>A window's host reads its document's images and SVG text through <see cref="UiResources"/> (a real window, hidden: skipped without a display).</summary>
[Collection("GPU")]
public class UiHostResourceTests(GpuFixture gpu)
{
    static bool HasDisplay => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"));

    [Fact]
    public void TheHostReadsImagesAndSvgTextFromTheDocumentsData()
    {
        if (!HasDisplay)
            return;
        using var folder = new TempFolder();
        var image = new RgbaImage(5, 4);
        ImageFile.SavePng(folder["data/ui/images/a.png"], image);
        folder.Write("data/ui/images/b.svg", "<svg width=\"8\" height=\"6\"></svg>");
        using var files = new VirtualFileSystem([Mount.Folder(folder["data"])]);
        var document = new UiDocument(TestFonts.Library) { Files = files };
        using var window = Statventure.Platform.Window.Create(new Statventure.Platform.WindowOptions { Title = "Statventure UI resources test", Width = 64, Height = 48, Visible = false });
        using var host = new UiHost(window, gpu.Device, document);
        Assert.Equal(new System.Numerics.Vector2(5, 4), document.Layout.Context.ImageSize!("ui/images/a.png"));
        Assert.Null(document.Layout.Context.ImageSize!("ui/images/gone.png"));
        Assert.Equal("<svg width=\"8\" height=\"6\"></svg>", host.Painter.TextResource!("ui/images/b.svg"));
        // The game's callbacks come first.
        host.LoadImage = url => url == "game:cursor" ? new RgbaImage(2, 2) : null;
        host.LoadText = url => url == "ui/images/c.svg" ? "<svg/>" : null;
        Assert.Equal(new System.Numerics.Vector2(2, 2), document.Layout.Context.ImageSize!("game:cursor"));
        Assert.Equal("<svg/>", host.Painter.TextResource!("ui/images/c.svg"));
        Assert.NotNull(host.LoadImage);
        Assert.NotNull(host.LoadText);
    }
}
