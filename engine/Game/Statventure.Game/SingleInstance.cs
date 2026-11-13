using System.IO.Pipes;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace Statventure.Game;

/// <summary>How a start went for <see cref="SingleInstance.Claim"/>.</summary>
public enum SingleInstanceOutcome
{
    /// <summary>This start is the only one on its per-user folder: it runs (and answers later starts).</summary>
    First,
    /// <summary>Another start runs on this per-user folder and was asked to bring its window forward: this one ends.</summary>
    Forwarded,
    /// <summary>Another start holds this per-user folder but didn't answer in time: this one ends all the same.</summary>
    Unanswered,
}

/// <summary>
/// One Stat Fick per per-user folder (GM1-P14; the old shell's single instance, <c>main.cjs</c> 17–19: a second start
/// brings the first window back and focuses it, then quits). The first start takes an exclusive lock on
/// <see cref="LockName"/> in the per-user folder (the folder made if missing, each level 0700 on Linux as the engine's
/// storage makes them) and listens on a local channel named from that folder: a Unix socket in Linux's abstract
/// namespace (no file, so nothing is left behind by a crash), a named pipe on Windows. A later start that can't take the
/// lock connects to the channel, asks the first to come forward and ends. Per per-user folder, so tests and scratch runs
/// on folders of their own never block each other; the same folder reached through a symbolic link is the same.
/// The lock file stays in the folder (empty; the lock is the open file's, released when the program ends, also by a
/// crash).
/// </summary>
public sealed class SingleInstance : IDisposable
{
    /// <summary>The lock file's name in the per-user folder.</summary>
    public const string LockName = "stat-fick.lock";

    /// <summary>What a later start sends, and the first's answer.</summary>
    const string Ask = "forward\n", Answer = "ok\n";

    /// <summary>How long a later start tries to reach the first (it may be just starting) before it gives up.</summary>
    public static readonly TimeSpan DefaultAnswerTime = TimeSpan.FromSeconds(3);

    readonly FileStream? _lock;
    readonly Socket? _socket;
    readonly CancellationTokenSource _stop = new();
    readonly Thread? _listener;

    /// <summary>How this start went.</summary>
    public SingleInstanceOutcome Outcome { get; }

    /// <summary>The per-user folder, its symbolic links resolved.</summary>
    public string Folder { get; }

    /// <summary>
    /// What went wrong taking the lock or listening, when this start runs all the same (<see cref="SingleInstanceOutcome.First"/>
    /// without the lock: a folder that can't be written, a channel that can't be made); null when all is well.
    /// </summary>
    public string? Problem { get; }

    /// <summary>
    /// A later start asked this one to bring its window forward. Raised on the listener's thread: the handler only notes
    /// it for the game's thread (the window is GLFW's, on the main thread).
    /// </summary>
    public event Action? ForwardAsked;

    SingleInstance(SingleInstanceOutcome outcome, string folder, FileStream? held, string? problem)
    {
        Outcome = outcome;
        Folder = folder;
        _lock = held;
        Problem = problem;
        if (outcome != SingleInstanceOutcome.First || held is null)
            return;
        try
        {
            if (OperatingSystem.IsWindows())
                _listener = Listen(() => ServePipe(ChannelName(folder)));
            else
            {
                _socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                _socket.Bind(Endpoint(folder));
                _socket.Listen(4);
                _listener = Listen(ServeSocket);
            }
        }
        catch (Exception problemListening) when (problemListening is SocketException or IOException or UnauthorizedAccessException)
        {
            _socket?.Dispose();
            _socket = null;
            Problem = $"later starts can't reach this one: {problemListening.Message}";
        }
    }

