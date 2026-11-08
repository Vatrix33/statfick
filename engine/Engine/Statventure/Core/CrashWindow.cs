using System.Diagnostics;
using System.Net;
using Statventure.Graphics;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.Text;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Core;

/// <summary>
/// The look the engine's own small windows share (Docs/Packaging.md, "Crashes"; Q22, Q23; M12 step 5): the editor's dark
/// look (P4's colours) in the engine's embedded Lato (<see cref="EngineFonts"/>), so they draw with no data at all; a
/// heading, the content, and the buttons at the foot on the right. The crash window and the plain message window.
/// </summary>
public static class EngineWindows
{
    /// <summary>Their size (CSS px; a fixed window, not resizable).</summary>
    public const int Width = 640, Height = 240;

    /// <summary>The stylesheet of both windows (every rule under <c>.engine-window</c> but the page's ground).</summary>
    public const string Css = """
        html, body { height: 100%; margin: 0; }
        html { background: #1a1c1a; }
        body { overflow: hidden; color: #eeeae1; font: 13px/20px Lato; }
        .engine-window { display: flex; flex-direction: column; box-sizing: border-box; height: 100%; padding: 16px 20px; }
        .engine-window .content { flex: 1; min-height: 0; overflow: auto; scrollbar-color: #7d817b #292c28; }
        .engine-window h1 { margin: 0 0 8px; font-size: 15px; line-height: 24px; font-weight: 700; }
        .engine-window p { margin: 0; }
        .engine-window .error { max-height: 60px; overflow: auto; overflow-wrap: anywhere; scrollbar-color: #7d817b #292c28; }
        .engine-window .details { margin-top: 12px; color: #aaa9a1; }
        .engine-window .file { display: flex; gap: 8px; margin-top: 4px; }
        .engine-window .message { white-space: pre-wrap; overflow-wrap: anywhere; }
        .engine-window input { flex: 1; min-width: 0; box-sizing: border-box; height: 24px; margin: 0; padding: 0 6px; border: 1px solid #3a3d38; border-radius: 3px; color: #eeeae1; background: #292c28; font: inherit; line-height: 22px; }
        .engine-window input:focus { outline: none; border-color: #d77a50; }
        .engine-window button { flex: none; box-sizing: border-box; height: 24px; margin: 0; padding: 0 10px; border: 1px solid #3a3d38; border-radius: 3px; color: #eeeae1; background: #292c28; font: inherit; line-height: 22px; white-space: nowrap; }
        .engine-window button:hover { border-color: #d77a50; }
        .engine-window button:active { background: #402a23; }
        .engine-window button.primary { border-color: #d77a50; color: #1a1c1a; background: #d77a50; }
        .engine-window button:focus-visible { outline: 2px solid #d77a50; outline-offset: 1px; }
        .engine-window .buttons { display: flex; flex: none; justify-content: flex-end; gap: 8px; margin-top: 16px; }
        """;

    internal static string E(string text) => WebUtility.HtmlEncode(text);

    /// <summary>A document in the windows' look holding <paramref name="markup"/>, its own font library with the engine's Lato.</summary>
    internal static UiDocument Document(FontLibrary fonts, string markup, string name)
    {
        EngineFonts.AddTo(fonts);
        var document = new UiDocument(fonts);
        document.AddStylesheet(Css, name);
        Markup.SetInnerHtml(document.Body, markup);
        return document;
    }

