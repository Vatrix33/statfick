using Statventure.Game.Data.Races;
using Statventure.Records;
using Statventure.Render3D;

namespace Statventure.Game.Data.Looks;

/// <summary>
/// A body the character maker builds a figure on (record <c>look.body</c>, the kind at <c>data/looks/bodies</c>; GM2-P7):
/// its race, sex (Q14) and build (Q15), and its model, which carries the rig family's skeleton and the race and sex's one
/// plain outfit (Q18). The race fixes the body and its defining traits (Q13); a look names its body
/// (<see cref="LookHero.Body"/>). One body per race, sex and build.
/// </summary>
[Kind("Look body", LookNames.BodiesFolder)]
[Help("A body the character maker builds a hero on: one for each race, sex and build, with its model.")]
[Record("look.body", 1)]
public sealed record LookBody
{
    [Field(1)] [Help("The race whose body it is.")] [References(typeof(Race))]
    public string Race { get; init; } = "";

    [Field(2)] [Help("Male or female (each race has a body of each).")] [Choices("male", "female")]
    public string Sex { get; init; } = LookNames.Male;

    [Field(3)] [Help("Slender, average or strong (each race and sex has the three).")] [Choices("slender", "average", "strong")]
    public string Build { get; init; } = LookNames.Average;

    [Field(4)] [Help("The body's model in the game's data (models/humanoid/bodies/…), with its skeleton and its plain outfit.")]
    [References(typeof(ModelData), Folder = LookNames.ModelsFolder, Rule = typeof(PathNames))]
    public string Model { get; init; } = "";

    /// <summary>"human male average": how the messages name the race, sex and build a body is.</summary>
    public string Words => $"{Race} {Sex} {Build}";

    /// <summary>The maker takes one body for a race, sex and build: two in the data (this one among them) are a problem.</summary>
    [Check]
    static void OneOfItsKind(LookBody body, CheckContext check)
    {
        if (check.Data is not { } data)
            return;
        var same = LookData.BodiesIn(data).Where(b => b.Value.Race == body.Race && b.Value.Sex == body.Sex && b.Value.Build == body.Build)
            .Select(b => b.Key).ToList();
        if (same.Count > 1)
            check.Report($"{same.Count} bodies are the {body.Words} body ({string.Join(", ", same)}): the maker takes one");
    }
}
