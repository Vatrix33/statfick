// Times the audio engine as the game runs it (Docs/Audio.md, step 7): each theme played through the
// AudioSystem's mixer at 48 kHz, its steps scheduled 0.2 s ahead before each quantum, for one pass of
// its form plus 4.5 s; every quantum's render time against the real-time budget (128 frames:
// 2.67 ms). Arguments name themes (all by default).
using System.Diagnostics;
using Statventure.Audio;

const int Rate = 48000;
var budget = AudioGraph.RenderQuantumFrames * 1000.0 / Rate;
if (args.Contains("--live"))
{
    // Plays through the real default device, silently (the master bus at 0), and counts underruns.
    var rest = args.Where(a => a != "--live").ToArray();
    var seconds = rest.Length > 1 ? double.Parse(rest[1], System.Globalization.CultureInfo.InvariantCulture) : 20;
    using var live = new AudioSystem();
    live.Post(() => live.MasterGain.SetValueAtTime(0, live.Graph.CurrentTime));
    Thread.Sleep(1500); // the rooms and the warm-up
    live.Music.Play(rest[0]);
    var started = live.Graph.CurrentTime;
    var clock = Stopwatch.StartNew();
    // Switch themes every 4 s (two performances cross-fade each time).
    var others = rest.Skip(2).ToArray();
    for (var i = 0; clock.Elapsed.TotalSeconds < seconds; i++)
    {
        Thread.Sleep(TimeSpan.FromSeconds(Math.Min(4, Math.Max(0, seconds - clock.Elapsed.TotalSeconds))));
        if (others.Length > 0 && clock.Elapsed.TotalSeconds < seconds)
        {
            Console.WriteLine($"  {clock.Elapsed.TotalSeconds:F3} s: playing {others[i % others.Length]}");
            live.Music.Play(others[i % others.Length]);
        }
    }
    var times = live.RecentQuantumMilliseconds().Order().ToArray();
    Console.WriteLine($"{rest[0]} live for {clock.Elapsed.TotalSeconds:F1} s on {(live.HasDevice ? "a device" : "no device")} at {live.SampleRate} Hz: " +
        $"graph advanced {live.Graph.CurrentTime - started:F2} s, underruns {live.Underruns}, last 4096 quanta max {times[^1]:F2} ms, p99 {times[(int)(0.99 * times.Length)]:F2} ms");
    return;
}
if (args.Contains("--start"))
{
    var warm = args.Contains("--warm");
    args = [.. args.Where(a => a != "--warm")];
    if (warm)
    {
        var warmer = new AudioSystem(new AudioGraph(Rate, 2));
        warmer.WarmUp();
    }
    // What starting a theme costs: the room's impulse response, its convolver, the first steps.
    foreach (var id in args.Where(a => a != "--start"))
        for (var run = 0; run < 3; run++)
        {
            var graph = new AudioGraph(Rate, 2);
            var system = new AudioSystem(graph);
            system.RoomsReady.Wait();
            var t0 = Stopwatch.GetTimestamp();
            system.Music.Play(id);
            system.BeforeQuantum();
            var t1 = Stopwatch.GetTimestamp();
            var convolver = new ConvolverNode(graph);
            var room = new AudioBuffer(2, (int)(4.8 * Rate), Rate);
            var t2 = Stopwatch.GetTimestamp();
            convolver.Buffer = room;
            var t3 = Stopwatch.GetTimestamp();
            double Ms(long a, long b) => (b - a) * 1000.0 / Stopwatch.Frequency;
            Console.WriteLine($"{id} run {run}: Play + first schedule {Ms(t0, t1):F2} ms (rooms prepared); a 4.8 s stereo convolver set up on the spot {Ms(t2, t3):F2} ms");
        }
    return;
}
var split = args.Contains("--split");
args = [.. args.Where(a => a != "--split")];
var themes = args.Length > 0 ? args : [.. Soundtrack.Themes];
// Warm up the JIT on one theme.
Measure(themes[0], 2);
Console.WriteLine($"{"theme",-30} {"seconds",8} {"mean ms",8} {"p99 ms",8} {"p99.9",8} {"max ms",8} {"over budget",12} {"GCs 0/1/2",12}  (budget {budget:F2} ms a quantum)");
var all = new List<double>();
foreach (var id in themes)
{
    int g0 = GC.CollectionCount(0), g1 = GC.CollectionCount(1), g2 = GC.CollectionCount(2);
    var (seconds, times) = Measure(id, double.PositiveInfinity, split);
    all.AddRange(times);
    Report(id, seconds, times, $"{GC.CollectionCount(0) - g0}/{GC.CollectionCount(1) - g1}/{GC.CollectionCount(2) - g2}");
}
Report("all", all.Count * AudioGraph.RenderQuantumFrames / (double)Rate, all, "");

void Report(string name, double seconds, List<double> times, string gcs)
{
    var sorted = times.Order().ToArray();
    double P(double q) => sorted[Math.Min(sorted.Length - 1, (int)(q * sorted.Length))];
    var over = times.Count(t => t > budget);
    Console.WriteLine($"{name,-30} {seconds,8:F1} {times.Average(),8:F3} {P(0.99),8:F3} {P(0.999),8:F3} {sorted[^1],8:F3} {over,12} {gcs,12}");
}

(double Seconds, List<double> Times) Measure(string id, double limitSeconds, bool split = false)
{
    var system = new AudioSystem(new AudioGraph(Rate, 2));
    system.RoomsReady.Wait();
    system.Music.Play(id);
    var seconds = Math.Min(limitSeconds, Soundtrack.OfflineLength(id, Rate) / (double)Rate);
    var quanta = (int)(seconds * Rate / AudioGraph.RenderQuantumFrames);
    var bus = new AudioBus(2, AudioGraph.RenderQuantumFrames);
    var times = new List<double>(quanta);
    double worstSchedule = 0, worstRender = 0;
    int scheduleOver = 0, renderOver = 0;
    for (var q = 0; q < quanta; q++)
    {
        var begin = Stopwatch.GetTimestamp();
        system.BeforeQuantum();
        var middle = Stopwatch.GetTimestamp();
        system.Graph.RenderQuantum(bus);
        var end = Stopwatch.GetTimestamp();
        double schedule = (middle - begin) * 1000.0 / Stopwatch.Frequency, render = (end - middle) * 1000.0 / Stopwatch.Frequency;
        worstSchedule = Math.Max(worstSchedule, schedule);
        worstRender = Math.Max(worstRender, render);
        if (schedule > budget / 2) scheduleOver++;
        if (render > budget / 2) renderOver++;
        times.Add(schedule + render);
    }
    if (split)
        Console.WriteLine($"  {id}: scheduling worst {worstSchedule:F2} ms ({scheduleOver} over half the budget), rendering worst {worstRender:F2} ms ({renderOver} over half)");
    return (seconds, times);
}
