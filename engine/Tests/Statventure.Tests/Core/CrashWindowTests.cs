using System.ComponentModel;
using System.Diagnostics;
using Statventure.Core;
using Statventure.Platform;
using Statventure.Storage;
using Statventure.UI.Dom;
using Statventure.Tests.Storage;

namespace Statventure.Tests.Core;

/// <summary>
/// The crash window and the plain message window (Docs/Packaging.md, "Crashes"; Q6, Q22–Q24; M12 step 5) on their own:
/// their documents (the heading, the error's line, the crash file's path in a read-only field focused with its text
/// selected, Copy, "Open logs folder", Close; the message's heading, text and Close), what their buttons do (the
/// clipboard, the file manager on the logs folder, failures caught, closing) and their windows: a window, device and loop
/// of their own, hidden and counted as minimized here (a hidden window's presents hang under wlroots after the first),
/// ended by Close or the window's own close, a failure thrown to the caller (Q24). Their look against Chromium:
/// <see cref="CrashWindowPaintReferenceTests"/>; shown by the application after a crash: <see cref="ApplicationScreensTests"/>.
/// In the non-parallel "Storage log" collection (their windows, and the log lines read).
/// </summary>
[Collection("Storage log")]
public class CrashWindowTests : IDisposable
{
    readonly LogLevel _level = Log.Level;

    public CrashWindowTests() => Log.Level = LogLevel.Info;

    public void Dispose() => Log.Level = _level;

    const string Program = "Statventure Sandbox";

    static readonly DateTimeOffset Time = new(2026, 11, 8, 14, 5, 10, TimeSpan.FromHours(1));

    static CrashReport Report(string? file, string message = "the sandbox crashed on purpose (--crash)") =>
        new(new InvalidOperationException(message), "on the main thread", Time, file);

    const string File = "/home/player/.local/share/stat-fick/logs/crash-2026-11-08_14-05-10.txt";
    const string Logs = "/home/player/.local/share/stat-fick/logs";

    static string[] Buttons(UiDocument document) => [.. document.QuerySelectorAll("button").Select(b => b.TextContent)];

    [Fact]
    public void TheCrashWindowNamesTheErrorAndHoldsThePathSelectedForCopying()
    {
        using var window = new CrashWindow(Program, Report(File), Logs);
        var document = window.Document;
        Assert.Equal("Statventure Sandbox stopped because of an error", document.QuerySelector("h1")!.TextContent);
        Assert.Equal(CrashWindow.Heading(Program), document.QuerySelector("h1")!.TextContent);
        Assert.Equal("InvalidOperationException: the sandbox crashed on purpose (--crash)", document.QuerySelector(".error")!.TextContent);
        Assert.Equal(window.Crash.Message, document.QuerySelector(".error")!.TextContent);
        Assert.Equal("The details are in:", document.QuerySelector(".details")!.TextContent);
        // The path in a read-only text field, focused, its whole text selected.
        var field = window.PathField!;
        Assert.Equal("input", field.Tag);
        Assert.Equal("text", field.InputType);
        Assert.True(field.ReadOnly);
        Assert.Equal(File, field.Value);
        Assert.Same(field, document.FocusedElement);
        Assert.Equal((0, File.Length), (field.SelectionStart!.Value, field.SelectionEnd!.Value));
        // Copy beside it; then "Open logs folder" and Close, the main one.
        Assert.Equal(["Copy", "Open logs folder", "Close"], Buttons(document));
        Assert.Same(field.Parent, document.QuerySelector(".copy")!.Parent);
        Assert.True(document.QuerySelector(".close")!.HasClass("primary"));
        Assert.Equal(Program, window.Program);
        Assert.Equal(Logs, window.LogsFolder);
        // The engine's Lato in its own library, nothing from the game's data.
        Assert.Null(document.Files);
    }

    [Fact]
    public void ItsTextIsTextNotMarkup()
    {
        using var window = new CrashWindow("A <b>program</b>", Report("/tmp/a \"quoted\" & <odd> path.txt", "a <b>bold</b> & \"quoted\" message\non two lines"), Logs);
        Assert.Equal("A <b>program</b> stopped because of an error", window.Document.QuerySelector("h1")!.TextContent);
        Assert.Equal("InvalidOperationException: a <b>bold</b> & \"quoted\" message on two lines", window.Document.QuerySelector(".error")!.TextContent);
        Assert.Equal("/tmp/a \"quoted\" & <odd> path.txt", window.PathField!.Value);
        Assert.Empty(window.Document.QuerySelectorAll("b"));
    }

