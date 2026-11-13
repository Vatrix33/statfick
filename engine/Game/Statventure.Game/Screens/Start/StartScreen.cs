namespace Statventure.Game.Screens.Start;

/// <summary>
/// The start screen (GM1-2, Q10, Q16, Q16b; Docs/Game/Foundation.md, "The start screen"; step 9): the game's name and
/// Continue (greyed while there is nothing to continue), New character, Load, Settings, Quit and About, the line naming
/// the mods left out; its commands <c>start-screen</c>, <c>new-character</c> and <c>continue</c>. <b>Step 2's stub:</b>
/// does nothing (the command <c>start-screen</c> the start runs isn't registered yet, so it is logged).
/// </summary>
public sealed class StartScreen : IGameScreen
{
    public void Attach(GameParts parts)
    {
    }
}
