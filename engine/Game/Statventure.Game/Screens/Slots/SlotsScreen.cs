using Statventure.Core;
using Statventure.Game.Session;
using Statventure.Game.Sounds;
using Statventure.Game.Ui;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.Slots;

/// <summary>
/// The slot list, Load, with its "Load Character…" button, and the damaged-save question (Q1, Q9, Q17, Q21, Q24, Q25, Q29;
/// Docs/Game/Foundation.md, "The slot list, Load" and "The damaged-save question"; step 10); its commands
/// <c>open-slots</c> and <c>open-slot</c> (its argument a slot: opened, or the damaged-save question asked). A window in
/// the old windows' look (a spell catalogue's frame, the companions' toolbar for "Load Character…", the catalogue's rows)
/// listing every slot the last played first (<see cref="GameSession.Slots"/>): the character's name, when it was last
/// played (in the player's time zone), and that it is damaged when its save can't be read; Open and Delete (Delete asks
/// once, its action focused, Q29). A status line of its own says what an action taken from it gave (a character file
/// refused). It follows the session: its rows when the slots change, and it closes when a character opens.
/// </summary>
public sealed class SlotsScreen : IGameScreen
{
    /// <summary>The command that opens the list.</summary>
    public const string OpenCommand = "open-slots";

    /// <summary>The command that opens a slot (its argument), or asks the damaged-save question.</summary>
    public const string OpenSlotCommand = "open-slot";

    /// <summary>The command "Load Character…" runs (the file picker's, step 10's <c>Screens/Picker</c>).</summary>
    public const string LoadCharacterCommand = "load-character";

    /// <summary>The screen's sheet in <c>ui.pak</c>.</summary>
    public const string Stylesheet = "ui/styles/slots";

    /// <summary>What the list says when there are no slots.</summary>
    public const string NoSlots = "No characters yet. New character on the start screen makes one, and Load Character… loads a character file.";

    /// <summary>What a damaged slot's row says beside its time.</summary>
    public const string Damaged = "Damaged";

    /// <summary>Delete's question (Q17): asked once, its action focused (Q29).</summary>
    public static string DeleteQuestion(string name) => $"Delete {name}? Its save and its backup are removed.";

    /// <summary>The damaged-save question (Q21).</summary>
    public static string DamagedQuestion(string name) => $"{name}'s save is damaged.";

    /// <summary>The damaged-save question's line under it (Q39 of M9: the damaged file is kept aside whatever the answer).</summary>
    public const string DamagedNote = "Whatever you choose, the damaged file is kept aside in the saves folder.";

    /// <summary>The damaged-save question's answers.</summary>
    public const string BackText = "Back", StartNewText = "Start new";

    /// <summary>"Use the backup (saved &lt;time&gt;)" (only when the backup reads).</summary>
    public static string UseBackupText(string time) => $"Use the backup (saved {time})";

    GameParts? _parts;
    UiElement? _layer, _list, _status;

    /// <summary>The time zone the times are written in (the player's; a test's own).</summary>
    internal TimeZoneInfo TimeZone { get; set; } = TimeZoneInfo.Local;

    /// <summary>The list's window (made at <see cref="Attach"/>), for the tests.</summary>
    internal UiElement Layer => _layer ?? throw new InvalidOperationException("the screen isn't attached");

    /// <summary>The damaged-save question's window while it is open, for the tests.</summary>
    internal UiElement? Question { get; private set; }

    public void Attach(GameParts parts)
    {
        _parts = parts;
        parts.Ui.UseStylesheet(Stylesheet);
        _layer = Markup.Element(Frame);
        _list = Find("slots-list");
        _status = Find("slots-status");
        foreach (var close in _layer.Descendants().Where(e => e.HasAttribute("data-slots-close")))
            close.AddEventListener("click", _ => parts.Ui.Windows.Close(_layer));
        Find("slots-load-file").AddEventListener("click", _ => parts.Ui.Commands.Run(LoadCharacterCommand));
        parts.Session.SlotsChanged += () =>
        {
            if (IsOpen)
                ShowSlots();
        };
        // A character opened (Open, the damaged-save question's answers, Load Character…): the list is done.
        parts.Session.Opened += () =>
        {
            if (IsOpen)
                parts.Ui.Windows.Close(_layer);
        };
        // What an action taken from the list gave (it is the topmost window then: a character file refused).
        parts.Ui.StatusShown += (text, kind) =>
        {
            if (parts.Ui.Windows.Top == _layer)
                Say(text, kind);
        };
        parts.Ui.Commands.Register(OpenCommand, _ => Open());
        parts.Ui.Commands.Register(OpenSlotCommand, slot =>
        {
            if (slot is null)
                Log.Warning("Slots", $"{OpenSlotCommand}: no slot given, nothing done");
            else
                OpenSlot(slot);
        });
    }

