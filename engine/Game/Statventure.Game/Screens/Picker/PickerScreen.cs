using Statventure.Core;
using Statventure.Game.Session;
using Statventure.Game.Sounds;
using Statventure.Game.Ui;
using Statventure.Storage;

namespace Statventure.Game.Screens.Picker;

/// <summary>
/// Save Character and Load Character through the in-game file picker (Q9, Q23, Q24, GM1-P15; Docs/Game/Foundation.md,
/// "Save Character and Load Character"; step 10), in place of the old native Save and Open dialogs (<c>main.cjs</c> 46–62,
/// titled "Save Character" and "Load Character", their filter "Stat Fick character", <c>.json</c>); its commands
/// <c>save-character</c> (Ctrl+S; while a character is open) and <c>load-character</c> (Ctrl+O; the slot list's "Load
/// Character…"; with a file as its argument, that file loaded without the picker, GM1-P20). Save starts in the Documents folder (<see cref="DocumentsFolder"/>) with
/// <see cref="CharacterFile.DefaultName"/> in the name field, replaces a file only after a question, writes the open
/// character's JSON form (<see cref="GameSession.Export"/>) and says "Character saved" or "Character could not be saved"
/// (the old texts). Load starts in the most recent place (else Documents) and loads the file into a new slot, which opens
/// (<see cref="GameSession.Import"/>): "Loaded &lt;file&gt;", or the session's problem ("That file is not a valid
/// character save", Q23). The picker closes before the status is said (<see cref="GameUi.Status"/>), so it shows where
/// the action was taken: the page's status line, or the slot list's. The folder of a file picked becomes the first of the
/// recent places (<c>GameSettings.AddRecentPlace</c>).
/// </summary>
public sealed class PickerScreen : IGameScreen, IPickerHost
{
    /// <summary>The commands (Ctrl+S, Ctrl+O; bound by the game's document, step 8).</summary>
    public const string SaveCommand = "save-character", LoadCommand = "load-character";

    /// <summary>The screen's sheet in <c>ui.pak</c>.</summary>
    public const string Stylesheet = "ui/styles/picker";

    /// <summary>The old game's status texts (<c>app.js</c> 1866–1899, 1975–1990).</summary>
    public const string SavedText = "Character saved", NotSavedText = "Character could not be saved";

    /// <summary>"Loaded &lt;file&gt;" (the old <c>importCharacterText</c>'s, the file's name).</summary>
    public static string LoadedText(string file) => $"Loaded {Path.GetFileName(file)}";

    /// <summary>The question before a file that is there is replaced.</summary>
    public static string ReplaceQuestion(string file) => $"{Path.GetFileName(file)} is already in this folder. Replace it?";

    GameParts? _parts;

    /// <summary>The picker while it is open, for the tests.</summary>
    internal FilePicker? Open { get; private set; }

    /// <summary>The Documents folder (<see cref="DocumentsFolder.ForThisMachine"/>; in the tests the run's scratch folder, or a test's own).</summary>
    internal Func<string?> Documents { get; set; } = ThisMachinesDocuments;

