namespace Statventure.Storage;

/// <summary>
/// The systems whose rules the per-user folders follow (Linux and Windows only: decided 2026-10-16).
/// The folders are resolved by pure functions of the system, so the tests check each system's rule
/// on any machine.
/// </summary>
public enum StorageSystem
{
    Linux,
    Windows,
}

/// <summary>Helpers shared by the storage code: this machine's system, folders made private, files written whole.</summary>
static class StorageFiles
{
    public static StorageSystem ThisSystem => OperatingSystem.IsWindows() ? StorageSystem.Windows : StorageSystem.Linux;

    const UnixFileMode PrivateFolder = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    const UnixFileMode PrivateFile = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    /// <summary>
    /// Makes a folder and its missing parents; on Linux each made with mode 0700, as the XDG base
    /// directory specification asks of its folders and as Chromium's <c>base::CreateDirectory</c> makes
    /// every level. (.NET's <c>Directory.CreateDirectory(path, mode)</c> gives the mode to the last
    /// folder only, the parents 0777 less the umask, so each missing level is made here.)
    /// </summary>
    public static void CreateFolder(string folder)
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
            Directory.CreateDirectory(missing.Pop(), PrivateFolder);
    }

    /// <summary>
    /// Writes <paramref name="bytes"/> as the whole of a new file (replacing one there), flushed to
    /// the disk before it returns, so a rename that follows can't publish a file whose bytes are lost
    /// in a crash. On Linux the file is made with mode 0600.
    /// </summary>
    public static void WriteFlushed(string file, ReadOnlySpan<byte> bytes)
    {
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write, Share = FileShare.None };
        if (!OperatingSystem.IsWindows())
            options.UnixCreateMode = PrivateFile;
        using var stream = new FileStream(file, options);
        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

    /// <summary>
    /// A name beside <paramref name="file"/> for keeping it aside: <c>&lt;file&gt;.damaged</c>, else
    /// <c>.damaged-2</c>, <c>-3</c> … the first that nothing has (Docs/Assets.md, Q39: never overwritten).
    /// </summary>
    public static string KeptAsideName(string file)
    {
        for (var n = 1; ; n++)
        {
            var name = n == 1 ? file + ".damaged" : $"{file}.damaged-{n}";
            if (!Path.Exists(name))
                return name;
        }
    }
}
