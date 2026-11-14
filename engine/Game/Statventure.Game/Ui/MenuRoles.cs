using Statventure.Platform;

namespace Statventure.Game.Ui;

/// <summary>
/// An item of the old Electron menu that was one of Electron's roles: its command in the game, its label and its keys as
/// Electron 44.5.0 gives them on this system (<see cref="MenuRoles"/>).
/// </summary>
/// <param name="Command">The game's command (<see cref="GameCommands"/>).</param>
/// <param name="Label">The item's words.</param>
/// <param name="Keys">The keys, as Electron's accelerator names them with CommandOrControl as Ctrl ("Ctrl+Plus"); null when the role has none on this system.</param>
public sealed record MenuRole(string Command, string Label, string? Keys);

/// <summary>
/// The old menu's role items (<c>old-game/desktop/main.cjs</c> 80–89: View › <c>reload</c>, <c>resetZoom</c>, <c>zoomIn</c>,
/// <c>zoomOut</c>, <c>togglefullscreen</c>; File › <c>quit</c>) with the labels and accelerators Electron 44.5.0's
/// <c>lib/browser/api/menu-item-roles.ts</c> gives them (saved in <c>~/.cache/stat-fick-build/reference-src/electron-44.5.0/</c>;
/// Docs/Game/Foundation.md, "How it works", step 8), Reload left out (Q18). On Linux: Actual Size Ctrl+0, Zoom In Ctrl+Plus,
/// Zoom Out Ctrl+-, Toggle Full Screen F11, Quit Ctrl+Q; on Windows Quit is "Exit" with no keys. Electron's accelerator
/// parser (<c>shell/browser/ui/accelerator_util.cc</c>, <c>shell/common/keyboard_util.cc</c>) makes "Plus" the =/+ key
/// with Shift (VKEY_OEM_PLUS and a shifted '+'), so Ctrl+Plus is Ctrl+Shift+= on a US keyboard; the zoom roles move
/// Electron's zoom level by half a step (<c>webContents.zoomLevel += 0.5</c>) and Actual Size sets it to 0.
/// </summary>
public static class MenuRoles
{
    public static MenuRole ResetZoom { get; } = new("zoom-reset", "Actual Size", "Ctrl+0");
    public static MenuRole ZoomIn { get; } = new("zoom-in", "Zoom In", "Ctrl+Plus");
    public static MenuRole ZoomOut { get; } = new("zoom-out", "Zoom Out", "Ctrl+-");
    public static MenuRole ToggleFullScreen { get; } = new("fullscreen", "Toggle Full Screen", "F11");

    /// <summary>Quit: "Quit" with Ctrl+Q on Linux, "Exit" with no keys on Windows (Electron's <c>quit</c> role).</summary>
    public static MenuRole Quit(bool windows) => windows ? new("quit", "Exit", null) : new("quit", "Quit", "Ctrl+Q");

    /// <summary>The View menu's roles in its order, then Quit, for the system the game follows (<see cref="PlatformBehaviour.Windows"/> by default).</summary>
    public static IReadOnlyList<MenuRole> All(bool? windows = null) => [ResetZoom, ZoomIn, ZoomOut, ToggleFullScreen, Quit(windows ?? PlatformBehaviour.Windows)];
}
