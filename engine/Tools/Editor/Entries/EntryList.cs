using Statventure.Editor.Session;
using Statventure.Editor.Shell;
using Statventure.UI.Dom;

namespace Statventure.Editor.Entries;

/// <summary>
/// The entry list (P8; Docs/Editor.md, "Opening packs (step 8)"), the entry pane: the open pack's name and whether it
/// takes changes, why not (each entry it can't read with its reason, M11-16), a filter, and a tree by path of the
/// session's entries (the open pack's over the game's; step 9's <see cref="EditorSession.Entries"/>): folders first,
/// then entries, each by its name; an entry with its kind's name beside it, the game's dimmed, and its marks: changed
/// (•), added (+), the mod's own copy of a game entry (↑), problems (!), unreadable (×). The tree is one stop for Tab
/// with an active item: the arrow keys, Home, End and type-ahead move it (Right and Left open and close folders),
/// Enter opens it (an entry is selected, the form shows it; a folder opens or closes), as a click does. Ctrl+F goes to
/// the filter, which keeps the entries whose path or kind's name holds its text (every folder open meanwhile).
/// <para>
/// Rows only where they are seen (P37: a pack of 2,000 entries measured first; painting every row of an open tree
/// took 109 ms a frame): the tree is a flat list of its shown items (<see cref="Visible"/>), each row
/// <see cref="RowHeight"/> high, of which only those in the pane's view and <see cref="Overscan"/> rows on either side
/// are elements, between two spacers as high as the rows they stand for; scrolling the pane brings the others in.
/// </para>
/// </summary>
public sealed class EntryList : EditorPart
{
    /// <summary>How long typed letters keep adding to the type-ahead's text (Blink's TypeAhead: one second).</summary>
    public const double TypeAheadMs = 1000;

    /// <summary>The indent of each level of the tree (CSS px).</summary>
    public const int Indent = 14;

    /// <summary>A row's height (CSS px): the shell's <c>--row</c>.</summary>
    public const int RowHeight = 24;

    /// <summary>Rows made beyond the pane's view on either side.</summary>
    public const int Overscan = 10;

    /// <summary>The rows made before the pane has been laid out.</summary>
    public const int FirstRows = 40;

    /// <summary>An item of the tree as shown: a folder (no entry) or an entry, its depth, a folder whether it is open.</summary>
    sealed record Item(string Path, string Name, int Depth, EditorEntry? Entry, bool Open);

