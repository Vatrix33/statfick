using Statventure.Game.Sounds;
using Statventure.Game.Ui;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Sheet;

/// <summary>
/// The character's page in GM1 (Q5, Q6, Q7, Q11; Docs/Game/Foundation.md, "What the player sees in a character in GM1" and
/// "The top bar"; step 9): the old page's frame, its markup the old HTML (<c>old-game/index.html</c> 23–25 and 64–88,
/// GM1-P7) without the theme toggle and the three character buttons and with the Menu button: the top bar ("Return to
/// combat" disabled until GM5, the ✦ brand mark, inert and always lit, "Stat Fick", the world clock standing at
/// "Day 1 · 08:00" until GM4, "Autosaved locally" with its dot, Menu), the status line under it for the file messages, an
/// empty parchment page where GM2's sheet goes, and the ⚙ Settings button at the bottom right. It shows while a character
/// is open (<see cref="Session.GameSession.Opened"/>, which plays <c>game-opened</c>) and the start screen while none is;
/// the dot lights ember for 350 ms on every save of the open character (<see cref="Session.GameSession.Saved"/>, the old
/// <c>saveCharacter</c>, <c>app.js</c> 1844–1846); the status line says what <see cref="GameUi.Status"/> says.
/// </summary>
public sealed class SheetScreen : IGameScreen
{
    /// <summary>The page's own sheet (<c>Assets/UI/styles/sheet.css</c>).</summary>
    public const string Stylesheet = "ui/styles/sheet";

    /// <summary>How long the autosave dot stays lit after a save (the old <c>saveCharacter</c>'s 350 ms).</summary>
    public const double SavingMilliseconds = 350;

    /// <summary>The class the dot has while lit (the old game's).</summary>
    public const string SavingClass = "is-saving";

    /// <summary>
    /// The old markup, as <c>index.html</c> has it, less the theme toggle and Save, Load and New Character (Q6), the ✦ no
    /// longer the Dev Mode switch (Q11: a mark, not a button), with the Menu button at the top bar's right end (Q7, styled as
    /// the top bar's old buttons) and the page left empty for GM2's sheet.
    /// </summary>
    internal const string Markup = """
        <button id="settings-open-button" class="settings-button" type="button" aria-haspopup="dialog" title="Open settings" hidden>
          <span class="settings-button-icon" aria-hidden="true">⚙</span><span>Settings</span>
        </button>
        <main class="shell game-page" hidden>
          <header class="topbar">
            <button id="combat-view-toggle" class="spellbook-button combat-view-toggle" type="button" disabled>Return to combat</button>
            <div class="brand">
              <span class="brand-mark" aria-hidden="true"><span>✦</span></span>
              <span>
                <span class="brand-name">Stat Fick</span>
              </span>
            </div>
            <div class="topbar-actions">
              <span class="world-clock" id="sheet-world-clock" aria-label="World time">Day 1 · 08:00</span>
              <span class="autosave"><span class="status-dot" aria-hidden="true"></span>Autosaved locally</span>
              <button class="backup-button menu-button" id="menu-button" type="button" aria-haspopup="dialog"><span aria-hidden="true">☰</span> Menu</button>
            </div>
          </header>
          <p class="backup-status topbar-file-status" id="backup-status" role="status" aria-live="polite"></p>
          <p class="day-status" id="day-status" role="status" hidden></p>
        </main>
        """;

    GameParts? _parts;
    UiElement? _page, _settingsButton, _dot, _status;
    int _dotTimer;

    public void Attach(GameParts parts)
    {
        _parts = parts;
        var ui = parts.Ui;
        ui.UseStylesheet(Stylesheet);
        var body = ui.Document.Body;
        foreach (var node in Statventure.UI.Dom.Markup.Parse(Markup))
            body.Append(node);
        var document = ui.Document;
        _settingsButton = document.QuerySelector("#settings-open-button")!;
        _page = document.QuerySelector("main.game-page")!;
        _dot = document.QuerySelector("main.game-page .status-dot")!;
        _status = document.QuerySelector("#backup-status")!;
        _settingsButton.AddEventListener("click", _ => ui.Commands.Run("open-settings"));
        document.QuerySelector("#menu-button")!.AddEventListener("click", _ => ui.Commands.Run(GameUi.MenuCommand));
        parts.Session.Opened += Opened;
        parts.Session.Closed += Closed;
        parts.Session.Saved += Saved;
        ui.StatusShown += StatusShown;
    }

    /// <summary>Whether the page shows (a character is open).</summary>
    public bool Showing => _page is { Hidden: false };

    void Opened()
    {
        var parts = _parts!;
        _status!.TextContent = "";
        _status.ClassName = "backup-status topbar-file-status";
        _page!.Hidden = false;
        _settingsButton!.Hidden = false;
        parts.Ui.Sound(SoundNames.Opened);
    }

    void Closed()
    {
        _page!.Hidden = true;
        _settingsButton!.Hidden = true;
        _dot!.RemoveClass(SavingClass);
    }

    /// <summary>A save of the open character: the dot lit ember for 350 ms, the timer started again (the old <c>saveCharacter</c>).</summary>
    void Saved()
    {
        var document = _parts!.Ui.Document;
        _dot!.AddClass(SavingClass);
        document.ClearTimeout(_dotTimer);
        _dotTimer = document.SetTimeout(() => _dot.RemoveClass(SavingClass), SavingMilliseconds);
    }

    /// <summary>
    /// A file message on the page's status line (the old <c>setBackupStatus</c>: "success" or "error") while the page is the
    /// topmost surface: it shows and no window is open (a window's own line takes what is said while it is the topmost; the
    /// manager's rule for the status lines, 2026-11-14).
    /// </summary>
    void StatusShown(string text, StatusKind kind)
    {
        if (!Showing || _parts!.Ui.Windows.Top is not null)
            return;
        _status!.TextContent = text;
        _status.ClassName = $"backup-status topbar-file-status {(kind == StatusKind.Problem ? "is-error" : "is-success")}";
    }
}
