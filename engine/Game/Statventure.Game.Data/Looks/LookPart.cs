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
/// Since version 2 (the manager's GM2-P36, GM2 step 9a) a part has its place in its category's list (<see cref="Order"/>),
/// as a preset has in its race's: the maker lists, rolls and refits by it.
/// </summary>
[Kind("Look part", LookNames.PartsFolder)]
[Help("An option of the character maker: a face, hair, a beard, a crest, ears, a tail, marks or a head piece, with its models for the bodies it fits.")]
[Record("look.part", 2)]
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

    [Field(7)] [Help("Where it comes in its category's list: lower numbers first, the same number by name. The game's: \"None\" 0, then 10, 20 … in the list's approved order.")]
    public int Order { get; init; }

    /// <summary>Version 1 (GM2 step 6): no place in its category's list (its options came in their names' order).</summary>
    [Record("look.part", 1)]
    public sealed record V1
    {
        [Field(1)] public string Category { get; init; } = "hair";
        [Field(2)] public string Name { get; init; } = "";
        [Field(3)] public IReadOnlyList<string> Races { get; init; } = [];
        [Field(4)] public IReadOnlyList<string> Sexes { get; init; } = [LookNames.Male, LookNames.Female];
        [Field(5)] public IReadOnlyList<LookPartModel> Models { get; init; } = [];
        [Field(6)] public IReadOnlyList<string> Slots { get; init; } = [];

        /// <summary>Every field kept; its place 0 (a list of version-1 parts keeps their names' order).</summary>
        public LookPart Upgrade() => new() { Category = Category, Name = Name, Races = Races, Sexes = Sexes, Models = Models, Slots = Slots };
    }

    /// <summary>
    /// Whether the part is offered on the body <paramref name="name"/> (<paramref name="body"/>) with the face
    /// <paramref name="face"/> (a look's face part; null: none chosen): its race and sex among the part's, and a model that
    /// fits that body and face (<see cref="LookPartModel.Fits"/>) unless the part has none.
    /// </summary>
    public bool IsFor(string name, LookBody body, string? face = null) => Refusal(name, body, face) is null;

    /// <summary>Why the part isn't offered on the body <paramref name="name"/> with the face <paramref name="face"/> (a phrase naming the part as "it"), or null when it is.</summary>
    public string? Refusal(string name, LookBody body, string? face = null) =>
        !Races.Contains(body.Race, StringComparer.Ordinal) ? $"isn't for the {body.Race}"
        : !Sexes.Contains(body.Sex, StringComparer.Ordinal) ? $"isn't for a {body.Sex} body"
        : Models.Count > 0 && !Models.Any(m => m.Bodies.Contains(name, StringComparer.Ordinal)) ? $"has no model for the body \"{name}\""
        : Models.Count > 0 && !Models.Any(m => m.Fits(name, face)) ? (face is null ? $"has no model for the body \"{name}\" without a face" : $"has no model for the face \"{face}\" on the body \"{name}\"")
        : null;

    /// <summary>The model to draw on the body <paramref name="body"/> with the face <paramref name="face"/>: the first that fits both, or null (none fits, or the part has no model).</summary>
    public LookPartModel? ModelFor(string body, string? face) => Models.FirstOrDefault(m => m.Fits(body, face));

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

    /// <summary>
    /// Each model for one body at least, each body and face one model's (a model naming no faces is every face's on its
    /// bodies, Q65), each body of the part's races and sexes; a face's own models name no faces; with the data, each face a
    /// model names a face offered on each of its bodies.
    /// </summary>
    [Check]
    static void ModelsFitItsBodies(LookPart part, CheckContext check)
    {
        var fitted = new Dictionary<string, int>(StringComparer.Ordinal);
        var everyFace = new Dictionary<string, int>(StringComparer.Ordinal);
        var byFace = new Dictionary<(string Body, string Face), int>();
        for (var i = 0; i < part.Models.Count; i++)
        {
            var model = part.Models[i];
            var at = RecordPath.Root.Field(nameof(Models)).Index(i);
            var bodies = model.Bodies;
            if (bodies.Count == 0)
                check.Report(at.Field(nameof(LookPartModel.Bodies)), "a model is for one body at least");
            Once(model.Faces, at.Field(nameof(LookPartModel.Faces)), check);
            if (part.Category == LookNames.Face && model.Faces.Count > 0)
                check.Report(at.Field(nameof(LookPartModel.Faces)), "a face's model names no faces (it is the face)");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var j = 0; j < bodies.Count; j++)
            {
                var body = bodies[j];
                var bodyAt = at.Field(nameof(LookPartModel.Bodies)).Index(j);
                if (!seen.Add(body))
                {
                    check.Report(bodyAt, $"\"{body}\" is there twice");
                    continue;
                }
                if (everyFace.TryGetValue(body, out var whole) || model.Faces.Count == 0 && fitted.TryGetValue(body, out whole))
                {
                    check.Report(bodyAt, $"the body \"{body}\" has a model already (models[{whole}])");
                    continue;
                }
                fitted.TryAdd(body, i);
                if (model.Faces.Count == 0)
                {
                    everyFace[body] = i;
                    continue;
                }
                for (var k = 0; k < model.Faces.Count; k++)
                    if (!byFace.TryAdd((body, model.Faces[k]), i) && byFace[(body, model.Faces[k])] != i)
                        check.Report(at.Field(nameof(LookPartModel.Faces)).Index(k),
                            $"the face \"{model.Faces[k]}\" on the body \"{body}\" has a model already (models[{byFace[(body, model.Faces[k])]}])");
            }
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
            var faces = part.Models[i].Faces;
            for (var k = 0; k < faces.Count; k++)
            {
                if (LookData.Part(data, faces[k]) is not { } face)
                    continue;
                var at = RecordPath.Root.Field(nameof(Models)).Index(i).Field(nameof(LookPartModel.Faces)).Index(k);
                if (face.Category != LookNames.Face)
                {
                    check.Report(at, $"\"{faces[k]}\" is a {face.Category}, not a face");
                    continue;
                }
                foreach (var name in bodies.Distinct(StringComparer.Ordinal))
                    if (LookData.Body(data, name) is { } body && face.Refusal(name, body) is { } refusal)
                        check.Report(at, $"the face \"{faces[k]}\" {refusal}");
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

/// <summary>
/// One model of a part, the bodies it is fitted to and the faces it fits (record <c>look.part.model</c>): one model file
/// may serve several bodies (a part fitted once per race and sex, say, for its three builds, GM2-P25). Since version 2 (the
/// user's Q65, GM2 step 9a) a model may name the faces it fits: a mark lies exactly on a face's skin, so it is made for each
/// face, and a look takes the model fitted to its body and its face; a model naming no faces fits any face.
/// </summary>
[Record("look.part.model", 2)]
[Help("A model of the part, the bodies it is fitted to and the faces it fits.")]
public sealed record LookPartModel
{
    [Field(1)] [Help("The model in the game's data (models/humanoid/parts/…).")]
    [References(typeof(ModelData), Folder = LookNames.ModelsFolder, Rule = typeof(PathNames))]
    public string Model { get; init; } = "";

    [Field(2)] [Help("The bodies it is fitted to.")] [References(typeof(LookBody))]
    public IReadOnlyList<string> Bodies { get; init; } = [];

    [Field(3)] [Help("The faces it fits, for a part that lies on a face (a mark); none: it fits any face.")] [References(typeof(LookPart))]
    public IReadOnlyList<string> Faces { get; init; } = [];

    /// <summary>Whether the model is the one for the body <paramref name="body"/> with the face <paramref name="face"/> (none chosen: null): fitted to the body, and naming the face or no face.</summary>
    public bool Fits(string body, string? face) =>
        Bodies.Contains(body, StringComparer.Ordinal) && (Faces.Count == 0 || face is not null && Faces.Contains(face, StringComparer.Ordinal));

    /// <summary>Version 1 (GM2 step 6): a model and its bodies, no faces (each model fits any face).</summary>
    [Record("look.part.model", 1)]
    public sealed record V1
    {
        [Field(1)] public string Model { get; init; } = "";
        [Field(2)] public IReadOnlyList<string> Bodies { get; init; } = [];

        public LookPartModel Upgrade() => new() { Model = Model, Bodies = Bodies };
    }
}
