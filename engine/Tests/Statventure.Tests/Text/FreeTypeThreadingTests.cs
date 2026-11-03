using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Statventure.Text;
using Statventure.Text.Autohint;
using Statventure.Text.Skia;

namespace Statventure.Tests.Text;

/// <summary>
/// FreeType lets one thread at a time use a face (freetype.h, FT_Face): its size, transform, glyph
/// slot, character map walk and stream are shared by everything that reads glyphs from it, and the
/// engine reads them on whatever thread lays out, paints or draws (the tests share their fonts
/// across parallel tests). Without that rule the map screen's layer analysis, which loads the
/// outlines of its underlined destinations for skipping ink, now and then failed with a
/// NullReferenceException (reading an outline another thread's glyph load was replacing) while
/// other tests drew glyphs of the same faces (2026-11-03). So each use of the face's FreeType state
/// must wait while another thread holds the face (<see cref="FontFace.FreeTypeLock"/>), and give
/// what it gives alone. A library's faces are made and destroyed one at a time (freetype.h,
/// FT_Library), and its LCD filter is its own, set and used with the library held
/// (<see cref="FontLibrary.FreeTypeLock"/>; the user's decision of 2026-11-03).
/// </summary>
public sealed unsafe class FreeTypeThreadingTests : IDisposable
{
    static string LatoPath => Path.Combine(AppContext.BaseDirectory, "Data", "Fonts", "Lato-Regular.ttf");

    // Faces of their own: nothing else uses them (each face is new, so no cache knows it yet).
    readonly FontLibrary _fonts = new();
    readonly FontFace _face;
    readonly FontFace _twin;

    public FreeTypeThreadingTests()
    {
        _face = _fonts.AddFile(LatoPath, "Lato");
        _twin = _fonts.AddFile(LatoPath, "Lato twin");
    }

    public void Dispose() => _fonts.Dispose();

    uint Glyph(char c) => _face.GlyphFor(c);

    [Fact]
    public void OutlinesForSkippingInkWaitForTheFace()
    {
        // The twin gives the expected outline (outlines are cached by face).
        var expected = GlyphOutlines.Contours(_twin, Glyph('g'), 31.5f, false, false);
        var outline = WhileAnotherThreadHoldsTheFace(_face, () => GlyphOutlines.Contours(_face, Glyph('g'), 31.5f, false, false));
        Assert.Equal(Points(expected), Points(outline));
    }

    [Fact]
    public void GlyphImagesWaitForTheFace()
    {
        var settings = new FontRenderSettings { Antialias = TextAntialias.Grayscale, Hinting = TextHinting.None };
        var expected = SkiaGlyphs.Render(_face, Glyph('g'), 31.5f, 0, false, false, settings, false, 0);
        var image = WhileAnotherThreadHoldsTheFace(_face, () => SkiaGlyphs.Render(_face, Glyph('g'), 31.5f, 0, false, false, settings, false, 0));
        AssertSameImage(expected, image);
    }

    [Fact]
    public void FreeTypeRasterizedGlyphsWaitForTheFace()
    {
        var expected = GlyphRasterizer.Render(_face, Glyph('g'), 31.5f, 0, false, false);
        var image = WhileAnotherThreadHoldsTheFace(_face, () => GlyphRasterizer.Render(_face, Glyph('g'), 31.5f, 0, false, false));
        AssertSameImage(expected, image);
    }

    [Fact]
    public void TheAutohintersFontWaitsForTheFace()
    {
        var expected = AutohintFont.For(_twin);
        var font = WhileAnotherThreadHoldsTheFace(_face, () => AutohintFont.For(_face));
        Assert.Equal(expected.Mappings, font.Mappings);
        Assert.Equal(expected.Gsub, font.Gsub);
        Assert.Equal((expected.IsFixedPitch, expected.IsItalic), (font.IsFixedPitch, font.IsItalic));
    }

    [Fact]
    public void TheAutohintersOutlinesWaitForTheFace()
    {
        var font = AutohintFont.For(_face);
        var expected = new List<UnscaledPoint>();
        Assert.True(font.DrawUnscaled(Glyph('g'), expected, out var expectedAdvance));
        var (points, advance) = WhileAnotherThreadHoldsTheFace(_face, () =>
        {
            var sink = new List<UnscaledPoint>();
            Assert.True(font.DrawUnscaled(Glyph('g'), sink, out var drawnAdvance));
            return (sink, drawnAdvance);
        });
        Assert.Equal(expected, points);
        Assert.Equal(expectedAdvance, advance);
    }

    [Fact]
    public void TheAutohintersAdvancesWaitForTheFace()
    {
        var font = AutohintFont.For(_face);
        var expected = font.AdvanceWidth(Glyph('g'));
        Assert.NotNull(expected);
        Assert.Equal(expected, WhileAnotherThreadHoldsTheFace(_face, () => font.AdvanceWidth(Glyph('g'))));
    }

    // ---------- The library: making and destroying faces, the LCD filter ----------

    [Fact]
    public void FacesAreMadeOneAtATimePerLibrary()
    {
        var data = File.ReadAllBytes(LatoPath);
        var face = WhileAnotherThreadHolds(_fonts.FreeTypeLock, "the library", () => _fonts.Add(data, "Lato three"));
        Assert.Equal(_face.GlyphFor('g'), face.GlyphFor('g'));
    }

