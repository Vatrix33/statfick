namespace Statventure.Game.Data.Effects;

/// <summary>
/// The eight attributes' keys as the old game names them (<c>statDefinitions</c>, <c>old-game/scripts/app.js</c>
/// 285–294), in its order: the keys effects name in their targets and the races' modifiers are named by.
/// </summary>
public static class AttributeKeys
{
    public const string Strength = "strength";
    public const string Vitality = "vitality";
    public const string Mind = "mind";
    public const string Dexterity = "dexterity";
    public const string Intelligence = "intelligence";
    public const string Wisdom = "wisdom";
    public const string Speed = "speed";
    public const string Luck = "luck";

    /// <summary>The eight, in the old game's order.</summary>
    public static IReadOnlyList<string> All { get; } = [Strength, Vitality, Mind, Dexterity, Intelligence, Wisdom, Speed, Luck];
}

/// <summary>The two defences' keys (<c>defenseBonusDefinitions</c>, <c>app.js</c> 301), in the old game's order.</summary>
public static class DefenseKeys
{
    public const string Armor = "armor";
    public const string Evasion = "evasion";

    public static IReadOnlyList<string> All { get; } = [Armor, Evasion];
}

/// <summary>A number for each of the eight attributes (a total of a source's effects, such as <see cref="RaceTotals.StatPermanentPercent"/>).</summary>
public sealed record AttributeValues(double Strength, double Vitality, double Mind, double Dexterity, double Intelligence, double Wisdom, double Speed, double Luck)
{
    /// <summary>0 for each.</summary>
    public static AttributeValues Zero { get; } = new(0, 0, 0, 0, 0, 0, 0, 0);

    /// <summary>Whether <paramref name="key"/> is one of the eight (the old game's <c>key in totals</c>, ordinal).</summary>
    public static bool Has(string key) => AttributeKeys.All.Contains(key, StringComparer.Ordinal);

    /// <summary>The value of the attribute <paramref name="key"/> (one of <see cref="AttributeKeys"/>).</summary>
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
        _ => throw new ArgumentException($"\"{key}\" isn't an attribute", nameof(key)),
    };

    /// <summary>These values with the attribute <paramref name="key"/>'s set to <paramref name="value"/>.</summary>
    public AttributeValues With(string key, double value) => key switch
    {
        AttributeKeys.Strength => this with { Strength = value },
        AttributeKeys.Vitality => this with { Vitality = value },
        AttributeKeys.Mind => this with { Mind = value },
        AttributeKeys.Dexterity => this with { Dexterity = value },
        AttributeKeys.Intelligence => this with { Intelligence = value },
        AttributeKeys.Wisdom => this with { Wisdom = value },
        AttributeKeys.Speed => this with { Speed = value },
        AttributeKeys.Luck => this with { Luck = value },
        _ => throw new ArgumentException($"\"{key}\" isn't an attribute", nameof(key)),
    };
}

/// <summary>A number for each of the two defences, Armor and Evasion.</summary>
public sealed record DefenseValues(double Armor, double Evasion)
{
    /// <summary>0 for each.</summary>
    public static DefenseValues Zero { get; } = new(0, 0);

    /// <summary>No cap on either (the old game's <c>Infinity</c>).</summary>
    public static DefenseValues Uncapped { get; } = new(double.PositiveInfinity, double.PositiveInfinity);

    /// <summary>Whether <paramref name="key"/> is <c>armor</c> or <c>evasion</c> (ordinal).</summary>
    public static bool Has(string key) => key is DefenseKeys.Armor or DefenseKeys.Evasion;

    public double this[string key] => key switch
    {
        DefenseKeys.Armor => Armor,
        DefenseKeys.Evasion => Evasion,
        _ => throw new ArgumentException($"\"{key}\" isn't a defence", nameof(key)),
    };

    public DefenseValues With(string key, double value) => key switch
    {
        DefenseKeys.Armor => this with { Armor = value },
        DefenseKeys.Evasion => this with { Evasion = value },
        _ => throw new ArgumentException($"\"{key}\" isn't a defence", nameof(key)),
    };
}
