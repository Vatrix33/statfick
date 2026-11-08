using System.Text;
using System.Text.RegularExpressions;
using Statventure.Core;
using Statventure.Packs;
using Statventure.Platform;
using Statventure.Tests.Infrastructure;
using Statventure.Text;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Tests.Ui;

/// <summary>
/// The engine's own Lato (<see cref="EngineFonts"/>), the BRUTAL splash (<see cref="Splash"/>) and the credits view
/// (<see cref="Credits"/>), M12 step 4 (Docs/Packaging.md): the faces are ui.pak's, the splash's timing and skipping
/// on the document's clock (Q29), the credits' lines against BRUTAL's notice and the shipped licence texts (Q30). How
/// both documents look against Chromium: <see cref="EngineScreensPaintReferenceTests"/>.
/// </summary>
public class EngineScreensTests
{
    static byte[] FromUiPack(string path)
    {
        using var files = new VirtualFileSystem([Mount.Pack(Path.Combine(TestPaths.Packs, "ui.pak"))]);
        return files.ReadAllBytes(path);
    }

    [Fact]
    public void TheEmbeddedFacesAreUiPaksBytes()
    {
        Assert.Equal(FromUiPack("ui/fonts/lato-regular.ttf"), EngineFonts.Regular.ToArray());
        Assert.Equal(FromUiPack("ui/fonts/lato-bold.ttf"), EngineFonts.Bold.ToArray());
        Assert.Equal(FromUiPack("ui/fonts/lato-ofl.txt"), EngineFonts.Licence.ToArray());
        // And the old game's files they were taken from (make-ui-pack.sh's sources).
        Assert.Equal(File.ReadAllBytes(Path.Combine(TestPaths.OldGame, "objects", "fonts", "Lato-Regular.ttf")), EngineFonts.Regular.ToArray());
        Assert.Equal(File.ReadAllBytes(Path.Combine(TestPaths.OldGame, "objects", "fonts", "Lato-Bold.ttf")), EngineFonts.Bold.ToArray());
    }

    [Fact]
    public void AFontLibraryGetsLatoRegularAndBold()
    {
        using var library = new FontLibrary();
        EngineFonts.AddTo(library);
        Assert.Equal(["Lato", "Lato"], library.Faces.Select(f => f.Family));
        var regular = library.Resolve("Lato", 400, false)!.Value;
        var bold = library.Resolve("Lato", 700, false)!.Value;
        Assert.Equal((400, false), (regular.Face.Weight, regular.SyntheticBold));
        Assert.Equal((700, false), (bold.Face.Weight, bold.SyntheticBold));
        Assert.NotEqual(regular.Face, bold.Face);
        // Each face is the file it says: the bold face's widest glyph is wider, its average glyph too.
        Assert.True(bold.Face.AverageCharWidth > regular.Face.AverageCharWidth);
    }

    [Fact]
    public void TheMountGivesThemAtUiPaksPaths()
    {
        using var files = new VirtualFileSystem([EngineFonts.Mount()]);
        Assert.Equal(FromUiPack(EngineFonts.RegularPath), files.ReadAllBytes(EngineFonts.RegularPath));
        Assert.Equal(FromUiPack(EngineFonts.BoldPath), files.ReadAllBytes(EngineFonts.BoldPath));
        Assert.Equal(FromUiPack(EngineFonts.LicencePath), files.ReadAllBytes(EngineFonts.LicencePath));
        // The editor's stylesheet's @font-face rules load them from a document's data.
        using var library = new FontLibrary();
        var document = new UiDocument(library) { Files = files };
        document.AddStylesheet("""
            @font-face { font-family: Lato; src: url("ui/fonts/lato-regular.ttf") format("truetype"); font-weight: 400; }
            @font-face { font-family: Lato; src: url("ui/fonts/lato-bold.ttf") format("truetype"); font-weight: 700; }
            """, "fonts");
        Assert.Equal([400, 700], library.SheetFaces.Select(f => f.Weight).Order());
    }

    // ---------- The splash (Q29) ----------

