using Statventure.Imaging;

namespace Statventure.Game.Tests;

/// <summary>
/// The held comparisons' rule (<see cref="HeldScreens"/>; the user's word Q34, 2026-11-14): only the named fixtures, only
/// at 1.65, never at 1×; what a held one still checks: a pixel meets the golden at its place or one device pixel either
/// side along the row, no further and not up or down.
/// </summary>
public class HeldScreensTests
{
    [Fact]
    public void OnlyTheNamedFixturesAreHeldAndOnlyAt165()
    {
        Assert.NotNull(HeldScreens.Held("picker-load-dark", 1.65f));
        Assert.Contains("Q34", HeldScreens.Held("picker-save-light", 1.65f));
        Assert.Null(HeldScreens.Held("picker-load-dark", 1));
        Assert.Null(HeldScreens.Held("picker-save-replace-dark", 1.65f));
        Assert.Null(HeldScreens.Held("settings-mods-dark", 1.65f));
        Assert.Null(HeldScreens.Held("dialog-confirm-dark", 1.65f));
    }

    /// <summary>A 6 × 2 image with one bright column at <paramref name="column"/> in row <paramref name="row"/>.</summary>
    static RgbaImage Column(int column, int row = 0)
    {
        var pixels = new byte[6 * 2 * 4];
        for (var i = 3; i < pixels.Length; i += 4)
            pixels[i] = 255;
        for (var c = 0; c < 3; c++)
            pixels[(row * 6 + column) * 4 + c] = 200;
        return new RgbaImage(6, 2, pixels);
    }

    [Fact]
    public void AHeldComparisonForgivesADevicePixelAlongTheRowAndNoMore()
    {
        Assert.Equal(0, HeldScreens.DifferentAside(Column(2), Column(2)));
        Assert.Equal(0, HeldScreens.DifferentAside(Column(3), Column(2)));
        Assert.Equal(0, HeldScreens.DifferentAside(Column(1), Column(2)));
        // Two pixels aside: the bright one is counted (the dark one where it should be meets the dark beside it).
        Assert.Equal(1, HeldScreens.DifferentAside(Column(4), Column(2)));
        // A row down isn't forgiven.
        Assert.Equal(1, HeldScreens.DifferentAside(Column(2, row: 1), Column(2)));
        // Within the levels: nothing; past them: counted.
        var near = Column(2);
        near.Pixels[(0 * 6 + 2) * 4] = 212;
        Assert.Equal(0, HeldScreens.DifferentAside(near, Column(2)));
        near.Pixels[(0 * 6 + 2) * 4] = 213;
        Assert.Equal(1, HeldScreens.DifferentAside(near, Column(2)));
    }
}
