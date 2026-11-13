using Statventure.Core;
using Statventure.Game.Mods;
using Statventure.Game.Settings;
using Statventure.Platform;

namespace Statventure.Game.Tests;

/// <summary>A folder of the test's own under the system's temporary folder, deleted afterwards (never the player's).</summary>
sealed class ScratchFolder : IDisposable
{
    public ScratchFolder() => Root = Directory.CreateTempSubdirectory("statventure-game-").FullName;

    public string Root { get; }

    public string this[string relative] => Path.Combine(Root, relative);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}

/// <summary>Tests that read the log or open a window run alone: <see cref="Log.Sink"/> is one for the process, and so is GLFW.</summary>
[CollectionDefinition("Game log", DisableParallelization = true)]
public sealed class GameLogCollection;

/// <summary>The program's test helpers (GM1 step 2).</summary>
static class GameTesting
{
    /// <summary>
    /// A game as the program makes it (<see cref="StatFickGame.OptionsFor"/>, the mods' options), for a test: its per-user
    /// folder <paramref name="folder"/>, no log (so no splash), no sound, its window hidden.
    /// </summary>
    public static StatFickGame Game(string folder, GameOptions? options = null, IReadOnlyList<IGameScreen>? screens = null, Action<GameSettings>? settingsLoaded = null)
    {
        var snapshot = GameSettings.Peek(folder);
        var engine = StatFickGame.OptionsFor(snapshot, folder);
        engine = engine with { LogName = null, Audio = false, Window = engine.Window with { Visible = false } };
        var mods = new GameMods();
        return new StatFickGame(mods.ApplyTo(engine, snapshot), options ?? new GameOptions(), snapshot, mods, screens)
        {
            SettingsLoaded = settingsLoaded,
        };
    }

    /// <summary>The log lines written while <paramref name="action"/> runs, as "Level [Category] message".</summary>
    public static List<string> CaptureLog(Action action)
    {
        var lines = new List<string>();
        var (sink, level) = (Log.Sink, Log.Level);
        Log.Sink = (lineLevel, category, message) =>
        {
            sink?.Invoke(lineLevel, category, message);
            lock (lines)
                lines.Add($"{lineLevel} [{category}] {message}");
        };
        Log.Level = LogLevel.Info;
        try
        {
            action();
        }
        finally
        {
            (Log.Sink, Log.Level) = (sink, level);
        }
        lock (lines)
            return [.. lines];
    }
}

/// <summary>A window of the test's own: its state set by the test, what the game did to it recorded.</summary>
sealed class FakeWindow : IGameWindow
{
    public int Width { get; set; } = StatFickGame.FirstWidth;
    public int Height { get; set; } = StatFickGame.FirstHeight;
    public bool Maximized { get; set; }
    public bool Minimized { get; set; }
    public bool Fullscreen { get; set; }
    public List<string> Done { get; } = [];

    public void Maximize()
    {
        Maximized = true;
        Done.Add("maximize");
    }

    public void Restore()
    {
        Maximized = false;
        Done.Add("restore");
    }

    public string BringForward()
    {
        Done.Add("forward");
        return "brought forward (the test's window)";
    }
}

/// <summary>The volumes the game gave, each time.</summary>
sealed class FakeVolumes : IGameVolumes
{
    public List<(double Master, double Music, double Effects)> Applied { get; } = [];

    public void Apply(double master, double music, double effects) => Applied.Add((master, music, effects));
}

/// <summary>A screen of the test's own: records its attaching (and the parts it got), may register commands that record their runs.</summary>
sealed class RecordingScreen(string name, List<string> record, params string[] commands) : IGameScreen
{
    public GameParts? Parts { get; private set; }

    public void Attach(GameParts parts)
    {
        Parts = parts;
        record.Add($"attach {name}");
        foreach (var command in commands)
            parts.Ui.Commands.Register(command, argument => record.Add($"run {command}{(argument is null ? "" : " " + argument)}"));
    }
}
