using System.Globalization;

namespace Statventure.Game.Screens.Slots;

/// <summary>
/// How the game's screens write a time (Q17; Docs/Game/Foundation.md, "The slot list, Load"): "13 November 2026, 14:03",
/// fixed English (the program runs in invariant globalization, GM1-9), in the player's time zone. The slot list's last
/// played, the damaged-save question's backup, the file picker's dates.
/// </summary>
public static class ScreenTimes
{
    /// <summary>The format: day, month's name, year, then the 24-hour time.</summary>
    public const string Format = "d MMMM yyyy, HH:mm";

    /// <summary><paramref name="time"/> in <paramref name="zone"/>, written as <see cref="Format"/>.</summary>
    public static string Write(DateTimeOffset time, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(time, zone).ToString(Format, CultureInfo.InvariantCulture);
}
