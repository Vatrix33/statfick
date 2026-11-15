using System.Diagnostics;
using System.Net.Sockets;

namespace Statventure.Game.Tests;

/// <summary>
/// One Stat Fick per per-user folder (GM1-P14): the lock, the channel, a later start asking the first forward. A later
/// start that should be answered has the program's own time (<see cref="SingleInstance.DefaultAnswerTime"/>: it comes
/// back as soon as the first answers); <see cref="Short"/> is for one that isn't, or that finds no first at all.
/// </summary>
public class SingleInstanceTests
{
    internal static readonly TimeSpan Short = TimeSpan.FromMilliseconds(400);

    [Fact]
    public void TheFirstStartHoldsItsFolderAndALaterOneAsksItForward()
    {
        using var scratch = new ScratchFolder();
        var folder = scratch["player/stat-fick"];
        using var first = SingleInstance.Claim(folder);
        var asked = 0;
        first.ForwardAsked += () => Interlocked.Increment(ref asked);
        Assert.Equal((SingleInstanceOutcome.First, null), (first.Outcome, first.Problem));
        Assert.True(File.Exists(Path.Combine(folder, SingleInstance.LockName)));
        using (var later = SingleInstance.Claim(folder))
            Assert.Equal(SingleInstanceOutcome.Forwarded, later.Outcome);
        // The first raised it before it answered.
        Assert.Equal(1, Volatile.Read(ref asked));
        using (var again = SingleInstance.Claim(folder))
            Assert.Equal(SingleInstanceOutcome.Forwarded, again.Outcome);
        Assert.Equal(2, Volatile.Read(ref asked));
    }

    [Fact]
    public void FoldersOfTheirOwnNeverBlockEachOther()
    {
        using var scratch = new ScratchFolder();
        using var a = SingleInstance.Claim(scratch["a"]);
        using var b = SingleInstance.Claim(scratch["b"]);
        Assert.Equal((SingleInstanceOutcome.First, SingleInstanceOutcome.First), (a.Outcome, b.Outcome));
        Assert.NotEqual(SingleInstance.ChannelName(a.Folder), SingleInstance.ChannelName(b.Folder));
        Assert.Matches("^stat-fick-[0-9a-f]{16}$", SingleInstance.ChannelName(a.Folder));
    }

    [Fact]
    public void AStartAfterTheFirstEndedIsTheFirst()
    {
        using var scratch = new ScratchFolder();
        SingleInstance.Claim(scratch["p"]).Dispose();
        using var next = SingleInstance.Claim(scratch["p"], Short);
        Assert.Equal(SingleInstanceOutcome.First, next.Outcome);
    }

    [Fact]
    public void TheSameFolderReachedThroughALinkIsTheSame()
    {
        if (OperatingSystem.IsWindows())
            return;
        using var scratch = new ScratchFolder();
        Directory.CreateDirectory(scratch["real/stat-fick"]);
        Directory.CreateSymbolicLink(scratch["home"], scratch["real"]);
        using var first = SingleInstance.Claim(scratch["real/stat-fick"]);
        // The program's own time for the answer, as every later start here that should be answered has: it comes back
        // as soon as the first answers (2026-11-15: 400 ms, which a busy machine's test run passed).
        using var later = SingleInstance.Claim(scratch["home/stat-fick"]);
        Assert.Equal((SingleInstanceOutcome.First, SingleInstanceOutcome.Forwarded), (first.Outcome, later.Outcome));
        Assert.Equal(first.Folder, later.Folder);
        Assert.Equal(SingleInstance.RealPath(scratch["real/stat-fick"]), SingleInstance.RealPath(scratch["home/stat-fick"]));
        // A link to a link, and one given relative to where it is.
        Directory.CreateSymbolicLink(scratch["again"], "home");
        Assert.Equal(first.Folder, SingleInstance.RealPath(scratch["again/stat-fick"]));
    }

