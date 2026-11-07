using System.Numerics;
using Brutal.GlfwApi;
using Brutal.Numerics;
using Statventure.Core;
using Statventure.Imaging;

namespace Statventure.Platform;

public sealed record WindowOptions
{
    public string Title { get; init; } = EngineInfo.Name;
    /// <summary>Size in logical units; the window system scales it on high-density screens.</summary>
    public int Width { get; init; } = 1280;
    public int Height { get; init; } = 720;
    public bool Resizable { get; init; } = true;
    public bool Visible { get; init; } = true;
    public int MinWidth { get; init; } = 320;
    public int MinHeight { get; init; } = 240;
}

/// <summary>
/// A desktop window (GLFW) without a graphics API attached; the GPU device draws into it through a
/// swapchain. Sizes come in two units: logical units (the window size, mouse positions, UI layout)
/// and pixels (the framebuffer). <see cref="ContentScale"/> converts between them.
/// </summary>
public sealed class Window : IDisposable
{
    static int _glfwUsers;
    static readonly object GlfwGate = new();
    readonly Dictionary<CursorShape, GlfwCursor> _cursors = [];
    CursorShape _cursor = CursorShape.Arrow;
    bool _fullscreen;
    int2 _windowedPosition, _windowedSize;

    public GlfwWindow Native { get; }
    public Input Input { get; } = new();

    /// <summary>Raised from <see cref="PollEvents"/> when the framebuffer changes size (in pixels).</summary>
    public event Action<int, int>? FramebufferResized;

    /// <summary>
    /// Raised from <see cref="PollEvents"/> when files or folders are dropped on the window from the desktop
    /// (GLFW's drop callback; Docs/Editor.md, step 3): their paths as the window system gives them, in the
    /// order it gives them. GLFW reports only the drop: no position and no events while dragging, so nothing
    /// can be shown before it. The game doesn't listen, and a drop nothing listens to does nothing.
    /// </summary>
    public event Action<IReadOnlyList<string>>? PathsDropped;

    Window(GlfwWindow native)
    {
        Native = native;
        native.OnKey += (_, key, scancode, action, mods) =>
        {
            var kind = action switch
            {
                GlfwKeyAction.Press => InputEventKind.KeyDown,
                GlfwKeyAction.Repeat => InputEventKind.KeyRepeat,
                _ => InputEventKind.KeyUp,
            };
            Input.Push(new InputEvent(kind, Key: (Key)(int)key, Modifiers: (KeyModifiers)(int)mods, Scancode: scancode));
        };
        native.OnChar += (_, codepoint) => Input.Push(new InputEvent(InputEventKind.Text, Codepoint: (int)codepoint));
        native.OnCursorPos += (_, position) => Input.Push(new InputEvent(InputEventKind.MouseMove, Position: new Vector2((float)position.X, (float)position.Y)));
        native.OnMouseButton += (_, button, action, mods) =>
        {
            var kind = action == GlfwButtonAction.Press ? InputEventKind.MouseDown : InputEventKind.MouseUp;
            var position = Native.CursorPos;
            Input.Push(new InputEvent(kind, Button: (MouseButton)(int)button, Modifiers: (KeyModifiers)(int)mods, Position: new Vector2((float)position.X, (float)position.Y)));
        };
        native.OnScroll += (_, offset) => Input.Push(new InputEvent(InputEventKind.Scroll, Scroll: new Vector2((float)offset.X, (float)offset.Y)));
        native.OnCursorEnter += (_, entered) =>
        {
            if (!entered)
                Input.Push(new InputEvent(InputEventKind.MouseLeave));
        };
        native.OnFocus += (_, focus) =>
            Input.Push(new InputEvent(focus == GlfwFocusEvent.LostFocus ? InputEventKind.FocusLost : InputEventKind.FocusGained));
        // Minimizing (X11 and Windows; Wayland's xdg-shell never tells, so GLFW never calls this there).
        native.OnIconify += (_, iconify) =>
            Input.Push(new InputEvent(iconify == GlfwIconifyEvent.Restored ? InputEventKind.Restored : InputEventKind.Minimized));
        // Caps Lock and Num Lock come with the modifiers (the keypad's keys depend on Num Lock).
        native.SetInputMode(GlfwInputMode.LockKeyMods, 1);
        native.OnFramebufferSize += (_, size) => FramebufferResized?.Invoke(size.X, size.Y);
        // Files dropped from the desktop: the paths only (X11's XDND and Wayland's data device, as GLFW 3.4 reads them).
        native.OnDrop += (_, _, paths) => PathsDropped?.Invoke(paths);
    }