    [Fact]
    public void TheSplashShowsTheWhiteLogoAtItsApprovedSizeOnTheDarkGround()
    {
        using var splash = new Splash();
        var logo = splash.Document.QuerySelector("img")!;
        Assert.Equal(Splash.LogoPath, logo.GetAttribute("src"));
        Assert.Equal(Splash.Logo.ToArray(), splash.Document.Files!.ReadAllBytes(Splash.LogoPath));
        // BRUTAL's file from ahwoo.com/brutal-branding (commit 5d38373a24), byte for byte: 4096 × 896.
        Assert.Equal("59768c2ab60fc835af29b1d686e5a76604e959fb4e84adf2bfb7fd26e7355d0c", Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(Splash.Logo)));
        var decoded = Imaging.ImageFile.Load(Splash.Logo);
        Assert.Equal((4096, 896), (decoded.Width, decoded.Height));
        Assert.Contains("background: #1a1c1a", Splash.Css);
        Assert.Contains("width: 696px", Splash.Css);
        // Laid out in a big window: 696 CSS px wide, the logo's ratio, centred.
        splash.Document.Layout.Context.ImageSize = _ => new System.Numerics.Vector2(4096, 896);
        splash.Document.SetViewport(1000, 600);
        splash.Document.Update(0);
        var box = logo.GetBoundingClientRect();
        Assert.Equal(696, box.Width, 2);
        Assert.Equal(696f * 896 / 4096, box.Height, 2);
        Assert.Equal((1000 - 696) / 2f, box.X, 2);
        Assert.Equal((600 - 696f * 896 / 4096) / 2, box.Y, 2);
        // A narrower window: scaled down to its width, never up.
        splash.Document.SetViewport(480, 300);
        splash.Document.Update(0.1);
        Assert.Equal(480, logo.GetBoundingClientRect().Width, 2);
    }

    static void Key(UiDocument document) => document.Input.KeyDown("Space", "Space", KeyModifiers.None);

    static void Click(UiDocument document) => document.Input.PointerDown(new System.Numerics.Vector2(10, 10), MouseButton.Left);

    [Fact]
    public void TheSplashLastsTwoSeconds()
    {
        using var splash = new Splash();
        Assert.False(splash.Finished);
        splash.Advance(10);
        Assert.False(splash.Finished);
        splash.Advance(11.999);
        Assert.False(splash.Finished);
        Assert.Equal(1.999, splash.Elapsed, 6);
        splash.Advance(12);
        Assert.True(splash.Finished);
        Assert.False(splash.Skipped);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AKeyOrAClickSkipsItAfterTheFirstSecond(bool key)
    {
        using var splash = new Splash();
        splash.Document.SetViewport(400, 300);
        void Press()
        {
            if (key)
                Key(splash.Document);
            else
                Click(splash.Document);
        }
        // Before it starts and in its first second: nothing.
        Press();
        splash.Advance(5);
        splash.Document.Update(5);
        Press();
        splash.Advance(5.999);
        splash.Document.Update(5.999);
        Press();
        Assert.False(splash.Finished);
        // From the first second on: ended.
        splash.Advance(6);
        splash.Document.Update(6);
        Press();
        Assert.True(splash.Skipped);
        Assert.True(splash.Finished);
        Assert.Equal(1, splash.Elapsed, 6);
    }

    // ---------- The credits (Q30) ----------

    static readonly ProgramInfo Release = new("Statventure.Sandbox", "2026.11.8.1", "0123456789abcdef");

    static string Text(Credits credits) => credits.Document.Body.TextContent;

    [Fact]
    public void TheCreditsNameTheProgramAndItsVersionButNotItsCommit()
    {
        using var credits = new Credits("Statventure Sandbox", Release);
        Assert.Equal("Statventure Sandbox", credits.Document.QuerySelector("h1")!.TextContent);
        Assert.Equal("Version 2026.11.8.1", credits.Document.QuerySelector(".version")!.TextContent);
        Assert.DoesNotContain("0123456789abcdef", Text(credits));
        using var dev = new Credits("Statventure Editor", ProgramInfo.Parse("Statventure.Editor", null));
        Assert.Equal("Version dev", dev.Document.QuerySelector(".version")!.TextContent);
    }

    static string NuGet => Environment.GetEnvironmentVariable("NUGET_PACKAGES") is { Length: > 0 } folder
        ? folder
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

    /// <summary>A file of a NuGet package (the single version the build restored).</summary>
    static string Package(string package, string path) =>
        Path.Combine(Directory.GetDirectories(Path.Combine(NuGet, package)).Single(), path);

    [Fact]
    public void TheCreditsHoldBrutalsNoticeVerbatim()
    {
        // Every BRUTAL package's NOTICE.txt, the one the downloads ship (Q10).
        var notice = File.ReadAllText(Package("ahwoo.brutal.core", "NOTICE.txt"));
        Assert.Equal(notice, Credits.Notice);
        Assert.StartsWith(Credits.BrutalCopyright + "\n", notice);
        using var credits = new Credits("Statventure Sandbox", Release);
        Assert.Equal("Built with BRUTAL", credits.Document.QuerySelectorAll("h2").First().TextContent);
        Assert.Equal(notice, credits.Document.QuerySelector(".notice")!.TextContent);
    }

    /// <summary>
    /// The licence texts the downloads ship (Docs/Packaging.md, Q10), as their sources hold them: <c>licenses/&lt;name&gt;</c>
    /// from the packages in the build's NuGet cache and the old game's font licences (the publish of M12 step 3
    /// copies them unchanged, Tools/Packaging/licenses.txt).
    /// </summary>
    static readonly Dictionary<string, Func<string>> Shipped = new()
    {
        ["licenses/BRUTAL-LICENSE.txt"] = () => Package("ahwoo.brutal.core", "LICENSE.txt"),
        ["licenses/FTL.txt"] = () => Package("ahwoo.brutal.freetype", "LICENSES/FTL.txt"),
        ["licenses/BRUTAL-FREETYPE-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.freetype", "LICENSES/BRUTAL-FREETYPE-THIRD-PARTY-NOTICES.txt"),
        ["licenses/BRUTAL-GLFW-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.glfw", "LICENSES/BRUTAL-GLFW-THIRD-PARTY-NOTICES.txt"),
        ["licenses/BRUTAL-GLI-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.gli", "LICENSES/BRUTAL-GLI-THIRD-PARTY-NOTICES.txt"),
        ["licenses/BRUTAL-GLTF-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.gltf", "LICENSES/BRUTAL-GLTF-THIRD-PARTY-NOTICES.txt"),
        ["licenses/BRUTAL-KTX-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.ktx", "LICENSES/BRUTAL-KTX-THIRD-PARTY-NOTICES.txt"),
        ["licenses/BRUTAL-OPENAL-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.openal", "LICENSES/BRUTAL-OPENAL-THIRD-PARTY-NOTICES.txt"),
        ["licenses/openal-soft-COPYING.txt"] = () => Package("ahwoo.brutal.openal", "LICENSES/openal-soft-COPYING.txt"),
        ["licenses/openal-soft-BSD-3Clause.txt"] = () => Package("ahwoo.brutal.openal", "LICENSES/openal-soft-BSD-3Clause.txt"),
        ["licenses/openal-soft-LICENSE-pffft.txt"] = () => Package("ahwoo.brutal.openal", "LICENSES/openal-soft-LICENSE-pffft.txt"),
        ["licenses/openal-soft-fmt-LICENSE.txt"] = () => Package("ahwoo.brutal.openal", "LICENSES/openal-soft-fmt-LICENSE.txt"),
        ["licenses/openal-soft-gsl-LICENSE.txt"] = () => Package("ahwoo.brutal.openal", "LICENSES/openal-soft-gsl-LICENSE.txt"),
        ["licenses/BRUTAL-STB-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.stb", "LICENSES/BRUTAL-STB-THIRD-PARTY-NOTICES.txt"),
        ["licenses/BRUTAL-VULKAN-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.vulkan", "LICENSES/BRUTAL-VULKAN-THIRD-PARTY-NOTICES.txt"),
        ["licenses/BRUTAL-VULKAN-ABSTRACTIONS-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.vulkan.abstractions", "LICENSES/BRUTAL-VULKAN-ABSTRACTIONS-THIRD-PARTY-NOTICES.txt"),
        ["licenses/BRUTAL-VULKAN-VMA-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.vulkan.vma", "LICENSES/BRUTAL-VULKAN-VMA-THIRD-PARTY-NOTICES.txt"),
        ["licenses/BRUTAL-ZLIB-THIRD-PARTY-NOTICES.txt"] = () => Package("ahwoo.brutal.zlib", "LICENSES/BRUTAL-ZLIB-THIRD-PARTY-NOTICES.txt"),
        ["licenses/HarfBuzzSharp-LICENSE.txt"] = () => Package("harfbuzzsharp", "LICENSE.txt"),
        ["licenses/HarfBuzzSharp-THIRD-PARTY-NOTICES.txt"] = () => Package("harfbuzzsharp.nativeassets.linux", "THIRD-PARTY-NOTICES.txt"),
        ["licenses/dotnet-LICENSE.txt"] = () => Package("microsoft.netcore.app.runtime.linux-x64", "LICENSE.TXT"),
        ["licenses/dotnet-THIRD-PARTY-NOTICES.txt"] = () => Package("microsoft.netcore.app.runtime.linux-x64", "THIRD-PARTY-NOTICES.TXT"),
        ["licenses/Microsoft.Extensions.ObjectPool-THIRD-PARTY-NOTICES.txt"] = () => Package("microsoft.extensions.objectpool", "THIRD-PARTY-NOTICES.TXT"),
        ["licenses/CommunityToolkit.HighPerformance-License.md"] = () => Package("communitytoolkit.highperformance", "License.md"),
        ["licenses/CommunityToolkit.HighPerformance-ThirdPartyNotices.txt"] = () => Package("communitytoolkit.highperformance", "ThirdPartyNotices.txt"),
        ["licenses/Lato-OFL.txt"] = () => Path.Combine(TestPaths.OldGame, "objects", "fonts", "Lato-OFL.txt"),
        ["licenses/STIX-OFL.txt"] = () => Path.Combine(TestPaths.OldGame, "objects", "fonts", "STIX-OFL.txt"),
    };

    /// <summary>A text with its line breaks and runs of spaces made single spaces (the licence texts wrap and align their lines).</summary>
    static string Flat(string text) => Regex.Replace(text.Replace(" * ", " "), @"\s+", " ");

    [Fact]
    public void EveryCopyrightAndNoteLineIsInItsLicenceText()
    {
        Assert.NotEmpty(Credits.Components);
        foreach (var component in Credits.Components)
        {
            Assert.True(Shipped.ContainsKey(component.Text), $"{component.Name}: {component.Text} isn't a shipped licence text");
            // The Lato OFL text's "Ł" is mis-encoded (its UTF-8 read as Latin-1 and written again: U+0106 U+0081); the
            // credits spell the name right.
            var text = Flat(File.ReadAllText(Shipped[component.Text]()).Replace("\u0106\u0081", "Ł"));
            foreach (var line in component.Copyrights)
                Assert.True(text.Contains(Flat(line), StringComparison.Ordinal), $"{component.Name}: \"{line}\" isn't in {component.Text}");
        }
        // FreeType's credit (FTL, section 2) with the year BRUTAL's FreeType gives, and OpenAL Soft's source.
        var freeType = Credits.Components.Single(c => c.Name.StartsWith("FreeType "));
        Assert.Contains("Portions of this software are copyright © 2026 The FreeType Project (https://freetype.org). All rights reserved.", freeType.Notes!);
        Assert.Contains(Flat(freeType.Notes![0]), Flat(File.ReadAllText(Shipped["licenses/BRUTAL-FREETYPE-THIRD-PARTY-NOTICES.txt"]())));
        var openAl = Credits.Components.Single(c => c.Name.StartsWith("OpenAL Soft "));
        Assert.StartsWith("GNU Library General Public License", openAl.Licence);
        var openAlNotice = File.ReadAllText(Shipped["licenses/BRUTAL-OPENAL-THIRD-PARTY-NOTICES.txt"]());
        Assert.Contains("https://github.com/kcat/openal-soft", openAlNotice);
        Assert.Contains("dd4e07de0fe73d8c0326c4502f63e71da8ef268b", openAlNotice);
        Assert.Contains("https://github.com/kcat/openal-soft at commit dd4e07de0fe73d8c0326c4502f63e71da8ef268b", openAl.Notes![0]);
    }

    /// <summary>
    /// The components a BRUTAL package's notices name: their "Name" fields, or, in the notices without them (KTX's,
    /// stb's), their sections' titles (before a " -- "), the appendix aside.
    /// </summary>
    static List<string> NamedIn(string text)
    {
        var names = Regex.Matches(text, @"^  Name +(.+?)(?:,| --|$)", RegexOptions.Multiline).Select(m => m.Groups[1].Value.Trim()).ToList();
        if (names.Count == 0)
            names = Regex.Matches(text, @"^=+\r?\n([^\r\n]+)\r?\n", RegexOptions.Multiline).Select(m => m.Groups[1].Value.Split(" -- ")[0].Trim())
                .Where(n => !n.StartsWith("Appendix", StringComparison.Ordinal)).ToList();
        return names;
    }

    /// <summary>Each name a package's notices give (its start) and the credits' name for it (its start).</summary>
    [Theory]
    [InlineData("licenses/BRUTAL-FREETYPE-THIRD-PARTY-NOTICES.txt", "FreeType=FreeType 2.14.3", "BDF font driver=BDF font driver", "PCF font driver=PCF font driver", "libpng=libpng 1.8.0")]
    [InlineData("licenses/BRUTAL-GLFW-THIRD-PARTY-NOTICES.txt", "GLFW=GLFW 3.5.1", "SDL_GameControllerDB=SDL_GameControllerDB", "10 Wayland protocol descriptions=Wayland protocol descriptions")]
    [InlineData("licenses/BRUTAL-OPENAL-THIRD-PARTY-NOTICES.txt", "OpenAL Soft=OpenAL Soft 1.25.2", "Spherical-Harmonic-Transform=Spherical-Harmonic-Transform", "PFFFT=PFFFT", "{fmt}={fmt} 11.2.0", "GSL: Guidelines Support Library=GSL: Guidelines Support Library 4.2.0")]
    [InlineData("licenses/BRUTAL-GLI-THIRD-PARTY-NOTICES.txt", "GLI=GLI 0.8.3", "OpenGL Mathematics=OpenGL Mathematics (GLM) 0.9.9.9")]
    [InlineData("licenses/BRUTAL-GLTF-THIRD-PARTY-NOTICES.txt", "The glTF 2.0 specification JSON schemas=glTF 2.0 specification JSON schemas")]
    [InlineData("licenses/BRUTAL-VULKAN-THIRD-PARTY-NOTICES.txt", "The Khronos Vulkan API registry and C headers=Vulkan API registry and C headers 1.4.335", "The Khronos Vulkan API specification reference pages=Vulkan API specification reference pages 1.4.335")]
    [InlineData("licenses/BRUTAL-VULKAN-ABSTRACTIONS-THIRD-PARTY-NOTICES.txt", "The Khronos Vulkan API registry and C headers=Vulkan API registry and C headers 1.4.335", "The Khronos Vulkan API specification reference pages=Vulkan API specification reference pages 1.4.335")]
    [InlineData("licenses/BRUTAL-VULKAN-VMA-THIRD-PARTY-NOTICES.txt", "Vulkan Memory Allocator=Vulkan Memory Allocator 3.4.0")]
    [InlineData("licenses/BRUTAL-ZLIB-THIRD-PARTY-NOTICES.txt", "zlib=zlib 1.3.2.1")]
    [InlineData("licenses/BRUTAL-KTX-THIRD-PARTY-NOTICES.txt", "KTX-Software (libktx) 4.4.0=KTX-Software 4.4.0", "Basis Universal 1.16=Basis Universal 1.16", "miniz v1.15=miniz 1.15",
        "Zstandard 1.5.5=Zstandard 1.5.5", "astc-encoder 5.3.0=astc-encoder 5.3.0", "dfdutils=dfdutils", "Vulkan headers=Vulkan headers (in libktx)")]
    [InlineData("licenses/BRUTAL-STB-THIRD-PARTY-NOTICES.txt", "stb=stb_image 2.30 and stb_image_write 1.16")]
    public void EveryComponentAPackagesNoticesNameIsCredited(string text, params string[] pairs)
    {
        var named = NamedIn(File.ReadAllText(Shipped[text]()));
        // The notices name exactly these, in this order (a package that names one more fails here until the credits list it).
        Assert.Equal(pairs.Length, named.Count);
        foreach (var (pair, name) in pairs.Zip(named))
        {
            var (inNotices, inCredits) = (pair.Split('=')[0], pair.Split('=')[1]);
            Assert.StartsWith(inNotices, name);
            Assert.Single(Credits.Components, c => c.Name.StartsWith(inCredits, StringComparison.Ordinal));
        }
    }

    [Fact]
    public void EveryShippedTextIsNamedByTheCredits()
    {
        // BRUTAL's licence by the closing line, every other text by a component (its text or a note).
        var named = Credits.Components.Select(c => c.Text).Concat(Credits.Components.SelectMany(c => c.Notes ?? []).SelectMany(n => Shipped.Keys.Where(n.Contains))).ToHashSet();
        foreach (var text in Shipped.Keys.Where(t => t is not "licenses/BRUTAL-LICENSE.txt"))
            Assert.True(named.Contains(text), $"no component names {text}");
        Assert.Contains("licenses/BRUTAL-LICENSE.txt", Credits.LicencesLine);
        Assert.Contains("BRUTAL License v1.0", Credits.Notice);
    }

    [Fact]
    public void TheCreditsShowEveryComponentWithItsLinesAndWhereTheTextsAre()
    {
        using var credits = new Credits("Statventure Editor", Release);
        var shown = credits.Document.QuerySelectorAll(".component").ToList();
        Assert.Equal(Credits.Components.Count, shown.Count);
        foreach (var (component, element) in Credits.Components.Zip(shown))
        {
            var lines = element.ElementChildren.Select(e => e.TextContent).ToList();
            Assert.Equal([component.Name, component.Licence, .. component.Copyrights, .. component.Notes ?? []], lines);
        }
        Assert.Equal(["Built with BRUTAL", "Components", "Licences"], credits.Document.QuerySelectorAll("h2").Select(h => h.TextContent));
        Assert.Contains(Credits.LicencesLine, Text(credits));
        Assert.Contains("licenses folder", Credits.LicencesLine);
        Assert.Contains("NOTICE.txt", Credits.LicencesLine);
    }
}
