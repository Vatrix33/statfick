using Statventure.Game.Data.Effects;
using Statventure.Records;

namespace Statventure.Game.Data.Actors;

/// <summary>
/// A sheet's permanent bonuses (<c>actor.bonuses</c>): a percentage for each attribute (the old
/// <c>permanentBonuses</c>) and for Armor and Evasion (<c>permanentDefenseBonuses</c>), as Dev Mode's fields and later
/// rewards set them; any number, fractions too. Read by the old game's keys (<see cref="AttributeKeys"/>,
/// <see cref="DefenseKeys"/>). Part of a sheet, never an entry of its own.
/// </summary>
[NotAPackKind]
[Help("The permanent percentage bonuses of the eight attributes and of Armor and Evasion.")]
[Record("actor.bonuses", 1)]
public sealed record ActorBonuses
{
    [Field(1)] [Unit("%")] public double Strength { get; init; }
    [Field(2)] [Unit("%")] public double Vitality { get; init; }
    [Field(3)] [Unit("%")] public double Mind { get; init; }
    [Field(4)] [Unit("%")] public double Dexterity { get; init; }
    [Field(5)] [Unit("%")] public double Intelligence { get; init; }
    [Field(6)] [Unit("%")] public double Wisdom { get; init; }
    [Field(7)] [Unit("%")] public double Speed { get; init; }
    [Field(8)] [Unit("%")] public double Luck { get; init; }
    [Field(9)] [Unit("%")] public double Armor { get; init; }
    [Field(10)] [Unit("%")] public double Evasion { get; init; }

    /// <summary>The bonus of the attribute or defence <paramref name="key"/>.</summary>
    public double this[string key] => key switch
    {
        AttributeKeys.Strength => Strength,
        AttributeKeys.Vitality => Vitality,
        AttributeKeys.Mind => Mind,
        AttributeKeys.Dexterity => Dexterity,
        AttributeKeys.Intelligence => Intelligence,
        AttributeKeys.Wisdom => Wisdom,
        AttributeKeys.Speed => Speed,
        AttributeKeys.Luck => Luck,
        DefenseKeys.Armor => Armor,
        DefenseKeys.Evasion => Evasion,
        _ => throw new ArgumentException($"\"{key}\" isn't an attribute or a defence", nameof(key)),
    };

    /// <summary>These bonuses with the attribute's or defence's <paramref name="key"/> set to <paramref name="value"/>.</summary>
    public ActorBonuses With(string key, double value) => key switch
    {
        AttributeKeys.Strength => this with { Strength = value },
        AttributeKeys.Vitality => this with { Vitality = value },
        AttributeKeys.Mind => this with { Mind = value },
        AttributeKeys.Dexterity => this with { Dexterity = value },
        AttributeKeys.Intelligence => this with { Intelligence = value },
        AttributeKeys.Wisdom => this with { Wisdom = value },
        AttributeKeys.Speed => this with { Speed = value },
        AttributeKeys.Luck => this with { Luck = value },
        DefenseKeys.Armor => this with { Armor = value },
        DefenseKeys.Evasion => this with { Evasion = value },
        _ => throw new ArgumentException($"\"{key}\" isn't an attribute or a defence", nameof(key)),
    };
}

/// <summary>
/// A sheet's purse (<c>actor.purse</c>, the old <c>currencies</c>): silver, gold and platinum, whole and not below 0
/// as the old fields keep them (100 silver a gold and 10,000 a platinum are the shops', GM6). Part of a sheet.
/// </summary>
[NotAPackKind]
[Help("The purse: silver, gold and platinum.")]
[Record("actor.purse", 1)]
public sealed record Purse
{
    [Field(1)] [Help("Silver coins (the old currencies.silver).")] public int Silver { get; init; }
    [Field(2)] [Help("Gold coins (the old currencies.gold).")] public int Gold { get; init; }
    [Field(3)] [Help("Platinum coins (the old currencies.platinum).")] public int Platinum { get; init; }

    public int this[Coin coin] => coin switch
    {
        Coin.Silver => Silver,
        Coin.Gold => Gold,
        Coin.Platinum => Platinum,
        _ => throw new ArgumentOutOfRangeException(nameof(coin)),
    };

    public Purse With(Coin coin, int value) => coin switch
    {
        Coin.Silver => this with { Silver = value },
        Coin.Gold => this with { Gold = value },
        Coin.Platinum => this with { Platinum = value },
        _ => throw new ArgumentOutOfRangeException(nameof(coin)),
    };
}