    /// <summary>
    /// Claims <paramref name="gameFolder"/> for this start: <see cref="SingleInstanceOutcome.First"/> holding the lock and
    /// listening; or, when another start holds it, that one asked forward (<see cref="SingleInstanceOutcome.Forwarded"/>,
    /// or <see cref="SingleInstanceOutcome.Unanswered"/> when it didn't answer within <paramref name="answerTime"/>).
    /// A lock that can't be taken for another reason (the folder can't be made or written) is a first start without it.
    /// </summary>
    public static SingleInstance Claim(string gameFolder, TimeSpan? answerTime = null)
    {
        string folder;
        try
        {
            CreateFolder(gameFolder);
            folder = RealPath(gameFolder);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            return new(SingleInstanceOutcome.First, Path.GetFullPath(gameFolder), null, $"the per-user folder can't be made: {problem.Message}");
        }
        var file = Path.Combine(folder, LockName);
        try
        {
            var options = new FileStreamOptions { Mode = FileMode.OpenOrCreate, Access = FileAccess.ReadWrite, Share = FileShare.None };
            if (!OperatingSystem.IsWindows())
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
            return new(SingleInstanceOutcome.First, folder, new FileStream(file, options), null);
        }
        catch (IOException problem) when (IsHeldElsewhere(problem))
        {
            return new(AskForward(folder, answerTime ?? DefaultAnswerTime), folder, null, null);
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            return new(SingleInstanceOutcome.First, folder, null, $"the lock {file} can't be taken: {problem.Message}");
        }
    }

    /// <summary>
    /// Whether opening the lock failed because another open file holds it: on Windows a sharing or lock violation
    /// (<c>ERROR_SHARING_VIOLATION</c> 32, <c>ERROR_LOCK_VIOLATION</c> 33); on Linux <c>flock</c>'s <c>EWOULDBLOCK</c> (11),
    /// which .NET gives as the exception's HResult (checked on the PC, 2026-11-13: "The process cannot access the file …
    /// because it is being used by another process", 0x0000000B; a folder that can't be written is an
    /// <see cref="UnauthorizedAccessException"/>).
    /// </summary>
    static bool IsHeldElsewhere(IOException problem) => OperatingSystem.IsWindows()
        ? (problem.HResult & 0xFFFF) is 32 or 33
        : problem.HResult == 11;

    /// <summary>
    /// The channel's name for a per-user folder: <c>stat-fick-</c> and the first 16 hexadecimal digits of the SHA-256 of
    /// its full path with links resolved (on Windows in capitals: its paths ignore case).
    /// </summary>
    internal static string ChannelName(string realFolder)
    {
        var path = OperatingSystem.IsWindows() ? realFolder.ToUpperInvariant() : realFolder;
        return "stat-fick-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(path)))[..16];
    }

    /// <summary>The Unix socket's address: in Linux's abstract namespace (a name, no file).</summary>
    static UnixDomainSocketEndPoint Endpoint(string realFolder) => new("\0" + ChannelName(realFolder));

