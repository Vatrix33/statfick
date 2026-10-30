using Statventure.Imaging;

namespace Statventure.UI.Paint;

/// <summary>The images the native controls use (Chromium's), decoded from the engine's resources once.</summary>
internal static class ControlImages
{
    static RgbaImage? _cancel, _cancelPressed;

    /// <summary>The search field's cancel button (IDR_SEARCH_CANCEL, IDR_SEARCH_CANCEL_PRESSED).</summary>
    public static RgbaImage SearchCancel(bool pressed) => pressed
        ? _cancelPressed ??= Load("search_cancel_pressed.png")
        : _cancel ??= Load("search_cancel.png");

    static RgbaImage Load(string name)
    {
        using var stream = typeof(ControlImages).Assembly.GetManifestResourceStream("Statventure.UI." + name)
            ?? throw new InvalidOperationException("missing resource " + name);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return ImageFile.Load(memory.ToArray());
    }
}