    /// <summary>
    /// Runs <paramref name="document"/> in a window of its own until <paramref name="closed"/> says so or the window's close
    /// is requested: a new window (<paramref name="title"/>, <see cref="Width"/> × <see cref="Height"/>, not resizable,
    /// hidden unless <paramref name="visible"/>), a new GPU device and swapchain, a <see cref="UiHost"/>, and a loop of its own
    /// drawing each frame (vsync); minimized, it draws nothing and sleeps until an event or a wake-up asked for. With
    /// <paramref name="capture"/>, its first frame is saved there as a PNG and <paramref name="captured"/> is called.
    /// <paramref name="eachFrame"/> runs each time round after the document's update (tests). Everything it made is gone
    /// when it returns; what fails (no display, no Vulkan) is thrown.
    /// </summary>
    internal static void Run(UiDocument document, string title, bool visible, Func<bool> closed, string? capture = null, Action? captured = null, Action<Window>? eachFrame = null)
    {
        Window? window = null;
        GpuDevice? gpu = null;
        Swapchain? swapchain = null;
        UiHost? host = null;
        try
        {
            window = Window.Create(new WindowOptions { Title = title, Width = Width, Height = Height, Resizable = false, Visible = visible, MinWidth = Width, MinHeight = Height });
            gpu = GpuDevice.Create(new GpuDeviceOptions { ApplicationName = title });
            swapchain = new Swapchain(gpu, window);
            host = new UiHost(window, gpu, document);
            var clock = Stopwatch.StartNew();
            // Until closed by its document (Close) or by the window's own close: seen before each time round and after the
            // update (a click on Close, the window's close), so it neither draws nor sleeps once closed.
            bool Ended() => closed() || window.CloseRequested;
            while (!Ended())
            {
                Window.PollEvents();
                window.Input.BeginFrame();
                host.Update(clock.Elapsed.TotalSeconds);
                eachFrame?.Invoke(window);
                if (Ended())
                    break;
                if (window.IsMinimized)
                {
                    if (window.TakeWakeUp() is { } wait)
                        Window.WaitEvents(wait);
                    else
                        Window.WaitEvents();
                    continue;
                }
                var frame = gpu.BeginFrame();
                if (swapchain.Acquire(frame) is { } image)
                {
                    host.Render(frame, image.Image);
                    var readback = capture is not null ? Application.CopyForCapture(gpu, frame, image.Image) : null;
                    gpu.EndFrame(frame, image);
                    if (readback is not null)
                    {
                        Application.WriteCapture(gpu, image.Image, readback, capture!);
                        capture = null;
                        captured?.Invoke();
                    }
                }
                else
                    gpu.EndFrame(frame);
            }
        }
        finally
        {
            gpu?.WaitIdle();
            host?.Dispose();
            swapchain?.Dispose();
            gpu?.Dispose();
            window?.Dispose();
        }
    }
}

/// <summary>
/// The crash window (Docs/Packaging.md, "Crashes"; M12-10, Q22–Q24; M12 step 5): what a program shows after a crash, once
/// the application is torn down (<see cref="Application.Run"/>), in a window, device and loop of its own. A heading
/// ("Statventure Sandbox stopped because of an error"), one line naming the error (<see cref="CrashReport.Message"/>),
/// "The details are in:" with the crash file's full path in a read-only field, focused with its text selected for
/// copying, and a Copy button (the clipboard); then "Open logs folder" (the system's file manager on <c>logs/</c>, as the
/// game's "Open game folder", not a dialog; caught if it fails) and Close. Without a crash file (it couldn't be written)
/// the path and Copy are left out; without a logs folder, its button. In the editor's dark look and the engine's Lato
/// (<see cref="EngineWindows"/>), so it draws when the game's packs are damaged or missing.
/// </summary>
public sealed class CrashWindow : IDisposable
{
    readonly FontLibrary _fonts = new();

    /// <summary>The window's markup for <paramref name="program"/>'s <paramref name="crash"/>, its buttons as given.</summary>
    public static string Markup(string program, CrashReport crash, bool logsFolder = true)
    {
        var E = EngineWindows.E;
        var file = crash.File is { } path
            ? $"""<p class="details">The details are in:</p><div class="file"><input class="crash-file" type="text" readonly value="{E(path)}"><button class="copy">Copy</button></div>"""
            : "";
        var open = logsFolder ? """<button class="open-logs">Open logs folder</button>""" : "";
        return $"""<div class="engine-window crash-window"><div class="content"><h1>{E(Heading(program))}</h1><p class="error">{E(crash.Message)}</p>{file}</div><div class="buttons">{open}<button class="close primary">Close</button></div></div>""";
    }

