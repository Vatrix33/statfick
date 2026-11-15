using Statventure.Core;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.About;

/// <summary>
/// About (Q20; Docs/Game/Foundation.md, "About"; step 9): a dialog in the game's parchment look holding the engine's credits
/// (<see cref="Credits.Markup"/> for "Stat Fick" at the program's version: "Built with BRUTAL" with BRUTAL's notice and its
/// copyright line, every component with its licence and copyright lines, where the licence texts are; the same text the
/// engine's credits tests check, nothing dropped or reworded), its content scrolling within the window. Its command
/// <c>about</c>, from the start screen, the menu and <c>--about</c>; × (or Escape, or the ground around it) closes it.
/// </summary>
public sealed class AboutScreen : IGameScreen
{
    /// <summary>About's own sheet (<c>Assets/UI/styles/about.css</c>: the credits in the game's lettering).</summary>
    public const string Stylesheet = "ui/styles/about";

    /// <summary>What the credits say of the game: its own program file's (the version, no commit; M12 Q3).</summary>
    public static ProgramInfo Program { get; } = ProgramInfo.Of(typeof(StatFickGame).Assembly);

    /// <summary>The window: the old dialogs' layer with its backdrop, a dialog with a leather header ("HELP", "About Stat Fick", ×) over the credits.</summary>
    internal static string Markup(ProgramInfo program) => $"""
        <div class="modal-layer about-layer" id="about-modal">
          <button class="modal-backdrop" type="button" aria-label="Close About"></button>
          <section class="spell-picker-dialog about-dialog" role="dialog" aria-modal="true" aria-labelledby="about-title">
            <header class="dialog-header compact-dialog-header">
              <div><p class="eyebrow">HELP</p><h2 id="about-title">About <em>Stat Fick</em></h2></div>
              <button class="modal-close" type="button" aria-label="Close About">×</button>
            </header>
            <div class="about-content">{Credits.Markup(StatFickGame.Title, program)}</div>
          </section>
        </div>
        """;

    GameParts? _parts;
    UiElement? _layer;

    /// <summary>The About window once made (null before its first opening).</summary>
    public UiElement? Layer => _layer;

    public void Attach(GameParts parts)
    {
        _parts = parts;
        parts.Ui.UseStylesheet(Stylesheet);
        parts.Ui.Commands.Register("about", _ => Open());
    }

    /// <summary>
    /// <c>about</c>: the window opened, its credits at their top (the window manager takes the window out of the document
    /// when it closes, and a box put in again scrolls from its top, as Blink's scroll offset goes with its box). Nothing
    /// when it is open already.
    /// </summary>
    void Open()
    {
        var ui = _parts!.Ui;
        if (_layer is null)
        {
            _layer = Statventure.UI.Dom.Markup.Element(Markup(Program));
            Find(_layer, ".modal-backdrop").AddEventListener("click", _ => ui.Windows.Close(_layer));
            Find(_layer, ".modal-close").AddEventListener("click", _ => ui.Windows.Close(_layer));
        }
        if (ui.Windows.IsOpen(_layer))
            return;
        ui.Windows.Open(_layer);
        Log.Info("Game", "the credits view opened");
    }

    static UiElement Find(UiElement root, string selector) => root.Descendants().First(e => e.Matches(selector));
}
