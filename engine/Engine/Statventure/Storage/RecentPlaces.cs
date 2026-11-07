namespace Statventure.Storage;

/// <summary>
/// The file browser's recent places (Docs/Editor.md, step 3, P26): the folders used lately, most recent first,
/// at most <see cref="Limit"/>. The caller keeps them where it keeps its own things (the editor in its
/// <c>editor/</c> folder, G6) and gives them back next time; nothing here reads or writes a file. Each folder
/// is kept as a full path without a separator at its end, once (as this system compares names:
/// <see cref="FileBrowser.NameComparison"/>). Folders that are gone stay listed here (a drive may come back);
/// <see cref="FileBrowser.Places(string, RecentPlaces?)"/> leaves them out.
/// </summary>
public sealed class RecentPlaces
{
    /// <summary>How many folders are kept.</summary>
    public const int Limit = 10;

    readonly List<string> _folders = [];

    /// <summary>
    /// The recent places as the caller stored them (<see cref="Folders"/>), most recent first; each read as
    /// <see cref="Add"/> reads it, the later of two equal ones, those past the limit and those that aren't paths
    /// on this system dropped.
    /// </summary>
    public RecentPlaces(IEnumerable<string>? stored = null)
    {
        var folders = stored?.ToList() ?? [];
        for (var i = folders.Count - 1; i >= 0; i--)
        {
            try
            {
                Add(folders[i]);
            }
            catch (Exception problem) when (problem is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // Not a path here (a stored list from another system, or damaged): left out.
            }
        }
    }

    /// <summary>The folders, most recent first: what the caller stores.</summary>
    public IReadOnlyList<string> Folders => _folders;

    /// <summary>
    /// Puts <paramref name="folder"/> (made full: relative to the working folder, the separator at its end
    /// dropped) first, removing it from where it was; the oldest goes past <see cref="Limit"/>. An empty name is
    /// ignored.
    /// </summary>
    /// <exception cref="ArgumentException">It isn't a path on this system.</exception>
    public void Add(string folder)
    {
        if (string.IsNullOrEmpty(folder))
            return;
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(folder));
        var comparison = FileBrowser.NameComparison;
        _folders.RemoveAll(kept => string.Equals(kept, full, comparison));
        _folders.Insert(0, full);
        if (_folders.Count > Limit)
            _folders.RemoveRange(Limit, _folders.Count - Limit);
    }
}