    bool IsOpen => _layer is not null && _parts is { } parts && parts.Ui.Windows.IsOpen(_layer);

    UiElement Find(string id) => Layer.Descendants().First(e => e.Id == id);

    /// <summary>Opens the list (its rows as the slots are now, its status line empty), or leaves it as it is when open.</summary>
    public void Open()
    {
        var parts = _parts ?? throw new InvalidOperationException("the screen isn't attached");
        if (IsOpen)
            return;
        Say("", StatusKind.Info);
        ShowSlots();
        parts.Ui.Windows.Open(Layer);
        Log.Info("Slots", "the slot list opened");
    }

    /// <summary>The rows: every slot, the last played first; or a line saying there are none.</summary>
    void ShowSlots()
    {
        var list = _list!;
        list.ClearChildren();
        var slots = _parts!.Session.Slots();
        if (slots.Count == 0)
        {
            var empty = list.Append("div", "spell-list-empty slots-empty");
            empty.Append("span", text: "…").SetAttribute("aria-hidden", "true");
            empty.Append("p", text: NoSlots);
            return;
        }
        foreach (var slot in slots)
            list.Append(Row(slot));
    }

    /// <summary>A slot's row (the old catalogue's, <c>.available-spell</c>): the name, when it was last played (and that it is damaged), Open and Delete.</summary>
    UiElement Row(SlotInfo slot)
    {
        var row = new UiElement("article") { ClassName = slot.Damaged ? "available-spell slot-row is-damaged" : "available-spell slot-row" };
        row.SetAttribute("data-slot", slot.Slot);
        var text = row.Append("div");
        text.Append("strong", text: slot.Name);
        var meta = text.Append("span", "slot-meta");
        if (slot.Damaged)
            meta.Append("span", "slot-damaged", Damaged);
        meta.Append("span", "slot-time", ScreenTimes.Write(slot.LastPlayed, TimeZone));
        var open = row.Append("button", "available-spell-add slot-open", "Open");
        open.SetAttribute("type", "button");
        open.SetAttribute("aria-label", $"Open {slot.Name}");
        open.AddEventListener("click", _ => OpenSlot(slot.Slot));
        var delete = row.Append("button", "available-spell-add slot-delete", "Delete");
        delete.SetAttribute("type", "button");
        delete.SetAttribute("aria-label", $"Delete {slot.Name}");
        delete.AddEventListener("click", _ => _parts!.Ui.Document.RunScript(() => Delete(slot)));
        return row;
    }

    /// <summary>Delete: asked once (its action focused, Q29), then the save and its backup removed (files kept aside stay).</summary>
    async Task Delete(SlotInfo slot)
    {
        var parts = _parts!;
        if (!await parts.Ui.Dialogs.Confirm(DeleteQuestion(slot.Name), "Delete"))
            return;
        var index = Rows.FindIndex(r => r.GetAttribute("data-slot") == slot.Slot);
        parts.Session.Delete(slot.Slot);
        parts.Ui.Sound(SoundNames.Deleted);
        // The row is gone (the list follows the slots): the focus goes to the row now in its place, else the one before,
        // else "Load Character…".
        var rows = Rows;
        var next = rows.Count == 0 ? Find("slots-load-file") : rows[Math.Clamp(index, 0, rows.Count - 1)].ElementChildren.First(e => e.HasClass("slot-open"));
        parts.Ui.Document.UpdateLayout();
        next.Focus();
    }

    List<UiElement> Rows => [.. _list!.ElementChildren.Where(e => e.HasClass("slot-row"))];

    /// <summary>
    /// Opens <paramref name="slot"/> (<see cref="GameSession.Open"/>): a readable one opens (the list closes with the
    /// session's <c>Opened</c>; the page that follows it plays <c>game-opened</c>, step 9's); a damaged or unreadable one
    /// asks the damaged-save question.
    /// </summary>
    public void OpenSlot(string slot)
    {
        var parts = _parts ?? throw new InvalidOperationException("the screen isn't attached");
        var result = parts.Session.Open(slot);
        if (!result.Opened)
            Ask(result);
    }