    [Fact]
    public void WithoutACrashFileOrALogsFolderTheirPartsAreLeftOut()
    {
        using (var noFile = new CrashWindow(Program, Report(null), Logs))
        {
            Assert.Null(noFile.PathField);
            Assert.Null(noFile.Document.QuerySelector(".details"));
            Assert.Equal(["Open logs folder", "Close"], Buttons(noFile.Document));
            Assert.Null(noFile.Document.FocusedElement);
            // Copy has nothing to copy.
            noFile.Copy();
            Assert.Null(noFile.Document.Clipboard.ReadText());
        }
        using var noFolder = new CrashWindow(Program, Report(File), null);
        Assert.Equal(["Copy", "Close"], Buttons(noFolder.Document));
        Assert.False(noFolder.OpenLogsFolder());
    }

    [Fact]
    public void CopyPutsThePathOnTheClipboard()
    {
        using var window = new CrashWindow(Program, Report(File), Logs);
        // The document's clipboard: in a window, the host makes it the system's (UiHost's SystemClipboard).
        var clipboard = new MemoryClipboard();
        window.Document.Clipboard = clipboard;
        clipboard.WriteText("something else");
        window.Document.RunScript(() => window.Document.QuerySelector(".copy")!.Click());
        Assert.Equal(File, clipboard.ReadText());
    }

    [Fact]
    public void OpenLogsFolderStartsTheFileManagerOnTheLogsFolderAndCatchesAFailure()
    {
        var started = new List<ProcessStartInfo>();
        using var window = new CrashWindow(Program, Report(File), Logs) { Launcher = start => { started.Add(start); return true; } };
        window.Document.RunScript(() => window.Document.QuerySelector(".open-logs")!.Click());
        var start = Assert.Single(started);
        var expected = GameStorage.FileManagerStart(OperatingSystem.IsWindows() ? StorageSystem.Windows : StorageSystem.Linux, Logs);
        Assert.Equal(expected.FileName, start.FileName);
        Assert.Equal(expected.ArgumentList, start.ArgumentList);
        Assert.Equal(expected.UseShellExecute, start.UseShellExecute);
        Assert.Equal(expected.WorkingDirectory, start.WorkingDirectory);
        if (!OperatingSystem.IsWindows())
        {
            // xdg-open on the logs folder, not a dialog.
            Assert.Equal("xdg-open", start.FileName);
            Assert.Equal([Logs], start.ArgumentList);
        }
        Assert.False(window.Closed);

        // A launcher that fails: caught and logged, the window stays.
        using var failing = new CrashWindow(Program, Report(File), Logs) { Launcher = _ => throw new Win32Exception(2, "No such file or directory") };
        var opened = true;
        var logged = TestApplication.CaptureLog("Application", () => opened = failing.OpenLogsFolder());
        Assert.False(opened);
        Assert.Equal([$"Warning: the file manager couldn't be opened on {Logs}: No such file or directory"], logged);
        Assert.False(failing.Closed);
        // Through the button too: nothing thrown out of the click.
        failing.Document.RunScript(() => failing.Document.QuerySelector(".open-logs")!.Click());
        // A launcher that says it didn't start.
        using var refused = new CrashWindow(Program, Report(File), Logs) { Launcher = _ => false };
        Assert.False(refused.OpenLogsFolder());
    }

    [Fact]
    public void CloseClosesIt()
    {
        using var window = new CrashWindow(Program, Report(File), Logs);
        Assert.False(window.Closed);
        window.Document.RunScript(() => window.Document.QuerySelector(".copy")!.Click());
        Assert.False(window.Closed);
        window.Document.RunScript(() => window.Document.QuerySelector(".close")!.Click());
        Assert.True(window.Closed);
    }

    [Fact]
    public void TheMessageWindowShowsItsHeadingAndTextWithItsLinesAndClose()
    {
        const string text = "'--bad' isn't an option\nusage: statventure-editor [--repo <engine folder> | --game <game folder>]";
        using var message = new MessageWindow("Statventure Editor can't start", text);
        var document = message.Document;
        Assert.Equal("Statventure Editor can't start", document.QuerySelector("h1")!.TextContent);
        var paragraph = document.QuerySelector(".message")!;
        Assert.Equal(text, paragraph.TextContent);
        document.Update(0);
        Assert.Equal(Statventure.Text.WhiteSpace.PreWrap, paragraph.Style!.WhiteSpace);
        Assert.Equal(["Close"], Buttons(document));
        Assert.Empty(document.QuerySelectorAll("b"));
        Assert.False(message.Closed);
        document.RunScript(() => document.QuerySelector(".close")!.Click());
        Assert.True(message.Closed);
    }

