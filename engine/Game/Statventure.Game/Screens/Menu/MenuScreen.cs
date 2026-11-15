using Statventure.Core;
using Statventure.Game.Ui;
using Statventure.Platform;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Menu;

/// <summary>An item of the in-game menu: its words, the command it runs, and whether it needs an open character.</summary>
/// <param name="Label">Its words (the old menu's, Electron's roles' for the View items and Quit).</param>
/// <param name="Command">The command it runs.</param>
/// <param name="NeedsCharacter">Left out while no character is open (on the start screen).</param>
public sealed record MenuItem(string Label, string Command, bool NeedsCharacter = false);

/// <summary>
/// The in-game menu (Q7, Q8, Q18; Docs/Game/Foundation.md, "The in-game menu"; step 9) in place of the old Electron menu
/// bar (<c>old-game/desktop/main.cjs</c> 80–89; Decisions for M5): a window in the old dialogs' look (a <c>.modal-layer</c>
/// with its backdrop, a dialog with a leather header and its × close) opened by the top bar's Menu button and by Escape
/// when no window is open (the command <c>open-menu</c>). Its items, the old menu's in its order and words, Reload left
/// out (Q18), Settings and Start screen added (Q8): Save Character, Load Character, Open game folder, Settings, Start
/// screen; Actual Size, Zoom In, Zoom Out, Toggle Full Screen (<see cref="MenuRoles"/>); About Stat Fick; Quit ("Exit" on
/// Windows). Each shows the keys bound to its command (<see cref="GameCommands.KeysOf"/>). On the start screen the items
/// that need an open character (Save Character, Start screen) are left out. An item closes the menu, then runs its
/// command (one not registered yet is logged and does nothing).
/// </summary>
public sealed class MenuScreen : IGameScreen
{
    /// <summary>The menu's own sheet (<c>Assets/UI/styles/menu.css</c>).</summary>
    public const string Stylesheet = "ui/styles/menu";

    /// <summary>The command that opens it.</summary>
    public const string OpenCommand = GameUi.MenuCommand;

    /// <summary>The menu's items in their groups, for the system the game follows (<paramref name="windows"/>, else this one's).</summary>
    public static IReadOnlyList<IReadOnlyList<MenuItem>> Items(bool? windows = null) =>
    [
        [
            new("Save Character", "save-character", NeedsCharacter: true),
            new("Load Character", "load-character"),
            new("Open game folder", "open-game-folder"),
            new("Settings", "open-settings"),
            new("Start screen", StatFickGame.StartCommand, NeedsCharacter: true),
        ],
        [.. new[] { MenuRoles.ResetZoom, MenuRoles.ZoomIn, MenuRoles.ZoomOut, MenuRoles.ToggleFullScreen }.Select(r => new MenuItem(r.Label, r.Command))],
        [new("About Stat Fick", "about")],
        [new(MenuRoles.Quit(windows ?? PlatformBehaviour.Windows).Label, "quit")],
    ];

    /// <summary>The menu's window: the old dialogs' layer, its header ("STAT FICK", "Game menu") and × close; the items are put in at each opening.</summary>
    internal const string Markup = """
        <div class="modal-layer game-menu-layer" id="game-menu">
          <button class="modal-backdrop" type="button" aria-label="Close the menu"></button>
          <section class="spell-picker-dialog game-menu-dialog" role="dialog" aria-modal="true" aria-labelledby="game-menu-title">
            <header class="dialog-header compact-dialog-header">
              <div><p class="eyebrow">STAT FICK</p><h2 id="game-menu-title">Game <em>menu</em></h2></div>
              <button class="modal-close" type="button" aria-label="Close the menu">×</button>
            </header>
            <nav class="game-menu-content" aria-label="Menu"></nav>
          </section>
        </div>
        """;

    GameParts? _parts;
    UiElement? _layer, _content;

    /// <summary>The menu's window (open or not).</summary>
    public UiElement Layer => _layer!;

    public void Attach(GameParts parts)
    {
        _parts = parts;
        var ui = parts.Ui;
        ui.UseStylesheet(Stylesheet);
        _layer = Statventure.UI.Dom.Markup.Element(Markup);
        _content = Find(_layer, ".game-menu-content");
        Find(_layer, ".modal-backdrop").AddEventListener("click", _ => ui.Windows.Close(_layer));
        Find(_layer, ".modal-close").AddEventListener("click", _ => ui.Windows.Close(_layer));
        _layer.AddEventListener<UiKeyboardEvent>("keydown", Arrows);
        ui.Commands.Register(OpenCommand, _ => Open());
    }

    /// <summary>
    /// <c>open-menu</c>: the menu with the items this moment has (those that need a character left out while none is
    /// open), the focus on its first item. Nothing when it is open already.
    /// </summary>
    void Open()
    {
        var ui = _parts!.Ui;
        if (ui.Windows.IsOpen(_layer!))
            return;
        var character = _parts.Session.Current is not null;
        _content!.TextContent = "";
        UiElement? first = null;
        foreach (var group in Items())
        {
            // No group is all items needing a character, so every group shows.
            var shown = group.Where(item => character || !item.NeedsCharacter).ToList();
            var list = _content.Append("div", "game-menu-group");
            foreach (var item in shown)
            {
                var button = list.Append("button", "game-menu-item");
                button.SetAttribute("type", "button");
                button.SetAttribute("data-command", item.Command);
                button.Append("span", "game-menu-label", item.Label);
                if (ui.Commands.KeysOf(item.Command) is { } keys)
                    button.Append("kbd", "game-menu-keys", keys);
                button.AddEventListener("click", _ => Choose(item));
                first ??= button;
            }
        }
        ui.Windows.Open(_layer!, first);
        Log.Info("Game", $"the menu opened ({(character ? "a character open" : "the start screen")})");
    }

    /// <summary>An item chosen: the menu closed, then its command run.</summary>
    void Choose(MenuItem item)
    {
        var ui = _parts!.Ui;
        ui.Windows.Close(_layer!);
        ui.Commands.Run(item.Command);
    }

    /// <summary>Up and Down move among the items, round from the last to the first, as a menu's (Home and End to the ends).</summary>
    void Arrows(UiKeyboardEvent e)
    {
        if (e.CtrlKey || e.AltKey || e.MetaKey || e.ShiftKey || e.DefaultPrevented)
            return;
        var items = _content!.Descendants().Where(e => e.HasClass("game-menu-item")).ToList();
        if (items.Count == 0)
            return;
        var at = items.IndexOf(_parts!.Ui.Document.FocusedElement!);
        var next = e.Key switch
        {
            "ArrowDown" => at < 0 ? 0 : (at + 1) % items.Count,
            "ArrowUp" => at < 0 ? items.Count - 1 : (at - 1 + items.Count) % items.Count,
            "Home" => 0,
            "End" => items.Count - 1,
            _ => -1,
        };
        if (next < 0)
            return;
        items[next].Focus();
        e.PreventDefault();
    }

    static UiElement Find(UiElement root, string selector) => root.Descendants().First(e => e.Matches(selector));
}
