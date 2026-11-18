using Statventure.Game.Settings;

namespace Statventure.Game.Tests;

/// <summary>
/// The 3D views' mask in a comparison against Chromium (GM2-P12, GM2 step 5): the game's page with a framed panel holding a
/// view's box (a <c>surface</c>, sized as a view must be: <c>display: block</c> with its width and its height, the engine
/// giving a <c>surface</c> a picture's 300 × 150 where Chromium gives an unknown element nothing) and words beside it. What
/// the box holds is the view's: the engine draws its picture there (none in the harness), Chromium the element's own content
/// (here a red block: a <c>surface</c> is a replaced element to the engine, which draws no children of it); every fixture's
/// <c>surface</c> boxes are the golden's in the render (<see cref="GameScreenFixtures.Views"/>), so only the page around the
/// view is compared, and this fixture differs by the whole box without the mask.
/// </summary>
sealed class ViewScreenFixtures : IGameScreenFixtures
{
    public IEnumerable<GameScreenFixture> Fixtures =>
    [
        new("view-masked-dark", 800, 600, GameTheme.Dark, h =>
        {
            var panel = h.Document.Body.Append("figure", "view-panel");
            // 402 × 482 by its border (the game's sheets size boxes so): a window of 360 × 440 within its padding.
            panel.SetAttribute("style", "position: absolute; left: 199px; top: 39px; width: 402px; height: 482px; margin: 0; padding: 20px; border: 1px solid #6e5538; background: #170f0a");
            var view = panel.Append("surface", "view-box");
            view.SetAttribute("style", "display: block; width: 360px; height: 440px; background: #2a1f1a");
            view.Append("div").SetAttribute("style", "width: 100%; height: 100%; background: #c03030");
            var words = h.Document.Body.Append("p", "hero-copy", "The hero's figure stands in the panel.");
            words.SetAttribute("style", "position: absolute; left: 40px; top: 530px; margin: 0");
            h.Frame();
        }),
    ];
}