    /// <summary>The heading: "<paramref name="program"/> stopped because of an error" (the terminal's first line, Q24, without its full stop).</summary>
    public static string Heading(string program) => $"{program} stopped because of an error";

    /// <summary>The crash window of <paramref name="program"/> (its name as it shows it) for <paramref name="crash"/>, its "Open logs folder" on <paramref name="logsFolder"/>.</summary>
    public CrashWindow(string program, CrashReport crash, string? logsFolder)
    {
        Program = program;
        Crash = crash;
        LogsFolder = logsFolder;
        Document = EngineWindows.Document(_fonts, Markup(program, crash, logsFolder is not null), "Statventure.Core.CrashWindow");
        Document.QuerySelector(".copy")?.AddEventListener("click", _ => Copy());
        Document.QuerySelector(".open-logs")?.AddEventListener("click", _ => OpenLogsFolder());
        Document.QuerySelector(".close")!.AddEventListener("click", _ => Close());
        // The path selected for copying (select() focuses the field too). Only a rendered field takes focus: laid out
        // first at the window's size (as Chromium's focus() updates the layout first); the host lays it out again at its own.
        Document.SetViewport(EngineWindows.Width, EngineWindows.Height);
        Document.Update(0);
        PathField?.Select();
    }

    /// <summary>The program's name as the window shows it (its title, and the heading's).</summary>
    public string Program { get; }

    /// <summary>The crash it shows.</summary>
    public CrashReport Crash { get; }

    /// <summary>The folder "Open logs folder" opens (null: no such button).</summary>
    public string? LogsFolder { get; }

    /// <summary>Its document.</summary>
    public UiDocument Document { get; }

    /// <summary>The read-only field holding the crash file's path (null without a crash file).</summary>
    public UiElement? PathField => Document.QuerySelector(".crash-file");

    /// <summary>Whether it was closed (Close, or the window's own close).</summary>
    public bool Closed { get; private set; }

    /// <summary>The window it shows in while <see cref="Show"/> runs (tests).</summary>
    internal Window? Window { get; private set; }

    /// <summary>Starts the file manager in place of the system's (tests): given how the system's would be started, gives whether it was.</summary>
    internal Func<ProcessStartInfo, bool>? Launcher { get; init; }

    /// <summary>Copy: the crash file's path to the clipboard (the document's, which a window's host makes the system's).</summary>
    public void Copy()
    {
        if (Crash.File is { } file)
            Document.Clipboard.WriteText(file);
    }

    /// <summary>
    /// "Open logs folder": the system's file manager on <see cref="LogsFolder"/> (<c>xdg-open</c> on Linux, the shell's
    /// open, Explorer, on Windows: <see cref="GameStorage.FileManagerStart"/>), not waited for. Gives whether it was
    /// started; a failure is logged, never thrown (the crash window stays).
    /// </summary>
    public bool OpenLogsFolder()
    {
        if (LogsFolder is not { } folder)
            return false;
        var start = GameStorage.FileManagerStart(StorageFiles.ThisSystem, folder);
        try
        {
            if (Launcher is not null)
                return Launcher(start);
            using var process = Process.Start(start);
            Log.Info("Application", "the logs folder opened in the file manager");
            return true;
        }
        catch (Exception problem)
        {
            Log.Warning("Application", $"the file manager couldn't be opened on {folder}: {problem.Message}");
            return false;
        }
    }

    /// <summary>Close: the window closes (and the program ends with its error code, <see cref="Application.CrashExitCode"/>).</summary>
    public void Close() => Closed = true;