    public static Window Create(WindowOptions options)
    {
        lock (GlfwGate)
        {
            if (_glfwUsers++ == 0 && !Glfw.Init())
            {
                _glfwUsers--;
                throw new InvalidOperationException("GLFW could not start (no display?)");
            }
        }
        Glfw.WindowHint(GlfwWindowHint.ClientApi, 0);
        Glfw.WindowHint(GlfwWindowHint.Resizable, options.Resizable ? 1 : 0);
        Glfw.WindowHint(GlfwWindowHint.Visible, options.Visible ? 1 : 0);
        var native = Glfw.CreateWindow(new GlfwWindow.CreateInfo { Title = options.Title, Size = new int2(options.Width, options.Height) });
        native.SetSizeLimits(new int2(options.MinWidth, options.MinHeight), new int2(-1, -1));
        var window = new Window(native);
        Log.Info("Platform", $"window {options.Width}x{options.Height}, framebuffer {window.FramebufferWidth}x{window.FramebufferHeight}, scale {window.ContentScale:0.##}");
        return window;
    }

    /// <summary>Processes window-system events for every window; call once per frame.</summary>
    public static void PollEvents() => Glfw.PollEvents();

    /// <summary>Sleeps until an event arrives (used while minimized).</summary>
    public static void WaitEvents() => Glfw.WaitEvents();

    /// <summary>Sleeps until an event arrives or <paramref name="seconds"/> have passed.</summary>
    public static void WaitEvents(double seconds) => Glfw.WaitEventsTimeout(Math.Max(seconds, 0));

    /// <summary>Wakes a thread sleeping in <see cref="WaitEvents()"/> (callable from any thread).</summary>
    public static void PostEmptyEvent() => Glfw.PostEmptyEvent();

    double? _wakeUp;

    /// <summary>
    /// Asks the loop that sleeps while the window is minimized to wake within
    /// <paramref name="seconds"/> (a hidden document's next timer); the earliest request wins.
    /// </summary>
    public void RequestWakeUp(double seconds) => _wakeUp = _wakeUp is { } earlier ? Math.Min(earlier, seconds) : seconds;

    /// <summary>The earliest wake-up requested since the last call (seconds from the request), and forgets it.</summary>
    public double? TakeWakeUp()
    {
        var wakeUp = _wakeUp;
        _wakeUp = null;
        return wakeUp;
    }

    public static bool VulkanSupported => Glfw.VulkanSupported();

    /// <summary>The instance extensions Vulkan needs to draw into windows on this system.</summary>
    public static unsafe IReadOnlyList<string> RequiredVulkanExtensions()
    {
        var names = Glfw.GetRequiredInstanceExtensions(out var count);
        var list = new List<string>((int)count);
        for (var i = 0; i < count; i++)
            list.Add(System.Runtime.InteropServices.Marshal.PtrToStringUTF8((nint)names[i])!);
        return list;
    }

    public string Title
    {
        get => Native.Title;
        set => Native.Title = value;
    }

    public int Width => Native.Size.X;
    public int Height => Native.Size.Y;
    public int FramebufferWidth => Native.FramebufferSize.X;
    public int FramebufferHeight => Native.FramebufferSize.Y;

    /// <summary>Pixels per logical unit (1 on normal screens, 2 on double-density ones).</summary>
    public float ContentScale => Width > 0 ? FramebufferWidth / (float)Width : Native.ContentScale.X;

    /// <summary>The window is minimized: GLFW says so (X11, Windows; never on Wayland) or there is nothing to draw into (Windows).</summary>
    public bool IsMinimized => MinimizedForTests || Native.IsIconified || FramebufferWidth == 0 || FramebufferHeight == 0;

