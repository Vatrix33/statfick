namespace Statventure.Platform;

/// <summary>
/// Whose behaviour the engine follows where Chromium differs by platform (subpixel text
/// positioning and line metrics, scrollbar thumb snap-back, wheel notches): the running OS's by
/// default. The tests set Linux, the platform their Chromium references were captured on, so
/// they give the same results on Windows.
/// </summary>
public static class PlatformBehaviour
{
    /// <summary>Follow Chromium on Windows (else Chromium on Linux).</summary>
    public static bool Windows { get; set; } = OperatingSystem.IsWindows();

    /// <summary>
    /// The preferred colour scheme is dark (Chromium's prefers-color-scheme, which also picks the scheme of
    /// <c>color-scheme: light dark</c>): light unless set (the engine doesn't read the desktop's preference). The tests set
    /// the reference PC's: its Chromium prefers dark. Read by each document as it is made and when its viewport changes.
    /// </summary>
    public static bool PrefersDarkColorScheme { get; set; }
}
