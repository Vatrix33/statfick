using System.Runtime.InteropServices;
using Brutal.GlfwApi;
using Statventure.Platform;
using Statventure.Tests.Core;

namespace Statventure.Tests.Platform;

/// <summary>
/// Tests that make a window of their own (hidden: nothing is shown; the swapchain test's is the one visible) run
/// alone: GLFW is one for the process and isn't thread-safe (a hidden window presenting beside the swapchain test's
/// stalled its frames under wlroots), and a drop callback's exception waits in BRUTAL's process-wide aggregator for
/// the next <see cref="Window.PollEvents"/>. (The application tests that read the log are in "Storage log", alone too.)
/// </summary>
[CollectionDefinition("Window", DisableParallelization = true)]
public sealed class WindowCollection;

/// <summary>
/// Files dropped on the window (Docs/Editor.md, step 3): GLFW's drop callback becomes <see cref="Window.PathsDropped"/>
/// with the paths, in order; nothing listening, nothing happens. The callback is called by the test as GLFW calls it on
/// a drop (the one the window registered, with the paths as UTF-8 strings), on a hidden window. A real drop from the
/// desktop is checked by hand (X11 and Wayland).
/// </summary>
[Collection("Window")]
public class WindowDropTests
{
    static Window Hidden() => Window.Create(new WindowOptions { Title = "Statventure drop test", Width = 160, Height = 100, Visible = false });

    /// <summary>What GLFW does on a drop: calls the window's drop callback with the window and the paths as UTF-8 strings.</summary>
    static unsafe void Drop(Window window, params string[] paths)
    {
        // The callback the window registered with GLFW (taken and put back).
        var callback = Glfw.PInvoke.SetDropCallback(window.Native, null!);
        Glfw.PInvoke.SetDropCallback(window.Native, callback);
        Assert.NotNull(callback);
        var strings = paths.Select(Marshal.StringToCoTaskMemUTF8).ToArray();
        try
        {
            fixed (nint* array = strings)
                callback((GlfwWindowHandle*)window.Native.NativeHandle, paths.Length, (byte**)array);
        }
        finally
        {
            foreach (var text in strings)
                Marshal.FreeCoTaskMem(text);
        }
    }

    [Fact]
    public void ADropGivesItsPathsInOrder()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var window = Hidden();
        var drops = new List<string[]>();
        window.PathsDropped += paths => drops.Add([.. paths]);
        Drop(window, "/home/player/mods/my mod.pak", "/home/player/Models/Ärmel/arena.glb", "/tmp");
        // Raised from within the callback, before it returns.
        Assert.Equal([["/home/player/mods/my mod.pak", "/home/player/Models/Ärmel/arena.glb", "/tmp"]], drops);
        Drop(window, "/one.txt");
        Assert.Equal(2, drops.Count);
        Assert.Equal(["/one.txt"], drops[1]);
        // Nothing for the frame's input (the game's UI reads it): a drop isn't an input event.
        Window.PollEvents();
        window.Input.BeginFrame();
        Assert.Empty(window.Input.Events);
    }

    [Fact]
    public void EveryListenerHearsADropAndOneThatLeftDoesnt()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var window = Hidden();
        var (first, second) = (0, 0);
        void Second(IReadOnlyList<string> paths) => second += paths.Count;
        window.PathsDropped += paths => first += paths.Count;
        window.PathsDropped += Second;
        Drop(window, "/a", "/b");
        window.PathsDropped -= Second;
        Drop(window, "/c");
        Assert.Equal((3, 2), (first, second));
    }

    [Fact]
    public void ADropNothingListensToDoesNothing()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var window = Hidden();
        Drop(window, "/home/player/mods/my-mod.pak");
        // An exception in the callback would wait in BRUTAL's aggregator and come out of the next poll.
        Window.PollEvents();
        Assert.False(window.CloseRequested);
    }
}
