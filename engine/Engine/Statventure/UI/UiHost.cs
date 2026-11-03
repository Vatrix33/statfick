using System.Numerics;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Platform;
using Statventure.UI.Dom;
using Statventure.UI.Paint;
using CanvasRenderer = Statventure.Canvas.Canvas;

namespace Statventure.UI;

/// <summary>
/// Runs a UI document in a window as Chromium 152 runs a page in its own: GLFW's input becomes the
/// document's (DOM key values and codes, wheel notches, focus and leaving), the document's cursor
/// becomes the window's (system shapes, cursor images), the system's clipboard, caret blink time
/// and reduced-motion setting become the document's, and every frame updates and paints it at the
/// window's pixel density. Images and text resources (SVG) come from <see cref="LoadImage"/> and
/// <see cref="LoadText"/>. A minimized window's document is hidden (visibilitychange; no frames,
/// its timers throttled as Chromium's, with the sound's audibility from <see cref="Audio"/>), and
/// the document unloads when the host goes (pagehide, unload).
/// </summary>
public sealed class UiHost : IDisposable
{
    readonly Window _window;
    readonly GpuDevice _gpu;
    readonly UiPainter _painter;
    readonly CanvasRenderer _canvas;
    readonly Dictionary<string, LoadedImage?> _images = [];
    readonly UiHostInput _input;
    Vector2 _viewport;
    float _scale;
    float _pageZoom = 1;

    sealed class LoadedImage
    {
        public required RgbaImage Pixels;
        public GpuImage? Texture;
    }

    public UiDocument Document { get; }

    /// <summary>The 3D views shown in the document's surface elements (Docs/Renderer-3D.md, "3D views in the UI").</summary>
    public SurfaceViews Views { get; }

    /// <summary>Loads an image by url (RGBA, not premultiplied); null when there is none.</summary>
    public Func<string, RgbaImage?>? LoadImage { get; set; }

    /// <summary>Loads a text resource by url (SVG images); null when there is none.</summary>
    public Func<string, string?>? LoadText { get; set; }

    /// <summary>
    /// The application's sound: whether it is audible becomes the document's
    /// (<see cref="UiDocument.Audible"/>), as Chromium's audio service tells a page's scheduler.
    /// </summary>
    public Audio.AudioSystem? Audio { get; set; }

    /// <summary>
    /// The page zoom on top of the window's scale (Electron's zoom factor, 1.2 to the power of the
    /// zoom level); the document lays out at the window's content scale times this.
    /// </summary>
    public float PageZoom
    {
        get => _pageZoom;
        set
        {
            _pageZoom = value;
            Document.SetZoom(_scale > 0 ? _scale : _window.ContentScale, value);
        }
    }

    /// <summary>Pixels a wheel notch scrolls: Chromium 152's 120 on Linux, 100 (three lines of 33⅓) on Windows.</summary>
    public float WheelNotch
    {
        get => _input.WheelNotch;
        set => _input.WheelNotch = value;
    }

    public UiHost(Window window, GpuDevice gpu, UiDocument document)
    {
        _window = window;
        _gpu = gpu;
        Document = document;
        document.Clipboard = new SystemClipboard();
        document.CaretBlinkInterval = SystemSettings.CaretBlinkInterval;
        document.Layout.Context.ImageSize = url => Image(url) is { } image ? new Vector2(image.Pixels.Width, image.Pixels.Height) : null;
        Views = new SurfaceViews(document);
        _painter = new UiPainter(document)
        {
            ImageTexture = url => Texture(url)?.BindlessIndex,
            TextResource = url => LoadText?.Invoke(url),
            SurfaceTexture = Views.Texture,
        };
        _canvas = new CanvasRenderer(gpu);
        _input = new UiHostInput(document.Input, Window.KeyName)
        {
            WheelNotch = Platform.PlatformBehaviour.Windows ? 100 : 120,
            // At the update's time: a hidden document's clock waits at its last wake-up.
            Visibility = hidden => document.SetHidden(hidden, _time),
        };
        // A task posted from another thread wakes the loop sleeping while the window is minimized.
        document.EventLoop.Wake = Window.PostEmptyEvent;
        _reducedMotion = SystemSettings.ReducedMotion;
        ResizeViewport();
    }

    readonly bool _reducedMotion;

    /// <summary>The system clipboard (GLFW's).</summary>
    sealed class SystemClipboard : IUiClipboard
    {
        public string? ReadText() => Window.Clipboard;
        public void WriteText(string text) => Window.Clipboard = text;
    }

    LoadedImage? Image(string url)
    {
        if (_images.TryGetValue(url, out var image))
            return image;
        image = LoadImage?.Invoke(url) is { Width: > 0, Height: > 0 } pixels ? new LoadedImage { Pixels = pixels } : null;
        _images[url] = image;
        return image;
    }

    GpuImage? Texture(string url)
    {
        if (Image(url) is not { } image)
            return null;
        return image.Texture ??= GpuImage.CreateTexture(_gpu, image.Pixels.Width, image.Pixels.Height, image.Pixels.Pixels);
    }

