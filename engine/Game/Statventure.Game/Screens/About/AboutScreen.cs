using Statventure.Core;
using Statventure.UI;
using Statventure.UI.Dom;

namespace Statventure.Game.Screens.About;

/// <summary>
/// About (Q20; Docs/Game/Foundation.md, "About"; step 9): a parchment dialog holding the engine's credits text and the
/// version; its command <c>about</c>, from the start screen and the menu. <b>Step 2's stub</b> (GM1-P12's <c>--about</c>
/// until step 9, which replaces it): the command <c>about</c> shows the engine's dark credits view over the whole
/// document, as the sandbox shows it, with a Close of its own.
/// </summary>
public sealed class AboutScreen : IGameScreen
{
    /// <summary>The stub's view: the engine's credits (<see cref="Credits.Css"/>) on their dark ground, filling the window, scrolling on their own.</summary>
    const string ViewCss = """
        .engine-credits { position: fixed; inset: 0; background: #1a1c1a; }
        .engine-credits .credits-view { height: 100%; overflow: auto; scrollbar-color: #7d817b #292c28; }
        .engine-credits .credits-close { position: absolute; top: 12px; right: 28px; height: 24px; padding: 0 10px; border: 1px solid #3a3d38; border-radius: 3px; color: #eeeae1; background: #292c28; font: 13px/22px Lato; }
        """;

    public void Attach(GameParts parts) => parts.Ui.Commands.Register("about", _ => Show(parts));

    void Show(GameParts parts)
    {
        var document = parts.Ui.Document;
        if (document.QuerySelector(".engine-credits") is not null)
            return;
        if (!_styled)
        {
            document.AddStylesheet(Credits.Css, "Statventure.UI.Credits");
            document.AddStylesheet(ViewCss, "game-about-stub");
            _styled = true;
        }
        var view = document.Body.Append("div", "engine-credits");
        Markup.SetInnerHtml(view, $"""<div class="credits-view">{Credits.Markup(StatFickGame.Title, ProgramInfo.Of(typeof(StatFickGame).Assembly))}</div><button class="credits-close">Close</button>""");
        document.QuerySelector(".engine-credits .credits-close")!.AddEventListener("click", _ =>
        {
            view.Remove();
            Log.Info("Game", "the credits view closed");
        });
        Log.Info("Game", "the credits view opened");
    }

    bool _styled;
}
