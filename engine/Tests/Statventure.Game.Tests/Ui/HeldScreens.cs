using Statventure.Imaging;
using Statventure.Tests.Infrastructure;

namespace Statventure.Game.Tests;

/// <summary>
/// The game-screen comparisons held, each with the user's word (Notes.md's rule since 2026-11-05: named, why, whose word
/// and when, still checking what it can). A held comparison isn't skipped: it checks the render against the golden as
/// <see cref="Golden.Match"/> does (12 levels, 0.2 % of the pixels) with each pixel allowed to meet the golden at its
/// place or one device pixel to either side along the row, what the cause it is held for moves. Turn a hold off by
/// removing its line, or for a run with <c>STATVENTURE_UNHOLD_SCREENS=1</c> (every comparison in full). A 1× comparison
/// is never held.
/// </summary>
static class HeldScreens
{
    /// <summary>
    /// The user's word, Q34 (2026-11-14, through the manager): at 1.65 the engine lays boxes out a hair off Chromium's
    /// (layout in floats where Blink's layout units round), so a box's border on a half device pixel snaps a pixel the
    /// other way; fixed as an engine task of its own (`gm1-layout-scale`), these comparisons held until it lands.
    /// </summary>
    const string LayoutAtScale = "Q34 (the user, 2026-11-14): boxes a hair off Chromium's at 1.65, borders a device pixel apart; held until gm1-layout-scale lands";

    /// <summary>The held comparisons at 1.65 (by fixture), each with why.</summary>
    static readonly Dictionary<string, string> AtScale165 = new(StringComparer.Ordinal)
    {
        // GM1 step 10: the file picker's 820 px dialog centred in 1000 px lies at 148.4937 device px in the engine,
        // 148.5 in Chromium: its side borders (and the name field's edge) snap to 148 and 1500 against 149 and 1501.
        ["picker-save-dark"] = LayoutAtScale,
        ["picker-save-light"] = LayoutAtScale,
        ["picker-load-dark"] = LayoutAtScale,
        ["picker-load-light"] = LayoutAtScale,
        ["picker-load-long-dark"] = LayoutAtScale,
        ["picker-load-long-light"] = LayoutAtScale,
        ["picker-load-recent-dark"] = LayoutAtScale,
        ["picker-load-recent-light"] = LayoutAtScale,
    };

    /// <summary>Why the comparison of <paramref name="fixture"/> at <paramref name="scale"/> is held, or null when it isn't.</summary>
    public static string? Held(string fixture, float scale) =>
        scale != 1 && Environment.GetEnvironmentVariable("STATVENTURE_UNHOLD_SCREENS") != "1" && AtScale165.TryGetValue(fixture, out var why) ? why : null;

    /// <summary>The held check: <paramref name="actual"/> against the golden <paramref name="name"/>, a pixel meeting it at its place or one device pixel either side along the row.</summary>
    public static void MatchWithinAPixel(string name, RgbaImage actual, string why, int tolerance = 12, double maxDifferentRatio = 0.002)
    {
        var golden = ImageFile.Load(Golden.GoldenPath(name));
        Assert.True(golden.Width == actual.Width && golden.Height == actual.Height, $"{name} (held: {why}): size {actual.Width}x{actual.Height}, reference {golden.Width}x{golden.Height}");
        var different = DifferentAside(actual, golden, tolerance);
        var ratio = different / (double)(actual.Width * actual.Height);
        Assert.True(ratio <= maxDifferentRatio, $"{name} (held: {why}): {different} pixels ({ratio:P2}) differ from the reference even a device pixel aside");
    }

    /// <summary>The pixels of <paramref name="actual"/> further than <paramref name="tolerance"/> in some channel from <paramref name="golden"/>'s at their place and at one device pixel either side along the row.</summary>
    public static int DifferentAside(RgbaImage actual, RgbaImage golden, int tolerance = 12)
    {
        var different = 0;
        for (var y = 0; y < actual.Height; y++)
            for (var x = 0; x < actual.Width; x++)
            {
                var meets = false;
                for (var dx = -1; dx <= 1 && !meets; dx++)
                {
                    var gx = x + dx;
                    if (gx < 0 || gx >= golden.Width)
                        continue;
                    var worst = 0;
                    for (var c = 0; c < 4; c++)
                        worst = Math.Max(worst, Math.Abs(actual.Pixels[(y * actual.Width + x) * 4 + c] - golden.Pixels[(y * golden.Width + gx) * 4 + c]));
                    meets = worst <= tolerance;
                }
                if (!meets)
                    different++;
            }
        return different;
    }
}