    /// <summary>The document's viewport is the window's size in window units (DIPs), laid out at the window's content scale.</summary>
    void ResizeViewport()
    {
        var size = new Vector2(_window.Width, _window.Height);
        var scale = _window.ContentScale;
        if ((size == _viewport && scale == _scale) || size.X <= 0 || size.Y <= 0)
            return;
        _viewport = size;
        _scale = scale;
        Document.SetZoom(scale, _pageZoom);
        Document.SetViewport(size.X, size.Y, _reducedMotion);
    }

    /// <summary>
    /// A frame: the window's input of this frame goes to the document, which then updates at
    /// <paramref name="time"/> (seconds); the window shows the document's cursor.
    /// </summary>
    double _time;

    public void Update(double time)
    {
        _time = time;
        ResizeViewport();
        _input.Dispatch(_window.Input.Events);
        Document.Audible = Audio?.Audible ?? false;
        Document.Update(time);
        if (Document.Hidden)
        {
            // No frame: the loop sleeps until the document's next throttled timer or change.
            if (Document.NextWakeUp is { } next)
                _window.RequestWakeUp(next - time);
            return;
        }
        UpdateCursor();
    }

    /// <summary>The window closes: the document unloads (pagehide, visibilitychange, unload; <see cref="UiDocument.Unload"/>).</summary>
    public void Unload() => Document.Unload();

    /// <summary>
    /// The window shows the document's cursor: a cursor image (scaled to the window's pixel
    /// density), else the keyword's system cursor. GLFW has no wait, help, cell, alias, copy,
    /// zoom or grab shapes; those show the arrow unless the page gives an image.
    /// </summary>
    void UpdateCursor()
    {
        var input = Document.Input;
        if (input.CursorImage is { } custom && Image(custom.Url) is { } image)
        {
            var scale = _window.ContentScale;
            var hotX = (int)MathF.Round(custom.Hotspot.X * scale);
            var hotY = (int)MathF.Round(custom.Hotspot.Y * scale);
            var key = $"{custom.Url}@{scale}";
            _window.SetCursorImage(key, scale == 1 ? image.Pixels : Scale(image.Pixels, scale), hotX, hotY);
            return;
        }
        _window.Cursor = input.Cursor switch
        {
            "none" => CursorShape.Hidden,
            "pointer" => CursorShape.Hand,
            "text" or "vertical-text" => CursorShape.Text,
            "crosshair" => CursorShape.Crosshair,
            "move" or "all-scroll" => CursorShape.Move,
            "not-allowed" or "no-drop" => CursorShape.NotAllowed,
            "ew-resize" or "e-resize" or "w-resize" or "col-resize" => CursorShape.ResizeHorizontal,
            "ns-resize" or "n-resize" or "s-resize" or "row-resize" => CursorShape.ResizeVertical,
            "nwse-resize" or "nw-resize" or "se-resize" => CursorShape.ResizeNwse,
            "nesw-resize" or "ne-resize" or "sw-resize" => CursorShape.ResizeNesw,
            _ => CursorShape.Arrow,
        };
    }

    /// <summary>An image resampled bilinearly to <paramref name="scale"/> times its size.</summary>
    static RgbaImage Scale(RgbaImage source, float scale)
    {
        var width = Math.Max(1, (int)MathF.Round(source.Width * scale));
        var height = Math.Max(1, (int)MathF.Round(source.Height * scale));
        var result = new RgbaImage(width, height);
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var sx = Math.Clamp((x + 0.5f) / scale - 0.5f, 0, source.Width - 1);
                var sy = Math.Clamp((y + 0.5f) / scale - 0.5f, 0, source.Height - 1);
                int x0 = (int)sx, y0 = (int)sy;
                int x1 = Math.Min(x0 + 1, source.Width - 1), y1 = Math.Min(y0 + 1, source.Height - 1);
                float fx = sx - x0, fy = sy - y0;
                for (var c = 0; c < 4; c++)
                {
                    float At(int px, int py) => source.Pixels[(py * source.Width + px) * 4 + c];
                    var top = At(x0, y0) + (At(x1, y0) - At(x0, y0)) * fx;
                    var bottom = At(x0, y1) + (At(x1, y1) - At(x0, y1)) * fx;
                    result.Pixels[(y * width + x) * 4 + c] = (byte)MathF.Round(top + (bottom - top) * fy);
                }
            }
        return result;
    }

    /// <summary>
    /// Paints the document into <paramref name="target"/> (the window's pixels; the document already
    /// works in device pixels), its 3D views rendered first where they changed.
    /// </summary>
    public void Render(Frame frame, GpuImage target)
    {
        Views.Update(frame);
        _canvas.Begin(target.Width, target.Height);
        _painter.Paint(_canvas);
        _canvas.Render(frame, target);
        Document.Painted();
    }

    public void Dispose()
    {
        // The document unloads with its window (its listeners run here if Unload wasn't called).
        Document.Unload();
        Document.EventLoop.Wake = null;
        _painter.Dispose();
        _canvas.Dispose();
        foreach (var image in _images.Values)
            image?.Texture?.Dispose();
        _images.Clear();
    }
}