    /// <summary>
    /// Tests: the window counts as minimized without the window system minimizing it, so the application's
    /// loop runs its minimized branch (updates, no frames) on a hidden window, which nothing shows and which
    /// Wayland can't minimize.
    /// </summary>
    internal bool MinimizedForTests { get; set; }

    /// <summary>
    /// GLFW's close flag: set when the player closes the window (or by setting it), cleared by setting it
    /// false. The application's loop ends on it once the application agrees (<c>Application.ConfirmClose</c>).
    /// </summary>
    public bool CloseRequested
    {
        get => Native.ShouldClose;
        set => Native.ShouldClose = value;
    }

    public bool Fullscreen
    {
        get => _fullscreen;
        set
        {
            if (value == _fullscreen)
                return;
            if (value)
            {
                _windowedPosition = new int2(0, 0);
                _windowedSize = Native.Size;
                var monitor = Glfw.PrimaryMonitor;
                var mode = monitor.VideoMode;
                Native.SetFullscreen(monitor, new int2(mode.Width, mode.Height), mode.RefreshRate);
            }
            else
            {
                Native.SetWindowed(_windowedPosition, _windowedSize);
                Native.CenterToMonitor(Glfw.PrimaryMonitor);
            }
            _fullscreen = value;
        }
    }

    public CursorShape Cursor
    {
        get => _cursor;
        set
        {
            if (value == _cursor && _imageCursor is null)
                return;
            _cursor = value;
            _imageCursor = null;
            Native.SetInputMode(GlfwInputMode.Cursor, (int)(value == CursorShape.Hidden ? GlfwCursorMode.Hidden : GlfwCursorMode.Normal));
            if (value == CursorShape.Hidden)
                return;
            if (!_cursors.TryGetValue(value, out var cursor))
            {
                cursor = Glfw.CreateCursor(value switch
                {
                    CursorShape.Text => GlfwStandardCursor.IBeam,
                    CursorShape.Hand => GlfwStandardCursor.PointingHand,
                    CursorShape.Crosshair => GlfwStandardCursor.Crosshair,
                    CursorShape.ResizeHorizontal => GlfwStandardCursor.ResizeEw,
                    CursorShape.ResizeVertical => GlfwStandardCursor.ResizeNs,
                    CursorShape.ResizeNwse => GlfwStandardCursor.ResizeNwse,
                    CursorShape.ResizeNesw => GlfwStandardCursor.ResizeNesw,
                    CursorShape.Move => GlfwStandardCursor.ResizeAll,
                    CursorShape.NotAllowed => GlfwStandardCursor.NotAllowed,
                    _ => GlfwStandardCursor.Arrow,
                });
                _cursors[value] = cursor;
            }
            Native.SetCursor(cursor);
        }
    }

    readonly Dictionary<string, GlfwCursor> _imageCursors = [];
    string? _imageCursor;

    /// <summary>Shows an image as the cursor (straight RGBA, in screen pixels), its hotspot in pixels from the top left; cached by <paramref name="key"/>.</summary>
    public unsafe void SetCursorImage(string key, RgbaImage image, int hotspotX, int hotspotY)
    {
        if (_imageCursor == key)
            return;
        if (!_imageCursors.TryGetValue(key, out var cursor))
        {
            fixed (byte* pixels = image.Pixels)
            {
                cursor = Glfw.CreateCursor(new GlfwCursor.CreateInfo
                {
                    Image = new GlfwImage { Width = image.Width, Height = image.Height, Pixels = pixels },
                    Hot = new int2(hotspotX, hotspotY),
                });
            }
            _imageCursors[key] = cursor;
        }
        _imageCursor = key;
        Native.SetInputMode(GlfwInputMode.Cursor, (int)GlfwCursorMode.Normal);
        Native.SetCursor(cursor);
    }

    /// <summary>The character a printable key types on this keyboard layout without modifiers (GLFW's key name), or null.</summary>
    public static string? KeyName(Key key, int scancode) => Glfw.GetKeyName((GlfwKey)(int)key, scancode);

    public static string? Clipboard
    {
        get => Glfw.Clipboard;
        set => Glfw.SetClipboard(value ?? "");
    }

    public void Dispose()
    {
        Native.Dispose();
        lock (GlfwGate)
        {
            if (--_glfwUsers == 0)
                Glfw.Terminate();
        }
    }
}
