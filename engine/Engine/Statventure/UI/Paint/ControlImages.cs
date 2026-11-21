using Statventure.Imaging;

namespace Statventure.UI.Paint;

/// <summary>The images the native controls use (Chromium's), decoded from the engine's resources once.</summary>
internal static class ControlImages
{
    static RgbaImage? _cancel, _cancelPressed, _cancelDark, _cancelPressedDark;

    /// <summary>
    /// The search field's cancel button (IDR_SEARCH_CANCEL, IDR_SEARCH_CANCEL_PRESSED), in the dark scheme its dark images
    /// (IDR_SEARCH_CANCEL_DARK_MODE, IDR_SEARCH_CANCEL_PRESSED_DARK_MODE: ThemePainterDefault::PaintSearchFieldCancelButton
    /// picks them by the button's used colour scheme).
    /// </summary>
    public static RgbaImage SearchCancel(bool pressed, bool dark) => (pressed, dark) switch
    {
        (true, false) => _cancelPressed ??= Load("search_cancel_pressed.png"),
        (false, false) => _cancel ??= Load("search_cancel.png"),
        (true, true) => _cancelPressedDark ??= Load("search_cancel_pressed_dark_mode.png"),
        (false, true) => _cancelDark ??= Load("search_cancel_dark_mode.png"),
    };

    static RgbaImage Load(string name)
    {
        using var stream = typeof(ControlImages).Assembly.GetManifestResourceStream("Statventure.UI." + name)
            ?? throw new InvalidOperationException("missing resource " + name);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return ImageFile.Load(memory.ToArray());
    }
}