    UiElement _element = null!;
    UiElement _pack = null!, _state = null!, _notice = null!, _filter = null!, _tree = null!;
    readonly List<Item> _items = [];
    readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);
    readonly Dictionary<string, UiElement> _rows = new(StringComparer.Ordinal);
    readonly HashSet<string> _open = new(StringComparer.Ordinal);
    int _first, _last;
    string? _active;
    string _typed = "";
    double _typedAt = double.NegativeInfinity;

    public override EditorPane? Pane => EditorPane.Entries;

    protected internal override void Attach(UiElement? element)
    {
        _element = element!;
        _element.AddClass("entries-part");
        var head = _element.Append("div", "entries-head");
        _pack = head.Append("span", "entries-pack");
        _state = head.Append("span", "entries-state");
        _notice = _element.Append("div", "entries-notice");
        _filter = _element.Append("input", "entries-filter");
        _filter.SetAttribute("type", "search");
        _filter.SetAttribute("placeholder", "Filter");
        _filter.SetAttribute("aria-label", "Filter the entries");
        _filter.SetAttribute("spellcheck", "false");
        _filter.AddEventListener("input", _ => Render());
        _filter.AddEventListener<UiKeyboardEvent>("keydown", FilterKey);
        _tree = _element.Append("div", "entries-tree");
        _tree.SetAttribute("role", "tree");
        _tree.SetAttribute("tabindex", "0");
        _tree.SetAttribute("aria-label", "Entries");
        _tree.AddEventListener<UiKeyboardEvent>("keydown", TreeKey);
        _tree.AddEventListener<UiMouseEvent>("click", Click);
        // The pane scrolls the tree: the rows in view are made as it does.
        View.Pane(EditorPane.Entries).AddEventListener("scroll", _ => Window());
        View.Document.AddEventListener("resize", _ => Window());
        Session.PackOpened += Opened;
        Session.EntriesChanged += Changed;
        Session.EntryChanged += entry =>
        {
            if (_rows.TryGetValue(entry.Path, out var row) && _index.TryGetValue(entry.Path, out var i) && _items[i].Entry == entry)
                Marks(entry, row);
        };
        Session.SelectionChanged += ShowSelection;
        Session.Changed += Head;
        View.Commands.Handle(EditorCommand.Filter, _ =>
        {
            _filter.Focus();
            _filter.Select();
            return true;
        });
        Head();
        Render();
    }

    /// <summary>The filter field.</summary>
    public UiElement Filter => _filter;

    /// <summary>The tree (its children are the rows made and the two spacers).</summary>
    public UiElement Tree => _tree;

    /// <summary>The row of <paramref name="entry"/> while it is made (shown and near the pane's view), else null.</summary>
    public UiElement? Row(EditorEntry entry) =>
        _rows.TryGetValue(entry.Path, out var row) && _index.TryGetValue(entry.Path, out var i) && _items[i].Entry == entry ? row : null;

    /// <summary>The row of <paramref name="folder"/> (a path without a separator at its end) while it is made, else null.</summary>
    public UiElement? Folder(string folder) =>
        _rows.TryGetValue(folder, out var row) && _index.TryGetValue(folder, out var i) && _items[i].Entry is null ? row : null;

    /// <summary>The path of the active item: an entry's path, or a folder's (no separator at its end); null when none.</summary>
    public string? Active => _active;

    /// <summary>Whether <paramref name="folder"/> is open (the filter opens every folder while it holds text).</summary>
    public bool IsOpen(string folder) => _filter.Value.Length > 0 || _open.Contains(folder);

    /// <summary>The paths of the items shown, top to bottom: folders and entries not inside a closed folder (made as rows or not).</summary>
    public IReadOnlyList<string> Visible() => [.. _items.Select(i => i.Path)];

    /// <summary>The range of shown items made as rows: from <c>First</c>, up to but not including <c>Last</c>.</summary>
    public (int First, int Last) Made => (_first, _last);

    void Opened()
    {
        // A new pack: its top folders open, the rest closed, the filter kept.
        _open.Clear();
        foreach (var entry in Session.Entries)
            if (entry.Path.IndexOf('/') is > 0 and var cut)
                _open.Add(entry.Path[..cut]);
        _active = null;
        View.Pane(EditorPane.Entries).ScrollTop = 0;
        Render();
    }

    void Changed(EntriesChange change)
    {
        if (change.Kind is EntriesChangeKind.Added or EntriesChangeKind.Renamed && change.Entry is { } entry)
            OpenFoldersOf(entry.Path);
        if (change.Kind == EntriesChangeKind.Renamed && change.OldPath == _active)
            _active = change.Entry!.Path;
        Render();
    }

    /// <summary>The pack's name, its state, and the notice of why it doesn't take changes (each unreadable entry with its reason).</summary>
    void Head()
    {
        var pack = Session.Pack;
        _pack.TextContent = pack?.Name ?? "No pack open";
        _state.TextContent = pack is null ? "" : pack.Writable ? (pack.IsGamePack ? "game pack" : "mod") : "read-only";
        var lines = new List<(string Text, string? Title)>();
        if (pack is null)
            lines.Add(("Open a pack with Ctrl+O, or drop a .pak file on the window.", null));
        else if (pack.ReadOnlyReason is { } reason)
            lines.Add(($"Read-only: {reason}.", null));
        if (pack is not null)
        {
            foreach (var entry in pack.Unreadable)
                lines.Add(($"{entry.Path}: {entry.Reason}", entry.Reason));
            foreach (var problem in Session.GamePackProblems)
                lines.Add(($"The game's pack {problem}", null));
        }
        var shown = _notice.ElementChildren.Select(e => e.TextContent).ToList();
        if (shown.SequenceEqual(lines.Select(l => l.Text)))
            return;
        _notice.ClearChildren();
        _notice.ToggleAttribute("hidden", lines.Count == 0);
        foreach (var (text, title) in lines)
        {
            var line = _notice.Append("div", title is null ? "entries-note" : "entries-note unreadable", text);
            if (title is not null)
                line.SetAttribute("title", title);
        }
    }

    sealed class Node
    {
        public readonly SortedDictionary<string, Node> Folders = new(StringComparer.Ordinal);
        public readonly List<EditorEntry> Entries = [];
    }

    /// <summary>The shown items made again from the session's entries (the filter applied, the open folders kept), then the rows in view.</summary>
    void Render()
    {
        _items.Clear();
        _index.Clear();
        var filter = _filter.Value.Trim();
        var root = new Node();
        foreach (var entry in Session.Entries)
        {
            if (filter.Length > 0 && !entry.Path.Contains(filter, StringComparison.OrdinalIgnoreCase)
                && !entry.KindName.Contains(filter, StringComparison.OrdinalIgnoreCase))
                continue;
            var node = root;
            var segments = entry.Path.Split('/');
            for (var i = 0; i < segments.Length - 1; i++)
            {
                if (!node.Folders.TryGetValue(segments[i], out var next))
                    node.Folders[segments[i]] = next = new Node();
                node = next;
            }
            node.Entries.Add(entry);
        }
        Flatten(root, "", 0);
        for (var i = 0; i < _items.Count; i++)
            _index[_items[i].Path] = i;
        if (_active is not null && !_index.ContainsKey(_active))
            _active = null;
        if (Session.Selected is { } selected && _index.TryGetValue(selected.Path, out var at) && _items[at].Entry == selected)
            _active ??= selected.Path;
        Window(force: true);
    }

    void Flatten(Node node, string path, int depth)
    {
        foreach (var (name, folder) in node.Folders)
        {
            var folderPath = path.Length == 0 ? name : path + "/" + name;
            var open = IsOpen(folderPath);
            _items.Add(new Item(folderPath, name, depth, null, open));
            if (open)
                Flatten(folder, folderPath, depth + 1);
        }
        foreach (var entry in node.Entries)
            _items.Add(new Item(entry.Path, entry.Path[(entry.Path.LastIndexOf('/') + 1)..], depth, entry, false));
    }

    /// <summary>
    /// Makes the rows of the shown items in the pane's view (and <see cref="Overscan"/> on either side) between two
    /// spacers, when that range changed (or <paramref name="force"/>); before the pane is laid out, the first
    /// <see cref="FirstRows"/>.
    /// </summary>
    void Window(bool force = false)
    {
        var (first, last) = (0, Math.Min(_items.Count, FirstRows));
        var pane = View.Pane(EditorPane.Entries);
        if (_tree.Document is not null)
        {
            View.Document.UpdateLayout();
            if (pane.Box is not null && _tree.Box is not null)
            {
                var top = pane.GetBoundingClientRect().Y - _tree.GetBoundingClientRect().Y;
                first = Math.Clamp((int)Math.Floor(top / RowHeight) - Overscan, 0, _items.Count);
                last = Math.Clamp((int)Math.Ceiling((top + pane.ClientHeight) / RowHeight) + Overscan, first, _items.Count);
            }
        }
        if (!force && (first, last) == (_first, _last))
            return;
        (_first, _last) = (first, last);
        _rows.Clear();
        _tree.ClearChildren();
        var above = _tree.Append("div", "entries-spacer");
        above.SetStyle("height", $"{first * RowHeight}px");
        for (var i = first; i < last; i++)
            MakeRow(_items[i]);
        var below = _tree.Append("div", "entries-spacer");
        below.SetStyle("height", $"{(_items.Count - last) * RowHeight}px");
        if (Session.Entries.Count > 0 && _items.Count == 0)
            _tree.Append("div", "entries-none", "No entry matches the filter.");
    }

    void MakeRow(Item item)
    {
        var row = _tree.Append("div", item.Entry is null ? "entries-item folder" : "entries-item entry");
        row.SetAttribute("role", "treeitem");
        row.SetAttribute("data-path", item.Path);
        row.SetAttribute("aria-level", (item.Depth + 1).ToString(System.Globalization.CultureInfo.InvariantCulture));
        row.Append("span", "name", item.Name);
        if (item.Entry is { } entry)
        {
            row.SetStyle("padding-left", $"{4 + item.Depth * Indent + Indent}px");
            row.SetAttribute("aria-selected", entry == Session.Selected ? "true" : "false");
            row.ToggleClass("selected", entry == Session.Selected);
            row.Append("span", "marks");
            row.Append("span", "kind", entry.KindName);
            Marks(entry, row);
        }
        else
        {
            row.SetStyle("padding-left", $"{4 + item.Depth * Indent}px");
            row.SetAttribute("aria-expanded", item.Open ? "true" : "false");
            row.ToggleClass("open", item.Open);
        }
        row.ToggleClass("active", item.Path == _active);
        _rows[item.Path] = row;
    }

    /// <summary>An entry's marks and look, brought up to date (after it changed).</summary>
    void Marks(EditorEntry entry, UiElement row)
    {
        var copy = Session.IsModCopy(entry);
        row.ToggleClass("game", entry.Source == EntrySource.Game && !copy);
        row.ToggleClass("unreadable", entry.Content == EntryContent.Unreadable);
        row.SetAttribute("title", entry.Reason is { } reason ? $"{entry.Path}: {reason}" : entry.Path);
        var marks = row.ElementChildren.First(c => c.HasClass("marks"));
        marks.ClearChildren();
        void Mark(string kind, string text, string title)
        {
            var mark = marks.Append("span", "mark " + kind, text);
            mark.SetAttribute("title", title);
        }
        if (copy)
            Mark("copy", "↑", "The mod's own copy of the game's entry");
        if (entry.Source == EntrySource.New)
            Mark("added", "+", "Added, not saved");
        else if (entry.IsChanged || entry.IsRenamed)
            Mark("changed", "•", "Changed, not saved");
        if (entry.Problems.Count > 0)
            Mark("problem", "!", entry.Problems.Count == 1 ? "A problem" : $"{entry.Problems.Count} problems");
        if (entry.Content == EntryContent.Unreadable)
            Mark("unreadable", "×", $"Can't be read: {entry.Reason}");
    }

    void ShowSelection()
    {
        foreach (var (path, row) in _rows)
        {
            var selected = Session.Selected is { } chosen && chosen.Path == path && _items[_index[path]].Entry == chosen;
            if (row.HasClass("selected") == selected)
                continue;
            row.ToggleClass("selected", selected);
            row.SetAttribute("aria-selected", selected ? "true" : "false");
        }
        if (Session.Selected is { } entry && Session.Entries.Contains(entry))
        {
            if (OpenFoldersOf(entry.Path))
            {
                _active = entry.Path;
                Render();
            }
            if (_index.ContainsKey(entry.Path))
                SetActive(entry.Path, reveal: true);
        }
    }

    /// <summary>Opens the folders that hold <paramref name="path"/>; gives whether any was closed (and the filter doesn't open them all).</summary>
    bool OpenFoldersOf(string path)
    {
        var opened = false;
        for (var cut = path.IndexOf('/'); cut > 0; cut = path.IndexOf('/', cut + 1))
            opened |= _open.Add(path[..cut]);
        return opened && _filter.Value.Length == 0;
    }

    /// <summary>Makes <paramref name="path"/> the active item (null: none), scrolling the pane to show it when <paramref name="reveal"/>.</summary>
    void SetActive(string? path, bool reveal)
    {
        if (_active is { } before && _rows.TryGetValue(before, out var old))
            old.RemoveClass("active");
        _active = path;
        if (path is null || !_index.TryGetValue(path, out var index))
            return;
        if (reveal)
            Reveal(index);
        if (_rows.TryGetValue(path, out var row))
            row.AddClass("active");
    }

    /// <summary>Scrolls the pane as little as shows the item's row whole (Blink's scrollIntoView nearest), then makes the rows in view.</summary>
    void Reveal(int index)
    {
        var pane = View.Pane(EditorPane.Entries);
        View.Document.UpdateLayout();
        if (pane.Box is null || _tree.Box is null)
            return;
        var view = pane.GetBoundingClientRect();
        var top = _tree.GetBoundingClientRect().Y + index * RowHeight;
        if (top < view.Y)
            pane.ScrollTop -= view.Y - top;
        else if (top + RowHeight > view.Y + pane.ClientHeight)
            pane.ScrollTop += top + RowHeight - (view.Y + pane.ClientHeight);
        Window();
    }

    /// <summary>Opens or closes a folder (its open state kept across rebuilds); the active item in a folder closed becomes the folder.</summary>
    public void Toggle(string folder, bool? open = null)
    {
        if (!_index.TryGetValue(folder, out var index) || _items[index].Entry is not null)
            return;
        var now = open ?? !_items[index].Open;
        if (now == _items[index].Open)
            return;
        if (now)
            _open.Add(folder);
        else
            _open.Remove(folder);
        if (!now && _active is { } active && active.StartsWith(folder + "/", StringComparison.Ordinal))
            _active = folder;
        Render();
    }

    /// <summary>What the active item opens: an entry is selected (the form shows it), a folder opens or closes.</summary>
    public void OpenActive()
    {
        if (_active is not { } path || !_index.TryGetValue(path, out var index))
            return;
        if (_items[index].Entry is { } entry)
            Session.Select(entry);
        else
            Toggle(path);
    }

    /// <summary>The item a press is on (a row), else null.</summary>
    static UiElement? ItemOf(UiEvent e)
    {
        for (var at = e.Target as UiElement; at is not null; at = at.Parent)
            if (at.HasClass("entries-item"))
                return at;
        return null;
    }

    /// <summary>A click on a row (a press focuses the tree, the rows being no stops of their own): the item active and opened.</summary>
    void Click(UiMouseEvent e)
    {
        if (e.Button != 0 || ItemOf(e) is not { } item || item.GetAttribute("data-path") is not { } path)
            return;
        SetActive(path, reveal: false);
        OpenActive();
    }

    void TreeKey(UiKeyboardEvent e)
    {
        if (e.CtrlKey || e.AltKey || e.MetaKey)
            return;
        var index = _active is not null && _index.TryGetValue(_active, out var at) ? at : -1;
        string? To(int i) => _items.Count == 0 ? null : _items[Math.Clamp(i, 0, _items.Count - 1)].Path;
        var folder = index >= 0 && _items[index].Entry is null;
        switch (e.Key)
        {
            case "ArrowDown":
                SetActive(To(index + 1), reveal: true);
                break;
            case "ArrowUp":
                SetActive(To(index < 0 ? _items.Count - 1 : index - 1), reveal: true);
                break;
            case "Home":
                SetActive(To(0), reveal: true);
                break;
            case "End":
                SetActive(To(_items.Count - 1), reveal: true);
                break;
            case "ArrowRight":
                if (folder && !_items[index].Open)
                    Toggle(_active!, true);
                else if (folder)
                    // An open folder's next item is its first (a folder is there only for what it holds).
                    SetActive(To(index + 1), reveal: true);
                break;
            case "ArrowLeft":
                if (folder && _items[index].Open && _filter.Value.Length == 0)
                    Toggle(_active!, false);
                else if (_active?.LastIndexOf('/') is > 0 and var cut && _index.ContainsKey(_active[..cut]))
                    SetActive(_active[..cut], reveal: true);
                break;
            case "Enter":
                OpenActive();
                break;
            default:
                if (e.Key.Length == 1 && !char.IsControl(e.Key[0]) && !(e.Key == " " && _typed.Length == 0))
                {
                    TypeAhead(e.Key, index);
                    break;
                }
                return;
        }
        e.PreventDefault();
    }

    /// <summary>
    /// Type-ahead (as Blink's list box's): letters typed within <see cref="TypeAheadMs"/> of each other make a text; the
    /// next item from the active one (from the one after it for a first letter) whose name starts with it, ignoring
    /// case, going round, becomes active.
    /// </summary>
    void TypeAhead(string key, int index)
    {
        var now = View.Document.EventLoop.Now;
        _typed = now - _typedAt > TypeAheadMs ? key : _typed + key;
        _typedAt = now;
        if (_items.Count == 0)
            return;
        var start = _typed.Length == 1 ? index + 1 : Math.Max(index, 0);
        for (var n = 0; n < _items.Count; n++)
        {
            var item = _items[(start + n) % _items.Count];
            if (item.Name.StartsWith(_typed, StringComparison.OrdinalIgnoreCase))
            {
                SetActive(item.Path, reveal: true);
                return;
            }
        }
    }

    void FilterKey(UiKeyboardEvent e)
    {
        if (e.CtrlKey || e.AltKey || e.MetaKey || e.ShiftKey)
            return;
        if (e.Key == "Escape" && _filter.Value.Length > 0)
        {
            _filter.Value = "";
            Render();
            e.PreventDefault();
        }
        else if (e.Key == "ArrowDown")
        {
            _tree.Focus();
            if (_active is null)
                SetActive(_items.FirstOrDefault()?.Path, reveal: true);
            e.PreventDefault();
        }
    }
}
