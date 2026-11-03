using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json.Nodes;
using Statventure.Core;

namespace Statventure.Storage;

/// <summary>
/// The per-user game folder (Docs/Assets.md, "The game folder: saves, settings, character files"):
/// <c>settings.json</c>, the save slots in <c>saves/</c> and the mods in <c>mods/</c>, side by side,
/// made on first use. One background writer writes the saves and settings; <see cref="Flush"/> (and
/// <see cref="Dispose"/>) waits for what is pending, which the game does when it quits (the writer
/// thread also keeps the process from ending before then).
/// </summary>
public sealed class GameStorage : IDisposable
{
    public const string SettingsName = "settings.json";
    public const string SavesName = "saves";
    public const string ModsName = "mods";

    readonly BackgroundWriter _writer = new();

    /// <summary>A game folder at <paramref name="folder"/> (tests use one of their own).</summary>
    public GameStorage(string folder)
    {
        Folder = Path.GetFullPath(folder);
        Saves = new SaveSlots(Path.Combine(Folder, SavesName), _writer);
        _writer.Failed += (file, error) => WriteFailed?.Invoke(file, error);
    }

    /// <summary>This machine's game folder (<see cref="GameFolder.ForThisMachine"/>).</summary>
    public static GameStorage ForThisMachine() => new(GameFolder.ForThisMachine());

    public string Folder { get; }

    /// <summary>The save slots, in <c>saves/</c>.</summary>
    public SaveSlots Saves { get; }

    /// <summary>The settings file.</summary>
    public string SettingsFile => Path.Combine(Folder, SettingsName);

    /// <summary>The mods folder (the virtual file system mounts the packs in it; not made here).</summary>
    public string ModsFolder => Path.Combine(Folder, ModsName);

    /// <summary>A save or settings file couldn't be written (raised on the writer thread; also logged): the file and the error. The next request for it tries again.</summary>
    public event Action<string, Exception>? WriteFailed;

    internal BackgroundWriter Writer => _writer;

    /// <summary>Tests: launches the file manager instead of starting a process; gives whether it launched.</summary>
    internal Func<ProcessStartInfo, bool>? Launcher { get; set; }

    /// <summary>
    /// Reads the settings (see <see cref="Settings"/>): <paramref name="version"/> is the game's current
    /// settings version, <paramref name="upgrades"/> the upgrade of each past version to the next.
    /// </summary>
    public Settings LoadSettings(int version, IReadOnlyDictionary<int, Action<JsonObject>>? upgrades = null)
    {
        _writer.WaitFor(SettingsFile);
        return Settings.Load(SettingsFile, version, upgrades, _writer);
    }

    /// <summary>Waits until every pending save and settings change is written.</summary>
    public void Flush() => _writer.Flush();

    /// <summary>
    /// Opens the game folder in the system's file manager (making it first), as the old game's
    /// "Open game folder" (Electron's <c>shell.openPath</c>): on Linux <c>xdg-open</c> on the folder,
    /// from its parent folder with <c>MM_NOTTTY=1</c>, not waited for; on Windows the shell's "open"
    /// (Explorer). Gives whether the file manager was launched (a failure is logged).
    /// </summary>
    public bool OpenInFileManager()
    {
        StorageFiles.CreateFolder(Folder);
        var start = FileManagerStart(StorageFiles.ThisSystem, Folder);
        try
        {
            if (Launcher != null)
                return Launcher(start);
            using var process = Process.Start(start);
            return true;
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or PlatformNotSupportedException)
        {
            Log.Warning("Storage", $"the file manager couldn't be opened on {Folder}: {e.Message}");
            return false;
        }
    }

    /// <summary>How the file manager is started on <paramref name="folder"/> on each system.</summary>
    public static ProcessStartInfo FileManagerStart(StorageSystem system, string folder)
    {
        if (system == StorageSystem.Windows)
            return new ProcessStartInfo(folder) { UseShellExecute = true, Verb = "open" };
        var start = new ProcessStartInfo("xdg-open")
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(folder.TrimEnd('/')) is { Length: > 0 } parent ? parent : "/",
        };
        start.ArgumentList.Add(folder);
        // As Electron: xdg-open may fall back on mailcap, whose command might want a terminal.
        start.Environment["MM_NOTTTY"] = "1";
        return start;
    }

    /// <summary>Flushes, then refuses new saves and settings changes.</summary>
    public void Dispose() => _writer.Dispose();
}
