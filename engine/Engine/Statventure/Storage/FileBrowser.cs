namespace Statventure.Storage;

/// <summary>What a place of the file browser is (the screen names it).</summary>
public enum BrowserPlaceKind
{
    /// <summary>The player's mods folder (<see cref="GameStorage.ModsFolder"/>).</summary>
    Mods,
    /// <summary>The Documents folder (<see cref="DocumentsFolder"/>), where character files go.</summary>
    Documents,
    /// <summary>The home folder.</summary>
    Home,
    /// <summary>A root of the file system (<see cref="FolderListing.Roots"/>).</summary>
    Root,
    /// <summary>A folder used lately (<see cref="RecentPlaces"/>).</summary>
    Recent,
}

/// <summary>A place the file browser offers to start from: what it is and its folder (a full path).</summary>
public sealed record BrowserPlace(BrowserPlaceKind Kind, string Folder);

/// <summary>What a path typed into the browser's path field names (<see cref="FileBrowser.ReadPath(string, string)"/>).</summary>
public enum TypedPathKind
{
    /// <summary>A folder: the browser goes there.</summary>
    Folder,
    /// <summary>A file: the browser picks it.</summary>
    File,
    /// <summary>Nothing is there, but the folder it would be in is: a new file's name (as for an export).</summary>
    New,
    /// <summary>Nothing is there, nor the folder it would be in (or it ends in a separator, naming a folder that isn't there).</summary>
    Missing,
    /// <summary>Not a path on this system (a character it doesn't allow).</summary>
    Invalid,
}

/// <summary>A typed path read: the full path it names (the text as typed when it is <see cref="TypedPathKind.Invalid"/>) and what is there.</summary>
public readonly record struct TypedPath(string FullPath, TypedPathKind Kind);

/// <summary>
/// The path field's completion of its last name: the field's new text, and the entries the name could
/// complete to (the screen may show them), in the browser's order.
/// </summary>
public sealed record PathCompletion(string Text, IReadOnlyList<FolderEntry> Matches);

/// <summary>
/// The file browser's model (Docs/Editor.md, "The window, the sound and the file browser's model (step 3)",
/// P26; M11-9): the places to start from, a folder's entries as the browser lists them, and the path field's
/// reading and completion, on <see cref="FolderListing"/>. The screen and its looks are the caller's: the
/// editor's browser (M11 step 8), and the game's Load Character later. Nothing here writes, except that
/// asking for this machine's Documents folder makes it when it is missing, as everywhere in the game
/// (<see cref="DocumentsFolder.ForThisMachine"/>, Electron's rule).
/// </summary>
public static class FileBrowser
{
    /// <summary>
    /// How names are compared on this system's file systems: case matters on Linux, not on Windows. Typed names
    /// match entries by it (completion).
    /// </summary>
    public static StringComparison NameComparison =>
        StorageFiles.ThisSystem == StorageSystem.Windows ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// The order of names in a listing, the same on every system: ignoring case (ordinal on the upper-case
    /// forms), names equal so ordered by their ordinal order ("Readme" before "readme").
    /// </summary>
    public static IComparer<string> NameOrder { get; } = Comparer<string>.Create((a, b) =>
        string.Compare(a, b, StringComparison.OrdinalIgnoreCase) is var byLetters and not 0 ? byLetters : string.CompareOrdinal(a, b));

