namespace Statventure.Editor.Session;

/// <summary>
/// The editor's session: its data, with no window and no document (P2), what the tests drive. Step 4 is its
/// skeleton: where the game's data is, the open pack's name for the title, whether there is unsaved work and
/// the change notice the view listens to. The later steps add their own files to this partial class
/// (Docs/Editor.md, "How it works", step 4): opening (step 8), drafts and histories (step 9) and so on.
/// </summary>
public sealed partial class EditorSession : IDisposable
{
    readonly List<Func<bool>> _unsavedWork = [];
    readonly List<IDisposable> _owned = [];
    string? _packName;

    public EditorSession(GameData game)
    {
        Game = game;
    }

    /// <summary>Where the game's packs are, and whether this is the dev setup (P5).</summary>
    public GameData Game { get; }

    /// <summary>The open pack as the title names it (its file name), or null when none is open.</summary>
    public string? PackName
    {
        get => _packName;
        set
        {
            if (_packName == value)
                return;
            _packName = value;
            NotifyChanged();
        }
    }

    /// <summary>Whether any of the sources of unsaved work says so (the title's "•", closing asks).</summary>
    public bool HasUnsavedWork => _unsavedWork.Exists(source => source());

    /// <summary>
    /// A source of unsaved work, asked each time the title is made (step 9's drafts, the entries added, renamed
    /// or removed): call <see cref="NotifyChanged"/> when its answer may have changed.
    /// </summary>
    public void AddUnsavedWork(Func<bool> source) => _unsavedWork.Add(source);

    /// <summary>The session's state changed: the view refreshes its title, status and commands.</summary>
    public event Action? Changed;

    public void NotifyChanged() => Changed?.Invoke();

    /// <summary>Something the session disposes with itself (an open pack, a file system).</summary>
    public T Own<T>(T disposable) where T : IDisposable
    {
        _owned.Add(disposable);
        return disposable;
    }

    public void Dispose()
    {
        for (var i = _owned.Count - 1; i >= 0; i--)
            _owned[i].Dispose();
        _owned.Clear();
    }
}
