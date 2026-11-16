using Statventure.Game.Data.Races;
using Statventure.Records;
using Statventure.Render3D;

namespace Statventure.Game.Data.Looks;

/// <summary>
/// One option of one of the maker's categories (record <c>look.part</c>, the kind at <c>data/looks/parts</c>; GM2-P7,
/// Q19, Q20, Q39): a face, a hair style, a beard, a crest, ears, a tail, marks or a head piece, named as the maker shows it,
/// for the races and sexes it lists, with its models: each made for (fitted to) one or more bodies, so several bodies can
/// wear one model file. A part with no model at all is an option drawn as nothing ("None", "Bald": Q23, Q24), offered on
/// every body of its races and sexes; a part with models is offered on the bodies they name. A category has "none" where
/// such a part is among its options, and is a choice every look makes where it isn't (the Half-Beast's ears and tail, Q13).
/// </summary>
[Kind("Look part", LookNames.PartsFolder)]
[Help("An option of the character maker: a face, hair, a beard, a crest, ears, a tail, marks or a head piece, with its models for the bodies it fits.")]
[Record("look.part", 1)]
public sealed record LookPart
{
    [Field(1)] [Help("The maker's category it is an option of.")]
    [Choices("face", "brows", "hair", "beard", "crest", "ears", "tail", "marks", "head-piece")]
    public string Category { get; init; } = "hair";

    [Field(2)] [Help("Its name as the maker shows it (Braid, Clean-shaven, None …).")]
    public string Name { get; init; } = "";

    [Field(3)] [Help("The races it is offered to.")] [References(typeof(Race))]
    public IReadOnlyList<string> Races { get; init; } = [];

    [Field(4)] [Help("The sexes it is offered to.")] [Choices("male", "female")]
    public IReadOnlyList<string> Sexes { get; init; } = [LookNames.Male, LookNames.Female];

    [Field(5)] [Help("Its models, each with the bodies it is fitted to (none: an option drawn as nothing, such as None or Bald).")]
    public IReadOnlyList<LookPartModel> Models { get; init; } = [];

    [Field(6)] [Help("The palette slots its materials take their colours from.")]
    [Choices("skin", "hair", "hair.tie", "eyes", "brows", "lips", "cloth", "cloth.trim", "trousers", "crest", "marking", "claws", "eye.white", "leather", "buckle")]
    public IReadOnlyList<string> Slots { get; init; } = [];

    /// <summary>Whether the part is offered on the body <paramref name="name"/> (<paramref name="body"/>): its race and sex among the part's, and a model fitted to it unless the part has none.</summary>
    public bool IsFor(string name, LookBody body) => Refusal(name, body) is null;

    /// <summary>Why the part isn't offered on the body <paramref name="name"/> (a phrase naming the part as "it"), or null when it is.</summary>
    public string? Refusal(string name, LookBody body) =>
        !Races.Contains(body.Race, StringComparer.Ordinal) ? $"isn't for the {body.Race}"
        : !Sexes.Contains(body.Sex, StringComparer.Ordinal) ? $"isn't for a {body.Sex} body"
        : Models.Count > 0 && !Models.Any(m => m.Bodies.Contains(name, StringComparer.Ordinal)) ? $"has no model for the body \"{name}\""
        : null;

    [Check]
    static void Named(LookPart part, CheckContext check)
    {
        if (string.IsNullOrWhiteSpace(part.Name))
            check.Report(RecordPath.Root.Field(nameof(Name)), "the name is empty");
    }

    [Check]
    static void ForSomeone(LookPart part, CheckContext check)
    {
        if (part.Races.Count == 0)
            check.Report(RecordPath.Root.Field(nameof(Races)), "a part is for one race at least");
        if (part.Sexes.Count == 0)
            check.Report(RecordPath.Root.Field(nameof(Sexes)), "a part is for one sex at least");
        Once(part.Races, RecordPath.Root.Field(nameof(Races)), check);
        Once(part.Sexes, RecordPath.Root.Field(nameof(Sexes)), check);
        Once(part.Slots, RecordPath.Root.Field(nameof(Slots)), check);
    }

    /// <summary>Each model for one body at least, each body one model's, each body of the part's races and sexes.</summary>
    [Check]
    static void ModelsFitItsBodies(LookPart part, CheckContext check)
    {
        var fitted = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < part.Models.Count; i++)
        {
            var at = RecordPath.Root.Field(nameof(Models)).Index(i).Field(nameof(LookPartModel.Bodies));
            var bodies = part.Models[i].Bodies;
            if (bodies.Count == 0)
                check.Report(at, "a model is for one body at least");
            for (var j = 0; j < bodies.Count; j++)
                if (!fitted.TryAdd(bodies[j], i))
                    check.Report(at.Index(j), fitted[bodies[j]] == i ? $"\"{bodies[j]}\" is there twice" : $"the body \"{bodies[j]}\" has a model already (models[{fitted[bodies[j]]}])");
        }
        if (check.Data is not { } data)
            return;
        for (var i = 0; i < part.Models.Count; i++)
        {
            var bodies = part.Models[i].Bodies;
            for (var j = 0; j < bodies.Count; j++)
            {
                if (LookData.Body(data, bodies[j]) is not { } body)
                    continue;
                var at = RecordPath.Root.Field(nameof(Models)).Index(i).Field(nameof(LookPartModel.Bodies)).Index(j);
                if (!part.Races.Contains(body.Race, StringComparer.Ordinal))
                    check.Report(at, $"the body \"{bodies[j]}\" is the {body.Race}'s, and the part isn't for the {body.Race}");
                else if (!part.Sexes.Contains(body.Sex, StringComparer.Ordinal))
                    check.Report(at, $"the body \"{bodies[j]}\" is a {body.Sex} body, and the part isn't for a {body.Sex} body");
            }
        }
    }

    static void Once(IReadOnlyList<string> names, RecordPath at, CheckContext check)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < names.Count; i++)
            if (!seen.Add(names[i]))
                check.Report(at.Index(i), $"\"{names[i]}\" is there twice");
    }
}

/// <summary>One model of a part and the bodies it is fitted to (record <c>look.part.model</c>): one model file may serve several bodies (a part fitted once per race and sex, say, for its three builds).</summary>
[Record("look.part.model", 1)]
[Help("A model of the part and the bodies it is fitted to.")]
public sealed record LookPartModel
{
    [Field(1)] [Help("The model in the game's data (models/humanoid/parts/…).")]
    [References(typeof(ModelData), Folder = LookNames.ModelsFolder, Rule = typeof(PathNames))]
    public string Model { get; init; } = "";

    [Field(2)] [Help("The bodies it is fitted to.")] [References(typeof(LookBody))]
    public IReadOnlyList<string> Bodies { get; init; } = [];
}
