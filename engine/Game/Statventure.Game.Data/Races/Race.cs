using Statventure.Game.Data.Effects;
using Statventure.Packs;
using Statventure.Records;

namespace Statventure.Game.Data.Races;

// The races as records (GM2-P4; Docs/Game/Hero.md, "The races as records" and "How it works", step 1): one entry per
// race at data/races/<id> in data.pak, its id the entry's name. The fields are what the old game's
// normalizeRaceDefinition keeps (old-game/scripts/app.js 2926–2954): the name, playable, the eight modifiers and the
// passive (id, name, condition, description, effects), and the seal as the emblem's name (Q44); the accent colour that
// was code (raceAccent, 2956–2958) is data now; the order the races come in is a number of each (Q43); the swatches'
// field is the maker's (GM2-P7), empty until the approved look gives its values. The checks refuse what the old loader
// refused, in the editor's words; what it refused that a record can't hold at all (not an object, a name or passive that
// isn't there) the record's own form refuses, and what it refused across the file (not an array, a repeated id) can't
// happen with one entry per path.

/// <summary>A race (record <c>race</c>, the kind at <c>data/races</c>): its name, modifiers, passive, colour, emblem and place in the list.</summary>
[Kind("Race", "data/races")]
[Help("A race a character can be: its attribute modifiers and its passive. The playable ones are offered to a new hero; the others are enemies' (and Dev Mode's).")]
[Record("race", 1)]
public sealed record Race
{
    /// <summary>The accent colour of a race the old game named none for (<c>raceAccent</c>'s default).</summary>
    public const string DefaultAccent = "#c7683f";

    /// <summary>The race's name as players see it (shown trimmed).</summary>
    [Field(1)] [Help("The race's name as players see it.")] public required string Name { get; init; }

    /// <summary>Whether a new hero can choose it (the old game: anything but <c>false</c> counts as playable).</summary>
    [Field(2)] [Help("Whether a new hero can choose it. Races that aren't are enemies', chosen only in Dev Mode.")] public bool Playable { get; init; } = true;

    /// <summary>The eight attributes' modifiers, added to the stored attributes.</summary>
    [Field(3)] [Help("What the race adds to each attribute (below 0 takes away).")] public RaceModifiers StatModifiers { get; init; } = new();

    /// <summary>The race's passive.</summary>
    [Field(4)] [Help("The race's passive: its name, when it applies and what it does.")] public required RacePassive Passive { get; init; }

    /// <summary>The race's colour in the race list and on the seal button (<c>raceAccent</c>), "#rrggbb".</summary>
    [Field(5)] [Help("The race's colour in the race list and on the sheet's seal (#rrggbb).")] [Colour] public string Accent { get; init; } = DefaultAccent;

    /// <summary>The name of the race's emblem (Q44: "human", "dark-elf" …), or null when it has none (the ✦ shows instead).</summary>
    [Field(6)] [Help("The name the race's emblem pictures and model are found by (not set: no emblem, the ✦ shows instead).")] public string? Emblem { get; init; }

    /// <summary>Where the race comes in the race list (Q43): lower first, the same number by id.</summary>
    [Field(7)] [Help("Where the race comes in the race list: lower numbers first, races with the same number by their id. The game's races are 10, 20, … in the old game's order.")] public int Order { get; init; }

    /// <summary>The colours the character maker offers for this race (GM2-P7; empty until the approved look gives them).</summary>
    [Field(8)] [Help("The colours the character maker offers for this race, each named, for a palette slot (skin, hair, eyes …).")] public IReadOnlyList<RaceSwatch> Swatches { get; init; } = [];

    /// <summary>
    /// The race's modifier of the attribute <paramref name="attribute"/> at <paramref name="level"/> (the old game's
    /// <c>raceStatModifier</c>, <c>app.js</c> 1084–1091): its stat modifier plus, for each <see cref="AllStatsFlatPerLevelUp"/>
    /// of its passive, <c>max(0, level − 1) × amount</c>. A key that isn't an attribute's has no stat modifier (0) and gets
    /// the growth all the same, as there.
    /// </summary>
    public double StatModifier(string attribute, int level)
    {
        var amount = AttributeValues.Has(attribute) ? JsValues.ToNumber(StatModifiers.Values[attribute]) : 0;
        foreach (var effect in Passive.Effects)
            if (effect is AllStatsFlatPerLevelUp all)
                amount += Math.Max(0, level - 1) * JsValues.ToNumber(all.Amount);
        return amount;
    }

    /// <summary>A name is empty when JavaScript's <c>trim</c> leaves nothing (the old game's "name must be a nonempty string").</summary>
    [Check]
    static void Named(Race race, CheckContext check)
    {
        if (JsValues.Trim(race.Name).Length == 0)
            check.Report(RecordPath.Root.Field(nameof(Name)), "the name is empty");
    }