    /// <summary>
    /// A later start on <paramref name="folder"/> (given <paramref name="answerTime"/>, the program's own when null) on a
    /// thread of its own: its outcome and how long it took; a start that isn't back within 30 s fails the test rather than
    /// hangs it.
    /// </summary>
    internal static (SingleInstanceOutcome Outcome, TimeSpan Took) LaterStart(string folder, TimeSpan? answerTime = null)
    {
        var outcome = (SingleInstanceOutcome?)null;
        var clock = Stopwatch.StartNew();
        var later = new Thread(() =>
        {
            using var start = SingleInstance.Claim(folder, answerTime);
            outcome = start.Outcome;
        }) { IsBackground = true, Name = "A later start" };
        later.Start();
        Assert.True(later.Join(TimeSpan.FromSeconds(30)), "the later start still waits");
        return (outcome!.Value, clock.Elapsed);
    }

    /// <summary>A first on <paramref name="folder"/> that holds the lock and listens (with <paramref name="backlog"/>) but takes no call and answers nothing.</summary>
    internal static (FileStream Lock, Socket Listener) SilentFirst(string folder, int backlog = 4)
    {
        Directory.CreateDirectory(folder);
        var held = new FileStream(Path.Combine(folder, SingleInstance.LockName), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(Channel(folder));
        listener.Listen(backlog);
        return (held, listener);
    }

    /// <summary>The channel of a per-user folder, as a later start calls it (Linux).</summary>
    internal static UnixDomainSocketEndPoint Channel(string folder) => new("\0" + SingleInstance.ChannelName(SingleInstance.RealPath(folder)));

    [Fact]
    public void AFirstThatDoesntAnswerLeavesTheLaterStartUnanswered()
    {
        using var scratch = new ScratchFolder();
        Directory.CreateDirectory(scratch["p"]);
        // Held as the game holds it, but no one listens.
        using (new FileStream(scratch[$"p/{SingleInstance.LockName}"], FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
        {
            var (outcome, took) = LaterStart(scratch["p"], Short);
            Assert.Equal(SingleInstanceOutcome.Unanswered, outcome);
            Assert.InRange(took, Short, Short + TimeSpan.FromSeconds(5));
        }
        if (OperatingSystem.IsWindows())
            return;
        // Held and listening, the call taken into its queue, but never answered: the later start waits for the answer
        // only until its time is over.
        var (held, listener) = SilentFirst(scratch["q"]);
        using (held)
        using (listener)
        {
            var (outcome, took) = LaterStart(scratch["q"], Short);
            Assert.Equal(SingleInstanceOutcome.Unanswered, outcome);
            Assert.InRange(took, Short, Short + TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public void ALaterStartKeepsToItsTimeWhenTheFirstTakesNoMoreCalls()
    {
        if (OperatingSystem.IsWindows())
            return;
        using var scratch = new ScratchFolder();
        // A first that holds the folder and listens, but takes no call (a stopped process): its queue of calls full.
        var (held, listener) = SilentFirst(scratch["p"], backlog: 0);
        using var heldLock = held;
        using var heldListener = listener;
        using var waiting = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified) { Blocking = false };
        waiting.Connect(Channel(scratch["p"]));
        using var refused = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified) { Blocking = false };
        Assert.Equal(SocketError.WouldBlock, Assert.Throws<SocketException>(() => refused.Connect(Channel(scratch["p"]))).SocketErrorCode);
        // The later start gives up when its time is over, as when the first doesn't answer (2026-11-15: its connect
        // waited for the call to be taken, without end).
        var (outcome, took) = LaterStart(scratch["p"], Short);
        Assert.Equal(SingleInstanceOutcome.Unanswered, outcome);
        Assert.InRange(took, Short, Short + TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ACallThatSaysNothingHoldsTheFirstASecondAtMost()
    {
        if (OperatingSystem.IsWindows())
            return;
        using var scratch = new ScratchFolder();
        using var first = SingleInstance.Claim(scratch["p"]);
        // Taken before the later start's: a call that hangs up at once (done with), then one that says nothing (the first
        // waits for its question a second), then the later start's is served.
        using (var gone = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
            gone.Connect(Channel(scratch["p"]));
        // Timed from before the silent call is made, so the first's second for it lies inside.
        var clock = Stopwatch.StartNew();
        using var mute = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        mute.Connect(Channel(scratch["p"]));
        // Given 10 s, back as soon as it is answered: after about a second.
        var (outcome, _) = LaterStart(scratch["p"], TimeSpan.FromSeconds(10));
        var took = clock.Elapsed;
        Assert.Equal(SingleInstanceOutcome.Forwarded, outcome);
        Assert.True(took >= TimeSpan.FromMilliseconds(950), $"served after {took.TotalMilliseconds:0} ms: the first didn't wait a second for the silent call's question");
    }

    [Fact]
    public void OnlyTheGamesQuestionAndAnswerCount()
    {
        if (OperatingSystem.IsWindows())
            return;
        using var scratch = new ScratchFolder();
        using var first = SingleInstance.Claim(scratch["p"]);
        var asked = 0;
        first.ForwardAsked += () => Interlocked.Increment(ref asked);
        // Something else said on the channel: not asked forward, no answer (the connection closed).
        using (var other = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified))
        {
            other.Connect(new UnixDomainSocketEndPoint("\0" + SingleInstance.ChannelName(first.Folder)));
            other.Send("hello!!\n"u8);
            var buffer = new byte[8];
            other.ReceiveTimeout = 5000;
            Assert.Equal(0, other.Receive(buffer));
        }
        Assert.Equal(0, Volatile.Read(ref asked));

        // A channel that answers something else is no game: the later start isn't forwarded.
        Directory.CreateDirectory(scratch["q"]);
        using var held = new FileStream(scratch[$"q/{SingleInstance.LockName}"], FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        using var listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
        listener.Bind(new UnixDomainSocketEndPoint("\0" + SingleInstance.ChannelName(SingleInstance.RealPath(scratch["q"]))));
        listener.Listen(4);
        var answering = new Thread(() =>
        {
            try
            {
                while (true)
                {
                    using var client = listener.Accept();
                    var question = new byte[8];
                    client.Receive(question);
                    client.Send("no\n"u8);
                }
            }
            catch (SocketException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }) { IsBackground = true };
        answering.Start();
        using var later = SingleInstance.Claim(scratch["q"], Short);
        Assert.Equal(SingleInstanceOutcome.Unanswered, later.Outcome);
    }

    [Fact]
    public void TheFolderIsMadePrivateAndAFolderThatCantBeMadeStartsAlone()
    {
        using var scratch = new ScratchFolder();
        using (var first = SingleInstance.Claim(scratch["data/stat-fick"]))
        {
            Assert.Equal(SingleInstanceOutcome.First, first.Outcome);
            if (!OperatingSystem.IsWindows())
            {
                var folder = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
                Assert.Equal((folder, folder), (File.GetUnixFileMode(scratch["data"]), File.GetUnixFileMode(scratch["data/stat-fick"])));
                Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(scratch[$"data/stat-fick/{SingleInstance.LockName}"]));
            }
        }
        File.WriteAllText(scratch["file"], "not a folder");
        using var alone = SingleInstance.Claim(scratch["file/stat-fick"], Short);
        Assert.Equal(SingleInstanceOutcome.First, alone.Outcome);
        Assert.StartsWith("the per-user folder can't be made: ", alone.Problem);
    }

    [Fact]
    public void ALaterStartEndsWithZeroAndNeverMakesAGame()
    {
        using var scratch = new ScratchFolder();
        using var first = SingleInstance.Claim(scratch["p"]);
        var asked = 0;
        first.ForwardAsked += () => Interlocked.Increment(ref asked);
        var said = new List<string>();
        var code = Program.Start(new GameOptions(), scratch["p"], said.Add, _ => throw new InvalidOperationException("a later start ran the game"));
        Assert.Equal((0, 1), (code, Volatile.Read(ref asked)));
        Assert.Equal([Program.ForwardedText], said);
        // Held, but not answering: it ends all the same.
        Directory.CreateDirectory(scratch["q"]);
        using var held = new FileStream(scratch[$"q/{SingleInstance.LockName}"], FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        said.Clear();
        Assert.Equal(0, Program.Start(new GameOptions(), scratch["q"], said.Add, _ => throw new InvalidOperationException("a later start ran the game"), Short));
        Assert.Equal([Program.UnansweredText], said);
    }

    [Fact]
    public void TheFirstStartRunsTheGameAndALaterStartReachesIt()
    {
        using var scratch = new ScratchFolder();
        var said = new List<string>();
        var options = new GameOptions { About = true };
        StatFickGame? ran = null;
        var window = new FakeWindow();
        var code = Program.Start(options, scratch["p"], said.Add, game =>
        {
            ran = game;
            game.GameWindow = window;
            // A later start while it runs: asked forward, which the game takes at its next update.
            using var later = SingleInstance.Claim(scratch["p"]);
            Assert.Equal(SingleInstanceOutcome.Forwarded, later.Outcome);
            game.TakeForwardRequest();
            return 7;
        });
        Assert.Equal(7, code);
        Assert.Empty(said);
        Assert.Equal(["forward"], window.Done);
        Assert.Equal((scratch["p"], StatFickGame.LogName), (ran!.Options.GameFolder, ran.Options.LogName));
        // The folder is free once the start is over.
        using var next = SingleInstance.Claim(scratch["p"], Short);
        Assert.Equal(SingleInstanceOutcome.First, next.Outcome);
    }
}

/// <summary>Tests that hold the process's thread pool run alone (after the others), so no other test waits on it.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class HeldThreadPoolCollection
{
    public const string Name = "The thread pool held";
}

/// <summary>
/// The single instance with the process's thread pool held (2026-11-15: a test host's pool, busy with the tests running
/// beside, held the first's answer and the later start's own time while both waited on the pool): on Linux both sides
/// wait on their own threads only.
/// </summary>
[Collection(HeldThreadPoolCollection.Name)]
public class SingleInstanceHeldPoolTests
{
    [Fact]
    public void TheFirstAnswersAndALaterStartKeepsToItsTimeWhileTheThreadPoolIsHeld()
    {
        // Windows' named pipe still waits on the pool (not changed: not checked there).
        if (OperatingSystem.IsWindows())
            return;
        using var scratch = new ScratchFolder();
        using var first = SingleInstance.Claim(scratch["p"]);
        // q: a first that holds its folder and listens, but takes no call and answers nothing.
        var (held, silent) = SingleInstanceTests.SilentFirst(scratch["q"]);
        using var heldLock = held;
        using var heldListener = silent;
        (SingleInstanceOutcome Outcome, TimeSpan Took) forwarded, unanswered;
        using (var pool = new HeldThreadPool(TimeSpan.FromSeconds(10)))
        {
            forwarded = SingleInstanceTests.LaterStart(scratch["p"]);
            unanswered = SingleInstanceTests.LaterStart(scratch["q"], SingleInstanceTests.Short);
            Assert.False(pool.LetGo, "the single instance waited for the thread pool");
            Assert.True(pool.Held, "the thread pool wasn't held all along");
        }
        Assert.Equal((SingleInstanceOutcome.Forwarded, SingleInstanceOutcome.Unanswered), (forwarded.Outcome, unanswered.Outcome));
        Assert.InRange(unanswered.Took, SingleInstanceTests.Short, SingleInstanceTests.Short + TimeSpan.FromSeconds(5));
    }
}

/// <summary>
/// The process's thread pool held: every thread it has kept busy, and more work queued behind them than it adds threads
/// for while held; let go when disposed, or by a thread of its own after a limit (so what waits on the pool comes back
/// late rather than hangs).
/// </summary>
sealed class HeldThreadPool : IDisposable
{
    readonly ManualResetEventSlim _go = new();
    int _holding, _probed;

    public HeldThreadPool(TimeSpan limit)
    {
        for (var i = 0; i < ThreadPool.ThreadCount + 64; i++)
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                Interlocked.Increment(ref _holding);
                _go.Wait();
            }, null);
        // Held once the threads stop taking work while work is still queued; a probe queued last runs only when the pool
        // gets past the work held.
        var clock = Stopwatch.StartNew();
        for (var holding = -1; Volatile.Read(ref _holding) != holding || ThreadPool.PendingWorkItemCount == 0;)
        {
            if (clock.Elapsed > TimeSpan.FromSeconds(10))
                throw new InvalidOperationException("the thread pool couldn't be held");
            holding = Volatile.Read(ref _holding);
            Thread.Sleep(100);
        }
        ThreadPool.UnsafeQueueUserWorkItem(_ => Volatile.Write(ref _probed, 1), null);
        new Thread(() =>
        {
            _go.Wait(limit);
            _go.Set();
        }) { IsBackground = true, Name = "The thread pool let go" }.Start();
    }

    /// <summary>Whether the pool is still held (the probe queued behind the held work hasn't run).</summary>
    public bool Held => Volatile.Read(ref _probed) == 0;

    /// <summary>Whether the pool was let go (the limit passed).</summary>
    public bool LetGo => _go.IsSet;

    public void Dispose() => _go.Set();
}
