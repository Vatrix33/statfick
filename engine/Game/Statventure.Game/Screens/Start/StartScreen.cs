using Statventure.Core;
using Statventure.Game.Data.Saves;
using Statventure.Game.Mods;
using Statventure.Game.Session;
using Statventure.Game.Settings;
using Statventure.Game.Ui;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Start;

/// <summary>
/// The start screen (GM1-2, Q2, Q10, Q16, Q16b; Docs/Game/Foundation.md, "The start screen" and "New character"; step 9):
/// the game's name and Continue, New character, Load, Settings, Quit and About in the old game's dialogs' look (a dialog's
/// leather header over its parchment, the brass-edged buttons), Continue greyed while there is nothing to continue
/// (<see cref="GameSession.ContinueSlot"/>), a line naming the mods left out at this start (<see cref="GameMods.LeftOut"/>)
/// and a status line of its own for the file messages taken from it (Ctrl+O, Escape's menu). It shows while no character
/// is open: at the start (<c>start-screen</c>), and when the open one is left (<see cref="GameSession.Closed"/>). Its
/// commands: <c>start-screen</c> (the open character left, saved, and this screen shown), <c>continue</c> (the last
/// character opened, or step 10's damaged-save question through <c>open-slot</c>) and <c>new-character</c> (the name asked,
/// "Character &lt;n&gt;" filled in, a name the save's check refuses refused in the field's bubble, then the character made
/// and opened).
/// </summary>
public sealed class StartScreen : IGameScreen
{
    /// <summary>The screen's own sheet (<c>Assets/UI/styles/start.css</c>).</summary>
    public const string Stylesheet = "ui/styles/start";

    /// <summary>The question New character asks (Q2).</summary>
    public const string NameQuestion = "Name your character";

    /// <summary>The prompt's OK.</summary>
    public const string NameOk = "OK";

    /// <summary>The start screen's markup: the dialog's look (a leather header, the parchment), its six buttons in Q16's order.</summary>
    internal const string Markup = """
        <section class="start-screen" id="start-screen" aria-labelledby="start-title" hidden>
          <div class="spell-picker-dialog start-dialog">
            <header class="dialog-header start-header">
              <div class="brand start-brand">
                <span class="brand-mark" aria-hidden="true"><span>✦</span></span>
                <div><p class="eyebrow">RPG / PERSONAL RECORD</p><h2 id="start-title">Stat Fick</h2></div>
              </div>
            </header>
            <div class="start-content">
              <nav class="start-actions" aria-label="Start">
                <button class="backup-button" id="start-continue" type="button" data-command="continue">Continue</button>
                <button class="backup-button new-character-button" id="start-new" type="button" data-command="new-character">New character</button>
                <button class="backup-button" id="start-load" type="button" data-command="open-slots">Load</button>
                <button class="backup-button" id="start-settings" type="button" data-command="open-settings">Settings</button>
                <button class="backup-button" id="start-quit" type="button" data-command="quit">Quit</button>
                <button class="backup-button" id="start-about" type="button" data-command="about">About</button>
              </nav>
              <p class="start-left-out" id="start-left-out" hidden></p>
              <p class="backup-status start-status" id="start-status" role="status" aria-live="polite"></p>
            </div>
          </div>
        </section>
        """;

    GameParts? _parts;
    UiElement? _screen, _continue, _leftOut, _status;

    public void Attach(GameParts parts)
    {
        _parts = parts;
        var ui = parts.Ui;
        ui.UseStylesheet(Stylesheet);
        foreach (var node in Statventure.UI.Dom.Markup.Parse(Markup))
            ui.Document.Body.Append(node);
        var document = ui.Document;
        _screen = document.QuerySelector("#start-screen")!;
        _continue = document.QuerySelector("#start-continue")!;
        _leftOut = document.QuerySelector("#start-left-out")!;
        _status = document.QuerySelector("#start-status")!;
        foreach (var button in document.QuerySelectorAll("#start-screen button[data-command]"))
        {
            var command = button.GetAttribute("data-command")!;
            button.AddEventListener("click", _ => ui.Commands.Run(command));
        }
        ui.Commands.Register(StatFickGame.StartCommand, _ => GoToStart());
        ui.Commands.Register("continue", _ => Continue(), () => parts.Session.ContinueSlot() is not null);
        // Run as a script of the document, so the dialog's answer continues on its event loop wherever the command came from.
        ui.Commands.Register("new-character", argument => document.RunScript(() => _ = NewCharacter()));
        // Left for another character (New, Load Character) the session opens the next one in the same call: the screen is
        // shown once the action is done, when no character is open then.
        parts.Session.Closed += () => ui.Document.QueueMicrotask(Show);
        parts.Session.Opened += Hide;
        parts.Settings.Changed += key =>
        {
            if (key == SettingKey.LastCharacter)
                UpdateContinue();
        };
        ui.StatusShown += StatusShown;
    }

    /// <summary>Whether the start screen shows (no character is open).</summary>
    public bool Showing => _screen is { Hidden: false };

    /// <summary>Where the line sends the player (the settings screen's mod list says why each was left out).</summary>
    public const string LeftOutWhere = "Settings › Mods tells why.";

    /// <summary>
    /// The line naming the mods left out at this start (Q10), or null when none was: how many and their names (up to three,
    /// then how many more), then <see cref="LeftOutWhere"/>.
    /// </summary>
    public static string? LeftOutLine(IReadOnlyList<string> names) => LeftOutNames(names) is { } named ? $"{named} {LeftOutWhere}" : null;

