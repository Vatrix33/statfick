using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using Statventure.Tests.Infrastructure;
using Statventure.Tests.Ui;
using Statventure.UI.Dom;
using Statventure.UI.Layout;
using CanvasRenderer = Statventure.Canvas.Canvas;

// Times the UI engine on the game's captured screens: a cold build, whole-document restyles and
// relayouts, typical single changes, and painting a frame. Usage: UiBenchmark [state ...] (default:
// sheet combat). Each line: mean time per run, memory allocated per run, styles computed and boxes
// laid out per run.

var capture = LoadCapture();
var names = args.Length > 0 ? args : ["sheet", "combat"];
using var gpu = new GpuFixture();
foreach (var name in names)
{
    var state = capture.GetProperty("states").EnumerateArray().Single(s => s.GetProperty("name").GetString() == name);
    Console.WriteLine($"{name}:");
    Cold(state);
    var document = GameLayoutTests.Build(state);
    Console.WriteLine($"  {document.AllElements().Count()} elements, {document.AllElements().Count(e => e.Style is not null)} styled, {document.Layout.Viewport.Descendants().Count()} boxes");
    var button = document.QuerySelectorAll("button").First(b => b.Box is not null);
    var block = document.Body.Descendants().Where(e => e.Box is { Type: BoxType.Block } && e.Children.Count > 0).ElementAt(40);
    var text = document.Body.Descendants().Where(e => e.Box is not null).SelectMany(e => e.Children.OfType<UiText>()).Where(t => t.Text.Trim().Length > 3).ElementAt(20);
    var original = text.Text;
    Run(document, "restyle everything", i => document.InvalidateStyle(document.Root, subtree: true));
    Run(document, "relayout everything", i => document.InvalidateLayout());
    Run(document, "attribute no rule uses", i => button.SetAttribute("data-benchmark", i.ToString()));
    Run(document, "class toggle", i =>
    {
        if (i % 2 == 0)
            button.AddClass("is-selected");
        else
            button.RemoveClass("is-selected");
    });
    Run(document, "paint-only style", i => block.SetAttribute("style", i % 2 == 0 ? "color: rgb(200, 10, 10)" : "color: rgb(10, 10, 200)"));
    Run(document, "text edit", i => text.Text = i % 2 == 0 ? original + " more" : original);
    Run(document, "padding change", i => block.SetAttribute("style", i % 2 == 0 ? "padding-left: 3px" : "padding-left: 4px"));
    Paint(gpu, document);
}

static JsonElement LoadCapture()
{
    var path = Path.Combine(Path.GetDirectoryName(Golden.GoldenPath("x"))!, "game-ui.json.gz");
    using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
    return JsonDocument.Parse(stream).RootElement.Clone();
}

static void Report(string label, int runs, double ms, long bytes, long styles, long layouts) =>
    Console.WriteLine($"  {label,-24} {ms / runs,8:0.000} ms {bytes / (double)runs / 1048576,8:0.00} MB {styles / (double)runs,8:0} styles {layouts / (double)runs,8:0} layouts");

static void Cold(JsonElement state)
{
    for (var i = 0; i < 2; i++)
        GameLayoutTests.Build(state);
    const int runs = 5;
    double parse = 0, update = 0;
    long bytes = 0;
    for (var i = 0; i < runs; i++)
    {
        var document = GameLayoutTests.Build(state, buildTree: false);
        var watch = Stopwatch.StartNew();
        Markup.SetInnerHtml(document.Body, state.GetProperty("markup").GetString()!);
        parse += watch.Elapsed.TotalMilliseconds;
        var before = GC.GetAllocatedBytesForCurrentThread();
        watch.Restart();
        document.Update();
        update += watch.Elapsed.TotalMilliseconds;
        bytes += GC.GetAllocatedBytesForCurrentThread() - before;
    }
    Console.WriteLine($"  {"parse markup",-24} {parse / runs,8:0.000} ms");
    Report("first update (cold)", runs, update, bytes, 0, 0);
}

static void Run(UiDocument document, string label, Action<int> change)
{
    for (var i = 0; i < 5; i++)
    {
        change(i);
        document.Update();
    }
    const int runs = 50;
    var styles = document.Styles.StylesComputed;
    var layouts = document.Layout.BoxesLaidOut;
    var before = GC.GetAllocatedBytesForCurrentThread();
    var watch = Stopwatch.StartNew();
    for (var i = 0; i < runs; i++)
    {
        change(i);
        document.Update();
    }
    Report(label, runs, watch.Elapsed.TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - before, document.Styles.StylesComputed - styles, document.Layout.BoxesLaidOut - layouts);
}

static void Paint(GpuFixture gpu, UiDocument document)
{
    var width = (int)document.ViewportSize.X;
    var height = (int)document.ViewportSize.Y;
    using var painter = new Statventure.UI.Paint.UiPainter(document);
    using var canvas = new CanvasRenderer(gpu.Device);
    using var target = Statventure.Graphics.GpuImage.CreateRenderTarget(gpu.Device, width, height);
    void Frame()
    {
        var frame = gpu.Device.BeginFrame();
        canvas.Begin(width, height);
        painter.Paint(canvas);
        canvas.Render(frame, target);
        gpu.Device.EndFrame(frame);
    }
    for (var i = 0; i < 20; i++)
        Frame();
    gpu.Device.WaitIdle();
    const int runs = 100;
    var before = GC.GetAllocatedBytesForCurrentThread();
    var watch = Stopwatch.StartNew();
    for (var i = 0; i < runs; i++)
        Frame();
    gpu.Device.WaitIdle();
    Report("paint and render a frame", runs, watch.Elapsed.TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - before, 0, 0);
}
