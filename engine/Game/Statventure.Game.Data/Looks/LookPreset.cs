using Statventure.Records;

namespace Statventure.Game.Data.Looks;

/// <summary>
/// A preset of the character maker (record <c>look.preset</c>, the kind at <c>data/looks/presets</c>; GM2-6, GM2-P7, Q20,
/// Q21): a look with its name in the maker's list, for its look's race and sex. The game's are the old game's 72 looks
/// rebuilt (<c>sprite-catalog.js</c>: each race's four per sex, under their old labels, in the old order: the male ones 10–40,
/// the female ones 50–80, as the races' order is, Q43), named <c>&lt;race&gt;-&lt;sex&gt;-&lt;n&gt;</c>; a mod adds its own
/// (Q39). Picking one sets every choice of the maker to its look (and the look's <see cref="LookHero.Preset"/> to it).
/// </summary>
[Kind("Look preset", LookNames.PresetsFolder)]
[Help("A ready-made look the character maker offers for a race and sex: its name in the list and every choice.")]
[Record("look.preset", 1)]
public sealed record LookPreset
{
    [Field(1)] [Help("Its name in the maker's list (the old game's labels: Wayfarer, Sellsword …).")]
    public string Label { get; init; } = "";

    [Field(2)] [Help("Where it comes in its race's list: lower numbers first, the same number by name. The game's are 10–40 (male) and 50–80 (female) in the old game's order.")]
    public int Order { get; init; }

    [Field(3)] [Help("The look it sets: the race, sex and body, the parts and the colours.")]
    public LookHero Look { get; init; } = new();

    [Check]
    static void Labelled(LookPreset preset, CheckContext check)
    {
        if (string.IsNullOrWhiteSpace(preset.Label))
            check.Report(RecordPath.Root.Field(nameof(Label)), "the label is empty");
    }

    /// <summary>A preset's look names no preset (a hero's names the one it started from).</summary>
    [Check]
    static void NoPresetInside(LookPreset preset, CheckContext check)
    {
        if (preset.Look.Preset is not null)
            check.Report(RecordPath.Root.Field(nameof(Look)).Field(nameof(LookHero.Preset)), "a preset's look names no preset");
    }
}
