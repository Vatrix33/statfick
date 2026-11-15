using Statventure.Game.Data.Effects;
using Statventure.Records;

namespace Statventure.Game.Data.Actors;

/// <summary>
/// A whole number for each of the eight attributes (<c>actor.attributes</c>): a sheet's stored attributes and the free
/// points spent on each, read by the old game's keys (<see cref="AttributeKeys"/>). Part of a sheet, never an entry of its
/// own (<see cref="NotAPackKindAttribute"/>).
/// </summary>
[NotAPackKind]
[Help("A whole number for each of the eight attributes.")]
[Record("actor.attributes", 1)]
public sealed record AttributeSet
{
    [Field(1)] public int Strength { get; init; }
    [Field(2)] public int Vitality { get; init; }
    [Field(3)] public int Mind { get; init; }
    [Field(4)] public int Dexterity { get; init; }
    [Field(5)] public int Intelligence { get; init; }
    [Field(6)] public int Wisdom { get; init; }
    [Field(7)] public int Speed { get; init; }
    [Field(8)] public int Luck { get; init; }

    /// <summary>The same number for all eight.</summary>
    public static AttributeSet All(int value) => new()
    {
        Strength = value, Vitality = value, Mind = value, Dexterity = value,
        Intelligence = value, Wisdom = value, Speed = value, Luck = value,
    };

    /// <summary>Each attribute's number from <paramref name="make"/> (given its key).</summary>
    public static AttributeSet From(Func<string, int> make) => new()
    {
        Strength = make(AttributeKeys.Strength), Vitality = make(AttributeKeys.Vitality), Mind = make(AttributeKeys.Mind),
        Dexterity = make(AttributeKeys.Dexterity), Intelligence = make(AttributeKeys.Intelligence),
        Wisdom = make(AttributeKeys.Wisdom), Speed = make(AttributeKeys.Speed), Luck = make(AttributeKeys.Luck),
    };

    /// <summary>The number of the attribute <paramref name="key"/> (one of <see cref="AttributeKeys"/>).</summary>
    public int this[string key] => key switch
    {
        AttributeKeys.Strength => Strength,
        AttributeKeys.Vitality => Vitality,
        AttributeKeys.Mind => Mind,
        AttributeKeys.Dexterity => Dexterity,
        AttributeKeys.Intelligence => Intelligence,
        AttributeKeys.Wisdom => Wisdom,
        AttributeKeys.Speed => Speed,
        AttributeKeys.Luck => Luck,
        _ => throw new ArgumentException($"\"{key}\" isn't an attribute", nameof(key)),
    };

    /// <summary>This set with the attribute <paramref name="key"/>'s number <paramref name="value"/>.</summary>
    public AttributeSet With(string key, int value) =>
        AttributeValues.Has(key) ? From(k => k == key ? value : this[k]) : throw new ArgumentException($"\"{key}\" isn't an attribute", nameof(key));

    /// <summary>The sum of the eight, in their order.</summary>
    public long Sum() => AttributeKeys.All.Sum(k => (long)this[k]);
}