    /// <summary>
    /// This machine's places: the mods folder given (the application's <see cref="GameStorage.ModsFolder"/>, so a
    /// game folder of the options' own is followed; listed even before it exists), the Documents folder
    /// (<see cref="DocumentsFolder.ForThisMachine"/>, which makes it when it is missing; left out when it can't be
    /// made), the home folder (left out when it isn't known), the roots, then the recent places that are folders
    /// now, most recent first.
    /// </summary>
    public static IReadOnlyList<BrowserPlace> Places(string modsFolder, RecentPlaces? recent = null)
    {
        string? documents;
        try
        {
            documents = DocumentsFolder.ForThisMachine();
        }
        catch (Exception problem) when (problem is IOException or InvalidOperationException)
        {
            // It can't be made (IOException), or Windows doesn't know it.
            documents = null;
        }
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);
        return Places(modsFolder, documents, home, FolderListing.Roots(), recent?.Folders ?? []);
    }

    /// <summary>
    /// The places in the browser's order: the mods folder, Documents, home (each left out when null or empty), the
    /// roots, then the recent places that are folders now, in the order given. Each folder as a full path.
    /// </summary>
    public static IReadOnlyList<BrowserPlace> Places(string modsFolder, string? documents, string? home, IEnumerable<string> roots, IEnumerable<string> recent)
    {
        var places = new List<BrowserPlace> { new(BrowserPlaceKind.Mods, Path.GetFullPath(modsFolder)) };
        if (!string.IsNullOrEmpty(documents))
            places.Add(new(BrowserPlaceKind.Documents, Path.GetFullPath(documents)));
        if (!string.IsNullOrEmpty(home))
            places.Add(new(BrowserPlaceKind.Home, Path.GetFullPath(home)));
        places.AddRange(roots.Select(root => new BrowserPlace(BrowserPlaceKind.Root, root)));
        places.AddRange(recent.Where(Directory.Exists).Select(folder => new BrowserPlace(BrowserPlaceKind.Recent, Path.GetFullPath(folder))));
        return places;
    }

    /// <summary>
    /// Whether a file named <paramref name="name"/> is one the browser shows for <paramref name="endings"/>: its
    /// name ends in one of them, in any case (as the mods folder's ".pak" and the importers' endings are read);
    /// any file when there are none.
    /// </summary>
    public static bool Shows(string name, IReadOnlyCollection<string>? endings) =>
        endings is not { Count: > 0 } || endings.Any(ending => name.EndsWith(ending, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// A folder's entries as the browser lists them: its folders, then its files, each in <see cref="NameOrder"/>;
    /// files only those <see cref="Shows"/> gives for <paramref name="endings"/> (every folder, to go into);
    /// hidden ones (<see cref="FolderEntry.IsHidden"/>) only when <paramref name="showHidden"/>. A folder that can't
    /// be read lists as empty (the listing skips what it may not read).
    /// </summary>
    /// <exception cref="DirectoryNotFoundException">There is no such folder (or it is a file).</exception>
    public static IReadOnlyList<FolderEntry> List(string folder, IReadOnlyCollection<string>? endings = null, bool showHidden = false) =>
        [.. FolderListing.List(folder)
            .Where(entry => (showHidden || !entry.IsHidden) && (entry.IsFolder || Shows(entry.Name, endings)))
            .OrderBy(entry => entry.IsFolder ? 0 : 1)
            .ThenBy(entry => entry.Name, NameOrder)];

    /// <summary>
    /// Reads what was typed into the path field while the browser shows <paramref name="folder"/>: a path relative
    /// to it (".." and "." resolved) or a full one, and <c>~</c> or a path starting <c>~/</c> (on Windows also
    /// <c>~\</c>) in the home folder; what is there decides its kind. Nothing typed reads as the folder itself.
    /// </summary>
    public static TypedPath ReadPath(string folder, string typed) =>
        ReadPath(folder, typed, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify));

    /// <summary><see cref="ReadPath(string, string)"/> with <paramref name="home"/> as the folder <c>~</c> names.</summary>
    internal static TypedPath ReadPath(string folder, string typed, string home)
    {
        string full;
        try
        {
            full = Path.GetFullPath(InHome(typed, home), Path.GetFullPath(folder));
        }
        catch (Exception problem) when (problem is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return new TypedPath(typed, TypedPathKind.Invalid);
        }
        var trimmed = Path.TrimEndingDirectorySeparator(full);
        if (Directory.Exists(trimmed))
            return new TypedPath(trimmed, TypedPathKind.Folder);
        if (EndsInSeparator(typed))
            return new TypedPath(trimmed, TypedPathKind.Missing);
        if (File.Exists(trimmed))
            return new TypedPath(trimmed, TypedPathKind.File);
        return new TypedPath(trimmed, Path.GetDirectoryName(trimmed) is { } parent && Directory.Exists(parent) ? TypedPathKind.New : TypedPathKind.Missing);
    }

    /// <summary>
    /// Completes the last name typed into the path field (after its last separator) while the browser shows
    /// <paramref name="folder"/>: the names in the folder the text before it reads as (see <see cref="ReadPath(string, string)"/>)
    /// that start with it (<see cref="NameComparison"/>), among what <see cref="List"/> shows for
    /// <paramref name="endings"/>, hidden ones too when the name starts with a dot. The text becomes the typed folder
    /// part and the longest start the matches share; a single match that is a folder gets a separator after it.
    /// No match (or a folder part that isn't a folder): the text unchanged.
    /// </summary>
    public static PathCompletion Complete(string folder, string typed, IReadOnlyCollection<string>? endings = null, bool showHidden = false) =>
        Complete(folder, typed, Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify), endings, showHidden);

    /// <summary><see cref="Complete(string, string, IReadOnlyCollection{string}?, bool)"/> with <paramref name="home"/> as the folder <c>~</c> names.</summary>
    internal static PathCompletion Complete(string folder, string typed, string home, IReadOnlyCollection<string>? endings = null, bool showHidden = false)
    {
        var cut = LastSeparator(typed) + 1;
        var (head, name) = (typed[..cut], typed[cut..]);
        var where = cut == 0 ? new TypedPath(Path.GetFullPath(folder), TypedPathKind.Folder) : ReadPath(folder, head, home);
        if (where.Kind != TypedPathKind.Folder)
            return new PathCompletion(typed, []);
        IReadOnlyList<FolderEntry> entries;
        try
        {
            entries = List(where.FullPath, endings, showHidden || name.StartsWith('.'));
        }
        catch (Exception problem) when (problem is IOException or UnauthorizedAccessException)
        {
            // Gone since it was read (a folder that can't be read lists as empty).
            return new PathCompletion(typed, []);
        }
        var comparison = NameComparison;
        var matches = entries.Where(entry => entry.Name.StartsWith(name, comparison)).ToList();
        if (matches.Count == 0)
            return new PathCompletion(typed, []);
        var shared = matches[0].Name;
        foreach (var match in matches.Skip(1))
        {
            var length = 0;
            while (length < shared.Length && length < match.Name.Length
                && string.Compare(shared, length, match.Name, length, 1, comparison) == 0)
                length++;
            shared = shared[..length];
        }
        var text = head + shared;
        if (matches is [{ IsFolder: true }])
            text += Path.DirectorySeparatorChar;
        return new PathCompletion(text, matches);
    }

    /// <summary><c>~</c> and <c>~/…</c> (Windows: also <c>~\…</c>) in the home folder; anything else as typed.</summary>
    static string InHome(string typed, string home)
    {
        if (string.IsNullOrEmpty(home))
            return typed;
        if (typed == "~")
            return home;
        if (typed.Length > 1 && typed[0] == '~' && IsSeparator(typed[1]))
            return Path.Join(home, typed[2..]);
        return typed;
    }

    static bool IsSeparator(char c) => c == '/' || OperatingSystem.IsWindows() && c == '\\';

    static bool EndsInSeparator(string typed) => typed.Length > 0 && IsSeparator(typed[^1]);

    static int LastSeparator(string typed)
    {
        for (var i = typed.Length - 1; i >= 0; i--)
            if (IsSeparator(typed[i]))
                return i;
        return -1;
    }
}