    /// <summary>
    /// Shows it in a window of its own until it is closed (<see cref="EngineWindows.Run"/>; the title the program's name),
    /// hidden unless <paramref name="visible"/> (tests). With <paramref name="capture"/> its first frame is saved there and
    /// it closes (a scripted run, <see cref="Application.CrashCapture"/>). Throws when no window can be made (Q24: the
    /// caller tells the terminal). <paramref name="eachFrame"/> runs each time round (tests).
    /// </summary>
    public void Show(bool visible = true, string? capture = null, Action<CrashWindow>? eachFrame = null)
    {
        try
        {
            EngineWindows.Run(Document, Program, visible, () => Closed, capture, Close, window =>
            {
                Window = window;
                eachFrame?.Invoke(this);
            });
            // Ended by Close, or by the window's own close.
            Closed = true;
        }
        finally
        {
            Window = null;
        }
    }

    public void Dispose() => _fonts.Dispose();
}

/// <summary>
/// A plain message in the crash window's look (Q6; M12 step 5): a heading, the text (its lines kept) and Close, in a
/// window of its own, for what a program must tell before it has a window of its own, where there may be no console to
/// tell it on (on Windows the programs have none: the editor's wrong command line, M12 step 6).
/// </summary>
public sealed class MessageWindow : IDisposable
{
    readonly FontLibrary _fonts = new();

    /// <summary>The window's markup: <paramref name="heading"/>, then <paramref name="text"/> with its lines kept, then Close.</summary>
    public static string Markup(string heading, string text)
    {
        var E = EngineWindows.E;
        return $"""<div class="engine-window message-window"><div class="content"><h1>{E(heading)}</h1><p class="message">{E(text)}</p></div><div class="buttons"><button class="close primary">Close</button></div></div>""";
    }

    public MessageWindow(string heading, string text)
    {
        Document = EngineWindows.Document(_fonts, Markup(heading, text), "Statventure.Core.MessageWindow");
        Document.QuerySelector(".close")!.AddEventListener("click", _ => Close());
    }

    /// <summary>Its document.</summary>
    public UiDocument Document { get; }

    /// <summary>Whether it was closed (Close, or the window's own close).</summary>
    public bool Closed { get; private set; }

    /// <summary>Close: the window closes.</summary>
    public void Close() => Closed = true;

    /// <summary>
    /// Shows it in a window titled <paramref name="title"/> until it is closed (<see cref="EngineWindows.Run"/>), hidden
    /// unless <paramref name="visible"/> (tests); throws when no window can be made. <paramref name="eachFrame"/> runs
    /// each time round (tests).
    /// </summary>
    public void Show(string title, bool visible = true, Action<MessageWindow, Window>? eachFrame = null)
    {
        EngineWindows.Run(Document, title, visible, () => Closed, eachFrame: eachFrame is null ? null : window => eachFrame(this, window));
        // Ended by Close, or by the window's own close.
        Closed = true;
    }

    /// <summary>
    /// Shows <paramref name="heading"/> and <paramref name="text"/> in a window titled <paramref name="title"/> until the
    /// player closes it. Gives false when no window could be shown (no display, no Vulkan: logged), so the caller can
    /// say it elsewhere (the terminal).
    /// </summary>
    public static bool Show(string title, string heading, string text) => Show(title, heading, text, visible: true, eachFrame: null);

    /// <summary><see cref="Show(string, string, string)"/>, its window hidden unless <paramref name="visible"/> and <paramref name="eachFrame"/> run each time round (tests).</summary>
    internal static bool Show(string title, string heading, string text, bool visible, Action<MessageWindow, Window>? eachFrame)
    {
        using var message = new MessageWindow(heading, text);
        try
        {
            message.Show(title, visible, eachFrame);
            return true;
        }
        catch (Exception problem)
        {
            Log.Error("Application", $"the message window couldn't be shown: {problem}");
            return false;
        }
    }

    public void Dispose() => _fonts.Dispose();
}
