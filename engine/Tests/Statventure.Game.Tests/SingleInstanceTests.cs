using System.Diagnostics;
using System.Net.Sockets;

namespace Statventure.Game.Tests;

/// <summary>One Stat Fick per per-user folder (GM1-P14): the lock, the channel, a later start asking the first forward.</summary>
public class SingleInstanceTests
{
    static readonly TimeSpan Short = TimeSpan.FromMilliseconds(400);

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
        using var later = SingleInstance.Claim(scratch["home/stat-fick"], Short);
        Assert.Equal((SingleInstanceOutcome.First, SingleInstanceOutcome.Forwarded), (first.Outcome, later.Outcome));
        Assert.Equal(first.Folder, later.Folder);
        Assert.Equal(SingleInstance.RealPath(scratch["real/stat-fick"]), SingleInstance.RealPath(scratch["home/stat-fick"]));
        // A link to a link, and one given relative to where it is.
        Directory.CreateSymbolicLink(scratch["again"], "home");
        Assert.Equal(first.Folder, SingleInstance.RealPath(scratch["again/stat-fick"]));
    }

    [Fact]
    public void AFirstThatDoesntAnswerLeavesTheLaterStartUnanswered()
    {
        using var scratch = new ScratchFolder();
        Directory.CreateDirectory(scratch["p"]);
        // Held as the game holds it, but no one listens.
        using var held = new FileStream(scratch[$"p/{SingleInstance.LockName}"], FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var clock = Stopwatch.StartNew();
        using var later = SingleInstance.Claim(scratch["p"], Short);
        Assert.Equal(SingleInstanceOutcome.Unanswered, later.Outcome);
        Assert.InRange(clock.Elapsed, Short, Short + TimeSpan.FromSeconds(5));
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