    [Fact]
    public void FacesAreDestroyedOneAtATimePerLibrary()
    {
        // A library of its own, destroyed (with its faces) on the other thread.
        var fonts = new FontLibrary();
        fonts.AddFile(LatoPath, "Lato");
        WhileAnotherThreadHolds(fonts.FreeTypeLock, "the library", () =>
        {
            fonts.Dispose();
            return 0;
        });
    }

    static readonly FontRenderSettings Lcd = new() { Antialias = TextAntialias.SubpixelRgb, Hinting = TextHinting.None, LcdFilter = 1 };

    [Fact]
    public void LcdGlyphsWaitForTheLibrary()
    {
        // The LCD filter is the library's: another face of it may set its own meanwhile.
        var expected = GlyphRasterizer.Render(_face, Glyph('g'), 31.5f, 0, false, false, Lcd, lcd: true);
        var image = WhileAnotherThreadHolds(_fonts.FreeTypeLock, "the library", () => GlyphRasterizer.Render(_face, Glyph('g'), 31.5f, 0, false, false, Lcd, lcd: true));
        AssertSameImage(expected, image);
    }

    [Fact]
    public void EachLibraryIsGivenItsOwnLcdFilter()
    {
        // FreeType keeps the LCD filter per library, so each library is given the filter asked for,
        // though another was just given it. (BRUTAL's FreeType is built without subpixel rendering:
        // FT_Library_SetLcdFilter answers Unimplemented_Feature and changes nothing, so the images
        // can't show it; the filter the library was given can.)
        using var other = new FontLibrary();
        var otherFace = other.AddFile(LatoPath, "Lato");
        void Draw(FontFace face, int filter) => GlyphRasterizer.Render(face, face.GlyphFor('g'), 31.5f, 0, false, false, Lcd with { LcdFilter = filter }, lcd: true);
        foreach (var filter in new[] { 2, 0, 2, 1 })
        {
            Draw(_face, filter);
            Draw(otherFace, filter);
            Assert.Equal((filter, filter), (_fonts.LcdFilter, other.LcdFilter));
        }
        // Grayscale glyphs leave it as it was.
        GlyphRasterizer.Render(otherFace, otherFace.GlyphFor('g'), 31.5f, 0, false, false, Lcd with { LcdFilter = 16 }, lcd: false);
        Assert.Equal(1, other.LcdFilter);
    }

    /// <summary>
    /// Runs <paramref name="use"/> on another thread while this one holds the face the way a glyph
    /// is drawn (another size, a sheared transform, another glyph in the slot), puts the transform
    /// back as every user does, and lets go. The use must not finish before that.
    /// </summary>
    static T WhileAnotherThreadHoldsTheFace<T>(FontFace face, Func<T> use) =>
        WhileAnotherThreadHolds(face.FreeTypeLock, "the face", use, holding: () =>
        {
            var ft = face.FtFace;
            FreeType.Check(FreeType.SetCharSize(ft, new CLong(0), new CLong(77 * 64), 72, 72), "FT_Set_Char_Size");
            var shear = new FreeType.Matrix { XX = new CLong(0x10000), XY = new CLong(0x8000), YX = new CLong(0), YY = new CLong(0x10000) };
            var delta = new FreeType.Vector { X = new CLong(40), Y = new CLong(0) };
            FreeType.SetTransform(ft, &shear, &delta);
            FreeType.Check(FreeType.LoadGlyph(ft, face.GlyphFor('W'), FreeType.LoadNoBitmap), "FT_Load_Glyph");
        }, releasing: () => FreeType.SetTransform(face.FtFace, null, null));

    /// <summary>
    /// Runs <paramref name="use"/> on another thread while this one holds <paramref name="gate"/>
    /// (doing <paramref name="holding"/> first and <paramref name="releasing"/> last), and checks the
    /// use didn't finish before this thread let go.
    /// </summary>
    static T WhileAnotherThreadHolds<T>(Lock gate, string what, Func<T> use, Action? holding = null, Action? releasing = null)
    {
        var released = false;
        var finishedAfterRelease = false;
        T result = default!;
        Exception? error = null;
        using var finished = new ManualResetEventSlim();
        var user = new Thread(() =>
        {
            try
            {
                result = use();
            }
            catch (Exception exception)
            {
                error = exception;
            }
            finishedAfterRelease = Volatile.Read(ref released);
            finished.Set();
        });
        lock (gate)
        {
            holding?.Invoke();
            user.Start();
            // Until the user waits (for the gate) or is done.
            var clock = Stopwatch.StartNew();
            while (!finished.Wait(1) && (user.ThreadState & System.Threading.ThreadState.WaitSleepJoin) == 0 && clock.Elapsed < TimeSpan.FromSeconds(10))
            {
            }
            releasing?.Invoke();
            Volatile.Write(ref released, true);
        }
        user.Join();
        Assert.Null(error);
        Assert.True(finishedAfterRelease, $"it used FreeType while another thread held {what}");
        return result;
    }

    static List<(int Contour, Vector2 Point)> Points(List<Vector2[]> contours) => contours.SelectMany((c, i) => c.Select(p => (i, p))).ToList();

    static void AssertSameImage(GlyphBitmap expected, GlyphBitmap actual)
    {
        Assert.Equal((expected.Width, expected.Height, expected.Left, expected.Top), (actual.Width, actual.Height, actual.Left, actual.Top));
        Assert.Equal(expected.Coverage, actual.Coverage);
    }
}
