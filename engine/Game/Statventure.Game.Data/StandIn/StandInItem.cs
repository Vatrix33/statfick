using Statventure.Records;

namespace Statventure.Game.Data.StandIn;

/// <summary>
/// A development-only kind (GM1-P3; Docs/Game/Foundation.md, "The program, the library and the repo"): the stand-in data
/// library's test kind of M12 step 6 (Q19), moved into the game's real library so the editor's tests and the end-to-end
/// mod keep a game kind with <see cref="KindAttribute"/> until the game has one of its own. Not a game rule and nothing
/// the game reads: it goes when GM2's first real kind with <see cref="KindAttribute"/> arrives, and the tests and the
/// flow switch to that kind then.
/// </summary>
[Kind("Stand-in item", "stand-in/items")]
[Help("A development-only test kind, not part of the game: it goes when the game's first real kind arrives (GM2).")]
[Record("stand-in.item", 1)]
public sealed record StandInItem
{
    /// <summary>The item's name as a player would see it.</summary>
    [Field(1)] [Help("The name players see.")] public string Title { get; init; } = "";

    /// <summary>What it costs in a shop.</summary>
    [Field(2)] [Unit("gold")] [Limits(0, double.PositiveInfinity)] public int Price { get; init; }

    /// <summary>How rare it is.</summary>
    [Field(3)] [Choices("Common", "Rare", "Exotic")] public string Rarity { get; init; } = "Common";
}
