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
}
