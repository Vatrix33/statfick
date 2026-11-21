using System.Reflection;
using Statventure.Records;

namespace Statventure.Game.Data.Looks;

// The character maker's records (GM2-P6, GM2-P7; Docs/Game/Hero.md, "The look as a parameter set", "The maker's parts
// model" and "How it works", step 6): a hero's look is its choices (look.hero, in the save), made from the bodies
// (look.body), the parts (look.part) and the presets (look.preset) the data holds, and the race's swatches (race). A
// look's models are references, refused when the model isn't in the data (the manager's GM2-P18, 2026-11-16): bodies,
// parts and presets enter data.pak with their models.

/// <summary>
/// The names the maker's records share: their folders, and the lists their fields choose from (each list written once, as
/// the <see cref="ChoicesAttribute"/> of one field, and read from it here; the other fields with the same choices are held
/// to it by the tests).
/// </summary>
public static class LookNames
{
    /// <summary>The bodies' folder (<see cref="LookBody"/>).</summary>
    public const string BodiesFolder = "data/looks/bodies";

    /// <summary>The parts' folder (<see cref="LookPart"/>).</summary>
    public const string PartsFolder = "data/looks/parts";

    /// <summary>The presets' folder (<see cref="LookPreset"/>).</summary>
    public const string PresetsFolder = "data/looks/presets";

    /// <summary>The models' folder the bodies and parts name theirs in (whole paths: <c>models/humanoid/bodies/human-male-average</c>).</summary>
    public const string ModelsFolder = "models";

    public const string Male = "male";
    public const string Female = "female";

    /// <summary>The build a preset takes (the old looks had one build per race, Q15's middle one).</summary>
    public const string Average = "average";

    /// <summary>The faces' category: a part whose models name faces (a mark, Q65) is drawn by the model of the look's face.</summary>
    public const string Face = "face";

    /// <summary>The two sexes (Q14), as a body's <see cref="LookBody.Sex"/> chooses them.</summary>
    public static IReadOnlyList<string> Sexes { get; } = Choices(typeof(LookBody), nameof(LookBody.Sex));

    /// <summary>The three builds (Q15), as a body's <see cref="LookBody.Build"/> chooses them.</summary>
    public static IReadOnlyList<string> Builds { get; } = Choices(typeof(LookBody), nameof(LookBody.Build));

    /// <summary>
    /// The maker's categories of parts, in the maker's order (Q16, Q19, Q22–Q24: the face, the brows, hair, a beard, a crest,
    /// ears, a tail, marks, a head piece), as a part's <see cref="LookPart.Category"/> chooses them. A look holds one part of
    /// each category its body offers (GM2-P6: a map from category to part).
    /// </summary>
    public static IReadOnlyList<string> Categories { get; } = Choices(typeof(LookPart), nameof(LookPart.Category));

    /// <summary>
    /// The figure's palette slots (GM2-P7's list as step 4a made it final: the gear's, GM3's, are apart, and the pupils' and
    /// glints' colours are fixed), as a part's <see cref="LookPart.Slots"/> chooses them. The same list as the humanoid
    /// scripts' <c>look.py</c> <c>SLOTS</c>, written twice for now (no list both sides read exists yet).
    /// </summary>
    public static IReadOnlyList<string> Slots { get; } = Choices(typeof(LookPart), nameof(LookPart.Slots));

    /// <summary>The choices of the field <paramref name="property"/> of <paramref name="type"/>.</summary>
    public static IReadOnlyList<string> Choices(Type type, string property) =>
        type.GetProperty(property)!.GetCustomAttribute<ChoicesAttribute>()!.Choices;
}