    /// <summary>The home folder (a test's own in place of it).</summary>
    internal Func<string?> Home { get; set; } = () => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile, Environment.SpecialFolderOption.DoNotVerify);

    /// <summary>The roots (<see cref="FolderListing.Roots"/>; a test's own in place of them).</summary>
    internal Func<IReadOnlyList<string>> Roots { get; set; } = FolderListing.Roots;

    /// <summary>A folder's listing (<see cref="FileBrowser.List"/> with <see cref="FilePicker.Endings"/>; a test's own, so no path of the machine is in a screen).</summary>
    internal Func<string, IReadOnlyList<FolderEntry>> Listing { get; set; } = folder => FileBrowser.List(folder, FilePicker.Endings);

    /// <summary>Whether a folder is there (a recent place left out when it isn't; a test's own with its own listing).</summary>
    internal Func<string, bool> FolderIsThere { get; set; } = Directory.Exists;

    /// <summary>Whether a file is there (saving over one asks first; a test's own with its own listing).</summary>
    internal Func<string, bool> FileIsThere { get; set; } = File.Exists;

    /// <summary>The time zone the dates are written in (the player's; a test's own).</summary>
    public TimeZoneInfo TimeZone { get; internal set; } = TimeZoneInfo.Local;

    public void Attach(GameParts parts)
    {
        _parts = parts;
        parts.Ui.UseStylesheet(Stylesheet);
        parts.Ui.Commands.Register(SaveCommand, _ => Run(Save), () => parts.Session.Current is not null);
        // With a file (--run load-character=<file>, GM1-P20): that file loaded as one the picker gave, no picker shown (a
        // path from the program's folder is read from there; one that can't be a path is the session's to refuse).
        parts.Ui.Commands.Register(LoadCommand, file => Run(file is null ? Load : () =>
        {
            LoadFile(file);
            return Task.CompletedTask;
        }));
    }

    /// <summary>Runs <paramref name="action"/> as a task of the document's loop, so what follows the picker runs there.</summary>
    void Run(Func<Task> action) => _parts!.Ui.Document.RunScript(() => _ = action());

    static string? ThisMachinesDocuments()
    {
        try
        {
            return DocumentsFolder.ForThisMachine();
        }
        catch (Exception problem) when (problem is IOException or InvalidOperationException)
        {
            Log.Warning("Picker", $"the Documents folder: {problem.Message}");
            return null;
        }
    }

    /// <summary>
    /// Save Character: the picker in Documents with <see cref="CharacterFile.DefaultName"/>; the open character written to
    /// the file picked, "Character saved" (<c>game-saved</c>) or "Character could not be saved" (<c>ui-refused</c>).
    /// </summary>
    async Task Save()
    {
        var parts = _parts!;
        if (Open is not null || parts.Session.Current is null)
            return;
        var file = await Pick(PickerMode.Save, Documents(), CharacterFile.DefaultName);
        if (file is null)
            return;
        RememberPlace(file);
        if (parts.Session.Export(file))
        {
            parts.Ui.Status(SavedText);
            parts.Ui.Sound(SoundNames.Saved);
        }
        else
        {
            parts.Ui.Status(NotSavedText, StatusKind.Problem);
            parts.Ui.Sound(SoundNames.Refused);
        }
    }

    /// <summary>Load Character: the picker in the most recent place (else Documents); the file picked loaded (<see cref="LoadFile"/>).</summary>
    async Task Load()
    {
        var parts = _parts!;
        if (Open is not null)
            return;
        var start = parts.Settings.RecentPlaces.FirstOrDefault(FolderIsThere) ?? Documents();
        var file = await Pick(PickerMode.Load, start, "");
        if (file is null)
            return;
        LoadFile(file);
    }

    /// <summary>
    /// The character file <paramref name="file"/> loaded, picked or given to <c>load-character</c>: its folder the first
    /// recent place; its character a new slot that opens, "Loaded &lt;file&gt;" (the page that follows the session's
    /// <c>Opened</c> plays <c>game-opened</c>, step 9's), or the session's problem (<c>ui-refused</c>).
    /// </summary>
    void LoadFile(string file)
    {
        var parts = _parts!;
        RememberPlace(file);
        var result = parts.Session.Import(file);
        if (result.Slot is not null)
            parts.Ui.Status(LoadedText(file));
        else
        {
            parts.Ui.Status(result.Problem ?? GameSession.NotACharacterSave, StatusKind.Problem);
            parts.Ui.Sound(SoundNames.Refused);
        }
    }

    /// <summary>The picker as a window: the file picked, or null; closed before this finishes.</summary>
    async Task<string?> Pick(PickerMode mode, string? start, string name)
    {
        var parts = _parts!;
        var picker = new FilePicker(this, mode, start, name);
        Open = picker;
        parts.Ui.Windows.Open(picker.Layer, picker.Focus, picker.Cancel);
        picker.Shown();
        if (mode == PickerMode.Save)
            picker.SelectName();
        // Closed by anything else (another part closing the topmost window): cancelled.
        void Closed(UI.Dom.UiElement layer)
        {
            if (layer == picker.Layer)
                picker.Cancel();
        }
        parts.Ui.Windows.Closed += Closed;
        Log.Info("Picker", $"the file picker opened to {mode.ToString().ToLowerInvariant()} in {picker.Folder}");
        string? file;
        try
        {
            file = await picker.Picked;
        }
        finally
        {
            parts.Ui.Windows.Closed -= Closed;
            Open = null;
            parts.Ui.Windows.Close(picker.Layer);
        }
        return file;
    }

    /// <summary>The folder of a file picked (or given) becomes the first recent place.</summary>
    void RememberPlace(string file)
    {
        if (Path.GetDirectoryName(file) is { Length: > 0 } folder)
            _parts!.Settings.AddRecentPlace(folder);
    }

    // ---------- IPickerHost ----------

    /// <summary>Documents, home, the roots, then the recent places that are folders now (the engine's model's order, without its mods folder).</summary>
    public IReadOnlyList<PickerPlace> Places()
    {
        var places = new List<PickerPlace>();
        if (Documents() is { Length: > 0 } documents)
            places.Add(new(BrowserPlaceKind.Documents, Path.GetFullPath(documents), "Documents"));
        if (Home() is { Length: > 0 } home)
            places.Add(new(BrowserPlaceKind.Home, Path.GetFullPath(home), "Home"));
        places.AddRange(Roots().Select(root => new PickerPlace(BrowserPlaceKind.Root, root, root)));
        foreach (var folder in _parts!.Settings.RecentPlaces.Where(FolderIsThere))
            places.Add(new(BrowserPlaceKind.Recent, folder, Path.GetFileName(Path.TrimEndingDirectorySeparator(folder)) is { Length: > 0 } leaf ? leaf : folder));
        return places;
    }

    public IReadOnlyList<FolderEntry> List(string folder) => Listing(folder);

    public bool FolderExists(string folder) => FolderIsThere(folder);

    public bool FileExists(string file) => FileIsThere(file);

    public Task<bool> Replace(string file) => _parts!.Ui.Dialogs.Confirm(ReplaceQuestion(file), "Replace");
}
