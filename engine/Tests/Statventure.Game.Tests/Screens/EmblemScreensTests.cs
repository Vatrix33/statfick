using Statventure.Game.Data.Looks;
using Statventure.Game.Screens.Races;
using Statventure.Game.Screens.Sheet;
using Statventure.UI.Dom;

namespace Statventure.Game.Tests;

/// <summary>
/// The race emblems' pictures in the game's screens (GM2 step 10; Q36: pictures where the emblem is small): the sheet's seal
/// and the carousel's medallion and strip show the race's emblem picture where the ✦ stood (steps 7 and 8 left them the
/// ✦), each the picture of the size nearest its box in device pixels (<see cref="EmblemPictures"/>: chosen again when the
/// window's width, its scale or the page's zoom changes the box), the ✦ staying for a race with no emblem or an emblem
/// with no pictures, the medallion's picture giving way to a live emblem.
/// </summary>
public class EmblemScreensTests
{
    static readonly string[] Playable = ["human", "elf", "dark-elf", "dwarf", "hobbit", "lizardman", "half-beast", "goliath", "merfolk"];

    /// <summary>The pictures' sizes are every place a screen shows an emblem at the game's two scales, rounded.</summary>
    [Fact]
    public void ThePicturesSizesAreThePlacesAtBothScales()
    {
        Assert.Equal([36, 61, 62, 89, 94, 103, 114, 148, 156, 189], EmblemPictures.Sizes);
        Assert.Equal(10, EmblemPictures.Places.Count);
        Assert.All(EmblemPictures.Places.GroupBy(p => p.Place), g => Assert.Equal([1f, 1.65f], g.Select(p => p.Scale)));
        Assert.Equal("ui/emblems/dark-elf-156.png", EmblemPictures.PathOf("dark-elf", 156));
    }

    /// <summary>The nearest picture by the ratio of sizes, the larger on a tie, the smallest and largest at the ends.</summary>
    [Theory]
    [InlineData(114, 114)]
    [InlineData(113, 114)]
    [InlineData(115, 114)]
    [InlineData(136.5f, 148)]
    [InlineData(129.8f, 114)]
    [InlineData(130.0f, 148)]
    [InlineData(10, 36)]
    [InlineData(46.8f, 36)]
    [InlineData(46.9f, 61)]
    [InlineData(61.4f, 61)]
    [InlineData(61.6f, 62)]
    [InlineData(1000, 189)]
    public void ThePictureDrawnIsTheNearestBySize(float devicePixels, int size) => Assert.Equal(size, EmblemPictures.Nearest(devicePixels));

    /// <summary>
    /// The sheet's seal shows the hero's race's picture, 72 % of the seal button's content box (172 px, 138 at 840 px or less,
    /// 100 at 650 or less: game.css; less its border and the button's padding), drawn as wide as that box, the picture of that
    /// size in device pixels: at the game's first window and at its smallest, at 1 and 1.65, and a page narrower than 650 px
    /// (a zoomed page). As Chromium lays it out (measured on the sheet's fixtures: 113.75 × 120.953 and 89.266 × 96.469 CSS px
    /// at 1, 114.328 and 89.848 wide at 1.65).
    /// </summary>
    [Theory]
    [InlineData(1440, 1f, 113.76f, 114)]
    [InlineData(1440, 1.65f, 188.64f, 189)]
    [InlineData(820, 1f, 89.28f, 89)]
    [InlineData(820, 1.65f, 148.25f, 148)]
    [InlineData(600, 1f, 61.92f, 62)]
    [InlineData(600, 1.65f, 103.10f, 103)]
    public void TheSheetsSealShowsThePictureOfItsSize(float width, float scale, float drawn, int size)
    {
        using var s = new ScreensHarness(width, 960, scale);
        SheetHeroes.OpenPlayed(s);
        var image = s.Q("#race-icon");
        Assert.False(image.Hidden);
        Assert.True(s.Q(".hero-sigil-fallback").Hidden);
        Assert.Equal($"ui/emblems/human-{size}.png", image.GetAttribute("src"));
        var box = image.Box!;
        Assert.Equal(drawn, box.ContentWidth, 0.05f);
        Assert.True(box.ContentHeight > box.ContentWidth);
        Assert.Contains(EmblemPictures.Places, p => p.Scale == scale && MathF.Abs(p.Size - drawn) < 1e-3f);
        Assert.Empty(s.H.Reported);
    }

    /// <summary>A change of the page's zoom (or the window's scale) changes the seal's box in device pixels, and the picture follows; so does another race.</summary>
    [Fact]
    public void ThePictureFollowsTheZoomAndTheRace()
    {
        using var s = new ScreensHarness(1440, 960);
        SheetHeroes.OpenPlayed(s);
        Assert.Equal("ui/emblems/human-114.png", s.Q("#race-icon").GetAttribute("src"));
        s.H.Document.SetZoom(1, 1.2f);
        s.H.Frame();
        Assert.Equal("ui/emblems/human-148.png", s.Q("#race-icon").GetAttribute("src"));
        s.H.Document.SetZoom(1.65f, 1);
        s.H.Frame();
        Assert.Equal("ui/emblems/human-189.png", s.Q("#race-icon").GetAttribute("src"));
        SheetHeroes.OpenNew(s);
        Assert.Equal("ui/emblems/elf-189.png", s.Q("#race-icon").GetAttribute("src"));
    }

