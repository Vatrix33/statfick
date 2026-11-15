namespace Statventure.Game.Data.Actors;

// The attributes and the defences are named by the old game's keys (Effects/AttributeValues.cs: AttributeKeys,
// DefenseKeys), as the races' effects name them; these two the sheet adds.

/// <summary>HP and Mana, the two resources the sheet changes by hand.</summary>
public enum ActorResource
{
    Hp,
    Mana,
}

/// <summary>The purse's coins (<c>currencyDefinitions</c>, <c>app.js</c> 296).</summary>
public enum Coin
{
    Silver,
    Gold,
    Platinum,
}