    /// <summary>A later start: asks the first, on the channel of <paramref name="folder"/>, to come forward; tries until <paramref name="answerTime"/> has passed.</summary>
    static SingleInstanceOutcome AskForward(string folder, TimeSpan answerTime)
    {
        var until = DateTime.UtcNow + answerTime;
        while (true)
        {
            var left = until - DateTime.UtcNow;
            if (left <= TimeSpan.Zero)
                return SingleInstanceOutcome.Unanswered;
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    using var pipe = new NamedPipeClientStream(".", ChannelName(folder), PipeDirection.InOut);
                    pipe.Connect((int)Math.Max(1, left.TotalMilliseconds));
                    if (Exchange(pipe, left))
                        return SingleInstanceOutcome.Forwarded;
                }
                else
                {
                    using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                    socket.Connect(Endpoint(folder));
                    using var stream = new NetworkStream(socket);
                    if (Exchange(stream, left))
                        return SingleInstanceOutcome.Forwarded;
                }
            }
            catch (Exception problem) when (problem is SocketException or IOException or TimeoutException or OperationCanceledException)
            {
                // Not listening yet (the first is just starting), or gone: try again until the time is up.
            }
            Thread.Sleep(50);
        }
    }

    /// <summary>Sends the question and waits for the answer.</summary>
    static bool Exchange(Stream stream, TimeSpan left)
    {
        stream.Write(Encoding.ASCII.GetBytes(Ask));
        stream.Flush();
        using var timeout = new CancellationTokenSource(left);
        var buffer = new byte[Answer.Length];
        var read = 0;
        while (read < buffer.Length)
        {
            var n = stream.ReadAsync(buffer.AsMemory(read), timeout.Token).AsTask().GetAwaiter().GetResult();
            if (n == 0)
                return false;
            read += n;
        }
        return Encoding.ASCII.GetString(buffer) == Answer;
    }

    Thread Listen(Action serve)
    {
        var thread = new Thread(() =>
        {
            try
            {
                serve();
            }
            catch (Exception problem) when (problem is SocketException or IOException or ObjectDisposedException or OperationCanceledException)
            {
                // Disposed: the start is over.
            }
        })
        {
            Name = "Stat Fick single instance",
            IsBackground = true,
        };
        thread.Start();
        return thread;
    }

    void ServeSocket()
    {
        while (!_stop.IsCancellationRequested)
        {
            using var client = _socket!.AcceptAsync(_stop.Token).AsTask().GetAwaiter().GetResult();
            using var stream = new NetworkStream(client);
            Serve(stream);
        }
    }

    void ServePipe(string name)
    {
        while (!_stop.IsCancellationRequested)
        {
            using var pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            pipe.WaitForConnectionAsync(_stop.Token).GetAwaiter().GetResult();
            Serve(pipe);
        }
    }

    /// <summary>One later start: its question read (a second at most), the event raised, the answer sent.</summary>
    void Serve(Stream stream)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token);
            timeout.CancelAfter(TimeSpan.FromSeconds(1));
            var buffer = new byte[Ask.Length];
            var read = 0;
            while (read < buffer.Length)
            {
                var n = stream.ReadAsync(buffer.AsMemory(read), timeout.Token).AsTask().GetAwaiter().GetResult();
                if (n == 0)
                    return;
                read += n;
            }
            if (Encoding.ASCII.GetString(buffer) != Ask)
                return;
            ForwardAsked?.Invoke();
            stream.Write(Encoding.ASCII.GetBytes(Answer));
            stream.Flush();
        }
        catch (Exception problem) when (problem is IOException or SocketException or OperationCanceledException)
        {
            // That start went away, or said something else: the next one is served.
        }
    }

    /// <summary>Makes a folder and its missing parents; on Linux each made with mode 0700, as the engine's storage makes them.</summary>
    static void CreateFolder(string folder)
    {
        if (OperatingSystem.IsWindows())
        {
            Directory.CreateDirectory(folder);
            return;
        }
        var missing = new Stack<string>();
        for (var at = Path.GetFullPath(folder); !Directory.Exists(at);)
        {
            missing.Push(at);
            if (Path.GetDirectoryName(at) is not { } parent)
                break;
            at = parent;
        }
        while (missing.Count > 0)
            Directory.CreateDirectory(missing.Pop(), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }

    /// <summary>The full path of an existing folder with every symbolic link in it resolved (a link to a link too).</summary>
    internal static string RealPath(string path, int depth = 0)
    {
        if (depth > 40)
            throw new IOException($"{path}: too many symbolic links");
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full)!;
        var real = root;
        foreach (var part in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            var next = Path.Combine(real, part);
            var info = new DirectoryInfo(next);
            if (info.LinkTarget is { } target)
                next = RealPath(Path.IsPathRooted(target) ? target : Path.Combine(real, target), depth + 1);
            real = next;
        }
        return real;
    }

    public void Dispose()
    {
        _stop.Cancel();
        _socket?.Dispose();
        _listener?.Join(TimeSpan.FromSeconds(2));
        _lock?.Dispose();
        _stop.Dispose();
    }
}
