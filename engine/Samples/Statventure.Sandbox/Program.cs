// The Statventure sandbox: a window showing engine features as they are built; until the port, the game's stand-in in
// the packages (M12-12), so it starts as the game will: the BRUTAL splash first (Application.Run, Q29), its logs as the
// game's, a crash shown in the crash window (Docs/Packaging.md, "Crashes"), the credits behind its About button (Q30).
//   dotnet run -- [--capture file.png] [--frames N] [--capture-splash file.png] [--about] [--crash | --crash-thread]
//                 [--log-visibility] [--cutscene jiruan]
// --capture saves the window at the N-th frame (30 by default) after the splash, the page's own frames counted (the
// splash's 2 s come first, as at every start), then quits; a run that crashes saves the crash window's first frame there
// instead and ends with the crash's exit code (Application.CrashCapture), so a capture run nobody watches always ends.
// --capture-splash saves the splash's first frame (with --capture, the run then ends by itself).
// --about clicks the About button at the start (its credits view). --crash throws in the first update after Load (a
// crash on the main thread); --crash-thread throws on a thread of its own then (another thread's crash, taken by the
// main loop): the end-to-end checks of the crash file and the crash window (M12 step 8).
// --cutscene plays Ji'ruan's intro, M10's proof (Cutscene.cs; Tools/Scripts/play-cutscene.sh); with --dump <folder>
// it draws the 7 s frame by frame on a clock of its own and writes each frame there (frame-NNN.png at 12 fps and
// still-<time>.png at the proof's sampled times); --volume 0-100 sets its master volume (100 by default; 0 to check
// it from a script without sound).
// --log-visibility logs the document's visibility, page and focus events and a 250 ms timer's
// firings (to see minimizing, restoring and closing the window: Docs/Scripting.md, "Hidden documents").
// From a script, give it a scratch per-user folder (XDG_DATA_HOME): it writes logs/ there.
using Statventure.Core;
using Statventure.Graphics;
using Statventure.Imaging;
using Statventure.Platform;
using Statventure.Text;
using Statventure.UI;
using Statventure.UI.Dom;

string? capture = null, captureSplash = null, crash = null;
var captureFrame = 30;
var logVisibility = false;
var about = false;
string? cutscene = null, dump = null;
var volume = 100.0;
for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--capture") capture = args[++i];
    else if (args[i] == "--frames") captureFrame = int.Parse(args[++i]);
    else if (args[i] == "--capture-splash") captureSplash = args[++i];
    else if (args[i] == "--about") about = true;
    else if (args[i] == "--crash") crash = "main";
    else if (args[i] == "--crash-thread") crash = "thread";
    else if (args[i] == "--log-visibility") logVisibility = true;
    else if (args[i] == "--cutscene") cutscene = args[++i];
    else if (args[i] == "--dump") dump = args[++i];
    else if (args[i] == "--volume") volume = double.Parse(args[++i], System.Globalization.CultureInfo.InvariantCulture);
}
if (cutscene is not null)
{
    using var player = new CutsceneSandbox(cutscene, capture, captureFrame) { DumpFolder = dump, Volume = volume / 100, CrashCapture = capture };
    player.Run();
    return;
}
using var sandbox = new Sandbox(capture, captureFrame, logVisibility, about, crash) { CrashCapture = capture };
if (captureSplash is not null)
    _ = sandbox.CaptureNextFrame(captureSplash);
sandbox.Run();