    /// <summary>
    /// The damaged-save question (Q21) in the old alert dialogs' look, as the confirm dialog (step 8): "&lt;Name&gt;'s save is
    /// damaged.", a line saying the damaged file is kept aside, and Back, Start new (a new character in this slot, its name
    /// kept, Q25) and, only when the backup reads, "Use the backup (saved &lt;time&gt;)". The answer that recovers most has
    /// the focus (the backup, else Start new: the action, as Q29's confirm); Escape is Back.
    /// </summary>
    void Ask(OpenResult result)
    {
        var parts = _parts!;
        var layer = new UiElement("div") { ClassName = "modal-layer map-encounter-layer game-dialog-layer damaged-save-layer" };
        layer.Append("div", "modal-backdrop").SetAttribute("aria-hidden", "true");
        var dialog = layer.Append("section", "map-encounter-dialog game-dialog damaged-save-dialog");
        dialog.SetAttribute("role", "alertdialog");
        dialog.SetAttribute("aria-modal", "true");
        dialog.SetAttribute("aria-labelledby", "damaged-save-question");
        dialog.SetAttribute("aria-describedby", "damaged-save-note");
        dialog.Append("p", "game-dialog-question", DamagedQuestion(result.Name)).Id = "damaged-save-question";
        dialog.Append("p", "damaged-save-note", DamagedNote).Id = "damaged-save-note";
        var actions = dialog.Append("div", "map-encounter-actions game-dialog-actions damaged-save-actions");
        var back = Button(actions, "damaged-save-back", BackText);
        var startNew = Button(actions, "damaged-save-new", StartNewText);
        UiElement? useBackup = null;
        if (result.Backup == BackupState.Readable && result.BackupTime is { } time)
            useBackup = Button(actions, "damaged-save-backup game-dialog-accept", UseBackupText(ScreenTimes.Write(time, TimeZone)));
        else
            startNew.AddClass("game-dialog-accept");
        void Close()
        {
            Question = null;
            parts.Ui.Windows.Close(layer);
        }
        back.AddEventListener("click", _ => Close());
        // Either answer opens the character (the session's Opened: the list closes, the page plays game-opened).
        startNew.AddEventListener("click", _ =>
        {
            Close();
            parts.Session.StartNew(result.Slot);
        });
        useBackup?.AddEventListener("click", _ =>
        {
            Close();
            parts.Session.UseBackup(result.Slot);
        });
        Question = layer;
        parts.Ui.Windows.Open(layer, useBackup ?? startNew, Close);
        Log.Info("Slots", $"the damaged-save question for {result.Slot} (its backup {result.Backup.ToString().ToLowerInvariant()})");
    }

    static UiElement Button(UiElement actions, string className, string text)
    {
        var button = actions.Append("button", "backup-button " + className, text);
        button.SetAttribute("type", "button");
        return button;
    }

    /// <summary>The list's status line: news, or a problem (the old <c>.spellbook-status</c>'s <c>is-success</c> and <c>is-error</c>).</summary>
    void Say(string text, StatusKind kind)
    {
        var status = _status!;
        status.TextContent = text;
        status.ClassName = text.Length == 0 ? "spellbook-status slots-status" : kind == StatusKind.Problem ? "spellbook-status slots-status is-error" : "spellbook-status slots-status is-success";
    }

    /// <summary>The window: the old windows' frame (a spell catalogue's dialog, <c>old-game/index.html</c> 375–392), the companions' toolbar (957–962) for "Load Character…".</summary>
    const string Frame = """
        <div class="modal-layer slots-layer" id="slots-modal" hidden>
          <button class="modal-backdrop" type="button" data-slots-close aria-label="Close the characters"></button>
          <section class="spell-picker-dialog slots-dialog" role="dialog" aria-modal="true" aria-labelledby="slots-title">
            <header class="dialog-header compact-dialog-header">
              <div><p class="eyebrow">LOAD</p><h2 id="slots-title">Your <em>characters</em></h2></div>
              <button class="modal-close" type="button" data-slots-close aria-label="Close the characters">×</button>
            </header>
            <div class="companion-import-toolbar slots-toolbar">
              <p>Every character saved here, the last played first.</p>
              <button class="backup-button" id="slots-load-file" type="button">Load Character…</button>
            </div>
            <div class="available-spells-list slots-list" id="slots-list"></div>
            <p class="spellbook-status slots-status" id="slots-status" role="status" aria-live="polite"></p>
          </section>
        </div>
        """;
}
