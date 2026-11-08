using Statventure.Records;

namespace Statventure.Game.Data;

/// <summary>
/// The stand-in data library's one test kind (M12 step 6, Q19): an entry of a kind that isn't the engine's, so the
/// editor and the packer are shown taking a game's kinds from its data library. Not a game rule: the port replaces
/// the library's contents with the game's own kinds (items, enemies, spells, shops).
/// </summary>
[Kind("Stand-in item", "stand-in/items")]
[Help("A test kind of the stand-in data library: the port replaces it with the game's own kinds.")]
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
