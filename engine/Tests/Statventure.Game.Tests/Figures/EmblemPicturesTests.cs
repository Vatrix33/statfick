using Statventure.Game.Screens.Sheet;
using Statventure.Imaging;
using Statventure.Packs;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The race emblems' pictures (GM2 step 10; Q36; the manager's GM2-P34): every picture in <c>Assets/UI/emblems/</c> (ui.pak's
/// <c>ui/emblems/&lt;emblem&gt;-&lt;size&gt;.png</c>, <see cref="EmblemPictures"/>) is Blender's render of its emblem
/// (<see cref="EmblemReferenceTests"/>: 640 × 640, the approval renders' camera and size) made smaller to its size, as the
/// small copies the user approved were (a render at the small size itself has the look's 3 px lines swallow the medallion:
/// its beads a black ring, the 36 px one mostly ink). Made here from the committed renders
/// (<see cref="Picture"/>; <c>STATVENTURE_EMBLEM_PICTURES=write</c> writes them, else every committed picture must be what
/// they make, pixel for pixel), packed by make-ui-pack.sh; Tools/Scripts/make-emblem-pictures.sh runs the whole chain.
/// <list type="number">
/// <item>The ground made transparent: the emblem was rendered over black and over white (the same scene), so a pixel's
/// cover is one less the two renders' difference in linear light (the mean of its three channels) and its colour, over
/// black, is already that cover times the emblem's.</item>
/// <item>The lines laid over: the ink render's coverage in the look's ink (compose.py's rule, as the engine lays its lines
/// over its view), the cover growing by the ink's past the emblem's edge.</item>
/// <item>Made smaller: in sRGB with the cover multiplied in (so the picture laid over any ground in the page is the render
/// laid over it made smaller, as the approval's copies over the dark ground were), by a Lanczos filter of three lobes
/// stretched to the scale (Pillow's LANCZOS, the approval copies' filter), across then down, each output pixel's weights
/// summing to one; the cover and the colour clamped to what a pixel can hold, then the colour divided by the cover again
/// (PNG's straight alpha), each rounded to the nearest level.</item>
/// </list>
/// </summary>
public class EmblemPicturesTests
{
    /// <summary>Set to <c>write</c>: the pictures are written from the renders.</summary>
    public const string WriteVariable = "STATVENTURE_EMBLEM_PICTURES";

    internal static string Folder => Path.Combine(RaceOracle.Engine, "Assets", "UI", "emblems");

    /// <summary>The look's ink (the game's style's, ember-hearth's) the lines are laid over in.</summary>
    internal static string Ink
    {
        get
        {
            using var data = EmblemReferenceTests.Data();
            return Figures.GameStyle.Load(data).Ink;
        }
    }

    static readonly double[] Linear = [.. Enumerable.Range(0, 256).Select(i => ToLinear(i / 255.0))];

    static double ToLinear(double c) => c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);

    static double ToSrgb(double c) => c <= 0.0031308 ? c * 12.92 : 1.055 * Math.Pow(c, 1 / 2.4) - 0.055;

    /// <summary>
    /// The emblem at the renders' size, transparent and lined: per pixel its colour in sRGB times its cover, and its cover
    /// (four doubles a pixel), from its render over black, over white and its lines' coverage, in ink <paramref name="ink"/>.
    /// </summary>
    internal static double[] Master(RgbaImage black, RgbaImage white, RgbaImage lines, string ink)
    {
        if (black.Width != white.Width || black.Height != white.Height || lines.Width != black.Width || lines.Height != black.Height)
            throw new ArgumentException("the renders aren't of one size");
        var value = Convert.ToInt32(ink.TrimStart('#'), 16);
        double[] inkLinear = [Linear[(value >> 16) & 255], Linear[(value >> 8) & 255], Linear[value & 255]];
        var master = new double[black.Width * black.Height * 4];
        for (var i = 0; i < black.Width * black.Height; i++)
        {
            var j = i * 4;
            var gap = 0.0;
            for (var c = 0; c < 3; c++)
                gap += Linear[white.Pixels[j + c]] - Linear[black.Pixels[j + c]];
            var cover = Math.Clamp(1 - gap / 3, 0, 1);
            var k = lines.Pixels[j + 3] / 255.0;
            var alpha = k + cover * (1 - k);
            for (var c = 0; c < 3; c++)
            {
                var surface = Math.Min(Linear[black.Pixels[j + c]], cover);
                var premultiplied = inkLinear[c] * k + surface * (1 - k);
                master[j + c] = alpha > 0 ? ToSrgb(premultiplied / alpha) * alpha : 0;
            }
            master[j + 3] = alpha;
        }
        return master;
    }

    /// <summary>Lanczos's kernel of three lobes.</summary>
    static double Lanczos(double x)
    {
        if (x == 0)
            return 1;
        if (x <= -3 || x >= 3)
            return 0;
        var px = Math.PI * x;
        return 3 * Math.Sin(px) * Math.Sin(px / 3) / (px * px);
    }

    /// <summary>For each of <paramref name="to"/> output pixels along a line of <paramref name="from"/>: the first input pixel and the weights (summing to one).</summary>
    static (int First, double[] Weights)[] Taps(int from, int to)
    {
        var scale = (double)from / to;
        var support = 3 * scale;
        var taps = new (int, double[])[to];
        for (var x = 0; x < to; x++)
        {
            var centre = (x + 0.5) * scale;
            var first = Math.Max(0, (int)Math.Floor(centre - support));
            var last = Math.Min(from, (int)Math.Ceiling(centre + support));
            var weights = new double[last - first];
            var sum = 0.0;
            for (var i = first; i < last; i++)
                sum += weights[i - first] = Lanczos((i + 0.5 - centre) / scale);
            for (var i = 0; i < weights.Length; i++)
                weights[i] /= sum;
            taps[x] = (first, weights);
        }
        return taps;
    }

    /// <summary>The master (<paramref name="width"/> × <paramref name="height"/>) made <paramref name="size"/> × <paramref name="size"/>: a picture (straight alpha, 8 bits).</summary>
    internal static RgbaImage Picture(double[] master, int width, int height, int size)
    {
        var across = Taps(width, size);
        var down = Taps(height, size);
        var narrow = new double[size * height * 4];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < size; x++)
            {
                var (first, weights) = across[x];
                for (var c = 0; c < 4; c++)
                {
                    var sum = 0.0;
                    for (var i = 0; i < weights.Length; i++)
                        sum += weights[i] * master[(y * width + first + i) * 4 + c];
                    narrow[(y * size + x) * 4 + c] = sum;
                }
            }
        var picture = new RgbaImage(size, size);
        for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var (first, weights) = down[y];
                Span<double> pixel = stackalloc double[4];
                for (var c = 0; c < 4; c++)
                {
                    var sum = 0.0;
                    for (var i = 0; i < weights.Length; i++)
                        sum += weights[i] * narrow[((first + i) * size + x) * 4 + c];
                    pixel[c] = sum;
                }
                var alpha = Math.Clamp(pixel[3], 0, 1);
                var a = (byte)Math.Round(alpha * 255, MidpointRounding.AwayFromZero);
                var j = (y * size + x) * 4;
                picture.Pixels[j + 3] = a;
                if (a == 0)
                    continue;
                for (var c = 0; c < 3; c++)
                    picture.Pixels[j + c] = (byte)Math.Round(Math.Clamp(pixel[c], 0, alpha) / alpha * 255, MidpointRounding.AwayFromZero);
            }
        return picture;
    }

    /// <summary>The renders of <paramref name="emblem"/> (over black, over white, its lines) as committed.</summary>
    internal static (RgbaImage Black, RgbaImage White, RgbaImage Lines) Renders(string emblem)
    {
        var name = EmblemReferenceTests.SceneName(emblem, "");
        RgbaImage Load(string golden)
        {
            var path = Golden.GoldenPath(golden);
            Assert.True(File.Exists(path), $"no Blender render {golden}: run Tools/Scripts/make-emblem-pictures.sh (or make-blender-references.sh with the emblem scenes' names)");
            return ImageFile.Load(path);
        }
        return (Load("r3d-" + name), Load("r3d-" + EmblemReferenceTests.SceneName(emblem, "-white")), Load("r3d-" + name + "-ink"));
    }

    /// <summary>Every picture of <paramref name="emblem"/> by its size.</summary>
    internal static IEnumerable<(int Size, RgbaImage Picture)> Pictures(string emblem, string ink)
    {
        var (black, white, lines) = Renders(emblem);
        var master = Master(black, white, lines, ink);
        return EmblemPictures.Sizes.Select(size => (size, Picture(master, black.Width, black.Height, size)));
    }

    public static TheoryData<string> Names => EmblemReferenceTests.Names;

    /// <summary>
    /// Every picture of the emblem is what its renders make (written with STATVENTURE_EMBLEM_PICTURES=write): a change of a
    /// render, the ink or the way a picture is made fails here until the pictures are made again (then make-ui-pack.sh).
    /// </summary>
    [Theory]
    [MemberData(nameof(Names))]
    public void ThePicturesAreWhatTheRendersMake(string emblem)
    {
        var write = Environment.GetEnvironmentVariable(WriteVariable) == "write";
        foreach (var (size, picture) in Pictures(emblem, Ink))
        {
            var path = Path.Combine(Folder, $"{emblem}-{size}.png");
            if (write)
                ImageFile.SavePng(path, picture);
            Assert.True(File.Exists(path), $"no picture {path}: run the game's tests with {WriteVariable}=write (EmblemPicturesTests), then Tools/Scripts/make-ui-pack.sh");
            var committed = ImageFile.Load(path);
            Assert.True(committed.Width == size && committed.Height == size && committed.Pixels.AsSpan().SequenceEqual(picture.Pixels),
                $"{path} isn't what the renders make: run the game's tests with {WriteVariable}=write (EmblemPicturesTests), then Tools/Scripts/make-ui-pack.sh");
        }
    }

    /// <summary>The folder holds the pictures of the emblems of the game's races, at every size, and nothing else; ui.pak holds them as they are, and nothing else under ui/emblems.</summary>
    [Fact]
    public void UiPakHoldsEveryPictureAndNothingElse()
    {
        using var data = EmblemReferenceTests.Data();
        var expected = EmblemReferenceTests.Races(data).SelectMany(r => EmblemPictures.Sizes.Select(s => $"{r.Race.Emblem}-{s}.png")).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(expected, Directory.GetFiles(Folder).Select(Path.GetFileName).Order(StringComparer.Ordinal));
        using var pack = PackFile.Open(Path.Combine(TestPaths.Packs, "ui.pak"));
        Assert.Equal(expected.Select(f => $"{EmblemPictures.Folder}/{f}"), pack.Entries.Select(e => e.Path).Where(p => p.StartsWith(EmblemPictures.Folder + "/", StringComparison.Ordinal)).Order(StringComparer.Ordinal));
        foreach (var file in expected)
        {
            Assert.True(File.ReadAllBytes(Path.Combine(Folder, file)).AsSpan().SequenceEqual(data.ReadAllBytes($"{EmblemPictures.Folder}/{file}")),
                $"ui.pak's {file} isn't Assets/UI/emblems/{file}: run Tools/Scripts/make-ui-pack.sh");
            Assert.True(EmblemPictures.Has(data, file[..file.LastIndexOf('-')]));
        }
    }

    /// <summary>
    /// The two renders are one scene over two grounds (so the cover taken from them is the emblem's): where the emblem
    /// covers its pixel the two are the same, and wherever the three channels each give the cover, they agree; the lines
    /// are the same over either ground.
    /// </summary>
    [Theory]
    [MemberData(nameof(Names))]
    public void TheTwoRendersAreOneSceneOverTwoGrounds(string emblem)
    {
        var (black, white, lines) = Renders(emblem);
        Assert.True(lines.Pixels.AsSpan().SequenceEqual(ImageFile.Load(Golden.GoldenPath("r3d-" + EmblemReferenceTests.SceneName(emblem, "-white") + "-ink")).Pixels),
            $"{emblem}'s lines differ over the two grounds");
        int opaque = 0, clear = 0, spread = 0;
        for (var i = 0; i < black.Width * black.Height; i++)
        {
            var j = i * 4;
            var covers = new double[3];
            for (var c = 0; c < 3; c++)
                covers[c] = 1 - (Linear[white.Pixels[j + c]] - Linear[black.Pixels[j + c]]);
            if (covers.Max() - covers.Min() > 0.02)
                spread++;
            if (covers.All(v => v == 1))
                opaque++;
            if (covers.All(v => v == 0))
                clear++;
        }
        // The medallion fills about (2.66 / 3)² × π / 4 = 0.62 of the square picture (2.66 units across of 3).
        Assert.InRange(opaque / (double)(black.Width * black.Height), 0.55, 0.65);
        Assert.InRange(clear / (double)(black.Width * black.Height), 0.35, 0.45);
        Assert.Equal(0, spread);
    }

    /// <summary>
    /// What a picture is made of, on small made-up renders: a pixel the emblem covers keeps its colour and is opaque; one it
    /// doesn't is clear; one half covered over black and white is half covered with the emblem's colour; a line's ink over
    /// the clear ground is the ink at its coverage; made smaller, a flat colour stays that colour, a picture's cover is the
    /// area's mean, and an edge stays where it was.
    /// </summary>
    [Fact]
    public void APictureIsTheRenderTransparentLinedAndSmaller()
    {
        static RgbaImage Flat(int size, byte r, byte g, byte b, byte a = 255)
        {
            var image = new RgbaImage(size, size);
            for (var i = 0; i < size * size; i++)
                (image.Pixels[i * 4], image.Pixels[i * 4 + 1], image.Pixels[i * 4 + 2], image.Pixels[i * 4 + 3]) = (r, g, b, a);
            return image;
        }
        // An opaque emblem of one colour: the same over both grounds.
        var master = Master(Flat(8, 200, 100, 50), Flat(8, 200, 100, 50), Flat(8, 0, 0, 0, 0), "#080605");
        Assert.Equal(1, master[3]);
        Assert.Equal(200 / 255.0, master[0] / master[3], 9);
        var small = Picture(master, 8, 8, 4);
        Assert.Equal((200, 100, 50, 255), (small.Pixels[0], small.Pixels[1], small.Pixels[2], small.Pixels[3]));
        // The clear ground: black over black, white over white.
        master = Master(Flat(4, 0, 0, 0), Flat(4, 255, 255, 255), Flat(4, 0, 0, 0, 0), "#080605");
        Assert.All(Enumerable.Range(0, 16), i => Assert.Equal(0, master[i * 4 + 3]));
        // Half covered by a white emblem: over black half white in linear light, over white white.
        var half = (byte)Math.Round(ToSrgb(0.5) * 255);
        master = Master(Flat(4, half, half, half), Flat(4, 255, 255, 255), Flat(4, 0, 0, 0, 0), "#080605");
        Assert.Equal(0.5, master[3], 2);
        Assert.Equal(1, master[0] / master[3], 2);
        // A line of full coverage over the clear ground: the ink, opaque; at half coverage the ink, half covering.
        master = Master(Flat(4, 0, 0, 0), Flat(4, 255, 255, 255), Flat(4, 0, 0, 0, 128), "#804020");
        Assert.Equal(128 / 255.0, master[3], 9);
        Assert.Equal((0x80, 0x40, 0x20), ((int)Math.Round(master[0] / master[3] * 255), (int)Math.Round(master[1] / master[3] * 255), (int)Math.Round(master[2] / master[3] * 255)));
        // Made smaller: the left half opaque red, the right half clear; the halves stay apart, the cover's mean a half.
        var split = new RgbaImage(16, 16);
        var clearRight = new RgbaImage(16, 16);
        for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
            {
                split[x, y] = x < 8 ? ((byte)255, (byte)0, (byte)0, (byte)255) : ((byte)0, (byte)0, (byte)0, (byte)255);
                clearRight[x, y] = x < 8 ? ((byte)255, (byte)0, (byte)0, (byte)255) : ((byte)255, (byte)255, (byte)255, (byte)255);
            }
        var picture = Picture(Master(split, clearRight, new RgbaImage(16, 16), "#080605"), 16, 16, 4);
        Assert.Equal((255, 0, 0, 255), (picture[0, 2].R, picture[0, 2].G, picture[0, 2].B, picture[0, 2].A));
        Assert.Equal(0, picture[3, 2].A);
        var row = Enumerable.Range(0, 4).Sum(x => picture[x, 2].A) / 4.0 / 255;
        Assert.InRange(row, 0.48, 0.52);
        Assert.True(picture[1, 2].A > 200 && picture[2, 2].A < 55, $"the edge moved: {picture[1, 2].A}, {picture[2, 2].A}");
    }
}
