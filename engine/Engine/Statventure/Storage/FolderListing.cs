namespace Statventure.Storage;

/// <summary>One entry of a folder's listing.</summary>
/// <param name="Size">A file's length in bytes; 0 for a folder.</param>
/// <param name="IsHidden">A dot-file on Linux, the hidden attribute on Windows.</param>
public sealed record FolderEntry(string Name, string FullPath, bool IsFolder, long Size, DateTime LastWrittenUtc, bool IsHidden);

/// <summary>
/// Folder listings for the port's file picker (Docs/Assets.md, Q19: M9 gives the functions, the
/// screen is made at port time): a folder's entries, and the roots to start from.
/// </summary>
public static class FolderListing
{
    static readonly EnumerationOptions Options = new()
    {
        RecurseSubdirectories = false,
        AttributesToSkip = 0,
        IgnoreInaccessible = true,
        ReturnSpecialDirectories = false,
    };

    /// <summary>
    /// The folders and files directly in <paramref name="folder"/>, hidden ones too (flagged), in
    /// ordinal order of their names; what the picker shows and how it sorts is the port's (the file
    /// browser's model, <see cref="FileBrowser"/>). A folder that can't be read lists as empty
    /// (<see cref="EnumerationOptions.IgnoreInaccessible"/>), without an exception.
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">There is no such folder (or it is a file).</exception>
    public static IReadOnlyList<FolderEntry> List(string folder)
    {
        var entries = new List<FolderEntry>();
        foreach (var info in new DirectoryInfo(folder).EnumerateFileSystemInfos("*", Options))
        {
            var isFolder = info is DirectoryInfo;
            entries.Add(new FolderEntry(info.Name, info.FullName, isFolder, isFolder ? 0 : ((FileInfo)info).Length,
                info.LastWriteTimeUtc, (info.Attributes & FileAttributes.Hidden) != 0));
        }
        entries.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
        return entries;
    }

    /// <summary>Where the picker can start from: <c>/</c> on Linux; the ready drives on Windows.</summary>
    public static IReadOnlyList<string> Roots() =>
        OperatingSystem.IsWindows() ? [.. DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.RootDirectory.FullName)] : ["/"];
}
