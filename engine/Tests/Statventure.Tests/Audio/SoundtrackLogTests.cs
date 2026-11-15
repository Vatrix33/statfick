using Statventure.Audio;
using Statventure.Core;

namespace Statventure.Tests.Audio;

/// <summary>
/// The soundtrack's log lines (GM1-P21; Docs/Game/Foundation.md, "How it works", step 11): every theme a program starts is
/// in its log, "[Audio] theme &lt;id&gt; started" for <see cref="Soundtrack.Play"/> and "[Audio] theme &lt;id&gt; scheduled at
/// &lt;t&gt; s" for <see cref="Soundtrack.PlayAt"/>, so the end-to-end flow can read that the game started none (Q15). Only
/// the music an <see cref="AudioSystem"/> plays (its calls dispatched to the audio thread) logs them: the warm-up's
/// throwaway soundtrack, which plays three themes into a graph no one hears at every start, doesn't. A line only: nothing
/// of the music changes. Run alone: they read the log (<see cref="Log.Sink"/> is one for the process).
/// </summary>
[Collection("Soundtrack log")]
public class SoundtrackLogTests
{
    static List<string> AudioLog(Action action)
    {
        var lines = new List<string>();
        var (sink, level) = (Log.Sink, Log.Level);
        Log.Sink = (lineLevel, category, message) =>
        {
            sink?.Invoke(lineLevel, category, message);
            if (category == "Audio")
                lock (lines)
                    lines.Add($"{lineLevel} {message}");
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

    /// <summary>A soundtrack as an <see cref="AudioSystem"/> makes its music: its calls dispatched (here run at once).</summary>
    static Soundtrack Played(AudioGraph graph) => new(graph, graph.Destination, TestMusic.Library, dispatch: action => action());

    [Fact]
    public void EachThemeStartedOrScheduledIsLogged()
    {
        var graph = new AudioGraph(48000, 2);
        var music = Played(graph);
        var log = AudioLog(() =>
        {
            music.Play("alder-vale");
            music.PlayAt(1.5, "combat@alder-vale");
            music.PlayAt(12.25, "alder-vale");
            music.StopAt(20);
            music.Stop();
        });
        Assert.Equal(["Info theme alder-vale started", "Info theme combat@alder-vale scheduled at 1.5 s", "Info theme alder-vale scheduled at 12.25 s"], log);
        // The play itself as before: the theme playing (no audio thread here: applied at once).
        Assert.Null(music.Theme);
        AudioLog(() => music.Play("alder-vale"));
        Assert.Equal("alder-vale", music.Theme);
    }

    [Fact]
    public void AThemeTheLibraryHasntIsRefusedWithoutALine()
    {
        var graph = new AudioGraph(48000, 2);
        var music = Played(graph);
        var log = AudioLog(() =>
        {
            Assert.Throws<ArgumentException>(() => music.Play("nowhere"));
            Assert.Throws<ArgumentException>(() => music.PlayAt(1, "nowhere"));
            Assert.Throws<ArgumentOutOfRangeException>(() => music.PlayAt(double.NaN, "alder-vale"));
        });
        Assert.Empty(log);
        Assert.Null(music.Theme);
    }

    [Fact]
    public void TheMusicNoOneHearsIsntLogged()
    {
        // A soundtrack made apart (the warm-up's throwaway graph, an offline render): its plays aren't the program's.
        var graph = new AudioGraph(48000, 2);
        var apart = new Soundtrack(graph, graph.Destination, TestMusic.Library);
        var log = AudioLog(() =>
        {
            apart.Play("alder-vale");
            apart.PlayAt(1, "combat");
        });
        Assert.Empty(log);
        Assert.Equal("alder-vale", apart.Theme);
        // An audio system's warm-up, as at every start: three themes into a graph of its own, nothing logged; its music,
        // the one a player hears, logged.
        var system = new AudioSystem(new AudioGraph(48000, 2), TestMusic.Library);
        log = AudioLog(() =>
        {
            system.WarmUp();
            system.Music.Play("alder-vale");
        });
        Assert.Equal(["Info theme alder-vale started"], log);
    }
}

[CollectionDefinition("Soundtrack log", DisableParallelization = true)]
public sealed class SoundtrackLogCollection;
