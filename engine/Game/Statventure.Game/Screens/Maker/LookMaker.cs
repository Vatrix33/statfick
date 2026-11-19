using Statventure.Game.Data.Looks;
using Statventure.Game.Data.Races;
using Statventure.Game.Random;

namespace Statventure.Game.Screens.Maker;

/// <summary>
/// The look being made in the character maker for one race (GM2-1, GM2-6; Q13–Q24, Q21; Docs/Game/Hero.md, "The new
/// hero's flow" and "How it works", step 8): the choices the race's bodies, parts and swatches in the game's data allow
/// (step 6's <see cref="LookData"/>), and the look they make, which every check of <c>look.hero</c> passes by
/// construction (a body of the race, one part of each category its body offers, a swatch of each slot the race has
/// swatches for). A preset sets every choice (its <see cref="LookHero.Preset"/> the look started from, kept through later
/// changes: "for the maker's display only"); a changed sex or build takes the body of the same build (or of the same
/// sex), keeping each part its new body offers and taking the category's first option otherwise; the random roll
/// (<see cref="Roll"/>) draws every choice uniformly over its list, no locks (Q21), "none" an option like the others where
/// a category has it (Q23, Q24). No look at all when the race has no body in the data.
/// </summary>
public sealed class LookMaker
{
    readonly LookData _looks;

    /// <summary>The maker for <paramref name="raceId"/> (<paramref name="race"/>) on <paramref name="looks"/>, starting from <see cref="Initial"/>.</summary>
    public LookMaker(string raceId, Race race, LookData looks, LookHero? start = null)
    {
        RaceId = raceId;
        Race = race;
        _looks = looks;
        Look = Initial(start);
    }

    /// <summary>The race's id.</summary>
    public string RaceId { get; }

    /// <summary>The race (its swatches are the colours offered).</summary>
    public Race Race { get; }

    /// <summary>The look as chosen now; null when the race has no body in the data (nothing can be made).</summary>
    public LookHero? Look { get; private set; }

    /// <summary>The race's presets (their names), in the maker's list order (<see cref="LookData.PresetsOf"/>).</summary>
    public IReadOnlyList<string> Presets => _looks.PresetsOf(RaceId);

    /// <summary>A preset's record (one of <see cref="Presets"/>).</summary>
    public LookPreset Preset(string name) => _looks.Presets[name];

    /// <summary>The sexes the race has a body for, in Q14's order (male, female).</summary>
    public IReadOnlyList<string> Sexes => [.. LookNames.Sexes.Where(sex => BuildsOf(sex).Count > 0)];

    /// <summary>The builds the race has a body for in <paramref name="sex"/>, in Q15's order (slender, average, strong).</summary>
    public IReadOnlyList<string> BuildsOf(string sex) => [.. LookNames.Builds.Where(build => _looks.BodyOf(RaceId, sex, build) is not null)];

    /// <summary>The look's body (null without a look).</summary>
    public LookBody? Body => Look is { } look ? _looks.Bodies[look.Body] : null;

    /// <summary>The look's build (null without a look).</summary>
    public string? Build => Body?.Build;

    /// <summary>The categories the look's body offers, each with its options (part names), in the maker's order (<see cref="LookData.Offered"/>).</summary>
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Categories =>
        Look is { } look ? _looks.Offered(look.Body, _looks.Bodies[look.Body]) : new Dictionary<string, IReadOnlyList<string>>();

    /// <summary>A part's record (an option of <see cref="Categories"/>).</summary>
    public LookPart Part(string name) => _looks.Parts[name];

    /// <summary>The palette slots the race has swatches for, in the slots' order (<see cref="LookNames.Slots"/>): a colour is chosen for each.</summary>
    public IReadOnlyList<string> Slots => [.. LookNames.Slots.Where(slot => Race.Swatches.Any(s => s.Slot == slot))];

    /// <summary>The race's swatches for <paramref name="slot"/>, in the race's order.</summary>
    public IReadOnlyList<RaceSwatch> SwatchesOf(string slot) => [.. Race.Swatches.Where(s => s.Slot == slot)];

    /// <summary>A preset picked: its look, every choice set; false when it is the look already.</summary>
    /// <exception cref="ArgumentException">It isn't one of the race's presets.</exception>
    public bool UsePreset(string name)
    {
        if (!Presets.Contains(name, StringComparer.Ordinal))
            throw new ArgumentException($"\"{name}\" isn't one of the {RaceId}'s presets", nameof(name));
        return Set(Fit(_looks.Presets[name].Look) with { Preset = name });
    }

    /// <summary>The sex changed: the body of that sex in the look's build, else its first build; false when it is the sex already.</summary>
    /// <exception cref="ArgumentException">The race has no body of that sex.</exception>
    public bool SetSex(string sex)
    {
        var look = Look ?? throw new InvalidOperationException($"the {RaceId} has no body");
        var builds = BuildsOf(sex);
        if (builds.Count == 0)
            throw new ArgumentException($"the {RaceId} has no {sex} body", nameof(sex));
        var build = builds.Contains(Build!, StringComparer.Ordinal) ? Build! : builds[0];
        return Set(Fit(look with { Sex = sex, Body = _looks.BodyOf(RaceId, sex, build)! }));
    }

