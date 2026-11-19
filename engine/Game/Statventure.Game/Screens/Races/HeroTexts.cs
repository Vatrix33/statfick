using System.Globalization;
using Statventure.Game.Data.Actors;
using Statventure.Game.Data.Effects;
using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;

namespace Statventure.Game.Screens.Races;

/// <summary>
/// The words the race carousel and the character maker show (GM2 step 8): the old race list's and appearance step's where
/// they had them (<c>old-game/scripts/app.js</c> <c>renderRaces</c> 2985–3017, <c>selectRace</c> 3038–3068,
/// <c>renderAppearanceOptions</c> 3108–3125, <c>index.html</c> 302–331), and the new screens' own, approved with their
/// captures (GM1-8).
/// </summary>
public static class HeroTexts
{
    /// <summary>The carousel's line under its heading (the old dialog's <c>.race-intro</c>).</summary>
    public const string RacesIntro = "Playable races are always available. Dev Mode also unlocks non-playable races.";

    /// <summary>The maker's line under its heading (the old appearance step's first sentence: GM3 brings the gear it named).</summary>
    public const string MakerIntro = "This is how you appear on the battlefield.";

    /// <summary>The old list's kind of a race: "Playable", the hero's own "Selected", an enemy-only one "Non-playable · Dev Mode".</summary>
    public static string Kind(Race race, bool selected) =>
        selected ? "Selected" : race.Playable ? "Playable" : "Non-playable · Dev Mode";

    /// <summary>The old status line of a race refused (<c>selectRace</c>): "Enable Dev Mode to select a non-playable race", "A chosen race can only be changed in Dev Mode".</summary>
    public static string Refusal(RaceRefusal refusal) => refusal switch
    {
        RaceRefusal.NotPlayable => "Enable Dev Mode to select a non-playable race",
        RaceRefusal.Locked => "A chosen race can only be changed in Dev Mode",
        _ => throw new ArgumentOutOfRangeException(nameof(refusal)),
    };

    /// <summary>The carousel's button: "Choose &lt;race&gt;", or "Race locked" (the old seal's title) where Dev Mode is needed to change the hero's race.</summary>
    public static string Choose(Race race, bool locked) => locked ? "Race locked" : $"Choose {race.Name}";

    /// <summary>The maker's button (the old appearance step's): "Begin as &lt;race&gt;", or "Use this look" for the hero's own race (Dev Mode).</summary>
    public static string Begin(Race race, bool own) => own ? "Use this look" : $"Begin as {race.Name}";

    /// <summary>The maker's eyebrow (the old one: the race's name in capitals, "· APPEARANCE").</summary>
    public static string MakerEyebrow(Race race) => $"{race.Name.ToUpperInvariant()} · APPEARANCE";

    /// <summary>
    /// The carousel's status line on a playable race with no look in the data (no body: the shipped data until GM2 steps 4a
    /// and 9, or a mod's race without bodies), its button disabled: shown, not choosable (the user, 2026-11-16).
    /// </summary>
    public static string NoLooks(Race race) => $"No looks for the {race.Name} yet";

    /// <summary>
    /// The maker's summary (the old "&lt;race&gt; · &lt;Male|Female&gt; · &lt;label&gt;", with the build): the race, the sex, the
    /// build, and the label of the preset the look started from when it has one.
    /// </summary>
    public static string Summary(Race race, LookHero look, string build, string? presetLabel) =>
        string.Join(" · ", new[] { race.Name, Capital(look.Sex), Capital(build), presetLabel }.Where(part => !string.IsNullOrEmpty(part)));

    /// <summary>The eight attributes' short names in the old order (<c>statDefinitions</c>, <c>app.js</c> 285–294).</summary>
    public static IReadOnlyList<(string Key, string Short)> Attributes { get; } =
    [
        (AttributeKeys.Strength, "STR"), (AttributeKeys.Vitality, "VIT"), (AttributeKeys.Mind, "MND"), (AttributeKeys.Dexterity, "DEX"),
        (AttributeKeys.Intelligence, "INT"), (AttributeKeys.Wisdom, "WIS"), (AttributeKeys.Speed, "SPD"), (AttributeKeys.Luck, "LCK"),
    ];

    /// <summary>A modifier as the old list writes it: <c>(amount &gt;= 0 ? "+" : "") + formatValue(amount)</c> ("+2", "-3", "+0").</summary>
    public static string Modifier(double amount) => (amount >= 0 ? "+" : "") + FormatValue(amount);

    /// <summary>
    /// The old game's <c>formatValue</c> (<c>app.js</c> 1627–1630): rounded to a tenth (<c>Math.round</c>'s halves toward
    /// +∞), a whole number as JavaScript's <c>String</c> writes it, else one decimal (<c>toFixed(1)</c>).
    /// </summary>
    public static string FormatValue(double value)
    {
        var rounded = JsNumbers.Round(value * 10) / 10;
        if (rounded == Math.Floor(rounded))
            return (rounded == 0 ? 0 : rounded).ToString("R", CultureInfo.InvariantCulture);
        return rounded.ToString("F1", CultureInfo.InvariantCulture);
    }

    /// <summary>A category of the maker's as it shows it (Q20: each named).</summary>
    public static string Category(string category) => category switch
    {
        "face" => "Face",
        "brows" => "Brows",
        "hair" => "Hair",
        "beard" => "Beard",
        "crest" => "Crest",
        "ears" => "Ears",
        "tail" => "Tail",
        "marks" => "Marks",
        "head-piece" => "Head piece",
        _ => Capital(category),
    };

    /// <summary>A palette slot as the maker's colours show it.</summary>
    public static string Slot(string slot) => slot switch
    {
        "skin" => "Skin",
        "hair" => "Hair",
        "hair.tie" => "Hair ties",
        "eyes" => "Eyes",
        "brows" => "Brows",
        "lips" => "Lips",
        "cloth" => "Cloth",
        "cloth.trim" => "Trim",
        "trousers" => "Trousers",
        "crest" => "Crest",
        "marking" => "Markings",
        "claws" => "Claws",
        "eye.white" => "Eye whites",
        "leather" => "Leather",
        "buckle" => "Buckles",
        _ => Capital(slot),
    };

    /// <summary>The sexes and builds as the maker shows them ("Male", "Slender").</summary>
    public static string Capital(string word) => word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..];
}