    /// <summary>An emblem the data has no pictures for (a mod's race) shows none: the image hidden with no picture; no emblem the same.</summary>
    [Fact]
    public void AnEmblemWithoutPicturesShowsNone()
    {
        using var h = new GameUiHarness(800, 600);
        var pictures = new EmblemPictures(h.Document);
        var image = h.Document.Body.Append("img");
        image.SetAttribute("style", "display: block; width: 36px; height: 36px");
        Assert.True(pictures.Show(image, "elf"));
        h.Frame();
        Assert.Equal(("ui/emblems/elf-36.png", false), (image.GetAttribute("src"), image.Hidden));
        Assert.False(pictures.Show(image, "a-mods-emblem"));
        Assert.Equal((null, true, null), (image.GetAttribute("src"), image.Hidden, image.GetAttribute("data-emblem")));
        Assert.False(pictures.Show(image, null));
        // A frame while hidden: its box is none, which chooses no size.
        h.Frame();
        Assert.False(EmblemPictures.Has(h.Document.Files, "a-mods-emblem"));
        Assert.False(EmblemPictures.Has(null, "elf"));
        Assert.True(EmblemPictures.Has(h.Document.Files, "elf"));
        // Shown again: its picture at once, the size it last had.
        Assert.True(pictures.Show(image, "merfolk"));
        Assert.Equal("ui/emblems/merfolk-36.png", image.GetAttribute("src"));
        Assert.Equal(1, pictures.Following);
        // The page zoomed: the same CSS box, twice the device pixels, the picture of that size; and back.
        h.Document.SetZoom(1, 2);
        h.Frame();
        Assert.Equal($"ui/emblems/merfolk-{EmblemPictures.Nearest(72)}.png", image.GetAttribute("src"));
        h.Document.SetZoom(1, 1);
        h.Frame();
        Assert.Equal("ui/emblems/merfolk-36.png", image.GetAttribute("src"));
        // Forgotten (taken out of the page): no longer followed, its size forgotten.
        pictures.Forget(image);
        Assert.Equal(0, pictures.Following);
        h.Document.SetZoom(2, 1);
        h.Frame();
        Assert.Equal("ui/emblems/merfolk-36.png", image.GetAttribute("src"));
        image.RemoveAttribute("src");
        Assert.True(pictures.Show(image, "elf"));
        Assert.Null(image.GetAttribute("src"));
        h.Frame();
        Assert.Equal($"ui/emblems/elf-{EmblemPictures.Nearest(72)}.png", image.GetAttribute("src"));
    }

    /// <summary>An emblem with pictures at only some sizes (a mod's, made by hand) counts as having none: every size or the ✦.</summary>
    [Fact]
    public void AnEmblemWithSomeSizesMissingHasNoPictures()
    {
        using var folder = new ScratchFolder();
        Directory.CreateDirectory(folder["data/ui/emblems"]);
        foreach (var size in EmblemPictures.Sizes.Skip(1))
            File.Copy(Path.Combine(EmblemPicturesTests.Folder, $"elf-{size}.png"), Path.Combine(folder["data/ui/emblems"], $"partial-{size}.png"));
        using var data = new Statventure.Packs.VirtualFileSystem([Statventure.Packs.Mount.Folder(folder["data"])]);
        Assert.False(EmblemPictures.Has(data, "partial"));
        File.Copy(Path.Combine(EmblemPicturesTests.Folder, $"elf-{EmblemPictures.Sizes[0]}.png"), Path.Combine(folder["data/ui/emblems"], $"partial-{EmblemPictures.Sizes[0]}.png"));
        using var whole = new Statventure.Packs.VirtualFileSystem([Statventure.Packs.Mount.Folder(folder["data"])]);
        Assert.True(EmblemPictures.Has(whole, "partial"));
    }