    /// <summary>The build changed: the body of the look's sex in that build; false when it is the build already.</summary>
    /// <exception cref="ArgumentException">The race has no body of the look's sex in that build.</exception>
    public bool SetBuild(string build)
    {
        var look = Look ?? throw new InvalidOperationException($"the {RaceId} has no body");
        var body = _looks.BodyOf(RaceId, look.Sex, build) ?? throw new ArgumentException($"the {RaceId} has no {look.Sex} {build} body", nameof(build));
        return Set(Fit(look with { Body = body }));
    }

    /// <summary>A part chosen for its category; false when it is the choice already.</summary>
    /// <exception cref="ArgumentException">The body doesn't offer it for that category.</exception>
    public bool SetPart(string category, string part)
    {
        var look = Look ?? throw new InvalidOperationException($"the {RaceId} has no body");
        if (!Categories.TryGetValue(category, out var options) || !options.Contains(part, StringComparer.Ordinal))
            throw new ArgumentException($"\"{part}\" isn't a {category} the body \"{look.Body}\" offers", nameof(part));
        return Set(look with { Parts = With(look.Parts, category, part) });
    }

    /// <summary>A swatch chosen for its slot; false when it is the choice already.</summary>
    /// <exception cref="ArgumentException">The race has no such swatch for that slot.</exception>
    public bool SetColour(string slot, string swatch)
    {
        var look = Look ?? throw new InvalidOperationException($"the {RaceId} has no body");
        if (!SwatchesOf(slot).Any(s => s.Name == swatch))
            throw new ArgumentException($"the {RaceId} has no {slot} colour \"{swatch}\"", nameof(swatch));
        return Set(look with { Colours = With(look.Colours, slot, swatch) });
    }

    /// <summary>
    /// The random button (Q21): every choice drawn uniformly over its list, one draw each, in this order: the sex (over
    /// <see cref="Sexes"/>), the build (over its <see cref="BuildsOf"/>), each category the body then offers (in the maker's
    /// order, over its options), each slot (in the slots' order, over its swatches); no preset. Nothing when the race has
    /// no body.
    /// </summary>
    public void Roll(IGameRandom random)
    {
        if (Look is null)
            return;
        var sex = random.Pick(Sexes);
        var body = _looks.BodyOf(RaceId, sex, random.Pick(BuildsOf(sex)))!;
        var parts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (category, options) in _looks.Offered(body, _looks.Bodies[body]))
            parts[category] = random.Pick(options);
        var colours = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var slot in Slots)
            colours[slot] = random.Pick(SwatchesOf(slot)).Name;
        Look = new LookHero { Race = RaceId, Sex = sex, Body = body, Parts = parts, Colours = colours, Preset = null };
    }

    /// <summary>
    /// Where the maker starts: <paramref name="start"/> when it is a look of this race (the hero's, in Dev Mode: the old
    /// "Use this look"), else the race's first preset, else a plain look (the first sex and its average build, or its first;
    /// each category's first option, each slot's first swatch); fitted to the data (<see cref="Fit"/>). Null when the race
    /// has no body.
    /// </summary>
    LookHero? Initial(LookHero? start)
    {
        if (Sexes.Count == 0)
            return null;
        if (start is not null && _looks.Bodies.TryGetValue(start.Body, out var body) && body.Race == RaceId)
            return Fit(start);
        if (Presets.Count > 0)
            return Fit(_looks.Presets[Presets[0]].Look) with { Preset = Presets[0] };
        var sex = Sexes[0];
        var builds = BuildsOf(sex);
        var build = builds.Contains(LookNames.Average, StringComparer.Ordinal) ? LookNames.Average : builds[0];
        return Fit(new LookHero { Race = RaceId, Sex = sex, Body = _looks.BodyOf(RaceId, sex, build)! });
    }

    /// <summary>
    /// <paramref name="look"/>, on a body of the race (every caller's: a preset's, the hero's own checked by
    /// <see cref="Initial"/>, the one a sex or a build gives), made one the data allows: each category its body offers kept
    /// when it is one of the options and the first option otherwise, the others dropped; each slot the race has swatches
    /// for kept when it names one, the first otherwise, the others dropped.
    /// </summary>
    LookHero Fit(LookHero look)
    {
        var body = look.Body;
        var parts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (category, options) in _looks.Offered(body, _looks.Bodies[body]))
            parts[category] = look.Parts.TryGetValue(category, out var part) && options.Contains(part, StringComparer.Ordinal) ? part : options[0];
        var colours = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var slot in Slots)
        {
            var swatches = SwatchesOf(slot);
            colours[slot] = look.Colours.TryGetValue(slot, out var name) && swatches.Any(s => s.Name == name) ? name : swatches[0].Name;
        }
        return look with { Race = RaceId, Sex = _looks.Bodies[body].Sex, Body = body, Parts = parts, Colours = colours };
    }

    bool Set(LookHero look)
    {
        if (Equals(look, Look))
            return false;
        Look = look;
        return true;
    }

    static Dictionary<string, string> With(IReadOnlyDictionary<string, string> map, string key, string value) =>
        new(map, StringComparer.Ordinal) { [key] = value };
}
