using System.Runtime.InteropServices;
using Statventure.Core;

namespace Statventure.Text;

public enum TextAntialias
{
    None,
    Grayscale,
    /// <summary>LCD subpixels in red, green, blue order.</summary>
    SubpixelRgb,
    /// <summary>LCD subpixels in blue, green, red order.</summary>
    SubpixelBgr,
}

public enum TextHinting
{
    None,
    Slight,
    Medium,
    Full,
}

/// <summary>
/// How glyphs are rasterized. <see cref="System"/> reads the desktop's font settings the way
/// Chromium does: fontconfig on Linux (antialias, rgba, hintstyle, lcdfilter), font smoothing
/// (ClearType) on Windows.
/// </summary>
public sealed partial record FontRenderSettings
{
    public TextAntialias Antialias { get; init; } = TextAntialias.Grayscale;
    public TextHinting Hinting { get; init; } = TextHinting.Slight;
    /// <summary>FreeType LCD filter: 0 none, 1 default, 2 light, 16 legacy.</summary>
    public int LcdFilter { get; init; } = 1;

    public static readonly FontRenderSettings Grayscale = new();

    public bool IsSubpixel => Antialias is TextAntialias.SubpixelRgb or TextAntialias.SubpixelBgr;

    /// <summary>
    /// Whether Chromium positions text at subpixels on a screen of this scale (gfx::FontRenderParams):
    /// on Linux above a scale of 1 (and then without hinting), on Windows with font smoothing.
    /// </summary>
    public bool SubpixelPositioningAt(float deviceScale) =>
        Antialias != TextAntialias.None && (Platform.PlatformBehaviour.Windows || deviceScale > 1);

    /// <summary>The settings glyphs are rasterized with on a screen of this scale: unhinted where Linux positions text at subpixels.</summary>
    public FontRenderSettings AtScale(float deviceScale) =>
        !Platform.PlatformBehaviour.Windows && SubpixelPositioningAt(deviceScale) && Hinting != TextHinting.None ? this with { Hinting = TextHinting.None } : this;

    static FontRenderSettings? _system;

    /// <summary>The desktop's settings (read once; the tests set the ones their references were captured with).</summary>
    public static FontRenderSettings System
    {
        get => _system ??= Detect();
        set => _system = value;
    }

    static FontRenderSettings Detect()
    {
        try
        {
            if (OperatingSystem.IsLinux())
                return Fontconfig.Read();
            if (OperatingSystem.IsWindows())
                return Windows.Read();
        }
        catch (Exception exception) when (exception is DllNotFoundException or EntryPointNotFoundException)
        {
            Log.Warning("Text", "system font settings unavailable (" + exception.Message + "); using grayscale");
        }
        return Grayscale;
    }

    static unsafe partial class Fontconfig
    {
        const string Library = "libfontconfig.so.1";
        const int MatchPattern = 0;

        [LibraryImport(Library)] private static partial nint FcPatternCreate();
        [LibraryImport(Library)] private static partial void FcPatternDestroy(nint pattern);
        [LibraryImport(Library)] private static partial int FcPatternAddString(nint pattern, byte* name, byte* value);
        [LibraryImport(Library)] private static partial int FcConfigSubstitute(nint config, nint pattern, int kind);
        [LibraryImport(Library)] private static partial void FcDefaultSubstitute(nint pattern);
        [LibraryImport(Library)] private static partial int FcPatternGetInteger(nint pattern, byte* name, int index, out int value);
        [LibraryImport(Library)] private static partial int FcPatternGetBool(nint pattern, byte* name, int index, out int value);
        [LibraryImport(Library)] private static partial nint FcFontMatch(nint config, nint pattern, out int result);

        public static FontRenderSettings Read()
        {
            var pattern = FcPatternCreate();
            try
            {
                fixed (byte* family = "family\0"u8)
                fixed (byte* sans = "sans-serif\0"u8)
                    FcPatternAddString(pattern, family, sans);
                FcConfigSubstitute(0, pattern, MatchPattern);
                FcDefaultSubstitute(pattern);
                // Rendering settings come from the font-target rules, applied when a font is matched.
                var matched = FcFontMatch(0, pattern, out _);
                if (matched != 0)
                {
                    FcPatternDestroy(pattern);
                    pattern = matched;
                }
                int Int(ReadOnlySpan<byte> name, int fallback)
                {
                    fixed (byte* n = name)
                        return FcPatternGetInteger(pattern, n, 0, out var value) == 0 ? value : fallback;
                }
                bool Bool(ReadOnlySpan<byte> name, bool fallback)
                {
                    fixed (byte* n = name)
                        return FcPatternGetBool(pattern, n, 0, out var value) == 0 ? value != 0 : fallback;
                }
                var antialias = Bool("antialias\0"u8, true);
                var hinting = Bool("hinting\0"u8, true);
                var rgba = Int("rgba\0"u8, 0);
                var hintStyle = Int("hintstyle\0"u8, 1);
                var filter = Int("lcdfilter\0"u8, 1);
                return new FontRenderSettings
                {
                    Antialias = !antialias ? TextAntialias.None : rgba switch
                    {
                        1 => TextAntialias.SubpixelRgb,
                        2 => TextAntialias.SubpixelBgr,
                        _ => TextAntialias.Grayscale,
                    },
                    Hinting = !hinting ? TextHinting.None : (TextHinting)Math.Clamp(hintStyle, 0, 3),
                    LcdFilter = filter switch { 0 => 0, 2 => 2, 3 => 16, _ => 1 },
                };
            }
            finally
            {
                FcPatternDestroy(pattern);
            }
        }
    }

    static unsafe partial class Windows
    {
        const uint GetFontSmoothing = 0x004A;
        const uint GetFontSmoothingType = 0x200A;
        const uint GetFontSmoothingOrientation = 0x2012;

        [LibraryImport("user32.dll", EntryPoint = "SystemParametersInfoW")]
        private static partial int SystemParametersInfo(uint action, uint parameter, out uint value, uint winIni);

        public static FontRenderSettings Read()
        {
            SystemParametersInfo(GetFontSmoothing, 0, out var smoothing, 0);
            SystemParametersInfo(GetFontSmoothingType, 0, out var type, 0);
            SystemParametersInfo(GetFontSmoothingOrientation, 0, out var orientation, 0);
            return new FontRenderSettings
            {
                Antialias = smoothing == 0 ? TextAntialias.None
                    : type == 2 ? (orientation == 0 ? TextAntialias.SubpixelBgr : TextAntialias.SubpixelRgb)
                    : TextAntialias.Grayscale,
                Hinting = TextHinting.Slight,
            };
        }
    }
}