    /// <summary>Each modifier a finite number (the old game's "<c>&lt;attribute&gt;</c> modifier must be a number").</summary>
    [Check]
    static void Modifiers(Race race, CheckContext check)
    {
        foreach (var attribute in AttributeKeys.All)
        {
            var value = race.StatModifiers.Values[attribute];
            if (!double.IsFinite(value))
                check.Report(RecordPath.Root.Field(nameof(StatModifiers)).Field(attribute), $"the {attribute} modifier must be a number (it is {Number(value)})");
        }
    }

    /// <summary>The accent a colour, as the screens use it.</summary>
    [Check]
    static void AccentIsAColour(Race race, CheckContext check)
    {
        if (!IsColour(race.Accent))
            check.Report(RecordPath.Root.Field(nameof(Accent)), $"\"{race.Accent}\" isn't a colour (#rrggbb)");
    }

    /// <summary>An emblem's name one name of the data path rule (its pictures and model are found by it, Q44).</summary>
    [Check]
    static void EmblemIsAName(Race race, CheckContext check)
    {
        if (race.Emblem is { } emblem && (emblem.Contains('/') || PackPath.Problem(emblem) is not null))
            check.Report(RecordPath.Root.Field(nameof(Emblem)), $"\"{emblem}\" isn't an emblem's name (one name of small letters a–z, digits, '-', '_' and '.')");
    }

    /// <summary>Whether <paramref name="text"/> is "#rrggbb".</summary>
    public static bool IsColour(string text) => text.Length == 7 && text[0] == '#' && text.Skip(1).All(char.IsAsciiHexDigit);

    static string Number(double value) => double.IsNaN(value) ? "NaN" : double.IsPositiveInfinity(value) ? "Infinity" : "-Infinity";
}

/// <summary>A race's modifiers of the eight attributes (record <c>race.modifiers</c>; the old <c>statModifiers</c>), 0 when not given.</summary>
[Record("race.modifiers", 1)]
[Help("What the race adds to each attribute.")]
public sealed record RaceModifiers
{
    [Field(1)] [Help("Added to Strength.")] public double Strength { get; init; }
    [Field(2)] [Help("Added to Vitality.")] public double Vitality { get; init; }
    [Field(3)] [Help("Added to Mind.")] public double Mind { get; init; }
    [Field(4)] [Help("Added to Dexterity.")] public double Dexterity { get; init; }
    [Field(5)] [Help("Added to Intelligence.")] public double Intelligence { get; init; }
    [Field(6)] [Help("Added to Wisdom.")] public double Wisdom { get; init; }
    [Field(7)] [Help("Added to Speed.")] public double Speed { get; init; }
    [Field(8)] [Help("Added to Luck.")] public double Luck { get; init; }

    /// <summary>The eight as attribute values, by key.</summary>
    public AttributeValues Values => new(Strength, Vitality, Mind, Dexterity, Intelligence, Wisdom, Speed, Luck);
}

/// <summary>A race's passive (record <c>race.passive</c>): its id, name, when it applies, its text and its effects.</summary>
[Record("race.passive", 1)]
[Help("A race's passive: what it is called, when it applies, what it says and what it does.")]
public sealed record RacePassive
{
    /// <summary>What <see cref="Condition"/> means when it is empty (the old game's default).</summary>
    public const string AlwaysActive = "Always active";

    [Field(1)] [Help("The passive's id (the old game made it from the name when there was none).")] public string Id { get; init; } = "";
    [Field(2)] [Help("The passive's name as players see it.")] public required string Name { get; init; }
    [Field(3)] [Help("When it applies, as players read it (empty: Always active).")] public string Condition { get; init; } = AlwaysActive;
    [Field(4)] [Help("What it does, as players read it.")] public string Description { get; init; } = "";
    [Field(5)] [Help("What it does, as the game's rules read it: its effects in order.")] public IReadOnlyList<Effect> Effects { get; init; } = [];

    /// <summary>The condition as shown: <see cref="AlwaysActive"/> when empty, as the old game made it.</summary>
    public string ShownCondition => Condition.Length == 0 ? AlwaysActive : Condition;
}

/// <summary>
/// A colour the character maker offers for a race (record <c>race.swatch</c>, GM2-P7, Q17, Q20): its palette slot, its
/// name and its colour. No race has any yet: their values come with the approved look (Hero.md, "Left to the steps").
/// </summary>
[Record("race.swatch", 1)]
[Help("A colour the character maker offers for this race: named, for one palette slot.")]
public sealed record RaceSwatch
{
    [Field(1)] [Help("The palette slot it colours (skin, hair, eyes …).")] public string Slot { get; init; } = "";
    [Field(2)] [Help("The colour's name as players see it.")] public string Name { get; init; } = "";
    [Field(3)] [Help("The colour (#rrggbb).")] [Colour] public string Colour { get; init; } = "#000000";

    [Check]
    static void ColourIsAColour(RaceSwatch swatch, CheckContext check)
    {
        if (!Race.IsColour(swatch.Colour))
            check.Report(RecordPath.Root.Field(nameof(Colour)), $"\"{swatch.Colour}\" isn't a colour (#rrggbb)");
    }
}
