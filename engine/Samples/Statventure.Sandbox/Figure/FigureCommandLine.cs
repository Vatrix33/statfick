// The figure mode's command line (GM2-P14; GM2 step 3a; Program.cs, Tools/Scripts/play-figure.sh):
//   --figure <body.glb> [--clips <glb>] [--look <name or file>] [--garment <glb>]... [--item <glb>@<socket>]...
//   [--view front|three-quarter|side|back|close-up|<degrees>] [--size WxH] [--elevation degrees] [--margin fraction]
//   [--extent metres] [--focus <bone>] [--perspective degrees] [--dump <folder> [--fps N] [--times t,...]
//   [--scene-file <file>]] [--capture <file.png> [--frames N]]
// A look by name is <looks>/<name>.json (the sandbox's Data/Looks: the files look.py writes), today's by default.
using System.Globalization;

namespace Statventure.Sandbox.Figure;

public static class FigureCommandLine
{
    /// <summary>The figure and how to run it; an <see cref="ArgumentException"/> names what is wrong.</summary>
    public static (FigureSpec Spec, FigureRun Run) Parse(IReadOnlyList<string> args, string looks, string? fonts)
    {
        string? body = null, clips = null, look = null, view = null, dump = null, sceneFile = null, capture = null, focus = null;
        var garments = new List<string>();
        var items = new List<(string, string)>();
        int width = 600, height = 800, frame = 30;
        float elevation = 5, margin = 0.06f;
        float? extent = null, perspective = null;
        double fps = 30;
        List<double>? times = null;
        static float F(string text) => float.Parse(text, CultureInfo.InvariantCulture);
        for (var i = 0; i < args.Count; i++)
        {
            var option = args[i];
            string Next() => i + 1 < args.Count ? args[++i] : throw new ArgumentException($"{option} needs a value");
            switch (option)
            {
                case "--figure": body = Next(); break;
                case "--clips": clips = Next(); break;
                case "--look": look = Next(); break;
                case "--garment": garments.Add(Next()); break;
                case "--item":
                    var item = Next();
                    var at = item.LastIndexOf('@');
                    if (at <= 0 || at == item.Length - 1)
                        throw new ArgumentException($"--item {item}: <glb>@<socket>");
                    items.Add((item[..at], item[(at + 1)..]));
                    break;
                case "--view": view = Next(); break;
                case "--size":
                    var size = Next().Split('x');
                    if (size.Length != 2 || !int.TryParse(size[0], CultureInfo.InvariantCulture, out width) || !int.TryParse(size[1], CultureInfo.InvariantCulture, out height)
                        || width <= 0 || height <= 0)
                        throw new ArgumentException("--size <width>x<height>");
                    break;
                case "--elevation": elevation = F(Next()); break;
                case "--margin": margin = F(Next()); break;
                case "--extent": extent = F(Next()); break;
                case "--focus": focus = Next(); break;
                case "--perspective": perspective = F(Next()); break;
                case "--dump": dump = Next(); break;
                case "--fps": fps = double.Parse(Next(), CultureInfo.InvariantCulture); break;
                case "--times": times = Next().Split(',').Select(t => double.Parse(t, CultureInfo.InvariantCulture)).ToList(); break;
                case "--scene-file": sceneFile = Next(); break;
                case "--capture": capture = Next(); break;
                case "--frames": frame = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                default: throw new ArgumentException($"the figure mode has no option {option}");
            }
        }
        if (body is null || !body.EndsWith(".glb", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("--figure takes a body .glb (a look.hero's figure is built from its parts once the figure builder exists: GM2 step 5)");
        var lookFile = look is null ? Path.Combine(looks, "today.json") : File.Exists(look) ? look : Path.Combine(looks, look + ".json");
        var spec = new FigureSpec
        {
            Body = body, Clips = clips, Look = FigureLook.Load(lookFile), Garments = garments, Items = items, View = view ?? "three-quarter",
            Width = width, Height = height, Elevation = elevation, Margin = margin, Extent = extent, Focus = focus, Perspective = perspective,
        };
        FigureFraming.TurnOf(spec.View);
        return (spec, new FigureRun { Dump = dump, Fps = fps, Times = times, SceneFile = sceneFile, Capture = capture, CaptureFrame = frame, Fonts = fonts });
    }
}
