using Statventure.Game.Data.Races;
using Statventure.Records;

namespace Statventure.Game.Data.Looks;

/// <summary>
/// A hero's look as its choices (record <c>look.hero</c>; GM2-P6; Docs/Game/Hero.md, "The look as a parameter set"): the
/// race, the sex (Q14) and the body (its build, Q15), one part for each category the body offers (the face, hair …; Q13,
/// Q16, Q19, Q22–Q24), a colour for each palette slot the race has swatches for (a swatch of the race's, by its name: Q17
/// left no free colour), and the preset it started from (GM2-6, for the maker's display only). Kept in the save
/// (<c>save.game</c> v2) and so in the character file, and in each preset; the figure is built from it at run time, so
/// nothing of the figure is stored but the choices. Its checks, run with the data, refuse what the maker couldn't make: a
/// body of another race or sex, a part its body isn't offered (its race, sex or build), a category the body offers left
/// out, a colour the race has no swatch of (G4). Whether the race, body, parts and preset are there at all is their
/// references' check (the editor's, the mod check's, a character file's at loading).
/// </summary>
[NotAPackKind]
[Help("A hero's look: the race, sex and body, a part of each category and a colour of each slot, chosen in the character maker.")]
[Record("look.hero", 1)]
public sealed record LookHero
{
    [Field(1)] [Help("The race whose look it is (the hero's).")] [References(typeof(Race))]
    public string Race { get; init; } = "";

    [Field(2)] [Help("Male or female.")] [Choices("male", "female")]
    public string Sex { get; init; } = LookNames.Male;

    [Field(3)] [Help("The body: the race's for the sex and the build chosen.")] [References(typeof(LookBody))]
    public string Body { get; init; } = "";

    [Field(4)] [Help("The part chosen for each category the body offers (face, hair, beard …).")] [References(typeof(LookPart))]
    public IReadOnlyDictionary<string, string> Parts { get; init; } = new Dictionary<string, string>();

    [Field(5)] [Help("The colour chosen for each palette slot the race has swatches for: the swatch's name.")]
    public IReadOnlyDictionary<string, string> Colours { get; init; } = new Dictionary<string, string>();

    [Field(6)] [Help("The preset the look started from, as the maker shows it (none: none).")] [References(typeof(LookPreset))]
    public string? Preset { get; init; }

    // A look is its choices: two looks with the same choices are equal whatever the maps' order.
    public bool Equals(LookHero? other) =>
        other is not null && Race == other.Race && Sex == other.Sex && Body == other.Body && Preset == other.Preset
        && Same(Parts, other.Parts) && Same(Colours, other.Colours);

    public override int GetHashCode() => HashCode.Combine(Race, Sex, Body, Preset, Parts.Count, Colours.Count);

    static bool Same(IReadOnlyDictionary<string, string> a, IReadOnlyDictionary<string, string> b) =>
        a.Count == b.Count && a.All(pair => b.TryGetValue(pair.Key, out var value) && value == pair.Value);

    /// <summary>The parts' keys are categories and the colours' keys palette slots (with or without the data).</summary>
    [Check]
    static void KnownKeys(LookHero look, CheckContext check)
    {
        foreach (var category in look.Parts.Keys)
            if (!LookNames.Categories.Contains(category, StringComparer.Ordinal))
                check.Report(RecordPath.Root.Field(nameof(Parts)).Key(category), $"\"{category}\" isn't a category ({string.Join(", ", LookNames.Categories)})");
        foreach (var slot in look.Colours.Keys)
            if (!LookNames.Slots.Contains(slot, StringComparer.Ordinal))
                check.Report(RecordPath.Root.Field(nameof(Colours)).Key(slot), $"\"{slot}\" isn't a palette slot ({string.Join(", ", LookNames.Slots)})");
    }

    /// <summary>The body the look's race's and sex's; each part of its category and offered on the body; every category the body offers chosen.</summary>
    [Check]
    static void BodyAndParts(LookHero look, CheckContext check)
    {
        if (check.Data is not { } data)
            return;
        var body = LookData.Body(data, look.Body);
        if (body is not null)
        {
            if (body.Race != look.Race)
                check.Report(RecordPath.Root.Field(nameof(Body)), $"the body \"{look.Body}\" is the {body.Race}'s, not the {look.Race}'s");
            else if (body.Sex != look.Sex)
                check.Report(RecordPath.Root.Field(nameof(Body)), $"the body \"{look.Body}\" is a {body.Sex} body, not a {look.Sex} one");
        }
        // A part lying on a face (a mark, Q65) is offered with the look's face only.
        var face = LookData.FaceOf(look);
        foreach (var (category, name) in look.Parts)
        {
            if (!LookNames.Categories.Contains(category, StringComparer.Ordinal) || LookData.Part(data, name) is not { } part)
                continue;
            var at = RecordPath.Root.Field(nameof(Parts)).Key(category);
            if (part.Category != category)
                check.Report(at, $"\"{name}\" is a {part.Category}, not a {category}");
            else if (body is not null && part.Refusal(look.Body, body, face) is { } refusal)
                check.Report(at, $"\"{name}\" {refusal}");
        }
        if (body is null)
            return;
        var offered = LookData.Offered(LookData.PartsIn(data), look.Body, body, face);
        foreach (var category in LookNames.Categories)
            if (offered.TryGetValue(category, out var options) && !look.Parts.ContainsKey(category))
                check.Report(RecordPath.Root.Field(nameof(Parts)), $"no {category} chosen (the body \"{look.Body}\" has {options.Count} to choose from)");
    }

    /// <summary>Each colour a swatch of the race's for its slot, by name; every slot the race has swatches for chosen.</summary>
    [Check]
    static void ColoursOfTheRace(LookHero look, CheckContext check)
    {
        if (check.Data is not { } data || LookData.RaceOf(data, look.Race) is not { } race)
            return;
        foreach (var (slot, name) in look.Colours)
        {
            if (!LookNames.Slots.Contains(slot, StringComparer.Ordinal))
                continue;
            var at = RecordPath.Root.Field(nameof(Colours)).Key(slot);
            var swatches = race.Swatches.Where(s => s.Slot == slot).ToList();
            if (swatches.Count == 0)
                check.Report(at, $"the {look.Race} has no {slot} colours");
            else if (!swatches.Any(s => s.Name == name))
                check.Report(at, $"the {look.Race} has no {slot} colour \"{name}\"");
        }
        foreach (var slot in race.Swatches.Select(s => s.Slot).Distinct(StringComparer.Ordinal))
            if (!look.Colours.ContainsKey(slot))
                check.Report(RecordPath.Root.Field(nameof(Colours)), $"no {slot} colour chosen (the {look.Race} has {race.Swatches.Count(s => s.Slot == slot)} to choose from)");
    }
}
