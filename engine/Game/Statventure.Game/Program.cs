using Statventure.Storage;

namespace Statventure.Game;

/// <summary>
/// The game's start (Docs/Game/Foundation.md, "Starting"): <c>stat-fick</c> / <c>Stat Fick.exe</c>
/// [<see cref="GameOptions">the checks' options</see>]. One instance per per-user folder (<see cref="SingleInstance"/>,
/// GM1-P14): a later start asks the running one to bring its window forward and ends with 0; otherwise the game runs
/// (<see cref="StatFickGame"/>). From a script, give it a scratch per-user folder (<c>XDG_DATA_HOME</c>) and OpenAL Soft's
/// null output (<c>ALSOFT_DRIVERS=null</c>).
/// </summary>
public static class Program
{
    /// <summary>What a later start says on the terminal before it ends.</summary>
    public const string ForwardedText = "Stat Fick is already running for this player: its window was asked to come forward.";

    /// <summary>What a later start says when the running one didn't answer.</summary>
    public const string UnansweredText = "Stat Fick is already running for this player, but it didn't answer.";

    public static int Main(string[] args) => Start(GameOptions.Parse(args), GameFolder.ForThisMachine(), Console.Out.WriteLine, game =>
    {
        game.Run();
        return 0;
    });

    /// <summary>
    /// One start on <paramref name="gameFolder"/>: the folder claimed (<see cref="SingleInstance.Claim"/>); a later start
    /// tells <paramref name="terminal"/> and ends with 0; the first makes the game and gives it to <paramref name="run"/>
    /// (the program's: <c>Run</c>, its exit code 0; a crash ends the process with the crash's exit code itself).
    /// </summary>
    internal static int Start(GameOptions options, string gameFolder, Action<string> terminal, Func<StatFickGame, int> run, TimeSpan? answerTime = null)
    {
        using var instance = SingleInstance.Claim(gameFolder, answerTime);
        switch (instance.Outcome)
        {
            case SingleInstanceOutcome.Forwarded:
                terminal(ForwardedText);
                return 0;
            case SingleInstanceOutcome.Unanswered:
                terminal(UnansweredText);
                return 0;
        }
        using var game = StatFickGame.Create(options, gameFolder);
        game.SingleInstanceProblem = instance.Problem;
        instance.ForwardAsked += game.AskForward;
        return run(game);
    }
}