sealed class Sandbox(string? capture, int captureFrame, bool logVisibility, bool about, string? crash) : Application(new ApplicationOptions
{
    Window = new WindowOptions { Title = Sandbox.Title, Width = 960, Height = 600 },
    LogName = "stat-fick", // its logs as the game's, whose stand-in it is (Docs/Packaging.md, "Logs")
})
{
    /// <summary>The stand-in's name (Q13): its window's title, its credits' and its crash window's.</summary>
    public const string Title = "Statventure Sandbox";

    UiHost _host = null!;

    /// <summary>The credits view while it shows (the About button, Q30): the engine's view as a document of its own, in place of the page.</summary>
    Credits? _credits;
    UiHost? _about;
    bool _openAbout, _closeAbout;

    /// <summary>The credits view's own Close, at its top right in the credits' dark look (Escape closes it too).</summary>
    const string AboutCss = """
        .about-close { position: absolute; top: 12px; right: 28px; height: 24px; padding: 0 10px; border: 1px solid #3a3d38; border-radius: 3px; color: #eeeae1; background: #292c28; font: 13px/22px Lato; }
        .about-close:hover { border-color: #d77a50; }
        .about-close:active { background: #402a23; }
        """;

    const string Css = """
        @keyframes pulse { from { box-shadow: 0 0 0 0 rgba(122, 42, 29, 0.6); } to { box-shadow: 0 0 0 10px rgba(122, 42, 29, 0); } }
        html { background: #e9dcc0; }
        body { margin: 0; padding: 16px 24px; font: 15px Lato; color: #3a2a1a; }
        h1 { font: 700 26px "STIX Two Text"; margin: 0 0 4px; }
        p { margin: 4px 0 12px; max-width: 640px; }
        .row { display: flex; gap: 12px; align-items: center; margin: 10px 0; flex-wrap: wrap; }
        button { font: inherit; padding: 6px 14px; }
        .fancy { background: #f6ecd2; border: 1px solid #7a2a1d; border-radius: 6px; transition: background-color 200ms, transform 200ms; }
        .fancy:hover { background: #ead2a4; transform: translateY(-1px); }
        .fancy:active { background: #d9b97c; }
        .glow { animation: pulse 1.4s infinite; }
        .zone { width: 120px; height: 56px; border: 1px dashed #7a2a1d; display: grid; place-items: center; user-select: none; }
        .cross { cursor: crosshair; }
        .grab { cursor: url(sandbox:grab) 8 8, grabbing; }
        .list { width: 220px; height: 120px; overflow: auto; border: 1px solid #a08060; background: #fbf5e6; }
        .list div { padding: 3px 8px; }
        .list div:nth-child(odd) { background: #f1e6cc; }
        textarea { font: inherit; width: 260px; height: 70px; }
        details { max-width: 400px; }
        .about { position: absolute; top: 16px; right: 24px; }
        """;

    const string Html = """
        <button class="fancy about" title="The program's credits">About</button>
        <h1>Statventure UI</h1>
        <p>The engine's UI as Chromium 152 runs it: select this text, tab through the controls, hover for tooltips, scroll the list, resize the text box by its corner.</p>
        <div class="row">
          <button class="fancy glow" title="A button with a tooltip">Hover me</button>
          <button disabled title="Disabled buttons show not-allowed">Disabled</button>
          <div class="zone cross" title="crosshair">Crosshair</div>
          <div class="zone grab" title="An image cursor">Image cursor</div>
        </div>
        <div class="row">
          <input type="text" placeholder="Your name" required title="">
          <input type="number" min="1" max="10" value="3">
          <select title="Pick a class">
            <option title="Swords and shields">Warrior</option>
            <option>Mage</option>
            <optgroup label="Sneaky"><option>Rogue</option><option disabled>Assassin</option></optgroup>
          </select>
          <label><input type="checkbox" checked> Music</label>
          <label><input type="radio" name="d" checked> Easy</label>
          <label><input type="radio" name="d"> Hard</label>
          <input type="range" min="0" max="100" value="40">
        </div>
        <div class="row">
          <textarea>Type here.
        A second line.</textarea>
          <div class="list" tabindex="0">
            <div>Row 1</div><div>Row 2</div><div>Row 3</div><div>Row 4</div><div>Row 5</div><div>Row 6</div>
            <div>Row 7</div><div>Row 8</div><div>Row 9</div><div>Row 10</div><div>Row 11</div><div>Row 12</div>
          </div>
        </div>
        <details><summary>More</summary><p>Details open and close with a click, Enter or Space.</p></details>
        """;

    protected override void Load()
    {
        var fonts = new FontLibrary();
        var dir = Path.Combine(AppContext.BaseDirectory, "Data", "Fonts");
        fonts.AddFile(Path.Combine(dir, "Lato-Regular.ttf"), "Lato", 400);
        fonts.AddFile(Path.Combine(dir, "Lato-Semibold.ttf"), "Lato", 500, weightMax: 600);
        fonts.AddFile(Path.Combine(dir, "Lato-Bold.ttf"), "Lato", 700, weightMax: 900);
        fonts.AddFile(Path.Combine(dir, "STIXTwoText-Regular.otf"), "STIX Two Text", 400);
        fonts.AddFile(Path.Combine(dir, "STIXTwoText-Italic.otf"), "STIX Two Text", 400, italic: true);
        var document = new UiDocument(fonts);
        document.AddStylesheet(Css, "sandbox");
        Markup.SetInnerHtml(document.Body, Html);
        _host = new UiHost(Window, Gpu, document) { LoadImage = url => url == "sandbox:grab" ? GrabCursor() : null, Audio = Audio };
        var aboutButton = document.QuerySelector(".about")!;
        aboutButton.AddEventListener("click", _ => _openAbout = true);
        // --about clicks the page's About button at the start (the checks: the button opens the view).
        if (about)
            document.RunScript(aboutButton.Click);
        if (logVisibility)
        {
            foreach (var type in new[] { "visibilitychange", "pagehide", "unload", "focus", "blur" })
                document.AddEventListener(type, e => Log.Info("Sandbox", $"{document.PerformanceNow:F0} ms: {e.Type} ({document.VisibilityState})"), capture: true);
            document.RunScript(() => document.SetInterval(() => Log.Info("Sandbox", $"{document.PerformanceNow:F0} ms: timer ({document.VisibilityState})"), 250));
        }
    }

    /// <summary>A 16 px ring with a dot, drawn here (the game's cursor images come with its assets).</summary>
    static RgbaImage GrabCursor()
    {
        var image = new RgbaImage(16, 16);
        for (var y = 0; y < 16; y++)
            for (var x = 0; x < 16; x++)
            {
                var d = MathF.Sqrt((x - 7.5f) * (x - 7.5f) + (y - 7.5f) * (y - 7.5f));
                var ring = Math.Clamp(1.5f - MathF.Abs(d - 6), 0, 1);
                var dot = Math.Clamp(2.5f - d, 0, 1);
                var alpha = MathF.Max(ring, dot);
                var i = (y * 16 + x) * 4;
                (image.Pixels[i], image.Pixels[i + 1], image.Pixels[i + 2], image.Pixels[i + 3]) = ((byte)122, (byte)42, (byte)29, (byte)(alpha * 255));
            }
        return image;
    }

    protected override void Update(in GameTime time)
    {
        if (crash is not null && time.Frame == 0)
            CrashOnPurpose(crash);
        if (Input.WasPressed(Key.F11))
            Window.Fullscreen = !Window.Fullscreen;
        if (_about is not null && Input.WasPressed(Key.Escape))
            _closeAbout = true;
        // Opened and closed between updates, never while the document that asked is dispatching.
        if (_closeAbout)
            CloseAbout();
        if (_openAbout)
            OpenAbout();
        (_about ?? _host).Update(time.Total);
        if (capture is not null && time.Frame == captureFrame)
        {
            Log.Info("Sandbox", $"window {Window.Width}x{Window.Height}, framebuffer {Window.FramebufferWidth}x{Window.FramebufferHeight}, scale {Window.ContentScale:0.###}, swapchain {Swapchain.Width}x{Swapchain.Height}");
            CaptureNextFrame(capture).ContinueWith(_ => Quit());
        }
    }

    /// <summary>The stand-in's own crash, for the end-to-end checks (--crash, --crash-thread): on the main thread, or on a thread of its own.</summary>
    static void CrashOnPurpose(string where)
    {
        if (where == "main")
            throw new InvalidOperationException("the sandbox crashed on purpose (--crash)");
        new Thread(() => throw new InvalidOperationException("the sandbox crashed on purpose on a thread of its own (--crash-thread)"))
        {
            Name = "Sandbox crash",
            IsBackground = true,
        }.Start();
    }

    /// <summary>The credits view (Q30): the engine's <see cref="Credits"/> for the stand-in and its version, in place of the page, with a Close of its own.</summary>
    void OpenAbout()
    {
        _openAbout = false;
        if (_about is not null)
            return;
        _credits = new Credits(Title, ProgramInfo.Of(typeof(Sandbox).Assembly));
        _credits.Document.AddStylesheet(AboutCss, "sandbox-about");
        _credits.Document.Body.Append("button", "about-close", "Close").AddEventListener("click", _ => _closeAbout = true);
        _about = new UiHost(Window, Gpu, _credits.Document) { Audio = Audio };
        Log.Info("Sandbox", "the credits view opened");
    }

    /// <summary>Back to the page.</summary>
    void CloseAbout()
    {
        _closeAbout = false;
        if (_about is null)
            return;
        Gpu.WaitIdle();
        _about.Dispose();
        _credits!.Dispose();
        (_about, _credits) = (null, null);
        Log.Info("Sandbox", "the credits view closed");
    }

    protected override void Render(Frame frame, GpuImage target, in GameTime time) => (_about ?? _host).Render(frame, target);

    protected override void Unload()
    {
        CloseAbout();
        _host.Dispose();
    }
}
