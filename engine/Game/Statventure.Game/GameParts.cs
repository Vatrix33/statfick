using Statventure.Game.Mods;
using Statventure.Game.Random;
using Statventure.Game.Session;
using Statventure.Game.Settings;
using Statventure.Game.Ui;

namespace Statventure.Game;

/// <summary>
/// The program's parts (Docs/Game/Foundation.md, "The seams"), made by the application class at <c>Load</c> and handed to
/// each screen (<see cref="IGameScreen.Attach"/>): the settings, the open character and its slots, the mods, the game's
/// document with its windows, commands and dialogs, the run's randomness and the application itself. Screens use these
/// and reach each other only through <see cref="GameUi.Commands"/>; no part calls a screen.
/// </summary>
/// <param name="Settings"><c>settings.json</c> (step 5).</param>
/// <param name="Session">The save slots and the open character (step 5).</param>
/// <param name="Mods">The mods checked at the start and their list (step 6).</param>
/// <param name="Ui">The game's document, its windows, commands, dialogs, status and sounds (step 8).</param>
/// <param name="Random">The randomness every gameplay roll draws from (step 5; GM1-5, GM1-P11).</param>
/// <param name="Game">The application: its window, storage, data and sound, and <see cref="Core.Application.Quit"/>.</param>
public sealed record GameParts(GameSettings Settings, GameSession Session, GameMods Mods, GameUi Ui, IGameRandom Random, StatFickGame Game);