    /// <summary>The line's first sentence: "1 mod was left out: Ember Rite.", "2 mods were left out: A and B.", "5 mods were left out: A, B, C and 2 more."</summary>
    static string? LeftOutNames(IReadOnlyList<string> names)
    {
        if (names.Count == 0)
            return null;
        var shown = names.Count <= 3 ? names : [.. names.Take(3), $"{names.Count - 3} more"];
        var list = shown.Count == 1 ? shown[0] : string.Join(", ", shown.Take(shown.Count - 1)) + " and " + shown[^1];
        var count = names.Count == 1 ? "1 mod was" : $"{names.Count} mods were";
        return $"{count} left out: {list}.";
    }

    /// <summary>
    /// The words the name prompt's bubble says for a name the save's check refuses (<see cref="GameSession.NameProblem"/>),
    /// or null for a good name: the empty name, the one a player meets, and the two the field's limits all but keep out.
    /// </summary>
    public static string? NameRefusal(string name)
    {
        if (GameSession.NameProblem(name) is null)
            return null;
        if (name.Trim().Length == 0)
            return "Your character needs a name.";
        if (name.Length > SaveGame.MaxNameLength)
            return $"A name can be at most {SaveGame.MaxNameLength} characters long.";
        return "A name can't hold a tab or another control character.";
    }

    /// <summary><c>start-screen</c>: the open character left (saved), which shows this screen; with none open, shown now.</summary>
    void GoToStart()
    {
        _parts!.Session.Leave();
        Show();
    }

    /// <summary>
    /// The screen shown when no character is open and it isn't showing: its status line emptied, Continue as the slots
    /// say, the mods left out at this start named, the focus on the first button that can act (Continue, else New
    /// character), as a window takes it. Logged with what it shows (GM1-P21): Continue's state from the slot the button
    /// was given, the left-out line's text as the line holds it.
    /// </summary>
    void Show()
    {
        if (_parts!.Session.Current is not null || !_screen!.Hidden)
            return;
        _status!.TextContent = "";
        _status.ClassName = "backup-status start-status";
        var continueSlot = UpdateContinue();
        // The two sentences, the second kept whole on its line (start.css).
        var named = LeftOutNames([.. _parts.Mods.LeftOut.Select(mod => mod.Name)]);
        _leftOut!.TextContent = named is null ? "" : named + " ";
        if (named is not null)
            _leftOut.Append("span", "start-left-out-where", LeftOutWhere);
        _leftOut.Hidden = named is null;
        _screen.Hidden = false;
        Log.Info("Game", "the start screen shown");
        Log.Info("Game", ContinueLine(continueSlot));
        if (!_leftOut.Hidden)
            Log.Info("Game", $"the left-out line: {_leftOut.TextContent}");
        _parts.Ui.Document.UpdateLayout();
        _parts.Ui.Document.QuerySelectorAll("#start-screen button").FirstOrDefault(b => b.IsFocusable)?.Focus();
    }

    void Hide()
    {
        if (_screen is { } screen)
            screen.Hidden = true;
    }

    /// <summary>Continue greyed (disabled) while there is nothing to continue (Q16b); the slot it opens (null: greyed).</summary>
    string? UpdateContinue()
    {
        var slot = _parts!.Session.ContinueSlot();
        if (slot is null)
            _continue!.SetAttribute("disabled", "");
        else
            _continue!.RemoveAttribute("disabled");
        return slot;
    }

    /// <summary>The log's line for Continue as the button was given it: "Continue greyed", or "Continue opens &lt;slot&gt;".</summary>
    static string ContinueLine(string? slot) => slot is null ? "Continue greyed" : $"Continue opens {slot}";

    /// <summary>
    /// <c>continue</c>: the last character's slot opened; when its save can't be read, step 10's <c>open-slot</c> asks the
    /// damaged-save question about it ("Opening a character").
    /// </summary>
    void Continue()
    {
        if (_parts!.Session.ContinueSlot() is not { } slot)
            return;
        if (!_parts.Session.Open(slot).Opened)
            _parts.Ui.Commands.Run("open-slot", slot);
    }

    /// <summary>
    /// <c>new-character</c> (Q1, Q2): "Name your character" with the next free "Character &lt;n&gt;" filled in, at most 80
    /// characters; a name the save's check refuses stays in the field with its words in the bubble (<c>ui-refused</c>);
    /// OK makes the character in its slot, saved, and opens it; Cancel changes nothing.
    /// </summary>
    async Task NewCharacter()
    {
        var parts = _parts!;
        var name = await parts.Ui.Dialogs.Prompt(NameQuestion, parts.Session.NextDefaultName(), NameOk, NameRefusal, SaveGame.MaxNameLength);
        if (name is null)
            return;
        var slot = parts.Session.New(name);
        Log.Info("Game", $"a new character, {slot}");
    }

    /// <summary>
    /// A file message on the start screen's own status line (Ctrl+O and Escape's menu reach the file commands from here)
    /// while the start screen is the topmost surface: it shows and no window is open (a window's own line takes what is
    /// said while it is the topmost; the manager's rule for the status lines, 2026-11-14).
    /// </summary>
    void StatusShown(string text, StatusKind kind)
    {
        if (!Showing || _parts!.Ui.Windows.Top is not null)
            return;
        _status!.TextContent = text;
        _status.ClassName = $"backup-status start-status {(kind == StatusKind.Problem ? "is-error" : "is-success")}";
    }
}
