using System.Runtime.InteropServices;
using Brutal.GlfwApi;
using Statventure.Audio;
using Statventure.Editor;
using Statventure.Editor.Session;
using Statventure.Editor.Settings;
using Statventure.Platform;
using Statventure.Tests.Core;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Editor.Opening;

/// <summary>
/// The editor's program wired as step 8 says (Docs/Editor.md, "Opening packs (step 8)"): the window's size and the page
/// zoom from <c>editor/shell.json</c> at start and written back at close, the player's folder given to the session,
/// files dropped on the window opening or waiting for import, and a close with unsaved work asked about first
/// (M11-17), the loop going on until it is answered. A real application in a hidden window counted as minimized (no
/// frame is presented; nothing is shown), in the "Window" collection (GLFW is one for the process).
/// </summary>
[Collection("Window")]
public class EditorApplicationTests
{
    /// <summary>What GLFW does on a drop: calls the window's drop callback with the window and the paths as UTF-8 strings (as step 3's tests).</summary>
    static unsafe void Drop(Window window, params string[] paths)
    {
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
    public void TheEditorStartsAsItWasLeftAsksBeforeClosingOverUnsavedWorkAndRemembers()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var folder = new StorageFolder();
        var player = folder["player"];
        new ShellSettings(2, 1000, 700).Write(player);
        var mod = folder["player/mods/my-mod.pak"];
        Directory.CreateDirectory(Path.GetDirectoryName(mod)!);
        var writer = new Statventure.Packs.PackWriter();
        writer.AddRecord("audio/rooms/crypt", new RoomSpec(2, 0.5, 0.5));
        writer.Write(mod);
        var image = folder.Write("drops/stone.png", [1, 2, 3]);
        var seen = new List<string>();
        var updates = 0;
        var asked = false;
        var application = new EditorApplication(GameData.Repo(EditorHarness.EngineFolder), player, visible: false)
        {
            AfterLoad = a =>
            {
                a.Window.MinimizedForTests = true;
                seen.Add($"load {a.Window.Width}x{a.Window.Height} zoom {a.View.ZoomLevel} {a.Session.PlayerFolder == Path.GetFullPath(player)}");
            },
            AfterUpdate = a =>
            {
                var n = updates++;
                if (n == 0)
                {
                    // A pack dropped on the window opens; another file waits for import.
                    Drop(a.Window, mod, image);
                }
                else if (n == 2)
                {
                    seen.Add($"opened {a.Session.PackName}, waiting {string.Join(", ", a.Session.PendingImports.Select(Path.GetFileName))}");
                    var crypt = a.Session.Entry("audio/rooms/crypt")!;
                    a.Session.Commit(crypt, crypt.Draft!.With("Seconds", Statventure.Records.DraftValue.Of(4.0)));
                    // Resized, then maximized: the size remembered is the last normal one.
                    a.WindowStateForTests = () => (1100, 750, true);
                }
                else if (n == 3)
                {
                    a.WindowStateForTests = () => (1600, 1000, false);
                    a.Window.CloseRequested = true;
                }
                else if (n > 3 && !asked && a.View.Dialogs.Top is { } dialog)
                {
                    asked = true;
                    seen.Add($"asked: {dialog.Element.ElementChildren.First().TextContent}; close requested {a.Window.CloseRequested}; running {a.Running}");
                    var discard = dialog.Buttons.ElementChildren.Single(b => b.TextContent == "Discard");
                    a.View.Document.RunScript(() => discard.Click());
                }
                // Woken at once: the minimized loop doesn't sleep; past enough updates it ends, so a loop that wouldn't fails rather than hangs.
                a.Window.RequestWakeUp(0);
                if (n >= 40)
                    a.Quit();
            },
        };
        application.Run();
        Assert.Equal([
            "load 1000x700 zoom 2 True",
            "opened my-mod.pak, waiting stone.png",
            "asked: Unsaved changes; close requested False; running True",
        ], seen);
        Assert.InRange(updates, 5, 39);
        Assert.Equal(new ShellSettings(2, 1100, 750), ShellSettings.Read(player));
    }
}