    [Fact]
    public void BothDrawInTheEnginesLookAndLato()
    {
        using var crash = new CrashWindow(Program, Report(File), Logs);
        using var message = new MessageWindow("Heading", "Text");
        foreach (var document in new[] { crash.Document, message.Document })
        {
            document.SetViewport(EngineWindows.Width, EngineWindows.Height);
            document.Update(0);
            Assert.Equal(["Lato"], document.Body.Style!.FontFamilies);
            Assert.Equal(Color.FromBytes(0x1a, 0x1c, 0x1a), document.Root.Style!.BackgroundColor);
            Assert.Equal(Color.FromBytes(0xee, 0xea, 0xe1), document.Body.Style.Color);
            Assert.False(document.Fonts.Resolve(["Lato"], 700, false)!.Value.SyntheticBold);
        }
        Assert.Equal((640, 240), (EngineWindows.Width, EngineWindows.Height));
    }

    // ---------- In a window of their own (hidden, counted as minimized: no frame presented) ----------

    /// <summary>A window a test expected to end long ago, ended both ways (Close and the window's close): the test fails on its count rather than hangs.</summary>
    internal static void EndAnyway(Window window, Action close)
    {
        window.CloseRequested = true;
        close();
    }

    [Fact]
    public void TheCrashWindowRunsInAWindowOfItsOwnUntilClosed()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var window = new CrashWindow(Program, Report(File), Logs);
        var frames = 0;
        string? seen = null;
        window.Show(visible: false, eachFrame: w =>
        {
            Assert.NotNull(w.Window);
            w.Window.MinimizedForTests = true;
            w.Window.RequestWakeUp(0);
            seen ??= $"{w.Window.Title}, {w.Window.Width}x{w.Window.Height}, resizable {Convert.ToInt32(w.Window.Native.GetAttribute(Brutal.GlfwApi.GlfwWindowAttribute.Resizable))}, visible {w.Window.Native.Visible}";
            if (++frames == 2)
            {
                // A click on Close through the window's input, as the player's (the next frame's update takes it).
                var close = w.Document.QuerySelector(".close")!.GetClientRects()[0];
                var at = new System.Numerics.Vector2(close.X + close.Width / 2, close.Y + close.Height / 2);
                w.Window.Input.Push(new InputEvent(InputEventKind.MouseMove, Position: at));
                w.Window.Input.Push(new InputEvent(InputEventKind.MouseDown, Position: at));
                w.Window.Input.Push(new InputEvent(InputEventKind.MouseUp, Position: at));
            }
            else if (frames > 10)
                EndAnyway(w.Window, w.Close);
        });
        Assert.Equal(3, frames);
        Assert.Equal($"{Program}, 640x240, resizable 0, visible False", seen);
        Assert.True(window.Closed);
        Assert.Null(window.Window);
    }

    [Fact]
    public void TheWindowsOwnCloseClosesItToo()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var window = new CrashWindow(Program, Report(File), Logs);
        var frames = 0;
        window.Show(visible: false, eachFrame: w =>
        {
            w.Window!.MinimizedForTests = true;
            w.Window.RequestWakeUp(0);
            if (++frames == 2)
                w.Window.CloseRequested = true;
            else if (frames > 10)
                EndAnyway(w.Window, w.Close);
        });
        Assert.Equal(2, frames);
        Assert.True(window.Closed);
    }

    [Fact]
    public void AWindowThatFailsIsThrownToTheCaller()
    {
        if (!TestApplication.HasDisplay)
            return;
        using var window = new CrashWindow(Program, Report(File), Logs);
        var thrown = Assert.Throws<InvalidOperationException>(() => window.Show(visible: false, eachFrame: _ => throw new InvalidOperationException("no window here")));
        Assert.Equal("no window here", thrown.Message);
        Assert.Null(window.Window);
        Assert.False(window.Closed);
    }

    [Fact]
    public void AMessageIsShownUntilClosedOrGivesFalseWhenItCantBe()
    {
        if (!TestApplication.HasDisplay)
            return;
        var frames = 0;
        string? title = null;
        Assert.True(MessageWindow.Show("Statventure Editor", "Statventure Editor can't start", "'--bad' isn't an option", visible: false, eachFrame: (message, window) =>
        {
            window.MinimizedForTests = true;
            window.RequestWakeUp(0);
            title ??= $"{window.Title}, {window.Width}x{window.Height}, visible {window.Native.Visible}";
            if (++frames == 2)
                message.Document.RunScript(() => message.Document.QuerySelector(".close")!.Click());
            else if (frames > 10)
                EndAnyway(window, message.Close);
        }));
        Assert.Equal(2, frames);
        Assert.Equal("Statventure Editor, 640x240, visible False", title);
        // A window that fails: false, logged, nothing thrown.
        var shown = true;
        var logged = TestApplication.CaptureLog("Application", () =>
            shown = MessageWindow.Show("Statventure Editor", "Heading", "Text", visible: false, eachFrame: (_, _) => throw new InvalidOperationException("no window here")));
        Assert.False(shown);
        Assert.StartsWith("Error: the message window couldn't be shown: System.InvalidOperationException: no window here", Assert.Single(logged));
    }
}
