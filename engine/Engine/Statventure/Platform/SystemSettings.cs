using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Statventure.Platform;

/// <summary>
/// The desktop settings Chromium 152 takes over: how fast the caret blinks and whether the user
/// asked for less motion. On Linux they are GTK's (gtk-cursor-blink, gtk-cursor-blink-time,
/// gtk-enable-animations), which GTK reads from GNOME's interface settings when they exist and
/// from settings.ini otherwise; on Windows the caret blink time and client area animations.
/// </summary>
public static partial class SystemSettings
{
    /// <summary>
    /// Seconds the caret stays on and off; 0 when it shouldn't blink. Linux: GtkUi's blink time /
    /// 2000 (GTK's default 1200 ms makes 0.6 s); Windows: GetCaretBlinkTime (INFINITE: no blink).
    /// </summary>
    public static double CaretBlinkInterval
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                var time = GetCaretBlinkTime();
                return time == uint.MaxValue ? 0 : time / 1000.0;
            }
            var blink = GtkSetting("cursor-blink", "gtk-cursor-blink") is not { } on || ParseBool(on) != false;
            var blinkTime = GtkSetting("cursor-blink-time", "gtk-cursor-blink-time") is { } text && int.TryParse(text, out var ms) ? ms : 1200;
            return blink ? blinkTime / 2000.0 : 0;
        }
    }

    /// <summary>prefers-reduced-motion: GTK's animations turned off (Linux), client area animations off (Windows).</summary>
    public static bool ReducedMotion
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                var enabled = 1;
                return SystemParametersInfo(SpiGetClientAreaAnimation, 0, ref enabled, 0) && enabled == 0;
            }
            return GtkSetting("enable-animations", "gtk-enable-animations") is { } text && ParseBool(text) == false;
        }
    }

    static bool? ParseBool(string text) => text.Trim().ToLowerInvariant() switch
    {
        "true" or "1" => true,
        "false" or "0" => false,
        _ => null,
    };

    /// <summary>A GTK setting: org.gnome.desktop.interface's key when gsettings has it, else settings.ini's.</summary>
    static string? GtkSetting(string gnomeKey, string iniKey) => GSettings(gnomeKey) ?? SettingsIni(iniKey);

    static string? GSettings(string key)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("gsettings", ["get", "org.gnome.desktop.interface", key])
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            });
            if (process is null)
                return null;
            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(2000) || process.ExitCode != 0)
                return null;
            // Integers print as "1000" or "int32 1000", booleans as "true".
            var value = output.Trim();
            return value.StartsWith("int32 ", StringComparison.Ordinal) ? value[6..] : value.Length > 0 ? value : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    static string? SettingsIni(string key)
    {
        var config = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") is { Length: > 0 } home
            ? home
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        var path = Path.Combine(config, "gtk-3.0", "settings.ini");
        if (!File.Exists(path))
            return null;
        try
        {
            foreach (var line in File.ReadLines(path))
            {
                var equals = line.IndexOf('=');
                if (equals > 0 && line[..equals].Trim() == key)
                    return line[(equals + 1)..].Trim();
            }
        }
        catch (IOException)
        {
        }
        return null;
    }

    const uint SpiGetClientAreaAnimation = 0x1042;

    [LibraryImport("user32.dll")]
    private static partial uint GetCaretBlinkTime();

    [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SystemParametersInfo(uint action, uint param, ref int value, uint winIni);
}
