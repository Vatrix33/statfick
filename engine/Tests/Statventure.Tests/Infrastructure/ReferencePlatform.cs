using System.Runtime.CompilerServices;
using Statventure.Platform;
using Statventure.Text;

namespace Statventure.Tests.Infrastructure;

/// <summary>
/// The tests run as on the PC the Chromium references were captured on, whatever machine runs
/// them: Chromium on Linux (<see cref="PlatformBehaviour"/>) with that PC's fontconfig settings
/// (subpixel RGB antialiasing, slight hinting, the default LCD filter). Fonts likewise come from
/// <see cref="SystemFonts"/>.
/// </summary>
static class ReferencePlatform
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        PlatformBehaviour.Windows = false;
        FontRenderSettings.System = new FontRenderSettings { Antialias = TextAntialias.SubpixelRgb, Hinting = TextHinting.Slight, LcdFilter = 1 };
    }
}
