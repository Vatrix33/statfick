using Statventure.Packs;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Sheet;

/// <summary>
/// The race emblems' pictures (Q36: pictures where the emblem is small; GM2 step 10, Docs/Game/Hero.md, "How it works",
/// step 10): PNGs in <c>ui.pak</c> at <c>ui/emblems/&lt;emblem&gt;-&lt;size&gt;.png</c>, one per device-pixel size in
/// <see cref="Sizes"/> (every place a screen shows an emblem, <see cref="Places"/>, at device scales 1 and 1.65), made from
/// Blender's render of the emblem's model in the game's look (<c>Figures/Emblems</c>) by the game's tests. A screen shows
/// one in an <c>img</c> by <see cref="Show"/>: the picture whose size is nearest the element's content box in device pixels,
/// chosen again whenever that changes (a resize observer on its device-pixel content box: the window's width, its scale,
/// the page's zoom), so a picture is drawn at its own size at the scales the game is tested at. A race with no emblem, or
/// an emblem with no pictures in the data (a mod's), shows none: its screen shows the ✦ as the old pages did.
/// </summary>
public sealed class EmblemPictures
{
    /// <summary>The pictures' folder in the game's data (ui.pak; Assets/UI/emblems/).</summary>
    public const string Folder = "ui/emblems";

    /// <summary>
    /// Where the screens show an emblem as a picture, at the device scales the game is tested at (1 and 1.65, GM1-P17), and
    /// the size the picture is drawn at there, device pixels, as laid out (the engine's and Chromium's alike):
    /// <list type="bullet">
    /// <item>the sheet's seal: 72 % of the seal button's content box, the button 172 px (138 at 840 px or less, 100 at 650 px or
    /// less: game.css, the later rules winning) less its 1 px border and the button's own padding of 1 by 6 px, so 113.76 ×
    /// 120.96 CSS px (89.28 × 96.48, 61.92 × 69.12), the square picture as wide as the box (object-fit: contain); at 1.65 the
    /// border a whole device pixel;</item>
    /// <item>the carousel's medallion: its inside, 96 px less its 1 px border (races.css);</item>
    /// <item>a medallion of the carousel's strip: its inside, 38 px less its 1 px border.</item>
    /// </list>
    /// </summary>
    public static readonly IReadOnlyList<(string Place, float Scale, float Size)> Places =
    [
        ("the sheet's seal", 1, 113.76f), ("the sheet's seal", 1.65f, 188.64f),
        ("the sheet's seal at 840 px or less", 1, 89.28f), ("the sheet's seal at 840 px or less", 1.65f, 148.25f),
        ("the sheet's seal at 650 px or less", 1, 61.92f), ("the sheet's seal at 650 px or less", 1.65f, 103.10f),
        ("the carousel's medallion", 1, 94), ("the carousel's medallion", 1.65f, 156.4f),
        ("a medallion of the carousel's strip", 1, 36), ("a medallion of the carousel's strip", 1.65f, 60.7f),
    ];

    /// <summary>The pictures' sizes, device pixels, smallest first: every place at both scales, rounded.</summary>
    public static IReadOnlyList<int> Sizes { get; } = [.. Places.Select(p => (int)MathF.Round(p.Size)).Distinct().Order()];

    /// <summary>The picture of <paramref name="emblem"/> at <paramref name="size"/> device pixels.</summary>
    public static string PathOf(string emblem, int size) => $"{Folder}/{emblem}-{size}.png";

    /// <summary>The size of the picture to draw in a box <paramref name="devicePixels"/> wide: the nearest of <see cref="Sizes"/> by their ratio (a tie the larger).</summary>
    public static int Nearest(float devicePixels)
    {
        var best = Sizes[^1];
        var bestOff = double.MaxValue;
        foreach (var size in Sizes)
        {
            var off = Math.Abs(Math.Log(size / (double)devicePixels));
            if (off <= bestOff)
                (best, bestOff) = (size, off);
        }
        return best;
    }

    /// <summary>Whether <paramref name="emblem"/> has its pictures in <paramref name="files"/>: one at every size.</summary>
    public static bool Has(VirtualFileSystem? files, string? emblem) =>
        emblem is { Length: > 0 } && files is not null && Sizes.All(s => files.Exists(PathOf(emblem, s)));

    readonly UiDocument _document;
    readonly UiResizeObserver _observer;
    // The images followed, and the size chosen for each once its box has been seen.
    readonly HashSet<UiElement> _following = [];
    readonly Dictionary<UiElement, int> _sizes = [];

    /// <summary>How many images this follows (tests: an image taken out of the page is let go).</summary>
    internal int Following => _following.Count;

    /// <summary>The pictures of <paramref name="document"/>'s emblems (its data's).</summary>
    public EmblemPictures(UiDocument document)
    {
        _document = document;
        _observer = new UiResizeObserver(document, Observed);
    }

    /// <summary>
    /// <paramref name="image"/> (an <c>img</c>) shows <paramref name="emblem"/>'s picture: true, the image shown, its
    /// <c>src</c> the picture of the size nearest its device-pixel box (from its first layout on); false for no emblem or
    /// one without pictures, the image hidden with no <c>src</c>.
    /// </summary>
    public bool Show(UiElement image, string? emblem)
    {
        if (!Has(_document.Files, emblem))
        {
            image.Hidden = true;
            image.RemoveAttribute("src");
            image.RemoveAttribute("data-emblem");
            return false;
        }
        image.Hidden = false;
        image.SetAttribute("data-emblem", emblem);
        _observer.Observe(image, ResizeObserverBox.DevicePixelContentBox);
        _following.Add(image);
        if (_sizes.TryGetValue(image, out var size))
            image.SetAttribute("src", PathOf(emblem!, size));
        return true;
    }

    /// <summary>Stops following <paramref name="image"/> (taken out of the document).</summary>
    public void Forget(UiElement image)
    {
        _observer.Unobserve(image);
        _following.Remove(image);
        _sizes.Remove(image);
    }

    void Observed(IReadOnlyList<UiResizeObserverEntry> entries)
    {
        foreach (var entry in entries)
        {
            // The picture is square, drawn as large as the box holds (object-fit: contain, or a square box).
            var box = entry.DevicePixelContentBoxSize;
            var pixels = MathF.Min(box.InlineSize, box.BlockSize);
            if (pixels <= 0)
                continue;
            var size = Nearest(pixels);
            _sizes[entry.Target] = size;
            if (entry.Target.GetAttribute("data-emblem") is { Length: > 0 } emblem)
                entry.Target.SetAttribute("src", PathOf(emblem, size));
        }
    }
}
