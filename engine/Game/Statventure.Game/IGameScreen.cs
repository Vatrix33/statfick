namespace Statventure.Game;

/// <summary>
/// A screen of the game (Docs/Game/Foundation.md, "The seams": <c>Screens/Start</c>, <c>Sheet</c>, <c>Menu</c>,
/// <c>About</c> are step 9's, <c>Screens/Settings</c>, <c>Slots</c>, <c>Picker</c> step 10's). The application class makes
/// one of each and attaches it at <c>Load</c>, after the parts are made and before the command <c>start-screen</c> runs.
/// </summary>
public interface IGameScreen
{
    /// <summary>Registers the screen's commands and builds its markup on <see cref="GameParts.Ui"/>; once per run.</summary>
    void Attach(GameParts parts);
}
