namespace Statventure.Game;

/// <summary>Which crash the game makes on purpose for the checks (<c>--crash</c>, <c>--crash-thread</c>).</summary>
public enum CrashOnPurpose
{
    /// <summary>None: the game runs.</summary>
    None,
    /// <summary><c>--crash</c>: thrown in the first update after <c>Load</c>, a crash on the main thread.</summary>
    MainThread,
    /// <summary><c>--crash-thread</c>: thrown on a thread of its own then, another thread's crash taken by the main loop.</summary>
    OtherThread,
}

/// <summary>
/// The game's command line (GM1-P12; Docs/Game/Foundation.md, "The program, the library and the repo"), for scripts and
/// checks only, not in a usage text (players start the game without arguments):
/// <c>--capture &lt;png&gt; [--frames N]</c> saves the window at the N-th frame after the splash (30 by default) and quits (a
/// run that crashes saves the crash window's first frame there instead); <c>--capture-splash &lt;png&gt;</c> saves the
/// splash's first frame; <c>--about</c> runs the command <c>about</c>; <c>--open &lt;screen&gt;</c> runs the command that opens
/// that screen (<see cref="OpenCommand"/>); <c>--crash</c> and <c>--crash-thread</c> crash on purpose. Nothing here stops a
/// start: a wrong argument is a problem logged once the run's log is open, and the run goes on without it.
/// </summary>
public sealed record GameOptions
{
    /// <summary>The frame <c>--capture</c> saves when <c>--frames</c> doesn't say (the sandbox's and the editor's).</summary>
    public const int DefaultFrames = 30;

    /// <summary><c>--capture</c>: the PNG file (a full path), or null.</summary>
    public string? Capture { get; init; }
    /// <summary><c>--frames</c>: the frame of the loop (its own, counted after the splash) <see cref="Capture"/> saves.</summary>
    public int Frames { get; init; } = DefaultFrames;
    /// <summary><c>--capture-splash</c>: the PNG file the splash's first frame goes to (a full path), or null.</summary>
    public string? CaptureSplash { get; init; }
    /// <summary><c>--about</c>: the command <c>about</c> run at the start (the engine's credits view until step 9).</summary>
    public bool About { get; init; }
    /// <summary><c>--open</c>: the screen named, or null.</summary>
    public string? Open { get; init; }
    /// <summary><c>--crash</c>, <c>--crash-thread</c>.</summary>
    public CrashOnPurpose Crash { get; init; }
    /// <summary>What was wrong with the command line, each ignored (logged at the start).</summary>
    public IReadOnlyList<string> Problems { get; init; } = [];

    /// <summary>
    /// The command <c>--open &lt;screen&gt;</c> runs: <c>start</c> the start screen's <c>start-screen</c>, any other screen
    /// <c>open-&lt;screen&gt;</c> (<c>settings</c> <c>open-settings</c>, <c>slots</c> <c>open-slots</c>, <c>menu</c>
    /// <c>open-menu</c>: "The seams", the commands); null without <c>--open</c>.
    /// </summary>
    public string? OpenCommand => Open switch
    {
        null => null,
        "start" => "start-screen",
        var screen => "open-" + screen,
    };

    /// <summary>The options <paramref name="args"/> give; paths made full from the current folder.</summary>
    public static GameOptions Parse(IReadOnlyList<string> args)
    {
        var options = new GameOptions();
        var problems = new List<string>();
        string? Value(ref int i, string option)
        {
            if (i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal))
                return args[++i];
            problems.Add($"{option} needs a value");
            return null;
        }
        for (var i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--capture":
                    if (Value(ref i, "--capture") is { } capture)
                        options = options with { Capture = Path.GetFullPath(capture) };
                    break;
                case "--frames":
                    if (Value(ref i, "--frames") is { } frames)
                    {
                        if (frames.Length is > 0 and < 10 && frames.All(char.IsAsciiDigit) && int.Parse(frames) >= 1)
                            options = options with { Frames = int.Parse(frames) };
                        else
                            problems.Add($"--frames needs a whole number from 1, not '{frames}'");
                    }
                    break;
                case "--capture-splash":
                    if (Value(ref i, "--capture-splash") is { } splash)
                        options = options with { CaptureSplash = Path.GetFullPath(splash) };
                    break;
                case "--about":
                    options = options with { About = true };
                    break;
                case "--open":
                    if (Value(ref i, "--open") is { } screen)
                        options = options with { Open = screen };
                    break;
                case "--crash":
                    options = options with { Crash = CrashOnPurpose.MainThread };
                    break;
                case "--crash-thread":
                    options = options with { Crash = CrashOnPurpose.OtherThread };
                    break;
                default:
                    problems.Add($"'{args[i]}' isn't an option");
                    break;
            }
        }
        return options with { Problems = problems };
    }
}