    /// <summary>
    /// The carousel: every playable race's medallion in the strip shows its picture (the medallion's inside, 36 px) where the
    /// ✦ stood, the shown race's medallion its picture (94 px) with the ✦ hidden; browsing changes it at once; at 1.65 the
    /// pictures of those sizes there.
    /// </summary>
    [Theory]
    [InlineData(1f, 36, 94)]
    [InlineData(1.65f, 61, 156)]
    public void TheCarouselShowsThePicturesWhereTheStarStood(float scale, int strip, int medallion)
    {
        using var s = new HeroScreensHarness(scale: scale);
        s.NewHero();
        s.Run(HeroFlow.RacesCommand);
        var items = s.Document.QuerySelectorAll("#races-strip .races-strip-item").ToList();
        Assert.Equal(Playable, items.Select(i => i.GetAttribute("data-race")));
        foreach (var item in items)
        {
            var picture = Assert.Single(item.ElementChildren);
            Assert.Equal(("img", "races-strip-picture", ""), (picture.Tag, picture.ClassName, picture.GetAttribute("alt")));
            Assert.Equal($"ui/emblems/{item.GetAttribute("data-emblem")}-{strip}.png", picture.GetAttribute("src"));
            Assert.True(item.HasClass("has-picture"));
            // The medallion's inside (38 px less its 1 px border, a whole device pixel at 1.65), as Chromium lays it out (36 and 36.78 CSS px).
            Assert.Equal(scale == 1 ? 36 : 60.7f, picture.Box!.ContentWidth, 0.05f);
            Assert.Equal(scale == 1 ? 36 : 60.7f, picture.Box!.ContentHeight, 0.05f);
        }
        var shown = s.Q("#races-emblem-picture");
        Assert.Equal(($"ui/emblems/human-{medallion}.png", false), (shown.GetAttribute("src"), shown.Hidden));
        // 96 px less its border (Chromium: 94 and 94.782 CSS px): the medallion's inside exactly (it is masked in the
        // screens' comparisons with its live view's box).
        Assert.Equal(scale == 1 ? 94 : 156.4f, shown.Box!.ContentWidth, 0.05f);
        var (inside, outside) = (shown.Box!.ViewportRect, s.Q("#races-emblem").Box!.ViewportRect);
        Assert.Equal((outside.X + 1, outside.Y + 1, outside.Right - 1, outside.Bottom - 1), (inside.X, inside.Y, inside.Right, inside.Bottom));
        Assert.Null(s.Q("#races-emblem .races-emblem-mark").Box);
        s.Key("ArrowRight");
        Assert.Equal($"ui/emblems/elf-{medallion}.png", shown.GetAttribute("src"));
        Assert.Empty(s.H.Reported);
    }

    /// <summary>In Dev Mode the enemy-only races (no emblem) keep the ✦, in the strip and in the medallion.</summary>
    [Fact]
    public void ARaceWithNoEmblemKeepsTheStarInTheCarousel()
    {
        using var s = new HeroScreensHarness(height: 1040, before: settings => settings.DevMode = true);
        s.NewHero();
        s.Run(HeroFlow.RacesCommand);
        var goblin = s.Q("#races-strip .races-strip-item[data-race=\"goblin\"]");
        Assert.Equal(("span", "✦"), (Assert.Single(goblin.ElementChildren).Tag, goblin.TextContent));
        Assert.False(goblin.HasClass("has-picture"));
        s.Click(goblin);
        Assert.Equal((true, null), (s.Q("#races-emblem-picture").Hidden, s.Q("#races-emblem-picture").GetAttribute("src")));
        Assert.NotNull(s.Q("#races-emblem .races-emblem-mark").Box);
        // Back to a race with an emblem: its picture again.
        s.Click(s.Q("#races-strip .races-strip-item[data-race=\"hobbit\"]"));
        Assert.Equal(("ui/emblems/hobbit-94.png", false), (s.Q("#races-emblem-picture").GetAttribute("src"), s.Q("#races-emblem-picture").Hidden));
        Assert.Null(s.Q("#races-emblem .races-emblem-mark").Box);
        // The strip made again (Dev Mode turned off while it shows): the pictures taken out are let go, the new ones and the
        // medallion's followed.
        Assert.Equal(10, s.Races.Pictures.Following);
        s.H.Settings.DevMode = false;
        s.H.Frame();
        Assert.Equal(9, s.Document.QuerySelectorAll("#races-strip .races-strip-picture").Count());
        Assert.Equal(10, s.Races.Pictures.Following);
    }

    /// <summary>A live emblem (GM2 step 11's) takes the picture's place: the picture isn't drawn while the view shows.</summary>
    [Fact]
    public void ALiveEmblemTakesThePicturesPlace()
    {
        using var s = new HeroScreensHarness();
        s.Races.Views = new LiveEmblem();
        s.NewHero();
        s.Run(HeroFlow.RacesCommand);
        Assert.True(s.Q("#races-emblem").HasClass("is-live"));
        Assert.Null(s.Q("#races-emblem-picture").Box);
        Assert.Null(s.Q("#races-emblem .races-emblem-mark").Box);
        s.Races.Views = NoHeroViews.Instance;
        s.Key("ArrowRight");
        Assert.NotNull(s.Q("#races-emblem-picture").Box);
    }

    sealed class LiveEmblem : IHeroViews
    {
        public void ShowFigure(UiElement surface, string race, LookHero? look)
        {
        }

        public bool ShowEmblem(UiElement surface, string race, string? emblem) => true;

        public void Hide(UiElement surface)
        {
        }
    }
}
