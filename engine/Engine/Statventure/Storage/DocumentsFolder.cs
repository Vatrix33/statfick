using System.Text;

namespace Statventure.Storage;

/// <summary>
/// The Documents folder, the default place of character files (Docs/Assets.md, "Character files"),
/// as the old game's Electron gives it: <c>app.getPath("documents")</c>. On Linux that is Chromium
/// 152's lookup of the XDG user directory (<c>base::nix::GetXDGUserDirectory("DOCUMENTS",
/// "Documents")</c> and the xdg-user-dirs <c>xdg_user_dir_lookup</c> it calls), ported with its quirks
/// and checked against Electron in every case of
/// <c>Tests/Statventure.Tests/Data/Storage/documents-reference.json</c>; .NET's
/// <c>SpecialFolder.MyDocuments</c> gives another folder in 31 of those 46 cases (no entry is
/// <c>~/Documents</c> there, the home folder in Chromium). On Windows it is the Documents known
/// folder. Like Chromium (<c>chrome_paths.cc</c>, <c>create_dir</c>), asking for it makes it when
/// it is missing.
/// </summary>
public static class DocumentsFolder
{
    /// <summary>
    /// The Documents folder by the system's rule; a pure function. Linux: the last line
    /// <c>XDG_DOCUMENTS_DIR="…"</c> of <c>$XDG_CONFIG_HOME/user-dirs.dirs</c> (or
    /// <c>$HOME/.config/user-dirs.dirs</c>) that gives <c>"$HOME/…"</c> or an absolute path, read as
    /// <c>fgets</c> reads it (511 bytes at a time, each piece up to its first NUL), backslashes
    /// escaping the next character; without one the home folder (<c>$HOME</c>, or <c>/tmp</c> when
    /// it isn't set); trailing separators stripped as Chromium's <c>FilePath</c> does. Windows:
    /// <paramref name="windowsDocuments"/>.
    /// </summary>
    /// <param name="environment">Gives an environment variable's value, or null when it isn't set.</param>
    /// <param name="readFile">Gives a file's bytes, or null when it can't be read (a path not starting with '/' is relative to the working folder).</param>
    /// <param name="windowsDocuments">The Documents known folder (Windows).</param>
    public static string Locate(StorageSystem system, Func<string, string?> environment, Func<string, byte[]?> readFile, string? windowsDocuments)
    {
        if (system == StorageSystem.Windows)
            return string.IsNullOrEmpty(windowsDocuments)
                ? throw new InvalidOperationException("the Documents known folder isn't known")
                : windowsDocuments;
        var home = environment("HOME");
        if (home == null)
            return "/tmp";
        var configHome = environment("XDG_CONFIG_HOME");
        var configFile = string.IsNullOrEmpty(configHome) ? home + "/.config/user-dirs.dirs" : configHome + "/user-dirs.dirs";
        var found = readFile(configFile) is { } bytes ? Lookup(bytes, "DOCUMENTS", home) : null;
        return StripTrailingSeparators(found ?? home);
    }

    /// <summary>This machine's Documents folder, made (with its missing parents) when it doesn't exist.</summary>
    /// <exception cref="IOException">It can't be made (as Electron's getPath fails then).</exception>
    public static string ForThisMachine() => Ensure(Locate(
        StorageFiles.ThisSystem,
        Environment.GetEnvironmentVariable,
        ReadFileOrNull,
        OperatingSystem.IsWindows() ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments, Environment.SpecialFolderOption.DoNotVerify) : null));

    /// <summary>
    /// Chromium's <c>create_dir</c> step: when nothing is at <paramref name="folder"/> it is made, with
    /// its missing parents (mode 0700 on Linux, as Chromium's <c>base::CreateDirectory</c>); a file
    /// already there is left as it is and its path given, as Chromium does. Gives the folder.
    /// </summary>
    /// <exception cref="IOException">It can't be made.</exception>
    public static string Ensure(string folder)
    {
        if (folder.Length == 0)
            throw new IOException("the Documents folder can't be made: its path is empty");
        if (Path.Exists(folder))
            return folder;
        try
        {
            StorageFiles.CreateFolder(folder);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            throw new IOException($"the Documents folder {folder} can't be made: {e.Message}", e);
        }
        return folder;
    }

    static byte[]? ReadFileOrNull(string file)
    {
        try
        {
            return File.ReadAllBytes(file);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>xdg_user_dir_lookup_with_fallback's loop over the file's lines: the last valid entry of <paramref name="type"/>, or null.</summary>
    static string? Lookup(byte[] file, string type, string home)
    {
        string? found = null;
        var key = Encoding.ASCII.GetBytes("XDG_" + type + "_DIR");
        foreach (var piece in Pieces(file))
        {
            ReadOnlySpan<byte> line = piece;
            if (line.Length > 0 && line[^1] == (byte)'\n')
                line = line[..^1];
            var p = SkipBlanks(line, 0);
            if (!line[p..].StartsWith(key))
                continue;
            p = SkipBlanks(line, p + key.Length);
            if (p >= line.Length || line[p] != (byte)'=')
                continue;
            p = SkipBlanks(line, p + 1);
            if (p >= line.Length || line[p] != (byte)'"')
                continue;
            p++;
            var value = new List<byte>();
            string prefix;
            if (line[p..].StartsWith("$HOME/"u8))
            {
                p += 6;
                prefix = home + "/";
            }
            else if (p < line.Length && line[p] == (byte)'/')
                prefix = "";
            else
                continue;
            while (p < line.Length && line[p] != (byte)'"')
            {
                if (line[p] == (byte)'\\' && p + 1 < line.Length)
                    p++;
                value.Add(line[p++]);
            }
            found = prefix + Encoding.UTF8.GetString(value.ToArray());
        }
        return found;
    }

    /// <summary>
    /// What <c>fgets(buffer, 512, file)</c> gives one call after another: at most 511 bytes, ending
    /// after a line feed if one comes first; each piece then ends at its first NUL, as <c>strlen</c>
    /// measures it.
    /// </summary>
    static IEnumerable<byte[]> Pieces(byte[] file)
    {
        const int Room = 511;
        for (var start = 0; start < file.Length;)
        {
            var length = Math.Min(Room, file.Length - start);
            var feed = Array.IndexOf(file, (byte)'\n', start, length);
            if (feed >= 0)
                length = feed - start + 1;
            var piece = file.AsSpan(start, length);
            var nul = piece.IndexOf((byte)0);
            yield return (nul >= 0 ? piece[..nul] : piece).ToArray();
            start += length;
        }
    }

    static int SkipBlanks(ReadOnlySpan<byte> line, int p)
    {
        while (p < line.Length && line[p] is (byte)' ' or (byte)'\t')
            p++;
        return p;
    }

    /// <summary>
    /// Chromium's <c>FilePath::StripTrailingSeparators</c> on POSIX: trailing '/' removed, but a path
    /// of separators keeps one ("/"), and exactly two leading ones stay ("//", which POSIX lets mean
    /// something else).
    /// </summary>
    internal static string StripTrailingSeparators(string path)
    {
        const int Start = 1;
        var length = path.Length;
        var lastStripped = -1;
        for (var pos = length; pos > Start && path[pos - 1] == '/'; pos--)
            if (pos != Start + 1 || lastStripped == Start + 2 || path[Start - 1] != '/')
            {
                length = pos - 1;
                lastStripped = pos;
            }
        return path[..length];
    }
}
